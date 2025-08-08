using System;
using System.Windows.Forms;

namespace CANUDS_DTC_Report
{
    public class MdiParentForm : Form
    {
        public MdiParentForm()
        {
            this.IsMdiContainer = true;
            this.WindowState = FormWindowState.Maximized;
            this.Load += MdiParentForm_Load;
        }

        private void MdiParentForm_Load(object sender, EventArgs e)
        {
            var main = new MainForm();
            main.MdiParent = this;
            main.WindowState = FormWindowState.Maximized;
            main.Show();
        }
    }
}
