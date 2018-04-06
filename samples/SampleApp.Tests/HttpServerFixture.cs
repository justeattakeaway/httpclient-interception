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

            ServerUrl = "http://localhost:5050";

            // Self-host the application, configuring the use of HTTP interception
            _server = new WebHostBuilder()
                .UseStartup<TestStartup>()
                .ConfigureServices((p) => p.AddTransient((_) => _interceptor.CreateHttpMessageHandler()))
                .UseKestrel()
                .UseUrls(ServerUrl)
                .Build();

            _server.Start();
        }

        ~HttpServerFixture() => Dispose(false);

        public HttpClientInterceptorOptions Interceptor => _interceptor;

        public string ServerUrl { get; }

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
