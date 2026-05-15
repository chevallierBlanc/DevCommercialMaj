Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Module ThemeHelper
        Private _modeSombre As Boolean

        Public Sub DefinirModeSombre(actif As Boolean)
            _modeSombre = actif
        End Sub

        Public Sub AppliquerTheme(form As Form)
            Dim primaire As Color = If(_modeSombre, Color.FromArgb(20, 22, 28), Color.FromArgb(30, 42, 68))
            Dim secondaire As Color = If(_modeSombre, Color.FromArgb(34, 36, 44), Color.FromArgb(242, 244, 247))
            Dim texte As Color = If(_modeSombre, Color.White, Color.FromArgb(30, 30, 30))

            form.BackColor = secondaire
            form.Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)
            AppliquerSurControles(form.Controls, primaire, secondaire, texte)
        End Sub

        Private Sub AppliquerSurControles(controls As Control.ControlCollection, primaire As Color, secondaire As Color, texte As Color)
            For Each c As Control In controls
                If TypeOf c Is Panel Then
                    Dim p As Panel = CType(c, Panel)
                    If p.Dock = DockStyle.Left OrElse p.Dock = DockStyle.Top Then
                        p.BackColor = primaire
                    Else
                        p.BackColor = secondaire
                    End If
                ElseIf TypeOf c Is Button Then
                    Dim b As Button = CType(c, Button)
                    b.BackColor = primaire
                    b.ForeColor = Color.White
                    b.FlatStyle = FlatStyle.Flat
                    b.FlatAppearance.BorderSize = 0
                ElseIf TypeOf c Is DataGridView Then
                    Dim g As DataGridView = CType(c, DataGridView)
                    g.BackgroundColor = If(_modeSombre, Color.FromArgb(28, 30, 36), Color.White)
                    g.BorderStyle = BorderStyle.FixedSingle
                    g.EnableHeadersVisualStyles = False
                    g.ColumnHeadersDefaultCellStyle.BackColor = primaire
                    g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
                ElseIf TypeOf c Is TextBox OrElse TypeOf c Is ComboBox OrElse TypeOf c Is DateTimePicker Then
                    c.BackColor = If(_modeSombre, Color.FromArgb(45, 47, 55), Color.White)
                    c.ForeColor = texte
                ElseIf TypeOf c Is Label Then
                    c.ForeColor = texte
                End If

                If c.HasChildren Then
                    AppliquerSurControles(c.Controls, primaire, secondaire, texte)
                End If
            Next
        End Sub
    End Module
End Namespace
