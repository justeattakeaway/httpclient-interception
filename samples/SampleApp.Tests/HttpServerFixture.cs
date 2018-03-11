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
                // If using multiple named clients, you must register an intercepting handler for each one
                services.AddHttpClient("github")
                        .AddHttpMessageHandler(() => _interceptor.CreateHttpMessageHandler());
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
