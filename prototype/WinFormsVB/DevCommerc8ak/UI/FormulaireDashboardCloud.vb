Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireDashboardCloud
        Inherits Form

        Private ReadOnly lblTitre As Label
        Private ReadOnly lblStatut As Label
        Private ReadOnly btnActualiser As Button
        Private ReadOnly btnConsultation As Button
        Private ReadOnly lblCaJour As Label
        Private ReadOnly lblCaMois As Label
        Private ReadOnly lblSorties As Label
        Private ReadOnly lblDepenses As Label
        Private ReadOnly lblBenefice As Label
        Private ReadOnly lblStockFaible As Label
        Private ReadOnly gridTopProduits As DataGridView
        Private ReadOnly gridSortiesManuelles As DataGridView
        Private ReadOnly gridDepenses As DataGridView
        Private ReadOnly clientApi As DashboardApiClient

        Public Sub New()
            Me.Text = "Dashboard Cloud"
            Me.StartPosition = FormStartPosition.CenterParent
            Me.Size = New Size(1280, 820)
            Me.BackColor = Color.WhiteSmoke
            Me.DoubleBuffered = True

            clientApi = New DashboardApiClient()

            lblTitre = New Label() With {.Text = "Dashboard Cloud / Local", .Font = New Font("Segoe UI", 18, FontStyle.Bold), .AutoSize = True, .Left = 20, .Top = 18}
            lblStatut = New Label() With {.Text = "Connexion API: en attente", .Font = New Font("Segoe UI", 9, FontStyle.Regular), .AutoSize = True, .Left = 22, .Top = 54, .ForeColor = Color.DimGray}
            btnActualiser = New Button() With {.Text = "Actualiser", .Left = 1040, .Top = 18, .Width = 100, .Height = 34}
            btnConsultation = New Button() With {.Text = "Consultation", .Left = 1150, .Top = 18, .Width = 100, .Height = 34}

            Dim pnlKpi As New FlowLayoutPanel() With {.Left = 20, .Top = 90, .Width = 1220, .Height = 110, .AutoScroll = False, .WrapContents = False}
            lblCaJour = CreerKpi(pnlKpi, "CA Jour")
            lblCaMois = CreerKpi(pnlKpi, "CA Mois")
            lblSorties = CreerKpi(pnlKpi, "Sorties")
            lblDepenses = CreerKpi(pnlKpi, "Dépenses")
            lblBenefice = CreerKpi(pnlKpi, "Bénéfice")
            lblStockFaible = CreerKpi(pnlKpi, "Stock faible")

            gridTopProduits = CreerGrid(20, 220, 390, 520)
            gridSortiesManuelles = CreerGrid(425, 220, 410, 520)
            gridDepenses = CreerGrid(850, 220, 390, 520)

            Me.Controls.AddRange(New Control() {lblTitre, lblStatut, btnActualiser, btnConsultation, pnlKpi, gridTopProduits, gridSortiesManuelles, gridDepenses})

            AddHandler btnActualiser.Click, AddressOf Actualiser
            AddHandler btnConsultation.Click, AddressOf OuvrirConsultation
            AddHandler Me.Load, AddressOf FormulaireDashboardCloud_Load
        End Sub

        Private Sub FormulaireDashboardCloud_Load(sender As Object, e As EventArgs)
            Actualiser(Nothing, EventArgs.Empty)
        End Sub

        Private Sub Actualiser(sender As Object, e As EventArgs)
            Try
                If RemoteApiSession.IsAuthenticated() Then
                    Dim api As ApiJournalierDashboardResponse = clientApi.ChargerJournalier(Date.Today)
                    Dim mensuel As ApiMensuelDashboardResponse = clientApi.ChargerMensuel(Date.Today.Year, Date.Today.Month)
                    lblStatut.Text = "Connexion API: " & RemoteApiSession.UsernameCourant() & " / " & RemoteApiSession.RoleCourant()
                    lblCaJour.Text = FormatageGlobal.FormatMontant(api.CaDuJour)
                    lblCaMois.Text = FormatageGlobal.FormatMontant(mensuel.CaMensuel)
                    lblSorties.Text = FormatageGlobal.FormatNombre(api.TotalSortiesManuelles)
                    lblDepenses.Text = FormatageGlobal.FormatMontant(api.DepensesDuJour)
                    lblBenefice.Text = FormatageGlobal.FormatMontant(api.BeneficeEstime)
                    lblStockFaible.Text = FormatageGlobal.FormatNombre(api.AlertesStockFaible.Count)
                    gridTopProduits.DataSource = api.ProduitsVendus
                    gridSortiesManuelles.DataSource = api.SortiesManuelles
                    gridDepenses.DataSource = api.DepensesParCategorie
                Else
                    lblStatut.Text = "Connexion API: hors ligne, affichage local"
                    ChargerFallbackLocal()
                End If
            Catch ex As Exception
                lblStatut.Text = "API indisponible: " & ex.Message
                ChargerFallbackLocal()
            End Try
        End Sub

        Private Sub ChargerFallbackLocal()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim rapport As New RapportService(dal)
                lblCaJour.Text = FormatageGlobal.FormatMontant(rapport.CAJournalier(Date.Today))
                lblCaMois.Text = FormatageGlobal.FormatMontant(rapport.CAMensuel(Date.Today))
                lblSorties.Text = "0"
                lblDepenses.Text = "0"
                lblBenefice.Text = FormatageGlobal.FormatMontant(rapport.CAJournalier(Date.Today))
                lblStockFaible.Text = FormatageGlobal.FormatNombre(rapport.StockCritique(20D))
                gridTopProduits.DataSource = rapport.RevenusParProduit()
                gridSortiesManuelles.DataSource = rapport.ActivitesRecentes()
                gridDepenses.DataSource = rapport.RevenusParMode()
            Catch
            End Try
        End Sub

        Private Sub OuvrirConsultation(sender As Object, e As EventArgs)
            Using frm As New FormulaireConsultationDashboard()
                frm.ShowDialog(Me)
            End Using
        End Sub

        Private Function CreerKpi(parent As FlowLayoutPanel, titre As String) As Label
            Dim panel As New Panel() With {.Width = 185, .Height = 90, .BackColor = Color.White, .Margin = New Padding(0, 0, 12, 0)}
            Dim lblTitle As New Label() With {.Text = titre, .AutoSize = True, .Left = 12, .Top = 10, .ForeColor = Color.DimGray}
            Dim lblValue As New Label() With {.Text = "0", .AutoSize = True, .Left = 12, .Top = 34, .Font = New Font("Segoe UI", 14, FontStyle.Bold)}
            panel.Controls.Add(lblTitle)
            panel.Controls.Add(lblValue)
            parent.Controls.Add(panel)
            Return lblValue
        End Function

        Private Function CreerGrid(left As Integer, top As Integer, width As Integer, height As Integer) As DataGridView
            Dim grid As New DataGridView() With {
                .Left = left,
                .Top = top,
                .Width = width,
                .Height = height,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AutoGenerateColumns = True,
                .BackgroundColor = Color.White
            }
            Return grid
        End Function
    End Class
End Namespace
