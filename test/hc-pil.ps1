$asm = [Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\bin\HeadsetBatteryTray.exe'))
$m = $asm.GetType('HeadsetControl').GetMethod('Pil')
$a = @($null, $null, $null)
$ok = $m.Invoke($null, $a)
"bulundu: $ok  ad: $($a[0])  pil: $($a[1])  sarj: $($a[2])"
