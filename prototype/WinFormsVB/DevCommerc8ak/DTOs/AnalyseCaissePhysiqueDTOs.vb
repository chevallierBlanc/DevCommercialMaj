Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak.DTO
    Public Class AnalyseCaissePhysiqueFiltreDTO
        Public Property DateDebut As DateTime
        Public Property DateFin As DateTime
        Public Property Utilisateur As String
        Public Property RoleSession As String
        Public Property Statut As String
    End Class

    Public Class RegularisationCaissePhysiqueDTO
        Public Property ClotureCaisseId As Integer
        Public Property NouveauStatut As String
        Public Property Motif As String
        Public Property Observation As String
        Public Property MontantRegularise As Decimal
        Public Property ModeRegularisation As String
        Public Property Reference As String
    End Class
End Namespace
