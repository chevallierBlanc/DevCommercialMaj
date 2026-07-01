Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Printing
Imports System.IO
Imports System.Windows.Forms
Imports System.Collections.Generic
Imports System.Windows.Forms.DataVisualization.Charting

Namespace DevCommerc8ak
    Public Class FormulaireApprovisionnement
        Inherits Form

        Private Const TaillePage As Integer = 12

        Private ReadOnly tabs As TabControl
        Private ReadOnly txtRecherche As TextBox
        Private ReadOnly btnNouveau As Button
        Private ReadOnly btnGenerer As Button
        Private ReadOnly btnActualiser As Button
        Private ReadOnly btnReceptionner As Button
        Private ReadOnly btnValiderBon As Button
        Private ReadOnly btnImprimer As Button
        Private ReadOnly btnImprimerGenerationAuto As Button
        Private ReadOnly btnSupprimerBon As Button
        Private ReadOnly btnRetirerLigne As Button
        Private ReadOnly btnPagePrecedente As Button
        Private ReadOnly btnPageSuivante As Button
        Private ReadOnly lblPagination As Label
        Private ReadOnly panelTopActions As Panel
        Private ReadOnly cmbFournisseur As ComboBox
        Private ReadOnly cmbTypePaiement As ComboBox
        Private ReadOnly txtNumeroBon As TextBox
        Private ReadOnly lblTotalBon As Label
        Private ReadOnly lblStatutBon As Label
        Private ReadOnly gridBons As DataGridView
        Private ReadOnly gridLignes As DataGridView
        Private ReadOnly gridSuggestions As DataGridView
        Private ReadOnly txtRechercheProduit As TextBox
        Private ReadOnly txtProduitChoisi As TextBox
        Private ReadOnly txtPrixPrecedent As TextBox
        Private ReadOnly txtPrixAchat As TextBox
        Private ReadOnly txtQuantite As TextBox
        Private ReadOnly lblTotalLigne As Label
        Private ReadOnly btnAjouterLigne As Button
        Private ReadOnly cmbAnnee As ComboBox
        Private ReadOnly cmbMois As ComboBox
        Private ReadOnly gridHistorique As DataGridView
        Private ReadOnly gridTopProduits As DataGridView
        Private ReadOnly chartHistorique As Chart
        Private ReadOnly chartFournisseurs As Chart
        Private ReadOnly timer As Timer
        Private ReadOnly aide As ToolTip
        Private ReadOnly panelHero As Panel
        Private ReadOnly lblHeroTitre As Label
        Private ReadOnly lblHeroSousTitre As Label
        Private ReadOnly grpApproManuel As GroupBox
        Private ReadOnly grpSuggestions As GroupBox
        Private ReadOnly panelHistoriqueFiltres As Panel

        Private _repo As BonApprovisionnementRepository
        Private _dal As DAL
        Private _produits As DataTable
        Private _bonsSource As DataTable
        Private _bonCourantId As Integer
        Private _pageCourante As Integer
        Private _texteRecherche As String
        Private _chargement As Boolean
        Private _dernierBonGenereAutoId As Integer
        Private _bonLigneCouranteId As Integer




        Public Sub New()
            ' --- CONFIGURATION DU DESIGN ---
            'Dim ColorPrimary As Color = Color.FromArgb(63, 81, 181)
            'Dim ColorSecondary As Color = Color.FromArgb(48, 63, 159)
            Dim ColorBackground As Color = Color.FromArgb(245, 247, 250)
            Dim ColorCard As Color = Color.White
            ' Dim FontTitle As New Font("Segoe UI Semibold", 18.0F)
            Dim FontSubTitle As New Font("Segoe UI", 10.0F)
            Dim FontLabel As New Font("Segoe UI Semibold", 9.0F)
            Dim FontControl As New Font("Segoe UI", 9.5F)




            Dim ColorPrimary As Color = Color.FromArgb(52, 73, 94) ' Gris Foncé
            Dim ColorSecondary As Color = Color.FromArgb(41, 128, 185) ' Bleu Moderne
            'Private ReadOnly ColorAccent As Color = Color.FromArgb(39, 174, 96) ' Vert Succès
            'Private ReadOnly ColorDanger As Color = Color.FromArgb(192, 57, 43) ' Rouge Annuler
            'Private ReadOnly ColorBg As Color = Color.FromArgb(245, 247, 250) ' Gris très clair
            'Private ReadOnly ColorWhite As Color = Color.White
            'Private ReadOnly FontMain As New Font("Segoe UI", 10)
            'Private ReadOnly FontBold As New Font("Segoe UI", 10, FontStyle.Bold)
            Dim FontTitle As New Font("Segoe UI", 18.0F, FontStyle.Bold)
            'Private ReadOnly FontTotal As New Font("Segoe UI", 22, FontStyle.Bold)


            Me.Text = "Approvisionnement"
            Me.Width = 1350
            Me.Height = 880
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = ColorBackground
            Me.DoubleBuffered = True

            aide = New ToolTip() With {.IsBalloon = True, .ShowAlways = True}
            _pageCourante = 1
            _texteRecherche = String.Empty
            _chargement = True

            tabs = New TabControl() With {.Dock = DockStyle.Fill, .Padding = New Point(15, 8)}
            Dim tabGestion As New TabPage("Gestion") With {.BackColor = ColorBackground}
            Dim tabAuto As New TabPage("Génération auto") With {.BackColor = ColorBackground}
            Dim tabHistorique As New TabPage("Historique") With {.BackColor = ColorBackground}
            tabs.TabPages.Add(tabGestion)
            tabs.TabPages.Add(tabAuto)
            tabs.TabPages.Add(tabHistorique)

            panelHero = New Panel() With {.Dock = DockStyle.Top, .Height = 90, .BackColor = ColorPrimary}
            lblHeroTitre = New Label() With {.Text = "Gestion des Approvisionnements", .Left = 25, .Top = 18, .AutoSize = True, .Font = FontTitle, .ForeColor = Color.White}
            lblHeroSousTitre = New Label() With {.Text = "Suivi des commandes fournisseurs et gestion des stocks critiques.", .Left = 27, .Top = 54, .AutoSize = True, .Font = FontSubTitle, .ForeColor = Color.FromArgb(210, 210, 255)}
            panelHero.Controls.Add(lblHeroTitre)
            panelHero.Controls.Add(lblHeroSousTitre)

            panelTopActions = New Panel() With {.Dock = DockStyle.Top, .Height = 130, .BackColor = ColorCard}
            txtRecherche = New TextBox() With {.Left = 20, .Top = 40, .Width = 240, .Font = FontControl, .BorderStyle = BorderStyle.FixedSingle}
            txtNumeroBon = New TextBox() With {.Left = 285, .Top = 40, .Width = 150, .ReadOnly = True, .Font = FontControl, .BorderStyle = BorderStyle.FixedSingle, .BackColor = Color.FromArgb(245, 245, 245)}
            cmbFournisseur = New ComboBox() With {.Left = 455, .Top = 40, .Width = 220, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl, .FlatStyle = FlatStyle.Flat}
            cmbTypePaiement = New ComboBox() With {.Left = 695, .Top = 40, .Width = 150, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl, .FlatStyle = FlatStyle.Flat}
            cmbTypePaiement.Items.AddRange(New Object() {"Cash", "Mobile Money", "Virement", "Crédit"})

            btnNouveau = New Button() With {.Text = "Nouveau", .Left = 865, .Top = 32, .Width = 100, .Height = 35, .BackColor = ColorPrimary, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = FontLabel, .Cursor = Cursors.Hand}
            btnNouveau.FlatAppearance.BorderSize = 0
            btnGenerer = New Button() With {.Text = "Génération auto", .Left = 975, .Top = 32, .Width = 130, .Height = 35, .BackColor = ColorSecondary, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = FontLabel, .Cursor = Cursors.Hand}
            btnGenerer.FlatAppearance.BorderSize = 0
            btnActualiser = New Button() With {.Text = "Actualiser", .Left = 1115, .Top = 32, .Width = 100, .Height = 35, .BackColor = Color.Gray, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = FontLabel, .Cursor = Cursors.Hand}
            btnActualiser.FlatAppearance.BorderSize = 0

            btnValiderBon = New Button() With {.Text = "Valider", .Left = 865, .Top = 78, .Width = 100, .Height = 35, .BackColor = Color.ForestGreen, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = FontLabel, .Cursor = Cursors.Hand}
            btnValiderBon.FlatAppearance.BorderSize = 0
            btnReceptionner = New Button() With {.Text = "Réceptionner", .Left = 975, .Top = 78, .Width = 130, .Height = 35, .BackColor = Color.DarkOrange, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = FontLabel, .Cursor = Cursors.Hand}
            btnReceptionner.FlatAppearance.BorderSize = 0
            btnImprimer = New Button() With {.Text = "Imprimer A4", .Left = 1115, .Top = 78, .Width = 100, .Height = 35, .BackColor = Color.SlateGray, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = FontLabel, .Cursor = Cursors.Hand}
            btnImprimer.FlatAppearance.BorderSize = 0
            btnSupprimerBon = New Button() With {.Text = "Supprimer bon", .Left = 1225, .Top = 78, .Width = 110, .Height = 35, .BackColor = Color.Crimson, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = FontLabel, .Cursor = Cursors.Hand, .Visible = True}
            btnSupprimerBon.FlatAppearance.BorderSize = 0

            lblTotalBon = New Label() With {.Text = "Total bon: 0", .Left = 20, .Top = 90, .AutoSize = True, .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold), .ForeColor = ColorPrimary}
            lblStatutBon = New Label() With {.Text = "Statut: EnAttente", .Left = 250, .Top = 92, .AutoSize = True, .Font = New Font("Segoe UI Semibold", 10.0F)}

            panelTopActions.Controls.Add(New Label() With {.Text = "Recherche", .Left = 20, .Top = 15, .AutoSize = True, .Font = FontLabel, .ForeColor = Color.Gray})
            panelTopActions.Controls.Add(New Label() With {.Text = "Numéro bon", .Left = 285, .Top = 15, .AutoSize = True, .Font = FontLabel, .ForeColor = Color.Gray})
            panelTopActions.Controls.Add(New Label() With {.Text = "Fournisseur", .Left = 455, .Top = 15, .AutoSize = True, .Font = FontLabel, .ForeColor = Color.Gray})
            panelTopActions.Controls.Add(New Label() With {.Text = "Paiement", .Left = 695, .Top = 15, .AutoSize = True, .Font = FontLabel, .ForeColor = Color.Gray})
            panelTopActions.Controls.Add(txtRecherche)
            panelTopActions.Controls.Add(txtNumeroBon)
            panelTopActions.Controls.Add(cmbFournisseur)
            panelTopActions.Controls.Add(cmbTypePaiement)
            panelTopActions.Controls.Add(btnNouveau)
            panelTopActions.Controls.Add(btnGenerer)
            panelTopActions.Controls.Add(btnActualiser)
            panelTopActions.Controls.Add(btnValiderBon)
            panelTopActions.Controls.Add(btnReceptionner)
            panelTopActions.Controls.Add(btnImprimer)
            panelTopActions.Controls.Add(btnSupprimerBon)
            panelTopActions.Controls.Add(lblTotalBon)
            panelTopActions.Controls.Add(lblStatutBon)

            gridBons = New DataGridView() With {.Left = 20, .Top = 145, .Width = 1310, .Height = 230, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AutoGenerateColumns = True, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .BackgroundColor = Color.White, .BorderStyle = BorderStyle.None, .EnableHeadersVisualStyles = False, .RowHeadersVisible = False, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right}
            gridBons.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
            gridBons.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            gridBons.ColumnHeadersHeight = 40
            gridBons.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 234, 246)
            gridBons.DefaultCellStyle.SelectionForeColor = ColorPrimary

            btnPagePrecedente = New Button() With {.Text = "<", .Left = 1050, .Top = 385, .Width = 45, .Height = 30, .BackColor = Color.LightGray, .FlatStyle = FlatStyle.Flat}
            lblPagination = New Label() With {.Text = "Page 1/1", .Left = 1110, .Top = 392, .AutoSize = True, .Font = FontLabel}
            btnPageSuivante = New Button() With {.Text = ">", .Left = 1200, .Top = 385, .Width = 45, .Height = 30, .BackColor = Color.LightGray, .FlatStyle = FlatStyle.Flat}

            grpApproManuel = New GroupBox() With {.Text = "Approvisionnement manuel", .Left = 20, .Top = 425, .Width = 1310, .Height = 130, .Font = FontLabel, .ForeColor = ColorPrimary, .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right}
            txtRechercheProduit = New TextBox() With {.Left = 20, .Top = 55, .Width = 180, .Font = FontControl, .BorderStyle = BorderStyle.FixedSingle}
            txtProduitChoisi = New TextBox() With {.Left = 215, .Top = 55, .Width = 250, .Font = FontControl, .BorderStyle = BorderStyle.FixedSingle}
            txtPrixPrecedent = New TextBox() With {.Left = 480, .Top = 55, .Width = 110, .ReadOnly = True, .Font = FontControl, .BorderStyle = BorderStyle.FixedSingle, .BackColor = Color.FromArgb(245, 245, 245)}
            txtPrixAchat = New TextBox() With {.Left = 605, .Top = 55, .Width = 110, .Font = FontControl, .BorderStyle = BorderStyle.FixedSingle}
            txtQuantite = New TextBox() With {.Left = 730, .Top = 55, .Width = 100, .Font = FontControl, .BorderStyle = BorderStyle.FixedSingle}
            lblTotalLigne = New Label() With {.Text = "Total ligne: 0", .Left = 850, .Top = 60, .AutoSize = True, .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)}
            btnAjouterLigne = New Button() With {.Text = "Ajouter ligne", .Left = 980, .Top = 48, .Width = 120, .Height = 35, .BackColor = ColorPrimary, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = FontLabel, .Cursor = Cursors.Hand}
            btnAjouterLigne.FlatAppearance.BorderSize = 0
            btnRetirerLigne = New Button() With {.Text = "Retirer ligne", .Left = 1060, .Top = 48, .Width = 155, .Height = 35, .BackColor = Color.FromArgb(220, 220, 220), .FlatStyle = FlatStyle.Flat, .Font = FontLabel, .Cursor = Cursors.Hand}
            btnRetirerLigne.FlatAppearance.BorderSize = 0

            grpApproManuel.Controls.Add(New Label() With {.Text = "Produit", .Left = 20, .Top = 30, .AutoSize = True, .Font = FontLabel, .ForeColor = Color.Gray})
            grpApproManuel.Controls.Add(New Label() With {.Text = "Choix", .Left = 215, .Top = 30, .AutoSize = True, .Font = FontLabel, .ForeColor = Color.Gray})
            grpApproManuel.Controls.Add(New Label() With {.Text = "Prix précédent", .Left = 480, .Top = 30, .AutoSize = True, .Font = FontLabel, .ForeColor = Color.Gray})
            grpApproManuel.Controls.Add(New Label() With {.Text = "Prix achat", .Left = 605, .Top = 30, .AutoSize = True, .Font = FontLabel, .ForeColor = Color.Gray})
            grpApproManuel.Controls.Add(New Label() With {.Text = "Quantité", .Left = 730, .Top = 30, .AutoSize = True, .Font = FontLabel, .ForeColor = Color.Gray})
            grpApproManuel.Controls.Add(txtRechercheProduit)
            grpApproManuel.Controls.Add(txtProduitChoisi)
            grpApproManuel.Controls.Add(txtPrixPrecedent)
            grpApproManuel.Controls.Add(txtPrixAchat)
            grpApproManuel.Controls.Add(txtQuantite)
            grpApproManuel.Controls.Add(lblTotalLigne)
            grpApproManuel.Controls.Add(btnAjouterLigne)
            grpApproManuel.Controls.Add(btnRetirerLigne)

            gridLignes = New DataGridView() With {.Left = 20, .Top = 565, .Width = 1310, .Height = 190, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AutoGenerateColumns = True, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .BackgroundColor = Color.White, .BorderStyle = BorderStyle.None, .EnableHeadersVisualStyles = False, .RowHeadersVisible = False, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right}
            gridLignes.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
            gridLignes.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            gridLignes.ColumnHeadersHeight = 40

            tabGestion.Controls.Add(panelTopActions)
            tabGestion.Controls.Add(gridBons)
            tabGestion.Controls.Add(btnPagePrecedente)
            tabGestion.Controls.Add(lblPagination)
            tabGestion.Controls.Add(btnPageSuivante)
            tabGestion.Controls.Add(grpApproManuel)
            tabGestion.Controls.Add(gridLignes)

            grpSuggestions = New GroupBox() With {.Text = "Produits critiques à approvisionner", .Left = 20, .Top = 20, .Width = 1280, .Height = 680, .Font = FontLabel, .ForeColor = Color.FromArgb(255, 64, 129)}
            gridSuggestions = New DataGridView() With {.Left = 20, .Top = 40, .Width = 1240, .Height = 580, .ReadOnly = False, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AutoGenerateColumns = True, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .BackgroundColor = Color.White, .BorderStyle = BorderStyle.None, .EnableHeadersVisualStyles = False, .RowHeadersVisible = False, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill}
            gridSuggestions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
            gridSuggestions.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            gridSuggestions.ColumnHeadersHeight = 40
            btnImprimerGenerationAuto = New Button() With {.Text = "Imprimer suggestions", .Left = 20, .Top = 630, .Width = 200, .Height = 35, .BackColor = ColorSecondary, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = FontLabel, .Cursor = Cursors.Hand}
            btnImprimerGenerationAuto.FlatAppearance.BorderSize = 0
            grpSuggestions.Controls.Add(gridSuggestions)
            grpSuggestions.Controls.Add(btnImprimerGenerationAuto)
            tabAuto.Controls.Add(grpSuggestions)

            panelHistoriqueFiltres = New Panel() With {.Dock = DockStyle.Top, .Height = 80, .BackColor = ColorCard}
            cmbAnnee = New ComboBox() With {.Left = 20, .Top = 35, .Width = 120, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl, .FlatStyle = FlatStyle.Flat}
            cmbMois = New ComboBox() With {.Left = 160, .Top = 35, .Width = 150, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl, .FlatStyle = FlatStyle.Flat}
            cmbMois.Items.AddRange(New Object() {"Janvier", "Février", "Mars", "Avril", "Mai", "Juin", "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"})
            panelHistoriqueFiltres.Controls.Add(New Label() With {.Text = "Année", .Left = 20, .Top = 10, .AutoSize = True, .Font = FontLabel, .ForeColor = Color.Gray})
            panelHistoriqueFiltres.Controls.Add(New Label() With {.Text = "Mois", .Left = 160, .Top = 10, .AutoSize = True, .Font = FontLabel, .ForeColor = Color.Gray})
            panelHistoriqueFiltres.Controls.Add(cmbAnnee)
            panelHistoriqueFiltres.Controls.Add(cmbMois)

            gridHistorique = New DataGridView() With {.Left = 20, .Top = 100, .Width = 620, .Height = 300, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AutoGenerateColumns = True, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .BackgroundColor = Color.White, .BorderStyle = BorderStyle.None, .EnableHeadersVisualStyles = False, .RowHeadersVisible = False, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill}
            gridHistorique.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
            gridHistorique.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            gridHistorique.ColumnHeadersHeight = 40
            gridTopProduits = New DataGridView() With {.Left = 660, .Top = 100, .Width = 640, .Height = 300, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AutoGenerateColumns = True, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .BackgroundColor = Color.White, .BorderStyle = BorderStyle.None, .EnableHeadersVisualStyles = False, .RowHeadersVisible = False, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill}
            gridTopProduits.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
            gridTopProduits.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            gridTopProduits.ColumnHeadersHeight = 40
            chartHistorique = New Chart() With {.Left = 20, .Top = 420, .Width = 620, .Height = 300}
            chartFournisseurs = New Chart() With {.Left = 660, .Top = 420, .Width = 640, .Height = 300}

            tabHistorique.Controls.Add(panelHistoriqueFiltres)
            tabHistorique.Controls.Add(gridHistorique)
            tabHistorique.Controls.Add(gridTopProduits)
            tabHistorique.Controls.Add(chartHistorique)
            tabHistorique.Controls.Add(chartFournisseurs)

            Me.Controls.Add(tabs)
            Me.Controls.Add(panelHero)

            ' --- LOGIQUE MÉTIER ORIGINALE (STRICTEMENT INCHANGÉE) ---
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            _dal = New DAL(cs)
            _repo = New BonApprovisionnementRepository(_dal)
            timer = New Timer() With {.Interval = 300}

            AddHandler Me.Load, AddressOf InitialiserFormulaire
            AddHandler btnNouveau.Click, AddressOf NouveauBon
            AddHandler btnGenerer.Click, AddressOf GenererAuto
            AddHandler btnActualiser.Click, AddressOf ChargerBons
            AddHandler btnValiderBon.Click, AddressOf ValiderBon
            AddHandler btnReceptionner.Click, AddressOf ReceptionnerBon
            AddHandler btnImprimer.Click, AddressOf ImprimerBon
            AddHandler btnImprimerGenerationAuto.Click, AddressOf ImprimerSuggestions
            AddHandler btnSupprimerBon.Click, AddressOf SupprimerBon
            AddHandler btnAjouterLigne.Click, AddressOf AjouterLigne
            AddHandler btnRetirerLigne.Click, AddressOf RetirerLigne
            AddHandler btnPagePrecedente.Click, Sub() ChangerPage(-1)
            AddHandler btnPageSuivante.Click, Sub() ChangerPage(1)
            AddHandler txtRecherche.TextChanged, AddressOf DeclencherRecherche
            AddHandler timer.Tick, AddressOf ExecuterRecherche
            AddHandler gridBons.SelectionChanged, AddressOf ChargerLignes
            AddHandler txtRechercheProduit.TextChanged, AddressOf RechercherProduit
            AddHandler txtPrixAchat.TextChanged, AddressOf MettreAJourTotalLigne
            AddHandler txtQuantite.TextChanged, AddressOf MettreAJourTotalLigne
            AddHandler cmbAnnee.SelectedIndexChanged, AddressOf ChargerHistorique
            AddHandler cmbMois.SelectedIndexChanged, AddressOf ChargerHistorique
            AddHandler gridBons.DataBindingComplete, AddressOf ColorerStatuts




        End Sub





        ' --- REPRODUCTION EXACTE DE TOUTES LES MÉTHODES ORIGINALES ---

        Private Sub InitialiserFormulaire(sender As Object, e As EventArgs)
            ChargerFournisseurs()
            ChargerProduits()
            ChargerBons(Nothing, EventArgs.Empty)
            ChargerSuggestions()
            RemplirFiltresHistorique()
            ConfigurerChart(chartHistorique, SeriesChartType.Column, "Montant")
            ConfigurerChart(chartFournisseurs, SeriesChartType.Pie, "Répartition")
            _chargement = False
        End Sub

        Private Sub ChargerBons(sender As Object, e As EventArgs)
            '  _bonsSource = _repo.ListerBons(_texteRecherche)
            ChargerSourceBons(_texteRecherche, True)
            'MettreAJourPagination()
        End Sub
        Private Sub ChargerSourceBons(texte As String, reinitialiserPage As Boolean)
            If texte.Trim() = "" Then
                _bonsSource = _repo.ListerBons()
            Else
                _bonsSource = _repo.RechercherBons(texte)
            End If

            If reinitialiserPage Then
                _pageCourante = 1
            ElseIf _pageCourante < 1 Then
                _pageCourante = 1
            End If

            ' AfficherPageBons()
            MettreAJourPagination()
        End Sub
        Private Sub MettreAJourPagination()
            If _bonsSource Is Nothing Then Return
            Dim total As Integer = _bonsSource.Rows.Count
            Dim nbPages As Integer = Math.Max(1, CInt(Math.Ceiling(total / TaillePage)))
            If _pageCourante > nbPages Then _pageCourante = nbPages
            lblPagination.Text = "Page " & _pageCourante.ToString() & "/" & nbPages.ToString()
            Dim dtPage As DataTable = _bonsSource.Clone()
            Dim debut As Integer = (_pageCourante - 1) * TaillePage
            Dim fin As Integer = Math.Min(debut + TaillePage, total) - 1
            For i As Integer = debut To fin
                dtPage.ImportRow(_bonsSource.Rows(i))
            Next
            gridBons.DataSource = dtPage
            gridBons.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            If gridBons.Columns.Contains("BonId") Then gridBons.Columns("BonId").Visible = False
            If gridBons.Columns.Contains("FournisseurId") Then gridBons.Columns("FournisseurId").Visible = False
            If gridBons.Columns.Contains("NomFournisseur") Then
                gridBons.Columns("NomFournisseur").HeaderText = "Fournisseur"
                gridBons.Columns("NomFournisseur").Width = 180
            End If
            If gridBons.Columns.Contains("TotalBon") Then
                gridBons.Columns("TotalBon").HeaderText = "Total bon"
                gridBons.Columns("TotalBon").DefaultCellStyle.Format = "N0"
                gridBons.Columns("TotalBon").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                gridBons.Columns("TotalBon").Width = 120
            End If
            If gridBons.Columns.Contains("NombreLignes") Then
                gridBons.Columns("NombreLignes").HeaderText = "Lignes"
                gridBons.Columns("NombreLignes").DefaultCellStyle.Format = "N0"
                gridBons.Columns("NombreLignes").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                gridBons.Columns("NombreLignes").Width = 80
            End If
            If gridBons.Columns.Contains("TypePaiement") Then gridBons.Columns("TypePaiement").Width = 100
            If gridBons.Columns.Contains("Statut") Then gridBons.Columns("Statut").Width = 100
        End Sub

        Private Sub ChangerPage(delta As Integer)
            _pageCourante += delta
            If _pageCourante < 1 Then _pageCourante = 1
            MettreAJourPagination()
        End Sub

        Private Sub DeclencherRecherche(sender As Object, e As EventArgs)
            timer.Stop()
            timer.Start()
        End Sub

        Private Sub ExecuterRecherche(sender As Object, e As EventArgs)
            timer.Stop()
            _texteRecherche = txtRecherche.Text.Trim()
            _pageCourante = 1
            ChargerBons(Nothing, EventArgs.Empty)
        End Sub

        Private Sub NouveauBon(sender As Object, e As EventArgs)
            _bonCourantId = _repo.CreerBon(Nothing, "", SessionUtilisateur.UtilisateurId, "EnAttente")
            _bonLigneCouranteId = 0
            txtNumeroBon.Text = ObtenirNumeroBon(_bonCourantId)
            cmbFournisseur.SelectedIndex = -1
            cmbTypePaiement.SelectedIndex = -1
            txtRechercheProduit.Clear()
            txtProduitChoisi.Clear()
            txtPrixPrecedent.Clear()
            txtPrixAchat.Clear()
            txtQuantite.Clear()
            gridLignes.DataSource = _repo.ListerLignes(_bonCourantId)
            ConfigurerGrilleLignes()
            gridLignes.ClearSelection()
            lblTotalBon.Text = "Total bon: 0"
            DefinirStatutAffiche("EnAttente")
            AjouterNotification("Nouveau bon d'approvisionnement créé : " & txtNumeroBon.Text)
            ChargerBons(Nothing, EventArgs.Empty)
            tabs.SelectedIndex = 0
        End Sub

        Private Sub ChargerLignes(sender As Object, e As EventArgs)
            If _chargement Then Return
            If gridBons.CurrentRow Is Nothing OrElse gridBons.CurrentRow.Cells("BonId").Value Is DBNull.Value Then Return
            _bonCourantId = Convert.ToInt32(gridBons.CurrentRow.Cells("BonId").Value)
            txtNumeroBon.Text = Convert.ToString(gridBons.CurrentRow.Cells("NumeroBon").Value)
            If gridBons.CurrentRow.Cells("FournisseurId").Value IsNot DBNull.Value Then
                cmbFournisseur.SelectedValue = Convert.ToInt32(gridBons.CurrentRow.Cells("FournisseurId").Value)
            Else
                cmbFournisseur.SelectedIndex = -1
            End If
            cmbTypePaiement.Text = Convert.ToString(gridBons.CurrentRow.Cells("TypePaiement").Value)
            lblTotalBon.Text = "Total bon: " & Convert.ToDecimal(gridBons.CurrentRow.Cells("TotalBon").Value).ToString("N0")
            DefinirStatutAffiche(Convert.ToString(gridBons.CurrentRow.Cells("Statut").Value))
            gridLignes.DataSource = _repo.ListerLignes(_bonCourantId)
            ConfigurerGrilleLignes()
        End Sub

        Private Sub ReSelectionnerBon(bonId As Integer)
            If gridBons.Rows.Count = 0 Then Return
            For Each row As DataGridViewRow In gridBons.Rows
                If row.IsNewRow Then Continue For
                If row.Cells("BonId").Value Is Nothing OrElse row.Cells("BonId").Value Is DBNull.Value Then Continue For
                If Convert.ToInt32(row.Cells("BonId").Value) = bonId Then
                    gridBons.ClearSelection()
                    row.Selected = True
                    For Each cell As DataGridViewCell In row.Cells
                        If cell.Visible Then
                            gridBons.CurrentCell = cell
                            Exit For
                        End If
                    Next
                    Exit For
                End If
            Next
        End Sub

        Private Sub ConfigurerGrilleLignes()
            If gridLignes.Columns.Count = 0 Then Return

            gridLignes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            gridLignes.ScrollBars = ScrollBars.Both
            If gridLignes.Columns.Contains("BonLigneId") Then gridLignes.Columns("BonLigneId").Visible = False
            If gridLignes.Columns.Contains("ProduitId") Then gridLignes.Columns("ProduitId").Visible = False
            If gridLignes.Columns.Contains("Libelle") Then
                gridLignes.Columns("Libelle").HeaderText = "Produit"
                gridLignes.Columns("Libelle").Width = 260
            End If
            If gridLignes.Columns.Contains("Quantite") Then
                gridLignes.Columns("Quantite").HeaderText = "Qté"
                gridLignes.Columns("Quantite").DefaultCellStyle.Format = "N0"
                gridLignes.Columns("Quantite").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                gridLignes.Columns("Quantite").Width = 90
            End If
            If gridLignes.Columns.Contains("PrixAchatPrecedent") Then
                gridLignes.Columns("PrixAchatPrecedent").HeaderText = "Prix précédent"
                gridLignes.Columns("PrixAchatPrecedent").DefaultCellStyle.Format = "N0"
                gridLignes.Columns("PrixAchatPrecedent").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                gridLignes.Columns("PrixAchatPrecedent").Width = 120
            End If
            If gridLignes.Columns.Contains("PrixAchat") Then
                gridLignes.Columns("PrixAchat").HeaderText = "Prix achat"
                gridLignes.Columns("PrixAchat").DefaultCellStyle.Format = "N0"
                gridLignes.Columns("PrixAchat").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                gridLignes.Columns("PrixAchat").Width = 120
            End If
            If gridLignes.Columns.Contains("TotalLigne") Then
                gridLignes.Columns("TotalLigne").HeaderText = "Total ligne"
                gridLignes.Columns("TotalLigne").DefaultCellStyle.Format = "N0"
                gridLignes.Columns("TotalLigne").DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                gridLignes.Columns("TotalLigne").Width = 130
            End If
        End Sub

        Private Sub ChargerFournisseurs()
            cmbFournisseur.DataSource = _repo.ListerFournisseurs()
            cmbFournisseur.DisplayMember = "NomFournisseur"
            cmbFournisseur.ValueMember = "FournisseurId"
            cmbFournisseur.SelectedIndex = -1
        End Sub

        Private Sub ChargerProduits()
            Dim repoProduit As New ProduitRepository(_dal)
            _produits = repoProduit.ListerTable()
        End Sub

        Private Sub RechercherProduit(sender As Object, e As EventArgs)
            If _produits Is Nothing Then Return
            Dim q As String = txtRechercheProduit.Text.Trim().ToLowerInvariant()
            If q = "" Then
                txtProduitChoisi.Text = ""
                txtPrixPrecedent.Text = ""
                Return
            End If
            For Each row As DataRow In _produits.Rows
                Dim libelle As String = Convert.ToString(row("Libelle"))
                If libelle.ToLowerInvariant().Contains(q) Then
                    txtProduitChoisi.Text = libelle
                    txtPrixPrecedent.Text = ObtenirPrixPrecedent(Convert.ToInt32(row("ProduitId"))).ToString("N2")
                    If txtPrixAchat.Text.Trim() = "" Then txtPrixAchat.Text = txtPrixPrecedent.Text
                    Exit For
                End If
            Next
        End Sub

        Private Sub MettreAJourTotalLigne(sender As Object, e As EventArgs)
            Dim quantite As Decimal = LireDecimal(txtQuantite.Text)
            Dim prix As Decimal = LireDecimal(txtPrixAchat.Text)
            lblTotalLigne.Text = "Total ligne: " & (quantite * prix).ToString("N0")
        End Sub

        Private Sub AjouterLigne(sender As Object, e As EventArgs)
            If _bonCourantId <= 0 Then
                If cmbFournisseur.SelectedIndex < 0 Then
                    MessageBox.Show("Fournisseur obligatoire.")
                    Return
                End If
                _bonCourantId = _repo.CreerBon(LireFournisseurIdSelectionne(), cmbTypePaiement.Text, SessionUtilisateur.UtilisateurId, "EnAttente")
                txtNumeroBon.Text = ObtenirNumeroBon(_bonCourantId)
            End If
            If ObtenirStatutCourant() <> "EnAttente" Then
                MessageBox.Show("Seuls les bons en attente peuvent être modifiés.")
                Return
            End If
            If txtProduitChoisi.Text.Trim() = "" Then
                MessageBox.Show("Produit obligatoire.")
                Return
            End If
            Dim quantite As Decimal = LireDecimal(txtQuantite.Text)
            If quantite <= 0D Then
                MessageBox.Show("Quantité invalide.")
                Return
            End If
            Dim produitId As Integer = ObtenirProduitIdParLibelle(txtProduitChoisi.Text.Trim())
            If produitId <= 0 Then
                MessageBox.Show("Produit introuvable.")
                Return
            End If
            _repo.MettreAJourEntete(_bonCourantId, LireFournisseurIdSelectionne(), cmbTypePaiement.Text)
            _repo.AjouterLigne(_bonCourantId, produitId, quantite, LireDecimal(txtPrixAchat.Text))
            gridLignes.DataSource = _repo.ListerLignes(_bonCourantId)
            ConfigurerGrilleLignes()
            lblTotalBon.Text = "Total bon: " & CalculerTotalBon(_bonCourantId).ToString("N0")
            DefinirStatutAffiche("EnAttente")
            ChargerBons(Nothing, EventArgs.Empty)
        End Sub

        Private Sub RetirerLigne(sender As Object, e As EventArgs)
            If _bonCourantId <= 0 Then Return
            Dim ligneSelectionnee As DataGridViewRow = ObtenirLigneSelectionnee(gridLignes)
            If ligneSelectionnee Is Nothing Then Return
            If ObtenirStatutCourant() <> "EnAttente" Then Return
            Dim ligneId As Integer = Convert.ToInt32(ligneSelectionnee.Cells("BonLigneId").Value)
            Dim rep As DialogResult = MessageBox.Show("Retirer cette ligne du bon ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If rep <> DialogResult.Yes Then Return
            _repo.SupprimerLigne(ligneId)
            gridLignes.DataSource = _repo.ListerLignes(_bonCourantId)
            ConfigurerGrilleLignes()
            lblTotalBon.Text = "Total bon: " & CalculerTotalBon(_bonCourantId).ToString("N0")
            ChargerBons(Nothing, EventArgs.Empty)
        End Sub

        Private Sub SupprimerBon(sender As Object, e As EventArgs)
            If _bonCourantId <= 0 Then Return
            If ObtenirStatutCourant() <> "EnAttente" Then Return
            Dim rep As DialogResult = MessageBox.Show("Supprimer complètement ce bon ?", "Suppression", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            If rep <> DialogResult.Yes Then Return
            _repo.SupprimerBon(_bonCourantId)
            AjouterNotification("Bon supprimé : " & txtNumeroBon.Text)
            ChargerBons(Nothing, EventArgs.Empty)
            _bonCourantId = 0
            _bonLigneCouranteId = 0
            txtNumeroBon.Clear()
            cmbFournisseur.SelectedIndex = -1
            cmbTypePaiement.SelectedIndex = -1
            txtRechercheProduit.Clear()
            txtProduitChoisi.Clear()
            txtPrixPrecedent.Clear()
            txtPrixAchat.Clear()
            txtQuantite.Clear()
            gridLignes.DataSource = Nothing
            lblTotalBon.Text = "Total bon: 0"
            DefinirStatutAffiche("EnAttente")
        End Sub

        Private Sub ValiderBon(sender As Object, e As EventArgs)
            If _bonCourantId <= 0 Then
                MessageBox.Show("Aucun bon sélectionné.")
                Return
            End If
            If cmbFournisseur.SelectedIndex < 0 Then
                MessageBox.Show("Fournisseur obligatoire avant validation.")
                Return
            End If
            If _repo.CompterLignes(_bonCourantId) = 0 Then
                MessageBox.Show("Ajoutez au moins un produit avant validation.")
                Return
            End If
            If ObtenirStatutCourant() = "Livre" Then
                MessageBox.Show("Ce bon est déjà livré.")
                Return
            End If

            Dim rep As DialogResult = MessageBox.Show("Valider ce bon d'approvisionnement ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If rep <> DialogResult.Yes Then
                Return
            End If

            _repo.MettreAJourEntete(_bonCourantId, LireFournisseurIdSelectionne(), cmbTypePaiement.Text)
            _repo.ChangerStatut(_bonCourantId, "Valide")
            DefinirStatutAffiche("Valide")
            AjouterNotification("Bon validé : " & txtNumeroBon.Text)
            ChargerBons(Nothing, EventArgs.Empty)
        End Sub

        Private Sub ReceptionnerBon(sender As Object, e As EventArgs)
            'If _bonCourantId <= 0 OrElse ObtenirStatutCourant() <> "Valide" Then Return
            'Dim rep As DialogResult = MessageBox.Show("Confirmer la réception ?", "Réception", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            'If rep <> DialogResult.Yes Then Return
            '_repo.Receptionner(_bonCourantId)
            'DefinirStatutAffiche("Livre")
            'AjouterNotification("Bon réceptionné : " & txtNumeroBon.Text)
            'ChargerBons(Nothing, EventArgs.Empty)


            If gridBons.CurrentRow Is Nothing Then
                MessageBox.Show("Sélectionnez un bon.")
                Return
            End If
            If ObtenirStatutCourant() <> "Valide" Then
                MessageBox.Show("Seuls les bons validés peuvent être réceptionnés.")
                Return
            End If

            Dim bonId As Integer = Convert.ToInt32(gridBons.CurrentRow.Cells("BonId").Value)
            Dim form As New FormulaireStock()
            form.PrechargerDepuisBonApprovisionnement(bonId)
            form.ShowDialog(Me)

            Dim rep As DialogResult = MessageBox.Show("La réception a-t-elle été enregistrée dans EntréeStock ?", "Confirmation réception", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If rep <> DialogResult.Yes Then
                Return
            End If

            _repo.ChangerStatut(bonId, "Livre")
            DefinirStatutAffiche("Livre")
            AjouterNotification("Bon réceptionné : " & txtNumeroBon.Text)
            ChargerBons(Nothing, EventArgs.Empty)

        End Sub

        Private Sub GenererAuto(sender As Object, e As EventArgs)
            If cmbFournisseur.SelectedIndex < 0 Then
                MessageBox.Show("Sélectionnez un fournisseur.")
                Return
            End If
            ChargerSuggestions()
            If gridSuggestions.Rows.Count = 0 Then
                MessageBox.Show("Aucun produit critique.")
                Return
            End If
            Dim rep As DialogResult = MessageBox.Show("Créer un bon automatique ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If rep <> DialogResult.Yes Then Return
            Dim bonId As Integer = _repo.CreerBon(LireFournisseurIdSelectionne(), cmbTypePaiement.Text, SessionUtilisateur.UtilisateurId, "EnAttente")
            For Each ligne As DataGridViewRow In gridSuggestions.Rows
                Dim qte As Decimal = LireDecimal(Convert.ToString(ligne.Cells("QuantiteSuggeree").Value))
                If qte > 0D Then
                    _repo.AjouterLigne(bonId, Convert.ToInt32(ligne.Cells("ProduitId").Value), qte, LireDecimal(Convert.ToString(ligne.Cells("PrixAchatPrecedent").Value)))
                End If
            Next
            _bonCourantId = bonId
            ChargerBons(Nothing, EventArgs.Empty)
            ChargerLignes(Nothing, EventArgs.Empty)
            tabs.SelectedTab = tabs.TabPages(0)
        End Sub

        Private Sub ImprimerBon(sender As Object, e As EventArgs)


            If _bonCourantId <= 0 Then
                MessageBox.Show("Aucun bon à imprimer.")
                Return
            End If

            Try
                Dim param As ParametreDTO = (New ParametreService(New ParametreRepository(_dal))).Charger()
                Dim dtLignes As DataTable = _repo.ListerLignes(_bonCourantId)
                Dim fournisseur As String = If(cmbFournisseur.Text.Trim() = "", "Sans fournisseur", cmbFournisseur.Text.Trim())
                Dim doc As New PrintDocument()
                If param IsNot Nothing AndAlso param.ImprimanteA4 <> "" Then
                    doc.PrinterSettings.PrinterName = param.ImprimanteA4
                End If
                doc.DefaultPageSettings.Color = If(param IsNot Nothing, param.ImpressionCouleur, True)

                AddHandler doc.PrintPage,
                    Sub(s As Object, pe As PrintPageEventArgs)
                        Dim y As Integer = 30
                        Dim x As Integer = 30
                        Dim pinceauBleu As New SolidBrush(Color.FromArgb(17, 35, 74))
                        Dim pinceauGris As New SolidBrush(Color.FromArgb(92, 104, 120))
                        Dim fontTitre As New Font("Segoe UI", 16, FontStyle.Bold)
                        Dim fontSousTitre As New Font("Segoe UI", 10, FontStyle.Regular)
                        Dim fontBloc As New Font("Segoe UI", 9.5F, FontStyle.Regular)
                        Dim fontBlocGras As New Font("Segoe UI", 10, FontStyle.Bold)

                        Dim logoPath As String = LogoPathHelper.GetLogoPath(param)
                        If logoPath <> "" AndAlso File.Exists(logoPath) Then
                            Using logo As Image = Image.FromFile(logoPath)
                                pe.Graphics.DrawImage(logo, x, y, 70, 70)
                            End Using
                            x += 84
                        End If

                        pe.Graphics.DrawString(If(param IsNot Nothing AndAlso param.NomMagasin <> "", param.NomMagasin, "Paons Rehoboth"), fontTitre, pinceauBleu, x, y)
                        y += 28
                        pe.Graphics.DrawString(If(param IsNot Nothing, param.AdresseMagasin, ""), fontSousTitre, pinceauGris, x, y)
                        y += 18
                        pe.Graphics.DrawString(If(param IsNot Nothing, param.TelephoneMagasin, ""), fontSousTitre, pinceauGris, x, y)
                        y = 118

                        pe.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(17, 35, 74)), 30, y, 760, 32)
                        pe.Graphics.DrawString("BON D'APPROVISIONNEMENT", New Font("Segoe UI", 12, FontStyle.Bold), Brushes.White, 42, y + 7)
                        y += 48

                        pe.Graphics.DrawRectangle(New Pen(Color.FromArgb(210, 219, 232)), 30, y, 360, 92)
                        pe.Graphics.DrawRectangle(New Pen(Color.FromArgb(210, 219, 232)), 430, y, 360, 92)
                        pe.Graphics.DrawString("Informations du bon", fontBlocGras, pinceauBleu, 42, y + 10)
                        pe.Graphics.DrawString("Numéro : " & txtNumeroBon.Text, fontBloc, Brushes.Black, 42, y + 34)
                        pe.Graphics.DrawString("Date : " & Date.Now.ToString("dd/MM/yyyy HH:mm"), fontBloc, Brushes.Black, 42, y + 54)
                        pe.Graphics.DrawString("Statut : " & ObtenirStatutCourant(), fontBloc, Brushes.Black, 42, y + 74)
                        pe.Graphics.DrawString("Fournisseur", fontBlocGras, pinceauBleu, 442, y + 10)
                        pe.Graphics.DrawString(fournisseur, fontBloc, Brushes.Black, 442, y + 34)
                        pe.Graphics.DrawString("Paiement : " & cmbTypePaiement.Text, fontBloc, Brushes.Black, 442, y + 54)
                        pe.Graphics.DrawString("Lignes : " & dtLignes.Rows.Count.ToString(), fontBloc, Brushes.Black, 442, y + 74)
                        y += 116

                        Dim colProduit As Integer = 42
                        Dim colQte As Integer = 420
                        Dim colPrix As Integer = 520
                        Dim colTotal As Integer = 650
                        pe.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(229, 239, 252)), 30, y, 760, 28)
                        pe.Graphics.DrawString("Produit", fontBlocGras, pinceauBleu, colProduit, y + 6)
                        pe.Graphics.DrawString("Quantité", fontBlocGras, pinceauBleu, colQte, y + 6)
                        pe.Graphics.DrawString("Prix achat", fontBlocGras, pinceauBleu, colPrix, y + 6)
                        pe.Graphics.DrawString("Total", fontBlocGras, pinceauBleu, colTotal, y + 6)
                        y += 34

                        For Each row As DataRow In dtLignes.Rows
                            pe.Graphics.DrawLine(New Pen(Color.FromArgb(232, 236, 242)), 30, y + 16, 790, y + 16)
                            pe.Graphics.DrawString(Convert.ToString(row("Libelle")), fontBloc, Brushes.Black, colProduit, y)
                            pe.Graphics.DrawString(Convert.ToDecimal(row("Quantite")).ToString("N2"), fontBloc, Brushes.Black, colQte, y)
                            pe.Graphics.DrawString(Convert.ToDecimal(row("PrixAchat")).ToString("N2"), fontBloc, Brushes.Black, colPrix, y)
                            pe.Graphics.DrawString(Convert.ToDecimal(row("TotalLigne")).ToString("N2"), fontBloc, Brushes.Black, colTotal, y)
                            y += 24
                        Next

                        y += 16
                        pe.Graphics.DrawRectangle(New Pen(Color.FromArgb(17, 35, 74), 1.4F), 520, y, 270, 44)
                        pe.Graphics.DrawString("TOTAL BON", fontBlocGras, pinceauBleu, 536, y + 7)
                        pe.Graphics.DrawString(CalculerTotalBon(_bonCourantId).ToString("N2"), New Font("Segoe UI", 12, FontStyle.Bold), Brushes.Black, 666, y + 8)
                        y += 70

                        pe.Graphics.DrawString("Observation : réception contrôlée selon le bon validé.", fontBloc, pinceauGris, 30, y)
                        y += 38
                        pe.Graphics.DrawLine(Pens.Black, 70, y + 38, 250, y + 38)
                        pe.Graphics.DrawLine(Pens.Black, 530, y + 38, 710, y + 38)
                        pe.Graphics.DrawString("Responsable achat", fontBloc, Brushes.Black, 108, y + 42)
                        pe.Graphics.DrawString("Réception / Magasin", fontBloc, Brushes.Black, 558, y + 42)
                    End Sub

                If param IsNot Nothing AndAlso param.ApercuAvantImpression Then
                    Dim preview As New PrintPreviewDialog() With {.Document = doc, .Width = 1000, .Height = 700}
                    preview.ShowDialog(Me)
                Else
                    doc.Print()
                End If
            Catch ex As Exception
                MessageBox.Show("Erreur impression bon: " & ex.Message)
            End Try


        End Sub

        Private Sub ImprimerSuggestions(sender As Object, e As EventArgs)
            'If gridSuggestions.DataSource Is Nothing Then Return
            'Dim printer As New BonApprovisionnementPrinter(_dal)
            'printer.ImprimerSuggestions(CType(gridSuggestions.DataSource, DataTable))

            If _bonCourantId <= 0 Then
                MessageBox.Show("Aucun bon généré à imprimer.")
                Return
            End If

            ImprimerBon(sender, e)
        End Sub

        ' --- MÉTHODES UTILITAIRES (STRICTEMENT IDENTIQUES À L'ORIGINAL) ---

        Private Function LireDecimal(s As String) As Decimal
            Dim res As Decimal = 0
            Decimal.TryParse(s.Replace(",", "."), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, res)
            Return res
        End Function

        Private Function LireFournisseurIdSelectionne() As Integer
            If cmbFournisseur.SelectedValue Is Nothing Then Return 0
            Return Convert.ToInt32(cmbFournisseur.SelectedValue)
        End Function

        Private Function ObtenirProduitIdParLibelle(libelle As String) As Integer
            If _produits Is Nothing Then Return 0
            For Each row As DataRow In _produits.Rows
                If Convert.ToString(row("Libelle")) = libelle Then Return Convert.ToInt32(row("ProduitId"))
            Next
            Return 0
        End Function

        Private Function ObtenirPrixPrecedent(produitId As Integer) As Decimal
            Dim sql As String = "SELECT TOP 1 PrixAchat FROM StockEntree WHERE ProduitId=@ProduitId ORDER BY DateEntree DESC"
            Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {New System.Data.SqlClient.SqlParameter("@ProduitId", produitId)}
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            If v Is Nothing OrElse v Is DBNull.Value Then
                Return 0D
            End If
            Return Convert.ToDecimal(v)
        End Function

        Private Function CalculerTotalBon(bonId As Integer) As Decimal
            'Dim dt As DataTable = _repo.ListerLignes(bonId)
            'Dim total As Decimal = 0
            'For Each row As DataRow In dt.Rows
            '    total += Convert.ToDecimal(row("SousTotal"))
            'Next
            'Return total

            Dim sql As String = "SELECT ISNULL(SUM(TotalLigne),0) FROM BonApprovisionnementLignes WHERE BonId=@BonId"
            Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {New System.Data.SqlClient.SqlParameter("@BonId", bonId)}
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToDecimal(v)
        End Function

        Private Function ObtenirNumeroBon(bonId As Integer) As String
            Dim dt As DataTable = _repo.ListerBons()
            Dim lignes() As DataRow = dt.Select("BonId = " & bonId.ToString())
            If lignes.Length = 0 Then Return String.Empty
            Return Convert.ToString(lignes(0)("NumeroBon"))

        End Function

        Private Function ObtenirStatutCourant() As String
            Return lblStatutBon.Text.Replace("Statut:", "").Trim()
        End Function

        Private Sub DefinirStatutAffiche(statut As String)
            lblStatutBon.Text = "Statut: " & statut
            Dim bonActif As Boolean = (_bonCourantId > 0)
            btnSupprimerBon.Visible = bonActif AndAlso (statut = "EnAttente")
            btnValiderBon.Visible = bonActif AndAlso (statut = "EnAttente")
            btnReceptionner.Enabled = bonActif AndAlso (statut = "Valide")
            btnRetirerLigne.Enabled = bonActif AndAlso (statut = "EnAttente")
            btnAjouterLigne.Enabled = bonActif AndAlso (statut = "EnAttente")
            txtRechercheProduit.Enabled = bonActif AndAlso (statut = "EnAttente")
            txtProduitChoisi.Enabled = bonActif AndAlso (statut = "EnAttente")
            txtPrixAchat.Enabled = bonActif AndAlso (statut = "EnAttente")
            txtQuantite.Enabled = bonActif AndAlso (statut = "EnAttente")
            Select Case statut
                Case "EnAttente" : lblStatutBon.ForeColor = Color.FromArgb(220, 70, 70)
                Case "Valide" : lblStatutBon.ForeColor = Color.FromArgb(227, 155, 49)
                Case Else : lblStatutBon.ForeColor = Color.FromArgb(42, 168, 94)
            End Select
        End Sub

        Private Sub ColorerStatuts(sender As Object, e As DataGridViewBindingCompleteEventArgs)
            For Each row As DataGridViewRow In gridBons.Rows
                If row.Cells("Statut").Value Is Nothing Then Continue For
                Dim statut As String = Convert.ToString(row.Cells("Statut").Value)
                If statut = "EnAttente" Then
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 240)
                ElseIf statut = "Valide" Then
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 230)
                ElseIf statut = "Livre" Then
                    row.DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240)
                End If
            Next
        End Sub

        Private Sub ChargerSuggestions()
            'gridSuggestions.DataSource = _repo.ListerSuggestions()


            Dim paramService As New ParametreService(New ParametreRepository(_dal))
            Dim p As ParametreDTO = paramService.Charger()
            Dim seuil As Decimal = If(p Is Nothing, 0D, p.SeuilStockCritique)
            gridSuggestions.DataSource = _repo.ListerSuggestionsAuto(seuil)
            For Each nomColonne As String In New String() {"ProduitId", "Libelle", "StockActuel", "SeuilCritique"}
                If gridSuggestions.Columns.Contains(nomColonne) Then
                    gridSuggestions.Columns(nomColonne).ReadOnly = True
                End If
            Next
        End Sub

        Private Sub RemplirFiltresHistorique()
            cmbAnnee.Items.Clear()
            For i As Integer = DateTime.Now.Year To DateTime.Now.Year - 5 Step -1
                cmbAnnee.Items.Add(i)
            Next
            cmbAnnee.SelectedIndex = 0
            cmbMois.SelectedIndex = DateTime.Now.Month - 1
        End Sub

        Private Sub ChargerHistorique(sender As Object, e As EventArgs)
            If _chargement Then Return
            Dim annee As Integer = Convert.ToInt32(cmbAnnee.SelectedItem)
            Dim mois As Integer = cmbMois.SelectedIndex + 1
            Dim dtHist As DataTable = _repo.HistoriqueParPeriode(annee, mois)
            gridHistorique.DataSource = dtHist
            AlimenterChart(chartHistorique, dtHist, "Mois", "TotalApprovisionnement")
            gridTopProduits.DataSource = _repo.ProduitsPlusCommandes(annee, mois)
            AlimenterChart(chartFournisseurs, _repo.RepartitionFournisseurs(annee, mois), "Fournisseur", "NombreBons")



        End Sub

        Private Sub ConfigurerChart(chart As Chart, chartType As SeriesChartType, nomSerie As String)
            chart.ChartAreas.Clear()
            chart.Series.Clear()
            chart.ChartAreas.Add(New ChartArea("Zone"))
            chart.Series.Add(New Series(nomSerie) With {.ChartType = chartType})
        End Sub

        Private Sub AlimenterChart(chart As Chart, dt As DataTable, colonneX As String, colonneY As String)
            'If chart.Series.Count = 0 Then Return
            'chart.Series(0).Points.Clear()
            'For Each row As DataRow In dt.Rows
            '    chart.Series(0).Points.AddXY(Convert.ToString(row(colonneX)), Convert.ToDecimal(row(colonneY)))
            'Next


            If chart.Series.Count = 0 Then
                Return
            End If
            chart.Series(0).Points.Clear()
            For Each row As DataRow In dt.Rows
                chart.Series(0).Points.AddXY(Convert.ToString(row(colonneX)), Convert.ToDecimal(row(colonneY)))
            Next
        End Sub


        Private Sub AjouterNotification(message As String) ''''''
            Dim service As New NotificationService(_dal)
            service.DeclencherEvenementMetier("Info", message, "approvisionnement:" & message, "APPROVISIONNEMENT", "", False, 5)
        End Sub

        Private Function ObtenirLigneSelectionnee(grid As DataGridView) As DataGridViewRow
            If grid.CurrentRow Is Nothing Then Return Nothing
            Return grid.CurrentRow
        End Function

    End Class
End Namespace
