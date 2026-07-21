Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.SqlClient
Imports DevCommerc8ak.DevCommerc8ak.DTO

Namespace DevCommerc8ak
    Public Class AnalyseCaissePhysiqueRepository
        Private ReadOnly _dal As DAL

        Public Sub New(dal As DAL)
            _dal = dal
            AssurerInfrastructure()
        End Sub

        Public Sub AssurerInfrastructure()
            Dim sql As String =
                "IF OBJECT_ID('dbo.CloturesCaisse', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.CloturesCaisse (" &
                "ClotureCaisseId INT IDENTITY(1,1) PRIMARY KEY, DateCaisse DATE NOT NULL, UtilisateurId INT NULL, NomUtilisateur NVARCHAR(80) NULL, RoleSession NVARCHAR(80) NULL, " &
                "SoldeTheoriqueFC DECIMAL(18,2) NOT NULL DEFAULT(0), MontantPhysiqueFC DECIMAL(18,2) NULL, EcartFC DECIMAL(18,2) NULL, " &
                "SoldeTheoriqueUSD DECIMAL(18,2) NULL, MontantPhysiqueUSD DECIMAL(18,2) NULL, EcartUSD DECIMAL(18,2) NULL, " &
                "MotifEcart NVARCHAR(250) NULL, Observation NVARCHAR(500) NULL, Statut NVARCHAR(30) NOT NULL DEFAULT('A_VERIFIER'), " &
                "ResponsableValidationId INT NULL, ValidePar NVARCHAR(80) NULL, ValideLe DATETIME2 NULL, CreeLe DATETIME2 NOT NULL DEFAULT(GETDATE()), " &
                "ModifieLe DATETIME2 NULL, ModifiePar NVARCHAR(80) NULL, EstCloturee BIT NOT NULL DEFAULT(0)); " &
                "CREATE UNIQUE INDEX UX_CloturesCaisse_Date_Utilisateur ON dbo.CloturesCaisse(DateCaisse, UtilisateurId) WHERE UtilisateurId IS NOT NULL; " &
                "END; " &
                "IF OBJECT_ID('dbo.RegularisationsEcartCaisse', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.RegularisationsEcartCaisse (" &
                "RegularisationId INT IDENTITY(1,1) PRIMARY KEY, ClotureCaisseId INT NOT NULL, Montant DECIMAL(18,2) NOT NULL DEFAULT(0), " &
                "ModeRegularisation NVARCHAR(80) NULL, Reference NVARCHAR(100) NULL, Observation NVARCHAR(500) NULL, CreeLe DATETIME2 NOT NULL DEFAULT(GETDATE()), CreePar NVARCHAR(80) NULL); " &
                "END; " &
                "IF OBJECT_ID('dbo.HistoriqueStatutClotureCaisse', 'U') IS NULL " &
                "BEGIN " &
                "CREATE TABLE dbo.HistoriqueStatutClotureCaisse (" &
                "HistoriqueStatutId INT IDENTITY(1,1) PRIMARY KEY, ClotureCaisseId INT NOT NULL, AncienStatut NVARCHAR(30) NULL, NouveauStatut NVARCHAR(30) NOT NULL, " &
                "MontantEcartAvant DECIMAL(18,2) NULL, MontantRegularise DECIMAL(18,2) NULL, MontantRestant DECIMAL(18,2) NULL, Motif NVARCHAR(250) NULL, Observation NVARCHAR(500) NULL, " &
                "UtilisateurResponsableId INT NULL, NomUtilisateurResponsable NVARCHAR(80) NULL, RoleResponsable NVARCHAR(80) NULL, " &
                "ModifieParUtilisateurId INT NULL, ModifiePar NVARCHAR(80) NULL, RoleModificateur NVARCHAR(80) NULL, ModifieLe DATETIME2 NOT NULL DEFAULT(GETDATE()), Reference NVARCHAR(100) NULL); " &
                "END"
            _dal.ExecuterNonRequete(sql, CommandType.Text, Nothing)
        End Sub

        Public Function ListerClotures(filtre As AnalyseCaissePhysiqueFiltreDTO) As DataTable
            Dim sql As String =
                "SELECT c.ClotureCaisseId, c.DateCaisse, c.NomUtilisateur AS Utilisateur, c.RoleSession, c.SoldeTheoriqueFC, c.MontantPhysiqueFC, c.EcartFC, " &
                "CASE WHEN ISNULL(c.EcartFC,0)=0 AND c.MontantPhysiqueFC IS NOT NULL THEN 'Conforme' WHEN ISNULL(c.EcartFC,0)<0 THEN 'Manquant' WHEN ISNULL(c.EcartFC,0)>0 THEN 'Surplus' ELSE 'À vérifier' END AS Resultat, " &
                "c.MotifEcart, c.Observation, c.Statut, c.ValidePar, c.ValideLe, c.CreeLe, ISNULL(r.TotalRegularise,0) AS MontantRegularise, " &
                "CASE WHEN c.EcartFC IS NULL THEN 0 ELSE CASE WHEN ABS(c.EcartFC)-ISNULL(r.TotalRegularise,0) < 0 THEN 0 ELSE ABS(c.EcartFC)-ISNULL(r.TotalRegularise,0) END END AS ResteRegulariser " &
                "FROM dbo.CloturesCaisse c " &
                "OUTER APPLY (SELECT SUM(Montant) AS TotalRegularise FROM dbo.RegularisationsEcartCaisse r WHERE r.ClotureCaisseId=c.ClotureCaisseId) r " &
                ClauseWhere(filtre) &
                " ORDER BY c.DateCaisse DESC, ISNULL(c.ValideLe, c.CreeLe) DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, ParametresFiltre(filtre))
        End Function

        Public Function ObtenirKpiClotures(filtre As AnalyseCaissePhysiqueFiltreDTO) As DataTable
            Dim sql As String =
                "SELECT COUNT(1) AS NombreClotures, ISNULL(SUM(SoldeTheoriqueFC),0) AS TotalTheoriqueFC, ISNULL(SUM(ISNULL(MontantPhysiqueFC,0)),0) AS TotalPhysiqueFC, " &
                "ISNULL(SUM(CASE WHEN EcartFC < 0 THEN ABS(EcartFC) ELSE 0 END),0) AS TotalManquants, " &
                "ISNULL(SUM(CASE WHEN EcartFC > 0 THEN EcartFC ELSE 0 END),0) AS TotalSurplus, " &
                "SUM(CASE WHEN Statut='CONFORME' THEN 1 ELSE 0 END) AS NombreConformes, " &
                "CAST(CASE WHEN COUNT(1)=0 THEN 0 ELSE (SUM(CASE WHEN Statut='CONFORME' THEN 1 ELSE 0 END) * 100.0 / COUNT(1)) END AS DECIMAL(18,2)) AS TauxConformite " &
                "FROM dbo.CloturesCaisse c " & ClauseWhere(filtre)
            Return _dal.ExecuterTable(sql, CommandType.Text, ParametresFiltre(filtre))
        End Function

        Public Function ObtenirSyntheseParUtilisateur(filtre As AnalyseCaissePhysiqueFiltreDTO) As DataTable
            Dim sql As String =
                "SELECT ISNULL(c.NomUtilisateur,'SYSTEM') AS Utilisateur, ISNULL(c.RoleSession,'') AS RoleSession, COUNT(1) AS NombreClotures, " &
                "SUM(CASE WHEN c.Statut='CONFORME' THEN 1 ELSE 0 END) AS NombreConformes, " &
                "SUM(CASE WHEN c.EcartFC < 0 THEN 1 ELSE 0 END) AS NombreManquants, ISNULL(SUM(CASE WHEN c.EcartFC < 0 THEN ABS(c.EcartFC) ELSE 0 END),0) AS TotalManquants, " &
                "SUM(CASE WHEN c.EcartFC > 0 THEN 1 ELSE 0 END) AS NombreSurplus, ISNULL(SUM(CASE WHEN c.EcartFC > 0 THEN c.EcartFC ELSE 0 END),0) AS TotalSurplus, " &
                "ISNULL(SUM(CASE WHEN c.EcartFC > 0 THEN c.EcartFC ELSE 0 END),0) - ISNULL(SUM(CASE WHEN c.EcartFC < 0 THEN ABS(c.EcartFC) ELSE 0 END),0) AS EcartNet, " &
                "ISNULL(SUM(ISNULL(r.TotalRegularise,0)),0) AS MontantRegularise, " &
                "ISNULL(SUM(CASE WHEN ABS(ISNULL(c.EcartFC,0))-ISNULL(r.TotalRegularise,0) < 0 THEN 0 ELSE ABS(ISNULL(c.EcartFC,0))-ISNULL(r.TotalRegularise,0) END),0) AS ResteRegulariser, " &
                "CAST(CASE WHEN COUNT(1)=0 THEN 0 ELSE (SUM(CASE WHEN c.Statut='CONFORME' THEN 1 ELSE 0 END) * 100.0 / COUNT(1)) END AS DECIMAL(18,2)) AS TauxConformite, " &
                "MAX(CASE WHEN ISNULL(c.EcartFC,0)<>0 THEN c.DateCaisse ELSE NULL END) AS DernierIncident " &
                "FROM dbo.CloturesCaisse c OUTER APPLY (SELECT SUM(Montant) AS TotalRegularise FROM dbo.RegularisationsEcartCaisse r WHERE r.ClotureCaisseId=c.ClotureCaisseId) r " &
                ClauseWhere(filtre) & " GROUP BY ISNULL(c.NomUtilisateur,'SYSTEM'), ISNULL(c.RoleSession,'') ORDER BY TotalManquants DESC, TotalSurplus DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, ParametresFiltre(filtre))
        End Function

        Public Function ListerHistoriqueStatuts(filtre As AnalyseCaissePhysiqueFiltreDTO) As DataTable
            Dim sql As String =
                "SELECT h.HistoriqueStatutId, c.DateCaisse, c.NomUtilisateur AS Utilisateur, c.RoleSession, h.AncienStatut, h.NouveauStatut, " &
                "h.MontantEcartAvant, h.MontantRegularise, h.MontantRestant, h.Motif, h.Observation, h.ModifiePar, h.RoleModificateur, h.ModifieLe, h.Reference " &
                "FROM dbo.HistoriqueStatutClotureCaisse h INNER JOIN dbo.CloturesCaisse c ON c.ClotureCaisseId=h.ClotureCaisseId " &
                ClauseWhere(filtre).Replace("WHERE", "WHERE") &
                " ORDER BY h.ModifieLe DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, ParametresFiltre(filtre))
        End Function

        Public Function ObtenirEvolutionEcarts(filtre As AnalyseCaissePhysiqueFiltreDTO) As DataTable
            Dim sql As String =
                "SELECT c.DateCaisse, ISNULL(SUM(CASE WHEN c.EcartFC < 0 THEN ABS(c.EcartFC) ELSE 0 END),0) AS Manquants, ISNULL(SUM(CASE WHEN c.EcartFC > 0 THEN c.EcartFC ELSE 0 END),0) AS Surplus " &
                "FROM dbo.CloturesCaisse c " & ClauseWhere(filtre) & " GROUP BY c.DateCaisse ORDER BY c.DateCaisse"
            Return _dal.ExecuterTable(sql, CommandType.Text, ParametresFiltre(filtre))
        End Function

        Public Function ObtenirRepartitionStatuts(filtre As AnalyseCaissePhysiqueFiltreDTO) As DataTable
            Dim sql As String = "SELECT c.Statut, COUNT(1) AS Total FROM dbo.CloturesCaisse c " & ClauseWhere(filtre) & " GROUP BY c.Statut ORDER BY Total DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, ParametresFiltre(filtre))
        End Function

        Public Function ObtenirEcartsParUtilisateur(filtre As AnalyseCaissePhysiqueFiltreDTO) As DataTable
            Dim sql As String =
                "SELECT ISNULL(c.NomUtilisateur,'SYSTEM') AS Utilisateur, ISNULL(SUM(CASE WHEN c.EcartFC < 0 THEN ABS(c.EcartFC) ELSE 0 END),0) AS Manquants, ISNULL(SUM(CASE WHEN c.EcartFC > 0 THEN c.EcartFC ELSE 0 END),0) AS Surplus " &
                "FROM dbo.CloturesCaisse c " & ClauseWhere(filtre) & " GROUP BY ISNULL(c.NomUtilisateur,'SYSTEM') ORDER BY Manquants DESC, Surplus DESC"
            Return _dal.ExecuterTable(sql, CommandType.Text, ParametresFiltre(filtre))
        End Function

        Public Sub RegulariserCloture(dto As RegularisationCaissePhysiqueDTO, utilisateurId As Integer, utilisateur As String, role As String)
            If dto Is Nothing OrElse dto.ClotureCaisseId <= 0 Then Throw New ArgumentException("Clôture invalide.")
            If String.IsNullOrWhiteSpace(dto.NouveauStatut) Then Throw New ArgumentException("Statut requis.")

            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    Try
                        Dim ancienStatut As String = String.Empty
                        Dim ecart As Decimal = 0D
                        Dim nomResponsable As String = String.Empty
                        Dim roleResponsable As String = String.Empty
                        Using cmdRead As New SqlCommand("SELECT Statut, ISNULL(EcartFC,0), ISNULL(NomUtilisateur,''), ISNULL(RoleSession,'') FROM dbo.CloturesCaisse WITH (UPDLOCK, HOLDLOCK) WHERE ClotureCaisseId=@Id", cn, tx)
                            cmdRead.Parameters.AddWithValue("@Id", dto.ClotureCaisseId)
                            Using reader As SqlDataReader = cmdRead.ExecuteReader()
                                If Not reader.Read() Then Throw New InvalidOperationException("Clôture introuvable.")
                                ancienStatut = Convert.ToString(reader.GetValue(0))
                                ecart = Convert.ToDecimal(reader.GetValue(1))
                                nomResponsable = Convert.ToString(reader.GetValue(2))
                                roleResponsable = Convert.ToString(reader.GetValue(3))
                            End Using
                        End Using

                        If dto.MontantRegularise > 0D Then
                            Using cmdReg As New SqlCommand("INSERT INTO dbo.RegularisationsEcartCaisse (ClotureCaisseId, Montant, ModeRegularisation, Reference, Observation, CreePar) VALUES (@Id, @Montant, @Mode, @Reference, @Observation, @CreePar)", cn, tx)
                                cmdReg.Parameters.AddWithValue("@Id", dto.ClotureCaisseId)
                                cmdReg.Parameters.AddWithValue("@Montant", dto.MontantRegularise)
                                cmdReg.Parameters.AddWithValue("@Mode", ValeurNullable(dto.ModeRegularisation))
                                cmdReg.Parameters.AddWithValue("@Reference", ValeurNullable(dto.Reference))
                                cmdReg.Parameters.AddWithValue("@Observation", ValeurNullable(dto.Observation))
                                cmdReg.Parameters.AddWithValue("@CreePar", ValeurNullable(utilisateur))
                                cmdReg.ExecuteNonQuery()
                            End Using
                        End If

                        Dim totalRegularise As Decimal = 0D
                        Using cmdTotal As New SqlCommand("SELECT ISNULL(SUM(Montant),0) FROM dbo.RegularisationsEcartCaisse WHERE ClotureCaisseId=@Id", cn, tx)
                            cmdTotal.Parameters.AddWithValue("@Id", dto.ClotureCaisseId)
                            totalRegularise = Convert.ToDecimal(cmdTotal.ExecuteScalar())
                        End Using
                        Dim restant As Decimal = Math.Max(0D, Math.Abs(ecart) - totalRegularise)

                        Using cmdUpdate As New SqlCommand("UPDATE dbo.CloturesCaisse SET Statut=@Statut, MotifEcart=COALESCE(@Motif, MotifEcart), Observation=COALESCE(@Observation, Observation), ModifieLe=GETDATE(), ModifiePar=@ModifiePar WHERE ClotureCaisseId=@Id", cn, tx)
                            cmdUpdate.Parameters.AddWithValue("@Statut", dto.NouveauStatut.Trim().ToUpperInvariant())
                            cmdUpdate.Parameters.AddWithValue("@Motif", ValeurNullable(dto.Motif))
                            cmdUpdate.Parameters.AddWithValue("@Observation", ValeurNullable(dto.Observation))
                            cmdUpdate.Parameters.AddWithValue("@ModifiePar", ValeurNullable(utilisateur))
                            cmdUpdate.Parameters.AddWithValue("@Id", dto.ClotureCaisseId)
                            cmdUpdate.ExecuteNonQuery()
                        End Using

                        Using cmdHist As New SqlCommand("INSERT INTO dbo.HistoriqueStatutClotureCaisse (ClotureCaisseId, AncienStatut, NouveauStatut, MontantEcartAvant, MontantRegularise, MontantRestant, Motif, Observation, NomUtilisateurResponsable, RoleResponsable, ModifieParUtilisateurId, ModifiePar, RoleModificateur, Reference) VALUES (@Id, @Ancien, @Nouveau, @Ecart, @Reg, @Restant, @Motif, @Observation, @NomResp, @RoleResp, @ModId, @ModPar, @RoleMod, @Reference)", cn, tx)
                            cmdHist.Parameters.AddWithValue("@Id", dto.ClotureCaisseId)
                            cmdHist.Parameters.AddWithValue("@Ancien", ValeurNullable(ancienStatut))
                            cmdHist.Parameters.AddWithValue("@Nouveau", dto.NouveauStatut.Trim().ToUpperInvariant())
                            cmdHist.Parameters.AddWithValue("@Ecart", ecart)
                            cmdHist.Parameters.AddWithValue("@Reg", dto.MontantRegularise)
                            cmdHist.Parameters.AddWithValue("@Restant", restant)
                            cmdHist.Parameters.AddWithValue("@Motif", ValeurNullable(dto.Motif))
                            cmdHist.Parameters.AddWithValue("@Observation", ValeurNullable(dto.Observation))
                            cmdHist.Parameters.AddWithValue("@NomResp", ValeurNullable(nomResponsable))
                            cmdHist.Parameters.AddWithValue("@RoleResp", ValeurNullable(roleResponsable))
                            cmdHist.Parameters.AddWithValue("@ModId", If(utilisateurId > 0, CType(utilisateurId, Object), DBNull.Value))
                            cmdHist.Parameters.AddWithValue("@ModPar", ValeurNullable(utilisateur))
                            cmdHist.Parameters.AddWithValue("@RoleMod", ValeurNullable(role))
                            cmdHist.Parameters.AddWithValue("@Reference", ValeurNullable(dto.Reference))
                            cmdHist.ExecuteNonQuery()
                        End Using

                        tx.Commit()
                    Catch
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Sub

        Private Function ClauseWhere(filtre As AnalyseCaissePhysiqueFiltreDTO) As String
            Dim clauses As New List(Of String) From {"c.DateCaisse BETWEEN @DateDebut AND @DateFin"}
            If filtre IsNot Nothing Then
                If Not String.IsNullOrWhiteSpace(filtre.Utilisateur) Then clauses.Add("ISNULL(c.NomUtilisateur,'') = @Utilisateur")
                If Not String.IsNullOrWhiteSpace(filtre.RoleSession) Then clauses.Add("ISNULL(c.RoleSession,'') = @RoleSession")
                If Not String.IsNullOrWhiteSpace(filtre.Statut) AndAlso Not String.Equals(filtre.Statut, "Tous", StringComparison.OrdinalIgnoreCase) Then clauses.Add("c.Statut = @Statut")
            End If
            Return " WHERE " & String.Join(" AND ", clauses.ToArray())
        End Function

        Private Function ParametresFiltre(filtre As AnalyseCaissePhysiqueFiltreDTO) As List(Of SqlParameter)
            Dim debut As DateTime = DateTime.Now.Date
            Dim fin As DateTime = DateTime.Now.Date
            If filtre IsNot Nothing Then
                debut = filtre.DateDebut.Date
                fin = filtre.DateFin.Date
            End If
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@DateDebut", debut),
                New SqlParameter("@DateFin", fin)
            }
            If filtre IsNot Nothing Then
                If Not String.IsNullOrWhiteSpace(filtre.Utilisateur) Then p.Add(New SqlParameter("@Utilisateur", filtre.Utilisateur.Trim()))
                If Not String.IsNullOrWhiteSpace(filtre.RoleSession) Then p.Add(New SqlParameter("@RoleSession", filtre.RoleSession.Trim()))
                If Not String.IsNullOrWhiteSpace(filtre.Statut) AndAlso Not String.Equals(filtre.Statut, "Tous", StringComparison.OrdinalIgnoreCase) Then p.Add(New SqlParameter("@Statut", filtre.Statut.Trim().ToUpperInvariant()))
            End If
            Return p
        End Function

        Private Shared Function ValeurNullable(texte As String) As Object
            If String.IsNullOrWhiteSpace(texte) Then Return DBNull.Value
            Return texte.Trim()
        End Function
    End Class
End Namespace
