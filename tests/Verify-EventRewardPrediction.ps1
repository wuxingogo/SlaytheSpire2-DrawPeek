param([string]$GameDir='E:\SteamLibrary\steamapps\common\Slay the Spire 2')
$ErrorActionPreference='Stop'
$data=Join-Path $GameDir 'data_sts2_windows_x86_64'
foreach($dll in @('GodotSharp.dll','0Harmony.dll','sts2.dll')) {
    [System.Reflection.Assembly]::LoadFrom((Join-Path $data $dll)) | Out-Null
}
$game=[MegaCrit.Sts2.Core.Models.CardModel].Assembly
$mod=[System.Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\dist\DrawPeek\DrawPeek.dll'))
$flags=[System.Reflection.BindingFlags]'Public,NonPublic,Instance,Static'
$names=@('Inflame','DemonForm','Anger','Offering','BattleTrance','DefendIronclad')
$types=[type[]]@($names | ForEach-Object {$game.GetType("MegaCrit.Sts2.Core.Models.Cards.$_",$true)})
[MegaCrit.Sts2.Core.Models.ModelDb]::Init($types)
$getCard=[MegaCrit.Sts2.Core.Models.ModelDb].GetMethod('Card')
$cards=@($types | ForEach-Object {$getCard.MakeGenericMethod($_).Invoke($null,$null)})
$select=$mod.GetType('DrawPeek.EventRewardPrediction').GetMethod('SelectRewardCard',$flags)
$clone=$mod.GetType('DrawPeek.GameCompat').GetMethod('CloneRng',$flags)
$nativeRarity=[MegaCrit.Sts2.Core.Factories.CardFactory].GetMethod('RollForRarity',$flags)
$player=[System.Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject([MegaCrit.Sts2.Core.Entities.Players.Player])
$odds=[Activator]::CreateInstance([MegaCrit.Sts2.Core.Odds.PlayerOddsSet],$true)
[MegaCrit.Sts2.Core.Entities.Players.Player].GetField('<PlayerOdds>k__BackingField',$flags).SetValue($player,$odds)
$pools=@(
    ,([MegaCrit.Sts2.Core.Models.CardModel[]]@($cards | Where-Object {$_.Type.ToString() -eq 'Power'}))
    ,([MegaCrit.Sts2.Core.Models.CardModel[]]@($cards | Where-Object {$_.EnergyCost.Canonical -eq 0 -and -not $_.EnergyCost.CostsX}))
)
$checked=0
foreach($pool in $pools) {
    $allowed=[System.Collections.Generic.HashSet[MegaCrit.Sts2.Core.Entities.Cards.CardRarity]]::new()
    foreach($card in $pool){$allowed.Add($card.Rarity) | Out-Null}
    foreach($offset in @([single]-0.05,[single]0.4)) {
        foreach($seed in 1..100) {
            $live=[MegaCrit.Sts2.Core.Random.Rng]::new([uint64]$seed)
            for($i=0;$i -lt ($seed % 7);$i++){$live.NextFloat() | Out-Null}
            $before=$live.ToSerializable() | ConvertTo-Json -Depth 20 -Compress
            $native=$clone.Invoke($null,@($live))
            $rarityOdds=[MegaCrit.Sts2.Core.Odds.CardRarityOdds]::new($offset,$native)
            [MegaCrit.Sts2.Core.Odds.PlayerOddsSet].GetField('<CardRarity>k__BackingField',$flags).SetValue($odds,$rarityOdds)
            $rarity=$nativeRarity.Invoke($null,@($player,[MegaCrit.Sts2.Core.Runs.CardRarityOddsType]::RegularEncounter,[MegaCrit.Sts2.Core.Runs.CardCreationSource]::Other,$allowed,$false))
            $eligible=[MegaCrit.Sts2.Core.Models.CardModel[]]@($pool | Where-Object {$_.Rarity -eq $rarity})
            $expected=$native.NextItem[MegaCrit.Sts2.Core.Models.CardModel]($eligible)
            $actual=$select.Invoke($null,@($pool,$live))
            $again=$select.Invoke($null,@($pool,$live))
            if($actual.Id -ne $expected.Id -or $again.Id -ne $actual.Id){throw "Reward mismatch at seed $seed"}
            if(($live.ToSerializable() | ConvertTo-Json -Depth 20 -Compress) -cne $before){throw 'Live rewards RNG advanced'}
            if($rarityOdds.CurrentValue -ne $offset){throw 'Native base odds unexpectedly changed pity offset'}
            $checked++
        }
    }
}
$empty=[MegaCrit.Sts2.Core.Models.CardModel[]]@()
if($null -ne $select.Invoke($null,@($empty,$live))){throw 'Empty pool must produce no preview'}
$basic=[MegaCrit.Sts2.Core.Models.CardModel[]]@($cards[-1])
if($null -ne $select.Invoke($null,@($basic,$live))){throw 'Pool with no eligible rarity must produce no preview'}
"PASS: $checked comparisons against native rarity/card selection, including missing common rarity, pity offsets, advanced RNG states, repeated hovering, RNG isolation, and empty/invalid pools."
