namespace CANUDS_DTC_Report.Models
{
    public class DtcInfo
    {
        public string Code { get; set; }            // Ejemplo: "P0135"
        public string Description { get; set; }     // Ejemplo: "Heated Oxygen Sensor Circuit Malfunction"
        public string Status { get; set; }          // Ejemplo: "Stored/Active"
        public string Severity { get; set; }        // Ejemplo: "High"
        public string Origin { get; set; }          // Ejemplo: "ECM"

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
