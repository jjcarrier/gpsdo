using System;
using System.Globalization;
using System.Diagnostics;
using System.Text.Json;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Parsing;
using libGpsdo;

namespace GpsdoCli
{
    internal partial class Program
    {
        private static bool? ParseBooleanArgument(ArgumentResult result)
        {
            if (result.Tokens.Count == 0)
                return null;

            var value = result.Tokens[0].Value;

            return value.ToLowerInvariant() switch
            {
                "true" or "1" => true,
                "false" or "0" => false,
                _ => throw new ArgumentException(string.Format(
                    CultureInfo.InvariantCulture,
                    CliMessages.InvalidBooleanValue,
                    value))
            };
        }

        private static int Main(string[] args)
        {
            RootCommand rootCommand = new("LBE-142x GPSDO Command Line Tool");

            Option<bool> enumerateOption = new("--enumerate", "When set, the tool will report all GPSDO connected devices supported by this tool.");
            Option<string> serialOption = new(["--serial" ,"--sn"], "When set, the specified serial-number will be used to ensure the correct device is accessed.");
            Option<bool> blinkOption = new("--blink", "Flicker output LED(s) for 3 seconds for visual identification.");
            Option<bool> statusOption = new("--status", "Display current device status.");
            Option<bool> enableOption = new("--on", "Turn the outputs ON");
            Option<bool> disableOption = new("--off", "Turn the outputs OFF");
            Option<string[]> f1Option = new(["--freq1", "--f1"], "Set frequency (Hz) for OUT1 (ex. 10000000 or sweep: start:step:stop[:delay]). Optionally add 'save' to store to flash (only applicable during non-sweep mode).")
            {
                Arity = ArgumentArity.ZeroOrMore
            };
            Option<string[]> f2Option = new(["--freq2", "--f2"], "Set frequency (Hz) for OUT2 (ex. 10000000 or sweep: start:step:stop[:delay]). Optionally add 'save' to store to flash (only applicable during non-sweep mode).")
            {
                Arity = ArgumentArity.ZeroOrMore
            };
            Option<string> step1Option = new(["--step1", "--s1"], "Increment OUT1 frequency by specified value (Hz)");
            Option<string> step2Option = new(["--step2", "--s2"], "Increment OUT2 frequency by specified value (Hz)");
            Option<string?> modeOption = new(["--mode", "--m"], "Set output mode for all outputs (NORMAL, LOW_POWER)");
            Option<string?> mode1Option = new(["--mode1", "--m1"], "Set output mode for OUT1 (NORMAL, LOW_POWER, PPS, NMEA)");
            Option<string?> mode2Option = new(["--mode2", "--m2"], "Set output mode for OUT2 (NORMAL, LOW_POWER)");
            Option<bool?> pllOption = new("--pll", ParseBooleanArgument, false, "Set PLL(1) or FLL(0) mode") { ArgumentHelpName = "1|0" };
            Option<bool> jsonOption = new("--json", "Output results in JSON format.");
            Option<bool> debugOption = new("--debug", "Displays raw HID reports to assist in debug/development");

            rootCommand.AddOption(enumerateOption);
            rootCommand.AddOption(serialOption);
            rootCommand.AddOption(blinkOption);
            rootCommand.AddOption(statusOption);
            rootCommand.AddOption(enableOption);
            rootCommand.AddOption(disableOption);
            rootCommand.AddOption(f1Option);
            rootCommand.AddOption(f2Option);
            rootCommand.AddOption(step1Option);
            rootCommand.AddOption(step2Option);
            rootCommand.AddOption(modeOption);
            rootCommand.AddOption(mode1Option);
            rootCommand.AddOption(mode2Option);
            rootCommand.AddOption(pllOption);
            rootCommand.AddOption(jsonOption);
            rootCommand.AddOption(debugOption);

            rootCommand.SetHandler((System.CommandLine.Invocation.InvocationContext ctx) =>
            {
                var f1Args = ctx.ParseResult.GetValueForOption(f1Option);
                var f2Args = ctx.ParseResult.GetValueForOption(f2Option);
                var enable = ctx.ParseResult.GetValueForOption(enableOption);
                var disable = ctx.ParseResult.GetValueForOption(disableOption);
                var pll = ctx.ParseResult.GetValueForOption(pllOption);
                var mode = ctx.ParseResult.GetValueForOption(modeOption);
                var mode1 = ctx.ParseResult.GetValueForOption(mode1Option);
                var mode2 = ctx.ParseResult.GetValueForOption(mode2Option);
                var blink = ctx.ParseResult.GetValueForOption(blinkOption);
                var status = ctx.ParseResult.GetValueForOption(statusOption);
                var debug = ctx.ParseResult.GetValueForOption(debugOption);
                var serial = ctx.ParseResult.GetValueForOption(serialOption);
                var enumerate = ctx.ParseResult.GetValueForOption(enumerateOption);
                var step1 = ctx.ParseResult.GetValueForOption(step1Option);
                var step2 = ctx.ParseResult.GetValueForOption(step2Option);
                var json = ctx.ParseResult.GetValueForOption(jsonOption);
                Run(f1Args, f2Args, enable, disable, pll, mode, mode1, mode2, blink, status, debug, serial, enumerate, step1, step2, json);
            });

            return rootCommand.Invoke(args);
        }

