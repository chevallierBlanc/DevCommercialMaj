Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.Globalization
Imports System.IO
Imports System.Linq

Namespace DevCommerc8ak
    Public Class SuperAdminService
        Private Function ObtenirDal() As DAL
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Return New DAL(cs)
        End Function

        Private Function ObtenirRepository() As SuperAdminRepository
            Return New SuperAdminRepository(ObtenirDal())
        End Function

        Public Sub AssurerInfrastructure()
            ObtenirRepository().AssurerInfrastructure()
        End Sub

        Public Function ListerRoles() As DataTable
            Return ObtenirRepository().ListerRoles()
        End Function

        Public Function ListerNomsRoles() As List(Of String)
            Dim resultat As New List(Of String)()
            Dim dt As DataTable = ListerRoles()
            For Each row As DataRow In dt.Rows
                If dt.Columns.Contains("EstActif") AndAlso Not row.IsNull("EstActif") AndAlso Not Convert.ToBoolean(row("EstActif")) Then
                    Continue For
                End If
                Dim nomRole As String = Convert.ToString(row("NomRole")).Trim()
                If nomRole <> String.Empty Then
                    resultat.Add(nomRole)
                End If
            Next

            Return resultat.OrderBy(Function(x) x).ToList()
        End Function

        Public Function ListerInterfaces() As DataTable
            Return ObtenirRepository().ListerInterfaces()
        End Function

        Public Function ListerInterfacesParRole(roleId As Integer) As DataTable
            Return ObtenirRepository().ListerInterfacesParRole(roleId)
        End Function

        Public Sub EnregistrerRole(roleId As Integer?, nomRole As String, estActif As Boolean, interfaceIds As IEnumerable(Of Integer))
            ObtenirRepository().EnregistrerRole(roleId, nomRole, estActif, interfaceIds)
            AuditActionService.Enregistrer("SuperAdmin", "Modification rôle", "Rôle " & nomRole.Trim().ToUpperInvariant() & " mis à jour.")
            AppEvents.OnRolePermissionsChanged()
        End Sub

        Public Function RoleExisteDeja(nomRole As String, roleIdExclu As Integer?) As Boolean
            Return ObtenirRepository().RoleExisteDeja(nomRole, roleIdExclu)
        End Function

        Public Function CompterUtilisateursParRole(roleId As Integer) As Integer
            Return ObtenirRepository().CompterUtilisateursParRole(roleId)
        End Function

        Public Function EstDernierRoleCritique(roleId As Integer) As Boolean
            Return ObtenirRepository().EstDernierRoleCritique(roleId)
        End Function

        Public Sub DesactiverRole(roleId As Integer, nomRole As String)
            ObtenirRepository().DesactiverRole(roleId)
            AuditActionService.Enregistrer("SuperAdmin", "Désactivation rôle", "Rôle " & nomRole.Trim().ToUpperInvariant() & " désactivé.")
            AppEvents.OnRolePermissionsChanged()
        End Sub

        Public Sub SupprimerRole(roleId As Integer, nomRole As String)
            ObtenirRepository().SupprimerRole(roleId)
            AuditActionService.Enregistrer("SuperAdmin", "Suppression rôle", "Rôle " & nomRole.Trim().ToUpperInvariant() & " supprimé.")
            AppEvents.OnRolePermissionsChanged()
        End Sub

        Public Function RoleUtilisePermissions(nomRole As String) As Boolean
            Return ObtenirRepository().RoleUtilisePermissions(nomRole)
        End Function

        Public Function RoleAutoriseInterface(nomRole As String, codeInterface As String) As Boolean
            Return ObtenirRepository().RoleAutoriseInterface(nomRole, codeInterface)
        End Function

        Public Function ListerProduitsStockInitial() As DataTable
            Dim repo As New ProduitRepository(ObtenirDal())
            Return repo.ListerTable()
        End Function

        Public Sub VerifierAccesStockInitialTechnique()
            If Not String.Equals(If(SessionUtilisateur.Role, String.Empty), "SUPERADMIN", StringComparison.OrdinalIgnoreCase) Then
                AuditActionService.Enregistrer("SuperAdmin", "Accès refusé stock initial", "Tentative d'accès au stock initial technique par " & If(SessionUtilisateur.NomUtilisateur, "SYSTEM"))
                Throw New UnauthorizedAccessException("Le stock initial technique est réservé au SUPERADMIN.")
            End If
        End Sub

        Public Function ListerCategories() As DataTable
            Dim sql As String = "SELECT CategorieId, ISNULL(NomCategorie, '') AS NomCategorie FROM dbo.CategoriesProduits ORDER BY NomCategorie"
            Return ObtenirDal().ExecuterTable(sql, CommandType.Text, Nothing)
        End Function

        Public Function AssurerCategorieProduit(nomCategorie As String) As Integer?
            Dim nomNormalise As String = NormaliserNomCategorie(nomCategorie)
            If nomNormalise = String.Empty Then
                Return Nothing
            End If

            Using cn As SqlConnection = ObtenirDal().CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction(IsolationLevel.Serializable)
                    Try
                        Dim categorieId As Integer? = Nothing
                        Using cmd As New SqlCommand("SELECT TOP 1 CategorieId FROM dbo.CategoriesProduits WITH (UPDLOCK, HOLDLOCK) WHERE UPPER(LTRIM(RTRIM(NomCategorie)))=@Nom", cn, tx)
                            cmd.Parameters.AddWithValue("@Nom", nomNormalise.ToUpperInvariant())
                            Dim v As Object = cmd.ExecuteScalar()
                            If v IsNot Nothing AndAlso v IsNot DBNull.Value Then
                                categorieId = Convert.ToInt32(v)
                            End If
                        End Using

                        If Not categorieId.HasValue Then
                            Using cmdInsert As New SqlCommand("INSERT INTO dbo.CategoriesProduits (NomCategorie) VALUES (@NomCategorie); SELECT CAST(SCOPE_IDENTITY() AS INT);", cn, tx)
                                cmdInsert.Parameters.AddWithValue("@NomCategorie", nomNormalise)
                                categorieId = Convert.ToInt32(cmdInsert.ExecuteScalar())
                            End Using
                            AuditActionService.Enregistrer("SuperAdmin", "Création catégorie", "Catégorie créée pendant stock initial : " & nomNormalise)
                        End If

                        tx.Commit()
                        Return categorieId
                    Catch
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Public Function CreerSessionStockInitialTechnique(modeOperation As String, observation As String) As Integer
            VerifierAccesStockInitialTechnique()
            Dim mode As String = If(String.IsNullOrWhiteSpace(modeOperation), "CORRECTION", modeOperation.Trim().ToUpperInvariant())
            Dim reference As String = "INIT-STOCK-" & Date.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) & "-" & Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()
            Using cn As SqlConnection = ObtenirDal().CreerConnexion()
                cn.Open()
                Using cmd As New SqlCommand("INSERT INTO dbo.StockInitialTechniqueSessions (ReferenceSession, ModeOperation, Observation, CreePar, Machine) VALUES (@ReferenceSession, @ModeOperation, @Observation, @CreePar, @Machine); SELECT CAST(SCOPE_IDENTITY() AS INT);", cn)
                    cmd.Parameters.AddWithValue("@ReferenceSession", reference)
                    cmd.Parameters.AddWithValue("@ModeOperation", mode)
                    cmd.Parameters.AddWithValue("@Observation", If(String.IsNullOrWhiteSpace(observation), CType(DBNull.Value, Object), observation.Trim()))
                    cmd.Parameters.AddWithValue("@CreePar", SessionUtilisateur.UtilisateurId)
                    cmd.Parameters.AddWithValue("@Machine", Environment.MachineName)
                    Return Convert.ToInt32(cmd.ExecuteScalar())
                End Using
            End Using
        End Function

        Public Sub EnregistrerLigneStockInitialTechnique(sessionId As Integer, produitId As Integer, ancienneQuantiteBase As Decimal, nouvelleQuantiteBase As Decimal, typeGestionStock As String, unitePrincipale As String, uniteSecondaire As String, contenuPrincipal As Decimal, contenuSecondaire As Decimal?, modeOperation As String, observation As String)
            If sessionId <= 0 OrElse produitId <= 0 Then
                Return
            End If

            Dim sql As String = "INSERT INTO dbo.StockInitialTechniqueLignes (StockInitialTechniqueSessionId, ProduitId, AncienneQuantiteBase, NouvelleQuantiteBase, DifferenceBase, TypeGestionStock, UnitePrincipale, UniteSecondaire, ContenuUnitePrincipale, ContenuUniteSecondaire, ModeOperation, Observation) " &
                                "VALUES (@SessionId, @ProduitId, @Ancienne, @Nouvelle, @Difference, @TypeGestionStock, @UnitePrincipale, @UniteSecondaire, @ContenuPrincipal, @ContenuSecondaire, @ModeOperation, @Observation)"
            Dim p As New List(Of SqlParameter) From {
                New SqlParameter("@SessionId", sessionId),
                New SqlParameter("@ProduitId", produitId),
                New SqlParameter("@Ancienne", ancienneQuantiteBase),
                New SqlParameter("@Nouvelle", nouvelleQuantiteBase),
                New SqlParameter("@Difference", nouvelleQuantiteBase - ancienneQuantiteBase),
                New SqlParameter("@TypeGestionStock", If(String.IsNullOrWhiteSpace(typeGestionStock), "UNITE", typeGestionStock.Trim().ToUpperInvariant())),
                New SqlParameter("@UnitePrincipale", If(String.IsNullOrWhiteSpace(unitePrincipale), CType(DBNull.Value, Object), unitePrincipale.Trim())),
                New SqlParameter("@UniteSecondaire", If(String.IsNullOrWhiteSpace(uniteSecondaire), CType(DBNull.Value, Object), uniteSecondaire.Trim())),
                New SqlParameter("@ContenuPrincipal", contenuPrincipal),
                New SqlParameter("@ContenuSecondaire", If(contenuSecondaire.HasValue, CType(contenuSecondaire.Value, Object), DBNull.Value)),
                New SqlParameter("@ModeOperation", If(String.IsNullOrWhiteSpace(modeOperation), "CORRECTION", modeOperation.Trim().ToUpperInvariant())),
                New SqlParameter("@Observation", If(String.IsNullOrWhiteSpace(observation), CType(DBNull.Value, Object), observation.Trim()))
            }
            ObtenirDal().ExecuterNonRequete(sql, CommandType.Text, p)
        End Sub

        Private Function NormaliserNomCategorie(nomCategorie As String) As String
            If String.IsNullOrWhiteSpace(nomCategorie) Then
                Return String.Empty
            End If
            Return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(nomCategorie.Trim().ToLowerInvariant())
        End Function

        Public Function ListerActionsUtilisateur(dateDebut As Date?, dateFin As Date?, utilisateur As String, role As String, moduleName As String, actionName As String, typeAction As String) As List(Of AuditLogEntryDTO)
            Try
                Dim dtAudit As DataTable = ObtenirRepository().ListerAuditActions(dateDebut, dateFin, utilisateur, role, moduleName, actionName, typeAction)
                If dtAudit IsNot Nothing AndAlso dtAudit.Rows.Count > 0 Then
                    Dim audits As New List(Of AuditLogEntryDTO)()
                    For Each row As DataRow In dtAudit.Rows
                        audits.Add(New AuditLogEntryDTO With {
                            .DateAction = If(row.IsNull("CreeLe"), Date.MinValue, Convert.ToDateTime(row("CreeLe"))),
                            .Utilisateur = If(row.IsNull("Utilisateur"), "SYSTEM", Convert.ToString(row("Utilisateur"))),
                            .Role = If(row.IsNull("Role"), "N/A", Convert.ToString(row("Role"))),
                            .Modul = If(row.IsNull("Module"), String.Empty, Convert.ToString(row("Module"))),
                            .Action = If(row.IsNull("Action"), String.Empty, Convert.ToString(row("Action"))),
                            .Description = If(row.IsNull("Description"), String.Empty, Convert.ToString(row("Description"))),
                            .Machine = If(row.IsNull("Machine"), String.Empty, Convert.ToString(row("Machine"))),
                            .Statut = If(row.IsNull("Statut"), String.Empty, Convert.ToString(row("Statut"))),
                            .Niveau = If(row.IsNull("Statut"), String.Empty, Convert.ToString(row("Statut")))
                        })
                    Next
                    Return audits
                End If
            Catch
            End Try

            Dim entries As New List(Of AuditLogEntryDTO)()
            Dim dossier As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CommercialPro", "Logs")
            If Not Directory.Exists(dossier) Then
                Return entries
            End If

            Dim rolesParUtilisateur As Dictionary(Of String, String) = ChargerRolesParUtilisateur()
            Dim fichiers As String() = Directory.GetFiles(dossier, "*.log", SearchOption.TopDirectoryOnly)
            Array.Sort(fichiers, StringComparer.OrdinalIgnoreCase)

            For Each fichier As String In fichiers
                For Each ligne As String In File.ReadAllLines(fichier)
                    Dim entry As AuditLogEntryDTO = ParserLigneLog(ligne, rolesParUtilisateur)
                    If entry Is Nothing Then
                        Continue For
                    End If

                    If dateDebut.HasValue AndAlso entry.DateAction.Date < dateDebut.Value.Date Then
                        Continue For
                    End If
                    If dateFin.HasValue AndAlso entry.DateAction.Date > dateFin.Value.Date Then
                        Continue For
                    End If
                    If Not String.IsNullOrWhiteSpace(utilisateur) AndAlso entry.Utilisateur.IndexOf(utilisateur.Trim(), StringComparison.OrdinalIgnoreCase) < 0 Then
                        Continue For
                    End If
                    If Not String.IsNullOrWhiteSpace(role) AndAlso Not String.Equals(entry.Role, role.Trim(), StringComparison.OrdinalIgnoreCase) Then
                        Continue For
                    End If
                    If Not String.IsNullOrWhiteSpace(moduleName) AndAlso entry.Modul.IndexOf(moduleName.Trim(), StringComparison.OrdinalIgnoreCase) < 0 Then
                        Continue For
                    End If
                    If Not String.IsNullOrWhiteSpace(actionName) AndAlso entry.Action.IndexOf(actionName.Trim(), StringComparison.OrdinalIgnoreCase) < 0 Then
                        Continue For
                    End If
                    If Not String.IsNullOrWhiteSpace(typeAction) AndAlso Not String.Equals(entry.Niveau, typeAction.Trim(), StringComparison.OrdinalIgnoreCase) Then
                        Continue For
                    End If

                    entries.Add(entry)
                Next
            Next

            Return entries.OrderByDescending(Function(x) x.DateAction).ToList()
        End Function

        Private Function ChargerRolesParUtilisateur() As Dictionary(Of String, String)
            Dim resultat As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Try
                Dim dt As DataTable = ObtenirRepository().ListerUtilisateursAvecRole()
                For Each row As DataRow In dt.Rows
                    Dim nomUtilisateur As String = Convert.ToString(row("NomUtilisateur")).Trim()
                    If nomUtilisateur <> String.Empty Then
                        resultat(nomUtilisateur) = Convert.ToString(row("NomRole")).Trim()
                    End If
                Next
            Catch
            End Try

            Return resultat
        End Function

        Private Function ParserLigneLog(ligne As String, rolesParUtilisateur As IDictionary(Of String, String)) As AuditLogEntryDTO
            If String.IsNullOrWhiteSpace(ligne) OrElse ligne.Length < 24 Then
                Return Nothing
            End If

            Dim dateAction As Date
            If Not Date.TryParseExact(ligne.Substring(0, 23), "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, dateAction) Then
                Return Nothing
            End If

            Dim niveau As String = ExtraireEntre(ligne, "[", "]")
            Dim utilisateur As String = ExtraireValeur(ligne, "User=")
            Dim machine As String = ExtraireValeur(ligne, "Host=")
            Dim moduleName As String = ExtraireValeur(ligne, "Module=")
            Dim actionName As String = ExtraireValeur(ligne, "Action=")
            Dim message As String = ExtraireValeur(ligne, "Message=")
            Dim role As String = "N/A"

            If utilisateur <> String.Empty AndAlso rolesParUtilisateur IsNot Nothing AndAlso rolesParUtilisateur.ContainsKey(utilisateur) Then
                role = rolesParUtilisateur(utilisateur)
            ElseIf String.Equals(utilisateur, "SYSTEM", StringComparison.OrdinalIgnoreCase) Then
                role = "SYSTEM"
            End If

            Dim statut As String = If(String.Equals(niveau, "ERROR", StringComparison.OrdinalIgnoreCase), "Erreur", "OK")
            Return New AuditLogEntryDTO With {
                .DateAction = dateAction,
                .Utilisateur = If(utilisateur = String.Empty, "SYSTEM", utilisateur),
                .Role = role,
                .Modul = moduleName,
                .Action = actionName,
                .Description = message,
                .Machine = machine,
                .Statut = statut,
                .Niveau = niveau
            }
        End Function

        Private Function ExtraireEntre(texte As String, debut As String, fin As String) As String
            Dim indexDebut As Integer = texte.IndexOf(debut, StringComparison.Ordinal)
            If indexDebut < 0 Then
                Return String.Empty
            End If

            indexDebut += debut.Length
            Dim indexFin As Integer = texte.IndexOf(fin, indexDebut, StringComparison.Ordinal)
            If indexFin <= indexDebut Then
                Return String.Empty
            End If

            Return texte.Substring(indexDebut, indexFin - indexDebut).Trim()
        End Function

        Private Function ExtraireValeur(texte As String, prefixe As String) As String
            Dim index As Integer = texte.IndexOf(prefixe, StringComparison.OrdinalIgnoreCase)
            If index < 0 Then
                Return String.Empty
            End If

            index += prefixe.Length
            Dim fin As Integer = texte.IndexOf(" | ", index, StringComparison.Ordinal)
            If fin < 0 Then
                fin = texte.Length
            End If

            Return texte.Substring(index, fin - index).Trim()
        End Function
    End Class
End Namespace
