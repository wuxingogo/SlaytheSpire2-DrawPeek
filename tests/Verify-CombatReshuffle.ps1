# Regression check for suspended combat reshuffle previews.
param([string]$GameDir='E:\SteamLibrary\steamapps\common\Slay the Spire 2')
$ErrorActionPreference='Stop'
$data=Join-Path $GameDir 'data_sts2_windows_x86_64'
foreach($dll in @('GodotSharp.dll','0Harmony.dll','sts2.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $data $dll)) | Out-Null}
$game=[MegaCrit.Sts2.Core.Models.CardModel].Assembly
$mod=[Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\dist\DrawPeek\DrawPeek.dll'))
$flags=[Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
[MegaCrit.Sts2.Core.Models.ModelDb]::Init([type[]]@($game.GetType('MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad')))
$factory=[MegaCrit.Sts2.Core.Models.ModelDb].GetMethod('Card').MakeGenericMethod($game.GetType('MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad'))
$build=$mod.GetType('DrawPeek.CombatDrawPreview').GetMethod('BuildFromPiles',$flags)
$state=[Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject([MegaCrit.Sts2.Core.Entities.Players.PlayerCombatState])
foreach($pair in @(@('Hand','Hand'),@('DrawPile','Draw'),@('DiscardPile','Discard'),@('PlayPile','Play'),@('ExhaustPile','Exhaust'))){
    $pile=[MegaCrit.Sts2.Core.Entities.Cards.CardPile]::new([Enum]::Parse([MegaCrit.Sts2.Core.Entities.Cards.PileType],$pair[1]))
    [MegaCrit.Sts2.Core.Entities.Players.PlayerCombatState].GetField("<$($pair[0])>k__BackingField",$flags).SetValue($state,$pile)
    $cards=[System.Collections.Generic.List[MegaCrit.Sts2.Core.Models.CardModel]]::new()
    1..7 | ForEach-Object {$cards.Add($factory.Invoke($null,$null).ToMutable())}
    [MegaCrit.Sts2.Core.Entities.Cards.CardPile].GetField('_cards',$flags).SetValue($pile,$cards)
}
foreach($size in @(0,1,3,5,7)){
    $draw=[System.Collections.Generic.List[MegaCrit.Sts2.Core.Models.CardModel]]::new()
    for($i=0;$i -lt $size;$i++){$draw.Add($factory.Invoke($null,$null).ToMutable())}
    [MegaCrit.Sts2.Core.Entities.Cards.CardPile].GetField('_cards',$flags).SetValue($state.DrawPile,$draw)
    $actual=$build.Invoke($null,@($state,5))
    if($actual.Count -ne [Math]::Min($size,5)){throw 'Preview contains extra cards or placeholders'}
    for($i=0;$i -lt $actual.Count;$i++){
        if(-not [object]::ReferenceEquals($actual[$i],$draw[$i])){throw 'Known draw order changed'}
    }
    if($state.DiscardPile.Cards.Count -ne 7 -or $draw.Count -ne $size){throw 'Live pile modified'}
}
'PASS: empty/short/full draw piles show only their known prefix, with no discard predictions or placeholders; existing order and piles preserved.'
