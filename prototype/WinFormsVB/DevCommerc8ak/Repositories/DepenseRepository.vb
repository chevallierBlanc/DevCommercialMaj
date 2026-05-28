Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class DepenseRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Function Ajouter(depense As Depense) As Integer
            Dim sql As String = "INSERT INTO Depenses (Categorie, Montant, Devise, Description, DateDepense, Source, TypeDepense, CreePar) " &
                                "VALUES (@Categorie, @Montant, @Devise, @Description, @DateDepense, @Source, @TypeDepense, @CreePar); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@Categorie", If(String.IsNullOrWhiteSpace(depense.Categorie), CType(DBNull.Value, Object), depense.Categorie)),
                New SqlParameter("@Montant", depense.Montant),
                New SqlParameter("@Devise", If(String.IsNullOrWhiteSpace(depense.Devise), CType(DBNull.Value, Object), depense.Devise)),
                New SqlParameter("@Description", If(String.IsNullOrWhiteSpace(depense.Description), CType(DBNull.Value, Object), depense.Description)),
                New SqlParameter("@DateDepense", depense.DateDepense),
                New SqlParameter("@Source", If(String.IsNullOrWhiteSpace(depense.Source), CType(DBNull.Value, Object), depense.Source)),
                New SqlParameter("@TypeDepense", If(String.IsNullOrWhiteSpace(depense.TypeDepense), CType(DBNull.Value, Object), depense.TypeDepense)),
                New SqlParameter("@CreePar", If(String.IsNullOrWhiteSpace(depense.CreePar), CType(DBNull.Value, Object), depense.CreePar))
            }
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(v)
        End Function
    End Class
End Namespace
