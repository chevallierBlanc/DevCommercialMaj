Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class NotificationDTO
        Public Property NotificationId As Integer
        Public Property TypeNotification As String
        Public Property Message As String
        Public Property CreeLe As Date
        Public Property Lue As Boolean
        Public Property CleNotification As String
        Public Property EcranCible As String
        Public Property DonneesCible As String
        Public Property CompteurOccurrences As Integer
        Public Property EstGroupee As Boolean
    End Class
End Namespace
