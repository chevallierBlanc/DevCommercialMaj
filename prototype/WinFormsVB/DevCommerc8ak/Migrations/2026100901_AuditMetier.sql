-- SQL Server 2014. Schéma additif ; aucun historique n'est réécrit.
-- Exécutable dans la transaction du migrateur ou directement dans SSMS.
SET XACT_ABORT ON;
DECLARE @OwnTransaction bit = CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
IF @OwnTransaction=1 BEGIN TRANSACTION;
BEGIN TRY
IF OBJECT_ID(N'dbo.SchemaVersion',N'U') IS NULL
    CREATE TABLE dbo.SchemaVersion(Version int NOT NULL PRIMARY KEY, DateApplication datetime2 NOT NULL DEFAULT(SYSDATETIME()), Description nvarchar(255) NOT NULL);
IF EXISTS(SELECT 1 FROM dbo.SchemaVersion WITH (UPDLOCK,HOLDLOCK) WHERE Version=2026100901)
BEGIN
    IF @OwnTransaction=1 COMMIT;
    RETURN;
END;
IF COL_LENGTH(N'dbo.Roles',N'EstActif') IS NULL
    ALTER TABLE dbo.Roles ADD EstActif bit NOT NULL CONSTRAINT DF_Roles_Actif_Audit DEFAULT(1);
IF OBJECT_ID(N'dbo.InterfacesApplication',N'U') IS NULL
    CREATE TABLE dbo.InterfacesApplication(InterfaceId int IDENTITY PRIMARY KEY, CodeInterface nvarchar(80) NOT NULL UNIQUE, Libelle nvarchar(150) NOT NULL, EstTechnique bit NOT NULL DEFAULT(0), EstActif bit NOT NULL DEFAULT(1));
-- Une installation possédant déjà RoleInterfaces conserve même ses rôles
-- sans aucun droit. Seule l'absence du système permet l'amorçage historique.
DECLARE @Amorcage bit=0;
DECLARE @RolesNouveaux TABLE(RoleId int);
INSERT dbo.Roles(NomRole)
OUTPUT inserted.RoleId INTO @RolesNouveaux
SELECT v.Nom FROM (VALUES(N'SUPERADMIN'),(N'ADMIN'),(N'FACTURIER'),(N'CAISSIER'),(N'CAISSIERE')) v(Nom)
WHERE NOT EXISTS(SELECT 1 FROM dbo.Roles r WHERE r.NomRole=v.Nom);
IF OBJECT_ID(N'dbo.RoleInterfaces',N'U') IS NULL
BEGIN
    SET @Amorcage=1;
    CREATE TABLE dbo.RoleInterfaces(RoleId int NOT NULL, InterfaceId int NOT NULL, PRIMARY KEY(RoleId,InterfaceId));
END;
DECLARE @Ecrans TABLE(Code nvarchar(80), Libelle nvarchar(150));
INSERT @Ecrans VALUES(N'FACTURIER',N'Facturation'),(N'CAISSE',N'Caisse'),(N'HISTORIQUE_FACTURES',N'Historique factures'),(N'ADMINISTRATION',N'Administration'),(N'FINANCE',N'Finance'),(N'PARAMETRES',N'Paramètres'),(N'STOCK_INVENTAIRE',N'Stock / Inventaire'),(N'ANALYSE_VENTES',N'Analyse ventes'),(N'INVENTAIRE',N'Inventaire'),(N'ANALYSE_CAISSE_PHYSIQUE',N'Analyse caisse physique'),(N'SUPERADMIN_TECH',N'Interfaces techniques'),(N'SUPERADMIN_ROLES',N'Rôles'),(N'SUPERADMIN_AUDIT',N'Journal métier'),(N'SUPERADMIN_STOCK_INITIAL',N'Stock initial'),(N'SUPERADMIN_INIT_VENTES',N'Initialisation ventes');
INSERT dbo.InterfacesApplication(CodeInterface,Libelle,EstTechnique,EstActif)
SELECT e.Code,e.Libelle,CASE WHEN e.Code LIKE N'SUPERADMIN_%' THEN 1 ELSE 0 END,1 FROM @Ecrans e WHERE NOT EXISTS(SELECT 1 FROM dbo.InterfacesApplication i WHERE i.CodeInterface=e.Code);
    INSERT dbo.RoleInterfaces(RoleId,InterfaceId)
    SELECT r.RoleId,i.InterfaceId FROM dbo.Roles r CROSS JOIN dbo.InterfacesApplication i
    WHERE (r.NomRole=N'SUPERADMIN' OR (r.NomRole=N'ADMIN' AND i.EstTechnique=0)
       OR (r.NomRole=N'FACTURIER' AND i.CodeInterface IN(N'FACTURIER',N'HISTORIQUE_FACTURES'))
       OR (r.NomRole IN(N'CAISSIER',N'CAISSIERE') AND i.CodeInterface IN(N'CAISSE',N'FINANCE')))
      AND (@Amorcage=1 OR EXISTS(SELECT 1 FROM @RolesNouveaux n WHERE n.RoleId=r.RoleId))
      AND NOT EXISTS(SELECT 1 FROM dbo.RoleInterfaces ri WHERE ri.RoleId=r.RoleId AND ri.InterfaceId=i.InterfaceId);
