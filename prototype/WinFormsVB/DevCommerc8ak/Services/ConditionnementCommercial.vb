Option Strict On
Option Explicit On
Imports System

Namespace DevCommerc8ak
    Public NotInheritable Class ConditionnementCommercial
        Private Sub New()
        End Sub

        ' Les unites doivent appartenir a la ligne connue, jamais au catalogue
        ' actuel lorsqu'on reimprime une ancienne vente sans instantane fiable.
        Public Shared Function Libelle(typeVente As String, principale As String, secondaire As String, quantite As Decimal, Optional personnalise As Boolean = False) As String
            Dim type As String = If(typeVente, "").Trim()
            If personnalise AndAlso type.Length > 0 Then Return type
            If String.IsNullOrWhiteSpace(principale) AndAlso String.IsNullOrWhiteSpace(secondaire) Then Return If(type.Length = 0, "conditionnement non renseigné", type)
            Select Case type.ToUpperInvariant()
                Case "GROS"
                    If Not String.IsNullOrWhiteSpace(principale) Then Return Pluraliser(principale.ToLowerInvariant(), quantite)
                Case "DETAIL", "DÉTAIL", "PIECE", "PIÈCE", "UNITE", "UNITÉ"
                    If Not String.IsNullOrWhiteSpace(secondaire) Then Return Pluraliser(secondaire.ToLowerInvariant(), quantite)
                Case "DEMI"
                    If Not String.IsNullOrWhiteSpace(principale) Then Return Pluraliser("demi-" & principale.Trim().ToLowerInvariant(), quantite)
                Case "QUART"
                    If Not String.IsNullOrWhiteSpace(principale) Then Return Pluraliser("quart de " & principale.Trim().ToLowerInvariant(), quantite)
                Case "DOUZAINE"
                    Return Pluraliser("douzaine", quantite)
            End Select
            ' Un type personnalise ou historique reste fidele au texte conserve.
            Return If(type.Length = 0, "conditionnement non renseigné", type)
        End Function

        Private Shared Function Pluraliser(texte As String, quantite As Decimal) As String
            texte = texte.Trim()
            If Math.Abs(quantite - 1D) < 0.0001D OrElse texte.EndsWith("s", StringComparison.OrdinalIgnoreCase) OrElse texte.EndsWith("x", StringComparison.OrdinalIgnoreCase) Then Return texte
            Return texte & "s"
        End Function
    End Class
End Namespace
