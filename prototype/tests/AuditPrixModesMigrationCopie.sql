-- SQLCMD uniquement, depuis prototype/tests. Ne changez pas le nom de copie.
:On Error exit
USE [ERP_Phase1_TEST];
GO
IF DB_NAME()<>N'ERP_Phase1_TEST' THROW 51000, 'Copie ERP_Phase1_TEST obligatoire.', 1;
SELECT PaiementId,FactureVenteId,ModePaiement,ReferencePaiement,Montant,MontantRecu,MonnaieRendue,Devise,PayePar,PayeLe INTO #PaiementsAvant FROM dbo.Paiements;
SELECT ProduitId,PrixAchat,PrixDetail,PrixGros,PrixDemi,PrixQuart,PrixDouzaine,PrixSpecial,ConversionUnite INTO #ProduitsAvant FROM dbo.Produits;
SELECT RoleId,InterfaceId INTO #DroitsAvant FROM dbo.RoleInterfaces;
SELECT UtilisateurId,RoleId,EstActif,EstRolePrincipal INTO #RolesAvant FROM dbo.UtilisateurRoles;
SELECT AuditId,[Action],Details,EffectuePar,EffectueLe,UtilisateurNom,RoleActif,AnciennesValeurs,NouvellesValeurs INTO #AuditAvant FROM dbo.JournalAudit;
GO
:r ../WinFormsVB/DevCommerc8ak/Migrations/2026100902_TracabilitePaiements.sql
GO
:r ../WinFormsVB/DevCommerc8ak/Migrations/2026100903_ModesTravail.sql
GO
:r ../WinFormsVB/DevCommerc8ak/Migrations/2026100904_DelegationPrix.sql
GO
:r ../WinFormsVB/DevCommerc8ak/Migrations/2026100902_TracabilitePaiements.sql
GO
:r ../WinFormsVB/DevCommerc8ak/Migrations/2026100903_ModesTravail.sql
GO
:r ../WinFormsVB/DevCommerc8ak/Migrations/2026100904_DelegationPrix.sql
GO
IF EXISTS(SELECT * FROM #PaiementsAvant EXCEPT SELECT PaiementId,FactureVenteId,ModePaiement,ReferencePaiement,Montant,MontantRecu,MonnaieRendue,Devise,PayePar,PayeLe FROM dbo.Paiements)
 OR (SELECT COUNT_BIG(*) FROM #PaiementsAvant)<>(SELECT COUNT_BIG(*) FROM dbo.Paiements)
 THROW 51000, 'Ancien paiement modifie ou perdu.', 1;
IF EXISTS(SELECT * FROM #ProduitsAvant EXCEPT SELECT ProduitId,PrixAchat,PrixDetail,PrixGros,PrixDemi,PrixQuart,PrixDouzaine,PrixSpecial,ConversionUnite FROM dbo.Produits)
 THROW 51000, 'Prix ou conversion modifies par la migration.', 1;
IF EXISTS(SELECT * FROM #DroitsAvant EXCEPT SELECT RoleId,InterfaceId FROM dbo.RoleInterfaces)
 OR EXISTS(SELECT RoleId,InterfaceId FROM dbo.RoleInterfaces EXCEPT SELECT * FROM #DroitsAvant)
 THROW 51000, 'Une permission a ete perdue ou attribuee automatiquement.', 1;
IF EXISTS(SELECT * FROM #RolesAvant EXCEPT SELECT UtilisateurId,RoleId,EstActif,EstRolePrincipal FROM dbo.UtilisateurRoles)
 THROW 51000, 'Association multirole modifiee.', 1;
IF EXISTS(SELECT * FROM #AuditAvant EXCEPT SELECT AuditId,[Action],Details,EffectuePar,EffectueLe,UtilisateurNom,RoleActif,AnciennesValeurs,NouvellesValeurs FROM dbo.JournalAudit)
 OR (SELECT COUNT_BIG(*) FROM #AuditAvant)<>(SELECT COUNT_BIG(*) FROM dbo.JournalAudit)
 THROW 51000, 'Ancienne trace modifiee ou perdue.', 1;
PRINT N'VALIDATION_MIGRATION_AUDIT_PRIX_MODES_CONSERVATION_OK';
GO
:r AuditPrixModesValidation.sql
