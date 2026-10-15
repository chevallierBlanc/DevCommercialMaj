Option Strict On
Option Explicit On
Imports System
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class ReglementComptoirPanel
        Inherits UserControl
        Public Property Reglement As ReglementComptoir
        Public Event Confirmation As EventHandler
        Public Event Annulation As EventHandler
        Public Sub New(totalFc As Decimal, tauxUsd As Decimal)
            BackColor = Color.FromArgb(245, 247, 250) : Font = New Font("Segoe UI", 10)
            AutoScroll = True : Padding = New Padding(16)
            Dim carte As New TableLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .ColumnCount = 2, .MaximumSize = New Size(560, 0), .Padding = New Padding(14), .BackColor = Color.White}
            carte.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 43))
            carte.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 57))
            Dim titre As New Label With {.Text = "RÈGLEMENT", .AutoSize = True, .Font = New Font(Font, FontStyle.Bold), .ForeColor = Color.FromArgb(41, 128, 185)}
            Dim total As New Label With {.Text = totalFc.ToString("N2") & " FC", .AutoSize = True, .Font = New Font("Segoe UI", 20, FontStyle.Bold)}
            Dim devise As New ComboBox With {.Name = "DeviseRecue", .DropDownStyle = ComboBoxStyle.DropDownList, .Width = 220}
            devise.Items.Add("FC") : If tauxUsd > 0D Then devise.Items.Add("USD")
            devise.SelectedIndex = 0
            Dim moyen As New ComboBox With {.Name = "MoyenPaiement", .DropDownStyle = ComboBoxStyle.DropDownList, .Width = 220}
            moyen.Items.AddRange(New Object() {"CASH", "MOBILE_MONEY", "CARTE", "AUTRE"}) : moyen.SelectedIndex = 0
            Dim recu As New TextBox With {.Name = "MontantRecu", .Width = 220, .MaxLength = 30, .TextAlign = HorizontalAlignment.Right, .Text = totalFc.ToString("0.00", CultureInfo.CurrentCulture)}
            Dim reference As New TextBox With {.Width = 220, .MaxLength = 120}
            Dim referenceLabel As New Label With {.Text = "Référence du paiement", .AutoSize = True}
            Dim externe As New CheckBox With {.Text = "Paiement externe effectivement reçu et confirmé", .Height = 46, .Dock = DockStyle.Top}
            Dim rendu As New Label With {.Name = "EtatReglement", .AutoSize = True, .Font = New Font(Font, FontStyle.Bold), .Padding = New Padding(8), .BackColor = Color.FromArgb(235, 245, 251)}
            Dim confirmer As New Button With {.Name = "ConfirmerReglement", .Text = "CONFIRMER LA VENTE ET LE PAIEMENT", .Dock = DockStyle.Top, .Height = 44, .BackColor = Color.FromArgb(39, 174, 96), .ForeColor = Color.White}
            Dim retour As New Button With {.Text = "REVENIR AU PANIER", .Dock = DockStyle.Top, .Height = 36}
            Dim ajouter As Action(Of Control, Control) = Sub(libelle, champ)
                                                             Dim ligne As Integer = carte.RowCount
                                                             carte.RowCount += 1
                                                             carte.RowStyles.Add(New RowStyle(SizeType.AutoSize))
                                                             libelle.Margin = New Padding(0, 5, 10, 12)
                                                             carte.Controls.Add(libelle, 0, ligne)
                                                             If champ Is Nothing Then
                                                                 carte.SetColumnSpan(libelle, 2)
                                                             Else
                                                                 champ.Margin = New Padding(0, 0, 0, 12)
                                                                 champ.Anchor = AnchorStyles.Left Or AnchorStyles.Right
                                                                 champ.MaximumSize = New Size(260, 0)
                                                                 carte.Controls.Add(champ, 1, ligne)
                                                             End If
                                                         End Sub
            ajouter(titre, Nothing) : ajouter(total, Nothing)
            ajouter(New Label With {.Text = "Montant de la facture", .AutoSize = True}, New Label With {.Text = "FC (devise de référence)", .AutoSize = True})
            ajouter(New Label With {.Text = "Devise reçue", .AutoSize = True}, devise)
            ajouter(New Label With {.Text = "Moyen de paiement", .AutoSize = True}, moyen)
            ajouter(New Label With {.Text = "Montant reçu", .AutoSize = True}, recu)
            ajouter(rendu, Nothing)
            ajouter(New Label With {.Text = If(tauxUsd > 0D, "Taux officiel : 1 USD = " & tauxUsd.ToString("N2") & " FC. Rendu en FC.", "USD indisponible : taux officiel non configuré."), .AutoSize = True, .MaximumSize = New Size(480, 0)}, Nothing)
            ajouter(referenceLabel, reference) : ajouter(externe, Nothing)
            ajouter(New Label With {.Text = "Règlement intégral. Paiement partiel : mettez en attente puis utilisez la caisse. Aucune confirmation externe automatique.", .AutoSize = True, .MaximumSize = New Size(480, 0)}, Nothing)
            ajouter(confirmer, Nothing) : ajouter(retour, Nothing)
            Dim actualiser As Action = Sub()
                                          Dim montant As Decimal
                                          Dim cash As Boolean = CStr(moyen.SelectedItem) = "CASH"
                                          reference.Visible = Not cash : referenceLabel.Visible = Not cash : externe.Visible = Not cash
                                          confirmer.Enabled = False
                                          rendu.ForeColor = Color.FromArgb(192, 57, 43)
                                          If Not EncaissementAuditRegles.LireMontant(recu.Text, CultureInfo.CurrentCulture, montant) Then
                                              rendu.Text = "Saisissez un montant valide (séparateur décimal : " & CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator & ")."
                                              Return
                                          End If
                                          Try
                                              Dim fc As Decimal = EncaissementAuditRegles.ConvertirRecuFc(montant, CStr(devise.SelectedItem), tauxUsd)
                                              rendu.ForeColor = If(fc < totalFc, Color.FromArgb(192, 57, 43), Color.FromArgb(39, 140, 80))
                                              rendu.Text = If(fc < totalFc, "Montant insuffisant : " & (totalFc - fc).ToString("N2") & " FC", "Monnaie à rendre : " & Math.Max(0D, fc - totalFc).ToString("N2") & " FC")
                                              If Not cash AndAlso fc >= totalFc Then
                                                  rendu.Text = If(fc = totalFc, "Montant confirmé : " & fc.ToString("N2") & " FC", "Le paiement externe doit correspondre exactement au total.")
                                                  If fc <> totalFc Then rendu.ForeColor = Color.FromArgb(192, 57, 43)
                                              End If
                                              confirmer.Enabled = fc >= totalFc AndAlso (cash OrElse (fc = totalFc AndAlso externe.Checked AndAlso reference.Text.Trim() <> ""))
                                          Catch ex As Exception When TypeOf ex Is ArgumentException OrElse TypeOf ex Is InvalidOperationException OrElse TypeOf ex Is OverflowException
                                              rendu.Text = ex.Message
                                          End Try
                                      End Sub
            AddHandler recu.TextChanged, Sub() actualiser()
            AddHandler devise.SelectedIndexChanged, Sub() actualiser()
            AddHandler moyen.SelectedIndexChanged, Sub() actualiser()
            AddHandler reference.TextChanged, Sub() actualiser()
            AddHandler externe.CheckedChanged, Sub() actualiser()
            AddHandler confirmer.Click, Sub()
                                             actualiser()
                                             If Not confirmer.Enabled Then Return
                                             Dim montant As Decimal
                                             If Not EncaissementAuditRegles.LireMontant(recu.Text, CultureInfo.CurrentCulture, montant) Then Return
                                             Reglement = New ReglementComptoir With {.Devise = CStr(devise.SelectedItem), .Moyen = CStr(moyen.SelectedItem), .MontantOrigine = montant, .Taux = If(CStr(devise.SelectedItem) = "FC", 1D, tauxUsd), .Reference = reference.Text.Trim(), .ConfirmationExterne = externe.Checked}
                                             RaiseEvent Confirmation(Me, EventArgs.Empty)
                                         End Sub
            AddHandler retour.Click, Sub() RaiseEvent Annulation(Me, EventArgs.Empty)
            Controls.Add(carte)
            actualiser()
        End Sub
    End Class
End Namespace
