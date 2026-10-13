# Phase 2 : integration operationnelle, configuration et validation

## Etat initial et diagnostic

Depart : main propre, HEAD et origin/main =
`7e9abb443a91a88531706e8f92d6363eb9b793ae`, apres git fetch.
Aucune connexion a CommercialMagDB, aucune reinitialisation et aucun effacement.

Constats verifies dans le code, pas dans une instance SQL Server de production :

- Le login choisissait deja un role puis un mode exclusivement autorise pour
  ce role. Les controles SQL des services existaient. Ce parcours est conserve.
- Le mode combine ouvrait uniquement FacturationForm et deux menus separes.
  Aucun passage de l'ID de la nouvelle facture vers la caisse n'etait relie.
- Les trois boutons de Parametres ouvraient des configurations independantes,
  hors du bouton principal Enregistrer. Ils ne fournissaient pas de vue integree.
- Les demandes en consultation etaient plafonnees a 500, sans pagination ni
  filtre SQL. L'approbation concernait uniquement le tarif officiel.
- Une exception hors seuil sur une facture exigeait un changement de tarif
  officiel : absence d'approbation propre a l'article.
- Le service de prix utilisait le nom du type. Un type personnalise homonyme
  d'un standard pouvait viser la colonne du prix standard. Le mode COEFFICIENT
  etait compare de facon sensible a la casse, contrairement au moteur existant.
- Une modification de tarif delegue ne mettait pas a jour AppDataVersions.
  Les autres postes ne recevaient pas le rafraichissement habituel du catalogue.
- La sauvegarde des privileges journalisait apres commit, sans differences
  structurees et sans controle de version pour un editeur concurrent.
- La sortie du poste ne verifiait pas le panier non enregistre.

Les causes de configuration restent distinctes des bugs : disponibilite globale,
droit de mode, droit d'ecran et droit d'action sont quatre controles differents.
Activer un parametre ne doit jamais accorder un privilege. Le diagnostic SQL
fourni permet de verifier la base utilisee, les versions, les colonnes, les
droits incomplets, les couts manquants et les noms commerciaux reserves.
L'etat reel des migrations sur votre poste Windows n'a pas ete presume.

## Utilisation par le gestionnaire

Ouvrir Parametres / Securite et autorisations. Trois cartes sont integrees :

1. Modes de travail : gestion globale et disponibilite Facturation, Caisse,
   Facturation et caisse. Les choix sont presentes dans deux colonnes si la
   largeur le permet, sinon verticalement avec defilement.
2. Delegation et controle des prix : activation, demandes, immediat selon droit,
   variation, marge sans approbation, sous-cout et duree des nouvelles demandes
   de 1 a 168 heures. La valeur initiale reste 24 heures.
3. Demandes et approbations : compteurs, recherche produit/demandeur/type,
   filtre de statut, pages de 25, decisions selon droits, double-clic pour details.

Le bouton Enregistrer la securite du pied du formulaire sauvegarde les cartes
modifiees dans UNE transaction, avec motif et confirmation. Le bouton principal
ne sauvegarde pas simultanement les modifications d'autres onglets : son contexte
est indique. Les autres parametres, imprimantes et sauvegardes gardent leur
fonctionnement. Une carte non chargee ou non autorisee ne remplace jamais la
configuration par des valeurs par defaut. Les erreurs sont affichees.

Les consultations et sauvegardes de ces cartes utilisent des taches asynchrones.
La fermeture/navigation est refusee pendant ces operations pour conserver le
contexte de l'acteur. Une decision ne peut pas continuer sous le compte suivant.

## Attribution des droits : jamais automatique

Dans Roles et privileges, selectionner le ROLE que l'employe choisira au login.
Attribuer les libelles correspondants aux codes ci-dessous, puis enregistrer
avec motif. Ne pas donner Administration/gestion des produits uniquement pour
permettre une modification de prix. Les roles existants ne sont ni effaces ni
fusionnes, et les liens multi-roles ne sont pas modifies par la migration.

