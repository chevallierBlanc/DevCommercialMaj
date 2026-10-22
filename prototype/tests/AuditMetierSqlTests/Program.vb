Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports DevCommerc8ak.DevCommerc8ak
Imports DevCommerc8ak.DevCommerc8ak.DTO
Imports DevCommerc8ak.DevCommerc8ak.Services

Module Program
    Private cs As String
    Private dal As DAL
    Private produit As Integer
    Private compteur As Integer
    Sub Main()
        cs = Environment.GetEnvironmentVariable("ERP_PHASE1_TEST_CONNECTION")
        If Environment.GetEnvironmentVariable("ERP_PHASE1_TEST_ENABLE") <> "YES" OrElse String.IsNullOrWhiteSpace(cs) Then
            Throw New InvalidOperationException("Tests SQL explicitement désactivés. Utiliser uniquement une copie _Phase1_TEST.")
        End If
        Using cn As New SqlConnection(cs)
            cn.Open()
            If Not cn.Database.EndsWith("_Phase1_TEST", StringComparison.OrdinalIgnoreCase) Then Throw New InvalidOperationException("Base de test dédiée obligatoire.")
        End Using
        ' Aucun fichier de connexion de production ne doit être repris par le banc.
        If ConfigurationManager.ConnectionStrings("CommercialMagDB") IsNot Nothing Then Throw New InvalidOperationException("Ne pas copier app.config de production dans le banc de tests.")
        dal = New DAL(cs)
        TesterDisponibiliteEtAbsenceRejeu()
        Exec("IF NOT EXISTS(SELECT 1 FROM dbo.SchemaVersion WHERE Version=2026101301) THROW 51000, 'Migration integration 2026101301 requise sur la copie de test.', 1;")
        Exec("IF NOT EXISTS(SELECT 1 FROM dbo.SchemaVersion WHERE Version=2026101501) THROW 51000, 'Migration Vente et Encaissement 2026101501 requise sur la copie.', 1;")
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.Parametres WHERE GestionModesActive=0 AND DelegationPrixActive=0") = 1, "Copie de test en parcours classique, migrations 0902/0903/0904 requises")
        Dim suffix As String = Guid.NewGuid().ToString("N").Substring(0, 12)
        Dim role As Integer = Scalar("INSERT dbo.Roles(NomRole,EstActif) VALUES('TEST_' + @s,1); SELECT CAST(SCOPE_IDENTITY() AS INT)", suffix)
        Dim user As Integer = Scalar("INSERT dbo.Utilisateurs(NomUtilisateur,MotDePasseHash,MotDePasseSel,EstActif) SELECT TOP(1) 'TEST_' + @s,MotDePasseHash,MotDePasseSel,1 FROM dbo.Utilisateurs; SELECT CAST(SCOPE_IDENTITY() AS INT)", suffix)
        Exec("INSERT dbo.UtilisateurRoles(UtilisateurId,RoleId,EstActif,EstRolePrincipal) VALUES(" & user.ToString() & "," & role.ToString() & ",1,1)")
        Exec("INSERT dbo.RoleInterfaces(RoleId,InterfaceId) SELECT " & role.ToString() & ",InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface IN('FACTURIER','CAISSE','HISTORIQUE_FACTURES','FACTURE_CREER','FACTURE_MODIFIER','FACTURE_ANNULER','ENCAISSEMENT_CREER')")
        SessionUtilisateur.UtilisateurId = user
        SessionUtilisateur.RoleIdActif = role
        SessionUtilisateur.NomUtilisateur = "TEST_" & suffix
        SessionUtilisateur.Role = "TEST_" & suffix
        SessionUtilisateur.SessionId = Scalar("INSERT dbo.UtilisateurSessions(UtilisateurId,RoleIdActif,RoleSession,Poste) VALUES(" & user.ToString() & "," & role.ToString() & ",'TEST','TEST'); SELECT CAST(SCOPE_IDENTITY() AS INT)")
        produit = Scalar("SELECT TOP(1) ProduitId FROM dbo.Produits ORDER BY ProduitId")
        Dim operations As New FactureOperationService(dal)
        Dim facture As FactureVente = NouvelleFacture(suffix)
        Dim id As Integer = operations.Enregistrer(facture, Lignes(2D), String.Empty, Nothing)
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.JournalAudit WHERE [Action]='FACTURE_CREEE' AND EntiteId='" & id.ToString() & "'") = 1, "Création et audit")
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.JournalAudit WHERE [Action]='FACTURE_CREEE' AND EntiteId='" & id.ToString() & "' AND EffectuePar=" & user.ToString() & " AND SessionId=" & SessionUtilisateur.SessionId.ToString() & " AND ReferenceDocument=@s AND AnciennesValeurs IS NULL AND NouvellesValeurs IS NOT NULL", facture.NumeroFacture) = 1, "Référence, acteur et snapshot réels")
        Dim panneAffichage As EventHandler = Sub(sender, e) Throw New InvalidOperationException("Panne UI apres commit simulee")
        Dim apresCommit As FactureVente = NouvelleFacture("UI_APRES_COMMIT_" & suffix)
        AddHandler AppEvents.VenteCreee, panneAffichage
        Try
            Refus(Of FactureEnregistreeAffichageException)(Sub() operations.Enregistrer(apresCommit, Lignes(2D), "", Nothing))
        Finally
            RemoveHandler AppEvents.VenteCreee, panneAffichage
        End Try
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.FacturesVente WHERE NumeroFacture=@s", apresCommit.NumeroFacture) = 1, "Echec de notification distingue de l'echec SQL : facture presente une fois")
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.JournalAudit WHERE ReferenceDocument=@s AND [Action]='FACTURE_ENREGISTREMENT_REFUSE'", apresCommit.NumeroFacture) = 0, "Aucun faux refus SQL apres commit reussi")
        Dim brouillon As FactureVente = NouvelleFacture("BROUILLON_" & suffix)
        Dim brouillonId As Integer = operations.Enregistrer(brouillon, Lignes(2D), "", Nothing, Nothing, False)
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.FacturesVente WHERE FactureVenteId=" & brouillonId.ToString() & " AND ValideeParUtilisateurId IS NULL AND Statut='EN_ATTENTE'") = 1, "Mettre en attente ne fabrique pas de validation commerciale")
        Dim entete As DataRow = operations.LireEntetePourEdition(id)
        Verifier(DirectCast(entete("VersionOperation"), Byte()).SequenceEqual(operations.LireVersion(id)), "Version et en-tête du même chargement")
        Dim factureRemise As FactureVente = NouvelleFacture(suffix & "-remise")
        factureRemise.MontantRemise = 2D
        factureRemise.MontantTotal = 18D
        Dim nouveauClient As New Client With {.NomClient = "TEST_CLIENT_" & suffix, .Telephone = "", .Email = "", .Adresse = "", .EstActif = True}
        Dim remiseId As Integer = operations.Enregistrer(factureRemise, Lignes(2D), "", Nothing, nouveauClient)
        Dim enteteRemise As DataRow = operations.LireEntetePourEdition(remiseId)
        Verifier(Convert.ToDecimal(enteteRemise("MontantRemise")) = 2D AndAlso Convert.ToString(enteteRemise("Telephone")) = "", "Remise et client sans téléphone chargés")
        factureRemise.FactureVenteId = remiseId
        factureRemise.ClientId = Convert.ToInt32(enteteRemise("ClientId"))
        operations.Enregistrer(factureRemise, Lignes(2D), "Vérification sans changement", DirectCast(enteteRemise("VersionOperation"), Byte()))
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.Clients WHERE NomClient=@s", nouveauClient.NomClient) = 1, "Aucun client dupliqué par le service")
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.FacturesVente WHERE FactureVenteId=" & remiseId.ToString() & " AND MontantRemise=2 AND MontantTotal=18 AND ClientId=" & factureRemise.ClientId.Value.ToString()) = 1, "Client, remise et total conservés")
        Dim factureTaxe As FactureVente = NouvelleFacture(suffix & "-taxe")
        factureTaxe.MontantTaxe = 1D
        factureTaxe.MontantTotal = 21D
        Dim taxeId As Integer = operations.Enregistrer(factureTaxe, Lignes(2D), "", Nothing)
        Refus(Of InvalidOperationException)(Sub() operations.LireEntetePourEdition(taxeId))
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.FacturesVente WHERE FactureVenteId=" & taxeId.ToString() & " AND MontantTaxe=1 AND MontantTotal=21") = 1, "Taxe non représentable dans l'éditeur : aucune réécriture")
        facture.FactureVenteId = id
        Dim version As Byte() = operations.LireVersion(id)
        Refus(Of ArgumentException)(Sub() operations.Enregistrer(facture, Lignes(3D), "  ", version))
        Verifier(operations.LireVersion(id).SequenceEqual(version), "Refus sans changement métier")
        facture.SousTotal = 30D : facture.MontantTotal = 30D
        operations.Enregistrer(facture, Lignes(3D), "Correction client", version)
        Refus(Of DBConcurrencyException)(Sub() operations.Enregistrer(facture, Lignes(4D), "Édition obsolète", version))
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.JournalAudit WHERE [Action]='FACTURE_MODIFIEE' AND EntiteId='" & id.ToString() & "'") = 1, "Conflit sans faux succès")
        ' Deux transactions réelles partent de la même version. Une seule gagne.
        version = operations.LireVersion(id)
        Dim succes As Integer = 0
        Dim conflits As Integer = 0
        Parallel.For(0, 2, Sub(i)
            Try
                Dim f As FactureVente = NouvelleFacture(suffix)
                f.FactureVenteId = id
                operations.Enregistrer(f, Lignes(2D), "Test concurrent", version)
                Interlocked.Increment(succes)
            Catch ex As DBConcurrencyException
                Interlocked.Increment(conflits)
            End Try
        End Sub)
        Verifier(succes = 1 AndAlso conflits = 1, "Une seule édition concurrente")
        Exec("DELETE ri FROM dbo.RoleInterfaces ri JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=" & role.ToString() & " AND i.CodeInterface='FACTURE_MODIFIER'")
        Dim infrastructure As New SuperAdminRepository(dal)
        infrastructure.AssurerInfrastructure()
        Refus(Of UnauthorizedAccessException)(Sub() operations.Enregistrer(facture, Lignes(2D), "Droit retiré", operations.LireVersion(id)))
        Verifier(Not infrastructure.RoleAutoriseInterface(SessionUtilisateur.Role, "FACTURE_MODIFIER"), "Lecture sans réattribution")
        Exec("INSERT dbo.RoleInterfaces(RoleId,InterfaceId) SELECT " & role.ToString() & ",InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface IN('AUDIT_CONSULTER','SUPERADMIN_AUDIT')")
        Exec("INSERT dbo.JournalAudit([Action],Entite,EntiteId,Details,EffectuePar) VALUES('LEGACY_" & suffix & "','Facture','" & id.ToString() & "','Trace ancienne de test'," & user.ToString() & ")")
        Dim ancienneTrace As DataTable = infrastructure.ListerAuditActions(Nothing, Nothing, SessionUtilisateur.NomUtilisateur, "", "", "LEGACY_" & suffix, "")
        Verifier(ancienneTrace.Rows.Count = 1 AndAlso Convert.ToString(ancienneTrace.Rows(0)("Provenance")) = "JournalAudit" AndAlso Convert.ToString(ancienneTrace.Rows(0)("Statut")) = "HISTORIQUE", "Ancienne trace sans nouvelles colonnes toujours consultable")
        Refus(Of ArgumentException)(Sub() operations.Annuler(id, " "))
        operations.Annuler(id, "Erreur de saisie")
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.FacturesVente WHERE FactureVenteId=" & id.ToString() & " AND Statut='ANNULEE'") = 1, "Annulation contrôlée")
        Refus(Of InvalidOperationException)(Sub() operations.Annuler(id, "Annulation répétée"))
        Refus(Of SqlException)(Sub() Exec("DELETE dbo.JournalAudit WHERE EntiteId='" & id.ToString() & "'"))
        ' Injection d'une panne d'audit uniquement sur la base dédiée de test.
        Exec("CREATE TRIGGER dbo.TR_TEST_AuditFailure ON dbo.JournalAudit AFTER INSERT AS BEGIN THROW 51001, 'Panne audit simulée', 1; END")
        Try
            Dim f As FactureVente = NouvelleFacture(Guid.NewGuid().ToString("N"))
            f.SousTotal = 10D : f.MontantTotal = 10D
            Refus(Of SqlException)(Sub() operations.Enregistrer(f, Lignes(1D), String.Empty, Nothing))
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.FacturesVente WHERE NumeroFacture=@s", f.NumeroFacture) = 0, "Échec audit : rollback facture et lignes")
        Finally
            Exec("DROP TRIGGER dbo.TR_TEST_AuditFailure")
        End Try
        ' Ce stock ne sert qu'au scénario de paiement de la copie de test.
        Exec("INSERT dbo.StockEntree(IdStock,ProduitId,QuantiteSaisie,Unite,QuantiteBase,PrixAchat,Devise,Taux,DateEntree,CreePar) VALUES('TEST_" & suffix & "'," & produit.ToString() & ",1000000,'base',1000000,0,'FC',1,GETDATE()," & user.ToString() & ")")
        facture = NouvelleFacture(Guid.NewGuid().ToString("N"))
        id = operations.Enregistrer(facture, Lignes(2D), String.Empty, Nothing)
        Dim caisse As New FacturationService(dal)
        Exec("DELETE ri FROM dbo.RoleInterfaces ri JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=" & role.ToString() & " AND i.CodeInterface='ENCAISSEMENT_CREER'")
        Refus(Of UnauthorizedAccessException)(Sub() caisse.EncaisserFacture(id, "ESPECES", "TEST", 20D, 0D, "FC", user))
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.Paiements WHERE FactureVenteId=" & id.ToString()) = 0, "Permission refusée : aucun paiement")
        Exec("INSERT dbo.RoleInterfaces(RoleId,InterfaceId) SELECT " & role.ToString() & ",InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface='ENCAISSEMENT_CREER'")
        Exec("CREATE TRIGGER dbo.TR_TEST_AuditFailure ON dbo.JournalAudit AFTER INSERT AS BEGIN THROW 51001, 'Panne audit simulée', 1; END")
        Try
            Refus(Of SqlException)(Sub() caisse.EncaisserFacture(id, "ESPECES", "TEST", 20D, 0D, "FC", user))
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.Paiements WHERE FactureVenteId=" & id.ToString()) = 0, "Échec audit : rollback paiement")
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.FacturesVente WHERE FactureVenteId=" & id.ToString() & " AND Statut='EN_ATTENTE'") = 1, "Échec audit : statut inchangé")
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.StockSortie WHERE Source='VENTE' AND RefSource=@s", facture.NumeroFacture) = 0, "Échec audit : rollback sortie de stock")
        Finally
            Exec("DROP TRIGGER dbo.TR_TEST_AuditFailure")
        End Try
        caisse.EncaisserFacture(id, "ESPECES", "TEST", 20D, 0D, "FC", user)
        Try
            caisse.EncaisserFacture(id, "ESPECES", "TEST", 20D, 0D, "FC", user)
            Throw New Exception("Double encaissement accepté.")
        Catch ex As Exception
            Verifier(ex.Message = "Facture deja payee ou invalide.", "Refus du double encaissement pour la bonne raison")
        End Try
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.Paiements WHERE FactureVenteId=" & id.ToString()) = 1, "Double encaissement refusé")
        Refus(Of InvalidOperationException)(Sub() operations.Annuler(id, "Facture payée"))
        Dim factureConcurrente As FactureVente = NouvelleFacture(Guid.NewGuid().ToString("N"))
        Dim concurrentId As Integer = operations.Enregistrer(factureConcurrente, Lignes(2D), "", Nothing)
        Dim paiementsReussis As Integer = 0
        Dim paiementsRefuses As Integer = 0
        Parallel.For(0, 2, Sub(i)
            Try
                caisse.EncaisserFacture(concurrentId, "ESPECES", "TEST", 20D, 0D, "FC", user)
                Interlocked.Increment(paiementsReussis)
            Catch ex As Exception
                If ex.Message <> "Facture deja payee ou invalide." Then Throw
                Interlocked.Increment(paiementsRefuses)
            End Try
        End Sub)
        Verifier(paiementsReussis = 1 AndAlso paiementsRefuses = 1, "Deux encaissements concurrents : un seul succès")
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.Paiements WHERE FactureVenteId=" & concurrentId.ToString()) = 1, "Un seul paiement concurrent persisté")
        Dim disponible As Decimal
        Using cn As New SqlConnection(cs)
            cn.Open()
            Using cmd As New SqlCommand("SELECT ISNULL((SELECT SUM(QuantiteBase) FROM dbo.StockEntree WHERE ProduitId=@id),0)-ISNULL((SELECT SUM(QuantiteBase) FROM dbo.StockSortie WHERE ProduitId=@id),0)-ISNULL((SELECT SUM(QuantiteBase) FROM dbo.StockPerte WHERE ProduitId=@id),0)", cn)
                cmd.Parameters.AddWithValue("@id", produit)
                disponible = Convert.ToDecimal(cmd.ExecuteScalar())
            End Using
        End Using
        Dim factureStock As FactureVente = NouvelleFacture(Guid.NewGuid().ToString("N"))
        Dim doublons As IList(Of LigneFactureVente) = Lignes(disponible * 0.75D)
        doublons.Add(Lignes(disponible * 0.75D)(0))
        factureStock.SousTotal = doublons.Sum(Function(l) l.Quantite * l.PrixUnitaire)
        factureStock.MontantTotal = factureStock.SousTotal
        Dim stockId As Integer = operations.Enregistrer(factureStock, doublons, "", Nothing)
        Try
            caisse.EncaisserFacture(stockId, "ESPECES", "TEST", factureStock.MontantTotal, 0D, "FC", user)
            Throw New Exception("Stock négatif accepté.")
        Catch ex As Exception
            Verifier(ex.Message = "Stock insuffisant pour un produit.", "Besoin total des lignes du même produit contrôlé")
        End Try
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.Paiements WHERE FactureVenteId=" & stockId.ToString()) = 0, "Stock insuffisant : aucun paiement")
        Dim repoCloture As New AnalyseCaissePhysiqueRepository(dal)
        Dim serviceCloture As New AnalyseCaissePhysiqueService(repoCloture)
        Dim cloture As Integer = Scalar("INSERT dbo.CloturesCaisse(DateCaisse,UtilisateurId,NomUtilisateur,RoleSession,EcartFC) VALUES(GETDATE()," & user.ToString() & ",'TEST','TEST',-10); SELECT CAST(SCOPE_IDENTITY() AS INT)")
        Dim regularisation As New RegularisationCaissePhysiqueDTO With {.ClotureCaisseId = cloture, .NouveauStatut = "REGULARISE", .MontantRegularise = 5D, .Motif = "Correction contrôlée"}
        Refus(Of UnauthorizedAccessException)(Sub() serviceCloture.RegulariserCloture(regularisation))
        Exec("INSERT dbo.RoleInterfaces(RoleId,InterfaceId) SELECT " & role.ToString() & ",InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface IN('CAISSE_REGULARISER','ANALYSE_CAISSE_PHYSIQUE')")
        regularisation.Motif = " "
        Refus(Of ArgumentException)(Sub() serviceCloture.RegulariserCloture(regularisation))
        regularisation.Motif = "Correction contrôlée"
        Exec("CREATE TRIGGER dbo.TR_TEST_AuditFailure ON dbo.JournalAudit AFTER INSERT AS BEGIN THROW 51001, 'Panne audit simulée', 1; END")
        Try
            Refus(Of SqlException)(Sub() serviceCloture.RegulariserCloture(regularisation))
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.RegularisationsEcartCaisse WHERE ClotureCaisseId=" & cloture.ToString()) = 0, "Échec audit : rollback régularisation")
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.HistoriqueStatutClotureCaisse WHERE ClotureCaisseId=" & cloture.ToString()) = 0, "Échec audit : rollback historique clôture")
        Finally
            Exec("DROP TRIGGER dbo.TR_TEST_AuditFailure")
        End Try
        serviceCloture.RegulariserCloture(regularisation)
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.JournalAudit WHERE [Action]='CAISSE_ECART_REGULARISE' AND Entite='ClotureCaisse' AND EntiteId='" & cloture.ToString() & "'") = 1, "Correction de clôture et audit atomiques")
        ' Régression historique : les rôles intégrés étaient réamorcés quand
        ' leur dernière permission était retirée. La copie sert à vérifier ce cas.
        Dim roleFacturier As Integer = Scalar("SELECT RoleId FROM dbo.Roles WHERE NomRole='FACTURIER'")
        Exec("DELETE dbo.RoleInterfaces WHERE RoleId=" & roleFacturier.ToString())
        infrastructure.AssurerInfrastructure()
        infrastructure.AssurerInfrastructure()
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.RoleInterfaces WHERE RoleId=" & roleFacturier.ToString()) = 0, "Rôle intégré vidé : aucun droit rétabli")
        Verifier(infrastructure.RoleUtilisePermissions("FACTURIER"), "Rôle intégré sans droits toujours administré")
        TesterPhase2(role, user, suffix)
        SessionUtilisateur.Reinitialiser()
        Refus(Of UnauthorizedAccessException)(Sub() operations.Enregistrer(NouvelleFacture(Guid.NewGuid().ToString("N")), Lignes(1D), String.Empty, Nothing))
        Console.WriteLine("PASS SQL : " & compteur.ToString() & " scénarios. Les fixtures et audits restent dans la copie de test.")
    End Sub

    Private Sub TesterPhase2(role As Integer, user As Integer, suffix As String)
        Dim prix As New DelegationPrixService(dal)
        Verifier(Not String.IsNullOrWhiteSpace(prix.DiagnostiquerActions().ExplicationTarif), "Tarif officiel : refus explique avant activation ou attribution des droits")
        Dim modes As New ModeTravailService(dal)
        Dim configModes As New ConfigurationModesService(dal)
        Dim produitPrix As Integer = Scalar("INSERT dbo.Produits(CodeBarres,Libelle,PrixDetail,PrixGros,PrixAchat,ConversionUnite,TypeGestionStock,ContenuUnitePrincipale,ContenuUniteSecondaire,VenteGros,VenteDetail) VALUES('PRIX_' + @s,'PRIX TEST',15,150,100,10,'UNITE',10,1,1,1); SELECT CAST(SCOPE_IDENTITY() AS int)", suffix)
        Refus(Of UnauthorizedAccessException)(Sub() prix.Consulter(produitPrix, "piece"))
        Exec("INSERT dbo.RoleInterfaces(RoleId,InterfaceId) SELECT " & role.ToString() & ",InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface IN('PRIX_CONSULTER','PRIX_TARIF_MODIFIER','PRIX_DEMANDER','PRIX_APPROUVER','PRIX_REFUSER','PRIX_FACTURE_EXCEPTION','PARAMETRES','PARAMETRES_PRIX','PARAMETRES_SECURITE','MODE_FACTURATION','UTILISATEUR_MODES','ADMINISTRATION')")
        Dim initialPrix As ConfigurationPrix = prix.Configuration()
        Dim initialModes As ConfigurationModes = configModes.Charger()
        Dim sessionInitiale As Integer = SessionUtilisateur.SessionId
        Try
            Verifier(modes.Lister(user, role).Count = 0, "Gestion des modes desactivee : parcours classique conserve")
            Verifier(prix.ActionsDisponibles().Count = 0, "Delegation desactivee : commandes masquees meme avec privileges")
            prix.Configurer(New ConfigurationPrix With {.Active = True, .Demandes = True, .Immediate = True, .Variation = 10D}, initialPrix, "Configuration de fixture isolée")
            Dim t As TarifDelegue = prix.Consulter(produitPrix, "piece")
            Dim catalogue As List(Of TarifDelegue) = prix.ListerTarifsActifs(produitPrix)
            Verifier(catalogue.Exists(Function(x) x.TypeVente = "piece") AndAlso catalogue.Exists(Function(x) x.TypeVente = "gros") AndAlso Not catalogue.Exists(Function(x) x.TypeVente = "demi"), "Catalogue delegue : seulement types actifs reels")
            Exec("UPDATE dbo.Produits SET VenteDetail=0 WHERE ProduitId=" & produitPrix.ToString())
            Try
                Verifier(Not prix.ListerTarifsActifs(produitPrix).Exists(Function(x) x.TypeVente = "piece"), "Type desactive retire du dialogue")
                Refus(Of UnauthorizedAccessException)(Sub() prix.Modifier(t, 16D, "Type devenu inactif", False))
            Finally
                Exec("UPDATE dbo.Produits SET VenteDetail=1 WHERE ProduitId=" & produitPrix.ToString())
            End Try
            Verifier(prix.ActionsDisponibles().Contains("PRIX_TARIF_MODIFIER"), "Session valide et action explicite : commande disponible")
            Exec("DELETE ri FROM dbo.RoleInterfaces ri JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=" & role.ToString() & " AND i.CodeInterface='PRIX_CONSULTER'")
            Try
                Verifier(prix.ActionsDisponibles().Count = 0, "Consulter revoque : commandes refusees meme avec selection")
                Refus(Of UnauthorizedAccessException)(Sub() prix.Modifier(t, 16D, "Permission consulter revoquee", False))
                Refus(Of UnauthorizedAccessException)(Sub() prix.VerifierExceptionProposee(t, 16D, "Permission consulter revoquee"))
            Finally
                Exec("INSERT dbo.RoleInterfaces(RoleId,InterfaceId) SELECT " & role.ToString() & ",InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface='PRIX_CONSULTER'")
            End Try
            Dim sessionValide As Integer = SessionUtilisateur.SessionId
            SessionUtilisateur.SessionId = -1
            Try
                Verifier(prix.ActionsDisponibles().Count = 0, "Session inexistante : aucune commande disponible")
                Refus(Of UnauthorizedAccessException)(Sub() prix.VerifierExceptionProposee(t, 16D, "Session invalide"))
            Finally
                SessionUtilisateur.SessionId = sessionValide
            End Try
            Dim activePrix As ConfigurationPrix = prix.Configuration()
            prix.Configurer(New ConfigurationPrix With {.Active = True, .Demandes = True, .Immediate = False, .Variation = 10D}, activePrix, "Refuser les modifications immediates")
            Verifier(Not prix.ActionsDisponibles().Contains("PRIX_FACTURE_EXCEPTION"), "Exception directe masquee si immediate desactivee")
            Dim refusAvant As Integer = Scalar("SELECT COUNT(*) FROM dbo.JournalAudit WHERE [Action]='PRIX_EXCEPTION_REFUSEE' AND Resultat='REFUS' AND EffectuePar=" & user.ToString())
            Refus(Of UnauthorizedAccessException)(Sub() prix.VerifierExceptionProposee(t, 16D, "Exception directe interdite"))
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.JournalAudit WHERE [Action]='PRIX_EXCEPTION_REFUSEE' AND Resultat='REFUS' AND EffectuePar=" & user.ToString()) = refusAvant + 1, "Exception refusee tracee sans faux succes")
            Refus(Of UnauthorizedAccessException)(Sub() prix.Modifier(t, 16D, "Tarif direct interdit", False))
            Verifier(prix.Consulter(produitPrix, "piece").Prix = 15D, "Refus sans modification du catalogue")
            prix.Configurer(New ConfigurationPrix With {.Active = False, .Demandes = True, .Immediate = True, .Variation = 10D}, prix.Configuration(), "Delegation globalement inactive dans la fixture")
            Verifier(prix.ActionsDisponibles().Count = 0, "Desactivation globale masque toutes les commandes")
            Refus(Of UnauthorizedAccessException)(Sub() prix.Modifier(t, 16D, "Demande interdite globalement", True))
            prix.Configurer(activePrix, prix.Configuration(), "Restaurer les actions de la fixture")
            Verifier(t.Prix = 15D AndAlso t.Cout = 10D AndAlso t.QuantiteBase = 1D, "Coût et tarif comparés dans la même unité")
            Refus(Of ArgumentException)(Sub() prix.Modifier(t, 16D, "  ", False))
            Refus(Of UnauthorizedAccessException)(Sub() prix.Modifier(t, 9D, "Sous coût interdit", True))
            prix.Modifier(t, 16.5D, "Variation autorisée", False)
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.JournalAudit WHERE [Action]='PRIX_MODIFICATION_APPLIQUEE' AND EntiteId='" & produitPrix.ToString() & "' AND EffectuePar=" & user.ToString() & " AND AnciennesValeurs LIKE '%""Marge"":50%' AND NouvellesValeurs LIKE '%""Marge"":65%'") = 1, "Ancienne/nouvelle marge et auteur reel audites")
            Verifier(prix.Consulter(produitPrix, "piece").Prix = 16.5D, "Tarif officiel persisté")
            Refus(Of DBConcurrencyException)(Sub() prix.Modifier(t, 16D, "Tarif obsolète", False))
            t = prix.Consulter(produitPrix, "piece")
            Dim gagnants As Integer = 0
            Dim perdants As Integer = 0
            Parallel.For(0, 2, Sub(i)
                                  Try
                                      prix.Modifier(t, 17D + CDec(i) / 100D, "Modification concurrente", False)
                                      Interlocked.Increment(gagnants)
                                  Catch ex As DBConcurrencyException
                                      Interlocked.Increment(perdants)
                                  End Try
                              End Sub)
            Verifier(gagnants = 1 AndAlso perdants = 1, "Tarifs concurrents : aucun écrasement silencieux")
            t = prix.Consulter(produitPrix, "piece")
            Refus(Of UnauthorizedAccessException)(Sub() prix.Modifier(t, 20D, "Variation hors seuil", False))
            prix.Modifier(t, 20D, "Demande à approuver", True)
            Dim demande As Integer = Scalar("SELECT MAX(DemandeId) FROM dbo.DemandesModificationPrix WHERE ProduitId=" & produitPrix.ToString())
            Dim lecturesAvant As Integer = Scalar("SELECT COUNT(*) FROM dbo.JournalAudit")
            Verifier(prix.ConsulterTarifDemande(demande, "FACTURIER").Prix = t.Prix, "Detail demande : tarif courant relu par le service")
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.JournalAudit") = lecturesAvant, "Consultation sans evenement de succes fictif")
            Dim demandesAvant As Integer = Scalar("SELECT COUNT(*) FROM dbo.DemandesModificationPrix WHERE ProduitId=" & produitPrix.ToString())
            Dim auditsAvant As Integer = Scalar("SELECT COUNT(*) FROM dbo.JournalAudit WHERE [Action]='PRIX_MODIFICATION_DEMANDEE'")
            Parallel.For(0, 2, Sub(i) prix.Modifier(t, 20D, "Demande à approuver", True))
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.DemandesModificationPrix WHERE ProduitId=" & produitPrix.ToString()) = demandesAvant, "Rejeux concurrents de demande officielle sans doublon")
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.JournalAudit WHERE [Action]='PRIX_MODIFICATION_DEMANDEE'") = auditsAvant, "Rejeu sans double audit de succes")
            Verifier(prix.Consulter(produitPrix, "piece").Prix = t.Prix, "Une demande ne change pas le tarif actif")
            Refus(Of UnauthorizedAccessException)(Sub() prix.Decider(demande, True, "Auto-approbation interdite"))
            Dim reviewer As Integer = Scalar("INSERT dbo.Utilisateurs(NomUtilisateur,MotDePasseHash,MotDePasseSel,EstActif) SELECT TOP(1) 'REV_' + @s,MotDePasseHash,MotDePasseSel,1 FROM dbo.Utilisateurs; SELECT CAST(SCOPE_IDENTITY() AS int)", suffix)
            Exec("INSERT dbo.UtilisateurRoles(UtilisateurId,RoleId,EstActif,EstRolePrincipal) VALUES(" & reviewer.ToString() & "," & role.ToString() & ",1,1)")
            SessionUtilisateur.UtilisateurId = reviewer
            SessionUtilisateur.NomUtilisateur = "REV_" & suffix
            SessionUtilisateur.SessionId = Scalar("INSERT dbo.UtilisateurSessions(UtilisateurId,RoleIdActif,RoleSession,Poste) VALUES(" & reviewer.ToString() & "," & role.ToString() & ",'TEST','TEST'); SELECT CAST(SCOPE_IDENTITY() AS int)")
            Dim reviewerSession As Integer = SessionUtilisateur.SessionId
            gagnants = 0 : perdants = 0
            Parallel.For(0, 2, Sub(i)
                                  Try
                                      prix.Decider(demande, True, "Approbation par un autre utilisateur")
                                      Interlocked.Increment(gagnants)
                                  Catch ex As DBConcurrencyException
                                      Interlocked.Increment(perdants)
                                  End Try
                              End Sub)
            Verifier(gagnants = 1 AndAlso perdants = 1, "Une seule approbation concurrente")
            Verifier(prix.Consulter(produitPrix, "piece").Prix = 20D, "Prix approuvé appliqué")
            Refus(Of DBConcurrencyException)(Sub() prix.Decider(demande, True, "Double approbation"))
            SessionUtilisateur.UtilisateurId = user
            SessionUtilisateur.NomUtilisateur = "TEST_" & suffix
            SessionUtilisateur.SessionId = sessionInitiale
            t = prix.Consulter(produitPrix, "piece")
            prix.Modifier(t, 21D, "Demande qui va expirer", True)
            Dim expiree As Integer = Scalar("SELECT MAX(DemandeId) FROM dbo.DemandesModificationPrix WHERE ProduitId=" & produitPrix.ToString())
            Exec("UPDATE dbo.DemandesModificationPrix SET DemandeLe=DATEADD(DAY,-2,SYSUTCDATETIME()),ExpireLe=DATEADD(DAY,-1,SYSUTCDATETIME()) WHERE DemandeId=" & expiree.ToString())
            prix.Decider(expiree, True, "Expiration vérifiée")
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.DemandesModificationPrix WHERE DemandeId=" & expiree.ToString() & " AND Etat='EXPIREE'") = 1 AndAlso prix.Consulter(produitPrix, "piece").Prix = 20D, "Demande expirée : aucun prix appliqué")
            Exec("CREATE TRIGGER dbo.TR_TEST_AuditFailure ON dbo.JournalAudit AFTER INSERT AS BEGIN THROW 51001, 'Panne audit simulée', 1; END")
            Try
                Refus(Of SqlException)(Sub() prix.Modifier(t, 21D, "Panne audit", False))
            Finally
                Exec("DROP TRIGGER dbo.TR_TEST_AuditFailure")
            End Try
            Verifier(prix.Consulter(produitPrix, "piece").Prix = 20D, "Échec d'audit : rollback du tarif et de son historique")
            configModes.Enregistrer(New ConfigurationModes With {.Active = True, .Facturation = True, .Caisse = True, .Combine = True}, initialModes, "Modes de la fixture")
            Dim autorises As List(Of String) = modes.Lister(user, role)
            Verifier(autorises.Count = 2 AndAlso autorises.Contains("FACTURATION") AndAlso autorises.Contains("CAISSE"), "Modes historiques disponibles, combine absent sans attribution explicite")
            Dim roleCaisse As Integer = Scalar("INSERT dbo.Roles(NomRole,EstActif) VALUES('CAISSE_TEST_' + @s,1); SELECT CAST(SCOPE_IDENTITY() AS int)", suffix)
            Exec("INSERT dbo.UtilisateurRoles(UtilisateurId,RoleId,EstActif,EstRolePrincipal) VALUES(" & user.ToString() & "," & roleCaisse.ToString() & ",1,0)")
            Exec("INSERT dbo.RoleInterfaces(RoleId,InterfaceId) SELECT " & roleCaisse.ToString() & ",InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface IN('CAISSE','ENCAISSEMENT_CREER')")
            Dim modesCaisse As List(Of String) = modes.Lister(user, roleCaisse)
            Verifier(modesCaisse.Count = 1 AndAlso modesCaisse(0) = "CAISSE", "Role caissiere historique reconnectable sans MODE_CAISSE et sans union des roles")
            Dim diagnostics As Dictionary(Of Integer, String) = modes.DiagnostiquerRoles(user)
            Verifier(diagnostics.ContainsKey(roleCaisse) AndAlso diagnostics(roleCaisse).Contains("Créer une facture"), "Diagnostic par role avec droits manquants, sans cumul")
            Dim sessions As New SessionRepository(dal)
            TesterOuvertureSessions(role, suffix, sessions)
            Refus(Of UnauthorizedAccessException)(Sub() sessions.DemarrerSession(user, -1, "ROLE INVENTE"))
            Refus(Of InvalidOperationException)(Sub() sessions.DemarrerSession(user, roleCaisse, "FAUX NOM"))
            Dim modesActifs As ConfigurationModes = configModes.Charger()
            configModes.Enregistrer(New ConfigurationModes With {.Active = True, .Facturation = True, .Caisse = False, .Combine = True}, modesActifs, "Caisse separee desactivee dans la fixture")
            Refus(Of UnauthorizedAccessException)(Sub() modes.Lister(user, roleCaisse))
            configModes.Enregistrer(modesActifs, configModes.Charger(), "Restaurer le mode caisse de la fixture")
            Refus(Of UnauthorizedAccessException)(Sub() modes.Activer("FACTURATION_ET_CAISSE"))
            modes.Activer("FACTURATION")
            Verifier(prix.ActionsDisponibles().Count > 0, "Precondition : commandes actives avant expiration")
            Dim pingAvant As Object = Nothing
            Using cn As SqlConnection = dal.CreerConnexion()
                cn.Open()
                Using cmd As New SqlCommand("SELECT DernierPing FROM dbo.UtilisateurSessions WHERE SessionId=@s", cn)
                    cmd.Parameters.AddWithValue("@s", SessionUtilisateur.SessionId)
                    pingAvant = cmd.ExecuteScalar()
                End Using
                Using cmd As New SqlCommand("UPDATE dbo.UtilisateurSessions SET DernierPing=DATEADD(MINUTE,-31,SYSUTCDATETIME()) WHERE SessionId=@s", cn)
                    cmd.Parameters.AddWithValue("@s", SessionUtilisateur.SessionId)
                    cmd.ExecuteNonQuery()
                End Using
                Try
                    Verifier(prix.ActionsDisponibles().Count = 0, "Session expiree : aucune commande autorisee")
                    sessions.Ping(SessionUtilisateur.SessionId)
                    Verifier(prix.ActionsDisponibles().Count = 0, "Ping ne ressuscite pas une session expiree")
                Finally
                    Using cmd As New SqlCommand("UPDATE dbo.UtilisateurSessions SET DernierPing=@ping WHERE SessionId=@s", cn)
                        cmd.Parameters.AddWithValue("@ping", pingAvant)
                        cmd.Parameters.AddWithValue("@s", SessionUtilisateur.SessionId)
                        cmd.ExecuteNonQuery()
                    End Using
                End Try
            End Using
            Using cn As SqlConnection = dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Refus(Of UnauthorizedAccessException)(Sub() AutorisationActionService.Exiger(cn, tx, "ENCAISSEMENT_CREER", "CAISSE"))
                    tx.Rollback()
                End Using
            End Using
            Exec("INSERT dbo.RoleInterfaces(RoleId,InterfaceId) SELECT " & role.ToString() & ",InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface='MODE_COMBINE'")
            Refus(Of UnauthorizedAccessException)(Sub() modes.Activer("FACTURATION_ET_CAISSE"))
            Dim attribution As New AttributionModeService(dal)
            TesterDroitAdministrationModes(attribution, role, roleCaisse, user)
            Exec("INSERT dbo.RoleInterfaces(RoleId,InterfaceId) SELECT " & roleCaisse.ToString() & ",InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface IN('FACTURIER','FACTURE_CREER','MODE_COMBINE')")
            attribution.Attribuer(user, roleCaisse, True, False, "Sarah : attribution au role caisse seulement")
            Dim etatAttributions As DataTable = attribution.Lister(user)
            Dim caisseAttribuee As DataRow = etatAttributions.Select("RoleId=" & roleCaisse.ToString())(0)
            Dim facturierNonAttribue As DataRow = etatAttributions.Select("RoleId=" & role.ToString())(0)
            Verifier(CBool(caisseAttribuee("CombineEligible")) AndAlso CBool(caisseAttribuee("VenteEncaissementAutorise")) AndAlso Not CBool(facturierNonAttribue("VenteEncaissementAutorise")), "Administration : affiche distinctement Tatiana/CAISSIERE et Tatiana/FACTURIER")
            Verifier(modes.Lister(user, roleCaisse).Contains("FACTURATION_ET_CAISSE") AndAlso Not modes.Lister(user, role).Contains("FACTURATION_ET_CAISSE"), "Sarah : caisse autorisee, facturier non autorise malgre le meme compte")
            attribution.Attribuer(user, roleCaisse, False, True, "Fin du scenario Sarah")
            Dim sarah As Integer = Scalar("INSERT dbo.Utilisateurs(NomUtilisateur,MotDePasseHash,MotDePasseSel,EstActif) SELECT TOP(1) 'SARAH_' + @s,MotDePasseHash,MotDePasseSel,1 FROM dbo.Utilisateurs; SELECT CAST(SCOPE_IDENTITY() AS int)", suffix)
            Dim comptes As New UtilisateurRepository(dal)
            comptes.MettreAJourRolesUtilisateur(sarah, New List(Of Integer) From {role, roleCaisse}, role)
            attribution.Attribuer(sarah, roleCaisse, True, False, "Attribution individuelle sur second role")
            comptes.MettreAJour(sarah, "SARAH_" & suffix, True, role, Nothing, Nothing, New List(Of Integer) From {role, roleCaisse})
            Verifier(modes.Lister(sarah, roleCaisse).Contains("FACTURATION_ET_CAISSE"), "Edition du compte conserve l'attribution du role conserve")
            comptes.MettreAJourRolesUtilisateur(sarah, New List(Of Integer) From {role}, role)
            comptes.MettreAJourRolesUtilisateur(sarah, New List(Of Integer) From {role, roleCaisse}, role)
            Verifier(Not modes.Lister(sarah, roleCaisse).Contains("FACTURATION_ET_CAISSE"), "Retrait puis reactivation d'un role ne ressuscite pas son attribution sensible")
            Verifier(Not modes.Lister(user, role).Contains("FACTURATION_ET_CAISSE"), "Eligibilite de role seule ne donne pas l'attribution individuelle")
            attribution.Attribuer(user, role, True, False, "Attribution explicite de la fixture")
            Verifier(modes.Lister(user, role).Contains("FACTURATION_ET_CAISSE") AndAlso Not modes.Lister(user, roleCaisse).Contains("FACTURATION_ET_CAISSE"), "Attribution limitee a l'association, jamais a l'autre role du compte")
            ' Une nouvelle session choisit le mode : jamais de cumul dans l'ancienne.
            Exec("UPDATE dbo.UtilisateurSessions SET Fin=SYSUTCDATETIME() WHERE SessionId=" & sessionInitiale.ToString())
            SessionUtilisateur.SessionId = Scalar("INSERT dbo.UtilisateurSessions(UtilisateurId,RoleIdActif,RoleSession,Poste) VALUES(" & user.ToString() & "," & role.ToString() & ",'TEST','TEST'); SELECT CAST(SCOPE_IDENTITY() AS int)")
            SessionUtilisateur.ModeActif = ""
            modes.Activer("FACTURATION_ET_CAISSE")
            Dim accesComptoir As New VenteEncaissementService(dal)
            accesComptoir.VerifierAcces()
            attribution.Attribuer(user, role, False, True, "Retrait pendant session combinee")
            Refus(Of UnauthorizedAccessException)(Sub() accesComptoir.VerifierAcces())
            Verifier(Not modes.Lister(user, role).Contains("FACTURATION_ET_CAISSE"), "Retrait : plus de case proposee a la prochaine connexion")
            attribution.Attribuer(user, role, True, False, "Restaurer l'attribution de test")
            Dim modesAvantRetrait As ConfigurationModes = configModes.Charger()
            configModes.Enregistrer(New ConfigurationModes With {.Active = True, .Facturation = True, .Caisse = True, .Combine = False}, modesAvantRetrait, "Desactivation globale combinee")
            Refus(Of UnauthorizedAccessException)(Sub() accesComptoir.VerifierAcces())
            Verifier(Not modes.Lister(user, role).Contains("FACTURATION_ET_CAISSE"), "Global desactive : aucun acces combine malgre attribution individuelle")
            configModes.Enregistrer(modesAvantRetrait, configModes.Charger(), "Restaurer le combine de test")
            Exec("INSERT dbo.StockEntree(IdStock,ProduitId,QuantiteSaisie,QuantiteBase,Unite,PrixAchat,CreePar) VALUES('PRIX_" & suffix & "'," & produitPrix.ToString() & ",10,10,'base',100," & user.ToString() & ")")
            Dim ligne As New LigneFactureVente With {.ProduitId = produitPrix, .TypeVente = "piece", .Quantite = 1D, .QteSaisie = 1D, .QuantiteBase = 1D, .PrixUnitaire = 21D, .MotifPrixException = "Exception sur cette facture"}
            Dim incoherente As FactureVente = NouvelleFacture("TOTAL_INCOHERENT_" & suffix)
            Dim operationsException As New FactureOperationService(dal)
            Refus(Of ArgumentException)(Sub() operationsException.Enregistrer(incoherente, New List(Of LigneFactureVente) From {ligne}, String.Empty, Nothing))
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.FacturesVente WHERE NumeroFacture='TEST-TOTAL_INCOHERENT_" & suffix & "'") = 0, "Exception de prix : en-tete incoherent annule integralement")
            Dim facture As FactureVente = NouvelleFacture("COMBINE_" & suffix)
            facture.SousTotal = 21D : facture.MontantTotal = 21D
            Dim factureId As Integer = New FactureOperationService(dal).Enregistrer(facture, New List(Of LigneFactureVente) From {ligne}, "", Nothing)
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.Paiements WHERE FactureVenteId=" & factureId.ToString()) = 0, "Mode combiné : création distincte de l'encaissement")
            Dim caisseCombinee As New FacturationService(dal)
            caisseCombinee.EncaisserFacture(factureId, "ESPECES", "REF_" & suffix, 25D, 4D, "FC", user, 25D, 1D)
            TesterComptoir(role, user, reviewer, reviewerSession, produitPrix, suffix)
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.Paiements WHERE FactureVenteId=" & factureId.ToString() & " AND Montant=21 AND MontantRecu=25 AND MonnaieRendue=4 AND MontantRecuOrigine=25 AND TauxConversionApplique=1 AND DeviseMontants='FC'") = 1, "Paiement : reçu, affecté et rendu persistés séparément")
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.JournalAudit WHERE EntiteId='" & factureId.ToString() & "' AND [Action]='ENCAISSEMENT_VALIDE' AND ModeActif='FACTURATION_ET_CAISSE' AND NouvellesValeurs LIKE '%EncaisseParNom%'") = 1, "Audit du mode combiné et des identités instantanées")
            Verifier(prix.Consulter(produitPrix, "piece").Prix = 20D, "L'exception facture ne modifie pas le tarif officiel")
            Dim contexte As Guid = Guid.NewGuid()
            Refus(Of UnauthorizedAccessException)(Sub() prix.DemanderException(prix.Consulter(produitPrix, "piece"), 27D, "Exception soumise", contexte, 1D))
            Exec("INSERT dbo.RoleInterfaces(RoleId,InterfaceId) SELECT " & role.ToString() & ",InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface='PRIX_DEMANDER_FACTURE'")
            Dim demandeArticle As Integer = prix.DemanderException(prix.Consulter(produitPrix, "piece"), 27D, "Exception pour cet article uniquement", contexte, 1D)
            Dim pageDemandes As PageDemandesPrix = prix.ListerPage(0, 1, "PRIX TEST", "EN_ATTENTE", "FACTURIER")
            Verifier(pageDemandes.Lignes.Rows.Count <= 1 AndAlso pageDemandes.Total >= 1, "Pagination et filtrage effectifs côté SQL")
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.DemandesModificationPrix WHERE DemandeId=" & demandeArticle.ToString() & " AND DATEDIFF(HOUR,DemandeLe,ExpireLe)=24 AND CoutReferenceDemande=10 AND Perimetre='FACTURE'") = 1, "Contexte, durée et coût de la demande persistés")
            Dim sessionCombinee As Integer = SessionUtilisateur.SessionId
            SessionUtilisateur.UtilisateurId = reviewer : SessionUtilisateur.NomUtilisateur = "REV_" & suffix : SessionUtilisateur.SessionId = reviewerSession
            prix.Decider(demandeArticle, True, "Prix exceptionnel approuvé, pas de nouveau tarif")
            SessionUtilisateur.UtilisateurId = user : SessionUtilisateur.NomUtilisateur = "TEST_" & suffix : SessionUtilisateur.SessionId = sessionCombinee
            Dim decisionAffichee As DataRow = prix.ListerPage(0, 100, "PRIX TEST", "APPROUVEE", "FACTURIER").Lignes.Rows.Cast(Of DataRow)().Single(Function(r) CInt(r("DemandeId")) = demandeArticle)
            Verifier(CInt(decisionAffichee("DecisionPar")) = reviewer AndAlso CStr(decisionAffichee("Decideur")) = "REV_" & suffix AndAlso Not decisionAffichee.IsNull("DecisionLe"), "Detail : decideur persiste distinct de l'utilisateur qui consulte")
            Verifier(prix.Consulter(produitPrix, "piece").Prix = 20D, "Approbation facture sans changement du tarif officiel")
            Refus(Of InvalidOperationException)(Sub() prix.LireExceptionApprouvee(produitPrix, "piece", Guid.NewGuid(), 1D))
            Refus(Of InvalidOperationException)(Sub() prix.LireExceptionApprouvee(produitPrix, "piece", contexte, 2D))
            Dim approuvee As LigneFactureVente = prix.LireExceptionApprouvee(produitPrix, "piece", contexte, 1D).Ligne
            approuvee.QuantiteBase = 1D
            Dim venteApprouvee As FactureVente = NouvelleFacture("EXCEPTION_" & suffix)
            venteApprouvee.SousTotal = 27D : venteApprouvee.MontantTotal = 27D
            Dim enregistrerException As New FactureOperationService(dal)
            Exec("CREATE TRIGGER dbo.TR_TEST_AuditFailure ON dbo.JournalAudit AFTER INSERT AS BEGIN THROW 51001, 'Panne audit simulée', 1; END")
            Try
                Refus(Of SqlException)(Sub() enregistrerException.Enregistrer(venteApprouvee, New List(Of LigneFactureVente) From {approuvee}, "", Nothing))
            Finally
                Exec("DROP TRIGGER dbo.TR_TEST_AuditFailure")
            End Try
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.DemandesModificationPrix WHERE DemandeId=" & demandeArticle.ToString() & " AND UtiliseeLe IS NULL") = 1, "Échec d'audit : consommation annulée")
            Dim factureExceptionId As Integer = enregistrerException.Enregistrer(venteApprouvee, New List(Of LigneFactureVente) From {approuvee}, "", Nothing)
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.DemandesModificationPrix WHERE DemandeId=" & demandeArticle.ToString() & " AND UtiliseeLe IS NOT NULL AND FactureUtilisationId=" & factureExceptionId.ToString()) = 1, "Approbation consommée dans la transaction facture")
            Refus(Of InvalidOperationException)(Sub() prix.LireExceptionApprouvee(produitPrix, "piece", contexte, 1D))
            Dim editee As DataTable = New LigneFactureVenteRepository(dal).ListerDetailsPourEdition(factureExceptionId)
            Verifier(DirectCast(editee.Rows(0)("ContextePrix"), Guid) = contexte, "Brouillon rechargé : contexte exact conservé")
            Dim remiseModifiee As FactureVente = NouvelleFacture("EXCEPTION_" & suffix)
            remiseModifiee.FactureVenteId = factureExceptionId : remiseModifiee.SousTotal = 27D
            remiseModifiee.MontantRemise = 1D : remiseModifiee.MontantTotal = 26D
            Refus(Of UnauthorizedAccessException)(Sub() enregistrerException.Enregistrer(remiseModifiee, New List(Of LigneFactureVente) From {approuvee}, "Modification de remise non approuvée", enregistrerException.LireVersion(factureExceptionId)))
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.FacturesVente WHERE FactureVenteId=" & factureExceptionId.ToString() & " AND MontantTotal=27 AND MontantRemise=0") = 1, "Contexte de remise protégé après approbation")
            Exec("INSERT dbo.RoleInterfaces(RoleId,InterfaceId) SELECT " & role.ToString() & ",InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface='SUPERADMIN_ROLES'")
            Dim rolesRepo As New SuperAdminRepository(dal)
            Dim donneesRole As DataRow = rolesRepo.ListerRoles().Rows.Cast(Of DataRow)().Single(Function(r) CInt(r("RoleId")) = role)
            Dim versionRole As Byte() = DirectCast(donneesRole("VersionAutorisations"), Byte())
            Dim droitsRole As New List(Of Integer)()
            Dim dtDroits As DataTable = dal.ExecuterTable("SELECT InterfaceId FROM dbo.RoleInterfaces WHERE RoleId=@r", CommandType.Text, New List(Of SqlParameter) From {New SqlParameter("@r", role)})
            For Each row As DataRow In dtDroits.Rows
                droitsRole.Add(CInt(row("InterfaceId")))
            Next
            Refus(Of ArgumentException)(Sub() rolesRepo.EnregistrerRole(role, "TEST_" & suffix, True, droitsRole, " ", versionRole))
            rolesRepo.EnregistrerRole(role, "TEST_" & suffix, True, droitsRole, "Configuration explicite des privilèges", versionRole)
            Refus(Of DBConcurrencyException)(Sub() rolesRepo.EnregistrerRole(role, "TEST_" & suffix, True, droitsRole, "Ancien éditeur concurrent", versionRole))
            donneesRole = rolesRepo.ListerRoles().Rows.Cast(Of DataRow)().Single(Function(r) CInt(r("RoleId")) = role)
            versionRole = DirectCast(donneesRole("VersionAutorisations"), Byte())
            Dim droitPrix As Integer = Scalar("SELECT InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface='PRIX_DEMANDER_FACTURE'")
            droitsRole.Remove(droitPrix)
            rolesRepo.EnregistrerRole(role, "TEST_" & suffix, True, droitsRole, "Retrait explicite du droit de demande facture", versionRole)
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.RoleInterfaces WHERE RoleId=" & role.ToString() & " AND InterfaceId=" & droitPrix.ToString()) = 0, "Permission retirée conservée comme retirée")
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.JournalAudit WHERE Entite='Role' AND EntiteId='" & role.ToString() & "' AND [Action]='PERMISSION_MODIFIEE' AND AnciennesValeurs LIKE '%PRIX_DEMANDER_FACTURE%' AND NouvellesValeurs NOT LIKE '%PRIX_DEMANDER_FACTURE%'") >= 1, "Permissions : différences exactes auditées dans la transaction")
            Dim typePersonnel As Integer = Scalar("INSERT dbo.TypesVenteProduit(ProduitId,Nom,QuantiteEquivalent,TypeUniteEquivalent,TypeQuantiteEquivalent,ModePrix,Coefficient,PrixVente,Actif) VALUES(" & produitPrix.ToString() & ",'piece',6,'SECONDAIRE','SECONDAIRE','coefficient',2,123,1); SELECT CAST(SCOPE_IDENTITY() AS int)")
            Refus(Of InvalidOperationException)(Sub() prix.Consulter(produitPrix, "piece"))
            Dim personnel As TarifDelegue = prix.Consulter(produitPrix, "piece", typePersonnel)
            Dim sachet As Integer = Scalar("INSERT dbo.TypesVenteProduit(ProduitId,Nom,QuantiteEquivalent,TypeUniteEquivalent,TypeQuantiteEquivalent,ModePrix,Coefficient,PrixVente,Actif) VALUES(" & produitPrix.ToString() & ",'Sachet Test',6,'SECONDAIRE','SECONDAIRE','coefficient',2,123,1); SELECT CAST(SCOPE_IDENTITY() AS int)")
            Verifier(prix.ListerTarifsActifs(produitPrix).Exists(Function(x) x.TypeId = sachet AndAlso x.Prix = 120D AndAlso x.CoutCoefficient = 60D), "Type personnalise coefficient charge sans cout unitaire incompatible")
            Dim jsonTarif As String = New System.Web.Script.Serialization.JavaScriptSerializer().Serialize(personnel)
            Verifier(Not jsonTarif.Contains("PrixAchatReference") AndAlso Not jsonTarif.Contains("TypeActif") AndAlso Not jsonTarif.Contains("UnitePrincipale") AndAlso Not jsonTarif.Contains("ReferenceQuantite"), "Metadonnees UI exclues de l'empreinte des anciennes approbations")
            Verifier(personnel.Prix = 120D AndAlso personnel.TypeId = typePersonnel, "Type personnalisé homonyme et coefficient insensible à la casse")
            prix.Modifier(personnel, 125D, "Tarif personnalisé uniquement", False)
            Verifier(prix.Consulter(produitPrix, "piece", 0).Prix = 20D AndAlso prix.Consulter(produitPrix, "piece", typePersonnel).Prix = 125D, "Tarif officiel standard intact, prix personnalisé appliqué effectivement")
            TesterCoutsPersonnalises(prix, suffix)
        Finally
            SessionUtilisateur.UtilisateurId = user
            SessionUtilisateur.NomUtilisateur = "TEST_" & suffix
            Dim actuel As ConfigurationPrix = prix.Configuration()
            prix.Configurer(initialPrix, actuel, "Restaurer les paramètres de la copie")
            Dim actuels As ConfigurationModes = configModes.Charger()
            configModes.Enregistrer(initialModes, actuels, "Restaurer les modes de la copie")
            SessionUtilisateur.ModeActif = ""
        End Try
    End Sub

    Private Sub TesterCoutsPersonnalises(prix As DelegationPrixService, suffix As String)
        Dim id As Integer = Scalar("INSERT dbo.Produits(CodeBarres,Libelle,PrixDetail,PrixGros,PrixDemi,PrixAchat,ConversionUnite,TypeGestionStock,ContenuUnitePrincipale,ContenuUniteSecondaire,VenteGros,VenteDemi,VenteDetail) VALUES('STYLO_'+@s,'Paquet stylo test',814,24200,12100,23000,30,'UNITE',30,1,1,1,1); SELECT CAST(SCOPE_IDENTITY() AS int)", suffix)
        Dim tarifId As Integer = Scalar("INSERT dbo.TypesVenteProduit(ProduitId,Nom,QuantiteEquivalent,TypeUniteEquivalent,TypeQuantiteEquivalent,ModePrix,Coefficient,PrixVente,Actif) VALUES(" & id.ToString() & ",'Paquets 10+1',11,'SECONDAIRE','SECONDAIRE','FIXE',0,14000,1); SELECT CAST(SCOPE_IDENTITY() AS int)")
        Dim t As TarifDelegue = prix.Consulter(id, "Paquets 10+1", tarifId)
        Verifier(t.QuantiteBase = 11D AndAlso Math.Round(t.Cout, 2) = 8433.33D, "SQL : cout du paquet secondaire, pas onze cartons")
        Verifier(prix.Consulter(id, "gros", 0).Cout = 23000D AndAlso prix.Consulter(id, "demi", 0).Cout = 11500D, "SQL : references gros et demi sans centime fictif")
        prix.Modifier(t, 14500D, "Correction de tarif sur onze pieces reelles", False)
        Verifier(prix.Consulter(id, "Paquets 10+1", tarifId).Prix = 14500D, "SQL : nouveau tarif reellement persiste")
        Refus(Of UnauthorizedAccessException)(Sub() prix.Modifier(prix.Consulter(id, "Paquets 10+1", tarifId), 8000D, "Vente reellement sous cout refusee", False))
        Exec("UPDATE dbo.TypesVenteProduit SET TypeUniteEquivalent='PRINCIPALE',TypeQuantiteEquivalent='PRINCIPALE' WHERE TypeVenteProduitId=" & tarifId.ToString())
        Dim principal As TarifDelegue = prix.Consulter(id, "Paquets 10+1", tarifId)
        Verifier(principal.QuantiteBase = 330D AndAlso principal.Cout = 253000D, "SQL : nom commercial non utilise pour reinterpretation du stock")
        Refus(Of UnauthorizedAccessException)(Sub() prix.Modifier(principal, 15000D, "Reference principale conservee, perte reelle", False))
    End Sub

    Private Sub TesterDisponibiliteEtAbsenceRejeu()
        Verifier(SqlDisponibiliteService.TesterUneFois(cs).Disponible, "Sonde SQL SELECT 1 sur copie autorisee")
        Verifier(SqlDisponibiliteService.TesterUneFois("").Categorie = "CONFIGURATION", "Sonde configuration absente sans attente reseau")
        ' Table temporaire globale, unique et gardee par cette connexion de test.
        Dim nom As String = "##ERP_Reseau_" & Guid.NewGuid().ToString("N")
        Using cn As New SqlConnection(cs)
            cn.Open()
            Using cmd As New SqlCommand("CREATE TABLE " & nom & "(Id int NOT NULL)", cn)
                cmd.ExecuteNonQuery()
            End Using
            Try
                Dim appels As Integer = 0
                Refus(Of SqlException)(Sub() dal.ExecuterUneFois(Of Integer)("INSERT INTO " & nom & " VALUES(1); WAITFOR DELAY '00:00:03'", CommandType.Text, Nothing, Function(cmd)
                                                                                                                                                                           appels += 1
                                                                                                                                                                           cmd.CommandTimeout = 1
                                                                                                                                                                           Return cmd.ExecuteNonQuery()
                                                                                                                                                                       End Function, "TestTimeoutSansRejeu"))
                Using cmd As New SqlCommand("SELECT COUNT(*) FROM " & nom, cn)
                    Verifier(appels = 1 AndAlso CInt(cmd.ExecuteScalar()) = 1, "Timeout apres ecriture autocommit : pas de double execution DAL")
                End Using
                Refus(Of SqlException)(Sub() dal.ExecuterUneFois(Of Integer)("INSERT INTO " & nom & " VALUES(2); WAITFOR DELAY '00:00:03'", CommandType.Text, Nothing, Function(cmd)
                                                                                                                                                                           appels += 1
                                                                                                                                                                           Using tx As SqlTransaction = cmd.Connection.BeginTransaction()
                                                                                                                                                                               cmd.Transaction = tx : cmd.CommandTimeout = 1
                                                                                                                                                                               Dim resultat As Integer = cmd.ExecuteNonQuery()
                                                                                                                                                                               tx.Commit()
                                                                                                                                                                               Return resultat
                                                                                                                                                                           End Using
                                                                                                                                                                       End Function, "TestRollbackSansRejeu"))
                Using cmd As New SqlCommand("SELECT COUNT(*) FROM " & nom, cn)
                    Verifier(appels = 2 AndAlso CInt(cmd.ExecuteScalar()) = 1, "Timeout transactionnel : aucune seconde ecriture conservee ni rejeu")
                End Using
                Verifier(SqlDisponibiliteService.TesterUneFois(cs).Disponible, "Sonde reussie apres erreur SQL sans reprise des ecritures")
            Finally
                Using cmd As New SqlCommand("DROP TABLE " & nom, cn)
                    cmd.ExecuteNonQuery()
                End Using
            End Try
        End Using
    End Sub

    Private Function NouvelleFacture(suffix As String) As FactureVente
        Return New FactureVente With {.NumeroFacture = "TEST-" & suffix, .Statut = "EN_ATTENTE", .SousTotal = 20D, .MontantTotal = 20D}
    End Function
    Private Function Lignes(qte As Decimal) As IList(Of LigneFactureVente)
        Return New List(Of LigneFactureVente) From {New LigneFactureVente With {.ProduitId = produit, .Quantite = qte, .QuantiteBase = qte, .QteSaisie = qte, .TypeVente = "TEST", .PrixUnitaire = 10D}}
    End Function
    Private Function Scalar(sql As String, Optional suffix As String = "") As Integer
        Using cn As New SqlConnection(cs)
            cn.Open()
            Using cmd As New SqlCommand(sql, cn)
                cmd.Parameters.AddWithValue("@s", suffix)
                Return Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        End Using
    End Function
    Private Sub Exec(sql As String)
        Using cn As New SqlConnection(cs)
            cn.Open()
            Using cmd As New SqlCommand(sql, cn)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub
    Private Sub Refus(Of T As Exception)(action As Action)
        Try
            action()
        Catch ex As Exception
            If Not TypeOf ex Is T Then Throw
            compteur += 1
            Return
        End Try
        Throw New Exception("Refus attendu : " & GetType(T).Name)
    End Sub
    Private Sub TesterDroitAdministrationModes(service As AttributionModeService, role As Integer, autreRole As Integer, user As Integer)
        Dim supprimerDroit As String = "DELETE ri FROM dbo.RoleInterfaces ri JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=" & role.ToString() & " AND i.CodeInterface='UTILISATEUR_MODES'"
        Exec(supprimerDroit)
        Dim lecture As DataTable = service.Lister(user)
        Verifier(lecture.Rows.Count > 0 AndAlso Not CBool(lecture.ExtendedProperties("PeutModifier")), "Roles charges en lecture seule sans UTILISATEUR_MODES, ComboBox non vide")
        Refus(Of UnauthorizedAccessException)(Sub() service.Attribuer(user, role, True, False, "Attribution interdite sans privilege"))
        Exec("INSERT dbo.RoleInterfaces(RoleId,InterfaceId) SELECT " & autreRole.ToString() & ",InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface='SUPERADMIN_ROLES'")
        Try
            Refus(Of UnauthorizedAccessException)(Sub() service.AccorderAdministrationAuRoleActif("Un autre role du compte ne suffit pas"))
            Verifier(Not CBool(service.Lister(user).ExtendedProperties("PeutAccorder")), "Droit d'un autre role non cumule dans le role actif")
        Finally
            Exec("DELETE ri FROM dbo.RoleInterfaces ri JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=" & autreRole.ToString() & " AND i.CodeInterface='SUPERADMIN_ROLES'")
        End Try
        Dim nomRole As String = SessionUtilisateur.Role
        Try
            SessionUtilisateur.Role = "SUPERADMIN"
            Refus(Of UnauthorizedAccessException)(Sub() service.AccorderAdministrationAuRoleActif("Nom de role local ne donne aucun pouvoir"))
        Finally
            SessionUtilisateur.Role = nomRole
        End Try
        Exec("INSERT dbo.RoleInterfaces(RoleId,InterfaceId) SELECT " & role.ToString() & ",InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface='SUPERADMIN_ROLES'")
        Try
            Verifier(CBool(service.Lister(user).ExtendedProperties("PeutAccorder")), "Gestionnaire reel de privileges peut proposer un grant explicite")
            Refus(Of ArgumentException)(Sub() service.AccorderAdministrationAuRoleActif(" "))
            Exec("CREATE TRIGGER dbo.TR_TEST_AuditFailure ON dbo.JournalAudit AFTER INSERT AS BEGIN THROW 51001, 'Panne audit grant simulee', 1; END")
            Try
                Refus(Of SqlException)(Sub() service.AccorderAdministrationAuRoleActif("Panne audit : aucun grant partiel"))
            Finally
                Exec("DROP TRIGGER dbo.TR_TEST_AuditFailure")
            End Try
            Verifier(Not CBool(service.Lister(user).ExtendedProperties("PeutModifier")), "Audit echoue : privilege non accorde")
            Dim editeur As New SuperAdminRepository(dal)
            Dim roleAvant As DataRow = editeur.ListerRoles().Rows.Cast(Of DataRow)().Single(Function(r) CInt(r("RoleId")) = role)
            Dim versionAvant As Byte() = DirectCast(roleAvant("VersionAutorisations"), Byte())
            Dim droitsAvant As List(Of Integer) = editeur.ListerInterfacesParRole(role).Rows.Cast(Of DataRow)().Where(Function(r) CBool(r("Autorise"))).Select(Function(r) CInt(r("InterfaceId"))).ToList()
            Dim avant As Integer = Scalar("SELECT COUNT(*) FROM dbo.RoleInterfaces WHERE RoleId=" & role.ToString())
            Dim audits As Integer = Scalar("SELECT COUNT(*) FROM dbo.JournalAudit WHERE Entite='Role' AND EntiteId='" & role.ToString() & "' AND [Action]='PERMISSION_MODIFIEE'")
            Verifier(service.AccorderAdministrationAuRoleActif("Autorisation explicite du gestionnaire"), "Grant explicite reussi")
            Refus(Of DBConcurrencyException)(Sub() editeur.EnregistrerRole(role, CStr(roleAvant("NomRole")), True, droitsAvant, "Editeur ouvert avant le grant", versionAvant))
            Verifier(Not service.AccorderAdministrationAuRoleActif("Repetition sans doublon"), "Grant idempotent sans seconde modification")
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.RoleInterfaces WHERE RoleId=" & role.ToString()) = avant + 1, "Aucun autre privilege change")
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.JournalAudit WHERE Entite='Role' AND EntiteId='" & role.ToString() & "' AND [Action]='PERMISSION_MODIFIEE'") = audits + 1, "Un grant et un audit transactionnel")
            Verifier(CBool(New AttributionModeService(dal).Lister(user).ExtendedProperties("PeutModifier")), "Grant relu depuis SQL, pas un cache de droits")
            Exec(supprimerDroit)
            Dim repo As New SuperAdminRepository(dal)
            repo.AssurerInfrastructure()
            Verifier(Not CBool(service.Lister(user).ExtendedProperties("PeutModifier")), "Retrait non restaure par l'amorcage")
            service.AccorderAdministrationAuRoleActif("Restaurer explicitement la fixture")
        Finally
            Exec("DELETE ri FROM dbo.RoleInterfaces ri JOIN dbo.InterfacesApplication i ON i.InterfaceId=ri.InterfaceId WHERE ri.RoleId=" & role.ToString() & " AND i.CodeInterface='SUPERADMIN_ROLES'")
        End Try
    End Sub

    Private Sub TesterComptoir(role As Integer, user As Integer, reviewer As Integer, reviewerSession As Integer, produitPrix As Integer, suffix As String)
        TesterClassementClients()
        Dim service As New VenteEncaissementService(dal)
        TesterAttenteComptoir(produitPrix, suffix)
        Dim operation As Guid = Guid.NewGuid()
        Dim facture As FactureVente = NouvelleFacture("COMPTOIR_" & suffix)
        facture.SousTotal = 20D : facture.MontantTotal = 20D
        Dim articles As New List(Of LigneFactureVente) From {New LigneFactureVente With {.ProduitId = produitPrix, .Quantite = 1D, .QteSaisie = 1D, .QuantiteBase = 1D, .TypeVente = "piece", .TypeTarifId = 0, .PrixUnitaire = 20D}}
        Dim paiement As New ReglementComptoir With {.MontantOrigine = 25D}
        Dim id As Integer = service.Finaliser(operation, facture, articles, "", Nothing, Nothing, paiement)
        Dim nombrePaiements As Integer = Scalar("SELECT COUNT(*) FROM dbo.Paiements")
        Dim nombreSorties As Integer = Scalar("SELECT COUNT(*) FROM dbo.StockSortie")
        Verifier(CInt(service.LireDocument(id).Tables("Entete").Rows(0)("FactureVenteId")) = id, "Document immediat relu apres paiement")
        Verifier(CInt(service.LireDocument().Tables("Entete").Rows(0)("FactureVenteId")) = id, "Derniere vente retrouvee sans etat du panier")
        service.LireDocument(id) : service.LireDocument(id)
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.Paiements") = nombrePaiements AndAlso Scalar("SELECT COUNT(*) FROM dbo.StockSortie") = nombreSorties, "Reimpressions sans nouvel effet financier ni stock")
        Verifier(service.Finaliser(operation, facture, articles, "", Nothing, Nothing, paiement) = id, "Repetition idempotente de la meme vente")
        Dim panneNotification As EventHandler = Sub(sender, e) Throw New InvalidOperationException("Panne notification comptoir")
        AddHandler AppEvents.VenteValidee, panneNotification
        Try
            Refus(Of FactureEnregistreeAffichageException)(Sub() service.Finaliser(operation, facture, articles, "", Nothing, Nothing, paiement))
        Finally
            RemoveHandler AppEvents.VenteValidee, panneNotification
        End Try
        Verifier(service.Finaliser(operation, facture, articles, "", Nothing, Nothing, paiement) = id, "Reprise apres notification UI echouee sans second encaissement")
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.Paiements WHERE FactureVenteId=" & id.ToString() & " AND Montant=20 AND MontantRecu=25 AND MonnaieRendue=5 AND PayePar=" & user.ToString()) = 1, "Un paiement, monnaie non comptabilisee comme recette")
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.FacturesVente WHERE FactureVenteId=" & id.ToString() & " AND CreePar=" & user.ToString() & " AND ValideeParUtilisateurId=" & user.ToString() & " AND Statut='PAYEE'") = 1, "Createur et validateur reels, distincts du champ encaisseur")
        paiement.MontantOrigine = 30D
        Refus(Of DBConcurrencyException)(Sub() service.Finaliser(operation, facture, articles, "", Nothing, Nothing, paiement))
        paiement.MontantOrigine = 25D
        Dim autre As FactureVente = NouvelleFacture("COMPTOIR_CONCURRENT_" & suffix)
        autre.SousTotal = 20D : autre.MontantTotal = 20D
        Dim cle As Guid = Guid.NewGuid(), premier As Integer = 0, second As Integer = 0
        Parallel.Invoke(Sub() premier = service.Finaliser(cle, autre, articles, "", Nothing, Nothing, paiement), Sub() second = service.Finaliser(cle, autre, articles, "", Nothing, Nothing, paiement))
        Verifier(premier = second AndAlso Scalar("SELECT COUNT(*) FROM dbo.Paiements WHERE FactureVenteId=" & premier.ToString()) = 1, "Double confirmation concurrente : une vente et un paiement")
        Dim impossible As FactureVente = NouvelleFacture("COMPTOIR_ROLLBACK_" & suffix)
        impossible.SousTotal = 19980D : impossible.MontantTotal = 19980D
        Dim trop As New List(Of LigneFactureVente) From {New LigneFactureVente With {.ProduitId = produitPrix, .Quantite = 999D, .QteSaisie = 999D, .QuantiteBase = 999D, .TypeVente = "piece", .TypeTarifId = 0, .PrixUnitaire = 20D}}
        Refus(Of Exception)(Sub() service.Finaliser(Guid.NewGuid(), impossible, trop, "", Nothing, Nothing, New ReglementComptoir With {.MontantOrigine = 20000D}))
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.FacturesVente WHERE NumeroFacture=@s", impossible.NumeroFacture) = 0, "Stock insuffisant : facture et paiement annules ensemble")
        Refus(Of ArgumentException)(Sub() service.Finaliser(Guid.NewGuid(), autre, articles, "", Nothing, Nothing, New ReglementComptoir With {.Moyen = "CARTE", .MontantOrigine = 20D}))
        Dim sid As Integer = SessionUtilisateur.SessionId
        Try
            SessionUtilisateur.UtilisateurId = reviewer
            SessionUtilisateur.NomUtilisateur = "REV_" & suffix
            SessionUtilisateur.SessionId = reviewerSession
            SessionUtilisateur.ModeActif = ""
            Dim modes As New ModeTravailService(dal)
            modes.Activer("FACTURATION")
            Dim preparee As FactureVente = NouvelleFacture("AUTEURS_DISTINCTS_" & suffix)
            preparee.SousTotal = 20D : preparee.MontantTotal = 20D
            Dim factureSeparee As Integer = New FactureOperationService(dal).Enregistrer(preparee, articles, "", Nothing)
            SessionUtilisateur.UtilisateurId = user
            SessionUtilisateur.NomUtilisateur = "TEST_" & suffix
            SessionUtilisateur.SessionId = sid
            SessionUtilisateur.ModeActif = "FACTURATION_ET_CAISSE"
            Dim caisse As New FacturationService(dal)
            caisse.EncaisserFacture(factureSeparee, "CASH", "TEST", 20D, 0D, "FC", user, 20D, 1D)
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.FacturesVente f JOIN dbo.Paiements p ON p.FactureVenteId=f.FactureVenteId WHERE f.FactureVenteId=" & factureSeparee.ToString() & " AND f.CreePar=" & reviewer.ToString() & " AND f.ValideeParUtilisateurId=" & reviewer.ToString() & " AND p.PayePar=" & user.ToString()) = 1, "Preparation par un utilisateur et encaissement par un autre preserves")
            Verifier(CInt(service.LireDocument(factureSeparee).Tables("Entete").Rows(0)("FactureVenteId")) = factureSeparee, "Reimpression autorisee a l'encaisseur distinct du validateur, origine auditee combinee")
            Refus(Of UnauthorizedAccessException)(Sub() service.LireDocument(Integer.MaxValue))
        Finally
            SessionUtilisateur.UtilisateurId = user
            SessionUtilisateur.NomUtilisateur = "TEST_" & suffix
            SessionUtilisateur.SessionId = sid
            SessionUtilisateur.ModeActif = "FACTURATION_ET_CAISSE"
        End Try
    End Sub

    Private Sub TesterClassementClients()
        ' Meme requete que le repository, mais seulement des tables temporaires.
        Using cn As SqlConnection = dal.CreerConnexion()
            cn.Open()
            Using cmd As New SqlCommand("CREATE TABLE #Clients(ClientId int,NomClient nvarchar(100),Telephone nvarchar(30)); CREATE TABLE #FacturesVente(FactureVenteId int,ClientId int NULL,CreeLe datetime2,Statut nvarchar(30),MontantTotal decimal(18,2)); INSERT #Clients VALUES(1,N'Même nom','111'),(2,N'Même nom','222'); INSERT #FacturesVente VALUES(1,1,'20260201','PAYEE',10.75),(2,1,'20260228 23:59:59','PAYEE',20.50),(3,2,'20260210','PAYEE',7.75),(4,1,'20260131','PAYEE',3.50),(5,1,'20260211','ANNULEE',999),(6,NULL,'20260212','PAYEE',500),(7,1,'20260301','PAYEE',800),(8,1,'20260220','EN_ATTENTE',300);", cn)
                cmd.ExecuteNonQuery()
            End Using
            Dim requete As String = ClientRepository.RequeteClassementAchats.Replace("dbo.Clients", "#Clients").Replace("dbo.FacturesVente", "#FacturesVente")
            Using cmd As New SqlCommand("CREATE TABLE #LignesFactureVente(LigneFactureVenteId int,FactureVenteId int); INSERT #LignesFactureVente VALUES(1,1),(2,1),(3,2),(4,3);", cn)
                cmd.ExecuteNonQuery()
            End Using
            requete = requete.Replace("dbo.LignesFactureVente", "#LignesFactureVente")
            Using cmd As New SqlCommand(requete, cn)
                cmd.Parameters.AddWithValue("@du", New DateTime(2026, 2, 1))
                cmd.Parameters.AddWithValue("@au", New DateTime(2026, 3, 1))
                Dim table As New DataTable
                Using lecteur As SqlDataReader = cmd.ExecuteReader()
                    table.Load(lecteur)
                End Using
                Verifier(table.Rows.Count = 2 AndAlso CInt(table.Rows(0)("ClientId")) = 1 AndAlso CInt(table.Rows(1)("ClientId")) = 2, "Deux identites de meme nom non fusionnees")
                Verifier(CLng(table.Rows(0)("NbAchats")) = 2 AndAlso CDec(table.Rows(0)("TotalAchats")) = 31.25D AndAlso CDec(table.Rows(0)("MoyenneAchat")) = 15.625D, "Agregation facture, decimals et periode sans annulation ni dette")
                cmd.Parameters("@du").Value = New DateTime(2026, 1, 1)
                cmd.Parameters("@au").Value = New DateTime(2026, 2, 1)
                table.Clear()
                Using lecteur As SqlDataReader = cmd.ExecuteReader()
                    table.Load(lecteur)
                End Using
                Verifier(table.Rows.Count = 1 AndAlso CDec(table.Rows(0)("TotalAchats")) = 3.5D, "Analyse de plusieurs mois sans cumul silencieux")
            End Using
        End Using
    End Sub

    Private Sub TesterAttenteComptoir(produitPrix As Integer, suffix As String)
        Dim operations As New FactureOperationService(dal)
        Dim f As FactureVente = NouvelleFacture("ATTENTE_" & suffix)
        f.SousTotal = 40D : f.MontantRemise = 1D : f.MontantTotal = 39D
        Dim lignes As New List(Of LigneFactureVente) From {New LigneFactureVente With {.ProduitId = produitPrix, .Quantite = 2D, .QteSaisie = 2D, .QuantiteBase = 2D, .TypeVente = "piece", .TypeTarifId = 0, .PrixUnitaire = 20D, .ContextePrix = Guid.NewGuid()}}
        Dim paiements As Integer = Scalar("SELECT COUNT(*) FROM dbo.Paiements"), sorties As Integer = Scalar("SELECT COUNT(*) FROM dbo.StockSortie")
        Dim client As New Client With {.NomClient = "ATTENTE_CLIENT_" & suffix, .Telephone = "", .Adresse = "", .Email = "", .EstActif = True}
        Dim id As Integer = operations.Enregistrer(f, lignes, "Client revient ; remise conservee", Nothing, client, False)
        Refus(Of SqlException)(Sub() operations.Enregistrer(f, lignes, "Repetition identique sans recréer le panier", Nothing, Nothing, False))
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.FacturesVente WHERE NumeroFacture=@s", f.NumeroFacture) = 1, "Contextes de lignes uniques : pas de brouillon duplique")
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.Paiements") = paiements AndAlso Scalar("SELECT COUNT(*) FROM dbo.StockSortie") = sorties, "Attente sans encaissement ni stock deduit")
        Dim entete As DataRow = operations.LireEntetePourEdition(id)
        Verifier(CDec(entete("MontantRemise")) = 1D AndAlso CStr(entete("NomClient")) = client.NomClient, "Client et remise relus pour reprise")
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.LignesFactureVente WHERE FactureVenteId=" & id.ToString() & " AND QuantiteSaisie=2 AND QuantiteBase=2 AND TypeVente='piece' AND PrixUnitaire=20") = 1, "Quantite, unite et prix conserves")
        f.FactureVenteId = id
        lignes(0).Quantite = 3D : lignes(0).QteSaisie = 3D : lignes(0).QuantiteBase = 3D
        f.SousTotal = 60D : f.MontantTotal = 59D
        operations.Enregistrer(f, lignes, "Reprise et correction quantite", operations.LireVersion(id), Nothing, False)
        Dim comptoir As New VenteEncaissementService(dal)
        Refus(Of UnauthorizedAccessException)(Sub() comptoir.LireDocument(id))
        comptoir.Finaliser(Guid.NewGuid(), f, lignes, "Validation apres reprise", operations.LireVersion(id), Nothing, New ReglementComptoir With {.MontantOrigine = 60D})
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.Paiements WHERE FactureVenteId=" & id.ToString() & " AND Montant=59 AND MonnaieRendue=1") = 1, "Reprise finalisee une seule fois avec paiement reel")
    End Sub

    Private Sub TesterOuvertureSessions(role As Integer, suffix As String, sessions As SessionRepository)
        Dim compte As Integer = Scalar("INSERT dbo.Utilisateurs(NomUtilisateur,MotDePasseHash,MotDePasseSel,EstActif) SELECT TOP(1) 'SES_' + @s,MotDePasseHash,MotDePasseSel,1 FROM dbo.Utilisateurs; SELECT CAST(SCOPE_IDENTITY() AS int)", suffix)
        Exec("INSERT dbo.UtilisateurRoles(UtilisateurId,RoleId,EstActif,EstRolePrincipal) VALUES(" & compte.ToString() & "," & role.ToString() & ",1,1)")
        Exec("UPDATE dbo.UtilisateurRoles SET EstActif=0 WHERE UtilisateurId=" & compte.ToString())
        Refus(Of UnauthorizedAccessException)(Sub() sessions.DemarrerSession(compte, role, "NOM INVENTE"))
        Exec("UPDATE dbo.UtilisateurRoles SET EstActif=1 WHERE UtilisateurId=" & compte.ToString())
        Exec("CREATE TRIGGER dbo.TR_TEST_SessionAuditFailure ON dbo.JournalAudit AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted WHERE [Action]='SESSION_ROLE_ACTIVE' AND EffectuePar=" & compte.ToString() & ") THROW 51000,'TEST SESSION AUDIT FAILURE',1; END")
        Try
            Refus(Of SqlException)(Sub() sessions.DemarrerSession(compte, role, "NOM INVENTE"))
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.UtilisateurSessions WHERE UtilisateurId=" & compte.ToString()) = 0, "Echec audit : aucune session partiellement ouverte")
        Finally
            Exec("DROP TRIGGER dbo.TR_TEST_SessionAuditFailure")
        End Try
        Dim gagnants As Integer = 0, refuses As Integer = 0, sessionCreee As Integer = 0
        Parallel.For(0, 2, Sub(i)
                              Try
                                  Dim id As Integer = sessions.DemarrerSession(compte, role, "NOM INVENTE")
                                  Interlocked.Exchange(sessionCreee, id)
                                  Interlocked.Increment(gagnants)
                              Catch ex As InvalidOperationException
                                  Interlocked.Increment(refuses)
                              End Try
                          End Sub)
        Verifier(gagnants = 1 AndAlso refuses = 1, "Deux ouvertures concurrentes : une seule session du compte")
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.JournalAudit a JOIN dbo.Roles r ON r.RoleId=" & role.ToString() & " WHERE a.SessionId=" & sessionCreee.ToString() & " AND a.[Action]='SESSION_ROLE_ACTIVE' AND a.EffectuePar=" & compte.ToString() & " AND a.RoleActif=r.NomRole AND a.Resultat='SUCCES'") = 1, "Role reel et auteur audites une seule fois dans la transaction")
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.UtilisateurSessions s JOIN dbo.Roles r ON r.RoleId=s.RoleIdActif WHERE s.SessionId=" & sessionCreee.ToString() & " AND s.RoleSession=r.NomRole") = 1, "Nom de role fourni par le client ignore au profit de SQL")
        sessions.FermerSession(sessionCreee)
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.UtilisateurSessions WHERE SessionId=" & sessionCreee.ToString() & " AND Fin IS NOT NULL") = 1, "Deconnexion ferme uniquement la session choisie")
    End Sub

    Private Sub Verifier(condition As Boolean, message As String)
        If Not condition Then Throw New Exception(message)
        compteur += 1
    End Sub
End Module
