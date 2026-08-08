Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class TypeVenteProduitDTO
        Public Property TypeVenteProduitId As Integer
        Public Property ProduitId As Integer
        Public Property Nom As String
        Public Property QuantiteEquivalent As Decimal
        Public Property TypeUniteEquivalent As String = "SECONDAIRE"
        Public Property TypeQuantiteEquivalent As String
        Public Property ModePrix As String
        Public Property Coefficient As Decimal?
        Public Property PrixVente As Decimal
        Public Property Actif As Boolean
        Public Property CreeLe As Date?
        Public Property ModifieLe As Date?
        Public Property ModifiePar As String

        Public ReadOnly Property ModePrixAffichage As String
            Get
                If String.Equals(ModePrix, "FIXE", StringComparison.OrdinalIgnoreCase) Then
                    Return "Prix fixe"
                End If

                If Coefficient.HasValue AndAlso Coefficient.Value > 0D Then
                    Return "Coeff. " & Coefficient.Value.ToString("N4")
                End If

                Return "Coefficient"
            End Get
        End Property

        Public ReadOnly Property NomAffichage As String
            Get
                Return Nom
            End Get
        End Property

        Public ReadOnly Property UniteEquivalentAffichage As String
            Get
                Dim typeAffichage As String = If(String.IsNullOrWhiteSpace(TypeQuantiteEquivalent), TypeUniteEquivalent, TypeQuantiteEquivalent)
                If String.Equals(typeAffichage, "PRINCIPALE", StringComparison.OrdinalIgnoreCase) Then
                    Return "Unité principale"
                End If
                If String.Equals(typeAffichage, "MESURE", StringComparison.OrdinalIgnoreCase) Then
                    Return "Unité de mesure"
                End If
                Return "Unité secondaire"
            End Get
        End Property
    End Class
End Namespace
