using System;
using System.IO;
using System.Windows.Forms;

namespace CANUDS_DTC_Report
{
    public class ReportViewerForm : Form
    {
        private readonly string reportPath;
        private readonly WebBrowser browser;

        public ReportViewerForm(string reportPath)
        {
            this.reportPath = reportPath;
            this.browser = new WebBrowser { Dock = DockStyle.Fill };
            this.Controls.Add(browser);
            this.Text = Path.GetFileName(reportPath);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            browser.Navigate(reportPath);
            this.WindowState = FormWindowState.Maximized;
        }
    }
}
