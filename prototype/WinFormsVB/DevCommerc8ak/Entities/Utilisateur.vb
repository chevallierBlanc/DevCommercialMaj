Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class Utilisateur
        Public Property UtilisateurId As Integer
        Public Property NomUtilisateur As String
        Public Property MotDePasseHash As Byte()
        Public Property MotDePasseSel As Byte()
        Public Property EstActif As Boolean
        Public Property CreeLe As Date
    End Class
End Namespace
