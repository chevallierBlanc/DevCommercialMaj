Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient

Namespace DevCommerc8ak
    Public NotInheritable Class SchemaMigrationService
        Private Sub New()
        End Sub

        Public Shared Sub ApplyPendingMigrations()
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Using cn As New SqlConnection(cs)
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        AssurerTableVersion(cn, tx)
                        AppliquerMigration(cn, tx, 2026080801, "Colonnes stock mesure et type vente mesure", AddressOf MigrationStockMesure)
                        AppliquerMigration(cn, tx, 2026080802, "Index production principaux", AddressOf MigrationIndexProduction)
                        AppliquerMigration(cn, tx, 2026080803, "Precision quantite types vente produit", AddressOf MigrationPrecisionTypesVente)
                        AppliquerMigration(cn, tx, 2026080804, "Index production dates et audit", AddressOf MigrationIndexDatesEtAudit)
                        AppliquerMigration(cn, tx, 2026080805, "Reparation identite audit actions", AddressOf MigrationAuditActionsIdentity)
                        AppliquerMigration(cn, tx, 2026080806, "Sequences metier multiposte", AddressOf MigrationBusinessSequences)
                        AppliquerMigration(cn, tx, 2026090801, "Sessions initialisation ventes", AddressOf MigrationInitialisationVentes)
                        AppliquerMigration(cn, tx, 2026090802, "Sessions stock initial technique", AddressOf MigrationStockInitialTechnique)
                        tx.Commit()
                    Catch
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        Private Shared Sub AssurerTableVersion(cn As SqlConnection, tx As SqlTransaction)
            Executer(cn, tx,
                "IF OBJECT_ID('dbo.SchemaVersion', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.SchemaVersion (" &
                "Version INT NOT NULL PRIMARY KEY, " &
                "DateApplication DATETIME2 NOT NULL CONSTRAINT DF_SchemaVersion_Date DEFAULT(SYSDATETIME()), " &
                "Description NVARCHAR(255) NOT NULL) " &
                "END")
        End Sub

        Private Shared Sub AppliquerMigration(cn As SqlConnection, tx As SqlTransaction, version As Integer, description As String, action As Action(Of SqlConnection, SqlTransaction))
            Using cmd As New SqlCommand("SELECT COUNT(1) FROM dbo.SchemaVersion WITH (UPDLOCK, HOLDLOCK) WHERE Version=@Version", cn, tx)
                cmd.Parameters.AddWithValue("@Version", version)
                Dim existe As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                If existe > 0 Then Return
            End Using

            action(cn, tx)

            Using cmd As New SqlCommand("INSERT INTO dbo.SchemaVersion (Version, Description) VALUES (@Version, @Description)", cn, tx)
                cmd.Parameters.AddWithValue("@Version", version)
                cmd.Parameters.AddWithValue("@Description", description)
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Shared Sub MigrationStockMesure(cn As SqlConnection, tx As SqlTransaction)
            Executer(cn, tx, "IF COL_LENGTH('dbo.Produits', 'TypeGestionStock') IS NULL ALTER TABLE dbo.Produits ADD TypeGestionStock NVARCHAR(20) NULL")
            Executer(cn, tx, "IF COL_LENGTH('dbo.Produits', 'UniteMesureStock') IS NULL ALTER TABLE dbo.Produits ADD UniteMesureStock NVARCHAR(20) NULL")
            Executer(cn, tx, "IF COL_LENGTH('dbo.Produits', 'ContenuUnitePrincipale') IS NULL ALTER TABLE dbo.Produits ADD ContenuUnitePrincipale DECIMAL(18,4) NULL")
            Executer(cn, tx, "IF COL_LENGTH('dbo.Produits', 'ContenuUniteSecondaire') IS NULL ALTER TABLE dbo.Produits ADD ContenuUniteSecondaire DECIMAL(18,4) NULL")
            Executer(cn, tx, "IF OBJECT_ID('dbo.TypesVenteProduit', 'U') IS NOT NULL AND COL_LENGTH('dbo.TypesVenteProduit', 'TypeUniteEquivalent') IS NULL ALTER TABLE dbo.TypesVenteProduit ADD TypeUniteEquivalent NVARCHAR(20) NULL")
            Executer(cn, tx, "IF OBJECT_ID('dbo.TypesVenteProduit', 'U') IS NOT NULL AND COL_LENGTH('dbo.TypesVenteProduit', 'TypeQuantiteEquivalent') IS NULL ALTER TABLE dbo.TypesVenteProduit ADD TypeQuantiteEquivalent NVARCHAR(20) NULL")
            Executer(cn, tx, "IF OBJECT_ID('dbo.TypesVenteProduit', 'U') IS NOT NULL UPDATE dbo.TypesVenteProduit SET TypeQuantiteEquivalent = ISNULL(NULLIF(TypeQuantiteEquivalent, ''), ISNULL(NULLIF(TypeUniteEquivalent, ''), 'SECONDAIRE')) WHERE TypeQuantiteEquivalent IS NULL OR LTRIM(RTRIM(TypeQuantiteEquivalent)) = ''")
        End Sub

        Private Shared Sub MigrationIndexProduction(cn As SqlConnection, tx As SqlTransaction)
            Dim indexSql As New List(Of String) From {
                "IF OBJECT_ID('dbo.Produits', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Produits_CodeBarres' AND object_id=OBJECT_ID('dbo.Produits')) CREATE INDEX IX_Produits_CodeBarres ON dbo.Produits(CodeBarres)",
                "IF OBJECT_ID('dbo.Produits', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Produits_Libelle' AND object_id=OBJECT_ID('dbo.Produits')) CREATE INDEX IX_Produits_Libelle ON dbo.Produits(Libelle)",
                "IF OBJECT_ID('dbo.Produits', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Produits_CategorieActif' AND object_id=OBJECT_ID('dbo.Produits')) CREATE INDEX IX_Produits_CategorieActif ON dbo.Produits(CategorieId, EstActif)",
                "IF OBJECT_ID('dbo.FacturesVente', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_FacturesVente_Statut_CreeLe' AND object_id=OBJECT_ID('dbo.FacturesVente')) CREATE INDEX IX_FacturesVente_Statut_CreeLe ON dbo.FacturesVente(Statut, CreeLe)",
                "IF OBJECT_ID('dbo.FacturesVente', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_FacturesVente_NumeroFacture' AND object_id=OBJECT_ID('dbo.FacturesVente')) CREATE INDEX IX_FacturesVente_NumeroFacture ON dbo.FacturesVente(NumeroFacture)",
                "IF OBJECT_ID('dbo.LignesFactureVente', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_LignesFactureVente_Facture' AND object_id=OBJECT_ID('dbo.LignesFactureVente')) CREATE INDEX IX_LignesFactureVente_Facture ON dbo.LignesFactureVente(FactureVenteId)",
                "IF OBJECT_ID('dbo.LignesFactureVente', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_LignesFactureVente_Produit' AND object_id=OBJECT_ID('dbo.LignesFactureVente')) CREATE INDEX IX_LignesFactureVente_Produit ON dbo.LignesFactureVente(ProduitId)",
                "IF OBJECT_ID('dbo.Paiements', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Paiements_Facture_PayeLe' AND object_id=OBJECT_ID('dbo.Paiements')) CREATE INDEX IX_Paiements_Facture_PayeLe ON dbo.Paiements(FactureVenteId, PayeLe)",
                "IF OBJECT_ID('dbo.StockEntree', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_StockEntree_Produit_Date' AND object_id=OBJECT_ID('dbo.StockEntree')) CREATE INDEX IX_StockEntree_Produit_Date ON dbo.StockEntree(ProduitId, DateEntree)",
                "IF OBJECT_ID('dbo.StockSortie', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_StockSortie_Produit_Date' AND object_id=OBJECT_ID('dbo.StockSortie')) CREATE INDEX IX_StockSortie_Produit_Date ON dbo.StockSortie(ProduitId, DateSortie)",
                "IF OBJECT_ID('dbo.TypesVenteProduit', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TypesVenteProduit_Produit_Actif' AND object_id=OBJECT_ID('dbo.TypesVenteProduit')) CREATE INDEX IX_TypesVenteProduit_Produit_Actif ON dbo.TypesVenteProduit(ProduitId, Actif)"
            }

            For Each sql As String In indexSql
                Executer(cn, tx, sql)
            Next
        End Sub

        Private Shared Sub MigrationPrecisionTypesVente(cn As SqlConnection, tx As SqlTransaction)
            Executer(cn, tx, "IF OBJECT_ID('dbo.TypesVenteProduit', 'U') IS NOT NULL ALTER TABLE dbo.TypesVenteProduit ALTER COLUMN QuantiteEquivalent DECIMAL(18,4) NOT NULL")
        End Sub

        Private Shared Sub MigrationIndexDatesEtAudit(cn As SqlConnection, tx As SqlTransaction)
            Dim indexSql As New List(Of String) From {
                "IF OBJECT_ID('dbo.Depenses', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Depenses_Date_Source' AND object_id=OBJECT_ID('dbo.Depenses')) CREATE INDEX IX_Depenses_Date_Source ON dbo.Depenses(DateDepense, Source)",
                "IF OBJECT_ID('dbo.Paiements', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Paiements_PayeLe' AND object_id=OBJECT_ID('dbo.Paiements')) CREATE INDEX IX_Paiements_PayeLe ON dbo.Paiements(PayeLe)",
                "IF OBJECT_ID('dbo.StockPerte', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_StockPerte_Produit_Date' AND object_id=OBJECT_ID('dbo.StockPerte')) CREATE INDEX IX_StockPerte_Produit_Date ON dbo.StockPerte(ProduitId, DatePerte)",
                "IF OBJECT_ID('dbo.MouvementsStock', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_MouvementsStock_Produit_Date' AND object_id=OBJECT_ID('dbo.MouvementsStock')) CREATE INDEX IX_MouvementsStock_Produit_Date ON dbo.MouvementsStock(ProduitId, EffectueLe)",
                "IF OBJECT_ID('dbo.Inventaires', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Inventaires_DateCreation' AND object_id=OBJECT_ID('dbo.Inventaires')) CREATE INDEX IX_Inventaires_DateCreation ON dbo.Inventaires(DateCreation)",
                "IF OBJECT_ID('dbo.AuditActions', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_AuditActions_CreeLe' AND object_id=OBJECT_ID('dbo.AuditActions')) CREATE INDEX IX_AuditActions_CreeLe ON dbo.AuditActions(CreeLe)",
                "IF OBJECT_ID('dbo.AuditActions', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_AuditActions_Utilisateur_CreeLe' AND object_id=OBJECT_ID('dbo.AuditActions')) CREATE INDEX IX_AuditActions_Utilisateur_CreeLe ON dbo.AuditActions(Utilisateur, CreeLe)"
            }

            For Each sql As String In indexSql
                Executer(cn, tx, sql)
            Next
        End Sub

        Private Shared Sub MigrationAuditActionsIdentity(cn As SqlConnection, tx As SqlTransaction)
            Executer(cn, tx,
                "IF OBJECT_ID('dbo.AuditActions', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.AuditActions (" &
                "AuditActionId BIGINT IDENTITY(1,1) PRIMARY KEY, " &
                "Utilisateur NVARCHAR(80) NULL, " &
                "[Role] NVARCHAR(50) NULL, " &
                "Module NVARCHAR(80) NULL, " &
                "[Action] NVARCHAR(100) NULL, " &
                "[Description] NVARCHAR(255) NULL, " &
                "Machine NVARCHAR(100) NULL, " &
                "[Statut] NVARCHAR(30) NULL, " &
                "CreeLe DATETIME2 NOT NULL CONSTRAINT DF_AuditActions_CreeLe DEFAULT(GETDATE())) " &
                "END")

            Executer(cn, tx,
                "IF OBJECT_ID('dbo.AuditActions', 'U') IS NOT NULL " &
                "AND COL_LENGTH('dbo.AuditActions', 'AuditActionId') IS NOT NULL " &
                "AND COLUMNPROPERTY(OBJECT_ID('dbo.AuditActions'), 'AuditActionId', 'IsIdentity') = 0 " &
                "BEGIN " &
                "IF OBJECT_ID('dbo.AuditActions_Rebuild', 'U') IS NOT NULL DROP TABLE dbo.AuditActions_Rebuild; " &
                "CREATE TABLE dbo.AuditActions_Rebuild (" &
                "AuditActionId BIGINT IDENTITY(1,1) PRIMARY KEY, " &
                "Utilisateur NVARCHAR(80) NULL, " &
                "[Role] NVARCHAR(50) NULL, " &
                "Module NVARCHAR(80) NULL, " &
                "[Action] NVARCHAR(100) NULL, " &
                "[Description] NVARCHAR(255) NULL, " &
                "Machine NVARCHAR(100) NULL, " &
                "[Statut] NVARCHAR(30) NULL, " &
                "CreeLe DATETIME2 NOT NULL CONSTRAINT DF_AuditActions_Rebuild_CreeLe DEFAULT(GETDATE())); " &
                "SET IDENTITY_INSERT dbo.AuditActions_Rebuild ON; " &
                "INSERT INTO dbo.AuditActions_Rebuild (AuditActionId, Utilisateur, [Role], Module, [Action], [Description], Machine, [Statut], CreeLe) " &
                "SELECT AuditActionId, Utilisateur, [Role], Module, [Action], [Description], Machine, [Statut], CreeLe " &
                "FROM dbo.AuditActions WITH (HOLDLOCK); " &
                "SET IDENTITY_INSERT dbo.AuditActions_Rebuild OFF; " &
                "IF (SELECT COUNT(1) FROM dbo.AuditActions_Rebuild) <> (SELECT COUNT(1) FROM dbo.AuditActions) " &
                "BEGIN RAISERROR('Reconstruction AuditActions interrompue : nombre de lignes incoherent.', 16, 1); RETURN; END " &
                "DROP TABLE dbo.AuditActions; " &
                "EXEC sp_rename 'dbo.AuditActions_Rebuild', 'AuditActions'; " &
                "END")

            Executer(cn, tx,
                "IF OBJECT_ID('dbo.AuditActions', 'U') IS NOT NULL " &
                "AND COLUMNPROPERTY(OBJECT_ID('dbo.AuditActions'), 'AuditActionId', 'IsIdentity') = 1 " &
                "BEGIN " &
                "DECLARE @MaxAuditActionId BIGINT; " &
                "SELECT @MaxAuditActionId = ISNULL(MAX(AuditActionId), 0) FROM dbo.AuditActions WITH (HOLDLOCK); " &
                "DECLARE @SqlCheckIdent NVARCHAR(400); " &
                "SET @SqlCheckIdent = N'DBCC CHECKIDENT (''dbo.AuditActions'', RESEED, ' + CAST(@MaxAuditActionId AS NVARCHAR(30)) + N') WITH NO_INFOMSGS'; " &
                "EXEC (@SqlCheckIdent); " &
                "END")

            Executer(cn, tx,
                "IF OBJECT_ID('dbo.AuditActions', 'U') IS NOT NULL " &
                "AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_AuditActions_CreeLe' AND object_id=OBJECT_ID('dbo.AuditActions')) " &
                "CREATE INDEX IX_AuditActions_CreeLe ON dbo.AuditActions(CreeLe)")

            Executer(cn, tx,
                "IF OBJECT_ID('dbo.AuditActions', 'U') IS NOT NULL " &
                "AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_AuditActions_Utilisateur_CreeLe' AND object_id=OBJECT_ID('dbo.AuditActions')) " &
                "CREATE INDEX IX_AuditActions_Utilisateur_CreeLe ON dbo.AuditActions(Utilisateur, CreeLe)")
        End Sub

        Private Shared Sub MigrationBusinessSequences(cn As SqlConnection, tx As SqlTransaction)
            Executer(cn, tx,
                "IF OBJECT_ID('dbo.BusinessSequences', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.BusinessSequences (" &
                "SequenceKey NVARCHAR(120) NOT NULL PRIMARY KEY, " &
                "Prefix NVARCHAR(30) NOT NULL, " &
                "Periode NVARCHAR(20) NOT NULL, " &
                "DernierNumero INT NOT NULL CONSTRAINT DF_BusinessSequences_DernierNumero DEFAULT(0), " &
                "CreeLe DATETIME2 NOT NULL CONSTRAINT DF_BusinessSequences_CreeLe DEFAULT(SYSDATETIME()), " &
                "ModifieLe DATETIME2 NOT NULL CONSTRAINT DF_BusinessSequences_ModifieLe DEFAULT(SYSDATETIME())) " &
                "END")

            Executer(cn, tx,
                "IF OBJECT_ID('dbo.BusinessSequences', 'U') IS NOT NULL " &
                "AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_BusinessSequences_Prefix_Periode' AND object_id=OBJECT_ID('dbo.BusinessSequences')) " &
                "CREATE INDEX IX_BusinessSequences_Prefix_Periode ON dbo.BusinessSequences(Prefix, Periode)")
        End Sub

        Private Shared Sub MigrationInitialisationVentes(cn As SqlConnection, tx As SqlTransaction)
            Executer(cn, tx,
                "IF OBJECT_ID('dbo.InitialisationVenteSessions', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.InitialisationVenteSessions (" &
                "InitialisationVenteSessionId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, " &
                "ReferenceSession NVARCHAR(50) NOT NULL UNIQUE, " &
                "DateDebut DATE NOT NULL, " &
                "DateFin DATE NOT NULL, " &
                "ModeStock NVARCHAR(30) NOT NULL CONSTRAINT DF_InitVenteSessions_ModeStock DEFAULT('HISTORIQUE_UNIQUEMENT'), " &
                "Observation NVARCHAR(500) NULL, " &
                "Statut NVARCHAR(20) NOT NULL CONSTRAINT DF_InitVenteSessions_Statut DEFAULT('BROUILLON'), " &
                "CreeLe DATETIME2 NOT NULL CONSTRAINT DF_InitVenteSessions_CreeLe DEFAULT(SYSDATETIME()), " &
                "CreePar INT NOT NULL, " &
                "Machine NVARCHAR(100) NULL, " &
                "ValideeLe DATETIME2 NULL, " &
                "ValideePar INT NULL, " &
                "AnnuleeLe DATETIME2 NULL, " &
                "AnnuleePar INT NULL) " &
                "END")

            Executer(cn, tx,
                "IF OBJECT_ID('dbo.InitialisationVenteLignes', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.InitialisationVenteLignes (" &
                "InitialisationVenteLigneId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, " &
                "InitialisationVenteSessionId INT NOT NULL, " &
                "DateVente DATETIME2 NOT NULL, " &
                "ProduitId INT NOT NULL, " &
                "CodeProduit NVARCHAR(80) NULL, " &
                "LibelleProduit NVARCHAR(200) NOT NULL, " &
                "Categorie NVARCHAR(150) NULL, " &
                "TypeVente NVARCHAR(80) NOT NULL, " &
                "UniteCommerciale NVARCHAR(80) NULL, " &
                "QuantiteCommerciale DECIMAL(18,4) NOT NULL, " &
                "PrixUnitaire DECIMAL(18,2) NOT NULL, " &
                "MontantLigne DECIMAL(18,2) NOT NULL, " &
                "QuantiteBase DECIMAL(18,4) NOT NULL, " &
                "CoutUnitaireBaseVente DECIMAL(18,4) NULL, " &
                "BeneficeEstime DECIMAL(18,2) NULL, " &
                "CreeLe DATETIME2 NOT NULL CONSTRAINT DF_InitVenteLignes_CreeLe DEFAULT(SYSDATETIME()), " &
                "CONSTRAINT FK_InitVenteLignes_Session FOREIGN KEY (InitialisationVenteSessionId) REFERENCES dbo.InitialisationVenteSessions(InitialisationVenteSessionId)) " &
                "END")

            Executer(cn, tx, "IF OBJECT_ID('dbo.FacturesVente', 'U') IS NOT NULL AND COL_LENGTH('dbo.FacturesVente', 'OrigineVente') IS NULL ALTER TABLE dbo.FacturesVente ADD OrigineVente NVARCHAR(30) NULL")
            Executer(cn, tx, "IF OBJECT_ID('dbo.FacturesVente', 'U') IS NOT NULL AND COL_LENGTH('dbo.FacturesVente', 'InitialisationVenteSessionId') IS NULL ALTER TABLE dbo.FacturesVente ADD InitialisationVenteSessionId INT NULL")

            Executer(cn, tx, "IF OBJECT_ID('dbo.InitialisationVenteSessions', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_InitVenteSessions_Statut_Dates' AND object_id=OBJECT_ID('dbo.InitialisationVenteSessions')) CREATE INDEX IX_InitVenteSessions_Statut_Dates ON dbo.InitialisationVenteSessions(Statut, DateDebut, DateFin)")
            Executer(cn, tx, "IF OBJECT_ID('dbo.InitialisationVenteLignes', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_InitVenteLignes_Session' AND object_id=OBJECT_ID('dbo.InitialisationVenteLignes')) CREATE INDEX IX_InitVenteLignes_Session ON dbo.InitialisationVenteLignes(InitialisationVenteSessionId)")
            Executer(cn, tx, "IF OBJECT_ID('dbo.InitialisationVenteLignes', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_InitVenteLignes_Date_Produit' AND object_id=OBJECT_ID('dbo.InitialisationVenteLignes')) CREATE INDEX IX_InitVenteLignes_Date_Produit ON dbo.InitialisationVenteLignes(DateVente, ProduitId)")
            Executer(cn, tx, "IF OBJECT_ID('dbo.FacturesVente', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_FacturesVente_Origine_InitSession' AND object_id=OBJECT_ID('dbo.FacturesVente')) CREATE INDEX IX_FacturesVente_Origine_InitSession ON dbo.FacturesVente(OrigineVente, InitialisationVenteSessionId)")
        End Sub

        Private Shared Sub MigrationStockInitialTechnique(cn As SqlConnection, tx As SqlTransaction)
            Executer(cn, tx,
                "IF OBJECT_ID('dbo.StockInitialTechniqueSessions', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.StockInitialTechniqueSessions (" &
                "StockInitialTechniqueSessionId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, " &
                "ReferenceSession NVARCHAR(60) NOT NULL UNIQUE, " &
                "DateSession DATETIME2 NOT NULL CONSTRAINT DF_StockInitTechSessions_Date DEFAULT(SYSDATETIME()), " &
                "ModeOperation NVARCHAR(30) NOT NULL, " &
                "Observation NVARCHAR(500) NULL, " &
                "CreePar INT NOT NULL, " &
                "Machine NVARCHAR(100) NULL, " &
                "CreeLe DATETIME2 NOT NULL CONSTRAINT DF_StockInitTechSessions_CreeLe DEFAULT(SYSDATETIME())) " &
                "END")

            Executer(cn, tx,
                "IF OBJECT_ID('dbo.StockInitialTechniqueLignes', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.StockInitialTechniqueLignes (" &
                "StockInitialTechniqueLigneId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, " &
                "StockInitialTechniqueSessionId INT NOT NULL, " &
                "ProduitId INT NOT NULL, " &
                "AncienneQuantiteBase DECIMAL(18,4) NOT NULL, " &
                "NouvelleQuantiteBase DECIMAL(18,4) NOT NULL, " &
                "DifferenceBase DECIMAL(18,4) NOT NULL, " &
                "TypeGestionStock NVARCHAR(20) NULL, " &
                "UnitePrincipale NVARCHAR(50) NULL, " &
                "UniteSecondaire NVARCHAR(50) NULL, " &
                "ContenuUnitePrincipale DECIMAL(18,4) NULL, " &
                "ContenuUniteSecondaire DECIMAL(18,4) NULL, " &
                "ModeOperation NVARCHAR(30) NOT NULL, " &
                "Observation NVARCHAR(500) NULL, " &
                "CreeLe DATETIME2 NOT NULL CONSTRAINT DF_StockInitTechLignes_CreeLe DEFAULT(SYSDATETIME()), " &
                "CONSTRAINT FK_StockInitTechLignes_Session FOREIGN KEY (StockInitialTechniqueSessionId) REFERENCES dbo.StockInitialTechniqueSessions(StockInitialTechniqueSessionId)) " &
                "END")

            Executer(cn, tx, "IF OBJECT_ID('dbo.StockInitialTechniqueLignes', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_StockInitTechLignes_Session' AND object_id=OBJECT_ID('dbo.StockInitialTechniqueLignes')) CREATE INDEX IX_StockInitTechLignes_Session ON dbo.StockInitialTechniqueLignes(StockInitialTechniqueSessionId)")
            Executer(cn, tx, "IF OBJECT_ID('dbo.StockInitialTechniqueLignes', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_StockInitTechLignes_Produit' AND object_id=OBJECT_ID('dbo.StockInitialTechniqueLignes')) CREATE INDEX IX_StockInitTechLignes_Produit ON dbo.StockInitialTechniqueLignes(ProduitId)")
        End Sub

        Private Shared Sub Executer(cn As SqlConnection, tx As SqlTransaction, sql As String)
            Using cmd As New SqlCommand(sql, cn, tx)
                cmd.CommandTimeout = 30
                cmd.ExecuteNonQuery()
            End Using
        End Sub
    End Class
End Namespace
