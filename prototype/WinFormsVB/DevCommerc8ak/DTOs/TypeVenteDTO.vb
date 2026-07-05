Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class TypeVenteDTO
        Public Property TypeVenteProduitId As Integer?
        Public Property Nom As String
        Public Property QuantiteEquivalent As Decimal
        Public Property ModePrix As String
        Public Property Coefficient As Decimal
        Public Property PrixVente As Decimal
        Public Property Actif As Boolean
        Public Property EstPersonnalise As Boolean

        Public ReadOnly Property NomAffichage As String
            Get
                Return Nom
            End Get
        End Property

        Public ReadOnly Property TypePrixAffichage As String
            Get
                If String.Equals(ModePrix, "FIXE", StringComparison.OrdinalIgnoreCase) Then
                    Return "Prix fixe"
                End If

                If Coefficient > 0D Then
                    Return "Coeff. " & Coefficient.ToString("N4")
                End If

                Return "Standard"
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return Nom
        End Function
    End Class
End Namespace
