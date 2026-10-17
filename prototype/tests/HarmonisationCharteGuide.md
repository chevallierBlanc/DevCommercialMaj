# Harmonisation UI ciblee Commercial Pro

## Perimetre et etat initial

Depart : main propre, HEAD et origin/main a
99394965dc4da4d1d83de99ba3419015cf676d64.
Pas de capture jointe accessible ici : inspection des descriptions et du code.
Aucune requete sur la base de production, aucune migration necessaire.

Fichiers applicatifs :

- UI/FormulaireUtilisateurs.vb : uniquement en-tete et en-tetes des deux grilles.
  Aucun changement de taille, position, bouton ou evenement de ce formulaire.
- UI/DialogueDelegationPrix.vb : presentation du dialogue Afficher uniquement.
  Aucun changement des services, permissions, seuils, prix, calculs ou decisions.
- Forms/FacturationForm.vb : style des boutons et en-tetes des deux grilles.
  Aucun repositionnement, raccourci, evenement ou traitement commercial modifie.
- UI/FormulaireClients1.vb (classe FormulaireClients) : alignement des champs,
  boutons, largeurs de colonnes et presentation du graphique existant.
- Services/ThemeHelper.vb (module ThemeHelper1) : methodes explicites et ciblees,
  pas de nouveau theme ni modification du parcours du theme global existant.

Autres : banc PanierWinFormsTests (projet et Program), lien du helper visuel
existant dans le projet AuditMetierSqlTests (qui compile deja le dialogue), et
ce guide. Aucun ajout de dependance externe.
Le fichier Clients, auparavant Windows-1252, est converti en UTF-8 avant edition.
Les accents sont conserves ; les messages metier ne sont pas reecrits.

## Couleurs exactes

En-tete Administration et bandeau du dialogue : Color.FromArgb(52,73,94).
En-tete Clients deja conforme, conserve.

Le helper StyliserEnteteCommerciale applique aux grilles concernees :

```vb
grille.EnableHeadersVisualStyles = False
With grille.ColumnHeadersDefaultCellStyle
    .BackColor = Color.FromArgb(245, 245, 245)
    .SelectionBackColor = Color.FromArgb(245, 245, 245)
    .ForeColor = Color.FromArgb(52, 73, 94)
    .SelectionForeColor = Color.FromArgb(52, 73, 94)
End With
```

Ne touche ni DefaultCellStyle.SelectionBackColor, ni SelectionForeColor des
cellules, ni RowHeadersDefaultCellStyle. Les selections de lignes sont preservees.
Application dans les factories de grilles, pas de boucle globale sur les autres
formulaires. Les chemins de chargement et de rafraichissement examines ne
redefinissent pas ces couleurs. Les appels de theme global de ces formulaires
sont commentes ; aucune modification des themes des autres modules.

Action principale : bleu existant 41/128/185. Secondaire, annulation et retour :
fond blanc, texte 52/73/94. Destruction : texte/bordure rouge discret 192/57/43.
Desactive : fond 245/245/245, texte 117/117/117 et bordure neutre.
Hover/focus natifs FlatStyle.Flat conserves ; couleurs de survol/pression harmonisees.
StyliserActionCommerciale ne change ni Enabled, ni Visible, ni Bounds, ni Click.
Le gestionnaire EnabledChanged actualise seulement les couleurs.

## Presentations

Dialogue : format initial 680x620 client au lieu de 780x780 externe. Bandeau
produit/type/code-barres ; informations tarifaires ; prix propose compact ;
variation/marge avec les calculs d'origine ; justification et responsable.
Erreur de motif reprise depuis la validation existante, affichee pres du champ,
sans changer le refus ni les messages existants. Corps defilant si necessaire,
pied de commandes separe. Seules les actions deja autorisees sont creees.
Redimensionnement et limite a l'espace de travail de l'ecran ; DPI a verifier.

Facturation : Ajouter/Valider principaux, commandes de consultation/impression,
prix, annulation et deconnexion neutres ; Retirer rouge discret. Positions,
dimensions, raccourcis et logique de visibilite inchangees. Aucun changement du
choix d'imprimante, des moteurs A4/80 mm ou du PDF. Aucun code du formulaire
combine modifie ; ses boutons herites de Facturation suivent ces memes styles.

Clients : trois colonnes de saisie, adresse regroupee, telephone/credit de
largeur bornee, case Client actif conservee. Colonnes de noms/adresses/email
flexibles avec minimum ; identifiants, telephone et montants plus compacts.
LimiteCredit/TotalAchats/MoyenneAchat formates N2 a droite pour affichage
uniquement. La precision des valeurs et les calculs SQL ne changent pas.
Graphique : titre moins encombrant, legende et infobulles pour les points deja
charges, hauteur minimale et defilement de la zone inferieure. Aucun chiffre
ajoute ni requete/statistique modifiee.

