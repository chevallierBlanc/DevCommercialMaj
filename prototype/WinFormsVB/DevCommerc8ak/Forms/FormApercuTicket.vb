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
        Private ReadOnly _creerDocumentImpression As Func(Of PrintDocument)
        Private _impressionEnCours As Boolean

        Public Sub New(documentApercu As PrintDocument, creerDocumentImpression As Func(Of PrintDocument))
            If documentApercu Is Nothing Then Throw New ArgumentNullException(NameOf(documentApercu))
            If creerDocumentImpression Is Nothing Then Throw New ArgumentNullException(NameOf(creerDocumentImpression))

            _creerDocumentImpression = creerDocumentImpression

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

            barreActions.Controls.Add(_btnFermer)
            barreActions.Controls.Add(_btnImprimer)

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
                Using doc As PrintDocument = _creerDocumentImpression()
                    doc.Print()
                End Using

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
