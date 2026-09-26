Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Configuration

Namespace DevCommerc8ak
    Public Class ProduitConditionnementService
        Private Function ObtenirRepository() As ProduitConditionnementRepository
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Return New ProduitConditionnementRepository(dal)
        End Function

        Public Function ListerParProduit(produitId As Integer, Optional actifSeulement As Boolean = True) As List(Of ProduitConditionnementDTO)
            Return ObtenirRepository().ListerParProduit(produitId, actifSeulement)
        End Function

        Public Function ObtenirParId(produitConditionnementId As Integer) As ProduitConditionnementDTO
            Return ObtenirRepository().ObtenirParId(produitConditionnementId)
        End Function
    End Class
End Namespace
