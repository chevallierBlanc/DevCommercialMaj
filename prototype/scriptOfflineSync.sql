USE [master]
GO

IF DB_ID(N'CommercialMagDB') IS NULL
BEGIN
    RAISERROR('La base CommercialMagDB doit exister avant l''installation de la synchronisation offline.', 16, 1);
    RETURN;
END
GO

USE [CommercialMagDB]
GO

IF OBJECT_ID(N'dbo.StockSortieNonSynchronise', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockSortieNonSynchronise
    (
        Id INT IDENTITY(1,1) NOT NULL,
        JsonData NVARCHAR(MAX) NOT NULL,
        DateCreation DATETIME2(7) NOT NULL CONSTRAINT DF_StockSortieNonSynchronise_DateCreation DEFAULT (sysutcdatetime()),
        NombreTentatives INT NOT NULL CONSTRAINT DF_StockSortieNonSynchronise_NombreTentatives DEFAULT ((0)),
        DerniereTentative DATETIME2(7) NULL,
        StatutSync NVARCHAR(30) NOT NULL CONSTRAINT DF_StockSortieNonSynchronise_StatutSync DEFAULT (N'EN_ATTENTE'),
        MessageErreur NVARCHAR(1000) NULL,
        CONSTRAINT PK_StockSortieNonSynchronise PRIMARY KEY CLUSTERED (Id ASC)
    );
END
GO

IF OBJECT_ID(N'dbo.DepensesNonSynchronisees', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DepensesNonSynchronisees
    (
        Id INT IDENTITY(1,1) NOT NULL,
        JsonData NVARCHAR(MAX) NOT NULL,
        DateCreation DATETIME2(7) NOT NULL CONSTRAINT DF_DepensesNonSynchronisees_DateCreation DEFAULT (sysutcdatetime()),
        NombreTentatives INT NOT NULL CONSTRAINT DF_DepensesNonSynchronisees_NombreTentatives DEFAULT ((0)),
        DerniereTentative DATETIME2(7) NULL,
        StatutSync NVARCHAR(30) NOT NULL CONSTRAINT DF_DepensesNonSynchronisees_StatutSync DEFAULT (N'EN_ATTENTE'),
        MessageErreur NVARCHAR(1000) NULL,
        CONSTRAINT PK_DepensesNonSynchronisees PRIMARY KEY CLUSTERED (Id ASC)
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_StockSortieNonSynchronise_StatutSync'
      AND object_id = OBJECT_ID(N'dbo.StockSortieNonSynchronise')
)
BEGIN
    CREATE INDEX IX_StockSortieNonSynchronise_StatutSync
    ON dbo.StockSortieNonSynchronise (StatutSync, DateCreation);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_DepensesNonSynchronisees_StatutSync'
      AND object_id = OBJECT_ID(N'dbo.DepensesNonSynchronisees')
)
BEGIN
    CREATE INDEX IX_DepensesNonSynchronisees_StatutSync
    ON dbo.DepensesNonSynchronisees (StatutSync, DateCreation);
END
GO
