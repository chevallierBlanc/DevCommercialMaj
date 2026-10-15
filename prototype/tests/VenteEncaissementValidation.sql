-- Diagnostic uniquement. Aucune modification des donnees commerciales.
SET NOCOUNT ON;
IF COL_LENGTH(N'dbo.UtilisateurRoles',N'VenteEncaissementAutorise') IS NULL
 OR COL_LENGTH(N'dbo.FacturesVente',N'OperationComptoirId') IS NULL
 OR COL_LENGTH(N'dbo.FacturesVente',N'OperationComptoirEmpreinte') IS NULL
 OR COL_LENGTH(N'dbo.FacturesVente',N'ValideeParUtilisateurId') IS NULL
 THROW 51000, 'Schema 2026101501 absent : appliquer la migration additive sur une copie avant validation.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.FacturesVente') AND name=N'UX_FacturesVente_OperationComptoir')
 THROW 51000, 'Index idempotence comptoir absent.', 1;
EXEC(N'
;WITH Droits AS (
 SELECT r.RoleId,r.NomRole,r.EstActif,
 COUNT(DISTINCT CASE WHEN i.CodeInterface IN(''FACTURIER'',''FACTURE_CREER'',''CAISSE'',''ENCAISSEMENT_CREER'',''MODE_COMBINE'') THEN i.CodeInterface END) AS NombreDroitsComptoir
 FROM dbo.Roles r LEFT JOIN dbo.RoleInterfaces ri ON ri.RoleId=r.RoleId
 LEFT JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId AND i.EstActif=1
 GROUP BY r.RoleId,r.NomRole,r.EstActif
)
SELECT u.UtilisateurId,u.NomUtilisateur,d.RoleId,d.NomRole,ur.EstRolePrincipal,
 u.EstActif AS CompteActif,u.EstVerrouille,ur.EstActif AS AssociationActive,
 d.NombreDroitsComptoir,ur.VenteEncaissementAutorise,p.GestionModesActive,p.ModeCombineAutorise,
 CASE WHEN u.EstActif=1 AND u.EstVerrouille=0 AND ur.EstActif=1 AND d.EstActif=1
 AND d.NombreDroitsComptoir=5 AND ur.VenteEncaissementAutorise=1 AND p.GestionModesActive=1 AND p.ModeCombineAutorise=1
 THEN N''AUTORISE A LA CONNEXION'' ELSE N''NON AUTORISE : verifier les conditions affichees'' END AS Diagnostic
FROM dbo.Utilisateurs u JOIN dbo.UtilisateurRoles ur ON ur.UtilisateurId=u.UtilisateurId
JOIN Droits d ON d.RoleId=ur.RoleId CROSS JOIN dbo.Parametres p ORDER BY u.UtilisateurId,d.RoleId;
SELECT TOP(100) f.FactureVenteId,f.NumeroFacture,f.Statut,f.MontantTotal,
 f.CreePar AS CreateurId,c.NomUtilisateur AS Createur,
 f.ValideeParUtilisateurId,v.NomUtilisateur AS Validateur,
 pa.PaiementId,pa.PayePar AS EncaisseurId,e.NomUtilisateur AS Encaisseur,
 pa.Montant,pa.MontantRecu,pa.MonnaieRendue,pa.Devise,pa.DeviseMontants,
 f.OperationComptoirId
FROM dbo.FacturesVente f LEFT JOIN dbo.Utilisateurs c ON c.UtilisateurId=f.CreePar
LEFT JOIN dbo.Utilisateurs v ON v.UtilisateurId=f.ValideeParUtilisateurId
LEFT JOIN dbo.Paiements pa ON pa.FactureVenteId=f.FactureVenteId
LEFT JOIN dbo.Utilisateurs e ON e.UtilisateurId=pa.PayePar ORDER BY f.FactureVenteId DESC;
IF EXISTS(SELECT 1 FROM dbo.FacturesVente f WHERE f.OperationComptoirId IS NOT NULL AND (f.Statut<>''PAYEE'' OR (SELECT COUNT(*) FROM dbo.Paiements p WHERE p.FactureVenteId=f.FactureVenteId)<>1))
 THROW 51000, ''Vente comptoir sans reglement unique complet.'', 1;
-- Une erreur UI ancienne peut avoir laisse un brouillon valide en base :
-- diagnostiquer et consulter, ne jamais supprimer automatiquement.
SELECT TOP(100) f.FactureVenteId,f.NumeroFacture,f.CreeLe,f.MontantTotal,
 (SELECT COUNT(*) FROM dbo.Paiements p WHERE p.FactureVenteId=f.FactureVenteId) AS Paiements,
 (SELECT COUNT(*) FROM dbo.StockSortie s WHERE s.RefSource=f.NumeroFacture AND s.Source=''VENTE'') AS Sorties
FROM dbo.FacturesVente f WHERE f.Statut=''EN_ATTENTE'' ORDER BY f.FactureVenteId DESC;
');
SELECT Version,Description FROM dbo.SchemaVersion WHERE Version IN(2026101301,2026101501);
PRINT N'VALIDATION_SCHEMA_VENTE_ENCAISSEMENT_OK';
-- Ce marqueur ne prouve pas le fonctionnement Windows ni les transactions reelles.
