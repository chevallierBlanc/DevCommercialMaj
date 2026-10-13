Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public NotInheritable Class ModeTravailRegles
        Private Sub New()
        End Sub

        Public Shared Function Permission(mode As String) As String
            Select Case mode
                Case "FACTURATION" : Return "MODE_FACTURATION"
                Case "CAISSE" : Return "MODE_CAISSE"
                Case "FACTURATION_ET_CAISSE" : Return "MODE_COMBINE"
                Case Else : Throw New UnauthorizedAccessException("Mode de travail inconnu.")
            End Select
        End Function

        Public Shared Function AutoriseRole(mode As String, droits As ISet(Of String)) As Boolean
            Dim facturation As Boolean = droits.Contains("FACTURIER") AndAlso droits.Contains("FACTURE_CREER")
            Dim caisse As Boolean = droits.Contains("CAISSE") AndAlso droits.Contains("ENCAISSEMENT_CREER")
            ' Les droits metier historiques autorisent le parcours separe, sans
            ' creer de grants. Seul le parcours combine exige un droit additionnel.
            Select Case mode
                Case "FACTURATION" : Return facturation
                Case "CAISSE" : Return caisse
                Case "FACTURATION_ET_CAISSE" : Return facturation AndAlso caisse AndAlso droits.Contains("MODE_COMBINE")
                Case Else : Throw New UnauthorizedAccessException("Mode de travail inconnu.")
            End Select
        End Function

        Public Shared Function AutoriseEcran(mode As String, ecran As String) As Boolean
            ' Un mode limite un droit existant, il n'accorde jamais les droits
            ' de l'autre role. Le mode combine reste une option unique explicite.
            If String.IsNullOrEmpty(mode) Then Return True
            Permission(mode)
            If ecran = "FACTURIER" OrElse ecran = "HISTORIQUE_FACTURES" Then Return mode = "FACTURATION" OrElse mode = "FACTURATION_ET_CAISSE"
            If ecran = "CAISSE" OrElse ecran = "ANALYSE_CAISSE_PHYSIQUE" Then Return mode = "CAISSE" OrElse mode = "FACTURATION_ET_CAISSE"
            Return True
        End Function
    End Class
End Namespace
