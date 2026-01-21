using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class ChargePoint
    {
        public static Model.ChargePoint FromDataModel(Core.Data.ChargePoint source, Model.CoreReferenceData refData)
        {
            return FromDataModel(source, false, false, false, true, refData);
        }

        public static Model.ChargePoint FromDataModel(Core.Data.ChargePoint source, bool loadUserComments, bool loadMediaItems, bool loadMetadataValues, bool isVerboseMode, Model.CoreReferenceData refData)
        {
            if (source == null) return null;

            var poi = new Model.ChargePoint();

            poi.ID = source.Id;
            poi.UUID = source.Uuid;

            //populate data provider info (full object or id only)
            if (isVerboseMode)
            {
                poi.DataProvider = refData.DataProviders.FirstOrDefault(i => i.ID == source.DataProviderId) ?? DataProvider.FromDataModel(source.DataProvider);
                poi.DataProviderID = source.DataProviderId;
            }
            else
            {
                poi.DataProviderID = source.DataProviderId;
            }

            poi.DataProvidersReference = source.DataProvidersReference;

            //populate Operator (full object or id only)
            if (isVerboseMode && source.OperatorId != null)
            {
                poi.OperatorInfo = refData.Operators.FirstOrDefault(i => i.ID == source.OperatorId) ?? OperatorInfo.FromDataModel(source.Operator);
                poi.OperatorID = source.OperatorId;
            }
            else
            {
                poi.OperatorID = source.OperatorId;
            }

            poi.OperatorsReference = source.OperatorsReference;

            //populate usage type (full object or id only)
            if (isVerboseMode && source.UsageTypeId != null)
            {
                poi.UsageType = refData.UsageTypes.FirstOrDefault(i => i.ID == source.UsageTypeId) ?? UsageType.FromDataModel(source.UsageType);
                poi.UsageTypeID = source.UsageTypeId;
            }
            else
            {
                poi.UsageTypeID = source.UsageTypeId;
            }

            poi.UsageCost = source.UsageCost;

            //populate address info
            if (source.AddressInfoId != null || source.AddressInfo != null)
            {
                poi.AddressInfo = AddressInfo.FromDataModel(source.AddressInfo, isVerboseMode);
            }

            poi.NumberOfPoints = source.NumberOfPoints;
            poi.GeneralComments = source.GeneralComments;

            poi.DatePlanned = source.DatePlanned;
            poi.DateLastConfirmed = source.DateLastConfirmed;

            //populate status type (full object or id only)
            if (isVerboseMode && source.StatusTypeId != null)
            {
                poi.StatusType = refData.StatusTypes.FirstOrDefault(i => i.ID == source.StatusTypeId) ?? StatusType.FromDataModel(source.StatusType);
                poi.StatusTypeID = source.StatusTypeId;
            }
            else
            {
                poi.StatusTypeID = source.StatusTypeId;
            }

            poi.DateLastStatusUpdate = source.DateLastStatusUpdate;
            poi.DataQualityLevel = source.DataQualityLevel;
            poi.DateCreated = source.DateCreated;

            //populate submission status type (full object or id only)
            if (isVerboseMode && source.SubmissionStatusTypeId != null)
            {
                poi.SubmissionStatus = refData.SubmissionStatusTypes.FirstOrDefault(i => i.ID == source.SubmissionStatusTypeId) ?? SubmissionStatusType.FromDataModel(source.SubmissionStatusType);
                poi.SubmissionStatusTypeID = source.SubmissionStatusTypeId;
            }
            else
            {
                poi.SubmissionStatusTypeID = source.SubmissionStatusTypeId;
            }

            poi.Connections = new List<Model.ConnectionInfo>();
            foreach (var conn in source.ConnectionInfos)
            {
                poi.Connections.Add(ConnectionInfo.FromDataModel(conn, isVerboseMode, refData));
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
                    Model.UserComment com = UserComment.FromDataModel(comment, isVerboseMode, refData);
                    poi.UserComments.Add(com);
                }
            }

            if (loadMediaItems && source.MediaItems != null)
            {
                foreach (var mediaItem in source.MediaItems.Where(mi => mi.IsEnabled != false).OrderByDescending(cm => cm.DateCreated))
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

            //mapping level of detail (priority level, lower is higher priority)
            poi.LevelOfDetail = source.LevelOfDetail;
            return poi;
        }
    }
}
