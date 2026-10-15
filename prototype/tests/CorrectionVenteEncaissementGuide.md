# Correction du parcours Vente & Encaissement

## Etat et causes confirmees dans le code

Depart : main propre, HEAD et origin/main
`fcb2fcccfec5fdde203fb16b04bb2d5449d0ead3`.

1. La migration 2026101501 initialise chaque attribution individuelle a 0,
   volontairement. Un role eligible et un parametre global actif ne creent pas
   cette attribution. Le message au login correspond a cette condition ; sans
   acces a la base de Tatiana, sa valeur reelle n'est PAS confirmee ici.
2. L'administration cachait entierement la commande si le droit nouveau
   UTILISATEUR_MODES etait absent. De plus, le huitieme bouton pouvait passer
   sur une seconde ligne hors de la barre de 60 px. Il n'existait pas de section
   persistante pour comprendre/configurer cette attribution.
3. MainForm ouvrait VenteEncaissementForm, tandis que le nom historique
   FacturationEtCaisseForm designait encore une juxtaposition de deux modules.
   Le nom attendu et le parcours reel etaient donc ambigus.
4. Le paiement du nouveau parcours etait un dialogue modal, et non une zone
   de l'interface principale. Le controle SQL existait lors de la finalisation,
   mais pas a l'ouverture du formulaire (seul le mode local etait controle).
5. Le diagnostic de prix expliquait essentiellement les droits d'exception
   de ligne, meme lorsque le besoin concernait le tarif officiel. Le bouton
   produit etait deja distinct du panier : cette separation est conservee.

## Corrections

- Administration Utilisateurs : section permanente **MODES PAR ROLE**, a droite
  de la liste des comptes/sessions, avec defilement interne. Choix parmi les
  roles effectivement enregistres du compte selectionne, droits classiques,
  attribution combinee, blocages, case, motif et enregistrement explicites.
- Si l'administrateur n'a pas le privilege necessaire, la section reste visible
  avec une explication. Aucun grant implicite ne contourne cette restriction.
- Le service relit eligibilite et association ; le bool attendu protege contre
  les modifications concurrentes. L'audit reste dans la meme transaction.
- Absence de colonne : diagnostic de migration explicite avant toute requete
  qui reference cette colonne, y compris dans la resolution au login.
- Role puis mode au login : parcours conserve, case combinee seulement dans
  les modes renvoyes par le service. Aucun cumul de roles, aucun auto-grant.
- FacturationEtCaisseForm devient le point d'entree du formulaire unique
  VenteEncaissementForm, sans onglet Facturation/Caisse ni seconde logique.
- L'ouverture ET la confirmation verifient en SQL la session, son mode,
  l'attribution individuelle, les droits du role actif et les parametres.
- Le paiement est maintenant une zone integree de ce formulaire. Le premier
  clic ne persiste rien. Revenir au panier annule cette etape sans paiement.
  La preparation est figee pendant le reglement ; la sortie exige son abandon
  ou sa finalisation. Une confirmation incertaine garde la meme intention.
- Le diagnostic tarif officiel indique les parametres/permissions manquants,
  distinctement du prix exceptionnel d'une ligne ; affichage et info-bulles.

Les noyaux financiers, l'index unique d'intention, les calculs et la transaction
globale existants ne sont pas reecrits. CreePar, ValideeParUtilisateurId et
Paiements.PayePar gardent leurs significations distinctes. Aucune ancienne
facture ni identite inconnue n'est reecrite. Les moteurs d'impression, les
conversions et le reset commercial ne sont pas modifies.

## Migration et diagnostic SQL

Aucune nouvelle migration n'est ajoutee. La migration additive/idempotente
`2026101501_VenteEncaissement.sql` reste requise. Elle n'accorde aucun mode.

Sur une copie de la base utilisee par l'application :

