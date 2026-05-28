Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulairePaiements
        Inherits Form

        Private ReadOnly grid As DataGridView
        Private ReadOnly txtFactureId As TextBox
        Private ReadOnly btnCharger As Button
        Private ReadOnly timer As Timer

        Public Sub New()
            Me.Text = "Paiements"
            Me.Width = 800
            Me.Height = 600

            Dim panelTop As New Panel() With {.Dock = DockStyle.Top, .Height = 50}
            txtFactureId = New TextBox() With {.Left = 20, .Top = 15, .Width = 120}
            btnCharger = New Button() With {.Text = "Charger", .Left = 160, .Top = 12, .Width = 100}
            AddHandler btnCharger.Click, AddressOf Charger

            panelTop.Controls.Add(New Label() With {.Text = "FactureId", .Left = 20, .Top = 0, .AutoSize = True})
            panelTop.Controls.Add(txtFactureId)
            panelTop.Controls.Add(btnCharger)

            grid = New DataGridView() With {.Dock = DockStyle.Fill, .AutoGenerateColumns = True, .ReadOnly = True}

            Me.Controls.Add(grid)
            Me.Controls.Add(panelTop)

            ThemeHelper.AppliquerTheme(Me)

            timer = New Timer() With {.Interval = 600000}
            AddHandler timer.Tick, AddressOf Charger
            timer.Start()
        End Sub

        Private Sub Charger(sender As Object, e As EventArgs)
            Try
                Dim id As Integer = Convert.ToInt32(txtFactureId.Text.Trim())
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim repo As New PaiementRepository(dal)
                grid.DataSource = repo.ListerParFacture(id)
            Catch ex As Exception
                MessageBox.Show("Erreur chargement paiements: " & ex.Message)
            End Try
        End Sub
    End Class
End Namespace
