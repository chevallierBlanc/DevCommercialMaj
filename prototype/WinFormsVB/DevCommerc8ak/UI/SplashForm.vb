Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D

Namespace DevCommerc8ak
    Public Class SplashForm
        Inherits Form

        ' --- Palette de Couleurs Identité Visuelle ---
        Private ReadOnly ColorBg As Color = Color.FromArgb(28, 35, 49) ' Bleu Nuit Profond (Identité MainForm)
        Private ReadOnly ColorAccent As Color = Color.FromArgb(59, 130, 246) ' Bleu Moderne
        Private ReadOnly ColorWhite As Color = Color.White
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(145, 158, 171)

        ' --- Composants (Noms conservés) ---
        Private ReadOnly picLogo As PictureBox
        Private ReadOnly lblTitre As Label

        ' --- Nouveaux éléments visuels pour l'optimisation UX ---
        Private ReadOnly pnlProgressBg As Panel
        Private ReadOnly pnlProgressBar As Panel
        Private ReadOnly lblStatus As Label
        Private ReadOnly lblVersion As Label
        Private ReadOnly timerAnim As Timer
        Private ReadOnly timerFermeture As Timer
        Private _progressWidth As Integer = 0

        Public Sub New()
            ' Configuration de la Form
            Me.FormBorderStyle = FormBorderStyle.None
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Size = New Size(550, 350)
            Me.BackColor = ColorBg
            Me.DoubleBuffered = True

            ' --- Logo (picLogo) ---
            picLogo = New PictureBox() With {
                .Size = New Size(100, 100),
                .Location = New Point((Me.Width - 100) \ 2, 60),
                .SizeMode = PictureBoxSizeMode.Zoom,
                .BackColor = Color.Transparent
            }

            ' --- Titre (lblTitre) ---
            lblTitre = New Label() With {
                .Text = "COMMERCIAL PRO",
                .Font = New Font("Segoe UI", 22, FontStyle.Bold),
                .ForeColor = ColorWhite,
                .AutoSize = False,
                .Size = New Size(Me.Width, 50),
                .Location = New Point(0, 170),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            ' --- Sous-titre / Status ---
            lblStatus = New Label() With {
                .Text = "Initialisation du système...",
                .Font = New Font("Segoe UI", 9),
                .ForeColor = ColorTextSecondary,
                .AutoSize = False,
                .Size = New Size(Me.Width, 20),
                .Location = New Point(0, 220),
                .TextAlign = ContentAlignment.MiddleCenter
            }

            ' --- Barre de Progression ---
            pnlProgressBg = New Panel() With {
                .Size = New Size(350, 4),
                .Location = New Point((Me.Width - 350) \ 2, 255),
                .BackColor = Color.FromArgb(45, 55, 75)
            }

            pnlProgressBar = New Panel() With {
                .Size = New Size(0, 4),
                .Location = New Point(0, 0),
                .BackColor = ColorAccent
            }
            pnlProgressBg.Controls.Add(pnlProgressBar)

            ' --- Version ---
            lblVersion = New Label() With {
                .Text = "Version 2.5.0",
                .Font = New Font("Segoe UI", 8),
                .ForeColor = Color.FromArgb(75, 85, 105),
                .AutoSize = True,
                .Location = New Point(Me.Width - 85, Me.Height - 25)
            }

            ' --- Animation ---
            timerAnim = New Timer() With {.Interval = 20}
            AddHandler timerAnim.Tick, AddressOf AnimerProgression
            timerAnim.Start()

            timerFermeture = New Timer() With {.Interval = 2000}
            AddHandler timerFermeture.Tick, AddressOf FermerAutomatiquement
            timerFermeture.Start()

            ' Assemblage
            Me.Controls.AddRange({picLogo, lblTitre, lblStatus, pnlProgressBg, lblVersion})

            ' Chargement et Thème
            ChargerLogo()
            ThemeHelper.AppliquerTheme(Me)

            ' Effet de bordure subtile
            AddHandler Me.Paint, Sub(s, e)
                                     Using pen As New Pen(Color.FromArgb(45, 55, 75), 1)
                                         e.Graphics.DrawRectangle(pen, 0, 0, Me.Width - 1, Me.Height - 1)
                                     End Using
                                 End Sub
        End Sub

        Private Sub AnimerProgression(sender As Object, e As EventArgs)
            If _progressWidth < 350 Then
                _progressWidth += 2
                pnlProgressBar.Width = _progressWidth

                ' Simulation de changement de statut
                If _progressWidth = 100 Then lblStatus.Text = "Chargement des modules..."
                If _progressWidth = 200 Then lblStatus.Text = "Connexion à la base de données..."
                If _progressWidth = 300 Then lblStatus.Text = "Préparation de l'interface..."
            Else
                timerAnim.Stop()
                ' Ici, le formulaire se fermerait normalement pour ouvrir le Login
            End If
        End Sub

        Private Sub FermerAutomatiquement(sender As Object, e As EventArgs)
            timerFermeture.Stop()
            Close()
        End Sub

        Private Sub ChargerLogo()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim paramService As New ParametreService(New ParametreRepository(dal))
                Dim p As ParametreDTO = paramService.Charger()
                Dim pathLogo As String = LogoPathHelper.GetLogoPath(p)
                If Not String.IsNullOrEmpty(pathLogo) AndAlso File.Exists(pathLogo) Then
                    picLogo.Image = Image.FromFile(pathLogo)
                End If
            Catch
                ' Fallback silencieux
            End Try
        End Sub

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            timerAnim.Stop()
            timerFermeture.Stop()
            MyBase.OnFormClosed(e)
        End Sub
    End Class
End Namespace
