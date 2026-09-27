param([string]$Jeton, [string]$Cikti, [string]$Png)
Add-Type -AssemblyName System.Drawing
$j = Get-Content -Raw -Encoding UTF8 $Jeton | ConvertFrom-Json
$zemin = [Drawing.ColorTranslator]::FromHtml($j.brand.surface.value)
$cizgi = [Drawing.ColorTranslator]::FromHtml($j.brand.'renk-1'.value)
$dolgu = [Drawing.ColorTranslator]::FromHtml($j.role.success.value)
$yaricap = $j.shape.'r-window'.value
$kalin = $j.shape.'border-w'.value
$boylar = 16, 20, 24, 32, 40, 48, 64, 256
$pngler = foreach ($n in $boylar) {
    $o = $n / 16.0
    $b = New-Object Drawing.Bitmap $n, $n
    $g = [Drawing.Graphics]::FromImage($b)
    $g.SmoothingMode = 'AntiAlias'
    $g.PixelOffsetMode = 'HighQuality'
    $r = [math]::Max(1, $yaricap * $o)
    $yol = New-Object Drawing.Drawing2D.GraphicsPath
    $yol.AddArc(0, 0, 2 * $r, 2 * $r, 180, 90); $yol.AddArc($n - 2 * $r, 0, 2 * $r, 2 * $r, 270, 90)
    $yol.AddArc($n - 2 * $r, $n - 2 * $r, 2 * $r, 2 * $r, 0, 90); $yol.AddArc(0, $n - 2 * $r, 2 * $r, 2 * $r, 90, 90); $yol.CloseFigure()
    $g.FillPath((New-Object Drawing.SolidBrush $zemin), $yol)
    $k = [math]::Max($kalin, [math]::Round(2 * $o))
    $x = 1 * $o; $y = 4 * $o; $w = 12 * $o; $h = 8 * $o
    $g.SmoothingMode = 'None'
    $g.FillRectangle((New-Object Drawing.SolidBrush $cizgi), [float]$x, [float]$y, [float]$w, [float]$h)
    $g.FillRectangle((New-Object Drawing.SolidBrush $zemin), [float]($x + $k), [float]($y + $k), [float]($w - 2 * $k), [float]($h - 2 * $k))
    $g.FillRectangle((New-Object Drawing.SolidBrush $cizgi), [float]($x + $w), [float]($y + 2 * $o), [float]$k, [float]($h - 4 * $o))
    $ic = $k + [math]::Max(1, [math]::Round($o))
    $g.FillRectangle((New-Object Drawing.SolidBrush $dolgu), [float]($x + $ic), [float]($y + $ic), [float](($w - 2 * $ic) * 0.75), [float]($h - 2 * $ic))
    $g.Dispose()
    $m = New-Object IO.MemoryStream
    if ($n -ge 256) { $b.Save($m, [Drawing.Imaging.ImageFormat]::Png) }
    else {
        $d = New-Object IO.BinaryWriter $m
        $d.Write([uint32]40); $d.Write([int32]$n); $d.Write([int32](2 * $n)); $d.Write([uint16]1); $d.Write([uint16]32)
        $d.Write([uint32]0); $d.Write([uint32]0); $d.Write([int32]0); $d.Write([int32]0); $d.Write([uint32]0); $d.Write([uint32]0)
        for ($yy = $n - 1; $yy -ge 0; $yy--) { for ($xx = 0; $xx -lt $n; $xx++) { $c = $b.GetPixel($xx, $yy); $d.Write([byte]$c.B); $d.Write([byte]$c.G); $d.Write([byte]$c.R); $d.Write([byte]$c.A) } }
        $d.Write((New-Object byte[] ([int]([math]::Ceiling($n / 32.0) * 4 * $n))))
        $d.Flush()
    }
    $b.Dispose()
    ,$m.ToArray()
}
$f = New-Object IO.MemoryStream
$w = New-Object IO.BinaryWriter $f
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$boylar.Count)
$ofs = 6 + 16 * $boylar.Count
for ($i = 0; $i -lt $boylar.Count; $i++) {
    $n = $boylar[$i]
    $w.Write([byte]($n % 256)); $w.Write([byte]($n % 256)); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32); $w.Write([uint32]$pngler[$i].Length); $w.Write([uint32]$ofs)
    $ofs += $pngler[$i].Length
}
foreach ($p in $pngler) { $w.Write($p) }
[IO.File]::WriteAllBytes($Cikti, $f.ToArray())
if ($Png) { [IO.File]::WriteAllBytes($Png, $pngler[-1]) }
