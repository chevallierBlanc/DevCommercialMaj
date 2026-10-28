Imports System
Imports System.Data
Imports DevCommerc8ak

Module ThermiqueMetierTests
    Private nombre As Integer
    Private Sub Verifier(ok As Boolean)
        If Not ok Then Throw New Exception("Echec thermique metier " & nombre.ToString())
        nombre += 1
    End Sub
    Private Sub Refus(action As Action)
        Dim refuse As Boolean = False
        Try
            action()
        Catch ex As InvalidOperationException
            refuse = True
        End Try
        Verifier(refuse)
    End Sub
    Friend Function Executer() As Integer
        nombre = 0
        Verifier(ConditionnementCommercial.Libelle("gros", "carton", "pièce", 1D) = "carton")
        Verifier(ConditionnementCommercial.Libelle("gros", "sac", "kg", 1D) = "sac")
        Verifier(ConditionnementCommercial.Libelle("demi", "sac", "kg", 1D) = "demi-sac")
        Verifier(ConditionnementCommercial.Libelle("demi", "carton", "pièce", 1D) = "demi-carton")
        Verifier(ConditionnementCommercial.Libelle("gros", "bidon", "litre", 2D) = "bidons")
        Verifier(ConditionnementCommercial.Libelle("DETAIL", "sac", "kg", 1D) = "kg")
        Verifier(ConditionnementCommercial.Libelle("pièce", "carton", "pièce", 2D) = "pièces")
        Verifier(ConditionnementCommercial.Libelle("QUART", "sac", "kg", 1D) = "quart de sac")
        Verifier(ConditionnementCommercial.Libelle("DOUZAINE", "carton", "pièce", 2D) = "douzaines")
        Verifier(ConditionnementCommercial.Libelle("Paquets 10+1", "carton", "pièce", 2D) = "Paquets 10+1")
        Verifier(ConditionnementCommercial.Libelle("GROS", "carton", "pièce", 1D, personnalise:=True) = "GROS")
        Verifier(ConditionnementCommercial.Libelle("DOUZAINE", "carton", "pièce", 2D, personnalise:=True) = "DOUZAINE")
        ' Sans instantane historique, ne jamais substituer une unite actuelle.
        Verifier(ConditionnementCommercial.Libelle("gros", Nothing, Nothing, 1D) = "gros")
        Verifier(ConditionnementCommercial.Libelle("demi", Nothing, Nothing, 1D) = "demi")
        Verifier(ConditionnementCommercial.Libelle("demi-sac", Nothing, Nothing, 1D) = "demi-sac")
        Verifier(ConditionnementCommercial.Libelle("DOUZAINE", Nothing, Nothing, 2D) = "DOUZAINE")
        Verifier(ConditionnementCommercial.Libelle(Nothing, Nothing, Nothing, 1D).Contains("non renseigné"))

        Dim paiements As New DataTable()
        paiements.Columns.Add("Montant", GetType(Decimal))
        paiements.Columns.Add("DeviseMontants", GetType(String))
        paiements.Columns.Add("Devise", GetType(String))
        paiements.Columns.Add("PayeLe", GetType(Date))
        Refus(Sub() JustificatifCaisseRegles.Verifier("PAYEE", 100D, paiements))
        paiements.Rows.Add(100D, "FC", "FC", New Date(2020, 1, 1))
        For Each statut As String In New String() {"EN_ATTENTE", "BROUILLON", "IMPAYEE", "PARTIELLEMENT_PAYEE", "ANNULEE", ""}
            Refus(Sub() JustificatifCaisseRegles.Verifier(statut, 100D, paiements))
        Next
        JustificatifCaisseRegles.Verifier("PAYEE", 100D, paiements) : Verifier(True)
        Refus(Sub() JustificatifCaisseRegles.Verifier("PAYEE", 0D, paiements))
        paiements.Rows(0)("Montant") = 50D
        Refus(Sub() JustificatifCaisseRegles.Verifier("PAYEE", 100D, paiements))
        paiements.Rows(0)("Montant") = 150D
        Refus(Sub() JustificatifCaisseRegles.Verifier("PAYEE", 100D, paiements))
        paiements.Rows(0)("Montant") = 100D
        paiements.Rows(0)("DeviseMontants") = DBNull.Value
        JustificatifCaisseRegles.Verifier("PAYEE", 100D, paiements) : Verifier(True)
        paiements.Rows(0)("Devise") = "USD"
        Refus(Sub() JustificatifCaisseRegles.Verifier("PAYEE", 100D, paiements))
        paiements.Rows(0)("DeviseMontants") = "FC"
        JustificatifCaisseRegles.Verifier("PAYEE", 100D, paiements) : Verifier(True)
        paiements.Rows(0)("PayeLe") = DBNull.Value
        Refus(Sub() JustificatifCaisseRegles.Verifier("PAYEE", 100D, paiements))
        paiements.Rows(0)("PayeLe") = New Date(2020, 1, 1)
        paiements.Rows(0)("Montant") = 30D
        paiements.Rows.Add(70D, "FC", "FC", New Date(2020, 1, 2))
        JustificatifCaisseRegles.Verifier("PAYEE", 100D, paiements) : Verifier(True)
        For i As Integer = 1 To 3
            JustificatifCaisseRegles.Verifier("PAYEE", 100D, paiements)
        Next
        Verifier(paiements.Rows.Count = 2 AndAlso CDec(paiements.Rows(0)("Montant")) = 30D AndAlso CDec(paiements.Rows(1)("Montant")) = 70D)
        Return nombre
    End Function
End Module
