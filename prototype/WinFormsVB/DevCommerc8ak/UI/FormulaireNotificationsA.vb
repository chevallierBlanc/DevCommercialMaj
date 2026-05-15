Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Runtime.InteropServices

Namespace DevCommerc8ak
    Public Class FormulaireNotifications
        Inherits Form

        ' --- Composants UI ---
        Private ReadOnly gridNotifications As DataGridView
        Private ReadOnly btnMarquerLues As Button
        Private ReadOnly btnActualiser As Button
        Private ReadOnly btnFermer As Button
        Private ReadOnly lblTitre As Label
        Private ReadOnly pnlHeader As Panel
        Private ReadOnly pnlMain As Panel

        Private ReadOnly ColorBackground As Color = Color.FromArgb(245, 247, 250)
        Private ReadOnly ColorCard As Color = Color.White
        Private ReadOnly ColorText As Color = Color.FromArgb(33, 33, 33)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(117, 117, 117)
        Private ReadOnly ColorBorder As Color = Color.FromArgb(230, 230, 230)
        'Private ReadOnly FontTitle As New Font("Segoe UI Semibold", 18.0F)
        Private ReadOnly FontSubTitle As New Font("Segoe UI", 10.0F)
        Private ReadOnly FontLabel As New Font("Segoe UI Semibold", 9.0F)
        Private ReadOnly FontControl As New Font("Segoe UI", 9.5F)





        Private ReadOnly ColorPrimary As Color = Color.FromArgb(52, 73, 94) ' Gris Foncé
        Private ReadOnly ColorSecondary As Color = Color.FromArgb(41, 128, 185) ' Bleu Moderne
        Private ReadOnly ColorAccent As Color = Color.FromArgb(39, 174, 96) ' Vert Succès
        Private ReadOnly ColorDanger As Color = Color.FromArgb(192, 57, 43) ' Rouge Annuler
        Private ReadOnly ColorBg As Color = Color.FromArgb(245, 247, 250) ' Gris très clair
        Private ReadOnly ColorWhite As Color = Color.White
        Private ReadOnly FontMain As New Font("Segoe UI", 10)
        Private ReadOnly FontBold As New Font("Segoe UI", 10, FontStyle.Bold)
        Private ReadOnly FontTitle As New Font("Segoe UI", 18.0F, FontStyle.Bold)
        Private ReadOnly FontTotal As New Font("Segoe UI", 22, FontStyle.Bold)

        ' --- Drag and Drop pour Form sans bordure ---
        <DllImport("user32.dll", CharSet:=CharSet.Auto)>
        Private Shared Function SendMessage(hWnd As IntPtr, Msg As Integer, wParam As Integer, lParam As Integer) As Integer
        End Function
        <DllImport("user32.dll", CharSet:=CharSet.Auto)>
        Private Shared Function ReleaseCapture() As Boolean
        End Function
        Private Const WM_NCLBUTTONDOWN As Integer = &HA1
        Private Const HT_CAPTION As Integer = 2

        Public Sub New()
            ' Configuration du Formulaire
            Me.FormBorderStyle = FormBorderStyle.None
            Me.Width = 900
            Me.Height = 600
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = Color.FromArgb(245, 246, 250) ' Gris très clair moderne
            Me.Padding = New Padding(2) ' Pour simuler une bordure fine

            ' --- Header ---
            pnlHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 60,
                .BackColor = ColorPrimary
            }
            AddHandler pnlHeader.MouseDown, Sub(s, e)
                                                If e.Button = MouseButtons.Left Then
                                                    ReleaseCapture()
                                                    SendMessage(Me.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0)
                                                End If
                                            End Sub

            lblTitre = New Label() With {
                .Text = "🔔 Centre de Notifications",
                .ForeColor = Color.White,
                .Font = New Font("Segoe UI Variable Display", 14, FontStyle.Bold),
                .AutoSize = True,
                .Left = 20,
                .Top = 18
            }

            btnFermer = New Button() With {
                .Text = "✕",
                .ForeColor = Color.White,
                .BackColor = ColorDanger,
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                .Width = 40,
                .Height = 40,
                .Top = 10,
                .Left = Me.Width - 50,
                .Cursor = Cursors.Hand
            }
            btnFermer.FlatAppearance.BorderSize = 0
            AddHandler btnFermer.Click, Sub() Me.Close()

            pnlHeader.Controls.Add(lblTitre)
            pnlHeader.Controls.Add(btnFermer)

            ' --- Zone Principale (Layout Propre) ---
            pnlMain = New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(25),
                .BackColor = Color.Transparent
            }

            ' Barre d'actions
            Dim pnlActions As New FlowLayoutPanel() With {
                .Dock = DockStyle.Top,
                .Height = 50,
                .FlowDirection = FlowDirection.LeftToRight
            }

            btnActualiser = New Button() With {
                .Text = "🔄 Actualiser",
                .Width = 130,
                .Height = 38,
                .BackColor = ColorSecondary,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 10, 0)
            }
            btnActualiser.FlatAppearance.BorderSize = 0

            btnMarquerLues = New Button() With {
                .Text = "✔️ Tout marquer comme lu",
                .Width = 220,
                .Height = 38,
                .BackColor = ColorAccent,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .Cursor = Cursors.Hand
            }
            btnMarquerLues.FlatAppearance.BorderSize = 0

            pnlActions.Controls.Add(btnActualiser)
            pnlActions.Controls.Add(btnMarquerLues)

            ' Carte pour la Grille
            Dim pnlGridCard As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Padding = New Padding(15),
                .Margin = New Padding(0, 15, 0, 0)
            }
            ' Simuler une ombre légère
            AddHandler pnlGridCard.Paint, Sub(s, e)
                                              ControlPaint.DrawBorder(e.Graphics, pnlGridCard.ClientRectangle, Color.FromArgb(220, 220, 220), ButtonBorderStyle.Solid)
                                          End Sub

            'gridNotifications = New DataGridView() With {
            '    .Dock = DockStyle.Fill,
            '    .BackgroundColor = Color.White,
            '    .BorderStyle = BorderStyle.None,
            '    .ReadOnly = True,
            '    .AllowUserToAddRows = False,
            '    .AllowUserToDeleteRows = False,
            '    .AutoGenerateColumns = True,
            '    .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            '    .MultiSelect = False,
            '    .RowTemplate = New DataGridViewRow With {.Height = 45},
            '    .EnableHeadersVisualStyles = False,
            '    .GridColor = Color.FromArgb(240, 240, 240)
            '}
            'gridNotifications.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 246, 250)
            'gridNotifications.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(100, 100, 100)
            'gridNotifications.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 10, FontStyle.Bold)
            'gridNotifications.ColumnHeadersDefaultCellStyle.Padding = New Padding(10, 5, 10, 5)
            'gridNotifications.DefaultCellStyle.Font = New Font("Segoe UI", 10)
            'gridNotifications.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 242, 252)
            'gridNotifications.DefaultCellStyle.SelectionForeColor = Color.FromArgb(0, 120, 212)

            gridNotifications = CreateStyledGrid()
            gridNotifications.Dock = DockStyle.Fill

            pnlGridCard.Controls.Add(gridNotifications)

            pnlMain.Controls.Add(pnlGridCard)
            pnlMain.Controls.Add(pnlActions)

            Me.Controls.Add(pnlMain)
            Me.Controls.Add(pnlHeader)

            ' --- Événements ---
            AddHandler btnActualiser.Click, AddressOf ChargerNotifications
            AddHandler btnMarquerLues.Click, AddressOf MarquerLues
            AddHandler gridNotifications.CellDoubleClick, AddressOf OuvrirNotification

            ' Chargement initial
            ChargerNotifications(Nothing, EventArgs.Empty)
        End Sub

        ' --- LOGIQUE MÉTIER RÉINTÉGRÉE ---

        Private Function ObtenirService() As NotificationService
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Return New NotificationService(dal)
            Catch ex As Exception
                ' Fallback si la config est absente (pour le dev)
                Return Nothing
            End Try
        End Function

        Private Sub ChargerNotifications(sender As Object, e As EventArgs)
            Try
                Dim service As NotificationService = ObtenirService()
                If service Is Nothing Then Return

                gridNotifications.DataSource = service.ListerToutes()

                ' Masquage des colonnes techniques
                Dim colonnesAMasquer As String() = {"CleNotification", "DonneesCible", "NotificationId"}
                For Each colName As String In colonnesAMasquer
                    If gridNotifications.Columns.Contains(colName) Then
                        gridNotifications.Columns(colName).Visible = False
                    End If
                Next

                ' Ajustement des colonnes visibles
                If gridNotifications.Columns.Contains("Message") Then
                    gridNotifications.Columns("Message").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                End If
                If gridNotifications.Columns.Contains("DateNotification") Then
                    gridNotifications.Columns("DateNotification").Width = 150
                    gridNotifications.Columns("DateNotification").HeaderText = "Date & Heure"
                End If
                If gridNotifications.Columns.Contains("EstLu") Then
                    gridNotifications.Columns("EstLu").Width = 80
                    gridNotifications.Columns("EstLu").HeaderText = "Statut"
                End If

            Catch ex As Exception
                MessageBox.Show("Erreur chargement notifications: " & ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub MarquerLues(sender As Object, e As EventArgs)
            Try
                Dim service As NotificationService = ObtenirService()
                If service Is Nothing Then Return

                service.MarquerToutesLues()
                ChargerNotifications(Nothing, EventArgs.Empty)
            Catch ex As Exception
                MessageBox.Show("Erreur mise à jour notifications: " & ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub OuvrirNotification(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex < 0 OrElse gridNotifications.Rows.Count <= e.RowIndex Then
                Return
            End If

            Try
                Dim ecranCible As String = Convert.ToString(gridNotifications.Rows(e.RowIndex).Cells("EcranCible").Value)
                If String.IsNullOrEmpty(ecranCible) Then Return

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
            Catch ex As Exception
                MessageBox.Show("Impossible d'ouvrir l'écran cible : " & ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try
        End Sub

        ' Dessiner une bordure fine autour du formulaire sans bordure
        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            ControlPaint.DrawBorder(e.Graphics, Me.ClientRectangle, Color.FromArgb(30, 42, 68), ButtonBorderStyle.Solid)
        End Sub

        Private Function CreateStyledGrid() As DataGridView
            Dim dgv As New DataGridView() With {
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None,
                .EnableHeadersVisualStyles = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AllowUserToAddRows = False,
                .ReadOnly = True,
                .RowHeadersVisible = False,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .GridColor = ColorBorder
            }
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
            dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            dgv.ColumnHeadersHeight = 45
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 234, 246)
            dgv.DefaultCellStyle.SelectionForeColor = ColorPrimary
            dgv.DefaultCellStyle.Font = FontControl
            dgv.RowTemplate.Height = 35
            Return dgv
        End Function
    End Class
End Namespace
