/*
    Validation non destructive de la réinitialisation commerciale ERPCommercial.
    Ce script NE SUPPRIME AUCUNE DONNÉE.

    Utilisation :
    - Avant reset : laisser @VerifierApresReset = 0 pour cartographier les volumes.
    - Après reset sur une base de TEST : passer @VerifierApresReset = 1 pour vérifier
      que les transactions sont à zéro et que les référentiels sont conservés.
*/

SET NOCOUNT ON;

DECLARE @VerifierApresReset BIT = 0;

DECLARE @Tables TABLE
(
    NomTable SYSNAME NOT NULL,
    RoleMetier NVARCHAR(100) NOT NULL,
    Conserver BIT NOT NULL,
    Raison NVARCHAR(300) NOT NULL,
    OrdreSuppression INT NOT NULL
);

INSERT INTO @Tables (NomTable, RoleMetier, Conserver, Raison, OrdreSuppression)
VALUES
    (N'RegularisationsEcartCaisse', N'Caisse', 0, N'Régularisations rattachées aux clôtures caisse', 10),
    (N'HistoriqueStatutClotureCaisse', N'Caisse', 0, N'Historique opérationnel des statuts de clôture caisse', 20),
    (N'InventaireLignes', N'Inventaire', 0, N'Lignes enfants des inventaires physiques', 30),
    (N'BonApprovisionnementLignes', N'Approvisionnement', 0, N'Lignes enfants des bons d''approvisionnement', 40),
    (N'LignesFactureVente', N'Ventes', 0, N'Lignes enfants des factures de vente', 50),
    (N'Paiements', N'Paiements', 0, N'Paiements rattachés aux factures', 60),
    (N'InitialisationVenteLignes', N'Initialisation ventes', 0, N'Lignes de ventes historiques initialisées', 70),
    (N'StockInitialTechniqueLignes', N'Stock initial', 0, N'Lignes de stock initial technique', 80),
    (N'CloturesCaisse', N'Caisse', 0, N'Clôtures physiques de caisse', 90),
    (N'Inventaires', N'Inventaire', 0, N'Sessions d''inventaire', 100),
    (N'BonsApprovisionnement', N'Approvisionnement', 0, N'Bons d''approvisionnement', 110),
    (N'FacturesVente', N'Ventes', 0, N'Factures commerciales', 120),
    (N'InitialisationVenteSessions', N'Initialisation ventes', 0, N'Sessions d''initialisation ventes', 130),
    (N'StockInitialTechniqueSessions', N'Stock initial', 0, N'Sessions de stock initial technique', 140),
    (N'MouvementsStock', N'Stock', 0, N'Journal des mouvements physiques', 150),
    (N'StockEntree', N'Stock', 0, N'Entrées alimentant vStockProduit', 160),
    (N'StockSortie', N'Stock', 0, N'Sorties alimentant vStockProduit', 170),
    (N'StockPerte', N'Stock', 0, N'Pertes alimentant vStockProduit', 180),
    (N'StockInventaire', N'Inventaire', 0, N'Ancien inventaire transactionnel', 190),
    (N'Depenses', N'Dépenses', 0, N'Dépenses d''exploitation', 200),
    (N'Banque', N'Caisse/Banque', 0, N'Mouvements financiers opérationnels', 210),
    (N'CloturesJournalieres', N'Caisse', 0, N'Clôtures journalières', 220),
    (N'FacturesFournisseurs', N'Achats', 0, N'Factures fournisseur', 230),
    (N'StockSortieNonSynchronise', N'Synchronisation', 0, N'File transactionnelle sorties hors ligne', 240),
    (N'DepensesNonSynchronisees', N'Synchronisation', 0, N'File transactionnelle dépenses hors ligne', 250),
    (N'Notifications', N'Opérationnel', 0, N'Notifications liées à l''ancienne exploitation', 260),
    (N'BusinessSequences', N'Numérotation', 0, N'Séquences métier visibles', 270),
    (N'FactureSequence', N'Numérotation', 0, N'Séquence factures', 280),
    (N'StockSequence', N'Numérotation', 0, N'Séquences stock', 290),
    (N'MouvementSequence', N'Numérotation', 0, N'Séquence mouvements stock', 300),
    (N'BonApprovisionnementSequence', N'Numérotation', 0, N'Séquence bons d''approvisionnement', 310),
    (N'Produits', N'Référentiel produit', 1, N'Produits/prix/configuration conservés', 0),
    (N'CategoriesProduits', N'Référentiel produit', 1, N'Catégories produit conservées', 0),
    (N'UnitesMesure', N'Conditionnements', 1, N'Unités conservées', 0),
    (N'ProduitConditionnements', N'Conditionnements', 1, N'Hiérarchie dynamique conservée', 0),
    (N'TypesVenteProduit', N'Types de vente', 1, N'Types personnalisés conservés', 0),
    (N'Utilisateurs', N'Sécurité', 1, N'Comptes conservés', 0),
    (N'Roles', N'Sécurité', 1, N'Rôles conservés', 0),
    (N'UtilisateurRoles', N'Sécurité', 1, N'Multi-rôles conservés', 0),
    (N'Parametres', N'Configuration', 1, N'Paramètres entreprise conservés', 0),
    (N'Magasins', N'Configuration', 1, N'Magasins conservés', 0),
    (N'JournalAudit', N'Audit', 1, N'Journal audit conservé', 0),
    (N'AuditActions', N'Audit', 1, N'Actions admin conservées', 0),
    (N'JournalFraude', N'Audit', 1, N'Journal fraude conservé', 0);

