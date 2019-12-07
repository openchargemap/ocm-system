using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OCM.API.Worker
{
    public class Startup : OCM.API.Web.Standard.Startup
    {
        public Startup(IConfiguration configuration):base(configuration)
        {
            
        }
    }
}
