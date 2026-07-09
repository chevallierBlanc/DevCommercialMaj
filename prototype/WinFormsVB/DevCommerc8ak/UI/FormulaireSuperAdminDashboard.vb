Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireSuperAdminDashboard
        Inherits Form

        Private ReadOnly _ouvrirStockInitial As Action
        Private ReadOnly _ouvrirRoles As Action
        Private ReadOnly _ouvrirJournal As Action

        Public Sub New(ouvrirStockInitial As Action, ouvrirRoles As Action, ouvrirJournal As Action)
            _ouvrirStockInitial = ouvrirStockInitial
            _ouvrirRoles = ouvrirRoles
            _ouvrirJournal = ouvrirJournal

            Text = "Interfaces techniques SuperAdmin"
            BackColor = Color.FromArgb(245, 247, 250)
            AutoScroll = True
            MinimumSize = New Size(900, 620)

            Dim layout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2,
                .Padding = New Padding(24)
            }
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 110))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim header As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(20)}
            Dim titre As New Label() With {
                .Text = "Tableau de bord technique SuperAdmin",
                .Font = New Font("Segoe UI", 18, FontStyle.Bold),
                .ForeColor = Color.FromArgb(31, 41, 55),
                .AutoSize = True,
                .Top = 16,
                .Left = 20
            }
            Dim sousTitre As New Label() With {
                .Text = "Accès rapide aux outils techniques : stock initial, rôles et journal des actions.",
                .Font = New Font("Segoe UI", 10),
                .ForeColor = Color.FromArgb(107, 114, 128),
                .AutoSize = True,
                .Top = 58,
                .Left = 22
            }
            header.Controls.Add(titre)
            header.Controls.Add(sousTitre)

            Dim cards As New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .WrapContents = True,
                .Padding = New Padding(0, 12, 0, 12)
            }

            cards.Controls.Add(CreerCarte("Stock initial technique", "Préparer et injecter rapidement les stocks initiaux avec conversions et prix.", Color.FromArgb(14, 116, 144), Sub() If _ouvrirStockInitial IsNot Nothing Then _ouvrirStockInitial()))
            cards.Controls.Add(CreerCarte("Rôles & privilèges", "Créer, modifier et restreindre les rôles applicatifs.", Color.FromArgb(79, 70, 229), Sub() If _ouvrirRoles IsNot Nothing Then _ouvrirRoles()))
            cards.Controls.Add(CreerCarte("Journal actions", "Consulter les audits utilisateurs et les événements métier.", Color.FromArgb(22, 163, 74), Sub() If _ouvrirJournal IsNot Nothing Then _ouvrirJournal()))

            layout.Controls.Add(header, 0, 0)
            layout.Controls.Add(cards, 0, 1)
            Controls.Add(layout)
        End Sub

        Private Function CreerCarte(titre As String, description As String, accent As Color, action As Action) As Control
            Dim card As New Panel() With {
                .Width = 260,
                .Height = 220,
                .Margin = New Padding(12),
                .Padding = New Padding(18),
                .BackColor = Color.White,
                .Cursor = Cursors.Hand
            }

            Dim bandeau As New Panel() With {.Dock = DockStyle.Top, .Height = 6, .BackColor = accent}
            Dim lblTitre As New Label() With {
                .Text = titre,
                .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                .ForeColor = Color.FromArgb(31, 41, 55),
                .Dock = DockStyle.Top,
                .Height = 54,
                .Padding = New Padding(0, 18, 0, 0)
            }
            Dim lblDesc As New Label() With {
                .Text = description,
                .Font = New Font("Segoe UI", 9.5F),
                .ForeColor = Color.FromArgb(75, 85, 99),
                .Dock = DockStyle.Fill
            }
            Dim btn As New Button() With {
                .Text = "Ouvrir",
                .Dock = DockStyle.Bottom,
                .Height = 38,
                .BackColor = accent,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat
            }
            btn.FlatAppearance.BorderSize = 0

            AddHandler btn.Click, Sub(sender, e) action()
            AddHandler card.Click, Sub(sender, e) action()
            AddHandler lblTitre.Click, Sub(sender, e) action()
            AddHandler lblDesc.Click, Sub(sender, e) action()

            card.Controls.Add(lblDesc)
            card.Controls.Add(btn)
            card.Controls.Add(lblTitre)
            card.Controls.Add(bandeau)
            Return card
        End Function
    End Class
End Namespace
