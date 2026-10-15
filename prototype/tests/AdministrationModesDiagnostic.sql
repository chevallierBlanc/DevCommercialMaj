-- NON DESTRUCTIF. Verifier d'abord la base effectivement utilisee par WinForms.
SET NOCOUNT ON;
DECLARE @Administrateur nvarchar(128)=N'OneLove', @Compte nvarchar(128)=N'Tatiana';
SELECT DB_NAME() AS BaseInspectee,@@SERVERNAME AS Serveur;
SELECT Version,Description FROM dbo.SchemaVersion WHERE Version=2026101501;
SELECT InterfaceId,CodeInterface,Libelle,EstActif FROM dbo.InterfacesApplication
WHERE CodeInterface IN(N'ADMINISTRATION',N'SUPERADMIN_ROLES',N'UTILISATEUR_MODES');
SELECT u.UtilisateurId,u.NomUtilisateur,u.EstActif,u.EstVerrouille,
 r.RoleId,r.NomRole,ur.EstActif AS AssociationActive,ur.EstRolePrincipal,r.EstActif AS RoleActif,
 d.AccesAdministration,d.GestionRoles,d.GestionModes
FROM dbo.Utilisateurs u JOIN dbo.UtilisateurRoles ur ON ur.UtilisateurId=u.UtilisateurId
JOIN dbo.Roles r ON r.RoleId=ur.RoleId
OUTER APPLY(SELECT
 ISNULL(MAX(CASE WHEN i.CodeInterface=N'ADMINISTRATION' THEN 1 ELSE 0 END),0) AS AccesAdministration,
 ISNULL(MAX(CASE WHEN i.CodeInterface=N'SUPERADMIN_ROLES' THEN 1 ELSE 0 END),0) AS GestionRoles,
 ISNULL(MAX(CASE WHEN i.CodeInterface=N'UTILISATEUR_MODES' THEN 1 ELSE 0 END),0) AS GestionModes
 FROM dbo.RoleInterfaces ri JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId AND i.EstActif=1
 WHERE ri.RoleId=r.RoleId) d WHERE u.NomUtilisateur=@Administrateur;
-- Comparer SessionId/Poste a la session courante ; ne pas additionner les roles.
SELECT TOP(20) s.SessionId,s.UtilisateurId,s.RoleIdActif,r.NomRole AS RoleReel,
 s.RoleSession,s.ModeActif,s.Poste,s.DernierPing,s.Fin
FROM dbo.UtilisateurSessions s JOIN dbo.Utilisateurs u ON u.UtilisateurId=s.UtilisateurId
LEFT JOIN dbo.Roles r ON r.RoleId=s.RoleIdActif
WHERE u.NomUtilisateur=@Administrateur ORDER BY s.SessionId DESC;
IF COL_LENGTH(N'dbo.UtilisateurRoles',N'VenteEncaissementAutorise') IS NULL
 THROW 51000, 'Migration 2026101501 absente de CETTE base. Appliquer la migration additive avant attribution.', 1;
EXEC sys.sp_executesql N'
 SELECT u.UtilisateurId,u.NomUtilisateur,ur.RoleId,r.NomRole,ur.EstActif,ur.EstRolePrincipal,ur.VenteEncaissementAutorise
 FROM dbo.Utilisateurs u JOIN dbo.UtilisateurRoles ur ON ur.UtilisateurId=u.UtilisateurId
 JOIN dbo.Roles r ON r.RoleId=ur.RoleId WHERE u.NomUtilisateur=@nom ORDER BY ur.EstRolePrincipal DESC,r.NomRole;',
 N'@nom nvarchar(128)',@nom=@Compte;
-- Aucun INSERT/UPDATE/DELETE ni reparation automatique dans ce diagnostic.
