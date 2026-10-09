Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Configuration
Imports System.Linq
Imports System

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

        Public Function ListerPourConfiguration(produitId As Integer) As List(Of ProduitConditionnementDTO)
            Return ListerParProduit(produitId, False)
        End Function

        Public Function ListerPourAchat(produitId As Integer) As List(Of ProduitConditionnementDTO)
            ' Achat autorisé est une règle contextuelle : un conditionnement
            ' peut rester actif pour l'inventaire ou l'affichage sans être
            ' proposé dans les approvisionnements.
            Return ListerParProduit(produitId, True).
                Where(Function(c) c.EstAchetable).
                ToList()
        End Function

        Public Function ListerPourVente(produitId As Integer) As List(Of ProduitConditionnementDTO)
            ' Vente autorisée filtre uniquement les choix commerciaux futurs.
            ' Les conditionnements non vendables restent utilisables pour le
            ' stock physique, la décomposition et l'historique.
            Return ListerParProduit(produitId, True).
                Where(Function(c) c.EstVendable).
                ToList()
        End Function

        Public Function ListerPourInventaire(produitId As Integer) As List(Of ProduitConditionnementDTO)
            Return ListerParProduit(produitId, True)
        End Function

        Public Function ListerPourOperationPhysique(produitId As Integer) As List(Of ProduitConditionnementDTO)
            Return ListerParProduit(produitId, True)
        End Function

        Public Function ListerUnites(Optional actifSeulement As Boolean = True) As List(Of UniteMesureDTO)
            Return ObtenirRepository().ListerUnites(actifSeulement)
        End Function

        Public Function ObtenirParId(produitConditionnementId As Integer) As ProduitConditionnementDTO
            Return ObtenirRepository().ObtenirParId(produitConditionnementId)
        End Function

        Public Function Enregistrer(conditionnement As ProduitConditionnementDTO) As Integer
            VerifierAccesTechnique()
            If conditionnement Is Nothing Then Throw New ArgumentNullException("conditionnement")
            If conditionnement.ProduitId <= 0 Then Throw New InvalidOperationException("Produit invalide.")
            If conditionnement.UniteMesureId <= 0 Then Throw New InvalidOperationException("Unité de mesure obligatoire.")

            Dim existants As List(Of ProduitConditionnementDTO) = ListerParProduit(conditionnement.ProduitId, False)

            ' Une seule unité de base active est autorisée : tout le stock interne
            ' d'un produit doit avoir une référence physique unique et stable.
            If conditionnement.EstUniteBase Then
                Dim autreBase As ProduitConditionnementDTO = existants.FirstOrDefault(Function(c) c.EstActif AndAlso c.EstUniteBase AndAlso c.ProduitConditionnementId <> conditionnement.ProduitConditionnementId)
                If autreBase IsNot Nothing Then Throw New InvalidOperationException("Ce produit possède déjà une unité de base active.")
                conditionnement.ConditionnementParentId = Nothing
                conditionnement.FacteurVersParent = Nothing
                conditionnement.FacteurVersBase = 1D
                conditionnement.Niveau = 0
            Else
                If Not conditionnement.ConditionnementParentId.HasValue Then Throw New InvalidOperationException("Un conditionnement non base doit avoir un parent.")
                If conditionnement.ConditionnementParentId.Value = conditionnement.ProduitConditionnementId Then Throw New InvalidOperationException("Un conditionnement ne peut pas être son propre parent.")
                If Not conditionnement.FacteurVersParent.HasValue OrElse conditionnement.FacteurVersParent.Value <= 0D Then Throw New InvalidOperationException("Le facteur vers le parent doit être supérieur à zéro.")

                Dim parent As ProduitConditionnementDTO = existants.FirstOrDefault(Function(c) c.ProduitConditionnementId = conditionnement.ConditionnementParentId.Value AndAlso c.EstActif)
                If parent Is Nothing Then Throw New InvalidOperationException("Conditionnement parent introuvable ou inactif.")
                VerifierAbsenceCycle(conditionnement, parent, existants)

                ' Le facteur vers base est dérivé de la hiérarchie :
                ' enfant base=1, parent=FacteurVersParent * parent.FacteurVersBase.
                conditionnement.FacteurVersBase = conditionnement.FacteurVersParent.Value * parent.FacteurVersBase
                conditionnement.Niveau = parent.Niveau + 1
            End If

            conditionnement.ModifiePar = ObtenirUtilisateur()
            Dim repo As ProduitConditionnementRepository = ObtenirRepository()
            Dim id As Integer = repo.Enregistrer(conditionnement)
            RecalculerDescendants(repo, conditionnement.ProduitId, id, New HashSet(Of Integer)())
            AuditActionService.Enregistrer("Conditionnements", "Configuration conditionnement", "ProduitId=" & conditionnement.ProduitId.ToString() & ", ConditionnementId=" & id.ToString())
            AppEvents.OnProduitModifie()
            AppEvents.OnDataChanged()
            Return id
        End Function

        Public Sub Desactiver(produitConditionnementId As Integer)
            VerifierAccesTechnique()
            ObtenirRepository().Desactiver(produitConditionnementId, ObtenirUtilisateur())
            AuditActionService.Enregistrer("Conditionnements", "Désactivation conditionnement", "ConditionnementId=" & produitConditionnementId.ToString())
            AppEvents.OnProduitModifie()
            AppEvents.OnDataChanged()
        End Sub

        Public Sub Activer(produitConditionnementId As Integer)
            VerifierAccesTechnique()
            Dim repo As ProduitConditionnementRepository = ObtenirRepository()
            Dim cible As ProduitConditionnementDTO = repo.ObtenirParId(produitConditionnementId)
            If cible Is Nothing Then Throw New InvalidOperationException("Conditionnement introuvable.")

            Dim existants As List(Of ProduitConditionnementDTO) = repo.ListerParProduit(cible.ProduitId, False)
            If cible.EstUniteBase AndAlso existants.Any(Function(c) c.EstActif AndAlso c.EstUniteBase AndAlso c.ProduitConditionnementId <> cible.ProduitConditionnementId) Then
                Throw New InvalidOperationException("Ce produit possède déjà une unité de base active.")
            End If
            If existants.Any(Function(c) c.EstActif AndAlso c.UniteMesureId = cible.UniteMesureId AndAlso c.ProduitConditionnementId <> cible.ProduitConditionnementId) Then
                Throw New InvalidOperationException("Un conditionnement actif existe déjà pour cette unité.")
            End If
            If Not cible.EstUniteBase AndAlso (Not cible.ConditionnementParentId.HasValue OrElse Not existants.Any(Function(c) c.EstActif AndAlso c.ProduitConditionnementId = cible.ConditionnementParentId.Value)) Then
                Throw New InvalidOperationException("Réactivez d'abord l'unité contenue par ce conditionnement.")
            End If

            ' La réactivation restaure uniquement la disponibilité future de ce
            ' conditionnement. Les QuantiteBase historiques restent immuables.
            repo.Activer(produitConditionnementId, ObtenirUtilisateur())
            AuditActionService.Enregistrer("Conditionnements", "Activation conditionnement", "ConditionnementId=" & produitConditionnementId.ToString())
            AppEvents.OnProduitModifie()
            AppEvents.OnDataChanged()
        End Sub

        Public Function FormaterStock(produitId As Integer, quantiteBase As Decimal, fallback As Func(Of String)) As String
            Dim conditionnements As List(Of ProduitConditionnementDTO) = ListerPourInventaire(produitId)
            If conditionnements.Count > 0 Then
                Return ConversionUniteService.DecomposerStock(quantiteBase, conditionnements)
            End If

            Return fallback()
        End Function

        Private Shared Sub VerifierAbsenceCycle(conditionnement As ProduitConditionnementDTO, parent As ProduitConditionnementDTO, existants As List(Of ProduitConditionnementDTO))
            Dim courant As ProduitConditionnementDTO = parent
            While courant IsNot Nothing AndAlso courant.ConditionnementParentId.HasValue
                If courant.ConditionnementParentId.Value = conditionnement.ProduitConditionnementId Then
                    Throw New InvalidOperationException("Cycle détecté dans la hiérarchie des conditionnements.")
                End If
                Dim parentId As Integer = courant.ConditionnementParentId.Value
                courant = existants.FirstOrDefault(Function(c) c.ProduitConditionnementId = parentId)
            End While
        End Sub

        Private Shared Sub RecalculerDescendants(repo As ProduitConditionnementRepository, produitId As Integer, parentId As Integer, visites As HashSet(Of Integer))
            If repo Is Nothing OrElse parentId <= 0 OrElse visites.Contains(parentId) Then Return
            visites.Add(parentId)

            Dim tous As List(Of ProduitConditionnementDTO) = repo.ListerParProduit(produitId, False)
            Dim parent As ProduitConditionnementDTO = tous.FirstOrDefault(Function(c) c.ProduitConditionnementId = parentId)
            If parent Is Nothing Then Return

            For Each enfant As ProduitConditionnementDTO In tous.Where(Function(c) c.EstActif AndAlso c.ConditionnementParentId.HasValue AndAlso c.ConditionnementParentId.Value = parentId).ToList()
                If Not enfant.FacteurVersParent.HasValue OrElse enfant.FacteurVersParent.Value <= 0D Then Continue For

                ' Quand un niveau intermédiaire change, tous les niveaux qui en dépendent
                ' doivent recalculer leur facteur cumulé. Les mouvements historiques gardent
                ' leur QuantiteBase déjà enregistrée ; seule la configuration future est mise à jour.
                enfant.FacteurVersBase = enfant.FacteurVersParent.Value * parent.FacteurVersBase
                enfant.Niveau = parent.Niveau + 1
                enfant.ModifiePar = ObtenirUtilisateur()
                repo.Enregistrer(enfant)
                RecalculerDescendants(repo, produitId, enfant.ProduitConditionnementId, visites)
            Next
        End Sub

        Private Shared Sub VerifierAccesTechnique()
            If Not String.Equals(If(SessionUtilisateur.Role, String.Empty), "SUPERADMIN", StringComparison.OrdinalIgnoreCase) Then
                Throw New UnauthorizedAccessException("La configuration des conditionnements est réservée au SUPERADMIN.")
            End If
        End Sub

        Private Shared Function ObtenirUtilisateur() As String
            If Not String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur) Then Return SessionUtilisateur.NomUtilisateur.Trim()
            Return "SYSTEM"
        End Function
    End Class
End Namespace
