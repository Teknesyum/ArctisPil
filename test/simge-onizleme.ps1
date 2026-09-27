Add-Type -AssemblyName System.Drawing
$o = New-Object Drawing.Bitmap 900, 280
$g = [Drawing.Graphics]::FromImage($o); $g.Clear([Drawing.ColorTranslator]::FromHtml('#202A31')); $g.InterpolationMode = 'NearestNeighbor'
$x = 10
foreach ($n in 16, 24, 32, 48, 256) {
  $i = New-Object Drawing.Icon ((Resolve-Path 'bin\obj\app.ico').Path), $n, $n
  $b = $i.ToBitmap()
  $z = if ($n -lt 256) { [int](96 / $n) * $n } else { 256 }
  $g.DrawImage($b, $x, 10, $z, $z); $x += $z + 20
}
$o.Save($args[0])
