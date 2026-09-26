# Plan: 1% Battery Estimator

Status: plan only.

## Problem

The Nova Pro reports battery as a level 0-8, so the tray moves in 12.5% jumps. HeadsetControl reads the same register (`level: 12` = level 1/8), so the step is a device limit, not ours. Many Bluetooth headsets report 10% steps.

## Model

No calibration run: the app learns from normal use and gets closer with every band the user drains.

- **Two learned values, updated together.**
  - `m`: minutes one percent lasts, averaged over the whole battery (assumes a roughly even drain).
  - `d[i]`: 100 cells, one per percent, the local shape. Starts as `d[i] = m`.
- **Bands.** A device level covers a band of cells (Nova: levels at 100, 87, 75, 62, 50, 37, 25, 12, 0). Only level changes are measurements.
- **Display between steps.** On entering a band, the estimate starts at its top and drops 1% each time the current cell's `d[i]` of use time runs out.
- **Wait at the edge.** The estimate never passes the next device level: in the 25 → 12 band it stops at 13 and waits for the device to report 12. An early report snaps it down.
- **When the band ends**, the measured band time updates both values (smoothing `a = 0.3`):
  - the band's cells are scaled toward the measured time, so the next pass through 25 → 12 ticks at the right pace;
  - `m` moves toward band time / band width, so bands not yet measured get a better guess too.
  A band longer than expected (a long wait at 13) raises both; a short one lowers both.
- **Censored bands are not learned.** The first band after app start, a battery swap or charging is partial; it only sets the anchor.
- **Remaining time** = sum of `d` over the cells left, scaled by the current use rate.

## Active Use

Drain differs between listening and idle. The log counts seconds with sound on the output each minute (`sesli_sn`).

- One use minute = `sesli_sn / 60` active plus the rest idle; idle weight `w` starts at 1 and is learned from bands with different active shares (least squares over the last bands).
- The countdown advances by use minutes, not wall minutes: during silence it slows, headset off pauses it.
- Phase 2: one multiplier per ANC mode, learned the same way.

## Events

| Event | Action |
|---|---|
| Level drops by one | learn band (unless censored), anchor at band top |
| Level rises, not charging | battery swap (Nova hot-swap): new cycle, censored |
| Charging (`0x02`) | stop drain; separate 100-cell charge curve, same method |
| Headset off | pause the clock |
| App restart, same level | resume from the saved estimate |

## Storage

Log: `%LOCALAPPDATA%\HeadsetBatteryTray\pil-<device>.csv`, one row per minute plus one per change: time, type (`d` minute / `s` change), raw level, state (`acik`/`sarj`/`kapali`/`yok`), `sesli_sn`, ANC mode, headset volume, Windows volume. Rotates to `.1.csv` at 8 MB.

Learned table: `pil-<device>.txt`: `m`, `w`, 100 drain values, 100 charge values, mode multipliers, last estimate with timestamp. The device key is VID:PID or the Bluetooth name. One profile per device model; the Nova's two batteries share it.

## UI

Tray shows the estimated percent. Tooltip: `~57% (device 50-62%), ~11 h 20 min left`. The low-battery warning uses the estimate. A menu toggle switches between the estimate and the raw device steps.

## Steps

1. Log only: record level changes with time, mode and volume; no UI change. **Done in v0.3.1.**
2. Estimator class with the cell table, fed from the log; test by replaying recorded cycles.
3. Tray and tooltip use the estimate; toggle in the menu.
4. Charge curve and ANC multipliers.
5. Generic band width for HeadsetControl and Bluetooth devices (inferred from the values seen).
