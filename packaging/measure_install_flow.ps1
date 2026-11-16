<#
.SYNOPSIS
    Mede o fluxo de instalação/desinstalação do Zip Buddy e verifica a limpeza das chaves HKCU do menu.

.DESCRIPTION
    1. Instala silenciosamente em /CURRENTUSER num diretório temporário
    2. Cria chaves HKCU falsas simulando um menu de contexto já registrado pelo app
    3. Desinstala silenciosamente (o [UninstallRun] deve remover essas chaves via --unregister-context-menu)
    4. Imprime timestamps, durações e resultado da limpeza

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File packaging\measure_install_flow.ps1
#>
Param(
    [string]$Version = "1.1.0"
)

$ErrorActionPreference = "Stop"
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$installer = Join-Path $projectRoot "dist\ZipBuddySetup-x64-v$Version.exe"

if (-not (Test-Path $installer)) {
    throw "Instalador nao encontrado: $installer"
}

$sandbox = Join-Path $env:TEMP ("ZipBuddy_Flow_" + [Guid]::NewGuid().ToString("N"))
$menuKeys = @(
    "Software\Classes\SystemFileAssociations\.zip\shell\Zip Buddy",
    "Software\Classes\SystemFileAssociations\.7z\shell\Zip Buddy",
    "Software\Classes\*\shell\Zip Buddy",
    "Software\Classes\Directory\shell\Zip Buddy"
)

function Remove-DummyKeys {
    foreach ($key in $menuKeys) {
        try { Remove-Item -Path "HKCU:\$key" -Recurse -Force -ErrorAction SilentlyContinue } catch { }
    }
}

$rows = New-Object 'System.Collections.Generic.List[object]'
function Add-Row {
    param([string]$Step, [datetime]$At, [string]$Extra)
    $rows.Add([pscustomobject]@{ Step = $Step; Time = $At.ToString("HH:mm:ss.fff"); Extra = $Extra }) | Out-Null
}

try {
    Remove-DummyKeys

    $t0 = Get-Date
    Add-Row "Inicio" $t0 "instalador: $(Split-Path $installer -Leaf)"

    $installArgs = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /CURRENTUSER /DIR=""$sandbox"""
    $installProc = Start-Process -FilePath $installer -ArgumentList $installArgs -Wait -PassThru
    $t1 = Get-Date
    Add-Row "Instalacao concluida" $t1 "exit=$($installProc.ExitCode); duracao=$([math]::Round(($t1-$t0).TotalSeconds,2))s"

    $installedExe = Join-Path $sandbox "ZipBuddy.UI.exe"
    $uninstaller = Join-Path $sandbox "unins000.exe"
    if (-not (Test-Path $installedExe) -or -not (Test-Path $uninstaller)) {
        throw "Artefatos instalados nao encontrados em $sandbox"
    }

    $registeredAfterInstall = @($menuKeys | Where-Object { Test-Path "HKCU:\$_" }).Count
    Add-Row "Menu apos instalacao silenciosa" (Get-Date) "$registeredAfterInstall/$($menuKeys.Count) chaves presentes"

    foreach ($key in $menuKeys) {
        New-Item -Path "HKCU:\$key" -Force | Out-Null
        New-ItemProperty -Path "HKCU:\$key" -Name "MUIVerb" -Value "Zip Buddy (teste)" -PropertyType String -Force | Out-Null
    }
    Add-Row "Chaves HKCU simuladas" (Get-Date) "$($menuKeys.Count) chaves criadas"

    $t2 = Get-Date
    $uninstallProc = Start-Process -FilePath $uninstaller -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" -Wait -PassThru
    $t3 = Get-Date
    Add-Row "Desinstalacao concluida" $t3 "exit=$($uninstallProc.ExitCode); duracao=$([math]::Round(($t3-$t2).TotalSeconds,2))s"

    # Give the UninstallRun child process a moment to settle
    Start-Sleep -Seconds 2

    $remaining = @($menuKeys | Where-Object { Test-Path "HKCU:\$_" })
    $exeGone = -not (Test-Path $installedExe)

    Write-Host ""
    Write-Host "===============================================================================" -ForegroundColor Cyan
    Write-Host "  ZIP BUDDY - TIMELINE DO FLUXO DE INSTALACAO (v$Version)" -ForegroundColor Cyan
    Write-Host "===============================================================================" -ForegroundColor Cyan
    $rows | Format-Table -AutoSize | Out-Host
    Write-Host "-------------------------------------------------------------------------------"
    Write-Host "  Paginas do wizard (WizardStyle=modern):" -ForegroundColor White
    Write-Host "    1. Welcome" -ForegroundColor Gray
    Write-Host "    2. Choose Destination Location" -ForegroundColor Gray
    Write-Host "    3. Select Additional Tasks (desktop, updates, associacoes)" -ForegroundColor Gray
    Write-Host "    4. Ready to Install" -ForegroundColor Gray
    Write-Host "    5. Installing (progresso)" -ForegroundColor Gray
    Write-Host "    6. Installation Complete" -ForegroundColor Gray
    Write-Host "    (Program Group desativado; sem pagina de licenca - DisableProgramGroupPage=yes)"
    Write-Host "-------------------------------------------------------------------------------"
    Write-Host "  Exe removido apos desinstalacao: $exeGone" -ForegroundColor $(if ($exeGone) { "Green" } else { "Red" })
    Write-Host "  Chaves HKCU restantes: $($remaining.Count)/$($menuKeys.Count)" -ForegroundColor $(if ($remaining.Count -eq 0) { "Green" } else { "Red" })
    if ($remaining.Count -gt 0) {
        $remaining | ForEach-Object { Write-Host "    RESIDUO: $_" -ForegroundColor Red }
    }
    Write-Host "===============================================================================" -ForegroundColor Cyan

    if (-not $exeGone -or $remaining.Count -gt 0) { exit 1 }
}
finally {
    Remove-DummyKeys
    if (Test-Path $sandbox) {
        try { Remove-Item -Path $sandbox -Recurse -Force -ErrorAction SilentlyContinue } catch { }
    }
}
