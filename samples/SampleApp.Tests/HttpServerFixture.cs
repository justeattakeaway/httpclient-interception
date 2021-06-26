// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.IO;
using JustEat.HttpClientInterception;
using MartinCostello.Logging.XUnit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace SampleApp.Tests
{
    public class HttpServerFixture : WebApplicationFactory<Startup>, ITestOutputHelperAccessor
    {
        public HttpServerFixture()
            : base()
        {
            Interceptor = new HttpClientInterceptorOptions()
                .ThrowsOnMissingRegistration();

            // HACK Force HTTP server startup
            using (CreateDefaultClient())
            {
            }
        }

        public HttpClientInterceptorOptions Interceptor { get; }

        public ITestOutputHelper OutputHelper { get; set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Configure filter that makes JustEat.HttpClientInterception available
            builder.ConfigureServices(
                (services) => services.AddSingleton<IHttpMessageHandlerBuilderFilter, HttpClientInterceptionFilter>(
                    (_) => new HttpClientInterceptionFilter(Interceptor)));

            // Add the test configuration file to override the application configuration
            string directory = Path.GetDirectoryName(typeof(HttpServerFixture).Assembly.Location);
            string fullPath = Path.Combine(directory, "testsettings.json");

            builder.ConfigureAppConfiguration(
                (_, config) => config.AddJsonFile(fullPath));

            // Route logs to xunit test output
            builder.ConfigureLogging((p) => p.AddXUnit(this));
        }
    }
}
