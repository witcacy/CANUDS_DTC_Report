using System.Collections.Generic;

namespace CANUDS_DTC_Report.Models
{
    public class UdsMessageInfo
    {
        public uint CanId { get; set; }
        public byte RawServiceId { get; set; }
        public byte ServiceId { get; set; }
        public string ServiceName { get; set; }
        public bool IsPositiveResponse { get; set; }
        public byte? NegativeResponseCode { get; set; }
        public List<byte> Payload { get; set; } = new List<byte>();
    }
}
