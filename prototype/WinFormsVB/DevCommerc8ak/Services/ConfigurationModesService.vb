Option Strict On
Option Explicit On

Imports System
Imports System.Data.SqlClient

Namespace DevCommerc8ak
    Public Class ConfigurationModes
        Public Property Active As Boolean
        Public Property Facturation As Boolean
        Public Property Caisse As Boolean
        Public Property Combine As Boolean
    End Class

    Public Class ConfigurationModesService
        Private ReadOnly _dal As DAL
        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Function Charger() As ConfigurationModes
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "PARAMETRES_SECURITE", "PARAMETRES")
                    Dim result As ConfigurationModes = Lire(cn, tx)
                    tx.Commit()
                    Return result
                End Using
            End Using
        End Function

        Private Shared Function Lire(cn As SqlConnection, tx As SqlTransaction) As ConfigurationModes
            Using cmd As New SqlCommand("SELECT GestionModesActive,ModeFacturationAutorise,ModeCaisseAutorise,ModeCombineAutorise FROM dbo.Parametres WITH(UPDLOCK,HOLDLOCK)", cn, tx)
                Using r As SqlDataReader = cmd.ExecuteReader()
                    If Not r.Read() Then Throw New InvalidOperationException("Configuration entreprise absente.")
                    Dim result As New ConfigurationModes With {.Active = CBool(r(0)), .Facturation = CBool(r(1)), .Caisse = CBool(r(2)), .Combine = CBool(r(3))}
                    If r.Read() Then Throw New InvalidOperationException("Plusieurs configurations entreprise : modification refusée.")
                    Return result
                End Using
            End Using
        End Function

        Public Sub Enregistrer(valeur As ConfigurationModes, attendu As ConfigurationModes, motif As String)
            motif = AuditMetierService.ValiderMotif(motif)
            If valeur.Active AndAlso Not (valeur.Facturation OrElse valeur.Caisse OrElse valeur.Combine) Then Throw New ArgumentException("Activez au moins un mode.")
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction()
                    AutorisationActionService.Exiger(cn, tx, "PARAMETRES_SECURITE", "PARAMETRES")
                    Dim avant As ConfigurationModes = Lire(cn, tx)
                    If avant.Active <> attendu.Active OrElse avant.Facturation <> attendu.Facturation OrElse avant.Caisse <> attendu.Caisse OrElse avant.Combine <> attendu.Combine Then Throw New System.Data.DBConcurrencyException("Les paramètres ont changé. Rechargez avant d'enregistrer.")
                    Using cmd As New SqlCommand("UPDATE dbo.Parametres SET GestionModesActive=@active,ModeFacturationAutorise=@fact,ModeCaisseAutorise=@caisse,ModeCombineAutorise=@combine", cn, tx)
                        cmd.Parameters.AddWithValue("@active", valeur.Active)
                        cmd.Parameters.AddWithValue("@fact", valeur.Facturation)
                        cmd.Parameters.AddWithValue("@caisse", valeur.Caisse)
                        cmd.Parameters.AddWithValue("@combine", valeur.Combine)
                        If cmd.ExecuteNonQuery() <> 1 Then Throw New InvalidOperationException("Configuration entreprise ambiguë.")
                    End Using
                    AuditMetierService.Enregistrer(cn, tx, "PERMISSION_MODIFIEE", "AUTORISATIONS", "Parametres", 0, "MODES_TRAVAIL", avant, valeur, motif, Guid.NewGuid())
                    tx.Commit()
                End Using
            End Using
        End Sub
    End Class
End Namespace
