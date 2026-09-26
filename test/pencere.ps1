param([string]$Cikti = (Join-Path $PSScriptRoot '..\docs\ui-denetim\2026-09-27\panel-gercek-100-sonra.png'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class Pencere
{
    public delegate bool Geri(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] public static extern bool EnumWindows(Geri g, IntPtr l);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
    public static IntPtr Bul(uint pid)
    {
        IntPtr bulunan = IntPtr.Zero;
        EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); RECT r; if (p == pid && IsWindowVisible(h) && GetWindowRect(h, out r) && r.R - r.L > 100) { bulunan = h; return false; } return true; }, IntPtr.Zero);
        return bulunan;
    }
}
'@
$exe = (Resolve-Path (Join-Path $PSScriptRoot '..\bin\HeadsetBatteryTray.exe')).Path
$calisan = Get-Process HeadsetBatteryTray -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $calisan) { $calisan = Start-Process $exe -PassThru; Start-Sleep -Seconds 6 }
if ($calisan.Path -ne $exe) { throw "çalışan kopya bu depodan değil: $($calisan.Path)" }
$ikinci = Start-Process $exe -PassThru
$ikinci.WaitForExit(5000) | Out-Null
"ikinci kopya çıktı: $($ikinci.HasExited), kod $($ikinci.ExitCode)"
Start-Sleep -Milliseconds 800
$h = [Pencere]::Bul([uint32]$calisan.Id)
if ($h -eq [IntPtr]::Zero) { throw 'panel penceresi bulunamadı' }
$r = New-Object Pencere+RECT
[void][Pencere]::GetWindowRect($h, [ref]$r)
$w = $r.R - $r.L; $y = $r.B - $r.T
$b = New-Object Drawing.Bitmap $w, $y
$g = [Drawing.Graphics]::FromImage($b)
$g.CopyFromScreen($r.L, $r.T, 0, 0, $b.Size)
$b.Save($Cikti)
"panel: $($r.L),$($r.T) ${w}x$y → $Cikti"
[void][Pencere]::PostMessage($h, 0x100, [IntPtr]0x1B, [IntPtr]::Zero)
Start-Sleep -Milliseconds 500
"Esc sonrası görünür: $([Pencere]::IsWindowVisible($h))"
