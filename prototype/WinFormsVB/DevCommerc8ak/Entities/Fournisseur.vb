Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class Fournisseur
        Public Property FournisseurId As Integer
        Public Property NomFournisseur As String
        Public Property Telephone As String
        Public Property Email As String
        Public Property Adresse As String
        Public Property EstActif As Boolean
    End Class
End Namespace
