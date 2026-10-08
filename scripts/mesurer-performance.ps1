param(
    [string]$Api = "http://localhost:5092",
    [int]$Iterations = 20,
    [int]$SeuilMs = 500
)

$ErrorActionPreference = "Stop"

$corps = @{ email = "admin@ct.local"; motDePasse = "Demo123!" } | ConvertTo-Json
$connexion = Invoke-RestMethod -Method Post -Uri "$Api/api/auth/login" -ContentType "application/json" -Body $corps
$entetes = @{ Authorization = "Bearer $($connexion.jeton)" }

$total = (Invoke-RestMethod -Uri "$Api/api/vehicules?page=1" -Headers $entetes).total
$dernierePage = [math]::Ceiling($total / 20)
Write-Host "Véhicules en base : $total — $Iterations mesures par requête, seuil $SeuilMs ms`n"

$cas = [ordered]@{
    "Véhicules, page 1"                 = "/api/vehicules?page=1"
    "Véhicules, dernière page"          = "/api/vehicules?page=$dernierePage"
    "Recherche d'immatriculation"       = "/api/vehicules?search=AB"
    "Recherche de n° de châssis"        = "/api/vehicules?search=VFA"
    "Recherche de propriétaire"         = "/api/vehicules?search=Martin"
    "Propriétaires, page 1"             = "/api/proprietaires?page=1"
    "Contrôles, page 1"                 = "/api/controles?page=1"
    "Statistiques sur 6 mois"           = "/api/statistiques/resume"
}

$resultats = foreach ($nom in $cas.Keys) {
    $url = "$Api$($cas[$nom])"
    1..2 | ForEach-Object { Invoke-RestMethod -Uri $url -Headers $entetes | Out-Null }

    $durees = 1..$Iterations | ForEach-Object {
        $chrono = [System.Diagnostics.Stopwatch]::StartNew()
        Invoke-RestMethod -Uri $url -Headers $entetes | Out-Null
        $chrono.Stop()
        $chrono.Elapsed.TotalMilliseconds
    } | Sort-Object

    $p95 = $durees[[math]::Ceiling(0.95 * $durees.Count) - 1]
    [pscustomobject]@{
        Requete    = $nom
        Mediane_ms = [math]::Round($durees[[math]::Floor($durees.Count / 2)], 1)
        P95_ms     = [math]::Round($p95, 1)
        Max_ms     = [math]::Round($durees[-1], 1)
        Resultat   = if ($p95 -lt $SeuilMs) { "OK" } else { "TROP LENT" }
    }
}

$resultats | Format-Table -AutoSize

if ($resultats | Where-Object Resultat -ne "OK") { exit 1 }
