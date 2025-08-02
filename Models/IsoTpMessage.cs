using System.Collections.Generic;

namespace CANUDS_DTC_Report.Models
{
    public class IsoTpMessage
    {
        public uint Id { get; set; }
        public List<byte> Payload { get; set; }
        public int ExpectedLength { get; set; }

        public IsoTpMessage(uint id)
        {
            Id = id;
            Payload = new List<byte>();
            ExpectedLength = 0;
        }
    }
}
