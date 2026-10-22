Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Runtime.CompilerServices

Namespace DevCommerc8ak
    Public Class DAL
        Private ReadOnly _chaineConnexion As String

        Public Sub New(chaineConnexion As String)
            _chaineConnexion = chaineConnexion
        End Sub

        ' Cree une connexion SQL pour les transactions avancees.
        Public Function CreerConnexion(<CallerMemberName> Optional operation As String = Nothing) As SqlConnection
            Dim cn As New SqlConnection(_chaineConnexion)
            Dim debut As Stopwatch = Stopwatch.StartNew()
            Dim correlation As Guid = Guid.NewGuid()
            AddHandler cn.StateChange, Sub(sender, e)
                                           If e.CurrentState = ConnectionState.Broken Then
                                               Dim log As New ProductionLogService()
                                               log.Warn("SQL", operation, "Connexion rompue | UTC=" & DateTime.UtcNow.ToString("O") & " | DureeMs=" & debut.ElapsedMilliseconds.ToString() & " | Correlation=" & correlation.ToString("D") & " | Aucun rejeu automatique")
                                           End If
                                       End Sub
            Return cn
        End Function

        ' Execute une commande INSERT/UPDATE/DELETE.
        Public Function ExecuterNonRequete(nomProcedureOuSql As String, typeCommande As CommandType, parametres As List(Of SqlParameter), <CallerMemberName> Optional operation As String = Nothing) As Integer
            Return ExecuterUneFois(nomProcedureOuSql, typeCommande, parametres, Function(cmd) cmd.ExecuteNonQuery(), operation)
        End Function

        ' Execute une commande et retourne un scalaire.
        Public Function ExecuterScalaire(nomProcedureOuSql As String, typeCommande As CommandType, parametres As List(Of SqlParameter), <CallerMemberName> Optional operation As String = Nothing) As Object
            Return ExecuterUneFois(nomProcedureOuSql, typeCommande, parametres, Function(cmd) cmd.ExecuteScalar(), operation)
        End Function

        ' Execute une commande et retourne un DataTable.
        Public Function ExecuterTable(nomProcedureOuSql As String, typeCommande As CommandType, parametres As List(Of SqlParameter), <CallerMemberName> Optional operation As String = Nothing) As DataTable
            Return ExecuterUneFois(nomProcedureOuSql, typeCommande, parametres, Function(cmd)
                                                                                 Using da As New SqlDataAdapter(cmd)
                                                                                     Dim dt As New DataTable()
                                                                                     da.Fill(dt)
                                                                                     Return dt
                                                                                 End Using
                                                                             End Function, operation)
        End Function

        Friend Function ExecuterUneFois(Of T)(sql As String, typeCommande As CommandType, parametres As List(Of SqlParameter), executer As Func(Of SqlCommand, T), operation As String) As T
            Dim duree As Stopwatch = Stopwatch.StartNew()
            Dim correlation As Guid = Guid.NewGuid()
            Dim ouverture As Boolean = True
            Try
                Using cn As SqlConnection = CreerConnexion(operation)
                    Using cmd As New SqlCommand(sql, cn)
                        cmd.CommandType = typeCommande
                        If parametres IsNot Nothing Then
                            cmd.Parameters.AddRange(parametres.ToArray())
                        End If
                        cn.Open()
                        ouverture = False
                        ' Une commande scalaire/procedure peut aussi ecrire. Aucun
                        ' rejeu automatique, meme apres retablissement du reseau.
                        Return executer(cmd)
                    End Using
                End Using
            Catch ex As SqlException
                SqlDiagnosticService.Signaler(operation, ex, ouverture, Not ouverture, duree.ElapsedMilliseconds, correlation)
                Throw
            End Try
        End Function
    End Class
End Namespace
