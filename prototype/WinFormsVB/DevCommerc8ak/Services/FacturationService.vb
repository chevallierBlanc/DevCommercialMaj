Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic
Imports System.Linq

Namespace DevCommerc8ak
    Public Class FacturationService
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        ' Cree une facture en attente.
        Public Function CreerFacture(numeroFacture As String, clientId As Integer?, sousTotal As Decimal, montantRemise As Decimal, montantTaxe As Decimal, montantTotal As Decimal, creePar As Integer) As Integer
            Dim repo As New FactureVenteRepository(_dal)
            Dim f As New FactureVente With {
                .NumeroFacture = numeroFacture,
                .ClientId = clientId,
                .SousTotal = sousTotal,
                .MontantRemise = montantRemise,
                .MontantTaxe = montantTaxe,
                .MontantTotal = montantTotal,
                .Statut = "EN_ATTENTE",
                .CreePar = creePar
            }
            Dim factureId As Integer
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "FACTURE_CREER", "FACTURIER")
                    If creePar <> SessionUtilisateur.UtilisateurId Then Throw New UnauthorizedAccessException("Auteur de facture invalide.")
                    factureId = repo.Ajouter(f, cn, tx)
                    AuditMetierService.Enregistrer(cn, tx, "FACTURE_CREEE", "FACTURATION", "Facture", factureId, numeroFacture,
                        Nothing, FactureOperationService.Snapshot(cn, tx, factureId), String.Empty, Guid.NewGuid())
                    tx.Commit()
                End Using
            End Using
            AppEvents.OnVenteCreee()
            AppEvents.OnDataChanged()
            Return factureId
        End Function

        ' Ajoute une ligne a une facture.
        Public Function AjouterLigne(factureVenteId As Integer, produitId As Integer, quantite As Decimal, QuantiteBase As Decimal, TypeVente As String, prixUnitaire As Decimal, montantRemise As Decimal, Optional quantiteFacturee As Decimal? = Nothing, Optional motif As String = Nothing) As Integer
            Dim repo As New LigneFactureVenteRepository(_dal)
            Dim quantiteMontant As Decimal = If(quantiteFacturee.HasValue, quantiteFacturee.Value, quantite)
            Dim montantLigne As Decimal = (quantiteMontant * prixUnitaire) - montantRemise
            Dim coutUnitaireBase As Decimal? = ObtenirCoutUnitaireBaseVente(produitId)
            Dim ligne As New LigneFactureVente With {
                .FactureVenteId = factureVenteId,
                .ProduitId = produitId,
                .Quantite = quantite,
                .QuantiteBase = QuantiteBase,
                .TypeVente = TypeVente,
                .PrixUnitaire = prixUnitaire,
                .MontantRemise = montantRemise,
                .MontantLigne = montantLigne,
                .QteSaisie = quantiteFacturee,
                .CoutUnitaireBaseVente = coutUnitaireBase
            }
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "FACTURE_MODIFIER", "FACTURIER")
                    motif = AuditMetierService.ValiderMotif(motif)
                    Using cmd As New SqlCommand("SELECT Statut FROM dbo.FacturesVente WITH (UPDLOCK,HOLDLOCK) WHERE FactureVenteId=@id", cn, tx)
                        cmd.Parameters.AddWithValue("@id", factureVenteId)
                        If Convert.ToString(cmd.ExecuteScalar()) <> "EN_ATTENTE" Then Throw New InvalidOperationException("Seul un brouillon peut recevoir une ligne.")
                    End Using
                    Dim avant As Dictionary(Of String, Object) = FactureOperationService.Snapshot(cn, tx, factureVenteId)
                    Dim id As Integer = repo.Ajouter(ligne, cn, tx)
                    ' L'ajout isolé invalide aussi la version ouverte sur un autre poste.
                    Using cmd As New SqlCommand("UPDATE dbo.FacturesVente SET ModifierPar=@user WHERE FactureVenteId=@id", cn, tx)
                        cmd.Parameters.AddWithValue("@id", factureVenteId)
                        cmd.Parameters.AddWithValue("@user", SessionUtilisateur.NomUtilisateur)
                        cmd.ExecuteNonQuery()
                    End Using
                    AuditMetierService.Enregistrer(cn, tx, "FACTURE_MODIFIEE", "FACTURATION", "Facture", factureVenteId,
                        Convert.ToString(DirectCast(avant("Entete"), Dictionary(Of String, Object))("NumeroFacture")),
                        avant, FactureOperationService.Snapshot(cn, tx, factureVenteId), motif, Guid.NewGuid())
                    tx.Commit()
                    Return id
                End Using
            End Using
        End Function

        Friend Function ObtenirCoutUnitaireBaseVente(produitId As Integer, Optional cn As SqlConnection = Nothing, Optional tx As SqlTransaction = Nothing) As Decimal?
            If produitId <= 0 Then
                Return Nothing
            End If

            Dim sql As String = "SELECT PrixAchat, ConversionUnite, ISNULL(TypeGestionStock,'UNITE') AS TypeGestionStock, ISNULL(ContenuUnitePrincipale, ISNULL(ConversionUnite,1)) AS ContenuUnitePrincipale FROM Produits WHERE ProduitId=@ProduitId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@ProduitId", produitId)}
            Dim dt As DataTable
            If cn Is Nothing Then
                dt = _dal.ExecuterTable(sql, CommandType.Text, p)
            Else
                dt = New DataTable()
                Using cmd As New SqlCommand(sql, cn, tx)
                    cmd.Parameters.AddRange(p.ToArray())
                    Using r As SqlDataReader = cmd.ExecuteReader()
                        dt.Load(r)
                    End Using
                End Using
            End If
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                Return Nothing
            End If

            Dim row As DataRow = dt.Rows(0)
            Dim prixAchat As Decimal = If(row.IsNull("PrixAchat"), 0D, Convert.ToDecimal(row("PrixAchat")))
            Dim conversion As Decimal = If(row.IsNull("ConversionUnite"), 0D, Convert.ToDecimal(row("ConversionUnite")))
            Dim typeGestion As String = If(row.IsNull("TypeGestionStock"), "UNITE", Convert.ToString(row("TypeGestionStock")))
            Dim contenuPrincipal As Decimal = If(row.IsNull("ContenuUnitePrincipale"), conversion, Convert.ToDecimal(row("ContenuUnitePrincipale")))
            Return CalculVenteService.CalculerCoutUnitaireBase(prixAchat, conversion, typeGestion, contenuPrincipal)
        End Function

        ' Valide le paiement d'une facture.
        Public Function ValiderPaiement(factureVenteId As Integer, modePaiement As String, referencePaiement As String, montant As Decimal, payePar As Integer) As Integer
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@FactureVenteId", factureVenteId),
                New SqlParameter("@ModePaiement", modePaiement),
                New SqlParameter("@ReferencePaiement", If(referencePaiement, CType(DBNull.Value, Object))),
                New SqlParameter("@Montant", montant),
                New SqlParameter("@PayePar", payePar)
            }

            Dim resultat As Integer
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "ENCAISSEMENT_CREER", "CAISSE")
                    If payePar <> SessionUtilisateur.UtilisateurId Then Throw New UnauthorizedAccessException("Auteur de paiement invalide.")
                    Using lockCmd As New SqlCommand("SELECT Statut FROM dbo.FacturesVente WITH (UPDLOCK,HOLDLOCK) WHERE FactureVenteId=@id", cn, tx)
                        lockCmd.Parameters.AddWithValue("@id", factureVenteId)
                        If Convert.ToString(lockCmd.ExecuteScalar()) <> "EN_ATTENTE" Then Throw New InvalidOperationException("Facture déjà payée ou invalide.")
                    End Using
                    Dim avant As Dictionary(Of String, Object) = FactureOperationService.Snapshot(cn, tx, factureVenteId)
                    Using cmd As New SqlCommand("sp_valider_paiement", cn, tx)
                        cmd.CommandType = CommandType.StoredProcedure
                        cmd.Parameters.AddRange(p.ToArray())
                        resultat = cmd.ExecuteNonQuery()
                    End Using
                    ' Le nombre de lignes ExecuteNonQuery peut valoir -1 avec
                    ' NOCOUNT. Le succès est constaté sur les données persistées.
                    Using cmd As New SqlCommand("SELECT COUNT(*) FROM dbo.Paiements p JOIN dbo.FacturesVente f ON f.FactureVenteId=p.FactureVenteId WHERE f.FactureVenteId=@id AND f.Statut='PAYEE' AND p.PayePar=@user AND p.Montant=@montant", cn, tx)
                        cmd.Parameters.AddWithValue("@id", factureVenteId)
                        cmd.Parameters.AddWithValue("@user", payePar)
                        cmd.Parameters.AddWithValue("@montant", montant)
                        If Convert.ToInt32(cmd.ExecuteScalar()) <> 1 Then Throw New InvalidOperationException("Le paiement n'a pas été validé : opération annulée.")
                    End Using
                    Dim apres As Dictionary(Of String, Object) = FactureOperationService.Snapshot(cn, tx, factureVenteId)
                    apres.Add("Paiement", New Dictionary(Of String, Object) From {{"Montant", montant}, {"EncaissePar", payePar}})
                    AuditMetierService.Enregistrer(cn, tx, "ENCAISSEMENT_VALIDE", "CAISSE", "Facture", factureVenteId,
                        Convert.ToString(DirectCast(avant("Entete"), Dictionary(Of String, Object))("NumeroFacture")),
                        avant, apres, String.Empty, Guid.NewGuid())
                    tx.Commit()
                End Using
            End Using
            AppEvents.OnPaiementValide()
            AppEvents.OnCaisseModifiee()
            AppEvents.OnAnalyseVenteModifiee()
            AppEvents.OnDataChanged()
            Return resultat
        End Function

        ' Encaissement avec transaction: paiement + stock + statut facture.
        Public Sub EncaisserFacture(factureVenteId As Integer, modePaiement As String, referencePaiement As String, montantRecuFc As Decimal, monnaieRendueFc As Decimal, devise As String, payePar As Integer)
            Dim correlation As Guid = Guid.NewGuid()
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        AutorisationActionService.Exiger(cn, tx, "ENCAISSEMENT_CREER", "CAISSE")
                        If payePar <> SessionUtilisateur.UtilisateurId Then Throw New UnauthorizedAccessException("L'auteur du paiement doit être l'utilisateur connecté.")
                        Dim total As Decimal = 0D
                        Dim statut As String = ""
                        Dim numeroFacture As String = ""
                        Using cmdTotal As New SqlCommand("SELECT MontantTotal, Statut, NumeroFacture FROM FacturesVente WITH (UPDLOCK, HOLDLOCK) WHERE FactureVenteId=@id", cn, tx)
                            cmdTotal.Parameters.AddWithValue("@id", factureVenteId)
                            Using r As SqlDataReader = cmdTotal.ExecuteReader()
                                If Not r.Read() Then
                                    Throw New Exception("Facture introuvable.")
                                End If
                                total = Convert.ToDecimal(r("MontantTotal"))
                                statut = Convert.ToString(r("Statut"))
                                numeroFacture = Convert.ToString(r("NumeroFacture"))
                            End Using
                        End Using

                        If statut <> "EN_ATTENTE" Then
                            Throw New Exception("Facture deja payee ou invalide.")
                        End If

                        Dim lignes As New List(Of Tuple(Of Integer, Decimal))()
                        Using cmdL As New SqlCommand("SELECT ProduitId, ISNULL(QuantiteBase, Quantite) AS QuantiteBase FROM LignesFactureVente WHERE FactureVenteId=@id", cn, tx)
                            cmdL.Parameters.AddWithValue("@id", factureVenteId)
                            Using r As SqlDataReader = cmdL.ExecuteReader()
                                While r.Read()
                                    lignes.Add(New Tuple(Of Integer, Decimal)(Convert.ToInt32(r("ProduitId")), Convert.ToDecimal(r("QuantiteBase"))))
                                End While
                            End Using
                        End Using

                        ' Plusieurs types de vente peuvent viser le même produit.
                        ' Vérifier leur besoin total évite un stock négatif, sans
                        ' changer les lignes de sortie ni leur QuantiteBase.
                        Dim besoins As IEnumerable(Of Tuple(Of Integer, Decimal)) = lignes.GroupBy(Function(l) l.Item1).
                            Select(Function(g) New Tuple(Of Integer, Decimal)(g.Key, g.Sum(Function(l) l.Item2))).OrderBy(Function(l) l.Item1)
                        For Each l As Tuple(Of Integer, Decimal) In besoins
                            Dim stock As Decimal = 0D
                            Using cmdS As New SqlCommand("" &
                                "SELECT ISNULL(e.Entree,0) - ISNULL(s.Sortie,0) - ISNULL(p.Perte,0) AS Stock " &
                                "FROM Produits pr " &
                                "LEFT JOIN (SELECT ProduitId, SUM(QuantiteBase) AS Entree FROM StockEntree WITH (UPDLOCK, HOLDLOCK) WHERE ProduitId=@id GROUP BY ProduitId) e ON e.ProduitId = pr.ProduitId " &
                                "LEFT JOIN (SELECT ProduitId, SUM(QuantiteBase) AS Sortie FROM StockSortie WITH (UPDLOCK, HOLDLOCK) WHERE ProduitId=@id GROUP BY ProduitId) s ON s.ProduitId = pr.ProduitId " &
                                "LEFT JOIN (SELECT ProduitId, SUM(QuantiteBase) AS Perte FROM StockPerte WITH (UPDLOCK, HOLDLOCK) WHERE ProduitId=@id GROUP BY ProduitId) p ON p.ProduitId = pr.ProduitId " &
                                "WHERE pr.ProduitId=@id", cn, tx)
                                cmdS.Parameters.AddWithValue("@id", l.Item1)
                                Dim v As Object = cmdS.ExecuteScalar()
                                stock = If(v Is Nothing, 0D, Convert.ToDecimal(v))
                            End Using
                            If stock < l.Item2 Then
                                Throw New Exception("Stock insuffisant pour un produit.")
                            End If
                        Next

                        For Each l As Tuple(Of Integer, Decimal) In lignes
                            Using cmdU As New SqlCommand("INSERT INTO StockSortie (ProduitId, QuantiteSaisie, Unite, QuantiteBase, DateSortie, Source, RefSource, CreePar) " &
                                                         "VALUES (@ProduitId, @QuantiteSaisie, @Unite, @QuantiteBase, GETDATE(), @Source, @RefSource, @CreePar)", cn, tx)
                                cmdU.Parameters.AddWithValue("@ProduitId", l.Item1)
                                cmdU.Parameters.AddWithValue("@QuantiteSaisie", l.Item2)
                                cmdU.Parameters.AddWithValue("@Unite", "base")
                                cmdU.Parameters.AddWithValue("@QuantiteBase", l.Item2)
                                cmdU.Parameters.AddWithValue("@Source", "VENTE")
                                cmdU.Parameters.AddWithValue("@RefSource", numeroFacture)
                                cmdU.Parameters.AddWithValue("@CreePar", payePar)
                                cmdU.ExecuteNonQuery()
                            End Using
                        Next

                        Using cmdP As New SqlCommand("INSERT INTO Paiements (FactureVenteId, ModePaiement, ReferencePaiement, Montant, MontantRecu, MonnaieRendue, Devise, PayePar, ModifierPar) " &
                                                     "VALUES (@FactureVenteId, @ModePaiement, @ReferencePaiement, @Montant, @MontantRecu, @MonnaieRendue, @Devise, @PayePar, @ModifierPar)", cn, tx)
                            cmdP.Parameters.AddWithValue("@FactureVenteId", factureVenteId)
                            cmdP.Parameters.AddWithValue("@ModePaiement", modePaiement)
                            cmdP.Parameters.AddWithValue("@ReferencePaiement", If(referencePaiement, CType(DBNull.Value, Object)))
                            cmdP.Parameters.AddWithValue("@Montant", total)
                            cmdP.Parameters.AddWithValue("@MontantRecu", montantRecuFc)
                            cmdP.Parameters.AddWithValue("@MonnaieRendue", monnaieRendueFc)
                            cmdP.Parameters.AddWithValue("@Devise", If(devise, CType(DBNull.Value, Object)))
                            cmdP.Parameters.AddWithValue("@PayePar", payePar)
                            cmdP.Parameters.AddWithValue("@ModifierPar", SessionUtilisateur.NomUtilisateur)
                            cmdP.ExecuteNonQuery()
                        End Using

                        Using cmdF As New SqlCommand("UPDATE FacturesVente SET Statut='PAYEE', ValideLe=GETDATE(), ModifierPar=@ModifierPar WHERE FactureVenteId=@id AND Statut='EN_ATTENTE'", cn, tx)
                            cmdF.Parameters.AddWithValue("@id", factureVenteId)
                            cmdF.Parameters.AddWithValue("@ModifierPar", SessionUtilisateur.NomUtilisateur)
                            If cmdF.ExecuteNonQuery() <> 1 Then
                                Throw New Exception("Facture deja payee ou invalide.")
                            End If
                        End Using

                        ' Paiement et audit sont validés ensemble : un échec
                        ' d'écriture du journal annule toute la transaction.
                        AuditMetierService.Enregistrer(cn, tx, "ENCAISSEMENT_VALIDE", "CAISSE", "Facture", factureVenteId, numeroFacture,
                            New With {.Statut = statut}, New With {.Statut = "PAYEE", .Montant = total, .ModePaiement = modePaiement, .Devise = devise, .FactureCreePar = LireAuteurFacture(cn, tx, factureVenteId), .EncaissePar = payePar}, String.Empty, correlation)
                        tx.Commit()
                    Catch ex As Exception
                        If tx.Connection IsNot Nothing Then tx.Rollback()
                        AuditMetierService.Echec(_dal, "ENCAISSEMENT_REFUSE", factureVenteId, correlation, ex)
                        Throw
                    End Try
                End Using
            End Using
            ' Les notifications UI sont émises après la transaction : une
            ' erreur d'affichage ne doit pas être classée comme un rollback SQL.
            AppEvents.OnVenteValidee()
            AppEvents.OnPaiementValide()
            AppEvents.OnStockModifie()
            AppEvents.OnCaisseModifiee()
            AppEvents.OnAnalyseVenteModifiee()
            AppEvents.OnDataChanged()
        End Sub

        Private Shared Function LireAuteurFacture(cn As SqlConnection, tx As SqlTransaction, id As Integer) As Integer
            Using cmd As New SqlCommand("SELECT CreePar FROM dbo.FacturesVente WHERE FactureVenteId=@id", cn, tx)
                cmd.Parameters.AddWithValue("@id", id)
                Return Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        End Function
    End Class
End Namespace
