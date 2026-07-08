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
            Dim user As Utilisateur = _utilisateurRepo.ObtenirParNom(nomUtilisateur)
            If user Is Nothing Then Return False
            If Not user.EstActif Then Return False

            Dim ok As Boolean = VerifierMotDePasse(motDePasse, user.MotDePasseSel, user.MotDePasseHash)
            If Not ok Then Return False

            SessionUtilisateur.UtilisateurId = user.UtilisateurId
            SessionUtilisateur.NomUtilisateur = user.NomUtilisateur
            SessionUtilisateur.Role = _utilisateurRepo.ObtenirRole(user.UtilisateurId)
            SessionUtilisateur.SessionId = _sessionRepo.DemarrerSession(user.UtilisateurId)
            Return True
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
            _utilisateurRepo.Ajouter(u, roleId)
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

        ' Met a jour mot de passe.
        Public Sub ReinitialiserMotDePasse(utilisateurId As Integer, nouveauMotDePasse As String)
            Dim sel As Byte() = GenererSel()
            Dim hash As Byte() = HashMotDePasse(nouveauMotDePasse, sel)
            _utilisateurRepo.MettreAJourMotDePasse(utilisateurId, hash, sel)
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
            Dim hash As Byte() = HashMotDePasse(motDePasse, sel)
            If hash.Length <> hashAttendu.Length Then Return False
            For i As Integer = 0 To hash.Length - 1
                If hash(i) <> hashAttendu(i) Then Return False
            Next
            Return True
        End Function
    End Class
End Namespace
