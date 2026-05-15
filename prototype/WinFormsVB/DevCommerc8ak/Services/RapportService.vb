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
            Dim sql As String = "SELECT ISNULL(SUM(s.QuantiteStock * CASE WHEN p.PrixAchat > 0 THEN p.PrixAchat " &
                                "WHEN p.PrixGros > 0 THEN p.PrixGros ELSE p.PrixDetail END),0) " &
                                "FROM Produits p JOIN vStockProduit s ON s.ProduitId = p.ProduitId"
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, Nothing)
            Return Convert.ToDecimal(v)
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
