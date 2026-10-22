Option Strict On
Option Explicit On
Imports System
Imports System.Data.SqlClient
Imports System.Globalization

Namespace DevCommerc8ak
    Public NotInheritable Class SqlDiagnosticService
        Private Sub New()
        End Sub
        Public Shared Sub Signaler(operation As String, erreur As SqlException, ouverture As Boolean, commandeDemarree As Boolean, dureeMs As Long, correlation As Guid, Optional phaseConnue As Boolean = True)
            ' Ne jamais transmettre Message, SQL, parametres ou chaine de connexion
            ' au journal : ils peuvent contenir des secrets ou donnees personnelles.
            Dim categorie As String = SqlDiagnosticRegles.Classer(erreur.Number, ouverture)
            Dim message As String = "UTC=" & DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) & " | Correlation=" & correlation.ToString("D") & " | Categorie=" & categorie & " | Numero=" & erreur.Number.ToString(CultureInfo.InvariantCulture) & " | Etat=" & erreur.State.ToString() & " | Classe=" & erreur.Class.ToString() & " | Connexion=" & erreur.ClientConnectionId.ToString("D") & " | DureeMs=" & dureeMs.ToString(CultureInfo.InvariantCulture) & " | Phase=" & If(Not phaseConnue, "TRANSACTION_INDETERMINEE", If(ouverture, "OUVERTURE", "COMMANDE")) & " | ResultatIncertain=" & SqlDiagnosticRegles.ResultatIncertain(erreur.Number, commandeDemarree).ToString()
            Dim log As New ProductionLogService()
            log.Warn("SQL", operation, message)
            ' L'exception d'origine et sa pile sont conservees par l'appelant.
        End Sub
    End Class
End Namespace
