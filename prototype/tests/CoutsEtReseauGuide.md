# Couts personnalises et disponibilite SQL : diagnostic et validation

## Etat initial et limites de preuve

Depart main propre : f3c94a3d2e549f68ab706a68ac5a2f6a1b2feb7c.
Pas de connexion au serveur de production, SQL Server ou Windows disponible ici.
Les valeurs du produit installe ne sont PAS dans le depot. Le diagnostic SQL
fourni doit etre execute en lecture seule et ses resultats examines avant toute
correction des donnees de reference. Aucune migration n'est introduite.

## Mission couts

PrixAchat est le prix d'une unite principale d'achat. En gestion UNITE,
ConversionUnite est son equivalent en unites de stock. En gestion mesuree
(MESURE/POIDS/VOLUME), ContenuUnitePrincipale est la reference lorsqu'elle est
positive. C'est la regle de StockUnitConversionService.CalculerCoutUnitaireStock.

Un type personnalise rattache multiplie QuantiteEquivalent par FacteurVersBase
du ProduitConditionnementId. Sans rattachement : PRINCIPALE multiplie par le
contenu principal (ou conversion), SECONDAIRE par le contenu secondaire (ou 1),
MESURE est deja en base. Ces regles de conversion existantes restent INCHANGEES.

La reproduction confirmee par test est :

- PrixAchat=23000, ConversionUnite=30, cout par piece arrondi a 4 chiffres=766.6667.
- Equivalent physique=330 : ancien cout 766.6667*330=253000.011, affiche 253000.01.
- Equivalent physique=11 : cout exact proportionnel 8433.333..., affiche 8433.33.

Un facteur carton 30 applique a QuantiteEquivalent=11, ou une reference
PRINCIPALE de 30 appliquee a 11, explique 330. Cela reste une HYPOTHESE sur la
vraie base jusqu'au diagnostic. Le nom Paquets 10+1 ne prouve pas l'unite.
Si la base reference 11 cartons, le refus a 14000 reste correct : ne PAS
diviser arbitrairement le cout par 30 ni reduire la quantite de stock.

Correction certaine du code : CalculerCoutComparable reprend la meme reference
et le calcul proportionnel deja utilise par CalculerCoutEquivalentCoefficient,
mais sans arrondir le cout d'une piece avant multiplication. Gros=23000 et
demi=11500 sans centime fictif. Le cout de 330 devient 253000, pas 8433.33.
Le dialogue montre l'equivalent physique et sa reference pour verifier le sens
du type. L'erreur sous cout donne le cout et la quantite concernes.

Les coefficients des tarifs COEFFICIENT gardent leur formule existante. Aucune
modification automatique des tarifs, conversions ou stocks. La marge se met
deja a jour par TextChanged/ValueChanged sur le cout du meme objet tarifaire.
Les ecritures et l'audit restent dans les services transactionnels existants.
La propagation AppDataVersions vers Produits/Stock/Facturation est conservee.

IMPORTANT : une demande en attente dont le cout/empreinte change apres cette
correction de precision sera refusee par concurrence. La recreer et la faire
approuver ; ne jamais modifier les anciens hashes ni prix de factures.

### Verification de la configuration reelle

1. Executer CoutsPersonnalisesDiagnostic.sql avec le bon ProduitId (ou libelle).
   Verifier serveur/base et identite du produit ; ne pas fusionner des homonymes.
2. Relever unite d'achat, conversion, contenu principal/secondaire, quantite du
   type, reference historique et conditionnement/facteur reellement rattache.
3. Comparer avec le conditionnement physique vendu. Pour 11 pieces, il faut une
   reference persistante qui donne 11 unites de base ; pour 11 cartons de 30,
   l'equivalent est 330. Ne pas deduire ce choix du prix souhaite.
4. Si la reference est erronee, la corriger explicitement dans la gestion des
   types personnalises avec un administrateur autorise, d'abord sur copie.
   Verifier les nouvelles ventes et les paniers/brouillons concernes. Ne pas
   modifier les historiques ; examiner separement tout ancien ecart de stock.
5. Tester tarif officiel, exception de ligne, refus sous cout apres remise,
   variation/marge exigeant approbation, refus/expiration/revocation, concurrence.
6. Verifier SQL : prix du type exact, historique et audit utilisateur reel.
   Recharger Produits/Stock via le bandeau ; nouvelle ligne Facturation au
   nouveau prix, lignes existantes et factures validees inchangees.

