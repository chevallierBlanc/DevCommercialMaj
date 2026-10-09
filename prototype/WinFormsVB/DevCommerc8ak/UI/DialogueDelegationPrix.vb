Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class ResultatPrixException
        Public Property Prix As Decimal
        Public Property Motif As String
    End Class
    Public NotInheritable Class DialogueDelegationPrix
        Private Sub New()
        End Sub

        Public Shared Function Afficher(owner As IWin32Window, service As DelegationPrixService, produitId As Integer, typeVente As String) As ResultatPrixException
            Dim t As TarifDelegue = service.Consulter(produitId, typeVente)
            Dim resultat As ResultatPrixException = Nothing
            Using form As New Form With {.Text = "Modification rapide du prix", .Size = New Size(700, 520), .MinimumSize = New Size(660, 520), .StartPosition = FormStartPosition.CenterParent, .BackColor = Color.FromArgb(245, 247, 250), .Font = New Font("Segoe UI", 10)}
                Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .Padding = New Padding(20), .ColumnCount = 1, .RowCount = 8}
                root.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
                For Each h As Integer In New Integer() {44, 60, 46, 36, 72, 60, 36, 48}
                    root.RowStyles.Add(New RowStyle(SizeType.Absolute, h))
                Next
                root.Controls.Add(New Label With {.Text = t.Produit & " — " & t.TypeVente, .Dock = DockStyle.Fill, .ForeColor = Color.FromArgb(41, 128, 185), .Font = New Font("Segoe UI", 14, FontStyle.Bold)}, 0, 0)
                root.Controls.Add(New Label With {.Text = "Tarif officiel : " & t.Prix.ToString("N2") & " FC" & Environment.NewLine & "Coût comparable pour ce type de vente : " & t.Cout.ToString("N2") & " FC", .Dock = DockStyle.Fill}, 0, 1)
                Dim prix As New NumericUpDown With {.Dock = DockStyle.Fill, .DecimalPlaces = 2, .Maximum = 9999999999999999.99D, .Minimum = 0.01D, .Value = Math.Max(0.01D, t.Prix)}
                root.Controls.Add(prix, 0, 2)
                Dim variation As New Label With {.Dock = DockStyle.Fill}
                AddHandler prix.ValueChanged, Sub() variation.Text = "Variation : " & If(t.Prix > 0D, ((prix.Value - t.Prix) / t.Prix * 100D).ToString("N2"), "?") & " %"
                root.Controls.Add(variation, 0, 3)
                Dim motif As New TextBox With {.Dock = DockStyle.Fill, .Multiline = True, .MaxLength = 1000}
                root.Controls.Add(motif, 0, 4)
                root.Controls.Add(New Label With {.Text = "Motif obligatoire. Tarif officiel : nouvelles ventes uniquement. Exception facture : seule la ligne de ce panier sera modifiée. Les autres lignes restent inchangées.", .Dock = DockStyle.Fill}, 0, 5)
                Dim actions As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft}
                actions.WrapContents = True
                Dim annuler As New Button With {.Text = "ANNULER", .Width = 100, .Height = 36, .DialogResult = DialogResult.Cancel}
                actions.Controls.Add(annuler)
                For Each code As String In New String() {"PRIX_TARIF_MODIFIER", "PRIX_DEMANDER", "PRIX_FACTURE_EXCEPTION"}
                    If Not service.Autorise(code, "FACTURIER") Then Continue For
                    Dim action As String = code
                    Dim button As New Button With {.Text = If(code = "PRIX_TARIF_MODIFIER", "TARIF OFFICIEL", If(code = "PRIX_DEMANDER", "SOUMETTRE", "CETTE FACTURE")), .Width = 145, .Height = 36, .BackColor = Color.FromArgb(41, 128, 185), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
                    AddHandler button.Click, Sub()
                                                 Try
                                                     Dim raison As String = AuditMetierService.ValiderMotif(motif.Text)
                                                     If MessageBox.Show(form, "Confirmer ce prix et le motif ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
                                                     If action = "PRIX_FACTURE_EXCEPTION" Then
                                                         service.VerifierExceptionProposee(t, prix.Value, raison)
                                                         resultat = New ResultatPrixException With {.Prix = prix.Value, .Motif = raison}
                                                         MessageBox.Show(form, "L'exception sera contrôlée et auditée lors de l'enregistrement de la facture.")
                                                     Else
                                                         MessageBox.Show(form, service.Modifier(t, prix.Value, raison, action = "PRIX_DEMANDER"))
                                                     End If
                                                     form.DialogResult = DialogResult.OK
                                                 Catch ex As Exception
                                                     MessageBox.Show(form, ex.Message, "Modification refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                 End Try
                                             End Sub
                    actions.Controls.Add(button)
                Next
                Dim suivi As New LinkLabel With {.Text = "MES DEMANDES DE PRIX", .Dock = DockStyle.Fill}
                AddHandler suivi.LinkClicked, Sub()
                                                  Try
                                                      Demandes(form, service, "FACTURIER")
                                                  Catch ex As Exception
                                                      MessageBox.Show(form, ex.Message, "Demandes indisponibles", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                  End Try
                                              End Sub
                root.Controls.Add(suivi, 0, 6)
                root.Controls.Add(actions, 0, 7)
                form.CancelButton = annuler
                form.Controls.Add(root)
                form.ShowDialog(owner)
                Return resultat
            End Using
        End Function

        Public Shared Sub Configurer(owner As IWin32Window, service As DelegationPrixService)
            Dim initial As ConfigurationPrix = service.Configuration()
            Using form As New Form With {.Text = "Gestion des prix et délégations", .Size = New Size(650, 510), .StartPosition = FormStartPosition.CenterParent, .BackColor = Color.FromArgb(245, 247, 250), .Font = New Font("Segoe UI", 10)}
                Dim root As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .Padding = New Padding(20), .FlowDirection = FlowDirection.TopDown, .WrapContents = False, .AutoScroll = True}
                Dim active As New CheckBox With {.Text = "Activer la délégation des prix", .AutoSize = True, .Checked = initial.Active}
                Dim demandes As New CheckBox With {.Text = "Autoriser les demandes (validité : 24 heures)", .AutoSize = True, .Checked = initial.Demandes}
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
                                              service.Configurer(New ConfigurationPrix With {.Active = active.Checked, .Demandes = demandes.Checked, .Immediate = immediate.Checked, .SousCout = sousCout.Checked, .Variation = variation.Value, .Marge = marge.Value}, initial, motif.Text)
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
