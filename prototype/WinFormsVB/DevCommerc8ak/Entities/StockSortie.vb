Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class StockSortie
        Public Property StockSortieId As Integer
        Public Property ProduitId As Integer
        Public Property QuantiteSaisie As Decimal
        Public Property Unite As String
        Public Property QuantiteBase As Decimal
        Public Property DateSortie As Date
        Public Property Source As String
        Public Property RefSource As String
        Public Property CreePar As Integer?
    End Class
End Namespace
