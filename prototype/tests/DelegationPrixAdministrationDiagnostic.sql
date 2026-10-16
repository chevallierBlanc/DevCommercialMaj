-- Diagnostic NON DESTRUCTIF. Choisir la copie de la base utilisee par WinForms.
SET NOCOUNT ON;
DECLARE @Compte nvarchar(128)=N'Tatiana', @Role nvarchar(128)=N'FACTURIER';
SELECT @@SERVERNAME AS Serveur, DB_NAME() AS BaseInspectee;
SELECT Version, Description FROM dbo.SchemaVersion
WHERE Version IN(2026100904,2026101301,2026101501) ORDER BY Version;

IF COL_LENGTH(N'dbo.Parametres', N'PrixDemandesActives') IS NULL
 OR COL_LENGTH(N'dbo.DemandesModificationPrix', N'Perimetre') IS NULL
 OR COL_LENGTH(N'dbo.DemandesModificationPrix', N'TypeTarifId') IS NULL
 THROW 51000, 'Schema de delegation incomplet : verifier les migrations existantes avant les essais.', 1;

EXEC sys.sp_executesql N'
SELECT DelegationPrixActive,PrixDemandesActives,PrixModificationImmediate,
 PrixSousCoutAutorise,PrixVariationMaxPourcent,PrixMargeMinimumPourcent,PrixDemandeDureeHeures
FROM dbo.Parametres;
SELECT u.UtilisateurId,u.NomUtilisateur,u.EstActif,u.EstVerrouille,
 r.RoleId,r.NomRole,ur.EstActif AS AssociationActive,ur.EstRolePrincipal
FROM dbo.Utilisateurs u JOIN dbo.UtilisateurRoles ur ON ur.UtilisateurId=u.UtilisateurId
 JOIN dbo.Roles r ON r.RoleId=ur.RoleId
WHERE u.NomUtilisateur=@nom AND r.NomRole=@role;
SELECT r.RoleId,r.NomRole,i.CodeInterface,i.Libelle,i.EstActif,
 CASE WHEN ri.InterfaceId IS NULL THEN 0 ELSE 1 END AS AttribueAuRole
FROM dbo.Roles r CROSS JOIN dbo.InterfacesApplication i
 LEFT JOIN dbo.RoleInterfaces ri ON ri.RoleId=r.RoleId AND ri.InterfaceId=i.InterfaceId
WHERE r.NomRole=@role AND i.CodeInterface IN(''FACTURIER'',''PRIX_CONSULTER'',
 ''PRIX_DEMANDER'',''PRIX_TARIF_MODIFIER'',''PRIX_DEMANDER_FACTURE'',
 ''PRIX_FACTURE_EXCEPTION'',''PRIX_APPROUVER'',''PRIX_REFUSER'',''PRIX_SOUS_COUT'')
ORDER BY i.CodeInterface;
SELECT TOP(10) s.SessionId,s.RoleIdActif,s.ModeActif,s.Poste,s.Fin,s.DernierPing
FROM dbo.UtilisateurSessions s JOIN dbo.Utilisateurs u ON u.UtilisateurId=s.UtilisateurId
WHERE u.NomUtilisateur=@nom ORDER BY s.SessionId DESC;
SELECT TOP(30) d.DemandeId,d.ProduitId,d.TypeVente,d.Perimetre,d.TypeTarifId,
 d.AncienPrix,d.NouveauPrix,d.Etat,d.DemandePar,d.DecisionPar,d.DemandeLe,d.ExpireLe
FROM dbo.DemandesModificationPrix d JOIN dbo.Utilisateurs u ON u.UtilisateurId=d.DemandePar
WHERE u.NomUtilisateur=@nom ORDER BY d.DemandeId DESC;',
N'@nom nvarchar(128),@role nvarchar(128)', @nom=@Compte,@role=@Role;
-- Aucun INSERT/UPDATE/DELETE ni attribution implicite de permission.
