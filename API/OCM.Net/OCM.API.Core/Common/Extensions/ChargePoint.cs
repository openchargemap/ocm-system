using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model.Extensions
{
    public class ChargePoint
    {
        public static Model.ChargePoint FromDataModel(Core.Data.ChargePoint source)
        {
            return FromDataModel(source, false);
        }

        public static Model.ChargePoint FromDataModel(Core.Data.ChargePoint source, bool loadUserComments)
        {
            if (source == null) return null;

            Model.ChargePoint c = new Model.ChargePoint();

            c.ID = source.ID;
            c.UUID = source.UUID;

            //populate data provider info
            if (source.DataProvider != null)
            {
                c.DataProvider = DataProvider.FromDataModel(source.DataProvider);
            }
            c.DataProvidersReference = source.DataProvidersReference;

            if (source.Operator != null)
            {
                //TODO: populate operator address info
                c.OperatorInfo = OperatorInfo.FromDataModel(source.Operator);
            }
            c.OperatorsReference = source.OperatorsReference;

            if (source.UsageType != null)
            {
                c.UsageType = UsageType.FromDataModel(source.UsageType);
            }
            c.UsageCost = source.UsageCost;

            //populate address info
            if (source.AddressInfo != null)
            {
                c.AddressInfo = AddressInfo.FromDataModel(source.AddressInfo);
            }

            c.NumberOfPoints = source.NumberOfPoints;
            c.GeneralComments = source.GeneralComments;

            c.DatePlanned = source.DatePlanned; 
            c.DateLastConfirmed = source.DateLastConfirmed;
            if (source.StatusType != null)
            {
                c.StatusType = StatusType.FromDataModel(source.StatusType);
            }
            c.DateLastStatusUpdate = source.DateLastStatusUpdate; 
            c.DataQualityLevel = source.DataQualityLevel;
            c.DateCreated = source.DateCreated;
           
            if (source.SubmissionStatusType != null) c.SubmissionStatus = SubmissionStatusType.FromDataModel(source.SubmissionStatusType);

            //load contributor details (basic not sensitive info)
            if (source.Contributor != null) c.Contributor = User.BasicFromDataModel(source.Contributor);

            if (source.Connections != null)
            {
                if (source.Connections.Count > 0)
                {
                    c.Connections = new List<Model.ConnectionInfo>();
                    foreach (var conn in source.Connections)
                    {
                        c.Connections.Add(ConnectionInfo.FromDataModel(conn));
                    }
                }
            }
            
            //optionally load user comments
            if (loadUserComments && source.UserComments != null)
            {
                foreach (var comment in source.UserComments)
                {
                    if (c.UserComments == null) c.UserComments = new List<Model.UserComment>();
                    Model.UserComment com = UserComment.FromDataModel(comment);
                    c.UserComments.Add(com);
                }
            }

            if (source.MediaItems != null)
            {
                foreach (var mediaItem in source.MediaItems)
                {
                    if (c.MediaItems==null) c.MediaItems= new List<Model.MediaItem>();
                    c.MediaItems.Add(MediaItem.FromDataModel(mediaItem));
                }
            }
            return c;
        }
    }
}