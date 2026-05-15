Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Collections.Generic
Imports System.Data

Namespace DevCommerc8ak
    Public Class NotificationPopup1
        Inherits Form

        Private ReadOnly timerClose As Timer

        Public Sub New(messages As IEnumerable(Of String))
            Me.FormBorderStyle = FormBorderStyle.None
            Me.StartPosition = FormStartPosition.Manual
            Me.Width = 320
            Me.Height = 160
            Me.BackColor = Color.FromArgb(35, 45, 65)
            Me.TopMost = True
            Me.Cursor = Cursors.Hand

            Dim lblTitre As New Label() With {.Text = "Notifications", .ForeColor = Color.White, .Left = 12, .Top = 10, .AutoSize = True, .Font = New Font("Segoe UI", 10, FontStyle.Bold)}
            Dim lblBody As New Label() With {.ForeColor = Color.White, .Left = 12, .Top = 35, .Width = 292, .Height = 100}

            Dim txt As String = String.Join(Environment.NewLine, messages)
            lblBody.Text = txt

            Me.Controls.Add(lblTitre)
            Me.Controls.Add(lblBody)

            Dim area As Rectangle = Screen.PrimaryScreen.WorkingArea
            Me.Left = area.Right - Me.Width - 10
            Me.Top = area.Bottom - Me.Height - 10

            timerClose = New Timer() With {.Interval = 4000}
            AddHandler timerClose.Tick, AddressOf Fermer
            AddHandler Me.Click, AddressOf OuvrirCentreNotifications
            AddHandler lblTitre.Click, AddressOf OuvrirCentreNotifications
            AddHandler lblBody.Click, AddressOf OuvrirCentreNotifications
            timerClose.Start()
        End Sub

        Private Sub Fermer(sender As Object, e As EventArgs)
            timerClose.Stop()
            Me.Close()
        End Sub

        Private Sub OuvrirCentreNotifications(sender As Object, e As EventArgs)
            timerClose.Stop()
            Dim frm As New FormulaireNotifications()
            frm.Show()
            Me.Close()
        End Sub
    End Class
End Namespace
