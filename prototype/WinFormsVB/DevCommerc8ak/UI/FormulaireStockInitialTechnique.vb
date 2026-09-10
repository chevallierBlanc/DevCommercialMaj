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
        Private cmbModeOperation As ComboBox
        Private lblResultats As Label
        Private lblAideSaisie As Label

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
            rootLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 130))

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
                .Text = "Initialisation des quantités réelles au démarrage. Les ventes antérieures se reprennent dans Initialisation des ventes.",
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
            contentLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 72))
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
            cmbModeOperation = New ComboBox() With {.Width = 240, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = FontMain}
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
            cmbModeOperation.Items.AddRange(New Object() {
                "REMPLACER / CORRIGER STOCK INITIAL",
                "AJOUTER PRODUIT OMIS"
            })
            cmbModeOperation.SelectedIndex = 0
            pnlFiltres.Controls.AddRange({
                New Label() With {.Text = "Recherche", .AutoSize = True, .Margin = New Padding(0, 10, 6, 0), .ForeColor = ColorTextSecondary},
                txtRecherche,
                New Label() With {.Text = "Filtre", .AutoSize = True, .Margin = New Padding(14, 10, 6, 0), .ForeColor = ColorTextSecondary},
                cmbFiltreRapide,
                New Label() With {.Text = "Catégorie", .AutoSize = True, .Margin = New Padding(14, 10, 6, 0), .ForeColor = ColorTextSecondary},
                cmbCategorieFiltre,
                New Label() With {.Text = "Mode", .AutoSize = True, .Margin = New Padding(14, 10, 6, 0), .ForeColor = ColorTextSecondary},
                cmbModeOperation,
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

            lblAideSaisie = New Label() With {
                .Text = "Aide à la saisie : QTÉ(P)=unité principale (carton/sac/bidon). QTÉ(S)=complément secondaire (pièce/sachet). STOCK BASE=quantité physique normalisée. Le mode correction vise un stock final, il n'additionne pas l'ancien stock.",
                .AutoSize = False,
                .Location = New Point(530, 18),
                .Size = New Size(820, 78),
                .ForeColor = ColorTextSecondary,
                .Font = FontMain,
                .BackColor = Color.FromArgb(248, 250, 252),
                .Padding = New Padding(10),
                .TextAlign = ContentAlignment.MiddleLeft
            }

            pnlFooter.Controls.AddRange({btnRecharger, btnEnregistrer, lblAideSaisie})
            rootLayout.Controls.Add(pnlFooter, 0, 2)

            Me.Controls.Add(rootLayout)

            AddHandler btnRecharger.Click, AddressOf Recharger
            AddHandler btnEnregistrer.Click, AddressOf EnregistrerStockInitial
            AddHandler grid.CellValueChanged, AddressOf Grid_CellValueChanged
            AddHandler grid.CurrentCellDirtyStateChanged, AddressOf Grid_CurrentCellDirtyStateChanged
            AddHandler grid.DataError, AddressOf Grid_DataError
            AddHandler txtRecherche.TextChanged, AddressOf ChangerFiltres
            AddHandler cmbFiltreRapide.SelectedIndexChanged, AddressOf ChangerFiltres
            AddHandler cmbCategorieFiltre.SelectedIndexChanged, AddressOf ChangerFiltres
            AddHandler cmbModeOperation.SelectedIndexChanged, AddressOf ModeOperationChanged
            AddHandler grid.KeyDown, AddressOf Grid_KeyDown
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
            _service.VerifierAccesStockInitialTechnique()
            AuditActionService.Enregistrer("SuperAdmin", "Ouverture stock initial", "Ouverture de l'interface stock initial technique.")
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
                    NormaliserColonnesStock(row)
                    row("RechercheNormalisee") = ConstruireTexteRecherche(row)
                    CalculerLigne(row)
                Next
                dt.AcceptChanges()

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
                {"TypeGestionStock", "TYPE STOCK"},
                {"UniteMesureStock", "UNITÉ MESURE"},
                {"ContenuUnitePrincipale", "CONTENU (P)"},
                {"ContenuUniteSecondaire", "CONTENU (S)"},
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

            RemplacerParCombo("TypeGestionStock", New String() {"UNITE", "MESURE"})
            RemplacerParCombo("UniteMesureStock", New String() {"PIECE", "KG", "G", "L", "ML", "M", "CM", "M2", "M3"})

            Dim readOnlyCols As String() = {"QuantiteInitiale", "EquivalentSecondaire", "StockActuelLisible", "StockApresLisible", "ResumeQuantite"}
            For Each col As String In readOnlyCols
                If grid.Columns.Contains(col) Then
                    grid.Columns(col).ReadOnly = True
                    grid.Columns(col).DefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250)
                End If
            Next

            Dim inputCols As String() = {"QuantitePrincipale", "QuantiteSecondaire", "PrixAchatOptionnel", "PrixGros", "PrixDemi", "PrixQuart", "PrixDetail", "PrixDouzaine", "NomCategorie", "Libelle", "UnitePrincipale", "UniteSecondaire", "ConversionUnite", "TypeGestionStock", "UniteMesureStock", "ContenuUnitePrincipale", "ContenuUniteSecondaire"}
            For Each col As String In inputCols
                If grid.Columns.Contains(col) Then
                    grid.Columns(col).DefaultCellStyle.ForeColor = ColorAccent
                    grid.Columns(col).DefaultCellStyle.Font = FontBold
                End If
            Next

            ColorerColonnesMetier()

            If grid.Columns.Contains("RechercheNormalisee") Then
                grid.Columns("RechercheNormalisee").Visible = False
            End If

            For Each column As DataGridViewColumn In grid.Columns
                column.Frozen = False
            Next

            If grid.Columns.Contains("CodeBarres") Then
                grid.Columns("CodeBarres").DisplayIndex = 0
                grid.Columns("CodeBarres").Frozen = True
                grid.Columns("CodeBarres").Width = Math.Max(grid.Columns("CodeBarres").Width, 120)
            End If

            If grid.Columns.Contains("Libelle") Then
                grid.Columns("Libelle").DisplayIndex = If(grid.Columns.Contains("CodeBarres"), 1, 0)
                grid.Columns("Libelle").Frozen = True
                grid.Columns("Libelle").Width = Math.Max(grid.Columns("Libelle").Width, 240)
                grid.Columns("Libelle").ToolTipText = "Produit"
            End If
        End Sub

        Private Sub RemplacerParCombo(columnName As String, values As String())
            If Not grid.Columns.Contains(columnName) OrElse TypeOf grid.Columns(columnName) Is DataGridViewComboBoxColumn Then
                Return
            End If

            Dim ancienne As DataGridViewColumn = grid.Columns(columnName)
            Dim index As Integer = ancienne.Index
            Dim displayIndex As Integer = ancienne.DisplayIndex
            Dim header As String = ancienne.HeaderText
            Dim width As Integer = ancienne.Width

            grid.Columns.Remove(ancienne)
            Dim combo As New DataGridViewComboBoxColumn() With {
                .Name = columnName,
                .DataPropertyName = columnName,
                .HeaderText = header,
                .Width = Math.Max(width, 105),
                .FlatStyle = FlatStyle.Flat,
                .DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
            }
            combo.Items.AddRange(values.Cast(Of Object).ToArray())
            grid.Columns.Insert(index, combo)
            grid.Columns(columnName).DisplayIndex = displayIndex
        End Sub

        Private Sub ColorerColonnesMetier()
            Dim calcCols As String() = {"QuantiteInitiale", "EquivalentSecondaire", "StockActuelLisible", "StockApresLisible", "ResumeQuantite"}
            Dim uniteCols As String() = {"ConversionUnite", "UniteSecondaire"}
            Dim mesureCols As String() = {"TypeGestionStock", "UniteMesureStock", "ContenuUnitePrincipale", "ContenuUniteSecondaire"}

            For Each col As String In calcCols
                If grid.Columns.Contains(col) Then
                    grid.Columns(col).DefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250)
                    grid.Columns(col).ToolTipText = "Champ calculé en lecture seule."
                End If
            Next
            For Each col As String In uniteCols
                If grid.Columns.Contains(col) Then
                    grid.Columns(col).DefaultCellStyle.BackColor = Color.FromArgb(239, 246, 255)
                    grid.Columns(col).ToolTipText = "Paramètre utilisé par les produits gérés en UNITE."
                End If
            Next
            For Each col As String In mesureCols
                If grid.Columns.Contains(col) Then
                    grid.Columns(col).DefaultCellStyle.BackColor = Color.FromArgb(240, 253, 244)
                    grid.Columns(col).ToolTipText = "Paramètre utilisé par les produits gérés en MESURE."
                End If
            Next
            If grid.Columns.Contains("QuantitePrincipale") Then grid.Columns("QuantitePrincipale").ToolTipText = "Quantité saisie en unité principale : carton, sac, bidon..."
            If grid.Columns.Contains("QuantiteSecondaire") Then grid.Columns("QuantiteSecondaire").ToolTipText = "Complément en unité secondaire : pièce, sachet..."
            If grid.Columns.Contains("QuantiteInitiale") Then grid.Columns("QuantiteInitiale").ToolTipText = "Quantité physique normalisée réellement utilisée par le stock."
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

            Dim rowView As DataRowView = TryCast(grid.Rows(e.RowIndex).DataBoundItem, DataRowView)
            If rowView Is Nothing OrElse rowView.Row Is Nothing Then
                Return
            End If

            rowView.Row("RechercheNormalisee") = ConstruireTexteRecherche(rowView.Row)
            CalculerLigne(rowView.Row)
        End Sub

        Private Sub ChangerFiltres(sender As Object, e As EventArgs)
            If _chargementEnCours Then
                Return
            End If

            AppliquerFiltres()
        End Sub

        Private Sub ModeOperationChanged(sender As Object, e As EventArgs)
            If _sourceTable Is Nothing Then
                Return
            End If
            For Each row As DataRow In _sourceTable.Rows
                CalculerLigne(row)
            Next
        End Sub

        Private Sub Grid_DataError(sender As Object, e As DataGridViewDataErrorEventArgs)
            e.ThrowException = False
        End Sub

        Private Sub Grid_KeyDown(sender As Object, e As KeyEventArgs)
            If e.Control AndAlso e.KeyCode = Keys.V Then
                e.SuppressKeyPress = True
                CollerDepuisPressePapiers()
            End If
        End Sub

        Private Sub CollerDepuisPressePapiers()
            If grid.CurrentCell Is Nothing OrElse Not Clipboard.ContainsText() Then
                Return
            End If

            Dim texte As String = Clipboard.GetText()
            Dim lignes As String() = texte.Replace(Environment.NewLine, ChrW(10)).Replace(ChrW(13), ChrW(10)).TrimEnd(ChrW(10)).Split(ChrW(10))
            Dim startRow As Integer = grid.CurrentCell.RowIndex
            Dim startCol As Integer = grid.CurrentCell.ColumnIndex

            _majGrilleEnCours = True
            Try
                For i As Integer = 0 To lignes.Length - 1
                    Dim rowIndex As Integer = startRow + i
                    If rowIndex >= grid.Rows.Count OrElse grid.Rows(rowIndex).IsNewRow Then Exit For
                    Dim cellules As String() = lignes(i).Split(ChrW(9))
                    For j As Integer = 0 To cellules.Length - 1
                        Dim colIndex As Integer = startCol + j
                        If colIndex >= grid.Columns.Count Then Exit For
                        Dim col As DataGridViewColumn = grid.Columns(colIndex)
                        If Not ColonneCollable(col.Name) Then Continue For
                        If col.ReadOnly OrElse Not col.Visible Then Continue For
                        grid.Rows(rowIndex).Cells(colIndex).Value = ConvertirValeurCollee(col.Name, cellules(j))
                    Next

                    Dim rowView As DataRowView = TryCast(grid.Rows(rowIndex).DataBoundItem, DataRowView)
                    If rowView IsNot Nothing AndAlso rowView.Row IsNot Nothing Then
                        rowView.Row("RechercheNormalisee") = ConstruireTexteRecherche(rowView.Row)
                        CalculerLigne(rowView.Row)
                    End If
                Next
            Catch ex As Exception
                _log.Warn("FormulaireStockInitialTechnique", "CollerDepuisPressePapiers", "Collage refusé : " & ex.Message)
                MessageBox.Show("Collage refusé : " & ex.Message, "Valeur invalide", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Finally
                _majGrilleEnCours = False
            End Try
        End Sub

        Private Function ColonneCollable(columnName As String) As Boolean
            Dim autorisees As String() = {"QuantitePrincipale", "QuantiteSecondaire", "PrixAchatOptionnel", "PrixGros", "PrixDemi", "PrixQuart", "PrixDetail", "PrixDouzaine", "PrixSpecial", "SeuilCritique", "ConversionUnite", "ContenuUnitePrincipale", "ContenuUniteSecondaire", "TypeGestionStock", "UniteMesureStock", "UnitePrincipale", "UniteSecondaire"}
            Return autorisees.Contains(columnName)
        End Function

        Private Function ConvertirValeurCollee(columnName As String, value As String) As Object
            If columnName = "TypeGestionStock" Then
                Dim v As String = If(value, String.Empty).Trim().ToUpperInvariant()
                Return If(v = "MESURE", "MESURE", "UNITE")
            End If
            If columnName = "UniteMesureStock" Then
                Return If(String.IsNullOrWhiteSpace(value), "PIECE", value.Trim().ToUpperInvariant())
            End If

            Dim colonnesDecimales As String() = {"QuantitePrincipale", "QuantiteSecondaire", "PrixAchatOptionnel", "PrixGros", "PrixDemi", "PrixQuart", "PrixDetail", "PrixDouzaine", "PrixSpecial", "SeuilCritique", "ConversionUnite", "ContenuUnitePrincipale", "ContenuUniteSecondaire"}
            If colonnesDecimales.Contains(columnName) Then
                Dim resultat As Decimal
                Dim texte As String = If(value, String.Empty).Trim().Replace(",", ".")
                If Decimal.TryParse(texte, NumberStyles.Any, CultureInfo.InvariantCulture, resultat) AndAlso resultat >= 0D Then
                    Return resultat
                End If
                Throw New FormatException("Valeur décimale invalide pour " & columnName & ".")
            End If

            Return If(value, String.Empty).Trim()
        End Function

        Private Function CellValue(row As DataGridViewRow, columnName As String) As Object
            If row Is Nothing OrElse Not grid.Columns.Contains(columnName) Then
                Return DBNull.Value
            End If
            Return row.Cells(columnName).Value
        End Function

        Private Function EstLigneAEnregistrer(row As DataGridViewRow) As Boolean
            If row Is Nothing OrElse row.IsNewRow Then
                Return False
            End If
            If StockSaisi(row) Then
                Return True
            End If

            Dim rowView As DataRowView = TryCast(row.DataBoundItem, DataRowView)
            Return rowView IsNot Nothing AndAlso rowView.Row IsNot Nothing AndAlso rowView.Row.RowState <> DataRowState.Unchanged
        End Function

        Private Function StockSaisi(row As DataGridViewRow) As Boolean
            If row Is Nothing Then
                Return False
            End If
            Return SafeDecimal(CellValue(row, "QuantitePrincipale")) > 0D OrElse SafeDecimal(CellValue(row, "QuantiteSecondaire")) > 0D
        End Function

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

        Private Sub CalculerLigne(row As DataRow)
            If row Is Nothing Then
                Return
            End If

            _majGrilleEnCours = True
            Try
                NormaliserColonnesStock(row)
                Dim quantitePrincipale As Decimal = SafeDecimal(row("QuantitePrincipale"))
                Dim quantiteSecondaire As Decimal = SafeDecimal(row("QuantiteSecondaire"))
                Dim stockActuelBase As Decimal = SafeDecimal(row("QuantiteStock"))
                Dim totalBase As Decimal = CalculerQuantiteBaseInitiale(row, quantitePrincipale, quantiteSecondaire)
                Dim stockApresBase As Decimal = If(ModeAjoutOmission(), stockActuelBase + totalBase, totalBase)
                Dim unitePrincipale As String = If(SafeString(row("UnitePrincipale")) = String.Empty, "Unité", SafeString(row("UnitePrincipale")))
                Dim uniteBase As String = ObtenirUniteBase(row)

                row("QuantiteInitiale") = totalBase
                row("EquivalentSecondaire") = totalBase
                row("StockActuelLisible") = FormaterStockInitial(row, stockActuelBase)
                row("StockApresLisible") = FormaterStockInitial(row, stockApresBase)
                row("ResumeQuantite") = ConstruireResumeSaisie(row, quantitePrincipale, quantiteSecondaire, totalBase, unitePrincipale, uniteBase)
            Catch ex As Exception
                row("QuantiteInitiale") = 0D
                row("EquivalentSecondaire") = 0D
                row("StockApresLisible") = "Paramètres invalides"
                row("ResumeQuantite") = ex.Message
            Finally
                _majGrilleEnCours = False
            End Try
        End Sub
        Private Sub NormaliserColonnesStock(row As DataRow)
            If row.Table.Columns.Contains("TypeGestionStock") AndAlso SafeString(row("TypeGestionStock")) = String.Empty Then row("TypeGestionStock") = "UNITE"
            If row.Table.Columns.Contains("UniteMesureStock") AndAlso SafeString(row("UniteMesureStock")) = String.Empty Then row("UniteMesureStock") = "PIECE"
            If row.Table.Columns.Contains("ConversionUnite") AndAlso SafeDecimal(row("ConversionUnite")) <= 0D Then row("ConversionUnite") = 1D
            If row.Table.Columns.Contains("ContenuUnitePrincipale") AndAlso SafeDecimal(row("ContenuUnitePrincipale")) <= 0D Then row("ContenuUnitePrincipale") = SafeDecimal(row("ConversionUnite"))
        End Sub

        Private Function CalculerQuantiteBaseInitiale(row As DataRow, quantitePrincipale As Decimal, quantiteSecondaire As Decimal) As Decimal
            If quantitePrincipale < 0D OrElse quantiteSecondaire < 0D Then
                Throw New InvalidOperationException("Les quantités ne peuvent pas être négatives.")
            End If

            Dim typeGestion As String = StockUnitConversionService.NormaliserTypeGestionStock(SafeString(row("TypeGestionStock")))
            If StockUnitConversionService.EstGestionMesuree(typeGestion) Then
                Dim contenuPrincipal As Decimal = SafeDecimal(row("ContenuUnitePrincipale"))
                Dim contenuSecondaire As Decimal = SafeDecimal(row("ContenuUniteSecondaire"))
                If contenuPrincipal <= 0D Then
                    Throw New InvalidOperationException("Le contenu de l'unité principale est obligatoire pour un produit MESURE.")
                End If
                Return (quantitePrincipale * contenuPrincipal) + (quantiteSecondaire * If(contenuSecondaire > 0D, contenuSecondaire, 1D))
            End If

            Dim conversion As Decimal = Math.Max(1D, SafeDecimal(row("ConversionUnite")))
            Return (quantitePrincipale * conversion) + quantiteSecondaire
        End Function

        Private Function ObtenirUniteBase(row As DataRow) As String
            Dim typeGestion As String = StockUnitConversionService.NormaliserTypeGestionStock(SafeString(row("TypeGestionStock")))
            If StockUnitConversionService.EstGestionMesuree(typeGestion) Then
                Dim uniteMesure As String = SafeString(row("UniteMesureStock"))
                Return If(uniteMesure = String.Empty, "unité", uniteMesure)
            End If
            Dim uniteSecondaire As String = SafeString(row("UniteSecondaire"))
            Return If(uniteSecondaire = String.Empty, "pièce", uniteSecondaire)
        End Function

        Private Function FormaterStockInitial(row As DataRow, stockBase As Decimal) As String
            Return FormatageGlobal.FormatStockSelonGestion(stockBase,
                                                           SafeDecimal(row("ConversionUnite")),
                                                           SafeString(row("UnitePrincipale")),
                                                           SafeString(row("UniteSecondaire")),
                                                           SafeString(row("TypeGestionStock")),
                                                           SafeString(row("UniteMesureStock")),
                                                           SafeDecimal(row("ContenuUnitePrincipale")),
                                                           SafeDecimal(row("ContenuUniteSecondaire")))
        End Function

        Private Function ConstruireResumeSaisie(row As DataRow, quantitePrincipale As Decimal, quantiteSecondaire As Decimal, totalBase As Decimal, unitePrincipale As String, uniteBase As String) As String
            Dim parties As New List(Of String)()
            If quantitePrincipale > 0D Then parties.Add(FormaterDecimal(quantitePrincipale) & " " & unitePrincipale)
            If quantiteSecondaire > 0D Then
                Dim uniteSecondaire As String = SafeString(row("UniteSecondaire"))
                If uniteSecondaire = String.Empty Then uniteSecondaire = uniteBase
                parties.Add(FormaterDecimal(quantiteSecondaire) & " " & uniteSecondaire)
            End If
            If parties.Count = 0 Then parties.Add("0 " & uniteBase)
            Return String.Join(" + ", parties) & " = " & FormaterDecimal(totalBase) & " " & uniteBase
        End Function

        Private Function FormaterDecimal(value As Decimal) As String
            If value = Decimal.Truncate(value) Then
                Return value.ToString("N0", CultureInfo.CurrentCulture)
            End If
            Return value.ToString("N3", CultureInfo.CurrentCulture).TrimEnd("0"c).TrimEnd(","c).TrimEnd("."c)
        End Function

        Private Function ModeAjoutOmission() As Boolean
            Return cmbModeOperation IsNot Nothing AndAlso String.Equals(Convert.ToString(cmbModeOperation.SelectedItem), "AJOUTER PRODUIT OMIS", StringComparison.OrdinalIgnoreCase)
        End Function

        Private Sub EnregistrerStockInitial(sender As Object, e As EventArgs)
            _service.VerifierAccesStockInitialTechnique()
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim produitService As New ProduitService(New ProduitRepository(dal))
            Dim stockService As New StockService(dal)
            Dim typeService As New TypeVenteProduitService()
            Dim lignesTraitees As Integer = 0

            Try
                Dim modeOperation As String = If(ModeAjoutOmission(), "AJOUT_OMISSION", "CORRECTION")
                Dim lignesCibles As New List(Of DataGridViewRow)()
                Dim categoriesACreer As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                For Each previewRow As DataGridViewRow In grid.Rows
                    If previewRow.IsNewRow Then Continue For
                    Dim libellePreview As String = SafeString(previewRow.Cells("Libelle").Value)
                    If libellePreview = String.Empty Then Continue For
                    If Not EstLigneAEnregistrer(previewRow) Then Continue For
                    lignesCibles.Add(previewRow)
                    If Not SafeNullableInteger(previewRow.Cells("CategorieId").Value).HasValue AndAlso SafeString(previewRow.Cells("NomCategorie").Value) <> String.Empty Then
                        categoriesACreer.Add(SafeString(previewRow.Cells("NomCategorie").Value))
                    End If
                Next
                If lignesCibles.Count = 0 Then
                    MessageBox.Show("Aucune ligne modifiée ou saisie à enregistrer.", "Stock initial technique", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If

                Dim messageConfirmation As String = "Mode : " & modeOperation & Environment.NewLine &
                    "Produits concernés : " & lignesCibles.Count.ToString(CultureInfo.InvariantCulture) & Environment.NewLine &
                    "Catégories à créer : " & categoriesACreer.Count.ToString(CultureInfo.InvariantCulture) & Environment.NewLine & Environment.NewLine &
                    If(ModeAjoutOmission(),
                       "Les quantités saisies seront ajoutées au stock actuel uniquement pour les produits concernés.",
                       "Le stock final deviendra exactement la valeur affichée dans STOCK APRÈS. L'ancien stock ne sera pas additionné une deuxième fois.") &
                    Environment.NewLine & "Continuer ?"
                If MessageBox.Show(messageConfirmation, "Confirmer l'initialisation technique", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then
                    Return
                End If

                Dim sessionId As Integer = 0
                Dim referenceSession As String = String.Empty

                For Each dgRow As DataGridViewRow In grid.Rows
                    If dgRow.IsNewRow Then
                        Continue For
                    End If
                    If Not EstLigneAEnregistrer(dgRow) Then
                        Continue For
                    End If

                    Dim quantitePrincipale As Decimal = SafeDecimal(dgRow.Cells("QuantitePrincipale").Value)
                    Dim quantiteSecondaire As Decimal = SafeDecimal(dgRow.Cells("QuantiteSecondaire").Value)
                    If quantitePrincipale < 0D OrElse quantiteSecondaire < 0D Then
                        Throw New InvalidOperationException("Les quantités ne peuvent pas être négatives.")
                    End If
                    Dim rowView As DataRowView = TryCast(dgRow.DataBoundItem, DataRowView)
                    If rowView Is Nothing OrElse rowView.Row Is Nothing Then
                        Continue For
                    End If
                    CalculerLigne(rowView.Row)
                    Dim quantiteInitiale As Decimal = SafeDecimal(rowView.Row("QuantiteInitiale"))
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

                    Dim typeGestionStock As String = StockUnitConversionService.NormaliserTypeGestionStock(SafeString(dgRow.Cells("TypeGestionStock").Value))
                    Dim uniteMesureStock As String = SafeString(dgRow.Cells("UniteMesureStock").Value)
                    Dim contenuPrincipal As Decimal = SafeDecimal(dgRow.Cells("ContenuUnitePrincipale").Value)
                    Dim contenuSecondaire As Decimal = SafeDecimal(dgRow.Cells("ContenuUniteSecondaire").Value)
                    If StockUnitConversionService.EstGestionMesuree(typeGestionStock) Then
                        If String.IsNullOrWhiteSpace(uniteMesureStock) Then Throw New InvalidOperationException("Unité de mesure obligatoire pour " & libelle & ".")
                        If contenuPrincipal <= 0D Then Throw New InvalidOperationException("Contenu unité principale obligatoire pour " & libelle & ".")
                    End If

                    Dim categorieId As Integer? = SafeNullableInteger(dgRow.Cells("CategorieId").Value)
                    Dim nomCategorie As String = SafeString(dgRow.Cells("NomCategorie").Value)
                    If Not categorieId.HasValue AndAlso nomCategorie <> String.Empty Then
                        categorieId = _service.AssurerCategorieProduit(nomCategorie)
                        dgRow.Cells("CategorieId").Value = If(categorieId.HasValue, CType(categorieId.Value, Object), DBNull.Value)
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
                        .CategorieId = categorieId,
                        .UnitePrincipale = If(SafeString(dgRow.Cells("UnitePrincipale").Value) = String.Empty, "Carton", SafeString(dgRow.Cells("UnitePrincipale").Value)),
                        .UniteSecondaire = If(SafeString(dgRow.Cells("UniteSecondaire").Value) = String.Empty, "Piece", SafeString(dgRow.Cells("UniteSecondaire").Value)),
                        .ConversionUnite = Math.Max(1D, SafeDecimal(dgRow.Cells("ConversionUnite").Value)),
                        .TypeGestionStock = typeGestionStock,
                        .UniteMesureStock = If(String.IsNullOrWhiteSpace(uniteMesureStock), "PIECE", uniteMesureStock),
                        .ContenuUnitePrincipale = contenuPrincipal,
                        .ContenuUniteSecondaire = If(contenuSecondaire > 0D, CType(contenuSecondaire, Decimal?), Nothing),
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

                    Dim stockActuelBase As Decimal = SafeDecimal(dgRow.Cells("QuantiteStock").Value)
                    Dim stockFinalBase As Decimal = stockActuelBase
                    If StockSaisi(dgRow) Then
                        If sessionId = 0 Then
                            sessionId = _service.CreerSessionStockInitialTechnique(modeOperation, "Stock initial technique")
                            referenceSession = "INIT-STOCK-" & sessionId.ToString(CultureInfo.InvariantCulture)
                        End If
                        stockFinalBase = If(ModeAjoutOmission(), stockActuelBase + quantiteInitiale, quantiteInitiale)
                        If stockFinalBase <> stockActuelBase Then
                            stockService.AjusterStockInitialTechnique(produitId, stockFinalBase, referenceSession, modeOperation & " - Stock initial technique", SessionUtilisateur.UtilisateurId, produit.PrixAchat)
                        End If
                        _service.EnregistrerLigneStockInitialTechnique(sessionId, produitId, stockActuelBase, stockFinalBase, produit.TypeGestionStock, produit.UnitePrincipale, produit.UniteSecondaire, produit.ContenuUnitePrincipale, produit.ContenuUniteSecondaire, modeOperation, SafeString(rowView.Row("ResumeQuantite")))
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
