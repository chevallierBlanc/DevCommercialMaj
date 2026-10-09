# Fondations sécurité et audit métier, phase 1

## Architecture et limites

Le RBAC reste `InterfacesApplication / RoleInterfaces`. Les nouvelles actions
ne constituent pas un second système. Une action sensible exige le droit
d'action ET le droit d'écran, un compte actif/non verrouillé, un rôle actif
réellement attribué et une session SQL active correspondant au rôle choisi.
Aucun privilège implicite ADMIN/SUPERADMIN ne contourne ce contrôle.

La migration `2026100901` est additive, embarquée dans WinForms et exécutée
par `SchemaMigrationService`. Elle peut aussi être exécutée sous SSMS :
`WinFormsVB/DevCommerc8ak/Migrations/2026100901_AuditMetier.sql`.
Les droits existants restent inchangés. Les nouvelles actions héritent une seule
fois des droits d'écran existants. Un rôle existant sans association reste sans
droit. Les rôles nouvellement créés et l'installation initiale du mécanisme
bénéficient de l'amorçage documenté dans le SQL. Les lectures au démarrage
ne rétablissent plus une association retirée ni un état actif désactivé.

Facture complète, client créé avec elle, articles et audit sont atomiques.
L'édition exige un motif de 1 à 1000 caractères après nettoyage et la version
rowversion ouverte par le formulaire. Deux éditions de la même version ne
peuvent pas toutes deux réussir. L'annulation reste limitée aux brouillons
EN_ATTENTE : aucune facture payée ne devient annulable.
L'encaissement garde les calculs et mouvements existants ; le contrôle
d'autorisation et son audit rejoignent sa transaction déjà verrouillée.

Les événements principaux sont FACTURE_CREEE, FACTURE_MODIFIEE,
FACTURE_ANNULEE, ENCAISSEMENT_VALIDE et CAISSE_ECART_REGULARISE.
La correction de clôture existante conserve ses calculs mais exige désormais
CAISSE_REGULARISER, le droit d'écran et un motif limité aux 250 caractères
de ses colonnes historiques ; son audit rejoint sa transaction existante.
Les refus/échecs portent les codes
FACTURE_ENREGISTREMENT_REFUSE, FACTURE_ANNULATION_REFUSEE et
ENCAISSEMENT_REFUSE et CAISSE_REGULARISATION_REFUSEE,
avec Resultat REFUS ou ECHEC, jamais SUCCES.
Les chemins principaux audités incluent les snapshots réels avant/après,
la référence, le motif, le compte réel, le rôle et la session SQL, le poste,
une corrélation et la date UTC. Le mode est nullable et non activé dans cette phase.

Les anciennes traces restent dans leurs tables d'origine. La consultation
réunit JournalAudit et AuditActions, avec provenance explicite, sans recopier
ni réécrire les données. AuditActions garde son heure locale historique ;
JournalAudit garde UTC, converti uniquement pour l'affichage et les filtres.
Le double-clic présente les champs réellement modifiés, pas seulement du JSON.
La consultation conserve une limite de 1000 événements ; pagination et filtres
avancés restent hors de cette première fondation.

Les triggers append-only et DENY UPDATE/DELETE public protègent les traces
contre les comptes SQL ordinaires. Ils ne protègent pas contre dbo/sysadmin,
le propriétaire ou un principal capable de modifier le schéma. Le démarrage
actuel crée le schéma : déployer avec un principal de migration séparé puis
réduire les droits du principal d'exploitation reste nécessaire pour une
protection opérationnelle forte. Le diagnostic affiche les privilèges réels ;
ne pas exécuter de révocation automatique sans inventorier les besoins DDL
historiques des repositories. Aucun chaînage cryptographique ni archivage signé
n'est implémenté : ces mécanismes nécessitent une politique de conservation
et des clés externes. Aucun bouton de modification/suppression d'audit n'est ajouté.

