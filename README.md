# GW2 RTTF Timer

A small borderless, always-on-top countdown overlay for the Guild Wars 2
festival activity "Run to the Finish" (RTTF). Press a hotkey to start a
10-minute countdown; the overlay shows the remaining time near the lower-right
corner of the Guild Wars 2 window when available.

## Features / controls

- F8 — start or reset the 10:00 countdown
- F11 — hide or show the overlay
- Draggable by clicking the border
- Border flashes white during the final minute
- Shows READY and plays a system sound when the countdown reaches zero
- Auto-hides 10 seconds after READY
- Single instance
- X hides the overlay; Quit closes the application

## Requirements

- Windows (x64)
- .NET 10 SDK (project targets `net10.0-windows`, WPF)

## Build

```text
dotnet build GW2RTTFTimer.slnx -c Release
```

The application is strictly a manual, reference-only timer. It does not read
game memory, inject input, call external APIs, or automate gameplay. It polls
the local F8/F11 hotkeys and looks for the Guild Wars 2 window title only to
place the overlay near that window; it does not inspect or interact with the
game process.

## Disclaimer

This is an unofficial fan-made utility for Guild Wars 2. It is not affiliated
with, endorsed by, or associated with ArenaNet, LLC or NCSOFT.

## License

Licensed under the MIT License. See [LICENSE](LICENSE).