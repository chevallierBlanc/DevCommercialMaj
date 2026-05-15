Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class BonApprovisionnementDTO
        Public Property BonId As Integer
        Public Property DateCreation As Date
        Public Property Statut As String
        Public Property FournisseurId As Integer?
        Public Property TypePaiement As String
    End Class
End Namespace
