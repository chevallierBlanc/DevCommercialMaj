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
            Dim sql As String = "IF OBJECT_ID('Parametres', 'U') IS NULL " &
                                "BEGIN " &
                                "CREATE TABLE Parametres (" &
                                "ParametreId INT IDENTITY(1,1) PRIMARY KEY, " &
                                "RemiseMaxPourcent DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                                "SeuilStockCritique DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                                "AlerteExpirationJours INT NOT NULL DEFAULT 30, " &
                                "ImprimanteA4 NVARCHAR(200) NULL, " &
                                "ImprimanteTicket NVARCHAR(200) NULL, " &
                                "DeviseParDefaut NVARCHAR(10) NOT NULL DEFAULT 'FC', " &
                                "TauxUsd DECIMAL(18,2) NOT NULL DEFAULT 0, " &
                                "ScannerIp NVARCHAR(50) NULL, " &
                                "ScannerPort INT NOT NULL DEFAULT 9000, " &
                                "ScannerActif BIT NOT NULL DEFAULT 0, " &
                                "NomMagasin NVARCHAR(200) NULL, " &
                                "AdresseMagasin NVARCHAR(300) NULL, " &
                                "TelephoneMagasin NVARCHAR(50) NULL, " &
                                "ModeSombre BIT NOT NULL DEFAULT 0, " &
                                "LogoPath NVARCHAR(300) NULL, " &
                                "ApercuAvantImpression BIT NOT NULL DEFAULT 0, " &
                                "ImpressionCouleur BIT NOT NULL DEFAULT 0 " &
                                "); " &
                                "INSERT INTO Parametres (RemiseMaxPourcent, SeuilStockCritique, AlerteExpirationJours, DeviseParDefaut, TauxUsd, ScannerPort, ScannerActif, ModeSombre) " &
                                "VALUES (0, 0, 30, 'FC', 0, 9000, 0, 0); " &
                                "END"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)

            Dim sqlAlter As String = "IF COL_LENGTH('Parametres','ScannerIp') IS NULL ALTER TABLE Parametres ADD ScannerIp NVARCHAR(50) NULL; " &
                                     "IF COL_LENGTH('Parametres','ScannerPort') IS NULL ALTER TABLE Parametres ADD ScannerPort INT NOT NULL DEFAULT 9000; " &
                                     "IF COL_LENGTH('Parametres','ScannerActif') IS NULL ALTER TABLE Parametres ADD ScannerActif BIT NOT NULL DEFAULT 0; " &
                                     "IF COL_LENGTH('Parametres','NomMagasin') IS NULL ALTER TABLE Parametres ADD NomMagasin NVARCHAR(200) NULL; " &
                                     "IF COL_LENGTH('Parametres','AdresseMagasin') IS NULL ALTER TABLE Parametres ADD AdresseMagasin NVARCHAR(300) NULL; " &
                                     "IF COL_LENGTH('Parametres','TelephoneMagasin') IS NULL ALTER TABLE Parametres ADD TelephoneMagasin NVARCHAR(50) NULL; " &
                                     "IF COL_LENGTH('Parametres','ModeSombre') IS NULL ALTER TABLE Parametres ADD ModeSombre BIT NOT NULL DEFAULT 0; " &
                                     "IF COL_LENGTH('Parametres','LogoPath') IS NULL ALTER TABLE Parametres ADD LogoPath NVARCHAR(300) NULL; " &
                                     "IF COL_LENGTH('Parametres','ApercuAvantImpression') IS NULL ALTER TABLE Parametres ADD ApercuAvantImpression BIT NOT NULL DEFAULT 0; " &
                                     "IF COL_LENGTH('Parametres','ImpressionCouleur') IS NULL ALTER TABLE Parametres ADD ImpressionCouleur BIT NOT NULL DEFAULT 0;"
            _dal.ExecuterNonRequete(sqlAlter, CommandType.Text, Nothing)
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
