Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class StockInventaire
        Public Property StockInventaireId As Integer
        Public Property ProduitId As Integer
        Public Property StockTheorique As Decimal
        Public Property StockReel As Decimal
        Public Property Ecart As Decimal
        Public Property DateInventaire As Date
        Public Property CreePar As Integer?
        Public Property Observation As String
    End Class
End Namespace
