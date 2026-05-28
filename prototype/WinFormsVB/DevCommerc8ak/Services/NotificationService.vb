Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data

Namespace DevCommerc8ak
    Public Class NotificationService
        Private ReadOnly _dal As DAL
        Private ReadOnly _repo As NotificationRepository
        Private Shared ReadOnly _memoireEmission As New Dictionary(Of String, Date)
        Private Shared ReadOnly _memoireSync As New Dictionary(Of String, Date)

        Public Sub New(dal As DAL)
            _dal = dal
            _repo = New NotificationRepository(dal)
        End Sub

        Public Function ListerNonLues() As DataTable
            Return _repo.ListerNonLues()
        End Function

        Public Function ListerToutes() As DataTable
            Return _repo.ListerToutes()
        End Function

        Public Function CompterNonLues() As Integer
            Return _repo.CompterNonLues()
        End Function

        Public Sub MarquerToutesLues()
            _repo.MarquerLues()
        End Sub

        Public Sub DeclencherEvenementMetier(typeNotification As String,
                                             message As String,
                                             cleNotification As String,
                                             ecranCible As String,
                                             donneesCible As String,
                                             Optional estGroupee As Boolean = False,
                                             Optional minutesAntiRepetition As Integer = 10)
            If String.IsNullOrWhiteSpace(cleNotification) Then
                cleNotification = typeNotification & ":" & message
            End If

            Dim cleMemoire As String = typeNotification & "|" & cleNotification
            Dim dernierEnvoi As Date = Date.MinValue
            If _memoireEmission.ContainsKey(cleMemoire) Then
                dernierEnvoi = _memoireEmission(cleMemoire)
            End If
            If dernierEnvoi <> Date.MinValue AndAlso (Date.Now - dernierEnvoi).TotalMinutes < minutesAntiRepetition Then
                Return
            End If

            Dim dto As New NotificationDTO With {
                .TypeNotification = typeNotification,
                .Message = message,
                .CleNotification = cleNotification,
                .EcranCible = ecranCible,
                .DonneesCible = donneesCible,
                .CompteurOccurrences = 1,
                .EstGroupee = estGroupee,
                .CreeLe = Date.Now,
                .Lue = False
            }

            _repo.AjouterOuMettreAJour(dto, minutesAntiRepetition)
            _memoireEmission(cleMemoire) = Date.Now
        End Sub

        Public Sub SynchroniserAlertesMetier(seuil As Decimal, alerteJours As Integer, utilisateurId As Integer)
            If PeutSynchroniser("synchronisation_globale", 2) = False Then
                Return
            End If

            Dim rapport As New RapportService(_dal)
            Dim dtAlertes As DataTable = rapport.AlertesDetail(seuil, alerteJours)
            If dtAlertes Is Nothing OrElse dtAlertes.Rows.Count = 0 Then
                Return
            End If

            Dim compteurs As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            For Each row As DataRow In dtAlertes.Rows
                Dim typeAlerte As String = Convert.ToString(row("TypeAlerte"))
                If Not compteurs.ContainsKey(typeAlerte) Then
                    compteurs(typeAlerte) = 0
                End If
                compteurs(typeAlerte) += 1
            Next

            For Each kv As KeyValuePair(Of String, Integer) In compteurs
                Select Case kv.Key
                    Case "Stock critique"
                        DeclencherEvenementMetier("Warning",
                                                  kv.Value.ToString() & " produits en seuil critique",
                                                  "stock_critique",
                                                  "ALERTES_STOCK",
                                                  "stock_critique",
                                                  True,
                                                  10)
                    Case "Expiration proche"
                        DeclencherEvenementMetier("Warning",
                                                  kv.Value.ToString() & " produits expirent bientôt",
                                                  "expiration_proche",
                                                  "ALERTES_STOCK",
                                                  "expiration",
                                                  True,
                                                  20)
                    Case "Facture non payee"
                        DeclencherEvenementMetier("Info",
                                                  kv.Value.ToString() & " factures en attente de paiement",
                                                  "facture_non_payee",
                                                  "FACTURES",
                                                  "en_attente",
                                                  True,
                                                  10)
                    Case "Produit sans prix"
                        DeclencherEvenementMetier("Critical",
                                                  kv.Value.ToString() & " produits sans prix défini",
                                                  "produit_sans_prix",
                                                  "PRODUITS",
                                                  "prix_manquant",
                                                  True,
                                                  30)
                End Select
            Next

            If compteurs.ContainsKey("Stock critique") Then
                Dim approRepo As New BonApprovisionnementRepository(_dal)
                Dim approService As New ApprovisionnementService(_dal, approRepo)
                approService.GenererBonAuto(seuil, utilisateurId)
                DeclencherEvenementMetier("Info",
                                          "Bon d'approvisionnement automatique généré",
                                          "bon_auto_stock_critique",
                                          "APPROVISIONNEMENT",
                                          "auto",
                                          False,
                                          10)
            End If
        End Sub

        Private Function PeutSynchroniser(cle As String, secondes As Integer) As Boolean
            Dim dernier As Date = Date.MinValue
            If _memoireSync.ContainsKey(cle) Then
                dernier = _memoireSync(cle)
            End If
            If dernier <> Date.MinValue AndAlso (Date.Now - dernier).TotalSeconds < secondes Then
                Return False
            End If
            _memoireSync(cle) = Date.Now
            Return True
        End Function
    End Class
End Namespace
