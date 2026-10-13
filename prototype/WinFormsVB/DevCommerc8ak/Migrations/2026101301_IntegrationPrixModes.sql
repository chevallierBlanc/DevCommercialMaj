SET XACT_ABORT ON;
DECLARE @Own bit=CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
IF @Own=1 BEGIN TRANSACTION;
BEGIN TRY
    IF OBJECT_ID(N'dbo.AppDataVersions',N'U') IS NULL
        CREATE TABLE dbo.AppDataVersions(Domaine nvarchar(50) NOT NULL PRIMARY KEY,
            VersionCourante bigint NOT NULL DEFAULT(0),ModifieLe datetime2 NOT NULL DEFAULT(SYSDATETIME()),
            ModifiePar nvarchar(80) NULL,Poste nvarchar(100) NULL);
    IF COL_LENGTH(N'dbo.Roles',N'VersionAutorisations') IS NULL AND EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Roles') AND system_type_id=189)
        THROW 51000, 'Roles possede une autre rowversion : cartographier cette colonne avant migration. Aucun remplacement automatique.', 1;
    IF COL_LENGTH(N'dbo.Roles',N'VersionAutorisations') IS NULL
        ALTER TABLE dbo.Roles ADD VersionAutorisations rowversion NOT NULL;
    IF EXISTS(SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Roles') AND name=N'VersionAutorisations' AND system_type_id<>189)
        THROW 51000, 'VersionAutorisations doit etre une rowversion SQL Server.', 1;
    IF COL_LENGTH(N'dbo.Parametres',N'PrixDemandeDureeHeures') IS NULL
        ALTER TABLE dbo.Parametres ADD PrixDemandeDureeHeures int NOT NULL CONSTRAINT DF_Parametres_PrixDuree DEFAULT(24) CONSTRAINT CK_Parametres_PrixDuree CHECK(PrixDemandeDureeHeures BETWEEN 1 AND 168);
    IF COL_LENGTH(N'dbo.DemandesModificationPrix',N'Perimetre') IS NULL
        ALTER TABLE dbo.DemandesModificationPrix ADD Perimetre nvarchar(16) NOT NULL CONSTRAINT DF_DemandesPrix_Perimetre DEFAULT(N'TARIF');
    IF COL_LENGTH(N'dbo.DemandesModificationPrix',N'CoutReferenceDemande') IS NULL
        ALTER TABLE dbo.DemandesModificationPrix ADD CoutReferenceDemande decimal(28,8) NULL;
    IF COL_LENGTH(N'dbo.DemandesModificationPrix',N'SousTotalContexte') IS NULL
        ALTER TABLE dbo.DemandesModificationPrix ADD SousTotalContexte decimal(28,8) NULL;
    IF COL_LENGTH(N'dbo.DemandesModificationPrix',N'RemiseContexte') IS NULL
        ALTER TABLE dbo.DemandesModificationPrix ADD RemiseContexte decimal(28,8) NULL;
    IF COL_LENGTH(N'dbo.DemandesModificationPrix',N'ContexteFacture') IS NULL
        ALTER TABLE dbo.DemandesModificationPrix ADD ContexteFacture uniqueidentifier NULL;
    IF COL_LENGTH(N'dbo.DemandesModificationPrix',N'QuantiteContexte') IS NULL
        ALTER TABLE dbo.DemandesModificationPrix ADD QuantiteContexte decimal(28,8) NULL;
    IF COL_LENGTH(N'dbo.DemandesModificationPrix',N'UtiliseeLe') IS NULL
        ALTER TABLE dbo.DemandesModificationPrix ADD UtiliseeLe datetime2 NULL;
    IF COL_LENGTH(N'dbo.DemandesModificationPrix',N'FactureUtilisationId') IS NULL
        ALTER TABLE dbo.DemandesModificationPrix ADD FactureUtilisationId int NULL;
    IF COL_LENGTH(N'dbo.LignesFactureVente',N'DemandePrixId') IS NULL
        ALTER TABLE dbo.LignesFactureVente ADD DemandePrixId int NULL;
    IF COL_LENGTH(N'dbo.LignesFactureVente',N'ContextePrix') IS NULL
        ALTER TABLE dbo.LignesFactureVente ADD ContextePrix uniqueidentifier NULL;
    IF COL_LENGTH(N'dbo.LignesFactureVente',N'TypeTarifId') IS NULL
        ALTER TABLE dbo.LignesFactureVente ADD TypeTarifId int NULL;
    IF COL_LENGTH(N'dbo.DemandesModificationPrix',N'TypeTarifId') IS NULL
        ALTER TABLE dbo.DemandesModificationPrix ADD TypeTarifId int NULL;
    -- Resolution dans un nouveau batch apres les ALTER additifs, pour les
    -- anciennes bases qui ne possedent pas encore ces colonnes.
    EXEC(N'IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N''dbo.DemandesModificationPrix'') AND name=N''CK_DemandesPrix_Perimetre'') ALTER TABLE dbo.DemandesModificationPrix WITH CHECK ADD CONSTRAINT CK_DemandesPrix_Perimetre CHECK(Perimetre IN(N''TARIF'',N''FACTURE'') AND (Perimetre=N''TARIF'' OR (ContexteFacture IS NOT NULL AND QuantiteContexte IS NOT NULL AND QuantiteContexte>0)));');
    EXEC(N'IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N''dbo.DemandesModificationPrix'') AND name=N''CK_DemandesPrix_Remise'') ALTER TABLE dbo.DemandesModificationPrix WITH CHECK ADD CONSTRAINT CK_DemandesPrix_Remise CHECK(Perimetre=N''TARIF'' OR (SousTotalContexte IS NOT NULL AND SousTotalContexte>0 AND RemiseContexte IS NOT NULL AND RemiseContexte>=0 AND RemiseContexte<=SousTotalContexte));');
    EXEC(N'IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N''dbo.DemandesModificationPrix'') AND name=N''IX_DemandesPrix_Contexte'') CREATE INDEX IX_DemandesPrix_Contexte ON dbo.DemandesModificationPrix(ContexteFacture,DemandePar,DemandeId DESC) INCLUDE(ProduitId,TypeVente,QuantiteContexte,Etat,ExpireLe,UtiliseeLe) WHERE ContexteFacture IS NOT NULL;');
    EXEC(N'IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N''dbo.LignesFactureVente'') AND name=N''UX_LignesFacture_ContextePrix'') CREATE UNIQUE INDEX UX_LignesFacture_ContextePrix ON dbo.LignesFactureVente(ContextePrix) WHERE ContextePrix IS NOT NULL;');
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.DemandesModificationPrix') AND name=N'IX_DemandesPrix_EtatDate')
        CREATE INDEX IX_DemandesPrix_EtatDate ON dbo.DemandesModificationPrix(Etat,DemandeId DESC) INCLUDE(ProduitId,DemandePar,ExpireLe);
    IF NOT EXISTS(SELECT 1 FROM dbo.InterfacesApplication WHERE CodeInterface=N'PRIX_DEMANDER_FACTURE')
        INSERT dbo.InterfacesApplication(CodeInterface,Libelle,EstTechnique,EstActif) VALUES(N'PRIX_DEMANDER_FACTURE',N'Prix : demander une exception pour une facture',0,1);
    -- Pas d'attribution automatique. Les anciens dossiers restent TARIF et
    -- aucune ligne de facture historique n'est rattachee retroactivement.
    IF @Own=1
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM dbo.SchemaVersion WHERE Version=2026101301)
            INSERT dbo.SchemaVersion(Version,Description) VALUES(2026101301,N'Integration prix facture et modes operationnels');
        COMMIT;
    END;
END TRY
BEGIN CATCH
    IF @Own=1 AND @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
