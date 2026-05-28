Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Configuration
Imports System.Drawing
Imports System.Drawing.Printing
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting
Imports DevCommerc8ak.DevCommerc8ak.DTO
Imports DevCommerc8ak.DevCommerc8ak.Services
Imports DevCommerc8ak.DevCommerc8ak.Finance

Namespace DevCommerc8ak
    Public Class FormulaireFinance1
        Inherits Form

        ' --- Services ---
        Private _depenseService As DepenseServiceFinance
        Private _caisseService As CaisseService
        Private _banqueService As BanqueService
        Private _catService As CategorieDepenseService

        ' --- Constantes de Design Windows 11 ---
        Private ReadOnly ColorPrimary As Color = Color.FromArgb(0, 120, 212)
        Private ReadOnly ColorPrimaryHeader As Color = Color.FromArgb(52, 73, 94)
        Private ReadOnly ColorBackground As Color = Color.FromArgb(245, 247, 250)
        Private ReadOnly ColorCard As Color = Color.White
        Private ReadOnly ColorText As Color = Color.FromArgb(32, 32, 32)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(102, 102, 102)
        Private ReadOnly ColorSuccess As Color = Color.FromArgb(16, 124, 16)
        Private ReadOnly ColorDanger As Color = Color.FromArgb(209, 52, 56)
        Private ReadOnly ColorWhite As Color = Color.White

        Private ReadOnly ColorDanger2 As Color = Color.FromArgb(192, 57, 43)
        Private ReadOnly ColorSecondary As Color = Color.FromArgb(41, 128, 185)

        Private ReadOnly FontTitle As New Font("Segoe UI", 18.0F, FontStyle.Bold)
        Private ReadOnly FontLabel As New Font("Segoe UI", 9.5F)
        Private ReadOnly FontValue As New Font("Segoe UI", 22.0F, FontStyle.Bold)
        Private ReadOnly FontControl As New Font("Segoe UI", 10.0F)

        ' --- Composants UI ---
        Private ReadOnly tabControlFinance As TabControl
        Private ReadOnly tpDepenses As TabPage
        Private ReadOnly tpCaisse As TabPage
        Private ReadOnly tpBanque As TabPage
        Private ReadOnly tpDashboard As TabPage

        ' Onglet Dépenses
        Private txtMontantDepense As TextBox
        Private cmbCategorieDepense As ComboBox
        Private btnAddCategorie As Button
        Private cmbDeviseDepense As ComboBox
        Private cmbSourceDepense As ComboBox
        Private cmbTypeDepense As ComboBox
        Private txtDescriptionDepense As TextBox
        Private dtpDateDepense As DateTimePicker
        Private btnValiderDepense As Button
        Private gridHistoriqueDepenses As DataGridView

        ' Onglet Caisse
        Private lblEncaisseFC As Label
        Private lblEncaisseUSD As Label
        Private lblDepensesCaisseFC As Label
        Private lblDepensesCaisseUSD As Label
        Private lblSoldeCaisseFC As Label
        Private lblSoldeCaisseUSD As Label
        Private lblStatusCloture As Label

        ' Onglet Banque
        Private lblSoldeBanqueFC As Label
        Private lblSoldeBanqueUSD As Label
        Private gridHistoriqueBanque As DataGridView

        ' Onglet Dashboard
        Private chartDepensesCat As Chart
        Private chartEvolutionFinance As Chart
        Private lblTotalEncaisse As Label
        Private lblTotalDepenses As Label
        Private lblSoldeGlobalBanque As Label

        ' Impression
        Private ReadOnly printDoc As New PrintDocument()
        Private ReadOnly printPreview As New PrintPreviewDialog()
        Private dtRapportAImprimer As DataTable
        Private titreRapport As String = ""

        ' Filtres Impression
        Private cmbAnneeRapport As ComboBox
        Private cmbMoisRapport As ComboBox
        Private btnImprimerRapport As Button

        Public Sub New()
            InitialiserServices()

            Me.Text = "Gestion Financière - Paon Rehoboth"
            Me.Width = 1200
            Me.Height = 850
            Me.BackColor = ColorBackground
            Me.DoubleBuffered = True
            Me.StartPosition = FormStartPosition.CenterScreen

            ' --- Header ---
            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 70, .BackColor = ColorPrimaryHeader}
            Dim lblTitle As New Label() With {.Text = "Gestion Financière", .Left = 25, .Top = 15, .AutoSize = True, .Font = FontTitle, .ForeColor = ColorWhite}
            pnlHeader.Controls.Add(lblTitle)


            ' --- TabControl ---
            tabControlFinance = New TabControl() With {.Dock = DockStyle.Fill, .Padding = New Point(20, 10), .Font = FontControl}

            tpDepenses = New TabPage("🧾 Dépenses")
            tpCaisse = New TabPage("💰 Caisse Journalière")
            tpBanque = New TabPage("🏦 Banque")
            tpDashboard = New TabPage("📊 Dashboard Financier")

            tabControlFinance.TabPages.AddRange(New TabPage() {tpDepenses, tpCaisse, tpBanque, tpDashboard})
            Me.Controls.Add(tabControlFinance)
            Me.Controls.Add(pnlHeader)

            InitOngletDepenses()
            InitOngletCaisse()
            InitOngletBanque()
            InitOngletDashboard()

            ' Configuration Impression
            AddHandler printDoc.PrintPage, AddressOf PrintDoc_PrintPage
            printPreview.Document = printDoc

            ClotureAutomatique()
            ChargerDonnees()
        End Sub

        Private Sub InitialiserServices()
            Dim connectionString As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(connectionString)

            Dim catRepo As New CategorieDepenseRepository(dal)
            Dim depRepo As New DepenseRepositoryFinance(dal)
            Dim banqueRepo As New BanqueRepository(dal)
            Dim caisseRepo As New CaisseRepository(dal)

            _catService = New CategorieDepenseService(catRepo)
            _banqueService = New BanqueService(banqueRepo)
            _caisseService = New CaisseService(caisseRepo, depRepo, _banqueService)
            _depenseService = New DepenseServiceFinance(depRepo, _banqueService, _caisseService)
        End Sub

        ' --- INITIALISATION DES ONGLETS ---

        Private Sub InitOngletDepenses()
            tpDepenses.BackColor = ColorBackground
            Dim mainLayout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1, .Padding = New Padding(15)}
            mainLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 400))
            mainLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

            ' Formulaire de saisie (Carte)
            Dim pnlSaisie As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Margin = New Padding(0, 0, 15, 0), .Padding = New Padding(20)}
            Dim lblSaisieTitle As New Label() With {.Text = "Nouvelle Dépense", .Dock = DockStyle.Top, .Height = 35, .Font = FontLabel, .ForeColor = ColorPrimary}

            Dim pnlCat As New Panel() With {.Dock = DockStyle.Top, .Height = 35, .Margin = New Padding(0, 0, 0, 15)}
            cmbCategorieDepense = New ComboBox() With {.Dock = DockStyle.Fill, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl}
            btnAddCategorie = New Button() With {.Text = "+", .Dock = DockStyle.Right, .Width = 35, .BackColor = ColorSecondary, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            pnlCat.Controls.Add(cmbCategorieDepense)
            pnlCat.Controls.Add(btnAddCategorie)

            txtMontantDepense = CreateStyledTextBox("0.00")
            cmbDeviseDepense = CreateStyledCombo(New String() {"FC", "USD"})
            cmbSourceDepense = CreateStyledCombo(New String() {"Caisse", "Banque"})
            cmbTypeDepense = CreateStyledCombo(New String() {"Normale", "Exceptionnelle"})
            txtDescriptionDepense = CreateStyledTextBox("Description...")
            dtpDateDepense = New DateTimePicker() With {.Dock = DockStyle.Top, .Margin = New Padding(0, 5, 0, 15)}
            btnValiderDepense = CreateStyledButton("Valider la dépense", ColorDanger2)

            pnlSaisie.Controls.Add(btnValiderDepense)
            pnlSaisie.Controls.Add(dtpDateDepense)
            pnlSaisie.Controls.Add(CreateLabel("Date"))
            pnlSaisie.Controls.Add(txtDescriptionDepense)
            pnlSaisie.Controls.Add(CreateLabel("Description"))
            pnlSaisie.Controls.Add(cmbTypeDepense)
            pnlSaisie.Controls.Add(CreateLabel("Type"))
            pnlSaisie.Controls.Add(cmbSourceDepense)
            pnlSaisie.Controls.Add(CreateLabel("Source"))
            pnlSaisie.Controls.Add(cmbDeviseDepense)
            pnlSaisie.Controls.Add(CreateLabel("Devise"))
            pnlSaisie.Controls.Add(txtMontantDepense)
            pnlSaisie.Controls.Add(CreateLabel("Montant"))
            pnlSaisie.Controls.Add(pnlCat)
            pnlSaisie.Controls.Add(CreateLabel("Catégorie"))
            pnlSaisie.Controls.Add(lblSaisieTitle)

            ' Historique (Carte)
            Dim pnlHistorique As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Padding = New Padding(20)}

            ' Barre d'outils impression
            Dim pnlPrintTools As New FlowLayoutPanel() With {.Dock = DockStyle.Top, .Height = 50, .BackColor = ColorCard}
            cmbAnneeRapport = New ComboBox() With {.Width = 80, .DropDownStyle = ComboBoxStyle.DropDownList}
            For i As Integer = DateTime.Now.Year To DateTime.Now.Year - 5 Step -1
                cmbAnneeRapport.Items.Add(i)
            Next
            cmbAnneeRapport.SelectedIndex = 0

            cmbMoisRapport = New ComboBox() With {.Width = 120, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbMoisRapport.Items.Add("Toute l'année")
            cmbMoisRapport.Items.AddRange(New String() {"Janvier", "Février", "Mars", "Avril", "Mai", "Juin", "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"})
            cmbMoisRapport.SelectedIndex = DateTime.Now.Month

            btnImprimerRapport = New Button() With {.Text = "🖨️ Imprimer Rapport", .Width = 150, .Height = 30, .BackColor = ColorPrimary, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}

            pnlPrintTools.Controls.Add(New Label() With {.Text = "Année:", .AutoSize = True, .Padding = New Padding(0, 5, 0, 0)})
            pnlPrintTools.Controls.Add(cmbAnneeRapport)
            pnlPrintTools.Controls.Add(New Label() With {.Text = "Mois:", .AutoSize = True, .Padding = New Padding(10, 5, 0, 0)})
            pnlPrintTools.Controls.Add(cmbMoisRapport)
            pnlPrintTools.Controls.Add(btnImprimerRapport)

            gridHistoriqueDepenses = CreateStyledGrid()
            pnlHistorique.Controls.Add(gridHistoriqueDepenses)
            pnlHistorique.Controls.Add(pnlPrintTools)
            pnlHistorique.Controls.Add(New Label() With {.Text = "Historique des Dépenses", .Dock = DockStyle.Top, .Height = 35, .Font = FontLabel, .ForeColor = ColorPrimary})

            mainLayout.Controls.Add(pnlSaisie, 0, 0)
            mainLayout.Controls.Add(pnlHistorique, 1, 0)
            tpDepenses.Controls.Add(mainLayout)

            AddHandler btnValiderDepense.Click, AddressOf ValiderDepense
            AddHandler btnAddCategorie.Click, AddressOf AjouterNouvelleCategorie
            AddHandler btnImprimerRapport.Click, AddressOf PreparerImpression
        End Sub

        Private Sub InitOngletCaisse()
            tpCaisse.BackColor = ColorBackground
            Dim flow As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(20), .AutoScroll = True}

            lblEncaisseFC = CreerKpiCard(flow, "Encaisse du Jour (FC)", ColorSuccess)
            lblEncaisseUSD = CreerKpiCard(flow, "Encaisse du Jour (USD)", ColorSuccess)
            lblDepensesCaisseFC = CreerKpiCard(flow, "Dépenses Caisse (FC)", ColorDanger)
            lblDepensesCaisseUSD = CreerKpiCard(flow, "Dépenses Caisse (USD)", ColorDanger)
            lblSoldeCaisseFC = CreerKpiCard(flow, "Solde Actuel (FC)", ColorPrimary)
            lblSoldeCaisseUSD = CreerKpiCard(flow, "Solde Actuel (USD)", ColorPrimary)

            lblStatusCloture = New Label() With {.Text = "Statut : Prêt", .Width = 1000, .Font = FontLabel, .ForeColor = ColorTextSecondary, .Margin = New Padding(10, 20, 0, 0)}
            flow.Controls.Add(lblStatusCloture)

            tpCaisse.Controls.Add(flow)
        End Sub

        Private Sub InitOngletBanque()
            tpBanque.BackColor = ColorBackground
            Dim mainLayout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2, .Padding = New Padding(20)}
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 150))
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim pnlSoldes As New FlowLayoutPanel() With {.Dock = DockStyle.Fill}
            lblSoldeBanqueFC = CreerKpiCard(pnlSoldes, "Solde Banque (FC)", ColorPrimary)
            lblSoldeBanqueUSD = CreerKpiCard(pnlSoldes, "Solde Banque (USD)", ColorPrimary)

            Dim pnlHist As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Padding = New Padding(20), .Margin = New Padding(0, 20, 0, 0)}
            Dim lblTitle As New Label() With {.Text = "Historique des Opérations Bancaires", .Dock = DockStyle.Top, .Height = 35, .Font = FontLabel}
            gridHistoriqueBanque = CreateStyledGrid()
            pnlHist.Controls.Add(gridHistoriqueBanque)
            pnlHist.Controls.Add(lblTitle)

            mainLayout.Controls.Add(pnlSoldes, 0, 0)
            mainLayout.Controls.Add(pnlHist, 0, 1)
            tpBanque.Controls.Add(mainLayout)
        End Sub

        Private Sub InitOngletDashboard()
            tpDashboard.BackColor = ColorBackground
            Dim mainLayout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 2, .Padding = New Padding(20)}
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 50))
            mainLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 50))

            chartDepensesCat = CreerChart("Dépenses par Catégorie", SeriesChartType.Pie)
            chartEvolutionFinance = CreerChart("Évolution Caisse vs Banque", SeriesChartType.Line)

            mainLayout.Controls.Add(WrapInCard(chartDepensesCat, "Répartition des Dépenses"), 0, 0)
            mainLayout.Controls.Add(WrapInCard(chartEvolutionFinance, "Évolution des Flux"), 1, 0)

            Dim pnlKpi As New FlowLayoutPanel() With {.Dock = DockStyle.Fill}
            lblTotalEncaisse = CreerKpiCard(pnlKpi, "Total Encaisse", ColorSuccess)
            lblTotalDepenses = CreerKpiCard(pnlKpi, "Total Dépenses", ColorDanger)
            lblSoldeGlobalBanque = CreerKpiCard(pnlKpi, "Solde Banque Global", ColorPrimary)

            mainLayout.Controls.Add(pnlKpi, 0, 1)
            mainLayout.SetColumnSpan(pnlKpi, 2)

            tpDashboard.Controls.Add(mainLayout)
        End Sub

        ' --- LOGIQUE MÉTIER ---

        Private Sub ClotureAutomatique()
            Try
                _caisseService.ClotureAutomatique()
                lblStatusCloture.Text = "Statut : Clôture automatique effectuée avec succès."
            Catch ex As Exception
                lblStatusCloture.Text = "Erreur clôture : " & ex.Message
            End Try
        End Sub

        Private Sub ChargerDonnees()
            Try
                Dim dtCat As DataTable = _catService.GetAll()
                cmbCategorieDepense.DataSource = dtCat
                cmbCategorieDepense.DisplayMember = "Libelle"
                cmbCategorieDepense.ValueMember = "Libelle"

                Dim dateJour As DateTime = DateTime.Now
                lblEncaisseFC.Text = _caisseService.GetEncaisse(dateJour, "FC").ToString("N2") & " FC"
                lblEncaisseUSD.Text = _caisseService.GetEncaisse(dateJour, "USD").ToString("N2") & " USD"
                lblDepensesCaisseFC.Text = _caisseService.GetDepensesCaisse(dateJour, "FC").ToString("N2") & " FC"
                lblDepensesCaisseUSD.Text = _caisseService.GetDepensesCaisse(dateJour, "USD").ToString("N2") & " USD"
                lblSoldeCaisseFC.Text = _caisseService.GetSoldeCaisse(dateJour, "FC").ToString("N2") & " FC"
                lblSoldeCaisseUSD.Text = _caisseService.GetSoldeCaisse(dateJour, "USD").ToString("N2") & " USD"

                lblSoldeBanqueFC.Text = _banqueService.GetSolde("FC").ToString("N2") & " FC"
                lblSoldeBanqueUSD.Text = _banqueService.GetSolde("USD").ToString("N2") & " USD"

                gridHistoriqueDepenses.DataSource = _depenseService.GetHistorique()
                gridHistoriqueBanque.DataSource = _banqueService.GetHistorique()

                ChargerGraphiques()
            Catch ex As Exception
                MessageBox.Show("Erreur lors du chargement : " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerGraphiques()
            chartDepensesCat.Series(0).Points.Clear()
            Dim dtStats As DataTable = _depenseService.GetStatsParCategorie()
            For Each row As DataRow In dtStats.Rows
                chartDepensesCat.Series(0).Points.AddXY(row("Categorie"), row("Total"))
            Next
        End Sub

        Private Sub ValiderDepense(sender As Object, e As EventArgs)
            Try
                Dim depense As New DepenseDTOFinance With {
                    .Categorie = cmbCategorieDepense.Text,
                    .Montant = Decimal.Parse(txtMontantDepense.Text),
                    .Devise = cmbDeviseDepense.Text,
                    .Source = cmbSourceDepense.Text,
                    .TypeDepense = cmbTypeDepense.Text,
                    .Description = txtDescriptionDepense.Text,
                    .DateDepense = dtpDateDepense.Value,
                    .CreePar = "Admin"
                }

                _depenseService.AjouterDepense(depense)
                MessageBox.Show("Dépense enregistrée avec succès !", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information)
                ChargerDonnees()
                txtMontantDepense.Text = "0.00"
                txtDescriptionDepense.Text = ""
            Catch ex As Exception
                MessageBox.Show("Erreur : " & ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub AjouterNouvelleCategorie(sender As Object, e As EventArgs)
            Dim libelle As String = InputBox("Entrez le nom de la nouvelle catégorie :", "Nouvelle Catégorie")
            If Not String.IsNullOrEmpty(libelle) Then
                _catService.Ajouter(libelle)
                ChargerDonnees()
            End If
        End Sub

        ' --- IMPRESSION ---

        Private Sub PreparerImpression(sender As Object, e As EventArgs)
            Try
                Dim annee As Integer = Convert.ToInt32(cmbAnneeRapport.SelectedItem)
                Dim mois As Integer = cmbMoisRapport.SelectedIndex ' 0 = Toute l'année, 1 = Janvier...

                dtRapportAImprimer = _depenseService.GetRapportDepenses(annee, mois)

                If mois = 0 Then
                    titreRapport = "RAPPORT ANNUEL DES DÉPENSES - " & annee.ToString()
                Else
                    titreRapport = "RAPPORT MENSUEL DES DÉPENSES - " & cmbMoisRapport.SelectedItem.ToString().ToUpper() & " " & annee.ToString()
                End If

                If dtRapportAImprimer.Rows.Count = 0 Then
                    MessageBox.Show("Aucune donnée trouvée pour cette période.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If

                printPreview.ShowDialog()
            Catch ex As Exception
                MessageBox.Show("Erreur lors de la préparation de l'impression : " & ex.Message)
            End Try
        End Sub

        Private Sub PrintDoc_PrintPage(sender As Object, e As PrintPageEventArgs)
            Dim graphics As Graphics = e.Graphics
            Dim fontTitre As New Font("Segoe UI", 16, FontStyle.Bold)
            Dim fontHeader As New Font("Segoe UI", 10, FontStyle.Bold)
            Dim fontBody As New Font("Segoe UI", 10)
            Dim brush As Brush = Brushes.Black
            Dim pen As New Pen(Color.Gray, 1)

            Dim x As Integer = 50
            Dim y As Integer = 50
            Dim colWidths As Integer() = {300, 150, 100}

            ' En-tête du document
            graphics.DrawString("PAON REHOBOTH", fontHeader, brush, x, y)
            y += 25
            graphics.DrawString(titreRapport, fontTitre, brush, x, y)
            y += 40
            graphics.DrawString("Date d'impression : " & DateTime.Now.ToString("dd/MM/yyyy HH:mm"), fontBody, brush, x, y)
            y += 40

            ' En-têtes de colonnes
            graphics.FillRectangle(Brushes.LightGray, x, y, 550, 25)
            graphics.DrawString("CATÉGORIE", fontHeader, brush, x + 5, y + 5)
            graphics.DrawString("MONTANT", fontHeader, brush, x + colWidths(0) + 5, y + 5)
            graphics.DrawString("DEVISE", fontHeader, brush, x + colWidths(0) + colWidths(1) + 5, y + 5)
            y += 25

            ' Lignes de données
            Dim totalFC As Decimal = 0
            Dim totalUSD As Decimal = 0

            For Each row As DataRow In dtRapportAImprimer.Rows
                Dim cat As String = row("Categorie").ToString()
                Dim montant As Decimal = Convert.ToDecimal(row("Total"))
                Dim devise As String = row("Devise").ToString()

                graphics.DrawString(cat, fontBody, brush, x + 5, y + 5)
                graphics.DrawString(montant.ToString("N2"), fontBody, brush, x + colWidths(0) + 5, y + 5)
                graphics.DrawString(devise, fontBody, brush, x + colWidths(0) + colWidths(1) + 5, y + 5)

                graphics.DrawLine(pen, x, y + 25, x + 550, y + 25)

                If devise = "FC" Then totalFC += montant Else totalUSD += montant

                y += 25
                If y > e.MarginBounds.Bottom Then
                    e.HasMorePages = True
                    Return
                End If
            Next

            ' Totaux
            y += 20
            graphics.DrawString("TOTAL GÉNÉRAL FC : " & totalFC.ToString("N2") & " FC", fontHeader, brush, x, y)
            y += 20
            graphics.DrawString("TOTAL GÉNÉRAL USD : " & totalUSD.ToString("N2") & " USD", fontHeader, brush, x, y)

            e.HasMorePages = False
        End Sub

        ' --- HELPERS UI ---

        Private Function CreateLabel(text As String) As Label
            Return New Label() With {.Text = text, .Dock = DockStyle.Top, .Height = 20, .Font = FontLabel, .ForeColor = ColorTextSecondary}
        End Function

        Private Function CreateStyledTextBox(placeholder As String) As TextBox
            Return New TextBox() With {.Text = placeholder, .Dock = DockStyle.Top, .Margin = New Padding(0, 0, 0, 15), .Font = FontControl}
        End Function

        Private Function CreateStyledCombo(items As String()) As ComboBox
            Dim cb As New ComboBox() With {.Dock = DockStyle.Top, .Margin = New Padding(0, 0, 0, 15), .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontControl}
            cb.Items.AddRange(items)
            If cb.Items.Count > 0 Then cb.SelectedIndex = 0
            Return cb
        End Function

        Private Function CreateStyledButton(text As String, color As Color) As Button
            Return New Button() With {.Text = text, .Dock = DockStyle.Top, .Height = 40, .BackColor = color, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = FontLabel}
        End Function

        Private Function CreerKpiCard(parent As Control, title As String, color As Color) As Label
            Dim p As New Panel() With {.Width = 220, .Height = 100, .BackColor = ColorCard, .Margin = New Padding(10)}
            Dim lblT As New Label() With {.Text = title, .Top = 15, .Left = 15, .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary}
            Dim lblV As New Label() With {.Text = "0", .Top = 45, .Left = 15, .AutoSize = True, .Font = FontValue, .ForeColor = color}
            p.Controls.Add(lblT)
            p.Controls.Add(lblV)
            parent.Controls.Add(p)
            Return lblV
        End Function

        Private Function CreateStyledGrid() As DataGridView
            Return New DataGridView() With {.Dock = DockStyle.Fill, .BackgroundColor = Color.White, .BorderStyle = BorderStyle.None, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .ReadOnly = True, .AllowUserToAddRows = False}
        End Function

        Private Function CreerChart(title As String, type As SeriesChartType) As Chart
            Dim c As New Chart() With {.Dock = DockStyle.Fill, .MinimumSize = New Size(100, 100)}
            Dim area As New ChartArea()
            c.ChartAreas.Add(area)
            Dim s As New Series(title) With {.ChartType = type}
            c.Series.Add(s)
            Return c
        End Function

        Private Function WrapInCard(ctrl As Control, title As String) As Panel
            Dim p As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Padding = New Padding(15), .Margin = New Padding(10)}
            Dim lbl As New Label() With {.Text = title, .Dock = DockStyle.Top, .Height = 30, .Font = FontLabel}
            p.Controls.Add(ctrl)
            p.Controls.Add(lbl)
            Return p
        End Function
    End Class
End Namespace
