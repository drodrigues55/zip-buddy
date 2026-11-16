<#
.SYNOPSIS
    Gates automáticos + checklist interativo da Fase 2 (validação pré-produção) do Zip Buddy.

.DESCRIPTION
    1. Build Release e suíte de testes
    2. Consistência de versão (props / Inno Setup / winget)
    3. Contratos de código (verbos CLI e chaves de localização novas)
    4. Publicação opcional do portable (-Publish) e hashes SHA256
    5. Snapshot somente-leitura das chaves do menu de contexto no HKCU
    6. Instalação/desinstalação silenciosa: registra 4 chaves HKCU e limpa todas (gate A08)
    7. Checklist manual M01-M11 (-Interactive), com relatório Markdown + JSON

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File packaging\validate_phase2.ps1
    powershell -ExecutionPolicy Bypass -File packaging\validate_phase2.ps1 -Interactive
    powershell -ExecutionPolicy Bypass -File packaging\validate_phase2.ps1 -Publish -Interactive
#>
Param(
    [switch]$Interactive,
    [switch]$Publish,
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $projectRoot "qa\results"
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$runDir = Join-Path $OutputDir $stamp
New-Item -ItemType Directory -Path $runDir -Force | Out-Null

$script:results = New-Object 'System.Collections.Generic.List[object]'
$script:snapshot = New-Object 'System.Collections.Generic.List[object]'
$script:environment = [ordered]@{}

function Add-Check {
    param(
        [string]$Id,
        [string]$Category,
        [string]$Status,
        [string]$Detail
    )
    $script:results.Add([pscustomobject]@{
        Id       = $Id
        Category = $Category
        Status   = $Status
        Detail   = $Detail
    }) | Out-Null

    $color = "Gray"
    if ($Status -eq "PASS") { $color = "Green" }
    elseif ($Status -eq "FAIL") { $color = "Red" }
    elseif ($Status -eq "WARN") { $color = "Yellow" }
    Write-Host ("[{0}] {1} - {2}" -f $Status, $Id, $Detail) -ForegroundColor $color
}

function Get-Text {
    param([string]$Path)
    if (Test-Path $Path) { return (Get-Content -Path $Path -Raw) }
    return ""
}

function Get-Tail {
    param([string]$Text, [int]$Length = 500)
    if ($Text.Length -le $Length) { return $Text }
    return "..." + $Text.Substring($Text.Length - $Length)
}

Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host "       ZIP BUDDY - FASE 2 / RELEASE CANDIDATE VALIDATION                       " -ForegroundColor Cyan
Write-Host "       Resultados: $runDir" -ForegroundColor Cyan
Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host ""

# -------------------------------------------------------------------------------
# A01 - Ambiente
# -------------------------------------------------------------------------------
try {
    $os = Get-CimInstance Win32_OperatingSystem
    $dotnet = & dotnet --version 2>&1
    $admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)

    $script:environment = [ordered]@{
        "Data/Hora"       = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        "OS"              = "$($os.Caption) (build $($os.BuildNumber))"
        "Arquitetura"     = $env:PROCESSOR_ARCHITECTURE
        ".NET SDK"        = "$dotnet"
        "Administrador"   = "$admin"
        "Diretorio raiz"  = $projectRoot
        "Checklist"       = (Join-Path $projectRoot "qa\PHASE2_CHECKLIST.md")
    }
    Add-Check "A01" "Ambiente" "INFO" "$($os.Caption) build $($os.BuildNumber), .NET $dotnet"
}
catch {
    Add-Check "A01" "Ambiente" "FAIL" "Nao foi possivel coletar informacoes do ambiente: $_"
}

# -------------------------------------------------------------------------------
# A02 - Build Release
# -------------------------------------------------------------------------------
$global:LASTEXITCODE = 0
$buildOutput = & dotnet build (Join-Path $projectRoot "ZipBuddy.slnx") -c Release 2>&1 | Out-String
$buildExit = $LASTEXITCODE
if ($buildExit -eq 0) {
    Add-Check "A02" "Build" "PASS" "dotnet build -c Release concluido sem erros"
}
else {
    Add-Check "A02" "Build" "FAIL" (Get-Tail $buildOutput)
}

