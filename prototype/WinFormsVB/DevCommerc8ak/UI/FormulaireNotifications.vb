Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireNotifications1
        Inherits Form

        Private ReadOnly gridNotifications As DataGridView
        Private ReadOnly btnMarquerLues As Button
        Private ReadOnly btnActualiser As Button

        Public Sub New()
            Me.Text = "Notifications"
            Me.Width = 920
            Me.Height = 560
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = Color.White

            btnActualiser = New Button() With {.Text = "Actualiser", .Left = 20, .Top = 18, .Width = 110}
            btnMarquerLues = New Button() With {.Text = "Marquer lues", .Left = 140, .Top = 18, .Width = 120}
            gridNotifications = New DataGridView() With {
                .Left = 20,
                .Top = 58,
                .Width = 860,
                .Height = 440,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AutoGenerateColumns = True,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False
            }

            Me.Controls.Add(btnActualiser)
            Me.Controls.Add(btnMarquerLues)
            Me.Controls.Add(gridNotifications)

            AddHandler btnActualiser.Click, AddressOf ChargerNotifications
            AddHandler btnMarquerLues.Click, AddressOf MarquerLues
            AddHandler gridNotifications.CellDoubleClick, AddressOf OuvrirNotification

            ThemeHelper.AppliquerTheme(Me)
            ChargerNotifications(Nothing, EventArgs.Empty)
        End Sub

        Private Function ObtenirService() As NotificationService
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Return New NotificationService(dal)
        End Function

        Private Sub ChargerNotifications(sender As Object, e As EventArgs)
            Try
                Dim service As NotificationService = ObtenirService()
                gridNotifications.DataSource = service.ListerToutes()
                If gridNotifications.Columns.Contains("CleNotification") Then gridNotifications.Columns("CleNotification").Visible = False
                If gridNotifications.Columns.Contains("DonneesCible") Then gridNotifications.Columns("DonneesCible").Visible = False
                If gridNotifications.Columns.Contains("NotificationId") Then gridNotifications.Columns("NotificationId").Visible = False
            Catch ex As Exception
                MessageBox.Show("Erreur chargement notifications: " & ex.Message)
            End Try
        End Sub

        Private Sub MarquerLues(sender As Object, e As EventArgs)
            Try
                Dim service As NotificationService = ObtenirService()
                service.MarquerToutesLues()
                ChargerNotifications(Nothing, EventArgs.Empty)
            Catch ex As Exception
                MessageBox.Show("Erreur mise à jour notifications: " & ex.Message)
            End Try
        End Sub

        Private Sub OuvrirNotification(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex < 0 OrElse gridNotifications.Rows.Count <= e.RowIndex Then
                Return
            End If

            Dim ecranCible As String = Convert.ToString(gridNotifications.Rows(e.RowIndex).Cells("EcranCible").Value)
            If ecranCible = "" Then
                Return
            End If

            Select Case ecranCible.ToUpperInvariant()
                Case "ALERTES_STOCK"
                    Dim frm As New FormulaireStock()
                    frm.Show()
                Case "APPROVISIONNEMENT"
                    Dim frm As New FormulaireApprovisionnement()
                    frm.Show()
                Case "FACTURES"
                    Dim frm As New FormulaireFactures()
                    frm.Show()
                Case "PRODUITS"
                    Dim frm As New FormulaireProduits()
                    frm.Show()
            End Select
        End Sub
    End Class
End Namespace
