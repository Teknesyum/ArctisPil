# Plan: Multi-Brand Headsets

Status: proposed, not started.

## Layers

1. **Direct HID** (current) — full control for devices we map ourselves: Arctis Nova Pro Wireless first.
2. **HeadsetControl backend** — run `headsetcontrol -o json` as a separate process (~70 models: Logitech, SteelSeries, Corsair, HyperX, Razer, EPOS…). Battery, sidetone, chatmix, lights, inactive time where the device supports it. Shipped next to the exe as its own GPL-3.0 program; ArctisPil does not link it.
3. **Windows Bluetooth battery** — `DEVPKEY_Bluetooth_Battery` and GATT Battery Service (0x180F) for any Bluetooth headset. Battery only.

## Code

- `IKulaklik` interface: `Pil`, `Ses`, `Anc`, `Sidetone`, `Mikrofon`, each nullable; the panel hides rows the device does not support.
- One class per layer; the first layer that finds a device wins.

## Open

- Name: "ArctisPil" is brand-specific; a rename (repo and exe) is the owner's call.
- HeadsetControl download and update path.
