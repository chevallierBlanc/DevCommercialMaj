Option Strict On
Option Explicit On
Imports System
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class ContexteCommercialPrix
        Public Property SousTotal As Decimal
        Public Property Remise As Decimal
    End Class
    Public NotInheritable Class DelegationPrixRegles
        Private Sub New()
        End Sub
        Public Shared Function InstantaneArticles(Of T)(articles As IEnumerable(Of T)) As List(Of T)
            ' La collection liee ne doit pas changer sous le CurrencyManager.
            Return New List(Of T)(articles)
        End Function
        Public Shared Function ChoisirArticle(Of T As Class)(articles As IList(Of T), selection As T) As T
            If selection IsNot Nothing Then
                For Each article As T In articles
                    If Object.ReferenceEquals(article, selection) Then Return article
                Next
            End If
            If articles.Count = 1 Then Return articles(0)
            Return Nothing
        End Function

        Public Shared Function AutoriseCommande(code As String, active As Boolean, demandes As Boolean, immediate As Boolean, droits As ISet(Of String)) As Boolean
            If Not active OrElse Not droits.Contains("PRIX_CONSULTER") OrElse Not droits.Contains(code) Then Return False
            Select Case code
                Case "PRIX_DEMANDER", "PRIX_DEMANDER_FACTURE" : Return demandes
                Case "PRIX_TARIF_MODIFIER", "PRIX_FACTURE_EXCEPTION" : Return immediate
                Case Else : Return False
            End Select
        End Function
        Public Shared Function CommandeDisponible(actions As ISet(Of String), perimetre As String, selectionValide As Boolean) As Boolean
            If Not selectionValide OrElse actions Is Nothing Then Return False
            Select Case perimetre
                Case "TARIF" : Return actions.Contains("PRIX_TARIF_MODIFIER") OrElse actions.Contains("PRIX_DEMANDER")
                Case "FACTURE" : Return actions.Contains("PRIX_FACTURE_EXCEPTION") OrElse actions.Contains("PRIX_DEMANDER_FACTURE")
                Case Else : Return False
            End Select
        End Function
        Public Shared Sub VerifierPrix(prix As Decimal)
            If prix <= 0D OrElse prix > 9999999999999999.99D OrElse Decimal.Round(prix, 2) <> prix Then Throw New ArgumentException("Saisissez un prix positif avec au maximum deux décimales.")
        End Sub
        Public Shared Sub VerifierPagination(page As Integer, taille As Integer)
            If page < 0 OrElse page > 1000000 OrElse taille < 1 OrElse taille > 100 Then Throw New ArgumentException("Pagination invalide.")
        End Sub
        Public Shared Sub VerifierContexte(contexte As Guid, quantite As Decimal)
            If contexte = Guid.Empty OrElse quantite <= 0D OrElse Decimal.Round(quantite, 8) <> quantite Then Throw New ArgumentException("Un article identifié et une quantité positive de huit décimales maximum sont requis.")
        End Sub
        Public Shared Sub VerifierDuree(heures As Integer)
            If heures < 1 OrElse heures > 168 Then Throw New ArgumentException("La validité doit être comprise entre 1 et 168 heures.")
        End Sub
        Public Shared Function PrixNet(prix As Decimal, contexte As ContexteCommercialPrix) As Decimal
            ' Comparaison d'autorisation uniquement : la remise deja calculee
            ' est repartie au prorata, sans changer le calcul ni les montants factures.
            If contexte Is Nothing OrElse contexte.SousTotal <= 0D OrElse contexte.Remise < 0D OrElse contexte.Remise > contexte.SousTotal OrElse Decimal.Round(contexte.SousTotal, 8) <> contexte.SousTotal OrElse Decimal.Round(contexte.Remise, 8) <> contexte.Remise Then Throw New ArgumentException("Contexte de remise invalide.")
            Return prix * (1D - contexte.Remise / contexte.SousTotal)
        End Function
        Public Shared Sub VerifierTotauxException(sommeLignes As Decimal, sousTotal As Decimal, remise As Decimal, taxe As Decimal, total As Decimal, Optional sommeArrondie As Decimal? = Nothing)
            ' Un prix exceptionnel ne doit pas permettre de conserver un ancien
            ' total d'en-tete. On refuse l'incoherence sans recalcul silencieux.
            ' Les colonnes monetaires existantes stockent deux decimales. Une
            ' edition peut relire les articles arrondis individuellement ; on
            ' accepte ces deux representations exactes, sans changer les montants.
            Dim attendu As Decimal = Decimal.Round(sommeLignes, 2, MidpointRounding.AwayFromZero)
            Dim saisi As Decimal = Decimal.Round(sousTotal, 2, MidpointRounding.AwayFromZero)
            If (attendu <> saisi AndAlso (Not sommeArrondie.HasValue OrElse sommeArrondie.Value <> saisi)) OrElse Decimal.Round(total, 2, MidpointRounding.AwayFromZero) <> Decimal.Round(sousTotal - remise + taxe, 2, MidpointRounding.AwayFromZero) Then Throw New ArgumentException("Les totaux de la facture ne correspondent pas aux articles à prix exceptionnel.")
        End Sub
        Public Shared Function NecessiteApprobation(ancien As Decimal, nouveau As Decimal, coutComparable As Decimal, variationMax As Decimal, margeMin As Decimal) As Boolean
            VerifierPrix(nouveau)
            If ancien <= 0D OrElse coutComparable <= 0D Then Throw New InvalidOperationException("Prix ou coût de référence indisponible : délégation refusée.")
            Return Math.Abs(nouveau - ancien) / ancien * 100D > variationMax OrElse nouveau < coutComparable * (1D + margeMin / 100D)
        End Function
    End Class
End Namespace
