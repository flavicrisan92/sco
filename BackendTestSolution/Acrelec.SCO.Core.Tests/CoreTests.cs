using Acrelec.SCO.Core.Helpers;
using Acrelec.SCO.Core.Interfaces;
using Acrelec.SCO.Core.Managers;
using Acrelec.SCO.Core.Model.RestExchangedMessages;
using Acrelec.SCO.Core.Providers;
using Acrelec.SCO.DataStructures;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Acrelec.SCO.Core.Tests
{
    [TestClass]
    public class CoreTests
    {
        [TestMethod]
        public void ItemsProviderTest()
        {
            var clientFactoryMock = new Mock<IHttpClientFactory>();
            Mock<IConfiguration> mockConfiguration = new();
            mockConfiguration.SetupGet(x => x[It.Is<string>(s => s == "itemsProviderDataPath")]).Returns("Data/ContentItems.json");

            IItemsProvider itemsProvider = new ItemsProvider(clientFactoryMock.Object, mockConfiguration.Object);
            Assert.AreEqual(4, itemsProvider.AllPOSItems.Count, "Different number of items are expected");
        }

        [TestMethod]
        public void ItemsProviderAvailablePOSItemsTest()
        {
            var clientFactoryMock = new Mock<IHttpClientFactory>();
            Mock<IConfiguration> mockConfiguration = new();
            mockConfiguration.SetupGet(x => x[It.Is<string>(s => s == "itemsProviderDataPath")]).Returns("Data/ContentItems.json");

            IItemsProvider itemsProvider = new ItemsProvider(clientFactoryMock.Object, mockConfiguration.Object);
            Assert.AreEqual(3, itemsProvider.AvailablePOSItems.Count, "Different number of available items are expected");

            //todo - write an assert to check only for items that are available (IsAvailable=True)
        }

        [TestMethod]
        public void OrderedItemsByCodeTest()
        {
            var clientFactoryMock = new Mock<IHttpClientFactory>();
            Mock<IConfiguration> mockConfiguration = new();
            mockConfiguration.SetupGet(x => x[It.Is<string>(s => s == "itemsProviderDataPath")]).Returns("Data/ContentItems.json");

            IItemsProvider itemsProvider = new ItemsProvider(clientFactoryMock.Object, mockConfiguration.Object);
            string[] expectedCodesOrder = new[] { "200", "100", "101", "50" };

            //todo - write the code to order the items ascendent by UnitPrice
            var orderedCodes = itemsProvider.AllPOSItems.SortByPriceAscending().Select(q => q.ItemCode).ToArray();

            //compare the ordered itemCodes to see it matches the expected order
            CollectionAssert.AreEqual(expectedCodesOrder, orderedCodes);
        }

        [TestMethod]
        public async Task CheckServerAvailabilityAsync_WhenServerAvailable_ShouldReturnTrue()
        {
            var clientHandlerMock = new Mock<DelegatingHandler>();
            var checkAvailabilityResponse = new CheckAvailabilityResponse()
            {
                CanInjectOrders = true,
            };
            string json = JsonConvert.SerializeObject(checkAvailabilityResponse, Formatting.Indented);
            var respone = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };

            clientHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(respone)
                .Verifiable();
            clientHandlerMock.As<IDisposable>().Setup(s => s.Dispose());

            var httpClient = new HttpClient(clientHandlerMock.Object)
            {
                BaseAddress = new Uri("https://localhost:44395")
            };

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            Mock<IConfiguration> mockConfiguration = new();
            mockConfiguration.SetupGet(x => x[It.Is<string>(s => s == "itemsProviderDataPath")]).Returns("Data/ContentItems.json");

            clientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            IItemsProvider itemsProvider = new ItemsProvider(clientFactoryMock.Object, mockConfiguration.Object);
            var result = await itemsProvider.CheckServerAvailabilityAsync();
            Assert.IsTrue(result);
        }


        [TestMethod]
        public async Task CheckServerAvailabilityAsync_WhenServerNotAvailable_ShouldReturFalse()
        {
            var clientHandlerMock = new Mock<DelegatingHandler>();
            var checkAvailabilityResponse = new CheckAvailabilityResponse()
            {
                CanInjectOrders = false,
            };
            string json = JsonConvert.SerializeObject(checkAvailabilityResponse, Formatting.Indented);
            var respone = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };

            clientHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(respone)
                .Verifiable();
            clientHandlerMock.As<IDisposable>().Setup(s => s.Dispose());

            var httpClient = new HttpClient(clientHandlerMock.Object)
            {
                BaseAddress = new Uri("https://localhost:44395")
            };

            var clientFactoryMock = new Mock<IHttpClientFactory>();

            clientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            Mock<IConfiguration> mockConfiguration = new();
            mockConfiguration.SetupGet(x => x[It.Is<string>(s => s == "itemsProviderDataPath")]).Returns("Data/ContentItems.json");

            IItemsProvider itemsProvider = new ItemsProvider(clientFactoryMock.Object, mockConfiguration.Object);
            var result = await itemsProvider.CheckServerAvailabilityAsync();
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task SendOrderAsync_WhenValidOrder_ShouldReturOrderNumber()
        {
            var ordereNumber = "10";
            var clientHandlerMock = new Mock<DelegatingHandler>();
            var injectOrderResponse = new InjectOrderResponse()
            {
                OrderNumber = ordereNumber
            };
            string json = JsonConvert.SerializeObject(injectOrderResponse, Formatting.Indented);
            var respone = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };

            clientHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(respone)
                .Verifiable();
            clientHandlerMock.As<IDisposable>().Setup(s => s.Dispose());

            var httpClient = new HttpClient(clientHandlerMock.Object)
            {
                BaseAddress = new Uri("https://localhost:44395")
            };

            var clientFactoryMock = new Mock<IHttpClientFactory>();

            clientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            Mock<IConfiguration> mockConfiguration = new();
            mockConfiguration.SetupGet(x => x[It.Is<string>(s => s == "itemsProviderDataPath")]).Returns("Data/ContentItems.json");

            IItemsProvider itemsProvider = new ItemsProvider(clientFactoryMock.Object, mockConfiguration.Object);

            var response = await itemsProvider.SendOrderAsync(new Order() { OrderItems = new System.Collections.Generic.List<OrderedItem> { new OrderedItem() { ItemCode = "100", Qty = 1 } } }, new Customer());
            Assert.AreEqual(ordereNumber, response);
        }

        [TestMethod]
        public async Task OrderManager()
        {
            var itemsProviderMock = new Mock<IItemsProvider>();
            itemsProviderMock.Setup(q => q.SendOrderAsync(It.IsAny<Order>(), It.IsAny<Customer>())).ReturnsAsync("10");

            IOrderManager orderManager = new OrderManager(itemsProviderMock.Object);

            var order = new Order()
            {
                OrderItems = new List<OrderedItem> {
                new OrderedItem() {
                    ItemCode = "100",
                    Qty =1
                    }
                }

            };

            var customer = new Customer()
            {
                Address = "Bucharest",
                Firstname = "Johm"
            };


            var result = await orderManager.InjectOrderAsync(order, customer);

            Assert.AreEqual("10", result);
        }
    }
}
