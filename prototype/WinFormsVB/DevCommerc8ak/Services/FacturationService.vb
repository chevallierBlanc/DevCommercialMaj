Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

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
            Dim factureId As Integer = repo.Ajouter(f)
            If factureId > 0 Then
                AppEvents.OnVenteCreee()
                AppEvents.OnDataChanged()
            End If
            Return factureId
        End Function

        ' Ajoute une ligne a une facture.
        Public Function AjouterLigne(factureVenteId As Integer, produitId As Integer, quantite As Decimal, QuantiteBase As Decimal, TypeVente As String, prixUnitaire As Decimal, montantRemise As Decimal, Optional quantiteFacturee As Decimal? = Nothing) As Integer
            Dim repo As New LigneFactureVenteRepository(_dal)
            Dim quantiteMontant As Decimal = If(quantiteFacturee.HasValue, quantiteFacturee.Value, quantite)
            Dim montantLigne As Decimal = (quantiteMontant * prixUnitaire) - montantRemise
            Dim ligne As New LigneFactureVente With {
                .FactureVenteId = factureVenteId,
                .ProduitId = produitId,
                .Quantite = quantite,
                .QuantiteBase = QuantiteBase,
                .TypeVente = TypeVente,
                .PrixUnitaire = prixUnitaire,
                .MontantRemise = montantRemise,
                .MontantLigne = montantLigne,
                .QteSaisie = quantiteFacturee
            }
            Return repo.Ajouter(ligne)
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

            Dim resultat As Integer = _dal.ExecuterNonRequete("sp_valider_paiement", CommandType.StoredProcedure, p)
            If resultat > 0 Then
                AppEvents.OnPaiementValide()
                AppEvents.OnCaisseModifiee()
                AppEvents.OnAnalyseVenteModifiee()
                AppEvents.OnDataChanged()
            End If
            Return resultat
        End Function

        ' Encaissement avec transaction: paiement + stock + statut facture.
        Public Sub EncaisserFacture(factureVenteId As Integer, modePaiement As String, referencePaiement As String, montantRecuFc As Decimal, monnaieRendueFc As Decimal, devise As String, payePar As Integer)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        Dim total As Decimal = 0D
                        Dim statut As String = ""
                        Dim numeroFacture As String = ""
                        Using cmdTotal As New SqlCommand("SELECT MontantTotal, Statut, NumeroFacture FROM FacturesVente WHERE FactureVenteId=@id", cn, tx)
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
                        Using cmdL As New SqlCommand("SELECT ProduitId, Quantite FROM LignesFactureVente WHERE FactureVenteId=@id", cn, tx)
                            cmdL.Parameters.AddWithValue("@id", factureVenteId)
                            Using r As SqlDataReader = cmdL.ExecuteReader()
                                While r.Read()
                                    lignes.Add(New Tuple(Of Integer, Decimal)(Convert.ToInt32(r("ProduitId")), Convert.ToDecimal(r("Quantite"))))
                                End While
                            End Using
                        End Using

                        For Each l As Tuple(Of Integer, Decimal) In lignes
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

                        Using cmdF As New SqlCommand("UPDATE FacturesVente SET Statut='PAYEE', ValideLe=GETDATE(), ModifierPar=@ModifierPar WHERE FactureVenteId=@id", cn, tx)
                            cmdF.Parameters.AddWithValue("@id", factureVenteId)
                            cmdF.Parameters.AddWithValue("@ModifierPar", SessionUtilisateur.NomUtilisateur)
                            cmdF.ExecuteNonQuery()
                        End Using

                        tx.Commit()
                        AppEvents.OnVenteValidee()
                        AppEvents.OnPaiementValide()
                        AppEvents.OnStockModifie()
                        AppEvents.OnCaisseModifiee()
                        AppEvents.OnAnalyseVenteModifiee()
                        AppEvents.OnDataChanged()
                    Catch
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub
    End Class
End Namespace
