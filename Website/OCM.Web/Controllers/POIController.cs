using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OCM.API.Common;
using OCM.API.Common.DataSummary;
using OCM.API.Common.Model;
using OCM.Core.Common;
using OCM.Core.Settings;
using OCM.MVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OCM.MVC.Controllers
{
    public class POIController : BaseController
    {
        // GET: /POI/
        private IHostEnvironment _host;
        private IMemoryCache _cache;

        private CoreSettings _settings;
        public POIController(IHostEnvironment host, IMemoryCache memoryCache, IConfiguration config)
        {
            _host = host;
            _cache = memoryCache;

            _settings = GetSettingsFromConfig(config);
        }

        private CoreReferenceData GetCoreReferenceData()
        {
            return GetCoreReferenceDataAsync().Result;
        }

        private async Task<CoreReferenceData> GetCoreReferenceDataAsync()
        {
            if (_cache.TryGetValue("_coreRefData", out var refData))
            {
                return refData as CoreReferenceData;
            }
            else
            {
                using (var refDataManager = new OCM.API.Common.ReferenceDataManager())
                {
                    var result = await refDataManager.GetCoreReferenceDataAsync();
                    _cache.Set("_coreRefData", result);
                    return result;
                }
            }
        }

        public async Task<ActionResult> Index(POIBrowseModel filter)
        {
            if (filter?.ReferenceData == null)
            {
                if (filter == null)
                {
                    filter = new POIBrowseModel();
                    filter.ShowAdvancedOptions = true;
                }
                filter.ReferenceData = await GetCoreReferenceDataAsync();

            }

            var cpManager = new API.Common.POIManager();

            //dropdown selections of -1 represent an intended null selection, fix this by nulling relevant items
            filter.CountryIDs = this.ConvertNullableSelection(filter.CountryIDs);
            filter.LevelIDs = this.ConvertNullableSelection(filter.LevelIDs);
            filter.ConnectionTypeIDs = this.ConvertNullableSelection(filter.ConnectionTypeIDs);
            filter.OperatorIDs = this.ConvertNullableSelection(filter.OperatorIDs);
            filter.StatusTypeIDs = this.ConvertNullableSelection(filter.StatusTypeIDs);
            filter.UsageTypeIDs = this.ConvertNullableSelection(filter.UsageTypeIDs);
            filter.DataProviderIDs = this.ConvertNullableSelection(filter.DataProviderIDs);
            filter.IncludeComments = true;

            if (IsRequestByRobot)
            {
                //force mirror db when browser is a robot
                filter.AllowMirrorDB = true;
                filter.AllowDataStoreDB = false;
            }

            if (!String.IsNullOrWhiteSpace(filter.Country))
            {
                //TODO: cache country id lookup
                using (var refData = new OCM.API.Common.ReferenceDataManager())
                {
                    var countrySelected = refData.GetCountryByName(filter.Country);
                    if (countrySelected != null)
                    {
                        filter.CountryIDs = new int[] { countrySelected.ID };
                    }
                }
            }
            else
            {
                //default to UK
                // if (filter.CountryIDs == null) filter.CountryIDs = new int[] { 1 };
            }

            filter.MaxResults = 100;

            if (!String.IsNullOrWhiteSpace(filter.SearchLocation))
            {
                if (filter.SearchLocation.ToUpper().StartsWith("OCM-")) filter.SearchLocation = filter.SearchLocation.Replace("OCM-", "");
                if (filter.SearchLocation.ToUpper().StartsWith("OCM")) filter.SearchLocation = filter.SearchLocation.Replace("OCM", "");

                if (IsNumeric(filter.SearchLocation.Trim()))
                {
                    int poiID = -1;
                    //treat numbers as OCM ref
                    if (int.TryParse(filter.SearchLocation.Trim(), out poiID))
                    {
                        filter.ChargePointIDs = new int[] { poiID };
                    }
                }
                else
                {
                    //attempt to geocode
                    var geocode = new GeocodingHelper(_settings);
                    string searchCountryName = null;
                    if (filter.CountryIDs != null && filter.CountryIDs.Count() > 0)
                    {
                        var searchCountry = filter.ReferenceData.Countries.FirstOrDefault(c => c.ID == filter.CountryIDs[0]);
                        searchCountryName = searchCountry.Title;
                    }

                    API.Common.Model.Extended.GeocodingResult position = null;

                    // FIXME: geocoding is disabled, find an laternative service to use
                    //geocode.GeolocateAddressInfo_Google(filter.SearchLocation.Trim() + (searchCountryName != null ? ", " + searchCountryName : ""));

                    if (filter.SearchLocation.Contains(","))
                    {
                        var vals = filter.SearchLocation.Split(',');
                        if (IsNumeric(vals[0]) && IsNumeric(vals[1]))
                        {
                            position = new API.Common.Model.Extended.GeocodingResult
                            {
                                Latitude = double.Parse(vals[0]),
                                Longitude = double.Parse(vals[1])
                            };
                        }
                    }
                    if (position != null)
                    {
                        filter.Latitude = position.Latitude;
                        filter.Longitude = position.Longitude;
                        if (filter.Distance == null) filter.Distance = 50;
                        filter.DistanceUnit = API.Common.Model.DistanceUnit.Miles;
                        //TODO: distance unit KM
                        //if (distanceunit == "km") searchfilters.DistanceUnit = API.Common.Model.DistanceUnit.KM;

                        ViewBag.FormattedAddress = position.Address ?? "";
                    }
                }
            }

            filter.POIList = (await cpManager.GetPOIListAsync((OCM.API.Common.APIRequestParams)filter, filter.ReferenceData)).ToList();
            return View(filter);
        }

        private int[] ConvertNullableSelection(int[] selectedItems)
        {
            if (selectedItems != null)
            {
                var list = selectedItems.ToList();
                list.RemoveAll(i => i == -1);
                if (list.Count > 0)
                {
                    return list.ToArray();
                }
            }
            return (int[])null;
        }

        private bool IsNumeric(string text)
        {
            Regex regex = new Regex(@"^[-+]?[0-9]*\.?[0-9]+$");
            return regex.IsMatch(text);
        }

        // GET: /POI/Details/5

        //[OutputCache(Duration=240, VaryByParam="id")]
        public async Task<ActionResult> Details(int id = 0, string layout = null, string status = null)
        {
            if (id <= 0) return RedirectToAction("Index");

            if (status != null) ViewBag.Status = status;

            OCM.API.Common.POIManager cpManager = new API.Common.POIManager();
            POIViewModel viewModel = new POIViewModel();

            var poi = await cpManager.Get(id, true, allowDiskCache: false, allowMirrorDB: true);
            if (poi != null)
            {
                ViewBag.FullTitle = "Location Details: OCM-" + poi.ID + " " + poi.AddressInfo.Title;

                List<LocationImage> imageList = null; // new OCM.MVC.App_Code.GeocodingHelper().GetGeneralLocationImages((double)poi.AddressInfo.Latitude, (double)poi.AddressInfo.Longitude);

                if (imageList != null)
                {
                    imageList = imageList.Where(i => i.Width >= 500).ToList();
                    ViewBag.ImageList = imageList.ToList();
                }

                viewModel.POI = poi;

                if (!IsRequestByRobot)
                {
                    viewModel.NewComment = new UserComment() { ChargePointID = poi.ID, CommentType = new UserCommentType { ID = 10 }, CheckinStatusType = new CheckinStatusType { ID = 0 } };

                    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                    sw.Start();
                    viewModel.POIListNearby = (await cpManager.GetPOIListAsync(new APIRequestParams { MaxResults = 10, Latitude = poi.AddressInfo.Latitude, Longitude = poi.AddressInfo.Longitude, Distance = 15, DistanceUnit = DistanceUnit.Miles, AllowMirrorDB = true })).ToList();
                    viewModel.POIListNearby.RemoveAll(p => p.ID == poi.ID); //don't include the current item in nearby POI list
                    sw.Stop();
                    System.Diagnostics.Debug.WriteLine(sw.ElapsedMilliseconds);

                    ViewBag.ReferenceData = new POIBrowseModel(await GetCoreReferenceDataAsync());

                    //get data quality report

                    //if (IsUserAdmin)
                    //{
                    viewModel.DataQualityReport = new DataAnalysisManager().GetDataQualityReport(poi);
                    //}

                    ViewBag.UserCanEditPOI = false;
                    if (IsUserSignedIn)
                    {
                        var user = new UserManager().GetUser((int)UserID);
                        if (POIManager.CanUserEditPOI(poi, user))
                        {
                            ViewBag.UserCanEditPOI = true;
                        }
                    }
                }
                else
                {
                    viewModel.POIListNearby = new List<ChargePoint>();
                }
            }

            return View(viewModel);

        }

        [HttpGet, Authorize(Roles = "StandardUser")]
        public ActionResult AddMediaItem(int id)
        {
            ViewBag.IsReadOnlyMode = this.IsReadOnlyMode;

            var cpManager = new API.Common.POIManager();
            var poi = cpManager.Get(id, true);

            return View(poi);
        }

        // POST: /POI/AddMediaItem

        [HttpPost, Authorize(Roles = "StandardUser")]
        public async Task<ActionResult> MediaItem(int id)
        {
            CheckForReadOnly();

            if (ModelState.IsValid)
            {

                var user = new UserManager().GetUser((int)UserID);
                var htmlInputProvider = new OCM.API.InputProviders.HTMLFormInputProvider();

                if (user != null)
                {
                    var mediaItem = new MediaItem();
                    var tempPath = System.IO.Path.GetTempPath() + "\\_ocm";
                    if (!System.IO.Directory.Exists(tempPath))
                    {
                        System.IO.Directory.CreateDirectory(tempPath);
                    }
                    bool uploaded = await htmlInputProvider.ProcessMediaItemSubmission(tempPath, HttpContext, mediaItem, user.ID).ConfigureAwait(true);
                    ViewBag.PoiId = id;

                    if (uploaded)
                    {
                        ViewBag.UploadCompleted = true;
                    }

                }
            }

            return View("AddMediaItem");
        }

        [Authorize(Roles = "StandardUser")]
        public async Task<ActionResult> Add()
        {
            var refData = new POIBrowseModel(GetCoreReferenceData());
            refData.AllowOptionalCountrySelection = false;

            ViewBag.ReferenceData = refData;
            ViewBag.ConnectionIndex = 0; //connection counter shared by equipment details
            ViewBag.EnableEditView = false;

            //get a default new POI using std core reference data
            using (var refDataManager = new ReferenceDataManager())
            {
                var coreReferenceData = await refDataManager.GetCoreReferenceDataAsync(new APIRequestParams());
                coreReferenceData.ChargePoint.OperatorInfo.ID = 1;// Unknown Operator
                coreReferenceData.ChargePoint.StatusType.ID = 50; //Operational
                coreReferenceData.ChargePoint.UsageType.ID = 6; //private for staff and visitors
                coreReferenceData.ChargePoint.SubmissionStatus = null; //let system decide on submit
                coreReferenceData.ChargePoint.SubmissionStatusTypeID = null;
                return View("Edit", coreReferenceData.ChargePoint);
            }
        }

        // GET: /POI/Edit/5
        [Authorize(Roles = "StandardUser")]
        public async Task<ActionResult> Edit(int? id, bool createCopy = false)
        {
            ViewBag.IsReadOnlyMode = this.IsReadOnlyMode;

            if (id > 0)
            {
                ChargePoint poi = null;

                if (createCopy)
                {
                    //get version of POI with location details removed, copying equipment etc
                    poi = await new POIManager().GetCopy((int)id, true);
                }
                else
                {
                    poi = await new POIManager().Get((int)id);
                }

                if (poi != null)
                {
                    InitEditReferenceData(poi);

                    var refData = new POIBrowseModel(GetCoreReferenceData());
                    ViewBag.ReferenceData = refData;
                    ViewBag.HideAdvancedInfo = true;

                    if (!createCopy)
                    {
                        try
                        {
                            var user = new UserManager().GetUser((int)UserID);
                            if (POIManager.CanUserEditPOI(poi, user))
                            {
                                ViewBag.HideAdvancedInfo = false;
                            }
                        }
                        catch (Exception)
                        {
                            ; ; //user not signed in
                        }
                    }

                    //enable advanced edit options for full editors/admin
                    return View(poi);
                }
            }

            //no applicable poi, jump back to browse
            return RedirectToAction("Index", "POI");
        }

        [HttpGet]
        [Authorize(Roles = "StandardUser")]
        public async Task<ActionResult> Approve(int id)
        {
            CheckForReadOnly();

            // if poi is awaiting review, publish now
            var poiManager = new POIManager();

            var poi = await poiManager.Get(id, true);
            if (poi.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Submitted_UnderReview || poi.SubmissionStatusTypeID == (int)StandardSubmissionStatusTypes.Imported_UnderReview)
            {
                var user = new UserManager().GetUser((int)UserID);
                if (POIManager.CanUserEditPOI(poi, user))
                {
                    poi.SubmissionStatusTypeID = (int)StandardSubmissionStatusTypes.Submitted_Published;
                    await new SubmissionManager().PerformPOISubmission(poi, user);
                }
            }

            // return to approval queue
            return RedirectToAction("Index", "POI", new { submissionStatusTypeId = 1 });
        }



        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "StandardUser")]
        public async Task<ActionResult> Edit(ChargePoint poi)
        {
            CheckForReadOnly();

            var refData = new POIBrowseModel(GetCoreReferenceData());
            refData.AllowOptionalCountrySelection = false;
            ViewBag.ReferenceData = refData;

            ViewBag.ConnectionIndex = 0; //connection counter shared by equipment details
            ViewBag.EnableEditView = true;

            if (Request.Form["editoption"] == "addconnection")
            {
                //add a placeholder for new equipment details
                if (poi.Connections == null) poi.Connections = new List<API.Common.Model.ConnectionInfo>();
                //TODO: setup defaults
                poi.Connections.Add(new API.Common.Model.ConnectionInfo());
                return View(poi);
            }

            if (Request.Form["editoption"].ToString().StartsWith("remove-equipment"))
            {
                //TODO:remove requested connection
                //poi.Connections.Remove();
                string[] equipmentElementIDs = Request.Form["editoption"].ToString().Split('-');
                int itemIndex = int.Parse(equipmentElementIDs[2]);
                poi.Connections.RemoveAt(itemIndex);
                return View(poi);
            }

            if (Request.Form["editoption"] == "preview")
            {
                //preview poi
                ViewBag.EnablePreviewMode = true;

                //reset any values provided as -1 to a standard default (unknown etc)
                PrepareDefaultsForBlankSelections(poi);

                //update preview of poi with fully populated reference data
                poi = new POIManager().PreviewPopulatedPOIFromModel(poi, refData.ReferenceData);

                InitEditReferenceData(poi);

                return View(poi);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    User user = null;

                    if (IsUserSignedIn) user = new UserManager().GetUser((int)UserID);

                    //reset any values provided as -1 to a standard default (unknown etc)
                    PrepareDefaultsForBlankSelections(poi);

                    if (poi.AddressInfo.Country == null || poi.AddressInfo.Country.ID == -1) ModelState.AddModelError("Country", "Required");

                    //perform actual POI submission, then redirect to POI details if we can
                    var poiSubmissionResult = await new SubmissionManager().PerformPOISubmission(poi, user);
                    if (poiSubmissionResult.IsValid)
                    {
                        if (poiSubmissionResult.ItemId > 0)
                        {
                            return RedirectToAction("Details", "POI", new { id = poiSubmissionResult.ItemId, status = "editsubmitted" });
                        }
                        else
                        {
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        ViewBag.ValidationFailed = true;
                        ModelState.AddModelError("Country", "Required");

                    }
                }
                catch
                {
                    //return View(poi);
                }
            }
            else
            {
                foreach (var modelState in ViewData.ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine(error.ToString());
                    }
                }
            }

            ViewBag.ReferenceData = new POIBrowseModel(GetCoreReferenceData());

            return View(poi);
        }

        private void PrepareDefaultsForBlankSelections(ChargePoint poi)
        {
            //Where -1 is supplied as dropdown value etc we need to revert to a default or null value;
            //FIXME: the binding method varies between hidden fields and dropdown values
            if (poi.DataProviderID >= 1 || (poi.DataProvider != null && poi.DataProvider.ID > 0))
            {
                int providerId = (poi.DataProvider != null ? poi.DataProvider.ID : (int)poi.DataProviderID);
                poi.DataProviderID = providerId;
            }
            else
            {
                poi.DataProvider = null;
                poi.DataProviderID = (int)StandardDataProviders.OpenChargeMapContrib;
            }

            if (poi.OperatorID >= 1 || poi.OperatorInfo != null)
            {
                int operatorId = poi.OperatorInfo != null ? poi.OperatorInfo.ID : (int)poi.OperatorID;
                poi.OperatorID = operatorId;
            }
            else
            {
                poi.OperatorID = (int)StandardOperators.UnknownOperator;
            }

            if (poi.UsageTypeID > 0 || poi.UsageType != null)
            {
                int usageTypeId = poi.UsageType != null ? poi.UsageType.ID : (int)poi.UsageTypeID;
                poi.UsageTypeID = usageTypeId;
            }
            else
            {
                poi.UsageTypeID = (int)StandardUsageTypes.Unknown;
            }

            if (poi.StatusTypeID >= 0 || (poi.StatusType != null && (poi.StatusTypeID == -1 || poi.StatusTypeID == null)))
            {
                int statusTypeId = poi.StatusType != null ? poi.StatusType.ID : (int)poi.StatusTypeID;
                poi.StatusTypeID = statusTypeId;
            }

            if (poi.StatusTypeID == -1 || poi.StatusTypeID == null)
            {
                poi.StatusType = null;
                poi.StatusTypeID = (int)StandardStatusTypes.Unknown;
            }

            if (poi.SubmissionStatusTypeID == -1)
            {
                poi.SubmissionStatus = null;
                poi.SubmissionStatusTypeID = null;
            }

            if (poi.Connections != null)
            {
                foreach (var connection in poi.Connections)
                {
                    if (connection.ConnectionTypeID == -1)
                    {
                        connection.ConnectionType = null;
                        connection.ConnectionTypeID = (int)StandardConnectionTypes.Unknown;
                    }

                    if (connection.CurrentTypeID == -1)
                    {
                        connection.CurrentType = null;
                        connection.CurrentTypeID = null;
                    }

                    if (connection.StatusTypeID == -1)
                    {
                        connection.StatusType = null;
                        connection.StatusTypeID = (int)StandardStatusTypes.Unknown;
                    }

                    if (connection.LevelID == -1)
                    {
                        connection.Level = null;
                        connection.LevelID = null;
                    }
                }
            }
        }

        private void InitEditReferenceData(ChargePoint poi)
        {
            //edit/preview may not have some basic info we need for further edits such as a default connectioninfo object
            if (poi.Connections == null)
            {
                poi.Connections = new List<OCM.API.Common.Model.ConnectionInfo>();
            }

            if (poi.Connections.Count == 0)
            {
                poi.Connections.Add(new OCM.API.Common.Model.ConnectionInfo());
            }

            //urls require at least http:// prefix
            if (poi.AddressInfo.RelatedURL != null && poi.AddressInfo.RelatedURL.Trim().Length > 0 && !poi.AddressInfo.RelatedURL.Trim().StartsWith("http"))
            {
                poi.AddressInfo.RelatedURL = "http://" + poi.AddressInfo.RelatedURL;
            }
        }

        public async Task<ActionResult> AddComment(int id)
        {
            ViewBag.IsReadOnlyMode = this.IsReadOnlyMode;

            var cpManager = new API.Common.POIManager();
            var poi = await cpManager.Get(id, true);

            POIViewModel viewModel = new POIViewModel();
            viewModel.NewComment = new UserComment() { ChargePointID = poi.ID, CommentType = new UserCommentType { ID = 10 }, CheckinStatusType = new CheckinStatusType { ID = 0 } };
            viewModel.POI = poi;

            ViewBag.ReferenceData = new POIBrowseModel(GetCoreReferenceData());

            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Roles = "StandardUser")]
        public async Task<ActionResult> CommentActioned(int poiId, int commentId)
        {
            CheckForReadOnly();

            // if poi is awaiting review, publish now
            var poiManager = new POIManager();

            var poi = await poiManager.Get(poiId, true);
            var comment = poi.UserComments.Find(c => c.ID == commentId);

            using (var userManager = new UserManager())
            {
                var user = userManager.GetUser((int)UserID);

                if (POIManager.CanUserEditPOI(poi, user))
                {
                    using (var commentManager = new UserCommentManager())
                    {
                        await commentManager.ActionComment(user.ID, commentId);
                    }
                }
            }

            // return to approval queue
            return RedirectToAction("details", "POI", new { id = poiId });
        }

        [HttpPost, Authorize(Roles = "StandardUser"), ValidateAntiForgeryToken]
        public async Task<ActionResult> Comment(POIViewModel model)
        {
            CheckForReadOnly();

            var comment = model.NewComment;
            if (ModelState.IsValid)
            {
                try
                {
                    using (var userManager = new UserManager())
                    {
                        var user = userManager.GetUser((int)UserID);
                        if (comment.Rating == 0) comment.Rating = null;

                        if (await new SubmissionManager().PerformSubmission(comment, user) > 0)
                        {
                            if (comment.ChargePointID > 0)
                            {
                                return RedirectToAction("Details", "POI", new { id = comment.ChargePointID });
                            }
                            else
                            {
                                return RedirectToAction("Index");
                            }
                        }
                    }
                }
                catch
                {
                    //return View(poi);
                }
            }
            else
            {
                foreach (var modelState in ViewData.ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine(error.ToString());
                    }
                }
                return View("AddComment", model);
            }
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Activity()
        {
            using (var summaryManager = new DataSummaryManager())
            {
                var summary = await summaryManager.GetActivitySummary(new APIRequestParams());
                ViewBag.ShowPOILink = true;
                return View(summary);
            }
        }

        public ActionResult ReviewDuplicates(int? countryId, bool enableCache = true, double maxDupeRange = 500, int minConfidence = 70)
        {
            if (countryId == null) countryId = 1;//default to UK
            ViewBag.MinConfidenceLevel = minConfidence;
            string cacheKey = "DupeList_" + countryId;

            OCM.API.Common.Model.Extended.POIDuplicates duplicateSummary = null;
            if (enableCache && _cache.Get(cacheKey) != null)
            {
                duplicateSummary = (OCM.API.Common.Model.Extended.POIDuplicates)_cache.Get(cacheKey);
            }
            else
            {
                duplicateSummary = new DataAnalysisManager().GetAllPOIDuplicates(new POIManager(), (int)countryId, maxDupeRange);
                _cache.Set(cacheKey, duplicateSummary);
            }

            ViewBag.ShowDataProvider = true;
            return View(duplicateSummary.DuplicateSummaryList);
        }
    }
}