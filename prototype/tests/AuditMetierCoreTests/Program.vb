Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Collections.Generic
Imports DevCommerc8ak

Module Program
    Private compte As Integer
    Sub Main()
        TesterCoutsEtReseau()
        Dim standards As List(Of String) = TarifCatalogueRegles.Standards(100D, 50D, 10D, 0D, 0D, 0D, True, False, True, False)
        Verifie(standards.Count = 2 AndAlso standards.Contains("gros") AndAlso standards.Contains("piece"), "Gros et detail seulement : pas de types fictifs")
        Verifie(TarifCatalogueRegles.Standards(100D, 50D, 10D, 25D, 120D, 8D, True, True, True, True).Count = 7, "Standards et aliases existants preserves")
        Verifie(TarifCatalogueRegles.Standards(100D, 50D, 10D, 0D, 0D, 0D, False, False, False, False).Count = 0, "Prix positifs sans autorisation de vente masques")
        Verifie(TarifCatalogueRegles.Marge(150D, 100D).Value = 50D AndAlso TarifCatalogueRegles.Coefficient(150D, 100D).Value = 1.5D, "Marge sur cout et coefficient comparables")
        Verifie(TarifCatalogueRegles.Marge(10D, 0D) Is Nothing, "Cout absent : pas de marge inventee")
        Dim ancienProduit As New DataTable
        ancienProduit.Columns.Add("PrixDetail", GetType(Decimal)) : ancienProduit.Columns.Add("VenteDetail", GetType(Boolean))
        ancienProduit.Rows.Add(DBNull.Value, DBNull.Value)
        Verifie(TarifCatalogueRegles.StandardsDepuisProduit(ancienProduit.Rows(0)).Count = 0, "Anciennes colonnes nulles ou absentes sans exception ni type invente")
        Verifie(ClientAnalyseRegles.DebutPeriode(2024, 2) = New DateTime(2024, 2, 1) AndAlso ClientAnalyseRegles.FinPeriode(2024, 2) = New DateTime(2024, 3, 1), "Mois bissextile borne exclusive")
        Verifie(ClientAnalyseRegles.FinPeriode(2026, 0) = New DateTime(2027, 1, 1), "Annee entiere sans perte du dernier jour")
        Refuse(Of ArgumentException)(Sub() ClientAnalyseRegles.DebutPeriode(2026, 13))
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
        Verifie(EncaissementAuditRegles.ConvertirRecuFc(24D, "USD", 2500D) = 60000D, "Recu USD au taux officiel")
        Verifie(EncaissementAuditRegles.ConvertirRecuFc(60000D, "FC", 0D) = 60000D, "FC sans inventer un taux USD")
        Verifie(EncaissementAuditRegles.ConvertirRecuFc(1.005D, "FC", 0D) = 1D, "Precision paiement deux decimales ToEven")
        Refuse(Of InvalidOperationException)(Sub() EncaissementAuditRegles.ConvertirRecuFc(1D, "USD", 0D))
        Refuse(Of ArgumentException)(Sub() EncaissementAuditRegles.ConvertirRecuFc(-1D, "FC", 1D))
        Refuse(Of ArgumentException)(Sub() EncaissementAuditRegles.ConvertirRecuFc(1D, "EUR", 1D))
        Dim montantLocal As Decimal
        Verifie(EncaissementAuditRegles.LireMontant("60000,50", Globalization.CultureInfo.GetCultureInfo("fr-FR"), montantLocal) AndAlso montantLocal = 60000.5D, "Separateur decimal local francais")
        Verifie(Not EncaissementAuditRegles.LireMontant("", Globalization.CultureInfo.InvariantCulture, montantLocal), "Saisie vide non confirmee")
        Verifie(Not EncaissementAuditRegles.LireMontant("abc", Globalization.CultureInfo.InvariantCulture, montantLocal), "Saisie invalide non confirmee")
        Verifie(Not EncaissementAuditRegles.LireMontant("0.000000001", Globalization.CultureInfo.InvariantCulture, montantLocal), "Precision origine non tronquee silencieusement")
        Refuse(Of ArgumentException)(Sub() EncaissementAuditRegles.ConvertirRecuFc(0.000000001D, "FC", 1D))
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
        Verifie(ModeTravailRegles.ModeSeparePrefere("CAISSIER", New List(Of String) From {"FACTURATION", "CAISSE"}) = "CAISSE", "Case decochee : parcours caisse habituel")
        Verifie(ModeTravailRegles.ModeSeparePrefere("CAISSIÈRE", New List(Of String) From {"CAISSE", "FACTURATION_ET_CAISSE"}) = "CAISSE", "Tatiana/CAISSIERE accentuee : case decochee reste en caisse")
        Verifie(ModeTravailRegles.ModeSeparePrefere("CAISSIÈRE", New List(Of String) From {"FACTURATION_ET_CAISSE"}) Is Nothing, "Aucun parcours classique fabrique si seul le combine est autorise")
        Verifie(ModeTravailRegles.ModeSeparePrefere("FACTURIER", New List(Of String) From {"FACTURATION", "CAISSE"}) = "FACTURATION", "Parcours facturier habituel sans cumul")
        Verifie(ModeTravailRegles.ModeSeparePrefere("CAISSIER", New List(Of String)()) Is Nothing, "Preference ne fabrique jamais un mode interdit")
        Dim droitsCaisse As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {"CAISSE", "ENCAISSEMENT_CREER"}
        Verifie(ModeTravailRegles.AutoriseRole("CAISSE", droitsCaisse), "Caissiere historique sans nouveau grant MODE_CAISSE")
        Verifie(Not ModeTravailRegles.AutoriseRole("FACTURATION", droitsCaisse), "Pas de facturation depuis les seuls droits caisse")
        Verifie(Not ModeTravailRegles.AutoriseRole("FACTURATION_ET_CAISSE", droitsCaisse), "Pas de combine depuis les seuls droits caisse")
        droitsCaisse.Remove("ENCAISSEMENT_CREER")
        Verifie(Not ModeTravailRegles.AutoriseRole("CAISSE", droitsCaisse), "Ecran sans droit encaissement insuffisant")
        droitsCaisse.Add("MODE_COMBINE")
        Verifie(Not ModeTravailRegles.AutoriseRole("FACTURATION_ET_CAISSE", droitsCaisse), "Mode combine ne fournit aucun droit metier")
        Dim droitsFacture As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {"FACTURIER", "FACTURE_CREER"}
        Verifie(ModeTravailRegles.AutoriseRole("FACTURATION", droitsFacture), "Facturier historique sans nouveau grant MODE_FACTURATION")
        droitsFacture.Add("CAISSE") : droitsFacture.Add("ENCAISSEMENT_CREER")
        Verifie(Not ModeTravailRegles.AutoriseRole("FACTURATION_ET_CAISSE", droitsFacture), "Deux parcours ne concedent pas le combine")
        droitsFacture.Add("MODE_COMBINE")
        Verifie(ModeTravailRegles.AutoriseRole("FACTURATION_ET_CAISSE", droitsFacture), "Combine explicitement autorise au role actif")
        droitsFacture.Remove("FACTURE_CREER")
        Verifie(Not ModeTravailRegles.AutoriseRole("FACTURATION_ET_CAISSE", droitsFacture), "Retrait facturation invalide immediatement le combine")
        Dim seulFacturier As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {"FACTURIER", "FACTURE_CREER"}
        Dim seuleCaisse As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {"CAISSE", "ENCAISSEMENT_CREER"}
        Verifie(ModeTravailRegles.Resoudre(False, False, False, False, "FACTURIER", seulFacturier).Count = 0, "Gestion inactive conserve le parcours historique")
        Verifie(ModeTravailRegles.Resoudre(True, True, True, True, "FACTURIER", seulFacturier).Count = 1, "Facturier voit un seul mode")
        Verifie(ModeTravailRegles.Resoudre(True, True, True, True, "CAISSIERE", seuleCaisse)(0) = "CAISSE", "Caissiere conserve son identite et son mode")
        Verifie(ModeTravailRegles.Resoudre(True, True, True, True, "CAISSIER", seuleCaisse)(0) = "CAISSE", "Caissier fonctionne sans fusion de roles")
        Refuse(Of UnauthorizedAccessException)(Sub() ModeTravailRegles.Resoudre(True, True, False, True, "CAISSIER", seuleCaisse))
        Dim combineAutorise As New HashSet(Of String)(seulFacturier, StringComparer.OrdinalIgnoreCase)
        combineAutorise.UnionWith(seuleCaisse)
        Verifie(ModeTravailRegles.Resoudre(True, True, True, True, "OPERATEUR", combineAutorise).Count = 2, "Deux droits ne fabriquent pas un combine")
        combineAutorise.Add("MODE_COMBINE")
        Verifie(ModeTravailRegles.Resoudre(True, True, True, True, "OPERATEUR", combineAutorise).Count = 3, "Trois choix lorsque le combine est explicite")
        Verifie(ModeTravailRegles.Resoudre(True, True, True, False, "OPERATEUR", combineAutorise).Count = 2, "Combine globalement desactive")
        Verifie(ModeTravailRegles.Resoudre(True, False, False, True, "OPERATEUR", combineAutorise)(0) = "FACTURATION_ET_CAISSE", "Combine unique eligible, opt-in explicite au login")
        Verifie(ModeTravailRegles.Resoudre(True, True, True, True, "ADMIN", New HashSet(Of String)(seulFacturier)).Count = 0, "Portail administratif historique preserve")
        Verifie(ModeTravailRegles.Resoudre(True, True, True, True, "ADMIN", combineAutorise).Count = 3, "Admin explicitement autorise peut choisir un mode")
        Verifie(ModeTravailRegles.Resoudre(True, False, False, False, "ADMIN", combineAutorise).Count = 0, "Portail administratif de recuperation sans attribuer le comptoir")
        Dim incomplet As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {"MODE_COMBINE"}
        Refuse(Of UnauthorizedAccessException)(Sub() ModeTravailRegles.Resoudre(True, True, True, True, "TEST", incomplet))
        Verifie(ModeTravailRegles.DiagnosticCombine(seuleCaisse).Contains("Créer une facture"), "Diagnostic des droits manquants sans union des roles")
        Dim premier As New Object(), second As New Object(), externe As New Object()
        Dim panier As New List(Of Object) From {premier}
        Dim lie As List(Of Object) = DelegationPrixRegles.InstantaneArticles(panier)
        panier.Clear()
        Verifie(lie.Count = 1 AndAlso Object.ReferenceEquals(lie(0), premier), "Clear du panier ne corrompt pas la position de la liste liee")
        panier.Add(premier)
        panier.Add(second)
        lie = DelegationPrixRegles.InstantaneArticles(panier)
        panier.RemoveAt(0)
        Verifie(lie.Count = 2 AndAlso Object.ReferenceEquals(lie(0), premier), "Suppression de selection isolee du CurrencyManager")
        panier.Clear() : panier.Add(premier)
        Verifie(Object.ReferenceEquals(DelegationPrixRegles.ChoisirArticle(panier, Nothing), premier), "Une seule ligne selectionnee sans ambiguite")
        panier.Add(second)
        Verifie(DelegationPrixRegles.ChoisirArticle(panier, Nothing) Is Nothing, "Plusieurs lignes exigent une selection")
        Verifie(Object.ReferenceEquals(DelegationPrixRegles.ChoisirArticle(panier, second), second), "Selection du second article conservee")
        Verifie(DelegationPrixRegles.ChoisirArticle(panier, externe) Is Nothing, "Reference hors panier refusee")
        panier.Clear()
        Verifie(DelegationPrixRegles.ChoisirArticle(panier, premier) Is Nothing, "Panier vide ou article retire")
        Dim droitsPrix As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {"PRIX_CONSULTER", "PRIX_DEMANDER_FACTURE"}
        Verifie(DelegationPrixRegles.AutoriseCommande("PRIX_DEMANDER_FACTURE", True, True, False, droitsPrix), "Demande facture autorisee sans modification directe")
        Verifie(Not DelegationPrixRegles.AutoriseCommande("PRIX_DEMANDER", True, True, True, droitsPrix), "Demande facture ne donne pas acces au tarif officiel")
        Dim actionsLigne As New HashSet(Of String) From {"PRIX_DEMANDER_FACTURE"}
        Verifie(DelegationPrixRegles.CommandeDisponible(actionsLigne, "FACTURE", True), "Commande ligne pour demandeur habilite")
        Verifie(Not DelegationPrixRegles.CommandeDisponible(actionsLigne, "TARIF", True), "Aucun heritage de la ligne vers le catalogue")
        Verifie(Not DelegationPrixRegles.CommandeDisponible(actionsLigne, "FACTURE", False), "Panier vide ou selection absente : commande desactivee")
        Dim actionsTarif As New HashSet(Of String) From {"PRIX_TARIF_MODIFIER"}
        Verifie(DelegationPrixRegles.CommandeDisponible(actionsTarif, "TARIF", True), "Commande catalogue avant ajout au panier")
        Verifie(Not DelegationPrixRegles.CommandeDisponible(actionsTarif, "FACTURE", True), "Aucun heritage du catalogue vers la ligne")
        Verifie(Not DelegationPrixRegles.CommandeDisponible(actionsTarif, "TARIF", False), "Type de vente absent : commande desactivee")
        actionsTarif.Clear()
        Verifie(Not DelegationPrixRegles.CommandeDisponible(actionsTarif, "TARIF", True), "Permission retiree : selection ne reactive pas le bouton")
        Verifie(Not DelegationPrixRegles.CommandeDisponible(Nothing, "TARIF", True), "Diagnostic indisponible : refus ferme")
        Verifie(Not DelegationPrixRegles.CommandeDisponible(actionsLigne, "TOUS", True), "Perimetre inconnu : aucune extension de droits")
        For Each code As String In New String() {"PRIX_DEMANDER", "PRIX_DEMANDER_FACTURE", "PRIX_TARIF_MODIFIER", "PRIX_FACTURE_EXCEPTION"}
            droitsPrix.Add(code)
            Verifie(Not DelegationPrixRegles.AutoriseCommande(code, False, True, True, droitsPrix), "Delegation desactivee : " & code)
            Verifie(DelegationPrixRegles.AutoriseCommande(code, True, True, True, droitsPrix), "Action explicite active : " & code)
            Verifie(Not DelegationPrixRegles.AutoriseCommande(code, True, False, False, droitsPrix), "Actions globales desactivees : " & code)
            droitsPrix.Remove(code)
            Verifie(Not DelegationPrixRegles.AutoriseCommande(code, True, True, True, droitsPrix), "Permission retiree : " & code)
        Next
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
        DelegationPrixRegles.VerifierPagination(0, 25)
        compte += 1
        Refuse(Of ArgumentException)(Sub() DelegationPrixRegles.VerifierPagination(-1, 25))
        Refuse(Of ArgumentException)(Sub() DelegationPrixRegles.VerifierPagination(0, 101))
        Refuse(Of ArgumentException)(Sub() DelegationPrixRegles.VerifierPagination(1000001, 25))
        DelegationPrixRegles.VerifierContexte(Guid.NewGuid(), 0.5D)
        compte += 1
        Refuse(Of ArgumentException)(Sub() DelegationPrixRegles.VerifierContexte(Guid.Empty, 1D))
        Refuse(Of ArgumentException)(Sub() DelegationPrixRegles.VerifierContexte(Guid.NewGuid(), 0D))
        Refuse(Of ArgumentException)(Sub() DelegationPrixRegles.VerifierContexte(Guid.NewGuid(), 0.000000001D))
        DelegationPrixRegles.VerifierDuree(24)
        compte += 1
        Refuse(Of ArgumentException)(Sub() DelegationPrixRegles.VerifierDuree(0))
        Refuse(Of ArgumentException)(Sub() DelegationPrixRegles.VerifierDuree(169))
        Verifie(DelegationPrixRegles.PrixNet(100D, New ContexteCommercialPrix With {.SousTotal = 200D, .Remise = 20D}) = 90D, "Coût comparé au prix après remise")
        Refuse(Of ArgumentException)(Sub() DelegationPrixRegles.PrixNet(100D, New ContexteCommercialPrix With {.SousTotal = 0D}))
        Refuse(Of ArgumentException)(Sub() DelegationPrixRegles.PrixNet(100D, New ContexteCommercialPrix With {.SousTotal = 100D, .Remise = 101D}))
        DelegationPrixRegles.VerifierTotauxException(50.275D, 50.28D, 0D, 0D, 50.28D)
        compte += 1
        DelegationPrixRegles.VerifierTotauxException(0.999D, 0.99D, 0D, 0D, 0.99D, 0.99D)
        compte += 1
        Refuse(Of ArgumentException)(Sub() DelegationPrixRegles.VerifierTotauxException(50.275D, 49D, 0D, 0D, 49D, 50.28D))
        Console.WriteLine("PASS : " & compte.ToString() & " contrôles métier réels, sans connexion SQL.")
    End Sub

    Private Sub TesterCoutsEtReseau()
        Dim achat As Decimal = 23000D
        Verifie(CalculVenteService.CalculerCoutComparable(achat, 30D, 30D, "UNITE", 30D).Value = 23000D, "Cout gros sans arrondi intermediaire")
        Verifie(CalculVenteService.CalculerCoutComparable(achat, 15D, 30D, "UNITE", 30D).Value = 11500D, "Cout demi sur reference achat")
        Verifie(Math.Round(CalculVenteService.CalculerCoutComparable(achat, 1D, 30D, "UNITE", 30D).Value, 2) = 766.67D, "Cout piece")
        Dim type As New TypeVenteProduitDTO With {.QuantiteEquivalent = 11D, .ProduitConditionnementId = 1}
        Dim piece As New ProduitConditionnementDTO With {.ProduitConditionnementId = 1, .FacteurVersBase = 1D}
        Dim qte As Decimal = ConversionUniteService.CalculerQuantiteBase(1D, type, piece)
        Dim cout As Decimal = CalculVenteService.CalculerCoutComparable(achat, qte, 30D, "UNITE", 30D).Value
        Verifie(qte = 11D AndAlso Math.Round(cout, 2) = 8433.33D, "11 pieces dans un carton de 30, pas 11 cartons")
        Verifie(TarifCatalogueRegles.Marge(14000D, cout).Value = 66.01D AndAlso TarifCatalogueRegles.Coefficient(14000D, cout).Value = 1.6601D, "Marge et coefficient du meme conditionnement")
        Dim carton As New ProduitConditionnementDTO With {.ProduitConditionnementId = 2, .FacteurVersBase = 30D}
        type.ProduitConditionnementId = 2
        Dim qteCartons As Decimal = ConversionUniteService.CalculerQuantiteBase(1D, type, carton)
        Verifie(qteCartons = 330D AndAlso CalculVenteService.CalculerCoutComparable(achat, qteCartons, 30D, "UNITE", 30D).Value = 253000D, "Reference carton conservee, pas de correction implicite selon le nom")
        Verifie(Math.Round(CalculVenteService.CalculerCoutUnitaireBase(achat, 30D).Value * qteCartons, 2) = 253000.01D, "Reproduction exacte de l'anomalie signalee avec reference 330")
        Verifie(14000D < CalculVenteService.CalculerCoutComparable(achat, qteCartons, 30D, "UNITE", 30D).Value, "Une hausse ne contourne pas le cout reel de 11 cartons")
        Verifie(CalculVenteService.CalculerQuantiteBaseTypeVente(11D, "SECONDAIRE", 30D, 30D, 1D) = 11D, "Type historique secondaire")
        Verifie(CalculVenteService.CalculerQuantiteBaseTypeVente(11D, "PRINCIPALE", 30D, 30D, 1D) = 330D, "Type historique principal distinct")
        Verifie(CalculVenteService.CalculerCoutComparable(25000D, 5D, 1D, "POIDS", 25D).Value = 5000D, "Cout mesure utilise contenu principal et non nombre de colis")
        Verifie(Not CalculVenteService.CalculerCoutComparable(0D, 11D, 30D, "UNITE", 30D).HasValue, "Achat inconnu jamais invente")
        Refuse(Of ArgumentOutOfRangeException)(Sub() CalculVenteService.CalculerCoutComparable(achat, 0D, 30D, "UNITE", 30D))
        Verifie(SqlDiagnosticRegles.Classer(-2, True) = "DELAI_CONNEXION" AndAlso SqlDiagnosticRegles.Classer(-2, False) = "DELAI_COMMANDE", "Timeout ouverture distinct de commande")
        Verifie(SqlDiagnosticRegles.Classer(1205, False) = "INTERBLOCAGE" AndAlso SqlDiagnosticRegles.Classer(1222, False) = "ATTENTE_VERROU", "Verrous distincts du reseau")
        Verifie(SqlDiagnosticRegles.Classer(18456, True) = "AUTHENTIFICATION" AndAlso SqlDiagnosticRegles.Classer(4060, True) = "BASE_INDISPONIBLE", "Authentification et base inaccessibles")
        Verifie(SqlDiagnosticRegles.Classer(10054, False) = "TRANSPORT" AndAlso SqlDiagnosticRegles.Classer(207, False) = "SQL_APPLICATIF", "Erreur schema jamais masquee comme panne reseau")
        Verifie(SqlDiagnosticRegles.ResultatIncertain(-2, True) AndAlso SqlDiagnosticRegles.ResultatIncertain(10054, True) AndAlso Not SqlDiagnosticRegles.ResultatIncertain(10054, False), "Ecriture envoyee potentiellement validee, ouverture sans commande non executee")
        Verifie(Not SqlDiagnosticRegles.ResultatIncertain(1205, True), "Victime interblocage distincte du COMMIT incertain")
        Dim delais As New TemporisationSondeSql
        Dim maintenant As New DateTime(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc)
        For Each secondes As Integer In New Integer() {15, 30, 60, 60}
            delais.Enregistrer(False, maintenant)
            Verifie(delais.ProchaineVerificationUtc = maintenant.AddSeconds(secondes), "Temporisation bornee apres interruption")
        Next
        delais.Enregistrer(True, maintenant)
        Verifie(delais.ProchaineVerificationUtc = maintenant.AddSeconds(15), "Reseau retabli : surveillance normale, sans action metier")
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
