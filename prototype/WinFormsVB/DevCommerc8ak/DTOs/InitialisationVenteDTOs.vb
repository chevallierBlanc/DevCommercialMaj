Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak
    Public Class InitialisationVenteSessionDTO
        Public Property InitialisationVenteSessionId As Integer
        Public Property ReferenceSession As String
        Public Property DateDebut As Date
        Public Property DateFin As Date
        Public Property ModeStock As String
        Public Property Observation As String
        Public Property Statut As String
        Public Property CreeLe As Date
        Public Property CreePar As Integer
        Public Property Machine As String
    End Class

    Public Class InitialisationVenteLigneDTO
        Public Property InitialisationVenteLigneId As Integer
        Public Property InitialisationVenteSessionId As Integer
        Public Property DateVente As Date
        Public Property ProduitId As Integer
        Public Property CodeProduit As String
        Public Property LibelleProduit As String
        Public Property Categorie As String
        Public Property TypeVente As String
        Public Property UniteCommerciale As String
        Public Property QuantiteCommerciale As Decimal
        Public Property PrixUnitaire As Decimal
        Public Property MontantLigne As Decimal
        Public Property QuantiteBase As Decimal
        Public Property CoutUnitaireBaseVente As Decimal?
        Public Property BeneficeEstime As Decimal?
        Public Property QuantiteBaseAffichage As String
    End Class
End Namespace
