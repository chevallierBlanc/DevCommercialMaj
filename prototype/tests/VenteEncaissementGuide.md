# Vente et Encaissement : livraison et validation

> Correctif suivant : consulter CorrectionVenteEncaissementGuide.md pour la
> section integree Modes par role, le point d'entree FacturationEtCaisseForm
> et le paiement integre qui remplacent le bouton/modal de cette livraison.

## Diagnostic et perimetre

Depart : `bee019d7cef7df914bc8ce73e0b9c47d0ba78f58`, branche main propre,
identique a origin/main avant intervention.

La collection mutable du panier etait directement liee a la grille. Apres
enregistrement SQL, le formulaire vidait cette collection puis lisait
`CurrentRow.DataBoundItem` avant de refaire la liaison. Le CurrencyManager
pouvait donc conserver la position 0 alors que la liste ne contenait plus
d'element. Le commit `470485464ff22e84496763fdae7bb2b8f6c27b7a` a introduit
cette lecture de selection avant reliaison. C'est la cause structurelle
identifiee ; l'exception Windows n'a pas ete reproduite sur cet environnement.
La grille utilise maintenant un instantane de la collection, sans cloner ni
recalculer les lignes. Un verrou de validation UI empeche la reentrance.

Attention : l'ancienne erreur pouvait survenir APRES le commit. Il faut
rechercher la facture dans l'historique avant de la ressaisir. Le diagnostic
SQL fourni liste les factures en attente et leurs paiements/mouvements. Il
ne les annule pas et ne corrige pas arbitrairement les anciennes donnees.

L'autorisation combinee etait uniquement portee par le role. Une association
utilisateur-role porte desormais une autorisation explicite, desactivee par
defaut. Les droits de plusieurs roles ne sont jamais additionnes.

Le bouton tarif pouvait reutiliser une ligne du panier : l'action catalogue
est maintenant toujours liee au produit et au type commercial selectionnes,
meme panier vide. L'action de ligne reste une exception limitee a la facture.

## Architecture retenue

- `VenteEncaissementForm` est un formulaire specialise de la saisie existante,
  par heritage, avec ses propres actions de comptoir. Il n'heberge pas deux
  formulaires Facturation/Caisse cote a cote.
- Les conversions, controles de ligne et calculs restent ceux de Facturation.
- `VenteEncaissementService` compose les noyaux transactionnels existants de
  preparation de facture et d'encaissement dans UNE transaction SQL.
- Les wrappers historiques gardent leurs transactions pour les modes separes.
- Le paiement n'est jamais enregistre lors de l'ouverture du dialogue.
- Les moteurs A4/80 mm, les conversions, le reset et la sauvegarde ne sont pas
  modifies. L'impression du dernier document recharge la facture persistee.
- `AttributionModeService` controle et audite chaque attribution par association.

## Migration additive

Fichier : `WinFormsVB/DevCommerc8ak/Migrations/2026101501_VenteEncaissement.sql`.
Prerequis : migrations precedentes appliquees, notamment 2026101301.
SchemaMigrationService enregistre et applique 2026101501 au demarrage.

Ajouts idempotents :

- UtilisateurRoles.VenteEncaissementAutorise : bit, defaut 0.
- FacturesVente.OperationComptoirId : GUID nullable.
- FacturesVente.OperationComptoirEmpreinte : empreinte de l'intention, nullable.
- FacturesVente.ValideeParUtilisateurId : nullable, confirmation de preparation.
- Index unique filtre UX_FacturesVente_OperationComptoir.
- Definition du privilege UTILISATEUR_MODES, SANS attribution automatique.

Pas de suppression, resequencement, reinitialisation, remplissage d'auteurs
historiques inconnus ni nouvelle table transactionnelle. Les anciennes
attributions de roles et permissions restent intactes. Le validateur des
anciennes factures reste inconnu. `ValideLe`, deja utilise par le circuit de
paiement, n'est pas reinterprete : la confirmation de preparation est datee
dans l'audit. Une preparation explicitement mise en attente ne confirme pas
ce validateur.

## Matrice et configuration gestionnaire

| Parcours | Parametre global | Droits du SEUL role actif | Attribution au compte pour CE role |
|---|---|---|---|
| Facturation | Mode facturation actif | FACTURIER + FACTURE_CREER | Sans nouveau grant individuel |
| Caisse | Mode caisse actif | CAISSE + ENCAISSEMENT_CREER | Sans nouveau grant individuel |
| Vente et Encaissement | Mode combine actif | Les quatre precedents + MODE_COMBINE | VenteEncaissementAutorise = 1 |

Gestion des modes desactivee : parcours historique conserve. Les roles
administratifs gardent leur portail de configuration lorsqu'aucun mode
operationnel n'est resolu ; cela ne leur accorde pas de droit commercial.