DECLARE @Resultats TABLE
(
    ActionAttendue NVARCHAR(20) NOT NULL,
    NomTable SYSNAME NOT NULL,
    RoleMetier NVARCHAR(100) NOT NULL,
    NombreLignes BIGINT NULL,
    Raison NVARCHAR(300) NOT NULL
);

DECLARE @NomTable SYSNAME;
DECLARE @RoleMetier NVARCHAR(100);
DECLARE @Conserver BIT;
DECLARE @Raison NVARCHAR(300);
DECLARE @Sql NVARCHAR(MAX);
DECLARE @Count BIGINT;

DECLARE c CURSOR LOCAL FAST_FORWARD FOR
    SELECT NomTable, RoleMetier, Conserver, Raison
    FROM @Tables
    ORDER BY Conserver, OrdreSuppression, NomTable;

OPEN c;
FETCH NEXT FROM c INTO @NomTable, @RoleMetier, @Conserver, @Raison;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Count = NULL;
    IF OBJECT_ID(N'dbo.' + @NomTable, N'U') IS NOT NULL
    BEGIN
        SET @Sql = N'SELECT @C = COUNT_BIG(*) FROM dbo.' + QUOTENAME(@NomTable) + N';';
        EXEC sp_executesql @Sql, N'@C BIGINT OUTPUT', @C = @Count OUTPUT;
    END

    INSERT INTO @Resultats (ActionAttendue, NomTable, RoleMetier, NombreLignes, Raison)
    VALUES (CASE WHEN @Conserver = 1 THEN N'CONSERVER' ELSE N'VIDER' END, @NomTable, @RoleMetier, @Count, @Raison);

    FETCH NEXT FROM c INTO @NomTable, @RoleMetier, @Conserver, @Raison;
END
CLOSE c;
DEALLOCATE c;

SELECT ActionAttendue, NomTable, RoleMetier, NombreLignes, Raison
FROM @Resultats
ORDER BY CASE WHEN ActionAttendue = N'VIDER' THEN 0 ELSE 1 END, NomTable;

SELECT
    tbl.name AS TableTransactionnelle,
    sch.name + N'.' + trg.name AS TriggerDml,
    CASE WHEN trg.is_disabled = 1 THEN N'DESACTIVE' ELSE N'ACTIF' END AS EtatTrigger
