Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireVente
        Inherits Form

        Private ReadOnly ColorBg As Color = Color.FromArgb(245, 247, 250)
        Private ReadOnly ColorPrimary As Color = Color.FromArgb(52, 73, 94)
        Private ReadOnly ColorSecondary As Color = Color.FromArgb(41, 128, 185)
        Private ReadOnly ColorAccent As Color = Color.FromArgb(39, 174, 96)
        Private ReadOnly ColorCard As Color = Color.White
        Private ReadOnly ColorBorder As Color = Color.FromArgb(230, 230, 230)

        Private ReadOnly FontTitle As New Font("Segoe UI", 18.0F, FontStyle.Bold)
        Private ReadOnly FontSubTitle As New Font("Segoe UI", 10.0F)
        Private ReadOnly FontLabel As New Font("Segoe UI", 9.5F, FontStyle.Bold)
        Private ReadOnly FontControl As New Font("Segoe UI", 9.5F)

        Private ReadOnly tabs As TabControl
        Private ReadOnly gridVentes As DataGridView
        Private ReadOnly gridStock As DataGridView
        Private ReadOnly cmbPeriode As ComboBox
        Private ReadOnly dtpJour As DateTimePicker
        Private ReadOnly cmbMois As ComboBox
        Private ReadOnly cmbAnnee As ComboBox
        Private ReadOnly btnRafraichirVentes As Button
        Private ReadOnly btnRafraichirStock As Button
        Private ReadOnly lblResumeVentes As Label
        Private ReadOnly lblResumeStock As Label

        Private ReadOnly _service As VenteService

        Public Sub New()
            Me.Text = "Ventes"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Width = 1350
            Me.Height = 850
            Me.BackColor = ColorBg
            Me.DoubleBuffered = True

            _service = New VenteService()

            Dim pnlHeader As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 92,
                .BackColor = ColorPrimary,
                .Padding = New Padding(24, 18, 24, 18)
            }

            Dim lblTitre As New Label() With {
                .Text = "Analyse des ventes",
                .Font = FontTitle,
                .ForeColor = Color.White,
                .AutoSize = True,
                .Left = 24,
                .Top = 14
            }
            Dim lblSousTitre As New Label() With {
                .Text = "Suivi des ventes journalieres, mensuelles et annuelles avec analyse du stock restant.",
                .Font = FontSubTitle,
                .ForeColor = Color.FromArgb(220, 230, 245),
                .AutoSize = True,
                .Left = 26,
                .Top = 54
            }
            pnlHeader.Controls.Add(lblTitre)
            pnlHeader.Controls.Add(lblSousTitre)

            Dim pnlContent As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(16), .BackColor = ColorBg}
            tabs = New TabControl() With {.Dock = DockStyle.Fill}

            Dim tabVentes As New TabPage("Ventes journalieres") With {.BackColor = ColorBg, .Padding = New Padding(12)}
            Dim tabStock As New TabPage("Stock produits") With {.BackColor = ColorBg, .Padding = New Padding(12)}
            tabs.TabPages.Add(tabVentes)
            tabs.TabPages.Add(tabStock)

            ' --- Onglet 1 : ventes ---
            Dim pnlFiltres As New Panel() With {.Dock = DockStyle.Top, .Height = 112, .BackColor = ColorCard, .Padding = New Padding(14)}
            pnlFiltres.BorderStyle = BorderStyle.FixedSingle

            pnlFiltres.Controls.Add(New Label() With {.Text = "Periode", .Left = 14, .Top = 16, .AutoSize = True, .Font = FontLabel, .ForeColor = Color.FromArgb(90, 90, 90)})
            cmbPeriode = New ComboBox() With {.Left = 14, .Top = 38, .Width = 160, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl}
            cmbPeriode.Items.AddRange(New Object() {"Jour", "Mois", "Annee"})

            pnlFiltres.Controls.Add(New Label() With {.Text = "Jour", .Left = 200, .Top = 16, .AutoSize = True, .Font = FontLabel, .ForeColor = Color.FromArgb(90, 90, 90)})
            dtpJour = New DateTimePicker() With {.Left = 200, .Top = 38, .Width = 145, .Format = DateTimePickerFormat.Short, .Font = FontControl}

            pnlFiltres.Controls.Add(New Label() With {.Text = "Mois", .Left = 365, .Top = 16, .AutoSize = True, .Font = FontLabel, .ForeColor = Color.FromArgb(90, 90, 90)})
            cmbMois = New ComboBox() With {.Left = 365, .Top = 38, .Width = 80, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl}

            pnlFiltres.Controls.Add(New Label() With {.Text = "Annee", .Left = 460, .Top = 16, .AutoSize = True, .Font = FontLabel, .ForeColor = Color.FromArgb(90, 90, 90)})
            cmbAnnee = New ComboBox() With {.Left = 460, .Top = 38, .Width = 90, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl}

            btnRafraichirVentes = New Button() With {
                .Text = "Actualiser",
                .Left = 580,
                .Top = 34,
                .Width = 120,
                .Height = 32,
                .BackColor = ColorSecondary,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontLabel,
                .Cursor = Cursors.Hand
            }
            btnRafraichirVentes.FlatAppearance.BorderSize = 0

            lblResumeVentes = New Label() With {
                .Left = 720,
                .Top = 38,
                .Width = 560,
                .Height = 38,
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .ForeColor = ColorPrimary,
                .Text = "CA: 0 FC | Benefice: 0 FC | Quantite: 0"
            }

            pnlFiltres.Controls.Add(cmbPeriode)
            pnlFiltres.Controls.Add(dtpJour)
            pnlFiltres.Controls.Add(cmbMois)
            pnlFiltres.Controls.Add(cmbAnnee)
            pnlFiltres.Controls.Add(btnRafraichirVentes)
            pnlFiltres.Controls.Add(lblResumeVentes)

            gridVentes = CreerGrille()
            gridVentes.Dock = DockStyle.Fill

            tabVentes.Controls.Add(gridVentes)
            tabVentes.Controls.Add(pnlFiltres)

            ' --- Onglet 2 : stock ---
            Dim pnlStockTop As New Panel() With {.Dock = DockStyle.Top, .Height = 66, .BackColor = ColorCard, .Padding = New Padding(14)}
            pnlStockTop.BorderStyle = BorderStyle.FixedSingle

            btnRafraichirStock = New Button() With {
                .Text = "Actualiser le stock",
                .Left = 14,
                .Top = 14,
                .Width = 160,
                .Height = 32,
                .BackColor = ColorAccent,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontLabel,
                .Cursor = Cursors.Hand
            }
            btnRafraichirStock.FlatAppearance.BorderSize = 0

            lblResumeStock = New Label() With {
                .Left = 196,
                .Top = 18,
                .Width = 1000,
                .Height = 24,
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .ForeColor = ColorPrimary,
                .Text = "Stock global: 0 | Sorties ventes: 0 | Sorties manuelles: 0"
            }

            pnlStockTop.Controls.Add(btnRafraichirStock)
            pnlStockTop.Controls.Add(lblResumeStock)

            gridStock = CreerGrille()
            gridStock.Dock = DockStyle.Fill

            tabStock.Controls.Add(gridStock)
            tabStock.Controls.Add(pnlStockTop)

            pnlContent.Controls.Add(tabs)
            Me.Controls.Add(pnlContent)
            Me.Controls.Add(pnlHeader)

            AddHandler cmbPeriode.SelectedIndexChanged, AddressOf ActualiserFiltresPeriode
            AddHandler btnRafraichirVentes.Click, Sub() ChargerVentes()
            AddHandler btnRafraichirStock.Click, Sub() ChargerStock()
            AddHandler tabs.SelectedIndexChanged, AddressOf ChargerOngletActif
            AddHandler Me.Load, AddressOf FormulaireVente_Load

            InitialiserCombos()
            ActualiserFiltresPeriode(Nothing, EventArgs.Empty)
        End Sub

        Private Sub FormulaireVente_Load(sender As Object, e As EventArgs)
            ChargerVentes()
            ChargerStock()
        End Sub

        Private Sub InitialiserCombos()
            cmbPeriode.SelectedIndex = 0
            cmbMois.Items.Clear()
            For i As Integer = 1 To 12
                cmbMois.Items.Add(i.ToString("00"))
            Next
            cmbMois.SelectedItem = Date.Today.Month.ToString("00")

            cmbAnnee.Items.Clear()
            Dim anneeCourante As Integer = Date.Today.Year
            For i As Integer = anneeCourante - 5 To anneeCourante + 5
                cmbAnnee.Items.Add(i.ToString())
            Next
            cmbAnnee.SelectedItem = anneeCourante.ToString()
        End Sub

        Private Sub ActualiserFiltresPeriode(sender As Object, e As EventArgs)
            Dim periode As String = Convert.ToString(cmbPeriode.SelectedItem)
            Dim afficherJour As Boolean = String.Equals(periode, "Jour", StringComparison.OrdinalIgnoreCase)
            Dim afficherMois As Boolean = String.Equals(periode, "Mois", StringComparison.OrdinalIgnoreCase)
            Dim afficherAnnee As Boolean = String.Equals(periode, "Annee", StringComparison.OrdinalIgnoreCase)

            dtpJour.Visible = afficherJour
            cmbMois.Visible = afficherMois
            cmbAnnee.Visible = afficherMois OrElse afficherAnnee

            ChargerVentes()
        End Sub

        Private Sub ChargerOngletActif(sender As Object, e As EventArgs)
            If tabs.SelectedTab IsNot Nothing AndAlso tabs.SelectedTab.Text.Contains("Stock") Then
                ChargerStock()
            Else
                ChargerVentes()
            End If
        End Sub

        Private Sub ChargerVentes()
            Try
                Dim periode As String = Convert.ToString(cmbPeriode.SelectedItem)
                Dim dt As DataTable

                Select Case periode
                    Case "Mois"
                        Dim mois As Integer = Convert.ToInt32(cmbMois.SelectedItem)
                        Dim annee As Integer = Convert.ToInt32(cmbAnnee.SelectedItem)
                        dt = _service.ListerVentesMois(annee, mois)
                    Case "Annee"
                        Dim annee As Integer = Convert.ToInt32(cmbAnnee.SelectedItem)
                        dt = _service.ListerVentesAnnee(annee)
                    Case Else
                        dt = _service.ListerVentesJour(dtpJour.Value.Date)
                End Select

                gridVentes.DataSource = dt
                ConfigurerGrilleVentes()
                MettreAJourResumeVentes(dt)
            Catch ex As Exception
                MessageBox.Show("Impossible de charger les ventes : " & ex.Message, "Ventes", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub ChargerStock()
            Try
                Dim dt As DataTable = _service.ListerStockResume()
                gridStock.DataSource = dt
                ConfigurerGrilleStock()
                MettreAJourResumeStock(dt)
            Catch ex As Exception
                MessageBox.Show("Impossible de charger le stock : " & ex.Message, "Ventes", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub MettreAJourResumeVentes(dt As DataTable)
            Dim totalMontant As Decimal = 0D
            Dim totalBenefice As Decimal = 0D
            Dim totalQuantite As Decimal = 0D

            If dt IsNot Nothing Then
                For Each row As DataRow In dt.Rows
                    totalMontant += LireDecimal(row, "MontantGenere")
                    totalBenefice += LireDecimal(row, "Benefice")
                    totalQuantite += LireDecimal(row, "QuantiteVenduePieces")
                Next
            End If

            lblResumeVentes.Text = "CA: " & FormatageGlobal.FormatMontant(totalMontant) &
                " | Benefice: " & FormatageGlobal.FormatMontant(totalBenefice) &
                " | Quantite: " & FormatageGlobal.FormatNombre(totalQuantite)
        End Sub

        Private Sub MettreAJourResumeStock(dt As DataTable)
            Dim stockGlobal As Decimal = 0D
            Dim sortiesVentes As Decimal = 0D
            Dim sortiesManuelles As Decimal = 0D

            If dt IsNot Nothing Then
                For Each row As DataRow In dt.Rows
                    stockGlobal += LireDecimal(row, "StockActuelPieces")
                    sortiesVentes += LireDecimal(row, "QuantiteVenduePieces")
                    sortiesManuelles += LireDecimal(row, "QuantiteSortieManuellePieces")
                Next
            End If

            lblResumeStock.Text = "Stock global: " & FormatageGlobal.FormatNombre(stockGlobal) &
                " | Sorties ventes: " & FormatageGlobal.FormatNombre(sortiesVentes) &
                " | Sorties manuelles: " & FormatageGlobal.FormatNombre(sortiesManuelles)
        End Sub

        Private Sub ConfigurerGrilleVentes()
            If gridVentes.Columns.Count = 0 Then
                Return
            End If

            ConfigurerColonne(gridVentes, "DateVente", "Date vente", 150, "dd/MM/yyyy HH:mm")
            ConfigurerColonne(gridVentes, "Produit", "Produit", 220)
            ConfigurerColonne(gridVentes, "PrixAchatCarton", "Prix achat carton (FC)", 140, "N0")
            ConfigurerColonne(gridVentes, "QuantiteVenduePieces", "Quantite vendue (pieces)", 140, "N0")
            ConfigurerColonne(gridVentes, "MontantGenere", "Montant genere (FC)", 160, "N0")
            ConfigurerColonne(gridVentes, "Benefice", "Benefice (FC)", 120, "N0")
        End Sub

        Private Sub ConfigurerGrilleStock()
            If gridStock.Columns.Count = 0 Then
                Return
            End If

            ConfigurerColonne(gridStock, "ProduitId", "ID", 60)
            If gridStock.Columns.Contains("ProduitId") Then
                gridStock.Columns("ProduitId").Visible = False
            End If
            ConfigurerColonne(gridStock, "Produit", "Produit", 220)
            ConfigurerColonne(gridStock, "ConversionUnite", "Conversion", 90, "N0")
            ConfigurerColonne(gridStock, "StockActuelPieces", "Stock actuel pieces", 120, "N0")
            ConfigurerColonne(gridStock, "StockActuelCartons", "Stock actuel cartons", 120, "N0")
            ConfigurerColonne(gridStock, "QuantiteVenduePieces", "Ventes pieces", 110, "N0")
            ConfigurerColonne(gridStock, "QuantiteVendueCartons", "Ventes cartons", 110, "N0")
            ConfigurerColonne(gridStock, "QuantiteSortieManuellePieces", "Sorties manuelles pieces", 130, "N0")
            ConfigurerColonne(gridStock, "QuantiteSortieManuelleCartons", "Sorties manuelles cartons", 130, "N0")
            ConfigurerColonne(gridStock, "SortiesTotalesPieces", "Sorties totales pieces", 120, "N0")
            ConfigurerColonne(gridStock, "SortiesTotalesCartons", "Sorties totales cartons", 120, "N0")
            ConfigurerColonne(gridStock, "RestantPieces", "Restant pieces", 110, "N0")
            ConfigurerColonne(gridStock, "RestantCartons", "Restant cartons", 110, "N0")
        End Sub

        Private Sub ConfigurerColonne(grid As DataGridView, nom As String, titre As String, largeur As Integer, Optional format As String = Nothing)
            If Not grid.Columns.Contains(nom) Then
                Return
            End If

            Dim col As DataGridViewColumn = grid.Columns(nom)
            col.HeaderText = titre
            col.Width = largeur
            If Not String.IsNullOrWhiteSpace(format) Then
                col.DefaultCellStyle.Format = format
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            End If
        End Sub

        Private Function CreerGrille() As DataGridView
            Dim dgv As New DataGridView() With {
                .BackgroundColor = ColorCard,
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
                .GridColor = ColorBorder
            }
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(45, 45, 45)
            dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            dgv.ColumnHeadersHeight = 38
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 234, 246)
            dgv.DefaultCellStyle.SelectionForeColor = ColorPrimary
            Return dgv
        End Function

        Private Shared Function LireDecimal(row As DataRow, colonne As String) As Decimal
            If row Is Nothing OrElse row.Table Is Nothing OrElse Not row.Table.Columns.Contains(colonne) OrElse row.IsNull(colonne) Then
                Return 0D
            End If
            Return Convert.ToDecimal(row(colonne))
        End Function
    End Class
End Namespace
