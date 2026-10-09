Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class ModeTravailService
        Private ReadOnly _dal As DAL
        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Function Lister(utilisateurId As Integer, roleId As Integer) As List(Of String)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Dim result As List(Of String) = Lister(cn, tx, utilisateurId, roleId)
                    tx.Commit()
                    Return result
                End Using
            End Using
        End Function

        Private Shared Function Lister(cn As SqlConnection, tx As SqlTransaction, userId As Integer, roleId As Integer) As List(Of String)
            Dim modes As New List(Of String)()
            Dim role As String
            Using cmd As New SqlCommand("SELECT r.NomRole FROM dbo.Utilisateurs u WITH(HOLDLOCK) JOIN dbo.UtilisateurRoles ur WITH(HOLDLOCK) ON ur.UtilisateurId=u.UtilisateurId AND ur.EstActif=1 JOIN dbo.Roles r WITH(HOLDLOCK) ON r.RoleId=ur.RoleId AND r.EstActif=1 WHERE u.UtilisateurId=@u AND u.EstActif=1 AND u.EstVerrouille=0 AND r.RoleId=@r", cn, tx)
                cmd.Parameters.AddWithValue("@u", userId)
                cmd.Parameters.AddWithValue("@r", roleId)
                Dim valeur As Object = cmd.ExecuteScalar()
                If valeur Is Nothing Then Throw New UnauthorizedAccessException("Le rôle n'est plus attribué au compte actif.")
                role = Convert.ToString(valeur)
            End Using
            If Not Possede(cn, tx, roleId, "FACTURIER") AndAlso Not Possede(cn, tx, roleId, "CAISSE") Then Return modes
            Using cmd As New SqlCommand("SELECT GestionModesActive,ModeFacturationAutorise,ModeCaisseAutorise,ModeCombineAutorise FROM dbo.Parametres WITH(HOLDLOCK)", cn, tx)
                Using r As SqlDataReader = cmd.ExecuteReader()
                    If Not r.Read() Then Throw New InvalidOperationException("Paramètres entreprise absents.")
                    If Not Convert.ToBoolean(r("GestionModesActive")) OrElse EstAdministratif(role) Then Return modes
                    If Convert.ToBoolean(r("ModeFacturationAutorise")) Then modes.Add("FACTURATION")
                    If Convert.ToBoolean(r("ModeCaisseAutorise")) Then modes.Add("CAISSE")
                    If Convert.ToBoolean(r("ModeCombineAutorise")) Then modes.Add("FACTURATION_ET_CAISSE")
                    If r.Read() Then Throw New InvalidOperationException("Plusieurs configurations entreprise : modes refusés.")
                End Using
            End Using
            modes.RemoveAll(Function(m) Not Possede(cn, tx, roleId, ModeTravailRegles.Permission(m)))
            modes.RemoveAll(Function(m) (m <> "CAISSE" AndAlso Not Possede(cn, tx, roleId, "FACTURIER")) OrElse (m <> "FACTURATION" AndAlso Not Possede(cn, tx, roleId, "CAISSE")))
            If modes.Count = 0 Then Throw New UnauthorizedAccessException("Aucun mode autorisé pour ce rôle. Contactez un administrateur.")
            Return modes
        End Function

        Private Shared Function EstAdministratif(role As String) As Boolean
            Return String.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(role, "SUPERADMIN", StringComparison.OrdinalIgnoreCase)
        End Function

        Private Shared Function Possede(cn As SqlConnection, tx As SqlTransaction, roleId As Integer, permission As String) As Boolean
            Using cmd As New SqlCommand("SELECT COUNT(*) FROM dbo.RoleInterfaces ri WITH(HOLDLOCK) JOIN dbo.InterfacesApplication i WITH(HOLDLOCK) ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=@r AND i.CodeInterface=@p AND i.EstActif=1", cn, tx)
                cmd.Parameters.AddWithValue("@r", roleId)
                cmd.Parameters.AddWithValue("@p", permission)
                Return Convert.ToInt32(cmd.ExecuteScalar()) > 0
            End Using
        End Function

        Public Sub Activer(mode As String)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Dim modes As List(Of String) = Lister(cn, tx, SessionUtilisateur.UtilisateurId, SessionUtilisateur.RoleIdActif)
                    If Not modes.Contains(mode) Then Throw New UnauthorizedAccessException("Mode non autorisé pour ce rôle.")
                    Using cmd As New SqlCommand("UPDATE dbo.UtilisateurSessions SET ModeActif=@mode WHERE SessionId=@s AND UtilisateurId=@u AND RoleIdActif=@r AND Fin IS NULL AND ModeActif IS NULL", cn, tx)
                        cmd.Parameters.AddWithValue("@mode", mode)
                        cmd.Parameters.AddWithValue("@s", SessionUtilisateur.SessionId)
                        cmd.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId)
                        cmd.Parameters.AddWithValue("@r", SessionUtilisateur.RoleIdActif)
                        If cmd.ExecuteNonQuery() <> 1 Then Throw New InvalidOperationException("Session déjà configurée ou fermée. Reconnectez-vous pour changer de mode.")
                    End Using
                    AuditMetierService.Enregistrer(cn, tx, "MODE_TRAVAIL_ACTIVE", "AUTORISATIONS", "Session", SessionUtilisateur.SessionId, SessionUtilisateur.SessionId.ToString(), Nothing, New With {.ModeActif = mode}, "", Guid.NewGuid())
                    tx.Commit()
                End Using
            End Using
            SessionUtilisateur.ModeActif = mode
        End Sub

        Public Shared Sub VerifierAction(cn As SqlConnection, tx As SqlTransaction, ecran As String)
            If ecran <> "FACTURIER" AndAlso ecran <> "HISTORIQUE_FACTURES" AndAlso ecran <> "CAISSE" AndAlso ecran <> "ANALYSE_CAISSE_PHYSIQUE" Then Return
            Dim modes As List(Of String) = Lister(cn, tx, SessionUtilisateur.UtilisateurId, SessionUtilisateur.RoleIdActif)
            If modes.Count = 0 Then Return
            Dim mode As String
            Using cmd As New SqlCommand("SELECT ModeActif FROM dbo.UtilisateurSessions WITH(HOLDLOCK) WHERE SessionId=@s AND Fin IS NULL AND UtilisateurId=@u AND RoleIdActif=@r", cn, tx)
                cmd.Parameters.AddWithValue("@s", SessionUtilisateur.SessionId)
                cmd.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId)
                cmd.Parameters.AddWithValue("@r", SessionUtilisateur.RoleIdActif)
                mode = Convert.ToString(cmd.ExecuteScalar())
            End Using
            If Not modes.Contains(mode) OrElse Not ModeTravailRegles.AutoriseEcran(mode, ecran) Then Throw New UnauthorizedAccessException("Cette opération est interdite dans le mode actif. Reconnectez-vous si les autorisations ont changé.")
        End Sub
    End Class
End Namespace
