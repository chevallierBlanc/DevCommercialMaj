Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class FournisseurService
        Private ReadOnly _repo As FournisseurRepository

        Public Sub New(repo As FournisseurRepository)
            _repo = repo
        End Sub

        ' Cree un fournisseur.
        Public Function Ajouter(fournisseur As Fournisseur) As Integer
            Return _repo.Ajouter(fournisseur)
        End Function

        ' Liste des fournisseurs.
        Public Function Lister() As List(Of FournisseurDTO)
            Return _repo.Lister()
        End Function

        ' Met a jour un fournisseur.
        Public Function MettreAJour(fournisseur As Fournisseur) As Integer
            Return _repo.MettreAJour(fournisseur)
        End Function

        ' Supprime un fournisseur.
        Public Function Supprimer(fournisseurId As Integer) As Integer
            Return _repo.Supprimer(fournisseurId)
        End Function
    End Class
End Namespace
