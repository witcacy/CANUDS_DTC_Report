using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CANUDS_DTC_Report.Models;

namespace CANUDS_DTC_Report
{
    public class HtmlReportGenerator
    {
        public void GenerateReport(List<DtcInfo> dtcs, List<UdsMessageInfo> udsMessages, string analysis, string outputPath)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head><meta charset='UTF-8'><title>UDS DTC Report</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial; }");
            html.AppendLine("table { border-collapse: collapse; width: 100%; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; }");
            html.AppendLine("th { background-color: #f2f2f2; }");
            html.AppendLine("pre { background-color: #f4f4f4; padding: 10px; border-radius: 5px; }");
            html.AppendLine("</style></head><body>");

            html.AppendLine("<h1>Reporte de Códigos DTC vía UDS</h1>");
            html.AppendLine("<p>Este reporte fue generado automáticamente interpretando respuestas UDS conforme a ISO 14229-1 y formato ISO-TP.</p>");
            html.AppendLine("<table>");
            html.AppendLine("<tr><th>DTC</th><th>Descripción</th><th>Estado</th><th>Origen</th><th>Explicación Técnica</th></tr>");

            foreach (var dtc in dtcs)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{dtc.Code}</td>");
                html.AppendLine($"<td>{dtc.Description}</td>");
                html.AppendLine($"<td>{dtc.Status}</td>");
                html.AppendLine($"<td>{dtc.Origin}</td>");
                html.AppendLine("<td>");

                html.AppendLine("<strong>Interpretación:</strong><br/>");
                html.AppendLine("<ul>");
                html.AppendLine("<li>Se utilizó el servicio <code>0x19 - ReadDTCInformation</code> del estándar <strong>ISO 14229-1:2013</strong>.</li>");
                html.AppendLine("<li>Se recibió una respuesta positiva <code>0x59</code> del ECU, lo cual indica éxito.</li>");
                html.AppendLine("<li>El DTC fue extraído de la estructura de respuesta codificada en ISO-TP (ISO 15765-2).</li>");
                html.AppendLine("<li>El código hexadecimal fue decodificado como un número de 24 bits: MSB→LSB → <code>AA BB CC</code> → combinado como un entero de 3 bytes.</li>");
                html.AppendLine("<li>Se identificó el tipo de DTC con los 2 bits más significativos según la tabla: 00 = P, 01 = C, 10 = B, 11 = U.</li>");
                html.AppendLine("<li>Se construyó el código estándar (por ejemplo, <code>P0100</code>) a partir de los bits restantes.</li>");
                html.AppendLine("</ul>");

                html.AppendLine("<strong>Más detalles:</strong>");
                html.AppendLine("<pre>");
                html.AppendLine($"- Código crudo (hex): 0x{dtc.Code}\n- Descripción: {dtc.Description ?? "No disponible"}");
                html.AppendLine($"- Estado interpretado: {dtc.Status}\n- Protocolo: ISO 15765-2 (ISO-TP) + ISO 14229-1 (UDS)");
                html.AppendLine("</pre>");

                html.AppendLine("</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</table>");

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
