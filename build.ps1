param(
    [string]$GameDir = 'E:\SteamLibrary\steamapps\common\Slay the Spire 2',
    [switch]$Install,
    [switch]$Launch,
    [string]$RitsuLibDir = 'E:\SteamLibrary\steamapps\workshop\content\2868840\3747602295'
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath (Join-Path $GameDir 'data_sts2_windows_x86_64\sts2.dll'))) {
    throw 'Game not found. Pass -GameDir with your game installation directory.'
}
if (($Install -or $Launch) -and (Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue)) {
    throw 'Please close the game before installing or launching a new build.'
}
& dotnet build (Join-Path $PSScriptRoot 'DrawPeek.csproj') -c Release "-p:GameDir=$GameDir" "-p:RitsuLibDir=$RitsuLibDir"
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$package = Join-Path $PSScriptRoot 'dist\DrawPeek'
New-Item -ItemType Directory -Path $package -Force | Out-Null
foreach ($file in @('DrawPeek.dll', 'DrawPeek.json')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot "bin\Release\net9.0\$file") -Destination $package -Force
}
Write-Host "Package ready: $package"
if ($Install -or $Launch) {
    $destination = Join-Path $GameDir 'mods\DrawPeek'
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    foreach ($file in @('DrawPeek.dll', 'DrawPeek.json')) {
        Copy-Item -LiteralPath (Join-Path $package $file) -Destination $destination -Force
    }
    Write-Host "Installed: $destination"
}
if ($Launch) { Start-Process 'steam://rungameid/2868840' }

