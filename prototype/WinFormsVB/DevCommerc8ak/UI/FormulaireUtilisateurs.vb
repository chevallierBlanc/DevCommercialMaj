Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireUtilisateurs
        Inherits Form

        ' --- Constantes de Design ---
        'Private ReadOnly ColorPrimary As Color = Color.FromArgb(63, 81, 181) ' Indigo
        'Private ReadOnly ColorSecondary As Color = Color.FromArgb(48, 63, 159)
        'Private ReadOnly ColorAccent As Color = Color.FromArgb(255, 64, 129)
        Private ReadOnly ColorBackground As Color = Color.FromArgb(245, 247, 250)
        Private ReadOnly ColorCard As Color = Color.White
        Private ReadOnly ColorText As Color = Color.FromArgb(33, 33, 33)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(117, 117, 117)
        Private ReadOnly ColorBorder As Color = Color.FromArgb(230, 230, 230)
        ' Private ReadOnly FontTitle As New Font("Segoe UI Semibold", 18.0F)
        Private ReadOnly FontSubTitle As New Font("Segoe UI", 10.0F)
        Private ReadOnly FontLabel As New Font("Segoe UI Semibold", 9.0F)
        Private ReadOnly FontControl As New Font("Segoe UI", 9.5F)


        Private ReadOnly ColorPrimary As Color = Color.FromArgb(52, 73, 94) ' Gris Foncé
        Private ReadOnly ColorSecondary As Color = Color.FromArgb(41, 128, 185) ' Bleu Moderne
        Private ReadOnly ColorAccent As Color = Color.FromArgb(39, 174, 96) ' Vert Succès
        Private ReadOnly ColorDanger As Color = Color.FromArgb(192, 57, 43) ' Rouge Annuler
        Private ReadOnly ColorBg As Color = Color.FromArgb(245, 247, 250) ' Gris très clair
        Private ReadOnly ColorWhite As Color = Color.White
        Private ReadOnly FontMain As New Font("Segoe UI", 10)
        Private ReadOnly FontBold As New Font("Segoe UI", 10, FontStyle.Bold)
        Private ReadOnly FontTitle As New Font("Segoe UI", 18.0F, FontStyle.Bold)
        Private ReadOnly FontTotal As New Font("Segoe UI", 22, FontStyle.Bold)


        ' --- Composants UI (Noms conservés) ---
        Private ReadOnly grid As DataGridView
        Private ReadOnly gridConnectes As DataGridView
        Private ReadOnly timer As Timer
        Private ReadOnly txtNom As TextBox
        Private ReadOnly txtMotDePasse As TextBox
        Private ReadOnly cmbRole As ComboBox
        Private ReadOnly clbRolesAutorises As CheckedListBox
        Private ReadOnly chkActif As CheckBox
        Private ReadOnly btnAjouter As Button
        Private ReadOnly btnModifier As Button
        Private ReadOnly btnResetMdp As Button
        Private ReadOnly btnRafraichir As Button
        Private _utilisateurSelectionneId As Integer = -1

        ' --- Nouveaux composants de structure (Layouts propres) ---
        Private ReadOnly panelHero As Panel
        Private ReadOnly lblHeroTitre As Label
        Private ReadOnly lblHeroSousTitre As Label
        Private ReadOnly mainTableLayout As TableLayoutPanel
        Private ReadOnly cardForm As Panel
        Private ReadOnly flowButtons As FlowLayoutPanel
        Private ReadOnly splitGrids As TableLayoutPanel
        Private _chargementUtilisateursEnCours As Boolean
        Private _chargementConnectesEnCours As Boolean
        Private _chargementRolesEnCours As Boolean

        Public Sub New()
            ' Configuration de base du formulaire
            Me.Text = "Gestion des Utilisateurs"
            Me.Width = 1100
            Me.Height = 800
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = ColorBackground
            Me.DoubleBuffered = True

            ' --- Header / Hero Section ---
            panelHero = New Panel() With {.Dock = DockStyle.Top, .Height = 90, .BackColor = ColorPrimary}
            lblHeroTitre = New Label() With {.Text = "Administration Utilisateurs", .Font = FontTitle, .AutoSize = True, .Left = 20, .Top = 15, .ForeColor = Color.White}
            lblHeroSousTitre = New Label() With {.Text = "Gérez les accès, les rôles et surveillez les sessions actives en temps réel.", .Left = 27, .Top = 54, .AutoSize = True, .Font = FontSubTitle, .ForeColor = Color.FromArgb(210, 210, 255)}
            panelHero.Controls.Add(lblHeroTitre)
            panelHero.Controls.Add(lblHeroSousTitre)

            ' --- Layout Principal ---
            mainTableLayout = New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3,
                .Padding = New Padding(20)
            }
            mainTableLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 170)) ' Carte Formulaire
            mainTableLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 60))  ' Boutons
            mainTableLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))  ' Grilles

            ' --- Carte de Formulaire ---
            cardForm = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorCard,
                .Padding = New Padding(20)
            }

            Dim formTable As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 5,
                .RowCount = 2
            }
            formTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 24))
            formTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 21))
            formTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))
            formTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 23))
            formTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 12))

            ' Initialisation des contrôles (Noms conservés)
            txtNom = CreateStyledTextBox()
            txtMotDePasse = CreateStyledTextBox()
            txtMotDePasse.PasswordChar = "*"c

            cmbRole = New ComboBox() With {
                .Dock = DockStyle.Top,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = FontControl,
                .FlatStyle = FlatStyle.Flat,
                .Margin = New Padding(0, 0, 20, 0)
            }
            clbRolesAutorises = New CheckedListBox() With {
                .Dock = DockStyle.Fill,
                .CheckOnClick = True,
                .Font = FontControl,
                .BorderStyle = BorderStyle.FixedSingle,
                .Margin = New Padding(0, 0, 20, 0)
            }
            chkActif = New CheckBox() With {
                .Text = "Compte Actif",
                .Font = FontControl,
                .ForeColor = ColorText,
                .AutoSize = True,
                .Margin = New Padding(0, 5, 0, 0)
            }

            ' Ajout au layout de formulaire
            formTable.Controls.Add(CreateLabel("Nom d'utilisateur"), 0, 0)
            formTable.Controls.Add(txtNom, 0, 1)
            formTable.Controls.Add(CreateLabel("Mot de passe"), 1, 0)
            formTable.Controls.Add(txtMotDePasse, 1, 1)
            formTable.Controls.Add(CreateLabel("Rôle / Privilège"), 2, 0)
            formTable.Controls.Add(cmbRole, 2, 1)
            formTable.Controls.Add(CreateLabel("Rôles autorisés"), 3, 0)
            formTable.Controls.Add(clbRolesAutorises, 3, 1)
            formTable.Controls.Add(chkActif, 4, 1)

            cardForm.Controls.Add(formTable)

            ' --- Barre de Boutons (FlowLayout) ---
            flowButtons = New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.LeftToRight,
                .Padding = New Padding(0, 10, 0, 0)
            }

            btnAjouter = CreateStyledButton("Ajouter", ColorPrimary)
            btnModifier = CreateStyledButton("Modifier", ColorAccent)
            btnResetMdp = CreateStyledButton("Reset MDP", ColorSecondary)
            btnRafraichir = CreateStyledButton("Rafraîchir", Color.Gray)

            flowButtons.Controls.AddRange(New Control() {btnAjouter, btnModifier, btnResetMdp, btnRafraichir})

            ' --- Grilles (Split Vertical) ---
            splitGrids = New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2
            }
            splitGrids.RowStyles.Add(New RowStyle(SizeType.Percent, 60)) ' Liste Utilisateurs
            splitGrids.RowStyles.Add(New RowStyle(SizeType.Percent, 40)) ' Sessions Actives

            grid = CreateStyledGrid("Liste des Utilisateurs")
            gridConnectes = CreateStyledGrid("Sessions Actives (Temps Réel)")

            ' Conteneurs pour titres de grilles
            Dim p1 As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 0, 0, 10)}
            p1.Controls.Add(grid)
            p1.Controls.Add(New Label() With {.Text = "Répertoire des comptes", .Dock = DockStyle.Top, .Height = 25, .Font = FontLabel, .ForeColor = ColorPrimary})

            Dim p2 As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 10, 0, 0)}
            p2.Controls.Add(gridConnectes)
            p2.Controls.Add(New Label() With {.Text = "Utilisateurs actuellement connectés", .Dock = DockStyle.Top, .Height = 25, .Font = FontLabel, .ForeColor = ColorAccent})

            splitGrids.Controls.Add(p1, 0, 0)
            splitGrids.Controls.Add(p2, 0, 1)

            ' Assemblage final
            mainTableLayout.Controls.Add(cardForm, 0, 0)
            mainTableLayout.Controls.Add(flowButtons, 0, 1)
            mainTableLayout.Controls.Add(splitGrids, 0, 2)

            Me.Controls.Add(mainTableLayout)
            Me.Controls.Add(panelHero)

            ' --- Liaison des événements (Logique conservée) ---
            AddHandler btnAjouter.Click, AddressOf Ajouter
            AddHandler btnModifier.Click, AddressOf Modifier
            AddHandler btnResetMdp.Click, AddressOf ResetMdp
            AddHandler btnRafraichir.Click, AddressOf Charger
            AddHandler grid.SelectionChanged, AddressOf ChargerSelectionUtilisateur
            AddHandler grid.CellClick, AddressOf Grid_CellClick

            ' --- Initialisation ---
            'ThemeHelper.AppliquerTheme(Me)
            timer = New Timer() With {.Interval = 5000}
            AddHandler timer.Tick, AddressOf ChargerConnectes
            timer.Start()
            AddHandler AppEvents.RolePermissionsChanged, AddressOf RafraichirRolesDepuisEvenement

            ' Chargement initial
            AddHandler Me.Load, AddressOf Charger
        End Sub

        ' --- Helpers de Design ---

        Private Function CreateLabel(text As String) As Label
            Return New Label() With {
                .Text = text,
                .AutoSize = True,
                .Font = FontLabel,
                .ForeColor = ColorTextSecondary,
                .Margin = New Padding(0, 0, 0, 2)
            }
        End Function

        Private Function CreateStyledTextBox() As TextBox
            Return New TextBox() With {
                .Dock = DockStyle.Top,
                .Font = FontControl,
                .BorderStyle = BorderStyle.FixedSingle,
                .Margin = New Padding(0, 0, 20, 0)
            }
        End Function

        Private Function CreateStyledButton(text As String, backColor As Color) As Button
            Dim btn As New Button() With {
                .Text = text,
                .Width = 120,
                .Height = 38,
                .BackColor = backColor,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontLabel,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 10, 0)
            }
            btn.FlatAppearance.BorderSize = 0
            Return btn
        End Function

        Private Function CreateStyledGrid(title As String) As DataGridView
            Dim dgv As New DataGridView() With {
                .Dock = DockStyle.Fill,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None,
                .EnableHeadersVisualStyles = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AllowUserToAddRows = False,
                .ReadOnly = True,
                .RowHeadersVisible = False,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .GridColor = ColorBorder
            }
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
            dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            dgv.ColumnHeadersHeight = 40
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 234, 246)
            dgv.DefaultCellStyle.SelectionForeColor = ColorPrimary
            dgv.DefaultCellStyle.Font = FontControl
            dgv.RowTemplate.Height = 32
            Return dgv
        End Function

        Private Sub ConfigurerGrilleUtilisateurs()
            If grid.Columns.Count = 0 Then
                Return
            End If

            If grid.Columns.Contains("UtilisateurId") Then
                grid.Columns("UtilisateurId").Visible = False
            End If
            If grid.Columns.Contains("NomUtilisateur") Then
                grid.Columns("NomUtilisateur").HeaderText = "Nom d'utilisateur"
            End If
            If grid.Columns.Contains("EstActif") Then
                grid.Columns("EstActif").HeaderText = "Compte actif"
            End If
            If grid.Columns.Contains("Role") Then
                grid.Columns("Role").HeaderText = "Rôle"
            End If
        End Sub

        ' --- LOGIQUE MÉTIER (STRICTEMENT IDENTIQUE À L'ORIGINAL) ---

        Private Function ObtenirService() As UtilisateurService
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim utilisateurRepo As New UtilisateurRepository(dal)
            Dim roleRepo As New RoleRepository(dal)
            Dim sessionRepo As New SessionRepository(dal)
            Return New UtilisateurService(utilisateurRepo, roleRepo, sessionRepo)
        End Function

        Private Async Function ChargerRolesDisponiblesAsync() As Task
            If _chargementRolesEnCours Then
                Return
            End If

            _chargementRolesEnCours = True
            Try
                Dim service As New SuperAdminService()
                Dim roles As List(Of String) = Await Task.Run(Function() service.ListerNomsRoles())
                If IsDisposed OrElse cmbRole Is Nothing Then
                    Return
                End If

                Dim roleSelectionne As String = If(cmbRole.SelectedItem Is Nothing, String.Empty, Convert.ToString(cmbRole.SelectedItem))
                cmbRole.Items.Clear()
                clbRolesAutorises.Items.Clear()
                For Each role As String In roles
                    cmbRole.Items.Add(role)
                    clbRolesAutorises.Items.Add(role, False)
                Next

                If roleSelectionne <> String.Empty AndAlso cmbRole.Items.Contains(roleSelectionne) Then
                    cmbRole.SelectedItem = roleSelectionne
                ElseIf cmbRole.Items.Count > 0 AndAlso cmbRole.SelectedIndex < 0 Then
                    cmbRole.SelectedIndex = 0
                End If
            Catch
            Finally
                _chargementRolesEnCours = False
            End Try
        End Function

        Private Async Sub Charger(sender As Object, e As EventArgs)
            If _chargementUtilisateursEnCours Then
                Return
            End If

            _chargementUtilisateursEnCours = True
            Try
                Await ChargerRolesDisponiblesAsync()
                Dim service As UtilisateurService = ObtenirService()
                Dim utilisateurs As List(Of UtilisateurDTO) = Await Task.Run(Function() service.Lister())
                If IsDisposed OrElse grid Is Nothing Then
                    Return
                End If
                grid.DataSource = utilisateurs
                ConfigurerGrilleUtilisateurs()
                ChargerSelectionUtilisateur(Nothing, EventArgs.Empty)
            Catch ex As Exception
                MessageBox.Show("Erreur chargement utilisateurs: " & ex.Message)
            Finally
                _chargementUtilisateursEnCours = False
            End Try
        End Sub

        Private Async Sub ChargerConnectes(sender As Object, e As EventArgs)
            If _chargementConnectesEnCours Then
                Return
            End If

            _chargementConnectesEnCours = True
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim repo As New SessionRepository(dal)
                Dim dt As DataTable = Await Task.Run(Function() repo.ListerConnectes())
                If Not IsDisposed AndAlso gridConnectes IsNot Nothing Then
                    gridConnectes.DataSource = dt
                End If
            Catch
            Finally
                _chargementConnectesEnCours = False
            End Try
        End Sub

        Private Sub Ajouter(sender As Object, e As EventArgs)
            Try
                If txtNom.Text.Trim() = "" OrElse txtMotDePasse.Text.Trim() = "" OrElse cmbRole.SelectedItem Is Nothing Then
                    MessageBox.Show("Nom, mot de passe et role obligatoires.")
                    Return
                End If
                Dim rolesCoches As List(Of String) = ObtenirRolesCochesAvecPrincipal()
                If rolesCoches.Any(Function(r) String.Equals(r, "SUPERADMIN", StringComparison.OrdinalIgnoreCase)) AndAlso
                   Not String.Equals(SessionUtilisateur.Role, "SUPERADMIN", StringComparison.OrdinalIgnoreCase) Then
                    MessageBox.Show("Seul un SUPERADMIN peut attribuer le rôle SUPERADMIN.")
                    Return
                End If

                Dim service As UtilisateurService = ObtenirService()
                service.CreerUtilisateur(txtNom.Text.Trim(), txtMotDePasse.Text.Trim(), cmbRole.SelectedItem.ToString())
                Dim rolesAutorises As List(Of String) = rolesCoches
                If rolesAutorises.Count > 1 Then
                    Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                    Dim repo As New UtilisateurRepository(New DAL(cs))
                    Dim utilisateurCree As Utilisateur = repo.ObtenirParNom(txtNom.Text.Trim())
                    If utilisateurCree IsNot Nothing Then
                        service.MettreAJourUtilisateurRoles(utilisateurCree.UtilisateurId, txtNom.Text.Trim(), rolesAutorises, cmbRole.SelectedItem.ToString(), True, Nothing)
                    End If
                End If
                Charger(sender, e)
            Catch ex As Exception
                MessageBox.Show("Erreur ajout utilisateur: " & ex.Message)
            End Try
        End Sub

        Private Sub Modifier(sender As Object, e As EventArgs)
            Try
                If _utilisateurSelectionneId <= 0 AndAlso grid.CurrentRow IsNot Nothing Then
                    _utilisateurSelectionneId = Convert.ToInt32(grid.CurrentRow.Cells("UtilisateurId").Value)
                End If

                If _utilisateurSelectionneId <= 0 Then
                    MessageBox.Show("Selectionnez un utilisateur.")
                    Return
                End If

                If txtNom.Text.Trim() = "" OrElse cmbRole.SelectedItem Is Nothing Then
                    MessageBox.Show("Nom utilisateur et role obligatoires.")
                    Return
                End If

                Dim rolesAutorises As List(Of String) = ObtenirRolesCochesAvecPrincipal()
                If rolesAutorises.Count = 0 Then
                    MessageBox.Show("Sélectionnez au moins un rôle autorisé.")
                    Return
                End If
                If rolesAutorises.Any(Function(r) String.Equals(r, "SUPERADMIN", StringComparison.OrdinalIgnoreCase)) AndAlso
                   Not String.Equals(SessionUtilisateur.Role, "SUPERADMIN", StringComparison.OrdinalIgnoreCase) Then
                    MessageBox.Show("Seul un SUPERADMIN peut attribuer le rôle SUPERADMIN.")
                    Return
                End If

                Dim nouveauMotDePasse As String = txtMotDePasse.Text.Trim()
                Dim service As UtilisateurService = ObtenirService()
                service.MettreAJourUtilisateurRoles(_utilisateurSelectionneId, txtNom.Text.Trim(), rolesAutorises, cmbRole.SelectedItem.ToString(), chkActif.Checked, If(nouveauMotDePasse = "", Nothing, nouveauMotDePasse))
                MessageBox.Show("Utilisateur mis a jour.")
                Charger(sender, e)
            Catch ex As Exception
                MessageBox.Show("Erreur modification utilisateur: " & ex.Message)
            End Try
        End Sub

        Private Sub ResetMdp(sender As Object, e As EventArgs)
            Try
                If grid.CurrentRow Is Nothing Then
                    MessageBox.Show("Selectionnez un utilisateur.")
                    Return
                End If
                If txtMotDePasse.Text.Trim() = "" Then
                    MessageBox.Show("Entrez un nouveau mot de passe.")
                    Return
                End If

                Dim id As Integer = Convert.ToInt32(grid.CurrentRow.Cells("UtilisateurId").Value)
                Dim service As UtilisateurService = ObtenirService()
                service.ReinitialiserMotDePasse(id, txtMotDePasse.Text.Trim())
                MessageBox.Show("Mot de passe mis a jour.")
            Catch ex As Exception
                MessageBox.Show("Erreur reset mot de passe: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerSelectionUtilisateur(sender As Object, e As EventArgs)
            Try
                If grid.CurrentRow Is Nothing OrElse grid.CurrentRow.IsNewRow Then
                    Return
                End If

                If grid.CurrentRow.Cells("UtilisateurId") Is Nothing Then
                    Return
                End If

                _utilisateurSelectionneId = Convert.ToInt32(grid.CurrentRow.Cells("UtilisateurId").Value)
                txtNom.Text = Convert.ToString(grid.CurrentRow.Cells("NomUtilisateur").Value)
                Dim valeurActif As Object = grid.CurrentRow.Cells("EstActif").Value
                chkActif.Checked = If(valeurActif Is Nothing OrElse Convert.IsDBNull(valeurActif), False, Convert.ToBoolean(valeurActif))

                Dim valeurRole As Object = grid.CurrentRow.Cells("Role").Value
                Dim role As String = If(valeurRole Is Nothing OrElse Convert.IsDBNull(valeurRole), "", Convert.ToString(valeurRole))
                If role.Contains(",") Then
                    role = role.Split(","c)(0).Trim()
                End If
                If cmbRole.Items.Contains(role) Then
                    cmbRole.SelectedItem = role
                Else
                    cmbRole.SelectedIndex = -1
                End If
                ChargerRolesAutorisesUtilisateur(_utilisateurSelectionneId)
                txtMotDePasse.Text = ""
            Catch
            End Try
        End Sub

        Private Function ObtenirRolesCochesAvecPrincipal() As List(Of String)
            Dim roles As New List(Of String)()
            For Each item As Object In clbRolesAutorises.CheckedItems
                Dim role As String = Convert.ToString(item)
                If role <> String.Empty AndAlso Not roles.Contains(role) Then
                    roles.Add(role)
                End If
            Next
            If cmbRole.SelectedItem IsNot Nothing Then
                Dim principal As String = cmbRole.SelectedItem.ToString()
                If principal <> String.Empty AndAlso Not roles.Contains(principal) Then
                    roles.Add(principal)
                End If
            End If
            Return roles
        End Function

        Private Sub ChargerRolesAutorisesUtilisateur(utilisateurId As Integer)
            Try
                For i As Integer = 0 To clbRolesAutorises.Items.Count - 1
                    clbRolesAutorises.SetItemChecked(i, False)
                Next

                Dim service As UtilisateurService = ObtenirService()
                Dim roles As List(Of RoleSessionInfo) = service.ListerRolesActifs(utilisateurId)
                For Each role As RoleSessionInfo In roles
                    For i As Integer = 0 To clbRolesAutorises.Items.Count - 1
                        If String.Equals(Convert.ToString(clbRolesAutorises.Items(i)), role.NomRole, StringComparison.OrdinalIgnoreCase) Then
                            clbRolesAutorises.SetItemChecked(i, True)
                        End If
                    Next
                Next
            Catch
            End Try
        End Sub

        Private Sub Grid_CellClick(sender As Object, e As DataGridViewCellEventArgs)
            ChargerSelectionUtilisateur(sender, EventArgs.Empty)
        End Sub

        Private Sub RafraichirRolesDepuisEvenement(sender As Object, e As EventArgs)
            If IsDisposed Then
                Return
            End If

            If InvokeRequired Then
                BeginInvoke(New MethodInvoker(Sub() RafraichirRolesDepuisEvenement(Nothing, EventArgs.Empty)))
                Return
            End If

            Dim t As Task = ChargerRolesDisponiblesAsync()
        End Sub

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            RemoveHandler AppEvents.RolePermissionsChanged, AddressOf RafraichirRolesDepuisEvenement
            MyBase.OnFormClosed(e)
        End Sub
    End Class
End Namespace
