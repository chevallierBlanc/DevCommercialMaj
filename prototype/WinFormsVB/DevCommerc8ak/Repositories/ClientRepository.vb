Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class ClientRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerColonnes()
        End Sub

        Private Sub AssurerColonnes()
            ' Schéma géré par le script SQL de déploiement.
        End Sub

        ' Cree un client et retourne son identifiant.
        Public Function Ajouter(client As Client) As Integer
            Dim sql As String = "INSERT INTO Clients (NomClient, Telephone, Email, Adresse, LimiteCredit, EstActif, ModifierPar) " &
                                "VALUES (@NomClient, @Telephone, @Email, @Adresse, @LimiteCredit, @EstActif, @ModifierPar); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"

            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@NomClient", client.NomClient),
                New SqlParameter("@Telephone", If(client.Telephone, CType(DBNull.Value, Object))),
                New SqlParameter("@Email", If(client.Email, CType(DBNull.Value, Object))),
                New SqlParameter("@Adresse", If(client.Adresse, CType(DBNull.Value, Object))),
                New SqlParameter("@LimiteCredit", client.LimiteCredit),
                New SqlParameter("@EstActif", client.EstActif),
                New SqlParameter("@ModifierPar", SessionUtilisateur.NomUtilisateur)
            }

            Dim id As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(id)
        End Function

        ' Retourne la liste des clients.
        Public Function Lister() As List(Of ClientDTO)
            Dim sql As String = "SELECT ClientId, NomClient, Telephone, Email, Adresse, LimiteCredit, EstActif FROM Clients"
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, Nothing)
            Dim liste As New List(Of ClientDTO)()

            For Each row As DataRow In dt.Rows
                liste.Add(MapVersDTO(row))
            Next

            Return liste
        End Function

        ' Retourne un client par identifiant.
        Public Function ObtenirParId(clientId As Integer) As ClientDTO
            Dim sql As String = "SELECT ClientId, NomClient, Telephone, Email, Adresse, LimiteCredit, EstActif FROM Clients WHERE ClientId = @ClientId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@ClientId", clientId)}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            If dt.Rows.Count = 0 Then
                Return Nothing
            End If
            Return MapVersDTO(dt.Rows(0))
        End Function

        ' Retourne un client par telephone.
        Public Function ObtenirParTelephone(telephone As String) As ClientDTO
            Dim sql As String = "SELECT TOP 1 ClientId, NomClient, Telephone, Email, Adresse, LimiteCredit, EstActif FROM Clients WHERE Telephone = @Telephone"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@Telephone", telephone)}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            If dt.Rows.Count = 0 Then
                Return Nothing
            End If
            Return MapVersDTO(dt.Rows(0))
        End Function

        ' Met a jour un client.
        Public Function MettreAJour(client As Client) As Integer
            Dim sql As String = "UPDATE Clients SET NomClient=@NomClient, Telephone=@Telephone, Email=@Email, Adresse=@Adresse, " &
                                "LimiteCredit=@LimiteCredit, EstActif=@EstActif, ModifierPar=@ModifierPar WHERE ClientId=@ClientId"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@NomClient", client.NomClient),
                New SqlParameter("@Telephone", If(client.Telephone, CType(DBNull.Value, Object))),
                New SqlParameter("@Email", If(client.Email, CType(DBNull.Value, Object))),
                New SqlParameter("@Adresse", If(client.Adresse, CType(DBNull.Value, Object))),
                New SqlParameter("@LimiteCredit", client.LimiteCredit),
                New SqlParameter("@EstActif", client.EstActif),
                New SqlParameter("@ClientId", client.ClientId),
                New SqlParameter("@ModifierPar", SessionUtilisateur.NomUtilisateur)
            }

            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function

        ' Supprime un client.
        Public Function Supprimer(clientId As Integer) As Integer
            Dim sql As String = "DELETE FROM Clients WHERE ClientId = @ClientId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@ClientId", clientId)}
            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function

        Private Function MapVersDTO(row As DataRow) As ClientDTO
            Return New ClientDTO With {
                .ClientId = Convert.ToInt32(row("ClientId")),
                .NomClient = Convert.ToString(row("NomClient")),
                .Telephone = If(row.IsNull("Telephone"), Nothing, Convert.ToString(row("Telephone"))),
                .Email = If(row.IsNull("Email"), Nothing, Convert.ToString(row("Email"))),
                .Adresse = If(row.IsNull("Adresse"), Nothing, Convert.ToString(row("Adresse"))),
                .LimiteCredit = Convert.ToDecimal(row("LimiteCredit")),
                .EstActif = Convert.ToBoolean(row("EstActif"))
            }
        End Function
    End Class
End Namespace
