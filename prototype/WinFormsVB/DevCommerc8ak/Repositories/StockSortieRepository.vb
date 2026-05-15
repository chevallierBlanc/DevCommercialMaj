Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Collections.Generic
Imports System.Data.SqlClient

Namespace DevCommerc8ak
    Public Class StockSortieRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerTable()
        End Sub

        Public Sub AssurerTable()
            ' Schéma géré par le script SQL de déploiement.
        End Sub

        Public Function Ajouter(sortie As StockSortie) As Integer
            Dim numeroSortie As String = "SORTM-" & DateTime.Now.ToString("yyyyMMdd-HHmmss")
            Dim sql As String = "INSERT INTO StockSortie (ProduitId, QuantiteSaisie, Unite, QuantiteBase, DateSortie, Source, RefSource, CreePar, NumeroSortie, ClientId, MotifId, TypeVente, PrixUnitaire, MontantLigne, StatutPaiement, MontantPaye, ResteAPayer, Observation) " &
                                "VALUES (@ProduitId, @QuantiteSaisie, @Unite, @QuantiteBase, @DateSortie, @Source, @RefSource, @CreePar, @NumeroSortie, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@ProduitId", sortie.ProduitId),
                New SqlParameter("@QuantiteSaisie", sortie.QuantiteSaisie),
                New SqlParameter("@Unite", If(sortie.Unite, CType(DBNull.Value, Object))),
                New SqlParameter("@QuantiteBase", sortie.QuantiteBase),
                New SqlParameter("@DateSortie", sortie.DateSortie),
                New SqlParameter("@Source", sortie.Source),
                New SqlParameter("@RefSource", If(sortie.RefSource, CType(DBNull.Value, Object))),
                New SqlParameter("@CreePar", If(sortie.CreePar.HasValue, CType(sortie.CreePar.Value, Object), DBNull.Value)),
                New SqlParameter("@NumeroSortie", numeroSortie)
            }
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(v)
        End Function

        '' --- MODULE 1 : SORTIE MANUELLE ---

        'Public Function EnregistrerSortieManuelle(panier As List(Of StockSortieDTO), motifId As Integer, clientId As Integer?, utilisateurId As Integer) As String
        '    Dim reference As String = "SORT-" & DateTime.Now.ToString("yyyyMMdd-HHmmss")

        '    Try
        '        For Each item As StockSortieDTO In panier
        '            Dim sql As String = "INSERT INTO StockSortie (NumeroSortie, ProduitId, Quantite, Unite, QuantiteBase, MotifId, ClientId, CreePar, DateSortie) " &
        '                              "VALUES (@ref, @pid, @qte, @unite, @qteBase, @motif, @client, @user, GETDATE())"

        '            Dim params As New Dictionary(Of String, Object) From {
        '                {"@ref", reference},
        '                {"@pid", item.ProduitId},
        '                {"@qte", item.Quantite},
        '                {"@unite", item.Unite},
        '                {"@qteBase", item.QuantiteBase},
        '                {"@motif", motifId},
        '                {"@client", If(clientId.HasValue, DirectCast(clientId.Value, Object), DBNull.Value)},
        '                {"@user", utilisateurId}
        '            }

        '            _dal.ExecuterNonRequete(sql, params)

            '            ' Mise à jour du stock réel
        '            Dim sqlStock As String = "UPDATE Produits SET StockActuel = StockActuel - @qte WHERE ProduitId = @id"
        '            Dim paramsStock As New Dictionary(Of String, Object) From {
        '                {"@qte", item.QuantiteBase},
        '                {"@id", item.ProduitId}
        '            }
        '            _dal.ExecuterNonRequete(sqlStock, paramsStock)
        '        Next
        '        Return reference
        '    Catch ex As Exception
        '        Throw New Exception("Erreur lors de l'enregistrement de la sortie : " & ex.Message)
        '    End Try
        'End Function

    End Class
End Namespace
