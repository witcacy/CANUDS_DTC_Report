using System.Collections.Generic;
using CANUDS_DTC_Report.Models;

namespace CANUDS_DTC_Report
{
    public class UdsInterpreter
    {
        public static  List<DtcInfo> ExtractDTCs(List<IsoTpMessage> messages)
        {
            var dtcs = new List<DtcInfo>();

            foreach (var msg in messages)
            {
                if (msg.Payload.Count < 3 || msg.Payload[0] != 0x59) // 0x59 = Positive Response to 0x19
                    continue;

                byte reportType = msg.Payload[1];
                byte dtcFormatIdentifier = msg.Payload[2];

                int index = 3;
                while (index + 3 <= msg.Payload.Count)
                {
                    uint dtcRaw = (uint)((msg.Payload[index] << 16) | (msg.Payload[index + 1] << 8) | msg.Payload[index + 2]);
                    string dtcCode = ConvertToDtcCode(dtcRaw);
                    string description = "Unknown"; // Puede mapearse con base de datos si se desea
                    string status = "Stored"; // Este campo puede mejorarse según los bits de estado
                    string severity = "Unknown";
                    string origin = "ECU";

                    dtcs.Add(new DtcInfo(dtcCode, description, status, severity, origin));

                    index += 4; // 3 bytes DTC + 1 byte status availability mask
                }
            }

            return dtcs;
        }

        private static  string ConvertToDtcCode(uint raw)
        {
            string[] dtcLetters = { "P", "C", "B", "U" };
            int firstNibble = (int)((raw & 0xC00000) >> 22);
            string dtc = dtcLetters[firstNibble] +
                         ((raw >> 16) & 0x3F).ToString("X2") +
                         ((raw >> 8) & 0xFF).ToString("X2");

            return dtc;
        }
    }
}
