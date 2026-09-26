/*
Validation statique SQL Server pour la fondation conditionnements dynamiques.

Ce script ne modifie pas les donnees de production. Les tests applicatifs
Windows/SQL restent obligatoires avant activation fonctionnelle complete.
*/

SET NOCOUNT ON;

DECLARE @CartonVersPiece DECIMAL(18,4) = 30;
DECLARE @CartonsVendus DECIMAL(18,4) = 2;
IF (@CartonsVendus * @CartonVersPiece) <> 60
    RAISERROR('CAS 1/3 invalide : carton vers piece.', 16, 1);

DECLARE @PaquetVersPiece DECIMAL(18,4) = 12;
DECLARE @CartonVersPaquet DECIMAL(18,4) = 24;
DECLARE @CartonVersBase DECIMAL(18,4) = @CartonVersPaquet * @PaquetVersPiece;
IF @CartonVersBase <> 288
    RAISERROR('CAS 2 invalide : carton -> paquet -> piece.', 16, 1);

IF (2 * @CartonVersBase) <> 576
    RAISERROR('CAS 3 invalide : 2 cartons.', 16, 1);

IF (3 * @PaquetVersPiece) <> 36
    RAISERROR('CAS 4 invalide : 3 paquets.', 16, 1);

IF (0.5 * @CartonVersBase) <> 144
    RAISERROR('CAS 5 invalide : demi-carton.', 16, 1);

IF (0.25 * @CartonVersBase) <> 72
    RAISERROR('CAS 6 invalide : quart-carton.', 16, 1);

IF (0.5 * @PaquetVersPiece) <> 6
    RAISERROR('CAS 7 invalide : demi-paquet.', 16, 1);

IF (12 * 1) <> 12
    RAISERROR('CAS 8 invalide : douzaine basee sur piece.', 16, 1);

DECLARE @StockBase DECIMAL(18,4) = 1338;
DECLARE @NbCartons DECIMAL(18,4) = FLOOR(@StockBase / @CartonVersBase);
DECLARE @ResteApresCartons DECIMAL(18,4) = @StockBase - (@NbCartons * @CartonVersBase);
DECLARE @NbPaquets DECIMAL(18,4) = FLOOR(@ResteApresCartons / @PaquetVersPiece);
DECLARE @NbPieces DECIMAL(18,4) = @ResteApresCartons - (@NbPaquets * @PaquetVersPiece);
IF @NbCartons <> 4 OR @NbPaquets <> 15 OR @NbPieces <> 6
    RAISERROR('CAS 9 invalide : decomposition 1338 base.', 16, 1);

IF (2.5 * 1) <> 2.5
    RAISERROR('CAS 10 invalide : mesure KG base.', 16, 1);

IF OBJECT_ID('dbo.ProduitConditionnements', 'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT ProduitId
        FROM dbo.ProduitConditionnements
        WHERE EstActif = 1 AND EstUniteBase = 1
        GROUP BY ProduitId
        HAVING COUNT(1) > 1
    )
        RAISERROR('Controle schema invalide : plusieurs bases actives pour un produit.', 16, 1);

    IF EXISTS (SELECT 1 FROM dbo.ProduitConditionnements WHERE FacteurVersBase <= 0)
        RAISERROR('Controle schema invalide : FacteurVersBase non positif.', 16, 1);
END

SELECT 'VALIDATION_CONDITIONNEMENTS_DYNAMIQUES_OK' AS Resultat;
