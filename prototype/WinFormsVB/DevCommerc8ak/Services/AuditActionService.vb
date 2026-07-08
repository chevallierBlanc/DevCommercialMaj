Option Strict On
Option Explicit On

Imports System
Imports System.Configuration

Namespace DevCommerc8ak
    Public NotInheritable Class AuditActionService
        Private Sub New()
        End Sub

        Public Shared Sub Enregistrer(moduleName As String, actionName As String, description As String, Optional statut As String = "OK")
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim repo As New SuperAdminRepository(New DAL(cs))
                repo.AjouterAuditAction(
                    ObtenirUtilisateurCourant(),
                    ObtenirRoleCourant(),
                    moduleName,
                    actionName,
                    description,
                    Environment.MachineName,
                    statut)
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Warn("AuditActionService", "Enregistrer", "Audit non enregistré : " & ex.Message)
            End Try
        End Sub

        Private Shared Function ObtenirUtilisateurCourant() As String
            If Not String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur) Then
                Return SessionUtilisateur.NomUtilisateur.Trim()
            End If

            Return "SYSTEM"
        End Function

        Private Shared Function ObtenirRoleCourant() As String
            If Not String.IsNullOrWhiteSpace(SessionUtilisateur.Role) Then
                Return SessionUtilisateur.Role.Trim()
            End If

            Return "SYSTEM"
        End Function
    End Class
End Namespace
