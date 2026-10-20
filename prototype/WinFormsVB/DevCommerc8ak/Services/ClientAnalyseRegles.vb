Option Strict On
Imports System
Namespace DevCommerc8ak
    Public NotInheritable Class ClientAnalyseRegles
        Private Sub New()
        End Sub
        Public Shared Function DebutPeriode(annee As Integer, mois As Integer) As DateTime
            If annee < 1900 OrElse annee > 9998 OrElse mois < 0 OrElse mois > 12 Then Throw New ArgumentException("Période invalide.")
            Return New DateTime(annee, If(mois = 0, 1, mois), 1)
        End Function
        Public Shared Function FinPeriode(annee As Integer, mois As Integer) As DateTime
            Dim debut As DateTime = DebutPeriode(annee, mois)
            Return If(mois = 0, debut.AddYears(1), debut.AddMonths(1))
        End Function
    End Class
End Namespace
