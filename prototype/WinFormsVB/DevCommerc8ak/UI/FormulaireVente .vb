Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D

Namespace DevCommerc8ak
    Public Class FormulaireVente
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
        Private ReadOnly ColorTabInactive As Color = Color.FromArgb(230, 230, 230)
        Private ReadOnly ColorTabActive As Color = Color.White

        Private ReadOnly FontTitle As New Font("Segoe UI", 18.0F, FontStyle.Bold) ' Taille originale
        Private ReadOnly FontSubtitle As New Font("Segoe UI", 10.0F, FontStyle.Regular) ' Taille originale
        Private ReadOnly FontLabel As New Font("Segoe UI", 10.0F, FontStyle.Regular)
        Private ReadOnly FontControl As New Font("Segoe UI", 9.5F)
        Private ReadOnly FontButton As New Font("Segoe UI", 10.0F, FontStyle.Bold)
        Private ReadOnly FontTab As New Font("Segoe UI", 10.0F, FontStyle.Bold)

        Private ReadOnly tabs As TabControl
        Private ReadOnly gridVentes As DataGridView
        Private ReadOnly gridStock As DataGridView
        Private ReadOnly tabDepenses As TabPage
        Private ReadOnly gridDepenses As DataGridView

        Private ReadOnly cmbPeriode As ComboBox
        Private ReadOnly dtpJour As DateTimePicker
        Private ReadOnly cmbMois As ComboBox
        Private ReadOnly cmbAnnee As ComboBox
        Private ReadOnly btnRafraichirVentes As Button

        Private ReadOnly btnRafraichirStock As Button

        Private ReadOnly cmbPeriodeDepenses As ComboBox
        Private ReadOnly dtpJourDepenses As DateTimePicker
        Private ReadOnly cmbMoisDepenses As ComboBox
        Private ReadOnly cmbAnneeDepenses As ComboBox
        Private ReadOnly btnRafraichirDepenses As Button
        Private ReadOnly btnImprimerVentes As Button
        Private ReadOnly btnExporterPdfVentes As Button
        Private ReadOnly btnImprimerStock As Button
        Private ReadOnly btnExporterPdfStock As Button

        Private ReadOnly lblResumeVentes As Label
        Private ReadOnly lblResumeStock As Label
        Private ReadOnly lblResumeDepenses As Label

        Private ReadOnly btnTabVentes As Button
        Private ReadOnly btnTabStock As Button
        Private ReadOnly btnTabDepenses As Button

        Private ReadOnly pdocDepenses As PrintDocument
        Private ReadOnly pdocVentes As PrintDocument
        Private ReadOnly pdocStock As PrintDocument

        Private ReadOnly _service As VenteService
        Private _depensesCourantes As DataTable
        Private _depensePrintRowIndex As Integer
        Private _ventesCourantes As DataTable
        Private _stockCourant As DataTable
        Private _ventePrintRowIndex As Integer
        Private _stockPrintRowIndex As Integer
        Private _venteRapportTitre As String = String.Empty
        Private _stockRapportTitre As String = String.Empty

        Public Sub New()
            Me.Text = "Ventes"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.WindowState = FormWindowState.Maximized
            Me.BackColor = ColorBg
            Me.DoubleBuffered = True

            _service = New VenteService()

            ' --- LAYOUT PRINCIPAL ---
            Dim mainLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3, ' Header, Tab Navigation, Content
                .Padding = New Padding(0)
            }
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 92)) ' Header (hauteur originale)
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 50))  ' Tab Navigation
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100)) ' Content (Tabs)
            mainLayout.BackColor = ColorBg

            ' --- HEADER (Restauré à l'original) ---
            Dim pnlHeader As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorOriginalHeaderBg, ' Couleur originale
                .Padding = New Padding(24, 18, 24, 18)
            }
            pnlHeader.BorderStyle = BorderStyle.None

            Dim lblTitre As New Label() With {
                .Text = "Analyse des ventes",
                .Font = FontTitle,
                .ForeColor = Color.White, ' Couleur originale
                .AutoSize = True,
                .Left = 24,
                .Top = 14
            }
            Dim lblSousTitre As New Label() With {
                .Text = "Suivi des ventes journalieres, mensuelles et annuelles avec analyse du stock restant.",
                .Font = FontSubtitle,
                .ForeColor = Color.FromArgb(220, 230, 245), ' Couleur originale
                .AutoSize = True,
                .Left = 26,
                .Top = 54
            }
            pnlHeader.Controls.Add(lblTitre)
            pnlHeader.Controls.Add(lblSousTitre)

            ' Boutons dans le header (positionnement ajusté pour être à droite)
            btnRafraichirVentes = New Button() With {
                .Text = "Actualiser",
                .Left = 1150,
                .Top = 34,
                .Width = 120,
                .Height = 36,
                .BackColor = ColorPrimary,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontButton,
                .Cursor = Cursors.Hand
                                       }
            btnRafraichirVentes.FlatAppearance.BorderSize = 0
            btnRafraichirVentes.FlatAppearance.MouseDownBackColor = Color.FromArgb(ColorPrimary.R - 20, ColorPrimary.G - 20, ColorPrimary.B - 20)
            btnRafraichirVentes.FlatAppearance.MouseOverBackColor = Color.FromArgb(ColorPrimary.R + 20, ColorPrimary.G + 20, ColorPrimary.B + 20)

            pnlHeader.Controls.Add(btnRafraichirVentes)

            ' --- NAVIGATION PAR ONGLET PERSONNALISÉE ---
            Dim pnlTabNavigation As New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.LeftToRight,
                .Padding = New Padding(24, 0, 0, 0),
                .Margin = New Padding(0, 0, 0, 0),
                .BackColor = ColorBg
            }

            btnTabVentes = New Button() With {
                .Text = "Ventes",
                .Width = 120,
                .Height = 40,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontTab,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 8, 0)
            }
            AddHandler btnTabVentes.Click, Sub() SetSelectedTab(0)

            btnTabStock = New Button() With {
                .Text = "Stock",
                .Width = 120,
                .Height = 40,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontTab,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 8, 0)
            }
            AddHandler btnTabStock.Click, Sub() SetSelectedTab(1)

            btnTabDepenses = New Button() With {
                .Text = "Dépenses",
                .Width = 120,
                .Height = 40,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontTab,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 0, 0)
            }
            AddHandler btnTabDepenses.Click, Sub() SetSelectedTab(2)

            pnlTabNavigation.Controls.Add(btnTabVentes)
            pnlTabNavigation.Controls.Add(btnTabStock)
            pnlTabNavigation.Controls.Add(btnTabDepenses)

            ' --- CONTENU PRINCIPAL (TABS) ---
            tabs = New TabControl() With {
                .Dock = DockStyle.Fill,
                .Padding = New Point(0, 0),
                .Margin = New Padding(24, 0, 24, 24) ' Marge autour des onglets, pas de marge supérieure car gérée par la navigation
            }
            tabs.Appearance = TabAppearance.FlatButtons
            tabs.ItemSize = New Size(0, 1) ' Cache les en-têtes d'onglets
            tabs.SizeMode = TabSizeMode.Fixed
            AddHandler tabs.GotFocus, AddressOf Tabs_GotFocus ' Empêche le focus sur les onglets

            Dim tabVentes As New TabPage("Ventes journalieres") With {.BackColor = ColorBg, .Padding = New Padding(0)}
            Dim tabStock As New TabPage("Stock produits") With {.BackColor = ColorBg, .Padding = New Padding(0)}
            tabDepenses = New TabPage("Dépenses") With {.BackColor = ColorBg, .Padding = New Padding(0)}
            tabs.TabPages.Add(tabVentes)
            tabs.TabPages.Add(tabStock)
            tabs.TabPages.Add(tabDepenses)

            ' --- Onglet 1 : ventes ---
            Dim pnlVentesContent As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3,
                .BackColor = ColorBg
            }
            pnlVentesContent.RowStyles.Add(New RowStyle(SizeType.Absolute, 130)) ' Filtres
            pnlVentesContent.RowStyles.Add(New RowStyle(SizeType.Absolute, 48)) ' Actions
            pnlVentesContent.RowStyles.Add(New RowStyle(SizeType.Percent, 100)) ' Grille

            Dim pnlFiltresVentesCard As Panel = CreerCarte()
            pnlFiltresVentesCard.Padding = New Padding(16)

            Dim filtresVentesLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 8,
                .RowCount = 3,
                .AutoSize = True
            }
            filtresVentesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            filtresVentesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120))
            filtresVentesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            filtresVentesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120))
            filtresVentesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            filtresVentesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))
            filtresVentesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 10))
            filtresVentesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            filtresVentesLayout.BackColor = Color.Transparent

            Dim lblPeriodeVentes As New Label() With {.Text = "Période :", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Anchor = AnchorStyles.Left}
            cmbPeriode = New ComboBox() With {
                .Width = 100,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = FontControl,
                .Anchor = AnchorStyles.Left
            }
            cmbPeriode.Items.AddRange(New Object() {"Jour", "Mois", "Annee"})

            Dim lblJourVentes As New Label() With {.Text = "Jour :", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Anchor = AnchorStyles.Left, .Margin = New Padding(10, 0, 0, 0)}
            dtpJour = New DateTimePicker() With {
                .Width = 100,
                .Format = DateTimePickerFormat.Short,
                .Font = FontControl,
                .Anchor = AnchorStyles.Left
            }

            Dim lblMoisVentes As New Label() With {.Text = "Mois :", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Anchor = AnchorStyles.Left, .Margin = New Padding(10, 0, 0, 0)}
            cmbMois = New ComboBox() With {
                .Width = 80,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = FontControl,
                .Anchor = AnchorStyles.Left
            }

            Dim lblAnneeVentes As New Label() With {.Text = "Année :", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Anchor = AnchorStyles.Left, .Margin = New Padding(10, 0, 0, 0)}
            cmbAnnee = New ComboBox() With {
                .Width = 80,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = FontControl,
                .Anchor = AnchorStyles.Left
            }

            lblResumeVentes = New Label() With {
                .Text = "CA: 0 FC | Bénéfice: 0 FC | Quantité: 0",
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .ForeColor = ColorPrimary,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft,
                .Margin = New Padding(0, 10, 0, 0)
            }

            filtresVentesLayout.Controls.Add(lblPeriodeVentes, 0, 0)
            filtresVentesLayout.Controls.Add(cmbPeriode, 1, 0)
            filtresVentesLayout.Controls.Add(lblJourVentes, 2, 0)
            filtresVentesLayout.Controls.Add(dtpJour, 3, 0)
            filtresVentesLayout.Controls.Add(lblMoisVentes, 4, 0)
            filtresVentesLayout.Controls.Add(cmbMois, 5, 0)
            filtresVentesLayout.Controls.Add(lblAnneeVentes, 6, 0)
            filtresVentesLayout.Controls.Add(cmbAnnee, 7, 0)
            filtresVentesLayout.SetColumnSpan(lblResumeVentes, 8)
            filtresVentesLayout.Controls.Add(lblResumeVentes, 0, 1)

            Dim pnlActionsVentes As New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.RightToLeft,
                .WrapContents = False,
                .Padding = New Padding(0, 2, 0, 0),
                .Margin = New Padding(0)
            }
            btnImprimerVentes = New Button() With {
                .Text = "Imprimer A4",
                .Width = 120,
                .Height = 32,
                .BackColor = ColorSecondary,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontButton,
                .Cursor = Cursors.Hand
            }
            btnImprimerVentes.FlatAppearance.BorderSize = 0
            btnExporterPdfVentes = New Button() With {
                .Text = "Exporter PDF",
                .Width = 120,
                .Height = 32,
                .BackColor = ColorAccent,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontButton,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 8, 0)
            }
            btnExporterPdfVentes.FlatAppearance.BorderSize = 0
            pnlActionsVentes.Controls.Add(btnImprimerVentes)
            pnlActionsVentes.Controls.Add(btnExporterPdfVentes)
            filtresVentesLayout.Controls.Add(pnlActionsVentes, 0, 2)
            filtresVentesLayout.SetColumnSpan(pnlActionsVentes, 8)

            pnlFiltresVentesCard.Controls.Add(filtresVentesLayout)

            gridVentes = CreerGrille()
            gridVentes.Dock = DockStyle.Fill
            gridVentes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            gridVentes.ScrollBars = ScrollBars.Both

            pnlVentesContent.Controls.Add(pnlFiltresVentesCard, 0, 0)
            pnlVentesContent.Controls.Add(gridVentes, 0, 2)
            tabVentes.Controls.Add(pnlVentesContent)

            ' --- Onglet 2 : stock ---
            Dim pnlStockContent As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2,
                .BackColor = ColorBg
            }
            pnlStockContent.RowStyles.Add(New RowStyle(SizeType.Absolute, 80)) ' Filtres et résumé
            pnlStockContent.RowStyles.Add(New RowStyle(SizeType.Percent, 100)) ' Grille

            Dim pnlStockCard As Panel = CreerCarte()
            pnlStockCard.Padding = New Padding(16)

            Dim stockLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 4,
                .RowCount = 1
            }
            stockLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 180))
            stockLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120))
            stockLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120))
            stockLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            stockLayout.BackColor = Color.Transparent

            btnRafraichirStock = New Button() With {
                .Text = "Actualiser le stock",
                .Width = 160,
                .Height = 36,
                .BackColor = ColorAccent,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontButton,
                .Cursor = Cursors.Hand,
                .Anchor = AnchorStyles.Left
            }
            btnRafraichirStock.FlatAppearance.BorderSize = 0
            btnRafraichirStock.FlatAppearance.MouseDownBackColor = Color.FromArgb(ColorAccent.R - 20, ColorAccent.G - 20, ColorAccent.B - 20)
            btnRafraichirStock.FlatAppearance.MouseOverBackColor = Color.FromArgb(ColorAccent.R + 20, ColorAccent.G + 20, ColorAccent.B + 20)

            btnImprimerStock = New Button() With {
                .Text = "Imprimer A4",
                .Width = 110,
                .Height = 36,
                .BackColor = ColorSecondary,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontButton,
                .Cursor = Cursors.Hand,
                .Anchor = AnchorStyles.Left
            }
            btnImprimerStock.FlatAppearance.BorderSize = 0

            btnExporterPdfStock = New Button() With {
                .Text = "Exporter PDF",
                .Width = 110,
                .Height = 36,
                .BackColor = ColorAccent,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontButton,
                .Cursor = Cursors.Hand,
                .Anchor = AnchorStyles.Left
            }
            btnExporterPdfStock.FlatAppearance.BorderSize = 0

            lblResumeStock = New Label() With {
                .Text = "Stock global: 0 | Sorties ventes: 0 | Sorties manuelles: 0",
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .ForeColor = ColorPrimary,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft,
                .Margin = New Padding(10, 0, 0, 0)
            }

            stockLayout.Controls.Add(btnRafraichirStock, 0, 0)
            stockLayout.Controls.Add(btnImprimerStock, 1, 0)
            stockLayout.Controls.Add(btnExporterPdfStock, 2, 0)
            stockLayout.Controls.Add(lblResumeStock, 3, 0)
            pnlStockCard.Controls.Add(stockLayout)

            gridStock = CreerGrille()
            gridStock.Dock = DockStyle.Fill
            gridStock.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            gridStock.ScrollBars = ScrollBars.Both

            pnlStockContent.Controls.Add(pnlStockCard, 0, 0)
            pnlStockContent.Controls.Add(gridStock, 0, 1)
            tabStock.Controls.Add(pnlStockContent)

            ' --- Onglet 3 : dépenses ---
            Dim pnlDepensesContent As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2,
                .BackColor = ColorBg
            }
            pnlDepensesContent.RowStyles.Add(New RowStyle(SizeType.Absolute, 120)) ' Filtres et résumé
            pnlDepensesContent.RowStyles.Add(New RowStyle(SizeType.Percent, 100)) ' Grille

            Dim pnlFiltresDepensesCard As Panel = CreerCarte()
            pnlFiltresDepensesCard.Padding = New Padding(16)

            Dim filtresDepensesLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 8,
                .RowCount = 2,
                .AutoSize = True
            }
            filtresDepensesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            filtresDepensesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120))
            filtresDepensesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            filtresDepensesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120))
            filtresDepensesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
            filtresDepensesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 100))
            filtresDepensesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 10))
            filtresDepensesLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            filtresDepensesLayout.BackColor = Color.Transparent

            Dim lblPeriodeDepenses As New Label() With {.Text = "Période :", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Anchor = AnchorStyles.Left}
            cmbPeriodeDepenses = New ComboBox() With {
                .Width = 100,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = FontControl,
                .Anchor = AnchorStyles.Left
            }
            cmbPeriodeDepenses.Items.AddRange(New Object() {"Jour", "Mois", "Annee"})

            Dim lblJourDepenses As New Label() With {.Text = "Jour :", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Anchor = AnchorStyles.Left, .Margin = New Padding(10, 0, 0, 0)}
            dtpJourDepenses = New DateTimePicker() With {
                .Width = 100,
                .Format = DateTimePickerFormat.Short,
                .Font = FontControl,
                .Anchor = AnchorStyles.Left
            }

            Dim lblMoisDepenses As New Label() With {.Text = "Mois :", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Anchor = AnchorStyles.Left, .Margin = New Padding(10, 0, 0, 0)}
            cmbMoisDepenses = New ComboBox() With {
                .Width = 80,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = FontControl,
                .Anchor = AnchorStyles.Left
            }

            Dim lblAnneeDepenses As New Label() With {.Text = "Année :", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Anchor = AnchorStyles.Left, .Margin = New Padding(10, 0, 0, 0)}
            cmbAnneeDepenses = New ComboBox() With {
                .Width = 80,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = FontControl,
                .Anchor = AnchorStyles.Left
            }

            btnRafraichirDepenses = New Button() With {
                .Text = "Actualiser",
                .Width = 120,
                .Height = 36,
                .BackColor = ColorPrimary,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontButton,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(8, 0, 0, 0)
            }
            btnRafraichirDepenses.FlatAppearance.BorderSize = 0
            btnRafraichirDepenses.FlatAppearance.MouseDownBackColor = Color.FromArgb(ColorPrimary.R - 20, ColorPrimary.G - 20, ColorPrimary.B - 20)
            btnRafraichirDepenses.FlatAppearance.MouseOverBackColor = Color.FromArgb(ColorPrimary.R + 20, ColorPrimary.G + 20, ColorPrimary.B + 20)

            lblResumeDepenses = New Label() With {
                .Text = "Total dépenses: 0 FC | Catégories: 0",
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .ForeColor = ColorPrimary,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft,
                .Margin = New Padding(0, 10, 0, 0)
            }

            filtresDepensesLayout.Controls.Add(lblPeriodeDepenses, 0, 0)
            filtresDepensesLayout.Controls.Add(cmbPeriodeDepenses, 1, 0)
            filtresDepensesLayout.Controls.Add(lblJourDepenses, 2, 0)
            filtresDepensesLayout.Controls.Add(dtpJourDepenses, 3, 0)
            filtresDepensesLayout.Controls.Add(lblMoisDepenses, 4, 0)
            filtresDepensesLayout.Controls.Add(cmbMoisDepenses, 5, 0)
            filtresDepensesLayout.Controls.Add(lblAnneeDepenses, 6, 0)
            filtresDepensesLayout.Controls.Add(cmbAnneeDepenses, 7, 0)
            filtresDepensesLayout.SetColumnSpan(lblResumeDepenses, 8)
            filtresDepensesLayout.Controls.Add(lblResumeDepenses, 0, 1)

            pnlFiltresDepensesCard.Controls.Add(filtresDepensesLayout)

            gridDepenses = CreerGrille()
            gridDepenses.Dock = DockStyle.Fill

            pnlDepensesContent.Controls.Add(pnlFiltresDepensesCard, 0, 0)
            pnlDepensesContent.Controls.Add(gridDepenses, 0, 1)
            tabDepenses.Controls.Add(pnlDepensesContent)

            ' Ajout des contrôles au layout principal
            mainLayout.Controls.Add(pnlHeader, 0, 0)
            mainLayout.Controls.Add(pnlTabNavigation, 0, 1)
            mainLayout.Controls.Add(tabs, 0, 2)
            Me.Controls.Add(mainLayout)

            pdocDepenses = New PrintDocument() With {
                .DocumentName = "Depenses",
                .OriginAtMargins = True
            }
            pdocDepenses.DefaultPageSettings.Margins = New Margins(50, 50, 60, 60)
            AddHandler pdocDepenses.PrintPage, AddressOf PdocDepenses_PrintPage
            pdocVentes = New PrintDocument() With {
                .DocumentName = "Ventes",
                .OriginAtMargins = True
            }
            pdocVentes.DefaultPageSettings.Margins = New Margins(35, 35, 50, 50)
            AddHandler pdocVentes.PrintPage, AddressOf PdocVentes_PrintPage
            pdocStock = New PrintDocument() With {
                .DocumentName = "Stock",
                .OriginAtMargins = True
            }
            pdocStock.DefaultPageSettings.Margins = New Margins(35, 35, 50, 50)
            AddHandler pdocStock.PrintPage, AddressOf PdocStock_PrintPage

            AddHandler cmbPeriode.SelectedIndexChanged, AddressOf ActualiserFiltresPeriode
            ' AddHandler dtpJour.ValueChanged, AddressOf ChargerVentes
            ' AddHandler cmbMois.SelectedIndexChanged, AddressOf ChargerVentes
            'AddHandler cmbAnnee.SelectedIndexChanged, AddressOf ChargerVentes
            AddHandler btnRafraichirVentes.Click, Sub() ChargerVentes()

            AddHandler btnRafraichirStock.Click, Sub() ChargerStock()

            AddHandler cmbPeriodeDepenses.SelectedIndexChanged, AddressOf ActualiserFiltresDepensesPeriode
            'AddHandler dtpJourDepenses.ValueChanged, AddressOf ChargerDepenses
            'AddHandler cmbMoisDepenses.SelectedIndexChanged, AddressOf ChargerDepenses
            'AddHandler cmbAnneeDepenses.SelectedIndexChanged, AddressOf ChargerDepenses
            AddHandler btnRafraichirDepenses.Click, Sub() ChargerDepenses()
            AddHandler btnImprimerVentes.Click, AddressOf ImprimerVentes
            AddHandler btnExporterPdfVentes.Click, AddressOf ExporterPdfVentes
            AddHandler btnImprimerStock.Click, AddressOf ImprimerStock
            AddHandler btnExporterPdfStock.Click, AddressOf ExporterPdfStock

            AddHandler tabs.SelectedIndexChanged, AddressOf ChargerOngletActif
            AddHandler Me.Load, AddressOf FormulaireVente_Load

            InitialiserCombos()
            InitialiserCombosDepenses()
            ActualiserFiltresPeriode(Nothing, EventArgs.Empty)
            ActualiserFiltresDepensesPeriode(Nothing, EventArgs.Empty)
            SetSelectedTab(0) ' Sélectionne l'onglet Ventes par défaut
        End Sub

        Private Sub Tabs_GotFocus(sender As Object, e As EventArgs)
            ' Empêche le focus sur les onglets pour un aspect plus propre
            ' Me.ActiveControl = If(tabs.SelectedTab Is tabVentes, tabVentes.Controls(0), If(tabs.SelectedTab Is tabStock, tabStock.Controls(0), tabDepenses.Controls(0)))
        End Sub

        Private Sub FormulaireVente_Load(sender As Object, e As EventArgs)
            ChargerVentes()
            ChargerStock()
            ChargerDepenses()
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

        Private Sub InitialiserCombosDepenses()
            cmbPeriodeDepenses.SelectedIndex = 0
            cmbMoisDepenses.Items.Clear()
            For i As Integer = 1 To 12
                cmbMoisDepenses.Items.Add(i.ToString("00"))
            Next
            cmbMoisDepenses.SelectedItem = Date.Today.Month.ToString("00")

            cmbAnneeDepenses.Items.Clear()
            Dim anneeCourante As Integer = Date.Today.Year
            For i As Integer = anneeCourante - 5 To anneeCourante + 5
                cmbAnneeDepenses.Items.Add(i.ToString())
            Next
            cmbAnneeDepenses.SelectedItem = anneeCourante.ToString()
        End Sub

        Private Sub ActualiserFiltresPeriode(sender As Object, e As EventArgs)
            Dim periode As String = Convert.ToString(cmbPeriode.SelectedItem)
            Dim afficherJour As Boolean = String.Equals(periode, "Jour", StringComparison.OrdinalIgnoreCase)
            Dim afficherMois As Boolean = String.Equals(periode, "Mois", StringComparison.OrdinalIgnoreCase)

            dtpJour.Visible = afficherJour
            cmbMois.Visible = afficherMois OrElse afficherJour
            cmbAnnee.Visible = True

            ' Re-charger les ventes après changement de période
            ChargerVentes()
        End Sub

        Private Sub ActualiserFiltresDepensesPeriode(sender As Object, e As EventArgs)
            Dim periode As String = Convert.ToString(cmbPeriodeDepenses.SelectedItem)
            Dim afficherJour As Boolean = String.Equals(periode, "Jour", StringComparison.OrdinalIgnoreCase)
            Dim afficherMois As Boolean = String.Equals(periode, "Mois", StringComparison.OrdinalIgnoreCase)

            dtpJourDepenses.Visible = afficherJour
            cmbMoisDepenses.Visible = afficherMois OrElse afficherJour
            cmbAnneeDepenses.Visible = True

            ' Re-charger les dépenses après changement de période
            ChargerDepenses()
        End Sub

        Private Sub ChargerOngletActif(sender As Object, e As EventArgs)
            Select Case tabs.SelectedIndex
                Case 0
                    ChargerVentes()
                    SetSelectedTab(0)
                Case 1
                    ChargerStock()
                    SetSelectedTab(1)
                Case 2
                    ChargerDepenses()
                    SetSelectedTab(2)
            End Select
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

                _ventesCourantes = dt
                gridVentes.DataSource = dt
                ConfigurerGrilleVentes()
                MettreAJourResumeVentes(dt)
            Catch ex As Exception
                MessageBox.Show("Impossible de charger les ventes : " & ex.Message, "Ventes", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub



        'Private Sub MettreAJourResumeVentes(dt As DataTable)
        '    Dim totalCA As Decimal = 0
        '    Dim totalBenefice As Decimal = 0
        '    Dim totalQuantite As Integer = 0

        '    For Each row As DataRow In dt.Rows
        '        totalCA += LireDecimal(row, "MontantTotal")
        '        totalBenefice += LireDecimal(row, "BeneficeTotal")
        '        totalQuantite += CInt(LireDecimal(row, "QuantiteTotale"))
        '    Next

        '    lblResumeVentes.Text = $"CA: {totalCA:N0} FC | Bénéfice: {totalBenefice:N0} FC | Quantité: {totalQuantite:N0}"
        'End Sub

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
        Private Sub ChargerStock()
            Try
                Dim dt As DataTable = _service.ListerStockResume()
                _stockCourant = dt
                gridStock.DataSource = dt
                ConfigurerGrilleStock()
                MettreAJourResumeStock(dt)
            Catch ex As Exception
                MessageBox.Show("Impossible de charger le stock : " & ex.Message, "Ventes", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub
        'Private Sub MettreAJourResumeStock(dt As DataTable)
        '    Dim stockGlobal As Decimal = 0D
        '    Dim sortiesVentes As Decimal = 0D
        '    Dim sortiesManuelles As Decimal = 0D

        '    If dt IsNot Nothing Then
        '        For Each row As DataRow In dt.Rows
        '            stockGlobal += LireDecimal(row, "StockActuelPieces")
        '            sortiesVentes += LireDecimal(row, "QuantiteVenduePieces")
        '            sortiesManuelles += LireDecimal(row, "QuantiteSortieManuellePieces")
        '        Next
        '    End If

        '    lblResumeStock.Text = "Stock global: " & FormatageGlobal.FormatNombre(stockGlobal) &
        '        " | Sorties ventes: " & FormatageGlobal.FormatNombre(sortiesVentes) &
        '        " | Sorties manuelles: " & FormatageGlobal.FormatNombre(sortiesManuelles)
        'End Sub
        Private Sub ChargerDepenses()
            Try
                Dim periode As String = Convert.ToString(cmbPeriodeDepenses.SelectedItem)
                Dim dt As DataTable

                Select Case periode
                    Case "Mois"
                        Dim mois As Integer = Convert.ToInt32(cmbMoisDepenses.SelectedItem)
                        Dim annee As Integer = Convert.ToInt32(cmbAnneeDepenses.SelectedItem)
                        dt = _service.ListerDepensesMois(annee, mois)
                    Case "Annee"
                        Dim annee As Integer = Convert.ToInt32(cmbAnneeDepenses.SelectedItem)
                        dt = _service.ListerDepensesAnnee(annee)
                    Case Else
                        dt = _service.ListerDepensesJour(dtpJourDepenses.Value.Date)
                End Select

                _depensesCourantes = dt
                gridDepenses.DataSource = dt
                ConfigurerGrilleDepenses()
                MettreAJourResumeDepenses(dt)
            Catch ex As Exception
                MessageBox.Show("Impossible de charger les dépenses : " & ex.Message, "Ventes", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub MettreAJourResumeDepenses(dt As DataTable)
            Dim totalMontant As Decimal = 0D
            Dim nbCategories As Integer = 0
            Dim nbLignes As Integer = 0

            If dt IsNot Nothing Then
                nbCategories = dt.Rows.Count
                For Each row As DataRow In dt.Rows
                    totalMontant += LireDecimal(row, "MontantTotal")
                    nbLignes += Convert.ToInt32(LireDecimal(row, "NombreDepenses"))
                Next
            End If

            lblResumeDepenses.Text = "Total dépenses: " & FormatageGlobal.FormatMontant(totalMontant) &
                " | Catégories: " & FormatageGlobal.FormatNombre(nbCategories) &
                " | Lignes: " & FormatageGlobal.FormatNombre(nbLignes)
        End Sub
        Private Sub ConfigurerGrilleVentes()
            If gridVentes.Columns.Count = 0 Then Return

            ConfigurerColonne(gridVentes, "DateVente", "Date", 120, "dd/MM/yyyy")
            ConfigurerColonne(gridVentes, "Produit", "Produit", 220)
            ConfigurerColonne(gridVentes, "PrixAchatCarton", "Prix achat carton", 130, "N0")
            ConfigurerColonne(gridVentes, "QuantiteVenduePieces", "Qté vendue (P)", 130, "N0")
            ConfigurerColonne(gridVentes, "MontantGenere", "Montant", 130, "N0")
            ConfigurerColonne(gridVentes, "Benefice", "Bénéfice", 130, "N0")
        End Sub

        Private Sub ConfigurerGrilleStock()
            If gridStock.Columns.Count = 0 Then Return

            If gridStock.Columns.Contains("ProduitId") Then
                gridStock.Columns("ProduitId").Visible = False
            End If
            ConfigurerColonne(gridStock, "Produit", "Produit", 220)
            ConfigurerColonne(gridStock, "ConversionUnite", "Conversion", 90, "N0")
            ConfigurerColonne(gridStock, "StockActuelPieces", "Stock actuel (pièces)", 120, "N0")
            ConfigurerColonne(gridStock, "StockActuelCartons", "Stock actuel (cartons)", 120, "N0")
            ConfigurerColonne(gridStock, "QuantiteVenduePieces", "Ventes (pièces)", 110, "N0")
            ConfigurerColonne(gridStock, "QuantiteVendueCartons", "Ventes (cartons)", 110, "N0")
            ConfigurerColonne(gridStock, "QuantiteSortieManuellePieces", "Sorties manuelles (pièces)", 130, "N0")
            ConfigurerColonne(gridStock, "QuantiteSortieManuelleCartons", "Sorties manuelles (cartons)", 130, "N0")
            ConfigurerColonne(gridStock, "SortiesTotalesPieces", "Sorties totales (pièces)", 120, "N0")
            ConfigurerColonne(gridStock, "SortiesTotalesCartons", "Sorties totales (cartons)", 120, "N0")
            ConfigurerColonne(gridStock, "RestantPieces", "Restant (pièces)", 110, "N0")
            ConfigurerColonne(gridStock, "RestantCartons", "Restant (cartons)", 110, "N0")
        End Sub

        Private Sub ConfigurerGrilleDepenses()
            If gridDepenses.Columns.Count = 0 Then Return

            If gridDepenses.Columns.Contains("Id") Then
                gridDepenses.Columns("Id").Visible = False
            End If
            ConfigurerColonne(gridDepenses, "Categorie", "Catégorie", 220)
            ConfigurerColonne(gridDepenses, "NombreDepenses", "Nombre", 100, "N0")
            ConfigurerColonne(gridDepenses, "MontantTotal", "Montant (FC)", 140, "N0")
            ConfigurerColonne(gridDepenses, "PremiereDate", "Première date", 120, "dd/MM/yyyy")
            ConfigurerColonne(gridDepenses, "DerniereDate", "Dernière date", 120, "dd/MM/yyyy")
        End Sub
        Private Sub ConfigurerColonne(grid As DataGridView, nom As String, titre As String, largeur As Integer, Optional format As String = Nothing)
            If Not grid.Columns.Contains(nom) Then Return

            Dim col As DataGridViewColumn = grid.Columns(nom)
            col.HeaderText = titre
            col.Width = largeur
            If Not String.IsNullOrWhiteSpace(format) Then
                col.DefaultCellStyle.Format = format
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            Else
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            End If
        End Sub

        Private Sub ImprimerVentes(sender As Object, e As EventArgs)
            Dim dt As DataTable = If(_ventesCourantes, TryCast(gridVentes.DataSource, DataTable))
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                MessageBox.Show("Aucune vente à imprimer.", "Ventes", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            _ventePrintRowIndex = 0
            _venteRapportTitre = "RAPPORT DES VENTES"
            Using preview As New PrintPreviewDialog()
                preview.Document = pdocVentes
                preview.Width = 1200
                preview.Height = 800
                preview.StartPosition = FormStartPosition.CenterParent
                preview.ShowDialog(Me)
            End Using
        End Sub

        Private Sub ExporterPdfVentes(sender As Object, e As EventArgs)
            Dim dt As DataTable = If(_ventesCourantes, TryCast(gridVentes.DataSource, DataTable))
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                MessageBox.Show("Aucune vente à exporter.", "Ventes", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Using sfd As New SaveFileDialog()
                sfd.Filter = "PDF (*.pdf)|*.pdf"
                sfd.FileName = "Rapport_Ventes_" & Date.Now.ToString("yyyyMMdd_HHmmss") & ".pdf"
                If sfd.ShowDialog(Me) <> DialogResult.OK Then
                    Return
                End If

                Dim lignes As New List(Of String)()
                lignes.Add("RAPPORT DES VENTES")
                lignes.Add("Période : " & Convert.ToString(cmbPeriode.SelectedItem))
                lignes.Add("Jour : " & dtpJour.Value.ToString("dd/MM/yyyy"))
                lignes.Add("Mois : " & Convert.ToString(cmbMois.SelectedItem))
                lignes.Add("Année : " & Convert.ToString(cmbAnnee.SelectedItem))
                lignes.Add("")
                For Each row As DataRow In dt.Rows
                    Dim dateVente As String = If(dt.Columns.Contains("DateVente") AndAlso Not row.IsNull("DateVente"), Convert.ToDateTime(row("DateVente")).ToString("dd/MM/yyyy HH:mm"), "")
                    Dim produit As String = If(dt.Columns.Contains("Produit") AndAlso Not row.IsNull("Produit"), Convert.ToString(row("Produit")), "")
                    Dim qte As String = If(dt.Columns.Contains("QuantiteVenduePieces") AndAlso Not row.IsNull("QuantiteVenduePieces"), Convert.ToDecimal(row("QuantiteVenduePieces")).ToString("N0"), "0")
                    Dim montant As String = If(dt.Columns.Contains("MontantGenere") AndAlso Not row.IsNull("MontantGenere"), FormatageGlobal.FormatMontant(Convert.ToDecimal(row("MontantGenere"))), "0 FC")
                    Dim benefice As String = If(dt.Columns.Contains("Benefice") AndAlso Not row.IsNull("Benefice"), FormatageGlobal.FormatMontant(Convert.ToDecimal(row("Benefice"))), "0 FC")
                    lignes.Add(dateVente & " | " & produit & " | Qté:" & qte & " | Mt:" & montant & " | B:" & benefice)
                Next
                PdfHelper.GenererPdfSimple(sfd.FileName, "RAPPORT DES VENTES", lignes)
            End Using
        End Sub

        Private Sub ImprimerStock(sender As Object, e As EventArgs)
            Dim dt As DataTable = If(_stockCourant, TryCast(gridStock.DataSource, DataTable))
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                MessageBox.Show("Aucun stock à imprimer.", "Ventes", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            _stockPrintRowIndex = 0
            _stockRapportTitre = "RAPPORT STOCK PRODUITS"
            Using preview As New PrintPreviewDialog()
                preview.Document = pdocStock
                preview.Width = 1200
                preview.Height = 800
                preview.StartPosition = FormStartPosition.CenterParent
                preview.ShowDialog(Me)
            End Using
        End Sub

        Private Sub ExporterPdfStock(sender As Object, e As EventArgs)
            Dim dt As DataTable = If(_stockCourant, TryCast(gridStock.DataSource, DataTable))
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                MessageBox.Show("Aucun stock à exporter.", "Ventes", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Using sfd As New SaveFileDialog()
                sfd.Filter = "PDF (*.pdf)|*.pdf"
                sfd.FileName = "Rapport_Stock_" & Date.Now.ToString("yyyyMMdd_HHmmss") & ".pdf"
                If sfd.ShowDialog(Me) <> DialogResult.OK Then
                    Return
                End If

                Dim lignes As New List(Of String)()
                lignes.Add("RAPPORT STOCK")
                lignes.Add("")
                For Each row As DataRow In dt.Rows
                    Dim produit As String = If(dt.Columns.Contains("Produit") AndAlso Not row.IsNull("Produit"), Convert.ToString(row("Produit")), "")
                    Dim stockPieces As String = If(dt.Columns.Contains("StockActuelPieces") AndAlso Not row.IsNull("StockActuelPieces"), Convert.ToDecimal(row("StockActuelPieces")).ToString("N0"), "0")
                    Dim stockCartons As String = If(dt.Columns.Contains("StockActuelCartons") AndAlso Not row.IsNull("StockActuelCartons"), Convert.ToDecimal(row("StockActuelCartons")).ToString("N0"), "0")
                    Dim ventes As String = If(dt.Columns.Contains("QuantiteVenduePieces") AndAlso Not row.IsNull("QuantiteVenduePieces"), Convert.ToDecimal(row("QuantiteVenduePieces")).ToString("N0"), "0")
                    Dim sorties As String = If(dt.Columns.Contains("QuantiteSortieManuellePieces") AndAlso Not row.IsNull("QuantiteSortieManuellePieces"), Convert.ToDecimal(row("QuantiteSortieManuellePieces")).ToString("N0"), "0")
                    Dim restant As String = If(dt.Columns.Contains("RestantPieces") AndAlso Not row.IsNull("RestantPieces"), Convert.ToDecimal(row("RestantPieces")).ToString("N0"), "0")
                    lignes.Add(produit & " | Stock:" & stockPieces & "P/" & stockCartons & "C | Ventes:" & ventes & " | Sorties:" & sorties & " | Restant:" & restant)
                Next
                PdfHelper.GenererPdfSimple(sfd.FileName, "RAPPORT STOCK", lignes)
            End Using
        End Sub

        Private Sub PdocVentes_PrintPage(sender As Object, e As PrintPageEventArgs)
            Dim data As DataTable = If(_ventesCourantes, TryCast(gridVentes.DataSource, DataTable))
            If data Is Nothing OrElse data.Rows.Count = 0 Then
                e.HasMorePages = False
                Return
            End If

            Dim left As Integer = e.MarginBounds.Left
            Dim top As Integer = e.MarginBounds.Top
            Dim pageWidth As Integer = e.MarginBounds.Width
            Dim y As Integer = top

            Using titreFont As New Font("Segoe UI", 14.0F, FontStyle.Bold),
                  sousTitreFont As New Font("Segoe UI", 9.0F, FontStyle.Regular),
                  enteteFont As New Font("Segoe UI", 9.0F, FontStyle.Bold),
                  ligneFont As New Font("Segoe UI", 9.0F, FontStyle.Regular)

                e.Graphics.DrawString("Rapport des ventes", titreFont, Brushes.Black, left, y)
                y += 24
                e.Graphics.DrawString("Période : " & Convert.ToString(cmbPeriode.SelectedItem) & " | " & lblResumeVentes.Text, sousTitreFont, Brushes.Black, left, y)
                y += 26

                Dim colonnes As String() = {"DateVente", "Produit", "PrixAchatCarton", "QuantiteVenduePieces", "MontantGenere", "Benefice"}
                Dim largeurs As Integer() = {
                    CInt(pageWidth * 0.16),
                    CInt(pageWidth * 0.28),
                    CInt(pageWidth * 0.14),
                    CInt(pageWidth * 0.14),
                    CInt(pageWidth * 0.14),
                    CInt(pageWidth * 0.14)
                }
                Dim titres As String() = {"Date", "Produit", "Prix achat", "Qté", "Montant", "Bénéfice"}
                Dim hauteurEntete As Integer = 24
                Dim hauteurLigne As Integer = 22

                Dim x As Integer = left
                For i As Integer = 0 To titres.Length - 1
                    e.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(240, 240, 240)), x, y, largeurs(i), hauteurEntete)
                    e.Graphics.DrawRectangle(Pens.Gray, x, y, largeurs(i), hauteurEntete)
                    e.Graphics.DrawString(titres(i), enteteFont, Brushes.Black, New RectangleF(x + 4, y + 4, largeurs(i) - 8, hauteurEntete - 8))
                    x += largeurs(i)
                Next
                y += hauteurEntete

                While _ventePrintRowIndex < data.Rows.Count
                    Dim row As DataRow = data.Rows(_ventePrintRowIndex)
                    If y + hauteurLigne > e.MarginBounds.Bottom Then
                        e.HasMorePages = True
                        Return
                    End If

                    x = left
                    For i As Integer = 0 To colonnes.Length - 1
                        Dim valeur As String = ""
                        If Not row.IsNull(colonnes(i)) Then
                            Select Case colonnes(i)
                                Case "DateVente"
                                    valeur = Convert.ToDateTime(row(colonnes(i))).ToString("dd/MM/yyyy HH:mm")
                                Case "Produit"
                                    valeur = Convert.ToString(row(colonnes(i)))
                                Case Else
                                    valeur = Convert.ToDecimal(row(colonnes(i))).ToString("N0")
                            End Select
                        End If
                        e.Graphics.DrawRectangle(Pens.Gray, x, y, largeurs(i), hauteurLigne)
                        e.Graphics.DrawString(valeur, ligneFont, Brushes.Black, New RectangleF(x + 4, y + 3, largeurs(i) - 8, hauteurLigne - 6))
                        x += largeurs(i)
                    Next

                    y += hauteurLigne
                    _ventePrintRowIndex += 1
                End While
            End Using

            _ventePrintRowIndex = 0
            e.HasMorePages = False
        End Sub

        Private Sub PdocStock_PrintPage(sender As Object, e As PrintPageEventArgs)
            Dim data As DataTable = If(_stockCourant, TryCast(gridStock.DataSource, DataTable))
            If data Is Nothing OrElse data.Rows.Count = 0 Then
                e.HasMorePages = False
                Return
            End If

            Dim left As Integer = e.MarginBounds.Left
            Dim top As Integer = e.MarginBounds.Top
            Dim pageWidth As Integer = e.MarginBounds.Width
            Dim y As Integer = top

            Using titreFont As New Font("Segoe UI", 14.0F, FontStyle.Bold),
                  sousTitreFont As New Font("Segoe UI", 9.0F, FontStyle.Regular),
                  enteteFont As New Font("Segoe UI", 9.0F, FontStyle.Bold),
                  ligneFont As New Font("Segoe UI", 9.0F, FontStyle.Regular)

                e.Graphics.DrawString("Rapport stock produits", titreFont, Brushes.Black, left, y)
                y += 24
                e.Graphics.DrawString("Synthèse actuelle du stock | " & lblResumeStock.Text, sousTitreFont, Brushes.Black, left, y)
                y += 26

                Dim colonnes As String() = {"Produit", "StockActuelPieces", "StockActuelCartons", "QuantiteVenduePieces", "QuantiteSortieManuellePieces", "RestantPieces"}
                Dim largeurs As Integer() = {
                    CInt(pageWidth * 0.32),
                    CInt(pageWidth * 0.12),
                    CInt(pageWidth * 0.12),
                    CInt(pageWidth * 0.14),
                    CInt(pageWidth * 0.14),
                    CInt(pageWidth * 0.16)
                }
                Dim titres As String() = {"Produit", "Stock P", "Stock C", "Ventes P", "Sorties P", "Restant P"}
                Dim hauteurEntete As Integer = 24
                Dim hauteurLigne As Integer = 22

                Dim x As Integer = left
                For i As Integer = 0 To titres.Length - 1
                    e.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(240, 240, 240)), x, y, largeurs(i), hauteurEntete)
                    e.Graphics.DrawRectangle(Pens.Gray, x, y, largeurs(i), hauteurEntete)
                    e.Graphics.DrawString(titres(i), enteteFont, Brushes.Black, New RectangleF(x + 4, y + 4, largeurs(i) - 8, hauteurEntete - 8))
                    x += largeurs(i)
                Next
                y += hauteurEntete

                While _stockPrintRowIndex < data.Rows.Count
                    Dim row As DataRow = data.Rows(_stockPrintRowIndex)
                    If y + hauteurLigne > e.MarginBounds.Bottom Then
                        e.HasMorePages = True
                        Return
                    End If

                    x = left
                    For i As Integer = 0 To colonnes.Length - 1
                        Dim valeur As String = ""
                        If Not row.IsNull(colonnes(i)) Then
                            If String.Equals(colonnes(i), "Produit", StringComparison.OrdinalIgnoreCase) Then
                                valeur = Convert.ToString(row(colonnes(i)))
                            Else
                                valeur = Convert.ToDecimal(row(colonnes(i))).ToString("N0")
                            End If
                        End If
                        e.Graphics.DrawRectangle(Pens.Gray, x, y, largeurs(i), hauteurLigne)
                        e.Graphics.DrawString(valeur, ligneFont, Brushes.Black, New RectangleF(x + 4, y + 3, largeurs(i) - 8, hauteurLigne - 6))
                        x += largeurs(i)
                    Next

                    y += hauteurLigne
                    _stockPrintRowIndex += 1
                End While
            End Using

            _stockPrintRowIndex = 0
            e.HasMorePages = False
        End Sub

        Private Sub ImprimerDepenses(sender As Object, e As EventArgs)
            If _depensesCourantes Is Nothing OrElse _depensesCourantes.Rows.Count = 0 Then
                MessageBox.Show("Aucune dépense à imprimer.", "Ventes", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            _depensePrintRowIndex = 0
            Using preview As New PrintPreviewDialog()
                preview.Document = pdocDepenses
                preview.Width = 1200
                preview.Height = 800
                preview.StartPosition = FormStartPosition.CenterParent
                preview.ShowDialog(Me)
            End Using
        End Sub

        Private Sub PdocDepenses_PrintPage(sender As Object, e As PrintPageEventArgs)
            Dim data As DataTable = _depensesCourantes
            If data Is Nothing OrElse data.Rows.Count = 0 Then
                e.HasMorePages = False
                Return
            End If

            Dim left As Integer = e.MarginBounds.Left
            Dim top As Integer = e.MarginBounds.Top
            Dim pageWidth As Integer = e.MarginBounds.Width
            Dim y As Integer = top

            Using titreFont As New Font("Segoe UI", 14.0F, FontStyle.Bold),
                  sousTitreFont As New Font("Segoe UI", 9.0F, FontStyle.Regular),
                  enteteFont As New Font("Segoe UI", 9.0F, FontStyle.Bold),
                  ligneFont As New Font("Segoe UI", 9.0F, FontStyle.Regular)

                e.Graphics.DrawString("Liste des dépenses", titreFont, Brushes.Black, left, y)
                y += 26
                e.Graphics.DrawString(lblResumeDepenses.Text, sousTitreFont, Brushes.Black, left, y)
                y += 28

                Dim colonnes As String() = {"Categorie", "NombreDepenses", "MontantTotal", "PremiereDate", "DerniereDate"}
                Dim largeurs As Integer() = {
                    CInt(pageWidth * 0.38),
                    CInt(pageWidth * 0.12),
                    CInt(pageWidth * 0.18),
                    CInt(pageWidth * 0.16),
                    CInt(pageWidth * 0.16)
                }
                Dim titres As String() = {"Catégorie", "Nombre", "Montant (FC)", "Première", "Dernière"}
                Dim hauteurEntete As Integer = 24
                Dim hauteurLigne As Integer = 22

                Dim x As Integer = left
                For i As Integer = 0 To titres.Length - 1
                    e.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(240, 240, 240)), x, y, largeurs(i), hauteurEntete)
                    e.Graphics.DrawRectangle(Pens.Gray, x, y, largeurs(i), hauteurEntete)
                    e.Graphics.DrawString(titres(i), enteteFont, Brushes.Black, New RectangleF(x + 4, y + 4, largeurs(i) - 8, hauteurEntete - 8))
                    x += largeurs(i)
                Next
                y += hauteurEntete

                While _depensePrintRowIndex < data.Rows.Count
                    Dim row As DataRow = data.Rows(_depensePrintRowIndex)
                    If y + hauteurLigne > e.MarginBounds.Bottom Then
                        e.HasMorePages = True
                        Return
                    End If

                    x = left
                    For i As Integer = 0 To colonnes.Length - 1
                        Dim valeur As String = ""
                        If Not row.IsNull(colonnes(i)) Then
                            If String.Equals(colonnes(i), "MontantTotal", StringComparison.OrdinalIgnoreCase) Then
                                valeur = FormatageGlobal.FormatMontant(Convert.ToDecimal(row(colonnes(i))))
                            ElseIf String.Equals(colonnes(i), "NombreDepenses", StringComparison.OrdinalIgnoreCase) Then
                                valeur = Convert.ToDecimal(row(colonnes(i))).ToString("N0")
                            ElseIf String.Equals(colonnes(i), "PremiereDate", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(colonnes(i), "DerniereDate", StringComparison.OrdinalIgnoreCase) Then
                                valeur = Convert.ToDateTime(row(colonnes(i))).ToString("dd/MM/yyyy")
                            Else
                                valeur = Convert.ToString(row(colonnes(i)))
                            End If
                        End If

                        e.Graphics.DrawRectangle(Pens.Gray, x, y, largeurs(i), hauteurLigne)
                        e.Graphics.DrawString(valeur, ligneFont, Brushes.Black, New RectangleF(x + 4, y + 3, largeurs(i) - 8, hauteurLigne - 6))
                        x += largeurs(i)
                    Next

                    y += hauteurLigne
                    _depensePrintRowIndex += 1
                End While
            End Using

            _depensePrintRowIndex = 0
            e.HasMorePages = False
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

        Private Sub SetSelectedTab(index As Integer)
            tabs.SelectedIndex = index
            ' Mettre à jour le style des boutons d'onglet
            btnTabVentes.BackColor = If(index = 0, ColorTabActive, ColorTabInactive)
            btnTabVentes.ForeColor = If(index = 0, ColorPrimary, ColorTextSecondary)
            btnTabVentes.FlatAppearance.BorderSize = 0
            RemoveHandler btnTabVentes.Paint, AddressOf TabButton_Paint ' Supprimer l'ancien handler
            AddHandler btnTabVentes.Paint, AddressOf TabButton_Paint ' Ajouter le nouveau handler

            btnTabStock.BackColor = If(index = 1, ColorTabActive, ColorTabInactive)
            btnTabStock.ForeColor = If(index = 1, ColorPrimary, ColorTextSecondary)
            btnTabStock.FlatAppearance.BorderSize = 0
            RemoveHandler btnTabStock.Paint, AddressOf TabButton_Paint
            AddHandler btnTabStock.Paint, AddressOf TabButton_Paint

            btnTabDepenses.BackColor = If(index = 2, ColorTabActive, ColorTabInactive)
            btnTabDepenses.ForeColor = If(index = 2, ColorPrimary, ColorTextSecondary)
            btnTabDepenses.FlatAppearance.BorderSize = 0
            RemoveHandler btnTabDepenses.Paint, AddressOf TabButton_Paint
            AddHandler btnTabDepenses.Paint, AddressOf TabButton_Paint

            ' Invalider pour forcer le redessin des bordures
            btnTabVentes.Invalidate()
            btnTabStock.Invalidate()
            btnTabDepenses.Invalidate()
        End Sub

        Private Sub TabButton_Paint(sender As Object, e As PaintEventArgs)
            Dim btn As Button = DirectCast(sender, Button)
            If btn.BackColor = ColorTabActive Then ' Si l'onglet est actif
                Using p As New Pen(ColorPrimary, 2)
                    e.Graphics.DrawLine(p, 0, btn.Height - 1, btn.Width, btn.Height - 1)
                End Using
            End If
        End Sub

        Private Shared Function LireDecimal(row As DataRow, colonne As String) As Decimal
            If row Is Nothing OrElse row.Table Is Nothing OrElse Not row.Table.Columns.Contains(colonne) OrElse row.IsNull(colonne) Then
                Return 0D
            End If
            Return Convert.ToDecimal(row(colonne))
        End Function

    End Class


End Namespace
