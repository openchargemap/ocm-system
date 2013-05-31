using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using OCM.API.Common;
using OCM.API.Common.Model;

namespace OCM.API.Test
{
    public partial class TestDataSimilarity : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var evseManager = new POIManager();
            var testPoint = evseManager.Get(3400);

            if (testPoint != null)
            {
                List<ChargePoint> sourceList = new List<ChargePoint>();
                sourceList.Add(testPoint);
                GridViewSource.DataSource = sourceList;
                GridViewSource.DataBind();

                var similarPoints = evseManager.FindSimilar(testPoint);
                
                GridViewSimilar.DataSource = similarPoints;
                GridViewSimilar.DataBind();
            }
            else
            {
            }

        }
    }
}