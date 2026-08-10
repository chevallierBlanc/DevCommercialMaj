Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Module FormatageGlobal
        Public Function FormatNombre(valeur As Decimal) As String
            Return valeur.ToString("N0")
        End Function

        Public Function FormatMontant(valeur As Decimal) As String
            Return valeur.ToString("N0") & " FC"
        End Function

        Public Function FormatPourcentage(valeur As Decimal) As String
            Return valeur.ToString("N0") & " %"
        End Function

        Public Function FormatQuantitePhysique(valeur As Decimal) As String
            Dim texte As String = valeur.ToString("N2")
            texte = texte.TrimEnd("0"c).TrimEnd(","c).TrimEnd("."c)
            If texte = String.Empty Then Return "0"
            Return texte
        End Function

        Public Function FormatStockAvecEquivalence(stockReel As Decimal, conversionUnite As Decimal, uniteBase As String, uniteSecondaire As String) As String
            Dim unitePrincipale As String = If(String.IsNullOrWhiteSpace(uniteBase), "base", uniteBase.Trim())
            Dim uniteSeconde As String = If(String.IsNullOrWhiteSpace(uniteSecondaire), "pièce", uniteSecondaire.Trim())

            If conversionUnite <= 0D Then
                Return FormatNombre(stockReel) & " " & unitePrincipale
            End If

            Dim cartons As Decimal = Decimal.Floor(stockReel / conversionUnite)
            Dim pieces As Decimal = stockReel - (cartons * conversionUnite)
            Return FormatNombre(cartons) & " " & unitePrincipale & " + " & FormatNombre(pieces) & " " & uniteSeconde
        End Function

        Public Function FormatStockSelonGestion(stockReel As Decimal,
                                                conversionUnite As Decimal,
                                                unitePrincipale As String,
                                                uniteSecondaire As String,
                                                typeGestionStock As String,
                                                uniteMesureStock As String,
                                                contenuUnitePrincipale As Decimal,
                                                Optional contenuUniteSecondaire As Decimal = 0D) As String
            If StockUnitConversionService.EstGestionMesuree(typeGestionStock) Then
                Dim unitePrincipaleAffichee As String = If(String.IsNullOrWhiteSpace(unitePrincipale), "unité", unitePrincipale.Trim())
                Dim uniteSecondaireAffichee As String = If(String.IsNullOrWhiteSpace(uniteSecondaire), String.Empty, uniteSecondaire.Trim())
                Dim uniteMesureAffichee As String = If(String.IsNullOrWhiteSpace(uniteMesureStock), "mesure", uniteMesureStock.Trim())
                Dim contenuPrincipal As Decimal = contenuUnitePrincipale
                If contenuPrincipal <= 0D Then contenuPrincipal = conversionUnite
                Dim stockPhysique As String = FormatQuantitePhysique(stockReel) & " " & uniteMesureAffichee

                If contenuPrincipal <= 0D Then
                    Return stockPhysique
                End If

                Dim quantitePrincipale As Decimal = Decimal.Floor(stockReel / contenuPrincipal)
                Dim resteMesure As Decimal = stockReel - (quantitePrincipale * contenuPrincipal)

                If contenuUniteSecondaire > 0D AndAlso uniteSecondaireAffichee <> String.Empty Then
                    Dim quantiteSecondaire As Decimal = Decimal.Floor(resteMesure / contenuUniteSecondaire)
                    Dim restePhysique As Decimal = resteMesure - (quantiteSecondaire * contenuUniteSecondaire)
                    Dim decomposition As String = FormatNombre(quantitePrincipale) & " " & unitePrincipaleAffichee & " + " & FormatNombre(quantiteSecondaire) & " " & uniteSecondaireAffichee
                    If restePhysique > 0D Then
                        decomposition &= " + " & FormatQuantitePhysique(restePhysique) & " " & uniteMesureAffichee
                    End If

                    Return stockPhysique & " = " & decomposition
                End If

                If resteMesure <= 0D Then
                    If quantitePrincipale <= 0D Then Return stockPhysique
                    Return stockPhysique & " = " & FormatNombre(quantitePrincipale) & " " & unitePrincipaleAffichee
                End If

                If quantitePrincipale <= 0D Then
                    Return stockPhysique
                End If

                Return stockPhysique & " = " & FormatNombre(quantitePrincipale) & " " & unitePrincipaleAffichee & " + " & FormatQuantitePhysique(resteMesure) & " " & uniteMesureAffichee
            End If

            Return FormatStockAvecEquivalence(stockReel, conversionUnite, unitePrincipale, uniteSecondaire)
        End Function
    End Module
End Namespace
