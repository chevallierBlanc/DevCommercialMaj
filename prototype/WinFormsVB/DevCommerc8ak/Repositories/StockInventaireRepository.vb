Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient

Namespace DevCommerc8ak
    Public Class StockInventaireRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerTable()
        End Sub

        Public Sub AssurerTable()
            Dim sql As String = "" &
                "IF OBJECT_ID('dbo.StockInventaire','U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.StockInventaire (" &
                "StockInventaireId INT IDENTITY(1,1) PRIMARY KEY," &
                "ProduitId INT NOT NULL," &
                "StockTheorique DECIMAL(18,2) NOT NULL," &
                "StockReel DECIMAL(18,2) NOT NULL," &
                "Ecart DECIMAL(18,2) NOT NULL," &
                "DateInventaire DATETIME NOT NULL DEFAULT GETDATE()," &
                "CreePar INT NULL," &
                "Observation NVARCHAR(200) NULL" &
                "); END"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub

        Public Function Ajouter(inv As StockInventaire) As Integer
            Dim sql As String = "INSERT INTO StockInventaire (ProduitId, StockTheorique, StockReel, Ecart, DateInventaire, CreePar, Observation) " &
                                "VALUES (@ProduitId, @StockTheorique, @StockReel, @Ecart, @DateInventaire, @CreePar, @Observation); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@ProduitId", inv.ProduitId),
                New SqlParameter("@StockTheorique", inv.StockTheorique),
                New SqlParameter("@StockReel", inv.StockReel),
                New SqlParameter("@Ecart", inv.Ecart),
                New SqlParameter("@DateInventaire", inv.DateInventaire),
                New SqlParameter("@CreePar", If(inv.CreePar.HasValue, CType(inv.CreePar.Value, Object), DBNull.Value)),
                New SqlParameter("@Observation", If(inv.Observation, CType(DBNull.Value, Object)))
            }
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(v)
        End Function
    End Class
End Namespace
