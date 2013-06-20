using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace Chromium_Updater
{
    public delegate void ChromiumCloseResult(bool closed);

    public partial class CloseChromiumForm : Form
    {
        public event ChromiumCloseResult CloseReturn;
        private DateTime open_time;

        public CloseChromiumForm()
        {
            InitializeComponent();
        }

        private void CloseChromiumForm_Load(object sender, EventArgs e)
        {
            open_time = DateTime.Now.AddMinutes(5);
        }

        private void CloseChromiumForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.Show_Chromium_Close_Dialog = (checkBox1.Checked) ? false : true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CloseReturn(closeChromium());
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            CloseReturn(false);
            this.Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!isChromiumRunning) { CloseReturn(true); this.Close(); }
            if (DateTime.Now > open_time) { button1_Click(this, null); }
        }

        public static bool isChromiumRunning
        {
            get
            {
                Process[] procs = Process.GetProcessesByName("chrome");
                foreach (Process proc in procs) { return true; }
                return false;
            }
        }

        public static bool closeChromium()
        {
            Process[] procs = Process.GetProcessesByName("chrome");
            foreach (Process proc in procs)
            {
                try
                {
                    if (!proc.CloseMainWindow()) { proc.Kill(); }
                    proc.Close();
                }
                catch { }
            }
            DateTime start_time = DateTime.Now.AddSeconds(5);
            while (isChromiumRunning)
            {
                if (DateTime.Now > start_time) { return false; }
                Thread.Sleep(100);
            }
            return true;
        }
    }
}
