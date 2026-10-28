Option Strict On
Option Explicit On
Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    ' Complement de lecture seulement. Aucun controle de schema ni ecriture.
    Public Class FactureImpressionRepository
        Private ReadOnly _dal As DAL
        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        ' Trois lectures groupees, sans NOLOCK ni ecriture. La transaction de
        ' lecture appartient au service et se termine avant tout dialogue/pilote.
        Public Function LireJustificatif(cn As SqlConnection, tx As SqlTransaction, factureId As Integer) As DataSet
            Dim result As New DataSet()
            Dim requetes As String() = {
                "SELECT f.NumeroFacture,f.CreeLe,f.Statut,f.MontantTotal,c.NomClient AS ClientNom,c.Telephone FROM dbo.FacturesVente f LEFT JOIN dbo.Clients c ON c.ClientId=f.ClientId WHERE f.FactureVenteId=@id",
                "SELECT p.PaiementId,p.Montant,p.Devise,p.DeviseMontants,p.MontantRecu,p.MonnaieRendue,p.ModePaiement,p.ReferencePaiement,p.PayeLe,p.PayePar,u.NomUtilisateur AS Caissier FROM dbo.Paiements p LEFT JOIN dbo.Utilisateurs u ON u.UtilisateurId=p.PayePar WHERE p.FactureVenteId=@id ORDER BY p.PaiementId",
                "SELECT l.ProduitId,pr.Libelle,l.TypeVente,ISNULL(l.QuantiteSaisie,l.Quantite) AS Quantite,l.PrixUnitaire,l.MontantLigne FROM dbo.LignesFactureVente l LEFT JOIN dbo.Produits pr ON pr.ProduitId=l.ProduitId WHERE l.FactureVenteId=@id ORDER BY l.LigneFactureVenteId"}
            Dim noms As String() = {"Entete", "Paiements", "Lignes"}
            For i As Integer = 0 To requetes.Length - 1
                Using cmd As New SqlCommand(requetes(i), cn, tx)
                    cmd.Parameters.Add("@id", SqlDbType.Int).Value = factureId
                    Using lecteur As SqlDataReader = cmd.ExecuteReader()
                        Dim table As New DataTable(noms(i))
                        table.Load(lecteur)
                        result.Tables.Add(table)
                    End Using
                End Using
            Next
            Return result
        End Function

        Public Function LireInstantaneConditionnements(cn As SqlConnection, tx As SqlTransaction, factureId As Integer) As String
            Using cmd As New SqlCommand("SELECT TOP(1) NouvellesValeurs FROM dbo.JournalAudit WHERE Entite='Facture' AND EntiteId=@id AND [Action] IN('FACTURE_CREEE','FACTURE_MODIFIEE') AND Resultat='SUCCES' AND Provenance='AUDIT_METIER' ORDER BY AuditId DESC", cn, tx)
                cmd.Parameters.Add("@id", SqlDbType.NVarChar, 30).Value = factureId.ToString(Globalization.CultureInfo.InvariantCulture)
                Return Convert.ToString(cmd.ExecuteScalar())
            End Using
        End Function

        Public Function LireReglementsEtAuteur(factureId As Integer) As DataTable
            If factureId <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(factureId))
            Return _dal.ExecuterTable("SELECT f.CreePar, auteur.NomUtilisateur AS Facturier, " &
                "p.PaiementId,p.Montant,p.DeviseMontants,p.ModePaiement,p.ReferencePaiement,p.PayeLe,p.PayePar,caissier.NomUtilisateur AS Caissier " &
                "FROM dbo.FacturesVente f LEFT JOIN dbo.Utilisateurs auteur ON auteur.UtilisateurId=f.CreePar " &
                "LEFT JOIN dbo.Paiements p ON p.FactureVenteId=f.FactureVenteId " &
                "LEFT JOIN dbo.Utilisateurs caissier ON caissier.UtilisateurId=p.PayePar " &
                "WHERE f.FactureVenteId=@id ORDER BY p.PaiementId", CommandType.Text,
                New List(Of SqlParameter) From {New SqlParameter("@id", factureId)})
        End Function
    End Class
End Namespace
