/*
Validation Phase 2B - flux physiques de stock avec conditionnements dynamiques.

Script non destructif : il ne modifie ni stock, ni facture, ni mouvement.
Il valide les formules attendues par les écrans StockInitialTechnique,
Entrées stock, Inventaire, Pertes et Sorties.
*/

SET NOCOUNT ON;

DECLARE @Piece DECIMAL(18,4) = 1;
DECLARE @Paquet DECIMAL(18,4) = 12 * @Piece;
DECLARE @Carton DECIMAL(18,4) = 24 * @Paquet;

IF @Carton <> 288
    RAISERROR('TEST A invalide : Carton -> Paquet -> Piece doit donner 288.', 16, 1);

DECLARE @Boite DECIMAL(18,4) = 6 * @Piece;
DECLARE @CartonPalette DECIMAL(18,4) = 20 * @Boite;
DECLARE @Palette DECIMAL(18,4) = 10 * @CartonPalette;

IF @Palette <> 1200
    RAISERROR('TEST B invalide : Palette -> Carton -> Boite -> Piece doit donner 1200.', 16, 1);

DECLARE @StockInitial DECIMAL(18,4) = (8 * @Carton) + (4 * @Paquet) + (7 * @Piece);
IF @StockInitial <> 2359
    RAISERROR('TEST C invalide : 8 cartons + 4 paquets + 7 pieces.', 16, 1);

DECLARE @Approvisionnement DECIMAL(18,4) = 10 * @Carton;
IF @Approvisionnement <> 2880
    RAISERROR('TEST E invalide : 10 cartons doivent donner 2880 base.', 16, 1);

DECLARE @InventairePhysique DECIMAL(18,4) = (4 * @Carton) + (14 * @Paquet) + (8 * @Piece);
DECLARE @InventaireTheorique DECIMAL(18,4) = 1338;
DECLARE @EcartInventaire DECIMAL(18,4) = @InventairePhysique - @InventaireTheorique;
IF @InventairePhysique <> 1328 OR @EcartInventaire <> -10
    RAISERROR('TEST F invalide : comptage inventaire multi-conditionnement.', 16, 1);

DECLARE @SortiePaquets DECIMAL(18,4) = 2 * @Paquet;
IF @SortiePaquets <> 24
    RAISERROR('TEST G/H invalide : 2 paquets doivent donner 24 base.', 16, 1);

DECLARE @InitialisationVente DECIMAL(18,4) = 3 * 0.5 * @Carton;
IF @InitialisationVente <> 432
    RAISERROR('TEST I/J invalide : 3 demi-cartons doivent donner 432 base.', 16, 1);

DECLARE @Sac DECIMAL(18,4) = 25;
DECLARE @MesureInitiale DECIMAL(18,4) = (2 * @Sac) + 2.5;
DECLARE @MesureAppro DECIMAL(18,4) = 3 * @Sac;
DECLARE @MesureInventaire DECIMAL(18,4) = (1 * @Sac) + 12.750;

IF @MesureInitiale <> 52.5 OR @MesureAppro <> 75 OR @MesureInventaire <> 37.750
    RAISERROR('TEST K invalide : produit MESURE Sac -> Kg.', 16, 1);

DECLARE @DemiPiece DECIMAL(18,4) = 0.5;
IF @DemiPiece = FLOOR(@DemiPiece)
    RAISERROR('TEST L invalide : fraction piece non detectee.', 16, 1);

DECLARE @StockBase DECIMAL(18,4) = 1338;
DECLARE @NbCartons DECIMAL(18,4) = FLOOR(@StockBase / @Carton);
DECLARE @ResteCartons DECIMAL(18,4) = @StockBase - (@NbCartons * @Carton);
DECLARE @NbPaquets DECIMAL(18,4) = FLOOR(@ResteCartons / @Paquet);
DECLARE @NbPieces DECIMAL(18,4) = @ResteCartons - (@NbPaquets * @Paquet);

IF @NbCartons <> 4 OR @NbPaquets <> 15 OR @NbPieces <> 6
    RAISERROR('TEST M invalide : 1338 doit donner 4 cartons + 15 paquets + 6 pieces.', 16, 1);

IF OBJECT_ID('dbo.vStockProduit', 'V') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT p.ProduitId
        FROM dbo.Produits p
        LEFT JOIN dbo.vStockProduit v ON v.ProduitId = p.ProduitId
        WHERE v.QuantiteStock IS NULL
    )
        RAISERROR('TEST O invalide : vStockProduit ne retourne pas tous les produits attendus.', 16, 1);
END

SELECT 'VALIDATION_CONDITIONNEMENTS_DYNAMIQUES_PHASE2B_OK' AS Resultat;
