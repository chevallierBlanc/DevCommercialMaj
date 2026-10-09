Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public NotInheritable Class DialogueMotifOperation
        Private Sub New()
        End Sub

        Public Shared Function Demander(owner As IWin32Window, titre As String) As String
            Using form As New Form() With {.Text = titre, .Size = New Size(490, 260), .StartPosition = FormStartPosition.CenterParent,
                .FormBorderStyle = FormBorderStyle.FixedDialog, .MaximizeBox = False, .MinimizeBox = False,
                .BackColor = Color.FromArgb(240, 242, 245), .Font = New Font("Segoe UI", 10)}
                Dim label As New Label() With {.Text = "Motif obligatoire (1 à 1000 caractères)", .Dock = DockStyle.Top, .Height = 38, .Padding = New Padding(12, 8, 0, 0)}
                Dim text As New TextBox() With {.Multiline = True, .MaxLength = 1000, .Dock = DockStyle.Fill, .ScrollBars = ScrollBars.Vertical}
                Dim buttons As New FlowLayoutPanel() With {.Dock = DockStyle.Bottom, .Height = 50, .FlowDirection = FlowDirection.RightToLeft, .Padding = New Padding(8)}
                Dim ok As New Button() With {.Text = "CONFIRMER", .Width = 130, .Height = 32, .BackColor = Color.FromArgb(0, 102, 204), .ForeColor = Color.White, .Enabled = False, .DialogResult = DialogResult.OK}
                Dim cancel As New Button() With {.Text = "ANNULER", .Width = 110, .Height = 32, .DialogResult = DialogResult.Cancel}
                AddHandler text.TextChanged, Sub() ok.Enabled = Not String.IsNullOrWhiteSpace(text.Text)
                buttons.Controls.Add(ok)
                buttons.Controls.Add(cancel)
                form.Controls.Add(text)
                form.Controls.Add(label)
                form.Controls.Add(buttons)
                form.AcceptButton = ok
                form.CancelButton = cancel
                If form.ShowDialog(owner) <> DialogResult.OK Then Return Nothing
                Return AuditMetierService.ValiderMotif(text.Text)
            End Using
        End Function
    End Class
End Namespace
