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
    public class EmailRendererService
    {
        private readonly ILogger _log;
        private readonly AppSettings _appSettings;
        private readonly MapService _mapService;

        public EmailRendererService(ILogger<EmailRendererService> log, IOptions<AppSettings> appSettings, MapService mapService)
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

            var templateStr = GetLiquidFileAsString("RestaurantOrderDetail.liquid");
            var template = Template.Parse(templateStr);

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
                    var userLocation =
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

            var airDistanceStr = airDistance.HasValue ? airDistance.Value.ToString() : "?";
            var currentYear = DateTime.UtcNow.Year.ToString();

            var orderMsg = CreateOrderMessage(order);
            const string openingTag1 = "<span style=\"color: #222; background: #ffc; font-weight: bold; \">";
            const string closingTag1 = "</span>";
            orderMsg = Regex.Replace(orderMsg, "[ ]", "&nbsp;");
            var resNew_DeliveryType_Order = string.Format(orderMsg, openingTag1, closingTag1);

            const string resPAID = "PAID";
            const string resUNPAID = "UNPAID";
            var resDistance = string.Format("{0} km from restaurant", airDistanceStr);

            const string openingTag2 = "<span style=\"font-weight: bold; font-size: inherit; line-height: 24px;color: rgb(208, 93, 104); \">";
            const string closingTag2 = "</span>";

            // TODO: Remove comment below?
            const string taxAmount = null;// (physicalRestaurant?.Menu?.DisplayTax ?? false) ? order.TotalTax.ToRawHtmlCurrencyString(order.Currency) : null;

            var resCall_the_Flipdish_ = string.Format("Call the Flipdish Hotline at {0}", openingTag2 + supportNumber + closingTag2);

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
            var templateStr = GetLiquidFileAsString("PreorderPartial.liquid");
            var template = Template.Parse(templateStr);

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

        private string GetLiquidFileAsString(string fileName)
        {
            var templateFilePath = Path.Combine("./LiquidTemplates", fileName);
            return new StreamReader(templateFilePath).ReadToEnd();
        }

        private string GetOrderStatusPartial(Order order, string appNameId)
        {
            var orderId = order.OrderId.Value;
            var webLink = string.Format(_appSettings.EmailServiceOrderUrl, appNameId, orderId);

            const string resOrder = "Order";
            const string resView_Order = "View Order";

            var templateStr = GetLiquidFileAsString("OrderStatusPartial.liquid");
            var template = Template.Parse(templateStr);
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
            var templateStr = GetLiquidFileAsString("OrderItemsPartial.liquid");
            var template = Template.Parse(templateStr);

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

        private string GetCustomerPickupLocationMessage(Order order)
        {
            if (!order.DropOffLocationId.HasValue || order.PickupLocationType != Order.PickupLocationTypeEnum.TableService)
                return null;

            var tableServiceCategoryMessage = order.TableServiceCatagory.Value.GetTableServiceCategoryLabel();
            return $"{tableServiceCategoryMessage}: {order.DropOffLocation}";
        }

        private string GetCustomerDetailsPartial(Order order)
        {
            var templateStr = GetLiquidFileAsString("CustomerDetailsPartial.liquid");
            var template = Template.Parse(templateStr);

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

            itemsPart.AppendLine("<tr>");
            itemsPart.AppendLine("<td cellpadding=\"2px\" valign=\"top\" style=\"font-weight: bold;\">Order items</td>");
            itemsPart.AppendLine("<td cellpadding=\"2px\" valign=\"top\"></td>");
            itemsPart.AppendLine("</tr>");
            itemsPart.AppendLine(GetSpaceDivider());
            var sectionsGrouped = OrderHelper.GetMenuSectionGroupedList(order.OrderItems, barcodeMetadataKey);
            var last = sectionsGrouped.Last();
            foreach (var section in sectionsGrouped)
            {
                itemsPart.AppendLine("<tr>");
                itemsPart.Append("<td cellpadding=\"2px\" valign=\"top\" style=\"font-size: 14px;\">").Append(section.Name.ToUpper()).AppendLine("</td>");
                itemsPart.AppendLine("<td cellpadding=\"2px\" valign=\"top\" style=\"font-size: 14px;\"></td>");
                itemsPart.AppendLine("</tr>");
                itemsPart.AppendLine(GetLineDivider());
                itemsPart.AppendLine(GetSpaceDivider());
                foreach (var item in section.MenuItemsGroupedList)
                {
                    itemsPart.AppendLine("<tr>");
                    var countStr = item.Count > 1 ? $"{item.Count} x " : string.Empty;
                    itemsPart.Append("<td cellpadding=\"2px\" valign=\"middle\" style=\"padding-left: 40px;\">").Append(countStr).Append(item.MenuItemUI.Name).AppendLine("</td>");
                    var itemPriceStr = item.MenuItemUI.Price.HasValue ? (item.MenuItemUI.Price.Value * item.Count).ToRawHtmlCurrencyString(currency) : string.Empty;
                    itemsPart.Append("<td cellpadding=\"2px\" valign=\"middle\">").Append(itemPriceStr).AppendLine("</td>");

                    if (!string.IsNullOrEmpty(item.MenuItemUI.Barcode))
                    {
                        Stream barcodeStream;

                        if (imagesWithNames.ContainsKey(item.MenuItemUI.Barcode + ".png"))
                        {
                            barcodeStream = imagesWithNames[item.MenuItemUI.Barcode + ".png"];
                        }
                        else
                        {
                            barcodeStream = GetBase64EAN13Barcode(item.MenuItemUI.Barcode, order);
                        }
                        if (barcodeStream != null)
                        {
                            if (!imagesWithNames.ContainsKey(item.MenuItemUI.Barcode + ".png"))
                                imagesWithNames.Add(item.MenuItemUI.Barcode + ".png", barcodeStream);

                            itemsPart.Append("<td cellpadding=\"2px\" valign=\"middle\"><img style=\"margin-left: 14px;margin-left: 9px;padding-top: 10px; padding-bottom:10px\" src=\"cid:").Append(item.MenuItemUI.Barcode).AppendLine(".png\"/></td>");
                            if (item.Count > 1)
                            {
                                itemsPart.AppendLine("<td cellpadding=\"2px\" valign=\"middle\" style=\"font-size:40px\">x</td>");
                                itemsPart.Append("<td cellpadding=\"2px\" valign=\"middle\" style=\"font-size:50px\">").Append(item.Count).AppendLine("</td>");
                            }
                        }
                    }

                    itemsPart.AppendLine("</tr>");

                    foreach (var option in item.MenuItemUI.MenuOptions)
                    {
                        itemsPart.AppendLine("<tr>");
                        itemsPart.Append("<td cellpadding=\"2px\" valign=\"middle\" style=\"padding-left: 40px;padding-top: 10px; padding-bottom:10px\">+ ").Append(option.Name).AppendLine("</td>");
                        itemsPart.Append("<td cellpadding=\"2px\" valign=\"middle\">").Append((option.Price * item.Count).ToRawHtmlCurrencyString(currency)).AppendLine("</td>");

                        if (!string.IsNullOrEmpty(option.Barcode))
                        {
                            Stream barcodeStream;

                            if (imagesWithNames.ContainsKey(option.Barcode + ".png"))
                            {
                                barcodeStream = imagesWithNames[option.Barcode + ".png"];
                            }
                            else
                            {
                                barcodeStream = GetBase64EAN13Barcode(option.Barcode, order);
                            }
                            if (barcodeStream != null)
                            {
                                if (!imagesWithNames.ContainsKey(option.Barcode + ".png"))
                                {
                                    imagesWithNames.Add(option.Barcode + ".png", barcodeStream);
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
            var result = new StringBuilder();

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
            var result = new StringBuilder();

            result.AppendLine("<tr>");
            result.AppendLine("<td colspan=\"2\" align =\"center\" valign=\"top\">");
            result.AppendLine("<table width=\"100%\" border=\"0\" cellspacing=\"0\" cellpadding=\"0\" align=\"center\" style=\"height: 22px;\">");
            result.AppendLine("</table>");
            result.AppendLine("</td>");
            result.AppendLine("</tr>");

            return result.ToString();
        }
    }
}