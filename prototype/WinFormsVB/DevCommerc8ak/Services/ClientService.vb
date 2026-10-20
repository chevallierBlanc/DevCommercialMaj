Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class ClientService
        Private ReadOnly _repo As ClientRepository

        Public Sub New(repo As ClientRepository)
            _repo = repo
        End Sub

        ' Cree un client.
        Public Function Ajouter(client As Client) As Integer
            Return _repo.Ajouter(client)
        End Function

        ' Liste des clients.
        Public Function Lister() As List(Of ClientDTO)
            Return _repo.Lister()
        End Function
        Public Function ClasserAchats(annee As Integer, mois As Integer) As DataTable
            Return _repo.ClasserAchats(ClientAnalyseRegles.DebutPeriode(annee, mois), ClientAnalyseRegles.FinPeriode(annee, mois))
        End Function
        Public Function RepartirAchats(clientId As Integer, annee As Integer, mois As Integer) As DataTable
            If clientId <= 0 Then Throw New ArgumentException("Client invalide.")
            Return _repo.RepartirAchats(clientId, ClientAnalyseRegles.DebutPeriode(annee, mois), ClientAnalyseRegles.FinPeriode(annee, mois))
        End Function

        ' Met a jour un client.
        Public Function MettreAJour(client As Client) As Integer
            Return _repo.MettreAJour(client)
        End Function

        ' Retourne un client par telephone.
        Public Function ObtenirParTelephone(telephone As String) As ClientDTO
            Return _repo.ObtenirParTelephone(telephone)
        End Function

        ' Supprime un client.
        Public Function Supprimer(clientId As Integer) As Integer
            Return _repo.Supprimer(clientId)
        End Function
    End Class
End Namespace
