Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class MouvementStockRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerColonnes()
        End Sub

        Private Sub AssurerColonnes()
            Dim sql As String = "" &
                "IF COL_LENGTH('MouvementsStock','NumeroMouvement') IS NULL ALTER TABLE MouvementsStock ADD NumeroMouvement NVARCHAR(30) NULL; " &
                "IF COL_LENGTH('MouvementsStock','QuantiteBase') IS NULL ALTER TABLE MouvementsStock ADD QuantiteBase DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('MouvementsStock','Unite') IS NULL ALTER TABLE MouvementsStock ADD Unite NVARCHAR(50) NULL; " &
                "IF COL_LENGTH('MouvementsStock','StockAvant') IS NULL ALTER TABLE MouvementsStock ADD StockAvant DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('MouvementsStock','StockApres') IS NULL ALTER TABLE MouvementsStock ADD StockApres DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('MouvementsStock','Observation') IS NULL ALTER TABLE MouvementsStock ADD Observation NVARCHAR(200) NULL; " &
                "IF COL_LENGTH('MouvementsStock','TypePerte') IS NULL ALTER TABLE MouvementsStock ADD TypePerte NVARCHAR(50) NULL; " &
                "IF COL_LENGTH('MouvementsStock','EstAnnule') IS NULL ALTER TABLE MouvementsStock ADD EstAnnule BIT NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('MouvementsStock','AnnulePar') IS NULL ALTER TABLE MouvementsStock ADD AnnulePar INT NULL; " &
                "IF COL_LENGTH('MouvementsStock','AnnuleLe') IS NULL ALTER TABLE MouvementsStock ADD AnnuleLe DATETIME NULL; " &
                "IF COL_LENGTH('MouvementsStock','AnnulationRef') IS NULL ALTER TABLE MouvementsStock ADD AnnulationRef NVARCHAR(200) NULL; " &
                "IF COL_LENGTH('MouvementsStock','ModifierPar') IS NULL ALTER TABLE MouvementsStock ADD ModifierPar NVARCHAR(80) NULL;"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub

        ' Cree un mouvement stock et retourne son identifiant.
        Public Function Ajouter(mouvement As MouvementStock) As Integer
            Dim sql As String = "INSERT INTO MouvementsStock (NumeroMouvement, ProduitId, TypeMouvement, Quantite, QuantiteBase, Unite, StockAvant, StockApres, Reference, Observation, TypePerte, EffectuePar, ModifierPar) " &
                                "VALUES (@NumeroMouvement, @ProduitId, @TypeMouvement, @Quantite, @QuantiteBase, @Unite, @StockAvant, @StockApres, @Reference, @Observation, @TypePerte, @EffectuePar, @ModifierPar); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"

            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@NumeroMouvement", If(mouvement.NumeroMouvement, CType(DBNull.Value, Object))),
                New SqlParameter("@ProduitId", mouvement.ProduitId),
                New SqlParameter("@TypeMouvement", mouvement.TypeMouvement),
                New SqlParameter("@Quantite", mouvement.Quantite),
                New SqlParameter("@QuantiteBase", mouvement.QuantiteBase),
                New SqlParameter("@Unite", If(mouvement.Unite, CType(DBNull.Value, Object))),
                New SqlParameter("@StockAvant", mouvement.StockAvant),
                New SqlParameter("@StockApres", mouvement.StockApres),
                New SqlParameter("@Reference", If(mouvement.Reference, CType(DBNull.Value, Object))),
                New SqlParameter("@Observation", If(mouvement.Observation, CType(DBNull.Value, Object))),
                New SqlParameter("@TypePerte", If(mouvement.TypePerte, CType(DBNull.Value, Object))),
                New SqlParameter("@EffectuePar", mouvement.EffectuePar),
                New SqlParameter("@ModifierPar", SessionUtilisateur.NomUtilisateur)
            }

            Dim id As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(id)
        End Function

        ' Liste des mouvements stock par produit.
        Public Function ListerParProduit(produitId As Integer) As List(Of MouvementStockDTO)
            Dim sql As String = "SELECT MouvementStockId, NumeroMouvement, ProduitId, TypeMouvement, Quantite, QuantiteBase, Unite, StockAvant, StockApres, Reference, Observation, TypePerte, EffectueLe, EstAnnule, AnnulePar, AnnuleLe, AnnulationRef " &
                                "FROM MouvementsStock WHERE ProduitId = @ProduitId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@ProduitId", produitId)}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            Dim liste As New List(Of MouvementStockDTO)()

            For Each row As DataRow In dt.Rows
                liste.Add(MapVersDTO(row))
            Next

            Return liste
        End Function

        ' Liste tous les mouvements stock.
        Public Function ListerTous() As List(Of MouvementStockDTO)
            Dim sql As String = "SELECT MouvementStockId, NumeroMouvement, ProduitId, TypeMouvement, Quantite, QuantiteBase, Unite, StockAvant, StockApres, Reference, Observation, TypePerte, EffectueLe, EstAnnule, AnnulePar, AnnuleLe, AnnulationRef " &
                                "FROM MouvementsStock ORDER BY EffectueLe DESC"
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, Nothing)
            Dim liste As New List(Of MouvementStockDTO)()

            For Each row As DataRow In dt.Rows
                liste.Add(MapVersDTO(row))
            Next

            Return liste
        End Function

        ' Supprime un mouvement stock.
        Public Function Supprimer(mouvementStockId As Integer) As Integer
            Dim sql As String = "DELETE FROM MouvementsStock WHERE MouvementStockId = @MouvementStockId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@MouvementStockId", mouvementStockId)}
            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function

        Private Function MapVersDTO(row As DataRow) As MouvementStockDTO
            Return New MouvementStockDTO With {
                .MouvementStockId = Convert.ToInt32(row("MouvementStockId")),
                .NumeroMouvement = If(row.IsNull("NumeroMouvement"), Nothing, Convert.ToString(row("NumeroMouvement"))),
                .ProduitId = Convert.ToInt32(row("ProduitId")),
                .TypeMouvement = Convert.ToString(row("TypeMouvement")),
                .Quantite = If(row.IsNull("Quantite"), 0D, Convert.ToDecimal(row("Quantite"))),
                .QuantiteBase = If(row.IsNull("QuantiteBase"), 0D, Convert.ToDecimal(row("QuantiteBase"))),
                .Unite = If(row.IsNull("Unite"), Nothing, Convert.ToString(row("Unite"))),
                .StockAvant = If(row.IsNull("StockAvant"), 0D, Convert.ToDecimal(row("StockAvant"))),
                .StockApres = If(row.IsNull("StockApres"), 0D, Convert.ToDecimal(row("StockApres"))),
                .Reference = If(row.IsNull("Reference"), Nothing, Convert.ToString(row("Reference"))),
                .Observation = If(row.IsNull("Observation"), Nothing, Convert.ToString(row("Observation"))),
                .TypePerte = If(row.IsNull("TypePerte"), Nothing, Convert.ToString(row("TypePerte"))),
                .EffectueLe = If(row.IsNull("EffectueLe"), Date.Now, Convert.ToDateTime(row("EffectueLe"))),
                .EstAnnule = If(row.IsNull("EstAnnule"), False, Convert.ToBoolean(row("EstAnnule"))),
                .AnnulePar = If(row.IsNull("AnnulePar"), CType(Nothing, Integer?), Convert.ToInt32(row("AnnulePar"))),
                .AnnuleLe = If(row.IsNull("AnnuleLe"), CType(Nothing, Date?), Convert.ToDateTime(row("AnnuleLe"))),
                .AnnulationRef = If(row.IsNull("AnnulationRef"), Nothing, Convert.ToString(row("AnnulationRef")))
            }
        End Function

        ' Retourne un mouvement par identifiant.
        Public Function ObtenirParId(mouvementStockId As Integer) As MouvementStockDTO
            Dim sql As String = "SELECT MouvementStockId, NumeroMouvement, ProduitId, TypeMouvement, Quantite, QuantiteBase, Unite, StockAvant, StockApres, Reference, Observation, TypePerte, EffectueLe, EstAnnule, AnnulePar, AnnuleLe, AnnulationRef " &
                                "FROM MouvementsStock WHERE MouvementStockId=@id"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@id", mouvementStockId)}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            If dt.Rows.Count = 0 Then Return Nothing
            Return MapVersDTO(dt.Rows(0))
        End Function
    End Class
End Namespace
