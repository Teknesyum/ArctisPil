# ArctisPil

Tray app for the SteelSeries Arctis Nova Pro Wireless base station (VID 1038, PID 12E0).

- `src/ArctisPil.cs` — whole app, .NET Framework 4 WinForms, single file.
- `build.ps1` — compiles to `bin/ArctisPil.exe` with the framework csc (full reference paths; the MSIX pwsh cwd breaks bare refs).
- `test/` — HID probes: `dinle.ps1` (dial listener), `komut-dene.ps1 -Komut 06-B0` (send one raw report, print reply).
- `rapor/` (gitignored, Turkish) — HID command research with sources, similar-project survey.

HID (MI_04): commands are 64-byte reports to COL01 (0xFFC0); events `07 xx` come on COL02 (0xFF00).
- Read `06 B0`: [6] battery 0-8, [8] transparency, [10] ANC mode, [15] 01 off / 02 charging / 08 on.
- Read `06 20`: [3] volume 0 loud…56 silent, [17] mic, [18] sidetone. Set: `06 25 v`, `06 BD m`, `06 B9 l`, `06 39 s`, `06 37 l`; `06 09` saves to flash.
- Never send `06 FD` (factory reset).

Volume transfer: dial event `07 25 00` → Windows ramps to 100%; first step down restores it; pending restore lives in `bin/aktarma.txt`.
`SesPaneli` rows are `Oge` items (bar or segment); settings save 0.8 s after the last change.
Tray digits: Bahnschrift Bold path stretched to 32x30; user preferred it over pixel blocks.
Multi-brand plan: `docs/plan.md`.
