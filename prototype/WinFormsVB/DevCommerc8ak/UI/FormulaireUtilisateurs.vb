Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireUtilisateurs
        Inherits Form

        Private ReadOnly grid As DataGridView
        Private ReadOnly gridConnectes As DataGridView
        Private ReadOnly timer As Timer
        Private ReadOnly txtNom As TextBox
        Private ReadOnly txtMotDePasse As TextBox
        Private ReadOnly cmbRole As ComboBox
        Private ReadOnly chkActif As CheckBox
        Private ReadOnly btnAjouter As Button
        Private ReadOnly btnResetMdp As Button
        Private ReadOnly btnRafraichir As Button

        Public Sub New()
            Me.Text = "Utilisateurs"
            Me.Width = 900
            Me.Height = 600

            Dim panelTop As New Panel() With {.Dock = DockStyle.Top, .Height = 80}
            txtNom = New TextBox() With {.Left = 20, .Top = 30, .Width = 160}
            txtMotDePasse = New TextBox() With {.Left = 200, .Top = 30, .Width = 120}
            cmbRole = New ComboBox() With {.Left = 340, .Top = 30, .Width = 120}
            cmbRole.Items.AddRange(New Object() {"ADMIN", "CAISSIERE", "FACTURIER"})
            chkActif = New CheckBox() With {.Left = 480, .Top = 32, .Text = "Actif"}

            btnAjouter = New Button() With {.Text = "Ajouter", .Left = 580, .Top = 27, .Width = 90}
            btnResetMdp = New Button() With {.Text = "Reset MDP", .Left = 680, .Top = 27, .Width = 90}
            btnRafraichir = New Button() With {.Text = "Rafraichir", .Left = 780, .Top = 27, .Width = 90}

            AddHandler btnAjouter.Click, AddressOf Ajouter
            AddHandler btnResetMdp.Click, AddressOf ResetMdp
            AddHandler btnRafraichir.Click, AddressOf Charger

            panelTop.Controls.Add(New Label() With {.Text = "Nom", .Left = 20, .Top = 10, .AutoSize = True})
            panelTop.Controls.Add(New Label() With {.Text = "Mot de passe", .Left = 200, .Top = 10, .AutoSize = True})
            panelTop.Controls.Add(New Label() With {.Text = "Role", .Left = 340, .Top = 10, .AutoSize = True})

            panelTop.Controls.Add(txtNom)
            panelTop.Controls.Add(txtMotDePasse)
            panelTop.Controls.Add(cmbRole)
            panelTop.Controls.Add(chkActif)
            panelTop.Controls.Add(btnAjouter)
            panelTop.Controls.Add(btnResetMdp)
            panelTop.Controls.Add(btnRafraichir)

            grid = New DataGridView() With {.Dock = DockStyle.Top, .Height = 300, .AutoGenerateColumns = True, .ReadOnly = True}
            gridConnectes = New DataGridView() With {.Dock = DockStyle.Fill, .AutoGenerateColumns = True, .ReadOnly = True}

            Me.Controls.Add(gridConnectes)
            Me.Controls.Add(grid)
            Me.Controls.Add(panelTop)

            ThemeHelper.AppliquerTheme(Me)

            timer = New Timer() With {.Interval = 5000}
            AddHandler timer.Tick, AddressOf ChargerConnectes
            timer.Start()
        End Sub

        Private Function ObtenirService() As UtilisateurService
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim utilisateurRepo As New UtilisateurRepository(dal)
            Dim roleRepo As New RoleRepository(dal)
            Dim sessionRepo As New SessionRepository(dal)
            Return New UtilisateurService(utilisateurRepo, roleRepo, sessionRepo)
        End Function

        Private Sub Charger(sender As Object, e As EventArgs)
            Try
                Dim service As UtilisateurService = ObtenirService()
                grid.DataSource = service.Lister()
            Catch ex As Exception
                MessageBox.Show("Erreur chargement utilisateurs: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerConnectes(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim repo As New SessionRepository(dal)
                gridConnectes.DataSource = repo.ListerConnectes()
            Catch
            End Try
        End Sub

        Private Sub Ajouter(sender As Object, e As EventArgs)
            Try
                If txtNom.Text.Trim() = "" OrElse txtMotDePasse.Text.Trim() = "" OrElse cmbRole.SelectedItem Is Nothing Then
                    MessageBox.Show("Nom, mot de passe et role obligatoires.")
                    Return
                End If

                Dim service As UtilisateurService = ObtenirService()
                service.CreerUtilisateur(txtNom.Text.Trim(), txtMotDePasse.Text.Trim(), cmbRole.SelectedItem.ToString())
                Charger(sender, e)
            Catch ex As Exception
                MessageBox.Show("Erreur ajout utilisateur: " & ex.Message)
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
    End Class
End Namespace
