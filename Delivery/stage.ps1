# Stages the delivery folder (zip its CONTENTS, not the folder itself):
#   <Target>\index.html   Playworks export (already there, or copied from -Html)
#   <Target>\README.md    copied from Delivery/README.md
#   <Target>\source\      Assets/, Packages/manifest.json, ProjectSettings/, luna.json
#
# Usage:
#   .\Delivery\stage.ps1                                   # refresh README + source
#   .\Delivery\stage.ps1 -Html "<export>\X_unityads.html"  # also replace index.html
#   .\Delivery\stage.ps1 -Zip                              # also build the zip and check the 5,000,000-byte limit

param(
    [string]$Target = "$env:USERPROFILE\Documents\Builds\buildWeb",
    [string]$Html,
    [switch]$Zip
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$limit = 5000000

New-Item -ItemType Directory -Force $Target | Out-Null

if ($Html) {
    Copy-Item $Html (Join-Path $Target 'index.html') -Force
}
if (-not (Test-Path (Join-Path $Target 'index.html'))) {
    throw "index.html missing in $Target. Pass -Html <exported *_unityads.html>."
}

# Playworks export extras must not ship (urls.txt points to an external host).
foreach ($extra in 'README.txt', 'urls.txt') {
    Remove-Item (Join-Path $Target $extra) -Force -ErrorAction SilentlyContinue
}

Copy-Item (Join-Path $repo 'Delivery\README.md') (Join-Path $Target 'README.md') -Force

$source = Join-Path $Target 'source'
Remove-Item $source -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force (Join-Path $source 'Packages') | Out-Null
Copy-Item (Join-Path $repo 'Assets') $source -Recurse -Force
Copy-Item (Join-Path $repo 'ProjectSettings') $source -Recurse -Force
Copy-Item (Join-Path $repo 'Packages\manifest.json') (Join-Path $source 'Packages') -Force
Copy-Item (Join-Path $repo 'luna.json') $source -Force

# Report
$todo = (Select-String (Join-Path $Target 'README.md') -Pattern 'TODO').Count
$total = (Get-ChildItem $Target -Recurse -File | Measure-Object Length -Sum).Sum
Write-Host "Staged: $Target"
Write-Host ("Uncompressed total: {0:N0} bytes" -f $total)
if ($todo -gt 0) { Write-Warning "README.md still has $todo TODO(s)." }

Write-Host "Largest folders in source/Assets:"
Get-ChildItem (Join-Path $source 'Assets') -Directory -Recurse -Depth 1 |
    ForEach-Object { [pscustomobject]@{ Folder = $_.FullName.Substring($source.Length + 1); Bytes = (Get-ChildItem $_.FullName -Recurse -File | Measure-Object Length -Sum).Sum } } |
    Sort-Object Bytes -Descending | Select-Object -First 8 |
    ForEach-Object { Write-Host ("  {0,14:N0}  {1}" -f $_.Bytes, $_.Folder) }

if ($Zip) {
    $outDir = Split-Path $Target -Parent
    $zipPath = Join-Path $outDir ("SimulaAd-Playable-{0}.zip" -f (Get-Date -Format 'yyyyMMdd-HHmm'))
    Compress-Archive -Path (Join-Path $Target '*') -DestinationPath $zipPath -Force
    $size = (Get-Item $zipPath).Length
    Write-Host ("Zip: {0}  ({1:N0} bytes)" -f $zipPath, $size)
    if ($size -gt $limit) { Write-Warning ("Zip exceeds {0:N0} bytes by {1:N0}." -f $limit, ($size - $limit)) }
    else { Write-Host ("OK: {0:N0} bytes under the limit." -f ($limit - $size)) }
}
