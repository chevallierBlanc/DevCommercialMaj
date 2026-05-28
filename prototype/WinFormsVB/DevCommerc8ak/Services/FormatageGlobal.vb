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
    End Module
End Namespace
