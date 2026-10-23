param(
    [string]$GameDir = 'E:\SteamLibrary\steamapps\common\Slay the Spire 2',
    [string]$ModDll = (Join-Path $PSScriptRoot '..\dist\DrawPeek\DrawPeek.dll')
)
$ErrorActionPreference = 'Stop'
$data = Join-Path $GameDir 'data_sts2_windows_x86_64'
[System.Reflection.Assembly]::LoadFrom((Join-Path $data 'GodotSharp.dll')) | Out-Null
$game = [System.Reflection.Assembly]::LoadFrom((Join-Path $data 'sts2.dll'))
$mod = [System.Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $ModDll).Path)
$flags = [System.Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$compat = $mod.GetType('DrawPeek.GameCompat', $true)
$cloneMethod = $compat.GetMethod('CloneRng', $flags)
$rngType = $game.GetType('MegaCrit.Sts2.Core.Random.Rng', $true)
foreach ($seed in @([uint32]0, [uint32]12345, [uint32]4294967295)) {
    foreach ($counter in @(0, 1, 37)) {
        $source = [Activator]::CreateInstance($rngType, @($seed, [int]$counter))
        $before = $rngType.GetProperty('Counter').GetValue($source)
        $clone = $cloneMethod.Invoke($null, @($source))
        if ([object]::ReferenceEquals($source, $clone)) { throw 'RNG clone aliases live RNG' }
        if ($rngType.GetProperty('Counter').GetValue($source) -ne $before) { throw 'Cloning advanced live RNG' }
        $next = $rngType.GetMethod('NextInt', [type[]]@([int], [int]))
        for ($i = 0; $i -lt 100; $i++) {
            $a = $next.Invoke($source, @([int]0, [int]1000000))
            $b = $next.Invoke($clone, @([int]0, [int]1000000))
            if ($a -ne $b) { throw "RNG sequence mismatch: seed=$seed counter=$counter draw=$i" }
        }
    }
}
'PASS: 9 RNG snapshots, 900 matching draws; cloning never advances the source.'
$potion = $game.GetType('MegaCrit.Sts2.Core.Odds.PotionRewardOdds', $true)
$roll = $potion.GetMethods() | Where-Object Name -eq 'Roll'
if ($roll.Count -ne 1 -or $roll.GetParameters().Length -notin @(2,3)) { throw 'Unsupported potion API' }
if ($roll.GetParameters().Length -eq 3) {
    $ascensionType = $roll.GetParameters()[1].ParameterType
    foreach ($level in @(0, 10)) {
        [Activator]::CreateInstance($ascensionType, @([int]$level)) | Out-Null
    }
}
'PASS: potion API signature and old-version ascension context construction.'
$cardFlags = $compat.GetField('CombatCardFlags', $flags).GetValue($null)
"Combat card flags: $cardFlags"
$version = [System.Reflection.AssemblyName]::GetAssemblyName((Resolve-Path -LiteralPath $ModDll).Path).Version
"Tested mod assembly: $version; game: $((Get-Content (Join-Path $GameDir 'release_info.json') -Raw | ConvertFrom-Json).version)"

