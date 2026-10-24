# Diagnostic réseau ERPCommercial — serveur et clients

## Objet et état des preuves

Ce dossier est autonome : le copier **en entier** sur les postes Windows.
Il ne dépend pas du répertoire `prototype` et ne modifie pas ERPCommercial.

Contexte retenu : PC caissière hébergeant SQL Server et ERP, postes clients
RJ45 via un routeur local, IP serveur fixe et veille serveur déjà désactivée.
Ces deux derniers points ne sont donc pas proposés comme « correctifs ».
**La cause des interruptions de production n'est pas établie.**

L'audit du dépôt `d3fb6a7` confirme : WinForms cible .NET Framework 4.7.2,
DAL avec connexions courtes libérées par `Using`, commandes exécutées une fois,
transactions commerciales conservées, configuration locale par compte Windows
dans `%LOCALAPPDATA%\CommercialPro\CommercialPro.exe.config`.
La sonde SQL existante ne mesure pas la passerelle, la carte Ethernet ou les
ressources serveur. Le helper `SqlNetworkProbe.cs` est repris à l'identique de
la livraison précédente pour rendre ce dossier transportable ; aucun fournisseur
SQL ni mécanisme applicatif de connexion supplémentaire n'est installé.

## Contenu à copier

| Fichier | Fonction |
| --- | --- |
| DiagnosticReseauERP.ps1 | Collecte SERVEUR ou CLIENT |
| DiagnosticCommun.ps1 | Fonctions communes, CSV/JSONL, incidents |
| SqlNetworkProbe.cs | Sondes DNS/TCP/ICMP et SQL en lecture seule |
| SyntheseDiagnosticERP.ps1 | Comparaison des journaux multi-postes |
| Tests-Diagnostic.ps1 | Syntaxe, incidents, sockets locales et synthèse |
| exemples/Campagne_EXEMPLE.jsonl | Données entièrement synthétiques |
| exemples/RAPPORT_EXEMPLE.md | Exemple d'interprétation, pas un résultat réel |

## Prérequis et sécurité

- Cible : Windows PowerShell **5.1**, .NET Framework **4.7.2**, modules Windows
  NetAdapter, NetTCPIP, CimCmdlets et Microsoft.PowerShell.Diagnostics.
  Windows 10/11 et Windows Server équipés de ces composants sont les cibles
  techniques ; la version réellement installée doit être relevée. Aucun support
  de Windows 7/PowerShell 2 ni exécution réelle sous Windows ne sont revendiqués.
- Utiliser `powershell.exe`, sans installer PowerShell 7. Les tests Linux ont
  utilisé PowerShell 7, ce qui ne valide pas les cmdlets Windows.
- Faire approuver/signature des scripts selon la politique locale. **Aucun
  ExecutionPolicy Bypass**, désactivation de protection ou changement de registre.
  Une politique interdisant Add-Type exige l'accord de l'administrateur.
- Les mesures locales CIM/événements peuvent demander une console administrateur
  ou des droits de lecture appropriés. Un refus devient `IGNORE` / collecte
  indisponible, jamais une panne SQL inventée. Aucune connexion WinRM distante.
- Sortie locale, jamais un partage réseau. Protéger le dossier par les ACL
  usuelles ; noms de machines, IP et événements restent des données internes.
- Sans option d'authentification, **aucune connexion SQL authentifiée** : seulement
  DNS, TCP et ICMP. Aucun identifiant applicatif n'est utilisé automatiquement.
- L'option `ConfigurationERP` ne lit que la cible et le nom de base, sans sauver
  ni déprotéger le fichier, sans reprendre son login. DPAPI doit être lisible
  par le compte Windows autorisé ; ne jamais transmettre/copier le fichier.
- Aucun mot de passe, login SQL, chaîne complète, message libre d'exception,
  requête commerciale ou contenu de table n'est écrit dans les journaux.
  Ne pas utiliser de transcription PowerShell contenant des secrets saisis ailleurs.

## 1. Relever le vrai port et le nom de carte

Sur le PC caissière, ouvrir une console PowerShell approuvée :

```powershell
$PSVersionTable.PSVersion
Get-NetIPConfiguration
Get-NetAdapter | Select-Object Name,Status,LinkSpeed,NdisPhysicalMedium
Get-Service -Name 'MSSQL*','SQLBrowser'
Get-CimInstance Win32_Service -Filter "Name='MSSQLSERVER'" | Select-Object Name,State,ProcessId
Get-NetTCPConnection -State Listen | Select-Object LocalAddress,LocalPort,OwningProcess
```

