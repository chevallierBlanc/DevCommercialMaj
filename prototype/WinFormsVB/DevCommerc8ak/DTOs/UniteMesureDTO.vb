Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class UniteMesureDTO
        Public Property UniteMesureId As Integer
        Public Property Code As String
        Public Property Libelle As String
        Public Property Symbole As String
        Public Property CategorieUnite As String
        Public Property AutoriseFraction As Boolean
        Public Property NombreDecimales As Integer
        Public Property EstActif As Boolean
        Public Property CreeLe As Date?
        Public Property ModifieLe As Date?
    End Class
End Namespace
