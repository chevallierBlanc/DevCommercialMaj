Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D

Namespace DevCommerc8ak
    Public Class FormulaireRapports
        Inherits Form

        Private ReadOnly ColorBg As Color = Color.FromArgb(248, 249, 250) ' Gris très clair pour le fond
        Private ReadOnly ColorOriginalHeaderBg As Color = Color.FromArgb(52, 73, 94) ' Couleur originale du Header
        Private ReadOnly ColorCardBg As Color = Color.White ' Fond des cartes blanc
        Private ReadOnly ColorPrimary As Color = Color.FromArgb(63, 81, 181) ' Indigo (plus doux)
        Private ReadOnly ColorAccent As Color = Color.FromArgb(103, 58, 183) ' Violet (conservé)
        Private ReadOnly ColorSuccess As Color = Color.FromArgb(76, 175, 80) ' Vert (conservé)
        Private ReadOnly ColorDanger As Color = Color.FromArgb(244, 67, 54) ' Rouge (plus doux)
        Private ReadOnly ColorWarning As Color = Color.FromArgb(255, 152, 0) ' Orange (conservé)
        Private ReadOnly ColorNetBenefit As Color = Color.FromArgb(0, 150, 136) ' Cyan (plus doux)
        Private ReadOnly ColorTextPrimary As Color = Color.FromArgb(33, 33, 33) ' Texte foncé
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(90, 90, 90) ' Texte gris légèrement plus foncé
        Private ReadOnly ColorBorder As Color = Color.FromArgb(224, 224, 224) ' Bordure légère pour les cartes

        Private ReadOnly FontTitle As New Font("Segoe UI", 18.0F, FontStyle.Bold)
        Private ReadOnly FontSubtitle As New Font("Segoe UI", 10.0F, FontStyle.Regular)
        Private ReadOnly FontLabel As New Font("Segoe UI", 10.0F, FontStyle.Regular)
        Private ReadOnly FontControl As New Font("Segoe UI", 9.5F)
        Private ReadOnly FontButton As New Font("Segoe UI", 10.0F, FontStyle.Bold)

        Private ReadOnly cmbType As ComboBox
        Private ReadOnly dtDebut As DateTimePicker
        Private ReadOnly dtFin As DateTimePicker
        Private ReadOnly btnCharger As Button
        Private ReadOnly btnExportCsv As Button
        Private ReadOnly btnExportPdf As Button
        Private ReadOnly grid As DataGridView
        Private ReadOnly timer As Timer
        Private _isRefreshingFromEvent As Boolean

        Public Sub New()
            Me.Text = "Rapports"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.WindowState = FormWindowState.Maximized
            Me.BackColor = ColorBg
            Me.DoubleBuffered = True

            ' --- LAYOUT PRINCIPAL ---
            Dim mainLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3, ' Header, Filtres, Contenu
                .Padding = New Padding(0)
            }
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 92)) ' Header (hauteur originale)
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 100)) ' Filtres
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100)) ' Contenu (Grille)
            mainLayout.BackColor = ColorBg

            ' --- HEADER (Restauré à l'original) ---
            Dim pnlHeader As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorOriginalHeaderBg, ' Couleur originale
                .Padding = New Padding(24, 18, 24, 18)
            }
            pnlHeader.BorderStyle = BorderStyle.None

            Dim lblTitre As New Label() With {
                .Text = "Rapports",
                .Font = FontTitle,
                .ForeColor = Color.White, ' Couleur originale
                .AutoSize = True,
                .Left = 24,
                .Top = 14
            }
            Dim lblSousTitre As New Label() With {
                .Text = "Génération et exportation des rapports de ventes et de stock.",
                .Font = FontSubtitle,
                .ForeColor = Color.FromArgb(220, 230, 245), ' Couleur originale
                .AutoSize = True,
                .Left = 26,
                .Top = 54
            }
            pnlHeader.Controls.Add(lblTitre)
            pnlHeader.Controls.Add(lblSousTitre)

            ' Boutons dans le header (positionnement ajusté pour être à droite)
            btnExportPdf = New Button() With {
                .Text = "Export PDF",
                .Width = 120,
                .Height = 36,
                .BackColor = ColorAccent,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontButton,
                .Cursor = Cursors.Hand,
                .Anchor = AnchorStyles.Right Or AnchorStyles.Top,
                .Location = New Point(Me.Width - 150 - 120, 30) ' Ajustement de la position
            }
            btnExportPdf.FlatAppearance.BorderSize = 0
            btnExportPdf.FlatAppearance.MouseDownBackColor = Color.FromArgb(ColorAccent.R - 20, ColorAccent.G - 20, ColorAccent.B - 20)
            btnExportPdf.FlatAppearance.MouseOverBackColor = Color.FromArgb(ColorAccent.R + 20, ColorAccent.G + 20, ColorAccent.B + 20)

            btnExportCsv = New Button() With {
                .Text = "Export Excel",
                .Width = 120,
                .Height = 36,
                .BackColor = ColorPrimary,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontButton,
                .Cursor = Cursors.Hand,
                .Anchor = AnchorStyles.Right Or AnchorStyles.Top,
                .Location = New Point(Me.Width - 150 - 120 - 8 - 120, 30) ' Ajustement de la position
            }
            btnExportCsv.FlatAppearance.BorderSize = 0
            btnExportCsv.FlatAppearance.MouseDownBackColor = Color.FromArgb(ColorPrimary.R - 20, ColorPrimary.G - 20, ColorPrimary.B - 20)
            btnExportCsv.FlatAppearance.MouseOverBackColor = Color.FromArgb(ColorPrimary.R + 20, ColorPrimary.G + 20, ColorPrimary.B + 20)

            pnlHeader.Controls.Add(btnExportPdf)
            pnlHeader.Controls.Add(btnExportCsv)

            ' --- ZONE DE FILTRES ---
            Dim pnlFiltresCard As Panel = CreerCarte()
            pnlFiltresCard.Padding = New Padding(16)
            pnlFiltresCard.Margin = New Padding(24, 8, 24, 8) ' Marge autour de la carte

            Dim filtresLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 7,
                .RowCount = 1,
                .AutoSize = True
            }
            filtresLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            filtresLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 180))
            filtresLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            filtresLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120))
            filtresLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            filtresLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120))
            filtresLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            filtresLayout.BackColor = Color.Transparent

            Dim lblType As New Label() With {.Text = "Type de rapport :", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Anchor = AnchorStyles.Left}
            cmbType = New ComboBox() With {
                .Width = 160,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = FontControl,
                .Anchor = AnchorStyles.Left
            }
            cmbType.Items.AddRange(New Object() {"Journalier", "Mensuel", "Produits plus vendus"})
            cmbType.SelectedIndex = 0

            Dim lblDebut As New Label() With {.Text = "Du :", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Anchor = AnchorStyles.Left, .Margin = New Padding(10, 0, 0, 0)}
            dtDebut = New DateTimePicker() With {
                .Width = 100,
                .Format = DateTimePickerFormat.Short,
                .Font = FontControl,
                .Anchor = AnchorStyles.Left
            }

            Dim lblFin As New Label() With {.Text = "Au :", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Anchor = AnchorStyles.Left, .Margin = New Padding(10, 0, 0, 0)}
            dtFin = New DateTimePicker() With {
                .Width = 100,
                .Format = DateTimePickerFormat.Short,
                .Font = FontControl,
                .Anchor = AnchorStyles.Left
            }

            btnCharger = New Button() With {
                .Text = "Charger",
                .Width = 100,
                .Height = 30,
                .BackColor = ColorPrimary,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontButton,
                .Cursor = Cursors.Hand,
                .Anchor = AnchorStyles.Left,
                .Margin = New Padding(20, 0, 0, 0)
            }
            btnCharger.FlatAppearance.BorderSize = 0
            btnCharger.FlatAppearance.MouseDownBackColor = Color.FromArgb(ColorPrimary.R - 20, ColorPrimary.G - 20, ColorPrimary.B - 20)
            btnCharger.FlatAppearance.MouseOverBackColor = Color.FromArgb(ColorPrimary.R + 20, ColorPrimary.G + 20, ColorPrimary.B + 20)

            filtresLayout.Controls.Add(lblType, 0, 0)
            filtresLayout.Controls.Add(cmbType, 1, 0)
            filtresLayout.Controls.Add(lblDebut, 2, 0)
            filtresLayout.Controls.Add(dtDebut, 3, 0)
            filtresLayout.Controls.Add(lblFin, 4, 0)
            filtresLayout.Controls.Add(dtFin, 5, 0)
            filtresLayout.Controls.Add(btnCharger, 6, 0)

            pnlFiltresCard.Controls.Add(filtresLayout)

            ' --- ZONE DE CONTENU PRINCIPAL (GRILLE) ---
            Dim pnlGridCard As Panel = CreerCarte()
            pnlGridCard.Margin = New Padding(24, 0, 24, 24) ' Marge autour de la carte

            grid = CreerGrille()
            grid.Dock = DockStyle.Fill
            pnlGridCard.Controls.Add(grid)

            ' Ajout des contrôles au layout principal
            mainLayout.Controls.Add(pnlHeader, 0, 0)
            mainLayout.Controls.Add(pnlFiltresCard, 0, 1)
            mainLayout.Controls.Add(pnlGridCard, 0, 2)
            Me.Controls.Add(mainLayout)

            AddHandler btnCharger.Click, AddressOf Charger
            AddHandler btnExportCsv.Click, AddressOf ExportCsv
            AddHandler btnExportPdf.Click, AddressOf ExportPdf
            AddHandler cmbType.SelectedIndexChanged, AddressOf Charger ' Recharger quand le type de rapport change
            AddHandler dtDebut.ValueChanged, AddressOf Charger ' Recharger quand la date change
            AddHandler dtFin.ValueChanged, AddressOf Charger ' Recharger quand la date change

            timer = New Timer() With {.Interval = 600000}
            AddHandler timer.Tick, AddressOf Charger
            AddHandler AppEvents.DataChanged, AddressOf RafraichirDepuisEvenement
            timer.Start()

            Charger(Nothing, EventArgs.Empty) ' Charger les données au démarrage
        End Sub

        Private Sub RafraichirDepuisEvenement(sender As Object, e As EventArgs)
            If IsDisposed Then Return
            If InvokeRequired Then
                BeginInvoke(New MethodInvoker(Sub() RafraichirDepuisEvenement(Nothing, EventArgs.Empty)))
                Return
            End If
            If _isRefreshingFromEvent Then Return

            _isRefreshingFromEvent = True
            Try
                Charger(Nothing, EventArgs.Empty)
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireRapports", "RafraichirDepuisEvenement", "Erreur lors du rafraichissement automatique des rapports.", ex)
            Finally
                _isRefreshingFromEvent = False
            End Try
        End Sub

        Private Sub Charger(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim service As New RapportService(dal)

                If cmbType.SelectedItem.ToString() = "Produits plus vendus" Then
                    grid.DataSource = service.ProduitsPlusVendus(dtDebut.Value, dtFin.Value)
                ElseIf cmbType.SelectedItem.ToString() = "Journalier" Then
                    Dim sql As String = "SELECT CAST(CreeLe AS DATE) AS Jour, SUM(MontantTotal) AS CA " &
                                        "FROM FacturesVente WHERE CreeLe BETWEEN @d1 AND @d2 AND Statut='PAYEE' " &
                                        "GROUP BY CAST(CreeLe AS DATE) ORDER BY Jour"
                    Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                        New System.Data.SqlClient.SqlParameter("@d1", dtDebut.Value),
                        New System.Data.SqlClient.SqlParameter("@d2", dtFin.Value)
                    }
                    grid.DataSource = dal.ExecuterTable(sql, System.Data.CommandType.Text, p)
                ElseIf cmbType.SelectedItem.ToString() = "Mensuel" Then
                    Dim sql As String = "SELECT FORMAT(CreeLe,'yyyy-MM') AS Mois, SUM(MontantTotal) AS CA " &
                                        "FROM FacturesVente WHERE CreeLe BETWEEN @d1 AND @d2 AND Statut='PAYEE' " &
                                        "GROUP BY FORMAT(CreeLe,'yyyy-MM') ORDER BY Mois"
                    Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                        New System.Data.SqlClient.SqlParameter("@d1", dtDebut.Value),
                        New System.Data.SqlClient.SqlParameter("@d2", dtFin.Value)
                    }
                    grid.DataSource = dal.ExecuterTable(sql, System.Data.CommandType.Text, p)
                End If
                ConfigurerGrilleRapports() ' Appliquer le style après le chargement des données
            Catch ex As Exception
                MessageBox.Show("Erreur rapport: " & ex.Message)
            End Try
        End Sub


        Private Sub ExportCsv(sender As Object, e As EventArgs)
            Try
                Dim sfd As New SaveFileDialog() With {.Filter = "CSV (*.csv)|*.csv"}
                If sfd.ShowDialog() <> DialogResult.OK Then Return

                Dim sb As New StringBuilder()
                For Each col As DataGridViewColumn In grid.Columns
                    sb.Append(col.HeaderText & ";")
                Next
                sb.AppendLine()

                For Each row As DataGridViewRow In grid.Rows
                    If row.IsNewRow Then Continue For
                    For Each cell As DataGridViewCell In row.Cells
                        sb.Append(Convert.ToString(cell.Value) & ";")
                    Next
                    sb.AppendLine()
                Next

                File.WriteAllText(sfd.FileName, sb.ToString())
                MessageBox.Show("Export CSV termine.")
            Catch ex As Exception
                MessageBox.Show("Erreur export CSV: " & ex.Message)
            End Try
        End Sub

        Private Sub ExportPdf(sender As Object, e As EventArgs)
            Try
                Dim sfd As New SaveFileDialog() With {.Filter = "PDF (*.pdf)|*.pdf"}
                If sfd.ShowDialog() <> DialogResult.OK Then Return
                Dim lignes As New List(Of String)()
                For Each row As DataGridViewRow In grid.Rows
                    If row.IsNewRow Then Continue For
                    Dim line As String = ""
                    For Each cell As DataGridViewCell In row.Cells
                        line &= Convert.ToString(cell.Value) & " | "
                    Next
                    lignes.Add(line)
                Next
                PdfHelper.GenererPdfSimple(sfd.FileName, "RAPPORT", lignes)
                MessageBox.Show("PDF genere.")
            Catch ex As Exception
                MessageBox.Show("Erreur export PDF: " & ex.Message)
            End Try
        End Sub

        Private Function CreerGrille() As DataGridView
            Dim dgv As New DataGridView() With {
                .BackgroundColor = ColorCardBg,
                .BorderStyle = BorderStyle.None,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .ReadOnly = True,
                .AutoGenerateColumns = True,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .RowHeadersVisible = False,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .EnableHeadersVisualStyles = False,
                .Font = FontControl,
                .GridColor = Color.FromArgb(220, 224, 229)
            }
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = ColorTextPrimary
            dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(245, 245, 245)
            dgv.ColumnHeadersHeight = 38
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 234, 246)
            dgv.DefaultCellStyle.SelectionForeColor = ColorPrimary
            Return dgv
        End Function

        Private Function CreerCarte() As Panel
            Dim cardPanel As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorCardBg,
                .Margin = New Padding(8),
                .Padding = New Padding(16),
                .BorderStyle = BorderStyle.None ' Supprimer la bordure par défaut
            }
            AddHandler cardPanel.Paint, Sub(s, ev) DessinerCarteBordureOmbre(s, ev, cardPanel)
            Return cardPanel
        End Function

        Private Sub DessinerCarteBordureOmbre(sender As Object, e As PaintEventArgs, pnl As Panel)
            Dim rect As New Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1)
            Using pen As New Pen(ColorBorder, 1)
                e.Graphics.DrawRectangle(pen, rect)
            End Using

            Using shadowBrush As New SolidBrush(Color.FromArgb(20, 0, 0, 0)) ' Très légère ombre
                e.Graphics.FillRectangle(shadowBrush, pnl.Width - 3, 3, 3, pnl.Height - 3)
                e.Graphics.FillRectangle(shadowBrush, 3, pnl.Height - 3, pnl.Width - 3, 3)
            End Using
        End Sub

        Private Sub ConfigurerGrilleRapports()
            If grid.Columns.Count = 0 Then Return

            ' Exemple de configuration de colonnes, à adapter selon les rapports générés
            For Each col As DataGridViewColumn In grid.Columns
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            Next

            ' Cas spécifiques pour les rapports
            If cmbType.SelectedItem.ToString() = "Produits plus vendus" Then
                If grid.Columns.Contains("Produit") Then grid.Columns("Produit").Width = 250
                If grid.Columns.Contains("QuantiteVendue") Then
                    grid.Columns("QuantiteVendue").HeaderText = "Quantité Vendue"
                    grid.Columns("QuantiteVendue").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                    grid.Columns("QuantiteVendue").AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                End If
                If grid.Columns.Contains("MontantTotal") Then
                    grid.Columns("MontantTotal").HeaderText = "Montant Total"
                    grid.Columns("MontantTotal").DefaultCellStyle.Format = "N0"
                    grid.Columns("MontantTotal").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                    grid.Columns("MontantTotal").AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                End If
            ElseIf cmbType.SelectedItem.ToString() = "Journalier" Then
                If grid.Columns.Contains("Jour") Then
                    grid.Columns("Jour").DefaultCellStyle.Format = "dd/MM/yyyy"
                    grid.Columns("Jour").AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                End If
                If grid.Columns.Contains("CA") Then
                    grid.Columns("CA").HeaderText = "Chiffre d'Affaires"
                    grid.Columns("CA").DefaultCellStyle.Format = "N0"
                    grid.Columns("CA").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                    grid.Columns("CA").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                End If
            ElseIf cmbType.SelectedItem.ToString() = "Mensuel" Then
                If grid.Columns.Contains("Mois") Then
                    grid.Columns("Mois").AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                End If
                If grid.Columns.Contains("CA") Then
                    grid.Columns("CA").HeaderText = "Chiffre d'Affaires"
                    grid.Columns("CA").DefaultCellStyle.Format = "N0"
                    grid.Columns("CA").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                    grid.Columns("CA").AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                End If
            End If
        End Sub

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            RemoveHandler AppEvents.DataChanged, AddressOf RafraichirDepuisEvenement
            MyBase.OnFormClosed(e)
        End Sub

    End Class


End Namespace
