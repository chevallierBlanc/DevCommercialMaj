# Guide de migration API - CommercialMagDb.Api

Ce document explique comment deplacer proprement le dossier `ApiCommercialMagDB` d'un environnement local vers un VPS, sans casser le client WinForms ni la structure SQL existante.

## 1. Objectif

- Garder la meme logique metier.
- Garder les tables existantes.
- Deployer l'API localement d'abord, puis sur un VPS.
- Ne pas ajouter de nouvelles tables metier pour les ventes, factures ou paiements.
- Utiliser uniquement les tables deja presentes dans la base.

## 2. Contenu du dossier API

Le dossier `prototype/ApiCommercialMagDB` contient :

- `CommercialMagDb.Api.csproj` : projet Web API
- `Program.cs` : point d'entree et declaration des routes
- `Infrastructure/` : acces SQL, auth, JWT, dashboard, synchro
- `Contracts/` : contrats d'entree et de sortie
- `appsettings.json` et `appsettings.Development.json` : configuration locale

## 3. Pre-requis de compilation

Pour compiler ce projet, il faut :

- Visual Studio avec support ASP.NET Core
- .NET SDK compatible avec la cible du projet
- SQL Server accessible depuis la machine qui heberge l'API
- Restaurer les packages NuGet du projet

Si le projet ne charge pas :

- verifier que le fichier `CommercialMagDb.Api.csproj` est bien en format SDK style
- verifier que la solution ouvre bien `CommercialMagDb.Api.sln`
- supprimer le dossier `.vs` si Visual Studio garde un cache invalide

## 4. Configuration locale

En local, l'API doit pointer vers :

- un serveur SQL local
- `localhost` pour le test HTTP

Les valeurs sensibles doivent rester hors du code :

- `ConnectionStrings`
- `Jwt:SigningKey`
- `Jwt:Issuer`
- `Jwt:Audience`

## 5. Deploiement local avant VPS

Ordre conseille :

1. restaurer les packages NuGet
2. compiler le projet API
3. lancer l'API en local
4. tester les routes :
   - `POST /api/auth/login`
   - `POST /api/auth/refresh`
   - `POST /api/stocksortie`
   - `POST /api/depenses`
   - `GET /api/dashboard/journalier`
   - `GET /api/dashboard/mensuel`
   - `GET /api/dashboard/annuel`
5. verifier que le client WinForms pointe vers `http://localhost:<port>/`

## 6. Deploiement VPS

Quand le test local est valide :

- publier l'API
- copier les fichiers publies sur le VPS
- configurer l'hote Web ou le service Windows
- exposer l'API en HTTPS
- ouvrir uniquement les ports necessaires

Recommandations :

- garder la meme structure de route
- ne pas renommer les contrats JSON sans raison
- ne pas changer la logique des methodes sans mettre a jour le client WinForms

## 7. Base de donnees

Le projet API doit utiliser strictement les tables deja existantes :

- `StockSortie`
- `MouvementsStock`
- `Produits`
- `StockEntree`
- `Clients`
- `MotifSortie`
- `Depenses`
- `CategoriesDepenses`
- `vStockProduit`
- `LignesFactureVente`
- `FacturesVente`
- `Paiements`

Interdictions :

- pas de `CREATE TABLE` dans le code applicatif
- pas de `ALTER TABLE` dans le code applicatif
- pas de `DROP TABLE` dans le code applicatif

Le schema structurel doit rester dans les scripts SQL de deploiement.

## 8. Fichiers SQL utiles

- `prototype/scriptBDDMAJ.sql`
- `prototype/scriptOfflineSync.sql`

Ces scripts doivent etre executes en base avant la mise en service.

## 9. Variables de configuration a verifier avant migration

Dans l'API :

- chaine de connexion SQL
- URL publique
- cle JWT
- expire du token
- duree du refresh token

Dans WinForms :

- URL de l'API
- activation de la synchro
- mode offline

## 10. Checklist de validation

Avant de passer en production :

- l'API demarre sans erreur
- le login fonctionne
- la synchronisation fonctionne en local
- le dashboard retourne des donnees
- les sorties stock et depenses sont enregistrees
- la base de donnees repond correctement
- le client WinForms se connecte a l'API locale
- le client WinForms conserve le mode offline si l'API tombe

## 11. Migration vers un vrai serveur distant

Pour migrer vers un vrai serveur distant :

1. deployer la base SQL sur le serveur cible
2. migrer les donnees de test
3. publier l'API sur le VPS
4. modifier seulement les valeurs de configuration
5. tester a nouveau le login et la synchro

Le but est de ne pas refaire le projet, mais de changer uniquement l'environnement d'execution.
