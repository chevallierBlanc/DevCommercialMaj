# Vente et Encaissement : ergonomie, attente et reimpression

## Etat initial et diagnostic

main propre, HEAD et origin/main : b407b879a311fe2e3a1dcf79c349c4fb85d956ca.
Aucune capture n'etait accessible dans la conversation ; les descriptions et
le code ont servi de base. Aucun acces a la base Windows de Tatiana ici.

- Choix du role : hauteur fixe de 400 px, diagnostic de 140 px et liste de
  taille residuelle. L'identite de l'utilisateur n'etait pas indiquee.
- Choix du mode : fenetre de 620 x 460, choix de largeur fixe, libelles
  techniques. Aucune modification necessaire de la resolution des droits.
- Reglement : controles etires par DockStyle.Top sur toute la largeur.
  NumericUpDown.ValueChanged n'observe pas toutes les frappes avant validation
  du texte. La monnaie affichait zero meme lorsqu'il manquait de l'argent.
- Impression : LireDocument imposait ValideeParUtilisateurId=utilisateur
  courant ET OperationComptoirId non NULL. C'est un mauvais critere de
  propriete pour l'encaisseur lorsque createur/validateur/encaisseur different.
  Le meme filtre etait utilise pour l'impression immediate et les deux boutons.
- La derniere facture n'etait qu'un entier en memoire, perdu a la fermeture.
- Attente : vrai enregistrement EN_ATTENTE deja present, sans paiement ni
  StockSortie, mais bloque par le controle UI de stock d'une vente finale.
  La reprise fermait son dialogue meme si le chargement avait echoue et pouvait
  commencer a remplacer le panier avant d'avoir lu toutes les lignes.

Limite importante : pour une vente NOUVELLE creee, validee et encaissee par la
meme personne, le filtre ancien devrait normalement correspondre. Sans SQL
Windows, on ne peut pas affirmer quel champ de la vente de Tatiana manque,
ni exclure un trigger propre a cette base ou un mauvais ID. Executer
ComptoirImpressionDiagnostic.sql pour etablir cette cause sur la base reelle.
Le correctif traite le mauvais critere d'auteur sans inventer ni remplir les
anciens validateurs. Il ne supprime pas les controles de session.

## Comportement corrige

### Interfaces

DialogueRoleSession : compacte, nom authentifie, liste des seuls roles fournis,
mention principal, diagnostic court et details accessibles separement,
Annuler/Continuer et clavier. Dimensions selon le nombre de roles, DPI et
limitation a la surface de travail. Login utilise ce dialogue sans changer
l'authentification, l'attribution des roles ni leur validation SQL.

DialogueModeTravail : format compact, libelles francais, role/utilisateur,
parcours existants et case combinee uniquement si le service l'autorise.
La preference du parcours classique et l'exclusivite restent inchangees.

ReglementComptoirPanel : carte a deux colonnes, controles plafonnes en largeur,
total prominent, etat financier visible, boutons principaux conserves,
defilement si necessaire. Les references externes ne s'affichent que pour
les moyens concernes. TextChanged recalcule pendant la frappe, sans blur.
Vide/invalide/negatif/precision excessive : confirmation desactivee.
Insuffisant : montant manquant, pas de rendu negatif. Externe : montant exact,
reference et confirmation humaine necessaires, pas de rendu fictif.

### Devises et arrondi

Le schema FacturesVente n'a pas de devise de facture : ce parcours exprime
les totaux en FC, comme la caisse existante. Ne pas reinterpreter ces totaux
avec DeviseParDefaut. Le montant recu peut etre FC ou USD ; la monnaie est FC.
Il n'existe pas de service monetaire central expose : la caisse utilisait un
helper prive montant*taux. La meme convention est reutilisee dans le helper
pur EncaissementAuditRegles pour l'apercu et le service combine.

TauxUsd est lu depuis les parametres existants, puis recontrole sous transaction
lors de la confirmation. USD absent du choix si taux non configure, aucun taux
invente. Deux decimales normalisees (precision de Paiements, ToEven comme le
controle existant), huit au plus pour le montant d'origine. Le montant original
et le taux restent persistes/audites. Aucun parametre d'arrondi additionnel
n'existe dans le schema examine. Les calculs de facture ne sont pas modifies.

### Attente / reprise

Meme brouillon, memes repositories et validations de prix ; pas de nouveau
systeme d'attente. Le stock UI ne bloque plus sa suspension, mais est controle
a la finalisation par le moteur existant. Prix/remises/sous-cout restent
controles : un refus commercial n'est pas une panne du bouton.
Le motif de suspension est demande et audite, les raisons de prix de ligne
restent persistees. Client, quantites, unites, prix, remise et auteur sont relus.
La liste exclut les documents avec paiements, reste limitee a 100 resultats
recherchables et indique PreparePar.

