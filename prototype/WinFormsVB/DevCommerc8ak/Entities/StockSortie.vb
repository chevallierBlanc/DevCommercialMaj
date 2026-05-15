Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class StockSortie
        Public Property StockSortieId As Integer
        Public Property NumeroSortie As String
        Public Property ProduitId As Integer
        Public Property QuantiteSaisie As Decimal
        Public Property Unite As String
        Public Property QuantiteBase As Decimal
        Public Property DateSortie As Date
        Public Property Source As String
        Public Property RefSource As String
        Public Property CreePar As Integer?
        Public Property ClientId As Integer?
        Public Property MotifId As Integer?
        Public Property TypeVente As String
        Public Property PrixUnitaire As Decimal?
        Public Property MontantLigne As Decimal?
        Public Property StatutPaiement As String
        Public Property MontantPaye As Decimal?
        Public Property ResteAPayer As Decimal?
        Public Property Observation As String
    End Class
End Namespace
