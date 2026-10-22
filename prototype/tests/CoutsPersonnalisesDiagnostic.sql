-- LECTURE SEULE. Aucun UPDATE de reference physique deduit du nom commercial.
SET NOCOUNT ON;
DECLARE @ProduitId int=NULL, @Libelle nvarchar(200)=N'paquet stylo';
SELECT @@SERVERNAME AS Serveur,DB_NAME() AS BaseInspectee,SYSUTCDATETIME() AS DateUtc;
SELECT p.ProduitId,p.Libelle,p.CodeBarres,p.PrixAchat,p.UnitePrincipale,p.UniteSecondaire,
 p.ConversionUnite,p.TypeGestionStock,p.ContenuUnitePrincipale,p.ContenuUniteSecondaire,
 p.PrixGros,p.PrixDemi,p.PrixDetail
FROM dbo.Produits p WHERE (@ProduitId IS NOT NULL AND p.ProduitId=@ProduitId)
 OR (@ProduitId IS NULL AND p.Libelle COLLATE Latin1_General_CI_AI LIKE N'%'+@Libelle+N'%');
;WITH Donnees AS (
 SELECT p.ProduitId,p.Libelle,p.PrixAchat,p.ConversionUnite,p.TypeGestionStock,
 p.ContenuUnitePrincipale,p.ContenuUniteSecondaire,
 t.TypeVenteProduitId,t.Nom,t.QuantiteEquivalent,t.TypeUniteEquivalent,
 t.TypeQuantiteEquivalent,t.ProduitConditionnementId,t.ModePrix,t.Coefficient,t.PrixVente,
 c.FacteurVersBase,c.EstActif AS ConditionnementActif,c.EstVendable,
 u.Code AS UniteConditionnement,
 CASE WHEN UPPER(p.TypeGestionStock) IN('MESURE','POIDS','VOLUME') AND p.ContenuUnitePrincipale>0 THEN p.ContenuUnitePrincipale
      WHEN p.ConversionUnite>0 THEN p.ConversionUnite ELSE 1 END AS ReferenceAchatBase
 FROM dbo.Produits p JOIN dbo.TypesVenteProduit t ON t.ProduitId=p.ProduitId
 LEFT JOIN dbo.ProduitConditionnements c ON c.ProduitConditionnementId=t.ProduitConditionnementId AND c.ProduitId=p.ProduitId
 LEFT JOIN dbo.UnitesMesure u ON u.UniteMesureId=c.UniteMesureId
 WHERE (@ProduitId IS NOT NULL AND p.ProduitId=@ProduitId)
 OR (@ProduitId IS NULL AND p.Libelle COLLATE Latin1_General_CI_AI LIKE N'%'+@Libelle+N'%')
), Physique AS (
 SELECT *,CASE WHEN ProduitConditionnementId IS NOT NULL THEN QuantiteEquivalent*FacteurVersBase
 WHEN UPPER(ISNULL(TypeQuantiteEquivalent,TypeUniteEquivalent))='PRINCIPALE' THEN QuantiteEquivalent*
  CASE WHEN ContenuUnitePrincipale>0 THEN ContenuUnitePrincipale WHEN ConversionUnite>0 THEN ConversionUnite ELSE 1 END
 WHEN UPPER(ISNULL(TypeQuantiteEquivalent,TypeUniteEquivalent))='MESURE' THEN QuantiteEquivalent
 ELSE QuantiteEquivalent*CASE WHEN ContenuUniteSecondaire>0 THEN ContenuUniteSecondaire ELSE 1 END END AS QuantiteBaseCalculee
 FROM Donnees
)
SELECT *,
 ROUND(ROUND(PrixAchat/NULLIF(ReferenceAchatBase,0),4)*QuantiteBaseCalculee,2) AS CoutAncienArrondiIntermediaire,
 ROUND(PrixAchat*(QuantiteBaseCalculee/NULLIF(ReferenceAchatBase,0)),2) AS CoutComparableFC,
 N'Verifier quantite et reference persistees. Un nom tel que 10+1 ne definit aucune conversion.' AS Diagnostic
FROM Physique ORDER BY ProduitId,TypeVenteProduitId;
-- Montrer toute la hierarchie, y compris les lignes desactivees pour diagnostic.
SELECT c.ProduitId,c.ProduitConditionnementId,c.ConditionnementParentId,u.Code,
 c.FacteurVersBase,c.EstUniteBase,c.EstActif,c.EstVendable
FROM dbo.ProduitConditionnements c JOIN dbo.UnitesMesure u ON u.UniteMesureId=c.UniteMesureId
JOIN dbo.Produits p ON p.ProduitId=c.ProduitId
WHERE (@ProduitId IS NOT NULL AND p.ProduitId=@ProduitId)
 OR (@ProduitId IS NULL AND p.Libelle COLLATE Latin1_General_CI_AI LIKE N'%'+@Libelle+N'%')
ORDER BY c.ProduitId,c.FacteurVersBase;
