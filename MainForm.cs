using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Thoughtful_Coding;

namespace Chromium_Updater
{
    public partial class MainForm : Form
    {
        #region Variables

        Mutex appMutex;
        //
        CUpdater updaterWorker;
        UpdaterForm selfUpdater;
        SettingsForm settingsForm;
        //
        List<string> messagePump = new List<string>();
        //
        private delegate void changeValueD(string value);
        private delegate void displayStatusD(string status, int refresh);
        private delegate void emptyD();

        #endregion

        #region Form Events

        #region Constructor/Load/Close

        #region Constructor

        public MainForm(string[] args)
        {
            Settings.load();
            //
            try
            {
                appMutex = Mutex.OpenExisting("chromium-updater-v2");
                //
                MessageBox.Show("Only one instance of Chromium Updater may exist");
                Environment.Exit(0);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                appMutex = new Mutex(false, "chromium-updater-v2");
            }
            //
            InitializeComponent();
        }

        #endregion

        #region Form Load

        private void MainForm_Load(object sender, EventArgs e)
        {
            WindowGeometry.GeometryFromString(Settings.Main_Window_Geometry, this);
            //
            if (Settings.Start_Hidden)
            {
                this.notifyIcon1.Visible = true;
                this.Hide();
            }
            //
            selfUpdater = new UpdaterForm(2);
            //
            label4.Text = Settings.Current_Revision.ToString();
            //
            updaterWorker = new CUpdater();
            updaterWorker.checkReturn += new updateCheck(updaterWorker_checkCompleted);
            updaterWorker.progressChanged += new downloadProgressChanged(updaterWorker_progressChanged);
            updaterWorker.downloadReturn += new Download(updaterWorker_fileDownloaded);
            updaterWorker.installReturn += new Install(updaterWorker_finished);
            //
            if (Settings.Check_Interval > 0)
            {
                timer1.Interval = Settings.Check_Interval * 60000;
                timer1.Enabled = true;
            }
            //
            new Thread(new ThreadStart(beginUpdate)).Start();
        }

        #endregion

