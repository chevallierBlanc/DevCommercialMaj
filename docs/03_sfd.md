# Specifications Fonctionnelles Detaillees (SFD)

## 1. Roles et permissions
1. Facturier
- Creer facture
- Mettre facture en attente
- Appliquer remise dans la limite
- Impression A4

2. Caissiere
- Consulter factures en attente
- Valider paiement
- Impression ticket

3. Administrateur
- Acces complet
- Parametrage
- Rapports
- Gestion utilisateurs

## 2. Workflow Facturation
1. Recherche produit par libelle, code-barres, categorie
2. Ajout au panier avec quantite
3. Calcul automatique total, remise, TVA
4. Enregistrement facture avec statut "EN_ATTENTE"
5. Impression A4 optionnelle

Regles
- Stock verifie a l'ajout
- Remise max definie en parametres
- Facture validee = non supprimable

## 2.1 Lecture code-barres
Modes supportes
- Scanner USB (HID): saisie automatique dans le champ recherche
- Scanner smartphone: application mobile qui envoie le code via LAN

Parametres
- Activation mode smartphone
- Adresse IP/port service d'ecoute
- Format accepte (EAN/QR/Autre)
- Timeout et journalisation des lectures

## 3. Workflow Paiement (Caisse)
1. Affichage des factures en attente
2. Selection facture
3. Choix mode paiement
4. Validation paiement
5. Impression ticket thermique

Regles
- Apres validation, facture passe a "PAYEE"
- Stock decremente en transaction
- Paiement modifiable interdit

## 4. Produits
Champs obligatoires
- Code-barres unique
- Libelle
- Prix detail, prix gros
- Stock initial
- Date expiration
- Categorie

Regles
- Historique prix enregistre a chaque modification
- Date expiration facultative mais recommande

## 5. Stock
Operations
1. Entree marchandise (achat fournisseur)
2. Sortie automatique apres paiement
3. Perte/Casse
4. Inventaire periodique
5. Alertes seuil minimum
6. Alertes expiration

Regles
- Toute sortie est journalisee
- Ajustements d'inventaire signés admin

## 6. Fournisseurs
- Fiche fournisseur
- Historique achats
- Situation dettes
- Echeances

## 7. Clients
- Fiche client
- Historique achats
- Credit client
- Limite credit configurable

## 8. Rapports
1. Journalier
2. Mensuel
3. Produits les plus vendus
4. Valeur totale stock
5. Marges par produit
6. Rotation stock

## 9. Parametres
- Utilisateurs, roles
- Remise max
- Seuil stock critique
- Alerte expiration (jours)
- Configuration lecture code-barres (USB/smartphone)
- Imprimantes
- Sauvegarde base

## 10. Journalisation et anti-fraude
- AuditLog: toutes actions critiques
- FraudLog: tentative modification apres validation
- Logs non modifiables (trigger + permissions)

## 11. Cas d'erreurs et regles metier fines
### Authentification
- Erreur: utilisateur inconnu -> message "Utilisateur invalide"
- Erreur: mot de passe incorrect -> message "Mot de passe invalide"
- Erreur: compte inactif -> message "Compte desactive"

### Facturation
- Erreur: code-barres inconnu -> message "Produit introuvable"
- Erreur: quantite <= 0 -> blocage validation ligne
- Erreur: remise > remise max -> blocage + audit
- Regle: une facture EN_ATTENTE ne peut etre payee que si elle contient au moins 1 ligne
- Regle: recalcul automatique des totaux a chaque modification

### Paiement
- Erreur: facture deja PAYEE -> blocage validation paiement
- Erreur: montant paiement < total -> blocage (si pas de paiement partiel)
- Erreur: stock insuffisant au moment du paiement -> blocage + message
- Regle: apres paiement, facture verrouillee (lecture seule)

### Stock
- Erreur: sortie manuelle sans stock suffisant -> blocage
- Erreur: inventaire non valide (valeurs negatives) -> blocage
- Regle: toute correction inventaire exige role Admin

### Produits
- Erreur: code-barres duplique -> blocage
- Erreur: prix negatif -> blocage
- Regle: changement de prix cree une entree historique

### Fournisseurs
- Erreur: facture fournisseur dupliquee (reference) -> avertissement
- Regle: suppression interdite si des achats existent

### Clients
- Erreur: depassement limite credit -> blocage ou demande autorisation admin
- Regle: client inactif non selectionnable

### Lecture code-barres
- Erreur: scanner smartphone non connecte -> afficher etat "DECONNECTE"
- Erreur: timeout lecture -> reessayer
- Regle: les lectures sont journalisees (date, utilisateur, mode)

### Rapports
- Erreur: plage dates invalide -> blocage
- Regle: export PDF/Excel journalise

