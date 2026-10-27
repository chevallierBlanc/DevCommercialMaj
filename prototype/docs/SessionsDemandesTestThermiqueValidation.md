# Sessions, demandes de prix et ticket de test thermique

## Perimetre et diagnostic

- Administration Utilisateurs : le conteneur imposait une surface defilante
  de 980 x 780 et reservait seulement 150 px aux sessions, titre compris.
  Le bas etait donc hors de la zone visible aux petites resolutions.
  Les sessions sont maintenant dans le volet inferieur d'un SplitContainer
  horizontal, reglable a la souris. La fiche, les actions, les comptes et les
  modes restent ensemble dans le volet superieur defilant. La liste des
  sessions dispose de ses propres barres de defilement et largeurs minimales.
- Demandes de prix : les colonnes etaient fixees principalement a 125 px sans
  retour a la ligne explicite des titres. Les en-tetes sont maintenant
  multilignes, avec une hauteur automatique et une hauteur initiale de 62 px.
  Les colonnes ont une largeur minimale ; le defilement horizontal est conserve.
  Le helper de charte existant reste utilise (fond et selection d'en-tete
  245,245,245 ; texte 52,73,94). La selection des lignes n'est pas modifiee.
- Test thermique : un PrintPage specifique utilisait MarginBounds et une
  hauteur fixe de 420, puis imprimait directement sans consulter l'option
  d'apercu. Il est remplace par FacturePrintRenderer.CreerDocumentThermique,
  identique a la Caisse, et FormApercuTicket. La geometrie, les polices, le
  logo, les mesures, les retours a la ligne et la pagination sont partages.

Le marqueur DiagnosticThermique est faux par defaut : aucun changement de
presentation pour les documents commerciaux existants. Le diagnostic affiche
une reference de test, pas un numero de facture, et exclut les reglements.
Ses trois articles et montants sont fictifs (sous-total 128 730 FC, remise
1 000 FC, total test 127 730 FC). Il ne porte aucune identite de client reel.
Il lit uniquement la configuration habituelle de l'entreprise et de
l'imprimante et le logo configure ; aucun chargement de facture/panier/paiement.
Le nom d'imprimante local est toujours prioritaire. L'option d'apercu est celle
enregistree dans les parametres ; enregistrer sa modification avant le test.
Le bouton conserve l'enregistrement local du choix d'imprimante existant.

Aucune migration, nouvelle requete de sessions, modification de frequence
(5 secondes), de permissions, de prix, de caisse, de stock ou de reset.
Le test A4 et les methodes de dessin A4 sont strictement conserves.

## Controles automatises

Depuis la racine du depot :

```powershell
pwsh -NoProfile -File prototype/tests/AdminThermalContracts.Tests.ps1
dotnet run --project prototype/tests/AdminThermalCoreTests -c Release
dotnet run --project prototype/tests/AuditMetierCoreTests -c Release
git diff --check
```

Les contrats verifient notamment la conservation exacte des methodes metier
et de chargement des utilisateurs/sessions, de la configuration d'imprimante,
du dessin thermique et du test A4. Ils controlent le raccordement du test au
renderer et l'absence d'appels commerciaux dans le jeu fictif. Ce sont des
controles de source, pas une execution des services SQL ou de WinForms.

Sous Windows, compiler l'application complete avec Visual Studio, .NET
Framework 4.7.2 Developer Pack et ses dependances habituelles, puis :

```powershell
dotnet build prototype/tests/AdminThermalWindowsTests -c Release
& .\prototype\tests\AdminThermalWindowsTests\bin\Release\net472\AdminThermalWindowsTests.exe
```

Le banc reference par defaut
`prototype/WinFormsVB/DevCommerc8ak/bin/Release/DevCommerc8ak.exe`.
Si le chemin est different, passer `-p:ErpAssemblyPath=C:\chemin\DevCommerc8ak.exe`
a la compilation du banc. Il controle le vrai plan et les mesures/dessins GDI,
dont les donnees fictives, les grands montants, les accents et plusieurs
largeurs utiles. Il ne se connecte pas a SQL et n'imprime pas physiquement.

