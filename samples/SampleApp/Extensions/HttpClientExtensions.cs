// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SampleApp.Extensions
{
    public static class HttpClientExtensions
    {
        public static IServiceCollection AddHttpClient(this IServiceCollection services)
        {
            return services.AddTransient(
                (p) =>
                {
                    // Has interception been configured?
                    var handler = p.GetServices<DelegatingHandler>().SingleOrDefault();

                    if (handler != null)
                    {
                        // If so, use HttpClient with interception
                        return new HttpClient(handler);
                    }
                    else
                    {
                        // Otherwise just use a standard HttpClient
                        return new HttpClient();
                    }
                });
        }
    }
}
