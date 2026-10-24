# Administration réactive et impression thermique commune

## Diagnostic : code démontré, mesures Windows encore nécessaires

Base de comparaison : `0a91bfd`, dernier `origin/main` au début du travail.
Le commit local antérieur `d3fb6a7` et le dossier local de diagnostic sont conservés
dans la copie initiale ; ils ne sont pas inclus dans cette livraison.

Le menu Administration appelle `MainForm.Dashbord`, puis `New AdminForm`, puis
`LoadForm`. Le chronomètre historique de `LoadForm` commençait **après** `New` :
il ne mesurait donc pas le principal travail du constructeur.

Le constructeur vérifiait 13 cartes. Pour chacune, `VerifierPermission` appelait
explicitement `AssurerInfrastructure`, puis `RoleUtilisePermissions`, puis, pour
un rôle existant, `RoleAutoriseInterface`. Ces deux méthodes rappellent aussi
`AssurerInfrastructure`.

Chaque appel d'infrastructure exécute 1 batch de schéma + 5 amorçages de rôles +
15 amorçages d'interfaces = **21 commandes SQL**. Le chemin d'un rôle existant
totalise donc `13 × (3 × 21 + 2) = 845 commandes`, dont 819 contrôles/amorçages
et 26 lectures, sur le thread UI avant affichage. Les rôles absents et les
exceptions peuvent écourter ce chemin. C'est un **comptage statique vérifié**,
pas une mesure de durée, ni la preuve d'une panne réseau.

Administration ne charge pas les utilisateurs/sessions ou les grilles des modules
enfants à ce stade : ces formulaires ne sont créés qu'au clic sur leur carte.
Ils ne sont pas préchargés ni modifiés par cette optimisation.

## Optimisation

- Constructeur UI sans SQL ; une seule lecture asynchrone groupée après `Shown`.
- Même matrice des 13 cartes et mêmes filtres rôle actif/interface active.
  Aucune permission nouvelle, aucun cumul de rôles ni repli SUPERADMIN implicite.
- Aucun contrôle de schéma dans cette lecture. Les migrations existantes restent
  au démarrage ; une base non migrée produit une erreur, pas un amorçage caché.
- Cartes absentes tant que les droits ne sont pas reçus ; refus de résultat si
  utilisateur, session ou rôle ont changé pendant l'attente.
- Indicateur discret, message d'attente après 5 secondes, erreur compréhensible.
  Quitter annule la lecture ; connexions/commandes/reader libérés par `Using`.
- Les clics répétés sur Administration ne reconstruisent pas le formulaire courant.
- Aucun timeout augmenté, aucune écriture rejouée, aucune transaction métier modifiée.

### Chronométrage réel à collecter

Dans `%LOCALAPPDATA%\CommercialPro\Logs`, rechercher `Administration` et
`CreationAdmin` : construction complète dans MainForm, construction UI interne,
Shown, premier Paint, ouverture SQL, exécution du reader, lecture des droits,
remplissage UI, retour à la boucle UI et premier Paint du conteneur des cartes.
Ce dernier repère ne mesure pas le rendu GPU ou chaque contrôle enfant. Les étapes
Administration partagent un identifiant de corrélation. Les durées sont en ms
avec Stopwatch ; l'ouverture inclut l'acquisition du pool, pas nécessairement
une authentification réseau fraîche. Les horodatages du logger sont locaux.

Faire 5 ouvertures à froid puis 5 à chaud sur serveur et client, avec même compte,
même rôle, mêmes données et charge comparable. Noter premier affichage, cartes
utilisables, réactivité et médiane/max. Comparer à la version précédente sur
**copie isolée**, par chronométrage utilisateur ou profileur Visual Studio ; ne
pas réexécuter les 845 commandes en production pour simuler un benchmark.
Les nouvelles traces ne permettent pas de reconstruire des temps anciens absents.
**Aucune durée avant/après Windows n'a été mesurée ici.**

## Impression commune

`FacturePrintRenderer.CreerDocumentThermique` reçoit les données de présentation
déjà calculées. Facturation (panier), Caisse (TicketData existant), Historique
(facture persistée), et le parcours combiné qui utilisait déjà ce renderer
partagent désormais le même dessin thermique. Aucun nouveau moteur commercial.

Présentation reprise du ticket Caisse : Segoe UI 10/7,5/8,5, logo centré, entreprise,
blocs libellé/valeur alignés à droite, désignation en gras, détail et montant de
ligne sur deux colonnes, total renforcé, séparateurs graphiques et pied.
La Caisse conserve ses deux exemplaires distincts et ses boutons existants.
Son document d'aperçu est maintenant explicitement libéré.

