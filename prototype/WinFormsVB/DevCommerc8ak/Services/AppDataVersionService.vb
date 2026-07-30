Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient

Namespace DevCommerc8ak
    Public Class AppDataVersionService
        Private Shared _infrastructureAssuree As Boolean
        Private Shared ReadOnly _lockObj As New Object()

        Public Shared Sub Touch(ParamArray domaines As String())
            If domaines Is Nothing OrElse domaines.Length = 0 Then Return
            Try
                AssurerInfrastructure()
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Using cn As New SqlConnection(cs)
                    cn.Open()
                    For Each domaine As String In domaines
                        If String.IsNullOrWhiteSpace(domaine) Then Continue For
                        Using cmd As New SqlCommand(
                            "IF EXISTS (SELECT 1 FROM dbo.AppDataVersions WHERE Domaine=@Domaine) " &
                            "UPDATE dbo.AppDataVersions SET VersionCourante=VersionCourante+1, ModifieLe=SYSDATETIME(), ModifiePar=@ModifiePar, Poste=@Poste WHERE Domaine=@Domaine " &
                            "ELSE INSERT INTO dbo.AppDataVersions (Domaine, VersionCourante, ModifieLe, ModifiePar, Poste) VALUES (@Domaine, 1, SYSDATETIME(), @ModifiePar, @Poste)", cn)
                            cmd.CommandTimeout = 5
                            cmd.Parameters.AddWithValue("@Domaine", domaine.Trim().ToUpperInvariant())
                            cmd.Parameters.AddWithValue("@ModifiePar", If(String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur), CType(DBNull.Value, Object), SessionUtilisateur.NomUtilisateur.Trim()))
                            cmd.Parameters.AddWithValue("@Poste", Environment.MachineName)
                            cmd.ExecuteNonQuery()
                        End Using
                    Next
                End Using
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Warn("AppDataVersionService", "Touch", "Version multi-postes non mise à jour : " & ex.Message)
            End Try
        End Sub

        Public Shared Function LireVersions(domaines As IEnumerable(Of String)) As Dictionary(Of String, Long)
            AssurerInfrastructure()
            Dim resultat As New Dictionary(Of String, Long)(StringComparer.OrdinalIgnoreCase)
            If domaines Is Nothing Then Return resultat

            Dim liste As New List(Of String)()
            For Each domaine As String In domaines
                If Not String.IsNullOrWhiteSpace(domaine) Then liste.Add(domaine.Trim().ToUpperInvariant())
            Next
            If liste.Count = 0 Then Return resultat

            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Using cn As New SqlConnection(cs)
                cn.Open()
                For Each domaine As String In liste
                    Using cmd As New SqlCommand("SELECT VersionCourante FROM dbo.AppDataVersions WHERE Domaine=@Domaine", cn)
                        cmd.CommandTimeout = 5
                        cmd.Parameters.AddWithValue("@Domaine", domaine)
                        Dim value As Object = cmd.ExecuteScalar()
                        Dim version As Long = 0
                        If value IsNot Nothing AndAlso value IsNot DBNull.Value Then Long.TryParse(Convert.ToString(value), version)
                        resultat(domaine) = version
                    End Using
                Next
            End Using
            Return resultat
        End Function

        Private Shared Sub AssurerInfrastructure()
            SyncLock _lockObj
                If _infrastructureAssuree Then Return
                Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
                Using cn As New SqlConnection(cs)
                    cn.Open()
                    Using cmd As New SqlCommand(
                        "IF OBJECT_ID('dbo.AppDataVersions', 'U') IS NULL " &
                        "CREATE TABLE dbo.AppDataVersions (" &
                        "Domaine NVARCHAR(50) NOT NULL PRIMARY KEY, " &
                        "VersionCourante BIGINT NOT NULL CONSTRAINT DF_AppDataVersions_Version DEFAULT(0), " &
                        "ModifieLe DATETIME2 NOT NULL CONSTRAINT DF_AppDataVersions_ModifieLe DEFAULT(SYSDATETIME()), " &
                        "ModifiePar NVARCHAR(80) NULL, Poste NVARCHAR(100) NULL);", cn)
                        cmd.CommandTimeout = 5
                        cmd.ExecuteNonQuery()
                    End Using
                End Using
                _infrastructureAssuree = True
            End SyncLock
        End Sub
    End Class
End Namespace
