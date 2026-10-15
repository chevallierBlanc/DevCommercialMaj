Option Strict On
Imports System
Imports System.Collections.Generic
Imports System.Windows.Forms
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
    Private Sub Relier(grille As DataGridView, panier As List(Of Article))
        grille.DataSource = Nothing
        grille.DataSource = DelegationPrixRegles.InstantaneArticles(panier)
        If grille.Columns.Contains("ProduitId") Then grille.Columns("ProduitId").Visible = False
        grille.CurrentCell = Nothing
        If grille.Rows.Count > 0 Then grille.CurrentCell = grille.Rows(0).Cells("Libelle")
        Application.DoEvents()
    End Sub
End Module
