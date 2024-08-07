// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Refit;

namespace JustEat.HttpClientInterception;

[EventPipeProfiler(EventPipeProfile.CpuSampling)]
[MemoryDiagnoser]
public class InterceptionBenchmarks
{
    private readonly HttpClientInterceptorOptions _options;
    private readonly HttpClient _client;
    private readonly IGitHub _service;

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
            .WithContent(() => [0, 1, 2, 3, 4])
            .RegisterWith(_options);

        builder
            .Requests()
            .ForHttps()
            .ForHost("api.github.com")
            .ForPath("orgs/justeattakeaway")
            .ForQuery(string.Empty)
            .Responds()
            .WithMediaType("application/json")
            .WithJsonContent(new { id = 1516790, login = "justeattakeaway", url = "https://api.github.com/orgs/justeattakeaway" })
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
        _service = RestService.For<IGitHub>(_options.CreateHttpClient("https://api.github.com"), refitSettings);
#pragma warning restore CA2000
    }

    [Benchmark]
    public async Task<byte[]> GetBytes()
        => await _client.GetByteArrayAsync("https://files.domain.com/setup.exe");

    [Benchmark]
    public async Task<string> GetHtml()
        => await _client.GetStringAsync("http://www.google.co.uk/search?q=Just+Eat");

    [Benchmark]
    public async Task<JsonDocument> GetJsonDocument()
    {
        using var stream = await _client.GetStreamAsync("https://api.github.com/orgs/justeattakeaway");
        return await JsonDocument.ParseAsync(stream);
    }

    [Benchmark]
    public async Task<GitHubOrganization> GetJsonObject()
        => await _client.GetFromJsonAsync<GitHubOrganization>("https://api.github.com/orgs/justeattakeaway");

#if !NETFRAMEWORK
    [Benchmark]
    public async Task<GitHubOrganization> GetJsonObjectSourceGenerator()
        => await _client.GetFromJsonAsync("https://api.github.com/orgs/justeattakeaway", GitHubJsonSerializerContext.Default.GitHubOrganization);
#endif

    [Benchmark]
    public async Task<GitHubOrganization> GetJsonObjectWithRefit()
        => await _service.GetOrganizationAsync("justeattakeaway");

    [Benchmark]
    public async Task<Stream> GetStream()
        => await _client.GetStreamAsync("https://api.github.com/orgs/justeattakeaway?page=1");
}
