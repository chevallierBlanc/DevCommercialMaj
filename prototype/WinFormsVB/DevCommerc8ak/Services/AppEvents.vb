Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Module AppEvents
        Public Event VenteCreee As EventHandler
        Public Event VenteValidee As EventHandler
        Public Event PaiementValide As EventHandler
        Public Event StockModifie As EventHandler
        Public Event ProduitModifie As EventHandler
        Public Event DepenseAjoutee As EventHandler
        Public Event CaisseModifiee As EventHandler
        Public Event AnalyseVenteModifiee As EventHandler
        Public Event DataChanged As EventHandler

        Public Sub OnVenteCreee()
            RaiseEvent VenteCreee(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnVenteValidee()
            RaiseEvent VenteValidee(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnPaiementValide()
            RaiseEvent PaiementValide(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnStockModifie()
            RaiseEvent StockModifie(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnProduitModifie()
            RaiseEvent ProduitModifie(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnDepenseAjoutee()
            RaiseEvent DepenseAjoutee(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnCaisseModifiee()
            RaiseEvent CaisseModifiee(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnAnalyseVenteModifiee()
            RaiseEvent AnalyseVenteModifiee(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnDataChanged()
            RaiseEvent DataChanged(Nothing, EventArgs.Empty)
        End Sub
    End Module
End Namespace
