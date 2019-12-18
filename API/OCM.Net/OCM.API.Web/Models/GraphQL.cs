using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using OCM.API.Common.Model;

namespace OCM.API.Web.Models.GraphQL
{

    public class GraphQLQuery
    {
        public string OperationName { get; set; }
        public string Query { get; set; }
        public Newtonsoft.Json.Linq.JObject Variables { get; set; }
        /*
                [GraphQLMetadata("poi")]
                public IEnumerable<Jedi> GetJedis()
                {
                    return StarWarsDB.GetJedis();
                }*/

        [GraphQLMetadata("poi")]
        public OCM.API.Common.Model.ChargePoint GetPoi()
        {
            return new Common.Model.ChargePoint { ID = 1234, AddressInfo = new Common.Model.AddressInfo { Title = "Test" } };
        }
    }


    public class PoiSchema : Schema
    {
        public PoiSchema(IDependencyResolver resolver) :
                base(resolver)
        {
            Query = resolver.Resolve<PoiQuery>();
          
        }
    }

    public interface IPoiRepository
    {
        public OCM.API.Common.Model.ChargePoint GetPoi(int id);
        public IEnumerable<OCM.API.Common.Model.ChargePoint> GetPoiList();
    }

    public class PoiRepository : IPoiRepository
    {
        private IEnumerable<OCM.API.Common.Model.ChargePoint> _pois = new List<OCM.API.Common.Model.ChargePoint>
    {
            new  Common.Model.ChargePoint
            {
                ID = 1,
                AddressInfo = new Common.Model.AddressInfo{ Title= "Larry" },
            },
            new  Common.Model.ChargePoint
            {
                ID = 2,
                AddressInfo = new Common.Model.AddressInfo{ Title= "Rupert" },
            }
    };

        public Common.Model.ChargePoint GetPoi(int id)
        {
            return _pois.FirstOrDefault(c => c.ID == id);
        }

        public IEnumerable<ChargePoint> GetPoiList()
        {
            return _pois;
        }
    }


    public class AddressInfoType : ObjectGraphType<OCM.API.Common.Model.AddressInfo>
    {
        public AddressInfoType()
        {
            
            Field(i => i.ID);
            Field(i => i.Title);
            

        }
    }

    public class PoiType : ObjectGraphType<OCM.API.Common.Model.ChargePoint>
    {
        public PoiType()
        {
            Field(i => i.ID);
            //Field(i => i.AddressInfo, );
           // Field(i => i.Connections);
          //  Field(i => i.DataProvider);
           // Field(i => i.OperatorInfo);

        }
    }

    public class PoiQuery : ObjectGraphType
    {
        private readonly IPoiRepository _poiRepository;
        public PoiQuery(IPoiRepository poiRespository)
        {
            _poiRepository = poiRespository;

            Field<ListGraphType<PoiType>>("pois", resolve: context => _poiRepository.GetPoiList());

            Field<PoiType>
                (
                    "poi",
                    arguments: new QueryArguments(new QueryArgument<IntGraphType> { Name = "id" }),
                    resolve: context => _poiRepository.GetPoi(context.GetArgument<int>("id"))
                  );
        }
    }

}
