Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'Font.cs') -ReferencedAssemblies System.Drawing
$a = @('Bahnschrift','Bahnschrift SemiBold Condensed','Bahnschrift Condensed','Bahnschrift SemiBold SemiConden','Roboto Condensed','Barlow Condensed','Arial Narrow','Segoe UI','Tahoma','Verdana','Impact','Cascadia Mono')
$s = @(1,0,1,0,1,1,1,1,1,1,0,1)
$y = @('100','50','38','7')
[FontDene]::Tablo($a, $s, $y, 16, (Join-Path $PSScriptRoot 'font-16.png'))
[FontDene]::Tablo($a, $s, $y, 24, (Join-Path $PSScriptRoot 'font-24.png'))
'ok'
