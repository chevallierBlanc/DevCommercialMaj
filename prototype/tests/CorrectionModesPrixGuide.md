# Correction des connexions et commandes de prix

## Etat initial et diagnostic

Baseline : `9918835d5fbf57716f03392956e6e56666fde710`, main propre et
identique a origin/main apres fetch.

Defauts confirmes dans le code :

- ModeTravailService.Lister exigeait MODE_CAISSE / MODE_FACTURATION en
  supplement des droits historiques. Ces nouvelles definitions ne sont pas
  automatiquement attribuees par les migrations : activer la gestion pouvait
  donc bloquer un caissier pourtant habilite. Les grants de la base Windows
  ne sont pas accessibles ici : le diagnostic SQL ci-dessous les expose.
- RafraichirPanier detruisait la liaison puis masquait les colonnes techniques
  sans restaurer une cellule courante visible. Les deux commandes de ligne
  dependaient exclusivement de CurrentRow, sans solution pour une ligne unique.
  La reproduction exacte du comportement DataGridView reste a faire sous Windows.
- La commande du panier et son menu etaient toujours visibles. Le bouton produit
  ne verifiait que PRIX_CONSULTER, sans configuration globale ni droit d'action.
- VerifierException ignorait PrixModificationImmediate : une exception directe
  pouvait passer le service malgre la desactivation de cette option.

Ce qui est deja relie et conserve : choix du role attribue, selection exclusive
du mode, activation SQL de la session, verification du mode dans les services,
portail combine reutilisant FacturationForm/CaisseForm, transfert de l'identifiant
apres commit, paiement distinct, sortie avec brouillon et garde d'encaissement.
Les demandes ont deja un contexte de ligne, un auteur, une validite, des seuils,
une version de tarif, une decision et une consommation transactionnelle.
Il n'y a pas de nouveau moteur commercial dans cette correction.

## Regles de compatibilite et securite

Gestion des modes inactive : parcours historique conserve. Une session deja
active avec un mode doit se reconnecter si la configuration devient incompatible.
Les parcours administratifs existants restent exempts de selection operationnelle.

Gestion active, pour le seul role choisi :

| Parcours | Parametre global | Droits actifs obligatoires du role |
| --- | --- | --- |
| Facturation | ModeFacturationAutorise | FACTURIER + FACTURE_CREER |
| Caisse | ModeCaisseAutorise | CAISSE + ENCAISSEMENT_CREER |
| Facturation et caisse | ModeCombineAutorise | Les quatre droits ci-dessus + MODE_COMBINE |

Les droits historiques constituent l'autorisation explicite des modes separes.
MODE_CAISSE et MODE_FACTURATION restent definis pour compatibilite mais ne sont
plus des verrous additionnels. Pour retirer un parcours separe, retirer son
droit metier ou son ecran, ou desactiver son parametre global ; retirer uniquement
MODE_CAISSE/MODE_FACTURATION ne retire pas les droits metier historiques.
Aucun grant n'est cree, reinitialise ou reconstruit par cette correction.
Deux roles ne sont jamais reunis pour fabriquer un parcours combine.

ActionsDisponibles relit les parametres et valide le compte, le role attribue,
la session SQL et le mode via AutorisationActionService. Les droits de prix
sont ensuite charges en une requete groupee dans la meme transaction.

| Commande | Configuration | Permission, en plus de PRIX_CONSULTER et FACTURIER |
| --- | --- | --- |
| Demande tarif officiel | Delegation active + demandes actives | PRIX_DEMANDER |
| Tarif officiel direct | Delegation active + modifications immediates actives | PRIX_TARIF_MODIFIER |
| Demande article | Delegation active + demandes actives | PRIX_DEMANDER_FACTURE |
| Exception directe article | Delegation active + modifications immediates actives | PRIX_FACTURE_EXCEPTION |

Les boutons produit et panier sont distingues. Le dialogue n'offre que les
actions disponibles, avec quantite et identite de la session demanderesse.
Une exception sans contexte d'article n'est pas proposee. Les approbations/refus
conservent PRIX_APPROUVER / PRIX_REFUSER et leurs controles existants.
La consultation des traces et demandes persiste lorsque la delegation est inactive.

