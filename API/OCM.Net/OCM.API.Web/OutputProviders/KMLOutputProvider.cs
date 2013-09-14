using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Text;

namespace OCM.API.OutputProviders
{
    public class KMLOutputProvider : OutputProviderBase, IOutputProvider
    {
        public KMLOutputProvider()
        {
            ContentType = "application/vnd.google-earth.kml+xml";
        }


        public void GetOutput(System.IO.Stream outputStream, List<Common.Model.ChargePoint> dataList, Common.APIRequestSettings settings)
        {
            /*<Document>
                  <Placemark>
                    <name>Sacramento County Parking Garage</name>
                    <description>1 SP Inductive  1 Avcon Conductive</description>
                    <Point>
                      <coordinates>-121.49610000,38.58460000</coordinates>
                    </Point>
                  </Placemark>
                  <Placemark>
                    <name>Sacramento City Public Parking Garage</name>
                    <description>3 SP Inductive</description>
                    <Point>
                      <coordinates>-121.49382900,38.57830300</coordinates>
                    </Point>
                  </Placemark>
             </Document>
             * */
            XmlTextWriter xml = new XmlTextWriter(outputStream, Encoding.UTF8);

            //start xml document
            xml.WriteStartDocument();
            xml.WriteStartElement("Document");
            xml.WriteElementString("name","Open Charge Map - Electric Vehicle Charging Locations");
            xml.WriteElementString("description", "Data from http://openchargemap.org/");
            foreach (var item in dataList)
            {
                if (item.AddressInfo != null)
                {
                    xml.WriteStartElement("Placemark");
                    xml.WriteElementString("name", item.AddressInfo.Title);

                    xml.WriteElementString("description", item.GetSummaryDescription(true));
                    if (item.AddressInfo.Latitude != null)
                    {
                        xml.WriteStartElement("Point");
                        string coords = item.AddressInfo.Longitude.ToString() + "," + item.AddressInfo.Latitude.ToString();
                        xml.WriteElementString("coordinates", coords);
                        xml.WriteEndElement();
                    }

                    xml.WriteEndElement();
                }
            }
            xml.WriteEndElement();
            xml.WriteEndDocument();
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