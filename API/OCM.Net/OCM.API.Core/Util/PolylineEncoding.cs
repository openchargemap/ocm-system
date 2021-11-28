using NetTopologySuite.Geometries;
using OCM.API.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCM.Core.Util
{
    /// <summary>
    /// See https://developers.google.com/maps/documentation/utilities/polylinealgorithm
    /// Forked from https://gist.github.com/shinyzhu/4617989
    /// </summary>
    public static class PolylineEncoder
    {
        /// <summary>
        /// Decode google style polyline coordinates.
        /// </summary>
        /// <param name="encodedPoints"></param>
        /// <returns></returns>
        public static IEnumerable<OCM.API.Common.LatLon> Decode(string encodedPoints)
        {
            if (string.IsNullOrEmpty(encodedPoints))
                throw new ArgumentNullException("encodedPoints");

            char[] polylineChars = encodedPoints.ToCharArray();
            int index = 0;

            int currentLat = 0;
            int currentLng = 0;
            int next5bits;
            int sum;
            int shifter;

            while (index < polylineChars.Length)
            {
                // calculate next latitude
                sum = 0;
                shifter = 0;
                do
                {
                    next5bits = (int)polylineChars[index++] - 63;
                    sum |= (next5bits & 31) << shifter;
                    shifter += 5;
                } while (next5bits >= 32 && index < polylineChars.Length);

                if (index >= polylineChars.Length)
                    break;

                currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                //calculate next longitude
                sum = 0;
                shifter = 0;
                do
                {
                    next5bits = (int)polylineChars[index++] - 63;
                    sum |= (next5bits & 31) << shifter;
                    shifter += 5;
                } while (next5bits >= 32 && index < polylineChars.Length);

                if (index >= polylineChars.Length && next5bits >= 32)
                    break;

                currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                yield return new OCM.API.Common.LatLon
                {
                    Latitude = Convert.ToDouble(currentLat) / 1E5,
                    Longitude = Convert.ToDouble(currentLng) / 1E5
                };
            }
        }

        /// <summary>
        /// Encode it
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static string Encode(IEnumerable<OCM.API.Common.LatLon> points)
        {
            var str = new StringBuilder();

            var encodeDiff = (Action<int>)(diff =>
            {
                int shifted = diff << 1;
                if (diff < 0)
                    shifted = ~shifted;

                int rem = shifted;

                while (rem >= 0x20)
                {
                    str.Append((char)((0x20 | (rem & 0x1f)) + 63));

                    rem >>= 5;
                }

                str.Append((char)(rem + 63));
            });

            int lastLat = 0;
            int lastLng = 0;

            foreach (var point in points)
            {
                int lat = (int)Math.Round((double)point.Latitude * 1E5);
                int lng = (int)Math.Round((double)point.Longitude * 1E5);

                encodeDiff(lat - lastLat);
                encodeDiff(lng - lastLng);

                lastLat = lat;
                lastLng = lng;
            }

            return str.ToString();
        }

        public static IEnumerable<OCM.API.Common.LatLon> SearchPolygonFromPolyLine(List<OCM.API.Common.LatLon> points, double distanceKM)
        {
            var searchPolygon = CreatePolygonFromPolyLine(points, distanceKM);

            //   if (!searchPolygon.Shell.IsCCW) searchPolygon = searchPolygon.Reverse();

            List<OCM.API.Common.LatLon> polyPoints = searchPolygon.Coordinates.Select(p => new LatLon { Latitude = p.Y, Longitude = p.X }).ToList();

            return polyPoints;
        }

        public static string SearchPolygonWKTFromPolyLine(List<OCM.API.Common.LatLon> points, double distanceKM)
        {
            var searchPolygon = CreatePolygonFromPolyLine(points, distanceKM);
            return searchPolygon.AsText();
        }

        /// <summary>
        /// Simplifies provided polyline and expands into polygon containing the search distance as a buffer
        /// </summary>
        /// <param name="points"></param>
        /// <param name="distanceKM"></param>
        /// <returns></returns>
        private static Polygon CreatePolygonFromPolyLine(List<OCM.API.Common.LatLon> points, double distanceKM)
        {
            var factory = GetGeometryFactoryEx();
           //helps make polygon shell counter clockwise
            LineString polyLine = factory.CreateLineString(points.Select(p => new Coordinate((double)p.Longitude, (double)p.Latitude)).ToArray());

            var searchPolygon = polyLine.Buffer(distanceKM / 2 / 100, points.Count) as Polygon;
            searchPolygon = NetTopologySuite.Simplify.TopologyPreservingSimplifier.Simplify(searchPolygon, 0.001) as Polygon;

            return searchPolygon;
        }

        private static GeometryFactoryEx GetGeometryFactoryEx()
        {
            var geo= new GeometryFactoryEx(new PrecisionModel(PrecisionModels.FloatingSingle), GeoManager.StandardSRID);
            geo.OrientationOfExteriorRing = LinearRingOrientation.CCW;
            return geo;
        }

        public static Geometry ConvertPointsToBoundingBox(List<LatLon> boundingBoxPoints)
        {
            var factory = GetGeometryFactoryEx(); //helps make polygon shell counter clockwise
            var polyLine = factory.CreateLineString(boundingBoxPoints.Select(p => new NetTopologySuite.Geometries.Coordinate((double)p.Longitude, (double)p.Latitude)).ToArray());
            return polyLine.Envelope;
        }
    }
}