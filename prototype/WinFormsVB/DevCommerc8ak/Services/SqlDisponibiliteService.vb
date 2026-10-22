Option Strict On
Option Explicit On
Imports System
Imports System.Data.SqlClient
Imports System.Diagnostics
Imports System.Threading
Imports System.Threading.Tasks

Namespace DevCommerc8ak
    Public Class ResultatDisponibiliteSql
        Public Property Disponible As Boolean
        Public Property Categorie As String
        Public Property Message As String
        Public Property VerifieLeUtc As DateTime
        Public Property Correlation As Guid
        Public Property DureeMs As Long
    End Class
    Public Class SqlDisponibiliteService
        Private ReadOnly verrou As New SemaphoreSlim(1, 1)
        Private ReadOnly temporisation As New TemporisationSondeSql
        Private dernier As ResultatDisponibiliteSql
        Private derniereChaine As String
        Public Async Function VerifierAsync(chaine As String) As Task(Of ResultatDisponibiliteSql)
            Await verrou.WaitAsync().ConfigureAwait(False)
            Try
                If dernier IsNot Nothing AndAlso String.Equals(chaine, derniereChaine, StringComparison.Ordinal) AndAlso DateTime.UtcNow < temporisation.ProchaineVerificationUtc Then Return dernier
                Dim resultat As ResultatDisponibiliteSql = Await Task.Run(Function() TesterUneFois(chaine)).ConfigureAwait(False)
                If dernier Is Nothing OrElse dernier.Disponible <> resultat.Disponible Then
                    Dim log As New ProductionLogService()
                    log.Info("SQL", "Disponibilite", "UTC=" & resultat.VerifieLeUtc.ToString("O") & " | Disponible=" & resultat.Disponible.ToString() & " | Correlation=" & resultat.Correlation.ToString("D"))
                End If
                dernier = resultat
                derniereChaine = chaine
                temporisation.Enregistrer(resultat.Disponible, DateTime.UtcNow)
                Return resultat
            Finally
                verrou.Release()
            End Try
        End Function
        Public Shared Function TesterUneFois(chaine As String) As ResultatDisponibiliteSql
            Dim resultat As New ResultatDisponibiliteSql With {.Correlation = Guid.NewGuid()}
            Dim duree As Stopwatch = Stopwatch.StartNew()
            Dim ouverture As Boolean = True
            Try
                If String.IsNullOrWhiteSpace(chaine) Then Throw New ArgumentException("Configuration absente.")
                Dim options As New SqlConnectionStringBuilder(chaine)
                ' Pool de sonde isole et borne, sans toucher au pool des transactions.
                options.ApplicationName = "Commercial Pro - Sonde SQL"
                options.MinPoolSize = 0 : options.MaxPoolSize = 1
                options.ConnectTimeout = If(options.ConnectTimeout = 0, 5, Math.Min(options.ConnectTimeout, 5))
                options.ConnectRetryCount = 0
                Using cn As New SqlConnection(options.ConnectionString)
                    cn.Open()
                    ouverture = False
                    Using cmd As New SqlCommand("SELECT 1", cn)
                        cmd.CommandTimeout = 3
                        resultat.Disponible = Convert.ToInt32(cmd.ExecuteScalar()) = 1
                    End Using
                End Using
                resultat.Categorie = "DISPONIBLE" : resultat.Message = "SQL disponible. Aucune opération commerciale n'a été rejouée."
            Catch ex As SqlException
                resultat.Disponible = False
                resultat.Categorie = SqlDiagnosticRegles.Classer(ex.Number, ouverture)
                resultat.Message = SqlDiagnosticRegles.Message(resultat.Categorie)
                SqlDiagnosticService.Signaler("SondeDisponibilite", ex, ouverture, False, duree.ElapsedMilliseconds, resultat.Correlation)
            Catch ex As ArgumentException
                resultat.Disponible = False
                resultat.Categorie = "CONFIGURATION" : resultat.Message = "Configuration SQL invalide. Contactez un administrateur."
            Catch ex As InvalidOperationException
                resultat.Disponible = False
                resultat.Categorie = "RESSOURCES_CLIENT" : resultat.Message = "Connexion SQL indisponible côté poste. Faites vérifier la configuration et les ressources."
            Finally
                resultat.VerifieLeUtc = DateTime.UtcNow : resultat.DureeMs = duree.ElapsedMilliseconds
            End Try
            Return resultat
        End Function
    End Class
End Namespace
