using System;

namespace CANUDS_DTC_Report.Models
{
    public class CanFrame
    {
        public uint Id { get; set; }
        public byte[] Data { get; set; }
        public DateTime Timestamp { get; set; }
        public string RawLine { get; set; }

        /// <summary>
        /// Número de línea original del archivo TRC de donde proviene el frame.
        /// Permite ubicar fácilmente el fragmento en la traza.
        /// </summary>
        public int LineNumber { get; set; }

        public CanFrame(uint id, byte[] data, DateTime timestamp, string rawLine, int lineNumber)
        {
            Id = id;
            Data = data;
            Timestamp = timestamp;
            RawLine = rawLine;
            LineNumber = lineNumber;
        }
    }
}
