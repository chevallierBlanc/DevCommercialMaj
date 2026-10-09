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
        EncaissementAuditRegles.Verifier(56000D, 0D, 60000D, 4000D)
        compte += 1
        Refuse(Of ArgumentException)(Sub() EncaissementAuditRegles.Verifier(56000D, 0D, 60000D, 0D))
        Refuse(Of ArgumentException)(Sub() EncaissementAuditRegles.Verifier(56000D, 0D, 55000D, 0D))
        Refuse(Of InvalidOperationException)(Sub() EncaissementAuditRegles.Verifier(56000D, 1000D, 60000D, 4000D))
        Dim paiement As Dictionary(Of String, Object) = EncaissementAuditRegles.Snapshot(56000D, 0D, 56000D, 60000D, 4000D, "ESPECES", "REF")
        Verifie(CDec(paiement("MontantAffecte")) = 56000D AndAlso CDec(paiement("ResteApres")) = 0D, "Monnaie non comptée comme recette")
        Verifie(EncaissementAuditRegles.Snapshot(10D, 0D, 10D, Nothing, Nothing, "", "")("MonnaieRendue") Is Nothing, "Historique inconnu non inventé")
        Dim noms As New Dictionary(Of Integer, String) From {{5, "NOM ACTUEL"}}
        Dim snapshotNoms As New Dictionary(Of String, String) From {{"EncaisseParNom", "NOM HISTORIQUE"}}
        Verifie(AuditPresentationService.AfficherValeur("EncaissePar", "5", snapshotNoms, noms).Contains("NOM HISTORIQUE"), "Instantané historique prioritaire")
        Verifie(AuditPresentationService.AfficherValeur("EncaissePar", "8", snapshotNoms, noms).Contains("ID 8"), "Identifiant historique conservé")
        Verifie(AuditPresentationService.Libelle("Entete.MontantTotal", snapshotNoms) = "Montant total", "Libellé financier")
        Verifie(AuditPresentationService.Libelle("Lignes[2].Quantite", snapshotNoms).Contains("ligne 3"), "Numérotation métier")
        snapshotNoms.Add("Lignes[2].Produit", "RIZ BB 25 KG")
        Verifie(AuditPresentationService.Libelle("Lignes[2].PrixUnitaire", snapshotNoms).Contains("RIZ BB 25 KG"), "Désignation snapshot")
        Verifie(ModeTravailRegles.AutoriseEcran("FACTURATION", "FACTURIER"), "Mode facturation")
        Verifie(Not ModeTravailRegles.AutoriseEcran("FACTURATION", "CAISSE"), "Caisse interdite au mode facturation")
        Verifie(Not ModeTravailRegles.AutoriseEcran("CAISSE", "FACTURIER"), "Facturation interdite au mode caisse")
        Verifie(ModeTravailRegles.AutoriseEcran("FACTURATION_ET_CAISSE", "FACTURIER") AndAlso ModeTravailRegles.AutoriseEcran("FACTURATION_ET_CAISSE", "CAISSE"), "Mode combiné unique")
        Refuse(Of UnauthorizedAccessException)(Sub() ModeTravailRegles.Permission("FACTURIER+CAISSIER"))
        Verifie(DelegationPrixRegles.NecessiteApprobation(100D, 120D, 60D, 10D, 0D), "Variation requiert approbation")
        Verifie(Not DelegationPrixRegles.NecessiteApprobation(100D, 105D, 60D, 10D, 0D), "Variation dans seuil")
        Verifie(DelegationPrixRegles.NecessiteApprobation(100D, 90D, 85D, 20D, 10D), "Marge minimale comparable")
        Refuse(Of ArgumentException)(Sub() DelegationPrixRegles.VerifierPrix(0D))
        Refuse(Of ArgumentException)(Sub() DelegationPrixRegles.VerifierPrix(1.001D))
        Refuse(Of InvalidOperationException)(Sub() DelegationPrixRegles.NecessiteApprobation(100D, 105D, 0D, 10D, 0D))
        Refuse(Of ArgumentException)(Sub() EncaissementAuditRegles.Verifier(100D, 0D, 100.0001D, 0.0001D))
        Verifie(CalculVenteService.CalculerCoutEquivalentCoefficient(250D, 5D, 25D) = 50D, "Coût du type à coefficient inchangé")
        Verifie(AuditPresentationService.AfficherValeur("MontantRecu", "60000", New Dictionary(Of String, String) From {{"DeviseMontants", "FC"}}, noms).EndsWith("FC"), "Devise explicite du montant reçu")
        DelegationPrixRegles.VerifierTotauxException(21D, 21D, 1D, 0D, 20D)
        compte += 1
        Refuse(Of ArgumentException)(Sub() DelegationPrixRegles.VerifierTotauxException(21D, 20D, 0D, 0D, 20D))
        Refuse(Of ArgumentException)(Sub() DelegationPrixRegles.VerifierTotauxException(21D, 21D, 0D, 0D, 20D))
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
