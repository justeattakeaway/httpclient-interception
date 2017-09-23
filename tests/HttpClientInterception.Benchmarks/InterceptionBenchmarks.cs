// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json.Linq;
using Refit;

namespace JustEat.HttpClientInterception
{
    public class InterceptionBenchmarks
    {
        private readonly HttpClientInterceptorOptions _options;
        private readonly HttpClient _client;
        private readonly IGitHub _service;

        public InterceptionBenchmarks()
        {
            var builderForBytes = new HttpRequestInterceptionBuilder()
                .ForHttps()
                .ForHost("files.domain.com")
                .ForPath("setup.exe")
                .WithMediaType("application/octet-stream")
                .WithContent(() => new byte[] { 0, 1, 2, 3, 4 });

            var builderForHtml = new HttpRequestInterceptionBuilder()
                .ForHttp()
                .ForHost("www.google.co.uk")
                .ForPath("search")
                .ForQuery("q=Just+Eat")
                .WithMediaType("text/html")
                .WithContent(@"<!DOCTYPE html><html dir=""ltr"" lang=""en""><head><title>Just Eat</title></head></html>");

            var builderForJson = new HttpRequestInterceptionBuilder()
                .ForHttps()
                .ForHost("api.github.com")
                .ForPath("orgs/justeat")
                .WithMediaType("application/json")
                .WithJsonContent(new { id = 1516790, login = "justeat", url = "https://api.github.com/orgs/justeat" });

            var builderForStream = new HttpRequestInterceptionBuilder()
                .ForHttps()
                .ForHost("api.github.com")
                .ForPath("orgs/justeat")
                .ForQuery("page=1")
                .WithMediaType("application/json")
                .WithContentStream(() => File.OpenRead("organization.json"));

            _options = new HttpClientInterceptorOptions()
                .Register(builderForBytes)
                .Register(builderForHtml)
                .Register(builderForJson)
                .Register(builderForStream);

            _client = _options.CreateHttpClient();
            _service = RestService.For<IGitHub>(_options.CreateHttpClient("https://api.github.com"));
        }

        [Benchmark]
        public async Task GetBytes()
        {
            await _client.GetByteArrayAsync("https://files.domain.com/setup.exe");
        }

        [Benchmark]
        public async Task GetHtml()
        {
            await _client.GetStringAsync("http://www.google.co.uk/search?q=Just+Eat");
        }

        [Benchmark]
        public async Task GetJson()
        {
            string json = await _client.GetStringAsync("https://api.github.com/orgs/justeat");
            JObject.Parse(json);
        }

        [Benchmark]
        public async Task GetStream()
        {
            using (await _client.GetStreamAsync("https://api.github.com/orgs/justeat?page=1"))
            {
            }
        }

        [Benchmark]
        public async Task Refit()
        {
            await _service.GetOrganizationAsync("justeat");
        }
    }
}
