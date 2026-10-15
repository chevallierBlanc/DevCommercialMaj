Option Strict On
Option Explicit On
Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    ' Un seul formulaire : la saisie/conversion existante est specialisee, pas
    ' juxtaposee avec une CaisseForm. Seule la confirmation change de parcours.
    Public Class VenteEncaissementForm
        Inherits FacturationForm
        Private _enAttente As Boolean
        Private _operation As Guid = Guid.NewGuid()
        Private _confirmationIncertaine As Boolean
        Private _reglement As ReglementComptoir
        Private _zoneReglement As ReglementComptoirPanel
        Private ReadOnly _dal As New DAL(ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString)
        Private ReadOnly horloge As New Timer With {.Interval = 30000}
        Public Sub New()
            If SessionUtilisateur.ModeActif <> "FACTURATION_ET_CAISSE" Then Throw New UnauthorizedAccessException("Choisissez Vente & Encaissement à la connexion.")
            Dim acces As New VenteEncaissementService(_dal)
            acces.VerifierAcces()
            Text = "Vente & Encaissement"
            Dim attente As New Button With {.Text = "METTRE EN ATTENTE"}
            Dim reprendre As New Button With {.Text = "REPRENDRE UNE VENTE"}
            Dim annuler As New Button With {.Text = "ANNULER LE BROUILLON"}
            Dim ticket As New Button With {.Text = "DERNIER TICKET 80 MM"}
            Dim a4 As New Button With {.Text = "DERNIERE FACTURE A4"}
            Dim autorisations As New DelegationPrixService(_dal)
            reprendre.Visible = autorisations.Autorise("FACTURE_MODIFIER", "FACTURIER")
            annuler.Visible = autorisations.Autorise("FACTURE_ANNULER", "HISTORIQUE_FACTURES")
            AddHandler attente.Click, Sub()
                                            _enAttente = True
                                            Try
                                                EnregistrerPreparation()
                                            Finally
                                                _enAttente = False
                                            End Try
                                        End Sub
            AddHandler reprendre.Click, AddressOf ReprendreVente
            AddHandler annuler.Click, Sub()
                                         Try
                                             If Not FactureEnEdition.HasValue Then
                                                 MessageBox.Show("Reprenez d'abord le brouillon à annuler.")
                                                 Return
                                             End If
                                             Dim motif As String = DialogueMotifOperation.Demander(Me, "Motif d'annulation du brouillon")
                                             If motif Is Nothing Then Return
                                             Dim service As New FactureOperationService(_dal)
                                             service.Annuler(FactureEnEdition.Value, motif)
                                             EffacerPreparation()
                                         Catch ex As Exception
                                             MessageBox.Show(ex.Message, "Annulation refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                         End Try
                                     End Sub
            AddHandler ticket.Click, Sub() ImprimerDerniere(True)
            AddHandler a4.Click, Sub() ImprimerDerniere(False)
            Dim param As ParametreDTO = PrintConfigurationHelper.ChargerParametres()
            Dim rafraichir As Action = Sub() ConfigurerComptoir(If(param Is Nothing, "Commercial Pro", param.NomMagasin) & " — VENTE & ENCAISSEMENT" & Environment.NewLine & SessionUtilisateur.NomUtilisateur & " | " & SessionUtilisateur.NomRoleActif & " | " & Date.Now.ToString("dd/MM/yyyy HH:mm"))
            ConfigurerComptoir("VENTE & ENCAISSEMENT", attente, reprendre, annuler, ticket, a4)
            rafraichir()
            AddHandler horloge.Tick, Sub() rafraichir()
            horloge.Start()
            AddHandler FactureEnregistree, Sub(id)
                                              _confirmationIncertaine = False
                                              FermerReglement()
                                              VerrouillerPreparation(False)
                                              _operation = Guid.NewGuid()
                                              If _enAttente Then Return
                                              If MessageBox.Show(Me, "Vente réglée. Imprimer le ticket 80 mm ?", "Vente & Encaissement", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then ImprimerDerniere(True, id)
                                          End Sub
        End Sub
        Protected Overrides ReadOnly Property TexteValidation As String
            Get
                Return If(_confirmationIncertaine, "VERIFIER LA CONFIRMATION", "VALIDER & ENCAISSER")
            End Get
        End Property
        Protected Overrides Function MessageOperation() As String
            Return If(_enAttente, "Vente en attente : ", "Vente encaissée : ")
        End Function
        Protected Overrides Function ConfirmerAbandon() As Boolean
            If _confirmationIncertaine OrElse _zoneReglement IsNot Nothing Then Return False
            Return MessageBox.Show(Me, "Abandonner ce panier ? Un brouillon déjà enregistré reste en attente tant qu'il n'est pas annulé avec un motif.", "Abandon explicite", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) = DialogResult.Yes
        End Function
        Protected Overrides Function DisponibiliteAvantValidation() As Boolean
            ' Un rejeu peut concerner la derniere unite deja vendue : laisser le
            ' service retrouver l'operation avant ses controles de nouvelle vente.
            Return _enAttente OrElse _confirmationIncertaine OrElse MyBase.DisponibiliteAvantValidation()
        End Function
        Protected Overrides Function PersisterOperation(dal As DAL, facture As FactureVente, lignes As IList(Of LigneFactureVente), motif As String, version As Byte(), client As Client) As Integer
            If _enAttente Then
                If _confirmationIncertaine OrElse _zoneReglement IsNot Nothing Then Throw New InvalidOperationException("Terminez le règlement ou revenez au panier avant de créer un brouillon.")
                If String.IsNullOrWhiteSpace(motif) Then
                    motif = DialogueMotifOperation.Demander(Me, "Motif de mise en attente (précisez toute remise exceptionnelle)")
                    If motif Is Nothing Then Return 0
                End If
                Return New FactureOperationService(dal).Enregistrer(facture, lignes, motif, version, client, False)
            End If
            If Not _confirmationIncertaine AndAlso _reglement Is Nothing Then
                If _zoneReglement IsNot Nothing Then Return 0
                Dim param As ParametreDTO = PrintConfigurationHelper.ChargerParametres()
                _zoneReglement = New ReglementComptoirPanel(facture.MontantTotal, If(param Is Nothing, 0D, param.TauxUsd))
                AddHandler _zoneReglement.Annulation, Sub()
                                                        FermerReglement()
                                                        VerrouillerPreparation(False)
                                                    End Sub
                AddHandler _zoneReglement.Confirmation, Sub()
                                                           _reglement = _zoneReglement.Reglement
                                                           EnregistrerPreparation()
                                                       End Sub
                VerrouillerPreparation(True)
                AfficherZoneEncaissement(_zoneReglement)
                Return 0
            End If
            Try
                Return New VenteEncaissementService(dal).Finaliser(_operation, facture, lignes, motif, version, client, _reglement)
            Catch ex As ConfirmationComptoirIncertaineException
                _confirmationIncertaine = True
                FermerReglement(False)
                VerrouillerPreparation(True)
                Throw
            Catch ex As VenteComptoirNonFinaliseeException
                _confirmationIncertaine = False
                FermerReglement()
                VerrouillerPreparation(False)
                Throw
            End Try
        End Function
        Public Overrides Function AutoriserSortie() As Boolean
            If _confirmationIncertaine Then
                MessageBox.Show("La confirmation doit être vérifiée avant de quitter. Réessayez la même opération lorsque SQL Server est disponible.")
                Return False
            End If
            If _zoneReglement IsNot Nothing Then
                MessageBox.Show("Terminez le règlement ou utilisez « Revenir au panier » avant de quitter.")
                Return False
            End If
            _enAttente = True
            Try
                Return MyBase.AutoriserSortie()
            Finally
                _enAttente = False
            End Try
        End Function
        Private Sub FermerReglement(Optional effacer As Boolean = True)
            AfficherZoneEncaissement(Nothing)
            If _zoneReglement IsNot Nothing Then _zoneReglement.Dispose()
            _zoneReglement = Nothing
            If effacer Then _reglement = Nothing
        End Sub
        Private Sub ReprendreVente(sender As Object, e As EventArgs)
            If Not AutoriserSortie() Then Return
            Try
                Using dialogue As New Form With {.Text = "Reprendre une vente — 100 derniers résultats", .Size = New Size(900, 520), .StartPosition = FormStartPosition.CenterParent}
                    Dim grille As New DataGridView With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False, .MultiSelect = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill}
                    Dim recherche As New TextBox With {.Dock = DockStyle.Top}
                    Dim charger As New Button With {.Text = "RECHERCHER", .Dock = DockStyle.Top, .Height = 36}
                    Dim ouvrir As New Button With {.Text = "REPRENDRE LA SELECTION", .Dock = DockStyle.Bottom, .Height = 42}
                    Dim lire As Action = Sub()
                                             grille.DataSource = New VenteEncaissementService(_dal).ListerAttentes(recherche.Text)
                                             grille.Columns("FactureVenteId").Visible = False
                                         End Sub
                    AddHandler charger.Click, Sub()
                                                  Try
                                                      lire()
                                                  Catch ex As Exception
                                                      MessageBox.Show(ex.Message)
                                                  End Try
                                              End Sub
                    AddHandler ouvrir.Click, Sub()
                                                 If grille.CurrentRow Is Nothing Then Return
                                                 Dim row As DataRow = DirectCast(grille.CurrentRow.DataBoundItem, DataRowView).Row
                                                 If ChargerFacturePourEdition(CInt(row("FactureVenteId")), CStr(row("NumeroFacture")), CStr(row("Client")), CStr(row("Telephone"))) Then dialogue.DialogResult = DialogResult.OK
                                             End Sub
                    dialogue.Controls.Add(grille) : dialogue.Controls.Add(ouvrir) : dialogue.Controls.Add(charger) : dialogue.Controls.Add(recherche)
                    lire()
                    dialogue.ShowDialog(Me)
                End Using
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Reprise impossible")
            End Try
        End Sub
        Private Sub ImprimerDerniere(thermique As Boolean, Optional factureId As Integer = 0)
            Dim documentCharge As Boolean = False
            Try
                Dim donnees As DataSet = New VenteEncaissementService(_dal).LireDocument(factureId)
                documentCharge = True
                Dim entete As DataRow = donnees.Tables("Entete").Rows(0)
                Dim data As New FacturePrintData With {.Numero = CStr(entete("NumeroFacture")), .DateDocument = CDate(entete("CreeLe")), .Statut = CStr(entete("Statut")), .SousTotal = CDec(entete("SousTotal")), .Remise = CDec(entete("MontantRemise")), .Total = CDec(entete("MontantTotal")), .Client = If(entete.IsNull("NomClient"), "CLIENT", CStr(entete("NomClient"))), .Telephone = If(entete.IsNull("Telephone"), "", CStr(entete("Telephone"))), .Facturier = If(entete.IsNull("NomUtilisateur"), "Auteur inconnu", CStr(entete("NomUtilisateur"))), .Lignes = New List(Of FacturePrintLine)()}
                For Each row As DataRow In donnees.Tables("Lignes").Rows
                    data.Lignes.Add(New FacturePrintLine With {.Produit = CStr(row("Libelle")), .Conditionnement = CStr(row("TypeVente")), .Quantite = CDec(row("Quantite")), .PrixUnitaire = CDec(row("PrixUnitaire")), .Montant = CDec(row("MontantLigne"))})
                Next
                Dim param As ParametreDTO = PrintConfigurationHelper.ChargerParametres()
                Using document As PrintDocument = If(thermique, FacturePrintRenderer.CreerDocumentThermique(data, param, Me, "VenteEncaissement", "Ticket"), FacturePrintRenderer.CreerDocumentA4(data, param))
                    If Not thermique Then param = PrintConfigurationHelper.ConfigurerDocumentA4(document, Me, "VenteEncaissement", "Facture")
                    If param IsNot Nothing AndAlso param.ApercuAvantImpression Then
                        Using apercu As New PrintPreviewDialog With {.Document = document}
                            apercu.ShowDialog(Me)
                        End Using
                    Else
                        document.Print()
                    End If
                End Using
            Catch ex As Exception
                MessageBox.Show(If(documentCharge OrElse factureId > 0, "La vente reste enregistrée. Vous pourrez réessayer l'impression. ", "Aucune opération commerciale n'a été effectuée. ") & "Impression impossible : " & ex.Message, "Impression", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub
        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            horloge.Stop() : horloge.Dispose()
            MyBase.OnFormClosed(e)
        End Sub

    End Class
End Namespace
