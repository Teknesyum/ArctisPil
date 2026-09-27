<!-- lang -->

[<img src="assets/badge-lang.svg" alt="English selected, switch to Türkçe" width="124" height="44">](README.tr.md)

# HeadsetBatteryTray

Headset battery in the Windows tray, for wired, wireless and Bluetooth headsets.

| Measure | Value |
|---|---|
| Popular headsets surveyed ([list](docs/devices.csv)) | 50 |
| Battery via bundled HeadsetControl | 29 of 50 |
| Full panel (volume, ANC, mic) over direct HID | Arctis Nova Pro Wireless |
| Dependencies | 0 (.NET Framework 4, ships with Windows) |

## What It Is

HeadsetBatteryTray is a single Windows tray program. The tray icon shows the headset battery as a number, colored by level, white while charging, with a warning at 25%. A left click opens a panel; starting the program a second time also opens it.

It reads the battery three ways, in order: direct HID for the SteelSeries Arctis Nova Pro Wireless, the bundled [HeadsetControl](https://github.com/Sapd/HeadsetControl) for about 40 wired and dongle headsets (SteelSeries, Logitech, Corsair, HyperX, Razer, Roccat, Audeze and more), and the Windows Bluetooth battery value for Bluetooth headsets. No vendor suite needs to run.

## Arctis Nova Pro Wireless Panel

The Nova Pro gets the full panel: battery, headset volume, Windows volume, noise cancelling, transparency (only in transparency mode) and microphone level.

- **Volume transfer (20-80).** The headset stays between 20% and 80%. Turn the dial past 80% and the extra points go to Windows; below 20% the difference comes off Windows. On the way back Windows moves first, toward 50%, then the headset follows.
- **Battery to 1%.** The Nova reports battery in 12.5% steps. The tray counts down in 1% steps between them, paced by how long each percent lasted on your earlier discharges. It learns while you use the headset: no calibration run, and quiet time counts less than listening. It never goes past the next device step: between 25 and 12 it waits at 13. The tooltip shows the device range and the time left. Turn it off with "Battery estimate (1% steps)" in the menu.
- **Round numbers.** Both volume bars snap to 5% steps.
- **Saved to the device.** Changes are written to the base station 0.8 s after the last change, so they survive a power cycle.

Other headsets get the battery panel only. With no headset found, the panel says so and offers **Search again**.

The panel works from the keyboard: Tab or the arrow keys move between rows, Left/Right change a value or a mode, Home/End jump to the ends, Enter presses the button, Esc closes. It follows the Windows display scale and the Windows animation setting.

## What It Does Not Do

- No EQ editing, no Sonar, no ChatMix, no firmware update.
- No drivers of its own for other brands. A headset HeadsetControl does not know is best [requested upstream](https://github.com/Sapd/HeadsetControl/issues); that helps every tool built on it. You can also [tell us](../../issues/new?template=device.yml).
- It never sends the Nova factory reset command (`06 FD`).
- Windows only.

## Install

Download `HeadsetBatteryTray.exe` from the [latest release](../../releases/latest) and run it. It adds itself to startup; untick "Windows ile başlat" in the right-click menu to stop that. The version is shown at the top of the right-click menu and in the exe's file properties.

## How It Works

The Nova base station exposes a vendor HID interface (interface 4). Commands go to the `0xFFC0` collection as 64-byte reports starting with `06`; events arrive on `0xFF00` starting with `07`.

| Command | Meaning |
|---|---|
| `06 B0` | status: battery `[6]`, headset state `[15]`, transparency `[8]`, ANC mode `[10]` |
| `06 20` | audio status: volume `[3]`, microphone `[17]` |
| `06 25 v` | headset volume, 0 = loudest, 56 = silent |
| `06 BD m` | 0 off, 1 transparency, 2 ANC |
| `06 B9 l` | transparency level 1-10 |
| `06 37 l` | microphone level 1-10 |
| `06 09` | save settings to the device |

Without a Nova, it checks once a minute: `headsetcontrol -b -o json` first, then the Bluetooth battery property that Windows keeps for Hands-Free devices.

## Program Shows What It Does

![The panel: battery, headset volume, Windows volume, noise cancelling, transparency and microphone rows.](assets/panel.png)

## Development

```powershell
powershell -ExecutionPolicy Bypass -File build.ps1
```

`src/HeadsetBatteryTray.cs` is the whole program. `build.ps1` embeds `vendor/headsetcontrol.exe` when present. `test/komut-dene.ps1 -Komut 06-B0` sends one raw report; `test/onizleme.ps1` renders the icon, menu and every panel state at 100/125/150% to PNG; `test/panel-test.ps1` checks layout, keyboard and mouse headlessly; `test/kontrast.ps1` measures every color pair; `test/pencere.ps1` captures the running panel; `test/hc-coz.ps1` and `test/bt-pil.ps1` check the HeadsetControl and Bluetooth readers.

## Third-Party Software

Release builds bundle [HeadsetControl](https://github.com/Sapd/HeadsetControl) 4.1.0 by Denis Arnst and contributors, unmodified, licensed GPL-3.0. It is run as a separate program; its source is at the link above. The panel font is [Atkinson Hyperlegible Next](https://github.com/googlefonts/atkinson-hyperlegible-next), SIL Open Font License 1.1, embedded in the program. See [docs/licenses.md](docs/licenses.md).

## Contributing

Open an issue first: [bug report](../../issues/new?template=bug.yml) or [headset support request](../../issues/new?template=device.yml). Keep pull requests small. The repository language is English. Contributions are accepted under the project license. If HeadsetBatteryTray saves you time, [sponsoring](https://github.com/sponsors/Teknesyum) keeps it going.

## License

AGPL-3.0-or-later. See [LICENSE](LICENSE).

<!-- signature -->
<div align="center">

<a href="https://github.com/sponsors/Teknesyum"><img src="assets/badge-sponsor.svg" alt="Support Teknesyum" height="38"></a>
&nbsp;
<a href="LICENSE"><img src="assets/badge-license.svg" alt="License AGPL-3.0" height="38"></a>

</div>
