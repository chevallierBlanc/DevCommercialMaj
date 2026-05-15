Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class ProduitService
        Private ReadOnly _repo As ProduitRepository

        Public Sub New(repo As ProduitRepository)
            _repo = repo
        End Sub

        ' Cree un produit.
        Public Function Ajouter(produit As Produit) As Integer
            Return _repo.Ajouter(produit)
        End Function

        ' Liste des produits.
        Public Function Lister() As List(Of ProduitDTO)
            Return _repo.Lister()
        End Function

        ' Met a jour un produit.
        Public Function MettreAJour(produit As Produit) As Integer
            Return _repo.MettreAJour(produit)
        End Function

        ' Supprime un produit.
        Public Function Supprimer(produitId As Integer) As Integer
            Return _repo.Supprimer(produitId)
        End Function

        Public Function ListerHistoriquePrixTable(produitId As Integer?, dateDebut As Date?, dateFin As Date?) As DataTable
            Return _repo.ListerHistoriquePrixTable(produitId, dateDebut, dateFin)
        End Function

        Public Function TopProduitsVendus(annee As Integer) As DataTable
            Return _repo.TopProduitsVendus(annee)
        End Function

        Public Function ProduitPlusVenduParMois(annee As Integer) As DataTable
            Return _repo.ProduitPlusVenduParMois(annee)
        End Function

        Public Function RepartitionParCategorie() As DataTable
            Return _repo.RepartitionParCategorie()
        End Function

        Public Function KpiProduits() As DataTable
            Return _repo.KpiProduits()
        End Function
    End Class
End Namespace
