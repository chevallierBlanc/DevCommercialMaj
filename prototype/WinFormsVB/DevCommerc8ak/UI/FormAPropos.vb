Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormAPropos
        Inherits Form

        Public Sub New()
            Me.Text = "À propos"
            Me.StartPosition = FormStartPosition.CenterParent
            Me.Size = New Size(560, 380)
            Me.MinimumSize = New Size(560, 380)
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = Color.White
            Me.Font = New Font("Segoe UI", 10.0F)

            BuildUi()
        End Sub

        Private Sub BuildUi()
            Dim root As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 3,
                .Padding = New Padding(20)
            }
            root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            root.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))
            root.RowStyles.Add(New RowStyle(SizeType.AutoSize))

            Dim header As New Panel() With {.Dock = DockStyle.Top, .Height = 70, .BackColor = Color.FromArgb(28, 35, 49)}
            Dim lblTitle As New Label() With {
                .Text = "ERPCommercial",
                .Dock = DockStyle.Top,
                .Height = 34,
                .Font = New Font("Segoe UI", 18.0F, FontStyle.Bold),
                .ForeColor = Color.White,
                .TextAlign = ContentAlignment.MiddleLeft
            }
            Dim lblSubtitle As New Label() With {
                .Text = "Informations de production",
                .Dock = DockStyle.Top,
                .Height = 24,
                .Font = New Font("Segoe UI", 9.5F),
                .ForeColor = Color.FromArgb(203, 213, 225),
                .TextAlign = ContentAlignment.MiddleLeft
            }
            header.Padding = New Padding(16, 12, 16, 12)
            header.Controls.Add(lblSubtitle)
            header.Controls.Add(lblTitle)

            Dim body As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(6, 16, 6, 6)}

            Dim info As New TableLayoutPanel() With {
                .Dock = DockStyle.Top,
                .ColumnCount = 2,
                .AutoSize = True,
                .AutoSizeMode = AutoSizeMode.GrowAndShrink
            }
            info.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 180))
            info.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

            AjouterLigne(info, 0, "Version", "1.0.0")
            AjouterLigne(info, 1, "Licence", "Production")
            AjouterLigne(info, 2, "Base", "CommercialMagDB")
            AjouterLigne(info, 3, "Serveur SQL", ObtenirServeurActuel())
            AjouterLigne(info, 4, "Dernière sauvegarde", ObtenirDerniereSauvegarde())
            AjouterLigne(info, 5, "Développé par", "NTANTA ANDY")

            body.Controls.Add(info)

            Dim btnClose As New Button() With {
                .Text = "Fermer",
                .Width = 120,
                .Height = 38,
                .Anchor = AnchorStyles.Right,
                .FlatStyle = FlatStyle.Flat,
                .BackColor = Color.FromArgb(59, 130, 246),
                .ForeColor = Color.White
            }
            btnClose.FlatAppearance.BorderSize = 0
            AddHandler btnClose.Click, Sub() Me.Close()

            Dim footer As New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.RightToLeft,
                .WrapContents = False,
                .AutoSize = True,
                .Padding = New Padding(0, 10, 0, 0)
            }
            footer.Controls.Add(btnClose)

            root.Controls.Add(header, 0, 0)
            root.Controls.Add(body, 0, 1)
            root.Controls.Add(footer, 0, 2)
            Me.Controls.Add(root)
        End Sub

        Private Sub AjouterLigne(parent As TableLayoutPanel, rowIndex As Integer, libelle As String, valeur As String)
            parent.RowStyles.Add(New RowStyle(SizeType.AutoSize))

            Dim lblKey As New Label() With {
                .Text = libelle,
                .Dock = DockStyle.Fill,
                .Height = 28,
                .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
                .ForeColor = Color.FromArgb(75, 85, 99),
                .TextAlign = ContentAlignment.MiddleLeft
            }
            Dim lblValue As New Label() With {
                .Text = If(String.IsNullOrWhiteSpace(valeur), "N/A", valeur),
                .Dock = DockStyle.Fill,
                .Height = 28,
                .Font = New Font("Segoe UI", 9.5F),
                .ForeColor = Color.FromArgb(31, 41, 55),
                .TextAlign = ContentAlignment.MiddleLeft
            }

            parent.Controls.Add(lblKey, 0, rowIndex)
            parent.Controls.Add(lblValue, 1, rowIndex)
        End Sub

        Private Function ObtenirServeurActuel() As String
            Try
                Dim settings As SqlConnectionSettings = SqlConfigurationService.LoadSettings()
                If settings Is Nothing Then
                    Return String.Empty
                End If

                Dim serveur As String = If(String.IsNullOrWhiteSpace(settings.Server), String.Empty, settings.Server.Trim())
                If settings.Port.HasValue AndAlso settings.Port.Value > 0 AndAlso serveur.IndexOf(","c) < 0 Then
                    serveur &= "," & settings.Port.Value.ToString()
                End If
                If Not String.IsNullOrWhiteSpace(settings.DatabaseName) Then
                    serveur &= " / " & settings.DatabaseName
                End If
                Return serveur
            Catch
                Return String.Empty
            End Try
        End Function

        Private Function ObtenirDerniereSauvegarde() As String
            Try
                Dim backupService As New BackupService()
                Dim settings As BackupSettings = backupService.ChargerParametres()
                If settings Is Nothing Then
                    Return String.Empty
                End If

                Dim dernier As String = backupService.ObtenirDerniereSauvegarde(settings.BackupFolder)
                If String.IsNullOrWhiteSpace(dernier) Then
                    Return "Aucune"
                End If
                Dim info As New FileInfo(dernier)
                Return info.LastWriteTime.ToString("yyyy-MM-dd HH:mm") & " - " & info.Name
            Catch
                Return String.Empty
            End Try
        End Function
    End Class
End Namespace
