// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.IO;
using System.Net.Http;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Newtonsoft.Json.Linq;
using Refit;

namespace JustEat.HttpClientInterception
{
    [EventPipeProfiler(EventPipeProfile.CpuSampling)]
    [MemoryDiagnoser]
    public class InterceptionBenchmarks
    {
        private readonly HttpClientInterceptorOptions _options;
        private readonly HttpClient _client;
        private readonly IGitHub _serviceNewtonsoftJson;
        private readonly IGitHub _serviceSystemTextJson;

        public InterceptionBenchmarks()
        {
            _options = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            var builder = new HttpRequestInterceptionBuilder();

            builder
                .Requests()
                .ForHttp()
                .ForHost("www.google.co.uk")
                .ForPath("search")
                .ForQuery("q=Just+Eat")
                .Responds()
                .WithMediaType("text/html")
                .WithContent(@"<!DOCTYPE html><html dir=""ltr"" lang=""en""><head><title>Just Eat</title></head></html>")
                .RegisterWith(_options);

            builder
                .Requests()
                .ForHttps()
                .ForHost("files.domain.com")
                .ForPath("setup.exe")
                .ForQuery(string.Empty)
                .Responds()
                .WithMediaType("application/octet-stream")
                .WithContent(() => new byte[] { 0, 1, 2, 3, 4 })
                .RegisterWith(_options);

            builder
                .Requests()
                .ForHttps()
                .ForHost("api.github.com")
                .ForPath("orgs/justeat")
                .ForQuery(string.Empty)
                .Responds()
                .WithMediaType("application/json")
                .WithJsonContent(new { id = 1516790, login = "justeat", url = "https://api.github.com/orgs/justeat" })
                .RegisterWith(_options);

            builder
                .Requests()
                .ForQuery("page=1")
                .Responds()
                .WithContentStream(() => File.OpenRead("organization.json"))
                .RegisterWith(_options);

            var refitSettings = new RefitSettings()
            {
                ContentSerializer = new SystemTextJsonContentSerializer(),
            };

#pragma warning disable CA2000
            _client = _options.CreateHttpClient();
            _serviceNewtonsoftJson = RestService.For<IGitHub>(_options.CreateHttpClient("https://api.github.com"));
            _serviceSystemTextJson = RestService.For<IGitHub>(_options.CreateHttpClient("https://api.github.com"), refitSettings);
#pragma warning restore CA2000
        }

        [Benchmark]
        public async Task GetBytes()
        {
            _ = await _client.GetByteArrayAsync("https://files.domain.com/setup.exe");
        }

        [Benchmark]
        public async Task GetHtml()
        {
            _ = await _client.GetStringAsync("http://www.google.co.uk/search?q=Just+Eat");
        }

        [Benchmark]
        public async Task GetJsonNewtonsoftJson()
        {
            string json = await _client.GetStringAsync("https://api.github.com/orgs/justeat");
            _ = JObject.Parse(json);
        }

        [Benchmark]
        public async Task GetJsonSystemTextJson()
        {
            var stream = await _client.GetStreamAsync("https://api.github.com/orgs/justeat");
            using var document = await JsonDocument.ParseAsync(stream);
        }

        [Benchmark]
        public async Task GetStream()
        {
            using (await _client.GetStreamAsync("https://api.github.com/orgs/justeat?page=1"))
            {
            }
        }

        [Benchmark]
        public async Task RefitNewtonsoftJson()
        {
            _ = await _serviceNewtonsoftJson.GetOrganizationAsync("justeat");
        }

        [Benchmark]
        public async Task RefitSystemTextJson()
        {
            _ = await _serviceSystemTextJson.GetOrganizationAsync("justeat");
        }
    }
}
