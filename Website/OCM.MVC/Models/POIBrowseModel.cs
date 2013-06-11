using OCM.API.Common.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OCM.MVC.Models
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

        public SelectList CurrentTypeList
        {
            get
            {
                return SimpleSelectList(ToListOfSimpleData(ReferenceData.CurrentTypes), null);
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

}