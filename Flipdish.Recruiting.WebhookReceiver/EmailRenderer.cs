using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DotLiquid;
using Flipdish.Recruiting.WebhookReceiver.Config;
using Flipdish.Recruiting.WebhookReceiver.Helpers;
using Flipdish.Recruiting.WebhookReceiver.Models;
using Flipdish.Recruiting.WebhookReceiver.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetBarcode;

namespace Flipdish.Recruiting.WebhookReceiver
{
    public class EmailRenderer : IDisposable
    {
        private readonly ILogger _log;
        private readonly AppSettings _appSettings;
        private readonly MapService _mapService;

        public EmailRenderer(ILogger log, IOptions<AppSettings> appSettings, MapService mapService)
        {
            _log = log;
            _appSettings = appSettings.Value;
            _mapService = mapService;
        }

        public string RenderEmailOrder(Order order, string appNameId, string barcodeMetadataKey, Currency currency)
        {
            string preorder_partial = order.IsPreOrder == true ? GetPreorderPartial(order) : null;
            string order_status_partial = GetOrderStatusPartial(order, appNameId);
            string order_items_partial = GetOrderItemsPartial(order, barcodeMetadataKey, currency);
            string customer_details_partial = GetCustomerDetailsPartial(order);

            string templateStr = GetLiquidFileAsString("RestaurantOrderDetail.liquid");
            Template template = Template.Parse(templateStr);

            string domain = _appSettings.FlipdishDomainWithScheme;
            int orderId = order.OrderId.Value;
            string mapUrl = String.Empty;
            string staticMapUrl = String.Empty;
            double? airDistance = null;
            string supportNumber = _appSettings.RestaurantSupportNumber;
            string physicalRestaurantName = order.Store.Name;
            string paymentAccountDescription = order.PaymentAccountDescription;
            int deliveryTypeNum = (int)order.DeliveryType;
            var orderPlacedLocal = order.PlacedTime.Value.UtcToLocalTime(order.Store.StoreTimezone);
            string tsOrderPlaced = EtaResponseMethods.GetClocksToString(orderPlacedLocal);
            string tsOrderPlacedDayMonth = EtaResponseMethods.GetDateString(orderPlacedLocal);
            string paid_unpaid = order.PaymentAccountType != Order.PaymentAccountTypeEnum.Cash ? "PAID" : "UNPAID";
            string foodAmount = order.OrderItemsAmount.Value.ToRawHtmlCurrencyString(currency);
            string onlineProcessingFee = order.ProcessingFee.Value.ToRawHtmlCurrencyString(currency);
            string deliveryAmount = order.DeliveryAmount.Value.ToRawHtmlCurrencyString(currency);
            string tipAmount = order.TipAmount.Value.ToRawHtmlCurrencyString(currency);
            string totalRestaurantAmount = order.Amount.Value.ToRawHtmlCurrencyString(currency);
            string voucherAmount = order.Voucher != null ? order.Voucher.Amount.Value.ToRawHtmlCurrencyString(currency) : "0";

            if (order.Store.Coordinates?.Latitude != null && order.Store.Coordinates.Longitude != null)
            {
                if (order.DeliveryType == Order.DeliveryTypeEnum.Delivery &&
                    order.DeliveryLocation.Coordinates != null)
                {
                    mapUrl =
                        _mapService.GetDynamicMapUrl(
                            order.DeliveryLocation.Coordinates.Latitude.Value,
                            order.DeliveryLocation.Coordinates.Longitude.Value, 18);
                    staticMapUrl = _mapService.GetStaticMapUrl(
                        order.DeliveryLocation.Coordinates.Latitude.Value,
                        order.DeliveryLocation.Coordinates.Longitude.Value,
                        18,
                        order.DeliveryLocation.Coordinates.Latitude.Value,
                        order.DeliveryLocation.Coordinates.Longitude.Value
                        );
                    var deliveryLocation = new Coordinates(
                        order.DeliveryLocation.Coordinates.Latitude.Value,
                        order.DeliveryLocation.Coordinates.Longitude.Value);
                    var storeCoordinates = new Coordinates(
                        order.Store.Coordinates.Latitude.Value,
                        order.Store.Coordinates.Longitude.Value);
                    airDistance = GeoUtils.GetAirDistance(deliveryLocation, storeCoordinates);
                }
                else if (order.DeliveryType == Order.DeliveryTypeEnum.Pickup &&
                         order.CustomerLocation != null)
                {
                    Coordinates userLocation =
                         new Coordinates(
                            order.CustomerLocation.Latitude.Value,
                            order.CustomerLocation.Longitude.Value);
                    var storeCoordinates = new Coordinates(
                        order.Store.Coordinates.Latitude.Value,
                        order.Store.Coordinates.Longitude.Value);
                    airDistance = GeoUtils.GetAirDistance(userLocation, storeCoordinates);
                }
            }

            if (airDistance.HasValue)
            {
                airDistance = Math.Round(airDistance.Value, 1);
            }

            string airDistanceStr = airDistance.HasValue ? airDistance.Value.ToString() : "?";
            string currentYear = DateTime.UtcNow.Year.ToString();

            string orderMsg;
            if (order.DeliveryType == Order.DeliveryTypeEnum.Delivery)
            {
                orderMsg = "NEW DELIVERY ORDER";
            }
            else if (order.DeliveryType == Order.DeliveryTypeEnum.Pickup)
            {
                switch (order.PickupLocationType)
                {
                    case Order.PickupLocationTypeEnum.TakeOut:
                        orderMsg = "NEW COLLECTION ORDER ";
                        break;

                    case Order.PickupLocationTypeEnum.TableService:
                        orderMsg = "NEW TABLE SERVICE ORDER ";
                        break;

                    case Order.PickupLocationTypeEnum.DineIn:
                        orderMsg = "NEW DINE IN ORDER ";
                        break;

                    default:
                        string orderMsgLower = $"NEW {order.PickupLocationType} ORDER";
                        orderMsg = orderMsgLower.ToUpper();
                        break;
                }
            }
            else
            {
                throw new Exception("Unknown DeliveryType.");
            }
            const string openingTag1 = "<span style=\"color: #222; background: #ffc; font-weight: bold; \">";
            const string closingTag1 = "</span>";
            orderMsg = Regex.Replace(orderMsg, "[ ]", "&nbsp;");
            string resNew_DeliveryType_Order = string.Format(orderMsg, openingTag1, closingTag1);

            const string resPAID = "PAID";
            const string resUNPAID = "UNPAID";
            string resDistance = string.Format("{0} km from restaurant", airDistanceStr);

            const string openingTag2 = "<span style=\"font-weight: bold; font-size: inherit; line-height: 24px;color: rgb(208, 93, 104); \">";
            const string closingTag2 = "</span>";

            const string taxAmount = null;// (physicalRestaurant?.Menu?.DisplayTax ?? false) ? order.TotalTax.ToRawHtmlCurrencyString(order.Currency) : null;

            string resCall_the_Flipdish_ = string.Format("Call the Flipdish Hotline at {0}", openingTag2 + supportNumber + closingTag2);

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

        private string GetPreorderPartial(Order order)
        {
            string templateStr = GetLiquidFileAsString("PreorderPartial.liquid");
            Template template = Template.Parse(templateStr);

            DateTime reqForLocal = order.RequestedForTime.Value.UtcToLocalTime(order.Store.StoreTimezone);

            string reqestedForDateStr = EtaResponseMethods.GetDateString(reqForLocal);
            string reqestedForTimeStr = EtaResponseMethods.GetClocksToString(reqForLocal);

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

        private string GetLiquidFileAsString(string fileName)
        {
            var templateFilePath = Path.Combine("./LiquidTemplates", fileName);
            return new StreamReader(templateFilePath).ReadToEnd();
        }

        private string GetOrderStatusPartial(Order order, string appNameId)
        {
            int orderId = order.OrderId.Value;
            string webLink = string.Format(_appSettings.EmailServiceOrderUrl, appNameId, orderId);

            const string resOrder = "Order";
            const string resView_Order = "View Order";

            string templateStr = GetLiquidFileAsString("OrderStatusPartial.liquid");
            DotLiquid.Template template = Template.Parse(templateStr);
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

        private string GetOrderItemsPartial(Order order, string barcodeMetadataKey, Currency currency)
        {
            string templateStr = GetLiquidFileAsString("OrderItemsPartial.liquid");
            Template template = Template.Parse(templateStr);

            string chefNote = order.ChefNote;
            string itemsPart = GetItemsPart(order, barcodeMetadataKey, currency);

            const string resSection = "Section";
            const string resItems = "Items";
            const string resOptions = "Options";
            const string resPrice = "Price";
            const string resChefNotes = "Chef Notes";

            const string customerLocationLabel = "Customer Location";

            string customerPickupLocation = GetCustomerPickupLocationMessage(order);

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

        private string GetCustomerPickupLocationMessage(Order order)
        {
            if (!order.DropOffLocationId.HasValue || order.PickupLocationType != Order.PickupLocationTypeEnum.TableService)
                return null;

            string tableServiceCategoryMessage = order.TableServiceCatagory.Value.GetTableServiceCategoryLabel();
            return $"{tableServiceCategoryMessage}: {order.DropOffLocation}";
        }

        private string GetCustomerDetailsPartial(Order order)
        {
            string templateStr = GetLiquidFileAsString("CustomerDetailsPartial.liquid");
            Template template = Template.Parse(templateStr);

            string domain = _appSettings.FlipdishDomainWithScheme;
            string customerName = order.Customer.Name;
            string deliveryInstructions = order.DeliveryLocation?.DeliveryInstructions;
            string deliveryLocationAddressString = order.DeliveryLocation?.PrettyAddressString;

            string phoneNumber = order.Customer.PhoneNumberLocalFormat;
            bool isDelivery = order.DeliveryType == Order.DeliveryTypeEnum.Delivery;

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

        public Dictionary<string, Stream> _imagesWithNames = new Dictionary<string, Stream>();

        private string GetItemsPart(Order order, string barcodeMetadataKey, Currency currency)
        {
            StringBuilder itemsPart = new StringBuilder();

            itemsPart.AppendLine("<tr>");
            itemsPart.AppendLine("<td cellpadding=\"2px\" valign=\"top\" style=\"font-weight: bold;\">Order items</td>");
            itemsPart.AppendLine("<td cellpadding=\"2px\" valign=\"top\"></td>");
            itemsPart.AppendLine("</tr>");
            itemsPart.AppendLine(GetSpaceDivider());
            List<MenuSectionGrouped> sectionsGrouped = OrderHelper.GetMenuSectionGroupedList(order.OrderItems, barcodeMetadataKey);
            var last = sectionsGrouped.Last();
            foreach (var section in sectionsGrouped)
            {
                itemsPart.AppendLine("<tr>");
                itemsPart.Append("<td cellpadding=\"2px\" valign=\"top\" style=\"font-size: 14px;\">").Append(section.Name.ToUpper()).AppendLine("</td>");
                itemsPart.AppendLine("<td cellpadding=\"2px\" valign=\"top\" style=\"font-size: 14px;\"></td>");
                itemsPart.AppendLine("</tr>");
                itemsPart.AppendLine(GetLineDivider());
                itemsPart.AppendLine(GetSpaceDivider());
                foreach (MenuItemsGrouped item in section.MenuItemsGroupedList)
                {
                    itemsPart.AppendLine("<tr>");
                    string countStr = item.Count > 1 ? $"{item.Count} x " : string.Empty;
                    itemsPart.Append("<td cellpadding=\"2px\" valign=\"middle\" style=\"padding-left: 40px;\">").Append(countStr).Append(item.MenuItemUI.Name).AppendLine("</td>");
                    string itemPriceStr = item.MenuItemUI.Price.HasValue ? (item.MenuItemUI.Price.Value * item.Count).ToRawHtmlCurrencyString(currency) : string.Empty;
                    itemsPart.Append("<td cellpadding=\"2px\" valign=\"middle\">").Append(itemPriceStr).AppendLine("</td>");

                    if (!string.IsNullOrEmpty(item.MenuItemUI.Barcode))
                    {
                        Stream barcodeStream;

                        if (_imagesWithNames.ContainsKey(item.MenuItemUI.Barcode + ".png"))
                        {
                            barcodeStream = _imagesWithNames[item.MenuItemUI.Barcode + ".png"];
                        }
                        else
                        {
                            barcodeStream = GetBase64EAN13Barcode(item.MenuItemUI.Barcode, order);
                        }
                        if (barcodeStream != null)
                        {
                            if (!_imagesWithNames.ContainsKey(item.MenuItemUI.Barcode + ".png"))
                                _imagesWithNames.Add(item.MenuItemUI.Barcode + ".png", barcodeStream);

                            itemsPart.Append("<td cellpadding=\"2px\" valign=\"middle\"><img style=\"margin-left: 14px;margin-left: 9px;padding-top: 10px; padding-bottom:10px\" src=\"cid:").Append(item.MenuItemUI.Barcode).AppendLine(".png\"/></td>");
                            if (item.Count > 1)
                            {
                                itemsPart.AppendLine("<td cellpadding=\"2px\" valign=\"middle\" style=\"font-size:40px\">x</td>");
                                itemsPart.Append("<td cellpadding=\"2px\" valign=\"middle\" style=\"font-size:50px\">").Append(item.Count).AppendLine("</td>");
                            }
                        }
                    }

                    itemsPart.AppendLine("</tr>");

                    foreach (MenuOption option in item.MenuItemUI.MenuOptions)
                    {
                        itemsPart.AppendLine("<tr>");
                        itemsPart.Append("<td cellpadding=\"2px\" valign=\"middle\" style=\"padding-left: 40px;padding-top: 10px; padding-bottom:10px\">+ ").Append(option.Name).AppendLine("</td>");
                        itemsPart.Append("<td cellpadding=\"2px\" valign=\"middle\">").Append((option.Price * item.Count).ToRawHtmlCurrencyString(currency)).AppendLine("</td>");

                        if (!string.IsNullOrEmpty(option.Barcode))
                        {
                            Stream barcodeStream;

                            if (_imagesWithNames.ContainsKey(option.Barcode + ".png"))
                            {
                                barcodeStream = _imagesWithNames[option.Barcode + ".png"];
                            }
                            else
                            {
                                barcodeStream = GetBase64EAN13Barcode(option.Barcode, order);
                            }
                            if (barcodeStream != null)
                            {
                                if (!_imagesWithNames.ContainsKey(option.Barcode + ".png"))
                                {
                                    _imagesWithNames.Add(option.Barcode + ".png", barcodeStream);
                                }
                                itemsPart.Append("<td cellpadding=\"2px\" valign=\"middle\"><img style=\"margin-left: 14px;margin-left: 9px;padding-top: 10px; padding-bottom:10px\" src=\"cid:").Append(option.Barcode).AppendLine(".png\"/></td>");
                            }
                        }

                        itemsPart.AppendLine("</tr>");
                    }
                }

                if (!section.Equals(last))
                {
                    itemsPart.AppendLine(GetSpaceDivider());
                }
            }

            return itemsPart.ToString();
        }

        private Stream GetBase64EAN13Barcode(string barcodeNumbers, Order order)
        {
            try
            {
                var barcode = new Barcode(barcodeNumbers, showLabel: true, width: 130, height: 110, labelPosition: LabelPosition.BottomCenter);

                var bytes = barcode.GetByteArray();
                return new MemoryStream(bytes);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"{barcodeNumbers} is not a valid barcode for order #{order.OrderId}");
                return null;
            }
        }

        private string GetLineDivider()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine("<tr>");
            result.AppendLine("<td colspan=\"2\" align =\"center\" valign=\"top\">");
            result.AppendLine("<table width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\" align=\"center\" style=\"height: 1px; background-color: rgb(186, 186, 186);\">");
            result.AppendLine("</table>");
            result.AppendLine("</td>");
            result.AppendLine("</tr>");

            return result.ToString();
        }

        private string GetSpaceDivider()
        {
            StringBuilder result = new StringBuilder();

            result.AppendLine("<tr>");
            result.AppendLine("<td colspan=\"2\" align =\"center\" valign=\"top\">");
            result.AppendLine("<table width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\" align=\"center\" style=\"height: 22px;\">");
            result.AppendLine("</table>");
            result.AppendLine("</td>");
            result.AppendLine("</tr>");

            return result.ToString();
        }

        public void Dispose()
        {
            if (_imagesWithNames == null)
                return;

            foreach (var kvp in _imagesWithNames)
            {
                kvp.Value.Dispose();
            }

            _imagesWithNames = null;
        }
    }
}