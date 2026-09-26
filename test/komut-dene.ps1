param([string]$Komut = '06-25-28')
$ErrorActionPreference = 'Stop'
$kok = $PSScriptRoot
Add-Type -Path (Join-Path $kok 'ArctisHid.cs'), (Join-Path $kok 'Dinle.cs') -ReferencedAssemblies 'System'
$hidGuid = '{4d1e55b2-f16f-11cf-88cb-001111000030}'
$saat = [Diagnostics.Stopwatch]::StartNew()
$log = New-Object 'System.Collections.Generic.List[string]'
$kilit = New-Object object
$d = @()
$yazYol = $null; $cikis = 0
Get-PnpDevice -PresentOnly -Class HIDClass | Where-Object { $_.InstanceId -like 'HID\VID_1038&PID_12E0&MI_04*' } | ForEach-Object {
    $yol = '\\?\' + ($_.InstanceId -replace '\\', '#').ToLowerInvariant() + '#' + $hidGuid
    $ad = ($_.InstanceId -split '\\')[1] -replace '^VID_1038&PID_12E0&', ''
    $caps = New-Object ArctisHid+HIDP_CAPS
    if ([ArctisHid]::TryGetCaps($yol, [ref]$caps)) {
        $d += New-Object HidDinleyici($yol, $ad, [int]$caps.InputReportByteLength, $saat, $log, $kilit)
        if ($caps.UsagePage -eq 0xFFC0) { $yazYol = $yol; $cikis = $caps.OutputReportByteLength }
    }
}
$hid = New-Object ArctisHid($yazYol, 64, [int]$cikis, $saat)
Start-Sleep -Milliseconds 300
$bayt = $Komut -split '-' | ForEach-Object { [Convert]::ToByte($_, 16) }
$o = New-Object byte[] $cikis
for ($i = 0; $i -lt $bayt.Count; $i++) { $o[$i] = $bayt[$i] }
$log.Add(('{0:F0}' -f $saat.Elapsed.TotalMilliseconds) + "`tGONDER`t$Komut")
$hid.GetType().GetField('wr', [Reflection.BindingFlags]'NonPublic,Instance').GetValue($hid).Write($o, 0, $o.Length)
Start-Sleep -Milliseconds 2500
$d | ForEach-Object { $_.Dispose() }; $hid.Dispose()
$log | ForEach-Object { $_ }
