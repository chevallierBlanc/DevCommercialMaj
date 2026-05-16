Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports Microsoft.VisualBasic
Imports System.Data
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D
Imports System.Drawing.Printing

Namespace DevCommerc8ak
    Public Class FormulaireStock1
        Inherits Form

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

        Private _produitsTable As DataTable
        Private _coefficientCalcule As Decimal
        Private _coefficientDetailCalcule As Decimal
        Private _parametres As ParametreDTO
        Private ReadOnly _typeVenteService As TypeVenteService

        Public Sub New()
            Me.Text = "Stock"
            Me.Width = 1250
            Me.Height = 780
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = Color.White
            _typeVenteService = New TypeVenteService()

            tabs = New TabControl() With {.Dock = DockStyle.Fill}

            Dim tabEntree As New TabPage("Stock Entrée") With {.AutoScroll = True}
            Dim tabSortie As New TabPage("Stock Sortie") With {.AutoScroll = True}
            Dim tabInventaire As New TabPage("Inventaire") With {.AutoScroll = True}
            Dim tabAlertes As New TabPage("Alertes") With {.AutoScroll = True}
            Dim tabPerte As New TabPage("Perte") With {.AutoScroll = True}
            Dim tabRapportEntrees As New TabPage("Rapport Entrées") With {.AutoScroll = True}

            tabs.TabPages.Add(tabEntree)
            tabs.TabPages.Add(tabSortie)
            tabs.TabPages.Add(tabInventaire)
            tabs.TabPages.Add(tabAlertes)
            tabs.TabPages.Add(tabPerte)
            tabs.TabPages.Add(tabRapportEntrees)

            ' ------------------- ENTREE -------------------
            Dim grpProduit As New GroupBox() With {.Text = "Informations produit", .Left = 10, .Top = 10, .Width = 600, .Height = 150, .Anchor = AnchorStyles.Top Or AnchorStyles.Left}
            chkProduitExistant = New CheckBox() With {.Text = "Produit existant", .Left = 20, .Top = 25, .AutoSize = True, .Checked = True}
            cmbProduitExistant = New ComboBox() With {.Left = 160, .Top = 22, .Width = 200, .DropDownStyle = ComboBoxStyle.DropDownList}
            txtNomProduit = New TextBox() With {.Left = 160, .Top = 55, .Width = 200}
            cmbCategorie = New ComboBox() With {.Left = 160, .Top = 85, .Width = 120}
            txtReference = New TextBox() With {.Left = 160, .Top = 115, .Width = 200, .ReadOnly = True}

            grpProduit.Controls.Add(New Label() With {.Text = "Nom produit", .Left = 20, .Top = 58, .AutoSize = True})
            grpProduit.Controls.Add(New Label() With {.Text = "Categorie", .Left = 20, .Top = 88, .AutoSize = True})
            grpProduit.Controls.Add(New Label() With {.Text = "Reference", .Left = 20, .Top = 118, .AutoSize = True})
            grpProduit.Controls.Add(chkProduitExistant)
            grpProduit.Controls.Add(cmbProduitExistant)
            grpProduit.Controls.Add(txtNomProduit)
            grpProduit.Controls.Add(cmbCategorie)
            grpProduit.Controls.Add(txtReference)

            Dim grpUnite As New GroupBox() With {.Text = "Unité & Conversion", .Left = 620, .Top = 10, .Width = 600, .Height = 150, .Anchor = AnchorStyles.Top Or AnchorStyles.Left}
            cmbUniteBase = New ComboBox() With {.Left = 150, .Top = 30, .Width = 150, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbUniteBase.Items.AddRange(New Object() {"Carton", "Sac", "Pack", "Paquet", "Bidon", "Sachet", "Kg", "Piece"})
            txtNbUniteParBase = New TextBox() With {.Left = 150, .Top = 60, .Width = 100}
            txtQuantiteEntree = New TextBox() With {.Left = 150, .Top = 90, .Width = 100}
            lblStockActuel = New Label() With {.Left = 330, .Top = 35, .AutoSize = True}
            lblStockActuelPiece = New Label() With {.Left = 330, .Top = 55, .AutoSize = True}
            lblStockApres = New Label() With {.Left = 330, .Top = 65, .AutoSize = True}
            lblStockApresPiece = New Label() With {.Left = 330, .Top = 85, .AutoSize = True}

            grpUnite.Controls.Add(New Label() With {.Text = "Unité base", .Left = 20, .Top = 33, .AutoSize = True})
            grpUnite.Controls.Add(New Label() With {.Text = "Nb unités/base", .Left = 20, .Top = 63, .AutoSize = True})
            grpUnite.Controls.Add(New Label() With {.Text = "Quantité entrée", .Left = 20, .Top = 93, .AutoSize = True})
            grpUnite.Controls.Add(cmbUniteBase)
            grpUnite.Controls.Add(txtNbUniteParBase)
            grpUnite.Controls.Add(txtQuantiteEntree)
            grpUnite.Controls.Add(lblStockActuel)
            grpUnite.Controls.Add(lblStockActuelPiece)
            grpUnite.Controls.Add(lblStockApres)
            grpUnite.Controls.Add(lblStockApresPiece)

            Dim grpFinance As New GroupBox() With {.Text = "Informations financières", .Left = 10, .Top = 170, .Width = 600, .Height = 190, .Anchor = AnchorStyles.Top Or AnchorStyles.Left}
            txtPrixAchat = New TextBox() With {.Left = 150, .Top = 25, .Width = 120}
            cmbDevise = New ComboBox() With {.Left = 280, .Top = 25, .Width = 70, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbDevise.Items.AddRange(New Object() {"CDF", "USD"})
            cmbDevise.SelectedIndex = 0
            txtTaux = New TextBox() With {.Left = 360, .Top = 25, .Width = 80, .ReadOnly = True}
            txtCoefficientInput = New TextBox() With {.Left = 150, .Top = 55, .Width = 120}
            txtCoefficientDetail = New TextBox() With {.Left = 150, .Top = 115, .Width = 120}
            lblTypeCoefficient = New Label() With {.Left = 280, .Top = 58, .AutoSize = True}
            lblMargeCalculee = New Label() With {.Left = 150, .Top = 85, .AutoSize = True}

            grpFinance.Controls.Add(New Label() With {.Text = "Prix achat", .Left = 20, .Top = 28, .AutoSize = True})
            grpFinance.Controls.Add(New Label() With {.Text = "Devise", .Left = 280, .Top = 8, .AutoSize = True})
            grpFinance.Controls.Add(New Label() With {.Text = "Taux", .Left = 360, .Top = 8, .AutoSize = True})
            grpFinance.Controls.Add(New Label() With {.Text = "Coef/Marge", .Left = 20, .Top = 58, .AutoSize = True})
            grpFinance.Controls.Add(New Label() With {.Text = "Coef detail", .Left = 20, .Top = 118, .AutoSize = True})
            grpFinance.Controls.Add(txtPrixAchat)
            grpFinance.Controls.Add(cmbDevise)
            grpFinance.Controls.Add(txtTaux)
            grpFinance.Controls.Add(txtCoefficientInput)
            grpFinance.Controls.Add(txtCoefficientDetail)
            grpFinance.Controls.Add(lblTypeCoefficient)
            grpFinance.Controls.Add(lblMargeCalculee)

            Dim grpPrix As New GroupBox() With {.Text = "Prix calculés", .Left = 620, .Top = 170, .Width = 600, .Height = 190, .Anchor = AnchorStyles.Top Or AnchorStyles.Left}
            txtPrixGros = New TextBox() With {.Left = 110, .Top = 30, .Width = 100, .ReadOnly = True}
            txtPrixDemi = New TextBox() With {.Left = 300, .Top = 30, .Width = 100, .ReadOnly = True}
            txtPrixQuart = New TextBox() With {.Left = 110, .Top = 70, .Width = 100, .ReadOnly = True}
            txtPrixPiece = New TextBox() With {.Left = 300, .Top = 70, .Width = 100, .ReadOnly = True}
            txtPrixDouzaine = New TextBox() With {.Left = 110, .Top = 110, .Width = 100, .ReadOnly = True}

            grpPrix.Controls.Add(New Label() With {.Text = "Prix gros", .Left = 20, .Top = 33, .AutoSize = True})
            grpPrix.Controls.Add(New Label() With {.Text = "Prix demi", .Left = 230, .Top = 33, .AutoSize = True})
            grpPrix.Controls.Add(New Label() With {.Text = "Prix 1/4", .Left = 20, .Top = 73, .AutoSize = True})
            grpPrix.Controls.Add(New Label() With {.Text = "Prix pièce", .Left = 230, .Top = 73, .AutoSize = True})
            grpPrix.Controls.Add(New Label() With {.Text = "Prix douzaine", .Left = 20, .Top = 113, .AutoSize = True})

            grpPrix.Controls.Add(txtPrixGros)
            grpPrix.Controls.Add(txtPrixDemi)
            grpPrix.Controls.Add(txtPrixQuart)
            grpPrix.Controls.Add(txtPrixPiece)
            grpPrix.Controls.Add(txtPrixDouzaine)

            Dim grpOptions As New GroupBox() With {.Text = "Options de vente", .Left = 10, .Top = 365, .Width = 600, .Height = 80, .Anchor = AnchorStyles.Top Or AnchorStyles.Left}
            chkGros = New CheckBox() With {.Text = "Gros", .Left = 20, .Top = 30}
            chkDemi = New CheckBox() With {.Text = "Demi", .Left = 110, .Top = 30}
            chkQuart = New CheckBox() With {.Text = "1/4", .Left = 200, .Top = 30}
            chkPiece = New CheckBox() With {.Text = "Pièce", .Left = 280, .Top = 30}
            chkDouzaine = New CheckBox() With {.Text = "Douzaine", .Left = 370, .Top = 30}
            lblEquivalentType = New Label() With {.Left = 470, .Top = 30, .Width = 110, .Height = 36}

            grpOptions.Controls.Add(chkGros)
            grpOptions.Controls.Add(chkDemi)
            grpOptions.Controls.Add(chkQuart)
            grpOptions.Controls.Add(chkPiece)
            grpOptions.Controls.Add(chkDouzaine)
            grpOptions.Controls.Add(lblEquivalentType)

            gridTypesVente = New DataGridView() With {.Left = 10, .Top = 455, .Width = 1210, .Height = 150, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AutoGenerateColumns = True}

            Dim grpAutres As New GroupBox() With {.Text = "Autres infos", .Left = 620, .Top = 365, .Width = 600, .Height = 80, .Anchor = AnchorStyles.Top Or AnchorStyles.Left}
            dtpDateEntree = New DateTimePicker() With {.Left = 110, .Top = 25, .Width = 140, .Format = DateTimePickerFormat.Short}
            txtObservationEntree = New TextBox() With {.Left = 330, .Top = 25, .Width = 240}
            grpAutres.Controls.Add(New Label() With {.Text = "Date entrée", .Left = 20, .Top = 28, .AutoSize = True})
            grpAutres.Controls.Add(New Label() With {.Text = "Observation", .Left = 260, .Top = 28, .AutoSize = True})
            grpAutres.Controls.Add(dtpDateEntree)
            grpAutres.Controls.Add(txtObservationEntree)

            btnEnregistrerEntree = New Button() With {.Text = "Enregistrer entrée", .Left = 10, .Top = 615, .Width = 160, .BackColor = Color.LightGreen}

            tabEntree.Controls.Add(grpProduit)
            tabEntree.Controls.Add(grpUnite)
            tabEntree.Controls.Add(grpFinance)
            tabEntree.Controls.Add(grpPrix)
            tabEntree.Controls.Add(grpOptions)
            tabEntree.Controls.Add(grpAutres)
            tabEntree.Controls.Add(gridTypesVente)
            tabEntree.Controls.Add(btnEnregistrerEntree)

            ' ------------------- SORTIE -------------------
            Dim grpSortieFiltres As New GroupBox() With {.Text = "Sorties du mois", .Left = 10, .Top = 10, .Width = 1200, .Height = 90, .Anchor = AnchorStyles.Top Or AnchorStyles.Left}
            txtRechercheSortie = New TextBox() With {.Left = 150, .Top = 28, .Width = 260}
            dtpSortieDu = New DateTimePicker() With {.Left = 520, .Top = 28, .Width = 130, .Format = DateTimePickerFormat.Short}
            dtpSortieAu = New DateTimePicker() With {.Left = 700, .Top = 28, .Width = 130, .Format = DateTimePickerFormat.Short}
            btnRafraichirSortie = New Button() With {.Text = "Actualiser", .Left = 860, .Top = 24, .Width = 120, .BackColor = Color.LightSkyBlue}
            gridSortieMois = New DataGridView() With {.Left = 10, .Top = 110, .Width = 1200, .Height = 520, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AutoGenerateColumns = True}

            grpSortieFiltres.Controls.Add(New Label() With {.Text = "Recherche facture/client", .Left = 20, .Top = 31, .AutoSize = True})
            grpSortieFiltres.Controls.Add(New Label() With {.Text = "Du", .Left = 490, .Top = 31, .AutoSize = True})
            grpSortieFiltres.Controls.Add(New Label() With {.Text = "Au", .Left = 670, .Top = 31, .AutoSize = True})
            grpSortieFiltres.Controls.Add(txtRechercheSortie)
            grpSortieFiltres.Controls.Add(dtpSortieDu)
            grpSortieFiltres.Controls.Add(dtpSortieAu)
            grpSortieFiltres.Controls.Add(btnRafraichirSortie)

            tabSortie.Controls.Add(grpSortieFiltres)
            tabSortie.Controls.Add(gridSortieMois)

            ' ------------------- INVENTAIRE -------------------
            Dim grpInvSel As New GroupBox() With {.Text = "Inventaire", .Left = 10, .Top = 10, .Width = 1200, .Height = 90, .Anchor = AnchorStyles.Top Or AnchorStyles.Left}
            cmbProduitInventaire = New ComboBox() With {.Left = 120, .Top = 30, .Width = 240, .DropDownStyle = ComboBoxStyle.DropDownList}
            grpInvSel.Controls.Add(New Label() With {.Text = "Produit", .Left = 20, .Top = 33, .AutoSize = True})
            grpInvSel.Controls.Add(cmbProduitInventaire)

            gridEntrees = New DataGridView() With {.Left = 10, .Top = 110, .Width = 580, .Height = 220, .ReadOnly = True, .AutoGenerateColumns = True}
            gridSorties = New DataGridView() With {.Left = 610, .Top = 110, .Width = 580, .Height = 220, .ReadOnly = True, .AutoGenerateColumns = True}

            Dim grpInvInfo As New GroupBox() With {.Text = "Infos inventaire", .Left = 10, .Top = 340, .Width = 1200, .Height = 130, .Anchor = AnchorStyles.Top Or AnchorStyles.Left}
            txtStockTheorique = New TextBox() With {.Left = 150, .Top = 25, .Width = 120, .ReadOnly = True}
            txtStockReel = New TextBox() With {.Left = 150, .Top = 55, .Width = 120}
            txtEcart = New TextBox() With {.Left = 150, .Top = 85, .Width = 120, .ReadOnly = True}
            dtpDateInventaire = New DateTimePicker() With {.Left = 380, .Top = 25, .Width = 140, .Format = DateTimePickerFormat.Short}
            txtObservationInventaire = New TextBox() With {.Left = 380, .Top = 55, .Width = 300}
            txtUtilisateurInventaire = New TextBox() With {.Left = 380, .Top = 85, .Width = 200, .ReadOnly = True}

            grpInvInfo.Controls.Add(New Label() With {.Text = "Stock théorique", .Left = 20, .Top = 28, .AutoSize = True})
            grpInvInfo.Controls.Add(New Label() With {.Text = "Stock réel", .Left = 20, .Top = 58, .AutoSize = True})
            grpInvInfo.Controls.Add(New Label() With {.Text = "Ecart", .Left = 20, .Top = 88, .AutoSize = True})
            grpInvInfo.Controls.Add(New Label() With {.Text = "Date", .Left = 300, .Top = 28, .AutoSize = True})
            grpInvInfo.Controls.Add(New Label() With {.Text = "Observation", .Left = 300, .Top = 58, .AutoSize = True})
            grpInvInfo.Controls.Add(New Label() With {.Text = "Utilisateur", .Left = 300, .Top = 88, .AutoSize = True})
            grpInvInfo.Controls.Add(txtStockTheorique)
            grpInvInfo.Controls.Add(txtStockReel)
            grpInvInfo.Controls.Add(txtEcart)
            grpInvInfo.Controls.Add(dtpDateInventaire)
            grpInvInfo.Controls.Add(txtObservationInventaire)
            grpInvInfo.Controls.Add(txtUtilisateurInventaire)

            btnValiderInventaire = New Button() With {.Text = "Valider inventaire", .Left = 10, .Top = 480, .Width = 170, .BackColor = Color.LightGreen}

            tabInventaire.Controls.Add(grpInvSel)
            tabInventaire.Controls.Add(gridEntrees)
            tabInventaire.Controls.Add(gridSorties)
            tabInventaire.Controls.Add(grpInvInfo)
            tabInventaire.Controls.Add(btnValiderInventaire)

            ' ------------------- ALERTES -------------------
            gridAlertes = New DataGridView() With {.Dock = DockStyle.Top, .Height = 400, .ReadOnly = True, .AutoGenerateColumns = True}
            btnRafraichirAlertes = New Button() With {.Text = "Rafraichir", .Left = 10, .Top = 420, .Width = 120}
            tabAlertes.Controls.Add(gridAlertes)
            tabAlertes.Controls.Add(btnRafraichirAlertes)

            ' ------------------- PERTE -------------------
            Dim grpPerte As New GroupBox() With {.Text = "Déclaration de perte", .Left = 10, .Top = 10, .Width = 1200, .Height = 200, .Anchor = AnchorStyles.Top Or AnchorStyles.Left}
            cmbProduitPerte = New ComboBox() With {.Left = 150, .Top = 30, .Width = 220, .DropDownStyle = ComboBoxStyle.DropDownList}
            txtQuantitePerte = New TextBox() With {.Left = 150, .Top = 60, .Width = 120}
            cmbTypePerte = New ComboBox() With {.Left = 150, .Top = 90, .Width = 180, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbTypePerte.Items.AddRange(New Object() {"Cassé", "Expiré", "Vol", "Détérioration"})
            dtpDatePerte = New DateTimePicker() With {.Left = 150, .Top = 120, .Width = 140, .Format = DateTimePickerFormat.Short}
            txtObservationPerte = New TextBox() With {.Left = 450, .Top = 30, .Width = 500}
            txtResponsablePerte = New TextBox() With {.Left = 450, .Top = 60, .Width = 200, .ReadOnly = True}

            grpPerte.Controls.Add(New Label() With {.Text = "Produit", .Left = 20, .Top = 33, .AutoSize = True})
            grpPerte.Controls.Add(New Label() With {.Text = "Quantité perdue", .Left = 20, .Top = 63, .AutoSize = True})
            grpPerte.Controls.Add(New Label() With {.Text = "Type perte", .Left = 20, .Top = 93, .AutoSize = True})
            grpPerte.Controls.Add(New Label() With {.Text = "Date", .Left = 20, .Top = 123, .AutoSize = True})
            grpPerte.Controls.Add(New Label() With {.Text = "Observation", .Left = 370, .Top = 33, .AutoSize = True})
            grpPerte.Controls.Add(New Label() With {.Text = "Responsable", .Left = 370, .Top = 63, .AutoSize = True})
            grpPerte.Controls.Add(cmbProduitPerte)
            grpPerte.Controls.Add(txtQuantitePerte)
            grpPerte.Controls.Add(cmbTypePerte)
            grpPerte.Controls.Add(dtpDatePerte)
            grpPerte.Controls.Add(txtObservationPerte)
            grpPerte.Controls.Add(txtResponsablePerte)

            btnEnregistrerPerte = New Button() With {.Text = "Enregistrer perte", .Left = 10, .Top = 220, .Width = 160, .BackColor = Color.LightCoral}

            tabPerte.Controls.Add(grpPerte)
            tabPerte.Controls.Add(btnEnregistrerPerte)

            ' ------------------- RAPPORT ENTREES -------------------
            Dim grpRapportFiltres As New GroupBox() With {.Text = "Rapport des entrées", .Left = 10, .Top = 10, .Width = 1200, .Height = 90, .Anchor = AnchorStyles.Top Or AnchorStyles.Left}
            dtpRapportDu = New DateTimePicker() With {.Left = 80, .Top = 28, .Width = 130, .Format = DateTimePickerFormat.Short}
            dtpRapportAu = New DateTimePicker() With {.Left = 280, .Top = 28, .Width = 130, .Format = DateTimePickerFormat.Short}
            btnChargerRapportEntrees = New Button() With {.Text = "Charger", .Left = 450, .Top = 24, .Width = 110, .BackColor = Color.LightSkyBlue}
            btnImprimerRapportEntrees = New Button() With {.Text = "Imprimer A4", .Left = 580, .Top = 24, .Width = 120, .BackColor = Color.LightGreen}
            gridRapportEntrees = New DataGridView() With {.Left = 10, .Top = 110, .Width = 1200, .Height = 520, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AutoGenerateColumns = True}

            grpRapportFiltres.Controls.Add(New Label() With {.Text = "Du", .Left = 20, .Top = 31, .AutoSize = True})
            grpRapportFiltres.Controls.Add(New Label() With {.Text = "Au", .Left = 240, .Top = 31, .AutoSize = True})
            grpRapportFiltres.Controls.Add(dtpRapportDu)
            grpRapportFiltres.Controls.Add(dtpRapportAu)
            grpRapportFiltres.Controls.Add(btnChargerRapportEntrees)
            grpRapportFiltres.Controls.Add(btnImprimerRapportEntrees)

            tabRapportEntrees.Controls.Add(grpRapportFiltres)
            tabRapportEntrees.Controls.Add(gridRapportEntrees)

            Me.Controls.Add(tabs)

            ' Events
            AddHandler chkProduitExistant.CheckedChanged, AddressOf BasculerProduitExistant
            AddHandler cmbProduitExistant.SelectedIndexChanged, AddressOf ChargerProduitSelection
            AddHandler txtNomProduit.TextChanged, AddressOf GenererReferenceAutomatique
            AddHandler txtQuantiteEntree.TextChanged, AddressOf RecalculerStock
            AddHandler txtNbUniteParBase.TextChanged, AddressOf RecalculerStock
            AddHandler txtPrixAchat.TextChanged, AddressOf RecalculerPrixAuto
            AddHandler txtCoefficientInput.TextChanged, AddressOf CoefficientInputChange
            AddHandler txtCoefficientDetail.TextChanged, AddressOf CoefficientDetailChange
            AddHandler chkGros.CheckedChanged, AddressOf MettreAJourChampsPrix
            AddHandler chkDemi.CheckedChanged, AddressOf MettreAJourChampsPrix
            AddHandler chkQuart.CheckedChanged, AddressOf MettreAJourChampsPrix
            AddHandler chkPiece.CheckedChanged, AddressOf MettreAJourChampsPrix
            AddHandler chkDouzaine.CheckedChanged, AddressOf MettreAJourChampsPrix
            AddHandler btnEnregistrerEntree.Click, AddressOf EnregistrerEntree

            AddHandler txtRechercheSortie.TextChanged, AddressOf ChargerSortiesDuMois
            AddHandler dtpSortieDu.ValueChanged, AddressOf ChargerSortiesDuMois
            AddHandler dtpSortieAu.ValueChanged, AddressOf ChargerSortiesDuMois
            AddHandler btnRafraichirSortie.Click, AddressOf ChargerSortiesDuMois

            AddHandler cmbProduitInventaire.SelectedIndexChanged, AddressOf ChargerInventaire
            AddHandler txtStockReel.TextChanged, AddressOf RecalculerEcart
            AddHandler btnValiderInventaire.Click, AddressOf ValiderInventaire

            AddHandler btnRafraichirAlertes.Click, AddressOf ChargerAlertes

            AddHandler btnEnregistrerPerte.Click, AddressOf EnregistrerPerte
            AddHandler btnChargerRapportEntrees.Click, AddressOf ChargerRapportEntrees
            AddHandler btnImprimerRapportEntrees.Click, AddressOf ImprimerRapportEntrees

            ThemeHelper.AppliquerTheme(Me)
            ChargerParametres()
            ChargerProduits()
            InitialiserChamps()
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

        Private Sub ChargerProduits()
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New ProduitRepository(dal)
            _produitsTable = repo.ListerTable()

            cmbProduitExistant.DataSource = _produitsTable
            cmbProduitExistant.DisplayMember = "Libelle"
            cmbProduitExistant.ValueMember = "ProduitId"

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

        Private Sub InitialiserChamps()
            chkProduitExistant.Checked = True
            txtResponsablePerte.Text = SessionUtilisateur.NomUtilisateur
            txtUtilisateurInventaire.Text = SessionUtilisateur.NomUtilisateur
            dtpSortieDu.Value = New Date(Date.Now.Year, Date.Now.Month, 1)
            dtpSortieAu.Value = dtpSortieDu.Value.AddMonths(1).AddDays(-1)
            dtpRapportDu.Value = New Date(Date.Now.Year, Date.Now.Month, 1)
            dtpRapportAu.Value = dtpRapportDu.Value.AddMonths(1).AddDays(-1)
            _coefficientCalcule = 0D
            _coefficientDetailCalcule = 0D
            lblTypeCoefficient.Text = ""
            lblMargeCalculee.Text = ""
            lblStockActuel.Text = "Stock actuel: 0"
            lblStockActuelPiece.Text = "Equivalent: 0 pièce"
            lblStockApres.Text = "Stock après: 0"
            lblStockApresPiece.Text = "Après: 0 pièce"
            lblEquivalentType.Text = "0 pièce / unité"
            MettreAJourChampsPrix(Nothing, EventArgs.Empty)
            RafraichirTypesVente()
            AppliquerStyleModerne()
        End Sub

        Private Sub BasculerProduitExistant(sender As Object, e As EventArgs)
            Dim existant As Boolean = chkProduitExistant.Checked
            cmbProduitExistant.Enabled = existant
            txtNomProduit.ReadOnly = existant
            cmbCategorie.Enabled = Not existant
            cmbUniteBase.Enabled = True
            txtNbUniteParBase.ReadOnly = existant
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
            End If
            RafraichirTypesVente()
        End Sub

        Private Sub GenererReferenceAutomatique(sender As Object, e As EventArgs)
            If chkProduitExistant.Checked Then
                Return
            End If
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
            txtNbUniteParBase.Text = If(r.IsNull("ConversionUnite"), "", Convert.ToDecimal(row("ConversionUnite")).ToString("N0"))
            txtPrixAchat.Text = If(r.IsNull("PrixAchat"), "", Convert.ToDecimal(row("PrixAchat")).ToString("N0"))
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
                lblStockActuel.Text = "Stock actuel: " & stockBase.ToString("N0") & " " & uniteBase
                lblStockActuelPiece.Text = "Equivalent: " & stockPieces.ToString("N0") & " pièces"
            End If
        End Sub

        Private Sub RecalculerStock(sender As Object, e As EventArgs)
            Dim nb As Decimal = LireDecimal(txtNbUniteParBase.Text)
            Dim quantiteEntree As Decimal = LireDecimal(txtQuantiteEntree.Text)
            Dim stockActuelPieces As Decimal = ExtraireDecimal(lblStockActuelPiece.Text)
            Dim stockActuelBase As Decimal = If(nb > 0D, Decimal.Floor(stockActuelPieces / nb), stockActuelPieces)
            Dim totalPiecesEntree As Decimal = quantiteEntree * If(nb > 0D, nb, 1D)
            Dim stockApresBase As Decimal = stockActuelBase + quantiteEntree
            Dim stockApresPieces As Decimal = stockActuelPieces + totalPiecesEntree
            Dim uniteBase As String = If(cmbUniteBase.Text.Trim() = "", "base", cmbUniteBase.Text.Trim())

            lblStockActuel.Text = "Stock actuel: " & stockActuelBase.ToString("N0") & " " & uniteBase
            lblStockActuelPiece.Text = "Equivalent: " & stockActuelPieces.ToString("N0") & " pièces"
            lblStockApres.Text = "Stock après: " & stockApresBase.ToString("N0") & " " & uniteBase
            lblStockApresPiece.Text = "Après: " & stockApresPieces.ToString("N0") & " pièces"
            lblEquivalentType.Text = If(nb > 0D, nb.ToString("N0") & " pièces / unité", "0 pièce / unité")
            RafraichirTypesVente()
        End Sub

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
                    'cas pourcentage
                    marge = valeur
                    coefficient = 1 + (marge / 100)
                    lblTypeCoefficient.Text = "Marge" & marge & "(%)"
                Else
                    'cas coefficient 
                    coefficient = valeur
                    marge = (coefficient - 1) * 100
                    lblTypeCoefficient.Text = "Coefficient" & coefficient & ""
                End If
                'affichage marge calculée
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
                    'cas pourcentage
                    marge = valeur
                    _coefficientDetailCalcule = 1 + (marge / 100)
                    ' lblTypeCoefficient.Text = "Marge" & marge & "(%)"
                Else
                    'cas coefficient 
                    _coefficientDetailCalcule = valeur
                    marge = (_coefficientDetailCalcule - 1) * 100
                    ' lblTypeCoefficient.Text = "Coefficient" & _coefficientDetailCalcule & ""
                End If
                RecalculerPrixAuto(Nothing, EventArgs.Empty)
            End If
        End Sub


        Private Sub RecalculerPrixAuto(sender As Object, e As EventArgs)
            Dim prixAchatVal As Decimal = LireDecimal(txtPrixAchat.Text)
            Dim nbUnites As Decimal = LireDecimal(txtNbUniteParBase.Text)
            Dim coefficientGros As Decimal = If(_coefficientCalcule > 0D, _coefficientCalcule, 0D)
            Dim coefficientDetail As Decimal = If(_coefficientDetailCalcule > 0D, _coefficientDetailCalcule, coefficientGros)
            If prixAchatVal <= 0D OrElse nbUnites <= 0D OrElse coefficientGros <= 0D Then
                Return
            End If

            Dim prixGros As Decimal = prixAchatVal * coefficientGros
            Dim prixDemi As Decimal = prixGros * 0.5D
            Dim prixPiece As Decimal = 0D
            If coefficientDetail > 0D Then
                prixPiece = (prixAchatVal * coefficientDetail) / nbUnites
            End If
            Dim prixQuart As Decimal = prixPiece * Math.Max(1D, Decimal.Floor(nbUnites / 4D))
            Dim prixDouzaine As Decimal = prixPiece * 12D

            txtPrixGros.Text = If(chkGros.Checked, prixGros.ToString("N0"), "-")
            txtPrixDemi.Text = If(chkDemi.Checked, prixDemi.ToString("N0"), "-")
            txtPrixQuart.Text = If(chkQuart.Checked, prixQuart.ToString("N0"), "-")
            txtPrixPiece.Text = If(chkPiece.Checked, prixPiece.ToString("N0"), "-")
            txtPrixDouzaine.Text = If(chkDouzaine.Checked, prixDouzaine.ToString("N0"), "-")
            RafraichirTypesVente()
        End Sub

        Private Sub MettreAJourChampsPrix(sender As Object, e As EventArgs)
            txtPrixGros.Enabled = chkGros.Checked
            txtPrixDemi.Enabled = chkDemi.Checked
            txtPrixQuart.Enabled = chkQuart.Checked
            txtPrixPiece.Enabled = chkPiece.Checked
            txtPrixDouzaine.Enabled = chkDouzaine.Checked
            RafraichirTypesVente()
        End Sub

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

        Private Sub AppliquerStyleModerne()
            Dim bleuFonce As Color = Color.FromArgb(18, 32, 64)
            Dim bleuClair As Color = Color.FromArgb(56, 120, 215)
            Dim blanc As Color = Color.White
            Dim rouge As Color = Color.FromArgb(220, 53, 69)
            Dim vert As Color = Color.FromArgb(46, 160, 67)
            Dim texteFonce As Color = Color.FromArgb(30, 30, 30)

            Me.BackColor = bleuFonce
            tabs.Appearance = TabAppearance.Normal
            tabs.BackColor = bleuFonce
            tabs.SizeMode = TabSizeMode.Normal
            tabs.Multiline = False

            For Each tp As TabPage In tabs.TabPages
                tp.BackColor = bleuFonce
                tp.Padding = New Padding(12)
            Next

            AppliquerStyleRecursif(Me, blanc, texteFonce)

            ' Boutons principaux
            AppliquerStyleBouton(btnEnregistrerEntree, vert, Color.FromArgb(70, 180, 90))
            AppliquerStyleBouton(btnRafraichirSortie, bleuClair, Color.FromArgb(80, 140, 230))
            AppliquerStyleBouton(btnValiderInventaire, vert, Color.FromArgb(70, 180, 90))
            AppliquerStyleBouton(btnRafraichirAlertes, bleuClair, Color.FromArgb(80, 140, 230))
            AppliquerStyleBouton(btnEnregistrerPerte, rouge, Color.FromArgb(235, 80, 95))
            AppliquerStyleBouton(btnChargerRapportEntrees, bleuClair, Color.FromArgb(80, 140, 230))
            AppliquerStyleBouton(btnImprimerRapportEntrees, vert, Color.FromArgb(70, 180, 90))
        End Sub

        Private Sub AppliquerStyleRecursif(c As Control, fondCarte As Color, texteFonce As Color)
            If TypeOf c Is GroupBox Then
                c.BackColor = fondCarte
                c.ForeColor = texteFonce
            ElseIf TypeOf c Is Panel Then
                c.BackColor = fondCarte
            ElseIf TypeOf c Is TextBox Then
                Dim tb As TextBox = DirectCast(c, TextBox)
                tb.BorderStyle = BorderStyle.FixedSingle
                tb.BackColor = Color.White
                tb.ForeColor = texteFonce
            ElseIf TypeOf c Is ComboBox Then
                Dim cb As ComboBox = DirectCast(c, ComboBox)
                cb.BackColor = Color.White
                cb.ForeColor = texteFonce
            ElseIf TypeOf c Is TabControl Then
                c.ForeColor = texteFonce
            ElseIf TypeOf c Is TabPage Then
                c.ForeColor = texteFonce
            ElseIf TypeOf c Is Label Then
                c.ForeColor = texteFonce
            ElseIf TypeOf c Is DataGridView Then
                Dim g As DataGridView = DirectCast(c, DataGridView)
                g.BackgroundColor = Color.White
                g.GridColor = Color.FromArgb(230, 230, 230)
                g.BorderStyle = BorderStyle.None
                g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            End If

            For Each child As Control In c.Controls
                AppliquerStyleRecursif(child, fondCarte, texteFonce)
            Next
        End Sub

        Private Sub AppliquerStyleBouton(btn As Button, baseColor As Color, hoverColor As Color)
            btn.BackColor = baseColor
            btn.ForeColor = Color.White
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            AppliquerCoinsArrondis(btn, 8)
            AddHandler btn.MouseEnter, Sub() btn.BackColor = hoverColor
            AddHandler btn.MouseLeave, Sub() btn.BackColor = baseColor
        End Sub

        Private Sub AppliquerCoinsArrondis(ctrl As Control, radius As Integer)
            Dim rect As Rectangle = ctrl.ClientRectangle
            If rect.Width <= 0 OrElse rect.Height <= 0 Then Return
            Dim path As New GraphicsPath()
            Dim d As Integer = radius * 2
            path.AddArc(rect.X, rect.Y, d, d, 180, 90)
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
            path.CloseFigure()
            ctrl.Region = New Region(path)
        End Sub

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
            End If
        End Sub

        Private Sub RecalculerEcart(sender As Object, e As EventArgs)
            Dim reel As Decimal = LireDecimal(txtStockReel.Text)
            Dim theo As Decimal = LireDecimal(txtStockTheorique.Text)
            Dim ecart As Decimal = reel - theo
            txtEcart.Text = ecart.ToString("N0")
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

        Private Function ObtenirStockService() As StockService
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Return New StockService(dal)
        End Function

        Private Function LireDecimal(texte As String) As Decimal
            Dim v As Decimal
            If Decimal.TryParse(If(texte.Trim() = "", "0", texte.Trim()), v) Then
                Return v
            End If
            Return 0D
        End Function

        Private Function ExtraireDecimal(texte As String) As Decimal
            If texte Is Nothing Then Return 0D
            Dim parts As String() = texte.Split(" "c)
            For Each p As String In parts
                Dim v As Decimal
                If Decimal.TryParse(p.Replace(":", ""), v) Then
                    Return v
                End If
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
            Dim baseRef As String = GenererReference(libelle, categorieId)
            Return baseRef & "-" & Date.Now.ToString("HHmmss")
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
    End Class
End Namespace
