# Plan: 1% Battery Estimator

Status: plan only.

## Problem

The Nova Pro reports battery as a level 0-8, so the tray moves in 12.5% jumps. HeadsetControl reads the same register (`level: 12` = level 1/8), so the step is a device limit, not ours. Many Bluetooth headsets report 10% steps.

## Model

- **100 cells, one per percent.** Cell `i` stores `d[i]`: minutes of use that percent lasts. Start uniform: rated runtime / 100; the first full cycle replaces it.
- **Bands.** A device level covers a band of cells (Nova: 8 bands of 12.5). Only level changes are real measurements.
- **Learning.** When the device drops from band `k` to `k-1`, the time spent in band `k` is one observation. The cells of that band are scaled so their sum matches it, smoothed across cycles: `d_new = d_old + 0.3 · (d_obs - d_old)`. The shape inside a band follows neighbouring bands until finer data exists.
- **Censored bands are not learned.** The first band after app start, a battery swap or charging is partial; it only sets the anchor.
- **Display between steps.** From the moment a band is entered, elapsed use time walks down the cells: each time a cell's `d[i]` runs out, the tray drops 1%. The estimate is clamped inside the current band: it never passes the lower edge before the device confirms, and snaps to the edge when the device steps early.
- **Next cycle.** Remaining time = sum of `d` over the cells left. Each full cycle refines `d`, so the next cycle's 1% ticks and remaining time get closer to reality.

## Load

Drain depends on ANC mode and volume. Phase 2: log mode and volume per minute, learn one multiplier per ANC mode (off / transparency / ANC) from band times, and scale `d[i]` by the current mode while counting down.

## Events

| Event | Action |
|---|---|
| Level drops by one | learn band (unless censored), anchor at band top |
| Level rises, not charging | battery swap (Nova hot-swap): new cycle, censored |
| Charging (`0x02`) | stop drain; separate 100-cell charge curve, same method |
| Headset off | pause the clock |
| App restart, same level | resume from the saved estimate |

## Storage

`%LOCALAPPDATA%\HeadsetBatteryTray\pil-<device>.txt`: 100 drain values, 100 charge values, mode multipliers, last estimate with timestamp. The device key is VID:PID or the Bluetooth name. One profile per device model; the Nova's two batteries share it.

## UI

Tray shows the estimated percent. Tooltip: `~57% (device 50-62%), ~11 h 20 min left`. The low-battery warning uses the estimate. A menu toggle switches between the estimate and the raw device steps.

## Steps

1. Log only: record level changes with time, mode and volume; no UI change.
2. Estimator class with the cell table, fed from the log; test by replaying recorded cycles.
3. Tray and tooltip use the estimate; toggle in the menu.
4. Charge curve and ANC multipliers.
5. Generic band width for HeadsetControl and Bluetooth devices (inferred from the values seen).
