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

        Public Shared Function LireNomUtilisateur(cn As SqlConnection, tx As SqlTransaction, id As Integer) As String
            ' Le nom est lu pour cet ID historique, jamais remplace par celui
            ' de la session courante. L'ID reste present dans le snapshot.
            Using cmd As New SqlCommand("SELECT NomUtilisateur FROM dbo.Utilisateurs WHERE UtilisateurId=@id", cn, tx)
                cmd.Parameters.AddWithValue("@id", id)
                Dim valeur As Object = cmd.ExecuteScalar()
                Return If(valeur Is Nothing OrElse valeur Is DBNull.Value, "Compte indisponible (ID " & id.ToString() & ")", Convert.ToString(valeur))
            End Using
        End Function

        Public Shared Function ChargerIdentites(dal As DAL, avant As Object, apres As Object) As IDictionary(Of Integer, String)
            Dim ids As New HashSet(Of Integer)()
            For Each snapshot As Object In New Object() {avant, apres}
                For Each champ As KeyValuePair(Of String, String) In AuditDifferenceService.Valeurs(snapshot)
                    Dim id As Integer
                    If AuditPresentationService.EstAuteur(champ.Key) AndAlso Integer.TryParse(champ.Value, id) Then ids.Add(id)
                Next
            Next
            Dim noms As New Dictionary(Of Integer, String)()
            Using cn As SqlConnection = dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "AUDIT_CONSULTER", "SUPERADMIN_AUDIT")
                    For Each id As Integer In ids
                        noms.Add(id, LireNomUtilisateur(cn, tx, id))
                    Next
                    tx.Commit()
                End Using
            End Using
            Return noms
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

        Public Shared Sub Echec(dal As DAL, evenement As String, id As Integer, correlation As Guid, ex As Exception, Optional entite As String = "Facture", Optional reference As String = Nothing)
            ' Après rollback, l'échec a sa propre transaction et ne peut jamais
            ' être présenté comme un succès. Aucun texte d'exception SQL brut
            ' (qui pourrait contenir une donnée confidentielle) n'est stocké.
            Try
                Using cn As SqlConnection = dal.CreerConnexion()
                    cn.Open()
                    Using tx As SqlTransaction = cn.BeginTransaction()
                        If reference Is Nothing AndAlso entite = "Facture" AndAlso id > 0 Then
                            Using cmd As New SqlCommand("SELECT NumeroFacture FROM dbo.FacturesVente WHERE FactureVenteId=@id", cn, tx)
                                cmd.Parameters.AddWithValue("@id", id)
                                reference = Convert.ToString(cmd.ExecuteScalar())
                            End Using
                        End If
                        If reference Is Nothing AndAlso entite <> "Facture" AndAlso id > 0 Then reference = id.ToString(Globalization.CultureInfo.InvariantCulture)
                        Enregistrer(cn, tx, evenement, "OPERATIONS_REFUSEES", entite, id, If(reference, String.Empty), Nothing, Nothing,
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
