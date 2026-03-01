using HidSharp;

namespace libGpsdo
{
    public class Gpsdo : IDisposable
    {
        const int REPORT_SIZE = 64;

        // Feature Report IDs
        const byte LBE_STATUS_FEATURE = 0x4B;

        // Status bits
        const byte LBE_1425_STATUS_MASK_GPS_LOCKED = 0x01;
        const byte LBE_1425_STATUS_MASK_PLL_LOCKED = 0x02;
        const byte LBE_1425_STATUS_MASK_ANT_OK = 0x04;
        const byte LBE_1425_STATUS_MASK_OUT1_LED = 0x08;
        const byte LBE_1425_STATUS_MASK_OUT2_LED = 0x10;
        const byte LBE_1425_STATUS_MASK_OUT1_EN = 0x20;
        const byte LBE_1425_STATUS_MASK_OUT2_EN = 0x40;
        const byte LBE_1425_STATUS_MASK_PPS_EN = 0x80;

        public static readonly UsbDeviceId[] Devices =
        [
            new() { VID = 0x1DD2, PID = 0x2443, Name = "LBE-1420" },
            new() { VID = 0x1DD2, PID = 0x2444, Name = "LBE-1421" },
            new() { VID = 0x1DD2, PID = 0x2269, Name = "LBE-1425" }
        ];

        // Device Instruction-Sets
        public static readonly GpsdoInstruction[] Lbe1425Instructions =
        [
            new() {Name = "EnableOutput", Instruction = 0x01, Supported = true},
            new() {Name = "Blink", Instruction = 0x02, Supported = true},
            new() {Name = "SetOut1FreqTemp", Instruction = 0x05, Supported = true},
            new() {Name = "SetOut1Freq", Instruction = 0x06, Supported = true},
            new() {Name = "SetOut2FreqTemp", Instruction = 0x09, Supported = true},
            new() {Name = "SetOut2Freq", Instruction = 0x0A, Supported = true},
            new() {Name = "SetPLL", Instruction = 0x0B, Supported = true},
            new() {Name = "EnableOut1PPS", Instruction = 0x0C, Supported = true},
            new() {Name = "SetOut1PowerLevel", Instruction = 0x0D, Supported = true},
            new() {Name = "SetOut2PowerLevel", Instruction = 0x0E, Supported = true},
            new() {Name = "EnableOut1NMEA", Instruction = 0x0F, Supported = true}
        ];

        public static readonly GpsdoInstruction[] Lbe1421Instructions =
        [
            new() {Name = "EnableOutput", Instruction = 0x01, Supported = true},
            new() {Name = "Blink", Instruction = 0x02, Supported = true},
            new() {Name = "SetOut1FreqTemp", Instruction = 0x05, Supported = true},
            new() {Name = "SetOut1Freq", Instruction = 0x06, Supported = true},
            new() {Name = "SetOut2FreqTemp", Instruction = 0x09, Supported = true},
            new() {Name = "SetOut2Freq", Instruction = 0x0A, Supported = true},
            new() {Name = "SetPLL", Instruction = 0x0B, Supported = true},
            new() {Name = "EnableOut1PPS", Instruction = 0x0C, Supported = true},
            new() {Name = "SetOut1PowerLevel", Instruction = 0x0D, Supported = true},
            new() {Name = "SetOut2PowerLevel", Instruction = 0x0E, Supported = true},
            new() {Name = "EnableOut1NMEA", Instruction = 0x0F, Supported = false}
        ];

