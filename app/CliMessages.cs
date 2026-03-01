namespace GpsdoCli
{
    internal static class CliMessages
    {
        public const string InvalidBooleanValue = "Invalid boolean value: '{0}'. Expected: true, false, 1, or 0";

        public const string InvalidSweepRange = "Invalid sweep range for OUT{0}: {1}-{2} (range: 1-{3} Hz)";
        public const string Sweeping = "Sweeping OUT{0}: {1} Hz -> {2} Hz, step: {3} Hz, delay: {4}ms";
        public const string SweepProgress = "\r  OUT{0}: {1}.{2:D06} Hz | PLL: {3} | GPS: {4}";
        public const string SweepComplete = "Sweep complete";
        public const string SweepFailed = "Sweep failed: {0}";
        public const string InvalidSweepFormat = "Invalid sweep format for OUT{0}: {1} (expected: start:step:stop[:delay])";

        public const string InvalidFrequency = "Invalid frequency for OUT{0}: {1} (range: 1-{2} Hz)";
        public const string SetFrequencyTemporary = "Set OUT{0} frequency temporarily: {1} Hz";
        public const string SetFrequencySaved = "Set OUT{0} frequency and stored to flash: {1} Hz";
        public const string SetFrequencyFailed = "Failed to set frequency: {0}";

        public const string OpenDeviceFailed = "Failed to open device: {0}";
        public const string InvalidStep1 = "Invalid step1 value: {0}";
        public const string InvalidStep2 = "Invalid step2 value: {0}";

        public const string OutputsEnabled = "Output(s) Enabled";
        public const string OutputsDisabled = "Output(s) Disabled";
        public const string SetOutputsFailed = "Failed to set outputs: {0}";

        public const string SetModePllFll = "Set {0} mode";
        public const string SetModeFailed = "Failed to set mode: {0}";

        public const string InvalidModeGlobal = "Invalid mode: {0} (expected: NORMAL, LOW_POWER)";
        public const string InvalidModeForGlobal = "Mode {0} is not valid for --mode (use --mode1 instead)";
        public const string SetAllOutputsMode = "Set all outputs to {0} mode";

        public const string InvalidModeOut1 = "Invalid mode for OUT1: {0} (expected: NORMAL, LOW_POWER, PPS, NMEA)";
        public const string SetOut1Mode = "Set OUT1 to {0} mode";
        public const string SetOut1ModeFailed = "Failed to set OUT1 mode: {0}";

        public const string InvalidModeOut2 = "Invalid mode for OUT2: {0} (expected: NORMAL, LOW_POWER)";
        public const string Out2NotSupported = "Device does not support output 2";
        public const string SetOut2Mode = "Set OUT2 to {0} mode";
        public const string SetOut2ModeFailed = "Failed to set OUT2 mode: {0}";

        public const string BlinkLeds = "Blink LED(s)";
        public const string BlinkLedsFailed = "Failed to blink LEDs: {0}";

        public const string DeviceInfo = "  Device Info";

        public const string DeviceHeader = "[ DEVICE ]";
        public const string StatusHeader = "[ STATUS ]";
        public const string Output1Header = "[ OUTPUT1 ]";
        public const string Output2Header = "[ OUTPUT2 ]";

        // Device Info
        public const string MfrLine = "  Mfr     : {0}";
        public const string ModelLine = "  Model   : {0}";
        public const string VersionLine = "  Version : {0}";
        public const string SerialLine = "  Serial  : {0}";

        // Device Status
        public const string AntennaLine = "  Antenna  : {0}";
        public const string GpsLockLine = "  GPS Lock : {0}";
        public const string PllLockLine = "  PLL Lock : {0}";
        public const string ModeLine = "  Mode     : {0}";

        // Output Status
        public const string OutModeLine = "  Out Mode  : {0}";
        public const string OutStateLine = "  Out State : {0}";
        public const string LedStateLine = "  LED State : {0}";
        public const string FrequencyLine = "  Frequency : {0}.{1:D06} Hz";

        public const string FailedToGetStatus = "Failed to get status: {0}";
        public const string NoChangesMade = "No changes made";

        public const string EnumerateHeader = "[ DEVICES ]";
        public const string EnumerateDevice = "  {0} (Serial: {1})";
        public const string EnumerateNoDevices = "No devices found";
        public const string EnumerateFailed = "Failed to enumerate devices: {0}";

        public const string StatusLocked = "LOCKED";
        public const string StatusUnlocked = "UNLOCKED";
        public const string AntennaOk = "OK";
        public const string AntennaShort = "Short Circuit";
        public const string ModeFll = "FLL";
        public const string ModePll = "PLL";
        public const string Yes = "Yes";
        public const string No = "No";
        public const string On = "On";
        public const string Off = "Off";
    }
}
