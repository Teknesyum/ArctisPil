param([string]$Cikti = (Join-Path $PSScriptRoot '..\docs\pil-tahmin-test.txt'))
$ErrorActionPreference = 'Stop'
$asm = [Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\bin\HeadsetBatteryTray.exe'))
$tt = $asm.GetType('PilTahmin')
$sonuc = New-Object System.Collections.Generic.List[string]
$hata = 0
function Dogrula($ad, $kosul) { if ($kosul) { $script:sonuc.Add("geçti  $ad") } else { $script:sonuc.Add("HATA   $ad"); $script:hata++ } }
function Yeni($yol) { if (Test-Path $yol) { Remove-Item $yol }; [Activator]::CreateInstance($tt, [object[]]@([string]$yol)) }
function Ust($k) { [Math]::Floor([Math]::Max(0, $k) * 100 / 8) }
$gecici = Join-Path $env:TEMP 'pil-tahmin-test.txt'

$sonuc.Add('Sentetik pil: aktif dakikada %1/15, boşta %1/40; 8 saat yoğun (%80 sesli), 8 saat hafif (%10 sesli). Cihaz 8 kademe (tavan).')
$p = Yeni $gecici
$t = Get-Date '2026-01-01'
$pil = 100.0; $dk = 0; $ihlal = 0
for ($dongu = 1; $dongu -le 5; $dongu++) {
    $pil = 100.0; $hataTop = 0; $hataMax = 0; $n = 0; $t75 = $null; $k75 = -1; $bas = $t
    $p.Olay($t, 8, 'acik')
    while ($pil -gt 0) {
        $s = if ([Math]::Floor($dk / 480) % 2 -eq 0) { 0.8 } else { 0.1 }
        $pil -= $s / 15 + (1 - $s) / 40
        $dk++; $t = $t.AddMinutes(1)
        $k = [int][Math]::Ceiling([Math]::Max(0, $pil) / 12.5)
        $p.Olay($t, $k, 'acik')
        $p.Dakika($t, $s)
        $y = $p.Yuzde
        if ($k -ge 1 -and ($y -gt (Ust $k) -or $y -lt (Ust ($k - 1)) + 1)) { $ihlal++ }
        if ($pil -gt 0) { $e = [Math]::Abs($y - $pil); $hataTop += $e; $n++; if ($e -gt $hataMax) { $hataMax = $e } }
        if ($null -eq $t75 -and $pil -le 75) { $t75 = $t; $k75 = $p.KalanDakika }
    }
    $gercek = ($t - $t75).TotalMinutes
    $oran = if ($k75 -gt 0) { '{0:0.00}' -f ($k75 / $gercek) } else { 'yok' }
    $sonuc.Add(('döngü {0}: süre {1:0} dk, ortalama sapma %{2:0.00}, en büyük %{3:0.00}, %75 anında kalan tahmin/gerçek {4}, m {5:0.00}, w {6:0.00}, öğrenilen bant {7}' -f $dongu, ($t - $bas).TotalMinutes, ($hataTop / $n), $hataMax, $oran, $p.M, $p.W, $p.Bantlar))
    if ($dongu -eq 1) { $ilk = $hataTop / $n }
    $son = $hataTop / $n; $sonOran = $k75 / $gercek
    $p.Olay($t, 0, 'kapali')
}
Dogrula 'tahmin cihaz bandının dışına hiç çıkmadı (13''te bekler, 12''yi geçmez)' ($ihlal -eq 0)
Dogrula ('5. döngü sapması 1. döngüden küçük ({0:0.00} < {1:0.00})' -f $son, $ilk) ($son -lt $ilk)
Dogrula ('5. döngüde ortalama sapma %2''nin altında ({0:0.00})' -f $son) ($son -lt 2)
Dogrula ('boşta ağırlığı w gerçeğe yakın (gerçek 0,375; bulunan {0:0.00})' -f $p.W) ([Math]::Abs($p.W - 0.375) -lt 0.1)
Dogrula ('kalan süre tahmini %20 içinde (oran {0:0.00})' -f $sonOran) ([Math]::Abs($sonOran - 1) -lt 0.2)

$p.Olay($t, 8, 'acik'); $t = $t.AddMinutes(1); $p.Dakika($t, 1)
for ($i = 0; $i -lt 60; $i++) { $t = $t.AddMinutes(1); $p.Dakika($t, 0.5) }
$p.Kaydet()
$q = [Activator]::CreateInstance($tt, [object[]]@([string]$gecici))
Dogrula "kaydet/yükle aynı tahmini verir ($($p.Yuzde) = $($q.Yuzde))" ($p.Yuzde -eq $q.Yuzde -and [Math]::Abs($p.M - $q.M) -lt 0.001 -and [Math]::Abs($p.W - $q.W) -lt 0.001)
$p.Olay($t, 6, 'sarj')
Dogrula "şarjda cihaz değeri gösterilir ($($p.Yuzde))" ($p.Yuzde -eq 75)
$p.Olay($t, 8, 'acik')
Dogrula 'şarj bitip 8''e dönünce temiz başlangıç, %100' ($p.Yuzde -eq 100 -and -not $p.Kesik)
$p.Olay($t, 5, 'acik')
Dogrula 'iki kademe birden düşüş öğrenilmez (kesik)' ($p.Kesik)
$t = $t.AddMinutes(1); $p.Dakika($t, 1); $t = $t.AddMinutes(300); $p.Dakika($t, 1)
Dogrula '3 saatten uzun boşluk bandı kesik yapar' ($p.Kesik)
Remove-Item $gecici

$kayit = Join-Path $env:LOCALAPPDATA 'HeadsetBatteryTray\pil-1038-12e0.csv'
if (Test-Path $kayit) {
    $r = Yeni $gecici
    $r.Oynat($kayit)
    $sonuc.Add(('gerçek kayıt oynatıldı: kademe {0}, tahmin %{1}, aralık {2}, aktif {3:0} dk, boş {4:0} dk, kesik {5}, öğrenilen bant {6}' -f $r.Kademe, $r.Yuzde, $r.Aralik, $r.Aktif, $r.Bos, $r.Kesik, $r.Bantlar))
    Remove-Item $gecici -ErrorAction SilentlyContinue
}

$sonuc.Add("hata: $hata")
$sonuc | Set-Content -Path $Cikti -Encoding UTF8
$sonuc
if ($hata -gt 0) { exit 1 }
