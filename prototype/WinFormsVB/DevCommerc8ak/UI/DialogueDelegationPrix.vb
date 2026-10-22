Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class ResultatPrixException
        Public Property Prix As Decimal
        Public Property Motif As String
        Public Property DemandePrixId As Integer?
    End Class
    Partial Public NotInheritable Class DialogueDelegationPrix
        Private Sub New()
        End Sub

        Public Shared Function Afficher(owner As IWin32Window, service As DelegationPrixService, produitId As Integer, typeVente As String, Optional contexte As Guid = Nothing, Optional quantite As Decimal = 0D, Optional prixArticle As Decimal? = Nothing, Optional contexteCommercial As Func(Of Decimal, ContexteCommercialPrix) = Nothing, Optional typeTarifId As Integer? = Nothing, Optional perimetre As String = "TOUS") As ResultatPrixException
            Dim disponibles As System.Collections.Generic.HashSet(Of String) = service.ActionsDisponibles()
            If disponibles.Count = 0 Then Throw New UnauthorizedAccessException("La délégation est désactivée ou aucune action de prix n'est autorisée pour cette session.")
            If perimetre <> "TOUS" AndAlso Not DelegationPrixRegles.CommandeDisponible(disponibles, perimetre, True) Then Throw New UnauthorizedAccessException("Aucune action autorisée pour ce périmètre. Un droit sur une facture ne donne pas accès au tarif officiel.")
            Dim t As TarifDelegue = service.Consulter(produitId, typeVente, typeTarifId)
            If Not t.TypeActif Then Throw New UnauthorizedAccessException("Ce type de vente est désactivé. Sa consultation historique reste disponible, mais aucune nouvelle modification de prix n'est autorisée.")
            Dim tarifs As System.Collections.Generic.List(Of TarifDelegue) = If(perimetre = "TARIF", service.ListerTarifsActifs(produitId), New System.Collections.Generic.List(Of TarifDelegue) From {t})
            If tarifs Is Nothing OrElse tarifs.Count = 0 Then Throw New InvalidOperationException("Aucun type de vente actif disponible pour ce produit. Rechargez les tarifs et vérifiez leur configuration.")
            If Not tarifs.Exists(Function(x) x.TypeId = t.TypeId AndAlso String.Equals(x.TypeVente, t.TypeVente, StringComparison.OrdinalIgnoreCase)) Then Throw New InvalidOperationException("Ce type n'est pas proposé par le catalogue actif : vérifiez son activation et les éventuels noms identiques à un tarif standard.")
            Dim resultat As ResultatPrixException = Nothing
            Using form As New Form With {.Text = "Modification du prix de vente", .ClientSize = New Size(680, 620), .MinimumSize = New Size(600, 460), .StartPosition = FormStartPosition.CenterParent, .BackColor = Color.FromArgb(245, 247, 250), .Font = New Font("Segoe UI", 10), .AutoScaleMode = AutoScaleMode.Dpi, .AutoScaleDimensions = New SizeF(96, 96)}
                form.Text = If(perimetre = "TARIF", "Modifier le prix de vente — tarif officiel", If(perimetre = "FACTURE", "Prix exceptionnel — article de facture", "Modification rapide du prix"))
                Dim cadre As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 3}
                cadre.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                cadre.RowStyles.Add(New RowStyle(SizeType.AutoSize))
                cadre.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
                cadre.RowStyles.Add(New RowStyle(SizeType.AutoSize))
                Dim entete As New TableLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True, .ColumnCount = 1, .BackColor = ThemeHelper1.CommercialEntete, .Padding = New Padding(16, 12, 16, 12), .Margin = New Padding(0)}
                entete.Controls.Add(New Label With {.Text = "Modification du prix de vente", .AutoSize = True, .Font = New Font("Segoe UI", 13, FontStyle.Bold), .ForeColor = Color.White})
                Dim identite As New Label With {.Text = t.Produit & " — " & t.TypeVente & " | " & t.CodeBarres, .AutoSize = True, .ForeColor = Color.White, .MaximumSize = New Size(610, 0)}
                entete.Controls.Add(identite)
                cadre.Controls.Add(entete, 0, 0)
                Dim corps As New Panel With {.Dock = DockStyle.Fill, .AutoScroll = True, .Margin = New Padding(0)}
                Dim root As New TableLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .Padding = New Padding(16, 12, 16, 6), .ColumnCount = 1, .RowCount = 8}
                root.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                For ligne As Integer = 0 To 7
                    root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
                Next
                root.Controls.Add(New Label With {.Text = "INFORMATIONS TARIFAIRES", .AutoSize = True, .ForeColor = ThemeHelper1.CommercialEntete, .Font = New Font("Segoe UI", 10, FontStyle.Bold)}, 0, 0)
                Dim tableau As New System.Data.DataTable
                tableau.Columns.Add("Type de vente") : tableau.Columns.Add("Prix FC", GetType(Decimal)) : tableau.Columns.Add("Coût comparable FC", GetType(Decimal)) : tableau.Columns.Add("Marge %", GetType(Decimal)) : tableau.Columns.Add("Mode") : tableau.Columns.Add("Coeff. / ratio", GetType(Decimal))
                For Each tarif As TarifDelegue In tarifs
                    Dim baseCoeff As Decimal = If(tarif.TypeId > 0 AndAlso String.Equals(tarif.ModePrix, "COEFFICIENT", StringComparison.OrdinalIgnoreCase), tarif.CoutCoefficient, tarif.Cout)
                    tableau.Rows.Add(tarif.TypeVente, tarif.Prix, tarif.Cout, If(TarifCatalogueRegles.Marge(tarif.Prix, tarif.Cout).HasValue, CType(TarifCatalogueRegles.Marge(tarif.Prix, tarif.Cout).Value, Object), DBNull.Value), If(tarif.TypeId > 0, tarif.ModePrix, If(tarif.ChampPrix = "PrixGros", "COEFFICIENT", "FIXE")), If(baseCoeff > 0D, CType(TarifCatalogueRegles.Coefficient(tarif.Prix, baseCoeff).GetValueOrDefault(), Object), DBNull.Value))
                Next
                Dim catalogue As New DataGridView With {.Dock = DockStyle.Top, .Height = 40 + Math.Min(5, tarifs.Count) * 30, .DataSource = tableau, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .RowHeadersVisible = False, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .BackgroundColor = Color.White, .RowTemplate = New DataGridViewRow With {.Height = 30}}
                ThemeHelper1.StyliserEnteteCommerciale(catalogue)
                Dim formaterCatalogue As Action = Sub()
                                                             For Each colonne As DataGridViewColumn In catalogue.Columns
                                                                 If colonne.ValueType Is GetType(Decimal) Then
                                                                     colonne.DefaultCellStyle.Format = If(colonne.Name = "Coeff. / ratio", "N4", "N2")
                                                                     colonne.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                                                                 End If
                                                             Next
                                                         End Sub
                AddHandler catalogue.DataBindingComplete, Sub() formaterCatalogue()
                formaterCatalogue()
                root.Controls.Add(catalogue, 0, 1)
                Dim informations As New Label With {.AutoSize = True}
                root.Controls.Add(informations, 0, 2)
                Dim prix As New NumericUpDown With {.Width = 210, .DecimalPlaces = 2, .Maximum = 9999999999999999.99D, .Minimum = 0.01D, .Value = Math.Max(0.01D, If(prixArticle, t.Prix)), .TextAlign = HorizontalAlignment.Right}
                Dim proposition As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .WrapContents = True, .Padding = New Padding(0, 6, 0, 6)}
                Dim choixTarif As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 180}
                Dim selectionInitiale As TarifDelegue = InitialiserChoixTarif(choixTarif, tarifs, Function(x) x.TypeId = t.TypeId AndAlso String.Equals(x.TypeVente, t.TypeVente, StringComparison.OrdinalIgnoreCase), perimetre = "TARIF")
                If selectionInitiale Is Nothing Then Throw New InvalidOperationException("Le tarif sélectionné n'est plus disponible. Rechargez les tarifs du produit.")
                t = selectionInitiale
                prix.Value = Math.Max(0.01D, If(prixArticle, t.Prix))
                proposition.Controls.Add(choixTarif)
                proposition.Controls.Add(New Label With {.Text = "Prix proposé (FC)", .AutoSize = True, .Margin = New Padding(0, 5, 16, 0)})
                proposition.Controls.Add(prix)
                root.Controls.Add(proposition, 0, 3)
                Dim variation As New Label With {.AutoSize = True, .ForeColor = ThemeHelper1.CommercialEntete}
                Dim actualiserVariation As Action = Sub()
                                                       Try
                                                           Dim propose As Decimal
                                                           If Not Decimal.TryParse(prix.Text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.CurrentCulture, propose) OrElse propose <= 0D Then
                                                               variation.Text = "Saisissez un prix positif valide." : Return
                                                           End If
                                                           Dim net As Decimal = If(contexteCommercial Is Nothing, propose, DelegationPrixRegles.PrixNet(propose, contexteCommercial(propose)))
                                                           Dim baseCoefficient As Decimal = If(t.TypeId > 0 AndAlso String.Equals(t.ModePrix, "COEFFICIENT", StringComparison.OrdinalIgnoreCase), t.CoutCoefficient, t.Cout)
                                                           variation.Text = "Écart au tarif : " & (propose - t.Prix).ToString("N2") & " FC • " & If(t.Prix > 0D, ((propose - t.Prix) / t.Prix * 100D).ToString("N2"), "?") & " %" & Environment.NewLine & "Marge après remise : " & If(t.Cout > 0D, TarifCatalogueRegles.Marge(net, t.Cout).GetValueOrDefault().ToString("N2"), "?") & " % | Coefficient équivalent : " & TarifCatalogueRegles.Coefficient(propose, baseCoefficient).GetValueOrDefault().ToString("N4") & If(t.TypeId = 0 AndAlso t.ChampPrix <> "PrixGros", " (tarif indépendant)", "")
                                                           variation.ForeColor = ThemeHelper1.CommercialEntete
                                                           If t.TypeId > 0 AndAlso String.Equals(t.ModePrix, "COEFFICIENT", StringComparison.OrdinalIgnoreCase) AndAlso baseCoefficient > 0D AndAlso Math.Round(baseCoefficient * TarifCatalogueRegles.Coefficient(propose, baseCoefficient).Value, 2) <> propose Then
                                                               variation.Text &= Environment.NewLine & "Ce prix n'est pas représentable par le coefficient à quatre décimales. Choisissez un prix compatible."
                                                               variation.ForeColor = Color.FromArgb(160, 55, 45)
                                                           End If
                                                       Catch ex As Exception When TypeOf ex Is ArgumentException OrElse TypeOf ex Is OverflowException
                                                           variation.Text = "Contrôlez le panier et la remise avant de soumettre un prix."
                                                       End Try
                                                   End Sub
                AddHandler prix.ValueChanged, Sub() actualiserVariation()
                AddHandler prix.TextChanged, Sub() actualiserVariation()
                Dim afficherTarif As Action = Sub()
                                                 identite.Text = t.Produit & " | " & t.CodeBarres & " | " & t.UnitePrincipale & " | " & t.TypeVente
                                                 informations.Text = "Achat de référence : " & t.PrixAchatReference.ToString("N2") & " FC (lecture seule)" & Environment.NewLine & "Tarif officiel : " & t.Prix.ToString("N2") & " FC | Coût comparable : " & t.Cout.ToString("N2") & " FC" & Environment.NewLine & "Demandeur : " & SessionUtilisateur.NomUtilisateur & If(prixArticle.HasValue, " | Quantité : " & quantite.ToString("0.########"), "")
                                                 informations.Text &= Environment.NewLine & "Équivalent physique : " & t.QuantiteBase.ToString("0.####") & " unité(s) de base" & If(String.IsNullOrWhiteSpace(t.ReferenceQuantite), "", " — " & t.ReferenceQuantite)
                                                 actualiserVariation()
                                             End Sub
                AddHandler choixTarif.SelectionChangeCommitted, Sub()
                                                                  Dim selection As TarifDelegue = TryCast(choixTarif.SelectedItem, TarifDelegue)
                                                                  If selection Is Nothing OrElse Object.ReferenceEquals(selection, t) Then Return
                                                                  If prix.Value <> t.Prix AndAlso MessageBox.Show(form, "Abandonner la proposition non enregistrée pour choisir un autre tarif ?", "Changer de type", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
                                                                      choixTarif.SelectedItem = tarifs.Find(Function(x) x.TypeId = t.TypeId AndAlso x.TypeVente = t.TypeVente) : Return
                                                                  End If
                                                                  t = selection
                                                                  prix.Value = Math.Max(0.01D, t.Prix) : afficherTarif()
                                                              End Sub
                afficherTarif()
                actualiserVariation()
                root.Controls.Add(variation, 0, 4)
                Dim motif As New TextBox With {.Dock = DockStyle.Top, .Height = 54, .Multiline = True, .MaxLength = 1000, .ScrollBars = ScrollBars.Vertical}
                motif.AccessibleName = "Motif obligatoire de modification du prix"
                Dim justification As New TableLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .ColumnCount = 1}
                justification.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                justification.Controls.Add(New Label With {.Text = "JUSTIFICATION — Motif obligatoire", .AutoSize = True, .Font = New Font("Segoe UI", 10, FontStyle.Bold), .ForeColor = ThemeHelper1.CommercialEntete})
                justification.Controls.Add(motif)
                Dim erreurMotif As New Label With {.AutoSize = True, .Visible = False, .ForeColor = Color.FromArgb(192, 57, 43)}
                justification.Controls.Add(erreurMotif)
                root.Controls.Add(justification, 0, 5)
                Dim responsable As New TextBox With {.Dock = DockStyle.Top, .MaxLength = 120, .MaximumSize = New Size(360, 0)}
                Dim instruction As New TableLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .ColumnCount = 1}
                instruction.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                instruction.Controls.Add(New Label With {.Text = "Responsable ayant donné l'instruction (facultatif, ne vaut pas approbation)", .AutoSize = True})
                instruction.Controls.Add(responsable)
                root.Controls.Add(instruction, 0, 6)
                Dim lireMotif As Func(Of String) = Function()
                                                     ' La validation existante reste souveraine ; seule
                                                     ' sa presentation est rapprochee du champ concerne.
                                                     erreurMotif.Visible = False
                                                     Try
                                                         Dim raison As String = AuditMetierService.ValiderMotif(motif.Text)
                                                         Return AuditMetierService.ValiderMotif(raison & If(responsable.Text.Trim() = "", "", Environment.NewLine & "Instruction déclarée : " & responsable.Text.Trim()))
                                                     Catch ex As ArgumentException
                                                         erreurMotif.Text = ex.Message : erreurMotif.Visible = True
                                                         motif.Focus()
                                                         Throw
                                                     End Try
                                                 End Function
                root.Controls.Add(New Label With {.Text = "Enregistrement d'un tarif à la fois, selon ses règles et approbations. Aucun autre prix n'est recalculé. L'exception de facture ne modifie que la ligne concernée.", .AutoSize = True, .ForeColor = Color.FromArgb(90, 100, 110)}, 0, 7)
                Dim pied As New TableLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True, .ColumnCount = 1, .Padding = New Padding(16, 8, 16, 12), .BackColor = Color.White, .Margin = New Padding(0)}
                pied.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                Dim actions As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .FlowDirection = FlowDirection.RightToLeft}
                actions.WrapContents = True
                Dim annuler As New Button With {.Text = "ANNULER", .Width = 100, .Height = 36, .DialogResult = DialogResult.Cancel}
                ThemeHelper1.StyliserActionCommerciale(annuler)
                actions.Controls.Add(annuler)
                For Each code As String In New String() {"PRIX_TARIF_MODIFIER", "PRIX_DEMANDER", "PRIX_FACTURE_EXCEPTION"}
                    If perimetre = "TARIF" AndAlso code = "PRIX_FACTURE_EXCEPTION" Then Continue For
                    If perimetre = "FACTURE" AndAlso code <> "PRIX_FACTURE_EXCEPTION" Then Continue For
                    If Not disponibles.Contains(code) Then Continue For
                    If code = "PRIX_FACTURE_EXCEPTION" AndAlso contexte = Guid.Empty Then Continue For
                    Dim action As String = code
                    Dim button As New Button With {.Text = If(code = "PRIX_TARIF_MODIFIER", "MODIFIER LE TARIF OFFICIEL", If(code = "PRIX_DEMANDER", "DEMANDER UN TARIF OFFICIEL", "PRIX POUR CET ARTICLE")), .Width = 215, .Height = 36, .BackColor = Color.FromArgb(41, 128, 185), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
                    ThemeHelper1.StyliserActionCommerciale(button, principale:=code <> "PRIX_DEMANDER")
                    AddHandler button.Click, Sub()
                                                 button.Enabled = False
                                                 Try
                                                     Dim raison As String = lireMotif()
                                                     If MessageBox.Show(form, "Confirmer ce prix et le motif ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
                                                     If action = "PRIX_FACTURE_EXCEPTION" Then
                                                         service.VerifierExceptionProposee(t, prix.Value, raison, If(contexteCommercial Is Nothing, Nothing, contexteCommercial(prix.Value)))
                                                         resultat = New ResultatPrixException With {.Prix = prix.Value, .Motif = raison}
                                                         MessageBox.Show(form, "L'exception sera contrôlée et auditée lors de l'enregistrement de la facture.")
                                                     Else
                                                         MessageBox.Show(form, service.Modifier(t, prix.Value, raison, action = "PRIX_DEMANDER"))
                                                     End If
                                                     form.DialogResult = DialogResult.OK
                                                 Catch ex As Exception
                                                     MessageBox.Show(form, ex.Message, "Modification refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                 Finally
                                                     button.Enabled = True
                                                 End Try
                                             End Sub
                    actions.Controls.Add(button)
                Next
                If perimetre <> "TARIF" AndAlso contexte <> Guid.Empty AndAlso disponibles.Contains("PRIX_DEMANDER_FACTURE") Then
                    Dim demander As New Button With {.Text = "DEMANDER POUR CET ARTICLE", .Width = 240, .Height = 36}
                    ThemeHelper1.StyliserActionCommerciale(demander)
                    AddHandler demander.Click, Sub()
                                                   demander.Enabled = False
                                                   Try
                                                       Dim raison As String = lireMotif()
                                                       If MessageBox.Show(form, "Soumettre une exception limitée à cet article et cette quantité ? Le tarif officiel restera inchangé.", "Demande d'approbation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
                                                       Dim commercial As ContexteCommercialPrix = If(contexteCommercial Is Nothing, Nothing, contexteCommercial(prix.Value))
                                                       Dim id As Integer = service.DemanderException(t, prix.Value, raison, contexte, quantite, commercial)
                                                       MessageBox.Show(form, "Demande " & id.ToString() & " EN ATTENTE. Conservez le panier, puis utilisez APPLIQUER L'APPROBATION après décision du responsable.")
                                                       form.DialogResult = DialogResult.OK
                                                   Catch ex As Exception
                                                       MessageBox.Show(form, ex.Message, "Demande refusée")
                                                   Finally
                                                       demander.Enabled = True
                                                   End Try
                                               End Sub
                    Dim appliquer As New Button With {.Text = "APPLIQUER L'APPROBATION", .Width = 240, .Height = 36}
                    ThemeHelper1.StyliserActionCommerciale(appliquer, principale:=True)
                    AddHandler appliquer.Click, Sub()
                                                    Try
                                                        Dim approuvee As ApprobationPrixArticle = service.LireExceptionApprouvee(produitId, typeVente, contexte, quantite)
                                                        Dim ligne As LigneFactureVente = approuvee.Ligne
                                                        If contexteCommercial IsNot Nothing Then
                                                            Dim actuel As ContexteCommercialPrix = contexteCommercial(ligne.PrixUnitaire)
                                                            If actuel.SousTotal <> approuvee.Contexte.SousTotal OrElse actuel.Remise <> approuvee.Contexte.Remise Then Throw New InvalidOperationException("Le panier ou la remise ont changé. Soumettez une nouvelle demande.")
                                                        End If
                                                        If MessageBox.Show(form, "Appliquer le prix approuvé de " & ligne.PrixUnitaire.ToString("N2") & " FC à cet article seulement ?", "Approbation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
                                                        resultat = New ResultatPrixException With {.Prix = ligne.PrixUnitaire, .Motif = ligne.MotifPrixException, .DemandePrixId = ligne.DemandePrixId}
                                                        form.DialogResult = DialogResult.OK
                                                    Catch ex As Exception
                                                        MessageBox.Show(form, ex.Message, "Approbation indisponible")
                                                    End Try
                                                End Sub
                    actions.Controls.Add(demander) : actions.Controls.Add(appliquer)
                End If
                Dim suivi As New LinkLabel With {.Text = "Consulter mes demandes", .AutoSize = True, .Margin = New Padding(0, 0, 0, 8)}
                AddHandler suivi.LinkClicked, Sub()
                                                  Try
                                                      Demandes(form, service, "FACTURIER")
                                                  Catch ex As Exception
                                                      MessageBox.Show(form, ex.Message, "Demandes indisponibles", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                  End Try
                                              End Sub
                pied.Controls.Add(suivi, 0, 0)
                pied.Controls.Add(actions, 0, 1)
                form.CancelButton = annuler
                corps.Controls.Add(root) : cadre.Controls.Add(corps, 0, 1)
                cadre.Controls.Add(pied, 0, 2) : form.Controls.Add(cadre)
                ' Le corps seul defile ; les commandes restent accessibles.
                Dim adapter As EventHandler = Sub(sender, e)
                                            Dim largeur As Integer = Math.Max(240, corps.ClientSize.Width - 48)
                                            For Each libelle As Label In New Label() {identite, variation, erreurMotif}
                                                libelle.MaximumSize = New Size(largeur, 0)
                                            Next
                                            For Each conteneur As Control In New Control() {root, instruction, justification}
                                                For Each element As Control In conteneur.Controls
                                                    If TypeOf element Is Label Then element.MaximumSize = New Size(largeur, 0)
                                                Next
                                            Next
                                            actions.MaximumSize = New Size(Math.Max(240, form.ClientSize.Width - 40), 0)
                                              End Sub
                AddHandler form.Resize, adapter
                AddHandler form.Shown, Sub()
                                          Dim surface As Rectangle = Screen.FromControl(form).WorkingArea
                                          form.MinimumSize = New Size(Math.Min(form.MinimumSize.Width, surface.Width - 24), Math.Min(form.MinimumSize.Height, surface.Height - 24))
                                          form.Size = New Size(Math.Min(form.Width, surface.Width - 24), Math.Min(form.Height, surface.Height - 24))
                                          adapter(form, EventArgs.Empty)
                                      End Sub
                form.PerformLayout()
                adapter(form, EventArgs.Empty)
                form.ShowDialog(owner)
                Return resultat
            End Using
        End Function

        Public Shared Sub Configurer(owner As IWin32Window, service As DelegationPrixService)
            Dim initial As ConfigurationPrix = service.Configuration()
            Using form As New Form With {.Text = "Gestion des prix et délégations", .Size = New Size(650, 510), .StartPosition = FormStartPosition.CenterParent, .BackColor = Color.FromArgb(245, 247, 250), .Font = New Font("Segoe UI", 10)}
                Dim root As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .Padding = New Padding(20), .FlowDirection = FlowDirection.TopDown, .WrapContents = False, .AutoScroll = True}
                Dim active As New CheckBox With {.Text = "Activer la délégation des prix", .AutoSize = True, .Checked = initial.Active}
                Dim demandes As New CheckBox With {.Text = "Autoriser les demandes (validité : " & initial.DureeHeures.ToString() & " heures)", .AutoSize = True, .Checked = initial.Demandes}
                Dim immediate As New CheckBox With {.Text = "Autoriser les modifications immédiates selon permission", .AutoSize = True, .Checked = initial.Immediate}
                Dim sousCout As New CheckBox With {.Text = "Autoriser sous coût uniquement avec PRIX_SOUS_COUT", .AutoSize = True, .Checked = initial.SousCout}
                root.Controls.AddRange(New Control() {active, demandes, immediate, sousCout})
                root.Controls.Add(New Label With {.Text = "Variation maximale sans approbation (%)", .AutoSize = True})
                Dim variation As New NumericUpDown With {.DecimalPlaces = 4, .Maximum = 10000D, .Value = initial.Variation, .Width = 200}
                root.Controls.Add(variation)
                root.Controls.Add(New Label With {.Text = "Marge minimale sur coût comparable (%)", .AutoSize = True})
                Dim marge As New NumericUpDown With {.DecimalPlaces = 4, .Maximum = 10000D, .Value = initial.Marge, .Width = 200}
                root.Controls.Add(marge)
                root.Controls.Add(New Label With {.Text = "Motif obligatoire pour changer ces paramètres", .AutoSize = True})
                Dim motif As New TextBox With {.Multiline = True, .Width = 570, .Height = 70, .MaxLength = 1000}
                root.Controls.Add(motif)
                Dim save As New Button With {.Text = "ENREGISTRER", .Width = 160, .Height = 36}
                AddHandler save.Click, Sub()
                                          Try
                                              service.Configurer(New ConfigurationPrix With {.Active = active.Checked, .Demandes = demandes.Checked, .Immediate = immediate.Checked, .SousCout = sousCout.Checked, .Variation = variation.Value, .Marge = marge.Value, .DureeHeures = initial.DureeHeures}, initial, motif.Text)
                                              form.DialogResult = DialogResult.OK
                                          Catch ex As Exception
                                              MessageBox.Show(form, ex.Message, "Configuration refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                          End Try
                                      End Sub
                root.Controls.Add(save)
                form.Controls.Add(root)
                form.ShowDialog(owner)
            End Using
        End Sub

        Public Shared Sub Demandes(owner As IWin32Window, service As DelegationPrixService, Optional ecran As String = "PARAMETRES")
            Using consultation As New Form With {.Text = "Demandes et approbations de prix", .Size = New Size(1080, 650), .MinimumSize = New Size(800, 500), .StartPosition = FormStartPosition.CenterParent}
                Dim contenu As New ConsultationDemandesPrix(service, ecran)
                consultation.Controls.Add(contenu)
                AddHandler consultation.FormClosing, Sub(sender, e)
                                                         If contenu.Occupe Then
                                                             e.Cancel = True
                                                             MessageBox.Show(consultation, "Attendez la fin de la consultation ou de la décision avant de fermer.")
                                                         End If
                                                     End Sub
                consultation.ShowDialog(owner)
            End Using
        End Sub

        Private Shared Sub AncienneListeDemandes(owner As IWin32Window, service As DelegationPrixService, ecran As String)
            Using form As New Form With {.Text = "Demandes de modification de prix", .Size = New Size(1050, 640), .MinimumSize = New Size(800, 460), .StartPosition = FormStartPosition.CenterParent, .BackColor = Color.FromArgb(245, 247, 250)}
                Dim grid As New DataGridView With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .RowHeadersVisible = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .MultiSelect = False, .BackgroundColor = Color.White, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells, .EnableHeadersVisualStyles = False}
                grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94)
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
                grid.RowTemplate.Height = 38
                grid.DataSource = service.ListerDemandes(ecran)
                Dim actions As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .Height = 60, .Padding = New Padding(10), .FlowDirection = FlowDirection.RightToLeft}
                For Each approuver As Boolean In New Boolean() {True, False}
                    If ecran <> "PARAMETRES" Then Continue For
                    If Not service.Autorise(If(approuver, "PRIX_APPROUVER", "PRIX_REFUSER"), "PARAMETRES") Then Continue For
                    Dim decision As Boolean = approuver
                    Dim bouton As New Button With {.Text = If(approuver, "APPROUVER", "REFUSER"), .Width = 140, .Height = 36}
                    AddHandler bouton.Click, Sub()
                                                 Try
                                                     If grid.CurrentRow Is Nothing Then Return
                                                     Dim motif As String = DialogueMotifOperation.Demander(form, "Motif de la décision")
                                                     If motif Is Nothing Then Return
                                                     service.Decider(Convert.ToInt32(grid.CurrentRow.Cells("DemandeId").Value), decision, motif)
                                                     grid.DataSource = service.ListerDemandes(ecran)
                                                 Catch ex As Exception
                                                     MessageBox.Show(form, ex.Message, "Décision refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                 End Try
                                             End Sub
                    actions.Controls.Add(bouton)
                Next
                If ecran = "FACTURIER" AndAlso service.Autorise("PRIX_DEMANDER", "FACTURIER") Then
                    Dim annuler As New Button With {.Text = "ANNULER LA DEMANDE", .Width = 190, .Height = 36}
                    AddHandler annuler.Click, Sub()
                                                  Try
                                                      If grid.CurrentRow Is Nothing Then Return
                                                      Dim motif As String = DialogueMotifOperation.Demander(form, "Annulation de votre demande")
                                                      If motif Is Nothing Then Return
                                                      service.AnnulerDemande(Convert.ToInt32(grid.CurrentRow.Cells("DemandeId").Value), motif)
                                                      grid.DataSource = service.ListerDemandes(ecran)
                                                  Catch ex As Exception
                                                      MessageBox.Show(form, ex.Message, "Annulation refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                  End Try
                                              End Sub
                    actions.Controls.Add(annuler)
                End If
                form.Controls.Add(grid)
                form.Controls.Add(actions)
                form.ShowDialog(owner)
            End Using
        End Sub
    End Class
End Namespace
