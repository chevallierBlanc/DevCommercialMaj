Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireComptagePhysiqueProduit
        Inherits Form

        Private ReadOnly ColorBg As Color = Color.FromArgb(244, 247, 252)
        Private ReadOnly ColorCard As Color = Color.White
        Private ReadOnly ColorPrimary As Color = Color.FromArgb(44, 62, 80)
        Private ReadOnly ColorSecondary As Color = Color.FromArgb(88, 101, 121)
        Private ReadOnly ColorAccent As Color = Color.FromArgb(59, 130, 246)
        Private ReadOnly ColorSuccess As Color = Color.FromArgb(34, 197, 94)
        Private ReadOnly ColorDanger As Color = Color.FromArgb(239, 68, 68)
        Private ReadOnly ColorWarning As Color = Color.FromArgb(249, 115, 22)
        Private ReadOnly ColorBorder As Color = Color.FromArgb(226, 232, 240)

        Private ReadOnly FontTitle As New Font("Segoe UI", 16, FontStyle.Bold)
        Private ReadOnly FontSection As New Font("Segoe UI", 11, FontStyle.Bold)
        Private ReadOnly FontLabel As New Font("Segoe UI", 9.5F)
        Private ReadOnly FontButton As New Font("Segoe UI", 9.5F, FontStyle.Bold)
        Private ReadOnly FontValue As New Font("Segoe UI", 13, FontStyle.Bold)
        Private ReadOnly FontTotal As New Font("Segoe UI", 18, FontStyle.Bold)

        Private ReadOnly _produitId As Integer
        Private ReadOnly _stockTheoriqueBase As Decimal
        Private ReadOnly _quantiteInitialeBase As Decimal?
        Private _produit As ProduitDTO

        Private lblProduit As Label
        Private lblCode As Label
        Private lblCategorie As Label
        Private lblMode As Label
        Private lblStockTheorique As Label
        Private lblPrincipale As Label
        Private lblSecondaire As Label
        Private lblMesureLibre As Label
        Private txtPrincipale As TextBox
        Private txtSecondaire As TextBox
        Private txtMesureLibre As TextBox
        Private lblTotal As Label
        Private lblEquivalent As Label
        Private lblEcart As Label
        Private lblResultat As Label
        Private btnAnnuler As Button
        Private btnValider As Button

        Public Property QuantitePhysiqueBase As Decimal
        Public Property RepresentationLisible As String = ""
        Public Property EcartBase As Decimal
        Public Property StatutComptage As String = "NON_COMPTE"

        Public Sub New(produitId As Integer, stockTheoriqueBase As Decimal, Optional quantiteInitialeBase As Decimal? = Nothing)
            _produitId = produitId
            _stockTheoriqueBase = stockTheoriqueBase
            _quantiteInitialeBase = quantiteInitialeBase

            Me.Text = "Comptage physique"
            Me.StartPosition = FormStartPosition.CenterParent
            Me.Size = New Size(680, 560)
            Me.MinimumSize = New Size(620, 520)
            Me.BackColor = ColorBg
            Me.Font = FontLabel
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False

            ConstruireInterface()
            AddHandler Me.Load, AddressOf FormulaireComptagePhysiqueProduit_Load
        End Sub

        Private Sub ConstruireInterface()
            Dim layout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 4,
                .Padding = New Padding(18)
            }
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 68))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 136))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 56))
            Me.Controls.Add(layout)

            Dim header As New Panel() With {.Dock = DockStyle.Fill, .BackColor = ColorPrimary, .Padding = New Padding(16), .Margin = New Padding(0, 0, 0, 10)}
            header.Controls.Add(New Label() With {
                .Text = "Comptage physique du produit",
                .Font = FontTitle,
                .ForeColor = Color.White,
                .Dock = DockStyle.Top,
                .Height = 30
            })
            header.Controls.Add(New Label() With {
                .Text = "Saisissez les quantités réellement comptées. Le total est converti automatiquement dans l'unité physique du stock.",
                .Font = FontLabel,
                .ForeColor = Color.FromArgb(203, 213, 225),
                .Dock = DockStyle.Bottom,
                .Height = 22
            })
            layout.Controls.Add(header, 0, 0)

            Dim info As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .ColumnCount = 2, .RowCount = 5, .Padding = New Padding(14), .Margin = New Padding(0, 0, 0, 10)}
            info.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150))
            info.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            For i As Integer = 1 To 5
                info.RowStyles.Add(New RowStyle(SizeType.Percent, 20))
            Next
            lblProduit = AjouterLigneInfo(info, 0, "Produit")
            lblCode = AjouterLigneInfo(info, 1, "Code")
            lblCategorie = AjouterLigneInfo(info, 2, "Catégorie")
            lblMode = AjouterLigneInfo(info, 3, "Mode de gestion")
            lblStockTheorique = AjouterLigneInfo(info, 4, "Stock théorique")
            layout.Controls.Add(info, 0, 1)

            Dim body As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .BackColor = ColorCard, .ColumnCount = 1, .RowCount = 2, .Padding = New Padding(14), .Margin = New Padding(0, 0, 0, 10)}
            body.RowStyles.Add(New RowStyle(SizeType.Absolute, 150))
            body.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim saisie As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 2, .RowCount = 4}
            saisie.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 210))
            saisie.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            For i As Integer = 1 To 4
                saisie.RowStyles.Add(New RowStyle(SizeType.Percent, 25))
            Next
            lblPrincipale = AjouterLigneSaisie(saisie, 0, "Quantité principale")
            txtPrincipale = CreerTextBox()
            saisie.Controls.Add(txtPrincipale, 1, 0)
            lblSecondaire = AjouterLigneSaisie(saisie, 1, "Quantité secondaire")
            txtSecondaire = CreerTextBox()
            saisie.Controls.Add(txtSecondaire, 1, 1)
            lblMesureLibre = AjouterLigneSaisie(saisie, 2, "Reste libre")
            txtMesureLibre = CreerTextBox()
            saisie.Controls.Add(txtMesureLibre, 1, 2)

            AddHandler txtPrincipale.TextChanged, AddressOf Quantites_TextChanged
            AddHandler txtSecondaire.TextChanged, AddressOf Quantites_TextChanged
            AddHandler txtMesureLibre.TextChanged, AddressOf Quantites_TextChanged
            body.Controls.Add(saisie, 0, 0)

            Dim resume As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .BackColor = Color.FromArgb(248, 250, 252), .Padding = New Padding(14), .ColumnCount = 1, .RowCount = 4}
            resume.RowStyles.Add(New RowStyle(SizeType.Absolute, 64))
            resume.RowStyles.Add(New RowStyle(SizeType.Absolute, 28))
            resume.RowStyles.Add(New RowStyle(SizeType.Absolute, 34))
            resume.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            lblTotal = New Label() With {.Text = "TOTAL COMPTÉ" & Environment.NewLine & "-", .Font = FontTotal, .ForeColor = ColorPrimary, .AutoSize = False, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}
            lblEquivalent = New Label() With {.Text = "Répartition : -", .Font = FontLabel, .ForeColor = ColorSecondary, .AutoSize = False, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}
            lblEcart = New Label() With {.Text = "Écart : -", .Font = FontValue, .ForeColor = ColorPrimary, .AutoSize = False, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}
            lblResultat = New Label() With {.Text = "Résultat : -", .Font = FontSection, .ForeColor = ColorSecondary, .AutoSize = False, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}
            resume.Controls.Add(lblTotal, 0, 0)
            resume.Controls.Add(lblEquivalent, 0, 1)
            resume.Controls.Add(lblEcart, 0, 2)
            resume.Controls.Add(lblResultat, 0, 3)
            body.Controls.Add(resume, 0, 1)
            layout.Controls.Add(body, 0, 2)

            Dim actions As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft, .WrapContents = False}
            btnValider = CreerBouton("Valider le comptage", ColorSuccess, 170)
            btnAnnuler = CreerBouton("Annuler", ColorSecondary, 110)
            actions.Controls.Add(btnValider)
            actions.Controls.Add(btnAnnuler)
            layout.Controls.Add(actions, 0, 3)

            AddHandler btnValider.Click, AddressOf ValiderComptage
            AddHandler btnAnnuler.Click, Sub() Me.DialogResult = DialogResult.Cancel
            Me.AcceptButton = btnValider
            Me.CancelButton = btnAnnuler
        End Sub

        Private Function AjouterLigneInfo(table As TableLayoutPanel, row As Integer, titre As String) As Label
            table.Controls.Add(New Label() With {.Text = titre & " :", .Dock = DockStyle.Fill, .Font = FontLabel, .ForeColor = ColorSecondary, .TextAlign = ContentAlignment.MiddleLeft}, 0, row)
            Dim valeur As New Label() With {.Text = "-", .Dock = DockStyle.Fill, .Font = FontSection, .ForeColor = ColorPrimary, .TextAlign = ContentAlignment.MiddleLeft}
            table.Controls.Add(valeur, 1, row)
            Return valeur
        End Function

        Private Function AjouterLigneSaisie(table As TableLayoutPanel, row As Integer, titre As String) As Label
            Dim label As New Label() With {.Text = titre & " :", .Dock = DockStyle.Fill, .Font = FontLabel, .ForeColor = ColorSecondary, .TextAlign = ContentAlignment.MiddleLeft}
            table.Controls.Add(label, 0, row)
            Return label
        End Function

        Private Function CreerTextBox() As TextBox
            Return New TextBox() With {.Dock = DockStyle.Fill, .Font = FontLabel, .Margin = New Padding(0, 6, 0, 6)}
        End Function

        Private Function CreerBouton(texte As String, couleur As Color, largeur As Integer) As Button
            Dim btn As New Button() With {
                .Text = texte,
                .Height = 38,
                .Width = largeur,
                .BackColor = couleur,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = FontButton,
                .Cursor = Cursors.Hand,
                .Margin = New Padding(8, 8, 0, 0)
            }
            btn.FlatAppearance.BorderSize = 0
            Return btn
        End Function

        Private Sub FormulaireComptagePhysiqueProduit_Load(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim service As New ProduitService(New ProduitRepository(New DAL(cs)))
                _produit = service.ObtenirParId(_produitId)
                If _produit Is Nothing Then
                    MessageBox.Show("Produit introuvable.", "Comptage", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Me.DialogResult = DialogResult.Cancel
                    Return
                End If

                ChargerInfosProduit()
                PreRemplirDepuisQuantite()
                RecalculerComptage()
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireComptagePhysiqueProduit", "Chargement", "Erreur chargement produit comptage.", ex)
                MessageBox.Show("Impossible de charger le produit à compter.", "Comptage", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Me.DialogResult = DialogResult.Cancel
            End Try
        End Sub

        Private Sub ChargerInfosProduit()
            Dim mode As String = StockUnitConversionService.NormaliserTypeGestionStock(_produit.TypeGestionStock)
            Dim uniteReference As String = ObtenirUniteReference()
            lblProduit.Text = If(_produit.Libelle, "")
            lblCode.Text = If(String.IsNullOrWhiteSpace(_produit.CodeBarres), "-", _produit.CodeBarres)
            lblCategorie.Text = If(String.IsNullOrWhiteSpace(_produit.NomCategorie), "-", _produit.NomCategorie)
            lblMode.Text = mode
            lblStockTheorique.Text = FormaterResumeStock(_stockTheoriqueBase, uniteReference)

            lblPrincipale.Text = QuantiteLabel(UnitePrincipale(), "Quantité principale")
            If StockUnitConversionService.EstGestionMesuree(mode) Then
                Dim contenuSecondaire As Decimal = ObtenirContenuSecondaire()
                Dim hasSecondaire As Boolean = Not String.IsNullOrWhiteSpace(_produit.UniteSecondaire) AndAlso contenuSecondaire > 0D
                lblSecondaire.Visible = hasSecondaire
                txtSecondaire.Visible = hasSecondaire
                lblSecondaire.Text = QuantiteLabel(_produit.UniteSecondaire, "Quantité secondaire")
                lblMesureLibre.Visible = True
                txtMesureLibre.Visible = True
                lblMesureLibre.Text = "Reste libre (" & uniteReference & ") :"
            Else
                lblSecondaire.Visible = True
                txtSecondaire.Visible = True
                lblSecondaire.Text = QuantiteLabel(UniteSecondaire(), "Quantité secondaire")
                lblMesureLibre.Visible = False
                txtMesureLibre.Visible = False
            End If
        End Sub

        Private Function QuantiteLabel(unite As String, fallback As String) As String
            Dim texte As String = If(String.IsNullOrWhiteSpace(unite), fallback, unite.Trim() & " compté(s)")
            Return texte & " :"
        End Function

        Private Sub PreRemplirDepuisQuantite()
            If Not _quantiteInitialeBase.HasValue Then Return
            Dim quantiteBase As Decimal = Math.Max(0D, _quantiteInitialeBase.Value)
            If quantiteBase <= 0D Then Return

            If EstProduitMesure() Then
                Dim contenuPrincipal As Decimal = ObtenirContenuPrincipal()
                Dim contenuSecondaire As Decimal = ObtenirContenuSecondaire()
                Dim principale As Decimal = If(contenuPrincipal > 0D, Decimal.Floor(quantiteBase / contenuPrincipal), 0D)
                Dim reste As Decimal = quantiteBase - (principale * contenuPrincipal)
                Dim secondaire As Decimal = 0D
                If contenuSecondaire > 0D AndAlso Not String.IsNullOrWhiteSpace(_produit.UniteSecondaire) Then
                    secondaire = Decimal.Floor(reste / contenuSecondaire)
                    reste -= secondaire * contenuSecondaire
                End If
                txtPrincipale.Text = FormatageGlobal.FormatQuantitePhysique(principale)
                txtSecondaire.Text = If(secondaire > 0D, FormatageGlobal.FormatQuantitePhysique(secondaire), "")
                txtMesureLibre.Text = If(reste > 0D, FormatageGlobal.FormatQuantitePhysique(reste), "")
            Else
                Dim conversion As Decimal = If(_produit.ConversionUnite > 0D, _produit.ConversionUnite, 1D)
                Dim principale As Decimal = Decimal.Floor(quantiteBase / conversion)
                Dim secondaire As Decimal = quantiteBase - (principale * conversion)
                txtPrincipale.Text = FormatageGlobal.FormatQuantitePhysique(principale)
                txtSecondaire.Text = If(secondaire > 0D, FormatageGlobal.FormatQuantitePhysique(secondaire), "")
            End If
        End Sub

        Private Sub Quantites_TextChanged(sender As Object, e As EventArgs)
            RecalculerComptage()
        End Sub

        Private Sub RecalculerComptage()
            If _produit Is Nothing Then Return

            Dim principale As Decimal
            Dim secondaire As Decimal
            Dim libre As Decimal
            If Not LireDecimalSaisie(txtPrincipale.Text, principale) OrElse
               Not LireDecimalSaisie(txtSecondaire.Text, secondaire) OrElse
               Not LireDecimalSaisie(txtMesureLibre.Text, libre) Then
                AfficherErreurSaisie()
                Return
            End If

            Dim total As Decimal
            If EstProduitMesure() Then
                total = (principale * ObtenirContenuPrincipal()) + libre
                If txtSecondaire.Visible Then
                    total += secondaire * ObtenirContenuSecondaire()
                End If
            Else
                Dim conversion As Decimal = If(_produit.ConversionUnite > 0D, _produit.ConversionUnite, 1D)
                total = (principale * conversion) + secondaire
            End If

            QuantitePhysiqueBase = total
            EcartBase = QuantitePhysiqueBase - _stockTheoriqueBase
            StatutComptage = If(EcartBase = 0D, "CONFORME", If(EcartBase < 0D, "MANQUE", "SURPLUS"))
            RepresentationLisible = FormaterStock(QuantitePhysiqueBase)

            Dim uniteReference As String = ObtenirUniteReference()
            lblTotal.Text = "TOTAL COMPTÉ" & Environment.NewLine & FormatageGlobal.FormatQuantitePhysique(QuantitePhysiqueBase) & " " & uniteReference
            lblEquivalent.Text = "Répartition : " & ObtenirRepartitionSeule(QuantitePhysiqueBase, uniteReference)
            lblEcart.Text = "Écart : " & If(EcartBase > 0D, "+", "") & FormatageGlobal.FormatQuantitePhysique(EcartBase) & " " & uniteReference
            lblResultat.Text = "Résultat : " & StatutComptage
            lblResultat.ForeColor = If(StatutComptage = "CONFORME", ColorSuccess, If(StatutComptage = "MANQUE", ColorDanger, ColorWarning))
            btnValider.Enabled = True
        End Sub

        Private Sub AfficherErreurSaisie()
            lblTotal.Text = "TOTAL COMPTÉ" & Environment.NewLine & "Saisie invalide"
            lblEquivalent.Text = "Répartition : -"
            lblEcart.Text = "Écart : -"
            lblResultat.Text = "Résultat : -"
            lblResultat.ForeColor = ColorDanger
            btnValider.Enabled = False
        End Sub

        Private Function LireDecimalSaisie(texte As String, ByRef valeur As Decimal) As Boolean
            valeur = 0D
            If String.IsNullOrWhiteSpace(texte) Then Return True
            Dim normalise As String = texte.Trim().Replace(" ", "").Replace(",", ".")
            If Not Decimal.TryParse(normalise, NumberStyles.Number, CultureInfo.InvariantCulture, valeur) Then
                Return False
            End If
            Return valeur >= 0D
        End Function

        Private Sub ValiderComptage(sender As Object, e As EventArgs)
            RecalculerComptage()
            If Not btnValider.Enabled Then Return
            Me.DialogResult = DialogResult.OK
        End Sub

        Private Function EstProduitMesure() As Boolean
            Return StockUnitConversionService.EstGestionMesuree(_produit.TypeGestionStock)
        End Function

        Private Function ObtenirUniteReference() As String
            If EstProduitMesure() Then
                Return If(String.IsNullOrWhiteSpace(_produit.UniteMesureStock), "mesure", _produit.UniteMesureStock.Trim())
            End If
            Return UniteSecondaire()
        End Function

        Private Function UnitePrincipale() As String
            Return If(String.IsNullOrWhiteSpace(_produit.UnitePrincipale), "unité principale", _produit.UnitePrincipale.Trim())
        End Function

        Private Function UniteSecondaire() As String
            Return If(String.IsNullOrWhiteSpace(_produit.UniteSecondaire), "pièce", _produit.UniteSecondaire.Trim())
        End Function

        Private Function ObtenirContenuPrincipal() As Decimal
            If _produit.ContenuUnitePrincipale > 0D Then Return _produit.ContenuUnitePrincipale
            If _produit.ConversionUnite > 0D Then Return _produit.ConversionUnite
            Return 1D
        End Function

        Private Function ObtenirContenuSecondaire() As Decimal
            If _produit.ContenuUniteSecondaire.HasValue AndAlso _produit.ContenuUniteSecondaire.Value > 0D Then
                Return _produit.ContenuUniteSecondaire.Value
            End If
            Return 0D
        End Function

        Private Function FormaterStock(quantiteBase As Decimal) As String
            Return FormatageGlobal.FormatStockSelonGestion(
                quantiteBase,
                _produit.ConversionUnite,
                _produit.UnitePrincipale,
                _produit.UniteSecondaire,
                _produit.TypeGestionStock,
                _produit.UniteMesureStock,
                _produit.ContenuUnitePrincipale,
                If(_produit.ContenuUniteSecondaire.HasValue, _produit.ContenuUniteSecondaire.Value, 0D))
        End Function

        Private Function FormaterResumeStock(quantiteBase As Decimal, uniteReference As String) As String
            Dim quantite As String = FormatageGlobal.FormatQuantitePhysique(quantiteBase) & " " & uniteReference
            Return quantite & " | Répartition : " & ObtenirRepartitionSeule(quantiteBase, uniteReference)
        End Function

        Private Function ObtenirRepartitionSeule(quantiteBase As Decimal, uniteReference As String) As String
            Dim quantite As String = FormatageGlobal.FormatQuantitePhysique(quantiteBase) & " " & uniteReference
            Dim stockFormate As String = FormaterStock(quantiteBase)
            Dim prefix As String = quantite & " = "
            If stockFormate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) Then
                Return stockFormate.Substring(prefix.Length)
            End If
            Return stockFormate
        End Function
    End Class
End Namespace
