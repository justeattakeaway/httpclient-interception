// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Shouldly;
using Xunit;

namespace JustEat.HttpClientInterception.Bundles
{
    public static class BundleExtensionsTests
    {
        public static IEnumerable<object[]> BundleFiles
        {
            get
            {
                var bundles = Directory.GetFiles("Bundles", "*.json");

                foreach (string path in bundles)
                {
                    if (path.Contains("invalid-", StringComparison.OrdinalIgnoreCase) ||
                        path.Contains("no", StringComparison.OrdinalIgnoreCase) ||
                        path.Contains("null-", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    yield return new object[] { path };
                }
            }
        }

        [Fact]
        public static async Task Can_Intercept_Http_Requests_From_Bundle_File()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            var headers = new Dictionary<string, string>()
            {
                { "accept", "application/vnd.github.v3+json" },
                { "authorization", "token my-token" },
                { "user-agent", "My-App/1.0.0" },
            };

            // Act
            options.RegisterBundle(Path.Join("Bundles", "http-request-bundle.json"));

            // Assert
            await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/", mediaType: "text/html");
            await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/order-history");
            await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeat", headers: headers, mediaType: "application/json");
        }

        [Fact]
        public static async Task Can_Intercept_Http_Requests_From_Bundle_File_With_String()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            // Act
            options.RegisterBundle(Path.Join("Bundles", "content-as-html-string.json"));

            // Assert
            string content = await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/");
            content.ShouldBe("<html><head><title>Just Eat</title></head></html>");
        }

        [Fact]
        public static async Task Can_Intercept_Http_Requests_From_Bundle_File_With_String_Encoded_As_Base64()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            // Act
            options.RegisterBundle(Path.Join("Bundles", "content-as-html-base64.json"));

            // Assert
            string content = await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/");
            content.ShouldBe("<html><head><title>Just Eat</title></head></html>");
        }

        [Fact]
        public static async Task Can_Intercept_Http_Requests_From_Bundle_File_With_Json_Array()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            // Act
            options.RegisterBundle(Path.Join("Bundles", "content-as-json-array.json"));

            // Assert
            string content = await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeat/repos");

