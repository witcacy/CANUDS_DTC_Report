using System;

namespace CANUDS_DTC_Report.Models
{
    public class EcuInfo
    {
        public string Service { get; set; }
        public string Identifier { get; set; }
        public string Value { get; set; }
        public uint CanId { get; set; } // ID CAN asociado a esta información
    }
}
