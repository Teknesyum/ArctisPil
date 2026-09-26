param(
    [ValidateSet('yokla', 'test')]
    [string]$Mod = 'test'
)

$ErrorActionPreference = 'Stop'
$kok = $PSScriptRoot
Add-Type -Path (Join-Path $kok 'ArctisHid.cs') -ReferencedAssemblies 'System'

$hidGuid = '{4d1e55b2-f16f-11cf-88cb-001111000030}'
$adaylar = Get-PnpDevice -PresentOnly -Class HIDClass |
    Where-Object { $_.InstanceId -like 'HID\VID_1038&PID_12E0&MI_04*' } |
    ForEach-Object {
        $yol = '\\?\' + ($_.InstanceId -replace '\\', '#').ToLowerInvariant() + '#' + $hidGuid
        $caps = New-Object ArctisHid+HIDP_CAPS
        $ok = [ArctisHid]::TryGetCaps($yol, [ref]$caps)
        [pscustomobject]@{
            Yol       = $yol
            Acildi    = $ok
            UsagePage = '0x{0:X4}' -f $caps.UsagePage
            Usage     = '0x{0:X2}' -f $caps.Usage
            Giris     = $caps.InputReportByteLength
            Cikis     = $caps.OutputReportByteLength
        }
    }

$secilen = $adaylar | Where-Object { $_.Acildi -and $_.Cikis -gt 0 -and $_.UsagePage -eq '0xFFC0' } | Select-Object -First 1
if (-not $secilen) { $secilen = $adaylar | Where-Object { $_.Acildi -and $_.Cikis -gt 0 } | Select-Object -First 1 }
if (-not $secilen) {
    $adaylar | Format-Table -AutoSize | Out-String | Write-Host
    throw 'Arctis Nova Pro taban istasyonunda yazılabilir HID arayüzü bulunamadı.'
}

$saat = [Diagnostics.Stopwatch]::StartNew()
$hid = New-Object ArctisHid($secilen.Yol, [int]$secilen.Giris, [int]$secilen.Cikis, $saat)

if ($Mod -eq 'yokla') {
    $adaylar | Format-Table UsagePage, Usage, Giris, Cikis, Acildi -AutoSize | Out-String | Write-Host
    Write-Host "Seçilen: $($secilen.Yol)"
    Start-Sleep -Milliseconds 300
    $hid.Query()
    Start-Sleep -Milliseconds 1500
    $hid.Dispose()
    [Threading.Monitor]::Enter($hid.Sync)
    try { $hid.Log | ForEach-Object { Write-Host $_ } } finally { [Threading.Monitor]::Exit($hid.Sync) }
    Write-Host ("Yanıt: {0}  Seviye(0-8): {1}  Yüzde: %{2}  Durum baytı: 0x{3:X2}" -f $hid.Responses, $hid.LastLevel, [ArctisHid]::Percent($hid.LastLevel), $hid.LastStatus)
    if ($hid.LastError) { Write-Host "Hata: $($hid.LastError)" }
    return
}

$fazlar = @(
    @{ Ad = 'SESSİZ — hiç sorgu yok';        Sn = 20; Aralik = 0 },
    @{ Ad = '3 SANİYEDE BİR sorgu';          Sn = 30; Aralik = 3000 },
    @{ Ad = 'SESSİZ — hiç sorgu yok';        Sn = 15; Aralik = 0 },
    @{ Ad = 'SANİYEDE 10 sorgu';             Sn = 20; Aralik = 100 },
    @{ Ad = 'SESSİZ — pasif dinleme';        Sn = 30; Aralik = 0 }
)

$host.UI.RawUI.WindowTitle = 'Arctis Pil Testi'
Clear-Host
Write-Host ''
Write-Host '  ARCTIS PİL SORGUSU — CIZIRTI ÖLÇÜMÜ' -ForegroundColor Cyan
Write-Host ''
Write-Host '  1. Kulaklıkta müzik ya da video çalsın.'
Write-Host '  2. Bu pencere önde kalsın.'
Write-Host '  3. Cızırtıyı duyduğun AN  BOŞLUK  tuşuna bas.' -ForegroundColor Yellow
Write-Host ''
Write-Host '  Test 5 aşama, toplam ~2 dakika. Aşamaların adını görmeden de basabilirsin,'
Write-Host '  hangi aşamada olduğunu ben kayıttan eşleştiririm.'
Write-Host ''
for ($i = 5; $i -ge 1; $i--) { Write-Host -NoNewline "`r  Başlıyor: $i "; Start-Sleep 1 }

$basmalar = New-Object System.Collections.Generic.List[object]
$fazSonuc = @()