La marge interne de référence de 3 mm est exprimée en centièmes de pouce (~12),
sans multiplication par le DPI du pilote. La zone utile est l'intersection
papier/PrintableArea, jamais une largeur minimale qui déborde du papier.
OriginAtMargins=False ; hard margins soustraites une seule fois pour passer des
coordonnées papier à l'origine imprimable du Graphics. La hauteur est mesurée
avec le même traitement des blocs que le dessin ; demande variable 420 à 5000
centièmes de pouce comme limite historique Caisse. Le pilote reste déterminant.
BeginPrint réinitialise la pagination à chaque aperçu/impression. Les blocs
articles sont reportés entiers sur la page suivante si nécessaire, avec rappel
du numéro et du type de document sur les pages de continuation. Un bloc
individuel plus haut qu'une page vide est refusé explicitement plutôt que tronqué
ou imprimé dans une boucle infinie : adapter la longueur papier du pilote.

### Sémantique des documents

- Proforma : données du panier actuel (conditionnements et prix exceptionnels
  déjà appliqués), marqueur explicite, mention NON ACQUITTÉ. Aucun bloc paiement
  ne peut être imprimé, même si des métadonnées de règlement étaient fournies.
  Aucune vente, réservation de stock ou écriture de caisse créée.
- Historique : identifiant strictement positif, entête/lignes persistés, ancien
  prix depuis `LignesFactureVente.PrixUnitaire`, jamais depuis le tarif produit.
  Ticket EN_ATTENTE : FACTURE / BROUILLON, pas un reçu payé ; ANNULÉE explicite.
  Auteur recherché depuis `CreePar`, jamais remplacé par l'utilisateur courant.
  Les règlements réels sont listés séparément, sans recalcul du dû ou conversion.
  Une ancienne `DeviseMontants` nulle/vide reste « non renseignée ».
- Caisse : l'adaptateur reçoit les montants déjà construits ; aucune logique
  d'encaissement changée. Correction de libellé démontrée : `MontantRecu` était
  fourni normalisé FC mais pouvait être étiqueté avec la devise de saisie.
  Il est maintenant affiché FC, sans conversion supplémentaire. Les sous-totaux
  et informations déjà disponibles sur TicketData ne sont pas réinventés.

La désignation produit et les coordonnées client restent celles disponibles via
les relations existantes du schéma ; aucun nouvel instantané historique n'est
inventé si elles ont changé depuis la vente. Les identités supprimées sont
signalées par leur identifiant ; pas d'identité historique fabriquée.

Le dialogue de Facturation est déplacé sans changer ses couleurs, dimensions,
choix ni événements dans `FormChoixImpressionDocument`, réutilisé par l'historique.
Annuler ne charge/imprime aucun document. Un garde empêche la réentrée depuis
l'historique. Les méthodes A4, le PDF et la branche A4 historique sont inchangés,
vérifiés contre le commit de référence. Les règles d'accès existantes restent
en place, sans nouveau privilège d'impression accordé.

## Fichiers

Sous `prototype/WinFormsVB/DevCommerc8ak` :

- `Forms/AdminForm.vb`, `Forms/MainForm.vb` : chargement/mesures/garde des clics.
- `Repositories/SuperAdminRepository.vb` : lecture groupée async seulement ajoutée.
- `Services/FacturePrintRenderer.vb`, `Services/ThermiqueMiseEnPage.vb` : présentation.
- `Forms/CaisseForm.vb` : adaptateur thermique, pas les calculs/encaissements.
- `Forms/FacturationForm.vb`, `UI/FormChoixImpressionDocument.vb` : dialogue partagé.
- `UI/FormulaireFactures.vb`, `Repositories/FactureImpressionRepository.vb` : ticket historique.
- `Repositories/LigneFactureVenteRepository.vb` : constructeur avec option explicite
  pour lecture sans DDL, défaut historique inchangé pour tous les autres appels.
- `DevCommerc8ak.vbproj` : inclusion des trois nouveaux fichiers.

Tests : `AdminThermalCoreTests`, `AdminThermalWindowsTests`,
`AdminThermalContracts.Tests.ps1`, `AdminThermalReadOnlyValidation.sql` et ce guide.
Chemins exacts des fichiers de validation ajoutes :

- `prototype/tests/AdminThermalCoreTests/AdminThermalCoreTests.vbproj`
- `prototype/tests/AdminThermalCoreTests/Program.vb`
- `prototype/tests/AdminThermalWindowsTests/AdminThermalWindowsTests.vbproj`
- `prototype/tests/AdminThermalWindowsTests/Program.vb`
- `prototype/tests/AdminThermalContracts.Tests.ps1`
- `prototype/tests/AdminThermalReadOnlyValidation.sql`
- `prototype/docs/AdministrationImpressionValidation.md`

Aucune migration ni nouvelle dépendance. La lecture des devises des règlements
suppose la migration **existante** `2026100902_TracabilitePaiements` déjà déployée.
Elle n'est pas exécutée ou modifiée par cette livraison.

