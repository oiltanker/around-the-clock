param(
  [switch]$Deploy
)
$ErrorActionPreference = 'Stop'
$env:DOTNET_ROOT = "$env:USERPROFILE\dotnet-sdk"
$env:PATH = "$env:USERPROFILE\dotnet-sdk;$env:PATH"

$gameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Timberborn\Timberborn_Data\Managed'
$harmonyDir = 'C:\Users\Oiltanker\Documents\Timberborn\Mods\harmony_4149561_2.4.1'
$root = $PSScriptRoot
$lib = Join-Path $root 'lib_public'

function Build-Tool($dir) {
  if (Test-Path (Join-Path $dir 'obj')) {
    Remove-Item -Recurse -Force (Join-Path $dir 'obj'), (Join-Path $dir 'bin')
  }
  dotnet build (Join-Path $dir '*.csproj' | Get-ChildItem | Select-Object -First 1 -ExpandProperty FullName) -c Release -v q | Out-Null
  if ($LASTEXITCODE -ne 0) { throw "tool build failed: $dir" }
}

# 1. Publicize assemblies whose private members the mod touches.
$publicizer = Join-Path $root 'tools\Publicizer'
Build-Tool $publicizer
$pubExe = Join-Path $publicizer 'bin\Release\net8.0\Publicizer.dll'
New-Item -ItemType Directory -Force $lib | Out-Null
foreach ($asm in 'Timberborn.WorkSystem', 'Timberborn.SleepSystem', 'Timberborn.DwellingSystem', 'Timberborn.WorkSystemUI', 'Timberborn.MechanicalSystem', 'Timberborn.PowerGeneration', 'Timberborn.TimeSystem', 'Timberborn.BeaverBehavior', 'Timberborn.PrioritySystemUI', 'Timberborn.InputSystemUI') {
  dotnet $pubExe (Join-Path $gameDir "$asm.dll") $lib
}

# 2. Build the three mod assemblies.
dotnet build (Join-Path $root 'SecondShift.Core\SecondShift.Core.csproj') -c Release "-p:GameDir=$gameDir" "-p:LibDir=$lib"
if ($LASTEXITCODE -ne 0) { throw 'Core build failed' }
dotnet build (Join-Path $root 'SecondShift.Patching\SecondShift.Patching.csproj') -c Release "-p:GameDir=$gameDir" "-p:LibDir=$lib" "-p:HarmonyDir=$harmonyDir"
if ($LASTEXITCODE -ne 0) { throw 'Patching build failed' }
dotnet build (Join-Path $root 'SecondShift.CoreUI\SecondShift.CoreUI.csproj') -c Release "-p:GameDir=$gameDir" "-p:LibDir=$lib"
if ($LASTEXITCODE -ne 0) { throw 'CoreUI build failed' }

# 3. Inject IgnoresAccessChecksTo so runtime visibility checks pass against
#    the real (unpublicized) game assemblies. Mandatory: without it every
#    publicized-API call is a MethodAccessException at runtime.
$injector = Join-Path $root 'tools\AccessInjector'
Build-Tool $injector
$injExe = Join-Path $injector 'bin\Release\net8.0\AccessInjector.dll'
$dlls = 'SecondShift.Core', 'SecondShift.Patching', 'SecondShift.CoreUI' |
  ForEach-Object { Join-Path $root "$_\bin\Release\$_.dll" }
dotnet $injExe @dlls
if ($LASTEXITCODE -ne 0) { throw 'injection failed' }

# 4. Deploy DLLs into the mod folder's version-1.1\Scripts.
if ($Deploy) {
  $scriptsDir = 'C:\Users\Oiltanker\Documents\Timberborn\Mods\second-shift-3shifts_5522813_1.1.1.0\version-1.1\Scripts'
  foreach ($dll in $dlls) {
    Copy-Item $dll $scriptsDir -Force
  }
  Get-ChildItem $scriptsDir | Select-Object Name
}
