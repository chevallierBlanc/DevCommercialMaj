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
        Refus(Of Exception)(Sub() caisse.EncaisserFacture(id, "ESPECES", "TEST", 20D, 0D, "FC", user))
        Verifier(Scalar("SELECT COUNT(*) FROM dbo.Paiements WHERE FactureVenteId=" & id.ToString()) = 1, "Double encaissement refusé")
        Refus(Of InvalidOperationException)(Sub() operations.Annuler(id, "Facture payée"))
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
        SessionUtilisateur.Reinitialiser()
        Refus(Of UnauthorizedAccessException)(Sub() operations.Enregistrer(NouvelleFacture(Guid.NewGuid().ToString("N")), Lignes(1D), String.Empty, Nothing))
        Console.WriteLine("PASS SQL : " & compteur.ToString() & " scénarios. Les fixtures et audits restent dans la copie de test.")
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
