Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireEvaluationDetails
        Inherits Form

        Private ReadOnly _dateDebut As Date
        Private ReadOnly _dateFin As Date
        Private ReadOnly _beneficeRealise As Decimal
        Private ReadOnly _depensesTotal As Decimal
        Private ReadOnly _chargesSansRecette As Decimal
        Private ReadOnly _beneficeNet As Decimal
        Private ReadOnly _margeBeneficiairePourcentage As Decimal
        Private ReadOnly _evaluation As String

        Private ReadOnly gridSeuils As DataGridView

        Public Sub New(dateDebut As Date, dateFin As Date, beneficeRealise As Decimal, depensesTotal As Decimal, chargesSansRecette As Decimal, beneficeNet As Decimal, margeBeneficiairePourcentage As Decimal, evaluation As String)
            _dateDebut = dateDebut
            _dateFin = dateFin
            _beneficeRealise = beneficeRealise
            _depensesTotal = depensesTotal
            _chargesSansRecette = chargesSansRecette
            _beneficeNet = beneficeNet
            _margeBeneficiairePourcentage = margeBeneficiairePourcentage
            _evaluation = If(String.IsNullOrWhiteSpace(evaluation), "-", evaluation.Trim())

            Me.Text = "Détail évaluation"
            Me.StartPosition = FormStartPosition.CenterParent
            Me.WindowState = FormWindowState.Maximized
            Me.BackColor = Color.FromArgb(248, 249, 250)
            Me.DoubleBuffered = True

            Dim header As New Panel() With {.Dock = DockStyle.Top, .Height = 112, .BackColor = Color.White, .Padding = New Padding(24, 16, 24, 16)}
            Dim lblTitre As New Label() With {.Text = "Détails de l'évaluation de rentabilité", .ForeColor = Color.FromArgb(33, 33, 33), .Font = New Font("Segoe UI", 18.0F, FontStyle.Bold), .AutoSize = True, .Left = 24, .Top = 12}
            Dim lblPeriode As New Label() With {.Text = "Période : " & _dateDebut.ToString("dd/MM/yyyy") & " au " & _dateFin.ToString("dd/MM/yyyy"), .ForeColor = Color.FromArgb(90, 90, 90), .Font = New Font("Segoe UI", 10.0F, FontStyle.Regular), .AutoSize = True, .Left = 26, .Top = 48}

            Dim btnFermer As New Button() With {.Text = "Fermer", .Width = 110, .Height = 36, .BackColor = Color.FromArgb(63, 81, 181), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Font = New Font("Segoe UI", 10.0F, FontStyle.Bold), .Cursor = Cursors.Hand, .Dock = DockStyle.Fill}
            btnFermer.FlatAppearance.BorderSize = 0
            AddHandler btnFermer.Click, Sub() Me.Close()
            Dim pnlAction As New Panel() With {.Dock = DockStyle.Right, .Width = 150, .Padding = New Padding(0, 14, 0, 14), .BackColor = Color.Transparent}
            pnlAction.Controls.Add(btnFermer)
            header.Controls.Add(lblTitre)
            header.Controls.Add(lblPeriode)
            header.Controls.Add(pnlAction)

            Dim pnlSynthese As New TableLayoutPanel() With {.Dock = DockStyle.Top, .Height = 170, .ColumnCount = 4, .RowCount = 1, .BackColor = Color.Transparent, .Padding = New Padding(16, 16, 16, 8)}
            For i As Integer = 1 To 4
                pnlSynthese.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25.0F))
            Next
            pnlSynthese.RowStyles.Add(New RowStyle(SizeType.Percent, 100.0F))

            pnlSynthese.Controls.Add(CreerCarte("Bénéfice réalisé", Color.FromArgb(76, 175, 80), FormatageGlobal.FormatMontant(_beneficeRealise)), 0, 0)
            pnlSynthese.Controls.Add(CreerCarte("Dépenses", Color.FromArgb(255, 152, 0), FormatageGlobal.FormatMontant(_depensesTotal)), 1, 0)
            pnlSynthese.Controls.Add(CreerCarte("Charges sans recette", Color.FromArgb(209, 52, 56), FormatageGlobal.FormatMontant(_chargesSansRecette)), 2, 0)
            pnlSynthese.Controls.Add(CreerCarte("Bénéfice net", Color.FromArgb(0, 150, 136), FormatageGlobal.FormatMontant(_beneficeNet)), 3, 0)

            Dim lblNote As New Label() With {.Dock = DockStyle.Top, .Height = 28, .Text = "L'évaluation reprend le bénéfice net et le compare aux seuils de rentabilité.", .ForeColor = Color.FromArgb(90, 90, 90), .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic), .Padding = New Padding(18, 2, 18, 0)}

            Dim lblEvaluation As New Label() With {.Dock = DockStyle.Top, .Height = 30, .Text = "Évaluation actuelle : " & _evaluation, .ForeColor = Color.FromArgb(63, 81, 181), .Font = New Font("Segoe UI", 11.0F, FontStyle.Bold), .Padding = New Padding(18, 0, 18, 0)}

            gridSeuils = New DataGridView() With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .AutoGenerateColumns = True, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .RowHeadersVisible = False, .BackgroundColor = Color.White, .BorderStyle = BorderStyle.None, .EnableHeadersVisualStyles = False, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .Font = New Font("Segoe UI", 9.5F, FontStyle.Regular), .GridColor = Color.FromArgb(225, 230, 235)}
            gridSeuils.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 242, 245)
            gridSeuils.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(33, 33, 33)
            gridSeuils.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
            gridSeuils.ColumnHeadersHeight = 38
            gridSeuils.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 234, 246)
            gridSeuils.DefaultCellStyle.SelectionForeColor = Color.FromArgb(33, 33, 33)

            Dim dt As New DataTable()
            dt.Columns.Add("Critère", GetType(String))
            dt.Columns.Add("Valeur", GetType(String))
            dt.Columns.Add("Lecture", GetType(String))
            dt.Rows.Add("Marge bénéficiaire", _margeBeneficiairePourcentage.ToString("N2") & " %", DeterminerLecture())
            dt.Rows.Add("Seuil critique", "< 0 %", "CRITIQUE / PERTE")
            dt.Rows.Add("Point mort", "= 0 %", "POINT MORT")
            dt.Rows.Add("Faible rentabilité", "< 10 %", "FAIBLE RENTABILITÉ")
            dt.Rows.Add("Progression", "10 % à 25 %", "PROGRÈS")
            dt.Rows.Add("Bonne rentabilité", "> 25 %", "BONNE RENTABILITÉ")

            gridSeuils.DataSource = dt
            ConfigurerGrille()

            Dim container As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 4, .Padding = New Padding(0)}
            container.RowStyles.Add(New RowStyle(SizeType.Absolute, 170))
            container.RowStyles.Add(New RowStyle(SizeType.Absolute, 30))
            container.RowStyles.Add(New RowStyle(SizeType.Absolute, 28))
            container.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            container.Controls.Add(pnlSynthese, 0, 0)
            container.Controls.Add(lblNote, 0, 1)
            container.Controls.Add(lblEvaluation, 0, 2)
            container.Controls.Add(gridSeuils, 0, 3)

            Me.Controls.Add(container)
            Me.Controls.Add(header)
        End Sub

        Private Sub ConfigurerGrille()
            If gridSeuils.Columns.Count = 0 Then
                Return
            End If
            gridSeuils.Columns("Critère").Width = 220
            gridSeuils.Columns("Valeur").Width = 160
            gridSeuils.Columns("Lecture").Width = 220
        End Sub

        Private Function DeterminerLecture() As String
            Dim texte As String = _evaluation.ToUpperInvariant()
            If texte.Contains("CRITIQUE") OrElse texte.Contains("PERTE") Then
                Return "Résultat négatif, la structure consomme plus qu'elle ne produit."
            End If
            If texte.Contains("POINT MORT") Then
                Return "Les charges absorbent pratiquement tout le bénéfice."
            End If
            If texte.Contains("FAIBLE") Then
                Return "Rentabilité faible, marge encore fragile."
            End If
            If texte.Contains("PROGR") Then
                Return "Tendance positive, rentabilité en amélioration."
            End If
            Return "Rentabilité jugée satisfaisante sur la période."
        End Function

        Private Function CreerCarte(titre As String, couleur As Color, valeur As String) As Panel
            Dim card As New Panel() With {.BackColor = Color.White, .BorderStyle = BorderStyle.FixedSingle, .Dock = DockStyle.Fill, .Margin = New Padding(8), .Padding = New Padding(14)}
            Dim bande As New Panel() With {.Dock = DockStyle.Left, .Width = 6, .BackColor = couleur}
            Dim lblTitre As New Label() With {.Text = titre, .Dock = DockStyle.Top, .Height = 24, .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold), .ForeColor = Color.FromArgb(90, 90, 90)}
            Dim lblValeur As New Label() With {.Text = valeur, .Dock = DockStyle.Fill, .Font = New Font("Segoe UI", 17.0F, FontStyle.Bold), .ForeColor = couleur, .TextAlign = ContentAlignment.MiddleLeft, .AutoEllipsis = True}
            card.Controls.Add(lblValeur)
            card.Controls.Add(lblTitre)
            card.Controls.Add(bande)
            Return card
        End Function
    End Class
End Namespace
