Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.IO
Imports System.Linq
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FacturePrintLine
        Public Property Produit As String
        Public Property Conditionnement As String
        Public Property Quantite As Decimal
        Public Property PrixUnitaire As Decimal
        Public Property Montant As Decimal
    End Class

    Public Class FacturePrintData
        Public Property Numero As String
        Public Property DateDocument As Date
        Public Property Client As String
        Public Property Telephone As String
        Public Property Facturier As String
        Public Property Statut As String
        Public Property SousTotal As Decimal
        Public Property Remise As Decimal
        Public Property Total As Decimal
        Public Property Lignes As List(Of FacturePrintLine)
        ' Metadonnees exclusivement thermiques ; le rendu A4 n'en depend pas.
        Public Property ProformaThermique As Boolean
        Public Property DiagnosticThermique As Boolean
        Public Property TitreThermique As String
        Public Property MentionThermique As String
        Public Property LibelleTotalThermique As String
        Public Property MasquerRemiseThermique As Boolean
        Public Property InformationsThermiques As New List(Of KeyValuePair(Of String, String))()
        Public Property ReglementsThermiques As New List(Of KeyValuePair(Of String, String))()
    End Class

    Public NotInheritable Class FacturePrintRenderer
        ' Marge interne de 3 mm comme le ticket de caisse de reference,
        ' exprimee une seule fois en centiemes de pouce, sans facteur DPI.
        Private Const MargeInterneThermique As Single = 12.0F
        Private Sub New()
        End Sub

        Public Shared Function CreerDocumentA4(data As FacturePrintData, param As ParametreDTO) As PrintDocument
            Dim doc As New PrintDocument()
            doc.DefaultPageSettings.PaperSize = New PaperSize("A4", 827, 1169)
            doc.DefaultPageSettings.Margins = New Margins(30, 30, 30, 30)
            doc.DefaultPageSettings.Color = If(param IsNot Nothing, param.ImpressionCouleur, True)

            Dim index As Integer = 0
            AddHandler doc.PrintPage,
                Sub(sender As Object, e As PrintPageEventArgs)
                    DessinerA4(e, data, param, index)
                End Sub
            Return doc
        End Function

        Public Shared Function CreerDocumentThermique(data As FacturePrintData, param As ParametreDTO, owner As IWin32Window, moduleName As String, actionName As String) As PrintDocument
            If data Is Nothing OrElse data.Lignes Is Nothing OrElse data.Lignes.Count = 0 Then Throw New InvalidOperationException("Aucun article a imprimer.")
            Dim doc As New PrintDocument()
            Try
                param = PrintConfigurationHelper.ConfigurerDocumentThermique(doc, owner, moduleName, actionName, 315, 1200)
                doc.OriginAtMargins = False
                doc.DefaultPageSettings.Landscape = False
                doc.DefaultPageSettings.Margins = New Margins(2, 2, 2, 2)
                doc.DefaultPageSettings.Color = If(param IsNot Nothing, param.ImpressionCouleur, True)
                doc.PrinterSettings.Copies = 1S
                Dim blocs As List(Of BlocThermique) = ConstruireBlocsThermiques(data, param)
                Dim largeur As Single = ThermiqueMiseEnPage.LargeurUtile(315, doc.DefaultPageSettings.PrintableArea.Left, doc.DefaultPageSettings.PrintableArea.Width, MargeInterneThermique)
                Dim hauteur As Single = 0
                Using bmp As New Bitmap(1, 1)
                    bmp.SetResolution(100, 100)
                    Using g As Graphics = Graphics.FromImage(bmp)
                        For Each bloc As BlocThermique In blocs
                            hauteur += TraiterBlocThermique(g, bloc, 0, 0, largeur, False)
                        Next
                    End Using
                End Using
                doc.DefaultPageSettings.PaperSize = New PaperSize("Ticket80mm", 315, CInt(Math.Max(420, Math.Min(5000, Math.Ceiling(hauteur + 40)))))
                Dim index As Integer = 0
                Dim suite As String = If(data.DiagnosticThermique, "TICKET DE TEST", If(EstAnnulee(data), "FACTURE ANNULÉE", If(data.ProformaThermique, "PROFORMA", "FACTURE"))) & " " & Safe(data.Numero) & " (suite)"
                AddHandler doc.BeginPrint, Sub(s, e) index = 0
                AddHandler doc.PrintPage,
                    Sub(s, e)
                        DessinerPageThermique(e, blocs, index, suite)
                    End Sub
                Return doc
            Catch
                doc.Dispose()
                Throw
            End Try
        End Function

        Private Shared Sub DessinerA4(e As PrintPageEventArgs, data As FacturePrintData, param As ParametreDTO, ByRef index As Integer)
            e.Graphics.PageUnit = GraphicsUnit.Display
            Dim printable As RectangleF = e.MarginBounds
            Dim reportWidth As Single = printable.Width * 0.93F
            Dim reportLeft As Single = printable.Left + ((printable.Width - reportWidth) / 2.0F)
            Dim reportRight As Single = reportLeft + reportWidth
            Dim y As Single = printable.Top
            Dim footerHeight As Single = 24.0F

            Using fontTitle As New Font("Segoe UI", 16, FontStyle.Bold),
                  fontHeader As New Font("Segoe UI", 10, FontStyle.Bold),
                  fontNormal As New Font("Segoe UI", 9),
                  fontSmall As New Font("Segoe UI", 8),
                  blueBrush As New SolidBrush(Color.FromArgb(41, 128, 185)),
                  dangerBrush As New SolidBrush(Color.FromArgb(192, 57, 43)),
                  grayBrush As New SolidBrush(Color.FromArgb(107, 114, 128)),
                  headerBrush As New SolidBrush(Color.FromArgb(41, 128, 185)),
                  lightBrush As New SolidBrush(Color.FromArgb(239, 246, 255))

                y = DessinerEnteteEntrepriseA4(e.Graphics, param, reportLeft, y, reportWidth, fontTitle, fontNormal, blueBrush, grayBrush)

                Dim titre As String = TitreDocument(data)
                Dim titreBrush As Brush = If(EstAnnulee(data), dangerBrush, headerBrush)
                e.Graphics.FillRectangle(titreBrush, reportLeft, y, reportWidth, 34)
                DessinerTexteCentre(e.Graphics, titre, fontTitle, Brushes.White, New RectangleF(reportLeft, y + 4, reportWidth, 26))
                y += 46

                Dim infoHeight As Single = 86
                e.Graphics.FillRectangle(lightBrush, reportLeft, y, reportWidth, infoHeight)
                DessinerLibelleValeur(e.Graphics, "N°", Safe(data.Numero), fontHeader, fontNormal, reportLeft + 12, y + 10, reportWidth * 0.48F)
                DessinerLibelleValeur(e.Graphics, "Date", data.DateDocument.ToString("dd/MM/yyyy HH:mm"), fontHeader, fontNormal, reportLeft + 12, y + 34, reportWidth * 0.48F)
                DessinerLibelleValeur(e.Graphics, "Facturier", Safe(data.Facturier), fontHeader, fontNormal, reportLeft + 12, y + 58, reportWidth * 0.48F)
                DessinerLibelleValeur(e.Graphics, "Client", If(String.IsNullOrWhiteSpace(data.Client), "CLIENT", data.Client), fontHeader, fontNormal, reportLeft + reportWidth * 0.52F, y + 10, reportWidth * 0.46F)
                DessinerLibelleValeur(e.Graphics, "Téléphone", Safe(data.Telephone), fontHeader, fontNormal, reportLeft + reportWidth * 0.52F, y + 34, reportWidth * 0.46F)
                DessinerLibelleValeur(e.Graphics, "Statut", StatutAffichage(data.Statut), fontHeader, fontNormal, reportLeft + reportWidth * 0.52F, y + 58, reportWidth * 0.46F)
                y += infoHeight + 18

                Dim widths As Single() = {reportWidth * 0.36F, reportWidth * 0.18F, reportWidth * 0.12F, reportWidth * 0.17F, reportWidth * 0.17F}
                Dim headers As String() = {"Désignation", "Conditionnement", "Quantité", "Prix unitaire", "Montant"}
                y = DessinerEnteteTable(e.Graphics, reportLeft, y, widths, headers, fontHeader, headerBrush)

                Dim tableBottom As Single = printable.Bottom - footerHeight
                Dim lignes As List(Of FacturePrintLine) = If(data.Lignes, New List(Of FacturePrintLine)())
                While index < lignes.Count
                    Dim ligne As FacturePrintLine = lignes(index)
                    Dim rowHeight As Single = MesurerHauteurLigne(e.Graphics, ligne, widths, fontNormal)
                    If y + rowHeight > tableBottom Then
                        DessinerFooterA4(e.Graphics, reportLeft, reportWidth, printable.Bottom - 16, fontSmall)
                        e.HasMorePages = True
                        Return
                    End If
                    DessinerLigneA4(e.Graphics, reportLeft, y, widths, rowHeight, ligne, fontNormal)
                    y += rowHeight
                    index += 1
                End While

                y += 14
                Dim totalLeft As Single = reportRight - 230
                DessinerTotalA4(e.Graphics, "Sous-total", data.SousTotal, fontNormal, totalLeft, y, 230)
                y += 24
                DessinerTotalA4(e.Graphics, "Remise", data.Remise, fontNormal, totalLeft, y, 230)
                y += 26
                DessinerTotalA4(e.Graphics, "TOTAL", data.Total, fontHeader, totalLeft, y, 230)
                y += 34

                If EstProforma(data) Then
                    DessinerTexteCentre(e.Graphics, "DOCUMENT PROFORMA — NON ACQUITTÉ", fontHeader, blueBrush, New RectangleF(reportLeft, y, reportWidth, 24))
                ElseIf EstAnnulee(data) Then
                    DessinerTexteCentre(e.Graphics, "FACTURE ANNULÉE", fontHeader, dangerBrush, New RectangleF(reportLeft, y, reportWidth, 24))
                End If

                DessinerFooterA4(e.Graphics, reportLeft, reportWidth, printable.Bottom - 16, fontSmall)
                index = 0
                e.HasMorePages = False
            End Using
        End Sub

        Private Shared Function DessinerEnteteEntrepriseA4(g As Graphics, param As ParametreDTO, left As Single, y As Single, width As Single, fontTitle As Font, fontNormal As Font, blueBrush As Brush, grayBrush As Brush) As Single
            Dim xText As Single = left
            Dim logoPath As String = If(param Is Nothing, String.Empty, LogoPathHelper.GetLogoPath(param))
            If Not String.IsNullOrWhiteSpace(logoPath) AndAlso File.Exists(logoPath) Then
                Using img As Image = Image.FromFile(logoPath)
                    g.DrawImage(img, left, y, 58, 58)
                End Using
                xText += 70
            End If
            g.DrawString(If(param IsNot Nothing AndAlso param.NomMagasin <> "", param.NomMagasin, "COMMERCIAL PRO"), fontTitle, blueBrush, xText, y)
            g.DrawString(If(param Is Nothing, String.Empty, param.AdresseMagasin), fontNormal, grayBrush, xText, y + 26)
            g.DrawString(If(param Is Nothing, String.Empty, param.TelephoneMagasin), fontNormal, grayBrush, xText, y + 46)
            Return y + 72
        End Function

        Private Shared Function DessinerEnteteTable(g As Graphics, left As Single, y As Single, widths As Single(), headers As String(), font As Font, brush As Brush) As Single
            Dim x As Single = left
            For i As Integer = 0 To headers.Length - 1
                g.FillRectangle(brush, x, y, widths(i), 28)
                g.DrawString(headers(i), font, Brushes.White, New RectangleF(x + 4, y + 6, widths(i) - 8, 18))
                x += widths(i)
            Next
            Return y + 28
        End Function

        Private Shared Function MesurerHauteurLigne(g As Graphics, ligne As FacturePrintLine, widths As Single(), font As Font) As Single
            Dim valeurs As String() = {Safe(ligne.Produit), Safe(ligne.Conditionnement), FormaterQuantite(ligne.Quantite), FormatMontant(ligne.PrixUnitaire), FormatMontant(ligne.Montant)}
            Dim hauteur As Single = 28
            For i As Integer = 0 To valeurs.Length - 1
                Dim size As SizeF = g.MeasureString(valeurs(i), font, New SizeF(widths(i) - 8, 500))
                hauteur = Math.Max(hauteur, CSng(Math.Ceiling(size.Height)) + 10)
            Next
            Return hauteur
        End Function

        Private Shared Sub DessinerLigneA4(g As Graphics, left As Single, y As Single, widths As Single(), height As Single, ligne As FacturePrintLine, font As Font)
            Dim valeurs As String() = {Safe(ligne.Produit), Safe(ligne.Conditionnement), FormaterQuantite(ligne.Quantite), FormatMontant(ligne.PrixUnitaire), FormatMontant(ligne.Montant)}
            Dim x As Single = left
            For i As Integer = 0 To valeurs.Length - 1
                g.DrawRectangle(Pens.LightGray, x, y, widths(i), height)
                Using fmt As New StringFormat()
                    fmt.Alignment = If(i >= 2, StringAlignment.Far, StringAlignment.Near)
                    fmt.LineAlignment = StringAlignment.Center
                    g.DrawString(valeurs(i), font, Brushes.Black, New RectangleF(x + 4, y + 3, widths(i) - 8, height - 6), fmt)
                End Using
                x += widths(i)
            Next
        End Sub

        Private Class BlocThermique
            Public Property Genre As String
            Public Property Texte As String
            Public Property Valeur As String
            Public Property Detail As String
            Public Property Police As Integer = 2
        End Class

        Private Shared Function ConstruireBlocsThermiques(data As FacturePrintData, param As ParametreDTO) As List(Of BlocThermique)
            Dim blocs As New List(Of BlocThermique)()
            Dim texte As Action(Of String, Integer) = Sub(t, f)
                                                         If Not String.IsNullOrWhiteSpace(t) Then blocs.Add(New BlocThermique With {.Genre = "CENTRE", .Texte = t, .Police = f})
                                                     End Sub
            Dim paire As Action(Of String, String) = Sub(t, v) blocs.Add(New BlocThermique With {.Genre = "PAIRE", .Texte = t, .Valeur = v})
            Dim separer As Action = Sub() blocs.Add(New BlocThermique With {.Genre = "SEPARATEUR"})
            If LogoDisponible(param) Then blocs.Add(New BlocThermique With {.Genre = "LOGO", .Texte = LogoPathHelper.GetLogoPath(param)})
            texte(If(param Is Nothing OrElse String.IsNullOrWhiteSpace(param.NomMagasin), "MAGASIN", param.NomMagasin), 0)
            If param IsNot Nothing Then
                texte(param.AdresseMagasin, 2)
                texte(param.TelephoneMagasin, 2)
            End If
            Dim titre As String = If(EstAnnulee(data), "FACTURE ANNULÉE", If(data.ProformaThermique, "PROFORMA", If(String.IsNullOrWhiteSpace(data.TitreThermique), "FACTURE", data.TitreThermique)))
            texte(titre, 1)
            separer()
            paire(If(data.DiagnosticThermique, "Diagnostic", "Facture"), Safe(data.Numero))
            paire("Date", data.DateDocument.ToString("dd/MM/yyyy HH:mm"))
            If Not String.IsNullOrWhiteSpace(data.Facturier) Then paire("Facturier", data.Facturier)
            If Not String.IsNullOrWhiteSpace(data.Client) Then paire("Client", data.Client)
            If Not String.IsNullOrWhiteSpace(data.Telephone) Then paire("Téléphone", data.Telephone)
            If Not data.ProformaThermique AndAlso Not String.IsNullOrWhiteSpace(data.Statut) Then paire("Statut", StatutAffichage(data.Statut))
            For Each info As KeyValuePair(Of String, String) In data.InformationsThermiques
                paire(info.Key, info.Value)
            Next
            separer()
            For Each ligne As FacturePrintLine In data.Lignes
                blocs.Add(New BlocThermique With {
                    .Genre = "ARTICLE", .Texte = Safe(ligne.Produit),
                    .Detail = FormaterQuantite(ligne.Quantite) & " " & Safe(ligne.Conditionnement) & " (" & MontantThermique(ligne.PrixUnitaire) & ")",
                    .Valeur = MontantThermique(ligne.Montant)})
            Next
            separer()
            paire("Sous-total", MontantThermique(data.SousTotal))
            If Not data.MasquerRemiseThermique Then paire("Remise", MontantThermique(data.Remise))
            blocs.Add(New BlocThermique With {.Genre = "PAIRE", .Texte = If(String.IsNullOrWhiteSpace(data.LibelleTotalThermique), "TOTAL", data.LibelleTotalThermique), .Valeur = MontantThermique(data.Total), .Police = 3})
            ' Ni proforma ni diagnostic ne prouvent un paiement, meme si un
            ' appelant fournissait par erreur des metadonnees de reglement.
            If Not data.ProformaThermique AndAlso Not data.DiagnosticThermique Then
                For Each reglement As KeyValuePair(Of String, String) In data.ReglementsThermiques
                    paire(reglement.Key, reglement.Value)
                Next
            End If
            separer()
            texte(If(EstAnnulee(data), "FACTURE ANNULÉE", If(data.ProformaThermique, "DOCUMENT PROFORMA - NON ACQUITTÉ", If(String.IsNullOrWhiteSpace(data.MentionThermique), "DOCUMENT FACTURE", data.MentionThermique))), 1)
            If Not data.ProformaThermique AndAlso Not data.DiagnosticThermique AndAlso Not EstAnnulee(data) Then texte("Merci pour votre confiance", 1)
            texte(Application.ProductName & " - v" & PrintConfigurationHelper.ObtenirVersionApplication(), 2)
            texte("Développé par : Andy NTANTA", 2)
            texte("Imprimé le " & Date.Now.ToString("dd/MM/yyyy HH:mm"), 2)
            Return blocs
        End Function

        Private Shared Sub DessinerPageThermique(e As PrintPageEventArgs, blocs As List(Of BlocThermique), ByRef index As Integer, suite As String)
            e.Graphics.PageUnit = GraphicsUnit.Display
            Dim zone As RectangleF = e.PageSettings.PrintableArea
            Dim largeur As Single = ThermiqueMiseEnPage.LargeurUtile(e.PageBounds.Width, zone.Left, zone.Width, MargeInterneThermique)
            ' OriginAtMargins=False : origine du Graphics au coin imprimable.
            ' Les metadonnees du pilote sont en coordonnees papier ; soustraire
            ' les hard margins une seule fois, pour l'apercu comme pour l'impression.
            Dim x As Single = Math.Max(0, zone.Left) + MargeInterneThermique - e.PageSettings.HardMarginX
            Dim y As Single = Math.Max(0, zone.Top) + 2 - e.PageSettings.HardMarginY
            Dim bas As Single = Math.Min(e.PageBounds.Bottom, zone.Bottom) - 2 - e.PageSettings.HardMarginY
            Dim debut As Integer = index
            If index > 0 Then
                Dim rappel As New BlocThermique With {.Genre = "CENTRE", .Texte = suite, .Police = 1}
                y += TraiterBlocThermique(e.Graphics, rappel, x, y, largeur, True)
            End If
            While index < blocs.Count
                Dim hauteur As Single = TraiterBlocThermique(e.Graphics, blocs(index), x, y, largeur, False)
                If ThermiqueMiseEnPage.DoitReporter(hauteur, bas - y, index = debut) Then
                    e.HasMorePages = True
                    Return
                End If
                TraiterBlocThermique(e.Graphics, blocs(index), x, y, largeur, True)
                y += hauteur
                index += 1
            End While
            e.HasMorePages = False
        End Sub

        Private Shared Function TraiterBlocThermique(g As Graphics, bloc As BlocThermique, x As Single, y As Single, largeur As Single, dessiner As Boolean) As Single
            If bloc.Genre = "LOGO" Then
                If dessiner AndAlso File.Exists(bloc.Texte) Then
                    Using image As Image = Image.FromFile(bloc.Texte)
                        Dim largeurLogo As Single = Math.Min(90, 50.0F * image.Width / image.Height)
                        g.DrawImage(image, x + (largeur - largeurLogo) / 2, y, largeurLogo, 50)
                    End Using
                End If
                Return 54
            End If
            Dim taille As Single = If(bloc.Police = 0, 10.0F, If(bloc.Police = 3, 8.5F, 7.5F))
            Using font As New Font("Segoe UI", taille, If(bloc.Police = 2, FontStyle.Regular, FontStyle.Bold)),
                  normal As New Font("Segoe UI", 7.5F),
                  article As New Font("Segoe UI", 7.5F, FontStyle.Bold)
                If bloc.Genre = "SEPARATEUR" Then
                    If dessiner Then g.DrawLine(Pens.Black, x, y + 3, x + largeur, y + 3)
                    Return CSng(Math.Ceiling(g.MeasureString("Ag", normal).Height)) + 2
                End If
                If bloc.Genre = "PAIRE" Then
                    Dim labelWidth As Single = Math.Min(82, largeur * 0.38F)
                    Dim hauteur As Single = Math.Max(MesurerTexteThermique(g, bloc.Texte & " :", font, labelWidth), MesurerTexteThermique(g, bloc.Valeur, font, largeur - labelWidth)) + 3
                    If dessiner Then
                        EcrireTexteThermique(g, bloc.Texte & " :", font, x, y, labelWidth, hauteur, StringAlignment.Near)
                        EcrireTexteThermique(g, bloc.Valeur, font, x + labelWidth, y, largeur - labelWidth, hauteur, StringAlignment.Far)
                    End If
                    Return hauteur
                End If
                If bloc.Genre = "ARTICLE" Then
                    Dim nomHeight As Single = MesurerTexteThermique(g, bloc.Texte, article, largeur) + 2
                    Dim montantSouhaite As Single = g.MeasureString(Safe(bloc.Valeur), normal).Width + 8
                    If montantSouhaite > largeur * 0.45F Then
                        ' Un grand montant garde toute la largeur sur sa propre
                        ' ligne, plutot qu'une colonne trop etroite ou une police minuscule.
                        Dim detailLong As Single = MesurerTexteThermique(g, bloc.Detail, normal, largeur - 4) + 3
                        Dim montantLong As Single = MesurerTexteThermique(g, bloc.Valeur, normal, largeur) + 3
                        If dessiner Then
                            EcrireTexteThermique(g, bloc.Texte, article, x, y, largeur, nomHeight, StringAlignment.Near)
                            EcrireTexteThermique(g, bloc.Detail, normal, x + 4, y + nomHeight, largeur - 4, detailLong, StringAlignment.Near)
                            EcrireTexteThermique(g, bloc.Valeur, normal, x, y + nomHeight + detailLong, largeur, montantLong, StringAlignment.Far)
                        End If
                        Return nomHeight + detailLong + montantLong + 2
                    End If
                    Dim montantWidth As Single = Math.Min(largeur * 0.45F, Math.Max(70, montantSouhaite))
                    Dim detailWidth As Single = largeur - montantWidth - 6
                    Dim detailHeight As Single = Math.Max(MesurerTexteThermique(g, bloc.Detail, normal, detailWidth - 4), MesurerTexteThermique(g, bloc.Valeur, normal, montantWidth)) + 3
                    If dessiner Then
                        EcrireTexteThermique(g, bloc.Texte, article, x, y, largeur, nomHeight, StringAlignment.Near)
                        EcrireTexteThermique(g, bloc.Detail, normal, x + 4, y + nomHeight, detailWidth - 4, detailHeight, StringAlignment.Near)
                        EcrireTexteThermique(g, bloc.Valeur, normal, x + largeur - montantWidth, y + nomHeight, montantWidth, detailHeight, StringAlignment.Far)
                    End If
                    Return nomHeight + detailHeight + 2
                End If
                Dim textHeight As Single = MesurerTexteThermique(g, bloc.Texte, font, largeur) + 2
                If dessiner Then EcrireTexteThermique(g, bloc.Texte, font, x, y, largeur, textHeight, StringAlignment.Center)
                Return textHeight
            End Using
        End Function

        Private Shared Function MesurerTexteThermique(g As Graphics, texte As String, font As Font, largeur As Single) As Single
            Return CSng(Math.Ceiling(g.MeasureString(Safe(texte), font, New SizeF(largeur, 100000)).Height))
        End Function

        Private Shared Sub EcrireTexteThermique(g As Graphics, texte As String, font As Font, x As Single, y As Single, largeur As Single, hauteur As Single, alignement As StringAlignment)
            Using format As New StringFormat() With {.Alignment = alignement}
                g.DrawString(Safe(texte), font, Brushes.Black, New RectangleF(x, y, largeur, hauteur), format)
            End Using
        End Sub

        Private Shared Function MontantThermique(valeur As Decimal) As String
            Return valeur.ToString("#,##0", Globalization.CultureInfo.GetCultureInfo("fr-FR")) & " FC"
        End Function

        Private Shared Sub DessinerTexteCentre(g As Graphics, texte As String, font As Font, brush As Brush, rect As RectangleF)
            Using fmt As New StringFormat()
                fmt.Alignment = StringAlignment.Center
                fmt.LineAlignment = StringAlignment.Center
                g.DrawString(Safe(texte), font, brush, rect, fmt)
            End Using
        End Sub

        Private Shared Sub DessinerLibelleValeur(g As Graphics, libelle As String, valeur As String, fontLibelle As Font, fontValeur As Font, x As Single, y As Single, width As Single)
            g.DrawString(libelle & " :", fontLibelle, Brushes.Black, New RectangleF(x, y, 85, 20))
            g.DrawString(Safe(valeur), fontValeur, Brushes.Black, New RectangleF(x + 90, y, width - 90, 22))
        End Sub

        Private Shared Sub DessinerTotalA4(g As Graphics, libelle As String, montant As Decimal, font As Font, x As Single, y As Single, width As Single)
            g.DrawString(libelle & " :", font, Brushes.Black, New RectangleF(x, y, width * 0.45F, 22))
            Using fmt As New StringFormat()
                fmt.Alignment = StringAlignment.Far
                g.DrawString(FormatMontant(montant), font, Brushes.Black, New RectangleF(x + width * 0.45F, y, width * 0.55F, 22), fmt)
            End Using
        End Sub

        Private Shared Sub DessinerFooterA4(g As Graphics, left As Single, width As Single, y As Single, font As Font)
            Dim texte As String = "Impression professionnelle générée depuis COMMERCIAL PRO - " & Date.Now.ToString("dd/MM/yyyy HH:mm")
            Using fmtCentre As New StringFormat()
                fmtCentre.Alignment = StringAlignment.Center
                g.DrawString(texte, font, Brushes.Gray, New RectangleF(left, y, width, 18), fmtCentre)
            End Using
        End Sub

        Private Shared Function HauteurTexte(g As Graphics, texte As String, font As Font, largeur As Integer) As Integer
            Dim size As SizeF = g.MeasureString(Safe(texte), font, New SizeF(largeur, 1000))
            Return CInt(Math.Ceiling(size.Height)) + 2
        End Function

        Private Shared Function LogoDisponible(param As ParametreDTO) As Boolean
            Dim logoPath As String = If(param Is Nothing, String.Empty, LogoPathHelper.GetLogoPath(param))
            Return Not String.IsNullOrWhiteSpace(logoPath) AndAlso File.Exists(logoPath)
        End Function

        Private Shared Function TitreDocument(data As FacturePrintData) As String
            If EstAnnulee(data) Then Return "FACTURE ANNULÉE"
            If EstProforma(data) Then Return "PROFORMA"
            Return "FACTURE"
        End Function

        Private Shared Function TexteBasDocument(data As FacturePrintData) As String
            If EstAnnulee(data) Then Return "FACTURE ANNULÉE"
            If EstProforma(data) Then Return "DOCUMENT PROFORMA - NON ACQUITTÉ"
            Return "DOCUMENT FACTURE"
        End Function

        Private Shared Function EstProforma(data As FacturePrintData) As Boolean
            Return data IsNot Nothing AndAlso String.Equals(Safe(data.Statut), "EN_ATTENTE", StringComparison.OrdinalIgnoreCase)
        End Function

        Private Shared Function EstAnnulee(data As FacturePrintData) As Boolean
            Return data IsNot Nothing AndAlso String.Equals(Safe(data.Statut), "ANNULEE", StringComparison.OrdinalIgnoreCase)
        End Function

        Public Shared Function StatutAffichage(statut As String) As String
            Select Case Safe(statut).Trim().ToUpperInvariant()
                Case "EN_ATTENTE"
                    Return "BROUILLON"
                Case "PAYEE"
                    Return "PAYÉE"
                Case "ANNULEE"
                    Return "ANNULÉE"
                Case Else
                    Return Safe(statut)
            End Select
        End Function

        Public Shared Function FormatMontant(montant As Decimal) As String
            Return montant.ToString("N0") & " FC"
        End Function

        Public Shared Function FormaterQuantite(qte As Decimal) As String
            If Decimal.Truncate(qte) = qte Then Return qte.ToString("N0")
            Return qte.ToString("0.####")
        End Function

        Private Shared Function Safe(value As String) As String
            Return If(value, String.Empty)
        End Function
    End Class
End Namespace
