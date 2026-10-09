Option Strict On
Option Explicit On
Imports System

Namespace DevCommerc8ak
    Public NotInheritable Class DelegationPrixRegles
        Private Sub New()
        End Sub
        Public Shared Sub VerifierPrix(prix As Decimal)
            If prix <= 0D OrElse prix > 9999999999999999.99D OrElse Decimal.Round(prix, 2) <> prix Then Throw New ArgumentException("Saisissez un prix positif avec au maximum deux décimales.")
        End Sub
        Public Shared Sub VerifierTotauxException(sommeLignes As Decimal, sousTotal As Decimal, remise As Decimal, taxe As Decimal, total As Decimal)
            ' Un prix exceptionnel ne doit pas permettre de conserver un ancien
            ' total d'en-tete. On refuse l'incoherence sans recalcul silencieux.
            If sommeLignes <> sousTotal OrElse total <> sousTotal - remise + taxe Then Throw New ArgumentException("Les totaux de la facture ne correspondent pas aux articles à prix exceptionnel.")
        End Sub
        Public Shared Function NecessiteApprobation(ancien As Decimal, nouveau As Decimal, coutComparable As Decimal, variationMax As Decimal, margeMin As Decimal) As Boolean
            VerifierPrix(nouveau)
            If ancien <= 0D OrElse coutComparable <= 0D Then Throw New InvalidOperationException("Prix ou coût de référence indisponible : délégation refusée.")
            Return Math.Abs(nouveau - ancien) / ancien * 100D > variationMax OrElse nouveau < coutComparable * (1D + margeMin / 100D)
        End Function
    End Class
End Namespace
