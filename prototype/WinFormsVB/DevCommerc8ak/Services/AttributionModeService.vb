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
                    AutorisationActionService.Exiger(cn, tx, "UTILISATEUR_MODES", "ADMINISTRATION")
                    Dim table As New DataTable()
                    Using cmd As New SqlCommand("SELECT ur.RoleId,r.NomRole,ur.EstRolePrincipal,ur.VenteEncaissementAutorise FROM dbo.UtilisateurRoles ur JOIN dbo.Roles r ON r.RoleId=ur.RoleId WHERE ur.UtilisateurId=@u AND ur.EstActif=1 AND r.EstActif=1 ORDER BY ur.EstRolePrincipal DESC,r.NomRole", cn, tx)
                        cmd.Parameters.AddWithValue("@u", utilisateurId)
                        Using lecteur As SqlDataReader = cmd.ExecuteReader()
                            table.Load(lecteur)
                        End Using
                    End Using
                    tx.Commit()
                    Return table
                End Using
            End Using
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
    End Class
End Namespace
