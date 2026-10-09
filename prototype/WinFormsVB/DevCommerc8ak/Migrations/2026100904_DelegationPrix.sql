SET XACT_ABORT ON;
DECLARE @Own bit=CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
IF @Own=1 BEGIN TRANSACTION;
BEGIN TRY
    IF COL_LENGTH(N'dbo.Parametres',N'PrixDemandesActives') IS NULL ALTER TABLE dbo.Parametres ADD PrixDemandesActives bit NOT NULL DEFAULT(0);
    IF COL_LENGTH(N'dbo.Parametres',N'PrixModificationImmediate') IS NULL ALTER TABLE dbo.Parametres ADD PrixModificationImmediate bit NOT NULL DEFAULT(0);
    IF COL_LENGTH(N'dbo.Parametres',N'PrixSousCoutAutorise') IS NULL ALTER TABLE dbo.Parametres ADD PrixSousCoutAutorise bit NOT NULL DEFAULT(0);
    IF COL_LENGTH(N'dbo.Parametres',N'PrixVariationMaxPourcent') IS NULL ALTER TABLE dbo.Parametres ADD PrixVariationMaxPourcent decimal(9,4) NOT NULL DEFAULT(0);
    IF COL_LENGTH(N'dbo.Parametres',N'PrixMargeMinimumPourcent') IS NULL ALTER TABLE dbo.Parametres ADD PrixMargeMinimumPourcent decimal(9,4) NOT NULL DEFAULT(0);
    IF COL_LENGTH(N'dbo.LignesFactureVente',N'MotifPrixException') IS NULL ALTER TABLE dbo.LignesFactureVente ADD MotifPrixException nvarchar(1000) NULL;
    IF OBJECT_ID(N'dbo.DemandesModificationPrix',N'U') IS NULL
        CREATE TABLE dbo.DemandesModificationPrix(
            DemandeId int IDENTITY PRIMARY KEY,ProduitId int NOT NULL REFERENCES dbo.Produits(ProduitId),
            TypeVente nvarchar(100) NOT NULL,AncienPrix decimal(18,2) NOT NULL,NouveauPrix decimal(18,2) NOT NULL,
            EmpreinteTarif nvarchar(64) NOT NULL,Motif nvarchar(1000) NOT NULL,
            Etat nvarchar(20) NOT NULL DEFAULT(N'EN_ATTENTE') CHECK(Etat IN(N'EN_ATTENTE',N'APPROUVEE',N'REFUSEE',N'ANNULEE',N'EXPIREE')),
            DemandePar int NOT NULL REFERENCES dbo.Utilisateurs(UtilisateurId),DemandeLe datetime2 NOT NULL DEFAULT(SYSUTCDATETIME()),
            ExpireLe datetime2 NOT NULL DEFAULT(DATEADD(DAY,1,SYSUTCDATETIME())),
            DecisionPar int NULL REFERENCES dbo.Utilisateurs(UtilisateurId),DecisionLe datetime2 NULL,MotifDecision nvarchar(1000) NULL);
    INSERT dbo.InterfacesApplication(CodeInterface,Libelle,EstTechnique,EstActif)
    SELECT v.Code,v.Libelle,0,1 FROM(VALUES
        (N'PRIX_CONSULTER',N'Prix : consulter les tarifs et demandes'),
        (N'PRIX_DEMANDER',N'Prix : demander une modification officielle'),
        (N'PRIX_TARIF_MODIFIER',N'Prix : modifier le tarif officiel par délégation'),
        (N'PRIX_FACTURE_EXCEPTION',N'Prix : exception sur une facture uniquement'),
        (N'PRIX_APPROUVER',N'Prix : approuver une modification'),
        (N'PRIX_REFUSER',N'Prix : refuser une modification'),
        (N'PRIX_SOUS_COUT',N'Prix : autoriser une exception sous le coût'),
        (N'PARAMETRES_PRIX',N'Configurer la délégation des prix')) v(Code,Libelle)
    WHERE NOT EXISTS(SELECT 1 FROM dbo.InterfacesApplication i WHERE i.CodeInterface=v.Code);
    -- Aucun grant : l'administrateur attribue explicitement ces droits.
    IF @Own=1
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM dbo.SchemaVersion WHERE Version=2026100904)
            INSERT dbo.SchemaVersion(Version,Description) VALUES(2026100904,N'Délégation de tarifs et exceptions facture');
        COMMIT;
    END;
END TRY
BEGIN CATCH
    IF @Own=1 AND @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
