using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using OCM.API.Common;
using OCM.API.Common.Model;

namespace OCM.API.OutputProviders
{
    interface IOutputProvider
    {
        string ContentType { get; set; }
        void GetOutput(Stream outputStream, List<OCM.API.Common.Model.ChargePoint> dataList, APIRequestParams settings);
        void GetOutput(Stream outputStream, CoreReferenceData data, APIRequestParams settings);
        void GetOutput(Stream outputStream, Object data, APIRequestParams settings);
    }
}