        public static readonly GpsdoInstruction[] Lbe1420Instructions =
        [
            new() {Name = "EnableOutput", Instruction = 0x01, Supported = true},
            new() {Name = "Blink", Instruction = 0x02, Supported = true},
            new() {Name = "SetOut1FreqTemp", Instruction = 0x03, Supported = true},
            new() {Name = "SetOut1Freq", Instruction = 0x04, Supported = true},
            new() {Name = "SetOut2FreqTemp", Instruction = 0x00, Supported = false},
            new() {Name = "SetOut2Freq", Instruction = 0x00, Supported = false},
            new() {Name = "SetPLL", Instruction = 0x0B, Supported = true},
            new() {Name = "EnableOut1PPS", Instruction = 0x00, Supported = false},
            new() {Name = "SetOut1PowerLevel", Instruction = 0x07, Supported = true},
            new() {Name = "SetOut2PowerLevel", Instruction = 0x00, Supported = false},
            new() {Name = "EnableOut1NMEA", Instruction = 0x00, Supported = false}
        ];

        #pragma warning disable CS0414
        HidDevice? device;
        #pragma warning restore CS0414
        HidStream? stream;
        public bool DualOutput { get; private set; }
        public GpsdoModel Model { get; private set; }
        public int ProductId { get; private set; }
        public string? SerialNumber { get; private set; }

        public Version ReleaseNumber { get; set; } = new Version();
        public string Manufacturer { get; set; } = "";
        public string ProductName { get; set; } = "";
        public GpsdoStatus Status { get; set; } = new();

        // Debug mode: if true, print HEX data for HID reports
        public bool Debug { get; set; } = false;

        // New: Instruction set and frequency format metadata
        public GpsdoInstruction[] InstructionSet { get; private set; } = Lbe1425Instructions;
        public bool SupportsQ32_32 { get; private set; } = true;

        public static Gpsdo Open(string? serial, bool debug = false)
        {
            var list = DeviceList.Local;

            HidDevice enum_device;
            var candidates = Devices.SelectMany(d => list.GetHidDevices(d.VID, d.PID));
            if (candidates.Count() > 1 && string.IsNullOrWhiteSpace(serial))
            {
                throw new Exception($"Multiple devices detected, use --enumerate and --serial to select the desired device");
            }

            if (!string.IsNullOrWhiteSpace(serial))
            {
                enum_device = candidates.FirstOrDefault(d => d.GetSerialNumber() == serial)
                    ?? throw new Exception($"Device with serial '{serial}' not found");
            }
            else
            {
                enum_device = candidates.First();
            }

            var dev = new Gpsdo
            {
                device = enum_device,
                ProductId = enum_device.ProductID,
                stream = enum_device.Open(),
                SerialNumber = enum_device.GetSerialNumber() ?? "",
                ReleaseNumber = enum_device.ReleaseNumber,
                ProductName = enum_device.GetProductName(),
                Manufacturer = enum_device.GetManufacturer(),
                Debug = debug
            };

            // Determine model and assign instruction set and metadata
            if (enum_device.ProductID == Devices[(int)GpsdoModel.LBE_1425].PID)
            {
                dev.Model = GpsdoModel.LBE_1425;
                dev.InstructionSet = Lbe1425Instructions;
                dev.DualOutput = true;
                dev.SupportsQ32_32 = true;
            }
            else if (enum_device.ProductID == Devices[(int)GpsdoModel.LBE_1421].PID)
            {
                dev.Model = GpsdoModel.LBE_1421;
                dev.InstructionSet = Lbe1421Instructions;
                dev.DualOutput = true;
                dev.SupportsQ32_32 = true;
            }
            else // LBE_1420
            {
                dev.Model = GpsdoModel.LBE_1420;
                dev.InstructionSet = Lbe1420Instructions;
                dev.DualOutput = false;
                dev.SupportsQ32_32 = false;
            }

            dev.GetStatus();
            return dev;
        }

        public void Dispose()
        {
            stream?.Dispose();
        }

        private void SetFeatureReport(byte[] report)
        {
            if (stream == null) throw new Exception("Device not open");
            if (Debug)
            {
                Console.WriteLine($"[ DEBUG ] SetFeatureReport: {BitConverter.ToString(report).Replace("-", " ")}");
            }
            stream.SetFeature(report);
        }

