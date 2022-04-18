using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using DotLiquid;
using Flipdish.Recruiting.WebhookReceiver.Config;
using Flipdish.Recruiting.WebhookReceiver.Helpers;
using Flipdish.Recruiting.WebhookReceiver.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetBarcode;

[assembly: InternalsVisibleTo("Flipdish.Recruiting.WebhookReceiverTests")]

namespace Flipdish.Recruiting.WebhookReceiver.Services
{
    public class EmailRendererService
    {
        private readonly ILogger _log;
        private readonly AppSettings _appSettings;
        private readonly IMapService _mapService;

        private const string SPACE_DIVIDER = "<tr><td colspan=\"2\" align =\"center\" valign=\"top\"><table width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\" align=\"center\" style=\"height: 22px;\"></table></td></tr>";
        private const string LINE_DIVIDER = "<tr><td colspan=\"2\" align =\"center\" valign=\"top\"><table width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\" align=\"center\" style=\"height: 1px; background-color: rgb(186, 186, 186);\"></table></td></tr>";

        public EmailRendererService(ILogger<EmailRendererService> log, IOptions<AppSettings> appSettings, IMapService mapService)
        {
            _log = log;
            _appSettings = appSettings.Value;
            _mapService = mapService;
        }

        public string RenderEmailOrder(Order order, string appNameId, string barcodeMetadataKey, Currency currency, out Dictionary<string, Stream> imagesWithNames)
        {
            imagesWithNames = new Dictionary<string, Stream>();

            var preorder_partial = order.IsPreOrder == true ? GetPreorderPartial(order) : null;
            var order_status_partial = GetOrderStatusPartial(order, appNameId);
            var order_items_partial = GetOrderItemsPartial(order, barcodeMetadataKey, currency, imagesWithNames);
            var customer_details_partial = GetCustomerDetailsPartial(order);

            var template = GetLiquidTemplate("RestaurantOrderDetail.liquid");

            var domain = _appSettings.FlipdishDomainWithScheme;
            var orderId = order.OrderId.Value;
            var mapUrl = string.Empty;
            var staticMapUrl = string.Empty;
            double? airDistance = null;
            var supportNumber = _appSettings.RestaurantSupportNumber;
            var physicalRestaurantName = order.Store.Name;
            var paymentAccountDescription = order.PaymentAccountDescription;
            var deliveryTypeNum = (int)order.DeliveryType;
            var orderPlacedLocal = order.PlacedTime.Value.UtcToLocalTime(order.Store.StoreTimezone);
            var tsOrderPlaced = EtaResponseMethods.GetClocksToString(orderPlacedLocal);
            var tsOrderPlacedDayMonth = EtaResponseMethods.GetDateString(orderPlacedLocal);
            var paid_unpaid = order.PaymentAccountType != Order.PaymentAccountTypeEnum.Cash ? "PAID" : "UNPAID";
            var foodAmount = order.OrderItemsAmount.Value.ToRawHtmlCurrencyString(currency);
            var onlineProcessingFee = order.ProcessingFee.Value.ToRawHtmlCurrencyString(currency);
            var deliveryAmount = order.DeliveryAmount.Value.ToRawHtmlCurrencyString(currency);
            var tipAmount = order.TipAmount.Value.ToRawHtmlCurrencyString(currency);
            var totalRestaurantAmount = order.Amount.Value.ToRawHtmlCurrencyString(currency);
            var voucherAmount = order.Voucher != null ? order.Voucher.Amount.Value.ToRawHtmlCurrencyString(currency) : "0";

            FillGeoData(order, ref mapUrl, ref staticMapUrl, ref airDistance);

            var airDistanceStr = airDistance.HasValue ? Math.Round(airDistance.Value, 1).ToString() : "?";
            var currentYear = DateTime.UtcNow.Year.ToString();

            var orderMsg = CreateOrderMessage(order);

            // TODO: Do we really need this?
            const string openingTag1 = "<span style=\"color: #222; background: #ffc; font-weight: bold; \">";
            const string closingTag1 = "</span>";
            orderMsg = Regex.Replace(orderMsg, "[ ]", "&nbsp;");
            var resNew_DeliveryType_Order = string.Format(orderMsg, openingTag1, closingTag1);

            const string resPAID = "PAID";
            const string resUNPAID = "UNPAID";
            var resDistance = string.Format("{0} km from restaurant", airDistanceStr);

            // TODO: Remove comment below?
            const string taxAmount = null;// (physicalRestaurant?.Menu?.DisplayTax ?? false) ? order.TotalTax.ToRawHtmlCurrencyString(order.Currency) : null;

            var resCall_the_Flipdish_ = $"Call the Flipdish Hotline at <span style=\"font-weight: bold; font-size: inherit; line-height: 24px;color: rgb(208, 93, 104); \">{supportNumber}</span>";

            const string resRestaurant_New_Order_Mail = "Restaurant New Order Mail";
            const string resVIEW_ONLINE = "VIEW ONLINE";
            const string resFood_Total = "Food Total";
            const string resVoucher = "Voucher";
            const string resProcessing_Fee = "Processing Fee";
            const string resDelivery_Fee = "Delivery Fee";
            const string resTip_Amount = "Tip Amount";
            const string resTotal = "Total";
            const string resCustomer_Location = "Customer Location";
            const string resTax = "Tax";

            var paramaters = new RenderParameters(CultureInfo.CurrentCulture)
            {
                LocalVariables = Hash.FromAnonymousObject(new
                {
                    order_status_partial,
                    order_items_partial,
                    customer_details_partial,
                    preorder_partial,
                    physicalRestaurantName,
                    mapUrl,
                    staticMapUrl,
                    resCall_the_Flipdish_,
                    airDistanceStr,
                    paymentAccountDescription,
                    deliveryTypeNum,
                    tsOrderPlaced,
                    tsOrderPlacedDayMonth,
                    paid_unpaid,
                    domain,
                    orderId,
                    foodAmount,
                    onlineProcessingFee,
                    deliveryAmount,
                    tipAmount,
                    totalRestaurantAmount,
                    currentYear,
                    voucherAmount,
                    resNew_DeliveryType_Order,
                    resPAID,
                    resUNPAID,
                    resRestaurant_New_Order_Mail,
                    resVIEW_ONLINE,
                    resFood_Total,
                    resVoucher,
                    resProcessing_Fee,
                    resDelivery_Fee,
                    resTip_Amount,
                    resTotal,
                    resCustomer_Location,
                    resDistance,
                    appNameId,
                    taxAmount,
                    resTax
                }),
                Filters = new[] { typeof(CurrencyFilter) }
            };

            return template.Render(paramaters);
        }

