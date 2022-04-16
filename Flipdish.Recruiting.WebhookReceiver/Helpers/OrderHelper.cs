using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using Flipdish.Recruiting.WebhookReceiver.Models;

namespace Flipdish.Recruiting.WebhookReceiver.Helpers
{
    public static class OrderHelper
    {
        public static List<MenuSectionGrouped> GetMenuSectionGroupedList(List<OrderItem> orderItems, string barcodeMetadataKey)
        {
            var result = new List<MenuSectionGrouped>();

            var sectionNames = orderItems.Select(a => new { a.MenuSectionName, a.MenuSectionDisplayOrder }).Distinct().ToList();
            var menuSectionDisplayOrder = 0;
            foreach (var sectionName in sectionNames.OrderBy(a => a.MenuSectionDisplayOrder).Select(a => a.MenuSectionName))
            {
                var menuItemsGroupedList = new List<MenuItemsGrouped>();
                var menuItemDisplayOrder = 0;
                foreach (var item in orderItems.Where(a => a.MenuSectionName == sectionName).OrderBy(a => a.MenuItemDisplayOrder))
                {
                    var menuItemUI = new MenuItemUI(item, barcodeMetadataKey);
                    var menuItemsGrouped = menuItemsGroupedList.SingleOrDefault(a => a.MenuItemUI.HashCode == menuItemUI.HashCode);

                    if (menuItemsGrouped != null)
                    {
                        menuItemsGrouped.Count++;
                    }
                    else
                    {
                        menuItemsGrouped = new MenuItemsGrouped
                        {
                            MenuItemUI = menuItemUI,
                            Count = 1,
                            DisplayOrder = menuItemDisplayOrder++
                        };

                        menuItemsGroupedList.Add(menuItemsGrouped);
                    }
                }

                var menuSectionGrouped = new MenuSectionGrouped
                {
                    Name = sectionName,
                    DisplayOrder = menuSectionDisplayOrder++,
                    MenuItemsGroupedList = menuItemsGroupedList
                };

                result.Add(menuSectionGrouped);
            }

            return result;
        }

        public static string ToCurrencyString(this decimal l, Currency currency, CultureInfo cultureInfo)
        {
            var numberFormatInfo = cultureInfo.NumberFormat;
            numberFormatInfo.CurrencySymbol = currency.ToSymbol(); // Replace with "$" or "£" or whatever you need

            var formattedPrice = l.ToString("C", numberFormatInfo);

            return formattedPrice;
        }

        public static string ToCurrencyString(this double l, Currency currency, CultureInfo cultureInfo)
        {
            var numberFormatInfo = cultureInfo.NumberFormat;
            numberFormatInfo.CurrencySymbol = currency.ToSymbol(); // Replace with "$" or "£" or whatever you need

            var formattedPrice = l.ToString("C", numberFormatInfo);

            return formattedPrice;
        }

        public static string ToCurrencyString(this decimal l, Currency currency)
        {
            var cultureInfo = new CultureInfo(Thread.CurrentThread.CurrentUICulture.IetfLanguageTag);
            return ToCurrencyString(l, currency, cultureInfo);
        }

        public static string ToCurrencyString(this double l, Currency currency)
        {
            var cultureInfo = new CultureInfo(Thread.CurrentThread.CurrentUICulture.IetfLanguageTag);
            return ToCurrencyString(l, currency, cultureInfo);
        }

        public static string ToRawHtmlCurrencyString(this decimal l, Currency currency)
        {
            var currencyString = l.ToCurrencyString(currency);
            var result = WebUtility.HtmlEncode(currencyString);

            return result.Replace(" ", "&nbsp;");
        }

        public static string ToRawHtmlCurrencyString(this double l, Currency currency)
        {
            var currencyString = l.ToCurrencyString(currency);
            var result = WebUtility.HtmlEncode(currencyString);

            return result.Replace(" ", "&nbsp;");
        }

        public static string ToSymbol(this Currency c)
        {
            return c.GetCurrencyItem().Symbol;
        }

        public static CurrencyItem GetCurrencyItem(this Currency currency)
        {
            var ci = new CurrencyItem
            {
                Currency = currency,
                IsoCode = currency.ToString().ToUpper(),
                Symbol = CurrencyCodeMapper.IsoCodeToSymbol(currency.ToString().ToUpper())
            };

            return ci;
        }

        public static string GetTableServiceCategoryLabel(this Order.TableServiceCatagoryEnum tableServiceCatagory)
        {
            return tableServiceCatagory switch
            {
                Order.TableServiceCatagoryEnum.Generic => "Generic Service n ",
                Order.TableServiceCatagoryEnum.Villa => "Villa Service n ",
                Order.TableServiceCatagoryEnum.House => "House Service n ",
                Order.TableServiceCatagoryEnum.Room => "Room Service n ",
                Order.TableServiceCatagoryEnum.Area => "Area Service n ",
                Order.TableServiceCatagoryEnum.Table => "Table Service n ",
                Order.TableServiceCatagoryEnum.ParkingBay => ".Parking Bay Service n ",
                Order.TableServiceCatagoryEnum.Gate => "Gate Service n ",
                _ => ">",
            };
        }

        public static DateTime UtcToLocalTime(this DateTime utcTime, string timeZoneInfoId)
        {
            // Getting strange exceptions when we have invalid times, near hour changes.
            // If any better developer than me can fix this, I'd be much obliged. CM
            // http://stackoverflow.com/questions/36422138/datetime-parsing-error-the-supplied-datetime-represents-an-invalid-time
            try
            {
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneInfoId);
                return TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZoneInfo);
            }
            catch (Exception)
            {
                return utcTime;
            }
        }
    }
}