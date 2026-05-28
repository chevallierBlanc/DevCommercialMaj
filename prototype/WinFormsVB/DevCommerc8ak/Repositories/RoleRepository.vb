Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class RoleRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        ' Cree un role si absent.
        Public Sub AssurerRole(nomRole As String)
            Dim sql As String = "IF NOT EXISTS (SELECT 1 FROM Roles WHERE NomRole = @NomRole) " &
                                "INSERT INTO Roles (NomRole) VALUES (@NomRole);"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@NomRole", nomRole)}
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        ' Retourne l'Id du role par nom.
        Public Function ObtenirIdParNom(nomRole As String) As Integer
            Dim sql As String = "SELECT RoleId FROM Roles WHERE NomRole = @NomRole"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@NomRole", nomRole)}
            Dim id As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(id)
        End Function
    End Class
End Namespace
