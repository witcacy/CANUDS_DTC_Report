using System.Collections.Generic;

namespace CANUDS_DTC_Report.Models
{
    public class DtcInfo
    {
        public string Code { get; set; }            // Ejemplo: "P0135"
        public string Description { get; set; }     // Ejemplo: "Heated Oxygen Sensor Circuit Malfunction"
        public string Status { get; set; }          // Ejemplo: "Stored/Active"
        public string Severity { get; set; }        // Ejemplo: "High"
        public string Origin { get; set; }          // Ejemplo: "ECM"
        public string Explanation { get; set; }     // Pasos e ISO que justifican el hallazgo
        public string SubFunction { get; set; }     // Subfunción / subcódigo del servicio UDS
        public string MessageFragment { get; set; } // Líneas sin parsear del TRC
        public string ColoredFragment { get; set; } // Fragmento de bytes del DTC coloreado
        public int MessageNumber { get; set; }      // Número de mensaje ISO-TP donde se encontró
        public string TypeBits { get; set; }        // Bits decodificados que indican el tipo de DTC
        public uint CanId { get; set; }             // ID CAN del ECU que envió el DTC

        // Código reducido derivado del DTC en formato Pxxxx
        public string LCode { get; set; }

        // Protocolo OBD II derivado del prefijo (P/C/B/U)
        public string ObdProtocol { get; set; }

        public List<string> CodeBytes { get; set; } = new List<string>(); // Bytes separados: MSB, middle, LSB, status

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
