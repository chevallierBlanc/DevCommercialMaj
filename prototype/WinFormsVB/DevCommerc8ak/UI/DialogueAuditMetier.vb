Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Web.Script.Serialization

Namespace DevCommerc8ak
    Public NotInheritable Class DialogueAuditMetier
        Private Sub New()
        End Sub

        Public Shared Sub Afficher(owner As IWin32Window, trace As AuditLogEntryDTO)
            Using form As New Form() With {.Text = "Détails du journal métier", .Size = New Size(950, 600),
                .MinimumSize = New Size(720, 420), .StartPosition = FormStartPosition.CenterParent,
                .BackColor = Color.FromArgb(240, 242, 245), .Font = New Font("Segoe UI", 10)}
                Dim head As New TextBox() With {.Dock = DockStyle.Top, .Height = 110, .Multiline = True, .ReadOnly = True,
                    .Text = trace.Action & " | " & trace.Statut & Environment.NewLine & trace.Description & Environment.NewLine &
                    "Utilisateur : " & trace.Utilisateur & " | Rôle : " & trace.Role & " | Mode : " & If(trace.ModeActif, "Non activé") & Environment.NewLine &
                    "Provenance : " & trace.Provenance & " | Session : " & Convert.ToString(trace.SessionId) & " | Poste : " & trace.Machine}
                Dim foot As New TextBox() With {.Dock = DockStyle.Bottom, .Height = 90, .Multiline = True, .ReadOnly = True, .Text = "Motif : " & trace.Motif}
                Dim grid As New DataGridView() With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False,
                    .AllowUserToDeleteRows = False, .RowHeadersVisible = False, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    .BackgroundColor = Color.White, .EnableHeadersVisualStyles = False}
                grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94)
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
                grid.RowTemplate.Height = 34
                Dim json As New JavaScriptSerializer() With {.MaxJsonLength = Integer.MaxValue}
                Dim avant As Object = If(String.IsNullOrWhiteSpace(trace.Avant), Nothing, json.DeserializeObject(trace.Avant))
                Dim apres As Object = If(String.IsNullOrWhiteSpace(trace.Apres), Nothing, json.DeserializeObject(trace.Apres))
                grid.DataSource = AuditDifferenceService.Comparer(avant, apres)
                form.Controls.Add(grid)
                form.Controls.Add(head)
                form.Controls.Add(foot)
                form.ShowDialog(owner)
            End Using
        End Sub
    End Class
End Namespace
