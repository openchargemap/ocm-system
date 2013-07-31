using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class ChargePoint
    {
        public static Model.ChargePoint FromDataModel(Core.Data.ChargePoint source)
        {
            return FromDataModel(source, false, false, false);
        }

        public static Model.ChargePoint FromDataModel(Core.Data.ChargePoint source, bool loadUserComments, bool loadMediaItems, bool loadMetadataValues)
        {
            if (source == null) return null;

            var poi = new Model.ChargePoint();

            poi.ID = source.ID;
            poi.UUID = source.UUID;

            //populate data provider info
            if (source.DataProvider != null)
            {
                poi.DataProvider = DataProvider.FromDataModel(source.DataProvider);
            }
            poi.DataProvidersReference = source.DataProvidersReference;

            if (source.Operator != null)
            {
                //TODO: populate operator address info
                poi.OperatorInfo = OperatorInfo.FromDataModel(source.Operator);
            }
            poi.OperatorsReference = source.OperatorsReference;

            if (source.UsageType != null)
            {
                poi.UsageType = UsageType.FromDataModel(source.UsageType);
            }
            poi.UsageCost = source.UsageCost;

            //populate address info
            if (source.AddressInfo != null)
            {
                poi.AddressInfo = AddressInfo.FromDataModel(source.AddressInfo);
            }

            poi.NumberOfPoints = source.NumberOfPoints;
            poi.GeneralComments = source.GeneralComments;

            poi.DatePlanned = source.DatePlanned;
            poi.DateLastConfirmed = source.DateLastConfirmed;
            if (source.StatusType != null)
            {
                poi.StatusType = StatusType.FromDataModel(source.StatusType);
            }
            poi.DateLastStatusUpdate = source.DateLastStatusUpdate;
            poi.DataQualityLevel = source.DataQualityLevel;
            poi.DateCreated = source.DateCreated;

            if (source.SubmissionStatusType != null) poi.SubmissionStatus = SubmissionStatusType.FromDataModel(source.SubmissionStatusType);

            //load contributor details (basic not sensitive info)
            if (source.Contributor != null) poi.Contributor = User.BasicFromDataModel(source.Contributor);

            if (source.Connections != null)
            {
                if (source.Connections.Count > 0)
                {
                    poi.Connections = new List<Model.ConnectionInfo>();
                    foreach (var conn in source.Connections)
                    {
                        poi.Connections.Add(ConnectionInfo.FromDataModel(conn));
                    }
                }
            }

            //optionally load user comments
            if (loadUserComments && source.UserComments != null)
            {
                foreach (var comment in source.UserComments.OrderByDescending(cm=>cm.DateCreated))
                {
                    if (poi.UserComments == null) poi.UserComments = new List<Model.UserComment>();
                    Model.UserComment com = UserComment.FromDataModel(comment);
                    poi.UserComments.Add(com);
                }
            }

            if (loadMediaItems && source.MediaItems != null)
            {
                foreach (var mediaItem in source.MediaItems)
                {
                    if (poi.MediaItems == null) poi.MediaItems = new List<Model.MediaItem>();
                    poi.MediaItems.Add(MediaItem.FromDataModel(mediaItem));
                }
            }

            if (loadMetadataValues && source.MetadataValues != null)
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