Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class ProduitConditionnementDTO
        Public Property ProduitConditionnementId As Integer
        Public Property ProduitId As Integer
        Public Property UniteMesureId As Integer
        Public Property ConditionnementParentId As Integer?
        Public Property FacteurVersParent As Decimal?
        Public Property FacteurVersBase As Decimal
        Public Property Niveau As Integer
        Public Property EstUniteBase As Boolean
        Public Property EstAchetable As Boolean
        Public Property EstVendable As Boolean
        Public Property AutoriseFraction As Boolean
        Public Property OrdreAffichage As Integer
        Public Property EstActif As Boolean
        Public Property CreeLe As Date?
        Public Property ModifieLe As Date?
        Public Property ModifiePar As String

        Public Property CodeUnite As String
        Public Property LibelleUnite As String
        Public Property SymboleUnite As String
        Public Property CategorieUnite As String
    End Class
End Namespace