Politique de rafraichissement : commandes masquees initialement, relues a
l'ouverture/activation, a Actualiser et avant ouverture du dialogue. Sur un
formulaire deja ouvert, utiliser Actualiser ou se reconnecter apres une modification
sur un autre poste. Pas de polling SQL agressif. Les services relisent toujours
les droits et parametres lors de l'operation : un bouton temporairement visible
ne permet jamais de contourner un retrait. Une erreur SQL masque les commandes.

Une ligne unique peut etre choisie automatiquement. Avec plusieurs lignes,
le choix courant est conserve par reference d'objet ; une reference retiree
n'est pas reutilisee. L'ajout selectionne la ligne ajoutee ou augmentee. La grille
autorise une seule selection et restaure une cellule visible apres rebinding.

Le nouveau refus PRIX_EXCEPTION_REFUSEE est ecrit apres rollback, avec REFUS
ou ECHEC, jamais SUCCES. L'audit peut lui-meme etre indisponible lors d'une panne
SQL/session invalide : le mecanisme existant conserve alors une alerte technique,
sans pretendre avoir enregistre un evenement en base.

## Deploiement sur copie SQL Server et validation Windows

Aucune nouvelle migration : cette correction utilise le schema 2026101301.
Ne pas executer de reset. Sauvegarder et restaurer une COPIE isolee pour les essais.

1. Dans SSMS connecte a cette copie, executer
   `prototype/tests/Phase2IntegrationValidation.sql`. Le script ne modifie rien.
   Verifier la version 2026101301, les parametres, les droits actifs et le nouveau
   diagnostic par role : ecrans, actions, modes disponibles et raison de refus.
   Le marqueur final ne prouve que les controles SQL, pas le parcours WinForms.
2. Avec Visual Studio et le Developer Pack 4.7.2, restaurer les packages et la
   reference Guna.UI2.WinForms attendue par LoginForm, puis Rebuild Release
   `prototype/WinFormsVB/DevCommerc8ak/DevCommerc8ak.vbproj`. Pointer sa connexion
   sur la COPIE, pas sur CommercialMagDB de production.
3. Modes inactifs : connecter un facturier puis une caissiere existants. Les
   parcours historiques doivent rester utilisables, sans nouvel ecran de mode.
4. Activer les modes separes. Role caisse avec CAISSE + ENCAISSEMENT_CREER,
   sans MODE_CAISSE : selection automatique CAISSE. Role facturier avec
   FACTURIER + FACTURE_CREER, sans MODE_FACTURATION : FACTURATION.
5. Retirer ENCAISSEMENT_CREER sur le role de test : refus explicite a la
   reconnexion si aucun autre mode disponible. Desactiver globalement CAISSE :
   meme refus, sans grant automatique. Restaurer manuellement les choix de test.
6. Attribuer les quatre droits metier mais pas MODE_COMBINE : deux modes separes,
   pas de combine. Attribuer MODE_COMBINE et activer globalement le combine :
   une troisieme option unique. Desactiver l'option globale : elle disparait.
7. Compte multi-roles : selectionner le role caisse uniquement. Les droits du
   role facturier non choisi ne doivent pas fournir le mode combine.
8. Dans le combine, creer une facture de 56 000 FC. Apres enregistrement,
   l'encaissement selectionne cette facture sans paiement automatique. Recevoir
   60 000 FC et rendre 4 000 FC : affecte 56 000 FC, paiement unique, audit avec
   createur/encaisseur reels, session et mode. Reessayer et tester depuis un
   second poste : aucun double encaissement.
9. Quitter avec panier non vide : sauvegarder brouillon, abandonner explicitement
   ou annuler la sortie. Avec paiement en cours, sortie refusee. Apres sortie,
   reconnexion en mode separe ; aucune autre session/poste n'est fermee.
10. Delegation inactive : bouton produit, bouton panier et menu article masques
    meme si le role possede les permissions. Les anciennes demandes/audits restent
    consultables depuis leur consultation autorisee.
11. Delegation active sans permission d'action : boutons masques. Ajouter
    PRIX_CONSULTER + PRIX_DEMANDER_FACTURE seulement : commande panier visible,
    commande tarif officiel absente. Aucune permission accordee automatiquement.
