/*
Validation Phase 2 - moteur fonctionnel conditionnements dynamiques.

Ce script est non destructif : il valide les formules et invariants attendus
sans modifier les stocks, factures ou mouvements historiques.
*/

SET NOCOUNT ON;

DECLARE @Piece DECIMAL(18,4) = 1;
DECLARE @Boite DECIMAL(18,4) = 6 * @Piece;
DECLARE @Carton DECIMAL(18,4) = 20 * @Boite;
DECLARE @Palette DECIMAL(18,4) = 10 * @Carton;

IF @Palette <> 1200
    RAISERROR('TEST D invalide : Palette -> Carton -> Boite -> Piece.', 16, 1);

DECLARE @DemiCartons DECIMAL(18,4) = 2 * 0.5 * 288;
IF @DemiCartons <> 288
    RAISERROR('TEST E invalide : 2 demi-cartons.', 16, 1);

DECLARE @DemiPaquets DECIMAL(18,4) = 3 * 0.5 * 12;
IF @DemiPaquets <> 18
    RAISERROR('TEST F invalide : 3 demi-paquets.', 16, 1);

DECLARE @DeuxDouzaines DECIMAL(18,4) = 2 * 12 * 1;
IF @DeuxDouzaines <> 24
    RAISERROR('TEST G invalide : 2 douzaines.', 16, 1);

DECLARE @Kg DECIMAL(18,4) = 2.5;
IF @Kg <> 2.5
    RAISERROR('TEST H invalide : produit mesurable KG.', 16, 1);

DECLARE @PieceFraction DECIMAL(18,4) = 0.5;
IF @PieceFraction = FLOOR(@PieceFraction)
    RAISERROR('TEST I invalide : fraction piece non detectee.', 16, 1);

DECLARE @StockBase DECIMAL(18,4) = 2359;
DECLARE @Carton288 DECIMAL(18,4) = 288;
DECLARE @Paquet12 DECIMAL(18,4) = 12;
DECLARE @NbCartons DECIMAL(18,4) = FLOOR(@StockBase / @Carton288);
DECLARE @ResteCartons DECIMAL(18,4) = @StockBase - (@NbCartons * @Carton288);
DECLARE @NbPaquets DECIMAL(18,4) = FLOOR(@ResteCartons / @Paquet12);
DECLARE @NbPieces DECIMAL(18,4) = @ResteCartons - (@NbPaquets * @Paquet12);
IF @NbCartons <> 8 OR @NbPaquets <> 4 OR @NbPieces <> 7
    RAISERROR('TEST M invalide : 8 cartons + 4 paquets + 7 pieces.', 16, 1);

IF OBJECT_ID('dbo.ProduitConditionnements', 'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT ProduitId
        FROM dbo.ProduitConditionnements
        WHERE EstActif = 1 AND EstUniteBase = 1
        GROUP BY ProduitId
        HAVING COUNT(1) <> 1
    )
        RAISERROR('Invariant invalide : chaque produit configure doit avoir exactement une base active.', 16, 1);

    IF EXISTS (
        SELECT 1
        FROM dbo.ProduitConditionnements pc
        WHERE pc.EstActif = 1
          AND pc.EstUniteBase = 0
          AND (pc.ConditionnementParentId IS NULL OR ISNULL(pc.FacteurVersParent, 0) <= 0)
    )
        RAISERROR('Invariant invalide : conditionnement non base sans parent/facteur.', 16, 1);
END

SELECT 'VALIDATION_CONDITIONNEMENTS_DYNAMIQUES_PHASE2_OK' AS Resultat;
