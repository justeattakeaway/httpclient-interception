// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SampleApp.Services;

namespace SampleApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReposController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IGitHub _github;

        public ReposController(IConfiguration configuration, IGitHub github)
        {
            _configuration = configuration;
            _github = github;
        }

        [HttpGet]
        public async Task<ICollection<string>> Get(int? count = 100)
        {
            // Get the configured organization's repositories
            string organization = _configuration["Organization"];

            ICollection<Repository> repositories = await _github.GetRepositoriesAsync(organization, count);

            // Return the repositories' names
            var names = new List<string>();

            foreach (var repository in repositories)
            {
                names.Add(repository.Name);
            }

            names.Sort();

            return names;
        }
    }
}
