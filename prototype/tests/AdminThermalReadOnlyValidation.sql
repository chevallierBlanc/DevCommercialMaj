-- LECTURE SEULE sur une COPIE de test avec les migrations deja deployees.
-- Executer avant/apres apercu et impression de la MEME facture, sans autre
-- operation simultanee sur cette facture. Aucun paiement ni stock n'est cree.
SET NOCOUNT ON;
SET LOCK_TIMEOUT 5000;
DECLARE @FactureId int = 0, @NomRole nvarchar(50) = N'ADMIN';
IF @FactureId <= 0 THROW 51000, 'Renseigner un identifiant de facture de test existant.', 1;
SELECT @@SERVERNAME AS Serveur, DB_NAME() AS BaseInspectee, SYSUTCDATETIME() AS CollecteUtc;
SELECT COL_LENGTH(N'dbo.Paiements', N'DeviseMontants') AS ColonneDeviseMontants;
SELECT i.CodeInterface
FROM dbo.RoleInterfaces ri JOIN dbo.Roles r ON r.RoleId=ri.RoleId
JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId
WHERE r.NomRole=@NomRole AND ISNULL(r.EstActif,1)=1 AND i.EstActif=1
ORDER BY i.CodeInterface;
SELECT f.FactureVenteId,f.NumeroFacture,f.CreeLe,f.CreePar,f.Statut,
 f.SousTotal,f.MontantRemise,f.MontantTotal
FROM dbo.FacturesVente f WHERE f.FactureVenteId=@FactureId;
SELECT LigneFactureVenteId,ProduitId,TypeVente,QuantiteSaisie,Quantite,
 PrixUnitaire,MontantLigne
FROM dbo.LignesFactureVente WHERE FactureVenteId=@FactureId ORDER BY LigneFactureVenteId;
SELECT PaiementId,FactureVenteId,Montant,DeviseMontants,PayePar,PayeLe,
 ModePaiement,ReferencePaiement
FROM dbo.Paiements WHERE FactureVenteId=@FactureId ORDER BY PaiementId;
SELECT s.RefSource,s.ProduitId,s.QuantiteBase
FROM dbo.StockSortie s JOIN dbo.FacturesVente f ON f.NumeroFacture=s.RefSource
WHERE f.FactureVenteId=@FactureId AND s.Source='VENTE';
-- Comparer les resultats avant/apres. Un timeout de lecture n'est pas une preuve
-- d'integrite ou d'absence d'ecriture. Ne pas debloquer via KILL ou NOLOCK.
