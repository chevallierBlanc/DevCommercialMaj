Option Strict On
Option Explicit On

Imports System
Imports System.Security.Cryptography
Imports System.Collections.Generic
Namespace DevCommerc8ak
    Public Class UtilisateurService
        Private ReadOnly _utilisateurRepo As UtilisateurRepository
        Private ReadOnly _roleRepo As RoleRepository
        Private ReadOnly _sessionRepo As SessionRepository

        Public Sub New(utilisateurRepo As UtilisateurRepository, roleRepo As RoleRepository, sessionRepo As SessionRepository)
            _utilisateurRepo = utilisateurRepo
            _roleRepo = roleRepo
            _sessionRepo = sessionRepo
        End Sub

        ' Verifie les identifiants et initialise la session.
        Public Function VerifierConnexion(nomUtilisateur As String, motDePasse As String) As Boolean
            Dim user As Utilisateur = VerifierIdentifiants(nomUtilisateur, motDePasse)
            If user Is Nothing Then Return False

            Dim roles As List(Of RoleSessionInfo) = _utilisateurRepo.ListerRolesActifs(user.UtilisateurId)
            If roles.Count = 0 Then Return False
            Dim roleActif As RoleSessionInfo = roles(0)

            SessionUtilisateur.UtilisateurId = user.UtilisateurId
            SessionUtilisateur.NomUtilisateur = user.NomUtilisateur
            SessionUtilisateur.Role = roleActif.NomRole
            SessionUtilisateur.RoleIdActif = roleActif.RoleId
            SessionUtilisateur.NomRoleActif = roleActif.NomRole
            SessionUtilisateur.DateConnexion = Date.Now
            SessionUtilisateur.Poste = Environment.MachineName
            SessionUtilisateur.SessionId = _sessionRepo.DemarrerSession(user.UtilisateurId, roleActif.RoleId, roleActif.NomRole)
            Return True
        End Function

        Public Function VerifierIdentifiants(nomUtilisateur As String, motDePasse As String) As Utilisateur
            Dim user As Utilisateur = _utilisateurRepo.ObtenirParNom(nomUtilisateur)
            If user Is Nothing Then Return Nothing
            If Not user.EstActif Then Return Nothing

            Dim ok As Boolean = VerifierMotDePasse(motDePasse, user.MotDePasseSel, user.MotDePasseHash)
            If Not ok Then Return Nothing
            Return user
        End Function

        Public Function ListerRolesActifs(utilisateurId As Integer) As List(Of RoleSessionInfo)
            Return _utilisateurRepo.ListerRolesActifs(utilisateurId)
        End Function

        Public Sub DemarrerSession(user As Utilisateur, roleSession As RoleSessionInfo)
            If user Is Nothing Then Throw New ArgumentNullException("user")
            If roleSession Is Nothing OrElse roleSession.RoleId <= 0 Then Throw New ArgumentException("Rôle session invalide.")

            If _sessionRepo.UtilisateurDejaConnecte(user.UtilisateurId) Then
                Throw New InvalidOperationException("Cet utilisateur possède déjà une session active. Fermez l'autre session ou demandez au SUPERADMIN de la libérer.")
            End If

            SessionUtilisateur.UtilisateurId = user.UtilisateurId
            SessionUtilisateur.NomUtilisateur = user.NomUtilisateur
            SessionUtilisateur.Role = roleSession.NomRole
            SessionUtilisateur.RoleIdActif = roleSession.RoleId
            SessionUtilisateur.NomRoleActif = roleSession.NomRole
            SessionUtilisateur.DateConnexion = Date.Now
            SessionUtilisateur.Poste = Environment.MachineName
            SessionUtilisateur.SessionId = _sessionRepo.DemarrerSession(user.UtilisateurId, roleSession.RoleId, roleSession.NomRole)
        End Sub

        ' Les comptes initiaux sont créés hors application, via les scripts de déploiement.
        Public Sub CreerUtilisateur(nomUtilisateur As String, motDePasse As String, nomRole As String)
            Dim sel As Byte() = GenererSel()
            Dim hash As Byte() = HashMotDePasse(motDePasse, sel)

            Dim roleId As Integer = _roleRepo.ObtenirIdParNom(nomRole)
            Dim u As New Utilisateur With {
                .NomUtilisateur = nomUtilisateur,
                .MotDePasseHash = hash,
                .MotDePasseSel = sel,
                .EstActif = True
            }
            Dim utilisateurId As Integer = _utilisateurRepo.Ajouter(u, roleId)
            _utilisateurRepo.MettreAJourRolesUtilisateur(utilisateurId, New List(Of Integer) From {roleId}, roleId)
            AuditActionService.Enregistrer("Utilisateurs", "Création utilisateur", "Utilisateur " & nomUtilisateur.Trim() & " créé avec le rôle " & nomRole.Trim().ToUpperInvariant() & ".")
        End Sub

        ' Liste des utilisateurs.
        Public Function Lister() As List(Of UtilisateurDTO)
            Return _utilisateurRepo.Lister()
        End Function

        ' Met a jour le compte utilisateur.
        Public Sub MettreAJourUtilisateur(utilisateurId As Integer, nomUtilisateur As String, nomRole As String, estActif As Boolean, Optional nouveauMotDePasse As String = Nothing)
            If utilisateurId <= 0 Then Throw New ArgumentException("Utilisateur invalide.")
            If String.IsNullOrWhiteSpace(nomUtilisateur) Then Throw New ArgumentException("Nom utilisateur obligatoire.")
            If String.IsNullOrWhiteSpace(nomRole) Then Throw New ArgumentException("Role obligatoire.")
            VerifierProtectionSuperAdmin(utilisateurId, New List(Of String) From {nomRole}, estActif)

            Dim roleId As Integer = _roleRepo.ObtenirIdParNom(nomRole)
            Dim hash As Byte() = Nothing
            Dim sel As Byte() = Nothing

            If Not String.IsNullOrWhiteSpace(nouveauMotDePasse) Then
                sel = GenererSel()
                hash = HashMotDePasse(nouveauMotDePasse, sel)
            End If

            _utilisateurRepo.MettreAJour(utilisateurId, nomUtilisateur.Trim(), estActif, roleId, hash, sel)
            AuditActionService.Enregistrer("Utilisateurs", "Modification utilisateur", "Utilisateur " & nomUtilisateur.Trim() & " mis à jour avec le rôle " & nomRole.Trim().ToUpperInvariant() & ".")
        End Sub

        Public Sub MettreAJourUtilisateurRoles(utilisateurId As Integer, nomUtilisateur As String, roles As IEnumerable(Of String), rolePrincipal As String, estActif As Boolean, Optional nouveauMotDePasse As String = Nothing)
            If roles Is Nothing Then Throw New ArgumentException("Au moins un rôle est obligatoire.")
            Dim nomsRoles As New List(Of String)(roles)
            If nomsRoles.Count = 0 Then Throw New ArgumentException("Au moins un rôle est obligatoire.")
            If String.IsNullOrWhiteSpace(rolePrincipal) Then Throw New ArgumentException("Rôle principal obligatoire.")
            VerifierProtectionSuperAdmin(utilisateurId, nomsRoles, estActif)

            Dim roleIds As New List(Of Integer)()
            For Each nomRole As String In nomsRoles
                If Not String.IsNullOrWhiteSpace(nomRole) Then
                    roleIds.Add(_roleRepo.ObtenirIdParNom(nomRole.Trim()))
                End If
            Next
            Dim rolePrincipalId As Integer = _roleRepo.ObtenirIdParNom(rolePrincipal.Trim())

            MettreAJourUtilisateur(utilisateurId, nomUtilisateur, rolePrincipal, estActif, nouveauMotDePasse)
            _utilisateurRepo.MettreAJourRolesUtilisateur(utilisateurId, roleIds, rolePrincipalId)
            AuditActionService.Enregistrer("Utilisateurs", "Modification rôles utilisateur", "Rôles autorisés mis à jour pour " & nomUtilisateur.Trim() & ". Rôle principal : " & rolePrincipal.Trim().ToUpperInvariant() & ".")
        End Sub

        ' Met a jour mot de passe.
        Public Sub ReinitialiserMotDePasse(utilisateurId As Integer, nouveauMotDePasse As String)
            VerifierProtectionSuperAdmin(utilisateurId, Nothing, True)
            Dim sel As Byte() = GenererSel()
            Dim hash As Byte() = HashMotDePasse(nouveauMotDePasse, sel)
            _utilisateurRepo.MettreAJourMotDePasse(utilisateurId, hash, sel)
        End Sub

        Private Sub VerifierProtectionSuperAdmin(utilisateurId As Integer, rolesDemandes As IEnumerable(Of String), estActif As Boolean)
            If utilisateurId <= 0 Then Return
            If Not _utilisateurRepo.EstDansRole(utilisateurId, "SUPERADMIN") Then Return

            Dim sessionSuperAdmin As Boolean = String.Equals(SessionUtilisateur.Role, "SUPERADMIN", StringComparison.OrdinalIgnoreCase)
            If Not sessionSuperAdmin Then
                Throw New InvalidOperationException("Le compte SUPERADMIN ne peut être modifié que par un SUPERADMIN.")
            End If

            If Not estActif Then
                Throw New InvalidOperationException("Le compte SUPERADMIN ne peut pas être désactivé.")
            End If

            If rolesDemandes IsNot Nothing Then
                Dim conserveSuperAdmin As Boolean = False
                For Each role As String In rolesDemandes
                    If String.Equals(role, "SUPERADMIN", StringComparison.OrdinalIgnoreCase) Then
                        conserveSuperAdmin = True
                        Exit For
                    End If
                Next
                If Not conserveSuperAdmin Then
                    Throw New InvalidOperationException("Le rôle SUPERADMIN ne peut pas être retiré du compte système.")
                End If
            End If
        End Sub

        Private Function GenererSel() As Byte()
            Dim sel(15) As Byte
            Using rng As New RNGCryptoServiceProvider()
                rng.GetBytes(sel)
            End Using
            Return sel
        End Function

        Private Function HashMotDePasse(motDePasse As String, sel As Byte()) As Byte()
            Using derive As New Rfc2898DeriveBytes(motDePasse, sel, 10000)
                Return derive.GetBytes(32)
            End Using
        End Function

        Private Function VerifierMotDePasse(motDePasse As String, sel As Byte(), hashAttendu As Byte()) As Boolean
            If sel Is Nothing OrElse hashAttendu Is Nothing Then Return False
            Dim hash As Byte() = HashMotDePasse(motDePasse, sel)
            Dim difference As Integer = hash.Length Xor hashAttendu.Length
            Dim longueurMax As Integer = Math.Max(hash.Length, hashAttendu.Length)
            For i As Integer = 0 To longueurMax - 1
                Dim octetCalcule As Byte = If(i < hash.Length, hash(i), CByte(0))
                Dim octetAttendu As Byte = If(i < hashAttendu.Length, hashAttendu(i), CByte(0))
                difference = difference Or (CInt(octetCalcule) Xor CInt(octetAttendu))
            Next
            Return difference = 0
        End Function
    End Class
End Namespace
