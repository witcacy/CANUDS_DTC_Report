using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CANUDS_DTC_Report.Models;

namespace CANUDS_DTC_Report
{

    public partial class MainForm : Form
    {
        private string trcFilePath;
        public MainForm()
        {
            InitializeComponent();
        }

        private void BtnLoadTrc_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "PCAN Trace Files (*.trc)|*.trc";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    trcFilePath = dialog.FileName;
                    txtOutput.AppendText($"Archivo cargado: {trcFilePath}{Environment.NewLine}");
                }
            }
        }

        private void BtnGenerateReport_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(trcFilePath))
            {
                MessageBox.Show("Por favor, carga un archivo .trc primero.");
                return;
            }

            try
            {
                // Paso 1: Leer y parsear .trc
                var frames = TrcParser.Parse(trcFilePath);
                var messages = IsoTpDecoder.Decode(frames);
                var udsMessages = UdsInterpreter.ClassifyUdsMessages(messages);
                var dtcs = UdsInterpreter.ExtractDTCs(messages);
                var ecuInfos = UdsInterpreter.ExtractEcuInfos(messages);
                var analysis = dtcs.Count == 0 ? UdsInterpreter.AnalyzeDtcAbsence(udsMessages) : $"Se encontraron {dtcs.Count} DTC(s).";

                foreach (var msg in udsMessages)
                {
                    var role = msg.IsPositiveResponse ? "Resp" : "Req";
                    var extra = msg.NegativeResponseCode.HasValue ? $" NRC 0x{msg.NegativeResponseCode.Value:X2}" : string.Empty;
                    txtOutput.AppendText($"{role} {msg.ServiceName} SID 0x{msg.RawServiceId:X2}{extra}{Environment.NewLine}");
                }
                foreach (var info in ecuInfos)
                {
                    txtOutput.AppendText($"ECU Info {info.Service} {info.Identifier}: {info.Value}{Environment.NewLine}");
                }
                txtOutput.AppendText($"Analisis: {analysis}{Environment.NewLine}");

                // Paso 2: Guardar HTML
                var saveDialog = new SaveFileDialog();
                saveDialog.Filter = "HTML Files (*.html)|*.html";
                saveDialog.FileName = "Reporte_DTC_CAN_UDS.html";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    var generator = new HtmlReportGenerator();
                    generator.GenerateReport(dtcs, udsMessages, ecuInfos, analysis, saveDialog.FileName);
                    txtOutput.AppendText($"Reporte generado exitosamente: {saveDialog.FileName}{Environment.NewLine}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}
