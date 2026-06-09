# DashboardWebPatron

Projet web séparé pour la consultation patron.

Ouvrir en priorité la solution dédiée:

- `DashboardWebPatron.sln`

## Rôle

- lecture seule
- consommation exclusive de l'API de synchronisation
- aucun accès direct à la base locale de l'ERP

## Points d'entrée

- `/Dashboard/Index`
- `/Dashboard/AnalyseVente`
- `/Dashboard/TV`
- `/Dashboard/Mobile`

## Configuration

Le client API est configuré dans `appsettings.json` via:

- `DashboardApi:BaseUrl`
- `DashboardApi:AccessToken`
- `DashboardApi:Username`
- `DashboardApi:Password`

## Lancement

- `dotnet run`
- ou ouverture dans Visual Studio via `DashboardWebPatron.sln`
