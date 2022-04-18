namespace Flipdish.Recruiting.WebhookReceiver.Services
{
    public interface IMapService
    {
        string GetDynamicMapUrl(double centerLatitude, double centerLongitude, int zoom);
        string GetStaticMapUrl(double centerLatitude, double centerLongitude, int zoom, double? markerLatitude, double? markerLongitude, int width = 1200, int height = 1200);
    }
}