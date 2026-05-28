Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Timers

Namespace DevCommerc8ak
    Public Module OfflineSyncScheduler
        Private ReadOnly _syncTimer As New Timer()
        Private _started As Boolean
        Private _dal As DAL
        Private ReadOnly _log As New SyncLogService()

        Public Sub Start(connectionString As String)
            SyncLock GetType(OfflineSyncScheduler)
                If _started Then
                    Return
                End If
                _dal = New DAL(connectionString)
                Dim intervalMs As Double = 120000D
                Dim raw As String = ConfigurationManager.AppSettings("OfflineSyncIntervalSeconds")
                Dim seconds As Integer
                If Integer.TryParse(raw, seconds) AndAlso seconds > 0 Then
                    intervalMs = CDbl(seconds) * 1000D
                End If
                _syncTimer.Interval = intervalMs
                AddHandler _syncTimer.Elapsed, AddressOf OnElapsed
                _syncTimer.AutoReset = True
                _syncTimer.Enabled = True
                _started = True
            End SyncLock
        End Sub

        Private Sub OnElapsed(sender As Object, e As ElapsedEventArgs)
            Try
                _log.Info("Cycle de synchronisation démarré")
                Dim service As New OfflineSyncService(_dal)
                service.SynchroniserTout()
                _log.Info("Cycle de synchronisation terminé")
            Catch ex As Exception
                _log.Error("Echec du cycle de synchronisation", ex)
            End Try
        End Sub
    End Module
End Namespace
