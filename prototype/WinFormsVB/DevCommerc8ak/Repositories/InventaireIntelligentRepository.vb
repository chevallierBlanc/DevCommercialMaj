Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class InventaireIntelligentRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Function GenererReferenceInventaire() As String
            Dim prefix As String = "INV-" & Date.Now.ToString("yyyyMMdd")
            Dim sql As String = "SELECT ISNULL(MAX(CAST(RIGHT(ReferenceInventaire, 3) AS INT)), 0) + 1 FROM Inventaires WHERE ReferenceInventaire LIKE @PrefixLike"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@PrefixLike", prefix & "-%")}
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Dim numero As Integer = Convert.ToInt32(v)
            Return prefix & "-" & numero.ToString("000")
        End Function

        Public Function ObtenirInventaireEnCours() As DataTable
            Dim sql As String = "" &
                "SELECT TOP 1 i.InventaireId, i.ReferenceInventaire, i.DateCreation, i.DateValidation, i.CreePar, i.ValidePar, i.Statut, i.Observation, " &
                "       ISNULL(lc.TotalLignes, 0) AS TotalLignes, " &
                "       ISNULL(lc.NombreComptes, 0) AS NombreComptes, " &
                "       ISNULL(lc.NombreNonComptes, 0) AS NombreNonComptes " &
                "FROM Inventaires i " &
                "OUTER APPLY ( " &
                "    SELECT COUNT(*) AS TotalLignes, " &
                "           SUM(CASE WHEN il.StockPhysique IS NOT NULL THEN 1 ELSE 0 END) AS NombreComptes, " &
                "           SUM(CASE WHEN il.StockPhysique IS NULL THEN 1 ELSE 0 END) AS NombreNonComptes " &
                "    FROM InventaireLignes il " &
                "    WHERE il.InventaireId = i.InventaireId " &
                ") lc " &
                "WHERE i.Statut = N'EN_COURS' " &
                "ORDER BY i.DateCreation DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        Public Function ObtenirInventaireParId(inventaireId As Integer) As DataTable
            Dim sql As String = "SELECT InventaireId, ReferenceInventaire, DateCreation, DateValidation, CreePar, ValidePar, Statut, Observation FROM Inventaires WHERE InventaireId=@InventaireId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@InventaireId", inventaireId)}
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        Public Function CreerInventaire(referenceInventaire As String, creePar As Integer, observation As String) As Integer
            Dim sql As String = "INSERT INTO Inventaires (ReferenceInventaire, CreePar, Statut, Observation) VALUES (@ReferenceInventaire, @CreePar, N'EN_COURS', @Observation); SELECT CAST(SCOPE_IDENTITY() AS INT);"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@ReferenceInventaire", referenceInventaire),
                New SqlParameter("@CreePar", creePar),
                New SqlParameter("@Observation", If(String.IsNullOrWhiteSpace(observation), CType(DBNull.Value, Object), observation))
            }
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(v)
        End Function

        Public Function AnnulerInventaire(inventaireId As Integer, annulePar As Integer, motif As String) As Integer
            Dim sql As String = "UPDATE Inventaires SET Statut=N'ANNULÉ', ValidePar=@ValidePar, DateValidation=SYSDATETIME(), Observation = CASE WHEN ISNULL(Observation,'')='' THEN @Observation ELSE CONCAT(Observation, CHAR(13)+CHAR(10), @Observation) END WHERE InventaireId=@InventaireId"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@ValidePar", annulePar),
                New SqlParameter("@Observation", If(String.IsNullOrWhiteSpace(motif), CType(DBNull.Value, Object), motif)),
                New SqlParameter("@InventaireId", inventaireId)
            }
            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function

        Public Function MarquerValide(inventaireId As Integer, validePar As Integer) As Integer
            Dim sql As String = "UPDATE Inventaires SET Statut=N'VALIDÉ', ValidePar=@ValidePar, DateValidation=SYSDATETIME() WHERE InventaireId=@InventaireId"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@ValidePar", validePar),
                New SqlParameter("@InventaireId", inventaireId)
            }
            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function

        Public Function InitialiserLignesInventaire(inventaireId As Integer) As Integer
            Dim sqlDelete As String = "DELETE FROM InventaireLignes WHERE InventaireId=@InventaireId"
            Dim sqlInsert As String = "" &
                "INSERT INTO InventaireLignes (InventaireId, ProduitId, StockTheorique, StockPhysique, Ecart, Statut, Motif, DateComptage) " &
                "SELECT @InventaireId, p.ProduitId, ISNULL(s.QuantiteStock,0), NULL, NULL, N'NON_COMPTE', NULL, NULL " &
                "FROM Produits p " &
                "LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId " &
                "WHERE p.EstActif = 1"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@InventaireId", inventaireId)}
            _dal.ExecuterNonRequete(sqlDelete, CommandType.Text, p)
            Return _dal.ExecuterNonRequete(sqlInsert, CommandType.Text, p)
        End Function

        Public Function ChargerLignesInventaire(inventaireId As Integer) As DataTable
            Dim sql As String = "" &
                "SELECT il.LigneInventaireId, il.InventaireId, il.ProduitId, " &
                "       p.CodeBarres AS CodeProduit, p.Libelle AS NomProduit, " &
                "       ISNULL(cat.NomCategorie, '') AS Categorie, " &
                "       ISNULL(il.StockTheorique, 0) AS StockTheorique, " &
                "       il.StockPhysique, il.Ecart, ISNULL(il.Statut, N'NON_COMPTE') AS Statut, " &
                "       ISNULL(il.Motif, '') AS Motif, " &
                "       CASE WHEN il.StockPhysique IS NULL THEN N'NON_COMPTÉ' ELSE N'COMPTÉ' END AS StatutComptage, " &
                "       p.ConversionUnite, p.PrixAchat " &
                "FROM InventaireLignes il " &
                "INNER JOIN Produits p ON p.ProduitId = il.ProduitId " &
                "LEFT JOIN CategoriesProduits cat ON cat.CategorieId = p.CategorieId " &
                "WHERE il.InventaireId = @InventaireId " &
                "ORDER BY p.Libelle"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@InventaireId", inventaireId)}
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        Public Function RemplacerLignesInventaire(inventaireId As Integer, lignes As DataTable) As Integer
            If lignes Is Nothing Then
                Throw New Exception("Aucune ligne d'inventaire à enregistrer.")
            End If

            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        Using cmdDelete As New SqlCommand("DELETE FROM InventaireLignes WHERE InventaireId=@InventaireId", cn, tx)
                            cmdDelete.Parameters.AddWithValue("@InventaireId", inventaireId)
                            cmdDelete.ExecuteNonQuery()
                        End Using

                        Dim sqlInsert As String = "INSERT INTO InventaireLignes (InventaireId, ProduitId, StockTheorique, StockPhysique, Ecart, Statut, Motif, DateComptage) " &
                                                   "VALUES (@InventaireId, @ProduitId, @StockTheorique, @StockPhysique, @Ecart, @Statut, @Motif, @DateComptage)"
                        For Each row As DataRow In lignes.Rows
                            Dim stockPhysiqueValue As Object = If(row.IsNull("StockPhysique"), CType(DBNull.Value, Object), row("StockPhysique"))
                            Dim ecartValue As Object = If(row.IsNull("Ecart"), CType(DBNull.Value, Object), row("Ecart"))
                            Dim statutValue As Object = If(row.IsNull("Statut"), CType(DBNull.Value, Object), row("Statut"))
                            Dim motifValue As Object = If(row.IsNull("Motif"), CType(DBNull.Value, Object), row("Motif"))
                            Dim dateComptageValue As Object = If(row.Table.Columns.Contains("DateComptage") AndAlso Not row.IsNull("DateComptage"), row("DateComptage"), CType(DBNull.Value, Object))

                            Using cmd As New SqlCommand(sqlInsert, cn, tx)
                                cmd.Parameters.AddWithValue("@InventaireId", inventaireId)
                                cmd.Parameters.AddWithValue("@ProduitId", Convert.ToInt32(row("ProduitId")))
                                cmd.Parameters.AddWithValue("@StockTheorique", Convert.ToDecimal(If(row.IsNull("StockTheorique"), 0D, row("StockTheorique"))))
                                cmd.Parameters.AddWithValue("@StockPhysique", stockPhysiqueValue)
                                cmd.Parameters.AddWithValue("@Ecart", ecartValue)
                                cmd.Parameters.AddWithValue("@Statut", statutValue)
                                cmd.Parameters.AddWithValue("@Motif", motifValue)
                                cmd.Parameters.AddWithValue("@DateComptage", dateComptageValue)
                                cmd.ExecuteNonQuery()
                            End Using
                        Next

                        tx.Commit()
                        Return lignes.Rows.Count
                    Catch
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Public Function CompterNonComptes(inventaireId As Integer) As Integer
            Dim sql As String = "SELECT COUNT(*) FROM InventaireLignes WHERE InventaireId=@InventaireId AND StockPhysique IS NULL"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@InventaireId", inventaireId)}
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(v)
        End Function

        Public Function ChargerHistoriqueStockInventaire(produitId As Integer) As DataTable
            Dim sql As String = "" &
                "SELECT StockInventaireId, ProduitId, StockTheorique, StockReel, Ecart, DateInventaire, CreePar, Observation " &
                "FROM StockInventaire WHERE ProduitId=@ProduitId ORDER BY DateInventaire DESC"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@ProduitId", produitId)}
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        Public Function ChargerHistoriqueEntrees(produitId As Integer) As DataTable
            Dim sql As String = "SELECT DateEntree, QuantiteBase, PrixAchat, Devise, Taux FROM StockEntree WHERE ProduitId=@ProduitId ORDER BY DateEntree DESC"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@ProduitId", produitId)}
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        Public Function ChargerHistoriqueSorties(produitId As Integer) As DataTable
            Dim sql As String = "SELECT DateSortie, QuantiteBase, Source, RefSource, TypeVente, StatutPaiement, MontantLigne, Observation FROM StockSortie WHERE ProduitId=@ProduitId ORDER BY DateSortie DESC"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@ProduitId", produitId)}
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function
    End Class
End Namespace