DECLARE @Actions TABLE(Code nvarchar(80), Libelle nvarchar(150), Ecran nvarchar(80));
INSERT @Actions VALUES(N'FACTURE_CREER',N'Créer une facture',N'FACTURIER'),(N'FACTURE_MODIFIER',N'Modifier un brouillon',N'FACTURIER'),(N'FACTURE_ANNULER',N'Annuler un brouillon',N'HISTORIQUE_FACTURES'),(N'ENCAISSEMENT_CREER',N'Encaisser une facture',N'CAISSE'),(N'AUDIT_CONSULTER',N'Consulter le journal métier',N'SUPERADMIN_AUDIT');
INSERT @Actions VALUES(N'CAISSE_REGULARISER',N'Régulariser un écart de clôture',N'ANALYSE_CAISSE_PHYSIQUE');
DECLARE @Nouvelles TABLE(InterfaceId int, Code nvarchar(80));
INSERT dbo.InterfacesApplication(CodeInterface,Libelle,EstTechnique,EstActif)
OUTPUT inserted.InterfaceId,inserted.CodeInterface INTO @Nouvelles
SELECT a.Code,a.Libelle,0,1 FROM @Actions a WHERE NOT EXISTS(SELECT 1 FROM dbo.InterfacesApplication i WHERE i.CodeInterface=a.Code);
-- Compatibilité initiale fondée exclusivement sur les droits SQL existants.
-- Pas de privilège automatique lié au nom ADMIN/SUPERADMIN.
INSERT dbo.RoleInterfaces(RoleId,InterfaceId)
SELECT DISTINCT ri.RoleId,n.InterfaceId FROM @Nouvelles n JOIN @Actions a ON a.Code=n.Code JOIN dbo.InterfacesApplication e ON e.CodeInterface=a.Ecran JOIN dbo.RoleInterfaces ri ON ri.InterfaceId=e.InterfaceId;
-- L'annulation existait aussi depuis la caisse : même action, écran distinct.
INSERT dbo.RoleInterfaces(RoleId,InterfaceId)
SELECT DISTINCT ri.RoleId,n.InterfaceId FROM @Nouvelles n JOIN dbo.InterfacesApplication e ON e.CodeInterface=N'CAISSE' JOIN dbo.RoleInterfaces ri ON ri.InterfaceId=e.InterfaceId
WHERE n.Code=N'FACTURE_ANNULER' AND NOT EXISTS(SELECT 1 FROM dbo.RoleInterfaces p WHERE p.RoleId=ri.RoleId AND p.InterfaceId=n.InterfaceId);
IF OBJECT_ID(N'dbo.JournalAudit',N'U') IS NULL
    CREATE TABLE dbo.JournalAudit(AuditId bigint IDENTITY PRIMARY KEY, [Action] nvarchar(100) NOT NULL, Entite nvarchar(100) NOT NULL, EntiteId nvarchar(50) NOT NULL, Details nvarchar(1000) NULL, EffectuePar int NOT NULL REFERENCES dbo.Utilisateurs(UtilisateurId), EffectueLe datetime2 NOT NULL DEFAULT(SYSUTCDATETIME()));
-- Sur une nouvelle installation, protéger aussi la source historique avant
-- que le premier appel de journalisation technique ne la crée implicitement.
IF OBJECT_ID(N'dbo.AuditActions',N'U') IS NULL
    CREATE TABLE dbo.AuditActions(AuditActionId bigint IDENTITY PRIMARY KEY,Utilisateur nvarchar(80) NULL,[Role] nvarchar(50) NULL,Module nvarchar(80) NULL,[Action] nvarchar(100) NULL,[Description] nvarchar(255) NULL,Machine nvarchar(100) NULL,[Statut] nvarchar(30) NULL,CreeLe datetime2 NOT NULL DEFAULT(GETDATE()));
