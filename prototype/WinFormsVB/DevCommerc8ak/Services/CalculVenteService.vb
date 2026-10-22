Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public NotInheritable Class CalculVenteService
        Private Sub New()
        End Sub

        Public Shared Function CalculerCoutEquivalentCoefficient(prixAchat As Decimal, quantiteBase As Decimal, contenuPrincipal As Decimal) As Decimal
            ' Reference tarifaire existante des types a coefficient. Elle est
            ' partagee entre affichage et delegation, sans changer son calcul.
            If contenuPrincipal <= 0D Then Throw New ArgumentOutOfRangeException("contenuPrincipal")
            Return prixAchat * (quantiteBase / contenuPrincipal)
        End Function

        Public Shared Function CalculerCoutComparable(prixAchat As Decimal, quantiteBase As Decimal, conversionUnite As Decimal, typeGestionStock As String, contenuPrincipal As Decimal) As Decimal?
            If prixAchat <= 0D Then Return Nothing
            If quantiteBase <= 0D Then Throw New ArgumentOutOfRangeException(NameOf(quantiteBase))
            ' Meme reference d'achat que CalculerCoutUnitaireStock, sans arrondir
            ' chaque piece avant multiplication. Ne change aucune quantite physique.
            Dim reference As Decimal = If(StockUnitConversionService.EstGestionMesuree(typeGestionStock) AndAlso contenuPrincipal > 0D, contenuPrincipal, If(conversionUnite > 0D, conversionUnite, 1D))
            Return CalculerCoutEquivalentCoefficient(prixAchat, quantiteBase, reference)
        End Function

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