```sql
SELECT DB_NAME() AS BaseInspectee, @@SERVERNAME AS Serveur;
SELECT Version,Description FROM dbo.SchemaVersion WHERE Version=2026101501;
SELECT name FROM sys.columns
WHERE object_id=OBJECT_ID(N'dbo.UtilisateurRoles')
  AND name=N'VenteEncaissementAutorise';
```

Executer `TatianaModesDiagnostic.sql` en adaptant seulement @Compte au vrai
nom de connexion. Il retourne les IDs reels, chaque association, les cinq
droits requis, les parametres, l'attribution et les roles habilites a
l'administration de ces attributions. Aucun UPDATE ni grant dans ce script.
Un marqueur de version seul ne prouve pas la presence de la colonne.
Si la migration manque, l'appliquer sur la copie (ou demarrer l'application
configuree sur la copie, SchemaMigrationService l'applique automatiquement).
Ne pas attribuer les modes par une mise a jour SQL improvisee.

## Configuration de Tatiana dans l'application

1. Dans Roles et privileges, le responsable administrant les comptes doit
   avoir ADMINISTRATION et **Utilisateurs : attribuer Vente et Encaissement
   par role** (UTILISATEUR_MODES). Attribuer ce droit explicitement a un role
   administratif autorise ; il n'est pas accorde automatiquement.
2. Le role CAISSIERE reel, sans fusionner/renommer ses IDs, doit posseder
   FACTURIER, FACTURE_CREER, CAISSE, ENCAISSEMENT_CREER et MODE_COMBINE pour le
   parcours complet. Le nom grammatical du role n'accorde aucun droit.
3. Parametres : Gestion des modes, Caisse et mode combine actives. Garder le
   mode Facturation actif pour le parcours separe du facturier.
4. Administration Utilisateurs : selectionner Tatiana, enregistrer ses roles
   autorises si necessaire. Dans MODES PAR ROLE, choisir FACTURIER : case
   combinee decochee. Choisir CAISSIERE : cocher, saisir un motif, enregistrer.
5. Fermer/reouvrir l'administration, reselecter Tatiana et chacun des roles :
   verifier la persistance et l'absence de contamination entre associations.
6. Reconnecter Tatiana : FACTURIER n'offre pas la case combinee ; CAISSIERE
   l'offre. Decochee = caisse classique ; cochee = FacturationEtCaisseForm.

Les droits classiques sont affiches en lecture seule dans la section : ils
restent administres dans Roles et privileges. Le droit de demander un tarif
officiel n'accorde pas le droit d'appliquer directement un tarif ou une
exception facture. Les conditions de cout, marge et approbation restent
controlees par DelegationPrixService.

## Acceptation Windows/SQL Server obligatoire

Utiliser une copie isolee ; ne pas reinitialiser la production.

1. Tatiana/FACTURIER sans attribution : pas de case ; facture classique
   validee sans erreur de liaison, y compris une seule ligne puis panier vide.
2. Tatiana/CAISSIERE avec attribution : case presente ; decochee ouvre Caisse.
3. Case cochee : un seul formulaire FacturationEtCaisse, pas deux onglets de
   modules. Client, code-barres, produits, conditionnements et panier utilisables.
4. Mise en attente/reprise puis validation : zone de paiement dans la meme
   fenetre. Avant confirmation, aucune nouvelle facture/paiement finalise.
5. Revenir au panier : aucune ecriture financiere ; preparation conservee.
   Refuser la fermeture pendant un paiement non termine ; abandon explicite.
6. Vente de 56 000 FC, recu 60 000, rendu 4 000 : une facture, un paiement net
   de 56 000, mouvements et audit coherents. Tester USD et taux devenu obsolete.
7. Double clic/rejeu/concurrence/rupture SQL : pas de second paiement. Verifier
   le comportement de confirmation incertaine sans ressaisie d'une vente.
8. Retirer l'attribution via la section : plus de case a la reconnexion et
   acces service refuse pour une session deja combinee. Refaire avec le
   parametre global desactive puis avec un droit de role retire.
