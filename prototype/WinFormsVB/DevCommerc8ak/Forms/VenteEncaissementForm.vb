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
        Private _derniereFacture As Integer
        Private _confirmationIncertaine As Boolean
        Private _reglement As ReglementComptoir
        Private _zoneReglement As ReglementZone
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
                                              _derniereFacture = id
                                              If MessageBox.Show(Me, "Vente réglée. Imprimer le ticket 80 mm ?", "Vente & Encaissement", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then ImprimerDerniere(True)
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
            Return _confirmationIncertaine OrElse MyBase.DisponibiliteAvantValidation()
        End Function
        Protected Overrides Function PersisterOperation(dal As DAL, facture As FactureVente, lignes As IList(Of LigneFactureVente), motif As String, version As Byte(), client As Client) As Integer
            If _enAttente Then
                If _confirmationIncertaine OrElse _zoneReglement IsNot Nothing Then Throw New InvalidOperationException("Terminez le règlement ou revenez au panier avant de créer un brouillon.")
                Return New FactureOperationService(dal).Enregistrer(facture, lignes, motif, version, client, False)
            End If
            If Not _confirmationIncertaine AndAlso _reglement Is Nothing Then
                If _zoneReglement IsNot Nothing Then Return 0
                Dim param As ParametreDTO = PrintConfigurationHelper.ChargerParametres()
                _zoneReglement = New ReglementZone(facture.MontantTotal, If(param Is Nothing, 0D, param.TauxUsd))
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
                                                 ChargerFacturePourEdition(CInt(row("FactureVenteId")), CStr(row("NumeroFacture")), CStr(row("Client")), CStr(row("Telephone")))
                                                 dialogue.DialogResult = DialogResult.OK
                                             End Sub
                    dialogue.Controls.Add(grille) : dialogue.Controls.Add(ouvrir) : dialogue.Controls.Add(charger) : dialogue.Controls.Add(recherche)
                    lire()
                    dialogue.ShowDialog(Me)
                End Using
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Reprise impossible")
            End Try
        End Sub
        Private Sub ImprimerDerniere(thermique As Boolean)
            If _derniereFacture <= 0 Then Return
            Try
                Dim donnees As DataSet = New VenteEncaissementService(_dal).LireDocument(_derniereFacture)
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
                MessageBox.Show("La vente reste enregistrée. Impression impossible : " & ex.Message, "Impression", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub
        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            horloge.Stop() : horloge.Dispose()
            MyBase.OnFormClosed(e)
        End Sub

        Private Class ReglementZone
            Inherits UserControl
            Public Property Reglement As ReglementComptoir
            Public Event Confirmation As EventHandler
            Public Event Annulation As EventHandler
            Public Sub New(total As Decimal, tauxUsd As Decimal)
                BackColor = Color.FromArgb(245, 247, 250) : Font = New Font("Segoe UI", 10)
                Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .Padding = New Padding(16), .ColumnCount = 1, .AutoScroll = True}
                root.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                Dim devise As New ComboBox With {.Width = 240, .DropDownStyle = ComboBoxStyle.DropDownList}
                devise.Items.Add("FC") : If tauxUsd > 0D Then devise.Items.Add("USD")
                devise.SelectedIndex = 0
                Dim moyen As New ComboBox With {.Width = 240, .DropDownStyle = ComboBoxStyle.DropDownList}
                moyen.Items.AddRange(New Object() {"CASH", "MOBILE_MONEY", "CARTE", "AUTRE"}) : moyen.SelectedIndex = 0
                Dim recu As New NumericUpDown With {.Width = 280, .DecimalPlaces = 8, .Maximum = 999999999999D, .Value = Math.Min(999999999999D, Math.Max(0D, total))}
                Dim reference As New TextBox With {.Width = 500, .MaxLength = 120}
                Dim externe As New CheckBox With {.Text = "Je confirme le paiement externe effectivement reçu.", .AutoSize = True}
                Dim rendu As New Label With {.Width = 510, .Height = 50, .Font = New Font("Segoe UI", 12, FontStyle.Bold), .ForeColor = Color.FromArgb(41, 128, 185)}
                Dim confirmer As New Button With {.Text = "CONFIRMER LA VENTE ET LE PAIEMENT", .Width = 510, .Height = 44, .BackColor = Color.FromArgb(39, 174, 96), .ForeColor = Color.White}
                Dim actualiser As Action = Sub()
                                              Dim fc As Decimal = recu.Value * If(CStr(devise.SelectedItem) = "FC", 1D, tauxUsd)
                                              rendu.Text = "Monnaie à rendre : " & Math.Max(0D, fc - total).ToString("N2") & " FC"
                                              confirmer.Enabled = fc >= total AndAlso (CStr(moyen.SelectedItem) = "CASH" OrElse (externe.Checked AndAlso reference.Text.Trim() <> "" AndAlso fc = total))
                                          End Sub
                AddHandler recu.ValueChanged, Sub() actualiser()
                AddHandler devise.SelectedIndexChanged, Sub() actualiser()
                AddHandler moyen.SelectedIndexChanged, Sub() actualiser()
                AddHandler externe.CheckedChanged, Sub() actualiser()
                AddHandler reference.TextChanged, Sub() actualiser()
                AddHandler confirmer.Click, Sub()
                                                 Reglement = New ReglementComptoir With {.Devise = CStr(devise.SelectedItem), .Moyen = CStr(moyen.SelectedItem), .MontantOrigine = recu.Value, .Taux = If(CStr(devise.SelectedItem) = "FC", 1D, tauxUsd), .Reference = reference.Text.Trim(), .ConfirmationExterne = externe.Checked}
                                                 RaiseEvent Confirmation(Me, EventArgs.Empty)
                                             End Sub
                Dim annuler As New Button With {.Text = "REVENIR AU PANIER", .Width = 240, .Height = 36}
                AddHandler annuler.Click, Sub() RaiseEvent Annulation(Me, EventArgs.Empty)
                root.Controls.Add(New Label With {.Text = "TOTAL : " & total.ToString("N2") & " FC", .AutoSize = True, .Font = New Font("Segoe UI", 16, FontStyle.Bold)})
                root.Controls.Add(New Label With {.Text = "Devise du montant reçu", .AutoSize = True}) : root.Controls.Add(devise)
                root.Controls.Add(New Label With {.Text = "Moyen de paiement", .AutoSize = True}) : root.Controls.Add(moyen)
                root.Controls.Add(New Label With {.Text = "Montant réellement reçu", .AutoSize = True}) : root.Controls.Add(recu)
                root.Controls.Add(rendu)
                root.Controls.Add(New Label With {.Text = "Référence externe (jamais de données carte)", .AutoSize = True}) : root.Controls.Add(reference)
                root.Controls.Add(externe)
                root.Controls.Add(New Label With {.Text = "Règlement intégral. Pour un paiement partiel, mettez en attente et utilisez le circuit de caisse existant. Aucune confirmation opérateur automatique.", .Width = 510, .Height = 54})
                root.Controls.Add(confirmer) : root.Controls.Add(annuler)
                For Each controle As Control In root.Controls
                    controle.Dock = DockStyle.Top
                    controle.Margin = New Padding(0, 0, 0, 8)
                    root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
                    If TypeOf controle Is Label Then
                        DirectCast(controle, Label).AutoSize = True
                        DirectCast(controle, Label).MaximumSize = New Size(400, 0)
                    End If
                Next
                AddHandler Resize, Sub()
                                       For Each controle As Control In root.Controls
                                           If TypeOf controle Is Label Then DirectCast(controle, Label).MaximumSize = New Size(Math.Max(180, Width - 50), 0)
                                       Next
                                   End Sub
                Controls.Add(root)
                actualiser()
            End Sub
        End Class
    End Class
End Namespace
