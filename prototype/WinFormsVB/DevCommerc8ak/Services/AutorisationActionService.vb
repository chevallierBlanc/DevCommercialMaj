Option Strict On
Option Explicit On

Imports System
Imports System.Data.SqlClient

Namespace DevCommerc8ak
    Public NotInheritable Class AutorisationActionService
        Private Sub New()
        End Sub

        Public Shared Sub Exiger(cn As SqlConnection, tx As SqlTransaction, action As String, ecran As String)
            ModeTravailService.VerifierAction(cn, tx, ecran)
            ' Les droits sont relus dans la même transaction. Le nom du rôle
            ' ne constitue jamais un privilège implicite, même pour SUPERADMIN.
            Dim sql As String = "SELECT COUNT(*) FROM dbo.Utilisateurs u WITH (HOLDLOCK) " &
                "JOIN dbo.UtilisateurRoles ur WITH (HOLDLOCK) ON ur.UtilisateurId=u.UtilisateurId AND ur.EstActif=1 " &
                "JOIN dbo.Roles r WITH (HOLDLOCK) ON r.RoleId=ur.RoleId AND r.EstActif=1 " &
                "JOIN dbo.UtilisateurSessions s WITH (HOLDLOCK) ON s.UtilisateurId=u.UtilisateurId AND s.RoleIdActif=r.RoleId " &
                "WHERE u.UtilisateurId=@user AND u.EstActif=1 AND u.EstVerrouille=0 " &
                "AND r.RoleId=@role AND s.SessionId=@session AND s.Fin IS NULL " &
                "AND EXISTS(SELECT 1 FROM dbo.RoleInterfaces ri WITH (HOLDLOCK) JOIN dbo.InterfacesApplication i WITH (HOLDLOCK) ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=r.RoleId AND i.CodeInterface=@action AND i.EstActif=1) " &
                "AND EXISTS(SELECT 1 FROM dbo.RoleInterfaces ri WITH (HOLDLOCK) JOIN dbo.InterfacesApplication i WITH (HOLDLOCK) ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=r.RoleId AND i.CodeInterface=@ecran AND i.EstActif=1)"
            Using cmd As New SqlCommand(sql, cn, tx)
                cmd.Parameters.AddWithValue("@user", SessionUtilisateur.UtilisateurId)
                cmd.Parameters.AddWithValue("@role", SessionUtilisateur.RoleIdActif)
                cmd.Parameters.AddWithValue("@session", SessionUtilisateur.SessionId)
                cmd.Parameters.AddWithValue("@action", action)
                cmd.Parameters.AddWithValue("@ecran", ecran)
                If Convert.ToInt32(cmd.ExecuteScalar()) = 0 Then
                    Throw New UnauthorizedAccessException("Votre session ne dispose pas de l'autorisation : " & action & ".")
                End If
            End Using
        End Sub
    End Class
End Namespace
