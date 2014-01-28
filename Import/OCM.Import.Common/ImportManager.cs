using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OCM.API.Client;
using OCM.API.Common.Model;
using OCM.Import.Providers;
using OCM.Import.Misc;
using Newtonsoft.Json;

namespace OCM.Import
{
    public class ImportManager
    {
        public bool ImportUpdatesOnly { get; set; }
        public string GeonamesAPIUserName { get; set; }
        /**
         * Generic Import Process

            Provider Properties
                Import Method
                Import URL/Path
                Import Frequency
                IsMaster

            Fetch Latest Data

            For each item
                Check If Exists or Strong Duplicate, Get ID
                If New, Add
                if Exists Then
                    Prepare update, if provider supports live status, set that
                        What if item updated manually on OCM?
                    Send Update
                End 
            Loop

            Log Exceptions
            Log Count of Items Added or Modified

            Way to remove item (or log items) which no longer exist in data source?
         * */

        public List<ChargePoint> DeDuplicateList(List<ChargePoint> cpList, bool updateDuplicate, CoreReferenceData coreRefData)
        {
            //get list of possible duplicates
            SearchFilters filters = new SearchFilters { MaxResults = 1000000, EnableCaching = false };
            List<ChargePoint> masterList = new OCMClient().GetLocations(filters); //new OCMClient().FindSimilar(null, 10000); //fetch all charge points regardless of status
            List<ChargePoint> duplicateList = new List<ChargePoint>();
            List<ChargePoint> updateList = new List<ChargePoint>();

            ChargePoint previousCP = null;

            foreach (var item in cpList)
            {
                //TODO: find better deduplication algorithm
                var dupeList = masterList.Where(c =>
                        (c.DataProvider != null && c.DataProvider.ID == item.DataProvider.ID && c.DataProvidersReference == item.DataProvidersReference)
                        ||
                        (c.AddressInfo != null && (
                            c.AddressInfo.ToString()==item.AddressInfo.ToString() || //same address
                                c.AddressInfo.AddressLine1 == item.AddressInfo.AddressLine1 || //same first address line
                                ( //same postcode
                                    !String.IsNullOrEmpty(c.AddressInfo.Postcode)
                                    &&
                                    !String.IsNullOrEmpty(item.AddressInfo.Postcode)
                                    &&
                                    c.AddressInfo.Postcode == item.AddressInfo.Postcode
                                )
                            )
                        )
                        ||
                        ( //very similar lat/lon
                        c.AddressInfo != null &&
                            (Math.Round(c.AddressInfo.Latitude, 4) == Math.Round(item.AddressInfo.Latitude, 4))
                            &&
                            (Math.Round(c.AddressInfo.Longitude, 4) == Math.Round(item.AddressInfo.Longitude, 4))
                        )
                );

                if (dupeList.Count() > 0)
                {
                    if (updateDuplicate)
                    {
                        //if updating duplicates, get exact matching duplicate based on provider reference and update/merge with this item to update status/merge properties
                        var updatedItem = dupeList.FirstOrDefault(d => d.DataProvider.ID == item.DataProvider.ID && d.DataProvidersReference == item.DataProvidersReference);
                        if (updatedItem != null)
                        {
                            //only merge/update from live published items
                            if (updatedItem.SubmissionStatus.IsLive == (bool?)true)
                            {
                                //item is an exact match from same data provider
                                //overwrite existing with imported data (use import as master)
                                MergeItemChanges(item, updatedItem, false);

                                updateList.Add(updatedItem);
                            }
                        }

                        if (updatedItem == null)
                        {
                            //duplicates are not exact match
                            //TODO: resolve whether imported data should change duplicate

                            //merge new properties from imported item
                            //if (item.StatusType != null) updatedItem.StatusType = item.StatusType;
                            //updateList.Add(updatedItem);
                        }
                    }
                    duplicateList.Add(item);
                }


                //mark item as duplicate if location/title exactly matches previous entry
                if (previousCP != null)
                {
                    if (IsDuplicateLocation(item, previousCP, true))
                    {
                        if (!duplicateList.Contains(item))
                        {
                            System.Diagnostics.Debug.WriteLine("Duplicated item removed:" + item.AddressInfo.Title);
                            duplicateList.Add(item);
                        }
                    }
                }
                previousCP = item;
            }

            //remove duplicates from list to apply
            foreach (var dupe in duplicateList)
            {
                cpList.Remove(dupe);
            }

            //add updated items (replace duplicates with property changes)
            foreach (var updatedItem in updateList)
            {
                if (!cpList.Contains(updatedItem))
                {
                    cpList.Add(updatedItem);
                }
            }

            //populate missing location info from geolocation cache if possible
            PopulateLocationFromGeolocationCache(cpList, coreRefData);

            //final pass to catch duplicates present in data source, mark additional items as Delisted Duplicate so we have a record for them
            var submissionStatusDelistedDupe = coreRefData.SubmissionStatusTypes.First(s => s.ID == 1001); //delisted duplicate
            previousCP = null;

            foreach (var cp in cpList)
            {
                if (previousCP != null)
                {
                    if (IsDuplicateLocation(cp, previousCP, false))
                    {
                        cp.SubmissionStatus = submissionStatusDelistedDupe;
                    }
                }
                previousCP = cp;
            }

            //return final processed list ready for applying as insert/updates
            return cpList;
        }

