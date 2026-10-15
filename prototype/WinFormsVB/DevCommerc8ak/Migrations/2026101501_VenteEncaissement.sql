SET XACT_ABORT ON;
IF OBJECT_ID(N'dbo.UtilisateurRoles',N'U') IS NULL OR OBJECT_ID(N'dbo.FacturesVente',N'U') IS NULL OR OBJECT_ID(N'dbo.InterfacesApplication',N'U') IS NULL OR OBJECT_ID(N'dbo.SchemaVersion',N'U') IS NULL
 THROW 51000, 'Prerequis du schema ERP (jusqu a 2026101301) absents. Cette migration ne cree pas une base commerciale vide.', 1;
DECLARE @Own bit=CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;
IF @Own=1 BEGIN TRANSACTION;
BEGIN TRY
 IF COL_LENGTH(N'dbo.UtilisateurRoles',N'VenteEncaissementAutorise') IS NULL
  ALTER TABLE dbo.UtilisateurRoles ADD VenteEncaissementAutorise bit NOT NULL CONSTRAINT DF_UtilisateurRoles_Comptoir DEFAULT(0);
 IF COL_LENGTH(N'dbo.FacturesVente',N'OperationComptoirId') IS NULL
  ALTER TABLE dbo.FacturesVente ADD OperationComptoirId uniqueidentifier NULL;
 IF COL_LENGTH(N'dbo.FacturesVente',N'OperationComptoirEmpreinte') IS NULL
  ALTER TABLE dbo.FacturesVente ADD OperationComptoirEmpreinte nvarchar(64) NULL;
 IF COL_LENGTH(N'dbo.FacturesVente',N'ValideeParUtilisateurId') IS NULL
  ALTER TABLE dbo.FacturesVente ADD ValideeParUtilisateurId int NULL;
 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.FacturesVente') AND name=N'UX_FacturesVente_OperationComptoir')
  EXEC(N'CREATE UNIQUE INDEX UX_FacturesVente_OperationComptoir ON dbo.FacturesVente(OperationComptoirId) WHERE OperationComptoirId IS NOT NULL');
 IF NOT EXISTS(SELECT 1 FROM dbo.InterfacesApplication WHERE CodeInterface=N'UTILISATEUR_MODES')
  INSERT dbo.InterfacesApplication(CodeInterface,Libelle,EstTechnique,EstActif) VALUES(N'UTILISATEUR_MODES',N'Utilisateurs : attribuer Vente et Encaissement par rôle',0,1);
 -- Aucun grant, aucun rattachement retroactif des anciens auteurs/paiements.
 IF @Own=1 BEGIN
  IF NOT EXISTS(SELECT 1 FROM dbo.SchemaVersion WHERE Version=2026101501)
   INSERT dbo.SchemaVersion(Version,Description) VALUES(2026101501,N'Attribution individuelle et vente comptoir atomique');
  COMMIT;
 END;
END TRY
BEGIN CATCH
 IF @Own=1 AND @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;
