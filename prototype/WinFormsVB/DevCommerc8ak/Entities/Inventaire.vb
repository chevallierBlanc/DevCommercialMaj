Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class Inventaire
        Public Property InventaireId As Integer
        Public Property ReferenceInventaire As String
        Public Property DateCreation As Date
        Public Property DateValidation As Date?
        Public Property CreePar As Integer?
        Public Property ValidePar As Integer?
        Public Property Statut As String
        Public Property Observation As String
    End Class
End Namespace
