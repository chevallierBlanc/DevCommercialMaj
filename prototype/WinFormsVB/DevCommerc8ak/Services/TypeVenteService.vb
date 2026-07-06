Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace DevCommerc8ak
    Public Class TypeVenteService
        Private ReadOnly _typeVenteProduitService As TypeVenteProduitService

        Public Sub New()
            _typeVenteProduitService = New TypeVenteProduitService()
        End Sub

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
                    .ModePrix = "COEFFICIENT",
                    .Coefficient = coeffGros,
                    .PrixVente = prixGros,
                    .Actif = True,
                    .EstPersonnalise = False
                })
            End If

            If venteDemi AndAlso prixDemi > 0D Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "demi",
                    .QuantiteEquivalent = Math.Max(1D, Decimal.Floor(nb / 2D)),
                    .ModePrix = "COEFFICIENT",
                    .Coefficient = coeffGros,
                    .PrixVente = prixDemi,
                    .Actif = True,
                    .EstPersonnalise = False
                })
            End If

            If prixQuart > 0D Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "quart",
                    .QuantiteEquivalent = Math.Max(1D, Decimal.Floor(nb / 4D)),
                    .ModePrix = "COEFFICIENT",
                    .Coefficient = coeffDetail,
                    .PrixVente = prixQuart,
                    .Actif = True,
                    .EstPersonnalise = False
                })
            End If

            If venteDetail AndAlso prixPiece > 0D Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "piece",
                    .QuantiteEquivalent = 1D,
                    .ModePrix = "COEFFICIENT",
                    .Coefficient = coeffDetail,
                    .PrixVente = prixPiece,
                    .Actif = True,
                    .EstPersonnalise = False
                })
            End If

            If venteDouzaine AndAlso prixDouzaine > 0D Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "douzaine",
                    .QuantiteEquivalent = 12D,
                    .ModePrix = "COEFFICIENT",
                    .Coefficient = coeffDetail,
                    .PrixVente = prixDouzaine,
                    .Actif = True,
                    .EstPersonnalise = False
                })
            End If

            If prixSpecial > 0D Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "speciale",
                    .QuantiteEquivalent = 1D,
                    .ModePrix = "COEFFICIENT",
                    .Coefficient = CalculerCoefficient(prixAchat, prixSpecial),
                    .PrixVente = prixSpecial,
                    .Actif = True,
                    .EstPersonnalise = False
                })
                liste.Add(New TypeVenteDTO With {
                    .Nom = "promo",
                    .QuantiteEquivalent = 1D,
                    .ModePrix = "COEFFICIENT",
                    .Coefficient = CalculerCoefficient(prixAchat, prixSpecial),
                    .PrixVente = prixSpecial,
                    .Actif = True,
                    .EstPersonnalise = False
                })
            End If

            Return liste
        End Function

        Public Function ConstruireTypesVentePourProduit(produitId As Integer,
                                                        nbUniteParBase As Decimal,
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
                                                        venteDouzaine As Boolean,
                                                        Optional typesPersonnalisesOverrides As IEnumerable(Of TypeVenteProduitDTO) = Nothing) As List(Of TypeVenteDTO)
            Dim liste As List(Of TypeVenteDTO) = ConstruireTypesVente(nbUniteParBase, prixAchat, prixGros, prixDemi, prixPiece, prixQuart, prixDouzaine, prixSpecial, venteGros, venteDemi, venteDetail, venteDouzaine)
            If produitId <= 0 Then
                Return liste
            End If

            Dim typesPersonnalises As List(Of TypeVenteProduitDTO)
            If typesPersonnalisesOverrides Is Nothing Then
                typesPersonnalises = _typeVenteProduitService.ListerParProduit(produitId, True)
            Else
                typesPersonnalises = typesPersonnalisesOverrides.
                    Where(Function(x) x IsNot Nothing AndAlso x.Actif).
                    Select(Function(x) x).
                    ToList()
            End If

            For Each item As TypeVenteProduitDTO In typesPersonnalises
                If liste.Any(Function(x) String.Equals(x.Nom, item.Nom, StringComparison.OrdinalIgnoreCase)) Then
                    Continue For
                End If

                Dim prixVente As Decimal = item.PrixVente
                Dim coefficient As Decimal = If(item.Coefficient.HasValue, item.Coefficient.Value, 0D)
                If String.Equals(item.ModePrix, "COEFFICIENT", StringComparison.OrdinalIgnoreCase) AndAlso coefficient > 0D Then
                    Dim nb As Decimal = If(nbUniteParBase > 0D, nbUniteParBase, 1D)
                    Dim coutEquivalent As Decimal = prixAchat * (item.QuantiteEquivalent / nb)
                    If coutEquivalent > 0D Then
                        prixVente = Math.Round(coutEquivalent * coefficient, 2)
                    End If
                End If

                liste.Add(New TypeVenteDTO With {
                    .TypeVenteProduitId = item.TypeVenteProduitId,
                    .Nom = item.Nom,
                    .QuantiteEquivalent = item.QuantiteEquivalent,
                    .ModePrix = item.ModePrix,
                    .Coefficient = coefficient,
                    .PrixVente = prixVente,
                    .Actif = item.Actif,
                    .EstPersonnalise = True
                })
            Next

            Return liste
        End Function

        Public Function CalculerCoefficient(prixAchat As Decimal, prixVente As Decimal) As Decimal
            If prixAchat <= 0D OrElse prixVente <= 0D Then
                Return 0D
            End If
            Return Math.Round(prixVente / prixAchat, 4)
        End Function

        Public Function FormaterStock(stockReel As Decimal, nbUniteParBase As Decimal, uniteBase As String, uniteSecondaire As String) As String
            Return FormatageGlobal.FormatStockAvecEquivalence(stockReel, nbUniteParBase, uniteBase, uniteSecondaire)
        End Function
    End Class
End Namespace
