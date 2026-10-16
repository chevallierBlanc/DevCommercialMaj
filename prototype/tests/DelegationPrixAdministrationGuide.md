# Delegation des prix et Administration Utilisateurs

## Diagnostic et perimetre

Depart : main propre, HEAD/origin/main
4b7cbea6241a348977d1a1238ec42505dda08453. Aucune base Windows accessible ici.
Le code existant contient deja les configurations, permissions, demandes,
approbations, empreintes concurrentes et audits. Aucun nouveau circuit ajoute.

Causes confirmees dans le code :

- ActualiserCommandesPrix masquait les actions refusees. Un gestionnaire ne
  pouvait pas distinguer une permission absente d'un point d'entree inexistant.
- MiseAJourPrixUnitaire reactivait le bouton selon la selection uniquement,
  sans les actions disponibles. Le service refusait ensuite a juste titre.
- Le groupe produits remplissait la hauteur restante mais ses boutons etaient
  fixes a y=477 : ils pouvaient etre coupes sur une fenetre plus courte.
- Le panier et le catalogue avaient des commandes differemment nommees et peu
  explicites. Leur distinction de securite existait pourtant deja en service.
- Une demande officielle identique pouvait etre recreee a chaque soumission.
- Administration utilisait vert/bleu/gris/rouge pour des actions equivalentes,
  une barre sans retour a la ligne et 40 % de la zone centrale pour les sessions.
- Au rafraichissement, AppliquerDroitsActions desactivait Deverrouiller APRES
  avoir charge l'etat verrouille du compte selectionne.

L'etat reel des permissions du facturier Windows n'est PAS connu ici. Le script
DelegationPrixAdministrationDiagnostic.sql permet de le constater sans rien
modifier. Aucun droit sensible n'est attribue ou restaure automatiquement.

## Corrections

Facturation : boutons explicites toujours presents, desactives avec diagnostic
si configuration/droit/selection manque. Catalogue et ligne ont des disponibilites
independantes. Le changement de selection utilise les actions du dernier
diagnostic, jamais le seul nom de role. Le service est relu a l'ouverture et
verifie a nouveau les droits dans la transaction. La zone produits defile sur
les petites fenetres. Les prix du panier ne sont pas remplaces par un rechargement
du catalogue. Une exception acceptée recalcule la ligne et les totaux existants.

Le dialogue refuse un perimetre non autorise meme si un autre droit de prix est
present. Une demande d'exception reussie ferme le dialogue ; son application
ulterieure conserve le controle du contexte exact deja present.

Le service exige Consulter aussi pour modifier/proposer directement une exception,
coherent avec l'entree UI. Les permissions de l'action, du role actif, de session
et d'ecran restent controlees en SQL par AutorisationActionService. Aucun cumul
de roles. Une demande officielle encore valide, identique (auteur, produit,
type, empreinte, prix, motif), est retrouvee sous les verrous existants, sans
nouvelle ligne ni double audit de succes. Une demande differente reste distincte.

Administration : bleu de la charte, surfaces blanches, action Enregistrer bleue,
secondaires blanches bordees, destructives rouges sur fond blanc. Boutons de
hauteur coherente et barre avec retour a la ligne. Informations compte en haut,
repertoire au centre, attribution a droite, sessions compactes en bas.
Defilement disponible si l'ecran est trop petit ; aucun panneau n'est remplace.
L'actualisation du repertoire conserve le compte selectionne ; celle des
attributions conserve le role selectionne. Les evenements,
services, motifs, confirmations et audit d'attribution ne changent pas.

Fichiers applicatifs modifies : Forms/FacturationForm.vb ;
Services/DelegationPrixRegles.vb, DelegationPrixService.vb ;
UI/DialogueDelegationPrix.vb, FormulaireUtilisateurs.vb, ModesUtilisateurPanel.vb.
Tests : AuditMetierCoreTests/Program.vb et AuditMetierSqlTests/Program.vb.
Ajoutes : ce guide et le diagnostic SQL en lecture seule.
Aucune migration, aucun schema ni ancien historique modifie.

## Configuration par le gestionnaire