IF COL_LENGTH(N'dbo.JournalAudit',N'Provenance') IS NULL ALTER TABLE dbo.JournalAudit ADD Provenance nvarchar(40) NULL;
IF COL_LENGTH(N'dbo.JournalAudit',N'Categorie') IS NULL ALTER TABLE dbo.JournalAudit ADD Categorie nvarchar(80) NULL;
IF COL_LENGTH(N'dbo.JournalAudit',N'ReferenceDocument') IS NULL ALTER TABLE dbo.JournalAudit ADD ReferenceDocument nvarchar(100) NULL;
IF COL_LENGTH(N'dbo.JournalAudit',N'UtilisateurNom') IS NULL ALTER TABLE dbo.JournalAudit ADD UtilisateurNom nvarchar(80) NULL;
IF COL_LENGTH(N'dbo.JournalAudit',N'RoleActif') IS NULL ALTER TABLE dbo.JournalAudit ADD RoleActif nvarchar(80) NULL;
IF COL_LENGTH(N'dbo.JournalAudit',N'ModeActif') IS NULL ALTER TABLE dbo.JournalAudit ADD ModeActif nvarchar(40) NULL;
IF COL_LENGTH(N'dbo.JournalAudit',N'SessionId') IS NULL ALTER TABLE dbo.JournalAudit ADD SessionId int NULL;
IF COL_LENGTH(N'dbo.JournalAudit',N'CorrelationId') IS NULL ALTER TABLE dbo.JournalAudit ADD CorrelationId uniqueidentifier NULL;
IF COL_LENGTH(N'dbo.JournalAudit',N'Poste') IS NULL ALTER TABLE dbo.JournalAudit ADD Poste nvarchar(100) NULL;
IF COL_LENGTH(N'dbo.JournalAudit',N'AnciennesValeurs') IS NULL ALTER TABLE dbo.JournalAudit ADD AnciennesValeurs nvarchar(max) NULL;
IF COL_LENGTH(N'dbo.JournalAudit',N'NouvellesValeurs') IS NULL ALTER TABLE dbo.JournalAudit ADD NouvellesValeurs nvarchar(max) NULL;
IF COL_LENGTH(N'dbo.JournalAudit',N'Motif') IS NULL ALTER TABLE dbo.JournalAudit ADD Motif nvarchar(1000) NULL;
IF COL_LENGTH(N'dbo.JournalAudit',N'Resultat') IS NULL ALTER TABLE dbo.JournalAudit ADD Resultat nvarchar(30) NULL;
IF COL_LENGTH(N'dbo.FacturesVente',N'VersionOperation') IS NULL ALTER TABLE dbo.FacturesVente ADD VersionOperation rowversion;
IF COL_LENGTH(N'dbo.LignesFactureVente',N'CoutUnitaireBaseVente') IS NULL ALTER TABLE dbo.LignesFactureVente ADD CoutUnitaireBaseVente decimal(18,4) NULL;
IF OBJECT_ID(N'dbo.UtilisateurSessions',N'U') IS NULL
    CREATE TABLE dbo.UtilisateurSessions(SessionId int IDENTITY PRIMARY KEY,UtilisateurId int NOT NULL,RoleIdActif int NULL,RoleSession nvarchar(80) NULL,Debut datetime2 NOT NULL DEFAULT(SYSUTCDATETIME()),DernierPing datetime2 NOT NULL DEFAULT(SYSUTCDATETIME()),Fin datetime2 NULL,Poste nvarchar(100) NULL);
IF COL_LENGTH(N'dbo.UtilisateurSessions',N'ModeActif') IS NULL ALTER TABLE dbo.UtilisateurSessions ADD ModeActif nvarchar(40) NULL;
-- Modes et délégation : réservés à la phase suivante, désactivés par défaut.
IF COL_LENGTH(N'dbo.Parametres',N'GestionModesActive') IS NULL ALTER TABLE dbo.Parametres ADD GestionModesActive bit NOT NULL DEFAULT(0);
IF COL_LENGTH(N'dbo.Parametres',N'DelegationPrixActive') IS NULL ALTER TABLE dbo.Parametres ADD DelegationPrixActive bit NOT NULL DEFAULT(0);
IF OBJECT_ID(N'dbo.TR_JournalAudit_AppendOnly',N'TR') IS NULL
    EXEC(N'CREATE TRIGGER dbo.TR_JournalAudit_AppendOnly ON dbo.JournalAudit INSTEAD OF UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51000, ''Le journal métier est en lecture seule.'', 1; END;');
-- Restriction applicable aux membres ordinaires de public. sysadmin/dbo et
-- un administrateur capable de modifier le schéma restent hors garantie.
DENY UPDATE, DELETE ON OBJECT::dbo.JournalAudit TO public;
IF OBJECT_ID(N'dbo.AuditActions',N'U') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'dbo.TR_AuditActions_AppendOnly',N'TR') IS NULL
        EXEC(N'CREATE TRIGGER dbo.TR_AuditActions_AppendOnly ON dbo.AuditActions INSTEAD OF UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51000, ''Les traces historiques sont en lecture seule.'', 1; END;');
    DENY UPDATE, DELETE ON OBJECT::dbo.AuditActions TO public;
END;
IF @OwnTransaction=1
BEGIN
    INSERT dbo.SchemaVersion(Version,Description) VALUES(2026100901,N'Fondations autorisations et journal métier');
    COMMIT;
END;
END TRY
BEGIN CATCH
    IF @OwnTransaction=1 AND XACT_STATE()<>0 ROLLBACK;
    THROW;
END CATCH;
