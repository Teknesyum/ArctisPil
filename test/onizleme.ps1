param([string]$Cikti = (Join-Path $PSScriptRoot '..\docs\ui-denetim\2026-09-27'), [string]$Ek = 'sonra')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
$asm = [Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\bin\HeadsetBatteryTray.exe'))
$bf = [Reflection.BindingFlags]'NonPublic,Public,Instance,Static'
$tema = $asm.GetType('Tema')
$tema.GetField('Hareket', $bf).SetValue($null, $false)
New-Item -ItemType Directory -Force $Cikti | Out-Null
function Renk($ad) { $tema.GetField($ad, $bf).GetValue($null) }

$tip = $asm.GetType('Uygulama')
$u = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($tip)
$tepsi = New-Object System.Windows.Forms.NotifyIcon
$tip.GetField('tepsi', $bf).SetValue($u, $tepsi)
$ciz = $tip.GetMethod('Ciz', $bf)
$pilYazi = $tema.GetMethod('PilYazi')
$ornek = @(@('100', 100, $false), @('75', 75, $false), @('50', 50, $false), @('25', 25, $false), @('12', 12, $false), @('0', 0, $false), @('88', 88, $true), @('–', -1, $false), @('?', -1, $false))
$tuval = New-Object Drawing.Bitmap (($ornek.Count * 80) + 20), 200
$g = [Drawing.Graphics]::FromImage($tuval)
$g.Clear([Drawing.ColorTranslator]::FromHtml('#202A31'))
$g.InterpolationMode = 'NearestNeighbor'
$i = 0
foreach ($o in $ornek) {
    $c = if ($o[1] -lt 0 -or $o[2]) { Renk 'TextBody' } else { $pilYazi.Invoke($null, @([int]$o[1], [bool]$o[2])) }
    $ciz.Invoke($u, @($o[0], $c))
    $bmp = $tepsi.Icon.ToBitmap()
    $g.DrawImage($bmp, (New-Object Drawing.Rectangle (20 + $i * 80), 20, 64, 64))
    $g.DrawImageUnscaled($bmp, 20 + $i * 80, 110)
    $i++
}
$tuval.Save((Join-Path $Cikti "simge-100-$Ek.png"))
$tepsi.Dispose()

$pt = $asm.GetType('SesPaneli')
function Oge($p, $ad) { $pt.GetField($ad, $bf).GetValue($p) }
function Alan($o, $ad) { $o.GetType().GetField($ad, $bf).GetValue($o) }
function Kur($p, $ad, $v) { $pt.GetField($ad, $bf).SetValue($p, $v) }

function Panel($olcek, $durum) {
    $p = [Activator]::CreateInstance($pt)
    $p.Olcek = [single]$olcek
    switch ($durum) {
        'okunuyor' { }
        'yok' { $p.Kisitli = $true; $p.PilAyarla(-1, 'yok') }
        'kisitli' { $p.Kisitli = $true; $p.PilAyarla(75, '') }
        'kapali' { $p.PilAyarla(-1, 'kapalı') }
        default {
            $pil = @{ 'dolu' = 50; 'dusuk' = 12; 'sarj' = 62; 'odak' = 37; 'uzerinde' = 88 }[$durum]
            $p.PilAyarla($pil, $(if ($durum -eq 'sarj') { 'şarjda' } else { '' }))
            $p.Dugme = 22
            $p.Ayarlar($(if ($durum -eq 'dusuk') { 2 } else { 1 }), 7, 10)
        }
    }
    $w = Oge $p 'win'; $w.GetType().GetField('Deger').SetValue($w, [single]0.67)
    if ($durum -eq 'odak') { Kur $p 'odak' (Oge $p 'kul'); Kur $p 'klavye' $true }
    if ($durum -eq 'uzerinde') {
        $a = Oge $p 'anc'; $a.GetType().GetField('UzerindeSec').SetValue($a, 0)
        (Alan $a 'Uzerinde').Ata([single]1); Kur $p 'uzerinde' $a
    }
    if ($durum -eq 'yok-odak') { }
    return $p
}

function Kaydet($p, $yol) {
    $b = New-Object Drawing.Bitmap $p.Width, $p.Height
    $p.DrawToBitmap($b, (New-Object Drawing.Rectangle 0, 0, $p.Width, $p.Height))
    $b.Save($yol)
    $b.Dispose()
}

$olcekler = @{ '100' = 1.0; '125' = 1.25; '150' = 1.5 }
$durumlar = 'dolu', 'dusuk', 'sarj', 'odak', 'uzerinde', 'kapali', 'yok', 'kisitli', 'okunuyor'
foreach ($ad in $olcekler.Keys) {
    foreach ($d in $durumlar) {
        $p = Panel $olcekler[$ad] $d
        Kaydet $p (Join-Path $Cikti "panel-$d-$ad-$Ek.png")
        $p.Dispose()
    }
    $p = Panel $olcekler[$ad] 'yok'
    $dg = Oge $p 'dugmeOge'
    Kur $p 'odak' $dg; Kur $p 'klavye' $true
    (Alan $dg 'Boy').Ata([single]1.02); Kur $p 'uzerinde' $dg
    Kaydet $p (Join-Path $Cikti "panel-yok-dugme-$ad-$Ek.png")
    $p.Dispose()

    $tema.GetField('olcek', $bf).SetValue($null, [single]$olcekler[$ad])
    $m = New-Object System.Windows.Forms.ContextMenuStrip
    $tema.Assembly.GetType('MenuTemasi').GetMethod('Uygula').Invoke($null, [object[]]@($m.psobject.BaseObject))
    $d0 = New-Object System.Windows.Forms.ToolStripMenuItem "HeadsetBatteryTray v0.3.2"; $d0.Enabled = $false; [void]$m.Items.Add($d0)
    $d1 = New-Object System.Windows.Forms.ToolStripMenuItem 'Arctis pil: %50'; $d1.Enabled = $false; [void]$m.Items.Add($d1)
    [void]$m.Items.Add('Pili şimdi yenile')
    [void]$m.Items.Add((New-Object System.Windows.Forms.ToolStripSeparator))
    $a = New-Object System.Windows.Forms.ToolStripMenuItem 'Ses aktarma (20-80)'; $a.Checked = $true; [void]$m.Items.Add($a)
    [void]$m.Items.Add('Windows ile başlat')
    [void]$m.Items.Add((New-Object System.Windows.Forms.ToolStripSeparator))
    [void]$m.Items.Add('Bize ulaşın')
    $t = New-Object System.Windows.Forms.ToolStripMenuItem 'Teknesyum'; $t.ForeColor = Renk 'Renk1'; [void]$m.Items.Add($t)
    [void]$m.Items.Add('Destekle')
    [void]$m.Items.Add((New-Object System.Windows.Forms.ToolStripSeparator))
    [void]$m.Items.Add('Çıkış')
    $m.Show(-3000, -3000)
    $m.Items[2].Select()
    $bmp = New-Object Drawing.Bitmap $m.Width, $m.Height
    $m.DrawToBitmap($bmp, (New-Object Drawing.Rectangle 0, 0, $m.Width, $m.Height))
    $bmp.Save((Join-Path $Cikti "menu-$ad-$Ek.png"))
    $m.Close(); $m.Dispose()
}
$tema.GetField('olcek', $bf).SetValue($null, [single]0)
"aileler: " + $tema.GetMethod('Aileler').Invoke($null, @())
'ok'
