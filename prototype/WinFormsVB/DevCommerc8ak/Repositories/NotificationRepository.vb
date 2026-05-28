Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class NotificationRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerTable()
        End Sub

        Public Sub AssurerTable()
            ' Schéma géré par le script SQL de déploiement.
        End Sub

        Public Sub Ajouter(message As String)
            Dim sql As String = "INSERT INTO Notifications (TypeNotification, Message, CompteurOccurrences, EstGroupee) VALUES ('Info', @Message, 1, 0)"
            Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                New System.Data.SqlClient.SqlParameter("@Message", message)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Public Sub AjouterOuMettreAJour(dto As NotificationDTO, minutesAntiRepetition As Integer)
            AssurerTable()
            Dim sqlRecherche As String = "SELECT TOP 1 NotificationId, CompteurOccurrences, CreeLe FROM Notifications WHERE Lue=0 AND CleNotification=@CleNotification ORDER BY CreeLe DESC"
            Dim pRecherche As New List(Of System.Data.SqlClient.SqlParameter) From {
                New System.Data.SqlClient.SqlParameter("@CleNotification", If(dto.CleNotification, CType(DBNull.Value, Object)))
            }
            Dim dt As DataTable = _dal.ExecuterTable(sqlRecherche, CommandType.Text, pRecherche)
            If dt.Rows.Count > 0 Then
                Dim creeLe As Date = Convert.ToDateTime(dt.Rows(0)("CreeLe"))
                If (Date.Now - creeLe).TotalMinutes < minutesAntiRepetition Then
                    Dim sqlUpdate As String = "UPDATE Notifications SET Message=@Message, TypeNotification=@TypeNotification, EcranCible=@EcranCible, DonneesCible=@DonneesCible, CompteurOccurrences=CompteurOccurrences+1, EstGroupee=@EstGroupee, CreeLe=SYSUTCDATETIME(), Lue=0 WHERE NotificationId=@NotificationId"
                    Dim pUpdate As New List(Of System.Data.SqlClient.SqlParameter) From {
                        New System.Data.SqlClient.SqlParameter("@Message", dto.Message),
                        New System.Data.SqlClient.SqlParameter("@TypeNotification", dto.TypeNotification),
                        New System.Data.SqlClient.SqlParameter("@EcranCible", If(dto.EcranCible, CType(DBNull.Value, Object))),
                        New System.Data.SqlClient.SqlParameter("@DonneesCible", If(dto.DonneesCible, CType(DBNull.Value, Object))),
                        New System.Data.SqlClient.SqlParameter("@EstGroupee", dto.EstGroupee),
                        New System.Data.SqlClient.SqlParameter("@NotificationId", Convert.ToInt32(dt.Rows(0)("NotificationId")))
                    }
                    _dal.ExecuterNonRequete(sqlUpdate, CommandType.Text, pUpdate)
                    Return
                End If
            End If

            Dim sql As String = "INSERT INTO Notifications (TypeNotification, Message, CleNotification, EcranCible, DonneesCible, CompteurOccurrences, EstGroupee) " &
                                "VALUES (@TypeNotification, @Message, @CleNotification, @EcranCible, @DonneesCible, @CompteurOccurrences, @EstGroupee)"
            Dim p As New List(Of System.Data.SqlClient.SqlParameter) From {
                New System.Data.SqlClient.SqlParameter("@TypeNotification", dto.TypeNotification),
                New System.Data.SqlClient.SqlParameter("@Message", dto.Message),
                New System.Data.SqlClient.SqlParameter("@CleNotification", If(dto.CleNotification, CType(DBNull.Value, Object))),
                New System.Data.SqlClient.SqlParameter("@EcranCible", If(dto.EcranCible, CType(DBNull.Value, Object))),
                New System.Data.SqlClient.SqlParameter("@DonneesCible", If(dto.DonneesCible, CType(DBNull.Value, Object))),
                New System.Data.SqlClient.SqlParameter("@CompteurOccurrences", Math.Max(1, dto.CompteurOccurrences)),
                New System.Data.SqlClient.SqlParameter("@EstGroupee", dto.EstGroupee)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Public Function ListerNonLues() As DataTable
            Dim sql As String = "SELECT NotificationId, TypeNotification, Message, CleNotification, EcranCible, DonneesCible, CompteurOccurrences, EstGroupee, CreeLe FROM Notifications WHERE Lue=0 ORDER BY CreeLe DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        Public Function ListerToutes() As DataTable
            Dim sql As String = "SELECT NotificationId, TypeNotification, Message, CleNotification, EcranCible, DonneesCible, CompteurOccurrences, EstGroupee, CreeLe, Lue FROM Notifications ORDER BY CreeLe DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        Public Function CompterNonLues() As Integer
            Dim sql As String = "SELECT COUNT(*) FROM Notifications WHERE Lue=0"
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, Nothing)
            Return Convert.ToInt32(v)
        End Function

        Public Sub MarquerLues()
            Dim sql As String = "UPDATE Notifications SET Lue=1 WHERE Lue=0"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub

        Public Sub ViderHistorique()
            Dim sql As String = "DELETE FROM Notifications"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub
    End Class
End Namespace