- Configuration modes : PARAMETRES + PARAMETRES_SECURITE.
- Configuration prix : PARAMETRES + PARAMETRES_PRIX.
- Responsable des demandes : PARAMETRES + PRIX_CONSULTER + PRIX_APPROUVER
  et/ou PRIX_REFUSER ; PRIX_SOUS_COUT si l'entreprise permet cette exception.
- Facturation : FACTURIER + FACTURE_CREER ; FACTURE_MODIFIER et FACTURE_ANNULER
  uniquement si souhaites ; HISTORIQUE_FACTURES pour l'historique autorise.
- Caisse : CAISSE + ENCAISSEMENT_CREER.
- Mode facturation : MODE_FACTURATION et ecran FACTURIER.
- Mode caisse : MODE_CAISSE et ecran CAISSE.
- Mode combine : MODE_COMBINE, les DEUX ecrans et les actions necessaires du
  MEME role actif. Deux roles distincts ne sont jamais additionnes.
- Lire le dialogue de prix : FACTURIER + PRIX_CONSULTER.
- Tarif officiel : PRIX_TARIF_MODIFIER pour immediat, PRIX_DEMANDER pour demande.
- Prix de l'article : PRIX_FACTURE_EXCEPTION pour direct dans seuils,
  PRIX_DEMANDER_FACTURE pour demande puis application approuvee.

Un editeur de privileges devenu obsolete est refuse par rowversion ; recharger
avant une nouvelle decision. La derniere voie active de gestion des roles ne
peut pas etre retiree par cette sauvegarde. Le service REFUSE l'operation ;
il ne restaure pas automatiquement une permission.

Gestion des modes desactivee signifie fonctionnement classique selon les droits
existants, pas retrait de droits. Pour suspendre le combine, desactiver sa
disponibilite en laissant la gestion active. Prevenir les employes et faire
enregistrer leurs brouillons avant un changement global : une session dont le
contexte n'est plus autorise devra se reconnecter. Aucun SID distant n'est ferme.
ADMIN/SUPERADMIN conservent leur parcours. Les autres roles administratifs ou
techniques sans attribution explicite de mode conservent aussi ce parcours.

## Poste Facturation et caisse

Apres authentification, choisir un seul role, puis un seul mode si plusieurs
sont proposes. Un mode unique est automatique. Le mode choisi est affiche dans
MainForm et stocke dans la session SQL, puis recontrole lors des actions sensibles.

Le portail Facturation et caisse est un espace commun a deux etapes :

1. Client et facture : reutilisation du formulaire de facturation existant.
2. Encaissement et ticket : reutilisation du formulaire de caisse existant.

Apres enregistrement, le portail transmet l'ID exact de la facture a la caisse
et selectionne cette facture. L'enregistrement reste un brouillon EN_ATTENTE :
ce n'est PAS un paiement. Le caissier saisit montant/devise/moyen puis effectue
l'encaissement normal. Si la facture est deja payee par un autre poste, aucune
autre facture n'est encaissee par substitution. Les verrous, stocks et montants
restent traites par FacturationService. Les impressions existantes sont reutilisees.

QUITTER LE MODE COMBINE ET SE RECONNECTER controle le paiement en cours,
puis propose sauvegarde du brouillon, abandon explicite ou annulation de sortie.
Un echec de sauvegarde empeche la sortie, sans effacer le panier. Le mode est
clos avec son audit dans une transaction pour le SID courant uniquement.
La reconnexion remet un contexte de role/mode unique, sans cumul.
Une panne SQL empechant une sortie propre est signalee ; aucune fausse trace
de fermeture reussie n'est produite. Les modes separes conservent leurs vues.

## Prix depuis un article de facture

Selectionner un article puis DEMANDER UNE MODIFICATION DE PRIX POUR L'ARTICLE
SELECTIONNE. Le dialogue distingue clairement tarif officiel et article de
facture, montre prix actuel, prix propose, variation et marge apres remise.

Une exception directe exige permission, motif, parametres et seuils. Hors seuil,
DEMANDER POUR CET ARTICLE garde le tarif et le panier intacts, en EN_ATTENTE.
Le responsable ouvre la carte des demandes, voit le perimetre, le cout connu,
les impacts et le motif, puis approuve/refuse avec motif. Auto-approbation refusee.

