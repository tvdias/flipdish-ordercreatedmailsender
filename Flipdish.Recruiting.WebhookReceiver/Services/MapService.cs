using System;
using System.Globalization;
using Flipdish.Recruiting.WebhookReceiver.Config;
using Microsoft.Extensions.Options;

namespace Flipdish.Recruiting.WebhookReceiver.Services
{
    public class MapService
    {
        private readonly AppSettings _appSettings;

        public MapService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public string GetDynamicMapUrl(double centerLatitude, double centerLongitude, int zoom)
        {
            // latitude
            string direction;
            double absoluteValue;
            if (centerLatitude < 0)
            {
                direction = "S";
                absoluteValue = -centerLatitude;
            }
            else
            {
                direction = "N";
                absoluteValue = centerLatitude;
            }

            string dmsLatitude = GetDms(absoluteValue) + direction;

            // longitude
            if (centerLongitude < 0)
            {
                direction = "W";
                absoluteValue = -centerLongitude;
            }
            else
            {
                direction = "E";
                absoluteValue = centerLongitude;
            }

            string dmsLongitude = GetDms(absoluteValue) + direction;

            string url = string.Format("https://www.google.ie/maps/place/{0}+{1}/@{2},{3},{4}z", dmsLatitude, dmsLongitude, centerLatitude, centerLongitude, zoom);
            return url;
        }

        public string GetStaticMapUrl(double centerLatitude, double centerLongitude, int zoom, double? markerLatitude, double? markerLongitude, int width = 1200, int height = 1200)
        {
            string googleStaticMapsApiKey = _appSettings.GoogleStaticMapsApiKey;

            string keyString = string.IsNullOrWhiteSpace(googleStaticMapsApiKey) ? "" : "&key=" + googleStaticMapsApiKey;
            string markerLatitudeStr = markerLatitude.HasValue ? markerLatitude.Value.ToString(CultureInfo.InvariantCulture) : "0";
            string markerLongitudeStr = markerLongitude.HasValue ? markerLongitude.Value.ToString(CultureInfo.InvariantCulture) : "0";

            const string mapBaseUri = "https://maps.googleapis.com/maps/api/staticmap?center={0},{1}&scale=2&zoom={2}&size={6}x{7}&format=png32&scale=1&maptype=roadmap&markers=size:mid|{3},{4}{5}";

            string mapFullUri = string.Format(mapBaseUri, centerLatitude.ToString(CultureInfo.InvariantCulture), centerLongitude.ToString(CultureInfo.InvariantCulture),
                zoom.ToString(CultureInfo.InvariantCulture), markerLatitudeStr,
                markerLongitudeStr, keyString, width.ToString(CultureInfo.InvariantCulture),
                height.ToString(CultureInfo.InvariantCulture));

            return mapFullUri;
        }

        private static string GetDms(double value)
        {
            double decimalDegrees = (double)value;
            double degrees = Math.Floor(decimalDegrees);
            double minutes = (decimalDegrees - Math.Floor(decimalDegrees)) * 60.0;
            double seconds = (minutes - Math.Floor(minutes)) * 60.0;
            double tenths = (seconds - Math.Floor(seconds)) * 1000.0;
            // get rid of fractional part
            minutes = Math.Floor(minutes);
            seconds = Math.Floor(seconds);
            tenths = Math.Floor(tenths);

            string result = string.Format("{0}°{1}'{2}.{3}\"", degrees, minutes, seconds, tenths);

            return result;
        }
    }
}