        private void FillGeoData(Order order, ref string mapUrl, ref string staticMapUrl, ref double? airDistance)
        {
            if ((order.Store.Coordinates?.Latitude) == null || order.Store.Coordinates.Longitude == null)
            {
                return;
            }

            if (order.DeliveryType == Order.DeliveryTypeEnum.Delivery &&
                order.DeliveryLocation.Coordinates?.Latitude != null && order.DeliveryLocation.Coordinates?.Longitude != null)
            {
                mapUrl = _mapService.GetDynamicMapUrl(
                        order.DeliveryLocation.Coordinates.Latitude.Value,
                        order.DeliveryLocation.Coordinates.Longitude.Value,
                        18);

                staticMapUrl = _mapService.GetStaticMapUrl(
                    order.DeliveryLocation.Coordinates.Latitude.Value,
                    order.DeliveryLocation.Coordinates.Longitude.Value,
                    18,
                    order.DeliveryLocation.Coordinates.Latitude.Value,
                    order.DeliveryLocation.Coordinates.Longitude.Value);

                airDistance = GeoUtils.GetAirDistance(order.DeliveryLocation.Coordinates, order.Store.Coordinates);
                return;
            }

            if (order.DeliveryType == Order.DeliveryTypeEnum.Pickup &&
                     order.CustomerLocation?.Latitude != null && order.CustomerLocation?.Longitude != null)
            {
                airDistance = GeoUtils.GetAirDistance(order.CustomerLocation, order.Store.Coordinates);
            }
        }