12. Ajouter un article : demande ouverte sans devoir reselectionner une ligne.
    Verifier produit, type, quantite, prix, demandeur, variation et motif. Ajouter
    plusieurs articles : selectionner successivement les lignes, controler la
    bonne cible, puis retirer une ligne et controler l'absence de reference stale.
13. Demande article en attente : prix catalogue et panier inchanges. Responsable
    distinct avec PRIX_APPROUVER approuve ; demandeur applique explicitement.
    Refus avec motif, expiration, changement de quantite/remise ou tentative
    d'utilisation sur un autre article doivent interdire une application invalide.
14. Immediate inactive : aucune action directe, refus meme par appel de service.
    Demandes actives : demandes possibles selon permission. Revoquer une permission
    sur un autre poste : Actualiser/reconnexion masque ; service refuse immediatement.
15. Tarif officiel : autorisation distincte, motif, limite de variation et cout
    comparable. Concurrence : un tarif obsolete doit etre refuse. Tester FIXE,
    COEFFICIENT et un type personnalise homonyme. Pas de modification silencieuse
    d'un panier existant ni de facture historique.
16. Tester une interruption SQL : pas d'action visible utilisable sans controle,
    ni operation reussie annoncée si commit echoue. Verifier les audits apres reprise.
17. Comparer les factures historiques avant/apres ; tester les encaissements
    separes, stock, conversions et remises. Ouvrir A4/80 mm/PDF pour confirmer
    la non-regression visuelle, sans modifier leurs moteurs.

Pour le banc SQL automatise, uniquement dans une COPIE suffixee `_Phase1_TEST` :

```powershell
$env:ERP_PHASE1_TEST_ENABLE = 'YES'
$env:ERP_PHASE1_TEST_CONNECTION = 'Server=localhost;Database=ERP_Phase1_TEST;Integrated Security=True;TrustServerCertificate=True'
dotnet build prototype/tests/AuditMetierSqlTests -c Release
& .\prototype\tests\AuditMetierSqlTests\bin\Release\net472\AuditMetierSqlTests.exe
```

Ne pas recopier app.config de production dans le banc. Il cree des fixtures et
des traces dans la COPIE seulement, exige les configurations initiales inactives,
restaure les parametres en fin de suite et ne reinitialise aucune base. Les scenarios
existants couvrent la consommation unique, les refus, expirations, concurrence et
rollback d'audit. Les nouveaux cas couvrent les modes historiques, le retrait global,
la session invalide, l'etat des commandes et le refus d'exception directe.

## Validations de cette livraison

- 98 controles purs executes avec succes, dont selection, droits historiques,
  absence de combine implicite et matrice des commandes de prix.
- API Release compilee ; avertissement CS1998 preexistant dans AuthService.
- Dashboard Release compile sans erreur.
- Banc d'integration net472 compile ; scenarios SQL NON EXECUTES.
- WinForms complet NON VALIDE dans cet environnement : MSB3644 en build normal ;
  avec references 4.7.2 explicites, BC30002 sur Guna2TextBox dans LoginForm.
- Aucune validation visuelle Windows, aucun test SQL Server ni de charge execute.
- Aucun changement de schema, d'impression/PDF, conversion, stock, reset,
  authentification, calcul des montants ou moteur d'encaissement.

## Fichiers concernes

- Services/ModeTravailRegles.vb et ModeTravailService.vb : compatibilite des modes.
- Services/DelegationPrixRegles.vb et DelegationPrixService.vb : cible stable,
  disponibilite des actions, option immediate et trace de refus.
- Forms/FacturationForm.vb : commandes conditionnelles et selection du panier.
- UI/DialogueDelegationPrix.vb : actions autorisees, quantite et demandeur.
- UI/CartesSecuriteAutorisations.vb : explication des droits historiques/combine.
- tests/AuditMetierCoreTests/Program.vb et AuditMetierSqlTests/Program.vb : regressions.
- tests/Phase2IntegrationValidation.sql : diagnostic administratif en lecture seule.
- tests/CorrectionModesPrixGuide.md : procedure et limites.
