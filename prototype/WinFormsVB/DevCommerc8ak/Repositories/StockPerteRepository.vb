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
            ' Schéma géré par le script SQL de déploiement.
        End Sub

        Public Function Ajouter(perte As StockPerte, Optional cn As SqlConnection = Nothing, Optional tx As SqlTransaction = Nothing) As Integer
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