        public static (uint integerPart, uint fractionalPart, double decimalValue, bool parseOk) StringToQ32_32(string value)
        {
            // Parse input safely using invariant culture for consistency
            bool parseOk = double.TryParse(value, CultureInfo.InvariantCulture, out double decimalValue);

            if (!parseOk)
            {
                return (0, 0, 0, false);
            }

            // Split into integer and fractional components
            uint integerPart = (uint)Math.Truncate(decimalValue);

            // Extract fraction, multiply by 2^32, and convert to uint
            double fraction = decimalValue - Math.Truncate(decimalValue);
            uint fractionalPart = (uint)(fraction * 4294967296.0); // 2^32

            // Console.WriteLine($"Val: {integerPart} ({integerPart:X08}), Fract: {fractionalPart} ({fractionalPart:X08}), Full: {decimalValue}");
            return (integerPart, fractionalPart, decimalValue, parseOk);
        }

        public static GpsdoOutputMode? ParseOutputMode(string value)
        {
            return value.ToUpperInvariant() switch
            {
                "NORMAL" or "NORM" => GpsdoOutputMode.Normal,
                "LOW_POWER" or "LP" => GpsdoOutputMode.LowPower,
                "PPS" => GpsdoOutputMode.PPS,
                "NMEA" => GpsdoOutputMode.NMEA,
                _ => null
            };
        }

        public static void SetOutputMode(Gpsdo dev, GpsdoOutput channel, GpsdoOutputMode mode)
        {
            // When switching to Normal or LowPower, ensure PPS and NMEA are disabled first
            if (mode == GpsdoOutputMode.Normal || mode == GpsdoOutputMode.LowPower)
            {
                if (channel == GpsdoOutput.All || channel == GpsdoOutput.One)
                {
                    var ppsInstr = dev.InstructionSet[(int)GpsdoInstructionIndex.EnableOut1PPS];
                    if (ppsInstr.Supported && dev.Status.Output1.Mode == GpsdoOutputMode.PPS)
                        dev.SetPps(false);

                    var nmeaInstr = dev.InstructionSet[(int)GpsdoInstructionIndex.EnableOut1Nmea];
                    if (nmeaInstr.Supported && dev.Status.Output1.Mode == GpsdoOutputMode.NMEA)
                        dev.SetNmea(false);
                }
            }

            switch (mode)
            {
                case GpsdoOutputMode.Normal:
                    if (channel == GpsdoOutput.All)
                    {
                        dev.SetPowerLevel(GpsdoOutput.One, false);
                        if (dev.DualOutput) dev.SetPowerLevel(GpsdoOutput.Two, false);
                    }
                    else
                    {
                        dev.SetPowerLevel(channel, false);
                    }
                    break;
                case GpsdoOutputMode.LowPower:
                    if (channel == 0)
                    {
                        dev.SetPowerLevel(GpsdoOutput.One, true);
                        if (dev.DualOutput) dev.SetPowerLevel(GpsdoOutput.Two, true);
                    }
                    else
                    {
                        dev.SetPowerLevel(channel, true);
                    }
                    break;
                case GpsdoOutputMode.PPS:
                    var nmeaInstr = dev.InstructionSet[(int)GpsdoInstructionIndex.EnableOut1Nmea];
                    if (nmeaInstr.Supported && dev.Status.Output1.Mode == GpsdoOutputMode.NMEA)
                        dev.SetNmea(false);

                    dev.SetPps(true);
                    break;
                case GpsdoOutputMode.NMEA:
                    var ppsInstr = dev.InstructionSet[(int)GpsdoInstructionIndex.EnableOut1PPS];
                    if (ppsInstr.Supported && dev.Status.Output1.Mode == GpsdoOutputMode.PPS)
                        dev.SetPps(false);

                    dev.SetNmea(true);
                    break;
            }
        }

