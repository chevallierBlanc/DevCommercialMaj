Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.IO
Imports System.Collections.Generic
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireFactures
        Inherits Form

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

        Public Sub New()
            Me.Text = "Historique des factures"
            Me.Width = 1200
            Me.Height = 720
            Me.StartPosition = FormStartPosition.CenterScreen

            Dim panelFiltres As New Panel() With {.Dock = DockStyle.Top, .Height = 120}
            Dim lblNumero As New Label() With {.Text = "Numero facture", .Left = 20, .Top = 12, .AutoSize = True}
            txtNumero = New TextBox() With {.Left = 20, .Top = 32, .Width = 160}

            Dim lblNom As New Label() With {.Text = "Nom client", .Left = 200, .Top = 12, .AutoSize = True}
            txtNomClient = New TextBox() With {.Left = 200, .Top = 32, .Width = 200}

            Dim lblTel As New Label() With {.Text = "Telephone", .Left = 420, .Top = 12, .AutoSize = True}
            txtTelephone = New TextBox() With {.Left = 420, .Top = 32, .Width = 160}

            chkDate = New CheckBox() With {.Text = "Filtrer par date", .Left = 600, .Top = 12, .AutoSize = True}
            dtDu = New DateTimePicker() With {.Left = 600, .Top = 32, .Width = 140, .Format = DateTimePickerFormat.Short}
            dtAu = New DateTimePicker() With {.Left = 750, .Top = 32, .Width = 140, .Format = DateTimePickerFormat.Short}

            Dim lblStatut As New Label() With {.Text = "Statut", .Left = 910, .Top = 12, .AutoSize = True}
            cmbStatut = New ComboBox() With {.Left = 910, .Top = 32, .Width = 140, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbStatut.Items.AddRange(New Object() {"Tous", "Brouillon", "Validee", "Annulee"})
            cmbStatut.SelectedIndex = 0

            btnActualiser = New Button() With {.Text = "Actualiser", .Left = 1060, .Top = 30, .Width = 100}

            panelFiltres.Controls.Add(lblNumero)
            panelFiltres.Controls.Add(txtNumero)
            panelFiltres.Controls.Add(lblNom)
            panelFiltres.Controls.Add(txtNomClient)
            panelFiltres.Controls.Add(lblTel)
            panelFiltres.Controls.Add(txtTelephone)
            panelFiltres.Controls.Add(chkDate)
            panelFiltres.Controls.Add(dtDu)
            panelFiltres.Controls.Add(dtAu)
            panelFiltres.Controls.Add(lblStatut)
            panelFiltres.Controls.Add(cmbStatut)
            panelFiltres.Controls.Add(btnActualiser)

            gridFactures = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True,
                .AutoGenerateColumns = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AllowUserToAddRows = False,
                .RowHeadersVisible = False
            }

            Me.Controls.Add(gridFactures)
            Me.Controls.Add(panelFiltres)

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

            ThemeHelper.AppliquerTheme(Me)
            ConfigurerGrille()
            ChargerFactures(Nothing, EventArgs.Empty)

            timer = New Timer() With {.Interval = 600000}
            AddHandler timer.Tick, AddressOf ChargerFactures
            timer.Start()
        End Sub

        Private Sub ConfigurerGrille()
            gridFactures.Columns.Clear()

            Dim colId As New DataGridViewTextBoxColumn() With {.DataPropertyName = "FactureVenteId", .Name = "FactureVenteId", .Visible = False}
            Dim colStatutDb As New DataGridViewTextBoxColumn() With {.DataPropertyName = "Statut", .Name = "Statut", .Visible = False}

            Dim colNumero As New DataGridViewTextBoxColumn() With {.DataPropertyName = "NumeroFacture", .HeaderText = "Numero facture", .Width = 140}
            Dim colClient As New DataGridViewTextBoxColumn() With {.DataPropertyName = "ClientNom", .HeaderText = "Client", .Width = 200}
            Dim colTel As New DataGridViewTextBoxColumn() With {.DataPropertyName = "Telephone", .HeaderText = "Telephone", .Width = 140}
            Dim colDate As New DataGridViewTextBoxColumn() With {.DataPropertyName = "CreeLe", .HeaderText = "Date", .Width = 140}
            Dim colMontant As New DataGridViewTextBoxColumn() With {.DataPropertyName = "MontantTotal", .HeaderText = "Montant total", .Width = 120}
            Dim colStatut As New DataGridViewTextBoxColumn() With {.DataPropertyName = "StatutAffichage", .HeaderText = "Statut", .Width = 120}

            Dim colVoir As New DataGridViewButtonColumn() With {.Name = "ActionVoir", .HeaderText = "Voir", .Text = "Voir", .UseColumnTextForButtonValue = True, .Width = 80}
            Dim colModifier As New DataGridViewButtonColumn() With {.Name = "ActionModifier", .HeaderText = "Modifier", .Text = "Modifier", .UseColumnTextForButtonValue = True, .Width = 90}
            Dim colAnnuler As New DataGridViewButtonColumn() With {.Name = "ActionAnnuler", .HeaderText = "Annuler", .Text = "Annuler", .UseColumnTextForButtonValue = True, .Width = 90}
            Dim colImprimer As New DataGridViewButtonColumn() With {.Name = "ActionImprimer", .HeaderText = "Imprimer", .Text = "Imprimer", .UseColumnTextForButtonValue = True, .Width = 90}

            gridFactures.Columns.AddRange(New DataGridViewColumn() {colId, colStatutDb, colNumero, colClient, colTel, colDate, colMontant, colStatut, colVoir, colModifier, colAnnuler, colImprimer})
        End Sub

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

                For Each row As DataRow In dt.Rows
                    Dim s As String = Convert.ToString(row("Statut"))
                    row("StatutAffichage") = MapStatutAffichage(s)
                Next

                gridFactures.DataSource = dt
            Catch ex As Exception
                MessageBox.Show("Erreur chargement factures: " & ex.Message)
            End Try
        End Sub

        Private Function MapStatutDb() As String
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
                    Return "VALIDEE"
                Case "ANNULEE"
                    Return "ANNULEE"
                Case Else
                    Return statutDb
            End Select
        End Function

        Private Sub ColorerStatut(sender As Object, e As DataGridViewCellFormattingEventArgs)
            If gridFactures.Columns(e.ColumnIndex).Name <> "StatutAffichage" Then Return
            If e.Value Is Nothing Then Return

            Dim s As String = e.Value.ToString()
            If s = "BROUILLON" Then
                e.CellStyle.BackColor = Color.Khaki
            ElseIf s = "VALIDEE" Then
                e.CellStyle.BackColor = Color.LightGreen
            ElseIf s = "ANNULEE" Then
                e.CellStyle.BackColor = Color.LightCoral
            End If
        End Sub

        Private Sub ActionsFacture(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex < 0 Then Return

            Dim row As DataGridViewRow = gridFactures.Rows(e.RowIndex)
            Dim factureId As Integer = Convert.ToInt32(row.Cells("FactureVenteId").Value)
            Dim statutDb As String = Convert.ToString(row.Cells("Statut").Value)
            Dim numero As String = Convert.ToString(row.Cells("NumeroFacture").Value)
            Dim client As String = Convert.ToString(row.Cells("ClientNom").Value)
            Dim tel As String = Convert.ToString(row.Cells("Telephone").Value)

            Dim colName As String = gridFactures.Columns(e.ColumnIndex).Name
            Select Case colName
                Case "ActionVoir"
                    VoirFacture(factureId, numero, client, tel)
                Case "ActionModifier"
                    If statutDb <> "EN_ATTENTE" Then
                        MessageBox.Show("Modification autorisee uniquement pour les brouillons.")
                        Return
                    End If
                    Dim rep As DialogResult = MessageBox.Show("Ouvrir le module Facturier pour modifier ce brouillon ?", "Modification", MessageBoxButtons.YesNo)
                    If rep = DialogResult.Yes Then
                        Dim f As New FacturationForm()
                        f.ShowDialog()
                    End If
                Case "ActionAnnuler"
                    If statutDb <> "PAYEE" Then
                        MessageBox.Show("Annulation autorisee uniquement pour les factures validees.")
                        Return
                    End If
                    Dim confirm As DialogResult = MessageBox.Show("Confirmer l'annulation de la facture ?", "Annuler", MessageBoxButtons.YesNo)
                    If confirm = DialogResult.Yes Then
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
                Dim dt As DataTable = repo.ListerDetailsParFacture(factureId)
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
