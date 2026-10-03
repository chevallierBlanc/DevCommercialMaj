/*
    Validation non destructive - Phase 3 conditionnements.
    Ce script contrôle uniquement le référentiel et les règles de filtrage
    achat/vente/fraction. Il ne modifie aucune transaction historique.
*/

SET NOCOUNT ON;

IF OBJECT_ID('dbo.UnitesMesure', 'U') IS NULL
    THROW 51000, 'Table dbo.UnitesMesure introuvable.', 1;

IF OBJECT_ID('dbo.ProduitConditionnements', 'U') IS NULL
    THROW 51000, 'Table dbo.ProduitConditionnements introuvable.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.UnitesMesure WHERE Code = N'PIECE' AND Libelle = N'Pièce')
BEGIN
    SELECT UniteMesureId, Code, Libelle, Symbole, EstActif
    FROM dbo.UnitesMesure
    WHERE UPPER(LTRIM(RTRIM(Code))) COLLATE Latin1_General_CI_AI = N'PIECE'
       OR UPPER(LTRIM(RTRIM(Libelle))) COLLATE Latin1_General_CI_AI = N'PIECE'
       OR UPPER(LTRIM(RTRIM(Symbole))) COLLATE Latin1_General_CI_AI = N'PIECE';

    THROW 51000, 'Unite PIECE non normalisee avec le libelle Pièce.', 1;
END

IF EXISTS (
    SELECT Code
    FROM dbo.UnitesMesure
    WHERE Code IN (N'BALLON', N'PLAQUETTE', N'CHAINE', N'SEAU', N'PEAU', N'DEMI_SACHET')
    GROUP BY Code
    HAVING COUNT(*) > 1
)
    THROW 51000, 'Doublon detecte dans les nouvelles unites de conditionnement.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.UnitesMesure WHERE Code = N'BALLON')
    THROW 51000, 'Unite BALLON absente.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UnitesMesure WHERE Code = N'PLAQUETTE')
    THROW 51000, 'Unite PLAQUETTE absente.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UnitesMesure WHERE Code = N'CHAINE' AND Libelle = N'Chaîne')
    THROW 51000, 'Unite CHAINE/Chaîne absente.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UnitesMesure WHERE Code = N'SEAU')
    THROW 51000, 'Unite SEAU absente.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UnitesMesure WHERE Code = N'PEAU')
    THROW 51000, 'Unite PEAU absente.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.UnitesMesure WHERE Code = N'DEMI_SACHET' AND Libelle = N'Demi-sachet')
    THROW 51000, 'Unite DEMI_SACHET/Demi-sachet absente.', 1;

IF EXISTS (
    SELECT ProduitId
    FROM dbo.ProduitConditionnements
    WHERE EstActif = 1 AND EstUniteBase = 1
    GROUP BY ProduitId
    HAVING COUNT(*) > 1
)
    THROW 51000, 'Produit avec plusieurs unites de base actives.', 1;

IF EXISTS (
    SELECT ProduitId, UniteMesureId
    FROM dbo.ProduitConditionnements
    WHERE EstActif = 1
    GROUP BY ProduitId, UniteMesureId
    HAVING COUNT(*) > 1
)
    THROW 51000, 'Doublon actif Produit + UniteMesure detecte.', 1;

IF EXISTS (
    SELECT 1
    FROM dbo.ProduitConditionnements
    WHERE EstActif = 1
      AND EstUniteBase = 0
      AND ConditionnementParentId IS NULL
)
    THROW 51000, 'Conditionnement actif non base sans unite contenue.', 1;

IF EXISTS (
    SELECT 1
    FROM dbo.ProduitConditionnements enfant
    LEFT JOIN dbo.ProduitConditionnements parent
        ON parent.ProduitConditionnementId = enfant.ConditionnementParentId
       AND parent.EstActif = 1
    WHERE enfant.EstActif = 1
      AND enfant.EstUniteBase = 0
      AND parent.ProduitConditionnementId IS NULL
)
    THROW 51000, 'Conditionnement actif rattache a une unite contenue inactive ou inexistante.', 1;

SELECT 'VALIDATION_CONDITIONNEMENTS_DYNAMIQUES_PHASE3_OK' AS Resultat;
