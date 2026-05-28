Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class FactureVenteDTO
        Public Property FactureVenteId As Integer
        Public Property NumeroFacture As String
        Public Property ClientId As Integer?
        Public Property MontantTotal As Decimal
        Public Property Statut As String
        Public Property CreeLe As Date
        Public Property ValideLe As Date?
    End Class
End Namespace
