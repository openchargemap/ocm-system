using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using OCM.API.Common;
using OCM.API.Common.DataSummary;
using OCM.API.Common.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Helpers;
using OCM.MVC.Models;

namespace OCM.MVC.Controllers
{

    public class POIController : BaseController
    {
        //
        // GET: /POI/

        public ActionResult Index(POIBrowseModel filter)
        {
            if (filter == null) filter = new POIBrowseModel();
            var cpManager = new API.Common.POIManager();

            //dropdown selections of -1 represent an intended null selection, fix this by nulling relevant items
            filter.CountryIDs = this.ConvertNullableSelection(filter.CountryIDs);
            filter.LevelIDs = this.ConvertNullableSelection(filter.LevelIDs);
            filter.OperatorIDs = this.ConvertNullableSelection(filter.OperatorIDs);
            filter.StatusTypeIDs = this.ConvertNullableSelection(filter.StatusTypeIDs);
            filter.UsageTypeIDs = this.ConvertNullableSelection(filter.UsageTypeIDs);
            filter.DataProviderIDs = this.ConvertNullableSelection(filter.DataProviderIDs);


            if (!String.IsNullOrWhiteSpace(filter.Country))
            {
                //TODO: cache country id lookup
                var countrySelected = new OCM.API.Common.ReferenceDataManager().GetCountryByName(filter.Country);
                if (countrySelected != null)
                {
                    filter.CountryIDs = new int[] { countrySelected.ID };
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
                        filter.ChargePointID = poiID;
                    }
                }
                else
                {
                    //attempt to geocode
                    var geocode = new OCM.MVC.App_Code.GeocodingHelper();
                    string searchCountryName = null;
                    if (filter.CountryIDs != null && filter.CountryIDs.Count() > 0)
                    {
                        var searchCountry = filter.ReferenceData.Countries.FirstOrDefault(c => c.ID == filter.CountryIDs[0]);
                        searchCountryName = searchCountry.Title;
                    }

                    var position = geocode.GeolocateAddressInfo_Google(filter.SearchLocation.Trim() + (searchCountryName != null ? ", " + searchCountryName : ""));

                    if (position != null)
                    {
                        filter.Latitude = position.Latitude;
                        filter.Longitude = position.Longitude;
                        if (filter.Distance == null) filter.Distance = 50;
                        filter.DistanceUnit = API.Common.Model.DistanceUnit.Miles;
                        //TODO: distance unit KM
                        //if (distanceunit == "km") searchfilters.DistanceUnit = API.Common.Model.DistanceUnit.KM;

                        ViewBag.FormattedAddress = position.Address;
                    }
                }
            }


            filter.POIList = cpManager.GetChargePoints((OCM.API.Common.APIRequestSettings)filter);
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

        bool IsNumeric(string text)
        {
            Regex regex = new Regex(@"^[-+]?[0-9]*\.?[0-9]+$");
            return regex.IsMatch(text);
        }

        //
        // GET: /POI/Details/5

        //[OutputCache(Duration=240, VaryByParam="id")]
        public ActionResult Details(int id = 0, string layout = null, string status = null)
        {
            if (id <= 0) return RedirectToAction("Index");

            if (status != null) ViewBag.Status = status;

            OCM.API.Common.POIManager cpManager = new API.Common.POIManager();
            var poi = cpManager.Get(id, true);
            ViewBag.FullTitle = "Location Details: OCM-" + poi.ID + " " + poi.AddressInfo.Title;

            var imageList = new OCM.MVC.App_Code.GeocodingHelper().GetGeneralLocationImages((double)poi.AddressInfo.Latitude, (double)poi.AddressInfo.Longitude);

            if (imageList != null)
            {
                imageList = imageList.Where(i => i.Width >= 500).ToList();
                ViewBag.ImageList = imageList.ToList();
            }

            POIViewModel viewModel = new POIViewModel();
            viewModel.NewComment = new UserComment() { ChargePointID = poi.ID, CommentType = new UserCommentType { ID = 10 }, CheckinStatusType = new CheckinStatusType { ID = 0 } };
            viewModel.POI = poi;

            ViewBag.ReferenceData = new POIBrowseModel();

            if (layout == "simple")
            {
                ViewBag.EnableSimpleView = true;
                return View("Details", "_SimpleLayout", viewModel);
            }
            else
            {
                return View(viewModel);
            }

        }

        public ActionResult AddMediaItem(int id)
        {
            var cpManager = new API.Common.POIManager();
            var poi = cpManager.Get(id, true);

            return View(poi);
        }

        //
        // POST: /POI/AddMediaItem

        [HttpPost, AuthSignedInOnly]
        public ActionResult AddMediaItem(int id, FormCollection collection)
        {
            var user = new UserManager().GetUser((int)Session["UserID"]);
            var htmlInputProvider = new OCM.API.InputProviders.HTMLFormInputProvider();

            if (user != null)
            {
                var mediaItem = new MediaItem();
                bool uploaded = htmlInputProvider.ProcessMediaItemSubmission(this.HttpContext.ApplicationInstance.Context, ref mediaItem, user.ID);
                ViewBag.PoiId = id;
                ViewBag.UploadCompleted = true;
                return View();

            }

            return View();
        }

