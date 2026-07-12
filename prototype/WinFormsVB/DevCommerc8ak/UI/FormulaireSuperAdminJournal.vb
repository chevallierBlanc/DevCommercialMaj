Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D

Namespace DevCommerc8ak
    Public Class FormulaireSuperAdminJournal
        Inherits Form

        ' --- Services ---
        Private ReadOnly _service As SuperAdminService
        Private ReadOnly _log As New ProductionLogService()

        ' --- Composants UI ---
        Private grid As DataGridView
        Private txtUtilisateur As TextBox
        Private txtRole As TextBox
        Private txtModule As TextBox
        Private txtAction As TextBox
        Private cmbType As ComboBox
        Private dtpDebut As DateTimePicker
        Private dtpFin As DateTimePicker
        Private btnFiltrer As Button
        Private btnExporter As Button
        Private lblTitle As Label
        Private lblSubtitle As Label

        ' --- Palette de Couleurs Enterprise ERP ---
        Private ReadOnly ColorBg As Color = Color.FromArgb(240, 242, 245)
        Private ReadOnly ColorHeaderBg As Color = Color.White
        Private ReadOnly ColorCardBg As Color = Color.White
        Private ReadOnly ColorPrimary As Color = Color.FromArgb(0, 102, 204) ' Bleu Enterprise
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
            _service = New SuperAdminService()

            ' Configuration de la Form
            Me.Text = "Administration - Journal d'Audit"
            Me.Size = New Size(1300, 800)
            Me.MinimumSize = New Size(1000, 700)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = ColorBg
            Me.Font = FontMain
            Me.DoubleBuffered = True

            BuildUi()
            
            AddHandler Me.Load, AddressOf FormulaireSuperAdminJournal_Load
            AddHandler btnFiltrer.Click, AddressOf ChargerJournal
            AddHandler btnExporter.Click, AddressOf ExporterCsv
        End Sub

        Private Sub BuildUi()
            Me.Controls.Clear()

            ' --- Layout Principal ---
            Dim rootLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3,
                .BackColor = ColorBg
            }
            rootLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 100)) ' Header
            rootLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 120)) ' Filtres
            rootLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))  ' Grille

            ' --- 1. Header ---
            Dim pnlHeader As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorHeaderBg,
                .Padding = New Padding(30, 20, 30, 20)
            }
            
            lblTitle = New Label() With {
                .Text = "Journal des Actions Utilisateurs",
                .Font = FontTitle,
                .ForeColor = ColorTextPrimary,
                .AutoSize = True,
                .Location = New Point(30, 20)
            }
            
            lblSubtitle = New Label() With {
                .Text = "Consultez et exportez l'historique complet des activités système pour l'audit et la sécurité.",
                .Font = FontSubtitle,
                .ForeColor = ColorTextSecondary,
                .AutoSize = True,
                .Location = New Point(30, 55)
            }
            
            pnlHeader.Controls.AddRange({lblTitle, lblSubtitle})
            rootLayout.Controls.Add(pnlHeader, 0, 0)

            ' --- 2. Zone de Filtres (Style ERP) ---
            Dim pnlFiltersContainer As New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(30, 15, 30, 15)
            }
            
            Dim cardFilters As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorCardBg,
                .Padding = New Padding(20, 15, 20, 15),
                .BorderStyle = BorderStyle.None
            }
            
            Dim flowFilters As New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.LeftToRight,
                .AutoScroll = False
            }

            ' Création des contrôles de filtrage
            dtpDebut = New DateTimePicker() With {.Width = 130, .Format = DateTimePickerFormat.Short}
            dtpFin = New DateTimePicker() With {.Width = 130, .Format = DateTimePickerFormat.Short}
            txtUtilisateur = CreerZone("Utilisateur")
            txtRole = CreerZone("Rôle")
            txtModule = CreerZone("Module")
            txtAction = CreerZone("Action")
            
            cmbType = New ComboBox() With {
                .Width = 100, 
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .FlatStyle = FlatStyle.Flat
            }
            cmbType.Items.AddRange(New Object() {"TOUS", "OK", "INFO", "WARN", "ERROR"})
            cmbType.SelectedIndex = 0

            btnFiltrer = New Button() With {
                .Text = "ACTUALISER",
                .Size = New Size(130, 35),
                .Margin = New Padding(10, 0, 0, 0)
            }
            StyliserBouton(btnFiltrer, ColorPrimary, Color.White, False)

            btnExporter = New Button() With {
                .Text = "EXPORTER CSV",
                .Size = New Size(130, 35),
                .Margin = New Padding(5, 0, 0, 0)
            }
            StyliserBouton(btnExporter, Color.White, ColorTextSecondary, True)

            ' Ajout au FlowLayout avec Labels
            AjouterFiltre(flowFilters, "DÉBUT", dtpDebut)
            AjouterFiltre(flowFilters, "FIN", dtpFin)
            AjouterFiltre(flowFilters, "UTILISATEUR", txtUtilisateur)
            AjouterFiltre(flowFilters, "RÔLE", txtRole)
            AjouterFiltre(flowFilters, "MODULE", txtModule)
            AjouterFiltre(flowFilters, "ACTION", txtAction)
            AjouterFiltre(flowFilters, "STATUT", cmbType)
            
            flowFilters.Controls.Add(btnFiltrer)
            flowFilters.Controls.Add(btnExporter)

            cardFilters.Controls.Add(flowFilters)
            pnlFiltersContainer.Controls.Add(cardFilters)
            rootLayout.Controls.Add(pnlFiltersContainer, 0, 1)

            ' --- 3. Zone de Grille ---
            Dim pnlGridContainer As New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(30, 0, 30, 30)
            }
            
            Dim cardGrid As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorCardBg,
                .Padding = New Padding(1)
            }
            
            grid = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None,
                .RowHeadersVisible = False,
                .EnableHeadersVisualStyles = False,
                .GridColor = ColorBorder,
                .ColumnHeadersHeight = 45
            }
            
            ' Style des en-têtes
            Dim headerStyle As New DataGridViewCellStyle() With {
                .BackColor = Color.FromArgb(248, 249, 251),
                .ForeColor = ColorTextPrimary,
                .Font = FontBold,
                .SelectionBackColor = Color.FromArgb(248, 249, 251),
                .Alignment = DataGridViewContentAlignment.MiddleLeft
            }
            grid.ColumnHeadersDefaultCellStyle = headerStyle
            
            ' Style des cellules
            Dim cellStyle As New DataGridViewCellStyle() With {
                .Font = FontMain,
                .ForeColor = ColorTextPrimary,
                .SelectionBackColor = Color.FromArgb(232, 240, 254),
                .SelectionForeColor = ColorPrimary,
                .Padding = New Padding(5, 0, 5, 0)
            }
            grid.DefaultCellStyle = cellStyle
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 253, 255)

            cardGrid.Controls.Add(grid)
            pnlGridContainer.Controls.Add(cardGrid)
            rootLayout.Controls.Add(pnlGridContainer, 0, 2)

            Me.Controls.Add(rootLayout)
        End Sub

        Private Sub AjouterFiltre(container As FlowLayoutPanel, labelText As String, control As Control)
            Dim pnl As New Panel() With {.AutoSize = True, .Margin = New Padding(0, 0, 15, 10)}
            Dim lbl As New Label() With {
                .Text = labelText,
                .Font = New Font("Segoe UI", 7.5F, FontStyle.Bold),
                .ForeColor = ColorTextSecondary,
                .AutoSize = True,
                .Location = New Point(0, 0)
            }
            control.Location = New Point(0, 18)
            pnl.Controls.AddRange({lbl, control})
            container.Controls.Add(pnl)
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

        Private Function CreerZone(placeholder As String) As TextBox
            Dim box As New TextBox() With {
                .Width = 120, 
                .Tag = placeholder,
                .BorderStyle = BorderStyle.FixedSingle,
                .Font = FontMain
            }
            Return box
        End Function

        Private Sub FormulaireSuperAdminJournal_Load(sender As Object, e As EventArgs)
            dtpDebut.Value = Date.Today.AddDays(-7)
            dtpFin.Value = Date.Today
            ChargerJournal(Nothing, EventArgs.Empty)
        End Sub

        Private Sub ChargerJournal(sender As Object, e As EventArgs)
            Try
                Me.Cursor = Cursors.WaitCursor
                Dim statutFiltre As String = If(cmbType.SelectedIndex = 0, "", Convert.ToString(cmbType.SelectedItem))
                
                Dim lignes As List(Of AuditLogEntryDTO) = _service.ListerActionsUtilisateur(
                    dtpDebut.Value.Date, 
                    dtpFin.Value.Date, 
                    txtUtilisateur.Text, 
                    txtRole.Text, 
                    txtModule.Text, 
                    txtAction.Text, 
                    statutFiltre
                )
                
                grid.DataSource = lignes
                ConfigurerColonnes()
            Catch ex As Exception
                _log.Error("FormulaireSuperAdminJournal", "ChargerJournal", "Chargement du journal impossible.", ex)
                MessageBox.Show("Impossible de charger le journal : " & ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Sub ConfigurerColonnes()
            If grid.Columns.Count = 0 Then Return

            Dim mappings As New Dictionary(Of String, String) From {
                {"DateAction", "DATE / HEURE"},
                {"Utilisateur", "UTILISATEUR"},
                {"Role", "RÔLE"},
                {"Module", "MODULE"},
                {"Action", "ACTION"},
                {"Description", "DESCRIPTION"},
                {"Machine", "POSTE / MACHINE"},
                {"Statut", "STATUT"}
            }

            For Each kvp As KeyValuePair(Of String, String) In mappings
                If grid.Columns.Contains(kvp.Key) Then
                    grid.Columns(kvp.Key).HeaderText = kvp.Value
                End If
            Next

            If grid.Columns.Contains("Niveau") Then grid.Columns("Niveau").Visible = False
            
            ' Ajustement des largeurs
            If grid.Columns.Contains("DateAction") Then grid.Columns("DateAction").Width = 140
            If grid.Columns.Contains("Statut") Then grid.Columns("Statut").Width = 80
        End Sub

        Private Sub ExporterCsv(sender As Object, e As EventArgs)
            Dim lignes As List(Of AuditLogEntryDTO) = TryCast(grid.DataSource, List(Of AuditLogEntryDTO))
            If lignes Is Nothing OrElse lignes.Count = 0 Then
                MessageBox.Show("Aucune donnée à exporter.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Using dialog As New SaveFileDialog()
                dialog.Filter = "Fichier CSV (*.csv)|*.csv"
                dialog.Title = "Exporter le journal d'audit"
                dialog.FileName = "AuditLog_" & Date.Now.ToString("yyyyMMdd_HHmmss") & ".csv"
                
                If dialog.ShowDialog(Me) <> DialogResult.OK Then Return

                Try
                    Dim lignesCsv As New List(Of String)()
                    lignesCsv.Add("Date;Utilisateur;Role;Module;Action;Description;Machine;Statut;Niveau")
                    
                    For Each item As AuditLogEntryDTO In lignes
                        lignesCsv.Add(String.Join(";", New String() {
                            item.DateAction.ToString("yyyy-MM-dd HH:mm:ss"),
                            NettoyerCsv(item.Utilisateur),
                            NettoyerCsv(item.Role),
                            NettoyerCsv(item.Modul),
                            NettoyerCsv(item.Action),
                            NettoyerCsv(item.Description),
                            NettoyerCsv(item.Machine),
                            NettoyerCsv(item.Statut),
                            NettoyerCsv(item.Niveau)
                        }))
                    Next

                    IO.File.WriteAllLines(dialog.FileName, lignesCsv, System.Text.Encoding.UTF8)
                    MessageBox.Show("Le journal a été exporté avec succès.", "Export Terminé", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    _log.Error("FormulaireSuperAdminJournal", "ExporterCsv", "Erreur d'export.", ex)
                    MessageBox.Show("Erreur lors de l'export : " & ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End Using
        End Sub

        Private Function NettoyerCsv(texte As String) As String
            If String.IsNullOrEmpty(texte) Then Return String.Empty
            Return texte.Replace(";", ",").Replace(Environment.NewLine, " ").Trim()
        End Function

    End Class
End Namespace
