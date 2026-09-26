param([double]$Carpan = 1)
$ErrorActionPreference = 'Stop'
$kok = $PSScriptRoot
Add-Type -Path (Join-Path $kok 'ArctisHid.cs'), (Join-Path $kok 'Dinle.cs') -ReferencedAssemblies 'System'

$hidGuid = '{4d1e55b2-f16f-11cf-88cb-001111000030}'
$saat = [Diagnostics.Stopwatch]::StartNew()
$log = New-Object 'System.Collections.Generic.List[string]'
$kilit = New-Object object
$dinleyiciler = @()
$arayuzler = @()

Get-PnpDevice -PresentOnly -Class HIDClass | Where-Object { $_.InstanceId -like 'HID\VID_1038&PID_12E0*' } | ForEach-Object {
    $yol = '\\?\' + ($_.InstanceId -replace '\\', '#').ToLowerInvariant() + '#' + $hidGuid
    $ad = ($_.InstanceId -split '\\')[1] -replace '^VID_1038&PID_12E0&', ''
    $caps = New-Object ArctisHid+HIDP_CAPS
    $ok = [ArctisHid]::TryGetCaps($yol, [ref]$caps)
    $durum = 'caps yok'
    if ($ok -and $caps.InputReportByteLength -gt 0) {
        try {
            $dinleyiciler += New-Object HidDinleyici($yol, $ad, [int]$caps.InputReportByteLength, $saat, $log, $kilit)
            $durum = 'dinleniyor'
        } catch { $durum = 'açılamadı: ' + $_.Exception.InnerException.Message }
    }
    $arayuzler += ('{0}  UsagePage 0x{1:X4} Usage 0x{2:X2} giriş {3}  -> {4}' -f $ad, $caps.UsagePage, $caps.Usage, $caps.InputReportByteLength, $durum)
}

$ucler = [UcNokta]::Hepsi()
$varsayilan = [UcNokta]::Varsayilan()

function Olc {
    foreach ($u in $ucler) {
        $v = [math]::Round($u.Seviye() * 100, 1)
        $m = $u.Sessiz()
        if ($v -ne $u.Son -or $m -ne $u.SonSessiz) {
            [Threading.Monitor]::Enter($kilit)
            try { $log.Add(('{0:F0}' -f $saat.Elapsed.TotalMilliseconds) + "`tSES`t$($u.Ad)`t$v" + $(if ($m) { ' (sessiz)' } else { '' })) } finally { [Threading.Monitor]::Exit($kilit) }
            $u.Son = $v; $u.SonSessiz = $m
        }
    }
}

function Isaret($metin) {
    [Threading.Monitor]::Enter($kilit)
    try { $log.Add(('{0:F0}' -f $saat.Elapsed.TotalMilliseconds) + "`tADIM`t$metin") } finally { [Threading.Monitor]::Exit($kilit) }
}

$adimlar = @(
    @{ Sn = 8;  Yazi = 'DOKUNMA — taban ölçülüyor' },
    @{ Sn = 15; Yazi = 'Taban istasyonunun düğmesini YAVAŞÇA EN ALTA çevir' },
    @{ Sn = 20; Yazi = 'Şimdi YAVAŞÇA EN ÜSTE çevir' },
    @{ Sn = 8;  Yazi = 'EN ÜSTTE birkaç kez daha yukarı çevirmeyi dene' },
    @{ Sn = 10; Yazi = 'ORTAYA getir, sonra dokunma' }
)

$host.UI.RawUI.WindowTitle = 'Arctis Ses Düğmesi Dinleme'
$hataMetni = $null
if ($Carpan -ge 1) {
    Clear-Host
    Write-Host ''
    Write-Host '  ARCTIS SES DÜĞMESİ DİNLEME' -ForegroundColor Cyan
    Write-Host ''
    Write-Host '  Hazır olunca ENTER — adımlar ondan sonra başlar.' -ForegroundColor Yellow
    [void][Console]::ReadLine()
}
try {
Olc
for ($a = 0; $a -lt $adimlar.Count; $a++) {
    $adim = $adimlar[$a]
    Isaret $adim.Yazi
    Clear-Host
    Write-Host ''
    Write-Host '  ARCTIS SES DÜĞMESİ DİNLEME' -ForegroundColor Cyan
    Write-Host ''
    Write-Host ("  ADIM {0}/{1}:  {2}" -f ($a + 1), $adimlar.Count, $adim.Yazi) -ForegroundColor Yellow
    Write-Host ''
    $bit = $saat.Elapsed.TotalMilliseconds + $adim.Sn * 1000 * $Carpan
    $ekran = 0
    while ($saat.Elapsed.TotalMilliseconds -lt $bit) {
        Olc
        $simdi = $saat.Elapsed.TotalMilliseconds
        if ($simdi -ge $ekran) {
            $hidSay = ($dinleyiciler | Measure-Object -Property Sayac -Sum).Sum
            Write-Host -NoNewline ("`r  Kalan {0,3} sn   HID raporu {1,4}   " -f [math]::Ceiling(($bit - $simdi) / 1000), $hidSay)
            $ekran = $simdi + 250
        }
        Start-Sleep -Milliseconds 50
    }
}
} catch { $hataMetni = $_ | Out-String } finally {
Start-Sleep -Milliseconds 300
$dinleyiciler | ForEach-Object { $_.Dispose() }

$damga = Get-Date -Format 'yyyyMMdd-HHmmss'
$yol = Join-Path $kok "dinle-$damga.log"
$cikti = @()
$cikti += "Arctis Ses Düğmesi Dinleme — $damga"
$cikti += 'Arayüzler:'
$cikti += $arayuzler | ForEach-Object { "  $_" }
$cikti += 'Uç noktalar:'
$cikti += $ucler | ForEach-Object { '  ' + $_.Ad + $(if ($_.Id -eq $varsayilan) { '  [varsayılan]' } else { '' }) }
$dinleyiciler | Where-Object { $_.Hata } | ForEach-Object { $cikti += "Okuma hatası: $($_.Hata)" }
$cikti += ''
[Threading.Monitor]::Enter($kilit)
try { $cikti += $log } finally { [Threading.Monitor]::Exit($kilit) }
if ($hataMetni) { $cikti += "HATA: $hataMetni" }
$cikti | Set-Content -Path $yol -Encoding UTF8
}

Clear-Host
Write-Host ''
Write-Host '  BİTTİ — kayıt alındı, pencereyi kapatabilirsin.' -ForegroundColor Green
Write-Host "  $yol"
