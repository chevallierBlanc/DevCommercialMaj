Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D

Namespace DevCommerc8ak
    Public Class FormulaireAnalyseVente
        Inherits Form

        Private Shared ReadOnly MoisFrancais As String() = {
            "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
            "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"
        }

        ' --- COULEURS (Ajustées pour le nouveau design) ---
        Private ReadOnly ColorBg As Color = Color.FromArgb(248, 249, 250) ' Gris très clair pour le fond
        Private ReadOnly ColorHeaderBg As Color = Color.White ' En-tête blanc
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

        ' --- POLICES (Ajustées pour le nouveau design) ---
        Private ReadOnly FontTitle As New Font("Segoe UI", 22.0F, FontStyle.Bold)
        Private ReadOnly FontSubtitle As New Font("Segoe UI", 12.0F, FontStyle.Regular) ' Augmenté la taille
        Private ReadOnly FontLabel As New Font("Segoe UI", 10.0F, FontStyle.Regular)
        Private ReadOnly FontValue As New Font("Segoe UI", 28.0F, FontStyle.Bold)
        Private ReadOnly FontValueSmall As New Font("Segoe UI", 16.0F, FontStyle.Bold)
        Private ReadOnly FontButton As New Font("Segoe UI", 10.0F, FontStyle.Bold)
        Private ReadOnly FontTab As New Font("Segoe UI", 10.0F, FontStyle.Bold)

        Private ReadOnly rapportService As RapportService
        Private ReadOnly venteService As VenteService

        Private ReadOnly tabs As TabControl
        Private ReadOnly tabSynthese As TabPage
        Private ReadOnly tabDetail As TabPage

        Private ReadOnly cmbPeriode As ComboBox
        Private ReadOnly cmbMois As ComboBox
        Private ReadOnly cmbAnnee As ComboBox
        Private ReadOnly btnActualiser As Button
        Private ReadOnly btnOuvrirVentes As Button
        Private ReadOnly lblContexte As Label

        Private ReadOnly gridDetailVentes As DataGridView
        Private ReadOnly panelEvaluationCard As Panel
        Private ReadOnly panelBeneficeNetCard As Panel
        Private ReadOnly lblEvaluationValue As Label

        Private ReadOnly btnTabSynthese As Button
        Private ReadOnly btnTabDetail As Button

        Private ReadOnly timerAnimation As Timer
        Private ReadOnly timerFade As Timer

        Private lblValeurStockEntree As Label
        Private lblCoutMarchandisesVendues As Label
        Private lblChiffreAffaires As Label
        Private lblBeneficesRealise As Label
        Private lblBeneficeNetRealise As Label
        Private lblCoutStockRestant As Label
        Private lblProjectionBeneficeRestant As Label
        Private lblMargeBeneficiairePourcentage As Label

        Private _cibleValeurStockEntree As Decimal
        Private _cibleCoutMarchandisesVendues As Decimal
        Private _cibleChiffreAffaires As Decimal
        Private _cibleBeneficesRealise As Decimal
        Private _cibleBeneficeNetRealise As Decimal
        Private _cibleCoutStockRestant As Decimal
        Private _cibleProjectionBeneficeRestant As Decimal
        Private _cibleMargeBeneficiairePourcentage As Decimal
        Private _cibleDepensesTotal As Decimal
        Private _cibleChargesSortiesManuelles As Decimal

        Private _courantValeurStockEntree As Decimal
        Private _courantCoutMarchandisesVendues As Decimal
        Private _courantChiffreAffaires As Decimal
        Private _courantBeneficesRealise As Decimal
        Private _courantBeneficeNetRealise As Decimal
        Private _courantCoutStockRestant As Decimal
        Private _courantProjectionBeneficeRestant As Decimal
        Private _courantMargeBeneficiairePourcentage As Decimal
        Private _dateAnalyseDebut As Date = Date.MinValue
        Private _dateAnalyseFin As Date = Date.MinValue

        Public Sub New()
            Me.Text = "Analyse ventes"
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.WindowState = FormWindowState.Maximized
            Me.BackColor = ColorBg
            Me.DoubleBuffered = True
            Me.Opacity = 0

            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            rapportService = New RapportService(dal)
            venteService = New VenteService()

            ' --- LAYOUT PRINCIPAL ---
            Dim mainLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 4, ' Augmenté pour inclure la navigation par onglets personnalisée
                .Padding = New Padding(0)
            }
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 100)) ' Header
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 80))  ' Filtres
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 50))  ' Navigation par onglets personnalisée
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100)) ' Contenu principal (Tabs)
            mainLayout.BackColor = ColorBg

            ' --- HEADER ---
            Dim pnlHeader As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorHeaderBg,
                .Padding = New Padding(24, 16, 24, 16)
            }
            pnlHeader.BorderStyle = BorderStyle.None ' Pas de bordure pour le header

            Dim headerLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 1
            }
            headerLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 70))
            headerLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 30))
            headerLayout.BackColor = Color.Transparent

            Dim pnlHeaderLeft As New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.TopDown,
                .WrapContents = False,
                .AutoSize = True
            }
            pnlHeaderLeft.BackColor = Color.Transparent

            Dim lblTitre As New Label() With {
                .Text = "Dashboard Analyse Vente",
                .Font = FontTitle,
                .ForeColor = ColorTextPrimary,
                .AutoSize = True,
                .Margin = New Padding(0, 0, 0, 0)
            }
            Dim lblSousTitre As New Label() With {
                .Text = "Lecture des ventes, rentabilité, coût de stock restant et projection de bénéfice.",
                .Font = FontSubtitle,
                .ForeColor = ColorTextSecondary,
                .AutoSize = True,
                .Left = 26,
                .Top = 52,
                .Margin = New Padding(0, 4, 0, 0)
                        }
            pnlHeaderLeft.Controls.Add(lblTitre)
            pnlHeaderLeft.Controls.Add(lblSousTitre)

            btnActualiser = New Button() With {
                .Text = "Actualiser",
                .Width = 120,
                .Height = 36,
                .BackColor = ColorPrimary,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontButton,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(8, 0, 0, 0)
            }
            btnActualiser.FlatAppearance.BorderSize = 0
            btnActualiser.FlatAppearance.MouseDownBackColor = Color.FromArgb(ColorPrimary.R - 20, ColorPrimary.G - 20, ColorPrimary.B - 20)
            btnActualiser.FlatAppearance.MouseOverBackColor = Color.FromArgb(ColorPrimary.R + 20, ColorPrimary.G + 20, ColorPrimary.B + 20)

            btnOuvrirVentes = New Button() With {
                .Text = "Ouvrir ventes",
                .Width = 130,
                .Height = 36,
                .BackColor = ColorAccent,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontButton,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(8, 0, 0, 0)
            }
            btnOuvrirVentes.FlatAppearance.BorderSize = 0
            btnOuvrirVentes.FlatAppearance.MouseDownBackColor = Color.FromArgb(ColorAccent.R - 20, ColorAccent.G - 20, ColorAccent.B - 20)
            btnOuvrirVentes.FlatAppearance.MouseOverBackColor = Color.FromArgb(ColorAccent.R + 20, ColorAccent.G + 20, ColorAccent.B + 20)

            Dim pnlActions As New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.RightToLeft,
                .WrapContents = False,
                .Padding = New Padding(0, 0, 0, 0),
                .AutoSize = True
            }
            pnlActions.BackColor = Color.Transparent
            pnlActions.Controls.Add(btnOuvrirVentes)
            pnlActions.Controls.Add(btnActualiser)

            headerLayout.Controls.Add(pnlHeaderLeft, 0, 0)
            headerLayout.Controls.Add(pnlActions, 1, 0)
            pnlHeader.Controls.Add(headerLayout)

            ' --- FILTRES ---
            Dim pnlFiltres As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorCardBg,
                .Padding = New Padding(24, 12, 24, 12),
                .Margin = New Padding(0, 1, 0, 0) ' Petite marge pour séparer du header
            }
            pnlFiltres.BorderStyle = BorderStyle.None ' Pas de bordure pour le panel de filtres

            Dim filtresLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 7,
                .RowCount = 1,
                .AutoSize = True
            }
            filtresLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize)) ' Label Période
            filtresLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 140)) ' ComboBox Période
            filtresLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize)) ' Label Mois
            filtresLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 180)) ' ComboBox Mois
            filtresLayout.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize)) ' Label Année
            filtresLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 120)) ' ComboBox Année
            filtresLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100)) ' Label Contexte (prend l'espace restant)
            filtresLayout.BackColor = Color.Transparent

            Dim lblPeriode As New Label() With {.Text = "Période :", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Anchor = AnchorStyles.Left}
            cmbPeriode = New ComboBox() With {
                .Width = 120,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = FontLabel,
                .Anchor = AnchorStyles.Left
            }
            cmbPeriode.Items.AddRange(New Object() {"Mensuel", "Annuel"})

            Dim lblMois As New Label() With {.Text = "Mois :", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Anchor = AnchorStyles.Left, .Margin = New Padding(20, 0, 0, 0)}
            cmbMois = New ComboBox() With {
                .Width = 170,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = FontLabel,
                .Anchor = AnchorStyles.Left
            }

            Dim lblAnnee As New Label() With {.Text = "Année :", .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Anchor = AnchorStyles.Left, .Margin = New Padding(20, 0, 0, 0)}
            cmbAnnee = New ComboBox() With {
                .Width = 100,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = FontLabel,
                .Anchor = AnchorStyles.Left
            }

            lblContexte = New Label() With {
                .Text = "Période analysée : -",
                .Width = 800,
                .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
                .ForeColor = ColorTextPrimary,
                .Anchor = AnchorStyles.Left,
                .Margin = New Padding(30, 0, 0, 0)
            }


            filtresLayout.Controls.Add(lblPeriode, 0, 0)
            filtresLayout.Controls.Add(cmbPeriode, 1, 0)
            filtresLayout.Controls.Add(lblMois, 2, 0)
            filtresLayout.Controls.Add(cmbMois, 3, 0)
            filtresLayout.Controls.Add(lblAnnee, 4, 0)
            filtresLayout.Controls.Add(cmbAnnee, 5, 0)
            filtresLayout.Controls.Add(lblContexte, 6, 0)

            pnlFiltres.Controls.Add(filtresLayout)

            ' --- NAVIGATION PAR ONGLET PERSONNALISÉE ---
            Dim pnlTabNavigation As New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.LeftToRight,
                .Padding = New Padding(24, 0, 0, 0),
                .Margin = New Padding(0, 0, 0, 0),
                .BackColor = ColorBg
            }

            btnTabSynthese = New Button() With {
                .Text = "Synthèse",
                .Width = 120,
                .Height = 40,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontTab,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 8, 0)
            }
            AddHandler btnTabSynthese.Click, Sub() SetSelectedTab(0)

            btnTabDetail = New Button() With {
                .Text = "Détail ventes",
                .Width = 120,
                .Height = 40,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontTab,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 0, 0)
            }
            AddHandler btnTabDetail.Click, Sub() SetSelectedTab(1)

            pnlTabNavigation.Controls.Add(btnTabSynthese)
            pnlTabNavigation.Controls.Add(btnTabDetail)

            ' --- CONTENU PRINCIPAL (TABS) ---
            tabs = New TabControl() With {
                .Dock = DockStyle.Fill,
                .Padding = New Point(0, 0),
                .Margin = New Padding(24, 0, 24, 24) ' Marge autour des onglets, pas de marge supérieure car gérée par la navigation
            }
            tabs.Appearance = TabAppearance.FlatButtons
            tabs.ItemSize = New Size(0, 1) ' Cache les en-têtes d'onglets
            tabs.SizeMode = TabSizeMode.Fixed
            AddHandler tabs.GotFocus, AddressOf Tabs_GotFocus ' Empêche le focus sur les onglets

            tabSynthese = New TabPage("Synthèse") With {.BackColor = ColorBg, .Padding = New Padding(0)}
            tabDetail = New TabPage("Détail ventes") With {.BackColor = ColorBg, .Padding = New Padding(0)}
            tabs.TabPages.Add(tabSynthese)
            tabs.TabPages.Add(tabDetail)

            ' --- SYNTHESE TAB CONTENT ---
            Dim pnlSynthese As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorBg, .Padding = New Padding(0)}

            Dim tableKpi As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 3,
                .RowCount = 3,
                .BackColor = Color.Transparent,
                .Padding = New Padding(0),
                .Margin = New Padding(0)
            }
            tableKpi.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.3333F))
            tableKpi.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.3333F))
            tableKpi.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.3333F))
            tableKpi.RowStyles.Add(New RowStyle(SizeType.Percent, 33.3333F))
            tableKpi.RowStyles.Add(New RowStyle(SizeType.Percent, 33.3333F))
            tableKpi.RowStyles.Add(New RowStyle(SizeType.Percent, 33.3333F))

            ' Initialisation des labels pour les cartes KPI
            lblValeurStockEntree = New Label()
            lblCoutMarchandisesVendues = New Label()
            lblChiffreAffaires = New Label()
            lblBeneficesRealise = New Label()
            lblBeneficeNetRealise = New Label()
            lblCoutStockRestant = New Label()
            lblProjectionBeneficeRestant = New Label()
            lblMargeBeneficiairePourcentage = New Label()

            tableKpi.Controls.Add(CreerCarteKpi("Valeur stock entrée", ColorPrimary, lblValeurStockEntree), 0, 0)
            tableKpi.Controls.Add(CreerCarteKpi("Coût marchandises vendues", ColorAccent, lblCoutMarchandisesVendues), 1, 0)
            tableKpi.Controls.Add(CreerCarteKpi("Chiffre d'affaires", ColorSuccess, lblChiffreAffaires), 2, 0)
            tableKpi.Controls.Add(CreerCarteKpi("Bénéfice réalisé", ColorSuccess, lblBeneficesRealise), 0, 1)
            tableKpi.Controls.Add(CreerCarteKpi("Coût stock restant", ColorWarning, lblCoutStockRestant), 1, 1)
            tableKpi.Controls.Add(CreerCarteKpi("Projection bénéfice restant", ColorPrimary, lblProjectionBeneficeRestant), 2, 1)
            tableKpi.Controls.Add(CreerCarteKpi("Marge bénéficiaire", ColorAccent, lblMargeBeneficiairePourcentage), 0, 2)
            panelBeneficeNetCard = CreerCarteKpi("Bénéfice net réalisé", ColorNetBenefit, lblBeneficeNetRealise)
            RendreCarteCliquable(panelBeneficeNetCard)
            tableKpi.Controls.Add(panelBeneficeNetCard, 1, 2)
            panelEvaluationCard = CreerCarteTexte("Évaluation", Color.FromArgb(76, 175, 80), lblEvaluationValue)
            tableKpi.Controls.Add(panelEvaluationCard, 2, 2)

            Dim lblNote As New Label() With {
                .Text = "Les valeurs sont affichées sans décimales inutiles. Les montants sont en FC.",
                .Dock = DockStyle.Bottom,
                .Height = 30,
                .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic),
                .ForeColor = ColorTextSecondary,
                .Padding = New Padding(0, 6, 0, 0),
                .TextAlign = ContentAlignment.MiddleRight
            }

            Dim syntheseLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2
            }
            syntheseLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            syntheseLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))
            syntheseLayout.Controls.Add(tableKpi, 0, 0)
            syntheseLayout.Controls.Add(lblNote, 0, 1)

            pnlSynthese.Controls.Add(syntheseLayout)
            tabSynthese.Controls.Add(pnlSynthese)

            ' --- DETAIL VENTES TAB CONTENT ---
            gridDetailVentes = CreerGrille()
            gridDetailVentes.Dock = DockStyle.Fill
            tabDetail.Controls.Add(gridDetailVentes)

            ' Ajout des contrôles au layout principal
            mainLayout.Controls.Add(pnlHeader, 0, 0)
            mainLayout.Controls.Add(pnlFiltres, 0, 1)
            mainLayout.Controls.Add(pnlTabNavigation, 0, 2) ' Ajout de la navigation par onglets personnalisée
            mainLayout.Controls.Add(tabs, 0, 3)
            Me.Controls.Add(mainLayout)

            timerAnimation = New Timer() With {.Interval = 20}
            timerFade = New Timer() With {.Interval = 20}
            AddHandler timerAnimation.Tick, AddressOf TimerAnimation_Tick
            AddHandler timerFade.Tick, AddressOf TimerFade_Tick

            AddHandler cmbPeriode.SelectedIndexChanged, AddressOf ActualiserVisibilitePeriode
            AddHandler cmbMois.SelectedIndexChanged, AddressOf ChargerDonnees
            AddHandler cmbAnnee.SelectedIndexChanged, AddressOf ChargerDonnees
            AddHandler btnActualiser.Click, AddressOf ChargerDonnees
            AddHandler btnOuvrirVentes.Click, AddressOf OuvrirFormulaireVentes
            AddHandler tabs.SelectedIndexChanged, AddressOf ChargerDetailSiBesoin
            AddHandler Me.Load, AddressOf FormulaireAnalyseVente_Load

            InitialiserFiltres()
            SetSelectedTab(0) ' Sélectionne l'onglet Synthèse par défaut
        End Sub

        Private Sub Tabs_GotFocus(sender As Object, e As EventArgs)
            ' Empêche le focus sur les onglets pour un aspect plus propre
            Me.ActiveControl = If(tabs.SelectedTab Is tabSynthese, tabSynthese.Controls(0), tabDetail.Controls(0))
        End Sub

        Private Sub FormulaireAnalyseVente_Load(sender As Object, e As EventArgs)
            Me.Opacity = 0
            timerFade.Start()
            ChargerDonnees(Nothing, EventArgs.Empty)
        End Sub

        Private Sub InitialiserFiltres()
            cmbPeriode.SelectedIndex = 0

            cmbMois.Items.Clear()
            For i As Integer = 1 To 12
                cmbMois.Items.Add(New MoisItem(i, MoisFrancais(i - 1)))
            Next
            cmbMois.SelectedIndex = Date.Today.Month - 1

            cmbAnnee.Items.Clear()
            Dim anneeCourante As Integer = Date.Today.Year
            For i As Integer = anneeCourante - 5 To anneeCourante + 5
                cmbAnnee.Items.Add(i.ToString())
            Next
            cmbAnnee.SelectedItem = anneeCourante.ToString()

            ActualiserVisibilitePeriode(Nothing, EventArgs.Empty)
        End Sub

        Private Sub ActualiserVisibilitePeriode(sender As Object, e As EventArgs)
            Dim annuel As Boolean = String.Equals(Convert.ToString(cmbPeriode.SelectedItem), "Annuel", StringComparison.OrdinalIgnoreCase)
            cmbMois.Enabled = Not annuel
            ChargerContexte()
        End Sub

        Private Sub ChargerContexte()
            Dim annee As Integer = LireAnneeSelectionnee()
            If String.Equals(Convert.ToString(cmbPeriode.SelectedItem), "Annuel", StringComparison.OrdinalIgnoreCase) Then
                lblContexte.Text = "Période analysée : Année " & annee.ToString()
            Else
                Dim moisItem As MoisItem = TryCast(cmbMois.SelectedItem, MoisItem)
                Dim nomMois As String = If(moisItem Is Nothing, "", moisItem.Libelle)
                lblContexte.Text = "Période analysée : " & nomMois & " " & annee.ToString()
            End If
        End Sub

        Private Sub ChargerDonnees(sender As Object, e As EventArgs)
            If cmbAnnee.SelectedItem Is Nothing OrElse cmbMois.SelectedItem Is Nothing Then
                Return
            End If

            Try
                Dim debut As Date
                Dim finInclusive As Date
                CalculerPeriode(debut, finInclusive)
                _dateAnalyseDebut = debut.Date
                _dateAnalyseFin = finInclusive.Date

                Dim dtResume As DataTable = rapportService.AnalyseVente(debut, finInclusive)
                Dim row As DataRow = Nothing
                If dtResume IsNot Nothing AndAlso dtResume.Rows.Count > 0 Then
                    row = dtResume.Rows(0)
                End If

                ChargerKpis(row)

                Dim dtDetail As DataTable = venteService.ListerVentesParPeriode(debut, finInclusive.AddDays(1))
                gridDetailVentes.DataSource = dtDetail
                ConfigurerGrilleDetail()

                ChargerContexte()
            Catch ex As Exception
                MessageBox.Show("Impossible de charger l'analyse ventes : " & ex.Message, "Analyse ventes", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub ChargerDetailSiBesoin(sender As Object, e As EventArgs)
            If tabs.SelectedTab IsNot Nothing AndAlso tabs.SelectedTab.Text = "Détail ventes" Then
                ChargerDonnees(Nothing, EventArgs.Empty)
            End If
        End Sub

        Private Sub ChargerKpis(row As DataRow)
            _cibleValeurStockEntree = LireDecimal(row, "ValeurStockEntree")
            _cibleCoutMarchandisesVendues = LireDecimal(row, "CoutMarchandisesVendues")
            _cibleChiffreAffaires = LireDecimal(row, "ChiffreAffaires")
            _cibleBeneficesRealise = LireDecimal(row, "BeneficeRealise")
            _cibleBeneficeNetRealise = LireDecimal(row, "BeneficeNetRealise")
            _cibleCoutStockRestant = LireDecimal(row, "CoutStockRestant")
            _cibleProjectionBeneficeRestant = LireDecimal(row, "ProjectionBeneficeRestant")
            _cibleMargeBeneficiairePourcentage = LireDecimal(row, "MargeBeneficiairePourcentage")
            _cibleDepensesTotal = LireDecimal(row, "DepensesTotal")
            _cibleChargesSortiesManuelles = LireDecimal(row, "ChargesSortiesManuelles")

            Dim evaluation As String = LireTexte(row, "Evaluation")
            lblEvaluationValue.Text = If(String.IsNullOrWhiteSpace(evaluation), "-", evaluation)
            AppliquerStyleEvaluation(lblEvaluationValue.Text)

            _courantValeurStockEntree = 0D
            _courantCoutMarchandisesVendues = 0D
            _courantChiffreAffaires = 0D
            _courantBeneficesRealise = 0D
            _courantBeneficeNetRealise = 0D
            _courantCoutStockRestant = 0D
            _courantProjectionBeneficeRestant = 0D
            _courantMargeBeneficiairePourcentage = 0D

            timerAnimation.Start()
        End Sub

        Private Sub RendreCarteCliquable(card As Control)
            If card Is Nothing Then
                Return
            End If

            card.Cursor = Cursors.Hand
            AddHandler card.Click, AddressOf OuvrirDetailsBeneficeNet

            For Each ctrl As Control In card.Controls
                ctrl.Cursor = Cursors.Hand
                AddHandler ctrl.Click, AddressOf OuvrirDetailsBeneficeNet
                For Each child As Control In ctrl.Controls
                    child.Cursor = Cursors.Hand
                    AddHandler child.Click, AddressOf OuvrirDetailsBeneficeNet
                Next
            Next
        End Sub

        Private Sub OuvrirDetailsBeneficeNet(sender As Object, e As EventArgs)
            Me.UseWaitCursor = True
            Try
                If _dateAnalyseDebut = Date.MinValue OrElse _dateAnalyseFin = Date.MinValue Then
                    Return
                End If

                Dim dtDetails As DataTable = rapportService.BeneficeNetDetails(_dateAnalyseDebut, _dateAnalyseFin)
                If dtDetails Is Nothing OrElse dtDetails.Rows.Count = 0 Then
                    MessageBox.Show("Aucun détail de bénéfice net disponible pour cette période.", "Analyse ventes", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If

                Using frm As New FormulaireBeneficeNetDetails(_dateAnalyseDebut, _dateAnalyseFin, dtDetails, _cibleBeneficesRealise, _cibleDepensesTotal, _cibleChargesSortiesManuelles, _cibleBeneficeNetRealise)
                    frm.ShowDialog(Me)
                End Using
            Catch ex As Exception
                MessageBox.Show("Impossible d'ouvrir le détail du bénéfice net : " & ex.Message, "Analyse ventes", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                Me.UseWaitCursor = False
            End Try
        End Sub


        Private Sub OuvrirFormulaireVentes(sender As Object, e As EventArgs)
            Using frm As New FormulaireVente()
                frm.ShowDialog(Me)
            End Using
        End Sub

        Private Sub TimerAnimation_Tick(sender As Object, e As EventArgs)
            Dim step1 As Decimal = 0.05D ' Vitesse de l'animation

            _courantValeurStockEntree = Math.Min(_courantValeurStockEntree + (_cibleValeurStockEntree * step1), _cibleValeurStockEntree)
            _courantCoutMarchandisesVendues = Math.Min(_courantCoutMarchandisesVendues + (_cibleCoutMarchandisesVendues * step1), _cibleCoutMarchandisesVendues)
            _courantChiffreAffaires = Math.Min(_courantChiffreAffaires + (_cibleChiffreAffaires * step1), _cibleChiffreAffaires)
            _courantBeneficesRealise = Math.Min(_courantBeneficesRealise + (_cibleBeneficesRealise * step1), _cibleBeneficesRealise)
            _courantBeneficeNetRealise = Math.Min(_courantBeneficeNetRealise + (_cibleBeneficeNetRealise * step1), _cibleBeneficeNetRealise)
            _courantCoutStockRestant = Math.Min(_courantCoutStockRestant + (_cibleCoutStockRestant * step1), _cibleCoutStockRestant)
            _courantProjectionBeneficeRestant = Math.Min(_courantProjectionBeneficeRestant + (_cibleProjectionBeneficeRestant * step1), _cibleProjectionBeneficeRestant)
            _courantMargeBeneficiairePourcentage = Math.Min(_courantMargeBeneficiairePourcentage + (_cibleMargeBeneficiairePourcentage * step1), _cibleMargeBeneficiairePourcentage)

            lblValeurStockEntree.Text = _courantValeurStockEntree.ToString("N0") & " FC"
            lblCoutMarchandisesVendues.Text = _courantCoutMarchandisesVendues.ToString("N0") & " FC"
            lblChiffreAffaires.Text = _courantChiffreAffaires.ToString("N0") & " FC"
            lblBeneficesRealise.Text = _courantBeneficesRealise.ToString("N0") & " FC"
            lblBeneficeNetRealise.Text = _courantBeneficeNetRealise.ToString("N0") & " FC"
            lblCoutStockRestant.Text = _courantCoutStockRestant.ToString("N0") & " FC"
            lblProjectionBeneficeRestant.Text = _courantProjectionBeneficeRestant.ToString("N0") & " FC"
            lblMargeBeneficiairePourcentage.Text = _courantMargeBeneficiairePourcentage.ToString("N1") & "%"

            If _courantValeurStockEntree >= _cibleValeurStockEntree AndAlso
               _courantCoutMarchandisesVendues >= _cibleCoutMarchandisesVendues AndAlso
               _courantChiffreAffaires >= _cibleChiffreAffaires AndAlso
               _courantBeneficesRealise >= _cibleBeneficesRealise AndAlso
               _courantBeneficeNetRealise >= _cibleBeneficeNetRealise AndAlso
               _courantCoutStockRestant >= _cibleCoutStockRestant AndAlso
               _courantProjectionBeneficeRestant >= _cibleProjectionBeneficeRestant AndAlso
               _courantMargeBeneficiairePourcentage >= _cibleMargeBeneficiairePourcentage Then
                timerAnimation.Stop()
            End If
        End Sub
        Private Sub TimerFade_Tick(sender As Object, e As EventArgs)
            If Me.Opacity < 1 Then
                Me.Opacity += 0.1
            Else
                timerFade.Stop()
            End If
        End Sub

        Private Sub SetSelectedTab(index As Integer)
            tabs.SelectedIndex = index
            ' Mettre à jour le style des boutons d'onglet
            btnTabSynthese.BackColor = If(index = 0, ColorTabActive, ColorTabInactive)
            btnTabSynthese.ForeColor = If(index = 0, ColorPrimary, ColorTextSecondary)
            btnTabSynthese.FlatAppearance.BorderSize = If(index = 0, 0, 0)
            btnTabSynthese.FlatAppearance.BorderColor = If(index = 0, ColorPrimary, ColorBorder)

            btnTabDetail.BackColor = If(index = 1, ColorTabActive, ColorTabInactive)
            btnTabDetail.ForeColor = If(index = 1, ColorPrimary, ColorTextSecondary)
            btnTabDetail.FlatAppearance.BorderSize = If(index = 1, 0, 0)
            btnTabDetail.FlatAppearance.BorderColor = If(index = 1, ColorPrimary, ColorBorder)

            ' Ajouter une bordure inférieure pour l'onglet actif pour un meilleur visuel
            If index = 0 Then
                btnTabSynthese.FlatAppearance.BorderSize = 0
                'btnTabSynthese.FlatAppearance.BorderColor = Color.Transparent
                btnTabSynthese.Tag = "Active"
                btnTabDetail.Tag = "Inactive"
            Else
                btnTabDetail.FlatAppearance.BorderSize = 0
                ' btnTabDetail.FlatAppearance.BorderColor = Color.Transparent
                btnTabDetail.Tag = "Active"
                btnTabSynthese.Tag = "Inactive"
            End If

            btnTabSynthese.Invalidate()
            btnTabDetail.Invalidate()
        End Sub

        Private Sub TabButton_Paint(sender As Object, e As PaintEventArgs)
            Dim btn As Button = DirectCast(sender, Button)
            If btn.Tag IsNot Nothing AndAlso btn.Tag.ToString() = "Active" Then
                Using p As New Pen(ColorPrimary, 2)
                    e.Graphics.DrawLine(p, 0, btn.Height - 1, btn.Width, btn.Height - 1)
                End Using
            End If
        End Sub

        ' --- FONCTIONS DE CRÉATION DE CARTES ---
        Private Function CreerCarteKpi(titre As String, couleurAccent As Color, ByRef valeurLabel As Label) As Panel
            Dim cardPanel As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorCardBg,
                .Margin = New Padding(8),
                .Padding = New Padding(16),
                .BorderStyle = BorderStyle.None ' Supprimer la bordure par défaut
            }
            cardPanel.Tag = couleurAccent ' Pour réutiliser la couleur si besoin

            ' Dessiner une bordure plus douce et une ombre
            AddHandler cardPanel.Paint, Sub(s, ev) DessinerCarteBordureOmbre(s, ev, cardPanel)

            Dim layout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2
            }
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            layout.BackColor = Color.Transparent

            Dim lblTitre As New Label() With {
                .Text = titre,
                .Font = FontLabel,
                .ForeColor = ColorTextSecondary,
                .AutoSize = True,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft
            }

            valeurLabel = New Label() With {
                .Text = "0 FC", ' Valeur initiale
                .Font = FontValue,
                .ForeColor = couleurAccent,
                .AutoSize = True,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleRight
            }

            layout.Controls.Add(lblTitre, 0, 0)
            layout.Controls.Add(valeurLabel, 0, 1)
            cardPanel.Controls.Add(layout)

            Return cardPanel
        End Function

        Private Function CreerCarteTexte(titre As String, couleurAccent As Color, ByRef valeurLabel As Label) As Panel
            Dim cardPanel As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorCardBg,
                .Margin = New Padding(8),
                .Padding = New Padding(16),
                .BorderStyle = BorderStyle.None ' Supprimer la bordure par défaut
            }
            AddHandler cardPanel.Paint, Sub(s, ev) DessinerCarteBordureOmbre(s, ev, cardPanel)

            Dim layout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2
            }
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            layout.BackColor = Color.Transparent

            Dim lblTitre As New Label() With {
                .Text = titre,
                .Font = FontLabel,
                .ForeColor = ColorTextSecondary,
                .AutoSize = True,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft
            }

            valeurLabel = New Label() With {
                .Text = "-", ' Valeur initiale
                .Font = FontValueSmall,
                .ForeColor = ColorTextPrimary,
                .AutoSize = True,
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleRight
            }

            layout.Controls.Add(lblTitre, 0, 0)
            layout.Controls.Add(valeurLabel, 0, 1)
            cardPanel.Controls.Add(layout)

            Return cardPanel
        End Function

        Private Sub DessinerCarteBordureOmbre(sender As Object, e As PaintEventArgs, pnl As Panel)
            Dim rect As New Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1)
            Using pen As New Pen(ColorBorder, 1)
                e.Graphics.DrawRectangle(pen, rect)
            End Using

            ' Ombre très légère (peut être plus complexe à simuler parfaitement en WinForms sans GDI+ avancé)
            ' Pour l'instant, une bordure suffit pour l'aspect "carte"
            Using shadowBrush As New SolidBrush(Color.FromArgb(20, 0, 0, 0)) ' Très légère ombre
                e.Graphics.FillRectangle(shadowBrush, pnl.Width - 3, 3, 3, pnl.Height - 3)
                e.Graphics.FillRectangle(shadowBrush, 3, pnl.Height - 3, pnl.Width - 3, 3)
            End Using
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
                .Font = New Font("Segoe UI", 9.5F),
                .GridColor = Color.FromArgb(220, 224, 229)
            }
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = ColorTextPrimary
            dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(245, 245, 245)
            dgv.ColumnHeadersHeight = 38
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 234, 246)
            dgv.DefaultCellStyle.SelectionForeColor = ColorTextPrimary
            Return dgv
        End Function

        Private Sub ConfigurerGrilleDetail()
            If gridDetailVentes.Columns.Count = 0 Then
                Return
            End If

            ConfigurerColonne(gridDetailVentes, "DateVente", "Date vente", 150, "dd/MM/yyyy HH:mm")
            ConfigurerColonne(gridDetailVentes, "Produit", "Produit", 240)
            ConfigurerColonne(gridDetailVentes, "PrixAchatCarton", "Prix achat carton (FC)", 150, "N0")
            ConfigurerColonne(gridDetailVentes, "QuantiteVenduePieces", "Quantité vendue (pièces)", 150, "N0")
            ConfigurerColonne(gridDetailVentes, "MontantGenere", "Montant généré (FC)", 170, "N0")
            ConfigurerColonne(gridDetailVentes, "Benefice", "Bénéfice (FC)", 140, "N0")
        End Sub

        Private Sub ConfigurerColonne(grid As DataGridView, nom As String, titre As String, largeur As Integer, Optional format As String = Nothing)
            If Not grid.Columns.Contains(nom) Then
                Return
            End If

            Dim col As DataGridViewColumn = grid.Columns(nom)
            col.HeaderText = titre
            col.Width = largeur
            If Not String.IsNullOrWhiteSpace(format) Then
                col.DefaultCellStyle.Format = format
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            End If
        End Sub

        Private Sub AppliquerStyleEvaluation(evaluation As String)
            panelEvaluationCard.BackColor = ColorCardBg

            If String.IsNullOrWhiteSpace(evaluation) OrElse evaluation.Trim() = "-" Then
                lblEvaluationValue.ForeColor = ColorTextSecondary
                Return
            End If

            Dim texte As String = evaluation.ToUpperInvariant()
            Dim couleurFond As Color

            If texte.Contains("CRITIQUE") OrElse texte.Contains("PERTE") Then
                couleurFond = ColorDanger
            ElseIf texte.Contains("POINT MORT") Then
                couleurFond = Color.FromArgb(96, 125, 139)
            ElseIf texte.Contains("FAIBLE") Then
                couleurFond = ColorWarning
            ElseIf texte.Contains("PROGRÈS") OrElse texte.Contains("PROGRES") Then
                couleurFond = Color.FromArgb(255, 152, 0)
            Else
                couleurFond = ColorSuccess
            End If

            lblEvaluationValue.ForeColor = couleurFond
        End Sub

        Private Sub CalculerPeriode(ByRef debut As Date, ByRef finInclusive As Date)
            Dim annee As Integer = LireAnneeSelectionnee()
            Dim mois As Integer = LireMoisSelectionne()

            If String.Equals(Convert.ToString(cmbPeriode.SelectedItem), "Annuel", StringComparison.OrdinalIgnoreCase) Then
                debut = New Date(annee, 1, 1)
                finInclusive = New Date(annee, 12, 31)
            Else
                debut = New Date(annee, mois, 1)
                finInclusive = debut.AddMonths(1).AddDays(-1)
            End If
        End Sub

        Private Function LireMoisSelectionne() As Integer
            Dim item As MoisItem = TryCast(cmbMois.SelectedItem, MoisItem)
            If item IsNot Nothing Then
                Return item.Numero
            End If
            Return Date.Today.Month
        End Function

        Private Function LireAnneeSelectionnee() As Integer
            Dim valeur As Integer
            If Integer.TryParse(Convert.ToString(cmbAnnee.SelectedItem), valeur) Then
                Return valeur
            End If
            Return Date.Today.Year
        End Function

        Private Shared Function LireDecimal(row As DataRow, colonne As String) As Decimal
            If row Is Nothing OrElse row.Table Is Nothing OrElse Not row.Table.Columns.Contains(colonne) OrElse row.IsNull(colonne) Then
                Return 0D
            End If
            Return Convert.ToDecimal(row(colonne))
        End Function

        Private Shared Function LireTexte(row As DataRow, colonne As String) As String
            If row Is Nothing OrElse row.Table Is Nothing OrElse Not row.Table.Columns.Contains(colonne) OrElse row.IsNull(colonne) Then
                Return String.Empty
            End If
            Return Convert.ToString(row(colonne))
        End Function

        Private NotInheritable Class MoisItem
            Public ReadOnly Numero As Integer
            Public ReadOnly Libelle As String

            Public Sub New(numero As Integer, libelle As String)
                Me.Numero = numero
                Me.Libelle = libelle
            End Sub

            Public Overrides Function ToString() As String
                Return Libelle
            End Function
        End Class
    End Class
End Namespace