        private static string CreateOrderMessage(Order order)
        {
            if (order.DeliveryType == Order.DeliveryTypeEnum.Delivery)
            {
                return "NEW DELIVERY ORDER";
            }

            if (order.DeliveryType == Order.DeliveryTypeEnum.Pickup)
            {
                return order.PickupLocationType switch
                {
                    Order.PickupLocationTypeEnum.TakeOut => "NEW COLLECTION ORDER ",
                    Order.PickupLocationTypeEnum.TableService => "NEW TABLE SERVICE ORDER ",
                    Order.PickupLocationTypeEnum.DineIn => "NEW DINE IN ORDER ",
                    _ => $"NEW {order.PickupLocationType} ORDER".ToUpper(),
                };
            }

            throw new Exception("Unknown DeliveryType.");
        }

        private string GetPreorderPartial(Order order)
        {
            var template = GetLiquidTemplate("PreorderPartial.liquid");

            var reqForLocal = order.RequestedForTime.Value.UtcToLocalTime(order.Store.StoreTimezone);

            var reqestedForDateStr = EtaResponseMethods.GetDateString(reqForLocal);
            var reqestedForTimeStr = EtaResponseMethods.GetClocksToString(reqForLocal);

            const string resPREORDER_FOR = "PREORDER FOR";

            var paramaters = new RenderParameters(CultureInfo.CurrentCulture)
            {
                LocalVariables = Hash.FromAnonymousObject(new
                {
                    reqestedForDateStr,
                    reqestedForTimeStr,
                    resPREORDER_FOR
                })
            };

            return template.Render(paramaters);
        }

        private string GetOrderStatusPartial(Order order, string appNameId)
        {
            var orderId = order.OrderId.Value;
            var webLink = string.Format(_appSettings.EmailServiceOrderUrl, appNameId, orderId);

            const string resOrder = "Order";
            const string resView_Order = "View Order";

            var template = GetLiquidTemplate("OrderStatusPartial.liquid");
            var paramaters = new RenderParameters(CultureInfo.CurrentCulture)
            {
                LocalVariables = Hash.FromAnonymousObject(new
                {
                    webLink,
                    orderId,
                    resOrder,
                    resView_Order
                })
            };

            return template.Render(paramaters);
        }

        private string GetOrderItemsPartial(Order order, string barcodeMetadataKey, Currency currency, Dictionary<string, Stream> imagesWithNames)
        {
            var template = GetLiquidTemplate("OrderItemsPartial.liquid");

            var chefNote = order.ChefNote;
            var itemsPart = GetItemsPart(order, barcodeMetadataKey, currency, imagesWithNames);

            const string resSection = "Section";
            const string resItems = "Items";
            const string resOptions = "Options";
            const string resPrice = "Price";
            const string resChefNotes = "Chef Notes";

            const string customerLocationLabel = "Customer Location";

            var customerPickupLocation = GetCustomerPickupLocationMessage(order);

            var paramaters = new RenderParameters(CultureInfo.CurrentCulture)
            {
                LocalVariables = Hash.FromAnonymousObject(new
                {
                    chefNote,
                    itemsPart,
                    resSection,
                    resItems,
                    resOptions,
                    resPrice,
                    resChefNotes,
                    customerLocationLabel,
                    customerPickupLocation
                })
            };

            return template.Render(paramaters);
        }

        private string GetCustomerDetailsPartial(Order order)
        {
            var template = GetLiquidTemplate("CustomerDetailsPartial.liquid");

            var domain = _appSettings.FlipdishDomainWithScheme;
            var customerName = order.Customer.Name;
            var deliveryInstructions = order.DeliveryLocation?.DeliveryInstructions;
            var deliveryLocationAddressString = order.DeliveryLocation?.PrettyAddressString;

            var phoneNumber = order.Customer.PhoneNumberLocalFormat;
            var isDelivery = order.DeliveryType == Order.DeliveryTypeEnum.Delivery;

            const string resDelivery_Instructions = "Delivery Instructions";

            var paramaters = new RenderParameters(CultureInfo.CurrentCulture)
            {
                LocalVariables = Hash.FromAnonymousObject(new
                {
                    domain,
                    customerName,
                    deliveryInstructions,
                    deliveryLocationAddressString,
                    phoneNumber,
                    isDelivery,
                    resDelivery_Instructions
                })
            };

            return template.Render(paramaters);
        }

