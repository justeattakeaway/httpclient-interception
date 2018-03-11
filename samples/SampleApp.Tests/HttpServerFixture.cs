// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using JustEat.HttpClientInterception;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace SampleApp.Tests
{
    public class HttpServerFixture : IDisposable
    {
        private readonly HttpClientInterceptorOptions _interceptor;
        private readonly IWebHost _server;

        private bool _disposed;

        public HttpServerFixture()
        {
            _interceptor = new HttpClientInterceptorOptions() { ThrowOnMissingRegistration = true };

            // Self-host the application, configuring the use of HTTP interception
            _server = new WebHostBuilder()
                .UseStartup<TestStartup>()
                .UseKestrel()
                .UseUrls(ServerUrl)
                .ConfigureServices((services) => services.AddSingleton<IHttpMessageHandlerBuilderFilter, InterceptionFilter>((_) => new InterceptionFilter(_interceptor)))
                .Build();

            _server.Start();
        }

        ~HttpServerFixture() => Dispose(false);

        public HttpClientInterceptorOptions Interceptor => _interceptor;

        public string ServerUrl => "http://localhost:5050";

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _server?.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// A class that registers an intercepting HTTP message handler at the end of
        /// the message handler pipeline when an <see cref="HttpClient"/> is created.
        /// </summary>
        private sealed class InterceptionFilter : IHttpMessageHandlerBuilderFilter
        {
            private readonly HttpClientInterceptorOptions _options;

            internal InterceptionFilter(HttpClientInterceptorOptions options)
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
    }
}
