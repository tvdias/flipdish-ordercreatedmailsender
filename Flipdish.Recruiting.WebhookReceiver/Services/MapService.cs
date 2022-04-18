using System;
using System.Globalization;
using Flipdish.Recruiting.WebhookReceiver.Config;
using Microsoft.Extensions.Options;

namespace Flipdish.Recruiting.WebhookReceiver.Services
{
    public class MapService : IMapService
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

            var dmsLatitude = GetDms(absoluteValue) + direction;

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

            var dmsLongitude = GetDms(absoluteValue) + direction;

            var url = string.Format("https://www.google.ie/maps/place/{0}+{1}/@{2},{3},{4}z", dmsLatitude, dmsLongitude, centerLatitude, centerLongitude, zoom);
            return url;
        }

        public string GetStaticMapUrl(double centerLatitude, double centerLongitude, int zoom, double? markerLatitude, double? markerLongitude, int width = 1200, int height = 1200)
        {
            var googleStaticMapsApiKey = _appSettings.GoogleStaticMapsApiKey;

            var keyString = string.IsNullOrWhiteSpace(googleStaticMapsApiKey) ? "" : "&key=" + googleStaticMapsApiKey;
            var markerLatitudeStr = markerLatitude.HasValue ? markerLatitude.Value.ToString(CultureInfo.InvariantCulture) : "0";
            var markerLongitudeStr = markerLongitude.HasValue ? markerLongitude.Value.ToString(CultureInfo.InvariantCulture) : "0";

            const string mapBaseUri = "https://maps.googleapis.com/maps/api/staticmap?center={0},{1}&scale=2&zoom={2}&size={6}x{7}&format=png32&scale=1&maptype=roadmap&markers=size:mid|{3},{4}{5}";

            var mapFullUri = string.Format(mapBaseUri, centerLatitude.ToString(CultureInfo.InvariantCulture), centerLongitude.ToString(CultureInfo.InvariantCulture),
                zoom.ToString(CultureInfo.InvariantCulture), markerLatitudeStr,
                markerLongitudeStr, keyString, width.ToString(CultureInfo.InvariantCulture),
                height.ToString(CultureInfo.InvariantCulture));

            return mapFullUri;
        }

        private static string GetDms(double value)
        {
            var decimalDegrees = (double)value;
            var degrees = Math.Floor(decimalDegrees);
            var minutes = (decimalDegrees - Math.Floor(decimalDegrees)) * 60.0;
            var seconds = (minutes - Math.Floor(minutes)) * 60.0;
            var tenths = (seconds - Math.Floor(seconds)) * 1000.0;
            // get rid of fractional part
            minutes = Math.Floor(minutes);
            seconds = Math.Floor(seconds);
            tenths = Math.Floor(tenths);

            var result = string.Format("{0}°{1}'{2}.{3}\"", degrees, minutes, seconds, tenths);

            return result;
        }
    }
}