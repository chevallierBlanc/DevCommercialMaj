# Deblocage explicite de l'administration des modes

## Cause demontree et limite du diagnostic

Depart : main propre, HEAD et origin/main identiques a
`ab85175477e6e36fcbe229fa1592e9bb41f616a6`.

2026101501 cree la definition UTILISATEUR_MODES, mais aucun RoleInterfaces :
c'est volontaire afin de ne pas retablir des droits retires ni accorder une
permission sensible a toutes les installations. Le nom SUPERADMIN n'accorde
pas ce droit. Lister exigeait pourtant UTILISATEUR_MODES AVANT de lire les
roles du compte : l'exception laissait le ComboBox vide. Celui-ci est une
selection de ROLE ; le compte est selectionne dans le repertoire a gauche.

Le controle existant utilise l'ID du role actif, l'utilisateur, la session SQL,
les associations actives et le dernier ping. Il ne cumule pas les autres
roles et ne se fonde pas sur le texte SUPERADMIN. La base Windows de OneLove
n'a pas ete interrogee ici : son contenu reel et l'application de la migration
doivent etre verifies avec le diagnostic fourni. Le message observe prouve
le refus du controle, pas a lui seul la presence de toutes les migrations.

## Correction fonctionnelle

- Un utilisateur ayant ADMINISTRATION peut lire les roles et les attributions
  du compte selectionne sans UTILISATEUR_MODES. Les controles d'ecriture
  restent en lecture seule si ce dernier droit manque.
- Un gestionnaire ayant reellement ADMINISTRATION ET SUPERADMIN_ROLES dans
  son SEUL role actif voit l'action **AUTORISER MON ROLE A GERER LES MODES**.
- Cette action demande confirmation et motif, puis ajoute UNIQUEMENT
  UTILISATEUR_MODES a ce role actif. Elle concerne les membres de ce role,
  pas seulement OneLove ; le texte de confirmation l'explique.
- Le grant est gere dans SuperAdminRepository, avec le meme controle que
  l'editeur de privileges, en transaction avec PERMISSION_MODIFIEE et les
  droits avant/apres. Le nom du role est relu en SQL. Sa rowversion change
  pour invalider un editeur de privileges deja ouvert.
- Un nouvel appel ne duplique ni le grant ni son evenement de modification.
  Un echec d'audit annule le grant. Un refus est trace separement.
- Rien n'est accorde au demarrage, par migration, sur le simple nom SUPERADMIN,
  depuis un autre role du compte, ou a Tatiana automatiquement.
- Apres le grant, la section relit SQL et permet de configurer les attributions.
  Une revocation reste effective : aucune reparation silencieuse.

Si OneLove ne possede pas SUPERADMIN_ROLES, l'action n'est PAS proposee.
Un autre gestionnaire deja autorise doit modifier le role dans Roles et
privileges. Si aucun compte n'a cette autorisation, intervention DBA controlee
necessaire ; aucun passe-droit ou script de grant automatique n'est fourni.

## Procedure Windows / Visual Studio / SSMS

1. Restaurer une copie isolee et configurer WinForms sur cette copie.
2. Executer AdministrationModesDiagnostic.sql en adaptant @Administrateur et
   @Compte aux vrais noms de connexion. Verifier DB_NAME, serveur, version
   2026101501, colonne, definition active du privilege et les droits du role
   REEL de la session OneLove (SessionId/Poste), pas une union des roles.
3. Si la migration manque, appliquer la migration additive existante
   2026101501_VenteEncaissement.sql sur la copie, ou laisser le demarrage la
   lancer via SchemaMigrationService. Aucune nouvelle migration necessaire.
4. Visual Studio : workload Desktop .NET, Developer Pack 4.7.2, vraie DLL
   Guna.UI2 licenciee ; restaurer puis reconstruire Release.
5. OneLove : Administration Utilisateurs, selectionner la LIGNE Tatiana.
   La section doit montrer ses roles, meme sans UTILISATEUR_MODES.
6. Si lecture seule et si OneLove gere reellement les privileges, cliquer
   AUTORISER MON ROLE A GERER LES MODES, confirmer la portee ROLE puis donner
   un motif. Aucun role operationnel de Tatiana ne change a cette etape.
