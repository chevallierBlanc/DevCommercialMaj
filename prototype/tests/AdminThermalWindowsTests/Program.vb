Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Reflection
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
        Console.WriteLine("PASS : " & nombre.ToString() & " controles du vrai plan thermique/GDI. Aucune connexion SQL ni impression physique.")
    End Sub
End Module
