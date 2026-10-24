# EXEMPLE SYNTHÉTIQUE — pas des mesures de production

Campagne : EXEMPLE. Date fictive : 23 octobre 2026. Toutes les heures sont UTC.
Adresses de documentation uniquement, aucun compte ni secret.

| Poste | Étape | Intervalle observé | Durée observée | Échantillons en échec |
| --- | --- | --- | --- | --- |
| PC_FACTURIER | TCP 192.0.2.10 | 12:01:00 – 12:02:00 | 60 s | 1/3 (33,33 %) |
| PC_BUREAU | TCP 192.0.2.10 | 12:01:10 – 12:02:10 | 60 s | 1/3 (33,33 %) |
| PC_CAISSE | Service MSSQLSERVER | Aucun échec observé | Sans incident | 0/3 |

**Constat de cet exemple :** les deux intervalles clients se chevauchent pendant
50 secondes. Le service serveur reste Running aux trois instants mesurés.

**Ce que cela ne prouve pas :** aucune authentification ou requête SQL n'a été
mesurée dans cet exemple ; aucune donnée de passerelle/carte n'est fournie.
Le service Running ne garantit ni le port accessible ni l'absence de blocage SQL.
La cause physique et la durée exacte ne sont pas connues.

**Suite proposée :** rapprocher les mesures routeur, carte Ethernet serveur,
TCP local/distant et événements aux mêmes heures, puis ERRORLOG. Ne pas conclure
« routeur défectueux » ou « SQL en panne » à partir de ces seules lignes.

Reproduire la synthèse depuis le dossier de l'outil :

```powershell
.\SyntheseDiagnosticERP.ps1 -DossierJournaux '.\exemples' -Campagne EXEMPLE
```

Les fichiers générés sont sous `exemples\synthese`, pas des preuves de production.
Ne jamais mélanger ce jeu synthétique avec une campagne réelle.
