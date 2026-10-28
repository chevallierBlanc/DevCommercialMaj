#requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$prefix = 'prototype/WinFormsVB/DevCommerc8ak/'
$script:checks = 0
function Assert-Contract($Ok, [string]$Message) {
    if (-not $Ok) { throw $Message }; $script:checks++
}
function Read-Current([string]$Path) {
    return [IO.File]::ReadAllText((Join-Path $root ($prefix + $Path))).Replace("`r",'')
}
function Read-Baseline([string]$Path) {
    $lines = @(& git -C $root show ('0a91bfd:' + $prefix + $Path))
    if ($LASTEXITCODE -ne 0) { throw 'Historique Git 0a91bfd requis pour comparer les contrats avant/apres.' }
    return ($lines -join "`n").Replace("`r",'')
}
function Method([string]$Text, [string]$Name) {
    $pattern = '(?ms)^        (?:Public|Private|Protected)(?: Shared)?(?: Async)? (Sub|Function) ' + [regex]::Escape($Name) + '\(.*?^        End \1'
    $m = [regex]::Match($Text,$pattern)
    if (-not $m.Success) { throw ('Methode introuvable : ' + $Name) }
    return $m.Value
}
$renderer = Read-Current 'Services/FacturePrintRenderer.vb'
$baseline = Read-Baseline 'Services/FacturePrintRenderer.vb'
foreach ($name in @('CreerDocumentA4','DessinerA4','DessinerEnteteEntrepriseA4','DessinerEnteteTable','MesurerHauteurLigne','DessinerLigneA4','DessinerTexteCentre','DessinerLibelleValeur','DessinerTotalA4','DessinerFooterA4','TitreDocument','EstProforma','EstAnnulee','StatutAffichage','FormatMontant','FormaterQuantite','Safe')) {
    Assert-Contract ((Method $renderer $name) -ceq (Method $baseline $name)) ('A4/helper historique inchange : ' + $name)
}
$facturation = Read-Current 'Forms/FacturationForm.vb'; $oldFacturation = Read-Baseline 'Forms/FacturationForm.vb'
foreach ($name in @('ValiderFacture','ImprimerProformaA4','ExporterPdf','CalculerSousTotalPanier','CalculerRemiseMontant')) {
    Assert-Contract ((Method $facturation $name) -ceq (Method $oldFacturation $name)) ('Facturation inchangee : ' + $name)
}
$caisse = Read-Current 'Forms/CaisseForm.vb'; $oldCaisse = Read-Baseline 'Forms/CaisseForm.vb'
foreach ($name in @('AutoriserSortie','SelectionnerFacture','ChargerFactures','ChargerDetails','ChargerLignes','ConvertirMontant','CalculerMonnaie')) {
    Assert-Contract ((Method $caisse $name) -ceq (Method $oldCaisse $name)) ('Caisse metier inchangee : ' + $name)
}
$historique = Read-Current 'UI/FormulaireFactures.vb'; $oldHistorique = Read-Baseline 'UI/FormulaireFactures.vb'
$a4 = '(?ms)                Dim data As FacturePrintData = ConstruireDonneesFacture.*?^            Catch ex As Exception'
Assert-Contract ([regex]::Match($historique,$a4).Value -ceq [regex]::Match($oldHistorique,$a4).Value) 'Branche A4 historique strictement conservee'
$admin = Read-Current 'Forms/AdminForm.vb'
Assert-Contract ($admin -notmatch 'VerifierPermission\(' -and $admin -notmatch 'AssurerInfrastructure\(') 'Aucun controle unitaire/DDL Administration'
Assert-Contract ($admin -match 'Await repo.ListerCodesInterfacesAutoriseesAsync' -and $admin -match '_annulation.Cancel\(') 'Lecture async et annulation'
Assert-Contract ($admin -match 'session <> SessionUtilisateur.SessionId' -and $admin -match 'roleId <> SessionUtilisateur.RoleIdActif') 'Resultat de session obsolete rejete'
$repo = Read-Current 'Repositories/SuperAdminRepository.vb'
$async = Method $repo 'ListerCodesInterfacesAutoriseesAsync'
Assert-Contract ($async -notmatch '(?i)AssurerInfrastructure|ALTER TABLE|INSERT INTO|DELETE FROM|UPDATE dbo') 'Lecture groupee sans ecriture'
Assert-Contract ($async -match 'r.NomRole=@role' -and $async -match 'i.EstActif=1' -and $async -match 'ISNULL\(r.EstActif,1\)=1') 'Filtres RBAC actifs conserves'
Assert-Contract ($caisse -notmatch 'Sub ImprimerPageTicket' -and $caisse -match 'FacturePrintRenderer.CreerDocumentThermique') 'Caisse utilise le renderer commun sans dessin inline'
Assert-Contract ($facturation -match 'FormChoixImpressionDocument' -and $historique -match 'FormChoixImpressionDocument') 'Dialogue partage'
Assert-Contract ($renderer -match 'If Not data.ProformaThermique AndAlso Not data.DiagnosticThermique Then' -and $renderer -match 'doc.BeginPrint') 'Paiements exclus proforma/diagnostic et pagination reinitialisee'
$oldAdmin = Read-Baseline 'Forms/AdminForm.vb'
$infrastructure = Method (Read-Baseline 'Repositories/SuperAdminRepository.vb') 'AssurerInfrastructure'
$cards = [regex]::Matches($oldAdmin,'If VerifierPermission\(').Count
$infraCommands = 1 + [regex]::Matches($infrastructure,'            AssurerRole\(').Count + [regex]::Matches($infrastructure,'            AssurerInterface\(').Count
Assert-Contract ($cards -eq 13 -and $infraCommands -eq 21 -and $cards * (3 * $infraCommands + 2) -eq 845) 'Comptage statique ancien parcours valide : 845 commandes'

