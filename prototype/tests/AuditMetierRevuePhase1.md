# Revue technique de la phase 1

Référence examinée : `0d0d05320e38af39b221797e0587bb8fcf0ffc6e`.
Les 32 fichiers du commit sont inventoriés dans `AuditMetierPhase1.md`.
Le dépôt initial est propre, attaché à main, HEAD identique à origin/main.

## Constats et corrections ciblées

1. **Brouillon : remise effacée et client sans téléphone dupliqué.**
   `ChargerFacturePourEdition` vidait txtRemise et ignorait ClientId ; un
   enregistrement inchangé pouvait donc supprimer la remise et créer un
   nouveau client. Le chargement relit désormais numéro, client, remise et
   rowversion ensemble depuis SQL. Le client inchangé garde son ID.
   Une remise et un sous-total inchangés gardent le montant décimal exact.
   Les documents avec taxe ou remise par article sont refusés par ce chargement
   d'édition : cet écran ne peut pas les représenter et les effaçait à la sauvegarde.
   Leur consultation/impression reste inchangée, aucun calcul fiscal n'est ajouté.
   Ce problème était déjà présent avant la phase 1 ; sa correction évite
   que le nouveau chemin transactionnel continue de le reproduire.
2. **Références d'audit vides dans les chemins de service historiques.**
   AjouterLigne et ValiderPaiement récupèrent maintenant le numéro depuis
   le snapshot SQL. Les traces de refus retrouvent le numéro réel lorsque
   la facture existe ; une création refusée garde sa référence demandée.
3. **Succès de procédure supposé / notifications dépendantes de NOCOUNT.**
   ValiderPaiement vérifie désormais PAYEE et un paiement réellement enregistré
   pour l'acteur/montant avant d'auditer SUCCES. Les snapshots sont conservés.
   Les notifications suivent le commit, même si ExecuteNonQuery renvoie -1.
   La procédure et ses calculs ne sont pas modifiés.
4. **Stock vérifié ligne par ligne, pas par produit.**
   Avec deux lignes du même produit, chacune pouvait tenir dans le stock
   alors que leur somme le dépassait. La validation agrège désormais les
   besoins en QuantiteBase et verrouille par ProduitId croissant. Les écritures
   StockSortie restent celles des lignes d'origine, sans nouvelle conversion.
5. **Dépendance WinForms non portable.**
   Guna.UI2 est référencé par un chemin absolu sur le poste du développeur,
   sans DLL ni package de restauration dans le dépôt. Une propriété facultative
   GunaUI2AssemblyPath permet d'utiliser la DLL autorisée sur un autre poste,
   sans supprimer le chemin historique ni embarquer de binaire.

## Contrôles statiques satisfaits, sans exécution SQL

- La migration n'efface ni comptes, ni rôles, ni permissions, ni anciennes traces.
  L'amorçage ne réattribue pas les droits des rôles existants vides ; les nouvelles
  actions héritent une seule fois des associations d'écran présentes.
- Les comptes multirôles utilisent seulement RoleIdActif de la session SQL.
  Les services relisent les droits avec des verrous conservés jusqu'au commit.
- Annulation : verrou de l'en-tête, EN_ATTENTE obligatoire, motif, changement
  de statut et audit dans la même transaction. Ni paiement ni sortie créée.
- Modification : rowversion, verrou en-tête, client/lignes/en-tête/audit communs.
  Une facture payée/annulée n'est pas éditable par ce service.
- Encaissement : verrou UPDLOCK/HOLDLOCK en-tête, transition conditionnelle
  EN_ATTENTE -> PAYEE, paiement/sorties/audit avant le commit.
- Régularisation : calculs conservés, motif <=250 caractères, compte réel,
  autorisation CAISSE_REGULARISER + écran, historique et audit atomiques.
- Audit : acteur et rôle chargés depuis Utilisateurs/Roles/UtilisateurSessions,
  provenance conservée, UTC pour JournalAudit, consultation en lecture seule.
  Aucun succès métier n'est écrit après rollback ; les refus des chemins
  principaux sont écrits séparément, avec REFUS/ECHEC.
