# HeadsetBatteryTray

Tray battery app for headsets; full panel for SteelSeries Arctis Nova Pro Wireless (VID 1038, PID 12E0).

- `src/HeadsetBatteryTray.cs` — whole app, .NET Framework 4 WinForms, single file. `Uygulama.Ad`/`Surum` drive name, version, log, mutex, Run key (old `ArctisPil` key is removed).
- `build.ps1` — framework csc → `bin/HeadsetBatteryTray.exe`; embeds `vendor/headsetcontrol.exe` and `assets/fonts/*.ttf` as resources.
- `test/` — HID probes (`dinle.ps1`, `komut-dene.ps1`), `onizleme.ps1` (every state at 100/125/150%), `panel-test.ps1` (headless layout/keyboard), `kontrast.ps1`, `pencere.ps1` (real window), `hc-coz.ps1`, `bt-pil.ps1`.
- UI: `Tema` holds teknesyum-ui `benim` tokens (C# 5 port of `teknesyum-ui/winforms/Palette.cs`); colors only from `Tema`. Audit: `docs/ui-denetim/`.
- `docs/` — `plan.md`, `plan-ses-koruma.md` (Loudness Equalization toggle, plan only), `devices.csv` (popular 50).
- `rapor/` (gitignored, Turkish) — research.

Battery order: Nova HID → HeadsetControl 4.1.0 (`-b -o json`, extracted to %LOCALAPPDATA%) → Windows BT battery DEVPKEY on BTHENUM profile nodes. Non-Nova sets `panel.Kisitli` (battery only).
Nova HID (MI_04): 64-byte reports to COL01 (0xFFC0), events `07 xx` on COL02. `06 B0` status, `06 20` audio, set `06 25 v` / `06 BD m` / `06 B9 l` / `06 37 l`, `06 09` saves. Never send `06 FD`.
Volume transfer 20-80: v<11 or v>45 clamps the headset and moves the rest to Windows; turning back moves Windows toward 50% first; bars snap to 5%.
No own drivers for other brands: missing headsets go upstream to HeadsetControl.
Battery estimator (1% from 12.5% steps): plan only, docs/plan-pil-tahmin.md.
