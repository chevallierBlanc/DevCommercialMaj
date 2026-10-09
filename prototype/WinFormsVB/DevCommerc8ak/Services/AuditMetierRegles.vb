Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Linq

Namespace DevCommerc8ak
    Public NotInheritable Class AuditMetierRegles
        Private Sub New()
        End Sub

        Public Shared Function ValiderMotif(motif As String) As String
            If String.IsNullOrWhiteSpace(motif) OrElse motif.Trim().Length > 1000 Then
                Throw New ArgumentException("Un motif de 1 à 1000 caractères est obligatoire.")
            End If
            Return motif.Trim()
        End Function

        Public Shared Sub VerifierVersion(attendue As Byte(), actuelle As Byte())
            ' Une édition issue d'un ancien chargement ne peut pas écraser
            ' les modifications validées entre-temps par un autre poste.
            If attendue Is Nothing OrElse actuelle Is Nothing OrElse Not attendue.SequenceEqual(actuelle) Then
                Throw New DBConcurrencyException("Cette facture a été modifiée depuis son ouverture. Rechargez-la avant de recommencer.")
            End If
        End Sub

        Public Shared Function ClientEditionInchange(id As Integer?, nomInitial As String, telephoneInitial As String, nom As String, telephone As String) As Boolean
            ' Un client sans téléphone reste identifiable par son ID d'origine.
            ' Une édition sans changement ne doit pas créer un second compte client.
            Return id.HasValue AndAlso String.Equals(If(nomInitial, String.Empty).Trim(), If(nom, String.Empty).Trim(), StringComparison.Ordinal) AndAlso
                String.Equals(If(telephoneInitial, String.Empty), If(telephone, String.Empty), StringComparison.Ordinal)
        End Function

        Public Shared Sub VerifierEditionCompatible(taxe As Decimal, remiseParLigne As Boolean)
            ' L'écran actuel ne sait saisir que la remise globale et aucune taxe.
            ' Refuser l'édition évite de remettre silencieusement ces montants à zéro.
            If taxe <> 0D OrElse remiseParLigne Then Throw New InvalidOperationException("Cette facture contient une taxe ou une remise par article non prise en charge par cet écran d'édition. La consultation reste possible ; aucune donnée n'a été modifiée.")
        End Sub
    End Class
End Namespace
