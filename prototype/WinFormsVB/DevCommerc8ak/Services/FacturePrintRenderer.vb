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
    End Class

    Public NotInheritable Class FacturePrintRenderer
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
            Dim doc As New PrintDocument()
            Dim hauteur As Integer = CalculerHauteurThermique(data, param, 315)
            PrintConfigurationHelper.ConfigurerDocumentThermique(doc, owner, moduleName, actionName, 315, hauteur)
            AddHandler doc.PrintPage,
                Sub(sender As Object, e As PrintPageEventArgs)
                    DessinerThermique(e, data, param)
                End Sub
            Return doc
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

        Private Shared Function CalculerHauteurThermique(data As FacturePrintData, param As ParametreDTO, largeurPapier As Integer) As Integer
            Using bmp As New Bitmap(1, 1),
                  g As Graphics = Graphics.FromImage(bmp),
                  fontTitre As New Font("Segoe UI", 10, FontStyle.Bold),
                  fontSection As New Font("Segoe UI", 7.5F, FontStyle.Bold),
                  fontLigne As New Font("Segoe UI", 7.5F),
                  fontTotal As New Font("Segoe UI", 8.5F, FontStyle.Bold)

                Dim largeur As Integer = Math.Max(220, largeurPapier - 16)
                Dim hauteur As Integer = 8
                If LogoDisponible(param) Then hauteur += 48
                hauteur += HauteurTexte(g, If(param IsNot Nothing AndAlso param.NomMagasin <> "", param.NomMagasin, "COMMERCIAL PRO"), fontTitre, largeur)
                If param IsNot Nothing AndAlso param.AdresseMagasin <> "" Then hauteur += HauteurTexte(g, param.AdresseMagasin, fontLigne, largeur)
                If param IsNot Nothing AndAlso param.TelephoneMagasin <> "" Then hauteur += HauteurTexte(g, param.TelephoneMagasin, fontLigne, largeur)
                hauteur += HauteurTexte(g, TitreDocument(data), fontSection, largeur) + 10
                hauteur += 18 * 5 + 22
                For Each ligne As FacturePrintLine In If(data.Lignes, New List(Of FacturePrintLine)())
                    hauteur += HauteurTexte(g, Safe(ligne.Produit), fontSection, largeur)
                    hauteur += HauteurTexte(g, FormaterQuantite(ligne.Quantite) & " " & Safe(ligne.Conditionnement) & " x " & FormatMontant(ligne.PrixUnitaire), fontLigne, largeur - 4)
                    hauteur += 18
                Next
                hauteur += 12 + (20 * 3) + 12
                hauteur += HauteurTexte(g, TexteBasDocument(data), fontSection, largeur)
                hauteur += HauteurTexte(g, Application.ProductName & " - v" & PrintConfigurationHelper.ObtenirVersionApplication(), fontLigne, largeur)
                hauteur += HauteurTexte(g, "Développé par : Andy Ntanta", fontLigne, largeur)
                Return Math.Max(420, hauteur + 44)
            End Using
        End Function

        Private Shared Sub DessinerThermique(e As PrintPageEventArgs, data As FacturePrintData, param As ParametreDTO)
            e.Graphics.PageUnit = GraphicsUnit.Display
            Dim page As RectangleF = e.PageBounds
            Dim contentMargin As Single = 8.0F
            Dim contentLeft As Single = page.Left + contentMargin
            Dim contentWidth As Single = Math.Max(220.0F, page.Width - (2.0F * contentMargin))
            Dim y As Single = page.Top + 6.0F

            Using fontTitre As New Font("Segoe UI", 10, FontStyle.Bold),
                  fontSection As New Font("Segoe UI", 7.5F, FontStyle.Bold),
                  fontLigne As New Font("Segoe UI", 7.5F),
                  fontTotal As New Font("Segoe UI", 8.5F, FontStyle.Bold)

                Dim logoPath As String = If(param Is Nothing, String.Empty, LogoPathHelper.GetLogoPath(param))
                If Not String.IsNullOrWhiteSpace(logoPath) AndAlso File.Exists(logoPath) Then
                    Using img As Image = Image.FromFile(logoPath)
                        e.Graphics.DrawImage(img, contentLeft + ((contentWidth - 42) / 2.0F), y, 42, 42)
                    End Using
                    y += 46
                End If

                y = DessinerTexteCentreTicket(e.Graphics, If(param IsNot Nothing AndAlso param.NomMagasin <> "", param.NomMagasin, "COMMERCIAL PRO"), fontTitre, contentLeft, contentWidth, y)
                If param IsNot Nothing AndAlso param.AdresseMagasin <> "" Then y = DessinerTexteCentreTicket(e.Graphics, param.AdresseMagasin, fontLigne, contentLeft, contentWidth, y)
                If param IsNot Nothing AndAlso param.TelephoneMagasin <> "" Then y = DessinerTexteCentreTicket(e.Graphics, param.TelephoneMagasin, fontLigne, contentLeft, contentWidth, y)
                y = DessinerSeparateurTicket(e.Graphics, contentLeft, contentWidth, y)
                y = DessinerTexteCentreTicket(e.Graphics, TitreDocument(data), fontSection, contentLeft, contentWidth, y + 2)
                y = DessinerSeparateurTicket(e.Graphics, contentLeft, contentWidth, y)
                y = DessinerPaireTicket(e.Graphics, "N°", Safe(data.Numero), fontLigne, contentLeft, contentWidth, y)
                y = DessinerPaireTicket(e.Graphics, "Date", data.DateDocument.ToString("dd/MM/yyyy HH:mm"), fontLigne, contentLeft, contentWidth, y)
                y = DessinerPaireTicket(e.Graphics, "Client", If(String.IsNullOrWhiteSpace(data.Client), "CLIENT", data.Client), fontLigne, contentLeft, contentWidth, y)
                If Not String.IsNullOrWhiteSpace(data.Telephone) Then y = DessinerPaireTicket(e.Graphics, "Téléphone", data.Telephone, fontLigne, contentLeft, contentWidth, y)
                y = DessinerPaireTicket(e.Graphics, "Facturier", Safe(data.Facturier), fontLigne, contentLeft, contentWidth, y)
                y = DessinerSeparateurTicket(e.Graphics, contentLeft, contentWidth, y)
                y = DessinerTexteGaucheTicket(e.Graphics, "ARTICLE", fontSection, contentLeft, contentWidth, y)

                For Each ligne As FacturePrintLine In If(data.Lignes, New List(Of FacturePrintLine)())
                    y = DessinerTexteGaucheTicket(e.Graphics, Safe(ligne.Produit), fontSection, contentLeft, contentWidth, y + 2)
                    y = DessinerTexteGaucheTicket(e.Graphics, FormaterQuantite(ligne.Quantite) & " " & Safe(ligne.Conditionnement) & " x " & FormatMontant(ligne.PrixUnitaire), fontLigne, contentLeft + 4, contentWidth - 4, y)
                    y = DessinerTexteDroiteTicket(e.Graphics, FormatMontant(ligne.Montant), fontLigne, contentLeft, contentWidth, y)
                Next

                y = DessinerSeparateurTicket(e.Graphics, contentLeft, contentWidth, y)
                y = DessinerPaireTicket(e.Graphics, "Sous-total", FormatMontant(data.SousTotal), fontLigne, contentLeft, contentWidth, y)
                y = DessinerPaireTicket(e.Graphics, "Remise", FormatMontant(data.Remise), fontLigne, contentLeft, contentWidth, y)
                y = DessinerPaireTicket(e.Graphics, "TOTAL", FormatMontant(data.Total), fontTotal, contentLeft, contentWidth, y)
                y = DessinerSeparateurDoubleTicket(e.Graphics, contentLeft, contentWidth, y)
                y = DessinerTexteCentreTicket(e.Graphics, TexteBasDocument(data), fontSection, contentLeft, contentWidth, y)
                y = DessinerTexteCentreTicket(e.Graphics, Application.ProductName & " - v" & PrintConfigurationHelper.ObtenirVersionApplication(), fontLigne, contentLeft, contentWidth, y)
                y = DessinerTexteCentreTicket(e.Graphics, "Développé par : Andy Ntanta", fontLigne, contentLeft, contentWidth, y)
            End Using
        End Sub

        Private Shared Function DessinerTexteCentreTicket(g As Graphics, texte As String, font As Font, x As Single, largeur As Single, y As Single) As Single
            Dim size As SizeF = g.MeasureString(Safe(texte), font, New SizeF(largeur, 1000))
            Using fmtCentre As New StringFormat()
                fmtCentre.Alignment = StringAlignment.Center
                g.DrawString(Safe(texte), font, Brushes.Black, New RectangleF(x, y, largeur, size.Height), fmtCentre)
            End Using
            Return y + CSng(Math.Ceiling(size.Height)) + 2
        End Function

        Private Shared Function DessinerTexteGaucheTicket(g As Graphics, texte As String, font As Font, x As Single, largeur As Single, y As Single) As Single
            Dim size As SizeF = g.MeasureString(Safe(texte), font, New SizeF(largeur, 1000))
            g.DrawString(Safe(texte), font, Brushes.Black, New RectangleF(x, y, largeur, size.Height))
            Return y + CSng(Math.Ceiling(size.Height)) + 2
        End Function

        Private Shared Function DessinerTexteDroiteTicket(g As Graphics, texte As String, font As Font, x As Single, largeur As Single, y As Single) As Single
            Using fmtDroite As New StringFormat()
                fmtDroite.Alignment = StringAlignment.Far
                g.DrawString(Safe(texte), font, Brushes.Black, New RectangleF(x, y, largeur, 18), fmtDroite)
            End Using
            Return y + 18
        End Function

        Private Shared Function DessinerPaireTicket(g As Graphics, libelle As String, valeur As String, font As Font, x As Single, largeur As Single, y As Single) As Single
            Dim largeurLibelle As Single = Math.Min(82.0F, largeur * 0.38F)
            g.DrawString(libelle & " :", font, Brushes.Black, New RectangleF(x, y, largeurLibelle, 18))
            Using fmtDroite As New StringFormat()
                fmtDroite.Alignment = StringAlignment.Far
                g.DrawString(Safe(valeur), font, Brushes.Black, New RectangleF(x + largeurLibelle, y, largeur - largeurLibelle, 34), fmtDroite)
            End Using
            Return y + 18
        End Function

        Private Shared Function DessinerSeparateurTicket(g As Graphics, x As Single, largeur As Single, y As Single) As Single
            g.DrawLine(Pens.Black, x, y + 4, x + largeur - 1, y + 4)
            Return y + 10
        End Function

        Private Shared Function DessinerSeparateurDoubleTicket(g As Graphics, x As Single, largeur As Single, y As Single) As Single
            g.DrawLine(Pens.Black, x, y + 4, x + largeur - 1, y + 4)
            g.DrawLine(Pens.Black, x, y + 8, x + largeur - 1, y + 8)
            Return y + 14
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
