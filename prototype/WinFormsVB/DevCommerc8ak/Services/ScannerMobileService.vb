Option Strict On
Option Explicit On

Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

Namespace DevCommerc8ak
    Public Class ScannerMobileService
        Private _listener As TcpListener
        Private _thread As Thread
        Private _actif As Boolean

        Public Event CodeRecu(code As String)

        Public Sub Demarrer(ip As String, port As Integer)
            If _actif Then Return
            _listener = New TcpListener(IPAddress.Parse(ip), port)
            _listener.Start()
            _actif = True
            _thread = New Thread(AddressOf Ecouter)
            _thread.IsBackground = True
            _thread.Start()
        End Sub

        Public Sub Arreter()
            _actif = False
            Try
                _listener.Stop()
            Catch
            End Try
        End Sub

        Private Sub Ecouter()
            While _actif
                Try
                    Dim client As TcpClient = _listener.AcceptTcpClient()
                    Using stream As NetworkStream = client.GetStream()
                        Dim buffer(1023) As Byte
                        Dim len As Integer = stream.Read(buffer, 0, buffer.Length)
                        Dim code As String = Encoding.UTF8.GetString(buffer, 0, len).Trim()
                        If code <> "" Then
                            RaiseEvent CodeRecu(code)
                        End If
                    End Using
                    client.Close()
                Catch
                    Thread.Sleep(200)
                End Try
            End While
        End Sub
    End Class
End Namespace
