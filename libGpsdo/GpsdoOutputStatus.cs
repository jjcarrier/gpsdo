namespace libGpsdo
{
    public class GpsdoOutputStatus
    {
        public bool Enabled { get; set; }
        public GpsdoOutputMode Mode { get; set; }
        public bool LedOn { get; set; }
        public uint Frequency { get; set; }
        public uint FreqFractional { get; set; }
    }
}
