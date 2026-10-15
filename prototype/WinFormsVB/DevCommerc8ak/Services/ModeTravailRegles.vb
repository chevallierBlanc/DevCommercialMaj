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
        Public Shared Function ModeSeparePrefere(role As String, modes As IList(Of String)) As String
            ' Preference d'ergonomie seulement, jamais une attribution de droits.
            If (String.Equals(role, "CAISSIER", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(role, "CAISSIERE", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(role, "CAISSIÈRE", StringComparison.OrdinalIgnoreCase)) AndAlso modes.Contains("CAISSE") Then Return "CAISSE"
            If String.Equals(role, "FACTURIER", StringComparison.OrdinalIgnoreCase) AndAlso modes.Contains("FACTURATION") Then Return "FACTURATION"
            If modes.Count = 1 AndAlso modes(0) <> "FACTURATION_ET_CAISSE" Then Return modes(0)
            Return Nothing
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

        Public Shared Function Resoudre(gestion As Boolean, facturation As Boolean, caisse As Boolean, combine As Boolean, role As String, droits As ISet(Of String)) As List(Of String)
            Dim modes As New List(Of String)()
            Dim explicite As Boolean = droits.Contains("MODE_FACTURATION") OrElse droits.Contains("MODE_CAISSE") OrElse droits.Contains("MODE_COMBINE")
            Dim administratif As Boolean = String.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(role, "SUPERADMIN", StringComparison.OrdinalIgnoreCase) OrElse droits.Contains("ADMINISTRATION") OrElse droits.Contains("SUPERADMIN_TECH")
            ' Un administrateur conserve son portail classique sauf s'il choisit
            ' explicitement un parcours operationnel via ses privileges.
            If Not gestion OrElse (administratif AndAlso Not explicite) Then Return modes
            If Not explicite AndAlso Not droits.Contains("FACTURIER") AndAlso Not droits.Contains("CAISSE") Then Return modes
            If facturation AndAlso AutoriseRole("FACTURATION", droits) Then modes.Add("FACTURATION")
            If caisse AndAlso AutoriseRole("CAISSE", droits) Then modes.Add("CAISSE")
            If combine AndAlso AutoriseRole("FACTURATION_ET_CAISSE", droits) Then modes.Add("FACTURATION_ET_CAISSE")
            ' Recuperation administrative si la configuration operationnelle est
            ' incomplete : aucun droit metier n'est cree, aucun mode combine actif.
            If modes.Count = 0 AndAlso administratif Then Return modes
            If modes.Count = 0 Then Throw New UnauthorizedAccessException("Aucun mode opérationnel accessible. Activez le mode global adapté et vérifiez les droits du rôle choisi : Facturation + Créer une facture, ou Caisse + Créer un encaissement. Le combiné exige ces deux ensembles et Mode combiné.")
            Return modes
        End Function

        Public Shared Function DiagnosticCombine(droits As ISet(Of String)) As String
            Dim manquants As New List(Of String)()
            For Each droit As KeyValuePair(Of String, String) In New Dictionary(Of String, String) From {{"FACTURIER", "accès Facturation"}, {"FACTURE_CREER", "Créer une facture"}, {"CAISSE", "accès Caisse"}, {"ENCAISSEMENT_CREER", "Créer un encaissement"}, {"MODE_COMBINE", "Mode combiné"}}
                If Not droits.Contains(droit.Key) Then manquants.Add(droit.Value)
            Next
            If manquants.Count = 0 Then Return "Rôle éligible : l'activation globale ET l'attribution à ce compte pour ce rôle restent nécessaires."
            Return "Combiné : droits manquants pour ce rôle : " & String.Join(", ", manquants) & ". Les droits d'autres rôles ne sont pas cumulés."
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
