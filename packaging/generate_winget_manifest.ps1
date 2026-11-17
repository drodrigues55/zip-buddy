# Zip Buddy Winget Manifest Generator Script
Param(
    [string]$Version = "1.1.0",
    [string]$InstallerPath = "",
    [string]$ReleaseBaseUrl = "https://github.com/drodrigues55/zip-buddy/releases/download/v"
)

$ErrorActionPreference = "Stop"

if (-not $InstallerPath) {
    $InstallerPath = Join-Path $PSScriptRoot "..\dist\ZipBuddySetup-x64-v$Version.exe"
}

if (-not (Test-Path $InstallerPath)) {
    Write-Error "Installer file not found at: $InstallerPath. Run build_installer.ps1 first."
}

Write-Host "[*] Calculating SHA-256 for: $InstallerPath" -ForegroundColor Cyan
$sha256 = (Get-FileHash -Path $InstallerPath -Algorithm SHA256).Hash.ToUpperInvariant()
Write-Host "[+] SHA-256: $sha256" -ForegroundColor Green

$manifestDir = Join-Path $PSScriptRoot "winget\manifests\z\ZipBuddy\ZipBuddy\$Version"
if (-not (Test-Path $manifestDir)) {
    New-Item -ItemType Directory -Path $manifestDir -Force | Out-Null
}

$installerUrl = "$ReleaseBaseUrl$Version/ZipBuddySetup-x64-v$Version.exe"

# 1. Version Manifest
$versionYaml = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.1.28.0.schema.json

PackageIdentifier: ZipBuddy.ZipBuddy
PackageVersion: $Version
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.28.0
"@

# 2. Installer Manifest
$installerYaml = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.1.28.0.schema.json

PackageIdentifier: ZipBuddy.ZipBuddy
PackageVersion: $Version
InstallerType: inno
Scope: machine
InstallModes:
  - interactive
  - silent
  - silentWithProgress
InstallerSwitches:
  Silent: /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-
  SilentWithProgress: /SILENT /SUPPRESSMSGBOXES /NORESTART /SP-
UpgradeBehavior: install
FileExtensions:
  - zip
  - 7z
  - rar
  - tar
  - gz
  - bz2
ProductCode: '{8B036367-AE8C-4D88-B1F3-E18F2E23A534}_is1'
Installers:
  - Architecture: x64
    InstallerUrl: $installerUrl
    InstallerSha256: $sha256
ManifestType: installer
ManifestVersion: 1.28.0
"@

# 3. Locale Manifest
$localeYaml = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.1.28.0.schema.json

PackageIdentifier: ZipBuddy.ZipBuddy
PackageVersion: $Version
PackageLocale: en-US
Publisher: Zip Buddy Open Source Team
PublisherUrl: https://github.com/drodrigues55/zip-buddy
PublisherSupportUrl: https://github.com/drodrigues55/zip-buddy/issues
PackageName: Zip Buddy
PackageUrl: https://github.com/drodrigues55/zip-buddy
License: MIT
LicenseUrl: https://github.com/drodrigues55/zip-buddy/blob/main/LICENSE
Copyright: Copyright (c) 2026 Zip Buddy Open Source Team
ShortDescription: 100% free, fast, and secure native Windows archive manager built with .NET 10 and WinUI 3.
Description: |-
  Zip Buddy is a modern, high-performance Windows desktop archive manager inspired by the workflows of Bandizip and 7-Zip.
  Features:
  - 100% free, zero ads, zero telemetry, no subscriptions.
  - Native Windows 10/11 WinUI 3 interface with dark/light theme support.
  - Streaming ZIP, 7Z, TAR, GZ, and RAR archive reading and creation.
  - Archive integrity testing and CRC-32 / CRC-64 / SHA-1 / SHA-256 checksum calculation.
  - Compress-and-e-mail workflows with Explorer context-menu integration.
  - Built-in Zip Slip, directory traversal, and decompression bomb protection.
  - Explorer drag-and-drop integration.
Moniker: zip-buddy
Tags:
  - archive
  - archiver
  - compression
  - decompression
  - zip
  - 7zip
  - rar
  - extractor
ReleaseNotesUrl: https://github.com/drodrigues55/zip-buddy/releases/tag/v$Version
ManifestType: defaultLocale
ManifestVersion: 1.28.0
"@

# 4. Portuguese (Brazil) Locale Manifest
$localePtBrYaml = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.locale.1.28.0.schema.json

PackageIdentifier: ZipBuddy.ZipBuddy
PackageVersion: $Version
PackageLocale: pt-BR
Publisher: Zip Buddy Open Source Team
PublisherUrl: https://github.com/drodrigues55/zip-buddy
PublisherSupportUrl: https://github.com/drodrigues55/zip-buddy/issues
PackageName: Zip Buddy
PackageUrl: https://github.com/drodrigues55/zip-buddy
License: MIT
LicenseUrl: https://github.com/drodrigues55/zip-buddy/blob/main/LICENSE
Copyright: Copyright (c) 2026 Zip Buddy Open Source Team
ShortDescription: Gerenciador de arquivos compactados nativo para Windows, 100% gratuito, rápido e seguro.
Description: |-
  O Zip Buddy é um gerenciador de compactação e descompactação moderno e veloz para Windows, inspirado no fluxo do Bandizip e 7-Zip.
  Destaques:
  - 100% gratuito, sem propagandas, sem telemetria e sem planos pagos.
  - Interface nativa em WinUI 3 compatível com Windows 10 e 11, com suporte a temas Claro e Escuro.
  - Leitura e criação em streaming para formatos ZIP, 7Z, TAR, GZ e RAR.
  - Teste de integridade de arquivos e cálculo de CRC-32, CRC-64, SHA-1 e SHA-256.
  - Compactação com envio por e-mail e integração com o menu de contexto do Explorer.
  - Proteção avançada integrada contra Zip Slip, path traversal e descompressão maliciosa (Zip Bombs).
  - Suporte completo aos idiomas Português do Brasil e Inglês.
Tags:
  - arquivo
  - compactador
  - compactação
  - descompactador
  - zip
  - 7zip
  - rar
  - extrator
ReleaseNotesUrl: https://github.com/drodrigues55/zip-buddy/releases/tag/v$Version
ManifestType: locale
ManifestVersion: 1.28.0
"@

# Write files with UTF-8 without BOM or standard UTF-8
[System.IO.File]::WriteAllText((Join-Path $manifestDir "ZipBuddy.ZipBuddy.yaml"), $versionYaml, [System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllText((Join-Path $manifestDir "ZipBuddy.ZipBuddy.installer.yaml"), $installerYaml, [System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllText((Join-Path $manifestDir "ZipBuddy.ZipBuddy.locale.en-US.yaml"), $localeYaml, [System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllText((Join-Path $manifestDir "ZipBuddy.ZipBuddy.locale.pt-BR.yaml"), $localePtBrYaml, [System.Text.Encoding]::UTF8)

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "[+] Winget manifests created successfully in:" -ForegroundColor Green
Write-Host "    $manifestDir" -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Green
