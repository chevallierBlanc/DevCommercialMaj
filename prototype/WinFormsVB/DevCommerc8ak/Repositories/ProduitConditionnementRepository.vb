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

        Public Function ListerUnites(Optional actifSeulement As Boolean = True) As List(Of UniteMesureDTO)
            Dim sql As String =
                "SELECT UniteMesureId, Code, Libelle, Symbole, CategorieUnite, AutoriseFraction, NombreDecimales, EstActif, CreeLe, ModifieLe " &
                "FROM dbo.UnitesMesure " &
                If(actifSeulement, "WHERE EstActif = 1 ", String.Empty) &
                "ORDER BY CategorieUnite, Libelle"

            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, Nothing)
            Dim resultat As New List(Of UniteMesureDTO)()
            For Each row As DataRow In dt.Rows
                resultat.Add(MapUnite(row))
            Next
            Return resultat
        End Function

        Public Function Enregistrer(conditionnement As ProduitConditionnementDTO) As Integer
            If conditionnement.ProduitConditionnementId > 0 Then
                Dim sqlUpdate As String =
                    "UPDATE dbo.ProduitConditionnements SET UniteMesureId=@UniteMesureId, ConditionnementParentId=@ConditionnementParentId, " &
                    "FacteurVersParent=@FacteurVersParent, FacteurVersBase=@FacteurVersBase, Niveau=@Niveau, EstUniteBase=@EstUniteBase, " &
                    "EstAchetable=@EstAchetable, EstVendable=@EstVendable, AutoriseFraction=@AutoriseFraction, OrdreAffichage=@OrdreAffichage, " &
                    "EstActif=@EstActif, ModifieLe=SYSDATETIME(), ModifiePar=@ModifiePar " &
                    "WHERE ProduitConditionnementId=@ProduitConditionnementId"
                _dal.ExecuterNonRequete(sqlUpdate, CommandType.Text, ConstruireParametres(conditionnement))
                Return conditionnement.ProduitConditionnementId
            End If

            Dim sqlInsert As String =
                "INSERT INTO dbo.ProduitConditionnements (ProduitId, UniteMesureId, ConditionnementParentId, FacteurVersParent, FacteurVersBase, Niveau, EstUniteBase, EstAchetable, EstVendable, AutoriseFraction, OrdreAffichage, EstActif, ModifiePar) " &
                "VALUES (@ProduitId, @UniteMesureId, @ConditionnementParentId, @FacteurVersParent, @FacteurVersBase, @Niveau, @EstUniteBase, @EstAchetable, @EstVendable, @AutoriseFraction, @OrdreAffichage, @EstActif, @ModifiePar); " &
                "SELECT CAST(SCOPE_IDENTITY() AS INT);"
            Return Convert.ToInt32(_dal.ExecuterScalaire(sqlInsert, CommandType.Text, ConstruireParametres(conditionnement)))
        End Function

        Public Sub Desactiver(produitConditionnementId As Integer, modifiePar As String)
            Dim sql As String = "UPDATE dbo.ProduitConditionnements SET EstActif=0, ModifieLe=SYSDATETIME(), ModifiePar=@ModifiePar WHERE ProduitConditionnementId=@Id"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@Id", produitConditionnementId),
                New SqlParameter("@ModifiePar", If(String.IsNullOrWhiteSpace(modifiePar), "SYSTEM", modifiePar.Trim()))
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Public Sub Activer(produitConditionnementId As Integer, modifiePar As String)
            Dim sql As String = "UPDATE dbo.ProduitConditionnements SET EstActif=1, ModifieLe=SYSDATETIME(), ModifiePar=@ModifiePar WHERE ProduitConditionnementId=@Id"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@Id", produitConditionnementId),
                New SqlParameter("@ModifiePar", If(String.IsNullOrWhiteSpace(modifiePar), "SYSTEM", modifiePar.Trim()))
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Private Shared Function ConstruireParametres(conditionnement As ProduitConditionnementDTO) As List(Of SqlParameter)
            Return New List(Of SqlParameter) From {
                New SqlParameter("@ProduitConditionnementId", conditionnement.ProduitConditionnementId),
                New SqlParameter("@ProduitId", conditionnement.ProduitId),
                New SqlParameter("@UniteMesureId", conditionnement.UniteMesureId),
                New SqlParameter("@ConditionnementParentId", If(conditionnement.ConditionnementParentId.HasValue, CType(conditionnement.ConditionnementParentId.Value, Object), DBNull.Value)),
                New SqlParameter("@FacteurVersParent", If(conditionnement.FacteurVersParent.HasValue, CType(conditionnement.FacteurVersParent.Value, Object), DBNull.Value)),
                New SqlParameter("@FacteurVersBase", conditionnement.FacteurVersBase),
                New SqlParameter("@Niveau", conditionnement.Niveau),
                New SqlParameter("@EstUniteBase", conditionnement.EstUniteBase),
                New SqlParameter("@EstAchetable", conditionnement.EstAchetable),
                New SqlParameter("@EstVendable", conditionnement.EstVendable),
                New SqlParameter("@AutoriseFraction", conditionnement.AutoriseFraction),
                New SqlParameter("@OrdreAffichage", conditionnement.OrdreAffichage),
                New SqlParameter("@EstActif", conditionnement.EstActif),
                New SqlParameter("@ModifiePar", If(String.IsNullOrWhiteSpace(conditionnement.ModifiePar), "SYSTEM", conditionnement.ModifiePar.Trim()))
            }
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

        Private Shared Function MapUnite(row As DataRow) As UniteMesureDTO
            Dim dto As New UniteMesureDTO With {
                .UniteMesureId = Convert.ToInt32(row("UniteMesureId")),
                .Code = Convert.ToString(row("Code")),
                .Libelle = Convert.ToString(row("Libelle")),
                .Symbole = If(row.IsNull("Symbole"), String.Empty, Convert.ToString(row("Symbole"))),
                .CategorieUnite = Convert.ToString(row("CategorieUnite")),
                .AutoriseFraction = Convert.ToBoolean(row("AutoriseFraction")),
                .NombreDecimales = Convert.ToInt32(row("NombreDecimales")),
                .EstActif = Convert.ToBoolean(row("EstActif"))
            }

            If Not row.IsNull("CreeLe") Then dto.CreeLe = Convert.ToDateTime(row("CreeLe"))
            If Not row.IsNull("ModifieLe") Then dto.ModifieLe = Convert.ToDateTime(row("ModifieLe"))
            Return dto
        End Function
    End Class
End Namespace
