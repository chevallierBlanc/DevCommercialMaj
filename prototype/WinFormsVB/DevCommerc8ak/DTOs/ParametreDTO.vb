Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class ParametreDTO
        Public Property RemiseMaxPourcent As Decimal
        Public Property SeuilStockCritique As Decimal
        Public Property AlerteExpirationJours As Integer
        Public Property ImprimanteA4 As String
        Public Property ImprimanteTicket As String
        Public Property DeviseParDefaut As String
        Public Property TauxUsd As Decimal
        Public Property ScannerIp As String
        Public Property ScannerPort As Integer
        Public Property ScannerActif As Boolean
        Public Property NomMagasin As String
        Public Property AdresseMagasin As String
        Public Property TelephoneMagasin As String
        Public Property ModeSombre As Boolean
        Public Property LogoPath As String
        Public Property ApercuAvantImpression As Boolean
        Public Property ImpressionCouleur As Boolean
    End Class
End Namespace