        private string GetItemsPart(Order order, string barcodeMetadataKey, Currency currency, Dictionary<string, Stream> imagesWithNames)
        {
            var itemsPart = new StringBuilder();

            itemsPart.AppendLine("<tr><td cellpadding=\"2px\" valign=\"top\" style=\"font-weight: bold;\">Order items</td><td cellpadding=\"2px\" valign=\"top\"></td></tr>");

            foreach (var section in GetMenuSectionGroupedList(order.OrderItems, barcodeMetadataKey))
            {
                itemsPart.AppendLine(SPACE_DIVIDER);
                itemsPart
                    .Append("<tr><td cellpadding=\"2px\" valign=\"top\" style=\"font-size: 14px;\">")
                    .Append(section.Name.ToUpper())
                    .AppendLine("</td><td cellpadding=\"2px\" valign=\"top\" style=\"font-size: 14px;\"></td></tr>")
                    .AppendLine(LINE_DIVIDER)
                    .AppendLine(SPACE_DIVIDER);

                foreach (var item in section.MenuItemsGroupedList)
                {
                    var countStr = item.Count > 1 ? $"{item.Count} x " : string.Empty;
                    var itemPriceStr = item.MenuItemUI.Price.HasValue ? (item.MenuItemUI.Price.Value * item.Count).ToRawHtmlCurrencyString(currency) : string.Empty;

                    itemsPart.AppendLine("<tr>");

                    itemsPart
                        .Append("<td cellpadding=\"2px\" valign=\"middle\" style=\"padding-left: 40px;\">")
                        .Append(countStr)
                        .Append(item.MenuItemUI.Name)
                        .AppendLine("</td>");

                    itemsPart
                        .Append("<td cellpadding=\"2px\" valign=\"middle\">")
                        .Append(itemPriceStr)
                        .AppendLine("</td>");

                    if (!string.IsNullOrEmpty(item.MenuItemUI.Barcode) && HasBarcode(item.MenuItemUI.Barcode, order, imagesWithNames))
                    {
                        itemsPart
                            .Append("<td cellpadding=\"2px\" valign=\"middle\"><img style=\"margin-left: 14px;margin-left: 9px;padding-top: 10px; padding-bottom:10px\" src=\"cid:")
                            .Append(item.MenuItemUI.Barcode)
                            .AppendLine(".png\"/></td>");

                        if (item.Count > 1)
                        {
                            itemsPart.AppendLine("<td cellpadding=\"2px\" valign=\"middle\" style=\"font-size:40px\">x</td>");
                            itemsPart
                                .Append("<td cellpadding=\"2px\" valign=\"middle\" style=\"font-size:50px\">")
                                .Append(item.Count)
                                .AppendLine("</td>");
                        }
                    }

                    itemsPart.AppendLine("</tr>");

                    foreach (var option in item.MenuItemUI.MenuOptions)
                    {
                        itemsPart.AppendLine("<tr>");
                        itemsPart
                            .Append("<td cellpadding=\"2px\" valign=\"middle\" style=\"padding-left: 40px;padding-top: 10px; padding-bottom:10px\">+ ")
                            .Append(option.Name)
                            .AppendLine("</td>");
                        itemsPart
                            .Append("<td cellpadding=\"2px\" valign=\"middle\">")
                            .Append((option.Price * item.Count).ToRawHtmlCurrencyString(currency))
                            .AppendLine("</td>");

                        if (!string.IsNullOrEmpty(option.Barcode) && HasBarcode(option.Barcode, order, imagesWithNames))
                        {
                            itemsPart.Append("<td cellpadding=\"2px\" valign=\"middle\"><img style=\"margin-left: 14px;margin-left: 9px;padding-top: 10px; padding-bottom:10px\" src=\"cid:").Append(option.Barcode).AppendLine(".png\"/></td>");
                        }

                        itemsPart.AppendLine("</tr>");
                    }
                }
            }

            return itemsPart.ToString();
        }

