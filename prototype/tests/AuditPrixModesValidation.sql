-- Diagnostic strictement non destructif, a lancer sur la copie apres migration.
SET NOCOUNT ON;
DECLARE @Colonnes table(TableNom sysname,Colonne sysname);
INSERT @Colonnes VALUES
(N'Paiements',N'MontantRecuOrigine'),(N'Paiements',N'TauxConversionApplique'),(N'Paiements',N'DeviseMontants'),
(N'Parametres',N'GestionModesActive'),(N'Parametres',N'ModeFacturationAutorise'),(N'Parametres',N'ModeCaisseAutorise'),(N'Parametres',N'ModeCombineAutorise'),
(N'Parametres',N'DelegationPrixActive'),(N'Parametres',N'PrixDemandesActives'),(N'Parametres',N'PrixModificationImmediate'),
(N'Parametres',N'PrixSousCoutAutorise'),(N'Parametres',N'PrixVariationMaxPourcent'),(N'Parametres',N'PrixMargeMinimumPourcent'),
(N'LignesFactureVente',N'MotifPrixException'),(N'UtilisateurSessions',N'ModeActif');
SELECT TableNom,Colonne AS ColonneManquante FROM @Colonnes WHERE COL_LENGTH(N'dbo.'+TableNom,Colonne) IS NULL;
IF EXISTS(SELECT 1 FROM @Colonnes WHERE COL_LENGTH(N'dbo.'+TableNom,Colonne) IS NULL)
    THROW 51000, 'Schema incomplet : appliquer les migrations 2026100902, 0903, 0904 apres la phase 1.', 1;
IF OBJECT_ID(N'dbo.DemandesModificationPrix',N'U') IS NULL
    THROW 51000, 'DemandesModificationPrix absente.', 1;
IF (SELECT COUNT(*) FROM dbo.SchemaVersion WHERE Version IN(2026100902,2026100903,2026100904))<>3
    THROW 51000, 'Versions de migration incompletes.', 1;
SELECT Version,Description,DateApplication FROM dbo.SchemaVersion WHERE Version BETWEEN 2026100901 AND 2026100904 ORDER BY Version;
SELECT r.NomRole,i.CodeInterface,i.Libelle
FROM dbo.RoleInterfaces ri JOIN dbo.Roles r ON r.RoleId=ri.RoleId JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId
WHERE i.CodeInterface LIKE N'PRIX[_]%' OR i.CodeInterface LIKE N'MODE[_]%' OR i.CodeInterface IN(N'PARAMETRES_PRIX',N'PARAMETRES_SECURITE') ORDER BY r.NomRole,i.CodeInterface;
-- SQL dynamique apres le controle : une ancienne base produit un diagnostic
-- explicite, pas une erreur de resolution de colonnes avant le IF.
EXEC(N'SELECT GestionModesActive,ModeFacturationAutorise,ModeCaisseAutorise,ModeCombineAutorise,DelegationPrixActive,PrixDemandesActives,PrixModificationImmediate,PrixSousCoutAutorise,PrixVariationMaxPourcent,PrixMargeMinimumPourcent FROM dbo.Parametres;
SELECT PaiementId,FactureVenteId,Montant,MontantRecu,MonnaieRendue,Devise,DeviseMontants,MontantRecuOrigine,TauxConversionApplique FROM dbo.Paiements WHERE DeviseMontants IS NOT NULL ORDER BY PaiementId DESC;
IF EXISTS(SELECT 1 FROM dbo.Paiements WHERE DeviseMontants=''FC'' AND (MontantRecu IS NULL OR MonnaieRendue IS NULL OR MontantRecu-MonnaieRendue<>Montant OR MontantRecu<Montant OR MonnaieRendue<0 OR (MontantRecuOrigine IS NULL AND TauxConversionApplique IS NOT NULL) OR (MontantRecuOrigine IS NOT NULL AND TauxConversionApplique IS NULL) OR (MontantRecuOrigine IS NOT NULL AND (TauxConversionApplique<=0 OR MontantRecuOrigine*TauxConversionApplique<>MontantRecu))))
    THROW 51000, ''Nouveau paiement incoherent : montant recu, net, rendu ou taux.'', 1;
IF EXISTS(SELECT 1 FROM dbo.DemandesModificationPrix WHERE NouveauPrix<=0 OR AncienPrix<=0 OR LEN(LTRIM(RTRIM(Motif)))=0 OR ExpireLe<=DemandeLe)
    THROW 51000, ''Demande de prix incoherente : prix, motif ou expiration.'', 1;
SELECT Etat,COUNT_BIG(*) AS NombreDemandes FROM dbo.DemandesModificationPrix GROUP BY Etat;
SELECT s.SessionId,u.NomUtilisateur,r.NomRole,s.ModeActif,s.Debut,s.Poste FROM dbo.UtilisateurSessions s JOIN dbo.Utilisateurs u ON u.UtilisateurId=s.UtilisateurId LEFT JOIN dbo.Roles r ON r.RoleId=s.RoleIdActif WHERE s.Fin IS NULL;
SELECT TOP(40) ReferenceDocument,[Action],UtilisateurNom,RoleActif,ModeActif,SessionId,Poste,EffectueLe,Resultat,Motif,AnciennesValeurs,NouvellesValeurs FROM dbo.JournalAudit WHERE Provenance=''AUDIT_METIER'' ORDER BY AuditId DESC;');
PRINT N'VALIDATION_SCHEMA_AUDIT_PRIX_MODES_OK';
-- Ce marqueur confirme le schema et les controles disponibles, pas les
-- scenarios de concurrence, rollback ni l'ergonomie WinForms.
