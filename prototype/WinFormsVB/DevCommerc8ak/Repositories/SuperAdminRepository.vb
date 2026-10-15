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
                "AuditActionId BIGINT IDENTITY(1,1) PRIMARY KEY, " &
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
            AssurerInterface("SUPERADMIN_INIT_VENTES", "Initialisation des ventes", True)
            AssurerInterface("SUPERADMIN_ROLES", "Rôles et privilèges", True)
            AssurerInterface("SUPERADMIN_AUDIT", "Journal actions utilisateurs", True)

            ' Les droits initiaux sont amorcés une seule fois par migration.
            ' Une lecture ne doit jamais réattribuer une permission retirée.
        End Sub

        Private Sub AssurerRole(nomRole As String)
            Dim sql As String = "IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE NomRole=@NomRole) INSERT INTO dbo.Roles (NomRole) VALUES (@NomRole);"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@NomRole", nomRole)}
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Private Sub AssurerInterface(codeInterface As String, libelle As String, estTechnique As Boolean)
            Dim sql As String =
                "IF NOT EXISTS (SELECT 1 FROM dbo.InterfacesApplication WHERE CodeInterface=@CodeInterface) " &
                "INSERT INTO dbo.InterfacesApplication (CodeInterface, Libelle, EstTechnique, EstActif) VALUES (@CodeInterface, @Libelle, @EstTechnique, 1)"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@CodeInterface", codeInterface),
                New SqlParameter("@Libelle", libelle),
                New SqlParameter("@EstTechnique", estTechnique)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Public Function ListerRoles() As DataTable
            AssurerInfrastructure()
            Return _dal.ExecuterTable("SELECT RoleId, NomRole, ISNULL(EstActif, 1) AS EstActif,VersionAutorisations FROM dbo.Roles ORDER BY NomRole", CommandType.Text, Nothing)
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
            ' Un rôle sans droits reste administré : zéro association signifie
            ' zéro autorisation, jamais un retour aux droits codés en dur.
            Dim sql As String = "SELECT COUNT(*) FROM dbo.Roles WHERE NomRole=@NomRole"
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

        Public Sub EnregistrerRole(roleId As Integer?, nomRole As String, estActif As Boolean, interfaceIds As IEnumerable(Of Integer), motif As String, Optional versionAttendue As Byte() = Nothing)
            motif = AuditMetierService.ValiderMotif(motif)
            If RoleExisteDeja(nomRole, roleId) Then
                Throw New InvalidOperationException("Un rôle portant ce nom existe déjà.")
            End If
            VerifierRoleSystemeProtege(roleId, nomRole, estActif)

            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        AutorisationActionService.Exiger(cn, tx, "SUPERADMIN_ROLES", "SUPERADMIN_ROLES")
                        Dim avant As Object = If(roleId.HasValue, LireRoleAudit(cn, tx, roleId.Value), Nothing)
                        Dim roleIdFinal As Integer
                        If roleId.HasValue AndAlso roleId.Value > 0 Then
                            Using version As New SqlCommand("SELECT VersionAutorisations FROM dbo.Roles WITH(UPDLOCK,HOLDLOCK) WHERE RoleId=@id", cn, tx)
                                version.Parameters.AddWithValue("@id", roleId.Value)
                                Dim actuelle As Object = version.ExecuteScalar()
                                If actuelle Is Nothing Then Throw New InvalidOperationException("Rôle introuvable.")
                                AuditMetierRegles.VerifierVersion(versionAttendue, DirectCast(actuelle, Byte()))
                            End Using
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

                        Using maintien As New SqlCommand("SELECT COUNT(*) FROM dbo.Roles r WITH(HOLDLOCK) JOIN dbo.RoleInterfaces ri WITH(HOLDLOCK) ON ri.RoleId=r.RoleId JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId WHERE ISNULL(r.EstActif,1)=1 AND i.EstActif=1 AND i.CodeInterface='SUPERADMIN_ROLES' AND EXISTS(SELECT 1 FROM dbo.UtilisateurRoles ur JOIN dbo.Utilisateurs u ON u.UtilisateurId=ur.UtilisateurId WHERE ur.RoleId=r.RoleId AND ur.EstActif=1 AND u.EstActif=1 AND u.EstVerrouille=0)", cn, tx)
                            If CInt(maintien.ExecuteScalar()) = 0 Then Throw New InvalidOperationException("Le dernier accès actif à la gestion des rôles ne peut pas être retiré. Aucun droit n'a été réattribué : l'opération entière est refusée.")
                        End Using
                        AuditMetierService.Enregistrer(cn, tx, "PERMISSION_MODIFIEE", "AUTORISATIONS", "Role", roleIdFinal, nomRole, avant, LireRoleAudit(cn, tx, roleIdFinal), motif, Guid.NewGuid())
                        tx.Commit()
                    Catch
                        If tx.Connection IsNot Nothing Then tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        Public Function AccorderAdministrationModesAuRoleActif(motif As String) As Boolean
            motif = AuditMetierService.ValiderMotif(motif)
            Dim roleId As Integer = SessionUtilisateur.RoleIdActif
            Try
                Using cn As SqlConnection = _dal.CreerConnexion()
                    cn.Open()
                    Using tx As SqlTransaction = cn.BeginTransaction()
                        ' Meme pouvoir que l'editeur de privileges, jamais un
                        ' passe-droit fonde sur le nom SUPERADMIN ou un autre role.
                        AutorisationActionService.Exiger(cn, tx, "SUPERADMIN_ROLES", "SUPERADMIN_ROLES")
                        AutorisationActionService.Exiger(cn, tx, "ADMINISTRATION", "ADMINISTRATION")
                        Dim avant As Object = LireRoleAudit(cn, tx, roleId)
                        Dim interfaceId As Integer
                        Using cmd As New SqlCommand("SELECT InterfaceId FROM dbo.InterfacesApplication WITH(HOLDLOCK) WHERE CodeInterface='UTILISATEUR_MODES' AND EstActif=1", cn, tx)
                            Dim valeur As Object = cmd.ExecuteScalar()
                            If valeur Is Nothing Then Throw New InvalidOperationException("Privilège UTILISATEUR_MODES absent ou désactivé. Vérifiez la migration 2026101501 et Rôles et privilèges sur cette base.")
                            interfaceId = CInt(valeur)
                        End Using
                        Dim ajoute As Boolean
                        Using cmd As New SqlCommand("DECLARE @ajoutes TABLE(InterfaceId int); INSERT dbo.RoleInterfaces(RoleId,InterfaceId) OUTPUT inserted.InterfaceId INTO @ajoutes SELECT @r,@i WHERE NOT EXISTS(SELECT 1 FROM dbo.RoleInterfaces WITH(UPDLOCK,HOLDLOCK) WHERE RoleId=@r AND InterfaceId=@i); SELECT COUNT(*) FROM @ajoutes;", cn, tx)
                            cmd.Parameters.AddWithValue("@r", roleId)
                            cmd.Parameters.AddWithValue("@i", interfaceId)
                            ajoute = CInt(cmd.ExecuteScalar()) = 1
                        End Using
                        If ajoute Then
                            Dim nom As String
                            Using cmd As New SqlCommand("UPDATE dbo.Roles SET EstActif=EstActif WHERE RoleId=@r", cn, tx)
                                cmd.Parameters.AddWithValue("@r", roleId)
                                cmd.ExecuteNonQuery()
                            End Using
                            Using cmd As New SqlCommand("SELECT NomRole FROM dbo.Roles WHERE RoleId=@r", cn, tx)
                                cmd.Parameters.AddWithValue("@r", roleId)
                                nom = CStr(cmd.ExecuteScalar())
                            End Using
                            ' La rowversion du role invalide aussi les editeurs
                            ' deja ouverts pour eviter un ecrasement silencieux.
                            AuditMetierService.Enregistrer(cn, tx, "PERMISSION_MODIFIEE", "AUTORISATIONS", "Role", roleId, nom, avant, LireRoleAudit(cn, tx, roleId), motif, Guid.NewGuid())
                        End If
                        tx.Commit()
                        Return ajoute
                    End Using
                End Using
            Catch ex As Exception
                AuditMetierService.Echec(_dal, "PERMISSION_MODIFICATION_REFUSEE", roleId, Guid.NewGuid(), ex, entite:="Role")
                Throw
            End Try
        End Function

        Private Shared Function LireRoleAudit(cn As SqlConnection, tx As SqlTransaction, id As Integer) As Object
            Dim nom As String
            Dim actif As Boolean
            Using cmd As New SqlCommand("SELECT NomRole,ISNULL(EstActif,1) FROM dbo.Roles WITH(UPDLOCK,HOLDLOCK) WHERE RoleId=@id", cn, tx)
                cmd.Parameters.AddWithValue("@id", id)
                Using r As SqlDataReader = cmd.ExecuteReader()
                    If Not r.Read() Then Throw New InvalidOperationException("Rôle introuvable.")
                    nom = Convert.ToString(r(0)) : actif = CBool(r(1))
                End Using
            End Using
            Dim droits As New List(Of String)()
            Using cmd As New SqlCommand("SELECT i.CodeInterface FROM dbo.RoleInterfaces ri WITH(UPDLOCK,HOLDLOCK) JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=@id ORDER BY i.CodeInterface", cn, tx)
                cmd.Parameters.AddWithValue("@id", id)
                Using r As SqlDataReader = cmd.ExecuteReader()
                    While r.Read()
                        droits.Add(Convert.ToString(r(0)))
                    End While
                End Using
            End Using
            Return New With {.Role = nom, .Actif = actif, .Permissions = droits}
        End Function

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
            VerifierRoleSystemeProtege(roleId, Nothing, False)
            Dim sql As String = "UPDATE dbo.Roles SET EstActif = 0 WHERE RoleId=@RoleId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@RoleId", roleId)}
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Public Sub SupprimerRole(roleId As Integer)
            VerifierRoleSystemeProtege(roleId, Nothing, False)
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
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "AUDIT_CONSULTER", "SUPERADMIN_AUDIT")
                    tx.Commit()
                End Using
            End Using
            ' Une seule consultation réunit les deux sources sans copier ni
            ' réécrire leurs anciennes lignes. La provenance reste explicite.
            Dim sql As String =
                "SELECT TOP (1000) * FROM (" &
                "SELECT AuditActionId, Utilisateur, [Role], Module, [Action], [Description], Machine, [Statut], CreeLe, " &
                "CAST('AuditActions' AS NVARCHAR(40)) AS Provenance, CAST(NULL AS NVARCHAR(MAX)) AS Avant, CAST(NULL AS NVARCHAR(MAX)) AS Apres, CAST(NULL AS NVARCHAR(1000)) AS Motif, CAST(NULL AS INT) AS SessionId, CAST(NULL AS NVARCHAR(40)) AS ModeActif, CAST(NULL AS NVARCHAR(100)) AS ReferenceDocument FROM dbo.AuditActions " &
                "UNION ALL SELECT j.AuditId, COALESCE(j.UtilisateurNom,u.NomUtilisateur), COALESCE(j.RoleActif,''), COALESCE(j.Categorie,j.Entite), j.[Action], j.Details, j.Poste, COALESCE(j.Resultat,'HISTORIQUE'), j.EffectueLe, COALESCE(j.Provenance,'JournalAudit'), j.AnciennesValeurs,j.NouvellesValeurs,j.Motif,j.SessionId,j.ModeActif,j.ReferenceDocument " &
                "FROM dbo.JournalAudit j LEFT JOIN dbo.Utilisateurs u ON u.UtilisateurId=j.EffectuePar) Journal WHERE 1=1 "
            Dim p As New List(Of SqlParameter)()

            If dateDebut.HasValue Then
                ' AuditActions utilisait l'heure locale ; JournalAudit utilise UTC.
                ' On conserve les dates sources et adapte seulement les bornes de consultation.
                sql &= "AND CreeLe >= CASE WHEN Provenance='AuditActions' THEN @DateDebut ELSE @DateDebutUtc END "
                p.Add(New SqlParameter("@DateDebut", dateDebut.Value.Date))
                p.Add(New SqlParameter("@DateDebutUtc", dateDebut.Value.Date.ToUniversalTime()))
            End If
            If dateFin.HasValue Then
                sql &= "AND CreeLe < CASE WHEN Provenance='AuditActions' THEN @DateFinExclusive ELSE @DateFinUtc END "
                p.Add(New SqlParameter("@DateFinExclusive", dateFin.Value.Date.AddDays(1)))
                p.Add(New SqlParameter("@DateFinUtc", dateFin.Value.Date.AddDays(1).ToUniversalTime()))
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

        Private Sub VerifierRoleSystemeProtege(roleId As Integer?, nouveauNomRole As String, estActif As Boolean)
            If Not roleId.HasValue OrElse roleId.Value <= 0 OrElse Not RoleIdEstSuperAdmin(roleId.Value) Then Return
            If Not String.Equals(If(SessionUtilisateur.Role, String.Empty), "SUPERADMIN", StringComparison.OrdinalIgnoreCase) Then
                Throw New InvalidOperationException("Le rôle SUPERADMIN ne peut être modifié que par un SUPERADMIN.")
            End If
            If Not estActif Then
                Throw New InvalidOperationException("Le rôle SUPERADMIN ne peut pas être désactivé ou supprimé.")
            End If
            If Not String.IsNullOrWhiteSpace(nouveauNomRole) AndAlso Not String.Equals(nouveauNomRole.Trim(), "SUPERADMIN", StringComparison.OrdinalIgnoreCase) Then
                Throw New InvalidOperationException("Le rôle SUPERADMIN ne peut pas être renommé.")
            End If
        End Sub

        Private Function RoleIdEstSuperAdmin(roleId As Integer) As Boolean
            Dim sql As String = "SELECT COUNT(*) FROM dbo.Roles WHERE RoleId=@RoleId AND UPPER(LTRIM(RTRIM(NomRole)))='SUPERADMIN'"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@RoleId", roleId)}
            Return Convert.ToInt32(_dal.ExecuterScalaire(sql, CommandType.Text, p)) > 0
        End Function
    End Class
End Namespace
