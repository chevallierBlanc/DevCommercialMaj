Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient
Imports System.Diagnostics


Namespace DevCommerc8ak
    Public Class ProduitService
        Private ReadOnly _repo As ProduitRepository

        Public Sub New(repo As ProduitRepository)
            _repo = repo
        End Sub

        ' Cree un produit.
        Public Function Ajouter(produit As Produit) As Integer
            Dim produitId As Integer = _repo.Ajouter(produit)
            If produitId > 0 Then
                AuditActionService.Enregistrer("Produits", "Création produit", "Produit " & produit.Libelle & " créé.")
                AppEvents.OnProduitModifie()
                AppEvents.OnDataChanged()
            End If
            Return produitId
        End Function

        ' Liste des produits.
        Public Function Lister() As List(Of ProduitDTO)
            Return _repo.Lister()
        End Function

        ' Met a jour un produit.
        Public Function MettreAJour(produit As Produit) As Integer
            Debug.WriteLine(String.Format(Globalization.CultureInfo.InvariantCulture, "[ProduitService] MettreAJour ProduitId={0}, PrixAchat={1}, PrixGros={2}, PrixDemi={3}, PrixDetail={4}, PrixQuart={5}, PrixDouzaine={6}, PrixSpecial={7}, CoefficientGros={8}, UnitePrincipale={9}, UniteSecondaire={10}, ConversionUnite={11}, VenteGros={12}, VenteDemi={13}, VenteDetail={14}, VenteDouzaine={15}",
                                         produit.ProduitId,
                                         produit.PrixAchat,
                                         produit.PrixGros,
                                         produit.PrixDemi,
                                         produit.PrixDetail,
                                         produit.PrixQuart,
                                         produit.PrixDouzaine,
                                         produit.PrixSpecial,
                                         produit.CoefficientGros,
                                         If(produit.UnitePrincipale, String.Empty),
                                         If(produit.UniteSecondaire, String.Empty),
                                         produit.ConversionUnite,
                                         produit.VenteGros,
                                         produit.VenteDemi,
                                         produit.VenteDetail,
                                         produit.VenteDouzaine))
            Dim resultat As Integer = _repo.MettreAJour(produit)
            If resultat > 0 Then
                AuditActionService.Enregistrer("Produits", "Modification prix produit", "Produit " & produit.Libelle & " mis à jour.")
                AppEvents.OnProduitModifie()
                AppEvents.OnDataChanged()
            End If
            Return resultat
        End Function

        ' Supprime un produit.
        Public Function Supprimer(produitId As Integer) As Integer
            Dim resultat As Integer = _repo.Supprimer(produitId)
            If resultat > 0 Then
                AppEvents.OnProduitModifie()
                AppEvents.OnDataChanged()
            End If
            Return resultat
        End Function

        Public Function ListerHistoriquePrixTable(produitId As Integer?, dateDebut As Date?, dateFin As Date?) As DataTable
            Return _repo.ListerHistoriquePrixTable(produitId, dateDebut, dateFin)
        End Function

        Public Function AfficherQteProduitSelect(produitId As Integer) As Integer
            Return _repo.ListerQteProduit(produitId)
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

        Public Function ExisteParLibelle(libelle As String, Optional produitIdExclu As Integer? = Nothing) As Boolean
            Return _repo.ExisteParLibelle(libelle, produitIdExclu)
        End Function

        Public Function ObtenirParId(produitId As Integer) As ProduitDTO
            Return _repo.ObtenirParId(produitId)
        End Function
    End Class
End Namespace
