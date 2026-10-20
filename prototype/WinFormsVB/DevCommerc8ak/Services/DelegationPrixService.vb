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
    Public Class DiagnosticActionsPrix
        Public ReadOnly Property Actions As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Public Property Explication As String
        Public Property ExplicationTarif As String
    End Class
    Public Class ApprobationPrixArticle
        Public Property Ligne As LigneFactureVente
        Public Property Contexte As ContexteCommercialPrix
    End Class
    Public Class PageDemandesPrix
        Public Property Lignes As DataTable
        Public Property Total As Integer
        Public Property EnAttente As Integer
        Public Property Approuvees As Integer
        Public Property Refusees As Integer
    End Class
    Public Class ConfigurationPrix
        Public Property Active As Boolean
        Public Property Demandes As Boolean
        Public Property Immediate As Boolean
        Public Property SousCout As Boolean
        Public Property Variation As Decimal
        Public Property Marge As Decimal
        Public Property DureeHeures As Integer = 24
    End Class
    Public Class TarifDelegue
        Public Property ProduitId As Integer
        Public Property Produit As String
        <ScriptIgnore>
        Public Property CodeBarres As String
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
        <ScriptIgnore>
        Public Property PrixAchatReference As Decimal
        <ScriptIgnore>
        Public Property UnitePrincipale As String
        <ScriptIgnore>
        Public Property TypeActif As Boolean
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

        Public Function ActionsDisponibles() As HashSet(Of String)
            Return DiagnostiquerActions().Actions
        End Function

        Public Function DiagnostiquerActions() As DiagnosticActionsPrix
            Dim diagnostic As New DiagnosticActionsPrix With {.Explication = "Délégation des prix désactivée dans les paramètres."}
            Dim actions As HashSet(Of String) = diagnostic.Actions
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Dim p As ConfigurationPrix = LireConfiguration(cn, tx)
                    If p.Active Then
                        Try
                            AutorisationActionService.Exiger(cn, tx, "PRIX_CONSULTER", "FACTURIER")
                            Dim droits As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                            Using cmd As New SqlCommand("SELECT i.CodeInterface FROM dbo.RoleInterfaces ri WITH(HOLDLOCK) JOIN dbo.InterfacesApplication i WITH(HOLDLOCK) ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=@role AND i.EstActif=1", cn, tx)
                                cmd.Parameters.AddWithValue("@role", SessionUtilisateur.RoleIdActif)
                                Using r As SqlDataReader = cmd.ExecuteReader()
                                    While r.Read()
                                        droits.Add(Convert.ToString(r(0)))
                                    End While
                                End Using
                            End Using
                            For Each code As String In New String() {"PRIX_TARIF_MODIFIER", "PRIX_FACTURE_EXCEPTION", "PRIX_DEMANDER", "PRIX_DEMANDER_FACTURE"}
                                If DelegationPrixRegles.AutoriseCommande(code, p.Active, p.Demandes, p.Immediate, droits) Then actions.Add(code)
                            Next
                            diagnostic.ExplicationTarif = "Tarif officiel : " & If(actions.Contains("PRIX_TARIF_MODIFIER"), "modification autorisée sous contrôle des seuils.", If(actions.Contains("PRIX_DEMANDER"), "demande d'approbation autorisée.", "accès refusé. " & If(Not p.Immediate, "Modifications immédiates désactivées. ", "") & If(Not p.Demandes, "Demandes désactivées. ", "") & If(Not droits.Contains("PRIX_TARIF_MODIFIER"), "Permission Modifier le tarif officiel absente. ", "") & If(Not droits.Contains("PRIX_DEMANDER"), "Permission Demander un tarif officiel absente.", "")))
                            If actions.Contains("PRIX_DEMANDER_FACTURE") OrElse actions.Contains("PRIX_FACTURE_EXCEPTION") Then
                                diagnostic.Explication = "Prix de l'article : sélectionnez une ligne du panier. Tarif officiel et exception de facture ont des droits distincts."
                            Else
                                diagnostic.Explication = "Prix de l'article indisponible : autorisez « Prix : demander une exception pour une facture » ou « Prix : exception sur une facture uniquement » dans le rôle actif, puis activez les demandes ou modifications immédiates correspondantes."
                            End If
                        Catch ex As UnauthorizedAccessException
                            actions.Clear()
                            diagnostic.Explication = "Prix indisponibles : vérifiez Consulter les prix, l'accès Facturation et la validité de la session du rôle actif. Une reconnexion peut être nécessaire."
                        End Try
                    End If
                    If diagnostic.ExplicationTarif Is Nothing Then diagnostic.ExplicationTarif = diagnostic.Explication
                    tx.Commit()
                End Using
            End Using
            Return diagnostic
        End Function

        Public Shared Function LireConfiguration(cn As SqlConnection, tx As SqlTransaction) As ConfigurationPrix
            Using cmd As New SqlCommand("SELECT DelegationPrixActive,PrixDemandesActives,PrixModificationImmediate,PrixSousCoutAutorise,PrixVariationMaxPourcent,PrixMargeMinimumPourcent,PrixDemandeDureeHeures FROM dbo.Parametres WITH(HOLDLOCK)", cn, tx)
                Using r As SqlDataReader = cmd.ExecuteReader()
                    If Not r.Read() Then Throw New InvalidOperationException("Configuration entreprise absente.")
                    Dim p As New ConfigurationPrix With {.Active = CBool(r(0)), .Demandes = CBool(r(1)), .Immediate = CBool(r(2)), .SousCout = CBool(r(3)), .Variation = CDec(r(4)), .Marge = CDec(r(5)), .DureeHeures = CInt(r(6))}
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
                    Configurer(cn, tx, p, attendu, motif)
                    tx.Commit()
                End Using
            End Using
        End Sub

        Friend Shared Sub Configurer(cn As SqlConnection, tx As SqlTransaction, p As ConfigurationPrix, attendu As ConfigurationPrix, motif As String)
                    DelegationPrixRegles.VerifierDuree(p.DureeHeures)
                    motif = AuditMetierService.ValiderMotif(motif)
                    If p.Variation < 0D OrElse p.Variation > 10000D OrElse p.Marge < 0D OrElse p.Marge > 10000D Then Throw New ArgumentException("Seuils hors limites.")
                    AutorisationActionService.Exiger(cn, tx, "PARAMETRES_PRIX", "PARAMETRES")
                    ' Le verrou de configuration garantit un seul changement,
                    ' sans écraser une modification concurrente d'administrateur.
                    Using cmd As New SqlCommand("SELECT COUNT(*) FROM dbo.Parametres WITH(UPDLOCK,HOLDLOCK)", cn, tx)
                        If CInt(cmd.ExecuteScalar()) <> 1 Then Throw New InvalidOperationException("Configuration entreprise ambiguë.")
                    End Using
                    Dim avant As ConfigurationPrix = LireConfiguration(cn, tx)
                    Dim json As New JavaScriptSerializer()
                    If json.Serialize(avant) <> json.Serialize(attendu) Then Throw New DBConcurrencyException("Configuration changée : rechargez.")
                    Using cmd As New SqlCommand("UPDATE dbo.Parametres SET DelegationPrixActive=@a,PrixDemandesActives=@d,PrixModificationImmediate=@i,PrixSousCoutAutorise=@s,PrixVariationMaxPourcent=@v,PrixMargeMinimumPourcent=@m,PrixDemandeDureeHeures=@heures", cn, tx)
                        cmd.Parameters.AddWithValue("@a", p.Active)
                        cmd.Parameters.AddWithValue("@d", p.Demandes)
                        cmd.Parameters.AddWithValue("@i", p.Immediate)
                        cmd.Parameters.AddWithValue("@s", p.SousCout)
                        cmd.Parameters.AddWithValue("@v", p.Variation)
                        cmd.Parameters.AddWithValue("@m", p.Marge)
                        cmd.Parameters.AddWithValue("@heures", p.DureeHeures)
                        cmd.ExecuteNonQuery()
                    End Using
                    AuditMetierService.Enregistrer(cn, tx, If(p.Active, "DELEGATION_PRIX_ACTIVEE", "DELEGATION_PRIX_DESACTIVEE"), "AUTORISATIONS", "Parametres", 0, "DELEGATION_PRIX", avant, p, motif, Guid.NewGuid())
        End Sub

        Public Sub ConfigurerCartes(modes As ConfigurationModes, modesAttendus As ConfigurationModes, prix As ConfigurationPrix, prixAttendus As ConfigurationPrix, motif As String)
            ' Les deux cartes partagent la transaction : aucun demi-enregistrement
            ' si une permission, une version ou l'audit de la seconde carte echoue.
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    If modes IsNot Nothing Then ConfigurationModesService.Enregistrer(cn, tx, modes, modesAttendus, motif)
                    If prix IsNot Nothing Then Configurer(cn, tx, prix, prixAttendus, motif)
                    tx.Commit()
                End Using
            End Using
        End Sub

        Public Function Consulter(produitId As Integer, typeVente As String, Optional typeTarifId As Integer? = Nothing) As TarifDelegue
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "PRIX_CONSULTER", "FACTURIER")
                    Dim tarif As TarifDelegue = LireTarif(cn, tx, produitId, typeVente, typeTarifId)
                    tx.Commit()
                    Return tarif
                End Using
            End Using
        End Function

        Public Function ListerTarifsActifs(produitId As Integer) As List(Of TarifDelegue)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "PRIX_CONSULTER", "FACTURIER")
                    Dim produit As New DataTable
                    Using cmd As New SqlCommand("SELECT * FROM dbo.Produits WITH(UPDLOCK,HOLDLOCK) WHERE ProduitId=@p AND EstActif=1", cn, tx)
                        cmd.Parameters.AddWithValue("@p", produitId)
                        Using r As SqlDataReader = cmd.ExecuteReader()
                            produit.Load(r)
                        End Using
                    End Using
                    If produit.Rows.Count <> 1 Then Throw New InvalidOperationException("Produit inactif ou introuvable.")
                    Dim p As DataRow = produit.Rows(0)
                    Dim types As New DataTable
                    Using cmd As New SqlCommand("SELECT t.* FROM dbo.TypesVenteProduit t WITH(HOLDLOCK) LEFT JOIN dbo.ProduitConditionnements c ON c.ProduitConditionnementId=t.ProduitConditionnementId AND c.ProduitId=t.ProduitId WHERE t.ProduitId=@p AND t.Actif=1 AND (t.ProduitConditionnementId IS NULL OR (c.EstActif=1 AND c.EstVendable=1)) ORDER BY t.TypeVenteProduitId", cn, tx)
                        cmd.Parameters.AddWithValue("@p", produitId)
                        Using r As SqlDataReader = cmd.ExecuteReader()
                            types.Load(r)
                        End Using
                    End Using
                    Dim conditionnements As New DataTable
                    Using cmd As New SqlCommand("SELECT ProduitConditionnementId,FacteurVersBase FROM dbo.ProduitConditionnements WITH(HOLDLOCK) WHERE ProduitId=@p AND EstActif=1 AND EstVendable=1", cn, tx)
                        cmd.Parameters.AddWithValue("@p", produitId)
                        Using r As SqlDataReader = cmd.ExecuteReader()
                            conditionnements.Load(r)
                        End Using
                    End Using
                    Dim noms As List(Of String) = TarifCatalogueRegles.StandardsDepuisProduit(p)
                    Dim resultat As New List(Of TarifDelegue)
                    For Each nom As String In noms
                        Dim typeConversion As String = If(nom = "speciale" OrElse nom = "promo", "PIECE", nom.ToUpperInvariant())
                        Dim conversion As Decimal = If(p.IsNull("ConversionUnite"), 1D, CDec(p("ConversionUnite")))
                        Dim principal As Decimal = If(p.IsNull("ContenuUnitePrincipale"), conversion, CDec(p("ContenuUnitePrincipale")))
                        Dim secondaire As Decimal = If(p.IsNull("ContenuUniteSecondaire"), 0D, CDec(p("ContenuUniteSecondaire")))
                        If StockUnitConversionService.CalculerQuantiteBaseTypeStandard(typeConversion, If(conversion > 0D, conversion, 1D), Convert.ToString(p("TypeGestionStock")), principal, secondaire).HasValue Then resultat.Add(LireTarifInterne(cn, tx, produitId, nom, 0, True, produit, types, conditionnements))
                    Next
                    For Each row As DataRow In types.Rows
                        Dim nom As String = CStr(row("Nom"))
                        If resultat.Exists(Function(t) String.Equals(t.TypeVente, nom, StringComparison.OrdinalIgnoreCase)) Then Continue For
                        resultat.Add(LireTarifInterne(cn, tx, produitId, nom, CInt(row("TypeVenteProduitId")), True, produit, types, conditionnements))
                    Next
                    tx.Commit()
                    Return resultat
                End Using
            End Using
        End Function

        Public Shared Function LireTarif(cn As SqlConnection, tx As SqlTransaction, id As Integer, typeVente As String, Optional typeTarifId As Integer? = Nothing, Optional exigerCout As Boolean = True) As TarifDelegue
            Return LireTarifInterne(cn, tx, id, typeVente, typeTarifId, exigerCout)
        End Function

        Private Shared Function LireTarifInterne(cn As SqlConnection, tx As SqlTransaction, id As Integer, typeVente As String, typeTarifId As Integer?, exigerCout As Boolean, Optional produitCharge As DataTable = Nothing, Optional typesCharges As DataTable = Nothing, Optional conditionnementsCharges As DataTable = Nothing) As TarifDelegue
            ' Le catalogue charge les trois jeux une seule fois. Les ecritures
            ' passent toujours sans cache, avec relecture et verrous existants.
            If typeTarifId.HasValue AndAlso typeTarifId.Value < 0 Then Throw New ArgumentException("Identifiant tarif invalide.")
            Dim produit As DataTable = produitCharge
            If produit Is Nothing Then
                produit = New DataTable()
                Using cmd As New SqlCommand("SELECT * FROM dbo.Produits WITH(UPDLOCK,HOLDLOCK) WHERE ProduitId=@id AND EstActif=1", cn, tx)
                    cmd.Parameters.AddWithValue("@id", id)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        produit.Load(reader)
                    End Using
                End Using
            End If
            If produit.Rows.Count <> 1 Then Throw New InvalidOperationException("Produit inactif ou introuvable.")
            Dim row As DataRow = produit.Rows(0)
            Dim conversion As Decimal = If(row.IsNull("ConversionUnite"), 1D, CDec(row("ConversionUnite")))
            Dim principal As Decimal = If(row.IsNull("ContenuUnitePrincipale"), conversion, CDec(row("ContenuUnitePrincipale")))
            Dim secondaire As Decimal = If(row.IsNull("ContenuUniteSecondaire"), 0D, CDec(row("ContenuUniteSecondaire")))
            Dim gestion As String = Convert.ToString(row("TypeGestionStock"))
            Dim achat As Decimal = CDec(row("PrixAchat"))
            Dim t As New TarifDelegue With {.ProduitId = id, .Produit = Convert.ToString(row("Libelle")), .CodeBarres = Convert.ToString(row("CodeBarres")), .TypeVente = typeVente}
            Dim champ As String = Nothing
            Dim standard As Boolean = Champs.TryGetValue(typeVente, champ)
            If standard AndAlso Not typeTarifId.HasValue Then
                Using doublon As New SqlCommand("SELECT COUNT(*) FROM dbo.TypesVenteProduit WITH(HOLDLOCK) WHERE ProduitId=@p AND Nom=@n AND Actif=1", cn, tx)
                    doublon.Parameters.AddWithValue("@p", id) : doublon.Parameters.AddWithValue("@n", typeVente)
                    If CInt(doublon.ExecuteScalar()) > 0 Then Throw New InvalidOperationException("Type de vente ambigu : sélectionnez le type exact avant toute délégation. Les anciennes données ne sont pas réinterprétées.")
                End Using
            End If
            If standard AndAlso (Not typeTarifId.HasValue OrElse typeTarifId.Value = 0) Then
                t.ChampPrix = champ
                t.Prix = CDec(row(champ))
                Dim unitaire As Boolean = String.Equals(typeVente, "speciale", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(typeVente, "promo", StringComparison.OrdinalIgnoreCase)
                Dim typeConversion As String = If(unitaire, "PIECE", typeVente.ToUpperInvariant())
                Dim qte As Decimal? = StockUnitConversionService.CalculerQuantiteBaseTypeStandard(typeConversion, If(conversion > 0D, conversion, 1D), gestion, principal, secondaire)
                If Not qte.HasValue Then Throw New InvalidOperationException("Type de vente non convertible.")
                t.QuantiteBase = qte.Value
            Else
                Dim types As New DataTable()
                If typeTarifId.HasValue AndAlso typeTarifId.Value = 0 Then Throw New InvalidOperationException("Tarif standard introuvable.")
                If typesCharges IsNot Nothing AndAlso typeTarifId.HasValue Then
                    types = typesCharges.Clone()
                    For Each ligne As DataRow In typesCharges.Select("TypeVenteProduitId=" & typeTarifId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        types.ImportRow(ligne)
                    Next
                Else
                    Using cmd As New SqlCommand("SELECT * FROM dbo.TypesVenteProduit WITH(UPDLOCK,HOLDLOCK) WHERE ProduitId=@p AND Nom=@nom AND Actif=1 AND (@id IS NULL OR TypeVenteProduitId=@id)", cn, tx)
                        cmd.Parameters.AddWithValue("@p", id)
                        cmd.Parameters.AddWithValue("@nom", typeVente)
                        cmd.Parameters.AddWithValue("@id", If(typeTarifId.HasValue, CType(typeTarifId.Value, Object), DBNull.Value))
                        Using reader As SqlDataReader = cmd.ExecuteReader()
                            types.Load(reader)
                        End Using
                    End Using
                End If
                If types.Rows.Count <> 1 Then Throw New InvalidOperationException("Type de vente introuvable ou ambigu.")
                Dim tr As DataRow = types.Rows(0)
                t.TypeId = CInt(tr("TypeVenteProduitId"))
                t.ModePrix = Convert.ToString(tr("ModePrix"))
                t.Coefficient = If(tr.IsNull("Coefficient"), 0D, CDec(tr("Coefficient")))
                t.Prix = CDec(tr("PrixVente"))
                If Not tr.IsNull("ProduitConditionnementId") Then
                    Dim facteur As Object = Nothing
                    If conditionnementsCharges IsNot Nothing Then
                        Dim correspondances As DataRow() = conditionnementsCharges.Select("ProduitConditionnementId=" & CInt(tr("ProduitConditionnementId")).ToString(System.Globalization.CultureInfo.InvariantCulture))
                        If correspondances.Length = 1 Then facteur = correspondances(0)("FacteurVersBase")
                    Else
                        Using cmd As New SqlCommand("SELECT FacteurVersBase FROM dbo.ProduitConditionnements WITH(UPDLOCK,HOLDLOCK) WHERE ProduitConditionnementId=@id AND ProduitId=@p AND EstActif=1 AND EstVendable=1", cn, tx)
                            cmd.Parameters.AddWithValue("@id", CInt(tr("ProduitConditionnementId")))
                            cmd.Parameters.AddWithValue("@p", id)
                            facteur = cmd.ExecuteScalar()
                        End Using
                    End If
                    If facteur Is Nothing Then Throw New InvalidOperationException("Conditionnement non vendable.")
                    Dim dto As New TypeVenteProduitDTO With {.QuantiteEquivalent = CDec(tr("QuantiteEquivalent")), .ProduitConditionnementId = CInt(tr("ProduitConditionnementId"))}
                    t.QuantiteBase = ConversionUniteService.CalculerQuantiteBase(1D, dto, New ProduitConditionnementDTO With {.ProduitConditionnementId = dto.ProduitConditionnementId.Value, .FacteurVersBase = CDec(facteur)})
                Else
                    Dim unite As String = If(tr.IsNull("TypeQuantiteEquivalent"), Convert.ToString(tr("TypeUniteEquivalent")), Convert.ToString(tr("TypeQuantiteEquivalent")))
                    t.QuantiteBase = CalculVenteService.CalculerQuantiteBaseTypeVente(CDec(tr("QuantiteEquivalent")), unite, conversion, If(principal > 0D, principal, conversion), secondaire)
                End If
                t.CoutCoefficient = CalculVenteService.CalculerCoutEquivalentCoefficient(achat, t.QuantiteBase, If(principal > 0D, principal, If(conversion > 0D, conversion, 1D)))
                If String.Equals(t.ModePrix, "COEFFICIENT", StringComparison.OrdinalIgnoreCase) AndAlso t.Coefficient > 0D AndAlso t.CoutCoefficient > 0D Then t.Prix = Math.Round(t.CoutCoefficient * t.Coefficient, 2)
            End If
            Dim coutBase As Decimal? = CalculVenteService.CalculerCoutUnitaireBase(achat, conversion, gestion, principal)
            If t.QuantiteBase <= 0D Then Throw New InvalidOperationException("Équivalent physique invalide.")
            If exigerCout AndAlso (Not coutBase.HasValue OrElse coutBase.Value <= 0D) Then Throw New InvalidOperationException("Coût comparable indisponible : délégation refusée.")
            t.Cout = If(coutBase.HasValue AndAlso coutBase.Value > 0D, coutBase.GetValueOrDefault() * t.QuantiteBase, 0D)
            t.PrixAchatReference = achat : t.UnitePrincipale = Convert.ToString(row("UnitePrincipale"))
            t.TypeActif = t.TypeId > 0 OrElse TarifCatalogueRegles.StandardsDepuisProduit(row).Contains(typeVente.ToLowerInvariant())
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
                    AutorisationActionService.Exiger(cn, tx, "PRIX_CONSULTER", "FACTURIER")
                    Dim p As ConfigurationPrix = LireConfiguration(cn, tx)
                    If Not p.Active Then Throw New UnauthorizedAccessException("Délégation des prix désactivée.")
                    Dim actuel As TarifDelegue = LireTarif(cn, tx, t.ProduitId, t.TypeVente, t.TypeId)
                    If actuel.Empreinte <> t.Empreinte Then Throw New DBConcurrencyException("Le tarif ou sa configuration a changé. Rechargez.")
                    If Not actuel.TypeActif Then Throw New UnauthorizedAccessException("Ce type de vente est désactivé pour le produit.")
                    Dim approbation As Boolean = DelegationPrixRegles.NecessiteApprobation(actuel.Prix, nouveau, actuel.Cout, p.Variation, p.Marge)
                    VerifierSousCout(cn, tx, p, actuel, nouveau, "FACTURIER")
                    If demande Then
                        If Not p.Demandes Then Throw New UnauthorizedAccessException("Les demandes sont désactivées.")
                        ' Le verrou produit pris par LireTarif serialise les
                        ' soumissions concurrentes ; un rejeu ne cree pas de doublon.
                        Using existante As New SqlCommand("SELECT TOP(1) DemandeId FROM dbo.DemandesModificationPrix WHERE ProduitId=@p AND TypeVente=@type AND ISNULL(TypeTarifId,0)=@tarif AND Perimetre='TARIF' AND DemandePar=@u AND EmpreinteTarif=@hash AND NouveauPrix=@prix AND Motif=@motif AND Etat='EN_ATTENTE' AND ExpireLe>SYSUTCDATETIME() ORDER BY DemandeId DESC", cn, tx)
                            existante.Parameters.AddWithValue("@p", actuel.ProduitId)
                            existante.Parameters.AddWithValue("@type", actuel.TypeVente)
                            existante.Parameters.AddWithValue("@tarif", actuel.TypeId)
                            existante.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId)
                            existante.Parameters.AddWithValue("@hash", actuel.Empreinte)
                            existante.Parameters.AddWithValue("@prix", nouveau)
                            existante.Parameters.AddWithValue("@motif", motif)
                            Dim idExistant As Object = existante.ExecuteScalar()
                            If idExistant IsNot Nothing Then
                                tx.Commit()
                                Return "Demande " & Convert.ToInt32(idExistant).ToString() & " déjà en attente. Le tarif actif est inchangé."
                            End If
                        End Using
                        Using cmd As New SqlCommand("INSERT dbo.DemandesModificationPrix(ProduitId,TypeVente,AncienPrix,NouveauPrix,EmpreinteTarif,Motif,DemandePar,ExpireLe,CoutReferenceDemande,TypeTarifId) VALUES(@p,@type,@a,@n,@hash,@motif,@u,DATEADD(HOUR,@heures,SYSUTCDATETIME()),@cout,@tarifId); SELECT CAST(SCOPE_IDENTITY() AS int)", cn, tx)
                            cmd.Parameters.AddWithValue("@p", actuel.ProduitId)
                            cmd.Parameters.AddWithValue("@type", actuel.TypeVente)
                            cmd.Parameters.AddWithValue("@a", actuel.Prix)
                            cmd.Parameters.AddWithValue("@n", nouveau)
                            cmd.Parameters.AddWithValue("@hash", actuel.Empreinte)
                            cmd.Parameters.AddWithValue("@motif", motif)
                            cmd.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId)
                            cmd.Parameters.AddWithValue("@heures", p.DureeHeures)
                            cmd.Parameters.AddWithValue("@cout", actuel.Cout)
                            cmd.Parameters.AddWithValue("@tarifId", actuel.TypeId)
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
                If String.Equals(t.ModePrix, "COEFFICIENT", StringComparison.OrdinalIgnoreCase) Then
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
            Dim serializer As New JavaScriptSerializer()
            Dim avantTarif As Dictionary(Of String, Object) = serializer.Deserialize(Of Dictionary(Of String, Object))(serializer.Serialize(t))
            avantTarif("Marge") = TarifCatalogueRegles.Marge(t.Prix, t.Cout)
            AuditMetierService.Enregistrer(cn, tx, "PRIX_MODIFICATION_APPLIQUEE", "PRIX", "Produit", t.ProduitId, t.Produit & " / " & t.TypeVente, avantTarif, New With {.Prix = nouveau, .Coefficient = coefficient, .Marge = TarifCatalogueRegles.Marge(nouveau, t.Cout), .TypeVente = t.TypeVente, .ProduitId = t.ProduitId}, motif, Guid.NewGuid())
            AppDataVersionService.Touch(cn, tx, "PRODUITS", "TYPES_VENTE")
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
                    Using cmd As New SqlCommand("SELECT TOP(500) d.DemandeId,d.Perimetre,p.Libelle AS Produit,d.TypeVente,d.AncienPrix,d.NouveauPrix,d.Etat,d.Motif,d.DemandeLe,d.ExpireLe,u.NomUtilisateur AS Demandeur FROM dbo.DemandesModificationPrix d JOIN dbo.Produits p ON p.ProduitId=d.ProduitId JOIN dbo.Utilisateurs u ON u.UtilisateurId=d.DemandePar WHERE (@mes=0 OR d.DemandePar=@user) ORDER BY d.DemandeId DESC", cn, tx)
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

        Public Function ConsulterTarifDemande(id As Integer, ecran As String) As TarifDelegue
            If ecran <> "PARAMETRES" AndAlso ecran <> "FACTURIER" Then Throw New ArgumentException("Contexte de consultation invalide.")
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "PRIX_CONSULTER", ecran)
                    Dim demande As New DataTable
                    Using cmd As New SqlCommand("SELECT ProduitId,TypeVente,TypeTarifId FROM dbo.DemandesModificationPrix WHERE DemandeId=@id AND (@responsable=1 OR DemandePar=@u)", cn, tx)
                        cmd.Parameters.AddWithValue("@id", id) : cmd.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId)
                        cmd.Parameters.AddWithValue("@responsable", ecran = "PARAMETRES")
                        Using r As SqlDataReader = cmd.ExecuteReader()
                            demande.Load(r)
                        End Using
                    End Using
                    If demande.Rows.Count <> 1 Then Throw New UnauthorizedAccessException("Demande inaccessible.")
                    Dim row As DataRow = demande.Rows(0)
                    Dim tarif As TarifDelegue = LireTarif(cn, tx, CInt(row("ProduitId")), CStr(row("TypeVente")), If(row.IsNull("TypeTarifId"), CType(Nothing, Integer?), CInt(row("TypeTarifId"))))
                    tx.Commit()
                    Return tarif
                End Using
            End Using
        End Function

        Public Function ListerPage(page As Integer, taille As Integer, recherche As String, etat As String, Optional ecran As String = "PARAMETRES") As PageDemandesPrix
            DelegationPrixRegles.VerifierPagination(page, taille)
            If If(recherche, "").Length > 200 Then Throw New ArgumentException("Recherche limitée à 200 caractères.")
            If ecran <> "PARAMETRES" AndAlso ecran <> "FACTURIER" Then Throw New ArgumentException("Contexte invalide.")
            If Not New List(Of String) From {"", "EN_ATTENTE", "APPROUVEE", "REFUSEE", "ANNULEE", "EXPIREE"}.Contains(etat) Then Throw New ArgumentException("Statut invalide.")
            Dim resultat As New PageDemandesPrix With {.Lignes = New DataTable()}
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "PRIX_CONSULTER", ecran)
                    ' L'expiration effective est presentee sans ecriture lors de
                    ' cette consultation paginee. La decision recontrole l'heure SQL.
                    Dim source As String = " FROM dbo.DemandesModificationPrix d JOIN dbo.Produits p ON p.ProduitId=d.ProduitId JOIN dbo.Utilisateurs u ON u.UtilisateurId=d.DemandePar LEFT JOIN dbo.Utilisateurs decideur ON decideur.UtilisateurId=d.DecisionPar WHERE (@mes=0 OR d.DemandePar=@u)"
                    Dim statut As String = "CASE WHEN d.Etat='EN_ATTENTE' AND d.ExpireLe<=SYSUTCDATETIME() THEN 'EXPIREE' ELSE d.Etat END"
                    Dim filtre As String = " AND (@q='' OR CHARINDEX(@q,p.Libelle COLLATE Latin1_General_CI_AI)>0 OR CHARINDEX(@q,u.NomUtilisateur COLLATE Latin1_General_CI_AI)>0 OR CHARINDEX(@q,d.TypeVente COLLATE Latin1_General_CI_AI)>0) AND (@etat='' OR " & statut & "=@etat)"
                    Dim compteurs As String = "SELECT COUNT(*),ISNULL(SUM(CASE WHEN " & statut & "='EN_ATTENTE' THEN 1 ELSE 0 END),0),ISNULL(SUM(CASE WHEN d.Etat='APPROUVEE' THEN 1 ELSE 0 END),0),ISNULL(SUM(CASE WHEN d.Etat='REFUSEE' THEN 1 ELSE 0 END),0)" & source
                    Dim total As String = "SELECT COUNT(*)" & source & filtre
                    Dim lignes As String = "SELECT d.DemandeId,d.Perimetre,u.NomUtilisateur AS Demandeur,p.Libelle AS Produit,d.TypeVente AS Conditionnement,d.AncienPrix,d.NouveauPrix,d.CoutReferenceDemande,d.QuantiteContexte,d.SousTotalContexte,d.RemiseContexte,d.TypeTarifId,d.Motif,d.DemandeLe,d.ExpireLe,d.UtiliseeLe,d.MotifDecision,d.DecisionPar,decideur.NomUtilisateur AS Decideur,d.DecisionLe," & statut & " AS Statut" & source & filtre & " ORDER BY d.DemandeId DESC OFFSET @offset ROWS FETCH NEXT @taille ROWS ONLY"
                    Using cmd As New SqlCommand(compteurs & ";" & total & ";" & lignes & ";", cn, tx)
                        cmd.Parameters.AddWithValue("@mes", ecran = "FACTURIER")
                        cmd.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId)
                        cmd.Parameters.AddWithValue("@q", If(recherche, "").Trim())
                        cmd.Parameters.AddWithValue("@etat", etat)
                        cmd.Parameters.AddWithValue("@offset", page * taille)
                        cmd.Parameters.AddWithValue("@taille", taille)
                        Using r As SqlDataReader = cmd.ExecuteReader()
                            r.Read()
                            resultat.EnAttente = CInt(r(1)) : resultat.Approuvees = CInt(r(2)) : resultat.Refusees = CInt(r(3))
                            r.NextResult() : r.Read() : resultat.Total = CInt(r(0))
                            r.NextResult() : resultat.Lignes.Load(r)
                        End Using
                    End Using
                    tx.Commit()
                End Using
            End Using
            Return resultat
        End Function

        Public Sub AnnulerDemande(id As Integer, motif As String)
            motif = AuditMetierService.ValiderMotif(motif)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Dim perimetre As String
                    Using lire As New SqlCommand("SELECT Perimetre FROM dbo.DemandesModificationPrix WITH(UPDLOCK,HOLDLOCK) WHERE DemandeId=@id AND DemandePar=@u", cn, tx)
                        lire.Parameters.AddWithValue("@id", id) : lire.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId)
                        perimetre = Convert.ToString(lire.ExecuteScalar())
                    End Using
                    AutorisationActionService.Exiger(cn, tx, If(perimetre = "FACTURE", "PRIX_DEMANDER_FACTURE", "PRIX_DEMANDER"), "FACTURIER")
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
                        Dim actuel As TarifDelegue = LireTarif(cn, tx, CInt(row("ProduitId")), Convert.ToString(row("TypeVente")), If(row.IsNull("TypeTarifId"), CType(Nothing, Integer?), CInt(row("TypeTarifId"))))
                        If Not actuel.TypeActif Then Throw New UnauthorizedAccessException("Type de vente désactivé : nouvelle demande nécessaire après réactivation.")
                        If actuel.Empreinte <> Convert.ToString(row("EmpreinteTarif")) Then Throw New DBConcurrencyException("Tarif ou coût modifié depuis la demande : nouvelle demande nécessaire.")
                        Dim nouveau As Decimal = CDec(row("NouveauPrix"))
                        DelegationPrixRegles.VerifierPrix(nouveau)
                        Dim net As Decimal = nouveau
                        If Convert.ToString(row("Perimetre")) = "FACTURE" Then net = DelegationPrixRegles.PrixNet(nouveau, New ContexteCommercialPrix With {.SousTotal = CDec(row("SousTotalContexte")), .Remise = CDec(row("RemiseContexte"))})
                        VerifierSousCout(cn, tx, p, actuel, net, "PARAMETRES")
                        If Convert.ToString(row("Perimetre")) = "TARIF" Then Appliquer(cn, tx, actuel, nouveau, motif)
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
            If Not p.Immediate Then Throw New UnauthorizedAccessException("Modifications immédiates désactivées. Soumettez une demande d'approbation pour cet article.")
            AutorisationActionService.Exiger(cn, tx, "PRIX_FACTURE_EXCEPTION", "FACTURIER")
            ligne.MotifPrixException = AuditMetierService.ValiderMotif(ligne.MotifPrixException)
            Dim t As TarifDelegue = LireTarif(cn, tx, ligne.ProduitId, ligne.TypeVente, ligne.TypeTarifId)
            If Not t.TypeActif Then Throw New UnauthorizedAccessException("Ce type de vente est désactivé pour une nouvelle exception.")
            If DelegationPrixRegles.NecessiteApprobation(t.Prix, ligne.PrixUnitaire, t.Cout, p.Variation, p.Marge) Then Throw New UnauthorizedAccessException("Exception hors seuil : soumettez une demande d'approbation pour cet article. Le tarif officiel reste inchangé.")
            VerifierSousCout(cn, tx, p, t, ligne.PrixUnitaire, "FACTURIER")
        End Sub

        Public Sub VerifierExceptionProposee(t As TarifDelegue, prix As Decimal, motif As String, Optional commercial As ContexteCommercialPrix = Nothing)
            Try
                VerifierExceptionProposeeInterne(t, prix, motif, commercial)
            Catch ex As Exception
                If t IsNot Nothing Then AuditMetierService.Echec(_dal, "PRIX_EXCEPTION_REFUSEE", t.ProduitId, Guid.NewGuid(), ex, "Produit", t.Produit & " / " & t.TypeVente)
                Throw
            End Try
        End Sub

        Private Sub VerifierExceptionProposeeInterne(t As TarifDelegue, prix As Decimal, motif As String, commercial As ContexteCommercialPrix)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    ' Meme ordre de verrouillage que les autres operations :
                    ' contexte de securite avant la ligne produit.
                    AutorisationActionService.Exiger(cn, tx, "PRIX_FACTURE_EXCEPTION", "FACTURIER")
                    AutorisationActionService.Exiger(cn, tx, "PRIX_CONSULTER", "FACTURIER")
                    Dim actuel As TarifDelegue = LireTarif(cn, tx, t.ProduitId, t.TypeVente, t.TypeId)
                    If actuel.Empreinte <> t.Empreinte Then Throw New DBConcurrencyException("Le tarif a changé. Rechargez.")
                    If Not actuel.TypeActif Then Throw New UnauthorizedAccessException("Ce type de vente est désactivé.")
                    VerifierException(cn, tx, New LigneFactureVente With {.ProduitId = t.ProduitId, .TypeVente = t.TypeVente, .TypeTarifId = t.TypeId, .PrixUnitaire = prix, .MotifPrixException = motif})
                    If commercial IsNot Nothing Then
                        Dim p As ConfigurationPrix = LireConfiguration(cn, tx)
                        If DelegationPrixRegles.NecessiteApprobation(actuel.Prix, DelegationPrixRegles.PrixNet(prix, commercial), actuel.Cout, p.Variation, p.Marge) Then Throw New UnauthorizedAccessException("Le prix après remise nécessite une approbation pour cet article.")
                    End If
                    tx.Commit()
                End Using
            End Using
        End Sub

        Public Shared Sub ValiderLigne(cn As SqlConnection, tx As SqlTransaction, ligne As LigneFactureVente, avant As Object, factureId As Integer, reference As String, Optional contexte As ContexteCommercialPrix = Nothing)
            ' Les prix deja valides dans ce brouillon ne sont pas rafraichis
            ' silencieusement. Une nouvelle exception passe par ses propres droits.
            Dim ancien As Dictionary(Of String, Object) = TryCast(avant, Dictionary(Of String, Object))
            If ancien IsNot Nothing Then
                Dim articles As List(Of Dictionary(Of String, Object)) = DirectCast(ancien("Lignes"), List(Of Dictionary(Of String, Object)))
                For Each article As Dictionary(Of String, Object) In articles
                    Dim ancienneApprobation As Integer? = If(article.ContainsKey("DemandePrixId") AndAlso article("DemandePrixId") IsNot Nothing, CType(CInt(article("DemandePrixId")), Integer?), Nothing)
                    Dim nouvelleApprobation As Boolean = ligne.DemandePrixId.HasValue AndAlso (Not ancienneApprobation.HasValue OrElse ligne.DemandePrixId.Value <> ancienneApprobation.Value)
                    Dim ancienTarif As Integer? = If(article.ContainsKey("TypeTarifId") AndAlso article("TypeTarifId") IsNot Nothing, CType(CInt(article("TypeTarifId")), Integer?), Nothing)
                    Dim memeTarif As Boolean = Not ligne.TypeTarifId.HasValue OrElse Not ancienTarif.HasValue OrElse ligne.TypeTarifId.Value = ancienTarif.Value
                    If memeTarif AndAlso Not nouvelleApprobation AndAlso CInt(article("ProduitId")) = ligne.ProduitId AndAlso String.Equals(Convert.ToString(article("TypeVente")), ligne.TypeVente, StringComparison.OrdinalIgnoreCase) AndAlso CDec(article("PrixUnitaire")) = ligne.PrixUnitaire Then
                        If article.ContainsKey("DemandePrixId") AndAlso article("DemandePrixId") IsNot Nothing AndAlso CDec(If(article("QuantiteSaisie"), article("Quantite"))) <> If(ligne.QteSaisie, ligne.Quantite) Then Throw New UnauthorizedAccessException("La quantité approuvée ne peut pas être modifiée. Demandez une nouvelle approbation.")
                        If article.ContainsKey("DemandePrixId") AndAlso article("DemandePrixId") IsNot Nothing AndAlso CDec(article("QuantiteBase")) <> ligne.QuantiteBase Then Throw New UnauthorizedAccessException("La quantité de stock approuvée ne peut pas être remplacée.")
                        If contexte IsNot Nothing Then
                            Dim ancienEntete As Dictionary(Of String, Object) = DirectCast(ancien("Entete"), Dictionary(Of String, Object))
                            Dim modifie As Boolean = contexte.SousTotal <> CDec(ancienEntete("SousTotal")) OrElse contexte.Remise <> CDec(ancienEntete("MontantRemise"))
                            If modifie AndAlso ancienneApprobation.HasValue Then Throw New UnauthorizedAccessException("Le panier ou la remise approuvés ont changé. Une nouvelle approbation est nécessaire.")
                            If modifie Then
                                Dim configuration As ConfigurationPrix = LireConfiguration(cn, tx)
                                If configuration.Active Then
                                    Dim actuel As TarifDelegue = LireTarif(cn, tx, ligne.ProduitId, ligne.TypeVente, ancienTarif, exigerCout:=Not String.IsNullOrWhiteSpace(Convert.ToString(article("MotifPrixException"))))
                                    Dim coutPhysique As Decimal = actuel.Cout / actuel.QuantiteBase * ligne.QuantiteBase / If(ligne.QteSaisie, ligne.Quantite)
                                    Dim net As Decimal = DelegationPrixRegles.PrixNet(ligne.PrixUnitaire, contexte)
                                    If net < coutPhysique Then Throw New UnauthorizedAccessException(If(configuration.SousCout, "Le prix après remise nécessite une approbation pour cet article.", "Vente sous coût interdite, remise comprise."))
                                    If Not String.IsNullOrWhiteSpace(Convert.ToString(article("MotifPrixException"))) AndAlso DelegationPrixRegles.NecessiteApprobation(actuel.Prix, net, coutPhysique, configuration.Variation, configuration.Marge) Then Throw New UnauthorizedAccessException("Le prix après remise nécessite une approbation.")
                                End If
                            End If
                        End If
                        ligne.MotifPrixException = Convert.ToString(article("MotifPrixException"))
                        ligne.DemandePrixId = ancienneApprobation
                        ligne.TypeTarifId = ancienTarif
                        If article.ContainsKey("ContextePrix") AndAlso article("ContextePrix") IsNot Nothing Then ligne.ContextePrix = DirectCast(article("ContextePrix"), Guid)
                        Return
                    End If
                Next
            End If
            Dim p As ConfigurationPrix = LireConfiguration(cn, tx)
            If Not p.Active AndAlso String.IsNullOrWhiteSpace(ligne.MotifPrixException) Then Return
            Dim tarif As TarifDelegue = LireTarif(cn, tx, ligne.ProduitId, ligne.TypeVente, ligne.TypeTarifId, exigerCout:=False)
            ligne.TypeTarifId = tarif.TypeId
            If ligne.DemandePrixId.HasValue Then
                ConsommerException(cn, tx, ligne, factureId, tarif, contexte)
                AuditMetierService.Enregistrer(cn, tx, "FACTURE_PRIX_MODIFIE", "PRIX", "Facture", factureId, reference, tarif, New With {.ProduitId = ligne.ProduitId, .Prix = ligne.PrixUnitaire, .DemandeId = ligne.DemandePrixId}, ligne.MotifPrixException, Guid.NewGuid())
                Return
            End If
            If tarif.Prix = ligne.PrixUnitaire AndAlso String.IsNullOrWhiteSpace(ligne.MotifPrixException) Then
                If contexte IsNot Nothing AndAlso DelegationPrixRegles.PrixNet(ligne.PrixUnitaire, contexte) < tarif.Cout Then Throw New UnauthorizedAccessException(If(p.SousCout, "Le prix après remise passe sous coût : demandez une approbation pour cet article.", "Vente sous coût interdite, remise comprise."))
                Return
            End If
            VerifierException(cn, tx, ligne)
            If contexte IsNot Nothing Then
                Dim net As Decimal = DelegationPrixRegles.PrixNet(ligne.PrixUnitaire, contexte)
                Dim configuration As ConfigurationPrix = LireConfiguration(cn, tx)
                If DelegationPrixRegles.NecessiteApprobation(tarif.Prix, net, tarif.Cout, configuration.Variation, configuration.Marge) Then Throw New UnauthorizedAccessException("Le prix après remise nécessite une approbation pour cet article.")
            End If
            AuditMetierService.Enregistrer(cn, tx, "FACTURE_PRIX_MODIFIE", "PRIX", "Facture", factureId, reference,
                tarif, New With {.ProduitId = ligne.ProduitId, .TypeVente = ligne.TypeVente, .Prix = ligne.PrixUnitaire}, ligne.MotifPrixException, Guid.NewGuid())
        End Sub

        Public Function DemanderException(t As TarifDelegue, nouveau As Decimal, motif As String, contexte As Guid, quantite As Decimal, Optional commercial As ContexteCommercialPrix = Nothing) As Integer
            Try
                Return DemanderExceptionInterne(t, nouveau, motif, contexte, quantite, commercial)
            Catch ex As Exception
                If t IsNot Nothing Then AuditMetierService.Echec(_dal, "PRIX_DEMANDE_FACTURE_REFUSEE", t.ProduitId, Guid.NewGuid(), ex, "Produit", t.Produit)
                Throw
            End Try
        End Function

        Private Function DemanderExceptionInterne(t As TarifDelegue, nouveau As Decimal, motif As String, contexte As Guid, quantite As Decimal, commercial As ContexteCommercialPrix) As Integer
            motif = AuditMetierService.ValiderMotif(motif)
            DelegationPrixRegles.VerifierPrix(nouveau)
            DelegationPrixRegles.VerifierContexte(contexte, quantite)
            If commercial Is Nothing Then commercial = New ContexteCommercialPrix With {.SousTotal = nouveau * quantite, .Remise = 0D}
            Dim net As Decimal = DelegationPrixRegles.PrixNet(nouveau, commercial)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "PRIX_DEMANDER_FACTURE", "FACTURIER")
                    Dim p As ConfigurationPrix = LireConfiguration(cn, tx)
                    If Not p.Active OrElse Not p.Demandes Then Throw New UnauthorizedAccessException("Les demandes de prix sont désactivées.")
                    Dim actuel As TarifDelegue = LireTarif(cn, tx, t.ProduitId, t.TypeVente, t.TypeId)
                    If actuel.Empreinte <> t.Empreinte Then Throw New DBConcurrencyException("Le tarif a changé. Rechargez.")
                    If Not actuel.TypeActif Then Throw New UnauthorizedAccessException("Ce type de vente est désactivé.")
                    If net < actuel.Cout AndAlso Not p.SousCout Then Throw New UnauthorizedAccessException("Vente sous coût interdite, remise comprise.")
                    Using cmd As New SqlCommand("INSERT dbo.DemandesModificationPrix(ProduitId,TypeVente,AncienPrix,NouveauPrix,EmpreinteTarif,Motif,DemandePar,Perimetre,ContexteFacture,QuantiteContexte,ExpireLe,CoutReferenceDemande,SousTotalContexte,RemiseContexte,TypeTarifId) VALUES(@p,@type,@a,@n,@hash,@motif,@u,'FACTURE',@contexte,@qte,DATEADD(HOUR,@heures,SYSUTCDATETIME()),@cout,@st,@remise,@tarifId); SELECT CAST(SCOPE_IDENTITY() AS int)", cn, tx)
                        cmd.Parameters.AddWithValue("@p", actuel.ProduitId) : cmd.Parameters.AddWithValue("@type", actuel.TypeVente)
                        cmd.Parameters.AddWithValue("@a", actuel.Prix) : cmd.Parameters.AddWithValue("@n", nouveau)
                        cmd.Parameters.AddWithValue("@hash", actuel.Empreinte) : cmd.Parameters.AddWithValue("@motif", motif)
                        cmd.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId) : cmd.Parameters.AddWithValue("@contexte", contexte)
                        cmd.Parameters.AddWithValue("@qte", quantite)
                        cmd.Parameters.AddWithValue("@heures", p.DureeHeures)
                        cmd.Parameters.AddWithValue("@cout", actuel.Cout)
                        cmd.Parameters.AddWithValue("@st", commercial.SousTotal) : cmd.Parameters.AddWithValue("@remise", commercial.Remise)
                        cmd.Parameters.AddWithValue("@tarifId", actuel.TypeId)
                        Dim id As Integer = CInt(cmd.ExecuteScalar())
                        AuditMetierService.Enregistrer(cn, tx, "PRIX_MODIFICATION_DEMANDEE", "PRIX", "DemandePrix", id, actuel.Produit, actuel, New With {.Prix = nouveau, .PrixNet = net, .Contexte = commercial, .Perimetre = "FACTURE", .ContexteFacture = contexte, .Quantite = quantite}, motif, Guid.NewGuid())
                        tx.Commit()
                        Return id
                    End Using
                End Using
            End Using
        End Function

        Public Function LireExceptionApprouvee(produitId As Integer, typeVente As String, contexte As Guid, quantite As Decimal) As ApprobationPrixArticle
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "PRIX_DEMANDER_FACTURE", "FACTURIER")
                    Dim dt As New DataTable()
                    Using cmd As New SqlCommand("SELECT TOP(1) DemandeId,NouveauPrix,Motif,SousTotalContexte,RemiseContexte,TypeTarifId FROM dbo.DemandesModificationPrix WHERE Perimetre='FACTURE' AND ContexteFacture=@c AND DemandePar=@u AND ProduitId=@p AND TypeVente=@type AND QuantiteContexte=@q AND Etat='APPROUVEE' AND UtiliseeLe IS NULL AND ExpireLe>SYSUTCDATETIME() ORDER BY DemandeId DESC", cn, tx)
                        cmd.Parameters.AddWithValue("@c", contexte) : cmd.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId)
                        cmd.Parameters.AddWithValue("@p", produitId) : cmd.Parameters.AddWithValue("@type", typeVente) : cmd.Parameters.AddWithValue("@q", quantite)
                        Using r As SqlDataReader = cmd.ExecuteReader()
                            dt.Load(r)
                        End Using
                    End Using
                    If dt.Rows.Count <> 1 Then Throw New InvalidOperationException("Aucune approbation valide pour cet article. Consultez vos demandes puis réessayez.")
                    Dim ligne As New LigneFactureVente With {.ProduitId = produitId, .TypeVente = typeVente, .Quantite = quantite, .QteSaisie = quantite, .ContextePrix = contexte, .DemandePrixId = CInt(dt.Rows(0)("DemandeId")), .PrixUnitaire = CDec(dt.Rows(0)("NouveauPrix")), .MotifPrixException = CStr(dt.Rows(0)("Motif")), .TypeTarifId = If(dt.Rows(0).IsNull("TypeTarifId"), CType(Nothing, Integer?), CInt(dt.Rows(0)("TypeTarifId")))}
                    Dim tarif As TarifDelegue = LireTarif(cn, tx, produitId, typeVente, ligne.TypeTarifId)
                    ligne.QuantiteBase = tarif.QuantiteBase * quantite
                    Dim commercial As New ContexteCommercialPrix With {.SousTotal = CDec(dt.Rows(0)("SousTotalContexte")), .Remise = CDec(dt.Rows(0)("RemiseContexte"))}
                    VerifierApprobation(cn, tx, ligne, tarif, commercial)
                    tx.Commit()
                    Return New ApprobationPrixArticle With {.Ligne = ligne, .Contexte = commercial}
                End Using
            End Using
        End Function

        Private Shared Sub VerifierApprobation(cn As SqlConnection, tx As SqlTransaction, ligne As LigneFactureVente, tarif As TarifDelegue, commercial As ContexteCommercialPrix)
            If Not tarif.TypeActif Then Throw New UnauthorizedAccessException("Type de vente désactivé : l'approbation ne peut plus être appliquée.")
            DelegationPrixRegles.VerifierPrix(ligne.PrixUnitaire)
            Dim net As Decimal = DelegationPrixRegles.PrixNet(ligne.PrixUnitaire, commercial)
            If ligne.QuantiteBase <> tarif.QuantiteBase * If(ligne.QteSaisie, ligne.Quantite) Then Throw New UnauthorizedAccessException("L'article ne correspond pas à la quantité physique approuvée.")
            AutorisationActionService.Exiger(cn, tx, "PRIX_DEMANDER_FACTURE", "FACTURIER")
            Dim p As ConfigurationPrix = LireConfiguration(cn, tx)
            If Not p.Active OrElse Not p.Demandes Then Throw New UnauthorizedAccessException("Les demandes de prix sont désactivées.")
            If net < tarif.Cout AndAlso Not p.SousCout Then Throw New UnauthorizedAccessException("Vente sous coût interdite, remise comprise.")
            Using cmd As New SqlCommand("SELECT Motif FROM dbo.DemandesModificationPrix WITH(UPDLOCK,HOLDLOCK) WHERE DemandeId=@id AND Perimetre='FACTURE' AND ContexteFacture=@c AND DemandePar=@u AND ProduitId=@p AND TypeVente=@type AND QuantiteContexte=@q AND NouveauPrix=@prix AND EmpreinteTarif=@hash AND Etat='APPROUVEE' AND UtiliseeLe IS NULL AND ExpireLe>SYSUTCDATETIME() AND SousTotalContexte=@st AND RemiseContexte=@remise", cn, tx)
                cmd.Parameters.AddWithValue("@id", ligne.DemandePrixId.Value) : cmd.Parameters.AddWithValue("@c", If(ligne.ContextePrix.HasValue, CType(ligne.ContextePrix.Value, Object), DBNull.Value))
                cmd.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId) : cmd.Parameters.AddWithValue("@p", ligne.ProduitId)
                cmd.Parameters.AddWithValue("@type", ligne.TypeVente) : cmd.Parameters.AddWithValue("@q", If(ligne.QteSaisie, ligne.Quantite))
                cmd.Parameters.AddWithValue("@prix", ligne.PrixUnitaire) : cmd.Parameters.AddWithValue("@hash", tarif.Empreinte)
                cmd.Parameters.AddWithValue("@st", commercial.SousTotal) : cmd.Parameters.AddWithValue("@remise", commercial.Remise)
                Dim motif As Object = cmd.ExecuteScalar()
                If motif Is Nothing Then Throw New UnauthorizedAccessException("Approbation expirée, utilisée, modifiée ou attribuée à un autre article.")
                ligne.MotifPrixException = Convert.ToString(motif)
            End Using
            ligne.MotifPrixException = AuditMetierService.ValiderMotif(ligne.MotifPrixException)
        End Sub

        Private Shared Sub ConsommerException(cn As SqlConnection, tx As SqlTransaction, ligne As LigneFactureVente, factureId As Integer, tarif As TarifDelegue, commercial As ContexteCommercialPrix)
            ' L'approbation est consommee une seule fois dans la transaction de
            ' facture : un rollback rend l'approbation disponible, pas un succes.
            VerifierApprobation(cn, tx, ligne, tarif, commercial)
            Using contexte As New SqlCommand("SELECT COUNT(*) FROM dbo.DemandesModificationPrix WITH(UPDLOCK,HOLDLOCK) WHERE ContexteFacture=@c AND FactureUtilisationId IS NOT NULL AND FactureUtilisationId<>@f", cn, tx)
                contexte.Parameters.AddWithValue("@c", ligne.ContextePrix.Value) : contexte.Parameters.AddWithValue("@f", factureId)
                If CInt(contexte.ExecuteScalar()) <> 0 Then Throw New UnauthorizedAccessException("Ce contexte commercial appartient déjà à une autre facture.")
            End Using
            Using cmd As New SqlCommand("UPDATE dbo.DemandesModificationPrix SET UtiliseeLe=SYSUTCDATETIME(),FactureUtilisationId=@f WHERE DemandeId=@id AND UtiliseeLe IS NULL", cn, tx)
                cmd.Parameters.AddWithValue("@f", factureId) : cmd.Parameters.AddWithValue("@id", ligne.DemandePrixId.Value)
                If cmd.ExecuteNonQuery() <> 1 Then Throw New DBConcurrencyException("Approbation déjà utilisée.")
            End Using
            AuditMetierService.Enregistrer(cn, tx, "PRIX_MODIFICATION_APPLIQUEE", "PRIX", "DemandePrix", ligne.DemandePrixId.Value, "Facture " & factureId.ToString(), New With {.Etat = "APPROUVEE", .Perimetre = "FACTURE"}, New With {.FactureId = factureId, .Prix = ligne.PrixUnitaire, .Contexte = ligne.ContextePrix, .Utilisee = True}, ligne.MotifPrixException, Guid.NewGuid())
        End Sub
    End Class
End Namespace
