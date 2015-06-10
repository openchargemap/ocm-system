using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OCM.API.Common.Model;

namespace OCM.API.Common
{
    public class GeoManager
    {
        const Double kEarthRadiusMiles = 3959.0;
        const Double kEarthRadiusKms = 6371;
        const Double KMToMilesConversion = 0.621371192;

        // code based on http://www.codeproject.com/KB/cs/distancebetweenlocations.aspx

        /// <summary>
        /// very approximate distance check for data filtering, with 2 decimal places being approx 1km 
        /// </summary>
        /// <param name="lat1"></param>
        /// <param name="long1"></param>
        /// <param name="lat2"></param>
        /// <param name="long2"></param>
        /// <param name="decimals"></param>
        public static bool IsClose(double lat1,
                  double long1, double lat2, double long2, int decimals = 2)
        {

            if (lat1 == lat2 || long1 == long2) return true;

            //http://gis.stackexchange.com/questions/8650/how-to-measure-the-accuracy-of-latitude-and-longitude
            
            double latDiff = Math.Round(Math.Abs(lat1-lat2),decimals);
            double lngDiff = Math.Round(Math.Abs(long1-long2),decimals);

            if (latDiff==0 && lngDiff== 0) {
                return true;
            }

            return false;
        }
        public static double CalcDistance(double Lat1,
                  double Long1, double Lat2, double Long2, DistanceUnit Unit)
        {
            /*
                The Haversine formula according to Dr. Math.
                http://mathforum.org/library/drmath/view/51879.html
                
                dlon = lon2 - lon1
                dlat = lat2 - lat1
                a = (sin(dlat/2))^2 + cos(lat1) * cos(lat2) * (sin(dlon/2))^2
                c = 2 * atan2(sqrt(a), sqrt(1-a)) 
                d = R * c
                
                Where
                    * dlon is the change in longitude
                    * dlat is the change in latitude
                    * c is the great circle distance in Radians.
                    * R is the radius of a spherical Earth.
                    * The locations of the two points in 
                        spherical coordinates (longitude and 
                        latitude) are lon1,lat1 and lon2, lat2.
            */
            double dDistance = Double.MinValue;
            double dLat1InRad = Lat1 * (Math.PI / 180.0);
            double dLong1InRad = Long1 * (Math.PI / 180.0);
            double dLat2InRad = Lat2 * (Math.PI / 180.0);
            double dLong2InRad = Long2 * (Math.PI / 180.0);

            double dLongitude = dLong2InRad - dLong1InRad;
            double dLatitude = dLat2InRad - dLat1InRad;

            // Intermediate result a.

            double a = Math.Pow(Math.Sin(dLatitude / 2.0), 2.0) +
                       Math.Cos(dLat1InRad) * Math.Cos(dLat2InRad) *
                       Math.Pow(Math.Sin(dLongitude / 2.0), 2.0);

            // Intermediate result c (great circle distance in Radians).

            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

            // Distance.
            if (Unit == DistanceUnit.Miles)
                dDistance = kEarthRadiusMiles * c;

            if (Unit == DistanceUnit.KM)
                dDistance = kEarthRadiusKms * c;

            return dDistance;
        }

        public static double CalcDistance(string NS1, double Lat1, double Lat1Min,
               string EW1, double Long1, double Long1Min, string NS2,
               double Lat2, double Lat2Min, string EW2,
               double Long2, double Long2Min, DistanceUnit Unit)
        {
            double NS1Sign = NS1.ToUpper() == "N" ? 1.0 : -1.0;
            double EW1Sign = EW1.ToUpper() == "E" ? 1.0 : -1.0;
            double NS2Sign = NS2.ToUpper() == "N" ? 1.0 : -1.0;
            double EW2Sign = EW2.ToUpper() == "E" ? 1.0 : -1.0;
            return (CalcDistance(
                (Lat1 + (Lat1Min / 60)) * NS1Sign,
                (Long1 + (Long1Min / 60)) * EW1Sign,
                (Lat2 + (Lat2Min / 60)) * NS2Sign,
                (Long2 + (Long2Min / 60)) * EW2Sign
                , Unit));
        }

        public static double? ConvertKMToMiles(double? km)
        {
            if (km == null) return null;

            return km * KMToMilesConversion;
        }

        public static double? ConvertMilesToKM(double? miles)
        {
            if (miles == null) return null;

            return miles / KMToMilesConversion;
        }
    }
}