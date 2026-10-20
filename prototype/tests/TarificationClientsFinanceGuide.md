# Tarification, Clients et presentation Finance

## Audit initial et decisions

Depart main propre : 5cd0bbf64a8d4c86b011e1df6a5ba72d55ce1f6d.
Aucune capture accessible dans ce message ; descriptions et code examines.
Pas d'acces a SQL Server ni de lancement Windows dans cet environnement.

- Facturation avait des secondaires blancs et le bouton tarif en police 9.5,
  contre 10 gras pour les autres commandes. La palette ciblee est conservee,
  avec fonds secondaires legerement teintes et typographie homogene.
- Produits/CalculerPrixAuto derive volontairement plusieurs prix du coefficient
  gros ; la delegation ne faisait pas cette operation, mais modifiait un tarif.
  Cette distinction est PRESERVEE : pas de recalcul general implicite.
- La liste des demandes avait deja recherche et pagination SQL (25 lignes).
  Elle est modernisee, pas remplacee. Aucune nouvelle trace de consultation.
- Top Clients regroupait DEJA par ClientId et nom, sans jointure sur les lignes.
  Un nom repete ne demontre donc pas une double facture : plusieurs comptes
  homonymes doivent rester distincts. Aucun DISTINCT ou fusion automatique.
- Defauts reels du classement : CAST AS int tronquait sommes/moyennes et la
  periode etait figee aux 30 derniers jours. Les identifiants techniques etaient
  visibles. Classement et graphique utilisent maintenant le meme mois/annee.
- Le rectangle orange Finance correspondait au badge de controle physique.
  Il ne faut pas le confondre avec une cloture globale de l'etablissement.
  EstCloturee est relu dans le controle du compte, sans changer aucune regle.

## Tarification : sources et securite

TarifCatalogueRegles partage uniquement la disponibilite standard historique
avec TypeVenteService : drapeaux Gros/Demi/Detail/Douzaine ET prix positif ;
Quart et Special/Promo n'ont pas de drapeau distinct, comme auparavant.
Les calculs et quantites du moteur TypeVenteService sont inchanges.

DelegationPrixService.ListerTarifsActifs charge en lot le produit, ses types
actifs et ses conditionnements vendables, dans une transaction courte. Le calcul
LireTarifInterne est le MEME que LireTarif, avec les donnees prechargees ; pas de
requete par ligne/type, pas de deuxieme calcul de conversion ou cout.
Les ecritures relisent toujours SQL sans ce cache et verifient l'empreinte.
Un nouveau changement ou une approbation sur un type devenu inactif est refuse.
Les anciens documents ne sont pas reecrits pour changer leur type ou prix.

Le dialogue montre les tarifs proposes par le catalogue et un seul prix
editable a la fois. La selection change le tarif cible, jamais les autres prix.
Les types personnalises homonymes suivent la priorite standard deja appliquee
par TypeVenteService ; aucune fusion de leurs identifiants SQL n'est effectuee.
Une exception FACTURE reste verrouillee sur sa ligne et son contexte exact.

Prix d'achat de reference : lecture seule apres PRIX_CONSULTER, permission
existante donnant deja acces au cout comparable. Aucun droit de modifier
l'achat n'est ajoute. Les droits PRIX_TARIF_MODIFIER, PRIX_DEMANDER,
PRIX_FACTURE_EXCEPTION et PRIX_DEMANDER_FACTURE restent independants.
Seuils, sous-cout apres remise, motif, approbateur distinct et usage unique
des approbations restent controles par les services existants.

Les indicateurs sont recalcules pendant la frappe (TextChanged/ValueChanged).
Marge : meme formule sur cout comparable que dans Produits, arrondie a 2 decimales.
Coefficient : ratio a 4 decimales, sur CoutCoefficient pour un type COEFFICIENT,
comme Appliquer ; les prix non representables sont signales puis refuses par
la validation existante. Les tarifs FIXE restent independants : leur ratio
affiche ne modifie pas un coefficient commun.

Les nouveaux champs de presentation portent ScriptIgnore. Ils n'entrent pas
dans l'empreinte des tarifs et n'invalident pas arbitrairement les anciennes
approbations. L'audit d'application garde les valeurs avant/apres et ajoute la
marge, dans la MEME transaction SQL. Demandeur/decideur sont ceux des comptes
et demandes existants. Pas de nouveau privilege active par defaut.

## Propagation controlee

Appliquer touche PRODUITS et TYPES_VENTE dans AppDataVersions avant COMMIT.
Facturation dispose deja de son moniteur : les tarifs disponibles se rechargent
sans remplacer les prix du panier existant.

