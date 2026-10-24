#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidateSet('SERVEUR','CLIENT')][string]$Mode,
    [string]$Serveur,
    [ValidateRange(0,65535)][int]$PortSql = 0,
    [string]$ServiceSql = 'MSSQLSERVER',
    [string]$ConfigurationERP,
    [string]$InterfaceEthernet,
    [string]$Passerelle,
    [ValidateRange(1,1440)][int]$DureeMinutes = 30,
    [ValidateRange(10,300)][int]$IntervalleSecondes = 30,
    [ValidateRange(30,600)][int]$IntervalleSqlSecondes = 60,
    [ValidateRange(30,600)][int]$IntervalleSystemeSecondes = 60,
    [ValidateRange(1,10)][int]$DelaiSondeSecondes = 3,
    [ValidatePattern('^[A-Za-z0-9_-]{1,64}$')][string]$Campagne = 'ERP_RESEAU',
    [string]$DepartUtc,
    [ValidateSet('Aucune','Windows','Sql')][string]$Authentification = 'Aucune',
    [System.Management.Automation.PSCredential]$IdentifiantsSql,
    [string]$BaseDiagnostic = 'master',
    [switch]$FaireConfianceCertificat,
    [string]$DossierSortie = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'CommercialPro\DiagnosticReseau')
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'DiagnosticCommun.ps1')

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'Collecte reservee a Windows. Utiliser Tests-Diagnostic.ps1 pour les tests hors Windows.' }
foreach ($required in @('Get-NetIPConfiguration','Get-NetAdapter','Get-NetAdapterStatistics','Get-NetTCPConnection','Get-CimInstance','Get-WinEvent','Get-Service')) {
    if ($null -eq (Get-Command $required -ErrorAction SilentlyContinue)) { throw ('Prerequis Windows absent : ' + $required + '. Aucun composant ne sera installe automatiquement.') }
}
Import-ERPProbe
if ($Authentification -eq 'Sql' -and $null -eq $IdentifiantsSql) { throw 'Fournir un PSCredential de diagnostic via Get-Credential ; jamais de mot de passe en ligne de commande.' }
if ($Authentification -ne 'Sql' -and $null -ne $IdentifiantsSql) { throw 'IdentifiantsSql exige Authentification Sql.' }
if ($FaireConfianceCertificat -and $Authentification -eq 'Aucune') { throw 'Option certificat sans sonde SQL.' }
if ($DepartUtc) { $depart = Get-ERPStart $DepartUtc }

# Detection facultative de la cible, jamais des secrets de production.
if ($ConfigurationERP) {
    $target = Read-ERPTarget $ConfigurationERP
    if (-not $Serveur) { $Serveur = $target.Source }
}
if (-not $Serveur) {
    if ($Mode -eq 'SERVEUR') { $Serveur = [Environment]::MachineName }
    else { throw 'Renseigner Serveur ou ConfigurationERP ; aucune cible par defaut inventee.' }
}
if ($ServiceSql -notmatch '^(MSSQLSERVER|MSSQL\$[A-Za-z0-9_]+)$') { throw 'ServiceSql attend MSSQLSERVER ou MSSQL$INSTANCE.' }
if ($PortSql -eq 0 -and $Serveur -notmatch ',\s*\d+$' -and $Mode -eq 'SERVEUR') {
    # Seulement les sockets du service SQL demande ; plusieurs ports restent ambigus.
    $svc = Get-CimInstance Win32_Service -Filter ("Name='" + $ServiceSql + "'")
    if ($null -ne $svc -and $svc.ProcessId -gt 0) {
        $ports = @(Get-NetTCPConnection -State Listen -OwningProcess $svc.ProcessId | Select-Object -ExpandProperty LocalPort -Unique)
        if ($ports.Count -eq 1) { $PortSql = [int]$ports[0] }
    }
}
$endpoint = [CommercialPro.Diagnostics.SqlNetworkProbe]::ParseEndpoint($Serveur, $PortSql)

