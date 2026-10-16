Option Strict On
Option Explicit On
Imports System
Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class ModesUtilisateurPanel
        Inherits UserControl
        Private ReadOnly _service As AttributionModeService
        Private ReadOnly roles As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Dock = DockStyle.Fill}
        Private ReadOnly compte As New Label With {.AutoSize = True, .MaximumSize = New Size(600, 0), .Font = New Font("Segoe UI", 11, FontStyle.Bold), .ForeColor = Color.FromArgb(52, 73, 94)}
        Private ReadOnly etat As New Label With {.AutoSize = True, .MaximumSize = New Size(600, 0)}
        Private ReadOnly combine As New CheckBox With {.Text = "Autoriser Vente & Encaissement pour ce rôle", .AutoSize = True}
        Private ReadOnly motif As New TextBox With {.Multiline = True, .Height = 55, .MaxLength = 1000, .Dock = DockStyle.Fill}
        Private ReadOnly enregistrer As New Button With {.Text = "ENREGISTRER CETTE ATTRIBUTION", .AutoSize = True, .BackColor = Color.FromArgb(41, 128, 185), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
        Private _id As Integer
        Private _table As DataTable
        Private _chargement As Boolean
        Private _peutModifier As Boolean
        Private ReadOnly accorder As New Button With {.Text = "AUTORISER MON RÔLE À GÉRER LES MODES", .AutoSize = True, .Visible = False}
        Public Sub New(service As AttributionModeService)
            _service = service
            Dock = DockStyle.Fill : BackColor = Color.White : Padding = New Padding(14)
            BorderStyle = BorderStyle.FixedSingle
            AutoScaleMode = AutoScaleMode.Dpi
            Font = New Font("Segoe UI", 9.5F)
            Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .AutoScroll = True}
            root.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            Dim titre As New Label With {.Text = "MODES PAR RÔLE", .AutoSize = True, .ForeColor = Color.FromArgb(41, 128, 185), .Font = New Font(Font, FontStyle.Bold)}
            Dim aide As New Label With {.Text = "Sélectionnez un compte à gauche puis son rôle ci-dessous. Chaque attribution est indépendante. Elle prend effet à la prochaine connexion.", .AutoSize = True, .MaximumSize = New Size(600, 0), .ForeColor = Color.FromArgb(90, 100, 110)}
            Dim actualiser As New Button With {.Text = "ACTUALISER LES AUTORISATIONS", .AutoSize = True}
            For Each c As Control In New Control() {titre, compte, aide, New Label With {.Text = "Rôle du compte sélectionné dans le répertoire", .AutoSize = True}, roles, etat, accorder, combine, New Label With {.Text = "Motif de l'attribution ou du retrait", .AutoSize = True}, motif, enregistrer, actualiser}
                root.RowStyles.Add(New RowStyle(SizeType.AutoSize))
                root.Controls.Add(c, 0, root.Controls.Count)
                c.Margin = New Padding(0, 0, 0, 10)
                If TypeOf c Is Button Then
                    Dim bouton As Button = DirectCast(c, Button)
                    bouton.FlatStyle = FlatStyle.Flat
                    bouton.MinimumSize = New Size(0, 38)
                    bouton.Padding = New Padding(6)
                    bouton.Dock = DockStyle.Top
                    bouton.FlatAppearance.BorderColor = Color.FromArgb(220, 225, 230)
                    If bouton IsNot enregistrer Then
                        bouton.BackColor = Color.White
                        bouton.ForeColor = Color.FromArgb(52, 73, 94)
                        bouton.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 242, 250)
                    End If
                End If
            Next
            Controls.Add(root)
            AddHandler roles.SelectedIndexChanged, Sub() AfficherRole()
            AddHandler combine.CheckedChanged, Sub() ActualiserBouton()
            AddHandler motif.TextChanged, Sub() ActualiserBouton()
            AddHandler actualiser.Click, Sub()
                                             Dim roleAvant As Object = roles.SelectedValue
                                             ChargerCompte(_id, compte.Text)
                                             If roleAvant IsNot Nothing AndAlso _table IsNot Nothing Then roles.SelectedValue = roleAvant
                                         End Sub
            AddHandler enregistrer.Click, AddressOf EnregistrerAttribution
            AddHandler accorder.Click, Sub()
                                          If MessageBox.Show(Me, "Ajouter uniquement le privilège d'administration des modes au rôle ACTIF « " & SessionUtilisateur.NomRoleActif & " » ? Cela concerne tous les comptes de ce rôle, mais n'attribue Vente & Encaissement à aucun utilisateur. Les autres privilèges restent inchangés.", "Attribution explicite d'un privilège", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Return
                                          Dim raison As String = DialogueMotifOperation.Demander(Me, "Motif de l'ajout du privilège au rôle actif")
                                          If raison Is Nothing Then Return
                                          Try
                                              _service.AccorderAdministrationAuRoleActif(raison)
                                              ChargerCompte(_id, compte.Text)
                                          Catch ex As Exception
                                              MessageBox.Show(Me, ex.Message, "Privilège non accordé", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                          End Try
                                      End Sub
            AddHandler Resize, Sub()
                                   Dim largeur As Integer = Math.Max(180, ClientSize.Width - 45)
                                   For Each label As Label In New Label() {compte, aide, etat}
                                       label.MaximumSize = New Size(largeur, 0)
                                   Next
                               End Sub
            ChargerCompte(0, "Sélectionnez un compte.")
        End Sub
        Public Sub ChargerCompte(id As Integer, nom As String)
            _chargement = True
            _id = id : compte.Text = nom : motif.Clear()
            roles.DataSource = Nothing : roles.Enabled = False : _table = Nothing
            _peutModifier = False : accorder.Visible = False
            combine.Enabled = False : combine.Checked = False : enregistrer.Enabled = False
            Try
                If id <= 0 Then
                    etat.Text = "Enregistrez d'abord le compte et ses rôles autorisés."
                    Return
                End If
                _table = _service.Lister(id)
                _peutModifier = CBool(_table.ExtendedProperties("PeutModifier"))
                accorder.Visible = Not _peutModifier AndAlso CBool(_table.ExtendedProperties("PeutAccorder"))
                roles.DisplayMember = "NomRole" : roles.ValueMember = "RoleId" : roles.DataSource = _table
                roles.Enabled = _table.Rows.Count > 0
                If _table.Rows.Count = 0 Then etat.Text = "Aucun rôle actif enregistré pour ce compte."
            Catch ex As Exception
                etat.Text = "Configuration indisponible : " & ex.Message & Environment.NewLine & "Le responsable doit posséder Administration et « Utilisateurs : attribuer Vente et Encaissement par rôle » dans Rôles et privilèges. Aucun droit n'est ajouté automatiquement."
            Finally
                _chargement = False
            End Try
            If _table IsNot Nothing Then AfficherRole()
        End Sub
        Private Function Selection() As DataRow
            Dim item As DataRowView = TryCast(roles.SelectedItem, DataRowView)
            Return If(item Is Nothing, Nothing, item.Row)
        End Function
        Private Sub AfficherRole()
            If _chargement Then Return
            Dim row As DataRow = Selection()
            If row Is Nothing Then Return
            _chargement = True
            combine.Checked = CBool(row("VenteEncaissementAutorise"))
            combine.Enabled = _peutModifier AndAlso (CBool(row("CombineEligible")) OrElse combine.Checked)
            motif.Clear()
            etat.Text = "Facturation classique : " & If(CBool(row("FacturationEligible")), "droits présents", "non autorisée par ce rôle") & Environment.NewLine &
                "Caisse classique : " & If(CBool(row("CaisseEligible")), "droits présents", "non autorisée par ce rôle") & Environment.NewLine &
                "Attribution combinée : " & If(combine.Checked, "OUI", "NON") & Environment.NewLine &
                If(Not CBool(row("CombineEligible")), "Éligibilité manquante : accès Facturation, Créer facture, accès Caisse, Encaisser et Mode combiné requis pour ce rôle.", If(Not CBool(row("GestionModesActive")) OrElse Not CBool(row("ModeCombineAutorise")), "Attribution conservée, mais mode global désactivé dans Paramètres.", If(Not CBool(row("CompteActif")) OrElse CBool(row("EstVerrouille")), "Compte désactivé ou verrouillé : connexion refusée.", If(combine.Checked, "Vente & Encaissement disponible à la prochaine connexion.", "Cochez puis enregistrez pour autoriser ce couple compte–rôle."))))
            If Not _peutModifier Then etat.Text &= Environment.NewLine & "LECTURE SEULE : UTILISATEUR_MODES manque au rôle actif. Utilisez l'action explicite ci-dessous si vous gérez les privilèges, sinon contactez un responsable habilité dans Rôles et privilèges."
            _chargement = False
            ActualiserBouton()
        End Sub
        Private Sub ActualiserBouton()
            Dim row As DataRow = Selection()
            enregistrer.Enabled = _peutModifier AndAlso Not _chargement AndAlso row IsNot Nothing AndAlso motif.Text.Trim().Length > 0 AndAlso combine.Checked <> CBool(row("VenteEncaissementAutorise"))
        End Sub
        Private Sub EnregistrerAttribution(sender As Object, e As EventArgs)
            Dim row As DataRow = Selection()
            If row Is Nothing Then Return
            Try
                _service.Attribuer(_id, CInt(row("RoleId")), combine.Checked, CBool(row("VenteEncaissementAutorise")), motif.Text)
                Dim roleId As Integer = CInt(row("RoleId"))
                ChargerCompte(_id, compte.Text)
                roles.SelectedValue = roleId
            Catch ex As Exception
                MessageBox.Show(Me, ex.Message, "Attribution refusée", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub
    End Class
End Namespace