APPLIQUER L'APPROBATION recharge la demande exacte pour le demandeur, article,
type, prix, quantite, sous-total et remise. Le panier n'est pas change silencieusement.
L'approbation est consommee UNE fois lors de la transaction facture, avec audit.
Une panne d'audit annule facture ET consommation. Une autre facture ne peut
pas reutiliser le contexte ; il est unique dans les articles et conserve dans
les demandes consommees. Un brouillon recharge conserve le contexte d'origine.
Modifier ensuite panier/remise/quantite exige une nouvelle approbation.

Les couts sont compares dans la meme quantite physique, par les services existants.
Le controle apres remise est une verification d'autorisation, pas un nouveau
calcul de facture. Les arrondis de validation acceptent les representations
monetaires existantes a deux decimales, sans reecrire les valeurs historiques.
Les nouvelles quantites et montants invalides sont refuses cote service.

Le tarif officiel modifie concerne les nouvelles saisies. Il actualise le
compteur PRODUITS dans sa transaction, donc le catalogue des autres postes,
mais jamais leurs paniers deja saisis. La sauvegarde recontrole les donnees.
Un type personnalise est identifie par son ID, pas uniquement son nom ;
0 designe un standard et NULL une ancienne identification non disponible.
Les anciennes identifications ne sont pas devinees/backfillees. En cas de
nom ancien ambigu, la delegation demande une selection non ambigue.
Le mode FIXE/COEFFICIENT est conserve, y compris sa casse historique.

Un cout inconnu interdit la delegation, sans fabriquer une marge. Le tarif
normal deja configure garde sa compatibilite. Pour les couts connus, la
politique active controle le sous-cout des nouvelles saisies/remises sensibles.
Un changement de conditionnement depuis un brouillon peut imposer une nouvelle
ligne/document pour une nouvelle approbation : la quantite historique n'est
jamais corrigee automatiquement pour satisfaire une approbation.

## Migration et verification sur copie uniquement

1. Sauvegarder et restaurer une COPIE JETABLE ERP_Phase1_TEST, de preference
   sur une instance de test distincte. Ne pas pointer la nouvelle application
   vers la production pour ces essais.
2. Visual Studio : workload .NET desktop, Developer Pack 4.7.2, DLL Guna.UI2
   autorisee compatible. Compiler avec le chemin de dependance existant :

```powershell
$env:GunaUI2AssemblyPath = 'D:\ERPDependencies\Guna.UI2.dll'
MSBuild .\prototype\WinFormsVB\DevCommerc8ak.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
```

3. SchemaMigrationService applique 2026101301 automatiquement au demarrage
   apres les migrations existantes. Extensions additives : duree dans Parametres,
   rowversion Roles, contexte et consommation des demandes, identifiant tarif,
   metadonnees de lignes, index demandes/contexte. AppDataVersions est creee
   seulement si absente, selon sa structure existante. Aucun grant automatique,
   aucun changement de prix/utilisateur/stock ancien. Autre rowversion Roles
   deja presente : refus explicite, cartographie a faire, pas remplacement.
4. Dans SSMS sur la copie, verifier :

```sql
SELECT DB_NAME() AS BaseVerifiee;
SELECT * FROM dbo.SchemaVersion WHERE Version=2026101301;
SELECT name,TYPE_NAME(system_type_id) AS TypeColonne
FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Roles');
```

5. Executer Phase2IntegrationValidation.sql : lecture seule, diagnostics de
   schema/configuration/droits/identifiants/contextes. Son marqueur ne prouve
   PAS l'execution d'un workflow. Pour idempotence/conservation, depuis
   prototype/tests avec SQLCMD :

```powershell
sqlcmd -S .\ERPCOMMERCIAL_TEST -E -b -i AuditPrixModesMigrationCopie.sql
```

   Le script applique deux fois les migrations sur ERP_Phase1_TEST et compare
   les anciennes donnees. Aucun reset. Ne pas lancer le banc SQL sur une base
   de production, meme en lui donnant un nom se terminant par _Phase1_TEST.

6. Banc SQL reproductible, sur copie migree avec flags modes/delegation
   desactives initialement :

