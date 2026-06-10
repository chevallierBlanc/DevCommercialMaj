Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class FormulaireParametres
        Inherits Form

        ' --- Constantes de Design Windows 11 ---
        Private ReadOnly ColorPrimary As Color = Color.FromArgb(0, 120, 212) ' Windows Blue
        Private ReadOnly ColorBackground As Color = Color.FromArgb(243, 243, 243) ' Windows 11 Light Background
        Private ReadOnly ColorCard As Color = Color.White
        Private ReadOnly ColorText As Color = Color.FromArgb(32, 32, 32)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(102, 102, 102)
        Private ReadOnly ColorBorder As Color = Color.FromArgb(229, 229, 229)

        Private ReadOnly FontTitle As New Font("Segoe UI Variable Display Semibold", 18.0F)
        Private ReadOnly FontSubTitle As New Font("Segoe UI Variable Text", 10.0F)
        Private ReadOnly FontLabel As New Font("Segoe UI Variable Text Semibold", 9.0F)
        Private ReadOnly FontControl As New Font("Segoe UI Variable Text", 9.5F)

        ' --- Composants UI (Noms conservés) ---
        Private ReadOnly txtRemiseMax As TextBox
        Private ReadOnly txtSeuilStock As TextBox
        Private ReadOnly txtAlerteJours As TextBox
        Private ReadOnly cmbImprimanteA4 As ComboBox
        Private ReadOnly cmbImprimanteTicket As ComboBox
        Private ReadOnly chkApercu As CheckBox
        Private ReadOnly chkCouleur As CheckBox
        Private ReadOnly cmbDevise As ComboBox
        Private ReadOnly txtTauxUsd As TextBox
        Private ReadOnly txtScannerIp As TextBox
        Private ReadOnly txtScannerPort As TextBox
        Private ReadOnly chkScannerActif As CheckBox
        Private ReadOnly txtNomMagasin As TextBox
        Private ReadOnly txtAdresseMagasin As TextBox
        Private ReadOnly txtTelephoneMagasin As TextBox
        Private ReadOnly chkModeSombre As CheckBox
        Private ReadOnly txtLogoPath As TextBox
        Private ReadOnly btnLogo As Button
        Private ReadOnly txtBackupFolder As TextBox
        Private ReadOnly btnBackupFolder As Button
        Private ReadOnly txtBackupInterval As TextBox
        Private ReadOnly chkBackupAuto As CheckBox
        Private ReadOnly chkBackupAvantSortie As CheckBox
        Private ReadOnly btnBackupNow As Button
        Private ReadOnly btnCharger As Button
        Private ReadOnly btnEnregistrer As Button
        Private ReadOnly backupService As BackupService

        ' --- Nouveaux composants de structure ---
        Private ReadOnly tabs As TabControl
        Private ReadOnly panelHero As Panel
        Private ReadOnly lblHeroTitre As Label
        Private ReadOnly lblHeroSousTitre As Label
        Private ReadOnly mainTableLayout As TableLayoutPanel
        Private ReadOnly flowFooter As FlowLayoutPanel

        Public Sub New()
            ' Configuration de base du formulaire
            Me.Text = "Paramètres du Système"
            Me.Width = 1000
            Me.Height = 750
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = ColorBackground
            Me.DoubleBuffered = True
            backupService = New BackupService()

            ' --- Header / Hero Section (Style Windows 11) ---
            panelHero = New Panel() With {.Dock = DockStyle.Top, .Height = 100, .BackColor = ColorBackground}
            lblHeroTitre = New Label() With {.Text = "Paramètres", .Left = 30, .Top = 25, .AutoSize = True, .Font = FontTitle, .ForeColor = ColorText}
            lblHeroSousTitre = New Label() With {.Text = "Personnalisez votre expérience, gérez les périphériques et configurez les règles métier.", .Left = 32, .Top = 65, .AutoSize = True, .Font = FontSubTitle, .ForeColor = ColorTextSecondary}
            panelHero.Controls.AddRange({lblHeroTitre, lblHeroSousTitre})

            ' --- TabControl (Style Windows 11) ---
            tabs = New TabControl() With {.Dock = DockStyle.Fill, .Padding = New Point(20, 10)}

            Dim tabGeneral As New TabPage("Général") With {.BackColor = ColorBackground, .AutoScroll = True}
            Dim tabProduits As New TabPage("Règles Métier") With {.BackColor = ColorBackground}
            Dim tabImprimantes As New TabPage("Périphériques") With {.BackColor = ColorBackground}
            Dim tabMonnaie As New TabPage("Finance") With {.BackColor = ColorBackground}
            Dim tabScan As New TabPage("Scanner IP") With {.BackColor = ColorBackground}

            tabs.TabPages.AddRange({tabGeneral, tabProduits, tabImprimantes, tabMonnaie, tabScan})

            ' --- INITIALISATION DES COMPOSANTS (Noms conservés) ---

            ' Tab Général
            Dim pnlGeneralScroll As New Panel() With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .Padding = New Padding(0),
                .BackColor = ColorBackground
            }

            Dim tableGeneral As New TableLayoutPanel() With {.Dock = DockStyle.Top, .ColumnCount = 1, .RowCount = 3, .Padding = New Padding(20), .AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowAndShrink}
            tableGeneral.RowStyles.Add(New RowStyle(SizeType.Absolute, 260))
            tableGeneral.RowStyles.Add(New RowStyle(SizeType.Absolute, 210))
            tableGeneral.RowStyles.Add(New RowStyle(SizeType.Absolute, 240))
            Dim cardMagasin As Panel = CreateCard("Informations du Magasin")
            txtNomMagasin = CreateField(cardMagasin, "Nom du Magasin", 20, 45, 400)
            txtAdresseMagasin = CreateField(cardMagasin, "Adresse Physique", 20, 105, 500)
            txtTelephoneMagasin = CreateField(cardMagasin, "Contact Téléphonique", 20, 165, 250)
            chkModeSombre = New CheckBox() With {.Text = "Activer le Mode Sombre", .Left = 20, .Top = 210, .Font = FontControl, .AutoSize = True}
            cardMagasin.Controls.Add(chkModeSombre)

            Dim cardLogo As Panel = CreateCard("Identité Visuelle")
            txtLogoPath = CreateField(cardLogo, "Chemin du Logo", 20, 45, 450)
            btnLogo = CreateStyledButton("Parcourir...", Color.LightGray, 120, 32)
            btnLogo.Left = 480 : btnLogo.Top = 42 : btnLogo.ForeColor = ColorText
            cardLogo.Controls.Add(btnLogo)

            Dim cardBackup As Panel = CreateCard("Sauvegarde Automatique")
            txtBackupFolder = CreateField(cardBackup, "Dossier de sauvegarde", 20, 45, 420)
            btnBackupFolder = CreateStyledButton("Parcourir...", Color.LightGray, 120, 32)
            btnBackupFolder.Left = 450
            btnBackupFolder.Top = 42
            btnBackupFolder.ForeColor = ColorText
            txtBackupInterval = CreateField(cardBackup, "Intervalle (minutes)", 20, 105, 120)
            chkBackupAuto = New CheckBox() With {.Text = "Activer la sauvegarde automatique", .Left = 20, .Top = 155, .Font = FontControl, .AutoSize = True}
            chkBackupAvantSortie = New CheckBox() With {.Text = "Sauvegarde avant fermeture", .Left = 20, .Top = 185, .Font = FontControl, .AutoSize = True}
            btnBackupNow = CreateStyledButton("Lancer la sauvegarde", ColorPrimary, 180, 34)
            btnBackupNow.Left = 20
            btnBackupNow.Top = 215
            Dim lblBackupInfo As New Label() With {
                .Text = "La destination proposée doit rester facilement accessible et la sauvegarde s'exécute en sourdine.",
                .Left = 210,
                .Top = 220,
                .Width = 430,
                .Height = 36,
                .Font = FontControl,
                .ForeColor = ColorTextSecondary,
                .AutoSize = False
            }
            cardBackup.Controls.AddRange({btnBackupFolder, chkBackupAuto, chkBackupAvantSortie, btnBackupNow, lblBackupInfo})

            tableGeneral.Controls.Add(cardMagasin, 0, 0)
            tableGeneral.Controls.Add(cardLogo, 0, 1)
            tableGeneral.Controls.Add(cardBackup, 0, 2)
            pnlGeneralScroll.Controls.Add(tableGeneral)
            tabGeneral.Controls.Add(pnlGeneralScroll)

            ' Tab Produits
            Dim tableProduits As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 1, .Padding = New Padding(20)}
            Dim cardRegles As Panel = CreateCard("Seuils et Alertes")
            txtRemiseMax = CreateField(cardRegles, "Remise Maximale Autorisée (%)", 20, 45, 200)
            txtSeuilStock = CreateField(cardRegles, "Seuil de Stock Critique (Défaut)", 20, 105, 200)
            txtAlerteJours = CreateField(cardRegles, "Alerte Expiration (Jours avant)", 20, 165, 200)
            tableProduits.Controls.Add(cardRegles, 0, 0)
            tabProduits.Controls.Add(tableProduits)

            ' Tab Imprimantes
            Dim tablePrinters As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 1, .Padding = New Padding(20)}
            Dim cardPrinters As Panel = CreateCard("Configuration des Impressions")
            cmbImprimanteA4 = CreateComboField(cardPrinters, "Imprimante A4 (Factures/Rapports)", 20, 45, 400)
            cmbImprimanteTicket = CreateComboField(cardPrinters, "Imprimante Thermique (Tickets)", 20, 105, 400)
            chkApercu = New CheckBox() With {.Text = "Afficher l'aperçu avant impression", .Left = 20, .Top = 160, .Font = FontControl, .AutoSize = True}
            chkCouleur = New CheckBox() With {.Text = "Forcer l'impression en couleur", .Left = 20, .Top = 190, .Font = FontControl, .AutoSize = True}
            cardPrinters.Controls.AddRange({chkApercu, chkCouleur})
            tablePrinters.Controls.Add(cardPrinters, 0, 0)
            tabImprimantes.Controls.Add(tablePrinters)

            ' Tab Monnaie
            Dim tableMonnaie As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 1, .Padding = New Padding(20)}
            Dim cardMonnaie As Panel = CreateCard("Devises et Taux de Change")
            cmbDevise = CreateComboField(cardMonnaie, "Devise par Défaut du Système", 20, 45, 150)
            cmbDevise.Items.AddRange({"FC", "USD"})
            txtTauxUsd = CreateField(cardMonnaie, "Taux de Change (1 USD = X FC)", 20, 105, 150)
            tableMonnaie.Controls.Add(cardMonnaie, 0, 0)
            tabMonnaie.Controls.Add(tableMonnaie)

            ' Tab Scan
            Dim tableScan As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 1, .Padding = New Padding(20)}
            Dim cardScan As Panel = CreateCard("Scanner Réseau (IP)")
            txtScannerIp = CreateField(cardScan, "Adresse IP du Scanner", 20, 45, 250)
            txtScannerPort = CreateField(cardScan, "Port de Communication", 20, 105, 100)
            chkScannerActif = New CheckBox() With {.Text = "Activer la liaison scanner réseau", .Left = 20, .Top = 160, .Font = FontControl, .AutoSize = True}
            cardScan.Controls.Add(chkScannerActif)
            tableScan.Controls.Add(cardScan, 0, 0)
            tabScan.Controls.Add(tableScan)

            ' --- Footer / Actions ---
            flowFooter = New FlowLayoutPanel() With {.Dock = DockStyle.Bottom, .Height = 70, .FlowDirection = FlowDirection.RightToLeft, .Padding = New Padding(20, 15, 20, 0), .BackColor = Color.White}
            btnEnregistrer = CreateStyledButton("Enregistrer les modifications", ColorPrimary, 220, 40)
            btnCharger = CreateStyledButton("Réinitialiser", Color.LightGray, 120, 40)
            btnCharger.ForeColor = ColorText
            flowFooter.Controls.AddRange({btnEnregistrer, btnCharger})

            ' Assemblage final
            Me.Controls.Add(tabs)
            Me.Controls.Add(panelHero)
            Me.Controls.Add(flowFooter)

            ' --- Liaison des événements (Logique conservée) ---
            AddHandler btnLogo.Click, AddressOf ChoisirLogo
            AddHandler btnBackupFolder.Click, AddressOf ChoisirDossierBackup
            AddHandler btnBackupNow.Click, AddressOf LancerSauvegardeManuelle
            AddHandler btnCharger.Click, AddressOf Charger
            AddHandler btnEnregistrer.Click, AddressOf Enregistrer

            ' --- Initialisation ---
            ChargerImprimantes()
            'ThemeHelper.AppliquerTheme(Me)

            ' Chargement initial des données
            AddHandler Me.Load, AddressOf Charger
        End Sub

        ' --- Helpers de Design Windows 11 ---

        Private Function CreateStyledButton(text As String, backColor As Color, w As Integer, h As Integer) As Button
            Dim btn As New Button() With {
                .Text = text, .Width = w, .Height = h,
                .BackColor = backColor, .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat, .Font = FontLabel, .Cursor = Cursors.Hand,
                .Margin = New Padding(10, 0, 0, 0)
            }
            btn.FlatAppearance.BorderSize = 0
            ' Simulation de coins arrondis (via Paint ou Region si nécessaire, ici on reste sur Flat)
            Return btn
        End Function

        Private Function CreateCard(title As String) As Panel
            Dim p As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Margin = New Padding(0, 0, 0, 20), .Padding = New Padding(20)}
            p.Controls.Add(New Label() With {.Text = title, .Font = FontLabel, .ForeColor = ColorPrimary, .AutoSize = True, .Top = 10, .Left = 15})
            Return p
        End Function

        Private Function CreateField(parent As Control, label As String, x As Integer, y As Integer, w As Integer) As TextBox
            parent.Controls.Add(New Label() With {.Text = label, .Left = x, .Top = y - 22, .Font = FontLabel, .ForeColor = ColorTextSecondary, .AutoSize = True})
            Dim txt As New TextBox() With {.Left = x, .Top = y, .Width = w, .Font = FontControl, .BorderStyle = BorderStyle.FixedSingle}
            parent.Controls.Add(txt)
            Return txt
        End Function

        Private Function CreateComboField(parent As Control, label As String, x As Integer, y As Integer, w As Integer) As ComboBox
            parent.Controls.Add(New Label() With {.Text = label, .Left = x, .Top = y - 22, .Font = FontLabel, .ForeColor = ColorTextSecondary, .AutoSize = True})
            Dim cmb As New ComboBox() With {.Left = x, .Top = y, .Width = w, .Font = FontControl, .DropDownStyle = ComboBoxStyle.DropDownList, .FlatStyle = FlatStyle.Flat}
            parent.Controls.Add(cmb)
            Return cmb
        End Function

        ' --- LOGIQUE MÉTIER (STRICTEMENT IDENTIQUE À L'ORIGINAL) ---

        Private Sub ChargerImprimantes()
            cmbImprimanteA4.Items.Clear()
            cmbImprimanteTicket.Items.Clear()
            For Each p As String In System.Drawing.Printing.PrinterSettings.InstalledPrinters
                cmbImprimanteA4.Items.Add(p)
                cmbImprimanteTicket.Items.Add(p)
            Next
        End Sub

        Private Function ObtenirService() As ParametreService
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New ParametreRepository(dal)
            Return New ParametreService(repo)
        End Function

        Private Sub Charger(sender As Object, e As EventArgs)
            Try
                Dim service As ParametreService = ObtenirService()
                Dim p As ParametreDTO = service.Charger()
                If p IsNot Nothing Then
                    txtRemiseMax.Text = p.RemiseMaxPourcent.ToString()
                    txtSeuilStock.Text = p.SeuilStockCritique.ToString()
                    txtAlerteJours.Text = p.AlerteExpirationJours.ToString()
                    cmbImprimanteA4.Text = p.ImprimanteA4
                    cmbImprimanteTicket.Text = p.ImprimanteTicket
                    cmbDevise.SelectedItem = p.DeviseParDefaut
                    txtTauxUsd.Text = p.TauxUsd.ToString()
                    txtScannerIp.Text = p.ScannerIp
                    txtScannerPort.Text = p.ScannerPort.ToString()
                    chkScannerActif.Checked = p.ScannerActif
                    txtNomMagasin.Text = p.NomMagasin
                    txtAdresseMagasin.Text = p.AdresseMagasin
                    txtTelephoneMagasin.Text = p.TelephoneMagasin
                    chkModeSombre.Checked = p.ModeSombre
                    txtLogoPath.Text = p.LogoPath
                    chkApercu.Checked = p.ApercuAvantImpression
                    chkCouleur.Checked = p.ImpressionCouleur
                End If
            Catch ex As Exception
                MessageBox.Show("Erreur chargement parametres: " & ex.Message)
            End Try

            Try
                Dim backup As BackupSettings = backupService.ChargerParametres()
                txtBackupFolder.Text = If(String.IsNullOrWhiteSpace(backup.BackupFolder), backupService.ObtenirDossierParDefaut(), backup.BackupFolder)
                txtBackupInterval.Text = backup.IntervalMinutes.ToString()
                chkBackupAuto.Checked = backup.Enabled
                chkBackupAvantSortie.Checked = backup.BackupBeforeExit
            Catch ex As Exception
                MessageBox.Show("Erreur chargement sauvegarde: " & ex.Message)
            End Try
        End Sub

        Private Sub Enregistrer(sender As Object, e As EventArgs)
            Try
                Dim service As ParametreService = ObtenirService()
                Dim intervalleBackup As Integer
                If Not Integer.TryParse(If(txtBackupInterval.Text.Trim() = "", "240", txtBackupInterval.Text.Trim()), intervalleBackup) Then
                    intervalleBackup = 240
                End If
                Dim p As New ParametreDTO With {
                    .RemiseMaxPourcent = Decimal.Parse(If(txtRemiseMax.Text.Trim() = "", "0", txtRemiseMax.Text.Trim())),
                    .SeuilStockCritique = Decimal.Parse(If(txtSeuilStock.Text.Trim() = "", "0", txtSeuilStock.Text.Trim())),
                    .AlerteExpirationJours = Convert.ToInt32(If(txtAlerteJours.Text.Trim() = "", "30", txtAlerteJours.Text.Trim())),
                    .ImprimanteA4 = cmbImprimanteA4.Text,
                    .ImprimanteTicket = cmbImprimanteTicket.Text,
                    .DeviseParDefaut = If(cmbDevise.SelectedItem Is Nothing, "FC", cmbDevise.SelectedItem.ToString()),
                    .TauxUsd = Decimal.Parse(If(txtTauxUsd.Text.Trim() = "", "0", txtTauxUsd.Text.Trim())),
                    .ScannerIp = txtScannerIp.Text.Trim(),
                    .ScannerPort = Convert.ToInt32(If(txtScannerPort.Text.Trim() = "", "9000", txtScannerPort.Text.Trim())),
                    .ScannerActif = chkScannerActif.Checked,
                    .NomMagasin = txtNomMagasin.Text.Trim(),
                    .AdresseMagasin = txtAdresseMagasin.Text.Trim(),
                    .TelephoneMagasin = txtTelephoneMagasin.Text.Trim(),
                    .ModeSombre = chkModeSombre.Checked,
                    .LogoPath = txtLogoPath.Text.Trim(),
                    .ApercuAvantImpression = chkApercu.Checked,
                    .ImpressionCouleur = chkCouleur.Checked
                }
                service.Enregistrer(p)

                Dim backupSettings As New BackupSettings With {
                    .Enabled = chkBackupAuto.Checked,
                    .IntervalMinutes = intervalleBackup,
                    .BackupFolder = If(String.IsNullOrWhiteSpace(txtBackupFolder.Text), backupService.ObtenirDossierParDefaut(), txtBackupFolder.Text.Trim()),
                    .BackupBeforeExit = chkBackupAvantSortie.Checked
                }
                backupService.EnregistrerParametres(backupSettings)
                MessageBox.Show("Parametres enregistres.")
            Catch ex As Exception
                MessageBox.Show("Erreur enregistrement parametres: " & ex.Message)
            End Try
        End Sub

        Private Sub ChoisirLogo(sender As Object, e As EventArgs)
            Dim ofd As New OpenFileDialog() With {.Filter = "Images|*.png;*.jpg;*.jpeg;*.ico"}
            If ofd.ShowDialog() = DialogResult.OK Then
                txtLogoPath.Text = ofd.FileName
            End If
        End Sub

        Private Sub ChoisirDossierBackup(sender As Object, e As EventArgs)
            Using dlg As New FolderBrowserDialog() With {
                .Description = "Choisissez un dossier de sauvegarde sûr et facilement récupérable.",
                .ShowNewFolderButton = True
            }
                If dlg.ShowDialog() = DialogResult.OK Then
                    txtBackupFolder.Text = dlg.SelectedPath
                End If
            End Using
        End Sub

        Private Sub LancerSauvegardeManuelle(sender As Object, e As EventArgs)
            Try
                Dim cible As String = If(String.IsNullOrWhiteSpace(txtBackupFolder.Text), backupService.ObtenirDossierParDefaut(), txtBackupFolder.Text.Trim())
                Dim resultat As BackupResult = backupService.ExecuterSauvegarde(cible)
                If resultat.Success Then
                    MessageBox.Show("Sauvegarde réalisée avec succès : " & resultat.FilePath, "Sauvegarde", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    MessageBox.Show("Sauvegarde impossible : " & resultat.Message, "Sauvegarde", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            Catch ex As Exception
                MessageBox.Show("Erreur sauvegarde : " & ex.Message, "Sauvegarde", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub
    End Class
End Namespace