if ($InterfaceEthernet) {
    $configs = @(Get-NetIPConfiguration -InterfaceAlias $InterfaceEthernet)
} else {
    $configs = @(Get-NetIPConfiguration | Where-Object { $null -ne $_.IPv4DefaultGateway -and $_.NetAdapter.Status -eq 'Up' })
}
if ($configs.Count -ne 1) { throw 'Carte reseau ambigue ou absente : preciser InterfaceEthernet (Get-NetIPConfiguration).' }
$nic = $configs[0].InterfaceAlias
if (-not $Passerelle -and $null -ne $configs[0].IPv4DefaultGateway) { $Passerelle = @($configs[0].IPv4DefaultGateway.NextHop)[0] }
if ($Passerelle) {
    $gatewayIp = $null
    if (-not [Net.IPAddress]::TryParse($Passerelle, [ref]$gatewayIp)) { throw 'Passerelle doit etre une adresse IP, sans resolution DNS implicite.' }
}
$output = [IO.Path]::GetFullPath($DossierSortie)
if ($output.StartsWith('\\')) { throw 'Le dossier de sortie doit etre local pour resister aux coupures.' }
[void][IO.Directory]::CreateDirectory($output)
$ctx = @{ Campagne = $Campagne; Execution = [Guid]::NewGuid().ToString('D'); Poste = [Environment]::MachineName; Mode = $Mode; Cycle = '' }
$stem = Join-Path $output ($Campagne + '_' + $ctx.Poste + '_' + $Mode + '_' + $ctx.Execution)
$writers = @{ Json = [IO.StreamWriter]::new($stem + '.jsonl', $false, [Text.UTF8Encoding]::new($false)); Csv = $null; Header = $false }
$states = @{}; $clock = [Diagnostics.Stopwatch]::new(); $sqlOptions = $null
$lastSql = -10000.0; $lastSystem = -10000.0; $lastEvent = [DateTime]::UtcNow
$sqlEnabled = $Authentification -ne 'Aucune'; $lastLink = ''; $lastRx = $null; $lastTx = $null

function Save-Measurement([string]$Stage, [string]$Target, [string]$Status, $Duration, $Details, [string]$Utc = '') {
    $row = New-ERPRecord $ctx 'MESURE' $Stage $Target $Status $Duration $Details
    if ($Utc) { $row.Utc = $Utc }
    $incident = Update-ERPIncident $states $row $clock.Elapsed.TotalSeconds
    Write-ERPRecord $writers $row
    if ($null -ne $incident) { Write-ERPRecord $writers $incident }
}

function Save-LocalFailure([string]$Stage, [string]$Target, $ErrorObject) {
    # Une collecte refusee n'est pas une preuve de panne du composant.
    Save-Measurement $Stage $Target 'IGNORE' $null @{ Categorie = 'COLLECTE_INDISPONIBLE'; Type = $ErrorObject.Exception.GetType().Name }
}

