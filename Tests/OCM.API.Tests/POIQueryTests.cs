using Microsoft.Extensions.Configuration;
using OCM.API.Common.Model;
using OCM.Core.Settings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace OCM.API.Tests
{
    /// <summary>
    /// Range of tests on filter parameters for POI retrieval
    /// </summary>
    public class POIQueryTests
    {
        private CoreSettings _settings = new CoreSettings();
        private bool _enableSQLSpatialTests = false;

        private IConfiguration GetConfiguration()
        {
            var configPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            return new ConfigurationBuilder()
                   .SetBasePath(configPath)
                   .AddJsonFile("appsettings.json", optional: true)
                   .AddUserSecrets("407b1e1a-5108-48b6-bb48-4990a0804269")
                   .Build();
        }
        public POIQueryTests()
        {
            var config = GetConfiguration();
            var settings = new Core.Settings.CoreSettings();
            config.GetSection("CoreSettings").Bind(settings);
            _settings = settings;

            var c = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            Core.Data.CacheManager.InitCaching(_settings);

            System.Diagnostics.Debug.WriteLine(Core.Data.CacheManager.RefreshCachedData(Core.Data.CacheUpdateStrategy.Modified).Result.NumDistinctPOIs);

        }
        [Fact]
        public void FilterOnChargePointID()
        {
            var api = new OCM.API.Common.POIManager();
            var results = api.GetPOIList(new Common.APIRequestParams { AllowMirrorDB = true, AllowDataStoreDB = false, ChargePointIDs = new int[] { 23190 } });
            Assert.True(results.Count() == 1, "Should return a single result");

            var poi = results.FirstOrDefault();
            Assert.NotNull(poi);

            Assert.True(poi.ID == 23190, "ID should match");
            Assert.True(poi.AddressInfo.CountryID == 112, "Country should be Italy"); // Italy

            results = api.GetPOIList(new Common.APIRequestParams { AllowMirrorDB = false, AllowDataStoreDB = true, ChargePointIDs = new int[] { 23190 } });
            Assert.True(results.Count() == 1, "Should return a single result");

            poi = results.FirstOrDefault();
            Assert.NotNull(poi);

            Assert.True(poi.ID == 23190, "ID should match");
            Assert.True(poi.AddressInfo.CountryID == 112, "Country should be Italy"); // Italy
        }

        [Fact]
        public void ReturnPOIResults()
        {
            var api = new OCM.API.Common.POIManager();
            var results = api.GetPOIList(new Common.APIRequestParams { AllowMirrorDB = true, AllowDataStoreDB = false, IsVerboseOutput = false, MaxResults = 10 }); ;
            Assert.True(results.Count() > 0);
            Assert.True(results.Count() == 10);

            var poi = results.FirstOrDefault();
            Assert.NotNull(poi);

            var dbResults = api.GetPOIList(new Common.APIRequestParams { AllowMirrorDB = false, AllowDataStoreDB = true, IsVerboseOutput = false, MaxResults = 10 }); ;
            Assert.True(dbResults.Count() > 0);
            Assert.True(dbResults.Count() == 10);

            poi = dbResults.FirstOrDefault();
            Assert.NotNull(poi);
        }

        [Fact]
        public void ReturnPOIResultFilterByPowerKW()
        {

            var minKw = 7;
            var maxKw = 49;

            var api = new OCM.API.Common.POIManager();
            var results = api.GetPOIList(new Common.APIRequestParams
            {
                AllowMirrorDB = true,
                AllowDataStoreDB = false,
                IsVerboseOutput = false,
                MaxResults = 1000,
                MaxPowerKW = maxKw,
                MinPowerKW = minKw
            });

            Assert.True(results.Count() > 0);
            Assert.True(results.Count() == 1000);

            var poi = results.FirstOrDefault();
            Assert.NotNull(poi);

            var allResultsHaveGreaterThanMinimum = results.All(r => r.Connections.Any(co => co.PowerKW >= minKw));
            Assert.True(allResultsHaveGreaterThanMinimum);

            var allResultsHaveLessThanMaximum = results.All(r => r.Connections.Any(co => co.PowerKW <= maxKw));
            Assert.True(allResultsHaveLessThanMaximum);

            //

            var dbResults = api.GetPOIList(new Common.APIRequestParams
            {
                AllowMirrorDB = false,
                AllowDataStoreDB = true,
                IsVerboseOutput = false,
                MaxResults = 1000,
                MaxPowerKW = maxKw,
                MinPowerKW = minKw
            });

            Assert.True(dbResults.Count() > 0);
            Assert.True(dbResults.Count() == 1000);


            poi = dbResults.FirstOrDefault();
            Assert.NotNull(poi);

            allResultsHaveGreaterThanMinimum = dbResults.All(r => r.Connections.Any(co => co.PowerKW >= minKw));
            Assert.True(allResultsHaveGreaterThanMinimum);

            allResultsHaveLessThanMaximum = dbResults.All(r => r.Connections.Any(co => co.PowerKW <= maxKw));
            Assert.True(allResultsHaveLessThanMaximum);

        }

        [Fact]
        public void ReturnPOIResultFilteredByPostcode()
        {

            var postcodes = new string[] { "29306", "29379" };


            var api = new OCM.API.Common.POIManager();
            var results = api.GetPOIList(new Common.APIRequestParams
            {
                AllowMirrorDB = true,
                AllowDataStoreDB = false,
                IsVerboseOutput = false,
                MaxResults = 1000,
                Postcodes = postcodes
            });

            Assert.True(results.Count() > 0);
            Assert.True(results.Count() < 100);

            var poi = results.FirstOrDefault();
            Assert.NotNull(poi);

            var allResultsArePostcodesInSet = results.All(r => postcodes.Contains(r.AddressInfo.Postcode));
            Assert.True(allResultsArePostcodesInSet);


            // db results

            var dbResults = api.GetPOIList(new Common.APIRequestParams
            {
                AllowMirrorDB = false,
                AllowDataStoreDB = true,
                IsVerboseOutput = false,
                MaxResults = 1000,
                Postcodes = postcodes
            });

            Assert.True(dbResults.Count() > 0);
            Assert.True(dbResults.Count() < 100);

            poi = results.FirstOrDefault();
            Assert.NotNull(poi);

            allResultsArePostcodesInSet = dbResults.All(r => postcodes.Contains(r.AddressInfo.Postcode));
            Assert.True(allResultsArePostcodesInSet);

            Assert.True(results.Count() == dbResults.Count());

        }
        [Fact]
        public void ReturnCompactPOIResults()
        {
            var api = new OCM.API.Common.POIManager();
            var results = api.GetPOIList(new Common.APIRequestParams { AllowMirrorDB = true, AllowDataStoreDB = false, IsCompactOutput = true, IsVerboseOutput = false, MaxResults = 10 }); ;
            Assert.True(results.Count() > 0);
            Assert.True(results.Count() == 10);

            var poi = results.FirstOrDefault();
            Assert.NotNull(poi);
            Assert.Null(poi.DataProvider);
            Assert.Null(poi.SubmissionStatus);

            var dbResults = api.GetPOIList(new Common.APIRequestParams { AllowMirrorDB = false, AllowDataStoreDB = true, IsCompactOutput = true, IsVerboseOutput = false, MaxResults = 10 }); ;
            Assert.True(dbResults.Count() > 0);
            Assert.True(results.Count() == 10);

            poi = dbResults.FirstOrDefault();
            Assert.NotNull(poi);
            Assert.Null(poi.DataProvider);
            Assert.Null(poi.SubmissionStatus);
        }

        [Fact]
        public void ReturnPOIResultsInAvgTime()
        {
            var api = new OCM.API.Common.POIManager();

            var stopwatch = Stopwatch.StartNew();
            var totalRuns = 10;
            var maxResults = 1;
            for (var i = 0; i < totalRuns; i++)
            {
                var results = api.GetPOIList(new Common.APIRequestParams { AllowMirrorDB = true, AllowDataStoreDB = false, IsVerboseOutput = false, MaxResults = maxResults }); ;
                Assert.True(results.Count() > 0);
                Assert.True(results.Count() == maxResults);
            }

            stopwatch.Stop();

            var requiredAvgMS = 100;
            Assert.True(stopwatch.ElapsedMilliseconds / totalRuns < requiredAvgMS, $"Avg query should take less than {requiredAvgMS}ms Actual:{stopwatch.ElapsedMilliseconds / totalRuns}ms");
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
            Assert.True(cacheResults.Count() > 0);

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);
            Assert.True(poi.AddressInfo.Distance > 0, "Distance should be more than zero");

            Assert.True(AreResultsNearPolyline(
                new List<Common.LatLon> { new Common.LatLon { Latitude = 51.507729, Longitude = -0.13457 } },
                searchParams,
                cacheResults),
                "One or more POIs in result set are not near the polyline");

            if (_enableSQLSpatialTests)
            {
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
                Assert.True(dbresults.Count() > 0);

                poi = dbresults.FirstOrDefault();
                Assert.NotNull(poi);
                Assert.True(poi.AddressInfo.Distance > 0, "Distance should be more than zero");

                Assert.True(AreResultsNearPolyline(
                    new List<Common.LatLon> { new Common.LatLon { Latitude = 51.507729, Longitude = -0.13457 } },
                    searchParams,
                    cacheResults),
                    "One or more POIs in result set are not near the polyline");

                Assert.True(cacheResults.Count() == dbresults.Count(), "Cached results and db results should be the same");
            }
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
            Assert.True(cacheResults.Count() > 0);
            Assert.True(cacheResults.Count() <= searchParams.MaxResults, $"Result count must be less than {searchParams.MaxResults})");

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);
            Assert.True(poi.AddressInfo.Distance > 0, "Distance should be more than zero");

            if (_enableSQLSpatialTests)
            {
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
                Assert.True(dbresults.Count() > 0);

                Assert.True(dbresults.Count() <= searchParams.MaxResults, $"Result count must be less than {searchParams.MaxResults})");

                poi = dbresults.FirstOrDefault();
                Assert.NotNull(poi);

                Assert.True(poi.AddressInfo.Distance > 0, "Distance should be more than zero");

                Assert.True(cacheResults.Count() == dbresults.Count(), "Cached results and db results should be the same");
            }
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

            var stopwatch = Stopwatch.StartNew();
            var cacheResults = api.GetPOIList(searchParams);

            stopwatch.Stop();

            var msAllowance = 2;
            Assert.True(stopwatch.ElapsedMilliseconds < cacheResults.Count() * msAllowance, $"Results must return in less than {cacheResults.Count() * msAllowance} ms (numResults * {msAllowance}ms) actual: {stopwatch.ElapsedMilliseconds}");
            Assert.True(cacheResults.Count() > 0);
            Assert.True(cacheResults.Count() <= searchParams.MaxResults, $"Result count must be less than {searchParams.MaxResults})");

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);

            // ensure all points are within the bounding box
            var bbox = Core.Util.PolylineEncoder.ConvertPointsToBoundingBox(searchParams.BoundingBox);


            foreach (var p in cacheResults)
            {
                var testPoint = new NetTopologySuite.Geometries.Point(p.AddressInfo.Longitude, p.AddressInfo.Latitude);
                testPoint.SRID = 4326;
                if (!bbox.Intersects(testPoint))
                {
                    if (bbox.IsWithinDistance(testPoint, 100))
                    {
                        // point is outside box but not far away
                    }
                    else
                    {
                        Assert.True(bbox.Intersects(testPoint), "Location must intersect the given bounding box.");
                    }
                }
            }

            if (_enableSQLSpatialTests)
            {

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
                Assert.True(dbresults.Count() > 0);
                Assert.True(dbresults.Count() <= searchParams.MaxResults, $"Result count must be less than {searchParams.MaxResults})");

                poi = dbresults.FirstOrDefault();
                Assert.NotNull(poi);

                // ensure all points are within the bounding box
                bbox = Core.Util.PolylineEncoder.ConvertPointsToBoundingBox(searchParams.BoundingBox);

                foreach (var p in cacheResults)
                {
                    Assert.True(bbox.Intersects(new NetTopologySuite.Geometries.Point(p.AddressInfo.Longitude, p.AddressInfo.Latitude)));
                }


                Assert.True(cacheResults.Count() == dbresults.Count(), "Cached results and db results should be the same");
            }
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
            Assert.True(cacheResults.Count() > 0);
            Assert.True(cacheResults.Count() <= searchParams.MaxResults, $"Result count must be less than {searchParams.MaxResults})");

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);

            Assert.True(AreResultsNearPolyline(polyline, searchParams, cacheResults), "One or more POIs in result set are not near the polyline");

            if (_enableSQLSpatialTests)
            {

                // database results
                searchParams = new Common.APIRequestParams { AllowMirrorDB = false, IsVerboseOutput = false, Polyline = polyline, DistanceUnit = Common.Model.DistanceUnit.KM, Distance = 1 };
                var dbresults = api.GetPOIList(searchParams);
                Assert.True(dbresults.Count() > 0);
                Assert.True(dbresults.Count() <= searchParams.MaxResults, $"Result count must be less than {searchParams.MaxResults})");

                poi = dbresults.FirstOrDefault();
                Assert.NotNull(poi);

                Assert.True(AreResultsNearPolyline(polyline, searchParams, dbresults), "One or more POIs in result set are not near the polyline");

                Assert.True(cacheResults.Count() == dbresults.Count(), "Cached results and db results should be the same");
            }
        }


        [Fact]
        public void ReturnPOIResultsAlongEncodedPolyline()
        {
            string encodedPolyline = @"_riyGmj_zBwIxTcPg@md@qBgIdBePvLoUrQCIOCQ`@sGhDgJrCqJM{Dv@sRnJq_@nUgEvC_DnEoEjGwCbByF~@oFBE{Ft@uA|AClAdEx@|e@s@ti@kD`vBs@zn@^lNnHnj@hEl_@^jYaCt_@iD|P{Kt^kIjb@qC`[iJn_@cHfTqCbEyEjDuDfAwVfEqYpHoXlK_TvK}XhRkZdXii@vn@gXtYuf@xa@ic@hXwU`Lua@dOe_@hLio@~TsT`Kqe@jXu^tW{QpOca@p`@cc@zd@g~AfaByp@jk@sa@`Zcn@b_@_a@`Skj@dUwq@rUyn@zRu{Ate@ytAbc@gd@zNab@dRyd@jZek@z`@ydAjs@q[~Oia@vMck@xJuTtCed@lHoUdGw]hNcd@hXkYfVk\~\_`@na@g\r]{a@jc@qYrX_SfO{\xRae@hQmTdFk`@~Ekb@xAqfBdCu_A`CoUnCw^`Iic@vP_\hReVtQyb@l_@ycAd}@_e@fZ{oArg@aa@dOwoAzf@oj@fXcw@|c@}jAdu@gw@pg@w]xWqVxUm|@nfAsa@pc@ci@fa@in@z_@g^rUsKxIuWlWqOjRaLxOa`@|r@uPxb@c[pbAeUns@cOb`@q_@fu@cUn]eYl]mUdUoUfRmi@~_@ib@b[icA`~@qRtOmZjRce@xSwf@xLoa@dEq_@\wYoAkb@yFsS{D}[uGwf@}Gac@qAkQ^ac@fE}WtFq|@nWey@|Me_AlL}]hHqc@tNqe@xT{^rUmk@|f@mXxZ_\db@w{A~rB{w@v~@il@dl@{l@|g@shAtx@c`Alo@etBxuAsl@je@k_@|^_`@|c@oPtTuz@loAe`@xg@y[`_@}a@hb@yXtVm`@b[gk@t_@cp@v^_j@d^{[hWab@z^qZrUsb@tXyh@v[o`@fYu[fXos@vu@we@vm@_a@bk@uo@xcA{n@viA{Uve@eP`]ap@fzAul@nnAo\bn@yl@xbAmp@bdA{i@f~@m]|q@il@bwAmg@~tAk`@hcAcb@vz@_\bi@mVp\uJrLwb@nd@eb@n^ga@xZ{`Abt@ao@re@ocBzoAkx@fj@kl@lXyMrEox@xSwf@hNcPlLiKzMaO`\iQn`@uP~Zqf@py@u]fl@_M`IaItEqLbL}SpSiIzFqHz@sG~C}DxGsMtZiBrGbApAhJd_@fBpLbB|YlAhHvBzXKrWIx~@QxJw@`BsAe@DaMo@w@_Jb@uVtG}LpJiQdRsY~]u]l`@wBdC_Q~NeKrHwRtIeOvDyEhB`@~C~DcAxIuCpC@n@I`@{@";

            var polyline = OCM.Core.Util.PolylineEncoder.Decode(encodedPolyline).ToList();
            var api = new OCM.API.Common.POIManager();

            var searchParams = new Common.APIRequestParams { AllowMirrorDB = true, AllowDataStoreDB = false, IsVerboseOutput = false, Polyline = polyline, DistanceUnit = Common.Model.DistanceUnit.KM, Distance = 5 };

            // POI cache db
            var cacheResults = api.GetPOIList(searchParams);

            Assert.True(cacheResults.Count() > 0);
            Assert.True(cacheResults.Count() <= searchParams.MaxResults, $"Result count must be less than {searchParams.MaxResults})");

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);

            Assert.True(AreResultsNearPolyline(polyline, searchParams, cacheResults), "One or more POIs in result set are not near the polyline");

            if (_enableSQLSpatialTests)
            {

                // POI db results
                searchParams = new Common.APIRequestParams { AllowMirrorDB = false, AllowDataStoreDB = true, IsVerboseOutput = false, Polyline = polyline, DistanceUnit = Common.Model.DistanceUnit.KM, Distance = 5 };
                var dbresults = api.GetPOIList(searchParams);

                Assert.True(dbresults.Count() > 0);
                Assert.True(dbresults.Count() <= searchParams.MaxResults, $"Result count must be less than {searchParams.MaxResults})");

                Assert.True(AreResultsNearPolyline(polyline, searchParams, dbresults), "One or more POIs in result set are not near the polyline");

                Assert.True(cacheResults.Count() == dbresults.Count(), $"Cached results {cacheResults.Count()} and db results {dbresults.Count()} should be the same");
            }
        }

        private static bool AreResultsNearPolyline(List<Common.LatLon> polyline, Common.APIRequestParams searchParams, IEnumerable<Common.Model.ChargePoint> results)
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

        [Fact]
        public void ReturnPOIResultsForSpecificOperatorID()
        {
            var api = new OCM.API.Common.POIManager();

            var searchParams = new Common.APIRequestParams
            {
                AllowMirrorDB = true,
                AllowDataStoreDB = false,
                IsVerboseOutput = false,
                DistanceUnit = Common.Model.DistanceUnit.Miles,
                Distance = null,
                MaxResults = 20,
                Latitude = 51.5077,
                Longitude = -0.134,
                OperatorIDs = new int[] { 19, 3 }
            };

            var cacheResults = api.GetPOIList(searchParams);
            Assert.True(cacheResults.Count() > 0);
            Assert.True(cacheResults.Count() <= searchParams.MaxResults, $"Result count must be less than {searchParams.MaxResults})");

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);
            Assert.True(poi.AddressInfo.Distance > 0, "Distance should be more than zero");

            // ensure only results from given operators
            Assert.All<Common.Model.ChargePoint>(cacheResults, a => searchParams.OperatorIDs.Contains((int)a.OperatorID));
            foreach (var p in cacheResults)
            {
                Assert.Contains((int)p.OperatorID, searchParams.OperatorIDs);
            }
        }

        [Fact]
        public void ReturnPOIResultsForSpecificCountryIDs()
        {
            var api = new OCM.API.Common.POIManager();

            var searchParams = new Common.APIRequestParams
            {
                AllowMirrorDB = true,
                AllowDataStoreDB = false,
                IsVerboseOutput = false,
                DistanceUnit = Common.Model.DistanceUnit.Miles,
                Distance = null,
                MaxResults = 20,
                Latitude = 51.5077,
                Longitude = -0.134,
                CountryIDs = new int[] { 3, 19 }
            };

            var cacheResults = api.GetPOIList(searchParams);
            Assert.True(cacheResults.Count() > 0);
            Assert.True(cacheResults.Count() <= searchParams.MaxResults, $"Result count must be less than {searchParams.MaxResults})");

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);
            Assert.True(poi.AddressInfo.Distance > 0, "Distance should be more than zero");

            // ensure only results from given countries

            foreach (var p in cacheResults)
            {
                Assert.Contains((int)p.AddressInfo.CountryID, searchParams.CountryIDs);
            }
        }

        [Fact]
        public void ReturnPOIResultsForSpecificSubmissionStatus()
        {
            var api = new OCM.API.Common.POIManager();

            var searchParams = new Common.APIRequestParams
            {
                AllowMirrorDB = true,
                AllowDataStoreDB = false,
                IsVerboseOutput = false,
                DistanceUnit = Common.Model.DistanceUnit.Miles,
                Distance = null,
                MaxResults = 20,
                Latitude = 51.5077,
                Longitude = -0.134,
                SubmissionStatusTypeID = 250
            };

            var cacheResults = api.GetPOIList(searchParams);
            Assert.True(cacheResults.Count() > 0);
            Assert.True(cacheResults.Count() <= searchParams.MaxResults, $"Result count must be less than {searchParams.MaxResults})");

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);
            Assert.True(poi.AddressInfo.Distance > 0, "Distance should be more than zero");

            // ensure only results from given countries
            foreach (var p in cacheResults)
            {
                Assert.True(p.SubmissionStatusTypeID == searchParams.SubmissionStatusTypeID);
            }

        }

        [Fact]
        public void ReturnPOIResultsForGreaterThanId()
        {
            var api = new OCM.API.Common.POIManager();

            var searchParams = new Common.APIRequestParams
            {
                AllowMirrorDB = true,
                AllowDataStoreDB = false,
                IsVerboseOutput = false,
                DistanceUnit = Common.Model.DistanceUnit.Miles,
                Distance = null,
                MaxResults = 20,
                Latitude = 51.5077,
                Longitude = -0.134,
                GreaterThanId = 25000
            };

            var cacheResults = api.GetPOIList(searchParams);
            Assert.True(cacheResults.Count() > 0);
            Assert.True(cacheResults.Count() <= searchParams.MaxResults, $"Result count must be less than {searchParams.MaxResults})");

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);
            Assert.True(poi.AddressInfo.Distance > 0, "Distance should be more than zero");

            // ensure only expected results
            Assert.True(cacheResults.All(a => a.ID > searchParams.GreaterThanId));

        }

        [Fact]
        public void ReturnPOIResultsForModifiedGreaterThanDate()
        {
            var api = new OCM.API.Common.POIManager();

            var searchParams = new Common.APIRequestParams
            {
                AllowMirrorDB = true,
                AllowDataStoreDB = false,
                IsVerboseOutput = false,
                DistanceUnit = Common.Model.DistanceUnit.Miles,
                Distance = null,
                MaxResults = 20,
                Latitude = 51.5077,
                Longitude = -0.134,
                ChangesFromDate = new DateTime(2021, 1, 1)
            };

            var cacheResults = api.GetPOIList(searchParams);
            Assert.True(cacheResults.Count() > 0);
            Assert.True(cacheResults.Count() <= searchParams.MaxResults, $"Result count must be less than {searchParams.MaxResults})");

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);
            Assert.True(poi.AddressInfo.Distance > 0, "Distance should be more than zero");

            // ensure only expected results
            Assert.True(cacheResults.All(a => a.DateLastStatusUpdate >= searchParams.ChangesFromDate));

        }


        [Fact]
        public void ReturnPOIResultsWithStatus()
        {
            var api = new OCM.API.Common.POIManager();

            var searchParams = new Common.APIRequestParams
            {
                AllowMirrorDB = true,
                AllowDataStoreDB = false,
                IsVerboseOutput = false,
                DistanceUnit = Common.Model.DistanceUnit.Miles,
                Distance = null,
                MaxResults = 20,
                Latitude = 51.5077,
                Longitude = -0.134,
                StatusTypeIDs = new int[] { (int)StandardStatusTypes.NotOperational }
            };

            var cacheResults = api.GetPOIList(searchParams);
            Assert.True(cacheResults.Count() > 0);
            Assert.True(cacheResults.Count() <= searchParams.MaxResults, $"Result count must be less than {searchParams.MaxResults})");

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);
            Assert.True(poi.AddressInfo.Distance > 0, "Distance should be more than zero");

            // ensure only expected results
            foreach (var p in cacheResults)
            {
                Assert.Contains((int)p.StatusTypeID, searchParams.StatusTypeIDs);
            }

        }

        [Fact]
        public void ReturnPOIResultAreOpenData()
        {
            var api = new OCM.API.Common.POIManager();

            var searchParams = new Common.APIRequestParams
            {
                AllowMirrorDB = true,
                AllowDataStoreDB = false,
                IsVerboseOutput = false,
                DistanceUnit = Common.Model.DistanceUnit.Miles,
                Distance = null,
                MaxResults = 20,
                Latitude = 51.5077,
                Longitude = -0.134,
                IsOpenData = true
            };

            var cacheResults = api.GetPOIList(searchParams);
            Assert.True(cacheResults.Count() > 0);
            Assert.True(cacheResults.Count() <= searchParams.MaxResults, $"Result count must be less than {searchParams.MaxResults})");

            var poi = cacheResults.FirstOrDefault();
            Assert.NotNull(poi);
            Assert.True(poi.AddressInfo.Distance > 0, "Distance should be more than zero");

            // ensure only expected results
            Assert.All<Common.Model.ChargePoint>(cacheResults, a => Assert.True(a.DataProvider?.IsOpenDataLicensed == true));
            foreach (var p in cacheResults)
            {
                Assert.True(p.DataProvider?.IsOpenDataLicensed == true);
            }
        }

    }
}