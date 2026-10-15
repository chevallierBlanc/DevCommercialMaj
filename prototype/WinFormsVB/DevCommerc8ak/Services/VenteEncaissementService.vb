Option Strict On
Option Explicit On
Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.SqlClient
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Text
Imports System.Web.Script.Serialization

Namespace DevCommerc8ak
    Public Class ConfirmationComptoirIncertaineException
        Inherits Exception
        Public Sub New(operation As Guid, cause As Exception)
            MyBase.New("Confirmation SQL incertaine pour " & operation.ToString("D") & ". Ne ressaisissez pas la vente : réessayez la même confirmation ou faites vérifier l'historique.", cause)
        End Sub
    End Class
    Public Class VenteComptoirNonFinaliseeException
        Inherits InvalidOperationException
        Public Sub New(cause As Exception)
            MyBase.New("Vente non finalisée, transaction annulée : " & cause.Message, cause)
        End Sub
    End Class
    Public Class VenteEncaissementService
        Private ReadOnly _dal As DAL
        Public Sub New(dal As DAL)
            _dal = dal
        End Sub
        Public Sub VerifierAcces()
            Try
                Using cn As SqlConnection = _dal.CreerConnexion()
                    cn.Open()
                    Using tx As SqlTransaction = cn.BeginTransaction()
                        ExigerContexte(cn, tx)
                        tx.Commit()
                    End Using
                End Using
            Catch ex As Exception
                AuditMetierService.Echec(_dal, "VENTE_COMPTOIR_ACCES_REFUSE", SessionUtilisateur.SessionId, Guid.NewGuid(), ex, entite:="Session")
                Throw
            End Try
        End Sub
        Private Shared Sub ExigerContexte(cn As SqlConnection, tx As SqlTransaction, Optional lectureDocument As Boolean = False)
            If Not lectureDocument Then AutorisationActionService.Exiger(cn, tx, "FACTURE_CREER", "FACTURIER")
            AutorisationActionService.Exiger(cn, tx, "ENCAISSEMENT_CREER", "CAISSE")
            Using session As New SqlCommand("SELECT COUNT(*) FROM dbo.UtilisateurSessions WHERE SessionId=@s AND UtilisateurId=@u AND RoleIdActif=@r AND ModeActif='FACTURATION_ET_CAISSE' AND Fin IS NULL", cn, tx)
                session.Parameters.AddWithValue("@s", SessionUtilisateur.SessionId)
                session.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId)
                session.Parameters.AddWithValue("@r", SessionUtilisateur.RoleIdActif)
                If CInt(session.ExecuteScalar()) <> 1 Then Throw New UnauthorizedAccessException("Vente & Encaissement n'est pas le mode de cette session.")
            End Using
        End Sub
        Public Function Finaliser(operationId As Guid, facture As FactureVente, lignes As IList(Of LigneFactureVente), motif As String, version As Byte(), client As Client, reglement As ReglementComptoir) As Integer
            If operationId = Guid.Empty Then Throw New ArgumentException("Identifiant de vente obligatoire.")
            If reglement Is Nothing OrElse lignes Is Nothing Then Throw New ArgumentException("Panier et règlement obligatoires.")
            If reglement.Devise <> "FC" AndAlso reglement.Devise <> "USD" Then Throw New ArgumentException("Devise non prise en charge.")
            If Not (New String() {"CASH", "MOBILE_MONEY", "CARTE", "AUTRE"}).Contains(reglement.Moyen) Then Throw New ArgumentException("Moyen de paiement non pris en charge.")
            If reglement.Moyen <> "CASH" AndAlso (Not reglement.ConfirmationExterne OrElse String.IsNullOrWhiteSpace(reglement.Reference)) Then Throw New ArgumentException("Confirmez le paiement externe effectif et sa référence, ou conservez la vente en attente.")
            Dim empreinte As String
            Dim contenu As String = New JavaScriptSerializer().Serialize(New With {
                .Utilisateur = SessionUtilisateur.UtilisateurId, .Role = SessionUtilisateur.RoleIdActif,
                .Facture = New With {facture.FactureVenteId, facture.NumeroFacture, facture.ClientId, facture.SousTotal, facture.MontantRemise, facture.MontantTaxe, facture.MontantTotal},
                .Client = If(client Is Nothing, Nothing, New With {client.NomClient, client.Telephone}),
                .Lignes = lignes.Select(Function(l) New With {l.ProduitId, l.Quantite, l.QteSaisie, l.QuantiteBase, l.TypeVente, l.TypeTarifId, l.PrixUnitaire, l.MontantRemise, l.DemandePrixId, l.ContextePrix, .Motif = If(l.MotifPrixException, "").Trim()}).ToArray(), .Reglement = reglement})
            Using sha As SHA256 = SHA256.Create()
                empreinte = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(contenu))).Replace("-", "")
            End Using
            Dim id As Integer
            Dim commitDemande As Boolean = False
            Dim dejaPayee As Boolean = False
            Dim intentionAbsente As Boolean = False
            Try
                Using cn As SqlConnection = _dal.CreerConnexion()
                    cn.Open()
                    Using tx As SqlTransaction = cn.BeginTransaction()
                        ExigerContexte(cn, tx)
                        ' Une cle d'intention de vente, pas un verrou global de caisse.
                        Using verrou As New SqlCommand("DECLARE @result int; EXEC @result=sys.sp_getapplock @Resource=@cle,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=5000; SELECT @result;", cn, tx)
                            verrou.Parameters.AddWithValue("@cle", "VENTE_COMPTOIR:" & operationId.ToString("D"))
                            If CInt(verrou.ExecuteScalar()) < 0 Then Throw New DBConcurrencyException("Cette vente est en cours de confirmation. Réessayez sans changer de panier.")
                        End Using
                        Using deja As New SqlCommand("SELECT FactureVenteId,OperationComptoirEmpreinte,Statut FROM dbo.FacturesVente WHERE OperationComptoirId=@op", cn, tx)
                            deja.Parameters.AddWithValue("@op", operationId)
                            Using lecteur As SqlDataReader = deja.ExecuteReader()
                                If lecteur.Read() Then
                                    If CStr(lecteur(1)) <> empreinte OrElse CStr(lecteur(2)) <> "PAYEE" Then Throw New DBConcurrencyException("Cette clé de vente correspond à une autre opération. Consultez l'historique.")
                                    id = CInt(lecteur(0))
                                    dejaPayee = True
                                End If
                            End Using
                        End Using
                        If id = 0 Then
                            intentionAbsente = True
                            Dim taux As Decimal = 1D
                            If reglement.Devise = "USD" Then
                                Using cmd As New SqlCommand("SELECT TauxUsd FROM dbo.Parametres WITH(HOLDLOCK)", cn, tx)
                                    Using lecteur As SqlDataReader = cmd.ExecuteReader()
                                        If Not lecteur.Read() Then Throw New InvalidOperationException("Taux de change absent.")
                                        taux = CDec(lecteur(0))
                                        If lecteur.Read() Then Throw New InvalidOperationException("Paramètres entreprise ambigus.")
                                    End Using
                                End Using
                            End If
                            If taux <= 0D OrElse reglement.Taux <> taux Then Throw New DBConcurrencyException("Le taux a changé. Rechargez le règlement.")
                            Dim recu As Decimal = EncaissementAuditRegles.ConvertirRecuFc(reglement.MontantOrigine, reglement.Devise, taux)
                            Dim rendu As Decimal = If(reglement.Moyen = "CASH", Math.Max(0D, recu - facture.MontantTotal), 0D)
                            EncaissementAuditRegles.Verifier(facture.MontantTotal, 0D, recu, rendu)
                            Dim copie As New FactureVente With {.FactureVenteId = facture.FactureVenteId, .NumeroFacture = facture.NumeroFacture, .ClientId = facture.ClientId, .SousTotal = facture.SousTotal, .MontantRemise = facture.MontantRemise, .MontantTaxe = facture.MontantTaxe, .MontantTotal = facture.MontantTotal, .Statut = facture.Statut}
                            Dim copies As New JavaScriptSerializer()
                            Dim articles As List(Of LigneFactureVente) = copies.Deserialize(Of List(Of LigneFactureVente))(copies.Serialize(lignes))
                            id = New FactureOperationService(_dal).EnregistrerDansTransaction(cn, tx, copie, articles, motif, version, client)
                            Using marque As New SqlCommand("UPDATE dbo.FacturesVente SET OperationComptoirId=@op,OperationComptoirEmpreinte=@hash WHERE FactureVenteId=@id", cn, tx)
                                marque.Parameters.AddWithValue("@op", operationId)
                                marque.Parameters.AddWithValue("@hash", empreinte)
                                marque.Parameters.AddWithValue("@id", id)
                                marque.ExecuteNonQuery()
                            End Using
                            Dim caisse As New FacturationService(_dal)
                            caisse.EncaisserDansTransaction(cn, tx, id, reglement.Moyen, reglement.Reference, recu, rendu, reglement.Devise, SessionUtilisateur.UtilisateurId, reglement.MontantOrigine, taux)
                            AppDataVersionService.Touch(cn, tx, "FACTURES", "FINANCE", "PAIEMENTS", "STOCK")
                        End If
                        commitDemande = True
                        tx.Commit()
                    End Using
                End Using
            Catch ex As Exception
                If dejaPayee Then Throw New FactureEnregistreeAffichageException(id, ex)
                If commitDemande Then Throw New ConfirmationComptoirIncertaineException(operationId, ex)
                AuditMetierService.Echec(_dal, "VENTE_COMPTOIR_REFUSEE", facture.FactureVenteId, operationId, ex, reference:=facture.NumeroFacture)
                If intentionAbsente Then Throw New VenteComptoirNonFinaliseeException(ex)
                Throw
            End Try
            Try
                FacturationService.NotifierEncaissement()
            Catch ex As Exception
                Throw New FactureEnregistreeAffichageException(id, ex)
            End Try
            Return id
        End Function

        Public Function ListerAttentes(recherche As String) As DataTable
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "FACTURE_MODIFIER", "FACTURIER")
                    Dim table As New DataTable()
                    Using cmd As New SqlCommand("SELECT TOP(100) f.FactureVenteId,f.NumeroFacture,ISNULL(c.NomClient,'CLIENT') AS Client,ISNULL(c.Telephone,'') AS Telephone,f.MontantTotal,f.CreeLe,u.NomUtilisateur AS PreparePar FROM dbo.FacturesVente f LEFT JOIN dbo.Clients c ON c.ClientId=f.ClientId LEFT JOIN dbo.Utilisateurs u ON u.UtilisateurId=f.CreePar WHERE f.Statut='EN_ATTENTE' AND NOT EXISTS(SELECT 1 FROM dbo.Paiements pa WHERE pa.FactureVenteId=f.FactureVenteId) AND (f.NumeroFacture LIKE @q OR c.NomClient LIKE @q) ORDER BY f.CreeLe DESC,f.FactureVenteId DESC", cn, tx)
                        cmd.Parameters.Add("@q", SqlDbType.NVarChar, 202).Value = "%" & If(recherche, "").Substring(0, Math.Min(200, If(recherche, "").Length)) & "%"
                        Using lecteur As SqlDataReader = cmd.ExecuteReader()
                            table.Load(lecteur)
                        End Using
                    End Using
                    tx.Commit()
                    Return table
                End Using
            End Using
        End Function

        Public Function LireDocument(Optional id As Integer = 0) As DataSet
            If id < 0 Then Throw New ArgumentException("Identifiant de facture invalide.")
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    ExigerContexte(cn, tx, lectureDocument:=True)
                    Dim data As New DataSet()
                    ' Reimpression de SON encaissement confirme, pas une nouvelle
                    ' vente ni un droit general de consulter les factures d'autrui.
                    Using cmd As New SqlCommand("SELECT TOP(1) f.FactureVenteId,f.NumeroFacture,f.CreeLe,f.Statut,f.SousTotal,f.MontantRemise,f.MontantTotal,c.NomClient,c.Telephone,u.NomUtilisateur FROM dbo.FacturesVente f LEFT JOIN dbo.Clients c ON c.ClientId=f.ClientId LEFT JOIN dbo.Utilisateurs u ON u.UtilisateurId=f.CreePar WHERE (@id=0 OR f.FactureVenteId=@id) AND f.Statut='PAYEE' AND EXISTS(SELECT 1 FROM dbo.Paiements pa WHERE pa.FactureVenteId=f.FactureVenteId AND pa.PayePar=@u) AND (f.OperationComptoirId IS NOT NULL OR EXISTS(SELECT 1 FROM dbo.JournalAudit a WHERE a.Entite='Facture' AND a.EntiteId=CONVERT(nvarchar(30),f.FactureVenteId) AND a.[Action]='ENCAISSEMENT_VALIDE' AND a.ModeActif='FACTURATION_ET_CAISSE' AND a.EffectuePar=@u AND a.Resultat='SUCCES')) ORDER BY f.ValideLe DESC,f.FactureVenteId DESC", cn, tx)
                        cmd.Parameters.AddWithValue("@id", id) : cmd.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId)
                        Using r As SqlDataReader = cmd.ExecuteReader()
                            Dim table As New DataTable("Entete")
                            table.Load(r)
                            If table.Rows.Count <> 1 Then Throw New UnauthorizedAccessException("Aucune vente finalisée encaissée par votre compte n'est accessible. Vérifiez le document sélectionné ou consultez l'historique avec un responsable autorisé.")
                            id = CInt(table.Rows(0)("FactureVenteId"))
                            data.Tables.Add(table)
                        End Using
                    End Using
                    Using cmd As New SqlCommand("SELECT p.Libelle,l.TypeVente,ISNULL(l.QuantiteSaisie,l.Quantite) AS Quantite,l.PrixUnitaire,l.MontantLigne FROM dbo.LignesFactureVente l JOIN dbo.Produits p ON p.ProduitId=l.ProduitId WHERE l.FactureVenteId=@id ORDER BY l.LigneFactureVenteId", cn, tx)
                        cmd.Parameters.AddWithValue("@id", id)
                        Using r As SqlDataReader = cmd.ExecuteReader()
                            Dim table As New DataTable("Lignes")
                            table.Load(r)
                            data.Tables.Add(table)
                        End Using
                    End Using
                    tx.Commit()
                    Return data
                End Using
            End Using
        End Function
    End Class
End Namespace
