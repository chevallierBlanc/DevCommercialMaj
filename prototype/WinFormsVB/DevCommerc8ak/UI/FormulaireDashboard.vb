Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting

Namespace DevCommerc8ak
    Public Class FormulaireDashboard
        Inherits Form

        'Private ReadOnly panelSidebar As Panel
        'Private ReadOnly panelHeader As Panel
        'Private ReadOnly panelKpi As Panel
        'Private ReadOnly panelCharts As Panel
        'Private ReadOnly panelAlerts As Panel
        'Private ReadOnly panelActivities As Panel
        'Private ReadOnly panelActions As Panel

        'Private ReadOnly lblEntreprise As Label
        'Private ReadOnly lblUtilisateur As Label
        'Private ReadOnly lblDateHeure As Label
        'Private ReadOnly btnNotif As Button
        'Private ReadOnly btnDeconnexion As Button
        'Private ReadOnly btnToggleMenu As Button

        'Private ReadOnly lblKpiCAJour As Label
        'Private ReadOnly lblKpiCAMois As Label
        'Private ReadOnly lblKpiValeurStock As Label
        'Private ReadOnly lblKpiStockCritique As Label
        'Private ReadOnly lblKpiClients As Label
        'Private ReadOnly lblKpiFacturesAttente As Label

        'Private ReadOnly chartVentesMois As Chart
        'Private ReadOnly chartRevenus As Chart
        'Private ReadOnly chartComparaison As Chart
        'Private ReadOnly chartTaux As Chart

        'Private ReadOnly gridAlertes As DataGridView
        'Private ReadOnly listActivites As ListView

        'Private ReadOnly timerRefresh As Timer
        'Private ReadOnly timerClock As Timer
        'Private ReadOnly timerAnim As Timer

        'Private ReadOnly kpiTargets As Dictionary(Of Label, Decimal)
        'Private ReadOnly kpiCurrent As Dictionary(Of Label, Decimal)

        'Private _menuCollapsed As Boolean

        'Public Sub New()
        '    Me.Text = "Dashboard Admin"
        '    Me.Width = 1280
        '    Me.Height = 820
        '    Me.StartPosition = FormStartPosition.CenterScreen
        '    Me.BackColor = Color.White

        '    panelSidebar = New Panel() With {.Left = 0, .Top = 0, .Width = 200, .Height = Me.Height, .BackColor = Color.FromArgb(20, 30, 45)}
        '    panelHeader = New Panel() With {.Left = 200, .Top = 0, .Width = 1060, .Height = 60, .BackColor = Color.White}
        '    panelKpi = New Panel() With {.Left = 200, .Top = 60, .Width = 1060, .Height = 130, .BackColor = Color.White}
        '    panelCharts = New Panel() With {.Left = 200, .Top = 190, .Width = 1060, .Height = 340, .BackColor = Color.White}
        '    panelAlerts = New Panel() With {.Left = 200, .Top = 540, .Width = 520, .Height = 170, .BackColor = Color.White}
        '    panelActivities = New Panel() With {.Left = 740, .Top = 540, .Width = 520, .Height = 170, .BackColor = Color.White}
        '    panelActions = New Panel() With {.Left = 200, .Top = 720, .Width = 1060, .Height = 70, .BackColor = Color.White}

        '    btnToggleMenu = New Button() With {.Text = "≡", .Left = 10, .Top = 10, .Width = 40, .Height = 30, .BackColor = Color.FromArgb(30, 45, 65), .ForeColor = Color.White}
        '    AddHandler btnToggleMenu.Click, AddressOf ToggleMenu
        '    panelSidebar.Controls.Add(btnToggleMenu)

        '    AjouterBoutonSidebar("Dashboard", 60, True, Sub() End)
        '    AjouterBoutonSidebar("Produits", 110, False, Sub() Ouvrir(New FormulaireProduits()))
        '    AjouterBoutonSidebar("Facturation", 160, False, Sub() Ouvrir(New FacturationForm()))
        '    AjouterBoutonSidebar("Caissier", 210, False, Sub() Ouvrir(New CaisseForm()))
        '    AjouterBoutonSidebar("Clients", 260, False, Sub() Ouvrir(New FormulaireClients()))
        '    AjouterBoutonSidebar("Approvisionnement", 310, False, Sub() Ouvrir(New FormulaireApprovisionnement()))
        '    AjouterBoutonSidebar("Rapports", 360, False, Sub() Ouvrir(New FormulaireRapports()))

        '    lblEntreprise = New Label() With {.Text = "Paons Rehoboth", .Left = 20, .Top = 18, .AutoSize = True, .Font = New Font("Segoe UI", 12, FontStyle.Bold)}
        '    lblUtilisateur = New Label() With {.Text = "Admin", .Left = 300, .Top = 20, .AutoSize = True}
        '    lblDateHeure = New Label() With {.Left = 500, .Top = 20, .AutoSize = True}
        '    btnNotif = New Button() With {.Text = "Notifications", .Left = 760, .Top = 15, .Width = 140, .BackColor = Color.FromArgb(20, 30, 45), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
        '    btnNotif.FlatAppearance.BorderSize = 0
        '    btnDeconnexion = New Button() With {.Text = "Deconnexion", .Left = 880, .Top = 15, .Width = 110}
        '    AddHandler btnNotif.Click, AddressOf AfficherNotifications
        '    AddHandler btnDeconnexion.Click, Sub() Me.Close()

        '    panelHeader.Controls.Add(lblEntreprise)
        '    panelHeader.Controls.Add(lblUtilisateur)
        '    panelHeader.Controls.Add(lblDateHeure)
        '    panelHeader.Controls.Add(btnNotif)
        '    panelHeader.Controls.Add(btnDeconnexion)

        '    lblKpiCAJour = CreerKpiCard(panelKpi, "CA du jour", 10, Color.LightGreen)
        '    lblKpiCAMois = CreerKpiCard(panelKpi, "CA du mois", 185, Color.LightGreen)
        '    lblKpiValeurStock = CreerKpiCard(panelKpi, "Valeur du stock", 360, Color.LightBlue)
        '    lblKpiStockCritique = CreerKpiCard(panelKpi, "Stock critique", 535, Color.LightCoral)
        '    lblKpiClients = CreerKpiCard(panelKpi, "Clients fideles", 710, Color.Plum)
        '    lblKpiFacturesAttente = CreerKpiCard(panelKpi, "Factures en attente", 885, Color.Orange)

        '    chartVentesMois = CreerChart(panelCharts, 10, 10, 500, 155, "Ventes mensuelles", SeriesChartType.Line)
        '    chartRevenus = CreerChart(panelCharts, 530, 10, 510, 155, "Repartition revenus", SeriesChartType.Pie)
        '    chartComparaison = CreerChart(panelCharts, 10, 180, 500, 150, "Comparaison mois", SeriesChartType.Column)
        '    chartTaux = CreerChart(panelCharts, 530, 180, 510, 150, "Taux de ventes", SeriesChartType.Column)

        '    Dim lblAlertes As New Label() With {.Text = "Alertes intelligentes", .Left = 10, .Top = 10, .AutoSize = True, .Font = New Font("Segoe UI", 10, FontStyle.Bold)}
        '    gridAlertes = New DataGridView() With {.Left = 10, .Top = 35, .Width = 500, .Height = 120, .ReadOnly = True, .AutoGenerateColumns = False, .AllowUserToAddRows = False, .RowHeadersVisible = False}
        '    ConfigurerAlertes()
        '    panelAlerts.Controls.Add(lblAlertes)
        '    panelAlerts.Controls.Add(gridAlertes)

        '    Dim lblAct As New Label() With {.Text = "Activite recente", .Left = 10, .Top = 10, .AutoSize = True, .Font = New Font("Segoe UI", 10, FontStyle.Bold)}
        '    listActivites = New ListView() With {.Left = 10, .Top = 35, .Width = 500, .Height = 120, .View = View.Details, .FullRowSelect = True}
        '    listActivites.Columns.Add("Type", 140)
        '    listActivites.Columns.Add("Info", 220)
        '    listActivites.Columns.Add("Date", 120)
        '    panelActivities.Controls.Add(lblAct)
        '    panelActivities.Controls.Add(listActivites)

        '    Dim lblActions As New Label() With {.Text = "Actions rapides", .Left = 10, .Top = 10, .AutoSize = True, .Font = New Font("Segoe UI", 10, FontStyle.Bold)}
        '    panelActions.Controls.Add(lblActions)
        '    Dim btnFacture As New Button() With {.Text = "Nouvelle facture", .Left = 10, .Top = 30, .Width = 160}
        '    Dim btnEncaisser As New Button() With {.Text = "Encaisser", .Left = 180, .Top = 30, .Width = 120}
        '    Dim btnProduit As New Button() With {.Text = "Ajouter produit", .Left = 310, .Top = 30, .Width = 140}
        '    Dim btnAppro As New Button() With {.Text = "Approvisionner", .Left = 460, .Top = 30, .Width = 140}
        '    AddHandler btnFacture.Click, Sub() Ouvrir(New FacturationForm())
        '    AddHandler btnEncaisser.Click, Sub() Ouvrir(New CaisseForm())
        '    AddHandler btnProduit.Click, Sub() Ouvrir(New FormulaireProduits())
        '    AddHandler btnAppro.Click, Sub() Ouvrir(New FormulaireApprovisionnement())
        '    panelActions.Controls.Add(btnFacture)
        '    panelActions.Controls.Add(btnEncaisser)
        '    panelActions.Controls.Add(btnProduit)
        '    panelActions.Controls.Add(btnAppro)

        '    Me.Controls.Add(panelSidebar)
        '    Me.Controls.Add(panelHeader)
        '    Me.Controls.Add(panelKpi)
        '    Me.Controls.Add(panelCharts)
        '    Me.Controls.Add(panelAlerts)
        '    Me.Controls.Add(panelActivities)
        '    Me.Controls.Add(panelActions)

        '    kpiTargets = New Dictionary(Of Label, Decimal)()
        '    kpiCurrent = New Dictionary(Of Label, Decimal)()

        '    'ThemeHelper.AppliquerTheme(Me)
        '    ' IconsHelper.AppliquerIconeFormulaire(Me)

        '    timerClock = New Timer() With {.Interval = 1000}
        '    AddHandler timerClock.Tick, AddressOf MajHorloge
        '    timerClock.Start()

        '    timerRefresh = New Timer() With {.Interval = 600000}
        '    AddHandler timerRefresh.Tick, AddressOf Timer_Tick
        '    timerRefresh.Start()

        '    timerAnim = New Timer() With {.Interval = 30}
        '    AddHandler timerAnim.Tick, AddressOf AnimerKpi

        '    Charger()
        'End Sub

        'Private Sub Timer_Tick(sender As Object, e As EventArgs)
        '    Charger()

        'End Sub

        'Private Sub AjouterBoutonSidebar(titre As String, top As Integer, actif As Boolean, action As Action)
        '    Dim btn As New Button() With {.Text = titre, .Tag = titre, .Left = 10, .Top = top, .Width = 180, .Height = 36, .ForeColor = Color.White, .BackColor = If(actif, Color.FromArgb(40, 70, 110), Color.FromArgb(30, 45, 65))}
        '    AddHandler btn.Click, Sub() action()
        '    panelSidebar.Controls.Add(btn)
        'End Sub

        'Private Function CreerKpiCard(parent As Panel, titre As String, left As Integer, color As Color) As Label
        '    Dim card As New Panel() With {.Left = left, .Top = 10, .Width = 165, .Height = 110, .BackColor = Color.White, .BorderStyle = BorderStyle.FixedSingle}
        '    Dim lblTitre As New Label() With {.Text = titre, .Left = 10, .Top = 10, .AutoSize = True, .ForeColor = Color.FromArgb(80, 80, 80)}
        '    Dim lblVal As New Label() With {.Text = "0", .Left = 10, .Top = 45, .AutoSize = True, .Font = New Font("Segoe UI", 14, FontStyle.Bold), .ForeColor = color}
        '    card.Controls.Add(lblTitre)
        '    card.Controls.Add(lblVal)
        '    parent.Controls.Add(card)
        '    Return lblVal
        'End Function

        'Private Function CreerChart(parent As Panel, left As Integer, top As Integer, width As Integer, height As Integer, titre As String, typeChart As SeriesChartType) As Chart
        '    Dim chart As New Chart() With {.Left = left, .Top = top, .Width = width, .Height = height}
        '    Dim area As New ChartArea()
        '    chart.ChartAreas.Add(area)
        '    Dim series As New Series(titre) With {.ChartType = typeChart}
        '    chart.Series.Add(series)
        '    chart.Titles.Add(titre)
        '    parent.Controls.Add(chart)
        '    Return chart
        'End Function

        'Private Sub ConfigurerAlertes()
        '    gridAlertes.Columns.Clear()
        '    gridAlertes.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "TypeAlerte", .HeaderText = "Alerte", .Width = 160})
        '    gridAlertes.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "Cible", .HeaderText = "Cible", .Width = 200})
        '    gridAlertes.Columns.Add(New DataGridViewButtonColumn() With {.Name = "ActionVoir", .HeaderText = "Voir", .Text = "Voir", .UseColumnTextForButtonValue = True, .Width = 60})
        '    gridAlertes.Columns.Add(New DataGridViewButtonColumn() With {.Name = "ActionCommander", .HeaderText = "Commander", .Text = "Commander", .UseColumnTextForButtonValue = True, .Width = 90})
        '    AddHandler gridAlertes.CellContentClick, AddressOf ActionsAlertes
        'End Sub

        'Private Sub ActionsAlertes(sender As Object, e As DataGridViewCellEventArgs)
        '    If e.RowIndex < 0 Then Return
        '    Dim typeAlerte As String = Convert.ToString(gridAlertes.Rows(e.RowIndex).Cells("TypeAlerte").Value)
        '    Dim col As String = gridAlertes.Columns(e.ColumnIndex).Name

        '    If col = "ActionVoir" Then
        '        If typeAlerte.Contains("Facture") Then
        '            Ouvrir(New FormulaireFactures())
        '        Else
        '            Ouvrir(New FormulaireProduits())
        '        End If
        '    ElseIf col = "ActionCommander" Then
        '        Ouvrir(New FormulaireApprovisionnement())
        '    End If
        'End Sub

        'Private Sub Ouvrir(f As Form)
        '    f.StartPosition = FormStartPosition.CenterParent
        '    f.ShowDialog(Me)
        'End Sub

        'Private Sub ToggleMenu(sender As Object, e As EventArgs)
        '    _menuCollapsed = Not _menuCollapsed
        '    panelSidebar.Width = If(_menuCollapsed, 60, 200)
        '    For Each ctrl As Control In panelSidebar.Controls
        '        If TypeOf ctrl Is Button AndAlso ctrl IsNot btnToggleMenu Then
        '            ctrl.Width = If(_menuCollapsed, 40, 180)
        '            ctrl.Text = If(_menuCollapsed, "", Convert.ToString(ctrl.Tag))
        '        End If
        '    Next
        'End Sub

        'Private Sub MajHorloge(sender As Object, e As EventArgs)
        '    lblDateHeure.Text = Date.Now.ToString("dd/MM/yyyy HH:mm:ss")
        'End Sub

        'Private Sub Charger()
        '    Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
        '    Dim dal As New DAL(cs)
        '    Dim paramService As New ParametreService(New ParametreRepository(dal))
        '    Dim param As ParametreDTO = paramService.Charger()
        '    If param IsNot Nothing AndAlso param.NomMagasin <> "" Then
        '        lblEntreprise.Text = param.NomMagasin
        '    End If

        '    Dim service As New RapportService(dal)
        '    Dim caJour As Decimal = service.CAJournalier(Date.Now)
        '    Dim caMois As Decimal = service.CAMensuel(Date.Now)
        '    Dim stockCrit As Integer = service.StockCritique(If(param Is Nothing, 0D, param.SeuilStockCritique))
        '    Dim valStock As Decimal = service.ValeurStock()
        '    Dim fideles As Integer = service.ClientsFideles()
        '    Dim facturesAttente As Integer = service.FacturesEnAttente()

        '    Animer(lblKpiCAJour, caJour)
        '    Animer(lblKpiCAMois, caMois)
        '    Animer(lblKpiValeurStock, valStock)
        '    Animer(lblKpiStockCritique, stockCrit)
        '    Animer(lblKpiClients, fideles)
        '    Animer(lblKpiFacturesAttente, facturesAttente)

        '    Dim dtMois As DataTable = service.VentesParMois()
        '    chartVentesMois.Series(0).Points.Clear()
        '    For Each row As DataRow In dtMois.Rows
        '        chartVentesMois.Series(0).Points.AddXY(Convert.ToString(row("Mois")), Convert.ToDecimal(row("CA")))
        '    Next

        '    Dim dtRevenus As DataTable = service.RevenusParProduit()
        '    chartRevenus.Series(0).Points.Clear()
        '    For Each row As DataRow In dtRevenus.Rows
        '        chartRevenus.Series(0).Points.AddXY(Convert.ToString(row("Libelle")), Convert.ToDecimal(row("Montant")))
        '    Next

        '    Dim dtComp As DataTable = service.ComparatifMois(Date.Now)
        '    chartComparaison.Series(0).Points.Clear()
        '    For Each row As DataRow In dtComp.Rows
        '        chartComparaison.Series(0).Points.AddXY(Convert.ToString(row("Periode")), Convert.ToDecimal(row("CA")))
        '    Next

        '    Dim taux As Decimal = service.TauxVenteStock()
        '    chartTaux.Series(0).Points.Clear()
        '    chartTaux.Series(0).Points.AddXY("Vendu", taux * 100D)
        '    chartTaux.Series(0).Points.AddXY("Stock", 100D - (taux * 100D))

        '    Dim alertes As DataTable = service.AlertesDetail(If(param Is Nothing, 0D, param.SeuilStockCritique), If(param Is Nothing, 0, param.AlerteExpirationJours))
        '    gridAlertes.DataSource = alertes

        '    Dim activites As DataTable = service.ActivitesRecentes()
        '    listActivites.Items.Clear()
        '    For Each row As DataRow In activites.Rows
        '        Dim item As New ListViewItem(Convert.ToString(row("TypeAct")))
        '        item.SubItems.Add(Convert.ToString(row("Info")))
        '        item.SubItems.Add(Convert.ToDateTime(row("DateAct")).ToString("dd/MM HH:mm"))
        '        listActivites.Items.Add(item)
        '    Next

        '    Dim notificationService As New NotificationService(dal)
        '    notificationService.SynchroniserAlertesMetier(If(param Is Nothing, 0D, param.SeuilStockCritique), If(param Is Nothing, 30, param.AlerteExpirationJours), SessionUtilisateur.UtilisateurId)
        '    Dim notifications As DataTable = notificationService.ListerNonLues()
        '    btnNotif.Text = "Notifications (" & notifications.Rows.Count.ToString() & ")"
        '    btnNotif.BackColor = If(notifications.Rows.Count > 0, Color.FromArgb(220, 70, 70), Color.FromArgb(20, 30, 45))
        'End Sub

        'Private Sub AfficherNotifications(sender As Object, e As EventArgs)
        '    Try
        '        Dim frm As New FormulaireNotifications()
        '        frm.ShowDialog(Me)
        '        Charger()
        '    Catch ex As Exception
        '        MessageBox.Show("Erreur chargement notifications: " & ex.Message)
        '    End Try
        'End Sub

        'Private Sub Animer(lbl As Label, valeur As Decimal)
        '    kpiTargets(lbl) = valeur
        '    If Not kpiCurrent.ContainsKey(lbl) Then
        '        kpiCurrent(lbl) = 0D
        '    End If
        '    If Not timerAnim.Enabled Then timerAnim.Start()
        'End Sub

        'Private Sub AnimerKpi(sender As Object, e As EventArgs)
        '    Dim termine As Boolean = True
        '    For Each kv As KeyValuePair(Of Label, Decimal) In kpiTargets
        '        Dim lbl As Label = kv.Key
        '        Dim cible As Decimal = kv.Value
        '        Dim cur As Decimal = kpiCurrent(lbl)
        '        Dim stepVal As Decimal = Math.Max(cible / 20D, 1D)
        '        If cur < cible Then
        '            cur = Math.Min(cible, cur + stepVal)
        '            kpiCurrent(lbl) = cur
        '            lbl.Text = cur.ToString("N0")
        '            termine = False
        '        Else
        '            lbl.Text = cible.ToString("N0")
        '        End If
        '    Next
        '    If termine Then
        '        timerAnim.Stop()
        '    End If
        'End Sub

        ' --- Constantes de Design (Inspirées de la capture d'écran) ---
        Private ReadOnly ColorPrimary As Color = Color.FromArgb(0, 120, 212) ' Bleu Paon
        Private ReadOnly ColorBackground As Color = Color.FromArgb(240, 242, 245)
        Private ReadOnly ColorCard As Color = Color.White
        Private ReadOnly ColorText As Color = Color.FromArgb(45, 45, 45)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(120, 120, 120)
        Private ReadOnly ColorBorder As Color = Color.FromArgb(225, 228, 232)
        Private ReadOnly ColorWhite As Color = Color.White

        'Private ReadOnly FontTitle As New Font("Segoe UI Semibold", 18.0F)
        Private ReadOnly FontSubTitle As New Font("Segoe UI", 9)
        Private ReadOnly FontLabel As New Font("Segoe UI", 10, FontStyle.Bold)
        Private ReadOnly FontValue As New Font("Segoe UI", 18, FontStyle.Bold)
        Private ReadOnly FontControl As New Font("Segoe UI", 10.0F)

        ' --- Polices ---
        Private ReadOnly FontMain As New Font("Segoe UI", 9)
        Private ReadOnly FontBold As New Font("Segoe UI", 10, FontStyle.Bold)
        Private ReadOnly FontTitle As New Font("Segoe UI", 16, FontStyle.Bold)
        Private ReadOnly FontKpiVal As New Font("Segoe UI", 18, FontStyle.Bold)

        ' --- Composants UI (Noms conservés) ---
        Private ReadOnly panelHeader As Panel
        Private ReadOnly panelKpi As Panel
        Private ReadOnly panelCharts As Panel
        Private ReadOnly panelAlerts As Panel
        Private ReadOnly panelActivities As Panel
        Private ReadOnly panelActions As Panel
        Private ReadOnly mainScrollPanel As Panel ' Pour le défilement

        Private ReadOnly lblEntreprise As Label
        Private ReadOnly lblUtilisateur As Label
        Private ReadOnly lblDateHeure As Label
        Private ReadOnly btnNotif As Button
        Private ReadOnly btnDeconnexion As Button

        Private ReadOnly lblKpiCAJour As Label
        Private ReadOnly lblKpiCAMois As Label
        Private ReadOnly lblKpiValeurStock As Label
        Private ReadOnly lblKpiStockCritique As Label
        Private ReadOnly lblKpiClients As Label
        Private ReadOnly lblKpiFacturesAttente As Label

        Private ReadOnly chartVentesMois As Chart
        Private ReadOnly chartRevenus As Chart
        Private ReadOnly chartComparaison As Chart
        Private ReadOnly chartTaux As Chart

        Private ReadOnly gridAlertes As DataGridView
        Private ReadOnly listActivites As ListView

        Private ReadOnly timerRefresh As Timer
        Private ReadOnly timerClock As Timer
        Private ReadOnly timerAnim As Timer

        Private ReadOnly kpiTargets As Dictionary(Of Label, Decimal)
        Private ReadOnly kpiCurrent As Dictionary(Of Label, Decimal)
        Private _chargementEnCours As Boolean

        Private Class DashboardSnapshot
            Public Property NomMagasin As String = String.Empty
            Public Property SeuilStockCritique As Decimal
            Public Property AlerteExpirationJours As Integer
            Public Property KpiCAJour As Decimal
            Public Property KpiCAMois As Decimal
            Public Property KpiValeurStock As Decimal
            Public Property KpiStockCritique As Decimal
            Public Property KpiClients As Decimal
            Public Property KpiFacturesAttente As Decimal
            Public Property VentesMois As DataTable
            Public Property RevenusParProduit As DataTable
            Public Property ComparatifMois As DataTable
            Public Property TauxVenteStock As Decimal
            Public Property Alertes As DataTable
            Public Property Activites As DataTable
            Public Property NotificationsCount As Integer
        End Class

        Public Sub New()
            ' Configuration de base
            Me.Text = "Dashboard - Portail Principal"
            Me.Width = 1300
            Me.Height = 850
            Me.BackColor = ColorBackground
            Me.DoubleBuffered = True

            ' --- 1. HEADER (Fixe en haut) ---
            ' panelHeader = New Panel() With {.Dock = DockStyle.Top, .Height = 80, .BackColor = ColorBackground, .Padding = New Padding(25, 15, 25, 15)}


            ' --- Header ---
            panelHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 70,
                .BackColor = ColorWhite,
                .Padding = New Padding(20, 0, 20, 0)
            }
            AddHandler panelHeader.Paint, Sub(s, e) e.Graphics.DrawLine(New Pen(Color.FromArgb(229, 231, 235)), 0, 69, panelHeader.Width, 69)
            lblEntreprise = New Label() With {.Text = "PAON REHOBOTH", .Left = 25, .Top = 20, .AutoSize = True, .Font = FontTitle, .ForeColor = ColorPrimary}
            lblUtilisateur = New Label() With {.Text = "Connecté en tant que : admin", .Left = 400, .Top = 28, .AutoSize = True, .Font = FontSubTitle, .ForeColor = ColorTextSecondary}
            lblDateHeure = New Label() With {.Text = "mardi 21 avril 2026, 17:59:44", .Left = 620, .Top = 28, .AutoSize = True, .Font = FontSubTitle, .ForeColor = ColorTextSecondary}

            btnNotif = CreateStyledButton("Notifications", Color.FromArgb(235, 243, 255), 140, 38)
            btnNotif.ForeColor = ColorPrimary : btnNotif.Left = 850 : btnNotif.Top = 20

            btnDeconnexion = CreateStyledButton("Déconnexion", Color.FromArgb(255, 240, 240), 120, 38)
            btnDeconnexion.ForeColor = Color.FromArgb(220, 53, 69) : btnDeconnexion.Left = 1000 : btnDeconnexion.Top = 20

            panelHeader.Controls.AddRange({lblEntreprise, lblUtilisateur, lblDateHeure, btnNotif, btnDeconnexion})


            ' --- 2. CONTENEUR AVEC BARRE DE DÉFILEMENT ---
            mainScrollPanel = New Panel() With {.Dock = DockStyle.Fill, .AutoScroll = True, .Padding = New Padding(25, 0, 25, 25)}
            Dim contentLayout As New TableLayoutPanel() With {.ColumnCount = 1, .RowCount = 5, .AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowAndShrink, .Dock = DockStyle.Top}
            contentLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            mainScrollPanel.Controls.Add(contentLayout)
            Me.Controls.Add(mainScrollPanel)
            Me.Controls.Add(panelHeader)

            ' --- 3. ACTIONS RAPIDES (Comme sur la capture) ---
            panelActions = New Panel() With {.Height = 100, .Dock = DockStyle.Fill, .Margin = New Padding(0, 0, 0, 20)}
            Dim lblActionsTitle As New Label() With {.Text = "Actions Rapides", .Top = 5, .Left = 0, .AutoSize = True, .Font = FontLabel, .ForeColor = ColorText}
            Dim flowActions As New FlowLayoutPanel() With {.Top = 35, .Left = 0, .Width = 1200, .Height = 60}

            Dim btnFacture As Button = CreateStyledButton("Nouvelle Facture", Color.FromArgb(0, 120, 212), 180, 42)
            Dim btnEncaisser As Button = CreateStyledButton("Encaisser", Color.FromArgb(40, 167, 69), 140, 42)
            Dim btnProduit As Button = CreateStyledButton("Ajouter Produit", Color.FromArgb(23, 162, 184), 160, 42)
            Dim btnAppro As Button = CreateStyledButton("Approvisionner", Color.FromArgb(255, 152, 0), 160, 42)

            flowActions.Controls.AddRange({btnFacture, btnEncaisser, btnProduit, btnAppro})
            panelActions.Controls.AddRange({lblActionsTitle, flowActions})


            ' --- 4. KPI CARDS (Larges et Visibles) ---
            panelKpi = New Panel() With {.Height = 140, .Dock = DockStyle.Fill, .Margin = New Padding(0, 0, 0, 20)}
            Dim kpiTable As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 6, .RowCount = 1}
            For i As Integer = 0 To 5
                kpiTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 16.66F))
            Next

            lblKpiCAJour = CreerKpiCard(kpiTable, "CA DU JOUR", 0, Color.FromArgb(40, 167, 69))
            lblKpiCAMois = CreerKpiCard(kpiTable, "CA DU MOIS", 1, Color.FromArgb(40, 167, 69))
            lblKpiValeurStock = CreerKpiCard(kpiTable, "VALEUR STOCK", 2, Color.FromArgb(0, 120, 212))
            lblKpiStockCritique = CreerKpiCard(kpiTable, "STOCK CRITIQUE", 3, Color.FromArgb(220, 53, 69))
            lblKpiClients = CreerKpiCard(kpiTable, "CLIENTS ACTIFS", 4, Color.FromArgb(111, 66, 193))
            lblKpiFacturesAttente = CreerKpiCard(kpiTable, "EN ATTENTE", 5, Color.FromArgb(255, 152, 0))

            panelKpi.Controls.Add(kpiTable)
            contentLayout.Controls.Add(panelKpi, 0, 1)
            contentLayout.Controls.Add(panelActions, 0, 0)
            'contentLayout.Controls.Add(panelHeader, 0, 1)

            ' --- 5. GRAPHIQUES PRINCIPAUX (Ventes & Répartition) ---
            panelCharts = New Panel() With {.Height = 400, .Dock = DockStyle.Fill, .Margin = New Padding(0, 0, 0, 20)}
            Dim chartsTable As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1}
            chartsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            chartsTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))

            chartVentesMois = CreerChartModern("Ventes Mensuelles", SeriesChartType.SplineArea, Color.FromArgb(0, 120, 212))
            chartRevenus = CreerChartModern("Répartition Revenus", SeriesChartType.Doughnut, Color.Empty)

            chartsTable.Controls.Add(WrapInCard(chartVentesMois, "Ventes Mensuelles"), 0, 0)
            chartsTable.Controls.Add(WrapInCard(chartRevenus, "Répartition Revenus"), 1, 0)
            panelCharts.Controls.Add(chartsTable)
            contentLayout.Controls.Add(panelCharts, 0, 2)

            ' --- 6. GRAPHIQUES SECONDAIRES (Comparaison & Taux) ---
            Dim panelChartsSec As New Panel() With {.Height = 350, .Dock = DockStyle.Fill, .Margin = New Padding(0, 0, 0, 20)}
            Dim chartsSecTable As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1}
            chartsSecTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            chartsSecTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))

            chartComparaison = CreerChartModern("Comparaison Mois", SeriesChartType.Column, Color.FromArgb(40, 167, 69))
            chartTaux = CreerChartModern("Taux de Ventes", SeriesChartType.Bar, Color.FromArgb(255, 152, 0))

            chartsSecTable.Controls.Add(WrapInCard(chartComparaison, "Comparaison Mois"), 0, 0)
            chartsSecTable.Controls.Add(WrapInCard(chartTaux, "Taux de Ventes"), 1, 0)
            panelChartsSec.Controls.Add(chartsSecTable)
            contentLayout.Controls.Add(panelChartsSec, 0, 3)

            ' --- 7. ALERTES & ACTIVITÉS ---
            Dim panelBottom As New Panel() With {.Height = 300, .Dock = DockStyle.Fill}
            Dim bottomTable As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 1}
            bottomTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            bottomTable.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))

            gridAlertes = CreateStyledGrid()
            listActivites = CreateStyledListView()

            bottomTable.Controls.Add(WrapInCard(gridAlertes, "Alertes Intelligentes"), 0, 0)
            bottomTable.Controls.Add(WrapInCard(listActivites, "Activité Récente"), 1, 0)
            panelBottom.Controls.Add(bottomTable)
            contentLayout.Controls.Add(panelBottom, 0, 4)

            ' --- LOGIQUE & TIMERS ---
            kpiTargets = New Dictionary(Of Label, Decimal)()
            kpiCurrent = New Dictionary(Of Label, Decimal)()

            AddHandler btnNotif.Click, AddressOf AfficherNotifications
            AddHandler btnDeconnexion.Click, Sub() Me.Close()
            AddHandler btnFacture.Click, Sub() Ouvrir(New FacturationForm())
            AddHandler btnEncaisser.Click, Sub() Ouvrir(New CaisseForm())
            AddHandler btnProduit.Click, Sub() Ouvrir(New FormulaireProduits())
            AddHandler btnAppro.Click, Sub() Ouvrir(New FormulaireApprovisionnement())
            AddHandler gridAlertes.CellContentClick, AddressOf ActionsAlertes

            timerClock = New Timer() With {.Interval = 1000}
            AddHandler timerClock.Tick, AddressOf MajHorloge
            timerClock.Start()

            timerRefresh = New Timer() With {.Interval = 600000}
            AddHandler timerRefresh.Tick, Sub() Charger()
            timerRefresh.Start()

            timerAnim = New Timer() With {.Interval = 30}
            AddHandler timerAnim.Tick, AddressOf AnimerKpi

            ' Initialisation
            ConfigurerAlertes()
            Charger()
        End Sub

        ' --- Helpers de Design ---

        Private Function CreateStyledButton(text As String, color As Color, w As Integer, h As Integer) As Button
            Return New Button() With {
                .Text = text, .Width = w, .Height = h,
                .BackColor = color, .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat, .Font = FontLabel, .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 15, 0)
            }
        End Function

        Private Function CreerKpiCard(parent As TableLayoutPanel, titre As String, col As Integer, color As Color) As Label
            Dim p As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Margin = New Padding(5), .Padding = New Padding(15)}
            Dim lblT As New Label() With {.Text = titre, .Top = 15, .Left = 15, .AutoSize = True, .Font = FontSubTitle, .ForeColor = ColorTextSecondary}
            Dim lblV As New Label() With {.Text = "0", .Top = 50, .Left = 15, .AutoSize = True, .Font = FontValue, .ForeColor = color}
            p.Controls.AddRange({lblT, lblV})
            parent.Controls.Add(p, col, 0)
            Return lblV
        End Function

        Private Function WrapInCard(ctrl As Control, title As String) As Panel
            Dim p As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Margin = New Padding(5), .Padding = New Padding(20)}
            Dim lbl As New Label() With {.Text = title, .Dock = DockStyle.Top, .Height = 40, .Font = FontLabel, .ForeColor = ColorText}
            ctrl.Dock = DockStyle.Fill
            p.Controls.Add(ctrl)
            p.Controls.Add(lbl)
            Return p
        End Function

        Private Function CreerChartModern(titre As String, type As SeriesChartType, color As Color) As Chart
            Dim c As New Chart() With {.BackColor = Color.Transparent}
            Dim area As New ChartArea() With {.BackColor = Color.Transparent}
            area.AxisX.MajorGrid.LineColor = Color.FromArgb(245, 245, 245)
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(245, 245, 245)
            area.AxisX.LabelStyle.Font = New Font("Segoe UI", 8)
            area.AxisY.LabelStyle.Font = New Font("Segoe UI", 8)
            area.AxisX.LineColor = ColorBorder
            area.AxisY.LineColor = ColorBorder
            c.ChartAreas.Add(area)

            Dim s As New Series(titre) With {.ChartType = type, .Font = New Font("Segoe UI", 8)}
            If color <> Color.Empty Then s.Color = color
            If type = SeriesChartType.SplineArea Then
                s.Color = Color.FromArgb(180, Color.FromArgb(150, 59, 130, 246))
                s.BackGradientStyle = GradientStyle.TopBottom
                s.BorderWidth = 2


            End If
            If type = SeriesChartType.Doughnut Then
                If c.Series.Count > 0 Then
                    c.Series(0)("PieLabelStyle") = "Outside"
                    c.Series(0)("PieDrawingStyle") = "Concave"
                End If
            End If

            c.Series.Add(s)
            Return c
        End Function

        Private Function CreateStyledGrid() As DataGridView
            Dim dgv As New DataGridView() With {
                .BackgroundColor = Color.White, .BorderStyle = BorderStyle.None,
                .EnableHeadersVisualStyles = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AllowUserToAddRows = False, .ReadOnly = True, .RowHeadersVisible = False,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .GridColor = ColorBorder
            }
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250)
            dgv.ColumnHeadersDefaultCellStyle.Font = FontLabel
            dgv.ColumnHeadersHeight = 40
            dgv.DefaultCellStyle.Font = FontControl
            Return dgv
        End Function

        Private Function CreateStyledListView() As ListView
            Dim lv As New ListView() With {.BorderStyle = BorderStyle.None, .View = View.Details, .FullRowSelect = True, .Font = FontControl}
            lv.Columns.Add("Type", 120)
            lv.Columns.Add("Info", 200)
            lv.Columns.Add("Date", 120)
            Return lv
        End Function

        ' --- LOGIQUE MÉTIER (STRICTEMENT IDENTIQUE À L'ORIGINAL) ---

        Private Sub ConfigurerAlertes()
            gridAlertes.Columns.Clear()
            gridAlertes.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "TypeAlerte", .HeaderText = "Alerte"})
            gridAlertes.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "Cible", .HeaderText = "Cible"})
            gridAlertes.Columns.Add(New DataGridViewButtonColumn() With {.Name = "ActionVoir", .HeaderText = "Voir", .Text = "Voir", .UseColumnTextForButtonValue = True})
            gridAlertes.Columns.Add(New DataGridViewButtonColumn() With {.Name = "ActionCommander", .HeaderText = "Commander", .Text = "Commander", .UseColumnTextForButtonValue = True})
        End Sub

        Private Sub ActionsAlertes(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex < 0 Then Return
            Dim typeAlerte As String = Convert.ToString(gridAlertes.Rows(e.RowIndex).Cells(0).Value)
            Dim colName As String = gridAlertes.Columns(e.ColumnIndex).Name
            If colName = "ActionVoir" Then
                If typeAlerte.Contains("Facture") Then Ouvrir(New FormulaireFactures()) Else Ouvrir(New FormulaireProduits())
            ElseIf colName = "ActionCommander" Then
                Ouvrir(New FormulaireApprovisionnement())
            End If
        End Sub

        Private Sub Ouvrir(f As Form)
            f.StartPosition = FormStartPosition.CenterParent
            f.ShowDialog(Me)
        End Sub

        Private Sub MajHorloge(sender As Object, e As EventArgs)
            lblDateHeure.Text = Date.Now.ToString("dddd d MMMM yyyy, HH:mm:ss")
        End Sub

        Private Async Sub Charger()
            If _chargementEnCours OrElse IsDisposed Then
                Return
            End If

            _chargementEnCours = True
            Try
                Dim snapshot As DashboardSnapshot = Await Task.Run(Function() ChargerSnapshot())
                If snapshot Is Nothing OrElse IsDisposed OrElse Not IsHandleCreated Then
                    Return
                End If

                If Not String.IsNullOrWhiteSpace(snapshot.NomMagasin) Then
                    lblEntreprise.Text = snapshot.NomMagasin.ToUpper()
                End If

                Animer(lblKpiCAJour, snapshot.KpiCAJour)
                Animer(lblKpiCAMois, snapshot.KpiCAMois)
                Animer(lblKpiValeurStock, snapshot.KpiValeurStock)
                Animer(lblKpiStockCritique, snapshot.KpiStockCritique)
                Animer(lblKpiClients, snapshot.KpiClients)
                Animer(lblKpiFacturesAttente, snapshot.KpiFacturesAttente)

                UpdateChart(chartVentesMois, snapshot.VentesMois, "Mois", "CA")
                UpdateChart(chartRevenus, snapshot.RevenusParProduit, "Libelle", "Montant")
                UpdateChart(chartComparaison, snapshot.ComparatifMois, "Periode", "CA")

                If chartTaux IsNot Nothing AndAlso chartTaux.Series IsNot Nothing AndAlso chartTaux.Series.Count > 0 Then
                    Dim taux As Decimal = snapshot.TauxVenteStock
                    chartTaux.Series(0).Points.Clear()
                    chartTaux.Series(0).Points.AddXY("Vendu", taux * 100D)
                    chartTaux.Series(0).Points.AddXY("Stock", 100D - (taux * 100D))
                End If

                gridAlertes.DataSource = snapshot.Alertes

                listActivites.Items.Clear()
                If snapshot.Activites IsNot Nothing Then
                    For Each row As DataRow In snapshot.Activites.Rows
                        If row Is Nothing Then Continue For
                        Dim item As New ListViewItem(Convert.ToString(row("TypeAct")))
                        item.SubItems.Add(Convert.ToString(row("Info")))
                        item.SubItems.Add(Convert.ToDateTime(row("DateAct")).ToString("dd/MM HH:mm"))
                        listActivites.Items.Add(item)
                    Next
                End If

                btnNotif.Text = "Notifications (" & snapshot.NotificationsCount.ToString() & ")"
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireDashboard", "Charger", "Erreur lors du chargement du dashboard.", ex)
            Finally
                _chargementEnCours = False
            End Try
        End Sub

        Private Function ChargerSnapshot() As DashboardSnapshot
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim paramService As New ParametreService(New ParametreRepository(dal))
            Dim param As ParametreDTO = paramService.Charger()
            Dim service As New RapportService(dal)
            Dim notificationService As New NotificationService(dal)

            Dim seuil As Decimal = If(param Is Nothing, 0D, param.SeuilStockCritique)
            Dim jours As Integer = If(param Is Nothing, 30, param.AlerteExpirationJours)

            notificationService.SynchroniserAlertesMetier(seuil, jours, SessionUtilisateur.UtilisateurId)

            Return New DashboardSnapshot With {
                .NomMagasin = If(param Is Nothing, String.Empty, param.NomMagasin),
                .SeuilStockCritique = seuil,
                .AlerteExpirationJours = jours,
                .KpiCAJour = service.CAJournalier(Date.Now),
                .KpiCAMois = service.CAMensuel(Date.Now),
                .KpiValeurStock = service.ValeurStock(),
                .KpiStockCritique = service.StockCritique(seuil),
                .KpiClients = service.ClientsFideles(),
                .KpiFacturesAttente = service.FacturesEnAttente(),
                .VentesMois = service.VentesParMois(),
                .RevenusParProduit = service.RevenusParProduit(),
                .ComparatifMois = service.ComparatifMois(Date.Now),
                .TauxVenteStock = service.TauxVenteStock(),
                .Alertes = service.AlertesDetail(seuil, jours),
                .Activites = service.ActivitesRecentes(),
                .NotificationsCount = notificationService.ListerNonLues().Rows.Count
            }
        End Function

        Private Sub UpdateChart(chart As Chart, dt As DataTable, xCol As String, yCol As String)
            Try
                If chart Is Nothing OrElse chart.Series Is Nothing OrElse chart.Series.Count = 0 Then
                    Return
                End If
                If String.IsNullOrWhiteSpace(xCol) OrElse String.IsNullOrWhiteSpace(yCol) Then
                    Return
                End If
                If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                    chart.Series(0).Points.Clear()
                    Return
                End If
                If Not dt.Columns.Contains(xCol) OrElse Not dt.Columns.Contains(yCol) Then
                    chart.Series(0).Points.Clear()
                    Return
                End If

                chart.Series(0).Points.Clear()
                For Each row As DataRow In dt.Rows
                    If row Is Nothing OrElse row.IsNull(xCol) OrElse row.IsNull(yCol) Then
                        Continue For
                    End If
                    chart.Series(0).Points.AddXY(Convert.ToString(row(xCol)), Convert.ToDecimal(row(yCol)))
                Next
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireDashboard", "UpdateChart", "Erreur lors de la mise à jour d'un graphique.", ex)
            End Try
        End Sub

        'Private Sub AfficherNotifications(sender As Object, e As EventArgs)
        '    Try
        '        Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
        '        Dim dal As New DAL(cs)
        '        Dim notifRepo As New NotificationRepository(dal)
        '        Dim dt As DataTable = notifRepo.ListerNonLues()
        '        If dt.Rows.Count = 0 Then
        '            MessageBox.Show("Aucune nouvelle notification.")
        '            Return
        '        End If
        '        Dim contenu As String = ""
        '        For Each row As DataRow In dt.Rows
        '            contenu &= "- " & Convert.ToString(row("Message")) & " (" & Convert.ToDateTime(row("CreeLe")).ToString("dd/MM HH:mm") & ")" & Environment.NewLine
        '        Next
        '        MessageBox.Show(contenu, "Notifications")
        '        notifRepo.MarquerLues()
        '        btnNotif.Text = "Notifications (0)"
        '    Catch ex As Exception
        '        MessageBox.Show("Erreur: " & ex.Message)
        '    End Try
        'End Sub

        Private Sub AfficherNotifications(sender As Object, e As EventArgs)
            Try
                Dim frm As New FormulaireNotifications()
                frm.ShowDialog(Me)
                Charger()
            Catch ex As Exception
                MessageBox.Show("Erreur chargement notifications: " & ex.Message)
            End Try
        End Sub
        Private Sub Animer(lbl As Label, valeur As Decimal)
            kpiTargets(lbl) = valeur
            If Not kpiCurrent.ContainsKey(lbl) Then kpiCurrent(lbl) = 0D
            If Not timerAnim.Enabled Then timerAnim.Start()
        End Sub

        Private Sub AnimerKpi(sender As Object, e As EventArgs)
            Dim termine As Boolean = True
            For Each kv As KeyValuePair(Of Label, Decimal) In kpiTargets
                Dim lbl As Label = kv.Key
                Dim cible As Decimal = kv.Value
                Dim cur As Decimal = kpiCurrent(lbl)
                Dim stepVal As Decimal = Math.Max(cible / 20D, 1D)
                If cur < cible Then
                    cur = Math.Min(cible, cur + stepVal)
                    kpiCurrent(lbl) = cur
                    lbl.Text = cur.ToString("N0")
                    termine = False
                Else
                    lbl.Text = cible.ToString("N0")
                End If
            Next
            If termine Then timerAnim.Stop()
        End Sub
    End Class
End Namespace
