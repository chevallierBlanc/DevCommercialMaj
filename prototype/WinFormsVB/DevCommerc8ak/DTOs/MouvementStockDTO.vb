Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class MouvementStockDTO
        Public Property MouvementStockId As Integer
        Public Property NumeroMouvement As String
        Public Property ProduitId As Integer
        Public Property TypeMouvement As String
        Public Property Quantite As Decimal
        Public Property QuantiteBase As Decimal
        Public Property Unite As String
        Public Property StockAvant As Decimal
        Public Property StockApres As Decimal
        Public Property Reference As String
        Public Property Observation As String
        Public Property TypePerte As String
        Public Property EffectueLe As Date
        Public Property EstAnnule As Boolean
        Public Property AnnulePar As Integer?
        Public Property AnnuleLe As Date?
        Public Property AnnulationRef As String
    End Class
End Namespace
