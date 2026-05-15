-- Migration stock: mouvements uniquement + reprise anciens stocks
-- Date: 2026-04-02

BEGIN TRY
    BEGIN TRAN;

    -- Table StockEntree
    IF OBJECT_ID('dbo.StockEntree','U') IS NULL
    BEGIN
        CREATE TABLE dbo.StockEntree (
            StockEntreeId INT IDENTITY(1,1) PRIMARY KEY,
            IdStock NVARCHAR(30) NOT NULL,
            ProduitId INT NOT NULL,
            QuantiteSaisie DECIMAL(18,2) NOT NULL,
            Unite NVARCHAR(50) NULL,
            QuantiteBase DECIMAL(18,2) NOT NULL,
            PrixAchat DECIMAL(18,2) NOT NULL,
            Devise NVARCHAR(10) NULL,
            Taux DECIMAL(18,6) NOT NULL DEFAULT 0,
            DateEntree DATETIME NOT NULL DEFAULT GETDATE(),
            FournisseurId INT NULL,
            CreePar INT NULL
        );
    END

    -- Table StockSortie
    IF OBJECT_ID('dbo.StockSortie','U') IS NULL
    BEGIN
        CREATE TABLE dbo.StockSortie (
            StockSortieId INT IDENTITY(1,1) PRIMARY KEY,
            ProduitId INT NOT NULL,
            QuantiteSaisie DECIMAL(18,2) NOT NULL,
            Unite NVARCHAR(50) NULL,
            QuantiteBase DECIMAL(18,2) NOT NULL,
            DateSortie DATETIME NOT NULL DEFAULT GETDATE(),
            Source NVARCHAR(50) NOT NULL,
            RefSource NVARCHAR(50) NULL,
            CreePar INT NULL
        );
    END

    -- Table StockPerte
    IF OBJECT_ID('dbo.StockPerte','U') IS NULL
    BEGIN
        CREATE TABLE dbo.StockPerte (
            StockPerteId INT IDENTITY(1,1) PRIMARY KEY,
            ProduitId INT NOT NULL,
            QuantiteSaisie DECIMAL(18,2) NOT NULL,
            Unite NVARCHAR(50) NULL,
            QuantiteBase DECIMAL(18,2) NOT NULL,
            TypePerte NVARCHAR(50) NULL,
            Motif NVARCHAR(200) NULL,
            DatePerte DATETIME NOT NULL DEFAULT GETDATE(),
            CreePar INT NULL
        );
    END

    -- Table StockInventaire
    IF OBJECT_ID('dbo.StockInventaire','U') IS NULL
    BEGIN
        CREATE TABLE dbo.StockInventaire (
            StockInventaireId INT IDENTITY(1,1) PRIMARY KEY,
            ProduitId INT NOT NULL,
            StockTheorique DECIMAL(18,2) NOT NULL,
            StockReel DECIMAL(18,2) NOT NULL,
            Ecart DECIMAL(18,2) NOT NULL,
            DateInventaire DATETIME NOT NULL DEFAULT GETDATE(),
            CreePar INT NULL,
            Observation NVARCHAR(200) NULL
        );
    END

    -- HistoriquePrixProduits: ajouter IdStock si absent
    IF COL_LENGTH('dbo.HistoriquePrixProduits','IdStock') IS NULL
    BEGIN
        ALTER TABLE dbo.HistoriquePrixProduits ADD IdStock NVARCHAR(30) NULL;
    END

    -- Vue stock courant
    IF OBJECT_ID('dbo.vStockProduit','V') IS NULL
    EXEC('CREATE VIEW dbo.vStockProduit AS SELECT 1 AS Dummy');

    EXEC('ALTER VIEW dbo.vStockProduit AS
        SELECT p.ProduitId,
               ISNULL(e.Entree,0) - ISNULL(s.Sortie,0) - ISNULL(pr.Perte,0) AS QuantiteStock
        FROM dbo.Produits p
        LEFT JOIN (SELECT ProduitId, SUM(QuantiteBase) AS Entree FROM dbo.StockEntree GROUP BY ProduitId) e ON e.ProduitId = p.ProduitId
        LEFT JOIN (SELECT ProduitId, SUM(QuantiteBase) AS Sortie FROM dbo.StockSortie GROUP BY ProduitId) s ON s.ProduitId = p.ProduitId
        LEFT JOIN (SELECT ProduitId, SUM(QuantiteBase) AS Perte FROM dbo.StockPerte GROUP BY ProduitId) pr ON pr.ProduitId = p.ProduitId
    ');

    -- Reprise anciens stocks
    IF COL_LENGTH('dbo.Produits','QuantiteStock') IS NOT NULL
    BEGIN
        INSERT INTO dbo.StockEntree (IdStock, ProduitId, QuantiteSaisie, Unite, QuantiteBase, PrixAchat, Devise, Taux, DateEntree, FournisseurId, CreePar)
        SELECT
            'INIT-' + CONVERT(VARCHAR(8), GETDATE(), 112) + '-' + RIGHT('000' + CAST(ROW_NUMBER() OVER (ORDER BY ProduitId) AS VARCHAR(3)), 3) AS IdStock,
            ProduitId,
            QuantiteStock AS QuantiteSaisie,
            UnitePrincipale,
            QuantiteStock AS QuantiteBase,
            CASE WHEN ISNULL(PrixAchat,0) > 0 THEN PrixAchat
                 WHEN ISNULL(PrixGros,0) > 0 THEN PrixGros
                 ELSE ISNULL(PrixDetail,0) END AS PrixAchat,
            'CDF',
            0,
            GETDATE(),
            NULL,
            NULL
        FROM dbo.Produits
        WHERE QuantiteStock > 0;

        -- Supprimer contraintes qui dependent de QuantiteStock
        DECLARE @sql NVARCHAR(MAX) = N'';

        SELECT @sql += 'ALTER TABLE dbo.Produits DROP CONSTRAINT [' + dc.name + '];' + CHAR(10)
        FROM sys.default_constraints dc
        JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID('dbo.Produits')
          AND c.name = 'QuantiteStock';

        SELECT @sql += 'ALTER TABLE dbo.Produits DROP CONSTRAINT [' + cc.name + '];' + CHAR(10)
        FROM sys.check_constraints cc
        WHERE cc.parent_object_id = OBJECT_ID('dbo.Produits')
          AND cc.definition LIKE '%QuantiteStock%';

        IF @sql <> '' EXEC sp_executesql @sql;

        -- Supprimer la colonne QuantiteStock
        ALTER TABLE dbo.Produits DROP COLUMN QuantiteStock;
    END

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