1. Dans Roles et privileges, attribuer explicitement ADMINISTRATION et
   UTILISATEUR_MODES a un responsable habilite a administrer les attributions.
   Un nom de role ADMIN/SUPERADMIN ne remplace pas ces controles.
2. Donner au role operationnel les cinq droits du combine ci-dessus.
3. Pour reprendre/modifier une vente en attente : FACTURE_MODIFIER + FACTURIER.
   Pour annuler : FACTURE_ANNULER + HISTORIQUE_FACTURES. Pour ouvrir
   l'historique : HISTORIQUE_FACTURES. Ces droits ne sont pas implicites.
4. Administration Utilisateurs : enregistrer le role principal et les roles
   autorises, selectionner Sarah puis **Vente & Encaissement**. Autoriser
   seulement l'association Sarah/CAISSIER avec un motif ; laisser
   Sarah/FACTURIER non autorisee.
5. Parametres : activer les modes globaux necessaires, puis reconnecter Sarah.
6. FACTURIER : parcours Facturation, pas de case combinee pour cette association.
   CAISSIER : case **Utiliser Vente & Encaissement** si toutes les conditions
   sont reunies. Decochee : caisse ; cochee : nouveau formulaire.

Retirer puis reattribuer un role ne ressuscite pas son ancienne autorisation
combinee. Modifier le compte sans retirer ce role conserve son autorisation.
Les controles sensibles relisent la session, le role actif et les droits en
SQL. Une revocation peut refuser une action d'une fenetre deja ouverte.
Pour actualiser integralement la navigation, se reconnecter.

## Tarifs et exceptions

Delegation globale active et permissions distinctes necessaires : consulter
les definitions existantes dans Roles et privileges. Le tarif officiel utilise
le dialogue **Modifier le prix de vente**, depuis la liste produit/type,
AVANT ajout au panier. Il ne remplace pas les prix des lignes deja saisies.
Le panier possede sa propre commande de prix exceptionnel.
Les seuils, couts comparables, approbations et conflits sont controles par le
service de delegation existant, sans accorder automatiquement ces droits.
Le responsable indique dans le dialogue est une instruction declaree dans
le motif, pas une approbation authentifiee. L'approbateur reel reste distinct.
L'ajout du code-barres a l'affichage ne change pas l'empreinte des demandes
existantes (propriete exclue de leur serialisation).

## Paiement, atomicite et limites explicites

Le nouveau dialogue traite le reglement INTEGRAL. Paiements partiels/dettes
restent dans le circuit Caisse existant : mettre en attente puis encaisser
dans ce circuit. Paiement mixte non ajoute au nouveau dialogue.
FC et USD : taux relu lors de la transaction ; monnaie rendue exprimee en FC.
Non-especes : reference et confirmation humaine du paiement effectivement
recu obligatoires, montant exact. Aucune integration de passerelle ni
confirmation automatique d'un operateur externe n'est fournie. Conserver
en attente un paiement externe non confirme.

Une intention GUID et son empreinte commerciale sont proteges par un verrou
SQL par intention et un index unique. Rejouer exactement la meme intention
retourne la facture deja payee sans nouvel encaissement ni mouvement.
Une meme cle avec un contenu different est refusee. Le commit couvre facture,
client nouveau, paiement, caisse selon le moteur existant, stock et audits.
Les notifications et impressions sont posterieures au commit.

En cas de reponse SQL incertaine pendant Commit, conserver la fenetre : le
panier et le reglement sont figes et la meme intention est verifiee au prochain
essai. Une erreur connue avant commit annule la transaction et autorise la
correction. Une erreur d'affichage apres commit signale explicitement que
la facture est deja enregistree. Ne jamais ressaisir aveuglement.

Limite : l'intention et le panier ne sont pas sauvegardes localement en cas
d'arret brutal du processus/poste. Apres un tel arret, ou si les droits/session
expirent pendant une confirmation incertaine, un responsable doit rapprocher
l'historique SQL et le reglement avant une nouvelle saisie. Ne pas annoncer
une reprise automatique apres crash ni une garantie sur un paiement externe.

CreePar demeure le createur ; ValideeParUtilisateurId identifie la confirmation
commerciale ; Paiements.PayePar demeure l'encaisseur reel. Une reprise
par une autre personne conserve le createur. Aucun auteur historique inconnu
n'est remplace par l'utilisateur courant.

## Validation reproductible sur copie Windows/SQL Server

Ne jamais executer les bancs d'integration sur CommercialMagDB de production.

1. Sauvegarder et restaurer une copie isolee `ERP_Phase1_TEST`. Verifier la
   chaine de connexion et le nom de base avant toute operation.
