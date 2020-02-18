using OCM.API.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCM.API.Common
{
    public class ServiceParameterParser
    {
        protected double? ParseDouble(string val)
        {
            double result = 0;

            if (val != "NaN" && double.TryParse(val, out result))
                return result;
            else
                return null;
        }

        protected int? ParseInt(string val)
        {
            int result = 0;

            if (val != "NaN" && int.TryParse(val, out result))
                return result;
            else
                return null;
        }

        protected DateTime? ParseDate(string val)
        {
            DateTime result = DateTime.Now;

            if (val != "NaN" && DateTime.TryParse(val, out result))
                return result;
            else
                return null;
        }

        /// <summary>
        /// Parse a comman seperated list of int values
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        protected int[] ParseIntList(string val)
        {
            if (val == null) return null;
            string[] valStrings = val.Split(',');
            List<int> valList = new List<int>();

            foreach (string value in valStrings)
            {
                int parsedInt = 0;
                if (int.TryParse(value, out parsedInt))
                {
                    valList.Add(parsedInt);
                }
            }
            return valList.ToArray();
        }

        protected DistanceUnit ParseDistanceUnit(string val)
        {
            if (val == null) return DistanceUnit.Miles;
            val = val.ToLower();

            if (val == "km" || val == "kilometers") return DistanceUnit.KM;
            if (val == "miles") return DistanceUnit.Miles;

            //default to miles
            return DistanceUnit.Miles;
        }

        protected string ParseString(string val)
        {
            if (val != null)
            {
                //limit string parameters to 100 characters
                if (val.Length > 100) val = val.Substring(0, 100);
            }
            return val;
        }

        protected string[] ParseStringList(string val)
        {
            if (val != null)
            {
                var strings = val.Split(",")
                            .Select(s => s.Trim().ToLower()).Where(s => !string.IsNullOrEmpty(s))
                            .ToArray();

                if (!strings.Any())
                {
                    return null;
                }
                else
                {
                    return strings;
                }
            }
            
            return null;
        }

        protected bool ParseBool(string val, bool defaultVal)
        {
            if (val == null) return defaultVal;
            val = val.Trim().ToLower();

            if (val == "true") return true;
            if (val == "false") return false;

            //none, return default
            return defaultVal;
        }

        protected bool? ParseBoolNullable(string val)
        {
            if (String.IsNullOrEmpty(val)) return null;

            val = val.Trim().ToLower();

            if (val == "true") return true;
            if (val == "false") return false;

            //value is neither empty, true or false, return null
            return null;
        }

        /// <summary>
        /// Parse bounding box in format (top left lat,top left lon), (bottom right lat, bottom right lon)
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        protected List<LatLon> ParseBoundingBox(string val)
        {
            var pointsList = ParsePointsList(val);// NorthEast and SouthWest lat/Lon pairs
            List<LatLon> rect = new List<LatLon>();
            rect.Add(pointsList[0]); //top left
            rect.Add(new LatLon { Latitude = pointsList[0].Latitude, Longitude = pointsList[1].Longitude }); //top right
            rect.Add(pointsList[1]); //bottom right
            rect.Add(new LatLon { Latitude = pointsList[1].Latitude, Longitude = pointsList[0].Longitude }); //bottom left

            return rect;
        }

        protected List<LatLon> ParsePolyline(string val)
        {
            return ParsePointsList(val);
        }

        protected List<LatLon> ParsePolygon(string val, bool closePolygon)
        {
            var pointsList = ParsePointsList(val);
            if (closePolygon)
            {
                //close polygon by ending on the starting point
                if (pointsList != null && pointsList.Any())
                {
                    var firstPoint = pointsList[0];
                    var lastPoint = pointsList.Last();
                    if (firstPoint.Latitude != lastPoint.Latitude || firstPoint.Longitude != lastPoint.Longitude)
                    {
                        pointsList.Add(firstPoint);
                    }
                }
            }
            return pointsList;
        }

        protected List<LatLon> ParsePointsList(string val)
        {
            List<LatLon> points = null;
            try
            {
                if (!String.IsNullOrEmpty(val))
                {
                    if (val.StartsWith("(") && val.EndsWith(")"))
                    {
                        //polyline is comma separated list of points

                        var pointStringList = val.Split(')');
                        points = new List<LatLon>();
                        foreach (var p in pointStringList)
                        {
                            if (!String.IsNullOrEmpty(p))
                            {
                                points.Add(LatLon.Parse(p));
                            }
                        }
                    }
                    else
                    {
                        //attempt polyline decoding
                        points = OCM.Core.Util.PolylineEncoder.Decode(val).ToList();
                    }
                }
            }
            catch (Exception)
            {
                ; ;//failed to parse supplied polyline
            }

            return points;
        }
    }
}