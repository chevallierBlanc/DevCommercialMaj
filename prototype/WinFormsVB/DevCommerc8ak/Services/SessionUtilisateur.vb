Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Module SessionUtilisateur
        Public Property UtilisateurId As Integer
        Public Property NomUtilisateur As String
        Public Property Role As String
        Public Property RoleIdActif As Integer
        Public Property NomRoleActif As String
        Public Property SessionId As Integer
        Public Property DateConnexion As Date
        Public Property Poste As String

        Public Sub Reinitialiser()
            UtilisateurId = 0
            NomUtilisateur = String.Empty
            Role = String.Empty
            RoleIdActif = 0
            NomRoleActif = String.Empty
            SessionId = 0
            DateConnexion = Date.MinValue
            Poste = String.Empty
        End Sub
    End Module
End Namespace
