Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class PaiementRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerColonnes()
        End Sub

        Private Sub AssurerColonnes()
            ' Schéma géré par le script SQL de déploiement.
        End Sub

        ' Cree un paiement et retourne son identifiant.
        Public Function Ajouter(paiement As Paiement) As Integer
            Dim sql As String = "INSERT INTO Paiements (FactureVenteId, ModePaiement, ReferencePaiement, Montant, MontantRecu, MonnaieRendue, Devise, PayePar, ModifierPar) " &
                                "VALUES (@FactureVenteId, @ModePaiement, @ReferencePaiement, @Montant, @MontantRecu, @MonnaieRendue, @Devise, @PayePar, @ModifierPar); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"

            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@FactureVenteId", paiement.FactureVenteId),
                New SqlParameter("@ModePaiement", paiement.ModePaiement),
                New SqlParameter("@ReferencePaiement", If(paiement.ReferencePaiement, CType(DBNull.Value, Object))),
                New SqlParameter("@Montant", paiement.Montant),
                New SqlParameter("@MontantRecu", paiement.MontantRecu),
                New SqlParameter("@MonnaieRendue", paiement.MonnaieRendue),
                New SqlParameter("@Devise", If(paiement.Devise, CType(DBNull.Value, Object))),
                New SqlParameter("@PayePar", paiement.PayePar),
                New SqlParameter("@ModifierPar", SessionUtilisateur.NomUtilisateur)
            }

            Dim id As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(id)
        End Function

        ' Liste des paiements par facture.
        Public Function ListerParFacture(factureVenteId As Integer) As List(Of PaiementDTO)
            Dim sql As String = "SELECT PaiementId, FactureVenteId, ModePaiement, Montant, MontantRecu, MonnaieRendue, Devise, PayeLe " &
                                "FROM Paiements WHERE FactureVenteId = @FactureVenteId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@FactureVenteId", factureVenteId)}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            Dim liste As New List(Of PaiementDTO)()

            For Each row As DataRow In dt.Rows
                liste.Add(New PaiementDTO With {
                    .PaiementId = Convert.ToInt32(row("PaiementId")),
                    .FactureVenteId = Convert.ToInt32(row("FactureVenteId")),
                    .ModePaiement = Convert.ToString(row("ModePaiement")),
                    .Montant = Convert.ToDecimal(row("Montant")),
                    .MontantRecu = Convert.ToDecimal(row("MontantRecu")),
                    .MonnaieRendue = Convert.ToDecimal(row("MonnaieRendue")),
                    .Devise = If(row.IsNull("Devise"), Nothing, Convert.ToString(row("Devise"))),
                    .PayeLe = Convert.ToDateTime(row("PayeLe"))
                })
            Next

            Return liste
        End Function

        ' Supprime un paiement.
        Public Function Supprimer(paiementId As Integer) As Integer
            Dim sql As String = "DELETE FROM Paiements WHERE PaiementId = @PaiementId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@PaiementId", paiementId)}
            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function
    End Class
End Namespace
