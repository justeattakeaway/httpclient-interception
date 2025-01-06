// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using NSubstitute;

namespace JustEat.HttpClientInterception;

public static class InterceptingHttpMessageHandlerTests
{
    [Fact]
    public static void Constructor_Throws_If_Options_Is_Null()
    {
        // Act and Assert
        Should.Throw<ArgumentNullException>(() => new InterceptingHttpMessageHandler(null)).ParamName.ShouldBe("options");
        Should.Throw<ArgumentNullException>(() => new InterceptingHttpMessageHandler(null, Substitute.For<HttpMessageHandler>())).ParamName.ShouldBe("options");
    }

    [Fact]
    public static async Task SendAsync_Throws_If_Registration_Missing_Get()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration();

        using var target = options.CreateHttpClient();

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => target.GetAsync("https://google.com/", TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("No HTTP response is configured for GET https://google.com/.");
    }

    [Fact]
    public static async Task SendAsync_Throws_If_Registration_Missing_Post()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration();

        using var handler = options.CreateHttpMessageHandler();
        using var target = new HttpClient(handler);
        using var content = new StringContent(string.Empty);

        // Act
        var exception = await Should.ThrowAsync<HttpRequestNotInterceptedException>(
            () => target.PostAsync("https://google.com/", content, TestContext.Current.CancellationToken));

        // Assert
        exception.Message.ShouldBe("No HTTP response is configured for POST https://google.com/.");
        exception.Request.ShouldNotBeNull();
        exception.Request.RequestUri.ShouldBe(new Uri("https://google.com/"));
    }

    [Fact]
    public static async Task SendAsync_Calls_Inner_Handler_If_Registration_Missing()
    {
        // Arrange
        var options = new HttpClientInterceptorOptions()
            .RegisterByteArray(HttpMethod.Get, new Uri("https://google.com/foo"), Array.Empty<byte>)
            .RegisterByteArray(HttpMethod.Options, new Uri("http://google.com/foo"), Array.Empty<byte>)
            .RegisterByteArray(HttpMethod.Options, new Uri("https://google.com/FOO"), Array.Empty<byte>);

        options.OnMissingRegistration = (request) => Task.FromResult<HttpResponseMessage>(null);

        using var handler = Substitute.For<HttpMessageHandler>();
        using var expected = new HttpResponseMessage(HttpStatusCode.OK);
        using var request = new HttpRequestMessage(HttpMethod.Options, "https://google.com/foo");

        handler.GetType()
               .GetMethod("SendAsync", BindingFlags.NonPublic | BindingFlags.Instance)
               .Invoke(handler, [request, Arg.Any<CancellationToken>()])
               .Returns(Task.FromResult(expected));

        using var httpClient = options.CreateHttpClient(handler);

        // Act
        var actual = await httpClient.SendAsync(request, CancellationToken.None);

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public static async Task SendAsync_Calls_OnSend_With_RequestMessage()
    {
        // Arrange
        var header = "x-request";
        var requestUrl = "https://google.com/foo";

        var options = new HttpClientInterceptorOptions()
            .RegisterByteArray(HttpMethod.Get, new Uri(requestUrl), Array.Empty<byte>);

        int expected = 7;
        int actual = 0;

        var requestIds = new ConcurrentBag<string>();

        options.OnSend = (p) =>
        {
            Interlocked.Increment(ref actual);
            requestIds.Add(p.Headers.GetValues(header).FirstOrDefault());
            return Task.CompletedTask;
        };

        Task GetAsync(int id)
        {
            var headers = new Dictionary<string, string>()
            {
                [header] = id.ToString(CultureInfo.InvariantCulture),
            };

            return HttpAssert.GetAsync(options, requestUrl, headers: headers);
        }

        // Act
        var tasks = Enumerable.Range(0, expected)
            .Select(GetAsync)
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert
        actual.ShouldBe(expected);
        requestIds.Count.ShouldBe(expected);
        requestIds.ShouldBeUnique();
    }

    [Fact]
    public static async Task SendAsync_Calls_OnMissingRegistration_With_RequestMessage()
    {
        // Arrange
        var header = "x-request";
        var requestUrl = "https://google.com/foo";

        var options = new HttpClientInterceptorOptions();

        int expected = 7;
        int actual = 0;

        var requestIds = new ConcurrentBag<string>();

        options.OnMissingRegistration = (p) =>
        {
            Interlocked.Increment(ref actual);
            requestIds.Add(p.Headers.GetValues(header).FirstOrDefault());

            var response = new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent(string.Empty),
            };

            return Task.FromResult(response);
        };

        Task GetAsync(int id)
        {
            var headers = new Dictionary<string, string>()
            {
                [header] = id.ToString(CultureInfo.InvariantCulture),
            };

            return HttpAssert.GetAsync(options, requestUrl, HttpStatusCode.Accepted, headers: headers);
        }

        // Act
        var tasks = Enumerable.Range(0, expected)
            .Select(GetAsync)
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert
        actual.ShouldBe(expected);
        requestIds.Count.ShouldBe(expected);
        requestIds.ShouldBeUnique();
    }
}
