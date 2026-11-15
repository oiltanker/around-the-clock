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

# 1. Publicize assemblies whose private members the mod touches.
$publicizer = Join-Path $root 'tools\Publicizer'
if (Test-Path (Join-Path $publicizer 'obj')) {
  Remove-Item -Recurse -Force (Join-Path $publicizer 'obj'), (Join-Path $publicizer 'bin')
}
dotnet build (Join-Path $publicizer 'Publicizer.csproj') -c Release -v q | Out-Null
$exe = Join-Path $publicizer 'bin\Release\net8.0\Publicizer.dll'
New-Item -ItemType Directory -Force $lib | Out-Null
foreach ($asm in 'Timberborn.WorkSystem', 'Timberborn.SleepSystem', 'Timberborn.DwellingSystem', 'Timberborn.WorkSystemUI', 'Timberborn.MechanicalSystem', 'Timberborn.PowerGeneration', 'Timberborn.TimeSystem', 'Timberborn.BeaverBehavior', 'Timberborn.PrioritySystemUI', 'Timberborn.InputSystemUI') {
  dotnet $exe (Join-Path $gameDir "$asm.dll") $lib
}

# 2. Build the three mod assemblies.
dotnet build (Join-Path $root 'SecondShift.Core\SecondShift.Core.csproj') -c Release "-p:GameDir=$gameDir" "-p:LibDir=$lib"
if ($LASTEXITCODE -ne 0) { throw 'Core build failed' }
dotnet build (Join-Path $root 'SecondShift.Patching\SecondShift.Patching.csproj') -c Release "-p:GameDir=$gameDir" "-p:LibDir=$lib" "-p:HarmonyDir=$harmonyDir"
if ($LASTEXITCODE -ne 0) { throw 'Patching build failed' }
dotnet build (Join-Path $root 'SecondShift.CoreUI\SecondShift.CoreUI.csproj') -c Release "-p:GameDir=$gameDir" "-p:LibDir=$lib"
if ($LASTEXITCODE -ne 0) { throw 'CoreUI build failed' }

# 3. Deploy DLLs into the mod folder's version-1.1\Scripts.
if ($Deploy) {
  $scriptsDir = 'C:\Users\Oiltanker\Documents\Timberborn\Mods\second-shift-3shifts_5522813_1.1.1.0\version-1.1\Scripts'
  foreach ($proj in 'SecondShift.Core', 'SecondShift.Patching', 'SecondShift.CoreUI') {
    Copy-Item (Join-Path $root "$proj\bin\Release\$proj.dll") $scriptsDir -Force
  }
  Get-ChildItem $scriptsDir | Select-Object Name
}


