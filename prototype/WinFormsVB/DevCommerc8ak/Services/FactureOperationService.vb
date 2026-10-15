Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic
Imports System.Linq

Namespace DevCommerc8ak
    Public Class FactureEnregistreeAffichageException
        Inherits Exception
        Public ReadOnly Property FactureId As Integer
        Public Sub New(id As Integer, cause As Exception)
            MyBase.New("L'opération est enregistrée (facture " & id.ToString() & "), mais son rafraîchissement a échoué. Consultez l'historique avant toute nouvelle vente.", cause)
            FactureId = id
        End Sub
    End Class
    Public Class FactureOperationService
        Private ReadOnly _dal As DAL
        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Function LireVersion(id As Integer) As Byte()
            Dim table As DataTable = _dal.ExecuterTable("SELECT VersionOperation FROM dbo.FacturesVente WHERE FactureVenteId=@id", CommandType.Text,
                New List(Of SqlParameter) From {New SqlParameter("@id", id)})
            If table.Rows.Count <> 1 Then Throw New InvalidOperationException("Facture introuvable.")
            Return DirectCast(table.Rows(0)("VersionOperation"), Byte())
        End Function

        Public Function LireEntetePourEdition(id As Integer) As DataRow
            ' Version, client et remise proviennent du même chargement SQL,
            ' jamais d'une ancienne ligne affichée dans l'historique.
            Dim table As DataTable = _dal.ExecuterTable("SELECT f.NumeroFacture,f.ClientId,f.Statut,f.VersionOperation,f.SousTotal,f.MontantRemise,f.MontantTaxe,CASE WHEN EXISTS(SELECT 1 FROM dbo.LignesFactureVente l WHERE l.FactureVenteId=f.FactureVenteId AND l.MontantRemise<>0) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS ARemiseParLigne,ISNULL(c.NomClient,'') AS NomClient,ISNULL(c.Telephone,'') AS Telephone FROM dbo.FacturesVente f LEFT JOIN dbo.Clients c ON c.ClientId=f.ClientId WHERE f.FactureVenteId=@id", CommandType.Text,
                New List(Of SqlParameter) From {New SqlParameter("@id", id)})
            If table.Rows.Count <> 1 Then Throw New InvalidOperationException("Facture introuvable.")
            AuditMetierRegles.VerifierEditionCompatible(Convert.ToDecimal(table.Rows(0)("MontantTaxe")), Convert.ToBoolean(table.Rows(0)("ARemiseParLigne")))
            Return table.Rows(0)
        End Function

        Public Function Enregistrer(facture As FactureVente, lignes As IList(Of LigneFactureVente), motif As String, versionAttendue As Byte(), Optional nouveauClient As Client = Nothing, Optional confirmerPreparation As Boolean = True) As Integer
            Dim id As Integer
            Try
                Using cn As SqlConnection = _dal.CreerConnexion()
                    cn.Open()
                    Using tx As SqlTransaction = cn.BeginTransaction()
                        id = EnregistrerDansTransaction(cn, tx, facture, lignes, motif, versionAttendue, nouveauClient, confirmerPreparation)
                        tx.Commit()
                    End Using
                End Using
            Catch ex As Exception
                AuditMetierService.Echec(_dal, "FACTURE_ENREGISTREMENT_REFUSE", facture.FactureVenteId, Guid.NewGuid(), ex, reference:=facture.NumeroFacture)
                Throw
            End Try
            Try
                AppEvents.OnVenteCreee()
                AppEvents.OnDataChanged()
            Catch ex As Exception
                Throw New FactureEnregistreeAffichageException(id, ex)
            End Try
            Return id
        End Function

        Friend Function EnregistrerDansTransaction(cn As SqlConnection, tx As SqlTransaction, facture As FactureVente, lignes As IList(Of LigneFactureVente), motif As String, versionAttendue As Byte(), Optional nouveauClient As Client = Nothing, Optional confirmerPreparation As Boolean = True) As Integer
            If tx Is Nothing OrElse tx.Connection IsNot cn Then Throw New ArgumentException("Transaction de facture obligatoire.")
            Dim correlation As Guid = Guid.NewGuid()
            Dim id As Integer = facture.FactureVenteId
            ' Les repositories conservent leurs colonnes/calculs. Seule la
            ' transaction englobe désormais en-tête, articles et trace métier.
            Dim repo As New FactureVenteRepository(_dal)
            Dim ligneRepo As New LigneFactureVenteRepository(_dal)
            AutorisationActionService.Exiger(cn, tx, If(id > 0, "FACTURE_MODIFIER", "FACTURE_CREER"), "FACTURIER")
            If lignes Is Nothing OrElse lignes.Count = 0 Then Throw New ArgumentException("La facture doit contenir au moins un article.")
            If lignes.Any(Function(l) If(l.QteSaisie, l.Quantite) <= 0D OrElse l.QuantiteBase <= 0D OrElse l.PrixUnitaire < 0D OrElse l.MontantRemise < 0D) Then Throw New ArgumentException("Quantités positives et montants non négatifs requis.")
            If facture.Statut <> "EN_ATTENTE" Then Throw New InvalidOperationException("Cette opération enregistre uniquement un brouillon.")
            Dim avant As Object = Nothing
            If id > 0 Then
                motif = AuditMetierService.ValiderMotif(motif)
                VerifierBrouillon(cn, tx, id, versionAttendue)
                avant = Snapshot(cn, tx, id)
            End If
            If nouveauClient IsNot Nothing Then
                ' Le client créé avec la facture appartient à la même
                ' transaction : ni refus ni annulation ne laisse un client orphelin.
                facture.ClientId = New ClientRepository(_dal).Ajouter(nouveauClient, cn, tx)
            End If
            If id > 0 Then
                repo.MettreAJour(facture, cn, tx)
                Using cmd As New SqlCommand("DELETE dbo.LignesFactureVente WHERE FactureVenteId=@id", cn, tx)
                    cmd.Parameters.AddWithValue("@id", id)
                    cmd.ExecuteNonQuery()
                End Using
            Else
                facture.CreePar = SessionUtilisateur.UtilisateurId
                facture.Statut = "EN_ATTENTE"
                id = repo.Ajouter(facture, cn, tx)
            End If
            For Each ligne As LigneFactureVente In lignes
                ligne.FactureVenteId = id
                DelegationPrixService.ValiderLigne(cn, tx, ligne, avant, id, facture.NumeroFacture, New ContexteCommercialPrix With {.SousTotal = facture.SousTotal, .Remise = facture.MontantRemise})
                ' Le calcul existant est conservé ; aucune quantité
                ' historique n'est recalculée lors d'une consultation.
                ligne.MontantLigne = If(ligne.QteSaisie.HasValue, ligne.QteSaisie.Value, ligne.Quantite) * ligne.PrixUnitaire - ligne.MontantRemise
                ligne.CoutUnitaireBaseVente = New FacturationService(_dal).ObtenirCoutUnitaireBaseVente(ligne.ProduitId, cn, tx)
                ligneRepo.Ajouter(ligne, cn, tx)
            Next
            If lignes.Where(Function(l) l.DemandePrixId.HasValue).GroupBy(Function(l) l.DemandePrixId.Value).Any(Function(g) g.Count() > 1) Then Throw New UnauthorizedAccessException("Une approbation ne peut pas autoriser plusieurs articles.")
            DelegationPrixRegles.VerifierTotauxException(lignes.Sum(Function(l) l.MontantLigne), facture.SousTotal, facture.MontantRemise, facture.MontantTaxe, facture.MontantTotal, lignes.Sum(Function(l) Decimal.Round(l.MontantLigne, 2, MidpointRounding.AwayFromZero)))
            Using cmd As New SqlCommand("UPDATE dbo.FacturesVente SET ValideeParUtilisateurId=@u WHERE FactureVenteId=@id", cn, tx)
                cmd.Parameters.AddWithValue("@u", If(confirmerPreparation, CType(SessionUtilisateur.UtilisateurId, Object), DBNull.Value))
                cmd.Parameters.AddWithValue("@id", id)
                cmd.ExecuteNonQuery()
            End Using
            Dim apres As Dictionary(Of String, Object) = Snapshot(cn, tx, id)
            AuditMetierService.Enregistrer(cn, tx, If(avant Is Nothing, "FACTURE_CREEE", "FACTURE_MODIFIEE"), "FACTURATION", "Facture", id,
                Convert.ToString(DirectCast(apres("Entete"), Dictionary(Of String, Object))("NumeroFacture")), avant, apres, motif, correlation)
            Return id
        End Function

        Public Sub Annuler(id As Integer, motif As String, Optional depuisCaisse As Boolean = False)
            Dim correlation As Guid = Guid.NewGuid()
            Try
                Using cn As SqlConnection = _dal.CreerConnexion()
                    cn.Open()
                    Using tx As SqlTransaction = cn.BeginTransaction()
                        Try
                            AutorisationActionService.Exiger(cn, tx, "FACTURE_ANNULER", If(depuisCaisse, "CAISSE", "HISTORIQUE_FACTURES"))
                            motif = AuditMetierService.ValiderMotif(motif)
                            VerifierBrouillon(cn, tx, id, Nothing, False)
                            Dim avant As Dictionary(Of String, Object) = Snapshot(cn, tx, id)
                            Using cmd As New SqlCommand("UPDATE dbo.FacturesVente SET Statut='ANNULEE', ModifierPar=@user WHERE FactureVenteId=@id AND Statut='EN_ATTENTE'", cn, tx)
                                cmd.Parameters.AddWithValue("@id", id)
                                cmd.Parameters.AddWithValue("@user", SessionUtilisateur.NomUtilisateur)
                                If cmd.ExecuteNonQuery() <> 1 Then Throw New DBConcurrencyException("La facture a changé.")
                            End Using
                            AuditMetierService.Enregistrer(cn, tx, "FACTURE_ANNULEE", "FACTURATION", "Facture", id,
                                Convert.ToString(DirectCast(avant("Entete"), Dictionary(Of String, Object))("NumeroFacture")), avant, Snapshot(cn, tx, id), motif, correlation)
                            tx.Commit()
                        Catch
                            If tx.Connection IsNot Nothing Then tx.Rollback()
                            Throw
                        End Try
                    End Using
                End Using
            Catch ex As Exception
                AuditMetierService.Echec(_dal, "FACTURE_ANNULATION_REFUSEE", id, correlation, ex)
                Throw
            End Try
            AppEvents.OnDataChanged()
        End Sub

        Private Shared Sub VerifierBrouillon(cn As SqlConnection, tx As SqlTransaction, id As Integer, version As Byte(), Optional verifierVersion As Boolean = True)
            Using cmd As New SqlCommand("SELECT Statut, VersionOperation FROM dbo.FacturesVente WITH (UPDLOCK,HOLDLOCK) WHERE FactureVenteId=@id", cn, tx)
                cmd.Parameters.AddWithValue("@id", id)
                Using r As SqlDataReader = cmd.ExecuteReader()
                    If Not r.Read() Then Throw New InvalidOperationException("Facture introuvable.")
                    If Convert.ToString(r("Statut")) <> "EN_ATTENTE" Then Throw New InvalidOperationException("Seuls les brouillons peuvent être modifiés ou annulés.")
                    If verifierVersion Then AuditMetierRegles.VerifierVersion(version, DirectCast(r("VersionOperation"), Byte()))
                End Using
            End Using
        End Sub

        Public Shared Function Snapshot(cn As SqlConnection, tx As SqlTransaction, id As Integer) As Dictionary(Of String, Object)
            Dim result As New Dictionary(Of String, Object)()
            Dim entete As List(Of Dictionary(Of String, Object)) = Lire(cn, tx,
                "SELECT f.NumeroFacture,f.ClientId,c.NomClient,c.Telephone,f.SousTotal,f.MontantRemise,f.MontantTaxe,f.MontantTotal,f.Statut,f.CreePar AS FactureCreePar,u.NomUtilisateur AS FactureCreeParNom,f.ValideeParUtilisateurId AS FactureValideePar,v.NomUtilisateur AS FactureValideeParNom FROM dbo.FacturesVente f LEFT JOIN dbo.Clients c ON c.ClientId=f.ClientId LEFT JOIN dbo.Utilisateurs u ON u.UtilisateurId=f.CreePar LEFT JOIN dbo.Utilisateurs v ON v.UtilisateurId=f.ValideeParUtilisateurId WHERE f.FactureVenteId=@id", id)
            If entete.Count <> 1 Then Throw New InvalidOperationException("Facture introuvable.")
            result.Add("Entete", entete(0))
            ' Les ID techniques recréés ne sont pas une différence commerciale.
            ' Les articles et leur ordre sont conservés pour comparer les vraies valeurs.
            result.Add("Lignes", Lire(cn, tx, "SELECT l.ProduitId,p.Libelle AS Produit,l.TypeVente,l.Quantite,l.QuantiteSaisie,l.QuantiteBase,l.PrixUnitaire,l.MontantRemise,l.MontantLigne,l.CoutUnitaireBaseVente,l.MotifPrixException,l.DemandePrixId,l.ContextePrix,l.TypeTarifId FROM dbo.LignesFactureVente l LEFT JOIN dbo.Produits p ON p.ProduitId=l.ProduitId WHERE l.FactureVenteId=@id ORDER BY l.LigneFactureVenteId", id))
            Return result
        End Function

        Private Shared Function Lire(cn As SqlConnection, tx As SqlTransaction, sql As String, id As Integer) As List(Of Dictionary(Of String, Object))
            Dim result As New List(Of Dictionary(Of String, Object))()
            Using cmd As New SqlCommand(sql, cn, tx)
                cmd.Parameters.AddWithValue("@id", id)
                Using r As SqlDataReader = cmd.ExecuteReader()
                    While r.Read()
                        Dim row As New Dictionary(Of String, Object)()
                        For i As Integer = 0 To r.FieldCount - 1
                            row.Add(r.GetName(i), If(r.IsDBNull(i), Nothing, r.GetValue(i)))
                        Next
                        result.Add(row)
                    End While
                End Using
            End Using
            Return result
        End Function
    End Class
End Namespace
