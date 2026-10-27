Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Windows.Forms
Imports Microsoft.VisualBasic

Namespace DevCommerc8ak
    Public NotInheritable Class PrintConfigurationHelper
        Private Const LocalFolderName As String = "CommercialPro"
        Private Const LocalPrinterFileName As String = "printer-settings.local"

        Private Sub New()
        End Sub

        Public Shared Function ChargerParametres() As ParametreDTO
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim service As New ParametreService(New ParametreRepository(dal))
            Dim param As ParametreDTO = service.Charger()
            If param Is Nothing Then
                param = New ParametreDTO With {
                    .ApercuAvantImpression = True,
                    .ImpressionCouleur = True
                }
            End If
            AppliquerImprimantesLocales(param)
            Return param
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

        Public Shared Sub AppliquerImprimantesLocales(param As ParametreDTO)
            If param Is Nothing Then
                Return
            End If

            Dim locales As Tuple(Of String, String) = ChargerImprimantesLocales()
            If locales IsNot Nothing Then
                If Not String.IsNullOrWhiteSpace(locales.Item1) Then
                    param.ImprimanteA4 = locales.Item1
                End If
                If Not String.IsNullOrWhiteSpace(locales.Item2) Then
                    param.ImprimanteTicket = locales.Item2
                End If
            End If
        End Sub

        Public Shared Function ChargerImprimantesLocales() As Tuple(Of String, String)
            Try
                Dim chemin As String = ObtenirCheminConfigurationLocale()
                If Not File.Exists(chemin) Then
                    Return Nothing
                End If

                Dim a4 As String = String.Empty
                Dim ticket As String = String.Empty
                For Each ligne As String In File.ReadAllLines(chemin)
                    If String.IsNullOrWhiteSpace(ligne) Then Continue For
                    Dim index As Integer = ligne.IndexOf("="c)
                    If index <= 0 Then Continue For
                    Dim cle As String = ligne.Substring(0, index).Trim()
                    Dim valeur As String = ligne.Substring(index + 1).Trim()
                    If cle.Equals("A4", StringComparison.OrdinalIgnoreCase) Then
                        a4 = valeur
                    ElseIf cle.Equals("THERMIQUE", StringComparison.OrdinalIgnoreCase) Then
                        ticket = valeur
                    End If
                Next

                Return Tuple.Create(a4, ticket)
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Warn("PrintConfigurationHelper", "ChargerImprimantesLocales", "Configuration imprimantes locale illisible : " & ex.Message)
                Return Nothing
            End Try
        End Function

        Public Shared Sub EnregistrerImprimantesLocales(imprimanteA4 As String, imprimanteThermique As String)
            Dim chemin As String = ObtenirCheminConfigurationLocale()
            Dim dossier As String = Path.GetDirectoryName(chemin)
            If Not Directory.Exists(dossier) Then
                Directory.CreateDirectory(dossier)
            End If

            File.WriteAllLines(chemin, New String() {
                "A4=" & NettoyerValeurLocale(imprimanteA4),
                "THERMIQUE=" & NettoyerValeurLocale(imprimanteThermique)
            })
        End Sub

        Public Shared Sub TesterImprimanteA4(owner As IWin32Window)
            Using doc As New PrintDocument()
                ConfigurerDocumentA4(doc, owner, "FormulaireParametres", "TesterImprimanteA4")
                AddHandler doc.PrintPage,
                    Sub(sender As Object, e As PrintPageEventArgs)
                        Using titre As New Font("Segoe UI", 16.0F, FontStyle.Bold),
                              texte As New Font("Segoe UI", 11.0F)
                            Dim y As Integer = e.MarginBounds.Top
                            e.Graphics.DrawString("COMMERCIAL PRO", titre, Brushes.Black, e.MarginBounds.Left, y)
                            y += 40
                            e.Graphics.DrawString("Test imprimante A4", texte, Brushes.Black, e.MarginBounds.Left, y)
                            y += 24
                            e.Graphics.DrawString("Nom imprimante : " & doc.PrinterSettings.PrinterName, texte, Brushes.Black, e.MarginBounds.Left, y)
                            y += 24
                            e.Graphics.DrawString("Date/heure : " & Date.Now.ToString("dd/MM/yyyy HH:mm:ss"), texte, Brushes.Black, e.MarginBounds.Left, y)
                        End Using
                        e.HasMorePages = False
                    End Sub
                doc.Print()
            End Using
        End Sub

        Public Shared Sub TesterImprimanteThermique(owner As IWin32Window)
            Dim param As ParametreDTO = ChargerParametres()
            Dim data As FacturePrintData = CreerDonneesTestThermique()
            Dim creerDocument As Func(Of PrintDocument) = Function() FacturePrintRenderer.CreerDocumentThermique(data, param, owner, "FormulaireParametres", "TesterImprimanteThermique")
            Using doc As PrintDocument = creerDocument()
                If param.ApercuAvantImpression Then
                    Using apercu As New FormApercuTicket(doc, creerDocument)
                        apercu.ShowDialog(owner)
                    End Using
                Else
                    doc.Print()
                End If
            End Using
        End Sub

        ' Jeu de demonstration autonome : aucun document, paiement ou stock reel.
        Public Shared Function CreerDonneesTestThermique() As FacturePrintData
            Return New FacturePrintData With {
                .DiagnosticThermique = True,
                .Numero = "TEST-80MM", .DateDocument = Date.Now,
                .TitreThermique = "TICKET DE TEST — IMPRIMANTE THERMIQUE",
                .MentionThermique = "TEST SANS VALEUR COMMERCIALE — AUCUNE VENTE ENREGISTRÉE",
                .LibelleTotalThermique = "TOTAL TEST",
                .SousTotal = 128730D, .Remise = 1000D, .Total = 127730D,
                .Lignes = New List(Of FacturePrintLine) From {
                    New FacturePrintLine With {.Produit = "Démonstration : café, thé et céréales — désignation longue pour vérifier le retour à la ligne", .Conditionnement = "pièce", .Quantite = 2D, .PrixUnitaire = 2500D, .Montant = 5000D},
                    New FacturePrintLine With {.Produit = "Article fictif — conditionnement personnalisé", .Conditionnement = "paquet 10+1", .Quantite = 1D, .PrixUnitaire = 23730D, .Montant = 23730D},
                    New FacturePrintLine With {.Produit = "Contrôle des grands montants", .Conditionnement = "carton", .Quantite = 4D, .PrixUnitaire = 25000D, .Montant = 100000D}
                }
            }
        End Function

        Public Shared Function ObtenirVersionApplication() As String
            Try
                Dim version As Version = Assembly.GetExecutingAssembly().GetName().Version
                If version IsNot Nothing Then
                    Return version.ToString()
                End If
            Catch
            End Try
            Return Application.ProductVersion
        End Function

        Private Shared Function ResoudreImprimante(imprimanteConfiguree As String, typeImprimante As String, owner As IWin32Window, moduleName As String, actionName As String) As String
            Dim installed As String() = PrinterSettings.InstalledPrinters.Cast(Of String)().ToArray()
            If installed.Length = 0 Then
                Throw New InvalidOperationException("Aucune imprimante Windows n'est installée sur ce poste.")
            End If

            If Not String.IsNullOrWhiteSpace(imprimanteConfiguree) AndAlso installed.Any(Function(p) String.Equals(p, imprimanteConfiguree, StringComparison.OrdinalIgnoreCase)) Then
                Return imprimanteConfiguree
            End If

            Dim log As New ProductionLogService()
            If String.IsNullOrWhiteSpace(imprimanteConfiguree) Then
                Dim messageNonConfiguree As String = "Aucune imprimante " & typeImprimante & " n'est configurée pour ce poste. Veuillez vérifier Paramètres > Périphériques."
                log.Warn(moduleName, actionName, messageNonConfiguree)
                Throw New InvalidOperationException(messageNonConfiguree)
            End If

            Dim message As String = "L'imprimante " & typeImprimante & " configurée n'est plus disponible. Veuillez vérifier Paramètres > Périphériques. Imprimante : " & imprimanteConfiguree
            log.Warn(moduleName, actionName, message)
            Throw New InvalidOperationException(message)
        End Function

        Private Shared Function ObtenirCheminConfigurationLocale() As String
            Dim dossierBase As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            If String.IsNullOrWhiteSpace(dossierBase) Then
                dossierBase = AppDomain.CurrentDomain.BaseDirectory
            End If
            Return Path.Combine(dossierBase, LocalFolderName, LocalPrinterFileName)
        End Function

        Private Shared Function NettoyerValeurLocale(valeur As String) As String
            If valeur Is Nothing Then
                Return String.Empty
            End If

            Return valeur.Replace(ChrW(13), " ").Replace(ChrW(10), " ").Trim()
        End Function
    End Class
End Namespace
