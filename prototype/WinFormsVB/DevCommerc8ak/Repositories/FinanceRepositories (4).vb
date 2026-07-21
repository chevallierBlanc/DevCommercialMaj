Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic
Imports DevCommerc8ak.DevCommerc8ak.DTO

Namespace DevCommerc8ak.Finance
    Public Class CategorieDepenseRepository
        Private ReadOnly _dal As DAL
        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Sub Ajouter(libelle As String)
            Dim sql As String = "IF NOT EXISTS (SELECT 1 FROM CategoriesDepenses WHERE Libelle = @lib) " &
                               "INSERT INTO CategoriesDepenses (Libelle) VALUES (@lib)"
            Dim params As New List(Of SqlParameter) From {New SqlParameter("@lib", libelle)}
            _dal.ExecuterNonRequete(sql, CommandType.Text, params)
        End Sub

        Public Sub Supprimer(id As Integer)
            Dim sql As String = "DELETE FROM CategoriesDepenses WHERE Id = @id AND IsSystem = 0"
            Dim params As New List(Of SqlParameter) From {New SqlParameter("@id", id)}
            _dal.ExecuterNonRequete(sql, CommandType.Text, params)
        End Sub

        Public Function GetAll() As DataTable
            Return _dal.ExecuterTable("SELECT * FROM CategoriesDepenses ORDER BY Libelle", CommandType.Text, Nothing)
        End Function
    End Class

    Public Class DepenseRepositoryFinance
        Private ReadOnly _dal As DAL
        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Sub Ajouter(depense As DepenseDTOFinance)
            Dim sql As String = "INSERT INTO Depenses (Categorie, Montant, Devise, Description, DateDepense, Source, TypeDepense, CreePar) " &
                               "VALUES (@cat, @montant, @devise, @desc, @date, @source, @type, @user)"
            Dim descValue As Object = If(String.IsNullOrEmpty(depense.Description), DBNull.Value, DirectCast(depense.Description, Object))
            Dim userValue As Object = If(String.IsNullOrEmpty(depense.CreePar), DBNull.Value, DirectCast(depense.CreePar, Object))
            Dim params As New List(Of SqlParameter) From {
                New SqlParameter("@cat", depense.Categorie),
                New SqlParameter("@montant", depense.Montant),
                New SqlParameter("@devise", depense.Devise),
                New SqlParameter("@desc", descValue),
                New SqlParameter("@date", depense.DateDepense),
                New SqlParameter("@source", depense.Source),
                New SqlParameter("@type", depense.TypeDepense),
                New SqlParameter("@user", userValue)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, params)
        End Sub

        Public Function GetSommeParDevise(dateDepense As DateTime, devise As String, source As String) As Decimal
            Dim sql As String = "SELECT ISNULL(CAST(SUM(ISNULL(Montant,0)) AS DECIMAL(18,0)),0) FROM Depenses WHERE DateDepense = @date AND Devise = @devise AND Source = @source"
            Dim params As New List(Of SqlParameter) From {
                New SqlParameter("@date", dateDepense.Date),
                New SqlParameter("@devise", devise),
                New SqlParameter("@source", source)
            }
            Dim result As Object = _dal.ExecuterScalaire(sql, CommandType.Text, params)
            Return If(result Is DBNull.Value OrElse result Is Nothing, 0D, Convert.ToDecimal(result))
        End Function

        Public Function GetAll() As DataTable
            Return _dal.ExecuterTable("SELECT * FROM Depenses ORDER BY CreatedAt DESC", CommandType.Text, Nothing)
        End Function

        Public Function GetHistorique(annee As Integer, Optional mois As Integer = 0) As DataTable
            Dim sql As String = "" &
                "SELECT " &
                "    Id, " &
                "    DateDepense, " &
                "    Categorie AS NomCategorie, " &
                "    Description, " &
                "    Montant, " &
                "    Devise, " &
                "    Source, " &
                "    TypeDepense, " &
                "    CreePar, " &
                "    CreatedAt " &
                "FROM Depenses " &
                "WHERE YEAR(DateDepense) = @annee "
            Dim params As New List(Of SqlParameter) From {
                New SqlParameter("@annee", annee)
            }

            If mois > 0 Then
                sql &= "AND MONTH(DateDepense) = @mois "
                params.Add(New SqlParameter("@mois", mois))
            End If

            sql &= "ORDER BY DateDepense DESC, CreatedAt DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, params)
        End Function

        Public Function GetStatsParCategorie() As DataTable
            Return _dal.ExecuterTable("SELECT Categorie, ISNULL(CAST(SUM(ISNULL(Montant,0)) AS BIGINT),0) as Total FROM Depenses GROUP BY Categorie", CommandType.Text, Nothing)
        End Function

        Public Function GetRapportDepenses(annee As Integer, Optional mois As Integer = 0) As DataTable
            Dim sql As String = "SELECT Categorie, ISNULL(CAST(SUM(ISNULL(Montant,0)) AS BIGINT),0) as Total, Devise " &
                               "FROM Depenses " &
                               "WHERE YEAR(DateDepense) = @annee "

            Dim params As New List(Of SqlParameter) From {New SqlParameter("@annee", annee)}

            If mois > 0 Then
                sql &= "AND MONTH(DateDepense) = @mois "
                params.Add(New SqlParameter("@mois", mois))
            End If

            sql &= "GROUP BY Categorie, Devise ORDER BY Categorie"

            Return _dal.ExecuterTable(sql, CommandType.Text, params)
        End Function
    End Class

    Public Class BanqueRepository
        Private ReadOnly _dal As DAL
        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Sub AjouterOperation(op As BanqueDTO)
            Dim sql As String = "INSERT INTO Banque (TypeOperation, Montant, Devise, Description, DateOperation, Reference) " &
                               "VALUES (@type, @montant, @devise, @desc, @date, @ref)"
            Dim descValue As Object = If(String.IsNullOrEmpty(op.Description), DBNull.Value, DirectCast(op.Description, Object))
            Dim refValue As Object = If(String.IsNullOrEmpty(op.Reference), DBNull.Value, DirectCast(op.Reference, Object))
            Dim params As New List(Of SqlParameter) From {
                New SqlParameter("@type", op.TypeOperation),
                New SqlParameter("@montant", op.Montant),
                New SqlParameter("@devise", op.Devise),
                New SqlParameter("@desc", descValue),
                New SqlParameter("@date", op.DateOperation),
                New SqlParameter("@ref", refValue)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, params)
        End Sub

        Public Function GetSoldeParDevise(devise As String) As Decimal
            Dim sql As String = "SELECT CAST((SELECT ISNULL(SUM(Montant), 0) FROM Banque WHERE TypeOperation = 'Depot' AND Devise = @devise) - " &
                               "(SELECT ISNULL(SUM(Montant), 0) FROM Banque WHERE TypeOperation = 'Retrait' AND Devise = @devise) AS DECIMAL(18,0))"
            Dim params As New List(Of SqlParameter) From {New SqlParameter("@devise", devise)}
            Dim result As Object = _dal.ExecuterScalaire(sql, CommandType.Text, params)
            Return If(result Is DBNull.Value OrElse result Is Nothing, 0D, Convert.ToDecimal(result))
        End Function

        Public Function GetHistorique() As DataTable
            Return _dal.ExecuterTable("SELECT * FROM Banque ORDER BY CreatedAt DESC", CommandType.Text, Nothing)
        End Function
    End Class

    Public Class CaisseRepository
        Private ReadOnly _dal As DAL
        Public Sub New(dal As DAL)
            _dal = dal
            AssurerTableCloturesCaisse()
            AssurerTableHistoriqueStatutClotureCaisse()
        End Sub

        Private Sub AssurerTableCloturesCaisse()
            Dim sql As String =
                "IF OBJECT_ID('dbo.CloturesCaisse', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.CloturesCaisse (" &
                "ClotureCaisseId INT IDENTITY(1,1) PRIMARY KEY, " &
                "DateCaisse DATE NOT NULL, " &
                "UtilisateurId INT NULL, " &
                "NomUtilisateur NVARCHAR(80) NULL, " &
                "RoleSession NVARCHAR(80) NULL, " &
                "SoldeTheoriqueFC DECIMAL(18,2) NOT NULL CONSTRAINT DF_CloturesCaisse_SoldeTheoriqueFC DEFAULT(0), " &
                "MontantPhysiqueFC DECIMAL(18,2) NULL, " &
                "EcartFC DECIMAL(18,2) NULL, " &
                "SoldeTheoriqueUSD DECIMAL(18,2) NULL, " &
                "MontantPhysiqueUSD DECIMAL(18,2) NULL, " &
                "EcartUSD DECIMAL(18,2) NULL, " &
                "MotifEcart NVARCHAR(250) NULL, " &
                "Observation NVARCHAR(500) NULL, " &
                "Statut NVARCHAR(30) NOT NULL CONSTRAINT DF_CloturesCaisse_Statut DEFAULT('A_VERIFIER'), " &
                "ResponsableValidationId INT NULL, " &
                "ValidePar NVARCHAR(80) NULL, " &
                "ValideLe DATETIME2 NULL, " &
                "CreeLe DATETIME2 NOT NULL CONSTRAINT DF_CloturesCaisse_CreeLe DEFAULT(GETDATE()), " &
                "ModifieLe DATETIME2 NULL, " &
                "ModifiePar NVARCHAR(80) NULL, " &
                "EstCloturee BIT NOT NULL CONSTRAINT DF_CloturesCaisse_EstCloturee DEFAULT(0)); " &
                "CREATE UNIQUE INDEX UX_CloturesCaisse_Date_Utilisateur ON dbo.CloturesCaisse(DateCaisse, UtilisateurId) WHERE UtilisateurId IS NOT NULL; " &
                "END"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub

        Private Sub AssurerTableHistoriqueStatutClotureCaisse()
            Dim sql As String =
                "IF OBJECT_ID('dbo.HistoriqueStatutClotureCaisse', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.HistoriqueStatutClotureCaisse (" &
                "HistoriqueStatutId INT IDENTITY(1,1) PRIMARY KEY, " &
                "ClotureCaisseId INT NOT NULL, AncienStatut NVARCHAR(30) NULL, NouveauStatut NVARCHAR(30) NOT NULL, " &
                "MontantEcartAvant DECIMAL(18,2) NULL, MontantRegularise DECIMAL(18,2) NULL, MontantRestant DECIMAL(18,2) NULL, " &
                "Motif NVARCHAR(250) NULL, Observation NVARCHAR(500) NULL, UtilisateurResponsableId INT NULL, NomUtilisateurResponsable NVARCHAR(80) NULL, RoleResponsable NVARCHAR(80) NULL, " &
                "ModifieParUtilisateurId INT NULL, ModifiePar NVARCHAR(80) NULL, RoleModificateur NVARCHAR(80) NULL, ModifieLe DATETIME2 NOT NULL DEFAULT(GETDATE()), Reference NVARCHAR(100) NULL); " &
                "END"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub

        Public Function GetEncaisse(dateJour As DateTime, devise As String) As Decimal
            If String.Equals(devise, "USD", StringComparison.OrdinalIgnoreCase) Then
                Dim tauxUsd As Decimal? = ObtenirTauxUsdActuel()
                If Not tauxUsd.HasValue OrElse tauxUsd.Value <= 0D Then
                    Return 0D
                End If

                Dim sqlUsd As String = "SELECT ISNULL(CAST(SUM(CASE " &
                                       "WHEN UPPER(ISNULL(Devise, '')) = 'USD' THEN ISNULL(NULLIF(MontantRecu, 0), ISNULL(Montant, 0)) " &
                                       "ELSE 0 END) AS DECIMAL(18,2)),0) " &
                                       "FROM Paiements WHERE CAST(PayeLe AS DATE) = @date"
                Dim paramsUsd As New List(Of SqlParameter) From {
                    New SqlParameter("@date", dateJour.Date)
                }
                Dim totalFc As Object = _dal.ExecuterScalaire(sqlUsd, CommandType.Text, paramsUsd)
                Dim montantFc As Decimal = If(totalFc Is DBNull.Value OrElse totalFc Is Nothing, 0D, Convert.ToDecimal(totalFc))
                Return Decimal.Round(montantFc / tauxUsd.Value, 2, MidpointRounding.AwayFromZero)
            End If

            Dim sql As String = "SELECT ISNULL(CAST(SUM(ISNULL(Montant,0)) AS DECIMAL(18,0)),0) FROM Paiements WHERE CAST(PayeLe AS DATE) = @date"
            Dim params As New List(Of SqlParameter) From {
                New SqlParameter("@date", dateJour.Date)
            }
            Dim result As Object = _dal.ExecuterScalaire(sql, CommandType.Text, params)
            Return If(result Is DBNull.Value OrElse result Is Nothing, 0D, Convert.ToDecimal(result))
        End Function

        Public Function PeutCalculerMontantUsd() As Boolean
            Dim tauxUsd As Decimal? = ObtenirTauxUsdActuel()
            Return tauxUsd.HasValue AndAlso tauxUsd.Value > 0D
        End Function

        Public Function GetDerniereCloture() As DateTime?
            Dim sql As String = "SELECT MAX(DateCloture) FROM CloturesJournalieres"
            Dim result As Object = _dal.ExecuterScalaire(sql, CommandType.Text, Nothing)
            Return If(result Is DBNull.Value OrElse result Is Nothing, CType(Nothing, DateTime?), Convert.ToDateTime(result))
        End Function

        Public Sub EnregistrerCloture(dateCloture As DateTime, fc As Decimal, usd As Decimal)
            Dim sql As String = "INSERT INTO CloturesJournalieres (DateCloture, MontantTransfertFC, MontantTransfertUSD) VALUES (@date, @fc, @usd)"
            Dim params As New List(Of SqlParameter) From {
                New SqlParameter("@date", dateCloture.Date),
                New SqlParameter("@fc", fc),
                New SqlParameter("@usd", usd)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, params)
        End Sub

        Public Sub EnregistrerComptagePhysique(dateCaisse As DateTime, utilisateurId As Integer, nomUtilisateur As String, roleSession As String, soldeTheoriqueFc As Decimal, montantPhysiqueFc As Decimal, motif As String, observation As String)
            AssurerTableCloturesCaisse()
            AssurerTableHistoriqueStatutClotureCaisse()
            Dim ecartFc As Decimal = montantPhysiqueFc - soldeTheoriqueFc
            Dim statut As String = "CONFORME"
            If ecartFc < 0D Then
                statut = "MANQUANT"
            ElseIf ecartFc > 0D Then
                statut = "SURPLUS"
            End If

            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        Using cmd As New SqlCommand("" &
                            "IF EXISTS (SELECT 1 FROM dbo.CloturesCaisse WITH (UPDLOCK, HOLDLOCK) WHERE DateCaisse=@DateCaisse AND UtilisateurId=@UtilisateurId) " &
                            "BEGIN " &
                            "UPDATE dbo.CloturesCaisse SET " &
                            "SoldeTheoriqueFC=@SoldeTheoriqueFC, MontantPhysiqueFC=@MontantPhysiqueFC, EcartFC=@EcartFC, " &
                            "MotifEcart=@MotifEcart, Observation=@Observation, Statut=@Statut, " &
                            "ValidePar=@ValidePar, ValideLe=GETDATE(), ModifieLe=GETDATE(), ModifiePar=@ModifiePar, EstCloturee=1 " &
                            "WHERE DateCaisse=@DateCaisse AND UtilisateurId=@UtilisateurId " &
                            "END " &
                            "ELSE " &
                            "BEGIN " &
                            "INSERT INTO dbo.CloturesCaisse (DateCaisse, UtilisateurId, NomUtilisateur, RoleSession, SoldeTheoriqueFC, MontantPhysiqueFC, EcartFC, MotifEcart, Observation, Statut, ResponsableValidationId, ValidePar, ValideLe, ModifiePar, EstCloturee) " &
                            "VALUES (@DateCaisse, @UtilisateurId, @NomUtilisateur, @RoleSession, @SoldeTheoriqueFC, @MontantPhysiqueFC, @EcartFC, @MotifEcart, @Observation, @Statut, @ResponsableValidationId, @ValidePar, GETDATE(), @ModifiePar, 1) " &
                            "END", cn, tx)
                            cmd.Parameters.AddWithValue("@DateCaisse", dateCaisse.Date)
                            cmd.Parameters.AddWithValue("@UtilisateurId", utilisateurId)
                            cmd.Parameters.AddWithValue("@NomUtilisateur", If(String.IsNullOrWhiteSpace(nomUtilisateur), CType(DBNull.Value, Object), nomUtilisateur.Trim()))
                            cmd.Parameters.AddWithValue("@RoleSession", If(String.IsNullOrWhiteSpace(roleSession), CType(DBNull.Value, Object), roleSession.Trim()))
                            cmd.Parameters.AddWithValue("@SoldeTheoriqueFC", soldeTheoriqueFc)
                            cmd.Parameters.AddWithValue("@MontantPhysiqueFC", montantPhysiqueFc)
                            cmd.Parameters.AddWithValue("@EcartFC", ecartFc)
                            cmd.Parameters.AddWithValue("@MotifEcart", If(String.IsNullOrWhiteSpace(motif), CType(DBNull.Value, Object), motif.Trim()))
                            cmd.Parameters.AddWithValue("@Observation", If(String.IsNullOrWhiteSpace(observation), CType(DBNull.Value, Object), observation.Trim()))
                            cmd.Parameters.AddWithValue("@Statut", statut)
                            cmd.Parameters.AddWithValue("@ResponsableValidationId", If(SessionUtilisateur.UtilisateurId > 0, CType(SessionUtilisateur.UtilisateurId, Object), DBNull.Value))
                            cmd.Parameters.AddWithValue("@ValidePar", If(String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur), CType(DBNull.Value, Object), SessionUtilisateur.NomUtilisateur.Trim()))
                            cmd.Parameters.AddWithValue("@ModifiePar", If(String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur), CType(DBNull.Value, Object), SessionUtilisateur.NomUtilisateur.Trim()))
                            cmd.ExecuteNonQuery()
                        End Using

                        Using cmdHist As New SqlCommand("" &
                            "INSERT INTO dbo.HistoriqueStatutClotureCaisse (ClotureCaisseId, AncienStatut, NouveauStatut, MontantEcartAvant, MontantRegularise, MontantRestant, Motif, Observation, UtilisateurResponsableId, NomUtilisateurResponsable, RoleResponsable, ModifieParUtilisateurId, ModifiePar, RoleModificateur, Reference) " &
                            "SELECT TOP 1 ClotureCaisseId, NULL, @Statut, @EcartFC, 0, ABS(@EcartFC), @MotifEcart, @Observation, @UtilisateurId, @NomUtilisateur, @RoleSession, @ModifieParUtilisateurId, @ModifiePar, @RoleModificateur, 'COMPTAGE_PHYSIQUE' " &
                            "FROM dbo.CloturesCaisse WHERE DateCaisse=@DateCaisse AND UtilisateurId=@UtilisateurId ORDER BY ISNULL(ValideLe, CreeLe) DESC", cn, tx)
                            cmdHist.Parameters.AddWithValue("@Statut", statut)
                            cmdHist.Parameters.AddWithValue("@EcartFC", ecartFc)
                            cmdHist.Parameters.AddWithValue("@MotifEcart", If(String.IsNullOrWhiteSpace(motif), CType(DBNull.Value, Object), motif.Trim()))
                            cmdHist.Parameters.AddWithValue("@Observation", If(String.IsNullOrWhiteSpace(observation), CType(DBNull.Value, Object), observation.Trim()))
                            cmdHist.Parameters.AddWithValue("@UtilisateurId", utilisateurId)
                            cmdHist.Parameters.AddWithValue("@NomUtilisateur", If(String.IsNullOrWhiteSpace(nomUtilisateur), CType(DBNull.Value, Object), nomUtilisateur.Trim()))
                            cmdHist.Parameters.AddWithValue("@RoleSession", If(String.IsNullOrWhiteSpace(roleSession), CType(DBNull.Value, Object), roleSession.Trim()))
                            cmdHist.Parameters.AddWithValue("@ModifieParUtilisateurId", If(SessionUtilisateur.UtilisateurId > 0, CType(SessionUtilisateur.UtilisateurId, Object), DBNull.Value))
                            cmdHist.Parameters.AddWithValue("@ModifiePar", If(String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur), CType(DBNull.Value, Object), SessionUtilisateur.NomUtilisateur.Trim()))
                            cmdHist.Parameters.AddWithValue("@RoleModificateur", If(String.IsNullOrWhiteSpace(SessionUtilisateur.Role), CType(DBNull.Value, Object), SessionUtilisateur.Role.Trim()))
                            cmdHist.Parameters.AddWithValue("@DateCaisse", dateCaisse.Date)
                            cmdHist.ExecuteNonQuery()
                        End Using

                        tx.Commit()
                    Catch
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        Public Function ObtenirControleCaisse(dateCaisse As DateTime, utilisateurId As Integer) As DataRow
            AssurerTableCloturesCaisse()
            Dim sql As String =
                "SELECT TOP 1 ClotureCaisseId, DateCaisse, UtilisateurId, NomUtilisateur, RoleSession, " &
                "SoldeTheoriqueFC, MontantPhysiqueFC, EcartFC, MotifEcart, Observation, Statut, " &
                "ValidePar, ValideLe, CreeLe, ModifieLe, ModifiePar, EstCloturee " &
                "FROM dbo.CloturesCaisse " &
                "WHERE DateCaisse=@DateCaisse AND ((@UtilisateurId > 0 AND UtilisateurId=@UtilisateurId) OR (@UtilisateurId <= 0 AND UtilisateurId IS NULL)) " &
                "ORDER BY ISNULL(ValideLe, CreeLe) DESC"
            Dim params As New List(Of SqlParameter) From {
                New SqlParameter("@DateCaisse", dateCaisse.Date),
                New SqlParameter("@UtilisateurId", utilisateurId)
            }
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, params)
            If dt Is Nothing OrElse dt.Rows.Count = 0 Then
                Return Nothing
            End If
            Return dt.Rows(0)
        End Function

        Private Function ObtenirTauxUsdActuel() As Decimal?
            Dim sql As String = "SELECT TOP 1 TauxUsd FROM Parametres WHERE TauxUsd IS NOT NULL AND TauxUsd > 0"
            Dim result As Object = _dal.ExecuterScalaire(sql, CommandType.Text, Nothing)
            If result Is Nothing OrElse result Is DBNull.Value Then
                Return Nothing
            End If
            Return Convert.ToDecimal(result)
        End Function
    End Class
End Namespace
