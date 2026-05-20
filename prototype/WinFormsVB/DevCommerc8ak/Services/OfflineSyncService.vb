Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Net.Http
Imports System.Text
Imports System.Web.Script.Serialization

Namespace DevCommerc8ak
    Public Class OfflineSyncService
        Private ReadOnly _stockQueueRepo As StockSortieNonSynchroniseRepository
        Private ReadOnly _depenseQueueRepo As DepensesNonSynchroniseesRepository
        Private ReadOnly _stockSortieRepo As StockSortieRepository
        Private ReadOnly _log As New SyncLogService()
        Public Sub New(dal As DAL)
            _stockQueueRepo = New StockSortieNonSynchroniseRepository(dal)
            _depenseQueueRepo = New DepensesNonSynchroniseesRepository(dal)
            _stockSortieRepo = New StockSortieRepository(dal)
        End Sub

        Public Function SynchronisationActive() As Boolean
            Dim raw As String = ConfigurationManager.AppSettings("OfflineSyncEnabled")
            Dim enabled As Boolean = True
            If Not String.IsNullOrWhiteSpace(raw) Then
                Boolean.TryParse(raw, enabled)
            End If
            Return enabled
        End Function

        Public Function BaseApiUrl() As String
            Dim url As String = ConfigurationManager.AppSettings("SyncApiBaseUrl")
            If String.IsNullOrWhiteSpace(url) Then
                Return "http://localhost:5080/"
            End If
            If Not url.EndsWith("/") Then
                url &= "/"
            End If
            Return url
        End Function

        Public Function EssayerSynchroniserStockSortie(sortie As StockSortie) As Boolean
            Dim payload As String = ConstruireJsonStockSortie(sortie)
            If Not SynchronisationActive() Then
                EnqueueStockSortie(payload)
                _log.Warn("Synchro stock désactivée, file d'attente alimentée pour " & sortie.NumeroSortie)
                Return False
            End If
            Try
                EnvoyerJson("api/stocksortie", payload)
                _log.Info("StockSortie synchronisée: " & sortie.NumeroSortie)
                Return True
            Catch ex As Exception
                EnqueueStockSortie(payload, ex.Message)
                _log.Error("Echec synchro StockSortie: " & sortie.NumeroSortie, ex)
                Return False
            End Try
        End Function

        Public Function EssayerSynchroniserSortieParNumero(numeroSortie As String) As Boolean
            If String.IsNullOrWhiteSpace(numeroSortie) Then
                Return False
            End If
            Dim dt As DataTable = _stockSortieRepo.ListerSortieManuelleParNumero(numeroSortie)
            Dim lignes As New List(Of Dictionary(Of String, Object))()
            For Each row As DataRow In dt.Rows
                lignes.Add(New Dictionary(Of String, Object) From {
                    {"NumeroSortie", Convert.ToString(row("NumeroSortie"))},
                    {"DateSortie", Convert.ToString(row("DateSortie"))},
                    {"Client", Convert.ToString(row("Client"))},
                    {"Motif", Convert.ToString(row("Motif"))},
                    {"Produit", Convert.ToString(row("Produit"))},
                    {"QuantiteSaisie", LireDecimal(row, "QuantiteSaisie")},
                    {"QuantiteBase", LireDecimal(row, "QuantiteBase")},
                    {"Unite", Convert.ToString(row("Unite"))},
                    {"TypeVente", Convert.ToString(row("TypeVente"))},
                    {"PrixUnitaire", LireDecimal(row, "PrixUnitaire")},
                    {"MontantLigne", LireDecimal(row, "MontantLigne")},
                    {"StatutPaiement", Convert.ToString(row("StatutPaiement"))},
                    {"MontantPaye", LireDecimal(row, "MontantPaye")},
                    {"ResteAPayer", LireDecimal(row, "ResteAPayer")},
                    {"Observation", Convert.ToString(row("Observation"))}
                })
            Next
            Dim payload As String = New JavaScriptSerializer().Serialize(New Dictionary(Of String, Object) From {
                {"NumeroSortie", numeroSortie},
                {"Lignes", lignes}
            })

            If Not SynchronisationActive() Then
                EnqueueStockSortie(payload)
                _log.Warn("Synchro stock désactivée, file d'attente alimentée pour " & numeroSortie)
                Return False
            End If

            Try
                EnvoyerJson("api/stocksortie", payload)
                _log.Info("StockSortie synchronisée par numéro: " & numeroSortie)
                Return True
            Catch ex As Exception
                EnqueueStockSortie(payload, ex.Message)
                _log.Error("Echec synchro StockSortie par numéro: " & numeroSortie, ex)
                Return False
            End Try
        End Function

        Public Function EssayerSynchroniserDepense(depense As Depense) As Boolean
            Dim payload As String = ConstruireJsonDepense(depense)
            If Not SynchronisationActive() Then
                EnqueueDepense(payload)
                _log.Warn("Synchro dépense désactivée, file d'attente alimentée pour " & depense.Categorie)
                Return False
            End If
            Try
                EnvoyerJson("api/depenses", payload)
                _log.Info("Depense synchronisée: " & depense.Categorie)
                Return True
            Catch ex As Exception
                EnqueueDepense(payload, ex.Message)
                _log.Error("Echec synchro Depense: " & depense.Categorie, ex)
                Return False
            End Try
        End Function

        Public Sub SynchroniserStockSortie()
            Dim dt As DataTable = _stockQueueRepo.ListerEnAttente()
            For Each row As DataRow In dt.Rows
                Dim id As Integer = Convert.ToInt32(row("Id"))
                Dim jsonData As String = Convert.ToString(row("JsonData"))
                If Not PeutRetenter(row) Then
                    _log.Info("Retry différé pour StockSortie Id=" & id.ToString())
                    Continue For
                End If
                Try
                    EnvoyerJson("api/stocksortie", jsonData)
                    _stockQueueRepo.MarquerResultat(id, "SYNC_OK", Nothing, Convert.ToInt32(row("NombreTentatives")) + 1)
                    _log.Info("Retry synchro StockSortie réussi (Id=" & id.ToString() & ")")
                Catch ex As Exception
                    _stockQueueRepo.MarquerResultat(id, "ECHEC", ex.Message, Convert.ToInt32(row("NombreTentatives")) + 1)
                    _log.Error("Retry synchro StockSortie échoué (Id=" & id.ToString() & ")", ex)
                End Try
            Next
        End Sub

        Public Sub SynchroniserDepenses()
            Dim dt As DataTable = _depenseQueueRepo.ListerEnAttente()
            For Each row As DataRow In dt.Rows
                Dim id As Integer = Convert.ToInt32(row("Id"))
                Dim jsonData As String = Convert.ToString(row("JsonData"))
                If Not PeutRetenter(row) Then
                    _log.Info("Retry différé pour Depense Id=" & id.ToString())
                    Continue For
                End If
                Try
                    EnvoyerJson("api/depenses", jsonData)
                    _depenseQueueRepo.MarquerResultat(id, "SYNC_OK", Nothing, Convert.ToInt32(row("NombreTentatives")) + 1)
                    _log.Info("Retry synchro Depense réussi (Id=" & id.ToString() & ")")
                Catch ex As Exception
                    _depenseQueueRepo.MarquerResultat(id, "ECHEC", ex.Message, Convert.ToInt32(row("NombreTentatives")) + 1)
                    _log.Error("Retry synchro Depense échoué (Id=" & id.ToString() & ")", ex)
                End Try
            Next
        End Sub

        Public Sub SynchroniserTout()
            SynchroniserStockSortie()
            SynchroniserDepenses()
        End Sub

        Public Sub QueueDepense(depense As Depense)
            EnqueueDepense(ConstruireJsonDepense(depense))
        End Sub

        Private Sub EnqueueStockSortie(jsonData As String, Optional messageErreur As String = Nothing)
            _stockQueueRepo.Ajouter(jsonData, "EN_ATTENTE", messageErreur)
        End Sub

        Private Sub EnqueueDepense(jsonData As String, Optional messageErreur As String = Nothing)
            _depenseQueueRepo.Ajouter(jsonData, "EN_ATTENTE", messageErreur)
        End Sub

        Private Sub EnvoyerJson(chemin As String, jsonData As String)
            RemoteApiSession.PostJson(chemin, jsonData)
        End Sub

        Private Function PeutRetenter(row As DataRow) As Boolean
            If row Is Nothing Then
                Return False
            End If

            Dim tentatives As Integer = 0
            If Not row.IsNull("NombreTentatives") Then
                tentatives = Convert.ToInt32(row("NombreTentatives"))
            End If

            Dim derniereTentative As DateTime? = Nothing
            If Not row.IsNull("DerniereTentative") Then
                derniereTentative = Convert.ToDateTime(row("DerniereTentative"))
            End If

            If Not derniereTentative.HasValue Then
                Return True
            End If

            Dim attenteMinutes As Integer = Math.Min(60, CInt(2 * Math.Pow(2, Math.Min(tentatives, 5))))
            Return (DateTime.UtcNow - derniereTentative.Value.ToUniversalTime()).TotalMinutes >= attenteMinutes
        End Function

        Private Function ConstruireJsonStockSortie(sortie As StockSortie) As String
            Dim payload As New Dictionary(Of String, Object) From {
                {"NumeroSortie", sortie.NumeroSortie},
                {"ProduitId", sortie.ProduitId},
                {"QuantiteSaisie", sortie.QuantiteSaisie},
                {"Unite", sortie.Unite},
                {"QuantiteBase", sortie.QuantiteBase},
                {"DateSortie", sortie.DateSortie},
                {"Source", sortie.Source},
                {"RefSource", sortie.RefSource},
                {"CreePar", sortie.CreePar},
                {"ClientId", sortie.ClientId},
                {"MotifId", sortie.MotifId},
                {"TypeVente", sortie.TypeVente},
                {"PrixUnitaire", sortie.PrixUnitaire},
                {"MontantLigne", sortie.MontantLigne},
                {"StatutPaiement", sortie.StatutPaiement},
                {"MontantPaye", sortie.MontantPaye},
                {"ResteAPayer", sortie.ResteAPayer},
                {"Observation", sortie.Observation}
            }
            Return New JavaScriptSerializer().Serialize(payload)
        End Function

        Private Function ConstruireJsonDepense(depense As Depense) As String
            Dim payload As New Dictionary(Of String, Object) From {
                {"Id", depense.Id},
                {"Categorie", depense.Categorie},
                {"Montant", depense.Montant},
                {"Devise", depense.Devise},
                {"Description", depense.Description},
                {"DateDepense", depense.DateDepense},
                {"Source", depense.Source},
                {"TypeDepense", depense.TypeDepense},
                {"CreePar", depense.CreePar}
            }
            Return New JavaScriptSerializer().Serialize(payload)
        End Function

        Private Shared Function LireDecimal(row As DataRow, colonne As String) As Decimal
            If row Is Nothing OrElse row.Table Is Nothing OrElse Not row.Table.Columns.Contains(colonne) OrElse row.IsNull(colonne) Then
                Return 0D
            End If
            Return Convert.ToDecimal(row(colonne))
        End Function
    End Class
End Namespace
