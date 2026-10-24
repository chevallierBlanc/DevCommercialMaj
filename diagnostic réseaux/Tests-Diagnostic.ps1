#requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'DiagnosticCommun.ps1')
$script:n = 0
function Assert-ERP($Ok, [string]$Message) {
    if (-not $Ok) { throw $Message }; $script:n++
}
foreach ($file in @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1')) {
    $tokens = $null; $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$tokens, [ref]$errors)
    Assert-ERP ($errors.Count -eq 0) ('Syntaxe : ' + $file.Name)
    $commands = @($ast.FindAll({param($a) $a -is [Management.Automation.Language.CommandAst]}, $true) | ForEach-Object { $_.GetCommandName() })
    Assert-ERP (@($commands | Where-Object { $_ -match '^(Restart-Service|Stop-Service|Start-Service|Set-Net|New-Net|Remove-Net|Set-ItemProperty|Invoke-Sqlcmd)$' }).Count -eq 0) ('Pas de commande de reconfiguration : ' + $file.Name)
}
# Verifier les exemples sans executer les collectes Windows ou demander de secret.
$readme = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Raw
$examples = 0
foreach ($block in [regex]::Matches($readme, '```powershell\s*([\s\S]*?)```')) {
    $tokens = $null; $errors = $null
    $ast = [Management.Automation.Language.Parser]::ParseInput($block.Groups[1].Value, [ref]$tokens, [ref]$errors)
    foreach ($call in @($ast.FindAll({param($a) $a -is [Management.Automation.Language.CommandAst]}, $true))) {
        $name = $call.GetCommandName()
        if ($name -notmatch '(^|[\\/])(DiagnosticReseauERP|SyntheseDiagnosticERP|Tests-Diagnostic)\.ps1$') { continue }
        $scriptName = ($name -split '[\\/]')[-1]
        $metadata = [Management.Automation.CommandMetadata]::new((Get-Command -Name (Join-Path $PSScriptRoot $scriptName)))
        $examples++
        foreach ($element in $call.CommandElements) {
            if ($element -isnot [Management.Automation.Language.CommandParameterAst]) { continue }
            Assert-ERP ($metadata.Parameters.ContainsKey($element.ParameterName)) ('Parametre README reconnu : ' + $scriptName + ' -' + $element.ParameterName)
        }
    }
}
Assert-ERP ($examples -eq 7) 'Sept commandes de scripts du README controlees sans execution'
Import-ERPProbe
$e = [CommercialPro.Diagnostics.SqlNetworkProbe]::ParseEndpoint('tcp:192.0.2.10,1533', 0)
Assert-ERP ($e.Host -eq '192.0.2.10' -and $e.Port -eq 1533) 'Port configure respecte'
$thrown = $false
try { [void][CommercialPro.Diagnostics.SqlNetworkProbe]::ParseEndpoint('serveur', 0) } catch { $thrown = $true }
Assert-ERP $thrown 'Aucun port par defaut suppose'
Assert-ERP ([CommercialPro.Diagnostics.SqlNetworkProbe]::ClassifySql(18456,$true) -eq 'AUTHENTIFICATION') 'Refus distinct du TCP'
Assert-ERP ([CommercialPro.Diagnostics.SqlNetworkProbe]::ClassifySql(-2,$false) -eq 'DELAI_COMMANDE') 'Timeout requete distinct de ouverture'
Assert-ERP ((Get-ERPStart '2026-10-23T12:00:00Z').Offset -eq [TimeSpan]::Zero) 'Depart UTC'
$thrown = $false; try { Get-ERPStart '2026-10-23T12:00:00' } catch { $thrown = $true }
Assert-ERP $thrown 'Heure ambigue refusee'
$ctx = @{ Campagne = 'TEST'; Execution = 'run'; Poste = 'POSTE_A'; Mode = 'CLIENT'; Cycle = 'cycle' }
$state = @{}
$a = New-ERPRecord $ctx 'MESURE' 'TCP' '192.0.2.10' 'SUCCES' 1 $null
[void](Update-ERPIncident $state $a 0)
$a = New-ERPRecord $ctx 'MESURE' 'TCP' '192.0.2.10' 'ECHEC' 1 $null
[void](Update-ERPIncident $state $a 10)
Assert-ERP ($a.Transition -eq 'INTERRUPTION') 'Debut incident'
$id = $a.IncidentId
$a = New-ERPRecord $ctx 'MESURE' 'TCP' '192.0.2.10' 'IGNORE' 1 $null
[void](Update-ERPIncident $state $a 15)
Assert-ERP ($null -ne $state['TCP|192.0.2.10'].Active) 'Inconnu ne termine pas incident'
$a = New-ERPRecord $ctx 'MESURE' 'TCP' '192.0.2.10' 'SUCCES' 1 $null
$incident = Update-ERPIncident $state $a 40
Assert-ERP ($incident.DureeObserveeSecondes -eq 30 -and $incident.IncidentId -eq $id -and -not $incident.CensureDebut) 'Duree monotone et identifiant conserves'
$a = New-ERPRecord $ctx 'MESURE' 'SERVICE_SQL' 'MSSQLSERVER' 'ECHEC' 1 $null
[void](Update-ERPIncident $state $a 50)
$open = @(Close-ERPIncidents $state $ctx 80)
Assert-ERP ($open.Count -eq 1 -and $open[0].CensureDebut -and $open[0].CensureFin -and $open[0].DureeObserveeSecondes -eq 30) 'Incident ouvert et debut inconnu explicites'
$left = [pscustomobject]@{ Campagne='TEST'; Poste='A'; Etape='TCP'; Cible='SQL'; IncidentId='1'; DebutIncidentUtc='2026-10-23T12:00:00Z'; FinIncidentUtc='2026-10-23T12:01:00Z' }
$right = $left.PSObject.Copy(); $right.Poste='B'; $right.DebutIncidentUtc='2026-10-23T12:00:30Z'
Assert-ERP ((Get-ERPOverlap $left $right 0).ChevauchementSecondes -eq 30) 'Chevauchement interpostes mesure'
$right.DebutIncidentUtc='2026-10-23T12:01:05Z'; $right.FinIncidentUtc='2026-10-23T12:02:00Z'
Assert-ERP ($null -eq (Get-ERPOverlap $left $right 0)) 'Pas de faux chevauchement'
Assert-ERP ((Get-ERPOverlap $left $right 5).Qualification -eq 'PROXIMITE_TEMPORELLE') 'Tolerance non presentee comme simultaneite'
$right.Campagne='AUTRE'
Assert-ERP ($null -eq (Get-ERPOverlap $left $right 120)) 'Campagnes non melangees'
$json = [IO.StringWriter]::new(); $csv = [IO.StringWriter]::new()
$writers = @{ Json=$json; Csv=$csv; Header=$false }
Write-ERPRecord $writers $a; Write-ERPRecord $writers $a
Assert-ERP (@($csv.ToString() -split "`n" | Where-Object { $_.Trim() }).Count -eq 3) 'CSV une seule entete'
Assert-ERP (@($json.ToString() -split "`n" | Where-Object { $_.Trim() }).Count -eq 2) 'JSONL ligne par evenement'
$listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,0)
$listener.Start(); $port = ([Net.IPEndPoint]$listener.LocalEndpoint).Port
try {
    $s = [CommercialPro.Diagnostics.SqlNetworkProbe]::TcpAsync('127.0.0.1',$port,1000).GetAwaiter().GetResult()
    Assert-ERP ($s.Status -eq 'SUCCES') 'TCP local ouvert'
    $listener.Stop()
    $s = [CommercialPro.Diagnostics.SqlNetworkProbe]::TcpAsync('127.0.0.1',$port,1000).GetAwaiter().GetResult()
    Assert-ERP ($s.Status -eq 'ECHEC') 'TCP local ferme'
    $listener.Start(); $port = ([Net.IPEndPoint]$listener.LocalEndpoint).Port
    $s = [CommercialPro.Diagnostics.SqlNetworkProbe]::TcpAsync('127.0.0.1',$port,1000).GetAwaiter().GetResult()
    Assert-ERP ($s.Status -eq 'SUCCES') 'TCP local retabli'
} finally { $listener.Stop() }
$temp = Join-Path ([IO.Path]::GetTempPath()) ('ERP_RESEAU_TEST_' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($temp)
try {
    $configPath = Join-Path $temp 'lecture.config'
    $configText = '<configuration><connectionStrings><add name="CommercialMagDB" connectionString="Data Source=tcp:192.0.2.10,1533;Initial Catalog=master;User ID=SECRET_LOGIN_TEST;Password=SECRET_PASSWORD_TEST" /></connectionStrings></configuration>'
    [IO.File]::WriteAllText($configPath,$configText)
    $date = [IO.File]::GetLastWriteTimeUtc($configPath)
    $target = Read-ERPTarget $configPath
    Assert-ERP ($target.Source -eq 'tcp:192.0.2.10,1533' -and $target.Base -eq 'master') 'Cible existante relue'
    Assert-ERP ((ConvertTo-Json $target) -notmatch 'SECRET_') 'Aucun identifiant retourne par lecture de cible'
    Assert-ERP ([IO.File]::ReadAllText($configPath) -ceq $configText -and [IO.File]::GetLastWriteTimeUtc($configPath) -eq $date) 'Configuration jamais modifiee'
    $stages = [CommercialPro.Diagnostics.SqlNetworkProbe]::MeasureAsync('', '127.0.0.1', 1, 1000, 1, $false, $false).GetAwaiter().GetResult()
    Assert-ERP (@($stages | Where-Object { $_.Stage -like 'SQL_*' -and $_.Status -eq 'IGNORE' }).Count -eq 2) 'Aucune connexion SQL sans activation explicite'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'exemples/Campagne_EXEMPLE.jsonl') -Destination (Join-Path $temp 'un.jsonl')
    Export-ERPJsonArray (Join-Path $temp 'un.jsonl') (Join-Path $temp 'un.json')
    $array = @(Get-Content -LiteralPath (Join-Path $temp 'un.json') -Raw | ConvertFrom-Json)
    Assert-ERP ($array.Count -eq 11) 'Journal JSON complet valide'
    Copy-Item -LiteralPath (Join-Path $temp 'un.jsonl') -Destination (Join-Path $temp 'copie.jsonl')
    & (Join-Path $PSScriptRoot 'SyntheseDiagnosticERP.ps1') -DossierJournaux $temp -Campagne EXEMPLE
    $path = @(Get-ChildItem -LiteralPath (Join-Path $temp 'synthese') -Filter '*.json')[0].FullName
    $report = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    Assert-ERP ($report.Lignes -eq 11) 'Copies du journal dedupliquees'
    Assert-ERP ($report.Incidents.Count -eq 2 -and $report.Coincidences.Count -eq 1) 'Synthese complete sur donnees synthetiques'
    Assert-ERP ($report.Coincidences[0].ChevauchementSecondes -eq 50) 'Comparaison UTC attendue'
    Assert-ERP (@($report.Mesures | Where-Object { $_.Poste -eq 'PC_CAISSE' -and $_.Echecs -eq 0 }).Count -eq 1) 'Serveur sain dans exemple non transforme en panne'
} finally { [IO.Directory]::Delete($temp,$true) }
Write-Host ('PASS : ' + $script:n + ' controles. OS=' + [Environment]::OSVersion.Platform + '. Aucune collecte Windows ou connexion SQL authentifiee executee.')
