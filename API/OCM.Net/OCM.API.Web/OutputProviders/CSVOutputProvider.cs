using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

namespace OCM.API.OutputProviders
{
    public class CSVOutputProvider : OutputProviderBase, IOutputProvider
    {
        StreamWriter output = null;
        string currentLine = "";

        public CSVOutputProvider()
        {
            ContentType = "text/csv; header=present; charset=UTF-8;";
           
        }

        private void AppendText(string val)
        {

            if (val == null) currentLine += ",";
            else currentLine += "\"" + val.Replace("\"", "").Trim() + "\"" + ",";
        }

        private void AppendValue(object val)
        {
            if (val == null) currentLine += ",";
            else currentLine += val.ToString() + ",";
        }

        public void GetOutput(System.IO.Stream outputStream, List<Common.Model.ChargePoint> dataList, Common.APIRequestSettings settings)
        {
            output = new StreamWriter(outputStream, System.Text.Encoding.UTF8);

            System.Web.HttpContext.Current.Response.AddHeader("Content-Disposition", "attachment; filename=ocm.csv");

            //header row
            output.WriteLine("ID,UUID, LocationTitle,AddressLine1,AddressLine2,Town,StateOrProvince,Postcode,Country,Latitude,Longitude,Distance,DistanceUnit,Addr_ContactTelephone1,Addr_ContactTelephone2,Addr_ContactEmail,Addr_AccessComments,Addr_GeneralComments,Addr_RelatedURL,ConnectionType,ChargerType,UsageType,NumberOfPoints,GeneralComments,DateLastConfirmed,StatusType,DateLastStatusUpdate");
            foreach (var item in dataList)
            {
                if (item.AddressInfo != null)
                {
                    currentLine = "";
                    AppendValue(item.ID);
                    AppendValue(item.UUID);
                    AppendText(item.AddressInfo.Title);
                    AppendText(item.AddressInfo.AddressLine1);
                    AppendText(item.AddressInfo.AddressLine2);
                    AppendText(item.AddressInfo.Town);
                    AppendText(item.AddressInfo.StateOrProvince);
                    AppendText(item.AddressInfo.Postcode);

                    if (item.AddressInfo.Country != null)
                        AppendText(item.AddressInfo.Country.Title);
                    else
                        AppendValue(null);

                    AppendValue(item.AddressInfo.Latitude);
                    AppendValue(item.AddressInfo.Longitude);
                    AppendValue(item.AddressInfo.Distance);
                    AppendValue(item.AddressInfo.DistanceUnit);
                    AppendText(item.AddressInfo.ContactTelephone1);
                    AppendText(item.AddressInfo.ContactTelephone2);
                    AppendText(item.AddressInfo.ContactEmail);
                    AppendText(item.AddressInfo.AccessComments);
#pragma warning disable 0612
                    AppendText(item.AddressInfo.GeneralComments);
#pragma warning restore 0612
                    AppendText(item.AddressInfo.RelatedURL);

                    //ConnectionType,ChargerType,UsageType,NumberOfPoints,GeneralComments,DateLastConfirmed,StatusType,DateLastStatusUpdate
                    bool appendedText = false;
                    if (item.Connections != null)
                    {
                        //compile semicolon seperated items for output within column
                        string itemList = "";
                        foreach (var ct in item.Connections)
                        {
                            if (ct.ConnectionType != null)
                            {
                                itemList += ct.ConnectionType.Title + "; ";
                                appendedText = true;
                            }
                        }

                        //output null placeholder or column values
                        if (!appendedText)
                        {
                            AppendValue(null);
                        }
                        else
                        {
                            AppendText(itemList);
                        }
                    }
                    else
                    {
                        AppendValue(null);
                    }

#pragma warning disable 0612
                    if (item.Chargers != null)
                    {
                        appendedText = false;

                        //compile semicolon seperated items for output within column
                        string itemList = "";
                        foreach (var cg in item.Chargers)
                        {
                            if (cg.ChargerType != null)
                            {
                                itemList += cg.ChargerType.Title + "; ";
                                appendedText = true;
                            }
                        }

                        //output null placeholder or column values
                        if (!appendedText)
                        {
                            AppendValue(null);
                        }
                        else
                        {
                            AppendText(itemList);
                        }
                    }
                    else
                    {
                        AppendValue(null);
                    }

                    if (item.UsageType != null)
                        AppendText(item.UsageType.Title);
                    else
                        AppendValue(null);

                    AppendValue(item.NumberOfPoints);
                    AppendText(item.GeneralComments);
                    AppendValue(item.DateLastConfirmed.ToString());

                    if (item.StatusType != null)
                        AppendText(item.StatusType.Title);
                    else
                        AppendValue(null);

                    //last item
                    currentLine += item.DateLastStatusUpdate.ToString();

                    currentLine = currentLine.Replace(System.Environment.NewLine, " ");
                    output.WriteLine(currentLine);
                }

            }
#pragma warning restore 0612
            output.Flush();
            //output.Close();
        }


        public void GetOutput(Stream outputStream, Common.Model.CoreReferenceData data, Common.APIRequestSettings settings)
        {
            throw new NotImplementedException();
        }

        public void GetOutput(Stream outputStream, Object data, Common.APIRequestSettings settings)
        {
            throw new NotImplementedException();
        }
    }
}