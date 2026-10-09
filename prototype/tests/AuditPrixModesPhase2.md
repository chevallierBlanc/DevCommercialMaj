# Journal métier, paiements, prix et modes : déploiement et validation

## Périmètre et architecture

Base de départ : `184a541b90c37a235e8a4eff79ee5f34e35f393d`, branche
`main`, dépôt propre et identique à `origin/main` après récupération distante.
Le RBAC existant reste `InterfacesApplication` / `RoleInterfaces` et les
associations multi-rôles restent `UtilisateurRoles`. Aucun droit n'est accordé
automatiquement par ces nouvelles migrations, aucun droit retiré n'est rétabli.

Les nouveaux services consomment `AutorisationActionService`, le rôle actif
réel, le compte et la session SQL. Un bouton masqué n'est pas une protection.
Le même `JournalAudit` est enrichi ; les anciennes traces et leur provenance
`AuditActions` restent consultables. Les protections append-only de la phase 1
restent inchangées. Aucun moteur d'impression ou de reset n'est modifié.

## Journal et identités

`DialogueAuditMetier` reprend la composition de l'aperçu de facture : en-tête,
cartes d'informations, onglets financiers/différences, motif, fermeture.
`AuditPresentationService` traduit les chemins techniques uniquement à
l'affichage et utilise les désignations des articles présentes dans l'instantané.
Les clés et identifiants d'origine restent enregistrés.

Pour un auteur historique : priorité au nom enregistré dans l'instantané,
sinon lecture du véritable compte, même désactivé, avec indication « nom actuel
du compte » et ID. Un compte introuvable garde un libellé explicite avec son ID.
Le schéma ne contient pas de nom complet distinct : le nom réel disponible
est `NomUtilisateur`, jamais un nom complet inventé. Le nom de l'auteur et son
rôle actif sont déjà instantanés dans les nouveaux événements JournalAudit.
Les nouvelles traces d'encaissement ajoutent aussi les noms du créateur de la
facture et du caissier. Aucun rôle historique inconnu n'est reconstitué.

## Paiements

Cause corrigée : `MontantRecu` et `MonnaieRendue` sont normalisés en FC dans
le flux de caisse, alors que `Devise` indique la devise de saisie. Le montant
saisi et le taux réellement utilisé n'étaient pas conservés séparément.

Migration `2026100902_TracabilitePaiements.sql` :

- `Paiements.MontantRecuOrigine` : montant saisi, nullable.
- `Paiements.TauxConversionApplique` : taux réellement utilisé, nullable.
- `Paiements.DeviseMontants` : devise des montants normalisés, nullable.

Les anciennes lignes ne sont pas réécrites. Une information inconnue reste
inconnue, particulièrement la monnaie rendue et le taux historique.
Le flux `EncaisserFacture` conserve le paiement, ses détails et l'audit dans
la même transaction. Le journal distingue total, déjà payé, reste avant,
reçu, affecté, rendu, reste après, devise, moyen et référence du paiement.
Les verrous sur facture/paiements et le refus d'un règlement déjà présent
préservent la prévention des doubles encaissements.

Le flux de caisse actuellement actif effectue un règlement complet en une
devise de saisie. Il ne propose pas de règlement partiel ou mixte. Cette passe
ne crée pas artificiellement ces workflows ; le chemin historique de validation
refuse désormais un paiement partiel qu'il aurait pourtant marqué PAYEE.
Les montants normalisés avec plus de deux décimales sont refusés plutôt
qu'arrondis silencieusement ; montant brut/taux sont limités à huit décimales.

## Modes

Migration `2026100903_ModesTravail.sql` : paramètres
`ModeFacturationAutorise`, `ModeCaisseAutorise`, `ModeCombineAutorise`, tous
désactivés par défaut. Le paramètre phase 1 `GestionModesActive` reste désactivé.
Permissions disponibles dans Rôles et privilèges :
`MODE_FACTURATION`, `MODE_CAISSE`, `MODE_COMBINE`, `PARAMETRES_SECURITE`.

Le login choisit un rôle réellement attribué, puis un mode explicitement
autorisé pour ce seul rôle. Un mode unique est automatique ; plusieurs modes
ouvrent un choix exclusif. Le mode combiné exige son propre droit, les deux
écrans et les droits métier concernés ; il n'est jamais déduit du cumul des
rôles. ADMIN/SUPERADMIN conservent leur parcours administratif.

