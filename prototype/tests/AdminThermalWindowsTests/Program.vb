Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Reflection
Imports System.Web.Script.Serialization
Imports DevCommerc8ak.DevCommerc8ak

Module Program
    Private nombre As Integer
    Private Sub Verifier(ok As Boolean)
        If Not ok Then Throw New Exception("Echec du controle " & nombre.ToString())
        nombre += 1
    End Sub
    Private Function Blocs(data As FacturePrintData) As IList
        Dim methode As MethodInfo = GetType(FacturePrintRenderer).GetMethod("ConstruireBlocsThermiques", BindingFlags.NonPublic Or BindingFlags.Static)
        Return DirectCast(methode.Invoke(Nothing, New Object() {data, New ParametreDTO With {.NomMagasin = "MAGASIN TEST"}}), IList)
    End Function
    Private Function Champ(bloc As Object, nom As String) As String
        Return Convert.ToString(bloc.GetType().GetProperty(nom).GetValue(bloc, Nothing))
    End Function
    <STAThread>
    Sub Main()
        If Environment.OSVersion.Platform <> PlatformID.Win32NT Then Throw New PlatformNotSupportedException("Tests GDI reserves a Windows ; aucune simulation d'impression.")
        Dim data As New FacturePrintData With {.Numero = "FAC-HIST-TEST", .DateDocument = New Date(2020, 1, 1), .Statut = "EN_ATTENTE", .ProformaThermique = True, .SousTotal = 500, .Remise = 10, .Total = 490,
            .Lignes = New List(Of FacturePrintLine) From {New FacturePrintLine With {.Produit = New String("X"c, 300), .Conditionnement = "Conditionnement personnalise tres long", .Quantite = 2.5D, .PrixUnitaire = 123D, .Montant = 500}}}
        data.ReglementsThermiques.Add(New KeyValuePair(Of String, String)("Paiement interdit sur proforma", "FICTIF"))
        Dim liste As IList = Blocs(data)
        Dim totalVu As Boolean = False, proforma As Boolean = False
        For Each b As Object In liste
            Verifier(Champ(b, "Valeur") <> "FICTIF")
            If Champ(b, "Texte") = "TOTAL" Then totalVu = Champ(b, "Valeur").Contains("490")
            If Champ(b, "Texte") = "PROFORMA" Then proforma = True
        Next
        Verifier(totalVu AndAlso proforma)
        data.ProformaThermique = False
        data.ReglementsThermiques.Clear()
        data.Statut = "ANNULEE"
        data.TitreThermique = "TICKET DE CAISSE"
        data.Lignes(0).Montant = 12345678901234567890D
        liste = Blocs(data)
        Dim annulee As Boolean = False
        For Each b As Object In liste
            If Champ(b, "Texte") = "FACTURE ANNULÉE" Then annulee = True
        Next
        Verifier(annulee)
        Dim dessiner As MethodInfo = GetType(FacturePrintRenderer).GetMethod("TraiterBlocThermique", BindingFlags.NonPublic Or BindingFlags.Static)
        Using bitmap As New Bitmap(315, 5000)
            bitmap.SetResolution(100, 100)
            Using g As Graphics = Graphics.FromImage(bitmap)
                For Each b As Object In liste
                    Dim mesure As Single = CSng(dessiner.Invoke(Nothing, New Object() {g, b, 0.0F, 0.0F, 280.0F, False}))
                    Dim rendu As Single = CSng(dessiner.Invoke(Nothing, New Object() {g, b, 0.0F, 0.0F, 280.0F, True}))
                    Verifier(mesure > 0 AndAlso mesure = rendu)
                Next
            End Using
        End Using
        Verifier(data.Lignes(0).PrixUnitaire = 123D AndAlso data.Total = 490D AndAlso data.Lignes(0).Montant = 12345678901234567890D)
        data.Statut = "PAYEE"
        data.ReglementsThermiques.Add(New KeyValuePair(Of String, String)("Montant payé", "490 FC"))
        liste = Blocs(data)
        Dim paiementVu As Boolean = False
        For Each b As Object In liste
            If Champ(b, "Texte") = "Montant payé" AndAlso Champ(b, "Valeur") = "490 FC" Then paiementVu = True
        Next
        Verifier(paiementVu)
        ' Le jeu de diagnostic et le plan sont testes sans configuration SQL/imprimante.
        Dim diagnostic As FacturePrintData = PrintConfigurationHelper.CreerDonneesTestThermique()
        Verifier(diagnostic.DiagnosticThermique AndAlso Not diagnostic.ProformaThermique)
        Verifier(diagnostic.Lignes.Count = 3 AndAlso diagnostic.Lignes.Sum(Function(l) l.Montant) = diagnostic.SousTotal)
        Verifier(diagnostic.SousTotal - diagnostic.Remise = diagnostic.Total)
        Verifier(diagnostic.Lignes.All(Function(l) l.Quantite * l.PrixUnitaire = l.Montant))
        Verifier(String.IsNullOrEmpty(diagnostic.Client) AndAlso String.IsNullOrEmpty(diagnostic.Facturier) AndAlso String.IsNullOrEmpty(diagnostic.Statut))
        diagnostic.ReglementsThermiques.Add(New KeyValuePair(Of String, String)("Paiement interdit dans un test", "FICTIF"))
        liste = Blocs(diagnostic)
        Dim titreTest As Boolean = False, mentionTest As Boolean = False, referenceTest As Boolean = False
        For Each b As Object In liste
            Verifier(Champ(b, "Valeur") <> "FICTIF")
            Verifier(Champ(b, "Texte") <> "Facture")
            If Champ(b, "Texte") = diagnostic.TitreThermique Then titreTest = True
            If Champ(b, "Texte") = diagnostic.MentionThermique Then mentionTest = True
            If Champ(b, "Texte") = "Diagnostic" AndAlso Champ(b, "Valeur") = "TEST-80MM" Then referenceTest = True
        Next
        Verifier(titreTest AndAlso mentionTest AndAlso referenceTest)
        Using bitmap As New Bitmap(315, 5000)
            bitmap.SetResolution(100, 100)
            Using g As Graphics = Graphics.FromImage(bitmap)
                For Each largeur As Single In New Single() {170.0F, 240.0F, 280.0F}
                    For Each b As Object In liste
                        Dim mesure As Single = CSng(dessiner.Invoke(Nothing, New Object() {g, b, 0.0F, 0.0F, largeur, False}))
                        Dim rendu As Single = CSng(dessiner.Invoke(Nothing, New Object() {g, b, 0.0F, 0.0F, largeur, True}))
                        Verifier(mesure > 0 AndAlso mesure = rendu)
                    Next
                Next
            End Using
        End Using
        For Each statut As String In New String() {"PAYEE", "EN_ATTENTE", "PARTIELLEMENT PAYÉE", "IMPAYÉE"}
            data.Statut = statut
            Dim statutVu As Boolean = False
            For Each b As Object In Blocs(data)
                If Champ(b, "Texte") = "STATUT" Then
                    statutVu = Champ(b, "Valeur") = FacturePrintRenderer.StatutAffichage(statut) AndAlso Champ(b, "Police") = "1"
                End If
            Next
            Verifier(statutVu)
        Next
        data.ProformaThermique = True
        data.Lignes(0).Conditionnement = "DEMI"
        data.Lignes(0).UnitePrincipaleImpression = "sac"
        data.Lignes(0).Quantite = 1D
        Dim demiSac As Boolean = False, mention As Boolean = False
        For Each b As Object In Blocs(data)
            Verifier(Champ(b, "Texte") <> "STATUT" AndAlso Champ(b, "Texte") <> "Montant payé")
            If Champ(b, "Genre") = "ARTICLE" AndAlso Champ(b, "Detail").StartsWith("1 demi-sac") Then demiSac = True
            If Champ(b, "Texte") = "DOCUMENT PROFORMA — NON ACQUITTÉ" Then mention = True
        Next
        Verifier(demiSac AndAlso mention)
        ' Ancienne ligne sans instantane : aucune unite du catalogue injectee.
        data.Lignes(0).UnitePrincipaleImpression = Nothing
        Dim repli As Boolean = False
        For Each b As Object In Blocs(data)
            If Champ(b, "Genre") = "ARTICLE" AndAlso Champ(b, "Detail").StartsWith("1 DEMI") Then repli = True
        Next
        Verifier(repli)
        Dim historique As New List(Of FacturePrintLine) From {New FacturePrintLine With {.ProduitId = 7, .Conditionnement = "GROS", .Quantite = 1D, .PrixUnitaire = 10D, .Montant = 10D}}
        Dim snapshot As String = New JavaScriptSerializer().Serialize(New With {.Lignes = New Object() {New With {.ProduitId = 7, .TypeVente = "GROS", .Quantite = 1D, .QuantiteSaisie = 1D, .PrixUnitaire = 10D, .MontantLigne = 10D, .TypeTarifId = 0, .UnitePrincipaleImpression = "carton", .UniteSecondaireImpression = "pièce"}}})
        ConditionnementsHistoriquesService.Appliquer(snapshot, historique)
        Verifier(historique(0).UnitePrincipaleImpression = "carton")
        historique(0).PrixUnitaire = 11D
        ConditionnementsHistoriquesService.Appliquer(snapshot, historique)
        Verifier(historique(0).UnitePrincipaleImpression Is Nothing)
        historique(0).PrixUnitaire = 10D
        historique(0).ProduitId = 8
        ConditionnementsHistoriquesService.Appliquer(snapshot, historique)
        Verifier(historique(0).UnitePrincipaleImpression Is Nothing)
        historique(0).ProduitId = 7
        For Each ancien As String In New String() {"", "{}", "{invalid", "{""Lignes"":[]}", "{""Lignes"":[{""ProduitId"":7}]}"}
            ConditionnementsHistoriquesService.Appliquer(ancien, historique)
            Verifier(historique(0).UnitePrincipaleImpression Is Nothing)
        Next
        IntegrationCaisse.ExecuterSiConfigure()
        Console.WriteLine("PASS : " & nombre.ToString() & " controles du vrai plan thermique/GDI. Aucune impression physique.")
    End Sub
End Module
