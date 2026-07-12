Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D

Namespace DevCommerc8ak
    Public Class FormAPropos
        Inherits Form

        ' --- Palette de Couleurs Enterprise ERP ---
        Private ReadOnly ColorBg As Color = Color.FromArgb(240, 242, 245)
        Private ReadOnly ColorHeader As Color = Color.FromArgb(44, 62, 80) ' Sidebar sombre pro
        Private ReadOnly ColorCardBg As Color = Color.White
        Private ReadOnly ColorAccent As Color = Color.FromArgb(59, 130, 246) ' Bleu Moderne
        Private ReadOnly ColorTextPrimary As Color = Color.FromArgb(33, 43, 54)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(99, 115, 129)
        Private ReadOnly ColorBorder As Color = Color.FromArgb(224, 224, 224)

        ' --- Polices ---
        Private ReadOnly FontMain As New Font("Segoe UI", 9.5F)
        Private ReadOnly FontBold As New Font("Segoe UI", 9.5F, FontStyle.Bold)
        Private ReadOnly FontTitle As New Font("Segoe UI", 18.0F, FontStyle.Bold)

        Public Sub New()
            ' Configuration de la Form
            Me.Text = "À Propos du Système"
            Me.Size = New Size(650, 520)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = ColorBg
            Me.Font = FontMain
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.DoubleBuffered = True

            BuildUi()
        End Sub

        Private Sub BuildUi()
            ' --- Layout Principal ---
            Dim pnlMain As New Panel() With {
                .Dock = DockStyle.Fill
            }
            Me.Controls.Add(pnlMain)

            ' --- En-tête (Header Sombre) ---
            Dim pnlHeader As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 120,
                .BackColor = ColorHeader,
                .Padding = New Padding(30, 20, 30, 20)
            }

            Dim lblTitle As New Label() With {
                .Text = "ERP COMMERCIAL PRO",
                .Font = FontTitle,
                .ForeColor = Color.White,
                .AutoSize = True,
                .Location = New Point(30, 25)
            }

            Dim lblSubtitle As New Label() With {
                .Text = "Solution de Gestion Commerciale Intégrée",
                .Font = FontMain,
                .ForeColor = Color.FromArgb(145, 158, 171),
                .AutoSize = True,
                .Location = New Point(30, 65)
            }

            pnlHeader.Controls.AddRange({lblTitle, lblSubtitle})
            pnlMain.Controls.Add(pnlHeader)

            ' --- Zone de Contenu (Cartes) ---
            Dim pnlContent As New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(30)
            }
            pnlMain.Controls.Add(pnlContent)

            ' Carte d'Information Système
            Dim cardInfo As New Panel() With {
                               .Height = 240,
                               .Width = 570,
                .BackColor = ColorCardBg,
                .Padding = New Padding(20),
                 .Location = New Point(30, 130)
            }
            StyliserCarte(cardInfo)
            pnlContent.Controls.Add(cardInfo)

            Dim grid As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 6
            }
            grid.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 180))
            grid.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100.0F))

            AjouterLigne(grid, 0, "Version du Logiciel", "2.5.0 Enterprise")
            AjouterLigne(grid, 1, "Type de Licence", "Production Illimitée")
            AjouterLigne(grid, 2, "Base de Données", "CommercialMagDB")
            AjouterLigne(grid, 3, "Instance SQL", ObtenirServeurActuel())
            AjouterLigne(grid, 4, "Dernier Backup", ObtenirDerniereSauvegarde())
            AjouterLigne(grid, 5, "Développeur Principal", "NTANTA ANDY")

            cardInfo.Controls.Add(grid)

            ' --- Pied de page (Footer) ---
            Dim pnlFooter As New Panel() With {
                .Dock = DockStyle.Bottom,
                .Height = 80,
                .Padding = New Padding(30, 15, 30, 15)
            }

            Dim lblCopyright As New Label() With {
                .Text = "© " & DateTime.Now.Year & " DevCommerc8ak. Tous droits réservés.",
                .ForeColor = ColorTextSecondary,
                .Font = New Font("Segoe UI", 8.5F),
                .AutoSize = True,
                .Location = New Point(30, 30)
            }

            Dim btnClose As New Button() With {
                .Text = "FERMER",
                .Size = New Size(120, 40),
                .Location = New Point(480, 20),
                .FlatStyle = FlatStyle.Flat,
                .BackColor = ColorAccent,
                .ForeColor = Color.White,
                .Font = FontBold,
                .Cursor = Cursors.Hand
            }
            btnClose.FlatAppearance.BorderSize = 0
            AddHandler btnClose.Click, Sub() Me.Close()

            pnlFooter.Controls.AddRange({lblCopyright, btnClose})
            pnlMain.Controls.Add(pnlFooter)
        End Sub

        ' --- Helpers de Design ---

        Private Sub StyliserCarte(pnl As Panel)
            AddHandler pnl.Paint, Sub(s, e)
                                      Dim rect As New Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1)
                                      e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                                      Using pen As New Pen(ColorBorder, 1)
                                          e.Graphics.DrawRectangle(pen, rect)
                                      End Using
                                  End Sub
        End Sub

        Private Sub AjouterLigne(parent As TableLayoutPanel, rowIndex As Integer, libelle As String, valeur As String)
            parent.RowStyles.Add(New RowStyle(SizeType.Absolute, 35))

            Dim lblKey As New Label() With {
                .Text = libelle.ToUpper(),
                .Dock = DockStyle.Fill,
                .Font = New Font("Segoe UI", 8.0F, FontStyle.Bold),
                .ForeColor = ColorTextSecondary,
                .TextAlign = ContentAlignment.MiddleLeft
            }
            Dim lblValue As New Label() With {
                .Text = If(String.IsNullOrWhiteSpace(valeur), "NON CONFIGURÉ", valeur),
                .Dock = DockStyle.Fill,
                .Font = FontMain,
                .ForeColor = ColorTextPrimary,
                .TextAlign = ContentAlignment.MiddleLeft
            }

            parent.Controls.Add(lblKey, 0, rowIndex)
            parent.Controls.Add(lblValue, 1, rowIndex)
        End Sub

        ' --- Logique Métier (Réintégrée et Fonctionnelle) ---

        Private Function ObtenirServeurActuel() As String
            Try
                Dim settings As SqlConnectionSettings = SqlConfigurationService.LoadSettings()
                If settings Is Nothing Then Return String.Empty

                Dim serveur As String = If(String.IsNullOrWhiteSpace(settings.Server), String.Empty, settings.Server.Trim())
                If settings.Port.HasValue AndAlso settings.Port.Value > 0 AndAlso serveur.IndexOf(","c) < 0 Then
                    serveur &= ":" & settings.Port.Value.ToString()
                End If
                If Not String.IsNullOrWhiteSpace(settings.DatabaseName) Then
                    serveur &= " (" & settings.DatabaseName & ")"
                End If
                Return serveur
            Catch
                Return "Erreur de lecture"
            End Try
        End Function

        Private Function ObtenirDerniereSauvegarde() As String
            Try
                Dim backupService As New BackupService()
                Dim settings As BackupSettings = backupService.ChargerParametres()
                If settings Is Nothing Then Return "Non configuré"

                Dim dernier As String = backupService.ObtenirDerniereSauvegarde(settings.BackupFolder)
                If String.IsNullOrWhiteSpace(dernier) Then Return "Aucune sauvegarde"

                Dim info As New FileInfo(dernier)
                Return info.LastWriteTime.ToString("dd/MM/yyyy HH:mm")
            Catch
                Return "Indisponible"
            End Try
        End Function
    End Class
End Namespace
