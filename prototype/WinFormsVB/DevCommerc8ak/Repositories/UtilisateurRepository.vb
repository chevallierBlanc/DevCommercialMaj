Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class UtilisateurRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        ' Cree un utilisateur et retourne son identifiant.
        Public Function Ajouter(utilisateur As Utilisateur, roleId As Integer) As Integer
            Dim sql As String = "INSERT INTO Utilisateurs (NomUtilisateur, MotDePasseHash, MotDePasseSel, EstActif) " &
                                "VALUES (@NomUtilisateur, @MotDePasseHash, @MotDePasseSel, @EstActif); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"

            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@NomUtilisateur", utilisateur.NomUtilisateur),
                New SqlParameter("@MotDePasseHash", utilisateur.MotDePasseHash),
                New SqlParameter("@MotDePasseSel", utilisateur.MotDePasseSel),
                New SqlParameter("@EstActif", utilisateur.EstActif)
            }

            Dim id As Integer = Convert.ToInt32(_dal.ExecuterScalaire(sql, CommandType.Text, p))
            AssignerRole(id, roleId)
            Return id
        End Function

        ' Assigne un role a un utilisateur.
        Public Sub AssignerRole(utilisateurId As Integer, roleId As Integer)
            AssurerUtilisateurRolesInfrastructure()
            Dim sql As String = "IF NOT EXISTS (SELECT 1 FROM UtilisateurRoles WHERE UtilisateurId=@UtilisateurId AND RoleId=@RoleId) " &
                                "INSERT INTO UtilisateurRoles (UtilisateurId, RoleId, EstActif, EstRolePrincipal, CreePar) VALUES (@UtilisateurId, @RoleId, 1, 0, @CreePar) " &
                                "ELSE UPDATE UtilisateurRoles SET EstActif=1, ModifieLe=GETDATE(), ModifiePar=@CreePar WHERE UtilisateurId=@UtilisateurId AND RoleId=@RoleId;"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@UtilisateurId", utilisateurId),
                New SqlParameter("@RoleId", roleId),
                New SqlParameter("@CreePar", If(String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur), CType(DBNull.Value, Object), SessionUtilisateur.NomUtilisateur))
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        ' Met a jour le compte et le role associe.
        Public Sub MettreAJour(utilisateurId As Integer, nomUtilisateur As String, estActif As Boolean, roleId As Integer, Optional hash As Byte() = Nothing, Optional sel As Byte() = Nothing)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        Dim sqlUpdate As String = "UPDATE Utilisateurs SET NomUtilisateur=@NomUtilisateur, EstActif=@EstActif"
                        If hash IsNot Nothing AndAlso sel IsNot Nothing Then
                            sqlUpdate &= ", MotDePasseHash=@MotDePasseHash, MotDePasseSel=@MotDePasseSel"
                        End If
                        sqlUpdate &= " WHERE UtilisateurId=@UtilisateurId"

                        Using cmdUpdate As New SqlCommand(sqlUpdate, cn, tx)
                            cmdUpdate.Parameters.AddWithValue("@NomUtilisateur", nomUtilisateur)
                            cmdUpdate.Parameters.AddWithValue("@EstActif", estActif)
                            cmdUpdate.Parameters.AddWithValue("@UtilisateurId", utilisateurId)
                            If hash IsNot Nothing AndAlso sel IsNot Nothing Then
                                cmdUpdate.Parameters.AddWithValue("@MotDePasseHash", hash)
                                cmdUpdate.Parameters.AddWithValue("@MotDePasseSel", sel)
                            End If
                            cmdUpdate.ExecuteNonQuery()
                        End Using

                        EnregistrerRolesUtilisateurTransaction(cn, tx, utilisateurId, New List(Of Integer) From {roleId}, roleId)

                        tx.Commit()
                    Catch
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        ' Recupere un utilisateur par nom.
        Public Function ObtenirParNom(nomUtilisateur As String) As Utilisateur
            Dim sql As String = "SELECT UtilisateurId, NomUtilisateur, MotDePasseHash, MotDePasseSel, EstActif, CreeLe " &
                                "FROM Utilisateurs WHERE NomUtilisateur = @NomUtilisateur"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@NomUtilisateur", nomUtilisateur)}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            If dt.Rows.Count = 0 Then Return Nothing

            Dim row As DataRow = dt.Rows(0)
            Return New Utilisateur With {
                .UtilisateurId = Convert.ToInt32(row("UtilisateurId")),
                .NomUtilisateur = Convert.ToString(row("NomUtilisateur")),
                .MotDePasseHash = CType(row("MotDePasseHash"), Byte()),
                .MotDePasseSel = CType(row("MotDePasseSel"), Byte()),
                .EstActif = Convert.ToBoolean(row("EstActif")),
                .CreeLe = Convert.ToDateTime(row("CreeLe"))
            }
        End Function

        ' Liste des utilisateurs avec role.
        Public Function Lister() As List(Of UtilisateurDTO)
            AssurerUtilisateurRolesInfrastructure()
            Dim sql As String = "" &
                "SELECT u.UtilisateurId, u.NomUtilisateur, u.EstActif, " &
                "STUFF((SELECT ', ' + r2.NomRole " &
                "       FROM UtilisateurRoles ur2 " &
                "       INNER JOIN Roles r2 ON r2.RoleId = ur2.RoleId " &
                "       WHERE ur2.UtilisateurId = u.UtilisateurId AND ISNULL(ur2.EstActif,1)=1 " &
                "       ORDER BY ISNULL(ur2.EstRolePrincipal,0) DESC, r2.NomRole " &
                "       FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 2, '') AS NomRole " &
                "FROM Utilisateurs u"
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, Nothing)
            Dim liste As New List(Of UtilisateurDTO)()

            For Each row As DataRow In dt.Rows
                liste.Add(New UtilisateurDTO With {
                    .UtilisateurId = Convert.ToInt32(row("UtilisateurId")),
                    .NomUtilisateur = Convert.ToString(row("NomUtilisateur")),
                    .EstActif = Convert.ToBoolean(row("EstActif")),
                    .Role = If(row.IsNull("NomRole"), "", Convert.ToString(row("NomRole")))
                })
            Next
            Return liste
        End Function

        ' Met a jour l'etat actif.
        Public Sub MettreAJourActif(utilisateurId As Integer, estActif As Boolean)
            Dim sql As String = "UPDATE Utilisateurs SET EstActif=@EstActif WHERE UtilisateurId=@UtilisateurId"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@EstActif", estActif),
                New SqlParameter("@UtilisateurId", utilisateurId)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        ' Met a jour le mot de passe.
        Public Sub MettreAJourMotDePasse(utilisateurId As Integer, hash As Byte(), sel As Byte())
            Dim sql As String = "UPDATE Utilisateurs SET MotDePasseHash=@MotDePasseHash, MotDePasseSel=@MotDePasseSel WHERE UtilisateurId=@UtilisateurId"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@MotDePasseHash", hash),
                New SqlParameter("@MotDePasseSel", sel),
                New SqlParameter("@UtilisateurId", utilisateurId)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        ' Retourne le role d'un utilisateur.
        Public Function ObtenirRole(utilisateurId As Integer) As String
            AssurerUtilisateurRolesInfrastructure()
            Dim sql As String = "SELECT TOP 1 r.NomRole FROM Roles r " &
                                "JOIN UtilisateurRoles ur ON r.RoleId = ur.RoleId " &
                                "WHERE ur.UtilisateurId = @UtilisateurId AND ISNULL(ur.EstActif,1)=1 AND ISNULL(r.EstActif,1)=1 " &
                                "ORDER BY ISNULL(ur.EstRolePrincipal,0) DESC, r.NomRole"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@UtilisateurId", utilisateurId)}
            Dim role As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return If(role Is Nothing, "", Convert.ToString(role))
        End Function

        Public Function EstDansRole(utilisateurId As Integer, nomRole As String) As Boolean
            AssurerUtilisateurRolesInfrastructure()
            Dim sql As String =
                "SELECT COUNT(*) " &
                "FROM dbo.UtilisateurRoles ur " &
                "INNER JOIN dbo.Roles r ON r.RoleId = ur.RoleId " &
                "WHERE ur.UtilisateurId=@UtilisateurId " &
                "AND ISNULL(ur.EstActif,1)=1 " &
                "AND UPPER(LTRIM(RTRIM(r.NomRole)))=@NomRole"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@UtilisateurId", utilisateurId),
                New SqlParameter("@NomRole", nomRole.Trim().ToUpperInvariant())
            }
            Dim resultat As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return resultat IsNot Nothing AndAlso Convert.ToInt32(resultat) > 0
        End Function

        Public Function ListerRolesActifs(utilisateurId As Integer) As List(Of RoleSessionInfo)
            AssurerUtilisateurRolesInfrastructure()
            Dim sql As String =
                "SELECT r.RoleId, r.NomRole, ISNULL(ur.EstRolePrincipal,0) AS EstRolePrincipal " &
                "FROM dbo.UtilisateurRoles ur " &
                "INNER JOIN dbo.Roles r ON r.RoleId = ur.RoleId " &
                "WHERE ur.UtilisateurId=@UtilisateurId AND ISNULL(ur.EstActif,1)=1 AND ISNULL(r.EstActif,1)=1 " &
                "ORDER BY ISNULL(ur.EstRolePrincipal,0) DESC, r.NomRole"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@UtilisateurId", utilisateurId)}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            Dim roles As New List(Of RoleSessionInfo)()
            For Each row As DataRow In dt.Rows
                roles.Add(New RoleSessionInfo With {
                    .RoleId = Convert.ToInt32(row("RoleId")),
                    .NomRole = Convert.ToString(row("NomRole")),
                    .EstRolePrincipal = Convert.ToBoolean(row("EstRolePrincipal"))
                })
            Next
            Return roles
        End Function

        Public Sub MettreAJourRolesUtilisateur(utilisateurId As Integer, roleIds As IEnumerable(Of Integer), rolePrincipalId As Integer)
            AssurerUtilisateurRolesInfrastructure()
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        EnregistrerRolesUtilisateurTransaction(cn, tx, utilisateurId, roleIds, rolePrincipalId)
                        tx.Commit()
                    Catch
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        Private Sub EnregistrerRolesUtilisateurTransaction(cn As SqlConnection, tx As SqlTransaction, utilisateurId As Integer, roleIds As IEnumerable(Of Integer), rolePrincipalId As Integer)
            Dim sourceRoles As IEnumerable(Of Integer) = roleIds
            If sourceRoles Is Nothing Then
                sourceRoles = New List(Of Integer)()
            End If
            Dim ids As New HashSet(Of Integer)(sourceRoles)
            If rolePrincipalId > 0 Then
                ids.Add(rolePrincipalId)
            End If
            If ids.Count = 0 Then
                Throw New InvalidOperationException("Au moins un rôle actif est obligatoire.")
            End If

            Using cmdDisable As New SqlCommand("UPDATE dbo.UtilisateurRoles SET EstActif=0, EstRolePrincipal=0, ModifieLe=GETDATE(), ModifiePar=@ModifiePar WHERE UtilisateurId=@UtilisateurId", cn, tx)
                cmdDisable.Parameters.AddWithValue("@UtilisateurId", utilisateurId)
                cmdDisable.Parameters.AddWithValue("@ModifiePar", If(String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur), CType(DBNull.Value, Object), SessionUtilisateur.NomUtilisateur))
                cmdDisable.ExecuteNonQuery()
            End Using

            For Each id As Integer In ids
                Using cmd As New SqlCommand("" &
                    "IF EXISTS (SELECT 1 FROM dbo.UtilisateurRoles WHERE UtilisateurId=@UtilisateurId AND RoleId=@RoleId) " &
                    "UPDATE dbo.UtilisateurRoles SET EstActif=1, EstRolePrincipal=@Principal, ModifieLe=GETDATE(), ModifiePar=@ModifiePar WHERE UtilisateurId=@UtilisateurId AND RoleId=@RoleId " &
                    "ELSE INSERT INTO dbo.UtilisateurRoles (UtilisateurId, RoleId, EstActif, EstRolePrincipal, CreePar) VALUES (@UtilisateurId, @RoleId, 1, @Principal, @ModifiePar)", cn, tx)
                    cmd.Parameters.AddWithValue("@UtilisateurId", utilisateurId)
                    cmd.Parameters.AddWithValue("@RoleId", id)
                    cmd.Parameters.AddWithValue("@Principal", id = rolePrincipalId)
                    cmd.Parameters.AddWithValue("@ModifiePar", If(String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur), CType(DBNull.Value, Object), SessionUtilisateur.NomUtilisateur))
                    cmd.ExecuteNonQuery()
                End Using
            Next
        End Sub

        Private Sub AssurerUtilisateurRolesInfrastructure()
            Dim sql As String =
                "IF OBJECT_ID('dbo.UtilisateurRoles', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.UtilisateurRoles (UtilisateurRoleId INT IDENTITY(1,1) PRIMARY KEY, UtilisateurId INT NOT NULL, RoleId INT NOT NULL, EstActif BIT NOT NULL CONSTRAINT DF_UtilisateurRoles_EstActif DEFAULT(1), EstRolePrincipal BIT NOT NULL CONSTRAINT DF_UtilisateurRoles_EstRolePrincipal DEFAULT(0), CreeLe DATETIME2 NOT NULL CONSTRAINT DF_UtilisateurRoles_CreeLe DEFAULT(GETDATE()), CreePar NVARCHAR(80) NULL, ModifieLe DATETIME2 NULL, ModifiePar NVARCHAR(80) NULL); " &
                "CREATE UNIQUE INDEX UX_UtilisateurRoles_Utilisateur_Role ON dbo.UtilisateurRoles(UtilisateurId, RoleId); " &
                "END " &
                "IF COL_LENGTH('dbo.UtilisateurRoles', 'EstActif') IS NULL ALTER TABLE dbo.UtilisateurRoles ADD EstActif BIT NOT NULL CONSTRAINT DF_UtilisateurRoles_EstActif2 DEFAULT(1); " &
                "IF COL_LENGTH('dbo.UtilisateurRoles', 'EstRolePrincipal') IS NULL ALTER TABLE dbo.UtilisateurRoles ADD EstRolePrincipal BIT NOT NULL CONSTRAINT DF_UtilisateurRoles_EstRolePrincipal2 DEFAULT(0); " &
                "IF COL_LENGTH('dbo.UtilisateurRoles', 'CreeLe') IS NULL ALTER TABLE dbo.UtilisateurRoles ADD CreeLe DATETIME2 NOT NULL CONSTRAINT DF_UtilisateurRoles_CreeLe2 DEFAULT(GETDATE()); " &
                "IF COL_LENGTH('dbo.UtilisateurRoles', 'CreePar') IS NULL ALTER TABLE dbo.UtilisateurRoles ADD CreePar NVARCHAR(80) NULL; " &
                "IF COL_LENGTH('dbo.UtilisateurRoles', 'ModifieLe') IS NULL ALTER TABLE dbo.UtilisateurRoles ADD ModifieLe DATETIME2 NULL; " &
                "IF COL_LENGTH('dbo.UtilisateurRoles', 'ModifiePar') IS NULL ALTER TABLE dbo.UtilisateurRoles ADD ModifiePar NVARCHAR(80) NULL; " &
                "WITH RolesUtilisateur AS (SELECT UtilisateurId, MIN(RoleId) AS RoleIdPrincipal FROM dbo.UtilisateurRoles WHERE ISNULL(EstActif,1)=1 GROUP BY UtilisateurId) " &
                "UPDATE ur SET EstRolePrincipal = CASE WHEN ur.RoleId = ru.RoleIdPrincipal THEN 1 ELSE 0 END FROM dbo.UtilisateurRoles ur INNER JOIN RolesUtilisateur ru ON ru.UtilisateurId = ur.UtilisateurId WHERE NOT EXISTS (SELECT 1 FROM dbo.UtilisateurRoles x WHERE x.UtilisateurId = ur.UtilisateurId AND ISNULL(x.EstRolePrincipal,0)=1 AND ISNULL(x.EstActif,1)=1);"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub
    End Class
End Namespace