2. Executer la migration 2026101501 dans SSMS sur cette copie, puis une seconde
   fois. Ou lancer l'application configuree sur la copie pour l'application
   automatique, et verifier `SELECT * FROM dbo.SchemaVersion WHERE Version=2026101501;`.
3. En SQLCMD, examiner puis executer `AuditPrixModesMigrationCopie.sql` : le
   script applique les migrations deux fois et compare les donnees preservees.
4. Executer `VenteEncaissementValidation.sql`, non destructif : colonnes/index,
   matrice effective par utilisateur-role, auteurs et ventes en attente.
5. Pour le banc SQL, utiliser une copie fraiche dont GestionModesActive et
   DelegationPrixActive sont desactives comme demande par ses preconditions.
   Le banc cree des fixtures et un trigger de panne uniquement sur la copie.

```powershell
$env:ERP_PHASE1_TEST_ENABLE = 'YES'
$env:ERP_PHASE1_TEST_CONNECTION = 'Server=localhost;Database=ERP_Phase1_TEST;Integrated Security=True;TrustServerCertificate=True'
msbuild prototype\tests\AuditMetierSqlTests\AuditMetierSqlTests.vbproj /restore /p:Configuration=Release
& prototype\tests\AuditMetierSqlTests\bin\Release\net472\AuditMetierSqlTests.exe
msbuild prototype\tests\PanierWinFormsTests\PanierWinFormsTests.vbproj /restore /p:Configuration=Release
& prototype\tests\PanierWinFormsTests\bin\Release\net472\PanierWinFormsTests.exe
```

Le banc SQL couvre refus, concurrence d'edition, panne d'audit, Sarah,
preservation/retrait d'attribution, rejeu identique/concurrent du comptoir,
stock insuffisant sans facture residuelle et auteurs distincts. Il n'a PAS
ete execute ici. Le banc UI exerce 40 cycles reels de liaison DataGridView ;
compile ici, execution Windows obligatoire.

## Compilation Windows

Visual Studio 2022 : workload Developpement Desktop .NET, VB.NET, outils
MSBuild et Developer/Targeting Pack .NET Framework 4.7.2 ; restaurer NuGet.
Fournir la DLL Guna.UI2 compatible et licenciee utilisee par l'installation.
Ne pas la remplacer par un faux composant ou committer une DLL inconnue.

```powershell
msbuild prototype\WinFormsVB\DevCommerc8ak\DevCommerc8ak.vbproj /restore /p:Configuration=Release /p:GunaUI2AssemblyPath="C:\Libs\Guna.UI2.dll"
```

Sous Linux, compilation WinForms normale bloquee par MSB3644. Avec les
references officielles net472 disponibles temporairement, la compilation
reste bloquee sur Guna2TextBox absent (LoginForm). WinForms NON VALIDE dans
cet environnement. Aucun apercu/ticket n'a ete visuellement valide ici.

## Checklist fonctionnelle Windows obligatoire

1. Panier vide refuse ; une ligne, plusieurs lignes, suppression de derniere
   ligne, changement de quantite/selection, validation puis nouvelle facture :
   aucune erreur CurrencyManager ni perte de ligne.
2. Double clic de validation, evenements de rafraichissement et stock derniere
   unite : une facture et un seul mouvement logique.
3. Facturier seul et caissier seul : parcours historiques identiques, avec
   gestion des modes active puis desactivee.
4. Sarah FACTURIER puis CAISSIER : case combinee seulement pour CAISSIER ;
   case decochee = caisse, cochee = Vente et Encaissement.
5. Global combine desactive, attribution absente, role retire ou permission
   revoquee : acces refuse sans cumul des autres roles.
6. Mettre en attente, reprendre, abandon explicite, annuler avec motif ; quitter
   avec panier non enregistre propose sauvegarde ou abandon, jamais paiement.
7. Ouverture du paiement puis Annuler : aucune facture ni paiement nouveau.
8. Especes 60 000 pour 56 000 FC : affectation 56 000, rendu 4 000, recette non
   gonflee ; tester USD/taux change et grands montants/arrondis.
9. Paiement externe sans confirmation/reference refuse ; confirmation manuelle
   du montant exact seulement ; aucun statut externe invente.
10. Couper la connexion avant commit puis lors du commit ; verifier rollback
    ou etat incertain, bouton de verification, meme cle, aucune ressaisie.
11. Deux postes sur le meme stock : stock coherent, pas de verrou global sur
    l'etablissement ; SQL parallele et erreur d'audit a exercer sur copie.
12. Sarah cree/valide/encaisse : trois identites Sarah distinctement tracees.
    Andy cree/valide puis Marie encaisse : Andy/Andy/Marie, anciens NULL preserves.
