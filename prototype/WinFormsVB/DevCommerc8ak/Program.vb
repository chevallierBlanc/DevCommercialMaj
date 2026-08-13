Imports System
Imports System.Windows.Forms
Imports System.Threading
Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Module Program
        <STAThread>
        Public Sub Main()
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)
            AddHandler Application.ThreadException, AddressOf HandleThreadException
            AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf HandleUnhandledException

            Dim log As New ProductionLogService()

            Try
                Application.Run(New StartupApplicationContext())
            Catch ex As Exception
                log.Error("Program", "Main", "Erreur fatale au démarrage de l'application.", ex)
                MessageBox.Show("Impossible de démarrer l'application." & Environment.NewLine & ex.Message,
                                "Erreur de démarrage",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub HandleThreadException(sender As Object, e As ThreadExceptionEventArgs)
            Dim log As New ProductionLogService()
            log.Error("Program", "HandleThreadException", "Erreur WinForms non gérée.", e.Exception)
            MessageBox.Show("Une erreur inattendue est survenue. Consultez le journal local pour plus de détails.",
                            "Erreur",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
        End Sub

        Private Sub HandleUnhandledException(sender As Object, e As UnhandledExceptionEventArgs)
            Dim ex As Exception = TryCast(e.ExceptionObject, Exception)
            If ex Is Nothing Then
                Return
            End If

            Dim log As New ProductionLogService()
            log.Error("Program", "HandleUnhandledException", "Erreur AppDomain non gérée.", ex)
        End Sub

        Private NotInheritable Class StartupApplicationContext
            Inherits ApplicationContext

            Private ReadOnly _log As New ProductionLogService()
            Private _mainForm As MainForm

            Public Sub New()
                DemarrerApplication()
            End Sub

            Private Sub DemarrerApplication()
                Try
                    _log.Info("Startup", "Program", "Initialisation du démarrage de l'application.")
                    SqlConfigurationService.InitializeConfiguration()

                    If Not AssurerConnexionSql() Then
                        ExitThread()
                        Return
                    End If

                    SchemaMigrationService.ApplyPendingMigrations()
                    _log.Info("Startup", "Program", "Migrations de schéma appliquées ou déjà à jour.")

                    If Not AssurerCompteAdministrateurInitial() Then
                        ExitThread()
                        Return
                    End If

                    AfficherSplash()
                    AfficherLogin()
                Catch ex As Exception
                    _log.Error("Startup", "Program", "Erreur lors du démarrage initial de l'application.", ex)
                    MessageBox.Show("Impossible de démarrer l'application." & Environment.NewLine & ex.Message,
                                    "Erreur de démarrage",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error)
                    ExitThread()
                End Try
            End Sub

            Private Function AssurerConnexionSql() As Boolean
                Dim erreurSql As String = Nothing
                If SqlConfigurationService.HasValidConnection(erreurSql) Then
                    _log.Info("SQLConnection", "Program", "Connexion SQL valide au démarrage.")
                    Return True
                End If

                _log.Warn("SQLConnection", "Program", "Connexion SQL invalide, ouverture de FormConfigurationSQL.")
                Using configForm As New FormConfigurationSQL()
                    If configForm.ShowDialog() <> DialogResult.OK Then
                        _log.Warn("SQLConnection", "Program", "Configuration SQL annulée par l'utilisateur.")
                        Return False
                    End If
                End Using

                If SqlConfigurationService.HasValidConnection(erreurSql) Then
                    _log.Info("SQLConnection", "Program", "Connexion SQL valide après configuration.")
                    Return True
                End If

                MessageBox.Show("Aucune connexion SQL valide n'est disponible." &
                                If(String.IsNullOrWhiteSpace(erreurSql), String.Empty, Environment.NewLine & erreurSql),
                                "Configuration SQL",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning)
                _log.Warn("SQLConnection", "Program", "Connexion SQL toujours invalide après configuration.")
                Return False
            End Function

            Private Function AssurerCompteAdministrateurInitial() As Boolean
                Try
                    Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                    Dim dal As New DAL(cs)

                    If Not TableExiste(dal, "Utilisateurs") Then
                        Throw New InvalidOperationException("La table Utilisateurs est absente.")
                    End If
                    If Not TableExiste(dal, "Roles") Then
                        Throw New InvalidOperationException("La table Roles est absente.")
                    End If
                    If Not TableExiste(dal, "UtilisateurRoles") Then
                        Throw New InvalidOperationException("La table UtilisateurRoles est absente.")
                    End If

                    Dim roleRepo As New RoleRepository(dal)
                    roleRepo.AssurerRole("ADMIN")
                    roleRepo.AssurerRole("SUPERADMIN")

                    If UtilisateurActifExiste(dal) Then
                        If Not SuperAdminExiste(dal) Then
                            _log.Warn("InitAdmin", "Program", "Aucun SUPERADMIN actif détecté. Démarrage conservé pour compatibilité avec les comptes existants.")
                        Else
                            _log.Info("InitAdmin", "Program", "Un compte SUPERADMIN actif existe déjà.")
                        End If
                        Return True
                    End If

                    Dim utilisateurRepo As New UtilisateurRepository(dal)
                    Dim sessionRepo As New SessionRepository(dal)
                    Dim service As New UtilisateurService(utilisateurRepo, roleRepo, sessionRepo)

                    Using bootstrap As New FormulaireBootstrapSuperAdmin(service)
                        If bootstrap.ShowDialog() <> DialogResult.OK Then
                            _log.Warn("InitAdmin", "Program", "Création du SUPERADMIN initial annulée.")
                            MessageBox.Show("La création du compte SUPERADMIN initial est obligatoire pour démarrer l'application.",
                                            "Initialisation SUPERADMIN",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Warning)
                            Return False
                        End If
                    End Using

                    _log.Info("InitAdmin", "Program", "Compte SUPERADMIN initial créé via assistant sécurisé.")
                    Return True
                Catch ex As Exception
                    _log.Error("InitAdmin", "Program", "Impossible d'initialiser le compte administrateur initial.", ex)
                    MessageBox.Show("Impossible d'initialiser le compte administrateur initial." & Environment.NewLine & ex.Message,
                                    "Initialisation administrateur",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error)
                    Return False
                End Try
            End Function

            Private Shared Function TableExiste(dal As DAL, nomTable As String) As Boolean
                Dim p As New List(Of SqlParameter) From {
                    New SqlParameter("@NomTable", nomTable)
                }
                Dim resultat As Object = dal.ExecuterScalaire("SELECT CASE WHEN OBJECT_ID('dbo.' + @NomTable, 'U') IS NULL THEN 0 ELSE 1 END", CommandType.Text, p)
                Return resultat IsNot Nothing AndAlso Convert.ToInt32(resultat) = 1
            End Function

            Private Shared Function UtilisateurActifExiste(dal As DAL) As Boolean
                Dim resultat As Object = dal.ExecuterScalaire("SELECT COUNT(*) FROM dbo.Utilisateurs WHERE ISNULL(EstActif,1)=1", CommandType.Text, Nothing)
                Return resultat IsNot Nothing AndAlso Convert.ToInt32(resultat) > 0
            End Function

            Private Shared Function SuperAdminExiste(dal As DAL) As Boolean
                Dim sql As String =
                    "SELECT COUNT(*) " &
                    "FROM Utilisateurs u " &
                    "INNER JOIN UtilisateurRoles ur ON ur.UtilisateurId = u.UtilisateurId " &
                    "INNER JOIN Roles r ON r.RoleId = ur.RoleId " &
                    "WHERE ISNULL(u.EstActif,1)=1 " &
                    "AND UPPER(LTRIM(RTRIM(r.NomRole))) = 'SUPERADMIN'"
                Dim resultat As Object = dal.ExecuterScalaire(sql, CommandType.Text, Nothing)
                Return resultat IsNot Nothing AndAlso Convert.ToInt32(resultat) > 0
            End Function

            Private Sub AfficherSplash()
                Using splash As New SplashForm()
                    splash.ShowDialog()
                End Using
            End Sub

            Private Sub AfficherLogin()
                If ApplicationLifecycle.IsShutdownRequested() Then
                    ExitThread()
                    Return
                End If

                ApplicationLifecycle.ConsumeReturnToLoginRequested()
                Using login As New LoginForm()
                    Dim resultat As DialogResult = login.ShowDialog()
                    If resultat <> DialogResult.OK Then
                        _log.Info("LoginForm", "Program", "Fermeture du login sans authentification réussie.")
                        ExitThread()
                        Return
                    End If
                End Using

                OuvrirMainForm()
            End Sub

            Private Sub OuvrirMainForm()
                _mainForm = New MainForm()
                Me.MainForm = _mainForm
                AddHandler _mainForm.FormClosed, AddressOf MainFormClosedHandler
                _mainForm.Show()
            End Sub

            Private Sub MainFormClosedHandler(sender As Object, e As FormClosedEventArgs)
                If _mainForm IsNot Nothing Then
                    RemoveHandler _mainForm.FormClosed, AddressOf MainFormClosedHandler
                    _mainForm = Nothing
                    Me.MainForm = Nothing
                End If

                If ApplicationLifecycle.ConsumeReturnToLoginRequested() AndAlso Not ApplicationLifecycle.IsShutdownRequested() Then
                    AfficherLogin()
                Else
                    ExitThread()
                End If
            End Sub
        End Class
    End Module

    Public Module ApplicationLifecycle
        Private ReadOnly _syncRoot As New Object()
        Private _returnToLoginRequested As Boolean
        Private _shutdownRequested As Boolean

        Public Sub RequestReturnToLogin()
            SyncLock _syncRoot
                _returnToLoginRequested = True
                _shutdownRequested = False
            End SyncLock

            StopBackgroundServices()
        End Sub

        Public Function IsReturnToLoginRequested() As Boolean
            SyncLock _syncRoot
                Return _returnToLoginRequested
            End SyncLock
        End Function

        Public Function ConsumeReturnToLoginRequested() As Boolean
            SyncLock _syncRoot
                Dim requested As Boolean = _returnToLoginRequested
                _returnToLoginRequested = False
                Return requested
            End SyncLock
        End Function

        Public Sub StopBackgroundServices()
            Try
                OfflineSyncScheduler.StopScheduler()
            Catch
            End Try
        End Sub

        Public Sub RequestShutdown()
            SyncLock _syncRoot
                If _shutdownRequested Then
                    Return
                End If
                _shutdownRequested = True
                _returnToLoginRequested = False
            End SyncLock

            StopBackgroundServices()

            Try
                Application.Exit()
            Catch
            End Try
        End Sub

        Public Function IsShutdownRequested() As Boolean
            SyncLock _syncRoot
                Return _shutdownRequested
            End SyncLock
        End Function
    End Module
End Namespace
