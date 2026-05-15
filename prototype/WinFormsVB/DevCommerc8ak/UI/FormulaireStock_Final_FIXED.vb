Option Strict On
Option Explicit On

Imports System
Imports Microsoft.VisualBasic
Imports System.Configuration
Imports System.Data
Imports System.Collections.Generic
Imports System.Drawing
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
        Private ReadOnly txtQuantiteEntree As TextBox
        Private ReadOnly lblStockActuel As Label
        Private ReadOnly lblStockActuelPiece As Label
        Private ReadOnly lblStockApres As Label
        Private ReadOnly lblStockApresPiece As Label
        Private ReadOnly txtPrixAchat As TextBox
        Private ReadOnly cmbDevise As ComboBox
        Private ReadOnly txtTaux As TextBox
        Private ReadOnly txtCoefficientInput As TextBox
        Private ReadOnly txtCoefficientDetail As TextBox
        Private ReadOnly lblTypeCoefficient As Label
        Private ReadOnly lblMargeCalculee As Label
        Private ReadOnly txtPrixGros As TextBox
        Private ReadOnly txtPrixDemi As TextBox
        Private ReadOnly txtPrixQuart As TextBox
        Private ReadOnly txtPrixPiece As TextBox
        Private ReadOnly txtPrixDouzaine As TextBox
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
        Private _coefficientCalcule As Decimal
        Private _coefficientDetailCalcule As Decimal
        Private _parametres As ParametreDTO
        Private ReadOnly _typeVenteService As TypeVenteService
        Private _typesVenteCourants As List(Of TypeVenteDTO) 'nouveau 
        Private ReadOnly _panier As List(Of PanierLigne)

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
            _typeVenteService = New TypeVenteService()
            _typesVenteCourants = New List(Of TypeVenteDTO)()
            _panier = New List(Of PanierLigne)()
            ' Main Layout
            Dim mainLayout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2}
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
            Dim tabSortieManuelle As New TabPage("SORTIE MANUELLE") With {.BackColor = ColorBackground, .AutoScroll = True}
            Dim tabDettes As New TabPage("DETTES & CRÉANCES") With {.BackColor = ColorBackground, .AutoScroll = True}
            Dim tabDashboardSorties As New TabPage("DASHBOARD SORTIES") With {.BackColor = ColorBackground, .AutoScroll = True}

            tabs.TabPages.AddRange(New TabPage() {tabEntree, tabSortie, tabSortieManuelle, tabDettes, tabInventaire, tabAlertes, tabPerte, tabRapportEntrees, tabDashboardSorties})

            ' --- TAB ENTREE DESIGN ---
            Dim layoutEntree As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(10), .ColumnCount = 1, .RowCount = 2}
            layoutEntree.RowStyles.Add(New RowStyle(SizeType.Absolute, 520)) ' Infos Produit / Unité/ Card produit
            layoutEntree.RowStyles.Add(New RowStyle(SizeType.Absolute, 80)) 'Card Finance / Prix
            'tabEntree.Controls.Add(layoutEntree)

            Dim mainTableEntree As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 3, .Padding = New Padding(5)}
            mainTableEntree.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            mainTableEntree.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            mainTableEntree.RowStyles.Add(New RowStyle(SizeType.Absolute, 180)) ' Infos Produit / Unité/ Card produit
            mainTableEntree.RowStyles.Add(New RowStyle(SizeType.Absolute, 220)) 'Card Finance / Prix
            mainTableEntree.RowStyles.Add(New RowStyle(SizeType.Absolute, 60)) ' Options / Autres
            ' mainTableEntree.RowStyles.Add(New RowStyle(SizeType.Absolute, 80))  ' Bouton

            ' Card 1: Produit
            Dim cardProduit As Panel = CreateCard(600, 180, "INFORMATIONS PRODUIT")
            chkProduitExistant = New CheckBox() With {.Text = "Produit existant", .Left = 20, .Top = 45, .AutoSize = True, .Checked = True}
            cmbProduitExistant = New ComboBox() With {.Left = 160, .Top = 42, .Width = 250, .DropDownStyle = ComboBoxStyle.DropDownList}
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
            Dim cardUnite As Panel = CreateCard(600, 180, "UNITÉ & CONVERSION")
            cmbUniteBase = New ComboBox() With {.Left = 150, .Top = 45, .Width = 150, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbUniteBase.Items.AddRange(New Object() {"Carton", "Sac", "Pack", "Paquet", "Bidon", "Sachet", "Kg", "Piece"})
            txtNbUniteParBase = New TextBox() With {.Left = 150, .Top = 75, .Width = 100}
            txtQuantiteEntree = New TextBox() With {.Left = 150, .Top = 105, .Width = 100}
            lblStockActuel = New Label() With {.Left = 330, .Top = 45, .AutoSize = True, .ForeColor = ColorSecondary}
            lblStockActuelPiece = New Label() With {.Left = 330, .Top = 65, .AutoSize = True}
            lblStockApres = New Label() With {.Left = 330, .Top = 85, .AutoSize = True, .ForeColor = ColorAccent}
            lblStockApresPiece = New Label() With {.Left = 330, .Top = 105, .AutoSize = True}
            cardUnite.Controls.AddRange(New Control() {
                New Label() With {.Text = "Unité base", .Left = 20, .Top = 48, .AutoSize = True},
                New Label() With {.Text = "Nb unités/base", .Left = 20, .Top = 78, .AutoSize = True},
                New Label() With {.Text = "Quantité entrée", .Left = 20, .Top = 108, .AutoSize = True},
                cmbUniteBase, txtNbUniteParBase, txtQuantiteEntree, lblStockActuel, lblStockActuelPiece, lblStockApres, lblStockApresPiece
            })
            'layoutEntree.Controls.Add(cardUnite)

            ' Card 3: Finance
            Dim cardFinance As Panel = CreateCard(600, 200, "INFORMATIONS FINANCIÈRES")
            txtPrixAchat = New TextBox() With {.Left = 150, .Top = 45, .Width = 120}
            cmbDevise = New ComboBox() With {.Left = 280, .Top = 45, .Width = 70, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbDevise.Items.AddRange(New Object() {"CDF", "USD"})
            cmbDevise.SelectedIndex = 0
            txtTaux = New TextBox() With {.Left = 360, .Top = 45, .Width = 80, .ReadOnly = True}
            txtCoefficientInput = New TextBox() With {.Left = 150, .Top = 75, .Width = 120}
            txtCoefficientDetail = New TextBox() With {.Left = 150, .Top = 135, .Width = 120}
            lblTypeCoefficient = New Label() With {.Left = 280, .Top = 78, .AutoSize = True}
            lblMargeCalculee = New Label() With {.Left = 150, .Top = 105, .AutoSize = True, .ForeColor = ColorAccent}
            cardFinance.Controls.AddRange(New Control() {
                New Label() With {.Text = "Prix Achat", .Left = 20, .Top = 48, .AutoSize = True},
                New Label() With {.Text = "Coeff. Gros", .Left = 20, .Top = 78, .AutoSize = True},
                New Label() With {.Text = "Coeff. Détail", .Left = 20, .Top = 138, .AutoSize = True},
                txtPrixAchat, cmbDevise, txtTaux, txtCoefficientInput, txtCoefficientDetail, lblTypeCoefficient, lblMargeCalculee
            })
            'layoutEntree.Controls.Add(cardFinance)

            ' Card 4: Prix Vente
            Dim cardPrix As Panel = CreateCard(600, 350, "PRIX DE VENTE CALCULÉS")
            txtPrixGros = New TextBox() With {.Left = 150, .Top = 45, .Width = 120, .ReadOnly = True}
            txtPrixDemi = New TextBox() With {.Left = 150, .Top = 75, .Width = 120, .ReadOnly = True}
            txtPrixQuart = New TextBox() With {.Left = 150, .Top = 105, .Width = 120, .ReadOnly = True}
            txtPrixPiece = New TextBox() With {.Left = 150, .Top = 135, .Width = 120, .ReadOnly = True}
            txtPrixDouzaine = New TextBox() With {.Left = 150, .Top = 165, .Width = 120, .ReadOnly = True, .Visible = True}
            chkGros = New CheckBox() With {.Text = "Gros", .Left = 20, .Top = 45, .AutoSize = True, .Checked = True}
            chkDemi = New CheckBox() With {.Text = "Demi", .Left = 20, .Top = 75, .AutoSize = True}
            chkQuart = New CheckBox() With {.Text = "Quart", .Left = 20, .Top = 105, .AutoSize = True}
            chkPiece = New CheckBox() With {.Text = "Pièce", .Left = 20, .Top = 135, .AutoSize = True, .Checked = True}
            chkDouzaine = New CheckBox() With {.Text = "Douzaine", .Left = 20, .Top = 165, .AutoSize = True, .Visible = True}
            cardPrix.Controls.AddRange(New Control() {
                chkGros, chkDemi, chkQuart, chkPiece, chkDouzaine,
                txtPrixGros, txtPrixDemi, txtPrixQuart, txtPrixPiece, txtPrixDouzaine
            })
            ' layoutEntree.Controls.Add(cardPrix)

            ' Card 5: Validation
            Dim cardValidation As Panel = CreateCard(1220, 100, "VALIDATION")
            dtpDateEntree = New DateTimePicker() With {.Left = 150, .Top = 45, .Width = 150}
            txtObservationEntree = New TextBox() With {.Left = 450, .Top = 45, .Width = 400}
            btnEnregistrerEntree = New Button() With {
                .Text = "ENREGISTRER L'ENTRÉE",
                .Left = 900, .Top = 35,
                .Width = 280, .Height = 45,
                .BackColor = ColorAccent,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat
            }

            cardValidation.Controls.AddRange(New Control() {
                New Label() With {.Text = "Date Entrée", .Left = 20, .Top = 48, .AutoSize = True},
                New Label() With {.Text = "Observation", .Left = 330, .Top = 48, .AutoSize = True},
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
            Dim layoutSortie As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(20), .ColumnCount = 1, .RowCount = 3}
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



            ' ------------------- SORTIE -------------------
            'Dim grpSortie As New GroupBox() With {.Text = "Stock Sortie", .Left = 10, .Top = 10, .Width = 1200, .Height = 220, .Anchor = AnchorStyles.Top Or AnchorStyles.Left}
            ' = New TextBox() With {.Left = 160, .Top = 25, .Width = 180}
            'cmbProduitSortie = New ComboBox() With {.Left = 160, .Top = 55, .Width = 240, .DropDownStyle = ComboBoxStyle.DropDownList}
            'dtpDateSortie = New DateTimePicker() With {.Left = 160, .Top = 85, .Width = 140, .Format = DateTimePickerFormat.Short}
            'txtQuantiteSortie = New TextBox() With {.Left = 160, .Top = 115, .Width = 120}
            'txtStockRestant = New TextBox() With {.Left = 160, .Top = 145, .Width = 120, .ReadOnly = True}
            'cmbTypeVente = New ComboBox() With {.Left = 160, .Top = 175, .Width = 160, .DropDownStyle = ComboBoxStyle.DropDownList}
            'cmbTypeVente.Items.AddRange(New Object() {"Gros", "Demi", "Quart", "Piece", "Douzaine"})
            'txtDescriptionSortie = New TextBox() With {.Left = 430, .Top = 25, .Width = 700}

            'grpSortie.Controls.Add(New Label() With {.Text = "Ref facture", .Left = 20, .Top = 28, .AutoSize = True})
            'grpSortie.Controls.Add(New Label() With {.Text = "Produit", .Left = 20, .Top = 58, .AutoSize = True})
            'grpSortie.Controls.Add(New Label() With {.Text = "Date", .Left = 20, .Top = 88, .AutoSize = True})
            'grpSortie.Controls.Add(New Label() With {.Text = "Quantité", .Left = 20, .Top = 118, .AutoSize = True})
            'grpSortie.Controls.Add(New Label() With {.Text = "Stock restant", .Left = 20, .Top = 148, .AutoSize = True})
            'grpSortie.Controls.Add(New Label() With {.Text = "Type vente", .Left = 20, .Top = 178, .AutoSize = True})
            'grpSortie.Controls.Add(New Label() With {.Text = "Description", .Left = 360, .Top = 28, .AutoSize = True})


            'btnEnregistrerSortie = New Button() With {.Text = "Enregistrer sortie", .Left = 10, .Top = 240, .Width = 160, .BackColor = Color.LightSalmon}
            'tabSortie.Controls.Add(grpSortie)
            'tabSortie.Controls.Add(btnEnregistrerSortie)




            ' NOUVEAU: Sortie Manuelle
            Dim cardSortieManuelle As Panel = CreateCard(1200, 280, " SORTIE MANUELLE (HORS-VENTE)")
            txtReferenceFacture = New TextBox() With {.Left = 160, .Top = 45, .Width = 180,
                .Enabled = False, .BackColor = ColorWhite,
                .BorderStyle = BorderStyle.FixedSingle,
                .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                .TextAlign = HorizontalAlignment.Center
            }

            ' cmbProduitSortie = New ComboBox() With {.Left = 160, .Top = 75, .Width = 240, .DropDownStyle = ComboBoxStyle.DropDownList}
            dtpDateSortie = New DateTimePicker() With {.Left = 160, .Top = 105, .Width = 140, .Format = DateTimePickerFormat.Short}
            ' txtQuantiteSortie = New TextBox() With {.Left = 160, .Top = 135, .Width = 120}
            ' txtStockRestant = New TextBox() With {.Left = 160, .Top = 165, .Width = 120, .ReadOnly = True}


            ' cmbTypeVente = New ComboBox() With {.Left = 160, .Top = 165, .Width = 160, .DropDownStyle = ComboBoxStyle.DropDownList}
            lblPrixProd = New Label() With {.Left = 320, .Top = 165, .AutoSize = True, .ForeColor = ColorSecondary}
            lblStock = New Label() With {.Left = 530, .Top = 120, .AutoSize = True, .ForeColor = ColorSecondary, .Font = New Font("Segoe UI", 9, FontStyle.Italic)}
            lblEquivalent = New Label() With {.Left = 530, .Top = 140, .AutoSize = True, .ForeColor = ColorDanger, .Font = New Font("Segoe UI", 9, FontStyle.Italic)}
            lblTotalReel = New Label() With {.Left = 530, .Top = 162, .AutoSize = True, .ForeColor = ColorAccent, .Font = New Font("Segoe UI", 9, FontStyle.Italic)}



            'lblStock = New Label() With {.Left = 20, .Top = 410, .AutoSize = True, .ForeColor = ColorDanger, .Font = New Font("Segoe UI", 9, FontStyle.Italic)}
            'lblEquivalent = New Label() With {.Left = 20, .Top = 432, .AutoSize = True, .ForeColor = ColorDanger, .Font = New Font("Segoe UI", 9, FontStyle.Italic)} '#########nouveau
            'lblTotalReel = New Label() With {.Left = 20, .Top = 454, .AutoSize = True, .ForeColor = ColorDanger, .Font = New Font("Segoe UI", 9, FontStyle.Italic)} '########### nouveau
            '' lblStockApresPieceS = New Label() With {.Left = 330, .Top = 105, .AutoSize = True}

            cmbSortieManuelleMotif = New ComboBox() With {.Left = 160, .Top = 195, .Width = 160, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbSortieManuelleMotif.Items.AddRange(New Object() {"Dettes Client", "Demande Patron", "Don", "Dettes Patron", "Colis Noel", "Colis Nouvel ans"})
            cmbSortieManuelleClient = New ComboBox() With {.Left = 530, .Top = 45, .Width = 240, .DropDownStyle = ComboBoxStyle.DropDownList}
            lblQteAchter = New Label() With {.Left = 890, .Top = 38, .AutoSize = True, .BackColor = ColorSuccess, .ForeColor = Color.White, .Font = New Font("Segoe UI Variable Display", 9.5F, FontStyle.Bold)}
            lblSMontantAchat = New Label() With {.Left = 1030, .Top = 38, .AutoSize = True, .BackColor = ColorPrimary, .ForeColor = Color.White, .Font = New Font("Segoe UI Variable Display", 9.5F, FontStyle.Bold)}
            lblSMoyenneAchat = New Label() With {.Left = 890, .Top = 56, .AutoSize = True, .BackColor = ColorSecondary, .ForeColor = Color.White, .Font = New Font("Segoe UI Variable Display", 9.5F, FontStyle.Bold)}
            txtDescriptionSortie = New TextBox() With {.Left = 530, .Top = 75, .Width = 500, .Height = 500}


            'txtSortieManuelleMotif = New TextBox() With {.Left = 250, .Top = 45, .Width = 250}
            ' txtSortieManuelleClient = New TextBox() With {.Left = 580, .Top = 45, .Width = 200}
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
            layoutSortie.Controls.Add(cardSortieManuelle, 0, 1)

            gridSortieMois = CreateStyledGrid()
            layoutSortie.Controls.Add(gridSortieMois, 0, 2)



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

            Dim lblC As New Label() With {.Text = "Client (Optionnel):", .Location = New Point(530, 20), .AutoSize = True}
            cmbSortieManuelleClient = New ComboBox() With {.Location = New Point(530, 40), .Width = 200, .DropDownStyle = ComboBoxStyle.DropDownList}
            Dim lblQte As New Label() With {.Text = "Qte deja Acheter", .Left = 780, .Top = 30, .AutoSize = True}
            Dim lblMont As New Label() With {.Text = "Montant Global", .Left = 920, .Top = 30, .AutoSize = True}
            Dim lblMoyenne As New Label() With {.Text = "Moyenne Achat", .Left = 780, .Top = 48, .AutoSize = True}
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

            pnlSaisie.Controls.AddRange({lblP, cmbProduitSortie, lblQ, txtQuantiteSortie, lblM, cmbMotif, lblC, cmbSortieManuelleClient, lblQte, lblMont, lblMoyenne, lblQteAchter, lblSMontantAchat, lblSMoyenneAchat, lblTypeVente, cmbTypeVente, lblPrixProd, btnAjouter, btnVider, lblSousTotal, lblTotal, btnValider, lblStock, lblEquivalent, lblTotalReel})


            ' Grille du Panier
            gridPanier = CreateStyledGrid()
            'gridPanier.Columns.Add("Produit", "Produit")
            'gridPanier.Columns.Add("Qte", "Quantité")
            'gridPanier.Columns.Add("Unite", "Unité")
            'gridPanier.Columns.Add("Prix", "Prix Unitaire")
            'gridPanier.Columns.Add("Total", "Sous-Total")

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
            tabDettes.Controls.Add(mainLayoutDette)

            ' --- TAB INVENTAIRE DESIGN ---
            Dim layoutInventaire As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(20), .ColumnCount = 2, .RowCount = 3}
            layoutInventaire.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 60))
            layoutInventaire.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 40))
            layoutInventaire.RowStyles.Add(New RowStyle(SizeType.Absolute, 150))
            layoutInventaire.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
            layoutInventaire.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
            tabInventaire.Controls.Add(layoutInventaire)

            ' Saisie Inventaire
            Dim cardSaisieInv As Panel = CreateCard(700, 130, "SAISIE INVENTAIRE")
            cmbProduitInventaire = New ComboBox() With {.Left = 120, .Top = 45, .Width = 250, .DropDownStyle = ComboBoxStyle.DropDownList}
            txtStockTheorique = New TextBox() With {.Left = 120, .Top = 75, .Width = 100, .ReadOnly = True}
            txtStockReel = New TextBox() With {.Left = 120, .Top = 105, .Width = 100}
            txtEcart = New TextBox() With {.Left = 300, .Top = 105, .Width = 100, .ReadOnly = True}
            btnValiderInventaire = New Button() With {.Text = "Valider Inventaire", .Left = 450, .Top = 95, .Width = 200, .Height = 40, .BackColor = ColorAccent, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            cardSaisieInv.Controls.AddRange(New Control() {
                New Label() With {.Text = "Produit:", .Left = 20, .Top = 48, .AutoSize = True}, cmbProduitInventaire,
                New Label() With {.Text = "Théorique:", .Left = 20, .Top = 78, .AutoSize = True}, txtStockTheorique,
                New Label() With {.Text = "Réel:", .Left = 20, .Top = 108, .AutoSize = True}, txtStockReel,
                New Label() With {.Text = "Ecart:", .Left = 240, .Top = 108, .AutoSize = True}, txtEcart,
                btnValiderInventaire
            })
            layoutInventaire.Controls.Add(cardSaisieInv, 0, 0)

            ' NOUVEAU: Analyse Inventaire
            Dim cardAnalyse As Panel = CreateCard(450, 130, "NOUVEAU: ANALYSE PRODUIT")
            lblAnalyseSortieGros = New Label() With {.Text = "Sorties Gros: 0", .Left = 20, .Top = 45, .AutoSize = True}
            lblAnalyseSortiePiece = New Label() With {.Text = "Sorties Pièces: 0", .Left = 20, .Top = 65, .AutoSize = True}
            lblAnalyseRestantGros = New Label() With {.Text = "Restant Gros: 0", .Left = 220, .Top = 45, .AutoSize = True, .ForeColor = ColorSecondary}
            lblAnalyseRestantPiece = New Label() With {.Text = "Restant Pièces: 0", .Left = 220, .Top = 65, .AutoSize = True, .ForeColor = ColorSecondary}
            lblAnalyseRealisation = New Label() With {.Text = "Réalisation: 0.00", .Left = 20, .Top = 95, .AutoSize = True, .Font = FontBold, .ForeColor = ColorAccent}
            cardAnalyse.Controls.AddRange(New Control() {lblAnalyseSortieGros, lblAnalyseSortiePiece, lblAnalyseRestantGros, lblAnalyseRestantPiece, lblAnalyseRealisation})
            layoutInventaire.Controls.Add(cardAnalyse, 1, 0)

            gridEntrees = CreateStyledGrid()
            gridSorties = CreateStyledGrid()
            layoutInventaire.Controls.Add(gridEntrees, 0, 1)
            layoutInventaire.Controls.Add(gridSorties, 0, 2)
            layoutInventaire.SetColumnSpan(gridEntrees, 2)
            layoutInventaire.SetColumnSpan(gridSorties, 2)

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
            tabDashboardSorties.Controls.Add(mainLayoutDASH)



            ' --- EVENT HANDLERS ---
            AddHandler Me.Load, AddressOf FormulaireStock_Load
            AddHandler chkProduitExistant.CheckedChanged, AddressOf BasculerProduitExistant
            AddHandler cmbProduitExistant.SelectedIndexChanged, AddressOf ChargerProduitSelection
            AddHandler txtNomProduit.TextChanged, AddressOf GenererReferenceAutomatique
            AddHandler cmbCategorie.SelectedIndexChanged, AddressOf GenererReferenceAutomatique
            AddHandler txtNbUniteParBase.TextChanged, AddressOf RecalculerStock
            AddHandler txtQuantiteEntree.TextChanged, AddressOf RecalculerStock
            AddHandler txtPrixAchat.TextChanged, AddressOf RecalculerPrixAuto
            AddHandler txtCoefficientInput.TextChanged, AddressOf CoefficientInputChange
            AddHandler txtCoefficientDetail.TextChanged, AddressOf CoefficientDetailChange
            AddHandler chkGros.CheckedChanged, AddressOf RecalculerPrixAuto
            AddHandler chkDemi.CheckedChanged, AddressOf RecalculerPrixAuto
            AddHandler chkQuart.CheckedChanged, AddressOf RecalculerPrixAuto
            AddHandler chkPiece.CheckedChanged, AddressOf RecalculerPrixAuto
            AddHandler chkDouzaine.CheckedChanged, AddressOf RecalculerPrixAuto
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

            AddHandler cmbProduitInventaire.SelectedIndexChanged, AddressOf ChargerInventaire
            AddHandler txtStockReel.TextChanged, AddressOf RecalculerEcart
            AddHandler btnValiderInventaire.Click, AddressOf ValiderInventaire
            AddHandler btnRafraichirAlertes.Click, AddressOf ChargerAlertes
            AddHandler btnEnregistrerPerte.Click, AddressOf EnregistrerPerte
            AddHandler btnChargerRapportEntrees.Click, AddressOf ChargerRapportEntrees
            AddHandler btnImprimerRapportEntrees.Click, AddressOf ImprimerRapportEntrees

            AddHandler btnAjouter.Click, AddressOf AjouterAuPanier
            ' AddHandler btnVider.Click, AddressOf RetirerDuPanier
            AddHandler btnVider.Click, AddressOf RetirerDuPanier

            ' NOUVEAU: Handlers
            AddHandler btnEnregistrerSortie.Click, AddressOf EnregistrerSortieManuelle
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
                'ChargerCategories()
                ChargerProduits()
                ChargerParametres()
                ChargerClientsActifs()
                ' ChargerSortiesMois(Nothing, EventArgs.Empty)
                ChargerSortiesDuMois(Nothing, EventArgs.Empty)
                ChargerAlertes(Nothing, EventArgs.Empty)
                BasculerProduitExistant(Nothing, EventArgs.Empty)
                RafraichirTypesVente()
            Catch ex As Exception
                MessageBox.Show("Erreur chargement: " & ex.Message)
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
                cmbSortieManuelleClient.DataSource = dal.ExecuterTable(sql, CommandType.Text, Nothing)
                cmbSortieManuelleClient.DisplayMember = "NomClient"
                cmbSortieManuelleClient.ValueMember = "ClientId"

            Catch ex As Exception
                MessageBox.Show("Erreur clients actifs: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerInfoAchatClientSelection(sender As Object, e As EventArgs)
            If cmbSortieManuelleClient.SelectedValue Is Nothing Then Return
            Dim row As DataRowView = TryCast(cmbSortieManuelleClient.SelectedItem, DataRowView)
            If row Is Nothing Then Return
            Dim r As DataRow = row.Row
            lblQteAchter.Text = If(r.IsNull("NbAchats"), "", Convert.ToString(row("NbAchats")))
            lblSMontantAchat.Text = If(r.IsNull("TotalAchats"), "", Convert.ToString(row("TotalAchats")) & "FC")
            lblSMoyenneAchat.Text = If(r.IsNull("MoyenneAchat"), "", Convert.ToString(row("MoyenneAchat")) & "FC")



        End Sub

        Private Sub ChargerProduits()
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New ProduitRepository(dal)
            _produitsTable = repo.ListerTable()

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

            cmbCategorie.Items.Clear()
            For Each row As DataRow In _produitsTable.Rows
                If Not row.IsNull("CategorieId") Then
                    Dim v As String = Convert.ToString(row("CategorieId"))
                    If Not cmbCategorie.Items.Contains(v) Then
                        cmbCategorie.Items.Add(v)
                    End If
                End If
            Next

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
            Catch
            End Try
        End Sub

        Private Sub BasculerProduitExistant(sender As Object, e As EventArgs)
            Dim existant As Boolean = chkProduitExistant.Checked
            cmbProduitExistant.Enabled = existant
            txtNomProduit.Enabled = Not existant
            cmbCategorie.Enabled = Not existant

            If existant Then
                ChargerProduitSelection(Nothing, EventArgs.Empty)
            Else
                txtNomProduit.Text = ""
                cmbCategorie.Text = ""
                txtReference.Text = ""
                lblStockActuel.Text = "Stock actuel: 0"
                lblStockActuelPiece.Text = "Equivalent: 0 pièce"
                lblStockApres.Text = "Stock après: 0"
                lblStockApresPiece.Text = "Après: 0 pièce"
                txtNbUniteParBase.Clear()
                txtCoefficientDetail.Clear()
                txtCoefficientInput.Clear()
                txtPrixAchat.Clear()
                txtPrixDemi.Clear()
                txtPrixDouzaine.Clear()
                txtPrixGros.Clear()
                txtPrixPiece.Clear()
                txtPrixQuart.Clear()
                txtObservationEntree.Clear()
                chkDemi.Checked = False
                chkDouzaine.Checked = False
                chkQuart.Checked = False
                chkGros.Checked = True
                chkPiece.Checked = True
            End If
            RafraichirTypesVente()
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

        Private Sub ChargerProduitSelection(sender As Object, e As EventArgs)
            If cmbProduitExistant.SelectedValue Is Nothing Then Return
            Dim row As DataRowView = TryCast(cmbProduitExistant.SelectedItem, DataRowView)
            If row Is Nothing Then Return
            Dim r As DataRow = row.Row
            txtNomProduit.Text = Convert.ToString(row("Libelle"))
            cmbCategorie.Text = If(r.IsNull("CategorieId"), "", Convert.ToString(row("CategorieId")))
            txtReference.Text = GenererReference(Convert.ToString(row("Libelle")), cmbCategorie.Text)

            cmbUniteBase.Text = If(r.IsNull("UnitePrincipale"), "", Convert.ToString(row("UnitePrincipale")))
            txtNbUniteParBase.Text = If(r.IsNull("ConversionUnite"), "", Convert.ToDecimal(row("ConversionUnite")).ToString())
            txtPrixAchat.Text = If(r.IsNull("PrixAchat"), "", Convert.ToDecimal(row("PrixAchat")).ToString())
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
            RecalculerStock(Nothing, EventArgs.Empty)
            RecalculerPrixAuto(Nothing, EventArgs.Empty)
        End Sub

        Private Sub AfficherStockActuel()
            If cmbProduitExistant.SelectedValue IsNot Nothing AndAlso Not TypeOf cmbProduitExistant.SelectedValue Is DataRowView Then
                Dim produitId As Integer = Convert.ToInt32(cmbProduitExistant.SelectedValue)
                Dim service As StockService = ObtenirStockService()
                Dim stockPieces As Decimal = service.ObtenirStockActuelProduit(produitId)
                Dim nb As Decimal = LireDecimal(txtNbUniteParBase.Text)
                Dim uniteBase As String = If(cmbUniteBase.Text.Trim() = "", "base", cmbUniteBase.Text.Trim())
                Dim stockBase As Decimal = If(nb > 0D, Decimal.Floor(stockPieces / nb), stockPieces)
                lblStockActuel.Text = "Stock actuel: " & stockBase.ToString("N2") & " " & uniteBase
                lblStockActuelPiece.Text = "Equivalent: " & stockPieces.ToString("N2") & " pièces"
            End If
        End Sub

        Private Function ObtenirService() As ProduitService
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New ProduitRepository(dal)
            Return New ProduitService(repo)
        End Function
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
            Dim stock As Integer = service.AfficherQteProduitSelect(produitId)


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
                ligne = New PanierLigne With {.produitId = produitId, .libelle = libelle, .unite = unite, .PrixUnitaire = prix, .Quantite = qte, .quantiteBase = quantiteBase, .QuantiteEquivalente = quantiteEquivalent, .QuantiteReelle = quantiteBase, .Total = prix * qte}
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

                Dim venteDetail As Boolean = If(IsDBNull(row("VenteDetail")), False, Convert.ToInt32(row("VenteDetail")) = 1)
                Dim venteDemi As Boolean = If(IsDBNull(row("VenteDemi")), False, Convert.ToDecimal(row("VenteDemi")) = 1)
                Dim venteDouzaine As Boolean = If(IsDBNull(row("VenteDouzaine")), False, Convert.ToDecimal(row("VenteDouzaine")) = 1)
                Dim venteGros As Boolean = If(IsDBNull(row("VenteGros")), False, Convert.ToDecimal(row("VenteGros")) = 1)

                _typesVenteCourants = _typeVenteService.ConstruireTypesVente(nbUnites, prixAchat, prixGros, prixDemi, prixDetail, prixQuart, prixDouzaine, prixSpecial, venteGros, venteDemi, venteDetail, venteDouzaine)
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
            lblPrixProd.Text = prix.ToString("N2") & "FC"
            If typeChoisi Is Nothing Then
                lblEquivalent.Text = "Equivalent: 0 pièce / unité"
            Else
                lblEquivalent.Text = "Equivalent: " & typeChoisi.QuantiteEquivalent.ToString("N2") & " pièces / unité"
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
                lblTotalReel.Text = "Total réel: 0 pièce"
                Return
            End If

            Dim typeChoisi As TypeVenteDTO = ObtenirTypeVenteSelectionne()
            If typeChoisi Is Nothing Then
                lblTotalReel.Text = "Total réel: 0 pièce"
                Return
            End If

            Dim quantiteReelle As Decimal = qte * typeChoisi.QuantiteEquivalent
            lblTotalReel.Text = "Total réel: " & quantiteReelle.ToString("N2") & " pièces"
        End Sub

        Private Sub MettreAJourAffichageStockProduit()
            If cmbProduitSortie.SelectedValue IsNot Nothing AndAlso Not TypeOf cmbProduitSortie.SelectedValue Is DataRowView Then
                Dim row As DataRowView = TryCast(cmbProduitSortie.SelectedItem, DataRowView)
                If row Is Nothing Then Return
                Dim r As DataRow = row.Row
                Dim produitId As Integer = Convert.ToInt32((If(r.IsNull("ProduitId"), "", Convert.ToDecimal(row("ProduitId")).ToString())))
                Dim stock As Decimal = Convert.ToDecimal(If(r.IsNull("QuantiteStock"), "", Convert.ToDecimal(row("QuantiteStock")).ToString()))
                Dim nbUnites As Decimal = Convert.ToDecimal((If(r.IsNull("ConversionUnite"), "", Convert.ToDecimal(row("ConversionUnite")).ToString())))
                Dim uniteBase As String = Convert.ToString(If(r.IsNull("UnitePrincipale"), "", Convert.ToString(row("UnitePrincipale"))))
                Dim uniteSecondaire As String = Convert.ToString(If(r.IsNull("UniteSecondaire"), "", Convert.ToString(row("UniteSecondaire"))))
                Dim reserve As Decimal = 0D
                For Each ligne As PanierLigne In _panier
                    If ligne.ProduitId = produitId Then
                        reserve += ligne.QuantiteBase
                    End If
                Next
                Dim restant As Decimal = Math.Max(0D, stock - reserve)
                lblStock.Text = "Stock: " & _typeVenteService.FormaterStock(stock, nbUnites, If(uniteBase = "", "base", uniteBase), If(uniteSecondaire = "", "pièce", uniteSecondaire)) &
                    " | Restant: " & _typeVenteService.FormaterStock(restant, nbUnites, If(uniteBase = "", "base", uniteBase), If(uniteSecondaire = "", "pièce", uniteSecondaire))
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
        '        lblStockActuelS.Text = "Stock actuel: " & stockBase.ToString("N2") & " " & uniteBase
        '        lblStockActuelPieceS.Text = "Equivalent: " & stockPieces.ToString("N2") & " pièces"
        '    End If
        'End Sub
        Private Sub RecalculerStock(sender As Object, e As EventArgs)
            Dim nb As Decimal = LireDecimal(txtNbUniteParBase.Text)
            Dim quantiteEntree As Decimal = LireDecimal(txtQuantiteEntree.Text)
            Dim stockActuelPieces As Decimal = ExtraireDecimal(lblStockActuelPiece.Text)
            Dim stockActuelBase As Decimal = If(nb > 0D, Decimal.Floor(stockActuelPieces / nb), stockActuelPieces)
            Dim totalPiecesEntree As Decimal = quantiteEntree * If(nb > 0D, nb, 1D)
            Dim stockApresBase As Decimal = stockActuelBase + quantiteEntree
            Dim stockApresPieces As Decimal = stockActuelPieces + totalPiecesEntree
            Dim uniteBase As String = If(cmbUniteBase.Text.Trim() = "", "base", cmbUniteBase.Text.Trim())

            lblStockActuel.Text = "Stock actuel: " & stockActuelBase.ToString("N2") & " " & uniteBase
            lblStockActuelPiece.Text = "Equivalent: " & stockActuelPieces.ToString("N2") & " pièces"
            lblStockApres.Text = "Stock après: " & stockApresBase.ToString("N2") & " " & uniteBase
            lblStockApresPiece.Text = "Après: " & stockApresPieces.ToString("N2") & " pièces"
            'lblEquivalentType.Text = If(nb > 0D, nb.ToString("N2") & " pièces / unité", "0 pièce / unité")
            RafraichirTypesVente()
        End Sub

        'Private Sub RecalculerStockSortie(sender As Object, e As EventArgs)
        '    If cmbProduitSortie.SelectedValue IsNot Nothing AndAlso Not TypeOf cmbProduitSortie.SelectedValue Is DataRowView Then
        '        Dim produitId As Integer = Convert.ToInt32(cmbProduitSortie.SelectedValue)
        '        Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
        '        Dim dal As New DAL(cs)
        '        Dim stock As Object = dal.ExecuterScalaire("SELECT ISNULL(QuantiteStock,0) FROM vStockProduit WHERE ProduitId=@id", CommandType.Text, New List(Of System.Data.SqlClient.SqlParameter) From {New System.Data.SqlClient.SqlParameter("@id", produitId)})
        '        Dim stockActuel As Decimal = If(stock Is Nothing, 0D, Convert.ToDecimal(stock))
        '        Dim qte As Decimal = LireDecimal(txtQuantiteSortie.Text)
        '        Dim restant As Decimal = stockActuel - qte
        '        txtStockRestant.Text = restant.ToString("N2")
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
            If String.IsNullOrWhiteSpace(txtCoefficientInput.Text) Then
                _coefficientCalcule = 0D
                lblTypeCoefficient.Text = ""
                lblMargeCalculee.Text = ""
                Exit Sub
            End If

            Dim input As String = txtCoefficientInput.Text.Replace("%", "").Replace(".", ",").Trim()
            Dim valeur As Decimal
            If Decimal.TryParse(input, valeur) Then
                Dim coefficient As Decimal
                Dim marge As Decimal

                If txtCoefficientInput.Text.Contains("%") OrElse valeur >= 10 Then
                    marge = valeur
                    coefficient = 1 + (marge / 100)
                    lblTypeCoefficient.Text = "Marge " & marge & "(%)"
                Else
                    coefficient = valeur
                    marge = (coefficient - 1) * 100
                    lblTypeCoefficient.Text = "Coefficient " & coefficient
                End If
                lblMargeCalculee.Text = $" {Math.Round(marge, 2)} %"
                _coefficientCalcule = coefficient
                RecalculerPrixAuto(Nothing, EventArgs.Empty)
            End If
        End Sub

        Private Sub CoefficientDetailChange(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(txtCoefficientDetail.Text) Then
                _coefficientDetailCalcule = 0D
                RecalculerPrixAuto(Nothing, EventArgs.Empty)
                Return
            End If

            Dim valeur As Decimal
            Dim marge As Decimal
            If Decimal.TryParse(txtCoefficientDetail.Text.Replace("%", "").Replace(".", ",").Trim(), valeur) Then
                If txtCoefficientDetail.Text.Contains("%") OrElse valeur > 1D Then
                    _coefficientDetailCalcule = 1D + (valeur / 100D)
                Else
                    _coefficientDetailCalcule = valeur
                End If

                If txtCoefficientDetail.Text.Contains("%") OrElse valeur >= 10 Then
                    marge = valeur
                    _coefficientDetailCalcule = 1 + (marge / 100)
                Else
                    _coefficientDetailCalcule = valeur
                    marge = (_coefficientDetailCalcule - 1) * 100
                End If
                RecalculerPrixAuto(Nothing, EventArgs.Empty)
            End If
        End Sub

        Private Sub RecalculerPrixAuto(sender As Object, e As EventArgs)
            Dim prixAchatVal As Decimal = LireDecimal(txtPrixAchat.Text)
            Dim nbUnites As Decimal = LireDecimal(txtNbUniteParBase.Text)
            Dim coefficientGros As Decimal = If(_coefficientCalcule > 0D, _coefficientCalcule, 0D)
            Dim coefficientDetail As Decimal = If(_coefficientDetailCalcule > 0D, _coefficientDetailCalcule, coefficientGros)

            If prixAchatVal <= 0D OrElse nbUnites <= 0D OrElse coefficientGros <= 0D Then Return

            Dim prixGros As Decimal = prixAchatVal * coefficientGros
            Dim prixDemi As Decimal = prixGros * 0.5D
            Dim prixPiece As Decimal = 0D
            If coefficientDetail > 0D Then
                prixPiece = (prixAchatVal * coefficientDetail) / nbUnites
            End If
            Dim prixQuart As Decimal = prixPiece * Math.Max(1D, Decimal.Floor(nbUnites / 4D))
            Dim prixDouzaine As Decimal = prixPiece * 12D

            txtPrixGros.Text = If(chkGros.Checked, prixGros.ToString("N2"), "-")
            txtPrixDemi.Text = If(chkDemi.Checked, prixDemi.ToString("N2"), "-")
            txtPrixQuart.Text = If(chkQuart.Checked, prixQuart.ToString("N2"), "-")
            txtPrixPiece.Text = If(chkPiece.Checked, prixPiece.ToString("N2"), "-")
            txtPrixDouzaine.Text = If(chkDouzaine.Checked, prixDouzaine.ToString("N2"), "-")
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
                    "SELECT ss.DateSortie, ISNULL(f.NumeroFacture, ss.RefSource) AS NumeroFacture, " &
                    "ISNULL(c.NomClient, '') AS Client, p.Libelle AS Produit, ss.QuantiteSaisie, ss.QuantiteBase, ss.Source " &
                    "FROM StockSortie ss " &
                    "INNER JOIN Produits p ON p.ProduitId = ss.ProduitId " &
                    "LEFT JOIN FacturesVente f ON f.NumeroFacture = ss.RefSource " &
                    "LEFT JOIN Clients c ON c.ClientId = f.ClientId " &
                    "WHERE CAST(ss.DateSortie AS DATE) BETWEEN @Du AND @Au " &
                    "AND (@Recherche = '' OR ISNULL(f.NumeroFacture, ss.RefSource) LIKE @Like OR ISNULL(c.NomClient, '') LIKE @Like) " &
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
            Dim liste As List(Of TypeVenteDTO) = _typeVenteService.ConstruireTypesVente(
                LireDecimal(txtNbUniteParBase.Text),
                LireDecimal(txtPrixAchat.Text),
                LireDecimal(txtPrixGros.Text.Replace("-", "0")),
                LireDecimal(txtPrixDemi.Text.Replace("-", "0")),
                LireDecimal(txtPrixPiece.Text.Replace("-", "0")),
                LireDecimal(txtPrixQuart.Text.Replace("-", "0")),
                LireDecimal(txtPrixDouzaine.Text.Replace("-", "0")),
                0D,
                chkGros.Checked,
                chkDemi.Checked,
                chkPiece.Checked,
                chkDouzaine.Checked)
            gridTypesVente.DataSource = Nothing
            gridTypesVente.DataSource = liste
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
                Dim qte As Decimal = LireDecimal(txtQuantiteEntree.Text)
                If qte <= 0D Then
                    MessageBox.Show("Quantite invalide.")
                    Return
                End If

                Dim produitId As Integer
                If chkProduitExistant.Checked Then
                    If cmbProduitExistant.SelectedValue Is Nothing Then
                        MessageBox.Show("Selectionnez un produit.")
                        Return
                    End If
                    produitId = Convert.ToInt32(cmbProduitExistant.SelectedValue)
                Else
                    Dim nom As String = txtNomProduit.Text.Trim()
                    If nom = "" Then
                        MessageBox.Show("Nom produit obligatoire.")
                        Return
                    End If
                    If txtReference.Text.Trim() = "" Then
                        txtReference.Text = GenererReferenceUnique(nom, cmbCategorie.Text.Trim())
                    End If
                    If LireDecimal(txtNbUniteParBase.Text) <= 0D Then
                        MessageBox.Show("Le nombre d'unités par base doit être supérieur à zéro.")
                        Return
                    End If
                    Dim prixAchatVal1 As Decimal = LireDecimal(txtPrixAchat.Text)
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
                        .CategorieId = If(IsNumeric(cmbCategorie.Text.Trim()), Convert.ToInt32(cmbCategorie.Text.Trim()), CType(Nothing, Integer?)),
                        .UnitePrincipale = cmbUniteBase.Text,
                        .UniteSecondaire = "Piece",
                        .ConversionUnite = LireDecimal(txtNbUniteParBase.Text),
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
                    ChargerProduits()
                    chkProduitExistant.Checked = True
                    cmbProduitExistant.SelectedValue = produitId
                End If
                Dim service As StockService = ObtenirStockService()

                Dim prixAchatVal As Decimal = LireDecimal(txtPrixAchat.Text)
                service.EnregistrerEntree(produitId, qte, cmbUniteBase.Text, txtReference.Text.Trim(), txtObservationEntree.Text.Trim(), SessionUtilisateur.UtilisateurId, prixAchatVal)

                MessageBox.Show("Entrée stock enregistrée.")
                AfficherStockActuel()
                RecalculerStock(Nothing, EventArgs.Empty)
            Catch ex As Exception
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
        '        txtStockTheorique.Text = service.ObtenirStockActuelProduit(produitId).ToString("N2")

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
                txtStockTheorique.Text = stockTheo.ToString("N2")
                RecalculerEcart(Nothing, EventArgs.Empty)
                ' NOUVEAU: Analyse Produit
                ChargerAnalyseProduit(produitId)
            End If
        End Sub

        ' NOUVEAU: Analyse Produit
        Private Sub ChargerAnalyseProduit(produitId As Integer)
            Try
                Dim service As StockService = ObtenirStockService()
                Dim analyse As DataTable = service.ObtenirAnalyseProduit(produitId)
                If analyse IsNot Nothing Then
                    'lblAnalyseSortieGros.Text = "Sorties Gros: " & analyse.SortieGros.ToString("N2")
                    'lblAnalyseSortiePiece.Text = "Sorties Pièces: " & analyse.SortiePiece.ToString("N2")
                    'lblAnalyseRestantGros.Text = "Restant Gros: " & analyse.RestantGros.ToString("N2")
                    'lblAnalyseRestantPiece.Text = "Restant Pièces: " & analyse.RestantPiece.ToString("N2")
                    'lblAnalyseRealisation.Text = "Réalisation: " & analyse.RealisationTotale.ToString("N2") & " " & cmbDevise.Text
                End If
            Catch
            End Try
        End Sub

        'Private Sub CalculerEcart(sender As Object, e As EventArgs)
        '    Dim theo As Decimal = LireDecimal(txtStockTheorique.Text)
        '    Dim reel As Decimal = LireDecimal(txtStockReel.Text)
        '    txtEcart.Text = (reel - theo).ToString("N2")
        'End Sub

        Private Sub RecalculerEcart(sender As Object, e As EventArgs)
            Dim reel As Decimal = LireDecimal(txtStockReel.Text)
            Dim theo As Decimal = LireDecimal(txtStockTheorique.Text)
            Dim ecart As Decimal = reel - theo
            txtEcart.Text = ecart.ToString("N2")
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
                If cmbProduitInventaire.SelectedValue Is Nothing Then
                    MessageBox.Show("Selectionnez un produit.")
                    Return
                End If
                Dim produitId As Integer = Convert.ToInt32(cmbProduitInventaire.SelectedValue)
                Dim qte As Decimal = LireDecimal(txtStockReel.Text)
                Dim service As StockService = ObtenirStockService()
                service.AjusterInventaire(produitId, qte, "base", "INV", txtObservationInventaire.Text.Trim(), SessionUtilisateur.UtilisateurId)
                MessageBox.Show("Inventaire enregistré.")
                ChargerInventaire(Nothing, EventArgs.Empty)
            Catch ex As Exception
                MessageBox.Show("Erreur inventaire: " & ex.Message)
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
                gridRapportEntrees.DataSource = dal.ExecuterTable(sql, CommandType.Text, p)
            Catch ex As Exception
                MessageBox.Show("Erreur rapport entrées: " & ex.Message)
            End Try
        End Sub

        Private Sub ImprimerRapportEntrees(sender As Object, e As EventArgs)
            Try
                If gridRapportEntrees.DataSource Is Nothing Then
                    ChargerRapportEntrees(Nothing, EventArgs.Empty)
                End If

                Dim doc As New PrintDocument()
                If _parametres IsNot Nothing AndAlso _parametres.ImprimanteA4 <> "" Then
                    doc.PrinterSettings.PrinterName = _parametres.ImprimanteA4
                End If
                doc.DefaultPageSettings.Landscape = True
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

            For Each row As DataGridViewRow In gridRapportEntrees.Rows
                If row.IsNewRow Then Continue For
                x = 40
                Dim values As String() = {
                    Convert.ToDateTime(row.Cells("DateEntree").Value).ToString("dd/MM/yyyy"),
                    Convert.ToString(row.Cells("ReferenceStock").Value),
                    Convert.ToString(row.Cells("Produit").Value),
                    Convert.ToString(row.Cells("QuantiteEntree").Value),
                    Convert.ToString(row.Cells("StockApresEntree").Value),
                    Convert.ToString(row.Cells("PrixAchat").Value),
                    Convert.ToString(row.Cells("PrixGros").Value),
                    Convert.ToString(row.Cells("MargePourcent").Value),
                    Convert.ToString(row.Cells("Devise").Value)
                }
                For i As Integer = 0 To values.Length - 1
                    g.DrawRectangle(Pens.Gray, x, y, widths(i), 22)
                    g.DrawString(values(i), fontTexte, Brushes.Black, x + 2, y + 4)
                    x += widths(i)
                Next
                y += 22
                If y > e.MarginBounds.Bottom - 40 Then
                    e.HasMorePages = True
                    Return
                End If
            Next

            e.HasMorePages = False
        End Sub
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

        Private Function LireDecimal(texte As String) As Decimal
            Dim v As Decimal
            If Decimal.TryParse(If(texte.Trim() = "", "0", texte.Trim()), v) Then Return v
            Return 0D
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
        ' Précharge l'onglet d'entrée stock à partir d'un bon d'approvisionnement.
        Public Sub PrechargerDepuisBonApprovisionnement(bonId As Integer)
            Try
                tabs.SelectedIndex = 0
                chkProduitExistant.Checked = True

                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim sql As String = "SELECT TOP 1 b.NumeroBon, l.ProduitId, l.Quantite, l.PrixAchat " &
                                    "FROM BonApprovisionnementLignes l " &
                                    "INNER JOIN BonsApprovisionnement b ON b.BonId = l.BonId " &
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
                txtQuantiteEntree.Text = Convert.ToDecimal(row("Quantite")).ToString("N2")
                txtPrixAchat.Text = Convert.ToDecimal(row("PrixAchat")).ToString("N2")
                txtReference.Text = Convert.ToString(row("NumeroBon"))
                txtObservationEntree.Text = "Réception depuis " & Convert.ToString(row("NumeroBon"))
                AfficherStockActuel()
                RecalculerStock(Nothing, EventArgs.Empty)
            Catch
                ' N'interrompt pas l'ouverture de l'écran si la précharge échoue.
            End Try
        End Sub
    End Class
End Namespace
