Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class DepensesNonSynchroniseesRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Function Ajouter(jsonData As String, Optional statutSync As String = "EN_ATTENTE", Optional messageErreur As String = Nothing) As Integer
            Dim sql As String = "INSERT INTO DepensesNonSynchronisees (JsonData, StatutSync, MessageErreur) VALUES (@JsonData, @StatutSync, @MessageErreur); SELECT CAST(SCOPE_IDENTITY() AS INT);"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@JsonData", jsonData),
                New SqlParameter("@StatutSync", statutSync),
                New SqlParameter("@MessageErreur", If(String.IsNullOrWhiteSpace(messageErreur), CType(DBNull.Value, Object), messageErreur))
            }
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(v)
        End Function

        Public Function ListerEnAttente() As DataTable
            Dim sql As String = "SELECT Id, JsonData, DateCreation, NombreTentatives, DerniereTentative, StatutSync, MessageErreur FROM DepensesNonSynchronisees WHERE StatutSync <> 'SYNC_OK' ORDER BY DateCreation ASC"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        Public Sub MarquerResultat(id As Integer, statutSync As String, messageErreur As String, nombreTentatives As Integer)
            Dim sql As String = "UPDATE DepensesNonSynchronisees SET StatutSync=@StatutSync, MessageErreur=@MessageErreur, NombreTentatives=@Nb, DerniereTentative=SYSUTCDATETIME() WHERE Id=@Id"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@StatutSync", statutSync),
                New SqlParameter("@MessageErreur", If(String.IsNullOrWhiteSpace(messageErreur), CType(DBNull.Value, Object), messageErreur)),
                New SqlParameter("@Nb", nombreTentatives),
                New SqlParameter("@Id", id)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub
    End Class
End Namespace
