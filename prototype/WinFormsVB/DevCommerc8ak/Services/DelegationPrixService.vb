Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic
Imports System.Security.Cryptography
Imports System.Text
Imports System.Web.Script.Serialization

Namespace DevCommerc8ak
    Public Class ConfigurationPrix
        Public Property Active As Boolean
        Public Property Demandes As Boolean
        Public Property Immediate As Boolean
        Public Property SousCout As Boolean
        Public Property Variation As Decimal
        Public Property Marge As Decimal
    End Class
    Public Class TarifDelegue
        Public Property ProduitId As Integer
        Public Property Produit As String
        Public Property TypeVente As String
        Public Property TypeId As Integer
        Public Property ChampPrix As String
        Public Property ModePrix As String
        Public Property Coefficient As Decimal
        Public Property Prix As Decimal
        Public Property QuantiteBase As Decimal
        Public Property Cout As Decimal
        Public Property CoutCoefficient As Decimal
        Public Property Empreinte As String
    End Class

    Public Class DelegationPrixService
        Private ReadOnly _dal As DAL
        Private Shared ReadOnly Champs As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
            {"gros", "PrixGros"}, {"demi", "PrixDemi"}, {"quart", "PrixQuart"}, {"piece", "PrixDetail"},
            {"douzaine", "PrixDouzaine"}, {"speciale", "PrixSpecial"}, {"promo", "PrixSpecial"}}
        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Function Autorise(action As String, ecran As String) As Boolean
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        AutorisationActionService.Exiger(cn, tx, action, ecran)
                        tx.Commit()
                        Return True
                    Catch ex As UnauthorizedAccessException
                        tx.Rollback()
                        Return False
                    End Try
                End Using
            End Using
        End Function

        Public Shared Function LireConfiguration(cn As SqlConnection, tx As SqlTransaction) As ConfigurationPrix
            Using cmd As New SqlCommand("SELECT DelegationPrixActive,PrixDemandesActives,PrixModificationImmediate,PrixSousCoutAutorise,PrixVariationMaxPourcent,PrixMargeMinimumPourcent FROM dbo.Parametres WITH(HOLDLOCK)", cn, tx)
                Using r As SqlDataReader = cmd.ExecuteReader()
                    If Not r.Read() Then Throw New InvalidOperationException("Configuration entreprise absente.")
                    Dim p As New ConfigurationPrix With {.Active = CBool(r(0)), .Demandes = CBool(r(1)), .Immediate = CBool(r(2)), .SousCout = CBool(r(3)), .Variation = CDec(r(4)), .Marge = CDec(r(5))}
                    If r.Read() Then Throw New InvalidOperationException("Configuration entreprise ambiguë.")
                    Return p
                End Using
            End Using
        End Function

        Public Function Configuration() As ConfigurationPrix
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "PARAMETRES_PRIX", "PARAMETRES")
                    Dim p As ConfigurationPrix = LireConfiguration(cn, tx)
                    tx.Commit()
                    Return p
                End Using
            End Using
        End Function

        Public Sub Configurer(p As ConfigurationPrix, attendu As ConfigurationPrix, motif As String)
            motif = AuditMetierService.ValiderMotif(motif)
            If p.Variation < 0D OrElse p.Variation > 10000D OrElse p.Marge < 0D OrElse p.Marge > 10000D Then Throw New ArgumentException("Seuils hors limites.")
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "PARAMETRES_PRIX", "PARAMETRES")
                    ' Le verrou de configuration garantit un seul changement,
                    ' sans écraser une modification concurrente d'administrateur.
                    Using cmd As New SqlCommand("SELECT COUNT(*) FROM dbo.Parametres WITH(UPDLOCK,HOLDLOCK)", cn, tx)
                        If CInt(cmd.ExecuteScalar()) <> 1 Then Throw New InvalidOperationException("Configuration entreprise ambiguë.")
                    End Using
                    Dim avant As ConfigurationPrix = LireConfiguration(cn, tx)
                    Dim json As New JavaScriptSerializer()
                    If json.Serialize(avant) <> json.Serialize(attendu) Then Throw New DBConcurrencyException("Configuration changée : rechargez.")
                    Using cmd As New SqlCommand("UPDATE dbo.Parametres SET DelegationPrixActive=@a,PrixDemandesActives=@d,PrixModificationImmediate=@i,PrixSousCoutAutorise=@s,PrixVariationMaxPourcent=@v,PrixMargeMinimumPourcent=@m", cn, tx)
                        cmd.Parameters.AddWithValue("@a", p.Active)
                        cmd.Parameters.AddWithValue("@d", p.Demandes)
                        cmd.Parameters.AddWithValue("@i", p.Immediate)
                        cmd.Parameters.AddWithValue("@s", p.SousCout)
                        cmd.Parameters.AddWithValue("@v", p.Variation)
                        cmd.Parameters.AddWithValue("@m", p.Marge)
                        cmd.ExecuteNonQuery()
                    End Using
                    AuditMetierService.Enregistrer(cn, tx, If(p.Active, "DELEGATION_PRIX_ACTIVEE", "DELEGATION_PRIX_DESACTIVEE"), "AUTORISATIONS", "Parametres", 0, "DELEGATION_PRIX", avant, p, motif, Guid.NewGuid())
                    tx.Commit()
                End Using
            End Using
        End Sub

        Public Function Consulter(produitId As Integer, typeVente As String) As TarifDelegue
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "PRIX_CONSULTER", "FACTURIER")
                    Dim tarif As TarifDelegue = LireTarif(cn, tx, produitId, typeVente)
                    tx.Commit()
                    Return tarif
                End Using
            End Using
        End Function

        Public Shared Function LireTarif(cn As SqlConnection, tx As SqlTransaction, id As Integer, typeVente As String) As TarifDelegue
            Dim produit As New DataTable()
            Using cmd As New SqlCommand("SELECT * FROM dbo.Produits WITH(UPDLOCK,HOLDLOCK) WHERE ProduitId=@id AND EstActif=1", cn, tx)
                cmd.Parameters.AddWithValue("@id", id)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    produit.Load(reader)
                End Using
            End Using
            If produit.Rows.Count <> 1 Then Throw New InvalidOperationException("Produit inactif ou introuvable.")
            Dim row As DataRow = produit.Rows(0)
            Dim conversion As Decimal = If(row.IsNull("ConversionUnite"), 1D, CDec(row("ConversionUnite")))
            Dim principal As Decimal = If(row.IsNull("ContenuUnitePrincipale"), conversion, CDec(row("ContenuUnitePrincipale")))
            Dim secondaire As Decimal = If(row.IsNull("ContenuUniteSecondaire"), 0D, CDec(row("ContenuUniteSecondaire")))
            Dim gestion As String = Convert.ToString(row("TypeGestionStock"))
            Dim achat As Decimal = CDec(row("PrixAchat"))
            Dim t As New TarifDelegue With {.ProduitId = id, .Produit = Convert.ToString(row("Libelle")), .TypeVente = typeVente}
            Dim champ As String = Nothing
            If Champs.TryGetValue(typeVente, champ) Then
                t.ChampPrix = champ
                t.Prix = CDec(row(champ))
                Dim typeConversion As String = If(typeVente = "speciale" OrElse typeVente = "promo", "PIECE", typeVente.ToUpperInvariant())
                Dim qte As Decimal? = StockUnitConversionService.CalculerQuantiteBaseTypeStandard(typeConversion, conversion, gestion, principal, secondaire)
                If Not qte.HasValue Then Throw New InvalidOperationException("Type de vente non convertible.")
                t.QuantiteBase = qte.Value
            Else
                Dim types As New DataTable()
                Using cmd As New SqlCommand("SELECT * FROM dbo.TypesVenteProduit WITH(UPDLOCK,HOLDLOCK) WHERE ProduitId=@p AND Nom=@nom AND Actif=1", cn, tx)
                    cmd.Parameters.AddWithValue("@p", id)
                    cmd.Parameters.AddWithValue("@nom", typeVente)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        types.Load(reader)
                    End Using
                End Using
                If types.Rows.Count <> 1 Then Throw New InvalidOperationException("Type de vente introuvable ou ambigu.")
                Dim tr As DataRow = types.Rows(0)
                t.TypeId = CInt(tr("TypeVenteProduitId"))
                t.ModePrix = Convert.ToString(tr("ModePrix"))
                t.Coefficient = If(tr.IsNull("Coefficient"), 0D, CDec(tr("Coefficient")))
                t.Prix = CDec(tr("PrixVente"))
                If Not tr.IsNull("ProduitConditionnementId") Then
                    Using cmd As New SqlCommand("SELECT FacteurVersBase FROM dbo.ProduitConditionnements WITH(UPDLOCK,HOLDLOCK) WHERE ProduitConditionnementId=@id AND ProduitId=@p AND EstActif=1 AND EstVendable=1", cn, tx)
                        cmd.Parameters.AddWithValue("@id", CInt(tr("ProduitConditionnementId")))
                        cmd.Parameters.AddWithValue("@p", id)
                        Dim facteur As Object = cmd.ExecuteScalar()
                        If facteur Is Nothing Then Throw New InvalidOperationException("Conditionnement non vendable.")
                        Dim dto As New TypeVenteProduitDTO With {.QuantiteEquivalent = CDec(tr("QuantiteEquivalent")), .ProduitConditionnementId = CInt(tr("ProduitConditionnementId"))}
                        t.QuantiteBase = ConversionUniteService.CalculerQuantiteBase(1D, dto, New ProduitConditionnementDTO With {.ProduitConditionnementId = dto.ProduitConditionnementId.Value, .FacteurVersBase = CDec(facteur)})
                    End Using
                Else
                    Dim unite As String = If(tr.IsNull("TypeQuantiteEquivalent"), Convert.ToString(tr("TypeUniteEquivalent")), Convert.ToString(tr("TypeQuantiteEquivalent")))
                    t.QuantiteBase = CalculVenteService.CalculerQuantiteBaseTypeVente(CDec(tr("QuantiteEquivalent")), unite, conversion, If(principal > 0D, principal, conversion), secondaire)
                End If
                t.CoutCoefficient = CalculVenteService.CalculerCoutEquivalentCoefficient(achat, t.QuantiteBase, If(principal > 0D, principal, If(conversion > 0D, conversion, 1D)))
                If t.ModePrix = "COEFFICIENT" AndAlso t.Coefficient > 0D Then t.Prix = Math.Round(t.CoutCoefficient * t.Coefficient, 2)
            End If
            Dim coutBase As Decimal? = CalculVenteService.CalculerCoutUnitaireBase(achat, conversion, gestion, principal)
            If Not coutBase.HasValue OrElse coutBase.Value <= 0D OrElse t.QuantiteBase <= 0D Then Throw New InvalidOperationException("Coût comparable indisponible : délégation refusée.")
            t.Cout = coutBase.Value * t.QuantiteBase
            ' L'empreinte couvre le prix, son coût et son équivalent physique.
            ' Une approbation ne s'applique pas sur une autre configuration.
            Using sha As SHA256 = SHA256.Create()
                t.Empreinte = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(New JavaScriptSerializer().Serialize(t)))).Replace("-", "")
            End Using
            Return t
        End Function

        Private Shared Sub VerifierSousCout(cn As SqlConnection, tx As SqlTransaction, p As ConfigurationPrix, t As TarifDelegue, prix As Decimal, ecran As String)
            If prix >= t.Cout Then Return
            If Not p.SousCout Then Throw New UnauthorizedAccessException("La vente sous coût est interdite par l'entreprise.")
            AutorisationActionService.Exiger(cn, tx, "PRIX_SOUS_COUT", ecran)
        End Sub

        Public Function Modifier(t As TarifDelegue, nouveau As Decimal, motif As String, demande As Boolean) As String
            Try
                Return ModifierInterne(t, nouveau, motif, demande)
            Catch ex As Exception
                AuditMetierService.Echec(_dal, "PRIX_MODIFICATION_ECHEC", t.ProduitId, Guid.NewGuid(), ex, "Produit", t.Produit & " / " & t.TypeVente)
                Throw
            End Try
        End Function

        Private Function ModifierInterne(t As TarifDelegue, nouveau As Decimal, motif As String, demande As Boolean) As String
            motif = AuditMetierService.ValiderMotif(motif)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, If(demande, "PRIX_DEMANDER", "PRIX_TARIF_MODIFIER"), "FACTURIER")
                    Dim p As ConfigurationPrix = LireConfiguration(cn, tx)
                    If Not p.Active Then Throw New UnauthorizedAccessException("Délégation des prix désactivée.")
                    Dim actuel As TarifDelegue = LireTarif(cn, tx, t.ProduitId, t.TypeVente)
                    If actuel.Empreinte <> t.Empreinte Then Throw New DBConcurrencyException("Le tarif ou sa configuration a changé. Rechargez.")
                    Dim approbation As Boolean = DelegationPrixRegles.NecessiteApprobation(actuel.Prix, nouveau, actuel.Cout, p.Variation, p.Marge)
                    VerifierSousCout(cn, tx, p, actuel, nouveau, "FACTURIER")
                    If demande Then
                        If Not p.Demandes Then Throw New UnauthorizedAccessException("Les demandes sont désactivées.")
                        Using cmd As New SqlCommand("INSERT dbo.DemandesModificationPrix(ProduitId,TypeVente,AncienPrix,NouveauPrix,EmpreinteTarif,Motif,DemandePar) VALUES(@p,@type,@a,@n,@hash,@motif,@u); SELECT CAST(SCOPE_IDENTITY() AS int)", cn, tx)
                            cmd.Parameters.AddWithValue("@p", actuel.ProduitId)
                            cmd.Parameters.AddWithValue("@type", actuel.TypeVente)
                            cmd.Parameters.AddWithValue("@a", actuel.Prix)
                            cmd.Parameters.AddWithValue("@n", nouveau)
                            cmd.Parameters.AddWithValue("@hash", actuel.Empreinte)
                            cmd.Parameters.AddWithValue("@motif", motif)
                            cmd.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId)
                            Dim id As Integer = CInt(cmd.ExecuteScalar())
                            AuditMetierService.Enregistrer(cn, tx, "PRIX_MODIFICATION_DEMANDEE", "PRIX", "DemandePrix", id, actuel.Produit & " / " & actuel.TypeVente, actuel, New With {.Prix = nouveau, .Etat = "EN_ATTENTE"}, motif, Guid.NewGuid())
                        End Using
                    Else
                        If Not p.Immediate OrElse approbation Then Throw New UnauthorizedAccessException("Approbation requise : soumettez une demande, l'ancien tarif reste actif.")
                        Appliquer(cn, tx, actuel, nouveau, motif)
                    End If
                    tx.Commit()
                    Return If(demande, "Demande enregistrée. Le tarif actif est inchangé.", "Tarif officiel modifié. Les lignes déjà saisies conservent leur prix.")
                End Using
            End Using
        End Function

        Private Shared Sub Appliquer(cn As SqlConnection, tx As SqlTransaction, t As TarifDelegue, nouveau As Decimal, motif As String)
            Dim sql As String
            Dim coefficient As Decimal = t.Coefficient
            If t.TypeId = 0 Then
                ' Champ issu d'une liste blanche interne, jamais d'une saisie SQL.
                Dim colonnes As String = "ProduitId,AncienPrixAchat,NouveauPrixAchat"
                Dim valeurs As String = "ProduitId,PrixAchat,PrixAchat"
                For Each champ As String In New String() {"PrixDetail", "PrixDemi", "PrixQuart", "PrixDouzaine", "PrixGros", "PrixSpecial"}
                    colonnes &= ",Ancien" & champ & ",Nouveau" & champ
                    valeurs &= "," & champ & "," & If(champ = t.ChampPrix, "@prix", champ)
                Next
                Using historique As New SqlCommand("INSERT dbo.HistoriquePrixProduits(" & colonnes & ",ModifiePar,ModifieLe,IdStock) SELECT " & valeurs & ",@user,GETDATE(),NULL FROM dbo.Produits WHERE ProduitId=@id", cn, tx)
                    historique.Parameters.AddWithValue("@prix", nouveau)
                    historique.Parameters.AddWithValue("@user", SessionUtilisateur.UtilisateurId)
                    historique.Parameters.AddWithValue("@id", t.ProduitId)
                    historique.ExecuteNonQuery()
                End Using
                If t.ChampPrix = "PrixGros" Then coefficient = Math.Round(nouveau / t.Cout, 4)
                sql = "UPDATE dbo.Produits SET " & t.ChampPrix & "=@prix" & If(t.ChampPrix = "PrixGros", ",CoefficientGros=@coefficient", "") & ",ModifieLe=GETDATE(),ModifierPar=@user WHERE ProduitId=@id"
            Else
                If t.ModePrix = "COEFFICIENT" Then
                    coefficient = Math.Round(nouveau / t.CoutCoefficient, 4)
                    If Math.Round(t.CoutCoefficient * coefficient, 2) <> nouveau Then Throw New ArgumentException("Ce prix ne peut pas être représenté exactement par le coefficient existant. Choisissez un prix compatible ou faites configurer un tarif fixe.")
                End If
                sql = "UPDATE dbo.TypesVenteProduit SET PrixVente=@prix,Coefficient=@coefficient,ModifieLe=GETDATE(),ModifiePar=@user WHERE TypeVenteProduitId=@id"
            End If
            Using cmd As New SqlCommand(sql, cn, tx)
                cmd.Parameters.AddWithValue("@prix", nouveau)
                cmd.Parameters.AddWithValue("@coefficient", coefficient)
                cmd.Parameters.AddWithValue("@user", SessionUtilisateur.NomUtilisateur)
                cmd.Parameters.AddWithValue("@id", If(t.TypeId = 0, t.ProduitId, t.TypeId))
                If CInt(cmd.ExecuteNonQuery()) <> 1 Then Throw New DBConcurrencyException("Tarif introuvable.")
            End Using
            AuditMetierService.Enregistrer(cn, tx, "PRIX_MODIFICATION_APPLIQUEE", "PRIX", "Produit", t.ProduitId, t.Produit & " / " & t.TypeVente, t, New With {.Prix = nouveau, .Coefficient = coefficient}, motif, Guid.NewGuid())
        End Sub

        Public Function ListerDemandes(Optional ecran As String = "PARAMETRES") As DataTable
            If ecran <> "PARAMETRES" AndAlso ecran <> "FACTURIER" Then Throw New ArgumentException("Contexte de consultation inconnu.")
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "PRIX_CONSULTER", ecran)
                    Dim expires As New List(Of Integer)()
                    Using cmd As New SqlCommand("SELECT TOP(500) DemandeId FROM dbo.DemandesModificationPrix WITH(UPDLOCK) WHERE Etat='EN_ATTENTE' AND ExpireLe<=SYSUTCDATETIME()", cn, tx)
                        Using r As SqlDataReader = cmd.ExecuteReader()
                            While r.Read()
                                expires.Add(CInt(r(0)))
                            End While
                        End Using
                    End Using
                    ' L'expiration ne change aucun tarif. Son etat et sa trace
                    ' sont valides ensemble, y compris lors d'une consultation.
                    For Each expire As Integer In expires
                        Using cmd As New SqlCommand("UPDATE dbo.DemandesModificationPrix SET Etat='EXPIREE',DecisionLe=SYSUTCDATETIME() WHERE DemandeId=@id AND Etat='EN_ATTENTE'", cn, tx)
                            cmd.Parameters.AddWithValue("@id", expire)
                            If cmd.ExecuteNonQuery() <> 1 Then Throw New DBConcurrencyException("Expiration concurrente.")
                        End Using
                        AuditMetierService.Enregistrer(cn, tx, "PRIX_MODIFICATION_EXPIREE", "PRIX", "DemandePrix", expire, expire.ToString(), New With {.Etat = "EN_ATTENTE"}, New With {.Etat = "EXPIREE"}, "Validité de 24 heures échue", Guid.NewGuid())
                    Next
                    Dim dt As New DataTable()
                    Using cmd As New SqlCommand("SELECT TOP(500) d.DemandeId,p.Libelle AS Produit,d.TypeVente,d.AncienPrix,d.NouveauPrix,d.Etat,d.Motif,d.DemandeLe,d.ExpireLe,u.NomUtilisateur AS Demandeur FROM dbo.DemandesModificationPrix d JOIN dbo.Produits p ON p.ProduitId=d.ProduitId JOIN dbo.Utilisateurs u ON u.UtilisateurId=d.DemandePar WHERE (@mes=0 OR d.DemandePar=@user) ORDER BY d.DemandeId DESC", cn, tx)
                        cmd.Parameters.AddWithValue("@mes", ecran = "FACTURIER")
                        cmd.Parameters.AddWithValue("@user", SessionUtilisateur.UtilisateurId)
                        Using r As SqlDataReader = cmd.ExecuteReader()
                            dt.Load(r)
                        End Using
                    End Using
                    tx.Commit()
                    Return dt
                End Using
            End Using
        End Function

        Public Sub AnnulerDemande(id As Integer, motif As String)
            motif = AuditMetierService.ValiderMotif(motif)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "PRIX_DEMANDER", "FACTURIER")
                    Using cmd As New SqlCommand("UPDATE dbo.DemandesModificationPrix SET Etat='ANNULEE',DecisionPar=@user,DecisionLe=SYSUTCDATETIME(),MotifDecision=@motif WHERE DemandeId=@id AND DemandePar=@user AND Etat='EN_ATTENTE' AND ExpireLe>SYSUTCDATETIME()", cn, tx)
                        cmd.Parameters.AddWithValue("@user", SessionUtilisateur.UtilisateurId)
                        cmd.Parameters.AddWithValue("@motif", motif)
                        cmd.Parameters.AddWithValue("@id", id)
                        If cmd.ExecuteNonQuery() <> 1 Then Throw New UnauthorizedAccessException("Seule votre demande encore en attente peut être annulée.")
                    End Using
                    AuditMetierService.Enregistrer(cn, tx, "PRIX_MODIFICATION_ANNULEE", "PRIX", "DemandePrix", id, id.ToString(), New With {.Etat = "EN_ATTENTE"}, New With {.Etat = "ANNULEE"}, motif, Guid.NewGuid())
                    tx.Commit()
                End Using
            End Using
        End Sub

        Public Sub Decider(id As Integer, approuver As Boolean, motif As String)
            Try
                DeciderInterne(id, approuver, motif)
            Catch ex As Exception
                AuditMetierService.Echec(_dal, "PRIX_DECISION_ECHEC", id, Guid.NewGuid(), ex, "DemandePrix")
                Throw
            End Try
        End Sub

        Private Sub DeciderInterne(id As Integer, approuver As Boolean, motif As String)
            motif = AuditMetierService.ValiderMotif(motif)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, If(approuver, "PRIX_APPROUVER", "PRIX_REFUSER"), "PARAMETRES")
                    Dim dt As New DataTable()
                    Using cmd As New SqlCommand("SELECT * FROM dbo.DemandesModificationPrix WITH(UPDLOCK,HOLDLOCK) WHERE DemandeId=@id AND Etat='EN_ATTENTE'", cn, tx)
                        cmd.Parameters.AddWithValue("@id", id)
                        Using r As SqlDataReader = cmd.ExecuteReader()
                            dt.Load(r)
                        End Using
                    End Using
                    If dt.Rows.Count <> 1 Then Throw New DBConcurrencyException("Demande déjà traitée ou introuvable.")
                    Dim row As DataRow = dt.Rows(0)
                    Dim expiree As Boolean = False
                    Using cmd As New SqlCommand("SELECT CASE WHEN @expire<=SYSUTCDATETIME() THEN 1 ELSE 0 END", cn, tx)
                        cmd.Parameters.AddWithValue("@expire", row("ExpireLe"))
                        expiree = CInt(cmd.ExecuteScalar()) = 1
                    End Using
                    If approuver AndAlso Not expiree Then
                        If CInt(row("DemandePar")) = SessionUtilisateur.UtilisateurId Then Throw New UnauthorizedAccessException("Une demande ne peut pas être approuvée par son auteur.")
                        Dim p As ConfigurationPrix = LireConfiguration(cn, tx)
                        If Not p.Active OrElse Not p.Demandes Then Throw New UnauthorizedAccessException("Délégation ou demandes désactivées.")
                        Dim actuel As TarifDelegue = LireTarif(cn, tx, CInt(row("ProduitId")), Convert.ToString(row("TypeVente")))
                        If actuel.Empreinte <> Convert.ToString(row("EmpreinteTarif")) Then Throw New DBConcurrencyException("Tarif ou coût modifié depuis la demande : nouvelle demande nécessaire.")
                        Dim nouveau As Decimal = CDec(row("NouveauPrix"))
                        DelegationPrixRegles.VerifierPrix(nouveau)
                        VerifierSousCout(cn, tx, p, actuel, nouveau, "PARAMETRES")
                        Appliquer(cn, tx, actuel, nouveau, motif)
                    End If
                    Dim etat As String = If(expiree, "EXPIREE", If(approuver, "APPROUVEE", "REFUSEE"))
                    Using cmd As New SqlCommand("UPDATE dbo.DemandesModificationPrix SET Etat=@etat,DecisionPar=@u,DecisionLe=SYSUTCDATETIME(),MotifDecision=@motif WHERE DemandeId=@id AND Etat='EN_ATTENTE'", cn, tx)
                        cmd.Parameters.AddWithValue("@etat", etat)
                        cmd.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId)
                        cmd.Parameters.AddWithValue("@motif", motif)
                        cmd.Parameters.AddWithValue("@id", id)
                        If cmd.ExecuteNonQuery() <> 1 Then Throw New DBConcurrencyException("Décision concurrente.")
                    End Using
                    AuditMetierService.Enregistrer(cn, tx, "PRIX_MODIFICATION_" & If(expiree, "EXPIREE", If(approuver, "APPROUVEE", "REFUSEE")), "PRIX", "DemandePrix", id, id.ToString(), New With {.Etat = "EN_ATTENTE"}, New With {.Etat = etat}, motif, Guid.NewGuid())
                    tx.Commit()
                End Using
            End Using
        End Sub

        Public Shared Sub VerifierException(cn As SqlConnection, tx As SqlTransaction, ligne As LigneFactureVente)
            Dim p As ConfigurationPrix = LireConfiguration(cn, tx)
            If Not p.Active Then Throw New UnauthorizedAccessException("Délégation des prix désactivée.")
            AutorisationActionService.Exiger(cn, tx, "PRIX_FACTURE_EXCEPTION", "FACTURIER")
            ligne.MotifPrixException = AuditMetierService.ValiderMotif(ligne.MotifPrixException)
            Dim t As TarifDelegue = LireTarif(cn, tx, ligne.ProduitId, ligne.TypeVente)
            If DelegationPrixRegles.NecessiteApprobation(t.Prix, ligne.PrixUnitaire, t.Cout, p.Variation, p.Marge) Then Throw New UnauthorizedAccessException("Exception hors seuil : faites approuver un tarif officiel avant la vente.")
            VerifierSousCout(cn, tx, p, t, ligne.PrixUnitaire, "FACTURIER")
        End Sub

        Public Sub VerifierExceptionProposee(t As TarifDelegue, prix As Decimal, motif As String)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Dim actuel As TarifDelegue = LireTarif(cn, tx, t.ProduitId, t.TypeVente)
                    If actuel.Empreinte <> t.Empreinte Then Throw New DBConcurrencyException("Le tarif a changé. Rechargez.")
                    VerifierException(cn, tx, New LigneFactureVente With {.ProduitId = t.ProduitId, .TypeVente = t.TypeVente, .PrixUnitaire = prix, .MotifPrixException = motif})
                    tx.Commit()
                End Using
            End Using
        End Sub

        Public Shared Sub ValiderLigne(cn As SqlConnection, tx As SqlTransaction, ligne As LigneFactureVente, avant As Object, factureId As Integer, reference As String)
            ' Les prix deja valides dans ce brouillon ne sont pas rafraichis
            ' silencieusement. Une nouvelle exception passe par ses propres droits.
            Dim ancien As Dictionary(Of String, Object) = TryCast(avant, Dictionary(Of String, Object))
            If ancien IsNot Nothing Then
                Dim articles As List(Of Dictionary(Of String, Object)) = DirectCast(ancien("Lignes"), List(Of Dictionary(Of String, Object)))
                For Each article As Dictionary(Of String, Object) In articles
                    If CInt(article("ProduitId")) = ligne.ProduitId AndAlso String.Equals(Convert.ToString(article("TypeVente")), ligne.TypeVente, StringComparison.OrdinalIgnoreCase) AndAlso CDec(article("PrixUnitaire")) = ligne.PrixUnitaire Then
                        ligne.MotifPrixException = Convert.ToString(article("MotifPrixException"))
                        Return
                    End If
                Next
            End If
            Dim p As ConfigurationPrix = LireConfiguration(cn, tx)
            If Not p.Active AndAlso String.IsNullOrWhiteSpace(ligne.MotifPrixException) Then Return
            Dim tarif As TarifDelegue = LireTarif(cn, tx, ligne.ProduitId, ligne.TypeVente)
            If tarif.Prix = ligne.PrixUnitaire AndAlso String.IsNullOrWhiteSpace(ligne.MotifPrixException) Then Return
            VerifierException(cn, tx, ligne)
            AuditMetierService.Enregistrer(cn, tx, "FACTURE_PRIX_MODIFIE", "PRIX", "Facture", factureId, reference,
                tarif, New With {.ProduitId = ligne.ProduitId, .TypeVente = ligne.TypeVente, .Prix = ligne.PrixUnitaire}, ligne.MotifPrixException, Guid.NewGuid())
        End Sub
    End Class
End Namespace
