Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.IO
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FrmInventaireIntelligent
        Inherits Form

        Private ReadOnly ColorBg As Color = Color.FromArgb(244, 247, 252)
        Private ReadOnly ColorCard As Color = Color.White
        Private ReadOnly ColorPrimary As Color = Color.FromArgb(44, 62, 80)
        Private ReadOnly ColorSecondary As Color = Color.FromArgb(88, 101, 121)
        Private ReadOnly ColorAccent As Color = Color.FromArgb(59, 130, 246)
        Private ReadOnly ColorSuccess As Color = Color.FromArgb(34, 197, 94)
        Private ReadOnly ColorWarning As Color = Color.FromArgb(249, 115, 22)
        Private ReadOnly ColorDanger As Color = Color.FromArgb(239, 68, 68)
        Private ReadOnly ColorInfo As Color = Color.FromArgb(14, 165, 233)
        Private ReadOnly ColorPurple As Color = Color.FromArgb(139, 92, 246)
        Private ReadOnly ColorMuted As Color = Color.FromArgb(229, 231, 235)
        Private ReadOnly ColorBorder As Color = Color.FromArgb(226, 232, 240)

        Private ReadOnly FontTitle As New Font("Segoe UI", 18, FontStyle.Bold)
        Private ReadOnly FontSection As New Font("Segoe UI", 11, FontStyle.Bold)
        Private ReadOnly FontLabel As New Font("Segoe UI", 9.5F)
        Private ReadOnly FontButton As New Font("Segoe UI", 9.5F, FontStyle.Bold)
        Private ReadOnly FontValue As New Font("Segoe UI", 12, FontStyle.Bold)

        Private ReadOnly _repo As InventaireIntelligentRepository
        Private ReadOnly _stockService As StockService
        Private ReadOnly _printDoc As New PrintDocument()
        Private ReadOnly _printPreview As New PrintPreviewDialog()

        Private _inventaireId As Integer
        Private _referenceInventaire As String = ""
        Private _inventaireStatut As String = ""
        Private _inventaireTable As DataTable
        Private _inventaireView As DataView
        Private _produitsConsultation As DataTable
        Private _chargementEnCours As Boolean

        Private tabMain As TabControl
        Private tabInventaire As TabPage
        Private tabConsultation As TabPage
        Private tabHistoriqueInventaires As TabPage

        Private btnNouvelInventaire As Button
        Private btnEnregistrerInventaire As Button
        Private btnValiderEtAjuster As Button
        Private btnImprimer As Button
        Private btnExporterPdf As Button
        Private btnReprendreInventaire As Button

        Private txtRecherche As TextBox
        Private cmbCategorie As ComboBox
        Private cmbStatut As ComboBox
        Private lblInventaireRef As Label
        Private lblInventaireStatut As Label

        Private gridInventaire As DataGridView

        Private lblTotalProduits As Label
        Private lblProduitsComptes As Label
        Private lblProduitsNonComptes As Label
        Private lblProduitsConformes As Label
        Private lblProduitsManques As Label
        Private lblProduitsSurplus As Label
        Private lblValeurEcarts As Label
        Private lblProgression As Label

        Private cmbProduitConsultation As ComboBox
        Private btnChargerConsultation As Button
        Private lblAnalyseSortieGros As Label
        Private lblAnalyseSortiePiece As Label
        Private lblAnalyseRestantGros As Label
        Private lblAnalyseRestantPiece As Label
        Private lblAnalyseRealisation As Label
        Private gridEntrees As DataGridView
        Private gridSorties As DataGridView
        Private gridAncienInventaire As DataGridView

        Private cmbMoisHistorique As ComboBox
        Private cmbAnneeHistorique As ComboBox
        Private btnChargerHistorique As Button
        Private btnImprimerHistorique As Button
        Private gridInventairesHistoriques As DataGridView
        Private gridHistoriqueLignes As DataGridView
        Private lblHistoriqueRef As Label
        Private lblHistoriqueStatut As Label
        Private lblHistoriqueDate As Label
        Private lblHistTotalProduits As Label
        Private lblHistProduitsComptes As Label
        Private lblHistProduitsNonComptes As Label
        Private lblHistProduitsConformes As Label
        Private lblHistProduitsManques As Label
        Private lblHistProduitsSurplus As Label
        Private lblHistValeurEcarts As Label
        Private lblHistProgression As Label

        Private _historiqueInventairesTable As DataTable
        Private _historiqueInventaireIdSelectionne As Integer
        Private _chargementHistoriqueEnCours As Boolean
        Private _impressionInventaireTable As DataTable
        Private _impressionInventaireTitre As String = "RAPPORT D'INVENTAIRE"
        Private _impressionInventaireReference As String = "-"
        Private _impressionInventaireStatut As String = "-"
        Private _impressionInventaireDate As String = "-"
        Private _impressionInventaireObservation As String = ""
        Private _impressionIndexLigne As Integer

        Public Sub New()
            Me.Text = "FrmInventaireIntelligent"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.WindowState = FormWindowState.Maximized
            Me.BackColor = ColorBg
            Me.Font = FontLabel
            Me.DoubleBuffered = True

            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            _repo = New InventaireIntelligentRepository(dal)
            _stockService = New StockService(dal)

            ConstruireInterface()
            AddHandler btnNouvelInventaire.Click, AddressOf NouvelInventaire
            AddHandler btnEnregistrerInventaire.Click, AddressOf EnregistrerInventaire
            AddHandler btnValiderEtAjuster.Click, AddressOf ValiderEtAjuster
            AddHandler btnImprimer.Click, AddressOf ImprimerInventaire
            AddHandler btnExporterPdf.Click, AddressOf ExporterPdfInventaire
            AddHandler btnReprendreInventaire.Click, AddressOf ReprendreInventaireEnCours
            AddHandler txtRecherche.TextChanged, AddressOf TexteRechercheOuFiltreChanged
            AddHandler cmbCategorie.SelectedIndexChanged, AddressOf TexteRechercheOuFiltreChanged
            AddHandler cmbStatut.SelectedIndexChanged, AddressOf TexteRechercheOuFiltreChanged
            AddHandler btnChargerConsultation.Click, AddressOf ChargerConsultationProduit
            AddHandler btnChargerHistorique.Click, AddressOf ChargerInventairesHistoriquesDepuisFiltres
            AddHandler btnImprimerHistorique.Click, AddressOf ImprimerInventaireHistorique
            AddHandler gridInventairesHistoriques.SelectionChanged, AddressOf GridInventairesHistoriques_SelectionChanged
            AddHandler gridInventaire.CellEndEdit, AddressOf GridInventaire_CellEndEdit
            AddHandler gridInventaire.CellValidating, AddressOf GridInventaire_CellValidating
            AddHandler gridInventaire.DataError, AddressOf GridInventaire_DataError
            AddHandler gridInventaire.RowPrePaint, AddressOf GridInventaire_RowPrePaint
            AddHandler Me.Load, AddressOf FrmInventaireIntelligent_Load
            AddHandler _printDoc.PrintPage, AddressOf ImprimerPageInventaire
        End Sub

        Private Sub ConstruireInterface()
            tabMain = New TabControl() With {.Dock = DockStyle.Fill, .Font = FontLabel}
            tabInventaire = New TabPage("Inventaire") With {.BackColor = ColorBg}
            tabConsultation = New TabPage("Consultation") With {.BackColor = ColorBg}
            tabHistoriqueInventaires = New TabPage("Historique inventaires") With {.BackColor = ColorBg}
            tabMain.TabPages.Add(tabInventaire)
            tabMain.TabPages.Add(tabConsultation)
            tabMain.TabPages.Add(tabHistoriqueInventaires)
            Me.Controls.Add(tabMain)

            ConstruireTabInventaire()
            ConstruireTabConsultation()
            ConstruireTabHistoriqueInventaires()
        End Sub

        Private Sub ConstruireTabInventaire()
            Dim layout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 4, .Padding = New Padding(20)}
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 70))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 56))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 170))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            tabInventaire.Controls.Add(layout)

            Dim pnlActions As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .AutoScroll = True, .WrapContents = False, .Padding = New Padding(0), .Margin = New Padding(0)}
            btnNouvelInventaire = CreerBoutonAction("Nouvel inventaire", ColorSuccess)
            btnEnregistrerInventaire = CreerBoutonAction("Enregistrer inventaire", ColorAccent)
            btnValiderEtAjuster = CreerBoutonAction("Valider et ajuster", ColorWarning)
            btnImprimer = CreerBoutonAction("Imprimer", ColorSecondary)
            btnExporterPdf = CreerBoutonAction("Exporter PDF", ColorPurple)
            btnReprendreInventaire = CreerBoutonAction("Reprendre inventaire en cours", ColorInfo)
            pnlActions.Controls.AddRange(New Control() {btnNouvelInventaire, btnEnregistrerInventaire, btnValiderEtAjuster, btnImprimer, btnExporterPdf, btnReprendreInventaire})
            layout.Controls.Add(pnlActions, 0, 0)

            Dim pnlFiltres As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .AutoSize = False, .WrapContents = False, .Padding = New Padding(0), .Margin = New Padding(0)}
            pnlFiltres.Controls.Add(New Label() With {.Text = "Recherche:", .AutoSize = True, .Margin = New Padding(0, 10, 8, 0), .Font = FontLabel, .ForeColor = ColorSecondary})
            txtRecherche = New TextBox() With {.Width = 220, .Font = FontLabel, .Margin = New Padding(0, 6, 15, 0)}
            pnlFiltres.Controls.Add(txtRecherche)
            pnlFiltres.Controls.Add(New Label() With {.Text = "Catégorie:", .AutoSize = True, .Margin = New Padding(0, 10, 8, 0), .Font = FontLabel, .ForeColor = ColorSecondary})
            cmbCategorie = New ComboBox() With {.Width = 220, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontLabel, .Margin = New Padding(0, 6, 15, 0)}
            pnlFiltres.Controls.Add(cmbCategorie)
            pnlFiltres.Controls.Add(New Label() With {.Text = "Statut:", .AutoSize = True, .Margin = New Padding(0, 10, 8, 0), .Font = FontLabel, .ForeColor = ColorSecondary})
            cmbStatut = New ComboBox() With {.Width = 180, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontLabel, .Margin = New Padding(0, 6, 15, 0)}
            cmbStatut.Items.AddRange(New Object() {"Tous", "Non comptés", "Comptés", "Conformes", "Manques", "Surplus"})
            cmbStatut.SelectedIndex = 0
            pnlFiltres.Controls.Add(cmbStatut)
            lblInventaireRef = New Label() With {.AutoSize = False, .Width = 280, .Height = 24, .Font = FontSection, .ForeColor = ColorPrimary, .Margin = New Padding(25, 10, 0, 0), .TextAlign = ContentAlignment.MiddleLeft, .Text = "Référence: -"}
            lblInventaireStatut = New Label() With {.AutoSize = False, .Width = 360, .Height = 24, .Font = FontSection, .ForeColor = ColorSecondary, .Margin = New Padding(15, 10, 0, 0), .TextAlign = ContentAlignment.MiddleLeft, .Text = "Statut: -"}
            pnlFiltres.Controls.Add(lblInventaireRef)
            pnlFiltres.Controls.Add(lblInventaireStatut)
            layout.Controls.Add(pnlFiltres, 0, 1)

            Dim tableKpi As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 4, .RowCount = 2, .Margin = New Padding(0), .Padding = New Padding(0)}
            For i As Integer = 1 To 4
                tableKpi.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
            Next
            tableKpi.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
            tableKpi.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
            Dim p1 As Label = New Label()
            Dim p2 As Label = New Label()
            Dim p3 As Label = New Label()
            Dim p4 As Label = New Label()
            Dim p5 As Label = New Label()
            Dim p6 As Label = New Label()
            Dim p7 As Label = New Label()
            Dim p8 As Label = New Label()
            tableKpi.Controls.Add(CreerCarteResume("Total produits", p1), 0, 0)
            tableKpi.Controls.Add(CreerCarteResume("Produits comptés", p2), 1, 0)
            tableKpi.Controls.Add(CreerCarteResume("Produits non comptés", p3), 2, 0)
            tableKpi.Controls.Add(CreerCarteResume("Produits conformes", p4), 3, 0)
            tableKpi.Controls.Add(CreerCarteResume("Produits avec manque", p5), 0, 1)
            tableKpi.Controls.Add(CreerCarteResume("Produits avec surplus", p6), 1, 1)
            tableKpi.Controls.Add(CreerCarteResume("Valeur totale des écarts", p7), 2, 1)
            tableKpi.Controls.Add(CreerCarteResume("Progression", p8), 3, 1)
            lblTotalProduits = p1
            lblProduitsComptes = p2
            lblProduitsNonComptes = p3
            lblProduitsConformes = p4
            lblProduitsManques = p5
            lblProduitsSurplus = p6
            lblValeurEcarts = p7
            lblProgression = p8
            layout.Controls.Add(tableKpi, 0, 2)

            gridInventaire = CreerGrille(True)
            layout.Controls.Add(gridInventaire, 0, 3)
        End Sub

        Private Sub ConstruireTabConsultation()
            Dim tabDetails As New TabControl() With {.Dock = DockStyle.Fill, .Font = FontLabel}
            Dim tabAnalyse As New TabPage("Analyse produit") With {.BackColor = ColorBg}
            Dim tabHistorique As New TabPage("Historique") With {.BackColor = ColorBg}
            tabDetails.TabPages.Add(tabAnalyse)
            tabDetails.TabPages.Add(tabHistorique)
            tabConsultation.Controls.Add(tabDetails)

            Dim layoutAnalyse As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 3, .Padding = New Padding(20)}
            layoutAnalyse.RowStyles.Add(New RowStyle(SizeType.Absolute, 60))
            layoutAnalyse.RowStyles.Add(New RowStyle(SizeType.Absolute, 170))
            layoutAnalyse.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            tabAnalyse.Controls.Add(layoutAnalyse)

            Dim pnlChoix As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .WrapContents = False, .AutoScroll = True}
            pnlChoix.Controls.Add(New Label() With {.Text = "Produit:", .AutoSize = True, .Margin = New Padding(0, 10, 8, 0), .Font = FontLabel, .ForeColor = ColorSecondary})
            cmbProduitConsultation = New ComboBox() With {.Width = 380, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontLabel, .Margin = New Padding(0, 6, 15, 0)}
            btnChargerConsultation = CreerBoutonAction("Charger", ColorAccent)
            btnChargerConsultation.Width = 120
            pnlChoix.Controls.Add(cmbProduitConsultation)
            pnlChoix.Controls.Add(btnChargerConsultation)
            layoutAnalyse.Controls.Add(pnlChoix, 0, 0)

            Dim cardAnalyse As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Padding = New Padding(20), .Margin = New Padding(0, 0, 0, 10)}
            cardAnalyse.Controls.Add(New Label() With {.Text = "Analyse produit", .Font = FontTitle, .ForeColor = ColorPrimary, .AutoSize = True, .Location = New Point(10, 10)})
            lblAnalyseSortieGros = New Label() With {.Text = "Entrées: 0 | Ventes: 0", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorSecondary, .Location = New Point(12, 60)}
            lblAnalyseSortiePiece = New Label() With {.Text = "Sorties manuelles: 0 | Pertes: 0", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorSecondary, .Location = New Point(12, 95)}
            lblAnalyseRestantGros = New Label() With {.Text = "Dons: 0 | Allocations: 0", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorSecondary, .Location = New Point(12, 130)}
            lblAnalyseRestantPiece = New Label() With {.Text = "Dettes client: 0 | Dettes boss: 0 | Hors caisse: 0", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorSecondary, .Location = New Point(12, 165)}
            lblAnalyseRealisation = New Label() With {.Text = "G:0 D:0 Q:0 P:0 Dz:0 | Stock réel: 0 P | Stock: 0C+0P | Mnt: 0 FC", .AutoSize = True, .Font = FontValue, .ForeColor = ColorAccent, .Location = New Point(12, 200)}
            cardAnalyse.Controls.AddRange(New Control() {lblAnalyseSortieGros, lblAnalyseSortiePiece, lblAnalyseRestantGros, lblAnalyseRestantPiece, lblAnalyseRealisation})
            layoutAnalyse.Controls.Add(cardAnalyse, 0, 1)

            Dim lblInfo As New Label() With {.Text = "La consultation reprend les données historiques entrées, sorties et stockInventaire.", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorSecondary, .Dock = DockStyle.Fill}
            layoutAnalyse.Controls.Add(lblInfo, 0, 2)

            Dim layoutHistorique As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2, .Padding = New Padding(20)}
            layoutHistorique.RowStyles.Add(New RowStyle(SizeType.Percent, 60))
            layoutHistorique.RowStyles.Add(New RowStyle(SizeType.Percent, 40))
            tabHistorique.Controls.Add(layoutHistorique)

            Dim grilleComparatif As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1}
            grilleComparatif.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 38))
            grilleComparatif.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 62))
            Dim cardEntrees As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Padding = New Padding(10), .Margin = New Padding(0, 0, 8, 0)}
            cardEntrees.Controls.Add(New Label() With {.Text = "Historique des entrées", .Font = FontSection, .AutoSize = True, .ForeColor = ColorPrimary, .Dock = DockStyle.Top})
            gridEntrees = CreerGrille(False)
            gridEntrees.Dock = DockStyle.Fill
            cardEntrees.Controls.Add(gridEntrees)
            grilleComparatif.Controls.Add(cardEntrees, 0, 0)

            Dim cardSorties As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Padding = New Padding(10), .Margin = New Padding(8, 0, 0, 0)}
            cardSorties.Controls.Add(New Label() With {.Text = "Historique des sorties", .Font = FontSection, .AutoSize = True, .ForeColor = ColorPrimary, .Dock = DockStyle.Top})
            gridSorties = CreerGrille(False)
            gridSorties.Dock = DockStyle.Fill
            cardSorties.Controls.Add(gridSorties)
            grilleComparatif.Controls.Add(cardSorties, 1, 0)

            layoutHistorique.Controls.Add(grilleComparatif, 0, 0)

            Dim cardAncien As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Padding = New Padding(10)}
            cardAncien.Controls.Add(New Label() With {.Text = "Historique ancien StockInventaire", .Font = FontSection, .AutoSize = True, .ForeColor = ColorPrimary, .Dock = DockStyle.Top})
            gridAncienInventaire = CreerGrille(False)
            gridAncienInventaire.Dock = DockStyle.Fill
            cardAncien.Controls.Add(gridAncienInventaire)
            layoutHistorique.Controls.Add(cardAncien, 0, 1)
        End Sub

        Private Sub ConstruireTabHistoriqueInventaires()
            Dim layout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 4, .Padding = New Padding(20)}
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 58))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 92))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 190))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            tabHistoriqueInventaires.Controls.Add(layout)

            Dim pnlFiltres As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .WrapContents = False, .AutoScroll = True}
            pnlFiltres.Controls.Add(New Label() With {.Text = "Mois:", .AutoSize = True, .Margin = New Padding(0, 10, 8, 0), .Font = FontLabel, .ForeColor = ColorSecondary})
            cmbMoisHistorique = New ComboBox() With {.Width = 180, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontLabel, .Margin = New Padding(0, 6, 15, 0)}
            pnlFiltres.Controls.Add(cmbMoisHistorique)
            pnlFiltres.Controls.Add(New Label() With {.Text = "Année:", .AutoSize = True, .Margin = New Padding(0, 10, 8, 0), .Font = FontLabel, .ForeColor = ColorSecondary})
            cmbAnneeHistorique = New ComboBox() With {.Width = 120, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontLabel, .Margin = New Padding(0, 6, 15, 0)}
            pnlFiltres.Controls.Add(cmbAnneeHistorique)
            btnChargerHistorique = CreerBoutonAction("Charger", ColorAccent)
            btnChargerHistorique.Width = 120
            btnImprimerHistorique = CreerBoutonAction("Imprimer A4", ColorSecondary)
            btnImprimerHistorique.Width = 140
            pnlFiltres.Controls.Add(btnChargerHistorique)
            pnlFiltres.Controls.Add(btnImprimerHistorique)
            layout.Controls.Add(pnlFiltres, 0, 0)

            Dim cardInfos As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Padding = New Padding(16), .Margin = New Padding(0, 0, 0, 8)}
            Dim lblTitre As New Label() With {.Text = "Historique des inventaires", .Font = FontSection, .ForeColor = ColorPrimary, .AutoSize = True, .Location = New Point(10, 10)}
            lblHistoriqueRef = New Label() With {.Text = "Référence: -", .Font = FontLabel, .ForeColor = ColorSecondary, .AutoSize = False, .Width = 300, .Height = 22, .Location = New Point(10, 40)}
            lblHistoriqueStatut = New Label() With {.Text = "Statut: -", .Font = FontLabel, .ForeColor = ColorSecondary, .AutoSize = False, .Width = 250, .Height = 22, .Location = New Point(340, 40)}
            lblHistoriqueDate = New Label() With {.Text = "Date: -", .Font = FontLabel, .ForeColor = ColorSecondary, .AutoSize = False, .Width = 250, .Height = 22, .Location = New Point(620, 40)}
            cardInfos.Controls.Add(lblTitre)
            cardInfos.Controls.Add(lblHistoriqueRef)
            cardInfos.Controls.Add(lblHistoriqueStatut)
            cardInfos.Controls.Add(lblHistoriqueDate)
            layout.Controls.Add(cardInfos, 0, 1)

            Dim kpiTable As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 4, .RowCount = 2, .Margin = New Padding(0), .Padding = New Padding(0)}
            For i As Integer = 1 To 4
                kpiTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
            Next
            kpiTable.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
            kpiTable.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
            Dim h1, h2, h3, h4, h5, h6, h7, h8 As Label
            kpiTable.Controls.Add(CreerCarteResume("Total produits", h1), 0, 0)
            kpiTable.Controls.Add(CreerCarteResume("Produits comptés", h2), 1, 0)
            kpiTable.Controls.Add(CreerCarteResume("Produits non comptés", h3), 2, 0)
            kpiTable.Controls.Add(CreerCarteResume("Produits conformes", h4), 3, 0)
            kpiTable.Controls.Add(CreerCarteResume("Produits avec manque", h5), 0, 1)
            kpiTable.Controls.Add(CreerCarteResume("Produits avec surplus", h6), 1, 1)
            kpiTable.Controls.Add(CreerCarteResume("Valeur des écarts", h7), 2, 1)
            kpiTable.Controls.Add(CreerCarteResume("Progression", h8), 3, 1)
            lblHistTotalProduits = h1
            lblHistProduitsComptes = h2
            lblHistProduitsNonComptes = h3
            lblHistProduitsConformes = h4
            lblHistProduitsManques = h5
            lblHistProduitsSurplus = h6
            lblHistValeurEcarts = h7
            lblHistProgression = h8
            layout.Controls.Add(kpiTable, 0, 2)

            Dim layoutGrilles As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2}
            layoutGrilles.RowStyles.Add(New RowStyle(SizeType.Absolute, 220))
            layoutGrilles.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim cardInventaires As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Padding = New Padding(10), .Margin = New Padding(0, 0, 0, 8)}
            cardInventaires.Controls.Add(New Label() With {.Text = "Inventaires de la période", .Font = FontSection, .ForeColor = ColorPrimary, .AutoSize = True, .Dock = DockStyle.Top})
            gridInventairesHistoriques = CreerGrille(False)
            gridInventairesHistoriques.Dock = DockStyle.Fill
            cardInventaires.Controls.Add(gridInventairesHistoriques)
            layoutGrilles.Controls.Add(cardInventaires, 0, 0)

            Dim cardLignes As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Padding = New Padding(10), .Margin = New Padding(0)}
            cardLignes.Controls.Add(New Label() With {.Text = "Détail inventaire sélectionné", .Font = FontSection, .ForeColor = ColorPrimary, .AutoSize = True, .Dock = DockStyle.Top})
            gridHistoriqueLignes = CreerGrille(False)
            gridHistoriqueLignes.Dock = DockStyle.Fill
            cardLignes.Controls.Add(gridHistoriqueLignes)
            layoutGrilles.Controls.Add(cardLignes, 0, 1)

            layout.Controls.Add(layoutGrilles, 0, 3)
        End Sub

        Private Sub ChargerFiltresHistoriqueInventaires()
            If cmbMoisHistorique Is Nothing OrElse cmbAnneeHistorique Is Nothing Then Return

            Dim mois As String() = {"Janvier", "Février", "Mars", "Avril", "Mai", "Juin", "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"}
            cmbMoisHistorique.DataSource = Nothing
            cmbMoisHistorique.Items.Clear()
            cmbMoisHistorique.Items.AddRange(mois)

            cmbAnneeHistorique.DataSource = Nothing
            cmbAnneeHistorique.Items.Clear()

            Dim dtAnnees As DataTable = _repo.ListerAnneesInventaires()
            If dtAnnees IsNot Nothing AndAlso dtAnnees.Rows.Count > 0 Then
                For Each r As DataRow In dtAnnees.Rows
                    cmbAnneeHistorique.Items.Add(Convert.ToInt32(r("Annee")))
                Next
            End If

            If cmbAnneeHistorique.Items.Count = 0 Then
                For annee As Integer = Date.Now.Year To Date.Now.Year - 5 Step -1
                    cmbAnneeHistorique.Items.Add(annee)
                Next
            End If

            cmbMoisHistorique.SelectedIndex = Date.Now.Month - 1
            Dim anneeCourante As Integer = Date.Now.Year
            If cmbAnneeHistorique.Items.Contains(anneeCourante) Then
                cmbAnneeHistorique.SelectedItem = anneeCourante
            ElseIf cmbAnneeHistorique.Items.Count > 0 Then
                cmbAnneeHistorique.SelectedIndex = 0
            End If
        End Sub

        Private Sub ChargerInventairesHistoriques()
            If cmbMoisHistorique Is Nothing OrElse cmbAnneeHistorique Is Nothing Then Return
            If cmbMoisHistorique.SelectedIndex < 0 OrElse cmbAnneeHistorique.SelectedItem Is Nothing Then Return

            _chargementHistoriqueEnCours = True
            Try
                Dim mois As Integer = cmbMoisHistorique.SelectedIndex + 1
                Dim annee As Integer = Convert.ToInt32(cmbAnneeHistorique.SelectedItem)
                Dim dt As DataTable = _repo.ListerInventairesParPeriode(mois, annee)
                _historiqueInventairesTable = dt
                If gridInventairesHistoriques IsNot Nothing Then
                    gridInventairesHistoriques.DataSource = If(dt Is Nothing, Nothing, dt.DefaultView)
                    ConfigurerGrilleInventairesHistoriques()
                End If
                If dt IsNot Nothing AndAlso dt.Rows.Count > 0 Then
                    ChargerInventaireHistoriqueSelectionne(0)
                Else
                    EffacerHistoriqueSelectionne()
                End If
            Finally
                _chargementHistoriqueEnCours = False
            End Try
        End Sub

        Private Sub ConfigurerGrilleInventairesHistoriques()
            If gridInventairesHistoriques Is Nothing OrElse gridInventairesHistoriques.Columns.Count = 0 Then Return

            For Each col As DataGridViewColumn In gridInventairesHistoriques.Columns
                col.Visible = True
                col.ReadOnly = True
            Next

            If gridInventairesHistoriques.Columns.Contains("InventaireId") Then gridInventairesHistoriques.Columns("InventaireId").Visible = False
            If gridInventairesHistoriques.Columns.Contains("Observation") Then gridInventairesHistoriques.Columns("Observation").Visible = False
            If gridInventairesHistoriques.Columns.Contains("ReferenceInventaire") Then
                gridInventairesHistoriques.Columns("ReferenceInventaire").HeaderText = "Référence"
                gridInventairesHistoriques.Columns("ReferenceInventaire").Width = 140
            End If
            If gridInventairesHistoriques.Columns.Contains("DateCreation") Then
                gridInventairesHistoriques.Columns("DateCreation").HeaderText = "Date"
                gridInventairesHistoriques.Columns("DateCreation").DefaultCellStyle.Format = "dd/MM/yyyy HH:mm"
                gridInventairesHistoriques.Columns("DateCreation").Width = 150
            End If
            If gridInventairesHistoriques.Columns.Contains("DateValidation") Then
                gridInventairesHistoriques.Columns("DateValidation").HeaderText = "Validation"
                gridInventairesHistoriques.Columns("DateValidation").DefaultCellStyle.Format = "dd/MM/yyyy HH:mm"
                gridInventairesHistoriques.Columns("DateValidation").Width = 150
            End If
            If gridInventairesHistoriques.Columns.Contains("Statut") Then
                gridInventairesHistoriques.Columns("Statut").HeaderText = "Statut"
                gridInventairesHistoriques.Columns("Statut").Width = 110
            End If
            If gridInventairesHistoriques.Columns.Contains("TotalLignes") Then
                gridInventairesHistoriques.Columns("TotalLignes").HeaderText = "Lignes"
                gridInventairesHistoriques.Columns("TotalLignes").DefaultCellStyle.Format = "N0"
                gridInventairesHistoriques.Columns("TotalLignes").Width = 90
            End If
            If gridInventairesHistoriques.Columns.Contains("NombreComptes") Then
                gridInventairesHistoriques.Columns("NombreComptes").HeaderText = "Comptés"
                gridInventairesHistoriques.Columns("NombreComptes").DefaultCellStyle.Format = "N0"
                gridInventairesHistoriques.Columns("NombreComptes").Width = 90
            End If
            If gridInventairesHistoriques.Columns.Contains("NombreNonComptes") Then
                gridInventairesHistoriques.Columns("NombreNonComptes").HeaderText = "Non comptés"
                gridInventairesHistoriques.Columns("NombreNonComptes").DefaultCellStyle.Format = "N0"
                gridInventairesHistoriques.Columns("NombreNonComptes").Width = 110
            End If
            If gridInventairesHistoriques.Columns.Contains("NombreConformes") Then
                gridInventairesHistoriques.Columns("NombreConformes").HeaderText = "Conformes"
                gridInventairesHistoriques.Columns("NombreConformes").DefaultCellStyle.Format = "N0"
                gridInventairesHistoriques.Columns("NombreConformes").Width = 100
            End If
            If gridInventairesHistoriques.Columns.Contains("NombreManques") Then
                gridInventairesHistoriques.Columns("NombreManques").HeaderText = "Manques"
                gridInventairesHistoriques.Columns("NombreManques").DefaultCellStyle.Format = "N0"
                gridInventairesHistoriques.Columns("NombreManques").Width = 90
            End If
            If gridInventairesHistoriques.Columns.Contains("NombreSurplus") Then
                gridInventairesHistoriques.Columns("NombreSurplus").HeaderText = "Surplus"
                gridInventairesHistoriques.Columns("NombreSurplus").DefaultCellStyle.Format = "N0"
                gridInventairesHistoriques.Columns("NombreSurplus").Width = 90
            End If
            If gridInventairesHistoriques.Columns.Contains("ValeurEcarts") Then
                gridInventairesHistoriques.Columns("ValeurEcarts").HeaderText = "Valeur écarts"
                gridInventairesHistoriques.Columns("ValeurEcarts").DefaultCellStyle.Format = "N0"
                gridInventairesHistoriques.Columns("ValeurEcarts").Width = 120
            End If

            If gridInventairesHistoriques.CurrentRow Is Nothing AndAlso gridInventairesHistoriques.Rows.Count > 0 Then
                gridInventairesHistoriques.Rows(0).Selected = True
            End If
        End Sub

        Private Sub ChargerInventaireHistoriqueSelectionne(index As Integer)
            If gridInventairesHistoriques Is Nothing OrElse gridInventairesHistoriques.Rows.Count = 0 Then
                EffacerHistoriqueSelectionne()
                Return
            End If
            If index < 0 OrElse index >= gridInventairesHistoriques.Rows.Count Then
                index = 0
            End If

            _chargementHistoriqueEnCours = True
            Try
                gridInventairesHistoriques.ClearSelection()
                gridInventairesHistoriques.Rows(index).Selected = True
                Dim celluleVisible As DataGridViewCell = ObtenirPremiereCelluleVisible(gridInventairesHistoriques.Rows(index))
                If celluleVisible IsNot Nothing Then
                    gridInventairesHistoriques.CurrentCell = celluleVisible
                End If

                Dim rowView As DataRowView = TryCast(gridInventairesHistoriques.Rows(index).DataBoundItem, DataRowView)
                If rowView Is Nothing Then
                    EffacerHistoriqueSelectionne()
                    Return
                End If

                Dim row As DataRow = rowView.Row
                _historiqueInventaireIdSelectionne = Convert.ToInt32(row("InventaireId"))
                _impressionInventaireTitre = "RAPPORT D'INVENTAIRE"
                _impressionInventaireReference = Convert.ToString(row("ReferenceInventaire"))
                _impressionInventaireStatut = Convert.ToString(row("Statut"))
                _impressionInventaireDate = Convert.ToDateTime(row("DateCreation")).ToString("dd/MM/yyyy HH:mm")
                _impressionInventaireObservation = If(row.IsNull("Observation"), "", Convert.ToString(row("Observation")))
                lblHistoriqueRef.Text = "Référence: " & Convert.ToString(row("ReferenceInventaire"))
                lblHistoriqueStatut.Text = "Statut: " & Convert.ToString(row("Statut"))
                lblHistoriqueDate.Text = "Date: " & Convert.ToDateTime(row("DateCreation")).ToString("dd/MM/yyyy HH:mm")
                ActualiserResumeInventaireHistorique(row)

                Dim dtLignes As DataTable = _repo.ChargerLignesInventaire(_historiqueInventaireIdSelectionne)
                _impressionInventaireTable = dtLignes
                If gridHistoriqueLignes IsNot Nothing Then
                    gridHistoriqueLignes.DataSource = If(dtLignes Is Nothing, Nothing, dtLignes.DefaultView)
                    ConfigurerGrilleHistoriqueLignes()
                End If
            Finally
                _chargementHistoriqueEnCours = False
            End Try
        End Sub

        Private Sub ConfigurerGrilleHistoriqueLignes()
            If gridHistoriqueLignes Is Nothing OrElse gridHistoriqueLignes.Columns.Count = 0 Then Return

            For Each col As DataGridViewColumn In gridHistoriqueLignes.Columns
                col.Visible = True
                col.ReadOnly = True
            Next

            Dim colonnesCachees As String() = {"LigneInventaireId", "InventaireId", "ProduitId", "ConversionUnite", "PrixAchat"}
            For Each nom As String In colonnesCachees
                If gridHistoriqueLignes.Columns.Contains(nom) Then
                    gridHistoriqueLignes.Columns(nom).Visible = False
                End If
            Next

            If gridHistoriqueLignes.Columns.Contains("NomProduit") Then
                gridHistoriqueLignes.Columns("NomProduit").HeaderText = "Produit"
                gridHistoriqueLignes.Columns("NomProduit").MinimumWidth = 180
            End If
            If gridHistoriqueLignes.Columns.Contains("Categorie") Then
                gridHistoriqueLignes.Columns("Categorie").HeaderText = "Catégorie"
                gridHistoriqueLignes.Columns("Categorie").Width = 140
            End If
            If gridHistoriqueLignes.Columns.Contains("StockTheorique") Then
                gridHistoriqueLignes.Columns("StockTheorique").HeaderText = "Stock théorique"
                gridHistoriqueLignes.Columns("StockTheorique").DefaultCellStyle.Format = "N0"
            End If
            If gridHistoriqueLignes.Columns.Contains("StockPhysique") Then
                gridHistoriqueLignes.Columns("StockPhysique").HeaderText = "Stock physique"
                gridHistoriqueLignes.Columns("StockPhysique").DefaultCellStyle.Format = "N0"
            End If
            If gridHistoriqueLignes.Columns.Contains("Ecart") Then
                gridHistoriqueLignes.Columns("Ecart").HeaderText = "Écart"
                gridHistoriqueLignes.Columns("Ecart").DefaultCellStyle.Format = "N0"
            End If
            If gridHistoriqueLignes.Columns.Contains("Statut") Then
                gridHistoriqueLignes.Columns("Statut").HeaderText = "Statut"
                gridHistoriqueLignes.Columns("Statut").Width = 110
            End If
            If gridHistoriqueLignes.Columns.Contains("Motif") Then
                gridHistoriqueLignes.Columns("Motif").HeaderText = "Motif"
                gridHistoriqueLignes.Columns("Motif").MinimumWidth = 200
            End If
            If gridHistoriqueLignes.Columns.Contains("StatutComptage") Then
                gridHistoriqueLignes.Columns("StatutComptage").HeaderText = "Comptage"
                gridHistoriqueLignes.Columns("StatutComptage").Width = 110
            End If
        End Sub

        Private Sub ActualiserResumeInventaireHistorique(row As DataRow)
            If row Is Nothing Then
                EffacerHistoriqueSelectionne()
                Return
            End If

            Dim total As Integer = If(row.IsNull("TotalLignes"), 0, Convert.ToInt32(row("TotalLignes")))
            Dim comptes As Integer = If(row.IsNull("NombreComptes"), 0, Convert.ToInt32(row("NombreComptes")))
            Dim nonComptes As Integer = If(row.IsNull("NombreNonComptes"), 0, Convert.ToInt32(row("NombreNonComptes")))
            Dim conformes As Integer = If(row.IsNull("NombreConformes"), 0, Convert.ToInt32(row("NombreConformes")))
            Dim manques As Integer = If(row.IsNull("NombreManques"), 0, Convert.ToInt32(row("NombreManques")))
            Dim surplus As Integer = If(row.IsNull("NombreSurplus"), 0, Convert.ToInt32(row("NombreSurplus")))
            Dim valeurEcarts As Decimal = If(row.IsNull("ValeurEcarts"), 0D, Convert.ToDecimal(row("ValeurEcarts")))

            lblHistTotalProduits.Text = FormatageGlobal.FormatNombre(total)
            lblHistProduitsComptes.Text = FormatageGlobal.FormatNombre(comptes)
            lblHistProduitsNonComptes.Text = FormatageGlobal.FormatNombre(nonComptes)
            lblHistProduitsConformes.Text = FormatageGlobal.FormatNombre(conformes)
            lblHistProduitsManques.Text = FormatageGlobal.FormatNombre(manques)
            lblHistProduitsSurplus.Text = FormatageGlobal.FormatNombre(surplus)
            lblHistValeurEcarts.Text = FormatageGlobal.FormatMontant(valeurEcarts)
            Dim progression As Decimal = If(total = 0, 0D, (comptes * 100D) / total)
            lblHistProgression.Text = FormatageGlobal.FormatPourcentage(Math.Round(progression, 0))
        End Sub

        Private Function ObtenirPremiereCelluleVisible(row As DataGridViewRow) As DataGridViewCell
            If row Is Nothing OrElse row.DataGridView Is Nothing Then Return Nothing
            For Each cell As DataGridViewCell In row.Cells
                If cell IsNot Nothing AndAlso cell.Visible Then
                    Return cell
                End If
            Next
            Return Nothing
        End Function

        Private Sub EffacerHistoriqueSelectionne()
            _historiqueInventaireIdSelectionne = 0
            _impressionInventaireTable = Nothing
            lblHistoriqueRef.Text = "Référence: -"
            lblHistoriqueStatut.Text = "Statut: -"
            lblHistoriqueDate.Text = "Date: -"
            lblHistTotalProduits.Text = "0"
            lblHistProduitsComptes.Text = "0"
            lblHistProduitsNonComptes.Text = "0"
            lblHistProduitsConformes.Text = "0"
            lblHistProduitsManques.Text = "0"
            lblHistProduitsSurplus.Text = "0"
            lblHistValeurEcarts.Text = "0 FC"
            lblHistProgression.Text = "0 %"
            If gridHistoriqueLignes IsNot Nothing Then
                gridHistoriqueLignes.DataSource = Nothing
            End If
        End Sub

        Private Function CreerCarteResume(titre As String, ByRef valeur As Label) As Panel
            Dim pnl As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Margin = New Padding(6), .Padding = New Padding(12), .BorderStyle = BorderStyle.FixedSingle}
            Dim lblTitre As New Label() With {.Text = titre, .Font = FontLabel, .ForeColor = ColorSecondary, .AutoSize = True, .Location = New Point(12, 10)}
            valeur = New Label() With {.Text = "0", .Font = FontValue, .ForeColor = ColorPrimary, .AutoSize = True, .Location = New Point(12, 38)}
            pnl.Controls.Add(lblTitre)
            pnl.Controls.Add(valeur)
            Return pnl
        End Function

        Private Function CreerBoutonAction(texte As String, couleur As Color) As Button
            Dim btn As New Button() With {
                .Text = texte,
                .Height = 38,
                .Width = 160,
                .BackColor = couleur,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontButton,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 10, 0)
            }
            btn.FlatAppearance.BorderSize = 0
            Return btn
        End Function

        Private Function CreerGrille(modeEdition As Boolean) As DataGridView
            Dim dgv As New DataGridView() With {
                .Dock = DockStyle.Fill,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None,
                .EnableHeadersVisualStyles = False,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .ReadOnly = Not modeEdition,
                .RowHeadersVisible = False,
                .MultiSelect = False,
                .Font = FontLabel
            }
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250)
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = ColorPrimary
            dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            dgv.ColumnHeadersHeight = 40
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254)
            dgv.DefaultCellStyle.SelectionForeColor = ColorPrimary
            dgv.RowTemplate.Height = 30
            dgv.GridColor = ColorBorder
            Return dgv
        End Function

        Private Sub FrmInventaireIntelligent_Load(sender As Object, e As EventArgs)
            Try
                ChargerProduitsConsultation()
                ChargerEtatInventaireCourant()
                ChargerFiltresHistoriqueInventaires()
                AjouterFiltresStatuts()
                MettreModeLectureSeule(True)
                ChargerInventairesHistoriques()
            Catch ex As Exception
                MessageBox.Show("Erreur initialisation inventaire: " & ex.Message)
            End Try
        End Sub

        Private Sub AjouterFiltresStatuts()
            If cmbStatut.Items.Count = 0 Then
                cmbStatut.Items.AddRange(New Object() {"Tous", "Non comptés", "Comptés", "Conformes", "Manques", "Surplus"})
                cmbStatut.SelectedIndex = 0
            End If
        End Sub

        Private Sub ChargerEtatInventaireCourant()
            Dim dt As DataTable = _repo.ObtenirInventaireEnCours()
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                _inventaireId = 0
                _referenceInventaire = ""
                _inventaireStatut = ""
                _inventaireTable = Nothing
                _inventaireView = Nothing
                gridInventaire.DataSource = Nothing
                lblInventaireRef.Text = "Référence: -"
                lblInventaireStatut.Text = "Statut: aucun inventaire en cours"
                ActualiserResumeInventaire()
                Return
            End If

            Dim row As DataRow = dt.Rows(0)
            _inventaireId = Convert.ToInt32(row("InventaireId"))
            _referenceInventaire = Convert.ToString(row("ReferenceInventaire"))
            _inventaireStatut = Convert.ToString(row("Statut"))
            _impressionInventaireTitre = "RAPPORT D'INVENTAIRE"
            _impressionInventaireReference = _referenceInventaire
            _impressionInventaireStatut = _inventaireStatut
            _impressionInventaireDate = If(row.IsNull("DateCreation"), Date.Now.ToString("dd/MM/yyyy HH:mm"), Convert.ToDateTime(row("DateCreation")).ToString("dd/MM/yyyy HH:mm"))
            _impressionInventaireObservation = If(row.IsNull("Observation"), "", Convert.ToString(row("Observation")))
            lblInventaireRef.Text = "Référence: " & _referenceInventaire
            lblInventaireStatut.Text = "Statut: " & _inventaireStatut

            ChargerInventaireDepuisBase(_inventaireId)
        End Sub

        Private Sub ChargerInventaireDepuisBase(inventaireId As Integer)
            _chargementEnCours = True
            Try
                _inventaireTable = _repo.ChargerLignesInventaire(inventaireId)
                If _inventaireTable Is Nothing Then
                    _inventaireTable = New DataTable()
                End If
                _inventaireView = _inventaireTable.DefaultView
                gridInventaire.DataSource = _inventaireView
                ConfigurerGrilleInventaire()
                ChargerFiltresCategories()
                AppliquerFiltresInventaire()
                ActualiserResumeInventaire()
            Finally
                _chargementEnCours = False
            End Try
        End Sub

        Private Sub ConfigurerGrilleInventaire()
            If gridInventaire.Columns.Count = 0 Then Return

            For Each col As DataGridViewColumn In gridInventaire.Columns
                col.Visible = True
                col.ReadOnly = True
            Next

            Dim colonnesCachees As String() = {"LigneInventaireId", "InventaireId", "ProduitId", "ConversionUnite", "PrixAchat"}
            For Each nom As String In colonnesCachees
                If gridInventaire.Columns.Contains(nom) Then
                    gridInventaire.Columns(nom).Visible = False
                End If
            Next

            If gridInventaire.Columns.Contains("CodeProduit") Then
                gridInventaire.Columns("CodeProduit").HeaderText = "Code"
                gridInventaire.Columns("CodeProduit").Width = 110
            End If
            If gridInventaire.Columns.Contains("NomProduit") Then
                gridInventaire.Columns("NomProduit").HeaderText = "Produit"
                gridInventaire.Columns("NomProduit").MinimumWidth = 180
            End If
            If gridInventaire.Columns.Contains("Categorie") Then
                gridInventaire.Columns("Categorie").HeaderText = "Catégorie"
                gridInventaire.Columns("Categorie").Width = 140
            End If
            If gridInventaire.Columns.Contains("StockTheorique") Then
                gridInventaire.Columns("StockTheorique").HeaderText = "Stock théorique"
                gridInventaire.Columns("StockTheorique").DefaultCellStyle.Format = "N0"
                gridInventaire.Columns("StockTheorique").Width = 120
            End If
            If gridInventaire.Columns.Contains("StockPhysique") Then
                gridInventaire.Columns("StockPhysique").HeaderText = "Stock physique"
                gridInventaire.Columns("StockPhysique").DefaultCellStyle.Format = "N0"
                gridInventaire.Columns("StockPhysique").ReadOnly = (_inventaireStatut <> "EN_COURS")
                gridInventaire.Columns("StockPhysique").Width = 120
            End If
            If gridInventaire.Columns.Contains("Ecart") Then
                gridInventaire.Columns("Ecart").HeaderText = "Écart"
                gridInventaire.Columns("Ecart").DefaultCellStyle.Format = "N0"
                gridInventaire.Columns("Ecart").Width = 90
            End If
            If gridInventaire.Columns.Contains("Statut") Then
                gridInventaire.Columns("Statut").HeaderText = "Statut"
                gridInventaire.Columns("Statut").Width = 110
            End If
            If gridInventaire.Columns.Contains("Motif") Then
                gridInventaire.Columns("Motif").HeaderText = "Motif / Observation"
                gridInventaire.Columns("Motif").ReadOnly = (_inventaireStatut <> "EN_COURS")
                gridInventaire.Columns("Motif").MinimumWidth = 220
            End If
            If gridInventaire.Columns.Contains("StatutComptage") Then
                gridInventaire.Columns("StatutComptage").HeaderText = "Comptage"
                gridInventaire.Columns("StatutComptage").Width = 110
            End If

            gridInventaire.ReadOnly = (_inventaireStatut <> "EN_COURS")
            btnEnregistrerInventaire.Enabled = (_inventaireStatut = "EN_COURS")
            btnValiderEtAjuster.Enabled = (_inventaireStatut = "EN_COURS")
        End Sub

        Private Sub ChargerFiltresCategories()
            Dim items As New List(Of String) From {"Toutes"}
            If _inventaireTable IsNot Nothing Then
                For Each row As DataRow In _inventaireTable.Rows
                    Dim cat As String = If(row.IsNull("Categorie"), "", Convert.ToString(row("Categorie")).Trim())
                    If Not String.IsNullOrWhiteSpace(cat) AndAlso Not items.Contains(cat) Then
                        items.Add(cat)
                    End If
                Next
            End If
            cmbCategorie.DataSource = Nothing
            cmbCategorie.Items.Clear()
            For Each item As String In items
                cmbCategorie.Items.Add(item)
            Next
            cmbCategorie.SelectedIndex = 0
        End Sub

        Private Sub AppliquerFiltresInventaire()
            If _inventaireView Is Nothing Then Return

            Dim filtres As New List(Of String)()
            Dim recherche As String = txtRecherche.Text.Trim()
            If Not String.IsNullOrWhiteSpace(recherche) Then
                Dim q As String = recherche.Replace("'", "''")
                filtres.Add("(CONVERT(NomProduit, 'System.String') LIKE '%" & q & "%' OR CONVERT(CodeProduit, 'System.String') LIKE '%" & q & "%' OR CONVERT(Categorie, 'System.String') LIKE '%" & q & "%' OR CONVERT(Motif, 'System.String') LIKE '%" & q & "%')")
            End If

            If cmbCategorie.SelectedItem IsNot Nothing Then
                Dim categorie As String = Convert.ToString(cmbCategorie.SelectedItem)
                If Not String.Equals(categorie, "Toutes", StringComparison.OrdinalIgnoreCase) Then
                    filtres.Add("Categorie = '" & categorie.Replace("'", "''") & "'")
                End If
            End If

            If cmbStatut.SelectedItem IsNot Nothing Then
                Dim statut As String = Convert.ToString(cmbStatut.SelectedItem)
                Select Case statut
                    Case "Non comptés"
                        filtres.Add("StockPhysique IS NULL")
                    Case "Comptés"
                        filtres.Add("StockPhysique IS NOT NULL")
                    Case "Conformes"
                        filtres.Add("Statut = 'CONFORME'")
                    Case "Manques"
                        filtres.Add("Statut = 'MANQUE'")
                    Case "Surplus"
                        filtres.Add("Statut = 'SURPLUS'")
                End Select
            End If

            _inventaireView.RowFilter = String.Join(" AND ", filtres.ToArray())
        End Sub

        Private Sub ActualiserResumeInventaire()
            If _inventaireTable Is Nothing OrElse _inventaireTable.Rows.Count = 0 Then
                lblTotalProduits.Text = "0"
                lblProduitsComptes.Text = "0"
                lblProduitsNonComptes.Text = "0"
                lblProduitsConformes.Text = "0"
                lblProduitsManques.Text = "0"
                lblProduitsSurplus.Text = "0"
                lblValeurEcarts.Text = "0 FC"
                lblProgression.Text = "0 %"
                Return
            End If

            Dim total As Integer = _inventaireTable.Rows.Count
            Dim comptes As Integer = 0
            Dim nonComptes As Integer = 0
            Dim conformes As Integer = 0
            Dim manques As Integer = 0
            Dim surplus As Integer = 0
            Dim valeurEcarts As Decimal = 0D

            For Each row As DataRow In _inventaireTable.Rows
                Dim stockPhysiqueNull As Boolean = row.IsNull("StockPhysique")
                If stockPhysiqueNull Then
                    nonComptes += 1
                Else
                    comptes += 1
                    Dim statut As String = If(row.IsNull("Statut"), "", Convert.ToString(row("Statut"))).ToUpperInvariant()
                    Select Case statut
                        Case "CONFORME"
                            conformes += 1
                        Case "MANQUE"
                            manques += 1
                        Case "SURPLUS"
                            surplus += 1
                    End Select
                    Dim ecart As Decimal = If(row.IsNull("Ecart"), 0D, Convert.ToDecimal(row("Ecart")))
                    Dim prixAchat As Decimal = If(row.Table.Columns.Contains("PrixAchat") AndAlso Not row.IsNull("PrixAchat"), Convert.ToDecimal(row("PrixAchat")), 0D)
                    valeurEcarts += Math.Abs(ecart) * prixAchat
                End If
            Next

            lblTotalProduits.Text = FormatageGlobal.FormatNombre(total)
            lblProduitsComptes.Text = FormatageGlobal.FormatNombre(comptes)
            lblProduitsNonComptes.Text = FormatageGlobal.FormatNombre(nonComptes)
            lblProduitsConformes.Text = FormatageGlobal.FormatNombre(conformes)
            lblProduitsManques.Text = FormatageGlobal.FormatNombre(manques)
            lblProduitsSurplus.Text = FormatageGlobal.FormatNombre(surplus)
            lblValeurEcarts.Text = FormatageGlobal.FormatMontant(valeurEcarts)
            Dim progression As Decimal = If(total = 0, 0D, (comptes * 100D) / total)
            lblProgression.Text = FormatageGlobal.FormatPourcentage(Math.Round(progression, 0))
        End Sub

        Private Sub ChargerProduitsConsultation()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim repoProduits As New ProduitRepository(New DAL(cs))
                _produitsConsultation = repoProduits.ListerTable()

                cmbProduitConsultation.DataSource = Nothing
                cmbProduitConsultation.DisplayMember = "Libelle"
                cmbProduitConsultation.ValueMember = "ProduitId"
                cmbProduitConsultation.DataSource = _produitsConsultation
            Catch ex As Exception
                MessageBox.Show("Erreur chargement produits consultation: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerConsultationProduit(sender As Object, e As EventArgs)
            Try
                If cmbProduitConsultation.SelectedValue Is Nothing OrElse TypeOf cmbProduitConsultation.SelectedValue Is DataRowView Then Return
                Dim produitId As Integer = Convert.ToInt32(cmbProduitConsultation.SelectedValue)
                Dim dt As DataTable = _stockService.ObtenirAnalyseProduit(produitId)
                If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return

                Dim row As DataRow = dt.Rows(0)
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
                Dim stockCartons As Decimal = LireDecimalTable(row, "StockRestantCartons")
                Dim stockPieces As Decimal = LireDecimalTable(row, "StockRestantPieces")
                Dim venteCarton As Decimal = LireDecimalTable(row, "TotalVenteCartons")
                Dim resteVentePieces As Decimal = LireDecimalTable(row, "ResteVentePieces")
                Dim montantTotalGenere As Decimal = LireDecimalTable(row, "MontantTotalGenere")

                lblAnalyseSortieGros.Text = "Entrées: " & FormatageGlobal.FormatNombre(totalEntrees) & " | Ventes: " & FormatageGlobal.FormatNombre(totalVentes) & Environment.NewLine &
                    "Vente en cartons: " & FormatageGlobal.FormatNombre(venteCarton) & "C + " & FormatageGlobal.FormatNombre(resteVentePieces) & "P"
                lblAnalyseSortiePiece.Text = "Sorties manuelles: " & FormatageGlobal.FormatNombre(totalSortiesManuelles) & " | Pertes: " & FormatageGlobal.FormatNombre(totalPertes)
                lblAnalyseRestantGros.Text = "Dons: " & FormatageGlobal.FormatNombre(totalDons) & " | Allocations: " & FormatageGlobal.FormatNombre(totalAllocations)
                lblAnalyseRestantPiece.Text = "Dettes client: " & FormatageGlobal.FormatNombre(totalDettesClients) & " | Dettes boss: " & FormatageGlobal.FormatNombre(totalDettesBoss) & " | Hors caisse: " & FormatageGlobal.FormatNombre(totalSortiesHorsCaisse)
                lblAnalyseRealisation.Text = "G:" & FormatageGlobal.FormatNombre(totalGros) &
                    " D:" & FormatageGlobal.FormatNombre(totalDemi) &
                    " Q:" & FormatageGlobal.FormatNombre(totalQuart) &
                    " P:" & FormatageGlobal.FormatNombre(totalPiece) &
                    " Dz:" & FormatageGlobal.FormatNombre(totalDouzaine) &
                    " | Stock réel: " & FormatageGlobal.FormatNombre(stockReel) & " P" &
                    " | Stock: " & FormatageGlobal.FormatNombre(stockCartons) & "C+" & FormatageGlobal.FormatNombre(stockPieces) & "P" &
                    " | Mnt: " & FormatageGlobal.FormatMontant(montantTotalGenere)

                gridEntrees.DataSource = _repo.ChargerHistoriqueEntrees(produitId)
                gridSorties.DataSource = _repo.ChargerHistoriqueSorties(produitId)
                gridAncienInventaire.DataSource = _repo.ChargerHistoriqueStockInventaire(produitId)
                ConfigurerGrillesConsultation()
            Catch ex As Exception
                MessageBox.Show("Erreur analyse consultation: " & ex.Message)
            End Try
        End Sub

        Private Sub ConfigurerGrillesConsultation()
            ConfigurerGrilleLectureSeule(gridEntrees)
            ConfigurerGrilleLectureSeule(gridSorties)
            ConfigurerGrilleLectureSeule(gridAncienInventaire)

            If gridEntrees.Columns.Contains("DateEntree") Then gridEntrees.Columns("DateEntree").DefaultCellStyle.Format = "dd/MM/yyyy"
            If gridSorties.Columns.Contains("DateSortie") Then gridSorties.Columns("DateSortie").DefaultCellStyle.Format = "dd/MM/yyyy"
            If gridAncienInventaire.Columns.Contains("DateInventaire") Then gridAncienInventaire.Columns("DateInventaire").DefaultCellStyle.Format = "dd/MM/yyyy"
        End Sub

        Private Sub ConfigurerGrilleLectureSeule(dgv As DataGridView)
            If dgv Is Nothing Then Return
            dgv.ReadOnly = True
            dgv.AllowUserToAddRows = False
            dgv.AllowUserToDeleteRows = False
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            dgv.RowHeadersVisible = False
            dgv.EnableHeadersVisualStyles = False
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250)
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = ColorPrimary
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254)
            dgv.DefaultCellStyle.SelectionForeColor = ColorPrimary
        End Sub

        Private Sub NouvelInventaire(sender As Object, e As EventArgs)
            Try
                Dim dtEnCours As DataTable = _repo.ObtenirInventaireEnCours()
                If dtEnCours IsNot Nothing AndAlso dtEnCours.Rows.Count > 0 Then
                    Dim choix As DialogResult = MessageBox.Show(
                        "Un inventaire EN_COURS existe déjà." & Environment.NewLine &
                        "Oui = continuer l'inventaire en cours" & Environment.NewLine &
                        "Non = annuler l'ancien inventaire et en créer un nouveau" & Environment.NewLine &
                        "Annuler = ne rien faire",
                        "Inventaire en cours",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question)

                    If choix = DialogResult.Yes Then
                        ChargerInventaireDepuisBase(Convert.ToInt32(dtEnCours.Rows(0)("InventaireId")))
                        Return
                    ElseIf choix = DialogResult.No Then
                        _repo.AnnulerInventaire(Convert.ToInt32(dtEnCours.Rows(0)("InventaireId")), SessionUtilisateur.UtilisateurId, "Annulation pour création d'un nouvel inventaire")
                    Else
                        Return
                    End If
                End If

                _referenceInventaire = _repo.GenererReferenceInventaire()
                _inventaireId = _repo.CreerInventaire(_referenceInventaire, SessionUtilisateur.UtilisateurId, "Inventaire créé depuis FrmInventaireIntelligent")
                _repo.InitialiserLignesInventaire(_inventaireId)
                _inventaireStatut = "EN_COURS"
                lblInventaireRef.Text = "Référence: " & _referenceInventaire
                lblInventaireStatut.Text = "Statut: EN_COURS"
                ChargerInventaireDepuisBase(_inventaireId)
                MessageBox.Show("Inventaire initialisé.")
            Catch ex As Exception
                MessageBox.Show("Erreur création inventaire: " & ex.Message)
            End Try
        End Sub

        Private Sub ReprendreInventaireEnCours(sender As Object, e As EventArgs)
            Try
                Dim dt As DataTable = _repo.ObtenirInventaireEnCours()
                If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                    MessageBox.Show("Aucun inventaire EN_COURS à reprendre.")
                    Return
                End If
                ChargerInventaireDepuisBase(Convert.ToInt32(dt.Rows(0)("InventaireId")))
            Catch ex As Exception
                MessageBox.Show("Erreur reprise inventaire: " & ex.Message)
            End Try
        End Sub

        Private Sub EnregistrerInventaire(sender As Object, e As EventArgs)
            Try
                If _inventaireId = 0 Then
                    MessageBox.Show("Créez ou reprenez un inventaire avant l'enregistrement.")
                    Return
                End If
                If _inventaireTable Is Nothing OrElse _inventaireTable.Rows.Count = 0 Then
                    MessageBox.Show("Aucune ligne à enregistrer.")
                    Return
                End If
                If Not String.Equals(_inventaireStatut, "EN_COURS", StringComparison.OrdinalIgnoreCase) Then
                    MessageBox.Show("Cet inventaire n'est plus modifiable.")
                    Return
                End If
                _repo.RemplacerLignesInventaire(_inventaireId, _inventaireTable)
                MessageBox.Show("Inventaire enregistré.")
                ChargerInventaireDepuisBase(_inventaireId)
            Catch ex As Exception
                MessageBox.Show("Erreur enregistrement inventaire: " & ex.Message)
            End Try
        End Sub

        Private Sub ValiderEtAjuster(sender As Object, e As EventArgs)
            Try
                If _inventaireId = 0 Then
                    MessageBox.Show("Créez ou reprenez un inventaire avant validation.")
                    Return
                End If
                If _inventaireTable Is Nothing OrElse _inventaireTable.Rows.Count = 0 Then
                    MessageBox.Show("Aucune ligne d'inventaire.")
                    Return
                End If

                Dim nonComptes As Integer = 0
                For Each row As DataRow In _inventaireTable.Rows
                    If row.IsNull("StockPhysique") Then nonComptes += 1
                Next
                If nonComptes > 0 Then
                    MessageBox.Show("Impossible de valider. Il reste " & nonComptes.ToString() & " produit(s) non compté(s).")
                    cmbStatut.SelectedItem = "Non comptés"
                    AppliquerFiltresInventaire()
                    Return
                End If

                If MessageBox.Show("Cette action va clôturer l'inventaire et ajuster le stock. Voulez-vous continuer ?", "Validation inventaire", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
                    Return
                End If

                Dim mouvements As Integer = _stockService.AppliquerAjustementsInventaire(_inventaireId, _referenceInventaire, _inventaireTable, SessionUtilisateur.UtilisateurId)
                MessageBox.Show("Inventaire validé et ajusté. " & mouvements.ToString() & " mouvement(s) créé(s).")
                ChargerInventaireDepuisBase(_inventaireId)
            Catch ex As Exception
                MessageBox.Show("Erreur validation inventaire: " & ex.Message)
            End Try
        End Sub

        Private Sub ImprimerInventaire(sender As Object, e As EventArgs)
            Try
                _impressionInventaireTitre = "RAPPORT D'INVENTAIRE"
                _impressionInventaireReference = If(String.IsNullOrWhiteSpace(_referenceInventaire), "-", _referenceInventaire)
                _impressionInventaireStatut = If(String.IsNullOrWhiteSpace(_inventaireStatut), "EN_COURS", _inventaireStatut)
                If String.IsNullOrWhiteSpace(_impressionInventaireDate) Then
                    _impressionInventaireDate = Date.Now.ToString("dd/MM/yyyy HH:mm")
                End If
                _impressionInventaireTable = _inventaireTable
                _impressionIndexLigne = 0

                _printDoc.DefaultPageSettings.PaperSize = New PaperSize("A4", 827, 1169)
                _printDoc.DefaultPageSettings.Margins = New System.Drawing.Printing.Margins(30, 30, 30, 30)
                Dim lignes As List(Of String) = ConstruireLignesInventaire()
                If lignes.Count = 0 Then
                    MessageBox.Show("Aucune donnée à imprimer.")
                    Return
                End If
                _printPreview.Document = _printDoc
                _printPreview.Width = 1000
                _printPreview.Height = 700
                _impressionIndexLigne = 0
                _printPreview.ShowDialog(Me)
            Catch ex As Exception
                MessageBox.Show("Erreur impression inventaire: " & ex.Message)
            End Try
        End Sub

        Private Sub ImprimerInventaireHistorique(sender As Object, e As EventArgs)
            Try
                If _historiqueInventaireIdSelectionne <= 0 OrElse _impressionInventaireTable Is Nothing OrElse _impressionInventaireTable.Rows.Count = 0 Then
                    MessageBox.Show("Sélectionnez un inventaire historique à imprimer.")
                    Return
                End If

                _printDoc.DefaultPageSettings.PaperSize = New PaperSize("A4", 827, 1169)
                _printDoc.DefaultPageSettings.Margins = New System.Drawing.Printing.Margins(30, 30, 30, 30)
                _printPreview.Document = _printDoc
                _printPreview.Width = 1000
                _printPreview.Height = 700
                _impressionIndexLigne = 0
                _printPreview.ShowDialog(Me)
            Catch ex As Exception
                MessageBox.Show("Erreur impression inventaire historique: " & ex.Message)
            End Try
        End Sub

        Private Sub ExporterPdfInventaire(sender As Object, e As EventArgs)
            Try
                Dim lignes As List(Of String) = ConstruireLignesInventaire()
                If lignes.Count = 0 Then
                    MessageBox.Show("Aucune donnée à exporter.")
                    Return
                End If
                Using sfd As New SaveFileDialog()
                    sfd.Filter = "PDF (*.pdf)|*.pdf"
                    sfd.FileName = "Rapport_Inventaire_" & If(String.IsNullOrWhiteSpace(_referenceInventaire), Date.Now.ToString("yyyyMMddHHmmss"), _referenceInventaire) & ".pdf"
                    If sfd.ShowDialog(Me) = DialogResult.OK Then
                        PdfHelper.GenererPdfSimple(sfd.FileName, "RAPPORT D'INVENTAIRE", lignes)
                        MessageBox.Show("PDF généré.")
                    End If
                End Using
            Catch ex As Exception
                MessageBox.Show("Erreur export PDF: " & ex.Message)
            End Try
        End Sub

        Private Function ConstruireLignesInventaire() As List(Of String)
            Dim lignes As New List(Of String)()
            lignes.Add("Référence: " & _impressionInventaireReference)
            lignes.Add("Date: " & _impressionInventaireDate)
            lignes.Add("Statut: " & _impressionInventaireStatut)
            If Not String.IsNullOrWhiteSpace(_impressionInventaireObservation) Then
                lignes.Add("Observation: " & _impressionInventaireObservation)
            End If
            lignes.Add("")

            Dim source As DataTable = If(_impressionInventaireTable IsNot Nothing, _impressionInventaireTable, _inventaireTable)
            If source IsNot Nothing Then
                For Each row As DataRow In source.Rows
                    Dim produit As String = If(row.IsNull("NomProduit"), "", Convert.ToString(row("NomProduit")))
                    Dim theo As Decimal = If(row.IsNull("StockTheorique"), 0D, Convert.ToDecimal(row("StockTheorique")))
                    Dim phys As String = If(row.IsNull("StockPhysique"), "N/C", Convert.ToDecimal(row("StockPhysique")).ToString("N0"))
                    Dim ecart As String = If(row.IsNull("Ecart"), "N/C", Convert.ToDecimal(row("Ecart")).ToString("N0"))
                    Dim statut As String = If(row.IsNull("Statut"), "", Convert.ToString(row("Statut")))
                    lignes.Add(produit & " | Theo:" & theo.ToString("N0") & " | Phys:" & phys & " | Ecart:" & ecart & " | " & statut)
                Next
            End If
            Return lignes
        End Function

        Private Sub ImprimerPageInventaire(sender As Object, e As PrintPageEventArgs)
            Try
                Dim dt As DataTable = If(_impressionInventaireTable IsNot Nothing, _impressionInventaireTable, _inventaireTable)
                If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                    e.HasMorePages = False
                    Return
                End If

                Dim param As ParametreDTO = (New ParametreService(New ParametreRepository(New DAL(ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString)))).Charger()
                Dim y As Integer = 30
                Dim x As Integer = 30
                Dim pinceauBleu As New SolidBrush(Color.FromArgb(17, 35, 74))
                Dim pinceauGris As New SolidBrush(Color.FromArgb(92, 104, 120))
                Dim fontTitre As New Font("Segoe UI", 16, FontStyle.Bold)
                Dim fontSousTitre As New Font("Segoe UI", 10, FontStyle.Regular)
                Dim fontBloc As New Font("Segoe UI", 9.5F, FontStyle.Regular)
                Dim fontBlocGras As New Font("Segoe UI", 10, FontStyle.Bold)
                Dim penBordure As New Pen(Color.FromArgb(210, 219, 232))
                Dim rowPen As New Pen(Color.FromArgb(232, 236, 242))

                If param IsNot Nothing AndAlso param.LogoPath <> "" AndAlso File.Exists(param.LogoPath) Then
                    Using logo As Image = Image.FromFile(param.LogoPath)
                        e.Graphics.DrawImage(logo, x, y, 70, 70)
                    End Using
                    x += 84
                End If

                e.Graphics.DrawString(If(param IsNot Nothing AndAlso param.NomMagasin <> "", param.NomMagasin, "Paons Rehoboth"), fontTitre, pinceauBleu, x, y)
                y += 28
                e.Graphics.DrawString(If(param IsNot Nothing, param.AdresseMagasin, ""), fontSousTitre, pinceauGris, x, y)
                y += 18
                e.Graphics.DrawString(If(param IsNot Nothing, param.TelephoneMagasin, ""), fontSousTitre, pinceauGris, x, y)
                y = 118

                e.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(17, 35, 74)), 30, y, 760, 32)
                e.Graphics.DrawString(_impressionInventaireTitre, New Font("Segoe UI", 12, FontStyle.Bold), Brushes.White, 42, y + 7)
                y += 48

                e.Graphics.DrawRectangle(penBordure, 30, y, 360, 92)
                e.Graphics.DrawRectangle(penBordure, 430, y, 360, 92)
                e.Graphics.DrawString("Informations inventaire", fontBlocGras, pinceauBleu, 42, y + 10)
                e.Graphics.DrawString("Référence : " & _impressionInventaireReference, fontBloc, Brushes.Black, 42, y + 34)
                e.Graphics.DrawString("Date : " & _impressionInventaireDate, fontBloc, Brushes.Black, 42, y + 54)
                e.Graphics.DrawString("Statut : " & _impressionInventaireStatut, fontBloc, Brushes.Black, 42, y + 74)
                e.Graphics.DrawString("Observation", fontBlocGras, pinceauBleu, 442, y + 10)
                e.Graphics.DrawString(If(String.IsNullOrWhiteSpace(_impressionInventaireObservation), "-", _impressionInventaireObservation), fontBloc, Brushes.Black, 442, y + 34)
                e.Graphics.DrawString("Lignes : " & dt.Rows.Count.ToString(), fontBloc, Brushes.Black, 442, y + 74)
                y += 116

                Dim colProduit As Integer = 42
                Dim colTheorique As Integer = 360
                Dim colPhysique As Integer = 460
                Dim colEcart As Integer = 560
                Dim colStatut As Integer = 640
                Dim colMotif As Integer = 720

                e.Graphics.FillRectangle(New SolidBrush(Color.FromArgb(229, 239, 252)), 30, y, 760, 28)
                e.Graphics.DrawString("Produit", fontBlocGras, pinceauBleu, colProduit, y + 6)
                e.Graphics.DrawString("Théorique", fontBlocGras, pinceauBleu, colTheorique, y + 6)
                e.Graphics.DrawString("Physique", fontBlocGras, pinceauBleu, colPhysique, y + 6)
                e.Graphics.DrawString("Écart", fontBlocGras, pinceauBleu, colEcart, y + 6)
                e.Graphics.DrawString("Statut", fontBlocGras, pinceauBleu, colStatut, y + 6)
                e.Graphics.DrawString("Motif", fontBlocGras, pinceauBleu, colMotif, y + 6)
                y += 34

                For i As Integer = _impressionIndexLigne To dt.Rows.Count - 1
                    Dim row As DataRow = dt.Rows(i)
                    If y > e.MarginBounds.Bottom - 40 Then
                        _impressionIndexLigne = i
                        e.HasMorePages = True
                        Return
                    End If
                    e.Graphics.DrawLine(rowPen, 30, y + 16, 790, y + 16)
                    e.Graphics.DrawString(Convert.ToString(row("NomProduit")), fontBloc, Brushes.Black, colProduit, y)
                    e.Graphics.DrawString(Convert.ToDecimal(If(row.IsNull("StockTheorique"), 0D, row("StockTheorique"))).ToString("N0"), fontBloc, Brushes.Black, colTheorique, y)
                    If row.IsNull("StockPhysique") Then
                        e.Graphics.DrawString("N/C", fontBloc, Brushes.Black, colPhysique, y)
                    Else
                        e.Graphics.DrawString(Convert.ToDecimal(row("StockPhysique")).ToString("N0"), fontBloc, Brushes.Black, colPhysique, y)
                    End If
                    If row.IsNull("Ecart") Then
                        e.Graphics.DrawString("N/C", fontBloc, Brushes.Black, colEcart, y)
                    Else
                        e.Graphics.DrawString(Convert.ToDecimal(row("Ecart")).ToString("N0"), fontBloc, Brushes.Black, colEcart, y)
                    End If
                    e.Graphics.DrawString(Convert.ToString(row("Statut")), fontBloc, Brushes.Black, colStatut, y)
                    e.Graphics.DrawString(If(row.IsNull("Motif"), "", Convert.ToString(row("Motif"))), fontBloc, Brushes.Black, colMotif, y)
                    y += 24
                Next

                _impressionIndexLigne = 0
                e.HasMorePages = False
            Catch ex As Exception
                MessageBox.Show("Erreur impression inventaire: " & ex.Message)
                e.HasMorePages = False
            End Try
        End Sub

        Private Sub TexteRechercheOuFiltreChanged(sender As Object, e As EventArgs)
            AppliquerFiltresInventaire()
        End Sub

        Private Sub ChargerInventairesHistoriquesDepuisFiltres(sender As Object, e As EventArgs)
            Try
                ChargerInventairesHistoriques()
            Catch ex As Exception
                MessageBox.Show("Erreur chargement historique inventaires: " & ex.Message)
            End Try
        End Sub

        Private Sub GridInventairesHistoriques_SelectionChanged(sender As Object, e As EventArgs)
            Try
                If _chargementHistoriqueEnCours Then Return
                If gridInventairesHistoriques Is Nothing OrElse gridInventairesHistoriques.CurrentRow Is Nothing Then
                    Exit Sub
                End If
                Dim idx As Integer = gridInventairesHistoriques.CurrentRow.Index
                Dim celluleVisible As DataGridViewCell = ObtenirPremiereCelluleVisible(gridInventairesHistoriques.CurrentRow)
                If celluleVisible IsNot Nothing AndAlso gridInventairesHistoriques.CurrentCell IsNot celluleVisible Then
                    gridInventairesHistoriques.CurrentCell = celluleVisible
                End If
                ChargerInventaireHistoriqueSelectionne(idx)
            Catch ex As Exception
                MessageBox.Show("Erreur sélection historique: " & ex.Message)
            End Try
        End Sub

        Private Sub GridInventaire_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs)
            If _chargementEnCours OrElse _inventaireTable Is Nothing Then Return
            If e.RowIndex < 0 OrElse e.RowIndex >= gridInventaire.Rows.Count Then Return
            If gridInventaire.Columns(e.ColumnIndex).Name <> "StockPhysique" AndAlso gridInventaire.Columns(e.ColumnIndex).Name <> "Motif" Then Return

            Dim rowView As DataRowView = TryCast(gridInventaire.Rows(e.RowIndex).DataBoundItem, DataRowView)
            If rowView Is Nothing Then Return
            Dim row As DataRow = rowView.Row
            MettreAJourStatutLigne(row, e.ColumnIndex)
            ActualiserResumeInventaire()
        End Sub

        Private Sub MettreAJourStatutLigne(row As DataRow, colonneIndex As Integer)
            If row Is Nothing Then Return
            Dim stockTheo As Decimal = If(row.IsNull("StockTheorique"), 0D, Convert.ToDecimal(row("StockTheorique")))
            Dim textePhysique As String = If(row.IsNull("StockPhysique"), "", Convert.ToString(row("StockPhysique")))
            Dim stockPhysiqueNull As Boolean = String.IsNullOrWhiteSpace(textePhysique)

            If stockPhysiqueNull Then
                row("StockPhysique") = DBNull.Value
                row("Ecart") = DBNull.Value
                row("Statut") = "NON_COMPTE"
                row("StatutComptage") = "NON_COMPTÉ"
                If row.Table.Columns.Contains("DateComptage") Then
                    row("DateComptage") = DBNull.Value
                End If
            Else
                Dim stockPhysique As Decimal = LireDecimalTexte(textePhysique)
                row("StockPhysique") = stockPhysique
                Dim ecart As Decimal = stockPhysique - stockTheo
                row("Ecart") = ecart
                row("Statut") = If(ecart = 0D, "CONFORME", If(ecart < 0D, "MANQUE", "SURPLUS"))
                row("StatutComptage") = "COMPTÉ"
                If row.Table.Columns.Contains("DateComptage") Then
                    row("DateComptage") = Date.Now
                End If
            End If
        End Sub

        Private Function LireDecimalTexte(valeur As String) As Decimal
            Dim d As Decimal = 0D
            If Decimal.TryParse(valeur, d) Then Return d
            If Decimal.TryParse(valeur.Replace(",", "."), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, d) Then Return d
            Return 0D
        End Function

        Private Function LireDecimalTable(row As DataRow, colonne As String) As Decimal
            If row Is Nothing OrElse row.Table Is Nothing OrElse Not row.Table.Columns.Contains(colonne) OrElse row.IsNull(colonne) Then
                Return 0D
            End If
            Return Convert.ToDecimal(row(colonne))
        End Function

        Private Sub GridInventaire_CellValidating(sender As Object, e As DataGridViewCellValidatingEventArgs)
            If gridInventaire.Columns(e.ColumnIndex).Name <> "StockPhysique" Then Return
            Dim txt As String = If(e.FormattedValue Is Nothing, "", e.FormattedValue.ToString().Trim())
            If String.IsNullOrWhiteSpace(txt) Then Return
            Dim d As Decimal
            If Not Decimal.TryParse(txt, d) AndAlso Not Decimal.TryParse(txt.Replace(",", "."), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, d) Then
                MessageBox.Show("Valeur de stock physique invalide.")
                e.Cancel = True
            End If
        End Sub

        Private Sub GridInventaire_DataError(sender As Object, e As DataGridViewDataErrorEventArgs)
            e.ThrowException = False
        End Sub

        Private Sub GridInventaire_RowPrePaint(sender As Object, e As DataGridViewRowPrePaintEventArgs)
            If e.RowIndex < 0 OrElse e.RowIndex >= gridInventaire.Rows.Count Then Return
            Dim row As DataGridViewRow = gridInventaire.Rows(e.RowIndex)
            If row Is Nothing OrElse row.IsNewRow Then Return

            Dim statut As String = ""
            If gridInventaire.Columns.Contains("Statut") Then
                statut = Convert.ToString(row.Cells("Statut").Value)
            End If
            Dim stockPhysiqueVide As Boolean = False
            If gridInventaire.Columns.Contains("StockPhysique") Then
                stockPhysiqueVide = row.Cells("StockPhysique").Value Is Nothing OrElse IsDBNull(row.Cells("StockPhysique").Value)
            End If

            If String.Equals(_inventaireStatut, "VALIDÉ", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(_inventaireStatut, "ANNULÉ", StringComparison.OrdinalIgnoreCase) Then
                row.DefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249)
                row.DefaultCellStyle.ForeColor = ColorSecondary
                Return
            End If

            If stockPhysiqueVide Then
                row.DefaultCellStyle.BackColor = Color.FromArgb(254, 242, 242)
            Else
                Select Case statut.ToUpperInvariant()
                    Case "CONFORME"
                        row.DefaultCellStyle.BackColor = Color.FromArgb(240, 253, 244)
                    Case "MANQUE"
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 247, 237)
                    Case "SURPLUS"
                        row.DefaultCellStyle.BackColor = Color.FromArgb(239, 246, 255)
                    Case Else
                        row.DefaultCellStyle.BackColor = Color.White
                End Select
            End If
        End Sub

        Private Sub AppliquerModeLectureSeule(lectureSeule As Boolean)
            If gridInventaire Is Nothing Then Return
            gridInventaire.ReadOnly = lectureSeule
            If gridInventaire.Columns.Contains("StockPhysique") Then
                gridInventaire.Columns("StockPhysique").ReadOnly = lectureSeule
            End If
            If gridInventaire.Columns.Contains("Motif") Then
                gridInventaire.Columns("Motif").ReadOnly = lectureSeule
            End If
            btnEnregistrerInventaire.Enabled = Not lectureSeule
            btnValiderEtAjuster.Enabled = Not lectureSeule
        End Sub

        Private Sub MettreModeLectureSeule(lectureSeule As Boolean)
            AppliquerModeLectureSeule(lectureSeule)
        End Sub

        Private Sub HandleGridSelection(sender As Object, e As EventArgs)
            If gridInventaire Is Nothing OrElse gridInventaire.CurrentRow Is Nothing Then Return
        End Sub

        Protected Overrides Sub OnShown(e As EventArgs)
            MyBase.OnShown(e)
            MettreModeLectureSeule(_inventaireStatut <> "EN_COURS")
        End Sub
    End Class
End Namespace
