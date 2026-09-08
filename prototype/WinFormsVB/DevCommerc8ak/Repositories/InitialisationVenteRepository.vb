Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.SqlClient

Namespace DevCommerc8ak
    Public Class InitialisationVenteRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerSchema()
        End Sub

        Public Sub AssurerSchema()
            Dim sql As String =
                "IF OBJECT_ID('dbo.InitialisationVenteSessions', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.InitialisationVenteSessions (" &
                "InitialisationVenteSessionId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, " &
                "ReferenceSession NVARCHAR(50) NOT NULL UNIQUE, DateDebut DATE NOT NULL, DateFin DATE NOT NULL, " &
                "ModeStock NVARCHAR(30) NOT NULL CONSTRAINT DF_InitVenteSessions_ModeStock_R DEFAULT('HISTORIQUE_UNIQUEMENT'), " &
                "Observation NVARCHAR(500) NULL, Statut NVARCHAR(20) NOT NULL CONSTRAINT DF_InitVenteSessions_Statut_R DEFAULT('BROUILLON'), " &
                "CreeLe DATETIME2 NOT NULL CONSTRAINT DF_InitVenteSessions_CreeLe_R DEFAULT(SYSDATETIME()), CreePar INT NOT NULL, Machine NVARCHAR(100) NULL, " &
                "ValideeLe DATETIME2 NULL, ValideePar INT NULL, AnnuleeLe DATETIME2 NULL, AnnuleePar INT NULL) " &
                "END " &
                "IF OBJECT_ID('dbo.InitialisationVenteLignes', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.InitialisationVenteLignes (" &
                "InitialisationVenteLigneId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, InitialisationVenteSessionId INT NOT NULL, DateVente DATETIME2 NOT NULL, ProduitId INT NOT NULL, " &
                "CodeProduit NVARCHAR(80) NULL, LibelleProduit NVARCHAR(200) NOT NULL, Categorie NVARCHAR(150) NULL, TypeVente NVARCHAR(80) NOT NULL, UniteCommerciale NVARCHAR(80) NULL, " &
                "QuantiteCommerciale DECIMAL(18,4) NOT NULL, PrixUnitaire DECIMAL(18,2) NOT NULL, MontantLigne DECIMAL(18,2) NOT NULL, QuantiteBase DECIMAL(18,4) NOT NULL, " &
                "CoutUnitaireBaseVente DECIMAL(18,4) NULL, BeneficeEstime DECIMAL(18,2) NULL, CreeLe DATETIME2 NOT NULL CONSTRAINT DF_InitVenteLignes_CreeLe_R DEFAULT(SYSDATETIME()), " &
                "CONSTRAINT FK_InitVenteLignes_Session_R FOREIGN KEY (InitialisationVenteSessionId) REFERENCES dbo.InitialisationVenteSessions(InitialisationVenteSessionId)) " &
                "END " &
                "IF OBJECT_ID('dbo.FacturesVente', 'U') IS NOT NULL AND COL_LENGTH('dbo.FacturesVente', 'OrigineVente') IS NULL ALTER TABLE dbo.FacturesVente ADD OrigineVente NVARCHAR(30) NULL; " &
                "IF OBJECT_ID('dbo.FacturesVente', 'U') IS NOT NULL AND COL_LENGTH('dbo.FacturesVente', 'InitialisationVenteSessionId') IS NULL ALTER TABLE dbo.FacturesVente ADD InitialisationVenteSessionId INT NULL; " &
                "IF OBJECT_ID('dbo.BusinessSequences', 'U') IS NULL " &
                "BEGIN CREATE TABLE dbo.BusinessSequences (SequenceKey NVARCHAR(120) NOT NULL PRIMARY KEY, Prefix NVARCHAR(30) NOT NULL, Periode NVARCHAR(20) NOT NULL, DernierNumero INT NOT NULL CONSTRAINT DF_BusinessSequences_DernierNumero_Init DEFAULT(0), CreeLe DATETIME2 NOT NULL CONSTRAINT DF_BusinessSequences_CreeLe_Init DEFAULT(SYSDATETIME()), ModifieLe DATETIME2 NOT NULL CONSTRAINT DF_BusinessSequences_ModifieLe_Init DEFAULT(SYSDATETIME())) END"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub

        Public Function CreerSession(dateDebut As Date, dateFin As Date, modeStock As String, observation As String, creePar As Integer, machine As String) As InitialisationVenteSessionDTO
            Dim reference As String = GenererReferenceSession(dateDebut)
            Dim sql As String =
                "INSERT INTO dbo.InitialisationVenteSessions (ReferenceSession, DateDebut, DateFin, ModeStock, Observation, CreePar, Machine) " &
                "VALUES (@ReferenceSession, @DateDebut, @DateFin, @ModeStock, @Observation, @CreePar, @Machine); " &
                "SELECT CAST(SCOPE_IDENTITY() AS INT);"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@ReferenceSession", reference),
                New SqlParameter("@DateDebut", dateDebut.Date),
                New SqlParameter("@DateFin", dateFin.Date),
                New SqlParameter("@ModeStock", modeStock),
                New SqlParameter("@Observation", If(String.IsNullOrWhiteSpace(observation), CType(DBNull.Value, Object), observation.Trim())),
                New SqlParameter("@CreePar", creePar),
                New SqlParameter("@Machine", If(String.IsNullOrWhiteSpace(machine), CType(DBNull.Value, Object), machine.Trim()))
            }
            Dim id As Integer = Convert.ToInt32(_dal.ExecuterScalaire(sql, CommandType.Text, p))
            Return ObtenirSession(id)
        End Function

        Public Sub MettreAJourSession(sessionId As Integer, dateDebut As Date, dateFin As Date, modeStock As String, observation As String)
            Dim sql As String =
                "UPDATE dbo.InitialisationVenteSessions SET DateDebut=@DateDebut, DateFin=@DateFin, ModeStock=@ModeStock, Observation=@Observation " &
                "WHERE InitialisationVenteSessionId=@SessionId AND Statut='BROUILLON'"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@SessionId", sessionId),
                New SqlParameter("@DateDebut", dateDebut.Date),
                New SqlParameter("@DateFin", dateFin.Date),
                New SqlParameter("@ModeStock", modeStock),
                New SqlParameter("@Observation", If(String.IsNullOrWhiteSpace(observation), CType(DBNull.Value, Object), observation.Trim()))
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Public Function ObtenirSession(sessionId As Integer) As InitialisationVenteSessionDTO
            Dim sql As String =
                "SELECT InitialisationVenteSessionId, ReferenceSession, DateDebut, DateFin, ModeStock, Observation, Statut, CreeLe, CreePar, Machine " &
                "FROM dbo.InitialisationVenteSessions WHERE InitialisationVenteSessionId=@SessionId"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@SessionId", sessionId)}
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            If dt.Rows.Count = 0 Then Return Nothing
            Return MapperSession(dt.Rows(0))
        End Function

        Public Function ListerSessions() As DataTable
            Dim sql As String =
                "SELECT InitialisationVenteSessionId, ReferenceSession, DateDebut, DateFin, ModeStock, Statut, CreeLe, Observation " &
                "FROM dbo.InitialisationVenteSessions ORDER BY CreeLe DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        Public Function AjouterLigne(ligne As InitialisationVenteLigneDTO) As Integer
            Dim sql As String =
                "DECLARE @NewIds TABLE (Id INT); " &
                "INSERT INTO dbo.InitialisationVenteLignes (InitialisationVenteSessionId, DateVente, ProduitId, CodeProduit, LibelleProduit, Categorie, TypeVente, UniteCommerciale, QuantiteCommerciale, PrixUnitaire, MontantLigne, QuantiteBase, CoutUnitaireBaseVente, BeneficeEstime) " &
                "OUTPUT inserted.InitialisationVenteLigneId INTO @NewIds " &
                "SELECT @SessionId, @DateVente, @ProduitId, @CodeProduit, @LibelleProduit, @Categorie, @TypeVente, @UniteCommerciale, @QuantiteCommerciale, @PrixUnitaire, @MontantLigne, @QuantiteBase, @CoutUnitaireBaseVente, @BeneficeEstime " &
                "WHERE EXISTS (SELECT 1 FROM dbo.InitialisationVenteSessions WHERE InitialisationVenteSessionId=@SessionId AND Statut='BROUILLON'); " &
                "SELECT ISNULL((SELECT TOP 1 Id FROM @NewIds), 0);"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@SessionId", ligne.InitialisationVenteSessionId),
                New SqlParameter("@DateVente", ligne.DateVente),
                New SqlParameter("@ProduitId", ligne.ProduitId),
                New SqlParameter("@CodeProduit", If(String.IsNullOrWhiteSpace(ligne.CodeProduit), CType(DBNull.Value, Object), ligne.CodeProduit.Trim())),
                New SqlParameter("@LibelleProduit", ligne.LibelleProduit),
                New SqlParameter("@Categorie", If(String.IsNullOrWhiteSpace(ligne.Categorie), CType(DBNull.Value, Object), ligne.Categorie.Trim())),
                New SqlParameter("@TypeVente", ligne.TypeVente),
                New SqlParameter("@UniteCommerciale", If(String.IsNullOrWhiteSpace(ligne.UniteCommerciale), CType(DBNull.Value, Object), ligne.UniteCommerciale.Trim())),
                New SqlParameter("@QuantiteCommerciale", ligne.QuantiteCommerciale),
                New SqlParameter("@PrixUnitaire", ligne.PrixUnitaire),
                New SqlParameter("@MontantLigne", ligne.MontantLigne),
                New SqlParameter("@QuantiteBase", ligne.QuantiteBase),
                New SqlParameter("@CoutUnitaireBaseVente", If(ligne.CoutUnitaireBaseVente.HasValue, CType(ligne.CoutUnitaireBaseVente.Value, Object), DBNull.Value)),
                New SqlParameter("@BeneficeEstime", If(ligne.BeneficeEstime.HasValue, CType(ligne.BeneficeEstime.Value, Object), DBNull.Value))
            }
            Dim id As Integer = Convert.ToInt32(_dal.ExecuterScalaire(sql, CommandType.Text, p))
            If id <= 0 Then Throw New InvalidOperationException("Une session validée ou annulée ne peut plus recevoir de lignes.")
            Return id
        End Function

        Public Sub SupprimerLigne(ligneId As Integer)
            Dim sql As String =
                "DELETE l FROM dbo.InitialisationVenteLignes l " &
                "INNER JOIN dbo.InitialisationVenteSessions s ON s.InitialisationVenteSessionId=l.InitialisationVenteSessionId " &
                "WHERE l.InitialisationVenteLigneId=@LigneId AND s.Statut='BROUILLON'"
            _dal.ExecuterNonRequete(sql, CommandType.Text, New List(Of SqlParameter) From {New SqlParameter("@LigneId", ligneId)})
        End Sub

        Public Function ListerLignes(sessionId As Integer) As DataTable
            Dim sql As String =
                "SELECT InitialisationVenteLigneId, InitialisationVenteSessionId, DateVente, ProduitId, CodeProduit, LibelleProduit, Categorie, TypeVente, UniteCommerciale, " &
                "QuantiteCommerciale, PrixUnitaire, MontantLigne, QuantiteBase, CoutUnitaireBaseVente, BeneficeEstime " &
                "FROM dbo.InitialisationVenteLignes WHERE InitialisationVenteSessionId=@SessionId ORDER BY DateVente, InitialisationVenteLigneId"
            Return _dal.ExecuterTable(sql, CommandType.Text, New List(Of SqlParameter) From {New SqlParameter("@SessionId", sessionId)})
        End Function

        Public Function ExistePeriodeValideeChevauchante(dateDebut As Date, dateFin As Date, Optional sessionIdExclu As Integer = 0) As Boolean
            Dim sql As String =
                "SELECT COUNT(1) FROM dbo.InitialisationVenteSessions " &
                "WHERE Statut='VALIDEE' AND DateDebut <= @DateFin AND DateFin >= @DateDebut " &
                "AND (@SessionIdExclu <= 0 OR InitialisationVenteSessionId <> @SessionIdExclu)"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@DateDebut", dateDebut.Date),
                New SqlParameter("@DateFin", dateFin.Date),
                New SqlParameter("@SessionIdExclu", sessionIdExclu)
            }
            Return Convert.ToInt32(_dal.ExecuterScalaire(sql, CommandType.Text, p)) > 0
        End Function

        Public Sub ValiderSession(sessionId As Integer, utilisateurId As Integer)
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        ValiderSessionTransaction(cn, tx, sessionId, utilisateurId)
                        tx.Commit()
                    Catch
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        Private Sub ValiderSessionTransaction(cn As SqlConnection, tx As SqlTransaction, sessionId As Integer, utilisateurId As Integer)
            Dim session As InitialisationVenteSessionDTO = ObtenirSessionTransaction(cn, tx, sessionId)
            If session Is Nothing Then Throw New InvalidOperationException("Session d'initialisation introuvable.")
            If Not String.Equals(session.Statut, "BROUILLON", StringComparison.OrdinalIgnoreCase) Then
                Throw New InvalidOperationException("Seule une session brouillon peut être validée.")
            End If
            If ExisteChevauchementTransaction(cn, tx, session.DateDebut, session.DateFin, sessionId) Then
                Throw New InvalidOperationException("Une session d'initialisation validée couvre déjà tout ou partie de cette période.")
            End If

            Dim lignes As DataTable = ListerLignesTransaction(cn, tx, sessionId)
            If lignes.Rows.Count = 0 Then Throw New InvalidOperationException("La session ne contient aucune ligne de vente.")

            For Each row As DataRow In lignes.Rows
                InsererVenteHistoriqueTransaction(cn, tx, session, row, utilisateurId)
            Next

            ExecuterNonQuery(cn, tx,
                "UPDATE dbo.InitialisationVenteSessions SET Statut='VALIDEE', ValideeLe=SYSDATETIME(), ValideePar=@UtilisateurId WHERE InitialisationVenteSessionId=@SessionId",
                New List(Of SqlParameter) From {New SqlParameter("@UtilisateurId", utilisateurId), New SqlParameter("@SessionId", sessionId)})
        End Sub

        Private Sub InsererVenteHistoriqueTransaction(cn As SqlConnection, tx As SqlTransaction, session As InitialisationVenteSessionDTO, row As DataRow, utilisateurId As Integer)
            Dim ligneId As Integer = Convert.ToInt32(row("InitialisationVenteLigneId"))
            Dim numeroFacture As String = session.ReferenceSession & "-" & ligneId.ToString("000000")
            Dim dateVente As Date = Convert.ToDateTime(row("DateVente"))
            Dim montant As Decimal = Convert.ToDecimal(row("MontantLigne"))
            Dim produitId As Integer = Convert.ToInt32(row("ProduitId"))
            Dim quantiteBase As Decimal = Convert.ToDecimal(row("QuantiteBase"))
            Dim quantiteCommerciale As Decimal = Convert.ToDecimal(row("QuantiteCommerciale"))
            Dim unite As String = Convert.ToString(row("UniteCommerciale"))

            Dim factureId As Integer = Convert.ToInt32(ExecuterScalar(cn, tx,
                "INSERT INTO dbo.FacturesVente (NumeroFacture, ClientId, SousTotal, MontantRemise, MontantTaxe, MontantTotal, Statut, CreePar, CreeLe, ValideLe, ModifierPar, OrigineVente, InitialisationVenteSessionId) " &
                "VALUES (@NumeroFacture, NULL, @Montant, 0, 0, @Montant, 'PAYEE', @UtilisateurId, @DateVente, @DateVente, @Utilisateur, 'INITIALISATION', @SessionId); " &
                "SELECT CAST(SCOPE_IDENTITY() AS INT);",
                New List(Of SqlParameter) From {
                    New SqlParameter("@NumeroFacture", numeroFacture),
                    New SqlParameter("@Montant", montant),
                    New SqlParameter("@UtilisateurId", utilisateurId),
                    New SqlParameter("@DateVente", dateVente),
                    New SqlParameter("@Utilisateur", ObtenirNomUtilisateur()),
                    New SqlParameter("@SessionId", session.InitialisationVenteSessionId)
                }))

            ExecuterNonQuery(cn, tx,
                "INSERT INTO dbo.LignesFactureVente (FactureVenteId, ProduitId, Quantite, QuantiteBase, TypeVente, PrixUnitaire, MontantRemise, MontantLigne, QuantiteSaisie, CoutUnitaireBaseVente) " &
                "VALUES (@FactureId, @ProduitId, @QuantiteBase, @QuantiteBase, @TypeVente, @PrixUnitaire, 0, @MontantLigne, @QuantiteCommerciale, @CoutUnitaireBaseVente)",
                New List(Of SqlParameter) From {
                    New SqlParameter("@FactureId", factureId),
                    New SqlParameter("@ProduitId", produitId),
                    New SqlParameter("@QuantiteBase", quantiteBase),
                    New SqlParameter("@TypeVente", Convert.ToString(row("TypeVente"))),
                    New SqlParameter("@PrixUnitaire", Convert.ToDecimal(row("PrixUnitaire"))),
                    New SqlParameter("@MontantLigne", montant),
                    New SqlParameter("@QuantiteCommerciale", quantiteCommerciale),
                    New SqlParameter("@CoutUnitaireBaseVente", If(row.IsNull("CoutUnitaireBaseVente"), CType(DBNull.Value, Object), row("CoutUnitaireBaseVente")))
                })

            ExecuterNonQuery(cn, tx,
                "INSERT INTO dbo.Paiements (FactureVenteId, ModePaiement, ReferencePaiement, Montant, MontantRecu, MonnaieRendue, Devise, PayePar, PayeLe, ModifierPar) " &
                "VALUES (@FactureId, 'INITIALISATION', @ReferencePaiement, @Montant, @Montant, 0, 'FC', @UtilisateurId, @DateVente, @Utilisateur)",
                New List(Of SqlParameter) From {
                    New SqlParameter("@FactureId", factureId),
                    New SqlParameter("@ReferencePaiement", numeroFacture),
                    New SqlParameter("@Montant", montant),
                    New SqlParameter("@UtilisateurId", utilisateurId),
                    New SqlParameter("@DateVente", dateVente),
                    New SqlParameter("@Utilisateur", ObtenirNomUtilisateur())
                })

            If String.Equals(session.ModeStock, "RECONSTITUTION_COMPLETE", StringComparison.OrdinalIgnoreCase) Then
                InsererSortieReconstitutionTransaction(cn, tx, numeroFacture, dateVente, produitId, quantiteCommerciale, quantiteBase, unite, utilisateurId, montant)
            End If
        End Sub

        Private Sub InsererSortieReconstitutionTransaction(cn As SqlConnection, tx As SqlTransaction, numeroFacture As String, dateVente As Date, produitId As Integer, quantiteCommerciale As Decimal, quantiteBase As Decimal, unite As String, utilisateurId As Integer, montant As Decimal)
            Dim stockAvant As Decimal = ObtenirStockCourantTransaction(cn, tx, produitId)
            Dim stockApres As Decimal = stockAvant - quantiteBase
            ExecuterNonQuery(cn, tx,
                "INSERT INTO dbo.StockSortie (ProduitId, QuantiteSaisie, Unite, QuantiteBase, DateSortie, Source, RefSource, CreePar, NumeroSortie, TypeVente, PrixUnitaire, MontantLigne, StatutPaiement, MontantPaye, ResteAPayer, Observation) " &
                "VALUES (@ProduitId, @QuantiteSaisie, @Unite, @QuantiteBase, @DateSortie, 'INITIALISATION_VENTE', @RefSource, @CreePar, @NumeroSortie, 'INITIALISATION', 0, @MontantLigne, 'PAYE', @MontantLigne, 0, @Observation)",
                New List(Of SqlParameter) From {
                    New SqlParameter("@ProduitId", produitId),
                    New SqlParameter("@QuantiteSaisie", quantiteCommerciale),
                    New SqlParameter("@Unite", If(String.IsNullOrWhiteSpace(unite), CType(DBNull.Value, Object), unite)),
                    New SqlParameter("@QuantiteBase", quantiteBase),
                    New SqlParameter("@DateSortie", dateVente),
                    New SqlParameter("@RefSource", numeroFacture),
                    New SqlParameter("@CreePar", utilisateurId),
                    New SqlParameter("@NumeroSortie", numeroFacture),
                    New SqlParameter("@MontantLigne", montant),
                    New SqlParameter("@Observation", "Reconstitution stock depuis initialisation des ventes")
                })

            ExecuterNonQuery(cn, tx,
                "INSERT INTO dbo.MouvementsStock (NumeroMouvement, ProduitId, TypeMouvement, Quantite, QuantiteBase, Unite, StockAvant, StockApres, Reference, Observation, EffectuePar, ModifierPar, EffectueLe) " &
                "VALUES (@NumeroMouvement, @ProduitId, 'SORTIE', @QuantiteBase, @QuantiteBase, @Unite, @StockAvant, @StockApres, @Reference, @Observation, @EffectuePar, @ModifierPar, @DateVente)",
                New List(Of SqlParameter) From {
                    New SqlParameter("@NumeroMouvement", numeroFacture),
                    New SqlParameter("@ProduitId", produitId),
                    New SqlParameter("@QuantiteBase", quantiteBase),
                    New SqlParameter("@Unite", If(String.IsNullOrWhiteSpace(unite), CType(DBNull.Value, Object), unite)),
                    New SqlParameter("@StockAvant", stockAvant),
                    New SqlParameter("@StockApres", stockApres),
                    New SqlParameter("@Reference", numeroFacture),
                    New SqlParameter("@Observation", "Reconstitution stock depuis initialisation des ventes"),
                    New SqlParameter("@EffectuePar", utilisateurId),
                    New SqlParameter("@ModifierPar", ObtenirNomUtilisateur()),
                    New SqlParameter("@DateVente", dateVente)
                })
        End Sub

        Private Function GenererReferenceSession(dateDebut As Date) As String
            Dim periode As String = dateDebut.ToString("yyyyMM")
            Dim prefix As String = "INIT-VENTE-" & periode
            Dim sql As String =
                "DECLARE @SequenceKey NVARCHAR(120) = N'InitialisationVente:' + @Periode; " &
                "DECLARE @LockResult INT; " &
                "EXEC @LockResult = sp_getapplock @Resource=@SequenceKey, @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=10000; " &
                "IF @LockResult < 0 BEGIN RAISERROR('Impossible de réserver une référence d''initialisation.', 16, 1); RETURN; END " &
                "IF NOT EXISTS (SELECT 1 FROM dbo.BusinessSequences WHERE SequenceKey=@SequenceKey) " &
                "BEGIN INSERT INTO dbo.BusinessSequences (SequenceKey, Prefix, Periode, DernierNumero) VALUES (@SequenceKey, N'INIT-VENTE', @Periode, 0); END " &
                "UPDATE dbo.BusinessSequences SET DernierNumero = DernierNumero + 1, ModifieLe = SYSDATETIME() OUTPUT inserted.DernierNumero WHERE SequenceKey=@SequenceKey;"
            Dim p As New List(Of SqlParameter) From {New SqlParameter("@Periode", periode)}
            Dim numero As Integer = Convert.ToInt32(_dal.ExecuterScalaire(sql, CommandType.Text, p))
            Return prefix & "-" & numero.ToString("000")
        End Function

        Private Function ObtenirSessionTransaction(cn As SqlConnection, tx As SqlTransaction, sessionId As Integer) As InitialisationVenteSessionDTO
            Dim dt As DataTable = ExecuterTable(cn, tx,
                "SELECT InitialisationVenteSessionId, ReferenceSession, DateDebut, DateFin, ModeStock, Observation, Statut, CreeLe, CreePar, Machine FROM dbo.InitialisationVenteSessions WITH (UPDLOCK, HOLDLOCK) WHERE InitialisationVenteSessionId=@SessionId",
                New List(Of SqlParameter) From {New SqlParameter("@SessionId", sessionId)})
            If dt.Rows.Count = 0 Then Return Nothing
            Return MapperSession(dt.Rows(0))
        End Function

        Private Function ListerLignesTransaction(cn As SqlConnection, tx As SqlTransaction, sessionId As Integer) As DataTable
            Return ExecuterTable(cn, tx,
                "SELECT InitialisationVenteLigneId, DateVente, ProduitId, TypeVente, UniteCommerciale, QuantiteCommerciale, PrixUnitaire, MontantLigne, QuantiteBase, CoutUnitaireBaseVente FROM dbo.InitialisationVenteLignes WHERE InitialisationVenteSessionId=@SessionId ORDER BY InitialisationVenteLigneId",
                New List(Of SqlParameter) From {New SqlParameter("@SessionId", sessionId)})
        End Function

        Private Function ExisteChevauchementTransaction(cn As SqlConnection, tx As SqlTransaction, dateDebut As Date, dateFin As Date, sessionIdExclu As Integer) As Boolean
            Dim v As Object = ExecuterScalar(cn, tx,
                "SELECT COUNT(1) FROM dbo.InitialisationVenteSessions WITH (UPDLOCK, HOLDLOCK) WHERE Statut='VALIDEE' AND DateDebut <= @DateFin AND DateFin >= @DateDebut AND InitialisationVenteSessionId <> @SessionIdExclu",
                New List(Of SqlParameter) From {
                    New SqlParameter("@DateDebut", dateDebut.Date),
                    New SqlParameter("@DateFin", dateFin.Date),
                    New SqlParameter("@SessionIdExclu", sessionIdExclu)
                })
            Return Convert.ToInt32(v) > 0
        End Function

        Private Function ObtenirStockCourantTransaction(cn As SqlConnection, tx As SqlTransaction, produitId As Integer) As Decimal
            Dim v As Object = ExecuterScalar(cn, tx, "SELECT ISNULL(QuantiteStock, 0) FROM dbo.vStockProduit WHERE ProduitId=@ProduitId", New List(Of SqlParameter) From {New SqlParameter("@ProduitId", produitId)})
            If v Is Nothing OrElse Convert.IsDBNull(v) Then Return 0D
            Return Convert.ToDecimal(v)
        End Function

        Private Function MapperSession(row As DataRow) As InitialisationVenteSessionDTO
            Return New InitialisationVenteSessionDTO With {
                .InitialisationVenteSessionId = Convert.ToInt32(row("InitialisationVenteSessionId")),
                .ReferenceSession = Convert.ToString(row("ReferenceSession")),
                .DateDebut = Convert.ToDateTime(row("DateDebut")),
                .DateFin = Convert.ToDateTime(row("DateFin")),
                .ModeStock = Convert.ToString(row("ModeStock")),
                .Observation = If(row.IsNull("Observation"), String.Empty, Convert.ToString(row("Observation"))),
                .Statut = Convert.ToString(row("Statut")),
                .CreeLe = Convert.ToDateTime(row("CreeLe")),
                .CreePar = Convert.ToInt32(row("CreePar")),
                .Machine = If(row.IsNull("Machine"), String.Empty, Convert.ToString(row("Machine")))
            }
        End Function

        Private Function ExecuterScalar(cn As SqlConnection, tx As SqlTransaction, sql As String, p As List(Of SqlParameter)) As Object
            Using cmd As New SqlCommand(sql, cn, tx)
                If p IsNot Nothing Then cmd.Parameters.AddRange(p.ToArray())
                Return cmd.ExecuteScalar()
            End Using
        End Function

        Private Sub ExecuterNonQuery(cn As SqlConnection, tx As SqlTransaction, sql As String, p As List(Of SqlParameter))
            Using cmd As New SqlCommand(sql, cn, tx)
                If p IsNot Nothing Then cmd.Parameters.AddRange(p.ToArray())
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Function ExecuterTable(cn As SqlConnection, tx As SqlTransaction, sql As String, p As List(Of SqlParameter)) As DataTable
            Using cmd As New SqlCommand(sql, cn, tx)
                If p IsNot Nothing Then cmd.Parameters.AddRange(p.ToArray())
                Using da As New SqlDataAdapter(cmd)
                    Dim dt As New DataTable()
                    da.Fill(dt)
                    Return dt
                End Using
            End Using
        End Function

        Private Function ObtenirNomUtilisateur() As String
            If String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur) Then Return "SYSTEM"
            Return SessionUtilisateur.NomUtilisateur.Trim()
        End Function
    End Class
End Namespace
