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
$obj = Join-Path $cikti 'obj'
New-Item -ItemType Directory -Force $obj | Out-Null
$palet = Get-Content -Encoding UTF8 (Join-Path $kok 'teknesyum-ui\winforms\Palette.cs')
$bas = [array]::IndexOf($palet, ($palet | Where-Object { $_ -match '^public static class Palette' } | Select-Object -First 1))
$son = [array]::IndexOf($palet, ($palet | Where-Object { $_ -match '^public static class Ansi' } | Select-Object -First 1))
$uyeler = $palet[$bas..$son] | Where-Object { $_ -match '^\s+public (const |static readonly Color )' }
$jeton = Get-Content -Raw -Encoding UTF8 (Join-Path $kok 'teknesyum-ui\theme.tokens.json') | ConvertFrom-Json
function Pascal($ad) { (($ad -split '-') | ForEach-Object { $_.Substring(0, 1).ToUpperInvariant() + $_.Substring(1) }) -join '' }
$sure = $jeton.duration.PSObject.Properties | ForEach-Object { "    public const int $(Pascal $_.Name) = $($_.Value.ms);" }
$egri = $jeton.easing.PSObject.Properties | Where-Object { $_.Value.bezier } | ForEach-Object { "    public static readonly double[] $(Pascal $_.Name) = { $(($_.Value.bezier | ForEach-Object { $_.ToString([Globalization.CultureInfo]::InvariantCulture) }) -join ', ') };" }
$ton = foreach ($t in $jeton.derived.'tone-scale'.bases) { foreach ($a in $jeton.derived.'tone-scale'.steps) { "    public static readonly Color $(Pascal $t)Yuzde$a = Color.FromArgb($([int][math]::Round($a * 255 / 100, [MidpointRounding]::AwayFromZero)), Palette.$(Pascal $t));" } }
$duzen = @('using System.Drawing;', 'namespace Teknesyum.Theme {', 'public static class Palette {') + $uyeler + @('}', 'public static class Sure {') + $sure + @('}', 'public static class Egri {') + $egri + @('}', 'public static class Ton {') + $ton + @('}', '}')
[IO.File]::WriteAllLines((Join-Path $obj 'Duzen.cs'), $duzen, (New-Object Text.UTF8Encoding $true))
$etiket = Join-Path $kok 'teknesyum-ui\winforms'
Get-ChildItem $etiket -Filter 'labels.*.json' | ForEach-Object { $ek += "/resource:$($_.FullName),$($_.Name)" }
& (Join-Path $kok 'simge.ps1') -Jeton (Join-Path $kok 'teknesyum-ui\theme.tokens.json') -Cikti (Join-Path $obj 'app.ico') -Png (Join-Path $kok 'assets\icon.png')
$ek += "/win32icon:$(Join-Path $obj 'app.ico')"
& $csc /nologo /optimize+ /target:winexe /codepage:65001 /out:"$cikti\HeadsetBatteryTray.exe" /noconfig /nostdlib /r:"$fw\mscorlib.dll" /r:"$fw\System.dll" /r:"$fw\System.Windows.Forms.dll" /r:"$fw\System.Drawing.dll" /r:"$fw\System.Core.dll" /r:"$fw\System.Web.Extensions.dll" @ek "$kok\src\HeadsetBatteryTray.cs" "$obj\Duzen.cs"
if ($LASTEXITCODE -ne 0) { throw "derleme başarısız ($LASTEXITCODE)" }
Write-Host "derlendi: $cikti\HeadsetBatteryTray.exe"
