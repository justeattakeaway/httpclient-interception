// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using SampleApp.Handlers;
using SampleApp.Services;

namespace SampleApp.Extensions
{
    public static class HttpClientExtensions
    {
        public static IHttpClientBuilder AddHttpClients(this IServiceCollection services)
        {
            // Register a Refit-based typed client for use in the controller, which
            // configures the HttpClient with the appropriate base URL and HTTP request
            // headers. It also adds two custom delegating handlers.
            return services
                .AddHttpClient<IGitHub>()
                .AddTypedClient(AddGitHub)
                .AddHttpMessageHandler(() => new AddRequestIdHandler())
                .AddHttpMessageHandler(() => new TimingHandler());
        }

        private static IGitHub AddGitHub(HttpClient client, IServiceProvider provider)
        {
            IConfiguration configuration = provider.GetRequiredService<IConfiguration>();

            client.BaseAddress = new Uri("https://api.github.com");

            string productName = configuration["UserAgent"];
            string productVersion = typeof(StartupBase).GetTypeInfo().Assembly.GetName().Version.ToString(3);

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(productName, productVersion));

            return RestService.For<IGitHub>(client);
        }
    }
}
