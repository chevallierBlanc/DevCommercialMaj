Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class SplashForm
        Inherits Form

        Private ReadOnly picLogo As PictureBox
        Private ReadOnly lblTitre As Label

        Public Sub New()
            Me.FormBorderStyle = FormBorderStyle.None
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Width = 500
            Me.Height = 300

            picLogo = New PictureBox() With {.Left = 20, .Top = 20, .Width = 120, .Height = 120, .SizeMode = PictureBoxSizeMode.Zoom}
            lblTitre = New Label() With {.Left = 160, .Top = 60, .AutoSize = True, .Font = New Font("Segoe UI", 16, FontStyle.Bold), .Text = "DevCommerc8ak"}

            Me.Controls.Add(picLogo)
            Me.Controls.Add(lblTitre)

            ChargerLogo()
            ThemeHelper.AppliquerTheme(Me)
        End Sub

        Private Sub ChargerLogo()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim paramService As New ParametreService(New ParametreRepository(dal))
                Dim p As ParametreDTO = paramService.Charger()

                Dim path As String = ""
                If p IsNot Nothing Then path = p.LogoPath
                If path <> "" AndAlso File.Exists(path) Then
                    picLogo.Image = Image.FromFile(path)
                Else
                    Dim baseDir As String = AppDomain.CurrentDomain.BaseDirectory
                    Dim defaultLogo As String = Path.Combine(baseDir, "Resources", "images", "logo.png")
                    If File.Exists(defaultLogo) Then
                        picLogo.Image = Image.FromFile(defaultLogo)
                    End If
                End If
            Catch
            End Try
        End Sub
    End Class
End Namespace
