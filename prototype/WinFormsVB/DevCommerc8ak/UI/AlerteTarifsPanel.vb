Option Strict On
Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Collections.Generic
Namespace DevCommerc8ak
    Public Class AlerteTarifsPanel
        Inherits UserControl
        Private ReadOnly suivi As New DataChangeMonitorService(New String() {"PRODUITS", "TYPES_VENTE"}, 5000)
        Private perimes As Boolean
        Private demarre As Boolean
        Private versionsChargees As New Dictionary(Of String, Long)(StringComparer.OrdinalIgnoreCase)
        Private versionsEnChargement As Dictionary(Of String, Long)
        Public Event RechargementDemande As EventHandler
        Public ReadOnly Property TarifsPerimes As Boolean
            Get
                Return perimes
            End Get
        End Property
        Public Sub New()
            Dock = DockStyle.Bottom : Height = 48 : Visible = False : BackColor = Color.FromArgb(232, 242, 250)
            Dim message As New Label With {.Text = "Nouveaux tarifs disponibles. Votre saisie est conservée ; rechargez avant d'enregistrer.", .Dock = DockStyle.Fill, .AutoEllipsis = True, .TextAlign = ContentAlignment.MiddleLeft}
            Dim recharger As New Button With {.Text = "Recharger les tarifs", .Dock = DockStyle.Right, .Width = 180}
            ThemeHelper1.StyliserActionCommerciale(recharger)
            Controls.Add(message) : Controls.Add(recharger)
            AddHandler recharger.Click, Sub()
                                            If MessageBox.Show(Me, "Recharger abandonnera les modifications de cette fiche non enregistrées. Continuer ?", "Actualiser les tarifs", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) = DialogResult.Yes Then RaiseEvent RechargementDemande(Me, EventArgs.Empty)
                                        End Sub
            AddHandler suivi.DomaineModifie, AddressOf Signaler
        End Sub
        Public Sub Demarrer()
            If demarre Then Return
            suivi.InitialiserVersions(versionsChargees)
            suivi.Start()
            demarre = True
        End Sub
        Public Sub DebutChargement()
            ' Lire AVANT les produits : un changement concurrent pendant leur
            ' lecture restera visible, sans acquitter une version jamais lue.
            versionsEnChargement = AppDataVersionService.LireVersions(New String() {"PRODUITS", "TYPES_VENTE"})
        End Sub
        Public Sub Acquitter()
            If versionsEnChargement IsNot Nothing Then versionsChargees = New Dictionary(Of String, Long)(versionsEnChargement, StringComparer.OrdinalIgnoreCase)
            perimes = False : Visible = False
        End Sub
        Private Sub Signaler(sender As Object, e As DataChangeEventArgs)
            Dim fenetre As Form = FindForm()
            If fenetre Is Nothing OrElse fenetre.IsDisposed OrElse Not fenetre.IsHandleCreated Then Return
            Try
                fenetre.BeginInvoke(New MethodInvoker(Sub()
                                                         If Not IsDisposed Then
                                                             Dim chargee As Long = 0
                                                             If versionsChargees.TryGetValue(e.Domaine, chargee) AndAlso chargee >= e.Version Then Return
                                                             perimes = True : Visible = True
                                                         End If
                                                     End Sub))
            Catch ex As InvalidOperationException
                ' Fermeture simultanee du formulaire ; aucune ecriture metier.
            End Try
        End Sub
        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                RemoveHandler suivi.DomaineModifie, AddressOf Signaler
                suivi.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub
    End Class
End Namespace
