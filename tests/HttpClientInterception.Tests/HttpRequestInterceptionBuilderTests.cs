// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace JustEat.HttpClientInterception;

public static class HttpRequestInterceptionBuilderTests
{
    [Fact]
    public static async Task Register_For_Builder_With_All_Defaults_Registers_Interception()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder();
        var options = new HttpClientInterceptorOptions().Register(builder);

        string actual;

        using (var client = options.CreateHttpClient())
        {
            // Act
            actual = await client.GetStringAsync("http://localhost");
        }

        // Assert
        actual.ShouldBe(string.Empty);
    }

    [Fact]
    public static async Task Register_For_Builder_With_Defaults_Registers_Interception()
    {
        // Arrange
        var requestUri = "https://google.com/";
        var expected = new[] { "foo", "bar" };

        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForUrl(requestUri)
            .WithJsonContent(expected);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        var actual = await HttpAssert.GetAsync<string[]>(options, requestUri);

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public static async Task Builder_Consumes_Content_By_Reference()
    {
        // Arrange
        var requestUri = "https://google.com/";

        var expected = new CustomObject()
        {
            FavoriteColor = CustomObject.Color.Blue,
            Number = 2,
            Text = "The elephant",
        };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(requestUri)
            .WithJsonContent(expected);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        var actual1 = await HttpAssert.GetAsync<CustomObject>(options, requestUri);

        // Assert
        actual1.ShouldNotBeNull();
        actual1.ShouldNotBeSameAs(expected);
        actual1.FavoriteColor.ShouldBe(CustomObject.Color.Blue);
        actual1.Number.ShouldBe(2);
        actual1.Text.ShouldBe("The elephant");

        // Arrange
        expected.FavoriteColor = CustomObject.Color.Red;
        expected.Number = 42;
        expected.Text = "L'éléphant";

        // Act
        var actual2 = await HttpAssert.GetAsync<CustomObject>(options, requestUri);

        // Assert
        actual2.ShouldNotBeNull();
        actual2.ShouldNotBeSameAs(actual1);
        actual2.FavoriteColor.ShouldBe(CustomObject.Color.Red);
        actual2.Number.ShouldBe(42);
        actual2.Text.ShouldBe("L'éléphant");
    }

    [Fact]
    public static async Task WithJsonContent_Uses_Default_Json_Serializer()
    {
        // Arrange
        var requestUri = "https://google.com/";
        var expected = new { mode = EventResetMode.ManualReset };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(requestUri)
            .WithJsonContent(expected);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        string actual = await HttpAssert.GetAsync(options, requestUri);

        // Assert
        actual.ShouldBe(@"{""mode"":1}");
    }

    [Fact]
    public static async Task WithSystemTextJsonContent_Uses_Default_Json_Serializer()
    {
        // Arrange
        var requestUri = "https://google.com/";
        var expected = new { mode = EventResetMode.ManualReset };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(requestUri)
            .WithSystemTextJsonContent(expected);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        string actual = await HttpAssert.GetAsync(options, requestUri);

        // Assert
        actual.ShouldBe(@"{""mode"":1}");
    }

    [Fact]
    public static async Task Builder_Uses_Specified_Json_Serializer_Options()
    {
        // Arrange
        var requestUri = "https://google.com/";
        var expected = new { mode = EventResetMode.ManualReset };

        var settings = new JsonSerializerOptions();
        settings.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUrl(requestUri)
            .WithSystemTextJsonContent(expected, settings);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        string actual = await HttpAssert.PostAsync(options, requestUri, new { });

        // Assert
        actual.ShouldBe(@"{""mode"":""ManualReset""}");
    }

    [Fact]
    public static async Task Builder_For_Plaintext_String_With_Custom_Status_Code_Registers_Interception()
    {
        // Arrange
        var requestUri = "https://google.com/";
        var expected = "Not found";

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(requestUri)
            .WithStatus(404)
            .WithContent(expected);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        string actual = await HttpAssert.GetAsync(options, requestUri, HttpStatusCode.NotFound);

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public static async Task Builder_For_Html_With_Custom_Media_Type_Registers_Interception()
    {
        // Arrange
        var requestUri = "https://google.com/";
        var mediaType = "text/html";
        var expected = "<html>foo></html>";

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(requestUri)
            .WithMediaType(mediaType)
            .WithContent(expected);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        string actual = await HttpAssert.GetAsync(options, requestUri, mediaType: mediaType);

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public static async Task Builder_If_Content_String_Is_Null_Registers_Interception()
    {
        // Arrange
        string requestUri = "https://google.com/";
        string expected = null;

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(requestUri)
            .WithContent(expected);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        string actual = await HttpAssert.GetAsync(options, requestUri);

        // Assert
        actual.ShouldBe(string.Empty);
    }

    [Fact]
    public static async Task Builder_For_Raw_Bytes_Registers_Interception_Synchronous()
    {
        // Arrange
        string requestUri = "https://google.com/";

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(requestUri)
            .WithContent(() => ".NET"u8.ToArray());

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        string actual = await HttpAssert.GetAsync(options, requestUri);

        // Assert
        actual.ShouldBe(".NET");
    }

    [Fact]
    public static async Task Builder_For_Raw_Bytes_Clears_Content()
    {
        // Arrange
        string requestUri = "https://google.com/";

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(requestUri)
            .WithContent(() => ".NET"u8.ToArray())
            .WithContent((Func<byte[]>)null)
            .WithContent((Func<Task<byte[]>>)null);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        string actual = await HttpAssert.GetAsync(options, requestUri);

        // Assert
        actual.ShouldBe(string.Empty);
    }

    [Fact]
    public static async Task Builder_For_Raw_Bytes_Registers_Interception_Asynchronous()
    {
        // Arrange
        string requestUri = "https://google.com/";

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(requestUri)
            .WithContentStream(() => Task.FromResult<Stream>(new MemoryStream(".NET"u8.ToArray())))
            .WithContent(() => Task.FromResult(".NET"u8.ToArray()));

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        string actual = await HttpAssert.GetAsync(options, requestUri);

        // Assert
        actual.ShouldBe(".NET");
    }

    [Fact]
    public static async Task Builder_For_Stream_Registers_Interception_Synchronous()
    {
        // Arrange
        string requestUri = "https://google.com/";

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(requestUri)
            .WithContentStream(() => new MemoryStream(".NET"u8.ToArray()));

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        string actual = await HttpAssert.GetAsync(options, requestUri);

        // Assert
        actual.ShouldBe(".NET");
    }

    [Fact]
    public static async Task Builder_For_Stream_Clears_Stream()
    {
        // Arrange
        string requestUri = "https://google.com/";

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(requestUri)
            .WithContentStream(() => new MemoryStream(".NET"u8.ToArray()))
            .WithContentStream((Func<Stream>)null)
            .WithContentStream((Func<Task<Stream>>)null);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        string actual = await HttpAssert.GetAsync(options, requestUri);

        // Assert
        actual.ShouldBe(string.Empty);
    }

    [Fact]
    public static async Task Builder_For_Stream_Registers_Interception_Asynchronous()
    {
        // Arrange
        string requestUri = "https://google.com/";

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(requestUri)
            .WithContent(() => Task.FromResult(".NET"u8.ToArray()))
            .WithContentStream(() => Task.FromResult<Stream>(new MemoryStream(".NET"u8.ToArray())));

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        string actual = await HttpAssert.GetAsync(options, requestUri);

        // Assert
        actual.ShouldBe(".NET");
    }

    [Fact]
    public static async Task GetResponseAsync_Returns_Empty_Response_If_Custom_Response_Header()
    {
        // Arrange
        string url = "https://google.com/";

        var headers = new Dictionary<string, string>()
        {
            ["a"] = "b",
            ["c"] = "d",
        };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(url)
            .WithResponseHeaders(headers);

        var options = new HttpClientInterceptorOptions()
            .Register(builder);

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        HttpResponseMessage actual = await options.GetResponseAsync(request);

        // Assert
        actual.ShouldNotBeNull();
        actual.Headers.GetValues("a").ShouldBe(new[] { "b" });
        actual.Headers.GetValues("c").ShouldBe(new[] { "d" });
    }

    [Fact]
    public static async Task GetResponseAsync_Returns_Empty_Response_If_Custom_Response_Headers()
    {
        // Arrange
        string url = "https://google.com/";

        var headers = new Dictionary<string, ICollection<string>>()
        {
            ["a"] = new[] { "b" },
            ["c"] = new[] { "d", "e" },
        };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(url)
            .WithResponseHeaders(headers);

        var options = new HttpClientInterceptorOptions()
            .Register(builder);

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        HttpResponseMessage actual = await options.GetResponseAsync(request);

        // Assert
        actual.ShouldNotBeNull();
        actual.Headers.GetValues("a").ShouldBe(new[] { "b" });
        actual.Headers.GetValues("c").ShouldBe(new[] { "d", "e" });
    }

    [Fact]
    public static async Task GetResponseAsync_Returns_Empty_Response_If_Individual_Custom_Response_Headers()
    {
        // Arrange
        string url = "https://google.com/";

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(url)
            .WithResponseHeader("a", "b")
            .WithResponseHeader("c", "d", "e", "f")
            .WithResponseHeader("c", "d", "e");

        var options = new HttpClientInterceptorOptions()
            .Register(builder);

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        HttpResponseMessage actual = await options.GetResponseAsync(request);

        // Assert
        actual.ShouldNotBeNull();
        actual.Headers.GetValues("a").ShouldBe(new[] { "b" });
        actual.Headers.GetValues("c").ShouldBe(new[] { "d", "e" });
    }

    [Fact]
    public static async Task GetResponseAsync_Returns_Empty_Response_If_Custom_Content_Header()
    {
        // Arrange
        string url = "https://google.com/";

        var headers = new Dictionary<string, string>()
        {
            ["a"] = "b",
            ["c"] = "d",
        };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(url)
            .WithContentHeaders(headers);

        var options = new HttpClientInterceptorOptions()
            .Register(builder);

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        HttpResponseMessage actual = await options.GetResponseAsync(request);

        // Assert
        actual.ShouldNotBeNull();
        actual.Content.Headers.GetValues("a").ShouldBe(new[] { "b" });
        actual.Content.Headers.GetValues("c").ShouldBe(new[] { "d" });
    }

    [Fact]
    public static async Task GetResponseAsync_Returns_Empty_Response_If_Custom_Content_Headers()
    {
        // Arrange
        string url = "https://google.com/";

        var headers = new Dictionary<string, ICollection<string>>()
        {
            ["a"] = new[] { "b" },
            ["c"] = new[] { "d", "e" },
        };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(url)
            .WithContentHeaders(headers);

        var options = new HttpClientInterceptorOptions()
            .Register(builder);

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        HttpResponseMessage actual = await options.GetResponseAsync(request);

        // Assert
        actual.ShouldNotBeNull();
        actual.Content.Headers.GetValues("a").ShouldBe(new[] { "b" });
        actual.Content.Headers.GetValues("c").ShouldBe(new[] { "d", "e" });
    }

    [Fact]
    public static async Task GetResponseAsync_Returns_Empty_Response_If_Individual_Custom_Content_Headers()
    {
        // Arrange
        string url = "https://google.com/";

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(url)
            .WithContentHeader("a", "b")
            .WithContentHeader("c", "d", "e", "f")
            .WithContentHeader("c", "d", "e");

        var options = new HttpClientInterceptorOptions()
            .Register(builder);

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        HttpResponseMessage actual = await options.GetResponseAsync(request);

        // Assert
        actual.ShouldNotBeNull();
        actual.ReasonPhrase.ShouldBe("OK");
        actual.Version.ShouldBe(new Version(1, 1));
        actual.Content.Headers.GetValues("a").ShouldBe(new[] { "b" });
        actual.Content.Headers.GetValues("c").ShouldBe(new[] { "d", "e" });
    }

    [Fact]
    public static async Task Register_Uses_Defaults_For_Plain_Builder()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();
        var builder = new HttpRequestInterceptionBuilder();

        // Act
        options.Register(builder);

        // Assert
        (await HttpAssert.GetAsync(options, "http://localhost")).ShouldBeEmpty();
    }

    [Fact]
    public static async Task Register_Builds_Uri_From_Components_From_Builder()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();

        var builder = new HttpRequestInterceptionBuilder()
            .ForHttps()
            .ForHost("something.com")
            .ForPort(444)
            .ForPath("my-path")
            .ForQuery("q=1");

        // Act
        options.Register(builder);

        // Assert
        (await HttpAssert.GetAsync(options, "https://something.com:444/my-path?q=1")).ShouldBeEmpty();
    }

    [Fact]
    public static async Task Register_Builds_Uri_From_Components_From_Builder_With_Http_And_Default_Port()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();

        HttpRequestInterceptionBuilder builder = new HttpRequestInterceptionBuilder()
            .ForHttps()
            .ForHttp()
            .ForHost("something.com")
            .ForPort(80);

        // Act
        options.Register(builder);

        // Assert
        (await HttpAssert.GetAsync(options, "http://something.com")).ShouldBeEmpty();
    }

    [Fact]
    public static async Task Register_Builds_Ignore_Path_Key()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();

        var builder = new HttpRequestInterceptionBuilder()
            .ForHost("something.com")
            .IgnoringPath();

        // Act
        options.Register(builder);

        // Assert
        (await HttpAssert.GetAsync(options, "http://something.com/path/1234")).ShouldBeEmpty();
    }

    [Fact]
    public static async Task Register_Builds_Without_Ignore_Path_Key()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();

        var builder = new HttpRequestInterceptionBuilder()
            .ForHost("something.com")
            .IgnoringPath()
            .IgnoringPath(false);

        // Act
        options.Register(builder);

        // Assert
        await Should.ThrowAsync<HttpRequestException>(() =>
            HttpAssert.GetAsync(options, "http://something.com/path/1234"));
    }

    [Fact]
    public static async Task Register_Builds_Ignore_Query_Key()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();

        HttpRequestInterceptionBuilder builder = new HttpRequestInterceptionBuilder()
            .ForHost("something.com")
            .IgnoringQuery();

        // Act
        options.Register(builder);

        // Assert
        (await HttpAssert.GetAsync(options, "http://something.com?query=1234")).ShouldBeEmpty();
    }

    [Fact]
    public static async Task Register_Builds_Without_Ignore_Query_Key()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions();

        HttpRequestInterceptionBuilder builder = new HttpRequestInterceptionBuilder()
            .ForHost("something.com")
            .IgnoringQuery()
            .IgnoringQuery(false);

        // Act
        options.Register(builder);

        // Assert
        await Should.ThrowAsync<HttpRequestException>(() =>
            HttpAssert.GetAsync(options, "http://something.com?query=1234"));
    }

    [Fact]
    public static async Task Register_Builds_Uri_From_UriBuilder()
    {
        // Arrange
        var uriBuilder = new UriBuilder("https://github.com/justeat");

        var options = new HttpClientInterceptorOptions();

        var builder = new HttpRequestInterceptionBuilder().ForUri(uriBuilder);

        // Act
        options.Register(builder);

        // Assert
        (await HttpAssert.GetAsync(options, "https://github.com/justeat")).ShouldBeEmpty();
    }

    [Fact]
    public static async Task Register_For_Callback_Invokes_Delegate_With_Message_Synchronous()
    {
        // Arrange
        var requestUri = new Uri("https://api.just-eat.com/");
        var content = new { foo = "bar" };

        bool wasDelegateInvoked = false;

        void OnIntercepted(HttpRequestMessage request)
        {
            request.ShouldNotBeNull();
            request.Method.ShouldBe(HttpMethod.Post);
            request.RequestUri.ShouldBe(requestUri);

            string json = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            var body = JObject.Parse(json);

            body.Value<string>("foo").ShouldBe("bar");

            wasDelegateInvoked = true;
        }

        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUri(requestUri)
            .WithInterceptionCallback(OnIntercepted);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        await HttpAssert.PostAsync(options, requestUri.ToString(), content);

        // Assert
        wasDelegateInvoked.ShouldBeTrue();
    }

    [Fact]
    public static async Task Register_For_Callback_Invokes_Delegate_With_Message_Asynchronous()
    {
        // Arrange
        var requestUri = new Uri("https://api.just-eat.com/");
        var content = new { foo = "bar" };

        bool wasDelegateInvoked = false;

        async Task OnInterceptedAsync(HttpRequestMessage request)
        {
            request.ShouldNotBeNull();
            request.Method.ShouldBe(HttpMethod.Post);
            request.RequestUri.ShouldBe(requestUri);

            string json = await request.Content.ReadAsStringAsync();

            var body = JObject.Parse(json);

            body.Value<string>("foo").ShouldBe("bar");

            wasDelegateInvoked = true;
        }

        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUri(requestUri)
            .WithInterceptionCallback(OnInterceptedAsync);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        await HttpAssert.PostAsync(options, requestUri.ToString(), content);

        // Assert
        wasDelegateInvoked.ShouldBeTrue();
    }

    [Fact]
    public static async Task GetResponseAsync_Uses_Reason_Phrase()
    {
        // Arrange
        string url = "https://google.com/";

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(url)
            .WithReason("My custom reason");

        var options = new HttpClientInterceptorOptions()
            .Register(builder);

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        HttpResponseMessage actual = await options.GetResponseAsync(request);

        // Assert
        actual.ShouldNotBeNull();
        actual.ReasonPhrase.ShouldBe("My custom reason");
    }

    [Fact]
    public static async Task GetResponseAsync_Uses_Version()
    {
        // Arrange
        string url = "https://google.com/";

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl(url)
            .WithVersion(new Version(2, 1));

        var options = new HttpClientInterceptorOptions()
            .Register(builder);

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Act
        HttpResponseMessage actual = await options.GetResponseAsync(request);

        // Assert
        actual.ShouldNotBeNull();
        actual.Version.ShouldBe(new Version(2, 1));
    }

    [Fact]
    public static void WithContent_Validates_Parameters()
    {
        // Act and Assert
        Should.Throw<ArgumentNullException>(() => ((HttpRequestInterceptionBuilder)null).WithContent((string)null), "builder");
    }

    [Fact]
    public static void WithFormContent_Validates_Parameters()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder();
        IEnumerable<KeyValuePair<string, string>> parameters = null;

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => ((HttpRequestInterceptionBuilder)null).WithFormContent(parameters), "builder");
        Should.Throw<ArgumentNullException>(() => builder.WithFormContent(parameters), "parameters");
    }

    [Fact]
    public static async Task WithFormContent_Returns_Correct_Response()
    {
        // Arrange
        var requestUri = "https://api.twitter.com/oauth/request_token";
        var parameters = new Dictionary<string, string>()
        {
            ["oauth_callback_confirmed"] = "true",
            ["oauth_token"] = "a b c",
            ["oauth_token_secret"] = "U5LJUL3eS+fl9bj9xqHKXyHpBc8=",
        };

        var options = new HttpClientInterceptorOptions();
        new HttpRequestInterceptionBuilder()
            .Requests().ForPost().ForUrl(requestUri)
            .Responds().WithFormContent(parameters)
            .RegisterWith(options);

        // Act
        string actual = await HttpAssert.PostAsync(options, requestUri, new { }, mediaType: "application/x-www-form-urlencoded");

        // Assert
        actual.ShouldBe("oauth_callback_confirmed=true&oauth_token=a+b+c&oauth_token_secret=U5LJUL3eS%2Bfl9bj9xqHKXyHpBc8%3D");
    }

    [Fact]
    public static void WithJsonContent_Validates_Parameters()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder();
        object content = null;

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => ((HttpRequestInterceptionBuilder)null).WithJsonContent(content), "builder");
        Should.Throw<ArgumentNullException>(() => builder.WithJsonContent(content), "content");
    }

    [Fact]
    public static void WithSystemTextJsonContent_Validates_Parameters()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder();
        object content = null;

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => ((HttpRequestInterceptionBuilder)null).WithSystemTextJsonContent(content), "builder");
        Should.Throw<ArgumentNullException>(() => builder.WithSystemTextJsonContent(content), "content");
    }

    [Fact]
    public static void ForUrl_Validates_Parameters()
    {
        // Arrange
        string uriString = "https://google.com/";

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => ((HttpRequestInterceptionBuilder)null).ForUrl(uriString), "builder");
    }

    [Fact]
    public static void ForDelete_Validates_Parameters()
    {
        // Act and Assert
        Should.Throw<ArgumentNullException>(() => ((HttpRequestInterceptionBuilder)null).ForDelete(), "builder");
    }

    [Fact]
    public static void ForGet_Validates_Parameters()
    {
        // Act and Assert
        Should.Throw<ArgumentNullException>(() => ((HttpRequestInterceptionBuilder)null).ForGet(), "builder");
    }

    [Fact]
    public static void ForPost_Validates_Parameters()
    {
        // Act and Assert
        Should.Throw<ArgumentNullException>(() => ((HttpRequestInterceptionBuilder)null).ForPost(), "builder");
    }

    [Fact]
    public static void ForPut_Validates_Parameters()
    {
        // Act and Assert
        Should.Throw<ArgumentNullException>(() => ((HttpRequestInterceptionBuilder)null).ForPut(), "builder");
    }

    [Fact]
    public static void ForHttp_Validates_Parameters()
    {
        // Act and Assert
        Should.Throw<ArgumentNullException>(() => ((HttpRequestInterceptionBuilder)null).ForHttp(), "builder");
    }

    [Fact]
    public static void ForHttps_Validates_Parameters()
    {
        // Act and Assert
        Should.Throw<ArgumentNullException>(() => ((HttpRequestInterceptionBuilder)null).ForHttps(), "builder");
    }

    [Fact]
    public static void ForMethod_Throws_If_Method_Is_Null()
    {
        // Arrange
        HttpRequestInterceptionBuilder builder = new HttpRequestInterceptionBuilder();

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => builder.ForMethod(null), "method");
    }

    [Fact]
    public static void ForUri_Throws_If_UriBuilder_Is_Null()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder();
        UriBuilder uriBuilder = null;

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => builder.ForUri(uriBuilder), "uriBuilder");
    }

    [Fact]
    public static async Task Register_For_Callback_Invokes_Delegate_And_Intercepts_If_Returns_True()
    {
        // Arrange
        var requestUri = new Uri("https://api.just-eat.com/");
        var content = new { foo = "bar" };

        bool wasDelegateInvoked = false;

        Task<bool> OnInterceptedAsync(HttpRequestMessage request)
        {
            wasDelegateInvoked = true;
            return Task.FromResult(true);
        }

        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUri(requestUri)
            .WithInterceptionCallback(OnInterceptedAsync);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        await HttpAssert.PostAsync(options, requestUri.ToString(), content);

        // Assert
        wasDelegateInvoked.ShouldBeTrue();
    }

    [Fact]
    public static async Task Register_For_Callback_Invokes_Delegate_With_CancellationToken_And_Intercepts_If_Returns_True()
    {
        // Arrange
        var requestUri = new Uri("https://api.just-eat.com/");
        var content = new { foo = "bar" };

        bool wasDelegateInvoked = false;

        Task<bool> OnInterceptedAsync(HttpRequestMessage request, CancellationToken token)
        {
            wasDelegateInvoked = true;
            return Task.FromResult(true);
        }

        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUri(requestUri)
            .WithInterceptionCallback(OnInterceptedAsync);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        await HttpAssert.PostAsync(options, requestUri.ToString(), content);

        // Assert
        wasDelegateInvoked.ShouldBeTrue();
    }

    [Fact]
    public static async Task Register_For_Callback_Invokes_Delegate_And_Does_Not_Intercept_If_Returns_False()
    {
        // Arrange
        var requestUri = new Uri("https://api.just-eat.com/");
        var content = new { foo = "bar" };

        bool wasDelegateInvoked = false;

        Task<bool> OnInterceptedAsync(HttpRequestMessage request)
        {
            wasDelegateInvoked = true;
            return Task.FromResult(false);
        }

        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUri(requestUri)
            .WithInterceptionCallback(OnInterceptedAsync);

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => HttpAssert.PostAsync(options, requestUri.ToString(), content));

        // Assert
        exception.Message.ShouldStartWith("No HTTP response is configured for ");
        wasDelegateInvoked.ShouldBeTrue();
    }

    [Fact]
    public static async Task Register_For_Callback_Invokes_Delegate_With_CancellationToken_And_Does_Not_Intercept_If_Returns_False()
    {
        // Arrange
        var requestUri = new Uri("https://api.just-eat.com/");
        var content = new { foo = "bar" };

        bool wasDelegateInvoked = false;

        Task<bool> OnInterceptedAsync(HttpRequestMessage request, CancellationToken token)
        {
            wasDelegateInvoked = true;
            return Task.FromResult(false);
        }

        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUri(requestUri)
            .WithInterceptionCallback(OnInterceptedAsync);

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => HttpAssert.PostAsync(options, requestUri.ToString(), content));

        // Assert
        exception.Message.ShouldStartWith("No HTTP response is configured for ");
        wasDelegateInvoked.ShouldBeTrue();
    }

    [Fact]
    public static async Task Register_For_Callback_Invokes_Predicate_Delegate_And_Does_Not_Intercept_If_Returns_False()
    {
        // Arrange
        var requestUri = new Uri("https://api.just-eat.com/");
        var content = new { foo = "bar" };

        bool wasDelegateInvoked = false;

        bool OnIntercepted(HttpRequestMessage request)
        {
            wasDelegateInvoked = true;
            request.ShouldNotBeNull();
            return false;
        }

        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUri(requestUri)
            .WithInterceptionCallback(OnIntercepted);

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => HttpAssert.PostAsync(options, requestUri.ToString(), content));

        // Assert
        exception.Message.ShouldStartWith("No HTTP response is configured for ");
        wasDelegateInvoked.ShouldBeTrue();
    }

    [Fact]
    public static async Task Register_For_Callback_Clears_Delegate_For_Action_If_Set_To_Null()
    {
        // Arrange
        var requestUri = new Uri("https://api.just-eat.com/");
        var content = new { foo = "bar" };

        bool wasDelegateInvoked = false;

        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUri(requestUri)
            .WithInterceptionCallback((request) => wasDelegateInvoked = true)
            .WithInterceptionCallback((Action<HttpRequestMessage>)null);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        await HttpAssert.PostAsync(options, requestUri.ToString(), content);

        // Assert
        wasDelegateInvoked.ShouldBeFalse();
    }

    [Fact]
    public static async Task Register_For_Callback_Clears_Delegate_For_Predicate_If_Set_To_Null()
    {
        // Arrange
        var requestUri = new Uri("https://api.just-eat.com/");
        var content = new { foo = "bar" };

        bool wasDelegateInvoked = false;

        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUri(requestUri)
            .WithInterceptionCallback((request) => wasDelegateInvoked = true)
            .WithInterceptionCallback((Predicate<HttpRequestMessage>)null);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        await HttpAssert.PostAsync(options, requestUri.ToString(), content);

        // Assert
        wasDelegateInvoked.ShouldBeFalse();
    }

    [Fact]
    public static async Task Register_For_Callback_Clears_Delegate_For_Predicate_With_Cancellation_If_Set_To_Null()
    {
        // Arrange
        var requestUri = new Uri("https://api.just-eat.com/");
        var content = new { foo = "bar" };

        bool wasDelegateInvoked = false;

        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUri(requestUri)
            .WithInterceptionCallback((request) => wasDelegateInvoked = true)
            .WithInterceptionCallback((Func<HttpRequestMessage, CancellationToken, Task>)null);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        await HttpAssert.PostAsync(options, requestUri.ToString(), content);

        // Assert
        wasDelegateInvoked.ShouldBeFalse();
    }

    [Fact]
    public static async Task Register_For_Callback_Clears_Delegate_For_Async_Predicate_If_Set_To_Null()
    {
        // Arrange
        var requestUri = new Uri("https://api.just-eat.com/");
        var content = new { foo = "bar" };

        bool wasDelegateInvoked = false;

        var builder = new HttpRequestInterceptionBuilder()
            .ForPost()
            .ForUri(requestUri)
            .WithInterceptionCallback((request) => wasDelegateInvoked = true)
            .WithInterceptionCallback((Func<HttpRequestMessage, Task<bool>>)null);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        await HttpAssert.PostAsync(options, requestUri.ToString(), content);

        // Assert
        wasDelegateInvoked.ShouldBeFalse();
    }

    [Fact]
    public static async Task Builder_For_Any_Host_Registers_Interception()
    {
        // Arrange
        string expected = "<html>foo></html>";

        var builder = new HttpRequestInterceptionBuilder()
            .ForAnyHost()
            .WithContent(expected);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        string actual1 = await HttpAssert.GetAsync(options, "http://google.com/");
        string actual2 = await HttpAssert.GetAsync(options, "http://bing.com/");

        // Assert
        actual1.ShouldBe(expected);
        actual2.ShouldBe(actual1);
    }

    [Fact]
    public static async Task Builder_For_Any_Host_Path_And_Query_Registers_Interception()
    {
        // Arrange
        string expected = "<html>foo></html>";

        var builder = new HttpRequestInterceptionBuilder()
            .ForAnyHost()
            .IgnoringPath()
            .IgnoringQuery()
            .WithContent(expected);

        var options = new HttpClientInterceptorOptions().Register(builder);

        // Act
        string actual1 = await HttpAssert.GetAsync(options, "http://google.com/blah/?foo=bar");
        string actual2 = await HttpAssert.GetAsync(options, "http://bing.com/qux/?foo=baz");

        // Assert
        actual1.ShouldBe(expected);
        actual2.ShouldBe(actual1);
    }

    [Fact]
    public static async Task Builder_Returns_Fallback_For_Missing_Registration()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForHost("google.com")
            .WithStatus(HttpStatusCode.BadRequest);

        var options = new HttpClientInterceptorOptions().Register(builder);

        options.OnMissingRegistration = (request) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway));

        // Act and Assert
        await HttpAssert.GetAsync(options, "http://google.com/", HttpStatusCode.BadRequest);
        await HttpAssert.GetAsync(options, "http://bing.com/", HttpStatusCode.BadGateway);
    }

    [Fact]
    public static void RegisterWith_Validates_Parameters()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder();

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => builder.RegisterWith(null), "options");
    }

    [Fact]
    public static async Task Using_The_Same_Builder_For_Multiple_Registrations_On_The_Same_Host_Retains_Default_Port()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration();

        var builder = new HttpRequestInterceptionBuilder()
            .ForHttps()
            .ForHost("api.github.com")
            .ForPath("orgs/justeat")
            .RegisterWith(options);

        // Change to a different scheme without an explicit port
        builder.ForHttp()
                .RegisterWith(options);

        // Act and Assert
        await HttpAssert.GetAsync(options, "http://api.github.com/orgs/justeat");
        await HttpAssert.GetAsync(options, "http://api.github.com:80/orgs/justeat");
        await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeat");
        await HttpAssert.GetAsync(options, "https://api.github.com:443/orgs/justeat");
    }

    [Fact]
    public static async Task Using_The_Same_Builder_For_Multiple_Registrations_With_Custom_Ports()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration();

        var builder = new HttpRequestInterceptionBuilder()
            .ForPort(123)
            .ForHttps()
            .ForHost("api.github.com")
            .ForPath("orgs/justeat")
            .RegisterWith(options);

        // Change to a different scheme without changing the port
        builder = builder
            .ForHttp()
            .RegisterWith(options);

        // Change the port and not the scheme
        builder = builder
            .ForPort(456)
            .RegisterWith(options);

        // Change the scheme and use an explicit port
        builder = builder
            .ForHttps()
            .ForPort(443)
            .RegisterWith(options);

        // Restore default scheme and port
        builder.ForHttp()
                .ForPort(-1)
                .RegisterWith(options);

        // Act and Assert
        await HttpAssert.GetAsync(options, "https://api.github.com:123/orgs/justeat");
        await HttpAssert.GetAsync(options, "http://api.github.com:123/orgs/justeat");
        await HttpAssert.GetAsync(options, "http://api.github.com:456/orgs/justeat");
        await HttpAssert.GetAsync(options, "https://api.github.com/orgs/justeat");
        await HttpAssert.GetAsync(options, "https://api.github.com:443/orgs/justeat");
        await HttpAssert.GetAsync(options, "http://api.github.com/orgs/justeat");
        await HttpAssert.GetAsync(options, "http://api.github.com:80/orgs/justeat");
    }

    [Fact]
    public static async Task Builder_With_Header_To_Match_Intercepts_Request()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForHttps()
            .ForHost("public.je-apis.com")
            .ForPath("consumer/order-history")
            .ForRequestHeader("Accept-Tenant", "uk")
            .WithJsonContent(new { History = new[] { new { Id = "abc123" } } });

        var options = new HttpClientInterceptorOptions()
            .Register(builder);

        string json;

        using (var client = options.CreateHttpClient())
        {
            client.DefaultRequestHeaders.Add("Accept-Tenant", "uk");

            // Act
            json = await client.GetStringAsync("https://public.je-apis.com/consumer/order-history");
        }

        // Assert
        var content = JObject.Parse(json);
        content["History"].First().Value<string>("Id").ShouldBe("abc123");
    }

    [Fact]
    public static async Task Builder_With_Headers_To_Match_Intercepts_Request()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForHttps()
            .ForHost("public.je-apis.com")
            .ForPath("consumer/order-history")
            .ForRequestHeader("Accept-Tenant", "uk")
            .ForRequestHeader("Authorization", "basic my-key")
            .WithJsonContent(new { History = new[] { new { Id = "abc123" } } });

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        string json;

        using (var client = options.CreateHttpClient())
        {
            client.DefaultRequestHeaders.Add("Accept-Tenant", "uk");
            client.DefaultRequestHeaders.Add("Authorization", "basic my-key");

            // Act
            json = await client.GetStringAsync("https://public.je-apis.com/consumer/order-history");
        }

        // Assert
        var content = JObject.Parse(json);
        content["History"].First().Value<string>("Id").ShouldBe("abc123");
    }

    [Fact]
    public static async Task Builder_With_Headers_From_Dictionary_To_Match_Intercepts_Request()
    {
        // Arrange
        var headers = new Dictionary<string, string>()
        {
            ["Accept-Tenant"] = "uk",
            ["Authorization"] = "basic my-key",
        };

        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForHttps()
            .ForHost("public.je-apis.com")
            .ForPath("consumer/order-history")
            .ForRequestHeaders(headers)
            .WithJsonContent(new { History = new[] { new { Id = "abc123" } } });

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        string json;

        using (var client = options.CreateHttpClient())
        {
            client.DefaultRequestHeaders.Add("Accept-Tenant", "uk");
            client.DefaultRequestHeaders.Add("Authorization", "basic my-key");

            // Act
            json = await client.GetStringAsync("https://public.je-apis.com/consumer/order-history");
        }

        // Assert
        var content = JObject.Parse(json);
        content["History"].First().Value<string>("Id").ShouldBe("abc123");
    }

    [Fact]
    public static async Task Builder_With_One_Multivalue_Header_To_Match_Intercepts_Request()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForHttps()
            .ForHost("public.je-apis.com")
            .ForPath("consumer/order-history")
            .ForRequestHeader("x-forwarded-for", "192.168.1.1", "192.168.1.2")
            .WithJsonContent(new { History = new[] { new { Id = "abc123" } } });

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        string json;

        using (var client = options.CreateHttpClient())
        {
            client.DefaultRequestHeaders.Add("x-forwarded-for", new[] { "192.168.1.1", "192.168.1.2" });

            // Act
            json = await client.GetStringAsync("https://public.je-apis.com/consumer/order-history");
        }

        // Assert
        var content = JObject.Parse(json);
        content["History"].First().Value<string>("Id").ShouldBe("abc123");
    }

    [Fact]
    public static async Task Builder_With_Many_Multivalue_Headers_To_Match_Intercepts_Request()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForHttps()
            .ForHost("public.je-apis.com")
            .ForPath("consumer/order-history")
            .ForRequestHeader("x-forwarded-for", "192.168.1.1", "192.168.1.2")
            .ForRequestHeader("x-forwarded-proto", "http", "https")
            .WithJsonContent(new { History = new[] { new { Id = "abc123" } } });

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        string json;

        using (var client = options.CreateHttpClient())
        {
            client.DefaultRequestHeaders.Add("x-forwarded-for", new[] { "192.168.1.1", "192.168.1.2" });
            client.DefaultRequestHeaders.Add("x-forwarded-proto", new[] { "http", "https" });

            // Act
            json = await client.GetStringAsync("https://public.je-apis.com/consumer/order-history");
        }

        // Assert
        var content = JObject.Parse(json);
        content["History"].First().Value<string>("Id").ShouldBe("abc123");
    }

    [Fact]
    public static async Task Builder_With_Many_Multivalue_Headers_From_Dictionary_To_Match_Intercepts_Request()
    {
        // Arrange
        var headers = new Dictionary<string, ICollection<string>>()
        {
            ["x-forwarded-for"] = new[] { "192.168.1.1", "192.168.1.2" },
            ["x-forwarded-proto"] = new[] { "http", "https" },
        };

        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForHttps()
            .ForHost("public.je-apis.com")
            .ForPath("consumer/order-history")
            .ForRequestHeaders(headers)
            .WithJsonContent(new { History = new[] { new { Id = "abc123" } } });

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        string json;

        using (var client = options.CreateHttpClient())
        {
            client.DefaultRequestHeaders.Add("x-forwarded-for", new[] { "192.168.1.1", "192.168.1.2" });
            client.DefaultRequestHeaders.Add("x-forwarded-proto", new[] { "http", "https" });

            // Act
            json = await client.GetStringAsync("https://public.je-apis.com/consumer/order-history");
        }

        // Assert
        var content = JObject.Parse(json);
        content["History"].First().Value<string>("Id").ShouldBe("abc123");
    }

    [Fact]
    public static async Task Builder_With_Header_To_Match_Does_Not_Intercept_Request_If_Header_Missing()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForHttps()
            .ForHost("public.je-apis.com")
            .ForPath("consumer/order-history")
            .ForRequestHeader("Accept-Tenant", "uk")
            .WithJsonContent(new { History = new[] { new { Id = "abc123" } } });

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        using var client = options.CreateHttpClient();

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => client.GetStringAsync("https://public.je-apis.com/consumer/order-history"));

        // Assert
        exception.Message.ShouldStartWith("No HTTP response is configured for ");
    }

    [Fact]
    public static async Task Builder_With_Header_To_Match_Does_Not_Intercept_Request_If_Header_Not_Equal()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForHttps()
            .ForHost("public.je-apis.com")
            .ForPath("consumer/order-history")
            .ForRequestHeader("Accept-Tenant", "uk")
            .WithJsonContent(new { History = new[] { new { Id = "abc123" } } });

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        using var client = options.CreateHttpClient();
        client.DefaultRequestHeaders.Add("Accept-Tenant", "ie");

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => client.GetStringAsync("https://public.je-apis.com/consumer/order-history"));

        // Assert
        exception.Message.ShouldStartWith("No HTTP response is configured for ");
    }

    [Fact]
    public static async Task Builder_With_Header_To_Match_Does_Not_Intercept_Request_If_Any_Header_Missing()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForHttps()
            .ForHost("public.je-apis.com")
            .ForPath("consumer/order-history")
            .ForRequestHeader("Accept-Tenant", string.Empty)
            .ForRequestHeader("Accept-Tenant", "uk")
            .ForRequestHeader("Authorization", "basic my-key")
            .WithJsonContent(new { History = new[] { new { Id = "abc123" } } });

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        using var client = options.CreateHttpClient();
        client.DefaultRequestHeaders.Add("Accept-Tenant", "uk");

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => client.GetStringAsync("https://public.je-apis.com/consumer/order-history"));

        // Assert
        exception.Message.ShouldStartWith("No HTTP response is configured for ");
    }

    [Fact]
    public static async Task Builder_With_Header_To_Match_Does_Not_Intercept_Request_If_Any_Header_Not_Equal()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForGet()
            .ForHttps()
            .ForHost("public.je-apis.com")
            .ForPath("consumer/order-history")
            .ForRequestHeader("Accept-Tenant", "uk")
            .ForRequestHeader("Authorization", "basic my-key")
            .WithJsonContent(new { History = new[] { new { Id = "abc123" } } });

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        using var client = options.CreateHttpClient();
        client.DefaultRequestHeaders.Add("Accept-Tenant", "uk");
        client.DefaultRequestHeaders.Add("Authorization", "basic my-other-key");

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => client.GetStringAsync("https://public.je-apis.com/consumer/order-history"));

        // Assert
        exception.Message.ShouldStartWith("No HTTP response is configured for ");
    }

    [Fact]
    public static void ForRequestHeader_Validates_Parameters()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder();
        string name = "name";
        var values = Array.Empty<string>();

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => builder.ForRequestHeader(null, string.Empty), "name");
        Should.Throw<ArgumentNullException>(() => builder.ForRequestHeader(null, values), "name");
        Should.Throw<ArgumentNullException>(() => builder.ForRequestHeader(null, (IEnumerable<string>)values), "name");
        Should.Throw<ArgumentNullException>(() => builder.ForRequestHeader(name, (string[])null), "values");
        Should.Throw<ArgumentNullException>(() => builder.ForRequestHeader(name, (IEnumerable<string>)null), "values");
    }

    [Fact]
    public static void ForRequestHeaders_Validates_Parameters()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder();
        IDictionary<string, string> headersOfString = null;
        IDictionary<string, ICollection<string>> headersOfStrings = null;
        Func<IEnumerable<KeyValuePair<string, ICollection<string>>>> headerFactory = null;

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => builder.ForRequestHeaders(headersOfString), "headers");
        Should.Throw<ArgumentNullException>(() => builder.ForRequestHeaders(headersOfStrings), "headers");
        Should.Throw<ArgumentNullException>(() => builder.ForRequestHeaders(headerFactory), "headerFactory");
    }

    [Fact]
    public static void WithContentHeader_Validates_Parameters()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder();
        string name = "name";
        var values = Array.Empty<string>();

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => builder.WithContentHeader(null, string.Empty), "name");
        Should.Throw<ArgumentNullException>(() => builder.WithContentHeader(null, values), "name");
        Should.Throw<ArgumentNullException>(() => builder.WithContentHeader(null, (IEnumerable<string>)values), "name");
        Should.Throw<ArgumentNullException>(() => builder.WithContentHeader(name, (string[])null), "values");
        Should.Throw<ArgumentNullException>(() => builder.WithContentHeader(name, (IEnumerable<string>)null), "values");
    }

    [Fact]
    public static void WithContentHeaders_Validates_Parameters()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder();
        IDictionary<string, string> headers = null;
        IDictionary<string, ICollection<string>> headerValues = null;
        Func<IEnumerable<KeyValuePair<string, ICollection<string>>>> headerFactory = null;

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => builder.WithContentHeaders(headers), "headers");
        Should.Throw<ArgumentNullException>(() => builder.WithContentHeaders(headerValues), "headers");
        Should.Throw<ArgumentNullException>(() => builder.WithContentHeaders(headerFactory), "headerFactory");
    }

    [Fact]
    public static void WithResponseHeader_Validates_Parameters()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder();
        string name = "name";
        var values = Array.Empty<string>();

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => builder.WithResponseHeader(null, string.Empty), "name");
        Should.Throw<ArgumentNullException>(() => builder.WithResponseHeader(null, values), "name");
        Should.Throw<ArgumentNullException>(() => builder.WithResponseHeader(null, (IEnumerable<string>)values), "name");
        Should.Throw<ArgumentNullException>(() => builder.WithResponseHeader(name, (string[])null), "values");
        Should.Throw<ArgumentNullException>(() => builder.WithResponseHeader(name, (IEnumerable<string>)null), "values");
    }

    [Fact]
    public static void WithResponseHeaders_Validates_Parameters()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder();
        IDictionary<string, string> headers = null;
        Func<IEnumerable<KeyValuePair<string, ICollection<string>>>> headerFactory = null;

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => builder.WithResponseHeaders(headers), "headers");
        Should.Throw<ArgumentNullException>(() => builder.WithResponseHeaders(headerFactory), "headerFactory");
    }

    [Fact]
    public static void ForFormContent_Validates_Parameters()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder();
        var parameters = new Dictionary<string, string>();

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => ((HttpRequestInterceptionBuilder)null).ForFormContent(parameters), "builder");
        Should.Throw<ArgumentNullException>(() => builder.ForFormContent(null), "parameters");
    }

    [Fact]
    public static void ForContent_Validates_Parameters()
    {
        // Arrange
        static bool Predicate(HttpContent content) => false;

        // Act and Assert
        Should.Throw<ArgumentNullException>(() => ((HttpRequestInterceptionBuilder)null).ForContent(Predicate), "builder");
    }

    [Fact]
    public static async Task Builder_For_Posted_Encoded_Form_To_Match_Intercepts_Request()
    {
        // Arrange
        // From https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SetQueueAttributes.html
        var expectedForm = new Dictionary<string, string>()
        {
            ["Action"] = "SetQueueAttributes",
            ["Attribute.Name"] = "VisibilityTimeout",
            ["Attribute.Value"] = "35",
            ["Expires"] = "2020-04-18T22:52:43PST",
            ["Version"] = "2012-11-05",
        };

        // Extra parameters to validate matches a subset
        var actualForm = new Dictionary<string, string>()
        {
            ["Foo"] = "Bar",
            ["Action"] = "SetQueueAttributes",
            ["Attribute.Name"] = "VisibilityTimeout",
            ["Attribute.Value"] = "35",
            ["Expires"] = "2020-04-18T22:52:43PST",
            ["Version"] = "2012-11-05",
            ["Fizz"] = "Buzz",
        };

        string expectedXml = @"<SetQueueAttributesResponse>
<ResponseMetadata>
    <RequestId>e5cca473-4fc0-4198-a451-8abb94d02c75</RequestId>
</ResponseMetadata>
</SetQueueAttributesResponse>";

        var builder = new HttpRequestInterceptionBuilder()
            .ForHttps()
            .ForPost()
            .ForHost("sqs.us-east-2.amazonaws.com")
            .ForPath("/123456789012/MyQueue/")
            .ForFormContent(expectedForm)
            .Responds()
            .WithContent(expectedXml);

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        string actualXml;

        using (var client = options.CreateHttpClient())
        {
            // Act
            using var content = new FormUrlEncodedContent(actualForm);
            actualXml = await HttpAssert.SendAsync(
                options,
                HttpMethod.Post,
                "https://sqs.us-east-2.amazonaws.com/123456789012/MyQueue/",
                content);
        }

        // Assert
        actualXml.ShouldBe(expectedXml);
    }

    [Fact]
    public static async Task Builder_For_Posted_Encoded_Form_To_Match_With_Missing_Parameter_Does_Not_Intercept_Request()
    {
        // Arrange
        var actualForm = new Dictionary<string, string>()
        {
            ["Foo"] = "Bar",
        };

        var expectedForm = new Dictionary<string, string>()
        {
            ["Bar"] = "Foo",
        };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl("https://test.local/form")
            .ForPost()
            .ForFormContent(expectedForm)
            .Responds()
            .WithContent("<xml/>");

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        using var client = options.CreateHttpClient();
        using var content = new FormUrlEncodedContent(actualForm);

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => HttpAssert.SendAsync(options, HttpMethod.Post, "https://test.local/form", content));

        // Assert
        exception.Message.ShouldStartWith("No HTTP response is configured for ");
    }

    [Fact]
    public static async Task Builder_For_Posted_Encoded_Form_To_Match_With_Missing_Parameters_Does_Not_Intercept_Request()
    {
        // Arrange
        var actualForm = new Dictionary<string, string>()
        {
            ["Foo"] = "Bar",
        };

        var expectedForm = new Dictionary<string, string>()
        {
            ["Foo"] = "Bar",
            ["Baz"] = "Qux",
        };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl("https://test.local/form")
            .ForPost()
            .ForFormContent(expectedForm)
            .Responds()
            .WithContent("<xml/>");

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        using var client = options.CreateHttpClient();
        using var content = new FormUrlEncodedContent(actualForm);

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => HttpAssert.SendAsync(options, HttpMethod.Post, "https://test.local/form", content));

        // Assert
        exception.Message.ShouldStartWith("No HTTP response is configured for ");
    }

    [Fact]
    public static async Task Builder_For_Posted_Encoded_Form_To_Match_With_No_Parameters_Does_Not_Intercept_Request()
    {
        // Arrange
        var actualForm = new Dictionary<string, string>();

        var expectedForm = new Dictionary<string, string>()
        {
            ["Foo"] = "Bar",
        };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl("https://test.local/form")
            .ForPost()
            .ForFormContent(expectedForm)
            .Responds()
            .WithContent("<xml/>");

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        using var client = options.CreateHttpClient();
        using var content = new FormUrlEncodedContent(actualForm);

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => HttpAssert.SendAsync(options, HttpMethod.Post, "https://test.local/form", content));

        // Assert
        exception.Message.ShouldStartWith("No HTTP response is configured for ");
    }

    [Fact]
    public static async Task Builder_For_Posted_Encoded_Form_To_Match_With_Incorrect_Parameter_Does_Not_Intercept_Request()
    {
        // Arrange
        var actualForm = new Dictionary<string, string>()
        {
            ["Foo"] = "Bar",
            ["Fizz"] = "Buzz",
        };

        var expectedForm = new Dictionary<string, string>()
        {
            ["Foo"] = "Bar",
            ["Fizz"] = "Buzzz",
        };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl("https://test.local/form")
            .ForPost()
            .ForFormContent(expectedForm)
            .Responds()
            .WithContent("<xml/>");

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        using var client = options.CreateHttpClient();
        using var content = new FormUrlEncodedContent(actualForm);

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => HttpAssert.SendAsync(options, HttpMethod.Post, "https://test.local/form", content));

        // Assert
        exception.Message.ShouldStartWith("No HTTP response is configured for ");
    }

    [Fact]
    public static async Task Builder_For_Posted_Encoded_Form_To_Match_With_Incorrect_Parameter_Case_Does_Not_Intercept_Request()
    {
        // Arrange
        var expectedForm = new Dictionary<string, string>()
        {
            ["Foo"] = "Bar",
        };

        var actualForm = new Dictionary<string, string>()
        {
            ["Foo"] = "bar",
        };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl("https://test.local/form")
            .ForPost()
            .ForFormContent(expectedForm)
            .Responds()
            .WithContent("<xml/>");

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        using var client = options.CreateHttpClient();
        using var content = new FormUrlEncodedContent(actualForm);

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => HttpAssert.SendAsync(options, HttpMethod.Post, "https://test.local/form", content));

        // Assert
        exception.Message.ShouldStartWith("No HTTP response is configured for ");
    }

    [Fact]
    public static async Task Builder_For_Posted_Encoded_Form_To_Match_Does_Not_Intercept_Request_If_Not_Url_Encoded_Form_String()
    {
        // Arrange
        var expectedForm = new Dictionary<string, string>()
        {
            ["Foo"] = "Bar",
        };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl("https://test.local/post")
            .ForPost()
            .ForFormContent(expectedForm)
            .Responds()
            .WithContent("<xml/>");

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => HttpAssert.PostAsync(options, "https://test.local/post", new { message = "Hello" }));

        // Assert
        exception.Message.ShouldStartWith("No HTTP response is configured for ");
    }

    [Fact]
    public static async Task Builder_For_Posted_Encoded_Form_To_Match_Does_Not_Intercept_Request_If_Not_Url_Encoded_Form_Bytes()
    {
        // Arrange
        var expectedForm = new Dictionary<string, string>()
        {
            ["Foo"] = "Bar",
        };

        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl("https://test.local/post")
            .ForPost()
            .ForFormContent(expectedForm)
            .Responds()
            .WithContent("<xml/>");

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        using var stream = new MemoryStream();
        using var content = new StreamContent(stream);

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => HttpAssert.SendAsync(options, HttpMethod.Post, "https://test.local/post", content));

        // Assert
        exception.Message.ShouldStartWith("No HTTP response is configured for ");
    }

    [Fact]
    public static async Task Builder_ForContent_Clears_Matcher_If_Null_Synchronous()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl("https://test.local/post")
            .ForPost()
            .ForContent((content) => false)
            .ForContent((Predicate<HttpContent>)null)
            .Responds()
            .WithContent("{}");

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        // Act
        string actual = await HttpAssert.PostAsync(
            options,
            "https://test.local/post",
            new { });

        // Assert
        actual.ShouldBe("{}");
    }

    [Fact]
    public static async Task Builder_ForContent_Clears_Matcher_If_Null_Asynchronous()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl("https://test.local/post")
            .ForPost()
            .ForContent((content) => Task.FromResult(false))
            .ForContent(null)
            .Responds()
            .WithContent("{}");

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        // Act
        string actual = await HttpAssert.PostAsync(
            options,
            "https://test.local/post",
            new { });

        // Assert
        actual.ShouldBe("{}");
    }

    [Fact]
    public static async Task Builder_Deregisters_With_Custom_Matcher()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .Requests().For((_) => Task.FromResult(true));

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        // Act
        options.Deregister(builder);

        // Assert
        await Should.ThrowAsync<HttpRequestNotInterceptedException>(() => HttpAssert.GetAsync(options, "https://google.com/"));
    }

    [Fact]
    public static void Builder_Deregistration_With_Custom_Matcher_Throws_If_Builder_Mutated()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .Requests().For((_) => Task.FromResult(true));

        var options = new HttpClientInterceptorOptions()
            .Register(builder);

        builder.ForAnyHost();

        // Act and Assert
        var exception = Should.Throw<InvalidOperationException>(() => options.Deregister(builder));
        exception.Message.ShouldBe("Failed to deregister HTTP request interception. The builder has not been used to register an HTTP request or has been mutated since it was registered.");
    }

    [Fact]
    public static void Builder_Deregistration_Throws_If_Builder_Not_Registered()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder();
        var options = new HttpClientInterceptorOptions();

        // Act and Assert
        var exception = Should.Throw<InvalidOperationException>(() => options.Deregister(builder));
        exception.Message.ShouldBe("Failed to deregister HTTP request interception. The builder has not been used to register an HTTP request or has been mutated since it was registered.");
    }

    [Fact]
    public static async Task Builder_For_Posted_Json_To_Match_Intercepts_Request()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .ForUrl("https://test.local/post")
            .ForPost()
            .ForContent((content) => content.ReadAsStringAsync().Result == @"{""message"":""Hello, Alice""}")
            .Responds()
            .WithJsonContent(new { message = "Hi Bob!" });

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        // Act
        string actual = await HttpAssert.PostAsync(
            options,
            "https://test.local/post",
            new { message = "Hello, Alice" });

        // Assert
        actual.ShouldBe(@"{""message"":""Hi Bob!""}");
    }

    [Fact]
    public static async Task Builder_For_Posted_Json_To_Match_Intercepts_Requests_That_Differ_By_Content_Only()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration();

        new HttpRequestInterceptionBuilder()
            .ForUrl("https://chat.local/send")
            .ForPost()
            .ForContent(async (content) => await content.ReadAsStringAsync() == @"{""message"":""Who are you?""}")
            .Responds()
            .WithJsonContent(new { message = "My name is Alice, what's your name?" })
            .RegisterWith(options);

        new HttpRequestInterceptionBuilder()
            .ForUrl("https://chat.local/send")
            .ForPost()
            .ForContent(async (content) => await content.ReadAsStringAsync() == @"{""message"":""Hello, Alice - I'm Bob.""}")
            .Responds()
            .WithJsonContent(new { message = "Hi Bob!" })
            .RegisterWith(options);

        // Act
        string actual = await HttpAssert.PostAsync(
            options,
            "https://chat.local/send",
            new { message = "Who are you?" });

        // Assert
        actual.ShouldBe(@"{""message"":""My name is Alice, what\u0027s your name?""}");

        // Act
        actual = await HttpAssert.PostAsync(
            options,
            "https://chat.local/send",
            new { message = "Hello, Alice - I'm Bob." });

        // Assert
        actual.ShouldBe(@"{""message"":""Hi Bob!""}");
    }

    [Fact]
    public static void Builder_For_Throws_ArgumentNullException_If_Custom_Matching_Delegate_Is_Null()
    {
        // Arrange
        HttpRequestInterceptionBuilder InterceptionBuilder() =>
            new HttpRequestInterceptionBuilder()
                .Requests()
                .ForHttps()
                .ForHost("api.github.com")
                .ForPath("orgs/justeat")
                .For((Predicate<HttpRequestMessage>[])null)
                .Responds()
                .WithStatus(HttpStatusCode.OK);

        // Act & Assert
        Should.Throw<ArgumentNullException>(InterceptionBuilder);
    }

    [Fact]
    public static void Builder_For_Throws_ArgumentNullException_If_Async_Custom_Matching_Delegate_Is_Null()
    {
        // Arrange
        HttpRequestInterceptionBuilder InterceptionBuilder() =>
            new HttpRequestInterceptionBuilder()
                .Requests()
                .ForHttps()
                .ForHost("api.github.com")
                .ForPath("orgs/justeat")
                .For((Func<HttpRequestMessage, Task<bool>>[])null)
                .Responds()
                .WithStatus(HttpStatusCode.OK);

        // Act & Assert
        Should.Throw<ArgumentNullException>(InterceptionBuilder);
    }

    [Fact]
    public static void Builder_For_Throws_InvalidOperationException_If_At_Least_One_Async_Custom_Matching_Delegate_Is_Null()
    {
        // Arrange
        HttpRequestInterceptionBuilder InterceptionBuilder() =>
            new HttpRequestInterceptionBuilder()
                .Requests()
                .ForHttps()
                .ForHost("api.github.com")
                .ForPath("orgs/justeat")
                .For(_ => Task.FromResult(true), null)
                .Responds()
                .WithStatus(HttpStatusCode.OK);

        // Act & Assert
        Should.Throw<InvalidOperationException>(InterceptionBuilder);
    }

    [Fact]
    public static void Builder_For_Throws_InvalidOperationException_If_At_Least_One_Custom_Matching_Delegate_Is_Null()
    {
        // Arrange
        HttpRequestInterceptionBuilder InterceptionBuilder() =>
            new HttpRequestInterceptionBuilder()
                .Requests()
                .ForHttps()
                .ForHost("api.github.com")
                .ForPath("orgs/justeat")
                .For(_ => true, null)
                .Responds()
                .WithStatus(HttpStatusCode.OK);

        // Act & Assert
        Should.Throw<InvalidOperationException>(InterceptionBuilder);
    }

    [Fact]
    public static async Task Use_Asynchronous_Custom_Request_Matching()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .Requests().For(async (request) => await Task.FromResult(request.RequestUri.Host == "google.com"))
            .Responds().WithContent(@"<!DOCTYPE html><html dir=""ltr"" lang=""en""><head><title>Google Search</title></head></html>");

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        using var client = options.CreateHttpClient();

        // Act and Assert
        (await client.GetStringAsync("https://google.com/")).ShouldContain("Google Search");
        (await client.GetStringAsync("https://google.com/search")).ShouldContain("Google Search");
        (await client.GetStringAsync("https://google.com/search?q=foo")).ShouldContain("Google Search");
    }

    [Fact]
    public static async Task Can_Deregister_Custom_Matcher()
    {
        // Arrange
        var builder = new HttpRequestInterceptionBuilder()
            .Requests().For(async (request) => await Task.FromResult(request.RequestUri.Host == "google.com"))
            .Responds().WithContent(@"<!DOCTYPE html><html dir=""ltr"" lang=""en""><head><title>Bing Search</title></head></html>")
            .Requests().For((Predicate<HttpRequestMessage>)null);

        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration()
            .Register(builder);

        using var client = options.CreateHttpClient();

        // Act and Assert
        await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => client.GetStringAsync("https://google.com/"));
    }

    [Fact]
    public static async Task Can_Do_Custom_Matching_Even_Number_Requests_Return_200_Odd_Number_Requests_Return_429()
    {
        // Arrange
        int requestCount = 0;

        bool EvenNumberedRequests(HttpRequestMessage message) => requestCount % 2 == 0;
        bool OddNumberedRequests(HttpRequestMessage message) => requestCount % 2 > 0;
        void IncrementRequestCounter(HttpRequestMessage message) => requestCount++;

        string requestUri = "https://api.github.com/orgs/justeat";

        var builder200 = new HttpRequestInterceptionBuilder()
            .Requests()
            .ForHttps()
            .ForHost("api.github.com")
            .ForPath("orgs/justeat")
            .For(EvenNumberedRequests)
            .WithInterceptionCallback(IncrementRequestCounter)
            .Responds()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonContent(new { id = 1516790, login = "justeat", url = requestUri });

        var builder429 = new HttpRequestInterceptionBuilder()
            .Requests()
            .ForHttps()
            .ForHost("api.github.com")
            .ForPath("orgs/justeat")
            .For(OddNumberedRequests)
            .WithInterceptionCallback(IncrementRequestCounter)
            .Responds()
            .WithStatus(HttpStatusCode.TooManyRequests)
            .WithJsonContent(new { error = "Too many requests" });

        var options = new HttpClientInterceptorOptions()
            .Register(builder200)
            .Register(builder429);

        using var client = options.CreateHttpClient();

        // Act & Assert (First request)
        HttpResponseMessage firstResponse = await client.GetAsync(requestUri);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act & Assert (Second request)
        HttpResponseMessage secondResponse = await client.GetAsync(requestUri);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        // Act & Assert (Third request)
        HttpResponseMessage thirdResponse = await client.GetAsync(requestUri);
        thirdResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act & Assert (Fourth request)
        HttpResponseMessage fourthResponse = await client.GetAsync(requestUri);
        fourthResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
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
