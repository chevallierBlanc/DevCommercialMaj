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
        Dim stockId As Integer = operations.Enregistrer(factureStock, doublons, "", Nothing)
        Try
            caisse.EncaisserFacture(stockId, "ESPECES", "TEST", 20D, 0D, "FC", user)
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
        Dim modes As New ModeTravailService(dal)
        Dim configModes As New ConfigurationModesService(dal)
        Dim produitPrix As Integer = Scalar("INSERT dbo.Produits(CodeBarres,Libelle,PrixDetail,PrixGros,PrixAchat,ConversionUnite,TypeGestionStock,ContenuUnitePrincipale,ContenuUniteSecondaire,VenteGros) VALUES('PRIX_' + @s,'PRIX TEST',15,150,100,10,'UNITE',10,1,1); SELECT CAST(SCOPE_IDENTITY() AS int)", suffix)
        Refus(Of UnauthorizedAccessException)(Sub() prix.Consulter(produitPrix, "piece"))
        Exec("INSERT dbo.RoleInterfaces(RoleId,InterfaceId) SELECT " & role.ToString() & ",InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface IN('PRIX_CONSULTER','PRIX_TARIF_MODIFIER','PRIX_DEMANDER','PRIX_APPROUVER','PRIX_REFUSER','PRIX_FACTURE_EXCEPTION','PARAMETRES','PARAMETRES_PRIX','PARAMETRES_SECURITE','MODE_FACTURATION')")
        Dim initialPrix As ConfigurationPrix = prix.Configuration()
        Dim initialModes As ConfigurationModes = configModes.Charger()
        Dim sessionInitiale As Integer = SessionUtilisateur.SessionId
        Try
            prix.Configurer(New ConfigurationPrix With {.Active = True, .Demandes = True, .Immediate = True, .Variation = 10D}, initialPrix, "Configuration de fixture isolée")
            Dim t As TarifDelegue = prix.Consulter(produitPrix, "piece")
            Verifier(t.Prix = 15D AndAlso t.Cout = 10D AndAlso t.QuantiteBase = 1D, "Coût et tarif comparés dans la même unité")
            Refus(Of ArgumentException)(Sub() prix.Modifier(t, 16D, "  ", False))
            Refus(Of UnauthorizedAccessException)(Sub() prix.Modifier(t, 9D, "Sous coût interdit", True))
            prix.Modifier(t, 16.5D, "Variation autorisée", False)
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
            Verifier(prix.Consulter(produitPrix, "piece").Prix = t.Prix, "Une demande ne change pas le tarif actif")
            Refus(Of UnauthorizedAccessException)(Sub() prix.Decider(demande, True, "Auto-approbation interdite"))
            Dim reviewer As Integer = Scalar("INSERT dbo.Utilisateurs(NomUtilisateur,MotDePasseHash,MotDePasseSel,EstActif) SELECT TOP(1) 'REV_' + @s,MotDePasseHash,MotDePasseSel,1 FROM dbo.Utilisateurs; SELECT CAST(SCOPE_IDENTITY() AS int)", suffix)
            Exec("INSERT dbo.UtilisateurRoles(UtilisateurId,RoleId,EstActif,EstRolePrincipal) VALUES(" & reviewer.ToString() & "," & role.ToString() & ",1,1)")
            SessionUtilisateur.UtilisateurId = reviewer
            SessionUtilisateur.NomUtilisateur = "REV_" & suffix
            SessionUtilisateur.SessionId = Scalar("INSERT dbo.UtilisateurSessions(UtilisateurId,RoleIdActif,RoleSession,Poste) VALUES(" & reviewer.ToString() & "," & role.ToString() & ",'TEST','TEST'); SELECT CAST(SCOPE_IDENTITY() AS int)")
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
            Verifier(autorises.Count = 1 AndAlso autorises(0) = "FACTURATION", "Mode combiné absent sans attribution explicite")
            Refus(Of UnauthorizedAccessException)(Sub() modes.Activer("FACTURATION_ET_CAISSE"))
            modes.Activer("FACTURATION")
            Using cn As SqlConnection = dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Refus(Of UnauthorizedAccessException)(Sub() AutorisationActionService.Exiger(cn, tx, "ENCAISSEMENT_CREER", "CAISSE"))
                    tx.Rollback()
                End Using
            End Using
            Exec("INSERT dbo.RoleInterfaces(RoleId,InterfaceId) SELECT " & role.ToString() & ",InterfaceId FROM dbo.InterfacesApplication WHERE CodeInterface='MODE_COMBINE'")
            ' Une nouvelle session choisit le mode : jamais de cumul dans l'ancienne.
            Exec("UPDATE dbo.UtilisateurSessions SET Fin=SYSUTCDATETIME() WHERE SessionId=" & sessionInitiale.ToString())
            SessionUtilisateur.SessionId = Scalar("INSERT dbo.UtilisateurSessions(UtilisateurId,RoleIdActif,RoleSession,Poste) VALUES(" & user.ToString() & "," & role.ToString() & ",'TEST','TEST'); SELECT CAST(SCOPE_IDENTITY() AS int)")
            SessionUtilisateur.ModeActif = ""
            modes.Activer("FACTURATION_ET_CAISSE")
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
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.Paiements WHERE FactureVenteId=" & factureId.ToString() & " AND Montant=21 AND MontantRecu=25 AND MonnaieRendue=4 AND MontantRecuOrigine=25 AND TauxConversionApplique=1 AND DeviseMontants='FC'") = 1, "Paiement : reçu, affecté et rendu persistés séparément")
            Verifier(Scalar("SELECT COUNT(*) FROM dbo.JournalAudit WHERE EntiteId='" & factureId.ToString() & "' AND [Action]='ENCAISSEMENT_VALIDE' AND ModeActif='FACTURATION_ET_CAISSE' AND NouvellesValeurs LIKE '%EncaisseParNom%'") = 1, "Audit du mode combiné et des identités instantanées")
            Verifier(prix.Consulter(produitPrix, "piece").Prix = 20D, "L'exception facture ne modifie pas le tarif officiel")
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
    Private Sub Verifier(condition As Boolean, message As String)
        If Not condition Then Throw New Exception(message)
        compteur += 1
    End Sub
End Module
