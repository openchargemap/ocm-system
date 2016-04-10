using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCM.API.Common.Model.Submissions
{
    public class MediaItemSubmission
    {
        public int ChargePointID { get; set; }
        public string Comment { get; set; }
        public string ImageDataBase64 { get; set; }
    }
}