Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting

Namespace DevCommerc8ak
    Public Class FormulaireDashboard
        Inherits Form

        Private ReadOnly panelSidebar As Panel
        Private ReadOnly panelHeader As Panel
        Private ReadOnly panelKpi As Panel
        Private ReadOnly panelCharts As Panel
        Private ReadOnly panelAlerts As Panel
        Private ReadOnly panelActivities As Panel
        Private ReadOnly panelActions As Panel

        Private ReadOnly lblEntreprise As Label
        Private ReadOnly lblUtilisateur As Label
        Private ReadOnly lblDateHeure As Label
        Private ReadOnly btnNotif As Button
        Private ReadOnly btnDeconnexion As Button
        Private ReadOnly btnToggleMenu As Button

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

        Private _menuCollapsed As Boolean

        Public Sub New()
            Me.Text = "Dashboard Admin"
            Me.Width = 1280
            Me.Height = 820
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = Color.White

            panelSidebar = New Panel() With {.Left = 0, .Top = 0, .Width = 200, .Height = Me.Height, .BackColor = Color.FromArgb(20, 30, 45)}
            panelHeader = New Panel() With {.Left = 200, .Top = 0, .Width = 1060, .Height = 60, .BackColor = Color.White}
            panelKpi = New Panel() With {.Left = 200, .Top = 60, .Width = 1060, .Height = 130, .BackColor = Color.White}
            panelCharts = New Panel() With {.Left = 200, .Top = 190, .Width = 1060, .Height = 340, .BackColor = Color.White}
            panelAlerts = New Panel() With {.Left = 200, .Top = 540, .Width = 520, .Height = 170, .BackColor = Color.White}
            panelActivities = New Panel() With {.Left = 740, .Top = 540, .Width = 520, .Height = 170, .BackColor = Color.White}
            panelActions = New Panel() With {.Left = 200, .Top = 720, .Width = 1060, .Height = 70, .BackColor = Color.White}

            btnToggleMenu = New Button() With {.Text = "≡", .Left = 10, .Top = 10, .Width = 40, .Height = 30, .BackColor = Color.FromArgb(30, 45, 65), .ForeColor = Color.White}
            AddHandler btnToggleMenu.Click, AddressOf ToggleMenu
            panelSidebar.Controls.Add(btnToggleMenu)

            AjouterBoutonSidebar("Dashboard", 60, True, Sub() End Sub)
            AjouterBoutonSidebar("Produits", 110, False, Sub() Ouvrir(New FormulaireProduits()))
            AjouterBoutonSidebar("Facturation", 160, False, Sub() Ouvrir(New FacturationForm()))
            AjouterBoutonSidebar("Caissier", 210, False, Sub() Ouvrir(New CaisseForm()))
            AjouterBoutonSidebar("Clients", 260, False, Sub() Ouvrir(New FormulaireClients()))
            AjouterBoutonSidebar("Approvisionnement", 310, False, Sub() Ouvrir(New FormulaireApprovisionnement()))
            AjouterBoutonSidebar("Rapports", 360, False, Sub() Ouvrir(New FormulaireRapports()))

            lblEntreprise = New Label() With {.Text = "Paons Rehoboth", .Left = 20, .Top = 18, .AutoSize = True, .Font = New Font("Segoe UI", 12, FontStyle.Bold)}
            lblUtilisateur = New Label() With {.Text = "Admin", .Left = 300, .Top = 20, .AutoSize = True}
            lblDateHeure = New Label() With {.Left = 500, .Top = 20, .AutoSize = True}
            btnNotif = New Button() With {.Text = "Notifications", .Left = 760, .Top = 15, .Width = 140, .BackColor = Color.FromArgb(20, 30, 45), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            btnNotif.FlatAppearance.BorderSize = 0
            btnDeconnexion = New Button() With {.Text = "Deconnexion", .Left = 880, .Top = 15, .Width = 110}
            AddHandler btnNotif.Click, AddressOf AfficherNotifications
            AddHandler btnDeconnexion.Click, Sub() Me.Close()

            panelHeader.Controls.Add(lblEntreprise)
            panelHeader.Controls.Add(lblUtilisateur)
            panelHeader.Controls.Add(lblDateHeure)
            panelHeader.Controls.Add(btnNotif)
            panelHeader.Controls.Add(btnDeconnexion)

            lblKpiCAJour = CreerKpiCard(panelKpi, "CA du jour", 10, Color.LightGreen)
            lblKpiCAMois = CreerKpiCard(panelKpi, "CA du mois", 185, Color.LightGreen)
            lblKpiValeurStock = CreerKpiCard(panelKpi, "Valeur du stock", 360, Color.LightBlue)
            lblKpiStockCritique = CreerKpiCard(panelKpi, "Stock critique", 535, Color.LightCoral)
            lblKpiClients = CreerKpiCard(panelKpi, "Clients fideles", 710, Color.Plum)
            lblKpiFacturesAttente = CreerKpiCard(panelKpi, "Factures en attente", 885, Color.Orange)

            chartVentesMois = CreerChart(panelCharts, 10, 10, 500, 155, "Ventes mensuelles", SeriesChartType.Line)
            chartRevenus = CreerChart(panelCharts, 530, 10, 510, 155, "Repartition revenus", SeriesChartType.Pie)
            chartComparaison = CreerChart(panelCharts, 10, 180, 500, 150, "Comparaison mois", SeriesChartType.Column)
            chartTaux = CreerChart(panelCharts, 530, 180, 510, 150, "Taux de ventes", SeriesChartType.Column)

            Dim lblAlertes As New Label() With {.Text = "Alertes intelligentes", .Left = 10, .Top = 10, .AutoSize = True, .Font = New Font("Segoe UI", 10, FontStyle.Bold)}
            gridAlertes = New DataGridView() With {.Left = 10, .Top = 35, .Width = 500, .Height = 120, .ReadOnly = True, .AutoGenerateColumns = False, .AllowUserToAddRows = False, .RowHeadersVisible = False}
            ConfigurerAlertes()
            panelAlerts.Controls.Add(lblAlertes)
            panelAlerts.Controls.Add(gridAlertes)

            Dim lblAct As New Label() With {.Text = "Activite recente", .Left = 10, .Top = 10, .AutoSize = True, .Font = New Font("Segoe UI", 10, FontStyle.Bold)}
            listActivites = New ListView() With {.Left = 10, .Top = 35, .Width = 500, .Height = 120, .View = View.Details, .FullRowSelect = True}
            listActivites.Columns.Add("Type", 140)
            listActivites.Columns.Add("Info", 220)
            listActivites.Columns.Add("Date", 120)
            panelActivities.Controls.Add(lblAct)
            panelActivities.Controls.Add(listActivites)

            Dim lblActions As New Label() With {.Text = "Actions rapides", .Left = 10, .Top = 10, .AutoSize = True, .Font = New Font("Segoe UI", 10, FontStyle.Bold)}
            panelActions.Controls.Add(lblActions)
            Dim btnFacture As New Button() With {.Text = "Nouvelle facture", .Left = 10, .Top = 30, .Width = 160}
            Dim btnEncaisser As New Button() With {.Text = "Encaisser", .Left = 180, .Top = 30, .Width = 120}
            Dim btnProduit As New Button() With {.Text = "Ajouter produit", .Left = 310, .Top = 30, .Width = 140}
            Dim btnAppro As New Button() With {.Text = "Approvisionner", .Left = 460, .Top = 30, .Width = 140}
            AddHandler btnFacture.Click, Sub() Ouvrir(New FacturationForm())
            AddHandler btnEncaisser.Click, Sub() Ouvrir(New CaisseForm())
            AddHandler btnProduit.Click, Sub() Ouvrir(New FormulaireProduits())
            AddHandler btnAppro.Click, Sub() Ouvrir(New FormulaireApprovisionnement())
            panelActions.Controls.Add(btnFacture)
            panelActions.Controls.Add(btnEncaisser)
            panelActions.Controls.Add(btnProduit)
            panelActions.Controls.Add(btnAppro)

            Me.Controls.Add(panelSidebar)
            Me.Controls.Add(panelHeader)
            Me.Controls.Add(panelKpi)
            Me.Controls.Add(panelCharts)
            Me.Controls.Add(panelAlerts)
            Me.Controls.Add(panelActivities)
            Me.Controls.Add(panelActions)

            kpiTargets = New Dictionary(Of Label, Decimal)()
            kpiCurrent = New Dictionary(Of Label, Decimal)()

            ThemeHelper.AppliquerTheme(Me)
            IconsHelper.AppliquerIconeFormulaire(Me)

            timerClock = New Timer() With {.Interval = 1000}
            AddHandler timerClock.Tick, AddressOf MajHorloge
            timerClock.Start()

            timerRefresh = New Timer() With {.Interval = 600000}
            AddHandler timerRefresh.Tick, AddressOf Charger
            timerRefresh.Start()

            timerAnim = New Timer() With {.Interval = 30}
            AddHandler timerAnim.Tick, AddressOf AnimerKpi

            Charger()
        End Sub

        Private Sub AjouterBoutonSidebar(titre As String, top As Integer, actif As Boolean, action As Action)
            Dim btn As New Button() With {.Text = titre, .Tag = titre, .Left = 10, .Top = top, .Width = 180, .Height = 36, .ForeColor = Color.White, .BackColor = If(actif, Color.FromArgb(40, 70, 110), Color.FromArgb(30, 45, 65))}
            AddHandler btn.Click, Sub() action()
            panelSidebar.Controls.Add(btn)
        End Sub

        Private Function CreerKpiCard(parent As Panel, titre As String, left As Integer, color As Color) As Label
            Dim card As New Panel() With {.Left = left, .Top = 10, .Width = 165, .Height = 110, .BackColor = Color.White, .BorderStyle = BorderStyle.FixedSingle}
            Dim lblTitre As New Label() With {.Text = titre, .Left = 10, .Top = 10, .AutoSize = True, .ForeColor = Color.FromArgb(80, 80, 80)}
            Dim lblVal As New Label() With {.Text = "0", .Left = 10, .Top = 45, .AutoSize = True, .Font = New Font("Segoe UI", 14, FontStyle.Bold), .ForeColor = color}
            card.Controls.Add(lblTitre)
            card.Controls.Add(lblVal)
            parent.Controls.Add(card)
            Return lblVal
        End Function

        Private Function CreerChart(parent As Panel, left As Integer, top As Integer, width As Integer, height As Integer, titre As String, typeChart As SeriesChartType) As Chart
            Dim chart As New Chart() With {.Left = left, .Top = top, .Width = width, .Height = height}
            Dim area As New ChartArea()
            chart.ChartAreas.Add(area)
            Dim series As New Series(titre) With {.ChartType = typeChart}
            chart.Series.Add(series)
            chart.Titles.Add(titre)
            parent.Controls.Add(chart)
            Return chart
        End Function

        Private Sub ConfigurerAlertes()
            gridAlertes.Columns.Clear()
            gridAlertes.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "TypeAlerte", .HeaderText = "Alerte", .Width = 160})
            gridAlertes.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "Cible", .HeaderText = "Cible", .Width = 200})
            gridAlertes.Columns.Add(New DataGridViewButtonColumn() With {.Name = "ActionVoir", .HeaderText = "Voir", .Text = "Voir", .UseColumnTextForButtonValue = True, .Width = 60})
            gridAlertes.Columns.Add(New DataGridViewButtonColumn() With {.Name = "ActionCommander", .HeaderText = "Commander", .Text = "Commander", .UseColumnTextForButtonValue = True, .Width = 90})
            AddHandler gridAlertes.CellContentClick, AddressOf ActionsAlertes
        End Sub

        Private Sub ActionsAlertes(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex < 0 Then Return
            Dim typeAlerte As String = Convert.ToString(gridAlertes.Rows(e.RowIndex).Cells("TypeAlerte").Value)
            Dim col As String = gridAlertes.Columns(e.ColumnIndex).Name

            If col = "ActionVoir" Then
                If typeAlerte.Contains("Facture") Then
                    Ouvrir(New FormulaireFactures())
                Else
                    Ouvrir(New FormulaireProduits())
                End If
            ElseIf col = "ActionCommander" Then
                Ouvrir(New FormulaireApprovisionnement())
            End If
        End Sub

        Private Sub Ouvrir(f As Form)
            f.StartPosition = FormStartPosition.CenterParent
            f.ShowDialog(Me)
        End Sub

        Private Sub ToggleMenu(sender As Object, e As EventArgs)
            _menuCollapsed = Not _menuCollapsed
            panelSidebar.Width = If(_menuCollapsed, 60, 200)
            For Each ctrl As Control In panelSidebar.Controls
                If TypeOf ctrl Is Button AndAlso ctrl IsNot btnToggleMenu Then
                    ctrl.Width = If(_menuCollapsed, 40, 180)
                    ctrl.Text = If(_menuCollapsed, "", Convert.ToString(ctrl.Tag))
                End If
            Next
        End Sub

        Private Sub MajHorloge(sender As Object, e As EventArgs)
            lblDateHeure.Text = Date.Now.ToString("dd/MM/yyyy HH:mm:ss")
        End Sub

        Private Sub Charger()
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim paramService As New ParametreService(New ParametreRepository(dal))
            Dim param As ParametreDTO = paramService.Charger()
            If param IsNot Nothing AndAlso param.NomMagasin <> "" Then
                lblEntreprise.Text = param.NomMagasin
            End If

            Dim service As New RapportService(dal)
            Dim caJour As Decimal = service.CAJournalier(Date.Now)
            Dim caMois As Decimal = service.CAMensuel(Date.Now)
            Dim stockCrit As Integer = service.StockCritique(If(param Is Nothing, 0D, param.SeuilStockCritique))
            Dim valStock As Decimal = service.ValeurStock()
            Dim fideles As Integer = service.ClientsFideles()
            Dim facturesAttente As Integer = service.FacturesEnAttente()

            Animer(lblKpiCAJour, caJour)
            Animer(lblKpiCAMois, caMois)
            Animer(lblKpiValeurStock, valStock)
            Animer(lblKpiStockCritique, stockCrit)
            Animer(lblKpiClients, fideles)
            Animer(lblKpiFacturesAttente, facturesAttente)

            Dim dtMois As DataTable = service.VentesParMois()
            chartVentesMois.Series(0).Points.Clear()
            For Each row As DataRow In dtMois.Rows
                chartVentesMois.Series(0).Points.AddXY(Convert.ToString(row("Mois")), Convert.ToDecimal(row("CA")))
            Next

            Dim dtRevenus As DataTable = service.RevenusParProduit()
            chartRevenus.Series(0).Points.Clear()
            For Each row As DataRow In dtRevenus.Rows
                chartRevenus.Series(0).Points.AddXY(Convert.ToString(row("Libelle")), Convert.ToDecimal(row("Montant")))
            Next

            Dim dtComp As DataTable = service.ComparatifMois(Date.Now)
            chartComparaison.Series(0).Points.Clear()
            For Each row As DataRow In dtComp.Rows
                chartComparaison.Series(0).Points.AddXY(Convert.ToString(row("Periode")), Convert.ToDecimal(row("CA")))
            Next

            Dim taux As Decimal = service.TauxVenteStock()
            chartTaux.Series(0).Points.Clear()
            chartTaux.Series(0).Points.AddXY("Vendu", taux * 100D)
            chartTaux.Series(0).Points.AddXY("Stock", 100D - (taux * 100D))

            Dim alertes As DataTable = service.AlertesDetail(If(param Is Nothing, 0D, param.SeuilStockCritique), If(param Is Nothing, 0, param.AlerteExpirationJours))
            gridAlertes.DataSource = alertes

            Dim activites As DataTable = service.ActivitesRecentes()
            listActivites.Items.Clear()
            For Each row As DataRow In activites.Rows
                Dim item As New ListViewItem(Convert.ToString(row("TypeAct")))
                item.SubItems.Add(Convert.ToString(row("Info")))
                item.SubItems.Add(Convert.ToDateTime(row("DateAct")).ToString("dd/MM HH:mm"))
                listActivites.Items.Add(item)
            Next

            Dim notificationService As New NotificationService(dal)
            notificationService.SynchroniserAlertesMetier(If(param Is Nothing, 0D, param.SeuilStockCritique), If(param Is Nothing, 30, param.AlerteExpirationJours), SessionUtilisateur.UtilisateurId)
            Dim notifications As DataTable = notificationService.ListerNonLues()
            btnNotif.Text = "Notifications (" & notifications.Rows.Count.ToString() & ")"
            btnNotif.BackColor = If(notifications.Rows.Count > 0, Color.FromArgb(220, 70, 70), Color.FromArgb(20, 30, 45))
        End Sub

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
            If Not kpiCurrent.ContainsKey(lbl) Then
                kpiCurrent(lbl) = 0D
            End If
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
                    lbl.Text = cur.ToString("0.##")
                    termine = False
                Else
                    lbl.Text = cible.ToString("0.##")
                End If
            Next
            If termine Then
                timerAnim.Stop()
            End If
        End Sub
    End Class
End Namespace