## Recette Windows indispensable

Sur une copie de test, puis sur le materiel cible sans perturber les ventes :

1. Tester Administration a 1024x768 et 1366x768, puis maximisee, a DPI 100 %,
   125 % et 150 %. Redemarrer l'application apres changement de DPI.
2. Verifier que titre, en-tetes, plusieurs lignes et barres de defilement des
   sessions restent visibles. Deplacer le separateur et parcourir une liste
   plus longue que le volet. Toutes les colonnes, dont SessionId, utilisateur,
   role de session et dernier ping, doivent rester consultables.
3. Verifier selection du compte, ses roles/modes et les actions habituelles.
   Verifier actualisation des sessions sans double chargement ni nouveaux
   acces SQL dus au redimensionnement. Aucun changement de droits attendu.
4. Ouvrir les demandes de prix : titres longs complets, colonnes non ecrasees,
   defilement horizontal. Rechercher, filtrer, changer de page et actualiser ;
   verifier que les styles persistent et que la selection reste lisible.
   Tester Approuver/Refuser uniquement sur des demandes de la base de test.
5. Dans Peripheriques, selectionner l'imprimante thermique du poste. Enregistrer
   l'option d'apercu activee. Tester : l'apercu doit s'ouvrir sans envoi automatique.
   Fermer sans imprimer : aucun message ne doit affirmer un envoi physique.
6. Relancer et imprimer depuis l'apercu, puis enregistrer l'option desactivee
   et retester l'envoi direct. Verifier logo, centrage, mentions TEST, accents,
   designation longue, conditionnement 10+1, montants, remise et total.
   Aucune mention de paiement ne doit apparaitre.
7. Sur une vraie imprimante 80 mm, verifier marges, largeur utile, absence de
   montants coupes, longueur, eventuelles pages et decoupe du pilote.
   Le logiciel partage le rendu de la Caisse ; la decoupe automatique reste
   une capacite/configuration du pilote, pas une commande ajoutee par ce correctif.
8. Tester une imprimante absente/mal configuree : erreur comprehensible,
   aucune creation commerciale. Verifier egalement que le test A4 est inchange.
9. Sur la copie SQL, utiliser Extended Events filtre sur la session applicative
   pendant le seul test thermique : lectures Parametres attendues, aucune ecriture
   de facture, paiement, mouvement de stock, caisse ou audit metier attendue.
   Ne pas comparer des comptages globaux sur une production multi-utilisateur.
10. Reimprimer une facture existante A4/thermique et une proforma : presentation
    habituelle et informations reelles conservees. Aucune nouvelle vente.

## Validation disponible dans cet environnement

- Resultats executes : 66 contrats de source, 109 controles purs de geometrie
  et pagination, 172 controles metier sans SQL reussis ; `git diff --check`
  reussi. Builds Release API et Dashboard reussis (avertissement CS1998
  preexistant dans AuthService de l'API).
- Compilation partielle VB avec references et LoginForm de substitution :
  reussie sans erreur ni avertissement. Banc Windows/GDI compile avec succes
  contre cette assembly de controle, mais NON EXECUTE.
- Linux : contrats de source et tests purs executables ; compilations API et
  Dashboard possibles. Aucun acces a une base SQL Server ou a une imprimante.
- La compilation WinForms standard requiert les assemblies de reference .NET
  Framework 4.7.2 absentes ici (MSB3644). Une compilation partielle de controle
  avec references temporaires et substitution du LoginForm dependant de Guna
  peut detecter des erreurs VB ; elle ne valide ni le LoginForm reel, ni
  l'application complete, ni son execution sous Windows.
- Le banc Windows/GDI peut etre compile pour controle de syntaxe avec ces
  references, mais son execution, la recette DPI, les essais SQL et l'impression
  physique restent obligatoires sous Windows. Un build ne prouve pas le rendu.
