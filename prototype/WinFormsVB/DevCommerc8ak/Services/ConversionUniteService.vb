Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq

Namespace DevCommerc8ak
    Public NotInheritable Class ConversionUniteService
        Private Sub New()
        End Sub

        Public Shared Function ConvertirVersBase(quantite As Decimal, conditionnement As ProduitConditionnementDTO) As Decimal
            If conditionnement Is Nothing Then Throw New ArgumentNullException("conditionnement")
            If quantite < 0D Then Throw New ArgumentOutOfRangeException("quantite", "La quantite ne peut pas etre negative.")
            If conditionnement.FacteurVersBase <= 0D Then Throw New InvalidOperationException("Le facteur vers base du conditionnement doit etre strictement positif.")

            ValiderQuantite(quantite, conditionnement)
            Return quantite * conditionnement.FacteurVersBase
        End Function

        Public Shared Function CalculerQuantiteBase(quantiteCommerciale As Decimal,
                                                    typeVente As TypeVenteProduitDTO,
                                                    conditionnement As ProduitConditionnementDTO) As Decimal
            If quantiteCommerciale < 0D Then Throw New ArgumentOutOfRangeException("quantiteCommerciale", "La quantite ne peut pas etre negative.")
            If typeVente Is Nothing Then Throw New ArgumentNullException("typeVente")

            Dim coefficient As Decimal = Math.Max(0D, typeVente.QuantiteEquivalent)
            If coefficient <= 0D OrElse quantiteCommerciale = 0D Then Return 0D

            If typeVente.ProduitConditionnementId.HasValue Then
                If conditionnement Is Nothing Then Throw New InvalidOperationException("Le type de vente reference un conditionnement introuvable.")
                If conditionnement.ProduitConditionnementId <> typeVente.ProduitConditionnementId.Value Then
                    Throw New InvalidOperationException("Le conditionnement fourni ne correspond pas au type de vente.")
                End If

                Return quantiteCommerciale * coefficient * conditionnement.FacteurVersBase
            End If

            ' Compatibilite : les anciens types stockent deja une quantite equivalente en base.
            Return quantiteCommerciale * coefficient
        End Function

        Public Shared Sub ValiderQuantite(quantite As Decimal, conditionnement As ProduitConditionnementDTO)
            If conditionnement Is Nothing Then Throw New ArgumentNullException("conditionnement")
            If quantite < 0D Then Throw New ArgumentOutOfRangeException("quantite", "La quantite ne peut pas etre negative.")
            If Not conditionnement.AutoriseFraction AndAlso Decimal.Truncate(quantite) <> quantite Then
                Throw New InvalidOperationException("Ce conditionnement n'autorise pas les quantites fractionnaires.")
            End If
        End Sub

        Public Shared Function DecomposerStock(quantiteBase As Decimal, conditionnements As IEnumerable(Of ProduitConditionnementDTO)) As String
            If conditionnements Is Nothing Then
                Return FormaterDecimal(quantiteBase)
            End If

            Dim actifs As List(Of ProduitConditionnementDTO) = conditionnements.
                Where(Function(c) c IsNot Nothing AndAlso c.EstActif AndAlso c.FacteurVersBase > 0D).
                OrderByDescending(Function(c) c.FacteurVersBase).
                ToList()

            If actifs.Count = 0 Then
                Return FormaterDecimal(quantiteBase)
            End If

            Dim baseUnite As ProduitConditionnementDTO = actifs.FirstOrDefault(Function(c) c.EstUniteBase)
            If baseUnite IsNot Nothing AndAlso baseUnite.AutoriseFraction AndAlso actifs.Count = 1 Then
                Return FormaterDecimal(quantiteBase) & " " & ObtenirLibelleUnite(baseUnite)
            End If

            Dim restant As Decimal = Math.Max(0D, quantiteBase)
            Dim morceaux As New List(Of String)()

            For Each conditionnement As ProduitConditionnementDTO In actifs
                Dim facteur As Decimal = conditionnement.FacteurVersBase
                If facteur <= 0D Then Continue For

                Dim quantiteConditionnement As Decimal
                If conditionnement.EstUniteBase AndAlso conditionnement.AutoriseFraction Then
                    quantiteConditionnement = restant / facteur
                    restant = 0D
                Else
                    quantiteConditionnement = Decimal.Floor(restant / facteur)
                    restant -= quantiteConditionnement * facteur
                End If

                If quantiteConditionnement > 0D OrElse (conditionnement.EstUniteBase AndAlso morceaux.Count = 0) Then
                    morceaux.Add(FormaterDecimal(quantiteConditionnement) & " " & ObtenirLibelleUnite(conditionnement))
                End If

                If restant <= 0D Then Exit For
            Next

            If morceaux.Count = 0 Then
                Return "0 " & ObtenirLibelleUnite(actifs.Last())
            End If

            Return String.Join(" + ", morceaux)
        End Function

        Private Shared Function ObtenirLibelleUnite(conditionnement As ProduitConditionnementDTO) As String
            If conditionnement Is Nothing Then Return String.Empty
            If Not String.IsNullOrWhiteSpace(conditionnement.SymboleUnite) Then Return conditionnement.SymboleUnite.Trim()
            If Not String.IsNullOrWhiteSpace(conditionnement.LibelleUnite) Then Return conditionnement.LibelleUnite.Trim()
            Return If(conditionnement.CodeUnite, String.Empty).Trim()
        End Function

        Private Shared Function FormaterDecimal(valeur As Decimal) As String
            Return valeur.ToString("0.####", CultureInfo.InvariantCulture)
        End Function
    End Class
End Namespace
