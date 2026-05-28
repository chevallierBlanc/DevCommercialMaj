Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class LigneFactureVenteDTO
        Public Property LigneFactureVenteId As Integer
        Public Property FactureVenteId As Integer
        Public Property ProduitId As Integer
        Public Property Quantite As Decimal
        Public Property PrixUnitaire As Decimal
        Public Property MontantLigne As Decimal
    End Class
End Namespace
