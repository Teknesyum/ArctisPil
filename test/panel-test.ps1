param([string]$Cikti = (Join-Path $PSScriptRoot '..\docs\ui-denetim\2026-09-27\panel-test.txt'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
$asm = [Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\bin\HeadsetBatteryTray.exe'))
$bf = [Reflection.BindingFlags]'NonPublic,Public,Instance,Static'
$asm.GetType('Tema').GetField('Hareket', $bf).SetValue($null, $false)
$pt = $asm.GetType('SesPaneli')
$sonuc = New-Object System.Collections.Generic.List[string]
$hata = 0
function Dogrula($ad, $kosul) { if ($kosul) { $script:sonuc.Add("geçti  $ad") } else { $script:sonuc.Add("HATA   $ad"); $script:hata++ } }
function F($p, $ad) { $pt.GetField($ad, $bf).GetValue($p) }
function A($o, $ad) { $o.GetType().GetField($ad, $bf).GetValue($o) }
$tus = $pt.GetMethod('ProcessCmdKey', $bf)
function Bas($p, [Windows.Forms.Keys]$k) { $m = New-Object Windows.Forms.Message; [void]$tus.Invoke($p, [object[]]@($m, $k)) }
$fare = $pt.GetMethod('OnMouseDown', $bf); $birak = $pt.GetMethod('OnMouseUp', $bf)
function Tikla($p, $x, $y) {
    $e = New-Object Windows.Forms.MouseEventArgs ([Windows.Forms.MouseButtons]::Left), 1, $x, $y, 0
    [void]$fare.Invoke($p, [object[]]@($e.psobject.BaseObject)); [void]$birak.Invoke($p, [object[]]@($e.psobject.BaseObject))
}

foreach ($olcek in 1.0, 1.25, 1.5) {
    foreach ($durum in 'dolu', 'yok') {
        $p = [Activator]::CreateInstance($pt)
        $p.Olcek = [single]$olcek
        if ($durum -eq 'yok') { $p.Kisitli = $true; $p.PilAyarla(-1, 'yok') } else { $p.PilAyarla(50, ''); $p.Dugme = 22; $p.Ayarlar(1, 7, 10) }
        $gorunen = @((F $p 'ogeler') | Where-Object { -not (A $_ 'Gizli') })
        $cakisma = 0; $tasma = 0; $kucuk = 0
        for ($i = 0; $i -lt $gorunen.Count; $i++) {
            $r = A $gorunen[$i] 'Alan'
            if ($r.Left -lt 0 -or $r.Right -gt $p.ClientSize.Width -or $r.Bottom -gt $p.ClientSize.Height) { $tasma++ }
            if ($gorunen[$i].Odaklanir -and $r.Height -lt [Math]::Round(24 * $olcek)) { $kucuk++ }
            for ($j = $i + 1; $j -lt $gorunen.Count; $j++) { if ($r.IntersectsWith((A $gorunen[$j] 'Alan'))) { $cakisma++ } }
        }
        Dogrula "$durum %$($olcek*100): öğe alanları çakışmıyor ($cakisma)" ($cakisma -eq 0)
        Dogrula "$durum %$($olcek*100): öğeler pencere içinde ($tasma taşma)" ($tasma -eq 0)
        Dogrula "$durum %$($olcek*100): tıklama alanları ≥ 24 DIP ($kucuk küçük)" ($kucuk -eq 0)
        Dogrula "$durum %$($olcek*100): genişlik 300 DIP ($($p.ClientSize.Width))" ($p.ClientSize.Width -eq [Math]::Round(300 * $olcek))
        $p.Dispose()
    }
}

$p = [Activator]::CreateInstance($pt)
$p.PilAyarla(50, ''); $p.Dugme = 22; $p.Ayarlar(1, 7, 10)
$gonderilen = New-Object System.Collections.Generic.List[string]
$p.KulaklikAyar = [Action[int]] { param($v) $gonderilen.Add("kul $v") }
$p.AncAyar = [Action[int]] { param($v) $gonderilen.Add("anc $v") }
$kul = F $p 'kul'; $anc = F $p 'anc'; $seffaf = F $p 'seffaf'
Bas $p ([Windows.Forms.Keys]::Tab)
Dogrula 'Tab ilk odaklanabilir öğeye gider (Kulaklık sesi)' ((F $p 'odak') -eq $kul)
Dogrula 'klavye odağı halkayı açar' (F $p 'klavye')
$once = $kul.Deger
Bas $p ([Windows.Forms.Keys]::Right)
Dogrula "Sağ ok kulaklık sesini artırır ($([Math]::Round($once,2)) → $([Math]::Round($kul.Deger,2)))" ($kul.Deger -gt $once)
Dogrula 'kulaklığa komut gitti' ($gonderilen -contains "kul $((56 - [Math]::Round($kul.Deger * 56)))")
Bas $p ([Windows.Forms.Keys]::Home)
Dogrula 'Home değeri sıfırlar' ($kul.Deger -eq 0)
Bas $p ([Windows.Forms.Keys]::End)
Dogrula 'End değeri tama çıkarır' ($kul.Deger -eq 1)
Bas $p ([Windows.Forms.Keys]::Tab); Bas $p ([Windows.Forms.Keys]::Tab)
Dogrula 'Tab iki adım sonra Gürültü engelleme' ((F $p 'odak') -eq $anc)
Bas $p ([Windows.Forms.Keys]::Right)
Dogrula 'Sağ ok segmentte ANC seçer' ($anc.Secili -eq 2 -and $gonderilen -contains 'anc 2')
Dogrula 'ANC seçilince Şeffaflık gizlenir' ($seffaf.Gizli)
Bas $p ([Windows.Forms.Keys]::Left)
Dogrula 'Sol ok Şeffaf seçer, Şeffaflık görünür' ($anc.Secili -eq 1 -and -not $seffaf.Gizli)
Bas $p ([Windows.Forms.Keys]::Tab -bor [Windows.Forms.Keys]::Shift)
Dogrula 'Shift+Tab geri gider' ((F $p 'odak') -ne $anc)
$r = A $anc 'Alan'
Tikla $p ($r.X + 5) ($r.Y + 5)
Dogrula 'fare ile Kapalı seçilir' ($anc.Secili -eq 0 -and $gonderilen -contains 'anc 0')
Dogrula 'fare tıklaması odak halkasını kapatır' (-not (F $p 'klavye'))
$p.Location = New-Object Drawing.Point -4000, -4000
$p.Show()
Bas $p ([Windows.Forms.Keys]::Escape)
Dogrula 'Esc paneli kapatır' (-not $p.Visible)
$p.Dispose()

$p = [Activator]::CreateInstance($pt)
$p.Kisitli = $true; $p.PilAyarla(-1, 'yok')
$arandi = 0
$p.YenidenAra = [Action] { $script:arandi++ }
$dg = F $p 'dugmeOge'
Dogrula 'kulaklık yokken ipucu ve düğme görünür' (-not (A (F $p 'ipucu') 'Gizli') -and -not (A $dg 'Gizli'))
Dogrula "düğme metni 'Yeniden ara'" ($dg.Ad -eq 'Yeniden ara')
Bas $p ([Windows.Forms.Keys]::Tab)
Dogrula 'Tab düğmeye gider' ((F $p 'odak') -eq $dg)
Bas $p ([Windows.Forms.Keys]::Enter)
Dogrula 'Enter yeniden arar' ($arandi -eq 1)
$r = A $dg 'Alan'
Tikla $p ($r.X + 10) ($r.Y + 10)
Dogrula 'tıklama yeniden arar' ($arandi -eq 2)
$p.PilAyarla(80, '')
Dogrula 'pil gelince düğme kaybolur, kısıtlı açıklaması kalır' ((A $dg 'Gizli') -and -not (A (F $p 'ipucu') 'Gizli') -and (A (F $p 'ipucu') 'Ipucu') -like 'Bu kulaklıkta yalnız pil*')
$p.Kisitli = $false
Dogrula 'tam modda ipucu gizli' (A (F $p 'ipucu') 'Gizli')
$p.PilAyarla(-1, 'kapalı')
Dogrula "kapalıyken 'Yeniden dene'" ($dg.Ad -eq 'Yeniden dene' -and -not (A $dg 'Gizli'))
$p.Dispose()

$sonuc.Add("hata: $hata")
$sonuc | Set-Content -Path $Cikti -Encoding UTF8
$sonuc
if ($hata -gt 0) { exit 1 }
