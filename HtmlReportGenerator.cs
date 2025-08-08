using CANUDS_DTC_Report.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CANUDS_DTC_Report
{
    public class HtmlReportGenerator
    {
        public string GenerateTrcFragment(string rawFragment, int dtcIndex,
                                          byte b1, byte b2, byte b3, byte statusByte)
        {
            var sb = new StringBuilder();
            var lines = rawFragment.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var frames = new List<(string id, byte[] data)>();
            var payload = new List<byte>();

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"^(0x[0-9A-Fa-f]+)\s+((?:[0-9A-Fa-f]{2}\s*)+)$");
                if (!match.Success) continue;

                string id = match.Groups[1].Value;
                byte[] bytes = match.Groups[2].Value
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => Convert.ToByte(x, 16))
                    .ToArray();

                frames.Add((id, bytes));
                payload.AddRange(bytes);
            }

            var highlights = LocateDtcBytes(payload, dtcIndex, b1, b2, b3, statusByte);

            sb.AppendLine("<table class='dtc-bytes'>");
            sb.AppendLine("<thead><tr><th>ID CAN</th><th></th><th></th><th></th><th></th><th></th><th></th><th></th><th></th></tr></thead>");
            sb.AppendLine("<tbody>");

            int globalIndex = 0;
            foreach (var (id, data) in frames)
            {
                sb.Append("<tr>");
                sb.AppendFormat("<td style='background:#ffc;font-weight:bold'>{0}</td>", id);

                foreach (var b in data)
                {
                    string hex = b.ToString("X2");
                    if (highlights.TryGetValue(globalIndex, out var hl))
                        sb.AppendFormat("<td class='{0}' title='{1}'>{2}</td>", hl.cssClass, hl.label, hex);
                    else
                        sb.AppendFormat("<td>{0}</td>", hex);

                    globalIndex++;
                }

                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</tbody></table>");
            return sb.ToString();
        }

        public string GenerateDtcTable(byte b1, byte b2, byte b3, byte statusByte)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<table class='dtc-bytes'>");
            sb.AppendLine("<thead><tr><th>MSB</th><th>Middle</th><th>LSB</th><th>Status</th></tr></thead>");
            sb.AppendLine("<tbody>");
            sb.AppendFormat("<tr><td class='b1'>{0:X2}</td><td class='b2'>{1:X2}</td><td class='b3'>{2:X2}</td><td class='status'>{3:X2}</td></tr>",
                            b1, b2, b3, statusByte);
            sb.AppendLine("</tbody></table>");
            return sb.ToString();
        }

        private Dictionary<int, (string cssClass, string label)> LocateDtcBytes(List<byte> allBytes, int occurrenceIndex,
                                                                               byte pb1, byte pb2, byte pb3, byte status)
        {
            var seq = new byte[] { pb1, pb2, pb3, status };
            int found = 0;
            for (int i = 0; i <= allBytes.Count - seq.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < seq.Length; j++)
                {
                    if (allBytes[i + j] != seq[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    if (found == occurrenceIndex)
                    {
                        return new Dictionary<int, (string cssClass, string label)>
                        {
                            { i,     ("b1", "MSB") },
                            { i + 1, ("b2", "Middle") },
                            { i + 2, ("b3", "LSB") },
                            { i + 3, ("status", "Status") }
                        };
                    }
                    found++;
                    i += seq.Length - 1;
                }
            }
            return new Dictionary<int, (string cssClass, string label)>();
        }

        public void GenerateReport(List<DtcInfo> dtcs, List<UdsMessageInfo> udsMessages, List<EcuInfo> ecuInfos, string analysis, string outputPath)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head><meta charset='UTF-8'><title>UDS DTC Report</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial; }");
            html.AppendLine("table { border-collapse: collapse; width: 100%; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; }");
            html.AppendLine("th { background-color: #f2f2f2; }");
            html.AppendLine("pre { margin:0; font-family:monospace; }");
            html.AppendLine(".dtc-bytes td { border:1px solid #ddd; font-family:monospace; padding:2px; width:2.5em; text-align:center; }");
            html.AppendLine(".b1 { background:#fdd; }");
            html.AppendLine(".b2 { background:#dfd; }");
            html.AppendLine(".b3 { background:#ddf; }");
            html.AppendLine(".status { background:#eee; }");
            html.AppendLine(".unused { color:#999; }");
            html.AppendLine("</style></head><body>");

            html.AppendLine("<h1>Reporte de Códigos DTC vía UDS</h1>");
            html.AppendLine("<p>Este reporte fue generado automáticamente interpretando respuestas UDS conforme a ISO 14229-1 y formato ISO-TP.</p>");

            var headerPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Report_Hader_Step_by_Step.html");
            if (File.Exists(headerPath))
            {
                html.AppendLine(File.ReadAllText(headerPath));
            }

            html.AppendLine($"<p>Total de mensajes UDS analizados: {udsMessages.Count}</p>");
            html.AppendLine("<h2>Información de ECU</h2>");
            if (ecuInfos.Count == 0)
            {
                html.AppendLine("<p>No se detectó información de ECU.</p>");
            }
            else
            {
                html.AppendLine("<table>");
                html.AppendLine("<tr><th>ID CAN</th><th>Servicio</th><th>Identificador</th><th>Valor</th></tr>");
                foreach (var info in ecuInfos)
                {
                    html.AppendLine("<tr>");
                    html.AppendLine($"<td>0x{info.CanId:X3}</td>");
                    html.AppendLine($"<td>{info.Service}</td>");
                    html.AppendLine($"<td>{info.Identifier}</td>");
                    html.AppendLine($"<td>{info.Value}</td>");
                    html.AppendLine("</tr>");
                }
                html.AppendLine("</table>");
            }

            html.AppendLine("<h2>Códigos DTC</h2>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>DTC</th><th>L-Code</th><th>Protocolo OBD II</th><th>Descripción</th><th>ID CAN</th><th>Subfunción</th><th>Fragmento TRC</th><th>Fragmento DTC</th><th>Paso a paso</th></tr>");

            foreach (var dtc in dtcs)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td><a href=\"https://dot.report/dtc/{dtc.Code}\" target=\"_blank\">{dtc.Code}</a></td>");
                html.AppendLine($"<td><a href=\"https://dot.report/dtc/{dtc.LCode}\" target=\"_blank\">{dtc.LCode}</a></td>");
                html.AppendLine($"<td>{dtc.ObdProtocol}</td>");
                html.AppendLine($"<td>{dtc.Description}</td>");
                html.AppendLine($"<td>0x{dtc.CanId:X3}</td>");
                html.AppendLine($"<td>{dtc.SubFunction}</td>");
                html.AppendLine($"<td>{dtc.MessageFragment}</td>");
                html.AppendLine($"<td>{dtc.ColoredFragment}</td>");
                html.AppendLine($"<td>{dtc.Explanation}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</table>");

            var ecuGroups = dtcs.GroupBy(d => d.CanId);
            if (ecuGroups.Any())
            {
                html.AppendLine("<h2>DTCs por ECU</h2>");
                foreach (var group in ecuGroups)
                {
                    var ecuName = ecuInfos.Where(e => e.CanId == group.Key)
                                           .Select(e => e.Value)
                                           .FirstOrDefault(v => v.Any(char.IsLetter));
                    html.AppendLine($"<h3>ECU ID: 0x{group.Key:X3}</h3>");
                    if (!string.IsNullOrEmpty(ecuName))
                        html.AppendLine($"<p>ECU Tipo: {ecuName}</p>");
                    html.AppendLine("<table>");
                    html.AppendLine("<tr><th>DTC</th><th>Descripción breve</th></tr>");
                    foreach (var d in group)
                    {
                        html.AppendLine($"<tr><td>{d.Code}</td><td>{d.Description}</td></tr>");
                    }
                    html.AppendLine("</table>");
                }
            }

            html.AppendLine("<h2>Mensajes UDS Detectados</h2>");
            if (udsMessages.Count == 0)
            {
                html.AppendLine("<p>No se detectaron mensajes compatibles con UDS.</p>");
            }
            else
            {
                html.AppendLine("<ul>");
                foreach (var msg in udsMessages)
                {
                    var role = msg.IsPositiveResponse ? "Resp" : "Req";
                    var extra = msg.NegativeResponseCode.HasValue ? $" NRC 0x{msg.NegativeResponseCode.Value:X2}" : string.Empty;
                    html.AppendLine($"<li>{role} {msg.ServiceName} (SID 0x{msg.RawServiceId:X2}){extra}</li>");
                }
                html.AppendLine("</ul>");
            }

            html.AppendLine("<h2>Análisis</h2>");
            html.AppendLine($"<p>{analysis}</p>");

            html.AppendLine("<p>Fin del reporte. Interpretado con librerías PEAK PCAN-UDS y PCAN-ISO-TP.</p>");
            html.AppendLine("</body></html>");

            File.WriteAllText(outputPath, html.ToString(), Encoding.UTF8);
        }
    }
}

