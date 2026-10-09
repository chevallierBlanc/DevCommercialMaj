-- Diagnostic NON DESTRUCTIF. Aucune donnée métier ou trace n'est modifiée.
-- À exécuter après la migration 2026100901, avec le principal applicatif
-- puis avec le principal de migration pour comparer les droits effectifs.
SET NOCOUNT ON;
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaVersion WHERE Version=2026100901)
    THROW 51000, 'Migration 2026100901 absente.', 1;
DECLARE @Colonnes TABLE(TableName sysname, ColumnName sysname);
INSERT @Colonnes VALUES
(N'JournalAudit',N'Provenance'),(N'JournalAudit',N'Categorie'),
(N'JournalAudit',N'ReferenceDocument'),(N'JournalAudit',N'UtilisateurNom'),
(N'JournalAudit',N'RoleActif'),(N'JournalAudit',N'ModeActif'),
(N'JournalAudit',N'SessionId'),(N'JournalAudit',N'CorrelationId'),
(N'JournalAudit',N'Poste'),(N'JournalAudit',N'AnciennesValeurs'),
(N'JournalAudit',N'NouvellesValeurs'),(N'JournalAudit',N'Motif'),
(N'JournalAudit',N'Resultat'),(N'FacturesVente',N'VersionOperation'),
(N'UtilisateurSessions',N'ModeActif'),(N'Parametres',N'GestionModesActive'),
(N'Parametres',N'DelegationPrixActive');
SELECT c.*, COL_LENGTH(N'dbo.'+c.TableName,c.ColumnName) AS Longueur
FROM @Colonnes c;
IF EXISTS(SELECT 1 FROM @Colonnes WHERE COL_LENGTH(N'dbo.'+TableName,ColumnName) IS NULL)
    THROW 51000, 'Schéma des fondations incomplet.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.FacturesVente') AND name=N'VersionOperation' AND system_type_id=189)
    THROW 51000, 'VersionOperation doit être de type rowversion.', 1;
IF EXISTS(SELECT 1 FROM (VALUES(N'FACTURE_CREER'),(N'FACTURE_MODIFIER'),
    (N'FACTURE_ANNULER'),(N'ENCAISSEMENT_CREER'),(N'CAISSE_REGULARISER'),(N'AUDIT_CONSULTER')) a(Code)
    WHERE NOT EXISTS(SELECT 1 FROM dbo.InterfacesApplication i WHERE i.CodeInterface=a.Code))
    THROW 51000, 'Une permission d’action est absente.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.triggers WHERE object_id=OBJECT_ID(N'dbo.TR_JournalAudit_AppendOnly') AND is_disabled=0)
    THROW 51000, 'Protection append-only JournalAudit absente ou désactivée.', 1;
IF OBJECT_ID(N'dbo.AuditActions',N'U') IS NOT NULL
   AND NOT EXISTS(SELECT 1 FROM sys.triggers WHERE object_id=OBJECT_ID(N'dbo.TR_AuditActions_AppendOnly') AND is_disabled=0)
    THROW 51000, 'Protection des anciennes traces absente ou désactivée.', 1;
IF EXISTS(SELECT 1 FROM (VALUES(N'JournalAudit'),(N'AuditActions')) t(Nom)
    WHERE OBJECT_ID(N'dbo.'+t.Nom,N'U') IS NOT NULL
      AND (SELECT COUNT(*) FROM sys.database_permissions p
           WHERE p.class=1 AND p.major_id=OBJECT_ID(N'dbo.'+t.Nom)
           AND p.grantee_principal_id=DATABASE_PRINCIPAL_ID(N'public')
           AND p.state=N'D' AND p.permission_name IN(N'UPDATE',N'DELETE'))<>2)
    THROW 51000, 'DENY UPDATE/DELETE public incomplet.', 1;
-- Les droits de contrôle/DDL permettent de contourner la protection.
-- Ce diagnostic ne prétend pas protéger contre dbo, db_owner ou sysadmin.
SELECT DB_NAME() AS BaseCourante, USER_NAME() AS Principal,
       IS_SRVROLEMEMBER(N'sysadmin') AS Sysadmin,
       IS_MEMBER(N'db_owner') AS DbOwner,
       HAS_PERMS_BY_NAME(DB_NAME(),N'DATABASE',N'CONTROL') AS ControleBase,
       HAS_PERMS_BY_NAME(N'dbo.JournalAudit',N'OBJECT',N'ALTER') AS AlterAudit,
       HAS_PERMS_BY_NAME(N'dbo.JournalAudit',N'OBJECT',N'UPDATE') AS UpdateAudit,
       HAS_PERMS_BY_NAME(N'dbo.JournalAudit',N'OBJECT',N'DELETE') AS DeleteAudit;
SELECT r.NomRole,i.CodeInterface,i.EstActif
FROM dbo.RoleInterfaces ri JOIN dbo.Roles r ON r.RoleId=ri.RoleId
JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId
ORDER BY r.NomRole,i.CodeInterface;
SELECT COUNT_BIG(*) AS TracesJournalConservees FROM dbo.JournalAudit;
IF OBJECT_ID(N'dbo.AuditActions',N'U') IS NOT NULL
    SELECT COUNT_BIG(*) AS TracesAnciennesConservees FROM dbo.AuditActions;
-- SQL dynamique : les nouvelles colonnes ne sont compilées qu'après leur contrôle.
EXEC(N'SELECT COALESCE(Provenance,N''JournalAudit'') AS Provenance,
       COALESCE(Resultat,N''HISTORIQUE'') AS Resultat,COUNT_BIG(*) AS Nombre
       FROM dbo.JournalAudit GROUP BY Provenance,Resultat;
       SELECT GestionModesActive,DelegationPrixActive FROM dbo.Parametres;');
PRINT N'VALIDATION_AUDIT_METIER_PHASE1_SCHEMA_OK';
PRINT N'Le contrôle du schéma ne remplace pas les tests transactionnels, de concurrence et les contrôles des privilèges du principal applicatif.';
