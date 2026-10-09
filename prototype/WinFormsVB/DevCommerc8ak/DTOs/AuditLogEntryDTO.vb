Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class AuditLogEntryDTO
        Public Property DateAction As Date
        Public Property Utilisateur As String
        Public Property Role As String
        Public Property Modul As String
        Public Property Action As String
        Public Property Description As String
        Public Property Machine As String
        Public Property Statut As String
        Public Property Niveau As String
        Public Property Provenance As String
        Public Property Avant As String
        Public Property Apres As String
        Public Property Motif As String
        Public Property SessionId As Integer?
        Public Property ModeActif As String
        Public Property ReferenceDocument As String
    End Class
End Namespace
