Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports DevCommerc8ak.DevCommerc8ak.DTO
Imports DevCommerc8ak.DevCommerc8ak.Finance

Namespace DevCommerc8ak.Services
    Public Class CategorieDepenseService
        Private ReadOnly _repo As CategorieDepenseRepository
        Public Sub New(repo As CategorieDepenseRepository)
            _repo = repo
        End Sub
        Public Sub Ajouter(libelle As String)
            _repo.Ajouter(libelle)
        End Sub
        Public Sub Supprimer(id As Integer)
            _repo.Supprimer(id)
        End Sub
        Public Function GetAll() As DataTable
            Return _repo.GetAll()
        End Function
    End Class

    Public Class DepenseServiceFinance
        Private ReadOnly _repo As DepenseRepositoryFinance
        Private ReadOnly _banqueService As BanqueService
        Private ReadOnly _caisseService As CaisseService

        Public Sub New(repo As DepenseRepositoryFinance, banqueService As BanqueService, caisseService As CaisseService)
            _repo = repo
            _banqueService = banqueService
            _caisseService = caisseService
        End Sub

        Public Sub AjouterDepense(depense As DepenseDTOFinance)
            Dim soldeDisponible As Decimal = 0
            If depense.Source = "Caisse" Then
                soldeDisponible = _caisseService.GetSoldeCaisse(DateTime.Now, depense.Devise)
            Else
                soldeDisponible = _banqueService.GetSolde(depense.Devise)
            End If

            If depense.Montant > soldeDisponible Then
                Throw New Exception("Solde insuffisant en " & depense.Source & " (" & soldeDisponible.ToString() & " " & depense.Devise & " disponible).")
            End If

            _repo.Ajouter(depense)

            If depense.Source = "Banque" Then
                _banqueService.Retrait(depense.Montant, depense.Devise, "Dépense: " & depense.Categorie & " - " & depense.Description)
            End If

            AppEvents.OnDepenseAjoutee()
            AppEvents.OnCaisseModifiee()
            AppEvents.OnAnalyseVenteModifiee()
            AppEvents.OnDataChanged()
        End Sub

        Public Function GetHistorique(Optional annee As Integer = 0, Optional mois As Integer = 0) As DataTable
            If annee <= 0 Then
                Return _repo.GetAll()
            End If

            Return _repo.GetHistorique(annee, mois)
        End Function

        Public Function GetStatsParCategorie() As DataTable
            Return _repo.GetStatsParCategorie()
        End Function

        Public Function GetRapportDepenses(annee As Integer, Optional mois As Integer = 0) As DataTable
            Return _repo.GetRapportDepenses(annee, mois)
        End Function
    End Class

    Public Class BanqueService
        Private ReadOnly _repo As BanqueRepository
        Public Sub New(repo As BanqueRepository)
            _repo = repo
        End Sub
        Public Sub Depot(montant As Decimal, devise As String, description As String)
            Dim op As New BanqueDTO With {.TypeOperation = "Depot", .Montant = montant, .Devise = devise, .Description = description, .DateOperation = DateTime.Now}
            _repo.AjouterOperation(op)
        End Sub
        Public Sub Retrait(montant As Decimal, devise As String, description As String)
            Dim op As New BanqueDTO With {.TypeOperation = "Retrait", .Montant = montant, .Devise = devise, .Description = description, .DateOperation = DateTime.Now}
            _repo.AjouterOperation(op)
        End Sub
        Public Function GetSolde(devise As String) As Decimal
            Return _repo.GetSoldeParDevise(devise)
        End Function
        Public Function GetHistorique() As DataTable
            Return _repo.GetHistorique()
        End Function
    End Class

    Public Class CaisseService
        Private ReadOnly _caisseRepo As CaisseRepository
        Private ReadOnly _depenseRepo As DepenseRepositoryFinance
        Private ReadOnly _banqueService As BanqueService

        Public Sub New(caisseRepo As CaisseRepository, depenseRepo As DepenseRepositoryFinance, banqueService As BanqueService)
            _caisseRepo = caisseRepo
            _depenseRepo = depenseRepo
            _banqueService = banqueService
        End Sub

        Public Function GetSoldeCaisse(dateJour As DateTime, devise As String) As Decimal
            Dim encaisse As Decimal = _caisseRepo.GetEncaisse(dateJour, devise)
            Dim depenses As Decimal = _depenseRepo.GetSommeParDevise(dateJour, devise, "Caisse")
            Return encaisse - depenses
        End Function

        Public Function GetEncaisse(dateJour As DateTime, devise As String) As Decimal
            Return _caisseRepo.GetEncaisse(dateJour, devise)
        End Function

        Public Function EstMontantUsdDisponible() As Boolean
            Return _caisseRepo.PeutCalculerMontantUsd()
        End Function

        Public Function GetDepensesCaisse(dateJour As DateTime, devise As String) As Decimal
            Return _depenseRepo.GetSommeParDevise(dateJour, devise, "Caisse")
        End Function

        Public Sub EnregistrerComptagePhysique(dateCaisse As DateTime, montantPhysiqueFc As Decimal, motif As String, observation As String)
            If montantPhysiqueFc < 0D Then
                Throw New ArgumentException("Le montant physique ne peut pas être négatif.")
            End If

            Dim soldeTheoriqueFc As Decimal = GetSoldeCaisse(dateCaisse, "FC")
            Dim ecartFc As Decimal = montantPhysiqueFc - soldeTheoriqueFc
            If ecartFc <> 0D AndAlso String.IsNullOrWhiteSpace(motif) Then
                Throw New ArgumentException("Le motif est obligatoire lorsqu'il existe un écart de caisse.")
            End If
            If String.Equals(motif, "Autre", StringComparison.OrdinalIgnoreCase) AndAlso String.IsNullOrWhiteSpace(observation) Then
                Throw New ArgumentException("L'observation est obligatoire lorsque le motif est Autre.")
            End If

            _caisseRepo.EnregistrerComptagePhysique(dateCaisse, SessionUtilisateur.UtilisateurId, SessionUtilisateur.NomUtilisateur, SessionUtilisateur.Role, soldeTheoriqueFc, montantPhysiqueFc, motif, observation)
            AuditActionService.Enregistrer("Finance", "Contrôle caisse physique", "Comptage physique validé. Théorique=" & soldeTheoriqueFc.ToString("N2") & " FC; Physique=" & montantPhysiqueFc.ToString("N2") & " FC; Écart=" & ecartFc.ToString("N2") & " FC.")
            AppEvents.OnCaisseModifiee()
            AppEvents.OnDataChanged()
        End Sub

        Public Sub ClotureAutomatique()
            Dim derniereCloture As DateTime? = _caisseRepo.GetDerniereCloture()
            Dim aujourdhui As DateTime = DateTime.Now.Date
            If Not derniereCloture.HasValue OrElse derniereCloture.Value.Date < aujourdhui Then
                Dim log As New ProductionLogService()
                Dim dateACloturer As DateTime = If(derniereCloture.HasValue, derniereCloture.Value.AddDays(1), aujourdhui.AddDays(-1))
                While dateACloturer < aujourdhui
                    Dim soldeFC As Decimal = GetSoldeCaisse(dateACloturer, "FC")
                    Dim soldeUSD As Decimal = GetSoldeCaisse(dateACloturer, "USD")
                    If soldeFC > 0 Then _banqueService.Depot(soldeFC, "FC", "Clôture automatique du " & dateACloturer.ToString("dd/MM/yyyy"))
                    If soldeUSD > 0D Then
                        log.Info("Finance", "ClotureAutomatique", "Montant USD affiché à titre informatif uniquement le " & dateACloturer.ToString("dd/MM/yyyy") & ". Aucun versement banque USD n'a été généré.")
                    End If
                    _caisseRepo.EnregistrerCloture(dateACloturer, soldeFC, 0D)
                    dateACloturer = dateACloturer.AddDays(1)
                End While

                AppEvents.OnCaisseModifiee()
                AppEvents.OnDataChanged()
            End If
        End Sub
    End Class
End Namespace
