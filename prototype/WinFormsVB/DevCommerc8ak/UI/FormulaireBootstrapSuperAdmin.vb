Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Text.RegularExpressions
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireBootstrapSuperAdmin
        Inherits Form

        Private ReadOnly _service As UtilisateurService
        Private ReadOnly txtNom As TextBox
        Private ReadOnly txtMotDePasse As TextBox
        Private ReadOnly txtConfirmation As TextBox
        Private ReadOnly lblErreur As Label

        Public Sub New(service As UtilisateurService)
            If service Is Nothing Then Throw New ArgumentNullException("service")
            _service = service

            Text = "Initialisation SUPERADMIN"
            StartPosition = FormStartPosition.CenterScreen
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            ClientSize = New Size(520, 360)
            BackColor = Color.White

            Dim header As New Label() With {
                .Text = "Création du compte SUPERADMIN",
                .Dock = DockStyle.Top,
                .Height = 56,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Font = New Font("Segoe UI", 15.0F, FontStyle.Bold),
                .ForeColor = Color.FromArgb(31, 41, 55)
            }

            Dim info As New Label() With {
                .Text = "Aucun utilisateur actif n'existe. Créez le compte système initial avec un mot de passe fort.",
                .Dock = DockStyle.Top,
                .Height = 42,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Font = New Font("Segoe UI", 9.0F),
                .ForeColor = Color.FromArgb(75, 85, 99)
            }

            Dim table As New TableLayoutPanel() With {
                .Dock = DockStyle.Top,
                .Height = 160,
                .Padding = New Padding(32, 10, 32, 0),
                .ColumnCount = 2,
                .RowCount = 3
            }
            table.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150.0F))
            table.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))
            For i As Integer = 0 To 2
                table.RowStyles.Add(New RowStyle(SizeType.Absolute, 48.0F))
            Next

            txtNom = New TextBox() With {.Dock = DockStyle.Fill, .Font = New Font("Segoe UI", 10.0F)}
            txtMotDePasse = New TextBox() With {.Dock = DockStyle.Fill, .UseSystemPasswordChar = True, .Font = New Font("Segoe UI", 10.0F)}
            txtConfirmation = New TextBox() With {.Dock = DockStyle.Fill, .UseSystemPasswordChar = True, .Font = New Font("Segoe UI", 10.0F)}

            AjouterLigne(table, 0, "Identifiant", txtNom)
            AjouterLigne(table, 1, "Mot de passe", txtMotDePasse)
            AjouterLigne(table, 2, "Confirmation", txtConfirmation)

            lblErreur = New Label() With {
                .Dock = DockStyle.Top,
                .Height = 46,
                .Padding = New Padding(32, 4, 32, 0),
                .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
                .ForeColor = Color.Firebrick,
                .TextAlign = ContentAlignment.MiddleLeft
            }

            Dim actions As New FlowLayoutPanel() With {
                .Dock = DockStyle.Bottom,
                .Height = 64,
                .FlowDirection = FlowDirection.RightToLeft,
                .Padding = New Padding(18, 12, 18, 12)
            }
            Dim btnValider As New Button() With {.Text = "Créer SUPERADMIN", .Width = 150, .Height = 36, .BackColor = Color.FromArgb(16, 185, 129), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            Dim btnAnnuler As New Button() With {.Text = "Annuler", .Width = 100, .Height = 36, .DialogResult = DialogResult.Cancel}
            btnValider.FlatAppearance.BorderSize = 0
            AddHandler btnValider.Click, AddressOf ValiderCreation
            actions.Controls.Add(btnValider)
            actions.Controls.Add(btnAnnuler)

            Controls.Add(actions)
            Controls.Add(lblErreur)
            Controls.Add(table)
            Controls.Add(info)
            Controls.Add(header)

            AcceptButton = btnValider
            CancelButton = btnAnnuler
        End Sub

        Private Shared Sub AjouterLigne(table As TableLayoutPanel, rowIndex As Integer, libelle As String, controle As Control)
            Dim lbl As New Label() With {
                .Text = libelle,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft,
                .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold),
                .ForeColor = Color.FromArgb(55, 65, 81)
            }
            table.Controls.Add(lbl, 0, rowIndex)
            table.Controls.Add(controle, 1, rowIndex)
        End Sub

        Private Sub ValiderCreation(sender As Object, e As EventArgs)
            Dim nom As String = txtNom.Text.Trim()
            Dim motDePasse As String = txtMotDePasse.Text
            Dim confirmation As String = txtConfirmation.Text

            Dim erreur As String = ValiderSaisie(nom, motDePasse, confirmation)
            If erreur <> String.Empty Then
                lblErreur.Text = erreur
                Return
            End If

            Try
                _service.CreerUtilisateur(nom, motDePasse, "SUPERADMIN")
                AuditActionService.Enregistrer("Sécurité", "Bootstrap SUPERADMIN", "Création du premier compte SUPERADMIN.")
                DialogResult = DialogResult.OK
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireBootstrapSuperAdmin", "ValiderCreation", "Création du SUPERADMIN initial impossible.", ex)
                lblErreur.Text = "Impossible de créer le compte SUPERADMIN. Vérifiez le journal technique."
            End Try
        End Sub

        Private Shared Function ValiderSaisie(nom As String, motDePasse As String, confirmation As String) As String
            If nom.Length < 3 Then Return "L'identifiant doit contenir au moins 3 caractères."
            If Regex.IsMatch(nom, "\s") Then Return "L'identifiant ne doit pas contenir d'espace."
            If motDePasse.Length < 10 Then Return "Le mot de passe doit contenir au moins 10 caractères."
            If Not Regex.IsMatch(motDePasse, "[A-Z]") Then Return "Le mot de passe doit contenir au moins une majuscule."
            If Not Regex.IsMatch(motDePasse, "[a-z]") Then Return "Le mot de passe doit contenir au moins une minuscule."
            If Not Regex.IsMatch(motDePasse, "\d") Then Return "Le mot de passe doit contenir au moins un chiffre."
            If Not Regex.IsMatch(motDePasse, "[^A-Za-z0-9]") Then Return "Le mot de passe doit contenir au moins un caractère spécial."
            If motDePasse <> confirmation Then Return "La confirmation ne correspond pas."
            Return String.Empty
        End Function
    End Class
End Namespace
