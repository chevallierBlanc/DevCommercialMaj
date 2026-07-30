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
        Public Event RolePermissionsChanged As EventHandler
        Public Event CaissePhysiqueModifiee As EventHandler

        Public Sub OnVenteCreee()
            AppDataVersionService.Touch("FACTURES")
            RaiseEvent VenteCreee(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnVenteValidee()
            AppDataVersionService.Touch("FACTURES", "STOCK")
            RaiseEvent VenteValidee(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnPaiementValide()
            AppDataVersionService.Touch("PAIEMENTS", "FACTURES", "FINANCE")
            RaiseEvent PaiementValide(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnStockModifie()
            AppDataVersionService.Touch("STOCK")
            RaiseEvent StockModifie(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnProduitModifie()
            AppDataVersionService.Touch("PRODUITS", "TYPES_VENTE")
            RaiseEvent ProduitModifie(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnDepenseAjoutee()
            AppDataVersionService.Touch("FINANCE")
            RaiseEvent DepenseAjoutee(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnCaisseModifiee()
            AppDataVersionService.Touch("FINANCE")
            RaiseEvent CaisseModifiee(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnAnalyseVenteModifiee()
            RaiseEvent AnalyseVenteModifiee(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnDataChanged()
            RaiseEvent DataChanged(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnRolePermissionsChanged()
            RaiseEvent RolePermissionsChanged(Nothing, EventArgs.Empty)
        End Sub

        Public Sub OnCaissePhysiqueModifiee()
            AppDataVersionService.Touch("FINANCE")
            RaiseEvent CaissePhysiqueModifiee(Nothing, EventArgs.Empty)
        End Sub
    End Module
End Namespace
