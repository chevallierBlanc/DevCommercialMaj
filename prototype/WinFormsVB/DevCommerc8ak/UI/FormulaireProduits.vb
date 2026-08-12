Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Printing
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class FormulaireProduits
        Inherits Form

        ' --- Constantes de Design ---
        'Private ReadOnly ColorPrimary As Color = Color.FromArgb(63, 81, 181) ' Indigo
        'Private ReadOnly ColorSecondary As Color = Color.FromArgb(48, 63, 159)
        'Private ReadOnly ColorAccent As Color = Color.FromArgb(255, 64, 129)
        Private ReadOnly ColorBackground As Color = Color.FromArgb(245, 247, 250)
        Private ReadOnly ColorCard As Color = Color.White
        Private ReadOnly ColorText As Color = Color.FromArgb(33, 33, 33)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(117, 117, 117)
        Private ReadOnly ColorBorder As Color = Color.FromArgb(230, 230, 230)
        ' Private ReadOnly FontTitle As New Font("Segoe UI Semibold", 18.0F)
        Private ReadOnly FontSubTitle As New Font("Segoe UI", 10.0F)
        Private ReadOnly FontLabel As New Font("Segoe UI Semibold", 9.0F)
        Private ReadOnly FontControl As New Font("Segoe UI", 9.5F)

        Private Const TaillePageProduits As Integer = 14


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

        ' --- Composants UI (Noms conservés) ---
        Private ReadOnly txtRecherche As TextBox
        Private ReadOnly btnNouveau As Button
        Private ReadOnly btnEnregistrer As Button
        Private ReadOnly btnSupprimer As Button
        Private ReadOnly btnActualiser As Button
        Private ReadOnly btnImprimerProduits As Button
        Private ReadOnly btnImprimerHistorique As Button
        Private ReadOnly btnTypesPersonnalises As Button

        Private ReadOnly txtLibelle As TextBox
        Private ReadOnly txtCodeBarres As TextBox
        Private ReadOnly cmbCategorie As ComboBox
        Private ReadOnly chkActif As CheckBox

        Private ReadOnly cmbUnitePrincipale As ComboBox
        Private ReadOnly cmbUniteSecondaire As ComboBox
        Private ReadOnly txtConversion As TextBox
        Private ReadOnly cmbTypeGestionStock As ComboBox
        Private ReadOnly cmbUniteMesureStock As ComboBox
        Private ReadOnly txtContenuUnitePrincipale As TextBox
        Private ReadOnly txtContenuUniteSecondaire As TextBox
        Private _typeGestionStockOriginal As String = "UNITE"
        Private _conversionUniteOriginale As Decimal = 1D
        Private _quantiteBaseOriginale As Decimal = 0D
        Private _unitePrincipaleOriginale As String = String.Empty
        Private _uniteSecondaireOriginale As String = String.Empty

        Private ReadOnly txtPrixUnite As TextBox
        Private ReadOnly txtPrixAchat As TextBox
        Private ReadOnly txtPrixDemi As TextBox
        Private ReadOnly txtPrixQuart As TextBox
        Private ReadOnly txtPrixDouzaine As TextBox
        Private ReadOnly txtPrixGros As TextBox
        Private ReadOnly txtPrixSpecial As TextBox
        Private ReadOnly txtCoeffGros As TextBox
        Private ReadOnly btnCalculerPrix As Button

        Private ReadOnly txtQuantite As TextBox
        Private ReadOnly txtSeuil As TextBox
        Private ReadOnly txtMarge As TextBox
        Private ReadOnly dtpExpiration As DateTimePicker

        Private ReadOnly chkVenteUnite As CheckBox
        Private ReadOnly chkVenteDemi As CheckBox
        Private ReadOnly chkVenteQuart As CheckBox
        Private ReadOnly chkVenteDouzaine As CheckBox
        Private ReadOnly chkVenteGros As CheckBox

        Private ReadOnly tabs As TabControl
        Private ReadOnly tabProduits As TabPage
        Private ReadOnly tabHistorique As TabPage
        Private ReadOnly tabDashboard As TabPage
        Private ReadOnly panelHero As Panel
        Private ReadOnly lblHeroTitre As Label
        Private ReadOnly lblHeroSousTitre As Label
        Private ReadOnly grid As DataGridView
        Private ReadOnly gridTypesPersonnalises As DataGridView
        Private ReadOnly gridHistorique As DataGridView
        Private ReadOnly gridProduitVedette As DataGridView
        Private ReadOnly lblPagination As Label
        Private ReadOnly btnPagePrecedente As Button
        Private ReadOnly btnPageSuivante As Button
        Private ReadOnly cmbAnneeDashboard As ComboBox
        Private ReadOnly cmbProduitHistorique As ComboBox
        Private ReadOnly dtpHistoriqueDu As DateTimePicker
        Private ReadOnly dtpHistoriqueAu As DateTimePicker
        Private ReadOnly chkFiltreDate As CheckBox
        Private ReadOnly chartTopProduits As Chart
        Private ReadOnly chartCategories As Chart
        Private ReadOnly gridLegendeCategories As DataGridView
        Private ReadOnly lblKpiProduitRentable As Label
        Private ReadOnly lblKpiTotalRecettes As Label
        Private ReadOnly lblKpiNombreProduits As Label
        Private ReadOnly lblKpiFaibleRotation As Label
        Private ReadOnly lblKpiDormants As Label

        ' --- Variables de Logique ---
        Private _produitsTable As DataTable
        Private _historiqueTable As DataTable
        Private _produitsView As DataView
        Private _categoriesTable As DataTable
        Private _produitId As Integer
        Private _pageCourante As Integer
        Private _isRefreshingFromEvent As Boolean

        Public Sub New()
            ' Configuration de base
            Me.Text = "Gestion du Catalogue Produits"
            Me.Width = 1350
            Me.Height = 900
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = ColorBackground
            Me.DoubleBuffered = True
            Me.AutoScaleMode = AutoScaleMode.Dpi
            Me.AutoScroll = True
            Me.MinimumSize = New Size(1080, 720)
            _pageCourante = 1

            ' --- Header / Hero Section ---
            panelHero = New Panel() With {.Dock = DockStyle.Top, .Height = 90, .BackColor = ColorPrimary}
            lblHeroTitre = New Label() With {.Text = "Catalogue Produits & Intelligence Tarifaire", .Left = 25, .Top = 18, .AutoSize = True, .Font = FontTitle, .ForeColor = Color.White}
            lblHeroSousTitre = New Label() With {.Text = "Édition des prix, historique détaillé et lecture décisionnelle du portefeuille produit.", .Left = 27, .Top = 54, .AutoSize = True, .Font = FontSubTitle, .ForeColor = Color.FromArgb(210, 210, 255)}
            panelHero.Controls.Add(lblHeroTitre)
            panelHero.Controls.Add(lblHeroSousTitre)

            ' --- TabControl ---
            tabs = New TabControl() With {.Dock = DockStyle.Fill, .Padding = New Point(15, 8)}
            tabProduits = New TabPage("Gestion Produits") With {.BackColor = ColorBackground, .AutoScroll = True}
            tabHistorique = New TabPage("Historique des Prix") With {.BackColor = ColorBackground, .AutoScroll = True}
            tabDashboard = New TabPage("Analyses & Dashboard") With {.BackColor = ColorBackground, .AutoScroll = True}
            tabs.TabPages.AddRange({tabProduits, tabHistorique, tabDashboard})

            ' --- TAB PRODUITS : STRUCTURE ---
            Dim mainTableProduits As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 5, .Padding = New Padding(10)}
            mainTableProduits.RowStyles.Add(New RowStyle(SizeType.Absolute, 60))  ' Barre Recherche/Actions
            mainTableProduits.RowStyles.Add(New RowStyle(SizeType.Absolute, 285)) ' Cartes Edition
            mainTableProduits.RowStyles.Add(New RowStyle(SizeType.Absolute, 130)) ' Types personnalisés
            mainTableProduits.RowStyles.Add(New RowStyle(SizeType.Percent, 100))  ' Grille
            mainTableProduits.RowStyles.Add(New RowStyle(SizeType.Absolute, 40))  ' Pagination

            ' 1. Barre de Recherche et Actions
            Dim flowHeader As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.LeftToRight, .Padding = New Padding(0, 10, 0, 0), .WrapContents = True, .AutoScroll = True}
            txtRecherche = New TextBox() With {.Width = 250, .Font = FontControl, .BorderStyle = BorderStyle.FixedSingle, .Margin = New Padding(0, 5, 20, 0)}
            btnNouveau = CreateStyledButton("Nouveau", ColorPrimary)
            btnEnregistrer = CreateStyledButton("Enregistrer", Color.ForestGreen)
            btnSupprimer = CreateStyledButton("Supprimer", Color.Crimson)
            btnActualiser = CreateStyledButton("Actualiser", Color.Gray)
            btnImprimerProduits = CreateStyledButton("Imprimer Liste", Color.SlateGray)
            btnTypesPersonnalises = CreateStyledButton("Types personnalisés", ColorSecondary, 160, 35)

            flowHeader.Controls.Add(New Label() With {.Text = "Recherche :", .Font = FontLabel, .ForeColor = ColorTextSecondary, .Margin = New Padding(0, 10, 5, 0), .AutoSize = True})
            flowHeader.Controls.Add(txtRecherche)
            flowHeader.Controls.AddRange({btnNouveau, btnEnregistrer, btnSupprimer, btnActualiser, btnImprimerProduits, btnTypesPersonnalises})

            ' 2. Cartes d'Édition (Layout Flexible)
            Dim tableEdition As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 4, .RowCount = 1}
            tableEdition.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
            tableEdition.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
            tableEdition.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
            tableEdition.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))

            ' Carte 1: Infos de base
            Dim cardInfos As Panel = CreateCard("Fiche Produit")
            txtLibelle = CreateField(cardInfos, "Désignation", 20, 45, 280)
            txtCodeBarres = CreateField(cardInfos, "Code Barres / QR", 20, 105, 160)
            cmbCategorie = CreateComboField(cardInfos, "Catégorie", 190, 105, 110)
            chkActif = New CheckBox() With {.Text = "Actif", .Left = 260, .Top = 108, .Font = FontControl, .AutoSize = True}
            cardInfos.Controls.Add(chkActif)

            ' Carte 2: Unités
            Dim cardUnites As Panel = CreateCard("Unités & Conversion")
            cmbUnitePrincipale = CreateComboField(cardUnites, "Unité Principale", 20, 45, 130)
            cmbUniteSecondaire = CreateComboField(cardUnites, "Unité Secondaire", 170, 45, 130)
            txtConversion = CreateField(cardUnites, "Taux Conv.", 20, 105, 110)
            cmbTypeGestionStock = CreateComboField(cardUnites, "Mode stock", 150, 105, 90)
            cmbUniteMesureStock = CreateComboField(cardUnites, "Mesure", 250, 105, 60)
            txtContenuUnitePrincipale = CreateField(cardUnites, "Contenu princ.", 20, 170, 120)
            txtContenuUniteSecondaire = CreateField(cardUnites, "Contenu second.", 170, 170, 120)
            cmbUnitePrincipale.Items.AddRange({"Carton", "Sac", "Paquet", "Farde", "Plateau", "Seau", "Bidon", "Bouteille", "Boîte", "Pièce"})
            cmbUniteSecondaire.Items.AddRange({"Pièce", "Sachet", "Paquet", "Farde", "Bidon", "Bouteille", "Boîte"})
            cmbTypeGestionStock.Items.AddRange({"UNITE", "MESURE"})
            cmbUniteMesureStock.Items.AddRange({"KG", "G", "L", "ML", "M", "CM"})
            cmbTypeGestionStock.SelectedItem = "UNITE"
            cmbUniteMesureStock.SelectedItem = "KG"

            ' Carte 3: Prix
            Dim cardPrix As Panel = CreateCard("Tarification")
            txtPrixAchat = CreateField(cardPrix, "Prix Achat", 20, 45, 100)
            txtCoeffGros = CreateField(cardPrix, "Coeff.", 130, 45, 60)
            btnCalculerPrix = CreateStyledButton("Calculer", ColorSecondary, 80, 30)
            btnCalculerPrix.Left = 200 : btnCalculerPrix.Top = 42
            cardPrix.Controls.Add(btnCalculerPrix)

            txtPrixGros = CreateField(cardPrix, "Prix Gros", 20, 105, 100)
            txtPrixUnite = CreateField(cardPrix, "Prix Détail", 130, 105, 100)
            txtPrixDemi = CreateField(cardPrix, "Demi", 20, 165, 80)
            txtPrixQuart = CreateField(cardPrix, "Quart", 110, 165, 80)
            txtPrixDouzaine = CreateField(cardPrix, "Douzaine", 200, 165, 80)
            txtPrixSpecial = CreateField(cardPrix, "Spécial", 20, 210, 100)

            ' Carte 4: Stock & Options
            Dim cardStock As Panel = CreateCard("Stock & Options")
            txtQuantite = CreateField(cardStock, "Stock Actuel", 20, 45, 100) : txtQuantite.ReadOnly = True
            txtSeuil = CreateField(cardStock, "Seuil Alerte", 130, 45, 100)
            txtMarge = CreateField(cardStock, "Marge %", 20, 105, 100) : txtMarge.ReadOnly = True
            dtpExpiration = New DateTimePicker() With {.Left = 130, .Top = 105, .Width = 150, .Format = DateTimePickerFormat.Short, .Font = FontControl}
            cardStock.Controls.Add(New Label() With {.Text = "Expiration", .Left = 130, .Top = 85, .Font = FontLabel, .ForeColor = ColorTextSecondary, .AutoSize = True})
            cardStock.Controls.Add(dtpExpiration)

            Dim flowOptions As New FlowLayoutPanel() With {.Left = 20, .Top = 160, .Width = 280, .Height = 80}
            chkVenteGros = CreateOption(flowOptions, "Vente Gros")
            chkVenteUnite = CreateOption(flowOptions, "Vente Détail")
            chkVenteDemi = CreateOption(flowOptions, "Vente Demi")
            chkVenteQuart = CreateOption(flowOptions, "Vente Quart")
            chkVenteDouzaine = CreateOption(flowOptions, "Vente Douzaine")
            cardStock.Controls.Add(flowOptions)

            tableEdition.Controls.Add(cardInfos, 0, 0)
            tableEdition.Controls.Add(cardUnites, 1, 0)
            tableEdition.Controls.Add(cardPrix, 2, 0)
            tableEdition.Controls.Add(cardStock, 3, 0)

            ' 3. Grille
            Dim cardTypesPersonnalises As Panel = CreateCard("Types de vente personnalisés")
            gridTypesPersonnalises = CreateStyledGrid()
            gridTypesPersonnalises.AutoGenerateColumns = False
            gridTypesPersonnalises.Dock = DockStyle.Fill
            cardTypesPersonnalises.Controls.Add(gridTypesPersonnalises)

            ' 4. Grille
            grid = CreateStyledGrid()
            grid.AutoGenerateColumns = False

            ' 5. Pagination
            Dim flowPager As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft}
            btnPageSuivante = CreateStyledButton(">", Color.LightGray, 40, 30)
            lblPagination = New Label() With {.Text = "Page 1/1", .Font = FontLabel, .Margin = New Padding(10, 8, 10, 0), .AutoSize = True}
            btnPagePrecedente = CreateStyledButton("<", Color.LightGray, 40, 30)
            flowPager.Controls.AddRange({btnPagePrecedente, lblPagination, btnPageSuivante})

            mainTableProduits.Controls.Add(flowHeader, 0, 0)
            mainTableProduits.Controls.Add(tableEdition, 0, 1)
            mainTableProduits.Controls.Add(cardTypesPersonnalises, 0, 2)
            mainTableProduits.Controls.Add(grid, 0, 3)
            mainTableProduits.Controls.Add(flowPager, 0, 4)
            tabProduits.Controls.Add(mainTableProduits)

            ' --- TAB HISTORIQUE : STRUCTURE ---
            Dim mainTableHist As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2, .Padding = New Padding(20)}
            mainTableHist.RowStyles.Add(New RowStyle(SizeType.Absolute, 80))
            mainTableHist.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim cardHistFiltres As Panel = CreateCard("Filtres Historique")
            cardHistFiltres.Height = 70
            cmbProduitHistorique = CreateComboField(cardHistFiltres, "Produit", 20, 30, 250)
            chkFiltreDate = New CheckBox() With {.Text = "Filtrer par date", .Left = 290, .Top = 32, .Font = FontControl, .AutoSize = True}
            dtpHistoriqueDu = New DateTimePicker() With {.Left = 420, .Top = 30, .Width = 130, .Format = DateTimePickerFormat.Short}
            dtpHistoriqueAu = New DateTimePicker() With {.Left = 560, .Top = 30, .Width = 130, .Format = DateTimePickerFormat.Short}
            btnImprimerHistorique = CreateStyledButton("Imprimer Rapport", ColorSecondary, 150, 35)
            btnImprimerHistorique.Left = 710 : btnImprimerHistorique.Top = 25
            cardHistFiltres.Controls.AddRange({chkFiltreDate, dtpHistoriqueDu, dtpHistoriqueAu, btnImprimerHistorique})

            gridHistorique = CreateStyledGrid()
            gridHistorique.AutoGenerateColumns = False
            mainTableHist.Controls.Add(cardHistFiltres, 0, 0)
            mainTableHist.Controls.Add(gridHistorique, 0, 1)
            tabHistorique.Controls.Add(mainTableHist)

            ' --- TAB DASHBOARD : STRUCTURE ---
            Dim mainTableDash As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 3, .Padding = New Padding(20)}
            mainTableDash.RowStyles.Add(New RowStyle(SizeType.Absolute, 60))
            mainTableDash.RowStyles.Add(New RowStyle(SizeType.Absolute, 120))
            mainTableDash.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            cmbAnneeDashboard = New ComboBox() With {.Width = 120, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl}
            Dim flowDashTop As New FlowLayoutPanel() With {.Dock = DockStyle.Fill}
            flowDashTop.Controls.Add(New Label() With {.Text = "Année d'analyse :", .Font = FontLabel, .Margin = New Padding(0, 8, 10, 0), .AutoSize = True})
            flowDashTop.Controls.Add(cmbAnneeDashboard)

            Dim tableKpi As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 5, .RowCount = 1}
            tableKpi.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))
            tableKpi.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))
            tableKpi.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))
            tableKpi.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))
            tableKpi.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))

            lblKpiProduitRentable = CreateKpiCard(tableKpi, "Produit Rentable", 0)
            lblKpiTotalRecettes = CreateKpiCard(tableKpi, "Total Recettes", 1)
            lblKpiNombreProduits = CreateKpiCard(tableKpi, "Nb Produits", 2)
            lblKpiFaibleRotation = CreateKpiCard(tableKpi, "Faible Rotation", 3)
            lblKpiDormants = CreateKpiCard(tableKpi, "Dormants", 4)

            Dim tableCharts As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 2}
            tableCharts.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 40))
            tableCharts.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 60))
            tableCharts.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
            tableCharts.RowStyles.Add(New RowStyle(SizeType.Percent, 50))

            gridProduitVedette = CreateStyledGrid()
            chartTopProduits = New Chart() With {.Dock = DockStyle.Fill, .BackColor = ColorCard}
            chartCategories = New Chart() With {.Dock = DockStyle.Fill, .BackColor = ColorCard}
            gridLegendeCategories = CreateStyledGrid()
            gridLegendeCategories.AutoGenerateColumns = False
            gridLegendeCategories.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            gridLegendeCategories.ScrollBars = ScrollBars.Vertical

            tableCharts.Controls.Add(gridProduitVedette, 0, 0)
            tableCharts.SetRowSpan(gridProduitVedette, 2)
            tableCharts.Controls.Add(chartTopProduits, 1, 0)
            Dim panelCategories As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1}
            panelCategories.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 55))
            panelCategories.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 45))
            panelCategories.Controls.Add(chartCategories, 0, 0)
            panelCategories.Controls.Add(gridLegendeCategories, 1, 0)
            tableCharts.Controls.Add(panelCategories, 1, 1)

            mainTableDash.Controls.Add(flowDashTop, 0, 0)
            mainTableDash.Controls.Add(tableKpi, 0, 1)
            mainTableDash.Controls.Add(tableCharts, 0, 2)
            tabDashboard.Controls.Add(mainTableDash)

            ' Assemblage final
            Me.Controls.Add(tabs)
            Me.Controls.Add(panelHero)

            ' --- Liaison des événements (Logique conservée) ---
            AddHandler btnNouveau.Click, AddressOf NouveauProduit
            AddHandler btnEnregistrer.Click, AddressOf EnregistrerProduit
            AddHandler btnSupprimer.Click, AddressOf SupprimerProduit
            AddHandler btnActualiser.Click, AddressOf ChargerDonnees
            AddHandler btnImprimerProduits.Click, AddressOf ImprimerListeProduits
            AddHandler btnImprimerHistorique.Click, AddressOf ImprimerHistoriquePrix
            AddHandler btnTypesPersonnalises.Click, AddressOf OuvrirTypesPersonnalises
            AddHandler txtRecherche.TextChanged, AddressOf Filtrer
            AddHandler cmbTypeGestionStock.SelectedIndexChanged, AddressOf ModeGestionStockProduitChange
            AddHandler grid.SelectionChanged, AddressOf ChargerSelection
            AddHandler btnPagePrecedente.Click, AddressOf PagePrecedente
            AddHandler btnPageSuivante.Click, AddressOf PageSuivante
            AddHandler txtPrixUnite.TextChanged, AddressOf MajOptionsVente
            AddHandler txtPrixDemi.TextChanged, AddressOf MajOptionsVente
            AddHandler txtPrixQuart.TextChanged, AddressOf MajOptionsVente
            AddHandler txtPrixDouzaine.TextChanged, AddressOf MajOptionsVente
            AddHandler txtPrixGros.TextChanged, AddressOf MajOptionsVente
            AddHandler txtPrixSpecial.TextChanged, AddressOf MajOptionsVente
            AddHandler txtPrixAchat.TextChanged, AddressOf MettreAJourMarge
            AddHandler txtPrixGros.TextChanged, AddressOf MettreAJourMarge
            AddHandler btnCalculerPrix.Click, AddressOf CalculerPrixAuto
            AddHandler cmbProduitHistorique.SelectedIndexChanged, AddressOf ChargerHistoriquePrix
            AddHandler chkFiltreDate.CheckedChanged, AddressOf ChargerHistoriquePrix
            AddHandler dtpHistoriqueDu.ValueChanged, AddressOf ChargerHistoriquePrix
            AddHandler dtpHistoriqueAu.ValueChanged, AddressOf ChargerHistoriquePrix
            AddHandler cmbAnneeDashboard.SelectedIndexChanged, AddressOf ChargerDashboard

            ' --- Initialisation ---
            ' ThemeHelper.AppliquerTheme(Me)
            ConfigurerCharts()
            ConfigurerGrilleProduits()
            ConfigurerGrilleTypesPersonnalises()
            ConfigurerGrilleHistorique()
            ChargerDonnees(Nothing, EventArgs.Empty)
            AddHandler AppEvents.ProduitModifie, AddressOf RafraichirDepuisEvenement
            AddHandler AppEvents.StockModifie, AddressOf RafraichirDepuisEvenement
        End Sub

        ' --- Helpers de Design ---

        Private Function CreateStyledButton(text As String, backColor As Color, Optional w As Integer = 110, Optional h As Integer = 35) As Button
            Return New Button() With {
                .Text = text, .Width = w, .Height = h,
                .BackColor = backColor, .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat, .Font = FontLabel, .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 10, 0)
            }
        End Function

        Private Function CreateCard(title As String) As Panel
            Dim p As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Margin = New Padding(5), .Padding = New Padding(10)}
            p.Controls.Add(New Label() With {.Text = title, .Font = FontLabel, .ForeColor = ColorPrimary, .AutoSize = True, .Top = 5, .Left = 10})
            Return p
        End Function

        Private Function CreateField(parent As Control, label As String, x As Integer, y As Integer, w As Integer) As TextBox
            parent.Controls.Add(New Label() With {.Text = label, .Left = x, .Top = y - 20, .Font = FontLabel, .ForeColor = ColorTextSecondary, .AutoSize = True})
            Dim txt As New TextBox() With {.Left = x, .Top = y, .Width = w, .Font = FontControl, .BorderStyle = BorderStyle.FixedSingle}
            parent.Controls.Add(txt)
            Return txt
        End Function

        Private Function CreateComboField(parent As Control, label As String, x As Integer, y As Integer, w As Integer) As ComboBox
            parent.Controls.Add(New Label() With {.Text = label, .Left = x, .Top = y - 20, .Font = FontLabel, .ForeColor = ColorTextSecondary, .AutoSize = True})
            Dim cmb As New ComboBox() With {.Left = x, .Top = y, .Width = w, .Font = FontControl, .DropDownStyle = ComboBoxStyle.DropDownList, .FlatStyle = FlatStyle.Flat}
            parent.Controls.Add(cmb)
            Return cmb
        End Function

        Private Function CreateOption(parent As Control, text As String) As CheckBox
            Dim chk As New CheckBox() With {.Text = text, .Font = FontSubTitle, .AutoSize = True, .Margin = New Padding(0, 0, 10, 5)}
            parent.Controls.Add(chk)
            Return chk
        End Function

        Private Function CreateKpiCard(parent As TableLayoutPanel, title As String, col As Integer) As Label
            Dim p As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Margin = New Padding(5)}
            Dim lblTitre As New Label() With {.Text = title, .Top = 10, .Left = 10, .Font = FontLabel, .ForeColor = ColorTextSecondary, .AutoSize = True}
            Dim lblValeur As New Label() With {.Top = 40, .Left = 10, .Font = New Font("Segoe UI", 14.0F, FontStyle.Bold), .ForeColor = ColorPrimary, .AutoSize = True}
            p.Controls.Add(lblTitre)
            p.Controls.Add(lblValeur)
            If String.Equals(title, "Dormants", StringComparison.OrdinalIgnoreCase) Then
                p.Cursor = Cursors.Hand
                lblTitre.Cursor = Cursors.Hand
                lblValeur.Cursor = Cursors.Hand
                AddHandler p.Click, AddressOf OuvrirProduitsDormants
                AddHandler lblTitre.Click, AddressOf OuvrirProduitsDormants
                AddHandler lblValeur.Click, AddressOf OuvrirProduitsDormants
            End If
            parent.Controls.Add(p, col, 0)
            Return lblValeur
        End Function

        Private Function CreateStyledGrid() As DataGridView
            Dim dgv As New DataGridView() With {
                .Dock = DockStyle.Fill, .BackgroundColor = Color.White, .BorderStyle = BorderStyle.None,
                .EnableHeadersVisualStyles = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AllowUserToAddRows = False, .ReadOnly = True, .RowHeadersVisible = False,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .GridColor = ColorBorder
            }
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
            dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            dgv.ColumnHeadersHeight = 40
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 234, 246)
            dgv.DefaultCellStyle.SelectionForeColor = ColorPrimary
            dgv.DefaultCellStyle.Font = FontControl
            Return dgv
        End Function

        Private Sub ConfigurerCharts()
            ConfigurerChart(chartTopProduits, SeriesChartType.Bar, "TopProduits")
            ConfigurerChart(chartCategories, SeriesChartType.Pie, "Categories")
            chartCategories.Series(0).IsValueShownAsLabel = False
            chartCategories.Series(0).LegendText = String.Empty
            chartCategories.Series(0).ToolTip = "#VALX : #PERCENT{P1}"
            chartCategories.Series(0)("PieLabelStyle") = "Inside"
            chartCategories.Series(0)("PieLineColor") = "Transparent"
            chartCategories.ChartAreas(0).Position = New ElementPosition(4, 4, 92, 92)
            chartCategories.ChartAreas(0).InnerPlotPosition = New ElementPosition(8, 6, 84, 88)
            ConfigurerGrilleLegendeCategories()
        End Sub

        Private Sub ConfigurerChart(chart As Chart, type As SeriesChartType, name As String)
            chart.ChartAreas.Clear()
            chart.Series.Clear()
            chart.ChartAreas.Add(New ChartArea("Main"))
            chart.Series.Add(New Series(name) With {.ChartType = type, .Palette = ChartColorPalette.Pastel})
        End Sub

        Private Sub ConfigurerGrilleProduits()
            grid.Columns.Clear()
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            grid.ScrollBars = ScrollBars.Both
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "ProduitId", .HeaderText = "ProduitId", .Visible = False})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "CategorieId", .HeaderText = "CategorieId", .Visible = False})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "Libelle", .HeaderText = "Désignation", .Width = 180})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "NomCategorie", .HeaderText = "Catégorie", .Width = 120})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "CodeBarres", .HeaderText = "Code Barres", .Width = 120})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "QuantiteStock", .HeaderText = "Stock", .Width = 80})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "UnitePrincipale", .HeaderText = "Unité", .Width = 80})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "UniteSecondaireAffichage", .HeaderText = "Unité 2", .Width = 80})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixAchat", .HeaderText = "P. Achat", .Width = 90})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixGros", .HeaderText = "P. Gros", .Width = 90})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixDetail", .HeaderText = "P. Détail", .Width = 90})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixDemi", .HeaderText = "P. Demi", .Width = 85})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixQuart", .HeaderText = "P. Quart", .Width = 85})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixDouzaine", .HeaderText = "P. Douzaine", .Width = 100})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "MargePourcent", .HeaderText = "Marge %", .Width = 80})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "DateExpiration", .HeaderText = "Expiration", .Width = 95})
            grid.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "VenteDetail", .HeaderText = "Détail", .Width = 60})
            grid.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "VenteDemi", .HeaderText = "Demi", .Width = 55})
            grid.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "VenteDouzaine", .HeaderText = "Douzaine", .Width = 70})
            grid.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "VenteGros", .HeaderText = "Gros", .Width = 55})
            grid.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "EstActif", .HeaderText = "Actif", .Width = 55})
        End Sub

        Private Sub ConfigurerGrilleHistorique()
            gridHistorique.Columns.Clear()
            gridHistorique.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            gridHistorique.ScrollBars = ScrollBars.Both
            gridHistorique.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "ProduitId", .HeaderText = "ProduitId", .Visible = False})
            gridHistorique.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "Produit", .HeaderText = "Produit", .Width = 180})
            gridHistorique.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "TypePrix", .HeaderText = "Type", .Width = 90})
            gridHistorique.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "AncienPrix", .HeaderText = "Ancien Prix", .Width = 100})
            gridHistorique.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "NouveauPrix", .HeaderText = "Nouveau Prix", .Width = 100})
            gridHistorique.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "ModifieLe", .HeaderText = "Date", .Width = 120})
            gridHistorique.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "Utilisateur", .HeaderText = "Utilisateur", .Width = 140})
        End Sub

        Private Sub ConfigurerGrilleLegendeCategories()
            gridLegendeCategories.Columns.Clear()
            gridLegendeCategories.RowTemplate.Height = 28
            gridLegendeCategories.Columns.Add(New DataGridViewImageColumn() With {.Name = "Couleur", .DataPropertyName = "CouleurImage", .HeaderText = "", .Width = 35, .ImageLayout = DataGridViewImageCellLayout.Normal})
            gridLegendeCategories.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "Categorie", .DataPropertyName = "Categorie", .HeaderText = "Catégorie", .Width = 180})
            gridLegendeCategories.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "Valeur", .DataPropertyName = "Valeur", .HeaderText = "Qté", .Width = 70})
            gridLegendeCategories.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "Pourcentage", .DataPropertyName = "Pourcentage", .HeaderText = "%", .Width = 70})
            gridLegendeCategories.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "CouleurArgb", .DataPropertyName = "CouleurArgb", .Visible = False})
        End Sub

        Private Sub ConfigurerGrilleTypesPersonnalises()
            gridTypesPersonnalises.Columns.Clear()
            gridTypesPersonnalises.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            gridTypesPersonnalises.ScrollBars = ScrollBars.Both
            gridTypesPersonnalises.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "TypeVenteProduitId", .HeaderText = "Id", .Visible = False})
            gridTypesPersonnalises.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "Nom", .HeaderText = "Nom", .Width = 180})
            gridTypesPersonnalises.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "QuantiteEquivalent", .HeaderText = "Qté équiv.", .Width = 90})
            gridTypesPersonnalises.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "ModePrixAffichage", .HeaderText = "Mode prix", .Width = 120})
            gridTypesPersonnalises.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixVente", .HeaderText = "Prix vente", .Width = 100})
            gridTypesPersonnalises.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "Actif", .HeaderText = "Actif", .Width = 60})
            gridTypesPersonnalises.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "NomAffichage", .HeaderText = "Nom affiché", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
        End Sub

        ' --- LOGIQUE MÉTIER (STRICTEMENT IDENTIQUE À L'ORIGINAL) ---

        Private Function ObtenirService() As ProduitService
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New ProduitRepository(dal)
            Return New ProduitService(repo)
        End Function

        Private Sub ChargerDonnees(sender As Object, e As EventArgs)
            'Try
            '    Dim service As ProduitService = ObtenirService()
            '    _produitsTable = service.
            '    _produitsView = New DataView(_produitsTable)
            '    MettreAJourPagination()
            '    RemplirComboProduitsHistorique()
            '    RemplirComboAnnees()
            'Catch ex As Exception
            '    MessageBox.Show("Erreur chargement: " & ex.Message)
            'End Try




            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim repo As New ProduitRepository(dal)
                _produitsTable = repo.ListerTable()
                _categoriesTable = (New SuperAdminService()).ListerCategories()
                If Not _produitsTable.Columns.Contains("MargePourcent") Then
                    _produitsTable.Columns.Add("MargePourcent", GetType(Decimal))
                End If
                If Not _produitsTable.Columns.Contains("UniteSecondaireAffichage") Then
                    _produitsTable.Columns.Add("UniteSecondaireAffichage", GetType(String))
                End If

                For Each row As DataRow In _produitsTable.Rows
                    Dim prixAchat As Decimal = Convert.ToDecimal(row("PrixAchat"))
                    Dim prixGros As Decimal = Convert.ToDecimal(row("PrixGros"))
                    Dim marge As Decimal = 0D
                    If prixAchat > 0D AndAlso prixGros > 0D Then
                        marge = Math.Round(((prixGros / prixAchat) - 1D) * 100D, 2)
                    End If
                    row("MargePourcent") = marge

                    Dim typeGestion As String = If(row.Table.Columns.Contains("TypeGestionStock") AndAlso Not row.IsNull("TypeGestionStock"), Convert.ToString(row("TypeGestionStock")).Trim(), String.Empty)
                    If StockUnitConversionService.EstGestionMesuree(typeGestion) Then
                        row("UniteSecondaireAffichage") = If(row.Table.Columns.Contains("UniteMesureStock") AndAlso Not row.IsNull("UniteMesureStock"), Convert.ToString(row("UniteMesureStock")).Trim(), String.Empty)
                    Else
                        row("UniteSecondaireAffichage") = If(row.Table.Columns.Contains("UniteSecondaire") AndAlso Not row.IsNull("UniteSecondaire"), Convert.ToString(row("UniteSecondaire")).Trim(), String.Empty)
                    End If
                Next

                _produitsView = New DataView(_produitsTable)
                ChargerCategories()
                ChargerPageProduits(True)
                RemplirComboProduitsHistorique()
                ChargerHistoriquePrix(Nothing, EventArgs.Empty)
                ChargerDashboard(Nothing, EventArgs.Empty)
                MettreAJourPagination()
                RemplirComboAnnees()
                ChargerTypesPersonnalisesProduit()
            Catch ex As Exception
                MessageBox.Show("Erreur chargement produits: " & ex.Message)
            End Try
        End Sub
        Private Sub ChargerPageProduits(reinitialiser As Boolean)
            If _produitsView Is Nothing Then
                Return
            End If
            If reinitialiser Then
                _pageCourante = 1
            End If
            Dim tablePage As DataTable = _produitsTable.Clone()
            Dim lignes As DataRow() = _produitsView.ToTable().Select()
            Dim totalLignes As Integer = lignes.Length
            Dim totalPages As Integer = Math.Max(1, CInt(Math.Ceiling(totalLignes / CType(TaillePageProduits, Decimal))))
            If _pageCourante > totalPages Then
                _pageCourante = totalPages
            End If
            Dim debut As Integer = (_pageCourante - 1) * TaillePageProduits
            Dim fin As Integer = Math.Min(debut + TaillePageProduits - 1, totalLignes - 1)
            If totalLignes > 0 Then
                For i As Integer = debut To fin
                    tablePage.ImportRow(lignes(i))
                Next
            End If
            grid.DataSource = tablePage
            lblPagination.Text = "Page " & _pageCourante.ToString() & "/" & totalPages.ToString()
            btnPagePrecedente.Enabled = _pageCourante > 1
            btnPageSuivante.Enabled = _pageCourante < totalPages
        End Sub
        Private Sub MettreAJourPagination()
            If _produitsView Is Nothing Then Return
            Dim total As Integer = _produitsView.Count
            Dim nbPages As Integer = Math.Max(1, CInt(Math.Ceiling(total / TaillePageProduits)))
            If _pageCourante > nbPages Then _pageCourante = nbPages
            lblPagination.Text = "Page " & _pageCourante.ToString() & "/" & nbPages.ToString()
            btnPagePrecedente.Enabled = _pageCourante > 1
            btnPageSuivante.Enabled = _pageCourante < nbPages

            Dim dtPage As DataTable = _produitsTable.Clone()
            Dim debut As Integer = (_pageCourante - 1) * TaillePageProduits
            Dim fin As Integer = Math.Min(debut + TaillePageProduits, total) - 1
            For i As Integer = debut To fin
                dtPage.ImportRow(_produitsView(i).Row)
            Next
            grid.DataSource = dtPage
        End Sub

        Private Sub PagePrecedente(sender As Object, e As EventArgs)
            If _pageCourante > 1 Then
                _pageCourante -= 1
                MettreAJourPagination()
            End If
        End Sub

        Private Sub PageSuivante(sender As Object, e As EventArgs)
            Dim total As Integer = _produitsView.Count
            Dim nbPages As Integer = Math.Max(1, CInt(Math.Ceiling(total / TaillePageProduits)))
            If _pageCourante < nbPages Then
                _pageCourante += 1
                MettreAJourPagination()
            End If
        End Sub

        Private Sub Filtrer(sender As Object, e As EventArgs)
            If _produitsView Is Nothing Then Return
            Dim q As String = txtRecherche.Text.Trim().Replace("'", "''")
            If q = "" Then
                _produitsView.RowFilter = ""
            Else
                _produitsView.RowFilter = String.Format("Libelle LIKE '%{0}%' OR CodeBarres LIKE '%{0}%'", q)
            End If
            _pageCourante = 1
            MettreAJourPagination()
        End Sub

        Private Sub NouveauProduit(sender As Object, e As EventArgs)
            _produitId = 0
            txtLibelle.Clear()
            txtCodeBarres.Clear()
            cmbCategorie.SelectedIndex = -1
            txtPrixAchat.Clear()
            txtPrixGros.Clear()
            txtPrixUnite.Clear()
            txtPrixDemi.Clear()
            txtPrixQuart.Clear()
            txtPrixDouzaine.Clear()
            txtPrixSpecial.Clear()
            txtCoeffGros.Clear()
            txtQuantite.Clear()
            txtSeuil.Clear()
            txtMarge.Clear()
            cmbUnitePrincipale.SelectedIndex = -1
            cmbUniteSecondaire.SelectedIndex = -1
            cmbTypeGestionStock.SelectedItem = "UNITE"
            cmbUniteMesureStock.SelectedItem = "KG"
            txtContenuUnitePrincipale.Clear()
            txtContenuUniteSecondaire.Clear()
            ModeGestionStockProduitChange(Nothing, EventArgs.Empty)
            chkActif.Checked = True
            chkVenteGros.Checked = False
            chkVenteUnite.Checked = False
            chkVenteDemi.Checked = False
            chkVenteQuart.Checked = False
            chkVenteDouzaine.Checked = False
            gridTypesPersonnalises.DataSource = Nothing
            MessageBox.Show("L'ajout direct n'est pas autorisé ici. Sélectionnez un produit existant pour le modifier.")
        End Sub

        Private Sub ChargerTypesPersonnalisesProduit()
            If _produitId <= 0 Then
                gridTypesPersonnalises.DataSource = Nothing
                Return
            End If

            Dim service As New TypeVenteProduitService()
            gridTypesPersonnalises.DataSource = Nothing
            gridTypesPersonnalises.DataSource = service.ListerParProduit(_produitId, False)
        End Sub

        Private Sub OuvrirTypesPersonnalises(sender As Object, e As EventArgs)
            If _produitId <= 0 Then
                MessageBox.Show("Sélectionnez d'abord un produit pour gérer ses types personnalisés.")
                Return
            End If

            Using frm As New FormulaireTypesVenteProduit(_produitId, LireDecimal(txtPrixAchat.Text), LireDecimal(txtConversion.Text), True, Nothing, Nothing, cmbUnitePrincipale.Text, cmbUniteSecondaire.Text, If(EstGestionMesureProduit(), UniteMesureStockProduit(), Nothing))
                frm.ShowDialog(Me)
            End Using

            ChargerTypesPersonnalisesProduit()
        End Sub

        Private Function TypeGestionStockProduit() As String
            Dim valeur As String = Convert.ToString(cmbTypeGestionStock.SelectedItem).Trim().ToUpperInvariant()
            If valeur = "MESURE" Then Return "MESURE"
            Return "UNITE"
        End Function

        Private Function EstGestionMesureProduit() As Boolean
            Return String.Equals(TypeGestionStockProduit(), "MESURE", StringComparison.OrdinalIgnoreCase)
        End Function

        Private Function UniteMesureStockProduit() As String
            Dim unite As String = Convert.ToString(cmbUniteMesureStock.Text).Trim().ToUpperInvariant()
            If String.IsNullOrWhiteSpace(unite) Then Return "KG"
            Return unite
        End Function

        Private Function LireContenuUnitePrincipaleProduit() As Decimal
            If EstGestionMesureProduit() Then
                Dim contenu As Decimal = LireDecimal(txtContenuUnitePrincipale.Text)
                If contenu > 0D Then Return contenu
            End If

            Dim conversion As Decimal = LireDecimal(txtConversion.Text)
            If conversion > 0D Then Return conversion
            Return 1D
        End Function

        Private Function LireContenuUniteSecondaireProduit() As Decimal?
            If Not EstGestionMesureProduit() Then Return Nothing
            Dim contenu As Decimal = LireDecimal(txtContenuUniteSecondaire.Text)
            If contenu > 0D Then Return contenu
            Return Nothing
        End Function

        Private Sub ModeGestionStockProduitChange(sender As Object, e As EventArgs)
            Dim mesure As Boolean = EstGestionMesureProduit()
            cmbUniteMesureStock.Enabled = mesure
            txtContenuUnitePrincipale.Enabled = mesure
            txtContenuUniteSecondaire.Enabled = mesure
            If Not mesure Then
                txtContenuUnitePrincipale.Text = txtConversion.Text
                txtContenuUniteSecondaire.Clear()
            End If
        End Sub

        Private Sub ChargerSelection(sender As Object, e As EventArgs)
            If grid.CurrentRow Is Nothing Then Return
            Dim row As DataRowView = TryCast(grid.CurrentRow.DataBoundItem, DataRowView)
            If row Is Nothing Then Return
            Dim r As DataRow = row.Row
            _produitId = Convert.ToInt32(row("ProduitId"))
            txtLibelle.Text = Convert.ToString(row("Libelle"))
            txtCodeBarres.Text = Convert.ToString(row("CodeBarres"))
            If Not r.IsNull("CategorieId") AndAlso cmbCategorie.DataSource IsNot Nothing Then
                cmbCategorie.SelectedValue = Convert.ToInt32(row("CategorieId"))
            Else
                cmbCategorie.SelectedIndex = -1
            End If
            chkActif.Checked = SafeBoolean(row("EstActif"))
            cmbUnitePrincipale.Text = If(r.IsNull("UnitePrincipale"), "", Convert.ToString(row("UnitePrincipale")))
            cmbUniteSecondaire.Text = If(r.IsNull("UniteSecondaire"), "", Convert.ToString(row("UniteSecondaire")))
            txtConversion.Text = LireDecimalRow(row, "ConversionUnite").ToString("N2")
            Dim typeGestion As String = If(r.Table.Columns.Contains("TypeGestionStock") AndAlso Not r.IsNull("TypeGestionStock"), Convert.ToString(row("TypeGestionStock")).Trim().ToUpperInvariant(), "UNITE")
            _typeGestionStockOriginal = StockUnitConversionService.NormaliserTypeGestionStock(typeGestion)
            _conversionUniteOriginale = Math.Max(1D, LireDecimalRow(row, "ConversionUnite"))
            _quantiteBaseOriginale = LireDecimalRow(row, "QuantiteStock")
            _unitePrincipaleOriginale = cmbUnitePrincipale.Text.Trim()
            _uniteSecondaireOriginale = cmbUniteSecondaire.Text.Trim()
            cmbTypeGestionStock.SelectedItem = If(typeGestion = "MESURE" OrElse typeGestion = "POIDS" OrElse typeGestion = "VOLUME", "MESURE", "UNITE")
            Dim uniteMesure As String = If(r.Table.Columns.Contains("UniteMesureStock") AndAlso Not r.IsNull("UniteMesureStock"), Convert.ToString(row("UniteMesureStock")).Trim().ToUpperInvariant(), "KG")
            If String.IsNullOrWhiteSpace(uniteMesure) Then uniteMesure = "KG"
            cmbUniteMesureStock.Text = uniteMesure
            txtContenuUnitePrincipale.Text = If(r.Table.Columns.Contains("ContenuUnitePrincipale") AndAlso Not r.IsNull("ContenuUnitePrincipale"), Convert.ToDecimal(row("ContenuUnitePrincipale")).ToString("N2"), txtConversion.Text)
            txtContenuUniteSecondaire.Text = If(r.Table.Columns.Contains("ContenuUniteSecondaire") AndAlso Not r.IsNull("ContenuUniteSecondaire"), Convert.ToDecimal(row("ContenuUniteSecondaire")).ToString("N2"), String.Empty)
            ModeGestionStockProduitChange(Nothing, EventArgs.Empty)
            txtPrixAchat.Text = LireDecimalRow(row, "PrixAchat").ToString("N2")
            txtCoeffGros.Text = LireDecimalRow(row, "CoefficientGros").ToString("N4")
            txtPrixGros.Text = LireDecimalRow(row, "PrixGros").ToString("N2")
            txtPrixUnite.Text = LireDecimalRow(row, "PrixDetail").ToString("N2")
            txtPrixDemi.Text = LireDecimalRow(row, "PrixDemi").ToString("N2")
            txtPrixQuart.Text = LireDecimalRow(row, "PrixQuart").ToString("N2")
            txtPrixDouzaine.Text = LireDecimalRow(row, "PrixDouzaine").ToString("N2")
            txtPrixSpecial.Text = LireDecimalRow(row, "PrixSpecial").ToString("N2")
            txtQuantite.Text = LireDecimalRow(row, "QuantiteStock").ToString("N2")
            txtSeuil.Text = LireDecimalRow(row, "SeuilCritique").ToString("N2")
            txtMarge.Text = LireDecimalRow(row, "MargePourcent").ToString("N2")
            If r.IsNull("DateExpiration") Then
                dtpExpiration.Value = Date.Now
            Else
                dtpExpiration.Value = Convert.ToDateTime(row("DateExpiration"))
            End If
            chkVenteUnite.Checked = SafeBoolean(row("VenteDetail"))
            chkVenteDemi.Checked = SafeBoolean(row("VenteDemi"))
            chkVenteDouzaine.Checked = SafeBoolean(row("VenteDouzaine"))
            chkVenteGros.Checked = SafeBoolean(row("VenteGros"))
            chkVenteQuart.Checked = LireDecimal(txtPrixQuart.Text) > 0D
            ChargerTypesPersonnalisesProduit()
        End Sub

        Private Sub EnregistrerProduit(sender As Object, e As EventArgs)
            Try
                If _produitId <= 0 Then
                    MessageBox.Show("Sélectionnez un produit existant à modifier.")
                    Return
                End If
                If Not ValiderFormulaire() Then Return

                Dim service As ProduitService = ObtenirService()
                Dim produit As New Produit With {
                    .ProduitId = _produitId,
                    .CodeBarres = txtCodeBarres.Text.Trim(),
                    .Libelle = txtLibelle.Text.Trim(),
                    .PrixAchat = LireDecimal(txtPrixAchat.Text),
                    .PrixDetail = LireDecimal(txtPrixUnite.Text),
                    .PrixDemi = If(chkVenteDemi.Checked, LireDecimal(txtPrixDemi.Text), 0D),
                    .PrixQuart = If(chkVenteQuart.Checked, LireDecimal(txtPrixQuart.Text), 0D),
                    .PrixDouzaine = If(chkVenteDouzaine.Checked, LireDecimal(txtPrixDouzaine.Text), 0D),
                    .PrixGros = If(chkVenteGros.Checked, LireDecimal(txtPrixGros.Text), 0D),
                    .PrixSpecial = LireDecimal(txtPrixSpecial.Text),
                    .CoefficientGros = LireDecimal(txtCoeffGros.Text),
                    .SeuilCritique = LireDecimal(txtSeuil.Text),
                    .DateExpiration = dtpExpiration.Value,
                    .CategorieId = LireCategorieSelectionnee(),
                    .UnitePrincipale = If(cmbUnitePrincipale.Text.Trim() = "", Nothing, cmbUnitePrincipale.Text.Trim()),
                    .UniteSecondaire = If(cmbUniteSecondaire.Text.Trim() = "", Nothing, cmbUniteSecondaire.Text.Trim()),
                    .ConversionUnite = If(LireDecimal(txtConversion.Text) > 0D, LireDecimal(txtConversion.Text), LireContenuUnitePrincipaleProduit()),
                    .TypeGestionStock = TypeGestionStockProduit(),
                    .UniteMesureStock = If(EstGestionMesureProduit(), UniteMesureStockProduit(), "PIECE"),
                    .ContenuUnitePrincipale = LireContenuUnitePrincipaleProduit(),
                    .ContenuUniteSecondaire = LireContenuUniteSecondaireProduit(),
                    .EstActif = chkActif.Checked,
                    .VenteDetail = chkVenteUnite.Checked,
                    .VenteDemi = chkVenteDemi.Checked,
                    .VenteDouzaine = chkVenteDouzaine.Checked,
                    .VenteGros = chkVenteGros.Checked
                }

                If DoitMigrerStockUniteVersMesure(produit) Then
                    Dim nouvelleQuantiteBase As Decimal = CalculerNouvelleQuantiteBaseMesure(produit)
                    If Not ConfirmerMigrationStockUniteVersMesure(produit, nouvelleQuantiteBase) Then Return
                    service.MettreAJourAvecMigrationUniteVersMesure(produit, _quantiteBaseOriginale, nouvelleQuantiteBase, SessionUtilisateur.UtilisateurId)
                Else
                    service.MettreAJour(produit)
                End If
                MessageBox.Show("Produit modifié.")
                ChargerDonnees(sender, e)
            Catch ex As Exception
                MessageBox.Show("Erreur enregistrement: " & ex.Message)
            End Try
        End Sub

        Private Sub SupprimerProduit(sender As Object, e As EventArgs)
            Try
                If _produitId <= 0 Then
                    MessageBox.Show("Sélectionnez un produit.")
                    Return
                End If
                Dim rep As DialogResult = MessageBox.Show("Voulez-vous supprimer ce produit ?", "Suppression", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                If rep <> DialogResult.Yes Then Return

                Dim service As ProduitService = ObtenirService()
                service.Supprimer(_produitId)
                _produitId = 0
                ChargerDonnees(sender, e)
            Catch ex As Exception
                MessageBox.Show("Erreur suppression: " & ex.Message)
            End Try
        End Sub

        Private Function ValiderFormulaire() As Boolean
            If txtLibelle.Text.Trim() = "" Then
                MessageBox.Show("La désignation est obligatoire.")
                Return False
            End If
            If cmbUnitePrincipale.Text.Trim() = "" Then
                MessageBox.Show("L'unité principale est obligatoire.")
                Return False
            End If
            If LireContenuUnitePrincipaleProduit() <= 0D Then
                MessageBox.Show(If(EstGestionMesureProduit(), "Le contenu de l'unité principale doit être supérieur à zéro.", "La conversion unité doit être supérieure à zéro."))
                Return False
            End If
            If EstGestionMesureProduit() AndAlso String.IsNullOrWhiteSpace(UniteMesureStockProduit()) Then
                MessageBox.Show("L'unité de mesure du stock est obligatoire.")
                Return False
            End If
            Return True
        End Function

        Private Function DoitMigrerStockUniteVersMesure(produit As Produit) As Boolean
            If produit Is Nothing Then Return False
            Return String.Equals(_typeGestionStockOriginal, "UNITE", StringComparison.OrdinalIgnoreCase) AndAlso
                   StockUnitConversionService.EstGestionMesuree(produit.TypeGestionStock)
        End Function

        Private Function CalculerNouvelleQuantiteBaseMesure(produit As Produit) As Decimal
            Dim ancienneConversion As Decimal = If(_conversionUniteOriginale > 0D, _conversionUniteOriginale, 1D)
            Dim ancienNombreUnitesPrincipales As Decimal = _quantiteBaseOriginale / ancienneConversion
            Return ancienNombreUnitesPrincipales * produit.ContenuUnitePrincipale
        End Function

        Private Function ConfirmerMigrationStockUniteVersMesure(produit As Produit, nouvelleQuantiteBase As Decimal) As Boolean
            If String.IsNullOrWhiteSpace(produit.UniteMesureStock) OrElse produit.ContenuUnitePrincipale <= 0D Then
                MessageBox.Show("L'unité de mesure et le contenu de l'unité principale sont obligatoires pour convertir ce stock en mode MESURE.", "Conversion stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If
            If Not String.IsNullOrWhiteSpace(produit.UniteSecondaire) AndAlso
               (Not produit.ContenuUniteSecondaire.HasValue OrElse produit.ContenuUniteSecondaire.Value <= 0D) Then
                MessageBox.Show("Le contenu de l'unité secondaire doit être supérieur à zéro pour convertir ce stock en mode MESURE.", "Conversion stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If

            Dim ancienneConversion As Decimal = If(_conversionUniteOriginale > 0D, _conversionUniteOriginale, 1D)
            Dim ancienNombreUnitesPrincipales As Decimal = _quantiteBaseOriginale / ancienneConversion
            Dim unitePrincipale As String = If(String.IsNullOrWhiteSpace(produit.UnitePrincipale), _unitePrincipaleOriginale, produit.UnitePrincipale)
            Dim message As String =
                "Ce produit possède actuellement " & FormatageGlobal.FormatQuantitePhysique(ancienNombreUnitesPrincipales) & " " & unitePrincipale & " en stock." &
                Environment.NewLine &
                "1 " & unitePrincipale & " = " & FormatageGlobal.FormatQuantitePhysique(produit.ContenuUnitePrincipale) & " " & produit.UniteMesureStock & "." &
                Environment.NewLine & Environment.NewLine &
                "Le stock sera converti en :" &
                Environment.NewLine &
                FormatageGlobal.FormatStockSelonGestion(
                    nouvelleQuantiteBase,
                    produit.ConversionUnite,
                    produit.UnitePrincipale,
                    produit.UniteSecondaire,
                    produit.TypeGestionStock,
                    produit.UniteMesureStock,
                    produit.ContenuUnitePrincipale,
                    If(produit.ContenuUniteSecondaire.HasValue, produit.ContenuUniteSecondaire.Value, 0D)) &
                Environment.NewLine & Environment.NewLine &
                "Continuer ?"

            Return MessageBox.Show(message, "Conversion du stock UNITE vers MESURE", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) = DialogResult.Yes
        End Function

        Private Function LireDecimal(texte As String) As Decimal
            Dim v As Decimal
            If Decimal.TryParse(If(texte.Trim() = "", "0", texte.Trim()), v) Then Return v
            Return 0D
        End Function

        Private Function LireDecimalRow(row As DataRowView, colonne As String) As Decimal
            If row Is Nothing OrElse row.Row.IsNull(colonne) Then Return 0D
            Return SafeDecimal(row(colonne))
        End Function

        Private Function SafeDecimal(value As Object) As Decimal
            If value Is Nothing OrElse value Is DBNull.Value Then
                Return 0D
            End If

            Dim texte As String = Convert.ToString(value).Trim()
            If texte = String.Empty Then
                Return 0D
            End If

            Dim nombre As Decimal
            If Decimal.TryParse(texte, nombre) Then
                Return nombre
            End If

            If Decimal.TryParse(texte, Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, nombre) Then
                Return nombre
            End If

            Return 0D
        End Function

        Private Function SafeBoolean(value As Object) As Boolean
            If value Is Nothing OrElse value Is DBNull.Value Then
                Return False
            End If

            If TypeOf value Is Boolean Then
                Return CBool(value)
            End If

            Dim texte As String = Convert.ToString(value).Trim()
            If texte = String.Empty Then
                Return False
            End If

            Dim resultat As Boolean
            If Boolean.TryParse(texte, resultat) Then
                Return resultat
            End If

            Dim nombre As Integer
            If Integer.TryParse(texte, nombre) Then
                Return nombre <> 0
            End If

            Return False
        End Function

        Private Sub MajOptionsVente(sender As Object, e As EventArgs)
            chkVenteGros.Checked = LireDecimal(txtPrixGros.Text) > 0D
            chkVenteUnite.Checked = LireDecimal(txtPrixUnite.Text) > 0D
            chkVenteDemi.Checked = LireDecimal(txtPrixDemi.Text) > 0D
            chkVenteQuart.Checked = LireDecimal(txtPrixQuart.Text) > 0D
            chkVenteDouzaine.Checked = LireDecimal(txtPrixDouzaine.Text) > 0D
        End Sub

        Private Sub MettreAJourMarge(sender As Object, e As EventArgs)
            Dim prixAchat As Decimal = LireDecimal(txtPrixAchat.Text)
            Dim prixGros As Decimal = LireDecimal(txtPrixGros.Text)
            If prixAchat > 0D AndAlso prixGros > 0D Then
                txtMarge.Text = Math.Round(((prixGros / prixAchat) - 1D) * 100D, 2).ToString("N2")
            Else
                txtMarge.Text = "0,00"
            End If
        End Sub

        Private Sub CalculerPrixAuto(sender As Object, e As EventArgs)
            Dim prixAchat As Decimal = LireDecimal(txtPrixAchat.Text)
            Dim coeff As Decimal = LireDecimal(txtCoeffGros.Text)
            If prixAchat <= 0D OrElse coeff <= 0D Then Return

            Dim prixGros As Decimal = prixAchat * coeff
            Dim prixDemi As Decimal = prixGros * 0.5D
            Dim prixQuart As Decimal = prixGros * 0.25D
            Dim conv As Decimal = LireDecimal(txtConversion.Text)
            Dim prixUnite As Decimal = 0D
            Dim prixDouzaine As Decimal = 0D
            If conv > 0D Then
                prixUnite = prixGros / conv
                prixDouzaine = prixUnite * 12D
            End If

            txtPrixGros.Text = prixGros.ToString("N2")
            txtPrixDemi.Text = prixDemi.ToString("N2")
            txtPrixQuart.Text = prixQuart.ToString("N2")
            txtPrixUnite.Text = prixUnite.ToString("N2")
            txtPrixDouzaine.Text = prixDouzaine.ToString("N2")
            MajOptionsVente(Nothing, EventArgs.Empty)
            MettreAJourMarge(Nothing, EventArgs.Empty)
        End Sub

        Private Sub RemplirComboProduitsHistorique()
            cmbProduitHistorique.Items.Clear()
            cmbProduitHistorique.Items.Add(New ComboProduitItem(0, "Tous les produits"))
            If _produitsTable IsNot Nothing Then
                For Each row As DataRow In _produitsTable.Rows
                    cmbProduitHistorique.Items.Add(New ComboProduitItem(Convert.ToInt32(row("ProduitId")), Convert.ToString(row("Libelle"))))
                Next
            End If
            cmbProduitHistorique.DisplayMember = "Libelle"
            cmbProduitHistorique.ValueMember = "ProduitId"
            cmbProduitHistorique.SelectedIndex = 0
        End Sub

        Private Sub RemplirComboAnnees()
            cmbAnneeDashboard.Items.Clear()
            For i As Integer = DateTime.Now.Year To DateTime.Now.Year - 5 Step -1
                cmbAnneeDashboard.Items.Add(i)
            Next
            cmbAnneeDashboard.SelectedIndex = 0
        End Sub

        Private Sub ChargerHistoriquePrix(sender As Object, e As EventArgs)
            Try
                Dim service As ProduitService = ObtenirService()
                Dim pId As Integer? = Nothing
                If cmbProduitHistorique.SelectedItem IsNot Nothing Then
                    Dim item As ComboProduitItem = DirectCast(cmbProduitHistorique.SelectedItem, ComboProduitItem)
                    If item.ProduitId > 0 Then pId = item.ProduitId
                End If
                Dim dDu As Date? = If(chkFiltreDate.Checked, dtpHistoriqueDu.Value.Date, CType(Nothing, Date?))
                Dim dAu As Date? = If(chkFiltreDate.Checked, dtpHistoriqueAu.Value.Date, CType(Nothing, Date?))
                _historiqueTable = service.ListerHistoriquePrixTable(pId, dDu, dAu)
                gridHistorique.DataSource = _historiqueTable
            Catch
                _historiqueTable = Nothing
            End Try
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
                Dim produitIdSelectionne As Integer = _produitId
                Dim ongletSelectionne As Integer = tabs.SelectedIndex
                Dim pageSelectionnee As Integer = _pageCourante

                ChargerDonnees(Nothing, EventArgs.Empty)
                tabs.SelectedIndex = Math.Max(0, Math.Min(ongletSelectionne, tabs.TabPages.Count - 1))

                If Not String.IsNullOrWhiteSpace(txtRecherche.Text) Then
                    Filtrer(Nothing, EventArgs.Empty)
                End If

                If pageSelectionnee > 1 Then
                    _pageCourante = pageSelectionnee
                    MettreAJourPagination()
                End If

                If produitIdSelectionne > 0 Then
                    For Each row As DataGridViewRow In grid.Rows
                        If row Is Nothing OrElse row.IsNewRow Then Continue For
                        If Convert.ToInt32(row.Cells("ProduitId").Value) = produitIdSelectionne Then
                            row.Selected = True
                            grid.CurrentCell = row.Cells(2)
                            Exit For
                        End If
                    Next
                End If
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireProduits", "RafraichirDepuisEvenement", "Erreur lors du rafraichissement automatique du catalogue produits.", ex)
            Finally
                _isRefreshingFromEvent = False
            End Try
        End Sub

        Private Sub ChargerDashboard(sender As Object, e As EventArgs)
            'Try
            '    Dim annee As Integer = Convert.ToInt32(cmbAnneeDashboard.SelectedItem)
            '    Dim service As ProduitService = ObtenirService()
            '    Dim ds As DataSet = service.KpiProduits

            '    ' KPI
            '    Dim dtKpi As DataTable = ds.Tables("KPI")
            '    If dtKpi.Rows.Count > 0 Then
            '        Dim r As Object = dtKpi.Rows(0)
            '        lblKpiProduitRentable.Text = Convert.ToString(r("TopProduit"))
            '        lblKpiTotalRecettes.Text = Convert.ToDecimal(r("TotalRecettes")).ToString("N0") & " FC"
            '        lblKpiNombreProduits.Text = Convert.ToString(r("NbProduits"))
            '        lblKpiFaibleRotation.Text = Convert.ToString(r("FaibleRotation"))
            '        lblKpiDormants.Text = Convert.ToString(r("Dormants"))
            '    End If

            '    ' Charts
            '    AlimenterChart(chartTopProduits, ds.Tables("TopVentes"), "Libelle", "TotalVentes")
            '    AlimenterChart(chartCategories, ds.Tables("ParCategorie"), "Categorie", "Nombre")
            '    gridProduitVedette.DataSource = ds.Tables("TopVentes")
            'Catch
            'End Try


            Try
                Dim service As ProduitService = ObtenirService()
                If cmbAnneeDashboard.Items.Count = 0 Then
                    For annee As Integer = Date.Now.Year - 4 To Date.Now.Year
                        cmbAnneeDashboard.Items.Add(annee.ToString())
                    Next
                    cmbAnneeDashboard.Text = Date.Now.Year.ToString()
                End If

                Dim anneeRef As Integer = Convert.ToInt32(cmbAnneeDashboard.Text)
                Dim dtTop As DataTable = service.TopProduitsVendus(anneeRef)
                Dim dtVedette As DataTable = service.ProduitPlusVenduParMois(anneeRef)
                Dim dtCategories As DataTable = service.RepartitionParCategorie()
                Dim dtKpi As DataTable = service.KpiProduits()

                If dtVedette IsNot Nothing AndAlso dtVedette.Columns.Contains("Mois") AndAlso Not dtVedette.Columns.Contains("NomMois") Then
                    dtVedette.Columns.Add("NomMois", GetType(String))
                    For Each row As DataRow In dtVedette.Rows
                        Dim mois As Integer = Convert.ToInt32(row("Mois"))
                        row("NomMois") = Globalization.CultureInfo.GetCultureInfo("fr-FR").DateTimeFormat.GetMonthName(mois)
                    Next
                End If

                gridProduitVedette.DataSource = dtVedette
                ConfigurerGrilleProduitVedetteDashboard()
                AlimenterChart(chartTopProduits, dtTop, "Libelle", "QuantiteVendue")
                AlimenterChartCategories(dtCategories)

                If dtKpi.Rows.Count > 0 Then
                    lblKpiProduitRentable.Text = Convert.ToString(dtKpi.Rows(0)("ProduitPlusRentable"))
                    lblKpiTotalRecettes.Text = Convert.ToDecimal(dtKpi.Rows(0)("TotalRecettes")).ToString("N2")
                    lblKpiNombreProduits.Text = Convert.ToInt32(dtKpi.Rows(0)("NombreTotalProduits")).ToString()
                    lblKpiFaibleRotation.Text = Convert.ToInt32(dtKpi.Rows(0)("FaibleRotation")).ToString()
                    lblKpiDormants.Text = Convert.ToInt32(dtKpi.Rows(0)("ProduitsDormants")).ToString()
                End If
            Catch ex As Exception
                MessageBox.Show("Erreur dashboard produit: " & ex.Message)
            End Try
        End Sub

        Private Sub ConfigurerGrilleProduitVedetteDashboard()
            If gridProduitVedette Is Nothing Then Return
            gridProduitVedette.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

            If gridProduitVedette.Columns.Contains("Mois") Then
                gridProduitVedette.Columns("Mois").Visible = False
            End If

            ConfigurerColonneDashboard("NomMois", "Mois", 22.0F, 80, 0)
            ConfigurerColonneDashboard("Libelle", "Produit / Libellé", 38.0F, 140, 1)
            ConfigurerColonneDashboard("QuantiteVendue", "Quantité", 15.0F, 70, 2)
            ConfigurerColonneDashboard("Recette", "Recette", 25.0F, 100, 3)
            ConfigurerColonneDashboard("TotalRecette", "Recette", 25.0F, 100, 3)
            ConfigurerColonneDashboard("Montant", "Recette", 25.0F, 100, 3)
        End Sub

        Private Sub ConfigurerColonneDashboard(nomColonne As String, entete As String, poids As Single, largeurMin As Integer, ordre As Integer)
            If Not gridProduitVedette.Columns.Contains(nomColonne) Then Return
            Dim col As DataGridViewColumn = gridProduitVedette.Columns(nomColonne)
            col.HeaderText = entete
            col.FillWeight = poids
            col.MinimumWidth = largeurMin
            col.DisplayIndex = Math.Min(ordre, gridProduitVedette.Columns.Count - 1)
        End Sub

        Private Sub AlimenterChart(chart As Chart, dt As DataTable, colX As String, colY As String)
            chart.Series(0).Points.Clear()
            For Each row As DataRow In dt.Rows
                chart.Series(0).Points.AddXY(Convert.ToString(row(colX)), Convert.ToDecimal(row(colY)))
            Next
        End Sub

        Private Sub AlimenterChartCategories(dt As DataTable)
            chartCategories.Series(0).Points.Clear()
            Dim legende As New DataTable()
            legende.Columns.Add("CouleurImage", GetType(Image))
            legende.Columns.Add("Categorie", GetType(String))
            legende.Columns.Add("Valeur", GetType(String))
            legende.Columns.Add("Pourcentage", GetType(String))
            legende.Columns.Add("CouleurArgb", GetType(Integer))

            If dt Is Nothing Then
                gridLegendeCategories.DataSource = legende
                Return
            End If

            Dim total As Decimal = 0D
            For Each row As DataRow In dt.Rows
                total += Convert.ToDecimal(row("NombreProduits"))
            Next

            Dim lignes As IEnumerable(Of DataRow) =
                dt.AsEnumerable().OrderByDescending(Function(r) Convert.ToDecimal(r("NombreProduits")))

            Dim palette As Color() = {
                Color.FromArgb(41, 128, 185),
                Color.FromArgb(39, 174, 96),
                Color.FromArgb(243, 156, 18),
                Color.FromArgb(192, 57, 43),
                Color.FromArgb(142, 68, 173),
                Color.FromArgb(22, 160, 133),
                Color.FromArgb(211, 84, 0),
                Color.FromArgb(52, 73, 94),
                Color.FromArgb(127, 140, 141),
                Color.FromArgb(46, 204, 113),
                Color.FromArgb(52, 152, 219),
                Color.FromArgb(155, 89, 182)
            }
            Dim index As Integer = 0
            For Each row As DataRow In lignes
                Dim categorie As String = Convert.ToString(row("Categorie"))
                If String.IsNullOrWhiteSpace(categorie) Then categorie = "Sans catégorie"
                Dim valeur As Decimal = Convert.ToDecimal(row("NombreProduits"))
                Dim pourcentage As Decimal = If(total > 0D, (valeur / total) * 100D, 0D)
                Dim point As DataPoint = chartCategories.Series(0).Points(chartCategories.Series(0).Points.AddXY(categorie, valeur))
                Dim couleur As Color = palette(index Mod palette.Length)
                point.Color = couleur
                point.AxisLabel = String.Empty
                point.Label = If(index < 5 AndAlso pourcentage >= 8D, pourcentage.ToString("N0") & "%", String.Empty)
                point.LegendText = String.Empty
                point.ToolTip = categorie & " : " & valeur.ToString("N0") & " (" & pourcentage.ToString("N2") & " %)"

                Dim ligne As DataRow = legende.NewRow()
                Dim couleurPoint As Color = chartCategories.Series(0).Points(index).Color
                ligne("CouleurImage") = CreerImageCouleurLegende(couleurPoint)
                ligne("Categorie") = categorie
                ligne("Valeur") = valeur.ToString("N0")
                ligne("Pourcentage") = pourcentage.ToString("N2") & " %"
                ligne("CouleurArgb") = couleurPoint.ToArgb()
                legende.Rows.Add(ligne)
                index += 1
            Next

            gridLegendeCategories.DataSource = legende
        End Sub

        Private Function CreerImageCouleurLegende(couleur As Color) As Image
            Dim image As New Bitmap(18, 18)
            Using g As Graphics = Graphics.FromImage(image)
                g.Clear(Color.Transparent)
                Using b As New SolidBrush(couleur)
                    g.FillRectangle(b, 2, 2, 14, 14)
                End Using
                Using p As New Pen(Color.FromArgb(180, 180, 180))
                    g.DrawRectangle(p, 2, 2, 14, 14)
                End Using
            End Using
            Return image
        End Function

        Private Sub ImprimerListeProduits(sender As Object, e As EventArgs)
            If _produitsView Is Nothing Then
                Return
            End If
            Dim dtPrint As DataTable = _produitsView.ToTable()
            ImprimerTableau("Liste des produits", dtPrint, New String() {"Libelle", "NomCategorie", "CodeBarres", "QuantiteStock", "UnitePrincipale", "PrixAchat", "PrixGros", "PrixDetail", "MargePourcent", "DateExpiration", "VenteDetail", "VenteDemi", "VenteDouzaine", "VenteGros"}, New Integer() {180, 120, 100, 70, 100, 80, 80, 80, 70, 90, 60, 60, 70, 60}, "Catalogue actuel")
        End Sub

        Private Sub ImprimerHistoriquePrix(sender As Object, e As EventArgs)
            If _historiqueTable Is Nothing Then
                ChargerHistoriquePrix(Nothing, EventArgs.Empty)
                If _historiqueTable Is Nothing Then
                    MessageBox.Show("Aucun historique à imprimer.")
                    Return
                End If
            End If
            ImprimerTableau("Historique des prix", _historiqueTable, New String() {"Produit", "TypePrix", "AncienPrix", "NouveauPrix", "ModifieLe", "Utilisateur"}, New Integer() {180, 90, 90, 90, 120, 140}, ObtenirResumeHistoriquePrix())
        End Sub

        Private Sub ImprimerTableau(titre As String, table As DataTable, colonnes As String(), Optional largeurs As Integer() = Nothing, Optional sousTitre As String = "")
            Try
                Dim doc As New PrintDocument()
                If table Is Nothing OrElse table.Rows.Count = 0 Then
                    MessageBox.Show("Aucune donnée à imprimer.", "Impression", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If

                Dim param As ParametreDTO = PrintConfigurationHelper.ConfigurerDocumentA4(doc, Me, "FormulaireProduits", "ImprimerTableau")
                Dim largeurColonnes() As Integer = ConstruireLargeursColonnes(colonnes, largeurs, If(doc.DefaultPageSettings.Landscape, 1000, 760))
                Dim largeurTotale As Integer = 0
                For Each largeur As Integer In largeurColonnes
                    largeurTotale += largeur
                Next
                If largeurTotale > 760 AndAlso Not doc.DefaultPageSettings.Landscape Then
                    doc.DefaultPageSettings.Landscape = True
                    largeurColonnes = ConstruireLargeursColonnes(colonnes, largeurs, 1000)
                End If
                Dim ligneCourante As Integer = 0
                Dim numeroPage As Integer = 1

                AddHandler doc.PrintPage,
                    Sub(s As Object, pe As PrintPageEventArgs)
                        Dim pinceauBleu As New SolidBrush(Color.FromArgb(17, 35, 74))
                        Dim pinceauGris As New SolidBrush(Color.FromArgb(92, 104, 120))
                        Dim fontTitre As New Font("Segoe UI", 16, FontStyle.Bold)
                        Dim fontSousTitre As New Font("Segoe UI", 10, FontStyle.Regular)
                        Dim fontBloc As New Font("Segoe UI", 9.5F, FontStyle.Regular)
                        Dim fontBlocGras As New Font("Segoe UI", 10, FontStyle.Bold)
                        Dim y As Integer = pe.MarginBounds.Top
                        Dim x As Integer = pe.MarginBounds.Left

                        Dim logoPath As String = LogoPathHelper.GetLogoPath(param)
                        If logoPath <> "" AndAlso File.Exists(logoPath) Then
                            Using img As Image = Image.FromFile(logoPath)
                                pe.Graphics.DrawImage(img, x, y, 60, 60)
                            End Using
                            x += 74
                        End If

                        pe.Graphics.DrawString(If(param IsNot Nothing AndAlso param.NomMagasin <> "", param.NomMagasin, "Paons Rehoboth"), fontTitre, pinceauBleu, x, y)
                        y += 24
                        pe.Graphics.DrawString(If(param IsNot Nothing, param.AdresseMagasin, ""), fontSousTitre, pinceauGris, x, y)
                        y += 18
                        pe.Graphics.DrawString(If(param IsNot Nothing, param.TelephoneMagasin, ""), fontSousTitre, pinceauGris, x, y)
                        y += 28

                        pe.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(17, 35, 74)), pe.MarginBounds.Left, y, pe.MarginBounds.Width, 32)
                        pe.Graphics.DrawString(titre, New Font("Segoe UI", 12, FontStyle.Bold), Brushes.White, pe.MarginBounds.Left + 12, y + 7)
                        y += 42

                        Dim boxHeight As Integer = 74
                        Dim boxWidth As Integer = Math.Max(220, Math.Min(380, (pe.MarginBounds.Width - 10) \ 2))
                        pe.Graphics.DrawRectangle(New Pen(Color.FromArgb(210, 219, 232)), pe.MarginBounds.Left, y, boxWidth, boxHeight)
                        pe.Graphics.DrawRectangle(New Pen(Color.FromArgb(210, 219, 232)), pe.MarginBounds.Left + boxWidth + 10, y, boxWidth, boxHeight)
                        pe.Graphics.DrawString("Informations du rapport", fontBlocGras, pinceauBleu, pe.MarginBounds.Left + 12, y + 8)
                        pe.Graphics.DrawString("Titre : " & titre, fontBloc, Brushes.Black, pe.MarginBounds.Left + 12, y + 28)
                        pe.Graphics.DrawString("Lignes : " & table.Rows.Count.ToString(), fontBloc, Brushes.Black, pe.MarginBounds.Left + 12, y + 48)
                        pe.Graphics.DrawString("Période", fontBlocGras, pinceauBleu, pe.MarginBounds.Left + boxWidth + 22, y + 8)
                        pe.Graphics.DrawString(If(String.IsNullOrWhiteSpace(sousTitre), "Toutes les dates", sousTitre), fontBloc, Brushes.Black, pe.MarginBounds.Left + boxWidth + 22, y + 28)
                        pe.Graphics.DrawString("Date impression : " & Date.Now.ToString("dd/MM/yyyy HH:mm"), fontBloc, Brushes.Black, pe.MarginBounds.Left + boxWidth + 22, y + 48)
                        y += boxHeight + 16

                        Dim headerHeight As Integer = 26
                        pe.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(229, 239, 252)), pe.MarginBounds.Left, y, pe.MarginBounds.Width, headerHeight)
                        Dim colX As Integer = pe.MarginBounds.Left
                        For i As Integer = 0 To colonnes.Length - 1
                            pe.Graphics.DrawRectangle(New Pen(Color.FromArgb(210, 219, 232)), colX, y, largeurColonnes(i), headerHeight)
                            pe.Graphics.DrawString(colonnes(i), fontBlocGras, pinceauBleu, New RectangleF(colX + 4, y + 4, largeurColonnes(i) - 8, headerHeight - 8), New StringFormat() With {.Alignment = StringAlignment.Near, .LineAlignment = StringAlignment.Center})
                            colX += largeurColonnes(i)
                        Next
                        y += headerHeight

                        Dim rowHeight As Integer = 22
                        Dim lignesImprimeesSurPage As Integer = 0
                        While ligneCourante < table.Rows.Count
                            If y + rowHeight > pe.MarginBounds.Bottom Then
                                pe.Graphics.DrawString("Page " & numeroPage.ToString(), fontBloc, pinceauGris, pe.MarginBounds.Right - 80, pe.MarginBounds.Bottom + 10)
                                pe.HasMorePages = lignesImprimeesSurPage > 0
                                If pe.HasMorePages Then
                                    numeroPage += 1
                                End If
                                Return
                            End If

                            Dim row As DataRow = table.Rows(ligneCourante)
                            colX = pe.MarginBounds.Left
                            For i As Integer = 0 To colonnes.Length - 1
                                Dim colonne As String = colonnes(i)
                                Dim rect As New Rectangle(colX, y, largeurColonnes(i), rowHeight)
                                pe.Graphics.DrawRectangle(New Pen(Color.FromArgb(232, 236, 242)), rect)
                                Dim text As String = FormaterValeurImpression(row(colonne), colonne)
                                pe.Graphics.DrawString(text, fontBloc, Brushes.Black, New RectangleF(rect.X + 4, rect.Y + 3, rect.Width - 8, rect.Height - 6), FormatString(colonne))
                                colX += largeurColonnes(i)
                            Next
                            y += rowHeight
                            ligneCourante += 1
                            lignesImprimeesSurPage += 1
                        End While

                        pe.Graphics.DrawString("Page " & numeroPage.ToString(), fontBloc, pinceauGris, pe.MarginBounds.Right - 80, pe.MarginBounds.Bottom + 10)
                        pe.HasMorePages = False
                    End Sub

                If param IsNot Nothing AndAlso param.ApercuAvantImpression Then
                    Dim preview As New PrintPreviewDialog() With {.Document = doc, .Width = 1000, .Height = 700}
                    preview.ShowDialog(Me)
                Else
                    doc.Print()
                End If
            Catch ex As Exception
                MessageBox.Show("Erreur impression: " & ex.Message)
            End Try
        End Sub

        Private Function ObtenirResumeHistoriquePrix() As String
            Dim morceaux As New List(Of String)()
            If cmbProduitHistorique.SelectedItem IsNot Nothing Then
                Dim item As ComboProduitItem = DirectCast(cmbProduitHistorique.SelectedItem, ComboProduitItem)
                If item IsNot Nothing AndAlso item.ProduitId > 0 Then
                    morceaux.Add("Produit : " & item.Libelle)
                Else
                    morceaux.Add("Produit : Tous les produits")
                End If
            End If

            If chkFiltreDate.Checked Then
                morceaux.Add("Période : du " & dtpHistoriqueDu.Value.ToString("dd/MM/yyyy") & " au " & dtpHistoriqueAu.Value.ToString("dd/MM/yyyy"))
            Else
                morceaux.Add("Période : Toutes les dates")
            End If

            Return String.Join(" | ", morceaux)
        End Function

        Private Sub ChargerCategories()
            If _categoriesTable Is Nothing Then
                Return
            End If

            Dim source As DataTable = _categoriesTable.Copy()
            Dim ligneVide As DataRow = source.NewRow()
            ligneVide("CategorieId") = DBNull.Value
            ligneVide("NomCategorie") = String.Empty
            source.Rows.InsertAt(ligneVide, 0)

            cmbCategorie.DataSource = source
            cmbCategorie.DisplayMember = "NomCategorie"
            cmbCategorie.ValueMember = "CategorieId"
            cmbCategorie.SelectedIndex = -1
        End Sub

        Private Function LireCategorieSelectionnee() As Integer?
            If cmbCategorie.SelectedValue Is Nothing OrElse TypeOf cmbCategorie.SelectedValue Is DataRowView OrElse Convert.IsDBNull(cmbCategorie.SelectedValue) Then
                Return Nothing
            End If

            Return Convert.ToInt32(cmbCategorie.SelectedValue)
        End Function

        Private Function ConstruireLargeursColonnes(colonnes As String(), largeurs As Integer(), largeurTotale As Integer) As Integer()
            Dim resultat(colonnes.Length - 1) As Integer
            If largeurs IsNot Nothing AndAlso largeurs.Length = colonnes.Length Then
                Dim somme As Integer = 0
                For Each largeur As Integer In largeurs
                    somme += largeur
                Next
                If somme <= 0 Then
                    Dim largeurMoyenne As Integer = Math.Max(1, largeurTotale \ Math.Max(1, colonnes.Length))
                    For i As Integer = 0 To colonnes.Length - 1
                        resultat(i) = largeurMoyenne
                    Next
                Else
                    Dim cumul As Integer = 0
                    For i As Integer = 0 To colonnes.Length - 1
                        If i = colonnes.Length - 1 Then
                            resultat(i) = Math.Max(40, largeurTotale - cumul)
                        Else
                            resultat(i) = Math.Max(40, CInt(Math.Round(largeurTotale * (largeurs(i) / CDbl(somme)))))
                            cumul += resultat(i)
                        End If
                    Next
                End If
            Else
                Dim largeurParDefaut As Integer = Math.Max(1, largeurTotale \ Math.Max(1, colonnes.Length))
                For i As Integer = 0 To colonnes.Length - 1
                    resultat(i) = largeurParDefaut
                Next
            End If

            Return resultat
        End Function

        Private Function FormaterValeurImpression(valeur As Object, colonne As String) As String
            If valeur Is Nothing OrElse Convert.IsDBNull(valeur) Then
                Return ""
            End If

            Dim texteColonne As String = colonne.ToLowerInvariant()
            If TypeOf valeur Is DateTime Then
                Return Convert.ToDateTime(valeur).ToString("dd/MM/yyyy")
            End If
            If TypeOf valeur Is Boolean Then
                Return If(Convert.ToBoolean(valeur), "Oui", "Non")
            End If
            If TypeOf valeur Is Decimal OrElse TypeOf valeur Is Double OrElse TypeOf valeur Is Single OrElse TypeOf valeur Is Integer OrElse TypeOf valeur Is Long Then
                Dim dec As Decimal = Convert.ToDecimal(valeur)
                If texteColonne.Contains("prix") OrElse texteColonne.Contains("montant") OrElse texteColonne.Contains("solde") OrElse texteColonne.Contains("recette") OrElse texteColonne.Contains("stock") OrElse texteColonne.Contains("marge") Then
                    If Math.Abs(dec - Math.Truncate(dec)) < 0.0001D Then
                        Return dec.ToString("N0")
                    End If
                    Return dec.ToString("N2")
                End If
                If Math.Abs(dec - Math.Truncate(dec)) < 0.0001D Then
                    Return dec.ToString("N0")
                End If
                Return dec.ToString("N2")
            End If
            Return Convert.ToString(valeur)
        End Function

        Private Function FormatString(colonne As String) As StringFormat
            Dim fmt As New StringFormat() With {
                .Alignment = If(IsColonneNumerique(colonne), StringAlignment.Far, StringAlignment.Near),
                .LineAlignment = StringAlignment.Center,
                .Trimming = StringTrimming.EllipsisCharacter,
                .FormatFlags = StringFormatFlags.NoWrap
            }
            Return fmt
        End Function

        Private Function IsColonneNumerique(colonne As String) As Boolean
            Dim c As String = colonne.ToLowerInvariant()
            Return c.Contains("prix") OrElse c.Contains("quantite") OrElse c.Contains("stock") OrElse c.Contains("marge") OrElse c.Contains("montant")
        End Function

        Private Sub OuvrirProduitsDormants(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim repo As New ProduitRepository(New DAL(cs))
                Dim dtDormants As DataTable = repo.ListerProduitsDormantsTable()
                Using frm As New FormProduitsDormants(dtDormants)
                    frm.ShowDialog(Me)
                End Using
            Catch ex As Exception
                MessageBox.Show("Impossible d'ouvrir la liste des produits dormants : " & ex.Message)
            End Try
        End Sub

        Private Class FormProduitsDormants
            Inherits Form

            Private ReadOnly _grid As DataGridView
            Private ReadOnly _btnFermer As Button

            Public Sub New(dt As DataTable)
                Me.Text = "Produits dormants"
                Me.StartPosition = FormStartPosition.CenterParent
                Me.Size = New Size(980, 620)
                Me.BackColor = Color.FromArgb(245, 247, 250)
                Me.FormBorderStyle = FormBorderStyle.FixedDialog
                Me.MaximizeBox = False
                Me.MinimizeBox = False

                Dim colorPrimaryLocal As Color = Color.FromArgb(52, 73, 94)
                Dim colorAccentLocal As Color = Color.FromArgb(39, 174, 96)
                Dim colorSelectedLocal As Color = Color.FromArgb(232, 234, 246)

                Dim header As New Panel() With {.Dock = DockStyle.Top, .Height = 60, .BackColor = colorPrimaryLocal, .Padding = New Padding(15, 10, 15, 10)}
                Dim lblTitre As New Label() With {.Text = "Produits dormants", .ForeColor = Color.White, .Font = New Font("Segoe UI", 15, FontStyle.Bold), .AutoSize = True, .Left = 15, .Top = 15}
                _btnFermer = New Button() With {.Text = "Fermer", .Width = 90, .Height = 30, .BackColor = colorAccentLocal, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Anchor = AnchorStyles.Top Or AnchorStyles.Right, .Left = 860, .Top = 15}
                AddHandler _btnFermer.Click, AddressOf Fermer
                header.Controls.Add(lblTitre)
                header.Controls.Add(_btnFermer)

                _grid = New DataGridView() With {
                    .Dock = DockStyle.Fill,
                    .BackgroundColor = Color.White,
                    .BorderStyle = BorderStyle.None,
                    .AllowUserToAddRows = False,
                    .AllowUserToDeleteRows = False,
                    .ReadOnly = True,
                    .RowHeadersVisible = False,
                    .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    .AutoGenerateColumns = False,
                    .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                }
                _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
                _grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
                _grid.ColumnHeadersHeight = 38
                _grid.DefaultCellStyle.Font = New Font("Segoe UI", 9.5F)
                _grid.DefaultCellStyle.SelectionBackColor = colorSelectedLocal
                _grid.DefaultCellStyle.SelectionForeColor = colorPrimaryLocal

                _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "ProduitId", .Visible = False})
                _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "Libelle", .HeaderText = "Libellé produit", .FillWeight = 180})
                _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "CodeBarres", .HeaderText = "Code-barres", .FillWeight = 120})
                _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "Categorie", .HeaderText = "Catégorie", .FillWeight = 120})
                _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "QuantiteStock", .HeaderText = "Stock", .FillWeight = 70})
                _grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "DerniereVente", .HeaderText = "Dernière vente", .FillWeight = 120})

                If dt IsNot Nothing Then
                    _grid.DataSource = dt
                End If

                Dim layout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2}
                layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 60))
                layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
                layout.Controls.Add(header, 0, 0)
                layout.Controls.Add(_grid, 0, 1)

                Me.Controls.Add(layout)
            End Sub

            Private Sub Fermer(sender As Object, e As EventArgs)
                Me.Close()
            End Sub
        End Class
        ' Classes internes pour les combos
        Private Class ComboProduitItem
            Public Property ProduitId As Integer
            Public Property Libelle As String
            Public Sub New(id As Integer, libel As String)
                ProduitId = id : Libelle = libel
            End Sub
        End Class

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            RemoveHandler AppEvents.ProduitModifie, AddressOf RafraichirDepuisEvenement
            RemoveHandler AppEvents.StockModifie, AddressOf RafraichirDepuisEvenement
            MyBase.OnFormClosed(e)
        End Sub
    End Class
End Namespace
