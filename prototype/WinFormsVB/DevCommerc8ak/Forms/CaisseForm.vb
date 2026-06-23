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

        Private Class TicketData
            Public Property Numero As String
            Public Property Client As String
            Public Property Telephone As String
            Public Property DateFacture As Date
            Public Property Total As Decimal
            Public Property MontantRecu As Decimal
            Public Property Monnaie As Decimal
            Public Property Devise As String
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
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.KeyPreview = True

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
                .Padding = New Padding(15)
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
                .Padding = New Padding(15, 0, 15, 0)
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
                .Padding = New Padding(15)
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
            Catch ex As Exception
                MessageBox.Show("Erreur chargement factures: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerDetails(sender As Object, e As EventArgs)
            If gridFactures.CurrentRow Is Nothing Then Return
            Dim numero As String = Convert.ToString(gridFactures.CurrentRow.Cells(1).Value)
            Dim client As String = Convert.ToString(gridFactures.CurrentRow.Cells(2).Value)
            Dim tel As String = Convert.ToString(gridFactures.CurrentRow.Cells(3).Value)
            Dim dtFacture As Date = Convert.ToDateTime(gridFactures.CurrentRow.Cells(4).Value)
            _totalCourant = Convert.ToDecimal(gridFactures.CurrentRow.Cells(5).Value)

            lblNumeroFacture.Text = "Facture: " & numero
            lblClient.Text = "Client: " & client & " / " & tel
            lblDateFacture.Text = "Date: " & dtFacture.ToString("dd/MM/yyyy")

            ChargerLignes()
            lblTotal.Text = FormatageGlobal.FormatMontant(_totalCourant)
            txtMontantRecu.Text = _totalCourant.ToString("N0")
            CalculerMonnaie(Nothing, EventArgs.Empty)
        End Sub

        Private Sub ChargerLignes()
            If gridFactures.CurrentRow Is Nothing Then Return
            Dim factureId As Integer = Convert.ToInt32(gridFactures.CurrentRow.Cells(0).Value)
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
                If gridFactures.CurrentRow Is Nothing Then
                    MessageBox.Show("Selectionnez une facture.")
                    Return
                End If

                Dim montantSaisi As Decimal = Decimal.Parse(If(txtMontantRecu.Text.Trim() = "", "0", txtMontantRecu.Text.Trim()))
                Dim devise As String = cmbDevise.SelectedItem.ToString()
                Dim montantFC As Decimal = ConvertirMontant(montantSaisi, devise)
                If montantFC < _totalCourant Then
                    MessageBox.Show("Montant recu insuffisant.")
                    Return
                End If

                Dim factureId As Integer = Convert.ToInt32(gridFactures.CurrentRow.Cells(0).Value)
                Dim monnaieFC As Decimal = montantFC - _totalCourant

                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim service As New FacturationService(dal)
                service.EncaisserFacture(factureId, cmbMode.SelectedItem.ToString(), txtReference.Text.Trim(), montantFC, monnaieFC, devise, SessionUtilisateur.UtilisateurId)

                _dernierTicket = ConstruireTicketDepuisSelection(montantFC, monnaieFC, devise)
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
                MessageBox.Show("Erreur paiement: " & ex.Message)
            End Try
        End Sub

        Private Function ConstruireTicketDepuisSelection(montantRecuFc As Decimal, monnaieFc As Decimal, devise As String) As TicketData
            Dim ticket As New TicketData()
            ticket.Numero = Convert.ToString(gridFactures.CurrentRow.Cells(1).Value)
            ticket.Client = Convert.ToString(gridFactures.CurrentRow.Cells(2).Value)
            ticket.Telephone = Convert.ToString(gridFactures.CurrentRow.Cells(3).Value)
            ticket.DateFacture = Convert.ToDateTime(gridFactures.CurrentRow.Cells(4).Value)
            ticket.Total = Convert.ToDecimal(gridFactures.CurrentRow.Cells(5).Value)
            ticket.MontantRecu = montantRecuFc
            ticket.Monnaie = monnaieFc
            ticket.Devise = devise

            Dim factureId As Integer = Convert.ToInt32(gridFactures.CurrentRow.Cells(0).Value)
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New LigneFactureVenteRepository(dal)
            ticket.Lignes = repo.ListerDetailsParFacture(factureId)
            Return ticket
        End Function

        Private Sub ImprimerTicket(sender As Object, e As EventArgs)
            Try
                Dim ticket As TicketData = Nothing
                If gridFactures.CurrentRow IsNot Nothing Then
                    Dim montantSaisi As Decimal = Decimal.Parse(If(txtMontantRecu.Text.Trim() = "", "0", txtMontantRecu.Text.Trim()))
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

                Dim doc As New Printing.PrintDocument()
                doc.PrinterSettings.PrinterName = _param.ImprimanteTicket
                doc.PrinterSettings.Copies = CShort(Math.Max(1, copies))
                ConfigurerTicket80Mm(doc)

                doc.DefaultPageSettings.Color = If(_param IsNot Nothing, _param.ImpressionCouleur, True)
                AddHandler doc.PrintPage, Sub(s, eV) ImprimerPageTicket(eV, ticket)

                If afficherApercu AndAlso _param IsNot Nothing AndAlso _param.ApercuAvantImpression Then
                    Dim preview As New PrintPreviewDialog()
                    preview.Document = doc
                    preview.ShowDialog()
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

        Private Sub ImprimerPageTicket(e As Printing.PrintPageEventArgs, ticket As TicketData)
            Dim y As Integer = 10
            Dim titre As String = If(_param Is Nothing OrElse _param.NomMagasin = "", "MAGASIN", _param.NomMagasin)
            e.Graphics.DrawString(titre, New Font("Segoe UI", 10, FontStyle.Bold), Brushes.Black, 10, y)
            y += 14
            If _param IsNot Nothing Then
                e.Graphics.DrawString(_param.AdresseMagasin, New Font("Segoe UI", 7), Brushes.Black, 10, y)
                y += 12
                e.Graphics.DrawString(_param.TelephoneMagasin, New Font("Segoe UI", 7), Brushes.Black, 10, y)
                y += 12
            End If

            e.Graphics.DrawString("------------------------", New Font("Segoe UI", 7), Brushes.Black, 10, y)
            y += 12
            e.Graphics.DrawString("Facture : " & ticket.Numero, New Font("Segoe UI", 7), Brushes.Black, 10, y)
            y += 12
            e.Graphics.DrawString("Date : " & ticket.DateFacture.ToString("dd/MM/yyyy"), New Font("Segoe UI", 7), Brushes.Black, 10, y)
            y += 12
            If ticket.Client <> "" Then
                e.Graphics.DrawString("Client : " & ticket.Client, New Font("Segoe UI", 7), Brushes.Black, 10, y)
                y += 12
            End If

            e.Graphics.DrawString("------------------------", New Font("Segoe UI", 7), Brushes.Black, 10, y)
            y += 12

            If ticket.Lignes IsNot Nothing Then
                For Each row As DataRow In ticket.Lignes.Rows
                    Dim libelle As String = Convert.ToString(row("Libelle"))
                    Dim qte As String = Convert.ToDecimal(row("QuantiteSaisie")).ToString()
                    Dim unite As String = Convert.ToString(row("TypeVente"))
                    Dim prix As String = Convert.ToDecimal(row("PrixUnitaire")).ToString()
                    Dim total As String = Convert.ToDecimal(row("MontantLigne")).ToString()
                    Dim line As String = libelle & "  " & qte & unite & " x " & prix
                    e.Graphics.DrawString(line, New Font("Segoe UI", 7), Brushes.Black, 10, y)
                    y += 12
                    e.Graphics.DrawString("   = " & total, New Font("Segoe UI", 7), Brushes.Black, 10, y)
                    y += 12
                Next
            End If

            e.Graphics.DrawString("------------------------", New Font("Segoe UI", 7), Brushes.Black, 10, y)
            y += 12
            e.Graphics.DrawString("TOTAL : " & ticket.Total.ToString() & " FC", New Font("Segoe UI", 8, FontStyle.Bold), Brushes.Black, 10, y)
            y += 12
            e.Graphics.DrawString("Recu : " & ticket.MontantRecu.ToString() & " " & ticket.Devise, New Font("Segoe UI", 7), Brushes.Black, 10, y)
            y += 12
            e.Graphics.DrawString("Monnaie : " & ticket.Monnaie.ToString() & " FC", New Font("Segoe UI", 7), Brushes.Black, 10, y)
            y += 12
            e.Graphics.DrawString("Merci pour votre visite", New Font("Segoe UI", 7), Brushes.Black, 10, y)
        End Sub

        Private Sub AnnulerSelection(sender As Object, e As EventArgs)
            gridDetails.DataSource = Nothing
            gridFactures.ClearSelection()
            _totalCourant = 0D
            lblNumeroFacture.Text = ""
            lblClient.Text = ""
            lblDateFacture.Text = ""
            lblTotal.Text = "0 FC"
            txtMontantRecu.Text = ""
            lblMonnaie.Text = ""
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
    End Class
End Namespace
