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

        Public Function Ajouter(sortie As StockSortie, Optional numeroSortie As String = Nothing, Optional cn As SqlConnection = Nothing, Optional tx As SqlTransaction = Nothing) As Integer
            Dim numero As String = If(Not String.IsNullOrWhiteSpace(numeroSortie), numeroSortie, If(Not String.IsNullOrWhiteSpace(sortie.NumeroSortie), sortie.NumeroSortie, GenererNumeroSortie()))
            Dim sql As String = "INSERT INTO StockSortie (ProduitId, QuantiteSaisie, Unite, QuantiteBase, DateSortie, Source, RefSource, CreePar, NumeroSortie, ClientId, MotifId, TypeVente, PrixUnitaire, MontantLigne, StatutPaiement, MontantPaye, ResteAPayer, Observation) " &
                                "VALUES (@ProduitId, @QuantiteSaisie, @Unite, @QuantiteBase, @DateSortie, @Source, @RefSource, @CreePar, @NumeroSortie, @ClientId, @MotifId, @TypeVente, @PrixUnitaire, @MontantLigne, @StatutPaiement, @MontantPaye, @ResteAPayer, @Observation); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@ProduitId", sortie.ProduitId),
                New SqlParameter("@QuantiteSaisie", sortie.QuantiteSaisie),
                New SqlParameter("@Unite", If(String.IsNullOrWhiteSpace(sortie.Unite), CType(DBNull.Value, Object), sortie.Unite)),
                New SqlParameter("@QuantiteBase", sortie.QuantiteBase),
                New SqlParameter("@DateSortie", sortie.DateSortie),
                New SqlParameter("@Source", If(String.IsNullOrWhiteSpace(sortie.Source), CType(DBNull.Value, Object), sortie.Source)),
                New SqlParameter("@RefSource", If(String.IsNullOrWhiteSpace(sortie.RefSource), CType(DBNull.Value, Object), sortie.RefSource)),
                New SqlParameter("@CreePar", If(sortie.CreePar.HasValue, CType(sortie.CreePar.Value, Object), DBNull.Value)),
                New SqlParameter("@NumeroSortie", numero),
                New SqlParameter("@ClientId", If(sortie.ClientId.HasValue, CType(sortie.ClientId.Value, Object), DBNull.Value)),
                New SqlParameter("@MotifId", If(sortie.MotifId.HasValue, CType(sortie.MotifId.Value, Object), DBNull.Value)),
                New SqlParameter("@TypeVente", If(String.IsNullOrWhiteSpace(sortie.TypeVente), CType(DBNull.Value, Object), sortie.TypeVente)),
                New SqlParameter("@PrixUnitaire", If(sortie.PrixUnitaire.HasValue, CType(sortie.PrixUnitaire.Value, Object), DBNull.Value)),
                New SqlParameter("@MontantLigne", If(sortie.MontantLigne.HasValue, CType(sortie.MontantLigne.Value, Object), DBNull.Value)),
                New SqlParameter("@StatutPaiement", If(String.IsNullOrWhiteSpace(sortie.StatutPaiement), CType(DBNull.Value, Object), sortie.StatutPaiement)),
                New SqlParameter("@MontantPaye", If(sortie.MontantPaye.HasValue, CType(sortie.MontantPaye.Value, Object), DBNull.Value)),
                New SqlParameter("@ResteAPayer", If(sortie.ResteAPayer.HasValue, CType(sortie.ResteAPayer.Value, Object), DBNull.Value)),
                New SqlParameter("@Observation", If(String.IsNullOrWhiteSpace(sortie.Observation), CType(DBNull.Value, Object), sortie.Observation))
            }
            If cn Is Nothing Then
                Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
                Return Convert.ToInt32(v)
            End If
            Dim ownsConnection As Boolean = False
            If cn.State <> ConnectionState.Open Then
                cn.Open()
                ownsConnection = True
            End If
            Try
                Using cmd As New SqlCommand(sql, cn)
                    If tx IsNot Nothing Then cmd.Transaction = tx
                    cmd.Parameters.AddRange(p.ToArray())
                    Dim v As Object = cmd.ExecuteScalar()
                    Return Convert.ToInt32(v)
                End Using
            Finally
                If ownsConnection Then
                    cn.Close()
                End If
            End Try
        End Function

        Private Function GenererNumeroSortie() As String
            Dim prefix As String = "SORT-" & DateTime.Now.ToString("yyyyMMdd")
            Dim sql As String = "SELECT ISNULL(MAX(CAST(RIGHT(NumeroSortie, 3) AS INT)), 0) + 1 FROM StockSortie WHERE NumeroSortie LIKE @PrefixLike"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@PrefixLike", prefix & "-%")}
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Dim numero As Integer = Convert.ToInt32(v)
            Return prefix & "-" & numero.ToString("000")
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
