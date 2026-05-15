Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class ProduitRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerColonnes()
        End Sub

        Private Sub AssurerColonnes()
            Dim sql As String = "" &
                "IF COL_LENGTH('Produits','PrixDemi') IS NULL ALTER TABLE Produits ADD PrixDemi DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('Produits','PrixAchat') IS NULL ALTER TABLE Produits ADD PrixAchat DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('Produits','PrixQuart') IS NULL ALTER TABLE Produits ADD PrixQuart DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('Produits','PrixDouzaine') IS NULL ALTER TABLE Produits ADD PrixDouzaine DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('Produits','PrixSpecial') IS NULL ALTER TABLE Produits ADD PrixSpecial DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('Produits','CoefficientGros') IS NULL ALTER TABLE Produits ADD CoefficientGros DECIMAL(18,4) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('Produits','SeuilCritique') IS NULL ALTER TABLE Produits ADD SeuilCritique DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('Produits','UnitePrincipale') IS NULL ALTER TABLE Produits ADD UnitePrincipale NVARCHAR(50) NULL; " &
                "IF COL_LENGTH('Produits','UniteSecondaire') IS NULL ALTER TABLE Produits ADD UniteSecondaire NVARCHAR(50) NULL; " &
                "IF COL_LENGTH('Produits','ConversionUnite') IS NULL ALTER TABLE Produits ADD ConversionUnite DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('Produits','VenteDetail') IS NULL ALTER TABLE Produits ADD VenteDetail BIT NOT NULL DEFAULT 1; " &
                "IF COL_LENGTH('Produits','VenteDemi') IS NULL ALTER TABLE Produits ADD VenteDemi BIT NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('Produits','VenteDouzaine') IS NULL ALTER TABLE Produits ADD VenteDouzaine BIT NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('Produits','VenteGros') IS NULL ALTER TABLE Produits ADD VenteGros BIT NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('Produits','ModifierPar') IS NULL ALTER TABLE Produits ADD ModifierPar NVARCHAR(80) NULL; " &
                "IF OBJECT_ID('HistoriquePrixProduits','U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE HistoriquePrixProduits (" &
                "HistoriquePrixId INT IDENTITY(1,1) PRIMARY KEY, " &
                "ProduitId INT NOT NULL, " &
                "AncienPrixAchat DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "NouveauPrixAchat DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "AncienPrixDetail DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "NouveauPrixDetail DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "AncienPrixDemi DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "NouveauPrixDemi DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "AncienPrixQuart DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "NouveauPrixQuart DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "AncienPrixDouzaine DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "NouveauPrixDouzaine DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "AncienPrixGros DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "NouveauPrixGros DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "AncienPrixSpecial DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "NouveauPrixSpecial DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "ModifiePar NVARCHAR(80) NULL, " &
                "ModifieLe DATETIME2 NOT NULL DEFAULT GETDATE(), " &
                "IdStock NVARCHAR(30) NULL" &
                "); " &
                "END; " &
                "IF COL_LENGTH('HistoriquePrixProduits','AncienPrixAchat') IS NULL ALTER TABLE HistoriquePrixProduits ADD AncienPrixAchat DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('HistoriquePrixProduits','NouveauPrixAchat') IS NULL ALTER TABLE HistoriquePrixProduits ADD NouveauPrixAchat DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('HistoriquePrixProduits','AncienPrixDemi') IS NULL ALTER TABLE HistoriquePrixProduits ADD AncienPrixDemi DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('HistoriquePrixProduits','NouveauPrixDemi') IS NULL ALTER TABLE HistoriquePrixProduits ADD NouveauPrixDemi DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('HistoriquePrixProduits','AncienPrixQuart') IS NULL ALTER TABLE HistoriquePrixProduits ADD AncienPrixQuart DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('HistoriquePrixProduits','NouveauPrixQuart') IS NULL ALTER TABLE HistoriquePrixProduits ADD NouveauPrixQuart DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('HistoriquePrixProduits','AncienPrixDouzaine') IS NULL ALTER TABLE HistoriquePrixProduits ADD AncienPrixDouzaine DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('HistoriquePrixProduits','NouveauPrixDouzaine') IS NULL ALTER TABLE HistoriquePrixProduits ADD NouveauPrixDouzaine DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('HistoriquePrixProduits','AncienPrixSpecial') IS NULL ALTER TABLE HistoriquePrixProduits ADD AncienPrixSpecial DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('HistoriquePrixProduits','NouveauPrixSpecial') IS NULL ALTER TABLE HistoriquePrixProduits ADD NouveauPrixSpecial DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('HistoriquePrixProduits','IdStock') IS NULL ALTER TABLE HistoriquePrixProduits ADD IdStock NVARCHAR(30) NULL;"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub

        ' Cree un produit et retourne son identifiant.
        Public Function Ajouter(produit As Produit) As Integer
            Dim sql As String = "INSERT INTO Produits (CodeBarres, Libelle, PrixDetail, PrixAchat, PrixDemi, PrixQuart, PrixDouzaine, PrixGros, PrixSpecial, CoefficientGros, SeuilCritique, DateExpiration, CategorieId, UnitePrincipale, UniteSecondaire, ConversionUnite, EstActif, VenteDetail, VenteDemi, VenteDouzaine, VenteGros, ModifierPar) " &
                                "VALUES (@CodeBarres, @Libelle, @PrixDetail, @PrixAchat, @PrixDemi, @PrixQuart, @PrixDouzaine, @PrixGros, @PrixSpecial, @CoefficientGros, @SeuilCritique, @DateExpiration, @CategorieId, @UnitePrincipale, @UniteSecondaire, @ConversionUnite, @EstActif, @VenteDetail, @VenteDemi, @VenteDouzaine, @VenteGros, @ModifierPar); " &
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
                New SqlParameter("@EstActif", produit.EstActif),
                New SqlParameter("@VenteDetail", produit.VenteDetail),
                New SqlParameter("@VenteDemi", produit.VenteDemi),
                New SqlParameter("@VenteDouzaine", produit.VenteDouzaine),
                New SqlParameter("@VenteGros", produit.VenteGros),
                New SqlParameter("@ModifierPar", SessionUtilisateur.NomUtilisateur)
            }

            Dim id As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(id)
        End Function

        ' Retourne la liste des produits.
        Public Function Lister() As List(Of ProduitDTO)
            Dim sql As String = "SELECT p.ProduitId, p.CodeBarres, p.Libelle, p.PrixDetail, p.PrixAchat, p.PrixDemi, p.PrixQuart, p.PrixDouzaine, p.PrixGros, p.PrixSpecial, p.CoefficientGros, " &
                                "ISNULL(s.QuantiteStock,0) AS QuantiteStock, p.SeuilCritique, p.DateExpiration, p.CategorieId, p.EstActif, p.UnitePrincipale, p.UniteSecondaire, p.ConversionUnite, " &
                                "p.VenteDetail, p.VenteDemi, p.VenteDouzaine, p.VenteGros " &
                                "FROM Produits p LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId"
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
                                " cast (ISNULL(s.QuantiteStock,0) as int) AS QuantiteStock, p.SeuilCritique, p.DateExpiration, p.CategorieId, p.EstActif, p.UnitePrincipale, p.UniteSecondaire, p.ConversionUnite, " &
                                "p.VenteDetail, p.VenteDemi, p.VenteDouzaine, p.VenteGros " &
                                "FROM Produits p LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function


        Public Function ListerQteProduit(produitId As Integer) As Integer
            Dim sql As String = "
                                SELECT cast (ISNULL(s.QuantiteStock,0)as int) AS QuantiteStock
                                FROM Produits p LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId
								where p.ProduitId=@ProduitId"

            Dim p As New List(Of SqlParameter) From {New SqlParameter("@ProduitId", produitId)}
            Dim id As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(id)
        End Function

        ' Retourne la liste des tepes ventes et prix produits sous forme de DataTable pour filtrage local.
        Public Function ListerTypeVente(produitId As Integer) As DataTable
            Dim sql As String = "SELECT TypeVente, Prix FROM (SELECT 'Détail' AS TypeVente, PrixDetail AS Prix, VenteDetail AS EstActif FROM Produits WHERE ProduitId = @ProduitId UNION ALL
                                 SELECT 'Demi', PrixDemi, VenteDemi FROM Produits WHERE ProduitId = @ProduitId
                                 UNION ALL
                                 SELECT 'Quart', PrixQuart, 1 FROM Produits WHERE ProduitId = @ProduitId
                                 UNION ALL
                                 SELECT 'Gros', PrixGros, VenteGros FROM Produits WHERE ProduitId = @ProduitId
                                 UNION ALL
                                 SELECT 'Douzaine', PrixDouzaine, VenteDouzaine FROM Produits WHERE ProduitId = @ProduitId
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
                                "ISNULL(s.QuantiteStock,0) AS QuantiteStock, p.SeuilCritique, p.DateExpiration, p.CategorieId, p.EstActif, p.UnitePrincipale, p.UniteSecondaire, p.ConversionUnite, " &
                                "p.VenteDetail, p.VenteDemi, p.VenteDouzaine, p.VenteGros " &
                                "FROM Produits p LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId " &
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
                                "ISNULL(s.QuantiteStock,0) AS QuantiteStock, p.SeuilCritique, p.DateExpiration, p.CategorieId, p.EstActif, p.UnitePrincipale, p.UniteSecondaire, p.ConversionUnite, " &
                                "p.VenteDetail, p.VenteDemi, p.VenteDouzaine, p.VenteGros " &
                                "FROM Produits p LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId WHERE p.ProduitId = @ProduitId"
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
                                "SeuilCritique=@SeuilCritique, DateExpiration=@DateExpiration, CategorieId=@CategorieId, UnitePrincipale=@UnitePrincipale, UniteSecondaire=@UniteSecondaire, ConversionUnite=@ConversionUnite, EstActif=@EstActif, " &
                                "VenteDetail=@VenteDetail, VenteDemi=@VenteDemi, VenteDouzaine=@VenteDouzaine, VenteGros=@VenteGros, ModifierPar=@ModifierPar " &
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
                New SqlParameter("@EstActif", produit.EstActif),
                New SqlParameter("@VenteDetail", produit.VenteDetail),
                New SqlParameter("@VenteDemi", produit.VenteDemi),
                New SqlParameter("@VenteDouzaine", produit.VenteDouzaine),
                New SqlParameter("@VenteGros", produit.VenteGros),
                New SqlParameter("@ProduitId", produit.ProduitId),
                New SqlParameter("@ModifierPar", SessionUtilisateur.NomUtilisateur)
            }

            Dim rows As Integer = _dal.ExecuterNonRequete(sql, CommandType.Text, p)

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
                                        "@AncienPrixSpecial, @NouveauPrixSpecial, @ModifiePar, GETDATE(), @IdStock)"
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
                    New SqlParameter("@ModifiePar", SessionUtilisateur.NomUtilisateur),
                    New SqlParameter("@IdStock", DBNull.Value)
                }
                _dal.ExecuterNonRequete(sqlHist, CommandType.Text, pHist)
            End If

            Return rows
        End Function

        ' Supprime un produit.
        Public Function Supprimer(produitId As Integer) As Integer
            Dim sql As String = "DELETE FROM Produits WHERE ProduitId = @ProduitId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@ProduitId", produitId)}
            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
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
                "SELECT ProduitId, Libelle AS Produit, AncienPrix, NouveauPrix, TypePrix, ModifieLe, ISNULL(ModifiePar,'') AS Utilisateur " &
                "FROM Hist " &
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
                "SELECT TOP 10 p.Libelle, SUM(l.Quantite) AS QuantiteVendue, SUM(l.MontantLigne) AS Recette " &
                "FROM LignesFactureVente l " &
                "JOIN FacturesVente f ON f.FactureVenteId=l.FactureVenteId " &
                "JOIN Produits p ON p.ProduitId=l.ProduitId " &
                "WHERE f.Statut='PAYEE' AND YEAR(f.CreeLe)=@Annee " &
                "GROUP BY p.Libelle ORDER BY SUM(l.Quantite) DESC"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@Annee", annee)
            }
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        Public Function ProduitPlusVenduParMois(annee As Integer) As DataTable
            Dim sql As String = "" &
                "WITH Rangs AS (" &
                "SELECT MONTH(f.CreeLe) AS Mois, p.Libelle, SUM(l.Quantite) AS QuantiteVendue, SUM(l.MontantLigne) AS Recette, " &
                "ROW_NUMBER() OVER(PARTITION BY MONTH(f.CreeLe) ORDER BY SUM(l.Quantite) DESC) AS Rang " &
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
                "SELECT ISNULL(CAST(p.CategorieId AS NVARCHAR(20)),'Sans categorie') AS Categorie, COUNT(*) AS NombreProduits " &
                "FROM Produits p GROUP BY ISNULL(CAST(p.CategorieId AS NVARCHAR(20)),'Sans categorie') ORDER BY COUNT(*) DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        Public Function KpiProduits() As DataTable
            Dim sql As String = "" &
                "SELECT " &
                "(SELECT TOP 1 p.Libelle FROM LignesFactureVente l JOIN FacturesVente f ON f.FactureVenteId=l.FactureVenteId JOIN Produits p ON p.ProduitId=l.ProduitId WHERE f.Statut='PAYEE' GROUP BY p.Libelle ORDER BY SUM(l.MontantLigne - (l.Quantite * CASE WHEN p.PrixAchat > 0 THEN p.PrixAchat ELSE 0 END)) DESC) AS ProduitPlusRentable, " &
                "(SELECT ISNULL(SUM(l.MontantLigne),0) FROM LignesFactureVente l JOIN FacturesVente f ON f.FactureVenteId=l.FactureVenteId WHERE f.Statut='PAYEE') AS TotalRecettes, " &
                "(SELECT COUNT(*) FROM Produits) AS NombreTotalProduits, " &
                "(SELECT COUNT(*) FROM Produits p LEFT JOIN (SELECT ProduitId, SUM(Quantite) AS Qte FROM LignesFactureVente l JOIN FacturesVente f ON f.FactureVenteId=l.FactureVenteId WHERE f.Statut='PAYEE' GROUP BY ProduitId) v ON v.ProduitId=p.ProduitId WHERE ISNULL(v.Qte,0) > 0 AND ISNULL(v.Qte,0) <= 3) AS FaibleRotation, " &
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
                .UnitePrincipale = If(row.IsNull("UnitePrincipale"), Nothing, Convert.ToString(row("UnitePrincipale"))),
                .UniteSecondaire = If(row.IsNull("UniteSecondaire"), Nothing, Convert.ToString(row("UniteSecondaire"))),
                .ConversionUnite = Convert.ToDecimal(row("ConversionUnite")),
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
    End Class
End Namespace
