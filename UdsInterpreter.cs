using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using CANUDS_DTC_Report.Models;
using Peak.Can.Uds;

namespace CANUDS_DTC_Report
{
    public class UdsInterpreter
    {
        private static readonly Dictionary<uint, string> EcuMap = new Dictionary<uint, string>()
        {
            { 0x7E8, "ECM (Engine Control Module)" },
            { 0x7EC, "TCM (Transmission Control Module)" },
            { 0x7EF, "BCM (Body Control Module)" },
            { 0x7F0, "EPS (Electric Power Steering)" }
        };

        public static List<DtcInfo> ExtractDTCs(List<IsoTpMessage> messages)
        {
            var dtcs = new List<DtcInfo>();

            for (int msgIndex = 0; msgIndex < messages.Count; msgIndex++)
            {
                var msg = messages[msgIndex];
                if (msg.Payload.Count < 3 || msg.Payload[0] != 0x59) // 0x59 = Positive Response to 0x19
                    continue;

                string subFunction = msg.Payload.Count > 1 ? msg.Payload[1].ToString("X2") : "";

                int index = 3;
                while (index + 3 < msg.Payload.Count)
                {
                    uint dtcRaw = (uint)((msg.Payload[index] << 16) | (msg.Payload[index + 1] << 8) | msg.Payload[index + 2]);
                    string dtcCode = ConvertToDtcCode(dtcRaw);
                    string description = "Unknown"; // Can map with database if desired
                    string status = "Stored"; // TODO: decode status bits
                    string severity = "Unknown";
                    string origin = EcuMap.TryGetValue(msg.Id, out var name) ? name : "Desconocido";

                    byte b1 = msg.Payload[index];
                    byte b2 = msg.Payload[index + 1];
                    byte b3 = msg.Payload[index + 2];
                    byte statusByte = msg.Payload[index + 3];

                    // Build a table of all payload bytes highlighting the DTC bytes
                    var bytesHtml = new StringBuilder();
                    bytesHtml.Append("<table class='dtc-bytes'><tr>");
                    for (int i = 0; i < msg.Payload.Count; i++)
                    {
                        string cls = "unused";
                        if (i == index) cls = "b1";
                        else if (i == index + 1) cls = "b2";
                        else if (i == index + 2) cls = "b3";
                        else if (i == index + 3) cls = "status";
                        bytesHtml.Append($"<td class='{cls}'>{msg.Payload[i]:X2}</td>");
                    }
                    bytesHtml.Append("</tr></table>");
                    string coloredFragment = bytesHtml.ToString();

                    // Preserve spacing of TRC lines
                    string fragment = string.Join("\n", msg.RawLines.Select(WebUtility.HtmlEncode));
                    int typeBitsVal = (int)((dtcRaw & 0xC00000) >> 22);
                    string bits = Convert.ToString(typeBitsVal, 2).PadLeft(2, '0');
                    string typeBits = $"{bits} -> {dtcCode[0]}";
                    string explanation =
                        "<ol>" +
                        $"<li>ISO-TP ID <mark>0x{msg.Id:X3}</mark> (ISO 15765-2)</li>" +
                        $"<li>Respuesta <mark>0x59</mark> al servicio <mark>0x19</mark> (ISO 14229-1)</li>" +
                        $"<li>Bytes <span class='b1'>{b1:X2}</span> <span class='b2'>{b2:X2}</span> <span class='b3'>{b3:X2}</span> -> DTC <strong>{dtcCode}</strong></li>" +
                        $"<li>Estado <span class='status'>{statusByte:X2}</span> (bits no usados en <span class='unused'>gris</span>)</li>" +
                        "</ol>";

                    var info = new DtcInfo(dtcCode, description, status, severity, origin)
                    {
                        SubFunction = subFunction,
                        MessageFragment = fragment,
                        ColoredFragment = coloredFragment,
                        MessageNumber = msgIndex + 1,
                        TypeBits = typeBits,
                        CanId = msg.Id,
                        Explanation = explanation
                    };

                    dtcs.Add(info);

                    index += 4; // 3 bytes DTC + 1 byte status availability mask
                }
            }

            return dtcs;
        }

        public static List<EcuInfo> ExtractEcuInfos(List<IsoTpMessage> messages)
        {
            var infos = new List<EcuInfo>();

            foreach (var msg in messages)
            {
                if (msg.Payload.Count < 3) continue;
                byte sid = msg.Payload[0];

                if (sid == 0x5A) // Positive response to 0x1A (ECUINF)
                {
                    byte subFunc = msg.Payload[1];
                    var data = msg.Payload.Skip(2).ToArray();
                    infos.Add(new EcuInfo
                    {
                        Service = "ECUINF",
                        Identifier = $"0x{subFunc:X2}",
                        Value = DecodeData(data),
                        CanId = msg.Id
                    });
                }
                else if (sid == 0x62) // Positive response to 0x22 (ReadDataByIdentifier)
                {
                    if (msg.Payload.Count < 4) continue;
                    ushort did = (ushort)((msg.Payload[1] << 8) | msg.Payload[2]);
                    var data = msg.Payload.Skip(3).ToArray();
                    infos.Add(new EcuInfo
                    {
                        Service = "ECU Information",
                        Identifier = $"0x{did:X4}",
                        Value = DecodeData(data),
                        CanId = msg.Id
                    });
                }
            }

            return infos;
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

        private static string DecodeData(byte[] data)
        {
            if (data.All(b => b >= 0x20 && b <= 0x7E))
                return Encoding.ASCII.GetString(data);
            return BitConverter.ToString(data).Replace("-", " ");
        }

        private static string GetServiceName(byte sid)
        {
            var name = Enum.GetName(typeof(uds_service), (uds_service)sid);
            if (name != null)
                return name;
            if (sid == 0x1A)
                return "ECU Information";
            return $"0x{sid:X2}";
        }
    }
}