## Audit SQL applicatif

DAL cree des connexions courtes, Using ferme/libere connexions, commandes et
adapters. CreerConnexion sert aux transactions explicitement englobantes ; pas
de connexion SQL globale persistante identifiee. Pooling ADO.NET reste configure
par la chaine existante ; aucune augmentation du pool ou des timeouts metier.
Des acces directs existent (migrations, sauvegardes, configuration, API) : ils
ne sont pas tous rediriges dans cette mission, notamment pour ne pas changer
les timeouts longs justifies des sauvegardes.

Le ping de session et la synchronisation des notifications existent toutes les
5 secondes ; les moniteurs de donnees font aussi des lectures periodiques.
Cela peut contribuer a la charge multi-postes, mais ne demontre pas une fuite
ou une cause de panne reseau. Mesurer blocages/charge et durees avant refonte.

Defauts demontres : l'ancien etat SQL ne testait que Open ; HasValidConnection
reconstruisait la chaine depuis l'ecran et pouvait perdre des options explicites.
Le ping ICMP pouvait etre negatif avec SQL fonctionnel (pare-feu/instance nommee).
Ces points sont corriges. Ils ne demontrent PAS la cause des ruptures physiques.

## Surveillance et recuperation

- Sonde reelle SELECT 1, avec la chaine active et ses options de securite.
  Pool de sonde separe (ApplicationName distinct, MaxPoolSize=1), ouverture
  bornee a 5 secondes maximum, commande 3 secondes, sans retry interne.
- Le timer existant tourne a 15 secondes ; apres echecs successifs, la sonde
  attend au moins 15/30/60 secondes, plafond 60. Les controles concurrents sont
  serialises. Un changement de chaine invalide la valeur en cache.
- L'etat indique l'heure du DERNIER test ; l'infobulle donne le diagnostic et
  sa correlation. Une reussite ne garantit pas la connexion de la prochaine
  commande, les permissions metier ni la sante globale des requetes.
- Apres restauration reseau, une nouvelle sonde ouvre/verifie SQL et actualise
  l'etat. Elle ne rejoue aucune facture, paiement, mouvement ou session expiree.
- DAL observe les erreurs mais execute UNE FOIS chaque commande. Pas de retry
  de SELECT deduit par analyse de texte : un scalaire/procedure peut ecrire.
- La commande d'origine, son exception et les Using/transactions sont conserves.
  Les transactions avancees ne passent pas par les executeurs DAL ; un etat
  Broken de leurs connexions est signale, sans injecter un nouvel essai.
- Les erreurs SQL de la DAL sont classees : transport, ouverture, timeout de
  commande, verrous, authentification, base inaccessible, erreur applicative.
  Un timeout de commande n'est PAS une preuve de panne reseau.

Journaux existants : %LOCALAPPDATA%\CommercialPro\Logs. Nouveaux diagnostics SQL :
poste, operation appelante, UTC, numero/etat/classe SQL, phase, duree en ms,
ClientConnectionId et correlation. Aucun SQL, parametre, chaine, mot de passe
ou message SQL brut ajoute au diagnostic. Le signal Broken ne connait pas le
numero SQL ; sa duree mesure la vie de la connexion, pas une commande precise.
Les autres journaux preexistants ne sont pas globalement reecrits.
AuditMetierService.Echec envoie aussi l'erreur SQL au journal local AVANT sa
tentative existante d'ecriture du refus : pas de nouvelle trace de succes.
Cette frontiere ne dispose pas toujours de la phase/duree de transaction :
Phase=TRANSACTION_INDETERMINEE et DureeMs=-1 signifient inconnu, pas zero ms.
Le ResultatIncertain est conservateur pour ces timeouts ; il ne prouve ni
COMMIT ni rollback. Les chemins qui ne passent pas par Echec gardent leurs
traces existantes ; aucun instrument global n'intercepte toutes les exceptions.

## Resultat incertain : regle operateur

En cas de perte reseau/timeout apres envoi, le serveur a peut-etre COMMIT sans
que le client ait recu la reponse. Ne pas annoncer un rollback certain et ne
pas ressaisir une nouvelle facture/paiement. Retrouver la reference dans
l'historique sur SQL retabli, verifier paiement, stock et audit avec responsable.
Le parcours combine conserve sa cle OperationComptoirId et son empreinte :
une reprise explicite de la MEME intention est differente d'une nouvelle vente.
Les autres ecritures ne sont pas declarees idempotentes par cette correction.
Une session expiree doit se reconnecter ; aucune prolongation de droits locale.

