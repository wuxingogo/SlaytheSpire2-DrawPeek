param(
    [string]$WorkshopDir = (Join-Path $PSScriptRoot '..\ModUploader\DrawPeekWorkshop')
)
$ErrorActionPreference = 'Stop'
$WorkshopDir = (Resolve-Path -LiteralPath $WorkshopDir).Path
$configPath = Join-Path $PSScriptRoot 'workshop.json'
$externalPath = Join-Path $WorkshopDir 'workshop.json'
$source = Get-Content -LiteralPath $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
$external = Get-Content -LiteralPath $externalPath -Raw -Encoding UTF8 | ConvertFrom-Json
$idPath = Join-Path $WorkshopDir 'mod_id.txt'
$originalId = [IO.File]::ReadAllText($idPath)
if ($originalId.Trim() -notmatch '^\d+$') { throw 'Existing workshop ID is missing or invalid.' }
# Preserve publisher-managed fields; only release text comes from the project.
foreach ($field in @('description', 'changeNote')) {
    $external | Add-Member -NotePropertyName $field -NotePropertyValue $source.$field -Force
}
$bytes = [Text.Encoding]::UTF8.GetByteCount([string]$external.description)
if ($bytes -ge 8000) { throw "Workshop description exceeds the limit: $bytes bytes." }
$package = Join-Path $PSScriptRoot 'dist\DrawPeek'
$manifest = Get-Content -LiteralPath (Join-Path $package 'DrawPeek.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$projectManifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'DrawPeek.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $package 'DrawPeek.dll')).Version
if ($manifest.id -ne 'DrawPeek' -or $manifest.version -ne $projectManifest.version -or $assemblyVersion.ToString(3) -ne $manifest.version) {
    throw 'Package versions do not match. Run build.ps1 first.'
}
$content = Join-Path $WorkshopDir 'content'
New-Item -ItemType Directory -Path $content -Force | Out-Null
foreach ($file in @('DrawPeek.dll', 'DrawPeek.json')) {
    $from = Join-Path $package $file
    $to = Join-Path $content $file
    Copy-Item -LiteralPath $from -Destination $to -Force
    if ((Get-FileHash -LiteralPath $from).Hash -ne (Get-FileHash -LiteralPath $to).Hash) { throw "Hash mismatch: $file" }
}
$json = $external | ConvertTo-Json -Depth 30
$utf8 = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText($configPath, $json + [Environment]::NewLine, $utf8)
Copy-Item -LiteralPath $configPath -Destination $externalPath -Force
$verified = Get-Content -LiteralPath $externalPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ((Get-FileHash -LiteralPath $configPath).Hash -ne (Get-FileHash -LiteralPath $externalPath).Hash) { throw 'Workshop config mismatch.' }
if ([IO.File]::ReadAllText($idPath) -cne $originalId) { throw 'Workshop ID changed unexpectedly.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'UploadDrawPeek.bat') -Destination (Split-Path $WorkshopDir -Parent) -Force
Write-Host "Workshop synchronized: $WorkshopDir (v$($manifest.version), ID $($originalId.Trim()), description $bytes bytes). Not uploaded."
