Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.Windows.Forms.DataVisualization.Charting
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class FormulaireClients
        Inherits Form

        ' --- Constantes de Design ---
        'Private ReadOnly ColorPrimary As Color = Color.FromArgb(63, 81, 181) ' Indigo
        'Private ReadOnly ColorSecondary As Color = Color.FromArgb(48, 63, 159)
        'Private ReadOnly ColorAccent As Color = Color.FromArgb(255, 64, 129)
        Private ReadOnly ColorBackground As Color = Color.FromArgb(245, 247, 250)
        Private ReadOnly ColorCard As Color = Color.White
        Private ReadOnly ColorText As Color = Color.FromArgb(33, 33, 33)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(117, 117, 117)
        Private ReadOnly ColorBorder As Color = Color.FromArgb(230, 230, 230)
        ' Private ReadOnly FontTitle As New Font("Segoe UI Semibold", 18.0F)
        Private ReadOnly FontSubTitle As New Font("Segoe UI", 10.0F)
        Private ReadOnly FontLabel As New Font("Segoe UI Semibold", 9.0F)
        Private ReadOnly FontControl As New Font("Segoe UI", 9.5F)





        Private ReadOnly ColorPrimary As Color = Color.FromArgb(52, 73, 94) ' Gris Foncé
        Private ReadOnly ColorSecondary As Color = Color.FromArgb(41, 128, 185) ' Bleu Moderne
        Private ReadOnly ColorAccent As Color = Color.FromArgb(39, 174, 96) ' Vert Succès
        Private ReadOnly ColorDanger As Color = Color.FromArgb(192, 57, 43) ' Rouge Annuler
        Private ReadOnly ColorBg As Color = Color.FromArgb(245, 247, 250) ' Gris très clair
        Private ReadOnly ColorWhite As Color = Color.White
        Private ReadOnly FontMain As New Font("Segoe UI", 10)
        Private ReadOnly FontBold As New Font("Segoe UI", 10, FontStyle.Bold)
        Private ReadOnly FontTitle As New Font("Segoe UI", 18.0F, FontStyle.Bold)
        Private ReadOnly FontTotal As New Font("Segoe UI", 22, FontStyle.Bold)


        ' --- Composants UI (Noms conservés) ---
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

        ' --- Nouveaux composants de structure ---
        Private ReadOnly panelHero As Panel
        Private ReadOnly lblHeroTitre As Label
        Private ReadOnly lblHeroSousTitre As Label
        Private ReadOnly panelFormCard As Panel
        Private ReadOnly panelActions As Panel

        Public Sub New()
            ' Configuration de base du formulaire
            Me.Text = "Gestion des Clients"
            Me.Width = 1300
            Me.Height = 850
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = ColorBackground
            Me.DoubleBuffered = True
            Me.AutoScaleMode = AutoScaleMode.Dpi
            Me.AutoScaleDimensions = New SizeF(96, 96)
            Me.MinimumSize = New Size(900, 650)

            ' --- Header / Hero Section ---
            panelHero = New Panel() With {.Dock = DockStyle.Top, .Height = 90, .BackColor = ColorPrimary}
            lblHeroTitre = New Label() With {.Text = "Répertoire Clients", .Left = 25, .Top = 18, .AutoSize = True, .Font = FontTitle, .ForeColor = Color.White}
            lblHeroSousTitre = New Label() With {.Text = "Gérez vos relations clients, suivez les limites de crédit et analysez les performances.", .Left = 27, .Top = 54, .AutoSize = True, .Font = FontSubTitle, .ForeColor = Color.FromArgb(210, 210, 255)}
            panelHero.Controls.Add(lblHeroTitre)
            panelHero.Controls.Add(lblHeroSousTitre)

            ' --- Carte de Formulaire (Saisie) ---
            panelFormCard = New Panel() With {.Dock = DockStyle.Top, .Height = 160, .BackColor = ColorCard, .Padding = New Padding(20)}

            txtNom = CreateStyledTextBox(20, 45, 200)
            txtTelephone = CreateStyledTextBox(240, 45, 160)
            txtEmail = CreateStyledTextBox(420, 45, 220)
            txtAdresse = CreateStyledTextBox(20, 105, 380)
            txtLimiteCredit = CreateStyledTextBox(420, 105, 120)
            chkActif = New CheckBox() With {.Left = 560, .Top = 108, .Text = "Client Actif", .Font = FontControl, .ForeColor = ColorText, .AutoSize = True}

            Dim saisie As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 3, .RowCount = 4, .MaximumSize = New Size(1080, 0)}
            saisie.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 38))
            saisie.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 27))
            saisie.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35))
            For Each hauteur As Integer In New Integer() {22, 34, 22, 34}
                saisie.RowStyles.Add(New RowStyle(SizeType.Absolute, hauteur))
            Next
            saisie.Controls.Add(CreateLabel("Nom complet", 0, 0), 0, 0)
            saisie.Controls.Add(CreateLabel("Téléphone", 0, 0), 1, 0)
            saisie.Controls.Add(CreateLabel("Email", 0, 0), 2, 0)
            saisie.Controls.Add(txtNom, 0, 1)
            saisie.Controls.Add(txtTelephone, 1, 1)
            saisie.Controls.Add(txtEmail, 2, 1)
            saisie.Controls.Add(CreateLabel("Adresse physique", 0, 0), 0, 2)
            saisie.Controls.Add(CreateLabel("Limite de crédit", 0, 0), 2, 2)
            saisie.Controls.Add(txtAdresse, 0, 3) : saisie.SetColumnSpan(txtAdresse, 2)
            Dim credit As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .WrapContents = False, .AutoScroll = True, .Margin = New Padding(0)}
            txtLimiteCredit.Width = 130 : txtLimiteCredit.TextAlign = HorizontalAlignment.Right
            chkActif.Margin = New Padding(12, 4, 0, 0)
            credit.Controls.Add(txtLimiteCredit) : credit.Controls.Add(chkActif)
            saisie.Controls.Add(credit, 2, 3)
            For Each champ As TextBox In New TextBox() {txtNom, txtTelephone, txtEmail, txtAdresse}
                champ.Dock = DockStyle.Top : champ.Margin = New Padding(0, 0, 18, 0)
            Next
            txtNom.MaximumSize = New Size(320, 0) : txtTelephone.MaximumSize = New Size(190, 0)
            txtEmail.MaximumSize = New Size(320, 0) : txtAdresse.MaximumSize = New Size(500, 0)
            panelFormCard.Controls.Add(saisie)

            ' --- Barre d'Actions ---
            panelActions = New Panel() With {.Dock = DockStyle.Top, .Height = 60, .BackColor = ColorCard, .Padding = New Padding(20, 10, 20, 10)}

            btnAjouter = CreateStyledButton("Ajouter", 20, 12, 100, ColorPrimary)
            btnModifier = CreateStyledButton("Modifier", 130, 12, 100, ColorSecondary)
            btnSupprimer = CreateStyledButton("Supprimer", 240, 12, 100, ColorDanger)
            btnRafraichir = CreateStyledButton("Rafraîchir", 350, 12, 100, ColorPrimary)
            btnStats = CreateStyledButton("Top Clients", 460, 12, 120, ColorPrimary)
            ThemeHelper1.StyliserActionCommerciale(btnAjouter, principale:=True)
            ThemeHelper1.StyliserActionCommerciale(btnModifier)
            ThemeHelper1.StyliserActionCommerciale(btnSupprimer, destructive:=True)
            ThemeHelper1.StyliserActionCommerciale(btnRafraichir)
            ThemeHelper1.StyliserActionCommerciale(btnStats)

            panelActions.Controls.Add(btnAjouter)
            panelActions.Controls.Add(btnModifier)
            panelActions.Controls.Add(btnSupprimer)
            panelActions.Controls.Add(btnRafraichir)
            panelActions.Controls.Add(btnStats)

            ' --- Grille Principale ---
            grid = CreateStyledGrid(0, 0, 0, 250)
            grid.Dock = DockStyle.Top
            grid.Height = 250

            ' --- Section Basse (Stats & Graphiques) ---

            Dim panelBas As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(10), .AutoScroll = True}

            gridActifs = CreateStyledGrid(0, 0, 600, 250)
            gridActifs.Dock = DockStyle.Fill
            chartProduits = New Chart() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .Size = New Size(200, 200)}


            chartProduits.ChartAreas.Clear()
            Dim area As New ChartArea("Produits")
            area.BackColor = Color.Transparent
            chartProduits.ChartAreas.Add(area)

            chartProduits.Series.Add(New Series("TopProduits") With {.ChartType = SeriesChartType.Doughnut, .Palette = ChartColorPalette.Pastel})
            chartProduits.Titles.Add(New Title("Répartition des Achats par Produit", Docking.Top, New Font("Segoe UI", 12.0F, FontStyle.Bold), ColorPrimary))
            chartProduits.Titles(0).Font = New Font("Segoe UI", 10, FontStyle.Bold)
            chartProduits.Legends.Add(New Legend With {.Docking = Docking.Bottom, .Font = New Font("Segoe UI", 8.5F), .IsTextAutoFit = False, .MaximumAutoSize = 40})
            chartProduits.Series("TopProduits").ToolTip = "#VALX : #VALY"


            Dim layout As New TableLayoutPanel()
            layout.Dock = DockStyle.Fill
            layout.ColumnCount = 2
            layout.RowCount = 1
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            layout.MinimumSize = New Size(780, 240)
            layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))

            layout.Controls.Add(gridActifs, 0, 0)
            layout.Controls.Add(chartProduits, 1, 0)

            panelBas.Controls.Add(layout)

            ' Ajout des contrôles au formulaire
            Me.Controls.Add(panelBas)
            Me.Controls.Add(grid)
            Me.Controls.Add(panelActions)
            Me.Controls.Add(panelFormCard)
            Me.Controls.Add(panelHero)

            ' --- Liaison des événements (Logique conservée) ---
            AddHandler btnAjouter.Click, AddressOf AjouterClient
            AddHandler btnModifier.Click, AddressOf ModifierClient
            AddHandler btnSupprimer.Click, AddressOf SupprimerClient
            AddHandler btnRafraichir.Click, AddressOf ChargerDonnees
            AddHandler btnStats.Click, AddressOf ChargerClientsActifs
            AddHandler grid.SelectionChanged, AddressOf ChargerSelection
            AddHandler gridActifs.SelectionChanged, AddressOf ChargerTopProduits

            ' --- Initialisation ---
            'ThemeHelper.AppliquerTheme(Me)
            timer = New Timer() With {.Interval = 600000}
            AddHandler timer.Tick, AddressOf ChargerClientsActifs
            timer.Start()

            ' Chargement initial
            AddHandler Me.Load, AddressOf ChargerDonnees
        End Sub

        ' --- Helpers de Design ---

        Private Function CreateLabel(text As String, x As Integer, y As Integer) As Label
            Return New Label() With {.Text = text, .Left = x, .Top = y, .AutoSize = True, .Font = FontLabel, .ForeColor = ColorTextSecondary}
        End Function

        Private Function CreateStyledTextBox(x As Integer, y As Integer, w As Integer) As TextBox
            Return New TextBox() With {.Left = x, .Top = y, .Width = w, .Font = FontControl, .BorderStyle = BorderStyle.FixedSingle}
        End Function

        Private Function CreateStyledButton(text As String, x As Integer, y As Integer, w As Integer, backColor As Color) As Button
            Dim btn As New Button() With {
                .Text = text, .Left = x, .Top = y, .Width = w, .Height = 38,
                .BackColor = backColor, .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat, .Font = FontLabel, .Cursor = Cursors.Hand
            }
            btn.FlatAppearance.BorderSize = 0
            Return btn
        End Function

        Private Function CreateStyledGrid(x As Integer, y As Integer, w As Integer, h As Integer) As DataGridView
            Dim dgv As New DataGridView() With {
                .Left = x, .Top = y, .Width = w, .Height = h,
                .BackgroundColor = Color.White, .BorderStyle = BorderStyle.None,
                .EnableHeadersVisualStyles = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AllowUserToAddRows = False, .ReadOnly = True, .RowHeadersVisible = False,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .GridColor = ColorBorder
            }
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(245, 245, 245)
            dgv.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            ThemeHelper1.StyliserEnteteCommerciale(dgv)
            dgv.ColumnHeadersHeight = 40
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 234, 246)
            dgv.DefaultCellStyle.SelectionForeColor = ColorPrimary
            dgv.DefaultCellStyle.Font = FontControl
            dgv.RowTemplate.Height = 34
            AddHandler dgv.DataBindingComplete, Sub() DimensionnerColonnes(dgv)
            Return dgv
        End Function

        ' Mise en forme seulement : ni requete ni modification de donnees.
        Private Sub DimensionnerColonnes(dgv As DataGridView)
            For Each colonne As DataGridViewColumn In dgv.Columns
                Select Case colonne.Name
                    Case "NomClient", "Adresse", "Email"
                        colonne.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                        colonne.MinimumWidth = If(colonne.Name = "NomClient", 160, 150)
                        colonne.FillWeight = If(colonne.Name = "Adresse", 30, 24)
                    Case Else
                        colonne.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                        colonne.Width = If(colonne.Name = "ClientId" OrElse colonne.Name = "EstActif" OrElse colonne.Name = "NbAchats", 75, 125)
                End Select
                If colonne.Name = "LimiteCredit" OrElse colonne.Name = "TotalAchats" OrElse colonne.Name = "MoyenneAchat" Then
                    colonne.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                    colonne.DefaultCellStyle.Format = "N2"
                End If
            Next
        End Sub

        ' --- LOGIQUE MÉTIER (STRICTEMENT IDENTIQUE À L'ORIGINAL) ---

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
            If grid.CurrentRow Is Nothing Then Return

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
                                    "cast(SUM(f.MontantTotal) as int ) AS TotalAchats, cast( AVG(f.MontantTotal) as int) AS MoyenneAchat " &
                                    "FROM Clients c JOIN FacturesVente f ON f.ClientId=c.ClientId " &
                                    "WHERE f.Statut='PAYEE' AND f.CreeLe >= DATEADD(DAY,-30,GETDATE()) " &
                                    "GROUP BY c.ClientId, c.NomClient ORDER BY TotalAchats DESC"
                gridActifs.DataSource = dal.ExecuterTable(sql, CommandType.Text, Nothing)
                'gridActifs.Columns("MoyenneAchat").DefaultCellStyle.Format = "NO"
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
                Dim sql As String = "SELECT TOP 10 p.Libelle, SUM(ISNULL(l.QuantiteBase, ISNULL(l.Quantite, 0))) AS Quantite " &
                                    "FROM LignesFactureVente l " &
                                    "JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId " &
                                    "JOIN Produits p ON p.ProduitId = l.ProduitId " &
                                    "WHERE f.ClientId=@id AND f.CreeLe >= DATEADD(DAY,-30,GETDATE()) AND f.Statut='PAYEE' " &
                                    "GROUP BY p.Libelle ORDER BY SUM(ISNULL(l.QuantiteBase, ISNULL(l.Quantite, 0))) DESC"

                ' Note: Utilisation de paramètres SQL selon votre structure originale
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
