Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class StockEntreeRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerTable()
        End Sub

        Public Sub AssurerTable()
            ' Schéma géré par le script SQL de déploiement.
        End Sub

        Public Function Ajouter(entree As StockEntree) As Integer
            Dim sql As String = "INSERT INTO StockEntree (IdStock, ProduitId, QuantiteSaisie, Unite, QuantiteBase, PrixAchat, Devise, Taux, DateEntree, FournisseurId, CreePar) " &
                                "VALUES (@IdStock, @ProduitId, @QuantiteSaisie, @Unite, @QuantiteBase, @PrixAchat, @Devise, @Taux, @DateEntree, @FournisseurId, @CreePar); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@IdStock", entree.IdStock),
                New SqlParameter("@ProduitId", entree.ProduitId),
                New SqlParameter("@QuantiteSaisie", entree.QuantiteSaisie),
                New SqlParameter("@Unite", If(entree.Unite, CType(DBNull.Value, Object))),
                New SqlParameter("@QuantiteBase", entree.QuantiteBase),
                New SqlParameter("@PrixAchat", entree.PrixAchat),
                New SqlParameter("@Devise", If(entree.Devise, CType(DBNull.Value, Object))),
                New SqlParameter("@Taux", entree.Taux),
                New SqlParameter("@DateEntree", entree.DateEntree),
                New SqlParameter("@FournisseurId", If(entree.FournisseurId.HasValue, CType(entree.FournisseurId.Value, Object), DBNull.Value)),
                New SqlParameter("@CreePar", If(entree.CreePar.HasValue, CType(entree.CreePar.Value, Object), DBNull.Value))
            }
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(v)
        End Function
    End Class
End Namespace