## Controles statiques effectues

- Comparaison des methodes Clients a partir d'ObtenirService avec HEAD, apres
  normalisation de l'encodage : identiques, y compris CRUD, limites de credit,
  selection, requetes des statistiques et alimentation des points du graphique.
- Calcul actualiserVariation du dialogue : identique.
- Appels service et conditions de permissions/perimetre du dialogue : identiques.
- Evenements Click/AddressOf et raccourcis de Facturation : identiques.
- Diff Administration : seules couleurs hero/en-tetes, aucun agencement modifie.
- Aucun moteur d'impression, reset, SQL, repository, service metier ou conversion
  edite. Les couleurs de selection des lignes ne sont pas remplacees.

## Validation Windows obligatoire

Sur copie de base uniquement pour les essais CRUD/prix ; ne pas lancer de reset.
Visual Studio : workload Desktop .NET, Developer Pack 4.7.2, DLL Guna.UI2 reelle.

```powershell
msbuild prototype\WinFormsVB\DevCommerc8ak\DevCommerc8ak.vbproj /restore /p:Configuration=Release /p:GunaUI2AssemblyPath="C:\Libs\Guna.UI2.dll"
msbuild prototype\tests\PanierWinFormsTests\PanierWinFormsTests.vbproj /restore /p:Configuration=Release
& .\prototype\tests\PanierWinFormsTests\bin\Release\net472\PanierWinFormsTests.exe
dotnet run --project prototype/tests/AuditMetierCoreTests -c Release
```

1. Administration : en-tete sombre et deux grilles grises, sans selection puis
   avec selection, clic/tri des colonnes, rafraichissement et sessions timer.
   Verifier les quatre proprietes exactes dans le debogueur VS.
2. Verifier que tailles, boutons, compte selectionne, roles, attribution combinee,
   reset MDP, deverrouillage/desactivation et audit sont inchanges.
3. Dialogue : tester tarif officiel et exception, permissions accordees/refusees,
   demande seule/direct/approbation ; aucun bouton d'action non autorise.
4. Produit et type longs, code-barres long, motif multiligne, erreur de motif,
   tres gros prix, variation et marge : lisibles et chiffres identiques a avant.
5. Tester 1024x768, 1366x768 et DPI 100/125/150 %, Tab/Maj-Tab/Echap, pied de
   dialogue accessible, pas de controles superposes ou tronques.
6. Facturation : positions identiques ; Ajouter/Retirer, commande de prix produit
   et de ligne, Valider, Imprimer, PDF, Historique, Annuler, Deconnexion et leurs
   raccourcis fonctionnent comme avant. Etats disabled puis enabled lisibles.
7. Clients : creer et selectionner un client de test, modifier nom/telephone/
   email/adresse/credit/actif, actualiser puis relire les valeurs persistantes.
   Suppression conserve sa confirmation et ses protections relationnelles.
8. Clients : noms longs, liste vide/volumineuse, colonnes triees, defilement et
   selection. Top Clients puis selection d'un client : graphique et legende
   coherents avec les memes lignes SQL, aucun chiffre supplementaire.
9. Prix sous cout, remise, refus de permission et conflit concurrent : protections
   existantes conservees. Anciennes factures et tarifs historiques inchanges.
10. Essai rapide facture/caisse classiques et Vente & Encaissement, attente/reprise,
    paiement unique, ticket/A4/PDF. Ne pas confondre apparence avec validation
    financiere : comparer les documents/paiements et mouvements sur la copie.

Les tests Windows ajoutes utilisent les vrais helpers : quatre couleurs
d'en-tete apres rebind, selection des lignes inchangee, etats des boutons,
position/visibilite et Click preserves. Leur compilation ne vaut pas execution.

## Resultats executes dans l'environnement Linux

- AuditMetierCoreTests : 139 controles PASS, reellement executes sans SQL.
- API Release : compilation reussie, avertissement CS1998 preexistant.
- Dashboard Release : compilation reussie sans avertissement.
- PanierWinFormsTests et AuditMetierSqlTests : compilation reussie avec les
  assemblies de reference net472 officielles temporaires ; NON EXECUTES.
- Application WinForms standard : echec MSB3644, Developer Pack 4.7.2 absent.
- Application avec les references temporaires : echec sur Guna2TextBox absent.
- Compilation isolee des autres formulaires/services : reussie, avec LoginForm
  remplace par un placeholder EXTERNE au depot. Ceci n'est pas une validation du
  binaire complet et ce resultat ne doit pas etre deploye.
- Comparaisons statiques decrites plus haut : aucune difference metier detectee.
- git diff --check : sans erreur.

Validation visuelle Windows, tests DPI et integration SQL Server : NON EFFECTUES.
Aucune reussite de CRUD, d'approbation ou d'impression sous Windows n'est annoncee.
