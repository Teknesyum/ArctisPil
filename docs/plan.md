# Plan: Multi-Brand Headsets (v0.3.0)

Status: in progress.

## Decisions

- HeadsetControl is bundled: its Windows exe is embedded as a resource, extracted to `%LOCALAPPDATA%` on first run and called as a separate process. Its license text and source link ship next to it.
- We do not write drivers for models outside HeadsetControl. Requests go to HeadsetControl upstream; our issue template points there. The popular-50 list (`docs/devices.csv`) documents coverage.
- The app is renamed after the SEO research (`rapor/isim-seo.md`, gitignored).
- Version is shown in the tray menu and the exe file properties.

## Layers

1. **Direct HID** — Arctis Nova Pro Wireless (full control, current code).
2. **HeadsetControl** — `headsetcontrol -o json`, polled; battery, sidetone, chatmix, lights, inactive time, EQ preset where the device supports it.
3. **Windows Bluetooth battery** — `DEVPKEY_Bluetooth_Battery` via SetupAPI for any paired Bluetooth headset. Battery only.

The first layer that finds a device wins. The panel hides rows the device does not support.

## Code

- `IKulaklik`: `Ad`, `Pil`, `Sarj`, nullable capabilities; one class per layer.
- `SesPaneli` rows get a `Destek` flag; `Yerlestir` already skips hidden rows.

## Later: Peak Protection

Plan only, see `docs/plan-ses-koruma.md`.
