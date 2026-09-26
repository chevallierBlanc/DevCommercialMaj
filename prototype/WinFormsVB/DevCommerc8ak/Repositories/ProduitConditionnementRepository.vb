Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.SqlClient

Namespace DevCommerc8ak
    Public Class ProduitConditionnementRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Function ListerParProduit(produitId As Integer, Optional actifSeulement As Boolean = True) As List(Of ProduitConditionnementDTO)
            Dim sql As String =
                "SELECT pc.ProduitConditionnementId, pc.ProduitId, pc.UniteMesureId, pc.ConditionnementParentId, " &
                "pc.FacteurVersParent, pc.FacteurVersBase, pc.Niveau, pc.EstUniteBase, pc.EstAchetable, pc.EstVendable, " &
                "pc.AutoriseFraction, pc.OrdreAffichage, pc.EstActif, pc.CreeLe, pc.ModifieLe, pc.ModifiePar, " &
                "u.Code AS CodeUnite, u.Libelle AS LibelleUnite, u.Symbole AS SymboleUnite, u.CategorieUnite " &
                "FROM dbo.ProduitConditionnements pc " &
                "INNER JOIN dbo.UnitesMesure u ON u.UniteMesureId = pc.UniteMesureId " &
                "WHERE pc.ProduitId = @ProduitId " &
                If(actifSeulement, "AND pc.EstActif = 1 AND u.EstActif = 1 ", String.Empty) &
                "ORDER BY pc.FacteurVersBase DESC, pc.Niveau DESC, pc.OrdreAffichage ASC, u.Libelle ASC"

            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@ProduitId", produitId)
            }

            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            Dim resultat As New List(Of ProduitConditionnementDTO)()
            For Each row As DataRow In dt.Rows
                resultat.Add(Map(row))
            Next
            Return resultat
        End Function

        Public Function ObtenirParId(produitConditionnementId As Integer) As ProduitConditionnementDTO
            Dim sql As String =
                "SELECT pc.ProduitConditionnementId, pc.ProduitId, pc.UniteMesureId, pc.ConditionnementParentId, " &
                "pc.FacteurVersParent, pc.FacteurVersBase, pc.Niveau, pc.EstUniteBase, pc.EstAchetable, pc.EstVendable, " &
                "pc.AutoriseFraction, pc.OrdreAffichage, pc.EstActif, pc.CreeLe, pc.ModifieLe, pc.ModifiePar, " &
                "u.Code AS CodeUnite, u.Libelle AS LibelleUnite, u.Symbole AS SymboleUnite, u.CategorieUnite " &
                "FROM dbo.ProduitConditionnements pc " &
                "INNER JOIN dbo.UnitesMesure u ON u.UniteMesureId = pc.UniteMesureId " &
                "WHERE pc.ProduitConditionnementId = @ProduitConditionnementId"

            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@ProduitConditionnementId", produitConditionnementId)
            }

            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            If dt.Rows.Count = 0 Then Return Nothing
            Return Map(dt.Rows(0))
        End Function

        Private Shared Function Map(row As DataRow) As ProduitConditionnementDTO
            Dim dto As New ProduitConditionnementDTO With {
                .ProduitConditionnementId = Convert.ToInt32(row("ProduitConditionnementId")),
                .ProduitId = Convert.ToInt32(row("ProduitId")),
                .UniteMesureId = Convert.ToInt32(row("UniteMesureId")),
                .FacteurVersBase = Convert.ToDecimal(row("FacteurVersBase")),
                .Niveau = Convert.ToInt32(row("Niveau")),
                .EstUniteBase = Convert.ToBoolean(row("EstUniteBase")),
                .EstAchetable = Convert.ToBoolean(row("EstAchetable")),
                .EstVendable = Convert.ToBoolean(row("EstVendable")),
                .AutoriseFraction = Convert.ToBoolean(row("AutoriseFraction")),
                .OrdreAffichage = Convert.ToInt32(row("OrdreAffichage")),
                .EstActif = Convert.ToBoolean(row("EstActif")),
                .ModifiePar = If(row.IsNull("ModifiePar"), String.Empty, Convert.ToString(row("ModifiePar"))),
                .CodeUnite = Convert.ToString(row("CodeUnite")),
                .LibelleUnite = Convert.ToString(row("LibelleUnite")),
                .SymboleUnite = Convert.ToString(row("SymboleUnite")),
                .CategorieUnite = Convert.ToString(row("CategorieUnite"))
            }

            If Not row.IsNull("ConditionnementParentId") Then
                dto.ConditionnementParentId = Convert.ToInt32(row("ConditionnementParentId"))
            End If

            If Not row.IsNull("FacteurVersParent") Then
                dto.FacteurVersParent = Convert.ToDecimal(row("FacteurVersParent"))
            End If

            If Not row.IsNull("CreeLe") Then
                dto.CreeLe = Convert.ToDateTime(row("CreeLe"))
            End If

            If Not row.IsNull("ModifieLe") Then
                dto.ModifieLe = Convert.ToDateTime(row("ModifieLe"))
            End If

            Return dto
        End Function
    End Class
End Namespace
