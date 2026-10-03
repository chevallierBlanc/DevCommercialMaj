Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class UtilisateurDTO
        Public Property UtilisateurId As Integer
        Public Property NomUtilisateur As String
        Public Property EstActif As Boolean
        Public Property Role As String
        Public Property NombreTentativesEchouees As Integer
        Public Property EstVerrouille As Boolean
        Public Property DateVerrouillage As Date?
        Public Property DoitChangerMotDePasse As Boolean
        Public ReadOnly Property EtatCompte As String
            Get
                If Not EstActif Then Return "DÉSACTIVÉ"
                If EstVerrouille Then Return "VERROUILLÉ"
                Return "ACTIF"
            End Get
        End Property
    End Class

    Public Enum AuthentificationStatut
        Succes
        IdentifiantsInvalides
        CompteDesactive
        CompteVerrouille
        CodeTemporaireValide
        CodeTemporaireExpire
        AucunRole
        SessionActive
        ErreurTechnique
    End Enum

    Public Class AuthentificationResultat
        Public Property Statut As AuthentificationStatut
        Public Property Utilisateur As Utilisateur
        Public Property Message As String
        Public Property TentativesRestantes As Integer
        Public ReadOnly Property EstSucces As Boolean
            Get
                Return Statut = AuthentificationStatut.Succes OrElse Statut = AuthentificationStatut.CodeTemporaireValide
            End Get
        End Property
    End Class

    Public Class ResetMotDePasseTemporaireDTO
        Public Property UtilisateurId As Integer
        Public Property NomUtilisateur As String
        Public Property CodeTemporaire As String
        Public Property Expiration As Date
    End Class

    Public Enum EtatResetMotDePasse
        Inexistant
        Actif
        Utilise
        Expire
    End Enum

    Public Class RoleSessionInfo
        Public Property RoleId As Integer
        Public Property NomRole As String
        Public Property EstRolePrincipal As Boolean
    End Class
End Namespace
