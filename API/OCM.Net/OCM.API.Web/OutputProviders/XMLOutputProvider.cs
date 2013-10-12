using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Text;

namespace OCM.API.OutputProviders
{
    public class XMLOutputProvider : OutputProviderBase, IOutputProvider
    {
        public XMLOutputProvider()
        {
            ContentType = "text/xml";
        }

        public void GetOutput(System.IO.Stream outputStream, List<Common.Model.ChargePoint> dataList, Common.APIRequestSettings settings)
        {
            bool isVerboseMode = true;

            XmlTextWriter xml = new XmlTextWriter(outputStream, Encoding.UTF8);

            //start xml document
            xml.WriteStartDocument();
            xml.WriteStartElement("ChargePoints");

            foreach (var item in dataList)
            {
                xml.WriteStartElement("ChargePoint");
                xml.WriteAttributeString("ID", item.ID.ToString());
                xml.WriteAttributeString("UUID", item.UUID.ToString());
                xml.WriteAttributeString("DateLastConfirmed", item.DateLastConfirmed.ToString());
                if (item.OperatorInfo != null)
                {
                    xml.WriteAttributeString("OperatorID", item.OperatorInfo.ID.ToString());
                    if (isVerboseMode)
                    {
                        xml.WriteAttributeString("OperatorTitle", item.OperatorInfo.Title.ToString());
                    }
                }
                if (item.DataProvider != null) xml.WriteAttributeString("DataProviderID", item.DataProvider.ID.ToString());
                xml.WriteAttributeString("NumberOfPoints", item.NumberOfPoints.ToString());
                xml.WriteAttributeString("DataQualityLevel", item.DataQualityLevel.ToString());
                xml.WriteAttributeString("PercentageSimilarity", item.PercentageSimilarity.ToString());
                xml.WriteElementString("GeneralComments", item.GeneralComments);

                //output address info
                if (item.AddressInfo != null)
                {
                    xml.WriteStartElement("AddressInfo");
                    xml.WriteElementString("LocationTitle", item.AddressInfo.Title);
                    xml.WriteElementString("AddressLine1", item.AddressInfo.AddressLine1);
                    xml.WriteElementString("AddressLine2", item.AddressInfo.AddressLine2);
                    xml.WriteElementString("Town", item.AddressInfo.Town);
                    xml.WriteElementString("StateOrProvince", item.AddressInfo.StateOrProvince);
                    xml.WriteElementString("Postcode", item.AddressInfo.Postcode);

                    if (item.AddressInfo.Country != null) xml.WriteElementString("Country", item.AddressInfo.Country.Title);
                    if (item.AddressInfo.Latitude != null) xml.WriteElementString("Latitude", item.AddressInfo.Latitude.ToString());
                    if (item.AddressInfo.Longitude != null) xml.WriteElementString("Longitude", item.AddressInfo.Longitude.ToString());

                    if (item.AddressInfo.Distance!=null)
                    {
                        xml.WriteStartElement("Distance");
                        xml.WriteAttributeString("Unit", item.AddressInfo.DistanceUnit.ToString());
                        xml.WriteAttributeString("Value", item.AddressInfo.Distance.ToString());
                        xml.WriteEndElement();
                    }
                    
                    if (item.AddressInfo.ContactTelephone1!=null) xml.WriteElementString("ContactTelephone1", item.AddressInfo.ContactTelephone1);
                    if (item.AddressInfo.ContactTelephone2 != null) xml.WriteElementString("ContactTelephone2", item.AddressInfo.ContactTelephone2);
                    if (item.AddressInfo.ContactEmail != null) xml.WriteElementString("ContactEmail", item.AddressInfo.ContactEmail);
                    if (item.AddressInfo.AccessComments != null) xml.WriteElementString("AccessComments", item.AddressInfo.AccessComments);
#pragma warning disable 0612
                    if (item.AddressInfo.GeneralComments != null) xml.WriteElementString("GeneralComments", item.AddressInfo.GeneralComments);
#pragma warning restore 0612
                    if (item.AddressInfo.RelatedURL != null) xml.WriteElementString("RelatedURL", item.AddressInfo.RelatedURL);

                    xml.WriteEndElement();
                }

                if (item.Connections != null)
                {
                    xml.WriteStartElement("Connections");
                    foreach (var ct in item.Connections)
                    {
                        xml.WriteStartElement("ConnectionInfo");
                        xml.WriteAttributeString("ID", ct.ID.ToString());
                        if (ct.ConnectionType != null)
                        {
                            xml.WriteAttributeString("TypeID", ct.ConnectionType.ID.ToString());
                            xml.WriteAttributeString("TypeName", ct.ConnectionType.Title);
                        }
                        
                        if (ct.Level != null)
                        {
                            xml.WriteAttributeString("LevelID", ct.Level.ID.ToString());
                            xml.WriteAttributeString("LevelName", ct.Level.Title);
                        }

                        if (ct.StatusType != null)
                        {
                            xml.WriteAttributeString("Status", ct.StatusType.Title);
                        }

                        if (ct.Amps!=null)
                        {
                            xml.WriteAttributeString("Amps", ct.Amps.ToString());
                        }

                        if (ct.Voltage != null)
                        {
                            xml.WriteAttributeString("Voltage", ct.Voltage.ToString());
                        }

                        if (ct.Quantity != null)
                        {
                            xml.WriteAttributeString("Quantity", ct.Quantity.ToString());
                        }

                        if (ct.Reference != null)
                        {
                            xml.WriteAttributeString("Reference", ct.Reference.ToString());
                        }
                        xml.WriteEndElement();
                    }
                    xml.WriteEndElement();
                }
#pragma warning disable 0612
                var chargerList = item.Chargers;
                if (chargerList==null || chargerList.Count==0){
                    if (item.Connections != null)
                    {
                        chargerList = new List<Common.Model.ChargerInfo>();
                        foreach(var con in item.Connections)
                        {
                            if (con.Level != null)
                            {
                                if (!chargerList.Exists(c => c.ChargerType == con.Level))
                                {
                                    chargerList.Add(new Common.Model.ChargerInfo() { ChargerType = con.Level });
                                }
                            }
                        }
                        chargerList = chargerList.Distinct().ToList();
                        
                    }
                }

#pragma warning restore 0612

                if (chargerList != null)
                {
                    xml.WriteStartElement("ChargerTypes");
                    foreach (var cg in chargerList)
                    {
                        if (cg.ChargerType != null)
                        {   
                            xml.WriteStartElement("ChargerType");
                            xml.WriteAttributeString("ID", cg.ChargerType.ID.ToString());
                            if (isVerboseMode)
                            {
                                xml.WriteAttributeString("Title", cg.ChargerType.Title);
                                xml.WriteAttributeString("IsFastChargeCapable", cg.ChargerType.IsFastChargeCapable.ToString());
                            }
                            xml.WriteEndElement();
                        }
                    }
                    xml.WriteEndElement();
                }
                
                if (item.UsageType != null)
                {
                    xml.WriteStartElement("UsageType");
                    xml.WriteAttributeString("ID", item.UsageType.ID.ToString());
                    if (isVerboseMode)
                    {
                        xml.WriteValue(item.UsageType.Title);
                    }
                    xml.WriteEndElement();
                }

                if (!String.IsNullOrEmpty(item.UsageCost))
                {
                    xml.WriteElementString("UsageCost", item.UsageCost);
                }

                /*if (!String.IsNullOrEmpty(item.MetadataTags))
                {
                    xml.WriteStartElement("MetadataTags");
                    xml.WriteCData(item.MetadataTags);
                    xml.WriteEndElement();
                }*/

                if (item.StatusType != null)
                {
                    xml.WriteStartElement("StatusType");
                    xml.WriteAttributeString("ID", item.StatusType.ID.ToString());
                    if (isVerboseMode)
                    {
                        xml.WriteAttributeString("Title", item.StatusType.Title);
                    }
                    xml.WriteAttributeString("DateLastStatusUpdate", item.DateLastStatusUpdate.ToString());
                    xml.WriteEndElement(); //end status
                }

                if (item.UserComments != null)
                {
                    xml.WriteStartElement("UserComments");
                    foreach (var comment in item.UserComments)
                    {
                        xml.WriteStartElement("UserComment");
                        xml.WriteAttributeString("CommentTypeID", comment.CommentType.ID.ToString());
                        if (isVerboseMode)
                        {
                            xml.WriteAttributeString("CommentType", comment.CommentType.Title);
                        }
                        xml.WriteAttributeString("UserName", comment.UserName);
                        xml.WriteAttributeString("Rating", comment.Rating.ToString());
                        xml.WriteAttributeString("RelatedURL", comment.RelatedURL);
                        xml.WriteValue(comment.Comment);
                        xml.WriteEndElement();
                    }
                    xml.WriteEndElement();
                }
                xml.WriteEndElement(); //end charge point
            }

            xml.WriteEndElement(); //end charge points list
            xml.WriteEndDocument(); //end xml
            xml.Flush();
            //xml.Close();
        }

        public void GetOutput(System.IO.Stream outputStream, Common.Model.CoreReferenceData data, Common.APIRequestSettings settings)
        {
            throw new NotImplementedException();
        }

        public void GetOutput(System.IO.Stream outputStream, Object data, Common.APIRequestSettings settings)
        {
            throw new NotImplementedException();
        }
    }
}