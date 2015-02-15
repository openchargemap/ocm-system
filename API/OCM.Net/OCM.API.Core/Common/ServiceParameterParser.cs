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

        protected List<LatLon> ParsePolyline(string val)
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
                                var temp = p.ToString().Trim();
                                if (temp.StartsWith(",")) temp = temp.Substring(1, temp.Length - 1).Trim();
                                var fragments = temp.Split(',');
                                var ll = new LatLon
                                {
                                    Latitude = double.Parse(fragments[0].Substring(1, fragments[0].Length - 1)),
                                    Longitude = double.Parse(fragments[1])
                                };
                                points.Add(ll);
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