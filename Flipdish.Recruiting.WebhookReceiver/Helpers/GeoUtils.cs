using System;
using Flipdish.Recruiting.WebhookReceiver.Models;

namespace Flipdish.Recruiting.WebhookReceiver.Helpers
{
    public static class GeoUtils
    {
        public static double GetAirDistance(Coordinates aCoords, Coordinates bCoords)
        {
            var lat1 = aCoords.Latitude.Value;
            var lat2 = bCoords.Latitude.Value;
            var lon1 = aCoords.Longitude.Value;
            var lon2 = bCoords.Longitude.Value;
            if ((lat1 == lat2) && (lon1 == lon2))
            {
                return 0;
            }
            else
            {
                double theta = lon1 - lon2;
                double dist = (Math.Sin(Deg2rad(lat1)) * Math.Sin(Deg2rad(lat2))) + (Math.Cos(Deg2rad(lat1)) * Math.Cos(Deg2rad(lat2)) * Math.Cos(Deg2rad(theta)));

                return Rad2deg(Math.Acos(dist)) * 60 * 1.1515 * 1.609344;
            }
        }

        private static double Deg2rad(double deg)
        {
            return deg * Math.PI / 180.0;
        }

        private static double Rad2deg(double rad)
        {
            return rad / Math.PI * 180.0;
        }
    }
}