Option Strict On
Namespace DevCommerc8ak
    Public Class ReglementComptoir
        Public Property Devise As String = "FC"
        Public Property MontantOrigine As Decimal
        Public Property Taux As Decimal = 1D
        Public Property Moyen As String = "CASH"
        Public Property Reference As String = ""
        Public Property ConfirmationExterne As Boolean
    End Class
End Namespace
