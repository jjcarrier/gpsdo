namespace libGpsdo
{
    public class GpsdoStatus
    {
        public byte RawStatus { get; set; }
        public GpsdoOutputStatus Output1 { get; set; } = new GpsdoOutputStatus();
        public GpsdoOutputStatus Output2 { get; set; } = new GpsdoOutputStatus();
        public bool FllEnabled { get; set; }
        public bool PllLocked { get; set; }
        public bool GpsLocked { get; set; }
        public bool AntennaOk { get; set; }
    }
}
