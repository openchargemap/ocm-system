using System;
using System.Collections.Generic;
using System.Linq;

namespace OCM.API.Common.Model
{
    public class SubmissionStatusType : SimpleReferenceDataType
    {
        public bool? IsLive { get; set; }
    }
}