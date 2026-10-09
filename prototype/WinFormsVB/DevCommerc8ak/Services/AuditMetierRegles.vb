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
    End Class
End Namespace
