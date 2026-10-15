-- NON DESTRUCTIF. Sur la copie SQL Server utilisee par WinForms.
SET NOCOUNT ON;
DECLARE @Compte nvarchar(128)=N'Tatiana';
SELECT DB_NAME() AS BaseInspectee,@@SERVERNAME AS Serveur;
IF COL_LENGTH(N'dbo.FacturesVente',N'OperationComptoirId') IS NULL
 OR COL_LENGTH(N'dbo.FacturesVente',N'ValideeParUtilisateurId') IS NULL
 THROW 51000, 'Migration 2026101501 requise sur cette base.', 1;
EXEC sys.sp_executesql N'
 SELECT TOP(50) f.FactureVenteId,f.NumeroFacture,f.Statut,f.CreePar,
 f.ValideeParUtilisateurId,f.OperationComptoirId,
 pa.PaiementId,pa.PayePar,pa.Montant,pa.MontantRecu,pa.MonnaieRendue,pa.Devise,pa.DeviseMontants,
 CASE WHEN f.ValideeParUtilisateurId=u.UtilisateurId AND f.OperationComptoirId IS NOT NULL THEN 1 ELSE 0 END AS AncienFiltreDocument,
 CASE WHEN EXISTS(SELECT 1 FROM dbo.JournalAudit a WHERE a.Entite=''Facture'' AND a.EntiteId=CONVERT(nvarchar(30),f.FactureVenteId) AND a.[Action]=''ENCAISSEMENT_VALIDE'' AND a.ModeActif=''FACTURATION_ET_CAISSE'' AND a.EffectuePar=u.UtilisateurId AND a.Resultat=''SUCCES'') THEN 1 ELSE 0 END AS EncaissementCombineAudite
 FROM dbo.Utilisateurs u JOIN dbo.Paiements pa ON pa.PayePar=u.UtilisateurId
 JOIN dbo.FacturesVente f ON f.FactureVenteId=pa.FactureVenteId
 WHERE u.NomUtilisateur=@nom ORDER BY f.ValideLe DESC,f.FactureVenteId DESC;
 SELECT TOP(20) a.EffectueLe,a.[Action],a.ReferenceDocument,a.EntiteId,a.EffectuePar,a.RoleActif,a.ModeActif,a.SessionId,a.Poste,a.Resultat
 FROM dbo.JournalAudit a JOIN dbo.Utilisateurs u ON u.UtilisateurId=a.EffectuePar
 WHERE u.NomUtilisateur=@nom AND a.[Action] IN(''FACTURE_CREEE'',''FACTURE_MODIFIEE'',''ENCAISSEMENT_VALIDE'')
 ORDER BY a.EffectueLe DESC;', N'@nom nvarchar(128)',@nom=@Compte;
-- Un trigger client non present dans Git peut expliquer un champ non persiste.
-- Lire seulement ; ne desactiver aucune protection.
SELECT t.name,t.is_disabled,t.is_instead_of_trigger,
 OBJECT_NAME(t.parent_id) AS TableConcernee,OBJECT_DEFINITION(t.object_id) AS Definition
FROM sys.triggers t WHERE t.parent_id IN(OBJECT_ID(N'dbo.FacturesVente'),OBJECT_ID(N'dbo.Paiements'));