# -------------------------------------------------------------------------------
# A03 - Testes
# -------------------------------------------------------------------------------
$global:LASTEXITCODE = 0
$testOutput = & dotnet test (Join-Path $projectRoot "ZipBuddy.slnx") --no-build -c Release 2>&1 | Out-String
$testExit = $LASTEXITCODE
$totalMatch = [regex]::Match($testOutput, "(?:Total|Total de testes):\s*(\d+)")
$failedMatch = [regex]::Match($testOutput, "(?:Com falha|Failed):\s*(\d+)")
$totalText = if ($totalMatch.Success) { $totalMatch.Groups[1].Value } else { "?" }
$failedText = if ($failedMatch.Success) { $failedMatch.Groups[1].Value } else { "?" }

if ($testExit -eq 0) {
    Add-Check "A03" "Testes" "PASS" "$totalText testes executados, $failedText falhas"
}
else {
    Add-Check "A03" "Testes" "FAIL" (Get-Tail $testOutput)
}

# -------------------------------------------------------------------------------
# A04 - Consistencia de versao
# -------------------------------------------------------------------------------
$props = Get-Text (Join-Path $projectRoot "Directory.Build.props")
$iss = Get-Text (Join-Path $projectRoot "packaging\installer\zipbuddy_setup.iss")
$version = [regex]::Match($props, "<Version>([^<]+)</Version>").Groups[1].Value
$issVersion = [regex]::Match($iss, '#define MyAppVersion\s+"([^"]+)"').Groups[1].Value

$wingetRoot = Join-Path $projectRoot "packaging\winget\manifests\z\ZipBuddy\ZipBuddy"
$wingetVersions = @()
if (Test-Path $wingetRoot) {
    $wingetVersions = @(Get-ChildItem -Path $wingetRoot -Directory | Select-Object -ExpandProperty Name)
}

if ([string]::IsNullOrWhiteSpace($version) -or $version -ne $issVersion) {
    Add-Check "A04" "Versao" "WARN" "Props=$version, Inno Setup=$issVersion (sincronizar antes da Fase 3)"
}
elseif ($wingetVersions -notcontains $version) {
    Add-Check "A04" "Versao" "WARN" "Props/Inno=$version, mas nao ha manifest winget para essa versao (existentes: $($wingetVersions -join ', '))"
}
else {
    Add-Check "A04" "Versao" "PASS" "Props, Inno Setup e winget apontam para $version"
}

# -------------------------------------------------------------------------------
# A05 - Contratos de codigo (verbos CLI + localizacao)
# -------------------------------------------------------------------------------
$appSource = Get-Text (Join-Path $projectRoot "src\ZipBuddy.UI\App.xaml.cs")
$cliVerbs = @("--test-archive", "--crc-sha", "--compress-dialog", "--compress-email-dialog", "--compress-email-zip", "--compress-email-7z", "--register-context-menu", "--unregister-context-menu")
$missingVerbs = @($cliVerbs | Where-Object { -not $appSource.Contains($_) })

$locSource = Get-Text (Join-Path $projectRoot "src\ZipBuddy.Infrastructure\Services\LocalizationService.cs")
$locKeys = @("Task_TestingTitle", "Task_TestCompleteMessage", "Checksum_Title", "Checksum_FileSize", "CompressDlg_Title", "CompressDlg_ErrorSameAsSource", "CompressDlg_OverwriteMessage")
$missingKeys = @($locKeys | Where-Object {
    ([regex]::Matches($locSource, [regex]::Escape('["' + $_ + '"]'))).Count -lt 2
})

if ($missingVerbs.Count -eq 0 -and $missingKeys.Count -eq 0) {
    Add-Check "A05" "Contratos" "PASS" "8 verbos CLI e chaves de localizacao presentes nos dois catalogos"
}
else {
    $detail = @()
    if ($missingVerbs.Count -gt 0) { $detail += "verbos ausentes: $($missingVerbs -join ', ')" }
    if ($missingKeys.Count -gt 0) { $detail += "chaves ausentes/duplicadas: $($missingKeys -join ', ')" }
    Add-Check "A05" "Contratos" "FAIL" ($detail -join "; ")
}

