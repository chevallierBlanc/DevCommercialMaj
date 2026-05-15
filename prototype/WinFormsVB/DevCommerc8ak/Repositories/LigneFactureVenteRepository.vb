Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient

Namespace DevCommerc8ak
    Public Class LigneFactureVenteRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        ' Cree une ligne de facture et retourne son identifiant.
        Public Function Ajouter(ligne As LigneFactureVente) As Integer
            Dim sql As String = "INSERT INTO LignesFactureVente (FactureVenteId, ProduitId, Quantite, PrixUnitaire, MontantRemise, MontantLigne) " &
                                "VALUES (@FactureVenteId, @ProduitId, @Quantite, @PrixUnitaire, @MontantRemise, @MontantLigne); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"

            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@FactureVenteId", ligne.FactureVenteId),
                New SqlParameter("@ProduitId", ligne.ProduitId),
                New SqlParameter("@Quantite", ligne.Quantite),
                New SqlParameter("@PrixUnitaire", ligne.PrixUnitaire),
                New SqlParameter("@MontantRemise", ligne.MontantRemise),
                New SqlParameter("@MontantLigne", ligne.MontantLigne)
            }

            Dim id As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(id)
        End Function

        ' Liste des lignes d'une facture.
        Public Function ListerParFacture(factureVenteId As Integer) As List(Of LigneFactureVenteDTO)
            Dim sql As String = "SELECT LigneFactureVenteId, FactureVenteId, ProduitId, Quantite, PrixUnitaire, MontantLigne " &
                                "FROM LignesFactureVente WHERE FactureVenteId = @FactureVenteId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@FactureVenteId", factureVenteId)}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            Dim liste As New List(Of LigneFactureVenteDTO)()

            For Each row As DataRow In dt.Rows
                liste.Add(New LigneFactureVenteDTO With {
                    .LigneFactureVenteId = Convert.ToInt32(row("LigneFactureVenteId")),
                    .FactureVenteId = Convert.ToInt32(row("FactureVenteId")),
                    .ProduitId = Convert.ToInt32(row("ProduitId")),
                    .Quantite = Convert.ToDecimal(row("Quantite")),
                    .PrixUnitaire = Convert.ToDecimal(row("PrixUnitaire")),
                    .MontantLigne = Convert.ToDecimal(row("MontantLigne"))
                })
            Next

            Return liste
        End Function

        ' Liste des lignes d'une facture avec libelle produit.
        Public Function ListerDetailsParFacture(factureVenteId As Integer) As DataTable
            Dim sql As String = "" &
                "SELECT l.LigneFactureVenteId, l.FactureVenteId, l.ProduitId, p.Libelle, l.Quantite, l.PrixUnitaire, l.MontantLigne " &
                "FROM LignesFactureVente l JOIN Produits p ON p.ProduitId = l.ProduitId WHERE l.FactureVenteId = @FactureVenteId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@FactureVenteId", factureVenteId)}
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        ' Supprime une ligne.
        Public Function Supprimer(ligneId As Integer) As Integer
            Dim sql As String = "DELETE FROM LignesFactureVente WHERE LigneFactureVenteId = @LigneFactureVenteId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@LigneFactureVenteId", ligneId)}
            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function
    End Class
End Namespace
