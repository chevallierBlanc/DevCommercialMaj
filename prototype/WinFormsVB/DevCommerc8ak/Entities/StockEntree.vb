Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class StockEntree
        Public Property StockEntreeId As Integer
        Public Property IdStock As String
        Public Property ProduitId As Integer
        Public Property QuantiteSaisie As Decimal
        Public Property Unite As String
        Public Property QuantiteBase As Decimal
        Public Property PrixAchat As Decimal
        Public Property Devise As String
        Public Property Taux As Decimal
        Public Property DateEntree As Date
        Public Property FournisseurId As Integer?
        Public Property CreePar As Integer?
    End Class
End Namespace
