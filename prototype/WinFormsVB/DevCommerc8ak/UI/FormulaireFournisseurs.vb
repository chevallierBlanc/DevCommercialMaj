Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireFournisseurs
        Inherits Form

        ' --- Constantes de Design ---
        'Private ReadOnly ColorPrimary As Color = Color.FromArgb(63, 81, 181) ' Indigo
        'Private ReadOnly ColorSecondary As Color = Color.FromArgb(48, 63, 159)
        Private ReadOnly ColorBackground As Color = Color.FromArgb(245, 247, 250)
        Private ReadOnly ColorCard As Color = Color.White
        Private ReadOnly ColorText As Color = Color.FromArgb(33, 33, 33)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(117, 117, 117)
        Private ReadOnly ColorBorder As Color = Color.FromArgb(230, 230, 230)
        'Private ReadOnly FontTitle As New Font("Segoe UI Semibold", 18.0F)
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
        Private ReadOnly btnAjouter As Button
        Private ReadOnly btnModifier As Button
        Private ReadOnly btnSupprimer As Button
        Private ReadOnly btnRafraichir As Button

        Private ReadOnly txtNom As TextBox
        Private ReadOnly txtTelephone As TextBox
        Private ReadOnly txtEmail As TextBox
        Private ReadOnly txtAdresse As TextBox
        Private ReadOnly chkActif As CheckBox
        Private ReadOnly timer As Timer

        ' --- Nouveaux composants de structure (Layouts propres) ---
        Private ReadOnly panelHero As Panel
        Private ReadOnly lblHeroTitre As Label
        Private ReadOnly lblHeroSousTitre As Label
        Private ReadOnly mainTableLayout As TableLayoutPanel
        Private ReadOnly cardForm As Panel
        Private ReadOnly flowButtons As FlowLayoutPanel

        Public Sub New()
            ' Configuration de base du formulaire
            Me.Text = "Gestion des Fournisseurs"
            Me.Width = 1100
            Me.Height = 750
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = ColorBackground
            Me.DoubleBuffered = True

            ' --- Header / Hero Section ---
            panelHero = New Panel() With {.Dock = DockStyle.Top, .Height = 90, .BackColor = ColorPrimary}
            lblHeroTitre = New Label() With {.Text = "Répertoire Fournisseurs", .Left = 25, .Top = 18, .AutoSize = True, .Font = FontTitle, .ForeColor = Color.White}
            lblHeroSousTitre = New Label() With {.Text = "Gérez vos partenaires commerciaux et suivez vos sources d'approvisionnement.", .Left = 27, .Top = 54, .AutoSize = True, .Font = FontSubTitle, .ForeColor = Color.FromArgb(210, 210, 255)}
            panelHero.Controls.Add(lblHeroTitre)
            panelHero.Controls.Add(lblHeroSousTitre)

            ' --- Layout Principal ---
            mainTableLayout = New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3,
                .Padding = New Padding(20)
            }
            mainTableLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 180)) ' Carte Formulaire
            mainTableLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 60))  ' Boutons
            mainTableLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))  ' Grille

            ' --- Carte de Formulaire ---
            cardForm = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorCard,
                .Padding = New Padding(20)
            }

            Dim formTable As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 3,
                .RowCount = 4
            }
            formTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33))
            formTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33))
            formTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 34))

            ' Initialisation des contrôles (Noms conservés)
            txtNom = CreateStyledTextBox()
            txtTelephone = CreateStyledTextBox()
            txtEmail = CreateStyledTextBox()
            txtAdresse = CreateStyledTextBox()
            chkActif = New CheckBox() With {.Text = "Fournisseur Actif", .Font = FontControl, .ForeColor = ColorText, .AutoSize = True, .Margin = New Padding(0, 10, 0, 0)}

            ' Ajout au layout de formulaire
            formTable.Controls.Add(CreateLabel("Nom du Fournisseur"), 0, 0)
            formTable.Controls.Add(txtNom, 0, 1)
            formTable.Controls.Add(CreateLabel("Téléphone"), 1, 0)
            formTable.Controls.Add(txtTelephone, 1, 1)
            formTable.Controls.Add(CreateLabel("Email"), 2, 0)
            formTable.Controls.Add(txtEmail, 2, 1)

            formTable.Controls.Add(CreateLabel("Adresse complète"), 0, 2)
            formTable.Controls.Add(txtAdresse, 0, 3)
            formTable.SetColumnSpan(txtAdresse, 2)
            formTable.Controls.Add(chkActif, 2, 3)

            cardForm.Controls.Add(formTable)

            ' --- Barre de Boutons (FlowLayout) ---
            flowButtons = New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.LeftToRight,
                .Padding = New Padding(0, 10, 0, 0)
            }

            btnAjouter = CreateStyledButton("Ajouter", ColorPrimary)
            btnModifier = CreateStyledButton("Modifier", ColorSecondary)
            btnSupprimer = CreateStyledButton("Supprimer", Color.Crimson)
            btnRafraichir = CreateStyledButton("Rafraîchir", Color.Gray)

            flowButtons.Controls.AddRange(New Control() {btnAjouter, btnModifier, btnSupprimer, btnRafraichir})

            ' --- Grille ---
            grid = CreateStyledGrid()
            grid.Dock = DockStyle.Fill

            ' Assemblage final
            mainTableLayout.Controls.Add(cardForm, 0, 0)
            mainTableLayout.Controls.Add(flowButtons, 0, 1)
            mainTableLayout.Controls.Add(grid, 0, 2)

            Me.Controls.Add(mainTableLayout)
            Me.Controls.Add(panelHero)

            ' --- Liaison des événements (Logique conservée) ---
            AddHandler btnAjouter.Click, AddressOf AjouterFournisseur
            AddHandler btnModifier.Click, AddressOf ModifierFournisseur
            AddHandler btnSupprimer.Click, AddressOf SupprimerFournisseur
            AddHandler btnRafraichir.Click, AddressOf ChargerDonnees
            AddHandler grid.SelectionChanged, AddressOf ChargerSelection

            ' --- Initialisation ---
            'ThemeHelper.AppliquerTheme(Me)
            timer = New Timer() With {.Interval = 600000}
            AddHandler timer.Tick, AddressOf ChargerDonnees
            timer.Start()

            ' Chargement initial
            AddHandler Me.Load, AddressOf ChargerDonnees
        End Sub

        ' --- Helpers de Design ---

        Private Function CreateLabel(text As String) As Label
            Return New Label() With {
                .Text = text,
                .AutoSize = True,
                .Font = FontLabel,
                .ForeColor = ColorTextSecondary,
                .Margin = New Padding(0, 5, 0, 2)
            }
        End Function

        Private Function CreateStyledTextBox() As TextBox
            Return New TextBox() With {
                .Dock = DockStyle.Top,
                .Font = FontControl,
                .BorderStyle = BorderStyle.FixedSingle,
                .Margin = New Padding(0, 0, 20, 10)
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

        Private Function CreateStyledGrid() As DataGridView
            Dim dgv As New DataGridView() With {
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
            dgv.ColumnHeadersHeight = 45
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 234, 246)
            dgv.DefaultCellStyle.SelectionForeColor = ColorPrimary
            dgv.DefaultCellStyle.Font = FontControl
            dgv.RowTemplate.Height = 35
            Return dgv
        End Function

        ' --- LOGIQUE MÉTIER (STRICTEMENT IDENTIQUE À L'ORIGINAL) ---

        Private Function ObtenirService() As FournisseurService
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New FournisseurRepository(dal)
            Return New FournisseurService(repo)
        End Function

        Private Sub ChargerDonnees(sender As Object, e As EventArgs)
            Try
                Dim service As FournisseurService = ObtenirService()
                grid.DataSource = service.Lister()
            Catch ex As Exception
                MessageBox.Show("Erreur chargement fournisseurs: " & ex.Message)
            End Try
        End Sub

        Private Sub AjouterFournisseur(sender As Object, e As EventArgs)
            Try
                If txtNom.Text.Trim() = "" Then
                    MessageBox.Show("Nom fournisseur obligatoire.")
                    Return
                End If
                Dim service As FournisseurService = ObtenirService()
                Dim f As New Fournisseur With {
                    .NomFournisseur = txtNom.Text.Trim(),
                    .Telephone = txtTelephone.Text.Trim(),
                    .Email = txtEmail.Text.Trim(),
                    .Adresse = txtAdresse.Text.Trim(),
                    .EstActif = chkActif.Checked
                }
                service.Ajouter(f)
                ChargerDonnees(sender, e)
            Catch ex As Exception
                MessageBox.Show("Erreur ajout fournisseur: " & ex.Message)
            End Try
        End Sub

        Private Sub ModifierFournisseur(sender As Object, e As EventArgs)
            Try
                If grid.CurrentRow Is Nothing Then
                    MessageBox.Show("Selectionnez un fournisseur.")
                    Return
                End If
                If txtNom.Text.Trim() = "" Then
                    MessageBox.Show("Nom fournisseur obligatoire.")
                    Return
                End If

                Dim id As Integer = Convert.ToInt32(grid.CurrentRow.Cells("FournisseurId").Value)
                Dim service As FournisseurService = ObtenirService()
                Dim f As New Fournisseur With {
                    .FournisseurId = id,
                    .NomFournisseur = txtNom.Text.Trim(),
                    .Telephone = txtTelephone.Text.Trim(),
                    .Email = txtEmail.Text.Trim(),
                    .Adresse = txtAdresse.Text.Trim(),
                    .EstActif = chkActif.Checked
                }
                service.MettreAJour(f)
                ChargerDonnees(sender, e)
            Catch ex As Exception
                MessageBox.Show("Erreur modification fournisseur: " & ex.Message)
            End Try
        End Sub

        Private Sub SupprimerFournisseur(sender As Object, e As EventArgs)
            Try
                If grid.CurrentRow Is Nothing Then
                    MessageBox.Show("Selectionnez un fournisseur.")
                    Return
                End If

                Dim id As Integer = Convert.ToInt32(grid.CurrentRow.Cells("FournisseurId").Value)
                Dim service As FournisseurService = ObtenirService()
                service.Supprimer(id)
                ChargerDonnees(sender, e)
            Catch ex As Exception
                MessageBox.Show("Erreur suppression fournisseur: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerSelection(sender As Object, e As EventArgs)
            If grid.CurrentRow Is Nothing Then Return

            txtNom.Text = Convert.ToString(grid.CurrentRow.Cells("NomFournisseur").Value)
            txtTelephone.Text = Convert.ToString(grid.CurrentRow.Cells("Telephone").Value)
            txtEmail.Text = Convert.ToString(grid.CurrentRow.Cells("Email").Value)
            txtAdresse.Text = Convert.ToString(grid.CurrentRow.Cells("Adresse").Value)
            chkActif.Checked = Convert.ToBoolean(grid.CurrentRow.Cells("EstActif").Value)
        End Sub
    End Class
End Namespace
