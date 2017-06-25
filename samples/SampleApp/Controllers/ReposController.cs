// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SampleApp.Controllers
{
    [Route("api/[controller]")]
    public class ReposController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ReposController(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        [HttpGet]
        public async Task<ICollection<string>> Get(int? count = 100)
        {
            // Build the request
            string organization = _configuration["Organization"];
            string requestUri = $"https://api.github.com/orgs/{organization}/repos?per_page={count}";

            // Set up the HTTP client to call the GitHub API
            SetRequestHeaders(_httpClient);

            // Get the organization's repositories
            string json = await _httpClient.GetStringAsync(requestUri);
            var respositories = JsonConvert.DeserializeObject<JArray>(json);

            // Return the repositories' names
            var names = new List<string>();

            foreach (JObject repository in respositories)
            {
                names.Add(repository.Value<string>("name"));
            }

            names.Sort();

            return names;
        }

        private void SetRequestHeaders(HttpClient httpClient)
        {
            string userAgent = _configuration["UserAgent"];
            string version = typeof(ReposController).GetTypeInfo().Assembly.GetName().Version.ToString(3);

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(userAgent, version));
        }
    }
}
