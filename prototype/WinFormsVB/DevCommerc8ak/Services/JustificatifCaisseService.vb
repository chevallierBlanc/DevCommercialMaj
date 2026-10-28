Option Strict On
Option Explicit On
Imports System
Imports System.Data
Imports System.Data.SqlClient
Imports System.Collections.Generic

Namespace DevCommerc8ak
    Public Class JustificatifCaisseService
        Private ReadOnly _dal As DAL
        Public Sub New(dal As DAL)
            _dal = dal
        End Sub

        Public Function Lire(factureId As Integer) As FacturePrintData
            If factureId <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(factureId))
            Using cn As SqlConnection = _dal.CreerConnexion()
                cn.Open()
                Using tx As SqlTransaction = cn.BeginTransaction(IsolationLevel.Serializable)
                    ' Droit existant de la caisse, du role actif uniquement.
                    AutorisationActionService.Exiger(cn, tx, "ENCAISSEMENT_CREER", "CAISSE")
                    Dim tables As DataSet = New FactureImpressionRepository(_dal).LireJustificatif(cn, tx, factureId)
                    If tables.Tables("Entete").Rows.Count <> 1 Then Throw New InvalidOperationException("Facture introuvable.")
                    Dim f As DataRow = tables.Tables("Entete").Rows(0)
                    Dim paiements As DataTable = tables.Tables("Paiements")
                    JustificatifCaisseRegles.Verifier(CStr(f("Statut")), CDec(f("MontantTotal")), paiements)
                    Dim propreEncaissement As Boolean = False
                    For Each p As DataRow In paiements.Rows
                        If Not p.IsNull("PayePar") AndAlso CInt(p("PayePar")) = SessionUtilisateur.UtilisateurId Then propreEncaissement = True
                    Next
                    If Not propreEncaissement Then Throw New UnauthorizedAccessException("Seuls vos encaissements confirmés peuvent être réimprimés depuis la Caisse. Utilisez l'historique avec les droits requis pour les autres documents.")
                    Dim data As New FacturePrintData With {
                        .Numero = Convert.ToString(f("NumeroFacture")), .DateDocument = CDate(f("CreeLe")),
                        .Client = Convert.ToString(f("ClientNom")), .Telephone = Convert.ToString(f("Telephone")),
                        .Statut = CStr(f("Statut")), .SousTotal = CDec(f("MontantTotal")), .Total = CDec(f("MontantTotal")),
                        .TitreThermique = "TICKET DE CAISSE", .LibelleTotalThermique = "Montant à payer", .MasquerRemiseThermique = True,
                        .MentionThermique = "ACHAT DÉFINITIF - Aucun échange ni reprise.", .Lignes = New List(Of FacturePrintLine)()}
                    For Each l As DataRow In tables.Tables("Lignes").Rows
                        data.Lignes.Add(New FacturePrintLine With {.ProduitId = CInt(l("ProduitId")), .Produit = Convert.ToString(l("Libelle")), .Conditionnement = Convert.ToString(l("TypeVente")),
                            .Quantite = CDec(l("Quantite")), .PrixUnitaire = CDec(l("PrixUnitaire")), .Montant = CDec(l("MontantLigne"))})
                    Next
                    If data.Lignes.Count = 0 Then Throw New InvalidOperationException("Aucune ligne de facture persistée à imprimer.")
                    For Each p As DataRow In paiements.Rows
                        data.ReglementsThermiques.Add(New KeyValuePair(Of String, String)("Règlement", CDec(p("Montant")).ToString("N2") & " FC"))
                        data.ReglementsThermiques.Add(New KeyValuePair(Of String, String)("Montant reçu", MontantHistorique(p, "MontantRecu")))
                        data.ReglementsThermiques.Add(New KeyValuePair(Of String, String)("Monnaie rendue", MontantHistorique(p, "MonnaieRendue")))
                        data.ReglementsThermiques.Add(New KeyValuePair(Of String, String)("Mode paiement", Convert.ToString(p("ModePaiement"))))
                        data.ReglementsThermiques.Add(New KeyValuePair(Of String, String)("Caissier", If(p.IsNull("Caissier"), "Auteur historique non disponible", Convert.ToString(p("Caissier")))))
                        data.ReglementsThermiques.Add(New KeyValuePair(Of String, String)("Payé le", CDate(p("PayeLe")).ToString("dd/MM/yyyy HH:mm")))
                        If Not String.IsNullOrWhiteSpace(Convert.ToString(p("ReferencePaiement"))) Then data.ReglementsThermiques.Add(New KeyValuePair(Of String, String)("Référence", CStr(p("ReferencePaiement"))))
                    Next
                    tx.Commit()
                    ' Le journal se lit apres liberation des verrous financiers.
                    ' Le contenu est rapproche de chaque ligne avant utilisation.
                    ConditionnementsHistoriquesService.Appliquer(New FactureImpressionRepository(_dal).LireInstantaneConditionnements(cn, Nothing, factureId), data.Lignes)
                    Return data
                End Using
            End Using
        End Function

        Private Shared Function MontantHistorique(row As DataRow, nom As String) As String
            Return If(row.IsNull(nom), "Non renseigné (historique)", Convert.ToDecimal(row(nom)).ToString("N2") & " FC")
        End Function
    End Class
End Namespace
