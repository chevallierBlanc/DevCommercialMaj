Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.IO
Imports System.Collections.Generic
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D

Namespace DevCommerc8ak
    Public Class FormulaireFactures
        Inherits Form

        ' --- Palette de Couleurs Professionnelle ---
        Private ReadOnly ColorBg As Color = Color.FromArgb(244, 247, 252)
        Private ReadOnly ColorCardBg As Color = Color.White
        Private ReadOnly ColorAccent As Color = Color.FromArgb(59, 130, 246) ' Bleu Moderne
        Private ReadOnly ColorSuccess As Color = Color.FromArgb(16, 185, 129)
        Private ReadOnly ColorWarning As Color = Color.FromArgb(245, 158, 11)
        Private ReadOnly ColorDanger As Color = Color.FromArgb(239, 68, 68)
        Private ReadOnly ColorTextPrimary As Color = Color.FromArgb(31, 41, 55)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(107, 114, 128)

        ' --- Polices ---
        Private ReadOnly FontMain As New Font("Segoe UI", 9)
        Private ReadOnly FontBold As New Font("Segoe UI", 9, FontStyle.Bold)
        Private ReadOnly FontTitle As New Font("Segoe UI", 14, FontStyle.Bold)
        Private ReadOnly FontKpi As New Font("Segoe UI", 16, FontStyle.Bold)

        ' --- Composants (Noms conservés) ---
        Private ReadOnly txtNumero As TextBox
        Private ReadOnly txtNomClient As TextBox
        Private ReadOnly txtTelephone As TextBox
        Private ReadOnly chkDate As CheckBox
        Private ReadOnly dtDu As DateTimePicker
        Private ReadOnly dtAu As DateTimePicker
        Private ReadOnly cmbStatut As ComboBox
        Private ReadOnly btnActualiser As Button

        Private ReadOnly gridFactures As DataGridView
        Private ReadOnly timer As Timer

        ' --- Nouveaux éléments visuels ---
        Private ReadOnly lblTotalFacture As Label
        Private ReadOnly lblTotalAttente As Label
        Private ReadOnly lblTotalPaye As Label
        Private _isRefreshingFromEvent As Boolean

        Public Sub New()
            ' Configuration de la Form
            Me.Text = "Historique et Gestion des Factures"
            Me.Width = 1250
            Me.Height = 800
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = ColorBg
            Me.Font = FontMain
            Me.DoubleBuffered = True

            ' --- En-tête / Cartes KPI ---
            Dim pnlKpiContainer As New Panel() With {.Dock = DockStyle.Top, .Height = 120, .Padding = New Padding(20, 15, 20, 15)}

            lblTotalFacture = CreerKpiCard(pnlKpiContainer, "TOTAL FACTURÉ", 85, ColorAccent)
            lblTotalAttente = CreerKpiCard(pnlKpiContainer, "EN ATTENTE (BROUILLONS)", 390, ColorWarning)
            lblTotalPaye = CreerKpiCard(pnlKpiContainer, "TOTAL ENCAISSÉ", 695, ColorSuccess)



            ' --- Zone de Filtres ---
            Dim panelFiltres As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 100,
                .BackColor = ColorCardBg,
                .Padding = New Padding(20, 10, 20, 10)
            }
            AddHandler panelFiltres.Paint, Sub(s, e) e.Graphics.DrawLine(New Pen(Color.FromArgb(230, 230, 230)), 0, 99, panelFiltres.Width, 99)

            Dim lblNumero As New Label() With {.Text = "N° FACTURE", .Font = New Font("Segoe UI", 8, FontStyle.Bold), .ForeColor = ColorTextSecondary, .Left = 25, .Top = 15, .AutoSize = True}
            txtNumero = New TextBox() With {.Left = 25, .Top = 38, .Width = 150, .Font = FontMain, .BorderStyle = BorderStyle.FixedSingle}

            Dim lblNom As New Label() With {.Text = "NOM CLIENT", .Font = New Font("Segoe UI", 8, FontStyle.Bold), .ForeColor = ColorTextSecondary, .Left = 190, .Top = 15, .AutoSize = True}
            txtNomClient = New TextBox() With {.Left = 190, .Top = 38, .Width = 180, .Font = FontMain, .BorderStyle = BorderStyle.FixedSingle}

            Dim lblTel As New Label() With {.Text = "TÉLÉPHONE", .Font = New Font("Segoe UI", 8, FontStyle.Bold), .ForeColor = ColorTextSecondary, .Left = 385, .Top = 15, .AutoSize = True}
            txtTelephone = New TextBox() With {.Left = 385, .Top = 38, .Width = 140, .Font = FontMain, .BorderStyle = BorderStyle.FixedSingle}

            chkDate = New CheckBox() With {.Text = "FILTRER PAR DATE", .Font = New Font("Segoe UI", 8, FontStyle.Bold), .ForeColor = ColorTextSecondary, .Left = 540, .Top = 15, .AutoSize = True}
            dtDu = New DateTimePicker() With {.Left = 540, .Top = 38, .Width = 120, .Format = DateTimePickerFormat.Short}
            dtAu = New DateTimePicker() With {.Left = 670, .Top = 38, .Width = 120, .Format = DateTimePickerFormat.Short}

            Dim lblStatut As New Label() With {.Text = "STATUT", .Font = New Font("Segoe UI", 8, FontStyle.Bold), .ForeColor = ColorTextSecondary, .Left = 805, .Top = 15, .AutoSize = True}
            cmbStatut = New ComboBox() With {.Left = 805, .Top = 38, .Width = 130, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontMain}
            cmbStatut.Items.AddRange(New Object() {"Tous", "Brouillon", "Validee", "Annulee"})
            cmbStatut.SelectedIndex = 0

            btnActualiser = New Button() With {
                .Text = "ACTUALISER",
                .Left = 950,
                .Top = 35,
                .Width = 120,
                .Height = 32,
                .FlatStyle = FlatStyle.Flat,
                .BackColor = ColorAccent,
                .ForeColor = Color.White,
                .Font = FontBold,
                .Cursor = Cursors.Hand
            }
            btnActualiser.FlatAppearance.BorderSize = 0

            panelFiltres.Controls.AddRange({lblNumero, txtNumero, lblNom, txtNomClient, lblTel, txtTelephone, chkDate, dtDu, dtAu, lblStatut, cmbStatut, btnActualiser})


            ' --- Grille de Données ---
            Dim pnlGridContainer As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(20)}
            gridFactures = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .BackgroundColor = ColorCardBg,
                .BorderStyle = BorderStyle.None,
                .ReadOnly = True,
                .AutoGenerateColumns = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AllowUserToAddRows = False,
                .RowHeadersVisible = False,
                .AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle() With {.BackColor = Color.FromArgb(250, 251, 252)},
                .ColumnHeadersHeight = 45,
                .RowTemplate = New DataGridViewRow() With {.Height = 40}
            }
            pnlGridContainer.Controls.Add(gridFactures)

            Me.Controls.Add(pnlGridContainer)
            Me.Controls.Add(pnlKpiContainer)
            Me.Controls.Add(panelFiltres)

            ' --- Handlers (Logique conservée) ---
            AddHandler txtNumero.TextChanged, AddressOf ChargerFactures
            AddHandler txtNomClient.TextChanged, AddressOf ChargerFactures
            AddHandler txtTelephone.TextChanged, AddressOf ChargerFactures
            AddHandler chkDate.CheckedChanged, AddressOf ChargerFactures
            AddHandler dtDu.ValueChanged, AddressOf ChargerFactures
            AddHandler dtAu.ValueChanged, AddressOf ChargerFactures
            AddHandler cmbStatut.SelectedIndexChanged, AddressOf ChargerFactures
            AddHandler btnActualiser.Click, AddressOf ChargerFactures
            AddHandler gridFactures.CellContentClick, AddressOf ActionsFacture
            AddHandler gridFactures.CellFormatting, AddressOf ColorerStatut

            ' Initialisation
            ' ThemeHelper.AppliquerTheme(Me)
            ConfigurerGrille()
            ChargerFactures(Nothing, EventArgs.Empty)

            timer = New Timer() With {.Interval = 600000}
            AddHandler timer.Tick, AddressOf ChargerFactures
            timer.Start()
            AddHandler AppEvents.DataChanged, AddressOf RafraichirDepuisEvenement
        End Sub

        ' --- Helpers de Design ---

        Private Function CreerKpiCard(parent As Panel, titre As String, left As Integer, color As Color) As Label
            Dim card As New Panel() With {
                .Location = New Point(left, 15),
                .Size = New Size(290, 90),
                .BackColor = ColorCardBg
            }
            AddHandler card.Paint, Sub(s, e)
                                       Dim rect As New Rectangle(0, 0, card.Width - 1, card.Height - 1)
                                       e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                       Using pen As New Pen(Color.FromArgb(230, 230, 230), 1)
                                           e.Graphics.DrawRectangle(pen, rect)
                                       End Using
                                       ' Barre d'accentuation
                                       Using brush As New SolidBrush(color)
                                           e.Graphics.FillRectangle(brush, 0, 0, 5, card.Height)
                                       End Using
                                   End Sub

            Dim lblT As New Label() With {
                .Text = titre,
                .Location = New Point(20, 15),
                .AutoSize = True,
                .ForeColor = ColorTextSecondary,
                .Font = New Font("Segoe UI", 8, FontStyle.Bold)
            }

            Dim lblV As New Label() With {
                .Text = "0.00 FC",
                .Location = New Point(20, 40),
                .AutoSize = True,
                .Font = FontKpi,
                .ForeColor = color
            }

            card.Controls.AddRange({lblT, lblV})
            parent.Controls.Add(card)
            Return lblV
        End Function

        Private Sub ConfigurerGrille()
            gridFactures.Columns.Clear()

            Dim colId As New DataGridViewTextBoxColumn() With {.DataPropertyName = "FactureVenteId", .Name = "FactureVenteId", .Visible = False}
            Dim colStatutDb As New DataGridViewTextBoxColumn() With {.DataPropertyName = "Statut", .Name = "Statut", .Visible = False}

            Dim colNumero As New DataGridViewTextBoxColumn() With {.DataPropertyName = "NumeroFacture", .HeaderText = "N° FACTURE", .Width = 145, .MinimumWidth = 135}
            Dim colClient As New DataGridViewTextBoxColumn() With {.DataPropertyName = "ClientNom", .HeaderText = "CLIENT", .Width = 190, .MinimumWidth = 150}
            Dim colTel As New DataGridViewTextBoxColumn() With {.DataPropertyName = "Telephone", .HeaderText = "TÉLÉPHONE", .Width = 125, .MinimumWidth = 115}
            Dim colDate As New DataGridViewTextBoxColumn() With {.DataPropertyName = "CreeLe", .HeaderText = "DATE", .Width = 145, .MinimumWidth = 135}
            Dim colMontant As New DataGridViewTextBoxColumn() With {.DataPropertyName = "MontantTotal", .HeaderText = "MONTANT TOTAL", .Width = 140, .MinimumWidth = 130}
            Dim colStatut As New DataGridViewTextBoxColumn() With {.DataPropertyName = "StatutAffichage", .HeaderText = "STATUT", .Width = 105, .MinimumWidth = 95}

            ' Boutons d'action stylisés
            Dim colVoir As New DataGridViewButtonColumn() With {.Name = "ActionVoir", .HeaderText = "", .Text = "VOIR", .UseColumnTextForButtonValue = True, .Width = 78}
            Dim colModifier As New DataGridViewButtonColumn() With {.Name = "ActionModifier", .HeaderText = "", .Text = "ÉDITER", .UseColumnTextForButtonValue = True, .Width = 78}
            Dim colAnnuler As New DataGridViewButtonColumn() With {.Name = "ActionAnnuler", .HeaderText = "", .Text = "ANNULER", .UseColumnTextForButtonValue = True, .Width = 86}
            Dim colImprimer As New DataGridViewButtonColumn() With {.Name = "ActionImprimer", .HeaderText = "", .Text = "IMPRIMER", .UseColumnTextForButtonValue = True, .Width = 96}

            gridFactures.Columns.AddRange(New DataGridViewColumn() {colId, colStatutDb, colNumero, colClient, colTel, colDate, colMontant, colStatut, colVoir, colModifier, colAnnuler, colImprimer})

            ' Style des en-têtes
            gridFactures.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
            gridFactures.ScrollBars = ScrollBars.Both
            gridFactures.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            gridFactures.GridColor = Color.FromArgb(229, 231, 235)
            gridFactures.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 234, 246)
            gridFactures.DefaultCellStyle.SelectionForeColor = ColorPrimary
            gridFactures.DefaultCellStyle.Padding = New Padding(4, 0, 4, 0)
            gridFactures.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            gridFactures.RowTemplate.Height = 46
            gridFactures.EnableHeadersVisualStyles = False
            gridFactures.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94)
            gridFactures.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
            gridFactures.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            gridFactures.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            gridFactures.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
            gridFactures.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 73, 94)
            colNumero.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            colClient.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            colClient.FillWeight = 34
            colTel.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            colDate.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            colMontant.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            colStatut.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            colMontant.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            colMontant.DefaultCellStyle.Format = "N0"
            colDate.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            colDate.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm"
            colStatut.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            For Each col As DataGridViewColumn In New DataGridViewColumn() {colVoir, colModifier, colAnnuler, colImprimer}
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            Next
        End Sub

        ' --- Logique Métier (Réintégrée et Fonctionnelle) ---

        Private Function ObtenirDAL() As DAL
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Return New DAL(cs)
        End Function

        Private Sub ChargerFactures(sender As Object, e As EventArgs)
            Try
                Dim repo As New FactureVenteRepository(ObtenirDAL())
                Dim statutDb As String = MapStatutDb()
                Dim dateDu As Date? = If(chkDate.Checked, CType(dtDu.Value.Date, Date?), Nothing)
                Dim dateAu As Date? = If(chkDate.Checked, CType(dtAu.Value.Date, Date?), Nothing)

                Dim dt As DataTable = repo.ListerHistorique(
                    txtNumero.Text.Trim(),
                    txtNomClient.Text.Trim(),
                    txtTelephone.Text.Trim(),
                    dateDu,
                    dateAu,
                    statutDb
                )

                If Not dt.Columns.Contains("StatutAffichage") Then
                    dt.Columns.Add("StatutAffichage", GetType(String))
                End If

                Dim totalFacture As Decimal = 0
                Dim totalAttente As Decimal = 0
                Dim totalPaye As Decimal = 0

                For Each row As DataRow In dt.Rows
                    Dim s As String = Convert.ToString(row("Statut"))
                    Dim m As Decimal = Convert.ToDecimal(row("MontantTotal"))
                    row("StatutAffichage") = MapStatutAffichage(s)

                    totalFacture += m
                    If s = "EN_ATTENTE" Then totalAttente += m
                    If s = "PAYEE" Then totalPaye += m
                Next

                ' Mise à jour des cartes KPI
                lblTotalFacture.Text = totalFacture.ToString("N2") & " FC"
                lblTotalAttente.Text = totalAttente.ToString("N2") & " FC"
                lblTotalPaye.Text = totalPaye.ToString("N2") & " FC"

            gridFactures.DataSource = dt
            For Each ligne As DataGridViewRow In gridFactures.Rows
                If ligne Is Nothing OrElse ligne.IsNewRow Then Continue For
                Dim statutDbLigne As String = Convert.ToString(ligne.Cells(1).Value)
                If String.Equals(statutDbLigne, "ANNULEE", StringComparison.OrdinalIgnoreCase) Then
                    ligne.DefaultCellStyle.ForeColor = ColorDanger
                    ligne.DefaultCellStyle.SelectionForeColor = ColorDanger
                End If
            Next
            Catch ex As Exception
                ' MessageBox.Show("Erreur chargement factures: " & ex.Message)
            End Try
        End Sub

        Private Function MapStatutDb() As String
            If cmbStatut.SelectedItem Is Nothing Then Return ""
            Select Case cmbStatut.SelectedItem.ToString()
                Case "Brouillon"
                    Return "EN_ATTENTE"
                Case "Validee"
                    Return "PAYEE"
                Case "Annulee"
                    Return "ANNULEE"
                Case Else
                    Return ""
            End Select
        End Function

        Private Function MapStatutAffichage(statutDb As String) As String
            Select Case statutDb
                Case "EN_ATTENTE"
                    Return "BROUILLON"
                Case "PAYEE"
                    Return "VALIDÉE"
                Case "ANNULEE"
                    Return "ANNULÉE"
                Case Else
                    Return statutDb
            End Select
        End Function

        Private Sub ColorerStatut(sender As Object, e As DataGridViewCellFormattingEventArgs)
            If gridFactures.Columns(e.ColumnIndex).Name <> "StatutAffichage" Then Return
            If e.Value Is Nothing Then Return

            Dim s As String = e.Value.ToString()
            If s = "BROUILLON" Then
                e.CellStyle.ForeColor = ColorWarning
                e.CellStyle.Font = FontBold
            ElseIf s = "VALIDÉE" Then
                e.CellStyle.ForeColor = ColorSuccess
                e.CellStyle.Font = FontBold
            ElseIf s = "ANNULÉE" Then
                e.CellStyle.ForeColor = ColorDanger
                e.CellStyle.Font = FontBold
            End If
        End Sub

        Private Sub ActionsFacture(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex < 0 Then Return

            Dim row As DataGridViewRow = gridFactures.Rows(e.RowIndex)
            Dim factureId As Integer = Convert.ToInt32(row.Cells(0).Value)
            Dim statutDb As String = Convert.ToString(row.Cells(1).Value)
            Dim numero As String = Convert.ToString(row.Cells(2).Value)
            Dim client As String = Convert.ToString(row.Cells(3).Value)
            Dim tel As String = Convert.ToString(row.Cells(4).Value)

            Dim colName As String = gridFactures.Columns(e.ColumnIndex).Name
            Select Case colName
                Case "ActionVoir"
                    VoirFacture(factureId, numero, client, tel)
                Case "ActionModifier"
                    If statutDb <> "EN_ATTENTE" Then
                        MessageBox.Show("Modification autorisée uniquement pour les brouillons.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Return
                    End If
                    Dim f As New FacturationForm()
                    f.ChargerFacturePourEdition(factureId, numero, client, tel)
                    f.ShowDialog()
                    ChargerFactures(Nothing, EventArgs.Empty)
                Case "ActionAnnuler"
                    If statutDb <> "EN_ATTENTE" Then
                        MessageBox.Show("Seules les factures brouillon peuvent être annulées depuis l'historique.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Return
                    End If
                    If MessageBox.Show("Confirmer l'annulation de la facture ?", "Annuler", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                        Dim repo As New FactureVenteRepository(ObtenirDAL())
                        repo.MettreAJourStatut(factureId, "ANNULEE")
                        AppEvents.OnDataChanged()
                        ChargerFactures(Nothing, EventArgs.Empty)
                    End If
                Case "ActionImprimer"
                    ImprimerFacture(factureId, numero, client, tel)
            End Select
        End Sub

        Private Sub RafraichirDepuisEvenement(sender As Object, e As EventArgs)
            If IsDisposed Then Return
            If InvokeRequired Then
                BeginInvoke(New MethodInvoker(Sub() RafraichirDepuisEvenement(Nothing, EventArgs.Empty)))
                Return
            End If
            If _isRefreshingFromEvent Then Return

            _isRefreshingFromEvent = True
            Try
                ChargerFactures(Nothing, EventArgs.Empty)
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireFactures", "RafraichirDepuisEvenement", "Erreur lors du rafraichissement automatique des factures.", ex)
            Finally
                _isRefreshingFromEvent = False
            End Try
        End Sub
        Private Sub VoirFacture(factureId As Integer, numero As String, client As String, tel As String)
            Try
                Dim dt As DataTable = New LigneFactureVenteRepository(ObtenirDAL()).ListerDetailsParFacture(factureId)
                Dim entete As DataRow = ChargerEnteteFacture(factureId)
                Using apercu As New FormApercuFacture(numero, client, tel, entete, dt)
                    apercu.ShowDialog(Me)
                End Using
            Catch ex As Exception
                MessageBox.Show("Erreur affichage facture: " & ex.Message)
            End Try

        End Sub

        Private Function ChargerEnteteFacture(factureId As Integer) As DataRow
            Dim sql As String =
                "SELECT f.NumeroFacture, f.SousTotal, f.MontantRemise, f.MontantTotal, f.Statut, f.CreeLe, " &
                "ISNULL(c.NomClient,'') AS ClientNom, ISNULL(c.Telephone,'') AS Telephone, ISNULL(f.ModifierPar,'') AS Facturier " &
                "FROM FacturesVente f LEFT JOIN Clients c ON c.ClientId=f.ClientId WHERE f.FactureVenteId=@Id"
            Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {New System.Data.SqlClient.SqlParameter("@Id", factureId)}
            Dim dt As DataTable = ObtenirDAL().ExecuterTable(sql, CommandType.Text, p)
            If dt.Rows.Count = 0 Then Return Nothing
            Return dt.Rows(0)
        End Function

        Private NotInheritable Class FormApercuFacture
            Inherits Form

            Private ReadOnly ColorPrimary As Color = Color.FromArgb(41, 128, 185)
            Private ReadOnly ColorDanger As Color = Color.FromArgb(192, 57, 43)
            Private ReadOnly ColorSuccess As Color = Color.FromArgb(16, 185, 129)
            Private ReadOnly ColorWarning As Color = Color.FromArgb(245, 158, 11)

            Public Sub New(numero As String, client As String, telephone As String, entete As DataRow, lignes As DataTable)
                Text = "Aperçu facture"
                StartPosition = FormStartPosition.CenterParent
                MinimumSize = New Size(850, 620)
                Size = New Size(980, 700)
                BackColor = Color.FromArgb(245, 247, 250)

                Dim statut As String = If(entete Is Nothing, String.Empty, Convert.ToString(entete("Statut")))
                Dim nombreLignes As Integer = If(lignes Is Nothing, 0, lignes.Rows.Count)
                Dim hauteurGrille As Integer = Math.Min(360, Math.Max(155, 44 + (Math.Max(1, nombreLignes) * 42)))
                Dim root As New TableLayoutPanel() With {
                    .Dock = DockStyle.Fill,
                    .BackColor = Color.FromArgb(245, 247, 250),
                    .ColumnCount = 1,
                    .RowCount = 4,
                    .Padding = New Padding(18)
                }
                root.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                root.RowStyles.Add(New RowStyle(SizeType.Absolute, 154))
                root.RowStyles.Add(New RowStyle(SizeType.Absolute, hauteurGrille))
                root.RowStyles.Add(New RowStyle(SizeType.Absolute, 112))
                root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

                Dim titre As String = If(String.Equals(statut, "EN_ATTENTE", StringComparison.OrdinalIgnoreCase), "PROFORMA", "FACTURE")
                If String.Equals(statut, "ANNULEE", StringComparison.OrdinalIgnoreCase) Then titre = "FACTURE ANNULÉE"
                Dim header As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(22), .Margin = New Padding(0, 0, 0, 10)}
                Dim lblTitre As New Label() With {.Text = titre & " " & numero, .Font = New Font("Segoe UI", 16, FontStyle.Bold), .ForeColor = ColorPrimary, .AutoSize = True, .Left = 22, .Top = 16}
                Dim lblInfos As New Label() With {
                    .Text = "Date : " & If(entete Is Nothing, Date.Now.ToString("dd/MM/yyyy HH:mm"), Convert.ToDateTime(entete("CreeLe")).ToString("dd/MM/yyyy HH:mm")) & Environment.NewLine &
                            "Client : " & If(String.IsNullOrWhiteSpace(client), "CLIENT", client) & Environment.NewLine &
                            "Téléphone : " & telephone & Environment.NewLine &
                            "Facturier : " & If(entete Is Nothing, String.Empty, Convert.ToString(entete("Facturier"))) & Environment.NewLine &
                            "Statut : " & FacturePrintRenderer.StatutAffichage(statut),
                    .Font = New Font("Segoe UI", 9.5F),
                    .ForeColor = Color.FromArgb(52, 73, 94),
                    .Left = 24,
                    .Top = 50,
                    .Width = 620,
                    .Height = 96
                }
                header.Controls.AddRange(New Control() {lblTitre, lblInfos})

                Dim statutColor As Color = CouleurStatut(statut)
                Dim lblStatut As New Label() With {.Text = FacturePrintRenderer.StatutAffichage(statut), .Font = New Font("Segoe UI", 12, FontStyle.Bold), .ForeColor = Color.White, .BackColor = statutColor, .TextAlign = ContentAlignment.MiddleCenter, .Anchor = AnchorStyles.Top Or AnchorStyles.Right, .Left = 690, .Top = 28, .Width = 220, .Height = 42}
                header.Controls.Add(lblStatut)

                Dim grid As New DataGridView() With {
                    .Dock = DockStyle.Fill,
                    .ReadOnly = True,
                    .AllowUserToAddRows = False,
                    .AllowUserToDeleteRows = False,
                    .AutoGenerateColumns = False,
                    .RowHeadersVisible = False,
                    .BackgroundColor = Color.White,
                    .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    .ScrollBars = ScrollBars.Both,
                    .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                    .CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                    .GridColor = Color.FromArgb(229, 231, 235),
                    .Margin = New Padding(0, 0, 0, 10),
                    .RowTemplate = New DataGridViewRow() With {.Height = 42}
                }
                grid.EnableHeadersVisualStyles = False
                grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94)
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
                grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
                grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 73, 94)
                grid.ColumnHeadersHeight = 40
                grid.DefaultCellStyle.Font = New Font("Segoe UI", 9.5F)
                grid.DefaultCellStyle.Padding = New Padding(5, 0, 5, 0)
                grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 234, 246)
                grid.DefaultCellStyle.SelectionForeColor = ColorPrimary
                Dim colProduit As New DataGridViewTextBoxColumn() With {.DataPropertyName = "Libelle", .HeaderText = "Produit", .MinimumWidth = 260, .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .FillWeight = 48}
                grid.Columns.Add(colProduit)
                grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "TypeVente", .HeaderText = "Conditionnement", .Width = 175, .MinimumWidth = 150})
                grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "QuantiteSaisie", .HeaderText = "Quantité", .Width = 95, .MinimumWidth = 85, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "N2", .Alignment = DataGridViewContentAlignment.MiddleRight}})
                grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixUnitaire", .HeaderText = "Prix unitaire", .Width = 130, .MinimumWidth = 115, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "N0", .Alignment = DataGridViewContentAlignment.MiddleRight}})
                grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "MontantLigne", .HeaderText = "Total", .Width = 130, .MinimumWidth = 115, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "N0", .Alignment = DataGridViewContentAlignment.MiddleRight}})
                grid.DataSource = lignes

                Dim footer As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(20), .Margin = New Padding(0)}
                Dim sousTotal As Decimal = If(entete Is Nothing OrElse entete.IsNull("SousTotal"), CalculerSommeLignes(lignes), Convert.ToDecimal(entete("SousTotal")))
                Dim remise As Decimal = If(entete Is Nothing OrElse entete.IsNull("MontantRemise"), 0D, Convert.ToDecimal(entete("MontantRemise")))
                Dim total As Decimal = If(entete Is Nothing OrElse entete.IsNull("MontantTotal"), sousTotal - remise, Convert.ToDecimal(entete("MontantTotal")))
                Dim pnlTotaux As New TableLayoutPanel() With {.Dock = DockStyle.Right, .Width = 300, .ColumnCount = 2, .RowCount = 3}
                pnlTotaux.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 46))
                pnlTotaux.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 54))
                AjouterTotalPreview(pnlTotaux, "Sous-total", sousTotal, New Font("Segoe UI", 9.5F), Color.FromArgb(31, 41, 55), 0)
                AjouterTotalPreview(pnlTotaux, "Remise", remise, New Font("Segoe UI", 9.5F), Color.FromArgb(31, 41, 55), 1)
                AjouterTotalPreview(pnlTotaux, "TOTAL", total, New Font("Segoe UI", 11, FontStyle.Bold), ColorPrimary, 2)
                footer.Controls.Add(pnlTotaux)

                root.Controls.Add(header, 0, 0)
                root.Controls.Add(grid, 0, 1)
                root.Controls.Add(footer, 0, 2)
                Controls.Add(root)
            End Sub

            Private Sub AjouterTotalPreview(panel As TableLayoutPanel, libelle As String, montant As Decimal, font As Font, couleur As Color, row As Integer)
                panel.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))
                panel.Controls.Add(New Label() With {.Text = libelle & " :", .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft, .Font = font, .ForeColor = couleur}, 0, row)
                panel.Controls.Add(New Label() With {.Text = montant.ToString("N0") & " FC", .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleRight, .Font = font, .ForeColor = couleur}, 1, row)
            End Sub

            Private Function CouleurStatut(statut As String) As Color
                Select Case If(statut, String.Empty).Trim().ToUpperInvariant()
                    Case "ANNULEE"
                        Return ColorDanger
                    Case "PAYEE"
                        Return ColorSuccess
                    Case "EN_ATTENTE"
                        Return ColorWarning
                    Case Else
                        Return ColorPrimary
                End Select
            End Function

            Private Shared Function CalculerSommeLignes(lignes As DataTable) As Decimal
                Dim total As Decimal = 0D
                If lignes Is Nothing Then Return total
                For Each r As DataRow In lignes.Rows
                    If Not r.IsNull("MontantLigne") Then total += Convert.ToDecimal(r("MontantLigne"))
                Next
                Return total
            End Function
        End Class

        Private Sub ImprimerFacture(factureId As Integer, numero As String, client As String, tel As String)
            Try
                Dim data As FacturePrintData = ConstruireDonneesFacture(factureId, numero, client, tel)
                Dim param As ParametreDTO = PrintConfigurationHelper.ChargerParametres()
                Using doc As Printing.PrintDocument = FacturePrintRenderer.CreerDocumentA4(data, param)
                    param = PrintConfigurationHelper.ConfigurerDocumentA4(doc, Me, "FormulaireFactures", "ImprimerFacture")

                    If param IsNot Nothing AndAlso param.ApercuAvantImpression Then
                        Using preview As New PrintPreviewDialog()
                            preview.Document = doc
                            preview.ShowDialog(Me)
                        End Using
                    Else
                        doc.Print()
                    End If
                End Using
            Catch ex As Exception
                MessageBox.Show("Erreur impression facture: " & ex.Message)
            End Try
        End Sub

        Private Function ConstruireDonneesFacture(factureId As Integer, numero As String, client As String, tel As String) As FacturePrintData
            Dim entete As DataRow = ChargerEnteteFacture(factureId)
            If entete Is Nothing Then Throw New InvalidOperationException("Facture introuvable.")

            Dim lignesTable As DataTable = New LigneFactureVenteRepository(ObtenirDAL()).ListerDetailsParFacture(factureId)
            Dim lignes As New List(Of FacturePrintLine)()
            For Each row As DataRow In lignesTable.Rows
                Dim quantite As Decimal = If(row.IsNull("QuantiteSaisie"), Convert.ToDecimal(row("Quantite")), Convert.ToDecimal(row("QuantiteSaisie")))
                lignes.Add(New FacturePrintLine With {
                    .Produit = Convert.ToString(row("Libelle")),
                    .Conditionnement = Convert.ToString(row("TypeVente")),
                    .Quantite = quantite,
                    .PrixUnitaire = Convert.ToDecimal(row("PrixUnitaire")),
                    .Montant = Convert.ToDecimal(row("MontantLigne"))
                })
            Next

            Return New FacturePrintData With {
                .Numero = Convert.ToString(entete("NumeroFacture")),
                .DateDocument = Convert.ToDateTime(entete("CreeLe")),
                .Client = If(String.IsNullOrWhiteSpace(Convert.ToString(entete("ClientNom"))), client, Convert.ToString(entete("ClientNom"))),
                .Telephone = If(String.IsNullOrWhiteSpace(Convert.ToString(entete("Telephone"))), tel, Convert.ToString(entete("Telephone"))),
                .Facturier = Convert.ToString(entete("Facturier")),
                .Statut = Convert.ToString(entete("Statut")),
                .SousTotal = Convert.ToDecimal(entete("SousTotal")),
                .Remise = Convert.ToDecimal(entete("MontantRemise")),
                .Total = Convert.ToDecimal(entete("MontantTotal")),
                .Lignes = lignes
            }
        End Function

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            RemoveHandler AppEvents.DataChanged, AddressOf RafraichirDepuisEvenement
            MyBase.OnFormClosed(e)
        End Sub

    End Class
End Namespace
