# Regression SelectedIndex du dialogue de prix

## Diagnostic

Base examinee : main / 9a6adc43f59512fbbfb3afee8e178d73d54d5e65.
Dans UI/DialogueDelegationPrix.vb, Afficher, lignes 69-70 avant correction,
choixTarif recevait sa DataSource puis SelectedIndex avant d'etre rattache au
formulaire. Une source non vide ne garantit pas des Items deja materialises :
la liaison WinForms depend du BindingContext. FindIndex renvoyait 0 pour le
premier tarif alors que le controle pouvait encore avoir zero Item.

Le chemin FacturationForm.ModifierPrixDelegue (ligne 891 avant correction)
fournit le produit et l'identifiant du tarif exact. Son autre SelectedIndex=0,
pour cmbUnite, est deja protege par Items.Count et n'est pas modifie.
Le catalogue DataGridView n'effectue aucune selection indexee. Le dialogue
n'utilise pas SelectedIndexChanged ; SelectionChangeCommitted est raccorde
apres initialisation et ne represente que le choix explicite de l'utilisateur.

## Correction

La partie Selection du MEME DialogueDelegationPrix remplit la petite liste
locale directement, sans DataSource differee et sans nouvelle source SQL.
L'index est obtenu par Items.Add puis valide contre Items.Count. La selection
doit correspondre a l'identifiant ET au nom du tarif ; aucun repli arbitraire
sur le premier type. Une liste vide/absente ou un tarif introuvable laisse
SelectedIndex=-1 et le choix desactive. Afficher refuse explicitement un
catalogue vide ou une selection introuvable avant toute action commerciale.

L'objet choisi devient la source des prix/achat/cout/coefficient et de
l'empreinte du dialogue : la selection et les informations affichees partagent
le meme instantane. Une exception reste sur l'objet de sa ligne. Aucun calcul,
permission, transaction, approbation, audit, tarif historique ou design modifie.
Le gestionnaire utilisateur ignore aussi une selection absente ou inchangee.

## Tests Windows sans SQL

Developer Pack .NET Framework 4.7.2 et outils VB.NET Desktop requis.

```powershell
msbuild prototype\tests\PanierWinFormsTests\PanierWinFormsTests.vbproj /restore /p:Configuration=Release
& .\prototype\tests\PanierWinFormsTests\bin\Release\net472\PanierWinFormsTests.exe
```

Le banc lie directement la methode de selection de production. Il verifie :
controle sans parent/handle, rattachement puis affichage, un type, plusieurs
standards, type personnalise homonyme distingue par ID, conservation des
donnees tarifaires, source modifiee apres chargement, liste vide, source absente,
tarif introuvable, rechargement apres liste vide, exception verrouillee et
absence de SelectionChangeCommitted provoque par l'initialisation.

## Acceptation Windows / SQL Server sur copie isolee

1. Compiler le vrai WinForms Release avec la DLL Guna.UI2 configuree.
2. Facturation, panier vide : selectionner un produit eligible avec un seul
   type actif puis Modifier le prix. Verifier type, prix, achat, cout et marge.
3. Refaire avec plusieurs standards puis un type personnalise FIXE/COEFFICIENT ;
   choisir un autre type, verifier ses valeurs et la confirmation d'abandon
   d'une proposition non enregistree. Aucun type desactive ne doit etre edite.
4. Retirer/desactiver un type sur l'autre poste avant ouverture : liste coherente
   ou refus explicite, jamais selection arbitraire. Tester aussi le produit sans
   aucun type actif (commande indisponible ou explication).
5. Enregistrer un tarif autorise avec motif. Verifier en SQL le prix du type
   exact, HistoriquePrixProduits pour le standard et JournalAudit : auteur reel,
   ancien/nouveau prix et marge. Les autres tarifs/factures restent inchanges.
6. Verifier les nouveaux prix en Produits et Stock par le bandeau de rechargement
   controle, et en Facturation sur une nouvelle ligne. Panier deja saisi intact.
7. Exception facture : seule la ligne change ; le catalogue reste intact.
8. Tester permission retiree, delegation inactive, motif vide, seuil depasse,
   approbation/refus/expiration et tarif concurrent. Aucun succes fictif.
9. Executer le banc AuditMetierSqlTests sur la copie suffixee _Phase1_TEST
   selon TarificationClientsFinanceGuide.md. Il couvre deja persistance,
   approbations, refus, concurrence, rollback audit et tarifs actifs.

Le diagnostic TarificationClientsFinanceDiagnostic.sql reste en lecture seule.
Le banc SQL ajoute des fixtures : jamais sur la production. Aucune migration
nouvelle, aucun reset et aucune connexion de production dans cette correction.

## Resultats disponibles sous Linux

- 148 tests AuditMetierCoreTests executes : reussis, sans SQL.
- Compilation Release API reussie (avertissement CS1998 preexistant) et
  Dashboard reussie. Aucune modification de ces projets.
- Compilation PanierWinFormsTests reussie avec references Microsoft 4.7.2
  temporaires. Nouveaux tests de selection Windows NON EXECUTES.
- WinForms standard : MSB3644, targeting pack 4.7.2 absent ; avec references
  temporaires, DLL Guna.UI2 toujours manquante. Compilation isolee hors depot
  des sources avec remplacement temporaire de LoginForm : reussie, mais ce
  n'est PAS le build complet du livrable ni une execution Windows.
- SQL Server, persistance reelle et propagation multi-postes : NON EXECUTES.
  Les services SQL ne sont pas modifies ; leur verification reste obligatoire
  sur copie. AuditMetierSqlTests compile avec la nouvelle partie Selection,
  mais n'a pas ete execute.
- git diff --check : reussi.

Aucune validation visuelle Windows, impression ou transaction SQL n'est annoncee.

## Fichiers de cette correction

- UI/DialogueDelegationPrix.vb : initialisation et selection utilisateur.
- UI/DialogueDelegationPrix.Selection.vb : partie testable du meme dialogue.
- DevCommerc8ak.vbproj : inclusion de cette partie.
- tests/PanierWinFormsTests/Program.vb et son projet : scenarios de chargement.
- tests/AuditMetierSqlTests/AuditMetierSqlTests.vbproj : inclusion liee.
- tests/DialoguePrixSelectionValidation.md : diagnostic et procedure.