1. Parametres > Securite : activer Delegation des prix. Activer les demandes
   et/ou la modification immediate. Renseigner variation et marge, et la regle
   sous cout. Ces parametres ne donnent aucune permission a eux seuls.
2. Roles et privileges : pour le role ACTIF concerne, attribuer explicitement
   FACTURIER (acces), PRIX_CONSULTER, puis seulement les actions necessaires :

| Operation | Permission | Parametre requis |
| --- | --- | --- |
| Demander un tarif officiel | PRIX_DEMANDER | Demandes |
| Appliquer un tarif officiel | PRIX_TARIF_MODIFIER | Modification immediate |
| Demander une exception de ligne | PRIX_DEMANDER_FACTURE | Demandes |
| Exception directe de ligne | PRIX_FACTURE_EXCEPTION | Modification immediate |
| Approuver/refuser | PRIX_APPROUVER / PRIX_REFUSER | Acces PARAMETRES ; delegation/demandes pour appliquer |

3. Reconnecter le compte sur le bon role. Les commandes se rechargent a
   l'activation de la fenetre ; les services relisent les droits a chaque action.
   Une revocation est donc refusee cote service meme si un bouton etait affiche.
4. Tarif officiel : selectionner un produit et son type de vente AVANT d'ajouter
   au panier. Cliquer Modifier le prix / Tarif officiel. Le changement ne touche
   que ce tarif (ou son coefficient selon la configuration existante).
5. Prix exceptionnel : selectionner une ligne du panier puis Modifier le prix /
   Cette ligne uniquement. Avec plusieurs lignes, la selection doit etre explicite.
6. Saisir le nouveau prix et un motif, confirmer. Hors limites : demander
   l'approbation avec le droit correspondant ; ne pas donner un droit direct
   supplementaire pour contourner un seuil. Le responsable ne peut pas approuver
   sa propre demande. Reprendre la meme ligne/contexte pour appliquer l'approbation.
7. Sous cout : politique globale ET permission PRIX_SOUS_COUT necessaires ;
   aucune de ces conditions n'est contournee par ce correctif.

Les montants restent en FC selon le modele existant. Le cout comparable est
calcule par LireTarif avec les services de conversion existants ; aucune
conversion monetaire ou physique nouvelle, aucun taux invente.

## Validation Windows et SQL Server obligatoire

Avant tout essai SQL : restaurer une COPIE ISOLEE, pas une production renommee.
Ne pas executer de reset. Verifier serveur/base dans le diagnostic non destructif.
Verifier les migrations existantes, parametres et permissions ; ce correctif
n'en ajoute aucune. Remplacer Compte/Role dans le diagnostic par le cas reel.

1. Compiler sous Visual Studio avec Desktop .NET, Developer Pack 4.7.2 et DLL
   Guna.UI2 reelle. Ne pas deployer un executable avec LoginForm de remplacement.
2. DPI 100/125/150 %, ecrans 1024x768 et 1366x768 : boutons accessibles par
   defilement, champs non superposes, barre d'actions et panneau lateral lisibles.
3. Delegation inactive, permission absente, session expiree : commandes desactivees
   et explication. Tenter les services : refus sans changement du catalogue.
4. Facturier autorise uniquement aux exceptions : catalogue refuse. Inversement,
   tarif officiel seul ne donne pas acces a une exception de ligne.
5. Panier vide : selection produit + type => tarif disponible si habilite.
   Modifier ce tarif, verifier historique/audit avec auteur reel et ancien/nouveau
   prix. Verifier un autre type, un ancien document et le panier deja constitue :
   aucun prix ne doit avoir change silencieusement.
6. Une puis plusieurs lignes : selection explicite, nouveau prix/motif, total de
   ligne et total panier corrects, catalogue inchange. Supprimer toutes les lignes,
   ajouter a nouveau, changer de selection : aucune erreur CurrencyManager.
7. Prix hors variation/marge, sous cout, motif vide : refus ; brouillon intact.
   Demande, approbation par un autre compte, refus avec motif, expiration : etats
   corrects. Une seule application au bon produit/type/quantite/contexte.
8. Soumission officielle identique et concurrente : une demande active, un audit
   de creation. Modification concurrente de tarif : conflit et pas d'ecrasement.
   Simuler echec d'audit sur copie : prix et historique annules ensemble.
