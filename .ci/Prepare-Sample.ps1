param(
    [string] $ProjectPath = (Join-Path $PSScriptRoot 'TestProject')
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'Samples~/AfterHours'
$assets = Join-Path $ProjectPath 'Assets'
if (-not (Test-Path -LiteralPath $assets -PathType Container)) {
    throw "Unity project Assets directory was not found: $assets"
}

$destination = Join-Path $assets 'AfterHours'
if ((Test-Path -LiteralPath $destination) -or (Test-Path -LiteralPath "$destination.meta")) {
    throw "The sample destination already exists at '$destination'. Use a clean test project or move the existing copy before preparing the sample."
}

Copy-Item -LiteralPath $source -Destination $destination -Recurse
Write-Host "Prepared the After Hours sample at '$destination'."
