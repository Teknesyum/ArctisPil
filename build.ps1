$ErrorActionPreference = 'Stop'
$kok = $PSScriptRoot
$cikti = Join-Path $kok 'bin'
New-Item -ItemType Directory -Force $cikti | Out-Null
Get-Process ArctisPil, HeadsetBatteryTray -ErrorAction SilentlyContinue | Stop-Process -Force
$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $fw 'csc.exe'
$ek = @()
$hc = Join-Path $kok 'vendor\headsetcontrol.exe'
if (Test-Path $hc) { $ek += "/resource:$hc,headsetcontrol.exe" } else { Write-Host "uyarı: vendor\headsetcontrol.exe yok, gömülmeden derleniyor" }
Get-ChildItem (Join-Path $kok 'assets\fonts') -Filter *.ttf | ForEach-Object { $ek += "/resource:$($_.FullName),$($_.Name)" }
& $csc /nologo /optimize+ /target:winexe /codepage:65001 /out:"$cikti\HeadsetBatteryTray.exe" /noconfig /nostdlib /r:"$fw\mscorlib.dll" /r:"$fw\System.dll" /r:"$fw\System.Windows.Forms.dll" /r:"$fw\System.Drawing.dll" /r:"$fw\System.Core.dll" /r:"$fw\System.Web.Extensions.dll" @ek "$kok\src\HeadsetBatteryTray.cs"
if ($LASTEXITCODE -ne 0) { throw "derleme başarısız ($LASTEXITCODE)" }
Write-Host "derlendi: $cikti\HeadsetBatteryTray.exe"
