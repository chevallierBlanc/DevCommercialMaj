Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient

Namespace DevCommerc8ak
    Public Class ApprovisionnementService
        Private ReadOnly _dal As DAL
        Private ReadOnly _repo As BonApprovisionnementRepository

        Public Sub New(dal As DAL, repo As BonApprovisionnementRepository)
            _dal = dal
            _repo = repo
        End Sub

        ' Genere automatiquement un bon pour produits en rupture/seuil critique.
        Public Function GenererBonAuto(seuil As Decimal, creePar As Integer, Optional fournisseurId As Integer? = Nothing, Optional typePaiement As String = "") As Integer
            _repo.AssurerTables()
            Dim dt As DataTable = _repo.ListerSuggestionsAuto(seuil)
            If dt.Rows.Count = 0 Then Return 0

            Dim bonId As Integer = _repo.CreerBon(fournisseurId, typePaiement, creePar)
            For Each row As DataRow In dt.Rows
                Dim produitId As Integer = Convert.ToInt32(row("ProduitId"))
                Dim quantite As Decimal = Convert.ToDecimal(row("QuantiteSuggeree"))
                Dim prix As Decimal = Convert.ToDecimal(row("PrixAchatPrecedent"))
                If quantite > 0D Then
                    _repo.AjouterLigne(bonId, produitId, quantite, prix)
                End If
            Next
            Return bonId
        End Function
    End Class
End Namespace
