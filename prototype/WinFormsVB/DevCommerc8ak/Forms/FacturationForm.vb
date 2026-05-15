Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.IO
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FacturationForm
        Inherits Form

        Private ReadOnly txtNumeroFacture As TextBox
        Private ReadOnly txtClientId As TextBox
        Private ReadOnly txtClientNom As TextBox
        Private ReadOnly txtClientTel As TextBox

        Private ReadOnly txtRecherche As TextBox
        Private ReadOnly btnActualiser As Button
        Private ReadOnly gridProduits As DataGridView
        Private ReadOnly txtQuantite As TextBox
        Private ReadOnly cmbUnite As ComboBox
        Private ReadOnly txtPrixUnitaire As TextBox
        Private ReadOnly lblStock As Label
        Private ReadOnly lblEquivalent As Label
        Private ReadOnly lblTotalReel As Label

        Private ReadOnly gridPanier As DataGridView
        Private ReadOnly txtRemise As TextBox
        Private ReadOnly lblSousTotal As Label
        Private ReadOnly lblTotal As Label

        Private ReadOnly btnAjouter As Button
        Private ReadOnly btnRetirer As Button
        Private ReadOnly btnValider As Button
        Private ReadOnly btnImprimer As Button
        Private ReadOnly btnPdf As Button
        Private ReadOnly btnExcel As Button
        Private ReadOnly btnHistorique As Button
        Private ReadOnly btnAnnuler As Button
        Private ReadOnly btnDeconnexion As Button

        Private ReadOnly _panier As List(Of PanierLigne)
        Private ReadOnly _typeVenteService As TypeVenteService
        Private _remiseMax As Decimal
        Private _produitsTable As DataTable
        Private _produitsView As DataView
        Private _parametres As ParametreDTO
        Private _typesVenteCourants As List(Of TypeVenteDTO)

        Private Class PanierLigne
            Public Property ProduitId As Integer
            Public Property Libelle As String
            Public Property Unite As String
            Public Property PrixUnitaire As Decimal
            Public Property Quantite As Decimal
            Public Property QuantiteBase As Decimal
            Public Property QuantiteEquivalente As Decimal
            Public Property QuantiteReelle As Decimal
            Public Property Total As Decimal
        End Class

        Public Sub New()
            Me.BackColor = Color.White
            Me.Text = "Facturier"
            Me.Width = 1280
            Me.Height = 760
            Me.StartPosition = FormStartPosition.CenterScreen

            _panier = New List(Of PanierLigne)()
            _typeVenteService = New TypeVenteService()
            _typesVenteCourants = New List(Of TypeVenteDTO)()

            Dim lblNumeroFacture As New Label() With {.Text = "Numero facture", .Left = 560, .Top = 20, .AutoSize = True}
            txtNumeroFacture = New TextBox() With {.Left = 680, .Top = 16, .Width = 180, .Enabled = False}

            Dim grpClient As New GroupBox() With {.Text = "Client", .Left = 20, .Top = 20, .Width = 520, .Height = 140}
            Dim lblClientId As New Label() With {.Text = "Numero client", .Left = 14, .Top = 28, .AutoSize = True}
            txtClientId = New TextBox() With {.Left = 130, .Top = 24, .Width = 120, .Enabled = False}
            Dim lblClientNom As New Label() With {.Text = "Nom du client", .Left = 14, .Top = 62, .AutoSize = True}
            txtClientNom = New TextBox() With {.Left = 130, .Top = 58, .Width = 360}
            Dim lblClientTel As New Label() With {.Text = "Numero de telephone", .Left = 14, .Top = 96, .AutoSize = True}
            txtClientTel = New TextBox() With {.Left = 160, .Top = 92, .Width = 180}

            grpClient.Controls.Add(lblClientId)
            grpClient.Controls.Add(txtClientId)
            grpClient.Controls.Add(lblClientNom)
            grpClient.Controls.Add(txtClientNom)
            grpClient.Controls.Add(lblClientTel)
            grpClient.Controls.Add(txtClientTel)

            Dim grpProduits As New GroupBox() With {.Text = "Produits", .Left = 20, .Top = 170, .Width = 520, .Height = 520}
            Dim lblProduit As New Label() With {.Text = "Produit (recherche)", .Left = 14, .Top = 28, .AutoSize = True}
            txtRecherche = New TextBox() With {.Left = 150, .Top = 24, .Width = 250}
            btnActualiser = New Button() With {.Text = "Actualiser", .Left = 410, .Top = 22, .Width = 90}

            gridProduits = New DataGridView() With {.Left = 14, .Top = 58, .Width = 486, .Height = 260, .ReadOnly = True, .AutoGenerateColumns = True, .SelectionMode = DataGridViewSelectionMode.FullRowSelect}

            Dim lblQuantite As New Label() With {.Text = "Quantite", .Left = 14, .Top = 332, .AutoSize = True}
            txtQuantite = New TextBox() With {.Left = 80, .Top = 328, .Width = 70}
            Dim lblUnite As New Label() With {.Text = "Unite", .Left = 170, .Top = 332, .AutoSize = True}
            cmbUnite = New ComboBox() With {.Left = 220, .Top = 328, .Width = 110, .DropDownStyle = ComboBoxStyle.DropDownList}
            Dim lblPrix As New Label() With {.Text = "Prix unitaire", .Left = 350, .Top = 332, .AutoSize = True}
            txtPrixUnitaire = New TextBox() With {.Left = 430, .Top = 328, .Width = 70, .ReadOnly = True}

            lblStock = New Label() With {.Left = 14, .Top = 366, .AutoSize = True}
            lblEquivalent = New Label() With {.Left = 14, .Top = 388, .AutoSize = True}
            lblTotalReel = New Label() With {.Left = 14, .Top = 410, .AutoSize = True}

            btnAjouter = New Button() With {.Text = "Ajouter", .Left = 14, .Top = 440, .Width = 100}
            btnRetirer = New Button() With {.Text = "Retirer", .Left = 120, .Top = 440, .Width = 100}

            grpProduits.Controls.Add(lblProduit)
            grpProduits.Controls.Add(txtRecherche)
            grpProduits.Controls.Add(btnActualiser)
            grpProduits.Controls.Add(gridProduits)
            grpProduits.Controls.Add(lblQuantite)
            grpProduits.Controls.Add(txtQuantite)
            grpProduits.Controls.Add(lblUnite)
            grpProduits.Controls.Add(cmbUnite)
            grpProduits.Controls.Add(lblPrix)
            grpProduits.Controls.Add(txtPrixUnitaire)
            grpProduits.Controls.Add(lblStock)
            grpProduits.Controls.Add(lblEquivalent)
            grpProduits.Controls.Add(lblTotalReel)
            grpProduits.Controls.Add(btnAjouter)
            grpProduits.Controls.Add(btnRetirer)

            Dim grpPanier As New GroupBox() With {.Text = "Panier", .Left = 560, .Top = 60, .Width = 680, .Height = 430}
            gridPanier = New DataGridView() With {.Left = 14, .Top = 24, .Width = 650, .Height = 390, .ReadOnly = True, .AutoGenerateColumns = True, .SelectionMode = DataGridViewSelectionMode.FullRowSelect}
            grpPanier.Controls.Add(gridPanier)

            Dim lblRemise As New Label() With {.Text = "Remise %", .Left = 560, .Top = 510, .AutoSize = True}
            txtRemise = New TextBox() With {.Left = 640, .Top = 506, .Width = 70}
            lblSousTotal = New Label() With {.Left = 730, .Top = 510, .AutoSize = True}
            lblTotal = New Label() With {.Left = 930, .Top = 510, .AutoSize = True}

            btnValider = New Button() With {.Text = "Valider facture", .Left = 560, .Top = 550, .Width = 140}
            btnImprimer = New Button() With {.Text = "Imprimer A4", .Left = 710, .Top = 550, .Width = 120}
            btnPdf = New Button() With {.Text = "Exporter PDF", .Left = 840, .Top = 550, .Width = 120}
            btnExcel = New Button() With {.Text = "Exporter Excel", .Left = 970, .Top = 550, .Width = 120}
            btnHistorique = New Button() With {.Text = "Historique", .Left = 560, .Top = 590, .Width = 140}
            btnAnnuler = New Button() With {.Text = "Annuler", .Left = 710, .Top = 590, .Width = 120}
            btnDeconnexion = New Button() With {.Text = "Deconnexion", .Left = 840, .Top = 590, .Width = 120}

            Me.Controls.Add(lblNumeroFacture)
            Me.Controls.Add(txtNumeroFacture)
            Me.Controls.Add(grpClient)
            Me.Controls.Add(grpProduits)
            Me.Controls.Add(grpPanier)
            Me.Controls.Add(lblRemise)
            Me.Controls.Add(txtRemise)
            Me.Controls.Add(lblSousTotal)
            Me.Controls.Add(lblTotal)
            Me.Controls.Add(btnValider)
            Me.Controls.Add(btnImprimer)
            Me.Controls.Add(btnPdf)
            Me.Controls.Add(btnExcel)
            Me.Controls.Add(btnHistorique)
            Me.Controls.Add(btnAnnuler)
            Me.Controls.Add(btnDeconnexion)

            AddHandler txtRecherche.TextChanged, AddressOf FiltrerProduits
            AddHandler btnActualiser.Click, AddressOf RechargerProduits
            AddHandler gridProduits.SelectionChanged, AddressOf ChargerUnites
            AddHandler gridProduits.RowPrePaint, AddressOf ColorerStockCritique
            AddHandler cmbUnite.SelectedIndexChanged, AddressOf MiseAJourPrixUnitaire
            AddHandler txtQuantite.TextChanged, AddressOf MiseAJourIndicateursQuantite
            AddHandler btnAjouter.Click, AddressOf AjouterAuPanier
            AddHandler btnRetirer.Click, AddressOf RetirerDuPanier
            AddHandler btnValider.Click, AddressOf ValiderFacture
            AddHandler btnImprimer.Click, AddressOf ImprimerA4
            AddHandler btnPdf.Click, AddressOf ExporterPdf
            AddHandler btnExcel.Click, AddressOf ExporterExcel
            AddHandler btnHistorique.Click, AddressOf OuvrirHistorique
            AddHandler btnAnnuler.Click, AddressOf AnnulerFacture
            AddHandler btnDeconnexion.Click, AddressOf Deconnecter
            AddHandler txtClientTel.TextChanged, AddressOf RechercherClientParTelephone

            ThemeHelper.AppliquerTheme(Me)
            ChargerParametres()
            ChargerProduits()
            GenererNouveauNumeroFacture()
        End Sub

        Private Sub ChargerParametres()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim paramService As New ParametreService(New ParametreRepository(dal))
                _parametres = paramService.Charger()
                If _parametres Is Nothing Then Return
                _remiseMax = _parametres.RemiseMaxPourcent
            Catch
            End Try
        End Sub

        Private Sub ChargerProduits()
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New ProduitRepository(dal)
            _produitsTable = repo.ListerTable()
            _produitsView = New DataView(_produitsTable)
            gridProduits.DataSource = _produitsView
        End Sub

        Private Sub RechargerProduits(sender As Object, e As EventArgs)
            ChargerProduits()
            FiltrerProduits(Nothing, EventArgs.Empty)
        End Sub

        Private Sub FiltrerProduits(sender As Object, e As EventArgs)
            If _produitsView Is Nothing Then Return
            Dim q As String = txtRecherche.Text.Trim().Replace("'", "''")
            If q = "" Then
                _produitsView.RowFilter = ""
            Else
                _produitsView.RowFilter = "CodeBarres LIKE '%" & q & "%' OR Libelle LIKE '%" & q & "%'"
            End If
        End Sub

        Private Sub ChargerUnites(sender As Object, e As EventArgs)
            If gridProduits.CurrentRow Is Nothing Then Return
            Dim nbUnites As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells("ConversionUnite").Value)
            Dim prixAchat As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells("PrixAchat").Value)
            Dim prixGros As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells("PrixGros").Value)
            Dim prixDemi As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells("PrixDemi").Value)
            Dim prixDetail As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells("PrixDetail").Value)
            Dim prixQuart As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells("PrixQuart").Value)
            Dim prixDouzaine As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells("PrixDouzaine").Value)
            Dim prixSpecial As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells("PrixSpecial").Value)
            Dim venteDetail As Boolean = Convert.ToBoolean(gridProduits.CurrentRow.Cells("VenteDetail").Value)
            Dim venteDemi As Boolean = Convert.ToBoolean(gridProduits.CurrentRow.Cells("VenteDemi").Value)
            Dim venteDouzaine As Boolean = Convert.ToBoolean(gridProduits.CurrentRow.Cells("VenteDouzaine").Value)
            Dim venteGros As Boolean = Convert.ToBoolean(gridProduits.CurrentRow.Cells("VenteGros").Value)

            _typesVenteCourants = _typeVenteService.ConstruireTypesVente(nbUnites, prixAchat, prixGros, prixDemi, prixDetail, prixQuart, prixDouzaine, prixSpecial, venteGros, venteDemi, venteDetail, venteDouzaine)
            cmbUnite.DataSource = Nothing
            cmbUnite.DisplayMember = "NomAffichage"
            cmbUnite.ValueMember = "Nom"
            cmbUnite.DataSource = _typesVenteCourants
            If cmbUnite.Items.Count > 0 Then cmbUnite.SelectedIndex = 0

            MettreAJourAffichageStockProduit()
            MiseAJourPrixUnitaire(Nothing, EventArgs.Empty)
        End Sub

        Private Sub MiseAJourPrixUnitaire(sender As Object, e As EventArgs)
            If gridProduits.CurrentRow Is Nothing Then Return
            Dim typeChoisi As TypeVenteDTO = ObtenirTypeVenteSelectionne()
            Dim prix As Decimal = PrixSelonUnite()
            txtPrixUnitaire.Text = prix.ToString("N2")
            If typeChoisi Is Nothing Then
                lblEquivalent.Text = "Equivalent: 0 pièce / unité"
            Else
                lblEquivalent.Text = "Equivalent: " & typeChoisi.QuantiteEquivalent.ToString("N2") & " pièces / unité"
            End If
            MiseAJourIndicateursQuantite(Nothing, EventArgs.Empty)
        End Sub

        Private Sub ColorerStockCritique(sender As Object, e As DataGridViewRowPrePaintEventArgs)
            Dim row As DataGridViewRow = gridProduits.Rows(e.RowIndex)
            If row.Cells("QuantiteStock").Value Is Nothing OrElse row.Cells("SeuilCritique").Value Is Nothing Then Return
            Dim stock As Decimal = Convert.ToDecimal(row.Cells("QuantiteStock").Value)
            Dim seuil As Decimal = Convert.ToDecimal(row.Cells("SeuilCritique").Value)
            If stock <= seuil Then
                row.DefaultCellStyle.BackColor = Color.LightCoral
            End If
        End Sub

        Private Function PrixSelonUnite() As Decimal
            Dim typeChoisi As TypeVenteDTO = ObtenirTypeVenteSelectionne()
            If typeChoisi Is Nothing Then
                Return 0D
            End If
            Return typeChoisi.PrixVente
        End Function

        Private Function ObtenirTypeVenteSelectionne() As TypeVenteDTO
            Return TryCast(cmbUnite.SelectedItem, TypeVenteDTO)
        End Function

        Private Sub MiseAJourIndicateursQuantite(sender As Object, e As EventArgs)
            Dim qte As Decimal
            If Not Decimal.TryParse(txtQuantite.Text.Trim(), qte) OrElse qte <= 0D Then
                lblTotalReel.Text = "Total réel: 0 pièce"
                Return
            End If

            Dim typeChoisi As TypeVenteDTO = ObtenirTypeVenteSelectionne()
            If typeChoisi Is Nothing Then
                lblTotalReel.Text = "Total réel: 0 pièce"
                Return
            End If

            Dim quantiteReelle As Decimal = qte * typeChoisi.QuantiteEquivalent
            lblTotalReel.Text = "Total réel: " & quantiteReelle.ToString("N2") & " pièces"
        End Sub

        Private Sub MettreAJourAffichageStockProduit()
            If gridProduits.CurrentRow Is Nothing Then Return
            Dim produitId As Integer = Convert.ToInt32(gridProduits.CurrentRow.Cells("ProduitId").Value)
            Dim stock As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells("QuantiteStock").Value)
            Dim nbUnites As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells("ConversionUnite").Value)
            Dim uniteBase As String = Convert.ToString(gridProduits.CurrentRow.Cells("UnitePrincipale").Value)
            Dim uniteSecondaire As String = Convert.ToString(gridProduits.CurrentRow.Cells("UniteSecondaire").Value)
            Dim reserve As Decimal = 0D
            For Each ligne As PanierLigne In _panier
                If ligne.ProduitId = produitId Then
                    reserve += ligne.QuantiteBase
                End If
            Next
            Dim restant As Decimal = Math.Max(0D, stock - reserve)
            lblStock.Text = "Stock: " & _typeVenteService.FormaterStock(stock, nbUnites, If(uniteBase = "", "base", uniteBase), If(uniteSecondaire = "", "pièce", uniteSecondaire)) &
                " | Restant: " & _typeVenteService.FormaterStock(restant, nbUnites, If(uniteBase = "", "base", uniteBase), If(uniteSecondaire = "", "pièce", uniteSecondaire))
        End Function

        Private Sub AjouterAuPanier(sender As Object, e As EventArgs)
            If gridProduits.CurrentRow Is Nothing Then Return

            Dim qte As Decimal
            If Not Decimal.TryParse(txtQuantite.Text.Trim(), qte) OrElse qte <= 0D Then
                MessageBox.Show("Quantite invalide.")
                Return
            End If

            If cmbUnite.SelectedItem Is Nothing Then
                MessageBox.Show("Veuillez choisir l'unite.")
                Return
            End If

            Dim produitId As Integer = Convert.ToInt32(gridProduits.CurrentRow.Cells("ProduitId").Value)
            Dim libelle As String = Convert.ToString(gridProduits.CurrentRow.Cells("Libelle").Value)
            Dim typeChoisi As TypeVenteDTO = ObtenirTypeVenteSelectionne()
            If typeChoisi Is Nothing Then
                MessageBox.Show("Type de vente invalide.")
                Return
            End If
            Dim unite As String = typeChoisi.Nom
            Dim prix As Decimal = PrixSelonUnite()
            Dim quantiteEquivalent As Decimal = typeChoisi.QuantiteEquivalent
            Dim quantiteBase As Decimal = qte * quantiteEquivalent
            Dim stock As Decimal = Convert.ToDecimal(gridProduits.CurrentRow.Cells("QuantiteStock").Value)

            Dim deja As Decimal = 0D
            For Each l As PanierLigne In _panier
                If l.ProduitId = produitId Then
                    deja += l.QuantiteBase
                End If
            Next

            If deja + quantiteBase > stock Then
                MessageBox.Show("Stock insuffisant pour ce produit.")
                Return
            End If

            Dim ligne As PanierLigne = _panier.Find(Function(x) x.ProduitId = produitId AndAlso x.Unite = unite)
            If ligne Is Nothing Then
                ligne = New PanierLigne With {.ProduitId = produitId, .Libelle = libelle, .Unite = unite, .PrixUnitaire = prix, .Quantite = qte, .QuantiteBase = quantiteBase, .QuantiteEquivalente = quantiteEquivalent, .QuantiteReelle = quantiteBase, .Total = prix * qte}
                _panier.Add(ligne)
            Else
                ligne.Quantite += qte
                ligne.QuantiteBase += quantiteBase
                ligne.QuantiteReelle += quantiteBase
                ligne.QuantiteEquivalente = quantiteEquivalent
                ligne.Total = ligne.PrixUnitaire * ligne.Quantite
            End If

            RafraichirPanier()
        End Sub

        Private Sub RetirerDuPanier(sender As Object, e As EventArgs)
            If gridPanier.CurrentRow Is Nothing Then Return
            Dim produitId As Integer = Convert.ToInt32(gridPanier.CurrentRow.Cells("ProduitId").Value)
            Dim unite As String = Convert.ToString(gridPanier.CurrentRow.Cells("Unite").Value)
            _panier.RemoveAll(Function(x) x.ProduitId = produitId AndAlso x.Unite = unite)
            RafraichirPanier()
        End Sub

        Private Sub RafraichirPanier()
            gridPanier.DataSource = Nothing
            gridPanier.DataSource = _panier
            If gridPanier.Columns.Contains("ProduitId") Then gridPanier.Columns("ProduitId").Visible = False
            If gridPanier.Columns.Contains("QuantiteBase") Then gridPanier.Columns("QuantiteBase").Visible = False
            If gridPanier.Columns.Contains("Quantite") Then gridPanier.Columns("Quantite").HeaderText = "Quantité saisie"
            If gridPanier.Columns.Contains("QuantiteEquivalente") Then gridPanier.Columns("QuantiteEquivalente").HeaderText = "Quantité équivalente"
            If gridPanier.Columns.Contains("QuantiteReelle") Then gridPanier.Columns("QuantiteReelle").HeaderText = "Quantité réelle"

            Dim sousTotal As Decimal = 0D
            For Each l As PanierLigne In _panier
                sousTotal += l.Total
            Next

            Dim remisePourcent As Decimal
            If Not Decimal.TryParse(txtRemise.Text.Trim(), remisePourcent) Then
                remisePourcent = 0D
            End If
            If remisePourcent > _remiseMax Then
                MessageBox.Show("Remise superieure au maximum autorise.")
                remisePourcent = _remiseMax
                txtRemise.Text = _remiseMax.ToString()
            End If

            Dim remiseMontant As Decimal = sousTotal * remisePourcent / 100D
            Dim total As Decimal = sousTotal - remiseMontant

            lblSousTotal.Text = "Sous-total: " & sousTotal.ToString()
            lblTotal.Text = "Total: " & total.ToString()
            MettreAJourAffichageStockProduit()
        End Sub

        Private Sub RechercherClientParTelephone(sender As Object, e As EventArgs)
            Dim tel As String = txtClientTel.Text.Trim()
            If tel = "" Then
                txtClientId.Text = ""
                Return
            End If

            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim service As New ClientService(New ClientRepository(dal))
                Dim c As ClientDTO = service.ObtenirParTelephone(tel)
                If c IsNot Nothing Then
                    txtClientId.Text = c.ClientId.ToString()
                    txtClientNom.Text = c.NomClient
                Else
                    txtClientId.Text = ""
                End If
            Catch
            End Try
        End Sub

        Private Function VerifierStockAvantValidation() As Boolean
            For Each l As PanierLigne In _panier
                Dim stock As Decimal = ObtenirStockParProduit(l.ProduitId)
                If l.QuantiteBase > stock Then
                    MessageBox.Show("Stock insuffisant pour: " & l.Libelle)
                    Return False
                End If
            Next
            Return True
        End Function

        Private Function ObtenirStockParProduit(produitId As Integer) As Decimal
            If _produitsTable Is Nothing Then Return 0D
            For Each row As DataRow In _produitsTable.Rows
                If Convert.ToInt32(row("ProduitId")) = produitId Then
                    Return Convert.ToDecimal(row("QuantiteStock"))
                End If
            Next
            Return 0D
        End Function

        Private Sub ValiderFacture(sender As Object, e As EventArgs)
            Try
                If _panier.Count = 0 Then
                    MessageBox.Show("Panier vide.")
                    Return
                End If

                If Not VerifierStockAvantValidation() Then
                    Return
                End If

                Dim numeroFacture As String = txtNumeroFacture.Text.Trim()
                If numeroFacture = "" Then
                    MessageBox.Show("Numero de facture invalide.")
                    Return
                End If

                Me.UseWaitCursor = True
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim service As New FacturationService(dal)
                Dim clientService As New ClientService(New ClientRepository(dal))

                Dim sousTotal As Decimal = 0D
                For Each l As PanierLigne In _panier
                    sousTotal += l.Total
                Next

                Dim remisePourcent As Decimal
                Decimal.TryParse(txtRemise.Text.Trim(), remisePourcent)
                Dim remiseMontant As Decimal = sousTotal * remisePourcent / 100D
                Dim total As Decimal = sousTotal - remiseMontant

                Dim clientId As Integer? = Nothing
                Dim tel As String = txtClientTel.Text.Trim()
                Dim nom As String = txtClientNom.Text.Trim()

                If tel <> "" Then
                    Dim c As ClientDTO = clientService.ObtenirParTelephone(tel)
                    If c IsNot Nothing Then
                        clientId = c.ClientId
                    Else
                        If nom = "" Then
                            MessageBox.Show("Veuillez saisir le nom du client pour ce numero.")
                            Return
                        End If
                        Dim nouveau As New Client With {
                            .NomClient = nom,
                            .Telephone = tel,
                            .Email = "",
                            .Adresse = "",
                            .LimiteCredit = 0D,
                            .EstActif = True
                        }
                        clientId = clientService.Ajouter(nouveau)
                    End If
                ElseIf nom <> "" Then
                    Dim nouveau As New Client With {
                        .NomClient = nom,
                        .Telephone = "",
                        .Email = "",
                        .Adresse = "",
                        .LimiteCredit = 0D,
                        .EstActif = True
                    }
                    clientId = clientService.Ajouter(nouveau)
                End If

                Dim factureId As Integer = service.CreerFacture(numeroFacture, clientId, sousTotal, remiseMontant, 0D, total, SessionUtilisateur.UtilisateurId)
                For Each l As PanierLigne In _panier
                    service.AjouterLigne(factureId, l.ProduitId, l.QuantiteBase, l.PrixUnitaire, 0D, l.Quantite)
                Next

                MessageBox.Show("Facture en attente: " & numeroFacture)
                _panier.Clear()
                RafraichirPanier()
                ChargerProduits()
                GenererNouveauNumeroFacture()
            Catch ex As Exception
                MessageBox.Show("Erreur validation facture: " & ex.Message)
            Finally
                Me.UseWaitCursor = False
            End Try
        End Sub

        Private Sub GenererNouveauNumeroFacture()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim repo As New FactureVenteRepository(dal)
                txtNumeroFacture.Text = repo.GenererNumeroFacture()
            Catch
                txtNumeroFacture.Text = ""
            End Try
        End Sub

        Private Sub ImprimerA4(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                _parametres = (New ParametreService(New ParametreRepository(dal))).Charger()

                Dim doc As New Printing.PrintDocument()
                If _parametres IsNot Nothing AndAlso _parametres.ImprimanteA4 <> "" Then
                    doc.PrinterSettings.PrinterName = _parametres.ImprimanteA4
                End If
                doc.DefaultPageSettings.Color = If(_parametres IsNot Nothing, _parametres.ImpressionCouleur, True)
                AddHandler doc.PrintPage, AddressOf ImprimerPage

                If _parametres IsNot Nothing AndAlso _parametres.ApercuAvantImpression Then
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

        Private Sub ExporterPdf(sender As Object, e As EventArgs)
            Try
                Dim sfd As New SaveFileDialog() With {.Filter = "PDF (*.pdf)|*.pdf"}
                If sfd.ShowDialog() <> DialogResult.OK Then Return
                Dim lignes As List(Of String) = ConstruireLignesExport()
                PdfHelper.GenererPdfSimple(sfd.FileName, "FACTURE", lignes)
                MessageBox.Show("PDF genere.")
            Catch ex As Exception
                MessageBox.Show("Erreur PDF: " & ex.Message)
            End Try
        End Sub

        Private Sub ExporterExcel(sender As Object, e As EventArgs)
            Try
                Dim sfd As New SaveFileDialog() With {.Filter = "Excel CSV (*.csv)|*.csv"}
                If sfd.ShowDialog() <> DialogResult.OK Then Return
                Dim lignes As List(Of String) = ConstruireLignesExportCsv()
                File.WriteAllLines(sfd.FileName, lignes)
                MessageBox.Show("Export CSV genere.")
            Catch ex As Exception
                MessageBox.Show("Erreur export CSV: " & ex.Message)
            End Try
        End Sub

        Private Function ConstruireLignesExport() As List(Of String)
            Dim lignes As New List(Of String)()
            Dim nomMag As String = If(_parametres IsNot Nothing, _parametres.NomMagasin, "")
            Dim adr As String = If(_parametres IsNot Nothing, _parametres.AdresseMagasin, "")
            Dim tel As String = If(_parametres IsNot Nothing, _parametres.TelephoneMagasin, "")

            lignes.Add(nomMag)
            lignes.Add(adr)
            lignes.Add(tel)
            lignes.Add("Facture: " & txtNumeroFacture.Text.Trim())
            lignes.Add("Date: " & Date.Now.ToString("dd/MM/yyyy HH:mm"))
            lignes.Add("Client: " & txtClientNom.Text.Trim())
            lignes.Add("Telephone: " & txtClientTel.Text.Trim())
            lignes.Add(" ")

            For Each l As PanierLigne In _panier
                lignes.Add(l.Libelle & " " & l.Unite & " x" & l.Quantite.ToString() & " = " & l.Total.ToString())
            Next

            Dim sousTotal As Decimal = 0D
            For Each l As PanierLigne In _panier
                sousTotal += l.Total
            Next
            Dim remisePourcent As Decimal
            Decimal.TryParse(txtRemise.Text.Trim(), remisePourcent)
            Dim remiseMontant As Decimal = sousTotal * remisePourcent / 100D
            Dim total As Decimal = sousTotal - remiseMontant

            lignes.Add(" ")
            lignes.Add("Sous-total: " & sousTotal.ToString())
            lignes.Add("Remise: " & remiseMontant.ToString())
            lignes.Add("Total: " & total.ToString())

            Return lignes
        End Function

        Private Function ConstruireLignesExportCsv() As List(Of String)
            Dim lignes As New List(Of String)()
            lignes.Add("Type;Valeur")
            lignes.Add("Facture;" & txtNumeroFacture.Text.Trim())
            lignes.Add("Date;" & Date.Now.ToString("dd/MM/yyyy HH:mm"))
            lignes.Add("Client;" & txtClientNom.Text.Trim())
            lignes.Add("Telephone;" & txtClientTel.Text.Trim())
            lignes.Add(" ")
            lignes.Add("Libelle;Unite;Quantite;PrixUnitaire;Total")

            For Each l As PanierLigne In _panier
                lignes.Add(l.Libelle & ";" & l.Unite & ";" & l.Quantite.ToString() & ";" & l.PrixUnitaire.ToString() & ";" & l.Total.ToString())
            Next

            Dim sousTotal As Decimal = 0D
            For Each l As PanierLigne In _panier
                sousTotal += l.Total
            Next
            Dim remisePourcent As Decimal
            Decimal.TryParse(txtRemise.Text.Trim(), remisePourcent)
            Dim remiseMontant As Decimal = sousTotal * remisePourcent / 100D
            Dim total As Decimal = sousTotal - remiseMontant

            lignes.Add("Sous-total;" & sousTotal.ToString())
            lignes.Add("Remise;" & remiseMontant.ToString())
            lignes.Add("Total;" & total.ToString())

            Return lignes
        End Function

        Private Sub ImprimerPage(sender As Object, e As Printing.PrintPageEventArgs)
            Dim y As Integer = 20
            Dim x As Integer = 20

            If _parametres IsNot Nothing AndAlso _parametres.LogoPath <> "" AndAlso File.Exists(_parametres.LogoPath) Then
                Using img As Image = Image.FromFile(_parametres.LogoPath)
                    e.Graphics.DrawImage(img, x, y, 60, 60)
                End Using
                x += 70
            End If

            Dim nomMag As String = If(_parametres IsNot Nothing, _parametres.NomMagasin, "")
            Dim adr As String = If(_parametres IsNot Nothing, _parametres.AdresseMagasin, "")
            Dim tel As String = If(_parametres IsNot Nothing, _parametres.TelephoneMagasin, "")

            e.Graphics.DrawString(nomMag, New Font("Segoe UI", 14, FontStyle.Bold), Brushes.Black, x, y)
            y += 24
            e.Graphics.DrawString(adr, New Font("Segoe UI", 10), Brushes.Black, x, y)
            y += 18
            e.Graphics.DrawString(tel, New Font("Segoe UI", 10), Brushes.Black, x, y)
            y += 26

            e.Graphics.DrawString("Facture: " & txtNumeroFacture.Text.Trim(), New Font("Segoe UI", 10, FontStyle.Bold), Brushes.Black, 20, y)
            y += 18
            e.Graphics.DrawString("Date: " & Date.Now.ToString("dd/MM/yyyy HH:mm"), New Font("Segoe UI", 10), Brushes.Black, 20, y)
            y += 18
            e.Graphics.DrawString("Client: " & txtClientNom.Text.Trim(), New Font("Segoe UI", 10), Brushes.Black, 20, y)
            y += 18
            e.Graphics.DrawString("Telephone: " & txtClientTel.Text.Trim(), New Font("Segoe UI", 10), Brushes.Black, 20, y)
            y += 24

            e.Graphics.DrawString("DETAILS", New Font("Segoe UI", 11, FontStyle.Bold), Brushes.Black, 20, y)
            y += 20

            For Each l As PanierLigne In _panier
                Dim line As String = l.Libelle & " " & l.Unite & " x" & l.Quantite.ToString() & " = " & l.Total.ToString()
                e.Graphics.DrawString(line, New Font("Segoe UI", 10), Brushes.Black, 20, y)
                y += 18
            Next

            Dim sousTotal As Decimal = 0D
            For Each l As PanierLigne In _panier
                sousTotal += l.Total
            Next
            Dim remisePourcent As Decimal
            Decimal.TryParse(txtRemise.Text.Trim(), remisePourcent)
            Dim remiseMontant As Decimal = sousTotal * remisePourcent / 100D
            Dim total As Decimal = sousTotal - remiseMontant

            y += 10
            e.Graphics.DrawString("Sous-total: " & sousTotal.ToString(), New Font("Segoe UI", 10), Brushes.Black, 20, y)
            y += 18
            e.Graphics.DrawString("Remise: " & remiseMontant.ToString(), New Font("Segoe UI", 10), Brushes.Black, 20, y)
            y += 18
            e.Graphics.DrawString("Total: " & total.ToString(), New Font("Segoe UI", 11, FontStyle.Bold), Brushes.Black, 20, y)
        End Sub

        Private Sub OuvrirHistorique(sender As Object, e As EventArgs)
            Dim f As New FormulaireFactures()
            f.ShowDialog()
        End Sub

        Private Sub AnnulerFacture(sender As Object, e As EventArgs)
            _panier.Clear()
            txtClientId.Text = ""
            txtClientNom.Text = ""
            txtClientTel.Text = ""
            txtRemise.Text = ""
            RafraichirPanier()
            GenererNouveauNumeroFacture()
        End Sub

        Private Sub Deconnecter(sender As Object, e As EventArgs)
            Dim main = Me.FindForm()
            If main IsNot Nothing Then
                main.Close()
            End If
        End Sub
    End Class
End Namespace
