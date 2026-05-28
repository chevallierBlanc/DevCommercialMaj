Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.Text
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Module PdfHelper
        ' Genere un PDF tres simple avec lignes de texte.
        Public Sub GenererPdfSimple(chemin As String, titre As String, lignes As List(Of String))
            Using fs As New FileStream(chemin, FileMode.Create, FileAccess.Write)
                Using w As New StreamWriter(fs, Encoding.ASCII)
                    w.WriteLine("%PDF-1.4")
                    w.WriteLine("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj")
                    w.WriteLine("2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj")
                    w.WriteLine("3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >> endobj")

                    Dim sb As New StringBuilder()
                    sb.AppendLine("BT")
                    sb.AppendLine("/F1 12 Tf")
                    sb.AppendLine("50 800 Td")
                    sb.AppendLine("(" & Echaper(titre) & ") Tj")

                    Dim y As Integer = 780
                    For Each l As String In lignes
                        y -= 14
                        sb.AppendLine("50 " & y.ToString() & " Td")
                        sb.AppendLine("(" & Echaper(l) & ") Tj")
                    Next
                    sb.AppendLine("ET")

                    Dim content As String = sb.ToString()
                    w.WriteLine("4 0 obj << /Length " & content.Length.ToString() & " >> stream")
                    w.Write(content)
                    w.WriteLine("endstream endobj")
                    w.WriteLine("5 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj")
                    w.WriteLine("xref")
                    w.WriteLine("0 6")
                    w.WriteLine("0000000000 65535 f ")
                    w.WriteLine("trailer << /Size 6 /Root 1 0 R >>")
                    w.WriteLine("startxref")
                    w.WriteLine("0")
                    w.WriteLine("%%EOF")
                End Using
            End Using
        End Sub

        Private Function Echaper(s As String) As String
            Return s.Replace("\\", "\\\\").Replace("(", "\(").Replace(")", "\)")
        End Function
    End Module
End Namespace
