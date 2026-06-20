Imports System
Imports System.Windows.Forms
Imports System.Threading

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
                SqlConfigurationService.InitializeConfiguration()

                Dim erreurSql As String = Nothing
                If Not SqlConfigurationService.HasValidConnection(erreurSql) Then
                    Using configForm As New FormConfigurationSQL()
                        If configForm.ShowDialog() <> DialogResult.OK Then
                            log.Warn("Configuration SQL annulée au premier démarrage.")
                            Return
                        End If
                    End Using

                    If Not SqlConfigurationService.HasValidConnection(erreurSql) Then
                        MessageBox.Show("Aucune connexion SQL valide n'est disponible. " & If(String.IsNullOrWhiteSpace(erreurSql), String.Empty, Environment.NewLine & erreurSql),
                                        "Configuration SQL",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning)
                        log.Warn("Connexion SQL invalide après passage par le formulaire de configuration.")
                        Return
                    End If
                End If

                Using splash As New SplashForm()
                    splash.Show()
                    Application.DoEvents()
                    Thread.Sleep(2000)
                    splash.Close()
                End Using

                Application.Run(New LoginForm())
            Catch ex As Exception
                log.Error("Erreur fatale au démarrage de l'application.", ex)
                MessageBox.Show("Impossible de démarrer l'application." & Environment.NewLine & ex.Message,
                                "Erreur de démarrage",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub HandleThreadException(sender As Object, e As ThreadExceptionEventArgs)
            Dim log As New ProductionLogService()
            log.Error("Erreur WinForms non gérée.", e.Exception)
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
            log.Error("Erreur AppDomain non gérée.", ex)
        End Sub
    End Module
End Namespace
