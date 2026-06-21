Imports System.Windows.Forms
Imports System.Drawing
Imports System.Configuration
Imports System.Drawing.Drawing2D
Imports Microsoft.VisualBasic
Imports System

Namespace DevCommerc8ak
    Public Class LoginForm
        Inherits Form

        ' --- Palette de Couleurs Identité Visuelle ---
        Private ReadOnly ColorBg As Color = Color.FromArgb(240, 242, 245)
        Private ReadOnly ColorCardBg As Color = Color.White
        Private ReadOnly ColorAccent As Color = Color.FromArgb(59, 130, 246) ' Bleu Moderne
        Private ReadOnly ColorTextPrimary As Color = Color.FromArgb(31, 41, 55)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(107, 114, 128)
        Private ReadOnly ColorSuccess As Color = Color.FromArgb(16, 185, 129)

        ' --- Polices ---
        Private ReadOnly FontMain As New Font("Segoe UI", 10)
        Private ReadOnly FontBold As New Font("Segoe UI", 10, FontStyle.Bold)
        Private ReadOnly FontTitle As New Font("Segoe UI", 18, FontStyle.Bold)

        ' --- Composants (Noms conservés) ---
        Private ReadOnly txtUser As Guna.UI2.WinForms.Guna2TextBox
        Private ReadOnly txtPass As Guna.UI2.WinForms.Guna2TextBox
        Private ReadOnly btnLogin As Button
        Private ReadOnly lblStatus As Label

        Public Sub New()
            ' Configuration de la Form
            Me.Text = "Accès au Système - Commercial Pro"
            Me.Size = New Size(450, 550)
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = ColorBg
            Me.FormBorderStyle = FormBorderStyle.None ' Design sans bordures pour un look moderne
            Me.DoubleBuffered = True
            Me.KeyPreview = True

            ' --- Carte de Connexion Centrale ---
            Dim pnlCard As New Panel() With {
                .Size = New Size(380, 480),
                .Location = New Point(35, 35),
                .BackColor = ColorCardBg
            }
            AddHandler pnlCard.Paint, Sub(s, e)
                                          Dim rect As New Rectangle(0, 0, pnlCard.Width - 1, pnlCard.Height - 1)
                                          e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                          Using pen As New Pen(Color.FromArgb(230, 230, 230), 1)
                                              e.Graphics.DrawRectangle(pen, rect)
                                          End Using
                                      End Sub

            ' Logo / Titre
            Dim lblAppTitle As New Label() With {
                .Text = "COMMERCIAL PRO",
                .Font = FontTitle,
                .ForeColor = ColorAccent,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Dock = DockStyle.Top,
                .Height = 80
            }

            Dim lblWelcome As New Label() With {
                .Text = "Bienvenue" & vbCrLf & "Veuillez vous identifier pour continuer",
                .Font = FontMain,
                .ForeColor = ColorTextSecondary,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Dock = DockStyle.Top,
                .Height = 60
            }

            ' Champs de saisie
            Dim pnlInputs As New Panel() With {.Dock = DockStyle.Top, .Height = 220, .Padding = New Padding(40, 20, 40, 0)}

            Dim lblUser As New Label() With {.Text = "UTILISATEUR", .Font = New Font("Segoe UI", 8, FontStyle.Bold), .ForeColor = ColorTextSecondary, .Dock = DockStyle.Top, .Height = 25}
            'txtUser = New TextBox() With {
            '    .Dock = DockStyle.Top,
            '    .Font = FontMain,
            '    .BorderStyle = BorderStyle.FixedSingle,
            '    .Height = 35
            '}


            Me.txtUser = New Guna.UI2.WinForms.Guna2TextBox
            Me.txtUser.Dock = DockStyle.Top
            Me.txtUser.BorderColor = Color.FromArgb(224, 224, 224)
            Me.txtUser.BorderRadius = 5
            Me.txtUser.Cursor = System.Windows.Forms.Cursors.IBeam
            Me.txtUser.DefaultText = ""
            Me.txtUser.DisabledState.BorderColor = Color.FromArgb(208, 208, 208)
            Me.txtUser.DisabledState.FillColor = Color.FromArgb(226, 226, 226)
            Me.txtUser.DisabledState.ForeColor = Color.FromArgb(138, 138, 138)
            Me.txtUser.DisabledState.Parent = Me.txtUser
            Me.txtUser.DisabledState.PlaceholderForeColor = Color.FromArgb(138, 138, 138)
            Me.txtUser.FocusedState.BorderColor = Color.FromArgb(0, 52, 91)
            Me.txtUser.FocusedState.ForeColor = System.Drawing.Color.Black
            Me.txtUser.FocusedState.Parent = Me.txtUser
            Me.txtUser.FocusedState.PlaceholderForeColor = System.Drawing.Color.Black
            Me.txtUser.HoverState.BorderColor = Color.FromArgb(0, 52, 91)
            Me.txtUser.HoverState.ForeColor = System.Drawing.Color.Black
            Me.txtUser.HoverState.Parent = Me.txtUser
            Me.txtUser.HoverState.PlaceholderForeColor = System.Drawing.Color.Black
            Me.txtUser.IconRightCursor = System.Windows.Forms.Cursors.Hand
            '  Me.txtPass.Location = New Point(40, 250)
            Me.txtUser.Margin = New System.Windows.Forms.Padding(5)
            Me.txtUser.Name = "txtUser"
            Me.txtUser.PasswordChar = Global.Microsoft.VisualBasic.ChrW(0)
            Me.txtUser.PlaceholderForeColor = System.Drawing.Color.Black
            Me.txtUser.PlaceholderText = "Saisir votre Nom d'utilisateur"
            Me.txtUser.SelectedText = ""
            Me.txtUser.ShadowDecoration.Parent = Me.txtUser
            Me.txtUser.MinimumSize = New Size(320, 40)
            Me.txtUser.Size = New Size(320, 40)
            Me.txtUser.TabIndex = 41
            ' Me.txtUser.UseSystemPasswordChar = True

            Dim pnlSpace1 As New Panel() With {.Dock = DockStyle.Top, .Height = 20}

            Dim lblPass As New Label() With {.Text = "MOT DE PASSE", .Font = New Font("Segoe UI", 8, FontStyle.Bold), .ForeColor = ColorTextSecondary, .Dock = DockStyle.Top, .Height = 25}
            'txtPass = New TextBox() With {
            '    .Dock = DockStyle.Top,
            '    .Font = FontMain,
            '    .BorderStyle = BorderStyle.FixedSingle,
            '    .Height = 35,
            '    .UseSystemPasswordChar = True
            '}

            Me.txtPass = New Guna.UI2.WinForms.Guna2TextBox
            Me.txtPass.Dock = DockStyle.Top
            Me.txtPass.BorderColor = Color.FromArgb(224, 224, 224)
            Me.txtPass.BorderRadius = 5
            Me.txtPass.Cursor = System.Windows.Forms.Cursors.IBeam
            Me.txtPass.DefaultText = ""
            Me.txtPass.DisabledState.BorderColor = Color.FromArgb(208, 208, 208)
            Me.txtPass.DisabledState.FillColor = Color.FromArgb(226, 226, 226)
            Me.txtPass.DisabledState.ForeColor = Color.FromArgb(138, 138, 138)
            Me.txtPass.DisabledState.Parent = Me.txtPass
            Me.txtPass.DisabledState.PlaceholderForeColor = Color.FromArgb(138, 138, 138)
            Me.txtPass.FocusedState.BorderColor = Color.FromArgb(0, 52, 91)
            Me.txtPass.FocusedState.ForeColor = System.Drawing.Color.Black
            Me.txtPass.FocusedState.Parent = Me.txtPass
            Me.txtPass.FocusedState.PlaceholderForeColor = System.Drawing.Color.Black
            Me.txtPass.HoverState.BorderColor = Color.FromArgb(0, 52, 91)
            Me.txtPass.HoverState.ForeColor = System.Drawing.Color.Black
            Me.txtPass.HoverState.Parent = Me.txtPass
            Me.txtPass.HoverState.PlaceholderForeColor = System.Drawing.Color.Black
            Me.txtPass.IconRightCursor = System.Windows.Forms.Cursors.Hand
            '  Me.txtPass.Location = New Point(40, 250)
            Me.txtPass.Margin = New System.Windows.Forms.Padding(5)
            Me.txtPass.Name = "txtPass"
            Me.txtPass.PasswordChar = Global.Microsoft.VisualBasic.ChrW(0)
            Me.txtPass.PlaceholderForeColor = System.Drawing.Color.Black
            Me.txtPass.PlaceholderText = "Saisir votre mot de passe"
            Me.txtPass.SelectedText = ""
            Me.txtPass.ShadowDecoration.Parent = Me.txtPass
            Me.txtPass.MinimumSize = New Size(320, 40)
            Me.txtPass.Size = New Size(320, 40)
            Me.txtPass.TabIndex = 41
            Me.txtPass.UseSystemPasswordChar = True

            pnlInputs.Controls.AddRange({txtPass, lblPass, pnlSpace1, txtUser, lblUser})

            ' Bouton de connexion
            Dim pnlAction As New Panel() With {.Dock = DockStyle.Top, .Height = 80, .Padding = New Padding(40, 10, 40, 0)}
            btnLogin = New Button() With {
                .Text = "SE CONNECTER",
                .Dock = DockStyle.Fill,
                .FlatStyle = FlatStyle.Flat,
                .BackColor = ColorAccent,
                .ForeColor = Color.White,
                .Font = FontBold,
                .Cursor = Cursors.Hand
            }
            btnLogin.FlatAppearance.BorderSize = 0
            AddHandler btnLogin.Click, AddressOf OnLogin
            pnlAction.Controls.Add(btnLogin)
            Me.AcceptButton = btnLogin
            AddHandler Me.KeyDown, AddressOf LoginForm_KeyDown

            ' Statut Serveur
            lblStatus = New Label() With {
                .Text = "État serveur: CONNECTÉ",
                .Font = New Font("Segoe UI", 8),
                .ForeColor = ColorSuccess,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Dock = DockStyle.Bottom,
                .Height = 40
            }

            ' Bouton Fermer (pour la Form sans bordures)
            Dim btnClose As New Button() With {
                .Text = "×",
                .Size = New Size(30, 30),
                .Location = New Point(345, 5),
                .FlatStyle = FlatStyle.Flat,
                .ForeColor = ColorTextSecondary,
                .Font = New Font("Arial", 12, FontStyle.Bold),
                .Cursor = Cursors.Hand
            }
            btnClose.FlatAppearance.BorderSize = 0
            AddHandler btnClose.Click, Sub() Me.Close()
            pnlCard.Controls.Add(btnClose)

            ' Assemblage de la carte
            pnlCard.Controls.AddRange({lblStatus, pnlAction, pnlInputs, lblWelcome, lblAppTitle})
            Me.Controls.Add(pnlCard)

            ' Logique de déplacement de la fenêtre (puisque sans bordures)
            AddHandler pnlCard.MouseDown, Sub(s, e)
                                              If e.Button = MouseButtons.Left Then
                                                  pnlCard.Capture = False
                                                  Const WM_NCLBUTTONDOWN As Integer = &HA1
                                                  Const HTCAPTION As Integer = 2
                                                  Dim msg As Message = Message.Create(Me.Handle, WM_NCLBUTTONDOWN, New IntPtr(HTCAPTION), IntPtr.Zero)
                                                  Me.DefWndProc(msg)
                                              End If
                                          End Sub

            ' Initialisation Logique (Inchangée)
            'ChargerModeSombre()
            'ThemeHelper.AppliquerTheme(Me)
            'IconsHelper.AppliquerIconeFormulaire(Me)
            InitialiserCompteAdminSiNecessaire()
        End Sub

        Private Sub LoginForm_KeyDown(sender As Object, e As KeyEventArgs)
            If e.KeyCode = Keys.Enter Then
                e.Handled = True
                e.SuppressKeyPress = True
                btnLogin.PerformClick()
            End If
        End Sub

        ' --- Logique Métier (Inchangée) ---



        Private Sub OnLogin(sender As Object, e As EventArgs)
            Dim log As New ProductionLogService()
            Dim erreurSql As String = Nothing
            If Not SqlConfigurationService.HasValidConnection(erreurSql) Then
                log.Warn("LoginForm", "OnLogin", "Connexion SQL indisponible lors de la tentative de login.")
                MessageBox.Show("La connexion SQL est indisponible ou invalide. Ouvrez la configuration SQL pour corriger le serveur, la base ou les identifiants." &
                                If(String.IsNullOrWhiteSpace(erreurSql), String.Empty, Environment.NewLine & erreurSql),
                                "Connexion SQL",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning)
                Return
            End If

            log.Info("LoginForm", "OnLogin", "Tentative de login utilisateur: " & txtUser.Text.Trim())
            Dim ok As Boolean = Authentifier(txtUser.Text.Trim(), txtPass.Text)
            If Not ok Then
                log.Warn("LoginForm", "OnLogin", "Login échoué pour l'utilisateur: " & txtUser.Text.Trim())
                MessageBox.Show("Identifiants invalides.")
                Return
            End If

            log.Info("LoginForm", "OnLogin", "Login réussi pour l'utilisateur: " & txtUser.Text.Trim())
            Dim apiOk As Boolean = RemoteApiSession.Authentifier(txtUser.Text.Trim(), txtPass.Text)
            lblStatus.Text = If(apiOk, "Etat serveur: API connectee", "Etat serveur: API indisponible, mode local")

            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            OfflineSyncScheduler.Start(cs)

            Dim main As New MainForm()
            main.Show()
            Me.Hide()
        End Sub

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            Try
                ApplicationLifecycle.StopBackgroundServices()
            Catch
            End Try
            MyBase.OnFormClosed(e)
        End Sub

        Private Function Authentifier(nomUtilisateur As String, motDePasse As String) As Boolean
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim utilisateurRepo As New UtilisateurRepository(dal)
                Dim roleRepo As New RoleRepository(dal)
                Dim sessionRepo As New SessionRepository(dal)
                Dim service As New UtilisateurService(utilisateurRepo, roleRepo, sessionRepo)
                Return service.VerifierConnexion(nomUtilisateur, motDePasse)
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("LoginForm", "Authentifier", "Erreur technique lors de l'authentification utilisateur.", ex)
                Return False
            End Try
        End Function

        Private Sub InitialiserCompteAdminSiNecessaire()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim utilisateurRepo As New UtilisateurRepository(dal)

                If utilisateurRepo.Lister().Count > 0 Then
                    Return
                End If

                Dim roleRepo As New RoleRepository(dal)
                Dim sessionRepo As New SessionRepository(dal)
                Dim service As New UtilisateurService(utilisateurRepo, roleRepo, sessionRepo)

                roleRepo.AssurerRole("ADMIN")

                Dim motDePasse As String = Interaction.InputBox(
                    "Aucun compte n'existe. Saisissez le mot de passe du premier administrateur.",
                    "Création du compte admin",
                    "")
                If String.IsNullOrWhiteSpace(motDePasse) Then
                    MessageBox.Show("La création du compte administrateur est obligatoire au premier démarrage.")
                    Me.Close()
                    Return
                End If

                Dim confirmation As String = Interaction.InputBox(
                    "Confirmez le mot de passe du premier administrateur.",
                    "Création du compte admin",
                    "")
                If motDePasse <> confirmation Then
                    MessageBox.Show("La confirmation ne correspond pas. Le compte administrateur n'a pas été créé.")
                    Me.Close()
                    Return
                End If

                service.CreerUtilisateur("admin", motDePasse, "ADMIN")
                MessageBox.Show("Compte administrateur initial créé. Utilisez l'utilisateur 'admin'.")
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("LoginForm", "InitialiserCompteAdminSiNecessaire", "Erreur lors de l'initialisation du compte administrateur.", ex)
                MessageBox.Show("Impossible d'initialiser le compte administrateur initial : " & ex.Message)
                Me.Close()
            End Try
        End Sub

        Private Sub ChargerModeSombre()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim paramService As New ParametreService(New ParametreRepository(dal))
                Dim p As ParametreDTO = paramService.Charger()
                If p IsNot Nothing Then
                    ThemeHelper.DefinirModeSombre(p.ModeSombre)
                End If
            Catch
            End Try
        End Sub

        Private Sub LoginForm_Activated(sender As Object, e As EventArgs) Handles Me.Activated

        End Sub
    End Class
End Namespace