Pour une instance nommée, remplacer le filtre par `Name='MSSQL$SQLEXPRESS'`
dans une chaîne littérale appropriée, par exemple :

```powershell
Get-CimInstance Win32_Service -Filter 'Name="MSSQL$SQLEXPRESS"' | Select-Object Name,State,ProcessId
```

Associer le PID au service et vérifier le port TCP dans SQL Server Configuration
Manager et le journal ERRORLOG, **sans changer la configuration**. Le service peut
écouter plusieurs ports (dont un port technique) : ne pas choisir arbitrairement.
L'auto-détection du port n'est utilisée qu'en mode SERVEUR, pour le service demandé,
et seulement si un unique port est trouvé. Sinon `-PortSql` est obligatoire.
Jamais de supposition que le port est 1433. Pas de découverte Browser active.

Le nom de carte est souvent `Ethernet`, mais peut être différent. Avec plusieurs
cartes/passerelles, `-InterfaceEthernet` est obligatoire. La passerelle IPv4 de la
carte est détectée, ou fournie explicitement par `-Passerelle` (adresse IP).
Un routeur qui ne répond pas au ping peut acheminer normalement les paquets.

## 2. Lancer une campagne commune de 30 à 60 minutes

Exemples ci-dessous : IP **192.168.1.10**, passerelle **192.168.1.1**, port
**1533**, service **MSSQLSERVER**. **Remplacer par les valeurs relevées.**
Copier le dossier sous `C:\ERPDiagnostic\diagnostic réseaux` sur chaque poste :

```powershell
Set-Location -LiteralPath 'C:\ERPDiagnostic\diagnostic réseaux'
Get-Date -Format o
w32tm /query /status
```

Sur un seul poste, calculer une heure UTC future et partager sa **valeur exacte** :

```powershell
([DateTimeOffset]::UtcNow.AddMinutes(5)).ToString('o')
```

La substituer à l'heure d'exemple dans toutes les commandes. Ne pas recalculer
une heure différente sur chaque poste. Les horloges doivent être cohérentes ;
le script ne les modifie pas.

**Sur le PC caissière / serveur**, depuis le dossier :

```powershell
.\DiagnosticReseauERP.ps1 -Mode SERVEUR -Serveur 192.168.1.10 -PortSql 1533 -ServiceSql MSSQLSERVER -InterfaceEthernet 'Ethernet' -Passerelle 192.168.1.1 -Campagne ERP_Incident_01 -DepartUtc '2026-10-23T15:00:00Z' -DureeMinutes 60
```

**Sur le PC facturier et chaque client**, depuis le même dossier local :

```powershell
.\DiagnosticReseauERP.ps1 -Mode CLIENT -Serveur 192.168.1.10 -PortSql 1533 -InterfaceEthernet 'Ethernet' -Passerelle 192.168.1.1 -Campagne ERP_Incident_01 -DepartUtc '2026-10-23T15:00:00Z' -DureeMinutes 60
```

Une vérification préalable courte est possible avec `-DureeMinutes 1`, puis
lancer la vraie campagne commune. Défaut : 30 minutes, une passe réseau/liaison
toutes les 30 secondes, système et SQL au plus toutes les 60 secondes.
Pas de cycles superposés ni de rafale pour rattraper un cycle lent. Une collecte
locale lente peut espacer les mesures et dépasser légèrement la durée prévue.

Pour tester la résolution du nom utilisée par l'application, lancer une campagne
avec son **nom serveur** plutôt que l'IP, ou lire sa cible :

```powershell
.\DiagnosticReseauERP.ps1 -Mode CLIENT -ConfigurationERP "$env:LOCALAPPDATA\CommercialPro\CommercialPro.exe.config" -PortSql 1533 -InterfaceEthernet 'Ethernet' -Campagne ERP_Nom_01 -DureeMinutes 30
```

Les options explicites `Serveur`/`PortSql` priment ; un conflit avec un port inclus
dans la cible est refusé. Une instance nommée est résolue en hôte + **port fourni**.
Le diagnostic force TCP vers cet hôte/port : il n'émule pas les alias SQL 32/64
bits, Named Pipes, Shared Memory ou la découverte Browser de l'ERP.
La base lue dans la configuration n'est pas utilisée implicitement pour SQL.

## 3. Ajouter les sondes SQL, seulement avec autorisation

