Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting
Imports DevCommerc8ak.DevCommerc8ak.DTO
Imports DevCommerc8ak.DevCommerc8ak.Services

Namespace DevCommerc8ak
    Public Class FormulaireAnalyseCaissePhysique
        Inherits Form

        Private ReadOnly ColorBg As Color = Color.FromArgb(244, 247, 252)
        Private ReadOnly ColorCardBg As Color = Color.White
        Private ReadOnly ColorPrimary As Color = Color.FromArgb(42, 93, 155)
        Private ReadOnly ColorSuccess As Color = Color.FromArgb(46, 125, 50)
        Private ReadOnly ColorDanger As Color = Color.FromArgb(198, 40, 40)
        Private ReadOnly ColorWarning As Color = Color.FromArgb(245, 124, 0)
        Private ReadOnly ColorText As Color = Color.FromArgb(31, 41, 55)
        Private ReadOnly FontTitle As New Font("Segoe UI", 18.0F, FontStyle.Bold)
        Private ReadOnly FontLabel As New Font("Segoe UI", 9.5F, FontStyle.Regular)
        Private ReadOnly FontButton As New Font("Segoe UI", 9.5F, FontStyle.Bold)

        Private ReadOnly _service As AnalyseCaissePhysiqueService
        Private _isLoading As Boolean

        Private cmbPeriode As ComboBox
        Private dtpDate As DateTimePicker
        Private cmbMois As ComboBox
        Private cmbAnnee As ComboBox
        Private dtpDebut As DateTimePicker
        Private dtpFin As DateTimePicker
        Private txtUtilisateur As TextBox
        Private txtRole As TextBox
        Private cmbStatut As ComboBox
        Private btnActualiser As Button
        Private btnReset As Button
        Private btnRegulariser As Button

        Private tab As TabControl
        Private gridClotures As DataGridView
        Private gridSynthese As DataGridView
        Private gridHistorique As DataGridView
        Private lblKpisClotures As Label
        Private lblKpisSynthese As Label
        Private lblKpisAnalyse As Label
        Private chartEvolution As Chart
        Private chartStatuts As Chart
        Private chartUtilisateurs As Chart

        Public Sub New()
            Me.Text = "Analyse caisse physique"
            Me.BackColor = ColorBg
            Me.AutoScaleMode = AutoScaleMode.Dpi
            Me.AutoScroll = True
            Me.MinimumSize = New Size(1080, 720)
            Me.WindowState = FormWindowState.Maximized

            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            _service = New AnalyseCaissePhysiqueService(New AnalyseCaissePhysiqueRepository(New DAL(cs)))

            ConstruireInterface()
            AddHandler AppEvents.CaissePhysiqueModifiee, AddressOf RafraichirDepuisEvenement
            ChargerDonneesAsync()
        End Sub

        Private Sub ConstruireInterface()
            Dim main As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 3, .Padding = New Padding(20)}
            main.RowStyles.Add(New RowStyle(SizeType.Absolute, 70))
            main.RowStyles.Add(New RowStyle(SizeType.Absolute, 88))
            main.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim lblTitle As New Label() With {
                .Text = "Analyse caisse physique",
                .Dock = DockStyle.Fill,
                .Font = FontTitle,
                .ForeColor = ColorText,
                .TextAlign = ContentAlignment.MiddleLeft
            }
            main.Controls.Add(lblTitle, 0, 0)
            main.Controls.Add(CreerBarreFiltres(), 0, 1)

            tab = New TabControl() With {.Dock = DockStyle.Fill}
            tab.TabPages.Add(CreerOngletClotures())
            tab.TabPages.Add(CreerOngletSynthese())
            tab.TabPages.Add(CreerOngletHistorique())
            tab.TabPages.Add(CreerOngletAnalyse())
            main.Controls.Add(tab, 0, 2)
            Controls.Add(main)
        End Sub

        Private Function CreerBarreFiltres() As Control
            Dim panel As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .BackColor = ColorCardBg, .Padding = New Padding(10), .AutoScroll = True}
            cmbPeriode = CreerCombo(New String() {"Journalier", "Mensuel", "Annuel", "Personnalisé"})
            dtpDate = New DateTimePicker() With {.Width = 120, .Format = DateTimePickerFormat.Short}
            cmbMois = CreerCombo(New String() {"Janvier", "Février", "Mars", "Avril", "Mai", "Juin", "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"})
            cmbAnnee = CreerCombo(New String() {DateTime.Now.Year.ToString(), DateTime.Now.AddYears(-1).Year.ToString(), DateTime.Now.AddYears(1).Year.ToString()})
            dtpDebut = New DateTimePicker() With {.Width = 120, .Format = DateTimePickerFormat.Short}
            dtpFin = New DateTimePicker() With {.Width = 120, .Format = DateTimePickerFormat.Short}
            txtUtilisateur = New TextBox() With {.Width = 130}
            txtRole = New TextBox() With {.Width = 110}
            cmbStatut = CreerCombo(New String() {"Tous", "CONFORME", "MANQUANT", "SURPLUS", "A_VERIFIER", "JUSTIFIE", "A_REMBOURSER", "REMBOURSE", "RETENU_SUR_PAIE", "ANNULE"})
            btnActualiser = CreerBouton("Actualiser", ColorPrimary)
            btnReset = CreerBouton("Réinitialiser", ColorWarning)
            btnRegulariser = CreerBouton("Régulariser / détail", ColorDanger)

            cmbPeriode.SelectedIndex = 1
            cmbMois.SelectedIndex = DateTime.Now.Month - 1
            cmbAnnee.SelectedIndex = 0
            cmbStatut.SelectedIndex = 0
            AddHandler btnActualiser.Click, Sub() ChargerDonneesAsync()
            AddHandler btnReset.Click, AddressOf ReinitialiserFiltres
            AddHandler btnRegulariser.Click, AddressOf OuvrirRegularisation

            panel.Controls.AddRange(New Control() {
                New Label() With {.Text = "Période", .AutoSize = True, .Padding = New Padding(0, 7, 0, 0)}, cmbPeriode,
                New Label() With {.Text = "Date", .AutoSize = True, .Padding = New Padding(0, 7, 0, 0)}, dtpDate,
                New Label() With {.Text = "Mois", .AutoSize = True, .Padding = New Padding(0, 7, 0, 0)}, cmbMois,
                New Label() With {.Text = "Année", .AutoSize = True, .Padding = New Padding(0, 7, 0, 0)}, cmbAnnee,
                New Label() With {.Text = "Du", .AutoSize = True, .Padding = New Padding(0, 7, 0, 0)}, dtpDebut,
                New Label() With {.Text = "Au", .AutoSize = True, .Padding = New Padding(0, 7, 0, 0)}, dtpFin,
                New Label() With {.Text = "Utilisateur", .AutoSize = True, .Padding = New Padding(0, 7, 0, 0)}, txtUtilisateur,
                New Label() With {.Text = "Rôle", .AutoSize = True, .Padding = New Padding(0, 7, 0, 0)}, txtRole,
                cmbStatut, btnActualiser, btnReset, btnRegulariser
            })
            Return panel
        End Function

        Private Function CreerOngletClotures() As TabPage
            Dim page As New TabPage("Clôtures et écarts") With {.BackColor = ColorBg}
            Dim layout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2, .Padding = New Padding(10)}
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 70))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            lblKpisClotures = CreerKpiLabel()
            gridClotures = CreerGrille()
            AddHandler gridClotures.RowPrePaint, AddressOf ColorerLignesClotures
            layout.Controls.Add(lblKpisClotures, 0, 0)
            layout.Controls.Add(gridClotures, 0, 1)
            page.Controls.Add(layout)
            Return page
        End Function

        Private Function CreerOngletSynthese() As TabPage
            Dim page As New TabPage("Synthèse par utilisateur") With {.BackColor = ColorBg}
            Dim layout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2, .Padding = New Padding(10)}
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 70))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            lblKpisSynthese = CreerKpiLabel()
            gridSynthese = CreerGrille()
            layout.Controls.Add(lblKpisSynthese, 0, 0)
            layout.Controls.Add(gridSynthese, 0, 1)
            page.Controls.Add(layout)
            Return page
        End Function

        Private Function CreerOngletHistorique() As TabPage
            Dim page As New TabPage("Historique des statuts") With {.BackColor = ColorBg}
            gridHistorique = CreerGrille()
            gridHistorique.ReadOnly = True
            page.Controls.Add(gridHistorique)
            Return page
        End Function

        Private Function CreerOngletAnalyse() As TabPage
            Dim page As New TabPage("Analyse dynamique") With {.BackColor = ColorBg}
            Dim layout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 3, .Padding = New Padding(10)}
            layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 70))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
            lblKpisAnalyse = CreerKpiLabel()
            layout.Controls.Add(lblKpisAnalyse, 0, 0)
            layout.SetColumnSpan(lblKpisAnalyse, 2)
            chartEvolution = CreerChart("Évolution manquants / surplus", SeriesChartType.Line)
            chartStatuts = CreerChart("Répartition par statut", SeriesChartType.Pie)
            chartUtilisateurs = CreerChart("Écarts par utilisateur", SeriesChartType.Column)
            layout.Controls.Add(chartEvolution, 0, 1)
            layout.Controls.Add(chartStatuts, 1, 1)
            layout.Controls.Add(chartUtilisateurs, 0, 2)
            layout.SetColumnSpan(chartUtilisateurs, 2)
            page.Controls.Add(layout)
            Return page
        End Function

        Private Async Sub ChargerDonneesAsync()
            If _isLoading OrElse IsDisposed OrElse Disposing Then Return
            _isLoading = True
            Try
                Dim filtre As AnalyseCaissePhysiqueFiltreDTO = ConstruireFiltre()
                Dim clotures As DataTable = Await Task.Run(Function() _service.ListerClotures(filtre))
                Dim kpi As DataTable = Await Task.Run(Function() _service.ObtenirKpiClotures(filtre))
                Dim synthese As DataTable = Await Task.Run(Function() _service.ObtenirSyntheseParUtilisateur(filtre))
                Dim historique As DataTable = Await Task.Run(Function() _service.ListerHistoriqueStatuts(filtre))
                Dim evolution As DataTable = Await Task.Run(Function() _service.ObtenirEvolutionEcarts(filtre))
                Dim repartition As DataTable = Await Task.Run(Function() _service.ObtenirRepartitionStatuts(filtre))
                Dim utilisateurs As DataTable = Await Task.Run(Function() _service.ObtenirEcartsParUtilisateur(filtre))

                If IsDisposed OrElse Disposing Then Return
                gridClotures.DataSource = clotures
                gridSynthese.DataSource = synthese
                gridHistorique.DataSource = historique
                AfficherKpis(kpi, synthese)
                AfficherGraphiques(evolution, repartition, utilisateurs)
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireAnalyseCaissePhysique", "ChargerDonneesAsync", "Erreur chargement analyse caisse physique.", ex)
                MessageBox.Show("Impossible de charger l'analyse caisse physique : " & ex.Message, "Analyse caisse physique", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                _isLoading = False
            End Try
        End Sub

        Private Function ConstruireFiltre() As AnalyseCaissePhysiqueFiltreDTO
            Dim debut As DateTime
            Dim fin As DateTime
            Dim periode As String = If(cmbPeriode.SelectedItem Is Nothing, "Mensuel", Convert.ToString(cmbPeriode.SelectedItem))
            Dim annee As Integer = DateTime.Now.Year
            Integer.TryParse(Convert.ToString(cmbAnnee.SelectedItem), annee)
            Select Case periode
                Case "Journalier"
                    debut = dtpDate.Value.Date
                    fin = debut
                Case "Annuel"
                    debut = New DateTime(annee, 1, 1)
                    fin = New DateTime(annee, 12, 31)
                Case "Personnalisé"
                    debut = dtpDebut.Value.Date
                    fin = dtpFin.Value.Date
                Case Else
                    Dim mois As Integer = Math.Max(1, cmbMois.SelectedIndex + 1)
                    debut = New DateTime(annee, mois, 1)
                    fin = debut.AddMonths(1).AddDays(-1)
            End Select
            Return New AnalyseCaissePhysiqueFiltreDTO With {
                .DateDebut = debut,
                .DateFin = fin,
                .Utilisateur = txtUtilisateur.Text.Trim(),
                .RoleSession = txtRole.Text.Trim(),
                .Statut = If(cmbStatut.SelectedItem Is Nothing, "Tous", Convert.ToString(cmbStatut.SelectedItem))
            }
        End Function

        Private Sub AfficherKpis(kpi As DataTable, synthese As DataTable)
            Dim row As DataRow = If(kpi IsNot Nothing AndAlso kpi.Rows.Count > 0, kpi.Rows(0), Nothing)
            Dim total As Integer = LireInt(row, "NombreClotures")
            Dim manquants As Decimal = LireDecimal(row, "TotalManquants")
            Dim surplus As Decimal = LireDecimal(row, "TotalSurplus")
            Dim conformes As Integer = LireInt(row, "NombreConformes")
            Dim taux As Decimal = LireDecimal(row, "TauxConformite")

            lblKpisClotures.Text = "Clôtures : " & total.ToString("N0") &
                "    Théorique : " & LireDecimal(row, "TotalTheoriqueFC").ToString("N0") & " FC" &
                "    Physique : " & LireDecimal(row, "TotalPhysiqueFC").ToString("N0") & " FC" &
                "    Manquants : " & manquants.ToString("N0") & " FC" &
                "    Surplus : " & surplus.ToString("N0") & " FC" &
                "    Conformes : " & conformes.ToString("N0") &
                "    Taux : " & taux.ToString("N2") & " %"

            Dim utilisateurs As Integer = If(synthese Is Nothing, 0, synthese.Rows.Count)
            lblKpisSynthese.Text = "Utilisateurs : " & utilisateurs.ToString("N0") &
                "    Total manquants : " & manquants.ToString("N0") & " FC" &
                "    Total surplus : " & surplus.ToString("N0") & " FC" &
                "    Écart net : " & (surplus - manquants).ToString("N0") & " FC" &
                "    Taux conformité : " & taux.ToString("N2") & " %"

            lblKpisAnalyse.Text = "Total clôtures : " & total.ToString("N0") &
                "    Taux conformité : " & taux.ToString("N2") & " %" &
                "    Manquants : " & manquants.ToString("N0") & " FC" &
                "    Surplus : " & surplus.ToString("N0") & " FC" &
                "    Net : " & (surplus - manquants).ToString("N0") & " FC"
        End Sub

        Private Sub AfficherGraphiques(evolution As DataTable, repartition As DataTable, utilisateurs As DataTable)
            RemplirChartDeuxSeries(chartEvolution, evolution, "DateCaisse", "Manquants", "Surplus")
            RemplirChartSimple(chartStatuts, repartition, "Statut", "Total", SeriesChartType.Pie)
            RemplirChartDeuxSeries(chartUtilisateurs, utilisateurs, "Utilisateur", "Manquants", "Surplus")
        End Sub

        Private Sub RemplirChartSimple(chart As Chart, dt As DataTable, xCol As String, yCol As String, type As SeriesChartType)
            Dim serie As Series = PreparerSerie(chart, "SeriePrincipale", type)
            If serie Is Nothing Then Return
            serie.Points.Clear()
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                chart.Titles.Clear()
                chart.Titles.Add("Aucune donnée")
                Return
            End If
            For Each row As DataRow In dt.Rows
                serie.Points.AddXY(Convert.ToString(row(xCol)), LireDecimal(row, yCol))
            Next
        End Sub

        Private Sub RemplirChartDeuxSeries(chart As Chart, dt As DataTable, xCol As String, y1 As String, y2 As String)
            Dim s1 As Series = PreparerSerie(chart, y1, SeriesChartType.Column)
            Dim s2 As Series = PreparerSerie(chart, y2, SeriesChartType.Column)
            If s1 Is Nothing OrElse s2 Is Nothing Then Return
            s1.Points.Clear()
            s2.Points.Clear()
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                chart.Titles.Clear()
                chart.Titles.Add("Aucune donnée")
                Return
            End If
            For Each row As DataRow In dt.Rows
                Dim axe As String = Convert.ToString(row(xCol))
                s1.Points.AddXY(axe, LireDecimal(row, y1))
                s2.Points.AddXY(axe, LireDecimal(row, y2))
            Next
        End Sub

        Private Function PreparerSerie(chart As Chart, nom As String, type As SeriesChartType) As Series
            If chart Is Nothing OrElse chart.IsDisposed OrElse chart.Disposing Then Return Nothing
            If chart.ChartAreas.Count = 0 Then chart.ChartAreas.Add(New ChartArea("MainArea"))
            If chart.Legends.Count = 0 Then chart.Legends.Add(New Legend("MainLegend"))
            Dim serie As Series = chart.Series.FindByName(nom)
            If serie Is Nothing Then
                serie = New Series(nom)
                chart.Series.Add(serie)
            End If
            serie.ChartType = type
            serie.ChartArea = chart.ChartAreas(0).Name
            serie.Legend = chart.Legends(0).Name
            serie.IsValueShownAsLabel = False
            Return serie
        End Function

        Private Sub OuvrirRegularisation(sender As Object, e As EventArgs)
            If gridClotures Is Nothing OrElse gridClotures.CurrentRow Is Nothing Then
                MessageBox.Show("Sélectionnez une clôture à consulter ou régulariser.", "Analyse caisse physique", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Dim id As Integer = LireIntDepuisGrille(gridClotures.CurrentRow, "ClotureCaisseId")
            If id <= 0 Then Return

            Using frm As New Form()
                frm.Text = "Régularisation écart caisse"
                frm.StartPosition = FormStartPosition.CenterParent
                frm.FormBorderStyle = FormBorderStyle.FixedDialog
                frm.ClientSize = New Size(520, 330)
                frm.BackColor = ColorBg
                Dim layout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 7, .Padding = New Padding(18)}
                layout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150))
                layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                Dim cmbNewStatut As ComboBox = CreerCombo(New String() {"JUSTIFIE", "A_REMBOURSER", "REMBOURSE", "RETENU_SUR_PAIE", "CONFORME", "ANNULE"})
                Dim txtMontant As New TextBox() With {.Dock = DockStyle.Fill, .Text = "0"}
                Dim txtMode As New TextBox() With {.Dock = DockStyle.Fill}
                Dim txtReference As New TextBox() With {.Dock = DockStyle.Fill}
                Dim txtMotif As New TextBox() With {.Dock = DockStyle.Fill}
                Dim txtObservation As New TextBox() With {.Dock = DockStyle.Fill, .Multiline = True, .ScrollBars = ScrollBars.Vertical}
                AjouterLigne(layout, 0, "Nouveau statut", cmbNewStatut)
                AjouterLigne(layout, 1, "Montant régularisé", txtMontant)
                AjouterLigne(layout, 2, "Mode", txtMode)
                AjouterLigne(layout, 3, "Référence", txtReference)
                AjouterLigne(layout, 4, "Motif", txtMotif)
                AjouterLigne(layout, 5, "Observation", txtObservation)
                Dim pnlActions As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft}
                Dim btnOk As Button = CreerBouton("Enregistrer", ColorSuccess)
                Dim btnCancel As Button = CreerBouton("Annuler", ColorWarning)
                pnlActions.Controls.AddRange(New Control() {btnOk, btnCancel})
                layout.Controls.Add(pnlActions, 0, 6)
                layout.SetColumnSpan(pnlActions, 2)
                frm.Controls.Add(layout)
                AddHandler btnCancel.Click, Sub() frm.DialogResult = DialogResult.Cancel
                AddHandler btnOk.Click,
                    Sub()
                        Try
                            Dim montant As Decimal = 0D
                            Decimal.TryParse(txtMontant.Text.Trim().Replace(","c, "."c), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, montant)
                            _service.RegulariserCloture(New RegularisationCaissePhysiqueDTO With {
                                .ClotureCaisseId = id,
                                .NouveauStatut = Convert.ToString(cmbNewStatut.SelectedItem),
                                .MontantRegularise = montant,
                                .ModeRegularisation = txtMode.Text.Trim(),
                                .Reference = txtReference.Text.Trim(),
                                .Motif = txtMotif.Text.Trim(),
                                .Observation = txtObservation.Text.Trim()
                            })
                            frm.DialogResult = DialogResult.OK
                        Catch ex As Exception
                            Dim log As New ProductionLogService()
                            log.Error("FormulaireAnalyseCaissePhysique", "OuvrirRegularisation", "Erreur lors de la régularisation.", ex)
                            MessageBox.Show(frm, "Impossible d'enregistrer la régularisation : " & ex.Message, "Analyse caisse physique", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        End Try
                    End Sub
                If frm.ShowDialog(Me) = DialogResult.OK Then ChargerDonneesAsync()
            End Using
        End Sub

        Private Sub ReinitialiserFiltres(sender As Object, e As EventArgs)
            cmbPeriode.SelectedIndex = 1
            cmbMois.SelectedIndex = DateTime.Now.Month - 1
            cmbAnnee.SelectedIndex = 0
            cmbStatut.SelectedIndex = 0
            txtUtilisateur.Clear()
            txtRole.Clear()
            ChargerDonneesAsync()
        End Sub

        Private Sub RafraichirDepuisEvenement(sender As Object, e As EventArgs)
            If IsDisposed OrElse Disposing Then Return
            If InvokeRequired Then
                BeginInvoke(New MethodInvoker(Sub() RafraichirDepuisEvenement(Nothing, EventArgs.Empty)))
                Return
            End If
            ChargerDonneesAsync()
        End Sub

        Private Function CreerCombo(items As String()) As ComboBox
            Dim cmb As New ComboBox() With {.Width = 130, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmb.Items.AddRange(items)
            If cmb.Items.Count > 0 Then cmb.SelectedIndex = 0
            Return cmb
        End Function

        Private Function CreerBouton(texte As String, couleur As Color) As Button
            Dim btn As New Button() With {.Text = texte, .Width = 135, .Height = 30, .BackColor = couleur, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = FontButton}
            btn.FlatAppearance.BorderSize = 0
            Return btn
        End Function

        Private Function CreerGrille() As DataGridView
            Return New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None
            }
        End Function

        Private Function CreerKpiLabel() As Label
            Return New Label() With {.Dock = DockStyle.Fill, .BackColor = ColorCardBg, .ForeColor = ColorText, .Font = FontButton, .TextAlign = ContentAlignment.MiddleLeft, .Padding = New Padding(12)}
        End Function

        Private Function CreerChart(titre As String, type As SeriesChartType) As Chart
            Dim chart As New Chart() With {.Dock = DockStyle.Fill, .BackColor = Color.White}
            chart.Titles.Add(titre)
            chart.ChartAreas.Add(New ChartArea("MainArea"))
            chart.Legends.Add(New Legend("MainLegend"))
            chart.Series.Add(New Series("SeriePrincipale") With {.ChartType = type, .ChartArea = "MainArea", .Legend = "MainLegend"})
            Return chart
        End Function

        Private Sub AjouterLigne(layout As TableLayoutPanel, row As Integer, texte As String, ctrl As Control)
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, If(row = 5, 80, 38)))
            layout.Controls.Add(New Label() With {.Text = texte, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}, 0, row)
            layout.Controls.Add(ctrl, 1, row)
        End Sub

        Private Sub ColorerLignesClotures(sender As Object, e As DataGridViewRowPrePaintEventArgs)
            If gridClotures Is Nothing OrElse Not gridClotures.Columns.Contains("Statut") Then Return
            Dim row As DataGridViewRow = gridClotures.Rows(e.RowIndex)
            Dim statut As String = Convert.ToString(row.Cells("Statut").Value).ToUpperInvariant()
            Select Case statut
                Case "CONFORME"
                    row.DefaultCellStyle.BackColor = Color.FromArgb(232, 245, 233)
                Case "MANQUANT"
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 238)
                Case "SURPLUS"
                    row.DefaultCellStyle.BackColor = Color.FromArgb(232, 244, 253)
                Case "A_VERIFIER"
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 225)
                Case "ANNULE"
                    row.DefaultCellStyle.BackColor = Color.FromArgb(238, 238, 238)
            End Select
        End Sub

        Private Shared Function LireDecimal(row As DataRow, colonne As String) As Decimal
            If row Is Nothing OrElse row.Table Is Nothing OrElse Not row.Table.Columns.Contains(colonne) OrElse row(colonne) Is DBNull.Value Then Return 0D
            Dim valeur As Decimal
            Decimal.TryParse(Convert.ToString(row(colonne)).Replace(","c, "."c), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, valeur)
            Return valeur
        End Function

        Private Shared Function LireInt(row As DataRow, colonne As String) As Integer
            If row Is Nothing OrElse row.Table Is Nothing OrElse Not row.Table.Columns.Contains(colonne) OrElse row(colonne) Is DBNull.Value Then Return 0
            Dim valeur As Integer
            Integer.TryParse(Convert.ToString(row(colonne)), valeur)
            Return valeur
        End Function

        Private Shared Function LireIntDepuisGrille(row As DataGridViewRow, colonne As String) As Integer
            If row Is Nothing OrElse Not row.DataGridView.Columns.Contains(colonne) Then Return 0
            Dim valeur As Integer
            Integer.TryParse(Convert.ToString(row.Cells(colonne).Value), valeur)
            Return valeur
        End Function

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            RemoveHandler AppEvents.CaissePhysiqueModifiee, AddressOf RafraichirDepuisEvenement
            MyBase.OnFormClosed(e)
        End Sub
    End Class
End Namespace
