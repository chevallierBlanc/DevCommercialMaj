#requires -Version 5.1
Set-StrictMode -Version Latest

function Import-ERPProbe {
    if (-not ('CommercialPro.Diagnostics.SqlNetworkProbe' -as [type])) {
        if ($PSVersionTable.PSEdition -eq 'Core') {
            $refs = @([IO.Directory]::GetFiles((Join-Path $PSHOME 'ref'), '*.dll')) + @([System.Data.SqlClient.SqlConnection].Assembly.Location)
        } else { $refs = @('System.dll', 'System.Core.dll', 'System.Data.dll') }
        Add-Type -Path (Join-Path $PSScriptRoot 'SqlNetworkProbe.cs') -ReferencedAssemblies $refs
    }
}

function Read-ERPTarget([string]$Path) {
    # Lecture seule : les identifiants de l'application ne sont jamais reutilises.
    try {
        Add-Type -AssemblyName System.Configuration
        if (-not [IO.File]::Exists($Path)) { throw 'absent' }
        $map = [System.Configuration.ExeConfigurationFileMap]::new()
        $map.ExeConfigFilename = [IO.Path]::GetFullPath($Path)
        $config = [System.Configuration.ConfigurationManager]::OpenMappedExeConfiguration($map, [System.Configuration.ConfigurationUserLevel]::None)
        $item = $config.ConnectionStrings.ConnectionStrings['CommercialMagDB']
        if ($null -eq $item) { throw 'absent' }
        $b = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($item.ConnectionString)
        try {
            if ($b.get_UserInstance() -or $b.get_AttachDBFilename()) { throw 'fichier' }
            return [pscustomobject]@{ Source = $b.get_DataSource(); Base = $b.get_InitialCatalog() }
        } finally { $b.Clear() }
    } catch { throw 'Configuration illisible : verifier le chemin et le compte Windows, sans transmettre son contenu.' }
}

function Get-ERPStart([string]$Value) {
    if ($Value -notmatch '(Z|[+-]\d{2}:\d{2})$') { throw 'DepartUtc exige Z ou un decalage UTC explicite.' }
    return [DateTimeOffset]::Parse($Value, [Globalization.CultureInfo]::InvariantCulture)
}

function New-ERPRecord($Context, [string]$Type, [string]$Stage, [string]$Target, [string]$Status, $Duration, $Details) {
    return [pscustomobject][ordered]@{
        Schema = 1; Type = $Type; Campagne = $Context.Campagne; Execution = $Context.Execution
        Poste = $Context.Poste; Mode = $Context.Mode; Cycle = $Context.Cycle
        Utc = [DateTime]::UtcNow.ToString('o'); Etape = $Stage; Cible = $Target
        Etat = $Status; DureeMs = $Duration; Transition = ''; IncidentId = ''
        DebutIncidentUtc = ''; FinIncidentUtc = ''; DureeObserveeSecondes = $null
        CensureDebut = $false; CensureFin = $false
        DetailsJson = $(if ($null -eq $Details) { '{}' } else { ConvertTo-Json -InputObject $Details -Depth 8 -Compress })
    }
}

function Write-ERPRecord($Writers, $Row) {
    # Schema identique pour toutes les lignes ; pas de message d'exception libre.
    $Writers.Json.WriteLine((ConvertTo-Json -InputObject $Row -Depth 8 -Compress))
    $csv = @($Row | ConvertTo-Csv -NoTypeInformation)
    if (-not $Writers.Header) { $Writers.Csv.WriteLine($csv[0]); $Writers.Header = $true }
    $Writers.Csv.WriteLine($csv[1])
    $Writers.Json.Flush(); $Writers.Csv.Flush()
}

function Export-ERPJsonArray([string]$Source, [string]$Destination) {
    # Conversion en flux de notre JSONL termine, sans charger toute la campagne.
    $reader = [IO.StreamReader]::new($Source)
    try {
        $writer = [IO.StreamWriter]::new($Destination, $false, [Text.UTF8Encoding]::new($false))
        try {
            $writer.Write('['); $first = $true
            while ($null -ne ($line = $reader.ReadLine())) {
                if ([string]::IsNullOrWhiteSpace($line)) { continue }
                if (-not $first) { $writer.Write(',') }
                $writer.WriteLine(); $writer.Write($line); $first = $false
            }
            $writer.WriteLine(); $writer.WriteLine(']')
        } finally { $writer.Dispose() }
    } finally { $reader.Dispose() }
}

