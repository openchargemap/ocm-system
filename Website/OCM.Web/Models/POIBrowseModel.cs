﻿using Microsoft.AspNetCore.Mvc.Rendering;
using OCM.API.Common.Model;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OCM.MVC.Models
{
    public class POIBrowseModel : OCM.API.Common.APIRequestParams
    {
        public POIBrowseModel()
        {
           
            this.AllowOptionalCountrySelection = true;
        }
        public POIBrowseModel(CoreReferenceData refData)
        {

            this.ReferenceData = refData;
            this.AllowOptionalCountrySelection = true;
        }

        public string SearchLocation { get; set; }
        public string Country { get; set; }
        public List<OCM.API.Common.Model.ChargePoint> POIList { get; set; }
        public CoreReferenceData ReferenceData { get; set; }

        public bool ShowAdvancedOptions { get; set; }
        public bool AllowOptionalCountrySelection { get; set; }

        public SelectList CountryList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData?.Countries), this.CountryIDs, AllowOptionalCountrySelection);
            }
        }

        public SelectList LevelList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData?.ChargerTypes), this.LevelIDs);
            }
        }

        public SelectList UsageTypeList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData?.UsageTypes), this.UsageTypeIDs);
            }
        }

        public SelectList StatusTypeList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData?.StatusTypes.Where(s => s.IsUserSelectable == true)), this.StatusTypeIDs);
            }
        }

        public SelectList OperatorList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData?.Operators), this.OperatorIDs);
            }
        }


        public SelectList ConnectionTypeList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData?.ConnectionTypes), this.ConnectionTypeIDs);
            }
        }

        public SelectList DataProviderList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData?.DataProviders.Where(p => p.IsApprovedImport == true || p.IsOpenDataLicensed == true)), null);
            }
        }

        public SelectList SubmissionTypeList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData?.SubmissionStatusTypes), null);
            }
        }

        public SelectList CurrentTypeList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData?.CurrentTypes), null);
            }
        }

        public SelectList UserCommentTypeList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData?.UserCommentTypes), null, false);
            }
        }

        public SelectList CheckinStatusTypeList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData?.CheckinStatusTypes), null, false);
            }
        }

        public SelectList RatingTypeList
        {
            get
            {
                var ratingtypes = new List<SimpleReferenceDataType>();
                ratingtypes.Add(new SimpleReferenceDataType { ID = 5, Title = "5 - Excellent" });
                ratingtypes.Add(new SimpleReferenceDataType { ID = 4, Title = "4 - Good" });
                ratingtypes.Add(new SimpleReferenceDataType { ID = 3, Title = "3 - Average" });
                ratingtypes.Add(new SimpleReferenceDataType { ID = 2, Title = "2 - Not Good" });
                ratingtypes.Add(new SimpleReferenceDataType { ID = 1, Title = "1 - Bad" });

                return SimpleSelectList(ratingtypes, null, true, 0, "Not Rated");
            }
        }

        private List<SimpleReferenceDataType> ToListOfSimpleData(IEnumerable list)
        {
            if (list == null)
            {
                return new List<SimpleReferenceDataType>();
            }
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
            return SimpleSelectList(list, selectedItems, includeNoSelectionValue, null, null);
        }

        private SelectList SimpleSelectList(List<SimpleReferenceDataType> list, int[] selectedItems, bool includeNoSelectionValue, int? noSelectionValue, string noSelectionText)
        {
            if (list == null)
            {
                return null;
            }
            else
            {
                if (includeNoSelectionValue)
                {
                    list.Insert(0, new SimpleReferenceDataType { ID = (noSelectionValue != null ? (int)noSelectionValue : -1), Title = (noSelectionText != null ? noSelectionText : "(None Selected)") });
                }
                return new SelectList(list, "ID", "Title", (selectedItems != null && selectedItems.Length > 0) ? selectedItems[0].ToString() : null);
            }
        }
    }

}