Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class CaisseForm
        Inherits Form

        ' --- Couleurs du Thème ---
        Private ReadOnly ColorPrimary As Color = Color.FromArgb(41, 128, 185) ' Bleu Moderne
        Private ReadOnly ColorSecondary As Color = Color.FromArgb(52, 73, 94) ' Gris Foncé
        Private ReadOnly ColorAccent As Color = Color.FromArgb(39, 174, 96) ' Vert Succès
        Private ReadOnly ColorDanger As Color = Color.FromArgb(192, 57, 43) ' Rouge Annuler
        Private ReadOnly ColorBg As Color = Color.FromArgb(245, 247, 250) ' Gris très clair
        Private ReadOnly ColorWhite As Color = Color.White
        Private ReadOnly FontMain As New Font("Segoe UI", 10)
        Private ReadOnly FontBold As New Font("Segoe UI", 10, FontStyle.Bold)
        Private ReadOnly FontTitle As New Font("Segoe UI", 14, FontStyle.Bold)
        Private ReadOnly FontTotal As New Font("Segoe UI", 22, FontStyle.Bold)

        ' --- Composants ---
        Private ReadOnly txtRecherche As TextBox
        Private ReadOnly chkDate As CheckBox
        Private ReadOnly dtDate As DateTimePicker
        Private ReadOnly btnActualiser As Button
        Private ReadOnly gridFactures As DataGridView

        Private ReadOnly lblNumeroFacture As Label
        Private ReadOnly lblClient As Label
        Private ReadOnly lblDateFacture As Label
        Private ReadOnly gridDetails As DataGridView

        Private ReadOnly lblTotal As Label
        Private ReadOnly txtMontantRecu As TextBox
        Private ReadOnly lblMonnaie As Label
        Private ReadOnly cmbMode As ComboBox
        Private ReadOnly cmbDevise As ComboBox
        Private ReadOnly txtReference As TextBox
        Private ReadOnly btnEncaisser As Button
        Private ReadOnly btnImprimer As Button
        Private ReadOnly btnAnnuler As Button
        Private ReadOnly btnAnnulerFacture As Button

        ' --- Données ---
        Private _param As ParametreDTO
        Private _totalCourant As Decimal
        Private _dernierTicket As TicketData
        Private _isRefreshingFromEvent As Boolean
        Private _dataMonitor As DataChangeMonitorService
        Private _impressionDepuisApercuEnCours As Boolean

        Private Class TicketData
            Public Property Numero As String
            Public Property Client As String
            Public Property Telephone As String
            Public Property DateFacture As Date
            Public Property Total As Decimal
            Public Property MontantRecu As Decimal
            Public Property Monnaie As Decimal
            Public Property Devise As String
            Public Property ModePaiement As String
            Public Property ReferencePaiement As String
            Public Property Caissier As String
            Public Property Lignes As DataTable
        End Class

        Public Sub New()
            ' Configuration de la Form
            Me.BackColor = ColorBg
            Me.Text = "Terminal de Caisse Professionnel"
            Me.Width = 1300
            Me.Height = 800
            Me.Font = FontMain
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.FormBorderStyle = FormBorderStyle.Sizable
            Me.MaximizeBox = True
            Me.KeyPreview = True
            Me.AutoScaleMode = AutoScaleMode.Dpi
            Me.AutoScroll = True
            Me.MinimumSize = New Size(1040, 700)

            ' --- Header Panel ---
            Dim pnlHeader As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 60,
                .BackColor = ColorSecondary
            }
            Dim lblAppTitle As New Label() With {
                .Text = "GESTION DE LA CAISSE",
                .ForeColor = ColorWhite,
                .Font = FontTitle,
                .AutoSize = True,
                .Left = 20,
                .Top = 15
            }
            pnlHeader.Controls.Add(lblAppTitle)

            ' --- Main Container ---
            Dim pnlMain As New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(15),
                .AutoScroll = True
            }

            ' --- Colonne Gauche (Liste des Factures) ---
            Dim pnlGauche As New Panel() With {
                .Width = 380,
                .Dock = DockStyle.Left,
                .BackColor = ColorWhite,
                .Padding = New Padding(10)
            }
            pnlGauche.BorderStyle = BorderStyle.FixedSingle

            Dim lblRechercheTitre As New Label() With {.Text = "RECHERCHE FACTURE", .Dock = DockStyle.Top, .Height = 25, .Font = FontBold, .ForeColor = ColorPrimary}
            txtRecherche = New TextBox() With {.Dock = DockStyle.Top, .Height = 30, .BorderStyle = BorderStyle.FixedSingle, .Font = New Font("Segoe UI", 11)}

            Dim pnlFiltreDate As New Panel() With {.Dock = DockStyle.Top, .Height = 45, .Padding = New Padding(0, 10, 0, 0)}
            chkDate = New CheckBox() With {.Text = "Par Date", .Left = 0, .Top = 12, .AutoSize = True, .Font = FontMain}
            dtDate = New DateTimePicker() With {.Left = 85, .Top = 10, .Width = 120, .Format = DateTimePickerFormat.Short}
            btnActualiser = New Button() With {
                .Text = "Actualiser", .Left = 215, .Top = 8, .Width = 100, .Height = 28,
                .FlatStyle = FlatStyle.Flat, .BackColor = ColorPrimary, .ForeColor = ColorWhite, .Cursor = Cursors.Hand
            }
            btnActualiser.FlatAppearance.BorderSize = 0
            pnlFiltreDate.Controls.AddRange({chkDate, dtDate, btnActualiser})

            gridFactures = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True, .BorderStyle = BorderStyle.None,
                .BackgroundColor = ColorWhite, .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AllowUserToAddRows = False, .RowHeadersVisible = False,
                .AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle() With {.BackColor = Color.FromArgb(245, 245, 245)}
            }
            gridFactures.ColumnHeadersDefaultCellStyle.BackColor = ColorSecondary
            gridFactures.ColumnHeadersDefaultCellStyle.ForeColor = ColorWhite
            gridFactures.EnableHeadersVisualStyles = False

            pnlGauche.Controls.Add(gridFactures)
            pnlGauche.Controls.Add(pnlFiltreDate)
            pnlGauche.Controls.Add(txtRecherche)
            pnlGauche.Controls.Add(lblRechercheTitre)

            ' --- Colonne Centre (Détails Facture) ---
            Dim pnlCentre As New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(15, 0, 15, 0),
                .AutoScroll = True
            }

            Dim grpDetails As New GroupBox() With {
                .Text = "DÉTAILS DE LA SÉLECTION",
                .Dock = DockStyle.Fill,
                .Font = FontBold,
                .ForeColor = ColorSecondary,
                .Padding = New Padding(10)
            }

            Dim pnlInfoFacture As New Panel() With {.Dock = DockStyle.Top, .Height = 90, .BackColor = Color.FromArgb(235, 240, 245)}
            lblNumeroFacture = New Label() With {.Text = "Facture: -", .Left = 15, .Top = 10, .AutoSize = True, .Font = FontBold, .ForeColor = ColorPrimary}
            lblClient = New Label() With {.Text = "Client: -", .Left = 15, .Top = 35, .AutoSize = True, .Font = FontMain}
            lblDateFacture = New Label() With {.Text = "Date: -", .Left = 15, .Top = 60, .AutoSize = True, .Font = FontMain}
            pnlInfoFacture.Controls.AddRange({lblNumeroFacture, lblClient, lblDateFacture})

            gridDetails = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True, .BorderStyle = BorderStyle.None,
                .BackgroundColor = ColorWhite, .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AllowUserToAddRows = False, .RowHeadersVisible = False,
                .AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle() With {.BackColor = Color.FromArgb(245, 245, 245)}
            }
            gridDetails.ColumnHeadersDefaultCellStyle.BackColor = ColorSecondary
            gridDetails.ColumnHeadersDefaultCellStyle.ForeColor = ColorWhite
            gridDetails.EnableHeadersVisualStyles = False

            grpDetails.Controls.Add(gridDetails)
            grpDetails.Controls.Add(pnlInfoFacture)
            pnlCentre.Controls.Add(grpDetails)

            ' --- Colonne Droite (Paiement) ---
            Dim pnlDroite As New Panel() With {
                .Width = 360,
                .Dock = DockStyle.Right,
                .BackColor = ColorWhite,
                .Padding = New Padding(15),
                .MinimumSize = New Size(320, 0),
                .AutoScroll = True
            }
            pnlDroite.BorderStyle = BorderStyle.FixedSingle

            Dim lblTotalTitre As New Label() With {.Text = "MONTANT À PERCEVOIR", .Dock = DockStyle.Top, .Height = 30, .Font = FontBold, .TextAlign = ContentAlignment.MiddleCenter}
            lblTotal = New Label() With {
                .Text = "0 FC", .Dock = DockStyle.Top, .Height = 60,
                .Font = FontTotal, .ForeColor = ColorPrimary, .TextAlign = ContentAlignment.MiddleCenter,
                .BackColor = Color.FromArgb(240, 248, 255)
            }

            Dim pnlRecu As New Panel() With {.Dock = DockStyle.Top, .Height = 80, .Padding = New Padding(0, 15, 0, 0)}
            Dim lblRecuTitre As New Label() With {.Text = "MONTANT REÇU", .Left = 0, .Top = 15, .AutoSize = True, .Font = FontBold}
            txtMontantRecu = New TextBox() With {
                .Left = 0, .Top = 38, .Width = 200, .Height = 35,
                .BorderStyle = BorderStyle.FixedSingle, .Font = New Font("Segoe UI", 14, FontStyle.Bold),
                .TextAlign = HorizontalAlignment.Right
            }
            cmbDevise = New ComboBox() With {
                .Left = 210, .Top = 38, .Width = 100, .Height = 35,
                .DropDownStyle = ComboBoxStyle.DropDownList, .Font = New Font("Segoe UI", 12)
            }
            cmbDevise.Items.AddRange(New Object() {"FC", "USD"})
            cmbDevise.SelectedIndex = 0
            pnlRecu.Controls.AddRange({lblRecuTitre, txtMontantRecu, cmbDevise})

            lblMonnaie = New Label() With {
                .Text = "Monnaie: 0 FC", .Dock = DockStyle.Top, .Height = 40,
                .Font = FontBold, .ForeColor = ColorDanger, .TextAlign = ContentAlignment.MiddleLeft
            }

            Dim pnlMode As New Panel() With {.Dock = DockStyle.Top, .Height = 70}
            Dim lblModeTitre As New Label() With {.Text = "MODE DE PAIEMENT", .Dock = DockStyle.Top, .Height = 25, .Font = FontBold}
            cmbMode = New ComboBox() With {.Dock = DockStyle.Top, .Height = 30, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbMode.Items.AddRange(New Object() {"CASH", "MOBILE_MONEY", "CARTE", "AUTRE"})
            cmbMode.SelectedIndex = 0
            pnlMode.Controls.AddRange({cmbMode, lblModeTitre})

            Dim pnlRef As New Panel() With {.Dock = DockStyle.Top, .Height = 70}
            Dim lblRefTitre As New Label() With {.Text = "RÉFÉRENCE / TRANSACTION", .Dock = DockStyle.Top, .Height = 25, .Font = FontBold}
            txtReference = New TextBox() With {.Dock = DockStyle.Top, .Height = 30, .BorderStyle = BorderStyle.FixedSingle}
            pnlRef.Controls.AddRange({txtReference, lblRefTitre})

            Dim pnlActions As New TableLayoutPanel() With {.Dock = DockStyle.Bottom, .Height = 260, .ColumnCount = 1, .RowCount = 7, .Padding = New Padding(0), .Margin = New Padding(0)}
            pnlActions.RowStyles.Add(New RowStyle(SizeType.Absolute, 55))
            pnlActions.RowStyles.Add(New RowStyle(SizeType.Absolute, 8))
            pnlActions.RowStyles.Add(New RowStyle(SizeType.Absolute, 45))
            pnlActions.RowStyles.Add(New RowStyle(SizeType.Absolute, 8))
            pnlActions.RowStyles.Add(New RowStyle(SizeType.Absolute, 45))
            pnlActions.RowStyles.Add(New RowStyle(SizeType.Absolute, 8))
            pnlActions.RowStyles.Add(New RowStyle(SizeType.Absolute, 45))
            btnEncaisser = New Button() With {
                .Text = "VALIDER L'ENCAISSEMENT", .Dock = DockStyle.Fill,
                .FlatStyle = FlatStyle.Flat, .BackColor = ColorAccent, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand
            }
            btnEncaisser.FlatAppearance.BorderSize = 0

            Dim btnSep1 As New Panel() With {.Dock = DockStyle.Top, .Height = 10}

            btnImprimer = New Button() With {
                .Text = "IMPRIMER TICKET", .Dock = DockStyle.Fill,
                .FlatStyle = FlatStyle.Flat, .BackColor = ColorSecondary, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand
            }
            btnImprimer.FlatAppearance.BorderSize = 0

            Dim btnSep2 As New Panel() With {.Dock = DockStyle.Top, .Height = 10}

            btnAnnuler = New Button() With {
                .Text = "EFFACER SÉLECTION", .Dock = DockStyle.Fill,
                .FlatStyle = FlatStyle.Flat, .BackColor = ColorDanger, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand
            }
            btnAnnuler.FlatAppearance.BorderSize = 0

            btnAnnulerFacture = New Button() With {
                .Text = "ANNULER FACTURE BROUILLON", .Dock = DockStyle.Fill,
                .FlatStyle = FlatStyle.Flat, .BackColor = ColorDanger, .ForeColor = ColorWhite, .Font = FontBold, .Cursor = Cursors.Hand
            }
            btnAnnulerFacture.FlatAppearance.BorderSize = 0

            pnlActions.Controls.Add(btnEncaisser, 0, 0)
            pnlActions.Controls.Add(btnSep1, 0, 1)
            pnlActions.Controls.Add(btnImprimer, 0, 2)
            pnlActions.Controls.Add(btnSep2, 0, 3)
            pnlActions.Controls.Add(btnAnnulerFacture, 0, 4)
            pnlActions.Controls.Add(New Panel() With {.Dock = DockStyle.Fill}, 0, 5)
            pnlActions.Controls.Add(btnAnnuler, 0, 6)

            pnlDroite.Controls.AddRange({pnlActions, pnlRef, pnlMode, lblMonnaie, pnlRecu, lblTotal, lblTotalTitre})
            pnlActions.BringToFront()

            ' Assemblage final
            pnlMain.Controls.Add(pnlCentre)
            pnlMain.Controls.Add(pnlGauche)
            pnlMain.Controls.Add(pnlDroite)
            Me.Controls.Add(pnlMain)
            Me.Controls.Add(pnlHeader)

            ' --- Handlers ---
            AddHandler btnActualiser.Click, AddressOf ChargerFactures
            AddHandler txtRecherche.TextChanged, AddressOf ChargerFactures
            AddHandler chkDate.CheckedChanged, AddressOf ChargerFactures
            AddHandler dtDate.ValueChanged, AddressOf ChargerFactures
            AddHandler gridFactures.SelectionChanged, AddressOf ChargerDetails
            AddHandler txtMontantRecu.TextChanged, AddressOf CalculerMonnaie
            AddHandler cmbDevise.SelectedIndexChanged, AddressOf CalculerMonnaie
            AddHandler btnEncaisser.Click, AddressOf Encaisser
            AddHandler btnImprimer.Click, AddressOf ImprimerTicket
            AddHandler btnAnnuler.Click, AddressOf AnnulerSelection
            AddHandler btnAnnulerFacture.Click, AddressOf AnnulerFactureBrouillon

            ' Initialisation
            ConfigurerGrilleFactures()
            ConfigurerGrilleChargerLignes()
            ChargerParametres()
            ChargerFactures(Nothing, EventArgs.Empty)
            MettreAJourEtatActions()
            AddHandler AppEvents.VenteCreee, AddressOf RafraichirFacturesDepuisEvenement
            AddHandler AppEvents.VenteValidee, AddressOf RafraichirFacturesDepuisEvenement
            AddHandler AppEvents.PaiementValide, AddressOf RafraichirFacturesDepuisEvenement
            AddHandler AppEvents.DataChanged, AddressOf RafraichirFacturesDepuisEvenement
            _dataMonitor = New DataChangeMonitorService(New String() {"FACTURES", "PAIEMENTS"}, 3000)
            AddHandler _dataMonitor.DomaineModifie, AddressOf RafraichirFacturesDepuisVersionSql
            _dataMonitor.Start()
        End Sub
        Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
            MyBase.OnKeyDown(e)
            If e.KeyCode = Keys.Enter Then
                Encaisser(Nothing, EventArgs.Empty)
                e.Handled = True
            ElseIf e.KeyCode = Keys.F5 Then
                ChargerFactures(Nothing, EventArgs.Empty)
                e.Handled = True
            ElseIf e.KeyCode = Keys.Escape Then
                AnnulerSelection(Nothing, EventArgs.Empty)
                e.Handled = True
            End If
        End Sub

        Private Sub ConfigurerGrilleFactures()
            gridFactures.Columns.Clear()
            Dim colId As New DataGridViewTextBoxColumn() With {.DataPropertyName = "FactureVenteId", .Name = "FactureVenteId", .Visible = False}
            Dim colNumero As New DataGridViewTextBoxColumn() With {.DataPropertyName = "NumeroFacture", .HeaderText = "N° Facture", .Width = 110}
            Dim colClient As New DataGridViewTextBoxColumn() With {.DataPropertyName = "ClientNom", .HeaderText = "Client", .Width = 120}
            Dim colTel As New DataGridViewTextBoxColumn() With {.DataPropertyName = "Telephone", .HeaderText = "Telephone", .Width = 120}
            Dim colDate As New DataGridViewTextBoxColumn() With {.DataPropertyName = "CreeLe", .HeaderText = "Date", .Width = 90}
            Dim colTotal As New DataGridViewTextBoxColumn() With {.DataPropertyName = "MontantTotal", .HeaderText = "Total", .Width = 80}
            Dim colStatut As New DataGridViewTextBoxColumn() With {.DataPropertyName = "Statut", .Name = "Statut", .Visible = False}
            gridFactures.Columns.AddRange(New DataGridViewColumn() {colId, colNumero, colClient, colTel, colDate, colTotal, colStatut})
        End Sub

        Private Sub ConfigurerGrilleChargerLignes()
            gridDetails.Columns.Clear()
            gridDetails.AutoGenerateColumns = False
            Dim colLigFact As New DataGridViewTextBoxColumn() With {.DataPropertyName = "LigneFactureVenteId", .Name = "LigneFactureVenteId", .Visible = False}
            Dim colFactV As New DataGridViewTextBoxColumn() With {.DataPropertyName = "FactureVenteId", .HeaderText = "FactureVenteId", .Width = 110, .Visible = False}
            Dim colprod As New DataGridViewTextBoxColumn() With {.DataPropertyName = "ProduitId", .HeaderText = "ProduitId", .Width = 120, .Visible = False}
            Dim colLib As New DataGridViewTextBoxColumn() With {.DataPropertyName = "Libelle", .HeaderText = "Libelle", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill}
            Dim colQte As New DataGridViewTextBoxColumn() With {.DataPropertyName = "Quantite", .HeaderText = "Qte", .Width = 80}
            Dim colPU As New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixUnitaire", .HeaderText = "PU", .Width = 80}
            Dim colTotal As New DataGridViewTextBoxColumn() With {.DataPropertyName = "MontantLigne", .HeaderText = "Total", .Width = 80, .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleRight}}
            gridDetails.Columns.AddRange(New DataGridViewColumn() {colLigFact, colFactV, colprod, colLib, colQte, colPU, colTotal})


        End Sub
        Private Sub ChargerParametres()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim paramService As New ParametreService(New ParametreRepository(dal))
                _param = paramService.Charger()
            Catch
            End Try
        End Sub

        Private Sub ChargerFactures(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim repo As New FactureVenteRepository(dal)
                Dim dateDu As Date? = If(chkDate.Checked, CType(dtDate.Value.Date, Date?), Nothing)
                Dim dateAu As Date? = If(chkDate.Checked, CType(dtDate.Value.Date, Date?), Nothing)
                Dim dt As DataTable = repo.ListerValideesNonPayees(txtRecherche.Text.Trim(), dateDu, dateAu)
                gridFactures.DataSource = dt
                For Each row As DataGridViewRow In gridFactures.Rows
                    If row Is Nothing OrElse row.IsNewRow Then Continue For
                    Dim statut As String = Convert.ToString(row.Cells("Statut").Value)
                    If String.Equals(statut, "ANNULEE", StringComparison.OrdinalIgnoreCase) Then
                        row.DefaultCellStyle.ForeColor = ColorDanger
                        row.DefaultCellStyle.SelectionForeColor = ColorDanger
                    End If
                Next
                gridFactures.ClearSelection()
                Try
                    gridFactures.CurrentCell = Nothing
                Catch
                End Try
                AnnulerSelection(Nothing, EventArgs.Empty)
            Catch ex As Exception
                MessageBox.Show("Erreur chargement factures: " & ex.Message)
            End Try
        End Sub

        Private Sub RafraichirFacturesDepuisEvenement(sender As Object, e As EventArgs)
            If IsDisposed Then Return
            If InvokeRequired Then
                BeginInvoke(New MethodInvoker(Sub() RafraichirFacturesDepuisEvenement(Nothing, EventArgs.Empty)))
                Return
            End If
            If _isRefreshingFromEvent Then Return

            _isRefreshingFromEvent = True
            Try
                Dim factureSelectionneeId As Integer? = Nothing
                If gridFactures.CurrentRow IsNot Nothing AndAlso gridFactures.CurrentRow.Cells(0).Value IsNot Nothing Then
                    factureSelectionneeId = Convert.ToInt32(gridFactures.CurrentRow.Cells(0).Value)
                End If

                ChargerFactures(Nothing, EventArgs.Empty)

                If factureSelectionneeId.HasValue Then
                    For Each row As DataGridViewRow In gridFactures.Rows
                        If row Is Nothing OrElse row.IsNewRow Then Continue For
                        If Convert.ToInt32(row.Cells(0).Value) = factureSelectionneeId.Value Then
                            row.Selected = True
                            gridFactures.CurrentCell = row.Cells(1)
                            Exit For
                        End If
                    Next
                End If

                If gridFactures.CurrentRow IsNot Nothing Then
                    ChargerDetails(Nothing, EventArgs.Empty)
                Else
                    AnnulerSelection(Nothing, EventArgs.Empty)
                End If
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("CaisseForm", "RafraichirFacturesDepuisEvenement", "Erreur lors du rafraichissement automatique des factures caisse.", ex)
            Finally
                _isRefreshingFromEvent = False
            End Try
        End Sub

        Private Sub RafraichirFacturesDepuisVersionSql(sender As Object, e As DataChangeEventArgs)
            If IsDisposed OrElse Disposing OrElse Not IsHandleCreated Then Return
            Try
                BeginInvoke(New MethodInvoker(Sub()
                    If IsDisposed OrElse Disposing Then Return
                    RafraichirFacturesDepuisEvenement(Nothing, EventArgs.Empty)
                End Sub))
            Catch ex As ObjectDisposedException
                Dim log As New ProductionLogService()
                log.Warn("CaisseForm", "RafraichirFacturesDepuisVersionSql", "Formulaire fermé avant rafraîchissement multi-postes : " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerDetails(sender As Object, e As EventArgs)
            Dim row As DataGridViewRow = Nothing
            If Not TryGetSelectedFactureRow(row) Then
                AnnulerSelection(Nothing, EventArgs.Empty)
                Return
            End If

            Dim numero As String = Convert.ToString(row.Cells(1).Value)
            Dim client As String = Convert.ToString(row.Cells(2).Value)
            Dim tel As String = Convert.ToString(row.Cells(3).Value)
            Dim dtFacture As Date = Convert.ToDateTime(row.Cells(4).Value)
            _totalCourant = Convert.ToDecimal(row.Cells(5).Value)

            lblNumeroFacture.Text = "Facture: " & numero
            lblClient.Text = "Client: " & client & " / " & tel
            lblDateFacture.Text = "Date: " & dtFacture.ToString("dd/MM/yyyy")

            ChargerLignes()
            lblTotal.Text = FormatageGlobal.FormatMontant(_totalCourant)
            txtMontantRecu.Text = _totalCourant.ToString("N0")
            CalculerMonnaie(Nothing, EventArgs.Empty)
            MettreAJourEtatActions()
        End Sub

        Private Sub ChargerLignes()
            Dim factureId As Integer
            If Not TryGetSelectedFactureId(factureId) Then
                gridDetails.DataSource = Nothing
                Return
            End If
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New LigneFactureVenteRepository(dal)
            gridDetails.DataSource = repo.ListerDetailsParFacture(factureId)
        End Sub

        Private Function ConvertirMontant(montantSaisi As Decimal, devise As String) As Decimal
            If devise = "FC" Then Return montantSaisi
            Dim taux As Decimal = If(_param Is Nothing, 0D, _param.TauxUsd)
            Return montantSaisi * taux
        End Function

        Private Sub CalculerMonnaie(sender As Object, e As EventArgs)
            Try
                Dim recu As Decimal = Decimal.Parse(If(txtMontantRecu.Text.Trim() = "", "0", txtMontantRecu.Text.Trim()))
                Dim devise As String = cmbDevise.SelectedItem.ToString()
                Dim recuFC As Decimal = ConvertirMontant(recu, devise)
                Dim monnaieFC As Decimal = recuFC - _totalCourant
                Dim taux As Decimal = If(_param Is Nothing, 0D, _param.TauxUsd)
                Dim monnaieUSD As Decimal = If(taux = 0D, 0D, monnaieFC / taux)
                lblMonnaie.Text = "Monnaie: " & FormatageGlobal.FormatMontant(monnaieFC) & " (" & monnaieUSD.ToString("N0") & " USD)"
            Catch
            End Try
        End Sub

        Private Sub Encaisser(sender As Object, e As EventArgs)
            Try
                Dim selectedRow As DataGridViewRow = Nothing
                If Not TryGetSelectedFactureRow(selectedRow) Then
                    MessageBox.Show("Veuillez sélectionner une facture à encaisser.")
                    Return
                End If

                Dim factureId As Integer
                If Not TryGetSelectedFactureId(factureId) OrElse factureId <= 0 Then
                    MessageBox.Show("Veuillez sélectionner une facture à encaisser.")
                    Return
                End If

                If _totalCourant <= 0D Then
                    MessageBox.Show("Le montant à percevoir est invalide pour cette facture.")
                    Return
                End If

                If cmbDevise.SelectedItem Is Nothing Then
                    MessageBox.Show("Veuillez sélectionner une devise.")
                    Return
                End If

                If cmbMode.SelectedItem Is Nothing Then
                    MessageBox.Show("Veuillez sélectionner un mode de paiement.")
                    Return
                End If

                If SessionUtilisateur.UtilisateurId <= 0 Then
                    MessageBox.Show("Utilisateur non connecté. Veuillez vous reconnecter.")
                    Return
                End If

                Dim repoVerification As New FactureVenteRepository(New DAL(ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString))
                Dim facture As FactureVenteDTO = repoVerification.ObtenirParId(factureId)
                If facture Is Nothing OrElse Not String.Equals(facture.Statut, "EN_ATTENTE", StringComparison.OrdinalIgnoreCase) Then
                    MessageBox.Show("La facture sélectionnée n'est plus disponible pour encaissement.")
                    ChargerFactures(Nothing, EventArgs.Empty)
                    Return
                End If

                Dim montantSaisi As Decimal
                If Not Decimal.TryParse(If(txtMontantRecu.Text.Trim() = "", "0", txtMontantRecu.Text.Trim()), montantSaisi) Then
                    MessageBox.Show("Le montant reçu est invalide.")
                    Return
                End If

                Dim devise As String = cmbDevise.SelectedItem.ToString()
                Dim montantFC As Decimal = ConvertirMontant(montantSaisi, devise)
                If montantFC < _totalCourant Then
                    MessageBox.Show("Montant recu insuffisant.")
                    Return
                End If

                Dim monnaieFC As Decimal = montantFC - _totalCourant
                Dim ticket As TicketData = ConstruireTicketDepuisSelection(montantFC, monnaieFC, devise)

                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim service As New FacturationService(dal)
                service.EncaisserFacture(factureId, cmbMode.SelectedItem.ToString(), txtReference.Text.Trim(), montantFC, monnaieFC, devise, SessionUtilisateur.UtilisateurId)

                _dernierTicket = ticket
                If Not ImprimerTicket(_dernierTicket, 2, True) Then
                    Dim log As New ProductionLogService()
                    log.Warn("CaisseForm", "Encaisser", "Le paiement a été validé, mais aucune imprimante ticket n'est configurée ou disponible.")
                    MessageBox.Show("Le paiement a été validé, mais aucune imprimante ticket n'est configurée ou disponible." & Environment.NewLine &
                                    "Le ticket n'a pas été imprimé.",
                                    "Impression",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning)
                End If
                MessageBox.Show("Paiement reussi.")
                ChargerFactures(Nothing, EventArgs.Empty)
                AnnulerSelection(Nothing, EventArgs.Empty)
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("CaisseForm", "Encaisser", "Erreur de validation de paiement." & Environment.NewLine &
                          "CurrentRow=" & If(gridFactures.CurrentRow Is Nothing, "Nothing", "Present") & Environment.NewLine &
                          "SelectedRows=" & gridFactures.SelectedRows.Count.ToString() & Environment.NewLine &
                          "TotalCourant=" & _totalCourant.ToString("N2") & Environment.NewLine &
                          "Devise=" & If(cmbDevise.SelectedItem Is Nothing, "Nothing", cmbDevise.SelectedItem.ToString()) & Environment.NewLine &
                          "Mode=" & If(cmbMode.SelectedItem Is Nothing, "Nothing", cmbMode.SelectedItem.ToString()) & Environment.NewLine &
                          "Utilisateur=" & SessionUtilisateur.UtilisateurId.ToString(), ex)
                MessageBox.Show("Erreur paiement: " & ex.Message)
            End Try
        End Sub

        Private Function ConstruireTicketDepuisSelection(montantRecuFc As Decimal, monnaieFc As Decimal, devise As String) As TicketData
            Dim row As DataGridViewRow = Nothing
            If Not TryGetSelectedFactureRow(row) Then
                Throw New InvalidOperationException("Aucune facture valide sélectionnée pour construire le ticket.")
            End If

            Dim ticket As New TicketData()
            ticket.Numero = Convert.ToString(row.Cells(1).Value)
            ticket.Client = Convert.ToString(row.Cells(2).Value)
            ticket.Telephone = Convert.ToString(row.Cells(3).Value)
            ticket.DateFacture = Convert.ToDateTime(row.Cells(4).Value)
            ticket.Total = Convert.ToDecimal(row.Cells(5).Value)
            ticket.MontantRecu = montantRecuFc
            ticket.Monnaie = monnaieFc
            ticket.Devise = devise
            ticket.ModePaiement = If(cmbMode.SelectedItem Is Nothing, String.Empty, cmbMode.SelectedItem.ToString())
            ticket.ReferencePaiement = txtReference.Text.Trim()
            ticket.Caissier = If(String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur), "SYSTEM", SessionUtilisateur.NomUtilisateur)

            Dim factureId As Integer = Convert.ToInt32(row.Cells(0).Value)
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New LigneFactureVenteRepository(dal)
            ticket.Lignes = repo.ListerDetailsParFacture(factureId)
            Return ticket
        End Function

        Private Sub ImprimerTicket(sender As Object, e As EventArgs)
            Try
                Dim ticket As TicketData = Nothing
                If HasFactureSelectionValide() Then
                    Dim montantSaisi As Decimal
                    Decimal.TryParse(If(txtMontantRecu.Text.Trim() = "", "0", txtMontantRecu.Text.Trim()), montantSaisi)
                    Dim devise As String = cmbDevise.SelectedItem.ToString()
                    Dim montantFC As Decimal = ConvertirMontant(montantSaisi, devise)
                    Dim monnaieFC As Decimal = montantFC - _totalCourant
                    ticket = ConstruireTicketDepuisSelection(montantFC, monnaieFC, devise)
                ElseIf _dernierTicket IsNot Nothing Then
                    ticket = _dernierTicket
                End If

                If ticket Is Nothing Then
                    MessageBox.Show("Aucune facture a imprimer.")
                    Return
                End If

                If Not ImprimerTicket(ticket, 2, True) Then
                    MessageBox.Show("Aucune imprimante ticket n'est configurée ou disponible.")
                End If
            Catch ex As Exception
                MessageBox.Show("Erreur impression: " & ex.Message)
            End Try
        End Sub

        Private Function ImprimerTicket(ticket As TicketData, Optional copies As Integer = 1, Optional afficherApercu As Boolean = True) As Boolean
            Try
                If ticket Is Nothing Then
                    Return False
                End If

                If _param Is Nothing Then
                    Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                    Dim dal As New DAL(cs)
                    _param = (New ParametreService(New ParametreRepository(dal))).Charger()
                End If

                If _param Is Nothing OrElse String.IsNullOrWhiteSpace(_param.ImprimanteTicket) Then
                    Dim log As New ProductionLogService()
                    log.Warn("CaisseForm", "ImprimerTicket", "Aucune imprimante ticket configurée.")
                    Return False
                End If

                Dim totalCopies As Integer = Math.Max(1, copies)
                Dim doc As Printing.PrintDocument = CreerDocumentTicket(ticket, totalCopies)

                If afficherApercu AndAlso _param IsNot Nothing AndAlso _param.ApercuAvantImpression Then
                    Dim preview As New PrintPreviewDialog()
                    preview.Document = doc
                    ConfigurerApercuImpression(preview, ticket, totalCopies)
                    preview.ShowDialog(Me)
                Else
                    doc.Print()
                End If
                Return True
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("CaisseForm", "ImprimerTicket", "Erreur lors de l'impression du ticket.", ex)
                Return False
            End Try
        End Function

        Private Function CreerDocumentTicket(ticket As TicketData, copies As Integer) As Printing.PrintDocument
            Dim doc As New Printing.PrintDocument()
            _param = PrintConfigurationHelper.ConfigurerDocumentThermique(doc, Me, "CaisseForm", "ImprimerTicket", 315, 1400)
            doc.PrinterSettings.Copies = 1S
            doc.DefaultPageSettings.Color = If(_param IsNot Nothing, _param.ImpressionCouleur, True)
            doc.DefaultPageSettings.Margins = New Printing.Margins(2, 2, 2, 2)

            Dim totalCopies As Integer = Math.Max(1, copies)
            Dim copieCourante As Integer = 1
            AddHandler doc.PrintPage,
                Sub(s, eV)
                    ImprimerPageTicket(eV, ticket, copieCourante, totalCopies)
                    copieCourante += 1
                    eV.HasMorePages = copieCourante <= totalCopies
                End Sub

            Return doc
        End Function

        Private Sub ConfigurerApercuImpression(preview As PrintPreviewDialog, ticket As TicketData, totalCopies As Integer)
            If preview Is Nothing Then
                Return
            End If

            preview.Width = 1000
            preview.Height = 720
            preview.KeyPreview = True
            AddHandler preview.Shown,
                Sub()
                    preview.Select()
                End Sub
            AddHandler preview.KeyDown,
                Sub(sender, eArgs)
                    If eArgs.KeyCode = Keys.Enter Then
                        eArgs.Handled = True
                        eArgs.SuppressKeyPress = True
                        If _impressionDepuisApercuEnCours Then
                            Return
                        End If

                        _impressionDepuisApercuEnCours = True
                        Dim docImpression As Printing.PrintDocument = CreerDocumentTicket(ticket, totalCopies)
                        Try
                            docImpression.Print()
                            preview.Close()
                        Finally
                            _impressionDepuisApercuEnCours = False
                        End Try
                    ElseIf eArgs.KeyCode = Keys.Escape Then
                        eArgs.Handled = True
                        eArgs.SuppressKeyPress = True
                        preview.Close()
                    End If
                End Sub
        End Sub

        Private Sub ConfigurerTicket80Mm(doc As Printing.PrintDocument)
            Try
                Dim largeur As Integer = CInt(Math.Round(80D / 25.4D * 100D))
                Dim hauteur As Integer = 1200
                doc.DefaultPageSettings.PaperSize = New Printing.PaperSize("Ticket80mm", largeur, hauteur)
                doc.DefaultPageSettings.Margins = New Printing.Margins(5, 5, 5, 5)
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Warn("CaisseForm", "ConfigurerTicket80Mm", "Impossible de configurer le format ticket 80mm.")
                log.Error("CaisseForm", "ConfigurerTicket80Mm", "Erreur configuration ticket 80mm.", ex)
            End Try
        End Sub

        Private Sub ImprimerPageTicket(e As Printing.PrintPageEventArgs, ticket As TicketData, copieCourante As Integer, totalCopies As Integer)
            Dim margeInterne As Integer = Math.Max(4, CInt(Math.Round(e.Graphics.DpiX * 3D / 25.4D)))
            Dim gauche As Integer = e.MarginBounds.Left + margeInterne
            Dim droite As Integer = e.MarginBounds.Right - margeInterne
            If droite <= gauche Then
                gauche = e.MarginBounds.Left
                droite = e.MarginBounds.Right
            End If

            Dim largeurDisponible As Integer = Math.Max(180, droite - gauche)
            Dim y As Integer = e.MarginBounds.Top + 1
            Dim fontTitre As New Font("Segoe UI", 10, FontStyle.Bold)
            Dim fontSection As New Font("Segoe UI", 7.5F, FontStyle.Bold)
            Dim fontLigne As New Font("Segoe UI", 7.5F)
            Dim fontTotal As New Font("Segoe UI", 8.5F, FontStyle.Bold)
            Dim titre As String = If(_param Is Nothing OrElse _param.NomMagasin = "", "MAGASIN", _param.NomMagasin)

            Dim logoPath As String = If(_param Is Nothing, String.Empty, LogoPathHelper.GetLogoPath(_param))
            If Not String.IsNullOrWhiteSpace(logoPath) AndAlso File.Exists(logoPath) Then
                Using image As Image = Image.FromFile(logoPath)
                    Dim ratio As Decimal = If(image.Height <= 0, 1D, CDec(image.Width) / CDec(image.Height))
                    Dim hauteurLogo As Integer = 50
                    Dim largeurLogo As Integer = CInt(Math.Min(90D, hauteurLogo * CDbl(ratio)))
                    Dim xLogo As Integer = gauche + ((largeurDisponible - largeurLogo) \ 2)
                    e.Graphics.DrawImage(image, xLogo, y, largeurLogo, hauteurLogo)
                    y += hauteurLogo + 4
                End Using
            End If

            y = DessinerTexteCentre(e.Graphics, titre, fontTitre, gauche, largeurDisponible, y)
            If _param IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(_param.AdresseMagasin) Then
                y = DessinerTexteCentre(e.Graphics, _param.AdresseMagasin, fontLigne, gauche, largeurDisponible, y)
            End If
            If _param IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(_param.TelephoneMagasin) Then
                y = DessinerTexteCentre(e.Graphics, _param.TelephoneMagasin, fontLigne, gauche, largeurDisponible, y)
            End If
            y = DessinerTexteCentre(e.Graphics, "TICKET DE CAISSE", fontSection, gauche, largeurDisponible, y + 2)
            y = DessinerSeparateurTicket(e.Graphics, fontLigne, gauche, y, largeurDisponible)
            y = DessinerBlocTicket(e.Graphics, "Facture", ticket.Numero, fontLigne, gauche, droite, y)
            y = DessinerBlocTicket(e.Graphics, "Date", ticket.DateFacture.ToString("dd/MM/yyyy HH:mm"), fontLigne, gauche, droite, y)
            y = DessinerBlocTicket(e.Graphics, "Caissier", ticket.Caissier, fontLigne, gauche, droite, y)
            If Not String.IsNullOrWhiteSpace(ticket.Client) Then
                y = DessinerBlocTicket(e.Graphics, "Client", ticket.Client, fontLigne, gauche, droite, y)
            End If
            y = DessinerBlocTicket(e.Graphics, "Exemplaire", copieCourante.ToString() & "/" & totalCopies.ToString(), fontLigne, gauche, droite, y)
            y = DessinerSeparateurTicket(e.Graphics, fontLigne, gauche, y, largeurDisponible)

            If ticket.Lignes IsNot Nothing Then
                For Each row As DataRow In ticket.Lignes.Rows
                    Dim libelle As String = Convert.ToString(row("Libelle"))
                    Dim qte As Decimal = SafeDecimalTicket(row, "QuantiteSaisie")
                    Dim prix As Decimal = SafeDecimalTicket(row, "PrixUnitaire")
                    Dim total As Decimal = SafeDecimalTicket(row, "MontantLigne")
                    Dim libelleType As String = GetLibelleTypeVentePourTicket(row, qte)

                    y = DessinerTexteGauche(e.Graphics, libelle, fontSection, gauche, largeurDisponible, y)
                    y = DessinerLigneArticleTicket(e.Graphics, FormaterQuantiteTicket(qte) & " " & libelleType & " (" & FormatMontantTicket(prix) & ")", FormatMontantTicket(total), fontLigne, gauche + 4, droite, y)
                Next
            End If

            y = DessinerSeparateurTicket(e.Graphics, fontLigne, gauche, y, largeurDisponible)
            y = DessinerBlocTicket(e.Graphics, "Sous-total", FormatMontantTicket(ticket.Total), fontLigne, gauche, droite, y)
            y = DessinerBlocTicket(e.Graphics, "Montant à payer", FormatMontantTicket(ticket.Total), fontTotal, gauche, droite, y)
            y = DessinerBlocTicket(e.Graphics, "Montant reçu", FormaterMontantTicket(ticket.MontantRecu, ticket.Devise), fontLigne, gauche, droite, y)
            y = DessinerBlocTicket(e.Graphics, "Monnaie rendue", FormatMontantTicket(ticket.Monnaie), fontLigne, gauche, droite, y)
            y = DessinerBlocTicket(e.Graphics, "Mode paiement", ticket.ModePaiement, fontLigne, gauche, droite, y)
            If Not String.IsNullOrWhiteSpace(ticket.ReferencePaiement) Then
                y = DessinerBlocTicket(e.Graphics, "Référence", ticket.ReferencePaiement, fontLigne, gauche, droite, y)
            End If
            y = DessinerSeparateurTicket(e.Graphics, fontLigne, gauche, y, largeurDisponible)
            y = DessinerTexteCentre(e.Graphics, "ACHAT DÉFINITIF - Aucun échange ni reprise.", fontLigne, gauche, largeurDisponible, y)
            y = DessinerTexteCentre(e.Graphics, "Merci pour votre confiance", fontSection, gauche, largeurDisponible, y)
            y = DessinerTexteCentre(e.Graphics, "Imprimé le " & Date.Now.ToString("dd/MM/yyyy HH:mm"), fontLigne, gauche, largeurDisponible, y)
        End Sub

        Private Function DessinerSeparateurTicket(graphics As Graphics, font As Font, x As Integer, y As Integer, largeur As Integer) As Integer
            graphics.DrawLine(Pens.Black, x, y + 3, x + largeur - 1, y + 3)
            Return y + CInt(Math.Ceiling(font.GetHeight(graphics))) + 2
        End Function

        Private Function DessinerBlocTicket(graphics As Graphics, libelle As String, valeur As String, font As Font, xGauche As Integer, xDroite As Integer, y As Integer) As Integer
            Dim largeurTotale As Integer = Math.Max(120, xDroite - xGauche)
            Dim largeurLibelle As Integer = Math.Min(82, Math.Max(58, CInt(largeurTotale * 0.38R)))
            Dim xValeur As Integer = xGauche + largeurLibelle
            Dim largeurValeur As Integer = Math.Max(50, xDroite - xValeur)
            Dim layoutValeur As New SizeF(largeurValeur, 1000)
            Dim tailleValeur As SizeF = graphics.MeasureString(If(valeur, String.Empty), font, layoutValeur)
            Dim hauteur As Integer = Math.Max(CInt(Math.Ceiling(font.GetHeight(graphics))), CInt(Math.Ceiling(tailleValeur.Height))) + 3

            graphics.DrawString(libelle & " :", font, Brushes.Black, New RectangleF(xGauche, y, largeurLibelle, hauteur))
            Using formatValeur As New StringFormat()
                formatValeur.Alignment = StringAlignment.Far
                graphics.DrawString(If(valeur, String.Empty), font, Brushes.Black, New RectangleF(xValeur, y, largeurValeur, hauteur), formatValeur)
            End Using
            Return y + hauteur
        End Function

        Private Function DessinerLigneArticleTicket(graphics As Graphics, detail As String, montant As String, font As Font, xGauche As Integer, xDroite As Integer, y As Integer) As Integer
            Dim largeurTotale As Integer = Math.Max(120, xDroite - xGauche)
            Dim largeurMontant As Integer = Math.Min(CInt(largeurTotale * 0.45R), Math.Max(70, CInt(Math.Ceiling(graphics.MeasureString(If(montant, String.Empty), font).Width)) + 8))
            Dim largeurDetail As Integer = Math.Max(60, largeurTotale - largeurMontant - 6)
            Dim tailleDetail As SizeF = graphics.MeasureString(If(detail, String.Empty), font, New SizeF(largeurDetail, 1000))
            Dim hauteur As Integer = Math.Max(CInt(Math.Ceiling(tailleDetail.Height)), CInt(Math.Ceiling(font.GetHeight(graphics)))) + 3

            graphics.DrawString(If(detail, String.Empty), font, Brushes.Black, New RectangleF(xGauche, y, largeurDetail, hauteur))
            Using formatMontant As New StringFormat()
                formatMontant.Alignment = StringAlignment.Far
                graphics.DrawString(If(montant, String.Empty), font, Brushes.Black, New RectangleF(xDroite - largeurMontant, y, largeurMontant, hauteur), formatMontant)
            End Using

            Return y + hauteur
        End Function

        Private Function DessinerTexteCentre(graphics As Graphics, texte As String, font As Font, x As Integer, largeur As Integer, y As Integer) As Integer
            If String.IsNullOrWhiteSpace(texte) Then
                Return y
            End If

            Dim layout As New SizeF(largeur, 1000)
            Dim taille As SizeF = graphics.MeasureString(texte, font, layout)
            graphics.DrawString(texte, font, Brushes.Black, New RectangleF(x, y, largeur, taille.Height), New StringFormat With {.Alignment = StringAlignment.Center})
            Return y + CInt(Math.Ceiling(taille.Height)) + 2
        End Function

        Private Function DessinerTexteGauche(graphics As Graphics, texte As String, font As Font, x As Integer, largeur As Integer, y As Integer) As Integer
            If String.IsNullOrWhiteSpace(texte) Then
                Return y
            End If

            Dim layout As New SizeF(largeur, 1000)
            Dim taille As SizeF = graphics.MeasureString(texte, font, layout)
            graphics.DrawString(texte, font, Brushes.Black, New RectangleF(x, y, largeur, taille.Height))
            Return y + CInt(Math.Ceiling(taille.Height)) + 2
        End Function

        Private Shared Function SafeDecimalTicket(row As DataRow, colonne As String) As Decimal
            If row Is Nothing OrElse String.IsNullOrWhiteSpace(colonne) OrElse Not row.Table.Columns.Contains(colonne) OrElse row.IsNull(colonne) Then
                Return 0D
            End If

            Dim valeur As Object = row(colonne)
            If TypeOf valeur Is Decimal Then
                Return DirectCast(valeur, Decimal)
            End If

            Dim texte As String = Convert.ToString(valeur).Trim().Replace(" ", String.Empty).Replace(","c, "."c)
            Dim resultat As Decimal
            If Decimal.TryParse(texte, Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, resultat) Then
                Return resultat
            End If

            Return 0D
        End Function

        Private Shared Function SafeStringTicket(row As DataRow, colonne As String) As String
            If row Is Nothing OrElse String.IsNullOrWhiteSpace(colonne) OrElse Not row.Table.Columns.Contains(colonne) OrElse row.IsNull(colonne) Then
                Return String.Empty
            End If

            Return Convert.ToString(row(colonne)).Trim()
        End Function

        Private Shared Function FormatMontantTicket(montant As Decimal) As String
            Return FormaterMontantTicket(montant, "FC")
        End Function

        Private Shared Function FormaterMontantTicket(montant As Decimal, devise As String) As String
            Dim deviseAffichee As String = If(String.IsNullOrWhiteSpace(devise), "FC", devise.Trim())
            Return montant.ToString("#,##0", Globalization.CultureInfo.GetCultureInfo("fr-FR")) & " " & deviseAffichee
        End Function

        Private Shared Function FormaterQuantiteTicket(quantite As Decimal) As String
            If Decimal.Truncate(quantite) = quantite Then
                Return quantite.ToString("N0", Globalization.CultureInfo.GetCultureInfo("fr-FR"))
            End If

            Return quantite.ToString("N2", Globalization.CultureInfo.GetCultureInfo("fr-FR")).TrimEnd("0"c).TrimEnd(","c)
        End Function

        Private Shared Function GetLibelleTypeVentePourTicket(row As DataRow, quantiteSaisie As Decimal) As String
            Dim typeVente As String = SafeStringTicket(row, "TypeVente")
            Dim unitePrincipale As String = SafeStringTicket(row, "UnitePrincipale")
            Dim uniteSecondaire As String = SafeStringTicket(row, "UniteSecondaire")
            Dim typeNormalise As String = typeVente.Trim().ToUpperInvariant()

            If String.IsNullOrWhiteSpace(unitePrincipale) Then
                unitePrincipale = "unité"
            End If
            If String.IsNullOrWhiteSpace(uniteSecondaire) Then
                uniteSecondaire = "pièce"
            End If

            Select Case typeNormalise
                Case "GROS"
                    Return PluraliserUnite(unitePrincipale, quantiteSaisie)
                Case "DETAIL", "DÉTAIL", "PIECE", "PIÈCE", "UNITE", "UNITÉ"
                    Return PluraliserUnite(uniteSecondaire, quantiteSaisie)
                Case "DEMI"
                    Return PluraliserExpression("demi-" & unitePrincipale.ToLowerInvariant(), quantiteSaisie)
                Case "QUART"
                    Return PluraliserExpression("quart de " & unitePrincipale.ToLowerInvariant(), quantiteSaisie)
                Case "DOUZAINE"
                    Return PluraliserUnite("douzaine", quantiteSaisie)
                Case Else
                    If String.IsNullOrWhiteSpace(typeVente) Then
                        Return PluraliserUnite(uniteSecondaire, quantiteSaisie)
                    End If
                    Return PluraliserExpression(typeVente, quantiteSaisie)
            End Select
        End Function

        Private Shared Function PluraliserUnite(unite As String, quantite As Decimal) As String
            Dim texte As String = If(String.IsNullOrWhiteSpace(unite), "unité", unite.Trim())
            If Math.Abs(quantite - 1D) < 0.0001D Then
                Return texte.ToLowerInvariant()
            End If

            Dim lower As String = texte.ToLowerInvariant()
            If lower.EndsWith("s", StringComparison.OrdinalIgnoreCase) OrElse lower.EndsWith("x", StringComparison.OrdinalIgnoreCase) Then
                Return lower
            End If

            Return lower & "s"
        End Function

        Private Shared Function PluraliserExpression(expression As String, quantite As Decimal) As String
            Dim texte As String = If(String.IsNullOrWhiteSpace(expression), "unité", expression.Trim())
            If Math.Abs(quantite - 1D) < 0.0001D Then
                Return texte
            End If

            If texte.EndsWith("s", StringComparison.OrdinalIgnoreCase) OrElse texte.EndsWith("x", StringComparison.OrdinalIgnoreCase) Then
                Return texte
            End If

            Return texte & "s"
        End Function

        Private Function HasFactureSelectionValide() As Boolean
            Dim row As DataGridViewRow = Nothing
            Return TryGetSelectedFactureRow(row)
        End Function

        Private Function TryGetSelectedFactureRow(ByRef row As DataGridViewRow) As Boolean
            row = Nothing
            If gridFactures Is Nothing OrElse gridFactures.SelectedRows Is Nothing OrElse gridFactures.SelectedRows.Count = 0 Then
                Return False
            End If

            Dim candidate As DataGridViewRow = gridFactures.SelectedRows(0)
            If candidate Is Nothing OrElse candidate.IsNewRow Then
                Return False
            End If
            If candidate.Cells.Count = 0 OrElse candidate.Cells(0) Is Nothing OrElse candidate.Cells(0).Value Is Nothing OrElse candidate.Cells(0).Value Is DBNull.Value Then
                Return False
            End If

            row = candidate
            Return True
        End Function

        Private Function TryGetSelectedFactureId(ByRef factureId As Integer) As Boolean
            factureId = 0
            Dim row As DataGridViewRow = Nothing
            If Not TryGetSelectedFactureRow(row) Then
                Return False
            End If

            Return Integer.TryParse(Convert.ToString(row.Cells(0).Value), factureId) AndAlso factureId > 0
        End Function

        Private Sub MettreAJourEtatActions()
            Dim factureValide As Boolean = HasFactureSelectionValide() AndAlso _totalCourant > 0D
            btnEncaisser.Enabled = factureValide
            btnAnnulerFacture.Enabled = HasFactureSelectionValide()
            btnImprimer.Enabled = HasFactureSelectionValide() OrElse _dernierTicket IsNot Nothing
        End Sub

        Private Sub AnnulerSelection(sender As Object, e As EventArgs)
            gridDetails.DataSource = Nothing
            gridFactures.ClearSelection()
            Try
                gridFactures.CurrentCell = Nothing
            Catch
            End Try
            _totalCourant = 0D
            lblNumeroFacture.Text = ""
            lblClient.Text = ""
            lblDateFacture.Text = ""
            lblTotal.Text = "0 FC"
            txtMontantRecu.Text = ""
            lblMonnaie.Text = ""
            MettreAJourEtatActions()
        End Sub

        Private Sub AnnulerFactureBrouillon(sender As Object, e As EventArgs)
            Try
                If gridFactures.CurrentRow Is Nothing Then
                    MessageBox.Show("Sélectionnez une facture brouillon à annuler.")
                    Return
                End If

                Dim statut As String = Convert.ToString(gridFactures.CurrentRow.Cells("Statut").Value)
                If Not String.Equals(statut, "EN_ATTENTE", StringComparison.OrdinalIgnoreCase) Then
                    MessageBox.Show("Seules les factures brouillon peuvent être annulées depuis la caisse.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If

                If MessageBox.Show("Confirmer l'annulation de la facture brouillon ?", "Annuler facture", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
                    Return
                End If

                Dim factureId As Integer = Convert.ToInt32(gridFactures.CurrentRow.Cells(0).Value)
                Dim dal As New DAL(ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString)
                Dim repo As New FactureVenteRepository(dal)
                repo.MettreAJourStatut(factureId, "ANNULEE")
                AppDataVersionService.Touch("FACTURES")
                AppEvents.OnDataChanged()

                Dim log As New ProductionLogService()
                log.Info("CaisseForm", "AnnulerFactureBrouillon", "Facture brouillon annulée: " & factureId.ToString())

                ChargerFactures(Nothing, EventArgs.Empty)
                AnnulerSelection(Nothing, EventArgs.Empty)
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("CaisseForm", "AnnulerFactureBrouillon", "Erreur lors de l'annulation de la facture brouillon.", ex)
                MessageBox.Show("Erreur annulation facture: " & ex.Message)
            End Try
        End Sub

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            RemoveHandler AppEvents.VenteCreee, AddressOf RafraichirFacturesDepuisEvenement
            RemoveHandler AppEvents.VenteValidee, AddressOf RafraichirFacturesDepuisEvenement
            RemoveHandler AppEvents.PaiementValide, AddressOf RafraichirFacturesDepuisEvenement
            RemoveHandler AppEvents.DataChanged, AddressOf RafraichirFacturesDepuisEvenement
            If _dataMonitor IsNot Nothing Then
                RemoveHandler _dataMonitor.DomaineModifie, AddressOf RafraichirFacturesDepuisVersionSql
                _dataMonitor.Dispose()
                _dataMonitor = Nothing
            End If
            MyBase.OnFormClosed(e)
        End Sub
    End Class
End Namespace