Faire valider un compte de diagnostic à droits minimaux permettant de se connecter
à la base choisie et d'exécuter `SELECT 1`/les métadonnées de sa propre connexion.
Pas besoin de droits d'écriture ou de lecture commerciale, ni de rôle sysadmin.
Le script ne crée aucun compte ni permission.

**Authentification Windows** : uniquement si le compte Windows courant a été
autorisé comme compte de diagnostic. Ajouter à la commande de collecte :

```powershell
-Authentification Windows -BaseDiagnostic master
```

Exemple complet sur un client :

```powershell
.\DiagnosticReseauERP.ps1 -Mode CLIENT -Serveur 'PC-CAISSE' -PortSql 1533 -InterfaceEthernet 'Ethernet' -Campagne ERP_SQL_01 -DureeMinutes 30 -Authentification Windows -BaseDiagnostic master
```

**Authentification SQL**, saisie dans l'invite sécurisée et jamais en clair :

```powershell
$compteDiagnostic = Get-Credential -Message 'Compte SQL de diagnostic autorisé'
.\DiagnosticReseauERP.ps1 -Mode SERVEUR -Serveur 'PC-CAISSE' -PortSql 1533 -ServiceSql MSSQLSERVER -InterfaceEthernet 'Ethernet' -Campagne ERP_SQL_01 -DureeMinutes 30 -Authentification Sql -IdentifiantsSql $compteDiagnostic -BaseDiagnostic master
Remove-Variable compteDiagnostic
```

Sur les clients, utiliser la même commande avec `-Mode CLIENT`, selon les droits
validés. Pour comparer exactement les heures, ajouter le même `-DepartUtc`.
Tester d'abord un poste ; après un refus d'authentification, les nouvelles sondes
SQL de cette exécution sont suspendues pour limiter les verrouillages. Les sondes
réseau continuent. Corriger puis relancer explicitement, pas de retry de login.

TLS est demandé avec validation du certificat. Préférer le nom correspondant au
certificat ; un refus de certificat ne prouve pas une panne physique. L'option
`-FaireConfianceCertificat` ne concerne que cette sonde, doit être approuvée
par l'administrateur et ne doit pas devenir un « correctif » général de sécurité.
Les erreurs non reconnues restent non classées (notamment certains détails TLS).
La connexion fraîche sans pooling est isolée du processus ERP ; délais des sondes
3 secondes par défaut, aucun timeout commercial changé. La base `master` disponible
ne prouve pas la disponibilité de la base métier : utiliser une base autorisée
appropriée si l'administrateur souhaite tester aussi ce niveau.

## Mesures et formats

Sortie par défaut : `%LOCALAPPDATA%\CommercialPro\DiagnosticReseau`.
Chaque exécution crée des fichiers uniques `campagne_poste_mode_identifiant` :

- `.csv` UTF-8 avec BOM pour Excel : une ligne par mesure, champs stables.
- `.jsonl` UTF-8 : objets JSON, un par ligne, conservables même après interruption.
- `.json` UTF-8 : tableau JSON complet produit à la fin normale par conversion en
  flux du JSONL. Après arrêt brutal, seul le JSONL/CSV peut être disponible.
  La synthèse produit aussi un rapport `.json` global. Conserver le JSONL pour elle.

Champs communs : poste, mode, campagne, exécution, cycle, UTC, étape, cible,
état, durée de sonde, transition, identifiant d'incident, début/fin observés,
durée observée et indicateurs de censure. `DetailsJson` contient uniquement des
valeurs autorisées (codes SQL/états/classes, compteurs, ressources, événements).
Les instants de début/fin des sondes DNS/TCP/SQL sont aussi conservés.

| Étape / type | Mesure réelle |
| --- | --- |
| PASSERELLE_ICMP | Réponse du routeur ; absence de réponse ≠ preuve de coupure LAN |
| DNS / ICMP | Résolution et ping de chaque adresse du serveur |
| TCP | Ouverture du véritable port SQL sur chaque IP résolue |
| ETHERNET | État local, débit et variations de liaison entre deux échantillons |
| COMPTEURS | Erreurs réception/émission, écarts ou remise à zéro du compteur |
| SERVICE_SQL | État du service local en mode SERVEUR, pas d'interrogation distante |
| SQL_OUVERTURE | Authentification ; codes exacts de tous les SqlError |
| SQL_REQUETE | Constante 1 et métadonnées de session ; aucun accès métier |
| RESSOURCES | CPU global, mémoire disponible, compteurs disques exposés par CIM |
| EVENEMENT_WINDOWS | UTC, fournisseur, numéro, niveau et RecordId, sans Message |

