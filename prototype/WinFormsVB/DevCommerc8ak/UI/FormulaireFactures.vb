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

            Dim colNumero As New DataGridViewTextBoxColumn() With {.DataPropertyName = "NumeroFacture", .HeaderText = "N° FACTURE", .Width = 130}
            Dim colClient As New DataGridViewTextBoxColumn() With {.DataPropertyName = "ClientNom", .HeaderText = "CLIENT", .Width = 180}
            Dim colTel As New DataGridViewTextBoxColumn() With {.DataPropertyName = "Telephone", .HeaderText = "TÉLÉPHONE", .Width = 120}
            Dim colDate As New DataGridViewTextBoxColumn() With {.DataPropertyName = "CreeLe", .HeaderText = "DATE", .Width = 130}
            Dim colMontant As New DataGridViewTextBoxColumn() With {.DataPropertyName = "MontantTotal", .HeaderText = "MONTANT TOTAL", .Width = 130}
            Dim colStatut As New DataGridViewTextBoxColumn() With {.DataPropertyName = "StatutAffichage", .HeaderText = "STATUT", .Width = 110}

            ' Boutons d'action stylisés
            Dim colVoir As New DataGridViewButtonColumn() With {.Name = "ActionVoir", .HeaderText = "", .Text = "VOIR", .UseColumnTextForButtonValue = True, .Width = 70}
            Dim colModifier As New DataGridViewButtonColumn() With {.Name = "ActionModifier", .HeaderText = "", .Text = "ÉDITER", .UseColumnTextForButtonValue = True, .Width = 70}
            Dim colAnnuler As New DataGridViewButtonColumn() With {.Name = "ActionAnnuler", .HeaderText = "", .Text = "ANNULER", .UseColumnTextForButtonValue = True, .Width = 80}
            Dim colImprimer As New DataGridViewButtonColumn() With {.Name = "ActionImprimer", .HeaderText = "", .Text = "IMPRIMER", .UseColumnTextForButtonValue = True, .Width = 85}

            gridFactures.Columns.AddRange(New DataGridViewColumn() {colId, colStatutDb, colNumero, colClient, colTel, colDate, colMontant, colStatut, colVoir, colModifier, colAnnuler, colImprimer})

            ' Style des en-têtes
            gridFactures.EnableHeadersVisualStyles = False
            gridFactures.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250)
            gridFactures.ColumnHeadersDefaultCellStyle.ForeColor = ColorTextSecondary
            gridFactures.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 8, FontStyle.Bold)
            gridFactures.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
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
                    f.ShowDialog()
                Case "ActionAnnuler"
                    If statutDb <> "PAYEE" Then
                        MessageBox.Show("Annulation autorisée uniquement pour les factures validées.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Return
                    End If
                    If MessageBox.Show("Confirmer l'annulation de la facture ?", "Annuler", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                        Dim repo As New FactureVenteRepository(ObtenirDAL())
                        repo.MettreAJourStatut(factureId, "ANNULEE")
                        ChargerFactures(Nothing, EventArgs.Empty)
                    End If
                Case "ActionImprimer"
                    ImprimerFacture(factureId, numero, client, tel)
            End Select
        End Sub
        Private Sub VoirFacture(factureId As Integer, numero As String, client As String, tel As String)
            Try
                Dim repo As New LigneFactureVenteRepository(ObtenirDAL())
                Dim dt As DataTable = repo.ListerDetailsParFacture(factureId) ' nom de colonne modifier 
                Dim lignes As New List(Of String)()
                For Each r As DataRow In dt.Rows
                    lignes.Add(Convert.ToString(r("Libelle")) & " x" & Convert.ToDecimal(r("Quantite")).ToString() & " = " & Convert.ToDecimal(r("MontantLigne")).ToString())
                Next
                Dim details As String = "Facture: " & numero & Environment.NewLine &
                    "Client: " & client & Environment.NewLine &
                    "Telephone: " & tel & Environment.NewLine &
                    "Lignes:" & Environment.NewLine & String.Join(Environment.NewLine, lignes)
                MessageBox.Show(details, "Apercu facture")
            Catch ex As Exception
                MessageBox.Show("Erreur affichage facture: " & ex.Message)
            End Try

        End Sub

        Private Sub ImprimerFacture(factureId As Integer, numero As String, client As String, tel As String)
            Try
                Dim dal As DAL = ObtenirDAL()
                Dim param As ParametreDTO = (New ParametreService(New ParametreRepository(dal))).Charger()
                Dim repo As New LigneFactureVenteRepository(dal)
                Dim dt As DataTable = repo.ListerDetailsParFacture(factureId)

                Dim doc As New Printing.PrintDocument()
                If param IsNot Nothing AndAlso param.ImprimanteA4 <> "" Then
                    doc.PrinterSettings.PrinterName = param.ImprimanteA4
                End If
                doc.DefaultPageSettings.Color = If(param IsNot Nothing, param.ImpressionCouleur, True)

                AddHandler doc.PrintPage,
                    Sub(s, e)
                        Dim y As Integer = 20
                        Dim x As Integer = 20
                        If param IsNot Nothing AndAlso param.LogoPath <> "" AndAlso File.Exists(param.LogoPath) Then
                            Using img As Image = Image.FromFile(param.LogoPath)
                                e.Graphics.DrawImage(img, x, y, 60, 60)
                            End Using
                            x += 70
                        End If

                        Dim nomMag As String = If(param IsNot Nothing, param.NomMagasin, "")
                        Dim adr As String = If(param IsNot Nothing, param.AdresseMagasin, "")
                        Dim telMag As String = If(param IsNot Nothing, param.TelephoneMagasin, "")
                        e.Graphics.DrawString(nomMag, New Font("Segoe UI", 14, FontStyle.Bold), Brushes.Black, x, y)
                        y += 24
                        e.Graphics.DrawString(adr, New Font("Segoe UI", 10), Brushes.Black, x, y)
                        y += 18
                        e.Graphics.DrawString(telMag, New Font("Segoe UI", 10), Brushes.Black, x, y)
                        y += 26

                        e.Graphics.DrawString("Facture: " & numero, New Font("Segoe UI", 10, FontStyle.Bold), Brushes.Black, 20, y)
                        y += 18
                        e.Graphics.DrawString("Date: " & Date.Now.ToString("dd/MM/yyyy HH:mm"), New Font("Segoe UI", 10), Brushes.Black, 20, y)
                        y += 18
                        e.Graphics.DrawString("Client: " & client, New Font("Segoe UI", 10), Brushes.Black, 20, y)
                        y += 18
                        e.Graphics.DrawString("Telephone: " & tel, New Font("Segoe UI", 10), Brushes.Black, 20, y)
                        y += 24

                        e.Graphics.DrawString("DETAILS", New Font("Segoe UI", 11, FontStyle.Bold), Brushes.Black, 20, y)
                        y += 20

                        For Each r As DataRow In dt.Rows
                            Dim line As String = Convert.ToString(r("Libelle")) & " x" & Convert.ToDecimal(r("Quantite")).ToString() & " = " & Convert.ToDecimal(r("MontantLigne")).ToString()
                            e.Graphics.DrawString(line, New Font("Segoe UI", 10), Brushes.Black, 20, y)
                            y += 18
                        Next
                    End Sub

                If param IsNot Nothing AndAlso param.ApercuAvantImpression Then
                    Dim preview As New PrintPreviewDialog()
                    preview.Document = doc
                    preview.ShowDialog()
                Else
                    doc.Print()
                End If
            Catch ex As Exception
                MessageBox.Show("Erreur impression facture: " & ex.Message)
            End Try
        End Sub

    End Class
End Namespace
