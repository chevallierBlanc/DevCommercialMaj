Option Strict On
Option Explicit On

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq

Namespace DevCommerc8ak
    Public Class AuditDifference
        Public Property Champ As String
        Public Property Avant As String
        Public Property Apres As String
    End Class

    Public NotInheritable Class AuditDifferenceService
        Private Sub New()
        End Sub

        Public Shared Function Comparer(avant As Object, apres As Object) As List(Of AuditDifference)
            ' Les chemins identifient les champs réels et l'ordre des articles.
            ' Une valeur identique n'est pas présentée comme une modification.
            Dim anciens As New SortedDictionary(Of String, String)(StringComparer.Ordinal)
            Dim nouveaux As New SortedDictionary(Of String, String)(StringComparer.Ordinal)
            Aplatir(avant, String.Empty, anciens)
            Aplatir(apres, String.Empty, nouveaux)
            Dim result As New List(Of AuditDifference)()
            For Each chemin As String In anciens.Keys.Union(nouveaux.Keys).OrderBy(Function(x) x, StringComparer.Ordinal)
                Dim a As String = Nothing
                Dim n As String = Nothing
                Dim existeAvant As Boolean = anciens.TryGetValue(chemin, a)
                Dim existeApres As Boolean = nouveaux.TryGetValue(chemin, n)
                If existeAvant <> existeApres OrElse Not String.Equals(a, n, StringComparison.Ordinal) Then
                    result.Add(New AuditDifference With {.Champ = chemin, .Avant = If(existeAvant, a, "Absent"), .Apres = If(existeApres, n, "Absent")})
                End If
            Next
            Return result
        End Function

        Public Shared Function Valeurs(snapshot As Object) As IDictionary(Of String, String)
            Dim result As New SortedDictionary(Of String, String)(StringComparer.Ordinal)
            Aplatir(snapshot, String.Empty, result)
            Return result
        End Function

        Private Shared Sub Aplatir(valeur As Object, chemin As String, result As IDictionary(Of String, String))
            Dim dictionnaire As IDictionary(Of String, Object) = TryCast(valeur, IDictionary(Of String, Object))
            If dictionnaire IsNot Nothing Then
                For Each item As KeyValuePair(Of String, Object) In dictionnaire
                    Aplatir(item.Value, If(chemin = String.Empty, item.Key, chemin & "." & item.Key), result)
                Next
                Return
            End If
            Dim liste As IEnumerable = TryCast(valeur, IEnumerable)
            If liste IsNot Nothing AndAlso Not TypeOf valeur Is String Then
                Dim index As Integer = 0
                For Each item As Object In liste
                    Aplatir(item, chemin & "[" & index.ToString(CultureInfo.InvariantCulture) & "]", result)
                    index += 1
                Next
                Return
            End If
            result(chemin) = If(valeur Is Nothing, "Non renseigné", Convert.ToString(valeur, CultureInfo.InvariantCulture))
        End Sub
    End Class
End Namespace
