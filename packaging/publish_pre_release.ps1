<#
.SYNOPSIS
    Prepara (e opcionalmente executa) a publicação da pre-release v1.1.0 no GitHub Releases.

.DESCRIPTION
    - Valida que o working tree está limpo e que os commits já foram enviados (git push)
    - Valida os hashes dos artefatos contra os arquivos .sha256 e as notas de release
    - Verifica gh CLI autenticado (winget install GitHub.cli, se necessário)
    - Por padrão apenas IMPRIME o comando. Use -Execute para publicar de verdade.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File packaging\publish_pre_release.ps1
    powershell -ExecutionPolicy Bypass -File packaging\publish_pre_release.ps1 -Execute
#>
Param(
    [string]$Version = "1.1.0",
    [switch]$Execute,
    [switch]$SkipPushCheck
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $projectRoot

$releaseDir = Join-Path $projectRoot "dist\release"
$notes = Join-Path $projectRoot "packaging\RELEASE_NOTES_v$Version.md"
$title = "Zip Buddy v$Version - Integrity, Checksums and E-mail Workflows"

$assetNames = @(
    "ZipBuddySetup-x64-v$Version.exe",
    "ZipBuddy-v$Version-windows-x64-portable.zip",
    "manifest.json",
    "ZipBuddySetup-x64-v$Version.exe.sha256",
    "ZipBuddy-v$Version-windows-x64-portable.zip.sha256"
)

Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host "       PRE-RELEASE v$Version - PREPARACAO DO GITHUB RELEASE" -ForegroundColor Cyan
Write-Host "===============================================================================" -ForegroundColor Cyan

# 1. Working tree / push
$status = git status --porcelain
if ($status) {
    throw "Working tree nao esta limpo. Commite suas alteracoes antes de publicar.`n$status"
}
$branch = (git branch --show-current).Trim()
$ahead = (git rev-list --count "@{u}..HEAD" 2>$null)
if ($LASTEXITCODE -ne 0) { $ahead = $null }
if (-not $SkipPushCheck -and $ahead -and [int]$ahead -gt 0) {
    throw "O branch $branch esta $ahead commit(s) a frente do upstream. Execute git push antes de publicar."
}
if ($SkipPushCheck) {
    Write-Host "[OK] Working tree limpo (verificacao de push ignorada por -SkipPushCheck)." -ForegroundColor Yellow
}
else {
    Write-Host "[OK] Working tree limpo; branch $branch sincronizado com upstream." -ForegroundColor Green
}

# 2. Assets + checksums
$assets = @()
foreach ($name in $assetNames) {
    $path = Join-Path $releaseDir $name
    if (-not (Test-Path $path)) { throw "Asset ausente: $path" }
    $assets += $path
}
foreach ($checksumFile in @($assets | Where-Object { $_.EndsWith('.sha256') })) {
    $expected = ((Get-Content $checksumFile -Raw) -split '\s+')[0].ToUpperInvariant()
    $target = Join-Path $releaseDir ([System.IO.Path]::GetFileName($checksumFile).Replace('.sha256', ''))
    $actual = (Get-FileHash $target -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($expected -ne $actual) { throw "SHA256 nao confere para $target" }
}
if (-not (Test-Path $notes)) { throw "Notas de release ausentes: $notes" }
$notesContent = Get-Content $notes -Raw
foreach ($name in @("ZipBuddySetup-x64-v$Version.exe", "ZipBuddy-v$Version-windows-x64-portable.zip")) {
    $hash = (Get-FileHash (Join-Path $releaseDir $name) -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($notesContent -notmatch $hash) { throw "Hash de $name nao consta nas notas de release" }
}
Write-Host "[OK] $($assets.Count) assets e hashes validados; notas de release conferem." -ForegroundColor Green

# 3. gh CLI (accept the default install path even when PATH has not been refreshed yet)
$gh = Get-Command gh -ErrorAction SilentlyContinue
if (-not $gh) {
    $ghFallback = "${env:ProgramFiles}\GitHub CLI\gh.exe"
    if (Test-Path $ghFallback) {
        Set-Alias -Name gh -Value $ghFallback -Scope Script
        $gh = Get-Command gh -ErrorAction SilentlyContinue
    }
}
if (-not $gh) {
    Write-Host "[AVISO] GitHub CLI nao encontrado. Instale com:" -ForegroundColor Yellow
    Write-Host "        winget install --id GitHub.cli" -ForegroundColor Yellow
    Write-Host "        gh auth login" -ForegroundColor Yellow
    if (-not $Execute) { Write-Host "[MODO DRY-RUN] Imprimindo o comando para uso manual." -ForegroundColor Yellow }
    else { throw "Nao e possivel publicar sem o GitHub CLI." }
}
else {
    $previousEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $auth = gh auth status 2>$null | Out-String
    $ErrorActionPreference = $previousEap
    if ($LASTEXITCODE -ne 0) { throw "gh nao autenticado. Execute: gh auth login`n$($auth.Trim())" }
    Write-Host "[OK] GitHub CLI autenticado." -ForegroundColor Green
}

# 4. Monta o comando
$quoted = $assets | ForEach-Object { '"' + $_ + '"' }
$assetArgs = $quoted -join ' '
$command = @(
    "gh release create `"v$Version`"",
    "  --title `"$title`"",
    "  --notes-file `"$notes`"",
    "  --prerelease",
    "  $assetArgs"
) -join "`n"

Write-Host ""
Write-Host "-------------------------------------------------------------------------------" -ForegroundColor Gray
Write-Host $command -ForegroundColor White
Write-Host "-------------------------------------------------------------------------------" -ForegroundColor Gray
Write-Host "Tag: v$Version | Assets: $($assets.Count) | Pre-release: sim" -ForegroundColor White
Write-Host "Notas: $notes" -ForegroundColor White

if ($Execute) {
    Write-Host ""
    Write-Host "Publicando..." -ForegroundColor Yellow
    $previousEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $ghOutput = & gh release create "v$Version" --title $title --notes-file $notes --prerelease @assets 2>&1 | Out-String
    $ErrorActionPreference = $previousEap
    if ($LASTEXITCODE -ne 0) {
        throw "gh release create falhou (exit $LASTEXITCODE):`n$($ghOutput.Trim())"
    }
    Write-Host "[OK] Pre-release publicada: https://github.com/drodrigues55/zip-buddy/releases/tag/v$Version" -ForegroundColor Green
}
else {
    Write-Host ""
    Write-Host "[DRY-RUN] Nada foi publicado. Rode com -Execute para publicar." -ForegroundColor Yellow
}
