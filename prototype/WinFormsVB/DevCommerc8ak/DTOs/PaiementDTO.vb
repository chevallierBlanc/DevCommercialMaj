Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class PaiementDTO
        Public Property PaiementId As Integer
        Public Property FactureVenteId As Integer
        Public Property ModePaiement As String
        Public Property Montant As Decimal
        Public Property MontantRecu As Decimal
        Public Property MonnaieRendue As Decimal
        Public Property Devise As String
        Public Property PayeLe As Date
    End Class
End Namespace
