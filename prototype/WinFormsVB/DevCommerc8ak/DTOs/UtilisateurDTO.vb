Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class UtilisateurDTO
        Public Property UtilisateurId As Integer
        Public Property NomUtilisateur As String
        Public Property EstActif As Boolean
        Public Property Role As String
    End Class
End Namespace
