Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class StockPerte
        Public Property StockPerteId As Integer
        Public Property ProduitId As Integer
        Public Property QuantiteSaisie As Decimal
        Public Property Unite As String
        Public Property QuantiteBase As Decimal
        Public Property TypePerte As String
        Public Property Motif As String
        Public Property DatePerte As Date
        Public Property CreePar As Integer?
    End Class
End Namespace
