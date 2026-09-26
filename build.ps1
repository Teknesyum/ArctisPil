$ErrorActionPreference = 'Stop'
$kok = $PSScriptRoot
$cikti = Join-Path $kok 'bin'
New-Item -ItemType Directory -Force $cikti | Out-Null
Get-Process ArctisPil -ErrorAction SilentlyContinue | Stop-Process -Force
$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $fw 'csc.exe'
& $csc /nologo /optimize+ /target:winexe /codepage:65001 /out:"$cikti\ArctisPil.exe" /noconfig /nostdlib /r:"$fw\mscorlib.dll" /r:"$fw\System.dll" /r:"$fw\System.Windows.Forms.dll" /r:"$fw\System.Drawing.dll" /r:"$fw\System.Core.dll" "$kok\src\ArctisPil.cs"
if ($LASTEXITCODE -ne 0) { throw "derleme başarısız ($LASTEXITCODE)" }
Write-Host "derlendi: $cikti\ArctisPil.exe"
