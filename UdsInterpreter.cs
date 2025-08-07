using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
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

        private static readonly Dictionary<string, string> DtcDescriptions = LoadDtcDescriptions();

        private static Dictionary<string, string> LoadDtcDescriptions()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IMAGE_DTC_EPLANATION.txt");
                if (File.Exists(path))
                {
                    foreach (var line in File.ReadLines(path))
                    {
                        var match = Regex.Match(line, @"^(?<code>[A-Z][0-9A-Fa-f]{6})\s+\S+\s+(?<desc>.+)$");
                        if (match.Success)
                        {
                            dict[match.Groups["code"].Value] = match.Groups["desc"].Value.Trim();
                        }
                    }
                }
            }
            catch
            {
                // Swallow exceptions; absence of description file should not break parsing
            }
            return dict;
        }

        public static List<DtcInfo> ExtractDTCs(List<IsoTpMessage> messages)
        {
            var dtcs = new List<DtcInfo>();

            for (int msgIndex = 0; msgIndex < messages.Count; msgIndex++)
            {
                var msg = messages[msgIndex];
                if (msg.Payload.Count < 3 || msg.Payload[0] != 0x59)
                    continue;

                string subFunction = msg.Payload.Count > 1 ? msg.Payload[1].ToString("X2") : "";
                int dtcNumber = 0;
                int index = 3;

                while (index + 3 < msg.Payload.Count)
                {
                    byte b1 = msg.Payload[index];
                    byte b2 = msg.Payload[index + 1];
                    byte b3 = msg.Payload[index + 2];
                    byte statusByte = msg.Payload[index + 3];

                    uint dtcRaw = (uint)((b1 << 16) | (b2 << 8) | b3);
                    string dtcCode = ConvertToFullDtcCode(dtcRaw);
                    string lCode = $"{dtcCode[0]}{(dtcRaw & 0xFFFF):X4}";
                    string description = "Unknown"; // Simplificado sin diccionario
                    string status = DecodeStatusFlags(statusByte);
                    string severity = "Unknown";
                    string origin = EcuMap.TryGetValue(msg.Id, out var name) ? name : "Desconocido";

                    string obdProtocol;
                    switch (dtcCode[0])
                    {
                        case 'P':
                            obdProtocol = "P (Powertrain)";
                            break;
                        case 'C':
                            obdProtocol = "C (Chassis)";
                            break;
                        case 'B':
                            obdProtocol = "B (Body)";
                            break;
                        case 'U':
                            obdProtocol = "U (Network)";
                            break;
                        default:
                            obdProtocol = "Unknown";
                            break;
                    }

                    string rawFragment = string.Join("\n", msg.RawLines);
                    var generator = new HtmlReportGenerator();
                    string fragment = generator.GenerateTrcFragment(
                        rawFragment,
                        dtcNumber,
                        b1, b2, b3, statusByte
                    );
                    string coloredFragment = generator.GenerateDtcTable(b1, b2, b3, statusByte);

                    int typeBitsVal = (int)((dtcRaw & 0xC00000) >> 22);
                    string bits = Convert.ToString(typeBitsVal, 2).PadLeft(2, '0');
                    string typeBits = $"{bits} -> {dtcCode[0]}";

                    var statusBits = new StringBuilder();
                    for (int bit = 7; bit >= 0; bit--)
                    {
                        bool set = ((statusByte >> bit) & 1) == 1;
                        statusBits.Append(set ? "1" : "<span class='unused'>0</span>");
                    }

                    string explanation =
                        "<ol>" +
                        $"<li>El identificador CAN <mark>0x{msg.Id:X3}</mark> proviene de la trama ISO-TP que encapsula la respuesta.</li>" +
                        $"<li>El primer byte es <mark>0x59</mark>; restando 0x40 se identifica el servicio original <mark>0x19</mark> (ReadDTCInformation).</li>" +
                        $"<li>Los bytes <span class='b1'>{b1:X2}</span> <span class='b2'>{b2:X2}</span> <span class='b3'>{b3:X2}</span> forman 0x{dtcRaw:X6}. " +
                        $"Los bits 22-23 (= {bits}) señalan la letra <strong>{dtcCode[0]}</strong> y el resto produce {dtcCode.Substring(1)}.</li>" +
                        $"<li>El estado <span class='status'>{statusByte:X2}</span> = {statusBits}b; los bits en <span class='unused'>gris</span> no están activos.</li>" +
                        "</ol>";

                    var info = new DtcInfo(dtcCode, description, status, severity, origin)
                    {
                        SubFunction = subFunction,
                        MessageFragment = fragment,
                        ColoredFragment = coloredFragment,
                        MessageNumber = msgIndex + 1,
                        TypeBits = typeBits,
                        CanId = msg.Id,
                        Explanation = explanation,
                        LCode = lCode,
                        ObdProtocol = obdProtocol
                    };
                    info.CodeBytes.AddRange(new[] { b1.ToString("X2"), b2.ToString("X2"), b3.ToString("X2"), statusByte.ToString("X2") });

                    dtcs.Add(info);
                    dtcNumber++;
                    index += 4;
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
                         ((raw >> 8) & 0xFF).ToString("X2") +
                         (raw & 0xFF).ToString("X2");
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
        private static string ConvertToFullDtcCode(uint raw)
        {
            int typeBits = (int)((raw >> 22) & 0x3);
            char prefix;
            switch (typeBits)
            {
                case 0: prefix = 'P'; break;
                case 1: prefix = 'C'; break;
                case 2: prefix = 'B'; break;
                case 3: prefix = 'U'; break;
                default: prefix = '?'; break;
            }

            uint code = raw & 0x3FFFFF;
            return string.Format("{0}{1:X6}", prefix, code);
        }

        private static string DecodeStatusFlags(byte status)
        {
            if ((status & 0x08) != 0) return "Active/static";
            if ((status & 0x01) != 0) return "Passive/Sporadic";
            return "Unknown";
        }

    }
}