        public static bool TryParseSweep(string value, out double start, out double step, out double stop, out uint delayMilliseconds)
        {
            start = step = stop = 0;
            delayMilliseconds = 10; // default 10ms

            var parts = value.Split(':');
            if (parts.Length < 3 || parts.Length > 4)
                return false;

            if (!double.TryParse(parts[0], CultureInfo.InvariantCulture, out start))
                return false;
            if (!double.TryParse(parts[1], CultureInfo.InvariantCulture, out step))
                return false;
            if (!double.TryParse(parts[2], CultureInfo.InvariantCulture, out stop))
                return false;

            if (parts.Length == 4)
            {
                if (!uint.TryParse(parts[3], CultureInfo.InvariantCulture, out delayMilliseconds))
                    return false;
            }

            return step > 0 && delayMilliseconds >= 0;
        }

        public static bool SweepFrequency(Gpsdo dev, GpsdoOutput output, double start, double step, double stop, uint delayMilliseconds)
        {
            uint maxCh1Freq = 800000000U;
            uint maxCh2Freq = (dev.Model == GpsdoModel.LBE_1421) ? 160000000U : 1400000000U;
            uint maxFreq = (output == GpsdoOutput.One) ? maxCh1Freq : maxCh2Freq;

            if (start < 1 || start > maxFreq || stop < 1 || stop > maxFreq)
            {
                Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.InvalidSweepRange, output, start, stop, maxFreq));
                return false;
            }

