Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class Utilisateur
        Public Property UtilisateurId As Integer
        Public Property NomUtilisateur As String
        Public Property MotDePasseHash As Byte()
        Public Property MotDePasseSel As Byte()
        Public Property EstActif As Boolean
        Public Property CreeLe As Date
        Public Property NombreTentativesEchouees As Integer
        Public Property EstVerrouille As Boolean
        Public Property DateVerrouillage As Date?
        Public Property ResetPasswordHash As Byte()
        Public Property ResetPasswordSel As Byte()
        Public Property ResetPasswordExpireAt As Date?
        Public Property ResetPasswordUsedAt As Date?
        Public Property DoitChangerMotDePasse As Boolean
    End Class
End Namespace
