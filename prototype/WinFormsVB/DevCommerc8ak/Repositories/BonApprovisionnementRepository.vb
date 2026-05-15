Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class BonApprovisionnementRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        ' Assure le schema du module approvisionnement.
        Public Sub AssurerTables()
            Dim sql As String = "" &
                "IF OBJECT_ID('BonsApprovisionnement','U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE BonsApprovisionnement (" &
                "BonId INT IDENTITY(1,1) PRIMARY KEY, " &
                "NumeroBon NVARCHAR(20) NULL, " &
                "DateCreation DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(), " &
                "Statut NVARCHAR(30) NOT NULL DEFAULT 'EnAttente', " &
                "FournisseurId INT NULL, " &
                "TypePaiement NVARCHAR(30) NULL, " &
                "CreePar INT NOT NULL, " &
                "ModifierPar NVARCHAR(80) NULL " &
                "); " &
                "END; " &
                "IF OBJECT_ID('BonApprovisionnementLignes','U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE BonApprovisionnementLignes (" &
                "BonLigneId INT IDENTITY(1,1) PRIMARY KEY, " &
                "BonId INT NOT NULL, " &
                "ProduitId INT NOT NULL, " &
                "Quantite DECIMAL(18,2) NOT NULL, " &
                "PrixAchat DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "PrixAchatPrecedent DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "TotalLigne DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                "CONSTRAINT fk_bonligne_bon FOREIGN KEY (BonId) REFERENCES BonsApprovisionnement(BonId)" &
                "); " &
                "END; " &
                "IF OBJECT_ID('BonApprovisionnementSequence','U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE BonApprovisionnementSequence (Annee CHAR(4) NOT NULL PRIMARY KEY, DernierNumero INT NOT NULL); " &
                "END; " &
                "IF COL_LENGTH('BonsApprovisionnement','NumeroBon') IS NULL ALTER TABLE BonsApprovisionnement ADD NumeroBon NVARCHAR(20) NULL; " &
                "IF COL_LENGTH('BonsApprovisionnement','ModifierPar') IS NULL ALTER TABLE BonsApprovisionnement ADD ModifierPar NVARCHAR(80) NULL; " &
                "IF COL_LENGTH('BonApprovisionnementLignes','PrixAchat') IS NULL ALTER TABLE BonApprovisionnementLignes ADD PrixAchat DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('BonApprovisionnementLignes','PrixAchatPrecedent') IS NULL ALTER TABLE BonApprovisionnementLignes ADD PrixAchatPrecedent DECIMAL(18,2) NOT NULL DEFAULT 0; " &
                "IF COL_LENGTH('BonApprovisionnementLignes','TotalLigne') IS NULL ALTER TABLE BonApprovisionnementLignes ADD TotalLigne DECIMAL(18,2) NOT NULL DEFAULT 0;"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub

        ' Cree un bon et retourne son identifiant.
        Public Function CreerBon(fournisseurId As Integer?, typePaiement As String, creePar As Integer, Optional statut As String = "EnAttente") As Integer
            AssurerTables()
            Dim numeroBon As String = GenererNumeroBon()
            Dim sql As String
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@NumeroBon", numeroBon),
                New SqlParameter("@FournisseurId", If(fournisseurId.HasValue, CType(fournisseurId.Value, Object), DBNull.Value)),
                New SqlParameter("@TypePaiement", If(String.IsNullOrWhiteSpace(typePaiement), CType(DBNull.Value, Object), typePaiement)),
                New SqlParameter("@CreePar", creePar),
                New SqlParameter("@Statut", statut)
            }

            If ColonneExiste("BonsApprovisionnement", "ModifierPar") Then
                sql = "INSERT INTO BonsApprovisionnement (NumeroBon, FournisseurId, TypePaiement, CreePar, Statut, ModifierPar) " &
                      "VALUES (@NumeroBon, @FournisseurId, @TypePaiement, @CreePar, @Statut, @ModifierPar); SELECT CAST(SCOPE_IDENTITY() AS INT);"
                p.Add(New SqlParameter("@ModifierPar", SessionUtilisateur.NomUtilisateur))
            Else
                sql = "INSERT INTO BonsApprovisionnement (NumeroBon, FournisseurId, TypePaiement, CreePar, Statut) " &
                      "VALUES (@NumeroBon, @FournisseurId, @TypePaiement, @CreePar, @Statut); SELECT CAST(SCOPE_IDENTITY() AS INT);"
            End If

            Dim id As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(id)
        End Function

        ' Ajoute une ligne a un bon.
        Public Sub AjouterLigne(bonId As Integer, produitId As Integer, quantite As Decimal, Optional prixAchat As Decimal = 0D)
            AssurerTables()
            Dim prixPrecedent As Decimal = ObtenirDernierPrixAchat(produitId)
            Dim prixUtilise As Decimal = If(prixAchat > 0D, prixAchat, prixPrecedent)
            Dim sql As String = "INSERT INTO BonApprovisionnementLignes (BonId, ProduitId, Quantite, PrixAchat, PrixAchatPrecedent, TotalLigne) " &
                                "VALUES (@BonId, @ProduitId, @Quantite, @PrixAchat, @PrixAchatPrecedent, @TotalLigne)"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@BonId", bonId),
                New SqlParameter("@ProduitId", produitId),
                New SqlParameter("@Quantite", quantite),
                New SqlParameter("@PrixAchat", prixUtilise),
                New SqlParameter("@PrixAchatPrecedent", prixPrecedent),
                New SqlParameter("@TotalLigne", quantite * prixUtilise)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        ' Supprime une ligne precise d'un bon.
        Public Sub SupprimerLigne(bonLigneId As Integer)
            AssurerTables()
            Dim sql As String = "DELETE FROM BonApprovisionnementLignes WHERE BonLigneId=@BonLigneId"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@BonLigneId", bonLigneId)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        ' Supprime un bon brouillon et ses lignes en une seule operation.
        Public Sub SupprimerBon(bonId As Integer)
            AssurerTables()
            Dim sql As String = "" &
                "BEGIN TRAN; " &
                "DELETE FROM BonApprovisionnementLignes WHERE BonId=@BonId; " &
                "DELETE FROM BonsApprovisionnement WHERE BonId=@BonId; " &
                "COMMIT;"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@BonId", bonId)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        ' Liste des bons avec total et fournisseur.
        Public Function ListerBons() As DataTable
            AssurerTables()
            Dim sql As String = "" &
                "SELECT b.BonId, b.NumeroBon, b.DateCreation, b.Statut, b.FournisseurId, f.NomFournisseur, b.TypePaiement, " &
                "ISNULL(SUM(l.TotalLigne),0) AS TotalBon, COUNT(l.BonLigneId) AS NombreLignes " &
                "FROM BonsApprovisionnement b " &
                "LEFT JOIN Fournisseurs f ON f.FournisseurId = b.FournisseurId " &
                "LEFT JOIN BonApprovisionnementLignes l ON l.BonId = b.BonId " &
                "GROUP BY b.BonId, b.NumeroBon, b.DateCreation, b.Statut, b.FournisseurId, f.NomFournisseur, b.TypePaiement " &
                "ORDER BY b.DateCreation DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        ' Liste detaillee des lignes d'un bon.
        Public Function ListerLignes(bonId As Integer) As DataTable
            AssurerTables()
            Dim sql As String = "SELECT l.BonLigneId, l.ProduitId, p.Libelle, l.Quantite, l.PrixAchatPrecedent, l.PrixAchat, l.TotalLigne " &
                                "FROM BonApprovisionnementLignes l JOIN Produits p ON p.ProduitId = l.ProduitId " &
                                "WHERE l.BonId=@BonId ORDER BY p.Libelle"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@BonId", bonId)}
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        ' Retourne le nombre de lignes associees a un bon.
        Public Function CompterLignes(bonId As Integer) As Integer
            AssurerTables()
            Dim sql As String = "SELECT COUNT(*) FROM BonApprovisionnementLignes WHERE BonId=@BonId"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@BonId", bonId)
            }
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(v)
        End Function

        ' Recherche temps reel des bons.
        Public Function RechercherBons(texte As String) As DataTable
            AssurerTables()
            Dim sql As String = "" &
                "SELECT b.BonId, b.NumeroBon, b.DateCreation, b.Statut, b.FournisseurId, f.NomFournisseur, b.TypePaiement, " &
                "ISNULL(SUM(l.TotalLigne),0) AS TotalBon, COUNT(l.BonLigneId) AS NombreLignes " &
                "FROM BonsApprovisionnement b " &
                "LEFT JOIN Fournisseurs f ON f.FournisseurId = b.FournisseurId " &
                "LEFT JOIN BonApprovisionnementLignes l ON l.BonId = b.BonId " &
                "WHERE b.NumeroBon LIKE @q OR ISNULL(f.NomFournisseur,'') LIKE @q OR b.Statut LIKE @q " &
                "GROUP BY b.BonId, b.NumeroBon, b.DateCreation, b.Statut, b.FournisseurId, f.NomFournisseur, b.TypePaiement " &
                "ORDER BY b.DateCreation DESC"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@q", "%" & texte & "%")}
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        ' Met a jour le statut d'un bon.
        Public Sub ChangerStatut(bonId As Integer, statut As String)
            AssurerTables()
            Dim sql As String
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@Statut", statut),
                New SqlParameter("@BonId", bonId)
            }

            If ColonneExiste("BonsApprovisionnement", "ModifierPar") Then
                sql = "UPDATE BonsApprovisionnement SET Statut=@Statut, ModifierPar=@ModifierPar WHERE BonId=@BonId"
                p.Add(New SqlParameter("@ModifierPar", SessionUtilisateur.NomUtilisateur))
            Else
                sql = "UPDATE BonsApprovisionnement SET Statut=@Statut WHERE BonId=@BonId"
            End If

            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        ' Met a jour l'entete du bon sans toucher aux lignes.
        Public Sub MettreAJourEntete(bonId As Integer, fournisseurId As Integer?, typePaiement As String)
            AssurerTables()
            Dim sql As String
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@BonId", bonId),
                New SqlParameter("@FournisseurId", If(fournisseurId.HasValue, CType(fournisseurId.Value, Object), DBNull.Value)),
                New SqlParameter("@TypePaiement", If(String.IsNullOrWhiteSpace(typePaiement), CType(DBNull.Value, Object), typePaiement))
            }

            If ColonneExiste("BonsApprovisionnement", "ModifierPar") Then
                sql = "UPDATE BonsApprovisionnement SET FournisseurId=@FournisseurId, TypePaiement=@TypePaiement, ModifierPar=@ModifierPar WHERE BonId=@BonId"
                p.Add(New SqlParameter("@ModifierPar", SessionUtilisateur.NomUtilisateur))
            Else
                sql = "UPDATE BonsApprovisionnement SET FournisseurId=@FournisseurId, TypePaiement=@TypePaiement WHERE BonId=@BonId"
            End If

            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        ' Retourne un tableau de suggestions auto pour stock critique.
        Public Function ListerSuggestionsAuto(seuil As Decimal) As DataTable
            AssurerTables()
            Dim sql As String = "" &
                "SELECT p.ProduitId, p.Libelle, ISNULL(p.SeuilCritique, @SeuilGlobal) AS SeuilCritique, ISNULL(s.QuantiteStock,0) AS StockActuel, " &
                "CASE WHEN (ISNULL(p.SeuilCritique, @SeuilGlobal) * 2) - ISNULL(s.QuantiteStock,0) > 0 " &
                "THEN (ISNULL(p.SeuilCritique, @SeuilGlobal) * 2) - ISNULL(s.QuantiteStock,0) ELSE 0 END AS QuantiteSuggeree, " &
                "ISNULL((SELECT TOP 1 se.PrixAchat FROM StockEntree se WHERE se.ProduitId = p.ProduitId ORDER BY se.DateEntree DESC), ISNULL(p.PrixAchat, 0)) AS PrixAchatPrecedent " &
                "FROM Produits p " &
                "LEFT JOIN vStockProduit s ON s.ProduitId = p.ProduitId " &
                "WHERE ISNULL(s.QuantiteStock,0) <= CASE WHEN p.SeuilCritique > 0 THEN p.SeuilCritique ELSE @SeuilGlobal END " &
                "ORDER BY p.Libelle"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@SeuilGlobal", seuil)}
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        ' Statistiques historiques par periode.
        Public Function HistoriqueParPeriode(annee As Integer, mois As Integer?) As DataTable
            AssurerTables()
            Dim sql As String = "" &
                "SELECT YEAR(b.DateCreation) AS Annee, MONTH(b.DateCreation) AS Mois, COUNT(DISTINCT b.BonId) AS NombreBons, " &
                "ISNULL(SUM(l.TotalLigne),0) AS TotalApprovisionnement " &
                "FROM BonsApprovisionnement b " &
                "LEFT JOIN BonApprovisionnementLignes l ON l.BonId = b.BonId " &
                "WHERE YEAR(b.DateCreation)=@Annee " &
                "AND (@Mois IS NULL OR MONTH(b.DateCreation)=@Mois) " &
                "GROUP BY YEAR(b.DateCreation), MONTH(b.DateCreation) " &
                "ORDER BY Mois"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@Annee", annee),
                New SqlParameter("@Mois", If(mois.HasValue, CType(mois.Value, Object), DBNull.Value))
            }
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        ' Produits les plus commandes.
        Public Function ProduitsPlusCommandes(annee As Integer, mois As Integer?) As DataTable
            AssurerTables()
            Dim sql As String = "" &
                "SELECT TOP 10 p.Libelle, SUM(l.Quantite) AS QuantiteCommandee " &
                "FROM BonsApprovisionnement b " &
                "JOIN BonApprovisionnementLignes l ON l.BonId = b.BonId " &
                "JOIN Produits p ON p.ProduitId = l.ProduitId " &
                "WHERE YEAR(b.DateCreation)=@Annee AND (@Mois IS NULL OR MONTH(b.DateCreation)=@Mois) " &
                "GROUP BY p.Libelle ORDER BY SUM(l.Quantite) DESC"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@Annee", annee),
                New SqlParameter("@Mois", If(mois.HasValue, CType(mois.Value, Object), DBNull.Value))
            }
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        ' Repartition des fournisseurs.
        Public Function RepartitionFournisseurs(annee As Integer, mois As Integer?) As DataTable
            AssurerTables()
            Dim sql As String = "" &
                "SELECT ISNULL(f.NomFournisseur, 'Sans fournisseur') AS Fournisseur, COUNT(*) AS NombreBons " &
                "FROM BonsApprovisionnement b " &
                "LEFT JOIN Fournisseurs f ON f.FournisseurId = b.FournisseurId " &
                "WHERE YEAR(b.DateCreation)=@Annee AND (@Mois IS NULL OR MONTH(b.DateCreation)=@Mois) " &
                "GROUP BY ISNULL(f.NomFournisseur, 'Sans fournisseur') ORDER BY COUNT(*) DESC"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@Annee", annee),
                New SqlParameter("@Mois", If(mois.HasValue, CType(mois.Value, Object), DBNull.Value))
            }
            Return _dal.ExecuterTable(sql, CommandType.Text, p)
        End Function

        ' Fournit la liste active des fournisseurs.
        Public Function ListerFournisseurs() As DataTable
            Dim sql As String = "SELECT FournisseurId, NomFournisseur FROM Fournisseurs WHERE EstActif = 1 ORDER BY NomFournisseur"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        Private Function ColonneExiste(tableName As String, columnName As String) As Boolean
            Dim sql As String = "SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(@TableName) AND name = @ColumnName"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@TableName", tableName),
                New SqlParameter("@ColumnName", columnName)
            }
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return Convert.ToInt32(v) > 0
        End Function

        Private Function ObtenirDernierPrixAchat(produitId As Integer) As Decimal
            Dim sql As String = "SELECT TOP 1 PrixAchat FROM StockEntree WHERE ProduitId=@ProduitId ORDER BY DateEntree DESC"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@ProduitId", produitId)}
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            If v Is Nothing OrElse v Is DBNull.Value Then
                Return 0D
            End If
            Return Convert.ToDecimal(v)
        End Function

        Private Function GenererNumeroBon() As String
            Dim annee As String = Date.Now.Year.ToString()
            Dim sql As String = "" &
                "DECLARE @n INT; BEGIN TRAN; " &
                "IF EXISTS (SELECT 1 FROM BonApprovisionnementSequence WITH (UPDLOCK, HOLDLOCK) WHERE Annee=@Annee) " &
                "BEGIN UPDATE BonApprovisionnementSequence SET DernierNumero = DernierNumero + 1 WHERE Annee=@Annee; " &
                "SELECT @n = DernierNumero FROM BonApprovisionnementSequence WHERE Annee=@Annee; END " &
                "ELSE BEGIN INSERT INTO BonApprovisionnementSequence (Annee, DernierNumero) VALUES (@Annee, 1); SET @n=1; END " &
                "COMMIT; SELECT @n;"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@Annee", annee)}
            Dim v As Object = _dal.ExecuterScalaire(sql, CommandType.Text, p)
            Return "APP-" & annee & "-" & Convert.ToInt32(v).ToString("0000")
        End Function
    End Class
End Namespace
