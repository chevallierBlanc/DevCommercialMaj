Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class SuperAdminRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Sub AssurerInfrastructure()
            Dim sql As String =
                "IF COL_LENGTH('dbo.Roles', 'EstActif') IS NULL " &
                "BEGIN " &
                "ALTER TABLE dbo.Roles ADD EstActif BIT NOT NULL CONSTRAINT DF_Roles_EstActif DEFAULT(1); " &
                "END " &
                "IF OBJECT_ID('dbo.InterfacesApplication', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.InterfacesApplication (" &
                "InterfaceId INT IDENTITY(1,1) PRIMARY KEY, " &
                "CodeInterface NVARCHAR(80) NOT NULL UNIQUE, " &
                "Libelle NVARCHAR(150) NOT NULL, " &
                "EstTechnique BIT NOT NULL CONSTRAINT DF_InterfacesApplication_EstTechnique DEFAULT(0), " &
                "EstActif BIT NOT NULL CONSTRAINT DF_InterfacesApplication_EstActif DEFAULT(1)); " &
                "END " &
                "IF OBJECT_ID('dbo.RoleInterfaces', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.RoleInterfaces (" &
                "RoleId INT NOT NULL, " &
                "InterfaceId INT NOT NULL, " &
                "PRIMARY KEY (RoleId, InterfaceId)); " &
                "END " &
                "IF OBJECT_ID('dbo.UtilisateurRoles', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.UtilisateurRoles (" &
                "UtilisateurRoleId INT IDENTITY(1,1) PRIMARY KEY, " &
                "UtilisateurId INT NOT NULL, " &
                "RoleId INT NOT NULL, " &
                "EstActif BIT NOT NULL CONSTRAINT DF_UtilisateurRoles_EstActif DEFAULT(1), " &
                "EstRolePrincipal BIT NOT NULL CONSTRAINT DF_UtilisateurRoles_EstRolePrincipal DEFAULT(0), " &
                "CreeLe DATETIME2 NOT NULL CONSTRAINT DF_UtilisateurRoles_CreeLe DEFAULT(GETDATE()), " &
                "CreePar NVARCHAR(80) NULL, " &
                "ModifieLe DATETIME2 NULL, " &
                "ModifiePar NVARCHAR(80) NULL); " &
                "CREATE UNIQUE INDEX UX_UtilisateurRoles_Utilisateur_Role ON dbo.UtilisateurRoles(UtilisateurId, RoleId); " &
                "END " &
                "IF COL_LENGTH('dbo.UtilisateurRoles', 'EstActif') IS NULL ALTER TABLE dbo.UtilisateurRoles ADD EstActif BIT NOT NULL CONSTRAINT DF_UtilisateurRoles_EstActif_SA DEFAULT(1); " &
                "IF COL_LENGTH('dbo.UtilisateurRoles', 'EstRolePrincipal') IS NULL ALTER TABLE dbo.UtilisateurRoles ADD EstRolePrincipal BIT NOT NULL CONSTRAINT DF_UtilisateurRoles_EstRolePrincipal_SA DEFAULT(0); " &
                "IF COL_LENGTH('dbo.UtilisateurRoles', 'CreeLe') IS NULL ALTER TABLE dbo.UtilisateurRoles ADD CreeLe DATETIME2 NOT NULL CONSTRAINT DF_UtilisateurRoles_CreeLe_SA DEFAULT(GETDATE()); " &
                "IF COL_LENGTH('dbo.UtilisateurRoles', 'CreePar') IS NULL ALTER TABLE dbo.UtilisateurRoles ADD CreePar NVARCHAR(80) NULL; " &
                "IF COL_LENGTH('dbo.UtilisateurRoles', 'ModifieLe') IS NULL ALTER TABLE dbo.UtilisateurRoles ADD ModifieLe DATETIME2 NULL; " &
                "IF COL_LENGTH('dbo.UtilisateurRoles', 'ModifiePar') IS NULL ALTER TABLE dbo.UtilisateurRoles ADD ModifiePar NVARCHAR(80) NULL; " &
                "IF OBJECT_ID('dbo.AuditActions', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.AuditActions (" &
                "AuditActionId INT IDENTITY(1,1) PRIMARY KEY, " &
                "Utilisateur NVARCHAR(80) NULL, " &
                "[Role] NVARCHAR(50) NULL, " &
                "Module NVARCHAR(80) NULL, " &
                "[Action] NVARCHAR(100) NULL, " &
                "[Description] NVARCHAR(255) NULL, " &
                "Machine NVARCHAR(100) NULL, " &
                "[Statut] NVARCHAR(30) NULL, " &
                "CreeLe DATETIME2 NOT NULL CONSTRAINT DF_AuditActions_CreeLe DEFAULT(GETDATE())); " &
                "END"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)

            AssurerRole("SUPERADMIN")
            AssurerRole("ADMIN")
            AssurerRole("FACTURIER")
            AssurerRole("CAISSIER")
            AssurerRole("CAISSIERE")

            AssurerInterface("FACTURIER", "Facturier", False)
            AssurerInterface("HISTORIQUE_FACTURES", "Historique factures", False)
            AssurerInterface("CAISSE", "Caisse", False)
            AssurerInterface("FINANCE", "Finance", False)
            AssurerInterface("ADMINISTRATION", "Administration", False)
            AssurerInterface("ANALYSE_CAISSE_PHYSIQUE", "Analyse caisse physique", False)
            AssurerInterface("STOCK_INVENTAIRE", "Stock / Inventaire", False)
            AssurerInterface("ANALYSE_VENTES", "Analyse ventes", False)
            AssurerInterface("INVENTAIRE", "Inventaire", False)
            AssurerInterface("PARAMETRES", "Paramètres", False)
            AssurerInterface("SUPERADMIN_TECH", "Interfaces techniques SuperAdmin", True)
            AssurerInterface("SUPERADMIN_STOCK_INITIAL", "Stock initial technique", True)
            AssurerInterface("SUPERADMIN_ROLES", "Rôles et privilèges", True)
            AssurerInterface("SUPERADMIN_AUDIT", "Journal actions utilisateurs", True)

            AssurerPermissionsParDefaut()
        End Sub

        Private Sub AssurerRole(nomRole As String)
            Dim sql As String = "IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE NomRole=@NomRole) INSERT INTO dbo.Roles (NomRole) VALUES (@NomRole); " &
                                "UPDATE dbo.Roles SET EstActif = 1 WHERE NomRole=@NomRole AND (EstActif IS NULL OR EstActif = 0);"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@NomRole", nomRole)}
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Private Sub AssurerInterface(codeInterface As String, libelle As String, estTechnique As Boolean)
            Dim sql As String =
                "IF NOT EXISTS (SELECT 1 FROM dbo.InterfacesApplication WHERE CodeInterface=@CodeInterface) " &
                "INSERT INTO dbo.InterfacesApplication (CodeInterface, Libelle, EstTechnique, EstActif) VALUES (@CodeInterface, @Libelle, @EstTechnique, 1) " &
                "ELSE UPDATE dbo.InterfacesApplication SET Libelle=@Libelle, EstTechnique=@EstTechnique, EstActif=1 WHERE CodeInterface=@CodeInterface"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@CodeInterface", codeInterface),
                New SqlParameter("@Libelle", libelle),
                New SqlParameter("@EstTechnique", estTechnique)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Private Sub AssurerPermissionsParDefaut()
            AssurerRoleInterfacesSiVide("FACTURIER", New String() {"FACTURIER", "HISTORIQUE_FACTURES"})
            AssurerRoleInterfacesSiVide("CAISSIER", New String() {"CAISSE", "FINANCE"})
            AssurerRoleInterfacesSiVide("CAISSIERE", New String() {"CAISSE", "FINANCE"})
            AssurerRoleInterfacesSiVide("ADMIN", New String() {"FACTURIER", "HISTORIQUE_FACTURES", "CAISSE", "FINANCE", "ADMINISTRATION", "ANALYSE_CAISSE_PHYSIQUE", "STOCK_INVENTAIRE", "ANALYSE_VENTES", "INVENTAIRE", "PARAMETRES"})
            AssurerRoleInterfacesSiVide("SUPERADMIN", New String() {"FACTURIER", "HISTORIQUE_FACTURES", "CAISSE", "FINANCE", "ADMINISTRATION", "ANALYSE_CAISSE_PHYSIQUE", "STOCK_INVENTAIRE", "ANALYSE_VENTES", "INVENTAIRE", "PARAMETRES", "SUPERADMIN_TECH", "SUPERADMIN_STOCK_INITIAL", "SUPERADMIN_ROLES", "SUPERADMIN_AUDIT"})
            AssurerRoleInterfaces("ADMIN", New String() {"ANALYSE_CAISSE_PHYSIQUE"})
            AssurerRoleInterfaces("SUPERADMIN", New String() {"ANALYSE_CAISSE_PHYSIQUE"})
        End Sub

        Private Sub AssurerRoleInterfacesSiVide(nomRole As String, codesInterfaces As IEnumerable(Of String))
            If RoleUtilisePermissionsInterne(nomRole) Then
                Return
            End If

            AssurerRoleInterfaces(nomRole, codesInterfaces)
        End Sub

        Private Sub AssurerRoleInterfaces(nomRole As String, codesInterfaces As IEnumerable(Of String))
            If codesInterfaces Is Nothing Then
                Return
            End If

            For Each code As String In codesInterfaces
                Dim sql As String =
                    "INSERT INTO dbo.RoleInterfaces (RoleId, InterfaceId) " &
                    "SELECT r.RoleId, i.InterfaceId " &
                    "FROM dbo.Roles r CROSS JOIN dbo.InterfacesApplication i " &
                    "WHERE r.NomRole=@NomRole AND i.CodeInterface=@CodeInterface " &
                    "AND NOT EXISTS (SELECT 1 FROM dbo.RoleInterfaces ri WHERE ri.RoleId=r.RoleId AND ri.InterfaceId=i.InterfaceId)"
                Dim p As New List(Of SqlParameter) From {
                    New SqlParameter("@NomRole", nomRole),
                    New SqlParameter("@CodeInterface", code)
                }
                _dal.ExecuterNonRequete(sql, CommandType.Text, p)
            Next
        End Sub

        Public Function ListerRoles() As DataTable
            AssurerInfrastructure()
            Return _dal.ExecuterTable("SELECT RoleId, NomRole, ISNULL(EstActif, 1) AS EstActif FROM dbo.Roles ORDER BY NomRole", CommandType.Text, Nothing)
        End Function

        Public Function ListerInterfaces() As DataTable
            AssurerInfrastructure()
            Return _dal.ExecuterTable("SELECT InterfaceId, CodeInterface, Libelle, EstTechnique, EstActif FROM dbo.InterfacesApplication WHERE EstActif = 1 ORDER BY EstTechnique, Libelle", CommandType.Text, Nothing)
        End Function

        Public Function ListerInterfacesParRole(roleId As Integer) As DataTable
            AssurerInfrastructure()
            Dim sql As String =
                "SELECT i.InterfaceId, i.CodeInterface, i.Libelle, i.EstTechnique, i.EstActif, " &
                "CASE WHEN ri.RoleId IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS Autorise " &
                "FROM dbo.InterfacesApplication i " &
                "LEFT JOIN dbo.RoleInterfaces ri ON ri.InterfaceId = i.InterfaceId AND ri.RoleId = @RoleId " &
                "WHERE i.EstActif = 1 " &
                "ORDER BY i.EstTechnique, i.Libelle"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@RoleId", roleId)}
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        Public Function RoleUtilisePermissions(nomRole As String) As Boolean
            AssurerInfrastructure()
            Return RoleUtilisePermissionsInterne(nomRole)
        End Function

        Private Function RoleUtilisePermissionsInterne(nomRole As String) As Boolean
            Dim sql As String =
                "SELECT COUNT(*) FROM dbo.RoleInterfaces ri " &
                "INNER JOIN dbo.Roles r ON r.RoleId = ri.RoleId " &
                "WHERE r.NomRole = @NomRole"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@NomRole", nomRole)}
            Return Convert.ToInt32(_dal.ExecuterScalaire(sql, CommandType.Text, p)) > 0
        End Function

        Public Function RoleAutoriseInterface(nomRole As String, codeInterface As String) As Boolean
            AssurerInfrastructure()
            Dim sql As String =
                "SELECT COUNT(*) FROM dbo.RoleInterfaces ri " &
                "INNER JOIN dbo.Roles r ON r.RoleId = ri.RoleId " &
                "INNER JOIN dbo.InterfacesApplication i ON i.InterfaceId = ri.InterfaceId " &
                "WHERE r.NomRole = @NomRole AND ISNULL(r.EstActif, 1) = 1 AND i.CodeInterface = @CodeInterface AND i.EstActif = 1"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@NomRole", nomRole),
                New SqlParameter("@CodeInterface", codeInterface)
            }
            Return Convert.ToInt32(_dal.ExecuterScalaire(sql, CommandType.Text, p)) > 0
        End Function

        Public Sub EnregistrerRole(roleId As Integer?, nomRole As String, estActif As Boolean, interfaceIds As IEnumerable(Of Integer))
            AssurerInfrastructure()
            If RoleExisteDeja(nomRole, roleId) Then
                Throw New InvalidOperationException("Un rôle portant ce nom existe déjà.")
            End If

            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        Dim roleIdFinal As Integer
                        If roleId.HasValue AndAlso roleId.Value > 0 Then
                            Using cmdUpdate As New SqlCommand("UPDATE dbo.Roles SET NomRole=@NomRole, EstActif=@EstActif WHERE RoleId=@RoleId", cn, tx)
                                cmdUpdate.Parameters.AddWithValue("@NomRole", nomRole)
                                cmdUpdate.Parameters.AddWithValue("@EstActif", estActif)
                                cmdUpdate.Parameters.AddWithValue("@RoleId", roleId.Value)
                                cmdUpdate.ExecuteNonQuery()
                            End Using
                            roleIdFinal = roleId.Value
                        Else
                            Using cmdInsert As New SqlCommand("INSERT INTO dbo.Roles (NomRole, EstActif) VALUES (@NomRole, @EstActif); SELECT CAST(SCOPE_IDENTITY() AS INT);", cn, tx)
                                cmdInsert.Parameters.AddWithValue("@NomRole", nomRole)
                                cmdInsert.Parameters.AddWithValue("@EstActif", estActif)
                                roleIdFinal = Convert.ToInt32(cmdInsert.ExecuteScalar())
                            End Using
                        End If

                        Using cmdDelete As New SqlCommand("DELETE FROM dbo.RoleInterfaces WHERE RoleId=@RoleId", cn, tx)
                            cmdDelete.Parameters.AddWithValue("@RoleId", roleIdFinal)
                            cmdDelete.ExecuteNonQuery()
                        End Using

                        If interfaceIds IsNot Nothing Then
                            For Each interfaceId As Integer In interfaceIds
                                Using cmdInsertRole As New SqlCommand("INSERT INTO dbo.RoleInterfaces (RoleId, InterfaceId) VALUES (@RoleId, @InterfaceId)", cn, tx)
                                    cmdInsertRole.Parameters.AddWithValue("@RoleId", roleIdFinal)
                                    cmdInsertRole.Parameters.AddWithValue("@InterfaceId", interfaceId)
                                    cmdInsertRole.ExecuteNonQuery()
                                End Using
                            Next
                        End If

                        tx.Commit()
                    Catch
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        Public Function RoleExisteDeja(nomRole As String, roleIdExclu As Integer?) As Boolean
            Dim sql As String = "SELECT COUNT(*) FROM dbo.Roles WHERE UPPER(NomRole)=@NomRole AND (@RoleIdExclu IS NULL OR RoleId<>@RoleIdExclu)"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@NomRole", nomRole.Trim().ToUpperInvariant()),
                New SqlParameter("@RoleIdExclu", If(roleIdExclu.HasValue, CType(roleIdExclu.Value, Object), DBNull.Value))
            }
            Return Convert.ToInt32(_dal.ExecuterScalaire(sql, CommandType.Text, p)) > 0
        End Function

        Public Function CompterUtilisateursParRole(roleId As Integer) As Integer
            Dim sql As String = "SELECT COUNT(*) FROM dbo.UtilisateurRoles WHERE RoleId=@RoleId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@RoleId", roleId)}
            Return Convert.ToInt32(_dal.ExecuterScalaire(sql, CommandType.Text, p))
        End Function

        Public Function EstDernierRoleCritique(roleId As Integer) As Boolean
            Dim sql As String =
                "SELECT COUNT(DISTINCT r.RoleId) " &
                "FROM dbo.Roles r " &
                "INNER JOIN dbo.RoleInterfaces ri ON ri.RoleId = r.RoleId " &
                "INNER JOIN dbo.InterfacesApplication i ON i.InterfaceId = ri.InterfaceId " &
                "WHERE ISNULL(r.EstActif,1)=1 " &
                "AND i.CodeInterface IN ('ADMINISTRATION','PARAMETRES','SUPERADMIN_ROLES','SUPERADMIN_TECH') " &
                "AND r.RoleId <> @RoleId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@RoleId", roleId)}
            Return Convert.ToInt32(_dal.ExecuterScalaire(sql, CommandType.Text, p)) = 0
        End Function

        Public Sub DesactiverRole(roleId As Integer)
            Dim sql As String = "UPDATE dbo.Roles SET EstActif = 0 WHERE RoleId=@RoleId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@RoleId", roleId)}
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Public Sub SupprimerRole(roleId As Integer)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        Using cmdDeleteInterfaces As New SqlCommand("DELETE FROM dbo.RoleInterfaces WHERE RoleId=@RoleId", cn, tx)
                            cmdDeleteInterfaces.Parameters.AddWithValue("@RoleId", roleId)
                            cmdDeleteInterfaces.ExecuteNonQuery()
                        End Using

                        Using cmdDeleteRole As New SqlCommand("DELETE FROM dbo.Roles WHERE RoleId=@RoleId", cn, tx)
                            cmdDeleteRole.Parameters.AddWithValue("@RoleId", roleId)
                            cmdDeleteRole.ExecuteNonQuery()
                        End Using

                        tx.Commit()
                    Catch
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        Public Sub AjouterAuditAction(utilisateur As String, role As String, moduleName As String, actionName As String, description As String, machine As String, statut As String)
            AssurerInfrastructure()
            Dim sql As String =
                "INSERT INTO dbo.AuditActions (Utilisateur, [Role], Module, [Action], [Description], Machine, [Statut]) " &
                "VALUES (@Utilisateur, @Role, @Module, @Action, @Description, @Machine, @Statut)"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@Utilisateur", If(String.IsNullOrWhiteSpace(utilisateur), CType(DBNull.Value, Object), utilisateur.Trim())),
                New SqlParameter("@Role", If(String.IsNullOrWhiteSpace(role), CType(DBNull.Value, Object), role.Trim())),
                New SqlParameter("@Module", If(String.IsNullOrWhiteSpace(moduleName), CType(DBNull.Value, Object), moduleName.Trim())),
                New SqlParameter("@Action", If(String.IsNullOrWhiteSpace(actionName), CType(DBNull.Value, Object), actionName.Trim())),
                New SqlParameter("@Description", If(String.IsNullOrWhiteSpace(description), CType(DBNull.Value, Object), description.Trim())),
                New SqlParameter("@Machine", If(String.IsNullOrWhiteSpace(machine), CType(DBNull.Value, Object), machine.Trim())),
                New SqlParameter("@Statut", If(String.IsNullOrWhiteSpace(statut), CType(DBNull.Value, Object), statut.Trim()))
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Public Function ListerAuditActions(dateDebut As Date?, dateFin As Date?, utilisateur As String, role As String, moduleName As String, actionName As String, statut As String) As DataTable
            AssurerInfrastructure()
            Dim sql As String =
                "SELECT AuditActionId, Utilisateur, [Role], Module, [Action], [Description], Machine, [Statut], CreeLe " &
                "FROM dbo.AuditActions WHERE 1=1 "
            Dim p As New List(Of SqlParameter)()

            If dateDebut.HasValue Then
                sql &= "AND CAST(CreeLe AS DATE) >= @DateDebut "
                p.Add(New SqlParameter("@DateDebut", dateDebut.Value.Date))
            End If
            If dateFin.HasValue Then
                sql &= "AND CAST(CreeLe AS DATE) <= @DateFin "
                p.Add(New SqlParameter("@DateFin", dateFin.Value.Date))
            End If
            If Not String.IsNullOrWhiteSpace(utilisateur) Then
                sql &= "AND ISNULL(Utilisateur,'') LIKE @Utilisateur "
                p.Add(New SqlParameter("@Utilisateur", "%" & utilisateur.Trim() & "%"))
            End If
            If Not String.IsNullOrWhiteSpace(role) Then
                sql &= "AND ISNULL([Role],'') LIKE @Role "
                p.Add(New SqlParameter("@Role", "%" & role.Trim() & "%"))
            End If
            If Not String.IsNullOrWhiteSpace(moduleName) Then
                sql &= "AND ISNULL(Module,'') LIKE @Module "
                p.Add(New SqlParameter("@Module", "%" & moduleName.Trim() & "%"))
            End If
            If Not String.IsNullOrWhiteSpace(actionName) Then
                sql &= "AND ISNULL([Action],'') LIKE @Action "
                p.Add(New SqlParameter("@Action", "%" & actionName.Trim() & "%"))
            End If
            If Not String.IsNullOrWhiteSpace(statut) Then
                sql &= "AND ISNULL([Statut],'') LIKE @Statut "
                p.Add(New SqlParameter("@Statut", "%" & statut.Trim() & "%"))
            End If

            sql &= "ORDER BY CreeLe DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        Public Function ListerUtilisateursAvecRole() As DataTable
            Dim sql As String =
                "SELECT u.UtilisateurId, u.NomUtilisateur, ISNULL(r.NomRole, '') AS NomRole " &
                "FROM dbo.Utilisateurs u " &
                "LEFT JOIN dbo.UtilisateurRoles ur ON ur.UtilisateurId = u.UtilisateurId " &
                "LEFT JOIN dbo.Roles r ON r.RoleId = ur.RoleId"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function
    End Class
End Namespace
