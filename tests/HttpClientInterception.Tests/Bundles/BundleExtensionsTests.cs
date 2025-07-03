// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace JustEat.HttpClientInterception.Bundles;

#pragma warning disable CA1849

public static class BundleExtensionsTests
{
    public static TheoryData<string> BundleFiles()
    {
        var bundles = Directory.GetFiles("Bundles", "*.json");
        var filePaths = new TheoryData<string>();

        foreach (string path in bundles)
        {
            if (path.Contains("invalid-", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("no", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("null-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            filePaths.Add(path);
        }

        return filePaths;
    }

    [Fact]
    public static async Task Can_Intercept_Http_Requests_From_Bundle_File()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

        var headers = new Dictionary<string, string>()
        {
            ["accept"] = "application/vnd.github.v3+json",
            ["authorization"] = "token my-token",
            ["user-agent"] = "My-App/1.0.0",
        };

        // Act
        options.RegisterBundle(Path.Join("Bundles", "http-request-bundle.json"));

        // Assert
        await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/", mediaType: "text/html");
        await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/order-history");
        await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeattakeaway", headers: headers, mediaType: "application/json");
    }

    [Fact]
    public static async Task Can_Intercept_Http_Requests_From_Bundle_File_Async()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

        var headers = new Dictionary<string, string>()
        {
            ["accept"] = "application/vnd.github.v3+json",
            ["authorization"] = "token my-token",
            ["user-agent"] = "My-App/1.0.0",
        };

        // Act
        await options.RegisterBundleAsync(Path.Join("Bundles", "http-request-bundle.json"), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/", mediaType: "text/html");
        await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/order-history");
        await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeattakeaway", headers: headers, mediaType: "application/json");
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
        string content = await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeattakeaway/repos");

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
        string content = await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeattakeaway");

        content
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .ShouldBe(@"{""id"":1516790,""login"":""justeattakeaway"",""url"":""https://api.github.com/orgs/justeattakeaway""}");
    }

    [Fact]
    public static async Task Can_Intercept_Http_Requests_From_Bundle_File_With_Null_Json()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

        // Act
        options.RegisterBundle(Path.Join("Bundles", "content-as-null-json.json"));

        // Assert
        string content = await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeattakeaway");

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
        await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/3", (HttpStatusCode)418);
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
            ["user-agent"] = "My-App/1.0.0",
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
            ["user-agent"] = "My-Other-App/1.0.0",
        };

        var templateValues = new Dictionary<string, string>()
        {
            ["ApplicationName"] = "My-Other-App",
        };

        // Act
        options.RegisterBundle(Path.Join("Bundles", "templated-bundle-string.json"), templateValues);

        // Assert
        string content = await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/", headers: headers);
        content.ShouldBe("<html><head><title>Just Eat</title></head></html>");
    }

    [Fact]
    public static async Task Can_Intercept_Http_Requests_From_Bundle_File_With_Templated_String_With_User_Template_Values_Async()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

        var headers = new Dictionary<string, string>()
        {
            ["user-agent"] = "My-Other-App/1.0.0",
        };

        var templateValues = new Dictionary<string, string>()
        {
            ["ApplicationName"] = "My-Other-App",
        };

        // Act
        await options.RegisterBundleAsync(Path.Join("Bundles", "templated-bundle-string.json"), templateValues, TestContext.Current.CancellationToken);

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

        var templateValues = new Dictionary<string, string>()
        {
            ["AvatarUrl"] = "https://avatars.githubusercontent.com/u/1516790?v=4",
            ["BlogUrl"] = "https://tech.justeattakeaway.com/",
        };

        // Act
        options.RegisterBundle(Path.Join("Bundles", "templated-bundle-json.json"), templateValues);

        // Assert
        string content = await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeattakeaway");
        content
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .ShouldBe(@"{""id"":1516790,""login"":""justeattakeaway"",""url"":""https://api.github.com/orgs/justeattakeaway"",""avatar_url"":""https://avatars.githubusercontent.com/u/1516790?v=4"",""name"":""JustEatTakeaway"",""blog"":""https://tech.justeattakeaway.com/""}");

        // Assert
        content = await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeattakeaway/repos");
        content
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .ShouldBe(@"[{""id"":123456,""name"":""httpclient-interception"",""full_name"":""justeattakeaway/httpclient-interception"",""private"":false,""owner"":{""login"":""justeattakeaway"",""id"":1516790}}]");
    }