Les événements Système critiques/erreurs/avertissements sont relevés sur chaque
poste. Le serveur ajoute Application. Certains événements informatifs Système
réseau/SCM et du fournisseur SQL sélectionné sont inclus. Limite : 100 événements par journal/passe avec indication
de troncature ; des changements très courts peuvent n'être vus que dans ces logs.
Les compteurs disque sont les valeurs exposées par CIM, pas un test de débit ou
un benchmark. Ils doivent être interprétés avec les mesures DBA/PerfMon.

`SUCCES` signifie réussite de **cette sonde**, pas santé globale du système.
`IGNORE` signifie non mesuré/inconnu, jamais une preuve de panne. Pas de sonde
SQL si non demandée. Une carte manquante ou un refus de droits est inconnu.

## 4. Centraliser et comparer

Après la campagne, copier uniquement ses `.jsonl` et `.csv` dans un dossier de
support protégé, par exemple `C:\CollecteERP\Incident01`. Ne pas copier la config
ERP, des secrets ou le profil Windows. Ne pas laisser de collecte active écrire
sur un partage réseau. Éviter de mélanger de vraies mesures avec les exemples.

Depuis le dossier de l'outil :

```powershell
.\SyntheseDiagnosticERP.ps1 -DossierJournaux 'C:\CollecteERP\Incident01' -Campagne ERP_Incident_01
```

La synthèse utilise les JSONL, déduplique les copies d'une même mesure et produit :

- `_mesures.csv` : réussites, échecs, inconnus, fréquence d'échec et incidents
  par poste/étape/cible. Le taux est un taux d'échantillons, pas un SLA.
- `_coincidences.csv` : couples d'incidents de postes différents avec chevauchement
  UTC (fichier absent s'il n'y a aucune coïncidence).
- `.json` : mesures, incidents ouverts/fermés et coïncidences, exploitable par outil.
- `.md` : résumé et limites.

La tolérance par défaut est zéro. L'option `-ToleranceSecondes 5` signale aussi
les proximités de cinq secondes, clairement distinguées des chevauchements.
Elle ne corrige pas les horloges et ne prouve pas une panne simultanée.
Au-delà de 2000 incidents, réduire la campagne pour borner la comparaison.

### Durées : ne pas surinterpréter

Un incident commence à la première mesure en échec et finit au premier succès.
La durée locale est calculée par chronomètre monotone. Les horodatages UTC servent
à la comparaison inter-postes. La résolution dépend des intervalles et des temps
de sonde ; la coupure physique peut commencer avant la détection et finir avant
le succès observé. Ce n'est donc pas sa durée exacte.
`CensureDebut=true` : déjà en échec au premier échantillon.
`CensureFin=true` : toujours en échec en fin de collecte.
Une mesure inconnue ne ferme pas un incident. Un arrêt brutal peut empêcher sa
ligne finale : les échecs bruts restent visibles, mais la synthèse ne reconstruit
pas fictivement sa fin. Une horloge qui saute rend les comparaisons UTC suspectes.

## Interprétation et contrôles complémentaires

| Observations corrélées | Piste à vérifier, pas conclusion automatique |
| --- | --- |
| Carte locale Down + routeur/serveur injoignables | Câble du poste, port du routeur/switch, NIC/pilote |
| Routeur ICMP seul indisponible, TCP/SQL réussis | Filtrage/priorisation ICMP ; pas une panne SQL démontrée |
| Plusieurs clients perdent TCP, serveur local/service restent bons | Segment partagé, routeur/switch, interface serveur, pare-feu ; mesurer sur serveur |
| TCP et service SQL arrêtés sur serveur aux mêmes heures | Arrêt/redémarrage SQL ; confronter SCM et ERRORLOG |
| TCP réussi, SQL 18456/18452 | Authentification refusée ; suspendre les tentatives et vérifier le compte |
| Ouverture réussie, timeout SQL_REQUETE | Attente/charge/transport après connexion ; pas forcément panne LAN |
| Nom échoue, campagne IP réussit | DNS/résolution/alias à examiner, sans modifier automatiquement |

Le test local du serveur vers sa propre IP **ne traverse pas forcément le câble
RJ45**. Son succès ne valide donc pas le port physique ou le switch.
Une sonde sur une nouvelle connexion ne surveille pas les sockets/pools de l'ERP.

