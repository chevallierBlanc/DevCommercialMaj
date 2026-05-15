Option Strict On
Option Explicit On

Imports System
Imports System.Configuration
Imports System.Windows.Forms

Namespace DevCommerc8ak
    Public Class FormulaireParametres
        Inherits Form

        Private ReadOnly txtRemiseMax As TextBox
        Private ReadOnly txtSeuilStock As TextBox
        Private ReadOnly txtAlerteJours As TextBox
        Private ReadOnly cmbImprimanteA4 As ComboBox
        Private ReadOnly cmbImprimanteTicket As ComboBox
        Private ReadOnly chkApercu As CheckBox
        Private ReadOnly chkCouleur As CheckBox
        Private ReadOnly cmbDevise As ComboBox
        Private ReadOnly txtTauxUsd As TextBox
        Private ReadOnly txtScannerIp As TextBox
        Private ReadOnly txtScannerPort As TextBox
        Private ReadOnly chkScannerActif As CheckBox
        Private ReadOnly txtNomMagasin As TextBox
        Private ReadOnly txtAdresseMagasin As TextBox
        Private ReadOnly txtTelephoneMagasin As TextBox
        Private ReadOnly chkModeSombre As CheckBox
        Private ReadOnly txtLogoPath As TextBox
        Private ReadOnly btnLogo As Button
        Private ReadOnly btnCharger As Button
        Private ReadOnly btnEnregistrer As Button

        Public Sub New()
            Me.Text = "Parametres"
            Me.Width = 900
            Me.Height = 600

            Dim tabs As New TabControl() With {.Dock = DockStyle.Fill}

            Dim tabGeneral As New TabPage("General")
            Dim tabProduits As New TabPage("Produits")
            Dim tabImprimantes As New TabPage("Imprimantes")
            Dim tabMonnaie As New TabPage("Monnaie")
            Dim tabScan As New TabPage("Scan")

            txtNomMagasin = New TextBox() With {.Left = 20, .Top = 30, .Width = 300}
            txtAdresseMagasin = New TextBox() With {.Left = 20, .Top = 80, .Width = 400}
            txtTelephoneMagasin = New TextBox() With {.Left = 20, .Top = 130, .Width = 200}
            chkModeSombre = New CheckBox() With {.Left = 20, .Top = 180, .Text = "Mode sombre"}
            txtLogoPath = New TextBox() With {.Left = 20, .Top = 230, .Width = 400}
            btnLogo = New Button() With {.Text = "Choisir logo", .Left = 430, .Top = 228, .Width = 120}
            AddHandler btnLogo.Click, AddressOf ChoisirLogo

            tabGeneral.Controls.Add(New Label() With {.Text = "Nom magasin", .Left = 20, .Top = 10, .AutoSize = True})
            tabGeneral.Controls.Add(New Label() With {.Text = "Adresse", .Left = 20, .Top = 60, .AutoSize = True})
            tabGeneral.Controls.Add(New Label() With {.Text = "Telephone", .Left = 20, .Top = 110, .AutoSize = True})
            tabGeneral.Controls.Add(txtNomMagasin)
            tabGeneral.Controls.Add(txtAdresseMagasin)
            tabGeneral.Controls.Add(txtTelephoneMagasin)
            tabGeneral.Controls.Add(chkModeSombre)
            tabGeneral.Controls.Add(New Label() With {.Text = "Logo", .Left = 20, .Top = 210, .AutoSize = True})
            tabGeneral.Controls.Add(txtLogoPath)
            tabGeneral.Controls.Add(btnLogo)

            txtRemiseMax = New TextBox() With {.Left = 20, .Top = 30, .Width = 100}
            txtSeuilStock = New TextBox() With {.Left = 20, .Top = 80, .Width = 100}
            txtAlerteJours = New TextBox() With {.Left = 20, .Top = 130, .Width = 100}

            tabProduits.Controls.Add(New Label() With {.Text = "Remise max (%)", .Left = 20, .Top = 10, .AutoSize = True})
            tabProduits.Controls.Add(New Label() With {.Text = "Seuil stock", .Left = 20, .Top = 60, .AutoSize = True})
            tabProduits.Controls.Add(New Label() With {.Text = "Alerte exp (jours)", .Left = 20, .Top = 110, .AutoSize = True})
            tabProduits.Controls.Add(txtRemiseMax)
            tabProduits.Controls.Add(txtSeuilStock)
            tabProduits.Controls.Add(txtAlerteJours)

            cmbImprimanteA4 = New ComboBox() With {.Left = 20, .Top = 30, .Width = 340}
            cmbImprimanteTicket = New ComboBox() With {.Left = 20, .Top = 80, .Width = 340}
            chkApercu = New CheckBox() With {.Left = 20, .Top = 130, .Text = "Apercu avant impression"}
            chkCouleur = New CheckBox() With {.Left = 20, .Top = 160, .Text = "Impression couleur"}

            tabImprimantes.Controls.Add(New Label() With {.Text = "Imprimante A4", .Left = 20, .Top = 10, .AutoSize = True})
            tabImprimantes.Controls.Add(New Label() With {.Text = "Imprimante thermique", .Left = 20, .Top = 60, .AutoSize = True})
            tabImprimantes.Controls.Add(cmbImprimanteA4)
            tabImprimantes.Controls.Add(cmbImprimanteTicket)
            tabImprimantes.Controls.Add(chkApercu)
            tabImprimantes.Controls.Add(chkCouleur)

            cmbDevise = New ComboBox() With {.Left = 20, .Top = 30, .Width = 100}
            cmbDevise.Items.AddRange(New Object() {"FC", "USD"})
            txtTauxUsd = New TextBox() With {.Left = 20, .Top = 80, .Width = 100}

            tabMonnaie.Controls.Add(New Label() With {.Text = "Devise par defaut", .Left = 20, .Top = 10, .AutoSize = True})
            tabMonnaie.Controls.Add(New Label() With {.Text = "Taux USD (FC)", .Left = 20, .Top = 60, .AutoSize = True})
            tabMonnaie.Controls.Add(cmbDevise)
            tabMonnaie.Controls.Add(txtTauxUsd)

            txtScannerIp = New TextBox() With {.Left = 20, .Top = 30, .Width = 140}
            txtScannerPort = New TextBox() With {.Left = 20, .Top = 80, .Width = 80}
            chkScannerActif = New CheckBox() With {.Left = 20, .Top = 120, .Text = "Scanner actif"}

            tabScan.Controls.Add(New Label() With {.Text = "IP scanner", .Left = 20, .Top = 10, .AutoSize = True})
            tabScan.Controls.Add(New Label() With {.Text = "Port", .Left = 20, .Top = 60, .AutoSize = True})
            tabScan.Controls.Add(txtScannerIp)
            tabScan.Controls.Add(txtScannerPort)
            tabScan.Controls.Add(chkScannerActif)

            tabs.TabPages.Add(tabGeneral)
            tabs.TabPages.Add(tabProduits)
            tabs.TabPages.Add(tabImprimantes)
            tabs.TabPages.Add(tabMonnaie)
            tabs.TabPages.Add(tabScan)

            btnCharger = New Button() With {.Text = "Charger", .Left = 20, .Top = 500, .Width = 100}
            btnEnregistrer = New Button() With {.Text = "Enregistrer", .Left = 140, .Top = 500, .Width = 100}
            AddHandler btnCharger.Click, AddressOf Charger
            AddHandler btnEnregistrer.Click, AddressOf Enregistrer

            Me.Controls.Add(btnCharger)
            Me.Controls.Add(btnEnregistrer)
            Me.Controls.Add(tabs)

            ChargerImprimantes()
            ThemeHelper.AppliquerTheme(Me)
        End Sub

        Private Sub ChargerImprimantes()
            cmbImprimanteA4.Items.Clear()
            cmbImprimanteTicket.Items.Clear()
            For Each p As String In System.Drawing.Printing.PrinterSettings.InstalledPrinters
                cmbImprimanteA4.Items.Add(p)
                cmbImprimanteTicket.Items.Add(p)
            Next
        End Sub

        Private Function ObtenirService() As ParametreService
            Dim cs As String = ConfigurationManager.ConnectionStrings("CommercialMagDB").ConnectionString
            Dim dal As New DAL(cs)
            Dim repo As New ParametreRepository(dal)
            Return New ParametreService(repo)
        End Function

        Private Sub Charger(sender As Object, e As EventArgs)
            Try
                Dim service As ParametreService = ObtenirService()
                Dim p As ParametreDTO = service.Charger()
                If p Is Nothing Then Return

                txtRemiseMax.Text = p.RemiseMaxPourcent.ToString()
                txtSeuilStock.Text = p.SeuilStockCritique.ToString()
                txtAlerteJours.Text = p.AlerteExpirationJours.ToString()
                cmbImprimanteA4.Text = p.ImprimanteA4
                cmbImprimanteTicket.Text = p.ImprimanteTicket
                cmbDevise.SelectedItem = p.DeviseParDefaut
                txtTauxUsd.Text = p.TauxUsd.ToString()
                txtScannerIp.Text = p.ScannerIp
                txtScannerPort.Text = p.ScannerPort.ToString()
                chkScannerActif.Checked = p.ScannerActif
                txtNomMagasin.Text = p.NomMagasin
                txtAdresseMagasin.Text = p.AdresseMagasin
                txtTelephoneMagasin.Text = p.TelephoneMagasin
                chkModeSombre.Checked = p.ModeSombre
                txtLogoPath.Text = p.LogoPath
                chkApercu.Checked = p.ApercuAvantImpression
                chkCouleur.Checked = p.ImpressionCouleur
            Catch ex As Exception
                MessageBox.Show("Erreur chargement parametres: " & ex.Message)
            End Try
        End Sub

        Private Sub Enregistrer(sender As Object, e As EventArgs)
            Try
                Dim service As ParametreService = ObtenirService()
                Dim p As New ParametreDTO With {
                    .RemiseMaxPourcent = Decimal.Parse(If(txtRemiseMax.Text.Trim() = "", "0", txtRemiseMax.Text.Trim())),
                    .SeuilStockCritique = Decimal.Parse(If(txtSeuilStock.Text.Trim() = "", "0", txtSeuilStock.Text.Trim())),
                    .AlerteExpirationJours = Convert.ToInt32(If(txtAlerteJours.Text.Trim() = "", "30", txtAlerteJours.Text.Trim())),
                    .ImprimanteA4 = cmbImprimanteA4.Text,
                    .ImprimanteTicket = cmbImprimanteTicket.Text,
                    .DeviseParDefaut = If(cmbDevise.SelectedItem Is Nothing, "FC", cmbDevise.SelectedItem.ToString()),
                    .TauxUsd = Decimal.Parse(If(txtTauxUsd.Text.Trim() = "", "0", txtTauxUsd.Text.Trim())),
                    .ScannerIp = txtScannerIp.Text.Trim(),
                    .ScannerPort = Convert.ToInt32(If(txtScannerPort.Text.Trim() = "", "9000", txtScannerPort.Text.Trim())),
                    .ScannerActif = chkScannerActif.Checked,
                    .NomMagasin = txtNomMagasin.Text.Trim(),
                    .AdresseMagasin = txtAdresseMagasin.Text.Trim(),
                    .TelephoneMagasin = txtTelephoneMagasin.Text.Trim(),
                    .ModeSombre = chkModeSombre.Checked,
                    .LogoPath = txtLogoPath.Text.Trim(),
                    .ApercuAvantImpression = chkApercu.Checked,
                    .ImpressionCouleur = chkCouleur.Checked
                }
                service.Enregistrer(p)
                MessageBox.Show("Parametres enregistres.")
            Catch ex As Exception
                MessageBox.Show("Erreur enregistrement parametres: " & ex.Message)
            End Try
        End Sub

        Private Sub ChoisirLogo(sender As Object, e As EventArgs)
            Dim ofd As New OpenFileDialog() With {.Filter = "Images|*.png;*.jpg;*.jpeg;*.ico"}
            If ofd.ShowDialog() = DialogResult.OK Then
                txtLogoPath.Text = ofd.FileName
            End If
        End Sub
    End Class
End Namespace
