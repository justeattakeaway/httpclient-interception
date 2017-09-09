# HttpClient Interception

A .NET Standard library for intercepting server-side HTTP dependencies.

## Introduction

This library provides functionality for intercepting HTTP requests made using the `HttpClient` class in code targeting .NET Standard  1.3 and later.

The primary use-case is for providing stub responses for use in tests for applications, such as an ASP.NET Core application, to drive your functional test scenarios.

The library is based around an implementation of [`DelegatingHandler`](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.delegatinghandler), which can either be used directly as an implementation of `HttpMessageHandler`, or can be provided to instances of `HttpClient`. This also allows it to be registered via Dependency Injection to make it available for use in code under test without the application itself requiring any references to `JustEat.HttpClientInterception` or any custom abstractions of `HttpClient`.

This design means that no HTTP server needs to be hosted to proxy traffic to/from, so does not consume any additional system resources, such as needing to bind a port for HTTP traffic, making it lightweight to use.

### Installation

To install the library from [NuGet](https://www.nuget.org/packages/JustEat.HttpClientInterception/) using the .NET SDK run:

```
dotnet add package JustEat.HttpClientInterception
```

### Simple Example

Below is a minimal example of intercepting a request to an HTTP API for a JSON resource:

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

### Further Examples

Further examples of using the library can be found by following the links below:

  1. [Example tests](https://github.com/justeat/httpclient-interception/blob/master/tests/HttpClientInterception.Tests/Examples.cs)
  1. [Sample application with tests](https://github.com/justeat/httpclient-interception/tree/master/samples)
  1. This library's [own tests](https://github.com/justeat/httpclient-interception/blob/master/tests/HttpClientInterception.Tests/HttpClientInterceptorOptionsTests.cs)

## Feedback

Any feedback or issues can be added to the issues for this project in [GitHub](https://github.com/justeat/httpclient-interception/issues).

## Repository

The repository is hosted in [GitHub](https://github.com/justeat/httpclient-interception): https://github.com/justeat/httpclient-interception.git

## Building and Testing

Compiling the library yourself requires Git and the [.NET Core SDK](https://www.microsoft.com/net/download/core) to be installed (version 2.0.0 or later).

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

This project is licensed under the [Apache 2.0](http://www.apache.org/licenses/LICENSE-2.0.html) license.
