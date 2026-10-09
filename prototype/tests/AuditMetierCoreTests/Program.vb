Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Collections.Generic
Imports DevCommerc8ak

Module Program
    Private compte As Integer
    Sub Main()
        Refuse(Of ArgumentException)(Sub() AuditMetierRegles.ValiderMotif(Nothing))
        Refuse(Of ArgumentException)(Sub() AuditMetierRegles.ValiderMotif("   " & vbTab))
        Refuse(Of ArgumentException)(Sub() AuditMetierRegles.ValiderMotif(New String("x"c, 1001)))
        Verifie(AuditMetierRegles.ValiderMotif(" Correction client ") = "Correction client", "Motif normalisé")
        Verifie(AuditMetierRegles.ValiderMotif(New String("x"c, 1000)).Length = 1000, "Limite du motif")
        AuditMetierRegles.VerifierVersion(New Byte() {1, 2}, New Byte() {1, 2})
        compte += 1
        Refuse(Of DBConcurrencyException)(Sub() AuditMetierRegles.VerifierVersion(New Byte() {1, 2}, New Byte() {1, 3}))
        Refuse(Of DBConcurrencyException)(Sub() AuditMetierRegles.VerifierVersion(Nothing, New Byte() {1, 3}))
        Dim avant As Object = Snapshot(2D, 112000D)
        Dim apres As Object = Snapshot(3D, 168000D)
        Dim differences As List(Of AuditDifference) = AuditDifferenceService.Comparer(avant, apres)
        Verifie(differences.Count = 2, "Seules quantité et total ont changé")
        Verifie(differences.Exists(Function(d) d.Champ = "Lignes[0].Quantite" AndAlso d.Avant = "2" AndAlso d.Apres = "3"), "Chemin de la quantité")
        Verifie(Not differences.Exists(Function(d) d.Champ.EndsWith("PrixUnitaire")), "Prix inchangé exclu")
        Verifie(AuditDifferenceService.Comparer(avant, avant).Count = 0, "Pas de fausse modification")
        Verifie(AuditDifferenceService.Comparer(Nothing, apres).Count > 0, "Création détaillée")
        Verifie(AuditDifferenceService.Comparer(avant, Nothing).Count > 0, "Suppression détaillée")
        Verifie(AuditMetierRegles.ClientEditionInchange(42, "CLIENT", "", "CLIENT", ""), "Client sans téléphone conservé")
        Verifie(Not AuditMetierRegles.ClientEditionInchange(Nothing, "CLIENT", "", "CLIENT", ""), "Pas d'ID client inventé")
        Verifie(Not AuditMetierRegles.ClientEditionInchange(42, "CLIENT", "", "AUTRE", ""), "Changement du nom détecté")
        Verifie(Not AuditMetierRegles.ClientEditionInchange(42, "CLIENT", "123", "CLIENT", "456"), "Changement téléphone détecté")
        AuditMetierRegles.VerifierEditionCompatible(0D, False)
        compte += 1
        Refuse(Of InvalidOperationException)(Sub() AuditMetierRegles.VerifierEditionCompatible(1D, False))
        Refuse(Of InvalidOperationException)(Sub() AuditMetierRegles.VerifierEditionCompatible(0D, True))
        Console.WriteLine("PASS : " & compte.ToString() & " contrôles métier réels, sans connexion SQL.")
    End Sub

    Private Function Snapshot(qte As Decimal, total As Decimal) As Object
        Return New Dictionary(Of String, Object) From {
            {"Lignes", New Object() {New Dictionary(Of String, Object) From {{"ProduitId", 12}, {"Quantite", qte}, {"PrixUnitaire", 56000D}}}},
            {"Total", total}}
    End Function
    Private Sub Refuse(Of T As Exception)(action As Action)
        Try
            action()
        Catch ex As Exception
            Verifie(TypeOf ex Is T, "Exception attendue : " & GetType(T).Name)
            Return
        End Try
        Throw New Exception("Refus attendu : " & GetType(T).Name)
    End Sub
    Private Sub Verifie(condition As Boolean, message As String)
        If Not condition Then Throw New Exception(message)
        compte += 1
    End Sub
End Module
