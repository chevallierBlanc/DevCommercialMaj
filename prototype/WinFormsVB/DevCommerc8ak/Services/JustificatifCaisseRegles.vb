Option Strict On
Option Explicit On
Imports System
Imports System.Data

Namespace DevCommerc8ak
    Public NotInheritable Class JustificatifCaisseRegles
        Private Sub New()
        End Sub

        Public Shared Sub Verifier(statut As String, total As Decimal, paiements As DataTable)
            ' Le parcours Caisse existant solde integralement une facture.
            ' Ne pas inventer ici un nouveau justificatif de paiement partiel.
            If statut <> "PAYEE" OrElse total <= 0D OrElse paiements Is Nothing OrElse paiements.Rows.Count = 0 Then
                Throw New InvalidOperationException("Ticket de caisse interdit : aucun encaissement intégral confirmé pour cette facture.")
            End If
            Dim affecte As Decimal = 0D
            For Each paiement As DataRow In paiements.Rows
                Dim devise As String = Convert.ToString(paiement("DeviseMontants")).Trim()
                ' Les anciens paiements en FC restent lisibles sans inventer un
                ' taux pour les paiements historiques en devise non documentee.
                If devise = "" AndAlso Convert.ToString(paiement("Devise")) = "FC" Then devise = "FC"
                If devise <> "FC" OrElse paiement.IsNull("Montant") OrElse CDec(paiement("Montant")) <= 0D OrElse paiement.IsNull("PayeLe") Then
                    Throw New InvalidOperationException("Justificatif indisponible : les montants ou la devise du règlement historique ne sont pas suffisamment documentés. Consultez l'historique des factures.")
                End If
                affecte += CDec(paiement("Montant"))
            Next
            If affecte <> total Then Throw New InvalidOperationException("Ticket acquitté interdit : les règlements enregistrés ne soldent pas exactement la facture. Consultez l'historique.")
        End Sub
    End Class
End Namespace
