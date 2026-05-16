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
        Private _dernierBonGenereAutoId As Integer
        Private _bonLigneCouranteId As Integer
        Private _pageCourante As Integer
        Private _texteRecherche As String
        Private _chargement As Boolean

        Public Sub New()
            Me.Text = "Approvisionnement"
            Me.Width = 1320
            Me.Height = 820
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = Color.FromArgb(240, 244, 252)

            aide = New ToolTip() With {.IsBalloon = False, .ShowAlways = True}
            _pageCourante = 1
            _texteRecherche = String.Empty
            _chargement = True

            tabs = New TabControl() With {.Dock = DockStyle.Fill}
            Dim tabGestion As New TabPage("Gestion")
            Dim tabAuto As New TabPage("Génération auto")
            Dim tabHistorique As New TabPage("Historique")
            tabs.TabPages.Add(tabGestion)
            tabs.TabPages.Add(tabAuto)
            tabs.TabPages.Add(tabHistorique)

            panelHero = New Panel() With {.Dock = DockStyle.Top, .Height = 86}
            lblHeroTitre = New Label() With {.Left = 22, .Top = 16, .AutoSize = True, .Font = New Font("Segoe UI Semibold", 18.0F, FontStyle.Bold)}
            lblHeroSousTitre = New Label() With {.Left = 24, .Top = 50, .AutoSize = True, .Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)}
            panelHero.Controls.Add(lblHeroTitre)
            panelHero.Controls.Add(lblHeroSousTitre)

            panelTopActions = New Panel() With {.Dock = DockStyle.Top, .Height = 118}
            txtRecherche = New TextBox() With {.Left = 20, .Top = 30, .Width = 240}
            txtNumeroBon = New TextBox() With {.Left = 285, .Top = 30, .Width = 150, .ReadOnly = True}
            cmbFournisseur = New ComboBox() With {.Left = 455, .Top = 30, .Width = 220, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbTypePaiement = New ComboBox() With {.Left = 695, .Top = 30, .Width = 150, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbTypePaiement.Items.AddRange(New Object() {"Cash", "Mobile Money", "Virement", "Crédit"})
            btnNouveau = New Button() With {.Text = "Nouveau", .Left = 865, .Top = 24, .Width = 95}
            btnGenerer = New Button() With {.Text = "Génération auto", .Left = 970, .Top = 24, .Width = 135}
            btnActualiser = New Button() With {.Text = "Actualiser", .Left = 1115, .Top = 24, .Width = 100}
            btnValiderBon = New Button() With {.Text = "Valider", .Left = 865, .Top = 68, .Width = 95}
            btnReceptionner = New Button() With {.Text = "Réceptionner", .Left = 970, .Top = 68, .Width = 135}
            btnImprimer = New Button() With {.Text = "Imprimer A4", .Left = 1115, .Top = 68, .Width = 100}
            btnSupprimerBon = New Button() With {.Text = "Supprimer", .Left = 865, .Top = 68, .Width = 95, .Visible = False}
            lblTotalBon = New Label() With {.Text = "Total bon: 0", .Left = 20, .Top = 82, .AutoSize = True, .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)}
            lblStatutBon = New Label() With {.Text = "Statut: EnAttente", .Left = 200, .Top = 82, .AutoSize = True, .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)}

            panelTopActions.Controls.Add(New Label() With {.Text = "Recherche", .Left = 20, .Top = 8, .AutoSize = True})
            panelTopActions.Controls.Add(New Label() With {.Text = "Numéro bon", .Left = 285, .Top = 8, .AutoSize = True})
            panelTopActions.Controls.Add(New Label() With {.Text = "Fournisseur", .Left = 455, .Top = 8, .AutoSize = True})
            panelTopActions.Controls.Add(New Label() With {.Text = "Paiement", .Left = 695, .Top = 8, .AutoSize = True})
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

            gridBons = New DataGridView() With {
                .Left = 20,
                .Top = 118,
                .Width = 1230,
                .Height = 220,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AutoGenerateColumns = True,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect
            }

            btnPagePrecedente = New Button() With {.Text = "<", .Left = 1000, .Top = 346, .Width = 40}
            lblPagination = New Label() With {.Text = "Page 1/1", .Left = 1050, .Top = 352, .AutoSize = True}
            btnPageSuivante = New Button() With {.Text = ">", .Left = 1140, .Top = 346, .Width = 40}

            grpApproManuel = New GroupBox() With {.Text = "Approvisionnement manuel", .Left = 20, .Top = 380, .Width = 1230, .Height = 120}
            txtRechercheProduit = New TextBox() With {.Left = 20, .Top = 46, .Width = 180}
            txtProduitChoisi = New TextBox() With {.Left = 215, .Top = 46, .Width = 250}
            txtPrixPrecedent = New TextBox() With {.Left = 480, .Top = 46, .Width = 110, .ReadOnly = True}
            txtPrixAchat = New TextBox() With {.Left = 605, .Top = 46, .Width = 110}
            txtQuantite = New TextBox() With {.Left = 730, .Top = 46, .Width = 100}
            lblTotalLigne = New Label() With {.Text = "Total ligne: 0", .Left = 850, .Top = 50, .AutoSize = True, .Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)}
            btnAjouterLigne = New Button() With {.Text = "Ajouter ligne", .Left = 980, .Top = 40, .Width = 110}
            btnRetirerLigne = New Button() With {.Text = "Retirer", .Left = 1105, .Top = 40, .Width = 95}

            grpApproManuel.Controls.Add(New Label() With {.Text = "Produit", .Left = 20, .Top = 24, .AutoSize = True})
            grpApproManuel.Controls.Add(New Label() With {.Text = "Choix", .Left = 215, .Top = 24, .AutoSize = True})
            grpApproManuel.Controls.Add(New Label() With {.Text = "Prix précédent", .Left = 480, .Top = 24, .AutoSize = True})
            grpApproManuel.Controls.Add(New Label() With {.Text = "Prix achat", .Left = 605, .Top = 24, .AutoSize = True})
            grpApproManuel.Controls.Add(New Label() With {.Text = "Quantité", .Left = 730, .Top = 24, .AutoSize = True})
            grpApproManuel.Controls.Add(txtRechercheProduit)
            grpApproManuel.Controls.Add(txtProduitChoisi)
            grpApproManuel.Controls.Add(txtPrixPrecedent)
            grpApproManuel.Controls.Add(txtPrixAchat)
            grpApproManuel.Controls.Add(txtQuantite)
            grpApproManuel.Controls.Add(lblTotalLigne)
            grpApproManuel.Controls.Add(btnAjouterLigne)
            grpApproManuel.Controls.Add(btnRetirerLigne)

            gridLignes = New DataGridView() With {.Left = 20, .Top = 515, .Width = 1230, .Height = 180, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AutoGenerateColumns = True, .SelectionMode = DataGridViewSelectionMode.FullRowSelect}

            tabGestion.Controls.Add(panelTopActions)
            tabGestion.Controls.Add(panelHero)
            tabGestion.Controls.Add(gridBons)
            tabGestion.Controls.Add(btnPagePrecedente)
            tabGestion.Controls.Add(lblPagination)
            tabGestion.Controls.Add(btnPageSuivante)
            tabGestion.Controls.Add(grpApproManuel)
            tabGestion.Controls.Add(gridLignes)

            grpSuggestions = New GroupBox() With {.Text = "Produits critiques à approvisionner", .Left = 20, .Top = 20, .Width = 1230, .Height = 640}
            gridSuggestions = New DataGridView() With {.Left = 20, .Top = 35, .Width = 1190, .Height = 580, .ReadOnly = False, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AutoGenerateColumns = True}
            btnImprimerGenerationAuto = New Button() With {.Text = "Imprimer bon", .Left = 1050, .Top = 590, .Width = 160}
            grpSuggestions.Controls.Add(gridSuggestions)
            grpSuggestions.Controls.Add(btnImprimerGenerationAuto)
            tabAuto.Controls.Add(grpSuggestions)

            panelHistoriqueFiltres = New Panel() With {.Dock = DockStyle.Top, .Height = 55}
            cmbAnnee = New ComboBox() With {.Left = 20, .Top = 20, .Width = 120, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbMois = New ComboBox() With {.Left = 160, .Top = 20, .Width = 120, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbMois.Items.Add("")
            For i As Integer = 1 To 12
                cmbMois.Items.Add(i.ToString("00"))
            Next
            panelHistoriqueFiltres.Controls.Add(New Label() With {.Text = "Année", .Left = 20, .Top = 0, .AutoSize = True})
            panelHistoriqueFiltres.Controls.Add(New Label() With {.Text = "Mois", .Left = 160, .Top = 0, .AutoSize = True})
            panelHistoriqueFiltres.Controls.Add(cmbAnnee)
            panelHistoriqueFiltres.Controls.Add(cmbMois)

            gridHistorique = New DataGridView() With {.Left = 20, .Top = 70, .Width = 520, .Height = 250, .ReadOnly = True, .AllowUserToAddRows = False, .AutoGenerateColumns = True}
            gridTopProduits = New DataGridView() With {.Left = 20, .Top = 340, .Width = 520, .Height = 260, .ReadOnly = True, .AllowUserToAddRows = False, .AutoGenerateColumns = True}
            chartHistorique = New Chart() With {.Left = 570, .Top = 70, .Width = 320, .Height = 250}
            chartFournisseurs = New Chart() With {.Left = 910, .Top = 70, .Width = 320, .Height = 250}
            ConfigurerChart(chartHistorique, SeriesChartType.Column, "ApproMensuel")
            ConfigurerChart(chartFournisseurs, SeriesChartType.Pie, "RepartitionFournisseurs")

            tabHistorique.Controls.Add(panelHistoriqueFiltres)
            tabHistorique.Controls.Add(gridHistorique)
            tabHistorique.Controls.Add(gridTopProduits)
            tabHistorique.Controls.Add(chartHistorique)
            tabHistorique.Controls.Add(chartFournisseurs)

            Me.Controls.Add(tabs)

            AddHandler btnActualiser.Click, AddressOf ChargerBons
            AddHandler btnNouveau.Click, AddressOf NouveauBon
            AddHandler btnGenerer.Click, AddressOf GenererAuto
            AddHandler btnReceptionner.Click, AddressOf ReceptionnerBon
            AddHandler btnValiderBon.Click, AddressOf ValiderBon
            AddHandler btnImprimer.Click, AddressOf ImprimerBonA4
            AddHandler btnImprimerGenerationAuto.Click, AddressOf ImprimerBonAuto
            AddHandler btnSupprimerBon.Click, AddressOf SupprimerBon
            AddHandler btnRetirerLigne.Click, AddressOf RetirerLigne
            AddHandler btnPagePrecedente.Click, AddressOf PagePrecedente
            AddHandler btnPageSuivante.Click, AddressOf PageSuivante
            AddHandler gridBons.SelectionChanged, AddressOf ChargerLignes
            AddHandler gridLignes.SelectionChanged, AddressOf MemoriserLigneSelectionnee
            AddHandler gridLignes.CellClick, AddressOf MemoriserLigneSelectionnee
            AddHandler txtRecherche.TextChanged, AddressOf RechercherBons
            AddHandler txtRechercheProduit.TextChanged, AddressOf RechercherProduit
            AddHandler txtPrixAchat.TextChanged, AddressOf MettreAJourTotalLigne
            AddHandler txtQuantite.TextChanged, AddressOf MettreAJourTotalLigne
            AddHandler btnAjouterLigne.Click, AddressOf AjouterLigne
            AddHandler cmbAnnee.SelectedIndexChanged, AddressOf ChargerHistorique
            AddHandler cmbMois.SelectedIndexChanged, AddressOf ChargerHistorique
            AddHandler cmbFournisseur.SelectedValueChanged, AddressOf EnregistrerEnteteCourante
            AddHandler cmbTypePaiement.SelectedIndexChanged, AddressOf EnregistrerEnteteCourante

            ThemeHelper.AppliquerTheme(Me)
            AppliquerStyleApprovisionnement()
            Initialiser()
            _chargement = False

            timer = New Timer() With {.Interval = 600000}
            AddHandler timer.Tick, AddressOf RafraichirTempsReel
            timer.Start()
        End Sub

        Private Sub AppliquerStyleApprovisionnement()
            Dim bleuFonce As Color = Color.FromArgb(17, 35, 74)
            Dim bleuClair As Color = Color.FromArgb(80, 170, 255)
            Dim blanc As Color = Color.White
            Dim vert As Color = Color.FromArgb(42, 168, 94)
            Dim rouge As Color = Color.FromArgb(220, 70, 70)
            Dim orange As Color = Color.FromArgb(227, 155, 49)
            Dim grisTexte As Color = Color.FromArgb(78, 92, 114)

            Me.BackColor = Color.FromArgb(232, 238, 248)
            tabs.BackColor = bleuFonce
            panelHero.BackColor = bleuFonce
            lblHeroTitre.Text = "Pilotage des approvisionnements"
            lblHeroTitre.ForeColor = Color.White
            lblHeroSousTitre.Text = "Bons, suggestions critiques, réception pilotée et historique consolidé"
            lblHeroSousTitre.ForeColor = Color.FromArgb(206, 221, 246)
            For Each page As TabPage In tabs.TabPages
                page.BackColor = Color.FromArgb(245, 248, 252)
                page.Padding = New Padding(14)
            Next

            AppliquerStyleBouton(btnNouveau, bleuClair)
            AppliquerStyleBouton(btnGenerer, bleuClair)
            AppliquerStyleBouton(btnActualiser, bleuClair)
            AppliquerStyleBouton(btnValiderBon, orange)
            AppliquerStyleBouton(btnReceptionner, vert)
            AppliquerStyleBouton(btnImprimer, bleuClair)
            AppliquerStyleBouton(btnImprimerGenerationAuto, bleuClair)
            AppliquerStyleBouton(btnSupprimerBon, rouge)
            AppliquerStyleBouton(btnAjouterLigne, vert)
            AppliquerStyleBouton(btnRetirerLigne, rouge)
            AppliquerStyleBouton(btnPagePrecedente, bleuClair)
            AppliquerStyleBouton(btnPageSuivante, bleuClair)

            aide.SetToolTip(btnNouveau, "Créer un nouveau bon d'approvisionnement.")
            aide.SetToolTip(btnGenerer, "Créer automatiquement un bon sur base du stock critique.")
            aide.SetToolTip(btnReceptionner, "Ouvrir EntréeStock sans modifier le stock ici.")
            aide.SetToolTip(btnValiderBon, "Valider un bon seulement après contrôle fournisseur et lignes.")
            aide.SetToolTip(btnSupprimerBon, "Supprimer complètement un bon encore en attente.")
            aide.SetToolTip(btnRetirerLigne, "Supprimer la ligne sélectionnée dans la grille.")
            aide.SetToolTip(btnImprimer, "Impression A4 complète du bon d'approvisionnement.")
            aide.SetToolTip(btnImprimerGenerationAuto, "Imprimer le bon généré automatiquement ou le bon actuellement actif.")
            aide.SetToolTip(txtRecherche, "Recherche temps réel par numéro, fournisseur ou statut.")
            aide.SetToolTip(txtRechercheProduit, "Tapez le nom du produit pour auto-complétion.")

            For Each zone As Control In New Control() {panelHero, panelTopActions, grpSuggestions, panelHistoriqueFiltres}
                zone.Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)
            Next

            For Each bloc As GroupBox In New GroupBox() {grpApproManuel, grpSuggestions}
                bloc.BackColor = blanc
                bloc.ForeColor = grisTexte
                bloc.FlatStyle = FlatStyle.Flat
            Next

            For Each panneau As Panel In New Panel() {panelTopActions, panelHero, panelHistoriqueFiltres}
                panneau.BackColor = If(panneau Is panelHero, bleuFonce, blanc)
            Next

            For Each grille As DataGridView In New DataGridView() {gridBons, gridLignes, gridSuggestions, gridHistorique, gridTopProduits}
                grille.BackgroundColor = blanc
                grille.BorderStyle = BorderStyle.None
                grille.GridColor = Color.FromArgb(225, 231, 240)
                grille.EnableHeadersVisualStyles = False
                grille.ColumnHeadersDefaultCellStyle.BackColor = bleuFonce
                grille.ColumnHeadersDefaultCellStyle.ForeColor = blanc
                grille.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.0F, FontStyle.Bold)
                grille.RowTemplate.Height = 28
                grille.SelectionMode = DataGridViewSelectionMode.FullRowSelect
                grille.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                grille.AllowUserToResizeRows = False
                grille.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 236, 255)
                grille.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 30, 45)
            Next

            gridSuggestions.ReadOnly = False
            gridSuggestions.EditMode = DataGridViewEditMode.EditOnEnter
            gridBons.Top = 216
            btnPagePrecedente.Top = 448
            lblPagination.Top = 454
            btnPageSuivante.Top = 448
            grpApproManuel.Top = 486
            gridLignes.Top = 622
        End Sub

        Private Sub AppliquerStyleBouton(bouton As Button, couleur As Color)
            bouton.BackColor = couleur
            bouton.ForeColor = Color.White
            bouton.FlatStyle = FlatStyle.Flat
            bouton.FlatAppearance.BorderSize = 0
            bouton.Cursor = Cursors.Hand
            AppliquerCoinsArrondis(bouton, 14)
            AddHandler bouton.MouseEnter, Sub(sender As Object, e As EventArgs)
                                              Dim btn As Button = DirectCast(sender, Button)
                                              btn.BackColor = Eclaircir(btn.BackColor, 18)
                                          End Sub
            AddHandler bouton.MouseLeave, Sub(sender As Object, e As EventArgs)
                                              Dim btn As Button = DirectCast(sender, Button)
                                              btn.BackColor = couleur
                                          End Sub
        End Sub

        Private Sub AppliquerCoinsArrondis(controle As Control, rayon As Integer)
            Dim rect As New Rectangle(0, 0, controle.Width, controle.Height)
            If rect.Width <= 0 OrElse rect.Height <= 0 Then
                Return
            End If

            Using path As New GraphicsPath()
                Dim diametre As Integer = rayon * 2
                path.StartFigure()
                path.AddArc(rect.X, rect.Y, diametre, diametre, 180, 90)
                path.AddArc(rect.Right - diametre, rect.Y, diametre, diametre, 270, 90)
                path.AddArc(rect.Right - diametre, rect.Bottom - diametre, diametre, diametre, 0, 90)
                path.AddArc(rect.X, rect.Bottom - diametre, diametre, diametre, 90, 90)
                path.CloseFigure()
                controle.Region = New Region(path)
            End Using
        End Sub

        Private Function Eclaircir(couleur As Color, delta As Integer) As Color
            Return Color.FromArgb(
                Math.Min(255, couleur.R + delta),
                Math.Min(255, couleur.G + delta),
                Math.Min(255, couleur.B + delta))
        End Function

        Private Sub Initialiser()
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            _dal = New DAL(cs)
            _repo = New BonApprovisionnementRepository(_dal)
            _repo.AssurerTables()

            ChargerFournisseurs()
            ChargerProduits()
            ChargerAnnees()
            ChargerBons(Nothing, EventArgs.Empty)
            ChargerSuggestions()
            ChargerHistorique(Nothing, EventArgs.Empty)
            InitialiserEcranSansCreation()
        End Sub

        Private Sub InitialiserEcranSansCreation()
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
            lblTotalBon.Text = "Total bon: 0"
            gridLignes.DataSource = Nothing
            gridLignes.ClearSelection()
            DefinirStatutAffiche("EnAttente")
        End Sub

        Private Sub ChargerBons(sender As Object, e As EventArgs)
            ChargerSourceBons(_texteRecherche, True)
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

            AfficherPageBons()
        End Sub

        Private Sub AfficherPageBons()
            If _bonsSource Is Nothing Then
                gridBons.DataSource = Nothing
                lblPagination.Text = "Page 0/0"
                Return
            End If

            Dim totalLignes As Integer = _bonsSource.Rows.Count
            Dim totalPages As Integer = Math.Max(1, CInt(Math.Ceiling(totalLignes / CType(TaillePage, Decimal))))
            If _pageCourante > totalPages Then
                _pageCourante = totalPages
            End If

            Dim tablePage As DataTable = _bonsSource.Clone()
            Dim debut As Integer = (_pageCourante - 1) * TaillePage
            Dim fin As Integer = Math.Min(debut + TaillePage - 1, totalLignes - 1)
            If totalLignes > 0 Then
                For index As Integer = debut To fin
                    tablePage.ImportRow(_bonsSource.Rows(index))
                Next
            End If

            gridBons.DataSource = tablePage
            lblPagination.Text = "Page " & _pageCourante.ToString() & "/" & totalPages.ToString()
            btnPagePrecedente.Enabled = _pageCourante > 1
            btnPageSuivante.Enabled = _pageCourante < totalPages
            ColorerStatuts()
        End Sub

        Private Sub RechercherBons(sender As Object, e As EventArgs)
            _texteRecherche = txtRecherche.Text.Trim()
            ChargerSourceBons(_texteRecherche, True)
        End Sub

        Private Sub PagePrecedente(sender As Object, e As EventArgs)
            If _pageCourante <= 1 Then
                Return
            End If
            _pageCourante -= 1
            AfficherPageBons()
        End Sub

        Private Sub PageSuivante(sender As Object, e As EventArgs)
            If _bonsSource Is Nothing Then
                Return
            End If
            Dim totalPages As Integer = Math.Max(1, CInt(Math.Ceiling(_bonsSource.Rows.Count / CType(TaillePage, Decimal))))
            If _pageCourante >= totalPages Then
                Return
            End If
            _pageCourante += 1
            AfficherPageBons()
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
            gridLignes.ClearSelection()
            lblTotalBon.Text = "Total bon: 0"
            DefinirStatutAffiche("EnAttente")
            AjouterNotification("Nouveau bon d'approvisionnement créé : " & txtNumeroBon.Text)
            ChargerBons(Nothing, EventArgs.Empty)
            tabs.SelectedIndex = 0
        End Sub

        Private Function ObtenirNumeroBon(bonId As Integer) As String
            Dim dt As DataTable = _repo.ListerBons()
            Dim lignes() As DataRow = dt.Select("BonId = " & bonId.ToString())
            If lignes.Length = 0 Then
                Return String.Empty
            End If
            Return Convert.ToString(lignes(0)("NumeroBon"))
        End Function

        Private Sub ChargerLignes(sender As Object, e As EventArgs)
            If gridBons.CurrentRow Is Nothing OrElse gridBons.CurrentRow.Cells("BonId").Value Is Nothing Then
                Return
            End If

            _bonCourantId = Convert.ToInt32(gridBons.CurrentRow.Cells("BonId").Value)
            _bonLigneCouranteId = 0
            txtNumeroBon.Text = Convert.ToString(gridBons.CurrentRow.Cells("NumeroBon").Value)
            If gridBons.CurrentRow.Cells("FournisseurId").Value IsNot DBNull.Value Then
                cmbFournisseur.SelectedValue = Convert.ToInt32(gridBons.CurrentRow.Cells("FournisseurId").Value)
            Else
                cmbFournisseur.SelectedIndex = -1
            End If
            cmbTypePaiement.Text = Convert.ToString(gridBons.CurrentRow.Cells("TypePaiement").Value)
            lblTotalBon.Text = "Total bon: " & FormatageGlobal.FormatMontant(Convert.ToDecimal(gridBons.CurrentRow.Cells("TotalBon").Value))
            DefinirStatutAffiche(Convert.ToString(gridBons.CurrentRow.Cells("Statut").Value))
            gridLignes.DataSource = _repo.ListerLignes(_bonCourantId)
            gridLignes.ClearSelection()
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
            If _produits Is Nothing Then
                Return
            End If
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
                    txtPrixPrecedent.Text = ObtenirPrixPrecedent(Convert.ToInt32(row("ProduitId"))).ToString("N0")
                    If txtPrixAchat.Text.Trim() = "" Then
                        txtPrixAchat.Text = txtPrixPrecedent.Text
                    End If
                    Exit For
                End If
            Next
        End Sub

        Private Sub MettreAJourTotalLigne(sender As Object, e As EventArgs)
            Dim quantite As Decimal = LireDecimal(txtQuantite.Text)
            Dim prix As Decimal = LireDecimal(txtPrixAchat.Text)
            lblTotalLigne.Text = "Total ligne: " & FormatageGlobal.FormatMontant(quantite * prix)
        End Sub

        Private Sub AjouterLigne(sender As Object, e As EventArgs)
            If _bonCourantId <= 0 Then
                MessageBox.Show("Créez d'abord un bon.")
                Return
            End If
            If ObtenirStatutCourant() <> "EnAttente" Then
                MessageBox.Show("Seuls les bons en attente peuvent être modifiés.")
                Return
            End If
            If cmbFournisseur.SelectedIndex < 0 Then
                MessageBox.Show("Fournisseur obligatoire.")
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
            _bonLigneCouranteId = 0
            gridLignes.ClearSelection()
            lblTotalBon.Text = "Total bon: " & FormatageGlobal.FormatMontant(CalculerTotalBon(_bonCourantId))
            DefinirStatutAffiche("EnAttente")
            ChargerBons(Nothing, EventArgs.Empty)
        End Sub

        Private Sub RetirerLigne(sender As Object, e As EventArgs)
            If _bonCourantId <= 0 Then
                MessageBox.Show("Aucun bon actif.")
                Return
            End If
            Dim ligneSelectionnee As DataGridViewRow = ObtenirLigneSelectionnee(gridLignes)
            If ligneSelectionnee Is Nothing Then
                MessageBox.Show("Sélectionnez une ligne à supprimer.")
                Return
            End If
            If ObtenirStatutCourant() <> "EnAttente" Then
                MessageBox.Show("Impossible de supprimer une ligne sur un bon déjà validé ou livré.")
                Return
            End If
            Dim ligneId As Integer = _bonLigneCouranteId
            If ligneId <= 0 AndAlso gridLignes.Columns.Contains("BonLigneId") AndAlso ligneSelectionnee.Cells("BonLigneId").Value IsNot Nothing Then
                ligneId = Convert.ToInt32(ligneSelectionnee.Cells("BonLigneId").Value)
            End If
            If ligneId <= 0 Then
                MessageBox.Show("Impossible d'identifier la ligne sélectionnée.")
                Return
            End If
            Dim rep As DialogResult = MessageBox.Show("Retirer cette ligne du bon ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If rep <> DialogResult.Yes Then
                Return
            End If
            _repo.SupprimerLigne(ligneId)
            gridLignes.DataSource = _repo.ListerLignes(_bonCourantId)
            _bonLigneCouranteId = 0
            gridLignes.ClearSelection()
            lblTotalBon.Text = "Total bon: " & FormatageGlobal.FormatMontant(CalculerTotalBon(_bonCourantId))
            ChargerBons(Nothing, EventArgs.Empty)
        End Sub

        Private Sub SupprimerBon(sender As Object, e As EventArgs)
            If _bonCourantId <= 0 Then
                MessageBox.Show("Aucun bon sélectionné.")
                Return
            End If
            If ObtenirStatutCourant() <> "EnAttente" Then
                MessageBox.Show("Seuls les bons en attente peuvent être supprimés.")
                Return
            End If

            Dim rep As DialogResult = MessageBox.Show("Supprimer complètement ce bon brouillon et toutes ses lignes ?", "Suppression du bon", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            If rep <> DialogResult.Yes Then
                Return
            End If

            Dim numero As String = txtNumeroBon.Text
            _repo.SupprimerBon(_bonCourantId)
            AjouterNotification("Bon brouillon supprimé : " & numero)
            ChargerBons(Nothing, EventArgs.Empty)
            NouveauBon(Nothing, EventArgs.Empty)
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

        Private Sub GenererAuto(sender As Object, e As EventArgs)
            If cmbFournisseur.SelectedIndex < 0 Then
                MessageBox.Show("Sélectionnez d'abord un fournisseur.")
                Return
            End If

            If gridSuggestions.DataSource Is Nothing OrElse gridSuggestions.Rows.Count = 0 Then
                ChargerSuggestions()
            End If
            If gridSuggestions.Rows.Count = 0 Then
                MessageBox.Show("Aucun produit critique.")
                Return
            End If

            Dim rep As DialogResult = MessageBox.Show("Créer un bon automatique à partir des produits critiques ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If rep <> DialogResult.Yes Then
                Return
            End If

            Dim apercu As String = ConstruireApercuSuggestions()
            Dim repApercu As DialogResult = MessageBox.Show(apercu, "Aperçu des suggestions", MessageBoxButtons.OKCancel, MessageBoxIcon.Information)
            If repApercu <> DialogResult.OK Then
                Return
            End If

            Dim bonId As Integer = _repo.CreerBon(LireFournisseurIdSelectionne(), cmbTypePaiement.Text, SessionUtilisateur.UtilisateurId, "EnAttente")
            For Each ligne As DataGridViewRow In gridSuggestions.Rows
                If ligne.IsNewRow Then
                    Continue For
                End If
                Dim qte As Decimal = LireDecimal(Convert.ToString(ligne.Cells("QuantiteSuggeree").Value))
                If qte > 0D Then
                    Dim produitId As Integer = Convert.ToInt32(ligne.Cells("ProduitId").Value)
                    Dim prix As Decimal = LireDecimal(Convert.ToString(ligne.Cells("PrixAchatPrecedent").Value))
                    _repo.AjouterLigne(bonId, produitId, qte, prix)
                End If
            Next
            _bonCourantId = bonId
            _dernierBonGenereAutoId = bonId
            _bonLigneCouranteId = 0
            txtNumeroBon.Text = ObtenirNumeroBon(_bonCourantId)
            gridLignes.DataSource = _repo.ListerLignes(_bonCourantId)
            gridLignes.ClearSelection()
            lblTotalBon.Text = "Total bon: " & FormatageGlobal.FormatMontant(CalculerTotalBon(_bonCourantId))
            DefinirStatutAffiche("EnAttente")
            ChargerBons(Nothing, EventArgs.Empty)
            ChargerSuggestions()
            AjouterNotification("Nouveau bon automatique créé : " & txtNumeroBon.Text)
            MessageBox.Show("Bon automatique créé.")
        End Sub

        Private Function ConstruireApercuSuggestions() As String
            Dim texte As String = "Produits qui seront ajoutés au bon :" & Environment.NewLine & Environment.NewLine
            Dim compteur As Integer = 0
            For Each ligne As DataGridViewRow In gridSuggestions.Rows
                If ligne.IsNewRow Then
                    Continue For
                End If
                Dim qte As Decimal = LireDecimal(Convert.ToString(ligne.Cells("QuantiteSuggeree").Value))
                If qte <= 0D Then
                    Continue For
                End If
                texte &= "- " & Convert.ToString(ligne.Cells("Libelle").Value) & " : " & qte.ToString("N0") & Environment.NewLine
                compteur += 1
                If compteur >= 8 Then
                    Exit For
                End If
            Next
            If compteur = 0 Then
                texte &= "Aucune ligne exploitable."
            End If
            Return texte
        End Function

        Private Sub ChargerSuggestions()
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

        Private Sub ReceptionnerBon(sender As Object, e As EventArgs)
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

        Private Sub ImprimerBonA4(sender As Object, e As EventArgs)
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

                        If param IsNot Nothing AndAlso param.LogoPath <> "" AndAlso File.Exists(param.LogoPath) Then
                            Using logo As Image = Image.FromFile(param.LogoPath)
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
                            pe.Graphics.DrawString(Convert.ToDecimal(row("Quantite")).ToString("N0"), fontBloc, Brushes.Black, colQte, y)
                            pe.Graphics.DrawString(Convert.ToDecimal(row("PrixAchat")).ToString("N0"), fontBloc, Brushes.Black, colPrix, y)
                            pe.Graphics.DrawString(Convert.ToDecimal(row("TotalLigne")).ToString("N0"), fontBloc, Brushes.Black, colTotal, y)
                            y += 24
                        Next

                        y += 16
                        pe.Graphics.DrawRectangle(New Pen(Color.FromArgb(17, 35, 74), 1.4F), 520, y, 270, 44)
                        pe.Graphics.DrawString("TOTAL BON", fontBlocGras, pinceauBleu, 536, y + 7)
                        pe.Graphics.DrawString(CalculerTotalBon(_bonCourantId).ToString("N0"), New Font("Segoe UI", 12, FontStyle.Bold), Brushes.Black, 666, y + 8)
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

        Private Sub ImprimerBonAuto(sender As Object, e As EventArgs)
            Dim bonId As Integer = If(_dernierBonGenereAutoId > 0, _dernierBonGenereAutoId, _bonCourantId)
            If bonId <= 0 Then
                MessageBox.Show("Aucun bon généré à imprimer.")
                Return
            End If

            _bonCourantId = bonId
            txtNumeroBon.Text = ObtenirNumeroBon(bonId)
            gridLignes.DataSource = _repo.ListerLignes(bonId)
            lblTotalBon.Text = "Total bon: " & FormatageGlobal.FormatMontant(CalculerTotalBon(bonId))
            ImprimerBonA4(sender, e)
        End Sub

        Private Sub ChargerHistorique(sender As Object, e As EventArgs)
            If cmbAnnee.SelectedIndex < 0 Then
                Return
            End If
            Dim annee As Integer = Convert.ToInt32(cmbAnnee.Text)
            Dim mois As Integer? = Nothing
            If cmbMois.SelectedIndex > 0 Then
                mois = Convert.ToInt32(cmbMois.Text)
            End If

            Dim dtHistorique As DataTable = _repo.HistoriqueParPeriode(annee, mois)
            gridHistorique.DataSource = dtHistorique
            gridTopProduits.DataSource = _repo.ProduitsPlusCommandes(annee, mois)
            AlimenterChart(chartHistorique, dtHistorique, "Mois", "TotalApprovisionnement")
            AlimenterChart(chartFournisseurs, _repo.RepartitionFournisseurs(annee, mois), "Fournisseur", "NombreBons")
        End Sub

        Private Sub RafraichirTempsReel(sender As Object, e As EventArgs)
            ChargerBons(Nothing, EventArgs.Empty)
            ChargerSuggestions()
        End Sub

        Private Sub ChargerAnnees()
            cmbAnnee.Items.Clear()
            For annee As Integer = Date.Now.Year - 4 To Date.Now.Year + 1
                cmbAnnee.Items.Add(annee.ToString())
            Next
            cmbAnnee.Text = Date.Now.Year.ToString()
            cmbMois.SelectedIndex = 0
        End Sub

        Private Function ObtenirProduitIdParLibelle(libelle As String) As Integer
            If _produits Is Nothing Then
                Return 0
            End If
            For Each row As DataRow In _produits.Rows
                If String.Equals(Convert.ToString(row("Libelle")), libelle, StringComparison.OrdinalIgnoreCase) Then
                    Return Convert.ToInt32(row("ProduitId"))
                End If
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
            Dim sql As String = "SELECT ISNULL(SUM(TotalLigne),0) FROM BonApprovisionnementLignes WHERE BonId=@BonId"
            Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {New System.Data.SqlClient.SqlParameter("@BonId", bonId)}
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToDecimal(v)
        End Function

        Private Function LireDecimal(texte As String) As Decimal
            Dim v As Decimal
            If Decimal.TryParse(If(texte.Trim() = "", "0", texte.Trim()), v) Then
                Return v
            End If
            Return 0D
        End Function

        Private Function ObtenirLigneSelectionnee(grille As DataGridView) As DataGridViewRow
            If _bonLigneCouranteId > 0 Then
                For Each row As DataGridViewRow In grille.Rows
                    If grille.Columns.Contains("BonLigneId") AndAlso row.Cells("BonLigneId").Value IsNot Nothing AndAlso Convert.ToInt32(row.Cells("BonLigneId").Value) = _bonLigneCouranteId Then
                        Return row
                    End If
                Next
            End If
            If grille.SelectedRows.Count > 0 Then
                Return grille.SelectedRows(0)
            End If
            If grille.SelectedCells.Count > 0 Then
                Return grille.SelectedCells(0).OwningRow
            End If
            If grille.CurrentRow IsNot Nothing Then
                Return grille.CurrentRow
            End If
            Return Nothing
        End Function

        Private Sub MemoriserLigneSelectionnee(sender As Object, e As EventArgs)
            Dim ligne As DataGridViewRow = Nothing
            If gridLignes.SelectedRows.Count > 0 Then
                ligne = gridLignes.SelectedRows(0)
            ElseIf gridLignes.SelectedCells.Count > 0 Then
                ligne = gridLignes.SelectedCells(0).OwningRow
            ElseIf gridLignes.CurrentRow IsNot Nothing Then
                ligne = gridLignes.CurrentRow
            End If

            If ligne Is Nothing OrElse Not gridLignes.Columns.Contains("BonLigneId") OrElse ligne.Cells("BonLigneId").Value Is Nothing Then
                _bonLigneCouranteId = 0
                Return
            End If

            _bonLigneCouranteId = Convert.ToInt32(ligne.Cells("BonLigneId").Value)
        End Sub

        Private Function LireFournisseurIdSelectionne() As Integer?
            If cmbFournisseur.SelectedIndex < 0 OrElse cmbFournisseur.SelectedValue Is Nothing OrElse TypeOf cmbFournisseur.SelectedValue Is DataRowView Then
                Return Nothing
            End If
            Return Convert.ToInt32(cmbFournisseur.SelectedValue)
        End Function

        Private Sub EnregistrerEnteteCourante(sender As Object, e As EventArgs)
            If _chargement Then
                Return
            End If
            If _bonCourantId <= 0 Then
                Return
            End If
            _repo.MettreAJourEntete(_bonCourantId, LireFournisseurIdSelectionne(), cmbTypePaiement.Text)
        End Sub

        Private Sub ColorerStatuts()
            For Each row As DataGridViewRow In gridBons.Rows
                If row.Cells("Statut").Value Is Nothing Then
                    Continue For
                End If
                Dim statut As String = Convert.ToString(row.Cells("Statut").Value)
                If statut = "EnAttente" Then
                    row.DefaultCellStyle.BackColor = Color.MistyRose
                ElseIf statut = "Valide" Then
                    row.DefaultCellStyle.BackColor = Color.LemonChiffon
                ElseIf statut = "Livre" Then
                    row.DefaultCellStyle.BackColor = Color.Honeydew
                End If
            Next
        End Sub

        Private Function ObtenirStatutCourant() As String
            Dim texte As String = lblStatutBon.Text.Replace("Statut:", "").Trim()
            If texte = "" Then
                Return "EnAttente"
            End If
            Return texte
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
                Case "EnAttente"
                    lblStatutBon.ForeColor = Color.FromArgb(220, 70, 70)
                Case "Valide"
                    lblStatutBon.ForeColor = Color.FromArgb(227, 155, 49)
                Case Else
                    lblStatutBon.ForeColor = Color.FromArgb(42, 168, 94)
            End Select
        End Sub

        Private Sub AjouterNotification(message As String)
            Dim service As New NotificationService(_dal)
            service.DeclencherEvenementMetier("Info", message, "approvisionnement:" & message, "APPROVISIONNEMENT", "", False, 5)
        End Sub

        Private Sub ConfigurerChart(chart As Chart, chartType As SeriesChartType, nomSerie As String)
            chart.ChartAreas.Clear()
            chart.Series.Clear()
            chart.ChartAreas.Add(New ChartArea("Zone"))
            chart.Series.Add(New Series(nomSerie) With {.ChartType = chartType})
        End Sub

        Private Sub AlimenterChart(chart As Chart, dt As DataTable, colonneX As String, colonneY As String)
            If chart.Series.Count = 0 Then
                Return
            End If
            chart.Series(0).Points.Clear()
            For Each row As DataRow In dt.Rows
                chart.Series(0).Points.AddXY(Convert.ToString(row(colonneX)), Convert.ToDecimal(row(colonneY)))
            Next
        End Sub
    End Class
End Namespace
