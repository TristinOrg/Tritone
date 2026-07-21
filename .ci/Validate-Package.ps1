$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $root 'package.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

if ($manifest.name -ne 'com.tristinwen.tritone') {
    throw "Unexpected package name '$($manifest.name)'."
}
if ($manifest.unity -ne '2022.3') {
    throw "The package must retain Unity 2022.3 compatibility."
}

$assetRoots = @('Runtime', 'Editor', 'Tests')
$guids = @{}
foreach ($assetRoot in $assetRoots) {
    $path = Join-Path $root $assetRoot
    foreach ($item in Get-ChildItem -LiteralPath $path -Recurse -Force) {
        if ($item.Name.EndsWith('.meta', [StringComparison]::OrdinalIgnoreCase)) {
            $assetPath = $item.FullName.Substring(0, $item.FullName.Length - 5)
            if (-not (Test-Path -LiteralPath $assetPath)) {
                throw "Orphaned meta file: $($item.FullName)"
            }

            $guidLine = Select-String -LiteralPath $item.FullName -Pattern '^guid: ([0-9a-f]{32})$'
            if (-not $guidLine) {
                throw "Missing or invalid GUID: $($item.FullName)"
            }
            $guid = $guidLine.Matches[0].Groups[1].Value
            if ($guids.ContainsKey($guid)) {
                throw "Duplicate GUID '$guid': $($guids[$guid]) and $($item.FullName)"
            }
            $guids.Add($guid, $item.FullName)
            continue
        }

        $metaPath = "$($item.FullName).meta"
        if (-not (Test-Path -LiteralPath $metaPath)) {
            throw "Missing meta file: $($item.FullName)"
        }
    }
}

$runtimeEditorReferences = Get-ChildItem -LiteralPath (Join-Path $root 'Runtime') -Recurse -Filter '*.cs' |
    Select-String -Pattern '^\s*using\s+UnityEditor'
if ($runtimeEditorReferences) {
    throw "Runtime source contains UnityEditor references: $($runtimeEditorReferences.Path -join ', ')"
}

Write-Host "Validated $($guids.Count) Unity assets and package manifest '$($manifest.name)@$($manifest.version)'."
