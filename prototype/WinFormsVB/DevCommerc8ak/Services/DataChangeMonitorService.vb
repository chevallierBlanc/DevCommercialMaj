Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Threading

Namespace DevCommerc8ak
    Public Class DataChangeEventArgs
        Inherits EventArgs
        Public Sub New(domaine As String, version As Long)
            Me.Domaine = domaine
            Me.Version = version
        End Sub
        Public ReadOnly Property Domaine As String
        Public ReadOnly Property Version As Long
    End Class

    Public Class DataChangeMonitorService
        Implements IDisposable

        Private ReadOnly _domaines As String()
        Private ReadOnly _intervalMs As Integer
        Private ReadOnly _versions As New Dictionary(Of String, Long)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _log As New ProductionLogService()
        Private _timer As Timer
        Private _isChecking As Integer
        Private _disposed As Boolean
        Private _erreurLoggee As Boolean

        Public Event DomaineModifie As EventHandler(Of DataChangeEventArgs)

        Public Sub New(domaines As IEnumerable(Of String), intervalMs As Integer)
            Dim liste As New List(Of String)()
            If domaines IsNot Nothing Then
                For Each domaine As String In domaines
                    If Not String.IsNullOrWhiteSpace(domaine) Then liste.Add(domaine.Trim().ToUpperInvariant())
                Next
            End If
            _domaines = liste.ToArray()
            _intervalMs = Math.Max(2000, intervalMs)
        End Sub

        Public Sub Start()
            If _disposed OrElse _domaines.Length = 0 Then Return
            If _timer IsNot Nothing Then Return
            _timer = New Timer(AddressOf Tick, Nothing, 250, _intervalMs)
        End Sub

        Private Sub Tick(state As Object)
            If _disposed Then Return
            If Interlocked.Exchange(_isChecking, 1) = 1 Then
                Return
            End If

            Try
                Dim nouvelles As Dictionary(Of String, Long) = AppDataVersionService.LireVersions(_domaines)
                For Each kvp As KeyValuePair(Of String, Long) In nouvelles
                    Dim ancienne As Long = 0
                    If Not _versions.TryGetValue(kvp.Key, ancienne) Then
                        _versions(kvp.Key) = kvp.Value
                    ElseIf kvp.Value > ancienne Then
                        _versions(kvp.Key) = kvp.Value
                        RaiseEvent DomaineModifie(Me, New DataChangeEventArgs(kvp.Key, kvp.Value))
                    End If
                Next
                _erreurLoggee = False
            Catch ex As Exception
                If Not _erreurLoggee Then
                    _log.Warn("DataChangeMonitorService", "Tick", "Surveillance multi-postes indisponible : " & ex.Message)
                    _erreurLoggee = True
                End If
            Finally
                Interlocked.Exchange(_isChecking, 0)
            End Try
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            _disposed = True
            If _timer IsNot Nothing Then
                _timer.Dispose()
                _timer = Nothing
            End If
        End Sub
    End Class
End Namespace