## Tests disponibles

Depuis la racine du dépôt :

```powershell
dotnet run --project prototype/tests/AdminThermalCoreTests -c Release
powershell.exe -NoProfile -File prototype/tests/AdminThermalContracts.Tests.ps1
dotnet run --project prototype/tests/AuditMetierCoreTests -c Release
```

Les contrats nécessitent l'historique Git `0a91bfd`. Ils comparent les méthodes
A4, validation de facture, encaissement et conversion, vérifient le retrait des
appels synchrones d'Administration et le comptage statique. Ce ne sont pas des
tests d'intégration métier ou de mise en page physique.

Sous Windows, compiler d'abord l'ERP Release dans Visual Studio avec .NET Framework
4.7.2 Developer Pack et les références habituelles (dont Guna.UI2), puis :

```powershell
dotnet build prototype/tests/AdminThermalWindowsTests -c Release
.\prototype\tests\AdminThermalWindowsTests\bin\Release\net472\AdminThermalWindowsTests.exe
```

Si la sortie ERP diffère, passer `-p:ErpAssemblyPath=C:\Chemin\DevCommerc8ak.exe`.
Ce banc exécute le vrai plan thermique et son dessin GDI en mémoire sous Windows,
sans SQL ni imprimante ; il ne valide donc pas le pilote ou le papier physique.
Le SQL de vérification est en lecture seule, à exécuter sur une copie avec
`@FactureId` et `@NomRole` renseignés, avant/après les impressions.

### Résultats dans l'environnement de développement

- 109 contrôles de géométrie/pagination : réussis sous Linux/.NET 8.
- 43 contrats de source : réussis sous PowerShell/Linux.
- 172 contrôles métier existants : réussis sans SQL (base distante sans le commit
  local `d3fb6a7`, qui apportait 7 autres contrôles non publiés).
- API Release : compilation réussie, avertissement préexistant CS1998 dans AuthService.
- Dashboard Release : compilation réussie.
- WinForms standard : MSB3644, targeting pack 4.7.2 absent.
- Avec références externes de secours : Guna.UI2 manque dans LoginForm.
- Compilation partielle avec substitution temporaire de LoginForm et ces références :
  réussie ; ce n'est pas le build complet de production.
- Banc Windows : compilation avec références de secours réussie, **non exécuté**.
- SQL, UI Windows, durées d'ouverture réelles, imprimante et rendu physique :
  **non testés**. WinForms non validé dans cet environnement.

## Acceptation Windows / copie de base puis contrôle pilote de production

1. Compiler l'ERP complet. Vérifier cartes pour un rôle complet, partiel, sans
   droits et désactivé ; aucune nouvelle association dans RoleInterfaces.
2. Cliquer Administration plusieurs fois ; une seule lecture. Quitter pendant
   le chargement ; pas de résultat tardif affiché. Simuler une erreur SQL seulement
   sur environnement de test ; UI réactive, erreur lisible, pas de repli permissif.
3. Relever les temps serveur/client comme décrit plus haut, sans attribuer la
   lenteur restante au réseau sans mesures de cette connexion et des verrous.
4. Facturation : panier vide, un article, plusieurs, noms/conditionnements longs,
   type personnalisé, remise, prix exceptionnel. Annuler le dialogue puis vérifier
   les deux sorties proforma. Aucun paiement ou stock créé par aperçu/impression.
5. Historique : A4 comparé à la version précédente, thermique payé/partiellement
   payé/impayé/annulé, ancien prix différent du catalogue et plusieurs articles.
   Numéro/date/totaux identiques à SQL ; ne pas utiliser le panier actuel.
6. Caisse : après un encaissement de test, comparer les deux exemplaires. Refaire
   une impression uniquement ; aucune seconde écriture. Vérifier notamment la
   devise affichée des montants FC déjà normalisés lors d'une saisie USD.
7. Imprimante 80 mm réelle : logo, caractères accentués, marges, montants longs,
   passage sur plusieurs pages, longueur driver, copies, imprimante indisponible.
   La vente reste enregistrée si l'impression échoue ; aucun rejeu financier.
8. Relancer le SQL de lecture sur la même facture de test et comparer lignes,
   statut, règlements et sorties de stock. Pas de test perturbateur sur production.
9. Seulement après cette acceptation, déployer sur un poste pilote approuvé et
   contrôler l'ouverture Administration et une réimpression autorisée sans créer
   une vente artificielle. Généraliser après validation, conserver l'ancien binaire.

## Références techniques

- [OriginAtMargins et origine imprimable](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.printing.printdocument.originatmargins)
- [Unités GraphicsUnit](https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphicsunit)

Ces règles de coordonnées ne remplacent jamais les essais avec le pilote et
l'imprimante réellement déployés.
