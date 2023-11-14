// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using JustEat.HttpClientInterception;
using Microsoft.Extensions.Http;

namespace SampleApp.Tests;

// begin-snippet: interception-filter

/// <summary>
/// A class that registers an intercepting HTTP message handler at the end of
/// the message handler pipeline when an <see cref="HttpClient"/> is created.
/// </summary>
public sealed class HttpClientInterceptionFilter(HttpClientInterceptorOptions options) : IHttpMessageHandlerBuilderFilter
{
    /// <inheritdoc/>
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return (builder) =>
        {
            // Run any actions the application has configured for itself
            next(builder);

            // Add the interceptor as the last message handler
            builder.AdditionalHandlers.Add(options.CreateHttpMessageHandler());
        };
    }
}

// end-snippet
