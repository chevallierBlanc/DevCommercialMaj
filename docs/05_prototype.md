# Prototype WinForms (Spec technique ecrans)

Note: l'environnement ne contient pas .NET. Ce prototype est une specification de layout + noms de controles pour implementation directe.

## 1. Theme
- Couleurs: BleuFonce #1E2A44, Gris #F2F4F7, GrisFonce #5B6573, Blanc #FFFFFF
- Police: Segoe UI 10/11

## 2. Composants communs
- SidebarPanel
- TopBarPanel
- ContentPanel
- PrimaryButton, SecondaryButton, DangerButton
- DataGridView stylisee
- SearchTextBox (avec icon loupe)

## 3. FormLogin
Controles
- txtUserName
- txtPassword
- btnLogin
- lblServerStatus

## 4. FormFacturation
Zones
1. Header
- txtSearchProduct
- btnScan
- cmbScanMode (USB/Smartphone)
- lblScanStatus

2. Results
- gridProducts

3. Cart
- gridCart
- txtDiscountPercent
- txtTaxPercent
- lblSubTotal
- lblTotal
- cmbCustomer
- cmbPaymentMode
- btnValidateInvoice
- btnHoldInvoice
- btnPrintA4

## 5. FormCaisse
- gridPendingInvoices
- lblAmount
- cmbPaymentMode
- txtPaymentRef
- btnValidatePayment
- btnPrintTicket

## 6. FormAdminDashboard
- kpiCards (CA jour, CA mois, Marge, Stock critique, Valeur stock)
- chartSales
- chartTopProducts
- btnProducts
- btnStock
- btnSuppliers
- btnCustomers

## 7. FormProduits
- gridProducts
- btnNew
- btnEdit
- btnImport
- btnExport
- btnPriceHistory
- txtSearch
- cmbCategory
- numStockBelow

## 8. FormStock
- tabsStock
- tabEntry
- tabExit
- tabLoss
- tabInventory
- tabAlerts

## 9. FormRapports
- cmbReportType
- dtStart
- dtEnd
- btnExportPdf
- btnExportExcel
- gridReport

## 10. FormParametres (nouveau)
- grpBarcode
- chkEnableMobileScanner
- txtScannerIp
- txtScannerPort
- cmbBarcodeFormat
- numScanTimeout

## 10. Navigation
- Sidebar contient liens par role
- Chaque form herite d'un BaseForm pour theme et header