Produits et Stock_Final_FIXED utilisent le meme DataChangeMonitorService via
AlerteTarifsPanel. Intervalle de 5 secondes, pas de polling par produit. Un
bandeau signale les nouvelles versions, sans remplacer les champs saisis.
Le rechargement exige une confirmation d'abandon des modifications de la fiche.
Une sauvegarde de cette fiche est bloquee tant qu'une version perimee a ete
signalee. L'alerte est acquittee apres rechargement reussi. Le moniteur est
dispose avec le controle ; aucune session d'un autre poste n'est changee.
Les versions sont capturees AVANT la lecture des produits, puis acquittees
seulement si elle reussit. L'initialisation facultative du moniteur reprend ces
versions : pas d'alerte pour une version deja rechargee, ni de changement perdu
entre le chargement initial et le premier passage du timer.

Limite : ce signal UI est asynchrone (jusqu'a environ 5 secondes), pas un nouveau
verrou SQL global. Il ne remplace pas la concurrence des services. Les tests
multi-postes, notamment avec saisie non enregistree et panne de connexion,
restent obligatoires ; aucune capacite de charge n'est annoncee.

## Clients et Finance

Le CRUD Clients est conserve. ClientId reste present dans les tables liees,
mais masque dans les deux grilles. Telephone permet de distinguer visuellement
les homonymes ; aucun nom n'est utilise comme cle d'identite.

ClientRepository.ClasserAchats agrege les entetes FacturesVente payees seulement,
une fois par facture, sans jointure articles. COUNT_BIG, SUM et AVG decimaux,
bornes @du inclus / @au exclu sur CreeLe de la facture, tri stable et TOP 20.
Il s'agit de la periode de creation des factures, pas d'un rapport de caisse
sur la date d'encaissement. Pas de conversion de paiements
USD appliquee une seconde fois : le MontantTotal des factures est deja en FC
dans ce modele. Les ventes sans ClientId fiable ne sont pas attribuees a une
identite inventee. Les annulees/non payees sont exclues comme auparavant.
Le mot REMBOURSE trouve dans l'analyse de caisse concerne une regularisation
d'ecart, pas un remboursement de facture : aucune nouvelle regle de vente nette
n'est deduite de ce statut sans modele commercial correspondant.

Le graphique reprend la meme somme de quantites par libelle produit, mais sur
la periode choisie. En cas d'erreur, les anciennes donnees ne restent pas
presentees comme celles du nouveau client/mois.

Finance : uniquement presentation des boutons +, Valider, Imprimer et controle ;
badge neutre avec texte distinct, statut du compte issu d'EstCloturee. Absence
de controle = aucun enregistrement, PAS une cloture ou ouverture inventee.
Aucune depense, ecriture de banque/caisse ou regularisation nouvelle.

Demandes : bandeau 52/73/94, en-tetes 245/245/245 (normal/selection), texte
52/73/94 et selection des lignes preservee. Actions separees des filtres, etat
vide, pagination active selon la page. Le prix de reference est celui enregistre
a la demande ; le detail relit en SQL le tarif courant, avec permission et
controle de propriete, sans N+1 sur la grille et sans audit fictif de lecture.
Le detail affiche DecisionPar et DecisionLe persistants, avec le nom actuel du
compte s'il existe ; aucun auteur de remplacement n'est invente. L'identite
historique au moment de l'action reste celle du JournalAudit existant.

## Fichiers

Applicatifs modifies : DevCommerc8ak.vbproj ; Forms/FacturationForm.vb ;
Repositories/ClientRepository.vb ; Services/ClientService.vb,
DelegationPrixService.vb, TypeVenteService.vb, DataChangeMonitorService.vb,
ThemeHelper.vb ;
UI/DialogueDelegationPrix.vb, CartesSecuriteAutorisations.vb,
FormulaireClients1.vb, FormulaireProduits.vb, FormulaireStock_Final_FIXED.vb,
FormulaireFinance (6).vb.
Nouveaux : Services/TarifCatalogueRegles.vb, ClientAnalyseRegles.vb,
UI/AlerteTarifsPanel.vb.
Tests : projets/programmes AuditMetierCoreTests et AuditMetierSqlTests,
PanierWinFormsTests/Program.vb ; ce guide et diagnostic SQL de lecture seule.
Aucune migration, aucune dependance externe, aucun moteur d'impression ou reset
edite. Aucun changement des conversions physiques ou des ecritures de stock.

## Verification Windows / SQL Server

Sur COPIE ISOLEE uniquement. Ne pas lancer de reset ou nettoyer les donnees.
Prerequis : migrations deja prevues par les phases precedentes, notamment prix,
conditionnements, audit et AppDataVersions ; aucun script nouveau a appliquer.
Visual Studio Desktop .NET, Developer Pack 4.7.2 et vraie DLL Guna.UI2.

1. Recuperer le commit, compiler Release. Executer le diagnostic SQL en lecture
   seule avec la bonne annee/mois, verifier serveur et base avant tout autre essai.
2. Produit Gros+Detail seulement : deux types, pas de Demi fictif. Puis plusieurs
   standards, Special/Promo, type personnalise FIXE/COEFFICIENT (y compris casse
   differente), conditionnement actif/non vendable. Aucun type desactive editable.
3. Prix autorise/refuse, motif vide, hors variation/marge, sous-cout apres remise,
   precision coefficient : messages pertinents et absence de modification refusee.
4. Tarif officiel : un tarif change en SQL, historique/audit auteur reel et marge
   avant/apres. Une approbation ne peut pas etre appliquee deux fois ou a une
   autre ligne/type/quantite. Tester expiration et revocation des permissions.
5. Exception facture : seule la ligne et le panier changent ; catalogue et anciennes
   factures inchanges. Verifier le montant lors de l'enregistrement reel.
6. Deux postes : modifier un tarif pendant une saisie Produits/Stock. Bandeau sans
   perte de saisie ; refus du rechargement conserve les champs ; acceptation
   recharge SQL. Facturation propose le nouveau tarif aux nouvelles lignes sans
   changer son panier. Verifier aussi les types personnalises et la panne SQL.
7. Consulter mes demandes : filtres, 0/25/26 resultats, details prix courant,
   demande propre vs demande d'un autre compte, approbation/refus/annulation avec
   motif et audit. Une simple consultation ne cree aucune trace de succes metier.
8. Clients homonymes : identites distinctes et telephone visible, ClientId masque
   mais selection/CRUD fonctionnels. Une facture multi-articles compte une seule
   fois. Decimales, janvier/fevrier/annee entiere, derniere seconde du mois,
   factures annulees/en attente et absence de ClientId : totaux attendus.
9. Graphique : meme client et periode que le classement, noms longs et pas de
   donnees anciennes conservees apres une erreur SQL.
10. Finance : +categorie, validation depense et impression ont les memes evenements
    et validations ; boutons alignes a 100/125/150 %. Controle absent, ouvert et
    cloture : texte lisible sans grand rectangle orange ; ecritures inchangees.
11. Parcours classiques/combine, attente/reprise, ticket 80 mm, A4/PDF et historique
    : essais de non-regression requis, pas seulement verification de compilation.

```powershell
msbuild prototype\WinFormsVB\DevCommerc8ak\DevCommerc8ak.vbproj /restore /p:Configuration=Release /p:GunaUI2AssemblyPath="C:\Libs\Guna.UI2.dll"
dotnet run --project prototype/tests/AuditMetierCoreTests -c Release
msbuild prototype\tests\PanierWinFormsTests\PanierWinFormsTests.vbproj /restore /p:Configuration=Release
& .\prototype\tests\PanierWinFormsTests\bin\Release\net472\PanierWinFormsTests.exe
$env:ERP_PHASE1_TEST_ENABLE='YES'
$env:ERP_PHASE1_TEST_CONNECTION='Server=.\ERPCOMMERCIAL_TEST;Database=ERP_Phase1_TEST;Integrated Security=True;TrustServerCertificate=True'
msbuild prototype\tests\AuditMetierSqlTests\AuditMetierSqlTests.vbproj /restore /p:Configuration=Release
& .\prototype\tests\AuditMetierSqlTests\bin\Release\net472\AuditMetierSqlTests.exe
Remove-Item Env:ERP_PHASE1_TEST_ENABLE
Remove-Item Env:ERP_PHASE1_TEST_CONNECTION
```

Le banc SQL reste opt-in sur base suffixee _Phase1_TEST. Le test d'agregation
utilise la requete EXACTE du repository, redirigee vers des tables temporaires
de la connexion de test. Les autres scenarios ajoutent des fixtures dans la
copie : ne pas le confondre avec le diagnostic strictement non destructif.

## Resultats disponibles dans cet environnement Linux

- AuditMetierCoreTests : 148 controles executes, tous reussis, sans SQL.
- API et Dashboard : compilations Release reussies ; aucune execution metier
  ou validation de leur interface n'est deduite de ces compilations.
- AuditMetierSqlTests et PanierWinFormsTests : compilation reussie avec les
  references Microsoft .NET Framework 4.7.2 temporaires ; NON EXECUTES.
- WinForms complet : compilation standard impossible (MSB3644, targeting pack
  absent). Avec les references temporaires, Guna.UI2 manque encore pour les
  Guna2TextBox du vrai LoginForm. Aucune dependance n'a ete supprimee du projet.
- Verification supplementaire des sources modifiees : compilation isolee,
  hors depot, avec substitution temporaire du LoginForm et sans Guna. Cette
  verification partielle n'est PAS la compilation du livrable WinForms complet.
- Aucun SQL execute, aucune migration appliquee, aucune base reinitialisee.
- Verification visuelle Windows, DPI, imprimantes, approbations/revocations,
  transactions/concurrence SQL et propagation multi-postes : NON EXECUTEES.

WinForms non valide dans cet environnement. Les essais d'acceptation Windows
et SQL Server ci-dessus restent requis avant deploiement en production.
