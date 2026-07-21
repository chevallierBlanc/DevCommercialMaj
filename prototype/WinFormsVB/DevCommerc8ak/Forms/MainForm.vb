Imports System.Windows.Forms
Imports System.Drawing
Imports System.Configuration
Imports System.Data
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports System.Net.NetworkInformation
Imports System

Namespace DevCommerc8ak
    Public Class MainForm
        Inherits Form

        'Private ReadOnly panelSidebar As Panel
        'Private ReadOnly panelContent As Panel
        'Private ReadOnly timer As Timer
        Private _dernierPopupNotificationId As Integer

        'Public Sub New()
        '    Me.Text = "Tableau de bord"
        '    Me.WindowState = FormWindowState.Maximized

        '    panelSidebar = New Panel() With {.Dock = DockStyle.Left, .Width = 200, .BackColor = Color.FromArgb(30, 42, 68)}
        '    panelContent = New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White}

        '    Dim btnFact As New Button() With {.Text = "Facturier", .Width = 160, .Height = 40, .Location = New Point(20, 40)}
        '    Dim btnCaisse As New Button() With {.Text = "Caisse", .Width = 160, .Height = 40, .Location = New Point(20, 90)}
        '    Dim btnAdmin As New Button() With {.Text = "Admin", .Width = 160, .Height = 40, .Location = New Point(20, 140)}
        '    Dim btnDeconnexion As New Button() With {.Text = "Deconnexion", .Width = 160, .Height = 40, .Location = New Point(20, 190)}

        '    'IconsHelper.AppliquerIconeBouton(btnFact, "FACTURE")
        '    'IconsHelper.AppliquerIconeBouton(btnCaisse, "CAISSE")
        '    'IconsHelper.AppliquerIconeBouton(btnAdmin, "ADMIN")
        '    'IconsHelper.AppliquerIconeBouton(btnDeconnexion, "INFO")

        '    AddHandler btnFact.Click, Sub() LoadForm(New FacturationForm())
        '    AddHandler btnCaisse.Click, Sub() LoadForm(New CaisseForm())
        '    AddHandler btnAdmin.Click, Sub() LoadForm(New AdminForm())
        '    AddHandler btnDeconnexion.Click, AddressOf Deconnecter

        '    If SessionUtilisateur.Role = "ADMIN" Then
        '        panelSidebar.Controls.Add(btnFact)
        '        panelSidebar.Controls.Add(btnCaisse)
        '        panelSidebar.Controls.Add(btnAdmin)
        '        panelSidebar.Controls.Add(btnDeconnexion)
        '        LoadForm(New FormulaireDashboard())
        '    ElseIf SessionUtilisateur.Role = "FACTURIER" Then
        '        panelSidebar.Controls.Add(btnFact)
        '        panelSidebar.Controls.Add(btnDeconnexion)
        '    ElseIf SessionUtilisateur.Role = "CAISSIERE" Then
        '        panelSidebar.Controls.Add(btnCaisse)
        '        panelSidebar.Controls.Add(btnDeconnexion)
        '    End If

        '    Me.Controls.Add(panelContent)
        '    Me.Controls.Add(panelSidebar)

        '    'ChargerModeSombre()
        '    'ThemeHelper.AppliquerTheme(Me)
        '    'IconsHelper.AppliquerIconeFormulaire(Me)

        '    timer = New Timer() With {.Interval = 5000}
        '    AddHandler timer.Tick, AddressOf PingSession
        '    timer.Start()
        'End Sub

        'Private Sub LoadForm(f As Form)
        '    panelContent.Controls.Clear()
        '    f.TopLevel = False
        '    f.FormBorderStyle = FormBorderStyle.None
        '    f.Dock = DockStyle.Fill
        '    panelContent.Controls.Add(f)
        '    f.Show()
        'End Sub

        'Private Sub PingSession(sender As Object, e As EventArgs)
        '    Try
        '        Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
        '        Dim dal As New DAL(cs)
        '        Dim repo As New SessionRepository(dal)
        '        repo.Ping(SessionUtilisateur.SessionId)

        '        Dim paramService As New ParametreService(New ParametreRepository(dal))
        '        Dim p As ParametreDTO = paramService.Charger()
        '        Dim seuil As Decimal = If(p Is Nothing, 0D, p.SeuilStockCritique)
        '        Dim jours As Integer = If(p Is Nothing, 30, p.AlerteExpirationJours)

        '        Dim notificationService As New NotificationService(dal)
        '        notificationService.SynchroniserAlertesMetier(seuil, jours, SessionUtilisateur.UtilisateurId)

        '        Dim dt As DataTable = notificationService.ListerNonLues()
        '        If dt.Rows.Count > 0 Then
        '            Dim derniereId As Integer = Convert.ToInt32(dt.Rows(0)("NotificationId"))
        '            If derniereId > _dernierPopupNotificationId Then
        '                Dim messages As New List(Of String)()
        '                For Each row As DataRow In dt.Rows
        '                    messages.Add(Convert.ToString(row("Message")))
        '                    If messages.Count >= 3 Then
        '                        Exit For
        '                    End If
        '                Next
        '                Dim popup As New NotificationPopup(messages)
        '                popup.Show()
        '                _dernierPopupNotificationId = derniereId
        '            End If
        '        End If
        '    Catch
        '    End Try
        'End Sub

        'Private Sub ChargerModeSombre()
        '    Try
        '        Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
        '        Dim dal As New DAL(cs)
        '        Dim paramService As New ParametreService(New ParametreRepository(dal))
        '        Dim p As ParametreDTO = paramService.Charger()
        '        If p IsNot Nothing Then
        '            ThemeHelper.DefinirModeSombre(p.ModeSombre)
        '        End If
        '    Catch
        '    End Try
        'End Sub

        'Private Sub Deconnecter(sender As Object, e As EventArgs)
        '    Try
        '        Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
        '        Dim dal As New DAL(cs)
        '        Dim repo As New SessionRepository(dal)
        '        repo.FermerSession(SessionUtilisateur.SessionId)
        '    Catch
        '    End Try
        '    Dim login As New LoginForm()
        '    login.Show()
        '    Me.Close()
        'End Sub

        'Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        '    Try
        '        Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
        '        Dim dal As New DAL(cs)
        '        Dim repo As New SessionRepository(dal)
        '        repo.FermerSession(SessionUtilisateur.SessionId)
        '    Catch
        '    End Try
        '    MyBase.OnFormClosing(e)
        'End Sub


        ' --- Couleurs du Thème Portail ---
        Private ReadOnly ColorSidebar As Color = Color.FromArgb(28, 35, 49) ' Bleu Nuit Profond
        Private ReadOnly ColorSidebarAccent As Color = Color.FromArgb(41, 128, 185) ' Bleu Action
        Private ReadOnly ColorHeader As Color = Color.White
        Private ReadOnly ColorBg As Color = Color.FromArgb(240, 242, 245) ' Gris Clair Moderne
        Private ReadOnly ColorTextPrimary As Color = Color.FromArgb(44, 62, 80)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(127, 140, 141)
        Private ReadOnly ColorWhite As Color = Color.White

        Private ReadOnly FontMain As New Font("Segoe UI", 10)
        Private ReadOnly FontBold As New Font("Segoe UI", 10, FontStyle.Bold)
        Private ReadOnly FontTitle As New Font("Segoe UI", 14, FontStyle.Bold)
        Private ReadOnly FontMenu As New Font("Segoe UI", 11)

        ' --- Composants ---
        Private ReadOnly panelSidebar As Panel
        Private ReadOnly panelContent As Panel
        Private ReadOnly panelHeader As Panel
        Private ReadOnly _flowPnlMenu As Panel
        Private ReadOnly timer As Timer
        Private ReadOnly _backupService As BackupService
        Private ReadOnly _backupTimer As Timer
        Private ReadOnly _etatTimer As Timer
        Private ReadOnly _statusStrip As StatusStrip
        Private ReadOnly _lblServeurStatus As ToolStripStatusLabel
        Private ReadOnly _lblSqlStatus As ToolStripStatusLabel
        Private ReadOnly _lblBackupStatus As ToolStripStatusLabel
        Private _backupSettings As BackupSettings
        Private _backupEnCours As Boolean
        Private _dernierBackupReussi As Boolean
        Private _dernierAlerte As Date = Date.MinValue
        Private _accueilAdminCharge As Boolean
        Private _dernierMessageBackup As String = String.Empty
        Private _moduleInitialCharge As Boolean
        Private _isCheckingSession As Boolean
        Private _isCheckingStatus As Boolean
        Private _childFormLoaded As Form
        Private _permissionsDepuisBase As Dictionary(Of String, Boolean)

        ' Boutons de navigation
        Private ReadOnly btnFact As Button
        Private ReadOnly btnCaisse As Button
        Private ReadOnly btnAdmin As Button
        Private ReadOnly btnDeconnexion As Button
        Private dernieBoutonSelectionne As Button

        Public Sub New()
            ' Configuration de la Form
            Me.Text = "Système de Gestion Commerciale - Portail Principal"
            Me.WindowState = FormWindowState.Maximized
            Me.BackColor = ColorBg
            Me.Font = FontMain
            _backupService = New BackupService()
            _backupSettings = _backupService.ChargerParametres()
            panelHeader = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 0,
                .Visible = False
            }
            _statusStrip = New StatusStrip() With {
                .Dock = DockStyle.Bottom,
                .SizingGrip = False,
                .RenderMode = ToolStripRenderMode.System,
                .BackColor = Color.White
            }
            _lblServeurStatus = New ToolStripStatusLabel("Serveur : ...") With {.Spring = True}
            _lblSqlStatus = New ToolStripStatusLabel("SQL : ...") With {.Spring = True}
            _lblBackupStatus = New ToolStripStatusLabel("Sauvegarde : en attente") With {.Spring = True}
            _statusStrip.Items.Add(_lblServeurStatus)
            _statusStrip.Items.Add(_lblSqlStatus)
            _statusStrip.Items.Add(_lblBackupStatus)

            ' --- Sidebar (Navigation Latérale) ---
            panelSidebar = New Panel() With {
                .Dock = DockStyle.Left,
                .Width = 220,
                .BackColor = ColorSidebar
            }

            ' Logo / Titre de l'application
            Dim pnlLogo As New Panel() With {.Dock = DockStyle.Top, .Height = 80, .BackColor = Color.FromArgb(22, 28, 40)}
            Dim lblLogo As New Label() With {
                .Text = "COMMERCIAL PRO",
                .ForeColor = ColorWhite,
                .Font = New Font("Segoe UI", 14, FontStyle.Bold),
                .Dock = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleCenter
            }
            pnlLogo.Controls.Add(lblLogo)


            ' Profil Utilisateur dans la Sidebar
            Dim pnlUser As New Panel() With {.Dock = DockStyle.Top, .Height = 100, .Padding = New Padding(20)}
            Dim lblUserName As New Label() With {
                .Text = SessionUtilisateur.NomUtilisateur,
                .ForeColor = ColorWhite,
                .Font = FontBold,
                .Dock = DockStyle.Top,
                .Height = 25,
                .TextAlign = ContentAlignment.MiddleLeft
            }
            Dim lblUserRole As New Label() With {
                .Text = SessionUtilisateur.Role,
                .ForeColor = Color.FromArgb(145, 158, 171),
                .Font = New Font("Segoe UI", 9),
                .Dock = DockStyle.Top,
                .Height = 20,
                .TextAlign = ContentAlignment.MiddleLeft
            }
            pnlUser.Controls.AddRange({lblUserRole, lblUserName})


            ' Séparateur
            Dim pnlSep As New Panel() With {.Dock = DockStyle.Top, .Height = 1, .BackColor = Color.FromArgb(45, 55, 75), .Margin = New Padding(20, 0, 20, 0)}


            ' Conteneur pour les boutons de menu
            _flowPnlMenu = New Panel() With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True,
                .Padding = New Padding(10, 0, 10, 0)
            }


            ' Initialisation des boutons
            btnFact = CreerBoutonMenu("Facturier")
            btnCaisse = CreerBoutonMenu("Caisse")
            btnAdmin = CreerBoutonMenu("Administration")
            btnDeconnexion = CreerBoutonMenu("Déconnexion")
            btnDeconnexion.ForeColor = Color.FromArgb(231, 76, 60) ' Rouge pour déconnexion

            ' --- Header (Barre Supérieure) ---
            'panelHeader = New Panel() With {
            '    .Dock = DockStyle.Top,
            '    .Height = 60,
            '    .BackColor = ColorHeader
            '}
            'AddHandler panelHeader.Paint, Sub(s, e)
            '                                  e.Graphics.DrawLine(New Pen(Color.FromArgb(230, 230, 230)), 0, 59, panelHeader.Width, 59)
            '                              End Sub

            'Dim lblPageTitle As New Label() With {
            '    .Text = "Tableau de Bord Principal",
            '    .Font = FontTitle,
            '    .ForeColor = ColorTextPrimary,
            '    .AutoSize = True,
            '    .Left = 25,
            '    .Top = 18
            '}
            'panelHeader.Controls.Add(lblPageTitle)

            ' --- Content Area ---
            panelContent = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorBg,
                .Padding = New Padding(8),
                .AutoScroll = True
            }

            ' Gestion des droits d'accès
            'If SessionUtilisateur.Role = "ADMIN" Then
            '    flowPnlMenu.Controls.Add(btnFact)
            '    flowPnlMenu.Controls.Add(btnCaisse)
            '    flowPnlMenu.Controls.Add(btnAdmin)
            '    flowPnlMenu.Controls.Add(btnDeconnexion)
            '    LoadForm(New FormulaireDashboard())
            'ElseIf SessionUtilisateur.Role = "FACTURIER" Then
            '    flowPnlMenu.Controls.Add(btnFact)
            '    flowPnlMenu.Controls.Add(btnDeconnexion)
            '    LoadForm(New FacturationForm())
            'ElseIf SessionUtilisateur.Role = "CAISSIERE" Then
            '    flowPnlMenu.Controls.Add(btnCaisse)
            '    flowPnlMenu.Controls.Add(btnDeconnexion)
            '    LoadForm(New CaisseForm())
            'End If
            panelSidebar.Controls.Add(_flowPnlMenu)
            panelSidebar.Controls.Add(pnlSep)
            panelSidebar.Controls.Add(pnlUser)
            panelSidebar.Controls.Add(pnlLogo)
            ConstruireMenuSidebar()

            ' Assemblage final
            Me.Controls.Add(panelContent)
            Me.Controls.Add(panelHeader)
            Me.Controls.Add(panelSidebar)
            Me.Controls.Add(_statusStrip)

            ' Handlers
            'AddHandler btnFact.Click, Sub()
            '                              lblPageTitle.Text = "Module Facturation"
            '                              LoadForm(New FacturationForm())
            '                          End Sub
            'AddHandler btnCaisse.Click, Sub()
            '                                lblPageTitle.Text = "Module Caisse"
            '                                LoadForm(New CaisseForm())
            '                            End Sub
            'AddHandler btnAdmin.Click, Sub()
            '                               lblPageTitle.Text = "Administration Système"
            '                               LoadForm(New AdminForm())
            '                           End Sub
            'AddHandler btnDeconnexion.Click, AddressOf Deconnecter

            ' Thèmes et Icônes
            'ChargerModeSombre()
            'ThemeHelper.AppliquerTheme(Me)
            'IconsHelper.AppliquerIconeFormulaire(Me)
            'IconsHelper.AppliquerIconeBouton(btnFact, "FACTURE")
            'IconsHelper.AppliquerIconeBouton(btnCaisse, "CAISSE")
            'IconsHelper.AppliquerIconeBouton(btnAdmin, "ADMIN")
            'IconsHelper.AppliquerIconeBouton(btnDeconnexion, "INFO")

            ' Timer Session
            timer = New Timer() With {.Interval = 5000}
            AddHandler timer.Tick, AddressOf PingSession
            timer.Start()

            _etatTimer = New Timer() With {.Interval = 60000}
            AddHandler _etatTimer.Tick, AddressOf ActualiserEtatPlateformeTick
            _etatTimer.Start()

            AddHandler Me.Shown, AddressOf MainForm_Shown
            AddHandler panelContent.Resize, AddressOf PanelContent_Resize
            AddHandler AppEvents.RolePermissionsChanged, AddressOf RafraichirPermissionsDepuisEvenement

            If EstRoleAdminEtendu() AndAlso _backupSettings IsNot Nothing AndAlso _backupSettings.Enabled Then
                _backupTimer = New Timer() With {.Interval = 21600000}
                AddHandler _backupTimer.Tick, AddressOf SauvegardeAutomatiqueTick
                _backupTimer.Start()
            End If
        End Sub

        Private Sub MainForm_Shown(sender As Object, e As EventArgs)
            If _moduleInitialCharge Then
                Return
            End If

            _moduleInitialCharge = True

            If String.Equals(SessionUtilisateur.Role, "FACTURIER", StringComparison.OrdinalIgnoreCase) Then
                LoadForm(New FacturationForm())
            ElseIf String.Equals(SessionUtilisateur.Role, "CAISSIERE", StringComparison.OrdinalIgnoreCase) OrElse
                   String.Equals(SessionUtilisateur.Role, "CAISSIER", StringComparison.OrdinalIgnoreCase) Then
                LoadForm(New CaisseForm())
            ElseIf EstRoleAdminEtendu() Then
                Me.BeginInvoke(New MethodInvoker(Sub()
                                                     Try
                                                         If panelContent Is Nothing OrElse panelContent.ClientSize.Width <= 0 OrElse panelContent.ClientSize.Height <= 0 Then
                                                             If Me.IsHandleCreated Then
                                                                 Me.BeginInvoke(New MethodInvoker(Sub()
                                                                                                      Try
                                                                                                          If panelContent IsNot Nothing AndAlso panelContent.ClientSize.Width > 0 AndAlso panelContent.ClientSize.Height > 0 Then
                                                                                                              LoadForm(New FormulaireDashboard())
                                                                                                          End If
                                                                                                      Catch
                                                                                                      End Try
                                                                                                  End Sub))
                                                             End If
                                                             Return
                                                         End If

                                                         LoadForm(New FormulaireDashboard())
                                                     Catch
                                                     End Try
                End Sub))
            End If

            ActualiserEtatPlateforme()
        End Sub

        ''' <summary>
        ''' Ajouter un bouton à la sidebar
        ''' </summary>
        Private Sub AjouterBoutonSidebar(panel As Panel, texte As String, y As Integer, action As EventHandler)
            Dim btn As New Button()
            btn.Text = texte
            btn.Width = panel.Width - 10
            btn.Height = 46
            btn.Location = New Point(5, y)
            btn.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            btn.BackColor = Color.FromArgb(44, 62, 80)
            btn.ForeColor = Color.White
            btn.Font = New Font("Arial", 11, FontStyle.Regular)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.Cursor = Cursors.Hand
            btn.TextAlign = ContentAlignment.MiddleLeft
            btn.Padding = New Padding(15, 0, 0, 0)
            AddHandler btn.Click, Sub(s, e)
                                      action(s, e)
                                      SelectionnerBouton(CType(s, Button))
                                  End Sub
            AddHandler btn.MouseEnter, Sub(s, e)
                                           Dim b As Button = CType(s, Button)
                                           If b IsNot dernieBoutonSelectionne Then
                                               b.BackColor = Color.FromArgb(28, 35, 49)
                                           End If
                                       End Sub
            AddHandler btn.MouseLeave, Sub(s, e)
                                           Dim b As Button = CType(s, Button)
                                           If b IsNot dernieBoutonSelectionne Then
                                               b.BackColor = Color.FromArgb(44, 62, 80)
                                           End If
                                       End Sub
            panel.Controls.Add(btn)
        End Sub

        Private Sub ConstruireMenuSidebar()
            If _flowPnlMenu Is Nothing Then
                Return
            End If

            _flowPnlMenu.Controls.Clear()
            Dim y As Integer = 50

            If VerifierPermission("FACTURIER") Then
                AjouterBoutonSidebar(_flowPnlMenu, "Facturier", y, AddressOf AfficherFacturier)
                y += 50
            End If
            If VerifierPermission("HISTORIQUE_FACTURES") Then
                AjouterBoutonSidebar(_flowPnlMenu, "Historique factures", y, AddressOf AfficherHistoriqueFactures)
                y += 50
            End If

            If VerifierPermission("CAISSE") Then
                AjouterBoutonSidebar(_flowPnlMenu, "Caisse", y, AddressOf AfficherCaisse)
                y += 50
            End If
            If VerifierPermission("FINANCE") Then
                AjouterBoutonSidebar(_flowPnlMenu, "Finance", y, AddressOf AfficherFinance)
                y += 50
            End If

            If VerifierPermission("ADMINISTRATION") Then
                AjouterBoutonSidebar(_flowPnlMenu, "Administration", y, AddressOf Dashbord)
                y += 50
            End If
            If VerifierPermission("ANALYSE_CAISSE_PHYSIQUE") Then
                AjouterBoutonSidebar(_flowPnlMenu, "Analyse caisse physique", y, AddressOf AfficherAnalyseCaissePhysique)
                y += 50
            End If
            If VerifierPermission("STOCK_INVENTAIRE") Then
                AjouterBoutonSidebar(_flowPnlMenu, "Stock / Inventaire", y, AddressOf AfficherStockAdmin)
                y += 50
            End If
            If VerifierPermission("ANALYSE_VENTES") Then
                AjouterBoutonSidebar(_flowPnlMenu, "Analyse ventes", y, AddressOf AfficherAnalyseVente)
                y += 50
            End If
            If VerifierPermission("INVENTAIRE") Then
                AjouterBoutonSidebar(_flowPnlMenu, "Inventaire", y, AddressOf AfficherInventaire)
                y += 50
            End If

            If VerifierPermission("SUPERADMIN_TECH") Then
                AjouterBoutonSidebar(_flowPnlMenu, "Interfaces techniques SuperAdmin", y, AddressOf AfficherTableauTechniqueSuperAdmin)
                y += 50
            End If
            If VerifierPermission("SUPERADMIN_STOCK_INITIAL") Then
                AjouterBoutonSidebar(_flowPnlMenu, "Stock initial technique", y, AddressOf AfficherStockInitialTechnique)
                y += 50
            End If
            If VerifierPermission("SUPERADMIN_ROLES") Then
                AjouterBoutonSidebar(_flowPnlMenu, "Rôles & privilèges", y, AddressOf AfficherRolesSuperAdmin)
                y += 50
            End If
            If VerifierPermission("SUPERADMIN_AUDIT") Then
                AjouterBoutonSidebar(_flowPnlMenu, "Journal actions", y, AddressOf AfficherJournalSuperAdmin)
                y += 50
            End If

            AjouterBoutonSidebar(_flowPnlMenu, "À propos", y, AddressOf AfficherAPropos)
            y += 50
            AjouterBoutonSidebar(_flowPnlMenu, "Déconnexion", y, AddressOf Deconnecter)
        End Sub
        ''' <summary>
        ''' Vérifier les permissions de l'utilisateur
        ''' </summary>
        Private Function VerifierPermission(fonctionnalite As String) As Boolean
            Dim role As String = If(SessionUtilisateur.Role, String.Empty).Trim().ToUpperInvariant()
            If role = "SUPERADMIN" Then
                Return True
            End If

            Dim codePermission As String = MapperCodePermission(fonctionnalite)
            Dim permissionBase As Boolean?

            If codePermission <> String.Empty Then
                permissionBase = LirePermissionDepuisBase(role, codePermission)
                If permissionBase.HasValue Then
                    Return permissionBase.Value
                End If
            End If

            Select Case role
                Case "ADMIN"
                    Return True
                Case "SUPERADMIN"
                    Return True

                Case "PASTEUR"
                    Return True

                Case "FACTURIER"
                    Select Case fonctionnalite
                        Case "Facturier"
                            Return True
                        Case "HistoriqueFactures"
                            Return True
                        Case Else
                            Return False
                    End Select

                Case "CAISSIERE"
                    Select Case fonctionnalite
                        Case "Caisse"
                            Return True
                        Case "Finance"
                            Return True
                        Case Else
                            Return False
                    End Select

                Case "CAISSIER"
                    Select Case fonctionnalite
                        Case "Caisse"
                            Return True
                        Case "Finance"
                            Return True
                        Case Else
                            Return False
                    End Select

                Case Else
                    Return False
            End Select
        End Function

        Private Function MapperCodePermission(fonctionnalite As String) As String
            Select Case fonctionnalite.Trim().ToUpperInvariant()
                Case "FACTURIER"
                    Return "FACTURIER"
                Case "HISTORIQUEFACTURES", "HISTORIQUE_FACTURES"
                    Return "HISTORIQUE_FACTURES"
                Case "CAISSE"
                    Return "CAISSE"
                Case "FINANCE"
                    Return "FINANCE"
                Case "ADMIN", "ADMINISTRATION"
                    Return "ADMINISTRATION"
                Case "ANALYSE_CAISSE_PHYSIQUE"
                    Return "ANALYSE_CAISSE_PHYSIQUE"
                Case "STOCK_INVENTAIRE"
                    Return "STOCK_INVENTAIRE"
                Case "ANALYSE_VENTES"
                    Return "ANALYSE_VENTES"
                Case "INVENTAIRE"
                    Return "INVENTAIRE"
                Case "PARAMETRES"
                    Return "PARAMETRES"
                Case "SUPERADMIN_TECH"
                    Return "SUPERADMIN_TECH"
                Case "SUPERADMIN_STOCK_INITIAL"
                    Return "SUPERADMIN_STOCK_INITIAL"
                Case "SUPERADMIN_ROLES"
                    Return "SUPERADMIN_ROLES"
                Case "SUPERADMIN_AUDIT"
                    Return "SUPERADMIN_AUDIT"
                Case Else
                    Return String.Empty
            End Select
        End Function

        Private Function LirePermissionDepuisBase(role As String, codePermission As String) As Boolean?
            If role = String.Empty OrElse codePermission = String.Empty Then
                Return Nothing
            End If

            If _permissionsDepuisBase Is Nothing Then
                _permissionsDepuisBase = New Dictionary(Of String, Boolean)(StringComparer.OrdinalIgnoreCase)
            End If

            Dim cle As String = role & "|" & codePermission
            If _permissionsDepuisBase.ContainsKey(cle) Then
                Return _permissionsDepuisBase(cle)
            End If

            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim repo As New SuperAdminRepository(New DAL(cs))
                repo.AssurerInfrastructure()
                If Not repo.RoleUtilisePermissions(role) Then
                    Return Nothing
                End If

                Dim autorise As Boolean = repo.RoleAutoriseInterface(role, codePermission)
                _permissionsDepuisBase(cle) = autorise
                Return autorise
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Warn("MainForm", "LirePermissionDepuisBase", "Permission base indisponible pour " & codePermission & " : " & ex.Message)
                Return Nothing
            End Try
        End Function

        Private Function EstRoleAdminEtendu() As Boolean
            Return String.Equals(SessionUtilisateur.Role, "ADMIN", StringComparison.OrdinalIgnoreCase) OrElse
                   String.Equals(SessionUtilisateur.Role, "SUPERADMIN", StringComparison.OrdinalIgnoreCase)
        End Function

        Private Sub RafraichirPermissionsDepuisEvenement(sender As Object, e As EventArgs)
            If IsDisposed Then
                Return
            End If

            If InvokeRequired Then
                BeginInvoke(New MethodInvoker(Sub() RafraichirPermissionsDepuisEvenement(Nothing, EventArgs.Empty)))
                Return
            End If

            _permissionsDepuisBase = Nothing
            ConstruireMenuSidebar()
        End Sub
        ' --- Helper pour créer les boutons du menu latéral ---
        Private Function CreerBoutonMenu(texte As String) As Button
            Dim btn As New Button() With {
                .Text = "      " & texte,
                .Width = 220, ' Largeur fixe pour remplir le FlowLayoutPanel
                .Height = 50,
                .Margin = New Padding(0, 0, 0, 5), ' Marge inférieure entre les boutons
                .FlatStyle = FlatStyle.Flat,
                .ForeColor = Color.FromArgb(180, 190, 210),
                .Font = FontMenu,
                .TextAlign = ContentAlignment.MiddleLeft,
                .Cursor = Cursors.Hand}
            btn.FlatAppearance.BorderSize = 0
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 55, 75)
            btn.FlatAppearance.MouseDownBackColor = ColorSidebarAccent

            Return btn
        End Function

        ' --- Logique Métier (Inchangée) ---

        Private Sub LoadForm(f As Form)
            If f Is Nothing Then Return
            If panelContent Is Nothing Then Return
            If panelContent.Controls.Count = 1 Then
                Dim currentForm As Form = TryCast(panelContent.Controls(0), Form)
                If currentForm IsNot Nothing AndAlso currentForm.GetType() = f.GetType() Then
                    f.Dispose()
                    Return
                End If
            End If
            If panelContent.ClientSize.Width <= 0 OrElse panelContent.ClientSize.Height <= 0 Then
                If Me.IsHandleCreated Then
                    Me.BeginInvoke(New MethodInvoker(Sub()
                                                         Try
                                                             If panelContent IsNot Nothing AndAlso panelContent.ClientSize.Width > 0 AndAlso panelContent.ClientSize.Height > 0 Then
                                                                 LoadForm(f)
                                                             End If
                                                         Catch
                                                         End Try
                                                     End Sub))
                End If
                Return
            End If

            panelContent.SuspendLayout()
            While panelContent.Controls.Count > 0
                Dim ancienControle As Control = panelContent.Controls(0)
                panelContent.Controls.RemoveAt(0)
                ancienControle.Dispose()
            End While
            f.TopLevel = False
            f.FormBorderStyle = FormBorderStyle.None
            f.AutoScroll = True
            f.AutoScaleMode = AutoScaleMode.Dpi
            f.Location = Point.Empty
            f.Anchor = AnchorStyles.Top Or AnchorStyles.Left
            panelContent.Controls.Add(f)
            _childFormLoaded = f
            AjusterFormulaireCharge()
            f.Show()
            panelContent.ResumeLayout(True)
        End Sub

        Private Sub PanelContent_Resize(sender As Object, e As EventArgs)
            AjusterFormulaireCharge()
        End Sub

        Private Sub AjusterFormulaireCharge()
            If panelContent Is Nothing OrElse _childFormLoaded Is Nothing OrElse _childFormLoaded.IsDisposed Then
                Return
            End If

            Dim largeurDisponible As Integer = Math.Max(0, panelContent.ClientSize.Width - panelContent.Padding.Horizontal)
            Dim hauteurDisponible As Integer = Math.Max(0, panelContent.ClientSize.Height - panelContent.Padding.Vertical)
            Dim largeurCible As Integer = Math.Max(largeurDisponible, If(_childFormLoaded.MinimumSize.Width > 0, _childFormLoaded.MinimumSize.Width, 0))
            Dim hauteurCible As Integer = Math.Max(hauteurDisponible, If(_childFormLoaded.MinimumSize.Height > 0, _childFormLoaded.MinimumSize.Height, 0))

            _childFormLoaded.Size = New Size(largeurCible, hauteurCible)
        End Sub
        ''' <summary>
        ''' Sélectionner un bouton et mettre à jour l'affichage
        ''' </summary>
        Private Sub SelectionnerBouton(btn As Button)
            ' Réinitialiser le bouton précédent
            If dernieBoutonSelectionne IsNot Nothing Then
                dernieBoutonSelectionne.BackColor = Color.FromArgb(44, 62, 80)
            End If

            ' Sélectionner le nouveau bouton
            btn.BackColor = Color.FromArgb(20, 96, 183)
            dernieBoutonSelectionne = btn
        End Sub
        'Private Sub PingSession(sender As Object, e As EventArgs)
        '    Try
        '        Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
        '        Dim dal As New DAL(cs)
        '        Dim repo As New SessionRepository(dal)
        '        repo.Ping(SessionUtilisateur.SessionId)

        '        Dim notifRepo As New NotificationRepository(dal)
        '        notifRepo.AssurerTable()
        '        Dim dt As DataTable = notifRepo.ListerNonLues()
        '        If dt.Rows.Count > 0 Then
        '            Dim msg As String = Convert.ToString(dt.Rows(0)("Message"))
        '            MessageBox.Show(msg, "Notification Système", MessageBoxButtons.OK, MessageBoxIcon.Information)
        '            notifRepo.MarquerLues()
        '        End If

        '        VerifierRuptures(dal)
        '    Catch
        '    End Try
        'End Sub

        Private Async Sub PingSession(sender As Object, e As EventArgs)
            If _isCheckingSession Then Return
            _isCheckingSession = True
            Try
                Dim result As Tuple(Of DataTable, Integer) = Await Task.Run(Function()
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

                                                                                Dim dtNotifications As DataTable = notificationService.ListerNonLues()
                                                                                Dim derniereId As Integer = 0
                                                                                If dtNotifications IsNot Nothing AndAlso dtNotifications.Rows.Count > 0 Then
                                                                                    derniereId = Convert.ToInt32(dtNotifications.Rows(0)("NotificationId"))
                                                                                End If

                                                                                Return Tuple.Create(dtNotifications, derniereId)
                                                                            End Function)

                Dim dtResultat As DataTable = If(result Is Nothing, Nothing, result.Item1)
                If dtResultat IsNot Nothing AndAlso dtResultat.Rows.Count > 0 Then
                    Dim derniereId As Integer = If(result Is Nothing, 0, result.Item2)
                    If derniereId > _dernierPopupNotificationId Then
                        Dim messages As New List(Of String)()
                        For Each row As DataRow In dtResultat.Rows
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
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Warn("MainForm", "PingSession", "Erreur lors du ping de session : " & ex.Message)
            Finally
                _isCheckingSession = False
            End Try
        End Sub

        Private Async Sub ActualiserEtatPlateformeTick(sender As Object, e As EventArgs)
            Await ActualiserEtatPlateformeAsync()
        End Sub

        Private Async Function ActualiserEtatPlateformeAsync() As Task
            If _isCheckingStatus Then Return
            _isCheckingStatus = True
            Try
                Dim etat As Tuple(Of Boolean, Boolean) = Await Task.Run(Function()
                                                                            Dim settings As SqlConnectionSettings = SqlConfigurationService.LoadSettings()
                                                                            Dim serveurConnecte As Boolean = TesterHoteServeur(If(settings Is Nothing, String.Empty, settings.Server))
                                                                            Dim erreurSql As String = Nothing
                                                                            Dim sqlConnecte As Boolean = SqlConfigurationService.HasValidConnection(erreurSql)
                                                                            Return Tuple.Create(serveurConnecte, sqlConnecte)
                                                                        End Function)

                If etat Is Nothing Then
                    MettreAJourStatusServeur(False)
                    MettreAJourStatusSql(False)
                Else
                    MettreAJourStatusServeur(etat.Item1)
                    MettreAJourStatusSql(etat.Item2)
                End If
                MettreAJourStatusSauvegarde()
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("MainForm", "ActualiserEtatPlateformeAsync", "Erreur lors de la mise à jour des statuts.", ex)
                MettreAJourStatusServeur(False)
                MettreAJourStatusSql(False)
                MettreAJourStatusSauvegarde()
            Finally
                _isCheckingStatus = False
            End Try
        End Function

        Private Sub ActualiserEtatPlateforme()
            Dim t As Task = ActualiserEtatPlateformeAsync()
        End Sub

        Private Function TesterHoteServeur(serveur As String) As Boolean
            If String.IsNullOrWhiteSpace(serveur) Then
                Return False
            End If

            Dim normalise As String = serveur.Trim()
            If normalise = "." OrElse normalise.Equals("(local)", StringComparison.OrdinalIgnoreCase) OrElse normalise.Equals("localhost", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If

            Dim indexPort As Integer = normalise.IndexOf(","c)
            If indexPort > 0 Then
                normalise = normalise.Substring(0, indexPort).Trim()
            End If

            Try
                Using ping As New Ping()
                    Dim reply As PingReply = ping.Send(normalise, 1000)
                    Return reply IsNot Nothing AndAlso reply.Status = IPStatus.Success
                End Using
            Catch
                Return False
            End Try
        End Function

        Private Sub MettreAJourStatusServeur(connecte As Boolean)
            If _lblServeurStatus Is Nothing Then Return
            _lblServeurStatus.Text = If(connecte, "Serveur : connecté", "Serveur : indisponible")
            _lblServeurStatus.ForeColor = If(connecte, Color.FromArgb(22, 163, 74), Color.FromArgb(185, 28, 28))
        End Sub

        Private Sub MettreAJourStatusSql(connecte As Boolean)
            If _lblSqlStatus Is Nothing Then Return
            _lblSqlStatus.Text = If(connecte, "SQL : connecté", "SQL : indisponible")
            _lblSqlStatus.ForeColor = If(connecte, Color.FromArgb(22, 163, 74), Color.FromArgb(185, 28, 28))
        End Sub

        Private Sub MettreAJourStatusSauvegarde()
            If _lblBackupStatus Is Nothing Then Return
            Dim texte As String
            Dim couleur As Color

            If _backupSettings Is Nothing OrElse Not _backupSettings.Enabled Then
                texte = "Sauvegarde : en attente"
                couleur = Color.FromArgb(107, 114, 128)
            ElseIf _dernierBackupReussi Then
                texte = "Sauvegarde : OK"
                couleur = Color.FromArgb(22, 163, 74)
            ElseIf Not String.IsNullOrWhiteSpace(_dernierMessageBackup) Then
                texte = "Sauvegarde : erreur"
                couleur = Color.FromArgb(185, 28, 28)
            Else
                texte = "Sauvegarde : en attente"
                couleur = Color.FromArgb(107, 114, 128)
            End If

            _lblBackupStatus.Text = texte
            _lblBackupStatus.ForeColor = couleur
        End Sub

        Private Sub VerifierRuptures(dal As DAL)
            If (Date.Now - _dernierAlerte).TotalSeconds < 30 Then Return

            Dim paramService As New ParametreService(New ParametreRepository(dal))
            Dim p As ParametreDTO = paramService.Charger()
            Dim seuil As Decimal = If(p Is Nothing, 0D, p.SeuilStockCritique)

            Dim sql As String = "SELECT p.ProduitId, p.Libelle, s.QuantiteStock, p.SeuilCritique FROM Produits p " &
                                "JOIN vStockProduit s ON s.ProduitId = p.ProduitId " &
                                "WHERE s.QuantiteStock <= CASE WHEN p.SeuilCritique > 0 THEN p.SeuilCritique ELSE @seuil END"

            Dim dt As DataTable = dal.ExecuterTable(sql, CommandType.Text, Nothing)
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then Return

            Dim notifRepo As New NotificationRepository(dal)
            notifRepo.Ajouter("Rupture ou seuil critique détecté. Bon d'approvisionnement généré.")

            Dim approRepo As New BonApprovisionnementRepository(dal)
            Dim approService As New ApprovisionnementService(dal, approRepo)
            approService.GenererBonAuto(seuil, SessionUtilisateur.UtilisateurId)

            _dernierAlerte = Date.Now
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

        Private Async Sub SauvegardeAutomatiqueTick(sender As Object, e As EventArgs)
            Try
                If _backupSettings Is Nothing OrElse Not _backupSettings.Enabled Then Return
                Await ExecuterSauvegardeSilencieuseAsync(False)
            Catch
            End Try
        End Sub

        Private Async Function ExecuterSauvegardeSilencieuseAsync(force As Boolean) As Task(Of Boolean)
            If _backupService Is Nothing OrElse _backupSettings Is Nothing Then Return False
            If Not force AndAlso Not _backupSettings.Enabled Then Return False
            If _backupEnCours Then Return False

            _backupEnCours = True
            Try
                Dim cible As String = _backupSettings.BackupFolder
                Dim resultat As BackupResult = Await Task.Run(Function() _backupService.ExecuterSauvegarde(cible))
                _dernierBackupReussi = resultat.Success
                _dernierMessageBackup = If(resultat Is Nothing, String.Empty, resultat.Message)
                MettreAJourStatusSauvegarde()
                Return resultat.Success
            Catch ex As Exception
                _dernierBackupReussi = False
                _dernierMessageBackup = ex.Message
                Dim log As New ProductionLogService()
                log.Error("MainForm", "ExecuterSauvegardeSilencieuseAsync", "Erreur lors de la sauvegarde automatique.", ex)
                MettreAJourStatusSauvegarde()
                Return False
            Finally
                _backupEnCours = False
            End Try
        End Function

        Private Sub Deconnecter(sender As Object, e As EventArgs)
            If MessageBox.Show("Voulez-vous vraiment vous déconnecter ?", "Déconnexion", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.No Then Return

            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim repo As New SessionRepository(dal)
                repo.FermerSession(SessionUtilisateur.SessionId)
            Catch
            End Try
            ApplicationLifecycle.RequestReturnToLogin()
            Me.Close()
        End Sub

        ''' <summary>
        ''' Afficher admin
        ''' </summary>
        Private Sub Dashbord(sender As Object, e As EventArgs)


            LoadForm(New AdminForm())

        End Sub

        ''' <summary>
        ''' Afficher analyse ventes
        ''' </summary>
        Private Sub AfficherAnalyseVente(sender As Object, e As EventArgs)
            LoadForm(New FormulaireAnalyseVente())
        End Sub

        Private Sub AfficherAnalyseCaissePhysique(sender As Object, e As EventArgs)
            LoadForm(New FormulaireAnalyseCaissePhysique())
        End Sub

        ''' <summary>
        ''' Afficher inventaire intelligent
        ''' </summary>
        Private Sub AfficherInventaire(sender As Object, e As EventArgs)
            LoadForm(New FrmInventaireIntelligent())
        End Sub

        Private Sub AfficherStockInitialTechnique(sender As Object, e As EventArgs)
            LoadForm(New FormulaireStockInitialTechnique())
        End Sub

        Private Sub AfficherTableauTechniqueSuperAdmin(sender As Object, e As EventArgs)
            LoadForm(New FormulaireSuperAdminDashboard(
                Sub() LoadForm(New FormulaireStockInitialTechnique()),
                Sub() LoadForm(New FormulaireSuperAdminRoles()),
                Sub() LoadForm(New FormulaireSuperAdminJournal())))
        End Sub

        Private Sub AfficherRolesSuperAdmin(sender As Object, e As EventArgs)
            LoadForm(New FormulaireSuperAdminRoles())
        End Sub

        Private Sub AfficherJournalSuperAdmin(sender As Object, e As EventArgs)
            LoadForm(New FormulaireSuperAdminJournal())
        End Sub

        Private Sub AfficherAPropos(sender As Object, e As EventArgs)
            Using frm As New FormAPropos()
                frm.ShowDialog(Me)
            End Using
        End Sub

        ''' <summary>
        ''' Afficher factureir
        ''' </summary>
        Private Sub AfficherFacturier(sender As Object, e As EventArgs)
            LoadForm(New FacturationForm())
        End Sub

        ''' <summary>
        ''' Afficher caisse
        ''' </summary>
        Private Sub AfficherCaisse(sender As Object, e As EventArgs)
            LoadForm(New CaisseForm())
        End Sub

        Private Sub AfficherFinance(sender As Object, e As EventArgs)
            LoadForm(New FormulaireFinance())
        End Sub

        Private Sub AfficherHistoriqueFactures(sender As Object, e As EventArgs)
            LoadForm(New FormulaireFactures())
        End Sub

        Private Sub AfficherStockAdmin(sender As Object, e As EventArgs)
            LoadForm(New FormulaireStock())
        End Sub

        Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
            Try
                If timer IsNot Nothing Then
                    timer.Stop()
                    timer.Dispose()
                End If
                If _backupTimer IsNot Nothing Then
                    _backupTimer.Stop()
                    _backupTimer.Dispose()
                End If
                If _etatTimer IsNot Nothing Then
                    _etatTimer.Stop()
                    _etatTimer.Dispose()
                End If

                If Not ApplicationLifecycle.IsReturnToLoginRequested() AndAlso EstRoleAdminEtendu() AndAlso _backupSettings IsNot Nothing AndAlso _backupSettings.Enabled AndAlso _backupSettings.BackupBeforeExit Then
                    Dim resultat As BackupResult = Nothing
                    Dim tacheBackup As Task(Of BackupResult) = Task.Run(Function() _backupService.ExecuterSauvegarde(_backupSettings.BackupFolder))
                    If tacheBackup.Wait(TimeSpan.FromSeconds(60)) Then
                        resultat = tacheBackup.Result
                        _dernierBackupReussi = resultat.Success
                        _dernierMessageBackup = If(resultat Is Nothing, String.Empty, resultat.Message)
                        MettreAJourStatusSauvegarde()
                        If Not resultat.Success Then
                            Dim log As New ProductionLogService()
                            log.Warn("MainForm", "OnFormClosing", "La sauvegarde avant fermeture a échoué : " & resultat.Message)
                        End If
                    Else
                        _dernierBackupReussi = False
                        _dernierMessageBackup = "Timeout de sauvegarde avant fermeture."
                        MettreAJourStatusSauvegarde()
                        Dim log As New ProductionLogService()
                        log.Warn("MainForm", "OnFormClosing", "La sauvegarde avant fermeture a dépassé le délai autorisé.")
                    End If
                End If

                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim dal As New DAL(cs)
                Dim repo As New SessionRepository(dal)
                repo.FermerSession(SessionUtilisateur.SessionId)
            Catch
            End Try
            MyBase.OnFormClosing(e)
        End Sub

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            Try
                If ApplicationLifecycle.IsReturnToLoginRequested() Then
                    ApplicationLifecycle.StopBackgroundServices()
                Else
                    ApplicationLifecycle.RequestShutdown()
                End If
            Catch
            End Try

            MyBase.OnFormClosed(e)
        End Sub
    End Class
End Namespace
