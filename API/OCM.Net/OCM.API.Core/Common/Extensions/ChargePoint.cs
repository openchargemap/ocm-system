using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class ChargePoint
    {
        public static Model.ChargePoint FromDataModel(Core.Data.ChargePoint source)
        {
            return FromDataModel(source, false, false, false, true);
        }

        public static Model.ChargePoint FromDataModel(Core.Data.ChargePoint source, bool loadUserComments, bool loadMediaItems, bool loadMetadataValues, bool isVerboseMode)
        {
            if (source == null) return null;

            var poi = new Model.ChargePoint();

            poi.ID = source.ID;
            poi.UUID = source.UUID;

            //populate data provider info (full object or id only)
            if (isVerboseMode && source.DataProvider != null)
            {
                poi.DataProvider = DataProvider.FromDataModel(source.DataProvider);
                poi.DataProviderID = source.DataProvider.ID;
            }
            else
            {
                poi.DataProviderID = source.DataProviderID;
            }

            poi.DataProvidersReference = source.DataProvidersReference;

            //populate Operator (full object or id only)
            if (isVerboseMode && source.Operator != null)
            {
                poi.OperatorInfo = OperatorInfo.FromDataModel(source.Operator);
                poi.OperatorID = source.Operator.ID;
            }
            else
            {
                poi.OperatorID = source.OperatorID;
            }

            poi.OperatorsReference = source.OperatorsReference;

            //populate usage type (full object or id only)
            if (isVerboseMode && source.UsageType != null)
            {
                poi.UsageType = UsageType.FromDataModel(source.UsageType);
                poi.UsageTypeID = source.UsageType.ID;
            }
            else
            {
                poi.UsageTypeID = source.UsageTypeID;
            }

            poi.UsageCost = source.UsageCost;

            //populate address info
            if (source.AddressInfo != null)
            {
                poi.AddressInfo = AddressInfo.FromDataModel(source.AddressInfo, isVerboseMode);
            }

            poi.NumberOfPoints = source.NumberOfPoints;
            poi.GeneralComments = source.GeneralComments;

            poi.DatePlanned = source.DatePlanned;
            poi.DateLastConfirmed = source.DateLastConfirmed;

            //populate status type (full object or id only)
            if (isVerboseMode && source.StatusType != null)
            {
                poi.StatusType = StatusType.FromDataModel(source.StatusType);
                poi.StatusTypeID = source.StatusType.ID;
            }
            else
            {
                poi.StatusTypeID = source.StatusTypeID;
            }

            poi.DateLastStatusUpdate = source.DateLastStatusUpdate;
            poi.DataQualityLevel = source.DataQualityLevel;
            poi.DateCreated = source.DateCreated;

            //populate submission status type (full object or id only)
            if (isVerboseMode && source.SubmissionStatusType != null)
            {
                poi.SubmissionStatus = SubmissionStatusType.FromDataModel(source.SubmissionStatusType);
                poi.SubmissionStatusTypeID = source.SubmissionStatusType.ID;
            }
            else
            {
                poi.SubmissionStatusTypeID = source.SubmissionStatusTypeID;
            }

            poi.Connections = new List<Model.ConnectionInfo>();
            foreach (var conn in source.Connections)
            {
                poi.Connections.Add(ConnectionInfo.FromDataModel(conn, isVerboseMode));
            }

            //loadUserComments = true;
            //loadMetadataValues = true;
            //loadMediaItems = true;

            //optionally load user comments
            if (loadUserComments)
            {
                foreach (var comment in source.UserComments.OrderByDescending(cm => cm.DateCreated))
                {
                    if (poi.UserComments == null) poi.UserComments = new List<Model.UserComment>();
                    Model.UserComment com = UserComment.FromDataModel(comment, isVerboseMode);
                    poi.UserComments.Add(com);
                }
            }

            if (loadMediaItems)
            {
                foreach (var mediaItem in source.MediaItems)
                {
                    if (poi.MediaItems == null) poi.MediaItems = new List<Model.MediaItem>();
                    poi.MediaItems.Add(MediaItem.FromDataModel(mediaItem));
                }
            }

            if (loadMetadataValues)
            {
                foreach (var metadataValue in source.MetadataValues)
                {
                    if (poi.MetadataValues == null) poi.MetadataValues = new List<Model.MetadataValue>();
                    poi.MetadataValues.Add(MetadataValue.FromDataModel(metadataValue));
                }
            }
            return poi;
        }
    }
}