try {
    $writers.Csv = [IO.StreamWriter]::new($stem + '.csv', $false, [Text.UTF8Encoding]::new($true))
    if ($sqlEnabled) {
        $sqlOptions = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()
        $hostSql = $(if ($endpoint.Host.Contains(':')) { '[' + $endpoint.Host + ']' } else { $endpoint.Host })
        $sqlOptions.set_DataSource('tcp:' + $hostSql + ',' + $endpoint.Port)
        $sqlOptions.set_InitialCatalog($BaseDiagnostic); $sqlOptions.set_Encrypt($true)
        $sqlOptions.set_TrustServerCertificate([bool]$FaireConfianceCertificat)
        $sqlOptions.set_IntegratedSecurity($Authentification -eq 'Windows')
        if ($Authentification -eq 'Sql') {
            $sqlOptions.set_UserID($IdentifiantsSql.UserName)
            $sqlOptions.set_Password($IdentifiantsSql.GetNetworkCredential().Password)
        }
    }
    Write-ERPRecord $writers (New-ERPRecord $ctx 'CONFIGURATION' 'DEMARRAGE' $endpoint.Host 'INFO' $null @{
        PortSql = $endpoint.Port; Interface = $nic; Passerelle = $Passerelle; ServiceSql = $ServiceSql
        Authentification = $Authentification; BaseDiagnostic = $BaseDiagnostic; Chiffrement = $true
        FaireConfianceCertificat = [bool]$FaireConfianceCertificat; IntervalleSecondes = $IntervalleSecondes
        IntervalleSqlSecondes = $IntervalleSqlSecondes; DureeMinutes = $DureeMinutes; ProcessBits = [IntPtr]::Size * 8
        PowerShell = $PSVersionTable.PSVersion.ToString(); Note = 'Lecture seule, aucune operation ERP, aucun secret exporte.'
    })
    if ($DepartUtc) { while ([DateTimeOffset]::UtcNow -lt $depart) { Start-Sleep -Milliseconds 200 } }
    Write-Host ('Collecte ' + $Mode + ' : ' + $stem + '.csv / .jsonl')
    $lastEvent = [DateTime]::UtcNow; $clock.Start()
    while ($clock.Elapsed.TotalMinutes -lt $DureeMinutes) {
        $cycleStart = $clock.Elapsed.TotalMilliseconds; $ctx.Cycle = [Guid]::NewGuid().ToString('D')
        # SQL est lance independamment du ping ; chaque sonde est de lecture seule.
        $measureSql = $sqlEnabled -and ($clock.Elapsed.TotalSeconds - $lastSql -ge $IntervalleSqlSecondes)
        if ($measureSql) { $lastSql = $clock.Elapsed.TotalSeconds }
        $cs = $(if ($measureSql) { $sqlOptions.get_ConnectionString() } else { '' })
        $networkTask = [CommercialPro.Diagnostics.SqlNetworkProbe]::MeasureAsync($cs, $endpoint.Host, $endpoint.Port, $DelaiSondeSecondes * 1000, $DelaiSondeSecondes, $false, $measureSql)
        $pingTask = $null; $ping = $null; $pingStartFailure = $null
        if ($Passerelle) {
            $pingWatch = [Diagnostics.Stopwatch]::StartNew()
            try {
                $ping = [Net.NetworkInformation.Ping]::new()
                $pingTask = $ping.SendPingAsync($gatewayIp, $DelaiSondeSecondes * 1000)
            } catch { $pingStartFailure = $_.Exception.GetType().Name }
        }
        try {
            try {
                $a = Get-NetAdapter -Name $nic
                $up = $a.Status -eq 'Up'
                $link = [string]$a.Status + '|' + [string]$a.LinkSpeed
                Save-Measurement 'ETHERNET' $nic $(if ($up) { 'SUCCES' } else { 'ECHEC' }) $null @{
                    Statut = [string]$a.Status; Debit = [string]$a.LinkSpeed; MediaNdis = $a.NdisPhysicalMedium
                    Variation = ($lastLink -ne '' -and $lastLink -ne $link)
                }
                $lastLink = $link
            } catch { Save-LocalFailure 'ETHERNET' $nic $_ }
            try {
                $stats = Get-NetAdapterStatistics -Name $nic
                $rx = [double]$stats.ReceivedPacketErrors; $tx = [double]$stats.OutboundPacketErrors
                Write-ERPRecord $writers (New-ERPRecord $ctx 'COMPTEURS' 'ETHERNET' $nic 'INFO' $null @{
                    ErreursReception = $rx; ErreursEmission = $tx
                    DeltaErreursReception = $(if ($null -ne $lastRx -and $rx -ge $lastRx) { $rx - $lastRx } else { $null })
                    DeltaErreursEmission = $(if ($null -ne $lastTx -and $tx -ge $lastTx) { $tx - $lastTx } else { $null })
                    ReinitialisationCompteurs = (($null -ne $lastRx -and $rx -lt $lastRx) -or ($null -ne $lastTx -and $tx -lt $lastTx))
                })
                $lastRx = $rx; $lastTx = $tx
            } catch { Save-LocalFailure 'COMPTEURS_ETHERNET' $nic $_ }
            if ($pingStartFailure) {
                Save-Measurement 'PASSERELLE_ICMP' $Passerelle 'ECHEC' $pingWatch.Elapsed.TotalMilliseconds @{ Type = $pingStartFailure }
            } elseif ($null -ne $pingTask) {
                try {
                    $reply = $pingTask.GetAwaiter().GetResult()
                    Save-Measurement 'PASSERELLE_ICMP' $Passerelle $(if ($reply.Status -eq 'Success') { 'SUCCES' } else { 'ECHEC' }) $reply.RoundtripTime @{ Code = [string]$reply.Status }
                } catch { Save-Measurement 'PASSERELLE_ICMP' $Passerelle 'ECHEC' $pingWatch.Elapsed.TotalMilliseconds @{ Type = $_.Exception.GetType().Name } }
            } else { Save-Measurement 'PASSERELLE_ICMP' 'non_configuree' 'IGNORE' $null @{ Categorie = 'PASSERELLE_ABSENTE' } }
            $stages = $networkTask.GetAwaiter().GetResult()
            foreach ($s in $stages) {
                if ($s.Stage -like 'SQL_*' -and -not $measureSql) { continue }
                Save-Measurement $s.Stage $s.Target $s.Status $s.DurationMs @{
                    Erreurs = @($s.Errors); Adresses = @($s.Addresses); Transport = $s.Transport
                    PortObserve = $s.ObservedSqlPort; ConnexionId = $s.ConnectionId
                    DebutUtc = $s.StartedUtc.ToString('o'); FinUtc = $s.EndedUtc.ToString('o')
                } $s.EndedUtc.ToString('o')
                if (@($s.Errors | Where-Object Category -eq 'AUTHENTIFICATION').Count -gt 0) {
                    $sqlEnabled = $false
                    Write-ERPRecord $writers (New-ERPRecord $ctx 'INFORMATION' 'SQL_SUSPENDU' $endpoint.Host 'INFO' $null @{ Raison = 'Authentification refusee : corriger puis relancer, sans multiplier les tentatives.' })
                }
            }
        } finally { if ($null -ne $ping) { $ping.Dispose() } }
        if ($Mode -eq 'SERVEUR') {
            try {
                $service = Get-Service -Name $ServiceSql
                Save-Measurement 'SERVICE_SQL' $ServiceSql $(if ($service.Status -eq 'Running') { 'SUCCES' } else { 'ECHEC' }) $null @{ Statut = [string]$service.Status }
            } catch { Save-LocalFailure 'SERVICE_SQL' $ServiceSql $_ }
        }
        if ($clock.Elapsed.TotalSeconds - $lastSystem -ge $IntervalleSystemeSecondes) {
            $lastSystem = $clock.Elapsed.TotalSeconds
            if ($Mode -eq 'SERVEUR') {
                try {
                    $cpu = Get-CimInstance Win32_PerfFormattedData_PerfOS_Processor -Filter "Name='_Total'" -OperationTimeoutSec 5
                    $mem = Get-CimInstance Win32_PerfFormattedData_PerfOS_Memory -OperationTimeoutSec 5
                    $disks = @(Get-CimInstance Win32_PerfFormattedData_PerfDisk_LogicalDisk -OperationTimeoutSec 5 | Select-Object Name,AvgDisksecPerRead,AvgDisksecPerWrite,CurrentDiskQueueLength,PercentFreeSpace)
                    Write-ERPRecord $writers (New-ERPRecord $ctx 'RESSOURCES' 'SERVEUR' $ctx.Poste 'INFO' $null @{ CpuPourcent = $cpu.PercentProcessorTime; MemoireDisponibleMo = $mem.AvailableMBytes; Disques = $disks })
                } catch { Save-LocalFailure 'RESSOURCES' $ctx.Poste $_ }
            }
            $until = [DateTime]::UtcNow
            $logs = $(if ($Mode -eq 'SERVEUR') { @('System','Application') } else { @('System') })
            foreach ($log in $logs) {
                try {
                    # Lecture bornee, sans Message ni donnees d'evenement confidentielles.
                    $fromText = $lastEvent.ToString('o'); $untilText = $until.ToString('o')
                    $levels = '(Level=1 or Level=2 or Level=3)'
                    if ($log -eq 'System') {
                        $levels = "(Level=1 or Level=2 or Level=3 or (Level=4 and (Provider[@Name='Service Control Manager'] or Provider[@Name='Microsoft-Windows-NDIS'] or Provider[@Name='Microsoft-Windows-NetworkProfile'] or Provider[@Name='Tcpip'])))"
                    } else {
                        $levels = "(Level=1 or Level=2 or Level=3 or (Level=4 and Provider[@Name='$ServiceSql']))"
                    }
                    $xpath = "*[System[TimeCreated[@SystemTime > '$fromText' and @SystemTime <= '$untilText'] and $levels]]"
                    $events = @(Get-WinEvent -LogName $log -FilterXPath $xpath -MaxEvents 101 -ErrorAction Stop)
                    foreach ($ev in @($events | Select-Object -First 100)) {
                        Write-ERPRecord $writers (New-ERPRecord $ctx 'EVENEMENT_WINDOWS' $log $ev.ProviderName 'INFO' $null @{ Id = $ev.Id; Niveau = $ev.Level; Numero = $ev.RecordId; EvenementUtc = $ev.TimeCreated.ToUniversalTime().ToString('o') })
                    }
                    if ($events.Count -gt 100) { Write-ERPRecord $writers (New-ERPRecord $ctx 'INFORMATION' 'EVENEMENTS_TRONQUES' $log 'INFO' $null @{ Limite = 100 }) }
                } catch {
                    if ($_.FullyQualifiedErrorId -notlike 'NoMatchingEventsFound*') { Save-LocalFailure 'EVENEMENTS' $log $_ }
                }
            }
            $lastEvent = $until
        }
        $remaining = [Math]::Min($IntervalleSecondes * 1000 - ($clock.Elapsed.TotalMilliseconds - $cycleStart), $DureeMinutes * 60000 - $clock.Elapsed.TotalMilliseconds)
        if ($remaining -gt 0) { Start-Sleep -Milliseconds ([int]$remaining) }
    }
} catch {
    if ($null -ne $writers.Csv) { Write-ERPRecord $writers (New-ERPRecord $ctx 'ERREUR_COLLECTEUR' 'COLLECTE' $ctx.Poste 'INFO' $null @{ Type = $_.Exception.GetType().Name; Raison = 'Collecte interrompue : verifier prerequis, droits et espace disque. Aucun message interne exporte.' }) }
    throw 'Collecte interrompue. Consulter les journaux locaux ; ne pas transmettre la configuration ou les identifiants.'
} finally {
    try { if ($null -ne $writers.Csv) { foreach ($r in @(Close-ERPIncidents $states $ctx $clock.Elapsed.TotalSeconds)) { Write-ERPRecord $writers $r } } }
    finally {
        $writers.Json.Dispose(); if ($null -ne $writers.Csv) { $writers.Csv.Dispose() }
        if ($null -ne $sqlOptions) { $sqlOptions.Clear() }
    }
}
Export-ERPJsonArray ($stem + '.jsonl') ($stem + '.json')
Write-Host ('Termine. Journaux : ' + $stem + '.csv / .jsonl / .json')
