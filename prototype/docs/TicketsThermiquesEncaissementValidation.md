# Tickets thermiques et preuve d'encaissement

## Causes confirmees dans le code

Base de travail : `a91dca87ea969fa4002427d031967c09b9724241`.

1. `CaisseForm.ImprimerTicket(sender,e)` construisait un `TicketData` depuis
   la selection et les champs montant/devise/mode/reference. Aucune lecture
   d'un paiement confirme n'etait exigee. Le bouton etait active des qu'une
   facture etait selectionnee. `Encaisser` memorisait aussi ce modele saisi
   AVANT le retour SQL, au lieu de relire les donnees de paiement.
2. `CaisseForm.GetLibelleTypeVentePourTicket` traduisait GROS/DEMI a partir
   d'UnitePrincipale/UniteSecondaire obtenues par jointure au catalogue ACTUEL.
   Facturation et Historique transmettaient directement le type technique.
3. `FormulaireFactures.CompleterTicketHistorique` concatenait `PaiementId`
   au libelle client `Reglement #...`.
4. Le statut thermique utilisait une paire generique. Il est maintenant
   explicitement `STATUT`, avec le style gras existant (Police=1).

## Corrections et securite

- `ConditionnementCommercial` reprend les regles de presentation de la Caisse
  pour gros, detail, demi, quart et douzaine, sans conversion ni calcul de prix.
  Les libelles personnalises restent exactement ceux configures, y compris
  lorsqu'ils ressemblent a un code standard.
- Le renderer appelle cette regle pour toutes les lignes thermiques. Les
  informations supplementaires sont exclusivement de presentation : A4 ne
  les utilise pas. Les unites connues sont capturees a l'ajout d'une nouvelle
  ligne du panier Facturation, sans modifier son TypeVente ni son enregistrement.
  Les nouvelles proprietes sont masquees de la generation de colonnes du panier.
- Pour les nouvelles creations/modifications, deux champs descriptifs sont
  ajoutes au SELECT du snapshot deja ecrit dans JournalAudit, dans sa transaction
  existante. Aucun nouvel evenement, changement de montant ou nouvel acces SQL
  n'est ajoute a l'enregistrement metier. Un lecteur restaure ces unites depuis
  le dernier snapshot reussi de la facture, apres comparaison de toutes les
  lignes ordonnees : produit, type, quantite, prix et montant. Un snapshot ancien,
  incomplet ou divergent n'est jamais utilise. Aucun identifiant technique
  recree n'est ajoute a la comparaison des differences commerciales d'audit.
- `JustificatifCaisseService.Lire` exige la session active et le droit EXISTANT
  ENCAISSEMENT_CREER sur CAISSE, sans privilege implicite de role. Il exige
  un encaissement de l'utilisateur reel. L'historique reste le parcours pour
  consulter les autres documents avec ses permissions propres.
- La facture, ses lignes et ses paiements sont lus dans une courte transaction
  Serializable. Il faut PAYEE, des paiements dates strictement positifs et une
  somme affectee exactement egale au total persiste, dans une devise comparable.
  Pas de NOLOCK, de transaction conservee pendant l'apercu ou de requete d'ecriture.
- Le paiement etant normalise en FC dans le flux existant, DeviseMontants doit
  etre FC. Pour les anciens paiements sans cette colonne renseignee, le repli
  n'est accepte que si la devise conservee est FC. Un ancien paiement USD de
  denomination inconnue est refuse, sans appliquer le taux actuel.
- La Caisse ne cree plus de modele depuis les champs de saisie. Elle memorise
  uniquement l'identifiant apres le retour reussi d'EncaisserFacture. Le bouton
  DERNIER TICKET est initialement desactive. Il reimprime le dernier encaissement
  confirme de cette fenetre, pas la nouvelle facture impayee selectionnee.
- Apercu et chaque exemplaire relisent les droits et le paiement : la memoire
  locale n'est pas la preuve du paiement. Les donnees inconnues de montant recu
  ou monnaie restent « Non renseigne (historique) », jamais inventees.
- Un echec d'impression ne rejoue ni creation, ni paiement, ni sortie de stock.
  Le paiement reste valide et le message l'indique. Une perte de confirmation
  SQL doit toujours etre verifiee dans l'historique avant toute nouvelle saisie.
- Le libelle client devient simplement « Reglement », repete pour chaque
  paiement avec son mode, sa date, son caissier et sa reference disponibles.
  Les identifiants SQL restent dans les lectures et la base, pas sur le ticket.
