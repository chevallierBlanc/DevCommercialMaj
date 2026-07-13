Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports System.Drawing.Drawing2D

Namespace DevCommerc8ak
    Public Class FormulaireSuperAdminRoles
        Inherits Form

        ' --- Services ---
        Private ReadOnly _service As SuperAdminService
        Private ReadOnly _log As New ProductionLogService()

        ' --- Composants UI ---
        Private gridRoles As DataGridView
        Private clbInterfaces As CheckedListBox
        Private txtNomRole As TextBox
        Private chkActif As CheckBox
        Private btnNouveau As Button
        Private btnEnregistrer As Button
        Private btnSupprimer As Button
        Private lblInfo As Label
        Private lblTitle As Label
        Private lblSubtitle As Label
        
        ' --- Données ---
        Private _roleIdCourant As Integer?
        Private _interfaces As DataTable

        ' --- Palette de Couleurs Enterprise ERP ---
        Private ReadOnly ColorBg As Color = Color.FromArgb(240, 242, 245)
        Private ReadOnly ColorHeaderBg As Color = Color.White
        Private ReadOnly ColorCardBg As Color = Color.White
        Private ReadOnly ColorPrimary As Color = Color.FromArgb(0, 102, 204) ' Bleu Enterprise
        Private ReadOnly ColorAccent As Color = Color.FromArgb(0, 102, 204)
        Private ReadOnly ColorSuccess As Color = Color.FromArgb(34, 197, 94)
        Private ReadOnly ColorDanger As Color = Color.FromArgb(211, 47, 47)
        Private ReadOnly ColorTextPrimary As Color = Color.FromArgb(33, 43, 54)
        Private ReadOnly ColorTextSecondary As Color = Color.FromArgb(99, 115, 129)
        Private ReadOnly ColorBorder As Color = Color.FromArgb(224, 224, 224)

        ' --- Polices ---
        Private ReadOnly FontMain As New Font("Segoe UI", 9.0F)
        Private ReadOnly FontBold As New Font("Segoe UI", 9.0F, FontStyle.Bold)
        Private ReadOnly FontTitle As New Font("Segoe UI", 15.0F, FontStyle.Bold)
        Private ReadOnly FontSubtitle As New Font("Segoe UI", 9.5F)
        Private ReadOnly FontButton As New Font("Segoe UI", 9.0F, FontStyle.Bold)

        Public Sub New()
            _service = New SuperAdminService()

            ' Configuration de la Form
            Me.Text = "Administration - Rôles et Privilèges"
            Me.Size = New Size(1200, 800)
            Me.MinimumSize = New Size(1000, 700)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = ColorBg
            Me.Font = FontMain
            Me.DoubleBuffered = True

            BuildUi()
            
            AddHandler Me.Load, AddressOf FormulaireSuperAdminRoles_Load
            AddHandler gridRoles.SelectionChanged, AddressOf ChargerRoleSelectionne
            AddHandler btnNouveau.Click, AddressOf NouveauRole
            AddHandler btnEnregistrer.Click, AddressOf EnregistrerRole
            AddHandler btnSupprimer.Click, AddressOf SupprimerRoleSelectionne
        End Sub

        Private Sub BuildUi()
            Me.Controls.Clear()

            ' --- Layout Principal ---
            Dim rootLayout As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 1,
                .RowCount = 2,
                .BackColor = ColorBg
            }
            rootLayout.RowStyles.Add(New RowStyle(SizeType.Absolute, 100)) ' Header
            rootLayout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))  ' Contenu

            ' --- 1. Header ---
            Dim pnlHeader As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorHeaderBg,
                .Padding = New Padding(30, 20, 30, 20)
            }
            
            lblTitle = New Label() With {
                .Text = "Gestion des Rôles et Privilèges",
                .Font = FontTitle,
                .ForeColor = ColorTextPrimary,
                .AutoSize = True,
                .Location = New Point(30, 20)
            }
            
            lblSubtitle = New Label() With {
                .Text = "Définissez les profils utilisateurs et gérez les autorisations d'accès aux modules du système.",
                .Font = FontSubtitle,
                .ForeColor = ColorTextSecondary,
                .AutoSize = True,
                .Location = New Point(30, 55)
            }
            
            pnlHeader.Controls.AddRange({lblTitle, lblSubtitle})
            rootLayout.Controls.Add(pnlHeader, 0, 0)

            ' --- 2. Zone de Contenu (Splitter) ---
            Dim pnlContent As New Panel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(30, 20, 30, 30)
            }
            
            Dim splitContainer As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 1
            }
            splitContainer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 40))
            splitContainer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 60))

            ' --- Gauche : Liste des Rôles ---
            Dim cardLeft As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorCardBg,
                .Padding = New Padding(20),
                .Margin = New Padding(0, 0, 10, 0)
            }
            
            Dim lblListTitle As New Label() With {
                .Text = "RÔLES EXISTANTS",
                .Font = FontBold,
                .ForeColor = ColorTextSecondary,
                .Dock = DockStyle.Top,
                .Height = 30
            }
            
            gridRoles = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None,
                .RowHeadersVisible = False,
                .EnableHeadersVisualStyles = False,
                .GridColor = ColorBorder,
                .ColumnHeadersHeight = 40
            }
            gridRoles.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 251)
            gridRoles.ColumnHeadersDefaultCellStyle.Font = FontBold
            gridRoles.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 254)
            gridRoles.DefaultCellStyle.SelectionForeColor = ColorPrimary

            cardLeft.Controls.AddRange({gridRoles, lblListTitle})
            splitContainer.Controls.Add(cardLeft, 0, 0)

            ' --- Droite : Détails et Privilèges ---
            Dim cardRight As New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = ColorCardBg,
                .Padding = New Padding(25),
                .Margin = New Padding(10, 0, 0, 0)
            }
            
            ' Formulaire de saisie
            Dim pnlForm As New Panel() With {.Dock = DockStyle.Top, .Height = 170}
            
            Dim lblNomRole As New Label() With {.Text = "NOM DU RÔLE", .Font = FontBold, .ForeColor = ColorTextSecondary, .Location = New Point(0, 0), .AutoSize = True}
            txtNomRole = New TextBox() With {.Location = New Point(0, 22), .Width = 350, .Font = FontMain, .BorderStyle = BorderStyle.FixedSingle}
            
            chkActif = New CheckBox() With {.Text = "Rôle actif et autorisé à se connecter", .Location = New Point(0, 60), .AutoSize = True, .Font = FontMain, .Checked = True}
            
            Dim pnlActions As New FlowLayoutPanel() With {
                .Dock = DockStyle.Top,
                .Location = New Point(0, 100),
                .Height = 50,
                .AutoSize = True,
                .AutoSizeMode = AutoSizeMode.GrowAndShrink,
                .WrapContents = False,
                .FlowDirection = FlowDirection.LeftToRight,
                .Margin = New Padding(0),
                .Padding = New Padding(0)
            }
            btnNouveau = New Button() With {.Text = "NOUVEAU", .Size = New Size(120, 38), .Margin = New Padding(0)}
            StyliserBouton(btnNouveau, Color.White, ColorTextSecondary, True)
            
            btnEnregistrer = New Button() With {.Text = "ENREGISTRER", .Size = New Size(150, 38), .Margin = New Padding(10, 0, 0, 0)}
            StyliserBouton(btnEnregistrer, ColorPrimary, Color.White, False)
            btnSupprimer = New Button() With {.Text = "SUPPRIMER", .Size = New Size(150, 38), .Margin = New Padding(10, 0, 0, 0)}
            StyliserBouton(btnSupprimer, ColorDanger, Color.White, False)
            btnSupprimer.Enabled = False

            pnlActions.Controls.AddRange({btnNouveau, btnEnregistrer, btnSupprimer})
            pnlForm.Controls.AddRange({lblNomRole, txtNomRole, chkActif, pnlActions})

            ' Liste des interfaces
            Dim pnlInterfaces As New Panel() With {.Dock = DockStyle.Fill, .Padding = New Padding(0, 20, 0, 0)}
            Dim lblIntTitle As New Label() With {.Text = "PRIVILÈGES ET ACCÈS AUX MODULES", .Font = FontBold, .ForeColor = ColorTextSecondary, .Dock = DockStyle.Top, .Height = 30}
            
            clbInterfaces = New CheckedListBox() With {
                .Dock = DockStyle.Fill, 
                .CheckOnClick = True, 
                .BorderStyle = BorderStyle.FixedSingle,
                .Font = FontMain,
                .BackColor = Color.White
            }
            
            lblInfo = New Label() With {
                .Text = "Note : Le rôle SUPERADMIN hérite automatiquement de tous les privilèges techniques.",
                .Font = New Font("Segoe UI", 8.5F, FontStyle.Italic),
                .ForeColor = ColorTextSecondary,
                .Dock = DockStyle.Bottom,
                .Height = 40,
                .TextAlign = ContentAlignment.MiddleLeft
            }

            pnlInterfaces.Controls.AddRange({clbInterfaces, lblIntTitle, lblInfo})
            
            cardRight.Controls.AddRange({pnlInterfaces, pnlForm})
            splitContainer.Controls.Add(cardRight, 1, 0)

            pnlContent.Controls.Add(splitContainer)
            rootLayout.Controls.Add(pnlContent, 0, 1)

            Me.Controls.Add(rootLayout)
        End Sub

        Private Sub StyliserBouton(btn As Button, bgColor As Color, fgColor As Color, hasBorder As Boolean)
            btn.FlatStyle = FlatStyle.Flat
            btn.BackColor = bgColor
            btn.ForeColor = fgColor
            btn.Font = FontButton
            btn.Cursor = Cursors.Hand
            btn.FlatAppearance.BorderSize = If(hasBorder, 1, 0)
            If hasBorder Then btn.FlatAppearance.BorderColor = ColorBorder
        End Sub

        Private Sub FormulaireSuperAdminRoles_Load(sender As Object, e As EventArgs)
            Try
                Me.Cursor = Cursors.WaitCursor
                _service.AssurerInfrastructure()
                _interfaces = _service.ListerInterfaces()
                
                clbInterfaces.Items.Clear()
                For Each row As DataRow In _interfaces.Rows
                    clbInterfaces.Items.Add(New InterfaceItem(
                        Convert.ToInt32(row("InterfaceId")), 
                        Convert.ToString(row("CodeInterface")), 
                        Convert.ToString(row("Libelle"))
                    ))
                Next

                ChargerRoles()
                NouveauRole(Nothing, EventArgs.Empty)
            Catch ex As Exception
                _log.Error("FormulaireSuperAdminRoles", "Load", "Erreur de chargement.", ex)
                MessageBox.Show("Impossible de charger les rôles : " & ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Sub ChargerRoles()
            Dim dt As DataTable = _service.ListerRoles()
            gridRoles.DataSource = dt
            
            If gridRoles.Columns.Contains("RoleId") Then gridRoles.Columns("RoleId").Visible = False
            If gridRoles.Columns.Contains("NomRole") Then gridRoles.Columns("NomRole").HeaderText = "RÔLE"
            If gridRoles.Columns.Contains("EstActif") Then gridRoles.Columns("EstActif").HeaderText = "ACTIF"
            UpdateDeleteButtonState()
        End Sub

        Private Sub ChargerRoleSelectionne(sender As Object, e As EventArgs)
            If gridRoles.CurrentRow Is Nothing OrElse gridRoles.CurrentRow.DataBoundItem Is Nothing Then Return

            Dim rowView As DataRowView = TryCast(gridRoles.CurrentRow.DataBoundItem, DataRowView)
            If rowView Is Nothing Then Return

            _roleIdCourant = Convert.ToInt32(rowView("RoleId"))
            txtNomRole.Text = Convert.ToString(rowView("NomRole"))
            chkActif.Checked = Convert.ToBoolean(rowView("EstActif"))

            ' Reset des sélections
            For i As Integer = 0 To clbInterfaces.Items.Count - 1
                clbInterfaces.SetItemChecked(i, False)
            Next

            ' Chargement des autorisations réelles
            Dim autorisations As DataTable = _service.ListerInterfacesParRole(_roleIdCourant.Value)
            Dim idsAutorises As New HashSet(Of Integer)()
            
            For Each row As DataRow In autorisations.Rows
                If Convert.ToBoolean(row("Autorise")) Then
                    idsAutorises.Add(Convert.ToInt32(row("InterfaceId")))
                End If
            Next

            For i As Integer = 0 To clbInterfaces.Items.Count - 1
                Dim item As InterfaceItem = TryCast(clbInterfaces.Items(i), InterfaceItem)
                If item IsNot Nothing AndAlso idsAutorises.Contains(item.InterfaceId) Then
                    clbInterfaces.SetItemChecked(i, True)
                End If
            Next

            UpdateDeleteButtonState()
        End Sub

        Private Sub NouveauRole(sender As Object, e As EventArgs)
            _roleIdCourant = Nothing
            txtNomRole.Clear()
            chkActif.Checked = True
            For i As Integer = 0 To clbInterfaces.Items.Count - 1
                clbInterfaces.SetItemChecked(i, False)
            Next
            UpdateDeleteButtonState()
            txtNomRole.Focus()
        End Sub

        Private Sub EnregistrerRole(sender As Object, e As EventArgs)
            Dim nomRole As String = txtNomRole.Text.Trim().ToUpperInvariant()
            If String.IsNullOrEmpty(nomRole) Then
                MessageBox.Show("Le nom du rôle est obligatoire.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtNomRole.Focus()
                Return
            End If

            If _service.RoleExisteDeja(nomRole, _roleIdCourant) Then
                MessageBox.Show("Un rôle portant ce nom existe déjà.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtNomRole.Focus()
                Return
            End If

            Dim interfaceIds As New List(Of Integer)()
            For Each item As Object In clbInterfaces.CheckedItems
                Dim intItem As InterfaceItem = TryCast(item, InterfaceItem)
                If intItem IsNot Nothing Then
                    interfaceIds.Add(intItem.InterfaceId)
                End If
            Next

            If _roleIdCourant.HasValue AndAlso String.Equals(SessionUtilisateur.Role, nomRole, StringComparison.OrdinalIgnoreCase) AndAlso Not SelectionContientAccesCritique(interfaceIds) Then
                MessageBox.Show("Vous ne pouvez pas retirer tous les accès critiques du rôle actuellement connecté.", "Sécurité", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Try
                Me.Cursor = Cursors.WaitCursor
                _service.EnregistrerRole(_roleIdCourant, nomRole, chkActif.Checked, interfaceIds)
                ChargerRoles()
                AppEvents.OnRolePermissionsChanged()
                MessageBox.Show("Le rôle et ses privilèges ont été enregistrés avec succès.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                _log.Error("FormulaireSuperAdminRoles", "EnregistrerRole", "Erreur d'enregistrement.", ex)
                MessageBox.Show("Erreur lors de l'enregistrement : " & ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Sub SupprimerRoleSelectionne(sender As Object, e As EventArgs)
            If Not _roleIdCourant.HasValue OrElse _roleIdCourant.Value <= 0 Then
                MessageBox.Show("Sélectionnez un rôle à supprimer.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim nomRole As String = txtNomRole.Text.Trim().ToUpperInvariant()
            If String.Equals(nomRole, "SUPERADMIN", StringComparison.OrdinalIgnoreCase) Then
                MessageBox.Show("Le rôle SUPERADMIN ne peut pas être supprimé.", "Protection", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If String.Equals(SessionUtilisateur.Role, nomRole, StringComparison.OrdinalIgnoreCase) Then
                MessageBox.Show("Vous ne pouvez pas supprimer le rôle de votre session active.", "Protection", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If _service.EstDernierRoleCritique(_roleIdCourant.Value) Then
                MessageBox.Show("Ce rôle est le dernier rôle actif disposant d'un accès critique au système. Suppression interdite.", "Protection", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim utilisateursAffectes As Integer = _service.CompterUtilisateursParRole(_roleIdCourant.Value)
            If utilisateursAffectes > 0 Then
                MessageBox.Show("Ce rôle est encore affecté à " & utilisateursAffectes.ToString() & " utilisateur(s)." & Environment.NewLine &
                                "Réaffectez d'abord les utilisateurs ou désactivez le rôle avant de le supprimer.", "Rôle utilisé", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim confirmation As DialogResult = MessageBox.Show("Voulez-vous réellement supprimer le rôle « " & nomRole & " » ?" & Environment.NewLine &
                                                               "Cette opération supprimera également ses associations de privilèges.", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            If confirmation <> DialogResult.Yes Then
                Return
            End If

            Try
                Me.Cursor = Cursors.WaitCursor
                _service.SupprimerRole(_roleIdCourant.Value, nomRole)
                ChargerRoles()
                NouveauRole(Nothing, EventArgs.Empty)
                MessageBox.Show("Le rôle a été supprimé.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                _log.Error("FormulaireSuperAdminRoles", "SupprimerRole", "Erreur de suppression.", ex)
                MessageBox.Show("Impossible de supprimer le rôle : " & ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                Me.Cursor = Cursors.Default
            End Try
        End Sub

        Private Sub UpdateDeleteButtonState()
            If btnSupprimer Is Nothing Then
                Return
            End If

            Dim nomRole As String = If(txtNomRole Is Nothing, String.Empty, txtNomRole.Text.Trim())
            btnSupprimer.Enabled = _roleIdCourant.HasValue AndAlso _roleIdCourant.Value > 0 AndAlso
                                   Not String.Equals(nomRole, "SUPERADMIN", StringComparison.OrdinalIgnoreCase)
        End Sub

        Private Function SelectionContientAccesCritique(interfaceIds As IEnumerable(Of Integer)) As Boolean
            If interfaceIds Is Nothing OrElse _interfaces Is Nothing Then
                Return False
            End If

            Dim ids As HashSet(Of Integer) = New HashSet(Of Integer)(interfaceIds)
            For Each row As DataRow In _interfaces.Rows
                Dim code As String = Convert.ToString(row("CodeInterface"))
                If code = "ADMINISTRATION" OrElse code = "PARAMETRES" OrElse code = "SUPERADMIN_ROLES" OrElse code = "SUPERADMIN_TECH" Then
                    Dim interfaceId As Integer = Convert.ToInt32(row("InterfaceId"))
                    If ids.Contains(interfaceId) Then
                        Return True
                    End If
                End If
            Next

            Return False
        End Function

        ' --- Classe Interne pour les Items de la CheckedListBox ---
        Private NotInheritable Class InterfaceItem
            Public ReadOnly Property InterfaceId As Integer
            Public ReadOnly Property CodeInterface As String
            Public ReadOnly Property Libelle As String

            Public Sub New(id As Integer, code As String, libelle As String)
                Me.InterfaceId = id
                Me.CodeInterface = code
                Me.Libelle = libelle
            End Sub

            Public Overrides Function ToString() As String
                Return Libelle
            End Function
        End Class

    End Class
End Namespace
