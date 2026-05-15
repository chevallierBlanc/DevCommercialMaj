Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class StockPerteRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerTable()
        End Sub

        Public Sub AssurerTable()
            Dim sql As String = "" &
                "IF OBJECT_ID('dbo.StockPerte','U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.StockPerte (" &
                "StockPerteId INT IDENTITY(1,1) PRIMARY KEY," &
                "ProduitId INT NOT NULL," &
                "QuantiteSaisie DECIMAL(18,2) NOT NULL," &
                "Unite NVARCHAR(50) NULL," &
                "QuantiteBase DECIMAL(18,2) NOT NULL," &
                "TypePerte NVARCHAR(50) NULL," &
                "Motif NVARCHAR(200) NULL," &
                "DatePerte DATETIME NOT NULL DEFAULT GETDATE()," &
                "CreePar INT NULL" &
                "); END"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub

        Public Function Ajouter(perte As StockPerte) As Integer
            Dim sql As String = "INSERT INTO StockPerte (ProduitId, QuantiteSaisie, Unite, QuantiteBase, TypePerte, Motif, DatePerte, CreePar) " &
                                "VALUES (@ProduitId, @QuantiteSaisie, @Unite, @QuantiteBase, @TypePerte, @Motif, @DatePerte, @CreePar); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@ProduitId", perte.ProduitId),
                New SqlParameter("@QuantiteSaisie", perte.QuantiteSaisie),
                New SqlParameter("@Unite", If(perte.Unite, CType(DBNull.Value, Object))),
                New SqlParameter("@QuantiteBase", perte.QuantiteBase),
                New SqlParameter("@TypePerte", If(perte.TypePerte, CType(DBNull.Value, Object))),
                New SqlParameter("@Motif", If(perte.Motif, CType(DBNull.Value, Object))),
                New SqlParameter("@DatePerte", perte.DatePerte),
                New SqlParameter("@CreePar", If(perte.CreePar.HasValue, CType(perte.CreePar.Value, Object), DBNull.Value))
            }
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(v)
        End Function
    End Class
End Namespace
