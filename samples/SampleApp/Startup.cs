// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SampleApp.Extensions;

namespace SampleApp
{
    public class Startup
    {
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClients();

            services
                .AddControllers()
                .AddJsonOptions((p) => p.JsonSerializerOptions.WriteIndented = true);
        }

        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints((endpoints) => endpoints.MapDefaultControllerRoute());
        }
    }
}
