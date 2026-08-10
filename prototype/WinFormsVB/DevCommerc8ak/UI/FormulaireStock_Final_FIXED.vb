Option Strict On
Option Explicit On

Imports System
Imports System.Diagnostics
Imports Microsoft.VisualBasic
Imports System.Configuration
Imports System.Data
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D
Imports System.Drawing.Printing

Namespace DevCommerc8ak
    Public Class FormulaireStock
        Inherits Form

        ' --- DESIGN CONSTANTS ---
        Private ReadOnly ColorPrimary As Color = Color.FromArgb(52, 73, 94)
        Private ReadOnly ColorSecondary As Color = Color.FromArgb(41, 128, 185)
        Private ReadOnly ColorAccent As Color = Color.FromArgb(39, 174, 96)
        Private ReadOnly ColorDanger As Color = Color.FromArgb(192, 57, 43)
        Private ReadOnly ColorBackground As Color = Color.FromArgb(245, 247, 250)
        Private ReadOnly ColorCard As Color = Color.White
        Private ReadOnly ColorWhite As Color = Color.White
        Private ReadOnly ColorBorder As Color = Color.FromArgb(230, 230, 230)

        Private ReadOnly FontTitle As New Font("Segoe UI Semibold", 18.0F)
        Private ReadOnly FontLabel As New Font("Segoe UI Semibold", 9.0F)
        Private ReadOnly FontControl As New Font("Segoe UI", 9.5F)
        Private _isRefreshingFromEvent As Boolean

        ' --- COMPONENTS ---
        Private ReadOnly tabs As TabControl

        ' --- Entree ---
        Private ReadOnly txtReference As TextBox
        Private ReadOnly txtNomProduit As TextBox
        Private ReadOnly cmbCategorie As ComboBox
        Private ReadOnly chkProduitExistant As CheckBox
        Private ReadOnly cmbProduitExistant As ComboBox
        Private ReadOnly cmbUniteBase As ComboBox
        Private ReadOnly txtNbUniteParBase As TextBox
        Private ReadOnly cmbTypeGestionStockEntree As ComboBox
        Private ReadOnly cmbUniteMesureStockEntree As ComboBox
        Private ReadOnly txtContenuUnitePrincipaleEntree As TextBox
        Private ReadOnly txtContenuUniteSecondaireEntree As TextBox
        Private ReadOnly txtQuantiteEntree As TextBox
        Private ReadOnly txtQuantiteSecondaireEntree As TextBox
        Private ReadOnly lblStockActuel As Label
        Private ReadOnly lblStockActuelPiece As Label
        Private ReadOnly lblStockApres As Label
        Private ReadOnly lblStockApresPiece As Label
        Private ReadOnly txtPrixAchat As TextBox
        Private ReadOnly cmbDevise As ComboBox
        Private ReadOnly txtTaux As TextBox
        Private ReadOnly lblEquivalentCdf As Label
        Private ReadOnly txtCoefficientInput As TextBox
        Private ReadOnly txtCoefficientDetail As TextBox
        Private ReadOnly lblTypeCoefficient As Label
        Private ReadOnly lblMargeCalculee As Label
        Private ReadOnly lblMargeDetailCalculee As Label
        Private ReadOnly txtPrixGros As TextBox
        Private ReadOnly txtPrixDemi As TextBox
        Private ReadOnly txtPrixQuart As TextBox
        Private ReadOnly txtPrixPiece As TextBox
        Private ReadOnly txtPrixDouzaine As TextBox
        Private ReadOnly btnTypesPersonnalisesEntree As Button
        Private ReadOnly pnlTypesPersonnalisesEntree As FlowLayoutPanel
        Private ReadOnly chkGros As CheckBox
        Private ReadOnly chkDemi As CheckBox
        Private ReadOnly chkQuart As CheckBox
        Private ReadOnly chkPiece As CheckBox
        Private ReadOnly chkDouzaine As CheckBox
        Private ReadOnly gridTypesVente As DataGridView
        Private ReadOnly lblEquivalentType As Label
        Private ReadOnly dtpDateEntree As DateTimePicker
        Private ReadOnly txtObservationEntree As TextBox
        Private ReadOnly btnEnregistrerEntree As Button

        ' --- Sortie ---
        Private ReadOnly txtRechercheSortie As TextBox
        Private ReadOnly dtpSortieDu As DateTimePicker
        Private ReadOnly dtpSortieAu As DateTimePicker
        Private ReadOnly btnRafraichirSortie As Button
        Private ReadOnly gridSortieMois As DataGridView

        ' --- Sortie ---##################################
        Private ReadOnly lblQteAchter As Label
        Private ReadOnly lblSMontantAchat As Label
        Private ReadOnly lblSMoyenneAchat As Label
        Private ReadOnly lblStockApresPieceS As Label
        Private ReadOnly lblQte As Label
        Private ReadOnly lblMont As Label
        Private ReadOnly lblMoyenne As Label

        Private ReadOnly lblStock As Label
        Private ReadOnly lblEquivalent As Label
        Private ReadOnly lblTotalReel As Label


        Private ReadOnly txtReferenceFacture As TextBox
        Private ReadOnly lblPrixProd As Label
        Private ReadOnly cmbProduitSortie As ComboBox
        Private ReadOnly dtpDateSortie As DateTimePicker
        Private ReadOnly txtQuantiteSortie As TextBox
        Private ReadOnly txtStockRestant As TextBox
        Private ReadOnly cmbTypeVente As ComboBox
        Private ReadOnly txtDescriptionSortie As TextBox
        Private ReadOnly btnEnregistrerSortie As Button
        Private ReadOnly cmbSortieManuelleMotif As ComboBox
        Private ReadOnly cmbSortieManuelleClient As ComboBox
        Private ReadOnly lblSortieManuelleClient As Label
        Private ReadOnly lblMagasinDestination As Label
        Private ReadOnly cmbMagasinDestination As ComboBox
        Private ReadOnly btnAjouterMagasin As Button


        ' --- NOUVEAU: Sortie Manuelle ---
        Private ReadOnly txtSortieManuelleQte As TextBox
        Private ReadOnly txtSortieManuelleMotif As TextBox
        Private ReadOnly txtSortieManuelleClient As TextBox
        Private ReadOnly btnValiderSortieManuelle As Button
        Private gridPanier As DataGridView


        Private cmbProduit, cmbMotif, cmbClient As ComboBox
        Private txtQte, txtPrix As TextBox
        Private btnAjouter, btnValider, btnVider As Button
        Private lblTotalPanier As Label
        Private ReadOnly lblSousTotal As Label
        Private ReadOnly lblTotal As Label

        ' Onglet Dettes (NOUVEAU)
        Private ReadOnly tabDettes As TabPage
        Private ReadOnly gridDettes As DataGridView
        Private ReadOnly btnPayer, btnTicket As Button


        ' Onglet Dashboard (NOUVEAU)
        Private ReadOnly tabDashboardSorties As TabPage
        Private ReadOnly pnlKpi As FlowLayoutPanel
        Private gridDetailsDashboard As DataGridView
        Private btnActualiserDash As Button



        ' --- Inventaire ---
        Private ReadOnly cmbProduitInventaire As ComboBox
        Private ReadOnly gridEntrees As DataGridView
        Private ReadOnly gridSorties As DataGridView
        Private ReadOnly txtStockTheorique As TextBox
        Private ReadOnly txtStockReel As TextBox
        Private ReadOnly txtEcart As TextBox
        Private ReadOnly dtpDateInventaire As DateTimePicker
        Private ReadOnly txtObservationInventaire As TextBox
        Private ReadOnly txtUtilisateurInventaire As TextBox
        Private ReadOnly btnValiderInventaire As Button

        ' --- NOUVEAU: Analyse Inventaire ---
        Private ReadOnly lblAnalyseSortieGros As Label
        Private ReadOnly lblAnalyseSortiePiece As Label
        Private ReadOnly lblAnalyseRestantGros As Label
        Private ReadOnly lblAnalyseRestantPiece As Label
        Private ReadOnly lblAnalyseRealisation As Label

        ' --- Alertes ---
        Private ReadOnly gridAlertes As DataGridView
        Private ReadOnly btnRafraichirAlertes As Button

        ' --- Perte ---
        Private ReadOnly cmbProduitPerte As ComboBox
        Private ReadOnly txtQuantitePerte As TextBox
        Private ReadOnly cmbTypePerte As ComboBox
        Private ReadOnly dtpDatePerte As DateTimePicker
        Private ReadOnly txtObservationPerte As TextBox
        Private ReadOnly txtResponsablePerte As TextBox
        Private ReadOnly btnEnregistrerPerte As Button

        ' --- Rapport Entrees ---
        Private ReadOnly gridRapportEntrees As DataGridView
        Private ReadOnly dtpRapportDu As DateTimePicker
        Private ReadOnly dtpRapportAu As DateTimePicker
        Private ReadOnly btnChargerRapportEntrees As Button
        Private ReadOnly btnImprimerRapportEntrees As Button

        ' --- LOGIC VARIABLES ---
        Private _produitsTable As DataTable
        Private _categoriesTable As DataTable
        Private _produitsAutoCompleteSource As AutoCompleteStringCollection
        Private _coefficientCalcule As Decimal
        Private _coefficientDetailCalcule As Decimal
        Private _parametres As ParametreDTO
        Private ReadOnly _typeVenteService As TypeVenteService
        Private _typesVenteCourants As List(Of TypeVenteDTO) 'nouveau 
        Private _isFilteringProduits As Boolean
        Private ReadOnly _panier As List(Of PanierLigne)
        Private ReadOnly _typesPersonnalisesTemporairesParProduit As Dictionary(Of Integer, List(Of TypeVenteProduitDTO))
        Private _prochainTypeTemporaireId As Integer
        Private ReadOnly _prixManuelOverrides As Dictionary(Of String, Boolean)
        Private _isUpdatingPrixAutomatiques As Boolean
        Private _miseAJourCoefficientDepuisPrix As Boolean
        Private _isSavingEntree As Boolean
        Private _stockActuelEntreeBase As Decimal
        Private _rapportEntreesPrintRowIndex As Integer
        Private _rapportEntreesPrintPageIndex As Integer
        Private _rapportEntreesTable As DataTable

        Private Class PanierLigne
            Public Property ProduitId As Integer
            Public Property Libelle As String
            Public Property Unite As String
            Public Property PrixUnitaire As Decimal
            Public Property Quantite As Decimal
            Public Property QuantiteBase As Decimal
            Public Property QuantiteEquivalente As Decimal 'nouveau 
            Public Property QuantiteReelle As Decimal 'nouveau 
            Public Property Total As Decimal
        End Class

        Public Sub New()
            ' Form Settings
            Me.Text = "Gestion des Stocks & Inventaire - Paon Rehoboth"
            Me.Width = 1300
            Me.Height = 850
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = ColorBackground
            Me.Font = FontControl
            Me.AutoScaleMode = AutoScaleMode.Dpi
            Me.AutoScroll = True
            Me.MinimumSize = New Size(1080, 720)
            _typeVenteService = New TypeVenteService()
            _typesVenteCourants = New List(Of TypeVenteDTO)()
            _panier = New List(Of PanierLigne)()
            _typesPersonnalisesTemporairesParProduit = New Dictionary(Of Integer, List(Of TypeVenteProduitDTO))()
            _prochainTypeTemporaireId = -1
            _prixManuelOverrides = New Dictionary(Of String, Boolean)(StringComparer.OrdinalIgnoreCase)
            ' Main Layout
            Dim mainLayout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2, .AutoScroll = True}
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 60))
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            ' Header
            Dim pnlHeader As New Panel() With {.BackColor = ColorPrimary, .Dock = DockStyle.Fill}
            Dim lblTitle As New Label() With {
                .Text = "GESTION DES STOCKS INVENTAIRE",
                .ForeColor = Color.White,
                .Font = FontTitle,
                .AutoSize = True,
                .Left = 20,
                .Top = 15
            }
            pnlHeader.Controls.Add(lblTitle)
            mainLayout.Controls.Add(pnlHeader, 0, 0)

            ' TabControl
            tabs = New TabControl() With {.Dock = DockStyle.Fill, .Padding = New Point(12, 5)}
            mainLayout.Controls.Add(tabs, 0, 1)
            Me.Controls.Add(mainLayout)

            ' --- INITIALIZE TABS ---
            Dim tabEntree As New TabPage("Stock Entrée") With {.BackColor = ColorBackground, .AutoScroll = True}
            Dim tabSortie As New TabPage("Stock Sortie") With {.BackColor = ColorBackground, .AutoScroll = True}
            Dim tabInventaire As New TabPage("Inventaire") With {.BackColor = ColorBackground, .AutoScroll = True}
            Dim tabAlertes As New TabPage("Alertes") With {.BackColor = ColorBackground, .AutoScroll = True}
            Dim tabPerte As New TabPage("Perte") With {.BackColor = ColorBackground, .AutoScroll = True}
            Dim tabRapportEntrees As New TabPage("Rapport Entrées") With {.BackColor = ColorBackground, .AutoScroll = True}
            ' Nouveaux Onglets
            Dim tabSortieManuelle As New TabPage("Sorties Manuelle") With {.BackColor = ColorBackground, .AutoScroll = True}
            Me.tabDettes = New TabPage("Dettes & Créances") With {.BackColor = ColorBackground, .AutoScroll = True}
            Me.tabDashboardSorties = New TabPage("Dashboard Sorties") With {.BackColor = ColorBackground, .AutoScroll = True}

            tabs.TabPages.AddRange(New TabPage() {tabEntree, tabSortie, tabSortieManuelle, Me.tabDettes, tabInventaire, tabAlertes, tabPerte, tabRapportEntrees})

            ' --- TAB ENTREE DESIGN ---
            Dim layoutEntree As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(10), .ColumnCount = 1, .RowCount = 2}
            layoutEntree.RowStyles.Add(New RowStyle(SizeType.Absolute, 545)) ' Cartes Stock Entrée incluant Validation
            layoutEntree.RowStyles.Add(New RowStyle(SizeType.Percent, 100)) ' Grille Informations Produit
            'tabEntree.Controls.Add(layoutEntree)

            Dim mainTableEntree As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 3, .Padding = New Padding(5)}
            mainTableEntree.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            mainTableEntree.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            mainTableEntree.RowStyles.Add(New RowStyle(SizeType.Absolute, 240)) ' Infos Produit / Unité/ Card produit
            mainTableEntree.RowStyles.Add(New RowStyle(SizeType.Absolute, 220)) 'Card Finance / Prix
            mainTableEntree.RowStyles.Add(New RowStyle(SizeType.Absolute, 85)) ' Validation compacte
            ' mainTableEntree.RowStyles.Add(New RowStyle(SizeType.Absolute, 80))  ' Bouton

            ' Card 1: Produit
            Dim cardProduit As Panel = CreateCard(600, 220, "INFORMATIONS PRODUIT")
            chkProduitExistant = New CheckBox() With {.Text = "Produit existant", .Left = 20, .Top = 45, .AutoSize = True, .Checked = True}
            cmbProduitExistant = New ComboBox() With {.Left = 160, .Top = 42, .Width = 250, .DropDownStyle = ComboBoxStyle.DropDown}
            cmbProduitExistant.AutoCompleteMode = AutoCompleteMode.SuggestAppend
            cmbProduitExistant.AutoCompleteSource = AutoCompleteSource.CustomSource
            txtNomProduit = New TextBox() With {.Left = 160, .Top = 75, .Width = 250}
            cmbCategorie = New ComboBox() With {.Left = 160, .Top = 105, .Width = 150}
            txtReference = New TextBox() With {.Left = 160, .Top = 135, .Width = 250, .ReadOnly = True}
            cardProduit.Controls.AddRange(New Control() {
                New Label() With {.Text = "Nom produit", .Left = 20, .Top = 78, .AutoSize = True},
                New Label() With {.Text = "Categorie", .Left = 20, .Top = 108, .AutoSize = True},
                New Label() With {.Text = "Reference", .Left = 20, .Top = 138, .AutoSize = True},
                chkProduitExistant, cmbProduitExistant, txtNomProduit, cmbCategorie, txtReference
            })
            ' layoutEntree.Controls.Add(cardProduit)

            ' Card 2: Unite
            Dim cardUnite As Panel = CreateCard(600, 235, "UNITÉ & CONVERSION")
            cmbUniteBase = New ComboBox() With {.Left = 160, .Top = 45, .Width = 150, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbUniteBase.Items.AddRange(New Object() {"Carton", "Sac", "Paquet", "Farde", "Plateau", "Seau", "Bidon", "Bouteille", "Boîte", "Pièce"})
            txtNbUniteParBase = New TextBox() With {.Left = 160, .Top = 75, .Width = 100}
            txtQuantiteEntree = New TextBox() With {.Left = 160, .Top = 105, .Width = 100}
            txtQuantiteSecondaireEntree = New TextBox() With {.Left = 160, .Top = 135, .Width = 100}
            cmbTypeGestionStockEntree = New ComboBox() With {.Left = 455, .Top = 45, .Width = 125, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbTypeGestionStockEntree.Items.AddRange(New Object() {"UNITE", "MESURE"})
            cmbTypeGestionStockEntree.SelectedItem = "UNITE"
            cmbUniteMesureStockEntree = New ComboBox() With {.Left = 455, .Top = 75, .Width = 125, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbUniteMesureStockEntree.Items.AddRange(New Object() {"KG", "G", "L", "ML", "M", "CM"})
            cmbUniteMesureStockEntree.SelectedItem = "KG"
            txtContenuUnitePrincipaleEntree = New TextBox() With {.Left = 455, .Top = 105, .Width = 125}
            txtContenuUniteSecondaireEntree = New TextBox() With {.Left = 455, .Top = 135, .Width = 125}
            lblStockActuel = New Label() With {.Left = 20, .Top = 170, .AutoSize = True, .ForeColor = ColorSecondary}
            lblStockActuelPiece = New Label() With {.Left = 20, .Top = 190, .AutoSize = True}
            lblStockApres = New Label() With {.Left = 300, .Top = 170, .AutoSize = True, .ForeColor = ColorAccent}
            lblStockApresPiece = New Label() With {.Left = 300, .Top = 190, .AutoSize = True}
            cardUnite.Controls.AddRange(New Control() {
                New Label() With {.Text = "Unité base", .Left = 20, .Top = 48, .AutoSize = True},
                New Label() With {.Text = "Nb unités/base", .Left = 20, .Top = 78, .AutoSize = True},
                New Label() With {.Text = "Quantité entrée", .Left = 20, .Top = 108, .AutoSize = True},
                New Label() With {.Text = "Qté secondaire bonus", .Left = 20, .Top = 138, .AutoSize = True},
                New Label() With {.Text = "Mode stock", .Left = 300, .Top = 48, .AutoSize = True},
                New Label() With {.Text = "Unité mesure", .Left = 300, .Top = 78, .AutoSize = True},
                New Label() With {.Text = "Contenu principal", .Left = 300, .Top = 108, .AutoSize = True},
                New Label() With {.Text = "Contenu secondaire", .Left = 300, .Top = 138, .AutoSize = True},
                cmbUniteBase, txtNbUniteParBase, txtQuantiteEntree, txtQuantiteSecondaireEntree, cmbTypeGestionStockEntree, cmbUniteMesureStockEntree, txtContenuUnitePrincipaleEntree, txtContenuUniteSecondaireEntree, lblStockActuel, lblStockActuelPiece, lblStockApres, lblStockApresPiece
            })
            'layoutEntree.Controls.Add(cardUnite)

            ' Card 3: Finance
            Dim cardFinance As Panel = CreateCard(600, 200, "INFORMATIONS FINANCIÈRES")
            txtPrixAchat = New TextBox() With {.Left = 150, .Top = 45, .Width = 120}
            cmbDevise = New ComboBox() With {.Left = 280, .Top = 45, .Width = 70, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbDevise.Items.AddRange(New Object() {"CDF", "USD"})
            cmbDevise.SelectedIndex = 0
            txtTaux = New TextBox() With {.Left = 360, .Top = 45, .Width = 80, .ReadOnly = True}
            lblEquivalentCdf = New Label() With {.Left = 440, .Top = 45, .Width = 300, .AutoSize = False, .ForeColor = ColorSecondary}
            txtCoefficientInput = New TextBox() With {.Left = 150, .Top = 75, .Width = 120}
            txtCoefficientDetail = New TextBox() With {.Left = 150, .Top = 135, .Width = 120}
            lblTypeCoefficient = New Label() With {.Left = 280, .Top = 78, .AutoSize = True}
            lblMargeCalculee = New Label() With {.Left = 150, .Top = 105, .AutoSize = True, .ForeColor = ColorAccent}
            lblMargeDetailCalculee = New Label() With {.Left = 150, .Top = 165, .AutoSize = True, .ForeColor = ColorAccent}
            cardFinance.Controls.AddRange(New Control() {
                New Label() With {.Text = "Prix Achat", .Left = 20, .Top = 48, .AutoSize = True},
                New Label() With {.Text = "Coeff. Gros", .Left = 20, .Top = 78, .AutoSize = True},
                New Label() With {.Text = "Coeff. Détail", .Left = 20, .Top = 138, .AutoSize = True},
                txtPrixAchat, cmbDevise, txtTaux, lblEquivalentCdf, txtCoefficientInput, txtCoefficientDetail, lblTypeCoefficient, lblMargeCalculee, lblMargeDetailCalculee
            })
            'layoutEntree.Controls.Add(cardFinance)

            ' Card 4: Prix Vente
            Dim cardPrix As Panel = CreateCard(600, 350, "PRIX DE VENTE CALCULÉS")
            txtPrixGros = New TextBox() With {.Name = "txtPrixGros", .Left = 160, .Top = 45, .Width = 120}
            txtPrixDemi = New TextBox() With {.Name = "txtPrixDemi", .Left = 160, .Top = 75, .Width = 120}
            txtPrixQuart = New TextBox() With {.Name = "txtPrixQuart", .Left = 160, .Top = 105, .Width = 120}
            txtPrixPiece = New TextBox() With {.Name = "txtPrixPiece", .Left = 160, .Top = 135, .Width = 120}
            txtPrixDouzaine = New TextBox() With {.Name = "txtPrixDouzaine", .Left = 160, .Top = 165, .Width = 120, .Visible = True}
            btnTypesPersonnalisesEntree = New Button() With {.Text = "Créer type personnalisé", .Left = 300, .Top = 42, .Width = 210, .Height = 32, .BackColor = ColorSecondary, .ForeColor = ColorWhite, .FlatStyle = FlatStyle.Flat}
            btnTypesPersonnalisesEntree.FlatAppearance.BorderSize = 0
            Dim lblTypesPersonnalisesEntree As New Label() With {.Text = "Types personnalisés", .Left = 300, .Top = 82, .AutoSize = True}
            pnlTypesPersonnalisesEntree = New FlowLayoutPanel() With {
                .Left = 300,
                .Top = 105,
                .Width = 290,
                .Height = 85,
                .AutoScroll = True,
                .FlowDirection = FlowDirection.TopDown,
                .WrapContents = False,
                .BorderStyle = BorderStyle.FixedSingle,
                .BackColor = Color.White
            }
            chkGros = New CheckBox() With {.Text = "Gros", .Left = 20, .Top = 45, .AutoSize = True, .Checked = True}
            chkDemi = New CheckBox() With {.Text = "Demi", .Left = 20, .Top = 75, .AutoSize = True}
            chkQuart = New CheckBox() With {.Text = "Quart", .Left = 20, .Top = 105, .AutoSize = True}
            chkPiece = New CheckBox() With {.Text = "Pièce", .Left = 20, .Top = 135, .AutoSize = True, .Checked = True}
            chkDouzaine = New CheckBox() With {.Text = "Douzaine", .Left = 20, .Top = 165, .AutoSize = True, .Visible = True}
            cardPrix.Controls.AddRange(New Control() {
                chkGros, chkDemi, chkQuart, chkPiece, chkDouzaine,
                txtPrixGros, txtPrixDemi, txtPrixQuart, txtPrixPiece, txtPrixDouzaine, btnTypesPersonnalisesEntree, lblTypesPersonnalisesEntree, pnlTypesPersonnalisesEntree
            })
            ' layoutEntree.Controls.Add(cardPrix)

            ' Card 5: Validation
            Dim cardValidation As Panel = CreateCard(1220, 80, "VALIDATION")
            dtpDateEntree = New DateTimePicker() With {.Left = 120, .Top = 42, .Width = 165}
            txtObservationEntree = New TextBox() With {.Left = 405, .Top = 42, .Width = 360, .Anchor = AnchorStyles.Top Or AnchorStyles.Left}
            btnEnregistrerEntree = New Button() With {
                .Text = "ENREGISTRER L'ENTRÉE",
                .Left = 785, .Top = 36,
                .Width = 250, .Height = 36,
                .BackColor = ColorAccent,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }

            cardValidation.Controls.AddRange(New Control() {
                New Label() With {.Text = "Date Entrée", .Left = 20, .Top = 45, .AutoSize = True},
                New Label() With {.Text = "Observation", .Left = 315, .Top = 45, .AutoSize = True},
                dtpDateEntree, txtObservationEntree, btnEnregistrerEntree
            })
            'layoutEntree.Controls.Add(cardValidation)
            'gridTypesVente = New DataGridView() With {.Left = 10, .Top = 455, .Width = 1210, .Height = 150, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AutoGenerateColumns = True}

            Dim cardGrid As Panel = CreateCard2(1220, 100, "Informations Produit")
            gridTypesVente = CreateStyledGrid()

            cardGrid.Controls.AddRange(New Control() {
                               gridTypesVente
            })
            'gridTypesVente = CreateStyledGrid()

            mainTableEntree.Controls.Add(cardProduit, 0, 0)
            mainTableEntree.Controls.Add(cardUnite, 1, 0)
            mainTableEntree.Controls.Add(cardFinance, 0, 1)
            mainTableEntree.Controls.Add(cardPrix, 1, 1)
            mainTableEntree.Controls.Add(cardValidation, 0, 2)
            mainTableEntree.SetColumnSpan(cardValidation, 2)
            'mainTableEntree.Controls.Add(cardGrid, 0, 3)
            'mainTableEntree.SetColumnSpan(cardGrid, 2)
            ' mainTableEntree.Controls.Add(layoutEntree)
            layoutEntree.Controls.Add(mainTableEntree, 0, 0)
            ' layoutEntree.Controls.Add(mainTableEntree, 0, 1)
            'layoutEntree.Controls.Add(mainTableEntree, 0, 2)
            layoutEntree.Controls.Add(cardGrid, 0, 1)
            tabEntree.Controls.Add(layoutEntree)
            'tabEntree.Controls.Add(mainTableEntree)


            ' --- TAB SORTIE DESIGN ---
            Dim layoutSortie As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(20), .ColumnCount = 1, .RowCount = 2}
            layoutSortie.RowStyles.Add(New RowStyle(SizeType.Absolute, 80))
            layoutSortie.RowStyles.Add(New RowStyle(SizeType.Absolute, 280)) ' NOUVEAU: Sortie Manuelle
            layoutSortie.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            tabSortie.Controls.Add(layoutSortie)

            ' Filtres Sortie
            Dim pnlFiltresSortie As New FlowLayoutPanel() With {.Dock = DockStyle.Fill}
            txtRechercheSortie = New TextBox() With {.Width = 250}
            dtpSortieDu = New DateTimePicker() With {.Width = 120}
            dtpSortieAu = New DateTimePicker() With {.Width = 120}
            btnRafraichirSortie = New Button() With {.Text = "Rafraîchir", .Width = 100, .BackColor = ColorSecondary, .Height = 30, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            pnlFiltresSortie.Controls.AddRange(New Control() {
                New Label() With {.Text = "Rechercher:", .AutoSize = True, .Margin = New Padding(0, 7, 0, 0)}, txtRechercheSortie,
                New Label() With {.Text = "Du:", .AutoSize = True, .Margin = New Padding(10, 7, 0, 0)}, dtpSortieDu,
                New Label() With {.Text = "Au:", .AutoSize = True, .Margin = New Padding(10, 7, 0, 0)}, dtpSortieAu,
                btnRafraichirSortie
            })
            layoutSortie.Controls.Add(pnlFiltresSortie, 0, 0)




            ' NOUVEAU: Sortie Manuelle
            Dim cardSortieManuelle As Panel = CreateCard(1200, 280, " SORTIE MANUELLE (HORS-VENTE)")
            txtReferenceFacture = New TextBox() With {.Left = 160, .Top = 45, .Width = 180,
                .Enabled = False, .BackColor = ColorWhite,
                .BorderStyle = BorderStyle.FixedSingle,
                .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                .TextAlign = HorizontalAlignment.Center
            }


            dtpDateSortie = New DateTimePicker() With {.Left = 160, .Top = 105, .Width = 140, .Format = DateTimePickerFormat.Short}
            lblPrixProd = New Label() With {.Left = 320, .Top = 165, .AutoSize = True, .ForeColor = ColorSecondary}
            lblStock = New Label() With {.Left = 530, .Top = 120, .AutoSize = True, .ForeColor = ColorSecondary, .Font = New Font("Segoe UI", 9, FontStyle.Italic)}
            lblEquivalent = New Label() With {.Left = 530, .Top = 140, .AutoSize = True, .ForeColor = ColorDanger, .Font = New Font("Segoe UI", 9, FontStyle.Italic)}
            lblTotalReel = New Label() With {.Left = 530, .Top = 162, .AutoSize = True, .ForeColor = ColorAccent, .Font = New Font("Segoe UI", 9, FontStyle.Italic)}
            cmbSortieManuelleMotif = New ComboBox() With {.Left = 160, .Top = 195, .Width = 160, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbSortieManuelleMotif.Items.AddRange(New Object() {"Dettes Client", "Demande Patron", "Don", "Dettes Patron", "Colis Noel", "Colis Nouvel ans"})
            cmbSortieManuelleClient = New ComboBox() With {.Left = 530, .Top = 45, .Width = 240, .DropDownStyle = ComboBoxStyle.DropDownList}
            lblQteAchter = New Label() With {.Left = 890, .Top = 38, .AutoSize = True, .BackColor = ColorSuccess, .ForeColor = Color.White, .Font = New Font("Segoe UI Variable Display", 9.5F, FontStyle.Bold)}
            lblSMontantAchat = New Label() With {.Left = 1030, .Top = 38, .AutoSize = True, .BackColor = ColorPrimary, .ForeColor = Color.White, .Font = New Font("Segoe UI Variable Display", 9.5F, FontStyle.Bold)}
            lblSMoyenneAchat = New Label() With {.Left = 890, .Top = 56, .AutoSize = True, .BackColor = ColorSecondary, .ForeColor = Color.White, .Font = New Font("Segoe UI Variable Display", 9.5F, FontStyle.Bold)}
            txtDescriptionSortie = New TextBox() With {.Left = 530, .Top = 75, .Width = 500, .Height = 500}
            btnEnregistrerSortie = New Button() With {.Text = "Valider Sortie", .Left = 860, .Top = 195, .Width = 160, .Height = 35, .BackColor = ColorDanger, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            cardSortieManuelle.Controls.AddRange(New Control() {
                New Label() With {.Text = "Ref facture", .Left = 20, .Top = 48, .AutoSize = True}, txtReferenceFacture,
                               New Label() With {.Text = "Date", .Left = 20, .Top = 108, .AutoSize = True}, dtpDateSortie,
                New Label() With {.Text = "Quantité", .Left = 20, .Top = 138, .AutoSize = True}, txtQuantiteSortie,
                             New Label() With {.Text = "Motif", .Left = 20, .Top = 198, .AutoSize = True}, cmbSortieManuelleMotif,
                New Label() With {.Text = "Client", .Left = 420, .Top = 48, .AutoSize = True}, cmbSortieManuelleClient,
                 New Label() With {.Text = "Qte deja Acheter", .Left = 780, .Top = 38, .AutoSize = True}, lblQteAchter,
                 New Label() With {.Text = "Montant Global", .Left = 920, .Top = 38, .AutoSize = True}, lblSMontantAchat,
                  New Label() With {.Text = "Moyenne Achat", .Left = 780, .Top = 56, .AutoSize = True}, lblSMoyenneAchat,
                 New Label() With {.Text = "Description", .Left = 420, .Top = 78, .AutoSize = True}, txtDescriptionSortie,
                             btnEnregistrerSortie, lblStock, lblEquivalent, lblTotalReel
            })
            'layoutSortie.Controls.Add(cardSortieManuelle, 0, 1)

            gridSortieMois = CreateStyledGrid()
            layoutSortie.Controls.Add(gridSortieMois, 0, 1)



            ''###########"" SORTIE MANUELLE #############3
            Dim mainLayoutSortie As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2}
            mainLayoutSortie.RowStyles.Add(New RowStyle(SizeType.Absolute, 280))
            mainLayoutSortie.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            ' Formulaire de saisie (Carte)
            Dim pnlSaisie As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(20)}
            ' ... (Ajout des contrôles cmbProduit, cmbMotif, txtQte, btnAjouter)
            Dim lblP As New Label() With {.Text = "Produit:", .Location = New Point(20, 20), .AutoSize = True}
            cmbProduitSortie = New ComboBox() With {.Location = New Point(20, 40), .Width = 250, .DropDownStyle = ComboBoxStyle.DropDownList}

            Dim lblQ As New Label() With {.Text = "Quantité:", .Location = New Point(280, 20), .AutoSize = True}
            txtQuantiteSortie = New TextBox() With {.Location = New Point(280, 40), .Width = 80}

            Dim lblM As New Label() With {.Text = "Motif:", .Location = New Point(370, 20), .AutoSize = True}
            cmbMotif = New ComboBox() With {.Location = New Point(370, 40), .Width = 150, .DropDownStyle = ComboBoxStyle.DropDownList}



            lblSortieManuelleClient = New Label() With {.Text = "Client", .Location = New Point(530, 20), .AutoSize = True}
            cmbSortieManuelleClient = New ComboBox() With {.Location = New Point(530, 40), .Width = 200, .DropDownStyle = ComboBoxStyle.DropDownList}
            lblMagasinDestination = New Label() With {.Text = "Magasin destination:", .Location = New Point(530, 20), .AutoSize = True, .Visible = False}
            cmbMagasinDestination = New ComboBox() With {.Location = New Point(530, 40), .Width = 200, .DropDownStyle = ComboBoxStyle.DropDownList, .Visible = False}
            btnAjouterMagasin = New Button() With {.Text = "+", .Location = New Point(735, 39), .Width = 34, .Height = 28, .Visible = False, .BackColor = ColorAccent, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            lblQte = New Label() With {.Text = "Qte deja Acheter", .Left = 780, .Top = 30, .AutoSize = True}
            lblMont = New Label() With {.Text = "Montant Global", .Left = 920, .Top = 30, .AutoSize = True}
            lblMoyenne = New Label() With {.Text = "Moyenne Achat", .Left = 780, .Top = 48, .AutoSize = True}
            lblQteAchter = New Label() With {.Left = 890, .Top = 30, .AutoSize = True, .BackColor = ColorSuccess, .ForeColor = Color.White, .Font = New Font("Segoe UI Variable Display", 9.5F, FontStyle.Bold)}
            lblSMontantAchat = New Label() With {.Left = 1030, .Top = 30, .AutoSize = True, .BackColor = ColorPrimary, .ForeColor = Color.White, .Font = New Font("Segoe UI Variable Display", 9.5F, FontStyle.Bold)}
            lblSMoyenneAchat = New Label() With {.Left = 890, .Top = 48, .AutoSize = True, .BackColor = ColorSecondary, .ForeColor = Color.White, .Font = New Font("Segoe UI Variable Display", 9.5F, FontStyle.Bold)}

            Dim lblTypeVente As New Label() With {.Text = "Type vente:", .Location = New Point(20, 80), .AutoSize = True}
            cmbTypeVente = New ComboBox() With {.Location = New Point(20, 100), .Width = 160, .DropDownStyle = ComboBoxStyle.DropDownList}
            lblPrixProd = New Label() With {.Location = New Point(190, 105), .AutoSize = True, .ForeColor = ColorSecondary}

            lblStock = New Label() With {.Left = 780, .Top = 120, .AutoSize = True, .ForeColor = ColorSecondary, .Font = New Font("Segoe UI", 9, FontStyle.Italic)}
            lblEquivalent = New Label() With {.Left = 780, .Top = 140, .AutoSize = True, .ForeColor = ColorDanger, .Font = New Font("Segoe UI", 9, FontStyle.Italic)}
            lblTotalReel = New Label() With {.Left = 780, .Top = 162, .AutoSize = True, .ForeColor = ColorAccent, .Font = New Font("Segoe UI", 9, FontStyle.Italic)}

            btnAjouter = New Button() With {.Text = "Ajouter au Panier", .Location = New Point(20, 130), .Width = 150, .Height = 35, .BackColor = ColorAccent, .ForeColor = Color.White}
            btnVider = New Button() With {.Text = "RETIRER", .Location = New Point(180, 130), .Width = 80, .Height = 35}

            'lblTotalPanier = New Label() With {.Text = "TOTAL: 0 FC", .Location = New Point(20, 175), .Font = FontBold, .ForeColor = ColorPrimary, .AutoSize = True}
            lblSousTotal = New Label() With {
                .Text = "SOUS-TOTAL : 0 FC", .Location = New Point(780, 190), .Width = 200,
                .Font = New Font("Segoe UI", 11, FontStyle.Bold), .ForeColor = ColorPrimary
            }

            lblTotal = New Label() With {
                .Text = "TOTAL À PAYER :0 FC", .Location = New Point(725, 210), .Width = 250,
                .Font = New Font("Segoe UI", 14, FontStyle.Bold), .ForeColor = ColorSecondary,
                .TextAlign = ContentAlignment.MiddleRight}
            btnValider = New Button() With {.Text = "VALIDER LA SORTIE", .Location = New Point(20, 200), .Width = 250, .Height = 45, .BackColor = ColorSuccess, .ForeColor = Color.White, .Font = FontBold}

            pnlSaisie.Controls.AddRange({lblP, cmbProduitSortie, lblQ, txtQuantiteSortie, lblM, cmbMotif, lblSortieManuelleClient, cmbSortieManuelleClient, lblMagasinDestination, cmbMagasinDestination, btnAjouterMagasin, lblQte, lblMont, lblMoyenne, lblQteAchter, lblSMontantAchat, lblSMoyenneAchat, lblTypeVente, cmbTypeVente, lblPrixProd, btnAjouter, btnVider, lblSousTotal, lblTotal, btnValider, lblStock, lblEquivalent, lblTotalReel})


            ' Grille du Panier
            gridPanier = CreateStyledGrid()


            ' ... (Configuration des colonnes)

            mainLayoutSortie.Controls.Add(pnlSaisie, 0, 0)
            mainLayoutSortie.Controls.Add(gridPanier, 0, 1)
            tabSortieManuelle.Controls.Add(mainLayoutSortie)

            '############ Tab Dette Design ################"

            Dim mainLayoutDette As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2}
            mainLayoutDette.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            mainLayoutDette.RowStyles.Add(New RowStyle(SizeType.Absolute, 60))

            gridDettes = CreateStyledGrid()
            Dim pnlActions As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft, .Padding = New Padding(10)}

            btnPayer = New Button() With {.Text = "Enregistrer Paiement", .BackColor = ColorAccent, .ForeColor = Color.White, .Height = 40, .Width = 180, .Font = FontBold}
            btnTicket = New Button() With {.Text = "Imprimer Ticket", .Height = 40, .Width = 150}

            pnlActions.Controls.AddRange({btnPayer, btnTicket})
            mainLayoutDette.Controls.Add(gridDettes, 0, 0)
            mainLayoutDette.Controls.Add(pnlActions, 0, 1)
            Me.tabDettes.Controls.Add(mainLayoutDette)

            ' --- TAB INVENTAIRE DESIGN ---
            Dim layoutInventaire As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(20), .ColumnCount = 2, .RowCount = 3}
            layoutInventaire.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35))
            layoutInventaire.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 65))
            layoutInventaire.RowStyles.Add(New RowStyle(SizeType.Absolute, 210))
            layoutInventaire.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            tabInventaire.Controls.Add(layoutInventaire)

            ' Saisie Inventaire
            Dim cardSaisieInv As Panel = CreateCard(600, 360, "SAISIE INVENTAIRE")
            cmbProduitInventaire = New ComboBox() With {.Left = 120, .Top = 45, .Width = 250, .DropDownStyle = ComboBoxStyle.DropDownList}
            txtStockTheorique = New TextBox() With {.Left = 120, .Top = 75, .Width = 100, .ReadOnly = True}
            txtStockReel = New TextBox() With {.Left = 120, .Top = 105, .Width = 100}
            txtEcart = New TextBox() With {.Left = 300, .Top = 105, .Width = 100, .ReadOnly = True}
            txtObservationInventaire = New TextBox() With {.Left = 120, .Top = 150, .Width = 280, .Height = 60, .Multiline = True, .ScrollBars = ScrollBars.Vertical}
            btnValiderInventaire = New Button() With {.Text = "Valider Inventaire", .Left = 240, .Top = 225, .Width = 180, .Height = 44, .BackColor = ColorAccent, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            cardSaisieInv.Controls.AddRange(New Control() {
                New Label() With {.Text = "Produit:", .Left = 20, .Top = 48, .AutoSize = True}, cmbProduitInventaire,
                New Label() With {.Text = "Théorique:", .Left = 20, .Top = 78, .AutoSize = True}, txtStockTheorique,
                New Label() With {.Text = "Réel:", .Left = 20, .Top = 108, .AutoSize = True}, txtStockReel,
                New Label() With {.Text = "Ecart:", .Left = 240, .Top = 108, .AutoSize = True}, txtEcart,
                New Label() With {.Text = "Observation:", .Left = 20, .Top = 154, .AutoSize = True}, txtObservationInventaire,
                btnValiderInventaire
            })
            layoutInventaire.Controls.Add(cardSaisieInv, 0, 0)

            ' NOUVEAU: Analyse Inventaire
            Dim cardAnalyse As Panel = CreateCard(550, 330, "ANALYSE PRODUIT")
            lblAnalyseSortieGros = New Label() With {.Text = "Sorties Gros: 0", .Left = 20, .Top = 45, .AutoSize = True}
            lblAnalyseSortiePiece = New Label() With {.Text = "Sorties Pièces: 0", .Left = 20, .Top = 80, .AutoSize = True}
            lblAnalyseRestantGros = New Label() With {.Text = "Restant Gros: 0", .Left = 220, .Top = 45, .AutoSize = True, .ForeColor = ColorSecondary}
            lblAnalyseRestantPiece = New Label() With {.Text = "Restant Pièces: 0", .Left = 220, .Top = 65, .AutoSize = True, .ForeColor = ColorSecondary}
            lblAnalyseRealisation = New Label() With {.Text = "Réalisation: 0.00", .Left = 20, .Top = 105, .AutoSize = True, .Font = FontBold, .ForeColor = ColorAccent}
            cardAnalyse.Controls.AddRange(New Control() {lblAnalyseSortieGros, lblAnalyseSortiePiece, lblAnalyseRestantGros, lblAnalyseRestantPiece, lblAnalyseRealisation})
            layoutInventaire.Controls.Add(cardAnalyse, 1, 0)

            gridEntrees = CreateStyledGrid()
            gridSorties = CreateStyledGrid()
            'layoutInventaire.Controls.Add(gridEntrees, 0, 1)
            'layoutInventaire.Controls.Add(gridSorties, 0, 2)
            'layoutInventaire.SetColumnSpan(gridEntrees, 2)
            'layoutInventaire.SetColumnSpan(gridSorties, 2)
            layoutInventaire.Controls.Add(gridEntrees, 0, 1)
            layoutInventaire.Controls.Add(gridSorties, 1, 1)

            ' --- TAB ALERTES DESIGN ---
            Dim layoutAlertes As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(20), .ColumnCount = 1, .RowCount = 2}
            layoutAlertes.RowStyles.Add(New RowStyle(SizeType.Absolute, 50))
            layoutAlertes.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            btnRafraichirAlertes = New Button() With {.Text = "Rafraîchir Alertes", .Width = 150, .BackColor = ColorDanger, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            layoutAlertes.Controls.Add(btnRafraichirAlertes, 0, 0)
            gridAlertes = CreateStyledGrid()
            layoutAlertes.Controls.Add(gridAlertes, 0, 1)
            tabAlertes.Controls.Add(layoutAlertes)

            ' --- TAB PERTE DESIGN ---
            Dim layoutPerte As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(20)}
            Dim cardPerte As Panel = CreateCard(600, 300, "ENREGISTRER UNE PERTE")
            cmbProduitPerte = New ComboBox() With {.Left = 150, .Top = 45, .Width = 300, .DropDownStyle = ComboBoxStyle.DropDownList}
            txtQuantitePerte = New TextBox() With {.Left = 150, .Top = 85, .Width = 100}
            cmbTypePerte = New ComboBox() With {.Left = 150, .Top = 125, .Width = 200, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbTypePerte.Items.AddRange(New Object() {"AVARIE", "VOL", "PEREMPTION", "AUTRE"})
            dtpDatePerte = New DateTimePicker() With {.Left = 150, .Top = 165, .Width = 150}
            txtObservationPerte = New TextBox() With {.Left = 150, .Top = 205, .Width = 300, .Multiline = True, .Height = 50}
            btnEnregistrerPerte = New Button() With {.Text = "Enregistrer Perte", .Left = 150, .Top = 265, .Width = 200, .Height = 40, .BackColor = ColorDanger, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            cardPerte.Controls.AddRange(New Control() {
                New Label() With {.Text = "Produit:", .Left = 20, .Top = 48, .AutoSize = True}, cmbProduitPerte,
                New Label() With {.Text = "Quantité:", .Left = 20, .Top = 88, .AutoSize = True}, txtQuantitePerte,
                New Label() With {.Text = "Type:", .Left = 20, .Top = 128, .AutoSize = True}, cmbTypePerte,
                New Label() With {.Text = "Date:", .Left = 20, .Top = 168, .AutoSize = True}, dtpDatePerte,
                New Label() With {.Text = "Observation:", .Left = 20, .Top = 208, .AutoSize = True}, txtObservationPerte,
                btnEnregistrerPerte
            })
            layoutPerte.Controls.Add(cardPerte)
            tabPerte.Controls.Add(layoutPerte)

            ' --- TAB RAPPORT DESIGN ---
            Dim layoutRapport As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(20), .ColumnCount = 1, .RowCount = 2}
            layoutRapport.RowStyles.Add(New RowStyle(SizeType.Absolute, 60))
            layoutRapport.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            Dim pnlFiltresRapport As New FlowLayoutPanel() With {.Dock = DockStyle.Fill}
            dtpRapportDu = New DateTimePicker() With {.Width = 120}
            dtpRapportAu = New DateTimePicker() With {.Width = 120}
            btnChargerRapportEntrees = New Button() With {.Text = "Charger", .Width = 100, .BackColor = ColorSecondary, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            btnImprimerRapportEntrees = New Button() With {.Text = "Imprimer", .Width = 100, .BackColor = ColorAccent, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            pnlFiltresRapport.Controls.AddRange(New Control() {
                New Label() With {.Text = "Du:", .AutoSize = True, .Margin = New Padding(0, 7, 0, 0)}, dtpRapportDu,
                New Label() With {.Text = "Au:", .AutoSize = True, .Margin = New Padding(10, 7, 0, 0)}, dtpRapportAu,
                btnChargerRapportEntrees, btnImprimerRapportEntrees
            })
            layoutRapport.Controls.Add(pnlFiltresRapport, 0, 0)
            gridRapportEntrees = CreateStyledGrid()
            layoutRapport.Controls.Add(gridRapportEntrees, 0, 1)
            tabRapportEntrees.Controls.Add(layoutRapport)


            '################## TAB DASHBORD DESIGN #####################
            Dim mainLayoutDASH As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 2}
            mainLayoutDASH.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 60))
            mainLayoutDASH.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 40))

            mainLayoutDASH.RowStyles.Add(New RowStyle(SizeType.Absolute, 50))
            mainLayoutDASH.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            btnActualiserDash = New Button() With {.Text = "Rafraîchir les Statistiques", .Width = 200, .Height = 35}
            pnlKpi = New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .AutoScroll = True, .BackColor = Color.FromArgb(240, 240, 240)}
            gridDetailsDashboard = CreateStyledGrid()

            mainLayoutDASH.Controls.Add(btnActualiserDash, 0, 0)
            mainLayoutDASH.Controls.Add(pnlKpi, 0, 1)
            mainLayoutDASH.Controls.Add(gridDetailsDashboard, 1, 1)
            Me.tabDashboardSorties.Controls.Add(mainLayoutDASH)



            ' --- EVENT HANDLERS ---
            AddHandler Me.Load, AddressOf FormulaireStock_Load
            AddHandler chkProduitExistant.CheckedChanged, AddressOf BasculerProduitExistant
            AddHandler cmbProduitExistant.SelectedIndexChanged, AddressOf ChargerProduitSelection
            AddHandler cmbProduitExistant.TextUpdate, AddressOf FiltrerProduitsExistants
            AddHandler cmbProduitExistant.SelectionChangeCommitted, AddressOf SelectionnerProduitExistant
            AddHandler cmbProduitExistant.Leave, AddressOf SelectionnerProduitExistant
            AddHandler txtNomProduit.TextChanged, AddressOf GenererReferenceAutomatique
            AddHandler txtNomProduit.Leave, AddressOf VerifierDoublonNomProduit
            AddHandler cmbCategorie.SelectedIndexChanged, AddressOf GenererReferenceAutomatique
            AddHandler txtNbUniteParBase.TextChanged, AddressOf RecalculerStock
            AddHandler cmbTypeGestionStockEntree.SelectedIndexChanged, AddressOf ModeGestionStockEntreeChange
            AddHandler cmbUniteMesureStockEntree.SelectedIndexChanged, AddressOf RecalculerStock
            AddHandler txtContenuUnitePrincipaleEntree.TextChanged, AddressOf RecalculerStock
            AddHandler txtContenuUniteSecondaireEntree.TextChanged, AddressOf RecalculerStock
            AddHandler txtQuantiteEntree.TextChanged, AddressOf RecalculerStock
            AddHandler txtQuantiteSecondaireEntree.TextChanged, AddressOf RecalculerStock
            AddHandler txtPrixAchat.TextChanged, AddressOf RecalculerPrixAuto
            AddHandler cmbDevise.SelectedIndexChanged, AddressOf DeviseOuPrixAchatChange
            AddHandler txtCoefficientInput.TextChanged, AddressOf CoefficientInputChange
            AddHandler txtCoefficientDetail.TextChanged, AddressOf CoefficientDetailChange
            AddHandler chkGros.CheckedChanged, AddressOf RecalculerPrixAuto
            AddHandler chkDemi.CheckedChanged, AddressOf RecalculerPrixAuto
            AddHandler chkQuart.CheckedChanged, AddressOf RecalculerPrixAuto
            AddHandler chkPiece.CheckedChanged, AddressOf RecalculerPrixAuto
            AddHandler chkDouzaine.CheckedChanged, AddressOf RecalculerPrixAuto
            AddHandler txtPrixGros.TextChanged, AddressOf PrixVenteManuelChange
            AddHandler txtPrixDemi.TextChanged, AddressOf PrixVenteManuelChange
            AddHandler txtPrixQuart.TextChanged, AddressOf PrixVenteManuelChange
            AddHandler txtPrixPiece.TextChanged, AddressOf PrixVenteManuelChange
            AddHandler txtPrixDouzaine.TextChanged, AddressOf PrixVenteManuelChange
            AddHandler btnTypesPersonnalisesEntree.Click, AddressOf OuvrirTypesPersonnalisesEntree
            AddHandler btnEnregistrerEntree.Click, AddressOf EnregistrerEntree
            AddHandler btnRafraichirSortie.Click, AddressOf ChargerSortiesDuMois
            AddHandler txtRechercheSortie.TextChanged, AddressOf ChargerSortiesDuMois

            ' AddHandler txtRechercheSortie.TextChanged, AddressOf ChargerSortiesDuMois
            AddHandler dtpSortieDu.ValueChanged, AddressOf ChargerSortiesDuMois
            AddHandler dtpSortieAu.ValueChanged, AddressOf ChargerSortiesDuMois
            'AddHandler cmbProduitSortie.SelectedIndexChanged, AddressOf chargerTypesVente
            'AddHandler txtQuantiteSortie.TextChanged, AddressOf RecalculerStockSortie
            AddHandler btnRafraichirSortie.Click, AddressOf ChargerSortiesDuMois
            ' AddHandler cmbTypeVente.SelectedIndexChanged, AddressOf chargerPrix
            AddHandler cmbTypeVente.SelectedIndexChanged, AddressOf MiseAJourPrixUnitaire

            AddHandler cmbProduitSortie.SelectedIndexChanged, AddressOf ChargerUnites
            AddHandler txtQuantiteSortie.TextChanged, AddressOf MiseAJourIndicateursQuantite
            AddHandler cmbSortieManuelleClient.SelectedIndexChanged, AddressOf ChargerInfoAchatClientSelection
            AddHandler cmbMotif.SelectedIndexChanged, AddressOf MettreAJourVisibiliteSortieManuelle
            AddHandler cmbSortieManuelleMotif.SelectedIndexChanged, AddressOf MettreAJourVisibiliteSortieManuelle
            AddHandler btnAjouterMagasin.Click, AddressOf AjouterMagasinDestination

            AddHandler cmbProduitInventaire.SelectedIndexChanged, AddressOf ChargerInventaire
            AddHandler txtStockReel.TextChanged, AddressOf RecalculerEcart
            AddHandler btnValiderInventaire.Click, AddressOf ValiderInventaire
            AddHandler btnRafraichirAlertes.Click, AddressOf ChargerAlertes
            AddHandler btnEnregistrerPerte.Click, AddressOf EnregistrerPerte
            AddHandler btnChargerRapportEntrees.Click, AddressOf ChargerRapportEntrees
            AddHandler btnImprimerRapportEntrees.Click, AddressOf ImprimerRapportEntrees
            AddHandler gridRapportEntrees.CellFormatting, AddressOf FormaterCelluleRapportEntrees

            AddHandler btnAjouter.Click, AddressOf AjouterAuPanier
            ' AddHandler btnVider.Click, AddressOf RetirerDuPanier
            AddHandler btnVider.Click, AddressOf RetirerDuPanier

            ' NOUVEAU: Handlers
            AddHandler btnEnregistrerSortie.Click, AddressOf EnregistrerSortieManuelle
            AddHandler btnValider.Click, AddressOf ValiderSortieManuelle
            AddHandler btnActualiserDash.Click, AddressOf ChargerDashboardSorties
            AddHandler btnPayer.Click, AddressOf EnregistrerPaiementDette
            AddHandler btnTicket.Click, AddressOf ImprimerTicketDette

            ' Inventaire historique remplacé par FrmInventaireIntelligent
            txtStockReel.ReadOnly = True
            txtObservationInventaire.ReadOnly = True
            btnValiderInventaire.Enabled = False
            AddHandler AppEvents.StockModifie, AddressOf RafraichirDepuisEvenement
            AddHandler AppEvents.ProduitModifie, AddressOf RafraichirDepuisEvenement
        End Sub

        ' --- DESIGN HELPERS ---
        Private Function CreateCard(w As Integer, h As Integer, title As String) As Panel
            Dim pnl As New Panel() With {
                .Width = w, .Height = h,
                .BackColor = ColorCard,
                .Margin = New Padding(0, 0, 20, 20)
            }
            Dim lblTitle As New Label() With {
                .Text = title,
                .Font = FontLabel,
                .ForeColor = ColorSecondary,
                .AutoSize = True,
                .Left = 15, .Top = 10
            }
            Dim line As New Panel() With {
                .Height = 1, .Width = w - 30,
                .BackColor = ColorBorder,
                .Left = 15, .Top = 32
            }
            pnl.Controls.Add(lblTitle)
            pnl.Controls.Add(line)
            Return pnl
        End Function
        Private Function CreateCard2(w As Integer, h As Integer, title As String) As Panel
            Dim pnl As New Panel() With {
                .Width = w, .Height = h,
                .BackColor = ColorCard,
                .Margin = New Padding(0, 0, 20, 20)
            }
            Dim p As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Margin = New Padding(5), .Padding = New Padding(10)}
            p.Controls.Add(New Label() With {.Text = title, .Font = FontLabel, .ForeColor = ColorPrimary, .AutoSize = True, .Top = 5, .Left = 10})
            Return p
        End Function
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
                .GridColor = ColorBorder,
                .Dock = DockStyle.Fill
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

        ' --- LOGIQUE MÉTIER (REPRISE INTÉGRALE DE FormulaireStockBON.vb) ---

        Private Sub FormulaireStock_Load(sender As Object, e As EventArgs)
            Try
                ChargerCategories()
                ChargerProduits()
                ChargerParametres()
                ChargerMotifsSortie()
                ChargerMagasins()
                ChargerClientsActifs()
                ' ChargerSortiesMois(Nothing, EventArgs.Empty)
                ChargerSortiesDuMois(Nothing, EventArgs.Empty)
                ChargerDettes(Nothing, EventArgs.Empty)
                ChargerDashboardSorties(Nothing, EventArgs.Empty)
                ChargerAlertes(Nothing, EventArgs.Empty)
                BasculerProduitExistant(Nothing, EventArgs.Empty)
                MettreAJourVisibiliteSortieManuelle(Nothing, EventArgs.Empty)
                RafraichirTypesVente()
                RafraichirResumeTypesPersonnalisesEntree()
            Catch ex As Exception
                MessageBox.Show("Erreur chargement: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerCategories()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                _categoriesTable = dal.ExecuterTable(
                    "SELECT CategorieId, ISNULL(NomCategorie, '') AS NomCategorie FROM CategoriesProduits ORDER BY NomCategorie",
                    CommandType.Text,
                    Nothing)
                cmbCategorie.DataSource = Nothing
                cmbCategorie.DisplayMember = "NomCategorie"
                cmbCategorie.ValueMember = "CategorieId"
                cmbCategorie.DataSource = _categoriesTable
                cmbCategorie.SelectedIndex = -1
            Catch
                _categoriesTable = Nothing
                cmbCategorie.DataSource = Nothing
                cmbCategorie.Items.Clear()
            End Try
        End Sub

        'Private Sub ChargerCategories()
        '    Try
        '        Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
        '        Dim dal As New DAL(cs)
        '        cmbCategorie.DataSource = dal.ExecuterTable("SELECT CategorieId, Libelle FROM Categories", CommandType.Text)
        '        cmbCategorie.DisplayMember = "Libelle"
        '        cmbCategorie.ValueMember = "CategorieId"
        '    Catch
        '    End Try
        'End Sub

        'Private Sub ChargerProduits()
        '    Try
        '        Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
        '        Dim dal As New DAL(cs)
        '        _produitsTable = dal.ExecuterTable("SELECT ProduitId, Libelle, CategorieId, UnitePrincipale, ConversionUnite, PrixAchat, CoefficientGros, VenteGros, VenteDemi, VenteDetail, VenteDouzaine, PrixDetail, PrixQuart FROM Produits ORDER BY Libelle", CommandType.Text)

        '        cmbProduitExistant.DataSource = _produitsTable
        '        cmbProduitExistant.DisplayMember = "Libelle"
        '        cmbProduitExistant.ValueMember = "ProduitId"

        '        cmbProduitInventaire.DataSource = _produitsTable.Copy()
        '        cmbProduitInventaire.DisplayMember = "Libelle"
        '        cmbProduitInventaire.ValueMember = "ProduitId"

        '        cmbProduitPerte.DataSource = _produitsTable.Copy()
        '        cmbProduitPerte.DisplayMember = "Libelle"
        '        cmbProduitPerte.ValueMember = "ProduitId"
        '    Catch
        '    End Try
        'End Sub
        Private Sub ChargerClientsActifs()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim sql As String = "SELECT TOP 20 c.ClientId, c.NomClient, COUNT(*) AS NbAchats, " &
                                    "cast(SUM(f.MontantTotal) as int ) AS TotalAchats,cast( AVG(f.MontantTotal)as int) AS MoyenneAchat " &
                                    "FROM Clients c JOIN FacturesVente f ON f.ClientId=c.ClientId " &
                                    "WHERE f.Statut='PAYEE' AND f.CreeLe >= DATEADD(DAY,-30,GETDATE()) " &
                                    "GROUP BY c.ClientId, c.NomClient ORDER BY TotalAchats DESC"
                Dim dt As DataTable = dal.ExecuterTable(sql, CommandType.Text, Nothing)
                Dim row As DataRow = dt.NewRow
                row("ClientId") = DBNull.Value
                row("NomClient") = ""
                dt.Rows.InsertAt(row, 0)

                cmbSortieManuelleClient.DataSource = dt
                cmbSortieManuelleClient.DisplayMember = "NomClient"
                cmbSortieManuelleClient.ValueMember = "ClientId"

                cmbSortieManuelleClient.SelectedIndex = 0
                ChargerInfoAchatClientSelection(Nothing, EventArgs.Empty)


            Catch ex As Exception
                MessageBox.Show("Erreur clients actifs: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerMotifsSortie()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                AssurerMotifTransfertMarchandises(dal)
                Dim dt As DataTable = dal.ExecuterTable("SELECT MotifId, Libelle FROM MotifSortie WHERE EstActif = 1 ORDER BY Libelle", CommandType.Text, Nothing)
                For Each cmb As ComboBox In New ComboBox() {cmbMotif, cmbSortieManuelleMotif}
                    cmb.DataSource = Nothing
                    cmb.DisplayMember = "Libelle"
                    cmb.ValueMember = "MotifId"
                    cmb.DataSource = dt.Copy()
                Next
                MettreAJourVisibiliteSortieManuelle(Nothing, EventArgs.Empty)
            Catch ex As Exception
                MessageBox.Show("Erreur chargement motifs: " & ex.Message)
            End Try
        End Sub

        Private Sub AssurerMotifTransfertMarchandises(dal As DAL)
            Dim sql As String =
                "IF NOT EXISTS (SELECT 1 FROM MotifSortie WHERE Libelle = @Libelle) " &
                "BEGIN " &
                "INSERT INTO MotifSortie (Libelle, EstActif) VALUES (@Libelle, 1) " &
                "END"
            Dim params As New List(Of System.Data.SqlClient.SqlParameter) From {
                New System.Data.SqlClient.SqlParameter("@Libelle", "Transfert marchandises")
            }
            dal.ExecuterNonRequete(sql, CommandType.Text, params)
        End Sub

        Private Sub AssurerTableMagasins()
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim sql As String =
                "IF OBJECT_ID('dbo.Magasins', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.Magasins (" &
                "MagasinId INT IDENTITY(1,1) PRIMARY KEY, " &
                "NomMagasin NVARCHAR(100) NOT NULL, " &
                "Adresse NVARCHAR(200) NULL, " &
                "EstActif BIT NOT NULL CONSTRAINT DF_Magasins_EstActif DEFAULT(1), " &
                "CreeLe DATETIME NOT NULL CONSTRAINT DF_Magasins_CreeLe DEFAULT(GETDATE())) " &
                "END"
            dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub

        Private Sub ChargerMagasins()
            Try
                AssurerTableMagasins()
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim dt As DataTable = dal.ExecuterTable("SELECT MagasinId, NomMagasin FROM Magasins WHERE EstActif = 1 ORDER BY NomMagasin", CommandType.Text, Nothing)
                Dim ligneVide As DataRow = dt.NewRow()
                ligneVide("MagasinId") = DBNull.Value
                ligneVide("NomMagasin") = ""
                dt.Rows.InsertAt(ligneVide, 0)

                cmbMagasinDestination.DataSource = dt
                cmbMagasinDestination.DisplayMember = "NomMagasin"
                cmbMagasinDestination.ValueMember = "MagasinId"
                cmbMagasinDestination.SelectedIndex = 0
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("Stock", "ChargerMagasins", "Impossible de charger les magasins de destination.", ex)
            End Try
        End Sub

        Private Sub ChargerInfoAchatClientSelection(sender As Object, e As EventArgs)
            If Not lblSortieManuelleClient.Visible OrElse Not cmbSortieManuelleClient.Visible Then
                lblQteAchter.Visible = False
                lblSMontantAchat.Visible = False
                lblSMoyenneAchat.Visible = False
                lblQte.Visible = False
                lblMont.Visible = False
                lblMoyenne.Visible = False
                Return
            End If

            If String.IsNullOrWhiteSpace(cmbSortieManuelleClient.Text) Or cmbSortieManuelleClient.SelectedIndex = 0 Then
                lblQteAchter.Visible = False
                lblSMontantAchat.Visible = False
                lblSMoyenneAchat.Visible = False
                lblQte.Enabled = False
                lblQte.Visible = False
                lblMont.Enabled = False
                lblMont.Visible = False
                lblMoyenne.Enabled = False
                lblMoyenne.Visible = False
            ElseIf cmbSortieManuelleClient.SelectedItem IsNot Nothing Then
                lblQte.Visible = True
                lblMont.Visible = True
                lblMoyenne.Visible = True
                lblQteAchter.Visible = True
                lblSMontantAchat.Visible = True
                lblSMoyenneAchat.Visible = True
                Dim row As DataRowView = TryCast(cmbSortieManuelleClient.SelectedItem, DataRowView)
                If row Is Nothing Then Return
                Dim r As DataRow = row.Row
                lblQteAchter.Text = If(r.IsNull("NbAchats"), "", Convert.ToString(row("NbAchats")))
                lblSMontantAchat.Text = If(r.IsNull("TotalAchats"), "", Convert.ToString(row("TotalAchats")) & "FC")
                lblSMoyenneAchat.Text = If(r.IsNull("MoyenneAchat"), "", Convert.ToString(row("MoyenneAchat")) & "FC")
            End If

        End Sub

        Private Sub MettreAJourVisibiliteSortieManuelle(sender As Object, e As EventArgs)
            Dim motif As String = String.Empty
            Dim motifView As DataRowView = TryCast(cmbMotif.SelectedItem, DataRowView)
            If motifView IsNot Nothing Then
                motif = Convert.ToString(motifView("Libelle"))
            ElseIf cmbMotif IsNot Nothing Then
                motif = cmbMotif.Text
            End If

            Dim clientObligatoire As Boolean = String.Equals(motif, "Dette Client", StringComparison.OrdinalIgnoreCase)
            Dim transfert As Boolean = String.Equals(motif, "Transfert marchandises", StringComparison.OrdinalIgnoreCase)

            lblSortieManuelleClient.Visible = clientObligatoire
            cmbSortieManuelleClient.Visible = clientObligatoire
            If Not clientObligatoire Then
                cmbSortieManuelleClient.SelectedIndex = If(cmbSortieManuelleClient.Items.Count > 0, 0, -1)
            End If

            lblMagasinDestination.Visible = transfert
            cmbMagasinDestination.Visible = transfert
            btnAjouterMagasin.Visible = transfert
            If Not transfert AndAlso cmbMagasinDestination IsNot Nothing Then
                cmbMagasinDestination.SelectedIndex = If(cmbMagasinDestination.Items.Count > 0, 0, -1)
            End If

            ChargerInfoAchatClientSelection(Nothing, EventArgs.Empty)
        End Sub

        Private Sub AjouterMagasinDestination(sender As Object, e As EventArgs)
            Using formulaire As New FormulaireMagasinRapide()
                If formulaire.ShowDialog(Me) <> DialogResult.OK Then
                    Return
                End If

                Dim nomMagasin As String = formulaire.NomMagasin
                If String.IsNullOrWhiteSpace(nomMagasin) Then
                    Return
                End If

                Try
                    AssurerTableMagasins()
                    Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                    Dim dal As New DAL(cs)
                    Dim sql As String =
                        "IF NOT EXISTS (SELECT 1 FROM Magasins WHERE NomMagasin = @NomMagasin) " &
                        "BEGIN " &
                        "INSERT INTO Magasins (NomMagasin, Adresse, EstActif) VALUES (@NomMagasin, @Adresse, 1) " &
                        "END"
                    Dim params As New List(Of System.Data.SqlClient.SqlParameter) From {
                        New System.Data.SqlClient.SqlParameter("@NomMagasin", nomMagasin.Trim()),
                        New System.Data.SqlClient.SqlParameter("@Adresse", If(String.IsNullOrWhiteSpace(formulaire.AdresseMagasin), CType(DBNull.Value, Object), formulaire.AdresseMagasin.Trim()))
                    }
                    dal.ExecuterNonRequete(sql, CommandType.Text, params)
                    ChargerMagasins()
                    cmbMagasinDestination.Text = nomMagasin.Trim()
                Catch ex As Exception
                    Dim log As New ProductionLogService()
                    log.Error("Stock", "AjouterMagasinDestination", "Impossible d'ajouter le magasin de destination.", ex)
                    MessageBox.Show("Impossible d'ajouter le magasin de destination : " & ex.Message)
                End Try
            End Using
        End Sub

        Private Sub FiltrerProduitsExistants(sender As Object, e As EventArgs)
            If _produitsTable Is Nothing OrElse _isFilteringProduits Then
                Return
            End If

            _isFilteringProduits = True
            BeginInvoke(New MethodInvoker(
                Sub()
                    _isFilteringProduits = False
                End Sub))
        End Sub

        Private Sub SelectionnerProduitExistant(sender As Object, e As EventArgs)
            If _produitsTable Is Nothing Then
                Return
            End If

            If _isFilteringProduits Then
                Return
            End If

            Dim texte As String = cmbProduitExistant.Text.Trim()
            If texte = String.Empty Then
                Return
            End If

            For Each row As DataRow In _produitsTable.Rows
                If String.Equals(Convert.ToString(row("Libelle")).Trim(), texte, StringComparison.CurrentCultureIgnoreCase) Then
                    Dim produitId As Integer = Convert.ToInt32(row("ProduitId"))
                    Dim valeurSelectionnee As Integer = If(cmbProduitExistant.SelectedValue Is Nothing OrElse IsDBNull(cmbProduitExistant.SelectedValue), 0, Convert.ToInt32(cmbProduitExistant.SelectedValue))
                    If valeurSelectionnee <> produitId Then
                        cmbProduitExistant.SelectedValue = produitId
                    End If
                    Exit For
                End If
            Next
        End Sub

        Private Sub ChargerProduits()
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New ProduitRepository(dal)
            _produitsTable = repo.ListerTable()
            _produitsAutoCompleteSource = New AutoCompleteStringCollection()
            For Each row As DataRow In _produitsTable.Rows
                Dim libelle As String = Convert.ToString(row("Libelle")).Trim()
                If libelle <> String.Empty Then
                    _produitsAutoCompleteSource.Add(libelle)
                End If
            Next

            cmbProduitExistant.AutoCompleteCustomSource = _produitsAutoCompleteSource
            cmbProduitExistant.DataSource = _produitsTable
            cmbProduitExistant.DisplayMember = "Libelle"
            cmbProduitExistant.ValueMember = "ProduitId"

            cmbProduitSortie.DataSource = _produitsTable.Copy
            cmbProduitSortie.DisplayMember = "Libelle"
            cmbProduitSortie.ValueMember = "ProduitId"

            cmbProduitInventaire.DataSource = _produitsTable.Copy()
            cmbProduitInventaire.DisplayMember = "Libelle"
            cmbProduitInventaire.ValueMember = "ProduitId"

            cmbProduitPerte.DataSource = _produitsTable.Copy()
            cmbProduitPerte.DisplayMember = "Libelle"
            cmbProduitPerte.ValueMember = "ProduitId"

            If _produitsTable.Rows.Count > 0 Then
                ChargerSortiesDuMois(Nothing, EventArgs.Empty)
                ChargerRapportEntrees(Nothing, EventArgs.Empty)
            End If
        End Sub


        Private Sub ChargerParametres()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim paramService As New ParametreService(New ParametreRepository(dal))
                Dim p As ParametreDTO = paramService.Charger()
                _parametres = p
                If p IsNot Nothing Then
                    txtTaux.Text = p.TauxUsd.ToString()
                End If
                MettreAJourEquivalentPrixAchat()
            Catch
            End Try
        End Sub

        Private Sub DeviseOuPrixAchatChange(sender As Object, e As EventArgs)
            MettreAJourEquivalentPrixAchat()
            RecalculerPrixAuto(sender, e)
        End Sub

        Private Sub MettreAJourEquivalentPrixAchat()
            Dim prixSaisi As Decimal = LireDecimal(txtPrixAchat.Text)
            Dim prixCdf As Decimal = CalculerPrixAchatEnCdf(prixSaisi)
            If prixSaisi <= 0D Then
                lblEquivalentCdf.Text = String.Empty
                Return
            End If

            If String.Equals(Convert.ToString(cmbDevise.SelectedItem), "USD", StringComparison.OrdinalIgnoreCase) Then
                lblEquivalentCdf.Text = "Équivalent CDF : " & prixCdf.ToString("N0") & " FC"
            Else
                lblEquivalentCdf.Text = "Montant CDF : " & prixCdf.ToString("N0") & " FC"
            End If
        End Sub

        Private Function CalculerPrixAchatEnCdf(prixSaisi As Decimal) As Decimal
            If prixSaisi <= 0D Then
                Return 0D
            End If

            If String.Equals(Convert.ToString(cmbDevise.SelectedItem), "USD", StringComparison.OrdinalIgnoreCase) Then
                Dim taux As Decimal = LireDecimal(txtTaux.Text)
                If taux <= 0D AndAlso _parametres IsNot Nothing Then
                    taux = _parametres.TauxUsd
                End If
                If taux > 0D Then
                    Return Math.Round(prixSaisi * taux, 2)
                End If
            End If

            Return prixSaisi
        End Function

        Private Function LirePrixAchatEntreeEnCdf() As Decimal
            Return CalculerPrixAchatEnCdf(LireDecimal(txtPrixAchat.Text))
        End Function

        Private Sub BasculerProduitExistant(sender As Object, e As EventArgs)
            Dim existant As Boolean = chkProduitExistant.Checked
            cmbProduitExistant.Enabled = existant
            txtNomProduit.Enabled = Not existant
            cmbCategorie.Enabled = Not existant

            If existant Then
                ReinitialiserOverridesPrixVente()
                ChargerProduitSelection(Nothing, EventArgs.Empty)
            Else
                txtNomProduit.Text = ""
                cmbCategorie.Text = ""
                txtReference.Text = ""
                _stockActuelEntreeBase = 0D
                lblStockActuel.Text = "Stock actuel: 0"
                lblStockActuelPiece.Text = "Equivalent: 0 pièce"
                lblStockApres.Text = "Stock après: 0"
                lblStockApresPiece.Text = "Après: 0 pièce"
                txtNbUniteParBase.Clear()
                txtQuantiteSecondaireEntree.Clear()
                txtCoefficientDetail.Clear()
                txtCoefficientInput.Clear()
                txtPrixAchat.Clear()
                txtPrixDemi.Clear()
                txtPrixDouzaine.Clear()
                txtPrixGros.Clear()
                txtPrixPiece.Clear()
                txtPrixQuart.Clear()
                txtObservationEntree.Clear()
                lblEquivalentCdf.Text = String.Empty
                ReinitialiserOverridesPrixVente()
                cmbDevise.SelectedItem = "CDF"
                chkDemi.Checked = False
                chkDouzaine.Checked = False
                chkQuart.Checked = False
                chkGros.Checked = True
                chkPiece.Checked = True
            End If
            RafraichirTypesVente()
            RafraichirResumeTypesPersonnalisesEntree()
        End Sub

        Private Sub GenererReferenceAutomatique(sender As Object, e As EventArgs)
            If chkProduitExistant.Checked Then Return
            Dim nom As String = txtNomProduit.Text.Trim()
            If nom = "" Then
                txtReference.Text = ""
                Return
            End If
            txtReference.Text = GenererReferenceUnique(nom, cmbCategorie.Text.Trim())
        End Sub

        Private Sub VerifierDoublonNomProduit(sender As Object, e As EventArgs)
            ProduitNouveauExisteDeja(True)
        End Sub

        Private Function ProduitNouveauExisteDeja(afficherMessage As Boolean) As Boolean
            If chkProduitExistant.Checked Then Return False

            Dim nom As String = txtNomProduit.Text.Trim()
            If nom = String.Empty Then Return False

            Dim service As ProduitService = ObtenirService()
            If Not service.ExisteParLibelle(nom) Then Return False

            If afficherMessage Then
                MessageBox.Show(
                    "Un produit portant ce nom existe déjà." & Environment.NewLine &
                    "Sélectionnez-le dans « Produit existant » ou saisissez un autre nom.",
                    "Produit existant",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
                txtNomProduit.Focus()
            End If

            Return True
        End Function

        Private Sub ChargerProduitSelection(sender As Object, e As EventArgs)
            If _isFilteringProduits Then
                Return
            End If
            If _isSavingEntree Then
                Return
            End If
            If cmbProduitExistant.SelectedValue Is Nothing Then Return
            Dim row As DataRowView = TryCast(cmbProduitExistant.SelectedItem, DataRowView)
            If row Is Nothing Then Return
            Dim r As DataRow = row.Row
            _stockActuelEntreeBase = LireDecimalTable(r, "QuantiteStock")
            txtNomProduit.Text = Convert.ToString(row("Libelle"))
            If Not r.IsNull("CategorieId") AndAlso cmbCategorie.DataSource IsNot Nothing Then
                cmbCategorie.SelectedValue = Convert.ToInt32(row("CategorieId"))
            Else
                cmbCategorie.SelectedIndex = -1
            End If
            txtReference.Text = GenererReference(Convert.ToString(row("Libelle")), cmbCategorie.Text)

            cmbUniteBase.Text = If(r.IsNull("UnitePrincipale"), "", Convert.ToString(row("UnitePrincipale")))
            txtNbUniteParBase.Text = If(r.IsNull("ConversionUnite"), "", Convert.ToDecimal(row("ConversionUnite")).ToString())
            Dim typeGestion As String = LireTexteCellule(r, "TypeGestionStock").ToUpperInvariant()
            If typeGestion = "MESURE" OrElse typeGestion = "POIDS" OrElse typeGestion = "VOLUME" Then
                cmbTypeGestionStockEntree.SelectedItem = "MESURE"
            Else
                cmbTypeGestionStockEntree.SelectedItem = "UNITE"
            End If
            Dim uniteMesure As String = LireTexteCellule(r, "UniteMesureStock").ToUpperInvariant()
            If String.IsNullOrWhiteSpace(uniteMesure) Then uniteMesure = "KG"
            cmbUniteMesureStockEntree.Text = uniteMesure
            txtContenuUnitePrincipaleEntree.Text = If(r.Table.Columns.Contains("ContenuUnitePrincipale") AndAlso Not r.IsNull("ContenuUnitePrincipale"), Convert.ToDecimal(r("ContenuUnitePrincipale")).ToString(), txtNbUniteParBase.Text)
            txtContenuUniteSecondaireEntree.Text = If(r.Table.Columns.Contains("ContenuUniteSecondaire") AndAlso Not r.IsNull("ContenuUniteSecondaire"), Convert.ToDecimal(r("ContenuUniteSecondaire")).ToString(), String.Empty)
            txtPrixAchat.Text = If(r.IsNull("PrixAchat"), "", Convert.ToDecimal(row("PrixAchat")).ToString())
            cmbDevise.SelectedItem = "CDF"
            txtCoefficientInput.Text = If(r.IsNull("CoefficientGros"), "", Convert.ToDecimal(row("CoefficientGros")).ToString("N4"))

            Dim prixAchatVal As Decimal = LireDecimal(txtPrixAchat.Text)
            Dim prixDetailVal As Decimal = If(r.IsNull("PrixDetail"), 0D, Convert.ToDecimal(row("PrixDetail")))
            Dim conversionVal As Decimal = LireDecimal(txtNbUniteParBase.Text)

            If prixAchatVal > 0D AndAlso conversionVal > 0D AndAlso prixDetailVal > 0D Then
                txtCoefficientDetail.Text = Math.Round((prixDetailVal * conversionVal) / prixAchatVal, 4).ToString("N4")
            Else
                txtCoefficientDetail.Text = ""
            End If

            chkGros.Checked = Convert.ToBoolean(row("VenteGros"))
            chkDemi.Checked = Convert.ToBoolean(row("VenteDemi"))
            chkQuart.Checked = (If(r.IsNull("PrixQuart"), 0D, Convert.ToDecimal(row("PrixQuart"))) > 0D)
            chkPiece.Checked = Convert.ToBoolean(row("VenteDetail"))
            chkDouzaine.Checked = Convert.ToBoolean(row("VenteDouzaine"))

            AfficherStockActuel()
            ModeGestionStockEntreeChange(Nothing, EventArgs.Empty)
            ReinitialiserOverridesPrixVente()
            MettreAJourEquivalentPrixAchat()
            RecalculerStock(Nothing, EventArgs.Empty)
            RecalculerPrixAuto(Nothing, EventArgs.Empty)
            RafraichirTypesVente()
            RafraichirResumeTypesPersonnalisesEntree()
        End Sub

        Private Sub AfficherStockActuel()
            If cmbProduitExistant.SelectedValue IsNot Nothing AndAlso Not TypeOf cmbProduitExistant.SelectedValue Is DataRowView Then
                Dim produitId As Integer = Convert.ToInt32(cmbProduitExistant.SelectedValue)
                Dim service As StockService = ObtenirStockService()
                Dim stockPieces As Decimal = service.ObtenirStockActuelProduit(produitId)
                _stockActuelEntreeBase = stockPieces
                Dim nb As Decimal = LireDecimal(txtNbUniteParBase.Text)
                Dim uniteBase As String = If(cmbUniteBase.Text.Trim() = "", "base", cmbUniteBase.Text.Trim())
                Dim uniteSecondaire As String = ObtenirUniteSecondaireEntree()
                If EstGestionMesureEntree() Then
                    lblStockActuel.Text = "Stock actuel: " & FormatageGlobal.FormatQuantitePhysique(stockPieces) & " " & uniteSecondaire
                    lblStockActuelPiece.Text = "Equivalent: " & FormaterStockLisible(stockPieces, nb, uniteBase, uniteSecondaire)
                Else
                    lblStockActuel.Text = "Stock actuel: " & FormaterStockLisible(stockPieces, nb, uniteBase, uniteSecondaire)
                    lblStockActuelPiece.Text = "Equivalent: " & stockPieces.ToString("N0") & " " & uniteSecondaire
                End If
            End If
        End Sub

        Private Function ObtenirUniteSecondaireEntree() As String
            If EstGestionMesureEntree() Then
                Return UniteMesureStockEntree()
            End If

            Dim produitId As Integer = ObtenirProduitEntreeSelectionneId()
            Dim row As DataRow = If(produitId > 0, ObtenirProduitCourantDepuisListe(produitId), Nothing)
            If row IsNot Nothing AndAlso row.Table.Columns.Contains("UniteSecondaire") AndAlso Not row.IsNull("UniteSecondaire") Then
                Dim uniteSecondaire As String = Convert.ToString(row("UniteSecondaire")).Trim()
                If uniteSecondaire <> String.Empty Then
                    Return uniteSecondaire
                End If
            End If

            Return "pièces"
        End Function

        Private Function TypeGestionStockEntree() As String
            Dim valeur As String = If(cmbTypeGestionStockEntree Is Nothing, "", Convert.ToString(cmbTypeGestionStockEntree.SelectedItem)).Trim().ToUpperInvariant()
            If valeur = "MESURE" Then Return "MESURE"
            Return "UNITE"
        End Function

        Private Function EstGestionMesureEntree() As Boolean
            Return String.Equals(TypeGestionStockEntree(), "MESURE", StringComparison.OrdinalIgnoreCase)
        End Function

        Private Function UniteMesureStockEntree() As String
            Dim unite As String = If(cmbUniteMesureStockEntree Is Nothing, "", Convert.ToString(cmbUniteMesureStockEntree.Text)).Trim().ToUpperInvariant()
            If String.IsNullOrWhiteSpace(unite) Then Return "KG"
            Return unite
        End Function

        Private Function LireContenuUnitePrincipaleEntree() As Decimal
            If EstGestionMesureEntree() Then
                Dim contenu As Decimal = LireDecimal(txtContenuUnitePrincipaleEntree.Text)
                If contenu > 0D Then Return contenu
            End If

            Dim conversion As Decimal = LireDecimal(txtNbUniteParBase.Text)
            If conversion > 0D Then Return conversion
            Return 1D
        End Function

        Private Function LireContenuUniteSecondaireEntree() As Decimal?
            If Not EstGestionMesureEntree() Then Return Nothing
            Dim contenu As Decimal = LireDecimal(txtContenuUniteSecondaireEntree.Text)
            If contenu > 0D Then Return contenu
            Return Nothing
        End Function

        Private Sub ModeGestionStockEntreeChange(sender As Object, e As EventArgs)
            Dim mesure As Boolean = EstGestionMesureEntree()
            cmbUniteMesureStockEntree.Enabled = mesure
            txtContenuUnitePrincipaleEntree.Enabled = mesure
            txtContenuUniteSecondaireEntree.Enabled = mesure
            If Not mesure Then
                txtContenuUnitePrincipaleEntree.Text = txtNbUniteParBase.Text
                txtContenuUniteSecondaireEntree.Clear()
            End If
            RecalculerStock(Nothing, EventArgs.Empty)
            RafraichirTypesVente()
        End Sub

        Private Function ObtenirService() As ProduitService
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New ProduitRepository(dal)
            Return New ProduitService(repo)
        End Function

        Private Function ObtenirProduitEntreeSelectionneId() As Integer
            If chkProduitExistant.Checked AndAlso cmbProduitExistant.SelectedValue IsNot Nothing AndAlso Not TypeOf cmbProduitExistant.SelectedValue Is DataRowView AndAlso Not IsDBNull(cmbProduitExistant.SelectedValue) Then
                Return Convert.ToInt32(cmbProduitExistant.SelectedValue)
            End If

            Return 0
        End Function

        Private Sub OuvrirTypesPersonnalisesEntree(sender As Object, e As EventArgs)
            Dim produitId As Integer = ObtenirProduitEntreeSelectionneId()
            If produitId <= 0 Then
                MessageBox.Show("Sélectionnez d'abord un produit existant pour gérer ses types personnalisés.")
                Return
            End If

            Dim typesCourants As List(Of TypeVenteProduitDTO) = ConstruireTypesPersonnalisesFusionnesPourProduit(produitId)
            Using frm As New FormulaireTypesVenteProduit(produitId, LirePrixAchatEntreeEnCdf(), LireDecimal(txtNbUniteParBase.Text), False, typesCourants, Nothing, cmbUniteBase.Text, ObtenirUniteSecondaireProduitSelectionne(), If(EstGestionMesureEntree(), UniteMesureStockEntree(), Nothing))
                If frm.ShowDialog(Me) = DialogResult.OK AndAlso frm.TypeVenteResultat IsNot Nothing Then
                    Dim typeResultat As TypeVenteProduitDTO = ClonerTypePersonnalise(frm.TypeVenteResultat)
                    If typeResultat.TypeVenteProduitId = 0 Then
                        typeResultat.TypeVenteProduitId = GenererTypeTemporaireId()
                    End If

                    AjouterOuRemplacerTypePersonnaliseTemporaire(produitId, typeResultat)
                End If
            End Using

            RafraichirTypesVente()
            RafraichirResumeTypesPersonnalisesEntree()
        End Sub

        Private Function GenererTypeTemporaireId() As Integer
            Dim id As Integer = _prochainTypeTemporaireId
            _prochainTypeTemporaireId -= 1
            Return id
        End Function

        Private Function ClonerTypePersonnalise(source As TypeVenteProduitDTO) As TypeVenteProduitDTO
            If source Is Nothing Then
                Return Nothing
            End If

            Return New TypeVenteProduitDTO With {
                .TypeVenteProduitId = source.TypeVenteProduitId,
                .ProduitId = source.ProduitId,
                .Nom = source.Nom,
                .QuantiteEquivalent = source.QuantiteEquivalent,
                .TypeUniteEquivalent = source.TypeUniteEquivalent,
                .TypeQuantiteEquivalent = source.TypeQuantiteEquivalent,
                .ModePrix = source.ModePrix,
                .Coefficient = source.Coefficient,
                .PrixVente = source.PrixVente,
                .Actif = source.Actif,
                .CreeLe = source.CreeLe,
                .ModifieLe = source.ModifieLe,
                .ModifiePar = source.ModifiePar
            }
        End Function

        Private Function ObtenirTypesPersonnalisesTemporaires(produitId As Integer) As List(Of TypeVenteProduitDTO)
            Dim liste As List(Of TypeVenteProduitDTO) = Nothing
            If Not _typesPersonnalisesTemporairesParProduit.TryGetValue(produitId, liste) Then
                liste = New List(Of TypeVenteProduitDTO)()
                _typesPersonnalisesTemporairesParProduit(produitId) = liste
            End If

            Return liste
        End Function

        Private Sub AjouterOuRemplacerTypePersonnaliseTemporaire(produitId As Integer, dto As TypeVenteProduitDTO)
            If dto Is Nothing Then
                Return
            End If

            Dim temporaires As List(Of TypeVenteProduitDTO) = ObtenirTypesPersonnalisesTemporaires(produitId)
            Dim index As Integer = temporaires.FindIndex(Function(x) x.TypeVenteProduitId = dto.TypeVenteProduitId)
            If index >= 0 Then
                temporaires(index) = ClonerTypePersonnalise(dto)
            Else
                temporaires.Add(ClonerTypePersonnalise(dto))
            End If
        End Sub

        Private Function ConstruireTypesPersonnalisesFusionnesPourProduit(produitId As Integer) As List(Of TypeVenteProduitDTO)
            Dim service As New TypeVenteProduitService()
            Dim resultat As List(Of TypeVenteProduitDTO) = service.ListerParProduit(produitId, False).Select(Function(x) ClonerTypePersonnalise(x)).ToList()

            Dim temporaires As List(Of TypeVenteProduitDTO) = Nothing
            If _typesPersonnalisesTemporairesParProduit.TryGetValue(produitId, temporaires) Then
                For Each item As TypeVenteProduitDTO In temporaires
                    Dim index As Integer = resultat.FindIndex(Function(x) x.TypeVenteProduitId = item.TypeVenteProduitId)
                    If index >= 0 Then
                        resultat(index) = ClonerTypePersonnalise(item)
                    Else
                        resultat.Add(ClonerTypePersonnalise(item))
                    End If
                Next
            End If

            Return resultat.OrderByDescending(Function(x) x.Actif).ThenBy(Function(x) x.Nom).ToList()
        End Function

        Private Function ConstruireTypesPersonnalisesActifsPourProduit(produitId As Integer) As List(Of TypeVenteProduitDTO)
            Return ConstruireTypesPersonnalisesFusionnesPourProduit(produitId).Where(Function(x) x.Actif).ToList()
        End Function

        Private Function TrouverTypePersonnalisePourProduit(produitId As Integer, typeVenteProduitId As Integer) As TypeVenteProduitDTO
            Return ConstruireTypesPersonnalisesFusionnesPourProduit(produitId).FirstOrDefault(Function(x) x.TypeVenteProduitId = typeVenteProduitId)
        End Function

        Private Sub ModifierTypePersonnaliseTemporaire(sender As Object, e As EventArgs)
            Dim bouton As Button = TryCast(sender, Button)
            If bouton Is Nothing OrElse bouton.Tag Is Nothing Then
                Return
            End If

            Dim produitId As Integer = ObtenirProduitEntreeSelectionneId()
            If produitId <= 0 Then
                Return
            End If

            Dim typeId As Integer = Convert.ToInt32(bouton.Tag)
            Dim typeCourant As TypeVenteProduitDTO = TrouverTypePersonnalisePourProduit(produitId, typeId)
            If typeCourant Is Nothing Then
                Return
            End If

            Dim typesCourants As List(Of TypeVenteProduitDTO) = ConstruireTypesPersonnalisesFusionnesPourProduit(produitId)
            Using frm As New FormulaireTypesVenteProduit(produitId, LirePrixAchatEntreeEnCdf(), LireDecimal(txtNbUniteParBase.Text), False, typesCourants, typeCourant, cmbUniteBase.Text, ObtenirUniteSecondaireProduitSelectionne(), If(EstGestionMesureEntree(), UniteMesureStockEntree(), Nothing))
                If frm.ShowDialog(Me) = DialogResult.OK AndAlso frm.TypeVenteResultat IsNot Nothing Then
                    AjouterOuRemplacerTypePersonnaliseTemporaire(produitId, frm.TypeVenteResultat)
                End If
            End Using

            RafraichirTypesVente()
            RafraichirResumeTypesPersonnalisesEntree()
        End Sub

        Private Sub BasculerTypePersonnaliseTemporaire(sender As Object, e As EventArgs)
            Dim bouton As Button = TryCast(sender, Button)
            If bouton Is Nothing OrElse bouton.Tag Is Nothing Then
                Return
            End If

            Dim produitId As Integer = ObtenirProduitEntreeSelectionneId()
            If produitId <= 0 Then
                Return
            End If

            Dim typeId As Integer = Convert.ToInt32(bouton.Tag)
            Dim typeCourant As TypeVenteProduitDTO = TrouverTypePersonnalisePourProduit(produitId, typeId)
            If typeCourant Is Nothing Then
                Return
            End If

            Dim copie As TypeVenteProduitDTO = ClonerTypePersonnalise(typeCourant)
            copie.Actif = Not copie.Actif
            AjouterOuRemplacerTypePersonnaliseTemporaire(produitId, copie)
            RafraichirTypesVente()
            RafraichirResumeTypesPersonnalisesEntree()
        End Sub

        Private Sub RafraichirResumeTypesPersonnalisesEntree()
            pnlTypesPersonnalisesEntree.Controls.Clear()

            Dim produitId As Integer = ObtenirProduitEntreeSelectionneId()
            If produitId <= 0 Then
                Dim lblVide As New Label() With {.Text = "Sélectionnez un produit existant.", .AutoSize = True, .ForeColor = Color.Gray, .Margin = New Padding(6)}
                pnlTypesPersonnalisesEntree.Controls.Add(lblVide)
                Return
            End If

            Dim types As List(Of TypeVenteProduitDTO) = ConstruireTypesPersonnalisesFusionnesPourProduit(produitId)
            If types.Count = 0 Then
                Dim lblVide As New Label() With {.Text = "Aucun type personnalisé.", .AutoSize = True, .ForeColor = Color.Gray, .Margin = New Padding(6)}
                pnlTypesPersonnalisesEntree.Controls.Add(lblVide)
                Return
            End If

            For Each item As TypeVenteProduitDTO In types
                Dim ligne As New Panel() With {.Width = Math.Max(220, pnlTypesPersonnalisesEntree.ClientSize.Width - 24), .Height = 30, .Margin = New Padding(4), .BackColor = Color.FromArgb(248, 249, 250)}
                Dim lbl As New Label() With {
                    .Left = 6,
                    .Top = 7,
                    .Width = Math.Max(90, ligne.Width - 112),
                    .AutoEllipsis = True,
                    .Text = item.NomAffichage & " — " & item.PrixVente.ToString("N0") & " FC" & If(item.Actif, String.Empty, " (Inactif)")
                }
                Dim btnModifier As New Button() With {
                    .Text = "Modif.",
                    .Width = 48,
                    .Height = 22,
                    .Left = ligne.Width - 100,
                    .Top = 4,
                    .Tag = item.TypeVenteProduitId
                }
                Dim btnToggle As New Button() With {
                    .Text = If(item.Actif, "Off", "On"),
                    .Width = 42,
                    .Height = 22,
                    .Left = ligne.Width - 48,
                    .Top = 4,
                    .Tag = item.TypeVenteProduitId
                }
                AddHandler btnModifier.Click, AddressOf ModifierTypePersonnaliseTemporaire
                AddHandler btnToggle.Click, AddressOf BasculerTypePersonnaliseTemporaire
                ligne.Controls.Add(lbl)
                ligne.Controls.Add(btnModifier)
                ligne.Controls.Add(btnToggle)
                pnlTypesPersonnalisesEntree.Controls.Add(ligne)
            Next
        End Sub

        Private Sub EnregistrerTypesPersonnalisesTemporaires(produitId As Integer)
            Dim temporaires As List(Of TypeVenteProduitDTO) = Nothing
            If produitId <= 0 OrElse Not _typesPersonnalisesTemporairesParProduit.TryGetValue(produitId, temporaires) OrElse temporaires.Count = 0 Then
                Return
            End If

            Dim service As New TypeVenteProduitService()
            For Each item As TypeVenteProduitDTO In temporaires
                Dim copie As TypeVenteProduitDTO = ClonerTypePersonnalise(item)
                copie.ProduitId = produitId
                If copie.TypeVenteProduitId > 0 Then
                    service.MettreAJour(copie)
                Else
                    service.Ajouter(copie)
                End If
            Next

            _typesPersonnalisesTemporairesParProduit.Remove(produitId)
        End Sub

        Private Sub NettoyerSaisieEntreeApresEnregistrement(produitId As Integer, produitExistant As Boolean)
            txtQuantiteEntree.Clear()
            txtQuantiteSecondaireEntree.Clear()
            txtObservationEntree.Clear()
            txtCoefficientInput.Clear()
            txtCoefficientDetail.Clear()
            txtReference.Clear()
            lblEquivalentCdf.Text = String.Empty
            _coefficientCalcule = 0D
            _coefficientDetailCalcule = 0D
            lblMargeCalculee.Text = String.Empty
            lblMargeDetailCalculee.Text = String.Empty
            lblTypeCoefficient.Text = String.Empty
            ReinitialiserOverridesPrixVente()

            If produitId > 0 Then
                _typesPersonnalisesTemporairesParProduit.Remove(produitId)
            End If

            If produitExistant AndAlso produitId > 0 Then
                chkProduitExistant.Checked = True
                cmbProduitExistant.SelectedValue = produitId
                ChargerProduitSelection(Nothing, EventArgs.Empty)
            Else
                txtNomProduit.Clear()
                cmbCategorie.Text = String.Empty
                cmbUniteBase.SelectedIndex = -1
                txtNbUniteParBase.Clear()
                txtPrixAchat.Clear()
                cmbDevise.SelectedItem = "CDF"
                txtPrixGros.Clear()
                txtPrixDemi.Clear()
                txtPrixQuart.Clear()
                txtPrixPiece.Clear()
                txtPrixDouzaine.Clear()
                chkGros.Checked = True
                chkDemi.Checked = False
                chkQuart.Checked = False
                chkPiece.Checked = True
                chkDouzaine.Checked = False
                RafraichirTypesVente()
            End If

            RafraichirResumeTypesPersonnalisesEntree()
            gridTypesVente.DataSource = Nothing
            RafraichirTypesVente()
            txtQuantiteEntree.Focus()
        End Sub

        Private Function ObtenirProduitCourantDepuisListe(produitId As Integer) As DataRow
            If _produitsTable Is Nothing Then
                Return Nothing
            End If

            For Each row As DataRow In _produitsTable.Rows
                If row IsNot Nothing AndAlso Not row.IsNull("ProduitId") AndAlso Convert.ToInt32(row("ProduitId")) = produitId Then
                    Return row
                End If
            Next

            Return Nothing
        End Function

        Private Function ObtenirCategorieSelectionneId() As Integer?
            If cmbCategorie.SelectedValue Is Nothing OrElse IsDBNull(cmbCategorie.SelectedValue) Then
                Return Nothing
            End If

            Return Convert.ToInt32(cmbCategorie.SelectedValue)
        End Function

        Private Function LireTexteCellule(row As DataRow, colonne As String) As String
            If row Is Nothing OrElse row.Table Is Nothing OrElse Not row.Table.Columns.Contains(colonne) OrElse row.IsNull(colonne) Then
                Return String.Empty
            End If

            Return Convert.ToString(row(colonne)).Trim()
        End Function

        Private Function LireBooleenCellule(row As DataRow, colonne As String) As Boolean
            If row Is Nothing OrElse row.Table Is Nothing OrElse Not row.Table.Columns.Contains(colonne) OrElse row.IsNull(colonne) Then
                Return False
            End If

            Return Convert.ToBoolean(row(colonne))
        End Function

        Private Function LireDateCellule(row As DataRow, colonne As String) As Date?
            If row Is Nothing OrElse row.Table Is Nothing OrElse Not row.Table.Columns.Contains(colonne) OrElse row.IsNull(colonne) Then
                Return Nothing
            End If

            Return Convert.ToDateTime(row(colonne))
        End Function

        Private Function ConstruireProduitMisAJourDepuisEntree(produitId As Integer) As Produit
            Dim row As DataRow = ObtenirProduitCourantDepuisListe(produitId)
            If row Is Nothing Then
                Return Nothing
            End If

            Dim prixGrosSaisi As Boolean = Not String.IsNullOrWhiteSpace(txtPrixGros.Text) AndAlso txtPrixGros.Text.Trim() <> "-"
            Dim prixDemiSaisi As Boolean = Not String.IsNullOrWhiteSpace(txtPrixDemi.Text) AndAlso txtPrixDemi.Text.Trim() <> "-"
            Dim prixQuartSaisi As Boolean = Not String.IsNullOrWhiteSpace(txtPrixQuart.Text) AndAlso txtPrixQuart.Text.Trim() <> "-"
            Dim prixDetailSaisi As Boolean = Not String.IsNullOrWhiteSpace(txtPrixPiece.Text) AndAlso txtPrixPiece.Text.Trim() <> "-"
            Dim prixDouzaineSaisi As Boolean = Not String.IsNullOrWhiteSpace(txtPrixDouzaine.Text) AndAlso txtPrixDouzaine.Text.Trim() <> "-"
            Dim coefficientGrosSaisi As Boolean = Not String.IsNullOrWhiteSpace(txtCoefficientInput.Text)
            Dim conversionSaisie As Boolean = Not String.IsNullOrWhiteSpace(txtNbUniteParBase.Text)

            Dim produit As New Produit With {
                .ProduitId = produitId,
                .CodeBarres = LireTexteCellule(row, "CodeBarres"),
                .Libelle = LireTexteCellule(row, "Libelle"),
                .PrixAchat = LirePrixAchatEntreeEnCdf(),
                .PrixGros = If(chkGros.Checked, If(prixGrosSaisi, LireDecimal(txtPrixGros.Text), LireDecimalTable(row, "PrixGros")), 0D),
                .PrixDemi = If(chkDemi.Checked, If(prixDemiSaisi, LireDecimal(txtPrixDemi.Text), LireDecimalTable(row, "PrixDemi")), 0D),
                .PrixQuart = If(chkQuart.Checked, If(prixQuartSaisi, LireDecimal(txtPrixQuart.Text), LireDecimalTable(row, "PrixQuart")), 0D),
                .PrixDetail = If(chkPiece.Checked, If(prixDetailSaisi, LireDecimal(txtPrixPiece.Text), LireDecimalTable(row, "PrixDetail")), 0D),
                .PrixDouzaine = If(chkDouzaine.Checked, If(prixDouzaineSaisi, LireDecimal(txtPrixDouzaine.Text), LireDecimalTable(row, "PrixDouzaine")), 0D),
                .PrixSpecial = LireDecimalTable(row, "PrixSpecial"),
                .CoefficientGros = If(coefficientGrosSaisi AndAlso _coefficientCalcule > 0D, _coefficientCalcule, LireDecimalTable(row, "CoefficientGros")),
                .QuantiteStock = LireDecimalTable(row, "QuantiteStock"),
                .SeuilCritique = LireDecimalTable(row, "SeuilCritique"),
                .DateExpiration = LireDateCellule(row, "DateExpiration"),
                .CategorieId = If(row.Table.Columns.Contains("CategorieId") AndAlso Not row.IsNull("CategorieId"), Convert.ToInt32(row("CategorieId")), CType(Nothing, Integer?)),
                .UnitePrincipale = If(String.IsNullOrWhiteSpace(cmbUniteBase.Text), LireTexteCellule(row, "UnitePrincipale"), cmbUniteBase.Text.Trim()),
                .UniteSecondaire = LireTexteCellule(row, "UniteSecondaire"),
                .ConversionUnite = If(conversionSaisie AndAlso LireDecimal(txtNbUniteParBase.Text) > 0D, LireDecimal(txtNbUniteParBase.Text), If(EstGestionMesureEntree(), LireContenuUnitePrincipaleEntree(), LireDecimalTable(row, "ConversionUnite"))),
                .TypeGestionStock = TypeGestionStockEntree(),
                .UniteMesureStock = If(EstGestionMesureEntree(), UniteMesureStockEntree(), "PIECE"),
                .ContenuUnitePrincipale = LireContenuUnitePrincipaleEntree(),
                .ContenuUniteSecondaire = LireContenuUniteSecondaireEntree(),
                .EstActif = If(row.Table.Columns.Contains("EstActif") AndAlso Not row.IsNull("EstActif"), Convert.ToBoolean(row("EstActif")), True),
                .VenteDetail = chkPiece.Checked,
                .VenteDemi = chkDemi.Checked,
                .VenteDouzaine = chkDouzaine.Checked,
                .VenteGros = chkGros.Checked
            }

            Debug.WriteLine(String.Format(
                Globalization.CultureInfo.InvariantCulture,
                "[StockEntree] Produit capture avant update ProduitId={0}, PrixAchat={1}, PrixGros={2}, PrixDemi={3}, PrixDetail={4}, PrixQuart={5}, PrixDouzaine={6}, PrixSpecial={7}, CoefficientGros={8}, UnitePrincipale={9}, UniteSecondaire={10}, ConversionUnite={11}, VenteGros={12}, VenteDemi={13}, VenteDetail={14}, VenteDouzaine={15}",
                produit.ProduitId,
                produit.PrixAchat,
                produit.PrixGros,
                produit.PrixDemi,
                produit.PrixDetail,
                produit.PrixQuart,
                produit.PrixDouzaine,
                produit.PrixSpecial,
                produit.CoefficientGros,
                If(produit.UnitePrincipale, String.Empty),
                If(produit.UniteSecondaire, String.Empty),
                produit.ConversionUnite,
                produit.VenteGros,
                produit.VenteDemi,
                produit.VenteDetail,
                produit.VenteDouzaine))

            Return produit
        End Function

        Private Sub MettreAJourProduitExistantDepuisEntree(produitId As Integer, Optional produitCapture As Produit = Nothing)
            Dim produit As Produit = If(produitCapture, ConstruireProduitMisAJourDepuisEntree(produitId))
            If produit Is Nothing Then
                Return
            End If

            Debug.WriteLine(String.Format(Globalization.CultureInfo.InvariantCulture, "[StockEntree] Appel ProduitService.MettreAJour pour ProduitId={0}", produit.ProduitId))
            Dim serviceProduit As ProduitService = ObtenirService()
            serviceProduit.MettreAJour(produit)
        End Sub
        '""""################################################################
        '"""########### Sortie manuel ################################"#####"
        Private Sub AjouterAuPanier(sender As Object, e As EventArgs) '"""#### Nouvelle logique tres bon
            If cmbProduitSortie.SelectedValue Is Nothing Then Return
            Dim service As ProduitService = ObtenirService()


            Dim qte As Decimal
            If Not Decimal.TryParse(txtQuantiteSortie.Text.Trim(), qte) OrElse qte <= 0D Then
                MessageBox.Show("Quantite invalide.")
                Return
            End If

            If cmbTypeVente.SelectedItem Is Nothing Then
                MessageBox.Show("Veuillez choisir l'unite.")
                Return
            End If

            Dim produitId As Integer = Convert.ToInt32(cmbProduitSortie.SelectedValue)
            Dim libelle As String = Convert.ToString(cmbProduitSortie.SelectedValue)
            Dim typeChoisi As TypeVenteDTO = ObtenirTypeVenteSelectionne()
            If typeChoisi Is Nothing Then
                MessageBox.Show("Type de vente invalide.")
                Return
            End If
            Dim unite As String = typeChoisi.Nom
            Dim prix As Decimal = PrixSelonUnite()
            Dim quantiteEquivalent As Decimal = typeChoisi.QuantiteEquivalent
            Dim quantiteBase As Decimal = qte * quantiteEquivalent
            Dim stock As Decimal = service.AfficherQteProduitSelect(produitId)


            MessageBox.Show(Convert.ToString(stock))

            Dim deja As Decimal = 0D
            For Each l As PanierLigne In _panier
                If l.ProduitId = produitId Then
                    deja += l.QuantiteBase
                End If
            Next

            If deja + quantiteBase > stock Then
                MessageBox.Show("Stock insuffisant pour ce produit.")
                Return
            End If

            Dim ligne As PanierLigne = _panier.Find(Function(x) x.ProduitId = produitId AndAlso x.Unite = unite)
            If ligne Is Nothing Then
                ligne = New PanierLigne With {.ProduitId = produitId, .Libelle = libelle, .Unite = unite, .PrixUnitaire = prix, .Quantite = qte, .QuantiteBase = quantiteBase, .QuantiteEquivalente = quantiteEquivalent, .QuantiteReelle = quantiteBase, .Total = prix * qte}
                _panier.Add(ligne)
            Else
                ligne.Quantite += qte
                ligne.QuantiteBase += quantiteBase
                ligne.QuantiteReelle += quantiteBase
                ligne.QuantiteEquivalente = quantiteEquivalent
                ligne.Total = ligne.PrixUnitaire * ligne.Quantite
            End If

            RafraichirPanier()
        End Sub

        Private Function ObtenirMotifSelectionne() As DataRow
            Dim row As DataRowView = TryCast(cmbMotif.SelectedItem, DataRowView)
            If row Is Nothing Then Return Nothing
            Return row.Row
        End Function

        Private Function CalculerTotalPanier() As Decimal
            Dim total As Decimal = 0D
            For Each l As PanierLigne In _panier
                total += l.Total
            Next
            Return total
        End Function

        Private Sub RetirerDuPanier(sender As Object, e As EventArgs)
            If gridPanier.CurrentRow Is Nothing Then Return
            Dim produitId As Integer = Convert.ToInt32(gridPanier.CurrentRow.Cells(0).Value)
            Dim unite As String = Convert.ToString(gridPanier.CurrentRow.Cells("Unite").Value) '######modifier indice
            _panier.RemoveAll(Function(x) x.ProduitId = produitId AndAlso x.Unite = unite)
            RafraichirPanier()
        End Sub

        Private Sub RafraichirPanier()
            gridPanier.DataSource = Nothing
            gridPanier.DataSource = _panier
            If gridPanier.Columns.Contains("ProduitId") Then gridPanier.Columns("ProduitId").Visible = False
            If gridPanier.Columns.Contains("QuantiteBase") Then gridPanier.Columns("QuantiteBase").Visible = False
            If gridPanier.Columns.Contains("Quantite") Then gridPanier.Columns("Quantite").HeaderText = "Quantité saisie"
            If gridPanier.Columns.Contains("QuantiteEquivalente") Then gridPanier.Columns("QuantiteEquivalente").HeaderText = "Quantité équivalente"
            If gridPanier.Columns.Contains("QuantiteReelle") Then gridPanier.Columns("QuantiteReelle").HeaderText = "Quantité réelle"

            Dim sousTotal As Decimal = 0D
            For Each l As PanierLigne In _panier
                sousTotal += l.Total
            Next


            Dim total As Decimal = sousTotal

            lblSousTotal.Text = "Sous-total: " & sousTotal.ToString()
            lblTotal.Text = "Total: " & total.ToString()
            MettreAJourAffichageStockProduit()
        End Sub
        Private Sub ChargerUnites(sender As Object, e As EventArgs)
            If cmbProduitSortie.SelectedValue IsNot Nothing AndAlso Not TypeOf cmbProduitSortie.SelectedValue Is DataRowView Then
                Dim produitId As Integer = Convert.ToInt32(cmbProduitSortie.SelectedValue)
                Dim row As DataRowView = TryCast(cmbProduitSortie.SelectedItem, DataRowView)
                If row Is Nothing Then Return
                Dim r As DataRow = row.Row
                Dim nbUnites As Decimal = Convert.ToDecimal((If(r.IsNull("ConversionUnite"), "", Convert.ToDecimal(row("ConversionUnite")).ToString())))
                Dim prixAchat As Decimal = Convert.ToDecimal(If(r.IsNull("PrixAchat"), "", Convert.ToDecimal(row("PrixAchat")).ToString()))
                Dim prixGros As Decimal = Convert.ToDecimal(If(r.IsNull("PrixGros"), "", Convert.ToDecimal(row("PrixGros")).ToString()))
                Dim prixDemi As Decimal = Convert.ToDecimal(If(r.IsNull("PrixDemi"), "", Convert.ToDecimal(row("PrixDemi")).ToString()))
	                Dim prixDetail As Decimal = Convert.ToDecimal(If(r.IsNull("PrixDetail"), "", Convert.ToDecimal(row("PrixDetail")).ToString()))
	                Dim prixQuart As Decimal = Convert.ToDecimal(If(r.IsNull("PrixQuart"), "", Convert.ToDecimal(row("PrixQuart")).ToString()))
	                Dim prixDouzaine As Decimal = Convert.ToDecimal(If(r.IsNull("PrixDouzaine"), "", Convert.ToDecimal(row("PrixDouzaine")).ToString()))
	                Dim prixSpecial As Decimal = Convert.ToDecimal(If(r.IsNull("PrixSpecial"), "", Convert.ToDecimal(row("PrixSpecial")).ToString()))
	                Dim contenuUnitePrincipale As Decimal = LireDecimalTable(r, "ContenuUnitePrincipale")
	                Dim contenuUniteSecondaire As Decimal = LireDecimalTable(r, "ContenuUniteSecondaire")
	                Dim typeGestion As String = LireTexteCellule(r, "TypeGestionStock")
	                Dim uniteSecondaire As String = LireTexteCellule(r, "UniteSecondaire")

	                Dim venteDetail As Boolean = If(IsDBNull(row("VenteDetail")), False, Convert.ToInt32(row("VenteDetail")) = 1)
	                Dim venteDemi As Boolean = If(IsDBNull(row("VenteDemi")), False, Convert.ToDecimal(row("VenteDemi")) = 1)
	                Dim venteDouzaine As Boolean = If(IsDBNull(row("VenteDouzaine")), False, Convert.ToDecimal(row("VenteDouzaine")) = 1)
	                Dim venteGros As Boolean = If(IsDBNull(row("VenteGros")), False, Convert.ToDecimal(row("VenteGros")) = 1)

	                _typesVenteCourants = _typeVenteService.ConstruireTypesVentePourProduit(produitId, nbUnites, prixAchat, prixGros, prixDemi, prixDetail, prixQuart, prixDouzaine, prixSpecial, venteGros, venteDemi, venteDetail, venteDouzaine, Nothing, contenuUnitePrincipale, contenuUniteSecondaire, typeGestion, uniteSecondaire)
                cmbTypeVente.DataSource = Nothing
                cmbTypeVente.DisplayMember = "NomAffichage"
                cmbTypeVente.ValueMember = "Nom"
                cmbTypeVente.DataSource = _typesVenteCourants
                If cmbTypeVente.Items.Count > 0 Then cmbTypeVente.SelectedIndex = 0

                MettreAJourAffichageStockProduit()
                MiseAJourPrixUnitaire(Nothing, EventArgs.Empty)
            End If
        End Sub

        Private Sub MiseAJourPrixUnitaire(sender As Object, e As EventArgs)
            If cmbProduitSortie.SelectedValue Is Nothing Then Return
            Dim typeChoisi As TypeVenteDTO = ObtenirTypeVenteSelectionne()
            Dim prix As Decimal = PrixSelonUnite()
            lblPrixProd.Text = FormatageGlobal.FormatMontant(prix)
            If typeChoisi Is Nothing Then
                lblEquivalent.Text = "Equivalent: 0 " & ObtenirUniteReferenceSortie() & " / unité"
            Else
                lblEquivalent.Text = "Equivalent: " & FormaterQuantiteReferenceSortie(typeChoisi.QuantiteEquivalent) & " " & ObtenirUniteReferenceSortie() & " / unité"
            End If
            MiseAJourIndicateursQuantite(Nothing, EventArgs.Empty)
        End Sub

        Private Function PrixSelonUnite() As Decimal
            Dim typeChoisi As TypeVenteDTO = ObtenirTypeVenteSelectionne()
            If typeChoisi Is Nothing Then
                Return 0D
            End If
            Return typeChoisi.PrixVente
        End Function

        Private Function ObtenirTypeVenteSelectionne() As TypeVenteDTO
            Return TryCast(cmbTypeVente.SelectedItem, TypeVenteDTO)
        End Function

        Private Function ObtenirLigneProduitSortieSelectionnee() As DataRow
            Dim rowView As DataRowView = TryCast(cmbProduitSortie.SelectedItem, DataRowView)
            If rowView Is Nothing Then Return Nothing
            Return rowView.Row
        End Function

        Private Function EstGestionMesureSortie() As Boolean
            Dim row As DataRow = ObtenirLigneProduitSortieSelectionnee()
            If row Is Nothing Then Return False
            Return StockUnitConversionService.EstGestionMesuree(LireTexteCellule(row, "TypeGestionStock"))
        End Function

        Private Function ObtenirUniteReferenceSortie() As String
            Dim row As DataRow = ObtenirLigneProduitSortieSelectionnee()
            If row Is Nothing Then Return "pièce"

            If EstGestionMesureSortie() Then
                Dim uniteMesure As String = LireTexteCellule(row, "UniteMesureStock")
                If uniteMesure <> String.Empty Then Return uniteMesure
                Return "mesure"
            End If

            Dim uniteSecondaire As String = LireTexteCellule(row, "UniteSecondaire")
            If uniteSecondaire <> String.Empty Then Return uniteSecondaire
            Return "pièce"
        End Function

        Private Function FormaterQuantiteReferenceSortie(valeur As Decimal) As String
            If EstGestionMesureSortie() Then
                Return FormatageGlobal.FormatQuantitePhysique(valeur)
            End If

            Return valeur.ToString("N0")
        End Function

        Private Function VerifierStockAvantValidation() As Boolean
            For Each l As PanierLigne In _panier
                Dim stock As Decimal = ObtenirStockParProduit(l.ProduitId)
                If l.QuantiteBase > stock Then
                    MessageBox.Show("Stock insuffisant pour: " & l.Libelle)
                    Return False
                End If
            Next
            Return True
        End Function

        Private Function ObtenirStockParProduit(produitId As Integer) As Decimal
            If _produitsTable Is Nothing Then Return 0D
            For Each row As DataRow In _produitsTable.Rows
                If Convert.ToInt32(row(0)) = produitId Then
                    Return Convert.ToDecimal(row(11))
                End If
            Next
            Return 0D
        End Function
        Private Sub MiseAJourIndicateursQuantite(sender As Object, e As EventArgs)
            Dim qte As Decimal
            If Not Decimal.TryParse(txtQuantiteSortie.Text.Trim(), qte) OrElse qte <= 0D Then
                lblTotalReel.Text = "Total réel: 0 " & ObtenirUniteReferenceSortie()
                Return
            End If

            Dim typeChoisi As TypeVenteDTO = ObtenirTypeVenteSelectionne()
            If typeChoisi Is Nothing Then
                lblTotalReel.Text = "Total réel: 0 " & ObtenirUniteReferenceSortie()
                Return
            End If

            Dim quantiteReelle As Decimal = qte * typeChoisi.QuantiteEquivalent
            lblTotalReel.Text = "Total réel: " & FormaterQuantiteReferenceSortie(quantiteReelle) & " " & ObtenirUniteReferenceSortie()
        End Sub

        Private Sub MettreAJourAffichageStockProduit()
            If cmbProduitSortie.SelectedValue IsNot Nothing AndAlso Not TypeOf cmbProduitSortie.SelectedValue Is DataRowView Then
                Dim row As DataRowView = TryCast(cmbProduitSortie.SelectedItem, DataRowView)
                If row Is Nothing Then Return
                Dim r As DataRow = row.Row
                Dim produitId As Integer = Convert.ToInt32((If(r.IsNull("ProduitId"), "", Convert.ToDecimal(row("ProduitId")).ToString())))
                Dim stock As Decimal = LireDecimalTable(r, "QuantiteStock")
                Dim nbUnites As Decimal = LireDecimalTable(r, "ConversionUnite")
                Dim uniteBase As String = LireTexteCellule(r, "UnitePrincipale")
                Dim uniteSecondaire As String = LireTexteCellule(r, "UniteSecondaire")
                Dim typeGestion As String = LireTexteCellule(r, "TypeGestionStock")
                Dim uniteMesure As String = LireTexteCellule(r, "UniteMesureStock")
                Dim contenuPrincipal As Decimal = LireDecimalTable(r, "ContenuUnitePrincipale")
                Dim contenuSecondaire As Decimal = LireDecimalTable(r, "ContenuUniteSecondaire")
                Dim reserve As Decimal = 0D
                For Each ligne As PanierLigne In _panier
                    If ligne.ProduitId = produitId Then
                        reserve += ligne.QuantiteBase
                    End If
                Next
                Dim restant As Decimal = Math.Max(0D, stock - reserve)
                lblStock.Text = "Stock: " & FormatageGlobal.FormatStockSelonGestion(stock, nbUnites, uniteBase, uniteSecondaire, typeGestion, uniteMesure, contenuPrincipal, contenuSecondaire) &
                    " | Restant: " & FormatageGlobal.FormatStockSelonGestion(restant, nbUnites, uniteBase, uniteSecondaire, typeGestion, uniteMesure, contenuPrincipal, contenuSecondaire)
            End If
        End Sub

        'Private Sub AfficherStockActuelSorti()
        '    If cmbProduitSortie.SelectedValue IsNot Nothing AndAlso Not TypeOf cmbProduitSortie.SelectedValue Is DataRowView Then
        '        Dim row As DataRowView = TryCast(cmbProduitSortie.SelectedItem, DataRowView)
        '        If row Is Nothing Then Return
        '        Dim r As DataRow = row.Row
        '        Dim produitId As Integer = Convert.ToInt32(cmbProduitSortie.SelectedValue)
        '        Dim service As StockService = ObtenirStockService()
        '        Dim stockPieces As Decimal = service.ObtenirStockActuelProduit(produitId)
        '        Dim nb As Decimal = LireDecimal(If(r.IsNull("ConversionUnite"), "", Convert.ToDecimal(row("ConversionUnite")).ToString()))
        '        Dim uniteBase As String = If(r.IsNull("UnitePrincipale"), "", Convert.ToString(row("UnitePrincipale")))
        '        Dim stockBase As Decimal = If(nb > 0D, Decimal.Floor(stockPieces / nb), stockPieces)
        '        lblStockActuelS.Text = "Stock actuel: " & stockBase.ToString("N0") & " " & uniteBase
        '        lblStockActuelPieceS.Text = "Equivalent: " & stockPieces.ToString("N0") & " pièces"
        '    End If
        'End Sub
        Private Sub RecalculerStock(sender As Object, e As EventArgs)
            Dim nb As Decimal = LireDecimal(txtNbUniteParBase.Text)
            Dim quantiteEntree As Decimal = LireDecimal(txtQuantiteEntree.Text)
            Dim quantiteSecondaire As Decimal = LireDecimal(txtQuantiteSecondaireEntree.Text)
            Dim stockActuelPieces As Decimal = _stockActuelEntreeBase
            Dim totalPiecesEntree As Decimal = CalculerQuantiteBaseEntree(quantiteEntree, quantiteSecondaire, nb)
            Dim stockApresPieces As Decimal = stockActuelPieces + totalPiecesEntree
            Dim uniteBase As String = If(cmbUniteBase.Text.Trim() = "", "base", cmbUniteBase.Text.Trim())
            Dim uniteSecondaire As String = ObtenirUniteSecondaireEntree()
            Dim contenuSecondaire As Decimal? = LireContenuUniteSecondaireEntree()
            Dim uniteComplement As String = uniteSecondaire
            If EstGestionMesureEntree() AndAlso contenuSecondaire.HasValue Then
                Dim uniteSecondaireProduit As String = ObtenirUniteSecondaireProduitSelectionne()
                If Not String.IsNullOrWhiteSpace(uniteSecondaireProduit) Then uniteComplement = uniteSecondaireProduit
            End If
            Dim formatQuantite As String = If(EstGestionMesureEntree(), "N2", "N0")
            Dim libelleEquivalent As String = If(EstGestionMesureEntree(), "Equivalent physique: ", "Equivalent: ")

            Dim stockActuelLisible As String = FormaterStockLisible(stockActuelPieces, nb, uniteBase, uniteSecondaire)
            Dim stockApresLisible As String = FormaterStockLisible(stockApresPieces, nb, uniteBase, uniteSecondaire)
            If EstGestionMesureEntree() Then
                lblStockActuel.Text = "Stock actuel: " & FormatageGlobal.FormatQuantitePhysique(stockActuelPieces) & " " & uniteSecondaire
                lblStockActuelPiece.Text = "Equivalent: " & stockActuelLisible
                lblStockApres.Text = "Stock après: " & FormatageGlobal.FormatQuantitePhysique(stockApresPieces) & " " & uniteSecondaire
                lblStockApresPiece.Text = "Après: " & FormatageGlobal.FormatQuantitePhysique(stockApresPieces) & " " & uniteSecondaire & " (" & stockApresLisible & ")"
            Else
                lblStockActuel.Text = "Stock actuel: " & stockActuelLisible
                lblStockActuelPiece.Text = libelleEquivalent & stockActuelPieces.ToString(formatQuantite) & " " & uniteSecondaire
                lblStockApres.Text = "Stock après: " & stockApresLisible
                lblStockApresPiece.Text = "Après: " & stockApresPieces.ToString(formatQuantite) & " " & uniteSecondaire & " (" & quantiteEntree.ToString("N0") & " " & uniteBase & " + " & quantiteSecondaire.ToString(formatQuantite) & " " & uniteComplement & ")"
            End If
            RafraichirTypesVente()
        End Sub

        Private Function CalculerQuantiteBaseEntree(quantitePrincipale As Decimal, quantiteSecondaire As Decimal, conversion As Decimal) As Decimal
            Dim contenuSecondaire As Decimal? = LireContenuUniteSecondaireEntree()
            Return StockUnitConversionService.CalculerQuantiteEntreeNormalisee(
                quantitePrincipale,
                quantiteSecondaire,
                conversion,
                TypeGestionStockEntree(),
                LireContenuUnitePrincipaleEntree(),
                If(contenuSecondaire.HasValue, contenuSecondaire.Value, 0D))
        End Function

        Private Function FormaterStockLisible(stockBase As Decimal, conversion As Decimal, unitePrincipale As String, uniteSecondaire As String) As String
            If EstGestionMesureEntree() Then
                Dim contenuSecondaire As Decimal? = LireContenuUniteSecondaireEntree()
                Dim decomposition As String = FormatageGlobal.DecomposerStockMesure(
                    stockBase,
                    conversion,
                    unitePrincipale,
                    ObtenirUniteSecondaireProduitSelectionne(),
                    uniteSecondaire,
                    LireContenuUnitePrincipaleEntree(),
                    If(contenuSecondaire.HasValue, contenuSecondaire.Value, 0D))
                If decomposition <> String.Empty Then Return decomposition
                Return FormatageGlobal.FormatQuantitePhysique(stockBase) & " " & uniteSecondaire
            End If

            If conversion <= 0D Then
                Return stockBase.ToString("N0") & " " & uniteSecondaire
            End If

            Dim quantitePrincipaleUnite As Decimal = Decimal.Floor(stockBase / conversion)
            Dim resteUnite As Decimal = stockBase - (quantitePrincipaleUnite * conversion)
            If resteUnite > 0D Then
                Return quantitePrincipaleUnite.ToString("N0") & " " & unitePrincipale & " + " & resteUnite.ToString("N0") & " " & uniteSecondaire
            End If

            Return quantitePrincipaleUnite.ToString("N0") & " " & unitePrincipale
        End Function

        Private Function ObtenirUniteSecondaireProduitSelectionne() As String
            Dim produitId As Integer = ObtenirProduitEntreeSelectionneId()
            Dim row As DataRow = If(produitId > 0, ObtenirProduitCourantDepuisListe(produitId), Nothing)
            If row IsNot Nothing Then
                Dim unite As String = LireTexteCellule(row, "UniteSecondaire")
                If Not String.IsNullOrWhiteSpace(unite) Then Return unite
            End If
            Return String.Empty
        End Function

        'Private Sub RecalculerStockSortie(sender As Object, e As EventArgs)
        '    If cmbProduitSortie.SelectedValue IsNot Nothing AndAlso Not TypeOf cmbProduitSortie.SelectedValue Is DataRowView Then
        '        Dim produitId As Integer = Convert.ToInt32(cmbProduitSortie.SelectedValue)
        '        Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
        '        Dim dal As New DAL(cs)
        '        Dim stock As Object = dal.ExecuterScalaire("SELECT ISNULL(QuantiteStock,0) FROM vStockProduit WHERE ProduitId=@id", CommandType.Text, New List(Of System.Data.SqlClient.SqlParameter) From {New System.Data.SqlClient.SqlParameter("@id", produitId)})
        '        Dim stockActuel As Decimal = If(stock Is Nothing, 0D, Convert.ToDecimal(stock))
        '        Dim qte As Decimal = LireDecimal(txtQuantiteSortie.Text)
        '        Dim restant As Decimal = stockActuel - qte
        '        txtStockRestant.Text = restant.ToString("N0")
        '    End If
        'End Sub
        'chargement types ventes tab sortie 
        'Private Sub chargerTypesVente(sender As Object, e As EventArgs)
        '    If cmbProduitSortie.SelectedValue IsNot Nothing AndAlso Not TypeOf cmbProduitSortie.SelectedValue Is DataRowView Then
        '        Dim produitId As Integer = Convert.ToInt32(cmbProduitSortie.SelectedValue)
        '        Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
        '        Dim dal As New DAL(cs)

        '        Dim repo As New ProduitRepository(dal)
        '        _produitsTable = repo.ListerTypeVente(produitId)


        '        cmbTypeVente.DataSource = _produitsTable
        '        cmbTypeVente.DisplayMember = "TypeVente"
        '        cmbTypeVente.ValueMember = "Prix"
        '        lblPrixProd.Text = cmbTypeVente.SelectedValue.ToString & "FC"
        '        AfficherStockActuelSorti()
        '    End If
        'End Sub


        'Private Sub chargerPrix(sender As Object, e As EventArgs)
        '    If cmbProduitSortie.SelectedValue IsNot Nothing AndAlso Not TypeOf cmbProduitSortie.SelectedValue Is DataRowView Then

        '        lblPrixProd.Text = cmbTypeVente.SelectedValue.ToString & "FC"
        '    End If
        'End Sub

        Private Sub CoefficientInputChange(sender As Object, e As EventArgs)
            If Not _miseAJourCoefficientDepuisPrix Then
                _prixManuelOverrides.Remove("txtPrixGros")
                _prixManuelOverrides.Remove("txtPrixDemi")
            End If
            If String.IsNullOrWhiteSpace(txtCoefficientInput.Text) Then
                _coefficientCalcule = 0D
                lblTypeCoefficient.Text = ""
                lblMargeCalculee.Text = ""
                RecalculerPrixAuto(Nothing, EventArgs.Empty)
                Exit Sub
            End If

            Dim coefficient As Decimal
            Dim marge As Decimal
            If Not TenterLireCoefficient(txtCoefficientInput.Text, coefficient, marge) Then
                Exit Sub
            End If

            _coefficientCalcule = coefficient
            lblTypeCoefficient.Text = "Coefficient " & coefficient.ToString("N2")
            lblMargeCalculee.Text = Math.Round(marge, 2).ToString("N2") & " %"
            RecalculerPrixAuto(Nothing, EventArgs.Empty)
        End Sub

        Private Sub CoefficientDetailChange(sender As Object, e As EventArgs)
            If Not _miseAJourCoefficientDepuisPrix Then
                _prixManuelOverrides.Remove("txtPrixQuart")
                _prixManuelOverrides.Remove("txtPrixPiece")
                _prixManuelOverrides.Remove("txtPrixDouzaine")
            End If
            If String.IsNullOrWhiteSpace(txtCoefficientDetail.Text) Then
                _coefficientDetailCalcule = 0D
                lblMargeDetailCalculee.Text = ""
                RecalculerPrixAuto(Nothing, EventArgs.Empty)
                Return
            End If

            Dim coefficient As Decimal
            Dim marge As Decimal
            If TenterLireCoefficient(txtCoefficientDetail.Text, coefficient, marge) Then
                _coefficientDetailCalcule = coefficient
                lblMargeDetailCalculee.Text = Math.Round(marge, 2).ToString("N2") & " %"
                RecalculerPrixAuto(Nothing, EventArgs.Empty)
            End If
        End Sub

        Private Sub PrixVenteManuelChange(sender As Object, e As EventArgs)
            If _isUpdatingPrixAutomatiques Then
                Return
            End If

            Dim txt As TextBox = TryCast(sender, TextBox)
            If txt Is Nothing Then
                Return
            End If

            _prixManuelOverrides(txt.Name) = Not String.IsNullOrWhiteSpace(txt.Text) AndAlso txt.Text.Trim() <> "-"
            If _prixManuelOverrides(txt.Name) Then
                RecalculerCoefficientDepuisPrixManuel(txt)
            End If
        End Sub

        Private Sub RecalculerCoefficientDepuisPrixManuel(zone As TextBox)
            If zone Is Nothing Then Return

            Dim prixAchatVal As Decimal = LirePrixAchatEntreeEnCdf()
            Dim nbUnites As Decimal = LireDecimal(txtNbUniteParBase.Text)
            Dim prixManuel As Decimal = LireDecimal(zone.Text)
            If prixAchatVal <= 0D OrElse prixManuel <= 0D Then Return

            Dim coefficient As Decimal = 0D
            If String.Equals(zone.Name, "txtPrixGros", StringComparison.OrdinalIgnoreCase) Then
                coefficient = CalculVenteService.CalculerCoefficientDepuisPrix(prixAchatVal, prixManuel)
                AppliquerCoefficientDepuisPrix(txtCoefficientInput, lblTypeCoefficient, lblMargeCalculee, coefficient)
            ElseIf String.Equals(zone.Name, "txtPrixDemi", StringComparison.OrdinalIgnoreCase) Then
                coefficient = CalculVenteService.CalculerCoefficientDepuisPrix(prixAchatVal, prixManuel * 2D)
                AppliquerCoefficientDepuisPrix(txtCoefficientInput, lblTypeCoefficient, lblMargeCalculee, coefficient)
            ElseIf nbUnites > 0D Then
                Dim quantiteType As Decimal = 1D
                If String.Equals(zone.Name, "txtPrixQuart", StringComparison.OrdinalIgnoreCase) Then
                    quantiteType = Math.Max(1D, Decimal.Floor(nbUnites / 4D))
                ElseIf String.Equals(zone.Name, "txtPrixDouzaine", StringComparison.OrdinalIgnoreCase) Then
                    quantiteType = 12D
                End If

                Dim prixReferenceDetail As Decimal = (prixManuel / quantiteType) * nbUnites
                coefficient = CalculVenteService.CalculerCoefficientDepuisPrix(prixAchatVal, prixReferenceDetail)
                AppliquerCoefficientDepuisPrix(txtCoefficientDetail, Nothing, lblMargeDetailCalculee, coefficient)
            End If
        End Sub

        Private Sub AppliquerCoefficientDepuisPrix(zoneCoefficient As TextBox, labelCoefficient As Label, labelPourcentage As Label, coefficient As Decimal)
            If zoneCoefficient Is Nothing OrElse coefficient <= 0D Then Return
            Dim pourcentage As Decimal = CalculVenteService.CalculerPourcentageDepuisCoefficient(coefficient)
            _miseAJourCoefficientDepuisPrix = True
            Try
                zoneCoefficient.Text = coefficient.ToString("N2")
                If labelCoefficient IsNot Nothing Then
                    labelCoefficient.Text = "Coefficient " & coefficient.ToString("N2")
                End If
                If labelPourcentage IsNot Nothing Then
                    labelPourcentage.Text = pourcentage.ToString("N2") & " %"
                End If
            Finally
                _miseAJourCoefficientDepuisPrix = False
            End Try
        End Sub

        Private Sub ReinitialiserOverridesPrixVente()
            _prixManuelOverrides.Clear()
        End Sub

        Private Function PrixVenteEnOverride(nomControle As String) As Boolean
            Dim valeur As Boolean = False
            If _prixManuelOverrides.TryGetValue(nomControle, valeur) Then
                Return valeur
            End If
            Return False
        End Function

        Private Sub DefinirPrixCalcule(zone As TextBox, valeur As Decimal, actif As Boolean)
            If zone Is Nothing Then
                Return
            End If
            If PrixVenteEnOverride(zone.Name) Then
                Return
            End If

            _isUpdatingPrixAutomatiques = True
            Try
                zone.Text = If(actif, valeur.ToString("N0"), "-")
            Finally
                _isUpdatingPrixAutomatiques = False
            End Try
        End Sub

        Private Sub RecalculerPrixAuto(sender As Object, e As EventArgs)
            MettreAJourEquivalentPrixAchat()
            Dim prixAchatVal As Decimal = LirePrixAchatEntreeEnCdf()
            Dim nbUnites As Decimal = LireDecimal(txtNbUniteParBase.Text)
            Dim coefficientGros As Decimal = If(_coefficientCalcule > 0D, _coefficientCalcule, 0D)
            Dim coefficientDetail As Decimal = If(_coefficientDetailCalcule > 0D, _coefficientDetailCalcule, 0D)

            If prixAchatVal <= 0D OrElse nbUnites <= 0D Then Return

            Dim prixGros As Decimal = 0D
            Dim prixDemi As Decimal = 0D
            If coefficientGros > 0D Then
                prixGros = prixAchatVal * coefficientGros
                prixDemi = prixGros * 0.5D
            End If
            Dim prixPiece As Decimal = 0D
            If coefficientDetail > 0D Then
                prixPiece = (prixAchatVal * coefficientDetail) / nbUnites
            End If
            Dim prixQuart As Decimal = prixPiece * Math.Max(1D, Decimal.Floor(nbUnites / 4D))
            Dim prixDouzaine As Decimal = prixPiece * 12D

            DefinirPrixCalcule(txtPrixGros, prixGros, chkGros.Checked AndAlso coefficientGros > 0D)
            DefinirPrixCalcule(txtPrixDemi, prixDemi, chkDemi.Checked AndAlso coefficientGros > 0D)
            DefinirPrixCalcule(txtPrixQuart, prixQuart, chkQuart.Checked)
            DefinirPrixCalcule(txtPrixPiece, prixPiece, chkPiece.Checked)
            DefinirPrixCalcule(txtPrixDouzaine, prixDouzaine, chkDouzaine.Checked)
        End Sub
        'Private Sub ChargerSortiesDuMois(sender As Object, e As EventArgs)
        '    Try
        '        Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
        '        Dim dal As New DAL(cs)
        '        Dim sql As String = "" &
        '            "SELECT ss.DateSortie, ISNULL(f.NumeroFacture, ss.RefSource) AS NumeroFacture, " &
        '            "ISNULL(c.NomClient, '') AS Client, p.Libelle AS Produit, ss.QuantiteSaisie, ss.QuantiteBase, ss.Source " &
        '            "FROM StockSortie ss " &
        '            "INNER JOIN Produits p ON p.ProduitId = ss.ProduitId " &
        '            "LEFT JOIN FacturesVente f ON f.NumeroFacture = ss.RefSource " &
        '            "LEFT JOIN Clients c ON c.ClientId = f.ClientId " &
        '            "WHERE CAST(ss.DateSortie AS DATE) BETWEEN @Du AND @Au " &
        '            "AND (@Recherche = '' OR ISNULL(f.NumeroFacture, ss.RefSource) LIKE @Like OR ISNULL(c.NomClient, '') LIKE @Like) " &
        '            "ORDER BY ss.DateSortie DESC"
        '        Dim recherche As String = txtRechercheSortie.Text.Trim()
        '        Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
        '            New System.Data.SqlClient.SqlParameter("@Du", dtpSortieDu.Value.Date),
        '            New System.Data.SqlClient.SqlParameter("@Au", dtpSortieAu.Value.Date),
        '            New System.Data.SqlClient.SqlParameter("@Recherche", recherche),
        '            New System.Data.SqlClient.SqlParameter("@Like", "%" & recherche & "%")
        '        }
        '        gridSortieMois.DataSource = dal.ExecuterTable(sql, CommandType.Text, p)
        '    Catch ex As Exception
        '        MessageBox.Show("Erreur chargement sorties: " & ex.Message)
        '    End Try
        'End Sub

        Private Sub ChargerSortiesDuMois(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim sql As String = "" &
                    "SELECT ss.NumeroSortie, ss.DateSortie, ISNULL(c.NomClient, '') AS Client, ISNULL(m.Libelle, ss.Source) AS Motif, " &
                    "p.Libelle AS Produit, ss.QuantiteSaisie, ss.QuantiteBase, ss.TypeVente, ss.PrixUnitaire, ss.MontantLigne, ss.StatutPaiement, ss.MontantPaye, ss.ResteAPayer " &
                    "FROM StockSortie ss " &
                    "INNER JOIN Produits p ON p.ProduitId = ss.ProduitId " &
                    "LEFT JOIN Clients c ON c.ClientId = ss.ClientId " &
                    "LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId " &
                    "WHERE CAST(ss.DateSortie AS DATE) BETWEEN @Du AND @Au " &
                    "AND (@Recherche = '' OR ss.NumeroSortie LIKE @Like OR ISNULL(c.NomClient, '') LIKE @Like OR ISNULL(m.Libelle, ss.Source) LIKE @Like) " &
                    "ORDER BY ss.DateSortie DESC"
                Dim recherche As String = txtRechercheSortie.Text.Trim()
                Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                    New System.Data.SqlClient.SqlParameter("@Du", dtpSortieDu.Value.Date),
                    New System.Data.SqlClient.SqlParameter("@Au", dtpSortieAu.Value.Date),
                    New System.Data.SqlClient.SqlParameter("@Recherche", recherche),
                    New System.Data.SqlClient.SqlParameter("@Like", "%" & recherche & "%")
                }
                gridSortieMois.DataSource = dal.ExecuterTable(sql, CommandType.Text, p)
            Catch ex As Exception
                MessageBox.Show("Erreur chargement sorties: " & ex.Message)
            End Try
        End Sub

        Private Sub RafraichirTypesVente()
            Dim produitId As Integer = ObtenirProduitEntreeSelectionneId()
            Dim typesPersonnalisesActifs As List(Of TypeVenteProduitDTO) = If(produitId > 0, ConstruireTypesPersonnalisesActifsPourProduit(produitId), New List(Of TypeVenteProduitDTO)())
            Dim contenuSecondaire As Decimal? = LireContenuUniteSecondaireEntree()
            Dim liste As List(Of TypeVenteDTO) = _typeVenteService.ConstruireTypesVentePourProduit(
                produitId,
                LireDecimal(txtNbUniteParBase.Text),
                LirePrixAchatEntreeEnCdf(),
                LireDecimal(txtPrixGros.Text.Replace("-", "0")),
                LireDecimal(txtPrixDemi.Text.Replace("-", "0")),
                LireDecimal(txtPrixPiece.Text.Replace("-", "0")),
                LireDecimal(txtPrixQuart.Text.Replace("-", "0")),
                LireDecimal(txtPrixDouzaine.Text.Replace("-", "0")),
                0D,
                chkGros.Checked,
                chkDemi.Checked,
                chkPiece.Checked,
                chkDouzaine.Checked,
                typesPersonnalisesActifs,
                LireContenuUnitePrincipaleEntree(),
                If(contenuSecondaire.HasValue, contenuSecondaire.Value, 0D),
                TypeGestionStockEntree(),
                ObtenirUniteSecondaireEntree())
            gridTypesVente.DataSource = Nothing
            gridTypesVente.DataSource = liste
            ConfigurerGrilleTypesVenteAffichage(gridTypesVente)
        End Sub

        Private Sub ConfigurerGrilleTypesVenteAffichage(grille As DataGridView)
            If grille Is Nothing Then
                Return
            End If

            If grille.Columns.Contains("TypeVenteProduitId") Then grille.Columns("TypeVenteProduitId").Visible = False
            If grille.Columns.Contains("Coefficient") Then grille.Columns("Coefficient").Visible = False
            If grille.Columns.Contains("EstPersonnalise") Then grille.Columns("EstPersonnalise").Visible = False
            If grille.Columns.Contains("ModePrix") Then grille.Columns("ModePrix").Visible = False
            If grille.Columns.Contains("Nom") Then grille.Columns("Nom").HeaderText = "Nom"
            If grille.Columns.Contains("QuantiteEquivalent") Then grille.Columns("QuantiteEquivalent").HeaderText = "Qté équiv."
            If grille.Columns.Contains("TypeUniteEquivalent") Then grille.Columns("TypeUniteEquivalent").HeaderText = "Unité"
            If grille.Columns.Contains("TypePrixAffichage") Then grille.Columns("TypePrixAffichage").HeaderText = "Coefficient / mode"
            If grille.Columns.Contains("PrixVente") Then grille.Columns("PrixVente").HeaderText = "Prix vente"
            If grille.Columns.Contains("Actif") Then grille.Columns("Actif").HeaderText = "Actif"
            If grille.Columns.Contains("NomAffichage") Then grille.Columns("NomAffichage").HeaderText = "Nom affiché"
        End Sub

        'Private Sub EnregistrerEntree(sender As Object, e As EventArgs)
        '    Try
        '        Dim libelle As String = txtNomProduit.Text.Trim()
        '        If libelle = "" Then
        '            MessageBox.Show("Nom produit requis.")
        '            Return
        '        End If

        '        Dim service As StockService = ObtenirStockService()
        '        Dim produitId As Integer = 0

        '        If chkProduitExistant.Checked Then
        '            produitId = Convert.ToInt32(cmbProduitExistant.SelectedValue)
        '        Else
        '            ' Création nouveau produit
        '            Dim p As New ProduitDTO With {
        '                .Libelle = libelle,
        '                .CategorieId = If(cmbCategorie.SelectedValue Is Nothing, 0, Convert.ToInt32(cmbCategorie.SelectedValue)),
        '                .UnitePrincipale = cmbUniteBase.Text,
        '                .ConversionUnite = LireDecimal(txtNbUniteParBase.Text),
        '                .PrixAchat = LireDecimal(txtPrixAchat.Text),
        '                .CoefficientGros = _coefficientCalcule,
        '                .VenteGros = chkGros.Checked,
        '                .VenteDemi = chkDemi.Checked,
        '                .VenteDetail = chkPiece.Checked,
        '                .VenteDouzaine = chkDouzaine.Checked,
        '                .PrixDetail = LireDecimal(txtPrixPiece.Text),
        '                .PrixQuart = LireDecimal(txtPrixQuart.Text)
        '            }
        '            ' Logique de création produit via repository (non fournie dans l'original mais implicite)
        '        End If

        '        Dim qte As Decimal = LireDecimal(txtQuantiteEntree.Text)
        '        If qte <= 0D Then
        '            MessageBox.Show("Quantité invalide.")
        '            Return
        '        End If

        '        service.EnregistrerEntree(produitId, qte, cmbUniteBase.Text, "ENTREE", txtObservationEntree.Text.Trim(), SessionUtilisateur.UtilisateurId)
        '        MessageBox.Show("Entrée enregistrée avec succès.")
        '        ChargerProduits()
        '        AfficherStockActuel()
        '    Catch ex As Exception
        '        MessageBox.Show("Erreur enregistrement: " & ex.Message)
        '    End Try
        'End Sub
        Private Sub EnregistrerEntree(sender As Object, e As EventArgs)
            Try
                If cmbUniteBase.SelectedIndex < 0 OrElse String.IsNullOrWhiteSpace(cmbUniteBase.Text) Then
                    MessageBox.Show("Sélectionnez l'unité de base avant d'enregistrer l'entrée.")
                    cmbUniteBase.Focus()
                    Return
                End If

                Dim quantitePrincipale As Decimal = LireDecimal(txtQuantiteEntree.Text)
                Dim quantiteSecondaire As Decimal = LireDecimal(txtQuantiteSecondaireEntree.Text)
                Dim conversionEntree As Decimal = LireDecimal(txtNbUniteParBase.Text)
                If LireContenuUnitePrincipaleEntree() <= 0D Then
                    MessageBox.Show("Le contenu de l'unité principale doit être supérieur à zéro.")
                    If EstGestionMesureEntree() Then
                        txtContenuUnitePrincipaleEntree.Focus()
                    Else
                        txtNbUniteParBase.Focus()
                    End If
                    Return
                End If
                Dim qte As Decimal = CalculerQuantiteBaseEntree(quantitePrincipale, quantiteSecondaire, conversionEntree)
                If qte <= 0D Then
                    MessageBox.Show("La quantité entrée doit être supérieure à zéro.")
                    txtQuantiteEntree.Focus()
                    Return
                End If

                Dim prixAchatVal As Decimal = LirePrixAchatEntreeEnCdf()
                If prixAchatVal <= 0D Then
                    MessageBox.Show("Le prix d'achat doit être renseigné et supérieur à zéro.")
                    txtPrixAchat.Focus()
                    Return
                End If

                Dim produitId As Integer
                Dim etaitProduitExistant As Boolean = chkProduitExistant.Checked
                Dim produitCapture As Produit = Nothing
                Dim produitNouvellementCree As Boolean = False
                If chkProduitExistant.Checked Then
                    If cmbProduitExistant.SelectedValue Is Nothing OrElse IsDBNull(cmbProduitExistant.SelectedValue) Then
                        MessageBox.Show("Selectionnez un produit.")
                        cmbProduitExistant.Focus()
                        Return
                    End If
                    produitId = Convert.ToInt32(cmbProduitExistant.SelectedValue)
                    produitCapture = ConstruireProduitMisAJourDepuisEntree(produitId)
                    If produitCapture Is Nothing Then
                        MessageBox.Show("Impossible de préparer les nouvelles valeurs du produit sélectionné.")
                        Return
                    End If
                Else
                    Dim nom As String = txtNomProduit.Text.Trim()
                    If nom = "" Then
                        MessageBox.Show("Nom produit obligatoire.")
                        txtNomProduit.Focus()
                        Return
                    End If
                    If ProduitNouveauExisteDeja(True) Then
                        Return
                    End If
                    If txtReference.Text.Trim() = "" Then
                        txtReference.Text = GenererReferenceUnique(nom, cmbCategorie.Text.Trim())
                    End If
                    If Not EstGestionMesureEntree() AndAlso LireDecimal(txtNbUniteParBase.Text) <= 0D Then
                        MessageBox.Show("Le nombre d'unités par base doit être supérieur à zéro.")
                        Return
                    End If
                    Dim prixAchatVal1 As Decimal = LirePrixAchatEntreeEnCdf()
                    Dim prixGrosVal As Decimal = LireDecimal(txtPrixGros.Text.Replace("-", "0"))
                    Dim prixDemiVal As Decimal = LireDecimal(txtPrixDemi.Text.Replace("-", "0"))
                    Dim prixQuartVal As Decimal = LireDecimal(txtPrixQuart.Text.Replace("-", "0"))
                    Dim prixPieceVal As Decimal = LireDecimal(txtPrixPiece.Text.Replace("-", "0"))
                    Dim prixDouzaineVal As Decimal = LireDecimal(txtPrixDouzaine.Text.Replace("-", "0"))

                    Dim produit As New Produit With {
                        .CodeBarres = txtReference.Text.Trim(),
                        .Libelle = nom,
                        .PrixAchat = prixAchatVal1,
                        .PrixGros = prixGrosVal,
                        .PrixDemi = prixDemiVal,
                        .PrixQuart = prixQuartVal,
                        .PrixDetail = prixPieceVal,
                        .PrixDouzaine = prixDouzaineVal,
                        .PrixSpecial = 0D,
                        .CoefficientGros = _coefficientCalcule,
                        .SeuilCritique = 0D,
                        .DateExpiration = Nothing,
                        .CategorieId = ObtenirCategorieSelectionneId(),
                        .UnitePrincipale = cmbUniteBase.Text,
                        .UniteSecondaire = "Piece",
                        .ConversionUnite = If(LireDecimal(txtNbUniteParBase.Text) > 0D, LireDecimal(txtNbUniteParBase.Text), LireContenuUnitePrincipaleEntree()),
                        .TypeGestionStock = TypeGestionStockEntree(),
                        .UniteMesureStock = If(EstGestionMesureEntree(), UniteMesureStockEntree(), "PIECE"),
                        .ContenuUnitePrincipale = LireContenuUnitePrincipaleEntree(),
                        .ContenuUniteSecondaire = LireContenuUniteSecondaireEntree(),
                        .EstActif = True,
                        .VenteDetail = chkPiece.Checked,
                        .VenteDemi = chkDemi.Checked,
                        .VenteDouzaine = chkDouzaine.Checked,
                        .VenteGros = chkGros.Checked
                    }
                    Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                    Dim dal As New DAL(cs)
                    Dim serviceProduit As New ProduitService(New ProduitRepository(dal))
                    produitId = serviceProduit.Ajouter(produit)
                    produitNouvellementCree = True
                End If

                _isSavingEntree = True
                If etaitProduitExistant Then
                    MettreAJourProduitExistantDepuisEntree(produitId, produitCapture)
                End If

                Dim service As StockService = ObtenirStockService()
                Dim contenuSecondaireDebug As Decimal? = LireContenuUniteSecondaireEntree()
                Dim stockAvant As Decimal = service.ObtenirStockActuelProduit(produitId)
                Dim stockApresAttendu As Decimal = stockAvant + qte
                Debug.WriteLine(String.Format(Globalization.CultureInfo.InvariantCulture,
                                              "[StockEntree] AVANT EnregistrerEntree Produit={0}; Mode={1}; StockAvant={2}; QteEntree={3}; ContenuPrincipal={4}; BonusSecondaire={5}; ContenuSecondaire={6}; StockAjoute={7}; StockApres={8}",
                                              produitId,
                                              TypeGestionStockEntree(),
                                              stockAvant,
                                              quantitePrincipale,
                                              LireContenuUnitePrincipaleEntree(),
                                              quantiteSecondaire,
                                              If(contenuSecondaireDebug.HasValue, contenuSecondaireDebug.Value, 0D),
                                              qte,
                                              stockApresAttendu))
                service.EnregistrerEntree(produitId, qte, ObtenirUniteSecondaireEntree(), txtReference.Text.Trim(), txtObservationEntree.Text.Trim(), SessionUtilisateur.UtilisateurId, prixAchatVal)
                Dim stockReluApresSauvegarde As Decimal = service.ObtenirStockActuelProduit(produitId)
                _stockActuelEntreeBase = stockReluApresSauvegarde
                Debug.WriteLine(String.Format(Globalization.CultureInfo.InvariantCulture,
                                              "[StockEntree] Stock réellement relu BDD après sauvegarde = {0}",
                                              stockReluApresSauvegarde))
                If etaitProduitExistant Then
                    EnregistrerTypesPersonnalisesTemporaires(produitId)
                End If
                _isSavingEntree = False

                ChargerProduits()
                If produitNouvellementCree Then
                    chkProduitExistant.Checked = True
                    cmbProduitExistant.SelectedValue = produitId
                End If
                MessageBox.Show("Entrée stock enregistrée.")
                NettoyerSaisieEntreeApresEnregistrement(produitId, etaitProduitExistant)
            Catch ex As Exception
                _isSavingEntree = False
                MessageBox.Show("Erreur entrée stock: " & ex.Message)
            End Try
        End Sub
        ' NOUVEAU: Sortie Manuelle
        Private Sub EnregistrerSortieManuelle(sender As Object, e As EventArgs)
            Try
                If cmbProduitExistant.SelectedValue Is Nothing Then
                    MessageBox.Show("Sélectionnez un produit.")
                    Return
                End If
                Dim qte As Decimal = LireDecimal(txtSortieManuelleQte.Text)
                If qte <= 0D Then
                    MessageBox.Show("Quantité invalide.")
                    Return
                End If

                Dim service As StockService = ObtenirStockService()
                Dim produitId As Integer = Convert.ToInt32(cmbProduitExistant.SelectedValue)


                ' Appel au service mis à jour
                service.EnregistrerSortieManuelle(produitId, qte, cmbUniteBase.Text, "Admin", txtSortieManuelleMotif.Text.Trim(), txtSortieManuelleClient.Text.Trim(), SessionUtilisateur.UtilisateurId)

                MessageBox.Show("Sortie manuelle enregistrée.")
                ' ChargerSortiesMois(Nothing, EventArgs.Empty)
                AfficherStockActuel()
            Catch ex As Exception
                MessageBox.Show("Erreur sortie manuelle: " & ex.Message)
            End Try
        End Sub

        Private Sub ValiderSortieManuelle(sender As Object, e As EventArgs)
            Try
                If _panier.Count = 0 Then
                    MessageBox.Show("Le panier est vide.")
                    Return
                End If

                Dim motifRow As DataRow = ObtenirMotifSelectionne()
                If motifRow Is Nothing Then
                    MessageBox.Show("Sélectionnez un motif.")
                    Return
                End If

                Dim motifId As Integer = Convert.ToInt32(motifRow("MotifId"))
                Dim motifLibelle As String = Convert.ToString(motifRow("Libelle"))
                Dim clientId As Integer? = Nothing
                Dim clientValue As Object = cmbSortieManuelleClient.SelectedValue
                Dim statutPaiement As String = "PAYE"
                Dim montantPaye As Decimal = CalculerTotalPanier()
                Dim resteAPayer As Decimal = 0D
                Dim observationSortie As String = txtDescriptionSortie.Text.Trim()

                If String.Equals(motifLibelle, "Dette Client", StringComparison.OrdinalIgnoreCase) Then
                    If clientValue Is Nothing OrElse IsDBNull(clientValue) Then
                        MessageBox.Show("Le client est obligatoire pour une dette client.")
                        Return
                    End If
                    clientId = Convert.ToInt32(clientValue)
                    statutPaiement = "IMPAYE"
                    montantPaye = 0D
                    resteAPayer = CalculerTotalPanier()
                ElseIf clientValue IsNot Nothing AndAlso Not IsDBNull(clientValue) AndAlso Not TypeOf clientValue Is DataRowView Then
                    clientId = Convert.ToInt32(clientValue)
                End If

                If String.Equals(motifLibelle, "Transfert marchandises", StringComparison.OrdinalIgnoreCase) Then
                    If cmbMagasinDestination.SelectedValue Is Nothing OrElse IsDBNull(cmbMagasinDestination.SelectedValue) OrElse String.IsNullOrWhiteSpace(cmbMagasinDestination.Text) Then
                        MessageBox.Show("Sélectionnez le magasin de destination pour le transfert.")
                        cmbMagasinDestination.Focus()
                        Return
                    End If

                    If String.IsNullOrWhiteSpace(observationSortie) Then
                        observationSortie = "Magasin destination: " & cmbMagasinDestination.Text.Trim()
                    Else
                        observationSortie &= " | Magasin destination: " & cmbMagasinDestination.Text.Trim()
                    End If
                End If

                Dim lignes As New List(Of StockSortie)()
                For Each l As PanierLigne In _panier
                    lignes.Add(New StockSortie With {
                        .ProduitId = l.ProduitId,
                        .QuantiteSaisie = l.Quantite,
                        .Unite = l.Unite,
                        .QuantiteBase = l.QuantiteBase,
                        .DateSortie = Date.Now,
                        .Source = "SORTIE_MANUELLE",
                        .RefSource = txtDescriptionSortie.Text.Trim(),
                        .CreePar = SessionUtilisateur.UtilisateurId,
                        .TypeVente = l.Unite,
                        .PrixUnitaire = l.PrixUnitaire,
                        .MontantLigne = l.Total,
                        .StatutPaiement = statutPaiement,
                        .MontantPaye = montantPaye,
                        .ResteAPayer = resteAPayer,
                        .Observation = observationSortie,
                        .ClientId = clientId,
                        .MotifId = motifId
                    })
                Next

                Dim service As StockService = ObtenirStockService()
                Dim numeroSortie As String = service.EnregistrerSortiesManuelles(lignes, motifId, clientId, statutPaiement, montantPaye, resteAPayer, observationSortie, SessionUtilisateur.UtilisateurId)

                _panier.Clear()
                RafraichirPanier()
                ChargerSortiesDuMois(Nothing, EventArgs.Empty)
                ChargerDettes(Nothing, EventArgs.Empty)
                ChargerDashboardSorties(Nothing, EventArgs.Empty)
                MessageBox.Show("Sortie enregistrée: " & numeroSortie)
            Catch ex As Exception
                MessageBox.Show("Erreur validation sortie: " & ex.Message)
            End Try
        End Sub

        'Private Sub EnregistrerSortie(sender As Object, e As EventArgs)
        '    Try
        '        If cmbProduitSortie.SelectedValue Is Nothing Then
        '            MessageBox.Show("Selectionnez un produit.")
        '            Return
        '        End If
        '        Dim qte As Decimal = LireDecimal(txtQuantiteSortie.Text)
        '        If qte <= 0D Then
        '            MessageBox.Show("Quantite invalide.")
        '            Return
        '        End If

        '        Dim produitId As Integer = Convert.ToInt32(cmbProduitSortie.SelectedValue)
        '        Dim typevente As String = Convert.ToString(cmbTypeVente.SelectedItem)
        '        Dim service As StockService = ObtenirStockService()
        '        service.EnregistrerSortie(produitId, qte, typevente, txtReferenceFacture.Text.Trim(), txtDescriptionSortie.Text.Trim(), SessionUtilisateur.UtilisateurId)
        '        MessageBox.Show("Sortie stock enregistrée.")
        '        RecalculerStockSortie(Nothing, EventArgs.Empty)
        '    Catch ex As Exception
        '        MessageBox.Show("Erreur sortie stock: " & ex.Message)
        '    End Try
        'End Sub
        ''Private Sub ChargerSortiesMois(sender As Object, e As EventArgs)
        '    Try
        '        Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
        '        Dim dal As New DAL(cs)
        '        Dim sql As String = "SELECT s.DateSortie, p.Libelle AS Produit, s.QuantiteSaisie AS Quantite, s.Unite, s.Motif, s.ClientInfo AS Client " &
        '                            "FROM StockSortie s INNER JOIN Produits p ON p.ProduitId = s.ProduitId " &
        '                            "WHERE s.DateSortie BETWEEN @Du AND @Au " &
        '                            "AND (p.Libelle LIKE @Search OR s.Motif LIKE @Search) ORDER BY s.DateSortie DESC"
        '        Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
        '            New System.Data.SqlClient.SqlParameter("@Du", dtpSortieDu.Value.Date),
        '            New System.Data.SqlClient.SqlParameter("@Au", dtpSortieAu.Value.Date),
        '            New System.Data.SqlClient.SqlParameter("@Search", "%" & txtRechercheSortie.Text.Trim() & "%")
        '        }
        '        gridSortieMois.DataSource = dal.ExecuterTable(sql, CommandType.Text, p)
        '    Catch
        '    End Try
        'End Sub

        'Private Sub ChargerMouvementsInventaire(sender As Object, e As EventArgs)
        '    If cmbProduitInventaire.SelectedValue Is Nothing Then Return
        '    Dim produitId As Integer = Convert.ToInt32(cmbProduitInventaire.SelectedValue)
        '    Try
        '        Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
        '        Dim dal As New DAL(cs)

        '        ' Entrées
        '        gridEntrees.DataSource = dal.ExecuterTable("SELECT DateEntree, QuantiteSaisie, Unite, Observation FROM StockEntree WHERE ProduitId=" & produitId & " ORDER BY DateEntree DESC", CommandType.Text)

        '        ' Sorties
        '        gridSorties.DataSource = dal.ExecuterTable("SELECT DateSortie, QuantiteSaisie, Unite, Motif FROM StockSortie WHERE ProduitId=" & produitId & " ORDER BY DateSortie DESC", CommandType.Text)

        '        ' Stock Théorique
        '        Dim service As StockService = ObtenirStockService()
        '        txtStockTheorique.Text = service.ObtenirStockActuelProduit(produitId).ToString("N0")

        '        ' NOUVEAU: Analyse Produit
        '        ChargerAnalyseProduit(produitId)
        '    Catch
        '    End Try
        'End Sub

        Private Sub ChargerInventaire(sender As Object, e As EventArgs)
            If cmbProduitInventaire.SelectedValue Is Nothing Then Return
            If cmbProduitInventaire.SelectedValue IsNot Nothing AndAlso Not TypeOf cmbProduitInventaire.SelectedValue Is DataRowView Then
                Dim produitId As Integer = Convert.ToInt32(cmbProduitInventaire.SelectedValue)
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)

                Dim dtEntree As DataTable = dal.ExecuterTable("SELECT DateEntree, QuantiteBase, PrixAchat FROM StockEntree WHERE ProduitId=@id ORDER BY DateEntree DESC", CommandType.Text, New List(Of System.Data.SqlClient.SqlParameter) From {New System.Data.SqlClient.SqlParameter("@id", produitId)})
                Dim dtSortie As DataTable = dal.ExecuterTable("SELECT DateSortie, QuantiteBase, Source FROM StockSortie WHERE ProduitId=@id ORDER BY DateSortie DESC", CommandType.Text, New List(Of System.Data.SqlClient.SqlParameter) From {New System.Data.SqlClient.SqlParameter("@id", produitId)})
                gridEntrees.DataSource = dtEntree
                gridSorties.DataSource = dtSortie

                Dim totalEntree As Object = dal.ExecuterScalaire("SELECT ISNULL(SUM(QuantiteBase),0) FROM StockEntree WHERE ProduitId=@id", CommandType.Text, New List(Of System.Data.SqlClient.SqlParameter) From {New System.Data.SqlClient.SqlParameter("@id", produitId)})
                Dim totalSortie As Object = dal.ExecuterScalaire("SELECT ISNULL(SUM(QuantiteBase),0) FROM StockSortie WHERE ProduitId=@id", CommandType.Text, New List(Of System.Data.SqlClient.SqlParameter) From {New System.Data.SqlClient.SqlParameter("@id", produitId)})
                Dim totalPerte As Object = dal.ExecuterScalaire("SELECT ISNULL(SUM(QuantiteBase),0) FROM StockPerte WHERE ProduitId=@id", CommandType.Text, New List(Of System.Data.SqlClient.SqlParameter) From {New System.Data.SqlClient.SqlParameter("@id", produitId)})

                Dim stockTheo As Decimal = Convert.ToDecimal(totalEntree) - Convert.ToDecimal(totalSortie) - Convert.ToDecimal(totalPerte)
                txtStockTheorique.Text = stockTheo.ToString("N0")
                RecalculerEcart(Nothing, EventArgs.Empty)
                ' NOUVEAU: Analyse Produit
                ChargerAnalyseProduit(produitId)
            End If
        End Sub

        Private Sub ChargerDettes(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim sql As String = "" &
                    "SELECT ss.NumeroSortie, MAX(ss.DateSortie) AS DateSortie, ISNULL(c.NomClient, '') AS Client, " &
                    "ISNULL(m.Libelle, ss.Source) AS Motif, SUM(ISNULL(ss.MontantLigne, 0)) AS Total, " &
                    "MAX(ISNULL(ss.MontantPaye, 0)) AS MontantPaye, MAX(ISNULL(ss.ResteAPayer, 0)) AS ResteAPayer, " &
                    "MAX(ISNULL(ss.StatutPaiement, 'IMPAYE')) AS StatutPaiement " &
                    "FROM StockSortie ss " &
                    "LEFT JOIN Clients c ON c.ClientId = ss.ClientId " &
                    "LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId " &
                    "WHERE ss.StatutPaiement = 'IMPAYE' " &
                    "GROUP BY ss.NumeroSortie, ISNULL(c.NomClient, ''), ISNULL(m.Libelle, ss.Source) " &
                    "ORDER BY MAX(ss.DateSortie) DESC"
                gridDettes.DataSource = dal.ExecuterTable(sql, CommandType.Text, Nothing)
            Catch ex As Exception
                MessageBox.Show("Erreur chargement dettes: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerDashboardSorties(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim sql As String = "" &
                    "SELECT TOP 20 ss.NumeroSortie, MAX(ss.DateSortie) AS DateSortie, ISNULL(c.NomClient, '') AS Client, " &
                    "ISNULL(m.Libelle, ss.Source) AS Motif, SUM(ISNULL(ss.MontantLigne, 0)) AS Total, " &
                    "MAX(ISNULL(ss.StatutPaiement, '')) AS StatutPaiement " &
                    "FROM StockSortie ss " &
                    "LEFT JOIN Clients c ON c.ClientId = ss.ClientId " &
                    "LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId " &
                    "GROUP BY ss.NumeroSortie, ISNULL(c.NomClient, ''), ISNULL(m.Libelle, ss.Source) " &
                    "ORDER BY MAX(ss.DateSortie) DESC"
                gridDetailsDashboard.DataSource = dal.ExecuterTable(sql, CommandType.Text, Nothing)

                pnlKpi.Controls.Clear()
                Dim totalSorties As Object = dal.ExecuterScalaire("SELECT COUNT(*) FROM StockSortie", CommandType.Text, Nothing)
                Dim totalImpayes As Object = dal.ExecuterScalaire("SELECT COUNT(*) FROM StockSortie WHERE StatutPaiement='IMPAYE'", CommandType.Text, Nothing)
                Dim montantImpaye As Object = dal.ExecuterScalaire("SELECT ISNULL(SUM(ResteAPayer),0) FROM StockSortie WHERE StatutPaiement='IMPAYE'", CommandType.Text, Nothing)
                pnlKpi.Controls.Add(New Label() With {.AutoSize = True, .Text = "Sorties: " & Convert.ToString(totalSorties), .Padding = New Padding(10)})
                pnlKpi.Controls.Add(New Label() With {.AutoSize = True, .Text = "Impayées: " & Convert.ToString(totalImpayes), .Padding = New Padding(10)})
                pnlKpi.Controls.Add(New Label() With {.AutoSize = True, .Text = "Reste à payer: " & Convert.ToString(montantImpaye), .Padding = New Padding(10)})
            Catch ex As Exception
                MessageBox.Show("Erreur dashboard sorties: " & ex.Message)
            End Try
        End Sub

        Private Sub EnregistrerPaiementDette(sender As Object, e As EventArgs)
            Try
                If gridDettes.CurrentRow Is Nothing Then
                    MessageBox.Show("Selectionnez une dette.")
                    Return
                End If

                Dim numeroSortie As String = LireCellString(gridDettes.CurrentRow.Cells("NumeroSortie"))
                Dim resteAPayer As Decimal = LireCellDecimal(gridDettes.CurrentRow.Cells("ResteAPayer"))
                If resteAPayer <= 0D Then
                    MessageBox.Show("Cette dette est déjà réglée.")
                    Return
                End If

                Dim saisie As String = Interaction.InputBox("Montant à enregistrer pour la dette " & numeroSortie & " :", "Paiement dette", resteAPayer.ToString("N0"))
                If String.IsNullOrWhiteSpace(saisie) Then Return

                Dim montant As Decimal
                If Not Decimal.TryParse(saisie.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, montant) Then
                    If Not Decimal.TryParse(saisie, montant) Then
                        MessageBox.Show("Montant invalide.")
                        Return
                    End If
                End If
                If montant <= 0D Then
                    MessageBox.Show("Montant invalide.")
                    Return
                End If

                Dim service As StockService = ObtenirStockService()
                Dim nouveauReste As Decimal = service.EnregistrerPaiementSortieManuelle(numeroSortie, montant, SessionUtilisateur.UtilisateurId)

                ChargerDettes(Nothing, EventArgs.Empty)
                ChargerDashboardSorties(Nothing, EventArgs.Empty)
                ChargerSortiesDuMois(Nothing, EventArgs.Empty)

                MessageBox.Show("Paiement enregistré. Reste à payer: " & FormatageGlobal.FormatMontant(nouveauReste))
            Catch ex As Exception
                MessageBox.Show("Erreur paiement dette: " & ex.Message)
            End Try
        End Sub

        Private Sub ImprimerTicketDette(sender As Object, e As EventArgs)
            Try
                If gridDettes.CurrentRow Is Nothing Then
                    MessageBox.Show("Selectionnez une dette.")
                    Return
                End If

                Dim numeroSortie As String = LireCellString(gridDettes.CurrentRow.Cells("NumeroSortie"))
                If String.IsNullOrWhiteSpace(numeroSortie) Then
                    MessageBox.Show("Sortie invalide.")
                    Return
                End If

                Dim service As StockService = ObtenirStockService()
                Dim dt As DataTable = service.ListerSortieManuelleParNumero(numeroSortie)
                If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                    MessageBox.Show("Aucune ligne trouvée pour cette sortie.")
                    Return
                End If

                Dim ticket As New DebtTicketData With {
                    .NumeroSortie = numeroSortie,
                    .Client = LireCellString(gridDettes.CurrentRow.Cells("Client")),
                    .Motif = LireCellString(gridDettes.CurrentRow.Cells("Motif")),
                    .DateSortie = If(gridDettes.CurrentRow.Cells("DateSortie").Value Is Nothing OrElse IsDBNull(gridDettes.CurrentRow.Cells("DateSortie").Value), Date.Now, Convert.ToDateTime(gridDettes.CurrentRow.Cells("DateSortie").Value)),
                    .Total = LireCellDecimal(gridDettes.CurrentRow.Cells("Total")),
                    .MontantPaye = LireCellDecimal(gridDettes.CurrentRow.Cells("MontantPaye")),
                    .ResteAPayer = LireCellDecimal(gridDettes.CurrentRow.Cells("ResteAPayer")),
                    .StatutPaiement = LireCellString(gridDettes.CurrentRow.Cells("StatutPaiement")),
                    .Lignes = dt
                }

                Dim doc As New Printing.PrintDocument()
                If _parametres IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(_parametres.ImprimanteTicket) Then
                    doc.PrinterSettings.PrinterName = _parametres.ImprimanteTicket
                End If
                AddHandler doc.PrintPage, Sub(s, ev) ImprimerPageDette(ev, ticket)

                If _parametres IsNot Nothing AndAlso _parametres.ApercuAvantImpression Then
                    Dim preview As New PrintPreviewDialog() With {.Document = doc, .Width = 900, .Height = 700}
                    preview.ShowDialog()
                Else
                    doc.Print()
                End If
            Catch ex As Exception
                MessageBox.Show("Erreur impression ticket dette: " & ex.Message)
            End Try
        End Sub

        Private Class DebtTicketData
            Public Property NumeroSortie As String
            Public Property Client As String
            Public Property Motif As String
            Public Property DateSortie As Date
            Public Property Total As Decimal
            Public Property MontantPaye As Decimal
            Public Property ResteAPayer As Decimal
            Public Property StatutPaiement As String
            Public Property Lignes As DataTable
        End Class

        Private Function LireCellString(cell As DataGridViewCell) As String
            If cell Is Nothing OrElse cell.Value Is Nothing OrElse IsDBNull(cell.Value) Then
                Return ""
            End If
            Return Convert.ToString(cell.Value)
        End Function

        Private Function LireCellDecimal(cell As DataGridViewCell) As Decimal
            If cell Is Nothing OrElse cell.Value Is Nothing OrElse IsDBNull(cell.Value) Then
                Return 0D
            End If
            Return Convert.ToDecimal(cell.Value)
        End Function

        Private Sub ImprimerPageDette(e As Printing.PrintPageEventArgs, ticket As DebtTicketData)
            Dim y As Integer = 10
            Dim titre As String = If(_parametres Is Nothing OrElse String.IsNullOrWhiteSpace(_parametres.NomMagasin), "MAGASIN", _parametres.NomMagasin)
            e.Graphics.DrawString(titre, New Font("Segoe UI", 10, FontStyle.Bold), Brushes.Black, 10, y)
            y += 14
            If _parametres IsNot Nothing Then
                If Not String.IsNullOrWhiteSpace(_parametres.AdresseMagasin) Then
                    e.Graphics.DrawString(_parametres.AdresseMagasin, New Font("Segoe UI", 7), Brushes.Black, 10, y)
                    y += 12
                End If
                If Not String.IsNullOrWhiteSpace(_parametres.TelephoneMagasin) Then
                    e.Graphics.DrawString(_parametres.TelephoneMagasin, New Font("Segoe UI", 7), Brushes.Black, 10, y)
                    y += 12
                End If
            End If

            e.Graphics.DrawString("------------------------", New Font("Segoe UI", 7), Brushes.Black, 10, y)
            y += 12
            e.Graphics.DrawString("Sortie : " & ticket.NumeroSortie, New Font("Segoe UI", 7), Brushes.Black, 10, y)
            y += 12
            e.Graphics.DrawString("Date : " & ticket.DateSortie.ToString("dd/MM/yyyy HH:mm"), New Font("Segoe UI", 7), Brushes.Black, 10, y)
            y += 12
            If Not String.IsNullOrWhiteSpace(ticket.Client) Then
                e.Graphics.DrawString("Client : " & ticket.Client, New Font("Segoe UI", 7), Brushes.Black, 10, y)
                y += 12
            End If
            If Not String.IsNullOrWhiteSpace(ticket.Motif) Then
                e.Graphics.DrawString("Motif : " & ticket.Motif, New Font("Segoe UI", 7), Brushes.Black, 10, y)
                y += 12
            End If

            e.Graphics.DrawString("------------------------", New Font("Segoe UI", 7), Brushes.Black, 10, y)
            y += 12
            For Each row As DataRow In ticket.Lignes.Rows
                Dim libelle As String = Convert.ToString(row("Produit"))
                Dim qte As String = Convert.ToDecimal(row("QuantiteSaisie")).ToString("N0")
                Dim unite As String = Convert.ToString(row("Unite"))
                Dim totalLigne As String = Convert.ToDecimal(row("MontantLigne")).ToString("N0")
                e.Graphics.DrawString(libelle & "  " & qte & " " & unite, New Font("Segoe UI", 7), Brushes.Black, 10, y)
                y += 12
                e.Graphics.DrawString("   = " & totalLigne & " FC", New Font("Segoe UI", 7), Brushes.Black, 10, y)
                y += 12
            Next

            e.Graphics.DrawString("------------------------", New Font("Segoe UI", 7), Brushes.Black, 10, y)
            y += 12
            e.Graphics.DrawString("Total : " & ticket.Total.ToString("N0") & " FC", New Font("Segoe UI", 8, FontStyle.Bold), Brushes.Black, 10, y)
            y += 12
            e.Graphics.DrawString("Payé : " & ticket.MontantPaye.ToString("N0") & " FC", New Font("Segoe UI", 7), Brushes.Black, 10, y)
            y += 12
            e.Graphics.DrawString("Reste : " & ticket.ResteAPayer.ToString("N0") & " FC", New Font("Segoe UI", 7), Brushes.Black, 10, y)
            y += 12
            e.Graphics.DrawString("Statut : " & If(String.IsNullOrWhiteSpace(ticket.StatutPaiement), "IMPAYE", ticket.StatutPaiement), New Font("Segoe UI", 7), Brushes.Black, 10, y)
            y += 12
            e.Graphics.DrawString("Merci pour votre visite", New Font("Segoe UI", 7), Brushes.Black, 10, y)
        End Sub

        ' NOUVEAU: Analyse Produit
        Private Sub ChargerAnalyseProduit(produitId As Integer)
            Try
                Dim service As StockService = ObtenirStockService()
                Dim analyse As DataTable = service.ObtenirAnalyseProduit(produitId)
                If analyse IsNot Nothing AndAlso analyse.Rows.Count > 0 Then
                    Dim row As DataRow = analyse.Rows(0)
                    Dim totalEntrees As Decimal = LireDecimalTable(row, "TotalEntrees")
                    Dim totalVentes As Decimal = LireDecimalTable(row, "TotalVentes")
                    Dim totalSortiesManuelles As Decimal = LireDecimalTable(row, "TotalSortiesManuelles")
                    Dim totalPertes As Decimal = LireDecimalTable(row, "TotalPertes")
                    Dim totalGros As Decimal = LireDecimalTable(row, "TotalGros")
                    Dim totalDemi As Decimal = LireDecimalTable(row, "TotalDemi")
                    Dim totalQuart As Decimal = LireDecimalTable(row, "TotalQuart")
                    Dim totalPiece As Decimal = LireDecimalTable(row, "TotalPiece")
                    Dim totalDouzaine As Decimal = LireDecimalTable(row, "TotalDouzaine")
                    Dim totalDons As Decimal = LireDecimalTable(row, "TotalDons")
                    Dim totalAllocations As Decimal = LireDecimalTable(row, "TotalAllocations")
                    Dim totalDettesClients As Decimal = LireDecimalTable(row, "TotalDettesClients")
                    Dim totalDettesBoss As Decimal = LireDecimalTable(row, "TotalDettesBoss")
                    Dim totalSortiesHorsCaisse As Decimal = LireDecimalTable(row, "TotalSortiesHorsCaisse")
                    Dim stockReel As Decimal = LireDecimalTable(row, "StockReelRestant")
                    Dim VenteCarton As Decimal = LireDecimalTable(row, "TotalVenteCartons")
                    Dim VentePiece As Decimal = LireDecimalTable(row, "ResteVentePieces")
                    Dim montantTotalGenere As Decimal = LireDecimalTable(row, "MontantTotalGenere")
                    Dim analyseMesuree As Boolean = StockUnitConversionService.EstGestionMesuree(If(row.Table.Columns.Contains("TypeGestionStock") AndAlso Not row.IsNull("TypeGestionStock"), Convert.ToString(row("TypeGestionStock")), "UNITE"))
                    Dim libelleVentes As String = If(analyseMesuree,
                        "Ventes commerciales: " & FormatageGlobal.FormatNombre(totalVentes),
                        "Vente en cartons: " & FormatageGlobal.FormatNombre(VenteCarton) & "C + " & FormatageGlobal.FormatNombre(VentePiece) & "P")
                    Dim stockLisible As String = FormatageGlobal.FormatStockSelonGestion(
                        stockReel,
                        LireDecimalTable(row, "ConversionUnite"),
                        If(row.Table.Columns.Contains("UnitePrincipale") AndAlso Not row.IsNull("UnitePrincipale"), Convert.ToString(row("UnitePrincipale")), String.Empty),
                        If(row.Table.Columns.Contains("UniteSecondaire") AndAlso Not row.IsNull("UniteSecondaire"), Convert.ToString(row("UniteSecondaire")), String.Empty),
                        If(row.Table.Columns.Contains("TypeGestionStock") AndAlso Not row.IsNull("TypeGestionStock"), Convert.ToString(row("TypeGestionStock")), "UNITE"),
                        If(row.Table.Columns.Contains("UniteMesureStock") AndAlso Not row.IsNull("UniteMesureStock"), Convert.ToString(row("UniteMesureStock")), String.Empty),
                        LireDecimalTable(row, "ContenuUnitePrincipale"),
                        LireDecimalTable(row, "ContenuUniteSecondaire"))

                    lblAnalyseSortieGros.Text = "Entrées: " & FormatageGlobal.FormatNombre(totalEntrees) & " | Ventes: " & FormatageGlobal.FormatNombre(totalVentes) & vbCrLf &
                     libelleVentes & vbCrLf
                    lblAnalyseSortiePiece.Text = "Sorties manuelles: " & FormatageGlobal.FormatNombre(totalSortiesManuelles) & " | Pertes: " & FormatageGlobal.FormatNombre(totalPertes)
                    lblAnalyseRestantGros.Text = "Dons: " & FormatageGlobal.FormatNombre(totalDons) & " | Allocations: " & FormatageGlobal.FormatNombre(totalAllocations)
                    lblAnalyseRestantPiece.Text = "Dettes client: " & FormatageGlobal.FormatNombre(totalDettesClients) & " | Dettes boss: " & FormatageGlobal.FormatNombre(totalDettesBoss) & " | Hors caisse: " & FormatageGlobal.FormatNombre(totalSortiesHorsCaisse)
                    lblAnalyseRealisation.Text = "G:" & FormatageGlobal.FormatNombre(totalGros) &
                        " D:" & FormatageGlobal.FormatNombre(totalDemi) &
                        " Q:" & FormatageGlobal.FormatNombre(totalQuart) &
                        " P:" & FormatageGlobal.FormatNombre(totalPiece) &
                        " Dz:" & FormatageGlobal.FormatNombre(totalDouzaine) & vbCrLf &
                        " | Stock réel: " & stockLisible &
                        " | Mnt: " & FormatageGlobal.FormatMontant(montantTotalGenere)
                Else
                    lblAnalyseSortieGros.Text = "Entrées: 0 | Ventes: 0"
                    lblAnalyseSortiePiece.Text = "Sorties manuelles: 0 | Pertes: 0"
                    lblAnalyseRestantGros.Text = "Dons: 0 | Allocations: 0"
                    lblAnalyseRestantPiece.Text = "Dettes client: 0 | Dettes boss: 0 | Hors caisse: 0"
                    lblAnalyseRealisation.Text = "G:0 D:0 Q:0 P:0 Dz:0 | Stock réel: 0 | Mnt: 0 FC"
                End If
            Catch
            End Try
        End Sub

        'Private Sub CalculerEcart(sender As Object, e As EventArgs)
        '    Dim theo As Decimal = LireDecimal(txtStockTheorique.Text)
        '    Dim reel As Decimal = LireDecimal(txtStockReel.Text)
        '    txtEcart.Text = (reel - theo).ToString("N0")
        'End Sub

        Private Sub RecalculerEcart(sender As Object, e As EventArgs)
            Dim reel As Decimal = LireDecimal(txtStockReel.Text)
            Dim theo As Decimal = LireDecimal(txtStockTheorique.Text)
            Dim ecart As Decimal = reel - theo
            txtEcart.Text = ecart.ToString("N0")
        End Sub
        'Private Sub ValiderInventaire(sender As Object, e As EventArgs)
        '    ' Logique de validation inventaire originale
        'End Sub

        Private Sub ChargerAlertes(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim paramService As New ParametreService(New ParametreRepository(dal))
                Dim p As ParametreDTO = paramService.Charger()

                Dim seuil As Decimal = If(p Is Nothing, 0D, p.SeuilStockCritique)
                Dim jours As Integer = If(p Is Nothing, 30, p.AlerteExpirationJours)

                Dim sql As String = "SELECT p.ProduitId, p.Libelle, ISNULL(s.QuantiteStock,0) AS QuantiteStock, p.SeuilCritique, p.DateExpiration " &
                                    "FROM Produits p LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId " &
                                    "WHERE ISNULL(s.QuantiteStock,0) <= @s OR ISNULL(s.QuantiteStock,0) <= 0 " &
                                    "OR (p.DateExpiration IS NOT NULL AND p.DateExpiration <= DATEADD(DAY, @j, CAST(GETDATE() AS DATE)))"
                Dim prms As New List(Of System.Data.SqlClient.SqlParameter) From {
                    New System.Data.SqlClient.SqlParameter("@s", seuil),
                    New System.Data.SqlClient.SqlParameter("@j", jours)
                }
                gridAlertes.DataSource = dal.ExecuterTable(sql, CommandType.Text, prms)
            Catch ex As Exception
                MessageBox.Show("Erreur alertes: " & ex.Message)
            End Try
        End Sub

        Private Sub ValiderInventaire(sender As Object, e As EventArgs)
            Try
                MessageBox.Show("La validation d'inventaire a été déplacée vers FrmInventaireIntelligent.", "Inventaire", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show("Erreur inventaire: " & ex.Message)
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
                ChargerProduits()
                ChargerSortiesDuMois(Nothing, EventArgs.Empty)
                ChargerDettes(Nothing, EventArgs.Empty)
                ChargerDashboardSorties(Nothing, EventArgs.Empty)
                ChargerAlertes(Nothing, EventArgs.Empty)
                If cmbProduitInventaire.SelectedValue IsNot Nothing Then
                    ChargerInventaire(Nothing, EventArgs.Empty)
                End If
                RafraichirResumeTypesPersonnalisesEntree()
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireStock", "RafraichirDepuisEvenement", "Erreur lors du rafraichissement automatique du stock.", ex)
            Finally
                _isRefreshingFromEvent = False
            End Try
        End Sub
        Private Sub EnregistrerPerte(sender As Object, e As EventArgs)
            Try
                If cmbProduitPerte.SelectedValue Is Nothing Then
                    MessageBox.Show("Selectionnez un produit.")
                    Return
                End If
                Dim qte As Decimal = LireDecimal(txtQuantitePerte.Text)
                If qte <= 0D Then
                    MessageBox.Show("Quantite invalide.")
                    Return
                End If

                Dim produitId As Integer = Convert.ToInt32(cmbProduitPerte.SelectedValue)
                Dim typeP As String = If(cmbTypePerte.SelectedItem Is Nothing, "", cmbTypePerte.SelectedItem.ToString())
                Dim service As StockService = ObtenirStockService()
                service.EnregistrerPerte(produitId, qte, "base", "PERTE", txtObservationPerte.Text.Trim(), typeP, SessionUtilisateur.UtilisateurId)
                MessageBox.Show("Perte enregistrée.")
            Catch ex As Exception
                MessageBox.Show("Erreur perte: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerRapportEntrees(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim sql As String = "" &
                    "SELECT se.DateEntree, se.IdStock AS ReferenceStock, p.Libelle AS Produit, se.QuantiteSaisie AS QuantiteEntree, " &
                    "stock.StockApresEntree, se.PrixAchat, p.PrixGros, " &
                    "CASE WHEN se.PrixAchat > 0 THEN ROUND(((p.PrixGros / se.PrixAchat) - 1) * 100, 2) ELSE 0 END AS MargePourcent, " &
                    "ISNULL(se.Devise, 'CDF') AS Devise " &
                    "FROM StockEntree se " &
                    "INNER JOIN Produits p ON p.ProduitId = se.ProduitId " &
                    "OUTER APPLY (" &
                    "   SELECT SUM(se2.QuantiteBase) - " &
                    "          ISNULL((SELECT SUM(ss.QuantiteBase) FROM StockSortie ss WHERE ss.ProduitId = se.ProduitId AND ss.DateSortie <= se.DateEntree), 0) - " &
                    "          ISNULL((SELECT SUM(sp.QuantiteBase) FROM StockPerte sp WHERE sp.ProduitId = se.ProduitId AND sp.DatePerte <= se.DateEntree), 0) AS StockApresEntree " &
                    "   FROM StockEntree se2 WHERE se2.ProduitId = se.ProduitId AND se2.DateEntree <= se.DateEntree" &
                    ") stock " &
                    "WHERE CAST(se.DateEntree AS DATE) BETWEEN @Du AND @Au " &
                    "ORDER BY se.DateEntree DESC"
                Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                    New System.Data.SqlClient.SqlParameter("@Du", dtpRapportDu.Value.Date),
                    New System.Data.SqlClient.SqlParameter("@Au", dtpRapportAu.Value.Date)
                }
                _rapportEntreesTable = dal.ExecuterTable(sql, CommandType.Text, p)
                gridRapportEntrees.DataSource = _rapportEntreesTable
            Catch ex As Exception
                MessageBox.Show("Erreur rapport entrées: " & ex.Message)
            End Try
        End Sub

        Private Sub ImprimerRapportEntrees(sender As Object, e As EventArgs)
            Try
                If gridRapportEntrees.DataSource Is Nothing Then
                    ChargerRapportEntrees(Nothing, EventArgs.Empty)
                End If

                Dim dt As DataTable = TryCast(gridRapportEntrees.DataSource, DataTable)
                If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                    MessageBox.Show("Aucune ligne à imprimer pour la période sélectionnée.", "Rapport des entrées", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If

                Dim doc As New PrintDocument()
                _parametres = PrintConfigurationHelper.ConfigurerDocumentA4(doc, Me, "FormulaireStock", "ImprimerRapportEntrees", True)
                _rapportEntreesPrintRowIndex = 0
                _rapportEntreesPrintPageIndex = 1
                AddHandler doc.PrintPage, AddressOf ImprimerPageRapportEntrees

                Dim preview As New PrintPreviewDialog()
                preview.Document = doc
                preview.Width = 1000
                preview.Height = 700
                preview.ShowDialog()
            Catch ex As Exception
                MessageBox.Show("Erreur impression rapport: " & ex.Message)
            End Try
        End Sub

        Private Sub ImprimerPageRapportEntrees(sender As Object, e As PrintPageEventArgs)
            Dim g As Graphics = e.Graphics
            Dim fontTitre As New Font("Segoe UI", 14, FontStyle.Bold)
            Dim fontTexte As New Font("Segoe UI", 9, FontStyle.Regular)
            Dim y As Integer = 40

            g.DrawString(If(_parametres IsNot Nothing AndAlso _parametres.NomMagasin <> "", _parametres.NomMagasin, "Entreprise"), fontTitre, Brushes.Black, 40, y)
            y += 28
            g.DrawString(If(_parametres IsNot Nothing, _parametres.AdresseMagasin, ""), fontTexte, Brushes.Black, 40, y)
            y += 18
            g.DrawString(If(_parametres IsNot Nothing, _parametres.TelephoneMagasin, ""), fontTexte, Brushes.Black, 40, y)
            y += 28
            g.DrawString("Rapport des entrées du " & dtpRapportDu.Value.ToString("dd/MM/yyyy") & " au " & dtpRapportAu.Value.ToString("dd/MM/yyyy"), fontTexte, Brushes.Black, 40, y)
            y += 30

            Dim x As Integer = 40
            Dim widths As Integer() = {90, 120, 180, 80, 80, 80, 80, 80, 70}
            Dim headers As String() = {"Date", "Ref stock", "Produit", "Qté entrée", "Stock après", "Prix achat", "Prix gros", "Marge %", "Devise"}

            For i As Integer = 0 To headers.Length - 1
                g.DrawRectangle(Pens.Black, x, y, widths(i), 24)
                g.DrawString(headers(i), fontTexte, Brushes.Black, x + 2, y + 4)
                x += widths(i)
            Next
            y += 26

            If _rapportEntreesTable Is Nothing OrElse _rapportEntreesTable.Rows.Count = 0 Then
                e.HasMorePages = False
                Return
            End If

            Dim lignesImprimees As Integer = 0
            For i As Integer = _rapportEntreesPrintRowIndex To _rapportEntreesTable.Rows.Count - 1
                Dim row As DataRow = _rapportEntreesTable.Rows(i)
                x = 40
                Dim values As String() = {
                    Convert.ToDateTime(row("DateEntree")).ToString("dd/MM/yyyy"),
                    Convert.ToString(row("ReferenceStock")),
                    Convert.ToString(row("Produit")),
                    Convert.ToDecimal(row("QuantiteEntree")).ToString("N0"),
                    Convert.ToDecimal(row("StockApresEntree")).ToString("N0"),
                    Convert.ToDecimal(row("PrixAchat")).ToString("N0"),
                    Convert.ToDecimal(row("PrixGros")).ToString("N0"),
                    FormaterMargePourcent(row("MargePourcent")),
                    Convert.ToString(row("Devise"))
                }
                For j As Integer = 0 To values.Length - 1
                    g.DrawRectangle(Pens.Gray, x, y, widths(j), 22)
                    g.DrawString(values(j), fontTexte, Brushes.Black, x + 2, y + 4)
                    x += widths(j)
                Next
                y += 22
                lignesImprimees += 1
                If y > e.MarginBounds.Bottom - 40 Then
                    g.DrawString("Page " & _rapportEntreesPrintPageIndex.ToString(), fontTexte, Brushes.Black, e.MarginBounds.Right - 70, e.MarginBounds.Bottom + 10)
                    e.HasMorePages = lignesImprimees > 0
                    If e.HasMorePages Then
                        _rapportEntreesPrintRowIndex = i + 1
                        _rapportEntreesPrintPageIndex += 1
                    End If
                    Return
                End If
            Next

            g.DrawString("Page " & _rapportEntreesPrintPageIndex.ToString(), fontTexte, Brushes.Black, e.MarginBounds.Right - 70, e.MarginBounds.Bottom + 10)
            _rapportEntreesPrintRowIndex = 0
            _rapportEntreesPrintPageIndex = 1
            e.HasMorePages = False
        End Sub

        Private Sub FormaterCelluleRapportEntrees(sender As Object, e As DataGridViewCellFormattingEventArgs)
            If e.RowIndex < 0 OrElse Not gridRapportEntrees.Columns.Contains("MargePourcent") Then
                Return
            End If

            If String.Equals(gridRapportEntrees.Columns(e.ColumnIndex).Name, "MargePourcent", StringComparison.OrdinalIgnoreCase) Then
                e.Value = FormaterMargePourcent(e.Value)
                e.FormattingApplied = True
            End If
        End Sub

        Private Function FormaterMargePourcent(valeur As Object) As String
            If valeur Is Nothing OrElse Convert.IsDBNull(valeur) Then
                Return String.Empty
            End If

            Dim marge As Decimal = Convert.ToDecimal(valeur)
            Return marge.ToString("0.##") & " %"
        End Function
        'Private Sub EnregistrerPerte(sender As Object, e As EventArgs)
        '    ' Logique de perte originale
        'End Sub

        'Private Sub ChargerRapportEntrees(sender As Object, e As EventArgs)
        '    ' Logique de rapport originale
        'End Sub

        'Private Sub ImprimerRapportEntrees(sender As Object, e As EventArgs)
        '    ' Logique d'impression originale
        'End Sub

        ' --- UTILS ---
        Private Function ObtenirStockService() As StockService
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Return New StockService(dal)
        End Function

        Private Function TenterLireCoefficient(saisie As String, ByRef coefficient As Decimal, ByRef marge As Decimal) As Boolean
            coefficient = 0D
            marge = 0D

            Dim saisieNormalisee As String = If(saisie, String.Empty).Trim()
            Dim texte As String = saisieNormalisee.Replace("%", String.Empty)
            If texte = String.Empty Then
                Return False
            End If

            Dim valeur As Decimal
            If Not Decimal.TryParse(texte.Replace(".", ","), NumberStyles.Number, CultureInfo.CurrentCulture, valeur) AndAlso
               Not Decimal.TryParse(texte.Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, valeur) Then
                Return False
            End If

            Dim aSeparateurDecimal As Boolean = saisieNormalisee.Contains(".") OrElse saisieNormalisee.Contains(",")
            Dim traiterCommeCoefficientDirect As Boolean = aSeparateurDecimal AndAlso valeur >= 1.01D AndAlso valeur <= 9.99D

            If Not traiterCommeCoefficientDirect Then
                marge = valeur
                coefficient = 1D + (marge / 100D)
            Else
                coefficient = valeur
                marge = (coefficient - 1D) * 100D
            End If

            Return coefficient > 0D
        End Function

        Private Function LireDecimal(texte As String) As Decimal
            Dim v As Decimal
            Dim valeur As String = If(texte, String.Empty).Trim()
            If valeur = String.Empty Then
                Return 0D
            End If
            If Decimal.TryParse(valeur, NumberStyles.Number, CultureInfo.CurrentCulture, v) Then Return v
            If Decimal.TryParse(valeur.Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, v) Then Return v
            Return 0D
        End Function

        Private Function LireDecimalTable(row As DataRow, colonne As String) As Decimal
            If row Is Nothing OrElse row.Table Is Nothing OrElse Not row.Table.Columns.Contains(colonne) OrElse row.IsNull(colonne) Then
                Return 0D
            End If
            Return Convert.ToDecimal(row(colonne))
        End Function

        Private Function ExtraireDecimal(texte As String) As Decimal
            If texte Is Nothing Then Return 0D
            Dim parts As String() = texte.Split(" "c)
            For Each p As String In parts
                Dim v As Decimal
                If Decimal.TryParse(p.Replace(":", ""), v) Then Return v
            Next
            Return 0D
        End Function

        Private Function GenererReference(libelle As String, categorieId As String) As String
            Dim cat As String = If(categorieId = "", "GEN", "CAT" & categorieId)
            Dim initials As String = ""
            For Each part As String In libelle.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
                initials &= part.Substring(0, 1).ToUpper()
                If initials.Length >= 3 Then Exit For
            Next
            If initials = "" Then initials = "PRD"
            Return cat & "-" & Date.Now.ToString("yyyyMMdd") & "-" & initials
        End Function

        Private Function GenererReferenceUnique(libelle As String, categorieId As String) As String
            Return GenererReference(libelle, categorieId) & "-" & Date.Now.ToString("HHmmss")
        End Function

        Private Class FormulaireMagasinRapide
            Inherits Form

            Private ReadOnly txtNom As TextBox
            Private ReadOnly txtAdresse As TextBox

            Public ReadOnly Property NomMagasin As String
                Get
                    Return txtNom.Text.Trim()
                End Get
            End Property

            Public ReadOnly Property AdresseMagasin As String
                Get
                    Return txtAdresse.Text.Trim()
                End Get
            End Property

            Public Sub New()
                Text = "Ajouter un magasin"
                FormBorderStyle = FormBorderStyle.FixedDialog
                StartPosition = FormStartPosition.CenterParent
                MinimizeBox = False
                MaximizeBox = False
                Width = 420
                Height = 220
                BackColor = Color.White

                Dim lblNom As New Label() With {.Text = "Nom magasin", .Left = 20, .Top = 20, .AutoSize = True}
                txtNom = New TextBox() With {.Left = 20, .Top = 42, .Width = 360}
                Dim lblAdresse As New Label() With {.Text = "Adresse", .Left = 20, .Top = 78, .AutoSize = True}
                txtAdresse = New TextBox() With {.Left = 20, .Top = 100, .Width = 360}
                Dim btnAnnuler As New Button() With {.Text = "Annuler", .Left = 210, .Top = 140, .Width = 80}
                Dim btnEnregistrer As New Button() With {.Text = "Enregistrer", .Left = 300, .Top = 140, .Width = 80}

                AddHandler btnEnregistrer.Click,
                    Sub(sender As Object, e As EventArgs)
                        If String.IsNullOrWhiteSpace(txtNom.Text) Then
                            MessageBox.Show("Le nom du magasin est obligatoire.")
                            txtNom.Focus()
                            Return
                        End If

                        DialogResult = DialogResult.OK
                        Close()
                    End Sub
                AddHandler btnAnnuler.Click,
                    Sub(sender As Object, e As EventArgs)
                        DialogResult = DialogResult.Cancel
                        Close()
                    End Sub

                Controls.AddRange(New Control() {lblNom, txtNom, lblAdresse, txtAdresse, btnAnnuler, btnEnregistrer})
            End Sub
        End Class

        ' Précharge l'onglet d'entrée stock à partir d'un bon d'approvisionnement.
        Public Sub PrechargerDepuisBonApprovisionnement(bonId As Integer)
            Try
                tabs.SelectedIndex = 0
                chkProduitExistant.Checked = True

                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim sql As String = "SELECT TOP 1 b.NumeroBon,P.Libelle, l.ProduitId, l.Quantite, l.PrixAchat " &
                                    "FROM BonApprovisionnementLignes l " &
                                    "INNER JOIN BonsApprovisionnement b ON b.BonId = l.BonId " &
                                     "INNER Join Produits p ON l.ProduitId = p.ProduitId " &
                                    "WHERE l.BonId=@BonId ORDER BY l.BonLigneId"

                Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                    New System.Data.SqlClient.SqlParameter("@BonId", bonId)
                }
                Dim dt As DataTable = dal.ExecuterTable(sql, CommandType.Text, p)
                If dt.Rows.Count = 0 Then
                    Return
                End If

                Dim row As DataRow = dt.Rows(0)
                cmbProduitExistant.SelectedValue = Convert.ToInt32(row("ProduitId"))
                cmbProduitExistant.Text = Convert.ToString(row("Libelle"))
                txtNomProduit.Text = Convert.ToString(row("Libelle"))
                txtQuantiteEntree.Text = Convert.ToDecimal(row("Quantite")).ToString("N0")
                txtPrixAchat.Text = Convert.ToDecimal(row("PrixAchat")).ToString("N0")
                txtReference.Text = Convert.ToString(row("NumeroBon"))
                txtObservationEntree.Text = "Réception depuis " & Convert.ToString(row("NumeroBon"))
                AfficherStockActuel()
                RecalculerStock(Nothing, EventArgs.Empty)
            Catch
                ' N'interrompt pas l'ouverture de l'écran si la précharge échoue.
            End Try
        End Sub

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            RemoveHandler AppEvents.StockModifie, AddressOf RafraichirDepuisEvenement
            RemoveHandler AppEvents.ProduitModifie, AddressOf RafraichirDepuisEvenement
            MyBase.OnFormClosed(e)
        End Sub
    End Class
End Namespace