- Historiques de prix nouveaux : ID de l'acteur connecté, pas la constante 1.
  Les anciennes lignes ne sont pas réattribuées.

Ces constats de code ne prouvent pas le comportement de la base réelle,
de ses triggers personnalisés ou de ses droits SQL.

## Compilation Windows exacte

1. Installer Visual Studio avec la charge **Développement Desktop .NET**,
   le SDK et le targeting pack **.NET Framework 4.7.2**. Installer aussi le
   SDK .NET 8 si le banc de tests SDK doit être compilé avec dotnet.
2. Obtenir la distribution Guna.UI2 autorisée/licenciée compatible avec
   l'installation existante. Le projet attend l'assembly Guna.UI2 et les
   types Guna.UI2.WinForms.Guna2TextBox. Ne pas remplacer cette DLL par
   un fichier non vérifié provenant du chemin de téléchargement historique.
3. Depuis **Developer PowerShell for Visual Studio**, à la racine du dépôt :

```powershell
$env:GunaUI2AssemblyPath = 'D:\ERPDependencies\Guna.UI2.dll'
Test-Path $env:GunaUI2AssemblyPath
MSBuild .\prototype\WinFormsVB\DevCommerc8ak.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
```

4. Pour travailler dans l'IDE, ouvrir la solution dans un nouveau processus
   Visual Studio lancé depuis ce terminal :

```powershell
devenv .\prototype\WinFormsVB\DevCommerc8ak.sln
```

5. Vérifier que Références/Guna.UI2 n'a plus de triangle jaune et que
   Copy Local est vrai ; vérifier que la DLL est présente avec l'exécutable.
   Ne pas retargeter le projet et ne pas créer de contrôles factices pour
   contourner BC30002. Configurer l'application UNIQUEMENT sur la copie SQL.

## Validation de migration sur copie uniquement

1. Sauvegarder puis restaurer la base sous **ERP_Phase1_TEST**, sur une instance
   de test isolée. Arrêter les applications/synchronisations sur cette copie.
   Ne pas configurer la production avec l'exécutable de test.
2. La copie doit disposer des migrations précédentes et de JournalAudit.
   Ne pas supprimer un marqueur SchemaVersion : utiliser une copie antérieure
   à la phase 1 pour tester une première application réelle ; une copie déjà
   migrée ne teste que la répétition/idempotence.
3. Depuis prototype/tests, lancer le script SQLCMD de conservation :

```powershell
Set-Location .\prototype\tests
sqlcmd -S '.\SQLEXPRESS' -E -d ERP_Phase1_TEST -b -i AuditMetierMigrationCopie.sql
```

   Adapter uniquement l'instance SQL. Le script fixe USE ERP_Phase1_TEST,
   capture les comptes (hashs inclus, jamais affichés), les rôles, associations,
   permissions et les deux sources d'audit dans des tables temporaires de
   session, exécute deux fois la migration additive et compare les données.
   Il n'effectue aucun reset, DELETE, restauration ni désactivation de FK.
   Il doit afficher VALIDATION_MIGRATION_COPIE_CONSERVATION_OK puis le
   résultat du diagnostic AuditMetierPhase1Validation.sql.
   Avec SSMS : activer **Requête > Mode SQLCMD** et vérifier les chemins
   relatifs des trois directives :r, ou employer sqlcmd comme ci-dessus.
4. Exécuter également le diagnostic avec le principal applicatif réel réduit,
   pour relever UPDATE/DELETE/ALTER/CONTROL et l'appartenance db_owner/sysadmin.
5. Exécuter le banc SQL suivant UNIQUEMENT sur cette copie jetable :