La reprise conserve les controles FACTURE_MODIFIER et les versions ; aucun
droit nouveau n'est accorde. Le chargement construit ses articles avant de
remplacer le panier et renvoie son resultat au dialogue de reprise. Les prix
conserves ne sont pas remplaces silencieusement par les tarifs actuels.
Les controles de securite restent ceux des brouillons existants ; les regles
de sous-cout ne sont ni supprimees ni affaiblies.

Double clic UI garde par _validationEnCours ; index unique existant sur les
contextes de lignes contre une recreation identique. Le banc SQL ajoute un
test de repetition, de reprise/modification et de paiement apres attente.
Ne pas pretendre a une reprise automatique apres crash : en cas de resultat
SQL incertain, consulter les documents/audits avant toute ressaisie.

### Impression / reimpression

Le document doit etre PAYEE, avoir un paiement reel par le compte courant,
et provenir du comptoir (marqueur OperationComptoirId ou evenement SUCCES
ENCAISSEMENT_VALIDE en mode combine du meme encaisseur). Les droits du role
actif, la session et l'attribution combinee sont reverifies en SQL.
Pas de droit de consultation generale d'autres comptes, pas d'heritage de role.
Le droit existant de caisse suffit a ses propres documents sous ce contexte ;
aucun privilege d'impression nouveau n'est invente ou attribue.

Impression immediate : ID exact du commit. Derniers boutons : recherche SQL
du dernier document accessible, y compris apres reconnexion, sans lire le
panier courant. A4 et 80 mm utilisent toujours FacturePrintRenderer et la
configuration d'imprimante existante. Ce chargement ne fait aucun INSERT,
UPDATE, paiement, mouvement ou modification de facture.

L'echec de l'imprimante ne revient jamais a Finaliser et ne remet pas la vente
en attente. Le message explique la reimpression ulterieure. Une erreur de
consultation ne pretend pas qu'un document a ete imprime.

## Validation Windows / SQL Server obligatoire

Sur copie isolee uniquement, migrations precedentes jusqu'a 2026101501 requises.
Aucune migration nouvelle dans ce correctif. Ne pas lancer de reset.

1. Executer ComptoirImpressionDiagnostic.sql avec le vrai compte de connexion.
   Identifier l'ID/numero precis refuse, le validateur, l'encaisseur et l'origine.
   Examiner les triggers en lecture seule si les champs ne sont pas persistes.
2. Compiler sous VS avec workload Desktop .NET, Developer Pack 4.7.2 et vraie
   DLL Guna.UI2 licenciee. Voir les commandes des guides precedents.
3. Role et mode : Tatiana FACTURIER sans combine, CAISSIERE avec case autorisee,
   case decochee/cochee, clavier Tab/Entree/Echap, principal visible.
4. Ecrans a 100/125/150 % et resolutions 1024x768, 1366x768, 1920x1080 : listes,
   texte, defilement, boutons, carte de reglement et absence de chevauchement.
5. Total 56 000 : recu 56 000, 60 000, 55 000, vide, invalide, decimales locales,
   modifications successives SANS quitter le champ : rendu 0/4 000, insuffisance
   1 000 et confirmation impossible pour les saisies invalides.
6. USD avec taux officiel : 24 USD a 2 500 = 60 000 FC, rendu 4 000 FC. Tester
   taux absent puis modifie avant confirmation : pas de taux invente ni paiement
   obsolete. Verifier les arrondis et valeurs d'origine en SQL.
7. Paiement externe sans reference/confirmation refuse ; excedent refuse ;
   pas de confirmation automatique d'une passerelle inexistante.
8. Mettre en attente : client, telephone, articles, conditionnements, prix,
   remise et motif conserves ; paiement/caisse/stock non modifies. Suspendre
   avec stock devenu insuffisant ; la vente finale doit etre refusee si le
   stock n'a pas ete reapprovisionne.
9. Reprise, modification, validation : version concurrente refusee, prix non
   remplaces silencieusement, paiement unique et stock correct. Sous-cout
   refuse et panier intact. Droits de modification necessaires explicitement.
10. Vente valide : imprimer immediatement, puis refuser l'impression proposee
    sur une autre vente. Les deux ventes restent payees sans second encaissement.
11. Dernier ticket et derniere A4 : avant/apres reconnexion, nom long,
    conditionnement long, meme client et memes totaux que l'historique.
12. Imprimante indisponible : vente conservee, retenter le bouton apres remise
    en service ; nombre de paiements et mouvements identique avant/apres.
