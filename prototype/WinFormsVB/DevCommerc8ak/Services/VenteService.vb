Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class VenteService
        Private ReadOnly _dal As DAL

        Public Sub New()
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            _dal = New DAL(cs)
        End Sub

        Public Function ListerVentesJour(dateRef As Date) As DataTable
            Return ListerVentesParPeriode(dateRef.Date, dateRef.Date.AddDays(1))
        End Function

        Public Function ListerVentesMois(annee As Integer, mois As Integer) As DataTable
            Dim debut As New DateTime(annee, mois, 1)
            Return ListerVentesParPeriode(debut, debut.AddMonths(1))
        End Function

        Public Function ListerVentesAnnee(annee As Integer) As DataTable
            Dim debut As New DateTime(annee, 1, 1)
            Return ListerVentesParPeriode(debut, debut.AddYears(1))
        End Function

        Public Function ListerStockResume() As DataTable
            Dim sql As String = "" &
                "SELECT p.ProduitId, p.Libelle AS Produit, ISNULL(p.ConversionUnite, 1) AS ConversionUnite, " &
                "ISNULL(s.QuantiteStock, 0) AS StockActuelPieces, " &
                "CASE WHEN ISNULL(p.ConversionUnite, 0) > 0 THEN CONVERT(decimal(18,0), FLOOR(ISNULL(s.QuantiteStock,0) / p.ConversionUnite)) ELSE 0 END AS StockActuelCartons, " &
                "ISNULL(vs.TotalVenduPieces, 0) AS QuantiteVenduePieces, " &
                "CASE WHEN ISNULL(p.ConversionUnite, 0) > 0 THEN CONVERT(decimal(18,0), FLOOR(ISNULL(vs.TotalVenduPieces,0) / p.ConversionUnite)) ELSE 0 END AS QuantiteVendueCartons, " &
                "ISNULL(ms.TotalSortiePieces, 0) AS QuantiteSortieManuellePieces, " &
                "CASE WHEN ISNULL(p.ConversionUnite, 0) > 0 THEN CONVERT(decimal(18,0), FLOOR(ISNULL(ms.TotalSortiePieces,0) / p.ConversionUnite)) ELSE 0 END AS QuantiteSortieManuelleCartons, " &
                "ISNULL(vs.TotalVenduPieces, 0) + ISNULL(ms.TotalSortiePieces, 0) AS SortiesTotalesPieces, " &
                "CASE WHEN ISNULL(p.ConversionUnite, 0) > 0 THEN CONVERT(decimal(18,0), FLOOR((ISNULL(vs.TotalVenduPieces,0) + ISNULL(ms.TotalSortiePieces,0)) / p.ConversionUnite)) ELSE 0 END AS SortiesTotalesCartons, " &
                "ISNULL(s.QuantiteStock, 0) AS RestantPieces, " &
                "CASE WHEN ISNULL(p.ConversionUnite, 0) > 0 THEN CONVERT(decimal(18,0), FLOOR(ISNULL(s.QuantiteStock,0) / p.ConversionUnite)) ELSE 0 END AS RestantCartons " &
                "FROM Produits p " &
                "LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId " &
                "LEFT JOIN (SELECT l.ProduitId, ISNULL(SUM(l.QuantiteBase), 0) AS TotalVenduPieces " &
                "           FROM LignesFactureVente l " &
                "           INNER JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId " &
                "           WHERE f.Statut = 'PAYEE' " &
                "           GROUP BY l.ProduitId) vs ON vs.ProduitId = p.ProduitId " &
                "LEFT JOIN (SELECT ProduitId, ISNULL(SUM(QuantiteBase), 0) AS TotalSortiePieces " &
                "           FROM StockSortie " &
                "           WHERE UPPER(ISNULL(Source, '')) IN ('SORTIE_MANUELLE','MANUEL','ADMIN') " &
                "           GROUP BY ProduitId) ms ON ms.ProduitId = p.ProduitId " &
                "ORDER BY p.Libelle"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        Public Function ListerVentesParPeriode(dateDebut As DateTime, dateFin As DateTime) As DataTable
            Dim sql As String = "" &
                "SELECT MAX(f.CreeLe) AS DateVente, " &
                "p.Libelle AS Produit, " &
                "CAST(MAX(ISNULL(p.PrixAchat, 0)) AS BIGINT) AS PrixAchatCarton, " &
                "CAST(SUM(ISNULL(l.Quantite, 0)) AS BIGINT) AS QuantiteVenduePieces, " &
                "CAST(SUM(ISNULL(l.MontantLigne, ISNULL(l.QuantiteSaisie, 0) * ISNULL(l.PrixUnitaire, 0))) AS BIGINT) AS MontantGenere, " &
                "CAST(SUM(ISNULL(l.MontantLigne, ISNULL(l.QuantiteSaisie, 0) * ISNULL(l.PrixUnitaire, 0)) - (ISNULL(l.Quantite, 0) * (ISNULL(p.PrixAchat, 0) / NULLIF(ISNULL(p.ConversionUnite, 0), 0)))) AS BIGINT) AS Benefice " &
                "FROM LignesFactureVente l " &
                "INNER JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId " &
                "INNER JOIN Produits p ON p.ProduitId = l.ProduitId " &
                "WHERE f.Statut = 'PAYEE' " &
                "AND f.CreeLe >= @DateDebut AND f.CreeLe < @DateFin " &
                "GROUP BY p.ProduitId, p.Libelle " &
                "ORDER BY MAX(f.CreeLe) DESC, p.Libelle ASC"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@DateDebut", dateDebut),
                New SqlParameter("@DateFin", dateFin)
            }
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function
    End Class
End Namespace
