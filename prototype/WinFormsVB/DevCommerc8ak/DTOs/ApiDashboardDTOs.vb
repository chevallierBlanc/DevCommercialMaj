Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class ApiLoginRequest
        Public Property Username As String
        Public Property Password As String
    End Class

    Public Class ApiTokenResponse
        Public Property AccessToken As String
        Public Property AccessTokenExpiresAtUtc As DateTime
        Public Property RefreshToken As String
        Public Property RefreshTokenExpiresAtUtc As DateTime
        Public Property Username As String
        Public Property Role As String
    End Class

    Public Class ApiDashboardMetric
        Public Property Label As String
        Public Property Value As Decimal
    End Class

    Public Class ApiDailyProductRow
        Public Property Product As String
        Public Property QuantitySold As Decimal
        Public Property TypeVente As String
        Public Property AmountGenerated As Decimal
        Public Property Hour As DateTime
        Public Property Agent As String
    End Class

    Public Class ApiManualExitRow
        Public Property Product As String
        Public Property Quantity As Decimal
        Public Property Motif As String
        Public Property Category As String
        Public Property User As String
        Public Property [Date] As DateTime
        Public Property Amount As Decimal
        Public Property Observation As String
    End Class

    Public Class ApiExpenseRow
        Public Property Category As String
        Public Property Amount As Decimal
    End Class

    Public Class ApiStockAlertRow
        Public Property Product As String
        Public Property Stock As Decimal
    End Class

    Public Class ApiPeriodSeriesPoint
        Public Property Label As String
        Public Property Value As Decimal
    End Class

    Public Class ApiJournalierDashboardResponse
        Public Property [Date] As DateTime
        Public Property CaDuJour As Decimal
        Public Property TotalSorties As Decimal
        Public Property DepensesDuJour As Decimal
        Public Property BeneficeEstime As Decimal
        Public Property TotalEntrees As Decimal
        Public Property TotalVentes As Decimal
        Public Property TotalSortiesManuelles As Decimal
        Public Property TotalPertes As Decimal
        Public Property TotalDons As Decimal
        Public Property TotalAllocations As Decimal
        Public Property TotalDettesClients As Decimal
        Public Property TotalDettesBoss As Decimal
        Public Property TotalSortiesHorsCaisse As Decimal
        Public Property TotalGros As Decimal
        Public Property TotalDemi As Decimal
        Public Property TotalQuart As Decimal
        Public Property TotalPiece As Decimal
        Public Property TotalDouzaine As Decimal
        Public Property MontantTotalGenere As Decimal
        Public Property ProduitsVendus As List(Of ApiDailyProductRow) = New List(Of ApiDailyProductRow)()
        Public Property SortiesManuelles As List(Of ApiManualExitRow) = New List(Of ApiManualExitRow)()
        Public Property DepensesParCategorie As List(Of ApiExpenseRow) = New List(Of ApiExpenseRow)()
        Public Property AlertesStockFaible As List(Of ApiStockAlertRow) = New List(Of ApiStockAlertRow)()
        Public Property SeriesVentes As List(Of ApiPeriodSeriesPoint) = New List(Of ApiPeriodSeriesPoint)()
        Public Property SeriesDepenses As List(Of ApiPeriodSeriesPoint) = New List(Of ApiPeriodSeriesPoint)()
    End Class

    Public Class ApiMensuelDashboardResponse
        Public Property Year As Integer
        Public Property Month As Integer
        Public Property CaMensuel As Decimal
        Public Property DepensesMensuelles As Decimal
        Public Property BeneficeEstime As Decimal
        Public Property TotalEntrees As Decimal
        Public Property TotalVentes As Decimal
        Public Property TotalSortiesManuelles As Decimal
        Public Property TotalPertes As Decimal
        Public Property TotalDons As Decimal
        Public Property TotalAllocations As Decimal
        Public Property TotalDettesClients As Decimal
        Public Property TotalDettesBoss As Decimal
        Public Property TotalSortiesHorsCaisse As Decimal
        Public Property TotalGros As Decimal
        Public Property TotalDemi As Decimal
        Public Property TotalQuart As Decimal
        Public Property TotalPiece As Decimal
        Public Property TotalDouzaine As Decimal
        Public Property MontantTotalGenere As Decimal
        Public Property TopProduits As List(Of ApiDailyProductRow) = New List(Of ApiDailyProductRow)()
        Public Property TopDepenses As List(Of ApiExpenseRow) = New List(Of ApiExpenseRow)()
        Public Property EvolutionVentes As List(Of ApiPeriodSeriesPoint) = New List(Of ApiPeriodSeriesPoint)()
        Public Property EvolutionSorties As List(Of ApiPeriodSeriesPoint) = New List(Of ApiPeriodSeriesPoint)()
        Public Property EvolutionDepenses As List(Of ApiPeriodSeriesPoint) = New List(Of ApiPeriodSeriesPoint)()
    End Class

    Public Class ApiAnnuelDashboardResponse
        Public Property Year As Integer
        Public Property CaAnnuel As Decimal
        Public Property DepensesAnnuelles As Decimal
        Public Property BeneficeEstime As Decimal
        Public Property TotalEntrees As Decimal
        Public Property TotalVentes As Decimal
        Public Property TotalSortiesManuelles As Decimal
        Public Property TotalPertes As Decimal
        Public Property TotalDons As Decimal
        Public Property TotalAllocations As Decimal
        Public Property TotalDettesClients As Decimal
        Public Property TotalDettesBoss As Decimal
        Public Property TotalSortiesHorsCaisse As Decimal
        Public Property TotalGros As Decimal
        Public Property TotalDemi As Decimal
        Public Property TotalQuart As Decimal
        Public Property TotalPiece As Decimal
        Public Property TotalDouzaine As Decimal
        Public Property MontantTotalGenere As Decimal
        Public Property VentesParMois As List(Of ApiPeriodSeriesPoint) = New List(Of ApiPeriodSeriesPoint)()
        Public Property DepensesParMois As List(Of ApiPeriodSeriesPoint) = New List(Of ApiPeriodSeriesPoint)()
        Public Property BeneficesParMois As List(Of ApiPeriodSeriesPoint) = New List(Of ApiPeriodSeriesPoint)()
        Public Property CategoriesDepensesGourmandes As List(Of ApiExpenseRow) = New List(Of ApiExpenseRow)()
        Public Property TopProduits As List(Of ApiDailyProductRow) = New List(Of ApiDailyProductRow)()
    End Class
End Namespace