function Update-ERPIncident($States, $Row, [double]$Seconds) {
    $key = $Row.Etape + '|' + $Row.Cible
    if ($Row.Etat -eq 'IGNORE') { $Row.Transition = 'NON_MESURE'; return $null }
    if (-not $States.ContainsKey($key)) {
        $States[$key] = @{ Previous = ''; Active = $null; Good = 0; Bad = 0 }
    }
    $s = $States[$key]
    if ($Row.Etat -eq 'ECHEC') {
        $s.Bad++
        if ($null -eq $s.Active) {
            $s.Active = @{ Id = [Guid]::NewGuid().ToString('D'); Utc = $Row.Utc; Seconds = $Seconds; Left = ($s.Previous -eq '') }
            $Row.Transition = 'INTERRUPTION'
        } else { $Row.Transition = 'ECHEC_PERSISTANT' }
        $Row.IncidentId = $s.Active.Id
    } elseif ($Row.Etat -eq 'SUCCES') {
        $s.Good++
        $Row.Transition = $(if ($s.Previous -eq '') { 'INITIAL_DISPONIBLE' } else { 'INCHANGE' })
        if ($null -ne $s.Active) {
            $Row.Transition = 'RETOUR_NORMAL'; $Row.IncidentId = $s.Active.Id
            $incident = $Row.PSObject.Copy()
            $incident.Type = 'INCIDENT'; $incident.DebutIncidentUtc = $s.Active.Utc
            $incident.FinIncidentUtc = $Row.Utc
            $incident.DureeObserveeSecondes = [Math]::Round([Math]::Max(0, $Seconds - $s.Active.Seconds), 3)
            $incident.CensureDebut = $s.Active.Left
            $s.Active = $null; $s.Previous = $Row.Etat
            return $incident
        }
    }
    $s.Previous = $Row.Etat
    return $null
}

function Close-ERPIncidents($States, $Context, [double]$Seconds) {
    foreach ($key in $States.Keys) {
        $s = $States[$key]
        if ($null -eq $s.Active) { continue }
        $parts = $key.Split(@('|'), 2)
        $r = New-ERPRecord $Context 'INCIDENT' $parts[0] $parts[1] 'ECHEC' $null $null
        $r.IncidentId = $s.Active.Id; $r.DebutIncidentUtc = $s.Active.Utc
        $r.FinIncidentUtc = $r.Utc; $r.CensureDebut = $s.Active.Left; $r.CensureFin = $true
        $r.DureeObserveeSecondes = [Math]::Round([Math]::Max(0, $Seconds - $s.Active.Seconds), 3)
        $r
    }
}

function Convert-ERPDate($Value) {
    # PowerShell 7 peut convertir automatiquement les dates JSON ; 5.1 les garde en texte.
    if ($Value -is [DateTimeOffset]) { return $Value }
    if ($Value -is [DateTime]) { return [DateTimeOffset]$Value }
    return [DateTimeOffset]::Parse([string]$Value, [Globalization.CultureInfo]::InvariantCulture)
}

function Get-ERPOverlap($Left, $Right, [double]$ToleranceSeconds) {
    if ($Left.Campagne -ne $Right.Campagne -or $Left.Poste -eq $Right.Poste) { return $null }
    $a = Convert-ERPDate $Left.DebutIncidentUtc
    $b = Convert-ERPDate $Left.FinIncidentUtc
    $c = Convert-ERPDate $Right.DebutIncidentUtc
    $d = Convert-ERPDate $Right.FinIncidentUtc
    $start = $(if ($a -gt $c) { $a } else { $c })
    $end = $(if ($b -lt $d) { $b } else { $d })
    if (($start - $end).TotalSeconds -gt $ToleranceSeconds) { return $null }
    return [pscustomobject]@{
        Campagne = $Left.Campagne; PosteA = $Left.Poste; PosteB = $Right.Poste
        EtapeA = $Left.Etape; EtapeB = $Right.Etape; CibleA = $Left.Cible; CibleB = $Right.Cible
        IncidentA = $Left.IncidentId; IncidentB = $Right.IncidentId
        DebutCommunUtc = $start.ToString('o'); FinCommuneUtc = $end.ToString('o')
        ChevauchementSecondes = [Math]::Round([Math]::Max(0, ($end - $start).TotalSeconds), 3)
        EcartSecondes = [Math]::Round([Math]::Max(0, ($start - $end).TotalSeconds), 3)
        Qualification = $(if ($end -ge $start) { 'CHEVAUCHEMENT_OBSERVE' } else { 'PROXIMITE_TEMPORELLE' })
    }
}
