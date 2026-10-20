Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.Data

Namespace DevCommerc8ak
    Public NotInheritable Class TarifCatalogueRegles
        Private Sub New()
        End Sub
        ' Disponibilite historique du catalogue, partagee avec la delegation.
        ' Quart et special n'ont pas de drapeau : leur prix positif les active.
        Public Shared Function Standards(gros As Decimal, demi As Decimal, piece As Decimal, quart As Decimal, douzaine As Decimal, special As Decimal, venteGros As Boolean, venteDemi As Boolean, venteDetail As Boolean, venteDouzaine As Boolean) As List(Of String)
            Dim noms As New List(Of String)
            If venteGros AndAlso gros > 0D Then noms.Add("gros")
            If venteDemi AndAlso demi > 0D Then noms.Add("demi")
            If venteDetail AndAlso piece > 0D Then noms.Add("piece")
            If quart > 0D Then noms.Add("quart")
            If venteDouzaine AndAlso douzaine > 0D Then noms.Add("douzaine")
            If special > 0D Then noms.Add("speciale") : noms.Add("promo")
            Return noms
        End Function
        Public Shared Function Marge(prix As Decimal, cout As Decimal) As Decimal?
            If cout <= 0D Then Return Nothing
            Return Math.Round((prix / cout - 1D) * 100D, 2)
        End Function
        Public Shared Function StandardsDepuisProduit(produit As DataRow) As List(Of String)
            Dim prix As Func(Of String, Decimal) = Function(champ) If(Not produit.Table.Columns.Contains(champ) OrElse produit.IsNull(champ), 0D, Convert.ToDecimal(produit(champ)))
            Dim actif As Func(Of String, Boolean) = Function(champ) produit.Table.Columns.Contains(champ) AndAlso Not produit.IsNull(champ) AndAlso Convert.ToBoolean(produit(champ))
            Return Standards(prix("PrixGros"), prix("PrixDemi"), prix("PrixDetail"), prix("PrixQuart"), prix("PrixDouzaine"), prix("PrixSpecial"), actif("VenteGros"), actif("VenteDemi"), actif("VenteDetail"), actif("VenteDouzaine"))
        End Function
        Public Shared Function Coefficient(prix As Decimal, cout As Decimal) As Decimal?
            If cout <= 0D Then Return Nothing
            Return Math.Round(prix / cout, 4)
        End Function
    End Class
End Namespace
