# Checklist tests production

## WinForms

- [ ] ouverture de session utilisateur
- [ ] ouverture du tableau de bord
- [ ] ajout produit
- [ ] entrée stock
- [ ] vente facturier
- [ ] validation caisse
- [ ] sortie stock automatique
- [ ] sortie manuelle
- [ ] dépense journalière
- [ ] inventaire
- [ ] impression ticket
- [ ] impression A4
- [ ] export PDF
- [ ] export Excel si présent
- [ ] affichage des montants sans décimales inutiles
- [ ] statistiques dashboard
- [ ] rapports journaliers
- [ ] rapports mensuels
- [ ] connexion depuis un poste client

## API

- [ ] login JWT
- [ ] refresh token
- [ ] `POST /api/stocksortie`
- [ ] `POST /api/depenses`
- [ ] `GET /api/dashboard/journalier`
- [ ] `GET /api/dashboard/mensuel`
- [ ] `GET /api/dashboard/annuel`
- [ ] journalisation des erreurs
- [ ] refus des routes sécurisées sans token

## Dashboard web

- [ ] accès téléphone
- [ ] accès tablette
- [ ] accès ordinateur
- [ ] accès TV / grand écran
- [ ] mode lecture seule
- [ ] auto-refresh
- [ ] KPI essentiels visibles
- [ ] analyse ventes chargée
- [ ] données synchronisées via API

## Tests de non-régression

- [ ] suppression d’un bon d’approvisionnement
- [ ] retrait d’une ligne d’un bon
- [ ] inventaire en cours / reprise
- [ ] impression d’un rapport inventaire
- [ ] impression vente / stock
- [ ] cohérence des montants en FC et USD
- [ ] cohérence des quantités entières

## Points sensibles à tester manuellement

- [ ] `App.config` modifié sans recompiler
- [ ] impression A4 avec grande table
- [ ] gros volumes de ventes et dépenses
- [ ] base SQL réseau lente
- [ ] API indisponible puis retour
- [ ] export PDF sur chemin choisi par l’utilisateur

## Validation Windows / SQL Server avant push final

### Compilation

- [ ] compiler la solution complète dans Visual Studio
- [ ] compiler le projet WinForms VB.NET `DevCommerc8ak`
- [ ] compiler l'API ASP.NET `ApiCommercialMagDB`
- [ ] compiler le dashboard web patron si utilisé
- [ ] vérifier `Option Strict On` / `Option Explicit On` sans nouvelle erreur

### Base SQL Server et migrations

- [ ] démarrer sur une base existante avec données réelles de test
- [ ] démarrer sur une base vide
- [ ] vérifier création/lecture de `SchemaVersion`
- [ ] exécuter toutes les migrations une première fois
- [ ] réexécuter les migrations sans erreur ni doublon d'index
- [ ] vérifier que la migration des index de production est non destructive
- [ ] vérifier que les requêtes avec plages de dates retournent les mêmes lignes que les anciens filtres par jour

### Stock UNITE / MESURE

- [ ] produit UNITE : 1 carton = 60 pièces, stock 120 pièces, vente 2 cartons, reste 0 carton + 0 pièce
- [ ] produit UNITE : stock 30 pièces, vérifier que la valeur reste 30 pièces et n'est jamais interprétée comme 30 KG
- [ ] produit UNITE : vente 0,5 carton si l'interface l'autorise, vérifier `QuantiteBase = 30`
- [ ] produit MESURE : 1 sac = 25 KG, stock 120 KG, vente 12,5 KG, reste 107,5 KG
- [ ] inventaire MESURE : stock théorique 395 KG, comptage 7 sacs de 50 KG + 9 sachets de 5 KG, écart 0
- [ ] comparer `QuantiteSaisie`, `QuantiteBase`, stock théorique, stock physique et écart dans les grilles et en base

### Vente / Caisse / Analyse

- [ ] vente Gros
- [ ] vente Demi
- [ ] vente Quart
- [ ] vente Pièce
- [ ] vente Douzaine
- [ ] vente Dizaine
- [ ] vente type personnalisé UNITE
- [ ] vente type personnalisé MESURE
- [ ] vérifier que la même `QuantiteBase` sert au contrôle stock, à la ligne facture, à `StockSortie`, au mouvement stock, au CMV et aux rapports
- [ ] vérifier CA, CMV, bénéfice et marge après encaissement
- [ ] vérifier qu'une ancienne vente avec `CoutUnitaireBaseVente` n'est pas recalculée avec le prix d'achat actuel
- [ ] vérifier le cas sans coût historique : analyse partielle signalée

### SUPERADMIN

- [ ] créer une base sans utilisateur et vérifier l'assistant de bootstrap SUPERADMIN
- [ ] refuser un mot de passe faible au bootstrap
- [ ] tester qu'un ADMIN ne peut pas supprimer le SUPERADMIN
- [ ] tester qu'un ADMIN ne peut pas désactiver le SUPERADMIN
- [ ] tester qu'un ADMIN ne peut pas retirer ou changer le rôle SUPERADMIN
- [ ] tester qu'un ADMIN ne peut pas changer le mot de passe du SUPERADMIN
- [ ] tester les mêmes actions via tout écran de gestion utilisateur disponible
- [ ] tester les mêmes actions via API si un endpoint utilisateur est exposé
- [ ] vérifier la journalisation des tentatives sensibles

### API

- [ ] `POST /api/auth/login` fonctionne sans token
- [ ] `POST /api/auth/refresh` fonctionne avec refresh token valide
- [ ] `/api/stocksortie` refuse une requête sans JWT
- [ ] `/api/stocksortie` exige la politique `StockSync`
- [ ] `/api/depenses` refuse une requête sans JWT
- [ ] `/api/depenses` exige la politique `FinanceSync`
- [ ] `/api/dashboard/*` refuse une requête sans JWT
- [ ] `/api/dashboard/*` exige la politique `DashboardRead`
- [ ] vérifier CORS avec l'origine réelle du réseau local
- [ ] vérifier que `Jwt:SigningKey` est fourni hors dépôt et assez long

### Sauvegarde / restauration

- [ ] sauvegarde manuelle
- [ ] sauvegarde automatique
- [ ] sauvegarde avant fermeture
- [ ] deux sauvegardes rapides consécutives créent deux fichiers distincts
- [ ] dossier de sauvegarde inaccessible : message clair et log
- [ ] SQL Server indisponible : message clair et log
- [ ] restauration sur base de test uniquement
- [ ] restauration : sélection explicite `.bak`
- [ ] restauration : refus fichier inexistant ou extension non `.bak`
- [ ] restauration : confirmation explicite
- [ ] restauration : autres postes fermés avant exécution
- [ ] restauration : succès puis redémarrage/reconnexion propre
- [ ] restauration : échec simulé puis base remise en `MULTI_USER`

### Multiposte

- [ ] deux postes vendent simultanément le dernier stock disponible
- [ ] double clic validation vente : une seule validation effective
- [ ] caisse encaisse la même facture depuis deux postes : pas de double encaissement
- [ ] inventaire et vente simultanés sur même produit : pas de stock négatif incohérent
- [ ] API indisponible puis retour : synchronisation offline sans doublon
