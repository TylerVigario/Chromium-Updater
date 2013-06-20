using System;
using System.IO;
using System.Windows.Forms;

namespace Chromium_Updater
{
    class Settings
    {
        private static IniFile ini;

        public static void load()
        {
            string path = Application.StartupPath + "\\settings.ini";
            if (!File.Exists(path)) { File.Create(path); }
            //
            ini = new IniFile(path);
            //
            if (ini.GetInt32("Settings", "Update_3", 0) == 0)
            {
                Check_Interval = 60;
                ini.WriteValue("Settings", "Update_3", 1);
            }
        }

        public static void reset()
        {
            Latest_Revision_URL = "http://commondatastorage.googleapis.com/chromium-browser-continuous/Win/LAST_CHANGE";
            Specific_Revision_URL = "http://commondatastorage.googleapis.com/chromium-browser-continuous/Win/";
            Start_Hidden = false;
            Check_Interval = 60;
            Installer_Timeout = 120;
            Installer_Arguments = "";
            Download_Directory = "%CURRENT_DIRECTORY%/Downloads";
            CUpdater_Log_Path = "%CURRENT_DIRECTORY%/CUpdater.log";
            Show_Chromium_Close_Dialog = true;
        }

        #region Chromium Updater URLs

        #region Latest Revision URL

        private static string lru = null;

        public static string Latest_Revision_URL
        {
            get
            {
                if (lru == null)
                {
                    lru = ini.GetString("CUU", "Latest_Revision", null);
                    if (String.IsNullOrEmpty(lru))
                    {
                        lru = "http://commondatastorage.googleapis.com/chromium-browser-continuous/Win/LAST_CHANGE";
                    }
                }
                return lru;
            }
            set
            {
                lru = value;
                ini.WriteValue("CUU", "Latest_Revision", value);
            }
        }

        #endregion

        #region Specific Revision URL

        private static string sru;

        public static string Specific_Revision_URL
        {
            get
            {
                if (sru == null)
                {
                    sru = ini.GetString("CUU", "Specific_Revision", null);
                    if (String.IsNullOrEmpty(sru))
                    {
                        sru = "http://commondatastorage.googleapis.com/chromium-browser-continuous/Win/";
                    }
                }
                return sru;
            }
            set
            {
                sru = value;
                ini.WriteValue("CUU", "Specific_Revision", value);
            }
        }

        #endregion

        #endregion

        #region Start Hidden

        private static int sh = -1;

        public static bool Start_Hidden
        {
            get
            {
                if (sh < 0)
                {
                    sh = ini.GetInt32("Settings", "Start_Hidden", -1);
                    if (sh < 0)
                    {
                        sh = 0;
                    }
                }
                return Convert.ToBoolean(sh);
            }
            set
            {
                sh = Convert.ToInt32(value);
                ini.WriteValue("Settings", "Start_Hidden", sh);
            }
        }

        #endregion

        #region Check Interval

        private static int ci = -1;

        public static int Check_Interval
        {
            get
            {
                if (ci < 0)
                {
                    ci = ini.GetInt32("Settings", "Check_Interval", -1);
                    if (ci < 0)
                    {
                        ci = 60;
                    }
                }
                return ci;
            }
            set
            {
                ci = value;
                ini.WriteValue("Settings", "Check_Interval", value);
            }
        }

        #endregion

        #region Current Revision

        private static int cr = -1;

        public static int Current_Revision
        {
            get
            {
                if (cr == -1)
                {
                    /*object version = Registry.CurrentUser.GetValue(@"Software\Chromium\Version", null);
                    if (version != null)
                    {
                        try
                        {
                            string r = new WebClient().DownloadString("http://omahaproxy.appspot.com/revision?version=" + version.ToString());
                            cr = Convert.ToInt32(r);
                        }
                        catch (Exception) { cr = -1; }
                    }
                    if (cr == -1)
                    {*/
                        cr = ini.GetInt32("Settings", "Current_Revision", -1);
                        if (cr == -1)
                        {
                            cr = 0;
                        }
                    //}
                }
                return cr;
            }
            set
            {
                cr = value;
                ini.WriteValue("Settings", "Current_Revision", value);
            }
        }

        #endregion

        #region Installer Timeout

