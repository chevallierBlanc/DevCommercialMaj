-- Extension additive : les anciens paiements gardent leurs valeurs intactes.
SET XACT_ABORT ON;
DECLARE @TransactionLocale bit = CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
IF @TransactionLocale=1 BEGIN TRANSACTION;
BEGIN TRY
    IF OBJECT_ID(N'dbo.Paiements',N'U') IS NULL
        THROW 51000, 'La table Paiements doit exister avant cette migration.', 1;
    IF COL_LENGTH(N'dbo.Paiements',N'MontantRecuOrigine') IS NULL
        ALTER TABLE dbo.Paiements ADD MontantRecuOrigine decimal(28,8) NULL;
    IF COL_LENGTH(N'dbo.Paiements',N'TauxConversionApplique') IS NULL
        ALTER TABLE dbo.Paiements ADD TauxConversionApplique decimal(28,8) NULL;
    IF COL_LENGTH(N'dbo.Paiements',N'DeviseMontants') IS NULL
        ALTER TABLE dbo.Paiements ADD DeviseMontants nvarchar(10) NULL;
    -- Aucun backfill : on ne connait pas le taux historique depuis le taux actuel.
    IF @TransactionLocale=1 AND NOT EXISTS(SELECT 1 FROM dbo.SchemaVersion WHERE Version=2026100902)
        INSERT dbo.SchemaVersion(Version,Description) VALUES(2026100902,N'Tracabilite des montants de paiement');
    IF @TransactionLocale=1 COMMIT;
END TRY
BEGIN CATCH
    IF @TransactionLocale=1 AND @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
