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

