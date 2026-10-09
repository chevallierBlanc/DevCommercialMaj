Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Web.Script.Serialization
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public NotInheritable Class DialogueAuditMetier
        Private Sub New()
        End Sub

        Public Shared Sub Afficher(owner As IWin32Window, trace As AuditLogEntryDTO)
            Dim json As New JavaScriptSerializer() With {.MaxJsonLength = Integer.MaxValue}
            Dim avant As Object = If(String.IsNullOrWhiteSpace(trace.Avant), Nothing, json.DeserializeObject(trace.Avant))
            Dim apres As Object = If(String.IsNullOrWhiteSpace(trace.Apres), Nothing, json.DeserializeObject(trace.Apres))
            Dim noms As IDictionary(Of Integer, String) = AuditMetierService.ChargerIdentites(New DAL(ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString), avant, apres)
            Using form As New Form() With {.Text = "Détails du journal métier", .Size = New Size(980, 730),
                .MinimumSize = New Size(760, 580), .StartPosition = FormStartPosition.CenterParent,
                .BackColor = Color.FromArgb(245, 247, 250), .Font = New Font("Segoe UI", 10)}
                ' Comme l'apercu facture : cartes blanches, marges regulieres,
                ' synthese lisible et zone centrale adaptable a la fenetre.
                Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .Padding = New Padding(18), .ColumnCount = 1, .RowCount = 5}
                root.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                For Each hauteur As Integer In New Integer() {88, 128}
                    root.RowStyles.Add(New RowStyle(SizeType.Absolute, hauteur))
                Next
                root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
                root.RowStyles.Add(New RowStyle(SizeType.Absolute, 98))
                root.RowStyles.Add(New RowStyle(SizeType.Absolute, 46))
                Dim header As New TableLayoutPanel With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(16), .ColumnCount = 2, .RowCount = 2, .Margin = New Padding(0, 0, 0, 10)}
                header.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 76))
                header.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 24))
                header.Controls.Add(New Label With {.Text = AuditPresentationService.Evenement(trace.Action), .Dock = DockStyle.Fill, .Font = New Font("Segoe UI", 16, FontStyle.Bold), .ForeColor = Color.FromArgb(41, 128, 185)}, 0, 0)
                header.Controls.Add(New Label With {.Text = If(String.IsNullOrWhiteSpace(trace.ReferenceDocument), "Référence non renseignée", trace.ReferenceDocument), .Dock = DockStyle.Fill}, 0, 1)
                Dim resultat As New Label With {.Text = trace.Statut, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleCenter, .ForeColor = Color.White,
                    .BackColor = If(trace.Statut = "SUCCES" OrElse trace.Statut = "OK", Color.FromArgb(16, 145, 105), If(trace.Statut = "REFUS" OrElse trace.Statut = "ECHEC", Color.FromArgb(192, 57, 43), Color.FromArgb(52, 73, 94))), .Font = New Font("Segoe UI", 11, FontStyle.Bold)}
                header.Controls.Add(resultat, 1, 0)
                header.SetRowSpan(resultat, 2)
                Dim infos As New TableLayoutPanel With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(16), .ColumnCount = 2, .Margin = New Padding(0, 0, 0, 10)}
                infos.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
                infos.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
                infos.Controls.Add(New Label With {.Dock = DockStyle.Fill, .Text = "INFORMATIONS GÉNÉRALES" & Environment.NewLine & "Date : " & trace.DateAction.ToString("dd/MM/yyyy HH:mm:ss") & Environment.NewLine & "Catégorie : " & trace.Modul & Environment.NewLine & "Source : " & trace.Provenance}, 0, 0)
                infos.Controls.Add(New Label With {.Dock = DockStyle.Fill, .Text = "AUTEUR ET SESSION" & Environment.NewLine & "Utilisateur : " & trace.Utilisateur & " • Rôle : " & trace.Role & Environment.NewLine & "Mode : " & If(String.IsNullOrWhiteSpace(trace.ModeActif), "Parcours classique", trace.ModeActif) & Environment.NewLine & "Poste : " & trace.Machine & " • Session : " & Convert.ToString(trace.SessionId)}, 1, 0)
                Dim pages As New TabControl With {.Dock = DockStyle.Fill, .Margin = New Padding(0, 0, 0, 10)}
                Dim paiement As Boolean = If(trace.Action, "").StartsWith("ENCAISSEMENT", StringComparison.Ordinal)
                AjouterPage(pages, If(paiement, "Règlement de la facture", "Valeurs avant / après"), AuditPresentationService.Presenter(avant, apres, noms, paiement), paiement)
                If paiement Then AjouterPage(pages, "Changements", AuditPresentationService.Presenter(avant, apres, noms), False)
                If avant Is Nothing AndAlso apres Is Nothing Then
                    Dim historique As New TabPage("Trace historique")
                    historique.Controls.Add(New TextBox With {.Dock = DockStyle.Fill, .Multiline = True, .ReadOnly = True, .Text = trace.Description, .ScrollBars = ScrollBars.Vertical})
                    pages.TabPages.Add(historique)
                    pages.SelectedTab = historique
                End If
                Dim motif As New GroupBox With {.Text = "Motif de l'opération", .Dock = DockStyle.Fill, .Padding = New Padding(12), .BackColor = Color.White, .Margin = New Padding(0, 0, 0, 10)}
                motif.Controls.Add(New TextBox With {.Dock = DockStyle.Fill, .ReadOnly = True, .Multiline = True, .BorderStyle = BorderStyle.None, .BackColor = Color.White, .ScrollBars = ScrollBars.Vertical,
                    .Text = If(String.IsNullOrWhiteSpace(trace.Motif), "Aucun motif enregistré pour cette opération.", trace.Motif)})
                Dim footer As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft}
                Dim fermer As New Button With {.Text = "FERMER", .Width = 125, .Height = 36, .FlatStyle = FlatStyle.Flat, .BackColor = Color.FromArgb(41, 128, 185), .ForeColor = Color.White, .DialogResult = DialogResult.Cancel}
                footer.Controls.Add(fermer)
                form.CancelButton = fermer
                root.Controls.Add(header, 0, 0)
                root.Controls.Add(infos, 0, 1)
                root.Controls.Add(pages, 0, 2)
                root.Controls.Add(motif, 0, 3)
                root.Controls.Add(footer, 0, 4)
                form.Controls.Add(root)
                form.ShowDialog(owner)
            End Using
        End Sub

        Private Shared Sub AjouterPage(pages As TabControl, titre As String, lignes As List(Of AuditDifference), paiement As Boolean)
            Dim page As New TabPage(titre) With {.Padding = New Padding(8), .BackColor = Color.White}
            Dim grid As New DataGridView With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False, .RowHeadersVisible = False, .AutoGenerateColumns = False,
                .BackgroundColor = Color.White, .BorderStyle = BorderStyle.None, .EnableHeadersVisualStyles = False,
                .CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, .GridColor = Color.FromArgb(229, 231, 235),
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells}
            grid.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle With {.BackColor = Color.FromArgb(245, 245, 245), .ForeColor = Color.FromArgb(52, 73, 94), .Font = New Font("Segoe UI Semibold", 9.5F)}
            grid.ColumnHeadersHeight = 40
            grid.DefaultCellStyle.Padding = New Padding(8)
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 234, 246)
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(41, 128, 185)
            grid.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Information", .DataPropertyName = "Champ", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .FillWeight = 40})
            If Not paiement Then grid.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Avant", .DataPropertyName = "Avant", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .FillWeight = 30})
            grid.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = If(paiement, "Valeur enregistrée", "Après"), .DataPropertyName = "Apres", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .FillWeight = 30})
            grid.DataSource = lignes
            page.Controls.Add(grid)
            If paiement Then
                Dim note As New Label With {.Dock = DockStyle.Top, .Height = 50, .ForeColor = Color.FromArgb(102, 102, 102),
                    .Text = "Une valeur absente n'est pas reconstituée. Sur les anciennes traces, la devise déclarée ne prouve pas la devise des montants. Les nouveaux montants normalisés indiquent explicitement FC."}
                page.Controls.Add(note)
            End If
            pages.TabPages.Add(page)
        End Sub
    End Class
End Namespace
