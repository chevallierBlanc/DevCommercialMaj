Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public NotInheritable Class DialogueConfigurationModes
        Private Sub New()
        End Sub
        Public Shared Sub Afficher(owner As IWin32Window, service As ConfigurationModesService)
            Dim initial As ConfigurationModes = service.Charger()
            Using form As New Form With {.Text = "Modes de travail", .Size = New Size(580, 420), .MinimumSize = New Size(540, 420), .StartPosition = FormStartPosition.CenterParent, .BackColor = Color.FromArgb(245, 247, 250), .Font = New Font("Segoe UI", 10)}
                Dim root As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .Padding = New Padding(20), .FlowDirection = FlowDirection.TopDown, .WrapContents = False, .AutoScroll = True}
                root.Controls.Add(New Label With {.Text = "MODES DE TRAVAIL", .AutoSize = True, .Font = New Font("Segoe UI", 16, FontStyle.Bold), .ForeColor = Color.FromArgb(41, 128, 185)})
                Dim active As New CheckBox With {.Text = "Activer la gestion des modes", .Checked = initial.Active, .AutoSize = True}
                Dim fact As New CheckBox With {.Text = "Autoriser FACTURATION", .Checked = initial.Facturation, .AutoSize = True}
                Dim caisse As New CheckBox With {.Text = "Autoriser CAISSE", .Checked = initial.Caisse, .AutoSize = True}
                Dim combine As New CheckBox With {.Text = "Autoriser FACTURATION + CAISSE", .Checked = initial.Combine, .AutoSize = True}
                root.Controls.AddRange(New Control() {active, fact, caisse, combine})
                root.Controls.Add(New Label With {.Text = "Attribuez séparément les permissions de mode et d'action dans Rôles & privilèges. Changement de mode : déconnexion puis reconnexion.", .Width = 490, .Height = 60})
                root.Controls.Add(New Label With {.Text = "Motif obligatoire", .AutoSize = True})
                Dim motif As New TextBox With {.Width = 490, .Height = 60, .Multiline = True, .MaxLength = 1000}
                root.Controls.Add(motif)
                Dim save As New Button With {.Text = "ENREGISTRER", .Width = 160, .Height = 36, .BackColor = Color.FromArgb(41, 128, 185), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
                AddHandler save.Click, Sub()
                                          Try
                                              service.Enregistrer(New ConfigurationModes With {.Active = active.Checked, .Facturation = fact.Checked, .Caisse = caisse.Checked, .Combine = combine.Checked}, initial, motif.Text)
                                              form.DialogResult = DialogResult.OK
                                          Catch ex As Exception
                                              MessageBox.Show(form, ex.Message, "Configuration refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                          End Try
                                      End Sub
                root.Controls.Add(save)
                form.Controls.Add(root)
                form.ShowDialog(owner)
            End Using
        End Sub
    End Class
End Namespace