Le mode est enregistré dans `UtilisateurSessions`, affiché dans MainForm et
contrôlé à nouveau dans les services sensibles, avec les permissions et les
paramètres SQL courants. La navigation est restreinte sans remplacer ces
contrôles. `MODE_TRAVAIL_ACTIVE` est écrit dans la transaction d'activation.
La création de facture n'encaisse rien : un paiement reste une opération
distincte, avec auteur/session/mode dans son propre événement.

Le changement de mode en cours de session n'est pas exposé : déconnexion puis
reconnexion obligatoires. Cela évite la perte de brouillons et le cumul de
permissions. Les modes et droits de l'ancienne session sont effacés.

## Délégation des prix

Migration `2026100904_DelegationPrix.sql` :

- Paramètres : `PrixDemandesActives`, `PrixModificationImmediate`,
  `PrixSousCoutAutorise` désactivés, `PrixVariationMaxPourcent` et
  `PrixMargeMinimumPourcent` initialisés à zéro.
- `LignesFactureVente.MotifPrixException`, nullable.
- `DemandesModificationPrix`, états EN_ATTENTE / APPROUVEE / REFUSEE /
  ANNULEE / EXPIREE, auteur, décision, expiration, empreinte de tarif.
- Droits : `PRIX_CONSULTER`, `PRIX_DEMANDER`, `PRIX_TARIF_MODIFIER`,
  `PRIX_FACTURE_EXCEPTION`, `PRIX_APPROUVER`, `PRIX_REFUSER`, `PRIX_SOUS_COUT`,
  `PARAMETRES_PRIX`. Aucun n'est automatiquement attribué.

Dans Paramètres, l'onglet sécurité donne accès à la configuration des modes,
de la délégation et aux demandes. Un motif est obligatoire pour les modifications.
Dans Facturation, MODIFIER PRIX ouvre un dialogue limité au produit/type choisi.
La modification du tarif et le prix exceptionnel d'une seule facture sont
séparés. L'exception s'applique à un article déjà présent dans le panier ; elle
est contrôlée à nouveau lors de l'enregistrement, avec son motif persistant.
Les totaux d'une facture comportant une exception doivent correspondre aux
lignes ; une incohérence est refusée sans recalcul silencieux.

`DelegationPrixService` relit tarif, coût et équivalent en base sous verrou,
en réutilisant les conversions existantes. Un coût non comparable ou absent
refuse la délégation. L'empreinte détecte un tarif/coût/configuration physique
modifié depuis la lecture. Un mode COEFFICIENT est conservé ; un nouveau prix
impossible à représenter exactement par le coefficient est refusé.

Un changement immédiat exige le paramètre ET la permission ET les seuils.
Hors seuil, une demande garde l'ancien tarif actif jusqu'à approbation par un
autre utilisateur autorisé. Validité fixe de 24 heures. Refus/annulation exigent
un motif ; annulation réservée à l'auteur. Les demandes expirent à la consultation
ou à la décision. Les listes et traitements d'expiration sont limités à 500
demandes par consultation. Aucun écran général de produits n'est accordé.

La vente sous coût exige le paramètre dédié et `PRIX_SOUS_COUT`. Un prix
exceptionnel hors seuil n'a pas de circuit distinct d'approbation de facture :
il est refusé et nécessite une demande de tarif officiel. Un responsable peut
approuver un tarif sous le seuil si les permissions/règles sous coût le permettent.
Cette restriction est intentionnelle ; ne pas confondre les deux approbations.

Tarifs officiels : historique existant avec véritable utilisateur et audit
avant/après dans la transaction. Types personnalisés : audit avant/après sans
détourner l'historique des colonnes produit. Une facture validée n'est pas
réécrite ; un panier n'est pas rafraîchi silencieusement. Un prix déjà enregistré
dans un brouillon inchangé est conservé. Un nouveau prix périmé est soumis aux
contrôles d'exception à l'enregistrement.

Événements supplémentaires : PRIX_MODIFICATION_DEMANDEE / APPLIQUEE /
APPROUVEE / REFUSEE / EXPIREE / ANNULEE, FACTURE_PRIX_MODIFIE,
DELEGATION_PRIX_ACTIVEE / DESACTIVEE, MODE_TRAVAIL_ACTIVE,
PERMISSION_MODIFIEE pour configuration des modes. Les échecs de modification
et décision sont tracés séparément, jamais comme un succès de la transaction
annulée. Les événements métier existants sont conservés.

