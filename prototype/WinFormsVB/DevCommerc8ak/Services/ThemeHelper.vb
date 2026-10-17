Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D

Namespace DevCommerc8ak
    ''' <summary>
    ''' Module centralisé pour la gestion de l'identité visuelle et de l'UX moderne.
    ''' </summary>
    Public Module ThemeHelper1
        Public ReadOnly CommercialEntete As Color = Color.FromArgb(52, 73, 94)
        Public ReadOnly CommercialEnteteGrille As Color = Color.FromArgb(245, 245, 245)
        Public ReadOnly CommercialAction As Color = Color.FromArgb(41, 128, 185)

        ' Application explicite seulement : ne change ni le theme global ni
        ' la selection des lignes ou les dimensions des controles existants.
        Public Sub StyliserEnteteCommerciale(grille As DataGridView)
            grille.EnableHeadersVisualStyles = False
            With grille.ColumnHeadersDefaultCellStyle
                .BackColor = CommercialEnteteGrille
                .SelectionBackColor = CommercialEnteteGrille
                .ForeColor = CommercialEntete
                .SelectionForeColor = CommercialEntete
            End With
        End Sub

        Public Sub StyliserActionCommerciale(bouton As Button, Optional principale As Boolean = False, Optional destructive As Boolean = False)
            bouton.FlatStyle = FlatStyle.Flat
            bouton.UseVisualStyleBackColor = False
            bouton.FlatAppearance.BorderSize = 1
            Dim actualiser As System.EventHandler = Sub(sender, e)
                                                       bouton.BackColor = If(Not bouton.Enabled, CommercialEnteteGrille, If(principale, CommercialAction, Color.White))
                                                       bouton.ForeColor = If(Not bouton.Enabled, Color.FromArgb(117, 117, 117), If(principale, Color.White, If(destructive, Color.FromArgb(192, 57, 43), CommercialEntete)))
                                                       bouton.FlatAppearance.BorderColor = If(Not bouton.Enabled, ColorBorder, If(principale, CommercialAction, If(destructive, Color.FromArgb(192, 57, 43), ColorBorder)))
                                                   End Sub
            bouton.FlatAppearance.MouseOverBackColor = If(principale, Color.FromArgb(34, 110, 160), If(destructive, Color.FromArgb(253, 240, 238), Color.FromArgb(232, 242, 250)))
            bouton.FlatAppearance.MouseDownBackColor = If(principale, Color.FromArgb(28, 94, 140), Color.FromArgb(225, 234, 242))
            AddHandler bouton.EnabledChanged, actualiser
            actualiser(bouton, System.EventArgs.Empty)
        End Sub

        ' --- Palette de Couleurs Windows 11 / Premium ---
        Public ReadOnly ColorPrimary As Color = Color.FromArgb(0, 120, 212)      ' Bleu Windows
        Public ReadOnly ColorAccent As Color = Color.FromArgb(0, 90, 158)       ' Bleu Foncé
        Public ReadOnly ColorBackground As Color = Color.FromArgb(243, 243, 243) ' Gris Neutre
        Public ReadOnly ColorCard As Color = Color.White                        ' Blanc Pur
        Public ReadOnly ColorText As Color = Color.FromArgb(32, 32, 32)         ' Noir Doux
        Public ReadOnly ColorTextSecondary As Color = Color.FromArgb(102, 102, 102) ' Gris Texte
        Public ReadOnly ColorSuccess As Color = Color.FromArgb(16, 124, 16)     ' Vert Succès
        Public ReadOnly ColorDanger As Color = Color.FromArgb(209, 52, 56)      ' Rouge Danger
        Public ReadOnly ColorWarning As Color = Color.FromArgb(157, 93, 0)      ' Orange Alerte
        Public ReadOnly ColorBorder As Color = Color.FromArgb(229, 229, 229)    ' Bordure Fine

        ' --- Typographie ---
        Public ReadOnly FontMain As New Font("Segoe UI Variable Display", 9.5F, FontStyle.Regular)
        Public ReadOnly FontBold As New Font("Segoe UI Variable Display", 9.5F, FontStyle.Bold)
        Public ReadOnly FontTitle As New Font("Segoe UI Variable Display", 14.0F, FontStyle.Bold)
        Public ReadOnly FontKPI As New Font("Segoe UI Variable Display", 20.0F, FontStyle.Bold)

        Private _modeSombre As Boolean = False

        Public Sub DefinirModeSombre(actif As Boolean)
            _modeSombre = actif
        End Sub

        ''' <summary>
        ''' Applique le thème moderne à un formulaire et à tous ses contrôles.
        ''' </summary>
        Public Sub AppliquerTheme(form As Form)
            If form Is Nothing Then Return

            form.BackColor = If(_modeSombre, Color.FromArgb(32, 32, 32), ColorBackground)
            form.Font = FontMain

            ' Optimisation du rendu
            'form.DoubleBuffered = True

            AppliquerSurControles(form.Controls)
        End Sub

        ''' <summary>
        ''' Parcourt récursivement les contrôles pour appliquer les styles spécifiques.
        ''' </summary>
        Private Sub AppliquerSurControles(controls As Control.ControlCollection)
            For Each c As Control In controls
                ' 1. Panels & Containers (Gestion des Cartes)
                If TypeOf c Is Panel Then
                    Dim p As Panel = DirectCast(c, Panel)
                    ' Si le panel n'est pas un header ou un menu, on le traite comme une carte
                    If p.Tag IsNot Nothing AndAlso p.Tag.ToString() = "Card" Then
                        p.BackColor = ColorCard
                        p.Padding = New Padding(15)
                        p.BorderStyle = BorderStyle.None
                    ElseIf p.Dock = DockStyle.Top OrElse p.Dock = DockStyle.Left Then
                        ' Headers ou Menus
                        p.BackColor = If(_modeSombre, Color.FromArgb(45, 45, 45), Color.White)
                    End If

                    ' 2. Boutons (Style Windows 11)
                ElseIf TypeOf c Is Button Then
                    Dim b As Button = DirectCast(c, Button)
                    b.FlatStyle = FlatStyle.Flat
                    b.FlatAppearance.BorderSize = 0
                    b.Cursor = Cursors.Hand
                    b.Font = FontBold

                    ' Style par défaut si non défini
                    If b.BackColor = SystemColors.Control OrElse b.BackColor = Color.Transparent Then
                        b.BackColor = ColorPrimary
                        b.ForeColor = Color.White
                    End If

                    ' 3. Grilles (DataGridView Moderne)
                ElseIf TypeOf c Is DataGridView Then
                    Dim g As DataGridView = DirectCast(c, DataGridView)
                    g.BackgroundColor = ColorCard
                    g.BorderStyle = BorderStyle.None
                    g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
                    g.GridColor = ColorBorder
                    g.EnableHeadersVisualStyles = False
                    g.RowTemplate.Height = 35
                    g.SelectionMode = DataGridViewSelectionMode.FullRowSelect

                    ' En-tête
                    g.ColumnHeadersDefaultCellStyle.BackColor = ColorBackground
                    g.ColumnHeadersDefaultCellStyle.ForeColor = ColorText
                    g.ColumnHeadersDefaultCellStyle.Font = FontBold
                    g.ColumnHeadersDefaultCellStyle.SelectionBackColor = ColorBackground

                    ' Cellules
                    g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 242, 252)
                    g.DefaultCellStyle.SelectionForeColor = ColorPrimary
                    g.DefaultCellStyle.Font = FontMain

                    ' 4. Champs de saisie
                ElseIf TypeOf c Is TextBox Then
                    Dim t As TextBox = DirectCast(c, TextBox)
                    t.BorderStyle = BorderStyle.FixedSingle
                    t.Font = FontMain

                ElseIf TypeOf c Is ComboBox Then
                    Dim cb As New ComboBox()
                    cb.FlatStyle = FlatStyle.Flat
                    cb.Font = FontMain

                    ' 5. Labels
                ElseIf TypeOf c Is Label Then
                    Dim l As Label = DirectCast(c, Label)
                    If l.Tag IsNot Nothing AndAlso l.Tag.ToString() = "Title" Then
                        l.Font = FontTitle
                        l.ForeColor = ColorPrimary
                    ElseIf l.Tag IsNot Nothing AndAlso l.Tag.ToString() = "KPI" Then
                        l.Font = FontKPI
                    Else
                        l.ForeColor = If(l.ForeColor = SystemColors.ControlText, ColorText, l.ForeColor)
                    End If
                End If

                ' Récursion pour les conteneurs
                If c.HasChildren Then
                    AppliquerSurControles(c.Controls)
                End If
            Next
        End Sub

        ''' <summary>
        ''' Helper pour créer une carte visuelle dynamiquement.
        ''' </summary>
        Public Function CreerCarte() As Panel
            Dim p As New Panel()
            p.BackColor = ColorCard
            p.Padding = New Padding(20)
            p.Tag = "Card"
            Return p
        End Function

        ''' <summary>
        ''' Applique un style de bouton spécifique (Succès, Danger, etc.)
        ''' </summary>
        Public Sub StyliserBouton(btn As Button, type As String)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.ForeColor = Color.White
            btn.Font = FontBold

            Select Case type.ToLower()
                Case "success" : btn.BackColor = ColorSuccess
                Case "danger" : btn.BackColor = ColorDanger
                Case "warning" : btn.BackColor = ColorWarning
                Case "primary" : btn.BackColor = ColorPrimary
                Case Else : btn.BackColor = Color.FromArgb(204, 204, 204) : btn.ForeColor = ColorText
            End Select
        End Sub
    End Module
End Namespace
