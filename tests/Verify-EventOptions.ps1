param([string]$GameDir='G:\SteamLibrary\steamapps\common\Slay the Spire 2')
$ErrorActionPreference='Stop'
$data=Join-Path $GameDir 'data_sts2_windows_x86_64'
[System.Reflection.Assembly]::LoadFrom((Join-Path $data 'GodotSharp.dll')) | Out-Null
[System.Reflection.Assembly]::LoadFrom((Join-Path $data 'sts2.dll')) | Out-Null
$mod=[System.Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\dist\DrawPeek\DrawPeek.dll'))
$flags=[System.Reflection.BindingFlags]'Public,NonPublic,Static'
$helper=$mod.GetType('DrawPeek.EventOptionPreview',$true)
$plain=$helper.GetMethod('PlainText',$flags)
$sample=$plain.Invoke($null,@('Gain [green]{Amount}[/green] HP. {When:cond|{Nested}}'))
if($sample -ne 'Gain ? HP. ?'){throw "Unexpected placeholder handling: $sample"}
foreach($language in @('eng','zhs')) {
    $path=Join-Path $PSScriptRoot "..\.inspect\$language-events.json"
    $dictionary=Get-Content -LiteralPath $path -Raw -Encoding utf8 | ConvertFrom-Json -AsHashtable
    $reader=[System.Func[string,string]]{param($key) $dictionary[$key]}
    $prefixes=@($dictionary.Keys | Where-Object {$_ -match '\.pages\.INITIAL\.options\..*\.title$'} | ForEach-Object {$_ -replace '(\.pages\.INITIAL\.options\.).*','$1'} | Sort-Object -Unique)
    $total=0
    foreach($prefix in $prefixes) {
        $lines=$helper.GetMethod('FormatOptions',$flags).Invoke($null,@([string[]]$dictionary.Keys,$reader,[string]$prefix))
        $expected=@($dictionary.Keys | Where-Object {$_.StartsWith($prefix) -and $_.EndsWith('.title')}).Count
        if($lines.Count -ne $expected){throw "Missing options for $prefix"}
        foreach($line in $lines) {if($line -match '\{[^{}]*\}|\[/?\w+\]'){throw "Unresolved markup: $line"}}
        $total+=$lines.Count
    }
    $absent=$helper.GetMethod('FormatOptions',$flags).Invoke($null,@([string[]]$dictionary.Keys,$reader,'NONEXISTENT.pages.INITIAL.options.'))
    if($absent.Count -ne 0){throw 'Missing event must produce no options'}
    "$language : $($prefixes.Count) events, $total initial options, missing-event fallback PASS"
}

