namespace libGpsdo
{
    public enum GpsdoStatusMask : byte
    {
        GpsLocked = 0x01,
        PllLocked = 0x02,
        AntennaOk = 0x04,
        Output1Led = 0x08,
        Output2Led = 0x10,
        Output1En = 0x20,
        Output2En = 0x40,
        PpsEnabled = 0x80
    }
}
