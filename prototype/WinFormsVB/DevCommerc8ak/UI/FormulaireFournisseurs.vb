Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireFournisseurs
        Inherits Form

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

        Public Sub New()
            Me.Text = "Fournisseurs"
            Me.Width = 1000
            Me.Height = 650

            Dim panelForm As New Panel() With {.Dock = DockStyle.Top, .Height = 120}
            Dim panelBoutons As New Panel() With {.Dock = DockStyle.Top, .Height = 45}

            txtNom = New TextBox() With {.Left = 20, .Top = 25, .Width = 200}
            txtTelephone = New TextBox() With {.Left = 240, .Top = 25, .Width = 160}
            txtEmail = New TextBox() With {.Left = 420, .Top = 25, .Width = 220}
            txtAdresse = New TextBox() With {.Left = 20, .Top = 75, .Width = 380}
            chkActif = New CheckBox() With {.Left = 420, .Top = 78, .Text = "Actif"}

            panelForm.Controls.Add(New Label() With {.Text = "Nom", .Left = 20, .Top = 5, .AutoSize = True})
            panelForm.Controls.Add(New Label() With {.Text = "Telephone", .Left = 240, .Top = 5, .AutoSize = True})
            panelForm.Controls.Add(New Label() With {.Text = "Email", .Left = 420, .Top = 5, .AutoSize = True})
            panelForm.Controls.Add(New Label() With {.Text = "Adresse", .Left = 20, .Top = 55, .AutoSize = True})

            panelForm.Controls.Add(txtNom)
            panelForm.Controls.Add(txtTelephone)
            panelForm.Controls.Add(txtEmail)
            panelForm.Controls.Add(txtAdresse)
            panelForm.Controls.Add(chkActif)

            btnAjouter = New Button() With {.Text = "Ajouter", .Left = 20, .Top = 8, .Width = 100}
            btnModifier = New Button() With {.Text = "Modifier", .Left = 130, .Top = 8, .Width = 100}
            btnSupprimer = New Button() With {.Text = "Supprimer", .Left = 240, .Top = 8, .Width = 100}
            btnRafraichir = New Button() With {.Text = "Rafraichir", .Left = 350, .Top = 8, .Width = 100}

            AddHandler btnAjouter.Click, AddressOf AjouterFournisseur
            AddHandler btnModifier.Click, AddressOf ModifierFournisseur
            AddHandler btnSupprimer.Click, AddressOf SupprimerFournisseur
            AddHandler btnRafraichir.Click, AddressOf ChargerDonnees

            panelBoutons.Controls.Add(btnAjouter)
            panelBoutons.Controls.Add(btnModifier)
            panelBoutons.Controls.Add(btnSupprimer)
            panelBoutons.Controls.Add(btnRafraichir)

            grid = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .AutoGenerateColumns = True,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .ReadOnly = True
            }
            AddHandler grid.SelectionChanged, AddressOf ChargerSelection

            Me.Controls.Add(grid)
            Me.Controls.Add(panelBoutons)
            Me.Controls.Add(panelForm)

            ThemeHelper.AppliquerTheme(Me)

            timer = New Timer() With {.Interval = 600000}
            AddHandler timer.Tick, AddressOf ChargerDonnees
            timer.Start()
        End Sub

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
