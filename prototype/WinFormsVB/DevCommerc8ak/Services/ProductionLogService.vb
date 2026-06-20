Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.Text

Namespace DevCommerc8ak
    Public Class ProductionLogService
        Private ReadOnly _folderPath As String

        Public Sub New()
            _folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CommercialPro", "Logs")
        End Sub

        Public Sub Info(message As String)
            WriteLine("INFO", message, Nothing)
        End Sub

        Public Sub Warn(message As String)
            WriteLine("WARN", message, Nothing)
        End Sub

        Public Sub [Error](message As String, ex As Exception)
            WriteLine("ERROR", message, ex)
        End Sub

        Private Sub WriteLine(level As String, message As String, ex As Exception)
            Try
                Directory.CreateDirectory(_folderPath)
                Dim filePath As String = Path.Combine(_folderPath, Date.UtcNow.ToString("yyyyMMdd") & ".log")
                Dim sb As New StringBuilder()
                sb.Append(Date.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                sb.Append(" [")
                sb.Append(level)
                sb.Append("] ")
                sb.Append(message)
                If ex IsNot Nothing Then
                    sb.Append(" | ")
                    sb.Append(ex.GetType().Name)
                    sb.Append(": ")
                    sb.Append(ex.Message)
                    If Not String.IsNullOrWhiteSpace(ex.StackTrace) Then
                        sb.Append(" | ")
                        sb.Append(ex.StackTrace)
                    End If
                End If
                sb.AppendLine()
                File.AppendAllText(filePath, sb.ToString())
            Catch
            End Try
        End Sub
    End Class
End Namespace