7. Selectionner FACTURIER : laisser Vente & Encaissement NON. Selectionner
   CAISSIERE : verifier les cinq droits necessaires du role, cocher, donner un
   motif et enregistrer. Relire chaque role apres Actualiser.
8. Fermer puis rouvrir l'administration et reconnecter OneLove : persistance
   du privilege, pas de dependance a un cache. Reconnecter Tatiana : FACTURIER
   sans case, CAISSIERE avec case ; decochee Caisse, cochee FacturationEtCaisseForm.
9. Retirer l'attribution CAISSIERE puis reconnecter Tatiana : disparition de
   la case. Desactiver le mode global : indisponible sans effacer les grants.
10. Retirer UTILISATEUR_MODES au role administratif dans Roles et privileges :
    la lecture reste possible, l'ecriture est refusee, et un redemarrage ne
    restaure pas ce droit. Verifier PERMISSION_MODIFIEE et MODE_UTILISATEUR_ATTRIBUE
    dans le journal avec auteur, role actif, session, motif et avant/apres.
11. Sur copie, tester role administratif sans droit de gestion des roles :
    pas d'action de grant, tentative service refusee meme si un autre role
    du compte possede ce droit. Test de session expiree egalement obligatoire.
12. Vente combinee, auteurs distincts, double encaissement, stock, caisse,
    facture classique et impressions : executer la checklist existante
    CorrectionVenteEncaissementGuide.md. Ces moteurs ne sont pas modifies ici.

```powershell
msbuild prototype\WinFormsVB\DevCommerc8ak\DevCommerc8ak.vbproj /restore /p:Configuration=Release /p:GunaUI2AssemblyPath="C:\Libs\Guna.UI2.dll"
$env:ERP_PHASE1_TEST_ENABLE='YES'
$env:ERP_PHASE1_TEST_CONNECTION='Server=localhost;Database=ERP_Phase1_TEST;Integrated Security=True;TrustServerCertificate=True'
msbuild prototype\tests\AuditMetierSqlTests\AuditMetierSqlTests.vbproj /restore /p:Configuration=Release
& prototype\tests\AuditMetierSqlTests\bin\Release\net472\AuditMetierSqlTests.exe
```

Le banc SQL cree des fixtures et injecte un trigger de panne UNIQUEMENT sur
la copie *_Phase1_TEST explicitement autorisee. Ne pas copier app.config de
production dans ce banc. Ses preconditions sont documentees dans les guides
existants. Il couvre lecture seule, refus, faux nom SUPERADMIN, grant explicite,
audit/rollback, idempotence, relecture SQL et absence de restauration au demarrage,
plus les attributions independantes et les transactions du parcours combine.

## Perimetre et validations

Fichiers modifies : Services/AttributionModeService.vb,
Repositories/SuperAdminRepository.vb, UI/ModesUtilisateurPanel.vb,
UI/FormulaireUtilisateurs.vb et tests/AuditMetierSqlTests/Program.vb.
Nouveaux fichiers : ce guide et AdministrationModesDiagnostic.sql.
Aucune migration nouvelle, aucune donnee existante modifiee automatiquement.
Pas de modification des impressions, prix, conversions, stock ou reset.

Les builds et controles purs executes sous Linux ne prouvent pas le parcours
Windows/SQL. Les tests SQL, l'ouverture du formulaire combine et les impressions
doivent etre executes sur copie Windows ; aucune validation de production
n'est revendiquee. Le controle isole qui exclut LoginForm sous /tmp n'est pas
un build valide de l'application complete (dependance Guna absente).

Controles de livraison : 119 controles purs passes ; builds API Release
(avertissement preexistant CS1998) et Dashboard Release reussis. Le banc SQL
compile, mais n'a pas ete execute faute de SQL Server. Le controle de
compilation isole des formulaires compile ; le build WinForms complet sous
Linux est bloque par MSB3644, puis Guna.UI2 manquant avec les references
temporaires. Windows, SQL Server, concurrence reelle et impressions NON VALIDES.
git diff --check et revue du diff effectues ; aucune base reinitialisee.
