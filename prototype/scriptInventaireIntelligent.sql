USE [CommercialMagDB]
GO

IF OBJECT_ID(N'dbo.Inventaires', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Inventaires
    (
        InventaireId INT IDENTITY(1,1) NOT NULL,
        ReferenceInventaire NVARCHAR(50) NOT NULL,
        DateCreation DATETIME2(7) NOT NULL CONSTRAINT DF_Inventaires_DateCreation DEFAULT (SYSDATETIME()),
        DateValidation DATETIME2(7) NULL,
        CreePar INT NOT NULL,
        ValidePar INT NULL,
        Statut NVARCHAR(30) NOT NULL CONSTRAINT DF_Inventaires_Statut DEFAULT (N'EN_COURS'),
        Observation NVARCHAR(1000) NULL,
        CONSTRAINT PK_Inventaires PRIMARY KEY CLUSTERED (InventaireId ASC)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Inventaires_ReferenceInventaire'
      AND object_id = OBJECT_ID(N'dbo.Inventaires')
)
BEGIN
    CREATE UNIQUE INDEX UX_Inventaires_ReferenceInventaire
    ON dbo.Inventaires (ReferenceInventaire);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Inventaires_Statut_DateCreation'
      AND object_id = OBJECT_ID(N'dbo.Inventaires')
)
BEGIN
    CREATE INDEX IX_Inventaires_Statut_DateCreation
    ON dbo.Inventaires (Statut, DateCreation DESC);
END
GO

IF OBJECT_ID(N'dbo.InventaireLignes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventaireLignes
    (
        LigneInventaireId INT IDENTITY(1,1) NOT NULL,
        InventaireId INT NOT NULL,
        ProduitId INT NOT NULL,
        StockTheorique DECIMAL(18,2) NOT NULL CONSTRAINT DF_InventaireLignes_StockTheorique DEFAULT ((0)),
        StockPhysique DECIMAL(18,2) NULL,
        Ecart DECIMAL(18,2) NULL,
        Statut NVARCHAR(30) NOT NULL CONSTRAINT DF_InventaireLignes_Statut DEFAULT (N'NON_COMPTE'),
        Motif NVARCHAR(500) NULL,
        DateComptage DATETIME2(7) NULL,
        CreeLe DATETIME2(7) NOT NULL CONSTRAINT DF_InventaireLignes_CreeLe DEFAULT (SYSDATETIME()),
        ModifieLe DATETIME2(7) NULL,
        CONSTRAINT PK_InventaireLignes PRIMARY KEY CLUSTERED (LigneInventaireId ASC)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_InventaireLignes_Inventaire_Produit'
      AND object_id = OBJECT_ID(N'dbo.InventaireLignes')
)
BEGIN
    CREATE UNIQUE INDEX IX_InventaireLignes_Inventaire_Produit
    ON dbo.InventaireLignes (InventaireId, ProduitId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_InventaireLignes_Statut'
      AND object_id = OBJECT_ID(N'dbo.InventaireLignes')
)
BEGIN
    CREATE INDEX IX_InventaireLignes_Statut
    ON dbo.InventaireLignes (InventaireId, Statut);
END
GO

ALTER TABLE dbo.Inventaires WITH CHECK ADD CONSTRAINT FK_Inventaires_Utilisateurs_CreePar
FOREIGN KEY (CreePar) REFERENCES dbo.Utilisateurs (UtilisateurId);
GO

ALTER TABLE dbo.Inventaires CHECK CONSTRAINT FK_Inventaires_Utilisateurs_CreePar;
GO

ALTER TABLE dbo.Inventaires WITH CHECK ADD CONSTRAINT FK_Inventaires_Utilisateurs_ValidePar
FOREIGN KEY (ValidePar) REFERENCES dbo.Utilisateurs (UtilisateurId);
GO

ALTER TABLE dbo.Inventaires CHECK CONSTRAINT FK_Inventaires_Utilisateurs_ValidePar;
GO

ALTER TABLE dbo.InventaireLignes WITH CHECK ADD CONSTRAINT FK_InventaireLignes_Inventaires
FOREIGN KEY (InventaireId) REFERENCES dbo.Inventaires (InventaireId)
ON DELETE CASCADE;
GO

ALTER TABLE dbo.InventaireLignes CHECK CONSTRAINT FK_InventaireLignes_Inventaires;
GO

ALTER TABLE dbo.InventaireLignes WITH CHECK ADD CONSTRAINT FK_InventaireLignes_Produits
FOREIGN KEY (ProduitId) REFERENCES dbo.Produits (ProduitId);
GO

ALTER TABLE dbo.InventaireLignes CHECK CONSTRAINT FK_InventaireLignes_Produits;
GO
