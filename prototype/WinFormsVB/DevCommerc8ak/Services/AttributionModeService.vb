Option Strict On
Option Explicit On
Imports System
Imports System.Data
Imports System.Data.SqlClient

Namespace DevCommerc8ak
    Public Class AttributionModeService
        Private ReadOnly _dal As DAL
        Public Sub New(dal As DAL)
            _dal = dal
        End Sub
        Public Function Lister(utilisateurId As Integer) As DataTable
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "ADMINISTRATION", "ADMINISTRATION")
                    Dim peutModifier As Boolean = Autorise(cn, tx, "UTILISATEUR_MODES", "ADMINISTRATION")
                    Dim peutAccorder As Boolean = Autorise(cn, tx, "SUPERADMIN_ROLES", "SUPERADMIN_ROLES")
                    VerifierSchema(cn, tx)
                    Using config As New SqlCommand("SELECT COUNT(*) FROM dbo.Parametres", cn, tx)
                        If CInt(config.ExecuteScalar()) <> 1 Then Throw New InvalidOperationException("Une configuration entreprise unique est requise pour diagnostiquer les modes.")
                    End Using
                    Dim table As New DataTable()
                    Using cmd As New SqlCommand("SELECT ur.RoleId,r.NomRole,ur.EstRolePrincipal,ur.VenteEncaissementAutorise,u.EstActif AS CompteActif,u.EstVerrouille,p.GestionModesActive,p.ModeFacturationAutorise,p.ModeCaisseAutorise,p.ModeCombineAutorise,d.FacturationEligible,d.CaisseEligible,d.CombineEligible FROM dbo.UtilisateurRoles ur JOIN dbo.Utilisateurs u ON u.UtilisateurId=ur.UtilisateurId JOIN dbo.Roles r ON r.RoleId=ur.RoleId CROSS JOIN dbo.Parametres p OUTER APPLY(SELECT CASE WHEN COUNT(DISTINCT CASE WHEN i.CodeInterface IN('FACTURIER','FACTURE_CREER') THEN i.CodeInterface END)=2 THEN 1 ELSE 0 END AS FacturationEligible,CASE WHEN COUNT(DISTINCT CASE WHEN i.CodeInterface IN('CAISSE','ENCAISSEMENT_CREER') THEN i.CodeInterface END)=2 THEN 1 ELSE 0 END AS CaisseEligible,CASE WHEN COUNT(DISTINCT i.CodeInterface)=5 THEN 1 ELSE 0 END AS CombineEligible FROM dbo.RoleInterfaces ri JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId AND i.EstActif=1 WHERE ri.RoleId=r.RoleId AND i.CodeInterface IN('FACTURIER','FACTURE_CREER','CAISSE','ENCAISSEMENT_CREER','MODE_COMBINE')) d WHERE ur.UtilisateurId=@u AND ur.EstActif=1 AND r.EstActif=1 ORDER BY ur.EstRolePrincipal DESC,r.NomRole", cn, tx)
                        cmd.Parameters.AddWithValue("@u", utilisateurId)
                        Using lecteur As SqlDataReader = cmd.ExecuteReader()
                            table.Load(lecteur)
                        End Using
                    End Using
                    table.ExtendedProperties("PeutModifier") = peutModifier
                    table.ExtendedProperties("PeutAccorder") = peutAccorder
                    tx.Commit()
                    Return table
                End Using
            End Using
        End Function
        Public Function AccorderAdministrationAuRoleActif(motif As String) As Boolean
            Return New SuperAdminRepository(_dal).AccorderAdministrationModesAuRoleActif(motif)
        End Function
        Private Shared Function Autorise(cn As SqlConnection, tx As SqlTransaction, action As String, ecran As String) As Boolean
            Try
                AutorisationActionService.Exiger(cn, tx, action, ecran)
                Return True
            Catch ex As UnauthorizedAccessException
                Return False
            End Try
        End Function
        Public Sub Attribuer(utilisateurId As Integer, roleId As Integer, valeur As Boolean, attendu As Boolean, motif As String)
            Try
                AttribuerDansTransaction(utilisateurId, roleId, valeur, attendu, motif)
            Catch ex As Exception
                AuditMetierService.Echec(_dal, "MODE_UTILISATEUR_REFUSE", utilisateurId, Guid.NewGuid(), ex, entite:="Utilisateur")
                Throw
            End Try
        End Sub
        Private Sub AttribuerDansTransaction(utilisateurId As Integer, roleId As Integer, valeur As Boolean, attendu As Boolean, motif As String)
            motif = AuditMetierService.ValiderMotif(motif)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "UTILISATEUR_MODES", "ADMINISTRATION")
                    VerifierSchema(cn, tx)
                    Using protection As New SqlCommand("SELECT CASE WHEN EXISTS(SELECT 1 FROM dbo.UtilisateurRoles ur JOIN dbo.Roles r ON r.RoleId=ur.RoleId WHERE ur.UtilisateurId=@u AND ur.EstActif=1 AND r.NomRole='SUPERADMIN') AND NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleId=@acteur AND NomRole='SUPERADMIN') THEN 1 ELSE 0 END", cn, tx)
                        protection.Parameters.AddWithValue("@u", utilisateurId)
                        protection.Parameters.AddWithValue("@acteur", SessionUtilisateur.RoleIdActif)
                        If CInt(protection.ExecuteScalar()) = 1 Then Throw New UnauthorizedAccessException("Seul un SUPERADMIN peut modifier les modes d'un compte SUPERADMIN.")
                    End Using
                    If valeur Then
                        Using eligibilite As New SqlCommand("SELECT COUNT(DISTINCT i.CodeInterface) FROM dbo.RoleInterfaces ri WITH(HOLDLOCK) JOIN dbo.Roles r WITH(HOLDLOCK) ON r.RoleId=ri.RoleId AND r.EstActif=1 JOIN dbo.InterfacesApplication i WITH(HOLDLOCK) ON i.InterfaceId=ri.InterfaceId AND i.EstActif=1 WHERE ri.RoleId=@r AND i.CodeInterface IN('FACTURIER','CAISSE','FACTURE_CREER','ENCAISSEMENT_CREER','MODE_COMBINE')", cn, tx)
                            eligibilite.Parameters.AddWithValue("@r", roleId)
                            If CInt(eligibilite.ExecuteScalar()) <> 5 Then Throw New UnauthorizedAccessException("Ce rôle n'est pas éligible : les deux accès, les deux actions et Mode combiné doivent être attribués explicitement.")
                        End Using
                    End If
                    Using cmd As New SqlCommand("UPDATE dbo.UtilisateurRoles SET VenteEncaissementAutorise=@v,ModifieLe=GETDATE(),ModifiePar=@acteur WHERE UtilisateurId=@u AND RoleId=@r AND EstActif=1 AND VenteEncaissementAutorise=@a", cn, tx)
                        cmd.Parameters.AddWithValue("@v", valeur)
                        cmd.Parameters.AddWithValue("@a", attendu)
                        cmd.Parameters.AddWithValue("@u", utilisateurId)
                        cmd.Parameters.AddWithValue("@r", roleId)
                        cmd.Parameters.AddWithValue("@acteur", SessionUtilisateur.NomUtilisateur)
                        If cmd.ExecuteNonQuery() <> 1 Then Throw New DBConcurrencyException("Cette association a changé. Rechargez les rôles de ce compte.")
                    End Using
                    AuditMetierService.Enregistrer(cn, tx, "MODE_UTILISATEUR_ATTRIBUE", "AUTORISATIONS", "Utilisateur", utilisateurId, utilisateurId.ToString(), New With {.RoleId = roleId, .Autorise = attendu}, New With {.RoleId = roleId, .Autorise = valeur}, motif, Guid.NewGuid())
                    tx.Commit()
                End Using
            End Using
        End Sub
        Friend Shared Sub VerifierSchema(cn As SqlConnection, tx As SqlTransaction)
            Using cmd As New SqlCommand("SELECT COL_LENGTH(N'dbo.UtilisateurRoles',N'VenteEncaissementAutorise')", cn, tx)
                If cmd.ExecuteScalar() Is DBNull.Value Then Throw New InvalidOperationException("Migration 2026101501 absente sur cette base : appliquez la migration additive avant de configurer les modes. Aucun droit ne sera attribué automatiquement.")
            End Using
        End Sub
    End Class
End Namespace
