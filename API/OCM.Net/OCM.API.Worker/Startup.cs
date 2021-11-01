using Microsoft.Extensions.Configuration;

namespace OCM.API.Worker
{
    public class Startup : OCM.API.Web.Standard.Startup
    {
        public Startup(IConfiguration configuration) : base(configuration)
        {

        }
    }
}
