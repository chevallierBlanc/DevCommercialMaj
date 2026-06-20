Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class StockInventaireRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerTable()
        End Sub

        Public Sub AssurerTable()
            ' Schéma géré par le script SQL de déploiement.
        End Sub

        Public Function Ajouter(inv As StockInventaire, Optional cn As SqlConnection = Nothing, Optional tx As SqlTransaction = Nothing) As Integer
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
            If cn Is Nothing Then
                Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
                Return Convert.ToInt32(v)
            End If

            Dim ownsConnection As Boolean = False
            If cn.State <> ConnectionState.Open Then
                cn.Open()
                ownsConnection = True
            End If
            Try
                Using cmd As New SqlCommand(sql, cn)
                    If tx IsNot Nothing Then cmd.Transaction = tx
                    cmd.Parameters.AddRange(p.ToArray())
                    Dim v As Object = cmd.ExecuteScalar()
                    Return Convert.ToInt32(v)
                End Using
            Finally
                If ownsConnection Then
                    cn.Close()
                End If
            End Try
        End Function
    End Class
End Namespace
