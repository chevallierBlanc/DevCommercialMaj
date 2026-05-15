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
            Me.BackColor = Color.White
            Me.Text = "Caisse"
            Me.Width = 1250
            Me.Height = 740
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.KeyPreview = True

            Dim panelGauche As New Panel() With {.Left = 10, .Top = 10, .Width = 380, .Height = 680}
            Dim panelCentre As New Panel() With {.Left = 400, .Top = 10, .Width = 460, .Height = 680}
            Dim panelDroite As New Panel() With {.Left = 870, .Top = 10, .Width = 360, .Height = 680}

            Dim lblRecherche As New Label() With {.Text = "Recherche (numero, client, telephone)", .Left = 10, .Top = 10, .AutoSize = True}
            txtRecherche = New TextBox() With {.Left = 10, .Top = 30, .Width = 260}
            chkDate = New CheckBox() With {.Text = "Date", .Left = 10, .Top = 62, .AutoSize = True}
            dtDate = New DateTimePicker() With {.Left = 70, .Top = 60, .Width = 120, .Format = DateTimePickerFormat.Short}
            btnActualiser = New Button() With {.Text = "Actualiser", .Left = 200, .Top = 58, .Width = 90}

            gridFactures = New DataGridView() With {
                .Left = 10,
                .Top = 95,
                .Width = 360,
                .Height = 570,
                .ReadOnly = True,
                .AutoGenerateColumns = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AllowUserToAddRows = False,
                .RowHeadersVisible = False
            }

            panelGauche.Controls.Add(lblRecherche)
            panelGauche.Controls.Add(txtRecherche)
            panelGauche.Controls.Add(chkDate)
            panelGauche.Controls.Add(dtDate)
            panelGauche.Controls.Add(btnActualiser)
            panelGauche.Controls.Add(gridFactures)

            Dim lblTitreDetail As New Label() With {.Text = "Details facture", .Left = 10, .Top = 10, .AutoSize = True, .Font = New Font("Segoe UI", 11, FontStyle.Bold)}
            lblNumeroFacture = New Label() With {.Left = 10, .Top = 40, .AutoSize = True}
            lblClient = New Label() With {.Left = 10, .Top = 65, .AutoSize = True}
            lblDateFacture = New Label() With {.Left = 10, .Top = 90, .AutoSize = True}

            gridDetails = New DataGridView() With {
                .Left = 10,
                .Top = 120,
                .Width = 440,
                .Height = 545,
                .ReadOnly = True,
                .AutoGenerateColumns = True,
                .AllowUserToAddRows = False,
                .RowHeadersVisible = False
            }

            panelCentre.Controls.Add(lblTitreDetail)
            panelCentre.Controls.Add(lblNumeroFacture)
            panelCentre.Controls.Add(lblClient)
            panelCentre.Controls.Add(lblDateFacture)
            panelCentre.Controls.Add(gridDetails)

            Dim lblTotalTitre As New Label() With {.Text = "TOTAL", .Left = 10, .Top = 10, .AutoSize = True, .Font = New Font("Segoe UI", 12, FontStyle.Bold)}
            lblTotal = New Label() With {.Left = 10, .Top = 38, .AutoSize = True, .Font = New Font("Segoe UI", 16, FontStyle.Bold), .ForeColor = Color.DarkBlue}

            Dim lblRecu As New Label() With {.Text = "Montant recu", .Left = 10, .Top = 90, .AutoSize = True}
            txtMontantRecu = New TextBox() With {.Left = 10, .Top = 110, .Width = 200}

            Dim lblDevise As New Label() With {.Text = "Devise", .Left = 220, .Top = 90, .AutoSize = True}
            cmbDevise = New ComboBox() With {.Left = 220, .Top = 110, .Width = 100, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbDevise.Items.AddRange(New Object() {"FC", "USD"})
            cmbDevise.SelectedIndex = 0

            lblMonnaie = New Label() With {.Left = 10, .Top = 145, .AutoSize = True, .Font = New Font("Segoe UI", 10, FontStyle.Bold)}

            Dim lblMode As New Label() With {.Text = "Mode paiement", .Left = 10, .Top = 185, .AutoSize = True}
            cmbMode = New ComboBox() With {.Left = 10, .Top = 205, .Width = 200, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbMode.Items.AddRange(New Object() {"CASH", "MOBILE_MONEY", "CARTE", "AUTRE"})
            cmbMode.SelectedIndex = 0

            Dim lblRef As New Label() With {.Text = "Reference", .Left = 10, .Top = 240, .AutoSize = True}
            txtReference = New TextBox() With {.Left = 10, .Top = 260, .Width = 200}

            btnEncaisser = New Button() With {.Text = "Encaisser", .Left = 10, .Top = 310, .Width = 140}
            btnImprimer = New Button() With {.Text = "Imprimer ticket", .Left = 160, .Top = 310, .Width = 140}
            btnAnnuler = New Button() With {.Text = "Annuler", .Left = 10, .Top = 350, .Width = 140}

            panelDroite.Controls.Add(lblTotalTitre)
            panelDroite.Controls.Add(lblTotal)
            panelDroite.Controls.Add(lblRecu)
            panelDroite.Controls.Add(txtMontantRecu)
            panelDroite.Controls.Add(lblDevise)
            panelDroite.Controls.Add(cmbDevise)
            panelDroite.Controls.Add(lblMonnaie)
            panelDroite.Controls.Add(lblMode)
            panelDroite.Controls.Add(cmbMode)
            panelDroite.Controls.Add(lblRef)
            panelDroite.Controls.Add(txtReference)
            panelDroite.Controls.Add(btnEncaisser)
            panelDroite.Controls.Add(btnImprimer)
            panelDroite.Controls.Add(btnAnnuler)

            Me.Controls.Add(panelGauche)
            Me.Controls.Add(panelCentre)
            Me.Controls.Add(panelDroite)

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

            ThemeHelper.AppliquerTheme(Me)
            ConfigurerGrilleFactures()
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
            gridFactures.Columns.AddRange(New DataGridViewColumn() {colId, colNumero, colClient, colTel, colDate, colTotal})
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
            Catch ex As Exception
                MessageBox.Show("Erreur chargement factures: " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerDetails(sender As Object, e As EventArgs)
            If gridFactures.CurrentRow Is Nothing Then Return
            Dim numero As String = Convert.ToString(gridFactures.CurrentRow.Cells("NumeroFacture").Value)
            Dim client As String = Convert.ToString(gridFactures.CurrentRow.Cells("ClientNom").Value)
            Dim tel As String = Convert.ToString(gridFactures.CurrentRow.Cells("Telephone").Value)
            Dim dtFacture As Date = Convert.ToDateTime(gridFactures.CurrentRow.Cells("CreeLe").Value)
            _totalCourant = Convert.ToDecimal(gridFactures.CurrentRow.Cells("MontantTotal").Value)

            lblNumeroFacture.Text = "Facture: " & numero
            lblClient.Text = "Client: " & client & " / " & tel
            lblDateFacture.Text = "Date: " & dtFacture.ToString("dd/MM/yyyy")

            ChargerLignes()
            lblTotal.Text = _totalCourant.ToString() & " FC"
            txtMontantRecu.Text = _totalCourant.ToString()
            CalculerMonnaie(Nothing, EventArgs.Empty)
        End Sub

        Private Sub ChargerLignes()
            If gridFactures.CurrentRow Is Nothing Then Return
            Dim factureId As Integer = Convert.ToInt32(gridFactures.CurrentRow.Cells("FactureVenteId").Value)
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
                lblMonnaie.Text = "Monnaie: " & monnaieFC.ToString() & " FC (" & monnaieUSD.ToString("0.00") & " USD)"
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

                Dim factureId As Integer = Convert.ToInt32(gridFactures.CurrentRow.Cells("FactureVenteId").Value)
                Dim monnaieFC As Decimal = montantFC - _totalCourant

                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim service As New FacturationService(dal)
                service.EncaisserFacture(factureId, cmbMode.SelectedItem.ToString(), txtReference.Text.Trim(), montantFC, monnaieFC, devise, SessionUtilisateur.UtilisateurId)

                _dernierTicket = ConstruireTicketDepuisSelection(montantFC, monnaieFC, devise)
                MessageBox.Show("Paiement reussi.")
                ImprimerTicket(Nothing, EventArgs.Empty)
                ChargerFactures(Nothing, EventArgs.Empty)
                AnnulerSelection(Nothing, EventArgs.Empty)
            Catch ex As Exception
                MessageBox.Show("Erreur paiement: " & ex.Message)
            End Try
        End Sub

        Private Function ConstruireTicketDepuisSelection(montantRecuFc As Decimal, monnaieFc As Decimal, devise As String) As TicketData
            Dim ticket As New TicketData()
            ticket.Numero = Convert.ToString(gridFactures.CurrentRow.Cells("NumeroFacture").Value)
            ticket.Client = Convert.ToString(gridFactures.CurrentRow.Cells("ClientNom").Value)
            ticket.Telephone = Convert.ToString(gridFactures.CurrentRow.Cells("Telephone").Value)
            ticket.DateFacture = Convert.ToDateTime(gridFactures.CurrentRow.Cells("CreeLe").Value)
            ticket.Total = Convert.ToDecimal(gridFactures.CurrentRow.Cells("MontantTotal").Value)
            ticket.MontantRecu = montantRecuFc
            ticket.Monnaie = monnaieFc
            ticket.Devise = devise

            Dim factureId As Integer = Convert.ToInt32(gridFactures.CurrentRow.Cells("FactureVenteId").Value)
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

                Dim doc As New Printing.PrintDocument()
                If _param IsNot Nothing AndAlso _param.ImprimanteTicket <> "" Then
                    doc.PrinterSettings.PrinterName = _param.ImprimanteTicket
                End If

                doc.DefaultPageSettings.Color = If(_param IsNot Nothing, _param.ImpressionCouleur, True)
                AddHandler doc.PrintPage, Sub(s, e) ImprimerPageTicket(e, ticket)

                If _param IsNot Nothing AndAlso _param.ApercuAvantImpression Then
                    Dim preview As New PrintPreviewDialog()
                    preview.Document = doc
                    preview.ShowDialog()
                Else
                    doc.Print()
                End If
            Catch ex As Exception
                MessageBox.Show("Erreur impression: " & ex.Message)
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
                    Dim qte As String = Convert.ToDecimal(row("Quantite")).ToString()
                    Dim prix As String = Convert.ToDecimal(row("PrixUnitaire")).ToString()
                    Dim total As String = Convert.ToDecimal(row("MontantLigne")).ToString()
                    Dim line As String = libelle & "  " & qte & " x " & prix
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
    End Class
End Namespace