```powershell
dotnet build .\AuditMetierSqlTests -c Release
$env:ERP_PHASE1_TEST_ENABLE = 'YES'
$env:ERP_PHASE1_TEST_CONNECTION = 'Server=.\SQLEXPRESS;Database=ERP_Phase1_TEST;Integrated Security=True;TrustServerCertificate=True'
& .\AuditMetierSqlTests\bin\Release\net472\AuditMetierSqlTests.exe
```

   Ce banc crée des fixtures et un trigger temporaire de panne, puis retire
   les droits du rôle FACTURIER de la copie pour vérifier le non-réamorçage.
   Il ne convient pas à une base utilisée par des opérateurs ; conserver une
   autre copie pour les tests manuels. Il ne réalise aucun reset commercial.
6. Dans WinForms, tester un brouillon avec client sans téléphone et remise
   non nulle : sauvegarde inchangée => même ClientId, remise et total.
   Tester modification quantité puis conflit entre deux postes ; annulation
   sans motif, avec motif, d'une facture payée ; retrait de droits/reconnexion.
7. Tester deux encaissements simultanés du même document : un succès,
   un refus, un paiement, sorties une seule fois, un audit SUCCES.
   Tester deux lignes d'un même produit dont la somme dépasse le stock :
   aucun paiement/sortie et refus stock, même si chaque ligne tient séparément.
8. Vérifier les audits (numéro exact, acteur, rôle/session, JSON avant/après)
   et consulter des anciennes traces par une période ancienne. Vérifier
   la régularisation avec/sans permission et motif ; simuler la panne d'audit
   uniquement par le banc de test pour constater le rollback complet.
9. Sur une copie manuelle séparée, vérifier impressions A4/80 mm et les
   parcours historiques, sans modifier les moteurs ni exécuter de reset.

## Risques résiduels à contrôler

- SQL Server et WinForms réel ne sont pas disponibles dans l'environnement
  Linux : tests SQL/concurrence/rollback, migration et rendu Windows non exécutés.
- La consultation du journal limite chaque recherche à 1000 lignes ; pas
  de pagination complète. Les dates locales d'AuditActions supposent le
  fuseau historique du magasin, contrairement aux dates UTC nouvelles.
- Les droits dbo/sysadmin/DDL peuvent contourner l'append-only ; réduire le
  principal d'exploitation et séparer le principal de migration reste à faire
  selon les droits réels de l'installation.
- En panne SQL/session, les refus peuvent seulement produire un diagnostic
  technique ; ils ne deviennent jamais des succès mais leur trace SQL n'est
  pas garantie. Les anciens chemins de service non utilisés par l'UI ont
  moins de couverture de refus que les chemins principaux.
- Les anciens documents avec taxe ou remise par ligne restent consultables,
  mais leur édition est maintenant explicitement refusée dans cet écran qui
  ne propose pas cette sémantique. Tester ce refus sans changement de données.
- Aucun chaînage cryptographique de traces, aucune phase 2 activée.

Les rôles qui bénéficiaient uniquement d'un accès codé en dur (notamment un
rôle personnalisé/PASTEUR sans associations SQL) ne récupèrent pas cet accès
implicitement. Vérifier et attribuer explicitement leurs droits, sans rétablir
le fallback ni contourner les décisions d'un administrateur.

## Résultats réellement obtenus pour cette revue

- 21 contrôles purs exécutés : succès.
- Compilation Release du banc net472 : succès, zéro erreur/avertissement.
- API Release : succès, CS1998 préexistant dans AuthService.cs.
- Dashboard Release : succès, zéro erreur/avertissement.
- WinForms complet : NON VALIDÉ. MSB3644 en build standard ; avec les
  références officielles net472 fournies hors dépôt, BC30002 sur les deux
  champs Guna2TextBox existants de LoginForm, DLL absente.
- git diff --check : succès.
- Migration, script de conservation, SQL/concurrence/rollback, impressions
  et UX Windows : NON EXÉCUTÉS. Aucun serveur SQL ni poste Windows utilisé.

Fichiers modifiés par la revue : projet WinForms, FacturationForm,
AuditMetierRegles, AuditMetierService, FacturationService,
FactureOperationService et les deux Program.vb des bancs de tests.
Fichiers ajoutés : AuditMetierMigrationCopie.sql et cette documentation.
Aucune nouvelle migration ni modification du script de migration existant.
