using System;

namespace CANUDS_DTC_Report.Models
{
    public class CanFrame
    {
        public uint Id { get; set; }
        public byte[] Data { get; set; }
        public DateTime Timestamp { get; set; }

        public CanFrame(uint id, byte[] data, DateTime timestamp)
        {
            Id = id;
            Data = data;
            Timestamp = timestamp;
        }
    }
}
