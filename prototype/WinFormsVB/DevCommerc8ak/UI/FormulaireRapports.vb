Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class FormulaireRapports
        Inherits Form

        Private ReadOnly cmbType As ComboBox
        Private ReadOnly dtDebut As DateTimePicker
        Private ReadOnly dtFin As DateTimePicker
        Private ReadOnly btnCharger As Button
        Private ReadOnly btnExportCsv As Button
        Private ReadOnly btnExportPdf As Button
        Private ReadOnly grid As DataGridView
        Private ReadOnly timer As Timer

        Public Sub New()
            Me.Text = "Rapports"
            Me.Width = 900
            Me.Height = 600

            Dim panelTop As New Panel() With {.Dock = DockStyle.Top, .Height = 50}
            cmbType = New ComboBox() With {.Left = 20, .Top = 15, .Width = 200}
            cmbType.Items.AddRange(New Object() {"Journalier", "Mensuel", "Produits plus vendus"})
            cmbType.SelectedIndex = 0

            dtDebut = New DateTimePicker() With {.Left = 240, .Top = 15, .Width = 120}
            dtFin = New DateTimePicker() With {.Left = 380, .Top = 15, .Width = 120}
            btnCharger = New Button() With {.Text = "Charger", .Left = 520, .Top = 12, .Width = 80}
            btnExportCsv = New Button() With {.Text = "Export Excel", .Left = 610, .Top = 12, .Width = 100}
            btnExportPdf = New Button() With {.Text = "Export PDF", .Left = 720, .Top = 12, .Width = 100}

            AddHandler btnCharger.Click, AddressOf Charger
            AddHandler btnExportCsv.Click, AddressOf ExportCsv
            AddHandler btnExportPdf.Click, AddressOf ExportPdf

            panelTop.Controls.Add(cmbType)
            panelTop.Controls.Add(dtDebut)
            panelTop.Controls.Add(dtFin)
            panelTop.Controls.Add(btnCharger)
            panelTop.Controls.Add(btnExportCsv)
            panelTop.Controls.Add(btnExportPdf)

            grid = New DataGridView() With {.Dock = DockStyle.Fill, .AutoGenerateColumns = True, .ReadOnly = True}

            Me.Controls.Add(grid)
            Me.Controls.Add(panelTop)

            ThemeHelper.AppliquerTheme(Me)

            timer = New Timer() With {.Interval = 600000}
            AddHandler timer.Tick, AddressOf Charger
            timer.Start()
        End Sub

        Private Sub Charger(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim service As New RapportService(dal)

                If cmbType.SelectedItem.ToString() = "Produits plus vendus" Then
                    grid.DataSource = service.ProduitsPlusVendus(dtDebut.Value, dtFin.Value)
                ElseIf cmbType.SelectedItem.ToString() = "Journalier" Then
                    Dim sql As String = "SELECT CAST(CreeLe AS DATE) AS Jour, SUM(MontantTotal) AS CA " &
                                        "FROM FacturesVente WHERE CreeLe BETWEEN @d1 AND @d2 AND Statut='PAYEE' " &
                                        "GROUP BY CAST(CreeLe AS DATE) ORDER BY Jour"
                    Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                        New System.Data.SqlClient.SqlParameter("@d1", dtDebut.Value),
                        New System.Data.SqlClient.SqlParameter("@d2", dtFin.Value)
                    }
                    grid.DataSource = dal.ExecuterTable(sql, System.Data.CommandType.Text, p)
                ElseIf cmbType.SelectedItem.ToString() = "Mensuel" Then
                    Dim sql As String = "SELECT FORMAT(CreeLe,'yyyy-MM') AS Mois, SUM(MontantTotal) AS CA " &
                                        "FROM FacturesVente WHERE CreeLe BETWEEN @d1 AND @d2 AND Statut='PAYEE' " &
                                        "GROUP BY FORMAT(CreeLe,'yyyy-MM') ORDER BY Mois"
                    Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                        New System.Data.SqlClient.SqlParameter("@d1", dtDebut.Value),
                        New System.Data.SqlClient.SqlParameter("@d2", dtFin.Value)
                    }
                    grid.DataSource = dal.ExecuterTable(sql, System.Data.CommandType.Text, p)
                End If
            Catch ex As Exception
                MessageBox.Show("Erreur rapport: " & ex.Message)
            End Try
        End Sub

        Private Sub ExportCsv(sender As Object, e As EventArgs)
            Try
                Dim sfd As New SaveFileDialog() With {.Filter = "CSV (*.csv)|*.csv"}
                If sfd.ShowDialog() <> DialogResult.OK Then Return

                Dim sb As New StringBuilder()
                For Each col As DataGridViewColumn In grid.Columns
                    sb.Append(col.HeaderText & ";")
                Next
                sb.AppendLine()

                For Each row As DataGridViewRow In grid.Rows
                    If row.IsNewRow Then Continue For
                    For Each cell As DataGridViewCell In row.Cells
                        sb.Append(Convert.ToString(cell.Value) & ";")
                    Next
                    sb.AppendLine()
                Next

                File.WriteAllText(sfd.FileName, sb.ToString())
                MessageBox.Show("Export CSV termine.")
            Catch ex As Exception
                MessageBox.Show("Erreur export CSV: " & ex.Message)
            End Try
        End Sub

        Private Sub ExportPdf(sender As Object, e As EventArgs)
            Try
                Dim sfd As New SaveFileDialog() With {.Filter = "PDF (*.pdf)|*.pdf"}
                If sfd.ShowDialog() <> DialogResult.OK Then Return
                Dim lignes As New List(Of String)()
                For Each row As DataGridViewRow In grid.Rows
                    If row.IsNewRow Then Continue For
                    Dim line As String = ""
                    For Each cell As DataGridViewCell In row.Cells
                        line &= Convert.ToString(cell.Value) & " | "
                    Next
                    lignes.Add(line)
                Next
                PdfHelper.GenererPdfSimple(sfd.FileName, "RAPPORT", lignes)
                MessageBox.Show("PDF genere.")
            Catch ex As Exception
                MessageBox.Show("Erreur export PDF: " & ex.Message)
            End Try
        End Sub
    End Class
End Namespace