    [Fact]
    public static async Task Can_Intercept_Http_Requests_From_Bundle_File_With_Templated_Json_TemplateValues_Only_Defined_In_Code()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

        var templateValues = new Dictionary<string, string>()
        {
            ["AvatarUrl"] = "https://avatars.githubusercontent.com/u/1516790?v=4",
            ["BlogUrl"] = "https://tech.justeattakeaway.com/",
            ["CompanyName"] = "justeattakeaway",
            ["RepoName"] = "httpclient-interception",
        };

        // Act
        options.RegisterBundle(Path.Join("Bundles", "templated-bundle-json-no-parameters.json"), templateValues);

        // Assert
        string content = await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeattakeaway/repos");
        content
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .ShouldBe(@"[{""id"":123456,""name"":""httpclient-interception"",""full_name"":""justeattakeaway/httpclient-interception"",""private"":false,""owner"":{""login"":""justeattakeaway"",""id"":1516790}}]");
    }

    [Fact]
    public static async Task Can_Intercept_Http_Requests_From_Bundle_Stream()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

        var headers = new Dictionary<string, string>()
        {
            ["accept"] = "application/vnd.github.v3+json",
            ["authorization"] = "token my-token",
            ["user-agent"] = "My-App/1.0.0",
        };

        using var stream = typeof(BundleExtensionsTests).Assembly.GetManifestResourceStream("JustEat.HttpClientInterception.Bundles.http-request-bundle.json");
        stream.ShouldNotBeNull();

        // Act
        options.RegisterBundleFromStream(stream);

        // Assert
        await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/", mediaType: "text/html");
        await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/order-history");
        await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeattakeaway", headers: headers, mediaType: "application/json");
    }

    [Fact]
    public static async Task Can_Intercept_Http_Requests_From_Bundle_Stream_Async()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

        var headers = new Dictionary<string, string>()
        {
            ["accept"] = "application/vnd.github.v3+json",
            ["authorization"] = "token my-token",
            ["user-agent"] = "My-App/1.0.0",
        };

        using var stream = typeof(BundleExtensionsTests).Assembly.GetManifestResourceStream("JustEat.HttpClientInterception.Bundles.http-request-bundle.json");
        stream.ShouldNotBeNull();

        // Act
        await options.RegisterBundleFromStreamAsync(stream, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/", mediaType: "text/html");
        await HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/order-history");
        await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeattakeaway", headers: headers, mediaType: "application/json");
    }

    [Fact]
    public static void RegisterBundle_Validates_Parameters()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();
        string path = "foo.bar";

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => ((HttpClientInterceptorOptions)null).RegisterBundle(path)).ParamName.ShouldBe("options");
        Should.Throw<ArgumentNullException>(() => options.RegisterBundle(null)).ParamName.ShouldBe("path");
        Should.Throw<ArgumentNullException>(() => options.RegisterBundle(path, null)).ParamName.ShouldBe("templateValues");
    }

    [Fact]
    public static void RegisterBundleFromStream_Validates_Parameters()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();
        var stream = Stream.Null;

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => ((HttpClientInterceptorOptions)null).RegisterBundleFromStream(stream)).ParamName.ShouldBe("options");
        Should.Throw<ArgumentNullException>(() => options.RegisterBundleFromStream(null)).ParamName.ShouldBe("stream");
    }

    [Fact]
    public static async Task RegisterBundleAsync_Validates_Parameters()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();
        string path = "foo.bar";

        // Act and Assert
        (await Should.ThrowAsync<ArgumentNullException>(() => ((HttpClientInterceptorOptions)null).RegisterBundleAsync(path, cancellationToken: TestContext.Current.CancellationToken))).ParamName.ShouldBe("options");
        (await Should.ThrowAsync<ArgumentNullException>(() => options.RegisterBundleAsync(null, cancellationToken: TestContext.Current.CancellationToken))).ParamName.ShouldBe("path");
    }

    [Fact]
    public static async Task RegisterBundleFromStreamAsync_Validates_Parameters()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();
        var stream = Stream.Null;

        // Act and Assert
        (await Should.ThrowAsync<ArgumentNullException>(() => ((HttpClientInterceptorOptions)null).RegisterBundleFromStreamAsync(stream, cancellationToken: TestContext.Current.CancellationToken))).ParamName.ShouldBe("options");
        (await Should.ThrowAsync<ArgumentNullException>(() => options.RegisterBundleFromStreamAsync(null, cancellationToken: TestContext.Current.CancellationToken))).ParamName.ShouldBe("stream");
    }

    [Fact]
    public static void RegisterBundle_Throws_If_Bundle_Version_Is_Not_Supported()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();
        string path = Path.Join("Bundles", "invalid-bundle-version.json");

        // Act and Assert
        Should.Throw<NotSupportedException>(() => options.RegisterBundle(path));
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
        var exception = Should.Throw<NotSupportedException>(() => options.RegisterBundle(path));
        exception.Message.ShouldBe("Content format 'foo' for bundle item with Id 'invalid-content-format' is not supported.");
    }

    [Fact]
    public static void RegisterBundle_Throws_If_Invalid_Status_Code_Configured()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();
        string path = Path.Join("Bundles", "invalid-status-code.json");

        // Act and Assert
        var exception = Should.Throw<InvalidOperationException>(() => options.RegisterBundle(path));
        exception.Message.ShouldBe("Bundle item with Id 'invalid-status' has an invalid HTTP status code 'foo' configured.");
    }

    [Fact]
    public static void RegisterBundle_Throws_If_Invalid_Uri_Configured()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();
        string path = Path.Join("Bundles", "invalid-uri.json");

        // Act and Assert
        var exception = Should.Throw<InvalidOperationException>(() => options.RegisterBundle(path));
        exception.Message.ShouldBe("Bundle item with Id 'invalid-uri' has an invalid absolute URI '::invalid' configured.");
    }

    [Fact]
    public static void RegisterBundle_Throws_If_Invalid_Version_Configured()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();
        string path = Path.Join("Bundles", "invalid-version.json");

        // Act and Assert
        var exception = Should.Throw<InvalidOperationException>(() => options.RegisterBundle(path));
        exception.Message.ShouldBe("Bundle item with Id 'invalid-version' has an invalid version 'foo' configured.");
    }

    [Fact]
    public static void RegisterBundle_Throws_If_No_Uri_Configured()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();
        string path = Path.Join("Bundles", "no-uri.json");

        // Act and Assert
        var exception = Should.Throw<InvalidOperationException>(() => options.RegisterBundle(path));
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
            .ShouldBe(@"{""id"":1516790,""login"":""justeattakeaway"",""url"":""https://api.github.com/orgs/justeattakeaway""}");
    }

    [Fact]
    public static async Task Can_Intercept_Http_Requests_With_Different_Path_If_The_Uri_Query_String()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

        // Act
        options.RegisterBundle(Path.Join("Bundles", "ignoring-query.json"));

        // Assert
        string content = await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeattakeaway?foo=bar");

        content
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .ShouldBe(@"{""id"":1516790,""login"":""justeattakeaway"",""url"":""https://api.github.com/orgs/justeattakeaway""}");
    }

    [Fact]
    public static async Task Can_Skip_Bundle_Items()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

        // Act
        options.RegisterBundle(Path.Join("Bundles", "skipped-item-bundle.json"));

        // Assert
        await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => HttpAssert.GetAsync(options, "https://www.just-eat.co.uk/"));
    }

    [Theory]
    [MemberData(nameof(BundleFiles))]
    public static async Task Bundle_Schema_Is_Valid_From_Test(string bundlePath)
    {
        // Arrange
        string schemaPath = Path.Join(".", "http-request-bundle-schema.json");

        var json = JToken.Parse(await File.ReadAllTextAsync(bundlePath, TestContext.Current.CancellationToken));

        var schema = JSchema.Parse(
            await File.ReadAllTextAsync(schemaPath, TestContext.Current.CancellationToken),
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

    [Fact]
    public static async Task Can_Intercept_Http_Requests_With_Correct_Precedence_For_Http_Request_Headers()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

        var requestUrl = "https://registry.hub.docker.com/v2/user/image/manifests/latest";

        var unauthorized = new Dictionary<string, string>()
        {
            ["Accept"] = "application/vnd.oci.image.index.v1+json",
        };

        var authorized = new Dictionary<string, string>()
        {
            ["Accept"] = "application/vnd.oci.image.index.v1+json",
            ["Authorization"] = "Bearer not-a-real-docker-hub-token",
        };

        options.RegisterBundle(Path.Join("Bundles", "header-matching.json"));

        // Act
        var first = await HttpAssert.GetAsync(options, requestUrl, headers: unauthorized);
        var second = await HttpAssert.GetAsync(options, requestUrl, headers: authorized);

        // Assert
        first.ShouldBe("unauthorized");
        second.ShouldBe("authorized");
    }
}
