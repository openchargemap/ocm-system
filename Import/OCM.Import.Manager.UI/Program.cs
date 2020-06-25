using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Import;
using OCM.Import.Manager.UI.Properties;
using OCM.Import.Providers;


namespace OCM.Import.Manager.UI
{
    class Program
    {
        static string LogFile = "import.log";
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
            else
            {
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

                    var settings = new ImportSettings
                    {
                        GeolocationShapefilePath = ConfigurationManager.AppSettings["GeolocationShapefilePath"],
                        ImportUserAPIKey = ConfigurationManager.AppSettings["APIKey"],
                        MasterAPIBaseUrl = ConfigurationManager.AppSettings["APIBaseUrl"],
                        TempFolderPath = Settings.Default.Import_DataFolder
                    };

                    ImportManager importManager = new ImportManager(settings);
                    LogEvent("Performing Import, Publishing via API: " + DateTime.UtcNow.ToShortTimeString());
                    importManager.PerformImportProcessing(new ImportProcessSettings
                    {
                        ExportType = exportType,
                        DefaultDataPath = Settings.Default.Import_DataFolder,
                        ApiIdentifier = Settings.Default.OCM_API_Identitifer,
                        ApiSessionToken = Settings.Default.OCM_API_SessionToken
                    });

                    LogEvent("Import Processed. Exiting. " + DateTime.UtcNow.ToShortTimeString());

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
                    LogEvent("Nothing to do. Exiting. " + DateTime.UtcNow.ToShortTimeString());
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
