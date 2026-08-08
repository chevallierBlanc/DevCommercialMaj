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

        Private Shared Sub Executer(cn As SqlConnection, tx As SqlTransaction, sql As String)
            Using cmd As New SqlCommand(sql, cn, tx)
                cmd.CommandTimeout = 30
                cmd.ExecuteNonQuery()
            End Using
        End Sub
    End Class
End Namespace
