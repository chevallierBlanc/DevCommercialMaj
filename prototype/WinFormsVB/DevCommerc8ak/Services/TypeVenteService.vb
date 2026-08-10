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
                                             venteDouzaine As Boolean,
                                             Optional typeGestionStock As String = "UNITE",
                                             Optional contenuUnitePrincipale As Decimal = 0D,
                                             Optional contenuUniteSecondaire As Decimal = 0D,
                                             Optional uniteSecondaire As String = "") As List(Of TypeVenteDTO)
            Dim liste As New List(Of TypeVenteDTO)()
            Dim nb As Decimal = If(nbUniteParBase > 0D, nbUniteParBase, 1D)
            Dim coeffGros As Decimal = CalculerCoefficient(prixAchat, prixGros)
            Dim coeffDetail As Decimal = CalculerCoefficient(prixAchat, prixPiece * nb)
            Dim libelleDetail As String = If(StockUnitConversionService.EstGestionMesuree(typeGestionStock) AndAlso Not String.IsNullOrWhiteSpace(uniteSecondaire), uniteSecondaire.Trim(), Nothing)
            Dim quantiteGros As Decimal? = StockUnitConversionService.CalculerQuantiteBaseTypeStandard("GROS", nb, typeGestionStock, contenuUnitePrincipale, contenuUniteSecondaire)
            Dim quantiteDemi As Decimal? = StockUnitConversionService.CalculerQuantiteBaseTypeStandard("DEMI", nb, typeGestionStock, contenuUnitePrincipale, contenuUniteSecondaire)
            Dim quantiteQuart As Decimal? = StockUnitConversionService.CalculerQuantiteBaseTypeStandard("QUART", nb, typeGestionStock, contenuUnitePrincipale, contenuUniteSecondaire)
            Dim quantiteDetail As Decimal? = StockUnitConversionService.CalculerQuantiteBaseTypeStandard("DETAIL", nb, typeGestionStock, contenuUnitePrincipale, contenuUniteSecondaire)
            Dim quantiteDouzaine As Decimal? = StockUnitConversionService.CalculerQuantiteBaseTypeStandard("DOUZAINE", nb, typeGestionStock, contenuUnitePrincipale, contenuUniteSecondaire)

            If venteGros AndAlso prixGros > 0D AndAlso quantiteGros.HasValue Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "gros",
                    .QuantiteEquivalent = quantiteGros.Value,
                    .TypeUniteEquivalent = "SECONDAIRE",
                    .TypeQuantiteEquivalent = "SECONDAIRE",
                    .ModePrix = "COEFFICIENT",
                    .Coefficient = coeffGros,
                    .PrixVente = prixGros,
                    .Actif = True,
                    .EstPersonnalise = False
                })
            End If

            If venteDemi AndAlso prixDemi > 0D AndAlso quantiteDemi.HasValue Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "demi",
                    .QuantiteEquivalent = quantiteDemi.Value,
                    .TypeUniteEquivalent = "SECONDAIRE",
                    .TypeQuantiteEquivalent = "SECONDAIRE",
                    .ModePrix = "COEFFICIENT",
                    .Coefficient = coeffGros,
                    .PrixVente = prixDemi,
                    .Actif = True,
                    .EstPersonnalise = False
                })
            End If

            If prixQuart > 0D AndAlso quantiteQuart.HasValue Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "quart",
                    .QuantiteEquivalent = quantiteQuart.Value,
                    .TypeUniteEquivalent = "SECONDAIRE",
                    .TypeQuantiteEquivalent = "SECONDAIRE",
                    .ModePrix = "COEFFICIENT",
                    .Coefficient = coeffDetail,
                    .PrixVente = prixQuart,
                    .Actif = True,
                    .EstPersonnalise = False
                })
            End If

            If venteDetail AndAlso prixPiece > 0D AndAlso quantiteDetail.HasValue Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "piece",
                    .QuantiteEquivalent = quantiteDetail.Value,
                    .TypeUniteEquivalent = "SECONDAIRE",
                    .TypeQuantiteEquivalent = "SECONDAIRE",
                    .ModePrix = "COEFFICIENT",
                    .Coefficient = coeffDetail,
                    .PrixVente = prixPiece,
                    .Actif = True,
                    .EstPersonnalise = False,
                    .LibelleAffichage = libelleDetail
                })
            End If

            If venteDouzaine AndAlso prixDouzaine > 0D AndAlso quantiteDouzaine.HasValue Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "douzaine",
                    .QuantiteEquivalent = quantiteDouzaine.Value,
                    .TypeUniteEquivalent = "SECONDAIRE",
                    .TypeQuantiteEquivalent = "SECONDAIRE",
                    .ModePrix = "COEFFICIENT",
                    .Coefficient = coeffDetail,
                    .PrixVente = prixDouzaine,
                    .Actif = True,
                    .EstPersonnalise = False
                })
            End If

            If prixSpecial > 0D AndAlso quantiteDetail.HasValue Then
                liste.Add(New TypeVenteDTO With {
                    .Nom = "speciale",
                    .QuantiteEquivalent = quantiteDetail.Value,
                    .TypeUniteEquivalent = "SECONDAIRE",
                    .TypeQuantiteEquivalent = "SECONDAIRE",
                    .ModePrix = "COEFFICIENT",
                    .Coefficient = CalculerCoefficient(prixAchat, prixSpecial),
                    .PrixVente = prixSpecial,
                    .Actif = True,
                    .EstPersonnalise = False
                })
                liste.Add(New TypeVenteDTO With {
                    .Nom = "promo",
                    .QuantiteEquivalent = quantiteDetail.Value,
                    .TypeUniteEquivalent = "SECONDAIRE",
                    .TypeQuantiteEquivalent = "SECONDAIRE",
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
                                                        Optional typesPersonnalisesOverrides As IEnumerable(Of TypeVenteProduitDTO) = Nothing,
                                                        Optional contenuUnitePrincipale As Decimal = 0D,
                                                        Optional contenuUniteSecondaire As Decimal = 0D,
                                                        Optional typeGestionStock As String = "UNITE",
                                                        Optional uniteSecondaire As String = "") As List(Of TypeVenteDTO)
            Dim liste As List(Of TypeVenteDTO) = ConstruireTypesVente(nbUniteParBase, prixAchat, prixGros, prixDemi, prixPiece, prixQuart, prixDouzaine, prixSpecial, venteGros, venteDemi, venteDetail, venteDouzaine, typeGestionStock, contenuUnitePrincipale, contenuUniteSecondaire, uniteSecondaire)
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
                Dim nb As Decimal = If(nbUniteParBase > 0D, nbUniteParBase, 1D)
                Dim typeQuantite As String = If(String.IsNullOrWhiteSpace(item.TypeQuantiteEquivalent), item.TypeUniteEquivalent, item.TypeQuantiteEquivalent)
                Dim contenuPrincipal As Decimal = If(contenuUnitePrincipale > 0D, contenuUnitePrincipale, nb)
                Dim quantiteBaseType As Decimal = CalculVenteService.CalculerQuantiteBaseTypeVente(item.QuantiteEquivalent, typeQuantite, nb, contenuPrincipal, contenuUniteSecondaire)
                If String.Equals(item.ModePrix, "COEFFICIENT", StringComparison.OrdinalIgnoreCase) AndAlso coefficient > 0D Then
                    Dim coutEquivalent As Decimal = prixAchat * (quantiteBaseType / contenuPrincipal)
                    If coutEquivalent > 0D Then
                        prixVente = Math.Round(coutEquivalent * coefficient, 2)
                    End If
                End If

                liste.Add(New TypeVenteDTO With {
                    .TypeVenteProduitId = item.TypeVenteProduitId,
                    .Nom = item.Nom,
                    .QuantiteEquivalent = quantiteBaseType,
                    .TypeUniteEquivalent = typeQuantite,
                    .TypeQuantiteEquivalent = typeQuantite,
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
