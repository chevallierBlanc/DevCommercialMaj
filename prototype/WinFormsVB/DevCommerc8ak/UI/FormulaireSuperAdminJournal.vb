Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireSuperAdminJournal
        Inherits Form

        Private ReadOnly _service As SuperAdminService
        Private ReadOnly grid As DataGridView
        Private ReadOnly txtUtilisateur As TextBox
        Private ReadOnly txtRole As TextBox
        Private ReadOnly txtModule As TextBox
        Private ReadOnly txtAction As TextBox
        Private ReadOnly cmbType As ComboBox
        Private ReadOnly dtpDebut As DateTimePicker
        Private ReadOnly dtpFin As DateTimePicker
        Private ReadOnly btnFiltrer As Button
        Private ReadOnly btnExporter As Button

        Public Sub New()
            _service = New SuperAdminService()

            Text = "SuperAdmin - Journal des actions utilisateurs"
            Width = 1200
            Height = 760
            StartPosition = FormStartPosition.CenterParent
            BackColor = Color.FromArgb(245, 247, 250)

            Dim root As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2, .Padding = New Padding(16)}
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 96))
            root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim filters As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .AutoScroll = True}
            txtUtilisateur = CreerZone("Utilisateur")
            txtRole = CreerZone("Rôle")
            txtModule = CreerZone("Module")
            txtAction = CreerZone("Action")
            cmbType = New ComboBox() With {.Width = 120, .DropDownStyle = ComboBoxStyle.DropDownList}
            cmbType.Items.AddRange(New Object() {"", "OK", "INFO", "WARN", "ERROR"})
            cmbType.SelectedIndex = 0
            dtpDebut = New DateTimePicker() With {.Width = 140, .Format = DateTimePickerFormat.Short}
            dtpFin = New DateTimePicker() With {.Width = 140, .Format = DateTimePickerFormat.Short}
            btnFiltrer = New Button() With {.Text = "Actualiser", .AutoSize = True}
            btnExporter = New Button() With {.Text = "Exporter CSV", .AutoSize = True}

            filters.Controls.AddRange(New Control() {
                New Label() With {.Text = "Début", .AutoSize = True, .Padding = New Padding(0, 10, 0, 0)}, dtpDebut,
                New Label() With {.Text = "Fin", .AutoSize = True, .Padding = New Padding(12, 10, 0, 0)}, dtpFin,
                txtUtilisateur, txtRole, txtModule, txtAction,
                New Label() With {.Text = "Type", .AutoSize = True, .Padding = New Padding(12, 10, 0, 0)}, cmbType,
                btnFiltrer, btnExporter
            })

            grid = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None
            }

            root.Controls.Add(filters, 0, 0)
            root.Controls.Add(grid, 0, 1)
            Controls.Add(root)

            AddHandler Load, AddressOf FormulaireSuperAdminJournal_Load
            AddHandler btnFiltrer.Click, AddressOf ChargerJournal
            AddHandler btnExporter.Click, AddressOf ExporterCsv
        End Sub

        Private Function CreerZone(placeholder As String) As TextBox
            Dim box As New TextBox() With {.Width = 140, .Tag = placeholder}
            Return box
        End Function

        Private Sub FormulaireSuperAdminJournal_Load(sender As Object, e As EventArgs)
            dtpDebut.Value = Date.Today.AddDays(-7)
            dtpFin.Value = Date.Today
            ChargerJournal(Nothing, EventArgs.Empty)
        End Sub

        Private Sub ChargerJournal(sender As Object, e As EventArgs)
            Try
                Dim lignes As List(Of AuditLogEntryDTO) = _service.ListerActionsUtilisateur(dtpDebut.Value.Date, dtpFin.Value.Date, txtUtilisateur.Text, txtRole.Text, txtModule.Text, txtAction.Text, Convert.ToString(cmbType.SelectedItem))
                grid.DataSource = lignes
                ConfigurerColonnes()
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireSuperAdminJournal", "ChargerJournal", "Chargement du journal impossible.", ex)
                MessageBox.Show("Impossible de charger le journal : " & ex.Message)
            End Try
        End Sub

        Private Sub ConfigurerColonnes()
            If grid.Columns.Count = 0 Then
                Return
            End If

            If grid.Columns.Contains("DateAction") Then grid.Columns("DateAction").HeaderText = "Date / Heure"
            If grid.Columns.Contains("Utilisateur") Then grid.Columns("Utilisateur").HeaderText = "Utilisateur"
            If grid.Columns.Contains("Role") Then grid.Columns("Role").HeaderText = "Rôle"
            If grid.Columns.Contains("Module") Then grid.Columns("Module").HeaderText = "Module"
            If grid.Columns.Contains("Action") Then grid.Columns("Action").HeaderText = "Action"
            If grid.Columns.Contains("Description") Then grid.Columns("Description").HeaderText = "Description"
            If grid.Columns.Contains("Machine") Then grid.Columns("Machine").HeaderText = "Machine / Poste"
            If grid.Columns.Contains("Statut") Then grid.Columns("Statut").HeaderText = "Statut"
            If grid.Columns.Contains("Niveau") Then grid.Columns("Niveau").Visible = False
        End Sub

        Private Sub ExporterCsv(sender As Object, e As EventArgs)
            Dim lignes As List(Of AuditLogEntryDTO) = TryCast(grid.DataSource, List(Of AuditLogEntryDTO))
            If lignes Is Nothing OrElse lignes.Count = 0 Then
                MessageBox.Show("Aucune ligne à exporter.")
                Return
            End If

            Using dialog As New SaveFileDialog()
                dialog.Filter = "CSV (*.csv)|*.csv"
                dialog.FileName = "journal_actions_" & Date.Now.ToString("yyyyMMdd_HHmmss") & ".csv"
                If dialog.ShowDialog(Me) <> DialogResult.OK Then
                    Return
                End If

                Dim lignesCsv As New List(Of String)()
                lignesCsv.Add("Date;Utilisateur;Role;Module;Action;Description;Machine;Statut;Niveau")
                For Each item As AuditLogEntryDTO In lignes
                    lignesCsv.Add(String.Join(";", New String() {
                        item.DateAction.ToString("yyyy-MM-dd HH:mm:ss"),
                        NettoyerCsv(item.Utilisateur),
                        NettoyerCsv(item.Role),
                        NettoyerCsv(item.Module),
                        NettoyerCsv(item.Action),
                        NettoyerCsv(item.Description),
                        NettoyerCsv(item.Machine),
                        NettoyerCsv(item.Statut),
                        NettoyerCsv(item.Niveau)
                    }))
                Next

                IO.File.WriteAllLines(dialog.FileName, lignesCsv)
                MessageBox.Show("Export terminé.")
            End Using
        End Sub

        Private Function NettoyerCsv(texte As String) As String
            If texte Is Nothing Then
                Return String.Empty
            End If
            Return texte.Replace(";", ",").Replace(Environment.NewLine, " ").Trim()
        End Function
    End Class
End Namespace
