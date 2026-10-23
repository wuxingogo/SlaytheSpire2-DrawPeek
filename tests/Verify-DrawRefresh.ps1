param([string]$GameDir='E:\SteamLibrary\steamapps\common\Slay the Spire 2')
$ErrorActionPreference='Stop'
$data=Join-Path $GameDir 'data_sts2_windows_x86_64'
foreach($dll in @('GodotSharp.dll','0Harmony.dll','sts2.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $data $dll)) | Out-Null}
$mod=[Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\dist\DrawPeek\DrawPeek.dll'))
$flags=[Reflection.BindingFlags]'Public,NonPublic,Static'
$activity=$mod.GetType('DrawPeek.DrawPreviewActivity')
$busy=$activity.GetMethod('IsBusy',$flags)
$player=[Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject([MegaCrit.Sts2.Core.Entities.Players.Player])
$other=[Runtime.CompilerServices.RuntimeHelpers]::GetUninitializedObject([MegaCrit.Sts2.Core.Entities.Players.Player])
function Assert-Busy($target,[bool]$expected){
    if($busy.Invoke($null,@($target)) -ne $expected){throw "Wrong busy state; expected $expected"}
}
function Patch-Task($name,$target,$task){
    $patch=$mod.GetType("DrawPeek.$name")
    $prefixArgs=[object[]]@($target,$null)
    $patch.GetMethod('Prefix',$flags).Invoke($null,$prefixArgs) | Out-Null
    $scope=$prefixArgs[1]
    $postArgs=[object[]]@($task,$scope)
    $patch.GetMethod('Postfix',$flags).Invoke($null,$postArgs) | Out-Null
    [PSCustomObject]@{Task=$postArgs[0];Scope=$scope;Patch=$patch}
}
function Await-Settled($task){
    try{$null=$task.Wait(2000)}catch{}
    if(-not $task.IsCompleted){throw 'Wrapped task failed to complete'}
}
$drawSource=[Threading.Tasks.TaskCompletionSource[System.Collections.Generic.IEnumerable[MegaCrit.Sts2.Core.Models.CardModel]]]::new()
$draw=Patch-Task 'DrawPreviewDrawPatch' $player $drawSource.Task
$shuffleSource=[Threading.Tasks.TaskCompletionSource[object]]::new()
$shuffle=Patch-Task 'DrawPreviewShufflePatch' $player $shuffleSource.Task
Assert-Busy $player $true
Assert-Busy $other $false
$shuffleSource.SetResult($null); Await-Settled $shuffle.Task
Assert-Busy $player $true
$result=[MegaCrit.Sts2.Core.Models.CardModel[]]@()
$drawSource.SetResult($result); Await-Settled $draw.Task
if(-not [object]::ReferenceEquals($draw.Task.Result,$result)){throw 'Draw result was changed'}
Assert-Busy $player $false
$shuffle.Scope.Dispose();$draw.Scope.Dispose()
Assert-Busy $player $false
foreach($failure in @('fault','cancel')){
    $source=[Threading.Tasks.TaskCompletionSource[object]]::new()
    $wrapped=Patch-Task 'DrawPreviewShufflePatch' $player $source.Task
    if($failure -eq 'fault'){$source.SetException([InvalidOperationException]::new('expected test fault'))}else{$source.SetCanceled()}
    Await-Settled $wrapped.Task
    if($failure -eq 'fault' -and -not $wrapped.Task.IsFaulted){throw 'Task fault swallowed'}
    if($failure -eq 'cancel' -and -not $wrapped.Task.IsCanceled){throw 'Task cancellation swallowed'}
    Assert-Busy $player $false
}
# Synchronous command failures never reach Postfix: Finalizer must release too.
$patch=$mod.GetType('DrawPeek.DrawPreviewDrawPatch')
$argsForPrefix=[object[]]@($player,$null)
$patch.GetMethod('Prefix',$flags).Invoke($null,$argsForPrefix) | Out-Null
$patch.GetMethod('Finalizer',$flags).Invoke($null,@([InvalidOperationException]::new('sync failure'),$argsForPrefix[1])) | Out-Null
Assert-Busy $player $false
$drawMethod=[MegaCrit.Sts2.Core.Commands.CardPileCmd].GetMethod('Draw',[type[]]@([MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext],[decimal],[MegaCrit.Sts2.Core.Entities.Players.Player],[bool]))
if($drawMethod.ReturnType -ne $drawSource.Task.GetType().BaseType -and $drawMethod.ReturnType -ne $drawSource.Task.GetType()){throw 'Draw patch return signature changed'}
if([MegaCrit.Sts2.Core.Commands.CardPileCmd].GetMethod('Shuffle').ReturnType -ne [Threading.Tasks.Task]){throw 'Shuffle patch return signature changed'}
'PASS: patch callback lifecycle across nested draw/shuffle, per-player isolation, async completion/results, fault/cancellation and synchronous exceptions; native signatures checked. Harmony dispatch/live UI not exercised.'
