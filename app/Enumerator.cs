using libGpsdo;

namespace GpsdoCli
{
    internal static class Enumerator
    {
        public static List<Gpsdo> Enumerate(int? vid = null, int? pid = null, string? serialNumber = null, bool debug = false)
        {
            var devices = new List<Gpsdo>();

            foreach (var supported in Gpsdo.Devices)
            {
                if (vid.HasValue && supported.VID != vid.Value)
                    continue;

                if (pid.HasValue && supported.PID != pid.Value)
                    continue;

                var candidates = HidSharp.DeviceList.Local.GetHidDevices(supported.VID, supported.PID);

                foreach (var candidate in candidates)
                {
                    string? serial = candidate.GetSerialNumber();

                    if (!string.IsNullOrWhiteSpace(serialNumber) && serial != serialNumber)
                        continue;

                    try
                    {
                        var dev = Gpsdo.Open(serial ?? "", debug: debug);
                        devices.Add(dev);
                    }
                    catch
                    {
                        // Device could not be opened; skip it.
                    }
                }
            }

            return devices;
        }
    }
}
