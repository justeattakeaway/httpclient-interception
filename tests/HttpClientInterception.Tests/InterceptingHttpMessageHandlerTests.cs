// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Shouldly;
using Xunit;

namespace JustEat.HttpClientInterception
{
    public static class InterceptingHttpMessageHandlerTests
    {
        [Fact]
        public static void Constructor_Throws_If_Options_Is_Null()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>("options", () => new InterceptingHttpMessageHandler(null));
            Assert.Throws<ArgumentNullException>("options", () => new InterceptingHttpMessageHandler(null, Mock.Of<HttpMessageHandler>()));
        }

        [Fact]
        public static async Task SendAsync_Throws_If_Registration_Missing_Get()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions()
            {
                ThrowOnMissingRegistration = true,
            };

            using (var target = options.CreateHttpClient())
            {
                // Act
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => target.GetAsync("https://google.com/"));

                // Assert
                exception.Message.ShouldBe("No HTTP response is configured for GET https://google.com/.");
            }
        }

        [Fact]
        public static async Task SendAsync_Throws_If_Registration_Missing_Post()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions()
            {
                ThrowOnMissingRegistration = true,
            };

            var mock = new Mock<HttpMessageHandler>();

            using (var handler = options.CreateHttpMessageHandler())
            {
                using (var target = new HttpClient(handler))
                {
                    using (var content = new StringContent(string.Empty))
                    {
                        // Act
                        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => target.PostAsync("https://google.com/", content));

                        // Assert
                        exception.Message.ShouldBe("No HTTP response is configured for POST https://google.com/.");
                    }
                }
            }
        }

        [Fact]
        public static async Task SendAsync_Calls_Inner_Handler_If_Registration_Missing()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions()
                .Register(HttpMethod.Get, new Uri("https://google.com/foo"), Array.Empty<byte>)
                .Register(HttpMethod.Options, new Uri("http://google.com/foo"), Array.Empty<byte>)
                .Register(HttpMethod.Options, new Uri("https://google.com/FOO"), Array.Empty<byte>);

            var mock = new Mock<HttpMessageHandler>();

            using (var expected = new HttpResponseMessage(System.Net.HttpStatusCode.OK))
            {
                using (var request = new HttpRequestMessage(HttpMethod.Options, "https://google.com/foo"))
                {
                    mock.Protected()
                        .Setup<Task<HttpResponseMessage>>("SendAsync", request, ItExpr.IsAny<CancellationToken>())
                        .ReturnsAsync(expected);

                    using (var httpClient = options.CreateHttpClient(mock.Object))
                    {
                        // Act
                        var actual = await httpClient.SendAsync(request, CancellationToken.None);

                        // Asert
                        actual.ShouldBe(expected);
                    }
                }
            }
        }
    }
}
