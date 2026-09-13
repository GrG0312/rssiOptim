<#
.SYNOPSIS
    Kiadási csomagot készít: platformonként egyetlen, önmagában futtatható fájlt
    a mintaadatokkal és a README-vel együtt, ZIP-be csomagolva.

.DESCRIPTION
    A kimenet telepített .NET futtatókörnyezet nélkül is elindul - a runtime bele van
    fordítva az exe-be. NuGet-függőség nincs, így a csomag valóban önálló.

    Az eredmény a publish/ könyvtárba kerül:
      publish/<rid>/            a kicsomagolt csomag (exe + data/ + README.md)
      publish/rssi-calibration-<verzió>-<rid>.zip

.PARAMETER Version
    A kiadás verziószáma, a ZIP nevébe kerül. Alapértelmezés: 1.0.0

.PARAMETER Rid
    A cél futtatókörnyezetek azonosítói. Alapértelmezés: win-x64

.EXAMPLE
    .\publish.ps1
    .\publish.ps1 -Version 1.0.0 -Rid win-x64, linux-x64
#>
[CmdletBinding()]
param(
    [string] $Version = '1.0.0',
    [string[]] $Rid = @('win-x64')
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$project = Join-Path $root 'RssiCalibration.Cli\RssiCalibration.Cli.csproj'
$publishRoot = Join-Path $root 'publish'

if (Test-Path $publishRoot) { Remove-Item $publishRoot -Recurse -Force }
New-Item -ItemType Directory -Path $publishRoot | Out-Null

foreach ($currentRid in $Rid) {
    $stage = Join-Path $publishRoot $currentRid
    Write-Host "==> $currentRid" -ForegroundColor Cyan

    dotnet publish $project `
        --configuration Release `
        --runtime $currentRid `
        --output $stage `
        "-p:Version=$Version"

    if ($LASTEXITCODE -ne 0) { throw "A publish nem sikerult: $currentRid" }

    # A README a csomagban is legyen ott - a data/ mappát a build már odamásolta.
    Copy-Item (Join-Path $root 'README.md') $stage

    $zip = Join-Path $publishRoot "rssi-calibration-$Version-$currentRid.zip"
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force

    $sizeMb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
    Write-Host "    $zip ($sizeMb MB)" -ForegroundColor Green
}

Write-Host ''
Write-Host 'Kesz. A ZIP fajlokat toltsd fel a GitHub release-hez.' -ForegroundColor Green
