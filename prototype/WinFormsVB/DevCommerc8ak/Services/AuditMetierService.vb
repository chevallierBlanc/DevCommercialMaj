Option Strict On
Option Explicit On

Imports System
Imports System.Data.SqlClient
Imports System.Collections.Generic
Imports System.Web.Script.Serialization

Namespace DevCommerc8ak
    Public NotInheritable Class AuditMetierService
        Private Sub New()
        End Sub

        Public Shared Function ValiderMotif(motif As String) As String
            Return AuditMetierRegles.ValiderMotif(motif)
        End Function

        Public Shared Sub Enregistrer(cn As SqlConnection, tx As SqlTransaction, evenement As String, categorie As String,
                                      entite As String, id As Integer, reference As String, avant As Object, apres As Object,
                                      motif As String, correlation As Guid, Optional resultat As String = "SUCCES")
            If tx Is Nothing OrElse tx.Connection IsNot cn Then Throw New ArgumentException("Transaction audit obligatoire.")
            ' Seuls des snapshots métier explicitement construits sont fournis.
            ' Aucun objet d'authentification ni secret n'est sérialisé.
            Dim json As New JavaScriptSerializer() With {.MaxJsonLength = Integer.MaxValue}
            Dim sql As String = "INSERT dbo.JournalAudit([Action],Entite,EntiteId,Details,EffectuePar,EffectueLe,Provenance,Categorie,ReferenceDocument,UtilisateurNom,RoleActif,ModeActif,SessionId,CorrelationId,Poste,AnciennesValeurs,NouvellesValeurs,Motif,Resultat) " &
                "SELECT @action,@entite,@id,@details,u.UtilisateurId,SYSUTCDATETIME(),'AUDIT_METIER',@categorie,@reference,u.NomUtilisateur,r.NomRole,s.ModeActif,s.SessionId,@correlation,@poste,@avant,@apres,@motif,@resultat " &
                "FROM dbo.Utilisateurs u JOIN dbo.UtilisateurSessions s ON s.UtilisateurId=u.UtilisateurId JOIN dbo.Roles r ON r.RoleId=s.RoleIdActif " &
                "WHERE u.UtilisateurId=@user AND s.SessionId=@session AND s.Fin IS NULL"
            Using cmd As New SqlCommand(sql, cn, tx)
                cmd.Parameters.AddWithValue("@action", evenement)
                cmd.Parameters.AddWithValue("@entite", entite)
                cmd.Parameters.AddWithValue("@id", id.ToString(Globalization.CultureInfo.InvariantCulture))
                cmd.Parameters.AddWithValue("@details", evenement & " : " & If(reference, String.Empty))
                cmd.Parameters.AddWithValue("@categorie", categorie)
                cmd.Parameters.AddWithValue("@reference", If(reference, String.Empty))
                cmd.Parameters.AddWithValue("@correlation", correlation)
                cmd.Parameters.AddWithValue("@poste", Environment.MachineName)
                cmd.Parameters.AddWithValue("@avant", If(avant Is Nothing, CType(DBNull.Value, Object), json.Serialize(avant)))
                cmd.Parameters.AddWithValue("@apres", If(apres Is Nothing, CType(DBNull.Value, Object), json.Serialize(apres)))
                cmd.Parameters.AddWithValue("@motif", If(motif, String.Empty))
                cmd.Parameters.AddWithValue("@resultat", resultat)
                cmd.Parameters.AddWithValue("@user", SessionUtilisateur.UtilisateurId)
                cmd.Parameters.AddWithValue("@session", SessionUtilisateur.SessionId)
                If cmd.ExecuteNonQuery() <> 1 Then Throw New InvalidOperationException("Session audit invalide : opération annulée.")
            End Using
        End Sub

        Public Shared Sub Echec(dal As DAL, evenement As String, id As Integer, correlation As Guid, ex As Exception, Optional entite As String = "Facture")
            ' Après rollback, l'échec a sa propre transaction et ne peut jamais
            ' être présenté comme un succès. Aucun texte d'exception SQL brut
            ' (qui pourrait contenir une donnée confidentielle) n'est stocké.
            Try
                Using cn As SqlConnection = dal.CreerConnexion()
                    cn.Open()
                    Using tx As SqlTransaction = cn.BeginTransaction()
                        Enregistrer(cn, tx, evenement, "OPERATIONS_REFUSEES", entite, id, String.Empty, Nothing, Nothing,
                                    ex.GetType().Name, correlation, If(TypeOf ex Is UnauthorizedAccessException, "REFUS", "ECHEC"))
                        tx.Commit()
                    End Using
                End Using
            Catch
                Dim log As New ProductionLogService()
                log.Warn("AuditMetier", "Echec", "Trace de refus indisponible. Corrélation=" & correlation.ToString())
            End Try
        End Sub
    End Class
End Namespace
