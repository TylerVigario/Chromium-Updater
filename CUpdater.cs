using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace Chromium_Updater
{
    public delegate void updateCheck(CheckResult r);
    public delegate void downloadProgressChanged(DownloadProgressChangedEventArgs e);
    public delegate void Download(DownloadResult r);
    public delegate void Install(InstallResult r);

    class CUpdater
    {
        private int latest_revision = 0;
        //
        private WebClient downloader;
        //
        public event updateCheck checkReturn;
        public event downloadProgressChanged progressChanged;
        public event Download downloadReturn;
        public event Install installReturn;

        public CUpdater()
        {
            downloader = new WebClient();
            downloader.DownloadProgressChanged += new DownloadProgressChangedEventHandler(downloader_DownloadProgressChanged);
            downloader.DownloadFileCompleted += new AsyncCompletedEventHandler(downloader_DownloadFileCompleted);
        }

        public void dispose()
        {
            downloader.Dispose();
        }

        #region Update Checking

        public void checkForUpdateAsync()
        {
            new Thread(new ThreadStart(checkForUpdateWorker)).Start();
        }

        private void checkForUpdateWorker()
        {
            try
            {
                latest_revision = Convert.ToInt32(downloader.DownloadString(Settings.Latest_Revision_URL));
                if (latest_revision > Settings.Current_Revision) { checkReturn(CheckResult.UpdateAvailable); return; }
                else { checkReturn(CheckResult.NoUpdateAvailable); return; }
            }
            catch (ArgumentNullException) { checkReturn(CheckResult.InvalidURL); return; }
            catch (FormatException) { checkReturn(CheckResult.InvalidURL); return; }
            catch (WebException e)
            {
                if (!isConnectedToInternet()) { checkReturn(CheckResult.NoInternet); return; }
                else { throw (e); }
            }
        }

        public int latestRevision
        {
            get { return latest_revision; }
        }

        #endregion

        #region Downloading

        public void downloadUpdateAsync()
        {
            if (!Directory.Exists(Settings.Download_Directory)) { Directory.CreateDirectory(Settings.Download_Directory); }
            //
            try
            {
                downloader.DownloadFileAsync(new Uri(Settings.Specific_Revision_URL + latest_revision + "/mini_installer.exe"), Settings.Download_Directory + "\\mini_installer.exe");
            }
            catch (ArgumentNullException) { downloadReturn(DownloadResult.InvalidURL); return; }
            catch (WebException e)
            {
                if (!isConnectedToInternet()) { checkReturn(CheckResult.NoInternet); return; }
                else { throw (e); }
            }
            catch (InvalidOperationException) { downloadReturn(DownloadResult.FileInUse); return; }
        }

        public void cancelDownload()
        {
            downloader.CancelAsync();
        }

        private void downloader_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressChanged(e);
        }

        private void downloader_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                downloadReturn(DownloadResult.Canceled);
            }
            else if (e.Error != null)
            {
                if (!isConnectedToInternet()) { downloadReturn(DownloadResult.NoInternet); }
                else { throw (e.Error); }
            }
            else
            {
                downloadReturn(DownloadResult.Success);
            }
        }

        #endregion

        #region Installing

        public void installUpdateAsync()
        {
            new Thread(new ThreadStart(chromiumChecker)).Start();
        }

        void close_form_CloseReturn(bool closed)
        {
            if (closed) { new Thread(new ThreadStart(installerWatcher)).Start(); }
            else { installReturn(InstallResult.ChromiumRunning); }
        }

        private void chromiumChecker()
        {
            if (CloseChromiumForm.isChromiumRunning)
            {
                if (Settings.Show_Chromium_Close_Dialog)
                {
                    CloseChromiumForm close_form = new CloseChromiumForm();
                    close_form.CloseReturn += close_form_CloseReturn;
                    close_form.ShowDialog();
                    close_form.Dispose();
                }
                else
                {
                    if (CloseChromiumForm.closeChromium()) { installerWatcher(); }
                    else { installReturn(InstallResult.ChromiumRunning); }
                }
            }
            else { installerWatcher(); }
        }

        private void installerWatcher()
        {
            try
            {
                Process.Start(Settings.Download_Directory + "\\mini_installer.exe", Settings.Installer_Arguments).WaitForExit(Settings.Installer_Timeout * 1000);
            }
            catch (Win32Exception)
            {
                installReturn(InstallResult.Fail); return;
            }
            //
            Settings.Current_Revision = latest_revision;
            installReturn(InstallResult.Success);
        }

        #endregion

        #region Misc functions

        public bool isBusy
        {
            get { return downloader.IsBusy; }
        }

        public static bool isConnectedToInternet(string url = "www.google.com")
        {
            try
            {
                IPAddress[] addresslist = Dns.GetHostAddresses(url);
                //
                if (addresslist[0].ToString().Length > 6)
                {
                    return true;
                }
                else { return false; }
            }
            catch { return false; }
        }

        #endregion
    }

    #region Custom Returns

    public enum CheckResult
    {
        UpdateAvailable,
        NoUpdateAvailable,
        InvalidURL,
        NoInternet
    }

    public enum DownloadResult
    {
        Canceled,
        Success,
        InvalidURL,
        NoInternet,
        FileInUse
    }

    public enum InstallResult
    {
        Success,
        Fail,
        ChromiumRunning
    }

    #endregion
}
