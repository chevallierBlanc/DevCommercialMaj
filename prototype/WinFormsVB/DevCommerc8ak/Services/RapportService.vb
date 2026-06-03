Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class RapportService
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        ' CA journalier pour une date.
        Public Function CAJournalier(dateRef As Date) As Decimal
            Dim sql As String = "SELECT ISNULL(SUM(MontantTotal),0) FROM FacturesVente WHERE CAST(CreeLe AS DATE)=@d AND Statut='PAYEE'"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@d", dateRef.Date)}
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToDecimal(v)
        End Function

        ' CA mensuel pour une date.
        Public Function CAMensuel(dateRef As Date) As Decimal
            Dim sql As String = "SELECT ISNULL(SUM(MontantTotal),0) FROM FacturesVente WHERE YEAR(CreeLe)=@y AND MONTH(CreeLe)=@m AND Statut='PAYEE'"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@y", dateRef.Year),
                New SqlParameter("@m", dateRef.Month)
            }
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToDecimal(v)
        End Function

        ' Stock critique.
        Public Function StockCritique(seuil As Decimal) As Integer
            Dim sql As String = "SELECT COUNT(*) FROM vStockProduit WHERE QuantiteStock <= @s"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@s", seuil)}
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(v)
        End Function

        ' Valeur stock.
        Public Function ValeurStock() As Decimal
            Dim sql As String = "" &
                "WITH CtePrixMoyen AS(" &
                "    SELECT se.ProduitId," &
                "           CASE " &
                "               WHEN ISNULL(p.ConversionUnite, 0) > 0 AND ISNULL(p.PrixAchat, 0) > 0 THEN ISNULL(p.PrixAchat, 0) / NULLIF(ISNULL(p.ConversionUnite, 0), 0) " &
                "               ELSE SUM(ISNULL(se.PrixAchat, 0)) / NULLIF(SUM(ISNULL(se.QuantiteBase, 0)), 0) " &
                "           END AS CoutAchatMoyenPiece " &
                "    FROM StockEntree se " &
                "    INNER JOIN Produits p ON p.ProduitId = se.ProduitId " &
                "    GROUP BY se.ProduitId, p.PrixAchat, p.ConversionUnite" &
                ") " &
                "SELECT ISNULL(CAST(SUM(ISNULL(s.QuantiteStock, 0) * ISNULL(pm.CoutAchatMoyenPiece, 0)) AS BIGINT), 0) " &
                "FROM vStockProduit s " &
                "LEFT JOIN CtePrixMoyen pm ON pm.ProduitId = s.ProduitId"
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, Nothing)
            Return Convert.ToDecimal(v)
        End Function

        ' Analyse vente et rentabilite sur une periode.
        Public Function AnalyseVente(dateDebut As Date, dateFin As Date) As DataTable
            Dim sql As String = "" &
                "WITH CTEStockEntree AS" &
                "(" &
                "    SELECT" &
                "       se.ProduitId," &
                "        SUM(ISNULL(se.QuantiteBase, 0)) AS QuantiteEntreePieces," &
                "        SUM(ISNULL(se.QuantiteSaisie, 0) * ISNULL(se.PrixAchat, 0)) AS ValeurStockEntree," &
                "        CASE " &
                "            WHEN ISNULL(p.ConversionUnite, 0) > 0 AND ISNULL(p.PrixAchat, 0) > 0 THEN ISNULL(p.PrixAchat, 0) / NULLIF(ISNULL(p.ConversionUnite, 0), 0) " &
                "            ELSE SUM(ISNULL(se.PrixAchat, 0)) / NULLIF(SUM(ISNULL(se.QuantiteBase, 0)), 0) " &
                "        END AS CoutAchatMoyenPiece " &
                "    FROM StockEntree se " &
                "    INNER JOIN Produits p ON p.ProduitId = se.ProduitId " &
                "    WHERE se.DateEntree >= @DateDebut " &
                "      AND se.DateEntree < DATEADD(DAY, 1, @DateFin) " &
                "    GROUP BY se.ProduitId, p.PrixAchat, p.ConversionUnite" &
                "), " &
                "Ventes AS" &
                "(" &
                "    SELECT" &
                "        l.ProduitId," &
                "        SUM(ISNULL(l.Quantite, 0)) AS QuantiteVenduePieces," &
                "        SUM(ISNULL(l.MontantLigne, ISNULL(l.QuantiteSaisie, 0) * ISNULL(l.PrixUnitaire, 0))) AS ChiffreAffaires " &
                "    FROM LignesFactureVente l " &
                "    INNER JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId " &
                "    WHERE f.Statut = 'PAYEE' " &
                "      AND f.CreeLe >= @DateDebut " &
                "      AND f.CreeLe < DATEADD(DAY, 1, @DateFin) " &
                "    GROUP BY l.ProduitId" &
                "), DepensesPeriode AS" &
                "(" &
                "    SELECT ISNULL(SUM(ISNULL(Montant, 0)), 0) AS TotalDepenses " &
                "    FROM Depenses " &
                "    WHERE DateDepense >= @DateDebut " &
                "      AND DateDepense < DATEADD(DAY, 1, @DateFin)" &
                "), SortiesManuelles AS" &
                "(" &
                "    SELECT ISNULL(SUM(ISNULL(ss.QuantiteBase, 0) * ISNULL(cp.CoutPiece, 0)), 0) AS TotalChargesManuelles " &
                "    FROM StockSortie ss " &
                "    LEFT JOIN (" &
                "        SELECT se.ProduitId, " &
                "               SUM(ISNULL(se.QuantiteSaisie, 0) * ISNULL(se.PrixAchat, 0)) / NULLIF(SUM(ISNULL(se.QuantiteBase, 0)), 0) AS CoutPiece " &
                "        FROM StockEntree se " &
                "        WHERE se.DateEntree < DATEADD(DAY, 1, @DateFin) " &
                "        GROUP BY se.ProduitId" &
                "    ) cp ON cp.ProduitId = ss.ProduitId " &
                "    WHERE ss.DateSortie >= @DateDebut " &
                "      AND ss.DateSortie < DATEADD(DAY, 1, @DateFin) " &
                "      AND UPPER(ISNULL(ss.Source, '')) IN ('SORTIE_MANUELLE', 'MANUEL')" &
                "), " &
                "AnalyseProduit AS" &
                "(" &
                "    SELECT" &
                "        p.ProduitId," &
                "        p.Libelle AS Produit," &
                "        ISNULL(se.ValeurStockEntree, 0) AS ValeurStockEntree," &
                "        ISNULL(v.QuantiteVenduePieces, 0) AS QuantiteVenduePieces," &
                "        ISNULL(v.ChiffreAffaires, 0) AS ChiffreAffaires," &
                "        ISNULL(v.QuantiteVenduePieces, 0) * ISNULL(se.CoutAchatMoyenPiece, 0) AS CoutMarchandisesVendues," &
                "        ISNULL(v.ChiffreAffaires, 0) - (ISNULL(v.QuantiteVenduePieces, 0) * ISNULL(se.CoutAchatMoyenPiece, 0)) AS Benefice," &
                "        ISNULL(s.QuantiteStock, 0) AS StockRestantPieces," &
                "        ISNULL(s.QuantiteStock, 0) * ISNULL(se.CoutAchatMoyenPiece, 0) AS CoutStockRestant " &
                "    FROM Produits p " &
                "    LEFT JOIN CTEStockEntree se ON se.ProduitId = p.ProduitId " &
                "    LEFT JOIN Ventes v ON v.ProduitId = p.ProduitId " &
                "    LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId" &
                ") " &
                "SELECT " &
                "    ISNULL(CAST(SUM(ValeurStockEntree) AS BIGINT), 0) AS ValeurStockEntree, " &
                "    ISNULL(CAST(SUM(CoutMarchandisesVendues) AS BIGINT), 0) AS CoutMarchandisesVendues, " &
                "    ISNULL(CAST(SUM(ChiffreAffaires) AS BIGINT), 0) AS ChiffreAffaires, " &
                "    ISNULL(CAST(SUM(Benefice) AS BIGINT), 0) AS BeneficeRealise, " &
                "    ISNULL(CAST(MAX(dp.TotalDepenses) AS BIGINT), 0) AS DepensesTotal, " &
                "    ISNULL(CAST(MAX(sm.TotalChargesManuelles) AS BIGINT), 0) AS ChargesSortiesManuelles, " &
                "    ISNULL(CAST(SUM(Benefice) - MAX(dp.TotalDepenses) - MAX(sm.TotalChargesManuelles) AS BIGINT), 0) AS BeneficeNetRealise, " &
                "    ISNULL(CAST(SUM(CoutStockRestant) AS BIGINT), 0) AS CoutStockRestant, " &
                "    ISNULL(CAST(SUM(CoutStockRestant) * (ISNULL(SUM(Benefice), 0) / NULLIF(ISNULL(SUM(CoutMarchandisesVendues), 0), 0)) AS BIGINT), 0) AS ProjectionBeneficeRestant, " &
                "    ISNULL(CAST(((ISNULL(SUM(Benefice), 0) - MAX(dp.TotalDepenses) - MAX(sm.TotalChargesManuelles)) * 100.0 / NULLIF(ISNULL(SUM(CoutMarchandisesVendues), 0), 0)) AS DECIMAL(10,2)), 0) AS MargeBeneficiairePourcentage, " &
                "    CASE " &
                "        WHEN ISNULL(SUM(Benefice), 0) - MAX(dp.TotalDepenses) - MAX(sm.TotalChargesManuelles) < 0 THEN 'CRITIQUE / PERTE' " &
                "        WHEN ISNULL(SUM(Benefice), 0) - MAX(dp.TotalDepenses) - MAX(sm.TotalChargesManuelles) = 0 THEN 'POINT MORT' " &
                "        WHEN (ISNULL(SUM(Benefice), 0) - MAX(dp.TotalDepenses) - MAX(sm.TotalChargesManuelles)) * 100.0 / NULLIF(ISNULL(SUM(CoutMarchandisesVendues), 0), 0) < 10 THEN 'FAIBLE RENTABILITÉ' " &
                "        WHEN (ISNULL(SUM(Benefice), 0) - MAX(dp.TotalDepenses) - MAX(sm.TotalChargesManuelles)) * 100.0 / NULLIF(ISNULL(SUM(CoutMarchandisesVendues), 0), 0) BETWEEN 10 AND 25 THEN 'PROGRÈS' " &
                "        ELSE 'BONNE RENTABILITÉ' " &
                "    END AS Evaluation " &
                "FROM AnalyseProduit " &
                "CROSS JOIN DepensesPeriode dp " &
                "CROSS JOIN SortiesManuelles sm"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@DateDebut", dateDebut.Date),
                New SqlParameter("@DateFin", dateFin.Date)
            }
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        Public Function BeneficeNetDetails(dateDebut As Date, dateFin As Date) As DataTable
            Dim dt As New DataTable()
            dt.Columns.Add("Ordre", GetType(Integer))
            dt.Columns.Add("Rubrique", GetType(String))
            dt.Columns.Add("Categorie", GetType(String))
            dt.Columns.Add("QuantitePieces", GetType(Decimal))
            dt.Columns.Add("Montant", GetType(Decimal))
            dt.Columns.Add("Commentaire", GetType(String))

            Dim resumeAnalyse As DataTable = AnalyseVente(dateDebut, dateFin)
            Dim row As DataRow = Nothing
            If resumeAnalyse IsNot Nothing AndAlso resumeAnalyse.Rows.Count > 0 Then
                row = resumeAnalyse.Rows(0)
            End If

            Dim beneficeRealise As Decimal = LireDecimal(row, "BeneficeRealise")
            Dim depensesTotal As Decimal = LireDecimal(row, "DepensesTotal")
            Dim chargesManuelles As Decimal = LireDecimal(row, "ChargesSortiesManuelles")
            Dim beneficeNet As Decimal = LireDecimal(row, "BeneficeNetRealise")

            AjouterLigneBeneficeNet(dt, 0, "Synthèse", "Bénéfice réalisé", 0D, beneficeRealise, "Résultat commercial avant charges")
            AjouterLigneBeneficeNet(dt, 1, "Synthèse", "Dépenses", 0D, depensesTotal, "Dépenses de la période")
            AjouterLigneBeneficeNet(dt, 2, "Synthèse", "Sorties manuelles", 0D, chargesManuelles, "Sorties valorisées au coût réel")

            Dim sqlDepenses As String = "" &
                "SELECT ISNULL(NULLIF(LTRIM(RTRIM(d.Categorie)), ''), 'Sans catégorie') AS Categorie, " &
                "       COUNT(*) AS NombreDepenses, " &
                "       SUM(ISNULL(d.Montant, 0)) AS MontantTotal " &
                "FROM Depenses d " &
                "WHERE d.DateDepense >= @DateDebut " &
                "  AND d.DateDepense < DATEADD(DAY, 1, @DateFin) " &
                "GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(d.Categorie)), ''), 'Sans catégorie') " &
                "ORDER BY SUM(ISNULL(d.Montant, 0)) DESC, Categorie ASC"
            Dim pDepenses As New List(Of SqlParameter) From {
                New SqlParameter("@DateDebut", dateDebut.Date),
                New SqlParameter("@DateFin", dateFin.Date)
            }
            Dim dtDepenses As DataTable = _dal.ExecuterTable(sqlDepenses, CommandType.Text, pDepenses)
            For Each dep As DataRow In dtDepenses.Rows
                AjouterLigneBeneficeNet(dt, 10, "Dépenses", Convert.ToString(dep("Categorie")), 0D, Convert.ToDecimal(dep("MontantTotal")), Convert.ToString(dep("NombreDepenses")) & " dépense(s)")
            Next

            Dim sqlCharges As String = "" &
                "WITH CoutPieceProduit AS (" &
                "    SELECT se.ProduitId, " &
                "           CASE " &
                "               WHEN ISNULL(p.ConversionUnite, 0) > 0 AND ISNULL(p.PrixAchat, 0) > 0 THEN ISNULL(p.PrixAchat, 0) / NULLIF(ISNULL(p.ConversionUnite, 0), 0) " &
                "               ELSE SUM(ISNULL(se.PrixAchat, 0)) / NULLIF(SUM(ISNULL(se.QuantiteBase, 0)), 0) " &
                "           END AS CoutPiece " &
                "    FROM StockEntree se " &
                "    INNER JOIN Produits p ON p.ProduitId = se.ProduitId " &
                "    WHERE se.DateEntree < DATEADD(DAY, 1, @DateFin) " &
                "    GROUP BY se.ProduitId, p.PrixAchat, p.ConversionUnite" &
                ") " &
                "SELECT Categorie, SUM(Pieces) AS QuantitePieces, SUM(Montant) AS MontantTotal " &
                "FROM (" &
                "    SELECT 'Sorties gratuites' AS Categorie, ISNULL(ss.QuantiteBase, 0) AS Pieces, ISNULL(ss.QuantiteBase, 0) * ISNULL(cp.CoutPiece, 0) AS Montant " &
                "    FROM StockSortie ss " &
                "    LEFT JOIN CoutPieceProduit cp ON cp.ProduitId = ss.ProduitId " &
                "    WHERE ss.DateSortie >= @DateDebut AND ss.DateSortie < DATEADD(DAY, 1, @DateFin) " &
                "      AND UPPER(ISNULL(ss.Source, '')) IN ('SORTIE_MANUELLE', 'MANUEL') " &
                "      AND UPPER(ISNULL(ss.StatutPaiement, '')) = 'GRATUIT' " &
                "    UNION ALL " &
                "    SELECT 'Dons' AS Categorie, ISNULL(ss.QuantiteBase, 0) AS Pieces, ISNULL(ss.QuantiteBase, 0) * ISNULL(cp.CoutPiece, 0) AS Montant " &
                "    FROM StockSortie ss " &
                "    LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId " &
                "    LEFT JOIN CoutPieceProduit cp ON cp.ProduitId = ss.ProduitId " &
                "    WHERE ss.DateSortie >= @DateDebut AND ss.DateSortie < DATEADD(DAY, 1, @DateFin) " &
                "      AND UPPER(ISNULL(ss.Source, '')) IN ('SORTIE_MANUELLE', 'MANUEL') " &
                "      AND (UPPER(ISNULL(m.Nature, '')) LIKE '%DON%' OR UPPER(ISNULL(m.Libelle, '')) LIKE '%DON%') " &
                "    UNION ALL " &
                "    SELECT 'Allocations' AS Categorie, ISNULL(ss.QuantiteBase, 0) AS Pieces, ISNULL(ss.QuantiteBase, 0) * ISNULL(cp.CoutPiece, 0) AS Montant " &
                "    FROM StockSortie ss " &
                "    LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId " &
                "    LEFT JOIN CoutPieceProduit cp ON cp.ProduitId = ss.ProduitId " &
                "    WHERE ss.DateSortie >= @DateDebut AND ss.DateSortie < DATEADD(DAY, 1, @DateFin) " &
                "      AND UPPER(ISNULL(ss.Source, '')) IN ('SORTIE_MANUELLE', 'MANUEL') " &
                "      AND (UPPER(ISNULL(m.Nature, '')) LIKE '%ALLOC%' OR UPPER(ISNULL(m.Libelle, '')) LIKE '%ALLOC%') " &
                "    UNION ALL " &
                "    SELECT 'Dettes clients' AS Categorie, ISNULL(ss.QuantiteBase, 0) AS Pieces, ISNULL(ss.QuantiteBase, 0) * ISNULL(cp.CoutPiece, 0) AS Montant " &
                "    FROM StockSortie ss " &
                "    LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId " &
                "    LEFT JOIN CoutPieceProduit cp ON cp.ProduitId = ss.ProduitId " &
                "    WHERE ss.DateSortie >= @DateDebut AND ss.DateSortie < DATEADD(DAY, 1, @DateFin) " &
                "      AND UPPER(ISNULL(ss.Source, '')) IN ('SORTIE_MANUELLE', 'MANUEL') " &
                "      AND (UPPER(ISNULL(m.Nature, '')) LIKE '%DETTE%' OR UPPER(ISNULL(m.Libelle, '')) LIKE '%DETTE%') " &
                "      AND (UPPER(ISNULL(m.Libelle, '')) LIKE '%CLIENT%' OR (UPPER(ISNULL(ss.StatutPaiement, '')) = 'IMPAYE' AND ss.ClientId IS NOT NULL)) " &
                "    UNION ALL " &
                "    SELECT 'Dettes boss' AS Categorie, ISNULL(ss.QuantiteBase, 0) AS Pieces, ISNULL(ss.QuantiteBase, 0) * ISNULL(cp.CoutPiece, 0) AS Montant " &
                "    FROM StockSortie ss " &
                "    LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId " &
                "    LEFT JOIN CoutPieceProduit cp ON cp.ProduitId = ss.ProduitId " &
                "    WHERE ss.DateSortie >= @DateDebut AND ss.DateSortie < DATEADD(DAY, 1, @DateFin) " &
                "      AND UPPER(ISNULL(ss.Source, '')) IN ('SORTIE_MANUELLE', 'MANUEL') " &
                "      AND (UPPER(ISNULL(m.Nature, '')) LIKE '%DETTE%' OR UPPER(ISNULL(m.Libelle, '')) LIKE '%DETTE%') " &
                "      AND (UPPER(ISNULL(m.Libelle, '')) LIKE '%BOSS%' OR UPPER(ISNULL(m.Libelle, '')) LIKE '%PATRON%' OR UPPER(ISNULL(m.Libelle, '')) LIKE '%MAISON%') " &
                "    UNION ALL " &
                "    SELECT 'Hors caisse' AS Categorie, ISNULL(ss.QuantiteBase, 0) AS Pieces, ISNULL(ss.QuantiteBase, 0) * ISNULL(cp.CoutPiece, 0) AS Montant " &
                "    FROM StockSortie ss " &
                "    LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId " &
                "    LEFT JOIN CoutPieceProduit cp ON cp.ProduitId = ss.ProduitId " &
                "    WHERE ss.DateSortie >= @DateDebut AND ss.DateSortie < DATEADD(DAY, 1, @DateFin) " &
                "      AND UPPER(ISNULL(ss.Source, '')) IN ('SORTIE_MANUELLE', 'MANUEL') " &
                "      AND (UPPER(ISNULL(m.Nature, '')) LIKE '%HORS%' OR UPPER(ISNULL(m.Libelle, '')) LIKE '%HORS%') " &
                "    UNION ALL " &
                "    SELECT 'Pertes' AS Categorie, ISNULL(sp.QuantiteBase, 0) AS Pieces, ISNULL(sp.QuantiteBase, 0) * ISNULL(cp.CoutPiece, 0) AS Montant " &
                "    FROM StockPerte sp " &
                "    LEFT JOIN CoutPieceProduit cp ON cp.ProduitId = sp.ProduitId " &
                "    WHERE sp.DatePerte >= @DateDebut AND sp.DatePerte < DATEADD(DAY, 1, @DateFin) " &
                ") q " &
                "GROUP BY Categorie " &
                "ORDER BY SUM(Montant) DESC, Categorie ASC"
            Dim pCharges As New List(Of SqlParameter) From {
                New SqlParameter("@DateDebut", dateDebut.Date),
                New SqlParameter("@DateFin", dateFin.Date)
            }
            Dim dtCharges As DataTable = _dal.ExecuterTable(sqlCharges, CommandType.Text, pCharges)
            For Each charge As DataRow In dtCharges.Rows
                AjouterLigneBeneficeNet(dt, 20, "Charges", Convert.ToString(charge("Categorie")), Convert.ToDecimal(charge("QuantitePieces")), Convert.ToDecimal(charge("MontantTotal")), "Charge consommant du stock ou sans recette")
            Next

            AjouterLigneBeneficeNet(dt, 99, "Synthèse", "Bénéfice net réalisé", 0D, beneficeNet, "Bénéfice après déductions")
            Return dt
        End Function

        Private Sub AjouterLigneBeneficeNet(dt As DataTable, ordre As Integer, rubrique As String, categorie As String, quantitePieces As Decimal, montant As Decimal, commentaire As String)
            dt.Rows.Add(ordre, rubrique, categorie, quantitePieces, montant, commentaire)
        End Sub

        Private Shared Function LireDecimal(row As DataRow, colonne As String) As Decimal
            If row Is Nothing OrElse row.Table Is Nothing OrElse Not row.Table.Columns.Contains(colonne) OrElse row.IsNull(colonne) Then
                Return 0D
            End If
            Return Convert.ToDecimal(row(colonne))
        End Function

        ' Factures en attente (non payees).
        Public Function FacturesEnAttente() As Integer
            Dim sql As String = "SELECT COUNT(*) FROM FacturesVente WHERE Statut='EN_ATTENTE'"
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, Nothing)
            Return Convert.ToInt32(v)
        End Function

        ' Produits sans prix defini.
        Public Function ProduitsSansPrix() As Integer
            Dim sql As String = "SELECT COUNT(*) FROM Produits WHERE (PrixDetail <= 0 AND PrixGros <= 0 AND PrixDouzaine <= 0 AND PrixDemi <= 0)"
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, Nothing)
            Return Convert.ToInt32(v)
        End Function

        ' Produits expirant bientot.
        Public Function ProduitsExpirant(alerteJours As Integer) As Integer
            Dim sql As String = "SELECT COUNT(*) FROM Produits WHERE DateExpiration IS NOT NULL AND DateExpiration <= DATEADD(DAY,@j,GETDATE())"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@j", alerteJours)}
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(v)
        End Function

        ' Nombre de clients fideles (>=10 achats / 30 jours).
        Public Function ClientsFideles() As Integer
            Dim sql As String = "SELECT COUNT(*) FROM (" &
                                "SELECT f.ClientId, COUNT(*) AS NbAchats " &
                                "FROM FacturesVente f WHERE f.Statut='PAYEE' AND f.CreeLe >= DATEADD(DAY,-30,GETDATE()) " &
                                "GROUP BY f.ClientId HAVING COUNT(*) >= 10" &
                                ") t"
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, Nothing)
            Return Convert.ToInt32(v)
        End Function

        ' Serie CA des 7 derniers jours.
        Public Function CA7Jours() As DataTable
            Dim sql As String = "SELECT CAST(CreeLe AS DATE) AS Jour, ISNULL(SUM(MontantTotal),0) AS CA " &
                                "FROM FacturesVente WHERE CreeLe >= DATEADD(DAY,-6,CAST(GETDATE() AS DATE)) AND Statut='PAYEE' " &
                                "GROUP BY CAST(CreeLe AS DATE) ORDER BY Jour"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        ' Ventes par mois (12 derniers mois).
        Public Function VentesParMois() As DataTable
            Dim sql As String = "SELECT FORMAT(CreeLe,'yyyy-MM') AS Mois, ISNULL(SUM(MontantTotal),0) AS CA " &
                                "FROM FacturesVente WHERE CreeLe >= DATEADD(MONTH,-11,GETDATE()) AND Statut='PAYEE' " &
                                "GROUP BY FORMAT(CreeLe,'yyyy-MM') ORDER BY Mois"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        ' Revenus par mode de paiement.
        Public Function RevenusParMode() As DataTable
            Dim sql As String = "SELECT ModePaiement, ISNULL(SUM(Montant),0) AS Montant " &
                                "FROM Paiements GROUP BY ModePaiement"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        ' Revenus par produit (top 5).
        Public Function RevenusParProduit() As DataTable
            Dim sql As String = "SELECT TOP 5 p.Libelle, ISNULL(SUM(l.MontantLigne),0) AS Montant " &
                                "FROM LignesFactureVente l " &
                                "JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId " &
                                "JOIN Produits p ON p.ProduitId = l.ProduitId " &
                                "WHERE f.Statut='PAYEE' " &
                                "GROUP BY p.Libelle ORDER BY SUM(l.MontantLigne) DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        ' Taux de ventes (factures payees / total).
        Public Function TauxVentes() As Decimal
            Dim sql As String = "SELECT CASE WHEN COUNT(*)=0 THEN 0 ELSE " &
                                "CAST(SUM(CASE WHEN Statut='PAYEE' THEN 1 ELSE 0 END) AS DECIMAL(18,2)) / COUNT(*) END " &
                                "FROM FacturesVente"
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, Nothing)
            Return Convert.ToDecimal(v)
        End Function

        ' Comparatif mois actuel vs precedent.
        Public Function ComparatifMois(dateRef As Date) As DataTable
            Dim sql As String = "SELECT 'MoisActuel' AS Periode, ISNULL(SUM(MontantTotal),0) AS CA " &
                                "FROM FacturesVente WHERE YEAR(CreeLe)=@y AND MONTH(CreeLe)=@m AND Statut='PAYEE' " &
                                "UNION ALL " &
                                "SELECT 'MoisPrecedent' AS Periode, ISNULL(SUM(MontantTotal),0) AS CA " &
                                "FROM FacturesVente WHERE YEAR(CreeLe)=@y2 AND MONTH(CreeLe)=@m2 AND Statut='PAYEE'"
            Dim prev As Date = dateRef.AddMonths(-1)
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@y", dateRef.Year),
                New SqlParameter("@m", dateRef.Month),
                New SqlParameter("@y2", prev.Year),
                New SqlParameter("@m2", prev.Month)
            }
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        ' Taux de vente vs stock (ratio).
        Public Function TauxVenteStock() As Decimal
            Dim sql As String = "SELECT CASE WHEN (ISNULL(s.Stock,0)+ISNULL(v.Vendu,0))=0 THEN 0 ELSE " &
                                "ISNULL(v.Vendu,0) / (ISNULL(s.Stock,0)+ISNULL(v.Vendu,0)) END " &
                                "FROM (SELECT SUM(QuantiteStock) AS Stock FROM vStockProduit) s " &
                                "CROSS JOIN (SELECT SUM(l.Quantite) AS Vendu FROM LignesFactureVente l JOIN FacturesVente f ON f.FactureVenteId=l.FactureVenteId WHERE f.Statut='PAYEE') v"
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, Nothing)
            Return Convert.ToDecimal(v)
        End Function

        ' Alertes detaillees.
        Public Function AlertesDetail(seuil As Decimal, alerteJours As Integer) As DataTable
            Dim sql As String = "" &
                "SELECT TOP 10 'Stock critique' AS TypeAlerte, p.Libelle AS Cible FROM Produits p JOIN vStockProduit s ON s.ProduitId=p.ProduitId WHERE s.QuantiteStock <= @s " &
                "UNION ALL " &
                "SELECT TOP 10 'Expiration proche' AS TypeAlerte, Libelle AS Cible FROM Produits WHERE DateExpiration IS NOT NULL AND DateExpiration <= DATEADD(DAY,@j,GETDATE()) " &
                "UNION ALL " &
                "SELECT TOP 10 'Facture non payee' AS TypeAlerte, NumeroFacture AS Cible FROM FacturesVente WHERE Statut='EN_ATTENTE' " &
                "UNION ALL " &
                "SELECT TOP 10 'Produit sans prix' AS TypeAlerte, Libelle AS Cible FROM Produits WHERE (PrixDetail <= 0 AND PrixGros <= 0 AND PrixDouzaine <= 0 AND PrixDemi <= 0)"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@s", seuil),
                New SqlParameter("@j", alerteJours)
            }
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        ' Activite recente.
        Public Function ActivitesRecentes() As DataTable
            Dim sql As String = "" &
                "SELECT TOP 10 TypeAct, Info, DateAct FROM (" &
                "SELECT TOP 5 'Facture creee' AS TypeAct, NumeroFacture AS Info, CreeLe AS DateAct FROM FacturesVente ORDER BY CreeLe DESC " &
                "UNION ALL " &
                "SELECT TOP 5 'Paiement valide' AS TypeAct, CAST(PaiementId AS NVARCHAR(20)) AS Info, PayeLe AS DateAct FROM Paiements ORDER BY PayeLe DESC" &
                ") t ORDER BY DateAct DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        ' Rapport produits les plus vendus.
        Public Function ProduitsPlusVendus(dateDebut As Date, dateFin As Date) As DataTable
            Dim sql As String = "SELECT TOP 20 p.Libelle, SUM(l.Quantite) AS Quantite " &
                                "FROM LignesFactureVente l " &
                                "JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId " &
                                "JOIN Produits p ON p.ProduitId = l.ProduitId " &
                                "WHERE f.CreeLe BETWEEN @d1 AND @d2 AND f.Statut='PAYEE' " &
                                "GROUP BY p.Libelle ORDER BY SUM(l.Quantite) DESC"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@d1", dateDebut),
                New SqlParameter("@d2", dateFin)
            }
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function
    End Class
End Namespace
