Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireInitialisationVentes
        Inherits Form

        Private ReadOnly _service As New InitialisationVenteService()
        Private _sessionId As Integer
        Private _produits As DataTable
        Private _vueProduits As DataView
        Private _chargement As Boolean

        Private ReadOnly dtpDebut As DateTimePicker
        Private ReadOnly dtpFin As DateTimePicker
        Private ReadOnly cmbMode As ComboBox
        Private ReadOnly txtObservation As TextBox
        Private ReadOnly dtpDateVente As DateTimePicker
        Private ReadOnly txtRecherche As TextBox
        Private ReadOnly cmbCategorie As ComboBox
        Private ReadOnly cmbProduit As ComboBox
        Private ReadOnly cmbTypeVente As ComboBox
        Private ReadOnly txtQuantite As TextBox
        Private ReadOnly txtPrix As TextBox
        Private ReadOnly lblAideMode As Label
        Private ReadOnly lblApercu As Label
        Private ReadOnly lblTotal As Label
        Private ReadOnly grid As DataGridView

        Public Sub New()
            Text = "Initialisation des ventes"
            BackColor = Color.FromArgb(245, 247, 250)
            MinimumSize = New Size(1100, 700)

            Dim fontTitre As New Font("Segoe UI", 18, FontStyle.Bold)
            Dim fontLabel As New Font("Segoe UI", 9.5F, FontStyle.Regular)
            Dim fontBold As New Font("Segoe UI", 10, FontStyle.Bold)
            Dim colorPrimary As Color = Color.FromArgb(31, 41, 55)
            Dim colorMuted As Color = Color.FromArgb(107, 114, 128)
            Dim colorAccent As Color = Color.FromArgb(14, 116, 144)

            Dim root As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 4, .Padding = New Padding(18)}
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 92))
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 178))
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 150))
            root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim header As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(18)}
            header.Controls.Add(New Label() With {.Text = "INITIALISATION DES VENTES", .Font = fontTitre, .ForeColor = colorPrimary, .AutoSize = True, .Left = 18, .Top = 12})
            header.Controls.Add(New Label() With {.Text = "Reprise sécurisée des ventes antérieures à la mise en service de l'ERP.", .Font = fontLabel, .ForeColor = colorMuted, .AutoSize = True, .Left = 20, .Top = 54})

            Dim sessionPanel As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(16), .ColumnCount = 4, .RowCount = 3}
            sessionPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 160))
            sessionPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35))
            sessionPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 160))
            sessionPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 65))
            sessionPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 42))
            sessionPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 42))
            sessionPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            dtpDebut = New DateTimePicker() With {.Dock = DockStyle.Fill, .Format = DateTimePickerFormat.Short, .Value = New Date(Date.Today.Year, Date.Today.Month, 1)}
            dtpFin = New DateTimePicker() With {.Dock = DockStyle.Fill, .Format = DateTimePickerFormat.Short, .Value = Date.Today}
            cmbMode = New ComboBox() With {.Dock = DockStyle.Fill, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbMode.Items.Add(New ComboItem("Historique uniquement - ne modifie pas le stock", InitialisationVenteService.ModeHistoriqueUniquement))
            cmbMode.Items.Add(New ComboItem("Reconstitution complète - déduit le stock", InitialisationVenteService.ModeReconstitutionComplete))
            cmbMode.SelectedIndex = 0
            txtObservation = New TextBox() With {.Dock = DockStyle.Fill, .Multiline = True}
            lblAideMode = New Label() With {.Dock = DockStyle.Fill, .ForeColor = colorMuted, .Font = fontLabel, .Text = "Mode recommandé : les ventes reprises alimentent CA, CMV et analyses, sans redéduire le stock déjà constaté au déploiement."}

            sessionPanel.Controls.Add(CreerLabel("Date début", fontBold, colorPrimary), 0, 0)
            sessionPanel.Controls.Add(dtpDebut, 1, 0)
            sessionPanel.Controls.Add(CreerLabel("Date fin", fontBold, colorPrimary), 2, 0)
            sessionPanel.Controls.Add(dtpFin, 3, 0)
            sessionPanel.Controls.Add(CreerLabel("Mode stock", fontBold, colorPrimary), 0, 1)
            sessionPanel.Controls.Add(cmbMode, 1, 1)
            sessionPanel.Controls.Add(lblAideMode, 2, 1)
            sessionPanel.SetColumnSpan(lblAideMode, 2)
            sessionPanel.Controls.Add(CreerLabel("Observation", fontBold, colorPrimary), 0, 2)
            sessionPanel.Controls.Add(txtObservation, 1, 2)
            sessionPanel.SetColumnSpan(txtObservation, 3)

            Dim saisie As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(16), .ColumnCount = 9, .RowCount = 3}
            For i As Integer = 0 To 8
                saisie.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 11.11F))
            Next
            saisie.RowStyles.Add(New RowStyle(SizeType.Absolute, 34))
            saisie.RowStyles.Add(New RowStyle(SizeType.Absolute, 46))
            saisie.RowStyles.Add(New RowStyle(SizeType.Absolute, 34))

            dtpDateVente = New DateTimePicker() With {.Dock = DockStyle.Fill, .Format = DateTimePickerFormat.Short, .Value = Date.Today}
            txtRecherche = New TextBox() With {.Dock = DockStyle.Fill}
            cmbCategorie = New ComboBox() With {.Dock = DockStyle.Fill, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbProduit = New ComboBox() With {.Dock = DockStyle.Fill, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbTypeVente = New ComboBox() With {.Dock = DockStyle.Fill, .DropDownStyle = ComboBoxStyle.DropDownList}
            txtQuantite = New TextBox() With {.Dock = DockStyle.Fill}
            txtPrix = New TextBox() With {.Dock = DockStyle.Fill}
            lblApercu = New Label() With {.Dock = DockStyle.Fill, .ForeColor = colorPrimary, .BackColor = Color.FromArgb(248, 250, 252), .Font = fontBold, .TextAlign = ContentAlignment.MiddleLeft, .Padding = New Padding(10, 0, 10, 0)}

            Dim btnAjouter As New Button() With {.Text = "Ajouter ligne", .Dock = DockStyle.Fill, .BackColor = colorAccent, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            btnAjouter.FlatAppearance.BorderSize = 0
            AddHandler btnAjouter.Click, AddressOf AjouterLigne

            saisie.Controls.Add(CreerLabel("Date vente", fontBold, colorPrimary), 0, 0)
            saisie.Controls.Add(CreerLabel("Recherche", fontBold, colorPrimary), 1, 0)
            saisie.Controls.Add(CreerLabel("Catégorie", fontBold, colorPrimary), 2, 0)
            saisie.Controls.Add(CreerLabel("Produit", fontBold, colorPrimary), 3, 0)
            saisie.Controls.Add(CreerLabel("Type vente", fontBold, colorPrimary), 5, 0)
            saisie.Controls.Add(CreerLabel("Qté", fontBold, colorPrimary), 6, 0)
            saisie.Controls.Add(CreerLabel("Prix unitaire", fontBold, colorPrimary), 7, 0)
            saisie.Controls.Add(dtpDateVente, 0, 1)
            saisie.Controls.Add(txtRecherche, 1, 1)
            saisie.Controls.Add(cmbCategorie, 2, 1)
            saisie.Controls.Add(cmbProduit, 3, 1)
            saisie.SetColumnSpan(cmbProduit, 2)
            saisie.Controls.Add(cmbTypeVente, 5, 1)
            saisie.Controls.Add(txtQuantite, 6, 1)
            saisie.Controls.Add(txtPrix, 7, 1)
            saisie.Controls.Add(btnAjouter, 8, 1)
            saisie.Controls.Add(lblApercu, 0, 2)
            saisie.SetColumnSpan(lblApercu, 9)

            grid = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .AutoGenerateColumns = False,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .MultiSelect = False,
                .BackgroundColor = Color.White,
                .RowHeadersVisible = False
            }
            ConfigurerGrille()

            Dim bas As New TableLayoutPanel() With {.Dock = DockStyle.Bottom, .Height = 48, .ColumnCount = 4, .Padding = New Padding(0, 8, 0, 0)}
            bas.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            bas.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150))
            bas.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 150))
            bas.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 170))
            lblTotal = New Label() With {.Dock = DockStyle.Fill, .Text = "TOTAL CA REPRIS : 0 FC | TOTAL CMV : 0 FC | BÉNÉFICE REPRIS : 0 FC | LIGNES : 0", .Font = fontBold, .ForeColor = colorPrimary, .BackColor = Color.FromArgb(248, 250, 252), .TextAlign = ContentAlignment.MiddleLeft, .Padding = New Padding(10, 0, 10, 0)}
            Dim btnSupprimer As New Button() With {.Text = "Supprimer ligne", .Dock = DockStyle.Fill, .BackColor = Color.FromArgb(107, 114, 128), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            Dim btnValider As New Button() With {.Text = "Valider session", .Dock = DockStyle.Fill, .BackColor = Color.FromArgb(22, 163, 74), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            Dim btnAnnuler As New Button() With {.Text = "Fermer", .Dock = DockStyle.Fill, .BackColor = Color.FromArgb(75, 85, 99), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            btnSupprimer.FlatAppearance.BorderSize = 0
            btnValider.FlatAppearance.BorderSize = 0
            btnAnnuler.FlatAppearance.BorderSize = 0
            AddHandler btnSupprimer.Click, AddressOf SupprimerLigne
            AddHandler btnValider.Click, AddressOf ValiderSession
            AddHandler btnAnnuler.Click, Sub(sender, e) Close()
            bas.Controls.Add(lblTotal, 0, 0)
            bas.Controls.Add(btnSupprimer, 1, 0)
            bas.Controls.Add(btnValider, 2, 0)
            bas.Controls.Add(btnAnnuler, 3, 0)

            Dim panelGrille As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(16)}
            panelGrille.Controls.Add(grid)
            panelGrille.Controls.Add(bas)

            root.Controls.Add(header, 0, 0)
            root.Controls.Add(sessionPanel, 0, 1)
            root.Controls.Add(saisie, 0, 2)
            root.Controls.Add(panelGrille, 0, 3)
            Controls.Add(root)

            AddHandler Load, AddressOf FormulaireInitialisationVentes_Load
            AddHandler cmbMode.SelectedIndexChanged, AddressOf ChangerMode
            AddHandler txtRecherche.TextChanged, AddressOf AppliquerFiltreProduits
            AddHandler cmbCategorie.SelectedIndexChanged, AddressOf AppliquerFiltreProduits
            AddHandler cmbProduit.SelectedIndexChanged, AddressOf ProduitSelectionne
            AddHandler cmbTypeVente.SelectedIndexChanged, AddressOf TypeVenteSelectionne
            AddHandler txtQuantite.TextChanged, AddressOf MettreAJourApercu
            AddHandler txtPrix.TextChanged, AddressOf MettreAJourApercu
        End Sub

        Private Sub FormulaireInitialisationVentes_Load(sender As Object, e As EventArgs)
            Try
                _chargement = True
                ChargerCategories()
                ChargerProduits()
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireInitialisationVentes", "Load", "Chargement impossible.", ex)
                MessageBox.Show("Impossible de charger l'initialisation des ventes : " & ex.Message)
            Finally
                _chargement = False
            End Try
        End Sub

        Private Sub ConfigurerGrille()
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "InitialisationVenteLigneId", .Name = "InitialisationVenteLigneId", .Visible = False})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "DateVente", .HeaderText = "Date", .Width = 95, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "dd/MM/yyyy"}})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "CodeProduit", .HeaderText = "Code", .Width = 110})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "LibelleProduit", .HeaderText = "Produit", .Width = 220})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "Categorie", .HeaderText = "Catégorie", .Width = 120})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "TypeVente", .HeaderText = "Type", .Width = 110})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "QuantiteCommerciale", .HeaderText = "Qté", .Width = 80})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "QuantiteBase", .HeaderText = "Quantité base", .Width = 110})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "PrixUnitaire", .HeaderText = "Prix", .Width = 90, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "N0"}})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "MontantLigne", .HeaderText = "Montant", .Width = 100, .DefaultCellStyle = New DataGridViewCellStyle() With {.Format = "N0"}})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "CoutUnitaireBaseVente", .HeaderText = "Coût base", .Width = 90})
            grid.Columns.Add(New DataGridViewTextBoxColumn() With {.DataPropertyName = "BeneficeEstime", .HeaderText = "Bénéfice", .Width = 100})
        End Sub

        Private Sub ChargerCategories()
            Dim dt As DataTable = _service.ListerCategories()
            Dim row As DataRow = dt.NewRow()
            row("CategorieId") = DBNull.Value
            row("NomCategorie") = "Toutes les catégories"
            dt.Rows.InsertAt(row, 0)
            cmbCategorie.DataSource = dt
            cmbCategorie.DisplayMember = "NomCategorie"
            cmbCategorie.ValueMember = "CategorieId"
        End Sub

        Private Sub ChargerProduits()
            _produits = _service.ListerProduits()
            If Not _produits.Columns.Contains("RechercheNormalisee") Then
                _produits.Columns.Add("RechercheNormalisee", GetType(String))
            End If
            For Each row As DataRow In _produits.Rows
                row("RechercheNormalisee") = (SafeString(row("Libelle")) & " " & SafeString(row("CodeBarres"))).ToUpperInvariant()
            Next

            _vueProduits = New DataView(_produits)
            cmbProduit.DataSource = _vueProduits
            cmbProduit.DisplayMember = "Libelle"
            cmbProduit.ValueMember = "ProduitId"
            AppliquerFiltreProduits(Nothing, EventArgs.Empty)
        End Sub

        Private Sub AppliquerFiltreProduits(sender As Object, e As EventArgs)
            If _chargement OrElse _vueProduits Is Nothing Then Return
            Dim clauses As New System.Collections.Generic.List(Of String)()
            Dim recherche As String = txtRecherche.Text.Trim().Replace("'", "''").ToUpperInvariant()
            If recherche <> String.Empty Then clauses.Add("RechercheNormalisee LIKE '%" & recherche & "%'")

            If cmbCategorie.SelectedValue IsNot Nothing AndAlso cmbCategorie.SelectedValue IsNot DBNull.Value Then
                Dim categorieId As Integer
                If Integer.TryParse(Convert.ToString(cmbCategorie.SelectedValue), categorieId) AndAlso categorieId > 0 Then
                    clauses.Add("CategorieId = " & categorieId.ToString(CultureInfo.InvariantCulture))
                End If
            End If
            _vueProduits.RowFilter = String.Join(" AND ", clauses)
        End Sub

        Private Sub ProduitSelectionne(sender As Object, e As EventArgs)
            If _chargement OrElse cmbProduit.SelectedValue Is Nothing Then Return
            Dim produitId As Integer
            If Not Integer.TryParse(Convert.ToString(cmbProduit.SelectedValue), produitId) Then Return
            Try
                Dim types As System.Collections.Generic.List(Of TypeVenteDTO) = _service.ListerTypesVente(produitId)
                cmbTypeVente.DataSource = types
                cmbTypeVente.DisplayMember = "NomAffichage"
                cmbTypeVente.ValueMember = "Nom"
                If types.Count > 0 Then cmbTypeVente.SelectedIndex = 0
                TypeVenteSelectionne(Nothing, EventArgs.Empty)
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireInitialisationVentes", "ProduitSelectionne", "Chargement types vente impossible.", ex)
            End Try
        End Sub

        Private Sub TypeVenteSelectionne(sender As Object, e As EventArgs)
            Dim typeVente As TypeVenteDTO = TryCast(cmbTypeVente.SelectedItem, TypeVenteDTO)
            If typeVente IsNot Nothing Then
                txtPrix.Text = typeVente.PrixVente.ToString("N0")
            End If
            MettreAJourApercu(Nothing, EventArgs.Empty)
        End Sub

        Private Sub AjouterLigne(sender As Object, e As EventArgs)
            Try
                Dim session As InitialisationVenteSessionDTO = EnregistrerSessionCourante()
                If session Is Nothing Then Return

                Dim produitId As Integer = Convert.ToInt32(cmbProduit.SelectedValue)
                Dim qte As Decimal = LireDecimalObligatoire(txtQuantite.Text, "quantité")
                Dim prix As Decimal = LireDecimalObligatoire(txtPrix.Text, "prix unitaire")
                Dim typeVente As TypeVenteDTO = TryCast(cmbTypeVente.SelectedItem, TypeVenteDTO)
                If dtpDateVente.Value.Date < session.DateDebut.Date OrElse dtpDateVente.Value.Date > session.DateFin.Date Then
                    Throw New InvalidOperationException("La date de vente doit appartenir à la période de reprise.")
                End If
                Dim ligne As InitialisationVenteLigneDTO = _service.ConstruireLigne(session.InitialisationVenteSessionId, dtpDateVente.Value.Date, produitId, typeVente, qte, prix)
                _service.AjouterLigne(ligne)
                RechargerLignes()
                txtQuantite.Clear()
                txtQuantite.Focus()
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Initialisation des ventes", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        Private Sub SupprimerLigne(sender As Object, e As EventArgs)
            If grid.CurrentRow Is Nothing Then Return
            Dim ligneId As Integer = Convert.ToInt32(grid.CurrentRow.Cells("InitialisationVenteLigneId").Value)
            If MessageBox.Show("Supprimer cette ligne de reprise ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            _service.SupprimerLigne(ligneId)
            RechargerLignes()
        End Sub

        Private Sub ValiderSession(sender As Object, e As EventArgs)
            Try
                Dim session As InitialisationVenteSessionDTO = EnregistrerSessionCourante()
                If session Is Nothing Then Return
                If _service.ExistePeriodeValideeChevauchante(session.DateDebut, session.DateFin, session.InitialisationVenteSessionId) Then
                    MessageBox.Show("Une session validée couvre déjà tout ou partie de cette période. Validation bloquée pour éviter une double reprise.", "Période déjà reprise", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If

                Dim modeMessage As String = If(String.Equals(session.ModeStock, InitialisationVenteService.ModeReconstitutionComplete, StringComparison.OrdinalIgnoreCase),
                    "RECONSTITUTION COMPLETE : les ventes reprises déduiront le stock physique.",
                    "HISTORIQUE UNIQUEMENT : les ventes reprises alimenteront les analyses sans modifier le stock.")
                If MessageBox.Show(modeMessage & Environment.NewLine & Environment.NewLine & "Valider définitivement la session " & session.ReferenceSession & " ?", "Validation initialisation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then Return
                _service.ValiderSession(session.InitialisationVenteSessionId)
                MessageBox.Show("Session validée.", "Initialisation des ventes", MessageBoxButtons.OK, MessageBoxIcon.Information)
                RechargerLignes()
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireInitialisationVentes", "ValiderSession", "Validation impossible.", ex)
                MessageBox.Show("Validation impossible : " & ex.Message, "Initialisation des ventes", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Function EnregistrerSessionCourante() As InitialisationVenteSessionDTO
            Dim mode As String = CType(cmbMode.SelectedItem, ComboItem).Value
            Dim session As InitialisationVenteSessionDTO = _service.CreerOuMettreAJourSession(_sessionId, dtpDebut.Value.Date, dtpFin.Value.Date, mode, txtObservation.Text)
            If session IsNot Nothing Then _sessionId = session.InitialisationVenteSessionId
            Return session
        End Function

        Private Sub RechargerLignes()
            If _sessionId <= 0 Then Return
            Dim dt As DataTable = _service.ListerLignes(_sessionId)
            grid.DataSource = dt
            Dim total As Decimal = 0D
            Dim totalCmv As Decimal = 0D
            Dim benefice As Decimal = 0D
            For Each row As DataRow In dt.Rows
                Dim montant As Decimal = SafeDecimal(row("MontantLigne"))
                Dim quantiteBase As Decimal = SafeDecimal(row("QuantiteBase"))
                Dim coutBase As Decimal = SafeDecimal(row("CoutUnitaireBaseVente"))
                total += montant
                totalCmv += quantiteBase * coutBase
                benefice += SafeDecimal(row("BeneficeEstime"))
            Next
            lblTotal.Text = "TOTAL CA REPRIS : " & FormatageGlobal.FormatMontant(total) &
                " | TOTAL CMV : " & FormatageGlobal.FormatMontant(totalCmv) &
                " | BÉNÉFICE REPRIS : " & FormatageGlobal.FormatMontant(benefice) &
                " | LIGNES : " & dt.Rows.Count.ToString("N0", CultureInfo.InvariantCulture)
        End Sub

        Private Sub ChangerMode(sender As Object, e As EventArgs)
            Dim mode As String = If(cmbMode.SelectedItem Is Nothing, InitialisationVenteService.ModeHistoriqueUniquement, CType(cmbMode.SelectedItem, ComboItem).Value)
            If String.Equals(mode, InitialisationVenteService.ModeReconstitutionComplete, StringComparison.OrdinalIgnoreCase) Then
                lblAideMode.Text = "Attention : les ventes reprises créeront des sorties et diminueront le stock physique."
            Else
                lblAideMode.Text = "Mode recommandé : les ventes reprises alimentent CA, CMV et analyses, sans redéduire le stock déjà constaté au déploiement."
            End If
        End Sub

        Private Sub MettreAJourApercu(sender As Object, e As EventArgs)
            Dim typeVente As TypeVenteDTO = TryCast(cmbTypeVente.SelectedItem, TypeVenteDTO)
            Dim qte As Decimal = LireDecimal(txtQuantite.Text)
            Dim prix As Decimal = LireDecimal(txtPrix.Text)
            If typeVente Is Nothing OrElse qte <= 0D Then
                lblApercu.Text = String.Empty
                Return
            End If
            Dim produitId As Integer
            If Not Integer.TryParse(Convert.ToString(cmbProduit.SelectedValue), produitId) Then
                lblApercu.Text = "Quantité base : " & FormatageGlobal.FormatQuantitePhysique(qte * typeVente.QuantiteEquivalent) & " | Montant : " & FormatageGlobal.FormatMontant(Math.Round(qte * prix, 2))
                Return
            End If

            Dim apercu As InitialisationVenteLigneDTO = _service.CalculerApercuLigne(produitId, typeVente, qte, prix)
            lblApercu.Text = "Quantité base : " & apercu.QuantiteBaseAffichage &
                " | Montant : " & FormatageGlobal.FormatMontant(apercu.MontantLigne) &
                " | Coût base : " & If(apercu.CoutUnitaireBaseVente.HasValue, FormatageGlobal.FormatMontant(apercu.CoutUnitaireBaseVente.Value), "N/C") &
                " | Bénéfice estimé : " & If(apercu.BeneficeEstime.HasValue, FormatageGlobal.FormatMontant(apercu.BeneficeEstime.Value), "N/C")
        End Sub

        Private Shared Function CreerLabel(texte As String, font As Font, couleur As Color) As Label
            Return New Label() With {.Text = texte, .Dock = DockStyle.Fill, .Font = font, .ForeColor = couleur, .TextAlign = ContentAlignment.MiddleLeft}
        End Function

        Private Shared Function LireDecimal(texte As String) As Decimal
            Dim t As String = If(texte, String.Empty).Trim()
            If t = String.Empty Then Return 0D
            Dim resultat As Decimal
            If Decimal.TryParse(t.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, resultat) Then Return resultat
            If Decimal.TryParse(t, NumberStyles.Any, CultureInfo.CurrentCulture, resultat) Then Return resultat
            Return 0D
        End Function

        Private Shared Function LireDecimalObligatoire(texte As String, nomChamp As String) As Decimal
            Dim resultat As Decimal = LireDecimal(texte)
            If resultat <= 0D Then Throw New InvalidOperationException("La valeur du champ " & nomChamp & " est invalide.")
            Return resultat
        End Function

        Private Shared Function SafeDecimal(value As Object) As Decimal
            If value Is Nothing OrElse Convert.IsDBNull(value) Then Return 0D
            Return Convert.ToDecimal(value)
        End Function

        Private Shared Function SafeString(value As Object) As String
            If value Is Nothing OrElse Convert.IsDBNull(value) Then Return String.Empty
            Return Convert.ToString(value)
        End Function

        Private NotInheritable Class ComboItem
            Public ReadOnly Property Text As String
            Public ReadOnly Property Value As String

            Public Sub New(text As String, value As String)
                Me.Text = text
                Me.Value = value
            End Sub

            Public Overrides Function ToString() As String
                Return Text
            End Function
        End Class
    End Class
End Namespace
