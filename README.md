# HttpClient Interception

A .NET Standard library for intercepting server-side HTTP dependencies.

## Introduction

This library provides functionality for intercepting HTTP requests made using the `HttpClient` class in code targeting .NET Standard  1.3 and later and .NET Framework 4.6.1 and later.

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

var builder = new HttpRequestInterceptionBuilder()
    .ForHost("public.je-apis.com")
    .ForPath("terms")
    .WithJsonContent(new { Id = 1, Link = "https://www.just-eat.co.uk/privacy-policy" });

var options = new HttpClientInterceptorOptions()
    .Register(builder);

var client = options.CreateHttpClient();

// The value of json will be "{\"Id\":1,\"Link\":\"https://www.just-eat.co.uk/privacy-policy\"}"
var json = await client.GetStringAsync("http://public.je-apis.com/terms");
```

#### Fault Injection

Below is a minimal example of intercepting a request to inject an HTTP fault:

```csharp
// using JustEat.HttpClientInterception;

var builder = new HttpRequestInterceptionBuilder()
    .ForHost("public.je-apis.com")
    .WithStatus(HttpStatusCode.InternalServerError);

var options = new HttpClientInterceptorOptions()
    .Register(builder);

var client = options.CreateHttpClient();

// Throws an HttpRequestException
await client.GetStringAsync("http://public.je-apis.com");
```

#### Setting Up HttpClient for Dependency Injection

Below is an example of setting up `IServiceCollection` to register `HttpClient` for Dependency Injection in a manner that allows tests to use `HttpClientInterceptorOptions` to intercept HTTP requests:

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

Then in the test project register `HttpClientInterceptorOptions` to provide an implementation of `DelegatingHandler`:

```csharp
var options = new HttpClientInterceptorOptions();

var server = new WebHostBuilder()
    .UseStartup<Startup>()
    .ConfigureServices((services) => services.AddTransient((_) => options.CreateHttpMessageHandler()))
    .Build();

server.Start();
```

### Further Examples

Further examples of using the library can be found by following the links below:

  1. [Example tests](https://github.com/justeat/httpclient-interception/blob/master/tests/HttpClientInterception.Tests/Examples.cs "Sample tests using JustEat.HttpClientInterception")
  1. [Sample application with tests](https://github.com/justeat/httpclient-interception/blob/master/samples/README.md "Sample application that uses JustEat.HttpClientInterception")
  1. This library's [own tests](https://github.com/justeat/httpclient-interception/blob/master/tests/HttpClientInterception.Tests/HttpClientInterceptorOptionsTests.cs "Tests for JustEat.HttpClientInterception itself")

### Benchmarks

Generated with the [Benchmarks project](https://github.com/justeat/httpclient-interception/blob/master/tests/HttpClientInterception.Benchmarks/InterceptionBenchmarks.cs "JustEat.HttpClientInterception benchmark code") using [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet "BenchmarkDotNet on GitHub.com") using commit [b8f6985](https://github.com/justeat/httpclient-interception/commit/b8f69850d4c3005e7e9e12dda6c42d3ba932de13 "Benchmark commit") on 17/09/2017.

``` ini
BenchmarkDotNet=v0.10.9, OS=Windows 10 Redstone 2 (10.0.15063)
Processor=Intel Core i7-6500U CPU 2.50GHz (Skylake), ProcessorCount=4
Frequency=2531249 Hz, Resolution=395.0619 ns, Timer=TSC
.NET Core SDK=2.0.0
  [Host]     : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT
  DefaultJob : .NET Core 2.0.0 (Framework 4.6.00001.0), 64bit RyuJIT
```

|  Method  |   Mean    |   Error   |   StdDev  |   Median  |
|----------|----------:|----------:|----------:|----------:|
| [`GetBytes`](https://github.com/justeat/httpclient-interception/blob/aa96cbeae9397e983b73c09d9a3d2f0b7ed4859c/tests/HttpClientInterception.Benchmarks/InterceptionBenchmarks.cs#L51-L55 "Benchmark using a byte array") |  3.519 μs | 0.0744 μs | 0.2133 μs |  3.463 μs |
|  [`GetHtml`](https://github.com/justeat/httpclient-interception/blob/aa96cbeae9397e983b73c09d9a3d2f0b7ed4859c/tests/HttpClientInterception.Benchmarks/InterceptionBenchmarks.cs#L57-L61 "Benchmark using HTML") |  4.365 μs | 0.1141 μs | 0.3238 μs |  4.200 μs |
|  [`GetJson`](https://github.com/justeat/httpclient-interception/blob/aa96cbeae9397e983b73c09d9a3d2f0b7ed4859c/tests/HttpClientInterception.Benchmarks/InterceptionBenchmarks.cs#L63-L68 "Benchmark using JSON") | 11.150 μs | 0.2215 μs | 0.5758 μs | 10.892 μs |
|    [`Refit`](https://github.com/justeat/httpclient-interception/blob/aa96cbeae9397e983b73c09d9a3d2f0b7ed4859c/tests/HttpClientInterception.Benchmarks/InterceptionBenchmarks.cs#L70-L74 "Benchmark using Refit") | 32.203 μs | 0.3286 μs | 0.2913 μs | 32.176 μs |

## Feedback

Any feedback or issues can be added to the issues for this project in [GitHub](https://github.com/justeat/httpclient-interception/issues "This project's issues on GitHub.com").

## Repository

The repository is hosted in [GitHub](https://github.com/justeat/httpclient-interception "This project on GitHub.com"): https://github.com/justeat/httpclient-interception.git

## Building and Testing

Compiling the library yourself requires Git and the [.NET Core SDK](https://www.microsoft.com/net/download/core "Download the .NET Core SDK") to be installed (version 2.0.0 or later).

To build and test the library locally from a terminal/command-line, run one of the following set of commands:

**Linux/OS X**

```sh
git clone https://github.com/justeat/httpclient-interception.git
cd httpclient-interception
./build.sh  --restore-packages
```

**Windows**

```powershell
git clone https://github.com/justeat/httpclient-interception.git
cd httpclient-interception
.\Build.ps1 -RestorePackages
```

## License

This project is licensed under the [Apache 2.0](http://www.apache.org/licenses/LICENSE-2.0.html "The Apache 2.0 license") license.
