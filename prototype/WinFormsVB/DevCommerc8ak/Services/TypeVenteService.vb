Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class TypeVenteService

        ' ########### Calcul type vente
        Public Function ConstruireTypesVente(nbUniteParBase As Decimal,
                                             prixAchat As Decimal,
                                             prixGros As Decimal,
                                             prixDemi As Decimal,
                                             prixPiece As Decimal,
                                             prixQuart As Decimal,
                                             prixDouzaine As Decimal,
                                             prixSpecial As Decimal,
                                             venteGros As Boolean,
                                             venteDemi As Boolean,
                                             venteDetail As Boolean,
                                             venteDouzaine As Boolean) As List(Of TypeVenteDTO)
            Dim liste As New List(Of TypeVenteDTO)()
            Dim nb As Decimal = If(nbUniteParBase > 0D, nbUniteParBase, 1D)
            Dim coeffGros As Decimal = CalculerCoefficient(prixAchat, prixGros)
            Dim coeffDetail As Decimal = CalculerCoefficient(prixAchat, prixPiece * nb)

            If venteGros AndAlso prixGros > 0D Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "gros",
                    .QuantiteEquivalent = nb,
                    .Coefficient = coeffGros,
                    .PrixVente = prixGros,
                    .Actif = True
                })
            End If

            If venteDemi AndAlso prixDemi > 0D Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "demi",
                    .QuantiteEquivalent = Math.Max(1D, Decimal.Floor(nb / 2D)),
                    .Coefficient = coeffGros,
                    .PrixVente = prixDemi,
                    .Actif = True
                })
            End If

            If prixQuart > 0D Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "quart",
                    .QuantiteEquivalent = Math.Max(1D, Decimal.Floor(nb / 4D)),
                    .Coefficient = coeffDetail,
                    .PrixVente = prixQuart,
                    .Actif = True
                })
            End If

            If venteDetail AndAlso prixPiece > 0D Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "piece",
                    .QuantiteEquivalent = 1D,
                    .Coefficient = coeffDetail,
                    .PrixVente = prixPiece,
                    .Actif = True
                })
            End If

            If venteDouzaine AndAlso prixDouzaine > 0D Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "douzaine",
                    .QuantiteEquivalent = 12D,
                    .Coefficient = coeffDetail,
                    .PrixVente = prixDouzaine,
                    .Actif = True
                })
            End If

            If prixSpecial > 0D Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "speciale",
                    .QuantiteEquivalent = 1D,
                    .Coefficient = CalculerCoefficient(prixAchat, prixSpecial),
                    .PrixVente = prixSpecial,
                    .Actif = True
                })
                liste.Add(New TypeVenteDTO With {
                    .Nom = "promo",
                    .QuantiteEquivalent = 1D,
                    .Coefficient = CalculerCoefficient(prixAchat, prixSpecial),
                    .PrixVente = prixSpecial,
                    .Actif = True
                })
            End If

            Return liste
        End Function

        Public Function CalculerCoefficient(prixAchat As Decimal, prixVente As Decimal) As Decimal
            If prixAchat <= 0D OrElse prixVente <= 0D Then
                Return 0D
            End If
            Return Math.Round(prixVente / prixAchat, 4)
        End Function

        Public Function FormaterStock(stockReel As Decimal, nbUniteParBase As Decimal, uniteBase As String, uniteSecondaire As String) As String
            If nbUniteParBase <= 0D OrElse String.IsNullOrWhiteSpace(uniteSecondaire) Then
                Return stockReel.ToString("N2") & " " & uniteBase
            End If

            Dim stockBase As Decimal = Decimal.Floor(stockReel / nbUniteParBase)
            Dim reste As Decimal = stockReel - (stockBase * nbUniteParBase)
            Return stockBase.ToString("N0") & " " & uniteBase & " / " & reste.ToString("N0") & " " & uniteSecondaire
        End Function
    End Class
End Namespace
