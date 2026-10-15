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
            AttributionModeService.VerifierSchema(cn, tx)
            Dim role As String
            Dim attribution As Boolean
            Using cmd As New SqlCommand("SELECT r.NomRole,ur.VenteEncaissementAutorise FROM dbo.Utilisateurs u WITH(HOLDLOCK) JOIN dbo.UtilisateurRoles ur WITH(HOLDLOCK) ON ur.UtilisateurId=u.UtilisateurId AND ur.EstActif=1 JOIN dbo.Roles r WITH(HOLDLOCK) ON r.RoleId=ur.RoleId AND r.EstActif=1 WHERE u.UtilisateurId=@u AND u.EstActif=1 AND u.EstVerrouille=0 AND r.RoleId=@r", cn, tx)
                cmd.Parameters.AddWithValue("@u", userId)
                cmd.Parameters.AddWithValue("@r", roleId)
                Using lecteur As SqlDataReader = cmd.ExecuteReader()
                    If Not lecteur.Read() Then Throw New UnauthorizedAccessException("Le rôle n'est plus attribué au compte actif.")
                    role = CStr(lecteur(0))
                    attribution = CBool(lecteur(1))
                End Using
            End Using
            Dim droits As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Using cmd As New SqlCommand("SELECT i.CodeInterface FROM dbo.RoleInterfaces ri WITH(HOLDLOCK) JOIN dbo.InterfacesApplication i WITH(HOLDLOCK) ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=@r AND i.EstActif=1", cn, tx)
                cmd.Parameters.AddWithValue("@r", roleId)
                Using r As SqlDataReader = cmd.ExecuteReader()
                    While r.Read()
                        droits.Add(Convert.ToString(r(0)))
                    End While
                End Using
            End Using
            ' Les roles techniques sans parcours operationnel gardent leur acces
            ' historique, y compris pour configurer une nouvelle installation.
            If Not droits.Contains("FACTURIER") AndAlso Not droits.Contains("CAISSE") AndAlso Not droits.Contains("MODE_FACTURATION") AndAlso Not droits.Contains("MODE_CAISSE") AndAlso Not droits.Contains("MODE_COMBINE") Then Return New List(Of String)()
            Using cmd As New SqlCommand("SELECT GestionModesActive,ModeFacturationAutorise,ModeCaisseAutorise,ModeCombineAutorise FROM dbo.Parametres WITH(HOLDLOCK)", cn, tx)
                Using r As SqlDataReader = cmd.ExecuteReader()
                    If Not r.Read() Then Throw New InvalidOperationException("Paramètres entreprise absents.")
                    Dim gestion As Boolean = CBool(r("GestionModesActive"))
                    Dim facturation As Boolean = CBool(r("ModeFacturationAutorise"))
                    Dim caisse As Boolean = CBool(r("ModeCaisseAutorise"))
                    Dim combine As Boolean = CBool(r("ModeCombineAutorise"))
                    If r.Read() Then Throw New InvalidOperationException("Plusieurs configurations entreprise : modes refusés.")
                    Return ModeTravailRegles.Resoudre(gestion, facturation, caisse, combine AndAlso attribution, role, droits)
                End Using
            End Using
        End Function

        Public Function DiagnostiquerRoles(utilisateurId As Integer) As Dictionary(Of Integer, String)
            Dim resultat As New Dictionary(Of Integer, String)()
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Dim noms As New Dictionary(Of Integer, String)()
                    AttributionModeService.VerifierSchema(cn, tx)
                    Dim attributions As New Dictionary(Of Integer, Boolean)()
                    Dim droits As New Dictionary(Of Integer, HashSet(Of String))()
                    ' Un seul chargement pour les roles autorises et leurs droits,
                    ' jamais une union entre roles et jamais une requete par role.
                    Using cmd As New SqlCommand("SELECT r.RoleId,r.NomRole,i.CodeInterface,ur.VenteEncaissementAutorise FROM dbo.Utilisateurs u WITH(HOLDLOCK) JOIN dbo.UtilisateurRoles ur WITH(HOLDLOCK) ON ur.UtilisateurId=u.UtilisateurId AND ur.EstActif=1 JOIN dbo.Roles r WITH(HOLDLOCK) ON r.RoleId=ur.RoleId AND r.EstActif=1 LEFT JOIN dbo.RoleInterfaces ri WITH(HOLDLOCK) ON ri.RoleId=r.RoleId LEFT JOIN dbo.InterfacesApplication i WITH(HOLDLOCK) ON i.InterfaceId=ri.InterfaceId AND i.EstActif=1 WHERE u.UtilisateurId=@u AND u.EstActif=1 AND u.EstVerrouille=0", cn, tx)
                        cmd.Parameters.AddWithValue("@u", utilisateurId)
                        Using r As SqlDataReader = cmd.ExecuteReader()
                            While r.Read()
                                Dim id As Integer = CInt(r(0))
                                If Not droits.ContainsKey(id) Then
                                    noms.Add(id, CStr(r(1)))
                                    attributions.Add(id, CBool(r(3)))
                                    droits.Add(id, New HashSet(Of String)(StringComparer.OrdinalIgnoreCase))
                                End If
                                If Not r.IsDBNull(2) Then droits(id).Add(CStr(r(2)))
                            End While
                        End Using
                    End Using
                    Dim operationnel As Boolean = False
                    For Each ensemble As HashSet(Of String) In droits.Values
                        If ensemble.Contains("FACTURIER") OrElse ensemble.Contains("CAISSE") OrElse ensemble.Contains("MODE_FACTURATION") OrElse ensemble.Contains("MODE_CAISSE") OrElse ensemble.Contains("MODE_COMBINE") Then operationnel = True
                    Next
                    If Not operationnel Then
                        For Each id As Integer In droits.Keys
                            resultat.Add(id, "Parcours historique ou administratif.")
                        Next
                        tx.Commit()
                        Return resultat
                    End If
                    Using cmd As New SqlCommand("SELECT GestionModesActive,ModeFacturationAutorise,ModeCaisseAutorise,ModeCombineAutorise FROM dbo.Parametres WITH(HOLDLOCK)", cn, tx)
                        Using r As SqlDataReader = cmd.ExecuteReader()
                            If Not r.Read() Then Throw New InvalidOperationException("Paramètres entreprise absents.")
                            Dim gestion As Boolean = CBool(r(0)), facturation As Boolean = CBool(r(1)), caisse As Boolean = CBool(r(2)), combine As Boolean = CBool(r(3))
                            If r.Read() Then Throw New InvalidOperationException("Plusieurs configurations entreprise.")
                            For Each paire As KeyValuePair(Of Integer, HashSet(Of String)) In droits
                                Try
                                    Dim modes As List(Of String) = ModeTravailRegles.Resoudre(gestion, facturation, caisse, combine AndAlso attributions(paire.Key), noms(paire.Key), paire.Value)
                                    resultat.Add(paire.Key, If(modes.Count = 0, "Parcours historique ou administratif.", "Modes accessibles : " & String.Join(" / ", modes).Replace("_ET_", " + ")) & If(gestion, Environment.NewLine & If(Not combine, "Mode combiné désactivé dans les paramètres.", ModeTravailRegles.DiagnosticCombine(paire.Value)), ""))
                                    If gestion AndAlso combine AndAlso Not attributions(paire.Key) Then resultat(paire.Key) &= Environment.NewLine & "Vente & Encaissement non attribué à ce compte pour ce rôle."
                                Catch ex As UnauthorizedAccessException
                                    resultat.Add(paire.Key, ex.Message)
                                End Try
                            Next
                        End Using
                    End Using
                    tx.Commit()
                End Using
            End Using
            Return resultat
        End Function

        Public Sub Activer(mode As String)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Dim modes As List(Of String) = Lister(cn, tx, SessionUtilisateur.UtilisateurId, SessionUtilisateur.RoleIdActif)
                    If Not modes.Contains(mode) Then Throw New UnauthorizedAccessException("Mode non autorisé pour ce rôle.")
                    Using cmd As New SqlCommand("UPDATE dbo.UtilisateurSessions SET ModeActif=@mode WHERE SessionId=@s AND UtilisateurId=@u AND RoleIdActif=@r AND Fin IS NULL AND DernierPing>=DATEADD(MINUTE,-30,SYSUTCDATETIME()) AND ModeActif IS NULL", cn, tx)
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

        Public Sub TracerSortieCombinee()
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Using cmd As New SqlCommand("SELECT Fin FROM dbo.UtilisateurSessions WITH(UPDLOCK,HOLDLOCK) WHERE SessionId=@s AND UtilisateurId=@u AND RoleIdActif=@r AND ModeActif='FACTURATION_ET_CAISSE'", cn, tx)
                        cmd.Parameters.AddWithValue("@s", SessionUtilisateur.SessionId)
                        cmd.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId)
                        cmd.Parameters.AddWithValue("@r", SessionUtilisateur.RoleIdActif)
                        Dim fin As Object = cmd.ExecuteScalar()
                        If fin Is Nothing Then Throw New UnauthorizedAccessException("La session combinée ne correspond pas au compte courant.")
                        If fin IsNot DBNull.Value Then Return
                    End Using
                    AuditMetierService.Enregistrer(cn, tx, "MODE_TRAVAIL_QUITTE", "AUTORISATIONS", "Session", SessionUtilisateur.SessionId, SessionUtilisateur.SessionId.ToString(), New With {.ModeActif = "FACTURATION_ET_CAISSE"}, New With {.ReconnexionRequise = True}, "Sortie du poste combiné après contrôle des opérations en cours", Guid.NewGuid())
                    Using cmd As New SqlCommand("UPDATE dbo.UtilisateurSessions SET Fin=SYSUTCDATETIME() WHERE SessionId=@s AND Fin IS NULL", cn, tx)
                        cmd.Parameters.AddWithValue("@s", SessionUtilisateur.SessionId)
                        If cmd.ExecuteNonQuery() <> 1 Then Throw New InvalidOperationException("Session déjà fermée.")
                    End Using
                    tx.Commit()
                End Using
            End Using
        End Sub

        Public Shared Sub VerifierAction(cn As SqlConnection, tx As SqlTransaction, ecran As String)
            If ecran <> "FACTURIER" AndAlso ecran <> "HISTORIQUE_FACTURES" AndAlso ecran <> "CAISSE" AndAlso ecran <> "ANALYSE_CAISSE_PHYSIQUE" Then Return
            Dim modes As List(Of String) = Lister(cn, tx, SessionUtilisateur.UtilisateurId, SessionUtilisateur.RoleIdActif)
            Dim mode As String
            Using cmd As New SqlCommand("SELECT ModeActif FROM dbo.UtilisateurSessions WITH(HOLDLOCK) WHERE SessionId=@s AND Fin IS NULL AND UtilisateurId=@u AND RoleIdActif=@r", cn, tx)
                cmd.Parameters.AddWithValue("@s", SessionUtilisateur.SessionId)
                cmd.Parameters.AddWithValue("@u", SessionUtilisateur.UtilisateurId)
                cmd.Parameters.AddWithValue("@r", SessionUtilisateur.RoleIdActif)
                mode = Convert.ToString(cmd.ExecuteScalar())
            End Using
            If modes.Count = 0 Then
                If Not String.IsNullOrWhiteSpace(mode) Then Throw New UnauthorizedAccessException("Le contexte de ce rôle a changé ou la gestion des modes a été désactivée. Reconnectez-vous ; aucun autre poste n'est déconnecté automatiquement.")
                Return
            End If
            If Not modes.Contains(mode) OrElse Not ModeTravailRegles.AutoriseEcran(mode, ecran) Then Throw New UnauthorizedAccessException("Cette opération est interdite dans le mode actif. Reconnectez-vous si les autorisations ont changé.")
        End Sub
    End Class
End Namespace
