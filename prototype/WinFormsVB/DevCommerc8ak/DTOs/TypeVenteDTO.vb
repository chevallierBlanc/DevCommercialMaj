Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class TypeVenteDTO
        Public Property Nom As String
        Public Property QuantiteEquivalent As Decimal
        Public Property Coefficient As Decimal
        Public Property PrixVente As Decimal
        Public Property Actif As Boolean

        Public ReadOnly Property NomAffichage As String
            Get
                Return Nom
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return Nom
        End Function
    End Class
End Namespace
