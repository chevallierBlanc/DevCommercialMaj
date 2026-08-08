Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.SqlClient

Namespace DevCommerc8ak
    Public Class TypeVenteProduitRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerTable()
        End Sub

        Private Function ObtenirNomUtilisateur() As String
            Try
                If Not String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur) Then
                    Return SessionUtilisateur.NomUtilisateur.Trim()
                End If
            Catch
            End Try

            Return "SYSTEM"
        End Function

        Private Sub AssurerTable()
            Dim sql As String =
                "IF OBJECT_ID('dbo.TypesVenteProduit', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.TypesVenteProduit (" &
                "TypeVenteProduitId INT IDENTITY(1,1) PRIMARY KEY, " &
                "ProduitId INT NOT NULL, " &
                "Nom NVARCHAR(100) NOT NULL, " &
                "QuantiteEquivalent DECIMAL(18,4) NOT NULL, " &
                "TypeUniteEquivalent NVARCHAR(20) NULL, " &
                "TypeQuantiteEquivalent NVARCHAR(20) NULL, " &
                "ModePrix NVARCHAR(20) NOT NULL, " &
                "Coefficient DECIMAL(18,4) NULL, " &
                "PrixVente DECIMAL(18,2) NOT NULL, " &
                "Actif BIT NOT NULL CONSTRAINT DF_TypesVenteProduit_Actif DEFAULT(1), " &
                "CreeLe DATETIME2 NOT NULL CONSTRAINT DF_TypesVenteProduit_CreeLe DEFAULT(GETDATE()), " &
                "ModifieLe DATETIME2 NULL, " &
                "ModifiePar NVARCHAR(80) NULL) " &
                "END " &
                "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_TypesVenteProduit_ProduitNomActif' AND object_id = OBJECT_ID('dbo.TypesVenteProduit')) " &
                "BEGIN " &
                "CREATE UNIQUE INDEX UX_TypesVenteProduit_ProduitNomActif ON dbo.TypesVenteProduit (ProduitId, Nom) WHERE Actif = 1 " &
                "END " &
                "IF COL_LENGTH('dbo.TypesVenteProduit', 'TypeUniteEquivalent') IS NULL " &
                "BEGIN ALTER TABLE dbo.TypesVenteProduit ADD TypeUniteEquivalent NVARCHAR(20) NULL END " &
                "IF COL_LENGTH('dbo.TypesVenteProduit', 'TypeQuantiteEquivalent') IS NULL " &
                "BEGIN ALTER TABLE dbo.TypesVenteProduit ADD TypeQuantiteEquivalent NVARCHAR(20) NULL END"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub

        Public Function ListerParProduit(produitId As Integer, actifSeulement As Boolean) As List(Of TypeVenteProduitDTO)
            Dim sql As String =
                "SELECT TypeVenteProduitId, ProduitId, Nom, QuantiteEquivalent, ISNULL(TypeUniteEquivalent, 'SECONDAIRE') AS TypeUniteEquivalent, ISNULL(TypeQuantiteEquivalent, ISNULL(TypeUniteEquivalent, 'SECONDAIRE')) AS TypeQuantiteEquivalent, ModePrix, Coefficient, PrixVente, Actif, CreeLe, ModifieLe, ModifiePar " &
                "FROM TypesVenteProduit WHERE ProduitId = @ProduitId " &
                If(actifSeulement, "AND Actif = 1 ", String.Empty) &
                "ORDER BY Actif DESC, Nom"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@ProduitId", produitId)
            }
            Dim dt As DataTable = _dal.ExecuterTable(sql, CommandType.Text, p)
            Dim liste As New List(Of TypeVenteProduitDTO)()
            For Each row As DataRow In dt.Rows
                liste.Add(Map(row))
            Next
            Return liste
        End Function

        Public Function Ajouter(dto As TypeVenteProduitDTO) As Integer
            Dim sql As String =
                "INSERT INTO TypesVenteProduit (ProduitId, Nom, QuantiteEquivalent, TypeUniteEquivalent, TypeQuantiteEquivalent, ModePrix, Coefficient, PrixVente, Actif, ModifiePar) " &
                "VALUES (@ProduitId, @Nom, @QuantiteEquivalent, @TypeUniteEquivalent, @TypeQuantiteEquivalent, @ModePrix, @Coefficient, @PrixVente, @Actif, @ModifiePar); " &
                "SELECT CAST(SCOPE_IDENTITY() AS INT);"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@ProduitId", dto.ProduitId),
                New SqlParameter("@Nom", dto.Nom.Trim()),
                New SqlParameter("@QuantiteEquivalent", dto.QuantiteEquivalent),
                New SqlParameter("@TypeUniteEquivalent", NormaliserTypeUnite(ObtenirTypeQuantite(dto))),
                New SqlParameter("@TypeQuantiteEquivalent", NormaliserTypeUnite(ObtenirTypeQuantite(dto))),
                New SqlParameter("@ModePrix", dto.ModePrix.Trim().ToUpperInvariant()),
                New SqlParameter("@Coefficient", If(dto.Coefficient.HasValue, CType(dto.Coefficient.Value, Object), DBNull.Value)),
                New SqlParameter("@PrixVente", dto.PrixVente),
                New SqlParameter("@Actif", dto.Actif),
                New SqlParameter("@ModifiePar", ObtenirNomUtilisateur())
            }
            Return Convert.ToInt32(_dal.ExecuterScalaire(sql, CommandType.Text, p))
        End Function

        Public Function MettreAJour(dto As TypeVenteProduitDTO) As Integer
            Dim sql As String =
                "UPDATE TypesVenteProduit SET Nom=@Nom, QuantiteEquivalent=@QuantiteEquivalent, TypeUniteEquivalent=@TypeUniteEquivalent, TypeQuantiteEquivalent=@TypeQuantiteEquivalent, ModePrix=@ModePrix, Coefficient=@Coefficient, " &
                "PrixVente=@PrixVente, Actif=@Actif, ModifieLe=GETDATE(), ModifiePar=@ModifiePar " &
                "WHERE TypeVenteProduitId=@TypeVenteProduitId"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@TypeVenteProduitId", dto.TypeVenteProduitId),
                New SqlParameter("@Nom", dto.Nom.Trim()),
                New SqlParameter("@QuantiteEquivalent", dto.QuantiteEquivalent),
                New SqlParameter("@TypeUniteEquivalent", NormaliserTypeUnite(ObtenirTypeQuantite(dto))),
                New SqlParameter("@TypeQuantiteEquivalent", NormaliserTypeUnite(ObtenirTypeQuantite(dto))),
                New SqlParameter("@ModePrix", dto.ModePrix.Trim().ToUpperInvariant()),
                New SqlParameter("@Coefficient", If(dto.Coefficient.HasValue, CType(dto.Coefficient.Value, Object), DBNull.Value)),
                New SqlParameter("@PrixVente", dto.PrixVente),
                New SqlParameter("@Actif", dto.Actif),
                New SqlParameter("@ModifiePar", ObtenirNomUtilisateur())
            }
            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function

        Public Function ChangerEtat(typeVenteProduitId As Integer, actif As Boolean) As Integer
            Dim sql As String =
                "UPDATE TypesVenteProduit SET Actif=@Actif, ModifieLe=GETDATE(), ModifiePar=@ModifiePar WHERE TypeVenteProduitId=@TypeVenteProduitId"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@TypeVenteProduitId", typeVenteProduitId),
                New SqlParameter("@Actif", actif),
                New SqlParameter("@ModifiePar", ObtenirNomUtilisateur())
            }
            Return _dal.ExecuterNonRequete(sql, CommandType.Text, p)
        End Function

        Private Function Map(row As DataRow) As TypeVenteProduitDTO
            Dim dto As New TypeVenteProduitDTO With {
                .TypeVenteProduitId = Convert.ToInt32(row("TypeVenteProduitId")),
                .ProduitId = Convert.ToInt32(row("ProduitId")),
                .Nom = Convert.ToString(row("Nom")),
                .QuantiteEquivalent = Convert.ToDecimal(row("QuantiteEquivalent")),
                .TypeUniteEquivalent = NormaliserTypeUnite(Convert.ToString(row("TypeUniteEquivalent"))),
                .TypeQuantiteEquivalent = NormaliserTypeUnite(Convert.ToString(row("TypeQuantiteEquivalent"))),
                .ModePrix = Convert.ToString(row("ModePrix")),
                .PrixVente = Convert.ToDecimal(row("PrixVente")),
                .Actif = Convert.ToBoolean(row("Actif")),
                .ModifiePar = If(row.IsNull("ModifiePar"), String.Empty, Convert.ToString(row("ModifiePar")))
            }

            If row.IsNull("Coefficient") Then
                dto.Coefficient = Nothing
            Else
                dto.Coefficient = Convert.ToDecimal(row("Coefficient"))
            End If

            If Not row.IsNull("CreeLe") Then
                dto.CreeLe = Convert.ToDateTime(row("CreeLe"))
            End If

            If Not row.IsNull("ModifieLe") Then
                dto.ModifieLe = Convert.ToDateTime(row("ModifieLe"))
            End If

            Return dto
        End Function

        Private Shared Function NormaliserTypeUnite(typeUnite As String) As String
            Return StockUnitConversionService.NormaliserTypeQuantiteEquivalent(typeUnite)
        End Function

        Private Shared Function ObtenirTypeQuantite(dto As TypeVenteProduitDTO) As String
            If dto Is Nothing Then Return "SECONDAIRE"
            If Not String.IsNullOrWhiteSpace(dto.TypeQuantiteEquivalent) Then Return dto.TypeQuantiteEquivalent
            Return dto.TypeUniteEquivalent
        End Function
    End Class
End Namespace
