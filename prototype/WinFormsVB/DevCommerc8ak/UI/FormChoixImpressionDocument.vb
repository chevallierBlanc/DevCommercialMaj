Option Strict On
Option Explicit On
Imports System
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public NotInheritable Class FormChoixImpressionDocument
        Inherits Form

        Public Property FormatSelectionne As String = String.Empty

        Public Sub New(Optional titreDocument As String = "Imprimer le proforma", Optional libelleA4 As String = "Proforma A4")
            Text = titreDocument
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MinimizeBox = False
            MaximizeBox = False
            ClientSize = New Size(360, 210)
            BackColor = Color.White

            Dim titre As New Label() With {.Text = titreDocument.ToUpperInvariant(), .Dock = DockStyle.Top, .Height = 54, .TextAlign = ContentAlignment.MiddleCenter, .Font = New Font("Segoe UI", 12, FontStyle.Bold), .ForeColor = Color.FromArgb(52, 73, 94)}
            Dim btnTicket As New Button() With {.Text = "Ticket thermique 80 mm", .Left = 55, .Top = 70, .Width = 250, .Height = 34, .BackColor = Color.FromArgb(52, 73, 94), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            Dim btnA4 As New Button() With {.Text = libelleA4, .Left = 55, .Top = 112, .Width = 250, .Height = 34, .BackColor = Color.FromArgb(41, 128, 185), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            Dim btnAnnuler As New Button() With {.Text = "Annuler", .Left = 55, .Top = 154, .Width = 250, .Height = 30, .DialogResult = DialogResult.Cancel}
            AddHandler btnTicket.Click, Sub()
                                            FormatSelectionne = "THERMIQUE"
                                            DialogResult = DialogResult.OK
                                        End Sub
            AddHandler btnA4.Click, Sub()
                                        FormatSelectionne = "A4"
                                        DialogResult = DialogResult.OK
                                    End Sub
            Controls.AddRange(New Control() {titre, btnTicket, btnA4, btnAnnuler})
            CancelButton = btnAnnuler
        End Sub
    End Class
End Namespace
