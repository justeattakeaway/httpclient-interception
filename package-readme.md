# HttpClient Interception

A .NET Standard library for intercepting server-side HTTP dependencies.

[![NuGet version](https://img.shields.io/nuget/v/JustEat.HttpClientInterception?logo=nuget&label=NuGet&color=blue)](https://www.nuget.org/packages/JustEat.HttpClientInterception/)
[![Build status](https://github.com/justeattakeaway/httpclient-interception/actions/workflows/build.yml/badge.svg?branch=main&event=push)](https://github.com/justeattakeaway/httpclient-interception/actions/workflows/build.yml?query=branch%3Amain+event%3Apush)
[![codecov](https://codecov.io/gh/justeattakeaway/httpclient-interception/branch/main/graph/badge.svg)](https://codecov.io/gh/justeattakeaway/httpclient-interception)

## Introduction

This library provides functionality for intercepting HTTP requests made using the `HttpClient` class in code targeting .NET Standard 2.0 (and later), and .NET Framework 4.7.2.

The primary use-case is for providing stub responses for use in tests for applications, such as an ASP.NET Core application, to drive your functional test scenarios.

The library is based around an implementation of [`DelegatingHandler`](https://learn.microsoft.com/dotnet/api/system.net.http.delegatinghandler "DelegatingHandler documentation"), which can either be used directly as an implementation of `HttpMessageHandler`, or can be provided to instances of `HttpClient`. This also allows it to be registered via Dependency Injection to make it available for use in code under test without the application itself requiring any references to `JustEat.HttpClientInterception` or any custom abstractions of `HttpClient`.

This design means that no HTTP server needs to be hosted to proxy traffic to/from, so does not consume any additional system resources, such as needing to bind a port for HTTP traffic, making it lightweight to use.

## Feedback

Any feedback or issues for this library can be added to the issues in [GitHub](https://github.com/justeattakeaway/httpclient-interception/issues "This package's issues on GitHub.com").

## License

This package is licensed under the [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0.html "The Apache 2.0 license") license.
