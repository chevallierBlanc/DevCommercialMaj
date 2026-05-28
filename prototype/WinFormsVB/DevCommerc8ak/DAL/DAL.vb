Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class DAL
        Private ReadOnly _chaineConnexion As String

        Public Sub New(chaineConnexion As String)
            _chaineConnexion = chaineConnexion
        End Sub

        ' Cree une connexion SQL pour les transactions avancees.
        Public Function CreerConnexion() As SqlConnection
            Return New SqlConnection(_chaineConnexion)
        End Function

        ' Execute une commande INSERT/UPDATE/DELETE.
        Public Function ExecuterNonRequete(nomProcedureOuSql As String, typeCommande As CommandType, parametres As List(Of SqlParameter)) As Integer
            Using cn As New SqlConnection(_chaineConnexion)
                Using cmd As New SqlCommand(nomProcedureOuSql, cn)
                    cmd.CommandType = typeCommande
                    If parametres IsNot Nothing Then
                        cmd.Parameters.AddRange(parametres.ToArray())
                    End If
                    cn.Open()
                    Return cmd.ExecuteNonQuery()
                End Using
            End Using
        End Function

        ' Execute une commande et retourne un scalaire.
        Public Function ExecuterScalaire(nomProcedureOuSql As String, typeCommande As CommandType, parametres As List(Of SqlParameter)) As Object
            Using cn As New SqlConnection(_chaineConnexion)
                Using cmd As New SqlCommand(nomProcedureOuSql, cn)
                    cmd.CommandType = typeCommande
                    If parametres IsNot Nothing Then
                        cmd.Parameters.AddRange(parametres.ToArray())
                    End If
                    cn.Open()
                    Return cmd.ExecuteScalar()
                End Using
            End Using
        End Function

        ' Execute une commande et retourne un DataTable.
        Public Function ExecuterTable(nomProcedureOuSql As String, typeCommande As CommandType, parametres As List(Of SqlParameter)) As DataTable
            Using cn As New SqlConnection(_chaineConnexion)
                Using cmd As New SqlCommand(nomProcedureOuSql, cn)
                    cmd.CommandType = typeCommande
                    If parametres IsNot Nothing Then
                        cmd.Parameters.AddRange(parametres.ToArray())
                    End If
                    Using da As New SqlDataAdapter(cmd)
                        Dim dt As New DataTable()
                        da.Fill(dt)
                        Return dt
                    End Using
                End Using
            End Using
        End Function
    End Class
End Namespace
