param([string]$Sol, [string]$Sag, [string]$Cikti)
Add-Type -AssemblyName System.Drawing
$a = [Drawing.Image]::FromFile((Resolve-Path $Sol).Path)
$b = [Drawing.Image]::FromFile((Resolve-Path $Sag).Path)
$o = New-Object Drawing.Bitmap ($a.Width + $b.Width), ([math]::Max($a.Height, $b.Height))
$g = [Drawing.Graphics]::FromImage($o)
$g.DrawImageUnscaled($a, 0, 0); $g.DrawImageUnscaled($b, $a.Width, 0)
$g.Dispose(); $a.Dispose(); $b.Dispose()
$o.Save($Cikti)
