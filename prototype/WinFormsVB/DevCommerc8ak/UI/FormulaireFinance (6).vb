Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Printing
Imports System.IO
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting
Imports DevCommerc8ak.DevCommerc8ak.DTO
Imports DevCommerc8ak.DevCommerc8ak.Finance

Imports DevCommerc8ak.DevCommerc8ak.Services
Imports Microsoft.VisualBasic



Namespace DevCommerc8ak
    Public Class FormulaireFinance
        Inherits Form
        ' --- Services ---
        Private _depenseService As DepenseServiceFinance
        Private _caisseService As CaisseService
        Private _banqueService As BanqueService
        Private _catService As CategorieDepenseService

        ' --- Constantes de Design ---
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

        Private ReadOnly FontTitle As New Font("Segoe UI", 18.0F, FontStyle.Bold)
        Private ReadOnly FontSubtitle As New Font("Segoe UI", 10.0F, FontStyle.Regular)
        Private ReadOnly FontLabel As New Font("Segoe UI", 10.0F, FontStyle.Regular)
        Private ReadOnly FontControl As New Font("Segoe UI", 9.5F)
        Private ReadOnly FontButton As New Font("Segoe UI", 10.0F, FontStyle.Bold)
        Private ReadOnly FontValue As New Font("Segoe UI", 22.0F, FontStyle.Bold)
        Private ReadOnly FontTab As New Font("Segoe UI", 10.0F, FontStyle.Bold)

        ' --- Composants UI ---
        Private ReadOnly tabControlFinance As TabControl
        Private ReadOnly tpDepenses As TabPage
        Private ReadOnly tpCaisse As TabPage
        Private ReadOnly tpBanque As TabPage
        Private ReadOnly tpDashboard As TabPage

        ' Onglet Dépenses
        Private txtMontantDepense As TextBox
        Private cmbCategorieDepense As ComboBox
        Private btnAddCategorie As Button
        Private cmbDeviseDepense As ComboBox
        Private cmbSourceDepense As ComboBox
        Private cmbTypeDepense As ComboBox
        Private txtDescriptionDepense As TextBox
        Private dtpDateDepense As DateTimePicker
        Private btnValiderDepense As Button
        Private gridHistoriqueDepenses As DataGridView

        ' Onglet Caisse
        Private lblEncaisseFC As Label
        Private lblEncaisseUSD As Label
        Private lblDepensesCaisseFC As Label
        Private lblDepensesCaisseUSD As Label
        Private lblSoldeCaisseFC As Label
        Private lblSoldeCaisseUSD As Label
        Private lblStatusCloture As Label

        ' Onglet Banque
        Private lblSoldeBanqueFC As Label
        Private lblSoldeBanqueUSD As Label
        Private gridHistoriqueBanque As DataGridView
        Private cmbFiltreBanque As ComboBox
        Private cmbAnneeBanque As ComboBox
        Private cmbMoisBanque As ComboBox
        Private dtpJourBanque As DateTimePicker
        Private lblBanqueFiltre As Label
        Private lblBanqueAnnee As Label
        Private lblBanqueMois As Label
        Private lblBanqueJour As Label

        ' Onglet Dashboard
        Private chartDepensesCat As Chart
        Private chartEvolutionFinance As Chart
        Private lblTotalEncaisse As Label
        Private lblTotalDepenses As Label
        Private lblSoldeGlobalBanque As Label

        ' Impression
        Private ReadOnly printDoc As New PrintDocument()
        Private ReadOnly printPreview As New PrintPreviewDialog()
        Private dtRapportAImprimer As DataTable
        Private titreRapport As String = ""
        Private _parametres As ParametreDTO
        Private _impressionIndex As Integer
        Private _impressionTotalFC As Decimal
        Private _impressionTotalUSD As Decimal
        Private _banqueUsdIndisponibleLoggee As Boolean

        ' Filtres Impression
        Private cmbAnneeRapport As ComboBox
        Private cmbMoisRapport As ComboBox
        Private btnImprimerRapport As Button

        Public Sub New()
            InitialiserServices()

            Me.Text = "Gestion Financière - Paon Rehoboth"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.WindowState = FormWindowState.Maximized
            Me.BackColor = ColorBg
            Me.DoubleBuffered = True

            ' --- Header ---
            Dim pnlHeader As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 70,
                .BackColor = ColorOriginalHeaderBg ' Couleur originale
            }
            Dim lblTitle As New Label() With {
                .Text = "Gestion Financière",
                .Left = 25,
                .Top = 15,
                .AutoSize = True,
                .Font = FontTitle,
                .ForeColor = Color.White ' Couleur originale
            }
            pnlHeader.Controls.Add(lblTitle)

            ' --- TabControl ---
            tabControlFinance = New TabControl() With {
                .Dock = DockStyle.Fill,
                .Padding = New Point(0, 0),
                .Margin = New Padding(24, 0, 24, 24) ' Marge autour des onglets
            }
            tabControlFinance.Appearance = TabAppearance.FlatButtons
            tabControlFinance.ItemSize = New Size(0, 1) ' Cache les en-têtes d'onglets
            tabControlFinance.SizeMode = TabSizeMode.Fixed
            AddHandler tabControlFinance.GotFocus, AddressOf Tabs_GotFocus ' Empêche le focus sur les onglets

            tpDepenses = New TabPage("Dépenses") With {.BackColor = ColorBg, .Padding = New Padding(0)}
            tpCaisse = New TabPage("Caisse Journalière") With {.BackColor = ColorBg, .Padding = New Padding(0)}
            tpBanque = New TabPage("Banque") With {.BackColor = ColorBg, .Padding = New Padding(0)}
            tpDashboard = New TabPage("Dashboard Financier") With {.BackColor = ColorBg, .Padding = New Padding(0)}

            tabControlFinance.TabPages.AddRange(New TabPage() {tpDepenses, tpCaisse, tpBanque, tpDashboard})

            ' --- NAVIGATION PAR ONGLET PERSONNALISÉE ---
            Dim pnlTabNavigation As New FlowLayoutPanel() With {
                .Dock = DockStyle.Top,
                .Height = 50,
                .FlowDirection = FlowDirection.LeftToRight,
                .Padding = New Padding(24, 0, 0, 0),
                .Margin = New Padding(0, 0, 0, 0),
                .BackColor = ColorBg
            }

            Dim btnTabDepenses As New Button() With {
                .Text = "🧾 Dépenses",
                .Width = 150,
                .Height = 40,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontTab,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 8, 0)
            }
            AddHandler btnTabDepenses.Click, Sub() SetSelectedTab(0)

            Dim btnTabCaisse As New Button() With {
                .Text = "💰 Caisse Journalière",
                .Width = 180,
                .Height = 40,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontTab,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 8, 0)
            }
            AddHandler btnTabCaisse.Click, Sub() SetSelectedTab(1)

            Dim btnTabBanque As New Button() With {
                .Text = "🏦 Banque",
                .Width = 120,
                .Height = 40,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontTab,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 8, 0)
            }
            AddHandler btnTabBanque.Click, Sub() SetSelectedTab(2)

            Dim btnTabDashboard As New Button() With {
                .Text = "📊 Dashboard Financier",
                .Width = 200,
                .Height = 40,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontTab,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 0, 0)
            }
            AddHandler btnTabDashboard.Click, Sub() SetSelectedTab(3)

            pnlTabNavigation.Controls.Add(btnTabDepenses)
            pnlTabNavigation.Controls.Add(btnTabCaisse)
            pnlTabNavigation.Controls.Add(btnTabBanque)
            pnlTabNavigation.Controls.Add(btnTabDashboard)

            ' --- LAYOUT PRINCIPAL ---
            Dim mainLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3,
                .Padding = New Padding(0)
            }
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 70)) ' Header
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 50)) ' Tab Navigation
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100)) ' Content (Tabs)
            mainLayout.BackColor = ColorBg

            mainLayout.Controls.Add(pnlHeader, 0, 0)
            mainLayout.Controls.Add(pnlTabNavigation, 0, 1)
            mainLayout.Controls.Add(tabControlFinance, 0, 2)
            Me.Controls.Add(mainLayout)

            InitOngletDepenses()
            InitOngletCaisse()
            InitOngletBanque()
            InitOngletDashboard()
            ChargerParametresApplication()

            ' Configuration Impression
            AddHandler printDoc.PrintPage, AddressOf PrintDoc_PrintPage
            printPreview.Document = printDoc

            ClotureAutomatique()
            ChargerDonnees()
            SetSelectedTab(0) ' Sélectionne l'onglet Dépenses par défaut
        End Sub

        Private Sub InitialiserServices()
            Dim connectionString As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(connectionString)

            Dim catRepo As New CategorieDepenseRepository(dal)
            Dim depRepo As New DepenseRepositoryFinance(dal)
            Dim banqueRepo As New BanqueRepository(dal)
            Dim caisseRepo As New CaisseRepository(dal)

            _catService = New CategorieDepenseService(catRepo)
            _banqueService = New BanqueService(banqueRepo)
            _caisseService = New CaisseService(caisseRepo, depRepo, _banqueService)
            _depenseService = New DepenseServiceFinance(depRepo, _banqueService, _caisseService)
        End Sub

        ' --- INITIALISATION DES ONGLETS ---

        Private Sub InitOngletDepenses()
            tpDepenses.BackColor = ColorBg
            Dim mainLayoutDepenses As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1, .Padding = New Padding(24), .BackColor = ColorBg}
            mainLayoutDepenses.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 460))
            mainLayoutDepenses.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

            ' Formulaire de saisie (Carte)
            Dim pnlSaisie As Panel = CreerCarte()
            pnlSaisie.Margin = New Padding(0, 0, 16, 0)
            pnlSaisie.Padding = New Padding(20)

            Dim layoutSaisie As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 12,
                .Padding = New Padding(0)
            }
            layoutSaisie.RowStyles.Add(New RowStyle(SizeType.Absolute, 32)) ' Titre
            layoutSaisie.RowStyles.Add(New RowStyle(SizeType.AutoSize)) ' Label Catégorie
            layoutSaisie.RowStyles.Add(New RowStyle(SizeType.Absolute, 32)) ' ComboBox Catégorie
            layoutSaisie.RowStyles.Add(New RowStyle(SizeType.AutoSize)) ' Label Montant
            layoutSaisie.RowStyles.Add(New RowStyle(SizeType.Absolute, 34)) ' Montant + Devise
            layoutSaisie.RowStyles.Add(New RowStyle(SizeType.AutoSize)) ' Label Source/Type
            layoutSaisie.RowStyles.Add(New RowStyle(SizeType.Absolute, 34)) ' Source + Type
            layoutSaisie.RowStyles.Add(New RowStyle(SizeType.AutoSize)) ' Label Description
            layoutSaisie.RowStyles.Add(New RowStyle(SizeType.Absolute, 64)) ' TextBox Description
            layoutSaisie.RowStyles.Add(New RowStyle(SizeType.AutoSize)) ' Label Date
            layoutSaisie.RowStyles.Add(New RowStyle(SizeType.Absolute, 28)) ' DatePicker
            layoutSaisie.RowStyles.Add(New RowStyle(SizeType.Absolute, 42)) ' Bouton Valider
            layoutSaisie.BackColor = Color.Transparent

            Dim lblSaisieTitle As New Label() With {.Text = "Nouvelle Dépense", .Font = FontTitle, .ForeColor = ColorPrimary, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}
            layoutSaisie.Controls.Add(lblSaisieTitle, 0, 0)

            layoutSaisie.Controls.Add(CreateLabel("Catégorie"), 0, 1)
            Dim pnlCat As New Panel() With {.Dock = DockStyle.Fill, .Height = 35}
            cmbCategorieDepense = New ComboBox() With {.Dock = DockStyle.Fill, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl, .Margin = New Padding(0, 0, 8, 0)}
            btnAddCategorie = New Button() With {.Text = "+", .Dock = DockStyle.Right, .Width = 35, .BackColor = ColorAccent, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
            btnAddCategorie.FlatAppearance.BorderSize = 0
            pnlCat.Controls.Add(cmbCategorieDepense)
            pnlCat.Controls.Add(btnAddCategorie)
            layoutSaisie.Controls.Add(pnlCat, 0, 2)

            layoutSaisie.Controls.Add(CreateLabel("Montant"), 0, 3)
            Dim pnlMontantDevise As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1, .Margin = New Padding(0), .Padding = New Padding(0)}
            pnlMontantDevise.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 74))
            pnlMontantDevise.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 26))
            txtMontantDepense = CreateStyledTextBox("0.00")
            txtMontantDepense.Dock = DockStyle.Fill
            txtMontantDepense.Margin = New Padding(0, 0, 10, 0)
            cmbDeviseDepense = CreateStyledCombo(New String() {"FC", "USD"})
            cmbDeviseDepense.Dock = DockStyle.Fill
            pnlMontantDevise.Controls.Add(txtMontantDepense, 0, 0)
            pnlMontantDevise.Controls.Add(cmbDeviseDepense, 1, 0)
            layoutSaisie.Controls.Add(pnlMontantDevise, 0, 4)

            layoutSaisie.Controls.Add(CreateLabel("Source / Type"), 0, 5)
            Dim pnlSourceType As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1, .Margin = New Padding(0), .Padding = New Padding(0)}
            pnlSourceType.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            pnlSourceType.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            cmbSourceDepense = CreateStyledCombo(New String() {"Caisse", "Banque"})
            cmbSourceDepense.Dock = DockStyle.Fill
            cmbSourceDepense.Margin = New Padding(0, 0, 10, 0)
            cmbTypeDepense = CreateStyledCombo(New String() {"Normale", "Exceptionnelle"})
            cmbTypeDepense.Dock = DockStyle.Fill
            pnlSourceType.Controls.Add(cmbSourceDepense, 0, 0)
            pnlSourceType.Controls.Add(cmbTypeDepense, 1, 0)
            layoutSaisie.Controls.Add(pnlSourceType, 0, 6)

            layoutSaisie.Controls.Add(CreateLabel("Description"), 0, 7)
            txtDescriptionDepense = CreateStyledTextBox("Description...")
            txtDescriptionDepense.Multiline = True
            txtDescriptionDepense.Dock = DockStyle.Fill
            txtDescriptionDepense.Height = 62 ' Réduit pour rendre l'ensemble plus compact
            txtDescriptionDepense.Margin = New Padding(0)
            layoutSaisie.Controls.Add(txtDescriptionDepense, 0, 8)

            layoutSaisie.Controls.Add(CreateLabel("Date"), 0, 9)
            dtpDateDepense = New DateTimePicker() With {.Dock = DockStyle.Fill, .Margin = New Padding(0, -6, 0, 4), .Font = FontControl}
            layoutSaisie.Controls.Add(dtpDateDepense, 0, 10)

            Dim pnlValidation As New Panel() With {.Dock = DockStyle.Fill, .Margin = New Padding(0, 4, 0, 0)}
            btnValiderDepense = CreateStyledButton("Valider la dépense", ColorDanger)
            btnValiderDepense.Width = 190
            btnValiderDepense.Height = 32
            btnValiderDepense.Dock = DockStyle.None
            btnValiderDepense.Anchor = AnchorStyles.None
            pnlValidation.Controls.Add(btnValiderDepense)
            AddHandler pnlValidation.Resize,
                Sub()
                    btnValiderDepense.Left = (pnlValidation.ClientSize.Width - btnValiderDepense.Width) \ 2
                    btnValiderDepense.Top = (pnlValidation.ClientSize.Height - btnValiderDepense.Height) \ 2
                End Sub
            layoutSaisie.Controls.Add(pnlValidation, 0, 11)

            pnlSaisie.Controls.Add(layoutSaisie)

            ' Historique (Carte)
            Dim pnlHistorique As Panel = CreerCarte()
            pnlHistorique.Padding = New Padding(16)

            Dim layoutHistorique As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3,
                .Padding = New Padding(0)
            }
            layoutHistorique.RowStyles.Add(New RowStyle(SizeType.Absolute, 32)) ' Titre
            layoutHistorique.RowStyles.Add(New RowStyle(SizeType.Absolute, 44)) ' Outils d'impression
            layoutHistorique.RowStyles.Add(New RowStyle(SizeType.Percent, 100)) ' Grille
            layoutHistorique.BackColor = Color.Transparent

            Dim lblHistoriqueTitle As New Label() With {.Text = "Historique des Dépenses", .Font = FontTitle, .ForeColor = ColorPrimary, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}
            layoutHistorique.Controls.Add(lblHistoriqueTitle, 0, 0)

            ' Barre d'outils impression
            Dim pnlPrintTools As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.LeftToRight, .WrapContents = False, .AutoSize = False, .Padding = New Padding(0, 4, 0, 4)}
            pnlPrintTools.Controls.Add(CreateLabel("Année:", New Padding(0, 5, 5, 0)))
            cmbAnneeRapport = New ComboBox() With {.Width = 70, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl}
            For i As Integer = DateTime.Now.Year To DateTime.Now.Year - 5 Step -1
                cmbAnneeRapport.Items.Add(i)
            Next
            cmbAnneeRapport.SelectedIndex = 0
            pnlPrintTools.Controls.Add(cmbAnneeRapport)

            pnlPrintTools.Controls.Add(CreateLabel("Mois:", New Padding(15, 5, 5, 0)))
            cmbMoisRapport = New ComboBox() With {.Width = 110, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl}
            cmbMoisRapport.Items.Add("Toute l'année")
            cmbMoisRapport.Items.AddRange(New String() {"Janvier", "Février", "Mars", "Avril", "Mai", "Juin", "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"})
            cmbMoisRapport.SelectedIndex = DateTime.Now.Month
            pnlPrintTools.Controls.Add(cmbMoisRapport)

            btnImprimerRapport = CreateStyledButton("🖨️ Imprimer Rapport", ColorAccent)
            btnImprimerRapport.Width = 150
            btnImprimerRapport.Margin = New Padding(20, 0, 0, 0)
            pnlPrintTools.Controls.Add(btnImprimerRapport)
            layoutHistorique.Controls.Add(pnlPrintTools, 0, 1)

            gridHistoriqueDepenses = CreerGrille()
            layoutHistorique.Controls.Add(gridHistoriqueDepenses, 0, 2)

            pnlHistorique.Controls.Add(layoutHistorique)

            mainLayoutDepenses.Controls.Add(pnlSaisie, 0, 0)
            mainLayoutDepenses.Controls.Add(pnlHistorique, 1, 0)
            tpDepenses.Controls.Add(mainLayoutDepenses)

            AddHandler btnValiderDepense.Click, AddressOf ValiderDepense

            AddHandler btnAddCategorie.Click, AddressOf AjouterNouvelleCategorie
            AddHandler btnImprimerRapport.Click, AddressOf PreparerImpression
            AddHandler cmbAnneeRapport.SelectedIndexChanged, AddressOf ChargerHistoriqueDepenses
            AddHandler cmbMoisRapport.SelectedIndexChanged, AddressOf ChargerHistoriqueDepenses
        End Sub

        Private Sub InitOngletCaisse()
            tpCaisse.BackColor = ColorBg
            Dim flowCaisse As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(24), .AutoScroll = True, .BackColor = ColorBg}
            flowCaisse.FlowDirection = FlowDirection.LeftToRight
            flowCaisse.WrapContents = True

            lblEncaisseFC = CreerKpiCard(flowCaisse, "Encaisse du Jour (FC)", ColorSuccess)
            lblEncaisseUSD = CreerKpiCard(flowCaisse, "Encaisse du Jour (USD)", ColorSuccess)
            lblDepensesCaisseFC = CreerKpiCard(flowCaisse, "Dépenses Caisse (FC)", ColorDanger)
            lblDepensesCaisseUSD = CreerKpiCard(flowCaisse, "Dépenses Caisse (USD)", ColorDanger)
            lblSoldeCaisseFC = CreerKpiCard(flowCaisse, "Solde Actuel (FC)", ColorPrimary)
            lblSoldeCaisseUSD = CreerKpiCard(flowCaisse, "Solde Actuel (USD)", ColorPrimary)

            lblStatusCloture = New Label() With {.Text = "Statut : Prêt", .Width = 1000, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Margin = New Padding(10, 20, 0, 0)}
            flowCaisse.Controls.Add(lblStatusCloture)

            tpCaisse.Controls.Add(flowCaisse)
        End Sub

        Private Sub InitOngletBanque()
            tpBanque.BackColor = ColorBg
            Dim mainLayoutBanque As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 3, .Padding = New Padding(24), .BackColor = ColorBg}
            mainLayoutBanque.RowStyles.Add(New RowStyle(SizeType.Absolute, 55))
            mainLayoutBanque.RowStyles.Add(New RowStyle(SizeType.Absolute, 155))
            mainLayoutBanque.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim pnlFiltresBanque As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.LeftToRight, .WrapContents = False, .AutoSize = False, .Padding = New Padding(0), .Margin = New Padding(0)}
            lblBanqueFiltre = CreateLabel("Filtre :", New Padding(0, 8, 5, 0))
            pnlFiltresBanque.Controls.Add(lblBanqueFiltre)
            cmbFiltreBanque = New ComboBox() With {.Width = 120, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl}
            cmbFiltreBanque.Items.AddRange(New Object() {"Toutes", "Par année", "Par mois", "Par jour"})
            cmbFiltreBanque.SelectedIndex = 0
            pnlFiltresBanque.Controls.Add(cmbFiltreBanque)
            lblBanqueAnnee = CreateLabel("Année :", New Padding(12, 8, 5, 0))
            pnlFiltresBanque.Controls.Add(lblBanqueAnnee)
            cmbAnneeBanque = New ComboBox() With {.Width = 90, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl}
            For i As Integer = DateTime.Now.Year To DateTime.Now.Year - 5 Step -1
                cmbAnneeBanque.Items.Add(i)
            Next
            cmbAnneeBanque.SelectedIndex = 0
            pnlFiltresBanque.Controls.Add(cmbAnneeBanque)
            lblBanqueMois = CreateLabel("Mois :", New Padding(12, 8, 5, 0))
            pnlFiltresBanque.Controls.Add(lblBanqueMois)
            cmbMoisBanque = New ComboBox() With {.Width = 130, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl}
            cmbMoisBanque.Items.AddRange(New Object() {"Janvier", "Février", "Mars", "Avril", "Mai", "Juin", "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"})
            cmbMoisBanque.SelectedIndex = DateTime.Now.Month - 1
            pnlFiltresBanque.Controls.Add(cmbMoisBanque)
            lblBanqueJour = CreateLabel("Jour :", New Padding(12, 8, 5, 0))
            pnlFiltresBanque.Controls.Add(lblBanqueJour)
            dtpJourBanque = New DateTimePicker() With {.Width = 120, .Format = DateTimePickerFormat.Short, .Font = FontControl}
            pnlFiltresBanque.Controls.Add(dtpJourBanque)

            mainLayoutBanque.Controls.Add(pnlFiltresBanque, 0, 0)

            Dim pnlSoldes As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.LeftToRight, .WrapContents = False, .AutoSize = False, .Padding = New Padding(0)}
            lblSoldeBanqueFC = CreerKpiCard(pnlSoldes, "Solde Banque (FC)", ColorPrimary)
            lblSoldeBanqueUSD = CreerKpiCard(pnlSoldes, "Solde Banque (USD)", ColorPrimary)

            Dim pnlHist As Panel = CreerCarte()
            pnlHist.Margin = New Padding(0, 16, 0, 0)
            pnlHist.Padding = New Padding(20)

            Dim layoutHistBanque As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2,
                .Padding = New Padding(0)
            }
            layoutHistBanque.RowStyles.Add(New RowStyle(SizeType.Absolute, 35)) ' Titre
            layoutHistBanque.RowStyles.Add(New RowStyle(SizeType.Percent, 100)) ' Grille
            layoutHistBanque.BackColor = Color.Transparent

            Dim lblTitleHistBanque As New Label() With {.Text = "Historique des Opérations Bancaires", .Font = FontTitle, .ForeColor = ColorPrimary, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}
            layoutHistBanque.Controls.Add(lblTitleHistBanque, 0, 0)

            gridHistoriqueBanque = CreerGrille()
            layoutHistBanque.Controls.Add(gridHistoriqueBanque, 0, 1)

            pnlHist.Controls.Add(layoutHistBanque)

            mainLayoutBanque.Controls.Add(pnlSoldes, 0, 1)
            mainLayoutBanque.Controls.Add(pnlHist, 0, 2)
            tpBanque.Controls.Add(mainLayoutBanque)

            AddHandler cmbFiltreBanque.SelectedIndexChanged, AddressOf ActualiserFiltreBanqueUI
            AddHandler cmbAnneeBanque.SelectedIndexChanged, AddressOf ActualiserFiltreBanqueUI
            AddHandler cmbMoisBanque.SelectedIndexChanged, AddressOf ActualiserFiltreBanqueUI
            AddHandler dtpJourBanque.ValueChanged, AddressOf ActualiserFiltreBanqueUI
            ActualiserFiltreBanqueUI(Nothing, EventArgs.Empty)
        End Sub

        Private Sub InitOngletDashboard()
            tpDashboard.BackColor = ColorBg
            Dim mainLayoutDashboard As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 2, .Padding = New Padding(24), .BackColor = ColorBg}
            mainLayoutDashboard.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            mainLayoutDashboard.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            mainLayoutDashboard.RowStyles.Add(New RowStyle(SizeType.Percent, 60))
            mainLayoutDashboard.RowStyles.Add(New RowStyle(SizeType.Percent, 40))

            chartDepensesCat = CreerChart("Dépenses par Catégorie", SeriesChartType.Pie)
            chartEvolutionFinance = CreerChart("Évolution Caisse vs Banque", SeriesChartType.Line)

            mainLayoutDashboard.Controls.Add(WrapInCard(chartDepensesCat, "Répartition des Dépenses"), 0, 0)
            mainLayoutDashboard.Controls.Add(WrapInCard(chartEvolutionFinance, "Évolution des Flux"), 1, 0)

            Dim pnlKpi As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.LeftToRight, .WrapContents = True, .Margin = New Padding(0, 16, 0, 0)}
            lblTotalEncaisse = CreerKpiCard(pnlKpi, "Total Encaisse", ColorSuccess)
            lblTotalDepenses = CreerKpiCard(pnlKpi, "Total Dépenses", ColorDanger)
            lblSoldeGlobalBanque = CreerKpiCard(pnlKpi, "Solde Global Banque", ColorPrimary)

            mainLayoutDashboard.Controls.Add(pnlKpi, 0, 1)
            mainLayoutDashboard.SetColumnSpan(pnlKpi, 2)

            tpDashboard.Controls.Add(mainLayoutDashboard)
        End Sub

        Private Sub Tabs_GotFocus(sender As Object, e As EventArgs)
            ' Empêche le focus sur les onglets pour un aspect plus propre
            Me.ActiveControl = tabControlFinance.SelectedTab
        End Sub

        Private Sub SetSelectedTab(index As Integer)
            tabControlFinance.SelectedIndex = index
            ' Mettre à jour le style des boutons d'onglet
            For Each ctrl As Control In DirectCast(Me.Controls(0), TableLayoutPanel).Controls(1).Controls
                If TypeOf ctrl Is Button Then
                    Dim btn As Button = DirectCast(ctrl, Button)
                    If btn.Text.Contains("Dépenses") AndAlso index = 0 Then
                        ApplyTabButtonStyle(btn, True)
                    ElseIf btn.Text.Contains("Caisse") AndAlso index = 1 Then
                        ApplyTabButtonStyle(btn, True)
                    ElseIf btn.Text.Contains("Banque") AndAlso index = 2 Then
                        ApplyTabButtonStyle(btn, True)
                    ElseIf btn.Text.Contains("Dashboard") AndAlso index = 3 Then
                        ApplyTabButtonStyle(btn, True)
                    Else
                        ApplyTabButtonStyle(btn, False)
                    End If
                End If
            Next
        End Sub

        Private Sub ApplyTabButtonStyle(btn As Button, isActive As Boolean)
            btn.BackColor = If(isActive, ColorTabActive, ColorTabInactive)
            btn.ForeColor = If(isActive, ColorPrimary, ColorTextSecondary)
            btn.FlatAppearance.BorderSize = 0
            RemoveHandler btn.Paint, AddressOf TabButton_Paint ' Supprimer l'ancien handler
            AddHandler btn.Paint, AddressOf TabButton_Paint ' Ajouter le nouveau handler
            btn.Invalidate()
        End Sub

        Private Sub TabButton_Paint(sender As Object, e As PaintEventArgs)
            Dim btn As Button = DirectCast(sender, Button)
            If btn.BackColor = ColorTabActive Then ' Si l'onglet est actif
                Using p As New Pen(ColorPrimary, 2)
                    e.Graphics.DrawLine(p, 0, btn.Height - 1, btn.Width, btn.Height - 1)
                End Using
            End If
        End Sub

        Private Sub ChargerDonnees()
            ChargerParametresApplication()
            ChargerHistoriqueDepenses(Nothing, EventArgs.Empty)
            ChargerCaisse()
            ChargerBanque()
            ChargerDashboard()
            ChargerCategoriesDepense()
        End Sub

        'Private Sub ChargerDonnees()
        '    Try
        '        Dim dtCat As DataTable = _catService.GetAll()
        '        cmbCategorieDepense.DataSource = dtCat
        '        cmbCategorieDepense.DisplayMember = "Libelle"
        '        cmbCategorieDepense.ValueMember = "Libelle"

        '        Dim dateJour As DateTime = DateTime.Now
        '        lblEncaisseFC.Text = _caisseService.GetEncaisse(dateJour, "FC").ToString("N2") & " FC"
        '        lblEncaisseUSD.Text = _caisseService.GetEncaisse(dateJour, "USD").ToString("N2") & " USD"
        '        lblDepensesCaisseFC.Text = _caisseService.GetDepensesCaisse(dateJour, "FC").ToString("N2") & " FC"
        '        lblDepensesCaisseUSD.Text = _caisseService.GetDepensesCaisse(dateJour, "USD").ToString("N2") & " USD"
        '        lblSoldeCaisseFC.Text = _caisseService.GetSoldeCaisse(dateJour, "FC").ToString("N2") & " FC"
        '        lblSoldeCaisseUSD.Text = _caisseService.GetSoldeCaisse(dateJour, "USD").ToString("N2") & " USD"

        '        lblSoldeBanqueFC.Text = _banqueService.GetSolde("FC").ToString("N2") & " FC"
        '        lblSoldeBanqueUSD.Text = _banqueService.GetSolde("USD").ToString("N2") & " USD"

        '        gridHistoriqueDepenses.DataSource = _depenseService.GetHistorique()
        '        gridHistoriqueBanque.DataSource = _banqueService.GetHistorique()

        '        ChargerGraphiques()
        '    Catch ex As Exception
        '        MessageBox.Show("Erreur lors du chargement : " & ex.Message)
        '    End Try
        'End Sub
        Private Sub ChargerHistoriqueDepenses(sender As Object, e As EventArgs)
            Try
                Dim annee As Integer = CInt(cmbAnneeRapport.SelectedItem)
                Dim mois As Integer = cmbMoisRapport.SelectedIndex

                'Dim depenses As List(Of DepenseDTO)
                'If mois = 0 Then ' Toute l'année
                '    depenses = _depenseService.ObtenirDepensesParAnnee(annee)
                'Else
                '    depenses = _depenseService.ObtenirDepensesParMois(annee, mois)
                'End If

                gridHistoriqueDepenses.DataSource = _depenseService.GetHistorique(annee, mois)
                ConfigurerGrilleDepenses()
            Catch ex As Exception
                MessageBox.Show("Erreur chargement historique dépenses: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerCaisse()
            Try
                Dim dateJour As DateTime = DateTime.Now
                lblEncaisseFC.Text = FormatMontant(_caisseService.GetEncaisse(dateJour, "FC"), "FC")
                lblEncaisseUSD.Text = FormaterSoldeUsd(_caisseService.GetEncaisse(dateJour, "USD"))
                lblDepensesCaisseFC.Text = FormatMontant(_caisseService.GetDepensesCaisse(dateJour, "FC"), "FC")
                lblDepensesCaisseUSD.Text = FormaterSoldeUsd(_caisseService.GetDepensesCaisse(dateJour, "USD"))
                lblSoldeCaisseFC.Text = FormatMontant(_caisseService.GetSoldeCaisse(dateJour, "FC"), "FC")
                lblSoldeCaisseUSD.Text = FormaterSoldeUsd(_caisseService.GetSoldeCaisse(dateJour, "USD"))
                ' lblStatusCloture.Text = If(caisse.EstCloture, "Statut : Clôturé", "Statut : Ouvert")
            Catch ex As Exception
                MessageBox.Show("Erreur chargement caisse: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerBanque()
            Try
                Dim historiqueAll As DataTable = _banqueService.GetHistorique()
                Dim historiqueFiltre As DataTable = FiltrerHistoriqueBanque(historiqueAll)
                gridHistoriqueBanque.DataSource = historiqueFiltre
                ConfigurerGrilleBanque()

                Dim soldeUsdDisponible As Boolean = BanqueUsdDisponible(historiqueFiltre)
                If EstFiltreBanqueToutes() OrElse historiqueFiltre Is Nothing Then
                    lblSoldeBanqueFC.Text = FormatMontant(_banqueService.GetSolde("FC"), "FC")
                    If soldeUsdDisponible Then
                        lblSoldeBanqueUSD.Text = FormaterSoldeUsd(_banqueService.GetSolde("USD"))
                    Else
                        lblSoldeBanqueUSD.Text = "Non disponible"
                    End If
                Else
                    lblSoldeBanqueFC.Text = FormatMontant(CalculerSoldeBanqueFiltre(historiqueFiltre, "FC"), "FC")
                    If soldeUsdDisponible Then
                        lblSoldeBanqueUSD.Text = FormaterSoldeUsd(CalculerSoldeBanqueFiltre(historiqueFiltre, "USD"))
                    Else
                        lblSoldeBanqueUSD.Text = "Non disponible"
                    End If
                End If

                If Not soldeUsdDisponible Then
                    If Not _banqueUsdIndisponibleLoggee Then
                        Dim log As New ProductionLogService()
                        log.Warn("FormulaireFinance", "ChargerBanque", "Solde Banque USD indisponible: aucune colonne de montant original USD n'est disponible dans l'historique Banque. Affichage neutralisé.")
                        _banqueUsdIndisponibleLoggee = True
                    End If
                End If
            Catch ex As Exception
                MessageBox.Show("Erreur chargement banque: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerDashboard()
            Try
                Dim dateJour As DateTime = DateTime.Now
                Dim totalEncaisseFC As Decimal = _caisseService.GetEncaisse(dateJour, "FC")
                Dim totalDepensesFC As Decimal = _caisseService.GetDepensesCaisse(dateJour, "FC")
                Dim soldeGlobalBanqueFC As Decimal = _banqueService.GetSolde("FC")

                lblTotalEncaisse.Text = FormatMontant(totalEncaisseFC, "FC")
                lblTotalDepenses.Text = FormatMontant(totalDepensesFC, "FC")
                lblSoldeGlobalBanque.Text = FormatMontant(soldeGlobalBanqueFC, "FC")

                ChargerRepartitionDepenses()
                ChargerEvolutionFlux()
            Catch ex As Exception
                MessageBox.Show("Erreur chargement dashboard: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerRepartitionDepenses()
            chartDepensesCat.Series(0).Points.Clear()
            chartDepensesCat.Series(0).IsVisibleInLegend = True
            chartDepensesCat.Series(0).IsValueShownAsLabel = True
            chartDepensesCat.Series(0).SmartLabelStyle.Enabled = True
            chartDepensesCat.Series(0).SmartLabelStyle.AllowOutsidePlotArea = LabelOutsidePlotAreaStyle.Yes
            chartDepensesCat.Series(0).SmartLabelStyle.CalloutLineColor = ColorTextSecondary
            chartDepensesCat.Series(0).LabelForeColor = ColorTextPrimary
            chartDepensesCat.Series(0)("PieLabelStyle") = "Outside"
            chartDepensesCat.Series(0).Label = "#VALX : #VALY{N0} FC"
            If chartDepensesCat.ChartAreas.Count > 0 Then
                chartDepensesCat.ChartAreas(0).Area3DStyle.Enable3D = False
            End If

            Dim dtStats As DataTable = _depenseService.GetHistorique()
            For Each row As DataRow In dtStats.Rows
                Dim categorie As String = If(dtStats.Columns.Contains("NomCategorie"), Convert.ToString(row("NomCategorie")), Convert.ToString(row("Categorie")))
                Dim description As String = If(dtStats.Columns.Contains("Description"), Convert.ToString(row("Description")), "")
                Dim libelle As String = categorie
                If Not String.IsNullOrWhiteSpace(description) Then
                    libelle &= " - " & description
                End If
                Dim total As Decimal = If(IsDBNull(row("Montant")), 0D, Convert.ToDecimal(row("Montant")))
                Dim indexPoint As Integer = chartDepensesCat.Series(0).Points.AddXY(libelle, total)
                Dim point As DataPoint = chartDepensesCat.Series(0).Points(indexPoint)
                point.Label = libelle & Environment.NewLine & total.ToString("N0") & " FC"
                point.LegendText = libelle
                point.ToolTip = libelle & " : " & total.ToString("N0") & " FC"
            Next
        End Sub

        Private Sub ChargerEvolutionFlux()
            chartEvolutionFinance.Series.Clear()
            chartEvolutionFinance.Legends.Clear()
            chartEvolutionFinance.Legends.Add(New Legend())

            Dim serieCaisse As New Series("Caisse") With {
                .ChartType = SeriesChartType.Line,
                .BorderWidth = 3,
                .IsValueShownAsLabel = False
            }
            Dim serieBanque As New Series("Banque") With {
                .ChartType = SeriesChartType.Line,
                .BorderWidth = 3,
                .IsValueShownAsLabel = False
            }

            chartEvolutionFinance.Series.Add(serieCaisse)
            chartEvolutionFinance.Series.Add(serieBanque)

            If chartEvolutionFinance.ChartAreas.Count > 0 Then
                chartEvolutionFinance.ChartAreas(0).AxisX.Interval = 1
                chartEvolutionFinance.ChartAreas(0).AxisX.MajorGrid.Enabled = False
                chartEvolutionFinance.ChartAreas(0).AxisY.MajorGrid.LineColor = Color.FromArgb(235, 235, 235)
            End If

            Dim historiqueBanque As DataTable = _banqueService.GetHistorique()
            For i As Integer = 6 To 0 Step -1
                Dim jour As DateTime = Date.Today.AddDays(-i)
                Dim fluxCaisse As Decimal = _caisseService.GetEncaisse(jour, "FC") - _caisseService.GetDepensesCaisse(jour, "FC")
                Dim fluxBanque As Decimal = CalculerFluxBanqueJour(historiqueBanque, jour)

                serieCaisse.Points.AddXY(jour.ToString("dd/MM"), fluxCaisse)
                serieBanque.Points.AddXY(jour.ToString("dd/MM"), fluxBanque)
            Next
        End Sub

        Private Function CalculerFluxBanqueJour(historiqueBanque As DataTable, jour As DateTime) As Decimal
            If historiqueBanque Is Nothing OrElse historiqueBanque.Rows.Count = 0 Then
                Return 0D
            End If

            Dim colonneDate As String = If(historiqueBanque.Columns.Contains("DateOperation"), "DateOperation", "DateTransaction")
            Dim colonneType As String = If(historiqueBanque.Columns.Contains("TypeOperation"), "TypeOperation", "TypeTransaction")
            Dim total As Decimal = 0D

            For Each row As DataRow In historiqueBanque.Rows
                If IsDBNull(row(colonneDate)) Then
                    Continue For
                End If

                Dim dateOperation As DateTime = Convert.ToDateTime(row(colonneDate))
                If dateOperation.Date <> jour.Date Then
                    Continue For
                End If

                Dim montant As Decimal = If(IsDBNull(row("Montant")), 0D, Convert.ToDecimal(row("Montant")))
                Dim typeOperation As String = Convert.ToString(row(colonneType)).Trim().ToLowerInvariant()
                If typeOperation.Contains("retrait") Then
                    total -= montant
                Else
                    total += montant
                End If
            Next

            Return total
        End Function

        Private Sub ActualiserFiltreBanqueUI(sender As Object, e As EventArgs)
            If cmbFiltreBanque Is Nothing Then
                Return
            End If

            Dim filtre As String = Convert.ToString(cmbFiltreBanque.SelectedItem)
            Dim visibleAnnee As Boolean = String.Equals(filtre, "Par année", StringComparison.OrdinalIgnoreCase) OrElse
                                          String.Equals(filtre, "Par mois", StringComparison.OrdinalIgnoreCase) OrElse
                                          String.Equals(filtre, "Par jour", StringComparison.OrdinalIgnoreCase)
            Dim visibleMois As Boolean = String.Equals(filtre, "Par mois", StringComparison.OrdinalIgnoreCase)
            Dim visibleJour As Boolean = String.Equals(filtre, "Par jour", StringComparison.OrdinalIgnoreCase)

            If lblBanqueFiltre IsNot Nothing Then lblBanqueFiltre.Visible = True
            If cmbAnneeBanque IsNot Nothing Then cmbAnneeBanque.Visible = visibleAnnee
            If lblBanqueAnnee IsNot Nothing Then lblBanqueAnnee.Visible = visibleAnnee
            If cmbMoisBanque IsNot Nothing Then cmbMoisBanque.Visible = visibleMois
            If lblBanqueMois IsNot Nothing Then lblBanqueMois.Visible = visibleMois
            If dtpJourBanque IsNot Nothing Then dtpJourBanque.Visible = visibleJour
            If lblBanqueJour IsNot Nothing Then lblBanqueJour.Visible = visibleJour

            ChargerBanque()
        End Sub

        Private Function EstFiltreBanqueToutes() As Boolean
            If cmbFiltreBanque Is Nothing Then
                Return True
            End If
            Return String.Equals(Convert.ToString(cmbFiltreBanque.SelectedItem), "Toutes", StringComparison.OrdinalIgnoreCase)
        End Function

        Private Function FiltrerHistoriqueBanque(historique As DataTable) As DataTable
            If historique Is Nothing Then
                Return Nothing
            End If

            If EstFiltreBanqueToutes() Then
                Return historique
            End If

            Dim filtre As String = Convert.ToString(cmbFiltreBanque.SelectedItem)
            Dim colonneDate As String = If(historique.Columns.Contains("DateOperation"), "DateOperation", If(historique.Columns.Contains("DateTransaction"), "DateTransaction", ""))
            If colonneDate = "" Then
                Return historique
            End If

            Dim annee As Integer = If(cmbAnneeBanque Is Nothing OrElse cmbAnneeBanque.SelectedItem Is Nothing, DateTime.Now.Year, Convert.ToInt32(cmbAnneeBanque.SelectedItem))
            Dim resultat As DataTable = historique.Clone()

            For Each row As DataRow In historique.Rows
                If IsDBNull(row(colonneDate)) Then
                    Continue For
                End If

                Dim dateOperation As DateTime = Convert.ToDateTime(row(colonneDate))
                Dim doitInclure As Boolean = False

                If String.Equals(filtre, "Par année", StringComparison.OrdinalIgnoreCase) Then
                    doitInclure = (dateOperation.Year = annee)
                ElseIf String.Equals(filtre, "Par mois", StringComparison.OrdinalIgnoreCase) Then
                    Dim mois As Integer = If(cmbMoisBanque Is Nothing OrElse cmbMoisBanque.SelectedIndex < 0, DateTime.Now.Month, cmbMoisBanque.SelectedIndex + 1)
                    doitInclure = (dateOperation.Year = annee AndAlso dateOperation.Month = mois)
                ElseIf String.Equals(filtre, "Par jour", StringComparison.OrdinalIgnoreCase) Then
                    doitInclure = (dateOperation.Date = dtpJourBanque.Value.Date)
                End If

                If doitInclure Then
                    resultat.ImportRow(row)
                End If
            Next

            Return resultat
        End Function

        Private Function CalculerSoldeBanqueFiltre(historique As DataTable, devise As String) As Decimal
            If historique Is Nothing OrElse historique.Rows.Count = 0 Then
                Return 0D
            End If

            Dim colonneType As String = If(historique.Columns.Contains("TypeOperation"), "TypeOperation", If(historique.Columns.Contains("TypeTransaction"), "TypeTransaction", ""))
            If colonneType = "" OrElse Not historique.Columns.Contains("Devise") Then
                Return 0D
            End If

            Dim colonneMontant As String = "Montant"
            If String.Equals(devise, "USD", StringComparison.OrdinalIgnoreCase) Then
                If historique.Columns.Contains("MontantUSD") Then
                    colonneMontant = "MontantUSD"
                ElseIf historique.Columns.Contains("MontantOriginalUSD") Then
                    colonneMontant = "MontantOriginalUSD"
                ElseIf historique.Columns.Contains("MontantOriginal") AndAlso historique.Columns.Contains("DeviseOriginale") Then
                    colonneMontant = "MontantOriginal"
                Else
                    Return 0D
                End If
            ElseIf Not historique.Columns.Contains("Montant") Then
                Return 0D
            End If

            Dim total As Decimal = 0D
            For Each row As DataRow In historique.Rows
                If IsDBNull(row("Devise")) OrElse Not String.Equals(Convert.ToString(row("Devise")), devise, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If

                Dim montant As Decimal = If(IsDBNull(row(colonneMontant)), 0D, Convert.ToDecimal(row(colonneMontant)))
                Dim typeOperation As String = Convert.ToString(row(colonneType)).Trim().ToLowerInvariant()
                If typeOperation.Contains("retrait") Then
                    total -= montant
                Else
                    total += montant
                End If
            Next

            Return total
        End Function

        Private Function BanqueUsdDisponible(historique As DataTable) As Boolean
            If historique Is Nothing OrElse historique.Rows.Count = 0 Then
                Return False
            End If

            If historique.Columns.Contains("MontantUSD") OrElse
               historique.Columns.Contains("MontantOriginalUSD") OrElse
               (historique.Columns.Contains("MontantOriginal") AndAlso historique.Columns.Contains("DeviseOriginale")) Then
                Return True
            End If

            Return False
        End Function

        Private Sub ChargerCategoriesDepense()
            Try
                Dim dtCat As DataTable = _catService.GetAll()
                cmbCategorieDepense.DataSource = dtCat
                cmbCategorieDepense.DisplayMember = "Libelle"
                cmbCategorieDepense.ValueMember = "Id"
            Catch ex As Exception
                MessageBox.Show("Erreur chargement catégories: " & ex.Message)
            End Try
        End Sub

        Private Sub ValiderDepense(sender As Object, e As EventArgs)
            Try
                Dim montant As Decimal = Decimal.Parse(txtMontantDepense.Text)
                Dim devise As String = cmbDeviseDepense.SelectedItem.ToString()
                Dim source As String = cmbSourceDepense.SelectedItem.ToString()
                Dim typeDepense As String = cmbTypeDepense.SelectedItem.ToString()
                Dim description As String = txtDescriptionDepense.Text
                Dim dateDepense As Date = dtpDateDepense.Value
                If cmbCategorieDepense.SelectedValue Is Nothing OrElse IsDBNull(cmbCategorieDepense.SelectedValue) Then
                    Throw New Exception("Sélectionnez une catégorie de dépense.")
                End If
                Dim categorieId As Integer = Convert.ToInt32(cmbCategorieDepense.SelectedValue)

                Dim nouvelleDepense As New DepenseDTOFinance With {
                    .Montant = montant,
                    .Devise = devise,
                    .Source = source,
                    .TypeDepense = typeDepense,
                    .Description = description,
                    .DateDepense = dateDepense,
                    .Categorie = cmbCategorieDepense.Text,
                    .CreePar = "Admin"
                }

                _depenseService.AjouterDepense(nouvelleDepense)
                MessageBox.Show("Dépense enregistrée avec succès.")

                ' Réinitialiser le formulaire et recharger les données
                txtMontantDepense.Text = "0.00"
                txtDescriptionDepense.Text = ""
                ChargerDonnees()
            Catch ex As Exception
                MessageBox.Show("Erreur validation dépense: " & ex.Message)
            End Try
        End Sub

        Private Sub AjouterNouvelleCategorie(sender As Object, e As EventArgs)
            Dim nomCategorie As String = InputBox("Entrez le nom de la nouvelle catégorie de dépense:", "Nouvelle Catégorie")
            If Not String.IsNullOrWhiteSpace(nomCategorie) Then
                Try
                    _catService.Ajouter(nomCategorie)
                    ChargerCategoriesDepense()
                    MessageBox.Show("Catégorie ajoutée avec succès.")
                Catch ex As Exception
                    MessageBox.Show("Erreur ajout catégorie: " & ex.Message)
                End Try
            End If
        End Sub



        Private Sub ClotureAutomatique()
            Try
                _caisseService.ClotureAutomatique()
                lblStatusCloture.Text = "Statut : Clôture automatique effectuée avec succès."
            Catch ex As Exception
                lblStatusCloture.Text = "Erreur clôture : " & ex.Message
            End Try
        End Sub

        Private Sub ConfigurerGrilleDepenses()
            If gridHistoriqueDepenses.Columns.Count = 0 Then Return

            ' Configuration des colonnes pour l'historique des dépenses
            gridHistoriqueDepenses.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            gridHistoriqueDepenses.ScrollBars = ScrollBars.Both
            ConfigurerColonne(gridHistoriqueDepenses, "Id", "", 0, Nothing, True)
            If gridHistoriqueDepenses.Columns.Contains("DateDepense") Then
                ConfigurerColonne(gridHistoriqueDepenses, "DateDepense", "Date", 100, "dd/MM/yyyy")
            ElseIf gridHistoriqueDepenses.Columns.Contains("Date") Then
                ConfigurerColonne(gridHistoriqueDepenses, "Date", "Date", 100, "dd/MM/yyyy")
            End If
            If gridHistoriqueDepenses.Columns.Contains("NomCategorie") Then
                ConfigurerColonne(gridHistoriqueDepenses, "NomCategorie", "Catégorie", 150)
            ElseIf gridHistoriqueDepenses.Columns.Contains("Categorie") Then
                ConfigurerColonne(gridHistoriqueDepenses, "Categorie", "Catégorie", 150)
            End If
            ConfigurerColonne(gridHistoriqueDepenses, "Description", "Description", 220)
            If gridHistoriqueDepenses.Columns.Contains("Montant") Then
                ConfigurerColonne(gridHistoriqueDepenses, "Montant", "Montant", 100, "N0")
            ElseIf gridHistoriqueDepenses.Columns.Contains("MontantTotal") Then
                ConfigurerColonne(gridHistoriqueDepenses, "MontantTotal", "Montant", 100, "N0")
            End If
            ConfigurerColonne(gridHistoriqueDepenses, "Devise", "Devise", 70)
            ConfigurerColonne(gridHistoriqueDepenses, "Source", "Source", 95)
            ConfigurerColonne(gridHistoriqueDepenses, "CreePar", "Créé par", 120)
            ConfigurerColonne(gridHistoriqueDepenses, "CreatedAt", "Créé le", 150, "dd/MM/yyyy HH:mm")
            If gridHistoriqueDepenses.Columns.Contains("TypeDepense") Then
                ConfigurerColonne(gridHistoriqueDepenses, "TypeDepense", "Type", 100)
            ElseIf gridHistoriqueDepenses.Columns.Contains("Type") Then
                ConfigurerColonne(gridHistoriqueDepenses, "Type", "Type", 100)
            End If
        End Sub

        Private Sub ConfigurerGrilleBanque()
            If gridHistoriqueBanque.Columns.Count = 0 Then Return

            ' Configuration des colonnes pour l'historique bancaire
            gridHistoriqueBanque.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            gridHistoriqueBanque.ScrollBars = ScrollBars.Both
            ConfigurerColonne(gridHistoriqueBanque, "Id", "", 0, Nothing, True)
            If gridHistoriqueBanque.Columns.Contains("DateTransaction") Then
                ConfigurerColonne(gridHistoriqueBanque, "DateTransaction", "Date", 100, "dd/MM/yyyy")
            ElseIf gridHistoriqueBanque.Columns.Contains("DateOperation") Then
                ConfigurerColonne(gridHistoriqueBanque, "DateOperation", "Date", 100, "dd/MM/yyyy")
            End If
            ConfigurerColonne(gridHistoriqueBanque, "Description", "Description", 220)
            ConfigurerColonne(gridHistoriqueBanque, "Reference", "Référence", 160)
            ConfigurerColonne(gridHistoriqueBanque, "Montant", "Montant", 120, "N0")
            ConfigurerColonne(gridHistoriqueBanque, "Devise", "Devise", 70)
            If gridHistoriqueBanque.Columns.Contains("TypeTransaction") Then
                ConfigurerColonne(gridHistoriqueBanque, "TypeTransaction", "Type", 120)
            ElseIf gridHistoriqueBanque.Columns.Contains("TypeOperation") Then
                ConfigurerColonne(gridHistoriqueBanque, "TypeOperation", "Type", 120)
            End If
            ConfigurerColonne(gridHistoriqueBanque, "CreatedAt", "Créé le", 150, "dd/MM/yyyy HH:mm")
        End Sub

        Private Sub ConfigurerColonne(grid As DataGridView, nom As String, titre As String, largeur As Integer, Optional format As String = Nothing, Optional cacher As Boolean = False)
            If Not grid.Columns.Contains(nom) Then Return

            Dim col As DataGridViewColumn = grid.Columns(nom)
            col.HeaderText = titre
            col.Width = largeur
            If cacher Then
                col.Visible = False
                Return
            End If
            If Not String.IsNullOrWhiteSpace(format) Then
                col.DefaultCellStyle.Format = format
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            Else
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            End If
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None ' Désactiver l'auto-ajustement par défaut
        End Sub

        Private Function CreateStyledTextBox(placeholder As String) As TextBox
            Dim tb As New TextBox() With {
                .Dock = DockStyle.Fill,
                .Font = FontControl,
                .Padding = New Padding(8),
                .BorderStyle = BorderStyle.FixedSingle
            }
            ' Placeholder logic (simple for now)
            tb.Text = placeholder
            AddHandler tb.GotFocus, Sub(s, e) If tb.Text = placeholder Then tb.Text = ""
            AddHandler tb.LostFocus, Sub(s, e) If String.IsNullOrWhiteSpace(tb.Text) Then tb.Text = placeholder
            Return tb
        End Function

        Private Function CreateStyledCombo(items As String()) As ComboBox
            Dim cb As New ComboBox() With {
                .Width = 100,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = FontControl,
                .Padding = New Padding(8)
            }
            cb.Items.AddRange(items)
            cb.SelectedIndex = 0
            Return cb
        End Function

        Private Function CreateStyledButton(text As String, backColor As Color) As Button
            Dim btn As New Button() With {
                .Text = text,
                .Dock = DockStyle.Fill,
                .Height = 36,
                .BackColor = backColor,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontButton,
                .Cursor = Cursors.Hand
            }
            btn.FlatAppearance.BorderSize = 0
            'btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(backColor.R - 20, backColor.G - 20, backColor.B - 20)
            'btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(backColor.R + 20, backColor.G + 20, backColor.B + 20)
            Return btn
        End Function

        Private Function CreateLabel(text As String, Optional margin As Padding = Nothing) As Label
            If margin = Nothing Then
                margin = New Padding(0, 10, 0, 5)
            End If

            Return New Label() With {
                .Text = text,
                .Dock = DockStyle.Top,
                .AutoSize = True,
                .Font = FontLabel,
                .ForeColor = ColorTextPrimary,
                .Margin = margin
            }
        End Function

        Private Function CreerGrille() As DataGridView
            Dim dgv As New DataGridView() With {
                .Dock = DockStyle.Fill,
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

        Private Function CreerKpiCard(parentPanel As FlowLayoutPanel, title As String, valueColor As Color) As Label
            Dim cardPanel As New Panel() With {
                .Width = 285,
                .Height = 120,
                .BackColor = ColorCardBg,
                .Margin = New Padding(8),
                .Padding = New Padding(15),
                .BorderStyle = BorderStyle.None
            }
            AddHandler cardPanel.Paint, Sub(s, ev) DessinerCarteBordureOmbre(s, ev, cardPanel)

            Dim lblTitle As New Label() With {
                .Text = title,
                .Dock = DockStyle.Top,
                .Font = FontLabel,
                .ForeColor = ColorTextSecondary,
                .AutoSize = False,
                .TextAlign = ContentAlignment.MiddleLeft,
                .Height = 25
            }

            Dim lblValue As New Label() With {
                .Text = "0 FC", ' Valeur par défaut
                .Dock = DockStyle.Fill,
                .Font = FontValue,
                .ForeColor = valueColor,
                .AutoSize = False,
                .TextAlign = ContentAlignment.MiddleRight
            }

            cardPanel.Controls.Add(lblValue)
            cardPanel.Controls.Add(lblTitle)
            parentPanel.Controls.Add(cardPanel)
            Return lblValue
        End Function

        Private Function CreerChart(title As String, chartType As SeriesChartType) As Chart
            Dim chart As New Chart() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.Transparent
            }
            chart.Titles.Add(title)
            chart.Titles(0).Font = FontLabel
            chart.Titles(0).ForeColor = ColorTextPrimary

            Dim series As New Series() With {
                .ChartType = chartType,
                .IsValueShownAsLabel = True
            }
            chart.Series.Add(series)

            Dim chartArea As New ChartArea()
            chart.ChartAreas.Add(chartArea)

            Return chart
        End Function

        Private Function WrapInCard(control As Control, title As String) As Panel
            Dim cardPanel As Panel = CreerCarte()
            cardPanel.Padding = New Padding(15)

            Dim layout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2
            }
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim lblTitle As New Label() With {
                .Text = title,
                .Dock = DockStyle.Fill,
                .Font = FontLabel,
                .ForeColor = ColorTextPrimary,
                .TextAlign = ContentAlignment.MiddleLeft
            }

            layout.Controls.Add(lblTitle, 0, 0)
            layout.Controls.Add(control, 0, 1)
            cardPanel.Controls.Add(layout)
            Return cardPanel
        End Function

        Private Function FormatMontant(montant As Decimal, Optional devise As String = "FC") As String
            Return montant.ToString("N0") & " " & devise
        End Function

        'Private Sub PreparerImpression(sender As Object, e As EventArgs)
        '    titreRapport = "Rapport des Dépenses " & cmbMoisRapport.SelectedItem.ToString() & " " & cmbAnneeRapport.SelectedItem.ToString()
        '    dtRapportAImprimer = DirectCast(gridHistoriqueDepenses.DataSource, List(Of DepenseDTO)).ToDataTable()

        '    If dtRapportAImprimer Is Nothing OrElse dtRapportAImprimer.Rows.Count = 0 Then
        '        MessageBox.Show("Aucune donnée à imprimer.", "Impression", MessageBoxButtons.OK, MessageBoxIcon.Information)
        '        Return
        '    End If

        '    printPreview.ShowDialog()
        'End Sub

        ' --- IMPRESSION ---

        Private Sub PreparerImpression(sender As Object, e As EventArgs)
            Try
                Dim annee As Integer = Convert.ToInt32(cmbAnneeRapport.SelectedItem)
                Dim mois As Integer = cmbMoisRapport.SelectedIndex ' 0 = Toute l'année, 1 = Janvier...

                dtRapportAImprimer = _depenseService.GetHistorique(annee, mois)

                If mois = 0 Then
                    titreRapport = "RAPPORT ANNUEL DES DÉPENSES - " & annee.ToString()
                Else
                    titreRapport = "RAPPORT MENSUEL DES DÉPENSES - " & cmbMoisRapport.SelectedItem.ToString().ToUpper() & " " & annee.ToString()
                End If

                If dtRapportAImprimer.Rows.Count = 0 Then
                    MessageBox.Show("Aucune donnée trouvée pour cette période.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If

                printDoc.DefaultPageSettings.Margins = New System.Drawing.Printing.Margins(30, 30, 30, 30)
                printDoc.DefaultPageSettings.Landscape = False
                _impressionIndex = 0
                _impressionTotalFC = 0D
                _impressionTotalUSD = 0D
                For Each row As DataRow In dtRapportAImprimer.Rows
                    Dim montant As Decimal = If(IsDBNull(row("Montant")), 0D, Convert.ToDecimal(row("Montant")))
                    Dim devise As String = If(IsDBNull(row("Devise")), "", Convert.ToString(row("Devise"))).Trim().ToUpperInvariant()
                    If devise = "USD" Then
                        _impressionTotalUSD += montant
                    Else
                        _impressionTotalFC += montant
                    End If
                Next

                printPreview.ShowDialog()
            Catch ex As Exception
                MessageBox.Show("Erreur lors de la préparation de l'impression : " & ex.Message)
            End Try
        End Sub

        Private Sub PrintDoc_PrintPage(sender As Object, e As PrintPageEventArgs)
            Dim param As ParametreDTO = _parametres
            Dim x As Integer = 30
            Dim y As Integer = 30
            Dim pinceauBleu As New SolidBrush(Color.FromArgb(17, 35, 74))
            Dim pinceauGris As New SolidBrush(Color.FromArgb(92, 104, 120))
            Dim fontTitre As New Font("Segoe UI", 16, FontStyle.Bold)
            Dim fontSousTitre As New Font("Segoe UI", 10, FontStyle.Regular)
            Dim fontBloc As New Font("Segoe UI", 9.5F, FontStyle.Regular)
            Dim fontBlocGras As New Font("Segoe UI", 10, FontStyle.Bold)
            Dim pageWidth As Integer = 760

            If param IsNot Nothing AndAlso param.LogoPath <> "" AndAlso File.Exists(param.LogoPath) Then
                Using logo As Image = Image.FromFile(param.LogoPath)
                    e.Graphics.DrawImage(logo, x, y, 70, 70)
                End Using
                x += 84
            End If

            e.Graphics.DrawString(If(param IsNot Nothing AndAlso param.NomMagasin <> "", param.NomMagasin, "Paon Rehoboth"), fontTitre, pinceauBleu, x, y)
            y += 28
            e.Graphics.DrawString(If(param IsNot Nothing, param.AdresseMagasin, ""), fontSousTitre, pinceauGris, x, y)
            y += 18
            e.Graphics.DrawString(If(param IsNot Nothing, param.TelephoneMagasin, ""), fontSousTitre, pinceauGris, x, y)
            y = 118

            e.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(17, 35, 74)), 30, y, pageWidth, 32)
            e.Graphics.DrawString("RAPPORT DES DÉPENSES", New Font("Segoe UI", 12, FontStyle.Bold), Brushes.White, 42, y + 7)
            y += 48

            e.Graphics.DrawRectangle(New Pen(Color.FromArgb(210, 219, 232)), 30, y, 360, 92)
            e.Graphics.DrawRectangle(New Pen(Color.FromArgb(210, 219, 232)), 430, y, 360, 92)
            e.Graphics.DrawString("Informations du rapport", fontBlocGras, pinceauBleu, 42, y + 10)
            e.Graphics.DrawString("Période : " & titreRapport, fontBloc, Brushes.Black, 42, y + 34)
            e.Graphics.DrawString("Lignes : " & dtRapportAImprimer.Rows.Count.ToString(), fontBloc, Brushes.Black, 42, y + 54)
            e.Graphics.DrawString("Date impression : " & Date.Now.ToString("dd/MM/yyyy HH:mm"), fontBloc, Brushes.Black, 42, y + 74)
            e.Graphics.DrawString("Synthèse", fontBlocGras, pinceauBleu, 442, y + 10)
            e.Graphics.DrawString("FC : " & _impressionTotalFC.ToString("N0") & " FC", fontBloc, Brushes.Black, 442, y + 34)
            e.Graphics.DrawString("USD : " & _impressionTotalUSD.ToString("N0") & " USD", fontBloc, Brushes.Black, 442, y + 54)
            e.Graphics.DrawString("Source : Dépenses", fontBloc, Brushes.Black, 442, y + 74)
            y += 116

            Dim colDate As Integer = 42
            Dim colCategorie As Integer = 132
            Dim colDescription As Integer = 252
            Dim colMontant As Integer = 502
            Dim colDevise As Integer = 592
            Dim colSource As Integer = 652
            Dim colType As Integer = 722

            e.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(229, 239, 252)), 30, y, pageWidth, 28)
            e.Graphics.DrawString("Date", fontBlocGras, pinceauBleu, colDate, y + 6)
            e.Graphics.DrawString("Catégorie", fontBlocGras, pinceauBleu, colCategorie, y + 6)
            e.Graphics.DrawString("Description", fontBlocGras, pinceauBleu, colDescription, y + 6)
            e.Graphics.DrawString("Montant", fontBlocGras, pinceauBleu, colMontant, y + 6)
            e.Graphics.DrawString("Devise", fontBlocGras, pinceauBleu, colDevise, y + 6)
            e.Graphics.DrawString("Source", fontBlocGras, pinceauBleu, colSource, y + 6)
            e.Graphics.DrawString("Type", fontBlocGras, pinceauBleu, colType, y + 6)
            y += 34

            Dim ligneHauteur As Integer = 24
            While _impressionIndex < dtRapportAImprimer.Rows.Count
                If y + ligneHauteur > e.MarginBounds.Bottom - 130 Then
                    e.HasMorePages = True
                    Return
                End If

                Dim row As DataRow = dtRapportAImprimer.Rows(_impressionIndex)
                e.Graphics.DrawLine(New Pen(Color.FromArgb(232, 236, 242)), 30, y + 16, 790, y + 16)
                e.Graphics.DrawString(DirectCast(row("DateDepense"), Date).ToString("dd/MM/yyyy"), fontBloc, Brushes.Black, colDate, y)
                e.Graphics.DrawString(Convert.ToString(row("NomCategorie")), fontBloc, Brushes.Black, colCategorie, y)
                e.Graphics.DrawString(Convert.ToString(row("Description")), fontBloc, Brushes.Black, colDescription, y)
                e.Graphics.DrawString(FormatMontant(If(IsDBNull(row("Montant")), 0D, Convert.ToDecimal(row("Montant"))), Convert.ToString(row("Devise"))), fontBloc, Brushes.Black, colMontant, y)
                e.Graphics.DrawString(Convert.ToString(row("Devise")), fontBloc, Brushes.Black, colDevise, y)
                e.Graphics.DrawString(Convert.ToString(row("Source")), fontBloc, Brushes.Black, colSource, y)
                e.Graphics.DrawString(Convert.ToString(row("TypeDepense")), fontBloc, Brushes.Black, colType, y)
                y += ligneHauteur
                _impressionIndex += 1
            End While

            y += 12
            e.Graphics.DrawRectangle(New Pen(Color.FromArgb(17, 35, 74), 1.4F), 470, y, 320, 44)
            e.Graphics.DrawString("TOTAL GÉNÉRAL FC", fontBlocGras, pinceauBleu, 486, y + 7)
            e.Graphics.DrawString(_impressionTotalFC.ToString("N0") & " FC", New Font("Segoe UI", 12, FontStyle.Bold), Brushes.Black, 650, y + 8)
            y += 56

            e.Graphics.DrawRectangle(New Pen(Color.FromArgb(17, 35, 74), 1.4F), 470, y, 320, 44)
            e.Graphics.DrawString("TOTAL GÉNÉRAL USD", fontBlocGras, pinceauBleu, 486, y + 7)
            e.Graphics.DrawString(_impressionTotalUSD.ToString("N0") & " USD", New Font("Segoe UI", 12, FontStyle.Bold), Brushes.Black, 650, y + 8)
            y += 70

            e.Graphics.DrawString("Observation : rapport généré à partir des données filtrées et validées.", fontBloc, pinceauGris, 30, y)
            y += 38
            e.Graphics.DrawLine(Pens.Black, 70, y + 38, 250, y + 38)
            e.Graphics.DrawLine(Pens.Black, 530, y + 38, 710, y + 38)
            e.Graphics.DrawString("Responsable financier", fontBloc, Brushes.Black, 102, y + 42)
            e.Graphics.DrawString("Vérification / Contrôle", fontBloc, Brushes.Black, 552, y + 42)

            e.HasMorePages = False
        End Sub

        Private Sub ChargerParametresApplication()
            Try
                Dim connectionString As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(connectionString)
                Dim paramService As New ParametreService(New ParametreRepository(dal))
                _parametres = paramService.Charger()
            Catch
                _parametres = Nothing
            End Try
        End Sub

        Private Function FormaterSoldeUsd(montant As Decimal) As String
            Dim taux As Decimal = If(_parametres Is Nothing, 0D, _parametres.TauxUsd)
            Dim valeurAffichee As Decimal = montant
            If taux > 0D Then
                valeurAffichee = Decimal.Round((montant / taux) * taux, 0, MidpointRounding.AwayFromZero)
            Else
                valeurAffichee = Decimal.Round(montant, 0, MidpointRounding.AwayFromZero)
            End If
            Return valeurAffichee.ToString("N0") & " USD"
        End Function

    End Class

    '' --- Placeholder DTOs, Services, Repositories ---
    'Namespace DTO
    '    Public Class DepenseDTO
    '        Public Property Id As Integer
    '        Public Property Montant As Decimal
    '        Public Property Devise As String
    '        Public Property Source As String
    '        Public Property TypeDepense As String
    '        Public Property Description As String
    '        Public Property DateDepense As Date
    '        Public Property CategorieId As Integer
    '        Public Property NomCategorie As String ' Pour l'affichage
    '    End Class

    '    Public Class CategorieDepenseDTO
    '        Public Property Id As Integer
    '        Public Property NomCategorie As String
    '    End Class

    '    Public Class CaisseDTO
    '        Public Property EncaisseFC As Decimal
    '        Public Property EncaisseUSD As Decimal
    '        Public Property DepensesFC As Decimal
    '        Public Property DepensesUSD As Decimal
    '        Public Property SoldeFC As Decimal
    '        Public Property SoldeUSD As Decimal
    '        Public Property EstCloture As Boolean
    '    End Class

    '    Public Class BanqueDTO
    '        Public Property SoldeFC As Decimal
    '        Public Property SoldeUSD As Decimal
    '    End Class

    '    Public Class TransactionBancaireDTO
    '        Public Property Id As Integer
    '        Public Property DateTransaction As Date
    '        Public Property Description As String
    '        Public Property Montant As Decimal
    '        Public Property Devise As String
    '        Public Property TypeTransaction As String
    '    End Class

    '    Public Class DepenseParCategorieDTO
    '        Public Property Categorie As String
    '        Public Property MontantTotal As Decimal
    '    End Class

    '    Public Class EvolutionFinanceDTO
    '        Public Property MoisAnnee As String
    '        Public Property SoldeCaisse As Decimal
    '        Public Property SoldeBanque As Decimal
    '    End Class

    '    Public Class GlobalKpiDTO
    '        Public Property TotalEncaisseFC As Decimal
    '        Public Property TotalDepensesFC As Decimal
    '        Public Property SoldeGlobalBanqueFC As Decimal
    '    End Class
    'End Namespace

    'Namespace Services
    '    Public Class DepenseService
    '        Private ReadOnly _depenseRepo As DepenseRepository
    '        Private ReadOnly _banqueService As BanqueService
    '        Private ReadOnly _caisseService As CaisseService

    '        Public Sub New(depenseRepo As DepenseRepository, banqueService As BanqueService, caisseService As CaisseService)
    '            _depenseRepo = depenseRepo
    '            _banqueService = banqueService
    '            _caisseService = caisseService
    '        End Sub

    '        Public Function ObtenirDepensesParAnnee(annee As Integer) As List(Of DepenseDTO)
    '            ' Placeholder
    '            Return New List(Of DepenseDTO) From {
    '                New DepenseDTO() With {.Id = 1, .DateDepense = New Date(annee, 1, 15), .NomCategorie = "Loyer", .Description = "Loyer bureau", .Montant = 1000, .Devise = "USD", .Source = "Banque", .TypeDepense = "Normale"},
    '                New DepenseDTO() With {.Id = 2, .DateDepense = New Date(annee, 1, 20), .NomCategorie = "Fournitures", .Description = "Stylos", .Montant = 50, .Devise = "FC", .Source = "Caisse", .TypeDepense = "Normale"}
    '            }
    '        End Function

    '        Public Function ObtenirDepensesParMois(annee As Integer, mois As Integer) As List(Of DepenseDTO)
    '            ' Placeholder
    '            Return New List(Of DepenseDTO) From {
    '                New DepenseDTO() With {.Id = 1, .DateDepense = New Date(annee, mois, 10), .NomCategorie = "Transport", .Description = "Carburant", .Montant = 120, .Devise = "FC", .Source = "Caisse", .TypeDepense = "Normale"},
    '                New DepenseDTO() With {.Id = 2, .DateDepense = New Date(annee, mois, 25), .NomCategorie = "Repas", .Description = "Déjeuner client", .Montant = 80, .Devise = "USD", .Source = "Banque", .TypeDepense = "Exceptionnelle"}
    '            }
    '        End Function

    '        Public Sub AjouterDepense(depense As DepenseDTO)
    '            ' Placeholder
    '            MessageBox.Show("Dépense ajoutée (Service Placeholder)")
    '        End Sub

    '        Public Function ObtenirDepensesParCategorie() As List(Of DepenseParCategorieDTO)
    '            ' Placeholder
    '            Return New List(Of DepenseParCategorieDTO) From {
    '                New DepenseParCategorieDTO() With {.Categorie = "Loyer", .MontantTotal = 12000},
    '                New DepenseParCategorieDTO() With {.Categorie = "Transport", .MontantTotal = 5000},
    '                New DepenseParCategorieDTO() With {.Categorie = "Fournitures", .MontantTotal = 2000}
    '            }
    '        End Function
    '    End Class

    '    Public Class CategorieDepenseService
    '        Private ReadOnly _catRepo As CategorieDepenseRepository

    '        Public Sub New(catRepo As CategorieDepenseRepository)
    '            _catRepo = catRepo
    '        End Sub

    '        Public Function ObtenirToutesCategories() As List(Of CategorieDepenseDTO)
    '            ' Placeholder
    '            Return New List(Of CategorieDepenseDTO) From {
    '                New CategorieDepenseDTO() With {.Id = 1, .NomCategorie = "Loyer"},
    '                New CategorieDepenseDTO() With {.Id = 2, .NomCategorie = "Transport"},
    '                New CategorieDepenseDTO() With {.Id = 3, .NomCategorie = "Fournitures"},
    '                New CategorieDepenseDTO() With {.Id = 4, .NomCategorie = "Repas"}
    '            }
    '        End Function

    '        Public Sub AjouterCategorie(nom As String)
    '            ' Placeholder
    '            MessageBox.Show("Catégorie ajoutée (Service Placeholder)")
    '        End Sub
    '    End Class

    '    Public Class CaisseService
    '        Private ReadOnly _caisseRepo As CaisseRepository
    '        Private ReadOnly _depenseRepo As DepenseRepository
    '        Private ReadOnly _banqueService As BanqueService

    '        Public Sub New(caisseRepo As CaisseRepository, depenseRepo As DepenseRepository, banqueService As BanqueService)
    '            _caisseRepo = caisseRepo
    '            _depenseRepo = depRepo
    '            _banqueService = banqueService
    '        End Sub

    '        Public Function ObtenirCaisseDuJour() As CaisseDTO
    '            ' Placeholder
    '            Return New CaisseDTO() With {
    '                .EncaisseFC = 1500000,
    '                .EncaisseUSD = 2500,
    '                .DepensesFC = 300000,
    '                .DepensesUSD = 500,
    '                .SoldeFC = 1200000,
    '                .SoldeUSD = 2000,
    '                .EstCloture = False
    '            }
    '        End Function

    '        Public Function ObtenirEvolutionFinance() As List(Of EvolutionFinanceDTO)
    '            ' Placeholder
    '            Return New List(Of EvolutionFinanceDTO) From {
    '                New EvolutionFinanceDTO() With {.MoisAnnee = "Jan-26", .SoldeCaisse = 1000000, .SoldeBanque = 5000000},
    '                New EvolutionFinanceDTO() With {.MoisAnnee = "Fev-26", .SoldeCaisse = 1100000, .SoldeBanque = 5200000},
    '                New EvolutionFinanceDTO() With {.MoisAnnee = "Mar-26", .SoldeCaisse = 900000, .SoldeBanque = 5100000}
    '            }
    '        End Function

    '        Public Function ObtenirGlobalKpi() As GlobalKpiDTO
    '            ' Placeholder
    '            Return New GlobalKpiDTO() With {
    '                .TotalEncaisseFC = 15000000,
    '                .TotalDepensesFC = 5000000,
    '                .SoldeGlobalBanqueFC = 10000000
    '            }
    '        End Function
    '    End Class

    '    Public Class BanqueService
    '        Private ReadOnly _banqueRepo As BanqueRepository

    '        Public Sub New(banqueRepo As BanqueRepository)
    '            _banqueRepo = banqueRepo
    '        End Sub

    '        Public Function ObtenirSoldeBanque() As BanqueDTO
    '            ' Placeholder
    '            Return New BanqueDTO() With {
    '                .SoldeFC = 10000000,
    '                .SoldeUSD = 50000
    '            }
    '        End Function

    '        Public Function ObtenirHistoriqueTransactions() As List(Of TransactionBancaireDTO)
    '            ' Placeholder
    '            Return New List(Of TransactionBancaireDTO) From {
    '                New TransactionBancaireDTO() With {.Id = 1, .DateTransaction = New Date(2026, 5, 1), .Description = "Virement fournisseur", .Montant = 200000, .Devise = "FC", .TypeTransaction = "Débit"},
    '                New TransactionBancaireDTO() With {.Id = 2, .DateTransaction = New Date(2026, 5, 5), .Description = "Dépôt client", .Montant = 1000, .Devise = "USD", .TypeTransaction = "Crédit"}
    '            }
    '        End Function
    '    End Class
    'End Namespace

    'Namespace Repositories
    '    Public Class DepenseRepository
    '        Private ReadOnly _dal As DAL
    '        Public Sub New(dal As DAL)
    '            _dal = dal
    '        End Sub
    '        ' Méthodes CRUD pour les dépenses
    '    End Class

    '    Public Class CategorieDepenseRepository
    '        Private ReadOnly _dal As DAL
    '        Public Sub New(dal As DAL)
    '            _dal = dal
    '        End Sub
    '        ' Méthodes CRUD pour les catégories de dépenses
    '    End Class

    '    Public Class CaisseRepository
    '        Private ReadOnly _dal As DAL
    '        Public Sub New(dal As DAL)
    '            _dal = dal
    '        End Sub
    '        ' Méthodes CRUD pour la caisse
    '    End Class

    '    Public Class BanqueRepository
    '        Private ReadOnly _dal As DAL
    '        Public Sub New(dal As DAL)
    '            _dal = dal
    '        End Sub
    '        ' Méthodes CRUD pour la banque
    '    End Class
    'End Namespace

    'Public Class DAL
    '    Private ReadOnly _connectionString As String

    '    Public Sub New(connectionString As String)
    '        _connectionString = connectionString
    '    End Sub

    '    Public Function ExecuterTable(sql As String, commandType As System.Data.CommandType, parameters As List(Of System.Data.SqlClient.SqlParameter)) As DataTable
    '        ' Placeholder
    '        Return New DataTable()
    '    End Function
    'End Class

    'Module Extensions
    '    <System.Runtime.CompilerServices.Extension()>
    '    Public Function ToDataTable(Of T)(list As List(Of T)) As DataTable
    '        Dim dt As New DataTable()
    '        If list Is Nothing OrElse list.Count = 0 Then Return dt

    '        Dim properties As System.Reflection.PropertyInfo() = GetType(T).GetProperties()
    '        For Each prop As System.Reflection.PropertyInfo In properties
    '            dt.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType)
    '        Next

    '        For Each item As T In list
    '            Dim row As DataRow = dt.NewRow()
    '            For Each prop As System.Reflection.PropertyInfo In properties
    '                row(prop.Name) = prop.GetValue(item) ?? DBNull.Value
    '            Next
    '            dt.Rows.Add(row)
    '        Next
    '        Return dt
    '    End Function
    'End Module

End Namespace
