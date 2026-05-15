# Architecture Technique

## 1. Vue d'ensemble
- Application Desktop WinForms VB.NET
- SQL Server 2014 sur serveur LAN
- Connexion TCP/IP locale
- Authentification et autorisation par roles

## 2. Couches logicielles
1. Presentation (WinForms)
- UI moderne (custom controls)
- DataGridView avec filtres instantanes
- Themes centralises

2. Application (Services)
- Logique metier: Facturation, Stock, Paiement, Rapports
- Validation des regles
- Orchestration des transactions

3. Acces aux donnees (DAL)
- ADO.NET (SqlConnection, SqlCommand)
- Requetes parametrées
- Transactions SQL

4. Base de donnees (SQL Server)
- Procedures stockees pour operations critiques
- Triggers pour journalisation non editable
- Index sur colonnes de recherche

## 2.1 Lecture code-barres (peripheriques)
- Scanner USB: mode clavier (HID) = lecture directe dans le champ recherche
- Scanner smartphone: mode \"app\" -> envoi du code via TCP/IP LAN (service local)
- Parametres: adresse IP/port, format du code, timeout, activation/desactivation

## 3. Securite
- Hash mot de passe (PBKDF2)
- Rôles: Facturier, Caissiere, Admin
- Journal inviolable en base
- Blocage modifications apres validation paiement

## 4. Deploiement
- Application installee sur postes
- Serveur SQL securise
- Sauvegardes planifiees

## 5. Schema de donnees (logique)
Entites principales:
- Users, Roles, UserRoles
- Products, ProductCategories, ProductPriceHistory
- StockMovements, StockAdjustments, StockAlerts
- Suppliers, SupplierInvoices, SupplierDebts
- Customers, CustomerCredits
- SalesInvoices, SalesInvoiceLines, Payments
- AuditLog, FraudLog

## 6. Conventions
- PK: int identity
- FK: nommage fk_<table>_<ref>
- Champs date: datetime2
- Champs montant: decimal(18,2)
- Soft delete interdit pour factures validees

## 7. Points de performance
- Index sur Products(CodeBarres), Products(Libelle)
- Index sur SalesInvoices(Date, Statut)
- Index sur StockMovements(ProductId, Date)
