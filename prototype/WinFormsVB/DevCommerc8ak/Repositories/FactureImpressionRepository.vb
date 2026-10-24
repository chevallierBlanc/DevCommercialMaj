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
