Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireStockInitialTechnique
        Inherits Form

        Private ReadOnly _service As SuperAdminService
        Private ReadOnly grid As DataGridView
        Private ReadOnly btnRecharger As Button
        Private ReadOnly btnEnregistrer As Button
        Private _categories As DataTable

        Public Sub New()
            _service = New SuperAdminService()

            Text = "SuperAdmin - Stock initial technique"
            Width = 1450
            Height = 780
            StartPosition = FormStartPosition.CenterParent
            BackColor = Color.FromArgb(245, 247, 250)

            Dim root As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2, .Padding = New Padding(16)}
            root.RowStyles.Add(New RowStyle(SizeType.Absolute, 48))
            root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))

            Dim toolbar As New FlowLayoutPanel() With {.Dock = DockStyle.Fill}
            btnRecharger = New Button() With {.Text = "Recharger", .AutoSize = True}
            btnEnregistrer = New Button() With {.Text = "Enregistrer le stock initial", .AutoSize = True}
            toolbar.Controls.Add(btnRecharger)
            toolbar.Controls.Add(btnEnregistrer)

            grid = New DataGridView() With {
                .Dock = DockStyle.Fill,
                .AllowUserToAddRows = True,
                .AllowUserToDeleteRows = False,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                .BackgroundColor = Color.White,
                .BorderStyle = BorderStyle.None
            }

            root.Controls.Add(toolbar, 0, 0)
            root.Controls.Add(grid, 0, 1)
            Controls.Add(root)

            AddHandler Load, AddressOf FormulaireStockInitialTechnique_Load
            AddHandler btnRecharger.Click, AddressOf Recharger
            AddHandler btnEnregistrer.Click, AddressOf EnregistrerStockInitial
        End Sub

        Private Sub FormulaireStockInitialTechnique_Load(sender As Object, e As EventArgs)
            Recharger(Nothing, EventArgs.Empty)
        End Sub

        Private Sub Recharger(sender As Object, e As EventArgs)
            Try
                _categories = _service.ListerCategories()
                Dim dt As DataTable = _service.ListerProduitsStockInitial()
                If Not dt.Columns.Contains("QuantiteInitiale") Then dt.Columns.Add("QuantiteInitiale", GetType(Decimal))
                If Not dt.Columns.Contains("PrixAchatOptionnel") Then dt.Columns.Add("PrixAchatOptionnel", GetType(Decimal))
                If Not dt.Columns.Contains("TypesPersonnalises") Then dt.Columns.Add("TypesPersonnalises", GetType(String))
                If Not dt.Columns.Contains("DateInitiale") Then dt.Columns.Add("DateInitiale", GetType(Date))
                For Each row As DataRow In dt.Rows
                    If row.IsNull("DateInitiale") Then
                        row("DateInitiale") = Date.Now
                    End If
                Next

                grid.DataSource = dt
                ConfigurerColonnes()
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireStockInitialTechnique", "Recharger", "Chargement du stock initial impossible.", ex)
                MessageBox.Show("Impossible de charger le stock initial : " & ex.Message)
            End Try
        End Sub

        Private Sub ConfigurerColonnes()
            If grid.Columns.Contains("ProduitId") Then grid.Columns("ProduitId").Visible = False
            If grid.Columns.Contains("CategorieId") Then grid.Columns("CategorieId").Visible = False

            RenommerColonne("Libelle", "Produit")
            RenommerColonne("NomCategorie", "Catégorie")
            RenommerColonne("CodeBarres", "Référence / Code-barres")
            RenommerColonne("UnitePrincipale", "Unité principale")
            RenommerColonne("UniteSecondaire", "Unité secondaire")
            RenommerColonne("ConversionUnite", "Nb unités/base")
            RenommerColonne("QuantiteInitiale", "Quantité initiale")
            RenommerColonne("PrixAchatOptionnel", "Prix achat optionnel")
            RenommerColonne("PrixGros", "Prix Gros")
            RenommerColonne("PrixDemi", "Prix Demi")
            RenommerColonne("PrixQuart", "Prix Quart")
            RenommerColonne("PrixDetail", "Prix Pièce")
            RenommerColonne("PrixDouzaine", "Prix Douzaine")
            RenommerColonne("TypesPersonnalises", "Types personnalisés")
            RenommerColonne("DateInitiale", "Date initiale")
            RenommerColonne("EstActif", "Actif")
        End Sub

        Private Sub RenommerColonne(nom As String, titre As String)
            If grid.Columns.Contains(nom) Then
                grid.Columns(nom).HeaderText = titre
            End If
        End Sub

        Private Sub EnregistrerStockInitial(sender As Object, e As EventArgs)
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim produitService As New ProduitService(New ProduitRepository(dal))
            Dim stockService As New StockService(dal)
            Dim typeService As New TypeVenteProduitService()
            Dim lignesTraitees As Integer = 0

            Try
                For Each dgRow As DataGridViewRow In grid.Rows
                    If dgRow.IsNewRow Then
                        Continue For
                    End If

                    Dim quantiteInitiale As Decimal = SafeDecimal(dgRow.Cells("QuantiteInitiale").Value)
                    Dim libelle As String = SafeString(dgRow.Cells("Libelle").Value)
                    If quantiteInitiale <= 0D AndAlso libelle = String.Empty Then
                        Continue For
                    End If
                    If libelle = String.Empty Then
                        Continue For
                    End If

                    Dim produitId As Integer = SafeInteger(dgRow.Cells("ProduitId").Value)
                    Dim prixAchatOptionnel As Decimal = SafeDecimal(dgRow.Cells("PrixAchatOptionnel").Value)
                    Dim prixAchatExistant As Decimal = SafeDecimal(dgRow.Cells("PrixAchat").Value)
                    If prixAchatOptionnel <= 0D Then
                        prixAchatOptionnel = prixAchatExistant
                    End If

                    Dim produit As New Produit With {
                        .ProduitId = produitId,
                        .CodeBarres = SafeString(dgRow.Cells("CodeBarres").Value),
                        .Libelle = libelle,
                        .PrixAchat = prixAchatOptionnel,
                        .PrixGros = SafeDecimal(dgRow.Cells("PrixGros").Value),
                        .PrixDemi = SafeDecimal(dgRow.Cells("PrixDemi").Value),
                        .PrixQuart = SafeDecimal(dgRow.Cells("PrixQuart").Value),
                        .PrixDetail = SafeDecimal(dgRow.Cells("PrixDetail").Value),
                        .PrixDouzaine = SafeDecimal(dgRow.Cells("PrixDouzaine").Value),
                        .PrixSpecial = SafeDecimal(dgRow.Cells("PrixSpecial").Value),
                        .CoefficientGros = SafeDecimal(dgRow.Cells("CoefficientGros").Value),
                        .SeuilCritique = SafeDecimal(dgRow.Cells("SeuilCritique").Value),
                        .DateExpiration = SafeDate(dgRow.Cells("DateExpiration").Value),
                        .CategorieId = SafeNullableInteger(dgRow.Cells("CategorieId").Value),
                        .UnitePrincipale = If(SafeString(dgRow.Cells("UnitePrincipale").Value) = String.Empty, "Carton", SafeString(dgRow.Cells("UnitePrincipale").Value)),
                        .UniteSecondaire = If(SafeString(dgRow.Cells("UniteSecondaire").Value) = String.Empty, "Piece", SafeString(dgRow.Cells("UniteSecondaire").Value)),
                        .ConversionUnite = Math.Max(1D, SafeDecimal(dgRow.Cells("ConversionUnite").Value)),
                        .EstActif = SafeBoolean(dgRow.Cells("EstActif").Value, True),
                        .VenteDetail = SafeBoolean(dgRow.Cells("VenteDetail").Value, True),
                        .VenteDemi = SafeBoolean(dgRow.Cells("VenteDemi").Value, False),
                        .VenteDouzaine = SafeBoolean(dgRow.Cells("VenteDouzaine").Value, False),
                        .VenteGros = SafeBoolean(dgRow.Cells("VenteGros").Value, True)
                    }

                    If produit.PrixAchat < 0D Then
                        produit.PrixAchat = 0D
                    End If

                    If produitId > 0 Then
                        produitService.MettreAJour(produit)
                    Else
                        produitId = produitService.Ajouter(produit)
                    End If

                    If quantiteInitiale > 0D Then
                        stockService.EnregistrerEntree(produitId, quantiteInitiale, produit.UnitePrincipale, SafeString(dgRow.Cells("CodeBarres").Value), "Stock initial technique", SessionUtilisateur.UtilisateurId, produit.PrixAchat)
                    End If

                    Dim typesTexte As String = SafeString(dgRow.Cells("TypesPersonnalises").Value)
                    For Each dto As TypeVenteProduitDTO In ParserTypes(typesTexte, produitId)
                        typeService.Ajouter(dto)
                    Next

                    lignesTraitees += 1
                Next

                MessageBox.Show(lignesTraitees.ToString(CultureInfo.InvariantCulture) & " ligne(s) enregistrée(s).")
                Recharger(Nothing, EventArgs.Empty)
            Catch ex As Exception
                Dim log As New ProductionLogService()
                log.Error("FormulaireStockInitialTechnique", "EnregistrerStockInitial", "Enregistrement du stock initial impossible.", ex)
                MessageBox.Show("Impossible d'enregistrer le stock initial : " & ex.Message)
            End Try
        End Sub

        Private Function ParserTypes(texte As String, produitId As Integer) As IEnumerable(Of TypeVenteProduitDTO)
            Dim resultat As New List(Of TypeVenteProduitDTO)()
            If String.IsNullOrWhiteSpace(texte) Then
                Return resultat
            End If

            Dim blocs As String() = texte.Split(";"c)
            For Each bloc As String In blocs
                Dim morceaux As String() = bloc.Split("|"c)
                If morceaux.Length < 5 Then
                    Continue For
                End If

                Dim nom As String = morceaux(0).Trim()
                Dim quantite As Decimal = SafeDecimal(morceaux(1))
                Dim modePrix As String = morceaux(2).Trim().ToUpperInvariant()
                Dim coefficient As Decimal = SafeDecimal(morceaux(3))
                Dim prixVente As Decimal = SafeDecimal(morceaux(4))
                Dim actif As Boolean = True
                If morceaux.Length >= 6 Then
                    actif = SafeBoolean(morceaux(5), True)
                End If

                If nom = String.Empty OrElse quantite <= 0D OrElse prixVente <= 0D Then
                    Continue For
                End If

                Dim dto As New TypeVenteProduitDTO With {
                    .ProduitId = produitId,
                    .Nom = nom,
                    .QuantiteEquivalent = quantite,
                    .ModePrix = If(modePrix = String.Empty, "FIXE", modePrix),
                    .PrixVente = prixVente,
                    .Actif = actif,
                    .ModifiePar = If(String.IsNullOrWhiteSpace(SessionUtilisateur.NomUtilisateur), "SYSTEM", SessionUtilisateur.NomUtilisateur)
                }

                If String.Equals(dto.ModePrix, "COEFFICIENT", StringComparison.OrdinalIgnoreCase) AndAlso coefficient > 0D Then
                    dto.Coefficient = coefficient
                End If

                resultat.Add(dto)
            Next

            Return resultat
        End Function

        Private Function SafeString(value As Object) As String
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return String.Empty
            End If
            Return Convert.ToString(value).Trim()
        End Function

        Private Function SafeDecimal(value As Object) As Decimal
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return 0D
            End If

            Dim texte As String = Convert.ToString(value).Trim().Replace(",", ".")
            Dim resultat As Decimal
            If Decimal.TryParse(texte, NumberStyles.Any, CultureInfo.InvariantCulture, resultat) Then
                Return resultat
            End If
            If Decimal.TryParse(texte, NumberStyles.Any, CultureInfo.CurrentCulture, resultat) Then
                Return resultat
            End If
            Return 0D
        End Function

        Private Function SafeInteger(value As Object) As Integer
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return 0
            End If
            Dim resultat As Integer
            If Integer.TryParse(Convert.ToString(value), resultat) Then
                Return resultat
            End If
            Return 0
        End Function

        Private Function SafeNullableInteger(value As Object) As Integer?
            Dim resultat As Integer = SafeInteger(value)
            If resultat <= 0 Then
                Return Nothing
            End If
            Return resultat
        End Function

        Private Function SafeDate(value As Object) As Date?
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return Nothing
            End If
            Dim resultat As Date
            If Date.TryParse(Convert.ToString(value), resultat) Then
                Return resultat
            End If
            Return Nothing
        End Function

        Private Function SafeBoolean(value As Object, defaultValue As Boolean) As Boolean
            If value Is Nothing OrElse Convert.IsDBNull(value) Then
                Return defaultValue
            End If

            Dim texte As String = Convert.ToString(value).Trim()
            Dim resultat As Boolean
            If Boolean.TryParse(texte, resultat) Then
                Return resultat
            End If

            If String.Equals(texte, "1", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            If String.Equals(texte, "0", StringComparison.OrdinalIgnoreCase) Then
                Return False
            End If
            Return defaultValue
        End Function
    End Class
End Namespace
