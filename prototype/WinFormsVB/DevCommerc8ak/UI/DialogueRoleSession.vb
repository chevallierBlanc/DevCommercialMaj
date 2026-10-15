Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class DialogueRoleSession
        Inherits Form
        Public Property RoleSelectionne As RoleSessionInfo
        Public Sub New(roles As IList(Of RoleSessionInfo), diagnostics As IDictionary(Of Integer, String), utilisateur As String)
            Text = "Choix du rôle" : Font = New Font("Segoe UI", 10)
            AutoScaleMode = AutoScaleMode.Dpi : AutoScaleDimensions = New SizeF(96, 96)
            StartPosition = FormStartPosition.CenterParent : FormBorderStyle = FormBorderStyle.FixedDialog
            MinimizeBox = False : MaximizeBox = False
            ClientSize = New Size(510, 208 + Math.Min(6, Math.Max(1, roles.Count)) * 30)
            BackColor = Color.FromArgb(245, 247, 250)
            Dim disponibles As New List(Of RoleSessionInfo)(roles)
            Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .Padding = New Padding(18), .ColumnCount = 1, .RowCount = 4}
            root.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 58))
            root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 58))
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 44))
            root.Controls.Add(New Label With {.Text = "Bonjour " & utilisateur & Environment.NewLine & "Choisissez votre rôle pour cette session", .Dock = DockStyle.Fill, .ForeColor = Color.FromArgb(41, 128, 185), .Font = New Font(Font, FontStyle.Bold)}, 0, 0)
            Dim liste As New ListBox With {.Dock = DockStyle.Fill, .Font = Font, .ItemHeight = 30, .IntegralHeight = False, .HorizontalScrollbar = True, .BorderStyle = BorderStyle.FixedSingle}
            For Each role As RoleSessionInfo In disponibles
                liste.Items.Add(role.NomRole & If(role.EstRolePrincipal, "  · principal", ""))
            Next
            Dim info As New Label With {.Dock = DockStyle.Fill, .AutoEllipsis = True, .Padding = New Padding(0, 8, 0, 0)}
            Dim details As New LinkLabel With {.Text = "Détails des autorisations", .AutoSize = True}
            Dim boutons As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft, .WrapContents = False}
            Dim continuer As New Button With {.Text = "CONTINUER", .Size = New Size(125, 34), .BackColor = Color.FromArgb(41, 128, 185), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            Dim annuler As New Button With {.Text = "ANNULER", .Size = New Size(110, 34), .DialogResult = DialogResult.Cancel}
            Dim diagnostic As Func(Of String) = Function()
                                                     If liste.SelectedIndex < 0 Then Return "Sélectionnez un rôle attribué à votre compte."
                                                     Dim texte As String = Nothing
                                                     If diagnostics IsNot Nothing AndAlso diagnostics.TryGetValue(disponibles(liste.SelectedIndex).RoleId, texte) Then Return texte
                                                     Return "Les parcours seront vérifiés avant ouverture de la session."
                                                 End Function
            AddHandler liste.SelectedIndexChanged, Sub() info.Text = diagnostic().Split(New String() {Environment.NewLine}, StringSplitOptions.None)(0)
            AddHandler details.LinkClicked, Sub() MessageBox.Show(Me, diagnostic(), "Autorisations du rôle", MessageBoxButtons.OK, MessageBoxIcon.Information)
            AddHandler continuer.Click, Sub()
                                            If liste.SelectedIndex < 0 Then Return
                                            RoleSelectionne = disponibles(liste.SelectedIndex)
                                            DialogResult = DialogResult.OK
                                        End Sub
            AddHandler liste.DoubleClick, Sub() continuer.PerformClick()
            boutons.Controls.AddRange(New Control() {continuer, annuler})
            Dim information As New Panel With {.Dock = DockStyle.Fill}
            details.Dock = DockStyle.Bottom : information.Controls.Add(info) : information.Controls.Add(details)
            root.Controls.Add(liste, 0, 1) : root.Controls.Add(information, 0, 2) : root.Controls.Add(boutons, 0, 3)
            Controls.Add(root) : AcceptButton = continuer : CancelButton = annuler
            If disponibles.Count > 0 Then liste.SelectedIndex = 0
            AddHandler Shown, Sub()
                                  Dim zone As Rectangle = Screen.FromControl(Me).WorkingArea
                                  Size = New Size(Math.Min(Width, zone.Width - 24), Math.Min(Height, zone.Height - 24))
                              End Sub
        End Sub
    End Class
End Namespace
