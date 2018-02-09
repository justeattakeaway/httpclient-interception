// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace JustEat.HttpClientInterception
{
    public static class HttpClientInterceptorOptionsExtensionsTests
    {
        [Fact]
        public static async Task RegisterGet_For_With_Defaults_Registers_Interception()
        {
            // Arrange
            string requestUri = "https://google.com/";
            string[] expected = new[] { "foo", "bar" };

            var options = new HttpClientInterceptorOptions().RegisterGet(requestUri, expected);

            // Act
            string[] actual = await HttpAssert.GetAsync<string[]>(options, requestUri);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static async Task RegisterGet_For_Plaintext_String_With_Custom_Status_Code_Registers_Interception()
        {
            // Arrange
            string requestUri = "https://google.com/";
            string expected = "foo";

            var options = new HttpClientInterceptorOptions().RegisterGet(requestUri, expected);

            // Act
            string actual = await HttpAssert.GetAsync(options, requestUri);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static async Task RegisterGet_For_Html_With_Custom_Media_Type_Registers_Interception()
        {
            // Arrange
            string requestUri = "https://google.com/";
            string mediaType = "text/html";
            string expected = "<html>foo></html>";

            var options = new HttpClientInterceptorOptions().RegisterGet(requestUri, expected, mediaType: mediaType);

            // Act
            string actual = await HttpAssert.GetAsync(options, requestUri, mediaType: mediaType);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static async Task RegisterGet_If_Content_String_Is_Null_Registers_Interception()
        {
            // Arrange
            string requestUri = "https://google.com/";
            string content = null;
            string expected = string.Empty;

            var options = new HttpClientInterceptorOptions().RegisterGet(requestUri, content);

            // Act
            string actual = await HttpAssert.GetAsync(options, requestUri);

            // Assert
            actual.ShouldBe(expected);
        }

        [Fact]
        public static void RegisterGet_Validates_Parameters_For_Null()
        {
            // Arrange
            HttpClientInterceptorOptions options = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("options", () => options.RegisterGet("https://google.com", new { }));
            Assert.Throws<ArgumentNullException>("options", () => options.RegisterGet("https://google.com", string.Empty));

            // Arrange
            options = new HttpClientInterceptorOptions();

            // Act and Assert
            Assert.Throws<ArgumentNullException>("content", () => options.RegisterGet("https://google.com", null as object));
        }

        [Fact]
        public static void DeregisterGet_Throws_If_Options_Is_Null()
        {
            // Arrange
            HttpClientInterceptorOptions options = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("options", () => options.DeregisterGet("https://google.com"));
        }

        [Fact]
        public static void Register_Throws_If_Options_Is_Null()
        {
            // Arrange
            var method = HttpMethod.Get;
            var uri = new Uri("https://google.com");

            HttpClientInterceptorOptions options = null;
            IEnumerable<KeyValuePair<string, string>> headers = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>(
                "options",
                () => options.Register(method, uri, contentFactory: Array.Empty<byte>, responseHeaders: headers));

            Assert.Throws<ArgumentNullException>(
                "options",
                () => options.Register(method, uri, contentStream: () => Stream.Null, responseHeaders: headers));
        }

        [Fact]
        public static void CreateHttpClient_Throws_If_Options_Is_Null()
        {
            // Arrange
            var baseAddress = new Uri("https://google.com");

            HttpClientInterceptorOptions options = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("options", () => options.CreateHttpClient(baseAddress));
        }

        [Fact]
        public static void Register_For_Collection_Throws_If_Options_Is_Null()
        {
            // Arrange
            var baseAddress = new Uri("https://google.com");

            HttpClientInterceptorOptions options = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("options", () => options.Register(new List<HttpRequestInterceptionBuilder>()));
        }

        [Fact]
        public static void Register_For_Collection_Throws_If_Collection_Is_Null()
        {
            // Arrange
            var baseAddress = new Uri("https://google.com");

            var options = new HttpClientInterceptorOptions();
            IEnumerable<HttpRequestInterceptionBuilder> collection = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("collection", () => options.Register(collection));
        }

        [Fact]
        public static async Task Register_For_Collection_Registers_Interceptions()
        {
            // Arrange
            var builder1 = new HttpRequestInterceptionBuilder()
                .ForGet()
                .ForUrl("https://google.com/")
                .WithContent("foo");

            var builder2 = new HttpRequestInterceptionBuilder()
                .ForGet()
                .ForUrl("https://bing.com/")
                .WithContent("bar");

            var options = new HttpClientInterceptorOptions()
                .Register(new[] { builder1, builder2 });

            string actual1;
            string actual2;

            // Act
            using (var httpClient = options.CreateHttpClient())
            {
                actual1 = await httpClient.GetStringAsync("https://google.com/");
                actual2 = await httpClient.GetStringAsync("https://bing.com/");
            }

            // Assert
            actual1.ShouldBe("foo");
            actual2.ShouldBe("bar");
        }

        [Fact]
        public static void Register_For_Array_Throws_If_Options_Is_Null()
        {
            // Arrange
            var baseAddress = new Uri("https://google.com");

            HttpClientInterceptorOptions options = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("options", () => options.Register(Array.Empty<HttpRequestInterceptionBuilder>()));
        }

        [Fact]
        public static void Register_For_Array_Throws_If_Collection_Is_Null()
        {
            // Arrange
            var baseAddress = new Uri("https://google.com");

            var options = new HttpClientInterceptorOptions();
            HttpRequestInterceptionBuilder[] collection = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>("collection", () => options.Register(collection));
        }

        [Fact]
        public static async Task Register_For_Array_Registers_Interceptions()
        {
            // Arrange
            var builder1 = new HttpRequestInterceptionBuilder()
                .ForGet()
                .ForUrl("https://google.com/")
                .WithContent("foo");

            var builder2 = new HttpRequestInterceptionBuilder()
                .ForGet()
                .ForUrl("https://bing.com/")
                .WithContent("bar");

            var options = new HttpClientInterceptorOptions()
                .Register(builder1, builder2);

            string actual1;
            string actual2;

            // Act
            using (var httpClient = options.CreateHttpClient())
            {
                actual1 = await httpClient.GetStringAsync("https://google.com/");
                actual2 = await httpClient.GetStringAsync("https://bing.com/");
            }

            // Assert
            actual1.ShouldBe("foo");
            actual2.ShouldBe("bar");
        }

        private sealed class CustomObject
        {
            internal enum Color
            {
                Red = 1,
                Blue = 2,
            }

            public Color FavoriteColor { get; set; }

            public int Number { get; set; }

            public string Text { get; set; }
        }
    }
}
