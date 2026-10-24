Imports System
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Configuration
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Threading
Imports System.Threading.Tasks

Namespace DevCommerc8ak
    Public Class AdminForm
        Inherits Form

        ' --- Palette de Couleurs Professionnelle ---
        Private ReadOnly ColorBg As Color = Color.FromArgb(244, 247, 252)
        Private ReadOnly ColorCardBg As Color = Color.FromArgb(239, 246, 255)
        Private ReadOnly ColorAccent As Color = Color.FromArgb(59, 130, 246) ' Bleu Moderne
        Private ReadOnly ColorTextPrimary As Color = Color.FromArgb(31, 41, 55)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(107, 114, 128)

        ' --- Polices ---
        Private ReadOnly FontTitle As New Font("Segoe UI", 18, FontStyle.Bold)
        Private ReadOnly FontSubtitle As New Font("Segoe UI", 10)
        Private ReadOnly FontButton As New Font("Segoe UI", 10, FontStyle.Bold)

        ' --- Composants (Noms conservés) ---
        Private ReadOnly btnDashboard As Button
        Private ReadOnly btnProduits As Button
        Private ReadOnly btnClients As Button
        Private ReadOnly btnFournisseurs As Button
        Private ReadOnly btnFactures As Button
        Private ReadOnly btnPaiements As Button
        Private ReadOnly btnStock As Button
        Private ReadOnly btnVentes As Button
        Private ReadOnly btnRapports As Button
        Private ReadOnly btnAppro As Button
        Private ReadOnly btnParametres As Button
        Private ReadOnly btnUtilisateurs As Button
        Private ReadOnly btnFinance As Button
        Private ReadOnly _cartes As New FlowLayoutPanel()
        Private ReadOnly _etatChargement As New Label()
        Private ReadOnly _annulation As New CancellationTokenSource()
        Private ReadOnly _correlation As Guid = Guid.NewGuid()
        Private ReadOnly _ouverture As Stopwatch = Stopwatch.StartNew()
        Private _charge As Boolean
        Private _premierAffichage As Boolean
        Private _libere As Boolean
        Private _droitsAffiches As Boolean
        Private _renduDroitsMesure As Boolean

        Public ReadOnly Property CorrelationOuverture As Guid
            Get
                Return _correlation
            End Get
        End Property

        Public Sub New()
            ' Configuration de la Form
            Me.Text = "Centre de Contrôle Administrateur"
            Me.Size = New Size(1000, 750)
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = ColorBg
            Me.DoubleBuffered = True

            ' --- En-tête ---
            Dim pnlHeader As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 120,
                .Padding = New Padding(30, 30, 30, 0)
            }

            Dim lbl As New Label() With {
                .Text = "Gestion du Système",
                .Font = FontTitle,
                .ForeColor = ColorTextPrimary,
                .AutoSize = True,
                .Location = New Point(30, 30)
            }

            Dim lblSub As New Label() With {
                .Text = "Accédez aux différents modules de configuration et de gestion opérationnelle.",
                .Font = FontSubtitle,
                .ForeColor = ColorTextSecondary,
                .AutoSize = True,
                .Location = New Point(32, 70)
            }
            pnlHeader.Controls.AddRange({lbl, lblSub})

            ' --- Grille de fonctionnalités (FlowLayoutPanel pour la fluidité) ---
            Dim flowMain As FlowLayoutPanel = _cartes
            flowMain.Dock = DockStyle.Fill
            flowMain.Padding = New Padding(30)
            flowMain.AutoScroll = True
            flowMain.BackColor = Color.Transparent
            AddHandler flowMain.Paint, Sub()
                                           If Not _droitsAffiches OrElse _renduDroitsMesure Then Return
                                           _renduDroitsMesure = True
                                           Dim journal As New ProductionLogService()
                                           journal.Info("Administration", "RenduCartes", "Correlation=" & _correlation.ToString("D") & " | TotalMs=" & _ouverture.ElapsedMilliseconds.ToString())
                                       End Sub

            ' Initialisation des boutons (Noms conservés)
            btnDashboard = CreerBoutonCard("Tableau de Bord", "Vue globale de l'activité", Color.FromArgb(76, 175, 80))
            btnProduits = CreerBoutonCard("Produits", "Gestion du catalogue articles", Color.FromArgb(255, 152, 0))
            btnClients = CreerBoutonCard("Clients", "Base de données clients", Color.FromArgb(244, 67, 54))
            btnFournisseurs = CreerBoutonCard("Fournisseurs", "Gestion des partenaires", Color.FromArgb(36, 36, 39))
            btnFactures = CreerBoutonCard("Factures", "Historique et suivi facturation", Color.FromArgb(33, 150, 243))
            btnPaiements = CreerBoutonCard("Analyse caisse physique", "Suivi des clôtures, écarts et régularisations", Color.FromArgb(30, 60, 114))
            btnStock = CreerBoutonCard("Stock", "État et mouvements de stock", Color.FromArgb(0, 188, 212))
            btnVentes = CreerBoutonCard("Ventes", "Analyse des ventes et stock", Color.FromArgb(103, 58, 183))
            btnAppro = CreerBoutonCard("Approvisionnement", "Bons de commande", Color.FromArgb(255, 152, 0))
            btnRapports = CreerBoutonCard("Rapports", "Analyses et statistiques", Color.FromArgb(0, 125, 141))
            btnUtilisateurs = CreerBoutonCard("Utilisateurs", "Gestion des accès et rôles", Color.FromArgb(42, 93, 155))
            btnParametres = CreerBoutonCard("Paramètres", "Configuration du système", Color.FromArgb(156, 39, 176))
            btnFinance = CreerBoutonCard("Finance", "Suivi des encaissements", Color.FromArgb(54, 99, 65))

            ' Handlers (Logique conservée)
            AddHandler btnDashboard.Click, Sub() OuvrirFenetre(New FormulaireDashboard())
            AddHandler btnProduits.Click, Sub() OuvrirFenetre(New FormulaireProduits())
            AddHandler btnClients.Click, Sub() OuvrirFenetre(New FormulaireClients())
            AddHandler btnFournisseurs.Click, Sub() OuvrirFenetre(New FormulaireFournisseurs())
            AddHandler btnFactures.Click, Sub() OuvrirFenetre(New FormulaireFactures())
            AddHandler btnPaiements.Click, Sub() OuvrirFenetre(New FormulaireAnalyseCaissePhysique())
            AddHandler btnStock.Click, Sub() OuvrirFenetre(New FormulaireStock())
            AddHandler btnVentes.Click, Sub() OuvrirFenetre(New FormulaireVente())
            AddHandler btnRapports.Click, Sub() OuvrirFenetre(New FormulaireRapports())
            AddHandler btnAppro.Click, Sub() OuvrirFenetre(New FormulaireApprovisionnement())
            AddHandler btnParametres.Click, Sub() OuvrirFenetre(New FormulaireParametres())
            AddHandler btnUtilisateurs.Click, Sub() OuvrirFenetre(New FormulaireUtilisateurs())
            AddHandler btnFinance.Click, Sub() OuvrirFenetre(New FormulaireFinance())

            ' Les cartes ne sont pas ajoutees avant le retour des droits reels.
            _etatChargement.Dock = DockStyle.Bottom
            _etatChargement.Height = 36
            _etatChargement.Padding = New Padding(30, 8, 0, 0)
            _etatChargement.ForeColor = ColorTextSecondary
            _etatChargement.Text = "Chargement des autorisations..."

            ' Assemblage final
            Me.Controls.Add(flowMain)
            Me.Controls.Add(pnlHeader)
            Me.Controls.Add(_etatChargement)

            ' Thèmes et Icônes (Logique conservée)
            'ThemeHelper.AppliquerTheme(Me)
            'IconsHelper.AppliquerIconeFormulaire(Me)

            '' Application des icônes via le helper existant
            'IconsHelper.AppliquerIconeBouton(btnProduits, "PRODUITS")
            'IconsHelper.AppliquerIconeBouton(btnClients, "CLIENTS")
            'IconsHelper.AppliquerIconeBouton(btnStock, "STOCK")
            'IconsHelper.AppliquerIconeBouton(btnRapports, "RAPPORTS")
            'IconsHelper.AppliquerIconeBouton(btnParametres, "PARAMETRES")
            'IconsHelper.AppliquerIconeBouton(btnUtilisateurs, "UTILISATEURS")
            Dim log As New ProductionLogService()
            log.Info("Administration", "ConstructionUI", "Correlation=" & _correlation.ToString("D") & " | Ms=" & _ouverture.ElapsedMilliseconds.ToString())
        End Sub

        ' --- Helper pour créer des boutons sous forme de cartes ---
        Private Function CreerBoutonCard(titre As String, description As String, couleur As Color) As Button
            Dim btn As New Button() With {
                .Size = New Size(210, 140),
                .Margin = New Padding(10),
                .FlatStyle = FlatStyle.Flat,
                .BackColor = ColorCardBg,
                .ForeColor = couleur,
                .Font = FontButton,
                .Text = vbCrLf & titre,
                .TextAlign = ContentAlignment.MiddleCenter,
                .Cursor = Cursors.Hand
            }
            btn.FlatAppearance.BorderSize = 1
            btn.FlatAppearance.BorderColor = couleur
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(249, 250, 251)





            ' Ajout d'un label de description sur le bouton (visuel seulement)
            Dim lblDesc As New Label() With {
                .Text = description,
                .Font = New Font("Segoe UI", 8),
                .ForeColor = couleur,
                .AutoSize = False,
                .Size = New Size(190, 40),
                .Location = New Point(10, 90),
                .TextAlign = ContentAlignment.TopCenter,
                .Enabled = False ' Pour que le clic passe au bouton
            }
            btn.Controls.Add(lblDesc)
            ' Bande de couleur à gauche
            Dim panelCouleur As New Panel()
            panelCouleur.BackColor = couleur
            panelCouleur.Size = New Size(5, 140)
            panelCouleur.Location = New Point(0, 0)
            panelCouleur.BorderStyle = BorderStyle.None
            btn.Controls.Add(panelCouleur)

            ' Effet de bordure au survol
            AddHandler btn.MouseEnter, Sub() btn.FlatAppearance.BorderColor = ColorAccent
            AddHandler btn.MouseLeave, Sub() btn.FlatAppearance.BorderColor = couleur

            Return btn
        End Function

        Private Sub OuvrirFenetre(f As Form)
            f.StartPosition = FormStartPosition.CenterParent
            f.ShowDialog(Me)
        End Sub

        Protected Overrides Async Sub OnShown(e As EventArgs)
            MyBase.OnShown(e)
            If _charge Then Return
            _charge = True
            Dim role As String = If(SessionUtilisateur.Role, String.Empty).Trim().ToUpperInvariant()
            Dim session As Integer = SessionUtilisateur.SessionId
            Dim utilisateur As Integer = SessionUtilisateur.UtilisateurId
            Dim roleId As Integer = SessionUtilisateur.RoleIdActif
            Dim log As New ProductionLogService()
            log.Info("Administration", "Shown", "Correlation=" & _correlation.ToString("D") & " | Ms=" & _ouverture.ElapsedMilliseconds.ToString())
            Dim attente As New System.Windows.Forms.Timer() With {.Interval = 1000}
            AddHandler attente.Tick, Sub()
                                        If IsDisposed Then Return
                                        If _ouverture.Elapsed.TotalSeconds >= 5 Then _etatChargement.Text = "Vérification des droits en cours... Vous pouvez quitter ce module."
                                    End Sub
            attente.Start()
            Try
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Dim repo As New SuperAdminRepository(New DAL(cs))
                Dim droits As HashSet(Of String) = Await repo.ListerCodesInterfacesAutoriseesAsync(role, _annulation.Token, _correlation)
                If IsDisposed OrElse _annulation.IsCancellationRequested Then Return
                If session <> SessionUtilisateur.SessionId OrElse utilisateur <> SessionUtilisateur.UtilisateurId OrElse roleId <> SessionUtilisateur.RoleIdActif OrElse role <> If(SessionUtilisateur.Role, String.Empty).Trim().ToUpperInvariant() Then
                    _etatChargement.Text = "La session a changé. Rouvrez Administration."
                    Return
                End If
                Dim chrono As Stopwatch = Stopwatch.StartNew()
                Dim cartes As Button() = {btnDashboard, btnProduits, btnClients, btnFournisseurs, btnFactures, btnPaiements, btnFinance, btnStock, btnVentes, btnAppro, btnRapports, btnUtilisateurs, btnParametres}
                Dim codes As String() = {"ADMINISTRATION", "STOCK_INVENTAIRE", "ADMINISTRATION", "ADMINISTRATION", "HISTORIQUE_FACTURES", "ANALYSE_CAISSE_PHYSIQUE", "FINANCE", "STOCK_INVENTAIRE", "ANALYSE_VENTES", "STOCK_INVENTAIRE", "ADMINISTRATION", "ADMINISTRATION", "PARAMETRES"}
                _cartes.SuspendLayout()
                Try
                    For i As Integer = 0 To cartes.Length - 1
                        If droits.Contains(codes(i)) Then _cartes.Controls.Add(cartes(i))
                    Next
                Finally
                    _cartes.ResumeLayout(True)
                End Try
                _etatChargement.Text = If(_cartes.Controls.Count = 0, "Aucun module autorisé pour ce rôle.", String.Empty)
                _droitsAffiches = True
                _cartes.Invalidate()
                log.Info("Administration", "RemplissageUI", "Correlation=" & _correlation.ToString("D") & " | Ms=" & chrono.ElapsedMilliseconds.ToString())
                BeginInvoke(New MethodInvoker(Sub()
                                                 If Not IsDisposed Then log.Info("Administration", "RetourBoucleUI", "Correlation=" & _correlation.ToString("D") & " | TotalMs=" & _ouverture.ElapsedMilliseconds.ToString())
                                             End Sub))
            Catch ex As OperationCanceledException
                ' Quitter le module annule uniquement cette lecture.
            Catch ex As Exception
                If Not IsDisposed Then _etatChargement.Text = "Chargement impossible. Quittez puis rouvrez Administration ; si le problème persiste, contactez le support."
                log.Warn("Administration", "ChargementRefuse", "Correlation=" & _correlation.ToString("D") & " | Type=" & ex.GetType().Name)
            Finally
                attente.Stop()
                attente.Dispose()
            End Try
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            If _premierAffichage Then Return
            _premierAffichage = True
            Dim log As New ProductionLogService()
            log.Info("Administration", "PremierPaint", "Correlation=" & _correlation.ToString("D") & " | Ms=" & _ouverture.ElapsedMilliseconds.ToString())
        End Sub

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing AndAlso Not _libere Then
                _libere = True
                _annulation.Cancel()
                _annulation.Dispose()
                For Each bouton As Button In New Button() {btnDashboard, btnProduits, btnClients, btnFournisseurs, btnFactures, btnPaiements, btnFinance, btnStock, btnVentes, btnAppro, btnRapports, btnUtilisateurs, btnParametres}
                    If bouton IsNot Nothing AndAlso bouton.Parent Is Nothing Then bouton.Dispose()
                Next
            End If
            MyBase.Dispose(disposing)
        End Sub
    End Class
End Namespace
