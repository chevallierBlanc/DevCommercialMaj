Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class ParametreRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        ' Cree la table Parametres si absente.
        Public Sub AssurerTable()
            ' Schéma géré par le script SQL de déploiement.
        End Sub

        ' Retourne les parametres uniques.
        Public Function Obtenir() As ParametreDTO
            Dim sql As String = "SELECT TOP 1 RemiseMaxPourcent, SeuilStockCritique, AlerteExpirationJours, ImprimanteA4, ImprimanteTicket, DeviseParDefaut, TauxUsd, ScannerIp, ScannerPort, ScannerActif, NomMagasin, AdresseMagasin, TelephoneMagasin, ModeSombre, LogoPath, ApercuAvantImpression, ImpressionCouleur FROM Parametres"
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, Nothing)
            If dt.Rows.Count = 0 Then
                Return Nothing
            End If
            Dim row As DataRow = dt.Rows(0)
            Return New ParametreDTO With {
                .RemiseMaxPourcent = Convert.ToDecimal(row("RemiseMaxPourcent")),
                .SeuilStockCritique = Convert.ToDecimal(row("SeuilStockCritique")),
                .AlerteExpirationJours = Convert.ToInt32(row("AlerteExpirationJours")),
                .ImprimanteA4 = If(row.IsNull("ImprimanteA4"), "", Convert.ToString(row("ImprimanteA4"))),
                .ImprimanteTicket = If(row.IsNull("ImprimanteTicket"), "", Convert.ToString(row("ImprimanteTicket"))),
                .DeviseParDefaut = Convert.ToString(row("DeviseParDefaut")),
                .TauxUsd = Convert.ToDecimal(row("TauxUsd")),
                .ScannerIp = If(row.IsNull("ScannerIp"), "", Convert.ToString(row("ScannerIp"))),
                .ScannerPort = Convert.ToInt32(row("ScannerPort")),
                .ScannerActif = Convert.ToBoolean(row("ScannerActif")),
                .NomMagasin = If(row.IsNull("NomMagasin"), "", Convert.ToString(row("NomMagasin"))),
                .AdresseMagasin = If(row.IsNull("AdresseMagasin"), "", Convert.ToString(row("AdresseMagasin"))),
                .TelephoneMagasin = If(row.IsNull("TelephoneMagasin"), "", Convert.ToString(row("TelephoneMagasin"))),
                .ModeSombre = Convert.ToBoolean(row("ModeSombre")),
                .LogoPath = If(row.IsNull("LogoPath"), "", Convert.ToString(row("LogoPath"))),
                .ApercuAvantImpression = Convert.ToBoolean(row("ApercuAvantImpression")),
                .ImpressionCouleur = Convert.ToBoolean(row("ImpressionCouleur"))
            }
        End Function

        ' Met a jour les parametres.
        Public Sub MettreAJour(p As ParametreDTO)
            Dim sql As String = "UPDATE Parametres SET RemiseMaxPourcent=@RemiseMaxPourcent, SeuilStockCritique=@SeuilStockCritique, " &
                                "AlerteExpirationJours=@AlerteExpirationJours, ImprimanteA4=@ImprimanteA4, ImprimanteTicket=@ImprimanteTicket, " &
                                "DeviseParDefaut=@DeviseParDefaut, TauxUsd=@TauxUsd, ScannerIp=@ScannerIp, ScannerPort=@ScannerPort, ScannerActif=@ScannerActif, " &
                                "NomMagasin=@NomMagasin, AdresseMagasin=@AdresseMagasin, TelephoneMagasin=@TelephoneMagasin, ModeSombre=@ModeSombre, " &
                                "LogoPath=@LogoPath, ApercuAvantImpression=@ApercuAvantImpression, ImpressionCouleur=@ImpressionCouleur"
            Dim prms As New List(Of SqlParameter) From {
                New SqlParameter("@RemiseMaxPourcent", p.RemiseMaxPourcent),
                New SqlParameter("@SeuilStockCritique", p.SeuilStockCritique),
                New SqlParameter("@AlerteExpirationJours", p.AlerteExpirationJours),
                New SqlParameter("@ImprimanteA4", If(p.ImprimanteA4, CType(DBNull.Value, Object))),
                New SqlParameter("@ImprimanteTicket", If(p.ImprimanteTicket, CType(DBNull.Value, Object))),
                New SqlParameter("@DeviseParDefaut", p.DeviseParDefaut),
                New SqlParameter("@TauxUsd", p.TauxUsd),
                New SqlParameter("@ScannerIp", If(p.ScannerIp, CType(DBNull.Value, Object))),
                New SqlParameter("@ScannerPort", p.ScannerPort),
                New SqlParameter("@ScannerActif", p.ScannerActif),
                New SqlParameter("@NomMagasin", If(p.NomMagasin, CType(DBNull.Value, Object))),
                New SqlParameter("@AdresseMagasin", If(p.AdresseMagasin, CType(DBNull.Value, Object))),
                New SqlParameter("@TelephoneMagasin", If(p.TelephoneMagasin, CType(DBNull.Value, Object))),
                New SqlParameter("@ModeSombre", p.ModeSombre),
                New SqlParameter("@LogoPath", If(p.LogoPath, CType(DBNull.Value, Object))),
                New SqlParameter("@ApercuAvantImpression", p.ApercuAvantImpression),
                New SqlParameter("@ImpressionCouleur", p.ImpressionCouleur)
            }
            _dal.ExecuterNonRequete(sql, CommandType.Text, prms)
        End Sub
    End Class
End Namespace