- La proforma n'a aucun statut/reglement atteste et conserve explicitement
  « DOCUMENT PROFORMA — NON ACQUITTE » (accents dans l'application).
- Marges, largeur utile, hard margins, polices, mesure/dessin et pagination
  sont inchanges. Aucune migration ni modification des moteurs financiers,
  prix, conversions, stock, permissions ou reset.

## Limite historique importante

Dans les donnees anterieures a cette livraison, `LignesFactureVente` et les
snapshots conservent TypeVente, QuantiteSaisie et QuantiteBase mais PAS le nom
historique de l'unite principale ou secondaire. La conversion numerique ne prouve pas si l'unite etait un carton,
un sac ou un bidon. Une jointure au produit actuel ne peut pas retablir ce passe.

Avec un nouveau snapshot correspondant, la reimpression restitue les unites
enregistrees dans l'audit, meme si le catalogue a change depuis. Sinon une
ligne ancienne conserve son texte TypeVente exact. Si elle
contient deja « demi-sac » ou un type personnalise, ce texte est respecte. Si
elle ne contient que « gros » ou « demi », ce repli reste affiche, y compris
sur une reimpression Caisse. Aucun conditionnement historique n'est invente.
Les lignes reprises d'un ancien brouillon restent egalement sans instantane
fiable tant qu'une nouvelle version n'est pas volontairement enregistree.
Le lecteur d'audit ne modifie aucune trace. Cette livraison ne cree aucune
migration et ne reecrit aucune facture pour contourner l'absence de donnees.

Le flux Caisse existant ne prend en charge que le solde integral. Aucune nouvelle
politique de justificatif partiel n'est introduite. Les statuts reellement
prevus sont EN_ATTENTE/BROUILLON, PAYEE et ANNULEE ; tout autre statut persiste
reste affiche tel quel dans Historique, sans deduire un statut par impression.

## Fichiers applicatifs

- `Forms/CaisseForm.vb` : suppression du modele temporaire et du dessin/adaptation obsolete.
- `Forms/FacturationForm.vb` : capture d'unites du panier pour la seule presentation.
- `Services/ConditionnementCommercial.vb` : regle partagee extraite de la Caisse.
- `Services/ConditionnementsHistoriquesService.vb` : restitution de l'instantane valide sans catalogue courant.
- `Services/FactureOperationService.vb` : deux champs descriptifs en plus dans le snapshot existant, regles inchangees.
- `Services/JustificatifCaisseRegles.vb` : validation pure des preuves de paiement.
- `Services/JustificatifCaisseService.vb` : autorisation et lecture coherente du justificatif.
- `Repositories/FactureImpressionRepository.vb` : lectures groupees parametrees.
- `Services/FacturePrintRenderer.vb` : libelles thermiques et statut explicite gras.
- `UI/FormulaireFactures.vb` : presentation commerciale du reglement.
- `DevCommerc8ak.vbproj` : inclusion des quatre nouveaux fichiers de service/regles.

Les tests adaptes sont `AdminThermalCoreTests` (projet, Program et
ThermiqueMetierTests), `AdminThermalContracts.Tests.ps1` et
`AdminThermalWindowsTests` (projet, Program et IntegrationCaisse).
Le diagnostic SQL en lecture seule est `prototype/tests/TicketsThermiquesDiagnostic.sql`.

## Controles disponibles

Depuis la racine :

```powershell
dotnet run --project prototype/tests/AdminThermalCoreTests -c Release
dotnet run --project prototype/tests/AuditMetierCoreTests -c Release
pwsh -NoProfile -File prototype/tests/AdminThermalContracts.Tests.ps1
git diff --check
```

Les contrats comparent les moteurs sensibles a la version anterieure et
verifient la conservation des methodes A4 et de geometrie thermique. Les tests
purs ne remplacent pas une execution SQL : ils couvrent les libelles, les
replis historiques, les montants, statuts, devises, absences et sommes de paiements.

Sous Windows, compiler l'application complete en Release avec ses dependances
habituelles et le Developer Pack .NET Framework 4.7.2, puis :

```powershell
dotnet build prototype/tests/AdminThermalWindowsTests -c Release
& .\prototype\tests\AdminThermalWindowsTests\bin\Release\net472\AdminThermalWindowsTests.exe
```

Ce banc utilise le vrai renderer/GDI et ne lance aucune impression physique.
Sans activation explicite, son integration SQL est annoncee NON EXECUTEE.
Pour la lancer, uniquement sur une copie nommee `..._THERMIQUE_TEST`, ouvrir
dans l'application de test une session CAISSE autorisee et encore active.
Preparer une facture payee par cet utilisateur et une facture impayee sur cette
copie. Relever leurs identifiants et celui de la session avec le diagnostic SQL.
Ne pas utiliser une copie sur laquelle d'autres tests ecrivent simultanement.

```powershell
$env:ERP_THERMIQUE_SQL_ENABLE = 'YES'
# Fournir la connexion a la COPIE avec le mecanisme securise du poste.
# Ne jamais publier cette valeur ni la mettre dans un fichier du depot.
$env:ERP_THERMIQUE_TEST_CONNECTION = Read-Host 'Connexion Windows integree de la copie (sans mot de passe)'
$env:ERP_THERMIQUE_TEST_SESSION = '123' # remplacer par la session active de test
$env:ERP_THERMIQUE_TEST_FACTURE_PAYEE = '456' # remplacer
$env:ERP_THERMIQUE_TEST_FACTURE_IMPAYEE = '789' # remplacer
try {
    & .\prototype\tests\AdminThermalWindowsTests\bin\Release\net472\AdminThermalWindowsTests.exe
} finally {
    Remove-Item Env:ERP_THERMIQUE_TEST_CONNECTION
    Remove-Item Env:ERP_THERMIQUE_SQL_ENABLE
}
```

Preferer une connexion Windows integree. Le banc refuse toute base sans le
suffixe impose. Il ne cree ni utilisateur, ni role, ni droit, ni facture : il
teste les refus, deux lectures du meme paiement, les sessions/roles invalides
et l'absence de variation des comptages/montants des principales tables.

## Recette manuelle Windows/SQL/imprimante indispensable

1. Sur une copie, proforma avec carton, sac, demi-sac, demi-carton, detail et
   types personnalises. Verifier quantites/prix/total, accents et noms longs.
2. Sur Historique, imprimer une facture payee, un brouillon et les etats
   disponibles : statut gras, aucun #PaiementId, montants historiques inchanges.
3. Sur la copie seulement, changer l'unite actuelle d'un produit apres une
   vente : la reimpression ancienne doit garder TypeVente, pas la nouvelle unite.
4. En Caisse, selection impayee et montant tape : bouton indisponible ; aucun
   appel direct au service ne doit accepter une facture sans paiement confirme.
5. Simuler une erreur/annulation SQL avant COMMIT en environnement de test :
   aucune preuve acquittee ; ne jamais simuler ce cas en production.
6. Valider normalement : ticket avec montants/mode/date du paiement persiste.
   Modifier ensuite les champs de saisie : DERNIER TICKET doit rester identique.
7. Dans la copie, retirer le droit ou annuler l'encaissement via le parcours
   autorise pendant un apercu. Le clic Imprimer doit relire et refuser.
8. Deux exemplaires/reimpressions : aucun nouveau paiement, facture, mouvement
   de stock ou ecriture de caisse. Verifier aussi les autres parcours d'historique.
9. Facture partiellement payee ou PAYEE sans reglement/coherence : refus du
   ticket acquitte, sans modification du paiement ni du statut.
10. Imprimante absente/arretee : vente deja enregistree conservee, nouvelle
    tentative d'impression sans nouveau paiement. Verifier A4 strictement inchange.
11. Papier reel 80 mm : marges, centrage, montants longs, accents et longues
    factures. Les jobs deja envoyes au spouleur ne sont pas rappelables : la
    verification porte sur l'etat confirme au moment de la lecture du document.
12. Creer une facture avec cette version puis changer l'unite du catalogue dans
    la copie : Historique et Caisse doivent utiliser les unites du snapshot de
    cette facture. Mesurer le plan/temps de lecture du dernier snapshot sur un
    volume d'audit representatif ; aucun index SQL n'est cree sans validation.

## Resultats observes sous Linux

- 143 controles purs geometrie/conditionnements/justificatifs reussis.
- 172 controles metier sans SQL reussis ; 75 contrats de source reussis.
- Builds API et Dashboard Release reussis (CS1998 preexistant dans l'API).
- Build WinForms standard impossible : MSB3644, references Framework 4.7.2
  manquantes. Controle VB partiel avec references temporaires et substitution
  temporaire du LoginForm dependant de Guna : compilation reussie, sans erreur.
  Ce controle ne valide pas le LoginForm reel ni l'application complete.
- Banc Windows/GDI et integration SQL : prepares pour Windows, non executes ici.

Ni Windows, ni SQL Server reel, ni imprimante physique ne sont accessibles
dans l'environnement Linux de developpement. Aucune validation de production
ni d'impression physique n'est annoncee par cette livraison.
