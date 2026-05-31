Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class InventaireLigne
        Public Property LigneInventaireId As Integer
        Public Property InventaireId As Integer
        Public Property ProduitId As Integer
        Public Property StockTheorique As Decimal
        Public Property StockPhysique As Decimal?
        Public Property Ecart As Decimal?
        Public Property Statut As String
        Public Property Motif As String
        Public Property DateComptage As Date?
        Public Property CreeLe As Date
        Public Property ModifieLe As Date?
    End Class
End Namespace
