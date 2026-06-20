# Guide de mise en production WinForms

Ce guide décrit une mise en production simple, robuste et multi-utilisateur pour `ERPCommercial / Commercial Pro`.

## 1. Préparer le PC serveur

### 1.1 Installer SQL Server
- Installer une instance SQL Server adaptée au volume attendu.
- Prévoir un compte administrateur SQL dédié à la maintenance.
- Vérifier que le service SQL Server démarre automatiquement.

### 1.2 Restaurer la base
- Restaurer la base de production à partir du backup validé.
- Vérifier que les objets attendus existent :
  - tables métiers
  - vues
  - procédures stockées
  - séquences éventuelles

### 1.3 Activer TCP/IP
- Ouvrir `SQL Server Configuration Manager`.
- Activer `TCP/IP` pour l’instance utilisée.
- Redémarrer le service SQL Server après modification.

### 1.4 Ouvrir le port SQL
- Ouvrir le port SQL utilisé, en général `1433`.
- Si une instance nommée utilise un port dynamique, fixer un port stable en production.
- Autoriser ce port dans le pare-feu Windows du serveur.

### 1.5 Créer un utilisateur SQL
- Créer un compte SQL dédié à l’application.
- Donner uniquement les droits nécessaires.
- Éviter d’utiliser `sa` en production.

### 1.6 Tester la connexion réseau
- Depuis le serveur, tester la connexion vers l’instance locale.
- Depuis un poste client, tester la connexion vers l’IP du serveur.
- Vérifier que la base répond rapidement et sans erreur d’authentification.

## 2. Préparer les postes clients

### 2.1 Installer l’application
- Copier l’application WinForms sur chaque poste.
- Vérifier que les dépendances sont présentes.
- Lancer l’application une première fois avec les droits nécessaires à la création du dossier de configuration local.

### 2.2 Configurer l’adresse IP du serveur
- Au premier lancement, ouvrir le formulaire de configuration SQL.
- Renseigner :
  - serveur SQL ou IP
  - port
  - nom de base
  - mode d’authentification
  - identifiants SQL si nécessaires

### 2.3 Tester la connexion SQL
- Utiliser le bouton `Tester la connexion`.
- Ne pas enregistrer tant que le test n’est pas réussi.
- Vérifier que la connexion reste stable depuis le poste client.

### 2.4 Lancer l’application
- Une fois la configuration valide, ouvrir l’application normalement.
- Vérifier que l’écran de connexion s’affiche.

## 3. Organisation multi-postes

### 3.1 Poste serveur / admin
- Sert d’hôte SQL et éventuellement de poste de supervision.
- Gère les sauvegardes et les opérations de maintenance.

### 3.2 Poste facturier
- Utilisé pour les factures et les validations de paiement.
- Doit avoir une connexion réseau stable.

### 3.3 Poste caisse
- Utilisé pour les encaissements et les contrôles rapides.
- Doit être rapide et fiable.

### 3.4 Sauvegarde
- Sauvegarder la base au moins une fois par jour.
- Conserver plusieurs points de restauration.
- Tester régulièrement une restauration complète.

### 3.5 Réseau local
- Préférer un réseau filaire pour le poste serveur.
- Réserver des IP fixes au serveur et aux postes critiques.

## 4. Bonnes pratiques production

- Mettre en place une sauvegarde automatique.
- Utiliser un onduleur `UPS` pour le serveur.
- Attribuer une IP fixe au serveur SQL.
- Éviter un Wi-Fi instable pour les postes transactionnels.
- Prévoir une maintenance hebdomadaire :
  - contrôle des backups
  - contrôle des journaux
  - vérification de l’espace disque
  - vérification des performances SQL
- Ne pas partager le compte administrateur entre plusieurs utilisateurs.
- Limiter chaque poste à son rôle métier.

## 5. Checklist finale avant livraison

- [ ] SQL Server installé et démarré
- [ ] Base restaurée
- [ ] TCP/IP activé
- [ ] Port SQL ouvert
- [ ] Compte SQL dédié créé
- [ ] Connexion testée depuis un poste client
- [ ] Application installée sur chaque poste
- [ ] Configuration SQL enregistrée localement
- [ ] Login opérationnel
- [ ] Encaissement testé
- [ ] Validation facture testée
- [ ] Sauvegarde testée
- [ ] Journaux locaux vérifiés
- [ ] IP fixe serveur confirmée

## 6. Remarques d’exploitation

- La configuration SQL initiale est gérée par `FormConfigurationSQL`.
- Les logs techniques sont écrits dans le dossier local de l’utilisateur.
- En cas d’indisponibilité SQL, l’application affiche une erreur claire au lieu de crasher silencieusement.

