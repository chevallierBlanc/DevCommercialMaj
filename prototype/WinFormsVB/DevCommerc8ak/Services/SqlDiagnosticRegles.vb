Option Strict On
Option Explicit On
Imports System

Namespace DevCommerc8ak
    Public NotInheritable Class SqlDiagnosticRegles
        Private Sub New()
        End Sub
        Public Shared Function Classer(numero As Integer, ouverture As Boolean) As String
            Select Case numero
                Case -2 : Return If(ouverture, "DELAI_CONNEXION", "DELAI_COMMANDE")
                Case 1205 : Return "INTERBLOCAGE"
                Case 1222 : Return "ATTENTE_VERROU"
                Case 18456, 18452 : Return "AUTHENTIFICATION"
                Case 4060, 942 : Return "BASE_INDISPONIBLE"
                Case 53, 26, 11001 : Return "ADRESSE_INSTANCE"
                Case 10054, 10053, 10060, 64, 233 : Return "TRANSPORT"
                Case Else : Return "SQL_APPLICATIF"
            End Select
        End Function
        Public Shared Function ResultatIncertain(numero As Integer, commandeDemarree As Boolean) As Boolean
            Return commandeDemarree AndAlso (numero = -2 OrElse Classer(numero, False) = "TRANSPORT")
        End Function
        Public Shared Function Message(categorie As String) As String
            Select Case categorie
                Case "DELAI_CONNEXION", "ADRESSE_INSTANCE", "TRANSPORT" : Return "Liaison SQL interrompue ou serveur injoignable. Vérifiez le réseau et le service SQL."
                Case "DELAI_COMMANDE" : Return "SQL ne répond pas dans le délai prévu : charge ou blocage possible, pas nécessairement une panne réseau."
                Case "INTERBLOCAGE", "ATTENTE_VERROU" : Return "Opération SQL bloquée par une autre transaction. Faites vérifier les verrous."
                Case "AUTHENTIFICATION" : Return "Connexion SQL refusée : faites vérifier le compte configuré."
                Case "BASE_INDISPONIBLE" : Return "La base demandée est indisponible ou inaccessible à ce compte."
                Case Else : Return "Erreur SQL applicative. Transmettez la référence technique au support."
            End Select
        End Function
        Public Shared Function LibelleCourt(categorie As String) As String
            Select Case categorie
                Case "TRANSPORT", "ADRESSE_INSTANCE", "DELAI_CONNEXION" : Return "liaison à vérifier"
                Case "DELAI_COMMANDE", "ATTENTE_VERROU", "INTERBLOCAGE" : Return "délai ou blocage"
                Case "AUTHENTIFICATION" : Return "accès refusé"
                Case "BASE_INDISPONIBLE" : Return "base indisponible"
                Case "CONFIGURATION" : Return "configuration invalide"
                Case Else : Return "erreur à diagnostiquer"
            End Select
        End Function
    End Class

    Public Class TemporisationSondeSql
        Private prochaine As DateTime = DateTime.MinValue
        Public ReadOnly Property ProchaineVerificationUtc As DateTime
            Get
                Return prochaine
            End Get
        End Property
        Private echecs As Integer
        Public Sub Enregistrer(disponible As Boolean, maintenantUtc As DateTime)
            echecs = If(disponible, 0, Math.Min(3, echecs + 1))
            prochaine = maintenantUtc.AddSeconds(If(echecs <= 1, 15, If(echecs = 2, 30, 60)))
        End Sub
    End Class
End Namespace
