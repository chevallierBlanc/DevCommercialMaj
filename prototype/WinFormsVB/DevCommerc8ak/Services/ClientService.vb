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
