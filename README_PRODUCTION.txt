ERP Commercial - Guide de production

1) Connexion SQL Server

Le client WinForms lit sa chaîne de connexion dans:
- `prototype/WinFormsVB/DevCommerc8ak/My Project/App.config`

Exemple:
Data Source=SERVEUR\SQLEXPRESS;
Initial Catalog=CommercialMagDB;
Integrated Security=False;
User ID=sa;
Password=********;
TrustServerCertificate=True;

Pour modifier:
- nom du serveur
- instance SQL
- base de données
- utilisateur
- mot de passe

2) API de synchronisation

L’API doit pointer vers la base cloud `CommercialMagCloudDB`.
Le dashboard web consomme uniquement l’API.

3) Installation WinForms

- installer le package généré par Inno Setup
- créer un raccourci Bureau si nécessaire
- créer un raccourci Menu Démarrer si nécessaire
- conserver le fichier de configuration généré par le setup

4) Règles de production

- ne pas supprimer les anciennes fonctionnalités
- ne pas renommer les tables ou colonnes
- valider les impressions avant la bascule
- valider les montants sans décimales inutiles

5) Secours

En cas de problème:
- restaurer la base SQL sauvegardée
- revenir au package précédent
- désactiver temporairement la synchro API

