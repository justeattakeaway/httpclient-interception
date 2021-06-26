// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using JustEat.HttpClientInterception;
using Microsoft.Extensions.Http;

namespace SampleApp.Tests
{
    // begin-snippet: interception-filter

    /// <summary>
    /// A class that registers an intercepting HTTP message handler at the end of
    /// the message handler pipeline when an <see cref="HttpClient"/> is created.
    /// </summary>
    public sealed class HttpClientInterceptionFilter : IHttpMessageHandlerBuilderFilter
    {
        private readonly HttpClientInterceptorOptions _options;

        public HttpClientInterceptionFilter(HttpClientInterceptorOptions options)
        {
            _options = options;
        }

        /// <inheritdoc/>
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

    // end-snippet
}