# -------------------------------------------------------------------------------
# A06 - Portable / artefatos
# -------------------------------------------------------------------------------
if ($Publish) {
    Write-Host "      Publicando ZipBuddy.UI (Release)..." -ForegroundColor Gray
    $global:LASTEXITCODE = 0
    & dotnet publish (Join-Path $projectRoot "src\ZipBuddy.UI\ZipBuddy.UI.csproj") -c Release 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Add-Check "A06" "Artefatos" "FAIL" "Falha ao publicar ZipBuddy.UI"
    }
    else {
        & dotnet publish (Join-Path $projectRoot "src\ZipBuddy.Launcher\ZipBuddy.Launcher.csproj") -c Release -r win-x64 2>&1 | Out-Null
        $launcher = Join-Path $projectRoot "src\ZipBuddy.Launcher\bin\Release\net10.0-windows\win-x64\publish\ZipBuddy.UI.exe"
        if (Test-Path $launcher) {
            Add-Check "A06" "Artefatos" "PASS" "Launcher publicado na pasta de publish do projeto"
        }
        else {
            Add-Check "A06" "Artefatos" "FAIL" "Launcher publicado, mas executavel nao encontrado: $launcher"
        }
    }
}

$rootInstaller = Join-Path $projectRoot "ZipBuddySetup-x64-v$version.exe"
if (Test-Path $rootInstaller) {
    $hash = (Get-FileHash -Path $rootInstaller -Algorithm SHA256).Hash
    $size = [math]::Round((Get-Item $rootInstaller).Length / 1MB, 2)
    $script:environment["Instalador raiz SHA256"] = $hash
    Add-Check "A06" "Artefatos" "PASS" "ZipBuddySetup-x64-v$version.exe ($size MB), SHA256=$hash"
}
else {
    Add-Check "A06" "Artefatos" "WARN" "Instalador nao encontrado na raiz; execute packaging\build_installer.ps1 -Version $version"
}

$installerDir = Join-Path $projectRoot "dist"
if (Test-Path $installerDir) {
    $installers = @(Get-ChildItem -Path $installerDir -Filter "ZipBuddySetup-*.exe")
    if ($installers.Count -gt 0) {
        foreach ($installer in $installers) {
            $ihash = (Get-FileHash -Path $installer.FullName -Algorithm SHA256).Hash
            Add-Check "A06i" "Artefatos" "INFO" "$($installer.Name) SHA256=$ihash"
        }
    }
    else {
        Add-Check "A06i" "Artefatos" "WARN" "Nenhum instalador em dist\ (use packaging\verify_release_candidate.ps1 apos gerar)"
    }
}

# -------------------------------------------------------------------------------
# A07 - Snapshot do registro (somente leitura)
# -------------------------------------------------------------------------------
$registryPaths = @(
    "Software\Classes\SystemFileAssociations\.zip\shell",
    "Software\Classes\SystemFileAssociations\.7z\shell",
    "Software\Classes\*\shell",
    "Software\Classes\Directory\shell"
)

foreach ($path in $registryPaths) {
    try {
        $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($path)
        if ($null -eq $key) {
            $script:snapshot.Add([pscustomobject]@{ Key = $path; Group = "(ausente)"; Verbs = "" })
            continue
        }
        $group = if ($key.GetSubKeyNames() -contains "Zip Buddy") { "presente" } else { "ausente" }
        $groupKey = $key.OpenSubKey("Zip Buddy\shell")
        $verbNames = @()
        if ($null -ne $groupKey) { $verbNames = @($groupKey.GetSubKeyNames()) }
        $flat = @($key.GetSubKeyNames() | Where-Object { $_ -like "Zip Buddy.*" })
        $script:snapshot.Add([pscustomobject]@{
            Key   = $path
            Group = $group
            Verbs = "$(@($verbNames).Count) em cascata / $(@($flat).Count) flat"
        })
        $key.Close()
    }
    catch {
        $script:snapshot.Add([pscustomobject]@{ Key = $path; Group = "erro: $_"; Verbs = "" })
    }
}
Add-Check "A07" "Registro" "INFO" "Snapshot de $($registryPaths.Count) chaves HKCU registrado no relatorio"

