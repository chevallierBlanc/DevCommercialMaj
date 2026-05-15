Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient

Namespace DevCommerc8ak
    Public Class FournisseurRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerColonnes()
        End Sub

        Private Sub AssurerColonnes()
            Dim sql As String = "IF COL_LENGTH('Fournisseurs','ModifierPar') IS NULL ALTER TABLE Fournisseurs ADD ModifierPar NVARCHAR(80) NULL;"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub

        ' Cree un fournisseur et retourne son identifiant.
        Public Function Ajouter(fournisseur As Fournisseur) As Integer
            Dim sql As String = "INSERT INTO Fournisseurs (NomFournisseur, Telephone, Email, Adresse, EstActif, ModifierPar) " &
                                "VALUES (@NomFournisseur, @Telephone, @Email, @Adresse, @EstActif, @ModifierPar); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"

            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@NomFournisseur", fournisseur.NomFournisseur),
                New SqlParameter("@Telephone", If(fournisseur.Telephone, CType(DBNull.Value, Object))),
                New SqlParameter("@Email", If(fournisseur.Email, CType(DBNull.Value, Object))),
                New SqlParameter("@Adresse", If(fournisseur.Adresse, CType(DBNull.Value, Object))),
                New SqlParameter("@EstActif", fournisseur.EstActif),
                New SqlParameter("@ModifierPar", SessionUtilisateur.NomUtilisateur)
            }

            Dim id As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(id)
        End Function

        ' Retourne la liste des fournisseurs.
        Public Function Lister() As List(Of FournisseurDTO)
            Dim sql As String = "SELECT FournisseurId, NomFournisseur, Telephone, Email, Adresse, EstActif FROM Fournisseurs"
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, Nothing)
            Dim liste As New List(Of FournisseurDTO)()

            For Each row As DataRow In dt.Rows
                liste.Add(MapVersDTO(row))
            Next

            Return liste
        End Function

        ' Retourne un fournisseur par identifiant.
        Public Function ObtenirParId(fournisseurId As Integer) As FournisseurDTO
            Dim sql As String = "SELECT FournisseurId, NomFournisseur, Telephone, Email, Adresse, EstActif FROM Fournisseurs WHERE FournisseurId = @FournisseurId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@FournisseurId", fournisseurId)}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            If dt.Rows.Count = 0 Then
                Return Nothing
            End If
            Return MapVersDTO(dt.Rows(0))
        End Function

        ' Met a jour un fournisseur.
        Public Function MettreAJour(fournisseur As Fournisseur) As Integer
            Dim sql As String = "UPDATE Fournisseurs SET NomFournisseur=@NomFournisseur, Telephone=@Telephone, Email=@Email, Adresse=@Adresse, " &
                                "EstActif=@EstActif, ModifierPar=@ModifierPar WHERE FournisseurId=@FournisseurId"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@NomFournisseur", fournisseur.NomFournisseur),
                New SqlParameter("@Telephone", If(fournisseur.Telephone, CType(DBNull.Value, Object))),
                New SqlParameter("@Email", If(fournisseur.Email, CType(DBNull.Value, Object))),
                New SqlParameter("@Adresse", If(fournisseur.Adresse, CType(DBNull.Value, Object))),
                New SqlParameter("@EstActif", fournisseur.EstActif),
                New SqlParameter("@FournisseurId", fournisseur.FournisseurId),
                New SqlParameter("@ModifierPar", SessionUtilisateur.NomUtilisateur)
            }

            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function

        ' Supprime un fournisseur.
        Public Function Supprimer(fournisseurId As Integer) As Integer
            Dim sql As String = "DELETE FROM Fournisseurs WHERE FournisseurId = @FournisseurId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@FournisseurId", fournisseurId)}
            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function

        Private Function MapVersDTO(row As DataRow) As FournisseurDTO
            Return New FournisseurDTO With {
                .FournisseurId = Convert.ToInt32(row("FournisseurId")),
                .NomFournisseur = Convert.ToString(row("NomFournisseur")),
                .Telephone = If(row.IsNull("Telephone"), Nothing, Convert.ToString(row("Telephone"))),
                .Email = If(row.IsNull("Email"), Nothing, Convert.ToString(row("Email"))),
                .Adresse = If(row.IsNull("Adresse"), Nothing, Convert.ToString(row("Adresse"))),
                .EstActif = Convert.ToBoolean(row("EstActif"))
            }
        End Function
    End Class
End Namespace
