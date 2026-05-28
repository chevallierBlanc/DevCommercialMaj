Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireConsultationDashboard
        Inherits Form

        Private tabs As TabControl
        Private dtpJour As DateTimePicker
        Private cmbMois As ComboBox
        Private cmbAnneeMensuel As ComboBox
        Private cmbAnneeAnnuel As ComboBox
        Private lblJourSynthese As Label
        Private lblMensuelSynthese As Label
        Private lblAnnuelSynthese As Label
        Private gridJourVentes As DataGridView
        Private gridJourSorties As DataGridView
        Private gridJourDepenses As DataGridView
        Private gridMensuelTopProduits As DataGridView
        Private gridMensuelDepenses As DataGridView
        Private gridAnnuelTopProduits As DataGridView
        Private gridAnnuelDepenses As DataGridView
        Private ReadOnly clientApi As DashboardApiClient

        Public Sub New()
            Me.Text = "Consultation Dashboard"
            Me.StartPosition = FormStartPosition.CenterParent
            Me.Size = New Size(1360, 860)
            Me.BackColor = Color.WhiteSmoke
            Me.DoubleBuffered = True

            clientApi = New DashboardApiClient()

            tabs = New TabControl() With {.Dock = DockStyle.Fill}
            tabs.TabPages.Add(CreerOngletJour())
            tabs.TabPages.Add(CreerOngletMensuel())
            tabs.TabPages.Add(CreerOngletAnnuel())
            Me.Controls.Add(tabs)

            AddHandler Me.Load, AddressOf FormulaireConsultationDashboard_Load
        End Sub

        Private Sub FormulaireConsultationDashboard_Load(sender As Object, e As EventArgs)
            RemplirAnnees()
            ActualiserJour(Nothing, EventArgs.Empty)
            ActualiserMensuel(Nothing, EventArgs.Empty)
            ActualiserAnnuel(Nothing, EventArgs.Empty)
        End Sub

        Private Function CreerOngletJour() As TabPage
            Dim page As New TabPage("Journalier")
            Dim btn As New Button() With {.Text = "Actualiser", .Left = 20, .Top = 20, .Width = 100}
            dtpJour = New DateTimePicker() With {.Left = 140, .Top = 21, .Width = 120, .Format = DateTimePickerFormat.Short}
            lblJourSynthese = New Label() With {.Left = 280, .Top = 25, .Width = 950, .Height = 24, .AutoSize = False}
            gridJourVentes = CreerGrid(20, 70, 400, 680)
            gridJourSorties = CreerGrid(440, 70, 400, 680)
            gridJourDepenses = CreerGrid(860, 70, 450, 680)
            page.Controls.AddRange(New Control() {btn, dtpJour, lblJourSynthese, gridJourVentes, gridJourSorties, gridJourDepenses})
            AddHandler btn.Click, AddressOf ActualiserJour
            AddHandler dtpJour.ValueChanged, AddressOf ActualiserJour
            Return page
        End Function

        Private Function CreerOngletMensuel() As TabPage
            Dim page As New TabPage("Mensuel")
            Dim btn As New Button() With {.Text = "Actualiser", .Left = 20, .Top = 20, .Width = 100}
            cmbMois = New ComboBox() With {.Left = 140, .Top = 20, .Width = 120, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbAnneeMensuel = New ComboBox() With {.Left = 280, .Top = 20, .Width = 120, .DropDownStyle = ComboBoxStyle.DropDownList}
            lblMensuelSynthese = New Label() With {.Left = 420, .Top = 24, .Width = 880, .Height = 24, .AutoSize = False}
            gridMensuelTopProduits = CreerGrid(20, 70, 610, 680)
            gridMensuelDepenses = CreerGrid(650, 70, 660, 680)
            page.Controls.AddRange(New Control() {btn, cmbMois, cmbAnneeMensuel, lblMensuelSynthese, gridMensuelTopProduits, gridMensuelDepenses})
            AddHandler btn.Click, AddressOf ActualiserMensuel
            AddHandler cmbMois.SelectedIndexChanged, AddressOf ActualiserMensuel
            AddHandler cmbAnneeMensuel.SelectedIndexChanged, AddressOf ActualiserMensuel
            Return page
        End Function

        Private Function CreerOngletAnnuel() As TabPage
            Dim page As New TabPage("Annuel")
            Dim btn As New Button() With {.Text = "Actualiser", .Left = 20, .Top = 20, .Width = 100}
            cmbAnneeAnnuel = New ComboBox() With {.Left = 140, .Top = 20, .Width = 120, .DropDownStyle = ComboBoxStyle.DropDownList}
            lblAnnuelSynthese = New Label() With {.Left = 280, .Top = 24, .Width = 980, .Height = 24, .AutoSize = False}
            gridAnnuelTopProduits = CreerGrid(20, 70, 610, 680)
            gridAnnuelDepenses = CreerGrid(650, 70, 660, 680)
            page.Controls.AddRange(New Control() {btn, cmbAnneeAnnuel, lblAnnuelSynthese, gridAnnuelTopProduits, gridAnnuelDepenses})
            AddHandler btn.Click, AddressOf ActualiserAnnuel
            AddHandler cmbAnneeAnnuel.SelectedIndexChanged, AddressOf ActualiserAnnuel
            Return page
        End Function

        Private Sub RemplirAnnees()
            cmbMois.Items.Clear()
            For mois As Integer = 1 To 12
                cmbMois.Items.Add(mois.ToString("00"))
            Next
            cmbMois.SelectedItem = Date.Today.Month.ToString("00")

            cmbAnneeMensuel.Items.Clear()
            cmbAnneeAnnuel.Items.Clear()
            For annee As Integer = Date.Today.Year - 3 To Date.Today.Year + 1
                cmbAnneeMensuel.Items.Add(annee.ToString())
                cmbAnneeAnnuel.Items.Add(annee.ToString())
            Next
            cmbAnneeMensuel.SelectedItem = Date.Today.Year.ToString()
            cmbAnneeAnnuel.SelectedItem = Date.Today.Year.ToString()
        End Sub

        Private Sub ActualiserJour(sender As Object, e As EventArgs)
            Try
                If RemoteApiSession.IsAuthenticated() Then
                    Dim api As ApiJournalierDashboardResponse = clientApi.ChargerJournalier(dtpJour.Value.Date)
                    lblJourSynthese.Text = "CA: " & FormatageGlobal.FormatMontant(api.CaDuJour) &
                        " | Entrées: " & FormatageGlobal.FormatNombre(api.TotalEntrees) &
                        " | Ventes: " & FormatageGlobal.FormatNombre(api.TotalVentes) &
                        " | Sorties manuelles: " & FormatageGlobal.FormatNombre(api.TotalSortiesManuelles) &
                        " | Pertes: " & FormatageGlobal.FormatNombre(api.TotalPertes) &
                        " | Bénéfice: " & FormatageGlobal.FormatMontant(api.BeneficeEstime)
                    gridJourVentes.DataSource = api.ProduitsVendus
                    gridJourSorties.DataSource = api.SortiesManuelles
                    gridJourDepenses.DataSource = api.DepensesParCategorie
                Else
                    lblJourSynthese.Text = "API non connectée."
                End If
            Catch ex As Exception
                lblJourSynthese.Text = "Erreur jour: " & ex.Message
            End Try
        End Sub

        Private Sub ActualiserMensuel(sender As Object, e As EventArgs)
            Try
                If cmbMois.SelectedItem Is Nothing OrElse cmbAnneeMensuel.SelectedItem Is Nothing Then
                    Return
                End If
                Dim mois As Integer = Convert.ToInt32(cmbMois.SelectedItem)
                Dim annee As Integer = Convert.ToInt32(cmbAnneeMensuel.SelectedItem)
                If RemoteApiSession.IsAuthenticated() Then
                    Dim api As ApiMensuelDashboardResponse = clientApi.ChargerMensuel(annee, mois)
                    lblMensuelSynthese.Text = "CA: " & FormatageGlobal.FormatMontant(api.CaMensuel) &
                        " | Dépenses: " & FormatageGlobal.FormatMontant(api.DepensesMensuelles) &
                        " | Entrées: " & FormatageGlobal.FormatNombre(api.TotalEntrees) &
                        " | Sorties: " & FormatageGlobal.FormatNombre(api.TotalSortiesManuelles) &
                        " | Bénéfice: " & FormatageGlobal.FormatMontant(api.BeneficeEstime)
                    gridMensuelTopProduits.DataSource = api.TopProduits
                    gridMensuelDepenses.DataSource = api.TopDepenses
                Else
                    lblMensuelSynthese.Text = "API non connectée."
                End If
            Catch ex As Exception
                lblMensuelSynthese.Text = "Erreur mois: " & ex.Message
            End Try
        End Sub

        Private Sub ActualiserAnnuel(sender As Object, e As EventArgs)
            Try
                If cmbAnneeAnnuel.SelectedItem Is Nothing Then
                    Return
                End If
                Dim annee As Integer = Convert.ToInt32(cmbAnneeAnnuel.SelectedItem)
                If RemoteApiSession.IsAuthenticated() Then
                    Dim api As ApiAnnuelDashboardResponse = clientApi.ChargerAnnuel(annee)
                    lblAnnuelSynthese.Text = "CA: " & FormatageGlobal.FormatMontant(api.CaAnnuel) &
                        " | Dépenses: " & FormatageGlobal.FormatMontant(api.DepensesAnnuelles) &
                        " | Entrées: " & FormatageGlobal.FormatNombre(api.TotalEntrees) &
                        " | Ventes: " & FormatageGlobal.FormatNombre(api.TotalVentes) &
                        " | Bénéfice: " & FormatageGlobal.FormatMontant(api.BeneficeEstime)
                    gridAnnuelTopProduits.DataSource = api.TopProduits
                    gridAnnuelDepenses.DataSource = api.CategoriesDepensesGourmandes
                Else
                    lblAnnuelSynthese.Text = "API non connectée."
                End If
            Catch ex As Exception
                lblAnnuelSynthese.Text = "Erreur année: " & ex.Message
            End Try
        End Sub

        Private Function CreerGrid(left As Integer, top As Integer, width As Integer, height As Integer) As DataGridView
            Return New DataGridView() With {
                .Left = left,
                .Top = top,
                .Width = width,
                .Height = height,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AutoGenerateColumns = True,
                .BackgroundColor = Color.White
            }
        End Function
    End Class
End Namespace