        private byte[] GetFeatureReport(byte reportId)
        {
            var buf = new byte[REPORT_SIZE];
            buf[0] = reportId;
            if (stream == null) throw new Exception("Device not open");
            stream.GetFeature(buf);
            if (Debug)
            {
                Console.WriteLine($"[ DEBUG ] GetFeatureReport: {BitConverter.ToString(buf).Replace("-", " ")}");
            }
            return buf;
        }

        public (uint integerPart, uint fractionalPart) GetQ32Dot32Parts(byte[] data, uint offset)
        {
            // Split into integer and fractional components
            uint integerPart = (uint)(data[offset + 4] | (data[offset + 5] << 8) | (data[offset + 6] << 16) | (data[offset + 7] << 24));
            uint fractionalPart = 0;
            if (SupportsQ32_32)
            {
                // TODO: improve this logic
                uint fractionalPartRaw = (uint)(data[offset + 0] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
                double fractional = (ulong)fractionalPartRaw * 1000000 / 4294967296.0;
                fractionalPart = (uint)Math.Round(fractional);
                //Console.WriteLine($"Raw: {fractionalPartRaw:X08}, Val: {fractionalPart}");
            }

            return (integerPart, fractionalPart);
        }

        public GpsdoStatus GetStatus()
        {
            var buf = GetFeatureReport(LBE_STATUS_FEATURE);
            (uint f1IntegerPart, uint f1FractionalPart) = GetQ32Dot32Parts(buf, offset: 3);
            (uint f2IntegerPart, uint f2FractionalPart) = GetQ32Dot32Parts(buf, offset: 11);

            if (Model == GpsdoModel.LBE_1425)
            {
                GpsdoOutputMode out1Mode = GpsdoOutputMode.Normal;
                GpsdoOutputMode out2Mode = GpsdoOutputMode.Normal;
                bool out1PpsMode = (buf[2] & LBE_1425_STATUS_MASK_PPS_EN) == LBE_1425_STATUS_MASK_PPS_EN;
                bool out1NmeaMode = buf[25] != 0;
                bool out1LowPowerMode = buf[20] != 0;
                bool out2LowPowerMode = buf[21] != 0;
                if (out1NmeaMode)
                {
                    out1Mode = GpsdoOutputMode.NMEA;
                }
                else if (out1PpsMode)
                {
                    out1Mode = GpsdoOutputMode.PPS;
                }
                else if (out1LowPowerMode)
                {
                    out1Mode = GpsdoOutputMode.LowPower;
                }

                if (out2LowPowerMode)
                {
                    out2Mode = GpsdoOutputMode.LowPower;
                }

                Status = new GpsdoStatus
                {
                    RawStatus = buf[2],
                    Output1 = new GpsdoOutputStatus
                    {
                        Enabled = (buf[2] & LBE_1425_STATUS_MASK_OUT1_EN) == LBE_1425_STATUS_MASK_OUT1_EN,
                        Mode = out1Mode,
                        LedOn = (buf[2] & LBE_1425_STATUS_MASK_OUT1_LED) == LBE_1425_STATUS_MASK_OUT1_LED,
                        Frequency = f1IntegerPart,
                        FreqFractional = f1FractionalPart
                    },
                    Output2 = new GpsdoOutputStatus
                    {
                        Enabled = (buf[2] & LBE_1425_STATUS_MASK_OUT2_EN) == LBE_1425_STATUS_MASK_OUT2_EN,
                        Mode = out2Mode,
                        LedOn = (buf[2] & LBE_1425_STATUS_MASK_OUT2_LED) == LBE_1425_STATUS_MASK_OUT2_LED,
                        Frequency = f2IntegerPart,
                        FreqFractional = f2FractionalPart
                    },
                    PllLocked = (buf[2] & LBE_1425_STATUS_MASK_PLL_LOCKED) == LBE_1425_STATUS_MASK_PLL_LOCKED,
                    AntennaOk = (buf[2] & LBE_1425_STATUS_MASK_ANT_OK) == LBE_1425_STATUS_MASK_ANT_OK,
                    GpsLocked = (buf[2] & LBE_1425_STATUS_MASK_GPS_LOCKED) == LBE_1425_STATUS_MASK_GPS_LOCKED,
                    FllEnabled = buf[19] != 0
                };
            }
            else if (Model == GpsdoModel.LBE_1421)
            {
                GpsdoOutputMode out1Mode = GpsdoOutputMode.Normal;
                GpsdoOutputMode out2Mode = GpsdoOutputMode.Normal;
                bool out1PpsMode = (buf[2] & LBE_1425_STATUS_MASK_PPS_EN) == LBE_1425_STATUS_MASK_PPS_EN;
                bool out1NmeaMode = buf[25] != 0;
                bool out1LowPowerMode = buf[20] != 0;
                bool out2LowPowerMode = buf[21] != 0;
                if (out1NmeaMode)
                {
                    out1Mode = GpsdoOutputMode.NMEA;
                }
                else if (out1PpsMode)
                {
                    out1Mode = GpsdoOutputMode.PPS;
                }
                else if (out1LowPowerMode)
                {
                    out1Mode = GpsdoOutputMode.LowPower;
                }

                if (out2LowPowerMode)
                {
                    out2Mode = GpsdoOutputMode.LowPower;
                }

                Status = new GpsdoStatus
                {
                    RawStatus = buf[2],
                    Output1 = new GpsdoOutputStatus
                    {
                        Enabled = (buf[2] & LBE_1425_STATUS_MASK_OUT1_EN) == LBE_1425_STATUS_MASK_OUT1_EN,
                        Mode = out1Mode,
                        LedOn = out1PpsMode, // No explicit LED bit, but the PPS mode turns on the LED according to other 3rd party tools
                        Frequency = f1IntegerPart,
                        FreqFractional = f1FractionalPart
                    },
                    Output2 = new GpsdoOutputStatus
                    {
                        Enabled = (buf[2] & LBE_1425_STATUS_MASK_OUT2_EN) == LBE_1425_STATUS_MASK_OUT2_EN,
                        Mode = out2Mode,
                        LedOn = false, // No LED bit or PPS mode for output 2, so assume LED is off when enabled
                        Frequency = f2IntegerPart,
                        FreqFractional = f2FractionalPart
                    },
                    FllEnabled = buf[19] != 0,
                    PllLocked = (buf[2] & LBE_1425_STATUS_MASK_PLL_LOCKED) != 0,
                    AntennaOk = (buf[2] & LBE_1425_STATUS_MASK_ANT_OK) != 0,
                    GpsLocked = true
                };
            }
            else if (Model == GpsdoModel.LBE_1420)
            {
                GpsdoOutputMode out1Mode = GpsdoOutputMode.Normal;
                bool out1LowPowerMode = buf[20] != 0;
                if (out1LowPowerMode)
                {
                    out1Mode = GpsdoOutputMode.LowPower;
                }
                Status = new GpsdoStatus
                {
                    RawStatus = buf[2],
                    Output1 = new GpsdoOutputStatus
                    {
                        Enabled = (buf[2] & LBE_1425_STATUS_MASK_OUT1_EN) == LBE_1425_STATUS_MASK_OUT1_EN, // Other 3rd Party tool's say this is always ON.
                        Mode = out1Mode,
                        LedOn = false, // No explicit LED bit, and the LED does not turn on in PPS mode for this model, so assume LED is always off
                        Frequency = f1IntegerPart,
                        FreqFractional = f1FractionalPart
                    },
                    Output2 = new GpsdoOutputStatus
                    {
                        Enabled = false,
                        Mode = GpsdoOutputMode.NotPresent,
                        LedOn = false,
                        Frequency = 0,
                        FreqFractional = 0
                    },
                    FllEnabled = buf[19] != 0,
                    PllLocked = (buf[2] & LBE_1425_STATUS_MASK_PLL_LOCKED) != 0,
                    AntennaOk = (buf[2] & LBE_1425_STATUS_MASK_ANT_OK) != 0,
                    GpsLocked = true
                };
            }
            else
            {
                throw new Exception("Unsupported model.");
            }

            return Status;
        }

        public void SetFrequency(GpsdoOutput output, uint frequency, uint factional = 0)
        {
            GpsdoInstruction outFreqInstr;
            if (output == GpsdoOutput.One)
            {
                outFreqInstr = InstructionSet[(int)GpsdoInstructionIndex.SetOut1Freq];
                if (!outFreqInstr.Supported) throw new Exception("Device does not support SET_FREQ (output 1) instruction.");
            }
            else if (output == GpsdoOutput.Two)
            {
                outFreqInstr = InstructionSet[(int)GpsdoInstructionIndex.SetOut2Freq];
                if (!outFreqInstr.Supported) throw new Exception("Device does not support SET_FREQ (output 2) instruction.");
            }
            else
            {
                throw new Exception("Invalid output specified.");
            }

            var buf = new byte[REPORT_SIZE];
            buf[0] = 0;

            if (SupportsQ32_32)
            {
                // Q32.32 format (dual output devices)
                buf[1] = outFreqInstr.Instruction;
                buf[2] = (byte)(factional >> 0);
                buf[3] = (byte)(factional >> 8);
                buf[4] = (byte)(factional >> 16);
                buf[5] = (byte)(factional >> 24);
                buf[6] = (byte)(frequency >> 0);
                buf[7] = (byte)(frequency >> 8);
                buf[8] = (byte)(frequency >> 16);
                buf[9] = (byte)(frequency >> 24);
            }
            else
            {
                // Only output 1, integer frequency
                buf[1] = outFreqInstr.Instruction;
                buf[2] = (byte)(frequency >> 0);
                buf[3] = (byte)(frequency >> 8);
                buf[4] = (byte)(frequency >> 16);
                buf[5] = (byte)(frequency >> 24);
            }

            SetFeatureReport(buf);
        }

        public void SetFrequencyTemp(GpsdoOutput output, uint frequency, uint factional = 0)
        {
            GpsdoInstruction outFreqInstr;
            if (output == GpsdoOutput.One)
            {
                outFreqInstr = InstructionSet[(int)GpsdoInstructionIndex.SetOut1FreqTemp];
                if (!outFreqInstr.Supported) throw new Exception("Device does not support SET_FREQ_TEMP (output 1) instruction.");
            }
            else if (output == GpsdoOutput.Two)
            {
                outFreqInstr = InstructionSet[(int)GpsdoInstructionIndex.SetOut2FreqTemp];
                if (!outFreqInstr.Supported) throw new Exception("Device does not support SET_FREQ_TEMP (output 2) instruction.");
            }
            else
            {
                throw new Exception("Invalid output specified.");
            }


            var buf = new byte[REPORT_SIZE];
            buf[0] = 0;

            if (SupportsQ32_32)
            {
                buf[1] = outFreqInstr.Instruction;
                buf[2] = (byte)(factional >> 0);
                buf[3] = (byte)(factional >> 8);
                buf[4] = (byte)(factional >> 16);
                buf[5] = (byte)(factional >> 24);
                buf[6] = (byte)(frequency >> 0);
                buf[7] = (byte)(frequency >> 8);
                buf[8] = (byte)(frequency >> 16);
                buf[9] = (byte)(frequency >> 24);
            }
            else
            {
                buf[1] = outFreqInstr.Instruction;
                buf[2] = (byte)(frequency >> 0);
                buf[3] = (byte)(frequency >> 8);
                buf[4] = (byte)(frequency >> 16);
                buf[5] = (byte)(frequency >> 24);
            }

            SetFeatureReport(buf);
        }

        public void SetOutputEnable(GpsdoOutput output, bool enable)
        {
            GpsdoInstruction outEnInstr = InstructionSet[(int)GpsdoInstructionIndex.EnableOutput];
            if (!outEnInstr.Supported) throw new Exception("Device does not support the ENABLE_OUT instruction.");

            var buf = new byte[REPORT_SIZE];
            buf[0] = 0;
            buf[1] = outEnInstr.Instruction;

            if (output == GpsdoOutput.All)
            {
                Status.Output1.Enabled = enable;
                if (DualOutput)
                {
                    Status.Output2.Enabled = enable;
                }
            }
            else if (output == GpsdoOutput.One)
            {
                Status.Output1.Enabled = enable;
            }
            else if (output == GpsdoOutput.Two)
            {
                Status.Output2.Enabled = enable;
            }

            buf[2] = 0;

            if (Status.Output1.Enabled)
            {
                buf[2] |= 0x01;
            }
            if (Status.Output2.Enabled)
            {
                buf[2] |= 0x02;
            }

            SetFeatureReport(buf);
        }

        public void BlinkLeds()
        {
            GpsdoInstruction blinkInstr = InstructionSet[(int)GpsdoInstructionIndex.Blink];
            if (!blinkInstr.Supported) throw new Exception("Device does not support the BLINK instruction.");

            var buf = new byte[REPORT_SIZE];
            buf[0] = 0;
            buf[1] = blinkInstr.Instruction;
            SetFeatureReport(buf);
        }

        public void SetPllMode(bool pllMode)
        {
            GpsdoInstruction pllInstr = InstructionSet[(int)GpsdoInstructionIndex.SetPLL];
            if (!pllInstr.Supported) throw new Exception("Device does not support the PLL instruction.");

            var buf = new byte[REPORT_SIZE];
            buf[0] = 0;
            buf[1] = pllInstr.Instruction;
            buf[2] = (byte)(pllMode ? 0x00 : 0x01);
            SetFeatureReport(buf);
        }

        public void SetPps(bool enable)
        {
            GpsdoInstruction ppsInstr = InstructionSet[(int)GpsdoInstructionIndex.EnableOut1PPS];
            if (!ppsInstr.Supported) throw new Exception("Device does not support PPS instruction.");

            var buf = new byte[REPORT_SIZE];
            buf[0] = 0;
            buf[1] = ppsInstr.Instruction;
            buf[2] = (byte)(enable ? 0x01 : 0x00);
            SetFeatureReport(buf);
        }

        public void SetNmea(bool enable)
        {
            GpsdoInstruction ppsInstr = InstructionSet[(int)GpsdoInstructionIndex.EnableOut1Nmea];
            if (!ppsInstr.Supported) throw new Exception("Device does not support NMEA instruction.");

            var buf = new byte[REPORT_SIZE];
            buf[0] = 0;
            buf[1] = ppsInstr.Instruction;
            buf[2] = (byte)(enable ? 0x01 : 0x00);
            SetFeatureReport(buf);
        }

        public void SetPowerLevel(GpsdoOutput output, bool lowPower)
        {
            GpsdoInstruction pwrInstr;
            if (output == GpsdoOutput.One)
            {
                pwrInstr = InstructionSet[(int)GpsdoInstructionIndex.SetOut1PowerLevel];
                if (!pwrInstr.Supported) throw new Exception("Device does not support POWER_LEVEL (output 1) instruction.");
            }
            else if (output == GpsdoOutput.Two)
            {
                pwrInstr = InstructionSet[(int)GpsdoInstructionIndex.SetOut2PowerLevel];
                if (!pwrInstr.Supported) throw new Exception("Device does not support POWER_LEVEL (output 2) instruction.");
            }
            else
            {
                throw new Exception("Invalid output specified.");
            }

            var buf = new byte[REPORT_SIZE];
            buf[0] = 0x0;
            buf[1] = pwrInstr.Instruction;
            buf[2] = (byte)(lowPower ? 0x01 : 0x00);
            SetFeatureReport(buf);
        }
    }
}
