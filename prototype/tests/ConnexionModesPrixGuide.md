# Connexion, modes et delegation : diagnostic et validation

Baseline : `470485464ff22e84496763fdae7bb2b8f6c27b7a`, branche main propre,
HEAD identique a origin/main apres fetch. Aucun acces a la base de production.

## Causes constatees et limites du diagnostic

Le login fait deja role puis mode, dans LoginForm.Authentifier. ListerRolesActifs
relit UtilisateurRoles et trie le role principal en premier. Le formulaire
Administration Utilisateurs enregistre principal et roles autorises ; leurs IDs
restent distincts. CAISSIER et CAISSIERE ne sont ni fusionnes ni renommes.

ModeTravailService calcule les modes pour un seul RoleId attribue au compte.
Le combine requiert les deux ecrans, les deux droits de creation et MODE_COMBINE
sur CE role. Posseder un role facturier et un role caisse ne suffit donc pas.
Cette exclusion n'est pas un bug a contourner par une union de privileges.
Les attributions et parametres exacts de la base Windows restent inconnus ici.

Defauts confirmes et corriges :

- L'exemption par nom ADMIN/SUPERADMIN ignorait meme les modes explicitement
  attribues. Le portail administratif reste classique sans opt-in ; avec un droit
  MODE_* explicite, ces roles passent par la meme resolution operationnelle.
- La fenetre de role ne signalait ni role principal ni parcours disponibles.
  Elle affiche maintenant ces informations et les droits manquants du combine.
- Trois choix de mode occupaient plus que la hauteur disponible du dialogue.
  Sa hauteur permet maintenant leur presentation, avec le role actif identifie.
- DemarrerSession inserait sans revalider l'association utilisateur-role, et
  le controle prealable de session active n'etait pas atomique avec l'insertion.
  Le repository valide compte et role dans une transaction, serialise l'ouverture
  par compte et utilise le nom de role SQL, pas le nom fourni par le formulaire.
- Le contexte memoire etait publie avant le commit SQL. Il est publie apres.
- L'expiration de 30 minutes etait appliquee pour autoriser une nouvelle connexion
  mais pas pour refuser les actions de l'ancienne session. Les services et
  l'activation du mode refusent maintenant la session expiree ; Ping ne la ranime pas.
- Les commandes de prix etaient masquees sans explication. Un etat visible indique
  maintenant activation globale, droits/options manquants ou session invalide.
- La facturation ne reagissait pas a RolePermissionsChanged. Elle recharge
  maintenant ses commandes sur cet evenement local, avec desabonnement a la fermeture.
- L'administration affichait une affirmation inexacte d'heritage automatique
  SUPERADMIN. Un diagnostic des droits selectionnes remplace ce message.

## Matrice de configuration

| Mode | Global requis | Droits du role actif |
| --- | --- | --- |
| FACTURATION | GestionModesActive + ModeFacturationAutorise | FACTURIER + FACTURE_CREER |
| CAISSE | GestionModesActive + ModeCaisseAutorise | CAISSE + ENCAISSEMENT_CREER |
| FACTURATION_ET_CAISSE | GestionModesActive + ModeCombineAutorise | Les quatre droits + MODE_COMBINE |

Une seule option : selection automatique. Plusieurs : dialogue exclusif.
Gestion inactive : parcours historique. Administratif sans MODE_* explicite :
portail existant. Un role demandant explicitement un mode sans aucun droit
operationnel est refuse, au lieu d'ouvrir silencieusement un parcours classique.
MODE_FACTURATION/MODE_CAISSE restent compatibles mais ne sont pas des verrous
additionnels aux droits historiques. Pour retirer un parcours separe, retirer
son ecran/droit de creation, ou desactiver son option globale.

| Prix | Global requis | Privileges explicites |
| --- | --- | --- |
| Demande article | DelegationPrixActive + PrixDemandesActives | FACTURIER + PRIX_CONSULTER + PRIX_DEMANDER_FACTURE |
| Exception directe | DelegationPrixActive + PrixModificationImmediate | FACTURIER + PRIX_CONSULTER + PRIX_FACTURE_EXCEPTION |
| Demande tarif officiel | DelegationPrixActive + PrixDemandesActives | FACTURIER + PRIX_CONSULTER + PRIX_DEMANDER |
| Tarif officiel direct | DelegationPrixActive + PrixModificationImmediate | FACTURIER + PRIX_CONSULTER + PRIX_TARIF_MODIFIER |
| Approbation/refus | Configuration et validite controlees par le service | PARAMETRES + PRIX_APPROUVER / PRIX_REFUSER |

