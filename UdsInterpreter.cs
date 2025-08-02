using System;
using System.Collections.Generic;
using System.Linq;
using CANUDS_DTC_Report.Models;
using Peak.Can.Uds;

namespace CANUDS_DTC_Report
{
    public class UdsInterpreter
    {
        public static List<DtcInfo> ExtractDTCs(List<IsoTpMessage> messages)
        {
            var dtcs = new List<DtcInfo>();

            for (int msgIndex = 0; msgIndex < messages.Count; msgIndex++)
            {
                var msg = messages[msgIndex];
                if (msg.Payload.Count < 3 || msg.Payload[0] != 0x59) // 0x59 = Positive Response to 0x19
                    continue;

                int index = 3;
                while (index + 3 <= msg.Payload.Count)
                {
                    uint dtcRaw = (uint)((msg.Payload[index] << 16) | (msg.Payload[index + 1] << 8) | msg.Payload[index + 2]);
                    string dtcCode = ConvertToDtcCode(dtcRaw);
                    string description = "Unknown"; // Can map with database if desired
                    string status = "Stored"; // TODO: decode status bits
                    string severity = "Unknown";
                    string origin = "ECU";

                    string fragment = BitConverter.ToString(msg.Payload.Skip(index).Take(4).ToArray()).Replace("-", " ");
                    int typeBitsVal = (int)((dtcRaw & 0xC00000) >> 22);
                    string bits = Convert.ToString(typeBitsVal, 2).PadLeft(2, '0');
                    string typeBits = $"{bits} -> {dtcCode[0]}";

                    var info = new DtcInfo(dtcCode, description, status, severity, origin)
                    {
                        MessageFragment = fragment,
                        MessageNumber = msgIndex + 1,
                        TypeBits = typeBits
                    };

                    dtcs.Add(info);

                    index += 4; // 3 bytes DTC + 1 byte status availability mask
                }
            }

            return dtcs;
        }

        public static List<UdsMessageInfo> ClassifyUdsMessages(List<IsoTpMessage> messages)
        {
            var result = new List<UdsMessageInfo>();
            foreach (var msg in messages)
            {
                if (msg.Payload.Count == 0) continue;
                byte sid = msg.Payload[0];
                var info = new UdsMessageInfo
                {
                    CanId = msg.Id,
                    RawServiceId = sid,
                    Payload = msg.Payload
                };

                if (sid == 0x7F && msg.Payload.Count >= 3)
                {
                    info.ServiceId = msg.Payload[1];
                    info.ServiceName = GetServiceName(info.ServiceId);
                    info.IsPositiveResponse = false;
                    info.NegativeResponseCode = msg.Payload[2];
                }
                else
                {
                    bool positive = sid >= 0x40;
                    info.IsPositiveResponse = positive;
                    info.ServiceId = (byte)(positive ? sid - 0x40 : sid);
                    info.ServiceName = GetServiceName(info.ServiceId);
                }

                result.Add(info);
            }
            return result;
        }

        public static string AnalyzeDtcAbsence(List<UdsMessageInfo> udsMessages)
        {
            if (udsMessages.Any(m => m.IsPositiveResponse && m.ServiceId == (byte)uds_service.PUDS_SERVICE_SI_ReadDTCInformation))
                return "Se recibieron respuestas positivas al servicio ReadDTCInformation; verifique el decodificador de DTC.";

            if (udsMessages.Any(m => !m.IsPositiveResponse && m.ServiceId == (byte)uds_service.PUDS_SERVICE_SI_ReadDTCInformation))
            {
                var negative = udsMessages.FirstOrDefault(m => !m.IsPositiveResponse && m.RawServiceId == 0x7F && m.ServiceId == (byte)uds_service.PUDS_SERVICE_SI_ReadDTCInformation);
                if (negative != null && negative.NegativeResponseCode.HasValue)
                    return $"El ECU respondió de forma negativa al servicio ReadDTCInformation con NRC 0x{negative.NegativeResponseCode.Value:X2}.";
                return "Se solicitó la lectura de DTCs pero no hubo respuesta positiva.";
            }

            return "No se encontró ninguna solicitud al servicio ReadDTCInformation en la traza.";
        }

        private static string ConvertToDtcCode(uint raw)
        {
            string[] dtcLetters = { "P", "C", "B", "U" };
            int firstNibble = (int)((raw & 0xC00000) >> 22);
            string dtc = dtcLetters[firstNibble] +
                         ((raw >> 16) & 0x3F).ToString("X2") +
                         ((raw >> 8) & 0xFF).ToString("X2");
            return dtc;
        }

        private static string GetServiceName(byte sid)
        {
            var name = Enum.GetName(typeof(uds_service), (uds_service)sid);
            return name ?? $"0x{sid:X2}";
        }
    }
}
