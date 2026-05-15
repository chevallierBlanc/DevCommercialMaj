# Maquettes UI/UX (Wireframes texte)

## Principes generaux
- Theme pro: bleu fonce, gris, blanc
- Typo: Segoe UI (standard Windows)
- Icons: pack coherent (Fluent ou FontAwesome)
- Tables: filtres en temps reel, recherche instantanee
- Navigation: barre laterale + zone de contenu

## Ecran Login
```
+--------------------------------------------------------------+
| LOGO                                Application Commerciale  |
|--------------------------------------------------------------|
|  Utilisateur: [_______________]                              |
|  Mot de passe: [_______________]  (Afficher)                  |
|                                                              |
|  [ Se connecter ]                                             |
|                                                              |
|  Etat serveur: CONNECTE                                       |
+--------------------------------------------------------------+
```

## Interface Facturier
```
+----------------------------------------------------------------------------------+
| Sidebar | Facturation                                                            |
|----------+------------------------------------------------------------------------|
|  Menu    | Recherche produit [___________________________________________] [Scan]|
|          |                                                                        |
|          | Resultats:                                                             |
|          |  - Code | Libelle | Prix | Stock | +                                   |
|          |                                                                        |
|          | Panier:                                                                |
|          |  - Code | Libelle | Qte | Prix | Remise | Total                         |
|          |                                                                        |
|          |---------------------------------------------------------------|        |
|          |  Sous-total: 0000   Remise: [__%]  TVA: [__%]  TOTAL: 0000     |        |
|          |  Client: [_________]  Mode: [Cash|Autre]                      |        |
|          |  [ Valider Facture ]  [ Mettre en attente ]  [ Imprimer A4 ]   |        |
|          |  Mode scan: [Scanner USB] [Scanner Smartphone]  Etat: CONNECTE |        |
+----------------------------------------------------------------------------------+
```

## Interface Caisse
```
+----------------------------------------------------------------------------------+
| Sidebar | Caisse                                                                  |
|----------+------------------------------------------------------------------------|
|  Menu    | Factures en attente (table)                                            |
|          |  - Numero | Date | Client | Montant | Etat | [Ouvrir]                  |
|          |                                                                        |
|          | Facture selectionnee:                                                  |
|          |  Montant: 0000  Remise: 0  TVA: 0  TOTAL: 0000                         |
|          |  Mode paiement: [Cash|Autre]  Reference: [_________]                   |
|          |  [ Valider Paiement ]  [ Imprimer Ticket ]                             |
+----------------------------------------------------------------------------------+
```

## Interface Administrateur (Dashboard)
```
+----------------------------------------------------------------------------------+
| Sidebar | Tableau de bord                                                        |
|----------+------------------------------------------------------------------------|
|  Menu    | KPI: [CA Jour] [CA Mois] [Marge] [Stock Critique] [Valeur Stock]       |
|          |                                                                        |
|          | Graphiques:                                                           |
|          |  - Courbe CA journalier                                                |
|          |  - Barres produits les plus vendus                                     |
|          |                                                                        |
|          | Raccourcis modules: [Produits] [Stock] [Fournisseurs] [Clients]        |
+----------------------------------------------------------------------------------+
```

## Ecran Produits (Admin)
```
+----------------------------------------------------------------------------------+
| Toolbar: [Nouveau] [Modifier] [Importer] [Exporter] [Historique Prix]            |
| Recherche: [____________________]  Categorie: [____]  Stock < [__]               |
|----------------------------------------------------------------------------------|
| Table Produits: Code | Libelle | Prix detail | Prix gros | Stock | Expiration     |
+----------------------------------------------------------------------------------+
```

## Ecran Stock
```
+----------------------------------------------------------------------------------+
| Tabs: [Entree] [Sortie] [Perte/Casse] [Inventaire] [Alertes]                     |
|----------------------------------------------------------------------------------|
| Entree: Fournisseur [____]  Ref facture [____]  Date [____]                      |
| Lignes: Produit | Qte | Prix achat | Total                                       |
| [ Valider Entree ]                                                               |
+----------------------------------------------------------------------------------+
```

## Ecran Rapports
```
+----------------------------------------------------------------------------------+
| Filtre: Date debut [__]  Date fin [__]  Type [Journalier|Mensuel|Produits]        |
|----------------------------------------------------------------------------------|
| Table/Graphique rapport                                                          |
| [ Exporter PDF ] [ Exporter Excel ]                                               |
+----------------------------------------------------------------------------------+
```
