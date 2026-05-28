Option Strict On
Option Explicit On

Imports System

Namespace DevCommerc8ak.DTO
    ''' <summary>
    ''' DTO pour la gestion des dépenses
    ''' </summary>
    Public Class DepenseDTOFinance
        Public Property Id As Integer
        Public Property Categorie As String
        Public Property Montant As Decimal
        Public Property Devise As String
        Public Property Description As String
        Public Property DateDepense As DateTime
        Public Property Source As String ' Caisse / Banque
        Public Property TypeDepense As String ' Normale / Exceptionnelle
        Public Property CreePar As String
        Public Property CreatedAt As DateTime
    End Class

    ''' <summary>
    ''' DTO pour la gestion des opérations bancaires
    ''' </summary>
    Public Class BanqueDTO
        Public Property Id As Integer
        Public Property TypeOperation As String ' Depot / Retrait
        Public Property Montant As Decimal
        Public Property Devise As String
        Public Property Description As String
        Public Property DateOperation As DateTime
        Public Property Reference As String
        Public Property CreatedAt As DateTime
    End Class

    ''' <summary>
    ''' DTO pour l'état de la caisse
    ''' </summary>
    Public Class CaisseEtatDTO
        Public Property DateJour As DateTime
        Public Property EncaisseFC As Decimal
        Public Property EncaisseUSD As Decimal
        Public Property DepensesFC As Decimal
        Public Property DepensesUSD As Decimal
        Public Property SoldeFC As Decimal
        Public Property SoldeUSD As Decimal
    End Class
    Public Class CategorieDepenseDTO
        Public Property Id As Integer
        Public Property Libelle As String
        Public Property Description As String
        Public Property IsSystem As Boolean
    End Class

End Namespace
