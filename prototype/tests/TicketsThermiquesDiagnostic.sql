-- Diagnostic strictement en lecture, SQL Server 2014 et schema ERP actuel.
-- Executer uniquement sur une copie isolee, pas sur la production.
SET NOCOUNT ON;
IF DB_NAME() NOT LIKE N'%[_]THERMIQUE[_]TEST'
BEGIN
    RAISERROR(N'Copie suffixee _THERMIQUE_TEST obligatoire.',16,1);
    RETURN;
END;

SELECT DB_NAME() AS BaseTest, @@VERSION AS VersionSql;

SELECT TOP(100) f.FactureVenteId,f.NumeroFacture,f.Statut,f.MontantTotal,
    COUNT(p.PaiementId) AS NombreReglements,
    SUM(CASE WHEN p.DeviseMontants='FC' OR (ISNULL(p.DeviseMontants,'')='' AND p.Devise='FC') THEN p.Montant ELSE 0 END) AS MontantDocumenteFc,
    SUM(CASE WHEN p.PaiementId IS NOT NULL AND ISNULL(p.DeviseMontants,'')<>'FC' AND NOT (ISNULL(p.DeviseMontants,'')='' AND p.Devise='FC') THEN 1 ELSE 0 END) AS DevisesNonDocumentees
FROM dbo.FacturesVente f LEFT JOIN dbo.Paiements p ON p.FactureVenteId=f.FactureVenteId
GROUP BY f.FactureVenteId,f.NumeroFacture,f.Statut,f.MontantTotal
ORDER BY f.FactureVenteId DESC;

SELECT TOP(100) SessionId,UtilisateurId,RoleIdActif,RoleSession,ModeActif,Poste,DernierPing,Fin
FROM dbo.UtilisateurSessions ORDER BY SessionId DESC;

-- Indicateur de presence uniquement, pas une validation du contenu JSON.
SELECT TOP(20) AuditId,EntiteId,[Action],EffectueLe,
    CASE WHEN NouvellesValeurs LIKE N'%UnitePrincipaleImpression%' THEN 1 ELSE 0 END AS CleUnitePresente
FROM dbo.JournalAudit WHERE Entite='Facture' AND [Action] IN('FACTURE_CREEE','FACTURE_MODIFIEE')
ORDER BY AuditId DESC;

-- Ces unites sont ACTUELLES : cette jointure ne prouve jamais l'unite vendue.
SELECT TOP(100) l.FactureVenteId,l.LigneFactureVenteId,l.ProduitId,l.TypeVente,
    l.QuantiteSaisie,l.QuantiteBase,l.PrixUnitaire,l.MontantLigne,
    p.UnitePrincipale AS UnitePrincipaleActuelle,p.UniteSecondaire AS UniteSecondaireActuelle
FROM dbo.LignesFactureVente l LEFT JOIN dbo.Produits p ON p.ProduitId=l.ProduitId
ORDER BY l.LigneFactureVenteId DESC;
