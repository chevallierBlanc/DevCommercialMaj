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
foreach ($name in @('AutoriserSortie','SelectionnerFacture','ChargerFactures','ChargerDetails','ChargerLignes','ConvertirMontant','CalculerMonnaie','Encaisser','ConstruireTicketDepuisSelection','ImprimerTicketsDistincts','GetLibelleTypeVentePourTicket')) {
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
Assert-Contract ($renderer -match 'If Not data.ProformaThermique Then' -and $renderer -match 'doc.BeginPrint') 'Paiements exclus proforma et pagination reinitialisee'
$oldAdmin = Read-Baseline 'Forms/AdminForm.vb'
$infrastructure = Method (Read-Baseline 'Repositories/SuperAdminRepository.vb') 'AssurerInfrastructure'
$cards = [regex]::Matches($oldAdmin,'If VerifierPermission\(').Count
$infraCommands = 1 + [regex]::Matches($infrastructure,'            AssurerRole\(').Count + [regex]::Matches($infrastructure,'            AssurerInterface\(').Count
Assert-Contract ($cards -eq 13 -and $infraCommands -eq 21 -and $cards * (3 * $infraCommands + 2) -eq 845) 'Comptage statique ancien parcours valide : 845 commandes'
Write-Host ('PASS : ' + $script:checks + ' contrats de source. Aucun essai Windows, SQL ou impression physique.')
