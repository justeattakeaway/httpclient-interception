// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using JustEat.HttpClientInterception;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace SampleApp.Tests
{
    public class HttpServerFixture : WebApplicationTestFixture<TestStartup>
    {
        private readonly HttpClientInterceptorOptions _interceptor;

        public HttpServerFixture()
            : base("samples/SampleApp")
        {
            _interceptor = new HttpClientInterceptorOptions() { ThrowOnMissingRegistration = true };
        }

        public HttpClientInterceptorOptions Interceptor => _interceptor;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(
                (services) => services.AddSingleton<IHttpMessageHandlerBuilderFilter, InterceptionFilter>(
                    (_) => new InterceptionFilter(_interceptor)));
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
