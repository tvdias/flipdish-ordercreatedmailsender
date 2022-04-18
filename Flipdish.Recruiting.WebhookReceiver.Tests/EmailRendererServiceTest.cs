using System.Collections.Generic;
using Flipdish.Recruiting.WebhookReceiver.Models;
using Flipdish.Recruiting.WebhookReceiver.Services;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Flipdish.Recruiting.WebhookReceiverTests
{
    public class EmailRendererServiceTest : BaseTest
    {
        private readonly string orderItemsStr = "[{\"OrderItemOptions\":[{\"Metadata\":{},\"MenuItemOptionPublicId\":\"d1e853f3-2081-47cc-b79d-9dafac93ddec\",\"MenuItemOptionId\":25077156,\"IsMasterOptionSetItem\":false,\"Name\":\"Cilantro Lime Rice\",\"Price\":10.0,\"MenuItemOptionDisplayOrder\":0,\"MenuItemOptionSetDisplayOrder\":0},{\"Metadata\":{},\"MenuItemOptionPublicId\":\"92ba0555-09e2-4980-b8e3-5a772d8d7193\",\"MenuItemOptionId\":25077152,\"IsMasterOptionSetItem\":false,\"Name\":\"No Beans\",\"Price\":0.0,\"MenuItemOptionDisplayOrder\":0,\"MenuItemOptionSetDisplayOrder\":0},{\"Metadata\":{},\"MenuItemOptionPublicId\":\"b8dcb18a-ae5d-4c74-99c8-250b7308b627\",\"MenuItemOptionId\":25077148,\"IsMasterOptionSetItem\":false,\"Name\":\"Pico De Gallo Salsa\",\"Price\":0.0,\"MenuItemOptionDisplayOrder\":0,\"MenuItemOptionSetDisplayOrder\":0},{\"Metadata\":{},\"MenuItemOptionPublicId\":\"c0094a42-fba1-4b1a-ae38-f7009942bf14\",\"MenuItemOptionId\":25077144,\"IsMasterOptionSetItem\":false,\"Name\":\"No Sour Cream\",\"Price\":0.0,\"MenuItemOptionDisplayOrder\":0,\"MenuItemOptionSetDisplayOrder\":0},{\"Metadata\":{},\"MenuItemOptionPublicId\":\"01ecbacb-8522-4e67-9d40-2f41aa387252\",\"MenuItemOptionId\":25077142,\"IsMasterOptionSetItem\":false,\"Name\":\"No Jalapeno\",\"Price\":0.0,\"MenuItemOptionDisplayOrder\":0,\"MenuItemOptionSetDisplayOrder\":0},{\"Metadata\":{},\"MenuItemOptionPublicId\":\"6aeabfda-8d61-4a7e-9455-b708a5179d94\",\"MenuItemOptionId\":25077140,\"IsMasterOptionSetItem\":false,\"Name\":\"No Cheese\",\"Price\":0.0,\"MenuItemOptionDisplayOrder\":0,\"MenuItemOptionSetDisplayOrder\":0},{\"Metadata\":{},\"MenuItemOptionPublicId\":\"ad1cc890-820f-4016-8509-7fb883130c23\",\"MenuItemOptionId\":25077137,\"IsMasterOptionSetItem\":false,\"Name\":\"No Guacamole\",\"Price\":0.0,\"MenuItemOptionDisplayOrder\":0,\"MenuItemOptionSetDisplayOrder\":0},{\"Metadata\":{\"eancode\":\"978020137962\"},\"MenuItemOptionPublicId\":\"ebfe0d4c-65f7-41fa-91cc-0d1b25393f7e\",\"MenuItemOptionId\":25077135,\"IsMasterOptionSetItem\":false,\"Name\":\"No Lettuce\",\"Price\":0.0,\"MenuItemOptionDisplayOrder\":0,\"MenuItemOptionSetDisplayOrder\":0}],\"Metadata\":{\"eancode\":\"978020137962\"},\"MenuItemPublicId\":\"f16caf40-c744-4534-9ebd-634c4ec50832\",\"MenuSectionName\":\"TACOS\",\"MenuSectionDisplayOrder\":0,\"Name\":\"Chilli Con Carne Taco\",\"Description\":\"Spicy ground lean beef cooked in chilli de arbol sauce & beans.\n\nContains Soybeans\n\",\"Price\":8.5,\"PriceIncludingOptionSetItems\":18.5,\"MenuItemId\":2168142,\"MenuItemDisplayOrder\":0,\"IsAvailable\":true}]";
        private readonly string expectedResultStr = "[{\"Name\":\"TACOS\",\"DisplayOrder\":0,\"MenuItemsGroupedList\":[{\"MenuItemUI\":{\"Name\":\"Chilli Con Carne Taco\",\"Price\":8.5,\"MenuOptions\":[{\"Name\":\"Cilantro Lime Rice\",\"Price\":10.0,\"OptionSetDisplayOrder\":0,\"DisplayOrder\":0,\"Barcode\":null},{\"Name\":\"No Beans\",\"Price\":0.0,\"OptionSetDisplayOrder\":0,\"DisplayOrder\":1,\"Barcode\":null},{\"Name\":\"Pico De Gallo Salsa\",\"Price\":0.0,\"OptionSetDisplayOrder\":0,\"DisplayOrder\":2,\"Barcode\":null},{\"Name\":\"No Sour Cream\",\"Price\":0.0,\"OptionSetDisplayOrder\":0,\"DisplayOrder\":3,\"Barcode\":null},{\"Name\":\"No Jalapeno\",\"Price\":0.0,\"OptionSetDisplayOrder\":0,\"DisplayOrder\":4,\"Barcode\":null},{\"Name\":\"No Cheese\",\"Price\":0.0,\"OptionSetDisplayOrder\":0,\"DisplayOrder\":5,\"Barcode\":null},{\"Name\":\"No Guacamole\",\"Price\":0.0,\"OptionSetDisplayOrder\":0,\"DisplayOrder\":6,\"Barcode\":null},{\"Name\":\"No Lettuce\",\"Price\":0.0,\"OptionSetDisplayOrder\":0,\"DisplayOrder\":7,\"Barcode\":\"978020137962\"}],\"HashCode\":-1653612664,\"Barcode\":\"978020137962\"},\"Count\":1,\"DisplayOrder\":0}]}]";

        [Fact]
        public void GetMenuSectionGroupedList_AllRight_Success()
        {
            // Arrange
            const string barcodeMetadataKey = "eancode";
            var orderItems = JsonConvert.DeserializeObject<List<OrderItem>>(orderItemsStr);
            var expectedResult = JsonConvert.DeserializeObject<List<MenuSectionGrouped>>(expectedResultStr);

            // Act
            var result = EmailRendererService.GetMenuSectionGroupedList(orderItems, barcodeMetadataKey);

            // Assert
            result[0].MenuItemsGroupedList.Should().BeEquivalentTo(
                expectedResult[0].MenuItemsGroupedList,
                options => options
                    .Excluding(x => x.MenuItemUI.Barcode)
                    .Excluding(x => x.MenuItemUI.HashCode));
        }
    }
}