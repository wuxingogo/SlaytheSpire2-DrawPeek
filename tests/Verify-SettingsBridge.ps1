param([string]$GameDir='G:\SteamLibrary\steamapps\common\Slay the Spire 2')
$ErrorActionPreference='Stop'
foreach($dll in @('GodotSharp.dll','0Harmony.dll','sts2.dll')){[Reflection.Assembly]::LoadFrom((Join-Path $GameDir "data_sts2_windows_x86_64\$dll")) | Out-Null}
$mod=[Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\dist\DrawPeek\DrawPeek.dll'))
if($mod.GetReferencedAssemblies().Name -match 'Jmc|Ritsu'){throw 'Optional framework became a hard dependency'}
$flags=[Reflection.BindingFlags]'Static,NonPublic,Public'
$bridge=$mod.GetType('DrawPeek.JmcSettingsBridge')
$register=$bridge.GetMethod('TryRegister',$flags)
$register.Invoke($null,$null) | Out-Null
if($bridge.GetField('registered',$flags).GetValue($null)){throw 'Registered without optional provider'}
# Contract double: simulate stale provider storage during registration, without
# loading Godot UI or writing real settings. Retains real delegate bindings.
Add-Type -TypeDefinition @"
using System;
using System.Reflection;
using System.Collections.Generic;
namespace JmcModLib.Config {
 public static class ConfigManager {
  public static readonly List<string> Keys = new List<string>();
  public static readonly List<Func<bool>> Getters = new List<Func<bool>>();
  public static Assembly Owner;
  public static string RegisterConfig<TValue>(string displayName, Func<TValue> getter, Action<TValue> setter, string group = "DefaultGroup", Action<TValue> onChanged = null, object uiAttribute = null, string storageKey = null, string locTable = null, string displayNameKey = null, string groupKey = null, string description = null, string descriptionKey = null, int order = 0, bool restartRequired = false, Assembly assembly = null) {
   Keys.Add(storageKey); Owner = assembly;
   Getters.Add(() => (bool)(object)getter());
   setter((TValue)(object)false);
   return storageKey;
  }
 }
}
"@
$settings=$mod.GetType('DrawPeek.Settings')
$before=@('Enabled','MapPredictionEnabled','EventPredictionEnabled') | ForEach-Object { $settings.GetProperty($_,$flags).GetValue($null) }
$register.Invoke($null,$null) | Out-Null
$register.Invoke($null,$null) | Out-Null
if([JmcModLib.Config.ConfigManager]::Keys.Count -ne 3){throw 'Expected exactly three entries after repeated registration'}
if([JmcModLib.Config.ConfigManager]::Owner -ne $mod){throw 'Entries associated with wrong mod assembly'}
for($i=0;$i -lt 3;$i++){if([JmcModLib.Config.ConfigManager]::Getters[$i].Invoke() -ne $before[$i]){throw 'Provider stale storage overwrote DrawPeek settings'}}
'PASS: provider absence, late availability, three entries, idempotence, mod ownership, existing settings preservation, no framework assembly dependencies. Live UI not exercised.'
