namespace libGpsdo
{

    public class GpsdoInstruction
    {
        public string Name {get; set;} = "";
        public byte Instruction {get; set;}
        public bool Supported {get; set;}
    }
}
