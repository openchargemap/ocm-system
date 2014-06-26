using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;

namespace OCM.API.OutputProviders
{
    public class KMLOutputProvider : OutputProviderBase, IOutputProvider
    {
        public enum KMLVersion
        {
            V1,
            V2
        }

        private KMLVersion SelectedKMLVersion = KMLVersion.V2;

        public KMLOutputProvider(KMLVersion version)
        {
            ContentType = "application/vnd.google-earth.kml+xml";
            SelectedKMLVersion = version;
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
            if (this.SelectedKMLVersion== KMLVersion.V2) xml.WriteStartElement("kml", "http://www.opengis.net/kml/2.2");
            xml.WriteStartElement("Document");
            xml.WriteElementString("name","Open Charge Map - Electric Vehicle Charging Locations");
            xml.WriteElementString("description", "Data from http://openchargemap.org/");
            foreach (var item in dataList)
            {
                if (item.AddressInfo != null)
                {
                    xml.WriteStartElement("Placemark");
                    xml.WriteAttributeString("id", "OCM-"+item.ID.ToString());
                    xml.WriteElementString("name", item.AddressInfo.Title);
                    
                    //remove invalid character ranges before serializing to XML
                    var description = Regex.Replace(item.GetSummaryDescription(true), @"[^\u0009\u000a\u000d\u0020-\uD7FF\uE000-\uFFFD]", string.Empty);
                    xml.WriteStartElement("description");
                    xml.WriteCData(description);
                    xml.WriteEndElement();
                    
                    xml.WriteStartElement("Point");
                    string coords = item.AddressInfo.Longitude.ToString() + "," + item.AddressInfo.Latitude.ToString();
                    xml.WriteElementString("coordinates", coords);
                    xml.WriteEndElement();

                    xml.WriteEndElement();
                }
            }
            xml.WriteEndElement();
            if (this.SelectedKMLVersion == KMLVersion.V2) xml.WriteEndElement(); //</kml>
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