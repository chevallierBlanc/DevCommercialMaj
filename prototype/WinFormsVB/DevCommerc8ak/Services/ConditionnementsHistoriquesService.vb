Option Strict On
Option Explicit On
Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Web.Script.Serialization

Namespace DevCommerc8ak
    Public NotInheritable Class ConditionnementsHistoriquesService
        Private Sub New()
        End Sub

        ' L'ordre est celui des lignes persistantes, comme dans Snapshot.
        ' Toute divergence invalide l'instantane entier : aucun melange de versions.
        Public Shared Sub Appliquer(json As String, lignes As List(Of FacturePrintLine))
            If lignes Is Nothing Then Return
            For Each ligne As FacturePrintLine In lignes
                ligne.UnitePrincipaleImpression = Nothing
                ligne.UniteSecondaireImpression = Nothing
                ligne.TypePersonnaliseImpression = False
            Next
            If String.IsNullOrWhiteSpace(json) Then Return
            Dim source As System.Collections.IList
            Try
                Dim serializer As New JavaScriptSerializer() With {.MaxJsonLength = Integer.MaxValue}
                Dim racine As Dictionary(Of String, Object) = TryCast(serializer.DeserializeObject(json), Dictionary(Of String, Object))
                If racine Is Nothing OrElse Not racine.ContainsKey("Lignes") Then Return
                source = TryCast(racine("Lignes"), System.Collections.IList)
                If source Is Nothing OrElse source.Count <> lignes.Count Then Return
                For i As Integer = 0 To lignes.Count - 1
                    Dim l As Dictionary(Of String, Object) = TryCast(source(i), Dictionary(Of String, Object))
                    If l Is Nothing Then Return
                    For Each cle As String In New String() {"ProduitId", "TypeVente", "Quantite", "QuantiteSaisie", "PrixUnitaire", "MontantLigne", "TypeTarifId", "UnitePrincipaleImpression", "UniteSecondaireImpression"}
                        If Not l.ContainsKey(cle) Then Return
                    Next
                    For Each cle As String In New String() {"UnitePrincipaleImpression", "UniteSecondaireImpression"}
                        If l(cle) IsNot Nothing AndAlso Not TypeOf l(cle) Is String Then Return
                    Next
                    If Convert.ToInt32(l("TypeTarifId"), CultureInfo.InvariantCulture) < 0 Then Return
                    Dim qte As Decimal = Convert.ToDecimal(If(l("QuantiteSaisie"), l("Quantite")), CultureInfo.InvariantCulture)
                    If Convert.ToInt32(l("ProduitId"), CultureInfo.InvariantCulture) <> lignes(i).ProduitId OrElse
                       Convert.ToString(l("TypeVente")) <> lignes(i).Conditionnement OrElse qte <> lignes(i).Quantite OrElse
                       Convert.ToDecimal(l("PrixUnitaire"), CultureInfo.InvariantCulture) <> lignes(i).PrixUnitaire OrElse
                       Convert.ToDecimal(l("MontantLigne"), CultureInfo.InvariantCulture) <> lignes(i).Montant Then Return
                Next
            Catch ex As ArgumentException
                Return ' Ancienne trace incomplete ou JSON non exploitable : repli fidele.
            Catch ex As InvalidCastException
                Return
            Catch ex As FormatException
                Return
            Catch ex As OverflowException
                Return
            Catch ex As InvalidOperationException
                Return
            End Try
            For i As Integer = 0 To lignes.Count - 1
                Dim l As Dictionary(Of String, Object) = DirectCast(source(i), Dictionary(Of String, Object))
                lignes(i).UnitePrincipaleImpression = Convert.ToString(l("UnitePrincipaleImpression"))
                lignes(i).UniteSecondaireImpression = Convert.ToString(l("UniteSecondaireImpression"))
                lignes(i).TypePersonnaliseImpression = Convert.ToInt32(l("TypeTarifId"), CultureInfo.InvariantCulture) > 0
            Next
        End Sub
    End Class
End Namespace
