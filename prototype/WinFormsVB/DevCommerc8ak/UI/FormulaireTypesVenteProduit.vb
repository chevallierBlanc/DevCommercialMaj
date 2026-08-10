Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireTypesVenteProduit
        Inherits Form

        Private ReadOnly _produitId As Integer
        Private ReadOnly _prixAchat As Decimal
        Private ReadOnly _conversionUnite As Decimal
        Private ReadOnly _unitePrincipale As String
        Private ReadOnly _uniteSecondaire As String
        Private ReadOnly _uniteMesureStock As String
        Private ReadOnly _service As TypeVenteProduitService
        Private ReadOnly _modeDirectBDD As Boolean
        Private ReadOnly _typesTemporaires As List(Of TypeVenteProduitDTO)

        Private ReadOnly grid As DataGridView
        Private ReadOnly txtNom As TextBox
        Private ReadOnly txtQuantiteEquivalent As TextBox
        Private ReadOnly cmbUniteEquivalent As ComboBox
        Private ReadOnly cmbModePrix As ComboBox
        Private ReadOnly txtCoefficient As TextBox
        Private ReadOnly txtPrixVente As TextBox
        Private ReadOnly chkActif As CheckBox
        Private ReadOnly btnNouveau As Button
        Private ReadOnly btnEnregistrer As Button
        Private ReadOnly btnChangerEtat As Button
        Private ReadOnly lblAide As Label

        Private _typeSelectionneId As Integer
        Private _typeResultat As TypeVenteProduitDTO

        Public ReadOnly Property TypeVenteResultat As TypeVenteProduitDTO
            Get
                Return _typeResultat
            End Get
        End Property

        Private Class UniteOption
            Public Sub New(typeUnite As String, libelle As String)
                Me.TypeUnite = typeUnite
                Me.Libelle = libelle
            End Sub

            Public ReadOnly Property TypeUnite As String
            Public ReadOnly Property Libelle As String

            Public Overrides Function ToString() As String
                Return Libelle
            End Function
        End Class

        Public Sub New(produitId As Integer,
                       prixAchat As Decimal,
                       conversionUnite As Decimal,
                       Optional modeDirectBDD As Boolean = True,
                       Optional typesTemporaires As List(Of TypeVenteProduitDTO) = Nothing,
                       Optional typeInitial As TypeVenteProduitDTO = Nothing,
                       Optional unitePrincipale As String = Nothing,
                       Optional uniteSecondaire As String = Nothing,
                       Optional uniteMesureStock As String = Nothing)
            _produitId = produitId
            _prixAchat = prixAchat
            _conversionUnite = If(conversionUnite > 0D, conversionUnite, 1D)
            _unitePrincipale = If(String.IsNullOrWhiteSpace(unitePrincipale), "Unité principale", unitePrincipale.Trim())
            _uniteSecondaire = If(String.IsNullOrWhiteSpace(uniteSecondaire), "Unité secondaire", uniteSecondaire.Trim())
            _uniteMesureStock = If(String.IsNullOrWhiteSpace(uniteMesureStock), String.Empty, uniteMesureStock.Trim())
            _service = New TypeVenteProduitService()
            _modeDirectBDD = modeDirectBDD
            _typesTemporaires = If(typesTemporaires, New List(Of TypeVenteProduitDTO)())

            Me.Text = "Types de vente personnalisés"
            Me.StartPosition = FormStartPosition.CenterParent
            Me.Size = New Size(840, 560)
            Me.MinimumSize = New Size(780, 520)
            Me.FormBorderStyle = FormBorderStyle.Sizable
            Me.AutoScaleMode = AutoScaleMode.Dpi
            Me.BackColor = Color.FromArgb(245, 247, 250)

            Dim layout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2, .Padding = New Padding(12)}
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 215))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim panelEdition As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(12)}
            Dim lblNom As New Label() With {.Text = "Nom", .Left = 10, .Top = 16, .AutoSize = True}
            txtNom = New TextBox() With {.Left = 10, .Top = 36, .Width = 220}
            Dim lblQuantite As New Label() With {.Text = "Quantité équivalente", .Left = 250, .Top = 16, .AutoSize = True}
            txtQuantiteEquivalent = New TextBox() With {.Left = 250, .Top = 36, .Width = 120}
            Dim lblUnite As New Label() With {.Text = "Unité", .Left = 390, .Top = 16, .AutoSize = True}
            cmbUniteEquivalent = New ComboBox() With {.Left = 390, .Top = 36, .Width = 140, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbUniteEquivalent.Items.Add(New UniteOption("PRINCIPALE", _unitePrincipale & " — unité principale"))
            cmbUniteEquivalent.Items.Add(New UniteOption("SECONDAIRE", _uniteSecondaire & " — unité secondaire"))
            If Not String.IsNullOrWhiteSpace(_uniteMesureStock) Then
                cmbUniteEquivalent.Items.Add(New UniteOption("MESURE", _uniteMesureStock & " — unité de mesure"))
            End If
            cmbUniteEquivalent.SelectedIndex = 1
            Dim lblMode As New Label() With {.Text = "Mode prix", .Left = 550, .Top = 16, .AutoSize = True}
            cmbModePrix = New ComboBox() With {.Left = 550, .Top = 36, .Width = 110, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbModePrix.Items.AddRange(New Object() {"FIXE", "COEFFICIENT"})
            cmbModePrix.SelectedIndex = 0
            Dim lblCoefficient As New Label() With {.Text = "Coeff. / %", .Left = 680, .Top = 16, .AutoSize = True}
            txtCoefficient = New TextBox() With {.Left = 680, .Top = 36, .Width = 100}
            Dim lblPrix As New Label() With {.Text = "Prix vente final", .Left = 10, .Top = 82, .AutoSize = True}
            txtPrixVente = New TextBox() With {.Left = 10, .Top = 102, .Width = 120}
            chkActif = New CheckBox() With {.Text = "Actif", .Left = 10, .Top = 82, .AutoSize = True, .Checked = True}
            chkActif.Left = 150
            chkActif.Top = 104
            lblAide = New Label() With {.Left = 250, .Top = 92, .Width = 540, .Height = 40, .AutoSize = False, .ForeColor = Color.FromArgb(41, 128, 185)}
            btnNouveau = New Button() With {.Text = "Nouveau", .Left = 10, .Top = 150, .Width = 110, .Height = 34, .BackColor = Color.FromArgb(52, 73, 94), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            btnEnregistrer = New Button() With {.Text = "Enregistrer", .Left = 135, .Top = 150, .Width = 120, .Height = 34, .BackColor = Color.FromArgb(39, 174, 96), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            btnChangerEtat = New Button() With {.Text = "Désactiver", .Left = 270, .Top = 150, .Width = 120, .Height = 34, .BackColor = Color.FromArgb(192, 57, 43), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            btnNouveau.FlatAppearance.BorderSize = 0
            btnEnregistrer.FlatAppearance.BorderSize = 0
            btnChangerEtat.FlatAppearance.BorderSize = 0
            panelEdition.Controls.AddRange(New Control() {lblNom, txtNom, lblQuantite, txtQuantiteEquivalent, lblUnite, cmbUniteEquivalent, lblMode, cmbModePrix, lblCoefficient, txtCoefficient, lblPrix, txtPrixVente, chkActif, lblAide, btnNouveau, btnEnregistrer, btnChangerEtat})

            grid = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .ReadOnly = True,
                .AutoGenerateColumns = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .RowHeadersVisible = False
            }
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "TypeVenteProduitId", .Name = "TypeVenteProduitId", .Visible = False})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "Nom", .HeaderText = "Nom", .Width = 180})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "QuantiteEquivalent", .HeaderText = "Qté équiv.", .Width = 90})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "UniteEquivalentAffichage", .HeaderText = "Unité", .Width = 120})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "ModePrixAffichage", .HeaderText = "Mode prix", .Width = 130})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixVente", .HeaderText = "Prix vente", .Width = 110})
            grid.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "Actif", .HeaderText = "Actif", .Width = 60})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "NomAffichage", .HeaderText = "Nom affiché", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})

            layout.Controls.Add(panelEdition, 0, 0)
            layout.Controls.Add(grid, 0, 1)
            Me.Controls.Add(layout)

            AddHandler btnNouveau.Click, AddressOf NouveauType
            AddHandler btnEnregistrer.Click, AddressOf EnregistrerType
            AddHandler btnChangerEtat.Click, AddressOf ChangerEtatType
            AddHandler grid.SelectionChanged, AddressOf ChargerSelection
            AddHandler txtCoefficient.TextChanged, AddressOf RecalculerPrixDepuisCoefficient
            AddHandler txtQuantiteEquivalent.TextChanged, AddressOf RecalculerPrixDepuisCoefficient
            AddHandler cmbUniteEquivalent.SelectedIndexChanged, AddressOf RecalculerPrixDepuisCoefficient
            AddHandler cmbModePrix.SelectedIndexChanged, AddressOf RecalculerPrixDepuisCoefficient

            ChargerListe()
            If typeInitial IsNot Nothing Then
                ChargerType(typeInitial)
            Else
                NouveauType(Nothing, EventArgs.Empty)
            End If
        End Sub

        Private Sub ChargerListe()
            Dim liste As List(Of TypeVenteProduitDTO)
            If _modeDirectBDD Then
                liste = _service.ListerParProduit(_produitId, False)
            Else
                liste = _typesTemporaires.OrderByDescending(Function(x) x.Actif).ThenBy(Function(x) x.Nom).ToList()
            End If
            grid.DataSource = Nothing
            grid.DataSource = liste
        End Sub

        Private Sub NouveauType(sender As Object, e As EventArgs)
            _typeSelectionneId = 0
            _typeResultat = Nothing
            txtNom.Clear()
            txtQuantiteEquivalent.Clear()
            SelectionnerTypeUnite("SECONDAIRE")
            cmbModePrix.SelectedItem = "FIXE"
            txtCoefficient.Clear()
            txtCoefficient.Enabled = False
            txtPrixVente.Clear()
            chkActif.Checked = True
            btnEnregistrer.Text = "Enregistrer"
            btnChangerEtat.Text = "Désactiver"
            lblAide.Text = "Mode COEFFICIENT : le prix final est calculé sur le coût équivalent de la quantité."
        End Sub

        Private Sub ChargerType(item As TypeVenteProduitDTO)
            If item Is Nothing Then
                Return
            End If

            _typeSelectionneId = item.TypeVenteProduitId
            txtNom.Text = item.Nom
            txtQuantiteEquivalent.Text = item.QuantiteEquivalent.ToString("N2")
            SelectionnerTypeUnite(If(String.IsNullOrWhiteSpace(item.TypeQuantiteEquivalent), item.TypeUniteEquivalent, item.TypeQuantiteEquivalent))
            cmbModePrix.SelectedItem = item.ModePrix.ToUpperInvariant()
            txtCoefficient.Enabled = String.Equals(item.ModePrix, "COEFFICIENT", StringComparison.OrdinalIgnoreCase)
            txtCoefficient.Text = If(item.Coefficient.HasValue, item.Coefficient.Value.ToString("N4"), String.Empty)
            txtPrixVente.Text = item.PrixVente.ToString("N2")
            chkActif.Checked = item.Actif
            btnEnregistrer.Text = If(_typeSelectionneId <> 0, "Modifier", "Enregistrer")
            btnChangerEtat.Text = If(item.Actif, "Désactiver", "Activer")
            lblAide.Text = item.ModePrixAffichage
        End Sub

        Private Sub ChargerSelection(sender As Object, e As EventArgs)
            If grid.CurrentRow Is Nothing Then
                Return
            End If

            Dim item As TypeVenteProduitDTO = TryCast(grid.CurrentRow.DataBoundItem, TypeVenteProduitDTO)
            If item Is Nothing Then
                Return
            End If

            ChargerType(item)
        End Sub

        Private Function LireDecimal(texte As String) As Decimal
            Dim valeur As Decimal
            If Decimal.TryParse(If(String.IsNullOrWhiteSpace(texte), "0", texte.Trim()), valeur) Then
                Return valeur
            End If
            If Decimal.TryParse(If(String.IsNullOrWhiteSpace(texte), "0", texte.Trim()), Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, valeur) Then
                Return valeur
            End If
            Return 0D
        End Function

        Private Function TenterLireCoefficient(texte As String, ByRef coefficient As Decimal) As Boolean
            coefficient = 0D
            Dim brut As String = If(texte, String.Empty).Trim()
            If brut = String.Empty Then
                Return False
            End If

            Dim valeur As Decimal = LireDecimal(brut)
            If valeur <= 0D Then
                Return False
            End If

            If brut.Contains(",") OrElse brut.Contains(".") Then
                coefficient = valeur
            Else
                coefficient = 1D + (valeur / 100D)
            End If

            Return coefficient > 0D
        End Function

        Private Sub RecalculerPrixDepuisCoefficient(sender As Object, e As EventArgs)
            Dim modePrix As String = Convert.ToString(cmbModePrix.SelectedItem)
            txtCoefficient.Enabled = String.Equals(modePrix, "COEFFICIENT", StringComparison.OrdinalIgnoreCase)
            If Not txtCoefficient.Enabled Then
                lblAide.Text = "Mode FIXE : le prix final saisi sera utilisé tel quel."
                Return
            End If

            Dim coefficient As Decimal
            If Not TenterLireCoefficient(txtCoefficient.Text, coefficient) Then
                lblAide.Text = "Saisissez un coefficient direct (1.25) ou un pourcentage (25)."
                Return
            End If

            Dim quantiteEquivalent As Decimal = Math.Max(0D, LireDecimal(txtQuantiteEquivalent.Text))
            If quantiteEquivalent <= 0D Then
                lblAide.Text = "Saisissez une quantité équivalente supérieure à zéro."
                Return
            End If

            Dim quantiteBase As Decimal = CalculVenteService.CalculerQuantiteBaseTypeVente(quantiteEquivalent, ObtenirTypeUniteSelectionne(), _conversionUnite)
            Dim coutEquivalent As Decimal = _prixAchat * (quantiteBase / _conversionUnite)
            Dim prixFinal As Decimal = Math.Round(coutEquivalent * coefficient, 2)
            txtPrixVente.Text = prixFinal.ToString("N2")
            lblAide.Text = "Prix calculé sur un coût équivalent de " & coutEquivalent.ToString("N2")
        End Sub

        Private Function ObtenirTypeUniteSelectionne() As String
            Dim optionUnite As UniteOption = TryCast(cmbUniteEquivalent.SelectedItem, UniteOption)
            If optionUnite IsNot Nothing Then
                Return optionUnite.TypeUnite
            End If

            Return "SECONDAIRE"
        End Function

        Private Sub SelectionnerTypeUnite(typeUnite As String)
            Dim cible As String = StockUnitConversionService.NormaliserTypeQuantiteEquivalent(typeUnite)
            For i As Integer = 0 To cmbUniteEquivalent.Items.Count - 1
                Dim optionUnite As UniteOption = TryCast(cmbUniteEquivalent.Items(i), UniteOption)
                If optionUnite IsNot Nothing AndAlso String.Equals(optionUnite.TypeUnite, cible, StringComparison.OrdinalIgnoreCase) Then
                    cmbUniteEquivalent.SelectedIndex = i
                    Return
                End If
            Next
        End Sub

        Private Function ConstruireDto() As TypeVenteProduitDTO
            Dim nom As String = txtNom.Text.Trim()
            If nom = String.Empty Then
                Throw New InvalidOperationException("Le nom du type est obligatoire.")
            End If

            Dim quantiteEquivalent As Decimal = LireDecimal(txtQuantiteEquivalent.Text)
            If quantiteEquivalent <= 0D Then
                Throw New InvalidOperationException("La quantité équivalente doit être supérieure à zéro.")
            End If

            Dim modePrix As String = Convert.ToString(cmbModePrix.SelectedItem)
            Dim coefficient As Decimal? = Nothing
            If String.Equals(modePrix, "COEFFICIENT", StringComparison.OrdinalIgnoreCase) Then
                Dim coefficientLu As Decimal
                If Not TenterLireCoefficient(txtCoefficient.Text, coefficientLu) Then
                    Throw New InvalidOperationException("Le coefficient est obligatoire en mode COEFFICIENT.")
                End If
                coefficient = coefficientLu
            End If

            Dim prixVente As Decimal = LireDecimal(txtPrixVente.Text)
            If prixVente <= 0D Then
                Throw New InvalidOperationException("Le prix de vente final doit être supérieur à zéro.")
            End If

            Return New TypeVenteProduitDTO With {
                .TypeVenteProduitId = _typeSelectionneId,
                .ProduitId = _produitId,
                .Nom = nom,
                .QuantiteEquivalent = quantiteEquivalent,
                .TypeUniteEquivalent = ObtenirTypeUniteSelectionne(),
                .TypeQuantiteEquivalent = ObtenirTypeUniteSelectionne(),
                .ModePrix = modePrix,
                .Coefficient = coefficient,
                .PrixVente = prixVente,
                .Actif = chkActif.Checked
            }
        End Function

        Private Sub EnregistrerType(sender As Object, e As EventArgs)
            Try
                Dim dto As TypeVenteProduitDTO = ConstruireDto()
                If _modeDirectBDD Then
                    If dto.TypeVenteProduitId > 0 Then
                        _service.MettreAJour(dto)
                    Else
                        _typeSelectionneId = _service.Ajouter(dto)
                    End If
                    ChargerListe()
                    SelectionnerType(_typeSelectionneId)
                Else
                    _typeResultat = dto
                    DialogResult = DialogResult.OK
                    Close()
                End If
            Catch ex As Exception
                Dim message As String = ex.Message
                If message.IndexOf("UX_TypesVenteProduit_ProduitNomActif", StringComparison.OrdinalIgnoreCase) >= 0 Then
                    message = "Un type actif avec ce nom existe déjà pour ce produit."
                End If
                MessageBox.Show(message, "Type personnalisé", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        Private Sub ChangerEtatType(sender As Object, e As EventArgs)
            Dim nouvelEtat As Boolean = Not chkActif.Checked
            chkActif.Checked = nouvelEtat
            btnChangerEtat.Text = If(nouvelEtat, "Désactiver", "Activer")

            If Not _modeDirectBDD Then
                Return
            End If

            If _typeSelectionneId <= 0 Then
                Return
            End If

            _service.ChangerEtat(_typeSelectionneId, nouvelEtat)
            ChargerListe()
            SelectionnerType(_typeSelectionneId)
        End Sub

        Private Sub SelectionnerType(typeVenteProduitId As Integer)
            For Each row As DataGridViewRow In grid.Rows
                Dim item As TypeVenteProduitDTO = TryCast(row.DataBoundItem, TypeVenteProduitDTO)
                If item IsNot Nothing AndAlso item.TypeVenteProduitId = typeVenteProduitId Then
                    row.Selected = True
                    grid.CurrentCell = row.Cells(1)
                    Exit For
                End If
            Next
        End Sub
    End Class
End Namespace
