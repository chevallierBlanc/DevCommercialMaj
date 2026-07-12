Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Drawing.Printing
Imports System.Linq
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public NotInheritable Class PrintConfigurationHelper
        Private Sub New()
        End Sub

        Public Shared Function ChargerParametres() As ParametreDTO
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim service As New ParametreService(New ParametreRepository(dal))
            Return service.Charger()
        End Function

        Public Shared Function ConfigurerDocumentA4(doc As PrintDocument, owner As IWin32Window, moduleName As String, actionName As String, Optional paysage As Boolean = False) As ParametreDTO
            Dim param As ParametreDTO = ChargerParametres()
            Dim printerName As String = ResoudreImprimante(If(param Is Nothing, String.Empty, param.ImprimanteA4), "A4", owner, moduleName, actionName)

            If Not String.IsNullOrWhiteSpace(printerName) Then
                doc.PrinterSettings.PrinterName = printerName
            End If

            doc.OriginAtMargins = True
            doc.DefaultPageSettings.Margins = New Margins(30, 30, 30, 30)
            doc.DefaultPageSettings.Color = If(param IsNot Nothing, param.ImpressionCouleur, True)
            doc.DefaultPageSettings.Landscape = paysage
            doc.DefaultPageSettings.PaperSize = New PaperSize("A4", 827, 1169)
            Return param
        End Function

        Public Shared Function ConfigurerDocumentThermique(doc As PrintDocument, owner As IWin32Window, moduleName As String, actionName As String, Optional largeur As Integer = 315, Optional hauteur As Integer = 1200) As ParametreDTO
            Dim param As ParametreDTO = ChargerParametres()
            Dim printerName As String = ResoudreImprimante(If(param Is Nothing, String.Empty, param.ImprimanteTicket), "thermique", owner, moduleName, actionName)

            If Not String.IsNullOrWhiteSpace(printerName) Then
                doc.PrinterSettings.PrinterName = printerName
            End If

            doc.DefaultPageSettings.PaperSize = New PaperSize("Ticket80mm", largeur, hauteur)
            Return param
        End Function

        Private Shared Function ResoudreImprimante(imprimanteConfiguree As String, typeImprimante As String, owner As IWin32Window, moduleName As String, actionName As String) As String
            Dim installed As String() = PrinterSettings.InstalledPrinters.Cast(Of String)().ToArray()
            If installed.Length = 0 Then
                Return String.Empty
            End If

            If Not String.IsNullOrWhiteSpace(imprimanteConfiguree) AndAlso installed.Any(Function(p) String.Equals(p, imprimanteConfiguree, StringComparison.OrdinalIgnoreCase)) Then
                Return imprimanteConfiguree
            End If

            Dim imprimanteParDefaut As String = New PrinterSettings().PrinterName
            If String.IsNullOrWhiteSpace(imprimanteParDefaut) OrElse Not installed.Any(Function(p) String.Equals(p, imprimanteParDefaut, StringComparison.OrdinalIgnoreCase)) Then
                imprimanteParDefaut = installed(0)
            End If

            Dim log As New ProductionLogService()
            If Not String.IsNullOrWhiteSpace(imprimanteConfiguree) Then
                log.Warn(moduleName, actionName, "Imprimante " & typeImprimante & " introuvable : " & imprimanteConfiguree & ". Bascule vers " & imprimanteParDefaut & ".")
                MessageBox.Show(owner, "L'imprimante " & typeImprimante & " configurée est introuvable :" & Environment.NewLine &
                                imprimanteConfiguree & Environment.NewLine & Environment.NewLine &
                                "L'application utilisera l'imprimante Windows par défaut :" & Environment.NewLine &
                                imprimanteParDefaut, "Imprimante indisponible", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If

            Return imprimanteParDefaut
        End Function
    End Class
End Namespace