13. Encaisseur different du validateur : son document reste accessible ; le
    document d'un autre encaisseur et un brouillon restent refuses.
14. Revocation du mode/droit, session expiree, mode FACTURIER : reimpression
    du comptoir refusee par le service. Les parcours classiques restent testes.

Bancs : AuditMetierCoreTests (sans SQL) ; PanierWinFormsTests (vrais controles,
tests de frappe, role, mode et liaison) ; AuditMetierSqlTests (copie *_Phase1_TEST
avec opt-in documente, transactions, attente/reprise, lecture de documents et
absence d'effet financier de relectures). Ces bancs ne prouvent pas qu'une
imprimante physique a imprime correctement.

## Fichiers et limites

Modifies : DevCommerc8ak.vbproj ; Forms/LoginForm.vb, FacturationForm.vb,
VenteEncaissementForm.vb ; Services/VenteEncaissementService.vb,
EncaissementAuditRegles.vb ; UI/DialogueModeTravail.vb ; projets/programmes
AuditMetierCoreTests, AuditMetierSqlTests et PanierWinFormsTests.
Nouveaux : DTOs/ReglementComptoir.vb (classe deplacee, meme type),
UI/DialogueRoleSession.vb, UI/ReglementComptoirPanel.vb,
ComptoirImpressionDiagnostic.sql et ce guide.

Reglement integral dans le parcours combine, paiements partiels dans la caisse
existante ; pas de nouvelle integration externe ni d'internationalisation de
la devise des anciennes factures. Aucune modification des moteurs d'impression,
de conversion de stock, des permissions, du reset ou des donnees de production.
L'ancien filtre d'impression est demontre dans le code, mais le champ fautif
du document Windows precis reste a confirmer par le diagnostic SQL.

## Controles executes dans cet environnement

- AuditMetierCoreTests : 130 controles PASS, reellement executes sans SQL.
- API Release : compilation reussie, avertissement CS1998 preexistant.
- Dashboard Release : compilation reussie sans avertissement.
- AuditMetierSqlTests et PanierWinFormsTests : compilation reussie avec les
  assemblies de reference officielles net472 ; execution NON EFFECTUEE.
- Application WinForms standard : MSB3644 (targeting pack absent). Avec les
  references net472 fournies temporairement : BC30002 sur Guna2TextBox.
- Verification isolee des autres formulaires/services avec LoginForm remplace
  par un placeholder EXTERNE au depot : compilation reussie. Ce controle ne
  valide ni LoginForm complet, ni le livrable Windows de production.
- Aucun serveur SQL, aucune execution WinForms, aucune imprimante testes.
- git diff --check : sans erreur. Aucun moteur d'impression ou reset modifie.

## Commandes Windows reproductibles

Developer PowerShell Visual Studio, workload Developpement Desktop .NET,
Developer Pack 4.7.2, SDK .NET requis par les autres projets et vraie DLL Guna.
Depuis la racine du depot :

```powershell
msbuild prototype\WinFormsVB\DevCommerc8ak\DevCommerc8ak.vbproj /restore /p:Configuration=Release /p:GunaUI2AssemblyPath="C:\Libs\Guna.UI2.dll"
dotnet run --project prototype/tests/AuditMetierCoreTests -c Release
msbuild prototype\tests\PanierWinFormsTests\PanierWinFormsTests.vbproj /restore /p:Configuration=Release
& .\prototype\tests\PanierWinFormsTests\bin\Release\net472\PanierWinFormsTests.exe
```

Pour le banc SQL, restaurer au prealable une COPIE JETABLE isolee, appliquer
les migrations existantes, et verifier serveur/base dans SSMS. Le banc ajoute
des fixtures et n'est PAS le script de diagnostic en lecture seule. Ne jamais
utiliser la production, meme renommee avec un suffixe de test.

```powershell
$env:ERP_PHASE1_TEST_ENABLE = 'YES'
$env:ERP_PHASE1_TEST_CONNECTION = 'Server=.\ERPCOMMERCIAL_TEST;Database=ERP_Phase1_TEST;Integrated Security=True;TrustServerCertificate=True'
msbuild prototype\tests\AuditMetierSqlTests\AuditMetierSqlTests.vbproj /restore /p:Configuration=Release
& .\prototype\tests\AuditMetierSqlTests\bin\Release\net472\AuditMetierSqlTests.exe
Remove-Item Env:ERP_PHASE1_TEST_ENABLE
Remove-Item Env:ERP_PHASE1_TEST_CONNECTION
```

Executer ensuite les 14 scenarios manuels ci-dessus. Comparer les nombres
de paiements et sorties avant/apres chaque reimpression et garder les preuves
du document precis qui avait echoue, ainsi que les resultats du diagnostic.
