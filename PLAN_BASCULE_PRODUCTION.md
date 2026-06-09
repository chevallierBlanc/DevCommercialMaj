# Plan de bascule en production

## 1. État actuel

Le dépôt contient trois blocs distincts:

- **ERP WinForms VB.NET** pour l'exploitation locale
- **API de synchronisation** pour le transit des données vers le cloud
- **Dashboard web patron** en lecture seule

Le fonctionnement actuel doit rester inchangé pendant la migration. Les tables SQL, colonnes et règles métier déjà en place ne doivent pas être renommées ou supprimées.

## 2. Prérequis

### Serveur / SQL

- SQL Server disponible sur le réseau local ou sur le serveur cible
- base locale: `CommercialMagDB`
- base cloud: `CommercialMagCloudDB`
- compte SQL ou authentification Windows selon le contexte
- sauvegarde complète avant toute bascule

### Postes clients

- Windows compatible avec l’exécutable WinForms
- accès au serveur SQL via réseau local
- impression ticket opérationnelle
- impression A4 / PDF validée

### API

- hébergement ASP.NET Core actif
- JWT fonctionnel
- accès à `CommercialMagCloudDB`

### Dashboard Web

- navigateur moderne
- accès réseau à l’API
- lecture seule

## 3. Sauvegarde obligatoire

Avant migration:

1. sauvegarder les deux bases SQL
2. exporter les configurations `App.config` et `appsettings.json`
3. archiver le dossier `installer/`
4. conserver une copie du dossier de déploiement WinForms

## 4. Ordre de mise en production

1. valider la base SQL de préproduction
2. déployer l’API
3. tester la synchronisation vers la base cloud
4. déployer le WinForms sur un poste pilote
5. valider les impressions et les flux métier
6. déployer le dashboard web patron
7. ouvrir l’accès au patron en lecture seule

## 5. Bascule WinForms

### Installation

- installer l’application sur le poste administrateur / serveur
- installer ensuite sur les postes facturier, caisse et gestionnaire

### Connexion SQL

- vérifier la chaîne dans `App.config`
- adapter `Server`, `InitialCatalog`, `User ID`, `Password`
- ne jamais modifier les tables ni les colonnes dans le code métier

### Tests WinForms

- connexion utilisateur
- vente
- entrée stock
- sortie stock
- inventaire
- caisse
- dépenses
- rapports
- impression ticket
- impression A4
- export PDF

## 6. Bascule API

### Déploiement

- publier l’API
- pointer vers `CommercialMagCloudDB`
- activer les logs applicatifs

### Tests API

- `auth/login`
- `stocksortie`
- `depenses`
- `dashboard/journalier`
- `dashboard/mensuel`
- `dashboard/annuel`

### Sécurité minimale

- JWT actif
- transport HTTPS en production
- validation des payloads
- journalisation des erreurs

## 7. Bascule Dashboard Web

### Accès

- ordinateur
- téléphone
- tablette
- TV / grand écran

### Fonctionnement

- lecture seule
- auto-refresh
- KPI principaux visibles
- analyse ventes et synthèses

### Principe de flux

ERP WinForms -> API -> Base cloud -> Dashboard Web

Le dashboard web ne doit jamais lire directement la base locale.

## 8. Retour arrière

Si un problème est détecté:

1. arrêter les nouveaux déploiements
2. restaurer la base SQL sauvegardée
3. revenir au package WinForms précédent
4. désactiver l’accès à la nouvelle API
5. remettre l’ancien dashboard en service si nécessaire

## 9. Validation finale

La mise en production n’est validée qu’après:

- validation des impressions
- validation des écritures SQL
- validation des montants affichés sans décimales inutiles
- validation des synchronisations
- validation du dashboard web sur mobile et TV

