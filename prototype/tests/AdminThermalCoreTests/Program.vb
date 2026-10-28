Imports System
Imports DevCommerc8ak

Module Program
    Private nombre As Integer
    Private Sub Verifier(ok As Boolean)
        If Not ok Then Throw New Exception("Echec du controle " & nombre.ToString())
        nombre += 1
    End Sub
    Sub Main()
        Verifier(ThermiqueMiseEnPage.LargeurUtile(315, 0, 315, 2) = 311)
        Verifier(ThermiqueMiseEnPage.LargeurUtile(315, 10, 290, 2) = 286)
        Verifier(ThermiqueMiseEnPage.LargeurUtile(315, 20, 400, 2) = 291)
        Verifier(ThermiqueMiseEnPage.LargeurUtile(315, -5, 315, 2) = 306)
        Dim refuse As Boolean = False
        Try
            ThermiqueMiseEnPage.LargeurUtile(80, 10, 20, 2)
        Catch ex As InvalidOperationException
            refuse = True
        End Try
        Verifier(refuse)
        Verifier(Not ThermiqueMiseEnPage.DoitReporter(50, 50, True))
        Verifier(ThermiqueMiseEnPage.DoitReporter(51, 50, False))
        refuse = False
        Try
            ThermiqueMiseEnPage.DoitReporter(51, 50, True)
        Catch ex As InvalidOperationException
            refuse = True
        End Try
        Verifier(refuse)
        Dim pages As Integer = 1, position As Integer = 0
        For index As Integer = 0 To 99
            If ThermiqueMiseEnPage.DoitReporter(50, 200 - position, position = 0) Then
                pages += 1
                position = 0
            End If
            position += 50
            Verifier(position <= 200)
        Next
        Verifier(pages = 25)
        nombre += ThermiqueMetierTests.Executer()
        Console.WriteLine("PASS : " & nombre.ToString() & " controles de geometrie, conditionnements et justificatifs, sans Windows ni SQL.")
    End Sub
End Module