# -------------------------------------------------------------------------------
# A08 - Instalacao/desinstalacao silenciosa + registro do menu (M11 automatizado)
# -------------------------------------------------------------------------------
$silentInstaller = Join-Path $projectRoot "dist\ZipBuddySetup-x64-v$version.exe"
$menuGroupKeys = @(
    "Software\Classes\SystemFileAssociations\.zip\shell\Zip Buddy",
    "Software\Classes\SystemFileAssociations\.7z\shell\Zip Buddy",
    "Software\Classes\*\shell\Zip Buddy",
    "Software\Classes\Directory\shell\Zip Buddy"
)

if (-not (Test-Path $silentInstaller)) {
    Add-Check "A08" "Registro" "WARN" "Instalador nao encontrado em dist\\ para o teste de registro"
}
else {
    $flowSandbox = Join-Path $env:TEMP ("ZipBuddy_Validate_" + [Guid]::NewGuid().ToString("N"))
    try {
        $t0 = Get-Date
        $installArgs = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /CURRENTUSER /DIR=""$flowSandbox"""
        $installProc = Start-Process -FilePath $silentInstaller -ArgumentList $installArgs -Wait -PassThru
        $t1 = Get-Date

        if ($installProc.ExitCode -ne 0) {
            throw "Instalacao silenciosa retornou exit code $($installProc.ExitCode)"
        }

        $present = @($menuGroupKeys | Where-Object { Test-Path "HKCU:\$_" }).Count
        if ($present -ne $menuGroupKeys.Count) {
            throw "Apos instalacao silenciosa so $present/$($menuGroupKeys.Count) chaves HKCU do menu estao presentes"
        }

        $uninstaller = Join-Path $flowSandbox "unins000.exe"
        if (-not (Test-Path $uninstaller)) {
            throw "unins000.exe nao encontrado no sandbox de instalacao"
        }
        $t2 = Get-Date
        $uninstallProc = Start-Process -FilePath $uninstaller -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" -Wait -PassThru
        $t3 = Get-Date
        Start-Sleep -Seconds 2

        $remaining = @($menuGroupKeys | Where-Object { Test-Path "HKCU:\$_" }).Count
        $exeGone = -not (Test-Path (Join-Path $flowSandbox "ZipBuddy.UI.exe"))

        if ($uninstallProc.ExitCode -ne 0 -or $remaining -ne 0 -or -not $exeGone) {
            throw "Desinstalacao incompleta: exit=$($uninstallProc.ExitCode), chaves restantes=$remaining, exe removido=$exeGone"
        }

        $installSeconds = [math]::Round(($t1 - $t0).TotalSeconds, 2)
        $uninstallSeconds = [math]::Round(($t3 - $t2).TotalSeconds, 2)
        Add-Check "A08" "Registro" "PASS" "Silencioso registra $($menuGroupKeys.Count)/$($menuGroupKeys.Count) chaves e remove todas na desinstalacao (instalar ${installSeconds}s / desinstalar ${uninstallSeconds}s)"
    }
    catch {
        Add-Check "A08" "Registro" "FAIL" $_
    }
    finally {
        foreach ($key in $menuGroupKeys) {
            try { Remove-Item -Path "HKCU:\$key" -Recurse -Force -ErrorAction SilentlyContinue } catch { }
        }
        if (Test-Path $flowSandbox) {
            try { Remove-Item -Path $flowSandbox -Recurse -Force -ErrorAction SilentlyContinue } catch { }
        }
    }
}

# -------------------------------------------------------------------------------
# A09 - Checklist manual interativo
# -------------------------------------------------------------------------------
$manualItems = @(
    @{ Id = "M01"; Title = "Menu em cascata em .zip/.7z (12 itens, idioma, icone)" }
    @{ Id = "M02"; Title = "Toggles do menu refletidos no Explorer" }
    @{ Id = "M03"; Title = "Cascata <-> flat e pt-BR <-> en-US sem chaves orfas" }
    @{ Id = "M04"; Title = "--test-archive em ZIP integro (velocidade > 0)" }
    @{ Id = "M05"; Title = "--test-archive em ZIP corrompido (CorruptedEntries)" }
    @{ Id = "M06"; Title = "Teste com senha (3 tentativas / cancelar)" }
    @{ Id = "M07"; Title = "CRC SHA (vetores CBF43926 / 995DC9BBDF1939FA)" }
    @{ Id = "M08"; Title = "Dialogo de compressao (validacoes e senha TAR/GZ)" }
    @{ Id = "M09"; Title = "Compactar e enviar por e-mail (UI nao congela)" }
    @{ Id = "M10"; Title = "Cancelar/fechar janelas sem processo orfao" }
    @{ Id = "M11"; Title = "Instalador sobre v1.0.1 + desinstalacao limpa" }
)

foreach ($item in $manualItems) {
    if ($Interactive) {
        Write-Host ""
        Write-Host ("  {0}: {1}" -f $item.Id, $item.Title) -ForegroundColor White
        $answer = Read-Host "     Resultado [P]assou / [F]alhou / [N]ao aplicavel / Enter=pulado"
        $note = Read-Host "     Evidencia / observacao (opcional)"

        $status = "PENDING"
        switch -Regex ($answer.Trim().ToUpperInvariant()) {
            "^P|^S" { $status = "PASS" }
            "^F"    { $status = "FAIL" }
            "^N"    { $status = "N/A" }
            default { $status = "PENDING" }
        }
        Add-Check $item.Id "Manual" $status "$($item.Title) | $note"
    }
    else {
        Add-Check $item.Id "Manual" "PENDING" $item.Title
    }
}

# -------------------------------------------------------------------------------
# Relatorio
# -------------------------------------------------------------------------------
$md = New-Object System.Text.StringBuilder
[void]$md.AppendLine("# Relatorio de Validacao Fase 2 - Zip Buddy")
[void]$md.AppendLine("")
[void]$md.AppendLine("Gerado em: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
[void]$md.AppendLine("")
[void]$md.AppendLine("## Ambiente")
[void]$md.AppendLine("")
[void]$md.AppendLine("| Campo | Valor |")
[void]$md.AppendLine("|:---|:---|")
foreach ($key in $script:environment.Keys) {
    [void]$md.AppendLine("| $key | $($script:environment[$key]) |")
}
[void]$md.AppendLine("")
[void]$md.AppendLine("## Resultados")
[void]$md.AppendLine("")
[void]$md.AppendLine("| ID | Categoria | Status | Detalhe |")
[void]$md.AppendLine("|:--|:---|:--|:---|")
foreach ($r in $script:results) {
    [void]$md.AppendLine("| $($r.Id) | $($r.Category) | $($r.Status) | $($r.Detail -replace '\|', '\|') |")
}
[void]$md.AppendLine("")
[void]$md.AppendLine("## Snapshot do registro (leitura)")
[void]$md.AppendLine("")
[void]$md.AppendLine("| Chave HKCU | Grupo Zip Buddy | Verbos |")
[void]$md.AppendLine("|:---|:---|:---|")
foreach ($s in $script:snapshot) {
    [void]$md.AppendLine("| $($s.Key) | $($s.Group) | $($s.Verbs) |")
}
[void]$md.AppendLine("")
$passes = @($script:results | Where-Object { $_.Status -eq "PASS" }).Count
$fails = @($script:results | Where-Object { $_.Status -eq "FAIL" }).Count
$pending = @($script:results | Where-Object { $_.Status -eq "PENDING" }).Count
$decision = if ($fails -eq 0 -and $pending -eq 0) { "GO" } else { "NO-GO / PENDENTE" }
[void]$md.AppendLine("## Decisao: **$decision**")
[void]$md.AppendLine("")
[void]$md.AppendLine("- PASS: $passes | FAIL: $fails | PENDENTE: $pending")
[void]$md.AppendLine("- Preencha `qa/PHASE2_CHECKLIST.md` com as evidencias antes do GO final.")

$reportPath = Join-Path $runDir "report.md"
$jsonPath = Join-Path $runDir "results.json"
Set-Content -Path $reportPath -Value $md.ToString() -Encoding UTF8
$script:results | ConvertTo-Json -Depth 4 | Set-Content -Path $jsonPath -Encoding UTF8

Write-Host ""
Write-Host "===============================================================================" -ForegroundColor Cyan
Write-Host "  PASS=$passes  FAIL=$fails  PENDENTE=$pending  -> $decision" -ForegroundColor White
Write-Host "  Relatorio: $reportPath" -ForegroundColor White
Write-Host "===============================================================================" -ForegroundColor Cyan

if ($fails -gt 0) { exit 1 }
exit 0
