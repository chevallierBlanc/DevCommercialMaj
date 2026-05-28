Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Drawing
Imports System.Collections.Generic
Imports System.Data.SqlClient


Namespace DevCommerc8ak
    Public Class ProduitDTO
        Public Property ProduitId As Integer
        Public Property CodeBarres As String
        Public Property Libelle As String
        Public Property PrixDetail As Decimal
        Public Property PrixAchat As Decimal
        Public Property PrixDemi As Decimal
        Public Property PrixQuart As Decimal
        Public Property PrixDouzaine As Decimal
        Public Property PrixGros As Decimal
        Public Property PrixSpecial As Decimal
        Public Property CoefficientGros As Decimal
        Public Property QuantiteStock As Decimal
        Public Property SeuilCritique As Decimal
        Public Property DateExpiration As Date?
        Public Property UnitePrincipale As String
        Public Property UniteSecondaire As String
        Public Property ConversionUnite As Decimal
        Public Property EstActif As Boolean
        Public Property VenteDetail As Boolean
        Public Property VenteDemi As Boolean
        Public Property VenteDouzaine As Boolean
        Public Property VenteGros As Boolean

    End Class
End Namespace
