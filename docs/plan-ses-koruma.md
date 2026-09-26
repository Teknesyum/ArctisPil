# Plan: Peak Protection

Status: plan only. Research: `rapor/ses-esitleme.md`, `rapor/max-ses-koruma.md` (gitignored, Turkish).

## Plan A: Windows Loudness Equalization Toggle

- Nova Pro Wireless output uses the Microsoft USB Audio class driver (`wdma_usb.inf`); its `FxProperties` list `WMALFXGFXDSP.dll`, so Loudness Equalization is available on this device (checked read-only on the dev machine).
- A tray/panel toggle writes the endpoint's `FxProperties` flag under `HKLM\...\MMDevices\Audio\Render\{id}`. HKLM needs admin: the exe relaunches itself once with `runas` only for the write, the tray stays unelevated.
- Open: whether `audiosrv` must restart for the change to apply (sources disagree); attack time is undocumented, so the first transient may still pass.
- Target endpoint: the physical headset, not the SteelSeries Sonar virtual devices.

## Plan B: Peak Meter Ducking

- `IAudioMeterInformation` polled every ~10 ms; above a threshold, lower the headset volume for a short hold, then restore.
- Reacts in ~20-50 ms: cuts the tail of a blast, not the first hit. Complements plan A.

## Steps

1. Read-only detect: show the toggle only when the endpoint has the Microsoft enhancement APO.
2. Elevated write helper (`--leq on|off`), then verify by reading back.
3. Measure: play a test impulse, record loopback with and without the toggle.
4. Plan B only if step 3 shows the first hit passes.
