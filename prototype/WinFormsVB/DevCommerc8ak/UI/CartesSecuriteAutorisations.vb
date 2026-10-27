Option Strict On
Option Explicit On
Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Threading.Tasks
Imports System.Web.Script.Serialization

Namespace DevCommerc8ak
    Public Class CartesSecuriteAutorisations
        Inherits UserControl
        Private ReadOnly _prix As DelegationPrixService
        Private ReadOnly _modes As ConfigurationModesService
        Private ReadOnly disposition As New TableLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .ColumnCount = 2}
        Private ReadOnly carteModes As GroupBox
        Private ReadOnly cartePrix As GroupBox
        Private ReadOnly carteDemandes As GroupBox
        Private ReadOnly modesActifs As New CheckBox With {.Text = "Activer la gestion des modes"}
        Private ReadOnly facturation As New CheckBox With {.Text = "Facturation : préparer les factures"}
        Private ReadOnly caisse As New CheckBox With {.Text = "Caisse : encaisser les factures"}
        Private ReadOnly combine As New CheckBox With {.Text = "Vente & Encaissement : parcours commercial complet"}
        Private ReadOnly delegation As New CheckBox With {.Text = "Activer la délégation des prix"}
        Private ReadOnly demandes As New CheckBox With {.Text = "Autoriser les demandes de modification"}
        Private ReadOnly immediate As New CheckBox With {.Text = "Autoriser les modifications immédiates sous conditions"}
        Private ReadOnly sousCout As New CheckBox With {.Text = "Autoriser les ventes sous coût avec permission spécifique"}
        Private ReadOnly variation As New NumericUpDown With {.DecimalPlaces = 4, .Maximum = 10000, .Width = 180}
        Private ReadOnly marge As New NumericUpDown With {.DecimalPlaces = 4, .Maximum = 10000, .Width = 180}
        Private ReadOnly duree As New NumericUpDown With {.Minimum = 1, .Maximum = 168, .Value = 24, .Width = 180}
        Private ReadOnly etatModes As New Label With {.AutoSize = True}
        Private ReadOnly etatPrix As New Label With {.AutoSize = True}
        Private _initialModes As ConfigurationModes
        Private _initialPrix As ConfigurationPrix
        Private _chargement As Boolean
        Public ReadOnly Property PeutQuitter As Boolean
            Get
                Dim demandesControle As ConsultationDemandesPrix = TryCast(carteDemandes.Controls(0), ConsultationDemandesPrix)
                Return Not _chargement AndAlso (demandesControle Is Nothing OrElse Not demandesControle.Occupe)
            End Get
        End Property

        Public Sub New(dal As DAL)
            _prix = New DelegationPrixService(dal)
            _modes = New ConfigurationModesService(dal)
            Dock = DockStyle.Fill : AutoScroll = True : Padding = New Padding(16)
            BackColor = Color.FromArgb(243, 243, 243) : Font = New Font("Segoe UI", 9.5F)
            carteModes = Carte("Modes de travail", "Définissez comment les utilisateurs autorisés travaillent à la facturation et à la caisse. Un seul mode par session.")
            Ajouter(carteModes, modesActifs, facturation, caisse, combine,
                Texte("Modes séparés : droits de facturation ou de caisse du rôle actif. Vente & Encaissement exige les deux ensembles, l'éligibilité du rôle ET une attribution pour ce compte et ce rôle dans Administration Utilisateurs. Aucun droit n'est ajouté automatiquement. Reconnexion après changement ; aucun autre poste n'est fermé."), etatModes)
            cartePrix = Carte("Délégation et contrôle des prix", "Autorisez certains employés à demander ou appliquer un prix sans accès à l'administration générale des produits.")
            Ajouter(cartePrix, delegation, demandes, immediate,
                Texte("Variation maximale sans approbation (%) : variation absolue par rapport au tarif officiel."), variation,
                Texte("Marge minimale sans approbation (%) sur le coût du même conditionnement. En dessous : décision d'un responsable nécessaire."), marge, sousCout,
                Texte("Les ventes sous coût exigent une approbation et la permission spécifique du responsable ; cette option n'autorise pas automatiquement les employés."),
                Texte("Validité des nouvelles demandes (heures, de 1 à 168)"), duree,
                Texte("Tarif officiel : nouvelles ventes. Prix exceptionnel : une facture seulement, avec une permission distincte. Une nouvelle durée ne modifie pas les demandes déjà déposées."), etatPrix)
            carteModes.Height = 460 : cartePrix.Height = 460
            carteDemandes = New GroupBox With {.Text = "Demandes et approbations de prix", .Height = 430, .Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(12), .Margin = New Padding(8)}
            carteDemandes.Controls.Add(New ConsultationDemandesPrix(_prix))
            Controls.Add(disposition)
            AddHandler Resize, Sub() Repartir()
            AddHandler Load, Async Sub(sender, e) Await ChargerAsync()
            Repartir()
        End Sub

        Private Shared Function Texte(valeur As String) As Label
            Return New Label With {.Text = valeur, .AutoSize = True, .MaximumSize = New Size(390, 0), .ForeColor = Color.FromArgb(90, 90, 90), .Margin = New Padding(3, 8, 3, 8)}
        End Function
        Private Shared Function Carte(titre As String, description As String) As GroupBox
            Dim groupe As New GroupBox With {.Text = titre, .Dock = DockStyle.Fill, .BackColor = Color.White, .Margin = New Padding(8), .Padding = New Padding(12)}
            Dim contenu As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.TopDown, .WrapContents = False, .AutoScroll = True}
            contenu.Controls.Add(Texte(description))
            groupe.Controls.Add(contenu)
            AddHandler contenu.Resize, Sub()
                                           For Each element As Control In contenu.Controls
                                               If TypeOf element Is Label OrElse TypeOf element Is CheckBox Then element.MaximumSize = New Size(Math.Max(230, contenu.ClientSize.Width - 28), 0)
                                           Next
                                       End Sub
            Return groupe
        End Function
        Private Shared Sub Ajouter(carte As GroupBox, ParamArray elements As Control())
            For Each element As Control In elements
                If TypeOf element Is CheckBox Then DirectCast(element, CheckBox).AutoSize = True
                carte.Controls(0).Controls.Add(element)
            Next
        End Sub
        Private Sub Repartir()
            ' Les cartes restent les memes controles lors du changement de
            ' largeur : aucune valeur saisie ni selection n'est recreee.
            disposition.SuspendLayout()
            disposition.Controls.Clear() : disposition.ColumnStyles.Clear() : disposition.RowStyles.Clear()
            Dim large As Boolean = ClientSize.Width >= 900
            disposition.ColumnCount = If(large, 2, 1)
            disposition.RowCount = If(large, 2, 3)
            disposition.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, If(large, 50, 100)))
            If large Then disposition.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
            disposition.RowStyles.Add(New RowStyle(SizeType.Absolute, 480))
            If Not large Then disposition.RowStyles.Add(New RowStyle(SizeType.Absolute, 480))
            disposition.RowStyles.Add(New RowStyle(SizeType.Absolute, 450))
            disposition.Controls.Add(carteModes, 0, 0)
            disposition.Controls.Add(cartePrix, If(large, 1, 0), If(large, 0, 1))
            disposition.Controls.Add(carteDemandes, 0, If(large, 1, 2))
            disposition.SetColumnSpan(carteDemandes, If(large, 2, 1))
            disposition.ResumeLayout()
        End Sub

        Public Async Function ChargerAsync() As Task
            If _chargement Then Return
            _chargement = True
            etatModes.Text = "Chargement…" : etatPrix.Text = "Chargement…"
            carteModes.Enabled = False : cartePrix.Enabled = False
            Try
                Try
                    Dim configuration As ConfigurationModes = Await Task.Run(Function() _modes.Charger())
                    If IsDisposed Then Return
                    _initialModes = configuration
                    modesActifs.Checked = configuration.Active : facturation.Checked = configuration.Facturation
                    caisse.Checked = configuration.Caisse : combine.Checked = configuration.Combine
                    carteModes.Enabled = True : etatModes.Text = If(configuration.Active, "Gestion active", "Parcours classique conservé")
                Catch ex As Exception
                    _initialModes = Nothing : etatModes.Text = "Configuration indisponible : " & ex.Message
                End Try
                Try
                    Dim configuration As ConfigurationPrix = Await Task.Run(Function() _prix.Configuration())
                    If IsDisposed Then Return
                    _initialPrix = configuration
                    delegation.Checked = configuration.Active : demandes.Checked = configuration.Demandes
                    immediate.Checked = configuration.Immediate : sousCout.Checked = configuration.SousCout
                    variation.Value = configuration.Variation : marge.Value = configuration.Marge
                    duree.Value = configuration.DureeHeures
                    cartePrix.Enabled = True : etatPrix.Text = If(configuration.Active, "Délégation active selon les permissions", "Délégation désactivée")
                Catch ex As Exception
                    _initialPrix = Nothing : etatPrix.Text = "Configuration indisponible : " & ex.Message
                End Try
            Finally
                _chargement = False
            End Try
        End Function

        Public Async Function EnregistrerAsync(owner As IWin32Window) As Task
            If _chargement Then Throw New InvalidOperationException("Attendez la fin du chargement.")
            Dim m As ConfigurationModes = If(_initialModes Is Nothing, Nothing, New ConfigurationModes With {.Active = modesActifs.Checked, .Facturation = facturation.Checked, .Caisse = caisse.Checked, .Combine = combine.Checked})
            Dim p As ConfigurationPrix = If(_initialPrix Is Nothing, Nothing, New ConfigurationPrix With {.Active = delegation.Checked, .Demandes = demandes.Checked, .Immediate = immediate.Checked, .SousCout = sousCout.Checked, .Variation = variation.Value, .Marge = marge.Value, .DureeHeures = CInt(duree.Value)})
            Dim json As New JavaScriptSerializer()
            If json.Serialize(m) = json.Serialize(_initialModes) Then m = Nothing
            If json.Serialize(p) = json.Serialize(_initialPrix) Then p = Nothing
            If m Is Nothing AndAlso p Is Nothing Then Return
            Dim motif As String = DialogueMotifOperation.Demander(owner, "Motif du changement des autorisations")
            If motif Is Nothing Then Return
            If MessageBox.Show(owner, "Confirmer ces règles pour l'établissement ? Aucun droit individuel ne sera attribué automatiquement.", "Configuration", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
            _chargement = True : Enabled = False
            Try
                Await Task.Run(Sub() _prix.ConfigurerCartes(m, _initialModes, p, _initialPrix, motif))
            Finally
                _chargement = False
                If Not IsDisposed Then Enabled = True
            End Try
            Await ChargerAsync()
        End Function
    End Class

    Public Class ConsultationDemandesPrix
        Inherits UserControl
        Private ReadOnly _service As DelegationPrixService
        Private ReadOnly _ecran As String
        Private ReadOnly grille As New DataGridView With {.Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False, .RowHeadersVisible = False, .MultiSelect = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells, .BackgroundColor = Color.White, .EnableHeadersVisualStyles = False}
        Private ReadOnly recherche As New TextBox With {.Width = 180, .MaxLength = 200}
        Private ReadOnly statut As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 130}
        Private ReadOnly synthese As New Label With {.Dock = DockStyle.Top, .Height = 50}
        Private ReadOnly position As New Label With {.AutoSize = True}
        Private ReadOnly boutonPrecedent As New Button With {.Text = "Précédent", .AutoSize = True}
        Private ReadOnly boutonSuivant As New Button With {.Text = "Suivant", .AutoSize = True}
        Private _page As Integer
        Private _total As Integer
        Private _occupe As Boolean
        Public ReadOnly Property Occupe As Boolean
            Get
                Return _occupe
            End Get
        End Property
        Public Sub New(service As DelegationPrixService, Optional ecran As String = "PARAMETRES")
            _service = service : _ecran = ecran : Dock = DockStyle.Fill
            BackColor = Color.FromArgb(245, 247, 250) : Padding = New Padding(12) : Font = New Font("Segoe UI", 9.5F)
            Dim entete As New Label With {.Text = If(ecran = "FACTURIER", "Mes demandes de prix", "Demandes et approbations de prix"), .Dock = DockStyle.Top, .Height = 54, .Padding = New Padding(12), .Font = New Font("Segoe UI", 13, FontStyle.Bold), .BackColor = ThemeHelper1.CommercialEntete, .ForeColor = Color.White}
            synthese.BackColor = Color.White : synthese.Padding = New Padding(8) : synthese.Height = 58
            Dim outils As New FlowLayoutPanel With {.Dock = DockStyle.Top, .Height = 54, .Padding = New Padding(0, 10, 0, 6), .WrapContents = False, .AutoScroll = True}
            Dim actionsMetier As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .AutoSize = True, .FlowDirection = FlowDirection.RightToLeft}
            statut.Items.AddRange(New Object() {"Tous", "En attente", "Approuvées", "Refusées", "Annulées", "Expirées"}) : statut.SelectedIndex = 0
            outils.Controls.AddRange(New Control() {New Label With {.Text = "Produit / demandeur", .AutoSize = True}, recherche, statut})
            Dim filtrer As New Button With {.Text = "Rechercher", .AutoSize = True}
            ThemeHelper1.StyliserActionCommerciale(filtrer, principale:=True)
            AddHandler filtrer.Click, Async Sub(sender, e)
                                         If _occupe Then Return
                                         _page = 0
                                         Await ChargerAsync()
                                     End Sub
            outils.Controls.Add(filtrer)
            For Each approuver As Boolean In New Boolean() {True, False}
                Dim decision As Boolean = approuver
                Dim bouton As New Button With {.Text = If(decision, "Approuver", "Refuser"), .AutoSize = True}
                ThemeHelper1.StyliserActionCommerciale(bouton, principale:=decision)
                bouton.Visible = ecran = "PARAMETRES"
                AddHandler bouton.Click, Async Sub(sender, e)
                                             If _occupe OrElse grille.CurrentRow Is Nothing Then Return
                                             Dim id As Integer = CInt(grille.CurrentRow.Cells("DemandeId").Value)
                                             Dim motif As String = DialogueMotifOperation.Demander(Me, "Motif de la décision")
                                             If motif Is Nothing Then Return
                                             _occupe = True : outils.Enabled = False
                                             Try
                                                 Await Task.Run(Sub() _service.Decider(id, decision, motif))
                                             Catch ex As Exception
                                                 MessageBox.Show(Me, ex.Message, "Décision refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                             Finally
                                                 _occupe = False : outils.Enabled = True
                                             End Try
                                             Await ChargerAsync()
                                         End Sub
                actionsMetier.Controls.Add(bouton)
                AddHandler Load, Async Sub(sender, e)
                                     Try
                                         bouton.Visible = ecran = "PARAMETRES" AndAlso Await Task.Run(Function() _service.Autorise(If(decision, "PRIX_APPROUVER", "PRIX_REFUSER"), "PARAMETRES"))
                                     Catch
                                         bouton.Visible = False
                                     End Try
                                 End Sub
            Next
            Dim bas As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .Height = 38}
            If ecran = "FACTURIER" Then
                Dim annuler As New Button With {.Text = "Annuler ma demande", .AutoSize = True}
                ThemeHelper1.StyliserActionCommerciale(annuler)
                AddHandler annuler.Click, Async Sub(sender, e)
                                             If _occupe OrElse grille.CurrentRow Is Nothing Then Return
                                             Dim id As Integer = CInt(grille.CurrentRow.Cells("DemandeId").Value)
                                             Dim motif As String = DialogueMotifOperation.Demander(Me, "Annuler ma demande de prix")
                                             If motif Is Nothing Then Return
                                             _occupe = True
                                             Try
                                                 Await Task.Run(Sub() _service.AnnulerDemande(id, motif))
                                             Catch ex As Exception
                                                 MessageBox.Show(Me, ex.Message, "Annulation refusée")
                                             Finally
                                                 _occupe = False
                                             End Try
                                             Await ChargerAsync()
                                         End Sub
                bas.Controls.Add(annuler)
            End If
            Dim precedent As Button = boutonPrecedent
            Dim suivant As Button = boutonSuivant
            ThemeHelper1.StyliserActionCommerciale(precedent) : ThemeHelper1.StyliserActionCommerciale(suivant)
            AddHandler precedent.Click, Async Sub(sender, e)
                                           If _occupe Then Return
                                           _page = Math.Max(0, _page - 1)
                                           Await ChargerAsync()
                                       End Sub
            AddHandler suivant.Click, Async Sub(sender, e)
                                         If _occupe OrElse (_page + 1) * 25 >= _total Then Return
                                         _page += 1
                                         Await ChargerAsync()
                                     End Sub
            bas.Controls.AddRange(New Control() {precedent, suivant, position})
            ThemeHelper1.StyliserEnteteCommerciale(grille)
            grille.RowTemplate.Height = 36
            grille.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
            grille.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True
            grille.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            grille.ColumnHeadersHeight = 62
            grille.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            grille.ScrollBars = ScrollBars.Both
            grille.DefaultCellStyle.Padding = New Padding(6)
            grille.DefaultCellStyle.SelectionBackColor = Color.FromArgb(225, 239, 250)
            grille.DefaultCellStyle.SelectionForeColor = Color.FromArgb(32, 32, 32)
            grille.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252)
            AddHandler grille.CellFormatting, Sub(sender, e)
                                                 If grille.Columns(e.ColumnIndex).Name = "Statut" AndAlso e.Value IsNot Nothing Then
                                                     Dim code As String = Convert.ToString(e.Value)
                                                     e.CellStyle.ForeColor = If(code = "REFUSEE" OrElse code = "ANNULEE", Color.FromArgb(160, 55, 45), If(code = "APPROUVEE", ThemeHelper1.CommercialAction, ThemeHelper1.CommercialEntete))
                                                 End If
                                                 If grille.Columns(e.ColumnIndex).Name <> "Perimetre" OrElse e.Value Is Nothing Then Return
                                                 e.Value = If(Convert.ToString(e.Value) = "TARIF", "Tarif officiel", "Article de facture")
                                                 e.FormattingApplied = True
                                             End Sub
            AddHandler grille.CellDoubleClick, Sub(sender, e)
                                                  If _occupe OrElse e.RowIndex < 0 Then Return
                                                  Dim ligne As DataGridViewRow = grille.Rows(e.RowIndex)
                                                  Dim demandeId As Integer = CInt(ligne.Cells("DemandeId").Value)
                                                  Using detail As New Form With {.Text = "Détails de la demande de prix", .Size = New Size(760, 550), .MinimumSize = New Size(650, 420), .StartPosition = FormStartPosition.CenterParent, .BackColor = Color.FromArgb(245, 247, 250)}
                                                      Dim zone As New TextBox With {.Dock = DockStyle.Fill, .ReadOnly = True, .Multiline = True, .ScrollBars = ScrollBars.Vertical, .Font = New Font("Segoe UI", 11), .BackColor = Color.White}
                                                      Dim nouveau As Decimal = CDec(ligne.Cells("NouveauPrix").Value)
                                                      Dim ancien As Decimal = CDec(ligne.Cells("AncienPrix").Value)
                                                      Dim cout As Object = ligne.Cells("CoutReferenceDemande").Value
                                                      Dim net As Decimal = nouveau
                                                      If Convert.ToString(ligne.Cells("Perimetre").Value) = "FACTURE" Then net = DelegationPrixRegles.PrixNet(nouveau, New ContexteCommercialPrix With {.SousTotal = CDec(ligne.Cells("SousTotalContexte").Value), .Remise = CDec(ligne.Cells("RemiseContexte").Value)})
                                                      Dim marge As String = If(cout Is DBNull.Value OrElse CDec(cout) <= 0D, "Non enregistré", ((net - CDec(cout)) / CDec(cout) * 100D).ToString("N2") & " %")
                                                      zone.Text = Convert.ToString(ligne.Cells("Produit").Value) & " — " & Convert.ToString(ligne.Cells("Conditionnement").Value) & Environment.NewLine & "Périmètre : " & Convert.ToString(ligne.Cells("Perimetre").Value) & Environment.NewLine & "Demandeur : " & Convert.ToString(ligne.Cells("Demandeur").Value) & Environment.NewLine & "État : " & Convert.ToString(ligne.Cells("Statut").Value) & Environment.NewLine & "Ancien prix : " & ancien.ToString("N2") & " FC" & Environment.NewLine & "Prix demandé : " & nouveau.ToString("N2") & " FC" & Environment.NewLine & "Variation : " & (nouveau - ancien).ToString("N2") & " FC" & Environment.NewLine & "Marge sur coût enregistré : " & marge & Environment.NewLine & "Motif : " & Convert.ToString(ligne.Cells("Motif").Value) & Environment.NewLine & "Décision : " & Convert.ToString(ligne.Cells("MotifDecision").Value) & Environment.NewLine & "Date : " & Convert.ToString(ligne.Cells("DemandeLe").Value) & Environment.NewLine & "Expire le : " & Convert.ToString(ligne.Cells("ExpireLe").Value)
                                                      Dim fermer As New Button With {.Text = "FERMER", .Dock = DockStyle.Bottom, .Height = 38, .DialogResult = DialogResult.Cancel}
                                                      detail.Padding = New Padding(18) : detail.Controls.Add(zone) : detail.Controls.Add(fermer)
                                                      Dim decideur As String = Convert.ToString(ligne.Cells("Decideur").Value)
                                                      If Not Convert.IsDBNull(ligne.Cells("DecisionPar").Value) Then
                                                          zone.AppendText(Environment.NewLine & "Décision par : " & If(String.IsNullOrWhiteSpace(decideur), "Compte non disponible", decideur) & " (ID " & Convert.ToString(ligne.Cells("DecisionPar").Value) & ")" & Environment.NewLine & "Décision le (UTC) : " & Convert.ToString(ligne.Cells("DecisionLe").Value))
                                                      End If
                                                      zone.AppendText(Environment.NewLine & "Prix net par unité après remise : " & net.ToString("N2") & " FC" & Environment.NewLine & "Sous-total du panier approuvé : " & Convert.ToString(ligne.Cells("SousTotalContexte").Value) & Environment.NewLine & "Remise globale approuvée : " & Convert.ToString(ligne.Cells("RemiseContexte").Value))
                                                      AddHandler detail.Shown, Async Sub(s, args)
                                                                                   Try
                                                                                       Dim tarif As TarifDelegue = Await Task.Run(Function() _service.ConsulterTarifDemande(demandeId, _ecran))
                                                                                       If Not detail.IsDisposed Then zone.AppendText(Environment.NewLine & "Tarif officiel courant (consultation SQL) : " & tarif.Prix.ToString("N2") & " FC" & If(tarif.TypeActif, "", " — type désactivé"))
                                                                                   Catch ex As Exception
                                                                                       If Not detail.IsDisposed Then zone.AppendText(Environment.NewLine & "Tarif courant non consultable : " & ex.Message)
                                                                                   End Try
                                                                               End Sub
                                                      detail.CancelButton = fermer : detail.ShowDialog(Me)
                                                  End Using
                                              End Sub
            Controls.Add(grille) : Controls.Add(bas) : Controls.Add(actionsMetier) : Controls.Add(outils) : Controls.Add(synthese) : Controls.Add(entete)
            AddHandler Load, Async Sub(sender, e) Await ChargerAsync()
        End Sub
        Public Async Function ChargerAsync() As Task
            If _occupe Then Return
            _occupe = True
            boutonPrecedent.Enabled = False : boutonSuivant.Enabled = False
            synthese.Text = "Chargement des demandes…"
            Dim q As String = recherche.Text
            Dim etats As String() = {"", "EN_ATTENTE", "APPROUVEE", "REFUSEE", "ANNULEE", "EXPIREE"}
            Dim etat As String = etats(statut.SelectedIndex)
            Try
                Dim resultat As PageDemandesPrix = Await Task.Run(Function() _service.ListerPage(_page, 25, q, etat, _ecran))
                If IsDisposed Then Return
                _total = resultat.Total : grille.DataSource = resultat.Lignes
                grille.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
                For Each colonne As DataGridViewColumn In grille.Columns
                    colonne.AutoSizeMode = DataGridViewAutoSizeColumnMode.None
                    colonne.MinimumWidth = 125
                    colonne.Width = If(colonne.Name = "Produit", 190, If(colonne.Name = "Conditionnement", 145, 125))
                    If colonne.Name = "Motif" Then
                        colonne.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill : colonne.MinimumWidth = 200
                    End If
                Next
                grille.Columns("DemandeId").Visible = False
                grille.Columns("CoutReferenceDemande").Visible = False
                grille.Columns("MotifDecision").Visible = False
                grille.Columns("DecisionPar").Visible = False
                grille.Columns("Decideur").Visible = False
                grille.Columns("DecisionLe").Visible = False
                grille.Columns("SousTotalContexte").Visible = False
                grille.Columns("RemiseContexte").Visible = False
                grille.Columns("TypeTarifId").Visible = False
                For Each nom As String In New String() {"AncienPrix", "NouveauPrix"}
                    grille.Columns(nom).DefaultCellStyle.Format = "N2"
                    grille.Columns(nom).DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                Next
                grille.Columns("Perimetre").HeaderText = "Périmètre"
                grille.Columns("AncienPrix").HeaderText = "Prix de référence"
                grille.Columns("NouveauPrix").HeaderText = "Prix demandé"
                grille.Columns("DemandeLe").HeaderText = "Demandée le (UTC)"
                grille.Columns("ExpireLe").HeaderText = "Expire le (UTC)"
                grille.Columns("UtiliseeLe").HeaderText = "Appliquée le (UTC)"
                grille.Columns("QuantiteContexte").HeaderText = "Quantité de l'article"
                synthese.Text = "Demandes des employés : " & resultat.EnAttente.ToString() & " en attente • " & resultat.Approuvees.ToString() & " approuvées • " & resultat.Refusees.ToString() & " refusées" & Environment.NewLine & "TARIF : prix officiel • FACTURE : article uniquement, application après approbation"
                position.Text = "Page " & (_page + 1).ToString() & " • " & _total.ToString() & " résultat(s)"
                If _total = 0 Then synthese.Text = "Aucune demande pour ces filtres. Modifiez la recherche ou le statut."
            Catch ex As Exception
                grille.DataSource = Nothing : synthese.Text = "Demandes indisponibles : " & ex.Message
                _total = 0 : position.Text = "Consultation indisponible"
            Finally
                _occupe = False
                boutonPrecedent.Enabled = _page > 0
                boutonSuivant.Enabled = (_page + 1) * 25 < _total
            End Try
        End Function
    End Class
End Namespace
