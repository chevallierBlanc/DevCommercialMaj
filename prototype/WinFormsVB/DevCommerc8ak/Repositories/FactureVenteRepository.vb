Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class FactureVenteRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerColonnes()
            AssurerSequence()
        End Sub

        Private Sub AssurerColonnes()
            ' Schéma géré par le script SQL de déploiement.
        End Sub

        Private Sub AssurerSequence()
            ' Séquence fournie par le script SQL de déploiement.
        End Sub

        ' Genere un numero de facture du type FAC-AAAAMM-0001.
        Public Function GenererNumeroFacture() As String
            Dim anneeMois As String = Date.Now.ToString("yyyyMM")
            Dim sql As String = "" &
                "DECLARE @n INT; " &
                "BEGIN TRAN; " &
                "IF EXISTS (SELECT 1 FROM FactureSequence WITH (UPDLOCK, HOLDLOCK) WHERE AnneeMois = @AnneeMois) " &
                "BEGIN " &
                "UPDATE FactureSequence SET DernierNumero = DernierNumero + 1 WHERE AnneeMois = @AnneeMois; " &
                "SELECT @n = DernierNumero FROM FactureSequence WHERE AnneeMois = @AnneeMois; " &
                "END " &
                "ELSE " &
                "BEGIN " &
                "INSERT INTO FactureSequence (AnneeMois, DernierNumero) VALUES (@AnneeMois, 1); " &
                "SET @n = 1; " &
                "END " &
                "COMMIT; " &
                "SELECT @n;"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@AnneeMois", anneeMois)}
            Dim val As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Dim numero As Integer = Convert.ToInt32(val)
            Return "FAC-" & anneeMois & "-" & numero.ToString("0000")
        End Function

        ' Cree une facture de vente et retourne son identifiant.
        Public Function Ajouter(facture As FactureVente) As Integer
            Dim sql As String = "INSERT INTO FacturesVente (NumeroFacture, ClientId, SousTotal, MontantRemise, MontantTaxe, MontantTotal, Statut, CreePar, ModifierPar) " &
                                "VALUES (@NumeroFacture, @ClientId, @SousTotal, @MontantRemise, @MontantTaxe, @MontantTotal, @Statut, @CreePar, @ModifierPar); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"

            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@NumeroFacture", facture.NumeroFacture),
                New SqlParameter("@ClientId", If(facture.ClientId.HasValue, CType(facture.ClientId.Value, Object), DBNull.Value)),
                New SqlParameter("@SousTotal", facture.SousTotal),
                New SqlParameter("@MontantRemise", facture.MontantRemise),
                New SqlParameter("@MontantTaxe", facture.MontantTaxe),
                New SqlParameter("@MontantTotal", facture.MontantTotal),
                New SqlParameter("@Statut", facture.Statut),
                New SqlParameter("@CreePar", facture.CreePar),
                New SqlParameter("@ModifierPar", SessionUtilisateur.NomUtilisateur)
            }

            Dim id As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(id)
        End Function

        ' Retourne la liste des factures.
        Public Function Lister() As List(Of FactureVenteDTO)
            Dim sql As String = "SELECT FactureVenteId, NumeroFacture, ClientId, MontantTotal, Statut, CreeLe, ValideLe FROM FacturesVente"
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, Nothing)
            Dim liste As New List(Of FactureVenteDTO)()

            For Each row As DataRow In dt.Rows
                liste.Add(MapVersDTO(row))
            Next

            Return liste
        End Function

        ' Retourne les factures en attente.
        Public Function ListerEnAttente() As List(Of FactureVenteDTO)
            Dim sql As String = "SELECT FactureVenteId, NumeroFacture, ClientId, MontantTotal, Statut, CreeLe, ValideLe " & _
                                "FROM FacturesVente WHERE Statut='EN_ATTENTE'"
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, Nothing)
            Dim liste As New List(Of FactureVenteDTO)()
            For Each row As DataRow In dt.Rows
                liste.Add(MapVersDTO(row))
            Next
            Return liste
        End Function

        ' Retourne une facture par identifiant.
        Public Function ObtenirParId(factureVenteId As Integer) As FactureVenteDTO
            Dim sql As String = "SELECT FactureVenteId, NumeroFacture, ClientId, MontantTotal, Statut, CreeLe, ValideLe FROM FacturesVente WHERE FactureVenteId = @FactureVenteId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@FactureVenteId", factureVenteId)}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            If dt.Rows.Count = 0 Then
                Return Nothing
            End If
            Return MapVersDTO(dt.Rows(0))
        End Function

        ' Met a jour une facture.
        Public Function MettreAJour(facture As FactureVente) As Integer
            Dim sql As String = "UPDATE FacturesVente SET NumeroFacture=@NumeroFacture, ClientId=@ClientId, SousTotal=@SousTotal, " &
                                "MontantRemise=@MontantRemise, MontantTaxe=@MontantTaxe, MontantTotal=@MontantTotal, Statut=@Statut, ModifierPar=@ModifierPar " &
                                "WHERE FactureVenteId=@FactureVenteId"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@NumeroFacture", facture.NumeroFacture),
                New SqlParameter("@ClientId", If(facture.ClientId.HasValue, CType(facture.ClientId.Value, Object), DBNull.Value)),
                New SqlParameter("@SousTotal", facture.SousTotal),
                New SqlParameter("@MontantRemise", facture.MontantRemise),
                New SqlParameter("@MontantTaxe", facture.MontantTaxe),
                New SqlParameter("@MontantTotal", facture.MontantTotal),
                New SqlParameter("@Statut", facture.Statut),
                New SqlParameter("@FactureVenteId", facture.FactureVenteId),
                New SqlParameter("@ModifierPar", SessionUtilisateur.NomUtilisateur)
            }

            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function

        ' Met a jour le statut d'une facture.
        Public Function MettreAJourStatut(factureVenteId As Integer, statut As String) As Integer
            Dim sql As String = "UPDATE FacturesVente SET Statut=@Statut, ModifierPar=@ModifierPar WHERE FactureVenteId=@FactureVenteId"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@Statut", statut),
                New SqlParameter("@FactureVenteId", factureVenteId),
                New SqlParameter("@ModifierPar", SessionUtilisateur.NomUtilisateur)
            }
            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function

        ' Liste des factures avec filtres pour l'historique.
        Public Function ListerHistorique(numeroFacture As String, nomClient As String, telephone As String, dateDu As Date?, dateAu As Date?, statutDb As String) As DataTable
            Dim sql As String = "" &
                "SELECT f.FactureVenteId, f.NumeroFacture, ISNULL(c.NomClient,'') AS ClientNom, ISNULL(c.Telephone,'') AS Telephone, " &
                "f.CreeLe, f.MontantTotal, f.Statut " &
                "FROM FacturesVente f LEFT JOIN Clients c ON c.ClientId = f.ClientId WHERE 1=1 "

            Dim p As New List(Of SqlParameter)()

            If numeroFacture <> "" Then
                sql &= "AND f.NumeroFacture LIKE @NumeroFacture "
                p.Add(New SqlParameter("@NumeroFacture", "%" & numeroFacture & "%"))
            End If
            If nomClient <> "" Then
                sql &= "AND c.NomClient LIKE @NomClient "
                p.Add(New SqlParameter("@NomClient", "%" & nomClient & "%"))
            End If
            If telephone <> "" Then
                sql &= "AND c.Telephone LIKE @Telephone "
                p.Add(New SqlParameter("@Telephone", "%" & telephone & "%"))
            End If
            If dateDu.HasValue Then
                sql &= "AND CAST(f.CreeLe AS DATE) >= @DateDu "
                p.Add(New SqlParameter("@DateDu", dateDu.Value))
            End If
            If dateAu.HasValue Then
                sql &= "AND CAST(f.CreeLe AS DATE) <= @DateAu "
                p.Add(New SqlParameter("@DateAu", dateAu.Value))
            End If
            If statutDb <> "" Then
                sql &= "AND f.Statut = @Statut "
                p.Add(New SqlParameter("@Statut", statutDb))
            End If

            sql &= "ORDER BY f.CreeLe DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        ' Liste des factures validees non payees (statut EN_ATTENTE) pour la caisse.
        Public Function ListerValideesNonPayees(recherche As String, dateDu As Date?, dateAu As Date?) As DataTable
            Dim sql As String = "" &
                "SELECT f.FactureVenteId, f.NumeroFacture, ISNULL(c.NomClient,'') AS ClientNom, ISNULL(c.Telephone,'') AS Telephone, " &
                "f.CreeLe, f.MontantTotal, f.Statut " &
                "FROM FacturesVente f LEFT JOIN Clients c ON c.ClientId = f.ClientId WHERE f.Statut = 'EN_ATTENTE' "

            Dim p As New List(Of SqlParameter)()

            If recherche <> "" Then
                sql &= "AND (f.NumeroFacture LIKE @q OR c.NomClient LIKE @q OR c.Telephone LIKE @q) "
                p.Add(New SqlParameter("@q", "%" & recherche & "%"))
            End If
            If dateDu.HasValue Then
                sql &= "AND CAST(f.CreeLe AS DATE) >= @DateDu "
                p.Add(New SqlParameter("@DateDu", dateDu.Value))
            End If
            If dateAu.HasValue Then
                sql &= "AND CAST(f.CreeLe AS DATE) <= @DateAu "
                p.Add(New SqlParameter("@DateAu", dateAu.Value))
            End If

            sql &= "ORDER BY f.CreeLe DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        ' Supprime une facture.
        Public Function Supprimer(factureVenteId As Integer) As Integer
            Dim sql As String = "DELETE FROM FacturesVente WHERE FactureVenteId = @FactureVenteId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@FactureVenteId", factureVenteId)}
            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function

        Private Function MapVersDTO(row As DataRow) As FactureVenteDTO
            Return New FactureVenteDTO With {
                .FactureVenteId = Convert.ToInt32(row("FactureVenteId")),
                .NumeroFacture = Convert.ToString(row("NumeroFacture")),
                .ClientId = If(row.IsNull("ClientId"), CType(Nothing, Integer?), Convert.ToInt32(row("ClientId"))),
                .MontantTotal = Convert.ToDecimal(row("MontantTotal")),
                .Statut = Convert.ToString(row("Statut")),
                .CreeLe = Convert.ToDateTime(row("CreeLe")),
                .ValideLe = If(row.IsNull("ValideLe"), CType(Nothing, Date?), Convert.ToDateTime(row("ValideLe")))
            }
        End Function
    End Class
End Namespace