9. Sarah/Tatiana cree, valide et encaisse : identites distinctement tracees.
   Andy prepare puis Marie encaisse : Andy/Andy/Marie, sans retro-attribution.
10. Tarif officiel avant ajout au panier, panier vide : demande ou application
    selon droits ; refus explique ; approbation/refus et seuils. Exception de
    ligne sans changement du catalogue ni des factures anciennes.
11. Ticket 80 mm et A4 : moteurs historiques, document et statut exacts,
    montants, conditionnements et impression physique a verifier.
12. DPI/resolutions : defilement de la section Modes, libelles et zone de
    reglement sans chevauchement ; facturation/caisse classiques conservees.

## Bancs et limites

AuditMetierCoreTests : controles purs executables sous Linux.
AuditMetierSqlTests : nouveaux controles d'affichage administratif par role,
retrait en cours de session, desactivation globale et refus d'acces direct,
en plus des transactions/rejeux/auteurs existants. Execution uniquement avec
ERP_PHASE1_TEST_ENABLE=YES et une connexion vers une copie *_Phase1_TEST,
avec les preconditions decrites dans VenteEncaissementGuide.md.

PanierWinFormsTests : tests des vrais controles DialogueModeTravail pour
CAISSIERE (case decochee/cochee) et FACTURIER non attribue, plus les 40 cycles
de liaison du panier. Compilation net472 possible ; execution Windows requise.

Le build WinForms de production reste a faire avec Visual Studio, le Developer
Pack .NET Framework 4.7.2 et la vraie DLL Guna.UI2 licenciee. La verification
isolee qui exclut LoginForm sous /tmp n'est PAS un build valide de production.
Aucun test SQL, aucun rendu imprime et aucun parcours WinForms n'est declare
reussi sans execution Windows/SQL Server.

```powershell
msbuild prototype\WinFormsVB\DevCommerc8ak\DevCommerc8ak.vbproj /restore /p:Configuration=Release /p:GunaUI2AssemblyPath="C:\Libs\Guna.UI2.dll"
msbuild prototype\tests\PanierWinFormsTests\PanierWinFormsTests.vbproj /restore /p:Configuration=Release
& prototype\tests\PanierWinFormsTests\bin\Release\net472\PanierWinFormsTests.exe
```

Controles executes ici : 119 controles purs passes ; builds API Release
(un avertissement preexistant CS1998, zero erreur) et Dashboard Release reussis.
Bancs SQL et controles Windows compiles, non executes. Compilation isolee
des formulaires reussie en excluant LoginForm (Guna absent), hors livrable.
Build WinForms standard bloque par MSB3644. SQL Server et Windows NON VALIDES.
Revue de diff et git diff --check executes ; aucune donnee commerciale touchee.

Limites preexistantes conservees : reglement integral dans le parcours
combine ; paiements partiels dans Caisse. Paiements externes confirmes
manuellement avec reference, pas de passerelle automatique. Apres crash
pendant une confirmation incertaine, rapprochement de l'historique obligatoire
avant une nouvelle saisie (intention non persistee localement).

## Fichiers de ce correctif

Sous WinFormsVB/DevCommerc8ak : DevCommerc8ak.vbproj ;
Forms/FacturationEtCaisseForm.vb, Forms/VenteEncaissementForm.vb,
Forms/FacturationForm.vb, Forms/MainForm.vb ;
UI/FormulaireUtilisateurs.vb, UI/ModesUtilisateurPanel.vb (nouveau) ;
Services/AttributionModeService.vb, Services/ModeTravailService.vb,
Services/VenteEncaissementService.vb, Services/DelegationPrixService.vb.

Sous tests : AuditMetierCoreTests/Program.vb, AuditMetierSqlTests/Program.vb,
PanierWinFormsTests/Program.vb, PanierWinFormsTests/PanierWinFormsTests.vbproj,
VenteEncaissementGuide.md, CorrectionVenteEncaissementGuide.md (nouveau),
TatianaModesDiagnostic.sql (nouveau, non destructif).