            long delayTicks = (long)(delayMilliseconds * (Stopwatch.Frequency / 1000.0));
            bool ascending = start <= stop;

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.Sweeping, output, start, stop, step, delayMilliseconds));

            try
            {
                var sw = Stopwatch.StartNew();
                for (double current = start;
                     ascending ? current <= stop : current >= stop;
                     current += ascending ? step : -step)
                {
                    long t0 = sw.ElapsedTicks;
                    uint fInt = (uint)current;
                    uint fFract = (uint)((current - fInt) * 4294967296.0);
                    dev.SetFrequencyTemp(output, fInt, fFract);

                    if (delayTicks > 0)
                    {
                        long target = t0 + delayTicks;
                        while (sw.ElapsedTicks < target)
                        {
                            var sts = dev.GetStatus();
                            string pllStatus = sts.PllLocked ? CliMessages.StatusLocked : CliMessages.StatusUnlocked;
                            string gpsStatus = sts.GpsLocked ? CliMessages.StatusLocked : CliMessages.StatusUnlocked;
                            Console.Write(string.Format(CultureInfo.InvariantCulture, CliMessages.SweepProgress, output, fInt, fFract, pllStatus, gpsStatus));
                        }
                    }
                }

                Console.WriteLine();
                Console.WriteLine(CliMessages.SweepComplete);
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.SweepFailed, ex.Message));
                return false;
            }
        }

        public static bool SetFrequency(Gpsdo dev, GpsdoOutput output, string freq, bool temporary)
        {
            if (freq.Contains(':'))
            {
                if (!TryParseSweep(freq, out double start, out double step, out double stop, out uint delayMilliseconds))
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.InvalidSweepFormat, output, freq));
                    return false;
                }
                return SweepFrequency(dev, output, start, step, stop, delayMilliseconds);
            }

            uint maxCh1Freq = 800000000U;
            uint maxCh2Freq = (dev.Model == GpsdoModel.LBE_1421) ? 160000000U : 1400000000U;
            uint maxFreq = (output == GpsdoOutput.One) ? maxCh1Freq : maxCh2Freq;
            (uint fInt, uint fFract, double fDouble, bool parseOk) = StringToQ32_32(freq);
            if (!parseOk | fInt < 1 || fDouble > maxCh1Freq)
            {
                Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.InvalidFrequency, output, freq, maxFreq));
                return false;
            }
            else
            {
                try
                {
                    if (temporary)
                    {
                        dev.SetFrequencyTemp(output, fInt, fFract);
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.SetFrequencyTemporary, output, fDouble));
                    }
                    else
                    {
                        dev.SetFrequency(output, fInt, fFract);
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.SetFrequencySaved, output, fDouble));
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.SetFrequencyFailed, ex.Message));
                    return false;
                }
            }
        }

        private static void Run(
            string[]? f1Args, string[]? f2Args,
            bool enable, bool disable, bool? pll, string? mode,
            string? mode1, string? mode2, bool blink, bool status, bool debug,
            string? serial, bool enumerate, string? step1 = null, string? step2 = null, bool json = false)
        {
            if (enumerate)
            {
                try
                {
                    var devices = Enumerator.Enumerate(serialNumber: serial, debug: debug);
                    if (json)
                    {
                        var jsonArr = devices.Select(d => new
                        {
                            model = d.Model.ToString().Replace('_', '-'),
                            serial = d.SerialNumber
                        }).ToArray();
                        Console.WriteLine(JsonSerializer.Serialize(jsonArr, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    else
                    {
                        if (devices.Count == 0)
                        {
                            Console.WriteLine(CliMessages.EnumerateNoDevices);
                        }
                        else
                        {
                            Console.WriteLine(CliMessages.EnumerateHeader);
                            foreach (var d in devices)
                            {
                                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.EnumerateDevice, d.Model.ToString().Replace('_', '-'), d.SerialNumber));
                            }
                        }
                    }
                    foreach (var d in devices)
                    {
                        d.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.EnumerateFailed, ex.Message));
                }
                return;
            }
            Gpsdo? dev = null;
            try
            {
                dev = Gpsdo.Open(serial, debug);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.OpenDeviceFailed, ex.Message));
                Environment.Exit(1);
            }

            bool changed = false;

            // Handle --f1
            if (f1Args != null && f1Args.Length > 0)
            {
                var freq = f1Args[0];
                bool save = f1Args.Length > 1 && f1Args[1].Equals("save", StringComparison.OrdinalIgnoreCase);
                if (SetFrequency(dev, output: GpsdoOutput.One, freq: freq, temporary: !save))
                {
                    changed = true;
                }
            }

            // Handle --f2
            if (f2Args != null && f2Args.Length > 0)
            {
                var freq = f2Args[0];
                bool save = f2Args.Length > 1 && f2Args[1].Equals("save", StringComparison.OrdinalIgnoreCase);
                Console.WriteLine(save);
                if (SetFrequency(dev, output: GpsdoOutput.Two, freq: freq, temporary: !save))
                {
                    changed = true;
                }
            }

            // Handle --step1
            if (!string.IsNullOrWhiteSpace(step1))
            {
                if (double.TryParse(step1, CultureInfo.InvariantCulture, out double stepVal))
                {
                    double currentFreq = dev.Status.Output1.Frequency + dev.Status.Output1.FreqFractional / 4294967296.0;
                    double newFreq = currentFreq + stepVal;
                    if (SetFrequency(dev, output: GpsdoOutput.One, freq: newFreq.ToString(CultureInfo.InvariantCulture), temporary: true))
                        changed = true;
                }
                else
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.InvalidStep1, step1));
                }
            }

            // Handle --step2
            if (!string.IsNullOrWhiteSpace(step2))
            {
                if (double.TryParse(step2, CultureInfo.InvariantCulture, out double stepVal))
                {
                    double currentFreq = dev.Status.Output2.Frequency + dev.Status.Output2.FreqFractional / 4294967296.0;
                    double newFreq = currentFreq + stepVal;
                    if (SetFrequency(dev, output: GpsdoOutput.Two, freq: newFreq.ToString(CultureInfo.InvariantCulture), temporary: true))
                        changed = true;
                }
                else
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.InvalidStep2, step2));
                }
            }

            if (enable)
            {
                try
                {
                    dev.SetOutputEnable(GpsdoOutput.All, true);
                    Console.WriteLine(CliMessages.OutputsEnabled);
                    changed = true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.SetOutputsFailed, ex.Message));
                }
            }
            if (disable)
            {
                try
                {
                    dev.SetOutputEnable(GpsdoOutput.All, false);
                    Console.WriteLine(CliMessages.OutputsDisabled);
                    changed = true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.SetOutputsFailed, ex.Message));
                }
            }
            if (pll.HasValue)
            {
                try
                {
                    dev.SetPllMode(pll.Value);
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.SetModePllFll, pll.Value ? CliMessages.ModePll : CliMessages.ModeFll));
                    changed = true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.SetModeFailed, ex.Message));
                }
            }
            if (!string.IsNullOrWhiteSpace(mode))
            {
                var parsedMode = ParseOutputMode(mode);
                if (parsedMode == null)
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.InvalidModeGlobal, mode));
                }
                else if (parsedMode == GpsdoOutputMode.PPS || parsedMode == GpsdoOutputMode.NMEA)
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.InvalidModeForGlobal, parsedMode));
                }
                else
                {
                    try
                    {
                        SetOutputMode(dev, GpsdoOutput.All, parsedMode.Value);
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.SetAllOutputsMode, parsedMode));
                        changed = true;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.SetModeFailed, ex.Message));
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(mode1))
            {
                var parsedMode = ParseOutputMode(mode1);
                if (parsedMode == null)
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.InvalidModeOut1, mode1));
                }
                else
                {
                    try
                    {
                        SetOutputMode(dev, GpsdoOutput.One, parsedMode.Value);
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.SetOut1Mode, parsedMode));
                        changed = true;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.SetOut1ModeFailed, ex.Message));
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(mode2))
            {
                var parsedMode = ParseOutputMode(mode2);
                if (parsedMode == null || parsedMode == GpsdoOutputMode.PPS || parsedMode == GpsdoOutputMode.NMEA)
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.InvalidModeOut2, mode2));
                }
                else
                {
                    if (!dev.DualOutput)
                    {
                        Console.Error.WriteLine(CliMessages.Out2NotSupported);
                    }
                    else
                    {
                        try
                        {
                            SetOutputMode(dev, GpsdoOutput.Two, parsedMode.Value);
                            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.SetOut2Mode, parsedMode));
                            changed = true;
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.SetOut2ModeFailed, ex.Message));
                        }
                    }
                }
            }
            if (blink)
            {
                try
                {
                    dev.BlinkLeds();
                    Console.WriteLine(CliMessages.BlinkLeds);
                    changed = true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.BlinkLedsFailed, ex.Message));
                }
            }

            if (status)
            {
                try
                {
                    var statusObj = dev.GetStatus();
                    if (json)
                    {
                        var output1 = new
                        {
                            mode = statusObj.Output1.Mode.ToString(),
                            enabled = statusObj.Output1.Enabled,
                            led = statusObj.Output1.LedOn,
                            frequency = $"{statusObj.Output1.Frequency}.{statusObj.Output1.FreqFractional:D06}"
                        };
                        object? output2 = dev.DualOutput ? new
                        {
                            mode = statusObj.Output2.Mode.ToString(),
                            enabled = statusObj.Output2.Enabled,
                            led = statusObj.Output2.LedOn,
                            frequency = $"{statusObj.Output2.Frequency}.{statusObj.Output2.FreqFractional:D06}"
                        } : null;
                        var jsonObj = new
                        {
                            device = new
                            {
                                manufacturer = dev.Manufacturer,
                                model = dev.Model.ToString().Replace('_', '-'),
                                version = dev.ReleaseNumber,
                                serial = dev.SerialNumber,
                            },
                            status = new
                            {
                                antenna = statusObj.AntennaOk ? "OK" : "Short Circuit",
                                gpsLocked = statusObj.GpsLocked,
                                pllLocked = statusObj.PllLocked,
                                mode = statusObj.FllEnabled ? "FLL" : "PLL"
                            },
                            output1,
                            output2
                        };
                        Console.WriteLine(JsonSerializer.Serialize(jsonObj, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    else
                    {
                        Console.WriteLine(CliMessages.DeviceHeader);
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.MfrLine, dev.Manufacturer));
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.ModelLine, dev.Model.ToString().Replace('_', '-')));
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.VersionLine, dev.ReleaseNumber));
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.SerialLine, dev.SerialNumber));
                        Console.WriteLine();
                        Console.WriteLine(CliMessages.StatusHeader);
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.AntennaLine, statusObj.AntennaOk ? CliMessages.AntennaOk : CliMessages.AntennaShort));
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.GpsLockLine, statusObj.GpsLocked ? CliMessages.Yes : CliMessages.No));
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.PllLockLine, statusObj.PllLocked ? CliMessages.Yes : CliMessages.No));
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.ModeLine, statusObj.FllEnabled ? CliMessages.ModeFll : CliMessages.ModePll));
                        Console.WriteLine();
                        Console.WriteLine(CliMessages.Output1Header);
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.OutModeLine, statusObj.Output1.Mode));
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.OutStateLine, statusObj.Output1.Enabled ? CliMessages.On : CliMessages.Off));
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.LedStateLine, statusObj.Output1.LedOn ? CliMessages.On : CliMessages.Off));
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.FrequencyLine, statusObj.Output1.Frequency, statusObj.Output1.FreqFractional));
                        if (dev.DualOutput)
                        {
                            Console.WriteLine("");
                            Console.WriteLine(CliMessages.Output2Header);
                            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.OutModeLine, statusObj.Output2.Mode));
                            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.OutStateLine, statusObj.Output2.Enabled ? CliMessages.On : CliMessages.Off));
                            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.LedStateLine, statusObj.Output2.LedOn ? CliMessages.On : CliMessages.Off));
                            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.FrequencyLine, statusObj.Output2.Frequency, statusObj.Output2.FreqFractional));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, CliMessages.FailedToGetStatus, ex.Message));
                }
            }
            if (!changed && !status)
            {
                Console.WriteLine(CliMessages.NoChangesMade);
            }
            dev.Dispose();
        }
    }
}
