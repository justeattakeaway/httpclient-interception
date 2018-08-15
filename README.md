# HttpClient Interception

A .NET Standard library for intercepting server-side HTTP dependencies.

[![NuGet version](https://buildstats.info/nuget/JustEat.HttpClientInterception?includePreReleases=false)](http://www.nuget.org/packages/JustEat.HttpClientInterception/)

| | Linux | Windows |
|:-:|:-:|:-:|
| **Build Status** | [![Build status](https://img.shields.io/travis/justeat/httpclient-interception/master.svg)](https://travis-ci.org/justeat/httpclient-interception) | [![Build status](https://img.shields.io/appveyor/ci/justeattech/httpclient-interception/master.svg)](https://ci.appveyor.com/project/justeattech/httpclient-interception) [![codecov](https://codecov.io/gh/justeat/httpclient-interception/branch/master/graph/badge.svg)](https://codecov.io/gh/justeat/httpclient-interception) |
| **Build History** | [![Build history](https://buildstats.info/travisci/chart/justeat/httpclient-interception?branch=master&includeBuildsFromPullRequest=false)](https://travis-ci.org/justeat/httpclient-interception) |  [![Build history](https://buildstats.info/appveyor/chart/justeattech/httpclient-interception?branch=master&includeBuildsFromPullRequest=false)](https://ci.appveyor.com/project/justeattech/httpclient-interception) |

## Introduction

This library provides functionality for intercepting HTTP requests made using the `HttpClient` class in code targeting .NET Standard 1.3 and 2.0 (and later), and .NET Framework 4.6.1 and later.

The primary use-case is for providing stub responses for use in tests for applications, such as an ASP.NET Core application, to drive your functional test scenarios.

The library is based around an implementation of [`DelegatingHandler`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.delegatinghandler "DelegatingHandler documentation"), which can either be used directly as an implementation of `HttpMessageHandler`, or can be provided to instances of `HttpClient`. This also allows it to be registered via Dependency Injection to make it available for use in code under test without the application itself requiring any references to `JustEat.HttpClientInterception` or any custom abstractions of `HttpClient`.

This design means that no HTTP server needs to be hosted to proxy traffic to/from, so does not consume any additional system resources, such as needing to bind a port for HTTP traffic, making it lightweight to use.

### Installation

To install the library from [NuGet](https://www.nuget.org/packages/JustEat.HttpClientInterception/ "JustEat.HttpClientInterception on NuGet.org") using the .NET SDK run:

```
dotnet add package JustEat.HttpClientInterception
```

### Basic Examples

#### Request Interception

Below is a minimal example of intercepting a request to an HTTP API for a JSON resource to return a custom response:

```csharp
// using JustEat.HttpClientInterception;

var options = new HttpClientInterceptorOptions();

var builder = new HttpRequestInterceptionBuilder()
    .Requests()
    .ForHost("public.je-apis.com")
    .ForPath("terms")
    .Responds()
    .WithJsonContent(new { Id = 1, Link = "https://www.just-eat.co.uk/privacy-policy" })
    .RegisterWith(options);

var client = options.CreateHttpClient();

// The value of json will be "{\"Id\":1,\"Link\":\"https://www.just-eat.co.uk/privacy-policy\"}"
var json = await client.GetStringAsync("http://public.je-apis.com/terms");
```

`HttpRequestInterceptionBuilder` objects are mutable, so properties can be freely changed once a particular setup has been registered with an instance of `HttpClientInterceptorOptions` as the state is captured at the point of registration. This allows multiple responses and paths to be configured from a single `HttpRequestInterceptionBuilder` instance where multiple registrations against a common hostname.

#### Fault Injection

Below is a minimal example of intercepting a request to inject an HTTP fault:

```csharp
// using JustEat.HttpClientInterception;

var options = new HttpClientInterceptorOptions();

var builder = new HttpRequestInterceptionBuilder()
    .Requests()
    .ForHost("public.je-apis.com")
    .WithStatus(HttpStatusCode.InternalServerError)
    .RegisterWith(options);

var client = options.CreateHttpClient();

// Throws an HttpRequestException
await client.GetStringAsync("http://public.je-apis.com");
```

#### Registering Request Interception When Using IHttpClientFactory

If you are using [`IHttpClientFactory`](https://github.com/aspnet/HttpClientFactory "ASP.NET Core HttpClientFactory") to register `HttpClient` for Dependency Injection in a .NET Core 2.1 application (or later), you can implement a custom `IHttpMessageHandlerBuilderFilter` to register during test setup, which makes an instance of `HttpClientInterceptorOptions` available to inject an HTTP handler.

A working example of this approach can be found in the [sample application](https://github.com/justeat/httpclient-interception/blob/master/samples/README.md "Sample application that uses JustEat.HttpClientInterception").

```csharp
using Microsoft.Extensions.Http;

public class InterceptionFilter : IHttpMessageHandlerBuilderFilter
{
    private readonly HttpClientInterceptorOptions _options;

    internal InterceptionFilter(HttpClientInterceptorOptions options)
    {
        _options = options;
    }

    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return (builder) =>
        {
            // Run any actions the application has configured for itself
            next(builder);

            // Add the interceptor as the last message handler
            builder.AdditionalHandlers.Add(_options.CreateHttpMessageHandler());
        };
    }
}
```

```csharp
var options = new HttpClientInterceptorOptions();
services.AddSingleton<IHttpMessageHandlerBuilderFilter, InterceptionFilter>(
    (_) => new InterceptionFilter(options));
```

#### Setting Up HttpClient for Dependency Injection Manually

Below is an example of setting up `IServiceCollection` to register `HttpClient` for Dependency Injection in a manner that allows tests to use `HttpClientInterceptorOptions` to intercept HTTP requests. Similar approaches can be used with other IoC containers.

You may wish to consider registering `HttpClient` as a singleton, rather than as transient, if you do not use properties such as `BaseAddress` on instances of `HttpClient`. This allows the same instance to be used throughout the application, which improves performance and resource utilisation under heavy server load. If using a singleton instance, ensure that you manage the lifetime of your message handlers appropriately so they are not disposed of incorrectly and update the registration for your `HttpClient` instance appropriately.

```csharp
services.AddTransient(
    (serviceProvider) =>
    {
        // Create a handler that makes actual HTTP calls
        HttpMessageHandler handler = new HttpClientHandler();

        // Have any delegating handlers been registered?
        var handlers = serviceProvider
            .GetServices<DelegatingHandler>()
            .ToList();

        if (handlers.Count > 0)
        {
            // Attach the initial handler to the first delegating handler
            DelegatingHandler previous = handlers.First();
            previous.InnerHandler = handler;

            // Chain any remaining handlers to each other
            foreach (DelegatingHandler next in handlers.Skip(1))
            {
                next.InnerHandler = previous;
                previous = next;
            }

            // Replace the initial handler with the last delegating handler
            handler = previous;
        }

        // Create the HttpClient using the inner HttpMessageHandler
        return new HttpClient(handler);
    });
```

Then in the test project register `HttpClientInterceptorOptions` to provide an implementation of `DelegatingHandler`. If using a singleton for `HttpClient` as described above, update the registration for the tests appropriately so that the message handler is shared.

```csharp
var options = new HttpClientInterceptorOptions();

var server = new WebHostBuilder()
    .UseStartup<Startup>()
    .ConfigureServices(
        (services) => services.AddTransient((_) => options.CreateHttpMessageHandler()))
    .Build();

server.Start();
```

### Further Examples

Further examples of using the library can be found by following the links below:

  1. [Example tests](https://github.com/justeat/httpclient-interception/blob/master/tests/HttpClientInterception.Tests/Examples.cs "Sample tests using JustEat.HttpClientInterception")
  1. [Sample application with tests](https://github.com/justeat/httpclient-interception/blob/master/samples/README.md "Sample application that uses JustEat.HttpClientInterception")
  1. This library's [own tests](https://github.com/justeat/httpclient-interception/blob/master/tests/HttpClientInterception.Tests/HttpClientInterceptorOptionsTests.cs "Tests for JustEat.HttpClientInterception itself")

### Benchmarks

Generated with the [Benchmarks project](https://github.com/justeat/httpclient-interception/blob/master/tests/HttpClientInterception.Benchmarks/InterceptionBenchmarks.cs "JustEat.HttpClientInterception benchmark code") using [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet "BenchmarkDotNet on GitHub.com") using commit [c09c38b](https://github.com/justeat/httpclient-interception/commit/c09c38bad3ed5db6cfffdbaebeac33c2d286764f "Benchmark commit") on 11/03/2018.

``` ini
BenchmarkDotNet=v0.10.12, OS=Windows 10 Redstone 3 [1709, Fall Creators Update] (10.0.16299.248)
Intel Core i7-6700HQ CPU 2.60GHz (Skylake), 1 CPU, 8 logical cores and 4 physical cores
Frequency=2531248 Hz, Resolution=395.0620 ns, Timer=TSC
.NET Core SDK=2.1.4
  [Host]     : .NET Core 2.0.5 (Framework 4.6.26020.03), 64bit RyuJIT
  DefaultJob : .NET Core 2.0.5 (Framework 4.6.26020.03), 64bit RyuJIT
```

 |    Method |      Mean |     Error |    StdDev |
 |---------- |----------:|----------:|----------:|
 | [`GetBytes`](https://github.com/justeat/httpclient-interception/blob/c09c38bad3ed5db6cfffdbaebeac33c2d286764f/tests/HttpClientInterception.Benchmarks/InterceptionBenchmarks.cs#L73-L77 "Benchmark using a byte array") |  8.310 μs | 0.0916 μs | 0.0857 μs |
 | [`GetHtml`](https://github.com/justeat/httpclient-interception/blob/c09c38bad3ed5db6cfffdbaebeac33c2d286764f/tests/HttpClientInterception.Benchmarks/InterceptionBenchmarks.cs#L79-L83 "Benchmark using HTML") |  8.476 μs | 0.0462 μs | 0.0361 μs |
 | [`GetJson`](https://github.com/justeat/httpclient-interception/blob/c09c38bad3ed5db6cfffdbaebeac33c2d286764f/tests/HttpClientInterception.Benchmarks/InterceptionBenchmarks.cs#L85-L90 "Benchmark using JSON") | 17.519 μs | 0.2440 μs | 0.2282 μs |
 | [`GetStream`](https://github.com/justeat/httpclient-interception/blob/c09c38bad3ed5db6cfffdbaebeac33c2d286764f/tests/HttpClientInterception.Benchmarks/InterceptionBenchmarks.cs#L92-L98 "Benchmark using a stream") | 72.442 μs | 0.9177 μs | 0.8135 μs |
 | [`Refit`](https://github.com/justeat/httpclient-interception/blob/c09c38bad3ed5db6cfffdbaebeac33c2d286764f/tests/HttpClientInterception.Benchmarks/InterceptionBenchmarks.cs#L100-L104 "Benchmark using Refit") | 46.388 μs | 0.4742 μs | 0.4436 μs |

## Feedback

Any feedback or issues can be added to the issues for this project in [GitHub](https://github.com/justeat/httpclient-interception/issues "This project's issues on GitHub.com").

## Repository

The repository is hosted in [GitHub](https://github.com/justeat/httpclient-interception "This project on GitHub.com"): https://github.com/justeat/httpclient-interception.git

## Building and Testing

Compiling the library yourself requires Git and the [.NET Core SDK](https://www.microsoft.com/net/download/core "Download the .NET Core SDK") to be installed (version 2.1.400 or later).

To build and test the library locally from a terminal/command-line, run one of the following set of commands:

**Linux/macOS**

```sh
git clone https://github.com/justeat/httpclient-interception.git
cd httpclient-interception
./build.sh
```

**Windows**

```powershell
git clone https://github.com/justeat/httpclient-interception.git
cd httpclient-interception
.\Build.ps1
```

## License

This project is licensed under the [Apache 2.0](http://www.apache.org/licenses/LICENSE-2.0.html "The Apache 2.0 license") license.
