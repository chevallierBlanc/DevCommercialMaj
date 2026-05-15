Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.IO
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FacturationForm
        Inherits Form

        ' --- Couleurs du Thème ---
        Private ReadOnly ColorPrimary As Color = Color.FromArgb(41, 128, 185) ' Bleu Moderne
        Private ReadOnly ColorSecondary As Color = Color.FromArgb(52, 73, 94) ' Gris Foncé
        Private ReadOnly ColorAccent As Color = Color.FromArgb(39, 174, 96) ' Vert Succès
        Private ReadOnly ColorDanger As Color = Color.FromArgb(192, 57, 43) ' Rouge Annuler
        Private ReadOnly ColorBg As Color = Color.FromArgb(245, 247, 250) ' Gris très clair
        Private ReadOnly FontControl As New Font("Segoe UI", 9.5F)
        Private ReadOnly ColorWhite As Color = Color.White
        Private ReadOnly FontMain As New Font("Segoe UI", 10)
        Private ReadOnly FontBold As New Font("Segoe UI", 10, FontStyle.Bold)
        Private ReadOnly FontTitle As New Font("Segoe UI", 14, FontStyle.Bold)

        ' --- Composants ---


        Private ReadOnly txtNumeroFacture As TextBox
        Private ReadOnly txtClientId As TextBox
        Private ReadOnly txtClientNom As TextBox
        Private ReadOnly txtClientTel As TextBox

        Private ReadOnly txtRecherche As TextBox
        Private ReadOnly btnActualiser As Button
        Private ReadOnly gridProduits As DataGridView
        Private ReadOnly txtQuantite As TextBox
        Private ReadOnly cmbUnite As ComboBox
        Private ReadOnly txtPrixUnitaire As TextBox
        Private ReadOnly lblStock As Label
        Private ReadOnly lblEquivalent As Label ' nouveau 
        Private ReadOnly lblTotalReel As Label 'nouveau 

        Private ReadOnly gridPanier As DataGridView
        Private ReadOnly txtRemise As TextBox
        Private ReadOnly lblSousTotal As Label
        Private ReadOnly lblTotal As Label

        Private ReadOnly btnAjouter As Button
        Private ReadOnly btnRetirer As Button
        Private ReadOnly btnValider As Button
        Private ReadOnly btnImprimer As Button
        Private ReadOnly btnPdf As Button
        Private ReadOnly btnExcel As Button
        Private ReadOnly btnHistorique As Button
        Private ReadOnly btnAnnuler As Button
        Private ReadOnly btnDeconnexion As Button

        Private ReadOnly _panier As List(Of PanierLigne)
        Private ReadOnly _typeVenteService As TypeVenteService 'nouveau
        Private _remiseMax As Decimal
        Private _produitsTable As DataTable
        Private _produitsView As DataView
        Private _parametres As ParametreDTO
        Private _typesVenteCourants As List(Of TypeVenteDTO) 'nouveau 

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
            ' Configuration de la Form
            Me.BackColor = ColorBg
            Me.Text = "Système de Facturation Professionnel"
            Me.Width = 1300
            Me.Height = 820
            Me.Font = FontMain
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False

            _panier = New List(Of PanierLigne)()
            _typeVenteService = New TypeVenteService()
            _typesVenteCourants = New List(Of TypeVenteDTO)()

            ' --- Header Panel ---
            Dim pnlHeader As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 70,
                .BackColor = Color.FromArgb(44, 62, 80)
            }
            Dim lblAppTitle As New Label() With {
                .Text = "GESTION DE FACTURATION",
                .ForeColor = ColorWhite,
                .Font = FontTitle,
                .AutoSize = True,
                .Left = 20,
                .Top = 20
            }
            Dim lblNumFactLabel As New Label() With {
                .Text = "N° FACTURE :",
                .ForeColor = ColorWhite,
                .Font = FontBold,
                .AutoSize = True,
                .Left = 950,
                .Top = 25
            }
            txtNumeroFacture = New TextBox() With {
                .Left = 1060, .Top = 22, .Width = 180,
                .Enabled = False, .BackColor = ColorWhite,
                .BorderStyle = BorderStyle.FixedSingle,
                .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                .TextAlign = HorizontalAlignment.Center
            }
            pnlHeader.Controls.Add(lblAppTitle)
            pnlHeader.Controls.Add(lblNumFactLabel)
            pnlHeader.Controls.Add(txtNumeroFacture)

            ' --- Main Container ---
            Dim pnlMain As New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(20)
            }

            ' --- Left Side (Client & Produits) ---
            Dim pnlLeft As New Panel() With {
                .Width = 550,
                .Dock = DockStyle.Left
            }

            ' GroupBox Client
            Dim grpClient As New GroupBox() With {
                .Text = "INFORMATIONS CLIENT",
                .Dock = DockStyle.Top,
                .Height = 160,
                .Font = FontBold,
                .ForeColor = ColorSecondary,
                .Padding = New Padding(10)
            }

            ' Dim lblClientId As New Label() With {.Text = "ID Client", .Left = 20, .Top = 35, .AutoSize = True, .Font = FontMain}
            txtClientId = New TextBox() With {.Left = 140, .Top = 32, .Width = 100, .Enabled = False, .BorderStyle = BorderStyle.FixedSingle, .Visible = False}

            Dim lblClientNom As New Label() With {.Text = "Nom Complet", .Left = 20, .Top = 75, .AutoSize = True, .Font = FontMain}
            txtClientNom = New TextBox() With {.Left = 140, .Top = 72, .Width = 380, .BorderStyle = BorderStyle.FixedSingle}

            Dim lblClientTel As New Label() With {.Text = "Téléphone", .Left = 20, .Top = 115, .AutoSize = True, .Font = FontMain}
            txtClientTel = New TextBox() With {.Left = 140, .Top = 112, .Width = 200, .BorderStyle = BorderStyle.FixedSingle}

            grpClient.Controls.AddRange({txtClientId, lblClientNom, txtClientNom, lblClientTel, txtClientTel})

            ' GroupBox Produits
            Dim grpProduits As New GroupBox() With {
                .Text = "SÉLECTION DES PRODUITS",
                .Dock = DockStyle.Fill,
                .Font = FontBold,
                .ForeColor = ColorSecondary,
                .Padding = New Padding(10),
                .Top = 170
            }

            Dim lblRecherche As New Label() With {.Text = "Rechercher", .Left = 20, .Top = 35, .AutoSize = True, .Font = FontMain}
            txtRecherche = New TextBox() With {.Left = 120, .Top = 32, .Width = 280, .BorderStyle = BorderStyle.FixedSingle}
            btnActualiser = New Button() With {
                .Text = "Actualiser", .Left = 410, .Top = 30, .Width = 110, .Height = 30,
                .FlatStyle = FlatStyle.Flat, .BackColor = ColorPrimary, .ForeColor = ColorWhite, .Cursor = Cursors.Hand
            }
            btnActualiser.FlatAppearance.BorderSize = 0

            gridProduits = New DataGridView() With {
                .Left = 20, .Top = 75, .Width = 500, .Height = 280,
                .ReadOnly = True, .BorderStyle = BorderStyle.None,
                .BackgroundColor = ColorWhite, .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle() With {.BackColor = Color.FromArgb(240, 240, 240)}
            }
            gridProduits.ColumnHeadersDefaultCellStyle.BackColor = ColorSecondary
            gridProduits.ColumnHeadersDefaultCellStyle.ForeColor = ColorWhite
            gridProduits.EnableHeadersVisualStyles = False

            Dim lblQuantite As New Label() With {.Text = "Qté", .Left = 20, .Top = 375, .AutoSize = True, .Font = FontMain}
            txtQuantite = New TextBox() With {.Left = 60, .Top = 372, .Width = 60, .BorderStyle = BorderStyle.FixedSingle, .TextAlign = HorizontalAlignment.Center}

            Dim lblUnite As New Label() With {.Text = "Unité", .Left = 135, .Top = 375, .AutoSize = True, .Font = FontMain}
            cmbUnite = New ComboBox() With {.Left = 185, .Top = 372, .Width = 100, .DropDownStyle = ComboBoxStyle.DropDownList}

            Dim lblPrix As New Label() With {.Text = "Prix Unitaire", .Left = 300, .Top = 375, .AutoSize = True, .Font = FontMain}
            txtPrixUnitaire = New TextBox() With {.Left = 400, .Top = 372, .Width = 120, .ReadOnly = True, .BorderStyle = BorderStyle.FixedSingle, .BackColor = Color.FromArgb(230, 230, 230), .TextAlign = HorizontalAlignment.Right}

            lblStock = New Label() With {.Left = 20, .Top = 410, .AutoSize = True, .ForeColor = ColorDanger, .Font = New Font("Segoe UI", 9, FontStyle.Italic)}
            lblEquivalent = New Label() With {.Left = 20, .Top = 432, .AutoSize = True, .ForeColor = ColorDanger, .Font = New Font("Segoe UI", 9, FontStyle.Italic)} '#########nouveau
            lblTotalReel = New Label() With {.Left = 20, .Top = 454, .AutoSize = True, .ForeColor = ColorDanger, .Font = New Font("Segoe UI", 9, FontStyle.Italic)} '########### nouveau


            btnAjouter = New Button() With {
                .Text = "AJOUTER AU PANIER", .Left = 20, .Top = 477, .Width = 240, .Height = 45,
                .FlatStyle = FlatStyle.Flat, .BackColor = ColorAccent, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand
            }
            btnAjouter.FlatAppearance.BorderSize = 0

            btnRetirer = New Button() With {
                .Text = "RETIRER", .Left = 280, .Top = 477, .Width = 240, .Height = 45,
                .FlatStyle = FlatStyle.Flat, .BackColor = ColorDanger, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand
            }
            btnRetirer.FlatAppearance.BorderSize = 0

            grpProduits.Controls.AddRange({lblRecherche, txtRecherche, btnActualiser, gridProduits, lblQuantite, txtQuantite, lblUnite, cmbUnite, lblPrix, txtPrixUnitaire, lblStock, lblEquivalent, lblTotalReel, btnAjouter, btnRetirer})

            pnlLeft.Controls.Add(grpProduits)
            pnlLeft.Controls.Add(grpClient)

            ' --- Right Side (Panier & Actions) ---
            Dim pnlRight As New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(20, 0, 0, 0)
            }

            Dim grpPanier As New GroupBox() With {
                .Text = "PANIER DE VENTE",
                .Dock = DockStyle.Top,
                .Height = 420,
                .Font = FontBold,
                .ForeColor = ColorSecondary
            }
            gridPanier = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True, .BorderStyle = BorderStyle.None,
                .BackgroundColor = ColorWhite, .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle() With {.BackColor = Color.FromArgb(240, 240, 240)}
            }
            'gridPanier.ColumnHeadersDefaultCellStyle.BackColor = ColorSecondary
            'gridPanier.ColumnHeadersDefaultCellStyle.ForeColor = ColorWhite
            'gridPanier.EnableHeadersVisualStyles = False

            gridPanier = CreateStyledGrid()
            gridPanier.Dock = DockStyle.Fill


            grpPanier.Controls.Add(gridPanier)

            ' Totaux Panel
            Dim pnlTotals As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 100,
                .BackColor = ColorWhite,
                .Padding = New Padding(10)
            }
            pnlTotals.BorderStyle = BorderStyle.FixedSingle

            Dim lblRemiseLabel As New Label() With {.Text = "REMISE (%)", .Left = 20, .Top = 15, .AutoSize = True, .Font = FontBold}
            txtRemise = New TextBox() With {.Left = 120, .Top = 12, .Width = 60, .BorderStyle = BorderStyle.FixedSingle, .TextAlign = HorizontalAlignment.Center}

            lblSousTotal = New Label() With {
                .Text = "SOUS-TOTAL : 0.00", .Left = 200, .Top = 15, .Width = 200,
                .Font = New Font("Segoe UI", 11, FontStyle.Bold), .ForeColor = ColorSecondary
            }

            lblTotal = New Label() With {
                .Text = "TOTAL À PAYER : 0.00", .Left = 400, .Top = 10, .Width = 250,
                .Font = New Font("Segoe UI", 14, FontStyle.Bold), .ForeColor = ColorPrimary,
                .TextAlign = ContentAlignment.MiddleRight
            }
            pnlTotals.Controls.AddRange({lblRemiseLabel, txtRemise, lblSousTotal, lblTotal})

            ' Actions Panel
            Dim pnlActions As New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(0, 20, 0, 0)
            }

            btnValider = New Button() With {.Text = "VALIDER LA VENTE", .Width = 210, .Height = 50, .FlatStyle = FlatStyle.Flat, .BackColor = ColorAccent, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}
            btnImprimer = New Button() With {.Text = "IMPRIMER A4", .Width = 140, .Height = 50, .FlatStyle = FlatStyle.Flat, .BackColor = ColorSecondary, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}
            btnPdf = New Button() With {.Text = "PDF", .Width = 100, .Height = 50, .FlatStyle = FlatStyle.Flat, .BackColor = ColorSecondary, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}
            btnExcel = New Button() With {.Text = "EXCEL", .Width = 100, .Height = 50, .FlatStyle = FlatStyle.Flat, .BackColor = ColorSecondary, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}

            btnHistorique = New Button() With {.Text = "HISTORIQUE", .Width = 150, .Height = 40, .FlatStyle = FlatStyle.Flat, .BackColor = ColorPrimary, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}
            btnAnnuler = New Button() With {.Text = "ANNULER", .Width = 150, .Height = 40, .FlatStyle = FlatStyle.Flat, .BackColor = ColorDanger, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}
            btnDeconnexion = New Button() With {.Text = "DÉCONNEXION", .Width = 150, .Height = 40, .FlatStyle = FlatStyle.Flat, .BackColor = ColorSecondary, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}

            For Each btn As Button In {btnValider, btnImprimer, btnPdf, btnExcel, btnHistorique, btnAnnuler, btnDeconnexion}
                btn.FlatAppearance.BorderSize = 0
                pnlActions.Controls.Add(btn)
            Next

            pnlRight.Controls.Add(pnlActions)
            pnlRight.Controls.Add(pnlTotals)
            pnlRight.Controls.Add(grpPanier)

            pnlMain.Controls.Add(pnlRight)
            pnlMain.Controls.Add(pnlLeft)

            Me.Controls.Add(pnlMain)
            Me.Controls.Add(pnlHeader)

            ' --- Handlers ---

            AddHandler txtRecherche.TextChanged, AddressOf FiltrerProduits
            AddHandler btnActualiser.Click, AddressOf RechargerProduits
            AddHandler gridProduits.SelectionChanged, AddressOf ChargerUnites
            AddHandler gridProduits.RowPrePaint, AddressOf ColorerStockCritique
            AddHandler cmbUnite.SelectedIndexChanged, AddressOf MiseAJourPrixUnitaire
            AddHandler txtQuantite.TextChanged, AddressOf MiseAJourIndicateursQuantite
            AddHandler btnAjouter.Click, AddressOf AjouterAuPanier
            AddHandler btnRetirer.Click, AddressOf RetirerDuPanier
            AddHandler btnValider.Click, AddressOf ValiderFacture
            AddHandler btnImprimer.Click, AddressOf ImprimerA4
            AddHandler btnPdf.Click, AddressOf ExporterPdf
            AddHandler btnExcel.Click, AddressOf ExporterExcel
            AddHandler btnHistorique.Click, AddressOf OuvrirHistorique
            AddHandler btnAnnuler.Click, AddressOf AnnulerFacture
            AddHandler btnDeconnexion.Click, AddressOf Deconnecter
            AddHandler txtClientTel.TextChanged, AddressOf RechercherClientParTelephone

            ' Initialisation
            ChargerParametres()
            ChargerProduits()
            GenererNouveauNumeroFacture()
            ConfigurerGrilleChargerProduit()
        End Sub



        Private Sub ConfigurerGrilleChargerProduit()
            gridProduits.Columns.Clear()
            gridProduits.AutoGenerateColumns = False
            Dim colProduitId As New DataGridViewTextBoxColumn() With {.DataPropertyName = "ProduitId", .Name = "ProduitId", .Visible = False}
            Dim colCodeBarres As New DataGridViewTextBoxColumn() With {.DataPropertyName = "CodeBarres", .HeaderText = "CodeBarres", .Width = 210}
            Dim colLibelle As New DataGridViewTextBoxColumn() With {.DataPropertyName = "Libelle", .HeaderText = "Libelle", .Width = 250}
            Dim colPrixDetail As New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixDetail", .HeaderText = "Prix Detail", .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}}
            Dim colPrixAchat As New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixAchat", .HeaderText = "PrixAchat", .Width = 80, .Visible = False, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}}
            Dim colPrixDemi As New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixDemi", .HeaderText = "Prix Demi", .Width = 100, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}}
            Dim colPrixQuart As New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixQuart", .HeaderText = "Prix Quart", .Width = 100, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}}
            Dim colPrixDouzaine As New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixDouzaine", .HeaderText = "Prix Douzaine", .Width = 100, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}}
            Dim colPrixGros As New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixGros", .HeaderText = "Prix Gros", .Width = 80, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}}
            Dim colPrixSpecial As New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixSpecial", .HeaderText = "PrixSpecial", .Width = 80, .Visible = False}
            Dim colCoefficientGros As New DataGridViewTextBoxColumn() With {.DataPropertyName = "CoefficientGros", .HeaderText = "CoefficientGros", .Width = 80, .Visible = False}
            Dim colQuantiteStock As New DataGridViewTextBoxColumn() With {.DataPropertyName = "QuantiteStock", .HeaderText = "Qte Stock", .Width = 100, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}}
            Dim colSeuilCritique As New DataGridViewTextBoxColumn() With {.DataPropertyName = "SeuilCritique", .HeaderText = "SeuilCritique", .Width = 80, .Visible = False}
            Dim colDateExpiration As New DataGridViewTextBoxColumn() With {.DataPropertyName = "DateExpiration", .HeaderText = "DateExpiration", .Width = 80, .Visible = False}
            Dim colCategorieId As New DataGridViewTextBoxColumn() With {.DataPropertyName = "CategorieId", .HeaderText = "CategorieId", .Width = 80, .Visible = False}
            Dim colEstActif As New DataGridViewTextBoxColumn() With {.DataPropertyName = "EstActif", .HeaderText = "EstActif", .Width = 80, .Visible = False}
            Dim colUnitePrincipale As New DataGridViewTextBoxColumn() With {.DataPropertyName = "UnitePrincipale", .HeaderText = "UnitePrincipale", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}}
            Dim colUniteSecondaire As New DataGridViewTextBoxColumn() With {.DataPropertyName = "UniteSecondaire", .HeaderText = "UniteSecondaire", .Width = 80, .Visible = False}
            Dim colConversionUnite As New DataGridViewTextBoxColumn() With {.DataPropertyName = "ConversionUnite", .HeaderText = "ConversionUnite", .Width = 150, .Visible = True}
            Dim colVenteDetail As New DataGridViewTextBoxColumn() With {.DataPropertyName = "VenteDetail", .HeaderText = "VenteDetail", .Width = 80, .Visible = False}
            Dim colSVenteDemi As New DataGridViewTextBoxColumn() With {.DataPropertyName = "VenteDemi", .HeaderText = "VenteDemi", .Width = 80, .Visible = False}
            Dim colVenteDouzaine As New DataGridViewTextBoxColumn() With {.DataPropertyName = "VenteDouzaine", .HeaderText = "VenteDouzaine", .Width = 80, .Visible = False}
            Dim colVenteGros As New DataGridViewTextBoxColumn() With {.DataPropertyName = "VenteGros", .HeaderText = "VenteGros", .Width = 80, .Visible = False}
            gridProduits.Columns.AddRange(New DataGridViewColumn() {colProduitId, colCodeBarres, colLibelle, colPrixDetail, colPrixAchat, colPrixDemi, colPrixQuart, colPrixDouzaine, colPrixGros, colPrixSpecial, colCoefficientGros, colQuantiteStock, colSeuilCritique, colDateExpiration, colCategorieId, colEstActif, colUnitePrincipale, colUniteSecondaire, colConversionUnite, colVenteDetail, colSVenteDemi, colVenteDouzaine, colVenteGros})


        End Sub

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
                .GridColor = ColorBorder
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
        Private Sub ChargerParametres()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim paramService As New ParametreService(New ParametreRepository(dal))
                _parametres = paramService.Charger()
                If _parametres Is Nothing Then Return
                _remiseMax = _parametres.RemiseMaxPourcent
            Catch
            End Try
        End Sub

        Private Sub ChargerProduits()
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New ProduitRepository(dal)
            _produitsTable = repo.ListerTable()
            _produitsView = New DataView(_produitsTable)
            gridProduits.DataSource = _produitsView
        End Sub

        Private Sub RechargerProduits(sender As Object, e As EventArgs)
            ChargerProduits()
            FiltrerProduits(Nothing, EventArgs.Empty)
        End Sub

        Private Sub FiltrerProduits(sender As Object, e As EventArgs)
            If _produitsView Is Nothing Then Return
            Dim q As String = txtRecherche.Text.Trim().Replace("'", "''")
            If q = "" Then
                _produitsView.RowFilter = ""
            Else
                _produitsView.RowFilter = "CodeBarres LIKE '%" & q & "%' OR Libelle LIKE '%" & q & "%'"
            End If
        End Sub

        Private Sub ChargerUnites(sender As Object, e As EventArgs)
            If gridProduits.CurrentRow Is Nothing Then Return
            Dim nbUnites As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells(18).Value)
            Dim prixAchat As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells(4).Value)
            Dim prixGros As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells(8).Value)
            Dim prixDemi As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells(5).Value)
            Dim prixDetail As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells(3).Value)
            Dim prixQuart As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells(6).Value)
            Dim prixDouzaine As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells(7).Value)
            Dim prixSpecial As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells(9).Value)
            Dim venteDetail As Boolean = Convert.ToBoolean(gridProduits.CurrentRow.Cells(19).Value)
            Dim venteDemi As Boolean = Convert.ToBoolean(gridProduits.CurrentRow.Cells(20).Value)
            Dim venteDouzaine As Boolean = Convert.ToBoolean(gridProduits.CurrentRow.Cells(21).Value)
            Dim venteGros As Boolean = Convert.ToBoolean(gridProduits.CurrentRow.Cells(22).Value)

            _typesVenteCourants = _typeVenteService.ConstruireTypesVente(nbUnites, prixAchat, prixGros, prixDemi, prixDetail, prixQuart, prixDouzaine, prixSpecial, venteGros, venteDemi, venteDetail, venteDouzaine)
            cmbUnite.DataSource = Nothing
            cmbUnite.DisplayMember = "NomAffichage"
            cmbUnite.ValueMember = "Nom"
            cmbUnite.DataSource = _typesVenteCourants
            If cmbUnite.Items.Count > 0 Then cmbUnite.SelectedIndex = 0

            MettreAJourAffichageStockProduit()
            MiseAJourPrixUnitaire(Nothing, EventArgs.Empty)
        End Sub

        Private Sub MiseAJourPrixUnitaire(sender As Object, e As EventArgs)
            If gridProduits.CurrentRow Is Nothing Then Return
            Dim typeChoisi As TypeVenteDTO = ObtenirTypeVenteSelectionne()
            Dim prix As Decimal = PrixSelonUnite()
            txtPrixUnitaire.Text = prix.ToString("N2")
            If typeChoisi Is Nothing Then
                lblEquivalent.Text = "Equivalent: 0 pièce / unité"
            Else
                lblEquivalent.Text = "Equivalent: " & typeChoisi.QuantiteEquivalent.ToString("N2") & " pièces / unité"
            End If
            MiseAJourIndicateursQuantite(Nothing, EventArgs.Empty)
        End Sub

        Private Sub ColorerStockCritique(sender As Object, e As DataGridViewRowPrePaintEventArgs)
            Dim row As DataGridViewRow = gridProduits.Rows(e.RowIndex)
            If row.Cells(11).Value Is Nothing OrElse row.Cells(12).Value Is Nothing Then Return
            Dim stock As Decimal = Convert.ToDecimal(row.Cells(11).Value)
            Dim seuil As Decimal = Convert.ToDecimal(row.Cells(12).Value)
            If stock <= seuil Then
                row.DefaultCellStyle.BackColor = Color.LightCoral
            End If
        End Sub

        Private Function PrixSelonUnite() As Decimal
            Dim typeChoisi As TypeVenteDTO = ObtenirTypeVenteSelectionne()
            If typeChoisi Is Nothing Then
                Return 0D
            End If
            Return typeChoisi.PrixVente
        End Function

        Private Function ObtenirTypeVenteSelectionne() As TypeVenteDTO
            Return TryCast(cmbUnite.SelectedItem, TypeVenteDTO)
        End Function

        Private Sub MiseAJourIndicateursQuantite(sender As Object, e As EventArgs)
            Dim qte As Decimal
            If Not Decimal.TryParse(txtQuantite.Text.Trim(), qte) OrElse qte <= 0D Then
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
            If gridProduits.CurrentRow Is Nothing Then Return
            Dim produitId As Integer = Convert.ToInt32(gridProduits.CurrentRow.Cells(0).Value)
            Dim stock As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells(11).Value)
            Dim nbUnites As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells(18).Value)
            Dim uniteBase As String = Convert.ToString(gridProduits.CurrentRow.Cells(16).Value)
            Dim uniteSecondaire As String = Convert.ToString(gridProduits.CurrentRow.Cells(17).Value)
            Dim reserve As Decimal = 0D
            For Each ligne As PanierLigne In _panier
                If ligne.ProduitId = produitId Then
                    reserve += ligne.QuantiteBase
                End If
            Next
            Dim restant As Decimal = Math.Max(0D, stock - reserve)
            lblStock.Text = "Stock: " & _typeVenteService.FormaterStock(stock, nbUnites, If(uniteBase = "", "base", uniteBase), If(uniteSecondaire = "", "pièce", uniteSecondaire)) &
                " | Restant: " & _typeVenteService.FormaterStock(restant, nbUnites, If(uniteBase = "", "base", uniteBase), If(uniteSecondaire = "", "pièce", uniteSecondaire))
        End Sub

        Private Sub AjouterAuPanier(sender As Object, e As EventArgs) '"""#### Nouvelle logique tres bon
            If gridProduits.CurrentRow Is Nothing Then Return

            Dim qte As Decimal
            If Not Decimal.TryParse(txtQuantite.Text.Trim(), qte) OrElse qte <= 0D Then
                MessageBox.Show("Quantite invalide.")
                Return
            End If

            If cmbUnite.SelectedItem Is Nothing Then
                MessageBox.Show("Veuillez choisir l'unite.")
                Return
            End If

            Dim produitId As Integer = Convert.ToInt32(gridProduits.CurrentRow.Cells(0).Value)
            Dim libelle As String = Convert.ToString(gridProduits.CurrentRow.Cells(2).Value)
            Dim typeChoisi As TypeVenteDTO = ObtenirTypeVenteSelectionne()
            If typeChoisi Is Nothing Then
                MessageBox.Show("Type de vente invalide.")
                Return
            End If
            Dim unite As String = typeChoisi.Nom
            Dim prix As Decimal = PrixSelonUnite()
            Dim quantiteEquivalent As Decimal = typeChoisi.QuantiteEquivalent
            Dim quantiteBase As Decimal = qte * quantiteEquivalent
            Dim stock As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells(11).Value)

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

            Dim remisePourcent As Decimal
            If Not Decimal.TryParse(txtRemise.Text.Trim(), remisePourcent) Then
                remisePourcent = 0D
            End If
            If remisePourcent > _remiseMax Then
                MessageBox.Show("Remise superieure au maximum autorise.")
                remisePourcent = _remiseMax
                txtRemise.Text = _remiseMax.ToString()
            End If

            Dim remiseMontant As Decimal = sousTotal * remisePourcent / 100D
            Dim total As Decimal = sousTotal - remiseMontant

            lblSousTotal.Text = "Sous-total: " & sousTotal.ToString()
            lblTotal.Text = "Total: " & total.ToString()
            MettreAJourAffichageStockProduit()
        End Sub

        Private Sub RechercherClientParTelephone(sender As Object, e As EventArgs)
            Dim tel As String = txtClientTel.Text.Trim()
            If tel = "" Then
                txtClientId.Text = ""
                Return
            End If

            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim service As New ClientService(New ClientRepository(dal))
                Dim c As ClientDTO = service.ObtenirParTelephone(tel)
                If c IsNot Nothing Then
                    txtClientId.Text = c.ClientId.ToString()
                    txtClientNom.Text = c.NomClient
                Else
                    txtClientId.Text = ""
                End If
            Catch
            End Try
        End Sub

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

        Private Sub ValiderFacture(sender As Object, e As EventArgs)
            Try
                If _panier.Count = 0 Then
                    MessageBox.Show("Panier vide.")
                    Return
                End If

                If Not VerifierStockAvantValidation() Then
                    Return
                End If

                Dim numeroFacture As String = txtNumeroFacture.Text.Trim()
                If numeroFacture = "" Then
                    MessageBox.Show("Numero de facture invalide.")
                    Return
                End If

                Me.UseWaitCursor = True
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim service As New FacturationService(dal)
                Dim clientService As New ClientService(New ClientRepository(dal))

                Dim sousTotal As Decimal = 0D
                For Each l As PanierLigne In _panier
                    sousTotal += l.Total
                Next

                Dim remisePourcent As Decimal
                Decimal.TryParse(txtRemise.Text.Trim(), remisePourcent)
                Dim remiseMontant As Decimal = sousTotal * remisePourcent / 100D
                Dim total As Decimal = sousTotal - remiseMontant

                Dim clientId As Integer? = Nothing
                Dim tel As String = txtClientTel.Text.Trim()
                Dim nom As String = txtClientNom.Text.Trim()

                If tel <> "" Then
                    Dim c As ClientDTO = clientService.ObtenirParTelephone(tel)
                    If c IsNot Nothing Then
                        clientId = c.ClientId
                    Else
                        If nom = "" Then
                            MessageBox.Show("Veuillez saisir le nom du client pour ce numero.")
                            Return
                        End If
                        Dim nouveau As New Client With {
                            .NomClient = nom,
                            .Telephone = tel,
                            .Email = "",
                            .Adresse = "",
                            .LimiteCredit = 0D,
                            .EstActif = True
                        }
                        clientId = clientService.Ajouter(nouveau)
                    End If
                ElseIf nom <> "" Then
                    Dim nouveau As New Client With {
                        .NomClient = nom,
                        .Telephone = "",
                        .Email = "",
                        .Adresse = "",
                        .LimiteCredit = 0D,
                        .EstActif = True
                    }
                    clientId = clientService.Ajouter(nouveau)
                End If

                Dim factureId As Integer = service.CreerFacture(numeroFacture, clientId, sousTotal, remiseMontant, 0D, total, SessionUtilisateur.UtilisateurId)
                For Each l As PanierLigne In _panier
                    service.AjouterLigne(factureId, l.ProduitId, l.QuantiteBase, l.QuantiteEquivalente, l.Unite, l.PrixUnitaire, 0D, l.Quantite)
                Next

                MessageBox.Show("Facture en attente: " & numeroFacture)
                _panier.Clear()
                RafraichirPanier()
                ChargerProduits()
                GenererNouveauNumeroFacture()
            Catch ex As Exception
                MessageBox.Show("Erreur validation facture: " & ex.Message)
            Finally
                Me.UseWaitCursor = False
            End Try
        End Sub

        Private Sub GenererNouveauNumeroFacture()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim repo As New FactureVenteRepository(dal)
                txtNumeroFacture.Text = repo.GenererNumeroFacture()
            Catch
                txtNumeroFacture.Text = ""
            End Try
        End Sub

        Private Sub ImprimerA4(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                _parametres = (New ParametreService(New ParametreRepository(dal))).Charger()

                Dim doc As New Printing.PrintDocument()
                If _parametres IsNot Nothing AndAlso _parametres.ImprimanteA4 <> "" Then
                    doc.PrinterSettings.PrinterName = _parametres.ImprimanteA4
                End If
                doc.DefaultPageSettings.Color = If(_parametres IsNot Nothing, _parametres.ImpressionCouleur, True)
                AddHandler doc.PrintPage, AddressOf ImprimerPage

                If _parametres IsNot Nothing AndAlso _parametres.ApercuAvantImpression Then
                    Dim preview As New PrintPreviewDialog()
                    preview.Document = doc
                    preview.ShowDialog()
                Else
                    doc.Print()
                End If
            Catch ex As Exception
                MessageBox.Show("Erreur impression: " & ex.Message)
            End Try
        End Sub

        Private Sub ExporterPdf(sender As Object, e As EventArgs)
            Try
                Dim sfd As New SaveFileDialog() With {.Filter = "PDF (*.pdf)|*.pdf"}
                If sfd.ShowDialog() <> DialogResult.OK Then Return
                Dim lignes As List(Of String) = ConstruireLignesExport()
                PdfHelper.GenererPdfSimple(sfd.FileName, "FACTURE", lignes)
                MessageBox.Show("PDF genere.")
            Catch ex As Exception
                MessageBox.Show("Erreur PDF: " & ex.Message)
            End Try
        End Sub

        Private Sub ExporterExcel(sender As Object, e As EventArgs)
            Try
                Dim sfd As New SaveFileDialog() With {.Filter = "Excel CSV (*.csv)|*.csv"}
                If sfd.ShowDialog() <> DialogResult.OK Then Return
                Dim lignes As List(Of String) = ConstruireLignesExportCsv()
                File.WriteAllLines(sfd.FileName, lignes)
                MessageBox.Show("Export CSV genere.")
            Catch ex As Exception
                MessageBox.Show("Erreur export CSV: " & ex.Message)
            End Try
        End Sub

        Private Function ConstruireLignesExport() As List(Of String)
            Dim lignes As New List(Of String)()
            Dim nomMag As String = If(_parametres IsNot Nothing, _parametres.NomMagasin, "")
            Dim adr As String = If(_parametres IsNot Nothing, _parametres.AdresseMagasin, "")
            Dim tel As String = If(_parametres IsNot Nothing, _parametres.TelephoneMagasin, "")

            lignes.Add(nomMag)
            lignes.Add(adr)
            lignes.Add(tel)
            lignes.Add("Facture: " & txtNumeroFacture.Text.Trim())
            lignes.Add("Date: " & Date.Now.ToString("dd/MM/yyyy HH:mm"))
            lignes.Add("Client: " & txtClientNom.Text.Trim())
            lignes.Add("Telephone: " & txtClientTel.Text.Trim())
            lignes.Add(" ")

            For Each l As PanierLigne In _panier
                lignes.Add(l.Libelle & " " & l.Unite & " x" & l.Quantite.ToString() & " = " & l.Total.ToString())
            Next

            Dim sousTotal As Decimal = 0D
            For Each l As PanierLigne In _panier
                sousTotal += l.Total
            Next
            Dim remisePourcent As Decimal
            Decimal.TryParse(txtRemise.Text.Trim(), remisePourcent)
            Dim remiseMontant As Decimal = sousTotal * remisePourcent / 100D
            Dim total As Decimal = sousTotal - remiseMontant

            lignes.Add(" ")
            lignes.Add("Sous-total: " & sousTotal.ToString())
            lignes.Add("Remise: " & remiseMontant.ToString())
            lignes.Add("Total: " & total.ToString())

            Return lignes
        End Function

        Private Function ConstruireLignesExportCsv() As List(Of String)
            Dim lignes As New List(Of String)()
            lignes.Add("Type;Valeur")
            lignes.Add("Facture;" & txtNumeroFacture.Text.Trim())
            lignes.Add("Date;" & Date.Now.ToString("dd/MM/yyyy HH:mm"))
            lignes.Add("Client;" & txtClientNom.Text.Trim())
            lignes.Add("Telephone;" & txtClientTel.Text.Trim())
            lignes.Add(" ")
            lignes.Add("Libelle;Unite;Quantite;PrixUnitaire;Total")

            For Each l As PanierLigne In _panier
                lignes.Add(l.Libelle & ";" & l.Unite & ";" & l.Quantite.ToString() & ";" & l.PrixUnitaire.ToString() & ";" & l.Total.ToString())
            Next

            Dim sousTotal As Decimal = 0D
            For Each l As PanierLigne In _panier
                sousTotal += l.Total
            Next
            Dim remisePourcent As Decimal
            Decimal.TryParse(txtRemise.Text.Trim(), remisePourcent)
            Dim remiseMontant As Decimal = sousTotal * remisePourcent / 100D
            Dim total As Decimal = sousTotal - remiseMontant

            lignes.Add("Sous-total;" & sousTotal.ToString())
            lignes.Add("Remise;" & remiseMontant.ToString())
            lignes.Add("Total;" & total.ToString())

            Return lignes
        End Function

        Private Sub ImprimerPage(sender As Object, e As Printing.PrintPageEventArgs)
            Dim y As Integer = 20
            Dim x As Integer = 20

            If _parametres IsNot Nothing AndAlso _parametres.LogoPath <> "" AndAlso File.Exists(_parametres.LogoPath) Then
                Using img As Image = Image.FromFile(_parametres.LogoPath)
                    e.Graphics.DrawImage(img, x, y, 60, 60)
                End Using
                x += 70
            End If

            Dim nomMag As String = If(_parametres IsNot Nothing, _parametres.NomMagasin, "")
            Dim adr As String = If(_parametres IsNot Nothing, _parametres.AdresseMagasin, "")
            Dim tel As String = If(_parametres IsNot Nothing, _parametres.TelephoneMagasin, "")

            e.Graphics.DrawString(nomMag, New Font("Segoe UI", 14, FontStyle.Bold), Brushes.Black, x, y)
            y += 24
            e.Graphics.DrawString(adr, New Font("Segoe UI", 10), Brushes.Black, x, y)
            y += 18
            e.Graphics.DrawString(tel, New Font("Segoe UI", 10), Brushes.Black, x, y)
            y += 26

            e.Graphics.DrawString("Facture: " & txtNumeroFacture.Text.Trim(), New Font("Segoe UI", 10, FontStyle.Bold), Brushes.Black, 20, y)
            y += 18
            e.Graphics.DrawString("Date: " & Date.Now.ToString("dd/MM/yyyy HH:mm"), New Font("Segoe UI", 10), Brushes.Black, 20, y)
            y += 18
            e.Graphics.DrawString("Client: " & txtClientNom.Text.Trim(), New Font("Segoe UI", 10), Brushes.Black, 20, y)
            y += 18
            e.Graphics.DrawString("Telephone: " & txtClientTel.Text.Trim(), New Font("Segoe UI", 10), Brushes.Black, 20, y)
            y += 24

            e.Graphics.DrawString("DETAILS", New Font("Segoe UI", 11, FontStyle.Bold), Brushes.Black, 20, y)
            y += 20

            For Each l As PanierLigne In _panier
                Dim line As String = l.Libelle & " " & l.Unite & " x" & l.Quantite.ToString() & " = " & l.Total.ToString()
                e.Graphics.DrawString(line, New Font("Segoe UI", 10), Brushes.Black, 20, y)
                y += 18
            Next

            Dim sousTotal As Decimal = 0D
            For Each l As PanierLigne In _panier
                sousTotal += l.Total
            Next
            Dim remisePourcent As Decimal
            Decimal.TryParse(txtRemise.Text.Trim(), remisePourcent)
            Dim remiseMontant As Decimal = sousTotal * remisePourcent / 100D
            Dim total As Decimal = sousTotal - remiseMontant

            y += 10
            e.Graphics.DrawString("Sous-total: " & sousTotal.ToString(), New Font("Segoe UI", 10), Brushes.Black, 20, y)
            y += 18
            e.Graphics.DrawString("Remise: " & remiseMontant.ToString(), New Font("Segoe UI", 10), Brushes.Black, 20, y)
            y += 18
            e.Graphics.DrawString("Total: " & total.ToString(), New Font("Segoe UI", 11, FontStyle.Bold), Brushes.Black, 20, y)
        End Sub

        Private Sub OuvrirHistorique(sender As Object, e As EventArgs)
            Dim f As New FormulaireFactures()
            f.ShowDialog()
        End Sub

        Private Sub AnnulerFacture(sender As Object, e As EventArgs)
            _panier.Clear()
            txtClientId.Text = ""
            txtClientNom.Text = ""
            txtClientTel.Text = ""
            txtRemise.Text = ""
            RafraichirPanier()
            GenererNouveauNumeroFacture()
        End Sub

        Private Sub Deconnecter(sender As Object, e As EventArgs)
            Dim main As Form = Me.FindForm()
            If main IsNot Nothing Then
                main.Close()
            End If
        End Sub
    End Class
End Namespace
