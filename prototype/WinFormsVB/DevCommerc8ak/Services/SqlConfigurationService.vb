Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Globalization
Imports System.IO

Namespace DevCommerc8ak
    Public Enum SqlAuthenticationMode
        WindowsAuthentication = 0
        SqlServerAuthentication = 1
    End Enum

    Public Class SqlConnectionSettings
        Public Property Server As String = "."
        Public Property Port As Integer?
        Public Property DatabaseName As String = "CommercialMagDB"
        Public Property AuthenticationMode As SqlAuthenticationMode = SqlAuthenticationMode.WindowsAuthentication
        Public Property Username As String = String.Empty
        Public Property Password As String = String.Empty
    End Class

    Public NotInheritable Class SqlConfigurationService
        Private Shared ReadOnly _log As New ProductionLogService()
        Private Shared ReadOnly _localConfigPath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CommercialPro", "CommercialPro.exe.config")
        Private Shared _initialized As Boolean

        Private Sub New()
        End Sub

        Public Shared Sub InitializeConfiguration()
            If _initialized Then
                Return
            End If

            Try
                Dim localFolder As String = Path.GetDirectoryName(_localConfigPath)
                If Not String.IsNullOrWhiteSpace(localFolder) Then
                    Directory.CreateDirectory(localFolder)
                End If

                Dim sourceConfig As String = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
                If Not File.Exists(_localConfigPath) AndAlso File.Exists(sourceConfig) Then
                    File.Copy(sourceConfig, _localConfigPath, True)
                End If

                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", _localConfigPath)
                ConfigurationManager.RefreshSection("connectionStrings")
                ConfigurationManager.RefreshSection("appSettings")
                _initialized = True
                _log.Info("Configuration SQL initialisée: " & _localConfigPath)
            Catch ex As Exception
                _log.Error("Impossible d'initialiser la configuration SQL.", ex)
                Throw
            End Try
        End Sub

        Public Shared Function GetLocalConfigPath() As String
            Return _localConfigPath
        End Function

        Public Shared Function LoadSettings() As SqlConnectionSettings
            InitializeConfiguration()

            Dim settings As New SqlConnectionSettings()
            Dim connectionString As String = GetActiveConnectionString()
            If String.IsNullOrWhiteSpace(connectionString) Then
                Return settings
            End If

            Dim builder As New SqlConnectionStringBuilder(connectionString)
            settings.Server = ExtractServer(builder.DataSource)
            settings.Port = ExtractPort(builder.DataSource)
            settings.DatabaseName = If(String.IsNullOrWhiteSpace(builder.InitialCatalog), settings.DatabaseName, builder.InitialCatalog)
            settings.AuthenticationMode = If(builder.IntegratedSecurity, SqlAuthenticationMode.WindowsAuthentication, SqlAuthenticationMode.SqlServerAuthentication)
            If settings.AuthenticationMode = SqlAuthenticationMode.SqlServerAuthentication Then
                settings.Username = builder.UserID
                settings.Password = builder.Password
            End If
            Return settings
        End Function

        Public Shared Function BuildConnectionString(settings As SqlConnectionSettings) As String
            If settings Is Nothing Then
                Throw New ArgumentNullException(NameOf(settings))
            End If

            Dim server As String = If(settings.Server, String.Empty).Trim()
            If String.IsNullOrWhiteSpace(server) Then
                Throw New ArgumentException("Le serveur SQL est obligatoire.", NameOf(settings))
            End If

            Dim databaseName As String = If(settings.DatabaseName, String.Empty).Trim()
            If String.IsNullOrWhiteSpace(databaseName) Then
                Throw New ArgumentException("Le nom de base de données est obligatoire.", NameOf(settings))
            End If

            Dim builder As New SqlConnectionStringBuilder() With {
                .DataSource = BuildDataSource(server, settings.Port),
                .InitialCatalog = databaseName,
                .ConnectTimeout = 5,
                .MultipleActiveResultSets = False,
                .TrustServerCertificate = True,
                .PersistSecurityInfo = False,
                .ApplicationName = "Commercial Pro"
            }

            If settings.AuthenticationMode = SqlAuthenticationMode.WindowsAuthentication Then
                builder.IntegratedSecurity = True
            Else
                builder.IntegratedSecurity = False
                builder.UserID = If(settings.Username, String.Empty).Trim()
                builder.Password = If(settings.Password, String.Empty)
            End If

            Return builder.ConnectionString
        End Function

        Public Shared Function TestConnection(settings As SqlConnectionSettings, Optional ByRef errorMessage As String = Nothing) As Boolean
            Try
                Dim connectionString As String = BuildConnectionString(settings)
                Dim builder As New SqlConnectionStringBuilder(connectionString) With {
                    .ConnectTimeout = 5
                }

                Using cn As New SqlConnection(builder.ConnectionString)
                    cn.Open()
                End Using

                errorMessage = String.Empty
                _log.Info("Test de connexion SQL réussi pour " & settings.Server)
                Return True
            Catch ex As Exception
                errorMessage = ex.Message
                _log.Error("Test de connexion SQL échoué.", ex)
                Return False
            End Try
        End Function

        Public Shared Function HasValidConnection(Optional ByRef errorMessage As String = Nothing) As Boolean
            InitializeConfiguration()

            Dim settings As SqlConnectionSettings = LoadSettings()
            Return TestConnection(settings, errorMessage)
        End Function

        Public Shared Sub SaveSettings(settings As SqlConnectionSettings)
            InitializeConfiguration()

            Dim config As Configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
            Dim connectionString As String = BuildConnectionString(settings)
            Dim section As ConnectionStringsSection = config.ConnectionStrings
            Dim existing As ConnectionStringSettings = section.ConnectionStrings("CommercialMagDB")
            If existing Is Nothing Then
                section.ConnectionStrings.Add(New ConnectionStringSettings("CommercialMagDB", connectionString, "System.Data.SqlClient"))
            Else
                existing.ConnectionString = connectionString
                existing.ProviderName = "System.Data.SqlClient"
            End If

            section.SectionInformation.ProtectSection("DataProtectionConfigurationProvider")
            section.SectionInformation.ForceSave = True
            config.Save(ConfigurationSaveMode.Full)
            ConfigurationManager.RefreshSection("connectionStrings")
            _log.Info("Configuration SQL enregistrée dans le fichier local.")
        End Sub

        Private Shared Function GetActiveConnectionString() As String
            Dim setting As ConnectionStringSettings = ConfigurationManager.ConnectionStrings("CommercialMagDB")
            If setting Is Nothing OrElse String.IsNullOrWhiteSpace(setting.ConnectionString) Then
                Return String.Empty
            End If
            Return setting.ConnectionString.Trim()
        End Function

        Private Shared Function BuildDataSource(server As String, port As Integer?) As String
            Dim trimmed As String = server.Trim()
            If port.HasValue AndAlso port.Value > 0 AndAlso Not trimmed.Contains(","c) Then
                Return trimmed & "," & port.Value.ToString(CultureInfo.InvariantCulture)
            End If
            Return trimmed
        End Function

        Private Shared Function ExtractServer(dataSource As String) As String
            If String.IsNullOrWhiteSpace(dataSource) Then
                Return "."
            End If

            Dim trimmed As String = dataSource.Trim()
            Dim index As Integer = trimmed.LastIndexOf(","c)
            If index > 0 Then
                Return trimmed.Substring(0, index).Trim()
            End If
            Return trimmed
        End Function

        Private Shared Function ExtractPort(dataSource As String) As Integer?
            If String.IsNullOrWhiteSpace(dataSource) Then
                Return Nothing
            End If

            Dim trimmed As String = dataSource.Trim()
            Dim index As Integer = trimmed.LastIndexOf(","c)
            If index <= 0 Then
                Return Nothing
            End If

            Dim brutPort As String = trimmed.Substring(index + 1).Trim()
            Dim port As Integer
            If Integer.TryParse(brutPort, NumberStyles.Integer, CultureInfo.InvariantCulture, port) Then
                Return port
            End If
            Return Nothing
        End Function
    End Class
End Namespace
