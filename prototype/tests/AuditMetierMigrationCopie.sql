-- Mode SQLCMD obligatoire. Lancer depuis le dossier prototype/tests.
-- Ce script applique uniquement la migration additive sur une COPIE dédiée.
-- Il ne réinitialise aucune base et ne supprime aucune donnée commerciale.
:On Error exit
USE [ERP_Phase1_TEST];
GO
IF DB_NAME() <> N'ERP_Phase1_TEST'
    THROW 51000, 'Copie ERP_Phase1_TEST obligatoire.', 1;
SELECT UtilisateurId,NomUtilisateur,MotDePasseHash,MotDePasseSel,EstActif
INTO #AvantUtilisateurs FROM dbo.Utilisateurs;
SELECT RoleId,NomRole INTO #AvantRoles FROM dbo.Roles;
CREATE TABLE #AvantPermissions(RoleId int,InterfaceId int);
CREATE TABLE #AvantInterfaces(InterfaceId int);
IF OBJECT_ID(N'dbo.RoleInterfaces',N'U') IS NOT NULL
    EXEC(N'INSERT #AvantPermissions SELECT RoleId,InterfaceId FROM dbo.RoleInterfaces');
IF OBJECT_ID(N'dbo.InterfacesApplication',N'U') IS NOT NULL
    EXEC(N'INSERT #AvantInterfaces SELECT InterfaceId FROM dbo.InterfacesApplication');
SELECT UtilisateurId,RoleId INTO #AvantAssociations FROM dbo.UtilisateurRoles;
SELECT AuditId,[Action],Entite,EntiteId,Details,EffectuePar,EffectueLe
INTO #AvantJournal FROM dbo.JournalAudit;
CREATE TABLE #AvantAuditActions(AuditActionId bigint,Utilisateur nvarchar(80),[Role] nvarchar(50),Module nvarchar(80),[Action] nvarchar(100),[Description] nvarchar(255),Machine nvarchar(100),[Statut] nvarchar(30),CreeLe datetime2);
IF OBJECT_ID(N'dbo.AuditActions',N'U') IS NOT NULL
    EXEC(N'INSERT #AvantAuditActions SELECT AuditActionId,Utilisateur,[Role],Module,[Action],[Description],Machine,[Statut],CreeLe FROM dbo.AuditActions');
GO
:r ../WinFormsVB/DevCommerc8ak/Migrations/2026100901_AuditMetier.sql
GO
-- Idempotence : la deuxième exécution ne doit rien réamorcer.
:r ../WinFormsVB/DevCommerc8ak/Migrations/2026100901_AuditMetier.sql
GO
IF EXISTS(SELECT * FROM #AvantUtilisateurs EXCEPT SELECT UtilisateurId,NomUtilisateur,MotDePasseHash,MotDePasseSel,EstActif FROM dbo.Utilisateurs)
    THROW 51000, 'Un ancien compte a été supprimé ou modifié.', 1;
IF (SELECT COUNT_BIG(*) FROM #AvantUtilisateurs)<>(SELECT COUNT_BIG(*) FROM dbo.Utilisateurs)
    THROW 51000, 'Le nombre de comptes a changé.', 1;
IF EXISTS(SELECT * FROM #AvantRoles EXCEPT SELECT RoleId,NomRole FROM dbo.Roles)
    THROW 51000, 'Un ancien rôle a été supprimé ou renommé.', 1;
IF EXISTS(SELECT * FROM #AvantPermissions EXCEPT SELECT RoleId,InterfaceId FROM dbo.RoleInterfaces)
    THROW 51000, 'Une permission existante a été perdue.', 1;
-- Aucun nouveau droit ne doit être ajouté pour un écran déjà connu.
IF EXISTS(SELECT ri.RoleId,ri.InterfaceId FROM dbo.RoleInterfaces ri
          WHERE ri.InterfaceId IN(SELECT InterfaceId FROM #AvantInterfaces)
            AND ri.RoleId IN(SELECT RoleId FROM #AvantRoles)
          EXCEPT SELECT RoleId,InterfaceId FROM #AvantPermissions)
    THROW 51000, 'Un droit existant retiré a été réamorcé.', 1;
IF EXISTS(SELECT * FROM #AvantAssociations EXCEPT SELECT UtilisateurId,RoleId FROM dbo.UtilisateurRoles)
    THROW 51000, 'Une association multirôle a été perdue.', 1;
IF EXISTS(SELECT * FROM #AvantJournal EXCEPT SELECT AuditId,[Action],Entite,EntiteId,Details,EffectuePar,EffectueLe FROM dbo.JournalAudit)
    THROW 51000, 'Une ancienne trace a été modifiée ou perdue.', 1;
IF (SELECT COUNT_BIG(*) FROM #AvantJournal)<>(SELECT COUNT_BIG(*) FROM dbo.JournalAudit)
    THROW 51000, 'Le nombre des anciennes traces a changé.', 1;
IF EXISTS(SELECT * FROM #AvantAuditActions EXCEPT SELECT AuditActionId,Utilisateur,[Role],Module,[Action],[Description],Machine,[Statut],CreeLe FROM dbo.AuditActions)
    THROW 51000, 'Une ancienne trace AuditActions a été modifiée ou perdue.', 1;
IF (SELECT COUNT_BIG(*) FROM #AvantAuditActions)<>(SELECT COUNT_BIG(*) FROM dbo.AuditActions)
    THROW 51000, 'Le nombre des anciennes traces AuditActions a changé.', 1;
PRINT N'VALIDATION_MIGRATION_COPIE_CONSERVATION_OK';
GO
:r AuditMetierPhase1Validation.sql