        //[AuthSignedInOnly]
        public ActionResult Add()
        {
            var refData = new POIBrowseModel();
            refData.AllowOptionalCountrySelection = false;

            ViewBag.ReferenceData = refData;
            ViewBag.ConnectionIndex = 0; //connection counter shared by equipment detais
            ViewBag.EnableEditView = false;

            //get a default new POI using std core reference data
            var coreReferenceData = new ReferenceDataManager().GetCoreReferenceData();
            coreReferenceData.ChargePoint.OperatorInfo.ID = 1;// Unknown Operator
            coreReferenceData.ChargePoint.StatusType.ID = 50; //Operational
            coreReferenceData.ChargePoint.UsageType.ID = 6; //private for staff and visitors
            coreReferenceData.ChargePoint.SubmissionStatus = null; //let system decide on submit
            coreReferenceData.ChargePoint.SubmissionStatusTypeID = null;
            return View("Edit", coreReferenceData.ChargePoint);
        }


        //
        // GET: /POI/Edit/5
        public ActionResult Edit(int id)
        {
            var poi = new POIManager().Get(id);
            InitEditReferenceData(poi);

            var refData = new POIBrowseModel();
            ViewBag.ReferenceData = refData;
            ViewBag.HideAdvancedInfo = false; //enable advanced edit options for full editors/admin
            return View(poi);
        }

        //
        // POST: /POI/Edit/

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(ChargePoint poi)
        {
            var refData = new POIBrowseModel();
            refData.AllowOptionalCountrySelection = false;
            ViewBag.ReferenceData = refData;

            ViewBag.ConnectionIndex = 0; //connection counter shared by equipment details
            ViewBag.EnableEditView = true;

            if (Request["editoption"] == "addconnection")
            {
                //add a placeholder for new equipment details
                if (poi.Connections == null) poi.Connections = new List<ConnectionInfo>();
                //TODO: setup defaults
                poi.Connections.Add(new ConnectionInfo());
                return View(poi);
            }

            if (Request["editoption"].ToString().StartsWith("remove-equipment"))
            {
                //TODO:remove requested connection
                //poi.Connections.Remove();
                string[] equipmentElementIDs = Request["editoption"].ToString().Split('-');
                int itemIndex = int.Parse(equipmentElementIDs[2]);
                poi.Connections.RemoveAt(itemIndex);
                return View(poi);
            }

            if (Request["editoption"] == "preview")
            {
                //preview poi
                ViewBag.EnablePreviewMode = true;
                //update preview of poi with fully populated reference data
                poi = new POIManager().PreviewPopulatedPOIFromModel(poi);

                InitEditReferenceData(poi);

                return View(poi);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    User user = null;

                    if (IsUserSignedIn) user = new UserManager().GetUser((int)Session["UserID"]);


                    //reset any values provided as -1 to a standard default (unknown etc) 
                    //the binding method varies between hidden fields and dropdown values (FIXME)
                    if (poi.DataProviderID == -1 || poi.DataProviderID == null)
                    {
                        poi.DataProvider = null;
                        poi.DataProviderID = (int)StandardDataProviders.OpenChargeMapContrib;
                    }

                    if (poi.OperatorInfo != null)
                    {
                        poi.OperatorID = poi.OperatorInfo.ID;
                    }
                    else
                    {
                        poi.OperatorID = (int)StandardOperators.UnknownOperator;
                    }

                    if (poi.UsageType != null)
                    {
                        poi.UsageTypeID = poi.UsageType.ID;
                    }
                    else
                    {
                        poi.UsageTypeID = (int)StandardUsageTypes.Unknown;
                    }

                    if (poi.StatusType != null && (poi.StatusTypeID == -1 || poi.StatusTypeID == null))
                    {
                        poi.StatusTypeID = poi.StatusType.ID;
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

                    if (poi.AddressInfo.Country == null || poi.AddressInfo.Country.ID == -1) ModelState.AddModelError("Country", "Required");

                    //perform actual POI submission, then redirect to POI details if we can
                    int poiSubmissionID = new SubmissionManager().PerformPOISubmission(poi, user);
                    if (poiSubmissionID > -1)
                    {
                        if (poiSubmissionID > 0)
                        {
                            return RedirectToAction("Details", "POI", new { id = poiSubmissionID, status = "editsubmitted" });
                        }
                        else
                        {
                            return RedirectToAction("Index");
                        }
                    }
                    else
                    {
                        ViewBag.ValidationFailed = true;
                    }

                }
                catch
                {
                    //return View(poi);
                }

            }

            ViewBag.ReferenceData = new POIBrowseModel();


            return View(poi);
        }

        private void InitEditReferenceData(ChargePoint poi)
        {
            //edit/preview may not have some basic info we need for further edits such as a default connectioninfo object
            if (poi.Connections == null)
            {
                poi.Connections = new List<OCM.API.Common.Model.ConnectionInfo>();
                poi.Connections.Add(new OCM.API.Common.Model.ConnectionInfo());
            }

            //urls require at least http:// prefix
            if (poi.AddressInfo.RelatedURL != null && poi.AddressInfo.RelatedURL.Trim().Length > 0 && !poi.AddressInfo.RelatedURL.Trim().StartsWith("http"))
            {
                poi.AddressInfo.RelatedURL = "http://" + poi.AddressInfo.RelatedURL;
            }
        }

        [HttpPost, AuthSignedInOnly, ValidateAntiForgeryToken]
        public ActionResult Comment(POIViewModel model)
        {
            var comment = model.NewComment;
            if (ModelState.IsValid)
            {
                try
                {
                    var user = new UserManager().GetUser((int)Session["UserID"]);
                    if (new SubmissionManager().PerformSubmission(comment, user) > 0)
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
                catch
                {
                    //return View(poi);
                }
            }
            return RedirectToAction("Index");
        }

        public ActionResult Activity()
        {
            var summaryManager = new DataSummaryManager();
            var summary = summaryManager.GetActivitySummary(new APIRequestSettings());
            ViewBag.ShowPOILink = true;
            return View(summary);
        }
    }
}
