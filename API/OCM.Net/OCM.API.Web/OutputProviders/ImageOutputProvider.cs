using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Text;
using System.Drawing;

namespace OCM.API.OutputProviders
{
    public class ImageOutputProvider : OutputProviderBase, IOutputProvider
    {
        public ImageOutputProvider()
        {
            ContentType = "image/png";
        }


        public void GetOutput(System.IO.Stream outputStream, List<Common.Model.ChargePoint> dataList, Common.APIRequestSettings settings)
        {
            
            foreach (var item in dataList)
            {
                if (item.AddressInfo != null)
                {
                    
                   /* xml.WriteStartElement("Placemark");
                    xml.WriteElementString("name", item.AddressInfo.Title);

                    xml.WriteElementString("description", item.GetSummaryDescription(true));
                    if (item.AddressInfo.Latitude != null)
                    {
                        xml.WriteStartElement("Point");
                        string coords = item.AddressInfo.Longitude.ToString() + "," + item.AddressInfo.Latitude.ToString();
                        xml.WriteElementString("coordinates", coords);
                        xml.WriteEndElement();
                    }

                    
                    * */
                }
            }

            Image outputImage = null;
            outputImage.Save(outputStream, System.Drawing.Imaging.ImageFormat.Jpeg);
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