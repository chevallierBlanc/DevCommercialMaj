-- Lecture seule. Choisir explicitement la copie de base avant execution.
SET NOCOUNT ON;
DECLARE @Annee int=2026, @Mois int=10;
IF @Annee<1900 OR @Annee>9998 OR @Mois<0 OR @Mois>12
 THROW 51000, 'Periode invalide.', 1;
DECLARE @Du datetime2=DATEFROMPARTS(@Annee,CASE WHEN @Mois=0 THEN 1 ELSE @Mois END,1);
DECLARE @Au datetime2=CASE WHEN @Mois=0 THEN DATEADD(YEAR,1,@Du) ELSE DATEADD(MONTH,1,@Du) END;
SELECT @@SERVERNAME AS Serveur,DB_NAME() AS BaseInspectee,@Du AS DebutInclus,@Au AS FinExclue;
-- Une ligne de facture commerciale compte une seule fois, independamment des articles.
SELECT TOP(20) c.ClientId,c.NomClient,c.Telephone,COUNT_BIG(*) AS NbAchats,
 SUM(CAST(f.MontantTotal AS decimal(28,2))) AS TotalAchatsFC,
 AVG(CAST(f.MontantTotal AS decimal(28,2))) AS MoyenneAchatFC
FROM dbo.Clients c JOIN dbo.FacturesVente f ON f.ClientId=c.ClientId
WHERE f.Statut='PAYEE' AND f.CreeLe>=@Du AND f.CreeLe<@Au
GROUP BY c.ClientId,c.NomClient,c.Telephone ORDER BY TotalAchatsFC DESC,c.ClientId;
-- Homonymes : diagnostic seulement, aucune fusion ou correction d'identite.
SELECT TOP(100) c.ClientId,c.NomClient,c.Telephone,c.EstActif
FROM dbo.Clients c
WHERE EXISTS(SELECT 1 FROM dbo.Clients autre WHERE autre.NomClient=c.NomClient AND autre.ClientId<>c.ClientId)
ORDER BY c.NomClient,c.ClientId;
SELECT COUNT_BIG(*) AS FacturesPayeesSansClientFiable
FROM dbo.FacturesVente f LEFT JOIN dbo.Clients c ON c.ClientId=f.ClientId
WHERE f.Statut='PAYEE' AND f.CreeLe>=@Du AND f.CreeLe<@Au AND c.ClientId IS NULL;
SELECT TOP(100) ProduitId,Libelle,UnitePrincipale,PrixAchat,PrixGros,PrixDetail,
 PrixDemi,PrixQuart,PrixDouzaine,PrixSpecial,VenteGros,VenteDetail,VenteDemi,VenteDouzaine
FROM dbo.Produits WHERE EstActif=1 ORDER BY ProduitId;
-- Valeurs stockees : un mode COEFFICIENT doit etre evalue par le service existant.
SELECT TOP(100) t.ProduitId,t.TypeVenteProduitId,t.Nom,t.Actif,t.ModePrix,
 t.Coefficient,t.PrixVente AS PrixStocke,t.ProduitConditionnementId,
 c.EstActif AS ConditionnementActif,c.EstVendable,c.FacteurVersBase
FROM dbo.TypesVenteProduit t LEFT JOIN dbo.ProduitConditionnements c
 ON c.ProduitConditionnementId=t.ProduitConditionnementId AND c.ProduitId=t.ProduitId
ORDER BY t.ProduitId,t.TypeVenteProduitId;
SELECT Domaine,VersionCourante,ModifieLe,Poste FROM dbo.AppDataVersions
WHERE Domaine IN('PRODUITS','TYPES_VENTE');
SELECT TOP(30) [Action],EntiteId,EffectuePar,UtilisateurNom,RoleActif,ModeActif,
 ReferenceDocument,Motif,Resultat,AnciennesValeurs,NouvellesValeurs
FROM dbo.JournalAudit WHERE Categorie='PRIX' ORDER BY EffectueLe DESC;
IF OBJECT_ID(N'dbo.CloturesCaisse',N'U') IS NOT NULL
 EXEC(N'SELECT TOP(20) DateCaisse,UtilisateurId,Statut,EstCloturee FROM dbo.CloturesCaisse ORDER BY DateCaisse DESC;');
-- Aucun INSERT/UPDATE/DELETE, aucune cloture ou regularisation executee.
