using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Flipdish.Recruiting.WebhookReceiver.Helpers
{
    internal static class CurrencyCodeMapper
    {
        private static readonly Dictionary<string, string> SymbolsByCode;

        public static Dictionary<string, string> IsoCountryCodesAndSymbols
        {
            get
            {
                return SymbolsByCode.ToDictionary(entry => entry.Key, entry => entry.Value);
            }
        }

        public static string IsoCodeToSymbol(string isoCode)
        {
            return SymbolsByCode[isoCode];
        }

        static CurrencyCodeMapper()
        {
            SymbolsByCode = new Dictionary<string, string>();
            var regions = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .Select(x => new RegionInfo(x.Name))
                .ToList();

            foreach (var region in regions)
            {
                if (!SymbolsByCode.ContainsKey(region.ISOCurrencySymbol.ToUpper()))
                {
                    SymbolsByCode.Add(region.ISOCurrencySymbol.ToUpper(), region.CurrencySymbol);
                }
            }
        }
    }
}