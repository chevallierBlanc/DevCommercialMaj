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
        Implements ISortieSessionControlee
        Public Event FactureEnregistree(factureId As Integer)

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
        Private ReadOnly btnPrixDelegue As Button
        Private ReadOnly prixDeLigne As Button
        Private ReadOnly prixArticle As ToolStripMenuItem
        Private ReadOnly lblDelegationPrix As Label
        Private ReadOnly aideDelegationPrix As New ToolTip()
        Private _actionsPrix As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
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
        Private ReadOnly actionsPanier As FlowLayoutPanel
        Private ReadOnly lblAppTitle As Label
        Private ReadOnly zoneEncaissement As New Panel With {.Dock = DockStyle.Fill, .Visible = False, .BackColor = Color.White}

        Private ReadOnly _panier As List(Of PanierLigne)
        Private _validationEnCours As Boolean
        Private _abandonConfirme As Boolean
        Private _enregistrementBrouillon As Boolean
        Private ReadOnly _typeVenteService As TypeVenteService 'nouveau
        Private ReadOnly _typeVenteProduitService As TypeVenteProduitService
        Private ReadOnly _conditionnementService As ProduitConditionnementService
        Private _remiseMax As Decimal
        Private _produitsTable As DataTable
        Private _produitsView As DataView
        Private _parametres As ParametreDTO
        Private _typesVenteCourants As List(Of TypeVenteDTO) 'nouveau
        Private _factureEnEditionId As Integer?
        Private _versionFactureEdition As Byte()
        Private _clientEditionId As Integer?
        Private _clientEditionNom As String
        Private _clientEditionTelephone As String
        Private _remiseEditionMontant As Decimal
        Private _sousTotalEdition As Decimal
        Private _remiseEditionTexte As String
        Private _isRefreshingFromEvent As Boolean
        Private _suppressSelectionEvents As Boolean
        Private _dataMonitor As DataChangeMonitorService
        Private _normalisationTelephoneEnCours As Boolean
        Private ReadOnly _prefixesTelephoneRdc As String() = {"081", "082", "083", "084", "085", "089", "097", "098", "099"}

        Private Class PanierLigne
            Public Property ProduitId As Integer
            Public Property Libelle As String
            Public Property Unite As String
            Public Property PrixUnitaire As Decimal
            Public Property MotifPrixException As String
            Public Property DemandePrixId As Integer?
            Public Property TypeTarifId As Integer?
            Public Property ContextePrix As Guid = Guid.NewGuid()
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
            lblAppTitle = New Label() With {
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
                .MinimumSize = New Size(520, 0),
                .AutoScroll = True
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
                .Dock = DockStyle.Top,
                .Height = 545,
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
            btnRetirer.Width = 110
            btnPrixDelegue = New Button With {.Text = "Modifier le prix" & Environment.NewLine & "Tarif officiel", .Enabled = False, .Left = 400, .Top = 477, .Width = 120, .Height = 45, .Font = FontControl, .FlatStyle = FlatStyle.Flat, .BackColor = ColorPrimary, .ForeColor = ColorWhite}
            grpProduits.Controls.Add(btnPrixDelegue)
            AddHandler btnPrixDelegue.Click, AddressOf ModifierPrixDelegue
            Dim menuPrix As New ContextMenuStrip()
            Dim mesDemandes As New ToolStripMenuItem("Mes demandes de prix")
            menuPrix.Items.Add(mesDemandes)
            btnPrixDelegue.ContextMenuStrip = menuPrix
            AddHandler mesDemandes.Click, Sub()
                                             Try
                                                 DialogueDelegationPrix.Demandes(Me, New DelegationPrixService(New DAL(ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString)), "FACTURIER")
                                             Catch ex As Exception
                                                 MessageBox.Show(ex.Message, "Mes demandes de prix", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                             End Try
                                         End Sub
            AddHandler Me.Shown, Sub() ActualiserCommandesPrix()
            AddHandler Me.Activated, Sub() ActualiserCommandesPrix()

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
            gridPanier.MultiSelect = False
            Dim menuArticle As New ContextMenuStrip()
            prixArticle = New ToolStripMenuItem("Modifier le prix — cette ligne uniquement") With {.Enabled = False}
            menuArticle.Items.Add(prixArticle)
            gridPanier.ContextMenuStrip = menuArticle
            AddHandler gridPanier.MouseDown, Sub(sender, e)
                                                If e.Button <> MouseButtons.Right Then Return
                                                Dim touche As DataGridView.HitTestInfo = gridPanier.HitTest(e.X, e.Y)
                                                If touche.RowIndex >= 0 AndAlso touche.ColumnIndex >= 0 Then gridPanier.CurrentCell = gridPanier.Rows(touche.RowIndex).Cells(touche.ColumnIndex)
                                            End Sub
            AddHandler prixArticle.Click, AddressOf ModifierPrixArticle


            grpPanier.Controls.Add(gridPanier)
            prixDeLigne = New Button With {.Text = "MODIFIER LE PRIX — CETTE LIGNE UNIQUEMENT", .Enabled = False, .Dock = DockStyle.Bottom, .Height = 40, .BackColor = ColorPrimary, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            AddHandler prixDeLigne.Click, AddressOf ModifierPrixArticle
            grpPanier.Controls.Add(prixDeLigne)
            lblDelegationPrix = New Label With {.Dock = DockStyle.Bottom, .Height = 46, .Padding = New Padding(8), .AutoEllipsis = True, .Text = "Chargement des autorisations de prix…", .Font = FontControl, .ForeColor = ColorSecondary}
            grpPanier.Controls.Add(lblDelegationPrix)
            AddHandler gridPanier.CellClick, Sub() ActualiserSelectionCommandesPrix()
            AddHandler gridPanier.KeyUp, Sub() ActualiserSelectionCommandesPrix()

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
            actionsPanier = pnlActions

            btnValider = New Button() With {.Text = TexteValidation, .Width = 210, .Height = 50, .FlatStyle = FlatStyle.Flat, .BackColor = ColorAccent, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}
            btnImprimer = New Button() With {.Text = "IMPRIMER", .Width = 140, .Height = 50, .FlatStyle = FlatStyle.Flat, .BackColor = ColorSecondary, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}
            btnPdf = New Button() With {.Text = "PDF", .Width = 100, .Height = 50, .FlatStyle = FlatStyle.Flat, .BackColor = ColorSecondary, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}

            btnHistorique = New Button() With {.Text = "HISTORIQUE", .Width = 150, .Height = 40, .FlatStyle = FlatStyle.Flat, .BackColor = ColorPrimary, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}
            btnAnnuler = New Button() With {.Text = "ANNULER", .Width = 150, .Height = 40, .FlatStyle = FlatStyle.Flat, .BackColor = ColorDanger, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}
            btnDeconnexion = New Button() With {.Text = "DÉCONNEXION", .Width = 150, .Height = 40, .FlatStyle = FlatStyle.Flat, .BackColor = ColorSecondary, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand}

            For Each btn As Button In {btnValider, btnImprimer, btnPdf, btnHistorique, btnAnnuler, btnDeconnexion}
                btn.FlatAppearance.BorderSize = 0
                pnlActions.Controls.Add(btn)
            Next
            For Each bouton As Button In {btnAjouter, btnValider}
                ThemeHelper1.StyliserActionCommerciale(bouton, principale:=True)
            Next
            For Each bouton As Button In {btnActualiser, btnPrixDelegue, prixDeLigne, btnImprimer, btnPdf, btnHistorique, btnAnnuler, btnDeconnexion}
                ThemeHelper1.StyliserActionCommerciale(bouton)
            Next
            ThemeHelper1.StyliserActionCommerciale(btnRetirer, destructive:=True)
            For Each bouton As Button In {btnAjouter, btnValider, btnActualiser, btnPrixDelegue, prixDeLigne, btnImprimer, btnPdf, btnHistorique, btnAnnuler, btnDeconnexion, btnRetirer}
                bouton.Font = FontBold
                bouton.Padding = New Padding(4, 0, 4, 0)
            Next
            btnPrixDelegue.Text = "MODIFIER PRIX" & Environment.NewLine & "TARIF OFFICIEL"

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
            pnlRight.Controls.Add(zoneEncaissement)

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
            AddHandler AppEvents.RolePermissionsChanged, AddressOf RafraichirAutorisationsPrix
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
            ThemeHelper1.StyliserEnteteCommerciale(dgv)
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
            ThemeHelper1.StyliserEnteteCommerciale(dgv)
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
            ActualiserCommandesPrix()
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
            ActualiserSelectionCommandesPrix()
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

        Private Sub RafraichirAutorisationsPrix(sender As Object, e As EventArgs)
            If IsDisposed OrElse Disposing OrElse Not IsHandleCreated Then Return
            If InvokeRequired Then
                BeginInvoke(New MethodInvoker(Sub() RafraichirAutorisationsPrix(Nothing, EventArgs.Empty)))
                Return
            End If
            ActualiserCommandesPrix()
        End Sub

        Private Sub ActualiserCommandesPrix()
            Try
                Dim service As New DelegationPrixService(New DAL(ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString))
                Dim diagnostic As DiagnosticActionsPrix = service.DiagnostiquerActions()
                Dim actions As HashSet(Of String) = diagnostic.Actions
                _actionsPrix = actions
                lblDelegationPrix.Text = diagnostic.ExplicationTarif & Environment.NewLine & diagnostic.Explication
                aideDelegationPrix.SetToolTip(lblDelegationPrix, lblDelegationPrix.Text)
                aideDelegationPrix.SetToolTip(btnPrixDelegue, diagnostic.ExplicationTarif & " Sélectionnez un produit et son type de vente, même avec un panier vide.")
                ActualiserSelectionCommandesPrix()
            Catch ex As Exception
                _actionsPrix.Clear()
                ActualiserSelectionCommandesPrix()
                lblDelegationPrix.Text = "Commandes de prix indisponibles. Vérifiez la connexion et reconnectez-vous ou utilisez Actualiser."
                Dim log As New ProductionLogService()
                log.Warn("FacturationForm", "DelegationPrix", "Configuration ou autorisation des commandes de prix indisponible.")
            End Try
        End Sub

        Private Sub ActualiserSelectionCommandesPrix()
            ' Presentation seulement : le service reverifie les droits en SQL.
            btnPrixDelegue.Enabled = DelegationPrixRegles.CommandeDisponible(_actionsPrix, "TARIF", gridProduits.CurrentRow IsNot Nothing AndAlso ObtenirTypeVenteSelectionne() IsNot Nothing)
            Dim selection As PanierLigne = If(gridPanier.CurrentRow Is Nothing, Nothing, TryCast(gridPanier.CurrentRow.DataBoundItem, PanierLigne))
            prixDeLigne.Enabled = DelegationPrixRegles.CommandeDisponible(_actionsPrix, "FACTURE", DelegationPrixRegles.ChoisirArticle(_panier, selection) IsNot Nothing)
            prixArticle.Visible = True : prixArticle.Enabled = prixDeLigne.Enabled
            aideDelegationPrix.SetToolTip(prixDeLigne, "Prix exceptionnel limité à la ligne sélectionnée ; le catalogue reste inchangé. " & If(prixDeLigne.Enabled, "", "Vérifiez la sélection, la délégation et les permissions décrites ci-dessous."))
        End Sub

        Private Sub ModifierPrixArticle(sender As Object, e As EventArgs)
            Dim selection As PanierLigne = If(gridPanier.CurrentRow Is Nothing, Nothing, TryCast(gridPanier.CurrentRow.DataBoundItem, PanierLigne))
            Dim ligne As PanierLigne = DelegationPrixRegles.ChoisirArticle(_panier, selection)
            If ligne Is Nothing Then
                MessageBox.Show("Sélectionnez la ligne concernée dans le panier. Avec plusieurs articles, le choix doit être explicite.", "Prix de l'article", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            ModifierPrixDelegue(ligne, EventArgs.Empty)
        End Sub

        Private Sub ModifierPrixDelegue(sender As Object, e As EventArgs)
            Try
                ActualiserCommandesPrix()
                Dim ligne As PanierLigne = TryCast(sender, PanierLigne)
                If Not DelegationPrixRegles.CommandeDisponible(_actionsPrix, If(ligne Is Nothing, "TARIF", "FACTURE"), True) Then Throw New UnauthorizedAccessException(lblDelegationPrix.Text)
                Dim id As Integer
                Dim nomType As String
                Dim typeTarifId As Integer?
                If ligne IsNot Nothing Then
                    id = ligne.ProduitId : nomType = ligne.Unite
                    typeTarifId = ligne.TypeTarifId
                Else
                    If gridProduits.CurrentRow Is Nothing Then Return
                    Dim type As TypeVenteDTO = ObtenirTypeVenteSelectionne()
                    If type Is Nothing Then Return
                    id = SafeInteger(CellValueByProperty(gridProduits.CurrentRow, "ProduitId")) : nomType = type.Nom
                    typeTarifId = If(type.EstPersonnalise, type.TypeVenteProduitId, CType(0, Integer?))
                End If
                Dim contexteCommercial As Func(Of Decimal, ContexteCommercialPrix) = Nothing
                If ligne IsNot Nothing Then
                    contexteCommercial = Function(nouveauPrix As Decimal)
                                             Dim sousTotal As Decimal = CalculerSousTotalPanier() - ligne.Total + ligne.Quantite * nouveauPrix
                                             Dim remise As Decimal = CalculerRemiseMontant(sousTotal)
                                             If _factureEnEditionId.HasValue AndAlso sousTotal = _sousTotalEdition AndAlso txtRemise.Text.Trim() = _remiseEditionTexte Then remise = _remiseEditionMontant
                                             Return New ContexteCommercialPrix With {.SousTotal = sousTotal, .Remise = remise}
                                         End Function
                End If
                Dim exception As ResultatPrixException = DialogueDelegationPrix.Afficher(Me, New DelegationPrixService(New DAL(ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString)), id, nomType, If(ligne Is Nothing, Guid.Empty, ligne.ContextePrix), If(ligne Is Nothing, 0D, ligne.Quantite), If(ligne Is Nothing, CType(Nothing, Decimal?), ligne.PrixUnitaire), contexteCommercial, typeTarifId, If(ligne Is Nothing, "TARIF", "FACTURE"))
                If exception IsNot Nothing Then
                    If ligne Is Nothing Then
                        MessageBox.Show("Ajoutez d'abord cet article au panier, puis appliquez son prix exceptionnel.")
                        Return
                    End If
                    ligne.PrixUnitaire = exception.Prix
                    ligne.MotifPrixException = exception.Motif
                    ligne.DemandePrixId = exception.DemandePrixId
                    ligne.Total = ligne.Quantite * ligne.PrixUnitaire
                    RafraichirPanier()
                Else
                    ' Rafraichissement volontaire des tarifs disponibles seulement.
                    ' Les prix du panier deja saisi ne sont jamais remplaces.
                    ChargerProduits()
                End If
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Modification de prix refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
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

            Dim typeTarifId As Integer? = If(typeChoisi.EstPersonnalise, typeChoisi.TypeVenteProduitId, CType(0, Integer?))
            Dim ligne As PanierLigne = _panier.Find(Function(x) x.ProduitId = produitId AndAlso x.Unite = unite AndAlso Nullable.Equals(x.TypeTarifId, typeTarifId))
            If ligne Is Nothing Then
                ligne = New PanierLigne With {.ProduitId = produitId, .Libelle = libelle, .Unite = unite, .TypeTarifId = typeTarifId, .PrixUnitaire = prix, .Quantite = qte, .QuantiteBase = quantiteBase, .QuantiteEquivalente = quantiteEquivalent, .QuantiteReelle = quantiteBase, .Total = prix * qte}
                _panier.Add(ligne)
            Else
                ligne.Quantite += qte
                ligne.QuantiteBase += quantiteBase
                ligne.QuantiteReelle += quantiteBase
                ligne.QuantiteEquivalente = quantiteEquivalent
                ligne.Total = ligne.PrixUnitaire * ligne.Quantite
            End If

            RafraichirPanier(ligne)
            txtQuantite.Clear()
            txtQuantite.Focus()
        End Sub
        Private Sub RetirerDuPanier(sender As Object, e As EventArgs)
            If gridPanier.CurrentRow Is Nothing Then Return
            Dim selection As PanierLigne = TryCast(gridPanier.CurrentRow.DataBoundItem, PanierLigne)
            If selection IsNot Nothing Then _panier.Remove(selection)
            RafraichirPanier()
        End Sub
        Private Sub RafraichirPanier(Optional selection As PanierLigne = Nothing)
            If selection Is Nothing AndAlso gridPanier.CurrentRow IsNot Nothing Then selection = TryCast(gridPanier.CurrentRow.DataBoundItem, PanierLigne)
            selection = DelegationPrixRegles.ChoisirArticle(_panier, selection)
            gridPanier.DataSource = Nothing
            gridPanier.DataSource = DelegationPrixRegles.InstantaneArticles(_panier)
            If gridPanier.Columns.Contains("ProduitId") Then gridPanier.Columns("ProduitId").Visible = False
            If gridPanier.Columns.Contains("DemandePrixId") Then gridPanier.Columns("DemandePrixId").Visible = False
            If gridPanier.Columns.Contains("ContextePrix") Then gridPanier.Columns("ContextePrix").Visible = False
            If gridPanier.Columns.Contains("TypeTarifId") Then gridPanier.Columns("TypeTarifId").Visible = False
            If gridPanier.Columns.Contains("QuantiteBase") Then gridPanier.Columns("QuantiteBase").Visible = False
            If gridPanier.Columns.Contains("Quantite") Then gridPanier.Columns("Quantite").HeaderText = "Quantité saisie"
            If gridPanier.Columns.Contains("QuantiteEquivalente") Then gridPanier.Columns("QuantiteEquivalente").HeaderText = "Quantité équivalente"
            If gridPanier.Columns.Contains("QuantiteReelle") Then gridPanier.Columns("QuantiteReelle").HeaderText = "Quantité réelle"

            ' Le rebinding et le masquage des colonnes techniques peuvent perdre
            ' CurrentRow. Restaurer une cellule visible de la meme ligne, jamais
            ' un premier article arbitraire dans un panier a plusieurs lignes.
            gridPanier.ClearSelection()
            gridPanier.CurrentCell = Nothing
            If selection IsNot Nothing Then
                For Each row As DataGridViewRow In gridPanier.Rows
                    If Not Object.ReferenceEquals(row.DataBoundItem, selection) Then Continue For
                    For Each cell As DataGridViewCell In row.Cells
                        If cell.OwningColumn.Visible Then
                            gridPanier.CurrentCell = cell
                            row.Selected = True
                            Exit For
                        End If
                    Next
                    Exit For
                Next
            End If

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
            ActualiserSelectionCommandesPrix()
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

        Public Function ChargerFacturePourEdition(factureVenteId As Integer, numeroFacture As String, clientNom As String, telephone As String) As Boolean
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim entete As DataRow = New FactureOperationService(dal).LireEntetePourEdition(factureVenteId)
                If Convert.ToString(entete("Statut")) <> "EN_ATTENTE" Then
                    MessageBox.Show("Seules les factures brouillon peuvent être modifiees.")
                    Return False
                End If

                Dim ligneRepo As New LigneFactureVenteRepository(dal)
                Dim dtLignes As DataTable = ligneRepo.ListerDetailsPourEdition(factureVenteId)

                ' Construire avant de remplacer le panier : une ancienne ligne
                ' incompatible ne doit pas laisser une reprise partielle.
                Dim articlesCharges As New List(Of PanierLigne)()
                For Each row As DataRow In dtLignes.Rows
                    Dim qteSaisie As Decimal = If(dtLignes.Columns.Contains("QuantiteSaisie") AndAlso Not row.IsNull("QuantiteSaisie"), Convert.ToDecimal(row("QuantiteSaisie")), Convert.ToDecimal(row("Quantite")))
                    Dim qteBase As Decimal = If(dtLignes.Columns.Contains("QuantiteBase") AndAlso Not row.IsNull("QuantiteBase"), Convert.ToDecimal(row("QuantiteBase")), qteSaisie)
                    Dim prix As Decimal = Convert.ToDecimal(row("PrixUnitaire"))
                    Dim totalLigne As Decimal = Convert.ToDecimal(row("MontantLigne"))
                    Dim unite As String = Convert.ToString(row("TypeVente"))
                    Dim quantiteEquivalente As Decimal = If(qteSaisie = 0D, qteBase, qteBase / qteSaisie)

                    articlesCharges.Add(New PanierLigne With {
                        .ProduitId = Convert.ToInt32(row("ProduitId")),
                        .Libelle = Convert.ToString(row("Libelle")),
                        .Unite = unite,
                        .PrixUnitaire = prix,
                        .MotifPrixException = Convert.ToString(row("MotifPrixException")),
                        .DemandePrixId = If(row.IsNull("DemandePrixId"), CType(Nothing, Integer?), CInt(row("DemandePrixId"))),
                        .TypeTarifId = If(row.IsNull("TypeTarifId"), CType(Nothing, Integer?), CInt(row("TypeTarifId"))),
                        .ContextePrix = If(row.IsNull("ContextePrix"), Guid.NewGuid(), DirectCast(row("ContextePrix"), Guid)),
                        .Quantite = qteSaisie,
                        .QuantiteBase = qteBase,
                        .QuantiteEquivalente = quantiteEquivalente,
                        .QuantiteReelle = qteBase,
                        .Total = totalLigne
                    })
                Next

                _factureEnEditionId = factureVenteId
                _versionFactureEdition = DirectCast(entete("VersionOperation"), Byte())
                _clientEditionId = If(entete.IsNull("ClientId"), CType(Nothing, Integer?), Convert.ToInt32(entete("ClientId")))
                _clientEditionNom = Convert.ToString(entete("NomClient"))
                _clientEditionTelephone = NettoyerTelephone(Convert.ToString(entete("Telephone")))
                _remiseEditionMontant = Convert.ToDecimal(entete("MontantRemise"))
                _sousTotalEdition = Convert.ToDecimal(entete("SousTotal"))
                ' Garder le montant exact d'une remise inchangee.
                _remiseEditionTexte = If(_sousTotalEdition = 0D, "0", (_remiseEditionMontant / _sousTotalEdition * 100D).ToString())
                txtNumeroFacture.Text = Convert.ToString(entete("NumeroFacture"))
                txtClientNom.Text = _clientEditionNom
                txtClientTel.Text = _clientEditionTelephone
                txtClientId.Text = ""
                txtRemise.Text = _remiseEditionTexte
                btnValider.Text = TexteValidation
                _panier.Clear()
                _panier.AddRange(articlesCharges)
                RafraichirPanier()
                ChargerProduits()
                Return True
            Catch ex As Exception
                MessageBox.Show("Erreur chargement facture brouillon: " & ex.Message)
                Return False
            End Try
        End Function

        Protected Overridable ReadOnly Property TexteValidation As String
            Get
                Return "VALIDER LA VENTE"
            End Get
        End Property
        Protected Sub ConfigurerComptoir(titre As String, ParamArray commandes As Button())
            lblAppTitle.AutoSize = False
            lblAppTitle.Width = 680
            lblAppTitle.Height = 58
            lblAppTitle.Top = 6
            lblAppTitle.Font = FontBold
            lblAppTitle.Text = titre
            Dim index As Integer = 1
            For Each bouton As Button In commandes
                bouton.Width = 185 : bouton.Height = 42 : bouton.FlatStyle = FlatStyle.Flat
                bouton.BackColor = ColorPrimary : bouton.ForeColor = Color.White
                actionsPanier.Controls.Add(bouton)
                actionsPanier.Controls.SetChildIndex(bouton, index)
                index += 1
            Next
            If commandes.Length > 0 Then
                btnImprimer.Visible = False
                btnPdf.Visible = False
                btnAnnuler.Text = "EFFACER LE PANIER"
                Dim droits As New DelegationPrixService(New DAL(ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString))
                btnHistorique.Visible = droits.Autorise("HISTORIQUE_FACTURES", "HISTORIQUE_FACTURES")
            End If
        End Sub
        Protected ReadOnly Property FactureEnEdition As Integer?
            Get
                Return _factureEnEditionId
            End Get
        End Property
        Protected Sub EnregistrerPreparation()
            ValiderFacture(Me, EventArgs.Empty)
        End Sub
        Protected Sub AfficherZoneEncaissement(contenu As Control)
            zoneEncaissement.Controls.Clear()
            For Each voisin As Control In zoneEncaissement.Parent.Controls
                If voisin IsNot zoneEncaissement Then voisin.Visible = contenu Is Nothing
            Next
            zoneEncaissement.Visible = contenu IsNot Nothing
            If contenu Is Nothing Then Return
            contenu.Dock = DockStyle.Fill
            zoneEncaissement.Controls.Add(contenu)
            zoneEncaissement.BringToFront()
        End Sub
        Protected Sub VerrouillerPreparation(verrouille As Boolean)
            txtClientNom.Parent.Enabled = Not verrouille
            btnAjouter.Parent.Enabled = Not verrouille
            gridPanier.Parent.Enabled = Not verrouille
            txtRemise.Enabled = Not verrouille
            btnAnnuler.Enabled = Not verrouille
            btnValider.Text = TexteValidation
        End Sub
        Protected Sub EffacerPreparation()
            _abandonConfirme = True
            Try
                AnnulerFacture(Me, EventArgs.Empty)
            Finally
                _abandonConfirme = False
            End Try
        End Sub
        Protected Overridable Function ConfirmerAbandon() As Boolean
            Return True
        End Function
        Protected Overridable Function PersisterOperation(dal As DAL, facture As FactureVente, lignes As IList(Of LigneFactureVente), motif As String, version As Byte(), client As Client) As Integer
            Return New FactureOperationService(dal).Enregistrer(facture, lignes, motif, version, client, Not _enregistrementBrouillon)
        End Function
        Protected Overridable Function MessageOperation() As String
            Return If(_factureEnEditionId.HasValue, "Facture brouillon mise à jour : ", "Facture en attente : ")
        End Function
        Protected Overridable Function DisponibiliteAvantValidation() As Boolean
            Return VerifierStockAvantValidation()
        End Function

        Private Sub ValiderFacture(sender As Object, e As EventArgs)
            If _validationEnCours Then Return
            _validationEnCours = True
            Dim factureEnregistree As Integer = 0
            Dim evenementFactureEmis As Boolean = False
            Try
                If _panier.Count = 0 Then
                    MessageBox.Show("Panier vide.")
                    Return
                End If

                If Not DisponibiliteAvantValidation() Then
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
                If _factureEnEditionId.HasValue AndAlso sousTotal = _sousTotalEdition AndAlso txtRemise.Text.Trim() = _remiseEditionTexte Then remiseMontant = _remiseEditionMontant
                Dim total As Decimal = sousTotal - remiseMontant

                Dim clientId As Integer? = Nothing
                Dim clientNouveau As Client = Nothing
                Dim tel As String = NettoyerTelephone(txtClientTel.Text)
                Dim nom As String = txtClientNom.Text.Trim()
                If tel <> "" AndAlso Not NumeroTelephoneValide(tel) Then
                    MessageBox.Show("Le numéro de téléphone doit contenir exactement 10 chiffres.")
                    txtClientTel.Focus()
                    txtClientTel.SelectAll()
                    Return
                End If

                If _factureEnEditionId.HasValue AndAlso AuditMetierRegles.ClientEditionInchange(_clientEditionId, _clientEditionNom, _clientEditionTelephone, nom, tel) Then
                    clientId = _clientEditionId
                ElseIf tel <> "" Then
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
                        clientNouveau = nouveau
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
                    clientNouveau = nouveau
                End If

                Dim motif As String = String.Empty
                If _factureEnEditionId.HasValue Then
                    motif = DialogueMotifOperation.Demander(Me, "Modification de la facture")
                    If motif Is Nothing Then Return
                End If
                Dim factureMaj As New FactureVente With {
                    .FactureVenteId = If(_factureEnEditionId.HasValue, _factureEnEditionId.Value, 0),
                    .NumeroFacture = numeroFacture, .ClientId = clientId, .SousTotal = sousTotal,
                    .MontantRemise = remiseMontant, .MontantTaxe = 0D, .MontantTotal = total, .Statut = "EN_ATTENTE"
                }
                Dim lignes As New List(Of LigneFactureVente)()
                For Each l As PanierLigne In _panier
                    lignes.Add(New LigneFactureVente With {.ProduitId = l.ProduitId, .Quantite = l.Quantite,
                        .QuantiteBase = l.QuantiteBase, .TypeVente = l.Unite, .PrixUnitaire = l.PrixUnitaire,
                        .MontantRemise = 0D, .QteSaisie = l.Quantite, .MotifPrixException = l.MotifPrixException, .DemandePrixId = l.DemandePrixId, .ContextePrix = l.ContextePrix, .TypeTarifId = l.TypeTarifId})
                Next
                Dim factureId As Integer = PersisterOperation(dal, factureMaj, lignes, motif, _versionFactureEdition, clientNouveau)
                If factureId = 0 Then Return
                factureEnregistree = factureId

                AppDataVersionService.Touch("FACTURES")
                AppEvents.OnDataChanged()

                MessageBox.Show(MessageOperation() & numeroFacture)
                _panier.Clear()
                _factureEnEditionId = Nothing
                btnValider.Text = TexteValidation
                RafraichirPanier()
                ChargerProduits()
                GenererNouveauNumeroFacture()
                evenementFactureEmis = True
                RaiseEvent FactureEnregistree(factureId)
            Catch ex As Exception
                If TypeOf ex Is FactureEnregistreeAffichageException Then
                    factureEnregistree = DirectCast(ex, FactureEnregistreeAffichageException).FactureId
                End If
                If factureEnregistree > 0 Then
                    ' Une erreur UI apres commit ne doit jamais laisser ce panier
                    ' disponible pour une seconde soumission commerciale.
                    _panier.Clear()
                    _factureEnEditionId = Nothing
                    btnValider.Enabled = False
                    txtNumeroFacture.Text = ""
                    Try
                        RafraichirPanier()
                    Catch nettoyage As Exception
                        Dim log As New ProductionLogService()
                        log.Error("FacturationForm", "NettoyageApresCommit", "Facture enregistrée : " & factureEnregistree.ToString(), nettoyage)
                    End Try
                    GenererNouveauNumeroFacture()
                    If Not evenementFactureEmis Then
                        Try
                            evenementFactureEmis = True
                            RaiseEvent FactureEnregistree(factureEnregistree)
                        Catch notification As Exception
                            Dim log As New ProductionLogService()
                            log.Error("FacturationForm", "NotificationApresCommit", "Facture enregistrée : " & factureEnregistree.ToString(), notification)
                        End Try
                    End If
                End If
                MessageBox.Show(If(factureEnregistree > 0, "La facture " & factureEnregistree.ToString() & " est enregistrée. Erreur de rafraîchissement : consultez l'historique avant toute nouvelle saisie. ", "Erreur validation facture : ") & ex.Message)
            Finally
                _validationEnCours = False
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
                    Using doc As Printing.PrintDocument = FacturePrintRenderer.CreerDocumentA4(ConstruireDonneesProforma(), _parametres)
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

        Private Sub ImprimerProformaA4()
            _parametres = PrintConfigurationHelper.ChargerParametres()
            Using doc As Printing.PrintDocument = FacturePrintRenderer.CreerDocumentA4(ConstruireDonneesProforma(), _parametres)
                _parametres = PrintConfigurationHelper.ConfigurerDocumentA4(doc, Me, "FacturationForm", "ImprimerProformaA4")

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
            _parametres = PrintConfigurationHelper.ChargerParametres()
            Using doc As Printing.PrintDocument = FacturePrintRenderer.CreerDocumentThermique(ConstruireDonneesProforma(), _parametres, Me, "FacturationForm", "ImprimerProformaThermique")
                Using preview As New FormApercuTicket(doc, Sub() doc.Print())
                    preview.ShowDialog(Me)
                End Using
            End Using
        End Sub

        Private Function CalculerSousTotalPanier() As Decimal
            Return _panier.Sum(Function(l) l.Total)
        End Function

        Private Function CalculerRemiseMontant(sousTotal As Decimal) As Decimal
            Dim remisePourcent As Decimal
            Decimal.TryParse(txtRemise.Text.Trim(), remisePourcent)
            Return sousTotal * remisePourcent / 100D
        End Function

        Private Function ConstruireDonneesProforma() As FacturePrintData
            Dim sousTotal As Decimal = CalculerSousTotalPanier()
            Dim remiseMontant As Decimal = CalculerRemiseMontant(sousTotal)
            Dim lignes As New List(Of FacturePrintLine)()
            For Each ligne As PanierLigne In _panier
                lignes.Add(New FacturePrintLine With {
                    .Produit = ligne.Libelle,
                    .Conditionnement = ligne.Unite,
                    .Quantite = ligne.Quantite,
                    .PrixUnitaire = ligne.PrixUnitaire,
                    .Montant = ligne.Total
                })
            Next

            Return New FacturePrintData With {
                .Numero = txtNumeroFacture.Text.Trim(),
                .DateDocument = Date.Now,
                .Client = If(txtClientNom.Text.Trim() = "", "CLIENT", txtClientNom.Text.Trim()),
                .Telephone = txtClientTel.Text.Trim(),
                .Facturier = If(String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur), "SYSTEM", SessionUtilisateur.NomUtilisateur),
                .Statut = "EN_ATTENTE",
                .SousTotal = sousTotal,
                .Remise = remiseMontant,
                .Total = sousTotal - remiseMontant,
                .Lignes = lignes
            }
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
            Try
                Dim droits As New DelegationPrixService(New DAL(ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString))
                If Not droits.Autorise("HISTORIQUE_FACTURES", "HISTORIQUE_FACTURES") Then
                    MessageBox.Show("L'historique n'est pas autorisé pour cette session.")
                    Return
                End If
                Using f As New FormulaireFactures()
                    f.ShowDialog(Me)
                End Using
            Catch ex As Exception
                MessageBox.Show("Historique inaccessible : " & ex.Message, "Historique", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        Private Sub AnnulerFacture(sender As Object, e As EventArgs)
            If _panier.Count > 0 AndAlso Not _abandonConfirme AndAlso Not ConfirmerAbandon() Then Return
            _panier.Clear()
            _factureEnEditionId = Nothing
            txtClientId.Text = ""
            txtClientNom.Text = ""
            txtClientTel.Text = ""
            txtRemise.Text = ""
            btnValider.Text = TexteValidation
            RafraichirPanier()
            GenererNouveauNumeroFacture()
        End Sub

        Private Sub Deconnecter(sender As Object, e As EventArgs)
            Dim main As MainForm = Nothing
            For Each ouvert As Form In Application.OpenForms
                If TypeOf ouvert Is MainForm Then main = DirectCast(ouvert, MainForm)
            Next
            If main IsNot Nothing Then
                main.DeconnecterDepuisModuleSession()
            Else
                If AutoriserSortie() Then Me.Close()
            End If
        End Sub

        Public Overridable Function AutoriserSortie() As Boolean Implements ISortieSessionControlee.AutoriserSortie
            If Me.UseWaitCursor Then Return False
            If _panier.Count = 0 Then Return True
            Dim choix As DialogResult = MessageBox.Show(Me, "Une facture est en préparation. Oui : enregistrer le brouillon. Non : abandonner explicitement le panier. Annuler : rester sur cette facture.", "Facture non finalisée", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button3)
            If choix = DialogResult.Cancel Then Return False
            If choix = DialogResult.Yes Then
                _enregistrementBrouillon = True
                Try
                    ValiderFacture(Me, EventArgs.Empty)
                Finally
                    _enregistrementBrouillon = False
                End Try
                Return _panier.Count = 0
            End If
            _panier.Clear()
            RafraichirPanier()
            Return True
        End Function

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            RemoveHandler AppEvents.ProduitModifie, AddressOf RafraichirProduitsDepuisEvenement
            RemoveHandler AppEvents.StockModifie, AddressOf RafraichirProduitsDepuisEvenement
            RemoveHandler AppEvents.RolePermissionsChanged, AddressOf RafraichirAutorisationsPrix
            aideDelegationPrix.Dispose()
            If _dataMonitor IsNot Nothing Then
                RemoveHandler _dataMonitor.DomaineModifie, AddressOf RafraichirProduitsDepuisVersionSql
                _dataMonitor.Dispose()
                _dataMonitor = Nothing
            End If
            MyBase.OnFormClosed(e)
        End Sub
    End Class
End Namespace