Sur le serveur réel, confronter la fenêtre UTC aux événements Windows et à
ERRORLOG (redémarrages, écoute TCP, erreurs réseau, base inaccessible), aux
sessions/verrous SQL via les outils DBA déjà approuvés, aux ressources CPU/RAM/
disque et aux journaux du routeur/switch. Vérifier carte réseau et économie
d'énergie **indépendamment** de la veille système déjà désactivée. Ne pas changer
les pilotes, câbles, ports, pare-feu ou options SQL sans intervention planifiée.
Si les sondes ne suffisent pas, faire réaliser des captures réseau simultanées
serveur/client approuvées ; elles peuvent contenir des informations sensibles.

## Tests et limites de validation

```powershell
.\Tests-Diagnostic.ps1
```

Les tests analysent la syntaxe, les incidents/censures, les chevauchements,
le port explicite, les catégories SQL, CSV/JSONL, les sockets TCP de boucle
locale ouvertes/fermées/rétablies et la synthèse complète sur les exemples.
Ils n'interrogent pas de service Windows ni de base SQL et ne prouvent pas
la santé de la production. Les fixtures temporaires sont les seuls fichiers
de test supprimés par ce banc ; les journaux existants ne sont jamais nettoyés.

Validation effectuée avant publication : **80 contrôles réussis sous
PowerShell 7.4.13 / Linux ARM64**, dont compilation du helper C#, lecture sans
modification d'une configuration temporaire et absence de restitution de ses
secrets. Les quatre scripts sont analysés syntaxiquement. Les sept commandes de
scripts présentes dans ce README sont également contrôlées par comparaison de
leurs paramètres avec les métadonnées réelles, sans lancer de collecte.
`Mode` accepte SERVEUR/CLIENT, `Serveur` accepte le nom/IP, `PortSql` accepte
le port explicite de 1 à 65535 (0 réservé à la détection) et `DureeMinutes`
accepte 1 à 1440 minutes, avec 30 minutes par défaut. Les exemples de 30 et
60 minutes et le port illustratif 1533 respectent ces contraintes.
Aucun module commercial
n'a été modifié. La collecte complète est volontairement interdite hors Windows.
Le banc existant `AuditMetierCoreTests` a également été relancé : **179 contrôles
métier sans SQL réussis**. Aucun build WinForms n'est présenté comme validé par
cette livraison de scripts indépendants.
**Non exécutés : Windows PowerShell 5.1, cmdlets réseau/services/CIM/événements
Windows, authentification SQL réelle, SELECT 1 sur SQL Server et tests RJ45.**
La compatibilité visée reste à confirmer sur les véritables versions Windows.

À exécuter sous Windows sur environnement **de test** :

1. Une minute SERVEUR/CLIENT, sans SQL puis avec compte diagnostic approuvé.
2. CSV/JSONL : vérifier même campagne, UTC, carte, passerelle, port réel et absence
   de secrets ; comparer l'état local du service aux mesures.
3. Campagne simultanée 30–60 minutes ; vérifier cadence et charge induite.
4. Débrancher/rebrancher le câble d'un poste TEST ; inspecter incident et retour.
5. Arrêter/redémarrer le service SQL TEST avec l'administrateur, hors production ;
   différencier service, TCP et routeur. Le script ne le fait jamais lui-même.
6. Tester un refus auth une seule fois sur compte de test, vérifier suspension
   SQL ; vérifier une requête légère réussie après correction et relance.
7. Droit événement/CIM refusé, routeur ne répondant pas ICMP, nom/port incorrect,
   dossier non accessible : erreurs explicites/inconnues sans reconfiguration.
8. Comparer la synthèse avec les véritables heures d'interruption notées.

Les scénarios pendant facture/paiement, s'ils sont ensuite nécessaires, relèvent
d'une procédure métier de test séparée. Ce collecteur ne déclenche aucune vente,
transaction ou tentative d'encaissement, et n'en répète aucune.

## Références Microsoft

- [Get-NetAdapterStatistics](https://learn.microsoft.com/en-us/powershell/module/netadapter/get-netadapterstatistics)
- [Get-NetIPConfiguration](https://learn.microsoft.com/en-us/powershell/module/nettcpip/get-netipconfiguration)
- [Get-NetTCPConnection](https://learn.microsoft.com/en-us/powershell/module/nettcpip/get-nettcpconnection)
- [Get-WinEvent](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.diagnostics/get-winevent)
- [Diagnostic des interruptions SQL](https://learn.microsoft.com/en-us/troubleshoot/sql/database-engine/connect/intermittent-periodic-network-issue)
