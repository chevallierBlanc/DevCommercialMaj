Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireAnalyseVente
        Inherits Form

        Private Shared ReadOnly MoisFrancais As String() = {
            "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
            "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"
        }

        Private ReadOnly ColorBg As Color = Color.FromArgb(244, 247, 252)
        Private ReadOnly ColorHeader As Color = Color.FromArgb(28, 35, 49)
        Private ReadOnly ColorCardBg As Color = Color.White
        Private ReadOnly ColorPrimary As Color = Color.FromArgb(41, 128, 185)
        Private ReadOnly ColorAccent As Color = Color.FromArgb(103, 58, 183)
        Private ReadOnly ColorSuccess As Color = Color.FromArgb(46, 125, 50)
        Private ReadOnly ColorDanger As Color = Color.FromArgb(198, 40, 40)
        Private ReadOnly ColorWarning As Color = Color.FromArgb(245, 124, 0)
        Private ReadOnly ColorTextPrimary As Color = Color.FromArgb(31, 41, 55)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(107, 114, 128)

        Private ReadOnly FontTitle As New Font("Segoe UI", 18.0F, FontStyle.Bold)
        Private ReadOnly FontSubtitle As New Font("Segoe UI", 10.0F)
        Private ReadOnly FontLabel As New Font("Segoe UI", 9.5F, FontStyle.Bold)
        Private ReadOnly FontValue As New Font("Segoe UI", 20.0F, FontStyle.Bold)
        Private ReadOnly FontValueSmall As New Font("Segoe UI", 13.0F, FontStyle.Bold)
        Private ReadOnly FontButton As New Font("Segoe UI", 10.0F, FontStyle.Bold)

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
        Private ReadOnly lblEvaluationValue As Label

        Private ReadOnly timerAnimation As Timer
        Private ReadOnly timerFade As Timer

        Private lblValeurStockEntree As Label
        Private lblCoutMarchandisesVendues As Label
        Private lblChiffreAffaires As Label
        Private lblBeneficesRealise As Label
        Private lblCoutStockRestant As Label
        Private lblProjectionBeneficeRestant As Label
        Private lblMargeBeneficiairePourcentage As Label

        Private _cibleValeurStockEntree As Decimal
        Private _cibleCoutMarchandisesVendues As Decimal
        Private _cibleChiffreAffaires As Decimal
        Private _cibleBeneficesRealise As Decimal
        Private _cibleCoutStockRestant As Decimal
        Private _cibleProjectionBeneficeRestant As Decimal
        Private _cibleMargeBeneficiairePourcentage As Decimal

        Private _courantValeurStockEntree As Decimal
        Private _courantCoutMarchandisesVendues As Decimal
        Private _courantChiffreAffaires As Decimal
        Private _courantBeneficesRealise As Decimal
        Private _courantCoutStockRestant As Decimal
        Private _courantProjectionBeneficeRestant As Decimal
        Private _courantMargeBeneficiairePourcentage As Decimal

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

            Dim pnlHeader As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 96,
                .BackColor = ColorHeader,
                .Padding = New Padding(24, 16, 24, 16)
            }

            Dim lblTitre As New Label() With {
                .Text = "Dashboard Analyse Vente",
                .Font = FontTitle,
                .ForeColor = Color.White,
                .AutoSize = True,
                .Left = 24,
                .Top = 14
            }
            Dim lblSousTitre As New Label() With {
                .Text = "Lecture des ventes, rentabilité, coût de stock restant et projection de bénéfice.",
                .Font = FontSubtitle,
                .ForeColor = Color.FromArgb(220, 230, 245),
                .AutoSize = True,
                .Left = 26,
                .Top = 52
            }
            pnlHeader.Controls.Add(lblTitre)
            pnlHeader.Controls.Add(lblSousTitre)

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

            Dim pnlActions As New FlowLayoutPanel() With {
                .Dock = DockStyle.Right,
                .Width = 280,
                .Height = 50,
                .BackColor = Color.Transparent,
                .FlowDirection = FlowDirection.RightToLeft,
                .WrapContents = False,
                .Padding = New Padding(0, 24, 0, 0)
            }
            pnlActions.Controls.Add(btnOuvrirVentes)
            pnlActions.Controls.Add(btnActualiser)

            pnlHeader.Controls.Add(pnlActions)

            Dim pnlFiltres As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 88,
                .BackColor = ColorCardBg,
                .Padding = New Padding(16),
                .BorderStyle = BorderStyle.FixedSingle
            }

            pnlFiltres.Controls.Add(New Label() With {.Text = "Période", .Left = 16, .Top = 14, .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary})
            cmbPeriode = New ComboBox() With {
                .Left = 16,
                .Top = 36,
                .Width = 120,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)
            }
            cmbPeriode.Items.AddRange(New Object() {"Mensuel", "Annuel"})

            pnlFiltres.Controls.Add(New Label() With {.Text = "Mois", .Left = 160, .Top = 14, .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary})
            cmbMois = New ComboBox() With {
                .Left = 160,
                .Top = 36,
                .Width = 170,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)
            }

            pnlFiltres.Controls.Add(New Label() With {.Text = "Année", .Left = 350, .Top = 14, .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary})
            cmbAnnee = New ComboBox() With {
                .Left = 350,
                .Top = 36,
                .Width = 100,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)
            }

            lblContexte = New Label() With {
                .Left = 480,
                .Top = 35,
                .Width = 800,
                .Height = 26,
                .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
                .ForeColor = ColorTextPrimary,
                .Text = "Période analysée : -"
            }

            pnlFiltres.Controls.Add(cmbPeriode)
            pnlFiltres.Controls.Add(cmbMois)
            pnlFiltres.Controls.Add(cmbAnnee)
            pnlFiltres.Controls.Add(lblContexte)

            tabs = New TabControl() With {.Dock = DockStyle.Fill, .Padding = New Point(12, 6)}
            tabSynthese = New TabPage("Synthèse") With {.BackColor = ColorBg, .Padding = New Padding(16)}
            tabDetail = New TabPage("Détail ventes") With {.BackColor = ColorBg, .Padding = New Padding(16)}
            tabs.TabPages.Add(tabSynthese)
            tabs.TabPages.Add(tabDetail)

            Dim pnlSynthese As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorBg}

            Dim tableKpi As New TableLayoutPanel() With {
                .Dock = DockStyle.Top,
                .Height = 380,
                .ColumnCount = 3,
                .RowCount = 3,
                .BackColor = ColorBg,
                .Padding = New Padding(0)
            }
            tableKpi.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.3333F))
            tableKpi.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.3333F))
            tableKpi.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 33.3333F))
            tableKpi.RowStyles.Add(New RowStyle(SizeType.Percent, 33.3333F))
            tableKpi.RowStyles.Add(New RowStyle(SizeType.Percent, 33.3333F))
            tableKpi.RowStyles.Add(New RowStyle(SizeType.Percent, 33.3333F))

            tableKpi.Controls.Add(CreerCarteKpi("Valeur stock entrée", ColorPrimary, lblValeurStockEntree), 0, 0)
            tableKpi.Controls.Add(CreerCarteKpi("Coût marchandises vendues", ColorAccent, lblCoutMarchandisesVendues), 1, 0)
            tableKpi.Controls.Add(CreerCarteKpi("Chiffre d'affaires", ColorSuccess, lblChiffreAffaires), 2, 0)
            tableKpi.Controls.Add(CreerCarteKpi("Bénéfice réalisé", ColorSuccess, lblBeneficesRealise), 0, 1)
            tableKpi.Controls.Add(CreerCarteKpi("Coût stock restant", ColorWarning, lblCoutStockRestant), 1, 1)
            tableKpi.Controls.Add(CreerCarteKpi("Projection bénéfice restant", ColorPrimary, lblProjectionBeneficeRestant), 2, 1)
            tableKpi.Controls.Add(CreerCarteKpi("Marge bénéficiaire", ColorAccent, lblMargeBeneficiairePourcentage), 0, 2)
            panelEvaluationCard = CreerCarteTexte("Évaluation", Color.FromArgb(76, 175, 80), lblEvaluationValue)
            tableKpi.Controls.Add(panelEvaluationCard, 1, 2)
            tableKpi.SetColumnSpan(panelEvaluationCard, 2)

            Dim lblNote As New Label() With {
                .Text = "Les valeurs sont affichées sans décimales inutiles. Les montants sont en FC.",
                .Dock = DockStyle.Top,
                .Height = 30,
                .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic),
                .ForeColor = ColorTextSecondary,
                .Padding = New Padding(2, 6, 0, 0)
            }

            pnlSynthese.Controls.Add(lblNote)
            pnlSynthese.Controls.Add(tableKpi)

            gridDetailVentes = CreerGrille()
            gridDetailVentes.Dock = DockStyle.Fill
            tabDetail.Controls.Add(gridDetailVentes)

            tabSynthese.Controls.Add(pnlSynthese)

            Me.Controls.Add(tabs)
            Me.Controls.Add(pnlFiltres)
            Me.Controls.Add(pnlHeader)

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
            _cibleCoutStockRestant = LireDecimal(row, "CoutStockRestant")
            _cibleProjectionBeneficeRestant = LireDecimal(row, "ProjectionBeneficeRestant")
            _cibleMargeBeneficiairePourcentage = LireDecimal(row, "MargeBeneficiairePourcentage")

            Dim evaluation As String = LireTexte(row, "Evaluation")
            lblEvaluationValue.Text = If(String.IsNullOrWhiteSpace(evaluation), "-", evaluation)
            AppliquerStyleEvaluation(lblEvaluationValue.Text)

            _courantValeurStockEntree = 0D
            _courantCoutMarchandisesVendues = 0D
            _courantChiffreAffaires = 0D
            _courantBeneficesRealise = 0D
            _courantCoutStockRestant = 0D
            _courantProjectionBeneficeRestant = 0D
            _courantMargeBeneficiairePourcentage = 0D

            timerAnimation.Start()
        End Sub

        Private Sub TimerAnimation_Tick(sender As Object, e As EventArgs)
            Dim termine As Boolean = True

            termine = AnimerValeur(lblValeurStockEntree, _courantValeurStockEntree, _cibleValeurStockEntree, AddressOf FormatageGlobal.FormatMontant) AndAlso termine
            termine = AnimerValeur(lblCoutMarchandisesVendues, _courantCoutMarchandisesVendues, _cibleCoutMarchandisesVendues, AddressOf FormatageGlobal.FormatMontant) AndAlso termine
            termine = AnimerValeur(lblChiffreAffaires, _courantChiffreAffaires, _cibleChiffreAffaires, AddressOf FormatageGlobal.FormatMontant) AndAlso termine
            termine = AnimerValeur(lblBeneficesRealise, _courantBeneficesRealise, _cibleBeneficesRealise, AddressOf FormatageGlobal.FormatMontant) AndAlso termine
            termine = AnimerValeur(lblCoutStockRestant, _courantCoutStockRestant, _cibleCoutStockRestant, AddressOf FormatageGlobal.FormatMontant) AndAlso termine
            termine = AnimerValeur(lblProjectionBeneficeRestant, _courantProjectionBeneficeRestant, _cibleProjectionBeneficeRestant, AddressOf FormatageGlobal.FormatMontant) AndAlso termine
            termine = AnimerValeur(lblMargeBeneficiairePourcentage, _courantMargeBeneficiairePourcentage, _cibleMargeBeneficiairePourcentage, AddressOf FormatageGlobal.FormatPourcentage) AndAlso termine

            If termine Then
                timerAnimation.Stop()
            End If
        End Sub

        Private Sub TimerFade_Tick(sender As Object, e As EventArgs)
            If Me.Opacity < 1.0R Then
                Me.Opacity = Math.Min(1.0R, Me.Opacity + 0.08R)
            Else
                timerFade.Stop()
            End If
        End Sub

        Private Function AnimerValeur(lbl As Label, ByRef courant As Decimal, objectif As Decimal, formateur As Func(Of Decimal, String)) As Boolean
            If Math.Abs(courant - objectif) <= 1D Then
                courant = objectif
                lbl.Text = formateur(courant)
                Return True
            End If

            Dim pas As Decimal = Math.Max(1D, Math.Abs(objectif) / 12D)
            If courant < objectif Then
                courant = Math.Min(objectif, courant + pas)
            Else
                courant = Math.Max(objectif, courant - pas)
            End If

            lbl.Text = formateur(courant)
            Return courant = objectif
        End Function

        Private Sub OuvrirFormulaireVentes(sender As Object, e As EventArgs)
            Using frm As New FormulaireVente()
                frm.ShowDialog(Me)
            End Using
        End Sub

        Private Function CreerCarteKpi(titre As String, couleur As Color, ByRef lblValeur As Label) As Panel
            Dim card As New Panel() With {
                .BackColor = ColorCardBg,
                .BorderStyle = BorderStyle.FixedSingle,
                .Dock = DockStyle.Fill,
                .Margin = New Padding(8),
                .Padding = New Padding(14)
            }

            Dim bande As New Panel() With {.Dock = DockStyle.Left, .Width = 6, .BackColor = couleur}
            Dim lblTitre As New Label() With {
                .Text = titre,
                .Dock = DockStyle.Top,
                .Height = 24,
                .Font = FontLabel,
                .ForeColor = ColorTextSecondary
            }
            lblValeur = New Label() With {
                .Text = "0 FC",
                .Dock = DockStyle.Fill,
                .Font = FontValue,
                .ForeColor = couleur,
                .TextAlign = ContentAlignment.MiddleLeft,
                .AutoEllipsis = True
            }

            card.Controls.Add(lblValeur)
            card.Controls.Add(lblTitre)
            card.Controls.Add(bande)
            Return card
        End Function

        Private Function CreerCarteTexte(titre As String, couleur As Color, ByRef lblValeur As Label) As Panel
            Dim card As New Panel() With {
                .BackColor = ColorCardBg,
                .BorderStyle = BorderStyle.FixedSingle,
                .Dock = DockStyle.Fill,
                .Margin = New Padding(8),
                .Padding = New Padding(14)
            }

            Dim bande As New Panel() With {.Dock = DockStyle.Left, .Width = 6, .BackColor = couleur}
            Dim lblTitre As New Label() With {
                .Text = titre,
                .Dock = DockStyle.Top,
                .Height = 24,
                .Font = FontLabel,
                .ForeColor = ColorTextSecondary
            }
            lblValeur = New Label() With {
                .Text = "-",
                .Dock = DockStyle.Fill,
                .Font = FontValueSmall,
                .ForeColor = couleur,
                .TextAlign = ContentAlignment.MiddleLeft,
                .AutoEllipsis = True
            }

            card.Controls.Add(lblValeur)
            card.Controls.Add(lblTitre)
            card.Controls.Add(bande)
            Return card
        End Function

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
