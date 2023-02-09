using GeoCoordinatePortable;
using OCM.API.Common;
using OCM.API.Common.Model.Extended;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.Core.Common
{
    public class DataAnalysisManager
    {
        public POIDuplicates GetAllPOIDuplicates(POIManager poiManager, int countryId, double maxDupeRange = 500)
        {
            List<DuplicatePOIItem> allDuplicates = new List<DuplicatePOIItem>();

            var dataModel = new OCM.Core.Data.OCMEntities();

            double DUPLICATE_DISTANCE_METERS = 25;
            double POSSIBLE_DUPLICATE_DISTANCE_METERS = maxDupeRange;

            //TODO: better method for large number of POIs
            //grab all live POIs (30-100,000 items)
            //var allPOIs = dataModel.ChargePoints.Where(s => s.AddressInfo.CountryID == countryId && (s.SubmissionStatusTypeID == 100 || s.SubmissionStatusTypeID == 200)).ToList();
            var allPOIs = poiManager.GetPOIList(new APIRequestParams { CountryIDs = new int[] { countryId }, MaxResults = 200000 });

            foreach (var poi in allPOIs)
            {
                //find pois which duplicate the given poi
                var dupePOIs = allPOIs.Where(p => p.ID != poi.ID &&
                    (
                        p.DataProvidersReference != null && p.DataProvidersReference.Length > 0 && p.DataProvidersReference == poi.DataProvidersReference
                        || new GeoCoordinate(p.AddressInfo.Latitude, p.AddressInfo.Longitude).GetDistanceTo(new GeoCoordinate(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude)) < POSSIBLE_DUPLICATE_DISTANCE_METERS
                    )
                    );

                if (dupePOIs.Any())
                {
                    var poiModel = poi;// OCM.API.Common.Model.Extensions.ChargePoint.FromDataModel(poi, true, true, true, true);

                    foreach (var dupe in dupePOIs)
                    {
                        //poi has duplicates
                        DuplicatePOIItem dupePoi = new DuplicatePOIItem { DuplicatePOI = dupe, DuplicateOfPOI = poiModel };
                        dupePoi.Reasons = new List<string>();
                        if (dupe.AddressInfo.Latitude == poi.AddressInfo.Latitude && dupe.AddressInfo.Longitude == poi.AddressInfo.Longitude)
                        {
                            dupePoi.Reasons.Add("POI location is exact match for OCM-" + poi.ID);
                            dupePoi.Confidence = 95;
                        }
                        else
                        {
                            double distanceMeters = new GeoCoordinate(dupe.AddressInfo.Latitude, dupe.AddressInfo.Longitude).GetDistanceTo(new GeoCoordinate(poi.AddressInfo.Latitude, poi.AddressInfo.Longitude));
                            if (distanceMeters < DUPLICATE_DISTANCE_METERS)
                            {
                                dupePoi.Reasons.Add("POI location is close proximity (" + distanceMeters + "m) to OCM-" + poi.ID);
                                dupePoi.Confidence = 75;
                            }
                            else
                            {
                                if (distanceMeters < POSSIBLE_DUPLICATE_DISTANCE_METERS)
                                {
                                    dupePoi.Reasons.Add("POI location is in surrounding proximity (" + distanceMeters + "m) to OCM-" + poi.ID);
                                    dupePoi.Confidence = 50;
                                }
                            }
                        }

                        allDuplicates.Add(dupePoi);
                    }
                }
            }

            //arrange all duplicates into groups
            POIDuplicates duplicatesSummary = new POIDuplicates();
            duplicatesSummary.DuplicateSummaryList = new List<DuplicatePOIGroup>();
            foreach (var dupe in allDuplicates)
            {
                bool isNewGroup = false;
                var dupeGroup = duplicatesSummary.DuplicateSummaryList.FirstOrDefault(d => d.DuplicatePOIList.Any(p => p.DuplicateOfPOI.ID == dupe.DuplicateOfPOI.ID || p.DuplicatePOI.ID == dupe.DuplicatePOI.ID) || d.SuggestedBestPOI.ID == dupe.DuplicatePOI.ID);
                if (dupeGroup == null)
                {
                    isNewGroup = true;
                    dupeGroup = new DuplicatePOIGroup();
                    dupeGroup.SuggestedBestPOI = dupe.DuplicatePOI;//TODO: select best

                    dupeGroup.DuplicatePOIList = new List<DuplicatePOIItem>();
                }

                //only add to dupe group if not already added for another reason
                if (!dupeGroup.DuplicatePOIList.Contains(dupe) && !dupeGroup.DuplicatePOIList.Any(d => d.DuplicatePOI.ID == dupe.DuplicatePOI.ID))
                {
                    dupeGroup.DuplicatePOIList.Add(dupe);
                }

                if (isNewGroup)
                {
                    duplicatesSummary.DuplicateSummaryList.Add(dupeGroup);
                }
            }

            //loop through groups and rearrange
            RearrangeDuplicates(duplicatesSummary);

            //go through all groups and populate final list of All POI per group
            foreach (var g in duplicatesSummary.DuplicateSummaryList)
            {
                var poiList = new List<OCM.API.Common.Model.ChargePoint>();
                foreach (var d in g.DuplicatePOIList)
                {
                    if (!poiList.Contains(d.DuplicatePOI))
                    {
                        poiList.Add(d.DuplicatePOI);
                    }

                    if (!poiList.Contains(d.DuplicateOfPOI))
                    {
                        poiList.Add(d.DuplicateOfPOI);
                    }

                    g.AllPOI = poiList;
                }
            }

            //TODO: go through all dupe groups and nominate best poi to be main poi (most comments, most equipment info etc)
            return duplicatesSummary;
        }

        private bool OtherDuplicationPOIGroupListHasReference(POIDuplicates duplicates, int poiId, DuplicatePOIGroup currentGroup)
        {
            var mentionedGroups = duplicates.DuplicateSummaryList.Where(d => d.DuplicatePOIList.Any(p => p.DuplicateOfPOI.ID == poiId || p.DuplicatePOI.ID == poiId));
            if (mentionedGroups.Any(m => m != currentGroup))
            {
                //POI has a mention in another group
                return true;
            }
            else
            {
                //POI not mentioned in any other group
                return false;
            }
        }

        /// <summary>
        /// Recursive grouping of duplicates into groups, removing unused/redundant groups
        /// </summary>
        /// <param name="duplicates"></param>
        /// <returns></returns>
        private bool RearrangeDuplicates(POIDuplicates duplicates)
        {
            var actionRequired = false;

            var removedgroups = new List<DuplicatePOIGroup>();
            foreach (var dupegroup in duplicates.DuplicateSummaryList)
            {
                var removedDupes = new List<DuplicatePOIItem>();
                foreach (var dupe in dupegroup.DuplicatePOIList)
                {
                    //if dupe variation is identified an another group, remove from this group
                    if (OtherDuplicationPOIGroupListHasReference(duplicates, dupe.DuplicatePOI.ID, dupegroup))
                    {
                        removedDupes.Add(dupe);
                    }
                }

                if (removedDupes.Any())
                {
                    actionRequired = true;
                    //remove all dupes already present in other groups
                    dupegroup.DuplicatePOIList.RemoveAll(d => removedDupes.Contains(d));
                }

                if (!dupegroup.DuplicatePOIList.Any())
                {
                    actionRequired = true;
                    removedgroups.Add(dupegroup);
                }
            }

            if (removedgroups.Any())
            {
                actionRequired = true;
                //remove empty groups
                duplicates.DuplicateSummaryList.RemoveAll(g => removedgroups.Contains(g));
            }

            //did something this pass, recurse to see if more needed
            if (actionRequired) RearrangeDuplicates(duplicates);

            return actionRequired;
        }

        /// <summary>
        /// For given POI, return data quality report and supporting analysis results
        /// </summary>
        /// <param name="poi"></param>
        /// <returns></returns>
        public POIDataQualityReport CheckPOIDataQuality(OCM.API.Common.Model.ChargePoint poi)
        {
            var report = new POIDataQualityReport();
            report.POI = poi;

            DateTime recentUpdateThreshold = DateTime.UtcNow.AddMonths(-6);

            if (
                (poi.DateLastConfirmed.HasValue && !(poi.DateLastConfirmed.Value > recentUpdateThreshold))
                ||
                (poi.UserComments != null && poi.UserComments.Any(u => u.CheckinStatusType != null && u.CheckinStatusType.IsPositive == true && u.DateCreated > recentUpdateThreshold))
                ||
                (poi.MediaItems != null && poi.MediaItems.Any(u => u.DateCreated > recentUpdateThreshold))
                )
            {
                //has either a recent datelastconfirmed value or a recent positive checkin
                report.ReportItems.Add(new DataQualityReportItem { Category = "User Feedback", Comment = "Has recent user verification", IsPositive = true });
            }
            else
            {
                //low quality score for date last confirmed
                report.ReportItems.Add(new DataQualityReportItem { Weighting = 10, Category = "User Feedback", Comment = "No recent user verification.", IsPositive = false });
            }

            if (poi.UserComments == null || (poi.UserComments != null && !poi.UserComments.Any()))
            {
                //low quality score for comments
                report.ReportItems.Add(new DataQualityReportItem { Weighting = 10, Category = "User Feedback", Comment = "No comments or check-ins", IsPositive = false });
            }
            else
            {
                report.ReportItems.Add(new DataQualityReportItem { Category = "User Feedback", Comment = "Has comments or check-ins", IsPositive = true });
            }

            if (poi.MediaItems == null || (poi.MediaItems != null && !poi.MediaItems.Any()))
            {
                //low quality score for photos
                report.ReportItems.Add(new DataQualityReportItem { Weighting = 10, Category = "User Feedback", Comment = "No photos", IsPositive = false });
            }
            else
            {
                report.ReportItems.Add(new DataQualityReportItem { Category = "User Feedback", Comment = "Has photos", IsPositive = true });
            }

            if (poi.UsageTypeID == null || (poi.UsageTypeID != null && poi.UsageType.ID == 0))
            {
                //low quality score for usage type
                report.ReportItems.Add(new DataQualityReportItem { Weighting = 10, Category = "Data Completeness", Comment = "Unknown Usage Type", IsPositive = false });
            }
            else
            {
                report.ReportItems.Add(new DataQualityReportItem { Category = "Data Completeness", Comment = "Usage Type Known", IsPositive = true });
            }
            if (poi.StatusType == null || (poi.StatusType != null && poi.StatusType.ID == 0))
            {
                //low quality score for status type
                report.ReportItems.Add(new DataQualityReportItem { Weighting = 10, Category = "Data Completeness", Comment = "Unknown Operational Status", IsPositive = false });
            }
            else
            {
                report.ReportItems.Add(new DataQualityReportItem { Category = "Data Completeness", Comment = "Operational Status Known", IsPositive = true });
            }

            if (poi.Connections == null || !poi.Connections.Any())
            {
                report.ReportItems.Add(new DataQualityReportItem { Weighting = 50, Category = "Data Completeness", Comment = "No Equipment Details", IsPositive = false });
            }
            else
            {
                report.ReportItems.Add(new DataQualityReportItem { Category = "Data Completeness", Comment = "Equipment Details Present", IsPositive = true });
            }

            //data quality score starts at 5 (excellent) and is reduced towards 0 for each data issue

            double totalPoints = 100;
            foreach (var p in report.ReportItems.Where(r => r.IsPositive == false))
            {
                totalPoints -= p.Weighting;
            }
            if (totalPoints < 0) totalPoints = 0;
            report.DataQualityScore = totalPoints;

            return report;
        }

        public DataQualityReport GetDataQualityReport(OCM.API.Common.Model.ChargePoint poi)
        {
            DataQualityReport report = new DataQualityReport();
            report.POIReports.Add(CheckPOIDataQuality(poi));
            return report;
        }

        public DataQualityReport GetDataQualityReport(List<OCM.API.Common.Model.ChargePoint> poiList)
        {
            DataQualityReport report = new DataQualityReport();

            foreach (var poi in poiList)
            {
                var poiReport = CheckPOIDataQuality(poi);
                report.POIReports.Add(poiReport);
            }

            return report;
        }
    }
}