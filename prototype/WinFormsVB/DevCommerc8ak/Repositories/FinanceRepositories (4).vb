Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic
Imports DevCommerc8ak.DevCommerc8ak.DTO

Namespace DevCommerc8ak.Finance
    Public Class CategorieDepenseRepository
        Private ReadOnly _dal As DAL
        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Sub Ajouter(libelle As String)
            Dim sql As String = "IF NOT EXISTS (SELECT 1 FROM CategoriesDepenses WHERE Libelle = @lib) " &
                               "INSERT INTO CategoriesDepenses (Libelle) VALUES (@lib)"
            Dim params As New List(Of SqlParameter) From {New SqlParameter("@lib", libelle)}
            _dal.ExecuterNonRequete(sql, CommandType.Text, params)
        End Sub

        Public Sub Supprimer(id As Integer)
            Dim sql As String = "DELETE FROM CategoriesDepenses WHERE Id = @id AND IsSystem = 0"
            Dim params As New List(Of SqlParameter) From {New SqlParameter("@id", id)}
            _dal.ExecuterNonRequete(sql, CommandType.Text, params)
        End Sub

        Public Function GetAll() As DataTable
            Return _dal.ExecuterTable("SELECT * FROM CategoriesDepenses ORDER BY Libelle", CommandType.Text, Nothing)
        End Function
    End Class

    Public Class DepenseRepositoryFinance
        Private ReadOnly _dal As DAL
        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Sub Ajouter(depense As DepenseDTOFinance)
            Dim sql As String = "INSERT INTO Depenses (Categorie, Montant, Devise, Description, DateDepense, Source, TypeDepense, CreePar) " &
                               "VALUES (@cat, @montant, @devise, @desc, @date, @source, @type, @user)"
            Dim descValue As Object = If(String.IsNullOrEmpty(depense.Description), DBNull.Value, DirectCast(depense.Description, Object))
            Dim userValue As Object = If(String.IsNullOrEmpty(depense.CreePar), DBNull.Value, DirectCast(depense.CreePar, Object))
            Dim params As New List(Of SqlParameter) From {
                New SqlParameter("@cat", depense.Categorie),
                New SqlParameter("@montant", depense.Montant),
                New SqlParameter("@devise", depense.Devise),
                New SqlParameter("@desc", descValue),
                New SqlParameter("@date", depense.DateDepense),
                New SqlParameter("@source", depense.Source),
                New SqlParameter("@type", depense.TypeDepense),
                New SqlParameter("@user", userValue)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, params)
        End Sub

        Public Function GetSommeParDevise(dateDepense As DateTime, devise As String, source As String) As Decimal
            Dim sql As String = "SELECT SUM(Montant) FROM Depenses WHERE DateDepense = @date AND Devise = @devise AND Source = @source"
            Dim params As New List(Of SqlParameter) From {
                New SqlParameter("@date", dateDepense.Date),
                New SqlParameter("@devise", devise),
                New SqlParameter("@source", source)
            }
            Dim result As Object = _dal.ExecuterScalaire(sql, CommandType.Text, params)
            Return If(result Is DBNull.Value OrElse result Is Nothing, 0D, Convert.ToDecimal(result))
        End Function

        Public Function GetAll() As DataTable
            Return _dal.ExecuterTable("SELECT * FROM Depenses ORDER BY CreatedAt DESC", CommandType.Text, Nothing)
        End Function

        Public Function GetHistorique(annee As Integer, Optional mois As Integer = 0) As DataTable
            Dim sql As String = "" &
                "SELECT " &
                "    Id, " &
                "    DateDepense, " &
                "    Categorie AS NomCategorie, " &
                "    Description, " &
                "    Montant, " &
                "    Devise, " &
                "    Source, " &
                "    TypeDepense, " &
                "    CreePar, " &
                "    CreatedAt " &
                "FROM Depenses " &
                "WHERE YEAR(DateDepense) = @annee "
            Dim params As New List(Of SqlParameter) From {
                New SqlParameter("@annee", annee)
            }

            If mois > 0 Then
                sql &= "AND MONTH(DateDepense) = @mois "
                params.Add(New SqlParameter("@mois", mois))
            End If

            sql &= "ORDER BY DateDepense DESC, CreatedAt DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, params)
        End Function

        Public Function GetStatsParCategorie() As DataTable
            Return _dal.ExecuterTable("SELECT Categorie, SUM(Montant) as Total FROM Depenses GROUP BY Categorie", CommandType.Text, Nothing)
        End Function

        Public Function GetRapportDepenses(annee As Integer, Optional mois As Integer = 0) As DataTable
            Dim sql As String = "SELECT Categorie, SUM(Montant) as Total, Devise " &
                               "FROM Depenses " &
                               "WHERE YEAR(DateDepense) = @annee "

            Dim params As New List(Of SqlParameter) From {New SqlParameter("@annee", annee)}

            If mois > 0 Then
                sql &= "AND MONTH(DateDepense) = @mois "
                params.Add(New SqlParameter("@mois", mois))
            End If

            sql &= "GROUP BY Categorie, Devise ORDER BY Categorie"

            Return _dal.ExecuterTable(sql, CommandType.Text, params)
        End Function
    End Class

    Public Class BanqueRepository
        Private ReadOnly _dal As DAL
        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Sub AjouterOperation(op As BanqueDTO)
            Dim sql As String = "INSERT INTO Banque (TypeOperation, Montant, Devise, Description, DateOperation, Reference) " &
                               "VALUES (@type, @montant, @devise, @desc, @date, @ref)"
            Dim descValue As Object = If(String.IsNullOrEmpty(op.Description), DBNull.Value, DirectCast(op.Description, Object))
            Dim refValue As Object = If(String.IsNullOrEmpty(op.Reference), DBNull.Value, DirectCast(op.Reference, Object))
            Dim params As New List(Of SqlParameter) From {
                New SqlParameter("@type", op.TypeOperation),
                New SqlParameter("@montant", op.Montant),
                New SqlParameter("@devise", op.Devise),
                New SqlParameter("@desc", descValue),
                New SqlParameter("@date", op.DateOperation),
                New SqlParameter("@ref", refValue)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, params)
        End Sub

        Public Function GetSoldeParDevise(devise As String) As Decimal
            Dim sql As String = "SELECT (SELECT ISNULL(SUM(Montant), 0) FROM Banque WHERE TypeOperation = 'Depot' AND Devise = @devise) - " &
                               "(SELECT ISNULL(SUM(Montant), 0) FROM Banque WHERE TypeOperation = 'Retrait' AND Devise = @devise)"
            Dim params As New List(Of SqlParameter) From {New SqlParameter("@devise", devise)}
            Dim result As Object = _dal.ExecuterScalaire(sql, CommandType.Text, params)
            Return If(result Is DBNull.Value OrElse result Is Nothing, 0D, Convert.ToDecimal(result))
        End Function

        Public Function GetHistorique() As DataTable
            Return _dal.ExecuterTable("SELECT * FROM Banque ORDER BY CreatedAt DESC", CommandType.Text, Nothing)
        End Function
    End Class

    Public Class CaisseRepository
        Private ReadOnly _dal As DAL
        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Function GetEncaisse(dateJour As DateTime, devise As String) As Decimal
            Dim sql As String = "SELECT SUM(Montant) FROM Paiements WHERE CAST(PayeLe AS DATE) = @date AND Devise = @devise"
            Dim params As New List(Of SqlParameter) From {
                New SqlParameter("@date", dateJour.Date),
                New SqlParameter("@devise", devise)
            }
            Dim result As Object = _dal.ExecuterScalaire(sql, CommandType.Text, params)
            Return If(result Is DBNull.Value OrElse result Is Nothing, 0D, Convert.ToDecimal(result))
        End Function

        Public Function GetDerniereCloture() As DateTime?
            Dim sql As String = "SELECT MAX(DateCloture) FROM CloturesJournalieres"
            Dim result As Object = _dal.ExecuterScalaire(sql, CommandType.Text, Nothing)
            Return If(result Is DBNull.Value OrElse result Is Nothing, CType(Nothing, DateTime?), Convert.ToDateTime(result))
        End Function

        Public Sub EnregistrerCloture(dateCloture As DateTime, fc As Decimal, usd As Decimal)
            Dim sql As String = "INSERT INTO CloturesJournalieres (DateCloture, MontantTransfertFC, MontantTransfertUSD) VALUES (@date, @fc, @usd)"
            Dim params As New List(Of SqlParameter) From {
                New SqlParameter("@date", dateCloture.Date),
                New SqlParameter("@fc", fc),
                New SqlParameter("@usd", usd)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, params)
        End Sub
    End Class
End Namespace