        /// <summary>
        /// Determine if 2 CPs have the same location details
        /// </summary>
        /// <param name="current"></param>
        /// <param name="previous"></param>
        /// <returns></returns>
        public bool IsDuplicateLocation(ChargePoint current, ChargePoint previous, bool compareTitle)
        {
            if (
                (compareTitle && (previous.AddressInfo.Title == current.AddressInfo.Title) || compareTitle == false)
                && previous.AddressInfo.AddressLine1 == current.AddressInfo.AddressLine1
                && previous.AddressInfo.Latitude == current.AddressInfo.Latitude
                && previous.AddressInfo.Longitude == current.AddressInfo.Longitude
                || (current.DataProvidersReference != null && current.DataProvidersReference.Length > 0 && previous.DataProvidersReference == current.DataProvidersReference)
                )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool MergeItemChanges(ChargePoint sourceItem, ChargePoint destItem, bool statusOnly)
        {
            bool hasDifferences = true;
            //var diffs = GetDifferingProperties(sourceItem, destItem);

            //merge changes in sourceItem into destItem, preserving destItem ID's 
            if (!statusOnly)
            {

                //update addressinfo
                destItem.AddressInfo.Title = sourceItem.AddressInfo.Title;
                destItem.AddressInfo.AddressLine1 = sourceItem.AddressInfo.AddressLine1;
                destItem.AddressInfo.AddressLine2 = sourceItem.AddressInfo.AddressLine2;
                destItem.AddressInfo.Town = sourceItem.AddressInfo.Town;
                destItem.AddressInfo.StateOrProvince = sourceItem.AddressInfo.StateOrProvince;
                destItem.AddressInfo.Postcode = sourceItem.AddressInfo.Postcode;
                destItem.AddressInfo.RelatedURL = sourceItem.AddressInfo.RelatedURL;
                if (sourceItem.AddressInfo.Country != null) destItem.AddressInfo.Country = sourceItem.AddressInfo.Country;
                destItem.AddressInfo.AccessComments = sourceItem.AddressInfo.AccessComments;
                destItem.AddressInfo.ContactEmail = sourceItem.AddressInfo.ContactEmail;
                destItem.AddressInfo.ContactTelephone1 = sourceItem.AddressInfo.ContactTelephone1;
                destItem.AddressInfo.ContactTelephone2 = sourceItem.AddressInfo.ContactTelephone2;
#pragma warning disable 0612
                destItem.AddressInfo.GeneralComments = sourceItem.AddressInfo.GeneralComments;
#pragma warning restore 0612
                destItem.AddressInfo.Latitude = sourceItem.AddressInfo.Latitude;
                destItem.AddressInfo.Longitude = sourceItem.AddressInfo.Longitude;

                //update general
                destItem.DataProvider = sourceItem.DataProvider;
                destItem.DataProvidersReference = sourceItem.DataProvidersReference;
                destItem.OperatorInfo = sourceItem.OperatorInfo;
                destItem.OperatorsReference = sourceItem.OperatorsReference;
                destItem.UsageType = sourceItem.UsageType;
                destItem.UsageCost = sourceItem.UsageCost;
                destItem.NumberOfPoints = sourceItem.NumberOfPoints;
                destItem.GeneralComments = sourceItem.GeneralComments;
                destItem.DateLastConfirmed = sourceItem.DateLastConfirmed;
                destItem.DateLastStatusUpdate = sourceItem.DateLastStatusUpdate;
                destItem.DatePlanned = sourceItem.DatePlanned;

                //update connections
                //TODO:: update connections
                var connDeleteList = new List<ConnectionInfo>();
                if (sourceItem.Connections != null)
                {
                    if (destItem.Connections == null)
                    {
                        destItem.Connections = sourceItem.Connections;
                    }
                    else
                    {
                        foreach (var conn in sourceItem.Connections)
                        {
                            var existingConnection = destItem.Connections.FirstOrDefault(d => (conn.ConnectionType != null && d.ConnectionType.ID == conn.ConnectionType.ID) || (d.Reference == conn.Reference && !String.IsNullOrEmpty(conn.Reference)));
                            if (existingConnection != null)
                            {
                                //update existing
                                if (conn.ConnectionType != null) existingConnection.ConnectionType = conn.ConnectionType;
                                existingConnection.Quantity = conn.Quantity;
                                existingConnection.Level = conn.Level;
                                existingConnection.Reference = conn.Reference;
                                existingConnection.StatusType = conn.StatusType;
                                existingConnection.Voltage = conn.Voltage;
                                existingConnection.Amps = conn.Amps;
                                existingConnection.PowerKW = conn.PowerKW;
                                existingConnection.Comments = conn.Comments;
                                if (conn.CurrentType != null) existingConnection.CurrentType = conn.CurrentType;

                            }
                            else
                            {
                                //add new
                                destItem.Connections.Add(conn);
                            }
                        }
                    }
                }
            }
            if (sourceItem.StatusType != null) destItem.StatusType = sourceItem.StatusType;

            return hasDifferences;
        }

        public APICredentials GetAPISessionCredentials(string identifier, string sessionToken)
        {
            return new APICredentials { Identifier = identifier, SessionToken = sessionToken };
        }

        public void PerformImportProcessing(ExportType exportType, string defaultDataPath, string apiIdentifier, string apiSessionToken, bool fetchLiveData)
        {
            OCMClient client = new OCMClient();
            var credentials = GetAPISessionCredentials(apiIdentifier, apiSessionToken);

            CoreReferenceData coreRefData = client.GetCoreReferenceData();

            string outputPath = "Data\\";
            List<IImportProvider> providers = new List<IImportProvider>();

            string inputDataPathPrefix = defaultDataPath;

            // providers.Add(new ImportProvider_RWEMobility() { InputPath = inputDataPathPrefix + "rwe-mobility\\data.json.txt" });

            //Working - Auto refreshed
            providers.Add(new ImportProvider_BlinkNetwork() { InputPath = inputDataPathPrefix + "blinknetwork.com\\jsondata2.txt" });
            providers.Add(new ImportProvider_CarStations() { InputPath = inputDataPathPrefix + "carstations.com\\jsonresults.txt" });
            providers.Add(new ImportProvider_RWEMobility() { InputPath = inputDataPathPrefix + "rwe-mobility\\data.json.txt" });
            providers.Add(new ImportProvider_UKChargePointRegistry() { InputPath = inputDataPathPrefix + "chargepoints.dft.gov.uk\\data.json" });
            providers.Add(new ImportProvider_Mobie() { InputPath = inputDataPathPrefix + "mobie.pt\\data.json.txt" });
            providers.Add(new ImportProvider_AFDC() { InputPath = inputDataPathPrefix + "afdc\\data.json" });
            providers.Add(new ImportProvider_ESB_eCars() { InputPath = inputDataPathPrefix + "esb_ecars\\data.kml" });

            //Working -manual refresh
            //providers.Add(new ImportProvider_NobilDotNo() { InputPath = inputDataPathPrefix + "nobil\\nobil.json.txt" });

            //Dev
            //providers.Add(new ImportProvider_CoulombChargepoint() { InputPath = inputDataPathPrefix + "coulomb\\data.json.txt" });
            //providers.Add(new ImportProvider_ChademoGroup() { InputPath = inputDataPathPrefix + "chademo\\chademo_jp.kml", ImportType= ChademoImportType.Japan, DefaultCountryID=114});

            //obsolete
            //providers.Add(new ImportProvider_PODPoint() { InputPath = inputDataPathPrefix + "pod-point.com\\export.htm" });
            //providers.Add(new ImportProvider_ChargeYourCar() { InputPath = inputDataPathPrefix + "chargeyourcar.org.uk\\data.htm" });


            foreach (var provider in providers)
            {
                var p = ((BaseImportProvider)provider);
                p.ExportType = exportType;
                try
                {
                    bool loadOK = false;


                    if (fetchLiveData && p.IsAutoRefreshed && !String.IsNullOrEmpty(p.AutoRefreshURL))
                    {
                        p.Log("Loading input data from URL..");
                        loadOK = p.LoadInputFromURL(p.AutoRefreshURL);
                    }
                    else
                    {
                        p.Log("Loading input data from file..");
                        loadOK = p.LoadInputFromFile(p.InputPath);
                    }

                    if (!loadOK)
                    {
                        //failed to load
                        p.Log("Failed to load input data.");
                        throw new Exception("Failed to fetch input data");
                    }
                    List<ChargePoint> duplicatesList = new List<ChargePoint>();

                    p.Log("Processing input..");

                    var list = provider.Process(coreRefData);
                    int numAdded = 0;
                    int numUpdated = 0;

                    if (list.Count > 0)
                    {
                        p.Log("De-Deuplicating list (" + p.ProviderName + ":: " + list.Count + " Items)..");

                        //de-duplicate and clean list based on existing data
                        //TODO: take original and replace in final update list, setting relevant updated properties (merge) and status
                        var finalList = DeDuplicateList(list, true, coreRefData);
                        //var finalList = list;

                        if (ImportUpdatesOnly)
                        {
                            finalList = finalList.Where(l => l.ID > 0).ToList();
                        }
                        //finalList = client.GetLocations(new SearchFilters { MaxResults = 10000 });

                        //export/apply updates
                        if (p.ExportType == ExportType.XML)
                        {
                            p.Log("Exporting XML..");

                            //output xml
                            p.ExportXMLFile(finalList, outputPath + p.OutputNamePrefix + ".xml");
                        }

                        if (p.ExportType == ExportType.CSV)
                        {
                            p.Log("Exporting CSV..");
                            //output csv
                            p.ExportCSVFile(finalList, outputPath + p.OutputNamePrefix + ".csv");
                        }

                        if (p.ExportType == ExportType.JSON)
                        {
                            p.Log("Exporting JSON..");
                            //output json
                            p.ExportJSONFile(finalList, outputPath + p.OutputNamePrefix + ".json");
                        }
                        if (p.ExportType == ExportType.API && p.IsProductionReady)
                        {

                            //publish list of locations to OCM via API
                            OCMClient ocmClient = new OCMClient();
                            p.Log("Publishing via API..");
                            foreach (ChargePoint cp in finalList.Where(l => l.AddressInfo.Country != null))
                            {

                                ocmClient.UpdateItem(cp, credentials);
                                if (cp.ID == 0)
                                {
                                    numAdded++;
                                }
                                else
                                {
                                    numUpdated++;
                                }
                            }
                        }
                    }

                    p.Log("Import Processed:" + provider.GetProviderName() + " Added:" + numAdded + " Updated:" + numUpdated);
                }
                catch (Exception exp)
                {
                    p.Log("Import Failed:" + provider.GetProviderName() + " ::" + exp.ToString());
                }
            }
        }

        /// <summary>
        /// For a list of ChargePoint objects, attempt to populate the AddressInfo (at least country) based on lat/lon if not already populated
        /// </summary>
        /// <param name="itemList"></param>
        /// <param name="coreRefData"></param>
        public void PopulateLocationFromGeolocationCache(List<ChargePoint> itemList, CoreReferenceData coreRefData)
        {
            GeolocationCacheManager geolocationCacheManager = new GeolocationCacheManager();
            geolocationCacheManager.GeonamesAPIUserName = GeonamesAPIUserName;
            geolocationCacheManager.LoadCache();

            //process list of locations, populating country refreshing cache where required
            foreach (var item in itemList)
            {
                if (item.AddressInfo.Country == null && item.AddressInfo.Latitude != null && item.AddressInfo.Longitude != null)
                {
                    var geoLookup = geolocationCacheManager.PerformLocationLookup((double)item.AddressInfo.Latitude, (double)item.AddressInfo.Longitude, coreRefData.Countries);
                    if (geoLookup != null)
                    {
                        var country = coreRefData.Countries.FirstOrDefault(c => c.ID == geoLookup.CountryID || c.ISOCode == geoLookup.CountryCode || c.Title == geoLookup.CountryName);
                        if (country != null)
                        {
                            item.AddressInfo.Country = country;

                            //remove country name from address line 1 if present
                            if (item.AddressInfo.AddressLine1.ToLower().Contains(country.Title.ToLower()))
                            {
                                item.AddressInfo.AddressLine1 = item.AddressInfo.AddressLine1.Replace(country.Title, "").Trim();
                            }
                        }
                    }
                }

                if (item.AddressInfo.Country == null)
                {
                    LogHelper.Log("Failed to resolve country for item:" + item.AddressInfo.Title);
                }
            }

            //cache may have updates, save for next time
            geolocationCacheManager.SaveCache();
        }

        public void GeocodingTest()
        {
            OCMClient client = new OCMClient();

            //get a few OCM listings
            SearchFilters filters = new SearchFilters { StatusTypeIDs = new int[] { (int)StandardSubmissionStatusTypes.Submitted_Published }, CountryIDs = new int[] { 1 }, DataProviderIDs = new int[] { 1 }, MaxResults = 2000, EnableCaching = false };

            var poiList = client.GetLocations(filters);
            /*
            GeocodingService g = new GeocodingService();
            List<GeolocationResult> list = new List<GeolocationResult>();

            //attempt OSM geocoding
            foreach (var poi in poiList)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("OCM-" + poi.ID + " : [" + poi.AddressInfo.Title + "] " + poi.AddressInfo.ToString());

                    System.Diagnostics.Debug.WriteLine("OCM : LL: " + poi.AddressInfo.Latitude + "," + poi.AddressInfo.Longitude);

                    var osm = g.GeolocateAddressInfo_OSM(poi.AddressInfo);
                    System.Diagnostics.Debug.WriteLine("OSM : LL: " + osm.Latitude + "," + osm.Longitude);
                    list.Add(osm);

                    var mpq = g.GeolocateAddressInfo_MapquestOSM(poi.AddressInfo);
                    System.Diagnostics.Debug.WriteLine("MPQ : LL: " + mpq.Latitude + "," + mpq.Longitude);
                    list.Add(mpq);

                }
                catch (Exception exp)
                {
                    System.Diagnostics.Debug.WriteLine("Exception during geocoding:" + exp.ToString());
                }
                System.Threading.Thread.Sleep(1000);
            }

            string json = JsonConvert.SerializeObject(list, Formatting.Indented);
            System.IO.File.WriteAllText("C:\\temp\\GeocodingResult.json", json);
             * */
        }
    }
}
