using System;
using System.Windows.Forms;
using System.Threading;
using CrashReporterDotNET;

namespace Chromium_Updater
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.ThreadException += ApplicationThreadException;
            //
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args));
        }

        static void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
        {
            var reportCrash = new ReportCrash
            {
                FromEmail = "TylerVigario90@gmail.com",
                ToEmail = "logicpwn.crashes@gmail.com",
                SMTPHost = "smtp.gmail.com",
                Port = 587,
                UserName = "logicpwn.crashes@gmail.com",
                Password = "pleasedontcrash",
                EnableSSL = true
            };
            reportCrash.Send(e.Exception);
        }
    }
}
