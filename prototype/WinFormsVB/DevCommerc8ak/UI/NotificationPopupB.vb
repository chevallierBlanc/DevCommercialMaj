Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Collections.Generic
Imports System.Drawing.Drawing2D

Namespace DevCommerc8ak
    Public Class NotificationPopup
        Inherits Form

        ' --- Composants ---
        Private ReadOnly timerClose As Timer
        Private ReadOnly lblTitre As Label
        Private ReadOnly lblBody As Label
        Private ReadOnly btnVoir As Button
        Private ReadOnly btnFermer As Button
        Private ReadOnly pnlAccent As Panel

        ' --- Design ---
        Private ReadOnly ColorBg As Color = Color.White
        Private ReadOnly ColorIndigo As Color = Color.FromArgb(52, 73, 94) ' Gris Foncé
        Private ReadOnly ColorText As Color = Color.FromArgb(45, 45, 45)
        Private ReadOnly ColorSecondary As Color = Color.FromArgb(100, 100, 100)
        Private ReadOnly FontTitre As New Font("Segoe UI Variable Display", 10, FontStyle.Bold)
        Private ReadOnly FontBody As New Font("Segoe UI", 9)
        Private ReadOnly FontBtn As New Font("Segoe UI", 8, FontStyle.Bold)

        Public Sub New(messages As IEnumerable(Of String))
            ' Configuration du Formulaire
            Me.FormBorderStyle = FormBorderStyle.None
            Me.StartPosition = FormStartPosition.Manual
            Me.Width = 350
            Me.Height = 130
            Me.BackColor = ColorBg
            Me.TopMost = True
            Me.ShowInTaskbar = False
            
            ' Bordure fine Indigo
            Me.Padding = New Padding(1)
            
            ' Barre d'accentuation à gauche
            pnlAccent = New Panel() With {
                .Dock = DockStyle.Left,
                .Width = 6,
                .BackColor = ColorIndigo
            }
            Me.Controls.Add(pnlAccent)

            ' Titre
            lblTitre = New Label() With {
                .Text = "NOTIFICATIONS",
                .ForeColor = ColorIndigo,
                .Font = FontTitre,
                .AutoSize = True,
                .Left = 20,
                .Top = 15
            }
            Me.Controls.Add(lblTitre)

            ' Bouton Fermer (X)
            btnFermer = New Button() With {
                .Text = "✕",
                .ForeColor = ColorSecondary,
                .FlatStyle = FlatStyle.Flat,
                .Width = 25,
                .Height = 25,
                .Top = 10,
                .Left = Me.Width - 35,
                .Cursor = Cursors.Hand
            }
            btnFermer.FlatAppearance.BorderSize = 0
            AddHandler btnFermer.Click, AddressOf Fermer
            Me.Controls.Add(btnFermer)

            ' Corps du message
            lblBody = New Label() With {
                .ForeColor = ColorText,
                .Font = FontBody,
                .Left = 20,
                .Top = 40,
                .Width = 310,
                .Height = 50,
                .AutoEllipsis = True
            }
            Dim txt As String = String.Join(Environment.NewLine, messages)
            lblBody.Text = txt
            Me.Controls.Add(lblBody)

            ' Bouton "Voir"
            btnVoir = New Button() With {
                .Text = "VOIR TOUT",
                .Width = 90,
                .Height = 28,
                .Top = Me.Height - 40,
                .Left = Me.Width - 110,
                .BackColor = ColorIndigo,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontBtn,
                .Cursor = Cursors.Hand
            }
            btnVoir.FlatAppearance.BorderSize = 0
            AddHandler btnVoir.Click, AddressOf OuvrirCentreNotifications
            Me.Controls.Add(btnVoir)

            ' Positionnement en bas à droite
            Dim area As Rectangle = Screen.PrimaryScreen.WorkingArea
            Me.Left = area.Right - Me.Width - 20
            Me.Top = area.Bottom - Me.Height - 20

            ' Timer de fermeture
            timerClose = New Timer() With {.Interval = 5000}
            AddHandler timerClose.Tick, AddressOf Timer_Tick
            timerClose.Start()

            ' Gestion du survol de la souris (Hover)
            ' On applique récursivement aux contrôles enfants
            AddHandler Me.MouseEnter, AddressOf OnMouseHoverEnter
            AddHandler Me.MouseLeave, AddressOf OnMouseHoverLeave
            For Each ctrl As Control In Me.Controls
                AddHandler ctrl.MouseEnter, AddressOf OnMouseHoverEnter
                AddHandler ctrl.MouseLeave, AddressOf OnMouseHoverLeave
            Next
        End Sub

        ' --- GESTION DU SURVOL ---
        Private Sub OnMouseHoverEnter(sender As Object, e As EventArgs)
            ' Arrêter le timer si la souris entre dans le popup
            timerClose.Stop()
        End Sub

        Private Sub OnMouseHoverLeave(sender As Object, e As EventArgs)
            ' Relancer le timer si la souris sort du popup (sauf si elle entre dans un enfant)
            Dim clientMousePos As Point = Me.PointToClient(Control.MousePosition)
            If Not Me.ClientRectangle.Contains(clientMousePos) Then
                timerClose.Start()
            End If
        End Sub

        ' --- LOGIQUE MÉTIER ---
        Private Sub Timer_Tick(sender As Object, e As EventArgs)
            Fermer(Nothing, EventArgs.Empty)
        End Sub

        Private Sub Fermer(sender As Object, e As EventArgs)
            timerClose.Stop()
            Me.Close()
        End Sub

        Private Sub OuvrirCentreNotifications(sender As Object, e As EventArgs)
            timerClose.Stop()
            Try
                Dim frm As New FormulaireNotifications()
                frm.Show()
            Catch ex As Exception
                ' En cas d'erreur d'ouverture
            End Try
            Me.Close()
        End Sub

        ' Dessiner une bordure fine
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            ControlPaint.DrawBorder(e.Graphics, Me.ClientRectangle, Color.FromArgb(220, 220, 220), ButtonBorderStyle.Solid)
        End Sub
    End Class
End Namespace