Les chemins legacy de service restent autorisés et audités en transaction,
mais ne remplacent pas le service de sauvegarde complète et son contrôle de
version. Les refus des chemins principaux disposent d'une trace indépendante
après rollback. Si SQL/session est indisponible, cette trace de refus ne peut
être garantie : seul un avertissement technique sans secret est émis.
Les historiques de prix nouveaux utilisent l'acteur connecté au lieu de 1 ;
les anciennes lignes et l'architecture transactionnelle des prix ne sont pas
réécrites. Aucun workflow de correction d'un paiement validé n'existe dans
les chemins caisse inspectés : cette phase n'en crée pas un artificiellement.
Les paramètres GestionModesActive et DelegationPrixActive sont à faux par
défaut ; aucune interface ni fonctionnalité de phase 2 n'est activée.

## Validation Windows / SQL Server, dans cet ordre

1. Sauvegarder, puis restaurer une copie dédiée nommée `ERP_Phase1_TEST`.
   Ne jamais utiliser le banc d'intégration sur CommercialMagDB ou une base réelle.
2. Compiler WinForms dans Visual Studio avec .NET Framework 4.7.2 et Guna.UI2
   disponibles. Aucun rendu d'impression ni reset commercial n'est modifié.
3. Exécuter la migration ci-dessus sur la copie, ou démarrer l'application
   pointée uniquement vers cette copie avec le principal de migration.
4. Vérifier `SELECT * FROM dbo.SchemaVersion WHERE Version=2026100901`.
   Exécuter `tests/AuditMetierPhase1Validation.sql` : schéma OK,
   triggers actifs, DENY présents, volumes historiques conservés.
5. Comparer les anciennes lignes d'audit, utilisateurs, rôles et associations
   à la sauvegarde : aucun historique ne doit être réécrit/supprimé.
6. Dans gestion des rôles, retirer FACTURE_MODIFIER à un rôle de test.
   Redémarrer deux fois : le droit doit rester absent. Retirer tous ses droits :
   aucun fallback de nom de rôle ne doit rouvrir les écrans.
7. Créer une facture avec deux articles : un FACTURE_CREEE SUCCES avec
   snapshot doit exister. Modifier quantité/prix/client avec un motif :
   FACTURE_MODIFIEE et les différences exactes doivent apparaître au double-clic.
   Les champs inchangés ne doivent pas apparaître comme différences.
8. Ouvrir le même brouillon sur deux postes. Enregistrer le premier, puis
   le second : conflit explicite, aucune réécriture silencieuse.
9. Annuler sans motif ou avec espaces : refus et statut inchangé.
   Annuler avec motif et droit : statut ANNULEE, audit dans la même transaction.
10. Retirer ENCAISSEMENT_CREER puis tenter l'appel de service : refus.
    Le réattribuer : encaissement habituel, un paiement et audit.
    Répéter : pas de double paiement/mouvement. Annuler une facture payée : refus.
    Tester aussi une régularisation de clôture : sans CAISSE_REGULARISER ou
    sans motif elle est refusée ; avec permission/motif, l'audit montre le
    statut et le total régularisé avant/après, dans la même transaction.
11. Sur la copie seulement, exécuter le banc d'intégration ci-dessous pour
    simuler une panne d'audit : facture et lignes doivent être annulées ensemble.
12. Sous le principal SQL applicatif non propriétaire, essayer UPDATE/DELETE
    sur les traces : refus. Ne pas confondre ce contrôle avec une garantie
    contre sysadmin. Examiner les privilèges affichés par le diagnostic.
13. Modifier un prix dans la gestion produit avec un utilisateur de test :
    la nouvelle ligne HistoriquePrix doit référencer son ID réel, pas 1.
14. Déconnexion : les anciennes permissions ne doivent plus être utilisables.
    Vérifier les comptes multirôles : seul le rôle SQL de la session est pris
    en compte, pas l'union des rôles attribués.
15. Vérifier les parcours existants : impression A4/80 mm, stock après paiement,
    historique factures, administration et reset technique sans exécuter de reset.

## Tests automatisés

Tests purs, exécutables sous Linux :
`dotnet run --project tests/AuditMetierCoreTests -c Release`.

Banc d'intégration Windows, **copie dédiée seulement**, après migration :

