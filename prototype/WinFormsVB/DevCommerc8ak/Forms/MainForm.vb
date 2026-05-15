Imports System.Windows.Forms
Imports System.Drawing
Imports System.Configuration
Imports System.Data
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class MainForm
        Inherits Form

        Private ReadOnly panelSidebar As Panel
        Private ReadOnly panelContent As Panel
        Private ReadOnly timer As Timer
        Private _dernierPopupNotificationId As Integer

        Public Sub New()
            Me.Text = "Tableau de bord"
            Me.WindowState = FormWindowState.Maximized

            panelSidebar = New Panel() With {.Dock = DockStyle.Left, .Width = 200, .BackColor = Color.FromArgb(30, 42, 68)}
            panelContent = New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White}

            Dim btnFact As New Button() With {.Text = "Facturier", .Width = 160, .Height = 40, .Location = New Point(20, 40)}
            Dim btnCaisse As New Button() With {.Text = "Caisse", .Width = 160, .Height = 40, .Location = New Point(20, 90)}
            Dim btnAdmin As New Button() With {.Text = "Admin", .Width = 160, .Height = 40, .Location = New Point(20, 140)}
            Dim btnDeconnexion As New Button() With {.Text = "Deconnexion", .Width = 160, .Height = 40, .Location = New Point(20, 190)}

            IconsHelper.AppliquerIconeBouton(btnFact, "FACTURE")
            IconsHelper.AppliquerIconeBouton(btnCaisse, "CAISSE")
            IconsHelper.AppliquerIconeBouton(btnAdmin, "ADMIN")
            IconsHelper.AppliquerIconeBouton(btnDeconnexion, "INFO")

            AddHandler btnFact.Click, Sub() LoadForm(New FacturationForm())
            AddHandler btnCaisse.Click, Sub() LoadForm(New CaisseForm())
            AddHandler btnAdmin.Click, Sub() LoadForm(New AdminForm())
            AddHandler btnDeconnexion.Click, AddressOf Deconnecter

            If SessionUtilisateur.Role = "ADMIN" Then
                panelSidebar.Controls.Add(btnFact)
                panelSidebar.Controls.Add(btnCaisse)
                panelSidebar.Controls.Add(btnAdmin)
                panelSidebar.Controls.Add(btnDeconnexion)
                LoadForm(New FormulaireDashboard())
            ElseIf SessionUtilisateur.Role = "FACTURIER" Then
                panelSidebar.Controls.Add(btnFact)
                panelSidebar.Controls.Add(btnDeconnexion)
            ElseIf SessionUtilisateur.Role = "CAISSIERE" Then
                panelSidebar.Controls.Add(btnCaisse)
                panelSidebar.Controls.Add(btnDeconnexion)
            End If

            Me.Controls.Add(panelContent)
            Me.Controls.Add(panelSidebar)

            ChargerModeSombre()
            ThemeHelper.AppliquerTheme(Me)
            IconsHelper.AppliquerIconeFormulaire(Me)

            timer = New Timer() With {.Interval = 5000}
            AddHandler timer.Tick, AddressOf PingSession
            timer.Start()
        End Sub

        Private Sub LoadForm(f As Form)
            panelContent.Controls.Clear()
            f.TopLevel = False
            f.FormBorderStyle = FormBorderStyle.None
            f.Dock = DockStyle.Fill
            panelContent.Controls.Add(f)
            f.Show()
        End Sub

        Private Sub PingSession(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim repo As New SessionRepository(dal)
                repo.Ping(SessionUtilisateur.SessionId)

                Dim paramService As New ParametreService(New ParametreRepository(dal))
                Dim p As ParametreDTO = paramService.Charger()
                Dim seuil As Decimal = If(p Is Nothing, 0D, p.SeuilStockCritique)
                Dim jours As Integer = If(p Is Nothing, 30, p.AlerteExpirationJours)

                Dim notificationService As New NotificationService(dal)
                notificationService.SynchroniserAlertesMetier(seuil, jours, SessionUtilisateur.UtilisateurId)

                Dim dt As DataTable = notificationService.ListerNonLues()
                If dt.Rows.Count > 0 Then
                    Dim derniereId As Integer = Convert.ToInt32(dt.Rows(0)("NotificationId"))
                    If derniereId > _dernierPopupNotificationId Then
                        Dim messages As New List(Of String)()
                        For Each row As DataRow In dt.Rows
                            messages.Add(Convert.ToString(row("Message")))
                            If messages.Count >= 3 Then
                                Exit For
                            End If
                        Next
                        Dim popup As New NotificationPopup(messages)
                        popup.Show()
                        _dernierPopupNotificationId = derniereId
                    End If
                End If
            Catch
            End Try
        End Sub

        Private Sub ChargerModeSombre()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim paramService As New ParametreService(New ParametreRepository(dal))
                Dim p As ParametreDTO = paramService.Charger()
                If p IsNot Nothing Then
                    ThemeHelper.DefinirModeSombre(p.ModeSombre)
                End If
            Catch
            End Try
        End Sub

        Private Sub Deconnecter(sender As Object, e As EventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim repo As New SessionRepository(dal)
                repo.FermerSession(SessionUtilisateur.SessionId)
            Catch
            End Try
            Dim login As New LoginForm()
            login.Show()
            Me.Close()
        End Sub

        Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim repo As New SessionRepository(dal)
                repo.FermerSession(SessionUtilisateur.SessionId)
            Catch
            End Try
            MyBase.OnFormClosing(e)
        End Sub
    End Class
End Namespace
