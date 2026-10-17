Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.Windows.Forms
Imports System.Globalization
Imports System.Linq
Imports DevCommerc8ak

Module Program
    Public Class Article
        Public Property ProduitId As Integer
        Public Property Libelle As String
        Public Property Quantite As Decimal
        Public Property Prix As Decimal
    End Class
    <STAThread>
    Sub Main()
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException)
        TesterCharteCommerciale()
        TesterChoixMode()
        TesterReglement()
        TesterChoixRole()
        Using fenetre As New Form(), grille As New DataGridView With {.Dock = DockStyle.Fill, .AllowUserToAddRows = False, .ReadOnly = True}
            fenetre.Controls.Add(grille) : fenetre.Show()
            Dim evenements As Integer = 0
            AddHandler grille.DataBindingComplete, Sub() evenements += 1
            AddHandler grille.DataError, Sub(sender, e) Throw New InvalidOperationException("Erreur de liaison", e.Exception)
            Dim panier As New List(Of Article)()
            For cycle As Integer = 1 To 40
                panier.Add(New Article With {.ProduitId = 1, .Libelle = "Sac / prix exceptionnel", .Quantite = 0.5D, .Prix = 12500D})
                panier.Add(New Article With {.ProduitId = 2, .Libelle = "Pièce", .Quantite = 1D, .Prix = 5000D})
                Relier(grille, panier)
                panier.RemoveAt(0)
                If grille.CurrentRow.DataBoundItem Is Nothing Then Throw New Exception("Selection perdue pendant suppression")
                Relier(grille, panier)
                panier.Clear()
                ' Chemin fautif de validation : le CurrencyManager reste sur 0,
                ' mais sa collection instantanee contient encore l'ancien article.
                If grille.CurrentRow.DataBoundItem Is Nothing Then Throw New Exception("CurrencyManager invalide apres Clear")
                Relier(grille, panier)
                If grille.Rows.Count <> 0 OrElse grille.CurrentRow IsNot Nothing Then Throw New Exception("Panier vide incorrect")
            Next
            If evenements < 80 Then Throw New Exception("Evenements de liaison non executes")
            Console.WriteLine("PASS Windows : 40 cycles suppression / selection / validation vide sans masquer DataError.")
        End Using
    End Sub
    Private Sub TesterCharteCommerciale()
        Using grille As New DataGridView With {.AllowUserToAddRows = False}, bouton As New Button With {.Bounds = New System.Drawing.Rectangle(12, 18, 150, 40), .Visible = False, .Enabled = False}
            Dim selection As System.Drawing.Color = System.Drawing.Color.FromArgb(225, 239, 250)
            grille.DefaultCellStyle.SelectionBackColor = selection
            grille.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Navy
            ThemeHelper1.StyliserEnteteCommerciale(grille)
            grille.DataSource = New List(Of Article) From {New Article With {.Libelle = "Article", .Quantite = 1D}}
            grille.ClearSelection() : grille.DataSource = Nothing
            grille.DataSource = New List(Of Article)()
            With grille.ColumnHeadersDefaultCellStyle
                If .BackColor <> System.Drawing.Color.FromArgb(245, 245, 245) OrElse .SelectionBackColor <> .BackColor Then Throw New Exception("Fond des entetes non conforme apres liaison")
                If .ForeColor <> System.Drawing.Color.FromArgb(52, 73, 94) OrElse .SelectionForeColor <> .ForeColor Then Throw New Exception("Texte des entetes non conforme")
            End With
            If grille.EnableHeadersVisualStyles OrElse grille.DefaultCellStyle.SelectionBackColor <> selection OrElse grille.DefaultCellStyle.SelectionForeColor <> System.Drawing.Color.Navy Then Throw New Exception("Selection des lignes modifiee par la charte")
            Dim bornes As System.Drawing.Rectangle = bouton.Bounds
            Dim clics As Integer = 0
            AddHandler bouton.Click, Sub() clics += 1
            ThemeHelper1.StyliserActionCommerciale(bouton, principale:=True)
            If bouton.Enabled OrElse bouton.Visible OrElse bouton.Bounds <> bornes Then Throw New Exception("Le style a modifie l'etat ou la position du bouton")
            If bouton.BackColor <> System.Drawing.Color.FromArgb(245, 245, 245) Then Throw New Exception("Etat desactive non distingue")
            bouton.Enabled = True
            If bouton.BackColor <> System.Drawing.Color.FromArgb(41, 128, 185) Then Throw New Exception("Action principale non restauree")
            bouton.Visible = True : bouton.PerformClick()
            If clics <> 1 Then Throw New Exception("Evenement Click perturbe par le style")
            Using secondaire As New Button(), destructif As New Button()
                ThemeHelper1.StyliserActionCommerciale(secondaire)
                ThemeHelper1.StyliserActionCommerciale(destructif, destructive:=True)
                If secondaire.BackColor <> System.Drawing.Color.White OrElse secondaire.ForeColor <> System.Drawing.Color.FromArgb(52, 73, 94) Then Throw New Exception("Action secondaire non neutre")
                If destructif.BackColor <> System.Drawing.Color.White OrElse destructif.ForeColor <> System.Drawing.Color.FromArgb(192, 57, 43) Then Throw New Exception("Action destructive non conforme")
            End Using
        End Using
        Console.WriteLine("PASS Windows : couleurs exactes, selection conservee, etats et Click preserves.")
    End Sub
    Private Sub TesterReglement()
        Dim culture As CultureInfo = CultureInfo.CurrentCulture
        Try
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR")
            Using fenetre As New Form With {.Size = New System.Drawing.Size(640, 700)}, reglement As New ReglementComptoirPanel(56000D, 2500D)
                fenetre.Controls.Add(reglement) : reglement.Dock = DockStyle.Fill : fenetre.Show() : Application.DoEvents()
                Dim controles As List(Of Control) = Descendants(reglement).ToList()
                Dim recu As TextBox = DirectCast(controles.Single(Function(c) c.Name = "MontantRecu"), TextBox)
                Dim etat As Label = DirectCast(controles.Single(Function(c) c.Name = "EtatReglement"), Label)
                Dim confirmer As Button = DirectCast(controles.Single(Function(c) c.Name = "ConfirmerReglement"), Button)
                recu.Focus()
                For Each texte As String In New String() {"56000", "60000", "55000", "", "abc", "60000,50"}
                    recu.Text = texte ' Aucun changement de focus ni Validate().
                    If (texte = "" OrElse texte = "abc" OrElse texte = "55000") AndAlso confirmer.Enabled Then Throw New Exception("Reglement invalide accepte")
                    If texte = "56000" AndAlso Not confirmer.Enabled Then Throw New Exception("Reglement exact refuse")
                    If texte = "60000" AndAlso (Not confirmer.Enabled OrElse Not etat.Text.Contains(4000D.ToString("N2"))) Then Throw New Exception("Monnaie non recalculee a la frappe")
                Next
                Dim devise As ComboBox = DirectCast(controles.Single(Function(c) c.Name = "DeviseRecue"), ComboBox)
                devise.SelectedItem = "USD" : recu.Text = "24"
                If Not confirmer.Enabled OrElse Not etat.Text.Contains(4000D.ToString("N2")) Then Throw New Exception("Conversion USD incorrecte")
                confirmer.PerformClick()
                If reglement.Reglement Is Nothing OrElse reglement.Reglement.MontantOrigine <> 24D OrElse reglement.Reglement.Taux <> 2500D Then Throw New Exception("Montant d'origine ou taux perdu")
            End Using
            Using reglement As New ReglementComptoirPanel(100D, 0D)
                Dim devise As ComboBox = DirectCast(Descendants(reglement).Single(Function(c) c.Name = "DeviseRecue"), ComboBox)
                If devise.Items.Contains("USD") Then Throw New Exception("Taux USD absent : option disponible a tort")
            End Using
        Finally
            CultureInfo.CurrentCulture = culture
        End Try
        Console.WriteLine("PASS Windows : frappe immediate, montant invalide/insuffisant et USD.")
    End Sub
    Private Sub TesterChoixRole()
        Dim roles As New List(Of RoleSessionInfo) From {New RoleSessionInfo With {.RoleId = 11, .NomRole = "FACTURIER", .EstRolePrincipal = True}, New RoleSessionInfo With {.RoleId = 22, .NomRole = "CAISSIÈRE"}}
        Using choix As New DialogueRoleSession(roles, New Dictionary(Of Integer, String)(), "Tatiana")
            choix.Show() : Application.DoEvents()
            Dim controles As List(Of Control) = Descendants(choix).ToList()
            DirectCast(controles.Single(Function(c) TypeOf c Is ListBox), ListBox).SelectedIndex = 1
            DirectCast(controles.Single(Function(c) TypeOf c Is Button AndAlso c.Text = "CONTINUER"), Button).PerformClick()
            If choix.RoleSelectionne.RoleId <> 22 Then Throw New Exception("Role choisi incorrect")
            choix.Close()
        End Using
    End Sub
    Private Sub TesterChoixMode()
        For Each activer As Boolean In New Boolean() {False, True}
            Using choix As New DialogueModeTravail(New List(Of String) From {"FACTURATION", "CAISSE", "FACTURATION_ET_CAISSE"}, "CAISSIÈRE")
                choix.Show() : Application.DoEvents()
                Dim caseCombinee As CheckBox = Nothing, continuer As Button = Nothing
                For Each controle As Control In Descendants(choix)
                    If TypeOf controle Is CheckBox Then caseCombinee = DirectCast(controle, CheckBox)
                    If TypeOf controle Is Button AndAlso controle.Text = "CONTINUER" Then continuer = DirectCast(controle, Button)
                Next
                If caseCombinee Is Nothing OrElse Not caseCombinee.Visible Then Throw New Exception("Tatiana/CAISSIERE : case combinee absente")
                caseCombinee.Checked = activer
                continuer.PerformClick()
                If choix.ModeSelectionne <> If(activer, "FACTURATION_ET_CAISSE", "CAISSE") Then Throw New Exception("Parcours incorrect selon la case")
                choix.Close()
            End Using
        Next
        Using choix As New DialogueModeTravail(New List(Of String) From {"FACTURATION"}, "FACTURIER")
            For Each controle As Control In Descendants(choix)
                If TypeOf controle Is CheckBox Then Throw New Exception("Tatiana/FACTURIER : aucune case combinee ne doit etre ajoutee")
            Next
        End Using
        Console.WriteLine("PASS Windows : choix exclusif caisse/combine et absence de case sans attribution.")
    End Sub
    Private Iterator Function Descendants(parent As Control) As IEnumerable(Of Control)
        For Each enfant As Control In parent.Controls
            Yield enfant
            For Each descendant As Control In Descendants(enfant)
                Yield descendant
            Next
        Next
    End Function
    Private Sub Relier(grille As DataGridView, panier As List(Of Article))
        grille.DataSource = Nothing
        grille.DataSource = DelegationPrixRegles.InstantaneArticles(panier)
        If grille.Columns.Contains("ProduitId") Then grille.Columns("ProduitId").Visible = False
        grille.CurrentCell = Nothing
        If grille.Rows.Count > 0 Then grille.CurrentCell = grille.Rows(0).Cells("Libelle")
        Application.DoEvents()
    End Sub
End Module
