Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public NotInheritable Class EncaissementAuditRegles
        Private Sub New()
        End Sub

        Public Shared Sub Verifier(total As Decimal, dejaPaye As Decimal, recu As Decimal, rendu As Decimal)
            ' Ce flux existant solde une facture en une fois. Un paiement partiel
            ' preexistant ne doit pas devenir un deuxieme encaissement integral.
            If total <= 0D OrElse dejaPaye <> 0D Then Throw New InvalidOperationException("Facture invalide ou paiement déjà enregistré.")
            If recu < total OrElse rendu < 0D OrElse recu - rendu <> total Then
                Throw New ArgumentException("Le montant reçu moins la monnaie rendue doit correspondre au montant dû.")
            End If
            If Decimal.Round(recu, 2) <> recu OrElse Decimal.Round(rendu, 2) <> rendu Then Throw New ArgumentException("Les montants normalisés doivent respecter les deux décimales de la table Paiements.")
        End Sub

        Public Shared Function Snapshot(total As Decimal, dejaPaye As Decimal, affecte As Decimal,
                                       recu As Decimal?, rendu As Decimal?, mode As String, reference As String) As Dictionary(Of String, Object)
            ' Les montants normalises ont tous la meme devise. Une information
            ' historique absente reste inconnue, jamais reconstruite arbitrairement.
            Return New Dictionary(Of String, Object) From {
                {"TotalFacture", total}, {"DejaPayeAvant", dejaPaye}, {"ResteAvant", total - dejaPaye},
                {"MontantRecu", If(recu.HasValue, CType(recu.Value, Object), Nothing)},
                {"MonnaieRendue", If(rendu.HasValue, CType(rendu.Value, Object), Nothing)},
                {"MontantAffecte", affecte}, {"ResteApres", total - dejaPaye - affecte},
                {"DeviseMontants", "FC"}, {"ModePaiement", mode}, {"ReferencePaiement", reference}}
        End Function
    End Class
End Namespace
