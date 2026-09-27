# HeadsetBatteryTray

Tray battery app for headsets; full panel for SteelSeries Arctis Nova Pro Wireless (VID 1038, PID 12E0).

- `src/HeadsetBatteryTray.cs` — whole app, .NET Framework 4 WinForms, single file. `Uygulama.Ad`/`Surum` drive name, version, log, mutex, Run key (old `ArctisPil` key is removed).
- `build.ps1` — framework csc → `bin/HeadsetBatteryTray.exe`; embeds `vendor/headsetcontrol.exe` as a resource. UI font is system Segoe UI (owner's choice); tray digits at 16 px are a hand-drawn pixel set.
- `test/` — HID probes (`dinle.ps1`, `komut-dene.ps1`), `onizleme.ps1` (every state at 100/125/150%), `panel-test.ps1` (headless layout/keyboard), `kontrast.ps1`, `pencere.ps1` (real window), `hc-coz.ps1`, `bt-pil.ps1`.
- UI: `build.ps1` lowers generated `teknesyum-ui/winforms/Palette.cs` + `theme.tokens.json` (durations, easings, tone scale) to C# 5 in `bin/obj/Duzen.cs`; `Tema` only aliases it. Menu brand/support text from embedded `labels.tr.json` (`Etiket.Al`). Exe icon drawn from tokens by `simge.ps1`. Owner exceptions: battery red `#FF9898`, taskbar `#202A31`. Audit: `docs/ui-denetim/`.
- Base: `teknesyum.json` (portable, icon `assets/icon.png` written by `simge.ps1 -Png`). `Golge` runs a shadow copy from `%LOCALAPPDATA%\HeadsetBatteryTray\calisan` with `--kaynak`; source exe changed → restart, gone 20 s → cleanup (Run, HKCU key, data) and exit. Lifecycle evidence: `docs/ui-denetim/2026-09-27/base/`.
- `docs/` — `plan.md`, `plan-ses-koruma.md` (Loudness Equalization toggle, plan only), `devices.csv` (popular 50).
- `rapor/` (gitignored, Turkish) — research.

Battery order: Nova HID → HeadsetControl 4.1.0 (`-b -o json`, extracted to %LOCALAPPDATA%) → Windows BT battery DEVPKEY on BTHENUM profile nodes. Non-Nova sets `panel.Kisitli` (battery only).
Nova HID (MI_04): 64-byte reports to COL01 (0xFFC0), events `07 xx` on COL02. `06 B0` status, `06 20` audio, set `06 25 v` / `06 BD m` / `06 B9 l` / `06 37 l`, `06 09` saves. Never send `06 FD`.
Volume transfer 20-80: v<11 or v>45 clamps the headset and moves the rest to Windows; turning back moves Windows toward 50% first; bars snap to 5%.
No own drivers for other brands: missing headsets go upstream to HeadsetControl.
Battery estimator: `PilTahmin` (Nova, 8 levels) learns minutes per 1% from the log, state in `pil-<device>.txt`; test `test/tahmin-test.ps1`; plan docs/plan-pil-tahmin.md.
