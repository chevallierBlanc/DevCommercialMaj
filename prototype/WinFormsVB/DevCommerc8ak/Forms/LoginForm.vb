Imports System.Windows.Forms
Imports System.Drawing
Imports System.Configuration

Namespace DevCommerc8ak
    Public Class LoginForm
        Inherits Form

        Private ReadOnly txtUser As TextBox
        Private ReadOnly txtPass As TextBox
        Private ReadOnly btnLogin As Button
        Private ReadOnly lblStatus As Label

        Public Sub New()
            Me.Text = "Connexion"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Size = New Size(420, 260)
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False

            Dim lblUser As New Label() With {.Text = "Utilisateur", .Location = New Point(30, 30), .AutoSize = True}
            Dim lblPass As New Label() With {.Text = "Mot de passe", .Location = New Point(30, 80), .AutoSize = True}

            txtUser = New TextBox() With {.Location = New Point(140, 25), .Width = 220}
            txtPass = New TextBox() With {.Location = New Point(140, 75), .Width = 220, .UseSystemPasswordChar = True}

            btnLogin = New Button() With {.Text = "Se connecter", .Location = New Point(140, 125), .Width = 120}
            AddHandler btnLogin.Click, AddressOf OnLogin

            lblStatus = New Label() With {.Text = "Etat serveur: CONNECTE", .Location = New Point(30, 170), .AutoSize = True}

            Me.Controls.Add(lblUser)
            Me.Controls.Add(lblPass)
            Me.Controls.Add(txtUser)
            Me.Controls.Add(txtPass)
            Me.Controls.Add(btnLogin)
            Me.Controls.Add(lblStatus)

            ChargerModeSombre()
            ThemeHelper.AppliquerTheme(Me)
            IconsHelper.AppliquerIconeFormulaire(Me)
            InitialiserComptesParDefaut()
        End Sub

        Private Sub OnLogin(sender As Object, e As EventArgs)
            Dim ok As Boolean = Authentifier(txtUser.Text.Trim(), txtPass.Text)
            If Not ok Then
                MessageBox.Show("Identifiants invalides.")
                Return
            End If

            Dim main As New MainForm()
            main.Show()
            Me.Hide()
        End Sub

        Private Function Authentifier(nomUtilisateur As String, motDePasse As String) As Boolean
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim utilisateurRepo As New UtilisateurRepository(dal)
            Dim roleRepo As New RoleRepository(dal)
            Dim sessionRepo As New SessionRepository(dal)
            Dim service As New UtilisateurService(utilisateurRepo, roleRepo, sessionRepo)
            Return service.VerifierConnexion(nomUtilisateur, motDePasse)
        End Function

        Private Sub InitialiserComptesParDefaut()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim utilisateurRepo As New UtilisateurRepository(dal)
                Dim roleRepo As New RoleRepository(dal)
                Dim sessionRepo As New SessionRepository(dal)
                Dim service As New UtilisateurService(utilisateurRepo, roleRepo, sessionRepo)
                service.EnsurerComptesParDefaut("1234", "1234", "1234")
            Catch
                ' Ignore l'erreur si la base n'est pas accessible.
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
    End Class
End Namespace
