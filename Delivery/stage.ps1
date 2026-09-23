# Stages the delivery folder (zip its CONTENTS, not the folder itself):
#   <Target>\index.html   Playworks export (already there, or copied from -Html)
#   <Target>\README.md    copied from Delivery/README.md
#   <Target>\source\      game source only (scripts, config, scene, manifest, luna.json);
#                         the full Unity project is on GitHub (linked in the README)
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

# Game source only (the full Unity project is on GitHub, linked in the README).
$source = Join-Path $Target 'source'
Remove-Item $source -Recurse -Force -ErrorAction SilentlyContinue
$sourceItems = @(
    'Assets\Game\Bubbles\Scripts',
    'Assets\Game\Bubbles\Config',
    'Assets\Game\InteractiveEndCardBuilder\Scripts',
    'Assets\Game\Shared\Scripts\AudioManager.cs',
    'Assets\Game\Shared\Scripts\RunRestarter.cs',
    'Assets\Game\Shared\Scripts\SoundID.cs',
    'Assets\Game\Core\Scripts\Singleton\AbstractSingleton.cs',
    'Assets\Game\Core\Scripts\Data\AudioSettings.cs',
    'Assets\Game\Shared\Scenes\Boot.unity',
    'Packages\manifest.json',
    'luna.json'
)
foreach ($item in $sourceItems) {
    $from = Join-Path $repo $item
    if (-not (Test-Path $from)) { Write-Warning "Missing source item: $item"; continue }
    $to = Join-Path $source $item
    New-Item -ItemType Directory -Force (Split-Path $to -Parent) | Out-Null
    Copy-Item $from $to -Recurse -Force
}
# Readable source only: drop Unity .meta files.
Get-ChildItem $source -Recurse -Filter *.meta | Remove-Item -Force

# Report
$todo = (Select-String (Join-Path $Target 'README.md') -Pattern 'TODO').Count
$total = (Get-ChildItem $Target -Recurse -File | Measure-Object Length -Sum).Sum
Write-Host "Staged: $Target"
Write-Host ("Uncompressed total: {0:N0} bytes" -f $total)
if ($todo -gt 0) { Write-Warning "README.md still has $todo TODO(s)." }

Write-Host ("source/: {0:N0} bytes in {1} files" -f (Get-ChildItem $source -Recurse -File | Measure-Object Length -Sum).Sum, (Get-ChildItem $source -Recurse -File).Count)

if ($Zip) {
    $outDir = Split-Path $Target -Parent
    $zipPath = Join-Path $outDir ("SimulaAd-Playable-{0}.zip" -f (Get-Date -Format 'yyyyMMdd-HHmm'))
    Compress-Archive -Path (Join-Path $Target '*') -DestinationPath $zipPath -Force
    $size = (Get-Item $zipPath).Length
    Write-Host ("Zip: {0}  ({1:N0} bytes)" -f $zipPath, $size)
    if ($size -gt $limit) { Write-Warning ("Zip exceeds {0:N0} bytes by {1:N0}." -f $limit, ($size - $limit)) }
    else { Write-Host ("OK: {0:N0} bytes under the limit." -f ($limit - $size)) }
}
