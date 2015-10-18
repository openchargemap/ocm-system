using DotSpatial.Data;
using DotSpatial.Topology;
using System;
using System.Collections.Generic;
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
            //http://dotspatial.codeplex.com/wikipage?title=CycleThroughVerticesCS&referringTitle=Desktop_SampleCode

            //create feature set from points
            Feature f = new Feature();
            FeatureSet fs = new FeatureSet(f.FeatureType);

            Coordinate[] coord = new Coordinate[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                coord[i] = new Coordinate((double)points[i].Latitude, (double)points[i].Longitude);
            }
            LineString ls = new LineString(coord);
            f = new Feature(ls);
            fs.Features.Add(f);
            fs.Buffer(distanceKM, false); //TODO: approx km to lat/long coord value
            IFeatureSet iF = fs.Buffer(10, true);

            //export polygon points
            List<OCM.API.Common.LatLon> polyPoints = new List<OCM.API.Common.LatLon>();
            Extent extent = new Extent(-180, -90, 180, 90);
            foreach (ShapeRange shape in fs.ShapeIndices)
            {
                if (shape.Intersects(extent))
                    foreach (PartRange part in shape.Parts)
                    {
                        foreach (Vertex vertex in part)
                        {
                            if (vertex.X > 0 && vertex.Y > 0)
                            {
                                // prepare export of polygon points
                                Console.WriteLine(vertex.X);
                                polyPoints.Add(new OCM.API.Common.LatLon { Latitude = vertex.X, Longitude = vertex.Y });
                            }
                        }
                    }
            }

            return polyPoints;
        }
    }
}