PRIX_DEMANDER n'autorise pas une demande d'exception article. Attribuer
PRIX_DEMANDER_FACTURE pour ce besoin. Une delegation active seule ne donne aucun
privilege. Les limites, le cout comparable, la marge, le motif, la concurrence et
la consommation de l'approbation restent controles dans DelegationPrixService.

Cases dans Roles et privileges : `Mode : facturation et caisse`,
`Prix : consulter les tarifs et demandes`,
`Prix : demander une exception pour une facture`,
`Prix : exception sur une facture uniquement`.
Ne pas confondre la demande de facture avec
`Prix : demander une modification officielle`.

Le bouton produit concerne le tarif officiel ; le bouton du panier concerne une
ligne. Une ligne unique peut etre selectionnee automatiquement ; plusieurs lignes
necessitent une cible identifiee. Aucune approbation ne reprice silencieusement un
panier, ni une facture historique. Catalogue et exception restent distincts.

## Sessions, audit et non-regression

Une ouverture de session et SESSION_ROLE_ACTIVE sont ecrits dans la meme
transaction. Auteur et role sont resolus dans Utilisateurs/Roles depuis la session
nouvellement creee. Un echec d'audit annule l'ouverture. Le mode est ensuite relu,
active et audite par le mecanisme existant ; s'il echoue, le login ferme cette session.
SESSION_ROLE_ACTIVE signifie ouverture reelle, pas facturation ou paiement reussi.
Les evenements financiers et MODE_TRAVAIL_ACTIVE existants ne sont pas dupliques.

Le portail combine reutilise FacturationForm et CaisseForm. FactureEnregistree
transmet l'ID apres commit ; aucun paiement n'est cree automatiquement.
La sortie conserve les gardes brouillon/paiement en cours et impose une reconnexion.
Aucune session distante n'est modifiee. La politique existante d'une session active
par compte est conservee et rendue atomique ; plusieurs comptes/postes restent possibles.

Les droits sont relus dans les services. Sur ce poste : rechargement a l'ouverture,
a Actualiser, a l'activation du formulaire et apres changement local de privileges.
Sur un autre poste : Actualiser ou reconnexion pour l'UX ; le refus cote service
est immediat. Un changement de mode exige une sortie/reconnexion sans cumul.
Une session sans ping pendant 30 minutes doit se reconnecter.

Risques a verifier : affichage a 125/150 % DPI, latence SQL du diagnostic,
concurrence reelle et coupure reseau. Aucune capacite de charge n'est annoncee.
Les recherches de sessions beneficieraient d'un index sur UtilisateurId/Fin/
DernierPing si absent sur la base de deploiement : examiner le plan reel avant
une migration d'optimisation. Pas de migration ajoutee pour ces corrections.

## Validation Windows et SQL Server, exclusivement sur copie

1. Sauvegarder/restaurer une copie isolee. Ne pas lancer de reset. Le schema
   2026101301 et les migrations precedentes doivent etre presents. Executer dans
   SSMS `prototype/tests/Phase2IntegrationValidation.sql` : aucune ecriture.
   Examiner les RoleId et associations, les privileges et les raisons d'exclusion,
   ainsi que les sessions ACTIVE/EXPIREE/FERMEE. Le marqueur n'est pas un test WinForms.
2. Compiler Release dans Visual Studio avec le Developer Pack 4.7.2 et une DLL
   Guna.UI2 compatible/licenciee. Le projet supporte GunaUI2AssemblyPath :

```powershell
msbuild prototype/WinFormsVB/DevCommerc8ak/DevCommerc8ak.vbproj /restore /t:Rebuild /p:Configuration=Release /p:GunaUI2AssemblyPath="C:\Libs\Guna.UI2.dll"
```

3. Modes inactifs : connecter facturier puis caissier/caissiere. Parcours historiques
   inchanges. Mode actif : un role avec les seuls droits facture ouvre Facturation,
   un role avec les seuls droits caisse ouvre Caisse, sans boite inutile.
4. Compte multiroles : voir le principal identifie ; choisir chaque role et verifier
   son diagnostic. Repartir les droits entre deux roles : pas de combine. Les
   attribuer explicitement au meme role avec MODE_COMBINE : trois options si les
   trois globals sont actifs. Retirer MODE_COMBINE : deux options seulement.
5. Admin sans MODE_* explicite : portail classique. Admin avec opt-in et les quatre
   droits : selection des modes. Desactiver le combine global : option absente.
   Role avec MODE_COMBINE seulement : refus explicite, aucun privilege fabrique.
