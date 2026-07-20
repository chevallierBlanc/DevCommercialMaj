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
            Dim sql As String =
                "IF OBJECT_ID('dbo.UtilisateurSessions', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.UtilisateurSessions (" &
                "SessionId INT IDENTITY(1,1) PRIMARY KEY, " &
                "UtilisateurId INT NOT NULL, " &
                "RoleIdActif INT NULL, " &
                "RoleSession NVARCHAR(80) NULL, " &
                "Debut DATETIME2 NOT NULL CONSTRAINT DF_UtilisateurSessions_Debut DEFAULT(SYSUTCDATETIME()), " &
                "DernierPing DATETIME2 NOT NULL CONSTRAINT DF_UtilisateurSessions_DernierPing DEFAULT(SYSUTCDATETIME()), " &
                "Fin DATETIME2 NULL, " &
                "Poste NVARCHAR(100) NULL); " &
                "END " &
                "IF COL_LENGTH('dbo.UtilisateurSessions', 'RoleIdActif') IS NULL ALTER TABLE dbo.UtilisateurSessions ADD RoleIdActif INT NULL; " &
                "IF COL_LENGTH('dbo.UtilisateurSessions', 'RoleSession') IS NULL ALTER TABLE dbo.UtilisateurSessions ADD RoleSession NVARCHAR(80) NULL; " &
                "IF COL_LENGTH('dbo.UtilisateurSessions', 'Poste') IS NULL ALTER TABLE dbo.UtilisateurSessions ADD Poste NVARCHAR(100) NULL;"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub

        Public Function DemarrerSession(utilisateurId As Integer) As Integer
            Return DemarrerSession(utilisateurId, 0, String.Empty)
        End Function

        Public Function DemarrerSession(utilisateurId As Integer, roleIdActif As Integer, roleSession As String) As Integer
            AssurerTable()
            Dim sql As String = "INSERT INTO UtilisateurSessions (UtilisateurId, RoleIdActif, RoleSession, Poste) VALUES (@UtilisateurId, @RoleIdActif, @RoleSession, @Poste); SELECT CAST(SCOPE_IDENTITY() AS INT);"
            Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                New System.Data.SqlClient.SqlParameter("@UtilisateurId", utilisateurId),
                New System.Data.SqlClient.SqlParameter("@RoleIdActif", If(roleIdActif > 0, CType(roleIdActif, Object), DBNull.Value)),
                New System.Data.SqlClient.SqlParameter("@RoleSession", If(String.IsNullOrWhiteSpace(roleSession), CType(DBNull.Value, Object), roleSession.Trim())),
                New System.Data.SqlClient.SqlParameter("@Poste", Environment.MachineName)
            }
            Dim id As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(id)
        End Function

        Public Function UtilisateurDejaConnecte(utilisateurId As Integer) As Boolean
            AssurerTable()
            Dim sql As String = "SELECT COUNT(*) FROM dbo.UtilisateurSessions WHERE UtilisateurId=@UtilisateurId AND Fin IS NULL AND DernierPing >= DATEADD(MINUTE,-30,SYSUTCDATETIME())"
            Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                New System.Data.SqlClient.SqlParameter("@UtilisateurId", utilisateurId)
            }
            Return Convert.ToInt32(_dal.ExecuterScalaire(sql, CommandType.Text, p)) > 0
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
            Dim sql As String = "SELECT s.SessionId, u.NomUtilisateur, ISNULL(s.RoleSession,'') AS RoleSession, s.DernierPing " &
                                "FROM UtilisateurSessions s JOIN Utilisateurs u ON u.UtilisateurId = s.UtilisateurId " &
                                "WHERE s.Fin IS NULL AND s.DernierPing >= DATEADD(SECOND,-10,SYSUTCDATETIME())"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function
    End Class
End Namespace
