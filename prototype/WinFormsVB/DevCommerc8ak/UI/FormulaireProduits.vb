Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.IO
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting

Namespace DevCommerc8ak
    Public Class FormulaireProduits
        Inherits Form

        Private Const TaillePageProduits As Integer = 14

        Private ReadOnly txtRecherche As TextBox
        Private ReadOnly btnNouveau As Button
        Private ReadOnly btnEnregistrer As Button
        Private ReadOnly btnSupprimer As Button
        Private ReadOnly btnActualiser As Button
        Private ReadOnly btnImprimerProduits As Button
        Private ReadOnly btnImprimerHistorique As Button

        Private ReadOnly txtLibelle As TextBox
        Private ReadOnly txtCodeBarres As TextBox
        Private ReadOnly txtCategorieId As TextBox
        Private ReadOnly chkActif As CheckBox

        Private ReadOnly cmbUnitePrincipale As ComboBox
        Private ReadOnly cmbUniteSecondaire As ComboBox
        Private ReadOnly txtConversion As TextBox

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
        Private ReadOnly lblKpiProduitRentable As Label
        Private ReadOnly lblKpiTotalRecettes As Label
        Private ReadOnly lblKpiNombreProduits As Label
        Private ReadOnly lblKpiFaibleRotation As Label
        Private ReadOnly lblKpiDormants As Label

        Private _produitsTable As DataTable
        Private _historiqueTable As DataTable
        Private _produitsView As DataView
        Private _produitId As Integer
        Private _pageCourante As Integer

        Public Sub New()
            Me.Text = "Produits"
            Me.Width = 1340
            Me.Height = 860
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = Color.FromArgb(232, 238, 248)

            _pageCourante = 1

            panelHero = New Panel() With {.Dock = DockStyle.Top, .Height = 90}
            lblHeroTitre = New Label() With {.Left = 28, .Top = 16, .AutoSize = True, .Font = New Font("Segoe UI Semibold", 18.0F, FontStyle.Bold)}
            lblHeroSousTitre = New Label() With {.Left = 30, .Top = 50, .AutoSize = True, .Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)}
            panelHero.Controls.Add(lblHeroTitre)
            panelHero.Controls.Add(lblHeroSousTitre)

            tabs = New TabControl() With {.Dock = DockStyle.Fill}
            tabProduits = New TabPage("Produits")
            tabHistorique = New TabPage("Historique des prix")
            tabDashboard = New TabPage("Dashboard produit")
            tabs.TabPages.Add(tabProduits)
            tabs.TabPages.Add(tabHistorique)
            tabs.TabPages.Add(tabDashboard)

            Dim panelHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 72}
            panelHeader.Controls.Add(New Label() With {.Text = "Recherche temps réel", .Left = 22, .Top = 10, .AutoSize = True})
            txtRecherche = New TextBox() With {.Left = 22, .Top = 30, .Width = 260}
            btnNouveau = New Button() With {.Text = "Nouveau", .Left = 310, .Top = 26, .Width = 100}
            btnEnregistrer = New Button() With {.Text = "Enregistrer", .Left = 420, .Top = 26, .Width = 120}
            btnSupprimer = New Button() With {.Text = "Supprimer", .Left = 550, .Top = 26, .Width = 110}
            btnActualiser = New Button() With {.Text = "Actualiser", .Left = 670, .Top = 26, .Width = 110}
            btnImprimerProduits = New Button() With {.Text = "Imprimer liste", .Left = 790, .Top = 26, .Width = 120}
            panelHeader.Controls.Add(txtRecherche)
            panelHeader.Controls.Add(btnNouveau)
            panelHeader.Controls.Add(btnEnregistrer)
            panelHeader.Controls.Add(btnSupprimer)
            panelHeader.Controls.Add(btnActualiser)
            panelHeader.Controls.Add(btnImprimerProduits)

            Dim panelEdition As New Panel() With {.Dock = DockStyle.Top, .Height = 280}
            Dim carteInfos As Panel = CreerCarte("Fiche produit", 22, 18, 380, 116)
            Dim cartePrix As Panel = CreerCarte("Prix et options", 418, 18, 430, 160)
            Dim carteStock As Panel = CreerCarte("Stock et seuils", 864, 18, 430, 160)
            Dim carteUnites As Panel = CreerCarte("Unites et conversion", 22, 150, 380, 112)

            txtLibelle = New TextBox() With {.Left = 128, .Top = 32, .Width = 220}
            txtCodeBarres = New TextBox() With {.Left = 128, .Top = 64, .Width = 220}
            txtCategorieId = New TextBox() With {.Left = 128, .Top = 96, .Width = 90}
            chkActif = New CheckBox() With {.Left = 238, .Top = 98, .Text = "Actif", .AutoSize = True}
            carteInfos.Controls.Add(New Label() With {.Text = "Designation", .Left = 18, .Top = 36, .AutoSize = True})
            carteInfos.Controls.Add(New Label() With {.Text = "QR / Code", .Left = 18, .Top = 68, .AutoSize = True})
            carteInfos.Controls.Add(New Label() With {.Text = "Categorie", .Left = 18, .Top = 100, .AutoSize = True})
            carteInfos.Controls.Add(txtLibelle)
            carteInfos.Controls.Add(txtCodeBarres)
            carteInfos.Controls.Add(txtCategorieId)
            carteInfos.Controls.Add(chkActif)

            cmbUnitePrincipale = New ComboBox() With {.Left = 140, .Top = 30, .Width = 190, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbUniteSecondaire = New ComboBox() With {.Left = 140, .Top = 62, .Width = 190, .DropDownStyle = ComboBoxStyle.DropDownList}
            txtConversion = New TextBox() With {.Left = 140, .Top = 94, .Width = 110}
            cmbUnitePrincipale.Items.AddRange(New Object() {"Carton", "Sac", "Paquet", "Pack", "Piece", "Kg", "Bidon", "Sachet"})
            cmbUniteSecondaire.Items.AddRange(New Object() {"Piece", "Demi", "Quart", "Douzaine"})
            carteUnites.Controls.Add(New Label() With {.Text = "Unite principale", .Left = 18, .Top = 34, .AutoSize = True})
            carteUnites.Controls.Add(New Label() With {.Text = "Unite secondaire", .Left = 18, .Top = 66, .AutoSize = True})
            carteUnites.Controls.Add(New Label() With {.Text = "Conversion", .Left = 18, .Top = 98, .AutoSize = True})
            carteUnites.Controls.Add(cmbUnitePrincipale)
            carteUnites.Controls.Add(cmbUniteSecondaire)
            carteUnites.Controls.Add(txtConversion)

            txtPrixAchat = New TextBox() With {.Left = 118, .Top = 30, .Width = 90}
            txtCoeffGros = New TextBox() With {.Left = 320, .Top = 30, .Width = 60}
            btnCalculerPrix = New Button() With {.Text = "Calculer", .Left = 308, .Top = 122, .Width = 90}
            txtPrixGros = New TextBox() With {.Left = 118, .Top = 62, .Width = 90}
            txtPrixUnite = New TextBox() With {.Left = 308, .Top = 62, .Width = 90}
            txtPrixDemi = New TextBox() With {.Left = 118, .Top = 94, .Width = 90}
            txtPrixQuart = New TextBox() With {.Left = 308, .Top = 94, .Width = 90}
            txtPrixDouzaine = New TextBox() With {.Left = 118, .Top = 126, .Width = 90}
            txtPrixSpecial = New TextBox() With {.Left = 308, .Top = 126, .Width = 90}
            chkVenteGros = New CheckBox() With {.Left = 18, .Top = 136, .Text = "Vente gros", .AutoSize = True}
            chkVenteUnite = New CheckBox() With {.Left = 214, .Top = 136, .Text = "Vente detail", .AutoSize = True}
            chkVenteDemi = New CheckBox() With {.Left = 18, .Top = 160, .Text = "Vente demi", .AutoSize = True}
            chkVenteQuart = New CheckBox() With {.Left = 122, .Top = 160, .Text = "Vente quart", .AutoSize = True}
            chkVenteDouzaine = New CheckBox() With {.Left = 238, .Top = 160, .Text = "Vente douzaine", .AutoSize = True}
            cartePrix.Controls.Add(New Label() With {.Text = "Prix achat", .Left = 18, .Top = 34, .AutoSize = True})
            cartePrix.Controls.Add(New Label() With {.Text = "Coef", .Left = 260, .Top = 34, .AutoSize = True})
            cartePrix.Controls.Add(New Label() With {.Text = "Prix gros", .Left = 18, .Top = 66, .AutoSize = True})
            cartePrix.Controls.Add(New Label() With {.Text = "Prix detail", .Left = 214, .Top = 66, .AutoSize = True})
            cartePrix.Controls.Add(New Label() With {.Text = "Prix demi", .Left = 18, .Top = 98, .AutoSize = True})
            cartePrix.Controls.Add(New Label() With {.Text = "Prix quart", .Left = 214, .Top = 98, .AutoSize = True})
            cartePrix.Controls.Add(New Label() With {.Text = "Prix douzaine", .Left = 18, .Top = 130, .AutoSize = True})
            cartePrix.Controls.Add(New Label() With {.Text = "Prix special", .Left = 214, .Top = 130, .AutoSize = True})
            cartePrix.Controls.Add(txtPrixAchat)
            cartePrix.Controls.Add(txtCoeffGros)
            cartePrix.Controls.Add(btnCalculerPrix)
            cartePrix.Controls.Add(txtPrixGros)
            cartePrix.Controls.Add(txtPrixUnite)
            cartePrix.Controls.Add(txtPrixDemi)
            cartePrix.Controls.Add(txtPrixQuart)
            cartePrix.Controls.Add(txtPrixDouzaine)
            cartePrix.Controls.Add(txtPrixSpecial)
            cartePrix.Controls.Add(chkVenteGros)
            cartePrix.Controls.Add(chkVenteUnite)
            cartePrix.Controls.Add(chkVenteDemi)
            cartePrix.Controls.Add(chkVenteQuart)
            cartePrix.Controls.Add(chkVenteDouzaine)

            txtQuantite = New TextBox() With {.Left = 136, .Top = 32, .Width = 110, .ReadOnly = True}
            txtSeuil = New TextBox() With {.Left = 136, .Top = 64, .Width = 110}
            txtMarge = New TextBox() With {.Left = 136, .Top = 96, .Width = 110, .ReadOnly = True}
            dtpExpiration = New DateTimePicker() With {.Left = 136, .Top = 128, .Width = 150, .Format = DateTimePickerFormat.Short}
            carteStock.Controls.Add(New Label() With {.Text = "Stock actuel", .Left = 18, .Top = 36, .AutoSize = True})
            carteStock.Controls.Add(New Label() With {.Text = "Seuil alerte", .Left = 18, .Top = 68, .AutoSize = True})
            carteStock.Controls.Add(New Label() With {.Text = "Marge %", .Left = 18, .Top = 100, .AutoSize = True})
            carteStock.Controls.Add(New Label() With {.Text = "Expiration", .Left = 18, .Top = 132, .AutoSize = True})
            carteStock.Controls.Add(txtQuantite)
            carteStock.Controls.Add(txtSeuil)
            carteStock.Controls.Add(txtMarge)
            carteStock.Controls.Add(dtpExpiration)

            panelEdition.Controls.Add(carteInfos)
            panelEdition.Controls.Add(cartePrix)
            panelEdition.Controls.Add(carteStock)
            panelEdition.Controls.Add(carteUnites)

            grid = New DataGridView() With {
                .Left = 22,
                .Top = 370,
                .Width = 1270,
                .Height = 360,
                .AutoGenerateColumns = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .RowHeadersVisible = False,
                .MultiSelect = False
            }

            btnPagePrecedente = New Button() With {.Text = "<", .Left = 1010, .Top = 744, .Width = 42}
            lblPagination = New Label() With {.Text = "Page 1/1", .Left = 1066, .Top = 752, .AutoSize = True}
            btnPageSuivante = New Button() With {.Text = ">", .Left = 1170, .Top = 744, .Width = 42}

            tabProduits.Controls.Add(panelHeader)
            tabProduits.Controls.Add(panelEdition)
            tabProduits.Controls.Add(grid)
            tabProduits.Controls.Add(btnPagePrecedente)
            tabProduits.Controls.Add(lblPagination)
            tabProduits.Controls.Add(btnPageSuivante)

            Dim panelHistoriqueFiltres As New Panel() With {.Dock = DockStyle.Top, .Height = 74}
            panelHistoriqueFiltres.Controls.Add(New Label() With {.Text = "Produit", .Left = 20, .Top = 12, .AutoSize = True})
            cmbProduitHistorique = New ComboBox() With {.Left = 20, .Top = 32, .Width = 250, .DropDownStyle = ComboBoxStyle.DropDownList}
            chkFiltreDate = New CheckBox() With {.Left = 300, .Top = 34, .Text = "Filtrer par date", .AutoSize = True}
            dtpHistoriqueDu = New DateTimePicker() With {.Left = 430, .Top = 30, .Width = 130, .Format = DateTimePickerFormat.Short}
            dtpHistoriqueAu = New DateTimePicker() With {.Left = 580, .Top = 30, .Width = 130, .Format = DateTimePickerFormat.Short}
            btnImprimerHistorique = New Button() With {.Text = "Imprimer rapport", .Left = 740, .Top = 28, .Width = 140}
            panelHistoriqueFiltres.Controls.Add(cmbProduitHistorique)
            panelHistoriqueFiltres.Controls.Add(chkFiltreDate)
            panelHistoriqueFiltres.Controls.Add(dtpHistoriqueDu)
            panelHistoriqueFiltres.Controls.Add(dtpHistoriqueAu)
            panelHistoriqueFiltres.Controls.Add(btnImprimerHistorique)

            gridHistorique = New DataGridView() With {.Left = 20, .Top = 90, .Width = 1260, .Height = 650, .ReadOnly = True, .AllowUserToAddRows = False, .AutoGenerateColumns = True}
            tabHistorique.Controls.Add(panelHistoriqueFiltres)
            tabHistorique.Controls.Add(gridHistorique)

            Dim panelDashFiltres As New Panel() With {.Dock = DockStyle.Top, .Height = 68}
            panelDashFiltres.Controls.Add(New Label() With {.Text = "Annee", .Left = 20, .Top = 12, .AutoSize = True})
            cmbAnneeDashboard = New ComboBox() With {.Left = 20, .Top = 30, .Width = 120, .DropDownStyle = ComboBoxStyle.DropDownList}
            panelDashFiltres.Controls.Add(cmbAnneeDashboard)

            lblKpiProduitRentable = CreerCarteKpi(tabDashboard, "Produit rentable", 20, 84, 230, 90)
            lblKpiTotalRecettes = CreerCarteKpi(tabDashboard, "Total recettes", 270, 84, 180, 90)
            lblKpiNombreProduits = CreerCarteKpi(tabDashboard, "Nb produits", 470, 84, 150, 90)
            lblKpiFaibleRotation = CreerCarteKpi(tabDashboard, "Faible rotation", 640, 84, 160, 90)
            lblKpiDormants = CreerCarteKpi(tabDashboard, "Dormants", 820, 84, 150, 90)

            gridProduitVedette = New DataGridView() With {.Left = 20, .Top = 196, .Width = 520, .Height = 250, .ReadOnly = True, .AllowUserToAddRows = False, .AutoGenerateColumns = True}
            chartTopProduits = New Chart() With {.Left = 560, .Top = 196, .Width = 720, .Height = 250}
            chartCategories = New Chart() With {.Left = 20, .Top = 470, .Width = 520, .Height = 250}
            ConfigurerChart(chartTopProduits, SeriesChartType.Bar, "TopProduits")
            ConfigurerChart(chartCategories, SeriesChartType.Pie, "Categories")
            tabDashboard.Controls.Add(panelDashFiltres)
            tabDashboard.Controls.Add(gridProduitVedette)
            tabDashboard.Controls.Add(chartTopProduits)
            tabDashboard.Controls.Add(chartCategories)

            Me.Controls.Add(tabs)
            Me.Controls.Add(panelHero)

            AddHandler btnNouveau.Click, AddressOf NouveauProduit
            AddHandler btnEnregistrer.Click, AddressOf EnregistrerProduit
            AddHandler btnSupprimer.Click, AddressOf SupprimerProduit
            AddHandler btnActualiser.Click, AddressOf ChargerDonnees
            AddHandler btnImprimerProduits.Click, AddressOf ImprimerListeProduits
            AddHandler btnImprimerHistorique.Click, AddressOf ImprimerHistoriquePrix
            AddHandler txtRecherche.TextChanged, AddressOf Filtrer
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

            ThemeHelper.AppliquerTheme(Me)
            AppliquerStyle()
            ChargerDonnees(Nothing, EventArgs.Empty)
        End Sub

        Private Function CreerCarte(titre As String, left As Integer, top As Integer, width As Integer, height As Integer) As Panel
            Dim panel As New Panel() With {.Left = left, .Top = top, .Width = width, .Height = height, .BackColor = Color.White}
            panel.BorderStyle = BorderStyle.FixedSingle
            panel.Controls.Add(New Label() With {.Text = titre, .Left = 16, .Top = 10, .AutoSize = True, .Font = New Font("Segoe UI Semibold", 10.0F, FontStyle.Bold), .ForeColor = Color.FromArgb(17, 35, 74)})
            Return panel
        End Function

        Private Function CreerCarteKpi(parent As Control, titre As String, left As Integer, top As Integer, width As Integer, height As Integer) As Label
            Dim carte As New Panel() With {.Left = left, .Top = top, .Width = width, .Height = height, .BackColor = Color.White, .BorderStyle = BorderStyle.FixedSingle}
            Dim lblTitre As New Label() With {.Text = titre, .Left = 12, .Top = 12, .AutoSize = True, .ForeColor = Color.FromArgb(88, 101, 124)}
            Dim lblValeur As New Label() With {.Left = 12, .Top = 42, .AutoSize = True, .Font = New Font("Segoe UI Semibold", 13.0F, FontStyle.Bold), .ForeColor = Color.FromArgb(17, 35, 74)}
            carte.Controls.Add(lblTitre)
            carte.Controls.Add(lblValeur)
            parent.Controls.Add(carte)
            Return lblValeur
        End Function

        Private Sub AppliquerStyle()
            Dim bleuFonce As Color = Color.FromArgb(17, 35, 74)
            Dim bleuClair As Color = Color.FromArgb(80, 170, 255)
            Dim vert As Color = Color.FromArgb(42, 168, 94)
            Dim rouge As Color = Color.FromArgb(220, 70, 70)

            panelHero.BackColor = bleuFonce
            lblHeroTitre.Text = "Catalogue produit et intelligence tarifaire"
            lblHeroTitre.ForeColor = Color.White
            lblHeroSousTitre.Text = "Edition des prix, historique detaille et lecture decisionnelle du portefeuille produit"
            lblHeroSousTitre.ForeColor = Color.FromArgb(207, 220, 246)

            For Each page As TabPage In tabs.TabPages
                page.BackColor = Color.FromArgb(245, 248, 252)
            Next

            StyliserBouton(btnNouveau, bleuClair)
            StyliserBouton(btnEnregistrer, vert)
            StyliserBouton(btnSupprimer, rouge)
            StyliserBouton(btnActualiser, bleuClair)
            StyliserBouton(btnImprimerProduits, bleuClair)
            StyliserBouton(btnImprimerHistorique, bleuClair)
            StyliserBouton(btnPagePrecedente, bleuClair)
            StyliserBouton(btnPageSuivante, bleuClair)
            StyliserBouton(btnCalculerPrix, bleuClair)

            ConfigurerGrilleProduits()
            StyliserGrille(gridHistorique)
            StyliserGrille(gridProduitVedette)
        End Sub

        Private Sub StyliserBouton(bouton As Button, couleur As Color)
            bouton.BackColor = couleur
            bouton.ForeColor = Color.White
            bouton.FlatStyle = FlatStyle.Flat
            bouton.FlatAppearance.BorderSize = 0
            bouton.Cursor = Cursors.Hand
        End Sub

        Private Sub StyliserGrille(grille As DataGridView)
            grille.BackgroundColor = Color.White
            grille.BorderStyle = BorderStyle.None
            grille.GridColor = Color.FromArgb(225, 231, 240)
            grille.EnableHeadersVisualStyles = False
            grille.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(17, 35, 74)
            grille.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
            grille.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.0F, FontStyle.Bold)
            grille.RowTemplate.Height = 28
            grille.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            grille.AllowUserToResizeRows = False
            grille.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 236, 255)
            grille.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 30, 45)
        End Sub

        Private Sub ConfigurerGrilleProduits()
            StyliserGrille(grid)
            grid.Columns.Clear()
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "Libelle", .HeaderText = "Designation", .Width = 180})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "CodeBarres", .HeaderText = "QR / Code", .Width = 110})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "QuantiteStock", .HeaderText = "Stock", .Width = 70})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixAchat", .HeaderText = "Prix achat", .Width = 80})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixGros", .HeaderText = "Gros", .Width = 75})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixDetail", .HeaderText = "Detail", .Width = 75})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixDemi", .HeaderText = "Demi", .Width = 75})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixDouzaine", .HeaderText = "Douzaine", .Width = 85})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixQuart", .HeaderText = "Quart", .Width = 70})
            grid.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "VenteGros", .HeaderText = "VG", .Width = 45})
            grid.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "VenteDetail", .HeaderText = "VD", .Width = 45})
            grid.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "VenteDemi", .HeaderText = "VDe", .Width = 45})
            grid.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "VenteDouzaine", .HeaderText = "VDo", .Width = 45})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "SeuilCritique", .HeaderText = "Seuil", .Width = 60})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "MargePourcent", .HeaderText = "Marge %", .Width = 70})
        End Sub

        Private Function ObtenirService() As ProduitService
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New ProduitRepository(dal)
            Return New ProduitService(repo)
        End Function

        Private Sub ChargerDonnees(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim repo As New ProduitRepository(dal)
                _produitsTable = repo.ListerTable()
                If Not _produitsTable.Columns.Contains("MargePourcent") Then
                    _produitsTable.Columns.Add("MargePourcent", GetType(Decimal))
                End If

                For Each row As DataRow In _produitsTable.Rows
                    Dim prixAchat As Decimal = Convert.ToDecimal(row("PrixAchat"))
                    Dim prixGros As Decimal = Convert.ToDecimal(row("PrixGros"))
                    Dim marge As Decimal = 0D
                    If prixAchat > 0D AndAlso prixGros > 0D Then
                        marge = Math.Round(((prixGros / prixAchat) - 1D) * 100D, 2)
                    End If
                    row("MargePourcent") = marge
                Next

                _produitsView = New DataView(_produitsTable)
                ChargerPageProduits(True)
                RemplirComboProduitsHistorique()
                ChargerHistoriquePrix(Nothing, EventArgs.Empty)
                ChargerDashboard(Nothing, EventArgs.Empty)
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

        Private Sub Filtrer(sender As Object, e As EventArgs)
            If _produitsView Is Nothing Then
                Return
            End If
            Dim q As String = txtRecherche.Text.Trim().Replace("'", "''")
            If q = "" Then
                _produitsView.RowFilter = ""
            Else
                _produitsView.RowFilter = "Libelle LIKE '%" & q & "%' OR CodeBarres LIKE '%" & q & "%'"
            End If
            ChargerPageProduits(True)
        End Sub

        Private Sub PagePrecedente(sender As Object, e As EventArgs)
            If _pageCourante <= 1 Then
                Return
            End If
            _pageCourante -= 1
            ChargerPageProduits(False)
        End Sub

        Private Sub PageSuivante(sender As Object, e As EventArgs)
            _pageCourante += 1
            ChargerPageProduits(False)
        End Sub

        Private Sub NouveauProduit(sender As Object, e As EventArgs)
            _produitId = 0
            txtLibelle.Clear()
            txtCodeBarres.Clear()
            txtCategorieId.Clear()
            txtPrixAchat.Clear()
            txtCoeffGros.Clear()
            txtPrixUnite.Clear()
            txtPrixDemi.Clear()
            txtPrixQuart.Clear()
            txtPrixDouzaine.Clear()
            txtPrixGros.Clear()
            txtPrixSpecial.Clear()
            txtQuantite.Clear()
            txtSeuil.Clear()
            txtMarge.Clear()
            cmbUnitePrincipale.SelectedIndex = -1
            cmbUniteSecondaire.SelectedIndex = -1
            chkActif.Checked = True
            chkVenteGros.Checked = False
            chkVenteUnite.Checked = False
            chkVenteDemi.Checked = False
            chkVenteQuart.Checked = False
            chkVenteDouzaine.Checked = False
            MessageBox.Show("L'ajout direct n'est pas autorisé ici. Sélectionnez un produit existant pour le modifier.")
        End Sub

        Private Sub ChargerSelection(sender As Object, e As EventArgs)
            If grid.CurrentRow Is Nothing Then
                Return
            End If
            Dim row As DataRowView = TryCast(grid.CurrentRow.DataBoundItem, DataRowView)
            If row Is Nothing Then
                Return
            End If
            Dim r As DataRow = row.Row

            _produitId = Convert.ToInt32(row("ProduitId"))
            txtLibelle.Text = Convert.ToString(row("Libelle"))
            txtCodeBarres.Text = Convert.ToString(row("CodeBarres"))
            txtCategorieId.Text = If(r.IsNull("CategorieId"), "", Convert.ToString(row("CategorieId")))
            chkActif.Checked = Convert.ToBoolean(row("EstActif"))
            cmbUnitePrincipale.Text = If(r.IsNull("UnitePrincipale"), "", Convert.ToString(row("UnitePrincipale")))
            cmbUniteSecondaire.Text = If(r.IsNull("UniteSecondaire"), "", Convert.ToString(row("UniteSecondaire")))
            txtConversion.Text = LireDecimalRow(row, "ConversionUnite").ToString("N0")
            txtPrixAchat.Text = LireDecimalRow(row, "PrixAchat").ToString("N0")
            txtCoeffGros.Text = LireDecimalRow(row, "CoefficientGros").ToString("N4")
            txtPrixGros.Text = LireDecimalRow(row, "PrixGros").ToString("N0")
            txtPrixUnite.Text = LireDecimalRow(row, "PrixDetail").ToString("N0")
            txtPrixDemi.Text = LireDecimalRow(row, "PrixDemi").ToString("N0")
            txtPrixQuart.Text = LireDecimalRow(row, "PrixQuart").ToString("N0")
            txtPrixDouzaine.Text = LireDecimalRow(row, "PrixDouzaine").ToString("N0")
            txtPrixSpecial.Text = LireDecimalRow(row, "PrixSpecial").ToString("N0")
            txtQuantite.Text = LireDecimalRow(row, "QuantiteStock").ToString("N0")
            txtSeuil.Text = LireDecimalRow(row, "SeuilCritique").ToString("N0")
            txtMarge.Text = LireDecimalRow(row, "MargePourcent").ToString("N0")
            If r.IsNull("DateExpiration") Then
                dtpExpiration.Value = Date.Now
            Else
                dtpExpiration.Value = Convert.ToDateTime(row("DateExpiration"))
            End If
            chkVenteUnite.Checked = Convert.ToBoolean(row("VenteDetail"))
            chkVenteDemi.Checked = Convert.ToBoolean(row("VenteDemi"))
            chkVenteDouzaine.Checked = Convert.ToBoolean(row("VenteDouzaine"))
            chkVenteGros.Checked = Convert.ToBoolean(row("VenteGros"))
            chkVenteQuart.Checked = LireDecimal(txtPrixQuart.Text) > 0D
        End Sub

        Private Sub EnregistrerProduit(sender As Object, e As EventArgs)
            Try
                If _produitId <= 0 Then
                    MessageBox.Show("Sélectionnez un produit existant à modifier.")
                    Return
                End If
                If Not ValiderFormulaire() Then
                    Return
                End If

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
                    .CategorieId = If(txtCategorieId.Text.Trim() = "", CType(Nothing, Integer?), Convert.ToInt32(txtCategorieId.Text.Trim())),
                    .UnitePrincipale = If(cmbUnitePrincipale.Text.Trim() = "", Nothing, cmbUnitePrincipale.Text.Trim()),
                    .UniteSecondaire = If(cmbUniteSecondaire.Text.Trim() = "", Nothing, cmbUniteSecondaire.Text.Trim()),
                    .ConversionUnite = LireDecimal(txtConversion.Text),
                    .EstActif = chkActif.Checked,
                    .VenteDetail = chkVenteUnite.Checked,
                    .VenteDemi = chkVenteDemi.Checked,
                    .VenteDouzaine = chkVenteDouzaine.Checked,
                    .VenteGros = chkVenteGros.Checked
                }

                service.MettreAJour(produit)
                MessageBox.Show("Produit modifié.")
                ChargerDonnees(sender, e)
            Catch ex As Exception
                MessageBox.Show("Erreur enregistrement produit: " & ex.Message)
            End Try
        End Sub

        Private Sub SupprimerProduit(sender As Object, e As EventArgs)
            Try
                If _produitId <= 0 Then
                    MessageBox.Show("Sélectionnez un produit.")
                    Return
                End If
                Dim rep As DialogResult = MessageBox.Show("Voulez-vous supprimer ce produit ?", "Suppression produit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                If rep <> DialogResult.Yes Then
                    Return
                End If

                Dim service As ProduitService = ObtenirService()
                service.Supprimer(_produitId)
                _produitId = 0
                ChargerDonnees(sender, e)
            Catch ex As Exception
                MessageBox.Show("Erreur suppression produit: " & ex.Message)
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
            For Each zone As TextBox In New TextBox() {txtPrixAchat, txtPrixGros, txtPrixUnite, txtPrixDemi, txtPrixQuart, txtPrixDouzaine, txtPrixSpecial, txtSeuil}
                Dim valeurControlee As Decimal
                If zone.Text.Trim() <> "" AndAlso Not Decimal.TryParse(zone.Text.Trim(), valeurControlee) Then
                    MessageBox.Show("Un prix ou seuil n'est pas valide.")
                    Return False
                End If
            Next
            Return True
        End Function

        Private Function LireDecimal(texte As String) As Decimal
            Dim v As Decimal
            If Decimal.TryParse(If(texte.Trim() = "", "0", texte.Trim()), v) Then
                Return v
            End If
            Return 0D
        End Function

        Private Function LireDecimalRow(row As DataRowView, colonne As String) As Decimal
            Dim r As DataRow = row.Row
            If row Is Nothing OrElse r.IsNull(colonne) Then
                Return 0D
            End If
            Return Convert.ToDecimal(row(colonne))
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
                txtMarge.Text = Math.Round(((prixGros / prixAchat) - 1D) * 100D, 2).ToString("N0")
            Else
                txtMarge.Text = "0"
            End If
        End Sub

        Private Sub CalculerPrixAuto(sender As Object, e As EventArgs)
            Dim prixAchat As Decimal = LireDecimal(txtPrixAchat.Text)
            Dim coeff As Decimal = LireDecimal(txtCoeffGros.Text)
            If prixAchat <= 0D Then
                MessageBox.Show("Prix d'achat obligatoire pour le calcul automatique.")
                Return
            End If
            If coeff <= 0D Then
                MessageBox.Show("Coefficient invalide.")
                Return
            End If

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

            txtPrixGros.Text = prixGros.ToString("N0")
            txtPrixDemi.Text = prixDemi.ToString("N0")
            txtPrixQuart.Text = prixQuart.ToString("N0")
            txtPrixUnite.Text = prixUnite.ToString("N0")
            txtPrixDouzaine.Text = prixDouzaine.ToString("N0")
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

        Private Sub ChargerHistoriquePrix(sender As Object, e As EventArgs)
            Try
                Dim service As ProduitService = ObtenirService()
                Dim produitId As Integer? = Nothing
                If cmbProduitHistorique.SelectedItem IsNot Nothing Then
                    Dim item As ComboProduitItem = DirectCast(cmbProduitHistorique.SelectedItem, ComboProduitItem)
                    If item.ProduitId > 0 Then
                        produitId = item.ProduitId
                    End If
                End If
                Dim dateDu As Date? = Nothing
                Dim dateAu As Date? = Nothing
                If chkFiltreDate.Checked Then
                    dateDu = dtpHistoriqueDu.Value.Date
                    dateAu = dtpHistoriqueAu.Value.Date
                End If
                _historiqueTable = service.ListerHistoriquePrixTable(produitId, dateDu, dateAu)
                gridHistorique.DataSource = _historiqueTable
                StyliserGrille(gridHistorique)
            Catch ex As Exception
                MessageBox.Show("Erreur chargement historique: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerDashboard(sender As Object, e As EventArgs)
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

                gridProduitVedette.DataSource = dtVedette
                StyliserGrille(gridProduitVedette)
                AlimenterChart(chartTopProduits, dtTop, "Libelle", "QuantiteVendue")
                AlimenterChart(chartCategories, dtCategories, "Categorie", "NombreProduits")

                If dtKpi.Rows.Count > 0 Then
                    lblKpiProduitRentable.Text = Convert.ToString(dtKpi.Rows(0)("ProduitPlusRentable"))
                    lblKpiTotalRecettes.Text = FormatageGlobal.FormatMontant(Convert.ToDecimal(dtKpi.Rows(0)("TotalRecettes")))
                    lblKpiNombreProduits.Text = Convert.ToInt32(dtKpi.Rows(0)("NombreTotalProduits")).ToString()
                    lblKpiFaibleRotation.Text = Convert.ToInt32(dtKpi.Rows(0)("FaibleRotation")).ToString()
                    lblKpiDormants.Text = Convert.ToInt32(dtKpi.Rows(0)("ProduitsDormants")).ToString()
                End If
            Catch ex As Exception
                MessageBox.Show("Erreur dashboard produit: " & ex.Message)
            End Try
        End Sub

        Private Sub ConfigurerChart(chart As Chart, typeSerie As SeriesChartType, nomSerie As String)
            chart.ChartAreas.Clear()
            chart.Series.Clear()
            chart.Titles.Clear()
            chart.BackColor = Color.White
            Dim area As New ChartArea("Zone")
            area.BackColor = Color.White
            chart.ChartAreas.Add(area)
            chart.Series.Add(New Series(nomSerie) With {.ChartType = typeSerie, .IsValueShownAsLabel = True})
        End Sub

        Private Sub AlimenterChart(chart As Chart, table As DataTable, colonneX As String, colonneY As String)
            If chart.Series.Count = 0 Then
                Return
            End If
            chart.Series(0).Points.Clear()
            For Each row As DataRow In table.Rows
                chart.Series(0).Points.AddXY(Convert.ToString(row(colonneX)), Convert.ToDecimal(row(colonneY)))
            Next
        End Sub

        Private Sub ImprimerListeProduits(sender As Object, e As EventArgs)
            If _produitsView Is Nothing Then
                Return
            End If
            Dim dtPrint As DataTable = _produitsView.ToTable()
            ImprimerTableau("Liste des produits", dtPrint, New String() {"Libelle", "CodeBarres", "QuantiteStock", "PrixAchat", "PrixGros", "PrixDetail", "PrixDemi", "PrixDouzaine", "PrixQuart", "MargePourcent"})
        End Sub

        Private Sub ImprimerHistoriquePrix(sender As Object, e As EventArgs)
            If _historiqueTable Is Nothing Then
                Return
            End If
            ImprimerTableau("Historique des prix", _historiqueTable, New String() {"Produit", "TypePrix", "AncienPrix", "NouveauPrix", "ModifieLe", "Utilisateur"})
        End Sub

        Private Sub ImprimerTableau(titre As String, table As DataTable, colonnes As String())
            Try
                Dim dal As New DAL(ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString)
                Dim param As ParametreDTO = (New ParametreService(New ParametreRepository(dal))).Charger()
                Dim doc As New PrintDocument()
                If param IsNot Nothing AndAlso param.ImprimanteA4 <> "" Then
                    doc.PrinterSettings.PrinterName = param.ImprimanteA4
                End If
                doc.DefaultPageSettings.Color = If(param IsNot Nothing, param.ImpressionCouleur, True)

                AddHandler doc.PrintPage,
                    Sub(s As Object, pe As PrintPageEventArgs)
                        Dim y As Integer = 30
                        If param IsNot Nothing AndAlso param.LogoPath <> "" AndAlso File.Exists(param.LogoPath) Then
                            Using img As Image = Image.FromFile(param.LogoPath)
                                pe.Graphics.DrawImage(img, 30, y, 60, 60)
                            End Using
                        End If
                        pe.Graphics.DrawString(If(param IsNot Nothing, param.NomMagasin, "Paons Rehoboth"), New Font("Segoe UI", 15, FontStyle.Bold), Brushes.Black, 105, y)
                        y += 24
                        pe.Graphics.DrawString(If(param IsNot Nothing, param.AdresseMagasin, ""), New Font("Segoe UI", 9), Brushes.Black, 105, y)
                        y += 18
                        pe.Graphics.DrawString(If(param IsNot Nothing, param.TelephoneMagasin, ""), New Font("Segoe UI", 9), Brushes.Black, 105, y)
                        y += 34
                        pe.Graphics.DrawString(titre, New Font("Segoe UI", 12, FontStyle.Bold), Brushes.Black, 30, y)
                        y += 28

                        Dim x As Integer = 30
                        For Each col As String In colonnes
                            pe.Graphics.DrawString(col, New Font("Segoe UI", 9, FontStyle.Bold), Brushes.Black, x, y)
                            x += 120
                        Next
                        y += 22

                        For Each row As DataRow In table.Rows
                            x = 30
                            For Each col As String In colonnes
                                pe.Graphics.DrawString(Convert.ToString(row(col)), New Font("Segoe UI", 8.5F), Brushes.Black, x, y)
                                x += 120
                            Next
                            y += 20
                            If y > 1020 Then
                                Exit For
                            End If
                        Next
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

        Private NotInheritable Class ComboProduitItem
            Public Sub New(produitIdValue As Integer, libelleValue As String)
                ProduitId = produitIdValue
                Libelle = libelleValue
            End Sub

            Public ReadOnly Property ProduitId As Integer
            Public ReadOnly Property Libelle As String
        End Class
    End Class
End Namespace