```powershell
dotnet run --project .\prototype\tests\AuditMetierCoreTests -c Release
$env:ERP_PHASE1_TEST_ENABLE = 'YES'
$env:ERP_PHASE1_TEST_CONNECTION = 'Server=.\ERPCOMMERCIAL_TEST;Database=ERP_Phase1_TEST;Integrated Security=True;TrustServerCertificate=True'
MSBuild .\prototype\tests\AuditMetierSqlTests\AuditMetierSqlTests.vbproj /restore /p:Configuration=Release
& .\prototype\tests\AuditMetierSqlTests\bin\Release\net472\AuditMetierSqlTests.exe
```

   Le banc cree des fixtures et des triggers temporaires d'echec sur LA COPIE,
   garde les fixtures et restaure les parametres. Il n'efface pas la base.

## Checklist Windows et SQL Server

1. Migration deux fois : versions/colonnes/index presents, anciennes donnees
   conservees, aucun privilege sensible ajoute a un role.
2. Cartes a 1024x768, 1366x768 et 1920x1080, DPI 100/125/150 % : pas de
   controle inaccessible, deux colonnes ou defilement, erreurs visibles.
3. Sauvegarder modes/prix ensemble avec motif ; fermer/reouvrir : identiques.
   Deux gestionnaires edits concurrents : refus de l'editeur obsolete.
   Simuler panne audit sur copie : aucune demi-configuration sauvegardee.
4. Retirer permission, redemarrer : non retablie. Sauvegarde privileges avec
   motif et differences exactes ; ancien token rowversion refuse. Dernier
   acces de gestion des roles protege sans grant automatique.
5. Role uniquement MODE_COMBINE sans ecrans : refuse. Role avec deux ecrans
   sans ce mode : option invisible. Ajouter explicitement mode et actions :
   portail unifie accessible, role/mode visibles.
6. Choisir FACTURATION puis CAISSE via reconnexion : vues et restrictions
   historiques preservees, aucune accumulation de droits.
7. Combine : produit 10 FC, quantite 2, stock suffisant. Enregistrer :
   facture 20 FC selectionnee automatiquement, aucun paiement avant action.
   Recu 25 FC : net 20, rendu 5, un paiement, stock decru de 2 bases.
8. USD : facture 56 000 FC, taux 2 000, recu 30 USD : normalise 60 000 FC,
   rendu 4 000 FC, net 56 000 FC. Verifier les valeurs persistees et l'audit.
9. Deux postes, facturier et caissier : meme facture en paiement simultane,
   un seul paiement et une seule sortie, autre poste informe. Le depart d'un
   combine ne ferme pas les SID des autres postes.
10. Quitter avec panier : Annuler conserve ; Oui sauvegarde brouillon ; Non
    abandon explicite. Echec SQL de sauvegarde : reste ouvert. Pendant paiement
    ou decision asynchrone : fermeture refusee. Reconnexion en mode separe.
11. Prix officiel piece 20, cout comparable 10, variation 10 % : 21 direct
    avec droit/motif ; sans droit ou sans motif : refus. Autre poste voit le
    catalogue actualise, panier existant ne change pas.
12. Article a 27 : demande EN_ATTENTE, prix officiel 20 conserve. Responsable
    autre utilisateur approuve ; demandeur applique puis enregistre : article
    27, tarif 20 intact. Reutilisation autre article/document/quantite : refus.
13. Changer remise/panier apres demande : application refusee. Apres sauvegarde
    approuvee, changer remise sans nouvelle approbation : refuse. Cout 20,
    prix 21 et remise 10 % : sous-cout detecte apres remise, pas seulement brut.
14. Refus motive, annulation propre demandeur, expiration (duree de 1 heure
    sur copie ou fixture SQL), auto-approbation et double decision refuses.
15. Type personnalise homonyme piece et mode coefficient en casse historique :
    selection par ID, seul ce tarif change, standard intact. Coefficient non
    representable exactement : refuse, jamais bascule en FIXE silencieusement.
16. Montants fractionnaires : 0,5 x 100,55, plusieurs articles a fractions,
    rechargement brouillon : tester les representations monetaires arrondies.
17. Injection d'echec d'audit : aucun prix, facture, paiement ou consommation
    partiellement valide. Refaire l'action apres correction de la panne.
