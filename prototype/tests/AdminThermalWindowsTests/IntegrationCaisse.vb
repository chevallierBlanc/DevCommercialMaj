Option Strict On
Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports DevCommerc8ak.DevCommerc8ak

Module IntegrationCaisse
    ' Banc de lecture facultatif. Aucun fixture ni droit n'est cree par ce banc.
    Friend Sub ExecuterSiConfigure()
        If Environment.GetEnvironmentVariable("ERP_THERMIQUE_SQL_ENABLE") <> "YES" Then
            Console.WriteLine("NON EXECUTE : integration SQL (activation explicite et copie _THERMIQUE_TEST requises).")
            Return
        End If
        Dim cs As String = Environment.GetEnvironmentVariable("ERP_THERMIQUE_TEST_CONNECTION")
        Using cn As New SqlConnection(cs)
            cn.Open()
            If Not cn.Database.EndsWith("_THERMIQUE_TEST", StringComparison.OrdinalIgnoreCase) Then Throw New InvalidOperationException("Copie isolee _THERMIQUE_TEST obligatoire.")
            Dim sessionId As Integer = Identifiant("SESSION")
            Using cmd As New SqlCommand("SELECT s.UtilisateurId,s.RoleIdActif,s.ModeActif,r.NomRole,u.NomUtilisateur FROM dbo.UtilisateurSessions s JOIN dbo.Roles r ON r.RoleId=s.RoleIdActif JOIN dbo.Utilisateurs u ON u.UtilisateurId=s.UtilisateurId WHERE s.SessionId=@id AND s.Fin IS NULL AND s.DernierPing>=DATEADD(MINUTE,-30,SYSUTCDATETIME())", cn)
                cmd.Parameters.AddWithValue("@id", sessionId)
                Using r As SqlDataReader = cmd.ExecuteReader()
                    If Not r.Read() Then Throw New InvalidOperationException("Session de test active requise.")
                    SessionUtilisateur.UtilisateurId = CInt(r(0)) : SessionUtilisateur.RoleIdActif = CInt(r(1))
                    SessionUtilisateur.ModeActif = Convert.ToString(r(2)) : SessionUtilisateur.Role = CStr(r(3))
                    SessionUtilisateur.NomRoleActif = CStr(r(3)) : SessionUtilisateur.NomUtilisateur = CStr(r(4))
                    SessionUtilisateur.SessionId = sessionId
                End Using
            End Using
            Try
                Dim avant As String = Empreinte(cn)
                Dim service As New JustificatifCaisseService(New DAL(cs))
                Dim payee As Integer = Identifiant("FACTURE_PAYEE")
                Dim impayee As Integer = Identifiant("FACTURE_IMPAYEE")
                Dim premier As FacturePrintData = service.Lire(payee)
                Dim second As FacturePrintData = service.Lire(payee)
                If premier.Statut <> "PAYEE" OrElse premier.Numero <> second.Numero OrElse premier.Total <> second.Total Then Throw New Exception("Reimpression incoherente.")
                Refus(Sub() service.Lire(impayee), False)
                SessionUtilisateur.SessionId = 0
                Refus(Sub() service.Lire(payee), True)
                SessionUtilisateur.SessionId = sessionId
                SessionUtilisateur.RoleIdActif = 0
                Refus(Sub() service.Lire(payee), True)
                If Empreinte(cn) <> avant Then Throw New Exception("Donnees modifiees pendant la recette. Utiliser une copie sans autres operations concurrentes.")
                Console.WriteLine("PASS SQL : paiement confirme, impaye refuse, session/role refuses, relecture stable et comptages/montants inchanges.")
            Finally
                SessionUtilisateur.Reinitialiser()
            End Try
        End Using
    End Sub
    Private Function Identifiant(nom As String) As Integer
        Dim id As Integer
        If Not Integer.TryParse(Environment.GetEnvironmentVariable("ERP_THERMIQUE_TEST_" & nom), id) OrElse id <= 0 Then Throw New ArgumentException("Identifiant de test manquant : " & nom)
        Return id
    End Function
    Private Sub Refus(action As Action, autorisation As Boolean)
        Try
            action()
        Catch ex As UnauthorizedAccessException When autorisation
            Return
        Catch ex As InvalidOperationException When Not autorisation
            Return
        End Try
        Throw New Exception("Refus attendu non observe.")
    End Sub
    Private Function Empreinte(cn As SqlConnection) As String
        Using cmd As New SqlCommand("SELECT (SELECT COUNT_BIG(*) FROM dbo.FacturesVente),(SELECT COUNT_BIG(*) FROM dbo.Paiements),(SELECT COUNT_BIG(*) FROM dbo.StockSortie),(SELECT COUNT_BIG(*) FROM dbo.JournalAudit),(SELECT ISNULL(SUM(Montant),0) FROM dbo.Paiements),(SELECT ISNULL(SUM(QuantiteBase),0) FROM dbo.StockSortie)", cn)
            Using r As SqlDataReader = cmd.ExecuteReader()
                r.Read()
                Dim valeurs(r.FieldCount - 1) As String
                For i As Integer = 0 To valeurs.Length - 1
                    valeurs(i) = Convert.ToString(r(i), Globalization.CultureInfo.InvariantCulture)
                Next
                Return String.Join("|", valeurs)
            End Using
        End Using
    End Function
End Module
