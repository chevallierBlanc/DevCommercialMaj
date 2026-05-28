Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class Depense
        Public Property Id As Integer
        Public Property Categorie As String
        Public Property Montant As Decimal
        Public Property Devise As String
        Public Property Description As String
        Public Property DateDepense As Date
        Public Property Source As String
        Public Property TypeDepense As String
        Public Property CreePar As String
        Public Property CreatedAt As Date
    End Class
End Namespace