6. Revoquer l'association utilisateur-role ou un droit entre choix et ouverture :
   pas de session utilisable dans le role revoque. Deux connexions simultanees du
   meme compte : une seule ouverture ; des comptes differents ne se deconnectent pas.
7. Session expiree sur la copie : DernierPing de plus de 30 minutes. Une action
   sensible doit etre refusee, le ping ne doit pas la ressusciter. Reconnexion
   requise. Ne pas modifier le ping d'une session de production pour ce test.
8. Prix inactifs : commandes masquees, explication visible, historiques conserves.
   Activer delegation + demandes, attribuer PRIX_CONSULTER et PRIX_DEMANDER_FACTURE
   au role choisi : bouton du panier present, tarif officiel non autorise.
9. Ajouter une ligne puis plusieurs lignes. Verifier cible, quantite, type,
   prix courant, demandeur et motif. Demande sans motif refusee. Une demande
   en attente ne change pas les prix. Responsable distinct approuve/refuse ;
   demandeur applique explicitement. Expiration, changement de contexte et
   reutilisation sur une autre ligne doivent etre refuses.
10. Direct : activer Immediate et attribuer le droit distinct ; limites de cout,
    marge et variation restent appliquees. Modifier un tarif sur un second poste :
    conflit detecte, pas d'ecrasement ni de reprise silencieuse du panier.
11. Combine : facture 56 000 FC, aucun paiement a sa creation. Recevoir 60 000 FC,
    rendre 4 000 FC, affecter 56 000 FC. Reessayer depuis deux postes : un paiement
    seulement. Verifier createur, encaisseur, role, mode, session dans l'audit.
12. Sortie avec panier : enregistrer brouillon, abandon explicite ou annuler sortie.
    Paiement en cours : sortie refusee. Reconnecter en mode separe. Verifier Fin
    et absence d'effet sur un autre compte/poste.
13. Tester erreurs SQL/coupures : pas de succes financier/audit errone, pas de
    session ouverte si son audit echoue. Comparer les factures historiques,
    prix FIXE/COEFFICIENT, conversions, stock, caisse, remises et devises.
14. Verifier A4, thermique et PDF : moteurs non modifies, validation visuelle requise.

Banc d'integration SQL : copie suffixee `_Phase1_TEST`, configurations initiales
inactives, pas d'app.config de production. Les fixtures/audits sont conserves.
Fermer les autres clients de cette copie avant la suite : elle modifie temporairement
les parametres et cree/supprime ses propres triggers de simulation d'echec.

```powershell
$env:ERP_PHASE1_TEST_ENABLE = 'YES'
$env:ERP_PHASE1_TEST_CONNECTION = 'Server=localhost;Database=ERP_Phase1_TEST;Integrated Security=True;TrustServerCertificate=True'
dotnet build prototype/tests/AuditMetierSqlTests -c Release
& .\prototype\tests\AuditMetierSqlTests\bin\Release\net472\AuditMetierSqlTests.exe
```

La suite ajoute : role revoque, double ouverture concurrente, vrai nom de role,
audit unique d'ouverture, rollback si audit echoue, expiration et fermeture.
Elle conserve les scenarios d'approbation, prix concurrents, consommation unique,
encaissement et rollback financier existants. Aucun reset de base.

## Resultats de livraison et fichiers

111 controles purs reussis. Banc net472 compile, tests SQL NON EXECUTES.
API Release compilee (avertissement CS1998 preexistant) ; Dashboard Release
compile sans erreur. WinForms complet NON VALIDE : MSB3644 en build normal,
puis BC30002 Guna2TextBox manquant avec les references 4.7.2 explicites.
Rendu et parcours Windows NON VALIDES dans cet environnement.
Aucun test de charge. Aucun changement de schema, impression, conversion,
stock, reset, formule de prix ou moteur d'encaissement.

Fichiers de production : Forms/LoginForm.vb, FacturationForm.vb ;
UI/DialogueModeTravail.vb, FormulaireSuperAdminRoles.vb ;
Repositories/SessionRepository.vb ; Services/UtilisateurService.vb,
ModeTravailService.vb, ModeTravailRegles.vb, DelegationPrixService.vb,
AutorisationActionService.vb, AuditMetierService.vb.
Tests/documents : AuditMetierCoreTests/Program.vb ; AuditMetierSqlTests/Program.vb
et son projet ; Phase2IntegrationValidation.sql ; ce guide et liens des guides precedents.
