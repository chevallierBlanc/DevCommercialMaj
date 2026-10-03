Option Strict On
Option Explicit On

Imports System
Imports System.Security.Cryptography
Imports System.Collections.Generic
Namespace DevCommerc8ak
    Public Class UtilisateurService
        Private Const SeuilTentativesConnexion As Integer = 3
        Private Const DureeResetMinutes As Integer = 30

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
            Dim resultat As AuthentificationResultat = AuthentifierCompte(nomUtilisateur, motDePasse)
            If resultat Is Nothing OrElse resultat.Statut <> AuthentificationStatut.Succes Then Return False
            Dim user As Utilisateur = resultat.Utilisateur

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
            Dim resultat As AuthentificationResultat = AuthentifierCompte(nomUtilisateur, motDePasse)
            If resultat Is Nothing OrElse resultat.Statut <> AuthentificationStatut.Succes Then Return Nothing
            Return resultat.Utilisateur
        End Function

        Public Function AuthentifierCompte(nomUtilisateur As String, secret As String) As AuthentificationResultat
            Dim user As Utilisateur = _utilisateurRepo.ObtenirParNom(nomUtilisateur)
            If user Is Nothing Then
                AuditActionService.Enregistrer("Sécurité", "LOGIN_FAILED", "Tentative de connexion pour un utilisateur inconnu : " & If(nomUtilisateur, String.Empty).Trim(), "REFUS")
                Return Echec(AuthentificationStatut.IdentifiantsInvalides, "Mot de passe incorrect. Il vous reste 2 tentatives.", 2)
            End If

            If Not user.EstActif Then
                AuditActionService.Enregistrer("Sécurité", "LOGIN_FAILED", "Compte désactivé : " & user.NomUtilisateur, "REFUS")
                Return Echec(AuthentificationStatut.CompteDesactive, "Ce compte est désactivé. Contactez un administrateur.", 0)
            End If

            ' Le code temporaire est vérifié avant le verrouillage afin qu'un reset
            ' demandé par l'administrateur permette de récupérer un compte bloqué.
            If CodeTemporaireValide(user, secret) Then
                AuditActionService.Enregistrer("Sécurité", "TEMP_PASSWORD_ACCEPTED", "Code temporaire accepté pour " & user.NomUtilisateur & ".")
                Return New AuthentificationResultat With {.Statut = AuthentificationStatut.CodeTemporaireValide, .Utilisateur = user, .Message = "Votre mot de passe temporaire doit être remplacé."}
            End If

            If CodeTemporaireExpire(user, secret) Then
                AuditActionService.Enregistrer("Sécurité", "PASSWORD_RESET_EXPIRED", "Code temporaire expiré pour " & user.NomUtilisateur & ".", "REFUS")
                Return Echec(AuthentificationStatut.CodeTemporaireExpire, "Le code temporaire a expiré. Demandez un nouveau reset mot de passe.", 0)
            End If

            If user.EstVerrouille Then
                AuditActionService.Enregistrer("Sécurité", "LOGIN_FAILED", "Compte verrouillé : " & user.NomUtilisateur, "REFUS")
                Return Echec(AuthentificationStatut.CompteVerrouille, "Votre compte est verrouillé. Veuillez contacter un administrateur.", 0)
            End If

            If VerifierMotDePasse(secret, user.MotDePasseSel, user.MotDePasseHash) Then
                _utilisateurRepo.ReinitialiserEchecsEtVerrouillage(user.UtilisateurId)
                AuditActionService.Enregistrer("Sécurité", "LOGIN_SUCCESS", "Connexion réussie pour " & user.NomUtilisateur & ".")
                user.NombreTentativesEchouees = 0
                Return New AuthentificationResultat With {.Statut = AuthentificationStatut.Succes, .Utilisateur = user, .Message = "Connexion autorisée."}
            End If

            Dim totalEchecs As Integer = _utilisateurRepo.EnregistrerEchecConnexion(user.UtilisateurId, SeuilTentativesConnexion)
            Dim restantes As Integer = Math.Max(0, SeuilTentativesConnexion - totalEchecs)
            If totalEchecs >= SeuilTentativesConnexion Then
                AuditActionService.Enregistrer("Sécurité", "ACCOUNT_LOCKED", "Compte verrouillé après 3 tentatives : " & user.NomUtilisateur, "ALERTE")
                Return Echec(AuthentificationStatut.CompteVerrouille, "Votre compte a été verrouillé après 3 tentatives infructueuses." & Environment.NewLine & "Veuillez contacter un administrateur.", 0)
            End If

            AuditActionService.Enregistrer("Sécurité", "LOGIN_FAILED", "Mot de passe incorrect pour " & user.NomUtilisateur & ". Tentatives restantes : " & restantes.ToString(), "REFUS")
            Return Echec(AuthentificationStatut.IdentifiantsInvalides, "Mot de passe incorrect. Il vous reste " & restantes.ToString() & " tentative" & If(restantes > 1, "s.", "."), restantes)
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

        Public Function DemarrerSessionApresAuthentification(user As Utilisateur, roleSession As RoleSessionInfo) As AuthentificationResultat
            Try
                DemarrerSession(user, roleSession)
                Return New AuthentificationResultat With {.Statut = AuthentificationStatut.Succes, .Utilisateur = user, .Message = "Connexion autorisée."}
            Catch ex As InvalidOperationException
                Return Echec(AuthentificationStatut.SessionActive, ex.Message, 0)
            End Try
        End Function

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

        Public Function CreerCodeResetMotDePasse(utilisateurId As Integer) As ResetMotDePasseTemporaireDTO
            If utilisateurId <= 0 Then Throw New ArgumentException("Utilisateur invalide.")
            VerifierDroitAdministrationComptes()
            VerifierProtectionSuperAdmin(utilisateurId, Nothing, True)

            Dim code As String = GenererCodeTemporaire4Chiffres()
            Dim sel As Byte() = GenererSel()
            Dim hash As Byte() = HashMotDePasse(code, sel)
            Dim expiration As Date = Date.Now.AddMinutes(DureeResetMinutes)
            _utilisateurRepo.EnregistrerResetTemporaire(utilisateurId, hash, sel, expiration)
            Dim nom As String = _utilisateurRepo.NomUtilisateurParId(utilisateurId)
            AuditActionService.Enregistrer("Sécurité", "PASSWORD_RESET_CREATED", "Code temporaire créé pour " & nom & ". Expiration : " & expiration.ToString("dd/MM/yyyy HH:mm:ss") & ".")
            Return New ResetMotDePasseTemporaireDTO With {
                .UtilisateurId = utilisateurId,
                .NomUtilisateur = nom,
                .CodeTemporaire = code,
                .Expiration = expiration
            }
        End Function

        Public Function ObtenirEtatResetMotDePasse(utilisateurId As Integer) As EtatResetMotDePasse
            Return _utilisateurRepo.ObtenirEtatReset(utilisateurId)
        End Function

        Public Sub ChangerMotDePasseApresReset(utilisateurId As Integer, nouveauMotDePasse As String)
            If utilisateurId <= 0 Then Throw New ArgumentException("Utilisateur invalide.")
            If String.IsNullOrWhiteSpace(nouveauMotDePasse) OrElse nouveauMotDePasse.Length < 4 Then
                Throw New ArgumentException("Le nouveau mot de passe doit contenir au moins 4 caractères.")
            End If

            Dim sel As Byte() = GenererSel()
            Dim hash As Byte() = HashMotDePasse(nouveauMotDePasse, sel)
            _utilisateurRepo.MarquerResetUtiliseEtChangerMotDePasse(utilisateurId, hash, sel)
            AuditActionService.Enregistrer("Sécurité", "PASSWORD_CHANGED_AFTER_RESET", "Mot de passe changé après reset pour UtilisateurId=" & utilisateurId.ToString() & ".")
        End Sub

        Public Sub DeverrouillerUtilisateur(utilisateurId As Integer)
            If utilisateurId <= 0 Then Throw New ArgumentException("Utilisateur invalide.")
            VerifierDroitAdministrationComptes()
            _utilisateurRepo.ReinitialiserEchecsEtVerrouillage(utilisateurId)
            AuditActionService.Enregistrer("Sécurité", "ACCOUNT_UNLOCKED", "Compte déverrouillé. UtilisateurId=" & utilisateurId.ToString() & ".")
        End Sub

        Public Sub SupprimerUtilisateurLogiquement(utilisateurId As Integer, nomUtilisateur As String)
            If utilisateurId <= 0 Then Throw New ArgumentException("Utilisateur invalide.")
            VerifierDroitAdministrationComptes()
            DesactiverUtilisateur(utilisateurId, nomUtilisateur)
            AuditActionService.Enregistrer("Sécurité", "USER_DISABLED", "Suppression logique du compte " & If(nomUtilisateur, String.Empty).Trim() & ".")
        End Sub

        Public Sub DesactiverUtilisateur(utilisateurId As Integer, nomUtilisateur As String)
            If utilisateurId <= 0 Then Throw New ArgumentException("Utilisateur invalide.")
            If utilisateurId = SessionUtilisateur.UtilisateurId Then
                Throw New InvalidOperationException("Vous ne pouvez pas désactiver votre propre compte connecté.")
            End If

            VerifierDroitAdministrationComptes()
            VerifierProtectionSuperAdmin(utilisateurId, Nothing, False)
            _utilisateurRepo.MettreAJourActif(utilisateurId, False)
            AuditActionService.Enregistrer("Utilisateurs", "UTILISATEUR_DESACTIVE", "Utilisateur " & If(nomUtilisateur, String.Empty).Trim() & " désactivé.")
        End Sub

        Private Sub VerifierProtectionSuperAdmin(utilisateurId As Integer, rolesDemandes As IEnumerable(Of String), estActif As Boolean)
            If utilisateurId <= 0 Then Return
            If Not _utilisateurRepo.EstDansRole(utilisateurId, "SUPERADMIN") Then Return

            Dim sessionSuperAdmin As Boolean = String.Equals(SessionUtilisateur.Role, "SUPERADMIN", StringComparison.OrdinalIgnoreCase)
            If Not sessionSuperAdmin Then
                Throw New InvalidOperationException("Le compte SUPERADMIN ne peut être modifié que par un SUPERADMIN.")
            End If

            If Not estActif AndAlso _utilisateurRepo.CompterSuperAdminActifs(utilisateurId) = 0 Then
                Throw New InvalidOperationException("Impossible de désactiver ou supprimer le dernier SUPERADMIN actif.")
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

        Private Sub VerifierDroitAdministrationComptes()
            Dim role As String = If(SessionUtilisateur.Role, String.Empty)
            If Not String.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase) AndAlso
               Not String.Equals(role, "SUPERADMIN", StringComparison.OrdinalIgnoreCase) Then
                Throw New UnauthorizedAccessException("Action réservée aux rôles ADMIN ou SUPERADMIN.")
            End If
        End Sub

        Private Shared Function Echec(statut As AuthentificationStatut, message As String, tentativesRestantes As Integer) As AuthentificationResultat
            Return New AuthentificationResultat With {.Statut = statut, .Message = message, .TentativesRestantes = tentativesRestantes}
        End Function

        Private Function CodeTemporaireValide(user As Utilisateur, code As String) As Boolean
            If user Is Nothing OrElse user.ResetPasswordHash Is Nothing OrElse user.ResetPasswordSel Is Nothing Then Return False
            If user.ResetPasswordUsedAt.HasValue Then Return False
            If Not user.ResetPasswordExpireAt.HasValue OrElse user.ResetPasswordExpireAt.Value <= Date.Now Then Return False
            Return VerifierMotDePasse(code, user.ResetPasswordSel, user.ResetPasswordHash)
        End Function

        Private Function CodeTemporaireExpire(user As Utilisateur, code As String) As Boolean
            If user Is Nothing OrElse user.ResetPasswordHash Is Nothing OrElse user.ResetPasswordSel Is Nothing Then Return False
            If user.ResetPasswordUsedAt.HasValue Then Return False
            If Not user.ResetPasswordExpireAt.HasValue OrElse user.ResetPasswordExpireAt.Value > Date.Now Then Return False
            Return VerifierMotDePasse(code, user.ResetPasswordSel, user.ResetPasswordHash)
        End Function

        Private Function GenererCodeTemporaire4Chiffres() As String
            Dim bytes(3) As Byte
            Using rng As New RNGCryptoServiceProvider()
                rng.GetBytes(bytes)
            End Using
            Dim valeur As UInteger = BitConverter.ToUInt32(bytes, 0) Mod 10000UI
            Return CInt(valeur).ToString("0000")
        End Function

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
