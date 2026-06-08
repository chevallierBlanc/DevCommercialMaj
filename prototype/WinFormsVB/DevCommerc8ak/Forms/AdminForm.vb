Imports System.Windows.Forms
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports Microsoft.VisualBasic

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
            Dim flowMain As New FlowLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(30),
                .AutoScroll = True,
                .BackColor = Color.Transparent
            }

            ' Initialisation des boutons (Noms conservés)
            btnDashboard = CreerBoutonCard("Tableau de Bord", "Vue globale de l'activité", Color.FromArgb(76, 175, 80))
            btnProduits = CreerBoutonCard("Produits", "Gestion du catalogue articles", Color.FromArgb(255, 152, 0))
            btnClients = CreerBoutonCard("Clients", "Base de données clients", Color.FromArgb(244, 67, 54))
            btnFournisseurs = CreerBoutonCard("Fournisseurs", "Gestion des partenaires", Color.FromArgb(36, 36, 39))
            btnFactures = CreerBoutonCard("Factures", "Historique et suivi facturation", Color.FromArgb(33, 150, 243))
            btnPaiements = CreerBoutonCard("Paiements", "Suivi des encaissements", Color.FromArgb(30, 60, 114))
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
            AddHandler btnPaiements.Click, Sub() OuvrirFenetre(New FormulairePaiements())
            AddHandler btnStock.Click, Sub() OuvrirFenetre(New FormulaireStock())
            AddHandler btnVentes.Click, Sub() OuvrirFenetre(New FormulaireVente())
            AddHandler btnRapports.Click, Sub() OuvrirFenetre(New FormulaireRapports())
            AddHandler btnAppro.Click, Sub() OuvrirFenetre(New FormulaireApprovisionnement())
            AddHandler btnParametres.Click, Sub() OuvrirFenetre(New FormulaireParametres())
            AddHandler btnUtilisateurs.Click, Sub() OuvrirFenetre(New FormulaireUtilisateurs())
            AddHandler btnFinance.Click, Sub() OuvrirFenetre(New FormulaireFinance())

            ' Ajout à la grille
            flowMain.Controls.AddRange({btnDashboard, btnProduits, btnClients, btnFournisseurs, btnFactures, btnPaiements, btnFinance, btnStock, btnVentes, btnAppro, btnRapports, btnUtilisateurs, btnParametres})

            ' Assemblage final
            Me.Controls.Add(flowMain)
            Me.Controls.Add(pnlHeader)

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
    End Class
End Namespace
