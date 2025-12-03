# HttpClient Interception Samples

## Introduction

This folder contains a sample ASP.NET Core application that uses the `JustEat.HttpClientInterception` NuGet package for testing its dependencies on other APIs.

## Application

The application (`/samples/SampleApp`) exposes a single HTTP resource at `GET api/repos`.
This resource consumes the [GitHub v3 API](https://developer.github.com/v3/) and returns the names of the public repositories belonging to the configured user/organisation as a JSON array of strings. For example:

```json
[
  "JustBehave",
  "JustSaying",
  "JustEat.RecruitmentTest"
]
```

To enable use of `JustEat.HttpClientInterception` for testing, the application has a small number of minor changes:

1. An [extension method](https://github.com/justeattakeaway/httpclient-interception/blob/63e27420c35f66bd4953184586fa10a4762b4bca/samples/SampleApp/Extensions/HttpClientExtensions.cs#L18-L28) that registers `HttpClient` for use with [Refit](https://github.com/reactiveui/refit) to call the GitHub API.
1. Dependencies for the GitHub API are [registered](https://github.com/justeattakeaway/httpclient-interception/blob/63e27420c35f66bd4953184586fa10a4762b4bca/samples/SampleApp/Startup.cs#L24) in the `Startup` class.
1. `IGitHub` is injected into the [constructor of `ReposController`](https://github.com/justeattakeaway/httpclient-interception/blob/63e27420c35f66bd4953184586fa10a4762b4bca/samples/SampleApp/Controllers/ReposController.cs#L18)

## Tests

The tests (`/samples/SampleApp.Tests`) self-host the application using [Kestrel](https://github.com/aspnet/KestrelHttpServer) so that the application can be tested in a [black-box](https://en.wikipedia.org/wiki/Black-box_testing) manner by performing HTTP requests against the API's `GET` resource.

This is enabled by:

1. An xunit [collection fixture](https://github.com/justeattakeaway/httpclient-interception/blob/63e27420c35f66bd4953184586fa10a4762b4bca/samples/SampleApp.Tests/HttpServerCollection.cs#L8-L9) that [self-hosts the server](https://github.com/justeattakeaway/httpclient-interception/blob/63e27420c35f66bd4953184586fa10a4762b4bca/samples/SampleApp.Tests/HttpServerFixture.cs#L25-L38) using `WebApplicationFactory<T>`.
1. [Creates a shared `HttpClientInterceptorOptions`](https://github.com/justeattakeaway/httpclient-interception/blob/63e27420c35f66bd4953184586fa10a4762b4bca/samples/SampleApp.Tests/HttpServerFixture.cs#L20) instance which is [registered](https://github.com/justeattakeaway/httpclient-interception/blob/63e27420c35f66bd4953184586fa10a4762b4bca/samples/SampleApp.Tests/HttpServerFixture.cs#L27-L30) to provide a `DelegatingHandler` implementation for use with Dependency Injection via a custom implementation of [`IHttpMessageHandlerBuilderFilter`](https://github.com/justeattakeaway/httpclient-interception/blob/63e27420c35f66bd4953184586fa10a4762b4bca/samples/SampleApp.Tests/HttpServerFixture.cs#L40-L65).
1. Using `JustEat.HttpClientInterception` to intercept HTTP calls to the GitHub API to [test the API resource](https://github.com/justeattakeaway/httpclient-interception/blob/63e27420c35f66bd4953184586fa10a4762b4bca/samples/SampleApp.Tests/ReposTests.cs#L15-L75).