13. Tarif officiel avant panier : modification autorisee, concurrente refusee,
    demande/approbation/refus ; panier deja constitue non modifie silencieusement.
14. Prix exceptionnel de ligne : demande liee au contexte, modification des
    conditions recontrolee, catalogue et anciennes factures inchanges.
15. Ticket 80 mm/A4 depuis donnees persistees, historique et PDF historiques :
    statuts, conditionnements, totaux, logo et imprimantes reels a verifier.
16. DPI 100/125/150 %, petite resolution, code-barres, libelles longs,
    defilement des actions, fermeture/reconnexion et journal append-only.

Les builds et tests purs ne prouvent ni le bon fonctionnement SQL reel, ni
la qualite visuelle, ni les performances sous charge. La validation finale
Windows/SQL sur copie reste une condition de deploiement en production.

## Controles effectivement executes lors de cette livraison

- AuditMetierCoreTests Release : 117 controles passes, sans SQL.
- API Release : compilation reussie, 0 avertissement / 0 erreur.
- Dashboard Release : compilation reussie, 0 avertissement / 0 erreur.
- AuditMetierSqlTests net472 : compilation reussie, PAS d'execution SQL.
- PanierWinFormsTests net472 : compilation reussie, PAS d'execution Windows.
- Controle de compilation isole des autres formulaires : reussi en excluant
  LoginForm et en utilisant un placeholder de Form uniquement sous /tmp,
  hors depot et hors livrable. Ce controle ne valide ni le vrai login/Guna,
  ni le binaire de production ; il n'a pas ete execute.
- WinForms complet : NON VALIDE ; pack net472 absent de l'installation Linux
  standard (MSB3644). L'essai avec references temporaires rencontre aussi le
  task host GenerateResource puis, avec ajustement du host, Guna.UI2 manquant.
- Migrations, scripts SQL, rollback/concurrence SQL, impressions et parcours
  utilisateur : NON EXECUTES sous SQL Server/Windows dans cet environnement.
- Aucun reset ni acces a une base de production. Aucune dependance binaire
  nouvelle ajoutee au depot. Revue de diff et git diff --check effectuees.

## Inventaire des fichiers de cette livraison

Sous `WinFormsVB/DevCommerc8ak` :

- `DevCommerc8ak.vbproj` : inclusion du nouveau formulaire, services et ressource.
- `Forms/FacturationForm.vb` : liaison panier, nettoyage apres commit, hooks du
  parcours specialise et separation des commandes de prix.
- `Forms/LoginForm.vb`, `Forms/MainForm.vb` : choix et routage du mode.
- `Forms/VenteEncaissementForm.vb` : nouveau parcours unique et reglement.
- `Repositories/UtilisateurRepository.vb`, `Services/UtilisateurService.vb` :
  edition atomique des associations, pas de resurrection d'attribution.
- `Services/AttributionModeService.vb` : nouveau controle individuel par role.
- `Services/VenteEncaissementService.vb` : nouvelle composition transactionnelle.
- `Services/FactureOperationService.vb`, `Services/FacturationService.vb` :
  noyaux reutilisables sous transaction existante et auteurs distincts.
- `Services/ModeTravailRegles.vb`, `Services/ModeTravailService.vb` : resolution,
  preference ergonomique et diagnostic sans ajout de permissions.
- `Services/DelegationPrixRegles.vb`, `Services/DelegationPrixService.vb` :
  instantane panier et code-barres d'affichage exclu de l'empreinte tarifaire.
- `Services/AuditPresentationService.vb` : libelle du validateur commercial.
- `Services/SchemaMigrationService.vb`,
  `Migrations/2026101501_VenteEncaissement.sql` : migration additive embarquee.
- `UI/DialogueDelegationPrix.vb`, `UI/DialogueModeTravail.vb` : perimetres de prix
  distincts et case d'adhesion au parcours combine.
- `UI/CartesSecuriteAutorisations.vb`, `UI/FormulaireSuperAdminRoles.vb`,
  `UI/FormulaireUtilisateurs.vb` : libelles d'eligibilite et attribution par role.

Sous `tests` :

- `AuditMetierCoreTests/Program.vb` : tests purs de liaison et modes.
- `AuditMetierSqlTests/AuditMetierSqlTests.vbproj`,
  `AuditMetierSqlTests/Program.vb` : vrais services, fixtures SQL et concurrence.
- `AuditPrixModesMigrationCopie.sql` : double application de migration.
- `PanierWinFormsTests/PanierWinFormsTests.vbproj`,
  `PanierWinFormsTests/Program.vb` : nouveau banc DataGridView Windows sans SQL.
- `VenteEncaissementValidation.sql` : nouveau diagnostic non destructif.
- `VenteEncaissementGuide.md` : ce rapport et les procedures de validation.
