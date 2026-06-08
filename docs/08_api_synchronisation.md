# API de synchronisation

L'API de synchronisation est un projet séparé de l'ERP WinForms et du Dashboard Web Patron.

## Technologie

- ASP.NET Core Web API
- JWT
- SQL Server

## Rôle

- recevoir les données transmises par l'ERP WinForms
- synchroniser les ventes
- synchroniser les sorties manuelles
- synchroniser les dépenses
- fournir les données du dashboard web patron

## Base de données

- base cloud: `CommercialMagCloudDB`

## Emplacement

- projet API: `prototype/ApiCommercialMagDB`

## Solution dédiée

- `prototype/ApiCommercialMagDB/CommercialMagDb.Api.sln`

## Contraintes

- pas d'accès direct au dashboard web
- pas d'accès direct à la base locale de l'ERP
- API consommée uniquement par les clients autorisés

