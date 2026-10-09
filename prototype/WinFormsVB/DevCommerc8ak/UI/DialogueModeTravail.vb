Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class DialogueModeTravail
        Inherits Form
        Public Property ModeSelectionne As String

        Public Sub New(modes As IList(Of String))
            Text = "Choisir votre mode de travail"
            StartPosition = FormStartPosition.CenterParent
            Size = New Size(580, 340)
            MinimumSize = Size
            BackColor = Color.FromArgb(245, 247, 250)
            Font = New Font("Segoe UI", 10)
            Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .Padding = New Padding(20), .ColumnCount = 1, .RowCount = 3}
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 50))
            root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 48))
            root.Controls.Add(New Label With {.Text = "Un rôle actif • Un mode de travail", .Dock = DockStyle.Fill, .Font = New Font("Segoe UI", 15, FontStyle.Bold), .ForeColor = Color.FromArgb(41, 128, 185)}, 0, 0)
            Dim choix As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.TopDown, .WrapContents = False, .AutoScroll = True}
            For Each mode As String In modes
                Dim description As String = If(mode = "FACTURATION", "Créer et gérer les factures autorisées.", If(mode = "CAISSE", "Encaisser et consulter les paiements.", "Facturer puis encaisser : deux opérations distinctes."))
                Dim radio As New RadioButton With {.Text = mode.Replace("_ET_", " + ") & Environment.NewLine & description, .Tag = mode, .Size = New Size(500, 54), .Margin = New Padding(0, 0, 0, 8)}
                choix.Controls.Add(radio)
            Next
            Dim footer As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft}
            Dim continuer As New Button With {.Text = "CONTINUER", .Width = 130, .Height = 36, .BackColor = Color.FromArgb(41, 128, 185), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            Dim annuler As New Button With {.Text = "ANNULER", .Width = 115, .Height = 36, .DialogResult = DialogResult.Cancel}
            AddHandler continuer.Click, Sub()
                                            For Each c As Control In choix.Controls
                                                Dim radio As RadioButton = TryCast(c, RadioButton)
                                                If radio IsNot Nothing AndAlso radio.Checked Then
                                                    ModeSelectionne = Convert.ToString(radio.Tag)
                                                    DialogResult = DialogResult.OK
                                                    Return
                                                End If
                                            Next
                                            MessageBox.Show("Sélectionnez un mode autorisé.")
                                        End Sub
            footer.Controls.AddRange(New Control() {continuer, annuler})
            root.Controls.Add(choix, 0, 1)
            root.Controls.Add(footer, 0, 2)
            Controls.Add(root)
            CancelButton = annuler
        End Sub
    End Class
End Namespace
