Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormConfigurationSQL
        Inherits Form

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
        Private ReadOnly log As New ProductionLogService()
        Private _testOk As Boolean

        Public Sub New()
            Me.Text = "Configuration SQL - Première installation"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Size = New Size(760, 520)
            Me.MinimumSize = New Size(760, 520)
            Me.Font = New Font("Segoe UI", 10.0F)
            Me.BackColor = Color.FromArgb(244, 247, 251)
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False

            BuildUi()
            AddHandler Me.Load, AddressOf FormConfigurationSQL_Load
        End Sub

        Private Sub BuildUi()
            Dim root As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3,
                .Padding = New Padding(20)
            }
            root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            root.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
            root.RowStyles.Add(New RowStyle(SizeType.AutoSize))

            Dim header As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 90,
                .BackColor = Color.White,
                .Padding = New Padding(16)
            }
            header.BorderStyle = BorderStyle.FixedSingle
            Dim title As New Label() With {
                .Text = "Configuration de la connexion SQL",
                .Dock = DockStyle.Top,
                .Height = 32,
                .Font = New Font("Segoe UI", 16.0F, FontStyle.Bold),
                .ForeColor = Color.FromArgb(31, 41, 55)
            }
            Dim subtitle As New Label() With {
                .Text = "Renseignez les paramètres du serveur SQL, testez la connexion, puis enregistrez.",
                .Dock = DockStyle.Top,
                .Height = 28,
                .Font = New Font("Segoe UI", 9.5F),
                .ForeColor = Color.FromArgb(107, 114, 128)
            }
            header.Controls.Add(subtitle)
            header.Controls.Add(title)

            Dim card As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Padding = New Padding(18)
            }
            card.BorderStyle = BorderStyle.FixedSingle

            Dim grid As New TableLayoutPanel() With {
                .Dock = DockStyle.Top,
                .ColumnCount = 4,
                .AutoSize = True,
                .AutoSizeMode = AutoSizeMode.GrowAndShrink
            }
            grid.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 180))
            grid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
            grid.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 180))
            grid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))

            AddFieldRow(grid, 0, "Serveur SQL / IP", txtServer, "Port", txtPort)
            AddFieldRow(grid, 1, "Nom de la base", txtDatabase, "Mode d'authentification", cboAuth)
            AddFieldRow(grid, 2, "Utilisateur SQL", txtUsername, "Mot de passe SQL", txtPassword, btnTogglePassword)

            txtServer.Dock = DockStyle.Fill
            txtPort.Dock = DockStyle.Fill
            txtDatabase.Dock = DockStyle.Fill
            cboAuth.Dock = DockStyle.Fill
            txtUsername.Dock = DockStyle.Fill
            txtPassword.Dock = DockStyle.Fill

            txtPort.Width = 120
            txtPort.MaxLength = 6
            txtPassword.UseSystemPasswordChar = True
            btnTogglePassword.Text = "Afficher"
            btnTogglePassword.Width = 90
            btnTogglePassword.Height = 36

            cboAuth.DropDownStyle = ComboBoxStyle.DropDownList
            cboAuth.Items.Add("Windows")
            cboAuth.Items.Add("SQL Server")

            Dim lblHint As New Label() With {
                .Text = "Le mot de passe est stocké dans un fichier de configuration local protégé.",
                .Dock = DockStyle.Top,
                .Height = 24,
                .ForeColor = Color.FromArgb(107, 114, 128)
            }

            Dim statusPanel As New Panel() With {.Dock = DockStyle.Bottom, .Height = 50}
            lblStatus.Dock = DockStyle.Fill
            lblStatus.TextAlign = ContentAlignment.MiddleLeft
            lblStatus.ForeColor = Color.FromArgb(107, 114, 128)
            lblStatus.Text = "Prêt."
            statusPanel.Controls.Add(lblStatus)

            Dim buttons As New FlowLayoutPanel() With {
                .Dock = DockStyle.Bottom,
                .Height = 54,
                .FlowDirection = FlowDirection.RightToLeft,
                .WrapContents = False,
                .Padding = New Padding(0, 8, 0, 0)
            }
            btnCancel.Text = "Annuler"
            btnCancel.Width = 110
            btnCancel.Height = 38
            btnCancel.DialogResult = DialogResult.Cancel

            btnSave.Text = "Enregistrer"
            btnSave.Width = 120
            btnSave.Height = 38
            btnSave.Enabled = False

            btnTest.Text = "Tester la connexion"
            btnTest.Width = 160
            btnTest.Height = 38

            buttons.Controls.Add(btnCancel)
            buttons.Controls.Add(btnSave)
            buttons.Controls.Add(btnTest)

            card.Controls.Add(statusPanel)
            card.Controls.Add(buttons)
            card.Controls.Add(lblHint)
            card.Controls.Add(grid)

            root.Controls.Add(header, 0, 0)
            root.Controls.Add(card, 0, 1)
            Me.Controls.Add(root)

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

        Private Sub AddFieldRow(grid As TableLayoutPanel, rowIndex As Integer, labelLeft As String, controlLeft As Control, labelRight As String, controlRight As Control, Optional rightButton As Control = Nothing)
            grid.RowStyles.Add(New RowStyle(SizeType.AutoSize))

            Dim lblLeft As New Label() With {
                .Text = labelLeft,
                .Dock = DockStyle.Fill,
                .Height = 26,
                .TextAlign = ContentAlignment.BottomLeft,
                .ForeColor = Color.FromArgb(75, 85, 99)
            }
            Dim lblRight As New Label() With {
                .Text = labelRight,
                .Dock = DockStyle.Fill,
                .Height = 26,
                .TextAlign = ContentAlignment.BottomLeft,
                .ForeColor = Color.FromArgb(75, 85, 99)
            }

            Dim panelLeft As New Panel() With {.Dock = DockStyle.Fill, .Height = 36}
            panelLeft.Controls.Add(controlLeft)
            Dim panelRight As New Panel() With {.Dock = DockStyle.Fill, .Height = 36}
            panelRight.Controls.Add(controlRight)
            If rightButton IsNot Nothing Then
                rightButton.Dock = DockStyle.Right
                panelRight.Controls.Add(rightButton)
            End If

            grid.Controls.Add(lblLeft, 0, rowIndex)
            grid.Controls.Add(panelLeft, 1, rowIndex)
            grid.Controls.Add(lblRight, 2, rowIndex)
            grid.Controls.Add(panelRight, 3, rowIndex)
        End Sub

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
                lblStatus.Text = "Configuration chargée. Testez la connexion avant d'enregistrer."
            Catch ex As Exception
                log.Error("Erreur lors du chargement de la configuration SQL.", ex)
                lblStatus.Text = "Impossible de charger la configuration actuelle."
            End Try
        End Sub

        Private Sub OnFieldChanged(sender As Object, e As EventArgs)
            _testOk = False
            btnSave.Enabled = False
            lblStatus.Text = "Modification détectée. Relancez un test de connexion."
            ApplyAuthMode()
        End Sub

        Private Sub ApplyAuthMode()
            Dim windowsAuth As Boolean = (cboAuth.SelectedIndex <= 0)
            txtUsername.Enabled = Not windowsAuth
            txtPassword.Enabled = Not windowsAuth
            btnTogglePassword.Enabled = Not windowsAuth
            If windowsAuth Then
                txtUsername.Text = String.Empty
                txtPassword.Text = String.Empty
            End If
        End Sub

        Private Sub TogglePasswordVisibility(sender As Object, e As EventArgs)
            txtPassword.UseSystemPasswordChar = Not txtPassword.UseSystemPasswordChar
            btnTogglePassword.Text = If(txtPassword.UseSystemPasswordChar, "Afficher", "Masquer")
        End Sub

        Private Sub TesterConnexion(sender As Object, e As EventArgs)
            Dim settings As SqlConnectionSettings = ConstruireSettingsDepuisEcran()
            Dim message As String = Nothing
            If SqlConfigurationService.TestConnection(settings, message) Then
                _testOk = True
                btnSave.Enabled = True
                lblStatus.ForeColor = Color.FromArgb(22, 163, 74)
                lblStatus.Text = "Connexion SQL testée avec succès."
                MessageBox.Show("Connexion réussie.", "Test de connexion", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                _testOk = False
                btnSave.Enabled = False
                lblStatus.ForeColor = Color.FromArgb(185, 28, 28)
                lblStatus.Text = "Connexion SQL impossible."
                MessageBox.Show("La connexion a échoué." & Environment.NewLine & If(String.IsNullOrWhiteSpace(message), String.Empty, message), "Test de connexion", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        End Sub

        Private Sub EnregistrerConfiguration(sender As Object, e As EventArgs)
            If Not _testOk Then
                MessageBox.Show("Testez la connexion avant d'enregistrer.", "Configuration SQL", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Try
                Dim settings As SqlConnectionSettings = ConstruireSettingsDepuisEcran()
                SqlConfigurationService.SaveSettings(settings)
                MessageBox.Show("Configuration SQL enregistrée avec succès.", "Configuration SQL", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Me.DialogResult = DialogResult.OK
                Me.Close()
            Catch ex As Exception
                log.Error("Erreur lors de l'enregistrement de la configuration SQL.", ex)
                MessageBox.Show("Impossible d'enregistrer la configuration SQL." & Environment.NewLine & ex.Message, "Configuration SQL", MessageBoxButtons.OK, MessageBoxIcon.Error)
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
            If Integer.TryParse(txtPort.Text.Trim(), portValue) Then
                settings.Port = portValue
            Else
                settings.Port = Nothing
            End If
            Return settings
        End Function
    End Class
End Namespace
