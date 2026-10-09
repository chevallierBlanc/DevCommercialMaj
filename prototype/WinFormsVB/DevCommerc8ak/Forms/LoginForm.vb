Imports System.Windows.Forms
Imports System.Drawing
Imports System.Configuration
Imports System.Drawing.Drawing2D
Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text

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
        Private ReadOnly chkAfficherMotDePasse As CheckBox
        Private ReadOnly chkSeSouvenir As CheckBox
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

            chkAfficherMotDePasse = New CheckBox() With {
                .Text = "Afficher le mot de passe",
                .Dock = DockStyle.Top,
                .Height = 28,
                .Font = New Font("Segoe UI", 8),
                .ForeColor = ColorTextSecondary,
                .Checked = False
            }
            AddHandler chkAfficherMotDePasse.CheckedChanged, Sub()
                                                                  txtPass.UseSystemPasswordChar = Not chkAfficherMotDePasse.Checked
                                                              End Sub

            chkSeSouvenir = New CheckBox() With {
                .Text = "Se souvenir de moi",
                .Dock = DockStyle.Top,
                .Height = 26,
                .Font = New Font("Segoe UI", 8),
                .ForeColor = ColorTextSecondary,
                .Checked = False
            }

            pnlInputs.Controls.AddRange({chkSeSouvenir, chkAfficherMotDePasse, txtPass, lblPass, pnlSpace1, txtUser, lblUser})

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
            ChargerUtilisateurMemorise()

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
            AddHandler btnClose.Click, Sub()
                                           ApplicationLifecycle.RequestShutdown()
                                           Me.Close()
                                       End Sub
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
            EnregistrerUtilisateurMemorise()
            Dim apiOk As Boolean = RemoteApiSession.Authentifier(txtUser.Text.Trim(), txtPass.Text)
            lblStatus.Text = If(apiOk, "Etat serveur: API connectee", "Etat serveur: API indisponible, mode local")

            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            OfflineSyncScheduler.Start(cs)

            Me.DialogResult = DialogResult.OK
            Me.Close()
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
                Dim resultat As AuthentificationResultat = service.AuthentifierCompte(nomUtilisateur, motDePasse)
                If resultat Is Nothing Then
                    Return False
                End If

                If resultat.Statut = AuthentificationStatut.CodeTemporaireValide Then
                    If Not ChangerMotDePasseTemporaire(service, resultat.Utilisateur) Then
                        Return False
                    End If
                    txtPass.Text = LireNouveauMotDePasseValide
                    resultat = service.AuthentifierCompte(nomUtilisateur, LireNouveauMotDePasseValide)
                End If

                If resultat.Statut <> AuthentificationStatut.Succes Then
                    MessageBox.Show(resultat.Message, "Connexion", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return False
                End If

                Dim utilisateur As Utilisateur = resultat.Utilisateur
                Dim roles As List(Of RoleSessionInfo) = service.ListerRolesActifs(utilisateur.UtilisateurId)
                If roles.Count = 0 Then
                    MessageBox.Show("Aucun rôle actif n'est associé à ce compte.", "Connexion", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return False
                End If

                Dim roleChoisi As RoleSessionInfo = roles(0)
                If roles.Count > 1 Then
                    Using frm As New FormChoixRoleSession(roles)
                        If frm.ShowDialog(Me) <> DialogResult.OK OrElse frm.RoleSelectionne Is Nothing Then
                            Return False
                        End If
                        roleChoisi = frm.RoleSelectionne
                    End Using
                End If

                Dim modesService As New ModeTravailService(dal)
                Dim modes As List(Of String) = modesService.Lister(utilisateur.UtilisateurId, roleChoisi.RoleId)
                Dim modeChoisi As String = Nothing
                If modes.Count = 1 Then
                    modeChoisi = modes(0)
                ElseIf modes.Count > 1 Then
                    Using selection As New DialogueModeTravail(modes)
                        If selection.ShowDialog(Me) <> DialogResult.OK Then Return False
                        modeChoisi = selection.ModeSelectionne
                    End Using
                End If
                Dim demarrage As AuthentificationResultat = service.DemarrerSessionApresAuthentification(utilisateur, roleChoisi)
                If demarrage.Statut <> AuthentificationStatut.Succes Then
                    MessageBox.Show(demarrage.Message, "Connexion", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return False
                End If
                SessionUtilisateur.ModeActif = String.Empty
                If modeChoisi IsNot Nothing Then
                    Try
                        modesService.Activer(modeChoisi)
                    Catch
                        ' Une permission retiree entre choix et activation ferme
                        ' la session ; aucun ERP n'est ouvert avec un mode invalide.
                        sessionRepo.FermerSession(SessionUtilisateur.SessionId)
                        SessionUtilisateur.Reinitialiser()
                        Throw
                    End Try
                End If
                Return True
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("LoginForm", "Authentifier", "Erreur technique lors de l'authentification utilisateur.", ex)
                MessageBox.Show(ex.Message, "Connexion", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End Try
        End Function

        Private _nouveauMotDePasseValide As String = String.Empty
        Private ReadOnly Property LireNouveauMotDePasseValide As String
            Get
                Return _nouveauMotDePasseValide
            End Get
        End Property

        Private Function ChangerMotDePasseTemporaire(service As UtilisateurService, utilisateur As Utilisateur) As Boolean
            Using frm As New FormChangementMotDePasseReset()
                If frm.ShowDialog(Me) <> DialogResult.OK Then Return False
                service.ChangerMotDePasseApresReset(utilisateur.UtilisateurId, frm.NouveauMotDePasse)
                _nouveauMotDePasseValide = frm.NouveauMotDePasse
                Return True
            End Using
        End Function

        Private Sub ChargerUtilisateurMemorise()
            Try
                Dim chemin As String = CheminRememberMe()
                If Not File.Exists(chemin) Then Return
                Dim protege As Byte() = File.ReadAllBytes(chemin)
                Dim clair As Byte() = ProtectedData.Unprotect(protege, Nothing, DataProtectionScope.CurrentUser)
                txtUser.Text = Encoding.UTF8.GetString(clair)
                chkSeSouvenir.Checked = txtUser.Text.Trim() <> String.Empty
            Catch
            End Try
        End Sub

        Private Sub EnregistrerUtilisateurMemorise()
            Try
                Dim chemin As String = CheminRememberMe()
                Dim dossier As String = Path.GetDirectoryName(chemin)
                If Not Directory.Exists(dossier) Then Directory.CreateDirectory(dossier)
                If Not chkSeSouvenir.Checked Then
                    If File.Exists(chemin) Then File.Delete(chemin)
                    Return
                End If

                ' Seul le nom utilisateur est mémorisé, protégé par DPAPI Windows.
                ' Aucun mot de passe ni code temporaire n'est stocké localement.
                Dim clair As Byte() = Encoding.UTF8.GetBytes(txtUser.Text.Trim())
                Dim protege As Byte() = ProtectedData.Protect(clair, Nothing, DataProtectionScope.CurrentUser)
                File.WriteAllBytes(chemin, protege)
            Catch
            End Try
        End Sub

        Private Shared Function CheminRememberMe() As String
            Dim dossier As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CommercialPro")
            Return Path.Combine(dossier, "remember-user.bin")
        End Function

        Private NotInheritable Class FormChangementMotDePasseReset
            Inherits Form

            Private ReadOnly txtNouveau As TextBox
            Private ReadOnly txtConfirmation As TextBox
            Private ReadOnly chkAfficher As CheckBox
            Public Property NouveauMotDePasse As String

            Public Sub New()
                Text = "Changer votre mot de passe"
                StartPosition = FormStartPosition.CenterParent
                FormBorderStyle = FormBorderStyle.FixedDialog
                MinimizeBox = False
                MaximizeBox = False
                ClientSize = New Size(420, 260)
                BackColor = Color.White

                Dim lblTitre As New Label() With {.Text = "CHANGER VOTRE MOT DE PASSE", .Dock = DockStyle.Top, .Height = 56, .TextAlign = ContentAlignment.MiddleCenter, .Font = New Font("Segoe UI", 12, FontStyle.Bold), .ForeColor = Color.FromArgb(52, 73, 94)}
                Dim pnl As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 4, .Padding = New Padding(22)}
                pnl.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 140))
                pnl.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                txtNouveau = New TextBox() With {.Dock = DockStyle.Fill, .UseSystemPasswordChar = True}
                txtConfirmation = New TextBox() With {.Dock = DockStyle.Fill, .UseSystemPasswordChar = True}
                chkAfficher = New CheckBox() With {.Text = "Afficher / masquer", .Dock = DockStyle.Fill}
                AddHandler chkAfficher.CheckedChanged, Sub()
                                                           txtNouveau.UseSystemPasswordChar = Not chkAfficher.Checked
                                                           txtConfirmation.UseSystemPasswordChar = Not chkAfficher.Checked
                                                       End Sub
                pnl.Controls.Add(New Label() With {.Text = "Nouveau mot de passe", .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}, 0, 0)
                pnl.Controls.Add(txtNouveau, 1, 0)
                pnl.Controls.Add(New Label() With {.Text = "Confirmation", .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}, 0, 1)
                pnl.Controls.Add(txtConfirmation, 1, 1)
                pnl.Controls.Add(chkAfficher, 1, 2)

                Dim actions As New FlowLayoutPanel() With {.Dock = DockStyle.Bottom, .Height = 55, .FlowDirection = FlowDirection.RightToLeft, .Padding = New Padding(10)}
                Dim btnSave As New Button() With {.Text = "Enregistrer", .Width = 120, .Height = 32, .BackColor = Color.FromArgb(41, 128, 185), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
                Dim btnCancel As New Button() With {.Text = "Annuler", .Width = 90, .Height = 32, .DialogResult = DialogResult.Cancel}
                AddHandler btnSave.Click, AddressOf Valider
                actions.Controls.AddRange(New Control() {btnSave, btnCancel})

                Controls.Add(pnl)
                Controls.Add(actions)
                Controls.Add(lblTitre)
                AcceptButton = btnSave
                CancelButton = btnCancel
            End Sub

            Private Sub Valider(sender As Object, e As EventArgs)
                If txtNouveau.Text.Length < 4 Then
                    MessageBox.Show(Me, "Le mot de passe doit contenir au moins 4 caractères.", "Mot de passe", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                If txtNouveau.Text <> txtConfirmation.Text Then
                    MessageBox.Show(Me, "La confirmation ne correspond pas.", "Mot de passe", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                NouveauMotDePasse = txtNouveau.Text
                DialogResult = DialogResult.OK
            End Sub
        End Class

        Private NotInheritable Class FormChoixRoleSession
            Inherits Form

            Private ReadOnly lstRoles As ListBox
            Private ReadOnly _roles As List(Of RoleSessionInfo)
            Public Property RoleSelectionne As RoleSessionInfo

            Public Sub New(roles As List(Of RoleSessionInfo))
                _roles = roles
                Text = "Choix du rôle"
                StartPosition = FormStartPosition.CenterParent
                FormBorderStyle = FormBorderStyle.FixedDialog
                MinimizeBox = False
                MaximizeBox = False
                ClientSize = New Size(360, 260)
                BackColor = Color.White

                Dim lbl As New Label() With {
                    .Text = "Sélectionnez le rôle pour cette session",
                    .Dock = DockStyle.Top,
                    .Height = 45,
                    .TextAlign = ContentAlignment.MiddleCenter,
                    .Font = New Font("Segoe UI", 10, FontStyle.Bold)
                }
                lstRoles = New ListBox() With {.Dock = DockStyle.Fill, .Font = New Font("Segoe UI", 10)}
                For Each role As RoleSessionInfo In _roles
                    lstRoles.Items.Add(role.NomRole)
                Next
                If lstRoles.Items.Count > 0 Then
                    lstRoles.SelectedIndex = 0
                End If

                Dim pnlActions As New FlowLayoutPanel() With {.Dock = DockStyle.Bottom, .Height = 55, .FlowDirection = FlowDirection.RightToLeft, .Padding = New Padding(10)}
                Dim btnOk As New Button() With {.Text = "Continuer", .Width = 110, .Height = 32, .DialogResult = DialogResult.None}
                Dim btnCancel As New Button() With {.Text = "Annuler", .Width = 90, .Height = 32, .DialogResult = DialogResult.Cancel}
                AddHandler btnOk.Click, AddressOf Valider
                pnlActions.Controls.AddRange(New Control() {btnOk, btnCancel})

                Controls.Add(lstRoles)
                Controls.Add(pnlActions)
                Controls.Add(lbl)
                AcceptButton = btnOk
                CancelButton = btnCancel
            End Sub

            Private Sub Valider(sender As Object, e As EventArgs)
                If lstRoles.SelectedIndex < 0 OrElse lstRoles.SelectedIndex >= _roles.Count Then
                    MessageBox.Show(Me, "Sélectionnez un rôle.", "Connexion", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                RoleSelectionne = _roles(lstRoles.SelectedIndex)
                DialogResult = DialogResult.OK
            End Sub
        End Class

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

                roleRepo.AssurerRole("SUPERADMIN")

                Using bootstrap As New FormulaireBootstrapSuperAdmin(service)
                    If bootstrap.ShowDialog(Me) <> DialogResult.OK Then
                        MessageBox.Show("La création du compte SUPERADMIN initial est obligatoire au premier démarrage.")
                        Me.DialogResult = DialogResult.Cancel
                        Me.Close()
                        Return
                    End If
                End Using
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("LoginForm", "InitialiserCompteAdminSiNecessaire", "Erreur lors de l'initialisation du compte administrateur.", ex)
                MessageBox.Show("Impossible d'initialiser le compte administrateur initial : " & ex.Message)
                Me.DialogResult = DialogResult.Cancel
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
