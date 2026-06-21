# Guide d'installation Inno Setup - ERPCommercial

## 1. Objectif

Ce guide explique comment préparer un installateur Inno Setup pour l'application WinForms `ERPCommercial`, sans embarquer SQL Server et sans écraser la configuration SQL déjà présente lors d'une mise à jour.

## 2. Prérequis

- .NET Framework requis par le projet WinForms: `4.7.2`
- Inno Setup installé sur le poste de génération
- SQL Server installé et configuré séparément
- Base `CommercialMagDB` restaurée ou créée avant utilisation
- Port SQL Server ouvert sur le pare-feu si plusieurs postes accèdent à la base

## 3. Compiler le projet WinForms en Release

1. Ouvrir la solution WinForms `prototype/WinFormsVB/DevCommerc8ak/DevCommerc8ak.sln`.
2. Sélectionner la configuration `Release`.
3. Compiler la solution.
4. Récupérer les fichiers générés dans:

```text
prototype/WinFormsVB/DevCommerc8ak/bin/Release/
```

Les fichiers essentiels sont:

- `DevCommerc8ak.exe`
- `DevCommerc8ak.exe.config`
- les DLL de dépendances
- les ressources nécessaires

## 4. Fichier Inno Setup

Le script principal est:

```text
installer/ERPCommercial_Setup.iss
```

Il est configuré avec:

- `AppName=ERPCommercial`
- `AppVersion=1.0.0`
- `AppPublisher=NTANTA ANDY`
- `DefaultDirName={pf}\ERPCommercial`
- `DefaultGroupName=ERPCommercial`
- `OutputBaseFilename=ERPCommercial_Setup`
- `Compression=lzma`
- `SolidCompression=yes`

Le setup généré doit produire:

```text
ERPCommercial_Setup.exe
```

## 5. Ce que l'installateur copie

L'installateur récupère le contenu du dossier `bin\Release` et installe:

- l'exécutable WinForms
- les DLL nécessaires
- les fichiers de configuration nécessaires
- les ressources nécessaires

Les dossiers applicatifs créés à l'installation sont:

- `{app}\Logs`
- `{app}\Backups`
- `{app}\Config`
- `{app}\Reports`

## 6. Protection de la configuration SQL

Le script Inno Setup est conçu pour ne pas écraser une configuration SQL déjà existante lors d'une réinstallation.

Points clés:

- le fichier `ERPCommercial.exe.config` n'est copié que s'il n'existe pas déjà
- les dossiers `Logs`, `Backups`, `Config` et `Reports` sont conservés
- les sauvegardes `.bak` ne sont pas supprimées automatiquement

## 7. Raccourcis créés

L'installation crée:

- un raccourci Bureau `ERPCommercial`
- un raccourci Menu Démarrer `ERPCommercial`

## 8. Générer le setup

1. Ouvrir `installer/ERPCommercial_Setup.iss` dans Inno Setup.
2. Vérifier que les chemins pointent bien vers le dossier `bin\Release`.
3. Compiler le script.
4. Récupérer l'exécutable généré:

```text
ERPCommercial_Setup.exe
```

## 9. Installation sur le PC serveur

1. Installer SQL Server séparément.
2. Restaurer ou créer la base `CommercialMagDB`.
3. Ouvrir le port SQL Server dans le pare-feu si nécessaire.
4. Lancer `ERPCommercial_Setup.exe`.
5. Installer l'application.
6. Au premier lancement, si aucune configuration SQL valide n'existe, l'application ouvre `FormConfigurationSQL`.
7. Tester la connexion SQL puis enregistrer la configuration.

## 10. Installation sur les postes clients

1. Installer l'application depuis `ERPCommercial_Setup.exe`.
2. Saisir l'adresse IP ou le nom du serveur SQL.
3. Vérifier le port SQL si la base est distante.
4. Tester la connexion dans `FormConfigurationSQL`.
5. Lancer ensuite `LoginForm`.

## 11. Mise à jour sans perdre la configuration SQL

Pour une mise à jour:

1. Installer la nouvelle version par-dessus l'ancienne.
2. Vérifier que la configuration SQL existante n'est pas supprimée.
3. Conserver les dossiers `Logs`, `Backups`, `Config` et `Reports`.
4. Vérifier que les fichiers `.bak` existants restent présents.

## 12. Désinstallation

Lors de la désinstallation:

- les fichiers programme sont supprimés
- les dossiers de données locaux sont conservés par défaut
- les sauvegardes `.bak` ne doivent pas être supprimées automatiquement

## 13. Contrôles à faire avant livraison

- vérifier que le `Release` a bien été recompilé
- vérifier que `ERPCommercial_Setup.iss` pointe vers `bin\Release`
- vérifier que les raccourcis Bureau et Menu Démarrer existent
- vérifier que la configuration SQL n'est pas écrasée
- vérifier que `Logs`, `Backups`, `Config` et `Reports` sont créés
- vérifier qu'une nouvelle installation ouvre `FormConfigurationSQL` si nécessaire

