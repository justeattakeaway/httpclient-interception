// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using JustEat.HttpClientInterception;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

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

            void ConfigureInterception(IServiceCollection services)
            {
                services
                    .AddHttpClient("github")
                    .ConfigureHttpMessageHandlerBuilder((builder) =>
                    {
                        // Create the intercepting DelegatingHandler
                        var interceptor = _interceptor.CreateHttpMessageHandler();

                        // Configure it to use the current primary handler of the
                        // HttpMessageHandlerBuilder as its inner handler. This sets things
                        // up so that any other delegating handlers are called before the
                        // interceptor and so that the interceptor is immediately before
                        // the handler that actually makes the HTTP requests.
                        interceptor.InnerHandler = builder.PrimaryHandler;

                        // Replace the primary handler with the intercepting handler
                        builder.PrimaryHandler = interceptor;
                    });
            }

            // Self-host the application, configuring the use of HTTP interception
            _server = new WebHostBuilder()
                .UseStartup<TestStartup>()
                .UseKestrel()
                .UseUrls(ServerUrl)
                .ConfigureServices(ConfigureInterception)
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
    }
}
