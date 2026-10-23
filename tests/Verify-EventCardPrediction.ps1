param([string]$GameDir='E:\SteamLibrary\steamapps\common\Slay the Spire 2')
$ErrorActionPreference='Stop'
$data=Join-Path $GameDir 'data_sts2_windows_x86_64'
[System.Reflection.Assembly]::LoadFrom((Join-Path $data 'GodotSharp.dll')) | Out-Null
[System.Reflection.Assembly]::LoadFrom((Join-Path $data '0Harmony.dll')) | Out-Null
$game=[System.Reflection.Assembly]::LoadFrom((Join-Path $data 'sts2.dll'))
$mod=[System.Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\dist\DrawPeek\DrawPeek.dll'))
$flags=[System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
$db=$game.GetType('MegaCrit.Sts2.Core.Models.ModelDb')
$cardType=$game.GetType('MegaCrit.Sts2.Core.Models.CardModel')
$eventType=$game.GetType('MegaCrit.Sts2.Core.Models.EventModel')
$names=@('Cards.StrikeIronclad','Cards.DefendIronclad','Events.DoorsOfLightAndDark','Events.EndlessConveyor','Events.Reflections')
$types=[type[]]@($names | ForEach-Object {$game.GetType("MegaCrit.Sts2.Core.Models.$_",$true)})
$db.GetMethod('Init').Invoke($null,@(,$types)) | Out-Null
function Get-Model([string]$kind,[string]$name) {
    $type=$game.GetType("MegaCrit.Sts2.Core.Models.$kind.$name",$true)
    $method=if($kind -eq 'Cards'){'Card'}else{'Event'}
    $db.GetMethod($method).MakeGenericMethod($type).Invoke($null,$null)
}
$playerType=$game.GetType('MegaCrit.Sts2.Core.Entities.Players.Player')
$player=[System.Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject($playerType)
$pileType=$game.GetType('MegaCrit.Sts2.Core.Entities.Cards.CardPile')
$pileEnum=$game.GetType('MegaCrit.Sts2.Core.Entities.Cards.PileType')
$pile=[Activator]::CreateInstance($pileType,@([Enum]::Parse($pileEnum,'Deck')))
$playerType.GetField('<Deck>k__BackingField',$flags).SetValue($player,$pile)
$cards=[Activator]::CreateInstance([System.Collections.Generic.List``1].MakeGenericType($cardType))
for($i=0;$i -lt 6;$i++) {
    $name=if($i % 2 -eq 0){'StrikeIronclad'}else{'DefendIronclad'}
    $card=(Get-Model 'Cards' $name).ToMutable()
    $card.Owner=$player
    if($i -lt 2){$card.UpgradeInternal();$card.FinalizeUpgradeInternal()}
    $cards.Add($card)
}
$pileType.GetField('_cards',$flags).SetValue($pile,$cards)
$rngType=$game.GetType('MegaCrit.Sts2.Core.Random.Rng')
$predict=$mod.GetType('DrawPeek.EventCardPrediction').GetMethod('ForOption',$flags)
$cases=@(
    @('DoorsOfLightAndDark','DOORS_OF_LIGHT_AND_DARK.pages.INITIAL.options.LIGHT',2),
    @('EndlessConveyor','ENDLESS_CONVEYOR.pages.INITIAL.options.OBSERVE_CHEF',1),
    @('EndlessConveyor','ENDLESS_CONVEYOR.pages.ALL.options.SPICY_SNAPPY',1),
    @('Reflections','REFLECTIONS.pages.INITIAL.options.TOUCH_A_MIRROR',6)
)
foreach($case in $cases) {
    $event=(Get-Model 'Events' $case[0]).ToMutable()
    $eventType.GetField('<Owner>k__BackingField',$flags).SetValue($event,$player)
    $rng=if($rngType.GetConstructor([type[]]@([uint64]))){
        [Activator]::CreateInstance($rngType,@([uint64]12345))
    }else{[Activator]::CreateInstance($rngType,@([uint32]12345,[int]13))}
    $eventType.GetField('<Rng>k__BackingField',$flags).SetValue($event,$rng)
    $before=($cards | ForEach-Object {$_.CurrentUpgradeLevel}) -join ','
    $rngSnapshot={if($rngType.GetMethod('ToSerializable')){ $rng.ToSerializable() | ConvertTo-Json -Compress -Depth 10 }else{"$($rng.Seed):$($rng.Counter)"}}
    $counter=& $rngSnapshot
    $first=$predict.Invoke($null,@($event,[string]$case[1]))
    $again=$predict.Invoke($null,@($event,[string]$case[1]))
    if($first.Count -ne $case[2]){throw "Wrong prediction count for $($case[0]): $($first.Count)"}
    $snapshot={param($list) ($list | ForEach-Object {"$($_.Id):$($_.CurrentUpgradeLevel)"}) -join ','}
    if((& $snapshot $first) -ne (& $snapshot $again)){throw 'Repeated hovering changed results'}
    if((& $rngSnapshot) -ne $counter){throw 'Preview advanced live event RNG'}
    if((($cards | ForEach-Object {$_.CurrentUpgradeLevel}) -join ',') -ne $before){throw 'Preview changed real upgrade levels'}
    foreach($result in $first) {foreach($original in $cards) {if([object]::ReferenceEquals($result,$original)){throw 'Preview leaked a live card'}}}
    "$($case[0]): $($first.Count) results; repeatability, RNG and deck isolation PASS"
}


$predictor=$mod.GetType('DrawPeek.EventCardPrediction')
$copyMethod=$predictor.GetMethod('MaterializeTransform',$flags)
$canonical=Get-Model 'Cards' 'DefendIronclad'
$beforeCount=$cards.Count
$transformed=$copyMethod.Invoke($null,@($cards[0],$canonical))
if([object]::ReferenceEquals($transformed,$canonical) -or [object]::ReferenceEquals($transformed,$cards[0])){throw 'Transform copy aliases existing card'}
if($transformed.Id -ne $canonical.Id -or $transformed.Owner -ne $player){throw 'Incorrect transform preview model'}
if($cards.Count -ne $beforeCount -or $cards.Contains($transformed)){throw 'Transform preview registered a live deck card'}
if($transformed.CurrentUpgradeLevel -ne 0){throw 'Default transformations should not inherit upgrades'}
'Unregistered transformation copy and owner/upgrade isolation PASS'
$known=$predictor.GetMethod('IsTransformOption',$flags)
if(-not $known.Invoke($null,@('AROMA_OF_CHAOS.pages.INITIAL.options.LET_GO'))){throw 'Known transform option not recognized'}
if($known.Invoke($null,@('UNKNOWN.pages.INITIAL.options.TRANSFORM'))){throw 'Unknown option must not be guessed'}
'Known-option whitelist and unknown-option fallback PASS'
$hoverType=$game.GetType('MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet')
$controlType=[Godot.Control]
$tipType=$game.GetType('MegaCrit.Sts2.Core.HoverTips.IHoverTip')
$tipSequence=[System.Collections.Generic.IEnumerable``1].MakeGenericType($tipType)
$alignmentType=$game.GetType('MegaCrit.Sts2.Core.HoverTips.HoverTipAlignment')
$target=$hoverType.GetMethod('CreateAndShow',[type[]]@($controlType,$tipSequence,$alignmentType))
if($null -eq $target){throw 'Native hover target missing'}
'Native hover method signature resolution PASS (not a live UI test)'
