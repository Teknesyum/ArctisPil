param([string]$Cikti = (Join-Path $PSScriptRoot '..\docs\ui-denetim\2026-09-27\kontrast.txt'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
$asm = [Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\bin\HeadsetBatteryTray.exe'))
$bf = [Reflection.BindingFlags]'NonPublic,Public,Instance,Static'
$tema = $asm.GetType('Tema')
$tema.GetField('Hareket', $bf).SetValue($null, $false)
function T($ad) { $tema.GetField($ad, $bf).GetValue($null) }

function Bindir($ust, $alt) {
    $a = $ust.A / 255.0
    [Drawing.Color]::FromArgb(255, [int][Math]::Round($ust.R * $a + $alt.R * (1 - $a)), [int][Math]::Round($ust.G * $a + $alt.G * (1 - $a)), [int][Math]::Round($ust.B * $a + $alt.B * (1 - $a)))
}
function Kanal($v) { $s = $v / 255.0; if ($s -le 0.04045) { $s / 12.92 } else { [Math]::Pow(($s + 0.055) / 1.055, 2.4) } }
function L($c) { 0.2126 * (Kanal $c.R) + 0.7152 * (Kanal $c.G) + 0.0722 * (Kanal $c.B) }
function Oran($a, $b) { $x = L $a; $y = L $b; ([Math]::Max($x, $y) + 0.05) / ([Math]::Min($x, $y) + 0.05) }
function Hex($c) { '#{0:X2}{1:X2}{2:X2}' -f $c.R, $c.G, $c.B }

$yuzey = T 'Surface'
$r20 = Bindir (T 'Renk1Yuzde20') $yuzey
$r30 = Bindir (T 'Renk1Yuzde30') $yuzey
$sonuc = New-Object System.Collections.Generic.List[object]
function Olc($ekran, $oge, $hal, $on, $zemin, $esik) {
    $on = Bindir $on $zemin
    $o = Oran $on $zemin
    $sonuc.Add([pscustomobject]@{ Ekran = $ekran; Oge = $oge; Hal = $hal; On = Hex $on; Zemin = Hex $zemin; Oran = [Math]::Round($o, 2); Esik = $esik; Sonuc = $(if ($o -ge $esik) { 'geçti' } else { 'HATA' }) })
}

$metin = 7.0; $nesne = 3.0
Olc 'panel' 'etiket' 'dinlenme' (T 'TextLabel') $yuzey $metin
Olc 'panel' 'değer' 'dinlenme' (T 'TextBody') $yuzey $metin
foreach ($y in 0, 25, 50, 75, 100) { Olc 'panel' "pil değeri %$y" 'dinlenme' ($tema.GetMethod('PilRenk').Invoke($null, [object[]]@([int]$y))) $yuzey $metin }
Olc 'panel' 'pil değeri şarjda' 'dinlenme' (T 'Renk1') $yuzey $metin
Olc 'panel' 'ipucu metni' 'boş/hata' (T 'TextBody') $yuzey $metin
Olc 'panel' 'segment metni' 'dinlenme' (T 'TextBody') $r20 $metin
Olc 'panel' 'segment metni' 'üzerinde' (T 'TextBody') $r30 $metin
Olc 'panel' 'segment metni' 'seçili' (T 'Surface') (T 'Renk1') $metin
Olc 'panel' 'düğme metni' 'dinlenme/üzerinde/basılı/odak' (T 'Surface') (T 'Renk1') $metin
Olc 'panel' 'odak halkası' 'odak' (T 'FocusRing') $yuzey $nesne
Olc 'panel' 'çubuk/segment kenarı' 'dinlenme' (T 'BorderDefault') $yuzey $nesne
Olc 'panel' 'pencere kenarı' 'dinlenme' (T 'BorderDefault') $yuzey $nesne
foreach ($d in 'Renk1', 'Renk3', 'Success', 'Danger') {
    Olc 'panel' "çubuk dolgusu $d" 'dinlenme' (T $d) $r20 $nesne
    Olc 'panel' "çubuk dolgusu $d" 'üzerinde' (T $d) $r30 $nesne
}
Olc 'panel' 'seçili segment' 'seçili' (T 'Renk1') $yuzey $nesne
Olc 'panel' 'düğme dolgusu' 'dinlenme' (T 'Renk1') $yuzey $nesne
Olc 'panel' 'ikincil düğme metni (kapalı)' 'dinlenme/üzerinde/basılı/odak' (T 'TextBody') $r20 $metin
Olc 'panel' 'ikincil düğme kenarı (kapalı)' 'dinlenme' (T 'BorderDefault') $yuzey $nesne
Olc 'menü' 'öğe metni' 'dinlenme' (T 'TextBody') $yuzey $metin
Olc 'menü' 'öğe metni' 'üzerinde/klavye seçimi' (T 'TextBody') $r20 $metin
Olc 'menü' 'bilgi satırı (etkisiz)' 'dinlenme' (T 'TextBody') $yuzey $metin
Olc 'menü' 'Teknesyum' 'dinlenme' (T 'Renk1') $yuzey $metin
Olc 'menü' 'Teknesyum' 'üzerinde' (T 'Renk1') $r20 $metin
Olc 'menü' 'onay işareti' 'seçili' (T 'Renk1') $yuzey $nesne
Olc 'menü' 'onay işareti' 'üzerinde' (T 'Renk1') $r20 $nesne
Olc 'menü' 'kenar' 'dinlenme' (T 'BorderDefault') $yuzey $nesne

$ekran = [Windows.Forms.Screen]::PrimaryScreen
$gorev = $ekran.Bounds.Bottom - $ekran.WorkingArea.Bottom
if ($gorev -gt 0) {
    $bmp = New-Object Drawing.Bitmap $ekran.Bounds.Width, 1
    $g = [Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($ekran.Bounds.Left, $ekran.WorkingArea.Bottom + [int]($gorev / 2), 0, 0, $bmp.Size)
    $ornek = 0..($bmp.Width - 1) | ForEach-Object { $bmp.GetPixel($_, 0).ToArgb() } | Group-Object | Sort-Object Count -Descending | Select-Object -First 1
    $cubuk = [Drawing.Color]::FromArgb([int]$ornek.Name)
    Olc 'tepsi simgesi' 'rakam şarjda' 'görev çubuğu (ölçülen)' (T 'TextBody') $cubuk $metin
    $pilRenk = $tema.GetMethod('PilRenk')
    foreach ($y in 0, 12, 25, 37, 50, 62, 75, 87, 100) { Olc 'tepsi simgesi' "rakam %$y" 'görev çubuğu (ölçülen)' ($pilRenk.Invoke($null, [object[]]@([int]$y))) $cubuk $metin }
    $en = 99; foreach ($y in 0..100) { $o = Oran ($pilRenk.Invoke($null, [object[]]@([int]$y))) $cubuk; if ($o -lt $en) { $en = $o; $enY = $y } }
    Olc 'tepsi simgesi' "rakam en düşük (%$enY)" 'görev çubuğu (ölçülen), 0-100 tarandı' ($pilRenk.Invoke($null, [object[]]@([int]$enY))) $cubuk $metin
}

$pt = $asm.GetType('SesPaneli')
$p = [Activator]::CreateInstance($pt)
$p.Olcek = [single]1
$p.PilAyarla(50, ''); $p.Dugme = 22; $p.Ayarlar(1, 7, 10)
$w = $pt.GetField('win', $bf).GetValue($p); $w.GetType().GetField('Deger').SetValue($w, [single]0.67)
$anc = $pt.GetField('anc', $bf).GetValue($p)
$anc.GetType().GetField('UzerindeSec').SetValue($anc, 0)
$anc.GetType().GetField('Uzerinde').GetValue($anc).Ata([single]1)
$pt.GetField('uzerinde', $bf).SetValue($p, $anc)
$b = New-Object Drawing.Bitmap $p.Width, $p.Height
$p.DrawToBitmap($b, (New-Object Drawing.Rectangle 0, 0, $p.Width, $p.Height))
$a = $anc.GetType().GetField('Alan').GetValue($anc)
$hw = ($a.Width - 8) / 3
$mik = $pt.GetField('mik', $bf).GetValue($p); $ma = $mik.GetType().GetField('Alan').GetValue($mik)
$kul = $pt.GetField('kul', $bf).GetValue($p); $ka = $kul.GetType().GetField('Alan').GetValue($kul)
$pikseller = @(
    @('segment zemini (üzerinde)', ($a.X + 4), ($a.Y + 3), $r30),
    @('segment zemini (dinlenme)', [int]($a.X + 2 * ($hw + 4) + 4), ($a.Y + 3), $r20),
    @('segment zemini (seçili)', [int]($a.X + ($hw + 4) + 4), ($a.Y + 3), (T 'Renk1')),
    @('çubuk izi (boş kısım)', ($ka.Right - 8), ($ka.Y + $ka.Height / 2), $r20),
    @('panel zemini', 4, [int]($p.Height / 2), $yuzey)
)
$piksel = foreach ($x in $pikseller) {
    $c = $b.GetPixel([int]$x[1], [int]$x[2])
    $fark = [Math]::Max([Math]::Abs($c.R - $x[3].R), [Math]::Max([Math]::Abs($c.G - $x[3].G), [Math]::Abs($c.B - $x[3].B)))
    [pscustomobject]@{ Nokta = $x[0]; Beklenen = Hex $x[3]; Olculen = Hex $c; Fark = $fark; Sonuc = $(if ($fark -le 2) { 'geçti' } else { 'HATA' }) }
}

$hata = @($sonuc | Where-Object Sonuc -eq 'HATA').Count + @($piksel | Where-Object Sonuc -eq 'HATA').Count
$rapor = @()
$rapor += 'Kontrast Ölçümü — ' + (Get-Date -Format 'yyyy-MM-dd HH:mm') + ' — bin\HeadsetBatteryTray.exe içindeki Tema değerleri'
$rapor += ($sonuc | Format-Table -AutoSize | Out-String -Width 200)
$rapor += 'Piksel doğrulaması (100%, gerçek render)'
$rapor += ($piksel | Format-Table -AutoSize | Out-String -Width 200)
$rapor += "hata: $hata"
$rapor | Set-Content -Path $Cikti -Encoding UTF8
$rapor
if ($hata -gt 0) { exit 1 }
