-- Diagnostic NON DESTRUCTIF : aucun reset, DELETE, UPDATE ni grant.
SET NOCOUNT ON;
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaVersion WHERE Version=2026101301)
    THROW 51000, 'Migration 2026101301 absente sur cette base. Demarrer la nouvelle application ou appliquer sa migration sur une copie.', 1;
DECLARE @Colonnes table(TableNom sysname,Colonne sysname);
INSERT @Colonnes VALUES(N'Parametres',N'PrixDemandeDureeHeures'),
(N'Roles',N'VersionAutorisations'),
(N'DemandesModificationPrix',N'Perimetre'),(N'DemandesModificationPrix',N'ContexteFacture'),
(N'DemandesModificationPrix',N'QuantiteContexte'),(N'DemandesModificationPrix',N'UtiliseeLe'),
(N'DemandesModificationPrix',N'FactureUtilisationId'),(N'DemandesModificationPrix',N'CoutReferenceDemande'),
(N'DemandesModificationPrix',N'SousTotalContexte'),(N'DemandesModificationPrix',N'RemiseContexte'),
(N'DemandesModificationPrix',N'TypeTarifId'),(N'LignesFactureVente',N'TypeTarifId'),
(N'LignesFactureVente',N'DemandePrixId'),(N'LignesFactureVente',N'ContextePrix'),
(N'AppDataVersions',N'VersionCourante');
SELECT TableNom,Colonne AS ColonneManquante FROM @Colonnes WHERE COL_LENGTH(N'dbo.'+TableNom,Colonne) IS NULL;
IF EXISTS(SELECT 1 FROM @Colonnes WHERE COL_LENGTH(N'dbo.'+TableNom,Colonne) IS NULL)
    THROW 51000, 'Schema integration prix/modes incomplet.', 1;
IF (SELECT COUNT(*) FROM dbo.Parametres)<>1
    THROW 51000, 'Configuration etablissement absente ou ambigue.', 1;
EXEC(N'SELECT DB_NAME() AS BaseVerifiee,GestionModesActive,ModeFacturationAutorise,ModeCaisseAutorise,ModeCombineAutorise,DelegationPrixActive,PrixDemandesActives,PrixModificationImmediate,PrixSousCoutAutorise,PrixVariationMaxPourcent,PrixMargeMinimumPourcent,PrixDemandeDureeHeures FROM dbo.Parametres;
IF EXISTS(SELECT 1 FROM dbo.DemandesModificationPrix WHERE Perimetre NOT IN(''TARIF'',''FACTURE'') OR (Perimetre=''FACTURE'' AND (ContexteFacture IS NULL OR QuantiteContexte IS NULL OR QuantiteContexte<=0)))
    THROW 51000, ''Demande facture sans contexte coherent.'', 1;
IF EXISTS(SELECT 1 FROM dbo.DemandesModificationPrix WHERE Perimetre=''FACTURE'' AND (SousTotalContexte IS NULL OR SousTotalContexte<=0 OR RemiseContexte IS NULL OR RemiseContexte<0 OR RemiseContexte>SousTotalContexte))
    THROW 51000, ''Contexte financier de demande incomplet.'', 1;
IF EXISTS(SELECT 1 FROM dbo.LignesFactureVente l LEFT JOIN dbo.DemandesModificationPrix d ON d.DemandeId=l.DemandePrixId WHERE l.DemandePrixId IS NOT NULL AND (d.DemandeId IS NULL OR d.Perimetre<>''FACTURE'' OR d.UtiliseeLe IS NULL OR d.FactureUtilisationId<>l.FactureVenteId OR d.NouveauPrix<>l.PrixUnitaire OR d.ProduitId<>l.ProduitId OR d.TypeVente<>l.TypeVente OR l.ContextePrix IS NULL OR d.ContexteFacture<>l.ContextePrix OR d.QuantiteContexte<>ISNULL(l.QuantiteSaisie,l.Quantite)))
    THROW 51000, ''Ligne et approbation facture non concordantes.'', 1;
SELECT Perimetre,Etat,COUNT_BIG(*) AS Nombre FROM dbo.DemandesModificationPrix GROUP BY Perimetre,Etat;
SELECT ProduitId,Libelle AS ProduitSansCoutPourDelegation FROM dbo.Produits WHERE EstActif=1 AND ISNULL(PrixAchat,0)<=0;
SELECT TypeVenteProduitId,ProduitId,Nom AS NomCommercialReserve FROM dbo.TypesVenteProduit WHERE Actif=1 AND LOWER(Nom) IN(''gros'',''demi'',''quart'',''piece'',''douzaine'',''promo'',''speciale'');
SELECT TOP(50) SessionId,UtilisateurId,RoleIdActif,ModeActif,Debut,Fin,Poste FROM dbo.UtilisateurSessions ORDER BY SessionId DESC;');
SELECT r.NomRole,i.CodeInterface,i.Libelle
FROM dbo.RoleInterfaces ri JOIN dbo.Roles r ON r.RoleId=ri.RoleId JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId
WHERE i.CodeInterface LIKE N'MODE[_]%' OR i.CodeInterface LIKE N'PRIX[_]%' OR i.CodeInterface IN(N'FACTURIER',N'CAISSE',N'PARAMETRES',N'PARAMETRES_SECURITE',N'PARAMETRES_PRIX',N'FACTURE_CREER',N'ENCAISSEMENT_CREER') ORDER BY r.NomRole,i.CodeInterface;
-- Aide a l'integration : un grant MODE_COMBINE ne fournit aucun ecran/action.
SELECT r.NomRole AS RoleCombineSansEcrans
FROM dbo.Roles r WHERE EXISTS(SELECT 1 FROM dbo.RoleInterfaces ri JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=r.RoleId AND i.CodeInterface=N'MODE_COMBINE')
AND (SELECT COUNT(DISTINCT i.CodeInterface) FROM dbo.RoleInterfaces ri JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=r.RoleId AND i.EstActif=1 AND i.CodeInterface IN(N'FACTURIER',N'CAISSE'))<>2;
SELECT r.NomRole AS RolePrixSansConsultation
FROM dbo.Roles r WHERE EXISTS(SELECT 1 FROM dbo.RoleInterfaces ri JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=r.RoleId AND i.CodeInterface IN(N'PRIX_TARIF_MODIFIER',N'PRIX_FACTURE_EXCEPTION',N'PRIX_DEMANDER',N'PRIX_DEMANDER_FACTURE'))
AND NOT EXISTS(SELECT 1 FROM dbo.RoleInterfaces ri JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=r.RoleId AND i.EstActif=1 AND i.CodeInterface=N'PRIX_CONSULTER');
PRINT N'VALIDATION_SCHEMA_INTEGRATION_PHASE2_OK';
-- Ce marqueur ne prouve ni l'execution des workflows ni la charge multi-postes.
