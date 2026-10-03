Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.IO
Imports System.Linq

Namespace DevCommerc8ak
    Public Class CommercialResetTableVolume
        Public Property NomTable As String
        Public Property RoleMetier As String
        Public Property Conserver As Boolean
        Public Property Raison As String
        Public Property OrdreSuppression As Integer
        Public Property NombreLignes As Long
    End Class

    Public Class CommercialResetPreview
        Public Property Tables As List(Of CommercialResetTableVolume)
        Public Property DossierSauvegarde As String
        Public ReadOnly Property TotalLignesTransactionnelles As Long
            Get
                If Tables Is Nothing Then Return 0
                Return Tables.Where(Function(t) Not t.Conserver).Sum(Function(t) t.NombreLignes)
            End Get
        End Property
    End Class

    Public Class CommercialResetResult
        Public Property Success As Boolean
        Public Property BackupFilePath As String
        Public Property Message As String
        Public Property TablesTraitees As List(Of CommercialResetTableVolume)
    End Class

    Friend Class CommercialResetTriggerInfo
        Public Property TriggerSchema As String
        Public Property TriggerName As String
        Public Property TableName As String
    End Class

    Public Class CommercialResetService
        Private Const AppLockName As String = "ERPCommercial_ResetCommercial"

        Private ReadOnly _connectionString As String
        Private ReadOnly _dal As DAL
        Private ReadOnly _backupService As BackupService
        Private ReadOnly _log As New ProductionLogService()

        Private ReadOnly _tablesTransactionnelles As List(Of CommercialResetTableVolume)
        Private ReadOnly _tablesConservees As List(Of CommercialResetTableVolume)

        Public Sub New(Optional connectionString As String = Nothing)
            _connectionString = If(String.IsNullOrWhiteSpace(connectionString),
                                   ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString,
                                   connectionString)
            _dal = New DAL(_connectionString)
            _backupService = New BackupService(_connectionString)
            _tablesTransactionnelles = ConstruireTablesTransactionnelles()
            _tablesConservees = ConstruireTablesConservees()
        End Sub

        Public Function Previsualiser() As CommercialResetPreview
            VerifierSuperAdmin()

            Dim tables As New List(Of CommercialResetTableVolume)()
            Using cn As New SqlConnection(_connectionString)
                cn.Open()
                For Each tableInfo As CommercialResetTableVolume In _tablesTransactionnelles
                    If TableExiste(cn, Nothing, tableInfo.NomTable) Then
                        tableInfo.NombreLignes = CompterLignes(cn, Nothing, tableInfo.NomTable)
                    Else
                        tableInfo.NombreLignes = 0
                    End If
                    tables.Add(CloneTableInfo(tableInfo))
                Next

                For Each tableInfo As CommercialResetTableVolume In _tablesConservees
                    If TableExiste(cn, Nothing, tableInfo.NomTable) Then
                        tableInfo.NombreLignes = CompterLignes(cn, Nothing, tableInfo.NomTable)
                    Else
                        tableInfo.NombreLignes = 0
                    End If
                    tables.Add(CloneTableInfo(tableInfo))
                Next
            End Using

            Return New CommercialResetPreview With {
                .Tables = tables,
                .DossierSauvegarde = _backupService.ObtenirDossierParDefaut()
            }
        End Function

        Public Function ExecuterReset(motDePasseSuperAdmin As String) As CommercialResetResult
            VerifierSuperAdmin()
            VerifierMotDePasseSuperAdmin(motDePasseSuperAdmin)

            AuditActionService.Enregistrer("SuperAdmin", "COMMERCIAL_RESET_REQUESTED", "Demande de réinitialisation commerciale par " & SessionUtilisateur.NomUtilisateur & ".", "ALERTE")

            Dim sauvegarde As BackupResult = _backupService.ExecuterSauvegarde()
            If sauvegarde Is Nothing OrElse Not sauvegarde.Success Then
                AuditActionService.Enregistrer("SuperAdmin", "COMMERCIAL_RESET_FAILED", "Sauvegarde préalable impossible : " & If(sauvegarde Is Nothing, "résultat nul", sauvegarde.Message), "ERREUR")
                Throw New InvalidOperationException("Sauvegarde préalable impossible. Aucune donnée n'a été modifiée.")
            End If

            If String.IsNullOrWhiteSpace(sauvegarde.FilePath) OrElse Not File.Exists(sauvegarde.FilePath) OrElse New FileInfo(sauvegarde.FilePath).Length <= 0 Then
                AuditActionService.Enregistrer("SuperAdmin", "COMMERCIAL_RESET_FAILED", "Sauvegarde non vérifiable : " & If(sauvegarde.FilePath, String.Empty), "ERREUR")
                Throw New InvalidOperationException("La sauvegarde SQL n'a pas pu être vérifiée. Aucune donnée commerciale n'a été réinitialisée.")
            End If

            Dim resultat As New CommercialResetResult With {
                .Success = False,
                .BackupFilePath = sauvegarde.FilePath,
                .TablesTraitees = New List(Of CommercialResetTableVolume)()
            }

            Using cn As New SqlConnection(_connectionString)
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction(IsolationLevel.Serializable)
                    Try
                        PrendreVerrouReset(cn, tx)

                        Dim produitsAvant As Long = CompterLignesSiExiste(cn, tx, "Produits")
                        Dim utilisateursAvant As Long = CompterLignesSiExiste(cn, tx, "Utilisateurs")
                        Dim conditionnementsAvant As Long = CompterLignesSiExiste(cn, tx, "ProduitConditionnements")
                        Dim typesVenteAvant As Long = CompterLignesSiExiste(cn, tx, "TypesVenteProduit")
                        If produitsAvant <= 0 Then Throw New InvalidOperationException("Réinitialisation refusée : aucun produit référentiel n'a été trouvé.")
                        If utilisateursAvant <= 0 Then Throw New InvalidOperationException("Réinitialisation refusée : aucun utilisateur référentiel n'a été trouvé.")

                        ' Les triggers de protection métier restent indispensables dans
                        ' l'exploitation normale. Pour le reset commercial global, ils sont
                        ' désactivés uniquement sur les tables transactionnelles, dans la
                        ' même transaction, puis réactivés avant le COMMIT.
                        Dim triggersDesactives As List(Of CommercialResetTriggerInfo) = DesactiverTriggersTransactionnels(cn, tx)

                        ' Les tables transactionnelles sont vidées explicitement, enfants avant parents.
                        ' On ne désactive pas les FK : si l'ordre est incomplet, SQL Server force le rollback.
                        For Each tableInfo As CommercialResetTableVolume In _tablesTransactionnelles.OrderBy(Function(t) t.OrdreSuppression)
                            If Not TableExiste(cn, tx, tableInfo.NomTable) Then
                                Continue For
                            End If

                            Dim avant As Long = CompterLignes(cn, tx, tableInfo.NomTable)
                            SupprimerToutesLesLignes(cn, tx, tableInfo.NomTable)
                            Dim copie As CommercialResetTableVolume = CloneTableInfo(tableInfo)
                            copie.NombreLignes = avant
                            resultat.TablesTraitees.Add(copie)
                        Next

                        ReactiverTriggersTransactionnels(cn, tx, triggersDesactives)
                        VerifierTriggersReactives(cn, tx, triggersDesactives)
                        VerifierApresReset(cn, tx, produitsAvant, utilisateursAvant, conditionnementsAvant, typesVenteAvant)
                        InsererAuditTransaction(cn, tx, "COMMERCIAL_RESET_COMPLETED", "Réinitialisation commerciale terminée. Sauvegarde : " & sauvegarde.FilePath, "OK")

                        tx.Commit()
                        NotifierChangementApresReset()
                        resultat.Success = True
                        resultat.Message = "Réinitialisation commerciale terminée avec succès."
                        _log.Warn("CommercialResetService", "ExecuterReset", resultat.Message & " Sauvegarde : " & sauvegarde.FilePath)
                    Catch ex As Exception
                        Try
                            tx.Rollback()
                        Catch rollbackEx As Exception
                            _log.Error("CommercialResetService", "ExecuterReset", "Rollback impossible après échec reset.", rollbackEx)
                        End Try

                        AuditActionService.Enregistrer("SuperAdmin", "COMMERCIAL_RESET_FAILED", "Échec de la réinitialisation commerciale : " & ex.Message & " | Sauvegarde : " & sauvegarde.FilePath, "ERREUR")
                        _log.Error("CommercialResetService", "ExecuterReset", "Échec reset commercial.", ex)
                        Throw
                    End Try
                End Using
            End Using

            Return resultat
        End Function

        Private Sub VerifierSuperAdmin()
            If Not String.Equals(If(SessionUtilisateur.Role, String.Empty).Trim(), "SUPERADMIN", StringComparison.OrdinalIgnoreCase) Then
                Throw New UnauthorizedAccessException("Cette opération est réservée au SUPERADMIN.")
            End If
            If SessionUtilisateur.UtilisateurId <= 0 OrElse String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur) Then
                Throw New UnauthorizedAccessException("Session SUPERADMIN invalide.")
            End If
        End Sub

        Private Sub VerifierMotDePasseSuperAdmin(motDePasse As String)
            If String.IsNullOrWhiteSpace(motDePasse) Then
                Throw New UnauthorizedAccessException("Mot de passe SUPERADMIN obligatoire.")
            End If

            ' La vérification réutilise le service d'authentification existant afin
            ' de conserver le hash actuel et d'éviter toute manipulation du secret.
            Dim dalAuth As New DAL(_connectionString)
            Dim serviceAuth As New UtilisateurService(New UtilisateurRepository(dalAuth), New RoleRepository(dalAuth), New SessionRepository(dalAuth))
            Dim resultat As AuthentificationResultat = serviceAuth.AuthentifierCompte(SessionUtilisateur.NomUtilisateur, motDePasse)
            If resultat Is Nothing OrElse resultat.Statut <> AuthentificationStatut.Succes OrElse resultat.Utilisateur Is Nothing OrElse resultat.Utilisateur.UtilisateurId <> SessionUtilisateur.UtilisateurId Then
                Throw New UnauthorizedAccessException("Mot de passe SUPERADMIN incorrect.")
            End If
        End Sub

        Private Sub PrendreVerrouReset(cn As SqlConnection, tx As SqlTransaction)
            Using cmd As New SqlCommand("DECLARE @r INT; EXEC @r = sp_getapplock @Resource=@Resource, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=0; SELECT @r;", cn, tx)
                cmd.Parameters.AddWithValue("@Resource", AppLockName)
                Dim code As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                If code < 0 Then
                    Throw New InvalidOperationException("Une autre réinitialisation commerciale est déjà en cours.")
                End If
            End Using
        End Sub

        Private Sub VerifierApresReset(cn As SqlConnection, tx As SqlTransaction, produitsAvant As Long, utilisateursAvant As Long, conditionnementsAvant As Long, typesVenteAvant As Long)
            If CompterLignesSiExiste(cn, tx, "Produits") <> produitsAvant Then
                Throw New InvalidOperationException("Contrôle post-reset refusé : le référentiel Produits a été modifié.")
            End If
            If CompterLignesSiExiste(cn, tx, "Utilisateurs") <> utilisateursAvant Then
                Throw New InvalidOperationException("Contrôle post-reset refusé : le référentiel Utilisateurs a été modifié.")
            End If
            If conditionnementsAvant > 0 AndAlso CompterLignesSiExiste(cn, tx, "ProduitConditionnements") <> conditionnementsAvant Then
                Throw New InvalidOperationException("Contrôle post-reset refusé : les conditionnements produit ont été modifiés.")
            End If
            If typesVenteAvant > 0 AndAlso CompterLignesSiExiste(cn, tx, "TypesVenteProduit") <> typesVenteAvant Then
                Throw New InvalidOperationException("Contrôle post-reset refusé : les types de vente ont été modifiés.")
            End If

            For Each tableInfo As CommercialResetTableVolume In _tablesTransactionnelles
                If TableExiste(cn, tx, tableInfo.NomTable) AndAlso CompterLignes(cn, tx, tableInfo.NomTable) <> 0 Then
                    Throw New InvalidOperationException("Contrôle post-reset refusé : la table " & tableInfo.NomTable & " contient encore des lignes.")
                End If
            Next

            If VueExiste(cn, tx, "vStockProduit") Then
                Using cmd As New SqlCommand("SELECT COUNT(*) FROM dbo.vStockProduit WHERE ABS(CAST(ISNULL(QuantiteStock, 0) AS DECIMAL(18,4))) > 0.0001", cn, tx)
                    If Convert.ToInt32(cmd.ExecuteScalar()) > 0 Then
                        Throw New InvalidOperationException("Contrôle post-reset refusé : au moins un produit conserve un stock non nul.")
                    End If
                End Using
            End If
        End Sub

        Private Sub InsererAuditTransaction(cn As SqlConnection, tx As SqlTransaction, actionName As String, description As String, statut As String)
            If Not TableExiste(cn, tx, "AuditActions") Then Return
            Using cmd As New SqlCommand("INSERT INTO dbo.AuditActions (Utilisateur, [Role], Module, [Action], [Description], Machine, [Statut]) VALUES (@Utilisateur, @Role, @Module, @Action, @Description, @Machine, @Statut)", cn, tx)
                cmd.Parameters.AddWithValue("@Utilisateur", If(String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur), "SYSTEM", SessionUtilisateur.NomUtilisateur))
                cmd.Parameters.AddWithValue("@Role", If(String.IsNullOrWhiteSpace(SessionUtilisateur.Role), "SYSTEM", SessionUtilisateur.Role))
                cmd.Parameters.AddWithValue("@Module", "SuperAdmin")
                cmd.Parameters.AddWithValue("@Action", actionName)
                cmd.Parameters.AddWithValue("@Description", LimiterTexte(description, 250))
                cmd.Parameters.AddWithValue("@Machine", Environment.MachineName)
                cmd.Parameters.AddWithValue("@Statut", statut)
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Function TableExiste(cn As SqlConnection, tx As SqlTransaction, nomTable As String) As Boolean
            Using cmd As New SqlCommand("SELECT CASE WHEN OBJECT_ID(@NomComplet, 'U') IS NULL THEN 0 ELSE 1 END", cn, tx)
                cmd.Parameters.AddWithValue("@NomComplet", "dbo." & nomTable)
                Return Convert.ToInt32(cmd.ExecuteScalar()) = 1
            End Using
        End Function

        Private Function VueExiste(cn As SqlConnection, tx As SqlTransaction, nomVue As String) As Boolean
            Using cmd As New SqlCommand("SELECT CASE WHEN OBJECT_ID(@NomComplet, 'V') IS NULL THEN 0 ELSE 1 END", cn, tx)
                cmd.Parameters.AddWithValue("@NomComplet", "dbo." & nomVue)
                Return Convert.ToInt32(cmd.ExecuteScalar()) = 1
            End Using
        End Function

        Private Function CompterLignesSiExiste(cn As SqlConnection, tx As SqlTransaction, nomTable As String) As Long
            If Not TableExiste(cn, tx, nomTable) Then Return 0
            Return CompterLignes(cn, tx, nomTable)
        End Function

        Private Function CompterLignes(cn As SqlConnection, tx As SqlTransaction, nomTable As String) As Long
            Using cmd As New SqlCommand("SELECT COUNT_BIG(*) FROM dbo." & Quoter(nomTable), cn, tx)
                Return Convert.ToInt64(cmd.ExecuteScalar())
            End Using
        End Function

        Private Sub SupprimerToutesLesLignes(cn As SqlConnection, tx As SqlTransaction, nomTable As String)
            Using cmd As New SqlCommand("DELETE FROM dbo." & Quoter(nomTable), cn, tx)
                cmd.CommandTimeout = 0
                cmd.ExecuteNonQuery()
            End Using
        End Sub

        Private Function DesactiverTriggersTransactionnels(cn As SqlConnection, tx As SqlTransaction) As List(Of CommercialResetTriggerInfo)
            Dim triggers As List(Of CommercialResetTriggerInfo) = LireTriggersTransactionnelsActifs(cn, tx)
            For Each triggerInfo As CommercialResetTriggerInfo In triggers
                Using cmd As New SqlCommand("DISABLE TRIGGER " & Quoter(triggerInfo.TriggerSchema) & "." & Quoter(triggerInfo.TriggerName) & " ON dbo." & Quoter(triggerInfo.TableName), cn, tx)
                    cmd.CommandTimeout = 0
                    cmd.ExecuteNonQuery()
                End Using
            Next
            Return triggers
        End Function

        Private Function LireTriggersTransactionnelsActifs(cn As SqlConnection, tx As SqlTransaction) As List(Of CommercialResetTriggerInfo)
            Dim resultat As New List(Of CommercialResetTriggerInfo)()
            Dim nomsTables As String = String.Join(",", _tablesTransactionnelles.Select(Function(t) "N'" & t.NomTable.Replace("'", "''") & "'"))
            If String.IsNullOrWhiteSpace(nomsTables) Then Return resultat

            Dim sql As String =
                "SELECT sch.name AS TriggerSchema, trg.name AS TriggerName, tbl.name AS TableName " &
                "FROM sys.triggers trg " &
                "INNER JOIN sys.objects obj ON obj.object_id = trg.object_id " &
                "INNER JOIN sys.tables tbl ON tbl.object_id = trg.parent_id " &
                "INNER JOIN sys.schemas sch ON sch.schema_id = obj.schema_id " &
                "WHERE trg.parent_class = 1 " &
                "AND trg.is_ms_shipped = 0 " &
                "AND trg.is_disabled = 0 " &
                "AND OBJECT_SCHEMA_NAME(tbl.object_id) = N'dbo' " &
                "AND tbl.name IN (" & nomsTables & ")"

            Using cmd As New SqlCommand(sql, cn, tx)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        resultat.Add(New CommercialResetTriggerInfo With {
                            .TriggerSchema = Convert.ToString(reader("TriggerSchema")),
                            .TriggerName = Convert.ToString(reader("TriggerName")),
                            .TableName = Convert.ToString(reader("TableName"))
                        })
                    End While
                End Using
            End Using

            Return resultat
        End Function

        Private Sub ReactiverTriggersTransactionnels(cn As SqlConnection, tx As SqlTransaction, triggers As List(Of CommercialResetTriggerInfo))
            If triggers Is Nothing Then Return
            For Each triggerInfo As CommercialResetTriggerInfo In triggers
                Using cmd As New SqlCommand("ENABLE TRIGGER " & Quoter(triggerInfo.TriggerSchema) & "." & Quoter(triggerInfo.TriggerName) & " ON dbo." & Quoter(triggerInfo.TableName), cn, tx)
                    cmd.CommandTimeout = 0
                    cmd.ExecuteNonQuery()
                End Using
            Next
        End Sub

        Private Sub VerifierTriggersReactives(cn As SqlConnection, tx As SqlTransaction, triggers As List(Of CommercialResetTriggerInfo))
            If triggers Is Nothing Then Return
            For Each triggerInfo As CommercialResetTriggerInfo In triggers
                Using cmd As New SqlCommand("SELECT COUNT(1) FROM sys.triggers trg INNER JOIN sys.objects obj ON obj.object_id = trg.object_id INNER JOIN sys.schemas sch ON sch.schema_id = obj.schema_id WHERE sch.name=@Schema AND trg.name=@TriggerName AND trg.is_disabled=1", cn, tx)
                    cmd.Parameters.AddWithValue("@Schema", triggerInfo.TriggerSchema)
                    cmd.Parameters.AddWithValue("@TriggerName", triggerInfo.TriggerName)
                    If Convert.ToInt32(cmd.ExecuteScalar()) > 0 Then
                        Throw New InvalidOperationException("Réinitialisation refusée : le trigger " & triggerInfo.TriggerSchema & "." & triggerInfo.TriggerName & " n'a pas été réactivé.")
                    End If
                End Using
            Next
        End Sub

        Private Function Quoter(nomTable As String) As String
            Return "[" & nomTable.Replace("]", "]]") & "]"
        End Function

        Private Function LimiterTexte(texte As String, longueurMax As Integer) As String
            If String.IsNullOrEmpty(texte) OrElse texte.Length <= longueurMax Then
                Return If(texte, String.Empty)
            End If
            Return texte.Substring(0, longueurMax)
        End Function

        Private Sub NotifierChangementApresReset()
            Try
                AppDataVersionService.Touch("STOCK", "FACTURES", "PAIEMENTS", "FINANCE", "PRODUITS", "TYPES_VENTE")
                AppEvents.OnDataChanged()
            Catch ex As Exception
                _log.Warn("CommercialResetService", "NotifierChangementApresReset", "Notification post-reset non bloquante : " & ex.Message)
            End Try
        End Sub

        Private Function CloneTableInfo(source As CommercialResetTableVolume) As CommercialResetTableVolume
            Return New CommercialResetTableVolume With {
                .NomTable = source.NomTable,
                .RoleMetier = source.RoleMetier,
                .Conserver = source.Conserver,
                .Raison = source.Raison,
                .OrdreSuppression = source.OrdreSuppression,
                .NombreLignes = source.NombreLignes
            }
        End Function

        Private Function ConstruireTablesTransactionnelles() As List(Of CommercialResetTableVolume)
            Return New List(Of CommercialResetTableVolume) From {
                Vider("RegularisationsEcartCaisse", "Caisse", "Régularisations rattachées aux clôtures caisse.", 10),
                Vider("HistoriqueStatutClotureCaisse", "Caisse", "Historique opérationnel des statuts de clôture caisse.", 20),
                Vider("InventaireLignes", "Inventaire", "Lignes enfants des inventaires physiques.", 30),
                Vider("BonApprovisionnementLignes", "Approvisionnement", "Lignes enfants des bons d'approvisionnement.", 40),
                Vider("LignesFactureVente", "Ventes", "Lignes enfants des factures de vente.", 50),
                Vider("Paiements", "Paiements", "Paiements rattachés aux factures.", 60),
                Vider("InitialisationVenteLignes", "Initialisation ventes", "Lignes importées/reconstituées des ventes historiques.", 70),
                Vider("StockInitialTechniqueLignes", "Stock initial", "Lignes transactionnelles de sessions de stock initial.", 80),
                Vider("CloturesCaisse", "Caisse", "Clôtures physiques de caisse.", 90),
                Vider("Inventaires", "Inventaire", "Sessions d'inventaire physique.", 100),
                Vider("BonsApprovisionnement", "Approvisionnement", "Bons d'approvisionnement transactionnels.", 110),
                Vider("FacturesVente", "Ventes", "Factures commerciales supprimées après suppression des lignes/paiements.", 120),
                Vider("InitialisationVenteSessions", "Initialisation ventes", "Sessions d'initialisation des ventes.", 130),
                Vider("StockInitialTechniqueSessions", "Stock initial", "Sessions de stock initial technique.", 140),
                Vider("MouvementsStock", "Stock", "Journal technique des mouvements physiques.", 150),
                Vider("StockEntree", "Stock", "Entrées stock alimentant vStockProduit.", 160),
                Vider("StockSortie", "Stock", "Sorties stock alimentant vStockProduit.", 170),
                Vider("StockPerte", "Stock", "Pertes stock alimentant vStockProduit.", 180),
                Vider("StockInventaire", "Inventaire", "Ancien inventaire transactionnel.", 190),
                Vider("Depenses", "Dépenses", "Dépenses d'exploitation transactionnelles.", 200),
                Vider("Banque", "Caisse/Banque", "Mouvements financiers opérationnels.", 210),
                Vider("CloturesJournalieres", "Caisse", "Clôtures journalières transactionnelles.", 220),
                Vider("FacturesFournisseurs", "Achats", "Factures fournisseur transactionnelles.", 230),
                Vider("StockSortieNonSynchronise", "Synchronisation", "File transactionnelle de sorties hors ligne.", 240),
                Vider("DepensesNonSynchronisees", "Synchronisation", "File transactionnelle de dépenses hors ligne.", 250),
                Vider("Notifications", "Opérationnel", "Notifications liées à l'activité avant reset.", 260),
                Vider("BusinessSequences", "Numérotation", "Séquences métier visibles à redémarrer proprement.", 270),
                Vider("FactureSequence", "Numérotation", "Séquence visible des factures après remise à zéro.", 280),
                Vider("StockSequence", "Numérotation", "Séquences visibles des mouvements/stock.", 290),
                Vider("MouvementSequence", "Numérotation", "Séquence visible des mouvements stock.", 300),
                Vider("BonApprovisionnementSequence", "Numérotation", "Séquence visible des bons d'approvisionnement.", 310)
            }
        End Function

        Private Function ConstruireTablesConservees() As List(Of CommercialResetTableVolume)
            Return New List(Of CommercialResetTableVolume) From {
                Conserver("Produits", "Référentiel produit", "Produits, prix et configurations legacy conservés."),
                Conserver("CategoriesProduits", "Référentiel produit", "Catégories produit conservées."),
                Conserver("UnitesMesure", "Conditionnements", "Référentiel des unités conservé."),
                Conserver("ProduitConditionnements", "Conditionnements", "Hiérarchie dynamique conservée."),
                Conserver("ProduitConditionnementMigrationDiagnostics", "Conditionnements", "Diagnostics de migration conservés."),
                Conserver("TypesVenteProduit", "Types de vente", "Types personnalisés et prix conservés."),
                Conserver("Clients", "Référentiel tiers", "Fichier client conservé pour le nouveau démarrage."),
                Conserver("Fournisseurs", "Référentiel tiers", "Fichier fournisseur conservé."),
                Conserver("CategoriesDepenses", "Référentiel dépenses", "Catégories de dépenses conservées."),
                Conserver("MotifSortie", "Référentiel stock", "Motifs de sortie conservés."),
                Conserver("Parametres", "Configuration", "Paramètres entreprise et applicatifs conservés."),
                Conserver("Magasins", "Configuration", "Informations magasin conservées."),
                Conserver("Utilisateurs", "Sécurité", "Comptes utilisateurs conservés."),
                Conserver("Roles", "Sécurité", "Rôles conservés."),
                Conserver("UtilisateurRoles", "Sécurité", "Associations multi-rôles conservées."),
                Conserver("InterfacesApplication", "Sécurité", "Référentiel des interfaces conservé."),
                Conserver("RoleInterfaces", "Sécurité", "Permissions par rôle conservées."),
                Conserver("UtilisateurSessions", "Sécurité", "Historique/session sécurité conservé."),
                Conserver("JournalAudit", "Audit", "Journal d'audit historique conservé."),
                Conserver("AuditActions", "Audit", "Journal des actions administratives conservé."),
                Conserver("JournalFraude", "Audit", "Journal fraude conservé."),
                Conserver("HistoriquePrixProduits", "Audit produit", "Historique de configuration prix conservé."),
                Conserver("AppDataVersions", "Technique", "Versions applicatives conservées."),
                Conserver("SchemaVersion", "Technique", "Versions de schéma conservées.")
            }
        End Function

        Private Function Vider(nomTable As String, roleMetier As String, raison As String, ordre As Integer) As CommercialResetTableVolume
            Return New CommercialResetTableVolume With {.NomTable = nomTable, .RoleMetier = roleMetier, .Conserver = False, .Raison = raison, .OrdreSuppression = ordre}
        End Function

        Private Function Conserver(nomTable As String, roleMetier As String, raison As String) As CommercialResetTableVolume
            Return New CommercialResetTableVolume With {.NomTable = nomTable, .RoleMetier = roleMetier, .Conserver = True, .Raison = raison, .OrdreSuppression = 0}
        End Function
    End Class
End Namespace
