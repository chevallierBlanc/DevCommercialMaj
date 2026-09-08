Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Globalization
Imports System.Linq

Namespace DevCommerc8ak
    Public Class InitialisationVenteService
        Public Const ModeHistoriqueUniquement As String = "HISTORIQUE_UNIQUEMENT"
        Public Const ModeReconstitutionComplete As String = "RECONSTITUTION_COMPLETE"

        Private Function ObtenirDal() As DAL
            Return New DAL(ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString)
        End Function

        Private Function ObtenirRepository() As InitialisationVenteRepository
            Return New InitialisationVenteRepository(ObtenirDal())
        End Function

        Private Sub VerifierAccesSuperAdmin()
            If Not String.Equals(If(SessionUtilisateur.Role, String.Empty), "SUPERADMIN", StringComparison.OrdinalIgnoreCase) Then
                Throw New UnauthorizedAccessException("L'initialisation des ventes est réservée au SUPERADMIN.")
            End If
        End Sub

        Public Function ListerProduits() As DataTable
            VerifierAccesSuperAdmin()
            Return (New ProduitRepository(ObtenirDal())).ListerTable()
        End Function

        Public Function ListerCategories() As DataTable
            VerifierAccesSuperAdmin()
            Return (New SuperAdminService()).ListerCategories()
        End Function

        Public Function ListerTypesVente(produitId As Integer) As List(Of TypeVenteDTO)
            VerifierAccesSuperAdmin()
            Dim produit As ProduitDTO = (New ProduitRepository(ObtenirDal())).ObtenirParId(produitId)
            If produit Is Nothing Then Return New List(Of TypeVenteDTO)()

            Dim contenuSecondaire As Decimal = If(produit.ContenuUniteSecondaire.HasValue, produit.ContenuUniteSecondaire.Value, 0D)
            Return (New TypeVenteService()).ConstruireTypesVentePourProduit(
                produit.ProduitId,
                produit.ConversionUnite,
                produit.PrixAchat,
                produit.PrixGros,
                produit.PrixDemi,
                produit.PrixDetail,
                produit.PrixQuart,
                produit.PrixDouzaine,
                produit.PrixSpecial,
                produit.VenteGros,
                produit.VenteDemi,
                produit.VenteDetail,
                produit.VenteDouzaine,
                Nothing,
                produit.ContenuUnitePrincipale,
                contenuSecondaire,
                produit.TypeGestionStock,
                produit.UniteSecondaire)
        End Function

        Public Function CreerOuMettreAJourSession(sessionId As Integer,
                                                  dateDebut As Date,
                                                  dateFin As Date,
                                                  modeStock As String,
                                                  observation As String) As InitialisationVenteSessionDTO
            VerifierAccesSuperAdmin()
            ValiderPeriode(dateDebut, dateFin)
            Dim modeNormalise As String = NormaliserModeStock(modeStock)
            Dim repo As InitialisationVenteRepository = ObtenirRepository()
            If sessionId <= 0 Then
                Dim session As InitialisationVenteSessionDTO = repo.CreerSession(dateDebut, dateFin, modeNormalise, observation, SessionUtilisateur.UtilisateurId, Environment.MachineName)
                AuditActionService.Enregistrer("Initialisation ventes", "Création session", "Session " & session.ReferenceSession & " créée en mode " & modeNormalise & ".")
                Return session
            End If

            repo.MettreAJourSession(sessionId, dateDebut, dateFin, modeNormalise, observation)
            Dim miseAJour As InitialisationVenteSessionDTO = repo.ObtenirSession(sessionId)
            If miseAJour IsNot Nothing Then
                AuditActionService.Enregistrer("Initialisation ventes", "Modification session", "Session " & miseAJour.ReferenceSession & " mise à jour.")
            End If
            Return miseAJour
        End Function

        Public Function ConstruireLigne(sessionId As Integer,
                                        dateVente As Date,
                                        produitId As Integer,
                                        typeVente As TypeVenteDTO,
                                        quantiteCommerciale As Decimal,
                                        prixUnitaire As Decimal) As InitialisationVenteLigneDTO
            VerifierAccesSuperAdmin()
            If sessionId <= 0 Then Throw New InvalidOperationException("Créez d'abord une session d'initialisation.")
            If produitId <= 0 Then Throw New InvalidOperationException("Sélectionnez un produit.")
            If typeVente Is Nothing Then Throw New InvalidOperationException("Sélectionnez un type de vente.")
            If quantiteCommerciale <= 0D Then Throw New InvalidOperationException("La quantité doit être supérieure à zéro.")
            If prixUnitaire < 0D Then Throw New InvalidOperationException("Le prix unitaire ne peut pas être négatif.")

            Dim session As InitialisationVenteSessionDTO = ObtenirRepository().ObtenirSession(sessionId)
            If session Is Nothing Then Throw New InvalidOperationException("Session d'initialisation introuvable.")
            If Not String.Equals(session.Statut, "BROUILLON", StringComparison.OrdinalIgnoreCase) Then
                Throw New InvalidOperationException("Une session validée ou annulée ne peut plus être modifiée.")
            End If
            If dateVente.Date < session.DateDebut.Date OrElse dateVente.Date > session.DateFin.Date Then
                Throw New InvalidOperationException("La date de vente doit appartenir à la période de reprise.")
            End If

            Dim produit As ProduitDTO = (New ProduitRepository(ObtenirDal())).ObtenirParId(produitId)
            If produit Is Nothing Then Throw New InvalidOperationException("Produit introuvable.")

            Return ConstruireLigneDepuisProduit(sessionId, dateVente, produit, typeVente, quantiteCommerciale, prixUnitaire)
        End Function

        Public Function CalculerApercuLigne(produitId As Integer,
                                            typeVente As TypeVenteDTO,
                                            quantiteCommerciale As Decimal,
                                            prixUnitaire As Decimal) As InitialisationVenteLigneDTO
            VerifierAccesSuperAdmin()
            If produitId <= 0 OrElse typeVente Is Nothing OrElse quantiteCommerciale <= 0D Then
                Return New InitialisationVenteLigneDTO()
            End If

            Dim produit As ProduitDTO = (New ProduitRepository(ObtenirDal())).ObtenirParId(produitId)
            If produit Is Nothing Then Return New InitialisationVenteLigneDTO()

            Return ConstruireLigneDepuisProduit(0, Date.Today, produit, typeVente, quantiteCommerciale, prixUnitaire)
        End Function

        Private Function ConstruireLigneDepuisProduit(sessionId As Integer,
                                                      dateVente As Date,
                                                      produit As ProduitDTO,
                                                      typeVente As TypeVenteDTO,
                                                      quantiteCommerciale As Decimal,
                                                      prixUnitaire As Decimal) As InitialisationVenteLigneDTO
            Dim quantiteBase As Decimal = quantiteCommerciale * typeVente.QuantiteEquivalent
            Dim montant As Decimal = Math.Round(quantiteCommerciale * prixUnitaire, 2)
            Dim coutUnitaire As Decimal? = CalculVenteService.CalculerCoutUnitaireBase(produit.PrixAchat, produit.ConversionUnite, produit.TypeGestionStock, produit.ContenuUnitePrincipale)
            Dim benefice As Decimal? = Nothing
            If coutUnitaire.HasValue Then
                benefice = montant - (quantiteBase * coutUnitaire.Value)
            End If

            Return New InitialisationVenteLigneDTO With {
                .InitialisationVenteSessionId = sessionId,
                .DateVente = dateVente,
                .ProduitId = produit.ProduitId,
                .CodeProduit = produit.CodeBarres,
                .LibelleProduit = produit.Libelle,
                .Categorie = produit.NomCategorie,
                .TypeVente = typeVente.Nom,
                .UniteCommerciale = typeVente.Nom,
                .QuantiteCommerciale = quantiteCommerciale,
                .PrixUnitaire = prixUnitaire,
                .MontantLigne = montant,
                .QuantiteBase = quantiteBase,
                .CoutUnitaireBaseVente = coutUnitaire,
                .BeneficeEstime = benefice,
                .QuantiteBaseAffichage = FormatageGlobal.FormatStockSelonGestion(
                    quantiteBase,
                    produit.ConversionUnite,
                    produit.UnitePrincipale,
                    produit.UniteSecondaire,
                    produit.TypeGestionStock,
                    produit.UniteMesureStock,
                    produit.ContenuUnitePrincipale,
                    If(produit.ContenuUniteSecondaire.HasValue, produit.ContenuUniteSecondaire.Value, 0D))
            }
        End Function

        Public Function AjouterLigne(ligne As InitialisationVenteLigneDTO) As Integer
            VerifierAccesSuperAdmin()
            Dim id As Integer = ObtenirRepository().AjouterLigne(ligne)
            AuditActionService.Enregistrer("Initialisation ventes", "Ajout ligne", "Ligne " & id.ToString(CultureInfo.InvariantCulture) & " ajoutée pour " & ligne.LibelleProduit & ".")
            Return id
        End Function

        Public Sub SupprimerLigne(ligneId As Integer)
            VerifierAccesSuperAdmin()
            ObtenirRepository().SupprimerLigne(ligneId)
            AuditActionService.Enregistrer("Initialisation ventes", "Suppression ligne", "Ligne " & ligneId.ToString(CultureInfo.InvariantCulture) & " supprimée d'une session brouillon.")
        End Sub

        Public Function ListerLignes(sessionId As Integer) As DataTable
            VerifierAccesSuperAdmin()
            Return ObtenirRepository().ListerLignes(sessionId)
        End Function

        Public Function ExistePeriodeValideeChevauchante(dateDebut As Date, dateFin As Date, Optional sessionIdExclu As Integer = 0) As Boolean
            VerifierAccesSuperAdmin()
            Return ObtenirRepository().ExistePeriodeValideeChevauchante(dateDebut, dateFin, sessionIdExclu)
        End Function

        Public Sub ValiderSession(sessionId As Integer)
            VerifierAccesSuperAdmin()
            Dim repo As InitialisationVenteRepository = ObtenirRepository()
            Dim session As InitialisationVenteSessionDTO = repo.ObtenirSession(sessionId)
            If session Is Nothing Then Throw New InvalidOperationException("Session introuvable.")
            If ExistePeriodeValideeChevauchante(session.DateDebut, session.DateFin, sessionId) Then
                Throw New InvalidOperationException("Une session validée couvre déjà cette période. Validation annulée pour éviter une double reprise.")
            End If

            repo.ValiderSession(sessionId, SessionUtilisateur.UtilisateurId)
            AuditActionService.Enregistrer("Initialisation ventes", "Validation session", "Session " & session.ReferenceSession & " validée en mode " & session.ModeStock & ".")
        End Sub

        Private Shared Sub ValiderPeriode(dateDebut As Date, dateFin As Date)
            If dateFin.Date < dateDebut.Date Then
                Throw New InvalidOperationException("La date de fin doit être supérieure ou égale à la date de début.")
            End If
        End Sub

        Private Shared Function NormaliserModeStock(modeStock As String) As String
            If String.Equals(modeStock, ModeReconstitutionComplete, StringComparison.OrdinalIgnoreCase) Then
                Return ModeReconstitutionComplete
            End If

            Return ModeHistoriqueUniquement
        End Function
    End Class
End Namespace
