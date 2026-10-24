#requires -Version 5.1
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$DossierJournaux,
    [Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9_-]{1,64}$')][string]$Campagne,
    [ValidateRange(0,120)][double]$ToleranceSecondes = 0,
    [string]$DossierSortie = (Join-Path $DossierJournaux 'synthese')
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'DiagnosticCommun.ps1')
$groups = @{}; $incidents = [Collections.Generic.List[object]]::new()
$seen = @{}; $files = @(Get-ChildItem -LiteralPath $DossierJournaux -Filter '*.jsonl' -File)
$read = 0; $truncated = 0
foreach ($file in $files) {
    $reader = [IO.StreamReader]::new($file.FullName)
    try {
        while ($null -ne ($line = $reader.ReadLine())) {
            try { $r = $line | ConvertFrom-Json } catch { $truncated++; continue }
            if ($r.Campagne -ne $Campagne -or $r.Schema -ne 1) { continue }
            # Copier le meme journal deux fois ne doit pas doubler les statistiques.
            $key = $r.Execution + '|' + $r.Cycle + '|' + $r.Type + '|' + $r.Etape + '|' + $r.Cible + '|' + $r.Utc + '|' + $r.IncidentId
            if ($seen.ContainsKey($key)) { continue }; $seen[$key] = $true; $read++
            if ($r.Type -eq 'INCIDENT') {
                if ($incidents.Count -ge 2000) { throw 'Plus de 2000 incidents : reduire la campagne ou la periode pour la comparaison.' }
                $incidents.Add($r)
            }
            if ($r.Type -ne 'MESURE') { continue }
            $k = $r.Poste + '|' + $r.Mode + '|' + $r.Etape + '|' + $r.Cible
            if (-not $groups.ContainsKey($k)) {
                $groups[$k] = [pscustomobject]@{ Campagne = $Campagne; Poste = $r.Poste; Mode = $r.Mode; Etape = $r.Etape; Cible = $r.Cible; Reussites = 0; Echecs = 0; NonMesures = 0; TauxEchecPourcent = 0.0; Incidents = 0; DureeObserveeSecondes = 0.0 }
            }
            $g = $groups[$k]
            switch ($r.Etat) { 'SUCCES' { $g.Reussites++ } 'ECHEC' { $g.Echecs++ } default { $g.NonMesures++ } }
        }
    } finally { $reader.Dispose() }
}
if ($read -eq 0) { throw 'Aucun journal de cette campagne trouve. Fournir les JSONL, pas seulement les CSV.' }
foreach ($r in $incidents) {
    $k = $r.Poste + '|' + $r.Mode + '|' + $r.Etape + '|' + $r.Cible
    if ($groups.ContainsKey($k)) { $groups[$k].Incidents++; $groups[$k].DureeObserveeSecondes += [double]$r.DureeObserveeSecondes }
}
$stats = @($groups.Values | Sort-Object Poste,Etape,Cible)
foreach ($g in $stats) {
    if ($g.Reussites + $g.Echecs -gt 0) { $g.TauxEchecPourcent = [Math]::Round(100.0 * $g.Echecs / ($g.Reussites + $g.Echecs), 2) }
}
$pairs = [Collections.Generic.List[object]]::new()
for ($i = 0; $i -lt $incidents.Count; $i++) {
    for ($j = $i + 1; $j -lt $incidents.Count; $j++) {
        $p = Get-ERPOverlap $incidents[$i] $incidents[$j] $ToleranceSecondes
        if ($null -ne $p) { $pairs.Add($p) }
    }
}
$out = [IO.Path]::GetFullPath($DossierSortie)
[void][IO.Directory]::CreateDirectory($out)
$prefix = Join-Path $out ('Synthese_' + $Campagne + '_' + [Guid]::NewGuid().ToString('N'))
$stats | Export-Csv -LiteralPath ($prefix + '_mesures.csv') -NoTypeInformation -Encoding UTF8
if ($pairs.Count -gt 0) { $pairs | Export-Csv -LiteralPath ($prefix + '_coincidences.csv') -NoTypeInformation -Encoding UTF8 }
$report = [ordered]@{
    Campagne = $Campagne; CreeUtc = [DateTime]::UtcNow.ToString('o'); Lignes = $read; LignesJsonInvalides = $truncated
    ToleranceSecondes = $ToleranceSecondes; Mesures = $stats; Incidents = @($incidents.ToArray()); Coincidences = @($pairs.ToArray())
    Limites = 'Cooccurrence observee, pas causalite. Horloges synchronisees necessaires. ICMP peut etre filtre. Durees echantillonnees et censurees, pas des durees physiques exactes. Aucun evenement INCIDENT final garanti apres arret brutal du processus.'
}
ConvertTo-Json -InputObject $report -Depth 10 | Set-Content -LiteralPath ($prefix + '.json') -Encoding UTF8
$text = @(
    '# Synthese reseau ERP - ' + $Campagne
    ''
    'Lignes lues : ' + $read + '. JSON invalides : ' + $truncated + '.'
    'Incidents observes : ' + $incidents.Count + '. Comparaisons inter-postes : ' + $pairs.Count + '.'
    ''
    'Les taux mesurent les echantillons en echec, pas une disponibilite SLA.'
    'Un chevauchement UTC est un indice de simultaneite, jamais la preuve de sa cause.'
    'Une proximite avec tolerance non nulle n est pas un chevauchement.'
    'Consulter les CSV et le JSON pour les etapes, cibles et intervalles exacts.'
    'Verifier les horloges, les incidents ouverts/censures et les donnees manquantes.'
)
$text | Set-Content -LiteralPath ($prefix + '.md') -Encoding UTF8
Write-Host ('Synthese generee : ' + $prefix + '.*')