# Contrats localises de cette correction, compares a la livraison precedente.
function Read-Previous([string]$Path) {
    $lines = @(& git -C $root show ('fcb041b:' + $prefix + $Path))
    if ($LASTEXITCODE -ne 0) { throw 'Historique Git fcb041b requis.' }
    return ($lines -join "`n").Replace("`r",'')
}
$users = Read-Current 'UI/FormulaireUtilisateurs.vb'
$previousUsers = Read-Previous 'UI/FormulaireUtilisateurs.vb'
foreach ($name in @('Charger','ChargerConnectes','ChargerSelectionUtilisateur','Ajouter','Modifier','DesactiverUtilisateur','ResetMdp','DeverrouillerUtilisateur','SupprimerUtilisateur')) {
    Assert-Contract ((Method $users $name) -ceq (Method $previousUsers $name)) ('Utilisateurs/session metier inchange : ' + $name)
}
Assert-Contract ($users -match 'Orientation = Orientation.Horizontal' -and $users -match 'volets.Panel2.Controls.Add\(p2\)' -and $users -match 'Panel2MinSize = 160') 'Volet de sessions distinct et redimensionnable'
Assert-Contract ($users -match 'Interval = 5000' -and $users -match 'gridConnectes.DataBindingComplete') 'Frequence et adaptation des colonnes conservees'
$requests = Read-Current 'UI/CartesSecuriteAutorisations.vb'
Assert-Contract ($requests -match 'ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True' -and $requests -match 'ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize') 'Entetes demandes multilignes et hauteur automatique'
Assert-Contract ($requests -match 'colonne.MinimumWidth = 125' -and $requests -match 'grille.ScrollBars = ScrollBars.Both') 'Largeurs minimales et defilement horizontal'
$printing = Read-Current 'Services/PrintConfigurationHelper.vb'
Assert-Contract ((Method $printing 'TesterImprimanteA4') -ceq (Method (Read-Previous 'Services/PrintConfigurationHelper.vb') 'TesterImprimanteA4')) 'Test A4 strictement inchange'
$test = Method $printing 'TesterImprimanteThermique'
Assert-Contract ($test -match 'FacturePrintRenderer.CreerDocumentThermique' -and $test -match 'FormApercuTicket' -and $test -match 'param.ApercuAvantImpression') 'Test utilise le renderer et respecte apercu'
Assert-Contract ($test -notmatch 'PrintPage|DrawString|SqlCommand|Repository|FactureService|PaiementService') 'Aucun rendu concurrent ni ecriture metier dans le test'
$fixture = Method $printing 'CreerDonneesTestThermique'
Assert-Contract ($fixture -notmatch 'Charger|Repository|Service|Sql|SessionUtilisateur' -and $fixture -match 'DiagnosticThermique = True') 'Donnees fictives autonomes sans lecture commerciale'
foreach ($name in @('ConfigurerDocumentThermique','ChargerParametres','AppliquerImprimantesLocales','ResoudreImprimante')) {
    Assert-Contract ((Method $printing $name) -ceq (Method (Read-Previous 'Services/PrintConfigurationHelper.vb') $name)) ('Configuration imprimantes conservee : ' + $name)
}
foreach ($name in @('TraiterBlocThermique','DessinerPageThermique')) {
    Assert-Contract ((Method $renderer $name) -ceq (Method (Read-Previous 'Services/FacturePrintRenderer.vb') $name)) ('Geometrie thermique conservee : ' + $name)
}
$justificatif = Read-Current 'Services/JustificatifCaisseService.vb'
$lecture = Method $justificatif 'Lire'
Assert-Contract ($lecture -match 'AutorisationActionService.Exiger\(cn, tx, "ENCAISSEMENT_CREER", "CAISSE"\)' -and $lecture -match 'JustificatifCaisseRegles.Verifier') 'Justificatif controle cote service et paiement persiste'
Assert-Contract ($lecture -match 'IsolationLevel.Serializable' -and $lecture -match 'tx.Commit\(' -and $lecture -match 'propreEncaissement') 'Lecture coherente et portee de reimpression'
$repoImpression = Read-Current 'Repositories/FactureImpressionRepository.vb'
Assert-Contract ((Method $repoImpression 'LireJustificatif') -notmatch '(?i)NOLOCK|INSERT|UPDATE|DELETE|ALTER|EXEC ') 'Lecture justificatif sans DML ni lecture sale'
Assert-Contract ($caisse -notmatch 'ConstruireTicketDepuisSelection|TicketData' -and $caisse -match 'New JustificatifCaisseService\(dal\).Lire\(ticket\)') 'Aucun recu construit depuis des champs temporaires'
Assert-Contract ((Method $caisse 'CreerDocumentTicket') -notmatch 'txtMontantRecu|cmbMode|txtReference|Encaisser') 'Impression separee de la saisie et de la finance'
Assert-Contract ($caisse -match 'btnImprimer.Enabled = Not _encaissementEnCours AndAlso _derniereFactureEncaissee > 0') 'Bouton indisponible sans encaissement confirme localement'
foreach ($path in @('Services/FacturationService.vb','Services/AutorisationActionService.vb','Services/StockUnitConversionService.vb','Repositories/PaiementRepository.vb')) {
    $previous = @(& git -C $root show ('a91dca8:' + $prefix + $path)) -join "`n"
    if ($LASTEXITCODE -ne 0) { throw ('Baseline introuvable : ' + $path) }
    Assert-Contract ((Read-Current $path).TrimEnd() -ceq $previous.Replace("`r",'').TrimEnd()) ('Moteur sensible inchange : ' + $path)
}
$operations = Read-Current 'Services/FactureOperationService.vb'
$avantOperations = (@(& git -C $root show ('a91dca8:' + $prefix + 'Services/FactureOperationService.vb')) -join "`n").Replace("`r",'')
$champsImpression = ',p.UnitePrincipale AS UnitePrincipaleImpression,p.UniteSecondaire AS UniteSecondaireImpression'
Assert-Contract ($operations.Replace($champsImpression,'').TrimEnd() -ceq $avantOperations.TrimEnd()) 'Seuls deux champs descriptifs sont ajoutes au snapshot existant, sans changer les operations'
Assert-Contract ($historique -notmatch 'Règlement #' -and $historique -match '"Règlement", montant') 'Identifiant de paiement non expose'
Assert-Contract ($renderer -match '\.Texte = "STATUT".*\.Police = 1' -and $renderer -match 'ConditionnementCommercial.Libelle') 'Statut gras et libelle thermique commun'
Write-Host ('PASS : ' + $script:checks + ' contrats de source. Aucun essai Windows, SQL ou impression physique.')
