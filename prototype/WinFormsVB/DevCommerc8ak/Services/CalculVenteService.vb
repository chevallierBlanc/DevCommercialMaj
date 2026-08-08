Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public NotInheritable Class CalculVenteService
        Private Sub New()
        End Sub

        Public Shared Function CalculerCoutUnitaireBase(prixAchat As Decimal, conversionUnite As Decimal) As Decimal?
            Return StockUnitConversionService.CalculerCoutUnitaireStock(prixAchat, conversionUnite, "UNITE", 0D)
        End Function

        Public Shared Function CalculerCoutUnitaireBase(prixAchat As Decimal, conversionUnite As Decimal, typeGestionStock As String, contenuUnitePrincipale As Decimal) As Decimal?
            Return StockUnitConversionService.CalculerCoutUnitaireStock(prixAchat, conversionUnite, typeGestionStock, contenuUnitePrincipale)
        End Function

        Public Shared Function CalculerMargeBeneficiaire(benefice As Decimal, chiffreAffaires As Decimal) As Decimal
            If chiffreAffaires <= 0D Then
                Return 0D
            End If

            Return Math.Round((benefice / chiffreAffaires) * 100D, 2)
        End Function

        Public Shared Function CalculerQuantiteBaseTypeVente(quantiteEquivalent As Decimal, typeUniteEquivalent As String, conversionUnite As Decimal) As Decimal
            Dim quantite As Decimal = Math.Max(0D, quantiteEquivalent)
            If quantite <= 0D Then
                Return 0D
            End If

            Return StockUnitConversionService.CalculerQuantiteBaseTypeVente(quantite, typeUniteEquivalent, conversionUnite, 0D, 1D)
        End Function

        Public Shared Function CalculerQuantiteBaseTypeVente(quantiteEquivalent As Decimal, typeUniteEquivalent As String, conversionUnite As Decimal, contenuUnitePrincipale As Decimal, contenuUniteSecondaire As Decimal) As Decimal
            Return StockUnitConversionService.CalculerQuantiteBaseTypeVente(quantiteEquivalent, typeUniteEquivalent, conversionUnite, contenuUnitePrincipale, contenuUniteSecondaire)
        End Function

        Public Shared Function CalculerCoefficientDepuisPrix(prixReference As Decimal, prixVente As Decimal) As Decimal
            If prixReference <= 0D OrElse prixVente <= 0D Then
                Return 0D
            End If

            Return Math.Round(prixVente / prixReference, 4)
        End Function

        Public Shared Function CalculerPourcentageDepuisCoefficient(coefficient As Decimal) As Decimal
            If coefficient <= 0D Then
                Return 0D
            End If

            Return Math.Round((coefficient - 1D) * 100D, 2)
        End Function
    End Class
End Namespace
