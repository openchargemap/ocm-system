using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OCM.Import;
using OCM.Import.Providers;
using OCM.API.Client;
using System.Threading.Tasks;

namespace OCM.Import.Manager.Console
{
    class Program
    {
        static string LogFile = "import.log";
        static bool EnableLogging = false;

        enum ActionType
        {
            DataImport,
            NetworkServices,
            ClientTest
        }

        static void LogEvent(string message)
        {
            LogEvent(message, false);
        }

        static void LogEvent(string message, bool createFile)
        {
            if (!EnableLogging)
            {
                System.Console.WriteLine(message);
            }
            else
            {

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
        }

        static void Main(string[] args)
        {
            bool isAutomaticMode = true;
            bool isAPIImportMode = false;

            string importFolder = "";
            string OCM_API_Identifier = null;
            string OCM_API_SessionToken = null;

            LogEvent("Starting Import:", true);
            if (args.Length > 0)
            {
                LogEvent("Arguments supplied: ");

                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    LogEvent("arg: " + arg);
                    if (arg == "-auto") isAutomaticMode = true;
                    if (arg == "-api") isAPIImportMode = true;
                    //if (arg == "-log") EnableLogging = true;

                    try
                    {
                        if (arg == "-api_identifier")
                        {
                            OCM_API_Identifier = args[i + 1];
                        }

                        if (arg == "-api_sessiontoken")
                        {
                            OCM_API_SessionToken = args[i + 1];
                        }
                    }
                    catch (Exception) { LogEvent("Invalid parameter supplied."); }
                }
            }
            else
            {
                LogEvent("No Arguments supplied.");
            }

            if (isAutomaticMode)
            {
                bool actionPerformed = false;

                ActionType mode = ActionType.ClientTest;

                if (mode == ActionType.DataImport && isAPIImportMode == true && OCM_API_Identifier != null && OCM_API_SessionToken != null)
                {
                    ExportType exportType = ExportType.API;

                    ImportManager importManager = new ImportManager(new ImportSettings { TempFolderPath = importFolder, MasterAPIBaseUrl = "https://api.openchargemap.io/v3" });
                    LogEvent("Performing Import, Publishing via API (" + OCM_API_Identifier + ":" + OCM_API_SessionToken + "): " + DateTime.UtcNow.ToShortTimeString());
                    Task<bool> processing = importManager.PerformImportProcessing(exportType, importFolder, OCM_API_Identifier, OCM_API_SessionToken, true);
                    processing.Wait();
                    LogEvent("Import Processed. Exiting. " + DateTime.UtcNow.ToShortTimeString());

                    actionPerformed = true;
                }

                if (mode == ActionType.NetworkServices)
                {
                    //network service polling
                    //OCM.API.NetworkServices.ServiceManager serviceManager = new OCM.API.NetworkServices.ServiceManager();
                    //serviceManager.Test(OCM.API.NetworkServices.ServiceProvider.CoulombChargePoint);
                }

#if DEBUG
                if (mode ==ActionType.ClientTest)
                {
                    OCMClient client = new OCMClient();
                    client.APITestTiming();
                    actionPerformed = true;
                }
#endif

                if (!actionPerformed)
                {
                    LogEvent("Nothing to do. Exiting. " + DateTime.UtcNow.ToShortTimeString());
                }
            }

        }
    }
}
