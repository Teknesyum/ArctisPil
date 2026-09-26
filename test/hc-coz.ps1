$asm = [Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\bin\HeadsetBatteryTray.exe'))
$m = $asm.GetType('HeadsetControl').GetMethod('Coz')
$ornek = @{
  'sarjda' = '{"name":"HeadsetControl","api_version":"1.5","device_count":1,"devices":[{"status":"success","device":"Logitech G PRO X 2","id_vendor":"0x046d","id_product":"0x0af7","capabilities":["CAP_BATTERY_STATUS"],"battery":{"status":"BATTERY_CHARGING","level":64}}]}'
  'normal' = '{"name":"HeadsetControl","api_version":"1.5","device_count":1,"devices":[{"status":"success","device":"Test Headset","battery":{"status":"BATTERY_AVAILABLE","level":75,"voltage_mv":3800},"chatmix":64}]}'
  'kapali' = '{"name":"HeadsetControl","device_count":1,"devices":[{"status":"partial","device":"X","battery":{"status":"BATTERY_UNAVAILABLE","level":-1}}]}'
  'yok' = '{"name":"HeadsetControl","device_count":0,"devices":[]}'
}
foreach ($k in 'normal','sarjda','kapali','yok') { $a = @($ornek[$k], $null, $null, $null); $ok = $m.Invoke($null, $a); "$k -> $ok | $($a[1]) | $($a[2]) | sarj=$($a[3])" }
