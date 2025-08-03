using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CANUDS_DTC_Report.Models;

namespace CANUDS_DTC_Report
{
    public class HtmlReportGenerator
    {
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
            html.AppendLine(".dtc-bytes .b1 { background:#fdd; }");
            html.AppendLine(".dtc-bytes .b2 { background:#dfd; }");
            html.AppendLine(".dtc-bytes .b3 { background:#ddf; }");
            html.AppendLine(".dtc-bytes .status { background:#eee; }");
            html.AppendLine(".dtc-bytes .unused { color:#999; }");
            html.AppendLine("</style></head><body>");

            html.AppendLine("<h1>Reporte de Códigos DTC vía UDS</h1>");
            html.AppendLine("<p>Este reporte fue generado automáticamente interpretando respuestas UDS conforme a ISO 14229-1 y formato ISO-TP.</p>");
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
            html.AppendLine("<tr><th>DTC</th><th>ID CAN</th><th>Subfunción</th><th>Fragmento TRC</th><th>Fragmento DTC</th><th>Paso a paso</th></tr>");

            foreach (var dtc in dtcs)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td><a href=\"https://dot.report/dtc/{dtc.Code}\" target=\"_blank\">{dtc.Code}</a></td>");
                html.AppendLine($"<td>0x{dtc.CanId:X3}</td>");
                html.AppendLine($"<td>{dtc.SubFunction}</td>");
                html.AppendLine($"<td><pre>{dtc.MessageFragment}</pre></td>");
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
                    foreach (var dtc in group)
                    {
                        html.AppendLine($"<tr><td>{dtc.Code}</td><td>{dtc.Description}</td></tr>");
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