9. Administration : selectionner deux comptes a tour de role. Verifier nom, role
   principal, roles coches, etat et attribution a droite. Modifier puis actualiser
   et reconnecter. Aucun droit ne doit migrer d'un compte ou role vers un autre.
10. Attribution combinee : autoriser seulement CAISSIERE, pas FACTURIER, avec motif.
    Actualiser conserve le role ; retrait persiste. Responsable sans permission :
    lecture seule/refus service. Ne pas changer les droits par defaut.
11. Compte verrouille : Deverrouiller active apres rafraichissement ; compte actif
    non verrouille : bouton inactif. Reset MDP, ajout et modification de compte
    conservent leur fonctionnement et leurs restrictions.
12. Non-regression : connexion avec choix role/mode, Vente & Encaissement,
    suspension/reprise, facture puis paiement unique, stock, devises et caisse,
    impression A4/80 mm et anciennes factures. Aucun moteur concerne n'a ete edite.

Depuis Developer PowerShell, racine du depot :

```powershell
msbuild prototype\WinFormsVB\DevCommerc8ak\DevCommerc8ak.vbproj /restore /p:Configuration=Release /p:GunaUI2AssemblyPath="C:\Libs\Guna.UI2.dll"
dotnet run --project prototype/tests/AuditMetierCoreTests -c Release
msbuild prototype\tests\PanierWinFormsTests\PanierWinFormsTests.vbproj /restore /p:Configuration=Release
& .\prototype\tests\PanierWinFormsTests\bin\Release\net472\PanierWinFormsTests.exe
$env:ERP_PHASE1_TEST_ENABLE='YES'
$env:ERP_PHASE1_TEST_CONNECTION='Server=.\ERPCOMMERCIAL_TEST;Database=ERP_Phase1_TEST;Integrated Security=True;TrustServerCertificate=True'
msbuild prototype\tests\AuditMetierSqlTests\AuditMetierSqlTests.vbproj /restore /p:Configuration=Release
& .\prototype\tests\AuditMetierSqlTests\bin\Release\net472\AuditMetierSqlTests.exe
Remove-Item Env:ERP_PHASE1_TEST_ENABLE
Remove-Item Env:ERP_PHASE1_TEST_CONNECTION
```

Le banc SQL ajoute des fixtures uniquement avec cet opt-in et suffixe de base.
Il n'est pas le diagnostic en lecture seule. Il couvre deja prix officiels,
exceptions/approbations, couts comparables, audit transactionnel, anciennes
donnees et attributions par role ; nouveaux controles : revocation de Consulter,
rejeux concurrents et absence de doublon d'audit.

## Limites

Aucun serveur SQL Server ou execution WinForms disponible ici. Les scenarios
d'integration, l'apparence aux differents DPI et les impressions physiques
restent NON VALIDES sous Windows. La reussite d'une compilation n'est pas
une validation metier ou visuelle. Aucun gain de capacite multi-utilisateur
n'est annonce sans mesure ; la recherche de demande est bornee au contexte,
mais un index supplementaire ne sera justifie que par les plans d'execution reels.

## Resultats effectivement obtenus sous Linux

- AuditMetierCoreTests : 139 controles reussis, executes sans SQL.
- API Release : compilation reussie ; CS1998 preexistant, aucun fichier API edite.
- Dashboard Release : compilation reussie, sans avertissement.
- AuditMetierSqlTests et PanierWinFormsTests : compilation reussie avec les
  references officielles net472 temporaires ; execution NON EFFECTUEE.
- WinForms standard : echec MSB3644, targeting pack 4.7.2 absent.
- WinForms avec references temporaires : echec sur Guna2TextBox absent.
- Controle isole des autres formulaires/services : compilation reussie avec
  LoginForm remplace par un placeholder EXTERNE au depot. Aucun livrable de
  production n'est valide par cette substitution ; ne pas le deployer.
- Aucun acces a SQL Server, aucun test graphique Windows ni impression physique.
- git diff --check : aucune erreur. Aucun fichier du moteur d'impression,
  conversion, stock, paiement, connexion, choix du mode ou reset modifie.
