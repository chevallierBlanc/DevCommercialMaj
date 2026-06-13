Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class UtilisateurRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        ' Cree un utilisateur et retourne son identifiant.
        Public Function Ajouter(utilisateur As Utilisateur, roleId As Integer) As Integer
            Dim sql As String = "INSERT INTO Utilisateurs (NomUtilisateur, MotDePasseHash, MotDePasseSel, EstActif) " &
                                "VALUES (@NomUtilisateur, @MotDePasseHash, @MotDePasseSel, @EstActif); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"

            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@NomUtilisateur", utilisateur.NomUtilisateur),
                New SqlParameter("@MotDePasseHash", utilisateur.MotDePasseHash),
                New SqlParameter("@MotDePasseSel", utilisateur.MotDePasseSel),
                New SqlParameter("@EstActif", utilisateur.EstActif)
            }

            Dim id As Integer = Convert.ToInt32(_dal.ExecuterScalaire(sql, CommandType.Text, p))
            AssignerRole(id, roleId)
            Return id
        End Function

        ' Assigne un role a un utilisateur.
        Public Sub AssignerRole(utilisateurId As Integer, roleId As Integer)
            Dim sql As String = "IF NOT EXISTS (SELECT 1 FROM UtilisateurRoles WHERE UtilisateurId=@UtilisateurId AND RoleId=@RoleId) " &
                                "INSERT INTO UtilisateurRoles (UtilisateurId, RoleId) VALUES (@UtilisateurId, @RoleId);"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@UtilisateurId", utilisateurId),
                New SqlParameter("@RoleId", roleId)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        ' Met a jour le compte et le role associe.
        Public Sub MettreAJour(utilisateurId As Integer, nomUtilisateur As String, estActif As Boolean, roleId As Integer, Optional hash As Byte() = Nothing, Optional sel As Byte() = Nothing)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        Dim sqlUpdate As String = "UPDATE Utilisateurs SET NomUtilisateur=@NomUtilisateur, EstActif=@EstActif"
                        If hash IsNot Nothing AndAlso sel IsNot Nothing Then
                            sqlUpdate &= ", MotDePasseHash=@MotDePasseHash, MotDePasseSel=@MotDePasseSel"
                        End If
                        sqlUpdate &= " WHERE UtilisateurId=@UtilisateurId"

                        Using cmdUpdate As New SqlCommand(sqlUpdate, cn, tx)
                            cmdUpdate.Parameters.AddWithValue("@NomUtilisateur", nomUtilisateur)
                            cmdUpdate.Parameters.AddWithValue("@EstActif", estActif)
                            cmdUpdate.Parameters.AddWithValue("@UtilisateurId", utilisateurId)
                            If hash IsNot Nothing AndAlso sel IsNot Nothing Then
                                cmdUpdate.Parameters.AddWithValue("@MotDePasseHash", hash)
                                cmdUpdate.Parameters.AddWithValue("@MotDePasseSel", sel)
                            End If
                            cmdUpdate.ExecuteNonQuery()
                        End Using

                        Using cmdDeleteRole As New SqlCommand("DELETE FROM UtilisateurRoles WHERE UtilisateurId=@UtilisateurId", cn, tx)
                            cmdDeleteRole.Parameters.AddWithValue("@UtilisateurId", utilisateurId)
                            cmdDeleteRole.ExecuteNonQuery()
                        End Using

                        Using cmdInsertRole As New SqlCommand("INSERT INTO UtilisateurRoles (UtilisateurId, RoleId) VALUES (@UtilisateurId, @RoleId)", cn, tx)
                            cmdInsertRole.Parameters.AddWithValue("@UtilisateurId", utilisateurId)
                            cmdInsertRole.Parameters.AddWithValue("@RoleId", roleId)
                            cmdInsertRole.ExecuteNonQuery()
                        End Using

                        tx.Commit()
                    Catch
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        ' Recupere un utilisateur par nom.
        Public Function ObtenirParNom(nomUtilisateur As String) As Utilisateur
            Dim sql As String = "SELECT UtilisateurId, NomUtilisateur, MotDePasseHash, MotDePasseSel, EstActif, CreeLe " &
                                "FROM Utilisateurs WHERE NomUtilisateur = @NomUtilisateur"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@NomUtilisateur", nomUtilisateur)}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            If dt.Rows.Count = 0 Then Return Nothing

            Dim row As DataRow = dt.Rows(0)
            Return New Utilisateur With {
                .UtilisateurId = Convert.ToInt32(row("UtilisateurId")),
                .NomUtilisateur = Convert.ToString(row("NomUtilisateur")),
                .MotDePasseHash = CType(row("MotDePasseHash"), Byte()),
                .MotDePasseSel = CType(row("MotDePasseSel"), Byte()),
                .EstActif = Convert.ToBoolean(row("EstActif")),
                .CreeLe = Convert.ToDateTime(row("CreeLe"))
            }
        End Function

        ' Liste des utilisateurs avec role.
        Public Function Lister() As List(Of UtilisateurDTO)
            Dim sql As String = "SELECT u.UtilisateurId, u.NomUtilisateur, u.EstActif, r.NomRole " &
                                "FROM Utilisateurs u " &
                                "LEFT JOIN UtilisateurRoles ur ON u.UtilisateurId = ur.UtilisateurId " &
                                "LEFT JOIN Roles r ON ur.RoleId = r.RoleId"
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, Nothing)
            Dim liste As New List(Of UtilisateurDTO)()

            For Each row As DataRow In dt.Rows
                liste.Add(New UtilisateurDTO With {
                    .UtilisateurId = Convert.ToInt32(row("UtilisateurId")),
                    .NomUtilisateur = Convert.ToString(row("NomUtilisateur")),
                    .EstActif = Convert.ToBoolean(row("EstActif")),
                    .Role = If(row.IsNull("NomRole"), "", Convert.ToString(row("NomRole")))
                })
            Next
            Return liste
        End Function

        ' Met a jour l'etat actif.
        Public Sub MettreAJourActif(utilisateurId As Integer, estActif As Boolean)
            Dim sql As String = "UPDATE Utilisateurs SET EstActif=@EstActif WHERE UtilisateurId=@UtilisateurId"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@EstActif", estActif),
                New SqlParameter("@UtilisateurId", utilisateurId)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        ' Met a jour le mot de passe.
        Public Sub MettreAJourMotDePasse(utilisateurId As Integer, hash As Byte(), sel As Byte())
            Dim sql As String = "UPDATE Utilisateurs SET MotDePasseHash=@MotDePasseHash, MotDePasseSel=@MotDePasseSel WHERE UtilisateurId=@UtilisateurId"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@MotDePasseHash", hash),
                New SqlParameter("@MotDePasseSel", sel),
                New SqlParameter("@UtilisateurId", utilisateurId)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        ' Retourne le role d'un utilisateur.
        Public Function ObtenirRole(utilisateurId As Integer) As String
            Dim sql As String = "SELECT r.NomRole FROM Roles r " &
                                "JOIN UtilisateurRoles ur ON r.RoleId = ur.RoleId " &
                                "WHERE ur.UtilisateurId = @UtilisateurId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@UtilisateurId", utilisateurId)}
            Dim role As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return If(role Is Nothing, "", Convert.ToString(role))
        End Function
    End Class
End Namespace
