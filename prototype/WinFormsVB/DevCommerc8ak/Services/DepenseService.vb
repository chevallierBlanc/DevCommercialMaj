Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class DepenseService
        Private ReadOnly _repo As DepenseRepository
        Private ReadOnly _syncService As OfflineSyncService

        Public Sub New(repo As DepenseRepository, syncService As OfflineSyncService)
            _repo = repo
            _syncService = syncService
        End Sub

        Public Function EnregistrerDepense(depense As Depense) As Integer
            Dim id As Integer = _repo.Ajouter(depense)
            depense.Id = id
            Try
                _syncService.EssayerSynchroniserDepense(depense)
            Catch
            End Try
            If id > 0 Then
                AuditActionService.Enregistrer("Finance", "Ajout dépense", "Dépense enregistrée.")
                AppEvents.OnDepenseAjoutee()
                AppEvents.OnCaisseModifiee()
                AppEvents.OnAnalyseVenteModifiee()
                AppEvents.OnDataChanged()
            End If
            Return id
        End Function
    End Class
End Namespace
