Option Strict On
Option Explicit On

Imports System
Imports System.Web.Script.Serialization

Namespace DevCommerc8ak
    Public Class DashboardApiClient
        Private ReadOnly _serializer As New JavaScriptSerializer() With {.MaxJsonLength = Integer.MaxValue}

        Public Function ChargerJournalier(dateRef As Date) As ApiJournalierDashboardResponse
            Dim json As String = RemoteApiSession.GetJson("api/dashboard/journalier?date=" & Uri.EscapeDataString(dateRef.ToString("yyyy-MM-dd")))
            Return _serializer.Deserialize(Of ApiJournalierDashboardResponse)(json)
        End Function

        Public Function ChargerMensuel(annee As Integer, mois As Integer) As ApiMensuelDashboardResponse
            Dim json As String = RemoteApiSession.GetJson("api/dashboard/mensuel?year=" & annee.ToString() & "&month=" & mois.ToString())
            Return _serializer.Deserialize(Of ApiMensuelDashboardResponse)(json)
        End Function

        Public Function ChargerAnnuel(annee As Integer) As ApiAnnuelDashboardResponse
            Dim json As String = RemoteApiSession.GetJson("api/dashboard/annuel?year=" & annee.ToString())
            Return _serializer.Deserialize(Of ApiAnnuelDashboardResponse)(json)
        End Function
    End Class
End Namespace
