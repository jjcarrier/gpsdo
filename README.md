# GPSDO CLI+TUI Tool for lbe-142x

A portable .NET Core CLI tool for controlling the LBE-142x GPS Disciplined Oscillator
(GPSDO) via USB HID.

> [!NOTE]
> At this time only the LBE-1424 GPSDO has been tested for compatibility.
> Please do not hesitate to report issues seen when using other GPSDO versions!

## Features

- [X] USB HID communication using HidSharp
- [X] USB HID communication debug messages to improve future development efforts
- [X] CLI for monitoring and control
- [ ] TUI (text-user-interface) interactive mode (using Spectre.Console)
- [X] Frequency sweep mode
- [X] Temporary frequency settings
- [X] Supports a JSON output mode for tooling integration
- [X] Supports multi-device via `--serial` and `--enumerate` parameters.

## Build

```shell
dotnet build --configuration Debug
```

## Usage

From the build directory (i.e. `.\app\bin\Debug\net10.0`).

Read the device status:

```shell
./gpsdo --status
```

Turn the output(s) on:

```shell
./gpsdo --on
```

Set the output to 10.000000000001 MHz

```shell
./gpsdo --f1 10000000.000001
```

## Tab Completion

### Tab-Completions

A tab-completer is available in the `completion` folder, offering an improved
user-experience. This module leverages [PS-HelpParser](https://github.com/jjcarrier/PS-HelpParser)
to automatically parse the help documentation and provide tab-completion results
to the user.

The completion file is provided in:

```pwsh
./completion/GpsdoCompletion.psm1
```

With the `HelpParser` module installed via
[PSGallery](https://www.powershellgallery.com/packages/HelpParser) import both
the parser and the completion module via:

```pwsh
Import-Module HelpParser
Import-Module <full_path_to_file>/GpsdoCompletion.psm1
```

Add the above to your `$PROFILE` to make the changes persist between sessions.

A bash completion script is also available in:

```bash
./completion/GpsdoCompletion.sh
```

Source it in your shell profile (for example `~/.bashrc`):

```bash
source /path/to/GpsdoCompletion.sh
```

## Requirements

- .NET 10.0 SDK
- LBE-142x GPSDO hardware (currently tested for LBE-1424)

## License

Copyright © Jon Carrier

This project is licensed under the MIT license. For more details please refer to
[LICENSE](./LICENSE). This software depends on the following third party
components:

- Spectre.Console (https://github.com/spectreconsole/spectre.console/blob/main/LICENSE.md)
- HidSharp (https://github.com/IntergatedCircuits/HidSharp/blob/master/License.txt)