## Déploiement : copie de base d'abord

1. Sauvegarder la base, restaurer une copie jetable `ERP_Phase1_TEST`. Ne pas
   exécuter le banc d'intégration sur CommercialMagDB ou une copie à conserver.
2. Compiler Windows avec Visual Studio, workload .NET desktop, targeting pack
   .NET Framework 4.7.2 et la DLL Guna.UI2 autorisée. Voir aussi
   `AuditMetierRevuePhase1.md` pour les dépendances existantes.

```powershell
$env:GunaUI2AssemblyPath = 'D:\ERPDependencies\Guna.UI2.dll'
MSBuild .\prototype\WinFormsVB\DevCommerc8ak.sln /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU"
```

3. L'application applique automatiquement les trois ressources de migration
   via SchemaMigrationService. Vérifier les versions 2026100902, 2026100903 et
   2026100904 dans SchemaVersion et lancer `AuditPrixModesValidation.sql` sur
   la copie. Ce script est strictement non destructif et valide le schéma et
   les invariants disponibles, pas les scénarios de concurrence.
4. Pour tester explicitement l'idempotence et la conservation, activer SQLCMD
   dans SSMS, ouvrir `AuditPrixModesMigrationCopie.sql` avec le répertoire de
   travail `prototype/tests`, ou lancer :

```powershell
Push-Location .\prototype\tests
sqlcmd -S .\SQLEXPRESS -E -b -i AuditPrixModesMigrationCopie.sql
Pop-Location
```

   Ce script cible exclusivement ERP_Phase1_TEST, applique deux fois les
   migrations et compare paiements/prix/permissions/rôles/audits antérieurs.
   Il ne fait aucun reset et aucun DELETE. La copie doit déjà avoir reçu phase 1.
5. Dans Rôles et privilèges, attribuer explicitement les droits nécessaires
   aux seuls rôles choisis, avec leurs écrans et actions existants. Puis activer
   volontairement les paramètres. Les comptes administratifs ne contournent pas
   les droits métier. Vérifier avec un compte sans permission également.
6. Avant passage en production, faire approuver les résultats Windows/SQL
   Server. Les droits SQL applicatifs doivent rester de moindre privilège ;
   ne pas accorder db_owner pour résoudre un refus d'audit. Les protections
   phase 1 ne garantissent pas l'immutabilité contre un administrateur SQL sysadmin.

## Tests automatisés

```powershell
dotnet run --project .\prototype\tests\AuditMetierCoreTests -c Release
$env:ERP_PHASE1_TEST_ENABLE = 'YES'
$env:ERP_PHASE1_TEST_CONNECTION = 'Server=.\SQLEXPRESS;Database=ERP_Phase1_TEST;Integrated Security=True;TrustServerCertificate=True'
MSBuild .\prototype\tests\AuditMetierSqlTests\AuditMetierSqlTests.vbproj /restore /p:Configuration=Release
& .\prototype\tests\AuditMetierSqlTests\bin\Release\net472\AuditMetierSqlTests.exe
```

Le banc SQL refuse une base sans suffixe `_Phase1_TEST`. Il crée des fixtures,
accorde des droits aux rôles de test, modifie les paramètres de la copie et
crée un trigger d'échec temporaire pour vérifier les rollbacks. Il ne supprime
pas les données commerciales ni les fixtures ; employer une copie jetable.
Les paramètres délégation/modes doivent être désactivés au départ. Il couvre
notamment droits refusés, tarifs concurrents, approbations concurrentes,
auto-approbation interdite, expiration, échec d'audit, modes explicites et
facture/encaissement combinés. Compilation du banc != exécution SQL.

## Checklist Windows fonctionnelle

1. Journal : ouvrir une création, modification, annulation et régularisation.
   Vérifier référence, statut, auteur/rôle/session et seuls changements réels.
   Redimensionner à la taille minimale puis maximiser ; motif/tabs lisibles.
2. Anciennes traces : auteur désactivé, ID sans compte et absence d'instantané.
   Attendre nom actuel explicitement signalé ou ID explicite, jamais l'auteur connecté.
