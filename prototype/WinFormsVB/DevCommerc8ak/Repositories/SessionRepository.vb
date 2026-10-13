Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
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
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    ' Serialiser les ouvertures pour ce compte, sans bloquer les
                    ' autres utilisateurs. Relire le role apres son choix dans l'UI.
                    Using compte As New SqlCommand("SELECT UtilisateurId FROM dbo.Utilisateurs WITH(UPDLOCK,HOLDLOCK) WHERE UtilisateurId=@u AND EstActif=1 AND EstVerrouille=0", cn, tx)
                        compte.Parameters.AddWithValue("@u", utilisateurId)
                        If compte.ExecuteScalar() Is Nothing Then Throw New UnauthorizedAccessException("Ce compte n'est plus actif ou est verrouillé.")
                    End Using
                    Dim idRole As Integer
                    Dim nomRole As String
                    Using role As New SqlCommand("SELECT TOP(1) r.RoleId,r.NomRole FROM dbo.UtilisateurRoles ur WITH(HOLDLOCK) JOIN dbo.Roles r WITH(HOLDLOCK) ON r.RoleId=ur.RoleId AND r.EstActif=1 WHERE ur.UtilisateurId=@u AND ur.EstActif=1 AND (@r=0 OR r.RoleId=@r) ORDER BY ur.EstRolePrincipal DESC,r.RoleId", cn, tx)
                        role.Parameters.AddWithValue("@u", utilisateurId)
                        role.Parameters.AddWithValue("@r", roleIdActif)
                        Using lecteur As SqlDataReader = role.ExecuteReader()
                            If Not lecteur.Read() Then Throw New UnauthorizedAccessException("Le rôle choisi n'est plus autorisé pour ce compte. Reconnectez-vous.")
                            idRole = CInt(lecteur(0)) : nomRole = CStr(lecteur(1))
                        End Using
                    End Using
                    Using active As New SqlCommand("SELECT COUNT(*) FROM dbo.UtilisateurSessions WHERE UtilisateurId=@u AND Fin IS NULL AND DernierPing>=DATEADD(MINUTE,-30,SYSUTCDATETIME())", cn, tx)
                        active.Parameters.AddWithValue("@u", utilisateurId)
                        If CInt(active.ExecuteScalar()) > 0 Then Throw New InvalidOperationException("Ce compte possède déjà une session active. Fermez-la avant de vous reconnecter.")
                    End Using
                    Using cmd As New SqlCommand("INSERT dbo.UtilisateurSessions(UtilisateurId,RoleIdActif,RoleSession,Poste) VALUES(@u,@r,@nom,@poste); SELECT CAST(SCOPE_IDENTITY() AS int);", cn, tx)
                        cmd.Parameters.AddWithValue("@u", utilisateurId)
                        cmd.Parameters.AddWithValue("@r", idRole)
                        cmd.Parameters.AddWithValue("@nom", nomRole)
                        cmd.Parameters.AddWithValue("@poste", Environment.MachineName)
                        Dim id As Integer = CInt(cmd.ExecuteScalar())
                        AuditMetierService.EnregistrerOuvertureSession(cn, tx, utilisateurId, id, idRole)
                        tx.Commit()
                        Return id
                    End Using
                End Using
            End Using
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
            Dim sql As String = "UPDATE UtilisateurSessions SET DernierPing=SYSUTCDATETIME() WHERE SessionId=@SessionId AND Fin IS NULL AND DernierPing>=DATEADD(MINUTE,-30,SYSUTCDATETIME())"
            Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                New System.Data.SqlClient.SqlParameter("@SessionId", sessionId)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Public Sub FermerSession(sessionId As Integer)
            Dim sql As String = "UPDATE UtilisateurSessions SET Fin=SYSUTCDATETIME() WHERE SessionId=@SessionId AND Fin IS NULL"
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
