Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireSuperAdminRoles
        Inherits Form

        Private ReadOnly _service As SuperAdminService
        Private ReadOnly gridRoles As DataGridView
        Private ReadOnly clbInterfaces As CheckedListBox
        Private ReadOnly txtNomRole As TextBox
        Private ReadOnly chkActif As CheckBox
        Private ReadOnly btnNouveau As Button
        Private ReadOnly btnEnregistrer As Button
        Private ReadOnly lblInfo As Label
        Private _roleIdCourant As Integer?
        Private _interfaces As DataTable

        Public Sub New()
            _service = New SuperAdminService()

            Text = "SuperAdmin - Rôles et privilèges"
            Width = 1100
            Height = 720
            StartPosition = FormStartPosition.CenterParent
            BackColor = Color.FromArgb(245, 247, 250)

            Dim root As New TableLayoutPanel() With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 1,
                .Padding = New Padding(16)
            }
            root.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 42))
            root.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 58))

            Dim leftCard As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(12)}
            Dim rightCard As New Panel() With {.Dock = DockStyle.Fill, .BackColor = Color.White, .Padding = New Padding(16)}

            gridRoles = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None
            }

            Dim leftTitle As New Label() With {.Text = "Rôles existants", .Dock = DockStyle.Top, .Height = 28, .Font = New Font("Segoe UI", 11, FontStyle.Bold)}
            leftCard.Controls.Add(gridRoles)
            leftCard.Controls.Add(leftTitle)

            Dim formLayout As New TableLayoutPanel() With {.Dock = DockStyle.Top, .Height = 120, .ColumnCount = 2, .RowCount = 3}
            formLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 160))
            formLayout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))

            formLayout.Controls.Add(New Label() With {.Text = "Nom du rôle", .AutoSize = True, .Anchor = AnchorStyles.Left, .Font = New Font("Segoe UI", 9, FontStyle.Bold)}, 0, 0)
            txtNomRole = New TextBox() With {.Dock = DockStyle.Top}
            formLayout.Controls.Add(txtNomRole, 1, 0)

            formLayout.Controls.Add(New Label() With {.Text = "État", .AutoSize = True, .Anchor = AnchorStyles.Left, .Font = New Font("Segoe UI", 9, FontStyle.Bold)}, 0, 1)
            chkActif = New CheckBox() With {.Text = "Rôle actif", .Checked = True, .AutoSize = True}
            formLayout.Controls.Add(chkActif, 1, 1)

            Dim buttonPanel As New FlowLayoutPanel() With {.Dock = DockStyle.Top, .Height = 42, .FlowDirection = FlowDirection.LeftToRight}
            btnNouveau = New Button() With {.Text = "Nouveau", .AutoSize = True}
            btnEnregistrer = New Button() With {.Text = "Enregistrer", .AutoSize = True}
            buttonPanel.Controls.Add(btnNouveau)
            buttonPanel.Controls.Add(btnEnregistrer)
            formLayout.Controls.Add(buttonPanel, 1, 2)

            Dim lblInterfaces As New Label() With {.Text = "Interfaces autorisées", .Dock = DockStyle.Top, .Height = 28, .Font = New Font("Segoe UI", 11, FontStyle.Bold)}
            clbInterfaces = New CheckedListBox() With {.Dock = DockStyle.Fill, .CheckOnClick = True, .BorderStyle = BorderStyle.FixedSingle}
            lblInfo = New Label() With {.Dock = DockStyle.Bottom, .Height = 42, .ForeColor = Color.DimGray, .Text = "SUPERADMIN hérite d'ADMIN et des interfaces techniques réservées."}

            rightCard.Controls.Add(clbInterfaces)
            rightCard.Controls.Add(lblInfo)
            rightCard.Controls.Add(lblInterfaces)
            rightCard.Controls.Add(formLayout)

            root.Controls.Add(leftCard, 0, 0)
            root.Controls.Add(rightCard, 1, 0)
            Controls.Add(root)

            AddHandler Load, AddressOf FormulaireSuperAdminRoles_Load
            AddHandler gridRoles.SelectionChanged, AddressOf ChargerRoleSelectionne
            AddHandler btnNouveau.Click, AddressOf NouveauRole
            AddHandler btnEnregistrer.Click, AddressOf EnregistrerRole
        End Sub

        Private Sub FormulaireSuperAdminRoles_Load(sender As Object, e As EventArgs)
            Try
                _service.AssurerInfrastructure()
                _interfaces = _service.ListerInterfaces()
                clbInterfaces.Items.Clear()
                For Each row As DataRow In _interfaces.Rows
                    clbInterfaces.Items.Add(New InterfaceItem(Convert.ToInt32(row("InterfaceId")), Convert.ToString(row("CodeInterface")), Convert.ToString(row("Libelle"))))
                Next

                ChargerRoles()
                NouveauRole(Nothing, EventArgs.Empty)
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireSuperAdminRoles", "Load", "Chargement des rôles impossible.", ex)
                MessageBox.Show("Impossible de charger les rôles et privilèges : " & ex.Message)
            End Try
        End Sub

        Private Sub ChargerRoles()
            Dim dt As DataTable = _service.ListerRoles()
            gridRoles.DataSource = dt
            If gridRoles.Columns.Contains("RoleId") Then
                gridRoles.Columns("RoleId").Visible = False
            End If
            If gridRoles.Columns.Contains("NomRole") Then
                gridRoles.Columns("NomRole").HeaderText = "Rôle"
            End If
            If gridRoles.Columns.Contains("EstActif") Then
                gridRoles.Columns("EstActif").HeaderText = "Actif"
            End If
        End Sub

        Private Sub ChargerRoleSelectionne(sender As Object, e As EventArgs)
            If gridRoles.CurrentRow Is Nothing OrElse gridRoles.CurrentRow.DataBoundItem Is Nothing Then
                Return
            End If

            Dim rowView As DataRowView = TryCast(gridRoles.CurrentRow.DataBoundItem, DataRowView)
            If rowView Is Nothing Then
                Return
            End If

            _roleIdCourant = Convert.ToInt32(rowView("RoleId"))
            txtNomRole.Text = Convert.ToString(rowView("NomRole"))
            chkActif.Checked = Convert.ToBoolean(rowView("EstActif"))

            For i As Integer = 0 To clbInterfaces.Items.Count - 1
                clbInterfaces.SetItemChecked(i, False)
            Next

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
        End Sub

        Private Sub NouveauRole(sender As Object, e As EventArgs)
            _roleIdCourant = Nothing
            txtNomRole.Clear()
            chkActif.Checked = True
            For i As Integer = 0 To clbInterfaces.Items.Count - 1
                clbInterfaces.SetItemChecked(i, False)
            Next
            txtNomRole.Focus()
        End Sub

        Private Sub EnregistrerRole(sender As Object, e As EventArgs)
            Dim nomRole As String = txtNomRole.Text.Trim().ToUpperInvariant()
            If nomRole = String.Empty Then
                MessageBox.Show("Le nom du rôle est obligatoire.")
                txtNomRole.Focus()
                Return
            End If

            Dim interfaceIds As New List(Of Integer)()
            For Each item As Object In clbInterfaces.CheckedItems
                Dim interfaceItem As InterfaceItem = TryCast(item, InterfaceItem)
                If interfaceItem IsNot Nothing Then
                    interfaceIds.Add(interfaceItem.InterfaceId)
                End If
            Next

            Try
                _service.EnregistrerRole(_roleIdCourant, nomRole, chkActif.Checked, interfaceIds)
                ChargerRoles()
                MessageBox.Show("Rôle enregistré.")
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireSuperAdminRoles", "EnregistrerRole", "Enregistrement du rôle impossible.", ex)
                MessageBox.Show("Impossible d'enregistrer le rôle : " & ex.Message)
            End Try
        End Sub

        Private NotInheritable Class InterfaceItem
            Public ReadOnly Property InterfaceId As Integer
            Public ReadOnly Property CodeInterface As String
            Public ReadOnly Property Libelle As String

            Public Sub New(interfaceId As Integer, codeInterface As String, libelle As String)
                Me.InterfaceId = interfaceId
                Me.CodeInterface = codeInterface
                Me.Libelle = libelle
            End Sub

            Public Overrides Function ToString() As String
                Return Libelle
            End Function
        End Class
    End Class
End Namespace
