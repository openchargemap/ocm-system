using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Text;
using System.IO;
using OCM.API.Common;

namespace OCM.API.OutputProviders
{
    /// <summary>
    /// Nissan CARWINGS format rss output, see: http://lab.nissan-carwings.com/CWL/Spec.cgi
    /// </summary>
    public class RSSOutputProvider : OutputProviderBase, IOutputProvider
    {
        public bool EnableCarwingsMode { get; set; }
        public bool EnableGeoRSS { get; set; }

        public RSSOutputProvider()
        {
            ContentType = "text/xml";
            EnableCarwingsMode = false;
            EnableGeoRSS = true;
        }

        public void GetOutput(Stream outputStream, List<Common.Model.ChargePoint> dataList, APIRequestSettings settings)
        {
            XmlTextWriter xml = new XmlTextWriter(outputStream, Encoding.UTF8);

            //start xml document
            xml.WriteStartDocument();

            //output rss tags
            xml.WriteStartElement("rss");
            xml.WriteAttributeString("version", "2.0");

            if (EnableCarwingsMode) xml.WriteAttributeString("xmlns", "carwings", "http://www.w3.org/2000/xmlns/", "http://www.nissan.co.jp/dtd/carwings.dtd");
            if (EnableGeoRSS) xml.WriteAttributeString("xmlns", "georss", "http://www.w3.org/2000/xmlns/", "http://www.georss.org/georss/10");

            //output feed details
            xml.WriteStartElement("channel");
            xml.WriteElementString("title", "OpenChargeMap Charge Points");
            xml.WriteElementString("link", "http://openchargemap.org");
            xml.WriteElementString("description", "Charge Point data contributed by OpenChargeMap community");
            xml.WriteElementString("copyright", "openchargemap.org");

            //output feed items
            foreach (var item in dataList)
            {
                if (item.AddressInfo != null)
                {
                    xml.WriteStartElement("item");
                    xml.WriteStartElement("guid");
                    xml.WriteAttributeString("isPermaLink","false");
                    xml.WriteString(item.UUID);
                    xml.WriteEndElement();

                    xml.WriteElementString("title", item.AddressInfo.Title);

                    if (EnableCarwingsMode)
                    {
                        xml.WriteElementString("carwings:readtitle", item.AddressInfo.Title);
                    }

                    string description = item.GetSummaryDescription(true);
                    string address = item.GetAddressSummary(true);

                    xml.WriteElementString("description", description);

                    if (item.AddressInfo.RelatedURL != null)
                    {
                        xml.WriteElementString("link", item.AddressInfo.RelatedURL);
                    }

                    if (EnableCarwingsMode)
                    {
                        if (address != "") xml.WriteElementString("carwings:address", address);
                        if (item.AddressInfo.ContactTelephone1 != null) xml.WriteElementString("carwings:tel", item.AddressInfo.ContactTelephone1);
                        if (item.AddressInfo.Latitude != null)
                        {
                            xml.WriteElementString("carwings:lat", item.AddressInfo.Latitude.ToString());
                            xml.WriteElementString("carwings:lon", item.AddressInfo.Longitude.ToString());
                        }
                    }

                    if (EnableGeoRSS)
                    {
                        xml.WriteElementString("georss:point", item.AddressInfo.Latitude.ToString()+" "+item.AddressInfo.Longitude.ToString());
                    }

                    xml.WriteEndElement();
                }
            }

            xml.WriteEndElement();  //end channel
            xml.WriteEndElement();  //end rss
            xml.WriteEndDocument(); //end xml
            xml.Flush();
            //xml.Close();

            #region example rss
            /*
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0" xmlns:carwings="http://www.nissan.co.jp/dtd/carwings.dtd">
              <channel>
                <title> WEB site update: NISSAN Topics </ title>
                <link> http://rss.nissan.co.jp/ </ link>
                <description> Nissan will deliver the latest information. </ Description>
                <language>en</ language>
                <copyright> Copyright NISSAN MOTOR CO., LTD. 2005 All Rights Reserved. </ copyright>
                <lastBuildDate> Wed, 25 Oct 2006 12:18:36 +0900 </ lastBuildDate>
                <docs> http://blogs.law.harvard.edu/tech/rss </ docs> 
                <item>
	            <title>update information</ title>
    	            <carwings:readtitle> Trail Jam update information </ carwings: readtitle>
    	            <description> X-TRAIL JAM IN TOKYO DOME general tickets on sale! ! </ Description>
    	            <carwings:readtext> Trail Jam Tokyo Dome tickets, general sale.</ Carwings: readtext>
	            <Carwings:itemimage> http://eplus.jp/sys/web/irg/2006x-trail/images/topmain.jpg </ Carwings: Itemimage>
	            <Carwings:data> <! [CDATA [
		            TRAIL <body> <br> <img src="http://lab.nissan-carwings.com/CWC/images/x-trail.jpg"> </ body>
		            ]]>
	            </ Carwings: data>
	            <carwings:lat> 35.70568 </ carwings: lat>
	            <carwings:lon> 139.75187 </ carwings: lon>
	            <link> http://blog.nissan.co.jp/X-TRAIL/?rss </ link>
	            <guid> http://blog.nissan.co.jp/X-TRAIL/?rss </ guid>
	            <Carwings:link> Http://Www.nissan.co.jp/Event/....../Coupon.html </ Carwings: Link>
	            <category> content </ category>
	            <pubDate> Mon, 16 Oct 2006 20:15:02 +0900 </ pubDate>
                </item>
              </channel>
            </rss>
             * */
            #endregion 

        }

        public void GetOutput(Stream outputStream, Common.Model.CoreReferenceData data, APIRequestSettings settings)
        {
            throw new NotImplementedException();
        }

        public void GetOutput(Stream outputStream, Object data, APIRequestSettings settings)
        {
            throw new NotImplementedException();
        }
    }
}