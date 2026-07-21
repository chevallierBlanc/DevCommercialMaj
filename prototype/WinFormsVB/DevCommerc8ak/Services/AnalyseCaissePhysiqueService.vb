Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports DevCommerc8ak.DevCommerc8ak
Imports DevCommerc8ak.DevCommerc8ak.DTO

Namespace DevCommerc8ak.Services
    Public Class AnalyseCaissePhysiqueService
        Private ReadOnly _repo As AnalyseCaissePhysiqueRepository

        Public Sub New(repo As AnalyseCaissePhysiqueRepository)
            _repo = repo
        End Sub

        Public Function ListerClotures(filtre As AnalyseCaissePhysiqueFiltreDTO) As DataTable
            Return _repo.ListerClotures(NormaliserFiltre(filtre))
        End Function

        Public Function ObtenirKpiClotures(filtre As AnalyseCaissePhysiqueFiltreDTO) As DataTable
            Return _repo.ObtenirKpiClotures(NormaliserFiltre(filtre))
        End Function

        Public Function ObtenirSyntheseParUtilisateur(filtre As AnalyseCaissePhysiqueFiltreDTO) As DataTable
            Return _repo.ObtenirSyntheseParUtilisateur(NormaliserFiltre(filtre))
        End Function

        Public Function ListerHistoriqueStatuts(filtre As AnalyseCaissePhysiqueFiltreDTO) As DataTable
            Return _repo.ListerHistoriqueStatuts(NormaliserFiltre(filtre))
        End Function

        Public Function ObtenirEvolutionEcarts(filtre As AnalyseCaissePhysiqueFiltreDTO) As DataTable
            Return _repo.ObtenirEvolutionEcarts(NormaliserFiltre(filtre))
        End Function

        Public Function ObtenirRepartitionStatuts(filtre As AnalyseCaissePhysiqueFiltreDTO) As DataTable
            Return _repo.ObtenirRepartitionStatuts(NormaliserFiltre(filtre))
        End Function

        Public Function ObtenirEcartsParUtilisateur(filtre As AnalyseCaissePhysiqueFiltreDTO) As DataTable
            Return _repo.ObtenirEcartsParUtilisateur(NormaliserFiltre(filtre))
        End Function

        Public Sub RegulariserCloture(dto As RegularisationCaissePhysiqueDTO)
            If dto Is Nothing Then Throw New ArgumentException("Régularisation invalide.")
            If dto.ClotureCaisseId <= 0 Then Throw New ArgumentException("Sélectionnez une clôture à régulariser.")
            If String.IsNullOrWhiteSpace(dto.NouveauStatut) Then Throw New ArgumentException("Sélectionnez le nouveau statut.")
            If dto.MontantRegularise < 0D Then Throw New ArgumentException("Le montant régularisé ne peut pas être négatif.")

            _repo.RegulariserCloture(dto, SessionUtilisateur.UtilisateurId, SessionUtilisateur.NomUtilisateur, SessionUtilisateur.Role)
            AuditActionService.Enregistrer("Analyse caisse physique", "Régularisation", "Clôture " & dto.ClotureCaisseId.ToString() & " passée au statut " & dto.NouveauStatut & ".")
            AppEvents.OnCaissePhysiqueModifiee()
            AppEvents.OnCaisseModifiee()
            AppEvents.OnDataChanged()
        End Sub

        Private Function NormaliserFiltre(filtre As AnalyseCaissePhysiqueFiltreDTO) As AnalyseCaissePhysiqueFiltreDTO
            If filtre Is Nothing Then
                Return New AnalyseCaissePhysiqueFiltreDTO With {.DateDebut = DateTime.Now.Date, .DateFin = DateTime.Now.Date}
            End If
            If filtre.DateDebut = DateTime.MinValue Then filtre.DateDebut = DateTime.Now.Date
            If filtre.DateFin = DateTime.MinValue Then filtre.DateFin = filtre.DateDebut
            If filtre.DateFin.Date < filtre.DateDebut.Date Then filtre.DateFin = filtre.DateDebut
            Return filtre
        End Function
    End Class
End Namespace