            content
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .ShouldBe(@"[""httpclient-interception""]");
        }

        [Fact]
        public static async Task Can_Intercept_Http_Requests_From_Bundle_File_With_Json_Object()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            // Act
            options.RegisterBundle(Path.Join("Bundles", "content-as-json-object.json"));

            // Assert
            string content = await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeat");

            content
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .ShouldBe(@"{""id"":1516790,""login"":""justeat"",""url"":""https://api.github.com/orgs/justeat""}");
        }

        [Fact]
        public static async Task Can_Intercept_Http_Requests_From_Bundle_File_With_Null_Json()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            // Act
            options.RegisterBundle(Path.Join("Bundles", "content-as-null-json.json"));

            // Assert
            string content = await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeat");

            content.ShouldBe(string.Empty);
        }

        [Fact]
        public static async Task Can_Intercept_Http_Requests_From_Bundle_File_With_Non_Default_Status_Codes()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            // Act
            options.RegisterBundle(Path.Join("Bundles", "http-status-codes.json"));

            // Assert
            await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/1", HttpStatusCode.NotFound);
            await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/2", HttpStatusCode.NotFound);
        }

        [Fact]
        public static async Task Can_Intercept_Http_Requests_From_Bundle_File_With_Non_Default_Method_And_Version()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            // Act
            options.RegisterBundle(Path.Join("Bundles", "http-methods-versions.json"));

            // Assert
            await HttpAssert.PostAsync(options, "https://public.je-apis.com/consumer", new { }, HttpStatusCode.NoContent);
        }

        [Fact]
        public static async Task Can_Intercept_Http_Requests_From_Bundle_File_With_Templated_String()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            var headers = new Dictionary<string, string>()
            {
                { "user-agent", "My-App/1.0.0" },
            };

            // Act
            options.RegisterBundle(Path.Join("Bundles", "templated-bundle-string.json"));

            // Assert
            string content = await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/", headers: headers);
            content.ShouldBe("<html><head><title>Just Eat</title></head></html>");
        }

        [Fact]
        public static async Task Can_Intercept_Http_Requests_From_Bundle_File_With_Templated_String_With_User_Template_Values()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            var headers = new Dictionary<string, string>()
            {
                { "user-agent", "My-Other-App/1.0.0" },
            };

            var templateValues = new Dictionary<string, string>()
            {
                { "ApplicationName", "My-Other-App" },
            };

            // Act
            options.RegisterBundle(Path.Join("Bundles", "templated-bundle-string.json"), templateValues);

            // Assert
            string content = await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/", headers: headers);
            content.ShouldBe("<html><head><title>Just Eat</title></head></html>");
        }

        [Fact]
        public static async Task Can_Intercept_Http_Requests_From_Bundle_File_With_Templated_Base64()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            // Act
            options.RegisterBundle(Path.Join("Bundles", "templated-bundle-base64.json"));

            // Assert
            string content = await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/");
            content.ShouldBe("<html><head><title>Just Eat</title></head></html>");
        }

        [Fact]
        public static async Task Can_Intercept_Http_Requests_From_Bundle_File_With_Templated_Json()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            // Act
            options.RegisterBundle(Path.Join("Bundles", "templated-bundle-json.json"));

            // Assert
            string content = await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeat");
            content
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .ShouldBe(@"{""id"":1516790,""login"":""justeat"",""url"":""https://api.github.com/orgs/justeat""}");
        }

        [Fact]
        public static void RegisterBundle_Validates_Parameters()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();
            string path = "foo.bar";

            // Act and Assert
            Assert.Throws<ArgumentNullException>("options", () => (null as HttpClientInterceptorOptions).RegisterBundle(path));
            Assert.Throws<ArgumentNullException>("path", () => options.RegisterBundle(null));
            Assert.Throws<ArgumentNullException>("templateValues", () => options.RegisterBundle(path, null));
        }

        [Fact]
        public static void RegisterBundle_Throws_If_Bundle_Version_Is_Not_Supported()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();
            string path = Path.Join("Bundles", "invalid-bundle-version.json");

            // Act and Assert
            Assert.Throws<NotSupportedException>(() => options.RegisterBundle(path));
        }

        [Fact]
        public static void RegisterBundle_Does_Not_Throw_If_Bundle_Items_Is_Null()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();
            string path = Path.Join("Bundles", "null-items.json");

            // Act and Assert
            options.RegisterBundle(path);
        }

        [Fact]
        public static void RegisterBundle_Throws_If_Invalid_Content_Format_Configured()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();
            string path = Path.Join("Bundles", "invalid-content-format.json");

            // Act and Assert
            var exception = Assert.Throws<NotSupportedException>(() => options.RegisterBundle(path));
            exception.Message.ShouldBe("Content format 'foo' for bundle item with Id 'invalid-content-format' is not supported.");
        }

        [Fact]
        public static void RegisterBundle_Throws_If_Invalid_Status_Code_Configured()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();
            string path = Path.Join("Bundles", "invalid-status-code.json");

            // Act and Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.RegisterBundle(path));
            exception.Message.ShouldBe("Bundle item with Id 'invalid-status' has an invalid HTTP status code 'foo' configured.");
        }

        [Fact]
        public static void RegisterBundle_Throws_If_Invalid_Uri_Configured()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();
            string path = Path.Join("Bundles", "invalid-uri.json");

            // Act and Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.RegisterBundle(path));
            exception.Message.ShouldBe("Bundle item with Id 'invalid-uri' has an invalid absolute URI '::invalid' configured.");
        }

        [Fact]
        public static void RegisterBundle_Throws_If_Invalid_Version_Configured()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();
            string path = Path.Join("Bundles", "invalid-version.json");

            // Act and Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.RegisterBundle(path));
            exception.Message.ShouldBe("Bundle item with Id 'invalid-version' has an invalid version 'foo' configured.");
        }

        [Fact]
        public static void RegisterBundle_Throws_If_No_Uri_Configured()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions();
            string path = Path.Join("Bundles", "no-uri.json");

            // Act and Assert
            var exception = Assert.Throws<InvalidOperationException>(() => options.RegisterBundle(path));
            exception.Message.ShouldBe("Bundle item with Id 'no-uri' has no URI configured.");
        }

        [Fact]
        public static async Task Can_Intercept_Http_Requests_From_Bundle_File_If_No_Content()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            // Act
            options.RegisterBundle(Path.Join("Bundles", "no-content.json"));

            // Assert
            string content = await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/");
            content.ShouldBe(string.Empty);
        }

        [Fact]
        public static async Task Can_Intercept_Http_Requests_With_Different_Path_If_The_Uri_Path_Is_Ignored()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            // Act
            options.RegisterBundle(Path.Join("Bundles", "ignoring-path.json"));

            // Assert
            string content = await HttpAssert.GetAsync(options, "https://api.github.com/orgs/some-other-org");

            content
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .ShouldBe(@"{""id"":1516790,""login"":""justeat"",""url"":""https://api.github.com/orgs/justeat""}");
        }

        [Fact]
        public static async Task Can_Intercept_Http_Requests_With_Different_Path_If_The_Uri_Query_String()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            // Act
            options.RegisterBundle(Path.Join("Bundles", "ignoring-query.json"));

            // Assert
            string content = await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeat?foo=bar");

            content
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("\n", string.Empty, StringComparison.Ordinal)
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .ShouldBe(@"{""id"":1516790,""login"":""justeat"",""url"":""https://api.github.com/orgs/justeat""}");
        }

        [Fact]
        public static async Task Can_Skip_Bundle_Items()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            // Act
            options.RegisterBundle(Path.Join("Bundles", "skipped-item-bundle.json"));

            // Assert
            await Assert.ThrowsAsync<HttpRequestNotInterceptedException>(
                () => HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/"));
        }

        [Theory]
        [MemberData(nameof(BundleFiles))]
        public static async Task Bundle_Schema_Is_Valid_From_Test(string bundlePath)
        {
            // Arrange
            string schemaPath = Path.Join(".", "http-request-bundle-schema.json");

            var json = JToken.Parse(await File.ReadAllTextAsync(bundlePath));

            var schema = JSchema.Parse(
                await File.ReadAllTextAsync(schemaPath),
                new JSchemaReaderSettings() { ValidateVersion = true });

            // Act
            bool actual = json.IsValid(schema, out IList<string> errors);

            // Assert
            actual.ShouldBeTrue();
            errors.Count.ShouldBe(0);
        }

        [Fact]
        public static void Can_Register_Bundle_With_Null_Header_Values_In_Bundle()
        {
            // Arrange
            var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();
            var headers = new Dictionary<string, string>();

            // Act
            options.RegisterBundle(Path.Join("Bundles", "templated-bundle-null-headers.json"), headers);
        }
    }
}
