Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireBeneficeNetDetails
        Inherits Form

        Private ReadOnly _dateDebut As Date
        Private ReadOnly _dateFin As Date
        Private ReadOnly _details As DataTable
        Private ReadOnly _beneficeRealise As Decimal
        Private ReadOnly _depensesTotal As Decimal
        Private ReadOnly _chargesSansRecette As Decimal
        Private ReadOnly _beneficeNet As Decimal

        Private ReadOnly gridDetails As DataGridView

        Public Sub New(dateDebut As Date, dateFin As Date, details As DataTable, beneficeRealise As Decimal, depensesTotal As Decimal, chargesSansRecette As Decimal, beneficeNet As Decimal)
            _dateDebut = dateDebut
            _dateFin = dateFin
            _details = details
            _beneficeRealise = beneficeRealise
            _depensesTotal = depensesTotal
            _chargesSansRecette = chargesSansRecette
            _beneficeNet = beneficeNet

            Me.Text = "Détail bénéfice net"
            Me.StartPosition = FormStartPosition.CenterParent
            Me.WindowState = FormWindowState.Maximized
            Me.BackColor = Color.FromArgb(248, 249, 250)
            Me.DoubleBuffered = True

            Dim header As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 112,
                .BackColor = Color.White,
                .Padding = New Padding(24, 16, 24, 16)
            }

            Dim lblTitre As New Label() With {
                .Text = "Détails du bénéfice net réalisé",
                .ForeColor = Color.FromArgb(33, 33, 33),
                .Font = New Font("Segoe UI", 18.0F, FontStyle.Bold),
                .AutoSize = True,
                .Left = 24,
                .Top = 12
            }

            Dim lblPeriode As New Label() With {
                .Text = "Période : " & _dateDebut.ToString("dd/MM/yyyy") & " au " & _dateFin.ToString("dd/MM/yyyy"),
                .ForeColor = Color.FromArgb(90, 90, 90),
                .Font = New Font("Segoe UI", 10.0F, FontStyle.Regular),
                .AutoSize = True,
                .Left = 26,
                .Top = 48
            }

            Dim btnFermer As New Button() With {
                .Text = "Fermer",
                .Width = 110,
                .Height = 36,
                .BackColor = Color.FromArgb(63, 81, 181),
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold),
                .Cursor = Cursors.Hand,
                .Dock = DockStyle.Fill
            }
            btnFermer.FlatAppearance.BorderSize = 0
            AddHandler btnFermer.Click, Sub() Me.Close()

            Dim pnlAction As New Panel() With {.Dock = DockStyle.Right, .Width = 150, .Padding = New Padding(0, 14, 0, 14), .BackColor = Color.Transparent}
            pnlAction.Controls.Add(btnFermer)

            header.Controls.Add(lblTitre)
            header.Controls.Add(lblPeriode)
            header.Controls.Add(pnlAction)

            Dim pnlSynthese As New TableLayoutPanel() With {
                .Dock = DockStyle.Top,
                .Height = 170,
                .ColumnCount = 4,
                .RowCount = 1,
                .BackColor = Color.Transparent,
                .Padding = New Padding(16, 16, 16, 8)
            }
            For i As Integer = 1 To 4
                pnlSynthese.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0F))
            Next
            pnlSynthese.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

            pnlSynthese.Controls.Add(CreerCarteSynthese("Bénéfice réalisé", Color.FromArgb(76, 175, 80), FormatageGlobal.FormatMontant(_beneficeRealise)), 0, 0)
            pnlSynthese.Controls.Add(CreerCarteSynthese("Dépenses", Color.FromArgb(255, 152, 0), FormatageGlobal.FormatMontant(_depensesTotal)), 1, 0)
            pnlSynthese.Controls.Add(CreerCarteSynthese("Charges sans recette", Color.FromArgb(209, 52, 56), FormatageGlobal.FormatMontant(_chargesSansRecette)), 2, 0)
            pnlSynthese.Controls.Add(CreerCarteSynthese("Bénéfice net", Color.FromArgb(0, 150, 136), FormatageGlobal.FormatMontant(_beneficeNet)), 3, 0)

            Dim lblNote As New Label() With {
                .Dock = DockStyle.Top,
                .Height = 44,
                .Text = "Le détail ci-dessous regroupe les dépenses et les charges qui réduisent réellement le résultat final. Les créances clients sont affichées à titre informatif et ne sont pas déduites du bénéfice net.",
                .ForeColor = Color.FromArgb(90, 90, 90),
                .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic),
                .Padding = New Padding(18, 2, 18, 0)
            }

            Dim lblFormule As New Label() With {
                .Dock = DockStyle.Top,
                .Height = 24,
                .Text = "Formule: Bénéfice net = Bénéfice réalisé - Dépenses - Charges sans recette",
                .ForeColor = Color.FromArgb(120, 120, 120),
                .Font = New Font("Segoe UI", 9.0F, FontStyle.Regular),
                .Padding = New Padding(18, 0, 18, 4)
            }

            gridDetails = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .AutoGenerateColumns = True,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .RowHeadersVisible = False,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None,
                .EnableHeadersVisualStyles = False,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .Font = New Font("Segoe UI", 9.5F, FontStyle.Regular),
                .GridColor = Color.FromArgb(225, 230, 235)
            }
            gridDetails.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 242, 245)
            gridDetails.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(33, 33, 33)
            gridDetails.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
            gridDetails.ColumnHeadersHeight = 38
            gridDetails.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 234, 246)
            gridDetails.DefaultCellStyle.SelectionForeColor = Color.FromArgb(33, 33, 33)

            Me.Controls.Add(gridDetails)
            Me.Controls.Add(lblNote)
            Me.Controls.Add(lblFormule)
            Me.Controls.Add(pnlSynthese)
            Me.Controls.Add(header)

            AddHandler Me.Load, AddressOf FormulaireBeneficeNetDetails_Load
        End Sub

        Private Sub FormulaireBeneficeNetDetails_Load(sender As Object, e As EventArgs)
            gridDetails.DataSource = _details
            ConfigurerGrille()
            AppliquerStyleLignes()
        End Sub

        Private Sub ConfigurerGrille()
            If gridDetails.Columns.Count = 0 Then
                Return
            End If

            ConfigurerColonne("Ordre", "", 0, True)
            ConfigurerColonne("Rubrique", "Rubrique", 160)
            ConfigurerColonne("Categorie", "Catégorie / détail", 240)
            ConfigurerColonne("QuantitePieces", "Quantité pièces", 140, False, "N0")
            ConfigurerColonne("Montant", "Montant (FC)", 150, False, "N0")
            ConfigurerColonne("Commentaire", "Commentaire", 320)
        End Sub

        Private Sub ConfigurerColonne(nom As String, titre As String, largeur As Integer, Optional cacher As Boolean = False, Optional format As String = Nothing)
            If Not gridDetails.Columns.Contains(nom) Then
                Return
            End If

            Dim col As DataGridViewColumn = gridDetails.Columns(nom)
            col.Visible = Not cacher
            If cacher Then
                Return
            End If

            col.HeaderText = titre
            col.Width = largeur
            If Not String.IsNullOrWhiteSpace(format) Then
                col.DefaultCellStyle.Format = format
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            End If
        End Sub

        Private Sub AppliquerStyleLignes()
            For Each row As DataGridViewRow In gridDetails.Rows
                If row.IsNewRow Then
                    Continue For
                End If

                Dim ordre As Integer = 0
                If gridDetails.Columns.Contains("Ordre") AndAlso row.Cells("Ordre").Value IsNot Nothing Then
                    Integer.TryParse(Convert.ToString(row.Cells("Ordre").Value), ordre)
                End If

                If ordre = 0 OrElse ordre = 1 OrElse ordre = 2 OrElse ordre = 99 Then
                    row.DefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
                    row.DefaultCellStyle.BackColor = If(ordre = 99, Color.FromArgb(232, 245, 233), Color.FromArgb(250, 250, 250))
                End If
            Next
        End Sub

        Private Function CreerCarteSynthese(titre As String, couleur As Color, valeur As String) As Panel
            Dim card As New Panel() With {
                .BackColor = Color.White,
                .BorderStyle = BorderStyle.FixedSingle,
                .Dock = DockStyle.Fill,
                .Margin = New Padding(8),
                .Padding = New Padding(14)
            }

            Dim bande As New Panel() With {.Dock = DockStyle.Left, .Width = 6, .BackColor = couleur}
            Dim lblTitre As New Label() With {
                .Text = titre,
                .Dock = DockStyle.Top,
                .Height = 24,
                .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold),
                .ForeColor = Color.FromArgb(90, 90, 90)
            }
            Dim lblValeur As New Label() With {
                .Text = valeur,
                .Dock = DockStyle.Fill,
                .Font = New Font("Segoe UI", 17.0F, FontStyle.Bold),
                .ForeColor = couleur,
                .TextAlign = ContentAlignment.MiddleLeft,
                .AutoEllipsis = True
            }

            card.Controls.Add(lblValeur)
            card.Controls.Add(lblTitre)
            card.Controls.Add(bande)
            Return card
        End Function
    End Class
End Namespace
