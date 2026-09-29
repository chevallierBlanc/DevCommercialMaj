/*
Validation Phase 2C - UX métier et calculs N niveaux.

Script non destructif : il ne modifie aucune transaction, aucun stock,
aucune facture et aucune QuantiteBase historique. Il valide uniquement les
formules attendues par le moteur de conditionnements dynamiques.
*/

SET NOCOUNT ON;

DECLARE @Piece DECIMAL(18,4) = 1;
DECLARE @Carton30 DECIMAL(18,4) = 30 * @Piece;
IF @Carton30 <> 30
    RAISERROR('CAS A invalide : 1 Carton doit donner 30 Pieces.', 16, 1);

DECLARE @Paquet12 DECIMAL(18,4) = 12 * @Piece;
DECLARE @Carton288 DECIMAL(18,4) = 24 * @Paquet12;
IF @Paquet12 <> 12 OR @Carton288 <> 288
    RAISERROR('CAS B invalide : Carton -> Paquet -> Piece doit donner 288.', 16, 1);

DECLARE @Paquet5N DECIMAL(18,4) = 10 * @Piece;
DECLARE @Boite5N DECIMAL(18,4) = 6 * @Paquet5N;
DECLARE @Carton5N DECIMAL(18,4) = 12 * @Boite5N;
DECLARE @Palette5N DECIMAL(18,4) = 40 * @Carton5N;
IF @Paquet5N <> 10 OR @Boite5N <> 60 OR @Carton5N <> 720 OR @Palette5N <> 28800
    RAISERROR('CAS C invalide : Palette -> Carton -> Boite -> Paquet -> Piece.', 16, 1);

DECLARE @Kg DECIMAL(18,4) = 1;
DECLARE @SachetKg DECIMAL(18,4) = 5 * @Kg;
DECLARE @SacKg DECIMAL(18,4) = 50 * @Kg;
IF @SachetKg <> 5 OR @SacKg <> 50
    RAISERROR('CAS D invalide : Sac/Sachet -> Kg.', 16, 1);

DECLARE @DemiCarton DECIMAL(18,4) = 0.5 * @Carton288;
DECLARE @Douzaine DECIMAL(18,4) = 12 * @Piece;
DECLARE @Paquet10Plus1 DECIMAL(18,4) = 11 * @Paquet12;
IF @DemiCarton <> 144 OR @Douzaine <> 12 OR @Paquet10Plus1 <> 132
    RAISERROR('CAS E invalide : types commerciaux dynamiques.', 16, 1);

DECLARE @StockTheorique DECIMAL(18,4) = 917;
DECLARE @ComptageInventaire DECIMAL(18,4) = (3 * @Carton288) + (4 * @Paquet12) + (5 * @Piece);
IF @ComptageInventaire <> @StockTheorique
    RAISERROR('CAS F invalide : inventaire 3 Cartons + 4 Paquets + 5 Pieces.', 16, 1);

DECLARE @Approvisionnement DECIMAL(18,4) = 10 * @Carton288;
IF @Approvisionnement <> 2880
    RAISERROR('CAS G invalide : approvisionnement 10 Cartons.', 16, 1);

DECLARE @Sortie DECIMAL(18,4) = 2 * @Paquet12;
IF @Sortie <> 24
    RAISERROR('CAS H invalide : sortie/perte 2 Paquets.', 16, 1);

DECLARE @InitHistorique DECIMAL(18,4) = 2 * @DemiCarton;
IF @InitHistorique <> 288
    RAISERROR('CAS I invalide : initialisation ventes demi-carton.', 16, 1);

IF OBJECT_ID('dbo.LignesFactureVente', 'U') IS NOT NULL
BEGIN
    DECLARE @HashAvant INT;
    DECLARE @HashApres INT;
    SELECT @HashAvant = CHECKSUM_AGG(CHECKSUM(LigneFactureVenteId, ProduitId, QuantiteBase))
    FROM dbo.LignesFactureVente;

    SELECT @HashApres = CHECKSUM_AGG(CHECKSUM(LigneFactureVenteId, ProduitId, QuantiteBase))
    FROM dbo.LignesFactureVente;

    IF ISNULL(@HashAvant, 0) <> ISNULL(@HashApres, 0)
        RAISERROR('CAS J invalide : une QuantiteBase historique a varie pendant la validation.', 16, 1);
END

SELECT 'VALIDATION_CONDITIONNEMENTS_DYNAMIQUES_PHASE2C_OK' AS Resultat;
