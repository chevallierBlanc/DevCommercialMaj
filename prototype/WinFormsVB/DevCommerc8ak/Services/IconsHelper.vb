Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Module IconsHelper
        Private Function ChargerBitmap(nom As String, fallback As Icon) As Bitmap
            Dim baseDir As String = AppDomain.CurrentDomain.BaseDirectory
            Dim pathPng As String = Path.Combine(baseDir, "Resources", "icons", nom & ".png")
            Dim pathIco As String = Path.Combine(baseDir, "Resources", "icons", nom & ".ico")

            If File.Exists(pathPng) Then
                Return New Bitmap(pathPng)
            End If
            If File.Exists(pathIco) Then
                Using ic As New Icon(pathIco)
                    Return ic.ToBitmap()
                End Using
            End If
            Return fallback.ToBitmap()
        End Function

        Public Sub AppliquerIconeFormulaire(f As Form)
            f.Icon = SystemIcons.Application
        End Sub

        Public Sub AppliquerIconeBouton(b As Button, typeIcone As String)
            Dim bmp As Bitmap
            Select Case typeIcone.ToUpperInvariant()
                Case "FACTURIER"
                    bmp = ChargerBitmap("facturier", SystemIcons.Application)
                Case "CAISSE"
                    bmp = ChargerBitmap("caisse", SystemIcons.Shield)
                Case "ADMIN"
                    bmp = ChargerBitmap("admin", SystemIcons.WinLogo)
                Case "PRODUITS"
                    bmp = ChargerBitmap("produits", SystemIcons.Information)
                Case "CLIENTS"
                    bmp = ChargerBitmap("clients", SystemIcons.Information)
                Case "STOCK"
                    bmp = ChargerBitmap("stock", SystemIcons.Warning)
                Case "RAPPORTS"
                    bmp = ChargerBitmap("rapports", SystemIcons.Information)
                Case "PARAMETRES"
                    bmp = ChargerBitmap("parametres", SystemIcons.Information)
                Case "UTILISATEURS"
                    bmp = ChargerBitmap("utilisateurs", SystemIcons.Information)
                Case Else
                    bmp = SystemIcons.Information.ToBitmap()
            End Select

            b.Image = bmp
            b.ImageAlign = ContentAlignment.MiddleLeft
            b.TextAlign = ContentAlignment.MiddleRight
        End Sub
    End Module
End Namespace
