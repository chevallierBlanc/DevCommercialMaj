Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms.DataVisualization.Charting


Namespace DevCommerc8ak
    Public Class FormConfigurationSQL
        Inherits Form

        ' --- Palette de Couleurs Enterprise ERP ---
        Private ReadOnly ColorBg As Color = Color.FromArgb(240, 242, 245)
        Private ReadOnly ColorSidebar As Color = Color.FromArgb(33, 43, 54) ' Sidebar sombre pro
        Private ReadOnly ColorCardBg As Color = Color.White
        Private ReadOnly ColorAccent As Color = Color.FromArgb(0, 102, 204) ' Bleu Enterprise
        Private ReadOnly ColorSuccess As Color = Color.FromArgb(34, 197, 94)
        Private ReadOnly ColorDanger As Color = Color.FromArgb(211, 47, 47)
        Private ReadOnly ColorTextPrimary As Color = Color.FromArgb(33, 43, 54)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(99, 115, 129)
        Private ReadOnly ColorBorder As Color = Color.FromArgb(224, 224, 224)

        ' --- Polices ---
        Private ReadOnly FontMain As New Font("Segoe UI", 9.0F)
        Private ReadOnly FontBold As New Font("Segoe UI", 9.0F, FontStyle.Bold)
        Private ReadOnly FontTitle As New Font("Segoe UI", 15.0F, FontStyle.Bold)

        ' --- Composants (Noms conservés) ---
        Private ReadOnly txtServer As New TextBox()
        Private ReadOnly txtPort As New TextBox()
        Private ReadOnly txtDatabase As New TextBox()
        Private ReadOnly cboAuth As New ComboBox()
        Private ReadOnly txtUsername As New TextBox()
        Private ReadOnly txtPassword As New TextBox()
        Private ReadOnly btnTogglePassword As New Button()
        Private ReadOnly btnTest As New Button()
        Private ReadOnly btnSave As New Button()
        Private ReadOnly btnCancel As New Button()
        Private ReadOnly lblStatus As New Label()

        ' --- Logique Métier (Conservée) ---
        Private ReadOnly log As New ProductionLogService()
        Private _testOk As Boolean

        Public Sub New()
            ' Configuration de la Form
            Me.Text = "Administration Système - Configuration SQL"
            Me.Size = New Size(900, 680)
            Me.MinimumSize = New Size(800, 600)
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = ColorBg
            Me.Font = FontMain
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.DoubleBuffered = True

            BuildUi()
            AddHandler Me.Load, AddressOf FormConfigurationSQL_Load
        End Sub

        Private Sub BuildUi()
            Me.Controls.Clear()

            ' --- Layout Principal (TableLayoutPanel pour une meilleure gestion des zones) ---
            Dim mainLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 1,
                .Padding = New Padding(0),
                .Margin = New Padding(0),
                .BackColor = ColorBg
            }
            mainLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 220)) ' Sidebar
            mainLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100)) ' Contenu principal

            ' --- Sidebar ---
            Dim pnlSidebar As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorSidebar
            }
            ' Logo / Titre Sidebar
            Dim lblLogo As New Label() With {
                .Text = "ERP SYSTEM" & Environment.NewLine & "CONFIG",
                .ForeColor = Color.White,
                .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold),
                .Location = New Point(20, 30),
                .AutoSize = True
            }
            pnlSidebar.Controls.Add(lblLogo)
            mainLayout.Controls.Add(pnlSidebar, 0, 0)

            ' --- Contenu Principal (Header, Formulaire, Footer) ---
            Dim pnlContentWrapper As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3,
                .Padding = New Padding(0),
                .Margin = New Padding(0),
                .BackColor = ColorBg
            }
            pnlContentWrapper.RowStyles.Add(New RowStyle(SizeType.Absolute, 80)) ' Header
            pnlContentWrapper.RowStyles.Add(New RowStyle(SizeType.Percent, 100)) ' Formulaire
            pnlContentWrapper.RowStyles.Add(New RowStyle(SizeType.Absolute, 100)) ' Footer

            ' --- En-tête ---
            Dim pnlHeader As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Padding = New Padding(30, 15, 30, 15)
            }
            Dim lblTitle As New Label() With {
                .Text = "Paramètres de Connexion SQL",
                .Font = FontTitle,
                .ForeColor = ColorTextPrimary,
                .AutoSize = True,
                .Location = New Point(0, 0)
            }
            Dim lblSubtitle As New Label() With {
                .Text = "Gestion de la connectivité à l'instance de base de données de production.",
                .Font = FontMain,
                .ForeColor = ColorTextSecondary,
                .AutoSize = True,
                .Location = New Point(0, 35)
            }
            pnlHeader.Controls.AddRange({lblTitle, lblSubtitle})
            pnlContentWrapper.Controls.Add(pnlHeader, 0, 0)

            ' --- Conteneur pour les groupes de paramètres (FlowLayoutPanel pour un layout propre) ---
            Dim pnlFormLayout As New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .FlowDirection = FlowDirection.TopDown,
                .WrapContents = False,
                .Padding = New Padding(30, 20, 30, 20),
                .BackColor = ColorBg
            }
            pnlContentWrapper.Controls.Add(pnlFormLayout, 0, 1)

            ' Groupe 1 : Serveur
            Dim grpServer As Panel = CreerGroupe("INSTANCE DU SERVEUR", 180)
            Dim gridServer As TableLayoutPanel = CType(CreerGrid(grpServer), TableLayoutPanel)
            AjouterChamp(gridServer, 0, 0, "Hôte / Adresse IP", txtServer)
            AjouterChamp(gridServer, 0, 1, "Port (Défaut: 1433)", txtPort)
            AjouterChamp(gridServer, 1, 0, "Nom de la Base", txtDatabase)
            grpServer.Controls.Add(gridServer)
            pnlFormLayout.Controls.Add(grpServer)

            ' Groupe 2 : Sécurité
            Dim grpSecurity As Panel = CreerGroupe("SÉCURITÉ ET AUTHENTIFICATION", 200)
            Dim gridSecurity As TableLayoutPanel = CType(CreerGrid(grpSecurity), TableLayoutPanel)
            AjouterChamp(gridSecurity, 0, 0, "Mode d'accès", cboAuth)
            AjouterChamp(gridSecurity, 1, 0, "Utilisateur", txtUsername)

            Dim pnlPass As New Panel() With {.Dock = DockStyle.Fill}
            txtPassword.Dock = DockStyle.Fill
            txtPassword.UseSystemPasswordChar = True
            btnTogglePassword.Text = "👁"
            btnTogglePassword.Dock = DockStyle.Right
            btnTogglePassword.Width = 25
            btnTogglePassword.FlatStyle = FlatStyle.Flat
            btnTogglePassword.FlatAppearance.BorderSize = 0
            btnTogglePassword.BackColor = Color.White
            btnTogglePassword.ForeColor = ColorTextSecondary
            btnTogglePassword.Cursor = Cursors.Hand
            pnlPass.Controls.AddRange({txtPassword, btnTogglePassword})
            AjouterChamp(gridSecurity, 1, 1, "Mot de passe", pnlPass)
            grpSecurity.Controls.Add(gridSecurity)
            pnlFormLayout.Controls.Add(grpSecurity)

            ' Configuration
            txtPort.MaxLength = 6
            cboAuth.DropDownStyle = ComboBoxStyle.DropDownList
            cboAuth.Items.AddRange({"Authentification Windows", "Authentification SQL Server"})

            ' --- Footer Actions ---
            Dim pnlFooter As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Padding = New Padding(30, 15, 30, 15)
            }
            AddHandler pnlFooter.Paint, Sub(s, e) e.Graphics.DrawLine(New Pen(ColorBorder), 0, 0, pnlFooter.Width, 0)
            pnlContentWrapper.Controls.Add(pnlFooter, 0, 2)

            lblStatus.Text = "En attente de test..."
            lblStatus.ForeColor = ColorTextSecondary
            lblStatus.Font = New Font("Segoe UI", 8.5F, FontStyle.Italic)
            lblStatus.AutoSize = True
            lblStatus.Location = New Point(0, 40)
            pnlFooter.Controls.Add(lblStatus)

            Dim pnlButtons As New FlowLayoutPanel() With {
                .Dock = DockStyle.Right,
                .Width = 450,
                .FlowDirection = FlowDirection.RightToLeft
            }

            btnSave.Text = "ENREGISTRER"
            StyliserBouton(btnSave, ColorAccent, Color.White, False)
            btnSave.Size = New Size(130, 42)
            btnSave.Enabled = False

            btnTest.Text = "TESTER LA CONNEXION"
            StyliserBouton(btnTest, Color.White, ColorAccent, True)
            btnTest.Size = New Size(180, 42)

            btnCancel.Text = "ANNULER"
            StyliserBouton(btnCancel, Color.White, ColorTextSecondary, True)
            btnCancel.Size = New Size(100, 42)

            pnlButtons.Controls.AddRange({btnSave, btnTest, btnCancel})
            pnlFooter.Controls.Add(pnlButtons)

            mainLayout.Controls.Add(pnlContentWrapper, 1, 0)
            Me.Controls.Add(mainLayout)

            ' Handlers
            AddHandler cboAuth.SelectedIndexChanged, AddressOf OnFieldChanged
            AddHandler txtServer.TextChanged, AddressOf OnFieldChanged
            AddHandler txtPort.TextChanged, AddressOf OnFieldChanged
            AddHandler txtDatabase.TextChanged, AddressOf OnFieldChanged
            AddHandler txtUsername.TextChanged, AddressOf OnFieldChanged
            AddHandler txtPassword.TextChanged, AddressOf OnFieldChanged
            AddHandler btnTogglePassword.Click, AddressOf TogglePasswordVisibility
            AddHandler btnTest.Click, AddressOf TesterConnexion
            AddHandler btnSave.Click, AddressOf EnregistrerConfiguration
            AddHandler btnCancel.Click, AddressOf CancelAndClose

            Me.AcceptButton = btnTest
            Me.CancelButton = btnCancel
        End Sub

        ' --- Helpers ERP Design ---

        Private Function CreerGroupe(titre As String, height As Integer) As Panel
            Dim pnl As New Panel() With {
                .Width = 580, ' S'adapte à la largeur du parent FlowLayoutPanel
                .Height = height,
                .Padding = New Padding(15, 35, 15, 10),
                .BackColor = ColorCardBg,
                .Margin = New Padding(0, 0, 0, 10) ' Marge entre les groupes
            }
            AddHandler pnl.Paint, Sub(s, e)
                                      Dim rect As New Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1)
                                      e.Graphics.DrawRectangle(New Pen(ColorBorder), rect)
                                      Using b As New SolidBrush(Color.FromArgb(248, 249, 251))
                                          e.Graphics.FillRectangle(b, 1, 1, pnl.Width - 2, 35)
                                      End Using
                                      e.Graphics.DrawString(titre, New Font("Segoe UI", 9.5F, FontStyle.Bold), New SolidBrush(ColorTextSecondary), 15, 10)
                                  End Sub
            Return pnl
        End Function


        Private Function CreerGrid(parent As Panel) As TableLayoutPanel
            Dim grid As New TableLayoutPanel() With {
                .Dock = DockStyle.Top, ' Prend l'espace du haut disponible dans le groupe
                .AutoSize = True,
                .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                .Padding = New Padding(12, 18, 12, 8), ' Padding pour laisser de la place au titre du groupe
                .ColumnCount = 2,
                .RowCount = 2
            }
            grid.ColumnStyles.Clear()
            grid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
            grid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
            parent.Controls.Add(grid)
            Return grid
        End Function
        Private Sub AjouterChamp(grid As TableLayoutPanel, row As Integer, col As Integer, label As String, ctrl As Control)
            Dim pnl As New Panel() With {.Dock = DockStyle.Top, .Height = 55, .Padding = New Padding(5, 0, 15, 0), .Margin = New Padding(0, 0, 0, 8)}
            Dim lbl As New Label() With {
                .Text = label,
                .Font = New Font("Segoe UI", 8.5F),
                .ForeColor = ColorTextPrimary,
                .Dock = DockStyle.Top,
                .Height = 20,
                .TextAlign = ContentAlignment.BottomLeft
            }
            ctrl.Dock = DockStyle.Top
            ctrl.Height = 26
            ctrl.Margin = New Padding(0, 0, 0, 5)
            If TypeOf ctrl Is TextBox Then CType(ctrl, TextBox).BorderStyle = BorderStyle.FixedSingle
            If TypeOf ctrl Is ComboBox Then CType(ctrl, ComboBox).FlatStyle = FlatStyle.Flat
            pnl.Controls.Add(ctrl) ' Puis le contrôle
            pnl.Controls.Add(lbl) ' Ajouter le label en premier

            grid.RowStyles.Add(New RowStyle(SizeType.Absolute, 63))
            grid.Controls.Add(pnl, col, row)
        End Sub

        Private Sub StyliserBouton(btn As Button, bgColor As Color, fgColor As Color, hasBorder As Boolean)
            btn.FlatStyle = FlatStyle.Flat
            btn.BackColor = bgColor
            btn.ForeColor = fgColor
            btn.Font = FontBold
            btn.Cursor = Cursors.Hand
            btn.FlatAppearance.BorderSize = If(hasBorder, 1, 0)
            If hasBorder Then btn.FlatAppearance.BorderColor = ColorBorder
        End Sub

        ' --- Logique Métier ---


        ' --- Logique Métier ---

        Private Sub FormConfigurationSQL_Load(sender As Object, e As EventArgs)
            Try
                Dim settings As SqlConnectionSettings = SqlConfigurationService.LoadSettings()
                txtServer.Text = settings.Server
                txtPort.Text = If(settings.Port.HasValue, settings.Port.Value.ToString(), String.Empty)
                txtDatabase.Text = settings.DatabaseName
                cboAuth.SelectedIndex = If(settings.AuthenticationMode = SqlAuthenticationMode.WindowsAuthentication, 0, 1)
                txtUsername.Text = settings.Username
                txtPassword.Text = settings.Password
                _testOk = False
                btnSave.Enabled = False
                ApplyAuthMode()
                lblStatus.Text = "Configuration chargée."
            Catch ex As Exception
                log.Error("Erreur chargement SQL.", ex)
                lblStatus.Text = "Erreur de chargement."
            End Try
        End Sub

        Private Sub OnFieldChanged(sender As Object, e As EventArgs)
            _testOk = False

            btnSave.Enabled = False
            lblStatus.ForeColor = ColorTextSecondary
            lblStatus.Text = "Modifications non testées..."
            ApplyAuthMode()
        End Sub

        Private Sub ApplyAuthMode()
            Dim windowsAuth As Boolean = (cboAuth.SelectedIndex <= 0)
            txtUsername.Enabled = Not windowsAuth
            txtPassword.Enabled = Not windowsAuth
            btnTogglePassword.Visible = True
            btnTogglePassword.Enabled = Not windowsAuth
            If windowsAuth Then
                txtUsername.Text = String.Empty
                txtPassword.Text = String.Empty
                btnTogglePassword.Visible = False
            End If
        End Sub

        Private Sub TogglePasswordVisibility(sender As Object, e As EventArgs)
            txtPassword.UseSystemPasswordChar = Not txtPassword.UseSystemPasswordChar
            btnTogglePassword.Text = If(txtPassword.UseSystemPasswordChar, "👁", "🙈")
        End Sub

        Private Sub TesterConnexion(sender As Object, e As EventArgs)
            Dim settings As SqlConnectionSettings = ConstruireSettingsDepuisEcran()
            Dim message As String = Nothing

            lblStatus.Text = "Test de connectivité..."
            Me.Cursor = Cursors.WaitCursor

            If SqlConfigurationService.TestConnection(settings, message) Then
                _testOk = True
                btnSave.Enabled = True
                lblStatus.ForeColor = ColorSuccess
                lblStatus.Text = "Connectivité établie avec succès."
                MessageBox.Show("Test de connexion réussi.", "ERP System", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                _testOk = False
                btnSave.Enabled = False
                lblStatus.ForeColor = ColorDanger
                lblStatus.Text = "Échec de la connectivité."
                MessageBox.Show("Échec : " & message, "ERP System", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
            Me.Cursor = Cursors.Default
        End Sub

        Private Sub EnregistrerConfiguration(sender As Object, e As EventArgs)
            If Not _testOk Then Return
            Try
                Dim settings As SqlConnectionSettings = ConstruireSettingsDepuisEcran()
                SqlConfigurationService.SaveSettings(settings)
                MessageBox.Show("Configuration enregistrée.", "ERP System", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Me.DialogResult = DialogResult.OK
                Me.Close()
            Catch ex As Exception
                log.Error("Erreur enregistrement SQL.", ex)
                MessageBox.Show("Erreur : " & ex.Message, "ERP System", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub CancelAndClose(sender As Object, e As EventArgs)
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End Sub

        Private Function ConstruireSettingsDepuisEcran() As SqlConnectionSettings
            Dim settings As New SqlConnectionSettings() With {
                .Server = txtServer.Text.Trim(),
                .DatabaseName = txtDatabase.Text.Trim(),
                .AuthenticationMode = If(cboAuth.SelectedIndex <= 0, SqlAuthenticationMode.WindowsAuthentication, SqlAuthenticationMode.SqlServerAuthentication),
                .Username = txtUsername.Text.Trim(),
                .Password = txtPassword.Text
            }
            Dim portValue As Integer
            If Integer.TryParse(txtPort.Text.Trim(), portValue) Then settings.Port = portValue
            Return settings
        End Function

    End Class
End Namespace
