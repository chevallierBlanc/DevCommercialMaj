-- Les droits de mode sont des permissions du RBAC existant, jamais attribuees
-- automatiquement. Les installations conservees restent en parcours classique.
SET XACT_ABORT ON;
DECLARE @Own bit=CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
IF @Own=1 BEGIN TRANSACTION;
BEGIN TRY
    IF COL_LENGTH(N'dbo.Parametres',N'ModeFacturationAutorise') IS NULL ALTER TABLE dbo.Parametres ADD ModeFacturationAutorise bit NOT NULL DEFAULT(0);
    IF COL_LENGTH(N'dbo.Parametres',N'ModeCaisseAutorise') IS NULL ALTER TABLE dbo.Parametres ADD ModeCaisseAutorise bit NOT NULL DEFAULT(0);
    IF COL_LENGTH(N'dbo.Parametres',N'ModeCombineAutorise') IS NULL ALTER TABLE dbo.Parametres ADD ModeCombineAutorise bit NOT NULL DEFAULT(0);
    INSERT dbo.InterfacesApplication(CodeInterface,Libelle,EstTechnique,EstActif)
    SELECT v.Code,v.Libelle,0,1 FROM (VALUES
        (N'MODE_FACTURATION',N'Mode : facturation'),
        (N'MODE_CAISSE',N'Mode : caisse'),
        (N'MODE_COMBINE',N'Mode : facturation et caisse'),
        (N'PARAMETRES_SECURITE',N'Configurer les modes de travail')) v(Code,Libelle)
    WHERE NOT EXISTS(SELECT 1 FROM dbo.InterfacesApplication i WHERE i.CodeInterface=v.Code);
    IF @Own=1
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM dbo.SchemaVersion WHERE Version=2026100903)
            INSERT dbo.SchemaVersion(Version,Description) VALUES(2026100903,N'Modes de travail explicites');
        COMMIT;
    END;
END TRY
BEGIN CATCH
    IF @Own=1 AND @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