FROM sys.triggers trg
INNER JOIN sys.tables tbl ON tbl.object_id = trg.parent_id
INNER JOIN sys.schemas sch ON sch.schema_id = trg.schema_id
INNER JOIN @Tables t ON t.NomTable = tbl.name AND t.Conserver = 0
WHERE trg.parent_class = 1
  AND trg.is_ms_shipped = 0
  AND OBJECT_SCHEMA_NAME(tbl.object_id) = N'dbo'
ORDER BY tbl.name, trg.name;

IF OBJECT_ID(N'dbo.vStockProduit', N'V') IS NOT NULL
BEGIN
    SELECT COUNT(*) AS ProduitsAvecStockNonZero
    FROM dbo.vStockProduit
    WHERE ABS(CAST(ISNULL(QuantiteStock, 0) AS DECIMAL(18,4))) > 0.0001;
END

IF @VerifierApresReset = 1
BEGIN
    IF EXISTS (SELECT 1 FROM @Resultats WHERE ActionAttendue = N'VIDER' AND ISNULL(NombreLignes, 0) <> 0)
        RAISERROR(N'Validation échouée : une table transactionnelle contient encore des lignes.', 16, 1);

    IF OBJECT_ID(N'dbo.vStockProduit', N'V') IS NOT NULL
       AND EXISTS (SELECT 1 FROM dbo.vStockProduit WHERE ABS(CAST(ISNULL(QuantiteStock, 0) AS DECIMAL(18,4))) > 0.0001)
        RAISERROR(N'Validation échouée : stock non nul après reset.', 16, 1);

    IF OBJECT_ID(N'dbo.Produits', N'U') IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Produits)
        RAISERROR(N'Validation échouée : Produits absent ou vide.', 16, 1);

    IF OBJECT_ID(N'dbo.Utilisateurs', N'U') IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Utilisateurs)
        RAISERROR(N'Validation échouée : Utilisateurs absent ou vide.', 16, 1);

    IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Roles)
        RAISERROR(N'Validation échouée : Roles absent ou vide.', 16, 1);

    IF OBJECT_ID(N'dbo.UnitesMesure', N'U') IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.UnitesMesure)
        RAISERROR(N'Validation échouée : UnitesMesure absent ou vide.', 16, 1);

    IF OBJECT_ID(N'dbo.ProduitConditionnements', N'U') IS NULL
        RAISERROR(N'Validation échouée : table ProduitConditionnements absente.', 16, 1);

    IF OBJECT_ID(N'dbo.TypesVenteProduit', N'U') IS NULL
        RAISERROR(N'Validation échouée : table TypesVenteProduit absente.', 16, 1);

    IF OBJECT_ID(N'dbo.Parametres', N'U') IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.Parametres)
        RAISERROR(N'Validation échouée : Parametres absent ou vide.', 16, 1);

    IF OBJECT_ID(N'dbo.AuditActions', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.AuditActions WHERE [Action] = N'COMMERCIAL_RESET_COMPLETED')
        RAISERROR(N'Validation échouée : audit COMMERCIAL_RESET_COMPLETED introuvable.', 16, 1);

    IF EXISTS (
        SELECT 1
        FROM sys.triggers trg
        INNER JOIN sys.tables tbl ON tbl.object_id = trg.parent_id
        INNER JOIN @Tables t ON t.NomTable = tbl.name AND t.Conserver = 0
        WHERE trg.parent_class = 1
          AND trg.is_ms_shipped = 0
          AND OBJECT_SCHEMA_NAME(tbl.object_id) = N'dbo'
          AND trg.is_disabled = 1
    )
        RAISERROR(N'Validation échouée : au moins un trigger transactionnel est resté désactivé.', 16, 1);

    PRINT N'VALIDATION_COMMERCIAL_RESET_OK';
END
