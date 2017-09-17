// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using SampleApp.Handlers;

namespace SampleApp.Extensions
{
    public static class HttpClientExtensions
    {
        public static IServiceCollection AddHttpClient(this IServiceCollection services)
        {
            // Register some message handlers
            services.AddTransient<DelegatingHandler, TimingHandler>();
            services.AddTransient<DelegatingHandler, AddRequestIdHandler>();

            return services.AddTransient(
                (p) =>
                {
                    // Create a handler that makes actual HTTP calls
                    HttpMessageHandler handler = new HttpClientHandler();

                    // Have any delegating handlers been registered?
                    var handlers = p.GetServices<DelegatingHandler>().ToList();

                    if (handlers.Count > 0)
                    {
                        // Attach the initial handler to the first delegating handler
                        DelegatingHandler previous = handlers.First();
                        previous.InnerHandler = handler;

                        // Chain any remaining handlers to each other
                        foreach (DelegatingHandler next in handlers.Skip(1))
                        {
                            next.InnerHandler = previous;
                            previous = next;
                        }

                        // Replace the initial handler with the last delegating handler
                        handler = previous;
                    }

                    return new HttpClient(handler);
                });
        }
    }
}
