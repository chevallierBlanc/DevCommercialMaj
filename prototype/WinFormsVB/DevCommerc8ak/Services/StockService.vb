Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Web.Script.Serialization

Namespace DevCommerc8ak
    Public Class StockService
        Private ReadOnly _dal As DAL
        Private ReadOnly _entreeRepo As StockEntreeRepository
        Private ReadOnly _sortieRepo As StockSortieRepository
        Private ReadOnly _perteRepo As StockPerteRepository
        Private ReadOnly _inventaireRepo As StockInventaireRepository
        Private ReadOnly _mvtRepo As MouvementStockRepository

        Public Sub New(dal As DAL)
            _dal = dal
            _entreeRepo = New StockEntreeRepository(dal)
            _sortieRepo = New StockSortieRepository(dal)
            _perteRepo = New StockPerteRepository(dal)
            _inventaireRepo = New StockInventaireRepository(dal)
            _mvtRepo = New MouvementStockRepository(dal)
            AssurerVueStock()
        End Sub

        ' Enregistre une entree de stock.
        Public Function EnregistrerEntree(produitId As Integer, quantiteSaisie As Decimal, unite As String, reference As String, observation As String, effectuePar As Integer, Optional prixAchatOverride As Decimal = 0D) As Integer
            Return EnregistrerMouvement(produitId, "ENTREE", quantiteSaisie, unite, reference, observation, Nothing, effectuePar, prixAchatOverride)
        End Function

        ' Enregistre une sortie de stock.
        Public Function EnregistrerSortie(produitId As Integer, quantiteSaisie As Decimal, unite As String, reference As String, observation As String, effectuePar As Integer) As Integer
            Return EnregistrerMouvement(produitId, "SORTIE", quantiteSaisie, unite, reference, observation, Nothing, effectuePar)
        End Function

        Public Function EnregistrerSortiesManuelles(lignes As IEnumerable(Of StockSortie), motifId As Integer, clientId As Integer?, statutPaiement As String, montantPaye As Decimal, resteAPayer As Decimal, observation As String, effectuePar As Integer) As String
            If lignes Is Nothing Then
                Throw New Exception("Aucune ligne de sortie à enregistrer.")
            End If
            Dim items As New List(Of StockSortie)(lignes)
            If items.Count = 0 Then
                Throw New Exception("Aucune ligne de sortie à enregistrer.")
            End If
            If String.Equals(statutPaiement, "IMPAYE", StringComparison.OrdinalIgnoreCase) AndAlso Not clientId.HasValue Then
                Throw New Exception("Le client est obligatoire pour une dette client.")
            End If

            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        Dim numeroSortie As String = GenererNumeroSortie(cn, tx)
                        For Each item As StockSortie In items
                            Dim stockActuel As Decimal = ObtenirStockActuel(item.ProduitId, cn, tx)
                            If stockActuel < item.QuantiteBase Then
                                Throw New Exception("Stock insuffisant pour le produit " & item.ProduitId.ToString() & ".")
                            End If

                            item.NumeroSortie = numeroSortie
                            item.ClientId = clientId
                            item.MotifId = motifId
                            item.StatutPaiement = statutPaiement
                            item.MontantPaye = montantPaye
                            item.ResteAPayer = resteAPayer
                            item.Observation = observation
                            item.Source = "SORTIE_MANUELLE"
                            item.RefSource = numeroSortie
                            item.CreePar = effectuePar

                            _sortieRepo.Ajouter(item, numeroSortie, cn, tx)

                            Dim mouvement As New MouvementStock With {
                                .NumeroMouvement = GenererNumeroMouvement(cn, tx),
                                .ProduitId = item.ProduitId,
                                .TypeMouvement = "SORTIE_MANUELLE",
                                .Quantite = item.QuantiteSaisie,
                                .QuantiteBase = item.QuantiteBase,
                                .Unite = item.Unite,
                                .StockAvant = stockActuel,
                                .StockApres = stockActuel - item.QuantiteBase,
                                .Reference = numeroSortie,
                                .Observation = If(String.IsNullOrWhiteSpace(observation), item.Observation, observation),
                                .EffectuePar = effectuePar
                            }
                            _mvtRepo.Ajouter(mouvement, cn, tx)
                        Next

                        tx.Commit()
                        Try
                            Dim syncService As New OfflineSyncService(_dal)
                            For Each item As StockSortie In items
                                syncService.EssayerSynchroniserStockSortie(item)
                            Next
                        Catch
                        End Try
                        Return numeroSortie
                    Catch
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Public Function EnregistrerPaiementSortieManuelle(numeroSortie As String, montantPaye As Decimal, effectuePar As Integer) As Decimal
            If String.IsNullOrWhiteSpace(numeroSortie) Then
                Throw New Exception("Numero de sortie invalide.")
            End If
            If montantPaye <= 0D Then
                Throw New Exception("Le montant du paiement doit etre superieur a zero.")
            End If

            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        Dim total As Decimal = 0D
                        Dim dejaPaye As Decimal = 0D
                        Using cmd As New SqlCommand("SELECT ISNULL(SUM(ISNULL(MontantLigne,0)),0) AS TotalLigne, ISNULL(MAX(ISNULL(MontantPaye,0)),0) AS MontantPaye FROM StockSortie WITH (UPDLOCK, HOLDLOCK) WHERE NumeroSortie=@NumeroSortie", cn, tx)
                            cmd.Parameters.AddWithValue("@NumeroSortie", numeroSortie)
                            Using r As SqlDataReader = cmd.ExecuteReader()
                                If Not r.Read() Then
                                    Throw New Exception("Sortie introuvable.")
                                End If
                                total = Convert.ToDecimal(r("TotalLigne"))
                                dejaPaye = Convert.ToDecimal(r("MontantPaye"))
                            End Using
                        End Using

                        Dim resteAvant As Decimal = Math.Max(0D, total - dejaPaye)
                        If resteAvant <= 0D Then
                            Throw New Exception("Cette sortie est deja totalement reglee.")
                        End If

                        Dim montantAffecte As Decimal = Math.Min(montantPaye, resteAvant)
                        Dim nouveauPaye As Decimal = dejaPaye + montantAffecte
                        Dim nouveauReste As Decimal = Math.Max(0D, total - nouveauPaye)
                        Dim statut As String = If(nouveauReste <= 0D, "PAYE", "IMPAYE")

                        Using cmdUpdate As New SqlCommand("UPDATE StockSortie SET MontantPaye=@MontantPaye, ResteAPayer=@ResteAPayer, StatutPaiement=@StatutPaiement, CreePar=CreePar WHERE NumeroSortie=@NumeroSortie", cn, tx)
                            cmdUpdate.Parameters.AddWithValue("@MontantPaye", nouveauPaye)
                            cmdUpdate.Parameters.AddWithValue("@ResteAPayer", nouveauReste)
                            cmdUpdate.Parameters.AddWithValue("@StatutPaiement", statut)
                            cmdUpdate.Parameters.AddWithValue("@NumeroSortie", numeroSortie)
                            cmdUpdate.ExecuteNonQuery()
                        End Using

                        tx.Commit()
                        Try
                            Dim syncService As New OfflineSyncService(_dal)
                            syncService.EssayerSynchroniserSortieParNumero(numeroSortie)
                        Catch
                        End Try
                        Return nouveauReste
                    Catch
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Public Function ListerSortieManuelleParNumero(numeroSortie As String) As DataTable
            Dim sql As String = "" &
                "SELECT ss.NumeroSortie, ss.DateSortie, ISNULL(c.NomClient, '') AS Client, ISNULL(m.Libelle, ss.Source) AS Motif, " &
                "p.Libelle AS Produit, ss.QuantiteSaisie, ss.QuantiteBase, ss.Unite, ss.TypeVente, ss.PrixUnitaire, ss.MontantLigne, " &
                "ss.StatutPaiement, ss.MontantPaye, ss.ResteAPayer, ss.Observation " &
                "FROM StockSortie ss " &
                "INNER JOIN Produits p ON p.ProduitId = ss.ProduitId " &
                "LEFT JOIN Clients c ON c.ClientId = ss.ClientId " &
                "LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId " &
                "WHERE ss.NumeroSortie = @NumeroSortie " &
                "ORDER BY ss.StockSortieId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@NumeroSortie", numeroSortie)}
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        ' Enregistre une perte/casse.
        Public Function EnregistrerPerte(produitId As Integer, quantiteSaisie As Decimal, unite As String, reference As String, observation As String, typePerte As String, effectuePar As Integer) As Integer
            Return EnregistrerMouvement(produitId, "PERTE", quantiteSaisie, unite, reference, observation, typePerte, effectuePar)
        End Function

        ' Inventaire: ajuste le stock a la quantite comptee et retourne l'ecart.
        Public Function AjusterInventaire(produitId As Integer, quantiteComptee As Decimal, unite As String, reference As String, observation As String, effectuePar As Integer) As Decimal
            Dim info As DataRow = ObtenirInfosProduit(produitId)
            Dim stockActuel As Decimal = ObtenirStockActuel(produitId)
            Dim quantiteBase As Decimal = ConvertirEnBase(info, unite, quantiteComptee)
            Dim ecart As Decimal = quantiteBase - stockActuel

            Dim inv As New StockInventaire With {
                .ProduitId = produitId,
                .StockTheorique = stockActuel,
                .StockReel = quantiteBase,
                .Ecart = ecart,
                .DateInventaire = Date.Now,
                .CreePar = effectuePar,
                .Observation = observation
            }
            _inventaireRepo.Ajouter(inv)

            If ecart > 0D Then
                Dim entree As New StockEntree With {
                    .IdStock = GenererNumeroStock("INV"),
                    .ProduitId = produitId,
                    .QuantiteSaisie = ecart,
                    .Unite = "base",
                    .QuantiteBase = ecart,
                    .PrixAchat = ObtenirPrixAchat(info),
                    .Devise = "CDF",
                    .Taux = 0D,
                    .DateEntree = Date.Now,
                    .FournisseurId = Nothing,
                    .CreePar = effectuePar
                }
                _entreeRepo.Ajouter(entree)
            ElseIf ecart < 0D Then
                Dim sortie As New StockSortie With {
                    .ProduitId = produitId,
                    .QuantiteSaisie = Math.Abs(ecart),
                    .Unite = "base",
                    .QuantiteBase = Math.Abs(ecart),
                    .DateSortie = Date.Now,
                    .Source = "INVENTAIRE",
                    .RefSource = reference,
                    .CreePar = effectuePar
                }
                _sortieRepo.Ajouter(sortie)
            End If

            Dim mouvement As New MouvementStock With {
                .NumeroMouvement = GenererNumeroMouvement(),
                .ProduitId = produitId,
                .TypeMouvement = "INVENTAIRE",
                .Quantite = quantiteComptee,
                .QuantiteBase = quantiteBase,
                .Unite = unite,
                .StockAvant = stockActuel,
                .StockApres = quantiteBase,
                .Reference = reference,
                .Observation = If(observation, ""),
                .TypePerte = Nothing,
                .EffectuePar = effectuePar
            }
            _mvtRepo.Ajouter(mouvement)

            Return ecart
        End Function

        ' Liste des mouvements par produit.
        Public Function ListerParProduit(produitId As Integer) As List(Of MouvementStockDTO)
            Return _mvtRepo.ListerParProduit(produitId)
        End Function

        ' Liste tous les mouvements.
        Public Function ListerTous() As List(Of MouvementStockDTO)
            Return _mvtRepo.ListerTous()
        End Function

        ' Retourne le stock reel courant en unite secondaire/base calculee.
        Public Function ObtenirStockActuelProduit(produitId As Integer) As Decimal
            Return ObtenirStockActuel(produitId)
        End Function

        ' Annule un mouvement et restaure le stock.
        Public Sub AnnulerMouvement(mouvementStockId As Integer, effectuePar As Integer, motif As String)
            Dim mv As MouvementStockDTO = _mvtRepo.ObtenirParId(mouvementStockId)
            If mv Is Nothing Then
                Throw New Exception("Mouvement introuvable.")
            End If
            If mv.EstAnnule Then
                Throw New Exception("Mouvement deja annule.")
            End If

            Dim info As DataRow = ObtenirInfosProduit(mv.ProduitId)
            If mv.TypeMouvement = "ENTREE" Then
                Dim sortie As New StockSortie With {
                    .ProduitId = mv.ProduitId,
                    .QuantiteSaisie = mv.QuantiteBase,
                    .Unite = "base",
                    .QuantiteBase = mv.QuantiteBase,
                    .DateSortie = Date.Now,
                    .Source = "ANNULATION",
                    .RefSource = mv.NumeroMouvement,
                    .CreePar = effectuePar
                }
                _sortieRepo.Ajouter(sortie)
            ElseIf mv.TypeMouvement = "SORTIE" OrElse mv.TypeMouvement = "PERTE" Then
                Dim entree As New StockEntree With {
                    .IdStock = GenererNumeroStock("ANN"),
                    .ProduitId = mv.ProduitId,
                    .QuantiteSaisie = mv.QuantiteBase,
                    .Unite = "base",
                    .QuantiteBase = mv.QuantiteBase,
                    .PrixAchat = ObtenirPrixAchat(info),
                    .Devise = "CDF",
                    .Taux = 0D,
                    .DateEntree = Date.Now,
                    .FournisseurId = Nothing,
                    .CreePar = effectuePar
                }
                _entreeRepo.Ajouter(entree)
            End If

            Dim sql As String = "UPDATE MouvementsStock SET EstAnnule=1, AnnulePar=@u, AnnuleLe=GETDATE(), AnnulationRef=@r WHERE MouvementStockId=@id"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@u", effectuePar),
                New SqlParameter("@r", motif),
                New SqlParameter("@id", mouvementStockId)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Private Function EnregistrerMouvement(produitId As Integer, typeMouvement As String, quantiteSaisie As Decimal, unite As String, reference As String, observation As String, typePerte As String, effectuePar As Integer, Optional prixAchatOverride As Decimal = 0D) As Integer
            Dim info As DataRow = ObtenirInfosProduit(produitId)
            Dim stockActuel As Decimal = ObtenirStockActuel(produitId)
            Dim quantiteBase As Decimal = ConvertirEnBase(info, unite, quantiteSaisie)

            Dim stockApres As Decimal = stockActuel
            If typeMouvement = "ENTREE" Then
                stockApres = stockActuel + quantiteBase
            ElseIf typeMouvement = "SORTIE" OrElse typeMouvement = "PERTE" Then
                stockApres = stockActuel - quantiteBase
                If stockApres < 0D Then
                    Throw New Exception("Stock insuffisant.")
                End If
            End If

            If typeMouvement = "ENTREE" Then
                Dim prixAchatUse As Decimal = If(prixAchatOverride > 0D, prixAchatOverride, ObtenirPrixAchat(info))
                Dim entree As New StockEntree With {
                    .IdStock = GenererNumeroStock("ENT"),
                    .ProduitId = produitId,
                    .QuantiteSaisie = quantiteSaisie,
                    .Unite = unite,
                    .QuantiteBase = quantiteBase,
                    .PrixAchat = prixAchatUse,
                    .Devise = "CDF",
                    .Taux = 0D,
                    .DateEntree = Date.Now,
                    .FournisseurId = Nothing,
                    .CreePar = effectuePar
                }
                _entreeRepo.Ajouter(entree)
            ElseIf typeMouvement = "SORTIE" Then
                Dim sortie As New StockSortie With {
                    .ProduitId = produitId,
                    .QuantiteSaisie = quantiteSaisie,
                    .Unite = unite,
                    .QuantiteBase = quantiteBase,
                    .DateSortie = Date.Now,
                    .Source = "MANUEL",
                    .RefSource = reference,
                    .CreePar = effectuePar
                }
                _sortieRepo.Ajouter(sortie)
            ElseIf typeMouvement = "PERTE" Then
                Dim perte As New StockPerte With {
                    .ProduitId = produitId,
                    .QuantiteSaisie = quantiteSaisie,
                    .Unite = unite,
                    .QuantiteBase = quantiteBase,
                    .TypePerte = typePerte,
                    .Motif = observation,
                    .DatePerte = Date.Now,
                    .CreePar = effectuePar
                }
                _perteRepo.Ajouter(perte)
            End If

            Dim mouvement As New MouvementStock With {
                .NumeroMouvement = GenererNumeroMouvement(),
                .ProduitId = produitId,
                .TypeMouvement = typeMouvement,
                .Quantite = quantiteSaisie,
                .QuantiteBase = quantiteBase,
                .Unite = unite,
                .StockAvant = stockActuel,
                .StockApres = stockApres,
                .Reference = reference,
                .Observation = If(observation, ""),
                .TypePerte = typePerte,
                .EffectuePar = effectuePar
            }
            Return _mvtRepo.Ajouter(mouvement)
        End Function

        Private Function ObtenirInfosProduit(produitId As Integer) As DataRow
            Dim sql As String = "SELECT ProduitId, Libelle, CategorieId, UnitePrincipale, UniteSecondaire, ConversionUnite, PrixAchat, PrixDetail, PrixGros " &
                                "FROM Produits WHERE ProduitId=@id"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@id", produitId)}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            If dt.Rows.Count = 0 Then
                Throw New Exception("Produit introuvable.")
            End If
            Return dt.Rows(0)
        End Function

        Private Function ObtenirStockActuel(produitId As Integer) As Decimal
            Dim sql As String = "SELECT ISNULL(QuantiteStock,0) FROM vStockProduit WHERE ProduitId=@id"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@id", produitId)}
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            If v Is Nothing Then Return 0D
            Return Convert.ToDecimal(v)
        End Function

        Private Function ObtenirStockActuel(produitId As Integer, cn As SqlConnection, tx As SqlTransaction) As Decimal
            Dim sql As String = "SELECT ISNULL(QuantiteStock,0) FROM vStockProduit WHERE ProduitId=@id"
            Using cmd As New SqlCommand(sql, cn, tx)
                cmd.Parameters.AddWithValue("@id", produitId)
                Dim v As Object = cmd.ExecuteScalar()
                If v Is Nothing OrElse v Is DBNull.Value Then Return 0D
                Return Convert.ToDecimal(v)
            End Using
        End Function

        Private Function ObtenirPrixAchat(info As DataRow) As Decimal
            Dim prixAchat As Decimal = 0D
            If Not info.IsNull("PrixAchat") Then
                prixAchat = Convert.ToDecimal(info("PrixAchat"))
            End If
            If prixAchat <= 0D AndAlso Not info.IsNull("PrixGros") Then
                prixAchat = Convert.ToDecimal(info("PrixGros"))
            End If
            Return prixAchat
        End Function

        Private Function ConvertirEnBase(info As DataRow, unite As String, quantite As Decimal) As Decimal
            Dim uniteBase As String = If(info.IsNull("UnitePrincipale"), "", Convert.ToString(info("UnitePrincipale")))
            Dim uniteSecondaire As String = If(info.IsNull("UniteSecondaire"), "", Convert.ToString(info("UniteSecondaire")))
            Dim conversion As Decimal = If(info.IsNull("ConversionUnite"), 0D, Convert.ToDecimal(info("ConversionUnite")))
            If conversion > 0D Then
                If uniteBase <> "" AndAlso String.Equals(unite, uniteBase, StringComparison.OrdinalIgnoreCase) Then
                    Return quantite * conversion
                End If
                If uniteSecondaire <> "" AndAlso String.Equals(unite, uniteSecondaire, StringComparison.OrdinalIgnoreCase) Then
                    Return quantite
                End If
            End If
            Return quantite
        End Function

        Private Function GenererNumeroMouvement() As String
            Dim anneeMois As String = Date.Now.ToString("yyyyMM")
            Dim sql As String = "" &
                "DECLARE @n INT; BEGIN TRAN; " &
                "IF EXISTS (SELECT 1 FROM MouvementSequence WITH (UPDLOCK, HOLDLOCK) WHERE AnneeMois=@AnneeMois) " &
                "BEGIN UPDATE MouvementSequence SET DernierNumero = DernierNumero + 1 WHERE AnneeMois=@AnneeMois; " &
                "SELECT @n = DernierNumero FROM MouvementSequence WHERE AnneeMois=@AnneeMois; END " &
                "ELSE BEGIN INSERT INTO MouvementSequence (AnneeMois, DernierNumero) VALUES (@AnneeMois, 1); SET @n=1; END " &
                "COMMIT; SELECT @n;"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@AnneeMois", anneeMois)}
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Dim numero As Integer = Convert.ToInt32(v)
            Return "MVT-" & anneeMois & "-" & numero.ToString("000")
        End Function

        Private Function GenererNumeroMouvement(cn As SqlConnection, tx As SqlTransaction) As String
            Dim anneeMois As String = Date.Now.ToString("yyyyMM")
            Dim sql As String = "" &
                "DECLARE @n INT; " &
                "IF EXISTS (SELECT 1 FROM MouvementSequence WITH (UPDLOCK, HOLDLOCK) WHERE AnneeMois=@AnneeMois) " &
                "BEGIN UPDATE MouvementSequence SET DernierNumero = DernierNumero + 1 WHERE AnneeMois=@AnneeMois; " &
                "SELECT @n = DernierNumero FROM MouvementSequence WHERE AnneeMois=@AnneeMois; END " &
                "ELSE BEGIN INSERT INTO MouvementSequence (AnneeMois, DernierNumero) VALUES (@AnneeMois, 1); SET @n=1; END " &
                "SELECT @n;"
            Using cmd As New SqlCommand(sql, cn, tx)
                cmd.Parameters.AddWithValue("@AnneeMois", anneeMois)
                Dim v As Object = cmd.ExecuteScalar()
                Dim numero As Integer = Convert.ToInt32(v)
                Return "MVT-" & anneeMois & "-" & numero.ToString("000")
            End Using
        End Function

        Private Function GenererNumeroStock(prefix As String) As String
            Dim anneeMois As String = Date.Now.ToString("yyyyMM")
            Dim sql As String = "" &
                "DECLARE @n INT; BEGIN TRAN; " &
                "IF EXISTS (SELECT 1 FROM StockSequence WITH (UPDLOCK, HOLDLOCK) WHERE Prefix=@Prefix AND AnneeMois=@AnneeMois) " &
                "BEGIN UPDATE StockSequence SET DernierNumero = DernierNumero + 1 WHERE Prefix=@Prefix AND AnneeMois=@AnneeMois; " &
                "SELECT @n = DernierNumero FROM StockSequence WHERE Prefix=@Prefix AND AnneeMois=@AnneeMois; END " &
                "ELSE BEGIN INSERT INTO StockSequence (Prefix, AnneeMois, DernierNumero) VALUES (@Prefix, @AnneeMois, 1); SET @n=1; END " &
                "COMMIT; SELECT @n;"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@Prefix", prefix),
                New SqlParameter("@AnneeMois", anneeMois)
            }
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Dim numero As Integer = Convert.ToInt32(v)
            Return prefix & "-" & anneeMois & "-" & numero.ToString("000")
        End Function

        Private Function GenererNumeroSortie(cn As SqlConnection, tx As SqlTransaction) As String
            Dim prefix As String = "SORT-" & Date.Now.ToString("yyyyMMdd")
            Dim sql As String = "" &
                "DECLARE @n INT; " &
                "SELECT @n = ISNULL(MAX(CAST(RIGHT(NumeroSortie, 3) AS INT)), 0) + 1 " &
                "FROM StockSortie WITH (UPDLOCK, HOLDLOCK) WHERE NumeroSortie LIKE @PrefixLike; " &
                "SELECT @n;"
            Using cmd As New SqlCommand(sql, cn, tx)
                cmd.Parameters.AddWithValue("@PrefixLike", prefix & "-%")
                Dim v As Object = cmd.ExecuteScalar()
                Dim numero As Integer = Convert.ToInt32(v)
                Return prefix & "-" & numero.ToString("000")
            End Using
        End Function

        Private Sub AssurerVueStock()
            ' Vue gérée par le script SQL de déploiement.
        End Sub



        ' --- MÉTHODES EXISTANTES (INCHANGÉES) ---

        'Public Function EnregistrerEntree(produitId As Integer, quantiteSaisie As Decimal, unite As String, reference As String, observation As String, effectuePar As Integer, Optional prixAchatOverride As Decimal = 0D) As Integer
        '    Return EnregistrerMouvement(produitId, "ENTREE", quantiteSaisie, unite, reference, observation, Nothing, effectuePar, prixAchatOverride)
        'End Function

        'Public Function EnregistrerSortie(produitId As Integer, quantiteSaisie As Decimal, unite As String, reference As String, observation As String, effectuePar As Integer) As Integer
        '    Return EnregistrerMouvement(produitId, "SORTIE", quantiteSaisie, unite, reference, observation, Nothing, effectuePar)
        'End Function

        'Public Function EnregistrerPerte(produitId As Integer, quantiteSaisie As Decimal, unite As String, reference As String, observation As String, typePerte As String, effectuePar As Integer) As Integer
        '    Return EnregistrerMouvement(produitId, "PERTE", quantiteSaisie, unite, reference, observation, typePerte, effectuePar)
        'End Function

        'Public Function AjusterInventaire(produitId As Integer, quantiteComptee As Decimal, unite As String, reference As String, observation As String, effectuePar As Integer) As Decimal
        '    Dim info As DataRow = ObtenirInfosProduit(produitId)
        '    Dim stockActuel As Decimal = ObtenirStockActuel(produitId)
        '    Dim quantiteBase As Decimal = ConvertirEnBase(info, unite, quantiteComptee)
        '    Dim ecart As Decimal = quantiteBase - stockActuel

        '    Dim inv As New StockInventaire With {
        '        .ProduitId = produitId,
        '        .StockTheorique = stockActuel,
        '        .StockReel = quantiteBase,
        '        .Ecart = ecart,
        '        .DateInventaire = Date.Now,
        '        .CreePar = effectuePar,
        '        .Observation = observation
        '    }
        '    _inventaireRepo.Ajouter(inv)

        '    If ecart > 0D Then
        '        Dim entree As New StockEntree With {
        '            .IdStock = GenererNumeroStock("INV"),
        '            .ProduitId = produitId,
        '            .QuantiteSaisie = ecart,
        '            .Unite = "base",
        '            .QuantiteBase = ecart,
        '            .PrixAchat = ObtenirPrixAchat(info),
        '            .Devise = "CDF",
        '            .Taux = 0D,
        '            .DateEntree = Date.Now,
        '            .FournisseurId = Nothing,
        '            .CreePar = effectuePar
        '        }
        '        _entreeRepo.Ajouter(entree)
        '    ElseIf ecart < 0D Then
        '        Dim sortie As New StockSortie With {
        '            .ProduitId = produitId,
        '            .QuantiteSaisie = Math.Abs(ecart),
        '            .Unite = "base",
        '            .QuantiteBase = Math.Abs(ecart),
        '            .DateSortie = Date.Now,
        '            .Source = "INVENTAIRE",
        '            .RefSource = reference,
        '            .CreePar = effectuePar
        '        }
        '        _sortieRepo.Ajouter(sortie)
        '    End If

        '    Dim mouvement As New MouvementStock With {
        '        .NumeroMouvement = GenererNumeroMouvement(),
        '        .ProduitId = produitId,
        '        .TypeMouvement = "INVENTAIRE",
        '        .Quantite = quantiteComptee,
        '        .QuantiteBase = quantiteBase,
        '        .Unite = unite,
        '        .StockAvant = stockActuel,
        '        .StockApres = quantiteBase,
        '        .Reference = reference,
        '        .Observation = If(observation, ""),
        '        .TypePerte = Nothing,
        '        .EffectuePar = effectuePar
        '    }
        '    _mvtRepo.Ajouter(mouvement)

        '    Return ecart
        'End Function

        'Public Function ListerParProduit(produitId As Integer) As List(Of MouvementStockDTO)
        '    Return _mvtRepo.ListerParProduit(produitId)
        'End Function

        'Public Function ListerTous() As List(Of MouvementStockDTO)
        '    Return _mvtRepo.ListerTous()
        'End Function

        'Public Function ObtenirStockActuelProduit(produitId As Integer) As Decimal
        '    Return ObtenirStockActuel(produitId)
        'End Function

        'Public Sub AnnulerMouvement(mouvementStockId As Integer, effectuePar As Integer, motif As String)
        '    Dim mv As MouvementStockDTO = _mvtRepo.ObtenirParId(mouvementStockId)
        '    If mv Is Nothing Then Throw New Exception("Mouvement introuvable.")
        '    If mv.EstAnnule Then Throw New Exception("Mouvement deja annule.")

        '    Dim info As DataRow = ObtenirInfosProduit(mv.ProduitId)
        '    If mv.TypeMouvement = "ENTREE" Then
        '        Dim sortie As New StockSortie With {
        '            .ProduitId = mv.ProduitId,
        '            .QuantiteSaisie = mv.QuantiteBase,
        '            .Unite = "base",
        '            .QuantiteBase = mv.QuantiteBase,
        '            .DateSortie = Date.Now,
        '            .Source = "ANNULATION",
        '            .RefSource = mv.NumeroMouvement,
        '            .CreePar = effectuePar
        '        }
        '        _sortieRepo.Ajouter(sortie)
        '    ElseIf mv.TypeMouvement = "SORTIE" OrElse mv.TypeMouvement = "PERTE" Then
        '        Dim entree As New StockEntree With {
        '            .IdStock = GenererNumeroStock("ANN"),
        '            .ProduitId = mv.ProduitId,
        '            .QuantiteSaisie = mv.QuantiteBase,
        '            .Unite = "base",
        '            .QuantiteBase = mv.QuantiteBase,
        '            .PrixAchat = ObtenirPrixAchat(info),
        '            .Devise = "CDF",
        '            .Taux = 0D,
        '            .DateEntree = Date.Now,
        '            .FournisseurId = Nothing,
        '            .CreePar = effectuePar
        '        }
        '        _entreeRepo.Ajouter(entree)
        '    End If

        '    Dim sql As String = "UPDATE MouvementsStock SET EstAnnule=1, AnnulePar=@u, AnnuleLe=GETDATE(), AnnulationRef=@r WHERE MouvementStockId=@id"
        '    Dim p As New List(Of SqlParameter) From {
        '        New SqlParameter("@u", effectuePar),
        '        New SqlParameter("@r", motif),
        '        New SqlParameter("@id", mouvementStockId)
        '    }
        '    _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        'End Sub

        ' --- NOUVELLES MÉTHODES (AJOUTÉES) ---

        ''' <summary>
        ''' Enregistre une sortie manuelle (Dette, Ordre Patron, etc.)
        ''' </summary>
        Public Sub EnregistrerSortieManuelle(produitId As Integer, quantiteSaisie As Decimal, unite As String, source As String, clientInfo As String, motif As String, effectuePar As Integer)
            Dim info As DataRow = ObtenirInfosProduit(produitId)
            Dim stockActuel As Decimal = ObtenirStockActuel(produitId)
            Dim quantiteBase As Decimal = ConvertirEnBase(info, unite, quantiteSaisie)

            If stockActuel < quantiteBase Then Throw New Exception("Stock insuffisant pour cette sortie.")

            Dim sortie As New StockSortie With {
                .ProduitId = produitId,
                .QuantiteSaisie = quantiteSaisie,
                .Unite = unite,
                .QuantiteBase = quantiteBase,
                .DateSortie = Date.Now,
                .Source = source,
                .RefSource = clientInfo,
                .CreePar = effectuePar
            }
            ' Note: On utilise RefSource pour stocker le client et Motif pour le motif (si colonnes ajoutées via SQL)
            _sortieRepo.Ajouter(sortie)

            ' Enregistrement du mouvement
            Dim mouvement As New MouvementStock With {
                .NumeroMouvement = GenererNumeroMouvement(),
                .ProduitId = produitId,
                .TypeMouvement = "SORTIE",
                .Quantite = quantiteSaisie,
                .QuantiteBase = quantiteBase,
                .Unite = unite,
                .StockAvant = stockActuel,
                .StockApres = stockActuel - quantiteBase,
                .Reference = clientInfo,
                .Observation = motif,
                .EffectuePar = effectuePar
            }
            _mvtRepo.Ajouter(mouvement)
        End Sub

        ''' <summary>
        ''' Récupère l'analyse détaillée d'un produit pour l'inventaire
        ''' </summary>
        Public Function ObtenirAnalyseProduit(produitId As Integer) As DataTable
            Dim sql As String = "" &
                "WITH Stock AS (" &
                "    SELECT ISNULL(s.QuantiteStock,0) AS StockReelRestant " &
                "    FROM vStockProduit s " &
                "    WHERE s.ProduitId = @ProduitId" &
                "), Ventes AS (" &
                "    SELECT " &
                "        ISNULL(SUM(ISNULL(l.QuantiteBase,0)),0) AS TotalVentes, " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(l.TypeVente,'')) = 'GROS' THEN ISNULL(l.QuantiteBase,0) ELSE 0 END),0) AS TotalGros, " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(l.TypeVente,'')) = 'DEMI' THEN ISNULL(l.QuantiteBase,0) ELSE 0 END),0) AS TotalDemi, " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(l.TypeVente,'')) = 'QUART' THEN ISNULL(l.QuantiteBase,0) ELSE 0 END),0) AS TotalQuart, " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(l.TypeVente,'')) IN ('PIECE','UNITE') THEN ISNULL(l.QuantiteBase,0) ELSE 0 END),0) AS TotalPiece, " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(l.TypeVente,'')) = 'DOUZAINE' THEN ISNULL(l.QuantiteBase,0) ELSE 0 END),0) AS TotalDouzaine, " &
                "        ISNULL(SUM(ISNULL(l.MontantLigne,0)),0) AS MontantVentes " &
                "    FROM LignesFactureVente l " &
                "    INNER JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId " &
                "    WHERE l.ProduitId = @ProduitId AND UPPER(ISNULL(f.Statut,'')) = 'PAYEE' " &
                "), SortiesManuelles AS (" &
                "    SELECT " &
                "        ISNULL(SUM(ISNULL(ss.QuantiteBase,0)),0) AS TotalSortiesManuelles, " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(ss.TypeVente,'')) = 'GROS' THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalGros, " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(ss.TypeVente,'')) = 'DEMI' THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalDemi, " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(ss.TypeVente,'')) = 'QUART' THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalQuart, " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(ss.TypeVente,'')) IN ('PIECE','UNITE') THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalPiece, " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(ss.TypeVente,'')) = 'DOUZAINE' THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalDouzaine, " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(m.Nature,'')) LIKE '%DON%' OR UPPER(ISNULL(m.Libelle,'')) LIKE '%DON%' THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalDons, " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(m.Nature,'')) LIKE '%ALLOC%' OR UPPER(ISNULL(m.Libelle,'')) LIKE '%ALLOC%' THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalAllocations, " &
                "        ISNULL(SUM(CASE WHEN (UPPER(ISNULL(m.Nature,'')) LIKE '%DETTE%' OR UPPER(ISNULL(m.Libelle,'')) LIKE '%DETTE%') AND (UPPER(ISNULL(m.Libelle,'')) LIKE '%CLIENT%' OR (UPPER(ISNULL(ss.StatutPaiement,'')) = 'IMPAYE' AND ss.ClientId IS NOT NULL)) THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalDettesClients, " &
                "        ISNULL(SUM(CASE WHEN (UPPER(ISNULL(m.Nature,'')) LIKE '%DETTE%' OR UPPER(ISNULL(m.Libelle,'')) LIKE '%DETTE%') AND (UPPER(ISNULL(m.Libelle,'')) LIKE '%BOSS%' OR UPPER(ISNULL(m.Libelle,'')) LIKE '%PATRON%' OR UPPER(ISNULL(m.Libelle,'')) LIKE '%MAISON%') THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalDettesBoss, " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(m.Nature,'')) LIKE '%HORS%' OR UPPER(ISNULL(m.Libelle,'')) LIKE '%HORS%' THEN ISNULL(ss.QuantiteBase,0) ELSE 0 END),0) AS TotalSortiesHorsCaisse, " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(ss.StatutPaiement,'')) <> 'GRATUIT' THEN ISNULL(ss.MontantLigne,0) ELSE 0 END),0) AS MontantManuel " &
                "    FROM StockSortie ss " &
                "    LEFT JOIN MotifSortie m ON m.MotifId = ss.MotifId " &
                "    WHERE ss.ProduitId = @ProduitId AND UPPER(ISNULL(ss.Source,'')) = 'SORTIE_MANUELLE' " &
                "), Pertes AS (" &
                "    SELECT ISNULL(SUM(ISNULL(QuantiteBase,0)),0) AS TotalPertes " &
                "    FROM StockPerte " &
                "    WHERE ProduitId = @ProduitId " &
                "), Mouvements AS (" &
                "    SELECT " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(TypeMouvement,'')) = 'ENTREE' THEN ISNULL(QuantiteBase,0) ELSE 0 END),0) AS TotalEntreesMouvements, " &
                "        ISNULL(SUM(CASE WHEN UPPER(ISNULL(TypeMouvement,'')) IN ('SORTIE','SORTIE_MANUELLE','PERTE') THEN ISNULL(QuantiteBase,0) ELSE 0 END),0) AS TotalSortiesMouvements " &
                "    FROM MouvementsStock " &
                "    WHERE ProduitId = @ProduitId " &
                "), Entrees AS (" &
                "    SELECT ISNULL(SUM(ISNULL(QuantiteBase,0)),0) AS TotalEntrees " &
                "    FROM StockEntree " &
                "    WHERE ProduitId = @ProduitId " &
                ") " &
                "SELECT p.ProduitId, p.Libelle, p.ConversionUnite, p.UnitePrincipale, p.UniteSecondaire, " &
                "       e.TotalEntrees, " &
                "       v.TotalVentes, " &
                "       m.TotalSortiesManuelles, " &
                "       v.TotalGros + m.TotalGros AS TotalGros, " &
                "       v.TotalDemi + m.TotalDemi AS TotalDemi, " &
                "       v.TotalQuart + m.TotalQuart AS TotalQuart, " &
                "       v.TotalPiece + m.TotalPiece AS TotalPiece, " &
                "       v.TotalDouzaine + m.TotalDouzaine AS TotalDouzaine, " &
                "       pte.TotalPertes, " &
                "       m.TotalDons, " &
                "       m.TotalAllocations, " &
                "       m.TotalDettesClients, " &
                "       m.TotalDettesBoss, " &
                "       m.TotalSortiesHorsCaisse, " &
                "       mv.TotalEntreesMouvements, " &
                "       mv.TotalSortiesMouvements, " &
                "       s.StockReelRestant, " &
                "       CASE WHEN ISNULL(p.ConversionUnite,0) > 0 THEN FLOOR(s.StockReelRestant / p.ConversionUnite) ELSE 0 END AS StockRestantCartons, " &
                "       CASE WHEN ISNULL(p.ConversionUnite,0) > 0 THEN s.StockReelRestant - (FLOOR(s.StockReelRestant / p.ConversionUnite) * p.ConversionUnite) ELSE s.StockReelRestant END AS StockRestantPieces, " &
                "       v.MontantVentes + m.MontantManuel AS MontantTotalGenere " &
                "FROM Produits p " &
                "CROSS JOIN Entrees e " &
                "CROSS JOIN Ventes v " &
                "CROSS JOIN SortiesManuelles m " &
                "CROSS JOIN Pertes pte " &
                "CROSS JOIN Mouvements mv " &
                "CROSS JOIN Stock s " &
                "WHERE p.ProduitId = @ProduitId"
            Dim params As New List(Of SqlParameter) From {
                New SqlParameter("@ProduitId", produitId)
            }
            Return _dal.ExecuterTable(sql, CommandType.Text, params)
        End Function

        ' --- MÉTHODES PRIVÉES (INCHANGÉES) ---

        'Private Function EnregistrerMouvement(produitId As Integer, typeMouvement As String, quantiteSaisie As Decimal, unite As String, reference As String, observation As String, typePerte As String, effectuePar As Integer, Optional prixAchatOverride As Decimal = 0D) As Integer
        '    Dim info As DataRow = ObtenirInfosProduit(produitId)
        '    Dim stockActuel As Decimal = ObtenirStockActuel(produitId)
        '    Dim quantiteBase As Decimal = ConvertirEnBase(info, unite, quantiteSaisie)

        '    Dim stockApres As Decimal = stockActuel
        '    If typeMouvement = "ENTREE" Then
        '        stockApres = stockActuel + quantiteBase
        '    ElseIf typeMouvement = "SORTIE" OrElse typeMouvement = "PERTE" Then
        '        stockApres = stockActuel - quantiteBase
        '        If stockApres < 0D Then Throw New Exception("Stock insuffisant.")
        '    End If

        '    If typeMouvement = "ENTREE" Then
        '        Dim prixAchatUse As Decimal = If(prixAchatOverride > 0D, prixAchatOverride, ObtenirPrixAchat(info))
        '        Dim entree As New StockEntree With {
        '            .IdStock = GenererNumeroStock("ENT"),
        '            .ProduitId = produitId,
        '            .QuantiteSaisie = quantiteSaisie,
        '            .Unite = unite,
        '            .QuantiteBase = quantiteBase,
        '            .PrixAchat = prixAchatUse,
        '            .Devise = "CDF",
        '            .Taux = 0D,
        '            .DateEntree = Date.Now,
        '            .FournisseurId = Nothing,
        '            .CreePar = effectuePar
        '        }
        '        _entreeRepo.Ajouter(entree)
        '    ElseIf typeMouvement = "SORTIE" Then
        '        Dim sortie As New StockSortie With {
        '            .ProduitId = produitId,
        '            .QuantiteSaisie = quantiteSaisie,
        '            .Unite = unite,
        '            .QuantiteBase = quantiteBase,
        '            .DateSortie = Date.Now,
        '            .Source = "MANUEL",
        '            .RefSource = reference,
        '            .CreePar = effectuePar
        '        }
        '        _sortieRepo.Ajouter(sortie)
        '    ElseIf typeMouvement = "PERTE" Then
        '        Dim perte As New StockPerte With {
        '            .ProduitId = produitId,
        '            .QuantiteSaisie = quantiteSaisie,
        '            .Unite = unite,
        '            .QuantiteBase = quantiteBase,
        '            .TypePerte = typePerte,
        '            .Motif = observation,
        '            .DatePerte = Date.Now,
        '            .CreePar = effectuePar
        '        }
        '        _perteRepo.Ajouter(perte)
        '    End If

        '    Dim mouvement As New MouvementStock With {
        '        .NumeroMouvement = GenererNumeroMouvement(),
        '        .ProduitId = produitId,
        '        .TypeMouvement = typeMouvement,
        '        .Quantite = quantiteSaisie,
        '        .QuantiteBase = quantiteBase,
        '        .Unite = unite,
        '        .StockAvant = stockActuel,
        '        .StockApres = stockApres,
        '        .Reference = reference,
        '        .Observation = If(observation, ""),
        '        .TypePerte = typePerte,
        '        .EffectuePar = effectuePar
        '    }
        '    Return _mvtRepo.Ajouter(mouvement)
        'End Function

        'Private Function ObtenirInfosProduit(produitId As Integer) As DataRow
        '    Dim sql As String = "SELECT ProduitId, Libelle, CategorieId, UnitePrincipale, UniteSecondaire, ConversionUnite, PrixAchat, PrixDetail, PrixGros FROM Produits WHERE ProduitId=@id"
        '    Dim p As New List(Of SqlParameter) From {New SqlParameter("@id", produitId)}
        '    Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
        '    If dt.Rows.Count = 0 Then Throw New Exception("Produit introuvable.")
        '    Return dt.Rows(0)
        'End Function

        'Private Function ObtenirStockActuel(produitId As Integer) As Decimal
        '    Dim sql As String = "SELECT ISNULL(SUM(q), 0) FROM (" &
        '                       "SELECT SUM(QuantiteBase) as q FROM StockEntree WHERE ProduitId = @id " &
        '                       "UNION ALL SELECT -SUM(QuantiteBase) FROM StockSortie WHERE ProduitId = @id " &
        '                       "UNION ALL SELECT -SUM(QuantiteBase) FROM StockPerte WHERE ProduitId = @id) t"
        '    Dim p As New List(Of SqlParameter) From {New SqlParameter("@id", produitId)}
        '    Return Convert.ToDecimal(_dal.ExecuterScalaire(sql, CommandType.Text, p))
        'End Function

        'Private Function ConvertirEnBase(info As DataRow, unite As String, quantite As Decimal) As Decimal
        '    Dim conversion As Decimal = If(IsDBNull(info("ConversionUnite")), 1D, Convert.ToDecimal(info("ConversionUnite")))
        '    If conversion <= 0D Then conversion = 1D
        '    Dim unitePrincipale As String = If(IsDBNull(info("UnitePrincipale")), "", info("UnitePrincipale").ToString().ToLower())
        '    If unite.ToLower() = unitePrincipale OrElse unite.ToLower() = "base" OrElse unite.ToLower() = "carton" OrElse unite.ToLower() = "sac" Then
        '        Return quantite * conversion
        '    End If
        '    Return quantite
        'End Function

        'Private Function ObtenirPrixAchat(info As DataRow) As Decimal
        '    Return If(IsDBNull(info("PrixAchat")), 0D, Convert.ToDecimal(info("PrixAchat")))
        'End Function

        'Private Function GenererNumeroStock(prefix As String) As String
        '    Return prefix & Date.Now.ToString("yyyyMMddHHmmss")
        'End Function

        'Private Function GenererNumeroMouvement() As String
        '    Return "MVT" & Date.Now.ToString("yyyyMMddHHmmss")
        'End Function

        'Private Sub AssurerVueStock()
        '    ' Logique optionnelle pour assurer l'existence de vues ou tables temporaires
        'End Sub
















    End Class

End Namespace
