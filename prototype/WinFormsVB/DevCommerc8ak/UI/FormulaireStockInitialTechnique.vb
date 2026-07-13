Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Diagnostics
Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D
Imports System.Data.SqlClient

Namespace DevCommerc8ak
    Public Class FormulaireStockInitialTechnique
        Inherits Form

        ' --- Services ---
        Private _service As SuperAdminService
        Private _log As New ProductionLogService()

        ' --- Composants UI ---
        Private grid As DataGridView
        Private btnRecharger As Button
        Private btnEnregistrer As Button
        Private lblTitle As Label
        Private lblSubtitle As Label
        Private txtRecherche As TextBox
        Private cmbFiltreRapide As ComboBox
        Private cmbCategorieFiltre As ComboBox
        Private lblResultats As Label

        ' --- Données ---
        Private _categories As DataTable
        Private _majGrilleEnCours As Boolean
        Private _chargementEnCours As Boolean
        Private _sourceTable As DataTable
        Private ReadOnly _bindingSource As New BindingSource()

        ' --- Palette de Couleurs Enterprise ERP ---
        Private ReadOnly ColorBg As Color = Color.FromArgb(240, 242, 245)
        Private ReadOnly ColorHeaderBg As Color = Color.White
        Private ReadOnly ColorCardBg As Color = Color.White
        Private ReadOnly ColorPrimary As Color = Color.FromArgb(0, 102, 204)
        Private ReadOnly ColorAccent As Color = Color.FromArgb(0, 102, 204)
        Private ReadOnly ColorSuccess As Color = Color.FromArgb(34, 197, 94)
        Private ReadOnly ColorDanger As Color = Color.FromArgb(211, 47, 47)
        Private ReadOnly ColorTextPrimary As Color = Color.FromArgb(33, 43, 54)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(99, 115, 129)
        Private ReadOnly ColorBorder As Color = Color.FromArgb(224, 224, 224)

        ' --- Polices ---
        Private ReadOnly FontMain As New Font("Segoe UI", 9.0F)
        Private ReadOnly FontBold As New Font("Segoe UI", 9.0F, FontStyle.Bold)
        Private ReadOnly FontTitle As New Font("Segoe UI", 15.0F, FontStyle.Bold)
        Private ReadOnly FontSubtitle As New Font("Segoe UI", 9.5F)
        Private ReadOnly FontButton As New Font("Segoe UI", 9.0F, FontStyle.Bold)

        Public Sub New()
            Try
                _service = New SuperAdminService()
            Catch ex As Exception
                MessageBox.Show("Erreur d'initialisation du service : " & ex.Message)
            End Try

            Me.Text = "Administration - Stock Initial Technique"
            Me.Size = New Size(1450, 850)
            Me.MinimumSize = New Size(1100, 750)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = ColorBg
            Me.Font = FontMain
            Me.DoubleBuffered = True

            BuildUi()
            AddHandler Me.Load, AddressOf FormulaireStockInitialTechnique_Load
        End Sub

        Private Sub BuildUi()
            Me.Controls.Clear()

            Dim rootLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3,
                .BackColor = ColorBg
            }
            rootLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 100))
            rootLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            rootLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 80))

            Dim pnlHeader As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorHeaderBg,
                .Padding = New Padding(30, 20, 30, 20)
            }

            lblTitle = New Label() With {
                .Text = "Gestion du Stock Initial Technique",
                .Font = FontTitle,
                .ForeColor = ColorTextPrimary,
                .AutoSize = True,
                .Location = New Point(30, 20)
            }

            lblSubtitle = New Label() With {
                .Text = "Initialisation des quantités réelles et configuration des types de vente par produit.",
                .Font = FontSubtitle,
                .ForeColor = ColorTextSecondary,
                .AutoSize = True,
                .Location = New Point(30, 55)
            }

            pnlHeader.Controls.AddRange({lblTitle, lblSubtitle})
            rootLayout.Controls.Add(pnlHeader, 0, 0)

            Dim pnlMain As New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(30, 10, 30, 10)
            }

            Dim card As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorCardBg,
                .Padding = New Padding(1)
            }

            Dim contentLayout As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2}
            contentLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 64))
            contentLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim pnlFiltres As New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(16, 12, 16, 8),
                .WrapContents = False,
                .AutoScroll = True,
                .BackColor = Color.White
            }
            txtRecherche = New TextBox() With {.Width = 260, .Font = FontMain}
            cmbFiltreRapide = New ComboBox() With {.Width = 210, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontMain}
            cmbCategorieFiltre = New ComboBox() With {.Width = 220, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontMain}
            lblResultats = New Label() With {.AutoSize = True, .ForeColor = ColorTextSecondary, .Font = FontBold, .Margin = New Padding(12, 10, 0, 0)}
            cmbFiltreRapide.Items.AddRange(New Object() {
                "Tous les produits",
                "Sans prix d'achat",
                "Sans prix de gros",
                "Sans prix de détail",
                "Sans aucun prix de vente",
                "Sans catégorie",
                "Sans unité principale",
                "Sans conversion d'unité",
                "Stock égal à zéro",
                "Stock non initialisé",
                "Produits inactifs",
                "Produits actifs",
                "Avec incohérence de données"
            })
            cmbFiltreRapide.SelectedIndex = 0
            pnlFiltres.Controls.AddRange({
                New Label() With {.Text = "Recherche", .AutoSize = True, .Margin = New Padding(0, 10, 6, 0), .ForeColor = ColorTextSecondary},
                txtRecherche,
                New Label() With {.Text = "Filtre", .AutoSize = True, .Margin = New Padding(14, 10, 6, 0), .ForeColor = ColorTextSecondary},
                cmbFiltreRapide,
                New Label() With {.Text = "Catégorie", .AutoSize = True, .Margin = New Padding(14, 10, 6, 0), .ForeColor = ColorTextSecondary},
                cmbCategorieFiltre,
                lblResultats
            })

            grid = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None,
                .RowHeadersVisible = False,
                .AllowUserToAddRows = True,
                .AllowUserToDeleteRows = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .EnableHeadersVisualStyles = False,
                .GridColor = ColorBorder,
                .ColumnHeadersHeight = 45
            }

            Dim headerStyle As New DataGridViewCellStyle() With {
                .BackColor = Color.FromArgb(248, 249, 251),
                .ForeColor = ColorTextPrimary,
                .Font = FontBold,
                .SelectionBackColor = Color.FromArgb(248, 249, 251),
                .Alignment = DataGridViewContentAlignment.MiddleLeft
            }
            grid.ColumnHeadersDefaultCellStyle = headerStyle

            Dim cellStyle As New DataGridViewCellStyle() With {
                .Font = FontMain,
                .ForeColor = ColorTextPrimary,
                .SelectionBackColor = Color.FromArgb(232, 240, 254),
                .SelectionForeColor = ColorPrimary,
                .Padding = New Padding(5, 0, 5, 0)
            }
            grid.DefaultCellStyle = cellStyle
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 253, 255)

            contentLayout.Controls.Add(pnlFiltres, 0, 0)
            contentLayout.Controls.Add(grid, 0, 1)
            card.Controls.Add(contentLayout)
            pnlMain.Controls.Add(card)
            rootLayout.Controls.Add(pnlMain, 0, 1)

            Dim pnlFooter As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = Color.White,
                .Padding = New Padding(30, 15, 30, 15)
            }
            AddHandler pnlFooter.Paint, Sub(s, e) e.Graphics.DrawLine(New Pen(ColorBorder), 0, 0, pnlFooter.Width, 0)

            btnRecharger = New Button() With {
                .Text = "RECHARGER LES DONNÉES",
                .Size = New Size(200, 45),
                .Location = New Point(30, 17)
            }
            StyliserBouton(btnRecharger, Color.White, ColorTextSecondary, True)

            btnEnregistrer = New Button() With {
                .Text = "ENREGISTRER LE STOCK INITIAL",
                .Size = New Size(250, 45)
                            }
            btnEnregistrer.Location = New Point(250, 17)
            StyliserBouton(btnEnregistrer, ColorPrimary, Color.White, False)

            pnlFooter.Controls.AddRange({btnRecharger, btnEnregistrer})
            rootLayout.Controls.Add(pnlFooter, 0, 2)

            Me.Controls.Add(rootLayout)

            AddHandler btnRecharger.Click, AddressOf Recharger
            AddHandler btnEnregistrer.Click, AddressOf EnregistrerStockInitial
            AddHandler grid.CellValueChanged, AddressOf Grid_CellValueChanged
            AddHandler grid.CurrentCellDirtyStateChanged, AddressOf Grid_CurrentCellDirtyStateChanged
            AddHandler txtRecherche.TextChanged, AddressOf ChangerFiltres
            AddHandler cmbFiltreRapide.SelectedIndexChanged, AddressOf ChangerFiltres
            AddHandler cmbCategorieFiltre.SelectedIndexChanged, AddressOf ChangerFiltres
        End Sub

        Private Sub StyliserBouton(btn As Button, bgColor As Color, fgColor As Color, hasBorder As Boolean)
            btn.FlatStyle = FlatStyle.Flat
            btn.BackColor = bgColor
            btn.ForeColor = fgColor
            btn.Font = FontButton
            btn.Cursor = Cursors.Hand
            btn.FlatAppearance.BorderSize = If(hasBorder, 1, 0)
            If hasBorder Then btn.FlatAppearance.BorderColor = ColorBorder
        End Sub

        Private Sub FormulaireStockInitialTechnique_Load(sender As Object, e As EventArgs)
            Recharger(Nothing, EventArgs.Empty)
        End Sub

        'Private Sub Recharger(sender As Object, e As EventArgs)
        '    Try
        '        Me.Cursor = Cursors.WaitCursor
        '        _categories = _service.ListerCategories()
        '        Dim dt As DataTable = _service.ListerProduitsStockInitial()

        '        Dim colonnes As String() = {"QuantiteInitiale", "QuantitePrincipale", "QuantiteSecondaire", "PrixAchatOptionnel", "TypesPersonnalises", "DateInitiale", "EquivalentSecondaire", "StockActuelLisible", "StockApresLisible", "ResumeQuantite"}
        '        For Each col As String In colonnes
        '            If Not dt.Columns.Contains(col) Then
        '                Dim type As Type = GetType(Decimal)
        '                If col = "TypesPersonnalises" Or col.Contains("Lisible") Or col = "ResumeQuantite" Then type = GetType(String)
        '                If col = "DateInitiale" Then type = GetType(Date)
        '                dt.Columns.Add(col, type)
        '            End If
        '        Next

        '        For Each row As DataRow In dt.Rows
        '            If row.IsNull("DateInitiale") Then row("DateInitiale") = Date.Now
        '            CalculerLigne(row)
        '        Next

        '        grid.DataSource = dt
        '        ConfigurerColonnes()
        '    Catch ex As Exception
        '        _log.Error("FormulaireStockInitialTechnique", "Recharger", "Erreur de chargement.", ex)
        '        MessageBox.Show("Erreur : " & ex.Message)
        '    Finally
        '        Me.Cursor = Cursors.Default
        '    End Try
        'End Sub

        Private Sub Recharger(sender As Object, e As EventArgs)
            Try
                If _sourceTable IsNot Nothing AndAlso _sourceTable.GetChanges() IsNot Nothing Then
                    Dim confirmation As DialogResult = MessageBox.Show("Des modifications non enregistrées existent. Voulez-vous recharger et perdre ces changements ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                    If confirmation <> DialogResult.Yes Then
                        Return
                    End If
                End If

                _chargementEnCours = True
                Me.Cursor = Cursors.WaitCursor
                _categories = _service.ListerCategories()
                Dim dt As DataTable = _service.ListerProduitsStockInitial()
                If Not dt.Columns.Contains("QuantiteInitiale") Then dt.Columns.Add("QuantiteInitiale", GetType(Decimal))
                If Not dt.Columns.Contains("QuantitePrincipale") Then dt.Columns.Add("QuantitePrincipale", GetType(Decimal))
                If Not dt.Columns.Contains("QuantiteSecondaire") Then dt.Columns.Add("QuantiteSecondaire", GetType(Decimal))
                If Not dt.Columns.Contains("PrixAchatOptionnel") Then dt.Columns.Add("PrixAchatOptionnel", GetType(Decimal))
                If Not dt.Columns.Contains("TypesPersonnalises") Then dt.Columns.Add("TypesPersonnalises", GetType(String))
                If Not dt.Columns.Contains("DateInitiale") Then dt.Columns.Add("DateInitiale", GetType(Date))
                If Not dt.Columns.Contains("EquivalentSecondaire") Then dt.Columns.Add("EquivalentSecondaire", GetType(Decimal))
                If Not dt.Columns.Contains("StockActuelLisible") Then dt.Columns.Add("StockActuelLisible", GetType(String))
                If Not dt.Columns.Contains("StockApresLisible") Then dt.Columns.Add("StockApresLisible", GetType(String))
                If Not dt.Columns.Contains("ResumeQuantite") Then dt.Columns.Add("ResumeQuantite", GetType(String))
                If Not dt.Columns.Contains("RechercheNormalisee") Then dt.Columns.Add("RechercheNormalisee", GetType(String))
                For Each row As DataRow In dt.Rows
                    If row.IsNull("DateInitiale") Then
                        row("DateInitiale") = Date.Now
                    End If
                    row("RechercheNormalisee") = ConstruireTexteRecherche(row)
                    CalculerLigne(row)
                Next

                _sourceTable = dt
                _bindingSource.DataSource = dt.DefaultView
                grid.DataSource = _bindingSource
                ChargerCategoriesFiltre()
                AppliquerFiltres()
                ConfigurerColonnes()
            Catch ex As Exception
                Debug.WriteLine("FormulaireStockInitialTechnique.Recharger")
                Debug.WriteLine("Type SelectedItem : " & If(cmbCategorieFiltre Is Nothing OrElse cmbCategorieFiltre.SelectedItem Is Nothing, "Nothing", cmbCategorieFiltre.SelectedItem.GetType().FullName))
                Debug.WriteLine("Type SelectedValue : " & If(cmbCategorieFiltre Is Nothing OrElse cmbCategorieFiltre.SelectedValue Is Nothing, "Nothing", cmbCategorieFiltre.SelectedValue.GetType().FullName))
                Debug.WriteLine("SelectedItem : " & If(cmbCategorieFiltre Is Nothing OrElse cmbCategorieFiltre.SelectedItem Is Nothing, "Nothing", cmbCategorieFiltre.SelectedItem.ToString()))
                Debug.WriteLine("SelectedValue : " & If(cmbCategorieFiltre Is Nothing OrElse cmbCategorieFiltre.SelectedValue Is Nothing, "Nothing", cmbCategorieFiltre.SelectedValue.ToString()))
                Debug.WriteLine(ex.ToString())
                Dim log As New ProductionLogService()
                log.Error("FormulaireStockInitialTechnique", "Recharger", "Chargement du stock initial impossible.", ex)
                MessageBox.Show("Impossible de charger le stock initial : " & ex.Message)
            Finally
                _chargementEnCours = False
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Sub ConfigurerColonnes()
            If grid.Columns.Contains("ProduitId") Then grid.Columns("ProduitId").Visible = False
            If grid.Columns.Contains("CategorieId") Then grid.Columns("CategorieId").Visible = False

            Dim mappings As New Dictionary(Of String, String) From {
                {"Libelle", "PRODUIT"},
                {"NomCategorie", "CATÉGORIE"},
                {"CodeBarres", "CODE-BARRES"},
                {"UnitePrincipale", "UNITÉ (P)"},
                {"UniteSecondaire", "UNITÉ (S)"},
                {"ConversionUnite", "CONVERSION"},
                {"QuantitePrincipale", "QTÉ (P)"},
                {"QuantiteSecondaire", "QTÉ (S)"},
                {"QuantiteInitiale", "STOCK BASE"},
                {"EquivalentSecondaire", "ÉQUIV. (S)"},
                {"StockActuelLisible", "STOCK ACTUEL"},
                {"StockApresLisible", "STOCK APRÈS"},
                {"ResumeQuantite", "RÉSUMÉ"},
                {"PrixAchatOptionnel", "PRIX ACHAT"},
                {"PrixGros", "PRIX GROS"},
                {"PrixDemi", "PRIX DEMI"},
                {"PrixQuart", "PRIX QUART"},
                {"PrixDetail", "PRIX PIÈCE"},
                {"PrixDouzaine", "PRIX DOUZAINE"},
                {"TypesPersonnalises", "TYPES PERSO."},
                {"DateInitiale", "DATE"},
                {"EstActif", "ACTIF"}
            }

            For Each kvp As KeyValuePair(Of String, String) In mappings
                If grid.Columns.Contains(kvp.Key) Then
                    grid.Columns(kvp.Key).HeaderText = kvp.Value
                End If
            Next

            Dim readOnlyCols As String() = {"QuantiteInitiale", "EquivalentSecondaire", "StockActuelLisible", "StockApresLisible", "ResumeQuantite"}
            For Each col As String In readOnlyCols
                If grid.Columns.Contains(col) Then
                    grid.Columns(col).ReadOnly = True
                    grid.Columns(col).DefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250)
                End If
            Next

            Dim inputCols As String() = {"QuantitePrincipale", "QuantiteSecondaire", "PrixAchatOptionnel", "PrixGros", "PrixDemi", "PrixQuart", "PrixDetail", "PrixDouzaine", "NomCategorie", "Libelle", "UnitePrincipale", "UniteSecondaire", "ConversionUnite"}
            For Each col As String In inputCols
                If grid.Columns.Contains(col) Then
                    grid.Columns(col).DefaultCellStyle.ForeColor = ColorAccent
                    grid.Columns(col).DefaultCellStyle.Font = FontBold
                End If
            Next

            If grid.Columns.Contains("RechercheNormalisee") Then
                grid.Columns("RechercheNormalisee").Visible = False
            End If
        End Sub

        'Private Sub Grid_CurrentCellDirtyStateChanged(sender As Object, e As EventArgs)
        '    If grid.IsCurrentCellDirty Then grid.CommitEdit(DataGridViewDataErrorContexts.Commit)
        'End Sub

        'Private Sub Grid_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs)
        '    If e.RowIndex < 0 Or _majGrilleEnCours Then Return

        '    _majGrilleEnCours = True
        '    Try
        '        Dim row As DataRow = CType(grid.Rows(e.RowIndex).DataBoundItem, DataRowView).Row
        '        CalculerLigne(row)
        '    Finally
        '        _majGrilleEnCours = False
        '    End Try
        'End Sub

        Private Sub Grid_CurrentCellDirtyStateChanged(sender As Object, e As EventArgs)
            If grid IsNot Nothing AndAlso grid.IsCurrentCellDirty Then
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit)
            End If
        End Sub

        Private Sub Grid_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs)
            If _majGrilleEnCours OrElse e.RowIndex < 0 OrElse grid.DataSource Is Nothing Then
                Return
            End If

            Dim dt As DataTable = TryCast(grid.DataSource, DataTable)
            If dt Is Nothing OrElse e.RowIndex >= dt.Rows.Count Then
                Return
            End If

            dt.Rows(e.RowIndex)("RechercheNormalisee") = ConstruireTexteRecherche(dt.Rows(e.RowIndex))
            CalculerLigne(dt.Rows(e.RowIndex))
        End Sub

        Private Sub ChangerFiltres(sender As Object, e As EventArgs)
            If _chargementEnCours Then
                Return
            End If

            AppliquerFiltres()
        End Sub

        Private Sub ChargerCategoriesFiltre()
            If _categories Is Nothing Then
                Return
            End If

            Dim dt As DataTable = _categories.Copy()
            Dim ligneToutes As DataRow = dt.NewRow()
            ligneToutes("CategorieId") = DBNull.Value
            ligneToutes("NomCategorie") = "Toutes les catégories"
            dt.Rows.InsertAt(ligneToutes, 0)

            cmbCategorieFiltre.DataSource = dt
            cmbCategorieFiltre.DisplayMember = "NomCategorie"
            cmbCategorieFiltre.ValueMember = "CategorieId"
            If cmbCategorieFiltre.Items.Count > 0 Then
                cmbCategorieFiltre.SelectedIndex = 0
            End If
        End Sub

        Private Sub AppliquerFiltres()
            If _chargementEnCours Then
                Return
            End If

            Dim vue As DataView = TryCast(_bindingSource.DataSource, DataView)
            If vue Is Nothing Then
                Return
            End If

            Dim filtres As New List(Of String)()
            Dim recherche As String = NormaliserTexte(txtRecherche.Text)
            If recherche <> String.Empty Then
                filtres.Add(String.Format(CultureInfo.InvariantCulture, "[RechercheNormalisee] LIKE '%{0}%'", recherche.Replace("'", "''")))
            End If

            Dim filtreRapide As String = ConstruireExpressionFiltreRapide()
            If filtreRapide <> String.Empty Then
                filtres.Add(filtreRapide)
            End If

            Dim categorieId As Integer? = GetSelectedIntegerValueSafe(cmbCategorieFiltre, "CategorieId")
            If categorieId.HasValue Then
                filtres.Add(String.Format(CultureInfo.InvariantCulture, "[CategorieId] = {0}", categorieId.Value))
            End If

            vue.RowFilter = String.Join(" AND ", filtres)
            lblResultats.Text = vue.Count.ToString("N0", CultureInfo.InvariantCulture) & " produits"
        End Sub

        Private Function GetSelectedIntegerValueSafe(combo As ComboBox, columnName As String) As Integer?
            If combo Is Nothing Then
                Return Nothing
            End If

            If combo.SelectedValue IsNot Nothing AndAlso
               Not Convert.IsDBNull(combo.SelectedValue) AndAlso
               Not TypeOf combo.SelectedValue Is DataRowView Then
                Return SafeNullableInteger(combo.SelectedValue)
            End If

            Dim rowView As DataRowView = TryCast(combo.SelectedItem, DataRowView)
            If rowView Is Nothing OrElse rowView.Row Is Nothing OrElse rowView.Row.Table Is Nothing Then
                Return Nothing
            End If

            If Not rowView.Row.Table.Columns.Contains(columnName) Then
                Return Nothing
            End If

            Return SafeNullableInteger(rowView(columnName))
        End Function

        Private Function ConstruireExpressionFiltreRapide() As String
            Select Case Convert.ToString(cmbFiltreRapide.SelectedItem)
                Case "Sans prix d'achat"
                    Return "IsNull([PrixAchat], 0) <= 0 AND IsNull([PrixAchatOptionnel], 0) <= 0"
                Case "Sans prix de gros"
                    Return "IsNull([PrixGros], 0) <= 0"
                Case "Sans prix de détail"
                    Return "IsNull([PrixDetail], 0) <= 0"
                Case "Sans aucun prix de vente"
                    Return "IsNull([PrixGros], 0) <= 0 AND IsNull([PrixDemi], 0) <= 0 AND IsNull([PrixQuart], 0) <= 0 AND IsNull([PrixDetail], 0) <= 0 AND IsNull([PrixDouzaine], 0) <= 0"
                Case "Sans catégorie"
                    Return "IsNull([CategorieId], 0) = 0"
                Case "Sans unité principale"
                    Return "IsNull([UnitePrincipale], '') = ''"
                Case "Sans conversion d'unité"
                    Return "IsNull([ConversionUnite], 0) <= 0"
                Case "Stock égal à zéro"
                    Return "IsNull([QuantiteStock], 0) = 0"
                Case "Stock non initialisé"
                    Return "IsNull([QuantiteInitiale], 0) = 0 AND IsNull([QuantiteStock], 0) = 0"
                Case "Produits inactifs"
                    Return "[EstActif] = False"
                Case "Produits actifs"
                    Return "[EstActif] = True"
                Case "Avec incohérence de données"
                    Return "IsNull([Libelle], '') = '' OR IsNull([CategorieId], 0) = 0 OR IsNull([UnitePrincipale], '') = '' OR IsNull([ConversionUnite], 0) <= 0 OR (IsNull([PrixGros], 0) < 0 OR IsNull([PrixDetail], 0) < 0)"
                Case Else
                    Return String.Empty
            End Select
        End Function

        Private Function ConstruireTexteRecherche(row As DataRow) As String
            Dim morceaux As New List(Of String) From {
                SafeString(row("Libelle")),
                SafeString(row("CodeBarres")),
                SafeString(row("NomCategorie")),
                SafeString(row("CategorieId"))
            }
            Return NormaliserTexte(String.Join(" ", morceaux))
        End Function

        Private Function NormaliserTexte(texte As String) As String
            If String.IsNullOrWhiteSpace(texte) Then
                Return String.Empty
            End If

            Dim normalized As String = texte.Normalize(NormalizationForm.FormD)
            Dim builder As New StringBuilder()
            For Each caractere As Char In normalized
                Dim category As UnicodeCategory = CharUnicodeInfo.GetUnicodeCategory(caractere)
                If category <> UnicodeCategory.NonSpacingMark Then
                    builder.Append(Char.ToUpperInvariant(caractere))
                End If
            Next

            Return builder.ToString().Normalize(NormalizationForm.FormC)
        End Function

        'Private Sub CalculerLigne(row As DataRow)
        '    Dim qteP As Decimal = SafeDecimal(row("QuantitePrincipale"))
        '    Dim qteS As Decimal = SafeDecimal(row("QuantiteSecondaire"))
        '    Dim conv As Decimal = SafeDecimal(row("ConversionUnite"))

        '    Dim qteBase As Decimal = CalculerQuantiteBase(qteP, qteS, conv)
        '    row("QuantiteInitiale") = qteBase

        '    'If conv > 0 Then
        '    '    row("EquivalentSecondaire") = qteBase * conv
        '    'Else
        '    '    row("EquivalentSecondaire") = 0
        '    'End If

        '    Dim uP As String = SafeString(row("UnitePrincipale"))
        '    Dim uS As String = SafeString(row("UniteSecondaire"))
        '    'row("ResumeQuantite") = $"{qteP} {uP}" & If(conv > 0, $" + {qteS} {uS}", "")


        '    row("ResumeQuantite") = qteP.ToString("N0") & " " & uP & " + " & qteS.ToString("N0") & " " & uS
        '    row("StockActuelLisible") = "Calculé..."
        '    row("StockApresLisible") = "Prêt"
        'End Sub


        Private Sub CalculerLigne(row As DataRow)
            If row Is Nothing Then
                Return
            End If

            _majGrilleEnCours = True
            Try
                Dim conversion As Decimal = Math.Max(1D, SafeDecimal(row("ConversionUnite")))
                Dim quantitePrincipale As Decimal = SafeDecimal(row("QuantitePrincipale"))
                Dim quantiteSecondaire As Decimal = SafeDecimal(row("QuantiteSecondaire"))
                Dim stockActuelBase As Decimal = SafeDecimal(row("QuantiteStock"))
                Dim totalBase As Decimal = CalculerQuantiteBase(quantitePrincipale, quantiteSecondaire, conversion)
                Dim unitePrincipale As String = If(SafeString(row("UnitePrincipale")) = String.Empty, "Unité", SafeString(row("UnitePrincipale")))
                Dim uniteSecondaire As String = If(SafeString(row("UniteSecondaire")) = String.Empty, "pièce", SafeString(row("UniteSecondaire")))

                row("QuantiteInitiale") = totalBase
                row("EquivalentSecondaire") = totalBase
                row("StockActuelLisible") = FormaterStock(stockActuelBase, conversion, unitePrincipale, uniteSecondaire)
                row("StockApresLisible") = FormaterStock(stockActuelBase + totalBase, conversion, unitePrincipale, uniteSecondaire)
                row("ResumeQuantite") = quantitePrincipale.ToString("N0") & " " & unitePrincipale & " + " & quantiteSecondaire.ToString("N0") & " " & uniteSecondaire
            Finally
                _majGrilleEnCours = False
            End Try
        End Sub
        Private Function CalculerQuantiteBase(quantitePrincipale As Decimal, quantiteSecondaire As Decimal, conversion As Decimal) As Decimal
            Dim conversionValide As Decimal = If(conversion > 0D, conversion, 1D)
            Return (quantitePrincipale * conversionValide) + quantiteSecondaire
        End Function

        Private Function FormaterStock(stockBase As Decimal, conversion As Decimal, unitePrincipale As String, uniteSecondaire As String) As String
            Dim conversionValide As Decimal = If(conversion > 0D, conversion, 1D)
            Dim principal As Decimal = Decimal.Floor(stockBase / conversionValide)
            Dim secondaire As Decimal = stockBase - (principal * conversionValide)
            Return principal.ToString("N0") & " " & unitePrincipale & " + " & secondaire.ToString("N0") & " " & uniteSecondaire & " (" & stockBase.ToString("N0") & " " & uniteSecondaire & ")"
        End Function

        'Private Sub EnregistrerStockInitial(sender As Object, e As EventArgs)
        '    If MessageBox.Show("Voulez-vous enregistrer ces modifications ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return

        '    Try
        '        Me.Cursor = Cursors.WaitCursor
        '        Dim dt As DataTable = CType(grid.DataSource, DataTable)
        '        Dim count As Integer = 0

        '        For Each row As DataRow In dt.Rows
        '            Dim p As New Produit() With {
        '                .ProduitId = SafeInteger(row("ProduitId")),
        '                .Libelle = SafeString(row("Libelle")),
        '                .PrixAchat = SafeDecimal(row("PrixAchatOptionnel")),
        '                .PrixGros = SafeDecimal(row("PrixGros")),
        '                .PrixDemi = SafeDecimal(row("PrixDemi")),
        '                .PrixQuart = SafeDecimal(row("PrixQuart")),
        '                .PrixDetail = SafeDecimal(row("PrixDetail")),
        '                .PrixDouzaine = SafeDecimal(row("PrixDouzaine")),
        '                .EstActif = SafeBoolean(row("EstActif"), True)
        '            }

        '            Dim prodService As New ProduitService(New ProduitRepository(New DAL(ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString)))
        '            prodService.MettreAJour(p)

        '            Dim qteInit As Decimal = SafeDecimal(row("QuantiteInitiale"))
        '            If qteInit > 0 Then
        '                Dim stockService As New StockService(New DAL(ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString))
        '                stockService.EnregistrerEntree(p.ProduitId, qteInit, SafeString(row("UnitePrincipale")), SafeString(row("CodeBarres")), "INITIALISATION TECHNIQUE", SessionUtilisateur.UtilisateurId, p.PrixAchat)
        '            End If

        '            count += 1
        '        Next

        '        MessageBox.Show($"{count} produits mis à jour.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information)
        '        Recharger(Nothing, EventArgs.Empty)
        '    Catch ex As Exception
        '        _log.Error("FormulaireStockInitialTechnique", "Enregistrer", "Erreur.", ex)
        '        MessageBox.Show("Erreur : " & ex.Message)
        '    Finally
        '        Me.Cursor = Cursors.Default
        '    End Try
        'End Sub

        Private Sub EnregistrerStockInitial(sender As Object, e As EventArgs)
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim produitService As New ProduitService(New ProduitRepository(dal))
            Dim stockService As New StockService(dal)
            Dim typeService As New TypeVenteProduitService()
            Dim lignesTraitees As Integer = 0

            Try
                For Each dgRow As DataGridViewRow In grid.Rows
                    If dgRow.IsNewRow Then
                        Continue For
                    End If

                    Dim quantitePrincipale As Decimal = SafeDecimal(dgRow.Cells("QuantitePrincipale").Value)
                    Dim quantiteSecondaire As Decimal = SafeDecimal(dgRow.Cells("QuantiteSecondaire").Value)
                    Dim quantiteInitiale As Decimal = CalculerQuantiteBase(quantitePrincipale, quantiteSecondaire, SafeDecimal(dgRow.Cells("ConversionUnite").Value))
                    Dim libelle As String = SafeString(dgRow.Cells("Libelle").Value)
                    If quantiteInitiale <= 0D AndAlso libelle = String.Empty Then
                        Continue For
                    End If
                    If libelle = String.Empty Then
                        Continue For
                    End If

                    Dim produitId As Integer = SafeInteger(dgRow.Cells("ProduitId").Value)
                    Dim prixAchatOptionnel As Decimal = SafeDecimal(dgRow.Cells("PrixAchatOptionnel").Value)
                    Dim prixAchatExistant As Decimal = SafeDecimal(dgRow.Cells("PrixAchat").Value)
                    If prixAchatOptionnel <= 0D Then
                        prixAchatOptionnel = prixAchatExistant
                    End If

                    Dim produit As New Produit With {
                        .ProduitId = produitId,
                        .CodeBarres = SafeString(dgRow.Cells("CodeBarres").Value),
                        .Libelle = libelle,
                        .PrixAchat = prixAchatOptionnel,
                        .PrixGros = SafeDecimal(dgRow.Cells("PrixGros").Value),
                        .PrixDemi = SafeDecimal(dgRow.Cells("PrixDemi").Value),
                        .PrixQuart = SafeDecimal(dgRow.Cells("PrixQuart").Value),
                        .PrixDetail = SafeDecimal(dgRow.Cells("PrixDetail").Value),
                        .PrixDouzaine = SafeDecimal(dgRow.Cells("PrixDouzaine").Value),
                        .PrixSpecial = SafeDecimal(dgRow.Cells("PrixSpecial").Value),
                        .CoefficientGros = SafeDecimal(dgRow.Cells("CoefficientGros").Value),
                        .SeuilCritique = SafeDecimal(dgRow.Cells("SeuilCritique").Value),
                        .DateExpiration = SafeDate(dgRow.Cells("DateExpiration").Value),
                        .CategorieId = SafeNullableInteger(dgRow.Cells("CategorieId").Value),
                        .UnitePrincipale = If(SafeString(dgRow.Cells("UnitePrincipale").Value) = String.Empty, "Carton", SafeString(dgRow.Cells("UnitePrincipale").Value)),
                        .UniteSecondaire = If(SafeString(dgRow.Cells("UniteSecondaire").Value) = String.Empty, "Piece", SafeString(dgRow.Cells("UniteSecondaire").Value)),
                        .ConversionUnite = Math.Max(1D, SafeDecimal(dgRow.Cells("ConversionUnite").Value)),
                        .EstActif = SafeBoolean(dgRow.Cells("EstActif").Value, True),
                        .VenteDetail = SafeDecimal(dgRow.Cells("PrixDetail").Value) > 0D,
                        .VenteDemi = SafeDecimal(dgRow.Cells("PrixDemi").Value) > 0D,
                        .VenteDouzaine = SafeDecimal(dgRow.Cells("PrixDouzaine").Value) > 0D,
                        .VenteGros = SafeDecimal(dgRow.Cells("PrixGros").Value) > 0D
                    }

                    If produit.PrixAchat < 0D Then
                        produit.PrixAchat = 0D
                    End If

                    If produitId > 0 Then
                        produitService.MettreAJour(produit)
                    Else
                        produitId = produitService.Ajouter(produit)
                    End If

                    If quantiteInitiale > 0D Then
                        Dim uniteSaisie As String = If(String.IsNullOrWhiteSpace(produit.UniteSecondaire), produit.UnitePrincipale, produit.UniteSecondaire)
                        stockService.EnregistrerEntree(produitId, quantiteInitiale, uniteSaisie, SafeString(dgRow.Cells("CodeBarres").Value), "Stock initial technique", SessionUtilisateur.UtilisateurId, produit.PrixAchat)
                    End If

                    Dim typesTexte As String = SafeString(dgRow.Cells("TypesPersonnalises").Value)
                    For Each dto As TypeVenteProduitDTO In ParserTypes(typesTexte, produitId)
                        typeService.Ajouter(dto)
                    Next

                    lignesTraitees += 1
                Next

                MessageBox.Show(lignesTraitees.ToString(CultureInfo.InvariantCulture) & " ligne(s) enregistrée(s).")
                Recharger(Nothing, EventArgs.Empty)
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireStockInitialTechnique", "EnregistrerStockInitial", "Enregistrement du stock initial impossible.", ex)
                MessageBox.Show("Impossible d'enregistrer le stock initial : " & ex.Message)
            End Try
        End Sub

        Private Function ParserTypes(texte As String, produitId As Integer) As IEnumerable(Of TypeVenteProduitDTO)
            Dim resultat As New List(Of TypeVenteProduitDTO)()
            If String.IsNullOrWhiteSpace(texte) Then
                Return resultat
            End If

            Dim blocs As String() = texte.Split(";"c)
            For Each bloc As String In blocs
                Dim morceaux As String() = bloc.Split("|"c)
                If morceaux.Length < 5 Then
                    Continue For
                End If

                Dim nom As String = morceaux(0).Trim()
                Dim quantite As Decimal = SafeDecimal(morceaux(1))
                Dim modePrix As String = morceaux(2).Trim().ToUpperInvariant()
                Dim coefficient As Decimal = SafeDecimal(morceaux(3))
                Dim prixVente As Decimal = SafeDecimal(morceaux(4))
                Dim actif As Boolean = True
                If morceaux.Length >= 6 Then
                    actif = SafeBoolean(morceaux(5), True)
                End If

                If nom = String.Empty OrElse quantite <= 0D OrElse prixVente <= 0D Then
                    Continue For
                End If

                Dim dto As New TypeVenteProduitDTO With {
                    .ProduitId = produitId,
                    .Nom = nom,
                    .QuantiteEquivalent = quantite,
                    .ModePrix = If(modePrix = String.Empty, "FIXE", modePrix),
                    .PrixVente = prixVente,
                    .Actif = actif,
                    .ModifiePar = If(String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur), "SYSTEM", SessionUtilisateur.NomUtilisateur)
                }

                If String.Equals(dto.ModePrix, "COEFFICIENT", StringComparison.OrdinalIgnoreCase) AndAlso coefficient > 0D Then
                    dto.Coefficient = coefficient
                End If

                resultat.Add(dto)
            Next

            Return resultat
        End Function
        'Private Function SafeNullableInteger(value As Object) As Integer?
        '    Dim resultat As Integer = SafeInteger(value)
        '    If resultat <= 0 Then
        '        Return Nothing
        '    End If
        '    Return resultat
        'End Function

        'Private Function SafeDate(value As Object) As Date?
        '    If value Is Nothing OrElse Convert.IsDBNull(value) Then
        '        Return Nothing
        '    End If
        '    Dim resultat As Date
        '    If Date.TryParse(Convert.ToString(value), resultat) Then
        '        Return resultat
        '    End If
        '    Return Nothing
        'End Function
        'Private Function SafeDecimal(value As Object) As Decimal
        '    If value Is Nothing OrElse DBNull.Value.Equals(value) Then Return 0D
        '    Dim res As Decimal
        '    If Decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, res) Then Return res
        '    Return 0D
        'End Function

        'Private Function SafeInteger(value As Object) As Integer
        '    If value Is Nothing OrElse DBNull.Value.Equals(value) Then Return 0
        '    Dim res As Integer
        '    If Integer.TryParse(value.ToString(), res) Then Return res
        '    Return 0
        'End Function

        'Private Function SafeString(value As Object) As String
        '    Return If(value Is Nothing OrElse DBNull.Value.Equals(value), String.Empty, value.ToString())
        'End Function

        'Private Function SafeBoolean(value As Object, def As Boolean) As Boolean
        '    If value Is Nothing OrElse DBNull.Value.Equals(value) Then Return def
        '    Dim res As Boolean
        '    If Boolean.TryParse(value.ToString(), res) Then Return res
        '    Return def
        'End Function






        Private Function SafeString(value As Object) As String
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return String.Empty
            End If
            If TypeOf value Is DataRowView Then
                Return String.Empty
            End If
            Return Convert.ToString(value).Trim()
        End Function

        Private Function SafeDecimal(value As Object) As Decimal
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return 0D
            End If
            If TypeOf value Is DataRowView Then
                Return 0D
            End If

            Dim texte As String = Convert.ToString(value).Trim().Replace(",", ".")
            Dim resultat As Decimal
            If Decimal.TryParse(texte, NumberStyles.Any, CultureInfo.InvariantCulture, resultat) Then
                Return resultat
            End If
            If Decimal.TryParse(texte, NumberStyles.Any, CultureInfo.CurrentCulture, resultat) Then
                Return resultat
            End If
            Return 0D
        End Function

        Private Function SafeInteger(value As Object) As Integer
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return 0
            End If
            If TypeOf value Is DataRowView Then
                Return 0
            End If
            Dim resultat As Integer
            If Integer.TryParse(Convert.ToString(value), resultat) Then
                Return resultat
            End If
            Return 0
        End Function

        Private Function SafeNullableInteger(value As Object) As Integer?
            Dim resultat As Integer = SafeInteger(value)
            If resultat <= 0 Then
                Return Nothing
            End If
            Return resultat
        End Function

        Private Function SafeDate(value As Object) As Date?
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return Nothing
            End If
            If TypeOf value Is DataRowView Then
                Return Nothing
            End If
            Dim resultat As Date
            If Date.TryParse(Convert.ToString(value), resultat) Then
                Return resultat
            End If
            Return Nothing
        End Function

        Private Function SafeBoolean(value As Object, defaultValue As Boolean) As Boolean
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return defaultValue
            End If
            If TypeOf value Is DataRowView Then
                Return defaultValue
            End If

            Dim texte As String = Convert.ToString(value).Trim()
            Dim resultat As Boolean
            If Boolean.TryParse(texte, resultat) Then
                Return resultat
            End If

            If String.Equals(texte, "1", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            If String.Equals(texte, "0", StringComparison.OrdinalIgnoreCase) Then
                Return False
            End If
            Return defaultValue
        End Function

    End Class
End Namespace
