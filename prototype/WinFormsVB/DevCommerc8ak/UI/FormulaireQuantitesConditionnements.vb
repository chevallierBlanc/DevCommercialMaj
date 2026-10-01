Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireQuantitesConditionnements
        Inherits Form

        Private ReadOnly _produitId As Integer
        Private ReadOnly _titre As String
        Private ReadOnly _quantiteInitialeBase As Decimal?
        Private ReadOnly _stockReferenceBase As Decimal?
        Private ReadOnly _filtrerAchetable As Boolean
        Private ReadOnly _filtrerVendable As Boolean
        Private ReadOnly _service As New ProduitConditionnementService()
        Private ReadOnly _textBoxes As New Dictionary(Of Integer, TextBox)()
        Private ReadOnly _labelsSousTotaux As New Dictionary(Of Integer, Label)()
        Private ReadOnly _conditionnements As List(Of ProduitConditionnementDTO)

        Private ReadOnly lblResume As Label
        Private ReadOnly lblTotal As Label
        Private ReadOnly lblEcart As Label
        Private ReadOnly lblRepartitionEcart As Label
        Private ReadOnly lblResultat As Label
        Private ReadOnly pnlSaisie As TableLayoutPanel

        Public Property QuantiteBase As Decimal
        Public Property RepresentationLisible As String = String.Empty

        Public Sub New(produitId As Integer,
                       titre As String,
                       Optional quantiteInitialeBase As Decimal? = Nothing,
                       Optional stockReferenceBase As Decimal? = Nothing,
                       Optional filtrerAchetable As Boolean = False,
                       Optional filtrerVendable As Boolean = False)
            _produitId = produitId
            _titre = If(String.IsNullOrWhiteSpace(titre), "Quantités par conditionnement", titre.Trim())
            _quantiteInitialeBase = quantiteInitialeBase
            _stockReferenceBase = stockReferenceBase
            _filtrerAchetable = filtrerAchetable
            _filtrerVendable = filtrerVendable
            _conditionnements = _service.ListerParProduit(produitId, True).
                Where(Function(c) c.EstActif AndAlso c.FacteurVersBase > 0D).
                Where(Function(c) Not _filtrerAchetable OrElse c.EstAchetable).
                Where(Function(c) Not _filtrerVendable OrElse c.EstVendable).
                OrderByDescending(Function(c) c.FacteurVersBase).
                ToList()

            Text = _titre
            StartPosition = FormStartPosition.CenterParent
            Size = New Size(760, 600)
            MinimumSize = New Size(680, 520)
            BackColor = Color.FromArgb(245, 247, 250)
            Font = New Font("Segoe UI", 9.5F)

            Dim root As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 4, .Padding = New Padding(16)}
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 58))
            root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 158))
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 54))
            Controls.Add(root)

            root.Controls.Add(New Label() With {
                .Text = _titre,
                .Dock = DockStyle.Fill,
                .Font = New Font("Segoe UI", 15.0F, FontStyle.Bold),
                .ForeColor = Color.FromArgb(31, 41, 55),
                .TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0)

            pnlSaisie = New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 4, .AutoScroll = True, .BackColor = Color.White, .Padding = New Padding(14)}
            pnlSaisie.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 30))
            pnlSaisie.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 20))
            pnlSaisie.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
            pnlSaisie.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25))
            root.Controls.Add(pnlSaisie, 0, 1)

            Dim pnlResume As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 5, .BackColor = Color.White, .Padding = New Padding(14)}
            lblTotal = New Label() With {.Dock = DockStyle.Fill, .Font = New Font("Segoe UI", 12.0F, FontStyle.Bold), .ForeColor = Color.FromArgb(14, 116, 144)}
            lblResume = New Label() With {.Dock = DockStyle.Fill, .ForeColor = Color.FromArgb(75, 85, 99)}
            lblEcart = New Label() With {.Dock = DockStyle.Fill, .ForeColor = Color.FromArgb(75, 85, 99)}
            lblRepartitionEcart = New Label() With {.Dock = DockStyle.Fill, .ForeColor = Color.FromArgb(75, 85, 99)}
            lblResultat = New Label() With {.Dock = DockStyle.Fill, .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold), .ForeColor = Color.FromArgb(75, 85, 99)}
            pnlResume.Controls.Add(lblTotal, 0, 0)
            pnlResume.Controls.Add(lblResume, 0, 1)
            pnlResume.Controls.Add(lblEcart, 0, 2)
            pnlResume.Controls.Add(lblRepartitionEcart, 0, 3)
            pnlResume.Controls.Add(lblResultat, 0, 4)
            root.Controls.Add(pnlResume, 0, 2)

            Dim pnlActions As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft, .WrapContents = False}
            Dim btnValider As New Button() With {.Text = "Valider", .Width = 120, .Height = 36, .BackColor = Color.FromArgb(34, 197, 94), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            Dim btnAnnuler As New Button() With {.Text = "Annuler", .Width = 110, .Height = 36, .BackColor = Color.FromArgb(107, 114, 128), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            btnValider.FlatAppearance.BorderSize = 0
            btnAnnuler.FlatAppearance.BorderSize = 0
            AddHandler btnValider.Click, AddressOf Valider
            AddHandler btnAnnuler.Click, Sub() DialogResult = DialogResult.Cancel
            pnlActions.Controls.Add(btnValider)
            pnlActions.Controls.Add(btnAnnuler)
            root.Controls.Add(pnlActions, 0, 3)

            ConstruireSaisie()
            If _quantiteInitialeBase.HasValue Then
                PreRemplirDepuisBase(Math.Max(0D, _quantiteInitialeBase.Value))
            End If
            Recalculer()
        End Sub

        Private Sub ConstruireSaisie()
            If _conditionnements.Count = 0 Then
                Throw New InvalidOperationException("Aucun conditionnement dynamique actif n'est configuré pour ce produit.")
            End If

            pnlSaisie.RowCount = _conditionnements.Count + 1
            pnlSaisie.Controls.Add(New Label() With {.Text = "Conditionnement", .Font = New Font(Font, FontStyle.Bold), .Dock = DockStyle.Fill}, 0, 0)
            pnlSaisie.Controls.Add(New Label() With {.Text = "Quantité comptée/saisie", .Font = New Font(Font, FontStyle.Bold), .Dock = DockStyle.Fill}, 1, 0)
            pnlSaisie.Controls.Add(New Label() With {.Text = "Équiv. par unité", .Font = New Font(Font, FontStyle.Bold), .Dock = DockStyle.Fill}, 2, 0)
            pnlSaisie.Controls.Add(New Label() With {.Text = "Sous-total base", .Font = New Font(Font, FontStyle.Bold), .Dock = DockStyle.Fill}, 3, 0)

            Dim rowIndex As Integer = 1
            For Each conditionnement As ProduitConditionnementDTO In _conditionnements
                Dim txt As New TextBox() With {.Dock = DockStyle.Fill, .Tag = conditionnement}
                Dim lblSousTotal As New Label() With {.Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleRight, .ForeColor = Color.FromArgb(55, 65, 81)}
                AddHandler txt.TextChanged, AddressOf Quantite_TextChanged
                _textBoxes(conditionnement.ProduitConditionnementId) = txt
                _labelsSousTotaux(conditionnement.ProduitConditionnementId) = lblSousTotal

                pnlSaisie.RowStyles.Add(New RowStyle(SizeType.Absolute, 34))
                pnlSaisie.Controls.Add(New Label() With {.Text = LibelleConditionnement(conditionnement) & " :", .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}, 0, rowIndex)
                pnlSaisie.Controls.Add(txt, 1, rowIndex)
                pnlSaisie.Controls.Add(New Label() With {.Text = "1 " & LibelleConditionnement(conditionnement) & " = " & FormaterDecimal(conditionnement.FacteurVersBase) & " " & LibelleUniteBase(), .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleLeft}, 2, rowIndex)
                pnlSaisie.Controls.Add(lblSousTotal, 3, rowIndex)
                rowIndex += 1
            Next
        End Sub

        Private Sub PreRemplirDepuisBase(quantiteBase As Decimal)
            Dim restant As Decimal = quantiteBase
            For Each conditionnement As ProduitConditionnementDTO In _conditionnements
                Dim facteur As Decimal = conditionnement.FacteurVersBase
                If facteur <= 0D Then Continue For

                Dim quantite As Decimal
                If conditionnement.EstUniteBase AndAlso conditionnement.AutoriseFraction Then
                    quantite = restant / facteur
                    restant = 0D
                Else
                    quantite = Decimal.Floor(restant / facteur)
                    restant -= quantite * facteur
                End If

                If _textBoxes.ContainsKey(conditionnement.ProduitConditionnementId) AndAlso quantite > 0D Then
                    _textBoxes(conditionnement.ProduitConditionnementId).Text = FormaterDecimal(quantite)
                End If
                If restant <= 0D Then Exit For
            Next
        End Sub

        Private Sub Quantite_TextChanged(sender As Object, e As EventArgs)
            Recalculer()
        End Sub

        Private Sub Recalculer()
            Try
                QuantiteBase = CalculerTotalBase()
                RepresentationLisible = ConversionUniteService.DecomposerStock(QuantiteBase, _conditionnements)
                lblTotal.Text = "TOTAL : " & FormaterDecimal(QuantiteBase) & " " & LibelleUniteBase()
                lblResume.Text = "RÉSUMÉ : " & RepresentationLisible
                AfficherEcartSiNecessaire()
            Catch ex As Exception
                lblTotal.Text = "STOCK BASE : saisie invalide"
                lblResume.Text = ex.Message
                lblEcart.Text = String.Empty
                lblRepartitionEcart.Text = String.Empty
                lblResultat.Text = String.Empty
            End Try
        End Sub

        Private Function CalculerTotalBase() As Decimal
            Dim total As Decimal = 0D
            For Each kvp As KeyValuePair(Of Integer, TextBox) In _textBoxes
                Dim conditionnement As ProduitConditionnementDTO = TryCast(kvp.Value.Tag, ProduitConditionnementDTO)
                If conditionnement Is Nothing Then Continue For

                Dim quantite As Decimal = LireDecimal(kvp.Value.Text)
                ' Chaque quantité saisie est convertie vers l'unité de base du produit.
                ' La hiérarchie peut avoir 1, 2, 3 ou N niveaux sans changer ce calcul.
                Dim sousTotal As Decimal = ConversionUniteService.ConvertirVersBase(quantite, conditionnement)
                If _labelsSousTotaux.ContainsKey(conditionnement.ProduitConditionnementId) Then
                    _labelsSousTotaux(conditionnement.ProduitConditionnementId).Text = If(sousTotal > 0D, FormaterDecimal(sousTotal) & " " & LibelleUniteBase(), String.Empty)
                End If
                total += sousTotal
            Next
            Return total
        End Function

        Private Sub AfficherEcartSiNecessaire()
            If Not _stockReferenceBase.HasValue Then
                lblEcart.Text = String.Empty
                lblRepartitionEcart.Text = String.Empty
                lblResultat.Text = String.Empty
                Return
            End If

            ' L'inventaire compare exclusivement des QuantiteBase.
            ' Les cartons, paquets ou sacs ne servent qu'à aider l'utilisateur à compter.
            Dim ecart As Decimal = QuantiteBase - _stockReferenceBase.Value
            lblEcart.Text = "STOCK THÉORIQUE : " & FormaterDecimal(_stockReferenceBase.Value) & " " & LibelleUniteBase() & " | ÉCART : " & FormaterDecimal(ecart) & " " & LibelleUniteBase()
            lblRepartitionEcart.Text = "RÉPARTITION ÉCART : " & FormaterEcartDynamique(ecart)
            If ecart = 0D Then
                lblResultat.Text = "RÉSULTAT : CONFORME"
                lblResultat.ForeColor = Color.FromArgb(34, 197, 94)
                lblRepartitionEcart.ForeColor = Color.FromArgb(34, 197, 94)
            ElseIf ecart < 0D Then
                lblResultat.Text = "RÉSULTAT : MANQUE"
                lblResultat.ForeColor = Color.FromArgb(192, 57, 43)
                lblRepartitionEcart.ForeColor = Color.FromArgb(192, 57, 43)
            Else
                lblResultat.Text = "RÉSULTAT : SURPLUS"
                lblResultat.ForeColor = Color.FromArgb(14, 116, 144)
                lblRepartitionEcart.ForeColor = Color.FromArgb(14, 116, 144)
            End If
        End Sub

        Private Function FormaterEcartDynamique(ecartBase As Decimal) As String
            If ecartBase = 0D Then
                Return "0 " & LibelleUniteBase()
            End If

            ' L'écart reste calculé en QuantiteBase. Cette méthode ne change
            ' pas la valeur métier : elle ajoute seulement une lecture humaine
            ' en utilisant les conditionnements actifs du produit.
            Dim signe As String = If(ecartBase < 0D, "-", "+")
            Dim decomposition As String = ConversionUniteService.DecomposerStock(Math.Abs(ecartBase), _conditionnements)
            If String.IsNullOrWhiteSpace(decomposition) Then
                Return signe & FormaterDecimal(Math.Abs(ecartBase)) & " " & LibelleUniteBase()
            End If

            Return signe & decomposition.Replace(" + ", " " & signe)
        End Function

        Private Sub Valider(sender As Object, e As EventArgs)
            Try
                QuantiteBase = CalculerTotalBase()
                RepresentationLisible = ConversionUniteService.DecomposerStock(QuantiteBase, _conditionnements)
                DialogResult = DialogResult.OK
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Quantités par conditionnement", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        Private Shared Function LireDecimal(texte As String) As Decimal
            If String.IsNullOrWhiteSpace(texte) Then Return 0D
            Dim valeur As Decimal
            Dim normalise As String = texte.Trim().Replace(" ", "").Replace(",", ".")
            If Decimal.TryParse(normalise, NumberStyles.Number, CultureInfo.InvariantCulture, valeur) AndAlso valeur >= 0D Then Return valeur
            Throw New InvalidOperationException("Quantité invalide : " & texte)
        End Function

        Private Shared Function LibelleConditionnement(conditionnement As ProduitConditionnementDTO) As String
            If conditionnement Is Nothing Then Return String.Empty
            If Not String.IsNullOrWhiteSpace(conditionnement.LibelleUnite) Then Return conditionnement.LibelleUnite
            If Not String.IsNullOrWhiteSpace(conditionnement.SymboleUnite) Then Return conditionnement.SymboleUnite
            Return If(conditionnement.CodeUnite, String.Empty)
        End Function

        Private Function LibelleUniteBase() As String
            Dim baseUnite As ProduitConditionnementDTO = _conditionnements.FirstOrDefault(Function(c) c.EstUniteBase)
            If baseUnite Is Nothing Then baseUnite = _conditionnements.OrderBy(Function(c) c.FacteurVersBase).FirstOrDefault()
            ' L'unité de base est déterminée depuis la configuration dynamique
            ' du produit. On lit son libellé affichable sans modifier la
            ' QuantiteBase transactionnelle.
            Dim libelle As String = LibelleConditionnement(baseUnite)
            Return If(String.IsNullOrWhiteSpace(libelle), "base", libelle)
        End Function

        Private Shared Function FormaterDecimal(valeur As Decimal) As String
            Return valeur.ToString("0.####", CultureInfo.CurrentCulture)
        End Function
    End Class
End Namespace
