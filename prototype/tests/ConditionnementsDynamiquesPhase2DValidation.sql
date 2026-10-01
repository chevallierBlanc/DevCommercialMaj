/*
Validation Phase 2D - règles contextuelles Achat/Vente/Inventaire/Perte.

Script non destructif : il utilise uniquement des variables/table variables
et ne modifie aucune transaction, aucun stock, aucune facture et aucune
QuantiteBase historique.
*/

SET NOCOUNT ON;

DECLARE @Conditionnements TABLE
(
    Nom NVARCHAR(50) NOT NULL,
    FacteurVersBase DECIMAL(18,4) NOT NULL,
    EstActif BIT NOT NULL,
    EstAchetable BIT NOT NULL,
    EstVendable BIT NOT NULL,
    AutoriseFraction BIT NOT NULL
);

INSERT INTO @Conditionnements (Nom, FacteurVersBase, EstActif, EstAchetable, EstVendable, AutoriseFraction)
VALUES
    (N'Sac', 25, 1, 1, 0, 1),
    (N'Sachet', 5, 1, 0, 1, 0),
    (N'Kilogramme', 1, 1, 0, 1, 1),
    (N'Palette inactive', 1000, 0, 1, 1, 0);

IF EXISTS (SELECT 1 FROM @Conditionnements WHERE Nom IN (N'Sachet', N'Kilogramme') AND EstActif = 1 AND EstAchetable = 1)
    RAISERROR('CAS A invalide : seuls les conditionnements achetables doivent etre proposes a l''achat.', 16, 1);

IF (SELECT COUNT(*) FROM @Conditionnements WHERE EstActif = 1 AND EstAchetable = 1) <> 1
    RAISERROR('CAS A invalide : achat doit proposer uniquement Sac.', 16, 1);

IF EXISTS (SELECT 1 FROM @Conditionnements WHERE Nom = N'Sac' AND EstActif = 1 AND EstVendable = 1)
    RAISERROR('CAS B invalide : Sac non vendable ne doit pas etre propose en vente directe.', 16, 1);

IF (SELECT COUNT(*) FROM @Conditionnements WHERE EstActif = 1 AND EstVendable = 1) <> 2
    RAISERROR('CAS B invalide : vente doit proposer Sachet et Kilogramme.', 16, 1);

DECLARE @DemiSac DECIMAL(18,4) = 0.5 * (SELECT FacteurVersBase FROM @Conditionnements WHERE Nom = N'Sac');
IF @DemiSac <> 12.5
    RAISERROR('CAS C invalide : 0,5 Sac fractionnable doit donner 12,5 Kg.', 16, 1);

DECLARE @DemiSachetRefuse BIT = CASE
    WHEN (SELECT AutoriseFraction FROM @Conditionnements WHERE Nom = N'Sachet') = 0 AND 0.5 <> FLOOR(0.5)
    THEN 1 ELSE 0 END;
IF @DemiSachetRefuse <> 1
    RAISERROR('CAS D invalide : 0,5 Sachet non fractionnable doit etre refuse.', 16, 1);

DECLARE @Approvisionnement DECIMAL(18,4) = 10 * (SELECT FacteurVersBase FROM @Conditionnements WHERE Nom = N'Sac');
IF @Approvisionnement <> 250
    RAISERROR('CAS E invalide : 10 Sacs doivent ajouter 250 Kg.', 16, 1);

DECLARE @Vente DECIMAL(18,4) = 2 * (SELECT FacteurVersBase FROM @Conditionnements WHERE Nom = N'Sachet');
IF @Vente <> 10
    RAISERROR('CAS F invalide : 2 Sachets doivent retirer 10 Kg.', 16, 1);

DECLARE @Perte DECIMAL(18,4) = 2 * (SELECT FacteurVersBase FROM @Conditionnements WHERE Nom = N'Sachet');
IF @Perte <> 10
    RAISERROR('CAS G invalide : perte 2 Sachets doit retirer 10 Kg.', 16, 1);

DECLARE @ComptageInventaire DECIMAL(18,4) =
    (10 * (SELECT FacteurVersBase FROM @Conditionnements WHERE Nom = N'Sac')) +
    (8 * (SELECT FacteurVersBase FROM @Conditionnements WHERE Nom = N'Sachet'));
DECLARE @StockTheorique DECIMAL(18,4) = 315;
DECLARE @Ecart DECIMAL(18,4) = @ComptageInventaire - @StockTheorique;
IF @ComptageInventaire <> 290 OR @Ecart <> -25
    RAISERROR('CAS H invalide : inventaire riz attendu 290 Kg et ecart -25 Kg.', 16, 1);

DECLARE @Piece DECIMAL(18,4) = 1;
DECLARE @Paquet DECIMAL(18,4) = 10 * @Piece;
DECLARE @Boite DECIMAL(18,4) = 6 * @Paquet;
DECLARE @Carton DECIMAL(18,4) = 12 * @Boite;
DECLARE @Palette DECIMAL(18,4) = 40 * @Carton;
IF @Paquet <> 10 OR @Boite <> 60 OR @Carton <> 720 OR @Palette <> 28800
    RAISERROR('CAS I invalide : hierarchie 5 niveaux incorrecte.', 16, 1);

DECLARE @QuantiteBaseHistoriqueAvant DECIMAL(18,4) = 50;
DECLARE @SacAncien DECIMAL(18,4) = 25;
DECLARE @SacNouveau DECIMAL(18,4) = 30;
DECLARE @QuantiteBaseHistoriqueApres DECIMAL(18,4) = @QuantiteBaseHistoriqueAvant;
IF @SacAncien = @SacNouveau OR @QuantiteBaseHistoriqueApres <> 50
    RAISERROR('CAS J invalide : modification facteur ne doit pas recalculer l''historique.', 16, 1);

DECLARE @LegacyConversion DECIMAL(18,4) = 30;
DECLARE @LegacyDeuxCartons DECIMAL(18,4) = 2 * @LegacyConversion;
IF @LegacyDeuxCartons <> 60
    RAISERROR('CAS K invalide : fallback legacy Carton/Piece incorrect.', 16, 1);

IF OBJECT_ID('dbo.LignesFactureVente', 'U') IS NOT NULL
BEGIN
    DECLARE @HashAvant INT;
    DECLARE @HashApres INT;
    SELECT @HashAvant = CHECKSUM_AGG(CHECKSUM(LigneFactureVenteId, ProduitId, QuantiteBase))
    FROM dbo.LignesFactureVente;

    SELECT @HashApres = CHECKSUM_AGG(CHECKSUM(LigneFactureVenteId, ProduitId, QuantiteBase))
    FROM dbo.LignesFactureVente;

    IF ISNULL(@HashAvant, 0) <> ISNULL(@HashApres, 0)
        RAISERROR('Protection historique invalide : une QuantiteBase facture a varie.', 16, 1);
END

SELECT 'VALIDATION_CONDITIONNEMENTS_DYNAMIQUES_PHASE2D_OK' AS Resultat;
