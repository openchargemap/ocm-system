using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OCM.API.Common;
using OCM.API.Common.Model;
using System.Threading.Tasks;

namespace OCM.API.InputProviders
{
    public class HTMLFormInputProvider : InputProviderBase, IInputProvider
    {
        public bool ProcessEquipmentSubmission(HttpContext context, ref OCM.API.Common.Model.ChargePoint cp)
        {
            return false; //html input provider no longer supported

            /*
            //construct simple model view of new CP based on input, to then be used to contruct data model version
            cp.AddressInfo = new Common.Model.AddressInfo();

            cp.AddressInfo.Title = ParseString(context.Request["ocm_loc_title"]);
            cp.AddressInfo.AddressLine1 = ParseString(context.Request["ocm_loc_addressline1"]);
            cp.AddressInfo.AddressLine2 = ParseString(context.Request["ocm_loc_addressline2"]);
            cp.AddressInfo.Town = ParseString(context.Request["ocm_loc_town"]);
            cp.AddressInfo.StateOrProvince = ParseString(context.Request["ocm_loc_stateorprovince"]);
            cp.AddressInfo.Postcode = ParseString(context.Request["ocm_loc_postcode"]);

            cp.AddressInfo.Latitude = (double)ParseDouble(context.Request["ocm_loc_latitude"]);
            cp.AddressInfo.Longitude = (double)ParseDouble(context.Request["ocm_loc_longitude"]);

            int? countryID = ParseInt(context.Request["ocm_loc_countryid"]);
            if (countryID != null) cp.AddressInfo.Country = new Common.Model.Country() { ID = (int)countryID };

            cp.AddressInfo.AccessComments = ParseLongString(context.Request["ocm_loc_accesscomments"]);
            cp.AddressInfo.ContactTelephone1 = ParseString(context.Request["ocm_loc_contacttelephone1"]);
            cp.AddressInfo.ContactEmail = ParseString(context.Request["ocm_loc_contactemail"]);

            //build connection info list 
            //connection 1 to n

            cp.Connections = new List<Common.Model.ConnectionInfo>();
            for (int i = 1; i <= 2; i++)
            {
                var connectionInfo = new Common.Model.ConnectionInfo();

                int? connectionTypeID = ParseInt(context.Request["ocm_cp_connection" + i + "_type"]);
                if (connectionTypeID != null)
                {
                    //TODO: remove/retire or remove entity reference for reference data lookup
                    var connectionType = new OCM.Core.Data.OCMEntities().ConnectionTypes.First(ct => ct.ID == (int)connectionTypeID);
                    connectionInfo.ConnectionType = OCM.API.Common.Model.Extensions.ConnectionType.FromDataModel(connectionType);
                }

                int? chargerLevelID = ParseInt(context.Request["ocm_cp_connection" + i + "_level"]);
                if (chargerLevelID != null)
                {
                    connectionInfo.Level = new Common.Model.ChargerType() { ID = (int)chargerLevelID };
                }

                connectionInfo.Amps = ParseInt(context.Request["ocm_cp_connection" + i + "_amps"]);
                connectionInfo.Voltage = ParseInt(context.Request["ocm_cp_connection" + i + "_volts"]);

                if (connectionInfo != null)
                {
                    cp.Connections.Add(connectionInfo);
                }
            }

            cp.NumberOfPoints = ParseInt(context.Request["ocm_cp_numberofpoints"]);

            int? usageTypeID = ParseInt(context.Request["ocm_cp_usagetype"]);
            if (usageTypeID != null)
            {
                cp.UsageType = new Common.Model.UsageType() { ID = (int)usageTypeID };
            }

            cp.UsageCost = ParseString(context.Request["ocm_cp_usagecost"]);

            int? statusTypeID = ParseInt(context.Request["ocm_cp_statustype"]);
            if (statusTypeID != null)
            {
                cp.StatusType = new Common.Model.StatusType() { ID = (int)statusTypeID };
            }

            cp.GeneralComments = ParseLongString(context.Request["ocm_cp_generalcomments"]);

            return true;
             * */
        }


        public bool ProcessUserCommentSubmission(HttpContext context, ref Common.Model.UserComment comment)
        {
            //not implemented
            return false;
        }

        public bool ProcessContactUsSubmission(HttpContext context, ref ContactSubmission contactSubmission)
        {
            //not implemented
            return false;
        }

        public bool ProcessMediaItemSubmission(HttpContext context, ref MediaItem mediaItem, int userId)
        {

            try
            {
                var files = context.Request.Files;
                string filePrefix = DateTime.UtcNow.Millisecond.ToString() + "_";
                int chargePointId = int.Parse(context.Request["id"]);
                string comment = context.Request["comment"];
                var tempFiles = new List<string>();

                string tempFolder = context.Server.MapPath("~/temp/uploads/");
                foreach (string file in context.Request.Files)
                {
                    var postedFile = context.Request.Files[file];
                    if (postedFile != null && postedFile.ContentLength > 0)
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
                        var photoAdded = mediaManager.AddPOIMediaItem(tempFolder, tmpFile, chargePointId, comment, false, userId);
                    }

                }, TaskCreationOptions.LongRunning);

                return true;

            }
            catch
            {
                return false;
            }
        }
    }
}