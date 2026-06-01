Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Drawing
Imports System.Drawing.Printing
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
            tabMain.TabPages.Add(tabInventaire)
            tabMain.TabPages.Add(tabConsultation)
            Me.Controls.Add(tabMain)

            ConstruireTabInventaire()
            ConstruireTabConsultation()
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
            Dim p1, p2, p3, p4, p5, p6, p7, p8 As Label
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

            Dim split As New SplitContainer() With {.Dock = DockStyle.Fill, .Orientation = Orientation.Vertical, .SplitterDistance = 550}
            Dim cardEntrees As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Padding = New Padding(10)}
            cardEntrees.Controls.Add(New Label() With {.Text = "Historique des entrées", .Font = FontSection, .AutoSize = True, .ForeColor = ColorPrimary, .Dock = DockStyle.Top})
            gridEntrees = CreerGrille(False)
            gridEntrees.Dock = DockStyle.Fill
            cardEntrees.Controls.Add(gridEntrees)
            split.Panel1.Controls.Add(cardEntrees)

            Dim cardSorties As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Padding = New Padding(10)}
            cardSorties.Controls.Add(New Label() With {.Text = "Historique des sorties", .Font = FontSection, .AutoSize = True, .ForeColor = ColorPrimary, .Dock = DockStyle.Top})
            gridSorties = CreerGrille(False)
            gridSorties.Dock = DockStyle.Fill
            cardSorties.Controls.Add(gridSorties)
            split.Panel2.Controls.Add(cardSorties)

            layoutHistorique.Controls.Add(split, 0, 0)

            Dim cardAncien As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Padding = New Padding(10)}
            cardAncien.Controls.Add(New Label() With {.Text = "Historique ancien StockInventaire", .Font = FontSection, .AutoSize = True, .ForeColor = ColorPrimary, .Dock = DockStyle.Top})
            gridAncienInventaire = CreerGrille(False)
            gridAncienInventaire.Dock = DockStyle.Fill
            cardAncien.Controls.Add(gridAncienInventaire)
            layoutHistorique.Controls.Add(cardAncien, 0, 1)
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
                AjouterFiltresStatuts()
                MettreModeLectureSeule(True)
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

                lblAnalyseSortieGros.Text = "Entrées: " & FormatageGlobal.FormatNombre(totalEntrees) & " | Ventes: " & FormatageGlobal.FormatNombre(totalVentes) & vbCrLf &
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
                        "Un inventaire EN_COURS existe déjà." & vbCrLf &
                        "Oui = continuer l'inventaire en cours" & vbCrLf &
                        "Non = annuler l'ancien inventaire et en créer un nouveau" & vbCrLf &
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
                Dim lignes As List(Of String) = ConstruireLignesInventaire()
                If lignes.Count = 0 Then
                    MessageBox.Show("Aucune donnée à imprimer.")
                    Return
                End If
                _printPreview.Document = _printDoc
                _printPreview.Width = 1000
                _printPreview.Height = 700
                _printPreview.ShowDialog(Me)
            Catch ex As Exception
                MessageBox.Show("Erreur impression inventaire: " & ex.Message)
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
            lignes.Add("Référence: " & If(String.IsNullOrWhiteSpace(_referenceInventaire), "-", _referenceInventaire))
            lignes.Add("Date: " & Date.Now.ToString("dd/MM/yyyy HH:mm"))
            lignes.Add("Statut: " & If(String.IsNullOrWhiteSpace(_inventaireStatut), "EN_COURS", _inventaireStatut))
            lignes.Add("")

            If _inventaireTable IsNot Nothing Then
                For Each row As DataRow In _inventaireTable.Rows
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
            Dim lignes As List(Of String) = ConstruireLignesInventaire()
            Dim y As Integer = 40
            e.Graphics.DrawString("RAPPORT D'INVENTAIRE", New Font("Segoe UI", 14, FontStyle.Bold), Brushes.Black, 40, y)
            y += 25
            e.Graphics.DrawString("Référence: " & If(String.IsNullOrWhiteSpace(_referenceInventaire), "-", _referenceInventaire), FontLabel, Brushes.Black, 40, y)
            y += 20
            e.Graphics.DrawString("Statut: " & If(String.IsNullOrWhiteSpace(_inventaireStatut), "EN_COURS", _inventaireStatut), FontLabel, Brushes.Black, 40, y)
            y += 25
            For Each line As String In lignes
                e.Graphics.DrawString(line, FontLabel, Brushes.Black, 40, y)
                y += 18
                If y > e.MarginBounds.Bottom - 40 Then
                    e.HasMorePages = False
                    Exit Sub
                End If
            Next
            e.HasMorePages = False
        End Sub

        Private Sub TexteRechercheOuFiltreChanged(sender As Object, e As EventArgs)
            AppliquerFiltresInventaire()
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
