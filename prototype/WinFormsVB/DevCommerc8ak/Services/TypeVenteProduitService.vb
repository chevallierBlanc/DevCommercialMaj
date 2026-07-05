Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Configuration

Namespace DevCommerc8ak
    Public Class TypeVenteProduitService
        Private Function ObtenirRepository() As TypeVenteProduitRepository
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Return New TypeVenteProduitRepository(dal)
        End Function

        Public Function ListerParProduit(produitId As Integer, Optional actifSeulement As Boolean = False) As List(Of TypeVenteProduitDTO)
            Return ObtenirRepository().ListerParProduit(produitId, actifSeulement)
        End Function

        Public Function Ajouter(dto As TypeVenteProduitDTO) As Integer
            Dim id As Integer = ObtenirRepository().Ajouter(dto)
            If id > 0 Then
                AppEvents.OnProduitModifie()
                AppEvents.OnDataChanged()
            End If
            Return id
        End Function

        Public Function MettreAJour(dto As TypeVenteProduitDTO) As Integer
            Dim resultat As Integer = ObtenirRepository().MettreAJour(dto)
            If resultat > 0 Then
                AppEvents.OnProduitModifie()
                AppEvents.OnDataChanged()
            End If
            Return resultat
        End Function

        Public Function ChangerEtat(typeVenteProduitId As Integer, actif As Boolean) As Integer
            Dim resultat As Integer = ObtenirRepository().ChangerEtat(typeVenteProduitId, actif)
            If resultat > 0 Then
                AppEvents.OnProduitModifie()
                AppEvents.OnDataChanged()
            End If
            Return resultat
        End Function
    End Class
End Namespace