        private static int it = -1;

        public static int Installer_Timeout
        {
            get
            {
                if (it < 0)
                {
                    it = ini.GetInt32("Settings", "Installer_Timeout", -1);
                    if (it < 0)
                    {
                        it = 120;
                    }
                }
                return it;
            }
            set
            {
                it = value;
                ini.WriteValue("Settings", "Installer_Timeout", value);
            }
        }

        #endregion

        #region Installer Arguments

        private static string ia = null;

        public static string Installer_Arguments
        {
            get
            {
                if (ia == null)
                {
                    ia = ini.GetString("Settings", "Installer_Arguments", "");
                    /*if (String.IsNullOrEmpty(ia))
                    {
                        ia = "";
                    }*/
                }
                return ia;
            }
            set
            {
                ia = value;
                ini.WriteValue("Settings", "Installer_Arguments", value);
            }
        }

        #endregion

        #region Download Directory

        private static string dd;

        public static string Download_Directory
        {
            get
            {
                if (String.IsNullOrEmpty(dd))
                {
                    dd = ini.GetString("Settings", "Download_Directory", null);
                    if (String.IsNullOrEmpty(dd))
                    {
                        dd = "%CURRENT_DIR%/Downloads";
                    }
                    dd = filePathConvert(dd);
                }
                return dd;
            }
            set
            {
                dd = filePathConvert(value);
                ini.WriteValue("Settings", "Download_Directory", value);
            }
        }

        #endregion

        #region CUpdater Log Path

        private static string culp;

        public static string CUpdater_Log_Path
        {
            get
            {
                if (String.IsNullOrEmpty(culp))
                {
                    culp = ini.GetString("Settings", "CUpdater_Log_Path", null);
                    if (String.IsNullOrEmpty(culp))
                    {
                        culp = "%CURRENT_DIR%/CUpdater.log";
                    }
                    culp = filePathConvert(culp);
                }
                return culp;
            }
            set
            {
                culp = filePathConvert(value);
                ini.WriteValue("Settings", "CUpdater_Log_Path", value);
            }
        }

        #endregion

        #region Main_Window_Geometry

        public static string Main_Window_Geometry
        {
            get
            {
                return ini.GetString("Settings", "Main_Window_Geometry", "");
            }
            set
            {
                ini.WriteValue("Settings", "Main_Window_Geometry", value);
            }
        }

        #endregion

        #region Settings_Window_Geometry

        public static string Settings_Window_Geometry
        {
            get
            {
                return ini.GetString("Settings", "Settings_Window_Geometry", "");
            }
            set
            {
                ini.WriteValue("Settings", "Settings_Window_Geometry", value);
            }
        }

        #endregion

        #region Exit_Confrimation

        private static int ec = -1;

        public static bool Exit_Confrimation
        {
            get
            {
                if (ec < 0)
                {
                    ec = ini.GetInt32("Settings", "Exit_Confrimation", -1);
                    if (ec < 0) { ec = 1; }
                }
                return Convert.ToBoolean(ec);
            }
            set
            {
                ec = Convert.ToInt32(value);
                ini.WriteValue("Settings", "Exit_Confrimation", ec);
            }
        }

        #endregion

        #region Show Chromium Close Dialog

        private static int sccd = -1;

        public static bool Show_Chromium_Close_Dialog
        {
            get
            {
                if (sccd < 0)
                {
                    sccd = ini.GetInt32("Settings", "Show_Chromium_Close_Dialog", -1);
                    if (sccd < 0)
                    {
                        sccd = 1;
                    }
                }
                return Convert.ToBoolean(sccd);
            }
            set
            {
                sccd = Convert.ToInt32(value);
                ini.WriteValue("Settings", "Show_Chromium_Close_Dialog", sccd);
            }
        }

        #endregion

        #region Misc Functions

        private static string filePathConvert(string filePath)
        {
            filePath = filePath.ToUpper();
            filePath = filePath.Replace("%CUR_DIR%", Application.StartupPath);
            filePath = filePath.Replace("%CURRENT_DIR%", Application.StartupPath);
            filePath = filePath.Replace("%CURRENT_DIRECTORY%", Application.StartupPath);
            return filePath;
        }

        #endregion
    }
}
