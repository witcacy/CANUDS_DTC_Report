using System;

namespace CANUDS_DTC_Report.Models
{
    public class CanFrame
    {
        public uint Id { get; set; }
        public byte[] Data { get; set; }
        public DateTime Timestamp { get; set; }
        public string RawLine { get; set; }

        public CanFrame(uint id, byte[] data, DateTime timestamp, string rawLine)
        {
            Id = id;
            Data = data;
            Timestamp = timestamp;
            RawLine = rawLine;
        }
    }
}
