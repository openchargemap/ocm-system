using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Import;
using OCM.Import.Providers;
using Import.Properties;

namespace OCM.Import.Manager.UI
{
    class Program
    {
        static string LogFile="import.log";
        static bool EnableLogging = true;

        enum ActionType
        {
            DataImport,
            NetworkServices
        }

        static void LogEvent(string message)
        {
            LogEvent(message, false);
        }

        static void LogEvent(string message, bool createFile)
        {
            if (!EnableLogging) return;

            message += "\r\n";
            if (createFile)
            {
                System.IO.File.WriteAllText(LogFile, message);
            }
            else {
                System.IO.File.AppendAllText(LogFile, message);
            }
           
        }

        static void Main(string[] args)
        {
            bool isAutomaticMode = false;
            bool isAPIImportMode = false;

            LogEvent("Starting Import:", true);
            if (args.Length > 0)
            {
                LogEvent("Arguments supplied: ");

                foreach (var arg in args)
                {
                    LogEvent("arg: " + arg);
                    if (arg == "-auto") isAutomaticMode = true;
                    if (arg == "-api") isAPIImportMode = true;
                }
            }
            else
            {
                LogEvent("No Arguments supplied.");
            }
  
            if (isAutomaticMode)
            {
                bool actionPerformed = false;

                ActionType mode = ActionType.DataImport;

                if (mode == ActionType.DataImport && isAPIImportMode == true)
                {
                    ExportType exportType = ExportType.API;

                    ImportManager importManager = new ImportManager();
                    LogEvent("Performing Import, Publishing via API: "+DateTime.Now.ToShortTimeString());
                    importManager.PerformImportProcessing(exportType, Settings.Default.Import_DataFolder, Settings.Default.OCM_API_Identitifer, Settings.Default.OCM_API_SessionToken, true);
                    LogEvent("Import Processed. Exiting. " + DateTime.Now.ToShortTimeString());

                    actionPerformed = true;
                }

                if (mode == ActionType.NetworkServices)
                {
                    //network service polling
                    //OCM.API.NetworkServices.ServiceManager serviceManager = new OCM.API.NetworkServices.ServiceManager();
                    //serviceManager.Test(OCM.API.NetworkServices.ServiceProvider.CoulombChargePoint);
                }

                if (!actionPerformed)
                {
                    LogEvent("Nothing to do. Exiting. " + DateTime.Now.ToShortTimeString());
                }
            }
            else
            {
                //show UI
                Application.Run(new AppMain());
            }
        }
    }
}
