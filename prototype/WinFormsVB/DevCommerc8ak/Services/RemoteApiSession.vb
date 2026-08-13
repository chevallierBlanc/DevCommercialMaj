Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Net
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Web.Script.Serialization

Namespace DevCommerc8ak
    Public Module RemoteApiSession
        Private ReadOnly _http As New HttpClient() With {.Timeout = TimeSpan.FromSeconds(15)}
        Private ReadOnly _serializer As New JavaScriptSerializer() With {.MaxJsonLength = Integer.MaxValue}
        Private ReadOnly _log As New SyncLogService()
        Private _accessToken As String = String.Empty
        Private _refreshToken As String = String.Empty
        Private _username As String = String.Empty
        Private _role As String = String.Empty
        Private _accessTokenExpiresAtUtc As DateTime

        Public Function BaseUrl() As String
            Dim raw As String = ConfigurationManager.AppSettings("SyncApiBaseUrl")
            If String.IsNullOrWhiteSpace(raw) Then
                raw = "http://localhost:5080/"
            End If
            If Not raw.EndsWith("/") Then
                raw &= "/"
            End If
            Return raw
        End Function

        Public Function IsAuthenticated() As Boolean
            Return Not String.IsNullOrWhiteSpace(_accessToken)
        End Function

        Public Function UsernameCourant() As String
            Return _username
        End Function

        Public Function RoleCourant() As String
            Return _role
        End Function

        Public Function Authentifier(username As String, password As String) As Boolean
            Try
                Dim req As New ApiLoginRequest With {.Username = username, .Password = password}
                Dim body As String = _serializer.Serialize(req)
                Dim resp As HttpResponseMessage = _http.PostAsync(BaseUrl() & "api/auth/login", New StringContent(body, Encoding.UTF8, "application/json")).GetAwaiter().GetResult()
                If Not resp.IsSuccessStatusCode Then
                    _log.Warn("Authentification API refusée pour l'utilisateur " & If(username, String.Empty) & " | HTTP " & CInt(resp.StatusCode).ToString())
                    Clear()
                    Return False
                End If

                Dim json As String = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                Dim token As ApiTokenResponse = _serializer.Deserialize(Of ApiTokenResponse)(json)
                If token Is Nothing OrElse String.IsNullOrWhiteSpace(token.AccessToken) Then
                    Clear()
                    Return False
                End If

                _accessToken = token.AccessToken
                _refreshToken = token.RefreshToken
                _username = token.Username
                _role = token.Role
                _accessTokenExpiresAtUtc = token.AccessTokenExpiresAtUtc
                Return True
            Catch ex As Exception
                _log.Error("Erreur d'authentification API pour l'utilisateur " & If(username, String.Empty), ex)
                Clear()
                Return False
            End Try
        End Function

        Public Function GetJson(path As String) As String
            Dim req As New HttpRequestMessage(HttpMethod.Get, BaseUrl() & path.TrimStart("/"c))
            AjouterAutorisation(req)
            Dim resp As HttpResponseMessage = _http.SendAsync(req).GetAwaiter().GetResult()
            If Not resp.IsSuccessStatusCode Then
                Throw New InvalidOperationException("API " & CInt(resp.StatusCode).ToString() & " : " & resp.ReasonPhrase)
            End If
            Return resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        End Function

        Public Function PostJson(path As String, jsonBody As String) As String
            Dim req As New HttpRequestMessage(HttpMethod.Post, BaseUrl() & path.TrimStart("/"c))
            AjouterAutorisation(req)
            req.Content = New StringContent(jsonBody, Encoding.UTF8, "application/json")
            Dim resp As HttpResponseMessage = _http.SendAsync(req).GetAwaiter().GetResult()
            If Not resp.IsSuccessStatusCode Then
                Throw New InvalidOperationException("API " & CInt(resp.StatusCode).ToString() & " : " & resp.ReasonPhrase)
            End If
            Return resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        End Function

        Public Sub Clear()
            _accessToken = String.Empty
            _refreshToken = String.Empty
            _username = String.Empty
            _role = String.Empty
            _accessTokenExpiresAtUtc = DateTime.MinValue
        End Sub

        Private Sub AjouterAutorisation(req As HttpRequestMessage)
            If Not String.IsNullOrWhiteSpace(_accessToken) Then
                req.Headers.Authorization = New AuthenticationHeaderValue("Bearer", _accessToken)
            End If
        End Sub
    End Module
End Namespace
