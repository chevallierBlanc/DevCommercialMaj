Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic
Imports System.Globalization

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
                Try
                    liste.Add(New PaiementDTO With {
                        .PaiementId = SafeInteger(row("PaiementId")),
                        .FactureVenteId = SafeInteger(row("FactureVenteId")),
                        .ModePaiement = SafeString(row("ModePaiement")),
                        .Montant = SafeDecimal(row("Montant")),
                        .MontantRecu = SafeDecimal(row("MontantRecu")),
                        .MonnaieRendue = SafeDecimal(row("MonnaieRendue")),
                        .Devise = SafeNullableString(row("Devise")),
                        .PayeLe = SafeDate(row("PayeLe"))
                    })
                Catch ex As Exception
                    Dim log As New ProductionLogService()
                    log.Warn("PaiementRepository", "ListerParFacture", "Ligne paiement ignorée car invalide : " & ex.Message)
                End Try
            Next

            Return liste
        End Function

        ' Supprime un paiement.
        Public Function Supprimer(paiementId As Integer) As Integer
            Dim sql As String = "DELETE FROM Paiements WHERE PaiementId = @PaiementId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@PaiementId", paiementId)}
            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function

        Private Function SafeInteger(value As Object) As Integer
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return 0
            End If

            Dim resultat As Integer
            If Integer.TryParse(Convert.ToString(value), resultat) Then
                Return resultat
            End If
            Return 0
        End Function

        Private Function SafeDecimal(value As Object) As Decimal
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return 0D
            End If

            If TypeOf value Is Decimal Then
                Return CType(value, Decimal)
            End If

            Dim texte As String = Convert.ToString(value).Trim()
            If texte = String.Empty Then
                Return 0D
            End If

            Dim invariant As String = texte.Replace(",", ".")
            Dim resultat As Decimal
            If Decimal.TryParse(invariant, NumberStyles.Any, CultureInfo.InvariantCulture, resultat) Then
                Return resultat
            End If
            If Decimal.TryParse(texte, NumberStyles.Any, CultureInfo.CurrentCulture, resultat) Then
                Return resultat
            End If
            Return 0D
        End Function

        Private Function SafeDate(value As Object) As Date
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return Date.MinValue
            End If

            If TypeOf value Is Date Then
                Return CType(value, Date)
            End If

            Dim resultat As Date
            If Date.TryParse(Convert.ToString(value), resultat) Then
                Return resultat
            End If
            Return Date.MinValue
        End Function

        Private Function SafeString(value As Object) As String
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return String.Empty
            End If
            Return Convert.ToString(value)
        End Function

        Private Function SafeNullableString(value As Object) As String
            Dim texte As String = SafeString(value)
            If String.IsNullOrWhiteSpace(texte) Then
                Return Nothing
            End If
            Return texte
        End Function
    End Class
End Namespace
