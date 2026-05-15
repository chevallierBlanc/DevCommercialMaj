Imports System.Windows.Forms
Imports System.Drawing

Namespace DevCommerc8ak
    Public Class AdminForm
        Inherits Form

        Public Sub New()
            Me.BackColor = Color.White
            Dim lbl As New Label() With {
                .Text = "Admin - Prototype",
                .Font = New Font("Segoe UI", 16, FontStyle.Bold),
                .Location = New Point(20, 20),
                .AutoSize = True
            }

            Dim btnDashboard As New Button() With {.Text = "Dashboard", .Location = New Point(20, 70)}
            Dim btnProduits As New Button() With {.Text = "Produits", .Location = New Point(140, 70)}
            Dim btnClients As New Button() With {.Text = "Clients", .Location = New Point(240, 70)}
            Dim btnFournisseurs As New Button() With {.Text = "Fournisseurs", .Location = New Point(340, 70)}
            Dim btnFactures As New Button() With {.Text = "Factures", .Location = New Point(460, 70)}
            Dim btnPaiements As New Button() With {.Text = "Paiements", .Location = New Point(560, 70)}
            Dim btnStock As New Button() With {.Text = "Stock", .Location = New Point(680, 70)}
            Dim btnRapports As New Button() With {.Text = "Rapports", .Location = New Point(20, 120)}
            Dim btnAppro As New Button() With {.Text = "Appro", .Location = New Point(120, 120)}
            Dim btnParametres As New Button() With {.Text = "Parametres", .Location = New Point(220, 120)}
            Dim btnUtilisateurs As New Button() With {.Text = "Utilisateurs", .Location = New Point(340, 120)}

            AddHandler btnDashboard.Click, Sub() OuvrirFenetre(New FormulaireDashboard())
            AddHandler btnProduits.Click, Sub() OuvrirFenetre(New FormulaireProduits())
            AddHandler btnClients.Click, Sub() OuvrirFenetre(New FormulaireClients())
            AddHandler btnFournisseurs.Click, Sub() OuvrirFenetre(New FormulaireFournisseurs())
            AddHandler btnFactures.Click, Sub() OuvrirFenetre(New FormulaireFactures())
            AddHandler btnPaiements.Click, Sub() OuvrirFenetre(New FormulairePaiements())
            AddHandler btnStock.Click, Sub() OuvrirFenetre(New FormulaireStock())
            AddHandler btnRapports.Click, Sub() OuvrirFenetre(New FormulaireRapports())
            AddHandler btnAppro.Click, Sub() OuvrirFenetre(New FormulaireApprovisionnement())
            AddHandler btnParametres.Click, Sub() OuvrirFenetre(New FormulaireParametres())
            AddHandler btnUtilisateurs.Click, Sub() OuvrirFenetre(New FormulaireUtilisateurs())

            Me.Controls.Add(lbl)
            Me.Controls.Add(btnDashboard)
            Me.Controls.Add(btnProduits)
            Me.Controls.Add(btnClients)
            Me.Controls.Add(btnFournisseurs)
            Me.Controls.Add(btnFactures)
            Me.Controls.Add(btnPaiements)
            Me.Controls.Add(btnStock)
            Me.Controls.Add(btnRapports)
            Me.Controls.Add(btnAppro)
            Me.Controls.Add(btnParametres)
            Me.Controls.Add(btnUtilisateurs)

            ThemeHelper.AppliquerTheme(Me)
            IconsHelper.AppliquerIconeFormulaire(Me)
            IconsHelper.AppliquerIconeBouton(btnProduits, "PRODUITS")
            IconsHelper.AppliquerIconeBouton(btnClients, "CLIENTS")
            IconsHelper.AppliquerIconeBouton(btnStock, "STOCK")
            IconsHelper.AppliquerIconeBouton(btnRapports, "RAPPORTS")
            IconsHelper.AppliquerIconeBouton(btnParametres, "PARAMETRES")
            IconsHelper.AppliquerIconeBouton(btnUtilisateurs, "UTILISATEURS")
        End Sub

        Private Sub OuvrirFenetre(f As Form)
            f.StartPosition = FormStartPosition.CenterParent
            f.ShowDialog(Me)
        End Sub
    End Class
End Namespace
