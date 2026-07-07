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
            AssurerRoleInterfaces("FACTURIER", New String() {"FACTURIER", "HISTORIQUE_FACTURES"})
            AssurerRoleInterfaces("CAISSIER", New String() {"CAISSE", "FINANCE"})
            AssurerRoleInterfaces("CAISSIERE", New String() {"CAISSE", "FINANCE"})
            AssurerRoleInterfaces("ADMIN", New String() {"FACTURIER", "HISTORIQUE_FACTURES", "CAISSE", "FINANCE", "ADMINISTRATION", "STOCK_INVENTAIRE", "ANALYSE_VENTES", "INVENTAIRE", "PARAMETRES"})
            AssurerRoleInterfaces("SUPERADMIN", New String() {"FACTURIER", "HISTORIQUE_FACTURES", "CAISSE", "FINANCE", "ADMINISTRATION", "STOCK_INVENTAIRE", "ANALYSE_VENTES", "INVENTAIRE", "PARAMETRES", "SUPERADMIN_TECH", "SUPERADMIN_STOCK_INITIAL", "SUPERADMIN_ROLES", "SUPERADMIN_AUDIT"})
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
            Dim roleIdFinal As Integer
            If roleId.HasValue AndAlso roleId.Value > 0 Then
                Dim sqlUpdate As String = "UPDATE dbo.Roles SET NomRole=@NomRole, EstActif=@EstActif WHERE RoleId=@RoleId"
                Dim pUpdate As New List(Of SqlParameter) From {
                    New SqlParameter("@NomRole", nomRole),
                    New SqlParameter("@EstActif", estActif),
                    New SqlParameter("@RoleId", roleId.Value)
                }
                _dal.ExecuterNonRequete(sqlUpdate, CommandType.Text, pUpdate)
                roleIdFinal = roleId.Value
            Else
                Dim sqlInsert As String = "INSERT INTO dbo.Roles (NomRole, EstActif) VALUES (@NomRole, @EstActif); SELECT CAST(SCOPE_IDENTITY() AS INT);"
                Dim pInsert As New List(Of SqlParameter) From {
                    New SqlParameter("@NomRole", nomRole),
                    New SqlParameter("@EstActif", estActif)
                }
                roleIdFinal = Convert.ToInt32(_dal.ExecuterScalaire(sqlInsert, CommandType.Text, pInsert))
            End If

            Dim pDelete As New List(Of SqlParameter) From {New SqlParameter("@RoleId", roleIdFinal)}
            _dal.ExecuterNonRequete("DELETE FROM dbo.RoleInterfaces WHERE RoleId=@RoleId", CommandType.Text, pDelete)

            If interfaceIds Is Nothing Then
                Return
            End If

            For Each interfaceId As Integer In interfaceIds
                Dim pInsertRole As New List(Of SqlParameter) From {
                    New SqlParameter("@RoleId", roleIdFinal),
                    New SqlParameter("@InterfaceId", interfaceId)
                }
                _dal.ExecuterNonRequete("INSERT INTO dbo.RoleInterfaces (RoleId, InterfaceId) VALUES (@RoleId, @InterfaceId)", CommandType.Text, pInsertRole)
            Next
        End Sub

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
