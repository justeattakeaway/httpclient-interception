# HttpClient Interception Samples

## Introduction

This folder contains a sample ASP.NET Core application that uses the `JustEat.HttpClientInterception` NuGet package for testing its dependencies on other APIs.

## Application

The application (`/samples/SampleApp`) exposes a single HTTP resource at `GET api/repos`.
This resource consumes the [GitHub v3 API](https://developer.github.com/v3/) and returns the names of the public repositories belonging to the configured user/organisation as a JSON array of strings. For example:

```
[
  "JustBehave",
  "JustSaying",
  "JustEat.RecruitmentTest"
]
```

To enable use of `JustEat.HttpClientInterception` for testing, the application has a small number of minor changes:
  1. An [extension method](https://github.com/justeat/httpclient-interception/blob/3cfa66ffc6c5f1812e65d6ad106aecf7ffe640cd/samples/SampleApp/Extensions/HttpClientExtensions.cs#L18-L31) that registers `HttpClient` for use with [Refit](https://github.com/paulcbetts/refit) to call the GitHub API.
  1. Dependencies for the GitHub API are [registered](https://github.com/justeat/httpclient-interception/blob/3cfa66ffc6c5f1812e65d6ad106aecf7ffe640cd/samples/SampleApp/StartupBase.cs#L20) in the `StartupBase` class.
  1. `IGitHub` is injected into the [constructor of `ReposController`](https://github.com/justeat/httpclient-interception/blob/3cfa66ffc6c5f1812e65d6ad106aecf7ffe640cd/samples/SampleApp/Controllers/ReposController.cs#L18)

## Tests

The tests (`/samples/SampleApp.Tests`) self-host the application using [Kestrel](https://github.com/aspnet/KestrelHttpServer) so that the application can be tested in a [black-box](https://en.wikipedia.org/wiki/Black-box_testing) manner by performing HTTP requests against the API's `GET` resource.

This is enabled by:
  1. An xunit [collection fixture](https://github.com/justeat/httpclient-interception/blob/3cfa66ffc6c5f1812e65d6ad106aecf7ffe640cd/samples/SampleApp.Tests/HttpServerCollection.cs#L8-L9) that [self-hosts the server](https://github.com/justeat/httpclient-interception/blob/3cfa66ffc6c5f1812e65d6ad106aecf7ffe640cd/samples/SampleApp.Tests/HttpServerFixture.cs#L43-L49) at `http://localhost:5050`.
  1. [Creates a shared `HttpClientInterceptorOptions`](https://github.com/justeat/httpclient-interception/blob/42af4e871ffe7d39f7caade615aafbeaad283f26/samples/SampleApp.Tests/HttpServerFixture.cs#L20) instance which is [registered](https://github.com/justeat/httpclient-interception/blob/3cfa66ffc6c5f1812e65d6ad106aecf7ffe640cd/samples/SampleApp.Tests/HttpServerFixture.cs#L48) to provide a `DelegatingHandler` implementation for use with Dependency Injection.
  1. Using `JustEat.HttpClientInterception` to intercept HTTP calls to the GitHub API to [test the API resource](https://github.com/justeat/httpclient-interception/blob/42af4e871ffe7d39f7caade615aafbeaad283f26/samples/SampleApp.Tests/ReposTests.cs#L15-L75).
