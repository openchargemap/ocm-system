using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Http;
using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Validation.Complexity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OCM.API.Web.Models.GraphQL;

namespace OCM.API.Web.Controllers
{


    [Route("v4/[controller]")]
    [ApiController]
    public class GraphQLController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IDocumentExecuter _executer;
        private readonly IDocumentWriter _writer;
        private readonly ISchema _schema;

        /// See also https://github.com/graphql-dotnet/examples/blob/master/src/AspNetWebApi/WebApi/Controllers/GraphQLController.cs
        /// 
        public GraphQLController(ILogger<GraphQLController> logger, ISchema schema, IDocumentExecuter executer, IDocumentWriter writer)
        {
            _logger = logger;

            _executer = executer;
            _writer = writer;
            _schema = schema;
        }

       /* [HttpGet]
        public string Get()
        {

            var configPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var schemaDefinition = System.IO.File.ReadAllText(configPath + "/Templates/GraphQL/poi-schema.gql");

            var schema = Schema.For(schemaDefinition, _ =>
            {
                _.Types.Include<Query>();
            });

            var json = schema.Execute(_ =>
            {
                _.Query = "{ poi { id datecreated } }";
            });

            return json;
        }*/


        [HttpGet]
        public Task<IActionResult> GetAsync(HttpRequestMessage request)
        {
            return PostAsync(new GraphQLQuery { Query = "query poi { poi { ID dateCreated } }", Variables = null });
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(GraphQLQuery query)
        {
            var inputs = query.Variables.ToInputs();
            var queryToExecute = query.Query;
         
            var result = await _executer.ExecuteAsync(_ =>
            {
                _.Schema = _schema;
                _.Query = queryToExecute;
                _.OperationName = query.OperationName;
                _.Inputs = inputs;
                _.ComplexityConfiguration = new ComplexityConfiguration { MaxDepth = 15 };
                _.FieldMiddleware.Use<InstrumentFieldsMiddleware>();

            }).ConfigureAwait(false);

            var httpResult = result.Errors?.Count > 0
                ? HttpStatusCode.BadRequest
                : HttpStatusCode.OK;

            var json = await _writer.WriteToStringAsync(result);


            return Content(json);
        }
    }
}