Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class SessionRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Sub AssurerTable()
            ' Schéma géré par le script SQL de déploiement.
        End Sub

        Public Function DemarrerSession(utilisateurId As Integer) As Integer
            AssurerTable()
            Dim sql As String = "INSERT INTO UtilisateurSessions (UtilisateurId) VALUES (@UtilisateurId); SELECT CAST(SCOPE_IDENTITY() AS INT);"
            Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                New System.Data.SqlClient.SqlParameter("@UtilisateurId", utilisateurId)
            }
            Dim id As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(id)
        End Function

        Public Sub Ping(sessionId As Integer)
            Dim sql As String = "UPDATE UtilisateurSessions SET DernierPing=SYSUTCDATETIME() WHERE SessionId=@SessionId"
            Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                New System.Data.SqlClient.SqlParameter("@SessionId", sessionId)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Public Sub FermerSession(sessionId As Integer)
            Dim sql As String = "UPDATE UtilisateurSessions SET Fin=SYSUTCDATETIME() WHERE SessionId=@SessionId"
            Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                New System.Data.SqlClient.SqlParameter("@SessionId", sessionId)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Public Function ListerConnectes() As DataTable
            Dim sql As String = "SELECT s.SessionId, u.NomUtilisateur, s.DernierPing " &
                                "FROM UtilisateurSessions s JOIN Utilisateurs u ON u.UtilisateurId = s.UtilisateurId " &
                                "WHERE s.Fin IS NULL AND s.DernierPing >= DATEADD(SECOND,-10,SYSUTCDATETIME())"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function
    End Class
End Namespace