## Diagnostic Windows : serveur puis clients

Ne tester les coupures que sur un reseau/base de TEST, pas en pleine activite.

### Serveur (administrateur)

1. Identifier l'instance/service SQL et le port reel dans SQL Server
   Configuration Manager et l'ERRORLOG (message Server is listening on).
   Ne pas supposer que le port est 1433.
2. Verifier TCP/IP actif pour cette instance, adresses d'ecoute et port statique
   reserve. Planifier tout redemarrage necessaire hors activite.
3. Reservation DHCP/IP fixe et DNS coherents. Preferer une adresse stable
   tcp:nom-serveur,port dans l'outil de configuration existant, pas localhost
   sur les postes distants. Ne pas publier le port SQL sur Internet.
4. Pare-feu : autoriser le port TCP exact depuis le sous-reseau applicatif,
   profil reseau correct, jamais desactiver globalement le pare-feu. Browser
   et UDP1434 utiles pour la decouverte d'instance nommee ; un port explicite
   evite cette dependance. Ne pas ouvrir UDP1434 sans besoin.
5. Verifier veilles serveur/postes, economie d'energie cartes reseau, cables,
   switch, erreurs NIC et Wi-Fi instable. Ne modifier les politiques qu'avec
   l'administrateur infrastructure ; privilegier Ethernet pour les caisses.
6. Consulter Evenements Windows System/Application et ERRORLOG autour de l'UTC
   du diagnostic : redemarrage SQL, disque, saturation, TCP reset, authentification.
7. Executer SqlReseauDiagnostic.sql en DBA pour requetes bloquees et transactions
   longues. Ne pas tuer de session ni augmenter tous les timeouts.

### Chaque poste (PowerShell)

Remplacer les exemples avec les valeurs relevees, sans mot de passe en ligne.

```powershell
$Serveur = 'SERVEUR-ERP'
$PortSql = 1433 # EXEMPLE : remplacer par le port reel
Resolve-DnsName $Serveur
Test-NetConnection -ComputerName $Serveur -Port $PortSql -InformationLevel Detailed
Get-NetAdapter | Select-Object Name, Status, LinkSpeed
Get-NetAdapterStatistics
Get-Date -Format o
# Comparer DNS puis IP relevee, sur chaque poste, pendant la fenetre incidente.
# Un ping ICMP negatif seul ne prouve pas une panne SQL.
```

Ouvrir SSMS sur le poste avec EXACTEMENT l'instance/base/authentification utilisees
par l'application ; executer SELECT 1 puis SqlReseauDiagnostic.sql si autorise.
Comparer la configuration locale reellement utilisee :
%LOCALAPPDATA%\CommercialPro\CommercialPro.exe.config. Ne jamais transmettre le
fichier complet sans masquer les secrets. Verifier aussi versions de l'EXE et
du framework, date/heure, TLS/certificat et compte SQL/Windows.
Ne pas desactiver TLS ni accepter arbitrairement tous les certificats pour
masquer un probleme de connexion. Apres changement de configuration SQL,
fermer proprement puis relancer l'application afin de recreer les repositories
qui avaient deja capture l'ancienne chaine.