```powershell
dotnet build tests/AuditMetierSqlTests -c Release
$env:ERP_PHASE1_TEST_ENABLE = 'YES'
$env:ERP_PHASE1_TEST_CONNECTION = 'Server=.;Database=ERP_Phase1_TEST;Integrated Security=True;TrustServerCertificate=True'
& .\tests\AuditMetierSqlTests\bin\Release\net472\AuditMetierSqlTests.exe
```

Le suffixe `_Phase1_TEST` et l'activation explicite sont obligatoires. Ne pas
copier app.config de production. Ce banc crée des fixtures et un trigger de
panne temporaire sur la copie ; son compte de test SQL doit disposer des droits
DDL nécessaires. Les fixtures/audits restent dans la copie pour inspection.
Il ne teste pas une réinitialisation et ne supprime aucun historique réel.
Tester séparément les DENY avec le véritable principal d'exploitation réduit.

## Fichiers de cette phase

Créés :

- `WinFormsVB/DevCommerc8ak/Migrations/2026100901_AuditMetier.sql`
- `WinFormsVB/DevCommerc8ak/Services/AutorisationActionService.vb`
- `WinFormsVB/DevCommerc8ak/Services/AuditMetierService.vb`
- `WinFormsVB/DevCommerc8ak/Services/AuditMetierRegles.vb`
- `WinFormsVB/DevCommerc8ak/Services/AuditDifferenceService.vb`
- `WinFormsVB/DevCommerc8ak/Services/FactureOperationService.vb`
- `WinFormsVB/DevCommerc8ak/UI/DialogueAuditMetier.vb`
- `WinFormsVB/DevCommerc8ak/UI/DialogueMotifOperation.vb`
- `tests/AuditMetierCoreTests/AuditMetierCoreTests.vbproj` et `Program.vb`
- `tests/AuditMetierSqlTests/AuditMetierSqlTests.vbproj` et `Program.vb`
- `tests/AuditMetierPhase1Validation.sql` et cette documentation

Modifiés dans `WinFormsVB/DevCommerc8ak` :

- `DevCommerc8ak.vbproj`
- `DTOs/AuditLogEntryDTO.vb`
- `Forms/AdminForm.vb`, `Forms/MainForm.vb`
- `Forms/FacturationForm.vb`, `Forms/CaisseForm.vb`
- `UI/FormulaireFactures.vb`, `UI/FormulaireSuperAdminJournal.vb`
- `Repositories/ClientRepository.vb`
- `Repositories/FactureVenteRepository.vb`
- `Repositories/LigneFactureVenteRepository.vb`
- `Repositories/ProduitRepository.vb`
- `Repositories/SuperAdminRepository.vb`
- `Repositories/AnalyseCaissePhysiqueRepository.vb`
- `Services/FacturationService.vb`
- `Services/AnalyseCaissePhysiqueService.vb`
- `Services/SchemaMigrationService.vb`
- `Services/SuperAdminService.vb`

Permissions nouvelles : FACTURE_CREER, FACTURE_MODIFIER, FACTURE_ANNULER,
ENCAISSEMENT_CREER, CAISSE_REGULARISER et AUDIT_CONSULTER.

## Résultats obtenus dans l'environnement Linux

- Tests purs : 14 contrôles exécutés et réussis.
- Banc SQL net472 : compilation réussie, zéro erreur/avertissement ; il
  compile les vrais services/repositories transactionnels et les deux nouveaux
  dialogues. Ses scénarios SQL ne sont pas exécutés, faute d'instance SQL Server.
- API Release : compilation réussie, un avertissement CS1998 préexistant.
- Dashboard Release : compilation réussie sans avertissement.
- WinForms complet : NON VALIDÉ. Build standard bloqué par MSB3644.
  Une seconde tentative avec les références officielles net472 téléchargées
  hors dépôt atteint le compilateur, puis échoue sur les deux champs
  Guna2TextBox existants de LoginForm (dépendance Guna.UI2 absente).
- Aucune migration SQL ni interface/impression Windows n'a été exécutée ici.
- Aucun reset, aucune suppression de données de production, aucune modification
  des moteurs d'impression, de conditionnements ou de réinitialisation.

La compilation partielle et les tests purs ne constituent pas une validation
de déploiement. Exécuter la checklist sur copie, puis approuver cette phase
avant d'entreprendre la délégation des prix ou les modes de travail.
