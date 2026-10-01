Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireConditionnementsProduit
        Inherits Form

        Private ReadOnly _service As New ProduitConditionnementService()
        Private ReadOnly cmbProduit As ComboBox
        Private ReadOnly grid As DataGridView
        Private ReadOnly cmbUnite As ComboBox
        Private ReadOnly cmbParent As ComboBox
        Private ReadOnly txtFacteurParent As TextBox
        Private ReadOnly txtOrdre As TextBox
        Private ReadOnly chkBase As CheckBox
        Private ReadOnly chkAchetable As CheckBox
        Private ReadOnly chkVendable As CheckBox
        Private ReadOnly chkFraction As CheckBox
        Private ReadOnly lblApercu As Label
        Private ReadOnly txtPrevisualisation As TextBox
        Private ReadOnly _toolTip As New ToolTip()

        Private _produits As DataTable
        Private _conditionnements As List(Of ProduitConditionnementDTO)
        Private _selectionId As Integer
        Private ReadOnly _produitInitialId As Integer?

        Private Class ComboItem(Of T)
            Public Sub New(libelle As String, valeur As T)
                Me.Libelle = libelle
                Me.Valeur = valeur
            End Sub

            Public ReadOnly Property Libelle As String
            Public ReadOnly Property Valeur As T

            Public Overrides Function ToString() As String
                Return Libelle
            End Function
        End Class

        Public Sub New(Optional produitInitialId As Integer? = Nothing)
            _produitInitialId = produitInitialId
            Text = "Conditionnements produits"
            BackColor = Color.FromArgb(245, 247, 250)
            MinimumSize = New Size(1080, 760)
            AutoScaleMode = AutoScaleMode.Dpi

            Dim root As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 3, .Padding = New Padding(18)}
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 86))
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 305))
            root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim header As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(18)}
            header.Controls.Add(New Label() With {.Text = "CONFIGURATION DES CONDITIONNEMENTS", .Font = New Font("Segoe UI", 17, FontStyle.Bold), .ForeColor = Color.FromArgb(31, 41, 55), .AutoSize = True, .Left = 18, .Top = 10})
            header.Controls.Add(New Label() With {.Text = "Définir la hiérarchie physique utilisée par le moteur de conversion vers la quantité base.", .Font = New Font("Segoe UI", 10), .ForeColor = Color.FromArgb(107, 114, 128), .AutoSize = True, .Left = 20, .Top = 50})

            Dim edition As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(14)}
            cmbProduit = New ComboBox() With {.Left = 12, .Top = 36, .Width = 330, .DropDownStyle = ComboBoxStyle.DropDown}
            cmbProduit.AutoCompleteMode = AutoCompleteMode.SuggestAppend
            cmbProduit.AutoCompleteSource = AutoCompleteSource.CustomSource
            cmbUnite = New ComboBox() With {.Left = 360, .Top = 36, .Width = 170, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbParent = New ComboBox() With {.Left = 550, .Top = 36, .Width = 210, .DropDownStyle = ComboBoxStyle.DropDownList}
            txtFacteurParent = New TextBox() With {.Left = 780, .Top = 36, .Width = 90}
            txtOrdre = New TextBox() With {.Left = 890, .Top = 36, .Width = 70}
            chkBase = New CheckBox() With {.Text = "Unité de base", .Left = 12, .Top = 92, .AutoSize = True}
            chkAchetable = New CheckBox() With {.Text = "Achetable", .Left = 150, .Top = 92, .AutoSize = True, .Checked = True}
            chkVendable = New CheckBox() With {.Text = "Vendable", .Left = 260, .Top = 92, .AutoSize = True, .Checked = True}
            chkFraction = New CheckBox() With {.Text = "Fraction autorisée", .Left = 370, .Top = 92, .AutoSize = True}
            lblApercu = New Label() With {.Left = 530, .Top = 84, .Width = 430, .Height = 48, .ForeColor = Color.FromArgb(14, 116, 144)}
            Dim btnNouveau As New Button() With {.Text = "Nouveau", .Left = 12, .Top = 135, .Width = 105, .Height = 34, .BackColor = Color.FromArgb(52, 73, 94), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            Dim btnEnregistrer As New Button() With {.Text = "Enregistrer", .Left = 130, .Top = 135, .Width = 120, .Height = 34, .BackColor = Color.FromArgb(39, 174, 96), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            Dim btnDesactiver As New Button() With {.Text = "Désactiver", .Left = 265, .Top = 135, .Width = 120, .Height = 34, .BackColor = Color.FromArgb(192, 57, 43), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            txtPrevisualisation = New TextBox() With {.Left = 12, .Top = 180, .Width = 950, .Height = 105, .Multiline = True, .ReadOnly = True, .ScrollBars = ScrollBars.Vertical, .BackColor = Color.FromArgb(249, 250, 251), .BorderStyle = BorderStyle.FixedSingle}
            btnNouveau.FlatAppearance.BorderSize = 0
            btnEnregistrer.FlatAppearance.BorderSize = 0
            btnDesactiver.FlatAppearance.BorderSize = 0

            edition.Controls.AddRange(New Control() {
                New Label() With {.Text = "Produit", .Left = 12, .Top = 14, .AutoSize = True},
                cmbProduit,
                New Label() With {.Text = "Conditionnement", .Left = 360, .Top = 14, .AutoSize = True},
                cmbUnite,
                New Label() With {.Text = "Unité contenue", .Left = 550, .Top = 14, .AutoSize = True},
                cmbParent,
                New Label() With {.Text = "Contient", .Left = 780, .Top = 14, .AutoSize = True},
                txtFacteurParent,
                New Label() With {.Text = "Ordre", .Left = 890, .Top = 14, .AutoSize = True},
                txtOrdre,
                chkBase, chkAchetable, chkVendable, chkFraction, lblApercu, btnNouveau, btnEnregistrer, btnDesactiver, txtPrevisualisation})

            grid = New DataGridView() With {.Dock = DockStyle.Fill, .AutoGenerateColumns = False, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .ReadOnly = True, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .RowHeadersVisible = False, .BackgroundColor = Color.White}
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "ProduitConditionnementId", .Name = "ProduitConditionnementId", .Visible = False})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "LibelleUnite", .HeaderText = "Conditionnement", .Width = 160})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "FacteurVersParent", .HeaderText = "Contient", .Width = 90})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "FacteurVersBase", .HeaderText = "Équivalent en unité de base", .Width = 170})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "Niveau", .HeaderText = "Niveau", .Width = 70})
            grid.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "EstUniteBase", .HeaderText = "Unité de base", .Width = 95})
            grid.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "EstAchetable", .HeaderText = "Achat autorisé", .Width = 105})
            grid.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "EstVendable", .HeaderText = "Vente autorisée", .Width = 105})
            grid.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "AutoriseFraction", .HeaderText = "Fraction autorisée", .Width = 115})
            grid.Columns.Add(New DataGridViewCheckBoxColumn() With {.DataPropertyName = "EstActif", .HeaderText = "Actif", .Width = 60})
            AppliquerStyleGrilleCommercialPro(grid)
            ConfigurerAideSaisie()

            root.Controls.Add(header, 0, 0)
            root.Controls.Add(edition, 0, 1)
            root.Controls.Add(grid, 0, 2)
            Controls.Add(root)

            AddHandler Load, AddressOf FormulaireConditionnementsProduit_Load
            AddHandler cmbProduit.SelectedIndexChanged, AddressOf ProduitSelectionne
            AddHandler grid.SelectionChanged, AddressOf ConditionnementSelectionne
            AddHandler cmbUnite.SelectedIndexChanged, AddressOf MettreAJourApercu
            AddHandler chkBase.CheckedChanged, AddressOf MettreAJourApercu
            AddHandler cmbParent.SelectedIndexChanged, AddressOf MettreAJourApercu
            AddHandler txtFacteurParent.TextChanged, AddressOf MettreAJourApercu
            AddHandler btnNouveau.Click, AddressOf Nouveau
            AddHandler btnEnregistrer.Click, AddressOf Enregistrer
            AddHandler btnDesactiver.Click, AddressOf Desactiver
        End Sub

        Private Sub FormulaireConditionnementsProduit_Load(sender As Object, e As EventArgs)
            ChargerUnites()
            ChargerProduits()
        End Sub

        Private Sub ChargerProduits()
            Dim dal As New DAL(ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString)
            _produits = (New ProduitRepository(dal)).ListerTable()
            cmbProduit.DataSource = _produits
            cmbProduit.DisplayMember = "Libelle"
            cmbProduit.ValueMember = "ProduitId"
            ConfigurerRechercheProduit()
            If _produitInitialId.HasValue Then
                cmbProduit.SelectedValue = _produitInitialId.Value
            End If
        End Sub

        Private Sub ConfigurerRechercheProduit()
            Dim source As New AutoCompleteStringCollection()
            If _produits IsNot Nothing Then
                For Each row As DataRow In _produits.Rows
                    If row IsNot Nothing AndAlso _produits.Columns.Contains("Libelle") AndAlso Not row.IsNull("Libelle") Then
                        source.Add(Convert.ToString(row("Libelle")))
                    End If
                Next
            End If
            cmbProduit.AutoCompleteCustomSource = source
        End Sub

        Private Shared Sub AppliquerStyleGrilleCommercialPro(grille As DataGridView)
            If grille Is Nothing Then Return
            grille.BorderStyle = BorderStyle.None
            grille.EnableHeadersVisualStyles = False
            grille.GridColor = Color.FromArgb(220, 224, 229)
            grille.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            grille.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245)
            grille.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(31, 41, 55)
            grille.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F)
            grille.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(245, 245, 245)
            grille.ColumnHeadersHeight = 38
            grille.DefaultCellStyle.Font = New Font("Segoe UI", 9.5F)
            grille.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 234, 246)
            grille.DefaultCellStyle.SelectionForeColor = Color.FromArgb(52, 73, 94)
            grille.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252)
            grille.RowTemplate.Height = 35
        End Sub

        Private Sub ChargerUnites()
            cmbUnite.DataSource = _service.ListerUnites(True)
            cmbUnite.DisplayMember = "Libelle"
            cmbUnite.ValueMember = "UniteMesureId"
        End Sub

        Private Sub ProduitSelectionne(sender As Object, e As EventArgs)
            If cmbProduit.SelectedValue Is Nothing OrElse TypeOf cmbProduit.SelectedValue Is DataRowView Then Return
            RechargerConditionnements()
        End Sub

        Private Sub RechargerConditionnements()
            Dim produitId As Integer = Convert.ToInt32(cmbProduit.SelectedValue)
            _conditionnements = _service.ListerParProduit(produitId, False)
            grid.DataSource = Nothing
            grid.DataSource = _conditionnements
            RechargerParents()
            MettreAJourPrevisualisationProduit()
            Nouveau(Nothing, EventArgs.Empty)
        End Sub

        Private Sub RechargerParents()
            cmbParent.Items.Clear()
            cmbParent.Items.Add(New ComboItem(Of Nullable(Of Integer))("Aucun - unité de base", Nothing))
            For Each c As ProduitConditionnementDTO In _conditionnements.Where(Function(x) x.EstActif).OrderByDescending(Function(x) x.FacteurVersBase)
                cmbParent.Items.Add(New ComboItem(Of Nullable(Of Integer))("Unité contenue : " & LibelleConditionnement(c) & " (" & c.FacteurVersBase.ToString("0.####") & " base)", c.ProduitConditionnementId))
            Next
            cmbParent.SelectedIndex = 0
        End Sub

        Private Sub ConfigurerAideSaisie()
            _toolTip.SetToolTip(cmbUnite, "Conditionnement que vous configurez. Exemple : Carton, Paquet, Pièce, Sac, Kg.")
            _toolTip.SetToolTip(cmbParent, "Unité directement contenue dans le conditionnement. Exemple : 1 Carton contient 24 Paquets.")
            _toolTip.SetToolTip(txtFacteurParent, "Quantité de l'unité contenue dans 1 conditionnement. Le facteur vers base est calculé automatiquement.")
            _toolTip.SetToolTip(chkBase, "Une seule unité de base active est autorisée par produit. Toutes les QuantiteBase sont exprimées dans cette unité.")
            _toolTip.SetToolTip(chkAchetable, "Autorise ce conditionnement dans les entrées de stock / approvisionnements.")
            _toolTip.SetToolTip(chkVendable, "Autorise ce conditionnement comme base des types de vente.")
            _toolTip.SetToolTip(chkFraction, "Autorise les quantités décimales pour ce conditionnement quand le métier le permet.")
            _toolTip.SetToolTip(txtPrevisualisation, "Prévisualisation métier calculée depuis les relations directes. Les anciennes transactions ne sont jamais recalculées.")
        End Sub

        Private Sub Nouveau(sender As Object, e As EventArgs)
            _selectionId = 0
            If cmbUnite.Items.Count > 0 Then cmbUnite.SelectedIndex = 0
            If cmbParent.Items.Count > 0 Then cmbParent.SelectedIndex = 0
            txtFacteurParent.Text = "1"
            txtOrdre.Text = "0"
            chkBase.Checked = False
            chkAchetable.Checked = True
            chkVendable.Checked = True
            chkFraction.Checked = False
            MettreAJourApercu(Nothing, EventArgs.Empty)
        End Sub

        Private Sub ConditionnementSelectionne(sender As Object, e As EventArgs)
            If grid.CurrentRow Is Nothing Then Return
            Dim item As ProduitConditionnementDTO = TryCast(grid.CurrentRow.DataBoundItem, ProduitConditionnementDTO)
            If item Is Nothing Then Return
            _selectionId = item.ProduitConditionnementId
            cmbUnite.SelectedValue = item.UniteMesureId
            SelectionnerParent(item.ConditionnementParentId)
            txtFacteurParent.Text = If(item.FacteurVersParent.HasValue, item.FacteurVersParent.Value.ToString("0.####", CultureInfo.InvariantCulture), "1")
            txtOrdre.Text = item.OrdreAffichage.ToString(CultureInfo.InvariantCulture)
            chkBase.Checked = item.EstUniteBase
            chkAchetable.Checked = item.EstAchetable
            chkVendable.Checked = item.EstVendable
            chkFraction.Checked = item.AutoriseFraction
            MettreAJourApercu(Nothing, EventArgs.Empty)
        End Sub

        Private Sub SelectionnerParent(parentId As Integer?)
            If cmbParent.Items.Count = 0 Then Return
            For i As Integer = 0 To cmbParent.Items.Count - 1
                Dim item As ComboItem(Of Nullable(Of Integer)) = TryCast(cmbParent.Items(i), ComboItem(Of Nullable(Of Integer)))
                If item IsNot Nothing AndAlso Object.Equals(CType(item.Valeur, Object), CType(parentId, Object)) Then
                    cmbParent.SelectedIndex = i
                    Return
                End If
            Next
            cmbParent.SelectedIndex = 0
        End Sub

        Private Sub MettreAJourApercu(sender As Object, e As EventArgs)
            If chkBase.Checked Then
                lblApercu.Text = "Unité de base : 1 " & LibelleUniteSelectionnee() & " = 1 " & LibelleUniteSelectionnee() & "."
                cmbParent.Enabled = False
                txtFacteurParent.Enabled = False
                Return
            End If

            cmbParent.Enabled = True
            txtFacteurParent.Enabled = True
            Dim facteur As Decimal = LireDecimal(txtFacteurParent.Text)
            Dim parent As ProduitConditionnementDTO = ObtenirParent()
            If parent Is Nothing OrElse facteur <= 0D Then
                lblApercu.Text = "Choisissez un parent et un facteur > 0."
                Return
            End If

            ' Le SuperAdmin saisit uniquement la relation directe :
            ' "1 conditionnement contient X unités contenues".
            ' Le facteur vers base est dérivé de la hiérarchie pour éviter les contradictions.
            lblApercu.Text = "1 " & LibelleUniteSelectionnee() & " contient " & facteur.ToString("0.####") & " " & LibelleConditionnement(parent) &
                " | Équiv. base calculé : " & (facteur * parent.FacteurVersBase).ToString("0.####")
        End Sub

        Private Function ObtenirParent() As ProduitConditionnementDTO
            Dim item As ComboItem(Of Nullable(Of Integer)) = TryCast(cmbParent.SelectedItem, ComboItem(Of Nullable(Of Integer)))
            If item Is Nothing OrElse Not item.Valeur.HasValue Then Return Nothing
            Return _conditionnements.FirstOrDefault(Function(c) c.ProduitConditionnementId = item.Valeur.Value)
        End Function

        Private Sub MettreAJourPrevisualisationProduit()
            If txtPrevisualisation Is Nothing Then Return
            If _conditionnements Is Nothing OrElse _conditionnements.Count = 0 Then
                txtPrevisualisation.Text = "HIÉRARCHIE DU PRODUIT" & Environment.NewLine & "Aucun conditionnement configuré."
                Return
            End If

            Dim actifs As List(Of ProduitConditionnementDTO) = _conditionnements.
                Where(Function(c) c.EstActif).
                OrderByDescending(Function(c) c.FacteurVersBase).
                ToList()
            Dim baseUnite As ProduitConditionnementDTO = actifs.FirstOrDefault(Function(c) c.EstUniteBase)
            Dim libelleBase As String = If(baseUnite Is Nothing, "base", LibelleConditionnement(baseUnite))
            Dim lignes As New List(Of String) From {"HIÉRARCHIE DU PRODUIT"}

            ' Les liens sont stockés du conditionnement vers l'unité contenue.
            ' Pour l'affichage métier, on part donc des plus grands conditionnements
            ' puis on descend récursivement vers l'unité de base.
            Dim racines As List(Of ProduitConditionnementDTO) = actifs.
                Where(Function(c) Not actifs.Any(Function(x) x.ConditionnementParentId.HasValue AndAlso x.ConditionnementParentId.Value = c.ProduitConditionnementId)).
                OrderByDescending(Function(c) c.FacteurVersBase).
                ToList()
            If racines.Count = 0 Then racines = actifs.Take(1).ToList()
            For Each racine As ProduitConditionnementDTO In racines
                AjouterBrancheHierarchie(lignes, racine, 0, actifs, New HashSet(Of Integer)())
            Next

            lignes.Add(String.Empty)
            lignes.Add("ÉQUIVALENCES CALCULÉES")
            For Each c As ProduitConditionnementDTO In actifs.OrderByDescending(Function(x) x.FacteurVersBase)
                lignes.Add("1 " & LibelleConditionnement(c) & " = " & c.FacteurVersBase.ToString("0.####") & " " & libelleBase)
            Next
            txtPrevisualisation.Text = String.Join(Environment.NewLine, lignes)
        End Sub

        Private Sub AjouterBrancheHierarchie(lignes As List(Of String), courant As ProduitConditionnementDTO, niveau As Integer, actifs As List(Of ProduitConditionnementDTO), visites As HashSet(Of Integer))
            If courant Is Nothing Then Return
            If visites.Contains(courant.ProduitConditionnementId) Then
                lignes.Add(New String(" "c, niveau * 4) & "└─ cycle détecté : configuration à corriger")
                Return
            End If
            visites.Add(courant.ProduitConditionnementId)

            Dim prefixe As String = New String(" "c, niveau * 4)
            If niveau = 0 Then
                lignes.Add(prefixe & LibelleConditionnement(courant))
            End If

            If Not courant.ConditionnementParentId.HasValue Then Return
            Dim enfant As ProduitConditionnementDTO = actifs.FirstOrDefault(Function(c) c.ProduitConditionnementId = courant.ConditionnementParentId.Value)
            If enfant Is Nothing Then
                lignes.Add(prefixe & "    └─ unité contenue inactive ou introuvable")
                Return
            End If

            Dim facteur As Decimal = If(courant.FacteurVersParent.HasValue, courant.FacteurVersParent.Value, 0D)
            lignes.Add(prefixe & "    └─ " & facteur.ToString("0.####") & " " & LibelleConditionnement(enfant) & " par " & LibelleConditionnement(courant))
            AjouterBrancheHierarchie(lignes, enfant, niveau + 1, actifs, visites)
        End Sub

        Private Function LibelleUniteSelectionnee() As String
            Dim unite As UniteMesureDTO = TryCast(cmbUnite.SelectedItem, UniteMesureDTO)
            If unite Is Nothing Then Return "conditionnement"
            If Not String.IsNullOrWhiteSpace(unite.Libelle) Then Return unite.Libelle.Trim()
            If Not String.IsNullOrWhiteSpace(unite.Symbole) Then Return unite.Symbole.Trim()
            Return If(unite.Code, "conditionnement").Trim()
        End Function

        Private Shared Function LibelleConditionnement(conditionnement As ProduitConditionnementDTO) As String
            If conditionnement Is Nothing Then Return "base"
            If Not String.IsNullOrWhiteSpace(conditionnement.LibelleUnite) Then Return conditionnement.LibelleUnite.Trim()
            If Not String.IsNullOrWhiteSpace(conditionnement.SymboleUnite) Then Return conditionnement.SymboleUnite.Trim()
            Return If(conditionnement.CodeUnite, "base").Trim()
        End Function

        Private Sub Enregistrer(sender As Object, e As EventArgs)
            Try
                If cmbProduit.SelectedValue Is Nothing OrElse TypeOf cmbProduit.SelectedValue Is DataRowView Then Return
                Dim unite As UniteMesureDTO = TryCast(cmbUnite.SelectedItem, UniteMesureDTO)
                If unite Is Nothing Then Throw New InvalidOperationException("Sélectionnez une unité.")

                Dim parent As ProduitConditionnementDTO = ObtenirParent()
                Dim dto As New ProduitConditionnementDTO With {
                    .ProduitConditionnementId = _selectionId,
                    .ProduitId = Convert.ToInt32(cmbProduit.SelectedValue),
                    .UniteMesureId = unite.UniteMesureId,
                    .ConditionnementParentId = If(chkBase.Checked OrElse parent Is Nothing, CType(Nothing, Integer?), CType(parent.ProduitConditionnementId, Integer?)),
                    .FacteurVersParent = If(chkBase.Checked, CType(Nothing, Decimal?), CType(LireDecimal(txtFacteurParent.Text), Decimal?)),
                    .EstUniteBase = chkBase.Checked,
                    .EstAchetable = chkAchetable.Checked,
                    .EstVendable = chkVendable.Checked,
                    .AutoriseFraction = chkFraction.Checked,
                    .OrdreAffichage = Convert.ToInt32(Math.Max(0D, LireDecimal(txtOrdre.Text))),
                    .EstActif = True
                }

                _service.Enregistrer(dto)
                ' La modification concerne uniquement la configuration future.
                ' Les QuantiteBase déjà stockées dans les transactions historiques ne sont jamais recalculées ici.
                RechargerConditionnements()
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Conditionnements", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        Private Sub Desactiver(sender As Object, e As EventArgs)
            If _selectionId <= 0 Then Return
            If MessageBox.Show("Désactiver ce conditionnement ? Les anciennes transactions resteront inchangées.", "Conditionnements", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then Return
            _service.Desactiver(_selectionId)
            RechargerConditionnements()
        End Sub

        Private Shared Function LireDecimal(texte As String) As Decimal
            Dim valeur As Decimal
            If Decimal.TryParse(If(texte, String.Empty).Trim().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, valeur) Then Return valeur
            If Decimal.TryParse(If(texte, String.Empty).Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, valeur) Then Return valeur
            Return 0D
        End Function
    End Class
End Namespace
