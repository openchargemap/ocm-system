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

namespace OCM.MVC.Controllers
{
    public class POIBrowseModel : OCM.API.Common.SearchFilterSettings
    {
        public POIBrowseModel()
        {
            this.ReferenceData = new OCM.API.Common.ReferenceDataManager().GetCoreReferenceData();
            //this.CountryIDs = new int[] { 1 }; //default to uk
        }

        public string SearchLocation { get; set; }
        public string Country { get; set; }
        public List<OCM.API.Common.Model.ChargePoint> POIList { get; set; }
        public CoreReferenceData ReferenceData { get; set; }
        public SelectList CountryList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData.Countries), this.CountryIDs);
            }
        }

        public SelectList LevelList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData.ChargerTypes), this.LevelIDs);
            }
        }

        public SelectList UsageTypeList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData.UsageTypes), this.UsageTypeIDs);
            }
        }

        public SelectList StatusTypeList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData.StatusTypes), this.StatusTypeIDs);
            }
        }

        public SelectList OperatorList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData.Operators), this.OperatorIDs);
            }
        }


        public SelectList ConnectionTypeList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData.ConnectionTypes), this.ConnectionTypeIDs);
            }
        }

        public SelectList DataProviderList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData.DataProviders), null);
            }
        }

        public SelectList SubmissionTypeList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData.SubmissionStatusTypes), null);
            }
        }

        private List<SimpleReferenceDataType> ToListOfSimpleData(IEnumerable list)
        {
            List<SimpleReferenceDataType> simpleList = new List<SimpleReferenceDataType>();
            if (list == null) return simpleList;

            var listEnumerator = list.GetEnumerator();
            foreach (var item in list)
            {
                simpleList.Add((SimpleReferenceDataType)item);
            }

            return simpleList;
        }

        private SelectList SimpleSelectList(List<SimpleReferenceDataType> list, int[] selectedItems)
        {
            return SimpleSelectList(list, selectedItems, true);
        }

        private SelectList SimpleSelectList(List<SimpleReferenceDataType> list, int[] selectedItems, bool includeNoSelectionValue)
        {
            if (list == null)
            {
                return null;
            }
            else
            {
                if (includeNoSelectionValue)
                {
                    list.Insert(0, new SimpleReferenceDataType { ID = -1, Title = "(None Selected)" });
                }
                return new SelectList(list, "ID", "Title", (selectedItems != null && selectedItems.Length > 0) ? selectedItems[0].ToString() : null);
            }
        }
    }

    public class POIController : Controller
    {
        //
        // GET: /POI/

        public ActionResult Index(POIBrowseModel filter)
        {
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

                    var position = geocode.GeoCodeFromString(filter.SearchLocation.Trim() + (searchCountryName != null ? ", " + searchCountryName : ""));

                    if (position != null)
                    {
                        filter.Latitude = position.Latitude;
                        filter.Longitude = position.Longitude;
                        if (filter.Distance == null) filter.Distance = 50;
                        filter.DistanceUnit = API.Common.Model.DistanceUnit.Miles;

                        //if (distanceunit == "km") searchfilters.DistanceUnit = API.Common.Model.DistanceUnit.KM;

                        ViewBag.FormattedAddress = position.Address;
                    }
                }
            }

            filter.POIList = cpManager.GetChargePoints((OCM.API.Common.SearchFilterSettings)filter);
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
        public ActionResult Details(int id, string layout)
        {
            OCM.API.Common.POIManager cpManager = new API.Common.POIManager();
            var poi = cpManager.Get(id, true);
            ViewBag.FullTitle = "Location Details: OCM-" + poi.ID + " " + poi.AddressInfo.Title;

            var imageList = new OCM.MVC.App_Code.GeocodingHelper().GetGeneralLocationImages((double)poi.AddressInfo.Latitude, (double)poi.AddressInfo.Longitude);

            if (imageList != null)
            {
                imageList = imageList.Where(i => i.Width >= 500).ToList();
                ViewBag.ImageList = imageList.ToList();
            }

            if (layout == "simple")
            {
                return View("Details", "_SimpleLayout", poi);
            }
            else
            {
                return View(poi);
            }

        }

        public ActionResult AddMediaItem(int id)
        {
            var cpManager = new API.Common.POIManager();
            var poi = cpManager.Get(id, true);

            return View(poi);
        }

        //
        // POST: /POI/UploadImage

        [HttpPost]
        public ActionResult AddMediaItem(int id, FormCollection collection)
        {
            try
            {
                int userId = (int)Session["UserID"];

                var files = Request.Files;
                string filePrefix = DateTime.UtcNow.Millisecond.ToString() + "_";
                string comment = collection["comment"];
                var tempFiles = new List<string>();

                string tempFolder = Server.MapPath("~/temp/uploads/");
                foreach (string file in Request.Files)
                {
                    HttpPostedFileBase postedFile = Request.Files[file];
                    if (postedFile!=null && postedFile.ContentLength>0)
                    {
                        string tmpFile = tempFolder + filePrefix + postedFile.FileName;
                        postedFile.SaveAs(tmpFile);
                        tempFiles.Add(tmpFile);
                    }
                }

                var task = Task.Factory.StartNew(() =>
                {
                    var mediaManager = new MediaItemManager();

                    foreach (var tmpFile in tempFiles)
                    {
                        var photoAdded = mediaManager.AddPOIMediaItem(tempFolder, tmpFile, id, comment, false, userId);
                    }

                }
            , TaskCreationOptions.LongRunning);

                ViewBag.PoiId = id;
                ViewBag.UploadCompleted = true;
                return View();
                //return RedirectToAction("Details", new { id = id });
            }
            catch
            {
                return View();
            }
        }

        //
        // GET: /POI/Edit/5

        public ActionResult Edit(int id)
        {
            return View();
        }

        //
        // POST: /POI/Edit/5

        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }


        public ActionResult Activity()
        {
            var summaryManager = new DataSummaryManager();
            var summary = summaryManager.GetActivitySummary(new SearchFilterSettings());

            return View(summary);
        }

        //
        // GET: /POI/Delete/5

        public ActionResult Delete(int id)
        {
            return View();
        }

        //
        // POST: /POI/Delete/5

        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
