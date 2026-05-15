Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class BonApprovisionnementLigne
        Public Property BonLigneId As Integer
        Public Property BonId As Integer
        Public Property ProduitId As Integer
        Public Property Quantite As Decimal
    End Class
End Namespace
