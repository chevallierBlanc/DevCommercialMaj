Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public NotInheritable Class StockUnitConversionService
        Private Sub New()
        End Sub

        Public Shared Function NormaliserTypeGestionStock(typeGestion As String) As String
            If String.Equals(typeGestion, "POIDS", StringComparison.OrdinalIgnoreCase) Then Return "POIDS"
            If String.Equals(typeGestion, "VOLUME", StringComparison.OrdinalIgnoreCase) Then Return "VOLUME"
            Return "UNITE"
        End Function

        Public Shared Function NormaliserTypeQuantiteEquivalent(typeQuantite As String) As String
            If String.Equals(typeQuantite, "PRINCIPALE", StringComparison.OrdinalIgnoreCase) Then Return "PRINCIPALE"
            If String.Equals(typeQuantite, "MESURE", StringComparison.OrdinalIgnoreCase) Then Return "MESURE"
            Return "SECONDAIRE"
        End Function

        Public Shared Function CalculerQuantiteBaseTypeVente(quantiteEquivalent As Decimal,
                                                             typeQuantiteEquivalent As String,
                                                             conversionUnite As Decimal,
                                                             contenuUnitePrincipale As Decimal,
                                                             contenuUniteSecondaire As Decimal) As Decimal
            Dim quantite As Decimal = Math.Max(0D, quantiteEquivalent)
            If quantite <= 0D Then Return 0D

            Dim typeNormalise As String = NormaliserTypeQuantiteEquivalent(typeQuantiteEquivalent)
            If typeNormalise = "PRINCIPALE" Then
                Dim contenuPrincipal As Decimal = If(contenuUnitePrincipale > 0D, contenuUnitePrincipale, If(conversionUnite > 0D, conversionUnite, 1D))
                Return quantite * contenuPrincipal
            End If

            If typeNormalise = "MESURE" Then
                Return quantite
            End If

            Dim contenuSecondaire As Decimal = If(contenuUniteSecondaire > 0D, contenuUniteSecondaire, 1D)
            Return quantite * contenuSecondaire
        End Function

        Public Shared Function CalculerCoutUnitaireStock(prixAchat As Decimal,
                                                         conversionUnite As Decimal,
                                                         typeGestionStock As String,
                                                         contenuUnitePrincipale As Decimal) As Decimal?
            If prixAchat <= 0D Then Return Nothing

            Dim typeNormalise As String = NormaliserTypeGestionStock(typeGestionStock)
            If (typeNormalise = "POIDS" OrElse typeNormalise = "VOLUME") AndAlso contenuUnitePrincipale > 0D Then
                Return Math.Round(prixAchat / contenuUnitePrincipale, 4)
            End If

            If conversionUnite > 0D Then
                Return Math.Round(prixAchat / conversionUnite, 4)
            End If

            Return Math.Round(prixAchat, 4)
        End Function

        Public Shared Function CalculerQuantiteEntreeNormalisee(quantitePrincipale As Decimal,
                                                                quantiteSecondaireOuMesure As Decimal,
                                                                conversionUnite As Decimal,
                                                                typeGestionStock As String,
                                                                contenuUnitePrincipale As Decimal,
                                                                contenuUniteSecondaire As Decimal) As Decimal
            Dim principale As Decimal = Math.Max(0D, quantitePrincipale)
            Dim complement As Decimal = Math.Max(0D, quantiteSecondaireOuMesure)
            Dim typeNormalise As String = NormaliserTypeGestionStock(typeGestionStock)

            If typeNormalise = "POIDS" OrElse typeNormalise = "VOLUME" Then
                Dim contenuPrincipal As Decimal = If(contenuUnitePrincipale > 0D, contenuUnitePrincipale, If(conversionUnite > 0D, conversionUnite, 1D))
                Return (principale * contenuPrincipal) + complement
            End If

            Dim conversion As Decimal = If(conversionUnite > 0D, conversionUnite, 1D)
            Return (principale * conversion) + complement
        End Function
    End Class
End Namespace
