$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
$asm = [Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\bin\ArctisPil.exe'))
$bf = [Reflection.BindingFlags]'NonPublic,Public,Instance'
$tip = $asm.GetType('Uygulama')
$u = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($tip)
$tepsi = New-Object System.Windows.Forms.NotifyIcon
$tip.GetField('tepsi', $bf).SetValue($u, $tepsi)
$ciz = $tip.GetMethod('Ciz', $bf)
$ornek = @(@('50', '#34d399'), @('38', '#fbbf24'), @('12', '#ff54eb'), @('88', '#00f3ff'), @('100', '#34d399'), @('7', '#ff54eb'), @('–', '#ffffff'), @('?', '#ffffff'))
$tuval = New-Object Drawing.Bitmap (($ornek.Count * 80) + 20), 200
$g = [Drawing.Graphics]::FromImage($tuval)
$g.Clear([Drawing.ColorTranslator]::FromHtml('#202020'))
$g.InterpolationMode = 'NearestNeighbor'
$i = 0
foreach ($o in $ornek) {
    $ciz.Invoke($u, @($o[0], [Drawing.ColorTranslator]::FromHtml($o[1])))
    $bmp = $tepsi.Icon.ToBitmap()
    $g.DrawImage($bmp, (New-Object Drawing.Rectangle (20 + $i * 80), 20, 64, 64))
    $g.DrawImageUnscaled($bmp, 20 + $i * 80, 110)
        $i++
}
$tuval.Save((Join-Path $PSScriptRoot 'onizleme-simge.png'))

$p = [Activator]::CreateInstance($asm.GetType('SesPaneli'))
$pt = $p.GetType()
$pt.GetMethod('PilAyarla').Invoke($p, @([int]50, '', [Drawing.ColorTranslator]::FromHtml('#34d399')))
$pt.GetProperty('Dugme').SetValue($p, 22)
$pt.GetMethod('Ayarlar').Invoke($p, @([int]1, [int]7, [int]10))
$w = $pt.GetField('win', $bf).GetValue($p); $w.GetType().GetField('Deger').SetValue($w, [single]0.67)
$b = New-Object Drawing.Bitmap $p.Width, $p.Height
$p.DrawToBitmap($b, (New-Object Drawing.Rectangle 0, 0, $p.Width, $p.Height))
$b.Save((Join-Path $PSScriptRoot 'onizleme-panel.png'))
'ok'
