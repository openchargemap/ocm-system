using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OCM.API.Tests
{
    /// <summary>
    /// Range of tests on filter parameters for POI retrieval
    /// </summary>
    public class TestPOIQuery
    {
        public TestPOIQuery()
        {
            Core.Data.CacheManager.RefreshCachedData(Core.Data.CacheUpdateStrategy.Modified).Wait();

        }
        [Fact]
        public void FilterOnChargePointID()
        {
            var api = new OCM.API.Common.POIManager();
            var results = api.GetPOIList(new Common.APIRequestParams { AllowMirrorDB = true, ChargePointIDs = new int[] { 23190 } });
            Assert.True(results.Count == 1, "Should return a single result");

            var poi = results.FirstOrDefault();
            Assert.NotNull(poi);

            Assert.True(poi.ID == 23190, "ID should match");
            Assert.True(poi.AddressInfo.CountryID == 112, "Country should be Itally"); // Italy
        }

        [Fact]
        public void ReturnPOIResults()
        {
            var api = new OCM.API.Common.POIManager();
            var results = api.GetPOIList(new Common.APIRequestParams { EnableCaching = false, IsVerboseOutput = false });
            Assert.True(results.Count > 0);

            var poi = results.FirstOrDefault();
            Assert.NotNull(poi);
        }

        [Fact]
        public void ReturnPOIResultsAroundPointWithDistanceLimit()
        {
            var api = new OCM.API.Common.POIManager();

            // mongodb cache results
            var searchParams = new Common.APIRequestParams
            {
                AllowMirrorDB = true,
                AllowDataStoreDB = false,
                IsVerboseOutput = false,
                DistanceUnit = Common.Model.DistanceUnit.Miles,
                Distance = 5,
                Latitude = 51.5077,
                Longitude = -0.134
            };

            var cacheResults = api.GetPOIList(searchParams);
            Assert.True(cacheResults.Count > 0);

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);
            Assert.True(poi.AddressInfo.Distance > 0, "Distance should be more than zero");

            Assert.True(AreResultsNearPolyline(
                new List<Common.LatLon> { new Common.LatLon { Latitude = 51.507729, Longitude = -0.13457 } },
                searchParams,
                cacheResults),
                "One or more POIs in result set are not near the polyline");

            // database results
            searchParams = new Common.APIRequestParams
            {
                AllowMirrorDB = false,
                AllowDataStoreDB = true,
                IsVerboseOutput = false,
                DistanceUnit = Common.Model.DistanceUnit.Miles,
                Distance = 5,
                Latitude = 51.5077,
                Longitude = -0.134
            };
            var dbresults = api.GetPOIList(searchParams);
            Assert.True(dbresults.Count > 0);

            poi = dbresults.FirstOrDefault();
            Assert.NotNull(poi);
            Assert.True(poi.AddressInfo.Distance > 0, "Distance should be more than zero");

            Assert.True(AreResultsNearPolyline(
                new List<Common.LatLon> { new Common.LatLon { Latitude = 51.507729, Longitude = -0.13457 } },
                searchParams,
                cacheResults),
                "One or more POIs in result set are not near the polyline");

            Assert.True(cacheResults.Count == dbresults.Count, "Cached results and db results should be the same");
        }

        [Fact]
        public void ReturnPOIResultsAroundPointNoDistanceLimit()
        {
            var api = new OCM.API.Common.POIManager();

            // mongodb cache results
            var searchParams = new Common.APIRequestParams
            {
                AllowMirrorDB = true,
                AllowDataStoreDB = false,
                IsVerboseOutput = false,
                DistanceUnit = Common.Model.DistanceUnit.Miles,
                Distance = null,
                MaxResults = 20,
                Latitude = 51.5077,
                Longitude = -0.134
            };

            var cacheResults = api.GetPOIList(searchParams);
            Assert.True(cacheResults.Count > 0);

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);
            Assert.True(poi.AddressInfo.Distance > 0, "Distance should be more than zero");

            // database results
            searchParams = new Common.APIRequestParams
            {
                AllowMirrorDB = false,
                AllowDataStoreDB = true,
                IsVerboseOutput = false,
                DistanceUnit = Common.Model.DistanceUnit.Miles,
                Distance = null,
                MaxResults = 20,
                Latitude = 51.5077,
                Longitude = -0.134
            };
            var dbresults = api.GetPOIList(searchParams);
            Assert.True(dbresults.Count > 0);

            poi = dbresults.FirstOrDefault();
            Assert.NotNull(poi);

            Assert.True(poi.AddressInfo.Distance > 0, "Distance should be more than zero");

            Assert.True(cacheResults.Count == dbresults.Count, "Cached results and db results should be the same");
        }

        [Fact]
        public void ReturnPOIResultsInBoundingBox()
        {
            var api = new OCM.API.Common.POIManager();

            // mongodb cache results
            var searchParams = new Common.APIRequestParams
            {
                AllowMirrorDB = true,
                AllowDataStoreDB = false,
                IsVerboseOutput = false,
                IsCompactOutput = true,
                MaxResults = 100000,
                BoundingBox = new List<Common.LatLon> {
                    new Common.LatLon { Latitude= 51.45580747856863, Longitude = -0.27510331130017107 } ,
                    new Common.LatLon { Latitude = 51.53701250046424, Longitude =  0.009162470112983101 }
                }
            };

            var cacheResults = api.GetPOIList(searchParams);
            Assert.True(cacheResults.Count > 0);

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);

            // ensure all points are within the bounding box
            var bbox = Core.Util.PolylineEncoder.ConvertPointsToBoundingBox(searchParams.BoundingBox);

            foreach (var p in cacheResults)
            {
                Assert.True(bbox.Intersects(new NetTopologySuite.Geometries.Point(p.AddressInfo.Longitude, p.AddressInfo.Latitude)));
            }

            // database results
            searchParams = new Common.APIRequestParams
            {
                AllowMirrorDB = false,
                AllowDataStoreDB = true,
                IsVerboseOutput = false,
                IsCompactOutput = true,
                MaxResults = 100000,
                BoundingBox = new List<Common.LatLon> {
                    new Common.LatLon { Latitude= 51.45580747856863, Longitude = -0.27510331130017107 } ,
                    new Common.LatLon { Latitude = 51.53701250046424, Longitude =  0.009162470112983101 }
                }
            };

            var dbresults = api.GetPOIList(searchParams);
            Assert.True(dbresults.Count > 0);

            poi = dbresults.FirstOrDefault();
            Assert.NotNull(poi);

            // ensure all points are within the bounding box
            bbox = Core.Util.PolylineEncoder.ConvertPointsToBoundingBox(searchParams.BoundingBox);

            foreach (var p in cacheResults)
            {
                Assert.True(bbox.Intersects(new NetTopologySuite.Geometries.Point(p.AddressInfo.Longitude, p.AddressInfo.Latitude)));
            }


            Assert.True(cacheResults.Count == dbresults.Count, "Cached results and db results should be the same");
        }


        [Fact]
        public void ReturnPOIResultsAlongSimplePolyline()
        {
            List<Common.LatLon> polyline = new List<Common.LatLon>
            {
                new Common.LatLon{ Latitude= 51.507729, Longitude=-0.13457},
                new Common.LatLon{ Latitude= 51.507099, Longitude=-0.130117},
                new Common.LatLon{ Latitude= 51.5064319588449, Longitude=-0.123735666275},

            };
            var api = new OCM.API.Common.POIManager();

            // mongodb cache results
            var searchParams = new Common.APIRequestParams { AllowMirrorDB = true, IsVerboseOutput = false, Polyline = polyline, DistanceUnit = Common.Model.DistanceUnit.KM, Distance = 1 };
            var cacheResults = api.GetPOIList(searchParams);
            Assert.True(cacheResults.Count > 0);

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);

            Assert.True(AreResultsNearPolyline(polyline, searchParams, cacheResults), "One or more POIs in result set are not near the polyline");

            // database results
            searchParams = new Common.APIRequestParams { AllowMirrorDB = false, IsVerboseOutput = false, Polyline = polyline, DistanceUnit = Common.Model.DistanceUnit.KM, Distance = 1 };
            var dbresults = api.GetPOIList(searchParams);
            Assert.True(dbresults.Count > 0);

            poi = dbresults.FirstOrDefault();
            Assert.NotNull(poi);

            Assert.True(AreResultsNearPolyline(polyline, searchParams, dbresults), "One or more POIs in result set are not near the polyline");

            Assert.True(cacheResults.Count == dbresults.Count, "Cached results and db results should be the same");
        }


        [Fact]
        public void ReturnPOIResultsAlongEncodedPolyline()
        {
            string encodedPolyline = @"_riyGmj_zBwIxTcPg@md@qBgIdBePvLoUrQCIOCQ`@sGhDgJrCqJM{Dv@sRnJq_@nUgEvC_DnEoEjGwCbByF~@oFBE{Ft@uA|AClAdEx@|e@s@ti@kD`vBs@zn@^lNnHnj@hEl_@^jYaCt_@iD|P{Kt^kIjb@qC`[iJn_@cHfTqCbEyEjDuDfAwVfEqYpHoXlK_TvK}XhRkZdXii@vn@gXtYuf@xa@ic@hXwU`Lua@dOe_@hLio@~TsT`Kqe@jXu^tW{QpOca@p`@cc@zd@g~AfaByp@jk@sa@`Zcn@b_@_a@`Skj@dUwq@rUyn@zRu{Ate@ytAbc@gd@zNab@dRyd@jZek@z`@ydAjs@q[~Oia@vMck@xJuTtCed@lHoUdGw]hNcd@hXkYfVk\~\_`@na@g\r]{a@jc@qYrX_SfO{\xRae@hQmTdFk`@~Ekb@xAqfBdCu_A`CoUnCw^`Iic@vP_\hReVtQyb@l_@ycAd}@_e@fZ{oArg@aa@dOwoAzf@oj@fXcw@|c@}jAdu@gw@pg@w]xWqVxUm|@nfAsa@pc@ci@fa@in@z_@g^rUsKxIuWlWqOjRaLxOa`@|r@uPxb@c[pbAeUns@cOb`@q_@fu@cUn]eYl]mUdUoUfRmi@~_@ib@b[icA`~@qRtOmZjRce@xSwf@xLoa@dEq_@\wYoAkb@yFsS{D}[uGwf@}Gac@qAkQ^ac@fE}WtFq|@nWey@|Me_AlL}]hHqc@tNqe@xT{^rUmk@|f@mXxZ_\db@w{A~rB{w@v~@il@dl@{l@|g@shAtx@c`Alo@etBxuAsl@je@k_@|^_`@|c@oPtTuz@loAe`@xg@y[`_@}a@hb@yXtVm`@b[gk@t_@cp@v^_j@d^{[hWab@z^qZrUsb@tXyh@v[o`@fYu[fXos@vu@we@vm@_a@bk@uo@xcA{n@viA{Uve@eP`]ap@fzAul@nnAo\bn@yl@xbAmp@bdA{i@f~@m]|q@il@bwAmg@~tAk`@hcAcb@vz@_\bi@mVp\uJrLwb@nd@eb@n^ga@xZ{`Abt@ao@re@ocBzoAkx@fj@kl@lXyMrEox@xSwf@hNcPlLiKzMaO`\iQn`@uP~Zqf@py@u]fl@_M`IaItEqLbL}SpSiIzFqHz@sG~C}DxGsMtZiBrGbApAhJd_@fBpLbB|YlAhHvBzXKrWIx~@QxJw@`BsAe@DaMo@w@_Jb@uVtG}LpJiQdRsY~]u]l`@wBdC_Q~NeKrHwRtIeOvDyEhB`@~C~DcAxIuCpC@n@I`@{@";

            var polyline = OCM.Core.Util.PolylineEncoder.Decode(encodedPolyline).ToList();
            var api = new OCM.API.Common.POIManager();

            var searchParams = new Common.APIRequestParams { AllowMirrorDB = true, IsVerboseOutput = false, Polyline = polyline, DistanceUnit = Common.Model.DistanceUnit.KM, Distance = 5 };

            // POI cache db
            var cacheResults = api.GetPOIList(searchParams);

            Assert.True(cacheResults.Count > 0);

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);

            Assert.True(AreResultsNearPolyline(polyline, searchParams, cacheResults), "One or more POIs in result set are not near the polyline");

            // POI db results
            searchParams = new Common.APIRequestParams { AllowMirrorDB = false, IsVerboseOutput = false, Polyline = polyline, DistanceUnit = Common.Model.DistanceUnit.KM, Distance = 5 };
            var dbresults = api.GetPOIList(searchParams);

            Assert.True(dbresults.Count > 0);

            Assert.True(AreResultsNearPolyline(polyline, searchParams, dbresults), "One or more POIs in result set are not near the polyline");

            Assert.True(cacheResults.Count == dbresults.Count, "Cached results and db results should be the same");
        }

        private static bool AreResultsNearPolyline(List<Common.LatLon> polyline, Common.APIRequestParams searchParams, List<Common.Model.ChargePoint> results)
        {

            if (searchParams.Distance == null) throw new Exception("Distance filter not specified, cannot check if near points");

            bool areAllResultsNearLine = true;

            foreach (var r in results)
            {
                var isNear = false;
                // check is POI near at least one point on polyline
                foreach (var p in polyline)
                {
                    var dist = Common.GeoManager.CalcDistance((double)p.Latitude, (double)p.Longitude, r.AddressInfo.Latitude, r.AddressInfo.Longitude, searchParams.DistanceUnit);

                    if (dist <= searchParams.Distance)
                    {
                        isNear = true;
                        break;
                    }
                }

                if (!isNear)
                {

                    areAllResultsNearLine = false;
                }
            }

            return areAllResultsNearLine;
        }

    }
}