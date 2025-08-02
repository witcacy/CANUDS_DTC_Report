namespace CANUDS_DTC_Report.Models
{
    public class DtcInfo
    {
        public string Code { get; set; }            // Ejemplo: "P0135"
        public string Description { get; set; }     // Ejemplo: "Heated Oxygen Sensor Circuit Malfunction"
        public string Status { get; set; }          // Ejemplo: "Stored/Active"
        public string Severity { get; set; }        // Ejemplo: "High"
        public string Origin { get; set; }          // Ejemplo: "ECM"
        public string MessageFragment { get; set; } // Fragmento hexadecimal del mensaje que contiene el DTC
        public int MessageNumber { get; set; }      // Número de mensaje ISO-TP donde se encontró
        public string TypeBits { get; set; }        // Bits decodificados que indican el tipo de DTC

        public DtcInfo(string code, string description, string status, string severity, string origin)
        {
            Code = code;
            Description = description;
            Status = status;
            Severity = severity;
            Origin = origin;
        }
    }
}
