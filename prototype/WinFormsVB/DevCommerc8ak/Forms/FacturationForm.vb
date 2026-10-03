Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
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
        Private ReadOnly ColorTextPrimary As Color = Color.FromArgb(33, 33, 33) ' Texte foncé
        Private ReadOnly ColorCardBg As Color = Color.White ' Fond des cartes blanc

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
        Private ReadOnly btnHistorique As Button
        Private ReadOnly btnAnnuler As Button
        Private ReadOnly btnDeconnexion As Button

        Private ReadOnly _panier As List(Of PanierLigne)
        Private ReadOnly _typeVenteService As TypeVenteService 'nouveau
        Private ReadOnly _typeVenteProduitService As TypeVenteProduitService
        Private ReadOnly _conditionnementService As ProduitConditionnementService
        Private _remiseMax As Decimal
        Private _produitsTable As DataTable
        Private _produitsView As DataView
        Private _parametres As ParametreDTO
        Private _typesVenteCourants As List(Of TypeVenteDTO) 'nouveau
        Private _factureEnEditionId As Integer?
        Private _isRefreshingFromEvent As Boolean
        Private _suppressSelectionEvents As Boolean
        Private _dataMonitor As DataChangeMonitorService
        Private _normalisationTelephoneEnCours As Boolean
        Private _proformaA4Index As Integer
        Private ReadOnly _prefixesTelephoneRdc As String() = {"081", "082", "083", "084", "085", "089", "097", "098", "099"}

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
            Me.FormBorderStyle = FormBorderStyle.Sizable
            Me.MaximizeBox = True
            Me.KeyPreview = True
            Me.AutoScaleMode = AutoScaleMode.Dpi
            Me.AutoScroll = True
            Me.MinimumSize = New Size(1080, 700)

            _panier = New List(Of PanierLigne)()
            _typeVenteService = New TypeVenteService()
            _typeVenteProduitService = New TypeVenteProduitService()
            _conditionnementService = New ProduitConditionnementService()
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
                .Top = 25,
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            txtNumeroFacture = New TextBox() With {
                .Left = 1060, .Top = 22, .Width = 180,
                .Enabled = False, .BackColor = ColorWhite,
                .BorderStyle = BorderStyle.FixedSingle,
                .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                .TextAlign = HorizontalAlignment.Center,
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            pnlHeader.Controls.Add(lblAppTitle)
            pnlHeader.Controls.Add(lblNumFactLabel)
            pnlHeader.Controls.Add(txtNumeroFacture)
            AddHandler pnlHeader.Resize,
                Sub()
                    txtNumeroFacture.Left = Math.Max(20, pnlHeader.ClientSize.Width - txtNumeroFacture.Width - 24)
                    lblNumFactLabel.Left = Math.Max(20, txtNumeroFacture.Left - lblNumFactLabel.Width - 12)
                End Sub

            ' --- Main Container ---
            Dim pnlMain As New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(20),
                .AutoScroll = True
            }

            ' --- Left Side (Client & Produits) ---
            Dim pnlLeft As New Panel() With {
                .Width = 550,
                .Dock = DockStyle.Left,
                .MinimumSize = New Size(520, 0)
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
            'gridProduits.ColumnHeadersDefaultCellStyle.BackColor = ColorSecondary
            'gridProduits.ColumnHeadersDefaultCellStyle.ForeColor = ColorWhite
            'gridProduits.EnableHeadersVisualStyles = False

            gridProduits = CreerGrille()
            'gridProduits.Dock = DockStyle.Fill


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
                .Padding = New Padding(20, 0, 0, 0),
                .MinimumSize = New Size(420, 0),
                .AutoScroll = True
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

            txtRemise = New TextBox() With {.Left = 120, .Top = 12, .Width = 60, .BorderStyle = BorderStyle.FixedSingle, .TextAlign = HorizontalAlignment.Center, .Visible = False}

            Dim totalsLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 1
            }
            totalsLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 45.0F))
            totalsLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 55.0F))

            lblSousTotal = New Label() With {
                .Text = "SOUS-TOTAL : 0.00",
                .Dock = DockStyle.Fill,
                .Font = New Font("Segoe UI", 11, FontStyle.Bold),
                .ForeColor = ColorSecondary,
                .TextAlign = ContentAlignment.MiddleLeft
            }

            lblTotal = New Label() With {
                .Text = "TOTAL À PAYER : 0.00",
                .Dock = DockStyle.Fill,
                .Font = New Font("Segoe UI", 14, FontStyle.Bold),
                .ForeColor = ColorPrimary,
                .TextAlign = ContentAlignment.MiddleRight
            }
            totalsLayout.Controls.Add(lblSousTotal, 0, 0)
            totalsLayout.Controls.Add(lblTotal, 1, 0)
            pnlTotals.Controls.Add(totalsLayout)

            ' Actions Panel
            Dim pnlActions As New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(0, 20, 0, 0),
                .WrapContents = True,
                .AutoScroll = True
            }

            btnValider = New Button() With {.Text = "VALIDER LA VENTE", .Width = 210, .Height = 50, .FlatStyle = FlatStyle.Flat, .BackColor = ColorAccent, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}
            btnImprimer = New Button() With {.Text = "IMPRIMER", .Width = 140, .Height = 50, .FlatStyle = FlatStyle.Flat, .BackColor = ColorSecondary, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}
            btnPdf = New Button() With {.Text = "PDF", .Width = 100, .Height = 50, .FlatStyle = FlatStyle.Flat, .BackColor = ColorSecondary, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}

            btnHistorique = New Button() With {.Text = "HISTORIQUE", .Width = 150, .Height = 40, .FlatStyle = FlatStyle.Flat, .BackColor = ColorPrimary, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}
            btnAnnuler = New Button() With {.Text = "ANNULER", .Width = 150, .Height = 40, .FlatStyle = FlatStyle.Flat, .BackColor = ColorDanger, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}
            btnDeconnexion = New Button() With {.Text = "DÉCONNEXION", .Width = 150, .Height = 40, .FlatStyle = FlatStyle.Flat, .BackColor = ColorSecondary, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}

            For Each btn As Button In {btnValider, btnImprimer, btnPdf, btnHistorique, btnAnnuler, btnDeconnexion}
                btn.FlatAppearance.BorderSize = 0
                pnlActions.Controls.Add(btn)
            Next

            Dim pnlRaccourcis As New Panel() With {.Dock = DockStyle.Bottom, .Height = 44, .BackColor = Color.White, .Padding = New Padding(6, 0, 0, 0)}
            Dim lblRaccourcis As New Label() With {
                .Dock = DockStyle.Fill,
                .Font = New Font("Segoe UI", 7.5F),
                .ForeColor = Color.FromArgb(120, 120, 120),
                .Text = "F2: Rechercher produit    Entrée: Ajouter au panier    F5: Actualiser    Ctrl+P: Imprimer    Échap: Effacer"
            }
            pnlRaccourcis.Controls.Add(lblRaccourcis)
            pnlActions.Controls.Add(pnlRaccourcis)

            pnlRight.Controls.Add(pnlActions)
            pnlRight.Controls.Add(pnlTotals)
            pnlRight.Controls.Add(grpPanier)

            pnlMain.Controls.Add(pnlRight)
            pnlMain.Controls.Add(pnlLeft)

            Me.Controls.Add(pnlMain)
            Me.Controls.Add(pnlHeader)
            Me.ResizeRedraw = True

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
            AddHandler btnHistorique.Click, AddressOf OuvrirHistorique
            AddHandler btnAnnuler.Click, AddressOf AnnulerFacture
            AddHandler btnDeconnexion.Click, AddressOf Deconnecter
            AddHandler txtClientTel.TextChanged, AddressOf RechercherClientParTelephone
            AddHandler txtClientTel.KeyPress, AddressOf Telephone_KeyPress
            AddHandler txtClientTel.TextChanged, AddressOf NormaliserTelephoneClient

            ' Initialisation
            ChargerParametres()
            ChargerProduits()
            GenererNouveauNumeroFacture()
            ConfigurerGrilleChargerProduit()
            MettreAJourBoutonsPanier()
            AddHandler AppEvents.ProduitModifie, AddressOf RafraichirProduitsDepuisEvenement
            AddHandler AppEvents.StockModifie, AddressOf RafraichirProduitsDepuisEvenement
            _dataMonitor = New DataChangeMonitorService(New String() {"PRODUITS", "STOCK", "TYPES_VENTE"}, 5000)
            AddHandler _dataMonitor.DomaineModifie, AddressOf RafraichirProduitsDepuisVersionSql
            _dataMonitor.Start()
        End Sub

        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
            If keyData = Keys.F2 Then
                txtRecherche.Focus()
                txtRecherche.SelectAll()
                Return True
            End If

            If keyData = Keys.F5 Then
                RechargerProduits(Nothing, EventArgs.Empty)
                Return True
            End If

            If keyData = (Keys.Control Or Keys.P) Then
                ImprimerA4(Nothing, EventArgs.Empty)
                Return True
            End If

            If keyData = Keys.Escape Then
                AnnulerFacture(Nothing, EventArgs.Empty)
                Return True
            End If

            If keyData = Keys.Enter Then
                If txtQuantite.Focused OrElse cmbUnite.Focused OrElse gridProduits.Focused OrElse txtPrixUnitaire.Focused Then
                    AjouterAuPanier(Nothing, EventArgs.Empty)
                    Return True
                End If
            End If

            Return MyBase.ProcessCmdKey(msg, keyData)
        End Function



        Private Sub ConfigurerGrilleChargerProduit()
            gridProduits.Columns.Clear()
            gridProduits.AutoGenerateColumns = False
            Dim colProduitId As New DataGridViewTextBoxColumn() With {.DataPropertyName = "ProduitId", .Name = "ProduitId", .Visible = False}
            Dim colCodeBarres As New DataGridViewTextBoxColumn() With {.DataPropertyName = "CodeBarres", .Name = "CodeBarres", .HeaderText = "CodeBarres", .Width = 120, .MinimumWidth = 90, .FillWeight = 15}
            Dim colLibelle As New DataGridViewTextBoxColumn() With {.DataPropertyName = "Libelle", .Name = "Libelle", .HeaderText = "Libelle", .Width = 320, .MinimumWidth = 220, .FillWeight = 42}
            Dim colPrixDetail As New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixDetail", .Name = "PrixDetail", .HeaderText = "Prix Detail", .Width = 90, .MinimumWidth = 75, .FillWeight = 11, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}}
            Dim colPrixAchat As New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixAchat", .HeaderText = "PrixAchat", .Width = 80, .Visible = False, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}}
            Dim colPrixDemi As New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixDemi", .Name = "PrixDemi", .HeaderText = "Prix Demi", .Width = 85, .MinimumWidth = 70, .FillWeight = 10, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}}
            Dim colPrixQuart As New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixQuart", .Name = "PrixQuart", .HeaderText = "Prix Quart", .Width = 100, .Visible = False, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}}
            Dim colPrixDouzaine As New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixDouzaine", .Name = "PrixDouzaine", .HeaderText = "Prix Douzaine", .Width = 100, .Visible = False, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}}
            Dim colPrixGros As New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixGros", .Name = "PrixGros", .HeaderText = "Prix Gros", .Width = 85, .MinimumWidth = 70, .FillWeight = 10, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}}
            Dim colPrixSpecial As New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixSpecial", .HeaderText = "PrixSpecial", .Width = 80, .Visible = False}
            Dim colCoefficientGros As New DataGridViewTextBoxColumn() With {.DataPropertyName = "CoefficientGros", .HeaderText = "CoefficientGros", .Width = 80, .Visible = False}
            Dim colQuantiteStock As New DataGridViewTextBoxColumn() With {.DataPropertyName = "QuantiteStock", .Name = "QuantiteStock", .HeaderText = "Qte Stock", .Width = 85, .MinimumWidth = 75, .FillWeight = 12, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}}
            Dim colSeuilCritique As New DataGridViewTextBoxColumn() With {.DataPropertyName = "SeuilCritique", .Name = "SeuilCritique", .HeaderText = "SeuilCritique", .Width = 80, .Visible = False}
            Dim colDateExpiration As New DataGridViewTextBoxColumn() With {.DataPropertyName = "DateExpiration", .HeaderText = "DateExpiration", .Width = 80, .Visible = False}
            Dim colCategorieId As New DataGridViewTextBoxColumn() With {.DataPropertyName = "CategorieId", .HeaderText = "CategorieId", .Width = 80, .Visible = False}
            Dim colNomCategorie As New DataGridViewTextBoxColumn() With {.DataPropertyName = "NomCategorie", .HeaderText = "NomCategorie", .Width = 80, .Visible = False}
            Dim colEstActif As New DataGridViewTextBoxColumn() With {.DataPropertyName = "EstActif", .HeaderText = "EstActif", .Width = 80, .Visible = False}
            Dim colUnitePrincipale As New DataGridViewTextBoxColumn() With {.DataPropertyName = "UnitePrincipale", .HeaderText = "UnitePrincipale", .Width = 120, .Visible = False, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}}
            Dim colUniteSecondaire As New DataGridViewTextBoxColumn() With {.DataPropertyName = "UniteSecondaire", .HeaderText = "UniteSecondaire", .Width = 80, .Visible = False}
            Dim colConversionUnite As New DataGridViewTextBoxColumn() With {.DataPropertyName = "ConversionUnite", .HeaderText = "ConversionUnite", .Width = 150, .Visible = False}
            Dim colTypeGestionStock As New DataGridViewTextBoxColumn() With {.DataPropertyName = "TypeGestionStock", .HeaderText = "TypeGestionStock", .Width = 80, .Visible = False}
            Dim colUniteMesureStock As New DataGridViewTextBoxColumn() With {.DataPropertyName = "UniteMesureStock", .HeaderText = "UniteMesureStock", .Width = 80, .Visible = False}
            Dim colContenuUnitePrincipale As New DataGridViewTextBoxColumn() With {.DataPropertyName = "ContenuUnitePrincipale", .HeaderText = "ContenuUnitePrincipale", .Width = 80, .Visible = False}
            Dim colContenuUniteSecondaire As New DataGridViewTextBoxColumn() With {.DataPropertyName = "ContenuUniteSecondaire", .HeaderText = "ContenuUniteSecondaire", .Width = 80, .Visible = False}
            Dim colVenteDetail As New DataGridViewTextBoxColumn() With {.DataPropertyName = "VenteDetail", .HeaderText = "VenteDetail", .Width = 80, .Visible = False}
            Dim colSVenteDemi As New DataGridViewTextBoxColumn() With {.DataPropertyName = "VenteDemi", .HeaderText = "VenteDemi", .Width = 80, .Visible = False}
            Dim colVenteDouzaine As New DataGridViewTextBoxColumn() With {.DataPropertyName = "VenteDouzaine", .HeaderText = "VenteDouzaine", .Width = 80, .Visible = False}
            Dim colVenteGros As New DataGridViewTextBoxColumn() With {.DataPropertyName = "VenteGros", .HeaderText = "VenteGros", .Width = 80, .Visible = False}
            gridProduits.Columns.AddRange(New DataGridViewColumn() {colProduitId, colCodeBarres, colLibelle, colPrixDetail, colPrixAchat, colPrixDemi, colPrixQuart, colPrixDouzaine, colPrixGros, colPrixSpecial, colCoefficientGros, colQuantiteStock, colSeuilCritique, colDateExpiration, colCategorieId, colNomCategorie, colEstActif, colUnitePrincipale, colUniteSecondaire, colConversionUnite, colTypeGestionStock, colUniteMesureStock, colContenuUnitePrincipale, colContenuUniteSecondaire, colVenteDetail, colSVenteDemi, colVenteDouzaine, colVenteGros})


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
                .GridColor = Color.FromArgb(220, 224, 229),
                  .Left = 20, .Top = 75, .Width = 500, .Height = 280
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
            _suppressSelectionEvents = True
            Try
                ChargerProduits()
                FiltrerProduits(Nothing, EventArgs.Empty)
            Finally
                _suppressSelectionEvents = False
            End Try
        End Sub

        Private Sub RafraichirProduitsDepuisEvenement(sender As Object, e As EventArgs)
            If IsDisposed OrElse Disposing Then Return
            If InvokeRequired Then
                BeginInvoke(New MethodInvoker(Sub() RafraichirProduitsDepuisEvenement(Nothing, EventArgs.Empty)))
                Return
            End If
            If _isRefreshingFromEvent Then Return

            _isRefreshingFromEvent = True
            Try
                Dim produitIdSelectionne As Integer? = Nothing
                If gridProduits.CurrentRow IsNot Nothing AndAlso gridProduits.CurrentRow.Cells("ProduitId").Value IsNot Nothing Then
                    produitIdSelectionne = Convert.ToInt32(gridProduits.CurrentRow.Cells("ProduitId").Value)
                End If

                RechargerProduits(Nothing, EventArgs.Empty)

                If produitIdSelectionne.HasValue Then
                    For Each row As DataGridViewRow In gridProduits.Rows
                        If row Is Nothing OrElse row.IsNewRow Then Continue For
                        If Convert.ToInt32(row.Cells("ProduitId").Value) = produitIdSelectionne.Value Then
                            row.Selected = True
                            gridProduits.CurrentCell = row.Cells(1)
                            Exit For
                        End If
                    Next
                End If

                ChargerUnites(Nothing, EventArgs.Empty)
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FacturationForm", "RafraichirProduitsDepuisEvenement", "Erreur lors du rafraichissement automatique des produits.", ex)
            Finally
                _isRefreshingFromEvent = False
            End Try
        End Sub

        Private Sub RafraichirProduitsDepuisVersionSql(sender As Object, e As DataChangeEventArgs)
            If IsDisposed OrElse Disposing OrElse Not IsHandleCreated Then Return
            Try
                BeginInvoke(New MethodInvoker(Sub()
                    If IsDisposed OrElse Disposing Then Return
                    RafraichirProduitsDepuisEvenement(Nothing, EventArgs.Empty)
                End Sub))
            Catch ex As ObjectDisposedException
                Dim log As New ProductionLogService()
                log.Warn("FacturationForm", "RafraichirProduitsDepuisVersionSql", "Formulaire fermé avant rafraîchissement multi-postes : " & ex.Message)
            End Try
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
            If _suppressSelectionEvents OrElse gridProduits.CurrentRow Is Nothing Then Return
            Dim produitId As Integer = SafeInteger(CellValueByProperty(gridProduits.CurrentRow, "ProduitId"))
            If produitId <= 0 Then Return

            Dim nbUnites As Decimal = SafeDecimal(CellValueByProperty(gridProduits.CurrentRow, "ConversionUnite"))
            Dim prixAchat As Decimal = SafeDecimal(CellValueByProperty(gridProduits.CurrentRow, "PrixAchat"))
            Dim prixGros As Decimal = SafeDecimal(CellValueByProperty(gridProduits.CurrentRow, "PrixGros"))
            Dim prixDemi As Decimal = SafeDecimal(CellValueByProperty(gridProduits.CurrentRow, "PrixDemi"))
            Dim prixDetail As Decimal = SafeDecimal(CellValueByProperty(gridProduits.CurrentRow, "PrixDetail"))
            Dim prixQuart As Decimal = SafeDecimal(CellValueByProperty(gridProduits.CurrentRow, "PrixQuart"))
            Dim prixDouzaine As Decimal = SafeDecimal(CellValueByProperty(gridProduits.CurrentRow, "PrixDouzaine"))
            Dim prixSpecial As Decimal = SafeDecimal(CellValueByProperty(gridProduits.CurrentRow, "PrixSpecial"))
            Dim contenuUnitePrincipale As Decimal = SafeDecimal(CellValueByProperty(gridProduits.CurrentRow, "ContenuUnitePrincipale"))
            Dim contenuUniteSecondaire As Decimal = SafeDecimal(CellValueByProperty(gridProduits.CurrentRow, "ContenuUniteSecondaire"))
            Dim typeGestion As String = SafeString(CellValueByProperty(gridProduits.CurrentRow, "TypeGestionStock"))
            Dim uniteSecondaire As String = SafeString(CellValueByProperty(gridProduits.CurrentRow, "UniteSecondaire"))
            Dim venteDetail As Boolean = SafeBoolean(CellValueByProperty(gridProduits.CurrentRow, "VenteDetail"))
            Dim venteDemi As Boolean = SafeBoolean(CellValueByProperty(gridProduits.CurrentRow, "VenteDemi"))
            Dim venteDouzaine As Boolean = SafeBoolean(CellValueByProperty(gridProduits.CurrentRow, "VenteDouzaine"))
            Dim venteGros As Boolean = SafeBoolean(CellValueByProperty(gridProduits.CurrentRow, "VenteGros"))
            Dim typesPersonnalisesActifs As List(Of TypeVenteProduitDTO) = _typeVenteProduitService.ListerParProduit(produitId, True)


            _typesVenteCourants = _typeVenteService.ConstruireTypesVentePourProduit(produitId, nbUnites, prixAchat, prixGros, prixDemi, prixDetail, prixQuart, prixDouzaine, prixSpecial, venteGros, venteDemi, venteDetail, venteDouzaine, typesPersonnalisesActifs, contenuUnitePrincipale, contenuUniteSecondaire, typeGestion, uniteSecondaire)
            cmbUnite.DataSource = Nothing
            cmbUnite.DisplayMember = "NomAffichage"
            cmbUnite.ValueMember = "Nom"
            cmbUnite.DataSource = _typesVenteCourants
            If cmbUnite.Items.Count > 0 Then cmbUnite.SelectedIndex = 0

            MettreAJourAffichageStockProduit()
            MiseAJourPrixUnitaire(Nothing, EventArgs.Empty)
        End Sub

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

        Private Function SafeInteger(value As Object) As Integer
            If value Is Nothing OrElse value Is DBNull.Value Then
                Return 0
            End If

            Dim nombre As Integer
            If Integer.TryParse(Convert.ToString(value).Trim(), nombre) Then
                Return nombre
            End If

            Return 0
        End Function

        Private Function SafeString(value As Object) As String
            If value Is Nothing OrElse value Is DBNull.Value Then
                Return String.Empty
            End If

            Return Convert.ToString(value).Trim()
        End Function

        Private Function CellValueByProperty(row As DataGridViewRow, propertyName As String) As Object
            If row Is Nothing OrElse row.DataGridView Is Nothing OrElse String.IsNullOrWhiteSpace(propertyName) Then
                Return Nothing
            End If

            For Each column As DataGridViewColumn In row.DataGridView.Columns
                If String.Equals(column.DataPropertyName, propertyName, StringComparison.OrdinalIgnoreCase) OrElse
                   String.Equals(column.Name, propertyName, StringComparison.OrdinalIgnoreCase) Then
                    Return row.Cells(column.Index).Value
                End If
            Next

            Return Nothing
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

        Private Sub MiseAJourPrixUnitaire(sender As Object, e As EventArgs)
            If gridProduits.CurrentRow Is Nothing Then Return
            Dim typeChoisi As TypeVenteDTO = ObtenirTypeVenteSelectionne()
            Dim prix As Decimal = PrixSelonUnite()
            txtPrixUnitaire.Text = prix.ToString("N0")
            If typeChoisi Is Nothing Then
                lblEquivalent.Text = "Equivalent: 0 " & ObtenirUniteReferenceCourante() & " / unité"
            ElseIf typeChoisi.ProduitConditionnementId.HasValue Then
                Dim produitId As Integer = SafeInteger(CellValueByProperty(gridProduits.CurrentRow, "ProduitId"))
                Dim conditionnements As List(Of ProduitConditionnementDTO) = _conditionnementService.ListerPourInventaire(produitId)
                lblEquivalent.Text = "Equivalent: " & ConversionUniteService.DecrireEquivalentTypeVente(typeChoisi, conditionnements) & " / unité"
            Else
                lblEquivalent.Text = "Equivalent: " & FormaterQuantiteReferenceCourante(typeChoisi.QuantiteEquivalent) & " " & ObtenirUniteReferenceCourante() & " / unité"
            End If
            MiseAJourIndicateursQuantite(Nothing, EventArgs.Empty)
        End Sub

        Private Sub ColorerStockCritique(sender As Object, e As DataGridViewRowPrePaintEventArgs)
            Dim row As DataGridViewRow = gridProduits.Rows(e.RowIndex)
            Dim stock As Decimal = SafeDecimal(CellValueByProperty(row, "QuantiteStock"))
            Dim seuil As Decimal = SafeDecimal(CellValueByProperty(row, "SeuilCritique"))
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

        Private Function ProduitCourantEstMesure() As Boolean
            If gridProduits.CurrentRow Is Nothing Then Return False
            Return StockUnitConversionService.EstGestionMesuree(SafeString(CellValueByProperty(gridProduits.CurrentRow, "TypeGestionStock")))
        End Function

        Private Function ObtenirUniteReferenceCourante() As String
            If gridProduits.CurrentRow Is Nothing Then Return "pièce"

            If ProduitCourantEstMesure() Then
                Dim uniteMesure As String = SafeString(CellValueByProperty(gridProduits.CurrentRow, "UniteMesureStock"))
                If uniteMesure <> String.Empty Then Return uniteMesure
                Return "mesure"
            End If

            Dim uniteSecondaire As String = SafeString(CellValueByProperty(gridProduits.CurrentRow, "UniteSecondaire"))
            If uniteSecondaire <> String.Empty Then Return uniteSecondaire
            Return "pièce"
        End Function

        Private Function FormaterQuantiteReferenceCourante(valeur As Decimal) As String
            If ProduitCourantEstMesure() Then
                Return FormatageGlobal.FormatQuantitePhysique(valeur)
            End If

            Return valeur.ToString("N0")
        End Function

        Private Sub MiseAJourIndicateursQuantite(sender As Object, e As EventArgs)
            Dim qte As Decimal
            If Not Decimal.TryParse(txtQuantite.Text.Trim(), qte) OrElse qte <= 0D Then
                lblTotalReel.Text = "Total réel: 0 " & ObtenirUniteReferenceCourante()
                Return
            End If

            Dim typeChoisi As TypeVenteDTO = ObtenirTypeVenteSelectionne()
            If typeChoisi Is Nothing Then
                lblTotalReel.Text = "Total réel: 0 " & ObtenirUniteReferenceCourante()
                Return
            End If

            ' Convertit la quantité commerciale choisie par le facturier
            ' vers l'unité de base utilisée par le moteur de stock.
            Dim quantiteReelle As Decimal
            Try
                quantiteReelle = ConversionUniteService.CalculerQuantiteBase(qte, typeChoisi)
            Catch ex As InvalidOperationException
                lblTotalReel.Text = ex.Message
                Return
            End Try
            If typeChoisi.ProduitConditionnementId.HasValue Then
                Dim produitId As Integer = SafeInteger(CellValueByProperty(gridProduits.CurrentRow, "ProduitId"))
                lblTotalReel.Text = "Total réel: " & _conditionnementService.FormaterStock(produitId, quantiteReelle, Function() FormaterQuantiteReferenceCourante(quantiteReelle) & " " & ObtenirUniteReferenceCourante())
            Else
                lblTotalReel.Text = "Total réel: " & FormaterQuantiteReferenceCourante(quantiteReelle) & " " & ObtenirUniteReferenceCourante()
            End If
        End Sub

        Private Sub MettreAJourAffichageStockProduit()
            If gridProduits.CurrentRow Is Nothing Then Return
            Dim produitId As Integer = SafeInteger(CellValueByProperty(gridProduits.CurrentRow, "ProduitId"))
            Dim stock As Decimal = SafeDecimal(CellValueByProperty(gridProduits.CurrentRow, "QuantiteStock"))
            Dim nbUnites As Decimal = SafeDecimal(CellValueByProperty(gridProduits.CurrentRow, "ConversionUnite"))
            Dim uniteBase As String = SafeString(CellValueByProperty(gridProduits.CurrentRow, "UnitePrincipale"))
            Dim uniteSecondaire As String = SafeString(CellValueByProperty(gridProduits.CurrentRow, "UniteSecondaire"))
            Dim typeGestion As String = SafeString(CellValueByProperty(gridProduits.CurrentRow, "TypeGestionStock"))
            Dim uniteMesure As String = SafeString(CellValueByProperty(gridProduits.CurrentRow, "UniteMesureStock"))
            Dim contenuPrincipal As Decimal = SafeDecimal(CellValueByProperty(gridProduits.CurrentRow, "ContenuUnitePrincipale"))
            Dim contenuSecondaire As Decimal = SafeDecimal(CellValueByProperty(gridProduits.CurrentRow, "ContenuUniteSecondaire"))
            Dim reserve As Decimal = 0D
            For Each ligne As PanierLigne In _panier
                If ligne.ProduitId = produitId Then
                    reserve += ligne.QuantiteBase
                End If
            Next
            Dim restant As Decimal = Math.Max(0D, stock - reserve)
            Dim stockAffichage As String = _conditionnementService.FormaterStock(produitId, stock, Function() FormatageGlobal.FormatStockSelonGestion(stock, nbUnites, uniteBase, uniteSecondaire, typeGestion, uniteMesure, contenuPrincipal, contenuSecondaire))
            Dim restantAffichage As String = _conditionnementService.FormaterStock(produitId, restant, Function() FormatageGlobal.FormatStockSelonGestion(restant, nbUnites, uniteBase, uniteSecondaire, typeGestion, uniteMesure, contenuPrincipal, contenuSecondaire))
            lblStock.Text = "Stock: " & stockAffichage & " | Restant: " & restantAffichage
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

            Dim produitId As Integer = SafeInteger(CellValueByProperty(gridProduits.CurrentRow, "ProduitId"))
            Dim libelle As String = SafeString(CellValueByProperty(gridProduits.CurrentRow, "Libelle"))
            If produitId <= 0 Then
                MessageBox.Show("Produit invalide.")
                Return
            End If
            Dim typeChoisi As TypeVenteDTO = ObtenirTypeVenteSelectionne()
            If typeChoisi Is Nothing Then
                MessageBox.Show("Type de vente invalide.")
                Return
            End If
            Dim unite As String = typeChoisi.Nom
            Dim prix As Decimal = PrixSelonUnite()
            Dim quantiteEquivalent As Decimal = typeChoisi.QuantiteEquivalent
            ' Le service central garantit que la même formule est utilisée
            ' par la facturation, l'initialisation des ventes et les futurs flux stock.
            Dim quantiteBase As Decimal
            Try
                quantiteBase = ConversionUniteService.CalculerQuantiteBase(qte, typeChoisi)
            Catch ex As InvalidOperationException
                MessageBox.Show(ex.Message, "Quantité invalide", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End Try
            Dim stock As Decimal = SafeDecimal(CellValueByProperty(gridProduits.CurrentRow, "QuantiteStock"))

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
            txtQuantite.Clear()
            txtQuantite.Focus()
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
            MettreAJourBoutonsPanier()
        End Sub

        Private Sub MettreAJourBoutonsPanier()
            Dim panierVide As Boolean = (_panier Is Nothing OrElse _panier.Count = 0)
            btnValider.Enabled = Not panierVide
            btnImprimer.Enabled = Not panierVide
            btnPdf.Enabled = Not panierVide
        End Sub

        Private Sub RechercherClientParTelephone(sender As Object, e As EventArgs)
            Dim tel As String = NettoyerTelephone(txtClientTel.Text)
            If tel = "" Then
                txtClientId.Text = ""
                Return
            End If
            If tel.Length <> 10 Then
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
                If row.Table.Columns.Contains("ProduitId") AndAlso row.Table.Columns.Contains("QuantiteStock") AndAlso
                   Convert.ToInt32(row("ProduitId")) = produitId Then
                    Return SafeDecimal(row("QuantiteStock"))
                End If
            Next
            Return 0D
        End Function

        Public Sub ChargerFacturePourEdition(factureVenteId As Integer, numeroFacture As String, clientNom As String, telephone As String)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim factureRepo As New FactureVenteRepository(dal)
                Dim facture As FactureVenteDTO = factureRepo.ObtenirParId(factureVenteId)
                If facture Is Nothing OrElse facture.Statut <> "EN_ATTENTE" Then
                    MessageBox.Show("Seules les factures brouillon peuvent être modifiees.")
                    Return
                End If

                Dim ligneRepo As New LigneFactureVenteRepository(dal)
                Dim dtLignes As DataTable = ligneRepo.ListerDetailsParFacture(factureVenteId)

                _factureEnEditionId = factureVenteId
                txtNumeroFacture.Text = numeroFacture
                txtClientNom.Text = clientNom
                txtClientTel.Text = telephone
                txtClientId.Text = ""
                txtRemise.Text = ""
                btnValider.Text = "METTRE À JOUR LA VENTE"

                _panier.Clear()
                For Each row As DataRow In dtLignes.Rows
                    Dim qteSaisie As Decimal = If(dtLignes.Columns.Contains("QuantiteSaisie") AndAlso Not row.IsNull("QuantiteSaisie"), Convert.ToDecimal(row("QuantiteSaisie")), Convert.ToDecimal(row("Quantite")))
                    Dim qteBase As Decimal = If(dtLignes.Columns.Contains("QuantiteBase") AndAlso Not row.IsNull("QuantiteBase"), Convert.ToDecimal(row("QuantiteBase")), qteSaisie)
                    Dim prix As Decimal = Convert.ToDecimal(row("PrixUnitaire"))
                    Dim totalLigne As Decimal = Convert.ToDecimal(row("MontantLigne"))
                    Dim unite As String = Convert.ToString(row("TypeVente"))
                    Dim quantiteEquivalente As Decimal = If(qteSaisie = 0D, qteBase, qteBase / qteSaisie)

                    _panier.Add(New PanierLigne With {
                        .ProduitId = Convert.ToInt32(row("ProduitId")),
                        .Libelle = Convert.ToString(row("Libelle")),
                        .Unite = unite,
                        .PrixUnitaire = prix,
                        .Quantite = qteSaisie,
                        .QuantiteBase = qteBase,
                        .QuantiteEquivalente = quantiteEquivalente,
                        .QuantiteReelle = qteBase,
                        .Total = totalLigne
                    })
                Next

                RafraichirPanier()
                ChargerProduits()
            Catch ex As Exception
                MessageBox.Show("Erreur chargement facture brouillon: " & ex.Message)
            End Try
        End Sub

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
                Dim factureRepo As New FactureVenteRepository(dal)
                Dim clientService As New ClientService(New ClientRepository(dal))

                If _factureEnEditionId.HasValue Then
                    Dim factureExistante As FactureVenteDTO = factureRepo.ObtenirParId(_factureEnEditionId.Value)
                    If factureExistante Is Nothing OrElse factureExistante.Statut <> "EN_ATTENTE" Then
                        MessageBox.Show("Seules les factures brouillon peuvent être modifiees.")
                        Return
                    End If
                End If

                Dim sousTotal As Decimal = 0D
                For Each l As PanierLigne In _panier
                    sousTotal += l.Total
                Next

                Dim remisePourcent As Decimal
                Decimal.TryParse(txtRemise.Text.Trim(), remisePourcent)
                Dim remiseMontant As Decimal = sousTotal * remisePourcent / 100D
                Dim total As Decimal = sousTotal - remiseMontant

                Dim clientId As Integer? = Nothing
                Dim tel As String = NettoyerTelephone(txtClientTel.Text)
                Dim nom As String = txtClientNom.Text.Trim()
                If tel <> "" AndAlso Not NumeroTelephoneValide(tel) Then
                    MessageBox.Show("Le numéro de téléphone doit contenir exactement 10 chiffres.")
                    txtClientTel.Focus()
                    txtClientTel.SelectAll()
                    Return
                End If

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

                Dim factureId As Integer
                If _factureEnEditionId.HasValue Then
                    factureId = _factureEnEditionId.Value
                    Dim ligneRepo As New LigneFactureVenteRepository(dal)

                    Dim factureMaj As New FactureVente With {
                        .FactureVenteId = factureId,
                        .NumeroFacture = numeroFacture,
                        .ClientId = clientId,
                        .SousTotal = sousTotal,
                        .MontantRemise = remiseMontant,
                        .MontantTaxe = 0D,
                        .MontantTotal = total,
                        .Statut = "EN_ATTENTE"
                    }
                    factureRepo.MettreAJour(factureMaj)

                    Dim lignesExistantes As List(Of LigneFactureVenteDTO) = ligneRepo.ListerParFacture(factureId)
                    For Each ligneExistante As LigneFactureVenteDTO In lignesExistantes
                        ligneRepo.Supprimer(ligneExistante.LigneFactureVenteId)
                    Next
                Else
                    factureId = service.CreerFacture(numeroFacture, clientId, sousTotal, remiseMontant, 0D, total, SessionUtilisateur.UtilisateurId)
                End If

                For Each l As PanierLigne In _panier
                    service.AjouterLigne(factureId, l.ProduitId, l.Quantite, l.QuantiteBase, l.Unite, l.PrixUnitaire, 0D, l.Quantite)
                Next

                AppDataVersionService.Touch("FACTURES")
                AppEvents.OnDataChanged()

                MessageBox.Show(If(_factureEnEditionId.HasValue, "Facture brouillon mise a jour: ", "Facture en attente: ") & numeroFacture)
                _panier.Clear()
                _factureEnEditionId = Nothing
                btnValider.Text = "VALIDER LA VENTE"
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
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FacturationForm", "GenererNouveauNumeroFacture", "Impossible de générer le numéro automatique de facture.", ex)
                txtNumeroFacture.Text = ""
            End Try
        End Sub

        Private Sub Telephone_KeyPress(sender As Object, e As KeyPressEventArgs)
            If Char.IsControl(e.KeyChar) Then
                Return
            End If

            If Not Char.IsDigit(e.KeyChar) OrElse txtClientTel.TextLength >= 10 Then
                e.Handled = True
            End If
        End Sub

        Private Sub NormaliserTelephoneClient(sender As Object, e As EventArgs)
            If _normalisationTelephoneEnCours Then
                Return
            End If

            Dim nettoye As String = NettoyerTelephone(txtClientTel.Text)
            If nettoye.Length > 10 Then
                nettoye = nettoye.Substring(0, 10)
            End If

            If txtClientTel.Text <> nettoye Then
                _normalisationTelephoneEnCours = True
                Dim position As Integer = Math.Min(nettoye.Length, txtClientTel.SelectionStart)
                txtClientTel.Text = nettoye
                txtClientTel.SelectionStart = position
                _normalisationTelephoneEnCours = False
            End If
        End Sub

        Private Function NumeroTelephoneValide(numero As String) As Boolean
            Dim nettoye As String = NettoyerTelephone(numero)
            If nettoye = String.Empty Then
                Return True
            End If
            If nettoye.Length <> 10 OrElse Not nettoye.All(Function(c) Char.IsDigit(c)) Then
                Return False
            End If
            Return _prefixesTelephoneRdc.Any(Function(prefixe) nettoye.StartsWith(prefixe, StringComparison.Ordinal))
        End Function

        Private Shared Function NettoyerTelephone(numero As String) As String
            If String.IsNullOrWhiteSpace(numero) Then
                Return String.Empty
            End If
            Return New String(numero.Where(Function(c) Char.IsDigit(c)).ToArray())
        End Function

        Private Sub ImprimerA4(sender As Object, e As EventArgs)
            Try
                If _panier Is Nothing OrElse _panier.Count = 0 Then
                    MessageBox.Show("Panier vide.")
                    Return
                End If

                Using choix As New FormChoixImpressionProforma()
                    If choix.ShowDialog(Me) <> DialogResult.OK Then Return
                    If choix.FormatSelectionne = "THERMIQUE" Then
                        ImprimerProformaThermique()
                    Else
                        ImprimerProformaA4()
                    End If
                End Using
            Catch ex As Exception
                MessageBox.Show("Erreur impression: " & ex.Message)
            End Try
        End Sub

        Private Sub ExporterPdf(sender As Object, e As EventArgs)
            Try
                If _panier Is Nothing OrElse _panier.Count = 0 Then
                    MessageBox.Show("Panier vide.")
                    Return
                End If

                Using sfd As New SaveFileDialog()
                    sfd.Filter = "PDF (*.pdf)|*.pdf"
                    sfd.FileName = ConstruireNomPdfProforma()
                    If sfd.ShowDialog(Me) <> DialogResult.OK Then Return

                    _parametres = PrintConfigurationHelper.ChargerParametres()
                    _proformaA4Index = 0
                    Using doc As New Printing.PrintDocument()
                        doc.DefaultPageSettings.PaperSize = New Printing.PaperSize("A4", 827, 1169)
                        doc.DefaultPageSettings.Margins = New Printing.Margins(30, 30, 30, 30)
                        doc.DefaultPageSettings.Color = If(_parametres IsNot Nothing, _parametres.ImpressionCouleur, True)
                        AddHandler doc.PrintPage, AddressOf ImprimerPage
                        Dim cheminPdf As String = PdfHelper.GenererPdfDepuisPrintDocumentEtRetournerChemin(sfd.FileName, doc)
                        MessageBox.Show("PDF généré avec succès." & Environment.NewLine & Environment.NewLine & "Fichier :" & Environment.NewLine & cheminPdf, "Export PDF", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    End Using
                End Using
            Catch ex As Exception
                MessageBox.Show("Erreur PDF: " & ex.Message)
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
            DessinerProformaA4(e)
        End Sub

        Private Sub ImprimerProformaA4()
            Using doc As New Printing.PrintDocument()
                _parametres = PrintConfigurationHelper.ConfigurerDocumentA4(doc, Me, "FacturationForm", "ImprimerProformaA4")
                _proformaA4Index = 0
                AddHandler doc.PrintPage, AddressOf ImprimerPage

                If _parametres IsNot Nothing AndAlso _parametres.ApercuAvantImpression Then
                    Using preview As New PrintPreviewDialog()
                        preview.Document = doc
                        preview.ShowDialog(Me)
                    End Using
                Else
                    doc.Print()
                End If
            End Using
        End Sub

        Private Sub ImprimerProformaThermique()
            Using doc As New Printing.PrintDocument()
                _parametres = PrintConfigurationHelper.ChargerParametres()
                Dim hauteur As Integer = CalculerHauteurProformaThermique(315)
                _parametres = PrintConfigurationHelper.ConfigurerDocumentThermique(doc, Me, "FacturationForm", "ImprimerProformaThermique", 315, hauteur)
                AddHandler doc.PrintPage, AddressOf ImprimerPageProformaThermique
                Using preview As New FormApercuTicket(doc, Sub() doc.Print())
                    preview.ShowDialog(Me)
                End Using
            End Using
        End Sub

        Private Sub DessinerProformaA4(e As Printing.PrintPageEventArgs)
            e.Graphics.PageUnit = GraphicsUnit.Display
            Dim printable As RectangleF = e.MarginBounds
            Dim reportWidth As Single = printable.Width * 0.93F
            Dim reportLeft As Single = printable.Left + ((printable.Width - reportWidth) / 2.0F)
            Dim reportRight As Single = reportLeft + reportWidth
            Dim y As Single = printable.Top
            Dim footerHeight As Single = 24.0F

            Using fontTitle As New Font("Segoe UI", 16, FontStyle.Bold),
                  fontHeader As New Font("Segoe UI", 10, FontStyle.Bold),
                  fontNormal As New Font("Segoe UI", 9),
                  fontSmall As New Font("Segoe UI", 8),
                  blueBrush As New SolidBrush(Color.FromArgb(41, 128, 185)),
                  grayBrush As New SolidBrush(Color.FromArgb(107, 114, 128)),
                  headerBrush As New SolidBrush(Color.FromArgb(41, 128, 185)),
                  lightBrush As New SolidBrush(Color.FromArgb(239, 246, 255))

                y = DessinerEnteteProformaA4(e.Graphics, reportLeft, y, reportWidth, fontTitle, fontNormal, blueBrush, grayBrush)

                e.Graphics.FillRectangle(headerBrush, reportLeft, y, reportWidth, 34)
                DessinerTexteCentreA4(e.Graphics, "PROFORMA", fontTitle, Brushes.White, New RectangleF(reportLeft, y + 4, reportWidth, 26))
                y += 46

                Dim infoHeight As Single = 82
                e.Graphics.FillRectangle(lightBrush, reportLeft, y, reportWidth, infoHeight)
                DessinerLibelleValeurA4(e.Graphics, "N°", txtNumeroFacture.Text.Trim(), fontHeader, fontNormal, reportLeft + 12, y + 10, reportWidth * 0.48F)
                DessinerLibelleValeurA4(e.Graphics, "Date", Date.Now.ToString("dd/MM/yyyy HH:mm"), fontHeader, fontNormal, reportLeft + 12, y + 34, reportWidth * 0.48F)
                DessinerLibelleValeurA4(e.Graphics, "Facturier", If(String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur), "SYSTEM", SessionUtilisateur.NomUtilisateur), fontHeader, fontNormal, reportLeft + 12, y + 58, reportWidth * 0.48F)
                DessinerLibelleValeurA4(e.Graphics, "Client", If(txtClientNom.Text.Trim() = "", "CLIENT", txtClientNom.Text.Trim()), fontHeader, fontNormal, reportLeft + reportWidth * 0.52F, y + 10, reportWidth * 0.46F)
                DessinerLibelleValeurA4(e.Graphics, "Téléphone", txtClientTel.Text.Trim(), fontHeader, fontNormal, reportLeft + reportWidth * 0.52F, y + 34, reportWidth * 0.46F)
                y += infoHeight + 18

                Dim widths As Single() = {reportWidth * 0.36F, reportWidth * 0.18F, reportWidth * 0.12F, reportWidth * 0.17F, reportWidth * 0.17F}
                Dim headers As String() = {"Désignation", "Conditionnement", "Quantité", "Prix unitaire", "Montant"}
                y = DessinerEnteteTableProforma(e.Graphics, reportLeft, y, widths, headers, fontHeader, headerBrush)

                Dim tableBottom As Single = printable.Bottom - footerHeight
                While _proformaA4Index < _panier.Count
                    Dim ligne As PanierLigne = _panier(_proformaA4Index)
                    Dim rowHeight As Single = MesurerHauteurLigneProforma(e.Graphics, ligne, widths, fontNormal)
                    If y + rowHeight > tableBottom Then
                        DessinerFooterProformaA4(e.Graphics, reportLeft, reportWidth, printable.Bottom - 16, fontSmall)
                        e.HasMorePages = True
                        Return
                    End If
                    DessinerLigneProformaA4(e.Graphics, reportLeft, y, widths, rowHeight, ligne, fontNormal)
                    y += rowHeight
                    _proformaA4Index += 1
                End While

                Dim sousTotal As Decimal = CalculerSousTotalPanier()
                Dim remiseMontant As Decimal = CalculerRemiseMontant(sousTotal)
                Dim total As Decimal = sousTotal - remiseMontant
                y += 14
                Dim totalLeft As Single = reportRight - 230
                DessinerTotalA4(e.Graphics, "Sous-total", sousTotal, fontNormal, totalLeft, y, 230)
                y += 24
                DessinerTotalA4(e.Graphics, "Remise", remiseMontant, fontNormal, totalLeft, y, 230)
                y += 26
                DessinerTotalA4(e.Graphics, "TOTAL", total, fontHeader, totalLeft, y, 230)
                y += 34
                Using fmtCentre As New StringFormat()
                    fmtCentre.Alignment = StringAlignment.Center
                    e.Graphics.DrawString("DOCUMENT PROFORMA — NON ACQUITTÉ", fontHeader, blueBrush, New RectangleF(reportLeft, y, reportWidth, 24), fmtCentre)
                End Using

                DessinerFooterProformaA4(e.Graphics, reportLeft, reportWidth, printable.Bottom - 16, fontSmall)
                _proformaA4Index = 0
                e.HasMorePages = False
            End Using
        End Sub

        Private Function DessinerEnteteProformaA4(g As Graphics, left As Single, y As Single, width As Single, fontTitle As Font, fontNormal As Font, blueBrush As Brush, grayBrush As Brush) As Single
            Dim xText As Single = left
            Dim logoPath As String = If(_parametres Is Nothing, String.Empty, LogoPathHelper.GetLogoPath(_parametres))
            If Not String.IsNullOrWhiteSpace(logoPath) AndAlso File.Exists(logoPath) Then
                Using img As Image = Image.FromFile(logoPath)
                    g.DrawImage(img, left, y, 58, 58)
                End Using
                xText += 70
            End If
            g.DrawString(If(_parametres IsNot Nothing AndAlso _parametres.NomMagasin <> "", _parametres.NomMagasin, "COMMERCIAL PRO"), fontTitle, blueBrush, xText, y)
            g.DrawString(If(_parametres Is Nothing, String.Empty, _parametres.AdresseMagasin), fontNormal, grayBrush, xText, y + 26)
            g.DrawString(If(_parametres Is Nothing, String.Empty, _parametres.TelephoneMagasin), fontNormal, grayBrush, xText, y + 46)
            Return y + 72
        End Function

        Private Shared Sub DessinerTexteCentreA4(g As Graphics, texte As String, font As Font, brush As Brush, rect As RectangleF)
            Using fmt As New StringFormat()
                fmt.Alignment = StringAlignment.Center
                fmt.LineAlignment = StringAlignment.Center
                g.DrawString(If(texte, String.Empty), font, brush, rect, fmt)
            End Using
        End Sub

        Private Shared Sub DessinerLibelleValeurA4(g As Graphics, libelle As String, valeur As String, fontLibelle As Font, fontValeur As Font, x As Single, y As Single, width As Single)
            g.DrawString(libelle & " :", fontLibelle, Brushes.Black, New RectangleF(x, y, 85, 20))
            g.DrawString(If(valeur, String.Empty), fontValeur, Brushes.Black, New RectangleF(x + 90, y, width - 90, 22))
        End Sub

        Private Shared Function DessinerEnteteTableProforma(g As Graphics, left As Single, y As Single, widths As Single(), headers As String(), font As Font, brush As Brush) As Single
            Dim x As Single = left
            For i As Integer = 0 To headers.Length - 1
                g.FillRectangle(brush, x, y, widths(i), 28)
                g.DrawString(headers(i), font, Brushes.White, New RectangleF(x + 4, y + 6, widths(i) - 8, 18))
                x += widths(i)
            Next
            Return y + 28
        End Function

        Private Shared Function MesurerHauteurLigneProforma(g As Graphics, ligne As PanierLigne, widths As Single(), font As Font) As Single
            Dim valeurs As String() = {
                ligne.Libelle,
                ligne.Unite,
                FormaterQuantiteProforma(ligne.Quantite),
                FormatMontantProforma(ligne.PrixUnitaire),
                FormatMontantProforma(ligne.Total)
            }
            Dim hauteur As Single = 28
            For i As Integer = 0 To valeurs.Length - 1
                Dim size As SizeF = g.MeasureString(If(valeurs(i), String.Empty), font, New SizeF(widths(i) - 8, 500))
                hauteur = Math.Max(hauteur, CSng(Math.Ceiling(size.Height)) + 10)
            Next
            Return hauteur
        End Function

        Private Shared Sub DessinerLigneProformaA4(g As Graphics, left As Single, y As Single, widths As Single(), height As Single, ligne As PanierLigne, font As Font)
            Dim valeurs As String() = {
                ligne.Libelle,
                ligne.Unite,
                FormaterQuantiteProforma(ligne.Quantite),
                FormatMontantProforma(ligne.PrixUnitaire),
                FormatMontantProforma(ligne.Total)
            }
            Dim x As Single = left
            For i As Integer = 0 To valeurs.Length - 1
                g.DrawRectangle(Pens.LightGray, x, y, widths(i), height)
                Using fmt As New StringFormat()
                    fmt.Alignment = If(i >= 2, StringAlignment.Far, StringAlignment.Near)
                    fmt.LineAlignment = StringAlignment.Center
                    g.DrawString(valeurs(i), font, Brushes.Black, New RectangleF(x + 4, y + 3, widths(i) - 8, height - 6), fmt)
                End Using
                x += widths(i)
            Next
        End Sub

        Private Shared Sub DessinerTotalA4(g As Graphics, libelle As String, montant As Decimal, font As Font, x As Single, y As Single, width As Single)
            g.DrawString(libelle & " :", font, Brushes.Black, New RectangleF(x, y, width * 0.45F, 22))
            Using fmt As New StringFormat()
                fmt.Alignment = StringAlignment.Far
                g.DrawString(FormatMontantProforma(montant), font, Brushes.Black, New RectangleF(x + width * 0.45F, y, width * 0.55F, 22), fmt)
            End Using
        End Sub

        Private Shared Sub DessinerFooterProformaA4(g As Graphics, left As Single, width As Single, y As Single, font As Font)
            Dim texte As String = "Impression professionnelle générée depuis COMMERCIAL PRO - " & Date.Now.ToString("dd/MM/yyyy HH:mm")
            Using fmtCentre As New StringFormat()
                fmtCentre.Alignment = StringAlignment.Center
                g.DrawString(texte, font, Brushes.Gray, New RectangleF(left, y, width, 18), fmtCentre)
            End Using
        End Sub

        Private Function CalculerHauteurProformaThermique(largeurPapier As Integer) As Integer
            Using bmp As New Bitmap(1, 1),
                  g As Graphics = Graphics.FromImage(bmp),
                  fontTitre As New Font("Segoe UI", 10, FontStyle.Bold),
                  fontSection As New Font("Segoe UI", 7.5F, FontStyle.Bold),
                  fontLigne As New Font("Segoe UI", 7.5F),
                  fontTotal As New Font("Segoe UI", 8.5F, FontStyle.Bold)

                Dim largeur As Integer = Math.Max(200, largeurPapier - 28)
                Dim hauteur As Integer = 8
                If LogoProformaDisponible() Then hauteur += 48

                ' La hauteur thermique est calculée sur le contenu réel afin que
                ' le pied proforma ne tombe pas dans la zone de coupe de l'imprimante.
                hauteur += HauteurTexteTicket(g, If(_parametres IsNot Nothing AndAlso _parametres.NomMagasin <> "", _parametres.NomMagasin, "COMMERCIAL PRO"), fontTitre, largeur)
                If _parametres IsNot Nothing AndAlso _parametres.AdresseMagasin <> "" Then hauteur += HauteurTexteTicket(g, _parametres.AdresseMagasin, fontLigne, largeur)
                If _parametres IsNot Nothing AndAlso _parametres.TelephoneMagasin <> "" Then hauteur += HauteurTexteTicket(g, _parametres.TelephoneMagasin, fontLigne, largeur)
                hauteur += HauteurTexteTicket(g, "PROFORMA", fontSection, largeur) + 10
                hauteur += 18 * 5 + 20

                For Each ligne As PanierLigne In _panier
                    hauteur += HauteurTexteTicket(g, ligne.Libelle, fontSection, largeur)
                    hauteur += HauteurTexteTicket(g, FormaterQuantiteProforma(ligne.Quantite) & " " & ligne.Unite & " x " & FormatMontantProforma(ligne.PrixUnitaire) & " = " & FormatMontantProforma(ligne.Total), fontLigne, largeur - 4)
                Next

                hauteur += 10 + (18 * 3) + 10
                hauteur += HauteurTexteTicket(g, "DOCUMENT PROFORMA - NON ACQUITTE", fontSection, largeur)
                hauteur += HauteurTexteTicket(g, Application.ProductName & " - v" & PrintConfigurationHelper.ObtenirVersionApplication(), fontLigne, largeur)
                hauteur += HauteurTexteTicket(g, "Développé par : Andy Ntanta", fontLigne, largeur)
                Return Math.Max(420, hauteur + 44)
            End Using
        End Function

        Private Function LogoProformaDisponible() As Boolean
            Dim logoPath As String = If(_parametres Is Nothing, String.Empty, LogoPathHelper.GetLogoPath(_parametres))
            Return Not String.IsNullOrWhiteSpace(logoPath) AndAlso File.Exists(logoPath)
        End Function

        Private Shared Function HauteurTexteTicket(g As Graphics, texte As String, font As Font, largeur As Integer) As Integer
            Dim size As SizeF = g.MeasureString(If(texte, String.Empty), font, New SizeF(largeur, 1000))
            Return CInt(Math.Ceiling(size.Height)) + 2
        End Function

        Private Sub ImprimerPageProformaThermique(sender As Object, e As Printing.PrintPageEventArgs)
            Dim gauche As Integer = e.MarginBounds.Left + 4
            Dim largeur As Integer = Math.Max(200, e.MarginBounds.Width - 8)
            Dim y As Integer = e.MarginBounds.Top + 2
            Using fontTitre As New Font("Segoe UI", 10, FontStyle.Bold),
                  fontSection As New Font("Segoe UI", 7.5F, FontStyle.Bold),
                  fontLigne As New Font("Segoe UI", 7.5F),
                  fontTotal As New Font("Segoe UI", 8.5F, FontStyle.Bold)

                Dim logoPath As String = If(_parametres Is Nothing, String.Empty, LogoPathHelper.GetLogoPath(_parametres))
                If Not String.IsNullOrWhiteSpace(logoPath) AndAlso File.Exists(logoPath) Then
                    Using img As Image = Image.FromFile(logoPath)
                        e.Graphics.DrawImage(img, gauche + ((largeur - 42) \ 2), y, 42, 42)
                    End Using
                    y += 46
                End If

                y = DessinerTexteCentreTicketProforma(e.Graphics, If(_parametres IsNot Nothing AndAlso _parametres.NomMagasin <> "", _parametres.NomMagasin, "COMMERCIAL PRO"), fontTitre, gauche, largeur, y)
                If _parametres IsNot Nothing AndAlso _parametres.AdresseMagasin <> "" Then y = DessinerTexteCentreTicketProforma(e.Graphics, _parametres.AdresseMagasin, fontLigne, gauche, largeur, y)
                If _parametres IsNot Nothing AndAlso _parametres.TelephoneMagasin <> "" Then y = DessinerTexteCentreTicketProforma(e.Graphics, _parametres.TelephoneMagasin, fontLigne, gauche, largeur, y)
                y = DessinerTexteCentreTicketProforma(e.Graphics, "PROFORMA", fontSection, gauche, largeur, y + 4)
                y = DessinerSeparateurTicketProforma(e.Graphics, gauche, largeur, y)
                y = DessinerPaireTicketProforma(e.Graphics, "N°", txtNumeroFacture.Text.Trim(), fontLigne, gauche, largeur, y)
                y = DessinerPaireTicketProforma(e.Graphics, "Date", Date.Now.ToString("dd/MM/yyyy HH:mm"), fontLigne, gauche, largeur, y)
                y = DessinerPaireTicketProforma(e.Graphics, "Client", If(txtClientNom.Text.Trim() = "", "CLIENT", txtClientNom.Text.Trim()), fontLigne, gauche, largeur, y)
                If txtClientTel.Text.Trim() <> "" Then y = DessinerPaireTicketProforma(e.Graphics, "Téléphone", txtClientTel.Text.Trim(), fontLigne, gauche, largeur, y)
                y = DessinerPaireTicketProforma(e.Graphics, "Facturier", If(String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur), "SYSTEM", SessionUtilisateur.NomUtilisateur), fontLigne, gauche, largeur, y)
                y = DessinerSeparateurTicketProforma(e.Graphics, gauche, largeur, y)

                For Each ligne As PanierLigne In _panier
                    y = DessinerTexteGaucheTicketProforma(e.Graphics, ligne.Libelle, fontSection, gauche, largeur, y)
                    y = DessinerTexteGaucheTicketProforma(e.Graphics, FormaterQuantiteProforma(ligne.Quantite) & " " & ligne.Unite & " x " & FormatMontantProforma(ligne.PrixUnitaire) & " = " & FormatMontantProforma(ligne.Total), fontLigne, gauche + 4, largeur - 4, y)
                Next

                Dim sousTotal As Decimal = CalculerSousTotalPanier()
                Dim remiseMontant As Decimal = CalculerRemiseMontant(sousTotal)
                Dim total As Decimal = sousTotal - remiseMontant
                y = DessinerSeparateurTicketProforma(e.Graphics, gauche, largeur, y)
                y = DessinerPaireTicketProforma(e.Graphics, "Sous-total", FormatMontantProforma(sousTotal), fontLigne, gauche, largeur, y)
                y = DessinerPaireTicketProforma(e.Graphics, "Remise", FormatMontantProforma(remiseMontant), fontLigne, gauche, largeur, y)
                y = DessinerPaireTicketProforma(e.Graphics, "TOTAL", FormatMontantProforma(total), fontTotal, gauche, largeur, y)
                y = DessinerSeparateurTicketProforma(e.Graphics, gauche, largeur, y)
                y = DessinerTexteCentreTicketProforma(e.Graphics, "DOCUMENT PROFORMA - NON ACQUITTE", fontSection, gauche, largeur, y)
                y = DessinerTexteCentreTicketProforma(e.Graphics, Application.ProductName & " - v" & PrintConfigurationHelper.ObtenirVersionApplication(), fontLigne, gauche, largeur, y)
                y = DessinerTexteCentreTicketProforma(e.Graphics, "Développé par : Andy Ntanta", fontLigne, gauche, largeur, y)
            End Using
        End Sub

        Private Shared Function DessinerTexteCentreTicketProforma(g As Graphics, texte As String, font As Font, x As Integer, largeur As Integer, y As Integer) As Integer
            Dim size As SizeF = g.MeasureString(If(texte, String.Empty), font, New SizeF(largeur, 1000))
            Using fmtCentre As New StringFormat()
                fmtCentre.Alignment = StringAlignment.Center
                g.DrawString(If(texte, String.Empty), font, Brushes.Black, New RectangleF(x, y, largeur, size.Height), fmtCentre)
            End Using
            Return y + CInt(Math.Ceiling(size.Height)) + 2
        End Function

        Private Shared Function DessinerTexteGaucheTicketProforma(g As Graphics, texte As String, font As Font, x As Integer, largeur As Integer, y As Integer) As Integer
            Dim size As SizeF = g.MeasureString(If(texte, String.Empty), font, New SizeF(largeur, 1000))
            g.DrawString(If(texte, String.Empty), font, Brushes.Black, New RectangleF(x, y, largeur, size.Height))
            Return y + CInt(Math.Ceiling(size.Height)) + 2
        End Function

        Private Shared Function DessinerPaireTicketProforma(g As Graphics, libelle As String, valeur As String, font As Font, x As Integer, largeur As Integer, y As Integer) As Integer
            Dim largeurLibelle As Integer = Math.Min(78, CInt(largeur * 0.38F))
            g.DrawString(libelle & " :", font, Brushes.Black, New RectangleF(x, y, largeurLibelle, 18))
            Using fmtDroite As New StringFormat()
                fmtDroite.Alignment = StringAlignment.Far
                g.DrawString(If(valeur, String.Empty), font, Brushes.Black, New RectangleF(x + largeurLibelle, y, largeur - largeurLibelle, 34), fmtDroite)
            End Using
            Return y + 18
        End Function

        Private Shared Function DessinerSeparateurTicketProforma(g As Graphics, x As Integer, largeur As Integer, y As Integer) As Integer
            g.DrawLine(Pens.Black, x, y + 4, x + largeur - 1, y + 4)
            Return y + 10
        End Function

        Private Function CalculerSousTotalPanier() As Decimal
            Return _panier.Sum(Function(l) l.Total)
        End Function

        Private Function CalculerRemiseMontant(sousTotal As Decimal) As Decimal
            Dim remisePourcent As Decimal
            Decimal.TryParse(txtRemise.Text.Trim(), remisePourcent)
            Return sousTotal * remisePourcent / 100D
        End Function

        Private Shared Function FormatMontantProforma(montant As Decimal) As String
            Return montant.ToString("N0") & " FC"
        End Function

        Private Shared Function FormaterQuantiteProforma(qte As Decimal) As String
            If Decimal.Truncate(qte) = qte Then Return qte.ToString("N0")
            Return qte.ToString("0.####")
        End Function

        Private Function ConstruireNomPdfProforma() As String
            Dim numero As String = NettoyerNomFichier(txtNumeroFacture.Text.Trim())
            If numero = "" Then numero = "SANS_NUMERO"
            Dim client As String = NettoyerNomFichier(txtClientNom.Text.Trim())
            If client = "" Then client = "CLIENT"
            Return "PROFORMA_" & numero & "_" & client & ".pdf"
        End Function

        Private Shared Function NettoyerNomFichier(valeur As String) As String
            Dim texte As String = If(valeur, String.Empty).Trim()
            For Each c As Char In Path.GetInvalidFileNameChars()
                texte = texte.Replace(c, "_"c)
            Next
            texte = texte.Replace(" "c, "_"c)
            Return texte
        End Function

        Private NotInheritable Class FormChoixImpressionProforma
            Inherits Form

            Public Property FormatSelectionne As String = String.Empty

            Public Sub New()
                Text = "Imprimer le proforma"
                StartPosition = FormStartPosition.CenterParent
                FormBorderStyle = FormBorderStyle.FixedDialog
                MinimizeBox = False
                MaximizeBox = False
                ClientSize = New Size(360, 210)
                BackColor = Color.White

                Dim titre As New Label() With {.Text = "IMPRIMER LE PROFORMA", .Dock = DockStyle.Top, .Height = 54, .TextAlign = ContentAlignment.MiddleCenter, .Font = New Font("Segoe UI", 12, FontStyle.Bold), .ForeColor = Color.FromArgb(52, 73, 94)}
                Dim btnTicket As New Button() With {.Text = "Ticket thermique 80 mm", .Left = 55, .Top = 70, .Width = 250, .Height = 34, .BackColor = Color.FromArgb(52, 73, 94), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
                Dim btnA4 As New Button() With {.Text = "Proforma A4", .Left = 55, .Top = 112, .Width = 250, .Height = 34, .BackColor = Color.FromArgb(41, 128, 185), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
                Dim btnAnnuler As New Button() With {.Text = "Annuler", .Left = 55, .Top = 154, .Width = 250, .Height = 30, .DialogResult = DialogResult.Cancel}
                AddHandler btnTicket.Click, Sub()
                                                FormatSelectionne = "THERMIQUE"
                                                DialogResult = DialogResult.OK
                                            End Sub
                AddHandler btnA4.Click, Sub()
                                            FormatSelectionne = "A4"
                                            DialogResult = DialogResult.OK
                                        End Sub
                Controls.AddRange(New Control() {titre, btnTicket, btnA4, btnAnnuler})
                CancelButton = btnAnnuler
            End Sub
        End Class

        Private Sub OuvrirHistorique(sender As Object, e As EventArgs)
            Dim f As New FormulaireFactures()
            f.ShowDialog()
        End Sub

        Private Sub AnnulerFacture(sender As Object, e As EventArgs)
            _panier.Clear()
            _factureEnEditionId = Nothing
            txtClientId.Text = ""
            txtClientNom.Text = ""
            txtClientTel.Text = ""
            txtRemise.Text = ""
            btnValider.Text = "VALIDER LA VENTE"
            RafraichirPanier()
            GenererNouveauNumeroFacture()
        End Sub

        Private Sub Deconnecter(sender As Object, e As EventArgs)
            Dim main As MainForm = TryCast(Me.FindForm(), MainForm)
            If main IsNot Nothing Then
                main.DeconnecterDepuisModuleSession()
            Else
                Me.Close()
            End If
        End Sub

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            RemoveHandler AppEvents.ProduitModifie, AddressOf RafraichirProduitsDepuisEvenement
            RemoveHandler AppEvents.StockModifie, AddressOf RafraichirProduitsDepuisEvenement
            If _dataMonitor IsNot Nothing Then
                RemoveHandler _dataMonitor.DomaineModifie, AddressOf RafraichirProduitsDepuisVersionSql
                _dataMonitor.Dispose()
                _dataMonitor = Nothing
            End If
            MyBase.OnFormClosed(e)
        End Sub
    End Class
End Namespace
