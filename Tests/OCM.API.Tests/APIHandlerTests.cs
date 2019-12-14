using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OCM.API.Web.Standard;
using OCM.Core.Settings;
using Xunit;

namespace OCM.API.Tests
{
    public class ApiHandlerTests
    : IClassFixture<WebApplicationFactory<OCM.API.Web.Standard.Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public ApiHandlerTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
            _factory.Server.AllowSynchronousIO = true;
            
            // refresh POI cache status before beginning tests
            _ = Core.Data.CacheManager.RefreshCachedData(Core.Data.CacheUpdateStrategy.Modified).Result;
        }

        [Theory]
        [InlineData("/referencedata")]
        [InlineData("/v2/poi")]
        [InlineData("/v3/poi")]
        [InlineData("/v2/referencedata")]
        [InlineData("/v3/referencedata")]
        public async Task Get_EndpointsReturnSuccessAndJsonContentType(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299

            Assert.Equal("application/json",
                response.Content.Headers.ContentType.ToString());

            /*Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());*/
        }


        [Theory]
        [InlineData("/v4/referencedata")]
        [InlineData("/v4/poi")]
        [InlineData("/v4/system/status")]
        public async Task Get_V4EndpointsReturnSuccessAndJsonContentType(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299

            Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/v2/poi?output=xml")]
        public async Task Get_EndpointsReturnSuccessAndXmlContentType(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299

            Assert.Equal("text/xml",
                response.Content.Headers.ContentType.ToString());

            /*Assert.Equal("application/json; charset=utf-8",
                response.Content.Headers.ContentType.ToString());*/
        }
    }
}
