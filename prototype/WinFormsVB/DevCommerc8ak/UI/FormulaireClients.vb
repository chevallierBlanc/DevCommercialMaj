Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting

Namespace DevCommerc8ak
    Public Class FormulaireClients
        Inherits Form

        Private ReadOnly grid As DataGridView
        Private ReadOnly gridActifs As DataGridView
        Private ReadOnly btnAjouter As Button
        Private ReadOnly btnModifier As Button
        Private ReadOnly btnSupprimer As Button
        Private ReadOnly btnRafraichir As Button
        Private ReadOnly btnStats As Button

        Private ReadOnly txtNom As TextBox
        Private ReadOnly txtTelephone As TextBox
        Private ReadOnly txtEmail As TextBox
        Private ReadOnly txtAdresse As TextBox
        Private ReadOnly txtLimiteCredit As TextBox
        Private ReadOnly chkActif As CheckBox

        Private ReadOnly chartProduits As Chart
        Private ReadOnly timer As Timer

        Public Sub New()
            Me.Text = "Clients"
            Me.Width = 1200
            Me.Height = 700

            Dim panelForm As New Panel() With {.Dock = DockStyle.Top, .Height = 140}
            Dim panelBoutons As New Panel() With {.Dock = DockStyle.Top, .Height = 45}

            txtNom = New TextBox() With {.Left = 20, .Top = 25, .Width = 200}
            txtTelephone = New TextBox() With {.Left = 240, .Top = 25, .Width = 160}
            txtEmail = New TextBox() With {.Left = 420, .Top = 25, .Width = 220}
            txtAdresse = New TextBox() With {.Left = 20, .Top = 75, .Width = 380}
            txtLimiteCredit = New TextBox() With {.Left = 420, .Top = 75, .Width = 120}
            chkActif = New CheckBox() With {.Left = 560, .Top = 78, .Text = "Actif"}

            panelForm.Controls.Add(New Label() With {.Text = "Nom", .Left = 20, .Top = 5, .AutoSize = True})
            panelForm.Controls.Add(New Label() With {.Text = "Telephone", .Left = 240, .Top = 5, .AutoSize = True})
            panelForm.Controls.Add(New Label() With {.Text = "Email", .Left = 420, .Top = 5, .AutoSize = True})
            panelForm.Controls.Add(New Label() With {.Text = "Adresse", .Left = 20, .Top = 55, .AutoSize = True})
            panelForm.Controls.Add(New Label() With {.Text = "Limite credit", .Left = 420, .Top = 55, .AutoSize = True})

            panelForm.Controls.Add(txtNom)
            panelForm.Controls.Add(txtTelephone)
            panelForm.Controls.Add(txtEmail)
            panelForm.Controls.Add(txtAdresse)
            panelForm.Controls.Add(txtLimiteCredit)
            panelForm.Controls.Add(chkActif)

            btnAjouter = New Button() With {.Text = "Ajouter", .Left = 20, .Top = 8, .Width = 100}
            btnModifier = New Button() With {.Text = "Modifier", .Left = 130, .Top = 8, .Width = 100}
            btnSupprimer = New Button() With {.Text = "Supprimer", .Left = 240, .Top = 8, .Width = 100}
            btnRafraichir = New Button() With {.Text = "Rafraichir", .Left = 350, .Top = 8, .Width = 100}
            btnStats = New Button() With {.Text = "Stats", .Left = 460, .Top = 8, .Width = 100}

            AddHandler btnAjouter.Click, AddressOf AjouterClient
            AddHandler btnModifier.Click, AddressOf ModifierClient
            AddHandler btnSupprimer.Click, AddressOf SupprimerClient
            AddHandler btnRafraichir.Click, AddressOf ChargerDonnees
            AddHandler btnStats.Click, AddressOf ChargerClientsActifs

            panelBoutons.Controls.Add(btnAjouter)
            panelBoutons.Controls.Add(btnModifier)
            panelBoutons.Controls.Add(btnSupprimer)
            panelBoutons.Controls.Add(btnRafraichir)
            panelBoutons.Controls.Add(btnStats)

            grid = New DataGridView() With {
                .Dock = DockStyle.Top,
                .Height = 200,
                .AutoGenerateColumns = True,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .ReadOnly = True
            }
            AddHandler grid.SelectionChanged, AddressOf ChargerSelection

            gridActifs = New DataGridView() With {.Dock = DockStyle.Left, .Width = 600, .AutoGenerateColumns = True, .ReadOnly = True}
            AddHandler gridActifs.SelectionChanged, AddressOf ChargerTopProduits

            chartProduits = New Chart() With {.Dock = DockStyle.Fill, .MinimumSize = New Size(300, 200)}
            Dim area As New ChartArea("Produits")
            chartProduits.ChartAreas.Add(area)
            chartProduits.Series.Add(New Series("TopProduits") With {.ChartType = SeriesChartType.Pie})

            Dim panelBas As New Panel() With {.Dock = DockStyle.Fill}
            panelBas.Controls.Add(chartProduits)
            panelBas.Controls.Add(gridActifs)

            Me.Controls.Add(panelBas)
            Me.Controls.Add(grid)
            Me.Controls.Add(panelBoutons)
            Me.Controls.Add(panelForm)

            ThemeHelper.AppliquerTheme(Me)

            timer = New Timer() With {.Interval = 600000}
            AddHandler timer.Tick, AddressOf ChargerClientsActifs
            timer.Start()
        End Sub

        Private Function ObtenirService() As ClientService
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New ClientRepository(dal)
            Return New ClientService(repo)
        End Function

        Private Sub ChargerDonnees(sender As Object, e As EventArgs)
            Try
                Dim service As ClientService = ObtenirService()
                grid.DataSource = service.Lister()
            Catch ex As Exception
                MessageBox.Show("Erreur chargement clients: " & ex.Message)
            End Try
        End Sub

        Private Sub AjouterClient(sender As Object, e As EventArgs)
            Try
                If Not ValiderFormulaire() Then Return
                Dim service As ClientService = ObtenirService()
                Dim client As New Client With {
                    .NomClient = txtNom.Text.Trim(),
                    .Telephone = txtTelephone.Text.Trim(),
                    .Email = txtEmail.Text.Trim(),
                    .Adresse = txtAdresse.Text.Trim(),
                    .LimiteCredit = Decimal.Parse(If(txtLimiteCredit.Text.Trim() = "", "0", txtLimiteCredit.Text.Trim())),
                    .EstActif = chkActif.Checked
                }
                service.Ajouter(client)
                ChargerDonnees(sender, e)
            Catch ex As Exception
                MessageBox.Show("Erreur ajout client: " & ex.Message)
            End Try
        End Sub

        Private Sub ModifierClient(sender As Object, e As EventArgs)
            Try
                If grid.CurrentRow Is Nothing Then
                    MessageBox.Show("Selectionnez un client.")
                    Return
                End If
                If Not ValiderFormulaire() Then Return

                Dim id As Integer = Convert.ToInt32(grid.CurrentRow.Cells("ClientId").Value)
                Dim service As ClientService = ObtenirService()
                Dim client As New Client With {
                    .ClientId = id,
                    .NomClient = txtNom.Text.Trim(),
                    .Telephone = txtTelephone.Text.Trim(),
                    .Email = txtEmail.Text.Trim(),
                    .Adresse = txtAdresse.Text.Trim(),
                    .LimiteCredit = Decimal.Parse(If(txtLimiteCredit.Text.Trim() = "", "0", txtLimiteCredit.Text.Trim())),
                    .EstActif = chkActif.Checked
                }
                service.MettreAJour(client)
                ChargerDonnees(sender, e)
            Catch ex As Exception
                MessageBox.Show("Erreur modification client: " & ex.Message)
            End Try
        End Sub

        Private Sub SupprimerClient(sender As Object, e As EventArgs)
            Try
                If grid.CurrentRow Is Nothing Then
                    MessageBox.Show("Selectionnez un client.")
                    Return
                End If

                Dim id As Integer = Convert.ToInt32(grid.CurrentRow.Cells("ClientId").Value)
                Dim service As ClientService = ObtenirService()
                service.Supprimer(id)
                ChargerDonnees(sender, e)
            Catch ex As Exception
                MessageBox.Show("Erreur suppression client: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerSelection(sender As Object, e As EventArgs)
            If grid.CurrentRow Is Nothing Then
                Return
            End If

            txtNom.Text = Convert.ToString(grid.CurrentRow.Cells("NomClient").Value)
            txtTelephone.Text = Convert.ToString(grid.CurrentRow.Cells("Telephone").Value)
            txtEmail.Text = Convert.ToString(grid.CurrentRow.Cells("Email").Value)
            txtAdresse.Text = Convert.ToString(grid.CurrentRow.Cells("Adresse").Value)
            txtLimiteCredit.Text = Convert.ToString(grid.CurrentRow.Cells("LimiteCredit").Value)
            chkActif.Checked = Convert.ToBoolean(grid.CurrentRow.Cells("EstActif").Value)
        End Sub

        Private Function ValiderFormulaire() As Boolean
            If txtNom.Text.Trim() = "" Then
                MessageBox.Show("Nom client obligatoire.")
                Return False
            End If
            Return True
        End Function

        Private Sub ChargerClientsActifs(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim sql As String = "SELECT TOP 20 c.ClientId, c.NomClient, COUNT(*) AS NbAchats, " &
                                    "SUM(f.MontantTotal) AS TotalAchats, AVG(f.MontantTotal) AS MoyenneAchat " &
                                    "FROM Clients c JOIN FacturesVente f ON f.ClientId=c.ClientId " &
                                    "WHERE f.Statut='PAYEE' AND f.CreeLe >= DATEADD(DAY,-30,GETDATE()) " &
                                    "GROUP BY c.ClientId, c.NomClient ORDER BY TotalAchats DESC"
                gridActifs.DataSource = dal.ExecuterTable(sql, CommandType.Text, Nothing)
            Catch ex As Exception
                MessageBox.Show("Erreur clients actifs: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerTopProduits(sender As Object, e As EventArgs)
            Try
                If gridActifs.CurrentRow Is Nothing Then Return
                Dim clientId As Integer = Convert.ToInt32(gridActifs.CurrentRow.Cells("ClientId").Value)

                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim sql As String = "SELECT TOP 10 p.Libelle, SUM(l.Quantite) AS Quantite " &
                                    "FROM LignesFactureVente l " &
                                    "JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId " &
                                    "JOIN Produits p ON p.ProduitId = l.ProduitId " &
                                    "WHERE f.ClientId=@id AND f.CreeLe >= DATEADD(DAY,-30,GETDATE()) AND f.Statut='PAYEE' " &
                                    "GROUP BY p.Libelle ORDER BY SUM(l.Quantite) DESC"
                Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                    New System.Data.SqlClient.SqlParameter("@id", clientId)
                }
                Dim dt As DataTable = dal.ExecuterTable(sql, CommandType.Text, p)

                chartProduits.Series("TopProduits").Points.Clear()
                For Each row As DataRow In dt.Rows
                    chartProduits.Series("TopProduits").Points.AddXY(Convert.ToString(row("Libelle")), Convert.ToDecimal(row("Quantite")))
                Next
            Catch
            End Try
        End Sub
    End Class
End Namespace
