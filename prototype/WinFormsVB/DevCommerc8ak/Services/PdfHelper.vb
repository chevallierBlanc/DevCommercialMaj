Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Text

Namespace DevCommerc8ak
    Public Module PdfHelper
        Private Const PdfWidth As Integer = 595
        Private Const PdfHeight As Integer = 842
        Private Const MarginLeft As Integer = 36
        Private Const MarginTop As Integer = 36
        Private Const MarginBottom As Integer = 36
        Private Const LignesParPage As Integer = 42

        Public Sub GenererPdfSimple(chemin As String, titre As String, lignes As List(Of String))
            If String.IsNullOrWhiteSpace(chemin) Then Throw New ArgumentException("Le chemin PDF est vide.", NameOf(chemin))

            Dim contenu As Byte() = ConstruirePdf(titre, If(lignes, New List(Of String)()))
            File.WriteAllBytes(chemin, contenu)
        End Sub

        Private Function ConstruirePdf(titre As String, lignes As IList(Of String)) As Byte()
            Dim lignesNettoyees As New List(Of String)()

            For Each l As String In lignes
                lignesNettoyees.Add(NormaliserTexte(If(l, String.Empty)))
            Next

            If lignesNettoyees.Count = 0 Then
                lignesNettoyees.Add(String.Empty)
            End If

            Dim totalPages As Integer = Math.Max(1, CInt(Math.Ceiling(lignesNettoyees.Count / CDbl(LignesParPage))))
            Dim enc As Encoding = Encoding.ASCII

            Dim catalogId As Integer = 1
            Dim pagesId As Integer = 2
            Dim firstPageId As Integer = 3
            Dim fontId As Integer = 3 + (totalPages * 2)

            Dim pageObjectIds As New List(Of Integer)()
            Dim contentObjectIds As New List(Of Integer)()
            For i As Integer = 0 To totalPages - 1
                pageObjectIds.Add(firstPageId + (i * 2))
                contentObjectIds.Add(firstPageId + (i * 2) + 1)
            Next

            Dim objets(fontId)() As Byte

            Dim nl As String = Environment.NewLine
            Dim catalogObj As String = catalogId.ToString(CultureInfo.InvariantCulture) & " 0 obj" & nl &
                "<< /Type /Catalog /Pages 2 0 R >>" & nl &
                "endobj" & nl
            Dim kids As String = String.Join(" ", pageObjectIds.Select(Function(id) id.ToString(CultureInfo.InvariantCulture) & " 0 R"))
            Dim pagesObj As String = pagesId.ToString(CultureInfo.InvariantCulture) & " 0 obj" & nl &
                "<< /Type /Pages /Kids [" & kids & "] /Count " & totalPages.ToString(CultureInfo.InvariantCulture) &
                " /MediaBox [0 0 " & PdfWidth.ToString(CultureInfo.InvariantCulture) & " " & PdfHeight.ToString(CultureInfo.InvariantCulture) & "] >>" & nl &
                "endobj" & nl
            Dim fontObj As String = fontId.ToString(CultureInfo.InvariantCulture) & " 0 obj" & nl &
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>" & nl &
                "endobj" & nl

            objets(catalogId) = enc.GetBytes(catalogObj)
            objets(pagesId) = enc.GetBytes(pagesObj)

            For pageIndex As Integer = 0 To totalPages - 1
                Dim debut As Integer = pageIndex * LignesParPage
                Dim fin As Integer = Math.Min(debut + LignesParPage, lignesNettoyees.Count) - 1
                Dim lignesPage As New List(Of String)()
                For i As Integer = debut To fin
                    lignesPage.Add(lignesNettoyees(i))
                Next

                Dim content As String = ConstruireContenuPage(titre, lignesPage, pageIndex + 1, totalPages)
                Dim contentBytes As Byte() = enc.GetBytes(content)
                Dim contentObj As String = contentObjectIds(pageIndex).ToString(CultureInfo.InvariantCulture) & " 0 obj" & nl &
                    "<< /Length " & contentBytes.Length.ToString(CultureInfo.InvariantCulture) & " >>" & nl &
                    "stream" & nl &
                    content &
                    nl & "endstream" & nl &
                    "endobj" & nl
                objets(contentObjectIds(pageIndex)) = enc.GetBytes(contentObj)

                Dim pageObj As String = pageObjectIds(pageIndex).ToString(CultureInfo.InvariantCulture) & " 0 obj" & nl &
                    "<< /Type /Page /Parent 2 0 R /Resources << /Font << /F1 " & fontId.ToString(CultureInfo.InvariantCulture) & " 0 R >> >> " &
                    "/Contents " & contentObjectIds(pageIndex).ToString(CultureInfo.InvariantCulture) & " 0 R " &
                    "/MediaBox [0 0 " & PdfWidth.ToString(CultureInfo.InvariantCulture) & " " & PdfHeight.ToString(CultureInfo.InvariantCulture) & "] >>" & nl &
                    "endobj" & nl
                objets(pageObjectIds(pageIndex)) = enc.GetBytes(pageObj)
            Next

            objets(fontId) = enc.GetBytes(fontObj)

            Using ms As New MemoryStream()
                WriteAscii(ms, "%PDF-1.4" & nl)
                Dim offsets As New List(Of Long) From {0L}

                For i As Integer = 1 To objets.Length - 1
                    If objets(i) Is Nothing Then
                        Throw New InvalidOperationException("Objet PDF manquant: " & i.ToString(CultureInfo.InvariantCulture))
                    End If
                    offsets.Add(ms.Position)
                    ms.Write(objets(i), 0, objets(i).Length)
                Next

                Dim xrefStart As Long = ms.Position
                WriteAscii(ms, "xref" & nl)
                WriteAscii(ms, "0 " & objets.Length.ToString(CultureInfo.InvariantCulture) & nl)
                WriteAscii(ms, "0000000000 65535 f " & nl)
                For i As Integer = 1 To objets.Length - 1
                    WriteAscii(ms, offsets(i).ToString("0000000000", CultureInfo.InvariantCulture) & " 00000 n " & nl)
                Next
                WriteAscii(ms, "trailer" & nl)
                WriteAscii(ms, "<< /Size " & objets.Length.ToString(CultureInfo.InvariantCulture) & " /Root 1 0 R >>" & nl)
                WriteAscii(ms, "startxref" & nl)
                WriteAscii(ms, xrefStart.ToString(CultureInfo.InvariantCulture) & nl)
                WriteAscii(ms, "%%EOF")
                Return ms.ToArray()
            End Using
        End Function

        Private Function ConstruireContenuPage(titre As String, lignes As IList(Of String), numeroPage As Integer, totalPages As Integer) As String
            Dim sb As New StringBuilder()
            Dim y As Integer = PdfHeight - MarginTop

            sb.AppendLine("BT")
            sb.AppendLine("/F1 16 Tf")
            sb.AppendLine((MarginLeft).ToString(CultureInfo.InvariantCulture) & " " & y.ToString(CultureInfo.InvariantCulture) & " Td")
            sb.AppendLine("(" & EchaperTexte(NormaliserTexte(titre)) & ") Tj")
            sb.AppendLine("ET")

            y -= 24
            sb.AppendLine("BT")
            sb.AppendLine("/F1 9 Tf")
            sb.AppendLine((MarginLeft).ToString(CultureInfo.InvariantCulture) & " " & y.ToString(CultureInfo.InvariantCulture) & " Td")
            sb.AppendLine("(Page " & numeroPage.ToString(CultureInfo.InvariantCulture) & "/" & totalPages.ToString(CultureInfo.InvariantCulture) & ") Tj")
            sb.AppendLine("ET")

            y -= 22
            sb.AppendLine("BT")
            sb.AppendLine("/F1 10 Tf")

            For Each ligne As String In lignes
                If y < MarginBottom + 20 Then
                    Exit For
                End If
                sb.AppendLine(MarginLeft.ToString(CultureInfo.InvariantCulture) & " " & y.ToString(CultureInfo.InvariantCulture) & " Td")
                sb.AppendLine("(" & EchaperTexte(ligne) & ") Tj")
                y -= 14
            Next

            sb.AppendLine("ET")
            Return sb.ToString()
        End Function

        Private Sub WriteAscii(ms As MemoryStream, texte As String)
            Dim buffer As Byte() = Encoding.ASCII.GetBytes(texte)
            ms.Write(buffer, 0, buffer.Length)
        End Sub

        Private Function NormaliserTexte(texte As String) As String
            If String.IsNullOrEmpty(texte) Then Return String.Empty

            Dim normalise As String = texte.Replace(Convert.ToChar(13), " ").Replace(Convert.ToChar(10), " ").Trim()
            Dim sb As New StringBuilder(normalise.Length)
            For Each ch As Char In normalise.Normalize(NormalizationForm.FormD)
                Dim category As UnicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch)
                If category = UnicodeCategory.NonSpacingMark Then
                    Continue For
                End If
                Dim codePoint As Integer = Convert.ToInt32(ch)
                If codePoint < 32 Then
                    Continue For
                End If
                If codePoint > 126 Then
                    sb.Append("?")
                Else
                    sb.Append(ch)
                End If
            Next
            Return sb.ToString()
        End Function

        Private Function EchaperTexte(texte As String) As String
            Return texte.Replace("\\", "\\\\").Replace("(", "\(").Replace(")", "\)")
        End Function
    End Module
End Namespace
