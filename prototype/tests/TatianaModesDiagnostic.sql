-- Diagnostic NON DESTRUCTIF. Executer sur la copie de la base utilisee par WinForms.
SET NOCOUNT ON;
DECLARE @Compte nvarchar(128)=N'Tatiana'; -- Adapter au vrai nom de connexion.
SELECT DB_NAME() AS BaseInspectee,@@SERVERNAME AS Serveur,@Compte AS CompteRecherche;
SELECT Version,Description FROM dbo.SchemaVersion WHERE Version=2026101501;
SELECT COL_LENGTH(N'dbo.UtilisateurRoles',N'VenteEncaissementAutorise') AS ColonneAttribution;
IF COL_LENGTH(N'dbo.UtilisateurRoles',N'VenteEncaissementAutorise') IS NULL
 THROW 51000, 'Migration 2026101501 absente de CETTE base. Ne pas attribuer de modes par script : appliquer la migration additive puis utiliser Administration Utilisateurs.', 1;
IF (SELECT COUNT(*) FROM dbo.Parametres)<>1
 THROW 51000, 'Configuration entreprise absente ou multiple.', 1;
IF NOT EXISTS(SELECT 1 FROM dbo.Utilisateurs WHERE NomUtilisateur=@Compte)
 THROW 51000, 'Nom de connexion introuvable dans cette base. Verifier le nom et la connexion de WinForms.', 1;
-- Compilation separee : le diagnostic reste explicite si la colonne manque.
EXEC sys.sp_executesql N'
SELECT u.UtilisateurId,u.NomUtilisateur,ur.RoleId,r.NomRole,ur.EstRolePrincipal,
 u.EstActif AS CompteActif,u.EstVerrouille,ur.EstActif AS AssociationActive,r.EstActif AS RoleActif,
 p.GestionModesActive,p.ModeFacturationAutorise,p.ModeCaisseAutorise,p.ModeCombineAutorise,
 ur.VenteEncaissementAutorise,
 d.DroitsComptoir,
 CASE WHEN ur.VenteEncaissementAutorise=0 THEN N''Attribution compte-role absente : enregistrer dans Modes par role''
 WHEN p.GestionModesActive=0 OR p.ModeCombineAutorise=0 THEN N''Mode global desactive''
 WHEN u.EstActif=0 OR u.EstVerrouille=1 OR ur.EstActif=0 OR r.EstActif=0 THEN N''Compte ou association indisponible''
 WHEN d.DroitsComptoir<>5 THEN N''Droits incomplets pour CE role''
 ELSE N''Eligible a la case au prochain login ; session encore a verifier'' END AS Diagnostic
FROM dbo.Utilisateurs u JOIN dbo.UtilisateurRoles ur ON ur.UtilisateurId=u.UtilisateurId
JOIN dbo.Roles r ON r.RoleId=ur.RoleId CROSS JOIN dbo.Parametres p
OUTER APPLY(SELECT COUNT(DISTINCT i.CodeInterface) AS DroitsComptoir FROM dbo.RoleInterfaces ri
 JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId AND i.EstActif=1
 WHERE ri.RoleId=ur.RoleId AND i.CodeInterface IN(''FACTURIER'',''FACTURE_CREER'',''CAISSE'',''ENCAISSEMENT_CREER'',''MODE_COMBINE'')) d
WHERE u.NomUtilisateur=@nom ORDER BY ur.EstRolePrincipal DESC,r.NomRole;
SELECT r.RoleId,r.NomRole,i.CodeInterface,i.Libelle,i.EstActif
FROM dbo.Utilisateurs u JOIN dbo.UtilisateurRoles ur ON ur.UtilisateurId=u.UtilisateurId
JOIN dbo.Roles r ON r.RoleId=ur.RoleId JOIN dbo.RoleInterfaces ri ON ri.RoleId=r.RoleId
JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId
WHERE u.NomUtilisateur=@nom AND i.CodeInterface IN(''FACTURIER'',''FACTURE_CREER'',''CAISSE'',''ENCAISSEMENT_CREER'',''MODE_COMBINE'')
ORDER BY r.RoleId,i.CodeInterface;',N'@nom nvarchar(128)',@nom=@Compte;
-- Roles habilites a administrer les attributions : ces deux droits sont requis.
SELECT r.RoleId,r.NomRole,
 MAX(CASE WHEN i.CodeInterface=N'ADMINISTRATION' THEN 1 ELSE 0 END) AS AccesAdministration,
 MAX(CASE WHEN i.CodeInterface=N'UTILISATEUR_MODES' THEN 1 ELSE 0 END) AS AttributionModes
FROM dbo.Roles r LEFT JOIN dbo.RoleInterfaces ri ON ri.RoleId=r.RoleId
LEFT JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId AND i.EstActif=1
WHERE r.EstActif=1 GROUP BY r.RoleId,r.NomRole ORDER BY r.NomRole;
-- Aucun UPDATE, aucun grant et aucune ouverture de session dans ce script.
