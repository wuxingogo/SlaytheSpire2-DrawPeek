param([string]$GameDir='G:\SteamLibrary\steamapps\common\Slay the Spire 2')
$ErrorActionPreference='Stop'
$data=Join-Path $GameDir 'data_sts2_windows_x86_64'
foreach($dll in @('GodotSharp.dll','0Harmony.dll','sts2.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $data $dll)) | Out-Null}
$game=[MegaCrit.Sts2.Core.Models.CardModel].Assembly
$mod=[Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\dist\DrawPeek\DrawPeek.dll'))
$flags=[Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
$names=@('Cards.StrikeIronclad','Cards.DefendIronclad','Enchantments.Imbued','Relics.BagOfPreparation','Relics.RingOfTheSnake','Relics.BigMushroom','Relics.Fiddle','Relics.PaelsBlood','Relics.RingOfTheDrake','Relics.SneckoEye','Relics.Pendulum','Relics.PollinousCore','Relics.BoomingConch','Relics.Pocketwatch')
[MegaCrit.Sts2.Core.Models.ModelDb]::Init([type[]]@($names | ForEach-Object {$game.GetType("MegaCrit.Sts2.Core.Models.$_",$true)}))
function Get-Model($kind,$name){
    [MegaCrit.Sts2.Core.Models.ModelDb].GetMethod($kind).MakeGenericMethod($game.GetType("MegaCrit.Sts2.Core.Models.$($kind)s.$name")).Invoke($null,$null)
}
$predictor=$mod.GetType('DrawPeek.OpeningHandPreview')
$order=$predictor.GetMethod('PredictOrder',$flags)
$draw=$predictor.GetMethod('OpeningDrawCount',$flags)
$clone=$mod.GetType('DrawPeek.GameCompat').GetMethod('CloneRng',$flags)
$shuffle=[MegaCrit.Sts2.Core.Extensions.ListExtensions].GetMethod('UnstableShuffle')
$cardShuffle=$shuffle.MakeGenericMethod([MegaCrit.Sts2.Core.Models.CardModel])
$intShuffle=$shuffle.MakeGenericMethod([int])
$enchantment=Get-Model 'Enchantment' 'Imbued'
$checks=0
foreach($mode in @('empty','single','normal','innate','bottom')){
    $deck=[System.Collections.Generic.List[MegaCrit.Sts2.Core.Models.CardModel]]::new()
    $size=if($mode -eq 'empty'){0}elseif($mode -eq 'single'){1}else{12}
    for($i=0;$i -lt $size;$i++){
        $name=if($i % 2 -eq 0){'StrikeIronclad'}else{'DefendIronclad'}
        $card=(Get-Model 'Card' $name).ToMutable()
        if($mode -in @('innate','bottom') -and $i -lt 4){$card.AddKeyword([MegaCrit.Sts2.Core.Entities.Cards.CardKeyword]::Innate)}
        if($mode -eq 'bottom' -and $i -in @(0,5,7)){
            [MegaCrit.Sts2.Core.Models.CardModel].GetField('<Enchantment>k__BackingField',$flags).SetValue($card,$enchantment)
        }
        $deck.Add($card)
    }
    $original=$deck.ToArray()
    foreach($priorCase in @(@{sizes=[int[]]@()},@{sizes=[int[]]@(10)},@{sizes=[int[]]@(0,1,19)})){
        $prior=$priorCase.sizes
        foreach($seed in 1..40){
            $live=[MegaCrit.Sts2.Core.Random.Rng]::new([uint64]$seed)
            for($i=0;$i -lt ($seed % 6);$i++){$live.NextFloat() | Out-Null}
            $before=$live.ToSerializable() | ConvertTo-Json -Depth 20 -Compress
            $native=$clone.Invoke($null,@($live))
            foreach($previousSize in $prior){
                $previous=[System.Collections.Generic.List[int]]::new()
                for($i=0;$i -lt $previousSize;$i++){$previous.Add($i)}
                $intShuffle.Invoke($null,@($previous,$native)) | Out-Null
            }
            $expected=[System.Collections.Generic.List[MegaCrit.Sts2.Core.Models.CardModel]]::new($deck)
            $cardShuffle.Invoke($null,@($expected,$native)) | Out-Null
            $pile=[MegaCrit.Sts2.Core.Entities.Cards.CardPile]::new([MegaCrit.Sts2.Core.Entities.Cards.PileType]::Draw)
            [MegaCrit.Sts2.Core.Entities.Cards.CardPile].GetField('_cards',$flags).SetValue($pile,$expected)
            $bottom=@($expected | Where-Object {$_.Enchantment -and $_.Enchantment.ShouldStartAtBottomOfDrawPile})
            foreach($card in $bottom){$pile.MoveToBottomInternal($card)}
            $innate=@($expected | Where-Object {$_.Keywords.Contains([MegaCrit.Sts2.Core.Entities.Cards.CardKeyword]::Innate) -and $_ -notin $bottom})
            foreach($card in $innate){$pile.MoveToTopInternal($card)}
            $actual=$order.Invoke($null,@($deck,$live,[int[]]$prior))
            $again=$order.Invoke($null,@($deck,$live,[int[]]$prior))
            for($i=0;$i -lt $size;$i++){
                if(-not [object]::ReferenceEquals($actual[$i],$expected[$i]) -or -not [object]::ReferenceEquals($again[$i],$actual[$i])){throw "Order mismatch: $mode / $seed"}
                if(-not [object]::ReferenceEquals($deck[$i],$original[$i])){throw 'Live deck order changed'}
            }
            if(($live.ToSerializable() | ConvertTo-Json -Depth 20 -Compress) -cne $before){throw 'Live shuffle RNG advanced'}
            $checks++
        }
    }
}
"PASS: $checks native shuffle/pile-order comparisons; empty/single decks, innate reversal, bottom priority, previous players, repeatability, deck and RNG isolation."
$player=[Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject([MegaCrit.Sts2.Core.Entities.Players.Player])
$combat=[Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject([MegaCrit.Sts2.Core.Entities.Players.PlayerCombatState])
[MegaCrit.Sts2.Core.Entities.Players.PlayerCombatState].GetField('<TurnNumber>k__BackingField',$flags).SetValue($combat,1)
[MegaCrit.Sts2.Core.Entities.Players.Player].GetField('<PlayerCombatState>k__BackingField',$flags).SetValue($player,$combat)
foreach($name in @('BagOfPreparation','RingOfTheSnake','BigMushroom','Fiddle','PaelsBlood','RingOfTheDrake','SneckoEye','Pocketwatch','Pendulum','PollinousCore')){
    $relic=(Get-Model 'Relic' $name).ToMutable()
    $relic.Owner=$player
    $turnField=$relic.GetType().GetField('_turnsSeen',$flags)
    $turnCases=if($turnField){@(0,1,2,3,7)}else{@(0)}
    foreach($turns in $turnCases){
        if($turnField){$turnField.SetValue($relic,$turns)}
        $predicted=$draw.Invoke($null,@([MegaCrit.Sts2.Core.Models.RelicModel[]]@($relic),$null))
        if($turnField){
            $next=if($name -eq 'Pendulum'){($turns+1) % $relic.DynamicVars['Turns'].IntValue}else{$turns+1}
            $turnField.SetValue($relic,$next)
        }
        $expected=[Math]::Clamp([int][Math]::Ceiling($relic.ModifyHandDraw($player,5)),0,[MegaCrit.Sts2.Core.Entities.Cards.CardPile]::MaxCardsInHand)
        if($predicted -ne $expected){throw "Draw count mismatch: $name/$turns"}
    }
}
$conch=(Get-Model 'Relic' 'BoomingConch').ToMutable()
foreach($room in @([MegaCrit.Sts2.Core.Rooms.RoomType]::Monster,[MegaCrit.Sts2.Core.Rooms.RoomType]::Elite,[MegaCrit.Sts2.Core.Rooms.RoomType]::Boss)){
    $actual=$draw.Invoke($null,@([MegaCrit.Sts2.Core.Models.RelicModel[]]@($conch),$room))
    $expected=if($room.ToString() -eq 'Elite'){7}else{5}
    if($actual -ne $expected){throw 'Incorrect elite-specific opening bonus'}
}
$many=[MegaCrit.Sts2.Core.Models.RelicModel[]]@(1..8 | ForEach-Object {(Get-Model 'Relic' 'BagOfPreparation').ToMutable()})
if($draw.Invoke($null,@($many,$null)) -ne [MegaCrit.Sts2.Core.Entities.Cards.CardPile]::MaxCardsInHand){throw 'Hand limit not applied'}
'PASS: native first-turn draw modifiers, persistent counters, elite-specific bonus and hand limit. No live UI test.'
