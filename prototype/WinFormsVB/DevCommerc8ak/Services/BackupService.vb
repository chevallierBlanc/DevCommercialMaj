Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data.SqlClient
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Xml.Linq

Namespace DevCommerc8ak
    Public Class BackupSettings
        Public Property Enabled As Boolean
        Public Property IntervalMinutes As Integer
        Public Property BackupFolder As String
        Public Property BackupBeforeExit As Boolean
    End Class

    Public Class BackupResult
        Public Property Success As Boolean
        Public Property FilePath As String
        Public Property Message As String
        Public Property BackedUpAt As DateTime
    End Class

    Public Class BackupService
        Private ReadOnly _connectionString As String
        Private ReadOnly _settingsFilePath As String
        Private ReadOnly _log As New ProductionLogService()

        Public Sub New(Optional connectionString As String = Nothing)
            _connectionString = If(String.IsNullOrWhiteSpace(connectionString),
                                   ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString,
                                   connectionString)
            Dim settingsFolder As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CommercialPro")
            _settingsFilePath = Path.Combine(settingsFolder, "backup-settings.xml")
        End Sub

        Public Function ObtenirDossierParDefaut() As String
            Dim dossierConfig As String = LireAppSetting("BackupFolder", String.Empty)
            If Not String.IsNullOrWhiteSpace(dossierConfig) Then
                Return dossierConfig.Trim()
            End If

            Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CommercialPro", "Backups")
        End Function

        Public Function ChargerParametres() As BackupSettings
            Try
                If Not File.Exists(_settingsFilePath) Then
                    Return CreerParametresParDefaut()
                End If

                Dim doc As XDocument = XDocument.Load(_settingsFilePath)
                Dim racine As XElement = doc.Root
                If racine Is Nothing Then
                    Return CreerParametresParDefaut()
                End If

                Return New BackupSettings With {
                    .Enabled = LireBool(racine, "Enabled", LireBoolAppSetting("BackupEnabled", True)),
                    .IntervalMinutes = LireInt(racine, "IntervalMinutes", LireIntAppSetting("BackupIntervalMinutes", 240)),
                    .BackupFolder = LireString(racine, "BackupFolder", ObtenirDossierParDefaut()),
                    .BackupBeforeExit = LireBool(racine, "BackupBeforeExit", LireBoolAppSetting("BackupBeforeExit", True))
                }
            Catch ex As Exception
                _log.Warn("BackupService", "ChargerParametres", "Paramètres de sauvegarde illisibles. Utilisation des valeurs par défaut. " & ex.Message)
                Return CreerParametresParDefaut()
            End Try
        End Function

        Public Sub EnregistrerParametres(settings As BackupSettings)
            If settings Is Nothing Then Throw New ArgumentNullException(NameOf(settings))

            Dim dossier As String = Path.GetDirectoryName(_settingsFilePath)
            If Not String.IsNullOrWhiteSpace(dossier) Then
                Directory.CreateDirectory(dossier)
            End If

            Dim doc As New XDocument(
                New XElement("BackupSettings",
                    New XElement("Enabled", settings.Enabled.ToString(CultureInfo.InvariantCulture)),
                    New XElement("IntervalMinutes", Math.Max(1, settings.IntervalMinutes).ToString(CultureInfo.InvariantCulture)),
                    New XElement("BackupFolder", If(settings.BackupFolder, String.Empty)),
                    New XElement("BackupBeforeExit", settings.BackupBeforeExit.ToString(CultureInfo.InvariantCulture))
                )
            )
            doc.Save(_settingsFilePath)
        End Sub

        Public Function ExecuterSauvegarde(Optional dossierCible As String = Nothing) As BackupResult
            Dim resultat As New BackupResult() With {.BackedUpAt = DateTime.Now}

            Try
                Dim builder As New SqlConnectionStringBuilder(_connectionString)
                Dim database As String = If(String.IsNullOrWhiteSpace(builder.InitialCatalog), "CommercialMagDB", builder.InitialCatalog)
                Dim dossierPrincipal As String = If(String.IsNullOrWhiteSpace(dossierCible), ObtenirDossierParDefaut(), dossierCible).Trim()
                Dim dossierFallback As String = ObtenirDossierFallback()

                If ExecuterSauvegardeVersDossier(database, dossierPrincipal, resultat, False) Then
                    _log.Info("BackupService", "ExecuterSauvegarde", "Sauvegarde réussie dans le dossier principal : " & resultat.FilePath)
                    Return resultat
                End If

                If Not String.Equals(dossierPrincipal, dossierFallback, StringComparison.OrdinalIgnoreCase) Then
                    Dim resultatFallback As New BackupResult() With {.BackedUpAt = DateTime.Now}
                    If ExecuterSauvegardeVersDossier(database, dossierFallback, resultatFallback, True) Then
                        _log.Warn("BackupService", "ExecuterSauvegarde", "Sauvegarde effectuée dans le dossier de secours : " & resultatFallback.FilePath)
                        Return resultatFallback
                    End If

                    If Not String.IsNullOrWhiteSpace(resultatFallback.Message) Then
                        resultat.Message = resultatFallback.Message
                    End If
                End If

                If String.IsNullOrWhiteSpace(resultat.Message) Then
                    resultat.Message = "Sauvegarde impossible."
                End If
                _log.Error("BackupService", "ExecuterSauvegarde", resultat.Message, Nothing)
            Catch ex As Exception
                resultat.Success = False
                resultat.Message = ex.Message
                _log.Error("BackupService", "ExecuterSauvegarde", "Erreur lors de la sauvegarde SQL.", ex)
            End Try

            Return resultat
        End Function

        Private Function ExecuterSauvegardeVersDossier(database As String, dossier As String, resultat As BackupResult, estFallback As Boolean) As Boolean
            Try
                If Not PeutEcrireDansDossier(dossier) Then
                    resultat.Success = False
                    If estFallback Then
                        resultat.Message = "Accès refusé au dossier de secours : " & dossier
                    Else
                        resultat.Message = "Accès refusé au dossier de sauvegarde : " & dossier
                    End If
                    Return False
                End If

                Dim nomFichier As String = "ERPCommercial_" & DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) & "_" & Guid.NewGuid().ToString("N").Substring(0, 8) & ".bak"
                Dim cheminComplet As String = Path.Combine(dossier, nomFichier)
                Dim cheminEchappe As String = cheminComplet.Replace("'", "''")
                Dim databaseEchappee As String = database.Replace("]", "]]")

                Using cn As New SqlConnection(_connectionString)
                    cn.Open()
                    Using cmd As New SqlCommand("BACKUP DATABASE [" & databaseEchappee & "] TO DISK = N'" & cheminEchappe & "' WITH INIT, COPY_ONLY, COMPRESSION, STATS = 10;", cn)
                        cmd.CommandTimeout = 0
                        cmd.ExecuteNonQuery()
                    End Using
                End Using

                resultat.Success = True
                resultat.FilePath = cheminComplet
                If estFallback Then
                    resultat.Message = "Dossier principal inaccessible. Sauvegarde effectuée dans le dossier de secours."
                Else
                    resultat.Message = "Sauvegarde réalisée avec succès."
                End If
                _log.Info("BackupService", "ExecuterSauvegardeVersDossier", resultat.Message)
                Return True
            Catch ex As Exception
                resultat.Success = False
                resultat.Message = ex.Message
                If estFallback Then
                    _log.Warn("BackupService", "ExecuterSauvegardeVersDossier", "Échec dossier de secours : " & dossier & " | " & ex.Message)
                Else
                    _log.Warn("BackupService", "ExecuterSauvegardeVersDossier", "Échec dossier principal : " & dossier & " | " & ex.Message)
                End If
                Return False
            End Try
        End Function

        Private Function PeutEcrireDansDossier(dossier As String) As Boolean
            Try
                If String.IsNullOrWhiteSpace(dossier) Then
                    Return False
                End If

                Directory.CreateDirectory(dossier)
                Dim fichierTest As String = Path.Combine(dossier, Path.GetRandomFileName())
                Using fs As New FileStream(fichierTest, FileMode.Create, FileAccess.Write, FileShare.None)
                    fs.WriteByte(0)
                End Using
                File.Delete(fichierTest)
                Return True
            Catch ex As Exception
                _log.Warn("BackupService", "PeutEcrireDansDossier", "Dossier de sauvegarde non accessible : " & If(dossier, String.Empty) & " | " & ex.Message)
                Return False
            End Try
        End Function

        Private Function ObtenirDossierFallback() As String
            Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CommercialPro", "Backups")
        End Function

        Public Function ObtenirDerniereSauvegarde(Optional dossierCible As String = Nothing) As String
            Dim dossier As String = If(String.IsNullOrWhiteSpace(dossierCible), ObtenirDossierParDefaut(), dossierCible)
            If String.IsNullOrWhiteSpace(dossier) OrElse Not Directory.Exists(dossier) Then
                dossier = ObtenirDossierFallback()
            End If

            If String.IsNullOrWhiteSpace(dossier) OrElse Not Directory.Exists(dossier) Then
                Return String.Empty
            End If

            Dim dernier As FileInfo = Nothing
            For Each fichier As String In Directory.GetFiles(dossier, "ERPCommercial_*.bak")
                Dim info As New FileInfo(fichier)
                If dernier Is Nothing OrElse info.LastWriteTimeUtc > dernier.LastWriteTimeUtc Then
                    dernier = info
                End If
            Next

            If dernier Is Nothing Then
                Return String.Empty
            End If
            Return dernier.FullName
        End Function

        Private Function CreerParametresParDefaut() As BackupSettings
            Return New BackupSettings With {
                .Enabled = LireBoolAppSetting("BackupEnabled", True),
                .IntervalMinutes = LireIntAppSetting("BackupIntervalMinutes", 240),
                .BackupFolder = ObtenirDossierParDefaut(),
                .BackupBeforeExit = LireBoolAppSetting("BackupBeforeExit", True)
            }
        End Function

        Private Function LireAppSetting(nom As String, valeurDefaut As String) As String
            Dim brut As String = ConfigurationManager.AppSettings(nom)
            If String.IsNullOrWhiteSpace(brut) Then Return valeurDefaut
            Return brut.Trim()
        End Function

        Private Function LireIntAppSetting(nom As String, valeurDefaut As Integer) As Integer
            Dim brut As String = LireAppSetting(nom, valeurDefaut.ToString(CultureInfo.InvariantCulture))
            Dim valeur As Integer
            If Integer.TryParse(brut, NumberStyles.Integer, CultureInfo.InvariantCulture, valeur) Then
                Return valeur
            End If
            If Integer.TryParse(brut, valeur) Then
                Return valeur
            End If
            Return valeurDefaut
        End Function

        Private Function LireBoolAppSetting(nom As String, valeurDefaut As Boolean) As Boolean
            Dim brut As String = LireAppSetting(nom, valeurDefaut.ToString(CultureInfo.InvariantCulture))
            Dim valeur As Boolean
            If Boolean.TryParse(brut, valeur) Then
                Return valeur
            End If
            Return valeurDefaut
        End Function

        Private Function LireString(racine As XElement, nom As String, valeurDefaut As String) As String
            Dim element As XElement = racine.Element(nom)
            If element Is Nothing Then Return valeurDefaut
            Dim valeur As String = Convert.ToString(element.Value)
            If String.IsNullOrWhiteSpace(valeur) Then Return valeurDefaut
            Return valeur.Trim()
        End Function

        Private Function LireInt(racine As XElement, nom As String, valeurDefaut As Integer) As Integer
            Dim brut As String = LireString(racine, nom, valeurDefaut.ToString(CultureInfo.InvariantCulture))
            Dim valeur As Integer
            If Integer.TryParse(brut, NumberStyles.Integer, CultureInfo.InvariantCulture, valeur) Then
                Return valeur
            End If
            If Integer.TryParse(brut, valeur) Then
                Return valeur
            End If
            Return valeurDefaut
        End Function

        Private Function LireBool(racine As XElement, nom As String, valeurDefaut As Boolean) As Boolean
            Dim brut As String = LireString(racine, nom, valeurDefaut.ToString(CultureInfo.InvariantCulture))
            Dim valeur As Boolean
            If Boolean.TryParse(brut, valeur) Then
                Return valeur
            End If
            Return valeurDefaut
        End Function
    End Class
End Namespace
