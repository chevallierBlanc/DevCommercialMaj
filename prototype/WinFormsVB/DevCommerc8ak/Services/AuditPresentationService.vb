Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Text.RegularExpressions

Namespace DevCommerc8ak
    Public NotInheritable Class AuditPresentationService
        Private Sub New()
        End Sub

        Private Shared ReadOnly Libelles As New Dictionary(Of String, String)(StringComparer.Ordinal) From {
            {"MontantTotal", "Montant total"}, {"SousTotal", "Sous-total"}, {"MontantRemise", "Remise"},
            {"MontantTaxe", "Taxe"}, {"NumeroFacture", "N° facture"}, {"Statut", "Statut"},
            {"NomClient", "Client"}, {"ClientId", "Identifiant client"}, {"Telephone", "Téléphone"},
            {"Quantite", "Quantité"}, {"QuantiteSaisie", "Quantité commerciale"}, {"QuantiteBase", "Quantité de stock"},
            {"PrixUnitaire", "Prix unitaire"}, {"MontantLigne", "Total article"}, {"ProduitId", "Identifiant produit"},
            {"Produit", "Produit"}, {"TypeVente", "Conditionnement"}, {"CoutUnitaireBaseVente", "Coût par unité de stock"},
            {"FactureCreePar", "Facture créée par"}, {"EncaissePar", "Encaissé par"}, {"PayePar", "Encaissé par"},
            {"TotalFacture", "Total facture"}, {"DejaPayeAvant", "Déjà payé avant"}, {"ResteAvant", "Reste à payer avant"},
            {"MontantRecu", "Montant reçu"}, {"MontantAffecte", "Montant affecté à la facture"},
            {"MonnaieRendue", "Monnaie rendue"}, {"ResteApres", "Reste à payer après"},
            {"DeviseMontants", "Devise des montants normalisés"}, {"DeviseSaisie", "Devise de saisie"},
            {"Devise", "Devise déclarée"}, {"MontantRecuOrigine", "Montant reçu en devise de saisie"},
            {"TauxConversionApplique", "Taux appliqué vers FC"}, {"ModePaiement", "Mode de paiement"},
            {"ReferencePaiement", "Référence du paiement"}, {"PaiementId", "Identifiant paiement"}, {"Montant", "Montant"},
            {"Prix", "Prix"}, {"TypeId", "Identifiant du type de vente"}, {"Cout", "Coût comparable"}, {"Coefficient", "Coefficient tarifaire"},
            {"MontantRegularise", "Montant régularisé"}, {"NouveauStatut", "Nouveau statut"}, {"ClotureCaisseId", "Clôture concernée"},
            {"ModePrix", "Mode de calcul du tarif"}, {"MotifPrixException", "Motif du prix exceptionnel"}}

        Public Shared Function EstAuteur(champ As String) As Boolean
            Dim cle As String = champ.Substring(champ.LastIndexOf("."c) + 1)
            Return cle = "FactureCreePar" OrElse cle = "EncaissePar" OrElse cle = "PayePar"
        End Function

        Public Shared Function Libelle(champ As String, valeurs As IDictionary(Of String, String)) As String
            Dim cle As String = champ.Substring(champ.LastIndexOf("."c) + 1)
            Dim label As String = Nothing
            If Not Libelles.TryGetValue(cle, label) Then label = cle
            Dim ligne As Match = Regex.Match(champ, "^Lignes\[(\d+)\]\.")
            If ligne.Success Then
                Dim produit As String = Nothing
                Dim prefixe As String = "Lignes[" & ligne.Groups(1).Value & "]."
                If Not valeurs.TryGetValue(prefixe & "Produit", produit) Then valeurs.TryGetValue(prefixe & "Libelle", produit)
                label &= " — " & If(String.IsNullOrWhiteSpace(produit), "ligne " & (Integer.Parse(ligne.Groups(1).Value, CultureInfo.InvariantCulture) + 1).ToString(), produit)
            End If
            Return label
        End Function

        Public Shared Function AfficherValeur(champ As String, valeur As String, snapshot As IDictionary(Of String, String), noms As IDictionary(Of Integer, String)) As String
            If Not EstAuteur(champ) Then
                Dim devise As String = Nothing
                Dim montant As Decimal
                Dim cle As String = champ.Substring(champ.LastIndexOf("."c) + 1)
                Dim prefixe As String = If(champ.Contains("."), champ.Substring(0, champ.LastIndexOf("."c) + 1), "")
                If (cle = "TotalFacture" OrElse cle = "DejaPayeAvant" OrElse cle = "ResteAvant" OrElse cle = "ResteApres" OrElse cle = "MontantRecu" OrElse cle = "MonnaieRendue" OrElse cle = "MontantAffecte") AndAlso snapshot.TryGetValue(prefixe & "DeviseMontants", devise) AndAlso Decimal.TryParse(valeur, NumberStyles.Number, CultureInfo.InvariantCulture, montant) Then Return montant.ToString("N2") & " " & devise
                If champ = "MontantRecuOrigine" AndAlso snapshot.TryGetValue("DeviseSaisie", devise) AndAlso Decimal.TryParse(valeur, NumberStyles.Number, CultureInfo.InvariantCulture, montant) Then Return montant.ToString("0.########") & " " & devise
                Return valeur
            End If
            Dim id As Integer
            If Not Integer.TryParse(valeur, NumberStyles.Integer, CultureInfo.InvariantCulture, id) Then Return valeur
            ' Le nom instantane prime sur le compte actuel : un renommage ne
            ' reattribue pas une operation historique. L'ID demeure visible.
            Dim nom As String = Nothing
            If Not snapshot.TryGetValue(champ & "Nom", nom) OrElse String.IsNullOrWhiteSpace(nom) OrElse nom = "Non renseigné" Then
                noms.TryGetValue(id, nom)
                If Not String.IsNullOrWhiteSpace(nom) Then nom &= " (nom actuel du compte)"
            End If
            Return If(String.IsNullOrWhiteSpace(nom), "Compte indisponible", nom) & " [ID " & id.ToString() & "]"
        End Function

        Public Shared Function Presenter(avant As Object, apres As Object, noms As IDictionary(Of Integer, String), Optional toutesValeurs As Boolean = False) As List(Of AuditDifference)
            Dim a As IDictionary(Of String, String) = AuditDifferenceService.Valeurs(avant)
            Dim n As IDictionary(Of String, String) = AuditDifferenceService.Valeurs(apres)
            Dim source As List(Of AuditDifference) = AuditDifferenceService.Comparer(If(toutesValeurs, Nothing, avant), apres)
            Dim result As New List(Of AuditDifference)()
            For Each d As AuditDifference In source
                If d.Champ.EndsWith("ParNom", StringComparison.Ordinal) Then Continue For
                If d.Champ = "Empreinte" OrElse d.Champ = "ChampPrix" Then Continue For
                result.Add(New AuditDifference With {.Champ = Libelle(d.Champ, n),
                    .Avant = AfficherValeur(d.Champ, d.Avant, a, noms), .Apres = AfficherValeur(d.Champ, d.Apres, n, noms)})
            Next
            Return result
        End Function

        Public Shared Function Evenement(code As String) As String
            Select Case code
                Case "FACTURE_CREEE" : Return "Facture créée"
                Case "FACTURE_MODIFIEE" : Return "Facture modifiée"
                Case "FACTURE_ANNULEE" : Return "Facture annulée"
                Case "ENCAISSEMENT_VALIDE" : Return "Encaissement validé"
                Case "CAISSE_REGULARISEE", "CAISSE_ECART_REGULARISE" : Return "Régularisation de caisse"
                Case "PRIX_MODIFICATION_APPLIQUEE" : Return "Tarif officiel modifié"
                Case "PRIX_MODIFICATION_DEMANDEE" : Return "Modification de tarif demandée"
                Case "PRIX_MODIFICATION_APPROUVEE" : Return "Modification de tarif approuvée"
                Case "PRIX_MODIFICATION_REFUSEE" : Return "Modification de tarif refusée"
                Case "FACTURE_PRIX_MODIFIE" : Return "Prix exceptionnel sur une facture"
                Case Else : Return If(code, "Opération historique").Replace("_", " ")
            End Select
        End Function
    End Class
End Namespace
