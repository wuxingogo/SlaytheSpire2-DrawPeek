param([string]$GameDir='G:\SteamLibrary\steamapps\common\Slay the Spire 2')
$ErrorActionPreference='Stop'
$data=Join-Path $GameDir 'data_sts2_windows_x86_64'
foreach($dll in @('GodotSharp.dll','0Harmony.dll','sts2.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $data $dll)) | Out-Null}
$game=[MegaCrit.Sts2.Core.Models.CardModel].Assembly
$mod=[Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\bin\Release\net9.0\DrawPeek.dll'))
$flags=[Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
$names=@('Cards.StrikeIronclad','Cards.DefendIronclad','Cards.Burn','Enchantments.PerfectFit','Powers.DrawCardsNextTurnPower','Powers.MindRotPower','Relics.SneckoEye','Relics.BagOfPreparation','Relics.Pendulum','Relics.RingOfTheDrake')
[MegaCrit.Sts2.Core.Models.ModelDb]::Init([type[]]@($names | ForEach-Object {$game.GetType("MegaCrit.Sts2.Core.Models.$_",$true)}))
function Model($kind,$name){ [MegaCrit.Sts2.Core.Models.ModelDb].GetMethod($kind).MakeGenericMethod($game.GetType("MegaCrit.Sts2.Core.Models.$($kind)s.$name")).Invoke($null,$null) }
function Cards([int]$n){
 $list=[Collections.Generic.List[MegaCrit.Sts2.Core.Models.CardModel]]::new()
 for($i=0;$i -lt $n;$i++){ $name=if($i%2){'DefendIronclad'}else{'StrikeIronclad'}; $list.Add((Model 'Card' $name).ToMutable()) }
 return ,$list
}
$t=$mod.GetType('DrawPeek.CombatDrawPreview')
$predict=$t.GetMethod('Predict',$flags)
$clone=$mod.GetType('DrawPeek.GameCompat').GetMethod('CloneRng',$flags)
$shuffle=[MegaCrit.Sts2.Core.Extensions.ListExtensions].GetMethod('StableShuffle').MakeGenericMethod([MegaCrit.Sts2.Core.Models.CardModel])
$checks=0
foreach($drawSize in @(0,2,8)) { foreach($discardSize in @(0,1,9)) { foreach($count in @(0,3,7,10)) { foreach($seed in 1..10) {
 $draw=Cards $drawSize; $discard=Cards $discardSize
 $rng=[MegaCrit.Sts2.Core.Random.Rng]::new([uint64]$seed)
 for($i=0;$i -lt $seed%4;$i++){ $null=$rng.NextFloat() }
 $before=$rng.ToSerializable() | ConvertTo-Json -Depth 20 -Compress
 $expected=[Collections.Generic.List[MegaCrit.Sts2.Core.Models.CardModel]]::new($discard)
 $null=$shuffle.Invoke($null,@($expected,$clone.Invoke($null,@($rng))))
 $want=@($draw)+@($expected); $want=@($want | Select-Object -First $count)
 $result=$predict.Invoke($null,@($draw,$discard,$rng,$count,$false))
 if($result.Cards.Count -ne $want.Count){throw 'Wrong draw count'}
 for($i=0;$i -lt $want.Count;$i++){if(-not [object]::ReferenceEquals($want[$i],$result.Cards[$i])){throw 'Native shuffle mismatch'}}
 $boundary=if($drawSize -lt $count -and $discardSize -gt 0){$drawSize}else{-1}
 if($result.ShuffleIndex -ne $boundary){throw 'Wrong shuffle separator index'}
 $again=$predict.Invoke($null,@($draw,$discard,$rng,$count,$false))
 for($i=0;$i -lt $want.Count;$i++){if(-not [object]::ReferenceEquals($again.Cards[$i],$result.Cards[$i])){throw 'Repeat preview changed'}}
 if(($rng.ToSerializable() | ConvertTo-Json -Depth 20 -Compress) -cne $before){throw 'Live RNG changed'}
 if($draw.Count -ne $drawSize -or $discard.Count -ne $discardSize){throw 'Live pile changed'}
 $checks++
}}}}
# Project only hand cards that will actually enter the discard pile.
$state=[Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject([MegaCrit.Sts2.Core.Entities.Players.PlayerCombatState])
foreach($pair in @(@('Hand','Hand'),@('DiscardPile','Discard'))){
 $pile=[MegaCrit.Sts2.Core.Entities.Cards.CardPile]::new([Enum]::Parse([MegaCrit.Sts2.Core.Entities.Cards.PileType],$pair[1]))
 [MegaCrit.Sts2.Core.Entities.Players.PlayerCombatState].GetField("<$($pair[0])>k__BackingField",$flags).SetValue($state,$pile)
 [MegaCrit.Sts2.Core.Entities.Cards.CardPile].GetField('_cards',$flags).SetValue($pile,(Cards 3))
}
$state.Hand.Cards[1].AddKeyword([MegaCrit.Sts2.Core.Entities.Cards.CardKeyword]::Retain)
$state.Hand.Cards[2].AddKeyword([MegaCrit.Sts2.Core.Entities.Cards.CardKeyword]::Ethereal)
$project=$t.GetMethod('ProjectHand',$flags)
$p=$project.Invoke($null,@($state,$true,$true))
if($p.Item1.Count -ne 4 -or $p.Item2 -ne 1 -or $p.Item3){throw 'Normal/retain/ethereal projection wrong'}
$p=$project.Invoke($null,@($state,$true,$false))
if($p.Item1.Count -ne 3 -or $p.Item2 -ne 2){throw 'Retain-all projection wrong'}
$p=$project.Invoke($null,@($state,$false,$true))
if($p.Item1.Count -ne 3 -or $p.Item2 -ne 3){throw 'Enemy turn projected hand twice'}
# Pending next-turn draw must work before AmountOnTurnStart is updated.
$power=(Model 'Power' 'DrawCardsNextTurnPower').ToMutable()
[MegaCrit.Sts2.Core.Models.PowerModel].GetField('_amount',$flags).SetValue($power,3)
$countMethod=$t.GetMethod('NextDrawCount',$flags)
$relics=[MegaCrit.Sts2.Core.Models.RelicModel[]]@()
$powers=[MegaCrit.Sts2.Core.Models.PowerModel[]]@($power)
if($countMethod.Invoke($null,@($relics,$powers,2,0)) -ne 8){throw 'Pending draw bonus missing'}
if($countMethod.Invoke($null,@($relics,$powers,2,6)) -ne 4){throw 'Retained hand capacity ignored'}
$bag=(Model 'Relic' 'BagOfPreparation').ToMutable()
if($countMethod.Invoke($null,@([MegaCrit.Sts2.Core.Models.RelicModel[]]@($bag),[MegaCrit.Sts2.Core.Models.PowerModel[]]@(),2,0)) -ne 5){throw 'Opening bonus leaked into next turn'}
"PASS: $checks native reshuffle/count/boundary/RNG checks; hand projection, pending draw bonus, retained capacity, first-turn relic exclusion."
# PerfectFit: compare against the native hook on copied lists, including multiple fits.
$discard=Cards 7
foreach($index in @(1,4)){
 $fit=(Model 'Enchantment' 'PerfectFit').ToMutable()
 [MegaCrit.Sts2.Core.Models.EnchantmentModel].GetField('_card',$flags).SetValue($fit,$discard[$index])
 [MegaCrit.Sts2.Core.Models.CardModel].GetField('<Enchantment>k__BackingField',$flags).SetValue($discard[$index],$fit)
}
$rng=[MegaCrit.Sts2.Core.Random.Rng]::new([uint64]123)
$expected=[Collections.Generic.List[MegaCrit.Sts2.Core.Models.CardModel]]::new($discard)
$null=$shuffle.Invoke($null,@($expected,$clone.Invoke($null,@($rng))))
foreach($card in $discard){if($card.Enchantment){$card.Enchantment.ModifyShuffleOrder($null,$expected,$false)}}
$actual=$predict.Invoke($null,@((Cards 0),$discard,$rng,7,$false))
for($i=0;$i -lt 7;$i++){if(-not [object]::ReferenceEquals($expected[$i],$actual.Cards[$i])){throw 'PerfectFit mismatch'}}
$unknown=$predict.Invoke($null,@((Cards 2),$discard,$rng,7,$true))
if($unknown.ShuffleIndex -ne 2 -or $unknown.Cards.Count -ne 7 -or @($unknown.Cards | Where-Object {$null -ne $_}).Count -ne 2){throw 'Unknown shuffle must preserve known prefix and use card backs'}
$mind=(Model 'Power' 'MindRotPower').ToMutable()
[MegaCrit.Sts2.Core.Models.PowerModel].GetField('_amount',$flags).SetValue($mind,2)
if($countMethod.Invoke($null,@($relics,[MegaCrit.Sts2.Core.Models.PowerModel[]]@($mind),2,0)) -ne 3){throw 'Reduced draw count ignored'}
$pendulum=(Model 'Relic' 'Pendulum').ToMutable()
$pendulum.TurnsSeen=2
if($countMethod.Invoke($null,@([MegaCrit.Sts2.Core.Models.RelicModel[]]@($pendulum),[MegaCrit.Sts2.Core.Models.PowerModel[]]@(),2,0)) -ne 6 -or $pendulum.TurnsSeen -ne 2){throw 'Upcoming counter draw or state isolation failed'}
$drake=(Model 'Relic' 'RingOfTheDrake').ToMutable()
if($countMethod.Invoke($null,@([MegaCrit.Sts2.Core.Models.RelicModel[]]@($drake),[MegaCrit.Sts2.Core.Models.PowerModel[]]@(),4,0)) -ne 5){throw 'Expired first turns bonus applied'}
'PASS: multiple PerfectFit native hooks, unknown shuffle fallback, reduced draw, upcoming relic counter, expiring relic bonus.'
