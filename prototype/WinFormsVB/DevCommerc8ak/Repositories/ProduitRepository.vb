Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic
Imports System.Diagnostics

Namespace DevCommerc8ak
    Public Class ProduitRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerColonnes()
        End Sub

        Private Function ObtenirNomUtilisateurModification() As String
            Dim nom As String = Nothing
            Try
                nom = SessionUtilisateur.NomUtilisateur
            Catch
            End Try

            If String.IsNullOrWhiteSpace(nom) Then
                Return "SYSTEM"
            End If

            Return nom.Trim()
        End Function

        Private Function ObtenirIdUtilisateurModification() As Integer
            Try
                If SessionUtilisateur.UtilisateurId > 0 Then
                    Return SessionUtilisateur.UtilisateurId
                End If
            Catch
            End Try

            Return 1
        End Function

        Private Sub AssurerColonnes()
            ' Schéma géré par le script SQL de déploiement.
        End Sub

        ' Cree un produit et retourne son identifiant.
        Public Function Ajouter(produit As Produit) As Integer
            Dim modifierPar As String = ObtenirNomUtilisateurModification()
            If String.IsNullOrWhiteSpace(modifierPar) Then
                modifierPar = "SYSTEM"
            End If

            Dim sql As String = "INSERT INTO Produits (CodeBarres, Libelle, PrixDetail, PrixAchat, PrixDemi, PrixQuart, PrixDouzaine, PrixGros, PrixSpecial, CoefficientGros, SeuilCritique, DateExpiration, CategorieId, UnitePrincipale, UniteSecondaire, ConversionUnite, TypeGestionStock, UniteMesureStock, ContenuUnitePrincipale, ContenuUniteSecondaire, EstActif, VenteDetail, VenteDemi, VenteDouzaine, VenteGros, ModifierPar) " &
                                "VALUES (@CodeBarres, @Libelle, @PrixDetail, @PrixAchat, @PrixDemi, @PrixQuart, @PrixDouzaine, @PrixGros, @PrixSpecial, @CoefficientGros, @SeuilCritique, @DateExpiration, @CategorieId, @UnitePrincipale, @UniteSecondaire, @ConversionUnite, @TypeGestionStock, @UniteMesureStock, @ContenuUnitePrincipale, @ContenuUniteSecondaire, @EstActif, @VenteDetail, @VenteDemi, @VenteDouzaine, @VenteGros, @ModifierPar); " &
                                "SELECT CAST(SCOPE_IDENTITY() AS INT);"

            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@CodeBarres", If(String.IsNullOrWhiteSpace(produit.CodeBarres), CType(DBNull.Value, Object), produit.CodeBarres.Trim())),
                New SqlParameter("@Libelle", produit.Libelle),
                New SqlParameter("@PrixDetail", produit.PrixDetail),
                New SqlParameter("@PrixAchat", produit.PrixAchat),
                New SqlParameter("@PrixDemi", produit.PrixDemi),
                New SqlParameter("@PrixQuart", produit.PrixQuart),
                New SqlParameter("@PrixDouzaine", produit.PrixDouzaine),
                New SqlParameter("@PrixGros", produit.PrixGros),
                New SqlParameter("@PrixSpecial", produit.PrixSpecial),
                New SqlParameter("@CoefficientGros", produit.CoefficientGros),
                New SqlParameter("@SeuilCritique", produit.SeuilCritique),
                New SqlParameter("@DateExpiration", If(produit.DateExpiration.HasValue, CType(produit.DateExpiration.Value, Object), DBNull.Value)),
                New SqlParameter("@CategorieId", If(produit.CategorieId.HasValue, CType(produit.CategorieId.Value, Object), DBNull.Value)),
                New SqlParameter("@UnitePrincipale", If(produit.UnitePrincipale, CType(DBNull.Value, Object))),
                New SqlParameter("@UniteSecondaire", If(produit.UniteSecondaire, CType(DBNull.Value, Object))),
                New SqlParameter("@ConversionUnite", produit.ConversionUnite),
                New SqlParameter("@TypeGestionStock", NormaliserTypeGestionStock(produit.TypeGestionStock)),
                New SqlParameter("@UniteMesureStock", If(String.IsNullOrWhiteSpace(produit.UniteMesureStock), CType(DBNull.Value, Object), produit.UniteMesureStock.Trim().ToUpperInvariant())),
                New SqlParameter("@ContenuUnitePrincipale", ObtenirContenuPrincipal(produit)),
                New SqlParameter("@ContenuUniteSecondaire", If(produit.ContenuUniteSecondaire.HasValue AndAlso produit.ContenuUniteSecondaire.Value > 0D, CType(produit.ContenuUniteSecondaire.Value, Object), DBNull.Value)),
                New SqlParameter("@EstActif", produit.EstActif),
                New SqlParameter("@VenteDetail", produit.VenteDetail),
                New SqlParameter("@VenteDemi", produit.VenteDemi),
                New SqlParameter("@VenteDouzaine", produit.VenteDouzaine),
                New SqlParameter("@VenteGros", produit.VenteGros),
                New SqlParameter("@ModifierPar", modifierPar)
            }

            Dim id As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(id)
        End Function

        ' Retourne la liste des produits.
        Public Function Lister() As List(Of ProduitDTO)
            Dim sql As String = "SELECT p.ProduitId, p.CodeBarres, p.Libelle, p.PrixDetail, p.PrixAchat, p.PrixDemi, p.PrixQuart, p.PrixDouzaine, p.PrixGros, p.PrixSpecial, p.CoefficientGros, " &
                                "ISNULL(s.QuantiteStock,0) AS QuantiteStock, p.SeuilCritique, p.DateExpiration, p.CategorieId, ISNULL(cat.NomCategorie, '') AS NomCategorie, p.EstActif, p.UnitePrincipale, p.UniteSecondaire, p.ConversionUnite, " &
                                "p.VenteDetail, p.VenteDemi, p.VenteDouzaine, p.VenteGros, ISNULL(p.TypeGestionStock,'UNITE') AS TypeGestionStock, ISNULL(p.UniteMesureStock,'PIECE') AS UniteMesureStock, ISNULL(p.ContenuUnitePrincipale, ISNULL(p.ConversionUnite,1)) AS ContenuUnitePrincipale, p.ContenuUniteSecondaire " &
                                "FROM Produits p LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId " &
                                "LEFT JOIN CategoriesProduits cat ON cat.CategorieId = p.CategorieId"
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, Nothing)
            Dim liste As New List(Of ProduitDTO)()

            For Each row As DataRow In dt.Rows
                liste.Add(MapVersDTO(row))
            Next

            Return liste
        End Function

        ' Retourne la liste des produits sous forme de DataTable pour filtrage local.
        Public Function ListerTable() As DataTable
            Dim sql As String = "SELECT p.ProduitId, p.CodeBarres, p.Libelle, p.PrixDetail, p.PrixAchat, p.PrixDemi, p.PrixQuart, p.PrixDouzaine, p.PrixGros, p.PrixSpecial, p.CoefficientGros, " &
                                "ISNULL(s.QuantiteStock,0) AS QuantiteStock, p.SeuilCritique, p.DateExpiration, p.CategorieId, ISNULL(cat.NomCategorie, '') AS NomCategorie, p.EstActif, p.UnitePrincipale, p.UniteSecondaire, p.ConversionUnite, " &
                                "p.VenteDetail, p.VenteDemi, p.VenteDouzaine, p.VenteGros, ISNULL(p.TypeGestionStock,'UNITE') AS TypeGestionStock, ISNULL(p.UniteMesureStock,'PIECE') AS UniteMesureStock, ISNULL(p.ContenuUnitePrincipale, ISNULL(p.ConversionUnite,1)) AS ContenuUnitePrincipale, p.ContenuUniteSecondaire " &
                                "FROM Produits p LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId " &
                                "LEFT JOIN CategoriesProduits cat ON cat.CategorieId = p.CategorieId"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function


        Public Function ListerQteProduit(produitId As Integer) As Decimal
            Dim sql As String = "
                                SELECT CAST(ISNULL(s.QuantiteStock, 0) AS DECIMAL(18,3)) AS QuantiteStock
                                FROM Produits p LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId
								where p.ProduitId=@ProduitId"

            Dim p As New List(Of SqlParameter) From {New SqlParameter("@ProduitId", produitId)}
            Dim id As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToDecimal(id)
        End Function

        ' Retourne la liste des tepes ventes et prix produits sous forme de DataTable pour filtrage local.
        Public Function ListerTypeVente(produitId As Integer) As DataTable
            Dim sql As String = "
                SELECT TypeVente, Prix
                FROM (
                    SELECT 'Détail' AS TypeVente, PrixDetail AS Prix, VenteDetail AS EstActif
                    FROM Produits
                    WHERE ProduitId = @ProduitId
                    UNION ALL
                    SELECT 'Demi', PrixDemi, VenteDemi
                    FROM Produits
                    WHERE ProduitId = @ProduitId
                    UNION ALL
                    SELECT 'Quart', PrixQuart, 1
                    FROM Produits
                    WHERE ProduitId = @ProduitId
                    UNION ALL
                    SELECT 'Gros', PrixGros, VenteGros
                    FROM Produits
                    WHERE ProduitId = @ProduitId
                    UNION ALL
                    SELECT 'Douzaine', PrixDouzaine, VenteDouzaine
                    FROM Produits
                    WHERE ProduitId = @ProduitId
                ) T
                WHERE EstActif = 1"

            Dim p As New List(Of SqlParameter) From {New SqlParameter("@ProduitId", produitId)}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            If dt.Rows.Count = 0 Then
                Return Nothing
            End If
            Return dt
        End Function

        ' Recherche par code-barres ou libelle.
        Public Function Rechercher(texte As String) As List(Of ProduitDTO)
            Dim sql As String = "SELECT p.ProduitId, p.CodeBarres, p.Libelle, p.PrixDetail, p.PrixAchat, p.PrixDemi, p.PrixQuart, p.PrixDouzaine, p.PrixGros, p.PrixSpecial, p.CoefficientGros, " &
                                "ISNULL(s.QuantiteStock,0) AS QuantiteStock, p.SeuilCritique, p.DateExpiration, p.CategorieId, ISNULL(cat.NomCategorie, '') AS NomCategorie, p.EstActif, p.UnitePrincipale, p.UniteSecondaire, p.ConversionUnite, " &
                                "p.VenteDetail, p.VenteDemi, p.VenteDouzaine, p.VenteGros, ISNULL(p.TypeGestionStock,'UNITE') AS TypeGestionStock, ISNULL(p.UniteMesureStock,'PIECE') AS UniteMesureStock, ISNULL(p.ContenuUnitePrincipale, ISNULL(p.ConversionUnite,1)) AS ContenuUnitePrincipale, p.ContenuUniteSecondaire " &
                                "FROM Produits p LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId " &
                                "LEFT JOIN CategoriesProduits cat ON cat.CategorieId = p.CategorieId " &
                                "WHERE p.CodeBarres LIKE @q OR p.Libelle LIKE @q"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@q", "%" & texte & "%")}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            Dim liste As New List(Of ProduitDTO)()
            For Each row As DataRow In dt.Rows
                liste.Add(MapVersDTO(row))
            Next
            Return liste
        End Function

        ' Retourne un produit par identifiant.
        Public Function ObtenirParId(produitId As Integer) As ProduitDTO
            Dim sql As String = "SELECT p.ProduitId, p.CodeBarres, p.Libelle, p.PrixDetail, p.PrixAchat, p.PrixDemi, p.PrixQuart, p.PrixDouzaine, p.PrixGros, p.PrixSpecial, p.CoefficientGros, " &
                                "ISNULL(s.QuantiteStock,0) AS QuantiteStock, p.SeuilCritique, p.DateExpiration, p.CategorieId, ISNULL(cat.NomCategorie, '') AS NomCategorie, p.EstActif, p.UnitePrincipale, p.UniteSecondaire, p.ConversionUnite, " &
                                "p.VenteDetail, p.VenteDemi, p.VenteDouzaine, p.VenteGros, ISNULL(p.TypeGestionStock,'UNITE') AS TypeGestionStock, ISNULL(p.UniteMesureStock,'PIECE') AS UniteMesureStock, ISNULL(p.ContenuUnitePrincipale, ISNULL(p.ConversionUnite,1)) AS ContenuUnitePrincipale, p.ContenuUniteSecondaire " &
                                "FROM Produits p LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId LEFT JOIN CategoriesProduits cat ON cat.CategorieId = p.CategorieId WHERE p.ProduitId = @ProduitId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@ProduitId", produitId)}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            If dt.Rows.Count = 0 Then
                Return Nothing
            End If
            Return MapVersDTO(dt.Rows(0))
        End Function

        ' Met a jour un produit.
        Public Function MettreAJour(produit As Produit) As Integer
            AssurerColonnes()
            Dim ancienPrixAchat As Decimal = 0D
            Dim ancienPrixDetail As Decimal = 0D
            Dim ancienPrixDemi As Decimal = 0D
            Dim ancienPrixQuart As Decimal = 0D
            Dim ancienPrixDouzaine As Decimal = 0D
            Dim ancienPrixGros As Decimal = 0D
            Dim ancienPrixSpecial As Decimal = 0D
            Dim modifierPar As String = ObtenirNomUtilisateurModification()
            If String.IsNullOrWhiteSpace(modifierPar) Then
                modifierPar = "SYSTEM"
            End If
            Dim sqlOld As String = "SELECT PrixAchat, PrixDetail, PrixDemi, PrixQuart, PrixDouzaine, PrixGros, PrixSpecial FROM Produits WHERE ProduitId=@ProduitId"
            Dim pOld As New List(Of SqlParameter) From {New SqlParameter("@ProduitId", produit.ProduitId)}
            Dim dtOld As DataTable = _dal.ExecuterTable(sqlOld, CommandType.Text, pOld)
            If dtOld.Rows.Count > 0 Then
                ancienPrixAchat = Convert.ToDecimal(dtOld.Rows(0)("PrixAchat"))
                ancienPrixDetail = Convert.ToDecimal(dtOld.Rows(0)("PrixDetail"))
                ancienPrixDemi = Convert.ToDecimal(dtOld.Rows(0)("PrixDemi"))
                ancienPrixQuart = Convert.ToDecimal(dtOld.Rows(0)("PrixQuart"))
                ancienPrixDouzaine = Convert.ToDecimal(dtOld.Rows(0)("PrixDouzaine"))
                ancienPrixGros = Convert.ToDecimal(dtOld.Rows(0)("PrixGros"))
                ancienPrixSpecial = Convert.ToDecimal(dtOld.Rows(0)("PrixSpecial"))
            End If

            Dim sql As String = "UPDATE Produits SET CodeBarres=@CodeBarres, Libelle=@Libelle, PrixDetail=@PrixDetail, PrixAchat=@PrixAchat, PrixDemi=@PrixDemi, PrixQuart=@PrixQuart, PrixDouzaine=@PrixDouzaine, PrixGros=@PrixGros, PrixSpecial=@PrixSpecial, CoefficientGros=@CoefficientGros, " &
                                "SeuilCritique=@SeuilCritique, DateExpiration=@DateExpiration, CategorieId=@CategorieId, UnitePrincipale=@UnitePrincipale, UniteSecondaire=@UniteSecondaire, ConversionUnite=@ConversionUnite, TypeGestionStock=@TypeGestionStock, UniteMesureStock=@UniteMesureStock, ContenuUnitePrincipale=@ContenuUnitePrincipale, ContenuUniteSecondaire=@ContenuUniteSecondaire, EstActif=@EstActif, " &
                                "VenteDetail=@VenteDetail, VenteDemi=@VenteDemi, VenteDouzaine=@VenteDouzaine, VenteGros=@VenteGros, ModifierPar=@ModifierPar, ModifieLe=GETDATE() " &
                                "WHERE ProduitId=@ProduitId"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@CodeBarres", If(String.IsNullOrWhiteSpace(produit.CodeBarres), CType(DBNull.Value, Object), produit.CodeBarres.Trim())),
                New SqlParameter("@Libelle", produit.Libelle),
                New SqlParameter("@PrixDetail", produit.PrixDetail),
                New SqlParameter("@PrixAchat", produit.PrixAchat),
                New SqlParameter("@PrixDemi", produit.PrixDemi),
                New SqlParameter("@PrixQuart", produit.PrixQuart),
                New SqlParameter("@PrixDouzaine", produit.PrixDouzaine),
                New SqlParameter("@PrixGros", produit.PrixGros),
                New SqlParameter("@PrixSpecial", produit.PrixSpecial),
                New SqlParameter("@CoefficientGros", produit.CoefficientGros),
                New SqlParameter("@SeuilCritique", produit.SeuilCritique),
                New SqlParameter("@DateExpiration", If(produit.DateExpiration.HasValue, CType(produit.DateExpiration.Value, Object), DBNull.Value)),
                New SqlParameter("@CategorieId", If(produit.CategorieId.HasValue, CType(produit.CategorieId.Value, Object), DBNull.Value)),
                New SqlParameter("@UnitePrincipale", If(produit.UnitePrincipale, CType(DBNull.Value, Object))),
                New SqlParameter("@UniteSecondaire", If(produit.UniteSecondaire, CType(DBNull.Value, Object))),
                New SqlParameter("@ConversionUnite", produit.ConversionUnite),
                New SqlParameter("@TypeGestionStock", NormaliserTypeGestionStock(produit.TypeGestionStock)),
                New SqlParameter("@UniteMesureStock", If(String.IsNullOrWhiteSpace(produit.UniteMesureStock), CType(DBNull.Value, Object), produit.UniteMesureStock.Trim().ToUpperInvariant())),
                New SqlParameter("@ContenuUnitePrincipale", ObtenirContenuPrincipal(produit)),
                New SqlParameter("@ContenuUniteSecondaire", If(produit.ContenuUniteSecondaire.HasValue AndAlso produit.ContenuUniteSecondaire.Value > 0D, CType(produit.ContenuUniteSecondaire.Value, Object), DBNull.Value)),
                New SqlParameter("@EstActif", produit.EstActif),
                New SqlParameter("@VenteDetail", produit.VenteDetail),
                New SqlParameter("@VenteDemi", produit.VenteDemi),
                New SqlParameter("@VenteDouzaine", produit.VenteDouzaine),
                New SqlParameter("@VenteGros", produit.VenteGros),
                New SqlParameter("@ProduitId", produit.ProduitId),
                New SqlParameter("@ModifierPar", modifierPar)
            }

            Debug.WriteLine(String.Format(Globalization.CultureInfo.InvariantCulture, "[ProduitRepository] UPDATE Produits ProduitId={0}, PrixAchat={1}, PrixGros={2}, PrixDemi={3}, PrixDetail={4}, PrixQuart={5}, PrixDouzaine={6}, PrixSpecial={7}, CoefficientGros={8}, UnitePrincipale={9}, UniteSecondaire={10}, ConversionUnite={11}, VenteGros={12}, VenteDemi={13}, VenteDetail={14}, VenteDouzaine={15}",
                                         produit.ProduitId,
                                         produit.PrixAchat,
                                         produit.PrixGros,
                                         produit.PrixDemi,
                                         produit.PrixDetail,
                                         produit.PrixQuart,
                                         produit.PrixDouzaine,
                                         produit.PrixSpecial,
                                         produit.CoefficientGros,
                                         If(produit.UnitePrincipale, String.Empty),
                                         If(produit.UniteSecondaire, String.Empty),
                                         produit.ConversionUnite,
                                         produit.VenteGros,
                                         produit.VenteDemi,
                                         produit.VenteDetail,
                                         produit.VenteDouzaine))
            Dim rows As Integer = _dal.ExecuterNonRequete(sql, CommandType.Text, p)
            Debug.WriteLine(String.Format(Globalization.CultureInfo.InvariantCulture, "[ProduitRepository] UPDATE Produits rows affected={0} pour ProduitId={1}", rows, produit.ProduitId))

            If ancienPrixAchat <> produit.PrixAchat OrElse
               ancienPrixDetail <> produit.PrixDetail OrElse
               ancienPrixDemi <> produit.PrixDemi OrElse
               ancienPrixQuart <> produit.PrixQuart OrElse
               ancienPrixDouzaine <> produit.PrixDouzaine OrElse
               ancienPrixGros <> produit.PrixGros OrElse
               ancienPrixSpecial <> produit.PrixSpecial Then

                Dim sqlHist As String = "INSERT INTO HistoriquePrixProduits (" &
                                        "ProduitId, AncienPrixAchat, NouveauPrixAchat, AncienPrixDetail, NouveauPrixDetail, AncienPrixDemi, NouveauPrixDemi, " &
                                        "AncienPrixQuart, NouveauPrixQuart, AncienPrixDouzaine, NouveauPrixDouzaine, AncienPrixGros, NouveauPrixGros, " &
                                        "AncienPrixSpecial, NouveauPrixSpecial, ModifiePar, ModifieLe, IdStock) " &
                                        "VALUES (" &
                                        "@ProduitId, @AncienPrixAchat, @NouveauPrixAchat, @AncienPrixDetail, @NouveauPrixDetail, @AncienPrixDemi, @NouveauPrixDemi, " &
                                        "@AncienPrixQuart, @NouveauPrixQuart, @AncienPrixDouzaine, @NouveauPrixDouzaine, @AncienPrixGros, @NouveauPrixGros, " &
                                        "@AncienPrixSpecial, @NouveauPrixSpecial, 1, GETDATE(), @IdStock)"
                Dim pHist As New List(Of SqlParameter) From {
                    New SqlParameter("@ProduitId", produit.ProduitId),
                    New SqlParameter("@AncienPrixAchat", ancienPrixAchat),
                    New SqlParameter("@NouveauPrixAchat", produit.PrixAchat),
                    New SqlParameter("@AncienPrixDetail", ancienPrixDetail),
                    New SqlParameter("@NouveauPrixDetail", produit.PrixDetail),
                    New SqlParameter("@AncienPrixDemi", ancienPrixDemi),
                    New SqlParameter("@NouveauPrixDemi", produit.PrixDemi),
                    New SqlParameter("@AncienPrixQuart", ancienPrixQuart),
                    New SqlParameter("@NouveauPrixQuart", produit.PrixQuart),
                    New SqlParameter("@AncienPrixDouzaine", ancienPrixDouzaine),
                    New SqlParameter("@NouveauPrixDouzaine", produit.PrixDouzaine),
                    New SqlParameter("@AncienPrixGros", ancienPrixGros),
                    New SqlParameter("@NouveauPrixGros", produit.PrixGros),
                    New SqlParameter("@AncienPrixSpecial", ancienPrixSpecial),
                    New SqlParameter("@NouveauPrixSpecial", produit.PrixSpecial),
                    New SqlParameter("@IdStock", DBNull.Value)
                }
                _dal.ExecuterNonRequete(sqlHist, CommandType.Text, pHist)
            End If

            Return rows
        End Function

        Public Function MettreAJourAvecMigrationUniteVersMesure(produit As Produit, ancienStockBase As Decimal, nouveauStockBase As Decimal, effectuePar As Integer) As Integer
            AssurerColonnes()

            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        Dim ancienPrixAchat As Decimal = 0D
                        Dim ancienPrixDetail As Decimal = 0D
                        Dim ancienPrixDemi As Decimal = 0D
                        Dim ancienPrixQuart As Decimal = 0D
                        Dim ancienPrixDouzaine As Decimal = 0D
                        Dim ancienPrixGros As Decimal = 0D
                        Dim ancienPrixSpecial As Decimal = 0D
                        Dim ancienTypeGestion As String = "UNITE"
                        Dim ancienneConversionUnite As Decimal = 1D

                        Using cmdOld As New SqlCommand("SELECT PrixAchat, PrixDetail, PrixDemi, PrixQuart, PrixDouzaine, PrixGros, PrixSpecial, ISNULL(TypeGestionStock,'UNITE') AS TypeGestionStock, ISNULL(ConversionUnite, 1) AS ConversionUnite FROM Produits WITH (UPDLOCK, HOLDLOCK) WHERE ProduitId=@ProduitId", cn, tx)
                            cmdOld.Parameters.AddWithValue("@ProduitId", produit.ProduitId)
                            Using r As SqlDataReader = cmdOld.ExecuteReader()
                                If Not r.Read() Then
                                    Throw New Exception("Produit introuvable.")
                                End If
                                ancienPrixAchat = If(r.IsDBNull(r.GetOrdinal("PrixAchat")), 0D, Convert.ToDecimal(r("PrixAchat")))
                                ancienPrixDetail = If(r.IsDBNull(r.GetOrdinal("PrixDetail")), 0D, Convert.ToDecimal(r("PrixDetail")))
                                ancienPrixDemi = If(r.IsDBNull(r.GetOrdinal("PrixDemi")), 0D, Convert.ToDecimal(r("PrixDemi")))
                                ancienPrixQuart = If(r.IsDBNull(r.GetOrdinal("PrixQuart")), 0D, Convert.ToDecimal(r("PrixQuart")))
                                ancienPrixDouzaine = If(r.IsDBNull(r.GetOrdinal("PrixDouzaine")), 0D, Convert.ToDecimal(r("PrixDouzaine")))
                                ancienPrixGros = If(r.IsDBNull(r.GetOrdinal("PrixGros")), 0D, Convert.ToDecimal(r("PrixGros")))
                                ancienPrixSpecial = If(r.IsDBNull(r.GetOrdinal("PrixSpecial")), 0D, Convert.ToDecimal(r("PrixSpecial")))
                                ancienTypeGestion = StockUnitConversionService.NormaliserTypeGestionStock(Convert.ToString(r("TypeGestionStock")))
                                ancienneConversionUnite = If(r.IsDBNull(r.GetOrdinal("ConversionUnite")), 1D, Convert.ToDecimal(r("ConversionUnite")))
                            End Using
                        End Using

                        If Not String.Equals(ancienTypeGestion, "UNITE", StringComparison.OrdinalIgnoreCase) OrElse
                           Not StockUnitConversionService.EstGestionMesuree(produit.TypeGestionStock) Then
                            Throw New Exception("La migration de stock demandée n'est autorisée que de UNITE vers MESURE.")
                        End If

                        Dim stockCourantBase As Decimal = ObtenirStockCourantTransaction(produit.ProduitId, cn, tx)
                        Dim conversionCourante As Decimal = If(ancienneConversionUnite > 0D, ancienneConversionUnite, 1D)
                        ancienStockBase = stockCourantBase
                        nouveauStockBase = (stockCourantBase / conversionCourante) * ObtenirContenuPrincipal(produit)

                        Dim rows As Integer = MettreAJourProduitTransaction(produit, cn, tx)
                        Dim delta As Decimal = nouveauStockBase - ancienStockBase
                        If delta <> 0D Then
                            AjouterAjustementMigrationStock(produit, ancienStockBase, nouveauStockBase, delta, effectuePar, cn, tx)
                        End If

                        InsererHistoriquePrixSiNecessaire(
                            produit,
                            ancienPrixAchat,
                            ancienPrixDetail,
                            ancienPrixDemi,
                            ancienPrixQuart,
                            ancienPrixDouzaine,
                            ancienPrixGros,
                            ancienPrixSpecial,
                            cn,
                            tx)

                        tx.Commit()
                        Return rows
                    Catch
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Private Function ObtenirStockCourantTransaction(produitId As Integer, cn As SqlConnection, tx As SqlTransaction) As Decimal
            Using cmd As New SqlCommand("SELECT ISNULL(QuantiteStock, 0) FROM vStockProduit WITH (UPDLOCK, HOLDLOCK) WHERE ProduitId=@ProduitId", cn, tx)
                cmd.Parameters.AddWithValue("@ProduitId", produitId)
                Dim valeur As Object = cmd.ExecuteScalar()
                If valeur Is Nothing OrElse valeur Is DBNull.Value Then Return 0D
                Return Convert.ToDecimal(valeur)
            End Using
        End Function

        Private Function MettreAJourProduitTransaction(produit As Produit, cn As SqlConnection, tx As SqlTransaction) As Integer
            Dim sql As String = "UPDATE Produits SET CodeBarres=@CodeBarres, Libelle=@Libelle, PrixDetail=@PrixDetail, PrixAchat=@PrixAchat, PrixDemi=@PrixDemi, PrixQuart=@PrixQuart, PrixDouzaine=@PrixDouzaine, PrixGros=@PrixGros, PrixSpecial=@PrixSpecial, CoefficientGros=@CoefficientGros, " &
                                "SeuilCritique=@SeuilCritique, DateExpiration=@DateExpiration, CategorieId=@CategorieId, UnitePrincipale=@UnitePrincipale, UniteSecondaire=@UniteSecondaire, ConversionUnite=@ConversionUnite, TypeGestionStock=@TypeGestionStock, UniteMesureStock=@UniteMesureStock, ContenuUnitePrincipale=@ContenuUnitePrincipale, ContenuUniteSecondaire=@ContenuUniteSecondaire, EstActif=@EstActif, " &
                                "VenteDetail=@VenteDetail, VenteDemi=@VenteDemi, VenteDouzaine=@VenteDouzaine, VenteGros=@VenteGros, ModifierPar=@ModifierPar, ModifieLe=GETDATE() " &
                                "WHERE ProduitId=@ProduitId"

            Using cmd As New SqlCommand(sql, cn, tx)
                AjouterParametresProduit(cmd, produit)
                Return cmd.ExecuteNonQuery()
            End Using
        End Function

        Private Sub AjouterParametresProduit(cmd As SqlCommand, produit As Produit)
            Dim modifierPar As String = ObtenirNomUtilisateurModification()
            If String.IsNullOrWhiteSpace(modifierPar) Then modifierPar = "SYSTEM"

            cmd.Parameters.AddWithValue("@CodeBarres", If(String.IsNullOrWhiteSpace(produit.CodeBarres), CType(DBNull.Value, Object), produit.CodeBarres.Trim()))
            cmd.Parameters.AddWithValue("@Libelle", produit.Libelle)
            cmd.Parameters.AddWithValue("@PrixDetail", produit.PrixDetail)
            cmd.Parameters.AddWithValue("@PrixAchat", produit.PrixAchat)
            cmd.Parameters.AddWithValue("@PrixDemi", produit.PrixDemi)
            cmd.Parameters.AddWithValue("@PrixQuart", produit.PrixQuart)
            cmd.Parameters.AddWithValue("@PrixDouzaine", produit.PrixDouzaine)
            cmd.Parameters.AddWithValue("@PrixGros", produit.PrixGros)
            cmd.Parameters.AddWithValue("@PrixSpecial", produit.PrixSpecial)
            cmd.Parameters.AddWithValue("@CoefficientGros", produit.CoefficientGros)
            cmd.Parameters.AddWithValue("@SeuilCritique", produit.SeuilCritique)
            cmd.Parameters.AddWithValue("@DateExpiration", If(produit.DateExpiration.HasValue, CType(produit.DateExpiration.Value, Object), DBNull.Value))
            cmd.Parameters.AddWithValue("@CategorieId", If(produit.CategorieId.HasValue, CType(produit.CategorieId.Value, Object), DBNull.Value))
            cmd.Parameters.AddWithValue("@UnitePrincipale", If(produit.UnitePrincipale, CType(DBNull.Value, Object)))
            cmd.Parameters.AddWithValue("@UniteSecondaire", If(produit.UniteSecondaire, CType(DBNull.Value, Object)))
            cmd.Parameters.AddWithValue("@ConversionUnite", produit.ConversionUnite)
            cmd.Parameters.AddWithValue("@TypeGestionStock", NormaliserTypeGestionStock(produit.TypeGestionStock))
            cmd.Parameters.AddWithValue("@UniteMesureStock", If(String.IsNullOrWhiteSpace(produit.UniteMesureStock), CType(DBNull.Value, Object), produit.UniteMesureStock.Trim().ToUpperInvariant()))
            cmd.Parameters.AddWithValue("@ContenuUnitePrincipale", ObtenirContenuPrincipal(produit))
            cmd.Parameters.AddWithValue("@ContenuUniteSecondaire", If(produit.ContenuUniteSecondaire.HasValue AndAlso produit.ContenuUniteSecondaire.Value > 0D, CType(produit.ContenuUniteSecondaire.Value, Object), DBNull.Value))
            cmd.Parameters.AddWithValue("@EstActif", produit.EstActif)
            cmd.Parameters.AddWithValue("@VenteDetail", produit.VenteDetail)
            cmd.Parameters.AddWithValue("@VenteDemi", produit.VenteDemi)
            cmd.Parameters.AddWithValue("@VenteDouzaine", produit.VenteDouzaine)
            cmd.Parameters.AddWithValue("@VenteGros", produit.VenteGros)
            cmd.Parameters.AddWithValue("@ProduitId", produit.ProduitId)
            cmd.Parameters.AddWithValue("@ModifierPar", modifierPar)
        End Sub

        Private Sub AjouterAjustementMigrationStock(produit As Produit, ancienStockBase As Decimal, nouveauStockBase As Decimal, delta As Decimal, effectuePar As Integer, cn As SqlConnection, tx As SqlTransaction)
            Dim reference As String = "MIG-UM-" & DateTime.Now.ToString("yyyyMMddHHmmssfff")
            Dim observation As String = "Migration stock UNITE vers MESURE. Ancien=" & ancienStockBase.ToString(Globalization.CultureInfo.InvariantCulture) & ", Nouveau=" & nouveauStockBase.ToString(Globalization.CultureInfo.InvariantCulture)

            If delta > 0D Then
                Using cmdEntree As New SqlCommand("INSERT INTO StockEntree (IdStock, ProduitId, QuantiteSaisie, Unite, QuantiteBase, PrixAchat, Devise, Taux, DateEntree, FournisseurId, CreePar) VALUES (@IdStock, @ProduitId, @QuantiteSaisie, @Unite, @QuantiteBase, @PrixAchat, @Devise, @Taux, GETDATE(), NULL, @CreePar)", cn, tx)
                    cmdEntree.Parameters.AddWithValue("@IdStock", reference)
                    cmdEntree.Parameters.AddWithValue("@ProduitId", produit.ProduitId)
                    cmdEntree.Parameters.AddWithValue("@QuantiteSaisie", delta)
                    cmdEntree.Parameters.AddWithValue("@Unite", If(String.IsNullOrWhiteSpace(produit.UniteMesureStock), "base", produit.UniteMesureStock))
                    cmdEntree.Parameters.AddWithValue("@QuantiteBase", delta)
                    cmdEntree.Parameters.AddWithValue("@PrixAchat", produit.PrixAchat)
                    cmdEntree.Parameters.AddWithValue("@Devise", "CDF")
                    cmdEntree.Parameters.AddWithValue("@Taux", 0D)
                    cmdEntree.Parameters.AddWithValue("@CreePar", effectuePar)
                    cmdEntree.ExecuteNonQuery()
                End Using
            Else
                Using cmdSortie As New SqlCommand("INSERT INTO StockSortie (ProduitId, QuantiteSaisie, Unite, QuantiteBase, DateSortie, Source, RefSource, CreePar, NumeroSortie, StatutPaiement, MontantLigne, MontantPaye, ResteAPayer, Observation) VALUES (@ProduitId, @QuantiteSaisie, @Unite, @QuantiteBase, GETDATE(), @Source, @RefSource, @CreePar, @NumeroSortie, @StatutPaiement, 0, 0, 0, @Observation)", cn, tx)
                    cmdSortie.Parameters.AddWithValue("@ProduitId", produit.ProduitId)
                    cmdSortie.Parameters.AddWithValue("@QuantiteSaisie", Math.Abs(delta))
                    cmdSortie.Parameters.AddWithValue("@Unite", If(String.IsNullOrWhiteSpace(produit.UniteMesureStock), "base", produit.UniteMesureStock))
                    cmdSortie.Parameters.AddWithValue("@QuantiteBase", Math.Abs(delta))
                    cmdSortie.Parameters.AddWithValue("@Source", "MIGRATION_UNITE_MESURE")
                    cmdSortie.Parameters.AddWithValue("@RefSource", reference)
                    cmdSortie.Parameters.AddWithValue("@CreePar", effectuePar)
                    cmdSortie.Parameters.AddWithValue("@NumeroSortie", reference)
                    cmdSortie.Parameters.AddWithValue("@StatutPaiement", "GRATUIT")
                    cmdSortie.Parameters.AddWithValue("@Observation", observation)
                    cmdSortie.ExecuteNonQuery()
                End Using
            End If

            Using cmdMouvement As New SqlCommand("INSERT INTO MouvementsStock (NumeroMouvement, ProduitId, TypeMouvement, Quantite, QuantiteBase, Unite, StockAvant, StockApres, Reference, Observation, EffectuePar, ModifierPar) VALUES (@NumeroMouvement, @ProduitId, @TypeMouvement, @Quantite, @QuantiteBase, @Unite, @StockAvant, @StockApres, @Reference, @Observation, @EffectuePar, @ModifierPar)", cn, tx)
                cmdMouvement.Parameters.AddWithValue("@NumeroMouvement", reference)
                cmdMouvement.Parameters.AddWithValue("@ProduitId", produit.ProduitId)
                cmdMouvement.Parameters.AddWithValue("@TypeMouvement", "MIGRATION_UNITE_MESURE")
                cmdMouvement.Parameters.AddWithValue("@Quantite", Math.Abs(delta))
                cmdMouvement.Parameters.AddWithValue("@QuantiteBase", Math.Abs(delta))
                cmdMouvement.Parameters.AddWithValue("@Unite", If(String.IsNullOrWhiteSpace(produit.UniteMesureStock), "base", produit.UniteMesureStock))
                cmdMouvement.Parameters.AddWithValue("@StockAvant", ancienStockBase)
                cmdMouvement.Parameters.AddWithValue("@StockApres", nouveauStockBase)
                cmdMouvement.Parameters.AddWithValue("@Reference", reference)
                cmdMouvement.Parameters.AddWithValue("@Observation", observation)
                cmdMouvement.Parameters.AddWithValue("@EffectuePar", effectuePar)
                cmdMouvement.Parameters.AddWithValue("@ModifierPar", ObtenirNomUtilisateurModification())
                cmdMouvement.ExecuteNonQuery()
            End Using
        End Sub

        Private Sub InsererHistoriquePrixSiNecessaire(produit As Produit,
                                                       ancienPrixAchat As Decimal,
                                                       ancienPrixDetail As Decimal,
                                                       ancienPrixDemi As Decimal,
                                                       ancienPrixQuart As Decimal,
                                                       ancienPrixDouzaine As Decimal,
                                                       ancienPrixGros As Decimal,
                                                       ancienPrixSpecial As Decimal,
                                                       cn As SqlConnection,
                                                       tx As SqlTransaction)
            If ancienPrixAchat = produit.PrixAchat AndAlso
               ancienPrixDetail = produit.PrixDetail AndAlso
               ancienPrixDemi = produit.PrixDemi AndAlso
               ancienPrixQuart = produit.PrixQuart AndAlso
               ancienPrixDouzaine = produit.PrixDouzaine AndAlso
               ancienPrixGros = produit.PrixGros AndAlso
               ancienPrixSpecial = produit.PrixSpecial Then Return

            Using cmdHist As New SqlCommand("INSERT INTO HistoriquePrixProduits (ProduitId, AncienPrixAchat, NouveauPrixAchat, AncienPrixDetail, NouveauPrixDetail, AncienPrixDemi, NouveauPrixDemi, AncienPrixQuart, NouveauPrixQuart, AncienPrixDouzaine, NouveauPrixDouzaine, AncienPrixGros, NouveauPrixGros, AncienPrixSpecial, NouveauPrixSpecial, ModifiePar, ModifieLe, IdStock) VALUES (@ProduitId, @AncienPrixAchat, @NouveauPrixAchat, @AncienPrixDetail, @NouveauPrixDetail, @AncienPrixDemi, @NouveauPrixDemi, @AncienPrixQuart, @NouveauPrixQuart, @AncienPrixDouzaine, @NouveauPrixDouzaine, @AncienPrixGros, @NouveauPrixGros, @AncienPrixSpecial, @NouveauPrixSpecial, 1, GETDATE(), @IdStock)", cn, tx)
                cmdHist.Parameters.AddWithValue("@ProduitId", produit.ProduitId)
                cmdHist.Parameters.AddWithValue("@AncienPrixAchat", ancienPrixAchat)
                cmdHist.Parameters.AddWithValue("@NouveauPrixAchat", produit.PrixAchat)
                cmdHist.Parameters.AddWithValue("@AncienPrixDetail", ancienPrixDetail)
                cmdHist.Parameters.AddWithValue("@NouveauPrixDetail", produit.PrixDetail)
                cmdHist.Parameters.AddWithValue("@AncienPrixDemi", ancienPrixDemi)
                cmdHist.Parameters.AddWithValue("@NouveauPrixDemi", produit.PrixDemi)
                cmdHist.Parameters.AddWithValue("@AncienPrixQuart", ancienPrixQuart)
                cmdHist.Parameters.AddWithValue("@NouveauPrixQuart", produit.PrixQuart)
                cmdHist.Parameters.AddWithValue("@AncienPrixDouzaine", ancienPrixDouzaine)
                cmdHist.Parameters.AddWithValue("@NouveauPrixDouzaine", produit.PrixDouzaine)
                cmdHist.Parameters.AddWithValue("@AncienPrixGros", ancienPrixGros)
                cmdHist.Parameters.AddWithValue("@NouveauPrixGros", produit.PrixGros)
                cmdHist.Parameters.AddWithValue("@AncienPrixSpecial", ancienPrixSpecial)
                cmdHist.Parameters.AddWithValue("@NouveauPrixSpecial", produit.PrixSpecial)
                cmdHist.Parameters.AddWithValue("@IdStock", DBNull.Value)
                cmdHist.ExecuteNonQuery()
            End Using
        End Sub

        ' Supprime un produit.
        Public Function Supprimer(produitId As Integer) As Integer
            Dim sql As String = "DELETE FROM Produits WHERE ProduitId = @ProduitId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@ProduitId", produitId)}
            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function

        Public Function ExisteParLibelle(libelle As String, Optional produitIdExclu As Integer? = Nothing) As Boolean
            If String.IsNullOrWhiteSpace(libelle) Then
                Return False
            End If

            Dim sql As String = "SELECT TOP 1 1 FROM Produits " &
                                "WHERE " & ExpressionLibelleNormalisee("Libelle") & " = " & ExpressionLibelleNormalisee("@Libelle") & " " &
                                "AND (@ProduitIdExclu IS NULL OR ProduitId <> @ProduitIdExclu)"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@Libelle", libelle),
                New SqlParameter("@ProduitIdExclu", If(produitIdExclu.HasValue, CType(produitIdExclu.Value, Object), DBNull.Value))
            }
            Dim resultat As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return resultat IsNot Nothing AndAlso resultat IsNot DBNull.Value
        End Function

        Private Shared Function ExpressionLibelleNormalisee(expression As String) As String
            Dim resultat As String = "UPPER(LTRIM(RTRIM(" & expression & ")))"
            For i As Integer = 1 To 8
                resultat = "REPLACE(" & resultat & ", '  ', ' ')"
            Next
            Return resultat
        End Function

        ' Historique detaille des prix pour impression et filtrage.
        Public Function ListerHistoriquePrixTable(produitId As Integer?, dateDebut As Date?, dateFin As Date?) As DataTable
            AssurerColonnes()
            Dim sql As String = "" &
                "WITH Hist AS (" &
                "SELECT h.HistoriquePrixId, h.ProduitId, p.Libelle, h.ModifieLe, h.ModifiePar, " &
                "'Achat' AS TypePrix, h.AncienPrixAchat AS AncienPrix, h.NouveauPrixAchat AS NouveauPrix " &
                "FROM HistoriquePrixProduits h JOIN Produits p ON p.ProduitId=h.ProduitId " &
                "UNION ALL " &
                "SELECT h.HistoriquePrixId, h.ProduitId, p.Libelle, h.ModifieLe, h.ModifiePar, " &
                "'Detail' AS TypePrix, h.AncienPrixDetail, h.NouveauPrixDetail " &
                "FROM HistoriquePrixProduits h JOIN Produits p ON p.ProduitId=h.ProduitId " &
                "UNION ALL " &
                "SELECT h.HistoriquePrixId, h.ProduitId, p.Libelle, h.ModifieLe, h.ModifiePar, " &
                "'Demi' AS TypePrix, h.AncienPrixDemi, h.NouveauPrixDemi " &
                "FROM HistoriquePrixProduits h JOIN Produits p ON p.ProduitId=h.ProduitId " &
                "UNION ALL " &
                "SELECT h.HistoriquePrixId, h.ProduitId, p.Libelle, h.ModifieLe, h.ModifiePar, " &
                "'Quart' AS TypePrix, h.AncienPrixQuart, h.NouveauPrixQuart " &
                "FROM HistoriquePrixProduits h JOIN Produits p ON p.ProduitId=h.ProduitId " &
                "UNION ALL " &
                "SELECT h.HistoriquePrixId, h.ProduitId, p.Libelle, h.ModifieLe, h.ModifiePar, " &
                "'Douzaine' AS TypePrix, h.AncienPrixDouzaine, h.NouveauPrixDouzaine " &
                "FROM HistoriquePrixProduits h JOIN Produits p ON p.ProduitId=h.ProduitId " &
                "UNION ALL " &
                "SELECT h.HistoriquePrixId, h.ProduitId, p.Libelle, h.ModifieLe, h.ModifiePar, " &
                "'Gros' AS TypePrix, h.AncienPrixGros, h.NouveauPrixGros " &
                "FROM HistoriquePrixProduits h JOIN Produits p ON p.ProduitId=h.ProduitId " &
                "UNION ALL " &
                "SELECT h.HistoriquePrixId, h.ProduitId, p.Libelle, h.ModifieLe, h.ModifiePar, " &
                "'Special' AS TypePrix, h.AncienPrixSpecial, h.NouveauPrixSpecial " &
                "FROM HistoriquePrixProduits h JOIN Produits p ON p.ProduitId=h.ProduitId " &
                ") " &
                "SELECT ProduitId, Libelle AS Produit, AncienPrix, NouveauPrix, TypePrix, ModifieLe, " &
                "ISNULL(NULLIF(LTRIM(RTRIM(u.NomUtilisateur)),''), 'Utilisateur inconnu') AS Utilisateur " &
                "FROM Hist " &
                "LEFT JOIN Utilisateurs u ON u.UtilisateurId = Hist.ModifiePar " &
                "WHERE AncienPrix <> NouveauPrix " &
                "AND (@ProduitId IS NULL OR ProduitId=@ProduitId) " &
                "AND (@DateDebut IS NULL OR CAST(ModifieLe AS DATE) >= @DateDebut) " &
                "AND (@DateFin IS NULL OR CAST(ModifieLe AS DATE) <= @DateFin) " &
                "ORDER BY ModifieLe DESC, Produit"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@ProduitId", If(produitId.HasValue, CType(produitId.Value, Object), DBNull.Value)),
                New SqlParameter("@DateDebut", If(dateDebut.HasValue, CType(dateDebut.Value.Date, Object), DBNull.Value)),
                New SqlParameter("@DateFin", If(dateFin.HasValue, CType(dateFin.Value.Date, Object), DBNull.Value))
            }
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        Public Function TopProduitsVendus(annee As Integer) As DataTable
            Dim sql As String = "" &
                "SELECT TOP 10 p.Libelle, SUM(ISNULL(l.QuantiteBase, ISNULL(l.Quantite, 0))) AS QuantiteVendue, SUM(l.MontantLigne) AS Recette " &
                "FROM LignesFactureVente l " &
                "JOIN FacturesVente f ON f.FactureVenteId=l.FactureVenteId " &
                "JOIN Produits p ON p.ProduitId=l.ProduitId " &
                "WHERE f.Statut='PAYEE' AND YEAR(f.CreeLe)=@Annee " &
                "GROUP BY p.Libelle ORDER BY SUM(ISNULL(l.QuantiteBase, ISNULL(l.Quantite, 0))) DESC"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@Annee", annee)
            }
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        Public Function ProduitPlusVenduParMois(annee As Integer) As DataTable
            Dim sql As String = "" &
                "WITH Rangs AS (" &
                "SELECT MONTH(f.CreeLe) AS Mois, p.Libelle, SUM(ISNULL(l.QuantiteBase, ISNULL(l.Quantite, 0))) AS QuantiteVendue, SUM(l.MontantLigne) AS Recette, " &
                "ROW_NUMBER() OVER(PARTITION BY MONTH(f.CreeLe) ORDER BY SUM(ISNULL(l.QuantiteBase, ISNULL(l.Quantite, 0))) DESC) AS Rang " &
                "FROM LignesFactureVente l " &
                "JOIN FacturesVente f ON f.FactureVenteId=l.FactureVenteId " &
                "JOIN Produits p ON p.ProduitId=l.ProduitId " &
                "WHERE f.Statut='PAYEE' AND YEAR(f.CreeLe)=@Annee " &
                "GROUP BY MONTH(f.CreeLe), p.Libelle" &
                ") " &
                "SELECT Mois, Libelle, QuantiteVendue, Recette FROM Rangs WHERE Rang=1 ORDER BY Mois"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@Annee", annee)
            }
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        Public Function RepartitionParCategorie() As DataTable
            Dim sql As String = "" &
                "SELECT ISNULL(cat.NomCategorie, 'Sans categorie') AS Categorie, COUNT(*) AS NombreProduits " &
                "FROM Produits p LEFT JOIN CategoriesProduits cat ON cat.CategorieId = p.CategorieId " &
                "GROUP BY ISNULL(cat.NomCategorie, 'Sans categorie') ORDER BY COUNT(*) DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        Public Function ListerProduitsDormantsTable() As DataTable
            Dim sql As String = "" &
                "SELECT p.ProduitId, p.Libelle, p.CodeBarres, ISNULL(cat.NomCategorie, 'Sans categorie') AS Categorie, " &
                "       ISNULL(s.QuantiteStock, 0) AS QuantiteStock, ds.DerniereVente " &
                "FROM Produits p " &
                "LEFT JOIN CategoriesProduits cat ON cat.CategorieId = p.CategorieId " &
                "LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId " &
                "OUTER APPLY ( " &
                "    SELECT MAX(f.CreeLe) AS DerniereVente " &
                "    FROM LignesFactureVente l " &
                "    INNER JOIN FacturesVente f ON f.FactureVenteId = l.FactureVenteId " &
                "    WHERE l.ProduitId = p.ProduitId AND f.Statut = 'PAYEE' " &
                ") ds " &
                "WHERE NOT EXISTS ( " &
                "    SELECT 1 " &
                "    FROM LignesFactureVente l2 " &
                "    INNER JOIN FacturesVente f2 ON f2.FactureVenteId = l2.FactureVenteId " &
                "    WHERE l2.ProduitId = p.ProduitId " &
                "      AND f2.Statut = 'PAYEE' " &
                "      AND f2.CreeLe >= DATEADD(DAY, -90, GETDATE()) " &
                ") " &
                "ORDER BY p.Libelle"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        Public Function KpiProduits() As DataTable
            Dim sql As String = "" &
                "SELECT " &
                "(SELECT TOP 1 p.Libelle FROM LignesFactureVente l JOIN FacturesVente f ON f.FactureVenteId=l.FactureVenteId JOIN Produits p ON p.ProduitId=l.ProduitId WHERE f.Statut='PAYEE' GROUP BY p.Libelle ORDER BY SUM(l.MontantLigne - (ISNULL(l.QuantiteBase, ISNULL(l.Quantite, 0)) * CASE WHEN p.PrixAchat <= 0 THEN 0 WHEN UPPER(ISNULL(p.TypeGestionStock, 'UNITE')) IN ('MESURE','POIDS','VOLUME') AND ISNULL(p.ContenuUnitePrincipale, 0) > 0 THEN p.PrixAchat / NULLIF(p.ContenuUnitePrincipale, 0) WHEN ISNULL(p.ConversionUnite, 0) > 0 THEN p.PrixAchat / NULLIF(p.ConversionUnite, 0) ELSE p.PrixAchat END)) DESC) AS ProduitPlusRentable, " &
                "(SELECT ISNULL(SUM(l.MontantLigne),0) FROM LignesFactureVente l JOIN FacturesVente f ON f.FactureVenteId=l.FactureVenteId WHERE f.Statut='PAYEE') AS TotalRecettes, " &
                "(SELECT COUNT(*) FROM Produits) AS NombreTotalProduits, " &
                "(SELECT COUNT(*) FROM Produits p LEFT JOIN (SELECT ProduitId, SUM(ISNULL(QuantiteBase, ISNULL(Quantite, 0))) AS Qte FROM LignesFactureVente l JOIN FacturesVente f ON f.FactureVenteId=l.FactureVenteId WHERE f.Statut='PAYEE' GROUP BY ProduitId) v ON v.ProduitId=p.ProduitId WHERE ISNULL(v.Qte,0) > 0 AND ISNULL(v.Qte,0) <= 3) AS FaibleRotation, " &
                "(SELECT COUNT(*) FROM Produits p LEFT JOIN (SELECT DISTINCT ProduitId FROM LignesFactureVente l JOIN FacturesVente f ON f.FactureVenteId=l.FactureVenteId WHERE f.Statut='PAYEE' AND f.CreeLe >= DATEADD(DAY,-90,GETDATE())) v ON v.ProduitId=p.ProduitId WHERE v.ProduitId IS NULL) AS ProduitsDormants"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        Private Function MapVersDTO(row As DataRow) As ProduitDTO
            Dim dto As New ProduitDTO With {
                .ProduitId = Convert.ToInt32(row("ProduitId")),
                .CodeBarres = Convert.ToString(row("CodeBarres")),
                .Libelle = Convert.ToString(row("Libelle")),
                .PrixDetail = Convert.ToDecimal(row("PrixDetail")),
                .PrixAchat = Convert.ToDecimal(row("PrixAchat")),
                .PrixDemi = Convert.ToDecimal(row("PrixDemi")),
                .PrixQuart = Convert.ToDecimal(row("PrixQuart")),
                .PrixDouzaine = Convert.ToDecimal(row("PrixDouzaine")),
                .PrixGros = Convert.ToDecimal(row("PrixGros")),
                .PrixSpecial = Convert.ToDecimal(row("PrixSpecial")),
                .CoefficientGros = If(row.IsNull("CoefficientGros"), 0D, Convert.ToDecimal(row("CoefficientGros"))),
                .QuantiteStock = Convert.ToDecimal(row("QuantiteStock")),
                .SeuilCritique = Convert.ToDecimal(row("SeuilCritique")),
                .CategorieId = If(row.Table.Columns.Contains("CategorieId") AndAlso Not row.IsNull("CategorieId"), CType(Convert.ToInt32(row("CategorieId")), Integer?), CType(Nothing, Integer?)),
                .NomCategorie = If(row.Table.Columns.Contains("NomCategorie") AndAlso Not row.IsNull("NomCategorie"), Convert.ToString(row("NomCategorie")), String.Empty),
                .UnitePrincipale = If(row.IsNull("UnitePrincipale"), Nothing, Convert.ToString(row("UnitePrincipale"))),
                .UniteSecondaire = If(row.IsNull("UniteSecondaire"), Nothing, Convert.ToString(row("UniteSecondaire"))),
                .ConversionUnite = Convert.ToDecimal(row("ConversionUnite")),
                .TypeGestionStock = If(row.Table.Columns.Contains("TypeGestionStock") AndAlso Not row.IsNull("TypeGestionStock"), NormaliserTypeGestionStock(Convert.ToString(row("TypeGestionStock"))), "UNITE"),
                .UniteMesureStock = If(row.Table.Columns.Contains("UniteMesureStock") AndAlso Not row.IsNull("UniteMesureStock"), Convert.ToString(row("UniteMesureStock")), "PIECE"),
                .ContenuUnitePrincipale = If(row.Table.Columns.Contains("ContenuUnitePrincipale") AndAlso Not row.IsNull("ContenuUnitePrincipale"), Convert.ToDecimal(row("ContenuUnitePrincipale")), If(row.IsNull("ConversionUnite"), 1D, Convert.ToDecimal(row("ConversionUnite")))),
                .ContenuUniteSecondaire = If(row.Table.Columns.Contains("ContenuUniteSecondaire") AndAlso Not row.IsNull("ContenuUniteSecondaire"), CType(Convert.ToDecimal(row("ContenuUniteSecondaire")), Decimal?), CType(Nothing, Decimal?)),
                .EstActif = Convert.ToBoolean(row("EstActif")),
                .VenteDetail = Convert.ToBoolean(row("VenteDetail")),
                .VenteDemi = Convert.ToBoolean(row("VenteDemi")),
                .VenteDouzaine = Convert.ToBoolean(row("VenteDouzaine")),
                .VenteGros = Convert.ToBoolean(row("VenteGros"))
            }

            If row.IsNull("DateExpiration") Then
                dto.DateExpiration = Nothing
            Else
                dto.DateExpiration = Convert.ToDateTime(row("DateExpiration"))
            End If

            Return dto
        End Function

        Private Shared Function NormaliserTypeGestionStock(typeGestion As String) As String
            Return StockUnitConversionService.NormaliserTypeGestionStock(typeGestion)
        End Function

        Private Shared Function ObtenirContenuPrincipal(produit As Produit) As Decimal
            If produit IsNot Nothing AndAlso produit.ContenuUnitePrincipale > 0D Then
                Return produit.ContenuUnitePrincipale
            End If
            If produit IsNot Nothing AndAlso produit.ConversionUnite > 0D Then
                Return produit.ConversionUnite
            End If
            Return 1D
        End Function
    End Class
End Namespace