18. Interruption reseau depuis un POSTE DE TEST, base et instance de TEST :
    apres reconnexion, relire facture/paiement avant de retenter. Une coupure
    autour de COMMIT peut perdre l'accuse de reception ; resultat attendu est
    soit rollback complet, soit commit complet avec audit, jamais moitie d'etat.
    Un commit confirme ne doit pas produire un deuxieme paiement au retry.
    Ne pas arreter un serveur hebergeant une base de production pour ce test.
19. Audit : acteurs reels, role, mode, poste, SID, UTC, documents et avant/apres.
    Creation et paiement distincts, refus/echec distincts des succes.
20. Regression : vieux produits/anciens comptes, factures payees et annulees,
    A4/PDF/ticket 80 mm, modes separes, quantites en base, caisse journaliere.
    Aucune execution de reset commercial dans cette mission.

## Limites et non-regression

Validations executees ici : 66 tests metier sans SQL reussis, banc net472
compile (zero erreur/avertissement), API et Dashboard Release compiles,
git diff --check reussi. Un build API precedent a aussi signale l'avertissement
CS1998 deja existant dans AuthService ; les builds incrementaux finaux sont propres.

Fichiers crees : FacturationEtCaisseForm.vb, CartesSecuriteAutorisations.vb,
ISortieSessionControlee.vb, 2026101301_IntegrationPrixModes.sql,
Phase2IntegrationValidation.sql et ce guide.

Fichiers modifies : DevCommerc8ak.vbproj, LigneFactureVente.vb,
CaisseForm.vb, FacturationForm.vb, MainForm.vb,
LigneFactureVenteRepository.vb, SuperAdminRepository.vb,
AppDataVersionService.vb, AuditPresentationService.vb,
ConfigurationModesService.vb, DelegationPrixRegles.vb,
DelegationPrixService.vb, FactureOperationService.vb,
ModeTravailService.vb, SchemaMigrationService.vb, SuperAdminService.vb,
DialogueDelegationPrix.vb, FormulaireParametres.vb,
FormulaireSuperAdminRoles.vb, Program.vb des deux bancs,
AuditMetierSqlTests.vbproj, AuditPrixModesMigrationCopie.sql,
AuditPrixModesPhase2.md. Aucun fichier de moteur d'impression, conversion,
inventaire, reset commercial, API, authentification ou sauvegarde modifie.

SQL Server, les migrations et les tests d'integration SQL n'ont pas ete executes
dans cet environnement Linux. WinForms complet et mise en page NON VALIDES
sous Windows : targeting pack systeme absent ; avec references 4.7.2 fournies,
Guna.UI2 manque encore. Le banc SQL compilable inclut les services et cartes
reels, mais sa compilation n'est pas un test de workflow.

Les calculs commerciaux, conversions, stocks et moteurs d'impression ne sont
pas remplaces. Les validations service refusent les donnees incoherentes ;
elles ne corrigent pas silencieusement les historiques. Les vieux identifiants
tarif NULL restent inconnus. Aucun compte/mot de passe/role attribue n'est efface.

Les operations heritees de facturation/caisse restent synchrones ; leur latence
doit etre mesuree sous Windows. Pas de promesse de milliers d'utilisateurs.
La pagination borne les donnees chargees, mais les compteurs et recherches
peuvent scanner des index : examiner plans/IO sur copie chargee. Pas de nouveau
moteur de paiement partiel, mixte, gratuit ou de devise internationale.

Une entreprise par base, configuration singleton, DECIMAL, Unicode et UTC
conservent un cadre extensible sans inventer du multi-tenant. Les montants de
prix restent en FC selon le schema actuel ; le paiement USD reutilise son taux.
Les privileges SQL et l'append-only du journal doivent etre verifies avec le
principal applicatif reel ; aucun service ne garantit l'immutabilite contre
un administrateur SQL privilegie. Les autres chemins administratifs herites
non modifies ne sont pas annonces comme entierement audites par cette livraison.

La cartographie du reset n'est pas modifiee. Les demandes deja conservees ont
des references de facture opaques, pas de nouvelle FK vers les transactions a
purger. Leur politique de retention lors d'un reset est un chantier distinct.