for ($f = 0; $f -lt $fazlar.Count; $f++) {
    $faz = $fazlar[$f]
    Clear-Host
    Write-Host ''
    Write-Host ("  AŞAMA {0}/{1}:  {2}" -f ($f + 1), $fazlar.Count, $faz.Ad) -ForegroundColor Cyan
    Write-Host '  Cızırtı duyunca BOŞLUK.' -ForegroundColor Yellow
    Write-Host ''
    $bas = $saat.Elapsed.TotalMilliseconds
    $bit = $bas + $faz.Sn * 1000
    $sonraki = $bas
    $q0 = $hid.QueryTimes.Count
    $r0 = $hid.Responses
    $b0 = $basmalar.Count
    $ekran = 0
    while ($saat.Elapsed.TotalMilliseconds -lt $bit) {
        $simdi = $saat.Elapsed.TotalMilliseconds
        if ($faz.Aralik -gt 0 -and $simdi -ge $sonraki) {
            $hid.Query()
            $sonraki += $faz.Aralik
        }
        while ([Console]::KeyAvailable) {
            $k = [Console]::ReadKey($true)
            if ($k.Key -eq 'Spacebar') {
                $basmalar.Add([pscustomobject]@{ T = $saat.Elapsed.TotalMilliseconds; Faz = $f })
                [Threading.Monitor]::Enter($hid.Sync)
                try { $hid.Log.Add(('{0:F0}' -f $saat.Elapsed.TotalMilliseconds) + "`tBOSLUK") } finally { [Threading.Monitor]::Exit($hid.Sync) }
            }
        }
        if ($simdi -ge $ekran) {
            $kalan = [math]::Ceiling(($bit - $simdi) / 1000)
            $yuzde = [ArctisHid]::Percent($hid.LastLevel)
            $pil = if ($yuzde -ge 0) { "%$yuzde" } else { '—' }
            Write-Host -NoNewline ("`r  Kalan {0,3} sn   Sorgu {1,4}   Yanıt {2,4}   Pil {3,5}   Basma {4,3}   " -f $kalan, ($hid.QueryTimes.Count - $q0), ($hid.Responses - $r0), $pil, ($basmalar.Count - $b0))
            $ekran = $simdi + 200
        }
        Start-Sleep -Milliseconds 5
    }
    $fazSonuc += [pscustomobject]@{
        Asama  = $f + 1
        Ad     = $faz.Ad
        Sn     = $faz.Sn
        Sorgu  = $hid.QueryTimes.Count - $q0
        Yanit  = $hid.Responses - $r0
        Basma  = $basmalar.Count - $b0
        _bas   = $bas
        _bit   = $bit
    }
}

Start-Sleep -Milliseconds 300
$hid.Dispose()

$sorgular = @($hid.QueryTimes)
$pushlar = @($hid.PushTimes)
$analiz = foreach ($b in $basmalar) {
    $once = $sorgular | Where-Object { $_ -le $b.T } | Select-Object -Last 1
    [pscustomobject]@{
        Asama            = $b.Faz + 1
        Zaman_sn         = [math]::Round($b.T / 1000, 2)
        SonSorgudan_ms   = if ($null -ne $once) { [math]::Round($b.T - $once) } else { $null }
    }
}

foreach ($s in $fazSonuc) {
    $s | Add-Member -NotePropertyName Olay -NotePropertyValue (@($pushlar | Where-Object { $_ -ge $s._bas -and $_ -lt $s._bit }).Count)
}

$damga = Get-Date -Format 'yyyyMMdd-HHmmss'
$logYol = Join-Path $kok "sonuc-$damga.log"
$ozetYol = Join-Path $kok "ozet-$damga.txt"

[Threading.Monitor]::Enter($hid.Sync)
try { $hid.Log | Set-Content -Path $logYol -Encoding UTF8 } finally { [Threading.Monitor]::Exit($hid.Sync) }

$ozet = @()
$ozet += "Arctis Pil Testi — $damga"
$ozet += "Arayüz: $($secilen.Yol)  UsagePage $($secilen.UsagePage)  Giriş $($secilen.Giris)  Çıkış $($secilen.Cikis)"
$ozet += "Son pil: seviye $($hid.LastLevel)/8  (%$([ArctisHid]::Percent($hid.LastLevel)))  durum 0x$('{0:X2}' -f $hid.LastStatus)"
if ($hid.LastError) { $ozet += "Hata: $($hid.LastError)" }
$ozet += ''
$ozet += ($fazSonuc | Select-Object Asama, Ad, Sn, Sorgu, Yanit, Olay, Basma | Format-Table -AutoSize | Out-String).TrimEnd()
$ozet += ''
$ozet += 'Her basma — son sorgudan geçen süre:'
$ozet += ($analiz | Format-Table -AutoSize | Out-String).TrimEnd()
$ozet | Set-Content -Path $ozetYol -Encoding UTF8

Clear-Host
Write-Host ''
Write-Host '  TEST BİTTİ' -ForegroundColor Green
Write-Host ''
$ozet | ForEach-Object { Write-Host "  $_" }
Write-Host ''
Write-Host "  Kayıt: $ozetYol"
Write-Host '  Bu pencereyi kapatabilirsin.'
