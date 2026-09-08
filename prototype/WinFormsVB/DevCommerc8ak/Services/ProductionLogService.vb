Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports System.Text
Imports System.Globalization

Namespace DevCommerc8ak
    Public Class ProductionLogService
        Private ReadOnly _folderPath As String

        Public Sub New()
            _folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CommercialPro", "Logs")
        End Sub

        Public Sub Info(message As String)
            WriteLine("INFO", String.Empty, String.Empty, message, Nothing)
        End Sub

        Public Sub Warn(message As String)
            WriteLine("WARN", String.Empty, String.Empty, message, Nothing)
        End Sub

        Public Sub [Error](message As String, ex As Exception)
            WriteLine("ERROR", String.Empty, String.Empty, message, ex)
        End Sub

        Public Sub Info(moduleName As String, action As String, message As String)
            WriteLine("INFO", moduleName, action, message, Nothing)
        End Sub

        Public Sub Warn(moduleName As String, action As String, message As String)
            WriteLine("WARN", moduleName, action, message, Nothing)
        End Sub

        Public Sub [Error](moduleName As String, action As String, message As String, ex As Exception)
            WriteLine("ERROR", moduleName, action, message, ex)
        End Sub

        Private Sub WriteLine(level As String, moduleName As String, action As String, message As String, ex As Exception)
            Try
                Directory.CreateDirectory(_folderPath)
                Dim filePath As String = Path.Combine(_folderPath, Date.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) & ".log")
                Dim sb As New StringBuilder()
                sb.Append(Date.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
                sb.Append(" [")
                sb.Append(level)
                sb.Append("] ")
                sb.Append("User=")
                sb.Append(GetCurrentUserName())
                sb.Append(" | Role=")
                sb.Append(GetCurrentRole())
                sb.Append(" | Host=")
                sb.Append(Environment.MachineName)
                If Not String.IsNullOrWhiteSpace(moduleName) Then
                    sb.Append(" | Module=")
                    sb.Append(moduleName)
                End If
                If Not String.IsNullOrWhiteSpace(action) Then
                    sb.Append(" | Action=")
                    sb.Append(action)
                End If
                sb.Append(" | Message=")
                sb.Append(message)
                If ex IsNot Nothing Then
                    sb.Append(" | ")
                    sb.Append(ex.GetType().FullName)
                    sb.Append(": ")
                    sb.Append(ex.Message)
                    AppendInnerExceptions(sb, ex.InnerException)
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

        Private Sub AppendInnerExceptions(sb As StringBuilder, inner As Exception)
            Dim current As Exception = inner
            Dim niveau As Integer = 1
            While current IsNot Nothing AndAlso niveau <= 5
                sb.Append(" | Inner")
                sb.Append(niveau.ToString(CultureInfo.InvariantCulture))
                sb.Append("=")
                sb.Append(current.GetType().FullName)
                sb.Append(": ")
                sb.Append(current.Message)
                current = current.InnerException
                niveau += 1
            End While
        End Sub

        Private Function GetCurrentUserName() As String
            Try
                If Not String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur) Then
                    Return SessionUtilisateur.NomUtilisateur
                End If
            Catch
            End Try
            Return Environment.UserName
        End Function

        Private Function GetCurrentRole() As String
            Try
                If Not String.IsNullOrWhiteSpace(SessionUtilisateur.Role) Then
                    Return SessionUtilisateur.Role
                End If
            Catch
            End Try
            Return "N/A"
        End Function
    End Class
End Namespace
