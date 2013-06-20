using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace Chromium_Updater
{
    public partial class SettingsForm : Form
    {
        private static bool formOpen = false;
        //
        private bool valid_latest = true;
        private bool valid_specific = true;

        public SettingsForm()
        {
            InitializeComponent();
        }

        #region Open & Close

        public static bool isOpen
        {
            get { return formOpen; }
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            formOpen = true;
            //
            WindowGeometry.GeometryFromString(Settings.Settings_Window_Geometry, this);
            //
            label4.Text          = "Version: " + Application.ProductVersion;
            textBox1.Text        = Settings.Latest_Revision_URL;
            toolTip1.SetToolTip(textBox1, Settings.Latest_Revision_URL);
            textBox3.Text        = Settings.Specific_Revision_URL;
            toolTip1.SetToolTip(textBox3, Settings.Specific_Revision_URL);
            textBox2.Text        = Settings.Installer_Arguments;
            toolTip1.SetToolTip(textBox2, Settings.Installer_Arguments);
            numericUpDown1.Value = Convert.ToDecimal(Settings.Check_Interval);
            checkBox2.Checked    = Settings.Start_Hidden;
            checkBox1.Checked    = Util.IsAutoStartEnabled("Chromium Updater", Application.StartupPath + "\\Chromium Updater.exe");
            //
            this.textBox3.TextChanged += new System.EventHandler(this.textBox3_TextChanged);
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
        }

        private void Settings_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (valid_latest) { Settings.Latest_Revision_URL = textBox1.Text.Trim(); }
            else
            {
                MessageBox.Show("Click the question mark next to the Latest Revision URL to validate.");
                e.Cancel = true;
            }
            if ((!textBox3.Text.EndsWith("/")) && (!textBox3.Text.EndsWith("\\"))) { textBox3.AppendText("/"); }
            if (valid_specific) { Settings.Specific_Revision_URL = textBox3.Text.Trim(); }
            else
            {
                MessageBox.Show("Click the question mark next to the Specific Revision URL to validate.");
                e.Cancel = true;
            }
            Settings.Check_Interval = Convert.ToInt32(numericUpDown1.Value);
            Settings.Installer_Arguments = textBox2.Text;
            Settings.Start_Hidden = checkBox2.Checked;
            //
            if (!e.Cancel) { Settings.Settings_Window_Geometry = WindowGeometry.GeometryToString(this); formOpen = false; }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Process.Start("http://logicpwn.com/chromium-updater/");
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                Util.SetAutoStart("Chromium Updater", Application.StartupPath + "\\Chromium Updater.exe");
            }
            else
            {
                Util.UnSetAutoStart("Chromium Updater");
            }
        }

        #endregion

        #region Checks & Reset

        #region URL Checker

        private void IsValidURL(string url_name, string url, bool exe, bool silent)
        {
            if (String.IsNullOrEmpty(url)) { DisplayValidURLResult(url_name, "empty"); }
            if (!Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute)) { DisplayValidURLResult(url_name, "Invalid URL"); }
            string[] p = new string[4];
            p[0] = url_name;
            p[1] = url;
            p[2] = exe.ToString();
            p[3] = silent.ToString();
            new Thread(new ParameterizedThreadStart(IsValidURLWorker)).Start((object)p);
        }

        private void IsValidURLWorker(object p)
        {
            string[] np = (string[])p;
            if (np[2] == Boolean.TrueString)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(np[1] + Settings.Current_Revision + "/mini_installer.exe");
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if (response.Headers["Content-Type"].Contains("application/octet-stream")) { DisplayValidURLResult(np[0], "valid"); }
                    response.Close();
                }
                catch (WebException) { DisplayValidURLResult(np[0], "Invalid webpage"); return; }
            }
            else
            {
                try
                {
                    string page = new WebClient().DownloadString(np[1]);
                    if (String.IsNullOrEmpty(page)) { DisplayValidURLResult(np[0], "Webpage returned no result"); }
                    Convert.ToInt32(page);
                }
                catch (FormatException) { DisplayValidURLResult(np[0], "Invalid webpage"); return; }
                catch (WebException e) { DisplayValidURLResult(np[0], e.Message); return; }
                //
                DisplayValidURLResult(np[0], "valid");
            }
        }

        private void DisplayValidURLResult(string url_name, string result)
        {
            if (result == "valid")
            {
                switch (url_name)
                {
                    case "Latest Revision URL":
                        valid_latest = true;
                        break;
                    case "Specific Revision URL":
                        valid_specific = true;
                        break;
                }
                //
                MessageBox.Show("The " + url_name + " is a valid URL.");
            }
            else if (result == "empty")
            {
                MessageBox.Show("The " + url_name + " is empty.");
            }
            else { MessageBox.Show(url_name + " returned error " + '"' + result + '"'); }
        }

        #endregion

        private void button2_Click(object sender, EventArgs e)
        {
            valid_latest = false;
            IsValidURL("Latest Revision URL", textBox1.Text.Trim(), false, false);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            valid_specific = false;
            if ((!textBox3.Text.EndsWith("/")) && (!textBox3.Text.EndsWith("\\"))) { textBox3.AppendText("/"); }
            IsValidURL("Specific Revision URL", textBox3.Text.Trim(), true, false);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            valid_latest = false;
            toolTip1.SetToolTip(textBox1, textBox1.Text);
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            valid_specific = false;
            toolTip1.SetToolTip(textBox3, textBox3.Text);
        }


        private void button4_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("This will reset all of the settings. Are you sure you want to continue?", "Chromium Updater", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Settings.reset();
                Settings_Load(this, null);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        #endregion
    }
}
