Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient

Namespace DevCommerc8ak
    Public Class StockSortieRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerTable()
        End Sub

        Public Sub AssurerTable()
            Dim sql As String = "" &
                "IF OBJECT_ID('dbo.StockSortie','U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.StockSortie (" &
                "StockSortieId INT IDENTITY(1,1) PRIMARY KEY," &
                "ProduitId INT NOT NULL," &
                "QuantiteSaisie DECIMAL(18,2) NOT NULL," &
                "Unite NVARCHAR(50) NULL," &
                "QuantiteBase DECIMAL(18,2) NOT NULL," &
                "DateSortie DATETIME NOT NULL DEFAULT GETDATE()," &
                "Source NVARCHAR(50) NOT NULL," &
                "RefSource NVARCHAR(50) NULL," &
                "CreePar INT NULL" &
                "); END"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub

        Public Function Ajouter(sortie As StockSortie) As Integer
            Dim sql As String = "INSERT INTO StockSortie (ProduitId, QuantiteSaisie, Unite, QuantiteBase, DateSortie, Source, RefSource, CreePar) " &
                                "VALUES (@ProduitId, @QuantiteSaisie, @Unite, @QuantiteBase, @DateSortie, @Source, @RefSource, @CreePar); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@ProduitId", sortie.ProduitId),
                New SqlParameter("@QuantiteSaisie", sortie.QuantiteSaisie),
                New SqlParameter("@Unite", If(sortie.Unite, CType(DBNull.Value, Object))),
                New SqlParameter("@QuantiteBase", sortie.QuantiteBase),
                New SqlParameter("@DateSortie", sortie.DateSortie),
                New SqlParameter("@Source", sortie.Source),
                New SqlParameter("@RefSource", If(sortie.RefSource, CType(DBNull.Value, Object))),
                New SqlParameter("@CreePar", If(sortie.CreePar.HasValue, CType(sortie.CreePar.Value, Object), DBNull.Value))
            }
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(v)
        End Function
    End Class
End Namespace