        private bool HasBarcode(string barcodeIdentifier, Order order, Dictionary<string, Stream> imagesWithNames)
        {
            if (imagesWithNames.ContainsKey(barcodeIdentifier + ".png"))
            {
                return true;
            }

            if (GetBase64EAN13Barcode(barcodeIdentifier, order, out var barcodeStream))
            {
                imagesWithNames.Add(barcodeIdentifier + ".png", barcodeStream);
                return true;
            }

            return false;
        }

        private bool GetBase64EAN13Barcode(string barcodeNumbers, Order order, out Stream barcodeStream)
        {
            try
            {
                var barcode = new Barcode(barcodeNumbers, true, 130, 110, LabelPosition.BottomCenter);
                barcodeStream = new MemoryStream(barcode.GetByteArray());
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "{barcodeNumbers} is not a valid barcode for order #{OrderId}", barcodeNumbers, order.OrderId);
            }

            barcodeStream = null;
            return false;
        }

        private static string GetCustomerPickupLocationMessage(Order order)
        {
            if (!order.DropOffLocationId.HasValue || order.PickupLocationType != Order.PickupLocationTypeEnum.TableService)
            {
                return null;
            }

            var tableServiceCategoryMessage = order.TableServiceCatagory.Value.GetTableServiceCategoryLabel();
            return $"{tableServiceCategoryMessage}: {order.DropOffLocation}";
        }

        private static Template GetLiquidTemplate(string fileName)
        {
            var templateFilePath = Path.Combine("./LiquidTemplates", fileName);
            return Template.Parse(new StreamReader(templateFilePath).ReadToEnd());
        }

        public static List<MenuSectionGrouped> GetMenuSectionGroupedList(List<OrderItem> orderItems, string barcodeMetadataKey)
        {
            var result = new List<MenuSectionGrouped>();

            var sortedOrderItems = orderItems
                .OrderBy(a => a.MenuSectionDisplayOrder)
                .ThenBy(a => a.MenuSectionName)
                .ThenBy(a => a.MenuItemDisplayOrder)
                .ThenBy(a => a.Name) // assuming using order + name is safe enought for sorting
                .ToList();

            var currentMenuItemDisplayOrder = int.MinValue; // assuming no entry will ever use it
            var menuItemDisplayOrder = 0;
            var currentSectionDisplayOrder = 0;
            MenuSectionGrouped currentSection = null;
            MenuItemsGrouped menuItemsGrouped = null;

            foreach (var item in sortedOrderItems)
            {
                // if it's a different section, let's initialize it!
                if (item.MenuSectionName != currentSection?.Name)
                {
                    currentMenuItemDisplayOrder = int.MinValue;
                    menuItemDisplayOrder = 0;

                    currentSection = new MenuSectionGrouped
                    {
                        Name = item.MenuSectionName,
                        DisplayOrder = currentSectionDisplayOrder++,
                        MenuItemsGroupedList = new List<MenuItemsGrouped>()
                    };

                    result.Add(currentSection);
                }

                // if it's a different currentMenuItemDisplayOrder then it's a different item
                if (item.MenuItemDisplayOrder != currentMenuItemDisplayOrder
                    // when having the same currentMenuItemDisplayOrder, item name is used for double check
                    || (item.Name != menuItemsGrouped.MenuItemUI.Name)
                    // and item hashcode as last resort (probably not needed)
                    || (new MenuItemUI(item, barcodeMetadataKey).GetHashCode() != menuItemsGrouped.MenuItemUI.GetHashCode()))
                {
                    menuItemsGrouped = new MenuItemsGrouped
                    {
                        MenuItemUI = new MenuItemUI(item, barcodeMetadataKey),
                        Count = 1,
                        DisplayOrder = menuItemDisplayOrder++
                    };

                    currentSection.MenuItemsGroupedList.Add(menuItemsGrouped);
                }
                else
                {
                    menuItemsGrouped.Count++;
                }
            }

            return result;
        }
    }
}