Sources Microsoft :
- [Diagnostic de connectivite](https://learn.microsoft.com/en-us/troubleshoot/sql/database-engine/connect/consistent-sql-network-connectivity-issue)
- [TCP/IP et instance SQL](https://learn.microsoft.com/en-us/troubleshoot/sql/database-engine/connect/network-related-or-instance-specific-error-occurred-while-establishing-connection)
- [Pooling SqlClient](https://learn.microsoft.com/en-us/sql/connect/ado-net/sql-server-connection-pooling)

## Tests a executer sur copie Windows / SQL Server

1. Compiler WinForms avec Developer Pack 4.7.2 et vraie DLL Guna.UI2.
2. Executer AuditMetierCoreTests (regles pures) puis PanierWinFormsTests.
3. Executer AuditMetierSqlTests uniquement avec ERP_PHASE1_TEST_ENABLE=YES et
   ERP_PHASE1_TEST_CONNECTION visant une copie suffixee _Phase1_TEST, sans
   app.config de production (voir guides precedents). Nouveaux tests : couts
   paquet/11 pieces contre 11 cartons, modification autorisee et refus reel,
   SELECT1, configuration absente, timeout APRES INSERT autocommit sans retry,
   rollback d'une transaction en timeout puis sonde de retour disponible.
   Le timeout utilise une table globale temporaire au nom aleatoire, supprimee
   en fin de test. Ce banc ajoute aussi des fixtures dans la COPIE.
4. Tester 2 postes, meme produit : variation, approbation, concurrence et audit.
5. Couper seulement la liaison du poste TEST avant ouverture : UI conserve les
   saisies, etat indisponible sans crash ; restaurer, etat retabli au prochain
   passage autorise (temporisation maximale 60s, granularite timer 15s et duree
   de la sonde, hors occupation de l'UI). Compte/rôle/mode ne changent pas.
6. Couper pendant validation sur la copie, puis pres du COMMIT. Verifier la
   reference exacte : zero ou UNE facture/paiement, pas deux, audit coherent et
   aucun stock/encaissement partiel. En cas d'incertitude ne pas creer une autre
   intention. Tester la reprise explicite combinee avec MEME OperationComptoirId.
7. Simuler un blocage SQL sur copie : reconnaitre DELAI_COMMANDE/ATTENTE_VERROU,
   pas seulement reseau. Tester mauvais port, compte SQL refuse, base inaccessible.
8. Tester etat/reconnexion avec session expiree, erreurs d'impression apres
   COMMIT, conservation des factures historiques et parcours separes.

La capacite multi-postes et la stabilite physique du LAN ne sont pas garanties
par une sonde : mesures reelles, essais de coupure et surveillance necessaires.

## Compilation et resultats dans cet environnement

- AuditMetierCoreTests : 172 controles executes et reussis, sans SQL. Ils
  couvrent calculs existants/ajoutes, marge/coefficient, references principales,
  secondaires, conditionnements, classification des erreurs et temporisation.
- API Release : reussie, avertissement CS1998 preexistant dans AuthService.
- Dashboard Release : reussie.
- Bancs AuditMetierSqlTests et PanierWinFormsTests : compilation avec references
  Microsoft .NET Framework 4.7.2 temporaires reussie ; NON EXECUTES.
- WinForms complet standard : MSB3644 (targeting pack 4.7.2 absent). Les
  references temporaires ne fournissent pas la DLL Guna.UI2 du LoginForm.
- Compilation isolee des sources, hors depot, avec LoginForm temporairement
  substitue : reussie. Ce n'est PAS le build complet du produit Windows.
- SQL Server reel, schemas et donnees installees, coupures LAN, confirmation
  pendant COMMIT, UI Windows, DPI et impressions : NON EXECUTES.
- Aucun script SQL, test avec fixtures, migration ou reset lance sur une base.

WinForms NON VALIDE SOUS WINDOWS. Aucune resolution de panne de production
n'est affirmee sans les mesures serveur/postes et les essais ci-dessus.

## Fichiers concernes

- DAL/DAL.vb : execution unique, liberation existante, diagnostics.
- Forms/MainForm.vb : indicateur existant et surveillance, sans refonte UI.
- Services/SqlDiagnosticRegles.vb, SqlDiagnosticService.vb,
  SqlDisponibiliteService.vb : classification, traces et sonde.
- Services/SqlConfigurationService.vb : test reel de la chaine active.
- Services/AuditMetierService.vb : diagnostic local de l'erreur SQL avant refus.
- Services/CalculVenteService.vb : cout comparable sans arrondi intermediaire.
- Services/DelegationPrixService.vb, UI/DialogueDelegationPrix.vb : cout et
  explication de la reference, controles conserves.
- DevCommerc8ak.vbproj : inclusion des trois services.
- tests/AuditMetierCoreTests et tests/AuditMetierSqlTests : projets et programmes.
- tests/CoutsPersonnalisesDiagnostic.sql, SqlReseauDiagnostic.sql, ce guide.

Aucune migration, conversion physique, impression, caisse, reset ou sauvegarde
modifiee. Les tests SQL des guides precedents utilisent une COPIE suffixee
_Phase1_TEST et exigent les parametres initiaux de fixture (modes/delegation
desactives). Ne pas changer ces parametres en production pour lancer un test.
