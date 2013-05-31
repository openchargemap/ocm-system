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
        void GetOutput(Stream outputStream, List<OCM.API.Common.Model.ChargePoint> dataList, SearchFilterSettings settings);
        void GetOutput(Stream outputStream, CoreReferenceData data, SearchFilterSettings settings);
    }
}