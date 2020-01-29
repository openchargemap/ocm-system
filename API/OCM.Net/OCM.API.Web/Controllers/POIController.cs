using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OCM.API.Common;
using OCM.API.Common.Model;

namespace OCM.API.Web.Standard.Controllers
{
    [ApiController]
    [Route("/v4/[controller]")]

    public class POIController : ControllerBase
    {
        private readonly ILogger _logger;

        public POIController(ILogger<POIController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<ChargePoint> Get()
        {

            // use custom query string parsing for compatibility
            var filter = new APIRequestParams();

            //set defaults
            var paramList = new NullSafeDictionary<string, string>();
            foreach (var k in Request.Query.Keys)
            {
                paramList.Add(k.ToLower(), Request.Query[k]);
            }

            filter.ParseParameters(filter, paramList);

            if (string.IsNullOrEmpty(filter.APIKey))
            {
                if (Request.Headers.ContainsKey("X-API-Key"))
                {
                    filter.APIKey = Request.Headers["X-API-Key"];
                }
            }

            var api = new POIManager();

            var list = api.GetPOIList(filter);

            return list;
        }

        [HttpGet]
        [Route("/v4/share/{id}")]
        public ContentResult Share(int id)
        {

            // use custom query string parsing for compatibility

            var api = new POIManager();


            var poi = api.GetFullDetails(id);

            if (poi != null)
            {
                var content = $"<meta http-equiv='refresh' content = '0;url=https://openchargemap.org/site/poi/details/{poi.ID}' />";

                content += $"<meta property='og: title' content='{poi.AddressInfo.Title} - OCM-{poi.ID}'>";

                if (poi.MediaItems != null && poi.MediaItems.Any())
                {
                    content += $"<meta property = 'og:image' content = '{poi.MediaItems[0].ItemThumbnailURL}' >";
                }

                content += $"<h1>{poi.AddressInfo.Title} - OCM-{poi.ID}</h1>";
               
                return base.Content(content, "text/html");

            }


            return base.Content("The location you have linked to does not currently exist. Please check your link and try again.", "text/html");
        }
    }
}