3. Encaisser 56 000 FC, saisir 60 000 FC : affecté 56 000, rendu 4 000,
   reste zéro ; paiement/audit identiques. Deux postes simultanés : un seul paiement.
4. USD : taux configuré 2 000, facture 56 000 FC, reçu 30 USD : brut 30 USD,
   taux 2 000, normalisé 60 000 FC, rendu 4 000 FC, net 56 000 FC.
   Changer ensuite le taux : la trace existante reste inchangée.
5. Ancien paiement sans nouveau taux : pas de taux ou monnaie inventés.
   Refuser montant insuffisant, reçu-rendu incohérent, arrondi silencieux.
6. Délégation désactivée ou permission retirée : opérations refusées côté service.
   Redémarrer : aucun droit retiré n'est réaccordé.
7. Prix pièce 15, coût comparable 10, variation 10 %, marge 0 : 16,50 accepté
   avec droit immédiat/motif ; 20 exige demande et laisse 16,50 actif avant décision.
8. Demande : approbation par autre responsable, refus motivé, annulation par
   auteur, expiration 24 h, deuxième décision refusée. Deux modifications sur
   même empreinte : un succès, un conflit. Exception SQL/audit : aucun tarif appliqué.
9. Prix sous coût : refus par défaut. Activer paramètre + permission spécifique
   pour circuit autorisé ; coût carton et prix pièce ne sont jamais comparés directement.
10. Exception facture : droit officiel seul ne suffit pas. Ajouter article puis
    prix exceptionnel avec motif : seule cette facture change, tarif reste intact.
    Hors seuil : refus. Enregistrer/recharger : motif conservé, totaux cohérents.
11. Types personnalisés FIXE/COEFFICIENT et liés aux conditionnements : prix
    comparable correct ; coefficient inexprimable refusé ; type non vendable refusé.
12. Modes désactivés : parcours existant. Activer gestion et modes explicitement.
    Rôle avec un seul mode : sélection automatique ; plusieurs : choix exclusif.
13. FACTURATION : caisse inaccessible. CAISSE : facture modifiable inaccessible.
    Cumuler deux rôles sans MODE_COMBINE : aucun mode combiné. Le rôle actif seul
    avec ses deux écrans/actions et MODE_COMBINE : mode combiné accessible.
14. Combiné : créer facture sans paiement, encaisser séparément ; deux traces
    distinctes avec même session/mode, véritables facturier/caissier ; contrôle
    double encaissement conservé. Retirer le droit depuis un autre poste : refus
    au prochain service sensible. Déconnecter/reconnecter : aucun cumul de mode.
15. Régression : impression A4/PDF/80 mm, ancien produit, stock après vente,
    caisse après paiement, anciens comptes/rôles, traces historiques. Ne pas
    exécuter de reset pour cette mission.

## Résultats exécutés dans cet environnement

- Tests purs Release : 49 contrôles réussis, sans connexion SQL.
- Banc d'intégration net472 Release compilé avec les références officielles :
  zéro erreur et zéro avertissement. Scénarios SQL non exécutés.
- API et Dashboard Release : builds réussis ; aucune modification de ces projets.
- WinForms standard : échec MSB3644, targeting pack 4.7.2 absent.
- WinForms avec références officielles explicitement fournies : échec BC30002
  sur les deux Guna2TextBox existants de LoginForm, dépendance Guna.UI2 absente.
- `git diff --check` réussi. Aucune base SQL ni impression Windows exécutée.

## Limites et validation réelle

Les tests purs et les builds exécutés sont détaillés dans le rapport de livraison.
SQL Server/SSMS, les migrations, les scénarios SQL de concurrence et les
interfaces/impressions Windows ne sont pas exécutés dans cet environnement Linux.
Le banc net472 compilé utilise les références officielles ; la compilation de
l'application entière reste bloquée sans la dépendance existante Guna.UI2.

Pas de nouvelle API distante, de synchronisation ou de multi-tenant implicite :
configuration d'une entreprise par base, refus d'une configuration ambiguë dans
les nouveaux services. Les demandes de prix ont uniquement des FK vers les
référentiels conservés ; le reset commercial n'est pas modifié et ne les purge
pas. Sa politique vis-à-vis de ces demandes nécessite une mission distincte.
Les limites de pagination existantes du journal ne sont pas étendues ici.
