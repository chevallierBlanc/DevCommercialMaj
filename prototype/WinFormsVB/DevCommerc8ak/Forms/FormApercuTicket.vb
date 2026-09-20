Option Strict On
Option Explicit On

Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormApercuTicket
        Inherits Form

        Private ReadOnly _preview As PrintPreviewControl
        Private ReadOnly _btnImprimer As Button
        Private ReadOnly _btnFermer As Button
        Private ReadOnly _cmbZoom As ComboBox
        Private ReadOnly _btnAjusterPage As Button
        Private ReadOnly _creerDocumentImpression As Func(Of PrintDocument)
        Private ReadOnly _lancerImpression As Action
        Private _impressionEnCours As Boolean

        Public Sub New(documentApercu As PrintDocument, creerDocumentImpression As Func(Of PrintDocument))
            Me.New(documentApercu, creerDocumentImpression, Nothing)
        End Sub

        Public Sub New(documentApercu As PrintDocument, lancerImpression As Action)
            Me.New(documentApercu, Nothing, lancerImpression)
        End Sub

        Private Sub New(documentApercu As PrintDocument, creerDocumentImpression As Func(Of PrintDocument), lancerImpression As Action)
            If documentApercu Is Nothing Then Throw New ArgumentNullException(NameOf(documentApercu))
            If creerDocumentImpression Is Nothing AndAlso lancerImpression Is Nothing Then Throw New ArgumentNullException(NameOf(creerDocumentImpression))

            _creerDocumentImpression = creerDocumentImpression
            _lancerImpression = lancerImpression

            Text = "Aperçu ticket"
            Width = 1000
            Height = 720
            StartPosition = FormStartPosition.CenterParent
            KeyPreview = True
            Font = New Font("Segoe UI", 9.0F)
            BackColor = Color.FromArgb(245, 247, 250)

            Dim barreActions As New FlowLayoutPanel() With {
                .Dock = DockStyle.Top,
                .Height = 48,
                .FlowDirection = FlowDirection.RightToLeft,
                .WrapContents = False,
                .Padding = New Padding(10, 8, 10, 8),
                .BackColor = Color.White
            }

            _btnImprimer = New Button() With {
                .Text = "Imprimer",
                .Width = 120,
                .Height = 30,
                .BackColor = Color.FromArgb(41, 128, 185),
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat
            }
            _btnImprimer.FlatAppearance.BorderSize = 0

            _btnFermer = New Button() With {
                .Text = "Fermer",
                .Width = 100,
                .Height = 30,
                .BackColor = Color.FromArgb(230, 235, 240),
                .ForeColor = Color.FromArgb(52, 73, 94),
                .FlatStyle = FlatStyle.Flat
            }
            _btnFermer.FlatAppearance.BorderSize = 0

            _cmbZoom = New ComboBox() With {
                .Width = 90,
                .Height = 30,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = Font
            }
            _cmbZoom.Items.AddRange(New Object() {"50 %", "75 %", "100 %", "125 %", "150 %", "200 %"})
            _cmbZoom.SelectedItem = "100 %"

            _btnAjusterPage = New Button() With {
                .Text = "Ajuster à la page",
                .Width = 130,
                .Height = 30,
                .BackColor = Color.FromArgb(230, 235, 240),
                .ForeColor = Color.FromArgb(52, 73, 94),
                .FlatStyle = FlatStyle.Flat
            }
            _btnAjusterPage.FlatAppearance.BorderSize = 0

            barreActions.Controls.Add(_btnFermer)
            barreActions.Controls.Add(_btnImprimer)
            barreActions.Controls.Add(_cmbZoom)
            barreActions.Controls.Add(_btnAjusterPage)

            _preview = New PrintPreviewControl() With {
                .Dock = DockStyle.Fill,
                .Document = documentApercu,
                .AutoZoom = True,
                .BackColor = Color.White
            }

            Controls.Add(_preview)
            Controls.Add(barreActions)

            AcceptButton = _btnImprimer
            CancelButton = _btnFermer

            AddHandler _btnImprimer.Click, Sub() LancerImpressionUnique()
            AddHandler _btnFermer.Click, Sub()
                                             DialogResult = DialogResult.Cancel
                                             Close()
                                         End Sub
            AddHandler _btnAjusterPage.Click, Sub()
                                                  _preview.AutoZoom = True
                                              End Sub
            AddHandler _cmbZoom.SelectedIndexChanged, AddressOf ChangerZoom
        End Sub

        Private Sub ChangerZoom(sender As Object, e As EventArgs)
            Dim texte As String = If(_cmbZoom.SelectedItem Is Nothing, String.Empty, _cmbZoom.SelectedItem.ToString()).Replace("%", String.Empty).Trim()
            Dim pourcentage As Integer
            If Integer.TryParse(texte, pourcentage) AndAlso pourcentage > 0 Then
                _preview.AutoZoom = False
                _preview.Zoom = pourcentage / 100.0R
            End If
        End Sub

        Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
            If keyData = Keys.Enter Then
                LancerImpressionUnique()
                Return True
            End If

            If keyData = Keys.Escape Then
                DialogResult = DialogResult.Cancel
                Close()
                Return True
            End If

            Return MyBase.ProcessCmdKey(msg, keyData)
        End Function

        Private Sub LancerImpressionUnique()
            If _impressionEnCours Then Return

            _impressionEnCours = True
            Try
                Debug.WriteLine("ENTER aperçu ticket détecté")
                If _lancerImpression IsNot Nothing Then
                    _lancerImpression()
                Else
                    Using doc As PrintDocument = _creerDocumentImpression()
                        doc.Print()
                    End Using
                End If

                DialogResult = DialogResult.OK
                Close()
            Catch ex As Exception
                _impressionEnCours = False
                Dim log As New ProductionLogService()
                log.Error("FormApercuTicket", "LancerImpressionUnique", "Impossible de lancer l'impression du ticket depuis l'aperçu.", ex)
                MessageBox.Show(Me, "Impossible de lancer l’impression du ticket.", "Impression", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub
    End Class
End Namespace
