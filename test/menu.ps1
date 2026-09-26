$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
$asm = [Reflection.Assembly]::LoadFrom((Join-Path $PSScriptRoot '..\bin\HeadsetBatteryTray.exe'))
$m = New-Object System.Windows.Forms.ContextMenuStrip
$m.Renderer = [Activator]::CreateInstance($asm.GetType('MenuTemasi'))
$m.ForeColor = [Drawing.Color]::White
$m.ShowImageMargin = $false
$m.ShowCheckMargin = $true
$d = New-Object System.Windows.Forms.ToolStripMenuItem 'Pil %50 · açık'
$d.Enabled = $false
[void]$m.Items.Add($d)
[void]$m.Items.Add('Pili şimdi yenile')
[void]$m.Items.Add((New-Object System.Windows.Forms.ToolStripSeparator))
$a = New-Object System.Windows.Forms.ToolStripMenuItem 'Ses aktarma (20-80)'
$a.Checked = $true
[void]$m.Items.Add($a)
[void]$m.Items.Add('Windows ile başlat')
[void]$m.Items.Add((New-Object System.Windows.Forms.ToolStripSeparator))
[void]$m.Items.Add('Bize ulaşın')
$t = New-Object System.Windows.Forms.ToolStripMenuItem 'Teknesyum'
$t.ForeColor = [Drawing.ColorTranslator]::FromHtml('#00f3ff')
[void]$m.Items.Add($t)
[void]$m.Items.Add('Destekle')
[void]$m.Items.Add((New-Object System.Windows.Forms.ToolStripSeparator))
[void]$m.Items.Add('Çıkış')
$m.Show(-2000, -2000)
$m.Items[1].Select()
$bmp = New-Object Drawing.Bitmap $m.Width, $m.Height
$m.DrawToBitmap($bmp, (New-Object Drawing.Rectangle 0, 0, $m.Width, $m.Height))
$bmp.Save((Join-Path $PSScriptRoot 'onizleme-menu.png'))
$m.Close()
'ok'
