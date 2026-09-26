$asm = [Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\bin\HeadsetBatteryTray.exe'))
$m = $asm.GetType('Bluetooth').GetMethod('Pil')
$a = @($null, $null, $true)
$ok = $m.Invoke($null, $a)
"bulundu: $ok  ad: $($a[0])  pil: $($a[1])"
