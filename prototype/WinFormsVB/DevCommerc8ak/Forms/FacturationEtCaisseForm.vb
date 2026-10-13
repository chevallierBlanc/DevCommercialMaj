Option Strict On
Option Explicit On
Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Configuration

Namespace DevCommerc8ak
    Public Class FacturationEtCaisseForm
        Inherits Form
        Implements ISortieSessionControlee
        Private ReadOnly facturation As FacturationForm
        Private ReadOnly caisse As CaisseForm
        Private ReadOnly etapes As New TabControl With {.Dock = DockStyle.Fill}
        Private _sortie As Boolean
        Public Sub New()
            If SessionUtilisateur.ModeActif <> "FACTURATION_ET_CAISSE" Then Throw New UnauthorizedAccessException("Le mode combiné n'est pas actif.")
            ' Le portail ne fait aucun calcul ni INSERT : les deux composants
            ' utilisent les services et impressions deja exploites separement.
            Text = "Facturation et caisse" : BackColor = Color.FromArgb(245, 247, 250)
            Dim entete As New Label With {.Dock = DockStyle.Top, .Height = 54, .Padding = New Padding(16), .Text = "FACTURATION ET CAISSE • Préparer une facture, puis encaisser séparément", .Font = New Font("Segoe UI", 12, FontStyle.Bold), .ForeColor = Color.FromArgb(41, 128, 185)}
            Dim quitter As New Button With {.Dock = DockStyle.Bottom, .Height = 40, .Text = "QUITTER LE MODE COMBINÉ ET SE RECONNECTER", .BackColor = Color.FromArgb(41, 128, 185), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            AddHandler quitter.Click, Sub()
                                          For Each ouvert As Form In Application.OpenForms
                                              If TypeOf ouvert Is MainForm Then
                                                  DirectCast(ouvert, MainForm).DeconnecterDepuisModuleSession()
                                                  Return
                                              End If
                                          Next
                                      End Sub
            facturation = New FacturationForm()
            caisse = New CaisseForm()
            AjouterEtape("1 • Client et facture", facturation)
            AjouterEtape("2 • Encaissement et ticket", caisse)
            AddHandler facturation.FactureEnregistree, Sub(id As Integer)
                                                         If _sortie Then Return
                                                         Try
                                                             caisse.SelectionnerFacture(id)
                                                             etapes.SelectedIndex = 1
                                                         Catch ex As Exception
                                                             MessageBox.Show(Me, "La facture a été enregistrée, mais le paiement n'a pas été engagé : " & ex.Message, "Reprise d'encaissement", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                         End Try
                                                     End Sub
            Controls.Add(etapes) : Controls.Add(quitter) : Controls.Add(entete)
        End Sub
        Private Sub AjouterEtape(titre As String, contenu As Form)
            Dim page As New TabPage(titre) With {.AutoScroll = True}
            contenu.TopLevel = False : contenu.FormBorderStyle = FormBorderStyle.None : contenu.Dock = DockStyle.Fill
            page.Controls.Add(contenu) : etapes.TabPages.Add(page) : contenu.Show()
        End Sub
        Public Function AutoriserSortie() As Boolean Implements ISortieSessionControlee.AutoriserSortie
            If Not caisse.AutoriserSortie() Then Return False
            _sortie = True
            Try
                Return facturation.AutoriserSortie()
            Finally
                _sortie = False
            End Try
        End Function
        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            facturation.Close() : caisse.Close()
            MyBase.OnFormClosed(e)
        End Sub
        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If facturation IsNot Nothing AndAlso Not facturation.IsDisposed Then facturation.Close()
                If caisse IsNot Nothing AndAlso Not caisse.IsDisposed Then caisse.Close()
            End If
            MyBase.Dispose(disposing)
        End Sub
    End Class
End Namespace
