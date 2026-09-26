<!-- lang -->

[<img src="assets/badge-lang.svg" alt="English selected, switch to Türkçe" width="124" height="44">](README.tr.md)

# ArctisPil

Tray control for Arctis Nova Pro Wireless.

| Measure | Value |
|---|---|
| Similar projects surveyed | 37 |
| HID commands used, each backed by 2+ public sources | 7 |
| Commands checked on real hardware (PID 12E0) | 3 (`06 B0`, `06 20`, `06 25`) |
| Dependencies | 0 (.NET Framework 4, ships with Windows) |

## What It Is

ArctisPil is a single Windows tray program for the SteelSeries Arctis Nova Pro Wireless base station. The tray icon shows the battery percentage. A left click opens a panel with the battery, headset volume, Windows volume, noise cancelling, transparency (shown only in transparency mode) and microphone level; every row except the battery can be changed from the panel. It talks to the base station over HID and does not need SteelSeries GG running.

## Doesn't SteelSeries GG Already Do This?

GG does all of this and much more: Sonar, per-app mixing, EQ editing, firmware updates. ArctisPil adds:

- **Battery in the tray** as a number, colored by level, blue while charging, with a warning at 25%.
- **One panel for both volumes.** The headset volume and the Windows volume sit next to each other; both can be dragged or scrolled.
- **Volume transfer (20-80).** The headset stays between 20% and 80%. Turn the dial past 80% and the headset is set back to 80% while the extra points go to Windows; below 20% the headset returns to 20% and the difference comes off Windows. On the way back Windows moves first: turning down from 80% lowers Windows until it reaches 50%, turning up from 20% raises it to 50%, then the headset follows.
- **Round numbers.** Both volume bars snap to 5% steps on drag and wheel.
- **No background suite.** One 30 KB exe, no services, no account.

## Features

- **Battery icon** — percentage drawn in the tray icon, refreshed every minute and on every headset event.
- **Headset volume** — read at start, follows the dial, settable from the panel (`06 25`).
- **Noise cancelling** — Off / Transparency / ANC, and the transparency level 1-10.
- **Microphone level** — 1-10.
- **Settings saved to the device** — changes are written to the base station 0.8 s after the last change (`06 09`), so they survive a power cycle.

## What It Does Not Do

- No EQ editing, no Sonar, no ChatMix control, no firmware update.
- Only the Arctis Nova Pro Wireless PC base station (VID 1038, PID 12E0) is supported. Other headsets are planned, see [issue template](../../issues/new?template=device.yml).
- It never sends the factory reset command (`06 FD`).
- Windows only.

## Install

Download `ArctisPil.exe` from the [latest release](../../releases/latest) and run it. It adds itself to startup; untick "Windows ile başlat" in the right-click menu to stop that.

## How It Works

The base station exposes a vendor HID interface (interface 4). Commands go to the `0xFFC0` collection as 64-byte reports starting with `06`; unsolicited events arrive on `0xFF00` starting with `07`.

| Command | Meaning |
|---|---|
| `06 B0` | status: battery `[6]`, headset state `[15]`, transparency `[8]`, ANC mode `[10]` |
| `06 20` | audio status: volume `[3]`, microphone `[17]` |
| `06 25 v` | headset volume, 0 = loudest, 56 = silent |
| `06 BD m` | 0 off, 1 transparency, 2 ANC |
| `06 B9 l` | transparency level 1-10 |
| `06 37 l` | microphone level 1-10 |
| `06 09` | save settings to the device |

Any `07 xx` event other than the dial triggers a fresh `06 B0` / `06 20` read, so a press of the ANC button on the headset shows up in the panel.

## Program Shows What It Does

![The panel: battery, headset volume, Windows volume, noise cancelling, transparency and microphone rows.](assets/panel.png)

## Development

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

`src/ArctisPil.cs` is the whole program. `test/komut-dene.ps1 -Komut 06-B0` sends one raw report and prints the reply. `test/onizleme.ps1` renders the icon and the panel to PNG.

## Contributing

Open an issue first: [bug report](../../issues/new?template=bug.yml) or [headset support request](../../issues/new?template=device.yml). Keep pull requests small. The repository language is English. Contributions are accepted under the project license. If ArctisPil saves you time, [sponsoring](https://github.com/sponsors/Teknesyum) keeps it going.

## License

AGPL-3.0-or-later. See [LICENSE](LICENSE).

<!-- signature -->
<div align="center">

<a href="https://github.com/sponsors/Teknesyum"><img src="assets/badge-sponsor.svg" alt="Support Teknesyum" height="38"></a>
&nbsp;
<a href="LICENSE"><img src="assets/badge-license.svg" alt="License AGPL-3.0" height="38"></a>

</div>
