using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                var dtcs = UdsInterpreter.ExtractDTCs(messages);

                // Paso 2: Guardar HTML
                var saveDialog = new SaveFileDialog();
                saveDialog.Filter = "HTML Files (*.html)|*.html";
                saveDialog.FileName = "Reporte_DTC_CAN_UDS.html";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    var generator = new HtmlReportGenerator();
                    generator.GenerateReport(dtcs, saveDialog.FileName);
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