        #region Form Closing

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.notifyIcon1.Visible = true;
                this.Hide();
            }
            else
            {
                closeCU(true);
            }
        }

        private void closeCU(bool skip_c = false)
        {
            if (!Settings.Exit_Confrimation || skip_c || (MessageBox.Show("Are you sure you want to exit Chromium Updater?", "Chromium Updater", MessageBoxButtons.YesNo) == DialogResult.Yes))
            {
                if ((updaterWorker.isBusy) && (MessageBox.Show("You are currently in the process of updating. Are you sure you want to exit?", "Chromium Updater", MessageBoxButtons.YesNo) == DialogResult.No))
                {
                    return;
                }
                //
                Settings.Main_Window_Geometry = WindowGeometry.GeometryToString(this);
                //
                appMutex.WaitOne();
                appMutex.ReleaseMutex();
                //
                notifyIcon1.Dispose();
                updaterWorker.dispose();
                //
                Environment.Exit(0);
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.ShowInTaskbar = true;
            this.notifyIcon1.Visible = false;
            this.Show();
            this.BringToFront();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            closeCU();
        }

        #endregion

        #endregion

        #region SettingsForm

        private void button2_Click(object sender, EventArgs e)
        {
            if (!SettingsForm.isOpen)
            {
                settingsForm = new SettingsForm();
                settingsForm.FormClosed += new FormClosedEventHandler(settingsForm_FormClosed);
                settingsForm.Show();
            }
            else { settingsForm.BringToFront(); }
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button2_Click(this, null);
        }

        private void settingsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (timer1.Interval != (Settings.Check_Interval * 60000))
            {
                timer1.Interval = Settings.Check_Interval * 60000;
            }
            settingsForm.Dispose();
        }

        #endregion

        #region Main Button logic

        private void button1_Click(object sender, EventArgs e)
        {
            switch (button1.Text)
            {
                case "Check":
                    beginUpdate();
                    break;
                case "Download":
                    beginDownload();
                    break;
                case "Cancel":
                    updaterWorker.cancelDownload();
                    break;
                case "Install":
                case "Reinstall":
                    beginInstall();
                    break;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            beginUpdate();
        }

        #endregion

        #region Context Menu

        private void checkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            beginUpdate();
        }

        private void downloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            beginDownload();
        }

        private void installToolStripMenuItem_Click(object sender, EventArgs e)
        {
            beginInstall();
        }

        #endregion

        #region Edit Local Revision

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Click += MainForm_Click;
            this.groupBox1.Click += MainForm_Click;
            textBox1.Text = Settings.Current_Revision.ToString();
            textBox1.Visible = true;
            textBox1.Focus();
            textBox1.SelectionStart = textBox1.Text.Length;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter) { changeCurrentRevision(textBox1.Text.Trim()); }
        }

        private void MainForm_Click(object sender, EventArgs e)
        {
            this.Click -= MainForm_Click;
            this.groupBox1.Click -= MainForm_Click;
            textBox1.Text = "";
            textBox1.Visible = false;
        }

        private void changeCurrentRevision(string new_revision)
        {
            try
            {
                int rev = Convert.ToInt32(new_revision);
                this.Click -= MainForm_Click;
                this.groupBox1.Click -= MainForm_Click;
                Settings.Current_Revision = rev;
                label4.Text = new_revision;
                textBox1.Text = "";
                textBox1.Visible = false;
                if (rev < Convert.ToInt32(label6.Text)) { beginUpdate(); }
            }
            catch (FormatException) { MessageBox.Show("Only numbers are allowed"); return; }
        }

        #endregion

        #endregion

        #region Delegated functions

        #region Local/Remote Version

        private void changeLocalVersion(string local_version)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new changeValueD(changeLocalVersion), new object[] { local_version });
                return;
            }
            //
            label4.Text = local_version;
        }

        private void changeLatestVersion(string latest_version)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new changeValueD(changeLatestVersion), new object[] { latest_version });
                return;
            }
            //
            label6.Text = latest_version;
        }

        #endregion

        #region Action Button

        private void changeNextAction(string action)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new changeValueD(changeNextAction), new object[] { action });
                return;
            }
            //
            button1.Text = action;
            action = action.ToLower();
            if (action == "cancel") { button1.Enabled = true; }
            else { button1.Enabled = (action.Contains("ing")) ? false : true; }
            //
            if (action == "check")
            {
                button1.ContextMenuStrip = null;
                toolStripSeparator1.Visible = true;
                checkToolStripMenuItem1.Visible = true;
            }
            else if (action == "checking")
            {
                button1.ContextMenuStrip = null;
                toolStripSeparator1.Visible = false;
                checkToolStripMenuItem1.Visible = false;
                updateToolStripMenuItem.Visible = false;
                installToolStripMenuItem1.Visible = false;
            }
            else if (action == "download")
            {
                button1.ContextMenuStrip = contextMenuStrip2;
                installToolStripMenuItem1.Visible = false;
                installToolStripMenuItem.Visible = false;
                toolStripSeparator1.Visible = true;
                checkToolStripMenuItem1.Visible = true;
                checkToolStripMenuItem.Visible = true;
                downloadToolStripMenuItem.Visible = true;
                updateToolStripMenuItem.Visible = true;
            }
            else if (action == "cancel")
            {
                checkToolStripMenuItem.Visible = false;
                downloadToolStripMenuItem.Visible = false;
                updateToolStripMenuItem.Visible = false;
                button1.ContextMenuStrip = contextMenuStrip2;
                toolStripSeparator1.Visible = false;
                checkToolStripMenuItem1.Visible = false;
                updateToolStripMenuItem.Visible = false;
                installToolStripMenuItem1.Visible = false;
            }
            else if (action == "install")
            {
                button1.ContextMenuStrip = contextMenuStrip2;
                toolStripSeparator1.Visible = true;
                checkToolStripMenuItem1.Visible = true;
                checkToolStripMenuItem.Visible = true;
                downloadToolStripMenuItem.Visible = true;
                updateToolStripMenuItem.Visible = true;
                installToolStripMenuItem1.Visible = true;
                installToolStripMenuItem.Visible = true;
            }
            else if (action == "installing")
            {
                button1.ContextMenuStrip = null;
                toolStripSeparator1.Visible = false;
                checkToolStripMenuItem1.Visible = false;
                updateToolStripMenuItem.Visible = false;
                installToolStripMenuItem1.Visible = false;
            }
        }

        #endregion

        #region Status

        private void displayStatus(string text, int timeout = 5)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new displayStatusD(displayStatus), new object[] { text, timeout });
                return;
            }
            //
            if (!statusClear.Enabled)
            {
                statusClear.Enabled = false;
                statusLabel.Text = text;
                notifyIcon1.Text = text;
                if (timeout > 0)
                {
                    statusClear.Interval = timeout * 1000;
                    statusClear.Enabled = true;
                }
            }
            else { messagePump.Add(text); }
        }

        private void StatusLabelRefresh_Tick(object sender, EventArgs e)
        {
            statusLabel.Text = "";
            notifyIcon1.Text = "Chromium Updater";
            //
            if (messagePump.Count > 0)
            {
                System.Threading.Thread.Sleep(500);
                statusLabel.Text = messagePump[0];
                notifyIcon1.Text = messagePump[0];
                messagePump.Remove(messagePump[0]);
            }
            else { statusClear.Enabled = false; }
        }

        #endregion

        #endregion

        #region Update Processor

        #region Checker

        private void beginUpdate()
        {
            changeNextAction("Checking");
            displayStatus("", 0);
            //
            updaterWorker.checkForUpdateAsync();
        }

        private void updaterWorker_checkCompleted(CheckResult r)
        {
            switch (r)
            {
                case CheckResult.UpdateAvailable:
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
                    changeLatestVersion(updaterWorker.latestRevision.ToString());
                    changeNextAction("Download");
                    // TODO: Idle Downloading also
                    break;
                case CheckResult.NoUpdateAvailable:
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
                    changeLatestVersion(updaterWorker.latestRevision.ToString());
                    displayStatus("You have the latest version of Chromium");
                    changeNextAction("Check");
                    break;
                case CheckResult.InvalidURL:
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Error);
                    if (MessageBox.Show("The updater URLs are invalid. Would you like to correct it now?", "Chromium Updater", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        new SettingsForm().Show();
                    changeNextAction("Check");
                    break;
                case CheckResult.NoInternet:
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Error);
                    displayStatus("You are not connected to the Internet", 0);
                    changeNextAction("Check");
                    break;
            }
        }

        #endregion

        #region Downloader

        private void beginDownload()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new emptyD(beginDownload));
                return;
            }
            //
            changeNextAction("Cancel");
            toolStripProgressBar1.Visible = true;
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
            //
            updaterWorker.downloadUpdateAsync();
        }

        private void updaterWorker_progressChanged(DownloadProgressChangedEventArgs e)
        { 
            toolStripProgressBar1.Value = e.ProgressPercentage;
            toolStripStatusLabel1.Text = e.ProgressPercentage.ToString() + "%";
            if (TaskbarManager.IsPlatformSupported)
                TaskbarManager.Instance.SetProgressValue(e.ProgressPercentage, 100);
        }

        private void updaterWorker_fileDownloaded(DownloadResult r)
        {
            toolStripProgressBar1.Value = 0;
            toolStripProgressBar1.Visible = false;
            toolStripStatusLabel1.Text = "";
            //
            switch (r)
            {
                case DownloadResult.Success:
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Indeterminate);
                    displayStatus("Revision " + '"' + updaterWorker.latestRevision + '"' + "has been downloaded");
                    beginInstall();
                    break;
                case DownloadResult.Canceled:
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Paused);
                    displayStatus("Download canceled");
                    changeNextAction("Download");
                    break;
                case DownloadResult.InvalidURL:
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Error);
                    if (MessageBox.Show("The updater URLs are invalid. Would you like to correct it now?", "Chromium Updater", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        new SettingsForm().Show();
                    changeNextAction("Check");
                    break;
                case DownloadResult.NoInternet:
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Error);
                    displayStatus("You are not connected to the Internet", 0);
                    changeNextAction("Check");
                    break;
                case DownloadResult.FileInUse:
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Error);
                    MessageBox.Show("The file that we are downloading to is in use.");
                    changeNextAction("Check");
                    break;
            }
        }

        #endregion

        #region Installer

        private void beginInstall()
        {
            changeNextAction("Installing");
            //
            updaterWorker.installUpdateAsync();
        }

        private void updaterWorker_finished(InstallResult r)
        {
            switch (r)
            {
                case InstallResult.Success:
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
                    changeLocalVersion(updaterWorker.latestRevision.ToString());
                    displayStatus("Revision " + '"' + updaterWorker.latestRevision + '"' + " installed successfully");
                    changeNextAction("Check");
                    break;
                case InstallResult.Fail:
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Error);
                    MessageBox.Show("There was trouble running the update file");
                    changeNextAction("Install");
                    break;
                case InstallResult.ChromiumRunning:
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Paused);
                    displayStatus("Chromium is currently running");
                    changeNextAction("Install");
                    break;
            }
        }

        #endregion

        #endregion
    }
}
